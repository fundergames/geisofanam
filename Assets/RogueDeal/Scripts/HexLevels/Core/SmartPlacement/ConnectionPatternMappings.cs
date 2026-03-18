using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.HexLevels
{
    /// <summary>
    /// ScriptableObject to store connection pattern mappings for roads, rivers, and coast tiles.
    /// This allows mappings to persist and be shared across projects.
    /// </summary>
    [CreateAssetMenu(fileName = "ConnectionPatternMappings", menuName = "Hex Levels/Connection Pattern Mappings", order = 1)]
    public class ConnectionPatternMappings : ScriptableObject
    {
        [System.Serializable]
        public class PatternMapping
        {
            public int pattern; // Bitmask
            public string variant; // e.g., "A", "B", "M"
        }

        [Header("Road Mappings")]
        public List<PatternMapping> roadMappings = new List<PatternMapping>();

        [Header("River Mappings")]
        public List<PatternMapping> riverMappings = new List<PatternMapping>();

        [Header("Coast Mappings")]
        public List<PatternMapping> coastMappings = new List<PatternMapping>();

        /// <summary>
        /// Get variant for a road pattern.
        /// </summary>
        public string GetRoadVariant(int pattern)
        {
            foreach (var mapping in roadMappings)
            {
                if (mapping.pattern == pattern)
                    return mapping.variant;
            }
            return null; // No mapping found
        }

        /// <summary>
        /// Get variant for a river pattern.
        /// </summary>
        public string GetRiverVariant(int pattern)
        {
            foreach (var mapping in riverMappings)
            {
                if (mapping.pattern == pattern)
                    return mapping.variant;
            }
            return null; // No mapping found
        }

        /// <summary>
        /// Get variant for a coast pattern.
        /// </summary>
        public string GetCoastVariant(int pattern)
        {
            foreach (var mapping in coastMappings)
            {
                if (mapping.pattern == pattern)
                    return mapping.variant;
            }
            return null; // No mapping found
        }

        /// <summary>
        /// Set a road mapping.
        /// </summary>
        public void SetRoadMapping(int pattern, string variant)
        {
            // Remove existing
            roadMappings.RemoveAll(m => m.pattern == pattern);
            // Add new
            roadMappings.Add(new PatternMapping { pattern = pattern, variant = variant });
        }

        /// <summary>
        /// Set a river mapping.
        /// </summary>
        public void SetRiverMapping(int pattern, string variant)
        {
            riverMappings.RemoveAll(m => m.pattern == pattern);
            riverMappings.Add(new PatternMapping { pattern = pattern, variant = variant });
        }

        /// <summary>
        /// Set a coast mapping.
        /// </summary>
        public void SetCoastMapping(int pattern, string variant)
        {
            coastMappings.RemoveAll(m => m.pattern == pattern);
            coastMappings.Add(new PatternMapping { pattern = pattern, variant = variant });
        }
    }
}