using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace RogueDeal.HexLevels.Editor
{
    /// <summary>
    /// Utility class to discover and browse KayKit prefabs.
    /// </summary>
    public static class PrefabBrowser
    {
        private static Dictionary<string, List<GameObject>> cachedPrefabs;
        private static string prefabBasePath = "Assets/KayKit/Packs/KayKit - Medieval Hexagon Pack (for Unity)/Prefabs";

        /// <summary>
        /// Get all prefabs organized by category.
        /// </summary>
        public static Dictionary<string, List<GameObject>> GetPrefabsByCategory()
        {
            if (cachedPrefabs != null)
                return cachedPrefabs;

            cachedPrefabs = new Dictionary<string, List<GameObject>>();
            
            // Search for all prefabs in KayKit directory
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabBasePath });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab == null)
                    continue;

                // Determine category from path
                string category = GetCategoryFromPath(path);
                
                if (!cachedPrefabs.ContainsKey(category))
                {
                    cachedPrefabs[category] = new List<GameObject>();
                }
                
                cachedPrefabs[category].Add(prefab);
            }

            // Sort each category alphabetically
            foreach (var category in cachedPrefabs.Keys.ToList())
            {
                cachedPrefabs[category] = cachedPrefabs[category]
                    .OrderBy(p => p.name)
                    .ToList();
            }

            return cachedPrefabs;
        }

        /// <summary>
        /// Get prefabs in a specific category.
        /// </summary>
        public static List<GameObject> GetPrefabsInCategory(string category)
        {
            var allPrefabs = GetPrefabsByCategory();
            if (allPrefabs.ContainsKey(category))
            {
                return allPrefabs[category];
            }
            return new List<GameObject>();
        }

        /// <summary>
        /// Get all available categories.
        /// </summary>
        public static List<string> GetCategories()
        {
            var allPrefabs = GetPrefabsByCategory();
            return allPrefabs.Keys.OrderBy(c => c).ToList();
        }

        /// <summary>
        /// Get the category that contains the given prefab, or null if not found.
        /// </summary>
        public static string GetCategoryForPrefab(GameObject prefab)
        {
            if (prefab == null) return null;
            var allPrefabs = GetPrefabsByCategory();
            foreach (var kvp in allPrefabs)
            {
                if (kvp.Value.Contains(prefab))
                    return kvp.Key;
            }
            return null;
        }

        /// <summary>
        /// Clear the prefab cache (useful if assets are added/removed).
        /// </summary>
        public static void ClearCache()
        {
            cachedPrefabs = null;
        }

        private static string GetCategoryFromPath(string path)
        {
            // Extract category from path structure:
            // Assets/KayKit/.../Prefabs/tiles/base/hex_grass.prefab -> tiles/base
            // Assets/KayKit/.../Prefabs/buildings/blue/building_blue.prefab -> buildings/blue
            
            string relativePath = path.Replace(prefabBasePath, "").TrimStart('/', '\\');
            
            // Remove filename
            int lastSlash = relativePath.LastIndexOf('/');
            if (lastSlash > 0)
            {
                relativePath = relativePath.Substring(0, lastSlash);
            }

            // Handle nested categories
            if (relativePath.Contains("/"))
            {
                // Use full path as category (e.g., "tiles/base", "buildings/blue")
                return relativePath.Replace("/", " / ");
            }

            return relativePath.Length > 0 ? relativePath : "root";
        }

        /// <summary>
        /// Filter prefabs by name (case-insensitive search).
        /// </summary>
        public static List<GameObject> FilterPrefabs(string searchTerm)
        {
            var allPrefabs = GetPrefabsByCategory();
            List<GameObject> results = new List<GameObject>();
            
            string lowerSearch = searchTerm.ToLower();
            
            foreach (var category in allPrefabs.Values)
            {
                foreach (var prefab in category)
                {
                    if (prefab.name.ToLower().Contains(lowerSearch))
                    {
                        results.Add(prefab);
                    }
                }
            }
            
            return results;
        }
    }
}