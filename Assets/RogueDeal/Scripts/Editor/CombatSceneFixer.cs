using UnityEngine;
using UnityEditor;
using RogueDeal.Levels;
using RogueDeal.Enemies;

namespace RogueDeal.Editor
{
    /// <summary>
    /// Helper to fix common combat scene setup issues
    /// </summary>
    public class CombatSceneFixer
    {
        [MenuItem("Funder Games/Rogue Deal/Fix Combat Scene Setup")]
        public static void FixCombatSceneSetup()
        {
            Debug.Log("=== FIXING COMBAT SCENE SETUP ===");
            
            bool hasFixed = false;
            
            // Check and create Level_Test if missing
            var testLevel = Resources.Load<LevelDefinition>("Data/Levels/Level_Test");
            if (testLevel == null)
            {
                Debug.Log("Creating missing Level_Test...");
                CreateLevelTest();
                hasFixed = true;
            }
            else
            {
                Debug.Log("✅ Level_Test already exists");
            }
            
            // Check if CombatController has testLevel assigned
            var combatController = Object.FindFirstObjectByType<Combat.CombatController>();
            if (combatController != null)
            {
                var so = new SerializedObject(combatController);
                var testLevelProp = so.FindProperty("testLevel");
                
                if (testLevelProp.objectReferenceValue == null)
                {
                    Debug.Log("Assigning Level_Test to CombatController...");
                    testLevel = Resources.Load<LevelDefinition>("Data/Levels/Level_Test");
                    if (testLevel == null)
                    {
                        // Try Level_1_1 as fallback
                        testLevel = Resources.Load<LevelDefinition>("Data/Levels/Level_1_1");
                    }
                    
                    if (testLevel != null)
                    {
                        testLevelProp.objectReferenceValue = testLevel;
                        so.ApplyModifiedProperties();
                        Debug.Log($"✅ Assigned {testLevel.name} to CombatController.testLevel");
                        hasFixed = true;
                    }
                }
                else
                {
                    Debug.Log("✅ CombatController.testLevel is already assigned");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ CombatController not found in scene (this is OK if scene isn't open)");
            }
            
            if (hasFixed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("=== COMBAT SCENE SETUP FIXED! ===");
                EditorUtility.DisplayDialog("Success", 
                    "Combat scene setup has been fixed!\n\n" +
                    "Created/verified:\n" +
                    "- Level_Test asset\n" +
                    "- CombatController.testLevel assignment\n\n" +
                    "You can now run the combat scene.", 
                    "OK");
            }
            else
            {
                Debug.Log("=== NO ISSUES FOUND - Everything looks good! ===");
            }
        }
        
        private static void CreateLevelTest()
        {
            string path = "Assets/RogueDeal/Resources/Data/Levels";
            if (!AssetDatabase.IsValidFolder(path))
            {
                System.IO.Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }

            // Check if it already exists (shouldn't, but just in case)
            var existing = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{path}/Level_Test.asset");
            if (existing != null)
            {
                Debug.Log("Level_Test already exists!");
                return;
            }

            // Try to load Goblin enemy
            var goblinEnemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/RogueDeal/Resources/Data/Enemies/Enemy_Goblin.asset");
            if (goblinEnemy == null)
            {
                // Try to find any enemy
                string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/RogueDeal/Resources/Data/Enemies" });
                if (guids.Length > 0)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    goblinEnemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(assetPath);
                    Debug.Log($"Using enemy: {goblinEnemy?.displayName ?? "null"}");
                }
            }

            var testLevel = ScriptableObject.CreateInstance<LevelDefinition>();
            testLevel.levelId = "level_test_001";
            testLevel.displayName = "Test Combat Arena";
            testLevel.description = "A test level for combat testing with 2 goblins.";
            testLevel.worldNumber = 1;
            testLevel.levelNumber = 1;
            testLevel.requiredPlayerLevel = 1;
            testLevel.energyCost = 0;
            testLevel.totalTurns = 10;
            testLevel.baseGoldReward = 50;
            testLevel.baseXPReward = 100;
            testLevel.twoStarTurnsRemaining = 3;
            testLevel.threeStarTurnsRemaining = 6;
            testLevel.combatSceneName = "Combat";
            
            if (goblinEnemy != null)
            {
                testLevel.enemySpawns = new System.Collections.Generic.List<EnemySpawn>
                {
                    new EnemySpawn { enemy = goblinEnemy, positionIndex = 0, isBoss = false },
                    new EnemySpawn { enemy = goblinEnemy, positionIndex = 1, isBoss = false }
                };
                Debug.Log($"✅ Created Level_Test with {testLevel.enemySpawns.Count} enemy spawns");
            }
            else
            {
                Debug.LogWarning("⚠️ No enemy found - Level_Test created without enemy spawns");
                Debug.LogWarning("   Run 'RogueDeal → Create Example Data → Create Test Enemy' first");
            }

            AssetDatabase.CreateAsset(testLevel, $"{path}/Level_Test.asset");
            AssetDatabase.SaveAssets();
            
            Debug.Log($"✅ Created Level_Test at {path}/Level_Test.asset");
        }
    }
}
