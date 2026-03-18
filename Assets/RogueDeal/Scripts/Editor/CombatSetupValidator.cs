using UnityEngine;
using UnityEditor;
using RogueDeal.Player;
using RogueDeal.Enemies;
using RogueDeal.Combat;
using System.Collections.Generic;
using System.Text;

namespace RogueDeal.Editor
{
    public class CombatSetupValidator : EditorWindow
    {
        private Vector2 scrollPosition;
        private StringBuilder report = new StringBuilder();
        
        [MenuItem("Rogue Deal/Combat Setup Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<CombatSetupValidator>("Combat Validator");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Combat Scene Setup Validator", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
            {
                RunValidation();
            }
            
            EditorGUILayout.Space(10);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.TextArea(report.ToString(), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
        
        private void RunValidation()
        {
            report.Clear();
            report.AppendLine("=== COMBAT SETUP VALIDATION REPORT ===");
            report.AppendLine($"Generated: {System.DateTime.Now}");
            report.AppendLine();
            
            ValidateHeroVisualData();
            ValidateEnemyDefinitions();
            ValidateCombatScene();
            ValidateAnimatorControllers();
            
            report.AppendLine();
            report.AppendLine("=== VALIDATION COMPLETE ===");
            
            Repaint();
        }
        
        private void ValidateHeroVisualData()
        {
            report.AppendLine("--- Hero Visual Data ---");
            
            string[] guids = AssetDatabase.FindAssets("t:HeroVisualData");
            int totalHeroes = guids.Length;
            int assignedHeroes = 0;
            int missingPrefabs = 0;
            
            List<string> missingList = new List<string>();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                HeroVisualData heroVisual = AssetDatabase.LoadAssetAtPath<HeroVisualData>(path);
                
                if (heroVisual != null)
                {
                    if (heroVisual.characterPrefab != null)
                    {
                        assignedHeroes++;
                        
                        Animator animator = heroVisual.characterPrefab.GetComponentInChildren<Animator>();
                        if (animator == null)
                        {
                            report.AppendLine($"  ⚠ {heroVisual.name}: Prefab missing Animator");
                        }
                    }
                    else
                    {
                        missingPrefabs++;
                        missingList.Add(heroVisual.name);
                    }
                }
            }
            
            report.AppendLine($"Total Heroes: {totalHeroes}");
            report.AppendLine($"✓ Assigned Prefabs: {assignedHeroes}");
            report.AppendLine($"⚠ Missing Prefabs: {missingPrefabs}");
            
            if (missingList.Count > 0)
            {
                report.AppendLine("Missing prefabs in:");
                foreach (string name in missingList)
                {
                    report.AppendLine($"  - {name}");
                }
            }
            
            report.AppendLine();
        }
        
        private void ValidateEnemyDefinitions()
        {
            report.AppendLine("--- Enemy Definitions ---");
            
            string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition");
            int totalEnemies = guids.Length;
            int assignedEnemies = 0;
            int missingPrefabs = 0;
            
            List<string> missingList = new List<string>();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EnemyDefinition enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                
                if (enemy != null)
                {
                    if (enemy.modelPrefab != null)
                    {
                        assignedEnemies++;
                        
                        Animator animator = enemy.modelPrefab.GetComponentInChildren<Animator>();
                        if (animator == null)
                        {
                            report.AppendLine($"  ⚠ {enemy.displayName}: Prefab missing Animator");
                        }
                    }
                    else
                    {
                        missingPrefabs++;
                        missingList.Add(enemy.displayName);
                    }
                }
            }
            
            report.AppendLine($"Total Enemies: {totalEnemies}");
            report.AppendLine($"✓ Assigned Prefabs: {assignedEnemies}");
            report.AppendLine($"⚠ Missing Prefabs: {missingPrefabs}");
            
            if (missingList.Count > 0)
            {
                report.AppendLine("Missing prefabs in:");
                foreach (string name in missingList)
                {
                    report.AppendLine($"  - {name}");
                }
            }
            
            report.AppendLine();
        }
        
        private void ValidateCombatScene()
        {
            report.AppendLine("--- Combat Scene Setup ---");
            
            string combatScenePath = "Assets/RogueDeal/Scenes/Combat.unity";
            
            var combatSceneAsset = AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(combatScenePath);
            if (combatSceneAsset == null)
            {
                report.AppendLine("⚠ Combat scene not found at expected path");
                report.AppendLine();
                return;
            }
            
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.path != combatScenePath)
            {
                report.AppendLine("ℹ Combat scene is not currently open");
                report.AppendLine("  Open the Combat scene to validate runtime setup");
                report.AppendLine();
                return;
            }
            
            CombatSceneManager sceneManager = Object.FindFirstObjectByType<CombatSceneManager>();
            if (sceneManager == null)
            {
                report.AppendLine("⚠ CombatSceneManager not found in scene");
            }
            else
            {
                report.AppendLine("✓ CombatSceneManager found");
            }
            
            CombatController combatController = Object.FindFirstObjectByType<CombatController>();
            if (combatController == null)
            {
                report.AppendLine("⚠ CombatController not found in scene");
            }
            else
            {
                report.AppendLine("✓ CombatController found");
                if (combatController.CombatManager == null)
                {
                    report.AppendLine("⚠ CombatManager not initialized (may need to start combat)");
                }
                else
                {
                    report.AppendLine("✓ CombatManager is initialized");
                }
            }
            
            PlayerVisual playerVisual = Object.FindFirstObjectByType<PlayerVisual>();
            if (playerVisual != null)
            {
                report.AppendLine($"✓ PlayerVisual found: {playerVisual.name}");
                if (playerVisual.Animator == null)
                {
                    report.AppendLine("  ⚠ PlayerVisual missing Animator");
                }
            }
            
            EnemyVisual[] enemyVisuals = Object.FindObjectsByType<EnemyVisual>(FindObjectsSortMode.None);
            if (enemyVisuals.Length > 0)
            {
                report.AppendLine($"✓ Found {enemyVisuals.Length} EnemyVisual(s)");
                
                foreach (var enemy in enemyVisuals)
                {
                    if (enemy.Animator == null)
                    {
                        report.AppendLine($"  ⚠ {enemy.name}: Missing Animator");
                    }
                }
            }
            else
            {
                report.AppendLine("ℹ No EnemyVisuals found (normal before combat starts)");
            }
            
            report.AppendLine();
        }
        
