using UnityEngine;
using RogueDeal.HexLevels;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RogueDeal.HexLevels.Verification
{
    public class VerifyRoadPrefabPatterns : MonoBehaviour
    {
        [Header("Database")]
        public RoadPatternDatabase_New database;
        
        [Header("Prefab Directory")]
        public string prefabPath = "Prefabs/Hexes/Roads";
        
#if UNITY_EDITOR
        [MenuItem("Hex Tools/Debug/Print Database Patterns")]
        public static void PrintDatabasePatterns()
        {
            var database = Resources.Load<RoadPatternDatabase_New>("Data/HexLevels/RoadPatternDatabaseV2");
            if (database == null)
            {
                UnityEngine.Debug.LogError("Database not found at Resources/Data/HexLevels/RoadPatternDatabaseV2");
                return;
            }
            
            UnityEngine.Debug.Log("=== ROAD PATTERN DATABASE ===");
            UnityEngine.Debug.Log($"Total prototypes: {database.prototypes.Count}");
            UnityEngine.Debug.Log("Hex directions: 0=E, 1=NE, 2=NW, 3=W, 4=SW, 5=SE");
            UnityEngine.Debug.Log("");
            
            string[] dirs = { "E", "NE", "NW", "W", "SW", "SE" };
            
            foreach (var proto in database.prototypes)
            {
                string binaryStr = System.Convert.ToString(proto.basePattern, 2).PadLeft(6, '0');
                System.Collections.Generic.List<string> connections = new System.Collections.Generic.List<string>();
                
                for (int bit = 0; bit < 6; bit++)
                {
                    if ((proto.basePattern & (1 << bit)) != 0)
                    {
                        connections.Add($"{dirs[bit]}({bit})");
                    }
                }
                
                UnityEngine.Debug.Log($"Variant {proto.variantName}: Pattern {proto.basePattern} (0b{binaryStr}) = {string.Join(", ", connections)}");
            }
        }
        
        [MenuItem("Hex Tools/Debug/Test Pattern Matching")]
        public static void TestPatternMatching()
        {
            var database = Resources.Load<RoadPatternDatabase_New>("Data/HexLevels/RoadPatternDatabaseV2");
            if (database == null)
            {
                UnityEngine.Debug.LogError("Database not found!");
                return;
            }
            
            UnityEngine.Debug.Log("=== TESTING PATTERN MATCHING ===");
            
            int[] testPatterns = { 9, 19, 38, 25 };
            string[] descriptions = { 
                "E-W (bits 0+3)", 
                "E+NE+SW (bits 0+1+4)", 
                "NE+NW+SE (bits 1+2+5)",
                "E+W+SW (bits 0+3+4)"
            };
            
            for (int i = 0; i < testPatterns.Length; i++)
            {
                int pattern = testPatterns[i];
                string variant;
                int rotation;
                
                if (database.FindMatch(pattern, out variant, out rotation))
                {
                    UnityEngine.Debug.Log($"Pattern {pattern} ({descriptions[i]}) -> Variant {variant} at {rotation*60}°");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Pattern {pattern} ({descriptions[i]}) -> NO MATCH FOUND!");
                }
            }
        }
#endif
        
        [ContextMenu("Print All Patterns")]
        public void PrintAllPatterns()
        {
            if (database == null)
            {
                UnityEngine.Debug.LogError("Database not assigned!");
                return;
            }
            
            UnityEngine.Debug.Log("=== ROAD PATTERN DATABASE ===");
            UnityEngine.Debug.Log("Hex directions: 0=E, 1=NE, 2=NW, 3=W, 4=SW, 5=SE");
            UnityEngine.Debug.Log("");
            
            for (int i = 0; i < database.prototypes.Count; i++)
            {
                var proto = database.prototypes[i];
                string binaryStr = System.Convert.ToString(proto.basePattern, 2).PadLeft(6, '0');
                
                string[] dirs = { "E", "NE", "NW", "W", "SW", "SE" };
                System.Collections.Generic.List<string> connections = new System.Collections.Generic.List<string>();
                
                for (int bit = 0; bit < 6; bit++)
                {
                    if ((proto.basePattern & (1 << bit)) != 0)
                    {
                        connections.Add($"{dirs[bit]}({bit})");
                    }
                }
                
                UnityEngine.Debug.Log($"Variant {proto.variantName}: Pattern {proto.basePattern} (0b{binaryStr})");
                UnityEngine.Debug.Log($"  Description: {proto.description}");
                UnityEngine.Debug.Log($"  Connections: {string.Join(", ", connections)}");
                UnityEngine.Debug.Log("");
            }
            
            UnityEngine.Debug.Log("=== ROTATION EXAMPLES ===");
            UnityEngine.Debug.Log("User rotation 0 (0°) should produce E-W connections (bits 0+3)");
            UnityEngine.Debug.Log("User rotation 1 (60°) should produce NE-SW connections (bits 1+4)");
            UnityEngine.Debug.Log("User rotation 2 (120°) should produce NW-SE connections (bits 2+5)");
        }
        
        [ContextMenu("Test Pattern Context Menu")]
        public void TestPatternContextMenu()
        {
            if (database == null)
            {
                UnityEngine.Debug.LogError("Database not assigned!");
                return;
            }
            
            UnityEngine.Debug.Log("=== TESTING PATTERN MATCHING ===");
            
            int[] testPatterns = { 9, 19, 38 };
            string[] descriptions = { 
                "E-W (bits 0+3)", 
                "E+NE+SW (bits 0+1+4)", 
                "NE+NW+SE (bits 1+2+5)" 
            };
            
            for (int i = 0; i < testPatterns.Length; i++)
            {
                int pattern = testPatterns[i];
                string variant;
                int rotation;
                
                if (database.FindMatch(pattern, out variant, out rotation))
                {
                    UnityEngine.Debug.Log($"Pattern {pattern} ({descriptions[i]}) -> Variant {variant} at {rotation*60}°");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Pattern {pattern} ({descriptions[i]}) -> NO MATCH FOUND!");
                }
            }
        }
    }
}
