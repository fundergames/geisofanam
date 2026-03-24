using UnityEngine;
using UnityEditor;
using RogueDeal.HexLevels;

namespace RogueDeal.HexLevels.Editor
{
    /// <summary>
    /// Custom editor for HexGridVisualizer that uses Handles for better Scene view drawing.
    /// Handles work better than Gizmos for always-visible lines.
    /// </summary>
    [CustomEditor(typeof(HexGridVisualizer))]
    [CanEditMultipleObjects]
    public class HexGridVisualizerEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            HexGridVisualizer visualizer = (HexGridVisualizer)target;

            if (visualizer == null || !visualizer.ShowGrid)
                return;

            HexGrid hexGrid = visualizer.GetComponent<HexGrid>();
            if (hexGrid == null || hexGrid.HexSize <= 0f)
                return;

            // Use public properties directly (not serializedObject in OnSceneGUI)
            int gridRadius = visualizer.GridRadius;
            Color gridColor = visualizer.GridColor;
            Color centerColor = visualizer.CenterColor;
            bool highlightCenter = visualizer.HighlightCenter;

            // Draw grid using Handles (always visible in Scene view)
            DrawHexGridWithHandles(hexGrid, gridRadius, gridColor, centerColor, highlightCenter);
        }

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();

            // Force repaint after changes
            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }

        private void DrawHexGridWithHandles(HexGrid hexGrid, int gridRadius, Color gridColor, Color centerColor, bool highlightCenter)
        {
            // Draw grid using Handles (always visible in Scene view)
            HexCoordinate centerHex = new HexCoordinate(0, 0);
            HexCoordinate[] hexes = centerHex.GetHexesInRange(gridRadius);

            HexGridVisualizer visualizer = (HexGridVisualizer)target;
            bool showCoords = visualizer != null && visualizer.ShowCoordinates;

            Handles.color = gridColor;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always; // Always draw on top

            // Get camera for distance calculation
            Camera sceneCam = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera : null;
            float baseFontSize = 14f; // Base font size for Scene view
            float referenceDistance = 20f; // Reference distance for scaling

            foreach (var hex in hexes)
            {
                if (!hexGrid.IsInBounds(hex))
                    continue;

                Vector3 worldPos = hexGrid.HexToWorld(hex);
                
                // Highlight center
                if (highlightCenter && hex == centerHex)
                {
                    Handles.color = centerColor;
                    DrawHexOutline(worldPos, hexGrid.HexSize);
                    Handles.color = gridColor;
                }
                else
                {
                    DrawHexOutline(worldPos, hexGrid.HexSize);
                }

                // Draw coordinate labels in Scene view with distance-based scaling
                if (showCoords)
                {
                    Vector3 labelPos = worldPos + Vector3.up * 0.2f; // Slightly above the hex
                    
                    // Calculate scaled font size based on camera distance
                    float scaledFontSize = baseFontSize;
                    if (sceneCam != null)
                    {
                        float distance = Vector3.Distance(sceneCam.transform.position, worldPos);
                        scaledFontSize = baseFontSize * (referenceDistance / Mathf.Max(distance, 1f));
                        scaledFontSize = Mathf.Clamp(scaledFontSize, 8f, 24f); // Clamp to reasonable range
                    }
                    
                    // Create label style with scaled size
                    GUIStyle labelStyle = new GUIStyle();
                    labelStyle.fontSize = Mathf.RoundToInt(scaledFontSize);
                    labelStyle.fontStyle = FontStyle.Bold;
                    labelStyle.normal.textColor = Color.white;
                    labelStyle.alignment = TextAnchor.MiddleCenter;
                    
                    Handles.Label(labelPos, hex.ToString(), labelStyle);
                }
            }
        }

        private void DrawHexOutline(Vector3 center, float size)
        {
            if (size <= 0f)
                return;

            float sqrt3 = Mathf.Sqrt(3f);
            Vector3[] corners = new Vector3[7]; // 7 points to close the loop (6 corners + back to first)
            
            // Flat-top hexagon (ensure Y is slightly above ground to avoid clipping)
            float yOffset = 0.1f;
            corners[0] = center + new Vector3(0f, yOffset, size);
            corners[1] = center + new Vector3(size * sqrt3 / 2f, yOffset, size / 2f);
            corners[2] = center + new Vector3(size * sqrt3 / 2f, yOffset, -size / 2f);
            corners[3] = center + new Vector3(0f, yOffset, -size);
            corners[4] = center + new Vector3(-size * sqrt3 / 2f, yOffset, -size / 2f);
            corners[5] = center + new Vector3(-size * sqrt3 / 2f, yOffset, size / 2f);
            corners[6] = corners[0]; // Close the loop

            // Draw using Handles.DrawPolyLine for better visibility
            Handles.DrawPolyLine(corners);
            
            // Also draw individual lines as backup
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                Handles.DrawLine(corners[i], corners[next]);
            }
        }
    }
}
