using UnityEngine;
using UnityEditor;

namespace RogueDeal.HexLevels.Editor
{
    public class GenerateRoadDatabase : EditorWindow
    {
        [MenuItem("Tools/Hex Levels/Generate Road Pattern Database")]
        static void Generate()
        {
            string path = "Assets/RogueDeal/Resources/Data/HexLevels/RoadPatternDatabase.asset";
            
            RoadPatternDatabase database = AssetDatabase.LoadAssetAtPath<RoadPatternDatabase>(path);
            
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<RoadPatternDatabase>();
                AssetDatabase.CreateAsset(database, path);
                Debug.Log($"Created new RoadPatternDatabase at {path}");
            }
            
            database.GenerateCompleteMappings();
            
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", 
                $"Generated road pattern database with {database.mappings.Count} mappings!\n\n" +
                "This maps all 64 connection patterns to specific prefab variants (A-M) without rotation.",
                "OK");
            
            Selection.activeObject = database;
        }
    }
}
