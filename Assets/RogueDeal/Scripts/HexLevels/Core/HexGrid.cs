using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.HexLevels
{
    /// <summary>
    /// Core hex grid system that manages hex coordinates and tile/object data.
    /// Uses dictionary-based storage for efficient O(1) lookups.
    /// </summary>
    public class HexGrid : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private float hexSize = 1f;
        [SerializeField] private int maxWidth = 100;
        [SerializeField] private int maxHeight = 100;

        // Dictionary for O(1) hex lookup - only stores occupied hexes
        private Dictionary<HexCoordinate, HexTileData> _hexes = new Dictionary<HexCoordinate, HexTileData>();

        /// <summary>
        /// Size of each hex (distance from center to corner).
        /// </summary>
        public float HexSize
        {
            get => hexSize;
            set => hexSize = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Maximum grid dimensions.
        /// </summary>
        public Vector2Int MaxDimensions => new Vector2Int(maxWidth, maxHeight);

        /// <summary>
        /// Get the hex coordinate at a world position.
        /// </summary>
        public HexCoordinate WorldToHex(Vector3 worldPos)
        {
            return HexCoordinate.FromWorldPosition(worldPos, hexSize);
        }

        /// <summary>
        /// Get the world position of a hex coordinate.
        /// </summary>
        public Vector3 HexToWorld(HexCoordinate hex)
        {
            return hex.ToWorldPosition(hexSize);
        }

        /// <summary>
        /// Check if a hex coordinate is within grid bounds.
        /// </summary>
        public bool IsInBounds(HexCoordinate hex)
        {
            // For a 100x100 grid centered at origin, bounds are roughly -50 to +50
            int halfWidth = maxWidth / 2;
            int halfHeight = maxHeight / 2;
            
            // Check bounds with safe comparison to avoid Mathf.Abs overflow
            return hex.q >= -halfWidth && hex.q <= halfWidth && 
                   hex.r >= -halfHeight && hex.r <= halfHeight &&
                   hex.s >= -Mathf.Max(halfWidth, halfHeight) && hex.s <= Mathf.Max(halfWidth, halfHeight);
        }

        /// <summary>
        /// Get tile data at a hex coordinate, or null if empty.
        /// </summary>
        public HexTileData GetTile(HexCoordinate hex)
        {
            _hexes.TryGetValue(hex, out HexTileData data);
            return data;
        }

        /// <summary>
        /// Set tile data at a hex coordinate.
        /// </summary>
        public void SetTile(HexCoordinate hex, HexTileData data)
        {
            if (!IsInBounds(hex))
            {
                Debug.LogWarning($"Hex {hex} is out of bounds!");
                return;
            }

            if (data == null)
            {
                _hexes.Remove(hex);
            }
            else
            {
                _hexes[hex] = data;
            }
        }

        /// <summary>
        /// Check if a hex coordinate has a tile.
        /// </summary>
        public bool HasTile(HexCoordinate hex)
        {
            return _hexes.ContainsKey(hex);
        }

        /// <summary>
        /// Get all occupied hex coordinates.
        /// </summary>
        public IEnumerable<HexCoordinate> GetAllHexes()
        {
            return _hexes.Keys;
        }

        /// <summary>
        /// Get all tiles in the grid as a dictionary.
        /// </summary>
        public Dictionary<HexCoordinate, HexTileData> GetAllTiles()
        {
            return new Dictionary<HexCoordinate, HexTileData>(_hexes);
        }

        /// <summary>
        /// Get neighbors of a hex coordinate.
        /// </summary>
        public HexCoordinate[] GetNeighbors(HexCoordinate hex)
        {
            return hex.GetNeighbors();
        }

        /// <summary>
        /// Get all hexes within a range of a center hex.
        /// </summary>
        public HexCoordinate[] GetHexesInRange(HexCoordinate center, int range)
        {
            return center.GetHexesInRange(range);
        }

        /// <summary>
        /// Clear all tiles from the grid.
        /// </summary>
        public void Clear()
        {
            _hexes.Clear();
        }

        /// <summary>
        /// Get the number of occupied hexes.
        /// </summary>
        public int Count => _hexes.Count;

        /// <summary>
        /// Draw debug visualization of the grid in the editor.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            // Draw occupied hexes
            Gizmos.color = Color.cyan;
            foreach (var hex in _hexes.Keys)
            {
                Vector3 worldPos = HexToWorld(hex);
                DrawHexGizmo(worldPos, hexSize);
            }
        }

        private void DrawHexGizmo(Vector3 center, float size)
        {
            // Draw hex outline
            Vector3[] corners = new Vector3[6];
            float sqrt3 = Mathf.Sqrt(3f);
            
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                corners[i] = center + new Vector3(
                    size * Mathf.Cos(angle),
                    0f,
                    size * Mathf.Sin(angle)
                );
            }

            // Draw lines between corners
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                Gizmos.DrawLine(corners[i], corners[next]);
            }
        }
    }
}
