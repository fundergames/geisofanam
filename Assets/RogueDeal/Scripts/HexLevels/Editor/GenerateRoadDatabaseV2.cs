using UnityEngine;
using UnityEditor;

namespace RogueDeal.HexLevels.Editor
{
    public static class GenerateRoadDatabaseV2
    {
        private const string DATABASE_PATH = "Assets/RogueDeal/Resources/Data/HexLevels/RoadPatternDatabaseV2.asset";
        
        [MenuItem("Tools/Hex Levels/Generate Road Pattern Database V2 (Prototype-Based)")]
        public static void GenerateDatabase()
        {
            RoadPatternDatabase_New database = AssetDatabase.LoadAssetAtPath<RoadPatternDatabase_New>(DATABASE_PATH);
            
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<RoadPatternDatabase_New>();
                
                string directory = System.IO.Path.GetDirectoryName(DATABASE_PATH);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                
                AssetDatabase.CreateAsset(database, DATABASE_PATH);
                Debug.Log($"Created new RoadPatternDatabase V2 at {DATABASE_PATH}");
            }
            
            database.GeneratePrototypes();
            
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"✓ Generated Road Pattern Database V2 with {database.prototypes.Count} prototypes at {DATABASE_PATH}");
            
            EditorGUIUtility.PingObject(database);
            Selection.activeObject = database;
        }
    }
}
