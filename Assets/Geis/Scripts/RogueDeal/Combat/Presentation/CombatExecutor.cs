/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Geis.Animation;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;
using RogueDeal.Combat.Core.Cooldowns;
using RogueDeal.Combat.Core.Targeting;
using RogueDeal.Combat;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Manages action execution. Handles targeting, movement, animations, and effect application.
    /// </summary>
    public partial class CombatExecutor : MonoBehaviour
    {
        private CombatEntity combatEntity;
        private CombatEntityData entityData;
        private ActionCooldownManager cooldownManager;
        private Animator animator;
        private PlayableDirector timelineDirector;
        private TimelineRootMotionController rootMotionController;
        
        // Current action context
        private CombatAction currentAction;
        private List<CombatEntity> currentTargets;
        private Vector3 currentTargetPosition;
        private int currentComboHit = 0;
        private bool isExecuting = false;
        private List<CombatEntity> forcedTargets;
        private bool lockForcedTargetsAtStrikeTime;

        /// <summary>True while an action is being executed (movement, animation, effects).</summary>
        public bool IsExecuting => isExecuting;

        [Header("Fallback strike timing")]
        [Tooltip("When a CombatAction's Damage Apply Delay Seconds is negative (use default), wait this long after the attack trigger before applying damage and defender hit reaction, if animation events are not used.")]
        [SerializeField] private float defaultDamageApplyDelaySeconds = 0.18f;

        // Movement
        private Vector3 originalPosition;
        private bool needsToMove = false;
#pragma warning disable CS0414
        private bool isMoving = false;
#pragma warning restore CS0414
        
        private void Awake()
        {
            combatEntity = GetComponent<CombatEntity>();
            
            // Get animator from CombatEntity (it searches children too)
            animator = combatEntity.animator;
            
            // Fallback: try to find animator ourselves
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            
            originalPosition = transform.position;
            
            // Get or create entity data
            entityData = combatEntity.GetEntityData();
            entityData.position = transform.position;
            entityData.originPosition = transform.position;
            
            // Create cooldown manager
            cooldownManager = new ActionCooldownManager(entityData);
            
            // Debug animator status
            if (animator == null)
            {
                Debug.LogWarning($"[CombatExecutor] No Animator found on {gameObject.name} or children");
            }
            else if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"[CombatExecutor] Animator on {animator.gameObject.name} has no controller assigned");
            }
            else
            {
                Debug.Log($"[CombatExecutor] Animator found on {animator.gameObject.name} with controller: {animator.runtimeAnimatorController.name}");
            }
            
            // Get or create PlayableDirector for Timeline support
            timelineDirector = GetComponent<PlayableDirector>();
            if (timelineDirector == null)
            {
                timelineDirector = gameObject.AddComponent<PlayableDirector>();
                timelineDirector.playOnAwake = false;
                Debug.Log($"[CombatExecutor] Added PlayableDirector for Timeline support");
            }
            
            // Get or create TimelineRootMotionController for root motion tracking
            rootMotionController = GetComponent<TimelineRootMotionController>();
            if (rootMotionController == null)
            {
                rootMotionController = gameObject.AddComponent<TimelineRootMotionController>();
                Debug.Log($"[CombatExecutor] Added TimelineRootMotionController for root motion tracking");
            }
        }
        
        private void Update()
        {
            // Update time-based cooldowns
            cooldownManager.Update(Time.deltaTime);
            
            // Sync position with transform (root motion controller handles the actual position)
            if (entityData != null)
            {
                entityData.position = transform.position;
            }
        }
        
        /// <summary>
        /// Executes a combat action
        /// </summary>
        public bool ExecuteAction(CombatAction action)
        {
            if (action == null)
            {
                Debug.LogWarning("[CombatExecutor] Cannot execute null action");
                return false;
            }
            
            Debug.Log($"[CombatExecutor] Executing action: {action.actionName} (isCombo: {action.isCombo}, hasTrigger: {!string.IsNullOrEmpty(action.animationTrigger)}, hasEffects: {action.effects != null && action.effects.Length > 0})");
            
            // Validate action configuration
            if (action.effects == null || action.effects.Length == 0)
            {
                Debug.LogWarning($"[CombatExecutor] Action '{action.actionName}' has no effects! Cannot execute.");
                return false;
            }
            
            if (action.targetingStrategy == null)
            {
                Debug.LogWarning($"[CombatExecutor] Action '{action.actionName}' has no targeting strategy! Cannot execute.");
                return false;
            }
            
            if (isExecuting)
            {
                Debug.LogWarning("[CombatExecutor] Already executing an action");
                return false;
            }
            
            // Check cooldown
            if (!cooldownManager.IsActionAvailable(action))
            {
                Debug.Log($"[CombatExecutor] Action {action.actionName} is on cooldown");
                return false;
            }
            
            // Sync position before targeting
            entityData.position = transform.position;
            
            // Resolve targets
            if (action.targetingStrategy == null)
            {
                Debug.LogWarning($"[CombatExecutor] Action {action.actionName} has no targeting strategy");
                return false;
            }
            
            if (!TryResolveActionTargets(action, out List<CombatEntity> resolvedTargets, out Vector3 resolvedTargetPosition))
            {
                string rangeInfo = action.targetingStrategy is SingleTargetSelector singleTarget 
                    ? $"Range: {singleTarget.maxRange}" 
                    : "Range: N/A";
                Debug.Log($"[CombatExecutor] Could not resolve targets for {action.actionName}. Position: {entityData.position}, {rangeInfo}");
                return false;
            }
            
            // Store action context
            currentAction = action;
            currentTargets = resolvedTargets;
            currentTargetPosition = resolvedTargetPosition;
            currentComboHit = 0;
            isExecuting = true;
            
            // Check if movement needed
            if (entityData.combatProfile != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTargetPosition);
                if (distanceToTarget > entityData.combatProfile.engagementDistance)
                {
                    needsToMove = true;
                    originalPosition = transform.position;
                    entityData.originPosition = originalPosition;
                }
            }
            
            // Start cooldown
            cooldownManager.StartCooldown(action);
            
            // Trigger animation
            // Priority: Timeline > Combo Animations > Animation Trigger
            
            // Check if action has Timeline (preferred for combos)
            if (action.timelineAsset != null)
            {
                StartTimelineCombo(action);
            }
            // Check if this is a combo with valid combo animations
            else if (action.isCombo && 
                     action.comboAnimations != null && 
                     action.comboAnimations.Length > 0 &&
                     action.comboAnimations[0] != null)
            {
                StartCombo(action);
            }
            else if (!string.IsNullOrEmpty(action.animationTrigger))
            {
                if (animator == null)
                {
                    Debug.LogWarning($"[CombatExecutor] Animator is null! Cannot play animation '{action.animationTrigger}'");
                    ApplyEffectsToTargets(action.effects);
                    CompleteAction();
                }
                else if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning($"[CombatExecutor] Animator has no controller assigned! Cannot play animation '{action.animationTrigger}'");
                    ApplyEffectsToTargets(action.effects);
                    CompleteAction();
                }
                else
                {
                    if (!AnimatorParameterGuard.HasTrigger(animator, action.animationTrigger))
                    {
                        Debug.LogWarning($"[CombatExecutor] Animator controller '{animator.runtimeAnimatorController.name}' does not have trigger parameter '{action.animationTrigger}'. Available parameters: {AnimatorParameterGuard.FormatParameterList(animator)}");
                        ApplyEffectsToTargets(action.effects);
                        CompleteAction();
                    }
                    else
                    {
                        // Check current state before trigger
                        var stateBefore = animator.GetCurrentAnimatorStateInfo(0);
                        Debug.Log($"[CombatExecutor] Before trigger - State: {GetStateName(animator)}, NormalizedTime: {stateBefore.normalizedTime:F2}");
                        
                        animator.SetTrigger(action.animationTrigger);
                        Debug.Log($"[CombatExecutor] ✓ Triggered animation: {action.animationTrigger} on {animator.gameObject.name}");
                        
                        // Wait a frame for transition to start
                        StartCoroutine(CheckAnimationState(action));
                        
                        // When no animation events apply hits, damage + hit reaction share this delay (tune per CombatAction or executor default).
                        ScheduleStrikeEffectsAfterAnimationFallback(action);
                    }
                }
            }
            else
            {
                // No animation trigger - apply effects immediately
                ApplyEffectsToTargets(action.effects);
                CompleteAction();
            }
            
            return true;
        }

        /// <summary>
        /// Like <see cref="ExecuteAction"/> but applies main or per-hit effects at absolute times (seconds from attack start)
        /// instead of a single damage-apply delay. Used when <see cref="Geis.Combat.GeisComboData"/> defines multi-hit contact
        /// times for enemies (same data as the player <c>SimpleAttackHitDetector</c> path).
        /// Does not support timeline or legacy combo clip arrays; returns false so callers can fall back to <see cref="ExecuteAction"/>.
        /// </summary>
        public bool ExecuteActionWithScheduledEffectTimes(CombatAction action, float[] effectApplyTimesSecondsFromAttackStart)
        {
            if (effectApplyTimesSecondsFromAttackStart == null || effectApplyTimesSecondsFromAttackStart.Length == 0)
            {
                Debug.LogWarning("[CombatExecutor] ExecuteActionWithScheduledEffectTimes: times array is null or empty.");
                return false;
            }

            if (action == null)
            {
                Debug.LogWarning("[CombatExecutor] ExecuteActionWithScheduledEffectTimes: action is null.");
                return false;
            }

            if (action.effects == null || action.effects.Length == 0)
            {
                Debug.LogWarning($"[CombatExecutor] ExecuteActionWithScheduledEffectTimes: action '{action.actionName}' has no effects.");
                return false;
            }

            if (action.targetingStrategy == null)
            {
                Debug.LogWarning($"[CombatExecutor] ExecuteActionWithScheduledEffectTimes: action '{action.actionName}' has no targeting strategy.");
                return false;
            }

            if (action.timelineAsset != null)
            {
                Debug.LogWarning($"[CombatExecutor] ExecuteActionWithScheduledEffectTimes: action '{action.actionName}' uses a timeline; use ExecuteAction instead.");
                return false;
            }

            if (action.isCombo && action.comboAnimations != null && action.comboAnimations.Length > 0 && action.comboAnimations[0] != null)
            {
                Debug.LogWarning($"[CombatExecutor] ExecuteActionWithScheduledEffectTimes: action '{action.actionName}' uses combo clip arrays; use ExecuteAction instead.");
                return false;
            }

            if (isExecuting)
            {
                Debug.LogWarning("[CombatExecutor] Already executing an action");
                return false;
            }

            if (!cooldownManager.IsActionAvailable(action))
            {
                Debug.Log($"[CombatExecutor] Action {action.actionName} is on cooldown");
                return false;
            }

            entityData.position = transform.position;

            if (!TryResolveActionTargets(action, out List<CombatEntity> resolvedTargets, out Vector3 resolvedTargetPosition))
            {
                Debug.Log($"[CombatExecutor] ExecuteActionWithScheduledEffectTimes: could not resolve targets for {action.actionName}.");
                return false;
            }

            currentAction = action;
            currentTargets = resolvedTargets;
            currentTargetPosition = resolvedTargetPosition;
            currentComboHit = 0;
            isExecuting = true;

            if (entityData.combatProfile != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTargetPosition);
                if (distanceToTarget > entityData.combatProfile.engagementDistance)
                {
                    needsToMove = true;
                    originalPosition = transform.position;
                    entityData.originPosition = originalPosition;
                }
            }

            cooldownManager.StartCooldown(action);

            bool animTriggerReady = animator != null
                && animator.runtimeAnimatorController != null
                && !string.IsNullOrEmpty(action.animationTrigger)
                && AnimatorParameterGuard.HasTrigger(animator, action.animationTrigger);

            if (animTriggerReady)
            {
                animator.SetTrigger(action.animationTrigger);
                StartCoroutine(CheckAnimationState(action));
            }
            else if (!string.IsNullOrEmpty(action.animationTrigger))
            {
                Debug.LogWarning(
                    $"[CombatExecutor] ExecuteActionWithScheduledEffectTimes: animator cannot play trigger '{action.animationTrigger}'. Scheduled hits still run.");
            }

            StartCoroutine(ApplyEffectsAtScheduledSecondsFromAttackStart(action, effectApplyTimesSecondsFromAttackStart));
            return true;
        }

        /// <summary>
        /// Called when a combo hit connects (from animation event or Timeline signal)
        /// </summary>
        public void OnComboHit()
        {
            if (currentAction == null || !currentAction.isCombo) return;
            
            currentComboHit++;
            
            // Apply per-hit effects if any
            if (currentAction.perHitEffects != null && 
                currentComboHit <= currentAction.perHitEffects.Length)
            {
                var effect = currentAction.perHitEffects[currentComboHit - 1];
                ApplyEffectToTargets(effect);
            }
            else
            {
                // Use main effects
                ApplyEffectsToTargets(currentAction.effects);
            }
            
            // Check if combo complete
            if (currentComboHit >= currentAction.comboHitCount)
            {
                // Don't complete if using Timeline (let Timeline finish)
                if (currentAction.timelineAsset == null)
                {
                    CompleteAction();
                }
            }
        }
        
        /// <summary>
        /// Called from Timeline signal to apply effects at a specific time
        /// </summary>
        public void OnTimelineApplyEffects()
        {
            if (currentAction != null && currentTargets != null)
            {
                Debug.Log("[CombatExecutor] Applying effects from Timeline signal");
                ApplyEffectsToTargets(currentAction.effects);
            }
        }
        
        /// <summary>
        /// Called from Timeline signal to apply per-hit effects
        /// </summary>
        public void OnTimelineComboHit(int hitNumber)
        {
            if (currentAction == null || !currentAction.isCombo) return;
            
            currentComboHit = hitNumber;
            
            if (currentAction.perHitEffects != null && 
                hitNumber > 0 && hitNumber <= currentAction.perHitEffects.Length)
            {
                var effect = currentAction.perHitEffects[hitNumber - 1];
                ApplyEffectToTargets(effect);
            }
            else
            {
                ApplyEffectsToTargets(currentAction.effects);
            }
        }
        
        /// <summary>
        /// Applies an action's effects to a list of targets (e.g. from OverlapSphere hit detection).
        /// Use this for simple, non-collider-based attack detection.
        /// </summary>
        public void ApplyActionToTargets(CombatAction action, List<CombatEntity> targets)
        {
            if (action == null || targets == null || action.effects == null || action.effects.Length == 0)
                return;
            ApplyEffectsToTargetList(action.effects, targets);
        }

        /// <summary>
        /// Applies effects for one strike of a multi-hit action (matches per-hit / main effects rules used by <see cref="OnTimelineComboHit"/>).
        /// </summary>
        /// <param name="hitNumber">1-based hit index.</param>
        public void ApplyActionToTargets(CombatAction action, List<CombatEntity> targets, int hitNumber)
        {
            if (action == null || targets == null) return;

            if (action.perHitEffects != null && hitNumber > 0 && hitNumber <= action.perHitEffects.Length)
            {
                var effect = action.perHitEffects[hitNumber - 1];
                if (effect != null)
                {
                    ApplyEffectsToTargetList(new[] { effect }, targets);
                    return;
                }
            }

            if (action.effects == null || action.effects.Length == 0) return;
            ApplyEffectsToTargetList(action.effects, targets);
        }

        /// <summary>
        /// Applies effects to all current targets
        /// </summary>
        public void ApplyEffectsToTargets(BaseEffect[] effects)
        {
            if (effects == null || currentTargets == null) return;
            ApplyEffectsToTargetList(effects, currentTargets);
        }

        private void ApplyEffectsToTargetList(BaseEffect[] effects, List<CombatEntity> targetList)
        {
            if (effects == null || targetList == null) return;

            CombatStrikeKind kind = currentAction != null
                ? CombatStrikeResolver.ResolveStrikeKind(currentAction, CombatStrikeKind.Melee)
                : CombatStrikeKind.Melee;
            float maxRange = CombatStrikeResolver.GetMaxMeleeRange(entityData);

            foreach (var target in targetList)
            {
                if (target == null) continue;

                float damageDealt = CombatStrikeResolver.TryApplyEffectsToTarget(
                    kind,
                    combatEntity,
                    target,
                    effects,
                    currentAction,
                    kind == CombatStrikeKind.Melee ? maxRange : -1f,
                    1f,
                    TriggerHitReaction);

                if (damageDealt > 0f)
                {
                    Debug.Log($"[CombatExecutor] Applied effects to {target.gameObject.name}. Damage: {damageDealt:F1}");
                }
            }
        }

        /// <summary>
        /// Re-resolves targets from the current action's strategy at strike time (position, LOS, range).
        /// </summary>
        private bool TryRefreshTargetsAtStrikeTime()
        {
            if (TryUseForcedTargets(out List<CombatEntity> resolvedTargets, out Vector3 targetPosition))
            {
                currentTargets = resolvedTargets;
                currentTargetPosition = targetPosition;
                return true;
            }

            if (currentAction == null || currentAction.targetingStrategy == null || entityData == null)
                return currentTargets != null && currentTargets.Count > 0;

            entityData.position = transform.position;
            var result = currentAction.targetingStrategy.ResolveTargets(entityData);
            if (!result.isReady || result.targets == null || result.targets.Count == 0)
                return false;

            currentTargets = result.targets;
            currentTargetPosition = result.targetPosition;
            return true;
        }

        /// <summary>
        /// Applies a single effect to all current targets
        /// </summary>
        private void ApplyEffectToTargets(BaseEffect effect)
        {
            if (effect == null || currentTargets == null) return;

            ApplyEffectsToTargetList(new[] { effect }, currentTargets);
        }
        
        /// <summary>
        /// Triggers hit reaction animation on the target.
        /// Visual feedback (damage numbers, health bars) is handled by CombatEvents.OnDamageApplied subscribers.
        /// </summary>
        public void TriggerHitReaction(CombatEntity target, float damageDealt, bool isCritical)
        {
            if (target == null) return;

            Vector3 hitPosition = target.GetHitPoint();
            CombatHitDirection hitDirection = CombatHitDirectionUtility.Resolve(combatEntity, target);
            var eventData = new CombatEventData
            {
                source = combatEntity,
                target = target,
                damageAmount = damageDealt,
                wasCritical = isCritical,
                hitPosition = hitPosition,
                hitDirection = hitDirection,
                effect = null
            };

            if (target.GetComponent<ICombatHitReactionPresenter>() == null)
            {
                CombatAnimationController animController = target.GetComponent<CombatAnimationController>();
                if (animController != null)
                {
                    animController.PlayHitReaction(EffectType.Damage, hitDirection);
                }
                else if (target.animator != null)
                {
                    if (AnimatorParameterGuard.HasParameterOfType(target.animator, "HitDirection", AnimatorControllerParameterType.Int))
                        target.animator.SetInteger("HitDirection", CombatHitDirectionUtility.ToAnimatorInt(hitDirection));

                    if (!AnimatorParameterGuard.TrySetTrigger(target.animator, target.hitTrigger))
                    {
                        Debug.LogWarning(
                            $"[CombatExecutor] Animator on '{target.animator.gameObject.name}' has no trigger '{target.hitTrigger}'. Available: {AnimatorParameterGuard.FormatParameterList(target.animator)}");
                    }
                }
            }

            CombatEvents.TriggerHitReactionStarted(eventData);
            
            // Fallback: if target has no EnemyVisual/PlayerVisual to receive OnDamageApplied, show popup directly
            bool hasVisualFeedback = target.GetComponent<EnemyVisual>() != null || target.GetComponentInParent<EnemyVisual>() != null
                || target.GetComponent<PlayerVisual>() != null || target.GetComponentInParent<PlayerVisual>() != null;
            if (!hasVisualFeedback && DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.ShowDamagePopup(Mathf.RoundToInt(damageDealt), isCritical, hitPosition);
        }
        
        /// <summary>
        /// Completes the current action
        /// </summary>
        public void CompleteAction()
        {
            currentAction = null;
            currentTargets = null;
            currentComboHit = 0;
            isExecuting = false;
            needsToMove = false;
            ClearForcedTargets();
        }
        
        /// <summary>
        /// Moves toward target (called from animation event)
        /// </summary>
        public void MoveToTarget()
        {
            if (!needsToMove || currentTargetPosition == Vector3.zero) return;
            
            isMoving = true;
            // Movement will be handled by animation root motion or a separate movement component
            // For now, we just set a flag
        }
        
        /// <summary>
        /// Returns to original position (called from animation event)
        /// </summary>
        public void ReturnToOrigin()
        {
            if (needsToMove && entityData.combatProfile != null && entityData.combatProfile.returnToOriginAfterAttack)
            {
                transform.position = originalPosition;
                entityData.position = originalPosition;
            }
            isMoving = false;
        }
        
        // Getters for other components
        public CombatAction GetCurrentAction() => currentAction;
        public int GetCurrentComboHit() => currentComboHit;
        
        /// <summary>
        /// Sets the current action for weapon collider-based combat.
        /// This allows WeaponHitbox to access the action without executing it with targeting.
        /// </summary>
        public void SetCurrentAction(CombatAction action)
        {
            if (action == null)
            {
                Debug.LogWarning("[CombatExecutor] Cannot set null action");
                return;
            }
            
            // Check cooldown
            if (!cooldownManager.IsActionAvailable(action))
            {
                Debug.Log($"[CombatExecutor] Action {action.actionName} is on cooldown, cannot set as current");
                return;
            }
            
            currentAction = action;
            
            // Start cooldown
            cooldownManager.StartCooldown(action);
            
            Debug.Log($"[CombatExecutor] Set current action for weapon collider: {action.actionName}");
        }

        /// <summary>
        /// Forces the next execution path to use these targets instead of the action's targeting strategy.
        /// Useful for AI that already picked a specific target and should not retarget at strike time.
        /// </summary>
        public void SetForcedTargets(List<CombatEntity> targets, bool lockAtStrikeTime = true)
        {
            forcedTargets = SanitizeTargets(targets);
            lockForcedTargetsAtStrikeTime = lockAtStrikeTime;
        }

        public void ClearForcedTargets()
        {
            forcedTargets = null;
            lockForcedTargetsAtStrikeTime = false;
        }
        
        /// <summary>
        /// Clears the current action (called when attack completes)
        /// </summary>
        public void ClearCurrentAction()
        {
            currentAction = null;
            currentTargets = null;
            isExecuting = false;
            ClearForcedTargets();
        }

        /// <summary>Stops strike coroutines and clears the active action (e.g. when hit during a swing).</summary>
        public void InterruptCurrentAction()
        {
            StopAllCoroutines();
            ClearCurrentAction();
        }
        
        // Getters for other components
        public List<CombatEntity> GetCurrentTargets() => currentTargets;
        public Vector3 GetTargetPosition() => currentTargetPosition;
        public CombatEntityData GetEntityData() => entityData;
        public ActionCooldownManager GetCooldownManager() => cooldownManager;

        private bool TryResolveActionTargets(CombatAction action, out List<CombatEntity> targets, out Vector3 targetPosition)
        {
            if (TryUseForcedTargets(out targets, out targetPosition))
                return true;

            var targetResult = action.targetingStrategy.ResolveTargets(entityData);
            if (!targetResult.isReady || targetResult.targets == null || targetResult.targets.Count == 0)
            {
                targets = null;
                targetPosition = entityData != null ? entityData.position : transform.position;
                return false;
            }

            targets = targetResult.targets;
            targetPosition = targetResult.targetPosition;
            return true;
        }

        private bool TryUseForcedTargets(out List<CombatEntity> targets, out Vector3 targetPosition)
        {
            targets = null;
            targetPosition = entityData != null ? entityData.position : transform.position;

            List<CombatEntity> sanitized = SanitizeTargets(forcedTargets);
            if (sanitized == null || sanitized.Count == 0)
                return false;

            if (!lockForcedTargetsAtStrikeTime)
                forcedTargets = sanitized;

            targets = sanitized;
            targetPosition = sanitized[0].transform.position;
            return true;
        }

        private static List<CombatEntity> SanitizeTargets(List<CombatEntity> targets)
        {
            if (targets == null || targets.Count == 0)
                return null;

            var result = new List<CombatEntity>(targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                CombatEntity target = targets[i];
                if (target == null)
                    continue;

                CombatEntityData data = target.GetEntityData();
                if (data == null || !data.IsAlive || !target.gameObject.activeInHierarchy)
                    continue;

                if (!result.Contains(target))
                    result.Add(target);
            }

            return result.Count > 0 ? result : null;
        }
        
        /// <summary>
        /// Called at the start of each turn (for turn-based combat)
        /// </summary>
        public void OnTurnStart()
        {
            entityData.OnTurnStart();
            cooldownManager.OnTurnStart();
        }
    }
}


