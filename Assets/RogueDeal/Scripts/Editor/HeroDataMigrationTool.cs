using UnityEngine;
using UnityEditor;
using System.IO;
using RogueDeal.Player;

namespace RogueDeal.Editor
{
    public class HeroDataMigrationTool : EditorWindow
    {
        private const string OLD_HERO_PATH = "Assets/TEMP_REMOVE_WHEN_CONVERTED/FunderGames/RPG/Data/Heros";
        private const string NEW_HERO_PATH = "Assets/RogueDeal/Resources/Data/Heroes";
        
        [MenuItem("RogueDeal/Tools/Migrate Hero Data")]
        public static void ShowWindow()
        {
            GetWindow<HeroDataMigrationTool>("Hero Data Migration");
        }

        private void OnGUI()
        {
            GUILayout.Label("Hero Data Migration Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will create new RogueDeal.Player.HeroData assets from the old FunderGames.RPG.HeroData assets.\n\n" +
                "The new assets will be created in: " + NEW_HERO_PATH,
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Migrate All Heroes", GUILayout.Height(30)))
            {
                MigrateAllHeroes();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Create Empty Hero Template", GUILayout.Height(30)))
            {
                CreateEmptyHero();
            }
        }

        private void MigrateAllHeroes()
        {
            if (!Directory.Exists(NEW_HERO_PATH))
            {
                Directory.CreateDirectory(NEW_HERO_PATH);
                AssetDatabase.Refresh();
            }

            string[] oldHeroGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { OLD_HERO_PATH });
            int migratedCount = 0;

            foreach (string guid in oldHeroGuids)
            {
                string oldPath = AssetDatabase.GUIDToAssetPath(guid);
                
                if (oldPath.Contains("VisualData"))
                    continue;

                var oldHero = AssetDatabase.LoadAssetAtPath<ScriptableObject>(oldPath);
                
                if (oldHero == null || oldHero.GetType().Name != "HeroData")
                    continue;

                string heroName = Path.GetFileNameWithoutExtension(oldPath);
                string newPath = Path.Combine(NEW_HERO_PATH, heroName + ".asset");

                HeroData newHero = ScriptableObject.CreateInstance<HeroData>();
                
                CopyHeroDataUsingReflection(oldHero, newHero);

                AssetDatabase.CreateAsset(newHero, newPath);
                migratedCount++;

                Debug.Log($"Migrated: {heroName} -> {newPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Migration Complete",
                $"Successfully migrated {migratedCount} hero(es).\n\nNew assets created in:\n{NEW_HERO_PATH}",
                "OK"
            );
        }

        private void CopyHeroDataUsingReflection(ScriptableObject oldHero, HeroData newHero)
        {
            var oldType = oldHero.GetType();
            var newType = newHero.GetType();

            CopyField(oldType, newType, oldHero, newHero, "playerName");
            CopyField(oldType, newType, oldHero, newHero, "level");
            CopyField(oldType, newType, oldHero, newHero, "levelProgress");
            CopyField(oldType, newType, oldHero, newHero, "power");

            var oldStatList = GetFieldValue<Object>(oldType, oldHero, "statList");
            var oldCharacterClass = GetFieldValue<Object>(oldType, oldHero, "characterClass");
            var oldHeroVisualData = GetFieldValue<Object>(oldType, oldHero, "heroVisualData");
            var oldAnimatorData = GetFieldValue<Object>(oldType, oldHero, "animatorData");

            string playerName = GetFieldValue<string>(oldType, oldHero, "playerName");
            
            Debug.Log($"[Migration] {playerName}:");
            Debug.Log($"  - StatList: {(oldStatList != null ? AssetDatabase.GetAssetPath(oldStatList) : "null")}");
            Debug.Log($"  - CharacterClass: {(oldCharacterClass != null ? AssetDatabase.GetAssetPath(oldCharacterClass) : "null")}");
            Debug.Log($"  - HeroVisualData: {(oldHeroVisualData != null ? AssetDatabase.GetAssetPath(oldHeroVisualData) : "null")}");
            Debug.Log($"  - AnimatorData: {(oldAnimatorData != null ? AssetDatabase.GetAssetPath(oldAnimatorData) : "null")}");

            EditorUtility.SetDirty(newHero);
        }

        private T GetFieldValue<T>(System.Type type, object obj, string fieldName)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var value = field.GetValue(obj);
                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            return default;
        }

        private void CopyField(System.Type oldType, System.Type newType, object oldObj, object newObj, string fieldName)
        {
            var oldField = oldType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var newField = newType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (oldField != null && newField != null)
            {
                var value = oldField.GetValue(oldObj);
                newField.SetValue(newObj, value);
            }
        }

        private void CreateEmptyHero()
        {
            if (!Directory.Exists(NEW_HERO_PATH))
            {
                Directory.CreateDirectory(NEW_HERO_PATH);
                AssetDatabase.Refresh();
            }

            string path = Path.Combine(NEW_HERO_PATH, "Hero_Template.asset");
            HeroData newHero = ScriptableObject.CreateInstance<HeroData>();

            AssetDatabase.CreateAsset(newHero, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newHero;

            Debug.Log($"Created empty hero template at: {path}");
        }
    }
}