        private void ValidateAnimatorControllers()
        {
            report.AppendLine("--- Animator Controllers ---");
            
            string baseBattleControllerPath = "Assets/TEMP_REMOVE_WHEN_CONVERTED/FunderGames/RPG/Assets/Animators/BaseBattleController.controller";
            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(baseBattleControllerPath);
            
            if (controller == null)
            {
                report.AppendLine("⚠ BaseBattleController not found at expected path");
                report.AppendLine();
                return;
            }
            
            report.AppendLine($"✓ Found: {controller.name}");
            
            bool hasAttack = HasParameter(controller, "Attack");
            bool hasDamage = HasParameter(controller, "Damage");
            bool hasSpawn = HasParameter(controller, "Spawn");
            bool hasDeath = HasParameter(controller, "Death");
            
            report.AppendLine($"  Attack Parameter: {(hasAttack ? "✓" : "⚠ Missing")}");
            report.AppendLine($"  Damage Parameter: {(hasDamage ? "✓" : "⚠ Missing")}");
            report.AppendLine($"  Spawn Parameter: {(hasSpawn ? "✓" : "⚠ Missing")}");
            report.AppendLine($"  Death Parameter: {(hasDeath ? "✓" : "⚠ Missing")}");
            
            report.AppendLine();
        }
        
        private bool HasParameter(UnityEditor.Animations.AnimatorController controller, string paramName)
        {
            foreach (var param in controller.parameters)
            {
                if (param.name == paramName)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
