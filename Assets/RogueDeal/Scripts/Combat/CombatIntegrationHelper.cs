using UnityEngine;
using RogueDeal.UI;

namespace RogueDeal.Combat
{
    [ExecuteInEditMode]
    public class CombatIntegrationHelper : MonoBehaviour
    {

        [Header("Required Scene Components")]
        [SerializeField] private CombatController combatController;
        [SerializeField] private CombatSceneManager sceneManager;
        [SerializeField] private CombatUIController uiController;
        [SerializeField] private CardHandUI cardHandUI;

        [Header("Spawn Configuration")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform[] enemySpawnPoints;

        [Header("Prefab Templates")]
        [SerializeField] private GameObject playerPrefabTemplate;
        [SerializeField] private GameObject enemyPrefabTemplate;

        [ContextMenu("Validate Setup")]
        public void ValidateSetup()
        {
            Debug.Log("=== COMBAT SCENE VALIDATION ===");
            
            bool allValid = true;

            if (combatController == null)
            {
                Debug.LogError("❌ CombatController is missing!");
                allValid = false;
            }
            else
            {
                Debug.Log("✓ CombatController found");
            }

            if (sceneManager == null)
            {
                Debug.LogWarning("⚠ CombatSceneManager is missing!");
                allValid = false;
            }
            else
            {
                Debug.Log("✓ CombatSceneManager found");
            }

            if (uiController == null)
            {
                Debug.LogWarning("⚠ CombatUIController is missing!");
                allValid = false;
            }
            else
            {
                Debug.Log("✓ CombatUIController found");
            }

            if (cardHandUI == null)
            {
                Debug.LogWarning("⚠ CardHandUI is missing!");
                allValid = false;
            }
            else
            {
                Debug.Log("✓ CardHandUI found");
            }

            if (playerSpawnPoint == null)
            {
                Debug.LogWarning("⚠ Player Spawn Point is not assigned!");
                allValid = false;
            }
            else
            {
                Debug.Log("✓ Player Spawn Point assigned");
            }

            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                Debug.LogWarning("⚠ Enemy Spawn Points array is empty!");
                allValid = false;
            }
            else
            {
                Debug.Log($"✓ {enemySpawnPoints.Length} Enemy Spawn Points configured");
            }

            if (allValid)
            {
                Debug.Log("=== ✓ ALL VALIDATIONS PASSED ===");
            }
            else
            {
                Debug.Log("=== ⚠ SOME VALIDATIONS FAILED - Check warnings above ===");
            }
        }

        [ContextMenu("Auto-Find Components")]
        public void AutoFindComponents()
        {
            if (combatController == null)
                combatController = FindFirstObjectByType<CombatController>();

            if (sceneManager == null)
                sceneManager = FindFirstObjectByType<CombatSceneManager>();

            if (uiController == null)
                uiController = FindFirstObjectByType<CombatUIController>(); 

            if (cardHandUI == null)
                cardHandUI = FindFirstObjectByType<CardHandUI>();

            Debug.Log("Auto-find complete. Check inspector for results.");
        }
    }
}
