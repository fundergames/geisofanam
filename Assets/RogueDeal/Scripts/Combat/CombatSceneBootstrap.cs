using RogueDeal.Player;
using UnityEngine;

namespace RogueDeal.Combat
{
    public class CombatSceneBootstrap : MonoBehaviour
    {
        [Header("Test Data")]
        [SerializeField] private ClassDefinition testClass;
        
        private void Awake()
        {
            EnsureCombatControllerEnabled();
            InitializeTestPlayer();
        }

        private void EnsureCombatControllerEnabled()
        {
            var controller = GetComponent<CombatController>();
            if (controller != null && !controller.enabled)
            {
                Debug.LogWarning("[CombatBootstrap] CombatController was DISABLED! Enabling it now...");
                controller.enabled = true;
            }
            else if (controller != null)
            {
                Debug.Log("[CombatBootstrap] CombatController is already enabled.");
            }
        }
        
        private void InitializeTestPlayer()
        {
            if (PlayerDataManager.Instance.CurrentPlayer == null)
            {
                if (testClass == null)
                {
                    testClass = Resources.Load<ClassDefinition>("Data/Classes/Class_Warrior");
                }
                
                if (testClass != null)
                {
                    PlayerDataManager.Instance.InitializePlayer(testClass, "TestPlayer");
                    Debug.Log("[CombatBootstrap] Created test player with class: " + testClass.displayName);
                }
                else
                {
                    Debug.LogError("[CombatBootstrap] No class found! Make sure to run 'RogueDeal → Create Example Data' first!");
                }
            }
            else
            {
                Debug.Log("[CombatBootstrap] Player already exists: " + PlayerDataManager.Instance.CurrentPlayer.characterName);
            }
        }
    }
}
