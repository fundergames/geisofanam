using UnityEngine;
using UnityEngine.InputSystem;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Presentation;

namespace RogueDeal.Combat.RealTime
{
    /// <summary>
    /// Real-time combat controller using the CombatAction/CombatExecutor system.
    /// </summary>
    public class RealTimeCombatController : MonoBehaviour
    {
        [SerializeField] private CombatEntity combatEntity;
        [SerializeField] private CombatAction[] equippedActions;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private float maxTargetRange = 20f;

        private CombatExecutor combatExecutor;

        private void Awake()
        {
            if (combatEntity == null)
                combatEntity = GetComponent<CombatEntity>();
            
            // Get or add CombatExecutor
            combatExecutor = combatEntity.GetComponent<CombatExecutor>();
            if (combatExecutor == null)
            {
                combatExecutor = combatEntity.gameObject.AddComponent<CombatExecutor>();
                Debug.Log($"[RealTimeCombatController] Added CombatExecutor to {combatEntity.gameObject.name}");
            }
        }

        public void UseAbility(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= equippedActions.Length)
                return;

            CombatAction action = equippedActions[abilityIndex];
            if (action == null)
            {
                Debug.LogWarning($"[RealTimeCombatController] Action at index {abilityIndex} is null");
                return;
            }

            // Execute using new system
            bool executed = combatExecutor.ExecuteAction(action);
            if (!executed)
            {
                Debug.LogWarning($"[RealTimeCombatController] Failed to execute action '{action.actionName}' - may be on cooldown or no valid targets");
            }
        }

        public void OnAbility1(InputValue value)
        {
            if (value.isPressed) UseAbility(0);
        }

        public void OnAbility2(InputValue value)
        {
            if (value.isPressed) UseAbility(1);
        }

        public void OnAbility3(InputValue value)
        {
            if (value.isPressed) UseAbility(2);
        }
        
        public float GetAbilityCooldownProgress(int index)
        {
            if (index < 0 || index >= equippedActions.Length || combatExecutor == null)
                return 0f;

            CombatAction action = equippedActions[index];
            if (action == null)
                return 0f;

            var cooldownManager = combatExecutor.GetCooldownManager();
            if (cooldownManager == null)
                return 0f;

            // Get cooldown progress from ActionCooldownManager
            // Note: This requires exposing cooldown progress from ActionCooldownManager
            // For now, return 0 if on cooldown, 1 if available
            bool isAvailable = cooldownManager.IsActionAvailable(action);
            return isAvailable ? 1f : 0f; // Simplified - full progress tracking can be added later
        }
    }
}
