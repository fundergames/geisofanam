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

        /// <summary>True while an action is being executed (movement, animation, effects).</summary>
        public bool IsExecuting => isExecuting;

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
            
            var targetResult = action.targetingStrategy.ResolveTargets(entityData);
            if (!targetResult.isReady || targetResult.targets == null || targetResult.targets.Count == 0)
            {
                string rangeInfo = action.targetingStrategy is SingleTargetSelector singleTarget 
                    ? $"Range: {singleTarget.maxRange}" 
                    : "Range: N/A";
                Debug.Log($"[CombatExecutor] Could not resolve targets for {action.actionName}. Position: {entityData.position}, {rangeInfo}");
                return false;
            }
            
            // Store action context
            currentAction = action;
            currentTargets = targetResult.targets;
            currentTargetPosition = targetResult.targetPosition;
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
                        
                        // For testing: If no animation events are set up, apply effects after a short delay
                        // This allows the animation to play while still applying effects
                        StartCoroutine(ApplyEffectsAfterDelay(action, 0.5f)); // Apply effects after 0.5 seconds
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
            
            foreach (var target in targetList)
            {
                if (target == null) continue;
                
                var targetData = target.GetEntityData();
                if (targetData == null || !targetData.IsAlive) continue;
                
                float hpBefore = targetData.currentHealth;
                bool wasCritical = false;
                float totalDamage = 0f;
                
                // Calculate and apply all effects
                foreach (var effect in effects)
                {
                    if (effect == null) continue;
                    
                    var calculated = effect.Calculate(entityData, targetData, entityData.equippedWeapon);
                    
                    // Track if any effect was a critical hit
                    if (calculated.wasCritical)
                    {
                        wasCritical = true;
                    }
                    
                    // Track total damage from this effect
                    if (calculated.damageAmount > 0)
                    {
                        totalDamage += calculated.damageAmount;
                    }
                    
                    effect.Apply(targetData, calculated);
                }
                
                float hpAfter = targetData.currentHealth;
                float damageDealt = hpBefore - hpAfter;
                
                if (damageDealt > 0)
                {
                    Debug.Log($"[CombatExecutor] Applied effects to {target.gameObject.name}. Damage: {damageDealt:F1}, HP: {hpBefore:F1} → {hpAfter:F1}");
                    
                    // Fire damage event (drives damage numbers, health bars, visual feedback)
                    CombatEvents.TriggerDamageApplied(new CombatEventData
                    {
                        source = combatEntity,
                        target = target,
                        damageAmount = damageDealt,
                        wasCritical = wasCritical,
                        hitPosition = target.GetHitPoint()
                    });
                    
                    // Trigger hit reaction: animation + damage popup
                    TriggerHitReaction(target, damageDealt, wasCritical);
                }
            }
        }

        /// <summary>
        /// Applies a single effect to all current targets
        /// </summary>
        private void ApplyEffectToTargets(BaseEffect effect)
        {
            if (effect == null || currentTargets == null) return;
            
            foreach (var target in currentTargets)
            {
                if (target == null) continue;
                
                var targetData = target.GetEntityData();
                if (targetData == null || !targetData.IsAlive) continue;
                
                float hpBefore = targetData.currentHealth;
                var calculated = effect.Calculate(entityData, targetData, entityData.equippedWeapon);
                effect.Apply(targetData, calculated);
                
                float hpAfter = targetData.currentHealth;
                float damageDealt = hpBefore - hpAfter;
                
                if (damageDealt > 0)
                {
                    // Fire damage event (drives damage numbers, health bars, visual feedback)
                    CombatEvents.TriggerDamageApplied(new CombatEventData
                    {
                        source = combatEntity,
                        target = target,
                        damageAmount = damageDealt,
                        wasCritical = calculated.wasCritical,
                        hitPosition = target.GetHitPoint()
                    });
                    
                    // Trigger hit reaction: animation + damage popup
                    TriggerHitReaction(target, damageDealt, calculated.wasCritical);
                }
            }
        }
        
        /// <summary>
        /// Triggers hit reaction animation on the target.
        /// Visual feedback (damage numbers, health bars) is handled by CombatEvents.OnDamageApplied subscribers.
        /// </summary>
        public void TriggerHitReaction(CombatEntity target, float damageDealt, bool isCritical)
        {
            if (target == null) return;
            
            // Play hit reaction animation
            CombatAnimationController animController = target.GetComponent<CombatAnimationController>();
            if (animController != null)
            {
                animController.PlayHitReaction(EffectType.Damage);
            }
            else if (target.animator != null)
            {
                target.animator.SetTrigger(target.hitTrigger);
            }
            
            // Fire hit reaction event (for VFX/SFX - damage visuals come from OnDamageApplied)
            CombatEvents.TriggerHitReactionStarted(new CombatEventData
            {
                source = combatEntity,
                target = target,
                damageAmount = damageDealt,
                wasCritical = isCritical,
                hitPosition = target.GetHitPoint(),
                effect = null
            });
            
            // Fallback: if target has no EnemyVisual/PlayerVisual to receive OnDamageApplied, show popup directly
            bool hasVisualFeedback = target.GetComponent<EnemyVisual>() != null || target.GetComponentInParent<EnemyVisual>() != null
                || target.GetComponent<PlayerVisual>() != null || target.GetComponentInParent<PlayerVisual>() != null;
            if (!hasVisualFeedback && DamagePopupManager.Instance != null)
            {
                Vector3 hitPosition = target.hitPoint != null ? target.hitPoint.position : target.transform.position + Vector3.up;
                DamagePopupManager.Instance.ShowDamagePopup(Mathf.RoundToInt(damageDealt), isCritical, hitPosition);
            }
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
        /// Clears the current action (called when attack completes)
        /// </summary>
        public void ClearCurrentAction()
        {
            currentAction = null;
            currentTargets = null;
            isExecuting = false;
        }
        
        // Getters for other components
        public List<CombatEntity> GetCurrentTargets() => currentTargets;
        public Vector3 GetTargetPosition() => currentTargetPosition;
        public CombatEntityData GetEntityData() => entityData;
        public ActionCooldownManager GetCooldownManager() => cooldownManager;
        
        /// <summary>
        /// Called at the start of each turn (for turn-based combat)
        /// </summary>
        public void OnTurnStart()
        {
            entityData.OnTurnStart();
            cooldownManager.OnTurnStart();
        }
        
        /// <summary>
        /// Checks animation state after triggering (for debugging)
        /// </summary>
    }
}


