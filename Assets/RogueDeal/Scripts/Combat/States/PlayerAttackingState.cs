using System.Collections;
using UnityEngine;
using DG.Tweening;
using RogueDeal.Combat.TurnBased;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Events;
using RogueDeal.Enemies;
using RogueDeal.Player;

namespace RogueDeal.Combat.States
{
    public class PlayerAttackingState : CombatState
    {
        private bool attackComplete = false;
        private PlayerAttackEvent lastAttackEvent;
        private bool waitingForReaction = false;

        private CombatEntity cachedPlayerEntity;
        private CombatEntity cachedEnemyEntity;
        private TurnBasedCombatPresenter presenter;

        private bool useNewCombatSystem = true;

        public PlayerAttackingState(CombatFlowStateMachine context) : base(context) 
        { 
            presenter = Object.FindObjectOfType<TurnBasedCombatPresenter>();
            if (presenter != null)
            {
                Debug.Log("[PlayerAttackingState] Found TurnBasedCombatPresenter - new system available");
            }
        }

        public override void OnEnter()
        {
            Debug.Log("[FSM] Entering PlayerAttackingState");
            attackComplete = false;
            waitingForReaction = false;
            
            EventBus<PlayerAttackEvent>.Subscribe(OnPlayerAttackEvent);
            
            if (useNewCombatSystem && presenter != null)
            {
                context.StartCoroutine(PerformAbilityBasedAttack());
            }
            else
            {
                context.StartCoroutine(PerformMultiHitAttack());
            }
        }

        public override void OnExit()
        {
            EventBus<PlayerAttackEvent>.Unsubscribe(OnPlayerAttackEvent);
        }

        public override void OnTick(float dt)
        {
            if (attackComplete)
            {
                Debug.Log("[FSM] All attacks complete, transitioning to EnemyReactingState");
                context.StateMachine.TryGo<EnemyReactingState>();
            }
        }

        private void OnPlayerAttackEvent(PlayerAttackEvent evt)
        {
            lastAttackEvent = evt;
            Debug.Log($"[PlayerAttackingState] Attack event received: {evt.hitNumber}/{evt.totalHits}, target: {evt.target.definition.displayName}, defeated: {evt.target.isDefeated}");
        }

        private IEnumerator PerformMultiHitAttack()
        {
            if (context.CurrentEnemyVisual == null)
            {
                Debug.Log("[PlayerAttackingState] EnemyVisual is null, attempting to find...");
                context.RefreshEnemyVisual();
            }

            if (context.PlayerVisual == null || context.CurrentEnemyVisual == null)
            {
                Debug.LogWarning("[PlayerAttackingState] Missing PlayerVisual or EnemyVisual, skipping attack");
                attackComplete = true;
                yield break;
            }

            context.CombatManager.InitializeAttackCombo();
            int totalHits = context.CombatManager.GetTotalHitsInCombo();
            Debug.Log($"[PlayerAttackingState] Starting multi-hit combo with {totalHits} hits");

            EnemyInstance lastTarget = null;

            while (true)
            {
                EnemyInstance currentTarget = context.CombatManager.CurrentEnemy;
                
                Debug.Log($"[PlayerAttackingState] Loop iteration - currentTarget: {(currentTarget != null ? currentTarget.definition.displayName : "NULL")}, lastTarget: {(lastTarget != null ? lastTarget.definition.displayName : "NULL")}");
                
                if (currentTarget == null)
                {
                    Debug.Log("[PlayerAttackingState] No current enemy, ending attack");
                    break;
                }

                if (currentTarget != lastTarget)
                {
                    Debug.Log($"[PlayerAttackingState] Target changed! Moving to enemy: {currentTarget.definition.displayName}");
                    context.RefreshEnemyVisual();

                    if (context.CurrentEnemyVisual == null)
                    {
                        Debug.LogWarning("[PlayerAttackingState] Could not find visual for current enemy");
                        break;
                    }

                    Vector3 attackPosition = context.CurrentEnemyVisual.transform.position + context.AttackPositionOffset;
                    float jumpDistance = Vector3.Distance(context.PlayerVisual.transform.position, attackPosition);
                    float jumpTime = jumpDistance / context.AttackMoveSpeed;

                    Debug.Log($"[PlayerAttackingState] Jumping to enemy at {context.CurrentEnemyVisual.transform.position} (attack pos: {attackPosition}) over {jumpTime} seconds");
                    
                    Sequence jumpSequence = DOTween.Sequence();
                    jumpSequence.Append(context.PlayerVisual.transform.DOMove(attackPosition, jumpTime).SetEase(Ease.OutQuad));
                    jumpSequence.Join(context.PlayerVisual.transform.DOLocalMoveY(context.PlayerVisual.transform.position.y + 1f, jumpTime * 0.5f).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo));
                    
                    yield return jumpSequence.WaitForCompletion();
                    
                    lastTarget = currentTarget;
                    Debug.Log($"[PlayerAttackingState] Movement complete, lastTarget updated to: {lastTarget.definition.displayName}");
                }

                Debug.Log($"[PlayerAttackingState] Performing hit {context.CombatManager.GetCurrentHitIndex() + 1}/{totalHits}");

                bool hasMoreHits = context.CombatManager.PerformNextHit();
                
                Debug.Log($"[PlayerAttackingState] PerformNextHit returned: {hasMoreHits}");

                yield return new WaitForSeconds(0.6f);

                if (!hasMoreHits)
                {
                    Debug.Log("[PlayerAttackingState] No more hits to perform");
                    break;
                }

                yield return new WaitForSeconds(0.2f);
            }

