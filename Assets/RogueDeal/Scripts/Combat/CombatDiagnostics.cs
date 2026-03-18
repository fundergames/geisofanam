using RogueDeal.Levels;
using RogueDeal.Enemies;
using RogueDeal.Player;
using UnityEngine;

namespace RogueDeal.Combat
{
    public class CombatDiagnostics : MonoBehaviour
    {
        [ContextMenu("Run Full Diagnostics")]
        public void RunFullDiagnostics()
        {
            Debug.Log("=== COMBAT DIAGNOSTICS START ===");
            
            CheckCombatManager();
            CheckPlayerData();
            CheckLevelData();
            CheckEnemyData();
            CheckSceneReferences();
            
            Debug.Log("=== COMBAT DIAGNOSTICS END ===");
        }
        
        private void CheckCombatManager()
        {
            Debug.Log("--- Combat Manager Check ---");
            
            CombatController combatController = FindFirstObjectByType<CombatController>();
            
            if (combatController == null)
            {
                Debug.LogError("❌ CombatController not found in scene!");
                return;
            }
            
            Debug.Log($"✅ CombatController found: {combatController.gameObject.name}");
            Debug.Log($"   Enabled: {combatController.enabled}");
            
            CombatManager combatManager = combatController.CombatManager;
            
            if (combatManager == null)
            {
                Debug.LogWarning("⚠️ CombatController.CombatManager is null!");
                Debug.LogWarning("   This is normal if:");
                Debug.LogWarning("   - The scene hasn't started yet (Start() hasn't been called)");
                Debug.LogWarning("   - CombatController.Start() failed (check for errors above)");
                Debug.LogWarning("   - PlayerDataManager.Instance.CurrentPlayer is null");
                Debug.LogWarning("   - CombatController is disabled");
                
                // Check if player exists
                var player = PlayerDataManager.Instance?.CurrentPlayer;
                if (player == null)
                {
                    Debug.LogError("   ❌ PlayerDataManager.Instance.CurrentPlayer is NULL!");
                    Debug.LogError("   → Make sure CombatSceneBootstrap is in the scene or initialize player manually");
                }
                else
                {
                    Debug.Log($"   ✅ Player exists: {player.characterName}");
                }
                
                return;
            }
            
            Debug.Log("✅ CombatManager initialized");
            
            if (combatManager.Player != null)
            {
                Debug.Log($"✅ Player: {combatManager.Player.characterName} (Level {combatManager.Player.level})");
                Debug.Log($"   Class: {combatManager.Player.classDefinition.displayName}");
            }
            else
            {
                Debug.LogWarning("⚠️ CombatManager.Player is null!");
            }
            
            if (combatManager.CurrentLevel != null)
            {
                Debug.Log($"✅ Level: {combatManager.CurrentLevel.displayName}");
                Debug.Log($"   Enemy spawns: {combatManager.CurrentLevel.enemySpawns?.Count ?? 0}");
            }
            else
            {
                Debug.LogWarning("⚠️ CombatManager.CurrentLevel is null!");
            }
        }
        
        private void CheckPlayerData()
        {
            Debug.Log("--- Player Data Check ---");
            
            var playerData = PlayerDataManager.Instance.CurrentPlayer;
            
            if (playerData != null)
            {
                Debug.Log($"✅ PlayerDataManager has player: {playerData.characterName}");
                Debug.Log($"   Class: {playerData.classDefinition.displayName}");
                
                if (playerData.classDefinition.animatorData != null)
                {
                    Debug.Log($"✅ Animator Data: {playerData.classDefinition.animatorData.name}");
                    
                    if (playerData.classDefinition.animatorData.battleAnimator != null)
                    {
                        Debug.Log($"✅ Battle Animator: {playerData.classDefinition.animatorData.battleAnimator.name}");
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ Battle Animator is null!");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ Animator Data is null!");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ PlayerDataManager has no current player!");
            }
        }
        
        private void CheckLevelData()
        {
            Debug.Log("--- Level Data Check ---");
            
            var testLevel = Resources.Load<LevelDefinition>("Data/Levels/Level_Test");
            
            if (testLevel != null)
            {
                Debug.Log($"✅ Test Level loaded: {testLevel.displayName}");
                Debug.Log($"   Enemy spawns: {testLevel.enemySpawns?.Count ?? 0}");
                
                if (testLevel.enemySpawns != null)
                {
                    for (int i = 0; i < testLevel.enemySpawns.Count; i++)
                    {
                        var spawn = testLevel.enemySpawns[i];
                        if (spawn.enemy != null)
                        {
                            Debug.Log($"   Spawn {i}: {spawn.enemy.displayName} at position {spawn.positionIndex}");
                        }
                        else
                        {
                            Debug.LogWarning($"   ⚠️ Spawn {i}: enemy is null!");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Could not load Level_Test from Resources!");
                Debug.LogWarning("   Checking for alternative levels...");
                
                // Try to find any level as fallback
                var level1_1 = Resources.Load<LevelDefinition>("Data/Levels/Level_1_1");
                if (level1_1 != null)
                {
                    Debug.Log($"   ✅ Found Level_1_1 as alternative: {level1_1.displayName}");
                    Debug.Log($"   → You can use this level or create Level_Test via 'RogueDeal → Create Example Data'");
                }
                else
                {
                    Debug.LogError("   ❌ No levels found in Resources/Data/Levels/");
                    Debug.LogError("   → Run 'RogueDeal → Create Example Data' to create test data");
                }
            }
        }
        
        private void CheckEnemyData()
        {
            Debug.Log("--- Enemy Data Check ---");
            
            var goblin = Resources.Load<EnemyDefinition>("Data/Enemies/Enemy_Goblin");
            
            if (goblin != null)
            {
                Debug.Log($"✅ Goblin enemy loaded: {goblin.displayName}");
                
                if (goblin.modelPrefab != null)
                {
                    Debug.Log($"✅ Model Prefab: {goblin.modelPrefab.name}");
                }
                else
                {
                    Debug.LogError("❌ Goblin has no model prefab assigned!");
                }
            }
            else
            {
                Debug.LogError("❌ Could not load Enemy_Goblin from Resources!");
            }
        }
        
        private void CheckSceneReferences()
        {
            Debug.Log("--- Scene References Check ---");
            
            PlayerVisual playerVisual = FindFirstObjectByType<PlayerVisual>();
            Debug.Log(playerVisual != null 
                ? $"✅ PlayerVisual found: {playerVisual.gameObject.name}" 
                : "⚠️ PlayerVisual not found in scene");
            
            EnemyVisual[] enemyVisuals = FindObjectsByType<EnemyVisual>(FindObjectsSortMode.None);
            Debug.Log($"Enemy Visuals in scene: {enemyVisuals.Length}");
            
            CombatFlowStateMachine fsm = FindFirstObjectByType<CombatFlowStateMachine>();
            Debug.Log(fsm != null 
                ? $"✅ CombatFlowStateMachine found" 
                : "❌ CombatFlowStateMachine not found!");
            
            CombatIntroController intro = FindFirstObjectByType<CombatIntroController>();
            Debug.Log(intro != null 
                ? $"✅ CombatIntroController found" 
                : "⚠️ CombatIntroController not found");
        }
    }
}
