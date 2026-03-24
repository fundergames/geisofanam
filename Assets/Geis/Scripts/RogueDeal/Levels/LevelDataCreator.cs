using UnityEngine;

namespace RogueDeal.Levels
{
    public class LevelDataCreator : MonoBehaviour
    {
        [ContextMenu("Create Sample Levels")]
        private void CreateSampleLevels()
        {
            #if UNITY_EDITOR
            CreateLevel("level_1_1", 1, 1, "Goblin Ambush", "Face off against two sneaky goblins.", 1, 0, null, 10);
            CreateLevel("level_1_2", 1, 2, "Goblin Raiders", "More goblins have appeared!", 2, 1, "level_1_1", 10);
            CreateLevel("level_1_3", 1, 3, "Goblin Boss", "The goblin chief challenges you!", 3, 1, "level_1_2", 12);
            
            CreateLevel("level_2_1", 2, 1, "Forest Entrance", "Enter the dark forest.", 4, 2, "level_1_3", 10);
            CreateLevel("level_2_2", 2, 2, "Deep Woods", "Venture deeper into danger.", 5, 2, "level_2_1", 12);
            CreateLevel("level_2_3", 2, 3, "Forest Guardian", "Face the ancient guardian.", 6, 3, "level_2_2", 15);

            Debug.Log("[LevelDataCreator] Sample levels created in Resources/Data/Levels");
            #endif
        }

        #if UNITY_EDITOR
        private void CreateLevel(string levelId, int worldNumber, int levelNumber, 
            string displayName, string description, int requiredPlayerLevel, 
            int energyCost, string prerequisiteLevelId, int totalTurns)
        {
            string path = $"Assets/RogueDeal/Resources/Data/Levels/Level_{worldNumber}_{levelNumber}.asset";
            
            if (System.IO.File.Exists(path))
            {
                Debug.Log($"[LevelDataCreator] Level already exists: {path}");
                return;
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
            level.baseGoldReward = worldNumber * 50;
            level.baseXPReward = worldNumber * 100;
            level.twoStarTurnsRemaining = 3;
            level.threeStarTurnsRemaining = 6;
            level.combatSceneName = "Combat";

            if (!string.IsNullOrEmpty(prerequisiteLevelId))
            {
                string prereqPath = $"Assets/RogueDeal/Resources/Data/Levels";
                var prereqLevels = UnityEditor.AssetDatabase.FindAssets("t:LevelDefinition", new[] { prereqPath });
                
                foreach (var guid in prereqLevels)
                {
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    LevelDefinition prereq = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDefinition>(assetPath);
                    
                    if (prereq != null && prereq.levelId == prerequisiteLevelId)
                    {
                        level.prerequisiteLevel = prereq;
                        break;
                    }
                }
            }

            UnityEditor.AssetDatabase.CreateAsset(level, path);
            UnityEditor.AssetDatabase.SaveAssets();
            
            Debug.Log($"[LevelDataCreator] Created: {displayName} at {path}");
        }
        #endif
    }
}
