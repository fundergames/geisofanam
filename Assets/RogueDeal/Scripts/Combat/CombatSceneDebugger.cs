using UnityEngine;
using RogueDeal.Player;
using RogueDeal.UI;

namespace RogueDeal.Combat
{
    public class CombatSceneDebugger : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("=== COMBAT SCENE DEBUGGER ===");
            
            var controller = FindFirstObjectByType<CombatController>();
            if (controller == null)
            {
                Debug.LogError("❌ CombatController not found in scene!");
            }
            else
            {
                Debug.Log($"✅ CombatController found - Enabled: {controller.enabled}, GameObject Active: {controller.gameObject.activeSelf}");
            }
            
            var bootstrap = FindFirstObjectByType<CombatSceneBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogWarning("⚠ CombatSceneBootstrap not found!");
            }
            else
            {
                Debug.Log("✅ CombatSceneBootstrap found");
            }
            
            var cardHandUI = FindFirstObjectByType<CardHandUI>();
            if (cardHandUI == null)
            {
                Debug.LogError("❌ CardHandUI not found in scene!");
            }
            else
            {
                Debug.Log("✅ CardHandUI found");
            }
            
            var combatUI = FindFirstObjectByType<CombatUIController>();
            if (combatUI == null)
            {
                Debug.LogError("❌ CombatUIController not found in scene!");
            }
            else
            {
                Debug.Log("✅ CombatUIController found");
            }
            
            if (PlayerDataManager.Instance != null)
            {
                var player = PlayerDataManager.Instance.CurrentPlayer;
                if (player == null)
                {
                    Debug.LogWarning("⚠ No player found in PlayerDataManager");
                }
                else
                {
                    Debug.Log($"✅ Player found: {player.characterName}");
                }
            }
            else
            {
                Debug.LogError("❌ PlayerDataManager.Instance is null!");
            }
            
            var testClass = Resources.Load<ClassDefinition>("Data/Classes/Class_Warrior");
            if (testClass == null)
            {
                Debug.LogError("❌ Class_Warrior not found in Resources!");
            }
            else
            {
                Debug.Log($"✅ Class_Warrior loaded: {testClass.displayName}");
            }
            
            var testLevel = Resources.Load<Levels.LevelDefinition>("Data/Levels/Level_Test");
            if (testLevel == null)
            {
                Debug.LogError("❌ Level_Test not found in Resources!");
            }
            else
            {
                Debug.Log($"✅ Level_Test loaded: {testLevel.name}");
            }
            
            var layoutConfig = Resources.Load<CardLayoutConfig>("Configs/CardLayoutConfig");
            if (layoutConfig == null)
            {
                Debug.LogError("❌ CardLayoutConfig not found in Resources!");
            }
            else
            {
                Debug.Log("✅ CardLayoutConfig loaded");
            }
            
            Debug.Log("=== END DEBUGGER ===");
        }
    }
}