            attackComplete = true;
            Debug.Log("[PlayerAttackingState] Multi-hit attack sequence complete");
        }

        private IEnumerator PerformAbilityBasedAttack()
        {
            Debug.Log("[PlayerAttackingState] Starting ability-based attack");

            CombatAction action = GetActionForCurrentHand();
            if (action == null)
            {
                Debug.LogWarning("[PlayerAttackingState] No action found for current hand, falling back to old system");
                yield return PerformMultiHitAttack();
                yield break;
            }

            CombatEntity playerEntity = GetOrCreatePlayerEntity();
            CombatEntity enemyEntity = GetOrCreateEnemyEntity();

            if (playerEntity == null || enemyEntity == null)
            {
                Debug.LogError("[PlayerAttackingState] Failed to get combat entities, falling back to old system");
                yield return PerformMultiHitAttack();
                yield break;
            }

            Debug.Log($"[PlayerAttackingState] Executing action: {action.actionName} using TurnBasedCombatPresenter");
            
            if (action.timelineAsset != null)
            {
                Debug.Log($"[PlayerAttackingState] Action has Timeline sequence assigned - movement/animations will be driven by Timeline");
            }
            else
            {
                Debug.LogWarning($"[PlayerAttackingState] Action '{action.actionName}' has no Timeline sequence - no movement/animations will play");
            }

            presenter.ExecuteTurnBasedAbility(playerEntity, action, enemyEntity);

            yield return new WaitUntil(() => presenter.IsExecutionComplete);

            Debug.Log("[PlayerAttackingState] Action execution complete - Timeline handles all movement");
            attackComplete = true;
        }

        private CombatAction GetActionForCurrentHand()
        {
            if (!context.LastHandType.HasValue)
            {
                Debug.LogWarning("[PlayerAttackingState] No hand type available");
                return null;
            }

            AbilityLookup lookup = Resources.Load<AbilityLookup>("Combat/AbilityLookup");
            if (lookup == null)
            {
                Debug.LogError("[PlayerAttackingState] AbilityLookup not found in Resources folder. Expected at /Assets/RogueDeal/Resources/Combat/AbilityLookup.asset");
                return null;
            }

            CombatAction action = lookup.GetAction(context.LastHandType);
            
            if (action == null)
            {
                Debug.LogWarning($"[PlayerAttackingState] No action mapped for hand type: {context.LastHandType.Value}");
                return null;
            }

            Debug.Log($"[PlayerAttackingState] Using action '{action.actionName}' for hand '{context.LastHandType.Value}'");
            return action;
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
                
                HeroData playerHeroData = Resources.Load<HeroData>("PlayerHeroData");
                
                if (playerHeroData == null)
                {
                    Debug.LogError("[PlayerAttackingState] PlayerHeroData not found in Resources folder. Create one at /Assets/Resources/PlayerHeroData.asset");
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
    }
}
