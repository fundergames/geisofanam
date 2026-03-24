using System;
using UnityEngine;

namespace RogueDeal.HexLevels
{
    /// <summary>
    /// Represents a hex coordinate using axial (q, r) coordinate system.
    /// Axial coordinates are simpler for hex math than offset coordinates.
    /// </summary>
    [Serializable]
    public struct HexCoordinate : IEquatable<HexCoordinate>
    {
        public int q; // Column (like x in square grids)
        public int r; // Row (like y in square grids, but offset)

        public HexCoordinate(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        /// <summary>
        /// The third coordinate in cube coordinates (s = -q - r).
        /// Useful for some hex algorithms.
        /// </summary>
        public int s => -q - r;

        /// <summary>
        /// Convert world position to hex coordinate.
        /// </summary>
        public static HexCoordinate FromWorldPosition(Vector3 worldPos, float hexSize)
        {
            // Axial coordinate conversion from world position
            // Hex size is the distance from center to corner (outer radius)
            float sqrt3 = Mathf.Sqrt(3f);
            float q = (sqrt3 / 3f * worldPos.x - 1f / 3f * worldPos.z) / hexSize;
            float r = (2f / 3f * worldPos.z) / hexSize;
            
            return RoundToHex(q, r);
        }

        /// <summary>
        /// Convert hex coordinate to world position.
        /// </summary>
        public Vector3 ToWorldPosition(float hexSize)
        {
            float sqrt3 = Mathf.Sqrt(3f);
            float x = hexSize * (sqrt3 * q + sqrt3 / 2f * r);
            float z = hexSize * (3f / 2f * r);
            return new Vector3(x, 0f, z);
        }

        /// <summary>
        /// Round fractional hex coordinates to nearest integer hex.
        /// </summary>
        private static HexCoordinate RoundToHex(float q, float r)
        {
            float s = -q - r;
            
            // Round to nearest integer
            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(s);
            
            // Check if rounding preserved constraint (q + r + s = 0)
            float qDiff = Mathf.Abs(rq - q);
            float rDiff = Mathf.Abs(rr - r);
            float sDiff = Mathf.Abs(rs - s);
            
            // If constraint broken, fix by rounding the coordinate with largest difference
            if (rq + rr + rs != 0)
            {
                if (qDiff > rDiff && qDiff > sDiff)
                {
                    rq = -rr - rs;
                }
                else if (rDiff > sDiff)
                {
                    rr = -rq - rs;
                }
                else
                {
                    rs = -rq - rr;
                }
            }
            
            return new HexCoordinate(rq, rr);
        }

        /// <summary>
        /// Get all 6 neighboring hex coordinates.
        /// </summary>
        public HexCoordinate[] GetNeighbors()
        {
            return new HexCoordinate[]
            {
                new HexCoordinate(q + 1, r),      // East
                new HexCoordinate(q + 1, r - 1),  // Northeast
                new HexCoordinate(q, r - 1),      // Northwest
                new HexCoordinate(q - 1, r),       // West
                new HexCoordinate(q - 1, r + 1),  // Southwest
                new HexCoordinate(q, r + 1)       // Southeast
            };
        }

        /// <summary>
        /// Get a specific neighbor by direction index (0-5).
        /// </summary>
        public HexCoordinate GetNeighbor(int direction)
        {
            direction = ((direction % 6) + 6) % 6; // Normalize to 0-5
            
            HexCoordinate[] neighbors = GetNeighbors();
            return neighbors[direction];
        }

        /// <summary>
        /// Calculate distance to another hex coordinate.
        /// </summary>
        public int DistanceTo(HexCoordinate other)
        {
            return (Mathf.Abs(q - other.q) + 
                   Mathf.Abs(q + r - other.q - other.r) + 
                   Mathf.Abs(r - other.r)) / 2;
        }

        /// <summary>
        /// Get all hexes within a certain range (inclusive).
        /// </summary>
        public HexCoordinate[] GetHexesInRange(int range)
        {
            System.Collections.Generic.List<HexCoordinate> results = 
                new System.Collections.Generic.List<HexCoordinate>();
            
            for (int dq = -range; dq <= range; dq++)
            {
                int rMin = Mathf.Max(-range, -dq - range);
                int rMax = Mathf.Min(range, -dq + range);
                
                for (int dr = rMin; dr <= rMax; dr++)
                {
                    results.Add(new HexCoordinate(q + dq, r + dr));
                }
            }
            
            return results.ToArray();
        }

        // Equality operators
        public bool Equals(HexCoordinate other)
        {
            return q == other.q && r == other.r;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(q, r);
        }

        public static bool operator ==(HexCoordinate left, HexCoordinate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HexCoordinate left, HexCoordinate right)
        {
            return !left.Equals(right);
        }

        // Arithmetic operators
        public static HexCoordinate operator +(HexCoordinate a, HexCoordinate b)
        {
            return new HexCoordinate(a.q + b.q, a.r + b.r);
        }

        public static HexCoordinate operator -(HexCoordinate a, HexCoordinate b)
        {
            return new HexCoordinate(a.q - b.q, a.r - b.r);
        }

        public static HexCoordinate operator *(HexCoordinate a, int scalar)
        {
            return new HexCoordinate(a.q * scalar, a.r * scalar);
        }

        public override string ToString()
        {
            return $"Hex({q}, {r})";
        }
    }
}
