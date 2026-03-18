using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using RogueDeal.Player;

namespace RogueDeal.Editor
{
    public class ComprehensiveDataMigrationTool : EditorWindow
    {
        private const string OLD_HERO_PATH = "Assets/TEMP_REMOVE_WHEN_CONVERTED/FunderGames/RPG/Data/Heros";
        private const string OLD_CLASS_PATH = "Assets/TEMP_REMOVE_WHEN_CONVERTED/FunderGames/RPG/Data/ClassData";
        private const string OLD_VISUAL_PATH = "Assets/TEMP_REMOVE_WHEN_CONVERTED/FunderGames/RPG/Data/Heros/VisualData";
        private const string OLD_ANIMATOR_PATH = "Assets/TEMP_REMOVE_WHEN_CONVERTED/FunderGames/RPG/Data/AnimatorData";
        private const string OLD_STATS_PATH = "Assets/TEMP_REMOVE_WHEN_CONVERTED/FunderGames/RPG/Data/ClassData/BaseStatData";
        private const string OLD_STAT_DATA_PATH = "Assets/TEMP_REMOVE_WHEN_CONVERTED/FunderGames/RPG/Data/StatData";
        
        private const string NEW_HERO_PATH = "Assets/RogueDeal/Resources/Data/Heroes";
        private const string NEW_CLASS_PATH = "Assets/RogueDeal/Resources/Data/Classes";
        private const string NEW_VISUAL_PATH = "Assets/RogueDeal/Resources/Data/HeroVisuals";
        private const string NEW_ANIMATOR_PATH = "Assets/RogueDeal/Resources/Data/Animators";
        private const string NEW_STATS_PATH = "Assets/RogueDeal/Resources/Data/Stats";
        private const string NEW_STAT_DATA_PATH = "Assets/RogueDeal/Resources/Data/StatData";
        
        private Dictionary<Object, Object> migrationMap = new Dictionary<Object, Object>();
        
        [MenuItem("RogueDeal/Tools/Comprehensive Data Migration")]
        public static void ShowWindow()
        {
            GetWindow<ComprehensiveDataMigrationTool>("Data Migration");
        }

        private void OnGUI()
        {
            GUILayout.Label("Comprehensive Data Migration Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This migrates ALL hero-related data:\n" +
                "• Character Classes\n" +
                "• Hero Visual Data\n" +
                "• Animator Data\n" +
                "• Stats Data\n" +
                "• Hero Data (with all references wired up)\n\n" +
                "New assets → /Assets/RogueDeal/Resources/Data/",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("🚀 Migrate Everything", GUILayout.Height(50)))
            {
                MigrateEverything();
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "If you already ran the migration and the data is incomplete, click below to delete all migrated assets and try again.",
                MessageType.Warning
            );

            if (GUILayout.Button("🗑️ Delete All Migrated Assets", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(
                    "Confirm Deletion",
                    "This will DELETE all migrated assets in:\n" +
                    $"• {NEW_HERO_PATH}\n" +
                    $"• {NEW_CLASS_PATH}\n" +
                    $"• {NEW_VISUAL_PATH}\n" +
                    $"• {NEW_ANIMATOR_PATH}\n" +
                    $"• {NEW_STATS_PATH}\n\n" +
                    "Are you sure?",
                    "Yes, Delete",
                    "Cancel"))
                {
                    DeleteAllMigratedAssets();
                }
            }
        }

        private void MigrateEverything()
        {
            migrationMap.Clear();
            
            Debug.Log("=== STARTING COMPREHENSIVE MIGRATION ===");
            
            MigrateCharacterClasses();
            MigrateHeroVisualData();
            MigrateAnimatorData();
            MigrateIndividualStatData();
            MigrateStatsData();
            MigrateHeroes();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("=== MIGRATION COMPLETE ===");
            
            EditorUtility.DisplayDialog(
                "Migration Complete!",
                $"Successfully migrated all data!\n\n" +
                $"Migrated items:\n" +
                $"• {migrationMap.Count} total assets\n\n" +
                $"Check Console for details.",
                "Awesome!"
            );
        }

