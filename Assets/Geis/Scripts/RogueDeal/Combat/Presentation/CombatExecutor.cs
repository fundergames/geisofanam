using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public class CombatExecutor : MonoBehaviour
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
                    // Check if parameter exists
                    bool hasParameter = false;
                    string availableParams = "";
                    foreach (AnimatorControllerParameter param in animator.parameters)
                    {
                        availableParams += $"{param.name} ({param.type}), ";
                        if (param.name == action.animationTrigger && param.type == AnimatorControllerParameterType.Trigger)
                        {
                            hasParameter = true;
                        }
                    }
                    
                    if (!hasParameter)
                    {
                        Debug.LogWarning($"[CombatExecutor] Animator controller '{animator.runtimeAnimatorController.name}' does not have trigger parameter '{action.animationTrigger}'. Available parameters: {availableParams}");
                        ApplyEffectsToTargets(action.effects);
                        CompleteAction();
                    }
                    else
                    {
                        // Check current state before trigger
                        var stateBefore = animator.GetCurrentAnimatorStateInfo(0);
                        Debug.Log($"[CombatExecutor] Before trigger - State: {GetStateName(animator)}, NormalizedTime: {stateBefore.normalizedTime:F2}");
                        
                        // Set the trigger
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
        /// Starts a combo attack using Timeline
        /// </summary>
        private void StartTimelineCombo(CombatAction action)
        {
            currentComboHit = 0;
            
            if (timelineDirector == null)
            {
                Debug.LogWarning("[CombatExecutor] Cannot start Timeline combo - PlayableDirector is null");
                ApplyEffectsToTargets(action.effects);
                CompleteAction();
                return;
            }
            
            if (action.timelineAsset == null)
            {
                Debug.LogWarning("[CombatExecutor] Cannot start Timeline combo - TimelineAsset is null");
                ApplyEffectsToTargets(action.effects);
                CompleteAction();
                return;
            }
            
            Debug.Log($"[CombatExecutor] Starting Timeline combo: {action.timelineAsset.name}");
            
            // Rotate character so animation forward (Z) points in the desired direction
            // Animations control movement direction directly - we just orient the character
            // Two scenarios:
            // 1. Combat with targets: Rotate so animation forward (Z) points at target
            // 2. Open-world/player-controlled: Use character's current facing direction (no rotation needed)
            
            bool shouldRotateToTarget = currentTargets != null && currentTargets.Count > 0 && currentTargetPosition != Vector3.zero;
            
            if (shouldRotateToTarget)
            {
                Vector3 directionToTarget = (currentTargetPosition - transform.position);
                directionToTarget.y = 0; // Keep rotation horizontal
                
                if (directionToTarget.magnitude > 0.01f)
                {
                    directionToTarget.Normalize();
                    
                    // Rotate character so animation forward (Z) points at target
                    // This ensures the animation's root motion moves toward the target
                    // LookRotation makes transform.forward (Z) point at the target
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = targetRotation;
                    
                    Debug.Log($"[CombatExecutor] Rotated character so animation forward (Z) points at target.");
                    Debug.Log($"[CombatExecutor]   Target position: {currentTargetPosition}, Character position: {transform.position}");
                    Debug.Log($"[CombatExecutor]   Target direction: {directionToTarget}");
                    Debug.Log($"[CombatExecutor]   Rotation applied: {targetRotation.eulerAngles}");
                }
            }
            else
            {
                // Open-world/player-controlled - use character's current facing direction
                // Animation will move in its forward direction (Z), which should match where player is facing
                Debug.Log($"[CombatExecutor] Using character's current facing direction for dash (open-world/player-controlled mode).");
                Debug.Log($"[CombatExecutor]   Character position: {transform.position}");
                Debug.Log($"[CombatExecutor]   Character rotation: {transform.rotation.eulerAngles}");
            }
            
            // Set up Timeline
            timelineDirector.playableAsset = action.timelineAsset;
            
            // Store starting position for root motion continuity
            Vector3 timelineStartPosition = transform.position;
            
            // Enable root motion on animator
            if (animator != null)
            {
                animator.applyRootMotion = true;
            }
            
            // Bind tracks to actual GameObjects
            foreach (var output in action.timelineAsset.outputs)
            {
                string trackName = output.streamName;
                
                // Bind Attacker/Character track to this entity's animator
                if (trackName.Contains("Attacker") || trackName.Contains("Character") || trackName.Contains("Player"))
                {
                    if (animator != null)
                    {
                        timelineDirector.SetGenericBinding(output.sourceObject, animator.gameObject);
                        Debug.Log($"[CombatExecutor] Bound '{trackName}' to {animator.gameObject.name}");
                        
                        // Try to disable Timeline's position control on the Animation Track
                        // This prevents Timeline from resetting position between clips
                        var animationTrack = output.sourceObject as UnityEngine.Timeline.AnimationTrack;
                        if (animationTrack != null)
                        {
                            // CRITICAL: Timeline's Animation Track applies position in LOCAL SPACE
                            // but root motion is in WORLD SPACE. This mismatch causes resets.
                            // We can't disable it programmatically, but we'll override in LateUpdate.
                            // USER MUST: In Timeline asset, select Animation Track → Inspector → 
                            // "Apply Transform Offsets" → UNCHECK "Position"
                            Debug.Log($"[CombatExecutor] Animation Track found - position will be manually controlled. " +
                                     $"IMPORTANT: In Timeline asset, disable 'Apply Transform Offsets' → 'Position' on the Animation Track!");
                        }
                    }
                }
                // Bind Target track to first target
                else if (trackName.Contains("Target") && currentTargets != null && currentTargets.Count > 0)
                {
                    var targetAnimator = currentTargets[0].GetComponentInChildren<Animator>();
                    if (targetAnimator != null)
                    {
                        timelineDirector.SetGenericBinding(output.sourceObject, targetAnimator.gameObject);
                        Debug.Log($"[CombatExecutor] Bound '{trackName}' to {targetAnimator.gameObject.name}");
                    }
                }
                // Bind Signal track to this executor (for Timeline signals/events)
                else if (output.outputTargetType == typeof(PlayableDirector))
                {
                    timelineDirector.SetGenericBinding(output.sourceObject, timelineDirector);
                }
            }
            
            // Enable root motion on animator for Timeline
            if (animator != null)
            {
                // CRITICAL: Enable root motion BEFORE Timeline plays
                animator.applyRootMotion = true;
                
                // Verify root motion is actually enabled
                if (!animator.applyRootMotion)
                {
                    Debug.LogError($"[CombatExecutor] CRITICAL: Failed to enable root motion! This will prevent animations from moving the character.");
                }
                
                // CRITICAL: Disable Timeline's position control by setting the Animation Track's position offset mode
                // We'll handle position entirely through root motion
                // Also try to disable any position control at the Animator level
                Debug.Log($"[CombatExecutor] Animator root motion enabled: {animator.applyRootMotion}");
                Debug.Log($"[CombatExecutor] Position will be controlled by root motion, not Timeline offsets.");
                Debug.Log($"[CombatExecutor] CRITICAL SETUP STEPS:");
                Debug.Log($"[CombatExecutor]   1. Timeline Animation Track → Inspector → 'Apply Transform Offsets' → UNCHECK 'Position'");
                Debug.Log($"[CombatExecutor]   2. Timeline Animation Track → Inspector → 'Track Offsets' → Set to 'None' or 'Apply Scene Offsets'");
                Debug.Log($"[CombatExecutor]   3. Each Animation Clip → Inspector → 'Clip Transform Offsets' → Position should be (0,0,0)");
                Debug.Log($"[CombatExecutor]   4. Animation Clip Import → 'Root Transform Position (XZ)' → MUST be 'Root Transform Position (XZ)' (NOT 'Bake Into Pose')");
            }
            
            // Subscribe to Timeline stopped event
            timelineDirector.stopped += OnTimelineStopped;
            
            // Start root motion tracking BEFORE playing Timeline
            if (rootMotionController != null)
            {
                rootMotionController.StartTracking(timelineDirector);
            }
            
            // Check Timeline duration and clips
            if (action.timelineAsset != null)
            {
                int clipCount = 0;
                foreach (var output in action.timelineAsset.outputs)
                {
                    var animationTrack = output.sourceObject as UnityEngine.Timeline.AnimationTrack;
                    if (animationTrack != null)
                    {
                        foreach (var clip in animationTrack.GetClips())
                        {
                            clipCount++;
                            Debug.Log($"[CombatExecutor] Timeline clip {clipCount}: {clip.displayName}, Duration: {clip.duration}s, Start: {clip.start}s");
                        }
                    }
                }
                Debug.Log($"[CombatExecutor] Timeline duration: {action.timelineAsset.duration} seconds, Total clips: {clipCount}, Outputs: {action.timelineAsset.outputs.Count()}");
                
                if (clipCount == 0)
                {
                    Debug.LogError("[CombatExecutor] Timeline has NO animation clips! Add clips to the Animation Track in the Timeline asset.");
                }
                else if (clipCount < 3)
                {
                    Debug.LogWarning($"[CombatExecutor] Timeline has only {clipCount} clip(s), expected 3. Add more clips to test seamless transitions.");
                }
            }
            
            // Play Timeline
            timelineDirector.Play();
            
            // Verify Timeline is actually playing
            if (timelineDirector.state != PlayState.Playing)
            {
                Debug.LogWarning($"[CombatExecutor] Timeline did not start playing! State: {timelineDirector.state}");
            }
            else
            {
                Debug.Log($"[CombatExecutor] Timeline is playing. Duration: {timelineDirector.duration}s");
            }
            
            // Monitor Timeline playback for debugging (optional)
            StartCoroutine(MonitorTimelinePlayback());
            
            // Apply effects after Timeline duration (or via Timeline signals)
            StartCoroutine(ApplyEffectsAfterTimeline(action));
        }
        
        /// <summary>
        /// Verifies that Timeline actually started playing
        /// </summary>
        private IEnumerator VerifyTimelinePlaying()
        {
            yield return null; // Wait one frame for Timeline to start
            
            if (timelineDirector == null)
            {
                Debug.LogError("[CombatExecutor] Timeline director is null!");
                yield break;
            }
            
            Debug.Log($"[CombatExecutor] Timeline state after start: {timelineDirector.state}, Duration: {timelineDirector.duration}s, Time: {timelineDirector.time}s");
            
            if (timelineDirector.state != PlayState.Playing)
            {
                Debug.LogError($"[CombatExecutor] Timeline did not start playing! State: {timelineDirector.state}. Check that the Timeline asset has clips and is properly configured.");
            }
            else if (timelineDirector.duration <= 0)
            {
                Debug.LogWarning($"[CombatExecutor] Timeline has zero duration! It will finish immediately. Check that your Timeline asset has animation clips.");
            }
        }
        
        /// <summary>
        /// Called when Timeline finishes playing
        /// </summary>
        private void OnTimelineStopped(PlayableDirector director)
        {
            if (director == timelineDirector && currentAction != null)
            {
                Debug.Log($"[CombatExecutor] Timeline finished. Final position before stop: {transform.position}");
                timelineDirector.stopped -= OnTimelineStopped;
                
                // Stop root motion tracking (this will enforce final position one last time)
                if (rootMotionController != null)
                {
                    Vector3 accumulated = rootMotionController.GetAccumulatedPosition();
                    Debug.Log($"[CombatExecutor] Root motion accumulated before stop: {accumulated}");
                    
                    rootMotionController.StopTracking();
                    
                    // Wait a few frames to ensure position is locked after Timeline cleanup
                    StartCoroutine(EnsureFinalPositionAfterTimeline());
                }
                
                // Effects should have been applied via Timeline signals, but apply as fallback
                if (currentAction != null && currentTargets != null)
                {
                    ApplyEffectsToTargets(currentAction.effects);
                }
                
                CompleteAction();
            }
        }
        
        /// <summary>
        /// Ensures final position is preserved after Timeline stops (Timeline might reset it during cleanup)
        /// </summary>
        private IEnumerator EnsureFinalPositionAfterTimeline()
        {
            if (rootMotionController == null) yield break;
            
            // Wait a few frames for Timeline to finish cleanup
            yield return null; // Frame 1
            yield return null; // Frame 2
            yield return null; // Frame 3
            
            // Get the expected final position from root motion controller
            Vector3 expectedPosition = rootMotionController.GetLastValidPosition();
            float distanceFromExpected = Vector3.Distance(transform.position, expectedPosition);
            
            if (distanceFromExpected > 0.01f)
            {
                Debug.LogWarning($"[CombatExecutor] Timeline reset position after stop! Expected: {expectedPosition}, Was: {transform.position}, Restoring...");
                transform.position = expectedPosition;
            }
        }
        
        /// <summary>
        /// Monitors Timeline playback for debugging (optional - root motion controller handles the actual work)
        /// </summary>
        private IEnumerator MonitorTimelinePlayback()
        {
            yield return null; // Wait a frame for Timeline to start
            
            if (timelineDirector == null) yield break;
            
            double lastTimelineTime = 0;
            float startTime = Time.time;
            int frameCount = 0;
            
            Debug.Log($"[CombatExecutor] Monitoring Timeline playback. Duration: {timelineDirector.duration}s");
            
            while (timelineDirector != null && timelineDirector.state == PlayState.Playing)
            {
                frameCount++;
                double currentTimelineTime = timelineDirector.time;
                
                // Log clip transitions for debugging
                if (lastTimelineTime > 0)
                {
                    bool crossedBoundary = (lastTimelineTime < 0.6 && currentTimelineTime >= 0.6) ||
                                          (lastTimelineTime < 1.07 && currentTimelineTime >= 1.07);
                    
                    if (crossedBoundary)
                    {
                        Debug.Log($"[CombatExecutor] Clip transition at Timeline time {currentTimelineTime:F2}s. Position: {transform.position}");
                    }
                }
                
                lastTimelineTime = currentTimelineTime;
                yield return null;
            }
            
            float elapsedTime = Time.time - startTime;
            Debug.Log($"[CombatExecutor] Timeline playback complete. Ran for {elapsedTime:F2}s ({frameCount} frames). Final position: {transform.position}");
        }
        
        /// <summary>
        /// Applies effects after Timeline duration (fallback if Timeline signals aren't set up)
        /// </summary>
        private IEnumerator ApplyEffectsAfterTimeline(CombatAction action)
        {
            if (action.timelineAsset != null)
            {
                yield return new WaitForSeconds((float)action.timelineAsset.duration);
                
                // Only apply if Timeline hasn't already finished (check if still executing)
                if (currentAction == action && currentTargets != null && isExecuting)
                {
                    Debug.Log("[CombatExecutor] Applying effects after Timeline (Timeline signals not set up)");
                    ApplyEffectsToTargets(action.effects);
                    CompleteAction();
                }
            }
        }
        
        /// <summary>
        /// Starts a combo attack (legacy method using individual animation clips)
        /// </summary>
        private void StartCombo(CombatAction action)
        {
            currentComboHit = 0;
            
            if (animator == null)
            {
                Debug.LogWarning("[CombatExecutor] Cannot start combo - Animator is null");
                ApplyEffectsToTargets(action.effects);
                CompleteAction();
                return;
            }
            
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("[CombatExecutor] Cannot start combo - Animator has no controller");
                ApplyEffectsToTargets(action.effects);
                CompleteAction();
                return;
            }
            
            // Play first combo animation
            if (action.comboAnimations != null && action.comboAnimations.Length > 0 && action.comboAnimations[0] != null)
            {
                AnimationClip firstClip = action.comboAnimations[0];
                string clipName = firstClip.name;
                
                // Try to play by clip name first (this works if the state name matches the clip name)
                // Note: animator.Play() looks for a state with that name, not the clip name
                // We need to find the state that uses this clip
                
                // Method 1: Try using animation trigger if available (preferred for combos)
                if (!string.IsNullOrEmpty(action.animationTrigger))
                {
                    bool hasParameter = false;
                    foreach (AnimatorControllerParameter param in animator.parameters)
                    {
                        if (param.name == action.animationTrigger && param.type == AnimatorControllerParameterType.Trigger)
                        {
                            hasParameter = true;
                            break;
                        }
                    }
                    
                    if (hasParameter)
                    {
                        animator.SetTrigger(action.animationTrigger);
                        Debug.Log($"[CombatExecutor] Started combo with trigger: {action.animationTrigger}");
                        StartCoroutine(ApplyEffectsAfterDelay(action, 0.5f));
                        return;
                    }
                }
                
                // Method 2: Try to play by clip name (works if state name matches clip name)
                // Note: animator.Play() looks for a STATE name, not a clip name
                // The state name in the Animator Controller must match exactly
                
                // Try playing by clip name (state name might match)
                animator.Play(clipName, 0, 0f);
                
                // Check if it actually started playing (wait a frame)
                StartCoroutine(VerifyComboAnimationStarted(action, clipName, firstClip));
            }
            else
            {
                Debug.LogWarning("[CombatExecutor] Combo action has no combo animations. Falling back to animation trigger.");
                
                // Fallback to animation trigger
                if (!string.IsNullOrEmpty(action.animationTrigger))
                {
                    animator.SetTrigger(action.animationTrigger);
                    StartCoroutine(ApplyEffectsAfterDelay(action, 0.5f));
                }
                else
                {
                    // No animation available - apply effects immediately
                    ApplyEffectsToTargets(action.effects);
                    CompleteAction();
                }
            }
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
        private IEnumerator CheckAnimationState(CombatAction action)
        {
            yield return null; // Wait one frame
            
            var state = animator.GetCurrentAnimatorStateInfo(0);
            var nextState = animator.GetNextAnimatorStateInfo(0);
            
            Debug.Log($"[CombatExecutor] After trigger - Current: {GetStateName(animator)}, IsTransitioning: {animator.IsInTransition(0)}, Next: {(animator.IsInTransition(0) ? GetNextStateName(animator) : "None")}");
            
            if (animator.IsInTransition(0))
            {
                Debug.Log($"[CombatExecutor] Transitioning to: {GetNextStateName(animator)}");
            }
        }
        
        /// <summary>
        /// Gets readable state name from animator
        /// </summary>
        private string GetStateName(Animator anim)
        {
            if (anim == null) return "No Animator";
            
            var state = anim.GetCurrentAnimatorStateInfo(0);
            // Try common state names
            if (state.IsName("Idle")) return "Idle";
            if (state.IsName("Attack_1")) return "Attack_1";
            if (state.IsName("Attack_2")) return "Attack_2";
            if (state.IsName("Attack_3")) return "Attack_3";
            
            return $"State_{state.fullPathHash}";
        }
        
        /// <summary>
        /// Gets readable next state name from animator
        /// </summary>
        private string GetNextStateName(Animator anim)
        {
            if (anim == null || !anim.IsInTransition(0)) return "None";
            
            var nextState = anim.GetNextAnimatorStateInfo(0);
            if (nextState.IsName("Attack_1")) return "Attack_1";
            if (nextState.IsName("Attack_2")) return "Attack_2";
            if (nextState.IsName("Attack_3")) return "Attack_3";
            
            return $"State_{nextState.fullPathHash}";
        }
        
        /// <summary>
        /// Verifies that a combo animation actually started playing
        /// </summary>
        private IEnumerator VerifyComboAnimationStarted(CombatAction action, string stateName, AnimationClip clip)
        {
            yield return null; // Wait one frame for animation to start
            
            var currentState = animator.GetCurrentAnimatorStateInfo(0);
            
            // Check if we're in the expected state (by checking if state changed or normalized time is near 0)
            // If the state name didn't exist, we'll still be in the previous state
            bool animationStarted = currentState.normalizedTime < 0.1f || 
                                   animator.IsInTransition(0) ||
                                   currentState.IsName(stateName);
            
            if (animationStarted)
            {
                Debug.Log($"[CombatExecutor] ✓ Combo animation started: {stateName}");
                StartCoroutine(ApplyEffectsAfterDelay(action, 0.5f));
            }
            else
            {
                Debug.LogWarning($"[CombatExecutor] ✗ Failed to play combo animation '{stateName}'. State not found in Animator Controller.");
                Debug.LogWarning($"[CombatExecutor] Current state: {GetStateName(animator)}");
                Debug.LogWarning($"[CombatExecutor] Tip: Add an 'animationTrigger' to your action and set up a trigger in your Animator Controller, OR");
                Debug.LogWarning($"[CombatExecutor] Tip: Make sure your Animator Controller has a state named '{stateName}' that uses the '{clip.name}' animation clip.");
                
                // Apply effects immediately since animation failed
                ApplyEffectsToTargets(action.effects);
                CompleteAction();
            }
        }
        
        /// <summary>
        /// Applies effects after a delay (for testing when animation events aren't set up)
        /// </summary>
        private IEnumerator ApplyEffectsAfterDelay(CombatAction action, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Check if effects were already applied (via animation event)
            if (currentAction == action && currentTargets != null)
            {
                Debug.Log($"[CombatExecutor] Applying effects after delay (animation events not set up)");
                ApplyEffectsToTargets(action.effects);
                CompleteAction();
            }
        }
    }
}


