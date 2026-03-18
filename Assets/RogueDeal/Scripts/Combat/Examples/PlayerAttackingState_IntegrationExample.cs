using System.Collections;
using UnityEngine;
using DG.Tweening;
using Funder.Core.Events;
using RogueDeal.Events;
using RogueDeal.Enemies;
using RogueDeal.Combat.TurnBased;
using RogueDeal.Player;

namespace RogueDeal.Combat.States
{
    public class PlayerAttackingState_IntegrationExample : CombatState
    {
        private bool attackComplete = false;
        private PlayerAttackEvent lastAttackEvent;
        private CombatEntity cachedPlayerEntity;
        private CombatEntity cachedEnemyEntity;

        public PlayerAttackingState_IntegrationExample(CombatFlowStateMachine context) : base(context) { }

        public override void OnEnter()
        {
            Debug.Log("[FSM] Entering PlayerAttackingState (New Combat System)");
            attackComplete = false;
            
            EventBus<PlayerAttackEvent>.Subscribe(OnPlayerAttackEvent);
            context.StartCoroutine(PerformCombatAbility());
        }

        public override void OnExit()
        {
            EventBus<PlayerAttackEvent>.Unsubscribe(OnPlayerAttackEvent);
        }

        public override void OnTick(float dt)
        {
            if (attackComplete)
            {
                Debug.Log("[FSM] Ability execution complete, transitioning to EnemyReactingState");
                context.StateMachine.TryGo<EnemyReactingState>();
            }
        }

        private void OnPlayerAttackEvent(PlayerAttackEvent evt)
        {
            lastAttackEvent = evt;
            Debug.Log($"[PlayerAttackingState] Legacy attack event received: {evt.hitNumber}/{evt.totalHits}");
        }

        private IEnumerator PerformCombatAbility()
        {
            AbilityData ability = GetAbilityForCurrentHand();
            
            if (ability == null)
            {
                Debug.LogWarning("[PlayerAttackingState] No ability found, skipping");
                attackComplete = true;
                yield break;
            }

            CombatEntity playerEntity = GetOrCreatePlayerEntity();
            CombatEntity enemyEntity = GetOrCreateEnemyEntity();

            if (playerEntity == null || enemyEntity == null)
            {
                Debug.LogError("[PlayerAttackingState] Failed to get combat entities");
                attackComplete = true;
                yield break;
            }

            if (context.PlayerVisual != null && context.CurrentEnemyVisual != null)
            {
                yield return MovePlayerToEnemy();
            }

            ExecuteAbilityThroughCombatSystem(playerEntity, ability, enemyEntity);

            yield return WaitForAbilityCompletion(ability);

            attackComplete = true;
        }

        private AbilityData GetAbilityForCurrentHand()
        {
            // TODO: You need to add AbilityLookup field to CombatFlowStateMachine
            // For now, this is a placeholder showing the pattern
            
            if (!context.LastHandType.HasValue)
            {
                Debug.LogWarning("[PlayerAttackingState] No hand type available");
                return null;
            }

            // Option 1: Access via context (requires adding to CombatFlowStateMachine)
            // return context.AbilityLookup?.GetAbility(context.LastHandType);
            
            // Option 2: Load from Resources
            AbilityLookup lookup = Resources.Load<AbilityLookup>("AbilityLookup");
            if (lookup == null)
            {
                Debug.LogError("[PlayerAttackingState] AbilityLookup not found in Resources folder");
                return null;
            }

            AbilityData ability = lookup.GetAbility(context.LastHandType);
            
            if (ability == null)
            {
                Debug.LogWarning($"[PlayerAttackingState] No ability mapped for hand type: {context.LastHandType.Value}");
                return null;
            }

            Debug.Log($"[PlayerAttackingState] Using ability '{ability.abilityName}' for hand '{context.LastHandType.Value}'");
            return ability;
        }

