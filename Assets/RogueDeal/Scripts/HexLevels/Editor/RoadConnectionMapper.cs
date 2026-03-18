using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using RogueDeal.HexLevels;

namespace RogueDeal.HexLevels.Editor
{
    /// <summary>
    /// Helper script to generate road connection pattern mappings from analyzed tile images.
    /// This creates mappings for all road variants (A-M) based on visual analysis.
    /// </summary>
    public static class RoadConnectionMapper
    {
        /// <summary>
        /// Generate all road connection mappings based on visual analysis of tiles.
        /// Maps each variant to its connection pattern(s), including all rotations.
        /// </summary>
        public static Dictionary<int, string> GenerateRoadMappings()
        {
            Dictionary<int, string> mappings = new Dictionary<int, string>();
            
            // Based on image analysis (top-right to left, top to bottom):
            // Direction mapping: 0=East, 1=NE, 2=NW, 3=West, 4=SW, 5=SE
            // Image edges: Edge1=NE(1), Edge2=East(0), Edge3=SE(5), Edge4=SW(4), Edge5=West(3), Edge6=NW(2)
            //
            // Row 1:
            // Tile 1: Straight NE-SW (Edge1=1, Edge4=4) → bitmask 0b010010 = 18 → hex_road_A
            // Tile 2: Straight NW-SE (Edge6=2, Edge3=5) → bitmask 0b100001 = 33 → hex_road_A (rotated)
            // Tile 3: Straight E-W (Edge2=0, Edge5=3) → bitmask 0b001100 = 12 → hex_road_A (rotated)
            // Tile 4: Curve NW-E (Edge6=2, Edge2=0) → bitmask 0b100100 = 36 → hex_road_B
            // Tile 5: Curve NE-W (Edge1=1, Edge5=3) → bitmask 0b010001 = 17 → hex_road_B (rotated)
            // Tile 6: Curve SW-NE (Edge4=4, Edge1=1) → bitmask 0b010010 = 18 → hex_road_B (curved version of Tile 1)
            //
            // Row 2:
            // Tile 7: Straight E-W (same as Tile 3) → bitmask 0b001100 = 12 → hex_road_A
            // Tile 8: T-junction NW-W-SE (Edge6=2, Edge5=3, Edge3=5) → bitmask 0b101001 = 41 → hex_road_E
            // Tile 9: T-junction NE-E-SW (Edge1=1, Edge2=0, Edge4=4) → bitmask 0b010110 = 22 → hex_road_E (rotated)
            // Tile 10: Curved W-NE (Edge5=3, Edge1=1) → bitmask 0b010001 = 17 → hex_road_C (gentle curve, same pattern as Tile 5)
            // Tile 11: Curved NW-E (Edge6=2, Edge2=0) → bitmask 0b100100 = 36 → hex_road_C (gentle curve, same pattern as Tile 4)
            // Tile 12: Curved W-SE (Edge5=3, Edge3=5) → bitmask 0b001001 = 9 → hex_road_D
            //
            // Row 3:
            // Tile 13: T-junction W-SE-E (Edge5=3, Edge3=5, Edge2=0) → bitmask 0b001101 = 13 → hex_road_F
            // Tile 14: T-junction NW-NE-SE (Edge6=2, Edge1=1, Edge3=5) → bitmask 0b110001 = 49 → hex_road_G
            // Tile 15: Dead end W (Edge5=3 only) → bitmask 0b001000 = 8 → hex_road_H
            //
            // Row 4:
            // Tile 16: 6-way crossroad (all edges) → bitmask 0b111111 = 63 → hex_road_I
            // Tile 17: 5-way junction (all except SE=5) → bitmask 0b111110 = 62 → hex_road_J
            
            // hex_road_A: Straight lines (opposite connections)
            AddPatternAndRotations(mappings, 0b010010, "A"); // NE-SW (Tile 1)
            AddPatternAndRotations(mappings, 0b100001, "A"); // NW-SE (Tile 2)
            AddPatternAndRotations(mappings, 0b001100, "A"); // E-W (Tile 3, 7)
            
            // hex_road_B: Sharp L-curves (adjacent connections, sharp 90-degree turns)
            // Note: Some patterns overlap with A (e.g., 0b010010), but B is the curved visual variant
            AddPatternAndRotations(mappings, 0b100100, "B"); // NW-E (Tile 4)
            AddPatternAndRotations(mappings, 0b010001, "B"); // NE-W (Tile 5)
            AddPatternAndRotations(mappings, 0b010010, "B"); // SW-NE (Tile 6) - curved version of straight
            AddPatternAndRotations(mappings, 0b001010, "B"); // E-SW
            AddPatternAndRotations(mappings, 0b000101, "B"); // W-SE
            AddPatternAndRotations(mappings, 0b101000, "B"); // SW-NW
            AddPatternAndRotations(mappings, 0b011000, "B"); // SE-NE
            
            // hex_road_C: Gentle S-curves (adjacent connections, gentle curves)
            // Note: Same patterns as B but visually different (gentle vs sharp)
            AddPatternAndRotations(mappings, 0b010001, "C"); // W-NE (Tile 10) - gentle version
            AddPatternAndRotations(mappings, 0b100100, "C"); // NW-E (Tile 11) - gentle version
            AddPatternAndRotations(mappings, 0b001001, "C"); // W-SE (alternative gentle)
            AddPatternAndRotations(mappings, 0b010010, "C"); // NE-SW (alternative gentle)
            AddPatternAndRotations(mappings, 0b100001, "C"); // NW-SE (alternative gentle)
            AddPatternAndRotations(mappings, 0b001100, "C"); // E-W (alternative gentle)
            
            // hex_road_D: Another curve pattern
            AddPatternAndRotations(mappings, 0b001001, "D"); // W-SE (Tile 12)
            AddPatternAndRotations(mappings, 0b010010, "D"); // NE-SW (alternative)
            AddPatternAndRotations(mappings, 0b100100, "D"); // NW-E (alternative)
            AddPatternAndRotations(mappings, 0b001100, "D"); // E-W (alternative)
            AddPatternAndRotations(mappings, 0b100001, "D"); // NW-SE (alternative)
            AddPatternAndRotations(mappings, 0b010001, "D"); // NE-W (alternative)
            
            // hex_road_E: T-junctions (3 connections, standard T-shape)
            AddPatternAndRotations(mappings, 0b101001, "E"); // NW-W-SE (Tile 8)
            AddPatternAndRotations(mappings, 0b010110, "E"); // NE-E-SW (Tile 9)
            AddPatternAndRotations(mappings, 0b100011, "E"); // NW-SE-E
            AddPatternAndRotations(mappings, 0b011100, "E"); // NE-SW-W
            AddPatternAndRotations(mappings, 0b001101, "E"); // E-W-SE
            AddPatternAndRotations(mappings, 0b110010, "E"); // SW-NW-NE
            
            // hex_road_F: T-junction variant W-SE-E
            AddPatternAndRotations(mappings, 0b001101, "F"); // W-SE-E (Tile 13)
            AddPatternAndRotations(mappings, 0b010011, "F"); // E-NW-NE
            AddPatternAndRotations(mappings, 0b100110, "F"); // NW-SW-E
            AddPatternAndRotations(mappings, 0b011001, "F"); // NE-SE-W
            AddPatternAndRotations(mappings, 0b101100, "F"); // SW-NW-W
            AddPatternAndRotations(mappings, 0b110001, "F"); // SE-NE-E
            
            // hex_road_G: T-junction variant NW-NE-SE
            AddPatternAndRotations(mappings, 0b110001, "G"); // NW-NE-SE (Tile 14)
            AddPatternAndRotations(mappings, 0b011010, "G"); // NE-E-SW
            AddPatternAndRotations(mappings, 0b001101, "G"); // E-W-SE
            AddPatternAndRotations(mappings, 0b100110, "G"); // W-NW-SW
            AddPatternAndRotations(mappings, 0b010011, "G"); // SW-NE-NW
            AddPatternAndRotations(mappings, 0b101100, "G"); // SE-SW-W
            
            // hex_road_H: Dead ends (1 connection)
            AddPatternAndRotations(mappings, 0b001000, "H"); // W only (Tile 15)
            AddPatternAndRotations(mappings, 0b000100, "H"); // E only
            AddPatternAndRotations(mappings, 0b000010, "H"); // NE only
            AddPatternAndRotations(mappings, 0b000001, "H"); // NW only
            AddPatternAndRotations(mappings, 0b010000, "H"); // SW only
            AddPatternAndRotations(mappings, 0b100000, "H"); // SE only
            
            // hex_road_I: 6-way crossroad (all connections)
            mappings[0b111111] = "I"; // All 6 directions (only one pattern, rotation doesn't matter)
            
            // hex_road_J: 5-way junction (all except SE/Edge 3)
            // Pattern: All except SE (bitmask: 0b111110 = 62)
            AddPatternAndRotations(mappings, 0b111110, "J"); // All except SE
            AddPatternAndRotations(mappings, 0b111101, "J"); // All except SW
            AddPatternAndRotations(mappings, 0b111011, "J"); // All except W
            AddPatternAndRotations(mappings, 0b110111, "J"); // All except NW
            AddPatternAndRotations(mappings, 0b101111, "J"); // All except NE
            AddPatternAndRotations(mappings, 0b011111, "J"); // All except E
            
            // hex_road_K, L, M: Additional patterns that might exist
            // These would need to be identified from additional tiles or variations
            // For now, I'll leave them unmapped or you can add them based on your assets
            
            return mappings;
        }
        
