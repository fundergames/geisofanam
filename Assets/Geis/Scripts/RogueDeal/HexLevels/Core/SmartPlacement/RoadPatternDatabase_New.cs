using UnityEngine;
using System.Collections.Generic;

namespace RogueDeal.HexLevels
{
    /// <summary>
    /// Defines a prefab variant's base connection pattern.
    /// Can be rotated to match different orientations.
    /// </summary>
    [System.Serializable]
    public class PrefabPrototype
    {
        public string variantName;
        public int basePattern;
        public string description;
    }
    
    [CreateAssetMenu(fileName = "RoadPatternDatabase", menuName = "Hex Levels/Road Pattern Database v2")]
    public class RoadPatternDatabase_New : ScriptableObject
    {
        [Header("Prefab Prototypes (Base Patterns)")]
        [Tooltip("Each prefab variant's base connection pattern. These can be rotated to match any orientation.")]
        public List<PrefabPrototype> prototypes = new List<PrefabPrototype>();
        
        /// <summary>
        /// Find which prefab variant and rotation matches the given connection pattern.
        /// </summary>
        public bool FindMatch(int pattern, out string variant, out int rotationDegrees)
        {
            // Try each prototype
            foreach (var prototype in prototypes)
            {
                // Try each rotation (0° to 300° in 60° increments)
                for (int rot = 0; rot < 6; rot++)
                {
                    int rotatedPattern = RotatePattern(prototype.basePattern, rot);
                    if (rotatedPattern == pattern)
                    {
                        variant = prototype.variantName;
                        rotationDegrees = rot * 60;
                        UnityEngine.Debug.Log($"[RoadPatternDatabase] Pattern {pattern} (0b{System.Convert.ToString(pattern, 2).PadLeft(6, '0')}) matched {variant} at {rotationDegrees}°");
                        return true;
                    }
                }
            }
            
            variant = null;
            rotationDegrees = 0;
            return false;
        }
        
        /// <summary>
        /// Find which prefab variant matches the given pattern at a SPECIFIC rotation.
        /// This is used when the user has specified a desired rotation via arrow keys.
        /// </summary>
        public bool FindMatchAtRotation(int pattern, int desiredRotation, out string variant)
        {
            // Try each prototype
            foreach (var prototype in prototypes)
            {
                // Check if this prototype at the desired rotation matches the pattern
                int rotatedPattern = RotatePattern(prototype.basePattern, desiredRotation);
                if (rotatedPattern == pattern)
                {
                    variant = prototype.variantName;
                    UnityEngine.Debug.Log($"[RoadPatternDatabase] Pattern {pattern} (0b{System.Convert.ToString(pattern, 2).PadLeft(6, '0')}) matched {variant} at user rotation {desiredRotation * 60}°");
                    return true;
                }
            }
            
            variant = null;
            return false;
        }
        
        /// <summary>
        /// Check if the given variant at the given rotation can support the required connections.
        /// Returns true if the variant's pattern includes all bits from requiredPattern (superset check).
        /// </summary>
        public bool CanSupportPattern(string variantName, int rotationIndex, int requiredPattern)
        {
            var prototype = prototypes.Find(p => p.variantName == variantName);
            if (prototype == null)
            {
                UnityEngine.Debug.Log($"[CanSupportPattern] Variant {variantName} not found");
                return false;
            }
            
            int rotatedPattern = RotatePattern(prototype.basePattern, rotationIndex);
            
            // Check if rotatedPattern is a superset of requiredPattern
            // (all required connection bits are present in the variant's pattern)
            bool canSupport = (rotatedPattern & requiredPattern) == requiredPattern;
            
            UnityEngine.Debug.Log($"[CanSupportPattern] Variant {variantName} (base {prototype.basePattern}) at rotation {rotationIndex} ({rotationIndex*60}°) = pattern {rotatedPattern} (0b{System.Convert.ToString(rotatedPattern, 2).PadLeft(6, '0')}), required {requiredPattern} (0b{System.Convert.ToString(requiredPattern, 2).PadLeft(6, '0')}) → {canSupport}");
            
            return canSupport;
        }
        
