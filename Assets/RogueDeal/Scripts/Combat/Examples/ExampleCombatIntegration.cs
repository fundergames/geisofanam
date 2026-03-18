using UnityEngine;
using RogueDeal.Combat.TurnBased;

namespace RogueDeal.Combat.Examples
{
    public class ExampleCombatIntegration : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurnBasedCombatPresenter combatPresenter;
        [SerializeField] private CombatEntity playerEntity;
        [SerializeField] private CombatEntity enemyEntity;
        
        [Header("Test Abilities")]
        [SerializeField] private AbilityData testAttackAbility;

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            CombatEvents.OnAttackStarted += OnAttackStarted;
            CombatEvents.OnDamageApplied += OnDamageApplied;
            CombatEvents.OnAttackCompleted += OnAttackCompleted;
        }

        private void UnsubscribeFromEvents()
        {
            CombatEvents.OnAttackStarted -= OnAttackStarted;
            CombatEvents.OnDamageApplied -= OnDamageApplied;
            CombatEvents.OnAttackCompleted -= OnAttackCompleted;
        }

        private void OnAttackStarted(CombatEventData data)
        {
            Debug.Log($"[Combat] {data.source.name} started attacking {data.target.name} with {data.ability.abilityName}");
        }

        private void OnDamageApplied(CombatEventData data)
        {
            string critText = data.wasCritical ? " (CRITICAL!)" : "";
            Debug.Log($"[Combat] {data.target.name} took {data.damageAmount:F1} damage{critText}");
        }

        private void OnAttackCompleted(CombatEventData data)
        {
            Debug.Log($"[Combat] Attack completed");
        }

        [ContextMenu("Test Player Attack")]
        public void TestPlayerAttack()
        {
            if (combatPresenter == null || playerEntity == null || enemyEntity == null || testAttackAbility == null)
            {
                Debug.LogError("Missing references for test attack!");
                return;
            }

            combatPresenter.ExecuteTurnBasedAbility(playerEntity, testAttackAbility, enemyEntity);
        }

        [ContextMenu("Test Enemy Attack")]
        public void TestEnemyAttack()
        {
            if (combatPresenter == null || playerEntity == null || enemyEntity == null || testAttackAbility == null)
            {
                Debug.LogError("Missing references for test attack!");
                return;
            }

            combatPresenter.ExecuteTurnBasedAbility(enemyEntity, testAttackAbility, playerEntity);
        }
    }
}