        private CombatEntity GetOrCreatePlayerEntity()
        {
            if (cachedPlayerEntity != null && cachedPlayerEntity.gameObject == context.PlayerVisual?.gameObject)
            {
                return cachedPlayerEntity;
            }

            if (context.PlayerVisual == null)
            {
                Debug.LogError("[PlayerAttackingState] PlayerVisual is null");
                return null;
            }

            cachedPlayerEntity = context.PlayerVisual.GetComponent<CombatEntity>();
            
            if (cachedPlayerEntity == null)
            {
                Debug.Log("[PlayerAttackingState] Initializing player CombatEntity at runtime");
                
                // TODO: Get player's HeroData
                // Option 1: Load from Resources
                HeroData playerHeroData = Resources.Load<HeroData>("PlayerHeroData");
                
                // Option 2: Find it in scene on a component
                // PlayerDataHolder holder = Object.FindObjectOfType<PlayerDataHolder>();
                // HeroData playerHeroData = holder?.heroData;
                
                if (playerHeroData == null)
                {
                    Debug.LogError("[PlayerAttackingState] Could not find player's HeroData. " +
                        "Create a HeroData asset and place it in Resources folder as 'PlayerHeroData'");
                    return null;
                }
                
                cachedPlayerEntity = CombatEntityInitializer.InitializePlayer(
                    context.PlayerVisual.gameObject,
                    playerHeroData
                );
            }

            return cachedPlayerEntity;
        }

        private CombatEntity GetOrCreateEnemyEntity()
        {
            if (context.CurrentEnemyVisual == null)
            {
                Debug.Log("[PlayerAttackingState] CurrentEnemyVisual is null, refreshing");
                context.RefreshEnemyVisual();
            }

            if (context.CurrentEnemyVisual == null)
            {
                Debug.LogError("[PlayerAttackingState] Failed to get CurrentEnemyVisual");
                return null;
            }

            if (cachedEnemyEntity != null && cachedEnemyEntity.gameObject == context.CurrentEnemyVisual.gameObject)
            {
                return cachedEnemyEntity;
            }

            cachedEnemyEntity = context.CurrentEnemyVisual.GetComponent<CombatEntity>();
            
            if (cachedEnemyEntity == null)
            {
                Debug.Log("[PlayerAttackingState] Initializing enemy CombatEntity at runtime");
                
                EnemyInstance currentEnemy = context.CombatManager?.CurrentEnemy;
                if (currentEnemy == null || currentEnemy.definition == null)
                {
                    Debug.LogError("[PlayerAttackingState] No valid enemy instance available");
                    return null;
                }
                
                HeroData enemyHeroData = EnemyHeroDataConverter.GetOrCreateRuntimeHeroData(currentEnemy);
                
                if (enemyHeroData == null)
                {
                    Debug.LogError("[PlayerAttackingState] Failed to create HeroData for enemy");
                    return null;
                }
                
                cachedEnemyEntity = CombatEntityInitializer.InitializeEnemy(
                    context.CurrentEnemyVisual.gameObject,
                    enemyHeroData
                );
            }

            return cachedEnemyEntity;
        }

        private IEnumerator MovePlayerToEnemy()
        {
            Vector3 attackPosition = context.CurrentEnemyVisual.transform.position + context.AttackPositionOffset;
            float jumpDistance = Vector3.Distance(context.PlayerVisual.transform.position, attackPosition);
            float jumpTime = jumpDistance / context.AttackMoveSpeed;

            Debug.Log($"[PlayerAttackingState] Moving player to attack position over {jumpTime}s");

            Sequence jumpSequence = DOTween.Sequence();
            jumpSequence.Append(context.PlayerVisual.transform.DOMove(attackPosition, jumpTime).SetEase(Ease.OutQuad));
            jumpSequence.Join(context.PlayerVisual.transform.DOLocalMoveY(
                context.PlayerVisual.transform.position.y + 1f, 
                jumpTime * 0.5f
            ).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo));

            yield return jumpSequence.WaitForCompletion();
        }

        private void ExecuteAbilityThroughCombatSystem(CombatEntity player, AbilityData ability, CombatEntity enemy)
        {
            TurnBasedCombatPresenter presenter = Object.FindObjectOfType<TurnBasedCombatPresenter>();
            
            if (presenter == null)
            {
                Debug.LogError("[PlayerAttackingState] TurnBasedCombatPresenter not found in scene!");
                return;
            }

            Debug.Log($"[PlayerAttackingState] Executing {ability.abilityName} through combat system");
            presenter.ExecuteTurnBasedAbility(player, ability, enemy);
        }

        private IEnumerator WaitForAbilityCompletion(AbilityData ability)
        {
            float totalDuration = 2.5f;
            
            if (ability.effects != null && ability.effects.Length > 0)
            {
                float effectDuration = 0f;
                foreach (var effect in ability.effects)
                {
                    if (effect != null)
                    {
                        effectDuration += 0.5f;
                    }
                }
                totalDuration = Mathf.Max(totalDuration, effectDuration);
            }

            Debug.Log($"[PlayerAttackingState] Waiting {totalDuration}s for ability completion");
            yield return new WaitForSeconds(totalDuration);
        }
    }
}
