using RogueDeal.Combat;
using RogueDeal.Enemies;
using RogueDeal.Levels;
using RogueDeal.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RogueDeal.Editor
{
    public class CombatSetupHelper : EditorWindow
    {
        [MenuItem("Funder Games/Rogue Deal/Combat Setup Helper")]
        public static void ShowWindow()
        {
            GetWindow<CombatSetupHelper>("Combat Setup Helper");
        }

        private void OnGUI()
        {
            GUILayout.Label("Combat Setup & Testing", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("1. Create Example Data", GUILayout.Height(40)))
            {
                GameDataCreator.ShowWindow();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("2. Fix Combat References", GUILayout.Height(40)))
            {
                FixEnemyModelReference.FixCombatDataReferences();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("3. Verify Setup", GUILayout.Height(40)))
            {
                VerifySetup();
            }

            GUILayout.Space(10);
            GUILayout.Label("Quick Actions:", EditorStyles.boldLabel);

            if (GUILayout.Button("Load Combat Scene", GUILayout.Height(30)))
            {
                EditorSceneManager.OpenScene("Assets/RogueDeal/Scenes/Combat.unity");
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Load Entry Scene", GUILayout.Height(30)))
            {
                EditorSceneManager.OpenScene("Assets/RogueDeal/Scenes/Entry.unity");
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Setup Steps:\n" +
                "1. Create Example Data (or click CREATE ALL)\n" +
                "2. Fix Combat References (assigns models & animators)\n" +
                "3. Verify Setup (check all data is correct)\n" +
                "4. Load Entry Scene and press Play!",
                MessageType.Info
            );
        }

        private void VerifySetup()
        {
            Debug.Log("=== COMBAT SETUP VERIFICATION ===");
            bool allGood = true;

            var warrior = AssetDatabase.LoadAssetAtPath<ClassDefinition>("Assets/RogueDeal/Resources/Data/Classes/Class_Warrior.asset");
            if (warrior == null)
            {
                Debug.LogError("❌ Class_Warrior.asset not found! Run 'Create Example Data' first.");
                allGood = false;
            }
            else
            {
                Debug.Log($"✅ Warrior class found: {warrior.displayName}");

                if (warrior.animatorData == null)
                {
                    Debug.LogWarning("⚠️ Warrior has no animator data! Run 'Fix Combat References'.");
                    allGood = false;
                }
                else
                {
                    Debug.Log($"✅ Animator data: {warrior.animatorData.name}");

                    if (warrior.animatorData.battleAnimator == null)
                    {
                        Debug.LogWarning("⚠️ Animator data has no battle controller!");
                        allGood = false;
                    }
                    else
                    {
                        Debug.Log($"✅ Battle controller: {warrior.animatorData.battleAnimator.name}");
                    }
                }
            }

            var goblin = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/RogueDeal/Resources/Data/Enemies/Enemy_Goblin.asset");
            if (goblin == null)
            {
                Debug.LogError("❌ Enemy_Goblin.asset not found! Run 'Create Example Data' first.");
                allGood = false;
            }
            else
            {
                Debug.Log($"✅ Goblin enemy found: {goblin.displayName}");

                if (goblin.modelPrefab == null)
                {
                    Debug.LogWarning("⚠️ Goblin has no model prefab! Run 'Fix Combat References'.");
                    allGood = false;
                }
                else
                {
                    Debug.Log($"✅ Model prefab: {goblin.modelPrefab.name}");
                }
            }

            var testLevel = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/RogueDeal/Resources/Data/Levels/Level_Test.asset");
            if (testLevel == null)
            {
                Debug.LogError("❌ Level_Test.asset not found! Run 'Create Example Data' first.");
                allGood = false;
            }
            else
            {
                Debug.Log($"✅ Test level found: {testLevel.displayName}");
                Debug.Log($"   Enemy spawns: {testLevel.enemySpawns?.Count ?? 0}");
            }

            Debug.Log("=== VERIFICATION COMPLETE ===");

            if (allGood)
            {
                EditorUtility.DisplayDialog(
                    "Setup Verified!",
                    "✅ All combat data is configured correctly!\n\nYou can now:\n- Load Entry scene\n- Press Play\n- Combat should start with player and 2 goblins",
                    "Great!"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Setup Issues Found",
                    "⚠️ Some issues were found. Check the Console for details.\n\nTry running:\n1. Create Example Data\n2. Fix Combat References",
                    "OK"
                );
            }
        }
    }
}
