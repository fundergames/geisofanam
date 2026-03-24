using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RogueDeal.HexLevels
{
    /// <summary>
    /// Selects appropriate tile variants based on neighboring tile context.
    /// Handles coast, road, and river auto-connection logic.
    /// Uses pre-oriented prefab variants (A-M) with NO rotation.
    /// </summary>
    public static class SmartTileSelector
    {
        // Prefab lookup delegate - set by editor system
        public delegate GameObject PrefabLookupDelegate(string searchPattern);
        private static PrefabLookupDelegate _prefabLookup;

        // Mappings asset - set by editor system (DEPRECATED - use database instead)
        private static ConnectionPatternMappings _mappingsAsset;
        
        // Road pattern database - OLD mapping system
        private static RoadPatternDatabase _roadDatabase;
        
        // Road pattern database - NEW prototype-based system
        private static RoadPatternDatabase_New _roadDatabaseV2;

        /// <summary>
        /// Set the prefab lookup function (called by editor system).
        /// </summary>
        public static void SetPrefabLookup(PrefabLookupDelegate lookup)
        {
            _prefabLookup = lookup;
        }

        /// <summary>
        /// Set the mappings asset (called by editor system).
        /// </summary>
        public static void SetMappingsAsset(ConnectionPatternMappings mappings)
        {
            _mappingsAsset = mappings;
        }
        
        /// <summary>
        /// Set the road pattern database (OLD mapping system).
        /// </summary>
        public static void SetRoadDatabase(RoadPatternDatabase database)
        {
            _roadDatabase = database;
            UnityEngine.Debug.Log($"[SmartTileSelector] Road database (old) set with {database?.mappings.Count ?? 0} mappings");
        }
        
        /// <summary>
        /// Set the road pattern database (NEW prototype-based system).
        /// </summary>
        public static void SetRoadDatabaseV2(RoadPatternDatabase_New database)
        {
            _roadDatabaseV2 = database;
            UnityEngine.Debug.Log($"[SmartTileSelector] Road database v2 set with {database?.prototypes.Count ?? 0} prototypes");
        }
        
        /// <summary>
        /// Get the current V2 database (for external checks).
        /// </summary>
        public static RoadPatternDatabase_New GetDatabaseV2()
        {
            return _roadDatabaseV2;
        }
        /// <summary>
        /// Get all possible tile prefabs that match the connection pattern.
        /// Returns a list of all variants that could be used for this pattern.
        /// </summary>
        /// <param name="hex">Hex coordinate to place at</param>
        /// <param name="grid">Hex grid</param>
        /// <param name="desiredType">Type of tile to place</param>
        /// <param name="fallbackPrefab">Fallback prefab if smart selection fails</param>
        /// <param name="rotation">Rotation index (0-5 for 60-degree increments). Used to adjust connection pattern.</param>
        /// <returns>List of all matching prefabs, ordered by preference</returns>
        public static List<GameObject> GetAllMatchingTilePrefabs(HexCoordinate hex, HexGrid grid, HexTileType desiredType, GameObject fallbackPrefab = null, int rotation = 0)
        {
            List<GameObject> results = new List<GameObject>();
            HexContextAnalyzer.HexContext context = HexContextAnalyzer.AnalyzeContext(hex, grid);

            switch (desiredType)
            {
                case HexTileType.Coast:
                    // For coast, return all variants that match the water count
                    results.AddRange(GetAllCoastVariants(context));
                    break;
                case HexTileType.Road:
                    results.AddRange(GetAllRoadVariants(context, grid, hex, rotation));
                    break;
                case HexTileType.River:
                    results.AddRange(GetAllRiverVariants(context, grid, hex, rotation));
                    break;
                case HexTileType.Grass:
                    GameObject grass = FindGrassPrefab(fallbackPrefab);
                    if (grass != null) results.Add(grass);
                    break;
                case HexTileType.Water:
                    GameObject water = FindWaterPrefab(fallbackPrefab);
                    if (water != null) results.Add(water);
                    break;
            }
            
            // Always add fallback if provided and not already in list
            if (fallbackPrefab != null && !results.Contains(fallbackPrefab))
            {
                results.Add(fallbackPrefab);
            }
            
            return results;
        }
        
        /// <summary>
        /// Result of smart tile selection including rotation.
        /// </summary>
        public struct SelectionResult
        {
            public GameObject prefab;
            public int rotationDegrees;
        }
        
        /// <summary>
        /// Select the best tile prefab AND rotation based on context.
        /// NEW: Returns rotation from database for proper orientation.
        /// </summary>
        public static SelectionResult SelectTilePrefabWithRotation(HexCoordinate hex, HexGrid grid, HexTileType desiredType, GameObject fallbackPrefab = null, int userRotation = -1)
        {
            return SelectTilePrefabWithRotation(hex, grid, desiredType, fallbackPrefab, userRotation, null, 0);
        }
        
        /// <summary>
        /// Select prefab and rotation for a tile, optionally preserving the current variant if it's sufficient.
        /// </summary>
        /// <param name="currentVariant">Current variant name (e.g., "A", "E"), or null if new placement</param>
        /// <param name="currentRotation">Current rotation index (0-5), ignored if currentVariant is null</param>
        public static SelectionResult SelectTilePrefabWithRotation(HexCoordinate hex, HexGrid grid, HexTileType desiredType, GameObject fallbackPrefab, int userRotation, string currentVariant, int currentRotation)
        {
            SelectionResult result = new SelectionResult();
            result.prefab = fallbackPrefab;
            result.rotationDegrees = 0;
            
            if (desiredType == HexTileType.Road && _roadDatabaseV2 != null)
            {
                int roadBitmask = HexContextAnalyzer.GetNeighborBitmask(hex, grid, HexTileType.Road);
                
                // CRITICAL: Check if current variant can already support the required connections
                // This preserves tiles that were placed with user intent for future expansion
                if (!string.IsNullOrEmpty(currentVariant))
                {
                    // Check if we can keep the current variant at current rotation
                    bool supportsAtCurrent = _roadDatabaseV2.CanSupportPattern(currentVariant, currentRotation, roadBitmask);
                    UnityEngine.Debug.Log($"[SelectTilePrefabWithRotation] Checking preservation: variant {currentVariant} at {currentRotation*60}° vs pattern {roadBitmask} - supports: {supportsAtCurrent}");
                    
                    if (supportsAtCurrent)
                    {
                        GameObject currentPrefab = FindRoadPrefab(currentVariant, null);
                        if (currentPrefab != null)
                        {
                            result.prefab = currentPrefab;
                            result.rotationDegrees = currentRotation * 60;
                            UnityEngine.Debug.Log($"[SelectTilePrefabWithRotation] Current variant {currentVariant} at {currentRotation*60}° already supports pattern {roadBitmask}, preserving it");
                            return result;
                        }
                    }
                    
                    // If current rotation doesn't work, try other rotations of the same variant
                    for (int testRotation = 0; testRotation < 6; testRotation++)
                    {
                        if (testRotation == currentRotation)
                            continue; // Already tested above
                            
                        if (_roadDatabaseV2.CanSupportPattern(currentVariant, testRotation, roadBitmask))
                        {
                            GameObject currentPrefab = FindRoadPrefab(currentVariant, null);
                            if (currentPrefab != null)
                            {
                                result.prefab = currentPrefab;
                                result.rotationDegrees = testRotation * 60;
                                UnityEngine.Debug.Log($"[SelectTilePrefabWithRotation] Current variant {currentVariant} can support pattern {roadBitmask} at {testRotation*60}°, rotating from {currentRotation*60}° to preserve variant");
                                return result;
                            }
                        }
                    }
                }
                
                // NEW: User-controlled placement with rotation preservation
                if (userRotation >= 0)
                {
                    // User specified a rotation - calculate which connections are needed
                    // based on their desired visual direction + neighbor requirements
                    int geometricRotation;
                    int desiredPattern = CalculateDesiredPattern(roadBitmask, userRotation, out geometricRotation);
                    
                    string variant;
                    
                    // PRIORITY 1: Find a variant that matches the pattern at the user's exact rotation
                    if (_roadDatabaseV2.FindMatchAtRotation(desiredPattern, geometricRotation, out variant))
                    {
                        GameObject prefab = FindRoadPrefab(variant, null);
                        if (prefab != null)
                        {
                            result.prefab = prefab;
                            result.rotationDegrees = userRotation * 60; // Use user's visual rotation for display
                            UnityEngine.Debug.Log($"[SelectTilePrefabWithRotation] User wants pattern {desiredPattern} at rotation {userRotation} (geo {geometricRotation}) -> {variant} at {userRotation*60}°");
                            return result;
                        }
                    }
                    
                    // PRIORITY 2: Find a variant at ANY rotation that gives the desired connections
                    int optimalRotation;
                    if (_roadDatabaseV2.FindMatch(desiredPattern, out variant, out optimalRotation))
                    {
                        GameObject prefab = FindRoadPrefab(variant, null);
                        if (prefab != null)
                        {
                            result.prefab = prefab;
                            result.rotationDegrees = optimalRotation * 60; // Use the rotation that makes connections work
                            UnityEngine.Debug.Log($"[SelectTilePrefabWithRotation] User wants pattern {desiredPattern} (visual rotation {userRotation}) but no match at that rotation -> {variant} at {optimalRotation*60}° (actual rotation)");
                            return result;
                        }
                    }
                    
                    // Fallback: Try to find a variant using ONLY neighbor connections
                    // (Drops the user's straight requirement if it creates an impossible pattern)
                    if (_roadDatabaseV2.FindMatch(roadBitmask, out variant, out optimalRotation))
                    {
                        GameObject prefab = FindRoadPrefab(variant, null);
                        if (prefab != null)
                        {
                            result.prefab = prefab;
                            result.rotationDegrees = optimalRotation * 60;
                            UnityEngine.Debug.Log($"[SelectTilePrefabWithRotation] Pattern {desiredPattern} not found, using neighbor-only pattern {roadBitmask} -> {variant} at {optimalRotation*60}°");
                            return result;
                        }
                    }
                    
                    // Final fallback: use fallback prefab at user rotation
                    result.rotationDegrees = userRotation * 60;
                    UnityEngine.Debug.Log($"[SelectTilePrefabWithRotation] No match found, using fallback at {userRotation*60}°");
                    return result;
                }
                
                // AUTO mode: Let database pick rotation
                string variant2;
                int rotation2;
                
                if (_roadDatabaseV2.FindMatch(roadBitmask, out variant2, out rotation2))
                {
                    GameObject prefab = FindRoadPrefab(variant2, null);
                    if (prefab != null)
                    {
                        result.prefab = prefab;
                        result.rotationDegrees = rotation2;
                        UnityEngine.Debug.Log($"[SelectTilePrefabWithRotation] Auto mode: Pattern {roadBitmask} -> {variant2} at {rotation2}°");
                        return result;
                    }
                }
                else
                {
                    // No match found in database - use fallback with current rotation
                    UnityEngine.Debug.Log($"[SelectTilePrefabWithRotation] Pattern {roadBitmask} not in database, using fallback");
                    return result; // Returns fallbackPrefab
                }
            }
            
            // Fallback: try old database system
            if (desiredType == HexTileType.Road && _roadDatabase != null)
            {
                int roadBitmask = HexContextAnalyzer.GetNeighborBitmask(hex, grid, HexTileType.Road);
                string variant = _roadDatabase.GetPrefabVariant(roadBitmask);
                int rotation = _roadDatabase.GetRotation(roadBitmask);
                
                if (!string.IsNullOrEmpty(variant))
                {
                    GameObject prefab = FindRoadPrefab(variant, null);
                    if (prefab != null)
                    {
                        result.prefab = prefab;
                        result.rotationDegrees = rotation;
                        UnityEngine.Debug.Log($"[SelectTilePrefabWithRotation] Pattern {roadBitmask} -> {variant} at {rotation}°");
                        return result;
                    }
                }
            }
            
            // Fallback to old method
            result.prefab = SelectTilePrefab(hex, grid, desiredType, fallbackPrefab, 0);
            return result;
        }
        
        /// <summary>
        /// Calculate the desired connection pattern based on user's rotation + neighbor connections.
        /// User rotation defines the visual "straight" direction, neighbors add required connections.
        /// Hex direction mapping: 0=E, 1=NE, 2=NW, 3=W, 4=SW, 5=SE
        /// Visual rotation mapping (as shown in editor):
        ///   0,3 → E-W (bits 0+3)
        ///   1,4 → NW-SE (bits 2+5)
        ///   2,5 → SW-NE (bits 4+1)
        /// </summary>
        private static int CalculateDesiredPattern(int neighborBitmask, int userRotation, out int geometricRotation)
        {
            // Convert screen rotation to geometric rotation
            // Screen has N↔S flipped: user rotation 1(SE-NW screen) = geo 5(SE-NW geo)
            // Mapping: 0→0, 1→5, 2→4, 3→3, 4→2, 5→1
            if (userRotation == 0 || userRotation == 3)
            {
                geometricRotation = userRotation; // E-W unchanged
            }
            else
            {
                geometricRotation = 6 - userRotation; // Flip N↔S: 1→5, 2→4, 4→2, 5→1
            }
            
            int straightDir1, straightDir2;
            string directionName = "";
            
            // Map rotation to one of 3 straight orientations
            // Each pair of opposite rotations (0°/180°, 60°/240°, 120°/300°) forms the same line
            int orientation = geometricRotation % 3;
            
            // Map orientation to straight direction bits
            // User rotation → geometric rotation → orientation (geomRot % 3)
            // User 0 → Geo 0 → Orient 0 → E-W (bits 0, 3)
            // User 1 → Geo 1 → Orient 1 → NE-SW (bits 1, 4)
            // User 5 → Geo 5 → Orient 2 → NW-SE (bits 2, 5)
            if (orientation == 0)
            {
                straightDir1 = 0; // E
                straightDir2 = 3; // W
                directionName = "E-W";
            }
            else if (orientation == 1)
            {
                straightDir1 = 1; // NE
                straightDir2 = 4; // SW
                directionName = "NE-SW";
            }
            else // orientation == 2
            {
                straightDir1 = 2; // NW
                straightDir2 = 5; // SE
                directionName = "NW-SE";
            }
            
            // Check if we can actually add a straight direction
            // We can only add straight bits if:
            // 1. No neighbors exist (free placement), OR
            // 2. Neighbors are aligned with the straight direction (form a line)
            
            bool canAddStraight = false;
            
            if (neighborBitmask == 0)
            {
                // No neighbors - can place any straight
                canAddStraight = true;
            }
            else
            {
                // Check if existing neighbors are compatible with this straight
                // Neighbors are compatible if they lie on the straight line OR don't conflict
                int straightMask = (1 << straightDir1) | (1 << straightDir2);
                
                // Count how many neighbor connections exist
                int neighborCount = 0;
                for (int i = 0; i < 6; i++)
                {
                    if ((neighborBitmask & (1 << i)) != 0) neighborCount++;
                }
                
                // If we only have 1 neighbor, we can extend into a straight that includes it
                // If we have 2 neighbors on opposite sides (straight line), keep that straight
                if (neighborCount == 1)
                {
                    canAddStraight = true;
                }
                else if (neighborCount == 2)
                {
                    // Check if the 2 neighbors form a straight line (opposite directions)
                    for (int dir = 0; dir < 3; dir++)
                    {
                        int opposite = dir + 3;
                        if ((neighborBitmask & (1 << dir)) != 0 && (neighborBitmask & (1 << opposite)) != 0)
                        {
                            // Neighbors form a straight line - preserve it
                            canAddStraight = false;
                            break;
                        }
                    }
                    if (!canAddStraight)
                    {
                        // Neighbors don't form a line (curve or junction) - can't add straight
                        canAddStraight = false;
                    }
                }
                else
                {
                    // 3+ neighbors - definitely a junction, can't add straight
                    canAddStraight = false;
                }
            }
            
            int desiredPattern;
            if (canAddStraight)
            {
                // Start with the straight connections
                desiredPattern = (1 << straightDir1) | (1 << straightDir2);
                // Add neighbor connections
                desiredPattern |= neighborBitmask;
            }
            else
            {
                // Can't add straight - use only neighbor connections
                desiredPattern = neighborBitmask;
            }
            
            UnityEngine.Debug.Log($"[CalculateDesiredPattern] Visual rotation {userRotation} → geometric rotation {geometricRotation} (orientation {orientation}, {directionName}) -> straight bits {straightDir1}+{straightDir2}, neighbors {neighborBitmask} (0b{System.Convert.ToString(neighborBitmask, 2).PadLeft(6, '0')}), canAddStraight={canAddStraight}, combined pattern {desiredPattern} (0b{System.Convert.ToString(desiredPattern, 2).PadLeft(6, '0')})");
            
            return desiredPattern;
        }
        
        /// <summary>
        /// Analyze neighboring road tiles and suggest a preferred rotation that maintains visual alignment.
        /// Returns rotation in 60° steps (0-5), or -1 if no preference.
        /// </summary>
        private static int GetPreferredRotationFromNeighbors(HexCoordinate hex, HexGrid grid, int roadBitmask)
        {
            // Count how many neighbors we have
            int neighborCount = 0;
            for (int i = 0; i < 6; i++)
            {
                if ((roadBitmask & (1 << i)) != 0)
                    neighborCount++;
            }
            
            // If we only have 1-2 neighbors, try to match their rotation for visual continuity
            if (neighborCount <= 2)
            {
                // Find the first neighbor and get its rotation
                for (int dir = 0; dir < 6; dir++)
                {
                    if ((roadBitmask & (1 << dir)) != 0)
                    {
                        int neighborRot = HexContextAnalyzer.GetNeighborRotation(hex, grid, dir);
                        if (neighborRot >= 0)
                        {
                            UnityEngine.Debug.Log($"[GetPreferredRotation] Found neighbor in direction {dir} with rotation {neighborRot} ({neighborRot * 60}°)");
                            return neighborRot;
                        }
                    }
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// Select the best tile prefab based on context and desired tile type.
        /// </summary>
        /// <param name="hex">Hex coordinate to place at</param>
        /// <param name="grid">Hex grid</param>
        /// <param name="desiredType">Type of tile to place</param>
        /// <param name="fallbackPrefab">Fallback prefab if smart selection fails</param>
        /// <param name="rotation">Rotation index (0-5 for 60-degree increments). Used to adjust connection pattern.</param>
        public static GameObject SelectTilePrefab(HexCoordinate hex, HexGrid grid, HexTileType desiredType, GameObject fallbackPrefab = null, int rotation = 0)
        {
            HexContextAnalyzer.HexContext context = HexContextAnalyzer.AnalyzeContext(hex, grid);

            switch (desiredType)
            {
                case HexTileType.Coast:
                    return SelectCoastTile(context, fallbackPrefab);
                case HexTileType.Road:
                    return SelectRoadTile(context, grid, hex, fallbackPrefab, rotation);
                case HexTileType.River:
                    return SelectRiverTile(context, grid, hex, fallbackPrefab, rotation);
                case HexTileType.Grass:
                    return SelectGrassTile(context, fallbackPrefab);
                case HexTileType.Water:
                    return SelectWaterTile(context, fallbackPrefab);
                default:
                    return fallbackPrefab;
            }
        }

        /// <summary>
        /// Select appropriate coast tile variant based on water neighbors.
        /// Coast tiles connect land to water.
        /// </summary>
        private static GameObject SelectCoastTile(HexContextAnalyzer.HexContext context, GameObject fallback)
        {
            // Coast tiles are typically selected based on which sides have water
            // For now, use a simple selection based on water neighbor count
            // A-E variants likely represent different water connection patterns
            
            int waterCount = context.waterNeighborCount;
            
            // Map water neighbor count to coast variant
            // This is a simplified mapping - you may need to adjust based on actual prefab meanings
            string variant = "A"; // Default
            
            if (waterCount == 1)
                variant = "A"; // Single water connection
            else if (waterCount == 2)
                variant = "B"; // Two water connections
            else if (waterCount == 3)
                variant = "C"; // Three water connections
            else if (waterCount >= 4)
                variant = "D"; // Many water connections
            else
                variant = "E"; // No water (edge case)

            return FindCoastPrefab(variant, fallback);
        }

        /// <summary>
        /// Select appropriate road tile variant based on road connections.
        /// Roads connect to other roads.
        /// </summary>
        private static GameObject SelectRoadTile(HexContextAnalyzer.HexContext context, HexGrid grid, HexCoordinate hex, GameObject fallback, int rotation)
        {
            // Get bitmask of road neighbors
            int roadBitmask = HexContextAnalyzer.GetNeighborBitmask(hex, grid, HexTileType.Road);
            
            // Rotate the bitmask to account for tile rotation
            // Rotation is in 60-degree increments (0-5), so we rotate the bitmask by that many positions
            int rotatedBitmask = RotateBitmask(roadBitmask, rotation);
            
            #if UNITY_EDITOR
            // UnityEngine.Debug.Log($"[SelectRoadTile] Hex: {hex}, Rotation: {rotation}, World bitmask: {roadBitmask} (0b{System.Convert.ToString(roadBitmask, 2).PadLeft(6, '0')}), Local bitmask: {rotatedBitmask} (0b{System.Convert.ToString(rotatedBitmask, 2).PadLeft(6, '0')})");
            #endif
            
            // First, try to use saved mappings from asset
            if (_mappingsAsset != null)
            {
                string variant = _mappingsAsset.GetRoadVariant(rotatedBitmask);
                if (!string.IsNullOrEmpty(variant))
                {
                    #if UNITY_EDITOR
                    UnityEngine.Debug.Log($"[SelectRoadTile] Found mapping: pattern {rotatedBitmask} -> variant {variant}");
                    #endif
                    GameObject prefab = FindRoadPrefab(variant, null);
                    if (prefab != null)
                        return prefab;
                }
                else
                {
                    #if UNITY_EDITOR
                    UnityEngine.Debug.Log($"[SelectRoadTile] No mapping found for pattern {rotatedBitmask}, using fallback logic");
                    #endif
                }
            }
            
            // Fallback to default logic if no mapping found
            int roadCount = context.roadNeighborCount;
            string defaultVariant = DetermineRoadVariant(rotatedBitmask, roadCount);
            #if UNITY_EDITOR
            UnityEngine.Debug.Log($"[SelectRoadTile] Fallback: roadCount={roadCount}, rotatedBitmask={rotatedBitmask} (0b{System.Convert.ToString(rotatedBitmask, 2).PadLeft(6, '0')}), selected variant={defaultVariant}");
            #endif
            GameObject result = FindRoadPrefab(defaultVariant, fallback);
            #if UNITY_EDITOR
            UnityEngine.Debug.Log($"[SelectRoadTile] FindRoadPrefab returned: {(result != null ? result.name : "null")}, fallback was: {(fallback != null ? fallback.name : "null")}");
            #endif
            return result;
        }

        /// <summary>
        /// Select appropriate river tile variant based on river connections.
        /// </summary>
        private static GameObject SelectRiverTile(HexContextAnalyzer.HexContext context, HexGrid grid, HexCoordinate hex, GameObject fallback, int rotation)
        {
            // Get bitmask of river neighbors
            int riverBitmask = HexContextAnalyzer.GetNeighborBitmask(hex, grid, HexTileType.River);
            
            // Rotate the bitmask to account for tile rotation
            int rotatedBitmask = RotateBitmask(riverBitmask, rotation);
            
            // First, try to use saved mappings from asset
            if (_mappingsAsset != null)
            {
                string variant = _mappingsAsset.GetRiverVariant(rotatedBitmask);
                if (!string.IsNullOrEmpty(variant))
                {
                    GameObject prefab = FindRiverPrefab(variant, null);
                    if (prefab != null)
                        return prefab;
                }
            }
            
            // Fallback to default logic if no mapping found
            int riverCount = context.riverNeighborCount;
            string defaultVariant = DetermineRiverVariant(rotatedBitmask, riverCount);
            return FindRiverPrefab(defaultVariant, fallback);
        }

        private static GameObject SelectGrassTile(HexContextAnalyzer.HexContext context, GameObject fallback)
        {
            // Simple grass tile - no variants needed for now
            return FindGrassPrefab(fallback);
        }

        private static GameObject SelectWaterTile(HexContextAnalyzer.HexContext context, GameObject fallback)
        {
            // Simple water tile
            return FindWaterPrefab(fallback);
        }
        
        /// <summary>
        /// Get all road variants that match the connection pattern.
        /// </summary>
        private static List<GameObject> GetAllRoadVariants(HexContextAnalyzer.HexContext context, HexGrid grid, HexCoordinate hex, int rotation)
        {
            List<GameObject> variants = new List<GameObject>();
            
            // Get bitmask of road neighbors
            int roadBitmask = HexContextAnalyzer.GetNeighborBitmask(hex, grid, HexTileType.Road);
            int rotatedBitmask = RotateBitmask(roadBitmask, rotation);
            
            // First, try to get all variants from mappings that match this pattern
            if (_mappingsAsset != null)
            {
                // Get the primary variant from mappings
                string primaryVariant = _mappingsAsset.GetRoadVariant(rotatedBitmask);
                if (!string.IsNullOrEmpty(primaryVariant))
                {
                    GameObject primaryPrefab = FindRoadPrefab(primaryVariant, null);
                    if (primaryPrefab != null)
                    {
                        variants.Add(primaryPrefab);
                    }
                    
                    // Also find all other variants that map to the same pattern
                    // (in case multiple variants are valid for the same pattern)
                    foreach (var mapping in _mappingsAsset.roadMappings)
                    {
                        if (mapping.pattern == rotatedBitmask && mapping.variant != primaryVariant)
                        {
                            GameObject variantPrefab = FindRoadPrefab(mapping.variant, null);
                            if (variantPrefab != null && !variants.Contains(variantPrefab))
                            {
                                variants.Add(variantPrefab);
                            }
                        }
                    }
                }
            }
            
            // If no variants found from mappings, try fallback logic
            if (variants.Count == 0)
            {
                int roadCount = context.roadNeighborCount;
                string defaultVariant = DetermineRoadVariant(rotatedBitmask, roadCount);
                GameObject fallbackPrefab = FindRoadPrefab(defaultVariant, null);
                if (fallbackPrefab != null)
                {
                    variants.Add(fallbackPrefab);
                }
            }
            
            return variants;
        }
        
        /// <summary>
        /// Get all river variants that match the connection pattern.
        /// </summary>
        private static List<GameObject> GetAllRiverVariants(HexContextAnalyzer.HexContext context, HexGrid grid, HexCoordinate hex, int rotation)
        {
            List<GameObject> variants = new List<GameObject>();
            
            int riverBitmask = HexContextAnalyzer.GetNeighborBitmask(hex, grid, HexTileType.River);
            int rotatedBitmask = RotateBitmask(riverBitmask, rotation);
            
            if (_mappingsAsset != null)
            {
                string primaryVariant = _mappingsAsset.GetRiverVariant(rotatedBitmask);
                if (!string.IsNullOrEmpty(primaryVariant))
                {
                    GameObject primaryPrefab = FindRiverPrefab(primaryVariant, null);
                    if (primaryPrefab != null)
                    {
                        variants.Add(primaryPrefab);
                    }
                    
                    // Find other variants for same pattern
                    foreach (var mapping in _mappingsAsset.riverMappings)
                    {
                        if (mapping.pattern == rotatedBitmask && mapping.variant != primaryVariant)
                        {
                            GameObject variantPrefab = FindRiverPrefab(mapping.variant, null);
                            if (variantPrefab != null && !variants.Contains(variantPrefab))
                            {
                                variants.Add(variantPrefab);
                            }
                        }
                    }
                }
            }
            
            return variants;
        }
        
        /// <summary>
        /// Get all coast variants that match the water neighbor count.
        /// </summary>
        private static List<GameObject> GetAllCoastVariants(HexContextAnalyzer.HexContext context)
        {
            List<GameObject> variants = new List<GameObject>();
            
            // Try all coast variants (A through E typically)
            string[] coastVariants = { "A", "B", "C", "D", "E" };
            foreach (string variant in coastVariants)
            {
                GameObject prefab = FindCoastPrefab(variant, null);
                if (prefab != null && !variants.Contains(prefab))
                {
                    variants.Add(prefab);
                }
            }
            
            return variants;
        }

        /// <summary>
        /// Determine road variant based on connection pattern.
        /// Uses bitmask to identify specific connection patterns.
        /// </summary>
        private static string DetermineRoadVariant(int roadBitmask, int roadCount)
        {
            // Common patterns:
            // 2 connections opposite (straight): A
            // 2 connections adjacent (curve): B, C, D, etc.
            // 3 connections (T-junction): E, F, G, etc.
            // 4+ connections (intersection): H, I, J, etc.
            
            if (roadCount == 0)
                return "A"; // No connections - default straight
            
            if (roadCount == 1)
                return "A"; // Dead end - use straight
            
            if (roadCount == 2)
            {
                // Check if opposite (straight) or adjacent (curve)
                if (IsOppositeConnections(roadBitmask))
                    return "A"; // Straight
                else
                    return "B"; // Curve
            }
            
            if (roadCount == 3)
                return "E"; // T-junction
            
            if (roadCount >= 4)
                return "H"; // Intersection
            
            return "A"; // Default
        }

        /// <summary>
        /// Determine river variant based on connection pattern.
        /// </summary>
        private static string DetermineRiverVariant(int riverBitmask, int riverCount)
        {
            // Similar logic to roads, but rivers may have different variants
            if (riverCount == 0)
                return "A"; // No connections
            
            if (riverCount == 1)
                return "A"; // Dead end
            
            if (riverCount == 2)
            {
                if (IsOppositeConnections(riverBitmask))
                    return "A"; // Straight
                else
                    return "A_curvy"; // Curved
            }
            
            if (riverCount == 3)
                return "C"; // T-junction
            
            if (riverCount >= 4)
                return "crossing_A"; // Crossing
            
            return "A"; // Default
        }

        /// <summary>
        /// Check if bitmask represents opposite connections (straight line).
        /// </summary>
        private static bool IsOppositeConnections(int bitmask)
        {
            // Check for opposite pairs: (0,3), (1,4), (2,5)
            return ((bitmask & (1 << 0)) != 0 && (bitmask & (1 << 3)) != 0) ||
                   ((bitmask & (1 << 1)) != 0 && (bitmask & (1 << 4)) != 0) ||
                   ((bitmask & (1 << 2)) != 0 && (bitmask & (1 << 5)) != 0);
        }

        /// <summary>
        /// Convert a world-space connection bitmask to a tile-local bitmask based on rotation.
        /// 
        /// The input bitmask represents connections in absolute world-space directions:
        ///   0 = East, 1 = NE, 2 = NW, 3 = West, 4 = SW, 5 = SE
        /// 
        /// When a tile is rotated R steps clockwise, its local direction 0 points to world direction R.
        /// To find the variant that matches, we need to convert world directions to tile-local directions.
        /// 
        /// Example: If tile is rotated 1 step clockwise (R=1):
        ///   - World direction 1 (NE) → tile-local direction 0 (because local 0 now points to world 1)
        ///   - World direction 2 (NW) → tile-local direction 1
        ///   - So: local direction L = (world direction W - R) mod 6
        ///   - Or: world direction W = (local direction L + R) mod 6
        /// 
        /// To convert world bitmask to local bitmask:
        ///   - If world direction W has a connection, then local direction (W - R) mod 6 should have it
        ///   - So: output[local] = input[(local + R) mod 6]
        /// </summary>
        /// <param name="bitmask">World-space connection bitmask</param>
        /// <param name="rotation">Tile rotation in 60-degree increments (0-5, clockwise)</param>
        /// <returns>Tile-local connection bitmask for variant lookup</returns>
        private static int RotateBitmask(int bitmask, int rotation)
        {
            if (rotation == 0)
                return bitmask;

            // Normalize rotation to 0-5 range
            rotation = ((rotation % 6) + 6) % 6;
            
            // Convert world-space bitmask to tile-local bitmask
            // If tile is rotated R clockwise, world direction W maps to local direction (W - R) mod 6
            // So: output[local] = input[(local + R) mod 6]
            int rotated = 0;
            for (int localDir = 0; localDir < 6; localDir++)
            {
                // Local direction localDir corresponds to world direction (localDir + rotation) mod 6
                int worldDir = (localDir + rotation) % 6;
                if ((bitmask & (1 << worldDir)) != 0)
                {
                    rotated |= (1 << localDir);
                }
            }
            
            return rotated;
        }

        // Prefab finding methods
        private static GameObject FindCoastPrefab(string variant, GameObject fallback)
        {
            if (_prefabLookup != null)
            {
                GameObject prefab = _prefabLookup($"hex_coast_{variant}");
                if (prefab != null) return prefab;
            }
            return fallback;
        }

        private static GameObject FindRoadPrefab(string variant, GameObject fallback)
        {
            if (_prefabLookup != null)
            {
                string searchName = $"hex_road_{variant}";
                GameObject prefab = _prefabLookup(searchName);
                #if UNITY_EDITOR
                UnityEngine.Debug.Log($"[FindRoadPrefab] Looking for '{searchName}', found: {(prefab != null ? prefab.name : "null")}");
                #endif
                if (prefab != null) return prefab;
            }
            #if UNITY_EDITOR
            UnityEngine.Debug.Log($"[FindRoadPrefab] Returning fallback: {(fallback != null ? fallback.name : "null")}");
            #endif
            return fallback;
        }

        private static GameObject FindRiverPrefab(string variant, GameObject fallback)
        {
            if (_prefabLookup != null)
            {
                GameObject prefab = _prefabLookup($"hex_river_{variant}");
                if (prefab != null) return prefab;
            }
            return fallback;
        }

        private static GameObject FindGrassPrefab(GameObject fallback)
        {
            if (_prefabLookup != null)
            {
                GameObject prefab = _prefabLookup("hex_grass");
                if (prefab != null) return prefab;
            }
            return fallback;
        }

        private static GameObject FindWaterPrefab(GameObject fallback)
        {
            if (_prefabLookup != null)
            {
                GameObject prefab = _prefabLookup("hex_water");
                if (prefab != null) return prefab;
            }
            return fallback;
        }
    }
}