        /// <summary>
        /// Rotate a 6-bit connection pattern by specified number of 60° steps.
        /// Bit 0=E, 1=NE, 2=NW, 3=W, 4=SW, 5=SE
        /// Rotates clockwise: E→SE→SW→W→NW→NE→E
        /// </summary>
        private int RotatePattern(int pattern, int steps)
        {
            steps = steps % 6;
            if (steps == 0) return pattern;
            
            int result = 0;
            for (int i = 0; i < 6; i++)
            {
                if ((pattern & (1 << i)) != 0)
                {
                    // Rotate clockwise: subtract steps
                    int newPos = (i - steps + 6) % 6;
                    result |= (1 << newPos);
                }
            }
            return result;
        }
        
        public void GeneratePrototypes()
        {
            prototypes.Clear();
            
            // Based on your analysis:
            // Directions: 0=E, 1=NE, 2=NW, 3=W, 4=SW, 5=SE
            
            prototypes.Add(new PrefabPrototype
            {
                variantName = "A",
                basePattern = 9,  // 0b001001 = E + W straight
                description = "Straight road (E-W)"
            });
            
            prototypes.Add(new PrefabPrototype
            {
                variantName = "B",
                basePattern = 17, // 0b010001 = E + SW curve
                description = "60° curve (SW-E)"
            });
            
            prototypes.Add(new PrefabPrototype
            {
                variantName = "C",
                basePattern = 33, // 0b100001 = E + SE curve
                description = "60° curve (SE-E)"
            });
            
            prototypes.Add(new PrefabPrototype
            {
                variantName = "D",
                basePattern = 21, // 0b010101 = E + NW + SW (3-way)
                description = "3-way NW-SW-E"
            });
            
            prototypes.Add(new PrefabPrototype
            {
                variantName = "E",
                basePattern = 25, // 0b011001 = E + W + SW (3-way T-junction)
                description = "3-way T-junction E-W-SW"
            });
            
            prototypes.Add(new PrefabPrototype
            {
                variantName = "F",
                basePattern = 13, // 0b001101 = E + W + NW (straight + curve)
                description = "E-W straight + NW curve"
            });
            
            prototypes.Add(new PrefabPrototype
            {
                variantName = "G",
                basePattern = 35, // 0b100011 = E + NE + SE (3-way)
                description = "3-way NE-SE-E"
            });
            
            prototypes.Add(new PrefabPrototype
            {
                variantName = "H",
                basePattern = 43, // 0b101011 = E + W + NE + SE (4-way with straight)
                description = "E-W straight + NE + SE curves"
            });
            
            prototypes.Add(new PrefabPrototype
            {
                variantName = "I",
                basePattern = 54, // 0b110110 = NE + NW + SW + SE (4-way no straight)
                description = "4-way NE-SE-SW-NW (no straight)"
            });
            
            prototypes.Add(new PrefabPrototype
            {
                variantName = "J",
                basePattern = 15, // 0b001111 = E + NE + NW + W (4-way with straight)
                description = "E-W straight + NE + NW curves"
            });
            
            prototypes.Add(new PrefabPrototype
            {
                variantName = "K",
                basePattern = 55, // 0b110111 = E + NE + NW + SW + SE (5-way, missing W)
                description = "5-way (all except W)"
            });
            
            prototypes.Add(new PrefabPrototype
            {
                variantName = "L",
                basePattern = 63, // 0b111111 = all 6 directions
                description = "6-way intersection"
            });
            
            prototypes.Add(new PrefabPrototype
            {
                variantName = "M",
                basePattern = 1,  // 0b000001 = E only (endcap)
                description = "Endcap (E)"
            });
            
            Debug.Log($"Generated {prototypes.Count} road prefab prototypes");
        }
    }
}
