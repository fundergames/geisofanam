using UnityEngine;
using UnityEditor;
using RogueDeal.HexLevels;

namespace RogueDeal.HexLevels.Editor
{
    /// <summary>
    /// Editor utilities for SmartTileSelector.
    /// Provides prefab lookup functionality using AssetDatabase.
    /// </summary>
    [InitializeOnLoad]
    public static class SmartTileSelectorEditor
    {
        private const string DEFAULT_MAPPINGS_PATH = "Assets/RogueDeal/Resources/Data/HexLevels/ConnectionPatternMappings.asset";
        private const string ROAD_DATABASE_PATH = "Assets/RogueDeal/Resources/Data/HexLevels/RoadPatternDatabase.asset";
        private const string ROAD_DATABASE_V2_PATH = "Assets/RogueDeal/Resources/Data/HexLevels/RoadPatternDatabaseV2.asset";
        
        static SmartTileSelectorEditor()
        {
            // Register prefab lookup function on editor load
            SmartTileSelector.SetPrefabLookup(FindPrefabByName);
            
            // Load mappings asset (old system)
            LoadMappingsAsset();
            
            // Load road pattern database V2 (NEW prototype-based system)
            LoadRoadDatabaseV2();
            
            // Load road pattern database (old mapping system - fallback)
            LoadRoadDatabase();
        }
        
        private static void LoadMappingsAsset()
        {
            ConnectionPatternMappings mappings = AssetDatabase.LoadAssetAtPath<ConnectionPatternMappings>(DEFAULT_MAPPINGS_PATH);
            if (mappings != null)
            {
                SmartTileSelector.SetMappingsAsset(mappings);
            }
        }
        
        private static void LoadRoadDatabase()
        {
            RoadPatternDatabase database = AssetDatabase.LoadAssetAtPath<RoadPatternDatabase>(ROAD_DATABASE_PATH);
            if (database != null)
            {
                SmartTileSelector.SetRoadDatabase(database);
                Debug.Log($"[SmartTileSelectorEditor] Loaded old road database with {database.mappings.Count} mappings");
            }
        }
        
        private static void LoadRoadDatabaseV2()
        {
            RoadPatternDatabase_New database = AssetDatabase.LoadAssetAtPath<RoadPatternDatabase_New>(ROAD_DATABASE_V2_PATH);
            if (database != null)
            {
                SmartTileSelector.SetRoadDatabaseV2(database);
                Debug.Log($"[SmartTileSelectorEditor] Loaded road pattern database V2 with {database.prototypes.Count} prototypes");
            }
            else
            {
                Debug.LogWarning($"[SmartTileSelectorEditor] Road pattern database V2 not found at {ROAD_DATABASE_V2_PATH}. Run Tools > Hex Levels > Generate Road Pattern Database V2");
            }
        }

        /// <summary>
        /// Find a prefab by name pattern using AssetDatabase.
        /// </summary>
        private static GameObject FindPrefabByName(string searchPattern)
        {
            string[] guids = AssetDatabase.FindAssets($"{searchPattern} t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                return prefab;
            }
            return null;
        }
    }
}