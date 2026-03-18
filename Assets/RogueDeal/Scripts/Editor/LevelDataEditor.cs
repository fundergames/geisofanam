using UnityEngine;
using UnityEditor;
using RogueDeal.Levels;
using System.IO;

namespace RogueDeal.Editor
{
    public class LevelDataEditor : EditorWindow
    {
        private int worldNumber = 1;
        private int levelNumber = 1;
        private string displayName = "New Level";
        private string description = "Level description";
        private int requiredPlayerLevel = 1;
        private int energyCost = 1;
        private int totalTurns = 10;
        private int baseGoldReward = 100;
        private int baseXPReward = 50;
        private LevelDefinition prerequisiteLevel;

        [MenuItem("Funder Games/Level Editor")]
        public static void ShowWindow()
        {
            GetWindow<LevelDataEditor>("Level Creator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Create New Level", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            worldNumber = EditorGUILayout.IntField("World Number", worldNumber);
            levelNumber = EditorGUILayout.IntField("Level Number", levelNumber);
            
            EditorGUILayout.Space();
            displayName = EditorGUILayout.TextField("Display Name", displayName);
            description = EditorGUILayout.TextField("Description", description, GUILayout.Height(60));
            
            EditorGUILayout.Space();
            requiredPlayerLevel = EditorGUILayout.IntField("Required Player Level", requiredPlayerLevel);
            energyCost = EditorGUILayout.IntField("Energy Cost", energyCost);
            totalTurns = EditorGUILayout.IntField("Total Turns", totalTurns);
            
            EditorGUILayout.Space();
            baseGoldReward = EditorGUILayout.IntField("Base Gold Reward", baseGoldReward);
            baseXPReward = EditorGUILayout.IntField("Base XP Reward", baseXPReward);
            
            EditorGUILayout.Space();
            prerequisiteLevel = (LevelDefinition)EditorGUILayout.ObjectField("Prerequisite Level", prerequisiteLevel, typeof(LevelDefinition), false);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Level", GUILayout.Height(40)))
            {
                CreateLevel();
            }

            EditorGUILayout.Space();
            
            if (GUILayout.Button("Create Sample World 1 (6 Levels)", GUILayout.Height(30)))
            {
                CreateSampleWorld1();
            }
            
            if (GUILayout.Button("Clear All Level Progress (PlayerPrefs)", GUILayout.Height(30)))
            {
                ClearLevelProgress();
            }
        }

        private void CreateLevel()
        {
            string folderPath = "Assets/RogueDeal/Resources/Data/Levels";
            
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string levelId = $"level_{worldNumber}_{levelNumber}";
            string fileName = $"Level_{worldNumber}_{levelNumber}.asset";
            string fullPath = Path.Combine(folderPath, fileName);

            if (File.Exists(fullPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Level Exists",
                    $"Level {worldNumber}-{levelNumber} already exists. Overwrite?",
                    "Yes", "No");
                    
                if (!overwrite)
                {
                    return;
                }
            }

            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.levelId = levelId;
            level.worldNumber = worldNumber;
            level.levelNumber = levelNumber;
            level.displayName = displayName;
            level.description = description;
            level.requiredPlayerLevel = requiredPlayerLevel;
            level.energyCost = energyCost;
            level.totalTurns = totalTurns;
            level.baseGoldReward = baseGoldReward;
            level.baseXPReward = baseXPReward;
            level.prerequisiteLevel = prerequisiteLevel;
            level.twoStarTurnsRemaining = Mathf.Max(1, totalTurns / 3);
            level.threeStarTurnsRemaining = Mathf.Max(2, totalTurns * 2 / 3);
            level.combatSceneName = "Combat";

            AssetDatabase.CreateAsset(level, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = level;

            Debug.Log($"[LevelDataEditor] Created level: {displayName} at {fullPath}");
        }

        private void CreateSampleWorld1()
        {
            CreateSampleLevel(1, 1, "First Steps", "Learn the basics of combat.", 1, 0, null, 10, 50, 100);
            
            LevelDefinition level1 = LoadLevel(1, 1);
            CreateSampleLevel(1, 2, "Goblin Trouble", "More goblins block your path!", 1, 1, level1, 10, 75, 125);
            
            LevelDefinition level2 = LoadLevel(1, 2);
            CreateSampleLevel(1, 3, "Goblin Chief", "Face the goblin leader!", 2, 1, level2, 12, 100, 150);
            
            LevelDefinition level3 = LoadLevel(1, 3);
            CreateSampleLevel(1, 4, "Forest Path", "Journey into the dark woods.", 3, 2, level3, 12, 125, 175);
            
            LevelDefinition level4 = LoadLevel(1, 4);
            CreateSampleLevel(1, 5, "Ambush!", "You've been ambushed!", 4, 2, level4, 15, 150, 200);
            
            LevelDefinition level5 = LoadLevel(1, 5);
            CreateSampleLevel(1, 6, "Boss Battle", "The dungeon boss awaits!", 5, 3, level5, 20, 200, 300);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[LevelDataEditor] Created 6 sample levels for World 1");
        }

        private void CreateSampleLevel(int world, int level, string name, string desc, 
            int reqLevel, int energy, LevelDefinition prereq, int turns, int gold, int xp)
        {
            worldNumber = world;
            levelNumber = level;
            displayName = name;
            description = desc;
            requiredPlayerLevel = reqLevel;
            energyCost = energy;
            totalTurns = turns;
            baseGoldReward = gold;
            baseXPReward = xp;
            prerequisiteLevel = prereq;
            
            CreateLevel();
        }

        private LevelDefinition LoadLevel(int world, int level)
        {
            string path = $"Assets/RogueDeal/Resources/Data/Levels/Level_{world}_{level}.asset";
            return AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
        }

        private void ClearLevelProgress()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Clear Progress",
                "This will delete all level progress from PlayerPrefs. Are you sure?",
                "Yes, Clear", "Cancel");
                
            if (confirm)
            {
                PlayerPrefs.DeleteKey("LevelProgress");
                PlayerPrefs.Save();
                Debug.Log("[LevelDataEditor] Cleared all level progress");
            }
        }
    }
}
