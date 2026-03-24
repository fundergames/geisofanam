using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RogueDeal.HexLevels
{
    /// <summary>
    /// Simple visualizer for testing the hex grid system.
    /// Draws hex outlines in the scene view and game view.
    /// </summary>
    [RequireComponent(typeof(HexGrid))]
    public class HexGridVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private bool showGrid = true;
        [SerializeField] private bool showCoordinates = false;
        [SerializeField] private Color gridColor = Color.cyan;
        [SerializeField] private int gridRadius = 10;
        [SerializeField] private bool highlightCenter = true;
        [SerializeField] private Color centerColor = Color.yellow;
        
        [Header("Coordinate Text Settings")]
        [Tooltip("Base font size for coordinates (will scale with distance)")]
        [SerializeField] private float coordinateFontSize = 16f;
        [Tooltip("Reference distance for font size scaling")]
        [SerializeField] private float referenceDistance = 20f;

        private HexGrid _hexGrid;

        // Public properties for editor access
        public bool ShowGrid => showGrid;
        public bool ShowCoordinates => showCoordinates;
        public Color GridColor => gridColor;
        public int GridRadius => gridRadius;
        public Color CenterColor => centerColor;
        public bool HighlightCenter => highlightCenter;
        
        public void SetGridVisibility(bool visible)
        {
            showGrid = visible;
        }

        private void Awake()
        {
            _hexGrid = GetComponent<HexGrid>();
        }

        private void OnDrawGizmos()
        {
            if (!showGrid)
                return;

            // Get hex grid (works in both edit and play mode)
            if (_hexGrid == null)
                _hexGrid = GetComponent<HexGrid>();

            if (_hexGrid == null || _hexGrid.HexSize <= 0f)
                return;

            // Draw a grid of hexes around the origin
            HexCoordinate centerHex = new HexCoordinate(0, 0);
            HexCoordinate[] hexes = centerHex.GetHexesInRange(gridRadius);

            foreach (var hex in hexes)
            {
                if (!_hexGrid.IsInBounds(hex))
                    continue;

                Vector3 worldPos = _hexGrid.HexToWorld(hex);
                
                // Highlight center hex
                if (highlightCenter && hex == centerHex)
                {
                    Gizmos.color = centerColor;
                    DrawHexGizmo(worldPos, _hexGrid.HexSize);
                }
                else
                {
                    Gizmos.color = gridColor;
                    DrawHexGizmo(worldPos, _hexGrid.HexSize);
                }
            }
        }

        private void DrawHexGizmo(Vector3 center, float size)
        {
            if (size <= 0f)
                return; // Safety check

            // Calculate hex corners (flat-top hexagon)
            // For flat-top hex, we need to use the proper orientation
            Vector3[] corners = new Vector3[6];
            float sqrt3 = Mathf.Sqrt(3f);
            
            // Flat-top hexagon corners - ensure Y offset to avoid z-fighting
            float yOffset = 0.05f; // Slightly above ground
            corners[0] = center + new Vector3(0f, yOffset, size);                    // Top
            corners[1] = center + new Vector3(size * sqrt3 / 2f, yOffset, size / 2f);  // Top-right
            corners[2] = center + new Vector3(size * sqrt3 / 2f, yOffset, -size / 2f);   // Bottom-right
            corners[3] = center + new Vector3(0f, yOffset, -size);                   // Bottom
            corners[4] = center + new Vector3(-size * sqrt3 / 2f, yOffset, -size / 2f);  // Bottom-left
            corners[5] = center + new Vector3(-size * sqrt3 / 2f, yOffset, size / 2f);   // Top-left

            // Draw lines between corners - color should already be set by caller
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                Gizmos.DrawLine(corners[i], corners[next]);
            }
        }

        // Draw gizmos when selected (for better visibility)
        private void OnDrawGizmosSelected()
        {
            if (!showGrid)
                return;

            if (_hexGrid == null)
                _hexGrid = GetComponent<HexGrid>();

            if (_hexGrid == null || _hexGrid.HexSize <= 0f)
                return;

            // Draw same grid but with brighter color when selected
            Color originalColor = Gizmos.color;
            Gizmos.color = new Color(gridColor.r, gridColor.g, gridColor.b, 1f); // Full opacity when selected

            HexCoordinate centerHex = new HexCoordinate(0, 0);
            HexCoordinate[] hexes = centerHex.GetHexesInRange(gridRadius);

            foreach (var hex in hexes)
            {
                if (!_hexGrid.IsInBounds(hex))
                    continue;

                Vector3 worldPos = _hexGrid.HexToWorld(hex);
                DrawHexGizmo(worldPos, _hexGrid.HexSize);
            }

            Gizmos.color = originalColor;
        }

        private void OnGUI()
        {
            if (!showCoordinates)
                return;

            if (_hexGrid == null)
                _hexGrid = GetComponent<HexGrid>();

            if (_hexGrid == null)
                return;

            // Draw coordinates on screen (simple debug)
            // This is a basic implementation - can be improved with proper UI
            Camera cam = Camera.main;
            if (cam == null)
                cam = FindFirstObjectByType<Camera>();

            if (cam != null)
            {
                HexCoordinate center = new HexCoordinate(0, 0);
                HexCoordinate[] hexes = center.GetHexesInRange(Mathf.Min(gridRadius, 5));

                foreach (var hex in hexes)
                {
                    Vector3 worldPos = _hexGrid.HexToWorld(hex);
                    Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

                    if (screenPos.z > 0)
                    {
                        // Calculate distance from camera to hex
                        float distance = Vector3.Distance(cam.transform.position, worldPos);
                        
                        // Scale font size to maintain consistent screen size
                        // Font size scales inversely with distance
                        float scaledFontSize = coordinateFontSize * (referenceDistance / Mathf.Max(distance, 1f));
                        scaledFontSize = Mathf.Clamp(scaledFontSize, 8f, 32f); // Clamp to reasonable range
                        
                        // Create font style with scaled size
                        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                        labelStyle.fontSize = Mathf.RoundToInt(scaledFontSize);
                        labelStyle.fontStyle = FontStyle.Bold;
                        labelStyle.normal.textColor = Color.white;
                        labelStyle.alignment = TextAnchor.MiddleCenter;

                        // Scale label rect size proportionally
                        float rectWidth = 100f * (scaledFontSize / coordinateFontSize);
                        float rectHeight = 30f * (scaledFontSize / coordinateFontSize);
                        
                        Rect labelRect = new Rect(screenPos.x - rectWidth * 0.5f, Screen.height - screenPos.y - rectHeight * 0.5f, rectWidth, rectHeight);
                        GUI.Label(labelRect, hex.ToString(), labelStyle);
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Force repaint when values change in inspector
            if (Application.isEditor && !Application.isPlaying)
            {
                SceneView.RepaintAll();
            }
        }
#endif
    }
}
