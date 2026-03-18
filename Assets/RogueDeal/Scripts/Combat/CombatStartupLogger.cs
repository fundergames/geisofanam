using RogueDeal.Combat;
using RogueDeal.Levels;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class CombatStartupLogger : MonoBehaviour
{
    [SerializeField] private bool enableDetailedLogging = true;

    private void Awake()
    {
        if (!enableDetailedLogging) return;

        Debug.Log("=== COMBAT STARTUP LOGGER ===");
        
        CheckLevelManager();
        CheckCombatController();
    }

    private void CheckLevelManager()
    {
        Debug.Log("--- Level Manager Check ---");
        
        if (LevelManager.Instance != null)
        {
            Debug.Log("✅ LevelManager exists");
            
            var selectedLevel = LevelManager.Instance.GetSelectedLevel();
            
            if (selectedLevel != null)
            {
                Debug.Log($"⚠️ LevelManager has selected level: {selectedLevel.displayName}");
                Debug.Log($"   This level will be used instead of testLevel!");
                Debug.Log($"   Enemy spawns: {selectedLevel.enemySpawns?.Count ?? 0}");
                
                if (selectedLevel.enemySpawns != null && selectedLevel.enemySpawns.Count > 0)
                {
                    for (int i = 0; i < selectedLevel.enemySpawns.Count; i++)
                    {
                        var spawn = selectedLevel.enemySpawns[i];
                        if (spawn.enemy != null)
                        {
                            Debug.Log($"      Spawn {i}: {spawn.enemy.displayName}");
                        }
                        else
                        {
                            Debug.LogError($"      ❌ Spawn {i}: enemy is NULL!");
                        }
                    }
                }
                else
                {
                    Debug.LogError("   ❌ Selected level has NO enemy spawns!");
                }
            }
            else
            {
                Debug.Log("✅ LevelManager has no selected level (testLevel will be used)");
            }
        }
        else
        {
            Debug.Log("✅ No LevelManager instance (testLevel will be used)");
        }
    }

    private void CheckCombatController()
    {
        Debug.Log("--- Combat Controller Check ---");
        
        var controller = FindFirstObjectByType<CombatController>();
        
        if (controller != null)
        {
            Debug.Log("✅ CombatController found");
        }
        else
        {
            Debug.LogError("❌ CombatController NOT found!");
        }
    }
}