        /// <summary>
        /// Add a pattern and all its rotations to the mappings dictionary.
        /// Each rotation represents the same tile rotated 60 degrees.
        /// </summary>
        private static void AddPatternAndRotations(Dictionary<int, string> mappings, int pattern, string variant)
        {
            // Add the pattern itself
            if (!mappings.ContainsKey(pattern))
            {
                mappings[pattern] = variant;
            }
            
            // Add all 5 rotations (60, 120, 180, 240, 300 degrees)
            int rotated = pattern;
            for (int rotation = 1; rotation < 6; rotation++)
            {
                rotated = RotateBitmask(rotated, 1); // Rotate by 1 step (60 degrees)
                if (!mappings.ContainsKey(rotated))
                {
                    mappings[rotated] = variant;
                }
            }
        }
        
        /// <summary>
        /// Rotate a bitmask by one step (60 degrees clockwise).
        /// </summary>
        private static int RotateBitmask(int bitmask, int steps)
        {
            int rotated = 0;
            for (int i = 0; i < 6; i++)
            {
                int newPos = (i + steps) % 6;
                if ((bitmask & (1 << i)) != 0)
                {
                    rotated |= (1 << newPos);
                }
            }
            return rotated;
        }
        
        /// <summary>
        /// Apply the generated mappings to a ConnectionPatternMappings asset.
        /// </summary>
        [MenuItem("Funder Games/Hex Levels/Apply Road Connection Mappings")]
        public static void ApplyRoadMappings()
        {
            const string DEFAULT_MAPPINGS_PATH = "Assets/RogueDeal/Resources/Data/HexLevels/ConnectionPatternMappings.asset";
            
            ConnectionPatternMappings mappingsAsset = AssetDatabase.LoadAssetAtPath<ConnectionPatternMappings>(DEFAULT_MAPPINGS_PATH);
            
            if (mappingsAsset == null)
            {
                if (EditorUtility.DisplayDialog("Create Asset?", 
                    "ConnectionPatternMappings asset not found. Create it?", 
                    "Yes", "Cancel"))
                {
                    // Create directory if needed
                    string directory = "Assets/RogueDeal/Resources/Data/HexLevels";
                    if (!AssetDatabase.IsValidFolder(directory))
                    {
                        string[] folders = directory.Replace("Assets/", "").Split('/');
                        string currentPath = "Assets";
                        foreach (string folder in folders)
                        {
                            string newPath = $"{currentPath}/{folder}";
                            if (!AssetDatabase.IsValidFolder(newPath))
                            {
                                AssetDatabase.CreateFolder(currentPath, folder);
                            }
                            currentPath = newPath;
                        }
                    }
                    
                    mappingsAsset = ScriptableObject.CreateInstance<ConnectionPatternMappings>();
                    AssetDatabase.CreateAsset(mappingsAsset, DEFAULT_MAPPINGS_PATH);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    return;
                }
            }
            
            // Generate mappings
            Dictionary<int, string> mappings = GenerateRoadMappings();
            
            // Apply to asset
            mappingsAsset.roadMappings.Clear();
            foreach (var kvp in mappings)
            {
                mappingsAsset.roadMappings.Add(new ConnectionPatternMappings.PatternMapping
                {
                    pattern = kvp.Key,
                    variant = kvp.Value
                });
            }
            
            EditorUtility.SetDirty(mappingsAsset);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Applied {mappings.Count} road connection mappings to {DEFAULT_MAPPINGS_PATH}");
            EditorUtility.DisplayDialog("Success", 
                $"Applied {mappings.Count} road connection pattern mappings!\n\n" +
                $"Mappings include all rotations for each pattern variant.", 
                "OK");
        }
    }
}
