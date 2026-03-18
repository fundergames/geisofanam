using UnityEngine;
using UnityEditor;
using System.Linq;

namespace RogueDeal.HexLevels.Editor
{
    /// <summary>
    /// Utility to measure hex prefab size from KayKit assets.
    /// Measures the actual size of hex tiles to set correct HexSize.
    /// </summary>
    public static class HexPrefabSizeMeasurer
    {
        [MenuItem("Funder Games/Hex Levels/Measure Hex Prefab Size")]
        public static void MeasureHexPrefabSize()
        {
            // Try to find a hex grass prefab
            string[] guids = AssetDatabase.FindAssets("hex_grass t:Prefab");
            
            if (guids.Length == 0)
            {
                Debug.LogError("Could not find hex_grass prefab! Make sure KayKit assets are imported.");
                EditorUtility.DisplayDialog("Error", 
                    "Could not find hex_grass prefab.\n\n" +
                    "Make sure:\n" +
                    "1. KayKit assets are imported\n" +
                    "2. The prefab is in the expected location:\n" +
                    "   Assets/KayKit/Packs/.../Prefabs/tiles/base/hex_grass.prefab",
                    "OK");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null)
            {
                Debug.LogError($"Failed to load prefab at: {path}");
                return;
            }

            // Get the mesh from the prefab
            MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogError($"Prefab {prefab.name} has no MeshFilter or mesh!");
                return;
            }

            Mesh mesh = meshFilter.sharedMesh;
            Bounds bounds = mesh.bounds;
            
            // For a flat-top hexagon in Unity:
            // - Width (x-axis): 2 * outerRadius
            // - Height (z-axis): outerRadius * sqrt(3)
            // - Outer radius = distance from center to corner
            // - Inner radius = distance from center to edge = outerRadius * sqrt(3) / 2
            
            // The bounds give us the actual rendered size
            // For a flat-top hex, width should be larger than height
            float width = bounds.size.x;
            float height = bounds.size.z;
            
            // Outer radius can be calculated from width (width = 2 * outerRadius)
            float outerRadiusFromWidth = width / 2f;
            
            // OR from height (height = outerRadius * sqrt(3))
            float outerRadiusFromHeight = height / Mathf.Sqrt(3f);
            
            // Use the average or the width-based one (more accurate for flat-top)
            float outerRadius = outerRadiusFromWidth;
            
            // Double-check using actual vertex positions for more precision
            Vector3[] vertices = mesh.vertices;
            if (vertices.Length > 0)
            {
                float maxDistance = 0f;
                
                foreach (var vertex in vertices)
                {
                    // Measure distance from origin (assuming mesh is centered)
                    Vector2 vertex2D = new Vector2(vertex.x, vertex.z);
                    float distance = vertex2D.magnitude;
                    if (distance > maxDistance)
                        maxDistance = distance;
                }
                
                // If vertex-based measurement is significantly different, use it
                if (Mathf.Abs(maxDistance - outerRadius) > 0.01f)
                {
                    outerRadius = (outerRadius + maxDistance) / 2f; // Average
                    Debug.Log($"Vertex-based measurement differs: {maxDistance:F4} vs bounds: {outerRadiusFromWidth:F4}");
                }
            }
            
            // Calculate inner radius (distance from center to edge, not corner)
            float innerRadius = outerRadius * Mathf.Sqrt(3f) / 2f;
            
            // Verify: The spacing between hex centers should be:
            // Horizontal (q direction): sqrt(3) * outerRadius
            // Vertical (r direction): 1.5 * outerRadius
            float horizontalSpacing = Mathf.Sqrt(3f) * outerRadius;
            float verticalSpacing = 1.5f * outerRadius;

            string report = $"=== Hex Prefab Size Measurement ===\n" +
                          $"Prefab: {prefab.name}\n" +
                          $"Path: {path}\n\n" +
                          $"Bounds Size: {bounds.size}\n" +
                          $"Bounds Center: {bounds.center}\n\n" +
                          $"Outer Radius (center to corner): {outerRadius:F4}\n" +
                          $"Inner Radius (center to edge): {innerRadius:F4}\n\n" +
                          $"Expected Hex Spacing:\n" +
                          $"  Horizontal: {horizontalSpacing:F4}\n" +
                          $"  Vertical: {verticalSpacing:F4}\n\n" +
                          $"Recommended HexSize: {outerRadius:F4}\n\n" +
                          $"Note: HexSize should be the outer radius (distance from center to corner).\n" +
                          $"This ensures proper spacing between adjacent hexes.";

