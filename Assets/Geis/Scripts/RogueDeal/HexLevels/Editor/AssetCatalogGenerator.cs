/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using UnityEngine;
using UnityEditor;
using System.IO;

namespace RogueDeal.HexLevels.Editor
{
    public static class AssetCatalogGenerator
    {
        [MenuItem("Funder Games/Geis/Rogue Deal/Hex Levels/Authoring/Generate Asset Catalog")]
        public static void GenerateAssetCatalog()
        {
            string catalogPath = "Assets/RogueDeal/Resources/Data/HexLevels";
            
            if (!Directory.Exists(catalogPath))
            {
                Directory.CreateDirectory(catalogPath);
            }
            
            string assetPath = $"{catalogPath}/AssetCatalog.asset";
            
            AssetCatalog existingCatalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(assetPath);
            
            if (existingCatalog != null)
            {
                bool regenerate = EditorUtility.DisplayDialog(
                    "Asset Catalog Exists",
                    "An Asset Catalog already exists. Regenerate it?\n\n" +
                    "This will replace the existing catalog and reset favorites.",
                    "Regenerate", "Cancel"
                );
                
                if (!regenerate)
                {
                    return;
                }
            }
            
            AssetCatalog catalog = ScriptableObject.CreateInstance<AssetCatalog>();
            catalog.AutoPopulateFromPrefabBrowser();
            
            if (existingCatalog != null)
            {
                EditorUtility.CopySerialized(catalog, existingCatalog);
                EditorUtility.SetDirty(existingCatalog);
                AssetDatabase.SaveAssets();
                Debug.Log($"Regenerated Asset Catalog at {assetPath} with {catalog.entries.Count} entries");
            }
            else
            {
                AssetDatabase.CreateAsset(catalog, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Created Asset Catalog at {assetPath} with {catalog.entries.Count} entries");
            }
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<AssetCatalog>(assetPath);
        }
        
        [MenuItem("Funder Games/Geis/Rogue Deal/Hex Levels/Authoring/Refresh Asset Catalog")]
        public static void RefreshAssetCatalog()
        {
            string catalogPath = "Assets/RogueDeal/Resources/Data/HexLevels/AssetCatalog.asset";
            AssetCatalog catalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(catalogPath);
            
            if (catalog == null)
            {
                EditorUtility.DisplayDialog(
                    "No Catalog Found",
                    "No Asset Catalog found. Use 'Generate Asset Catalog' first.",
                    "OK"
                );
                return;
            }
            
            int oldCount = catalog.entries.Count;
            catalog.AutoPopulateFromPrefabBrowser();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Refreshed Asset Catalog: {oldCount} → {catalog.entries.Count} entries");
            
            EditorUtility.DisplayDialog(
                "Catalog Refreshed",
                $"Asset Catalog updated:\n\n" +
                $"Previous: {oldCount} entries\n" +
                $"Current: {catalog.entries.Count} entries",
                "OK"
            );
        }
        
        [MenuItem("Funder Games/Geis/Rogue Deal/Hex Levels/Authoring/Open Asset Catalog")]
        public static void OpenAssetCatalog()
        {
            string catalogPath = "Assets/RogueDeal/Resources/Data/HexLevels/AssetCatalog.asset";
            AssetCatalog catalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(catalogPath);
            
            if (catalog != null)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = catalog;
            }
            else
            {
                bool create = EditorUtility.DisplayDialog(
                    "No Catalog Found",
                    "No Asset Catalog found. Create one now?",
                    "Create", "Cancel"
                );
                
                if (create)
                {
                    GenerateAssetCatalog();
                }
            }
        }
    }
}
