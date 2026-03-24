using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RogueDeal.HexLevels
{
    [CreateAssetMenu(fileName = "AssetCatalog", menuName = "Hex Editor/Asset Catalog")]
    public class AssetCatalog : ScriptableObject
    {
        [System.Serializable]
        public class AssetEntry
        {
            public string id;
            public GameObject prefab;
            public Texture2D thumbnail;
            public string displayName;
            public AssetCategory category;
            public string[] tags;
            public bool isFavorite;
            public bool isMultiHex;
            public int footprintSize = 1;
            public bool canRotate = true;
        }
        
        public List<AssetEntry> entries = new List<AssetEntry>();
        
        public AssetEntry GetEntry(string id)
        {
            return entries.FirstOrDefault(e => e.id == id);
        }
        
        public AssetEntry GetEntry(GameObject prefab)
        {
            return entries.FirstOrDefault(e => e.prefab == prefab);
        }
        
        public List<AssetEntry> GetEntriesByCategory(AssetCategory category)
        {
            return entries.Where(e => e.category == category).ToList();
        }
        
        public List<AssetEntry> GetEntriesByTag(string tag)
        {
            return entries.Where(e => e.tags != null && e.tags.Contains(tag)).ToList();
        }
        
        public List<AssetEntry> GetFavorites()
        {
            return entries.Where(e => e.isFavorite).ToList();
        }
        
        public List<AssetEntry> Search(string query)
        {
            if (string.IsNullOrEmpty(query))
                return entries;
            
            query = query.ToLower();
            return entries.Where(e => 
                e.displayName.ToLower().Contains(query) ||
                e.id.ToLower().Contains(query) ||
                (e.tags != null && e.tags.Any(t => t.ToLower().Contains(query)))
            ).ToList();
        }
        
        public void ToggleFavorite(string id)
        {
            AssetEntry entry = GetEntry(id);
            if (entry != null)
            {
                entry.isFavorite = !entry.isFavorite;
            }
        }
        
        public List<string> GetAllTags()
        {
            HashSet<string> allTags = new HashSet<string>();
            foreach (var entry in entries)
            {
                if (entry.tags != null)
                {
                    foreach (var tag in entry.tags)
                    {
                        allTags.Add(tag);
                    }
                }
            }
            return allTags.ToList();
        }
        
        public void AutoPopulateFromPrefabBrowser()
        {
#if UNITY_EDITOR
            entries.Clear();
            
            var categories = RogueDeal.HexLevels.Editor.PrefabBrowser.GetCategories();
            foreach (var categoryName in categories)
            {
                var prefabs = RogueDeal.HexLevels.Editor.PrefabBrowser.GetPrefabsInCategory(categoryName);
                AssetCategory category = ParseCategory(categoryName);
                
                foreach (var prefab in prefabs)
                {
                    AssetEntry entry = new AssetEntry
                    {
                        id = System.Guid.NewGuid().ToString(),
                        prefab = prefab,
                        displayName = prefab.name,
                        category = category,
                        tags = new[] { categoryName.ToLower() },
                        isFavorite = false,
                        canRotate = true,
                        footprintSize = 1,
                        isMultiHex = false
                    };
                    
                    entries.Add(entry);
                }
            }
#endif
        }
        
        private AssetCategory ParseCategory(string categoryName)
        {
            string lower = categoryName.ToLower();
            
            if (lower.Contains("tile") || lower.Contains("grass") || lower.Contains("water") || 
                lower.Contains("road") || lower.Contains("river") || lower.Contains("coast"))
                return AssetCategory.Tiles;
            
            if (lower.Contains("building") || lower.Contains("house") || lower.Contains("tower"))
                return AssetCategory.Buildings;
            
            return AssetCategory.Decorations;
        }
    }
    
    public enum AssetCategory
    {
        All,
        Tiles,
        Buildings,
        Decorations
    }
}
