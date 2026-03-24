using UnityEngine;
using System.Collections.Generic;

namespace RogueDeal.HexLevels
{
    [CreateAssetMenu(fileName = "RoadPatternDatabase", menuName = "Hex Levels/Road Pattern Database")]
    public class RoadPatternDatabase : ScriptableObject
    {
        [System.Serializable]
        public class PatternPrefabMapping
        {
            public int pattern;
            public string prefabVariant;
            public int rotationDegrees;
            public string description;
        }

        public List<PatternPrefabMapping> mappings = new List<PatternPrefabMapping>();

        public string GetPrefabVariant(int pattern)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.pattern == pattern)
                    return mapping.prefabVariant;
            }
            return null;
        }
        
        public int GetRotation(int pattern)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.pattern == pattern)
                    return mapping.rotationDegrees;
            }
            return 0;
        }

        public void SetMapping(int pattern, string variant, int rotation = 0, string description = "")
        {
            for (int i = 0; i < mappings.Count; i++)
            {
                if (mappings[i].pattern == pattern)
                {
                    mappings[i].prefabVariant = variant;
                    mappings[i].rotationDegrees = rotation;
                    mappings[i].description = description;
                    return;
                }
            }
            
            mappings.Add(new PatternPrefabMapping
            {
                pattern = pattern,
                prefabVariant = variant,
                rotationDegrees = rotation,
                description = description
            });
        }
        
        public void GenerateCompleteMappings()
        {
            mappings.Clear();
            
            // Pattern format: 6-bit bitmask
            // Bit 0 = East, 1 = NE, 2 = NW, 3 = West, 4 = SW, 5 = SE
            // Example: pattern 33 (binary 100001) = connections at E(0) and SE(5)
            
            // Based on actual prefab analysis:
            // A = E-W straight line
            // B = curve SW-E
            // C = curve SE-E  
            // D = 3-way NW-SW-E
            // E = E-W straight + SW curve (3-way)
            // F = E-W straight + NW curve (3-way)
            // G = 3-way NE-SE-E
            // H = E-W straight + NE curve + SE curve (4-way)
            // I = 4-way NE-SE-SW-NW (no E-W)
            // J = 4-way E-W straight + NE curve + NW curve
            // K = 5-way NW-SW-SE-NE-E (all except W)
            // L = 6-way (all directions)
            // M = endcap on east side (single connection E)
            
            // 0 connections - use M (endcap as fallback)
            SetMapping(0, "M", 0, "No connections");
            
            // 1 connection - endcap M (pointing east)
            SetMapping(1, "M", 0, "E only");           // bit 0
            SetMapping(2, "M", 0, "NE only");          // bit 1
            SetMapping(4, "M", 0, "NW only");          // bit 2
            SetMapping(8, "M", 0, "W only");           // bit 3
            SetMapping(16, "M", 0, "SW only");         // bit 4
            SetMapping(32, "M", 0, "SE only");         // bit 5
            
            // 2 connections - straights or curves
            // STRAIGHT ROADS: Use variant A with rotation
            SetMapping(9, "A", 0, "E + W straight (0°)");      // bits 0,3
            SetMapping(18, "A", 60, "NE + SW straight (60°)"); // bits 1,4
            SetMapping(36, "A", 120, "NW + SE straight (120°)"); // bits 2,5
            
            // Curves with E
            SetMapping(17, "B", 0, "E + SW curve");     // bits 0,4 = SW-E curve (B)
            SetMapping(33, "C", 0, "E + SE curve");     // bits 0,5 = SE-E curve (C)
            SetMapping(3, "M", 0, "E + NE");            // bits 0,1 - use endcap fallback
            
            // Other 2-connection patterns - use closest match
            SetMapping(6, "M", 0, "NE + NW");           // bits 1,2
            SetMapping(12, "M", 0, "NW + W");           // bits 2,3
            SetMapping(24, "M", 0, "W + SW");           // bits 3,4
            SetMapping(48, "M", 0, "SW + SE");          // bits 4,5
            SetMapping(10, "M", 0, "NE + W");           // bits 1,3
            SetMapping(20, "M", 0, "NW + SW");          // bits 2,4
            SetMapping(40, "M", 0, "W + SE");           // bits 3,5
            SetMapping(34, "M", 0, "NE + SE");          // bits 1,5
            SetMapping(5, "M", 0, "E + NW");            // bits 0,2
            
            // 3 connections - T-junctions
            SetMapping(25, "D", 0, "E + SW + NW (D)");   // bits 0,4,2 = NW-SW-E (D)
            SetMapping(21, "F", 0, "E + W + NW");        // bits 0,3,2 = E-W + NW curve (F)
            SetMapping(11, "F", 0, "E + NE + W");        // bits 0,1,3 = E-W + NE (F variant)
            SetMapping(35, "G", 0, "E + NE + SE");       // bits 0,1,5 = NE-SE-E (G)
            SetMapping(25, "E", 0, "E + W + SW");        // bits 0,3,4 = E-W + SW curve (E)
            
            // Fill remaining 3-connection patterns
            SetMapping(7, "G", 0, "E + NE + NW");        // bits 0,1,2
            SetMapping(14, "F", 0, "NE + NW + W");       // bits 1,2,3
            SetMapping(28, "E", 0, "NW + W + SW");       // bits 2,3,4
            SetMapping(56, "E", 0, "W + SW + SE");       // bits 3,4,5
            SetMapping(49, "D", 0, "E + SW + SE");       // bits 0,4,5
            SetMapping(19, "G", 0, "E + NE + SW");       // bits 0,1,4
            SetMapping(13, "F", 0, "E + NW + W");        // bits 0,2,3
            SetMapping(22, "F", 0, "NE + NW + SW");      // bits 1,2,4
            SetMapping(26, "E", 0, "NE + W + SW");       // bits 1,3,4
            SetMapping(44, "E", 0, "NW + W + SE");       // bits 2,3,5
            SetMapping(52, "D", 0, "NW + SW + SE");      // bits 2,4,5
            SetMapping(50, "D", 0, "NE + SW + SE");      // bits 1,4,5
            SetMapping(38, "G", 0, "NE + NW + SE");      // bits 1,2,5
            SetMapping(41, "G", 0, "E + W + SE");        // bits 0,3,5
            SetMapping(37, "G", 0, "E + NW + SE");       // bits 0,2,5
            
            // 4 connections
            SetMapping(27, "H", 0, "E + W + NE + SW");   // bits 0,3,1,4
            SetMapping(45, "H", 0, "E + W + NW + SE");   // bits 0,3,2,5 = E-W + NE + SE (H)
            SetMapping(54, "I", 0, "NE + NW + SW + SE"); // bits 1,2,4,5 = no E-W (I)
            SetMapping(23, "J", 0, "E + W + NE + NW");   // bits 0,3,1,2 = E-W + NE + NW (J)
            
            // Fill remaining 4-connection patterns
            SetMapping(15, "J", 0, "E + NE + NW + W");
            SetMapping(30, "J", 0, "NE + NW + W + SW");
            SetMapping(60, "H", 0, "NW + W + SW + SE");
            SetMapping(57, "H", 0, "E + W + SW + SE");
            SetMapping(51, "H", 0, "E + NE + SW + SE");
            SetMapping(39, "J", 0, "E + NE + NW + SE");
            SetMapping(46, "J", 0, "NE + NW + W + SE");
            SetMapping(58, "H", 0, "NE + W + SW + SE");
            SetMapping(53, "I", 0, "E + NW + SW + SE");
            SetMapping(43, "J", 0, "E + NE + W + SE");
            SetMapping(29, "H", 0, "E + NW + W + SW");
            
            // 5 connections - use K (5-way NW-SW-SE-NE-E, missing W)
            SetMapping(31, "K", 0, "All except SE");     // bits 0,1,2,3,4 = missing SE, use K
            SetMapping(62, "K", 0, "All except E");      // bits 1,2,3,4,5 = missing E (not K pattern but close)
            SetMapping(61, "K", 0, "All except NE");
            SetMapping(59, "K", 0, "All except NW");
            SetMapping(55, "K", 0, "All except W");      // bits 0,1,2,4,5 = NW-SW-SE-NE-E (K!)
            SetMapping(47, "K", 0, "All except SW");
            
            // 6 connections - full L
            SetMapping(63, "L", 0, "All directions");
            
            Debug.Log($"Generated {mappings.Count} road pattern mappings based on actual prefab analysis");
        }
    }
}
