using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Presentation;

namespace RogueDeal.Combat.TurnBased
{
    /// <summary>
    /// Turn-based combat presenter using the CombatAction/CombatExecutor system.
    /// </summary>
    public class TurnBasedCombatPresenter : MonoBehaviour
    {
        [SerializeField] private PlayableDirector sequenceDirector;
        
        private bool isExecutionComplete = true;

        public bool IsExecutionComplete => isExecutionComplete;

        /// <summary>
        /// Executes a turn-based action using the CombatExecutor system.
        /// </summary>
        public void ExecuteTurnBasedAbility(CombatEntity caster, CombatAction action, CombatEntity target)
        {
            if (caster == null || action == null)
            {
                Debug.LogWarning("[TurnBasedCombatPresenter] Cannot execute action: caster or action is null");
                return;
            }

            // Get or add CombatExecutor to caster
            CombatExecutor combatExecutor = caster.GetComponent<CombatExecutor>();
            if (combatExecutor == null)
            {
                combatExecutor = caster.gameObject.AddComponent<CombatExecutor>();
                Debug.Log($"[TurnBasedCombatPresenter] Added CombatExecutor to {caster.gameObject.name}");
            }

            isExecutionComplete = false;

            Debug.Log($"[TurnBasedCombatPresenter] ExecuteTurnBasedAbility called - Action: {action.actionName}");

            // Execute using new CombatExecutor system
            // CombatExecutor handles targeting, animations, and effects
            bool executed = combatExecutor.ExecuteAction(action);
            
            if (!executed)
            {
                Debug.LogWarning($"[TurnBasedCombatPresenter] Failed to execute action '{action.actionName}' - may be on cooldown or no valid targets");
                isExecutionComplete = true;
                return;
            }

            // Wait for execution to complete
            // Note: CombatExecutor doesn't have a completion callback yet
            // For now, we'll use a coroutine to wait
            StartCoroutine(WaitForExecutionComplete(combatExecutor));
        }
        
        /// <summary>
        /// Legacy method for backward compatibility - converts AbilityData to CombatAction
        /// </summary>
        [System.Obsolete("Use ExecuteTurnBasedAbility with CombatAction instead")]
        public void ExecuteTurnBasedAbility(CombatEntity caster, AbilityData ability, CombatEntity target)
        {
            Debug.LogWarning("[TurnBasedCombatPresenter] ExecuteTurnBasedAbility(AbilityData) is deprecated. Convert AbilityData to CombatAction first.");
            // Could add adapter call here if needed, but better to migrate
        }

        private IEnumerator WaitForExecutionComplete(CombatExecutor executor)
        {
            // Wait until action is no longer executing
            // This is a simple approach - ideally CombatExecutor would have an event/callback
            while (executor.GetCurrentAction() != null)
            {
                yield return null;
            }
            
            isExecutionComplete = true;
            Debug.Log("[TurnBasedCombatPresenter] Execution complete");
        }

        // Legacy methods removed - ExecuteTurnBasedAbility now uses CombatExecutor directly
        // Timeline and effects are handled by CombatExecutor
    }
}