        private void MigrateCharacterClasses()
        {
            EnsureDirectoryExists(NEW_CLASS_PATH);
            
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { OLD_CLASS_PATH });
            int count = 0;

            foreach (string guid in guids)
            {
                string oldPath = AssetDatabase.GUIDToAssetPath(guid);
                
                if (!oldPath.EndsWith("ClassData.asset") || oldPath.Contains("BaseStatData"))
                    continue;

                var oldAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(oldPath);
                
                if (oldAsset == null || oldAsset.GetType().Name != "CharacterClassData")
                    continue;

                string name = Path.GetFileNameWithoutExtension(oldPath);
                string newPath = Path.Combine(NEW_CLASS_PATH, name + ".asset");
                
                if (File.Exists(newPath))
                {
                    migrationMap[oldAsset] = AssetDatabase.LoadAssetAtPath<CharacterClassData>(newPath);
                    continue;
                }

                CharacterClassData newAsset = ScriptableObject.CreateInstance<CharacterClassData>();
                
                CopyField(oldAsset, newAsset, "classDisplayName");
                CopyField(oldAsset, newAsset, "description");
                CopyField(oldAsset, newAsset, "icon");

                AssetDatabase.CreateAsset(newAsset, newPath);
                migrationMap[oldAsset] = newAsset;
                count++;

                Debug.Log($"✓ Character Class: {name}");
            }
            
            Debug.Log($"[Classes] Migrated {count} character class(es)");
        }

        private void MigrateHeroVisualData()
        {
            EnsureDirectoryExists(NEW_VISUAL_PATH);
            
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { OLD_VISUAL_PATH });
            int count = 0;

            foreach (string guid in guids)
            {
                string oldPath = AssetDatabase.GUIDToAssetPath(guid);
                var oldAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(oldPath);
                
                if (oldAsset == null || oldAsset.GetType().Name != "HeroVisualData")
                    continue;

                string name = Path.GetFileNameWithoutExtension(oldPath);
                string newPath = Path.Combine(NEW_VISUAL_PATH, name + ".asset");

                HeroVisualData newAsset = ScriptableObject.CreateInstance<HeroVisualData>();
                
                CopyField(oldAsset, newAsset, "icon");
                CopyField(oldAsset, newAsset, "fullImage");
                CopyField(oldAsset, newAsset, "characterPrefab");

                AssetDatabase.CreateAsset(newAsset, newPath);
                migrationMap[oldAsset] = newAsset;
                count++;

                Debug.Log($"✓ Visual Data: {name}");
            }
            