            Debug.Log(report);
            EditorUtility.DisplayDialog("Hex Size Measurement", report, "OK");
            
            // Try to find and update HexGrid in the scene
            HexGrid hexGrid = Object.FindFirstObjectByType<HexGrid>();
            if (hexGrid != null)
            {
                if (EditorUtility.DisplayDialog("Update HexGrid?", 
                    $"Found HexGrid in scene.\n\n" +
                    $"Current HexSize: {hexGrid.HexSize}\n" +
                    $"Measured HexSize: {outerRadius:F4}\n\n" +
                    $"Update HexSize to {outerRadius:F4}?",
                    "Update", "Cancel"))
                {
                    Undo.RecordObject(hexGrid, "Update HexSize from prefab measurement");
                    hexGrid.HexSize = outerRadius;
                    EditorUtility.SetDirty(hexGrid);
                    Debug.Log($"Updated HexGrid.HexSize to {outerRadius:F4}");
                }
            }
        }

        [MenuItem("Funder Games/Hex Levels/Measure Selected Prefab")]
        public static void MeasureSelectedPrefab()
        {
            GameObject selected = Selection.activeGameObject;
            
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a GameObject in the hierarchy to measure.", "OK");
                return;
            }

            MeshFilter meshFilter = selected.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                // Try to find MeshFilter in children
                meshFilter = selected.GetComponentInChildren<MeshFilter>();
            }

            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Selected GameObject '{selected.name}' has no MeshFilter component with a mesh.\n\n" +
                    "Select a prefab instance or GameObject with a mesh to measure.",
                    "OK");
                return;
            }

            Mesh mesh = meshFilter.sharedMesh;
            Bounds bounds = mesh.bounds;
            
            // Calculate considering the transform scale
            Vector3 scale = selected.transform.lossyScale;
            Vector3 scaledSize = new Vector3(
                bounds.size.x * scale.x,
                bounds.size.y * scale.y,
                bounds.size.z * scale.z
            );

            float outerRadius = Mathf.Max(scaledSize.x, scaledSize.z) / 2f;
            float innerRadius = outerRadius * Mathf.Sqrt(3f) / 2f;

            string report = $"=== Selected Object Measurement ===\n" +
                          $"Object: {selected.name}\n" +
                          $"Scale: {scale}\n\n" +
                          $"Mesh Bounds Size: {bounds.size}\n" +
                          $"Scaled Size: {scaledSize}\n\n" +
                          $"Outer Radius: {outerRadius:F4}\n" +
                          $"Inner Radius: {innerRadius:F4}\n\n" +
                          $"Recommended HexSize: {outerRadius:F4}";

            Debug.Log(report);
            EditorUtility.DisplayDialog("Object Size Measurement", report, "OK");
        }

        [MenuItem("Funder Games/Hex Levels/Calibrate Hex Size from Two Adjacent Tiles")]
        public static void CalibrateFromAdjacentTiles()
        {
            HexGrid hexGrid = Object.FindFirstObjectByType<HexGrid>();
            if (hexGrid == null)
            {
                EditorUtility.DisplayDialog("Error", 
                    "No HexGrid found in scene!\n\n" +
                    "Please add a HexGrid component to a GameObject first.",
                    "OK");
                return;
            }

            // Find hex grass prefab
            string[] guids = AssetDatabase.FindAssets("hex_grass t:Prefab");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", 
                    "Could not find hex_grass prefab.\n\n" +
                    "This tool needs a hex tile prefab to place and measure.",
                    "OK");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject hexPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (hexPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load prefab: {path}", "OK");
                return;
            }

            // Check if we should place test tiles
            GameObject existingTile1 = GameObject.Find("_HexCalibration_Tile1");
            GameObject existingTile2 = GameObject.Find("_HexCalibration_Tile2");

            if (existingTile1 == null || existingTile2 == null)
            {
                // Place two adjacent hexes for measurement
                if (!EditorUtility.DisplayDialog("Place Test Tiles?", 
                    "This will place two hex tiles adjacent to each other for measurement.\n\n" +
                    "Make sure you're in a test scene - tiles will be placed at origin.",
                    "Place Tiles", "Cancel"))
                {
                    return;
                }

                // Clean up old calibration tiles
                if (existingTile1 != null) Object.DestroyImmediate(existingTile1);
                if (existingTile2 != null) Object.DestroyImmediate(existingTile2);

                // Place first tile at origin (hex 0,0)
                HexCoordinate hex1 = new HexCoordinate(0, 0);
                Vector3 pos1 = hexGrid.HexToWorld(hex1);
                GameObject tile1 = (GameObject)PrefabUtility.InstantiatePrefab(hexPrefab);
                tile1.transform.position = pos1;
                tile1.name = "_HexCalibration_Tile1";
                
                // Place second tile adjacent (hex 1,0) - east neighbor
                HexCoordinate hex2 = new HexCoordinate(1, 0);
                Vector3 pos2 = hexGrid.HexToWorld(hex2);
                GameObject tile2 = (GameObject)PrefabUtility.InstantiatePrefab(hexPrefab);
                tile2.transform.position = pos2;
                tile2.name = "_HexCalibration_Tile2";

                Undo.RegisterCreatedObjectUndo(tile1, "Place calibration tiles");
                Undo.RegisterCreatedObjectUndo(tile2, "Place calibration tiles");

                EditorUtility.DisplayDialog("Tiles Placed", 
                    "Two test tiles have been placed.\n\n" +
                    "1. Look at the Scene view - check the spacing\n" +
                    "2. Select both tiles if you need to adjust their position\n" +
                    "3. Run this menu item again to calculate the correct HexSize\n\n" +
                    "The tiles should be touching with no gaps.",
                    "OK");
                
                Selection.activeGameObject = tile1;
                Selection.objects = new Object[] { tile1, tile2 };
                return;
            }

            // Measure actual spacing between the two tiles
            Vector3 tilePos1 = existingTile1.transform.position;
            Vector3 tilePos2 = existingTile2.transform.position;
            float actualSpacing = Vector3.Distance(tilePos1, tilePos2);

            // Calculate hex size from actual spacing
            // For horizontal neighbors (q direction), spacing = sqrt(3) * hexSize
            // So hexSize = spacing / sqrt(3)
            float calculatedHexSize = actualSpacing / Mathf.Sqrt(3f);

            // Also check if tiles are actually touching by measuring renderer bounds
            Renderer r1 = existingTile1.GetComponent<Renderer>();
            Renderer r2 = existingTile2.GetComponent<Renderer>();
            if (r1 == null) r1 = existingTile1.GetComponentInChildren<Renderer>();
            if (r2 == null) r2 = existingTile2.GetComponentInChildren<Renderer>();

            float gapSize = 0f;
            if (r1 != null && r2 != null)
            {
                Bounds b1 = r1.bounds;
                Bounds b2 = r2.bounds;
                
                // Calculate gap between the two tiles
                Vector3 direction = (tilePos2 - tilePos1).normalized;
                float distFromCenter1 = Vector3.Dot(direction, b1.max - tilePos1);
                float distFromCenter2 = Vector3.Dot(-direction, b2.max - tilePos2);
                
                // Distance from edge of tile1 to edge of tile2
                float edgeToEdge = actualSpacing - distFromCenter1 - distFromCenter2;
                gapSize = Mathf.Max(0f, edgeToEdge);
            }

            string report = $"=== Hex Size Calibration from Adjacent Tiles ===\n" +
                          $"Current HexSize: {hexGrid.HexSize:F4}\n\n" +
                          $"Tile 1 Position: {tilePos1}\n" +
                          $"Tile 2 Position: {tilePos2}\n" +
                          $"Actual Center-to-Center Distance: {actualSpacing:F4}\n\n" +
                          $"Expected Spacing: {hexGrid.HexSize * Mathf.Sqrt(3f):F4}\n" +
                          $"Difference: {Mathf.Abs(actualSpacing - hexGrid.HexSize * Mathf.Sqrt(3f)):F4}\n\n" +
                          $"Calculated HexSize from spacing: {calculatedHexSize:F4}\n";

            if (gapSize > 0.001f)
            {
                report += $"⚠️ Gap detected between tiles: {gapSize:F4}\n\n";
                report += $"To eliminate gap, HexSize should be: {calculatedHexSize:F4}\n";
            }
            else
            {
                report += $"✅ Tiles appear to be touching (no significant gap)\n\n";
            }

            Debug.Log(report);

            // Offer to update with fine-tuning options
            if (gapSize > 0.001f)
            {
                // If there's a gap, offer to compensate by increasing hex size slightly
                // The gap suggests the prefab might be slightly smaller than the grid spacing
                float compensationFactor = 1f + (gapSize / actualSpacing);
                float compensatedHexSize = calculatedHexSize * compensationFactor;

                int choice = EditorUtility.DisplayDialogComplex("Calibrate Hex Size", 
                    report + "\n\n" +
                    $"Gap detected! To eliminate gap, use compensated HexSize:\n" +
                    $"  Compensated: {compensatedHexSize:F4} (accounts for {gapSize:F4} gap)\n" +
                    $"  Or calculated: {calculatedHexSize:F4} (theoretical)\n\n" +
                    $"Which should we use?",
                    "Use Compensated (eliminate gap)", "Use Calculated", "Cancel");

                if (choice == 0) // Compensated
                {
                    Undo.RecordObject(hexGrid, "Calibrate HexSize with gap compensation");
                    hexGrid.HexSize = compensatedHexSize;
                    EditorUtility.SetDirty(hexGrid);
                    Debug.Log($"Updated HexGrid.HexSize to {compensatedHexSize:F4} (compensated for {gapSize:F4} gap)");
                }
                else if (choice == 1) // Calculated
                {
                    Undo.RecordObject(hexGrid, "Calibrate HexSize from adjacent tiles");
                    hexGrid.HexSize = calculatedHexSize;
                    EditorUtility.SetDirty(hexGrid);
                    Debug.Log($"Updated HexGrid.HexSize to {calculatedHexSize:F4}");
                }
                else // Cancel
                {
                    return;
                }

                // Clean up calibration tiles
                if (existingTile1 != null) Undo.DestroyObjectImmediate(existingTile1);
                if (existingTile2 != null) Undo.DestroyObjectImmediate(existingTile2);

                EditorUtility.DisplayDialog("Success", 
                    $"HexSize updated!\n\n" +
                    "Calibration tiles have been removed.\n" +
                    "Try placing new tiles to verify spacing is correct.\n\n" +
                    "If there's still a small gap, the hex prefab might not be perfectly regular.\n" +
                    "You can manually adjust HexSize slightly in the Inspector.",
                    "OK");
            }
            else
            {
                // No gap detected, just use calculated size
                if (EditorUtility.DisplayDialog("Calibrate Hex Size", 
                    report + "\n\nUpdate HexSize to calculated value?",
                    "Update & Cleanup", "Cancel"))
                {
                    Undo.RecordObject(hexGrid, "Calibrate HexSize from adjacent tiles");
                    hexGrid.HexSize = calculatedHexSize;
                    EditorUtility.SetDirty(hexGrid);

                    // Clean up calibration tiles
                    if (existingTile1 != null) Undo.DestroyObjectImmediate(existingTile1);
                    if (existingTile2 != null) Undo.DestroyObjectImmediate(existingTile2);

                    Debug.Log($"Updated HexGrid.HexSize to {calculatedHexSize:F4} and cleaned up calibration tiles");
                    
                    EditorUtility.DisplayDialog("Success", 
                        $"HexSize updated to {calculatedHexSize:F4}!\n\n" +
                        "Calibration tiles have been removed.\n" +
                        "Try placing new tiles to verify spacing is correct.",
                        "OK");
                }
            }
        }

        [MenuItem("Funder Games/Hex Levels/Calibrate Hex Size from Selected Hex Tile")]
        public static void CalibrateFromSelectedTile()
        {
            // Find HexGrid in scene
            HexGrid hexGrid = Object.FindFirstObjectByType<HexGrid>();
            if (hexGrid == null)
            {
                EditorUtility.DisplayDialog("Error", 
                    "No HexGrid found in scene!\n\n" +
                    "Please add a HexGrid component to a GameObject first.",
                    "OK");
                return;
            }

            // Get selected GameObject (should be a placed hex tile)
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Instructions", 
                    "1. Place a hex tile prefab at the origin (0,0,0)\n" +
                    "2. Select the placed tile in the hierarchy\n" +
                    "3. Run this menu item again\n\n" +
                    "This will measure the actual tile size and update HexSize accordingly.",
                    "OK");
                return;
            }

            // Measure the actual bounds of the selected tile (in world space)
            Renderer renderer = selected.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = selected.GetComponentInChildren<Renderer>();
            }

            if (renderer == null)
            {
                EditorUtility.DisplayDialog("Error", 
                    "Selected GameObject has no Renderer component.\n\n" +
                    "Please select a placed hex tile with a visible mesh.",
                    "OK");
                return;
            }

            Bounds worldBounds = renderer.bounds;
            Vector3 size = worldBounds.size;
            Vector3 center = worldBounds.center;
            
            // For a flat-top hexagon:
            // Width (x-axis) = 2 * outerRadius
            // Height (z-axis) = outerRadius * sqrt(3)
            
            // Calculate outer radius from both dimensions
            float outerRadiusFromWidth = size.x / 2f;
            float outerRadiusFromHeight = size.z / Mathf.Sqrt(3f);
            
            // Use the average for better accuracy
            float outerRadius = (outerRadiusFromWidth + outerRadiusFromHeight) / 2f;
            
            // Also check if the tile is centered at origin
            float distanceFromOrigin = new Vector2(center.x, center.z).magnitude;
            bool isCentered = distanceFromOrigin < 0.1f;
            
            string report = $"=== Hex Size Calibration ===\n" +
                          $"Selected Tile: {selected.name}\n" +
                          $"Tile Center: {center}\n" +
                          $"Tile Size: {size}\n" +
                          $"Distance from Origin: {distanceFromOrigin:F4}\n" +
                          $"Is Centered: {(isCentered ? "Yes" : "No - consider placing at origin")}\n\n" +
                          $"Outer Radius (from width): {outerRadiusFromWidth:F4}\n" +
                          $"Outer Radius (from height): {outerRadiusFromHeight:F4}\n" +
                          $"Average Outer Radius: {outerRadius:F4}\n\n" +
                          $"Current HexSize: {hexGrid.HexSize:F4}\n" +
                          $"Recommended HexSize: {outerRadius:F4}\n\n" +
                          $"Expected spacing with this size:\n" +
                          $"  Horizontal: {outerRadius * Mathf.Sqrt(3f):F4}\n" +
                          $"  Vertical: {outerRadius * 1.5f:F4}";

            Debug.Log(report);
            
            if (!isCentered)
            {
                EditorUtility.DisplayDialog("Warning", 
                    report + "\n\n⚠️ Tile is not centered at origin. Results may be less accurate.\n\n" +
                    "Consider moving the tile to (0,0,0) for better measurement.",
                    "Continue Anyway", "Cancel");
            }

            // Offer to update
            if (EditorUtility.DisplayDialog("Calibrate Hex Size", 
                report + "\n\nUpdate HexSize to measured value?",
                "Update", "Cancel"))
            {
                Undo.RecordObject(hexGrid, "Calibrate HexSize from placed tile");
                hexGrid.HexSize = outerRadius;
                EditorUtility.SetDirty(hexGrid);
                Debug.Log($"Updated HexGrid.HexSize to {outerRadius:F4}");
                
                EditorUtility.DisplayDialog("Success", 
                    $"HexSize updated to {outerRadius:F4}!\n\n" +
                    "Place another tile adjacent to verify spacing is correct.",
                    "OK");
            }
        }
    }
}
