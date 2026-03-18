using System.Collections;
using UnityEngine;
using RogueDeal.Combat.TurnBased;

namespace RogueDeal.Combat.States
{
    public class ExecutingAbilityState : CombatState
    {
        private bool abilityComplete = false;
        private AbilityData currentAbility;
        private CombatEntity targetEntity;

        public ExecutingAbilityState(CombatFlowStateMachine context) : base(context) { }

        public void SetAbilityData(AbilityData ability, CombatEntity target)
        {
            currentAbility = ability;
            targetEntity = target;
        }

        public override void OnEnter()
        {
            Debug.Log("[FSM] Entering ExecutingAbilityState");
            abilityComplete = false;
            
            CombatEvents.OnAttackCompleted += OnAttackCompleted;
            
            context.StartCoroutine(ExecuteAbility());
        }

        public override void OnExit()
        {
            CombatEvents.OnAttackCompleted -= OnAttackCompleted;
        }

        public override void OnTick(float dt)
        {
            if (abilityComplete)
            {
                Debug.Log("[FSM] Ability execution complete");
            }
        }

        private IEnumerator ExecuteAbility()
        {
            TurnBasedCombatPresenter presenter = Object.FindObjectOfType<TurnBasedCombatPresenter>();
            
            if (presenter == null)
            {
                Debug.LogError("[ExecutingAbilityState] No TurnBasedCombatPresenter found in scene!");
                abilityComplete = true;
                yield break;
            }

            CombatEntity playerEntity = GetPlayerEntity();
            
            if (playerEntity == null || currentAbility == null || targetEntity == null)
            {
                Debug.LogError("[ExecutingAbilityState] Missing required references!");
                abilityComplete = true;
                yield break;
            }

            presenter.ExecuteTurnBasedAbility(playerEntity, currentAbility, targetEntity);
            
            yield return new WaitForSeconds(2f);
        }

        private void OnAttackCompleted(CombatEventData data)
        {
            abilityComplete = true;
        }

        private CombatEntity GetPlayerEntity()
        {
            return Object.FindObjectOfType<CombatEntity>();
        }
    }
}