            Debug.Log($"[Visuals] Migrated {count} visual data asset(s)");
        }

        private void MigrateAnimatorData()
        {
            EnsureDirectoryExists(NEW_ANIMATOR_PATH);
            
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { OLD_ANIMATOR_PATH });
            int count = 0;

            foreach (string guid in guids)
            {
                string oldPath = AssetDatabase.GUIDToAssetPath(guid);
                var oldAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(oldPath);
                
                if (oldAsset == null || oldAsset.GetType().Name != "ClassAnimatorData")
                    continue;

                string name = Path.GetFileNameWithoutExtension(oldPath);
                string newPath = Path.Combine(NEW_ANIMATOR_PATH, name + ".asset");

                ClassAnimatorData newAsset = ScriptableObject.CreateInstance<ClassAnimatorData>();
                
                CopyField(oldAsset, newAsset, "battleAnimator");
                CopyField(oldAsset, newAsset, "characterSelectAnimator");
                CopyField(oldAsset, newAsset, "idleClip");
                CopyField(oldAsset, newAsset, "attack1Clip");
                CopyField(oldAsset, newAsset, "attack2Clip");
                CopyField(oldAsset, newAsset, "attack3Clip");
                CopyField(oldAsset, newAsset, "attack4Clip");
                CopyField(oldAsset, newAsset, "attack5Clip");
                CopyField(oldAsset, newAsset, "tauntAnimationClip");
                CopyField(oldAsset, newAsset, "battleIdleClip");
                CopyField(oldAsset, newAsset, "levelUpClip");
                CopyField(oldAsset, newAsset, "dieClip");
                CopyField(oldAsset, newAsset, "dizzyClip");
                CopyField(oldAsset, newAsset, "takeDamage1Clip");
                CopyField(oldAsset, newAsset, "takeDamage2Clip");
                CopyField(oldAsset, newAsset, "defendClip");
                CopyField(oldAsset, newAsset, "victoryClip");
                CopyField(oldAsset, newAsset, "sprintClip");
                CopyField(oldAsset, newAsset, "comboClip");

                AssetDatabase.CreateAsset(newAsset, newPath);
                EditorUtility.SetDirty(newAsset);
                migrationMap[oldAsset] = newAsset;
                count++;

                Debug.Log($"✓ Animator Data: {name}");
            }
            
            Debug.Log($"[Animators] Migrated {count} animator data asset(s)");
        }

        private void MigrateIndividualStatData()
        {
            EnsureDirectoryExists(NEW_STAT_DATA_PATH);
            
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { OLD_STAT_DATA_PATH });
            int count = 0;

            foreach (string guid in guids)
            {
                string oldPath = AssetDatabase.GUIDToAssetPath(guid);
                var oldAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(oldPath);
                
                if (oldAsset == null || oldAsset.GetType().Name != "StatData")
                    continue;

                string name = Path.GetFileNameWithoutExtension(oldPath);
                string newPath = Path.Combine(NEW_STAT_DATA_PATH, name + ".asset");

                StatData newAsset = ScriptableObject.CreateInstance<StatData>();
                
                CopyField(oldAsset, newAsset, "icon");
                CopyField(oldAsset, newAsset, "displayText");
                CopyField(oldAsset, newAsset, "amount");
                CopyField(oldAsset, newAsset, "color");
                
                var oldType = GetFieldValue<System.Enum>(oldAsset, "type");
                if (oldType != null)
                {
                    string enumName = oldType.ToString();
                    if (System.Enum.TryParse(typeof(StatType), enumName, out var newType))
                    {
                        SetFieldValue(newAsset, "type", newType);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not convert StatType '{enumName}' for {name}");
                    }
                }

                AssetDatabase.CreateAsset(newAsset, newPath);
                EditorUtility.SetDirty(newAsset);
                migrationMap[oldAsset] = newAsset;
                count++;

                Debug.Log($"✓ Stat Data: {name}");
            }
            
            Debug.Log($"[Individual Stats] Migrated {count} stat data asset(s)");
        }

        private void MigrateStatsData()
        {
            EnsureDirectoryExists(NEW_STATS_PATH);
            
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { OLD_STATS_PATH });
            int count = 0;

            foreach (string guid in guids)
            {
                string oldPath = AssetDatabase.GUIDToAssetPath(guid);
                var oldAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(oldPath);
                
                if (oldAsset == null || oldAsset.GetType().Name != "StatsData")
                    continue;

                string name = Path.GetFileNameWithoutExtension(oldPath);
                string newPath = Path.Combine(NEW_STATS_PATH, name + ".asset");

                StatsData newAsset = ScriptableObject.CreateInstance<StatsData>();

                var oldStatsList = GetFieldValue<System.Collections.IList>(oldAsset, "stats");
                if (oldStatsList != null)
                {
                    var newStatsList = new System.Collections.Generic.List<StatData>();
                    
                    foreach (var oldStat in oldStatsList)
                    {
                        if (oldStat != null && migrationMap.TryGetValue(oldStat as Object, out var newStat))
                        {
                            newStatsList.Add(newStat as StatData);
                        }
                    }
                    
                    SetFieldValue(newAsset, "stats", newStatsList);
                }

                AssetDatabase.CreateAsset(newAsset, newPath);
                EditorUtility.SetDirty(newAsset);
                migrationMap[oldAsset] = newAsset;
                count++;

                Debug.Log($"✓ Stats Data: {name} (with {GetFieldValue<System.Collections.IList>(newAsset, "stats")?.Count ?? 0} stats)");
            }
            
            Debug.Log($"[Stats Collections] Migrated {count} stats data asset(s)");
        }

        private void MigrateHeroes()
        {
            EnsureDirectoryExists(NEW_HERO_PATH);
            
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { OLD_HERO_PATH });
            int count = 0;

            foreach (string guid in guids)
            {
                string oldPath = AssetDatabase.GUIDToAssetPath(guid);
                
                if (oldPath.Contains("VisualData"))
                    continue;

                var oldHero = AssetDatabase.LoadAssetAtPath<ScriptableObject>(oldPath);
                
                if (oldHero == null || oldHero.GetType().Name != "HeroData")
                    continue;

                string name = Path.GetFileNameWithoutExtension(oldPath);
                string newPath = Path.Combine(NEW_HERO_PATH, name + ".asset");

                HeroData newHero = ScriptableObject.CreateInstance<HeroData>();
                
                CopyField(oldHero, newHero, "playerName");
                CopyField(oldHero, newHero, "level");
                CopyField(oldHero, newHero, "levelProgress");
                CopyField(oldHero, newHero, "power");

                var oldStatList = GetFieldValue<Object>(oldHero, "statList");
                var oldClass = GetFieldValue<Object>(oldHero, "characterClass");
                var oldVisual = GetFieldValue<Object>(oldHero, "heroVisualData");
                var oldAnimator = GetFieldValue<Object>(oldHero, "animatorData");

                if (oldStatList != null && migrationMap.TryGetValue(oldStatList, out var newStatList))
                    SetFieldValue(newHero, "statList", newStatList);
                
                if (oldClass != null && migrationMap.TryGetValue(oldClass, out var newClass))
                    SetFieldValue(newHero, "characterClass", newClass);
                
                if (oldVisual != null && migrationMap.TryGetValue(oldVisual, out var newVisual))
                    SetFieldValue(newHero, "heroVisualData", newVisual);
                
                if (oldAnimator != null && migrationMap.TryGetValue(oldAnimator, out var newAnimator))
                    SetFieldValue(newHero, "animatorData", newAnimator);

                AssetDatabase.CreateAsset(newHero, newPath);
                EditorUtility.SetDirty(newHero);
                count++;

                Debug.Log($"✓ Hero: {name}");
            }
            
            Debug.Log($"[Heroes] Migrated {count} hero(es) with all references!");
        }

        private void CopyField(Object source, Object target, string fieldName)
        {
            var sourceType = source.GetType();
            var targetType = target.GetType();
            
            var sourceField = GetField(sourceType, fieldName);
            var targetField = GetField(targetType, fieldName);

            if (sourceField != null && targetField != null)
            {
                var value = sourceField.GetValue(source);
                targetField.SetValue(target, value);
            }
        }

        private T GetFieldValue<T>(Object obj, string fieldName)
        {
            var field = GetField(obj.GetType(), fieldName);
            if (field != null)
            {
                var value = field.GetValue(obj);
                if (value is T typedValue)
                    return typedValue;
            }
            return default;
        }

        private void SetFieldValue(Object obj, string fieldName, object value)
        {
            var field = GetField(obj.GetType(), fieldName);
            field?.SetValue(obj, value);
        }

        private System.Reflection.FieldInfo GetField(System.Type type, string fieldName)
        {
            const System.Reflection.BindingFlags flags = 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance;
            
            return type.GetField(fieldName, flags);
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private void DeleteAllMigratedAssets()
        {
            int deletedCount = 0;

            string[] pathsToDelete = new[]
            {
                NEW_HERO_PATH,
                NEW_CLASS_PATH,
                NEW_VISUAL_PATH,
                NEW_ANIMATOR_PATH,
                NEW_STATS_PATH,
                NEW_STAT_DATA_PATH
            };

            foreach (string path in pathsToDelete)
            {
                if (Directory.Exists(path))
                {
                    string[] assets = Directory.GetFiles(path, "*.asset", SearchOption.AllDirectories);
                    foreach (string asset in assets)
                    {
                        AssetDatabase.DeleteAsset(asset);
                        deletedCount++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Cleanup] Deleted {deletedCount} migrated asset(s)");
            EditorUtility.DisplayDialog("Cleanup Complete", $"Deleted {deletedCount} migrated asset(s).\n\nYou can now run the migration again.", "OK");
        }
    }
}
