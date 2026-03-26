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
    public partial class CombatExecutor
    {
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
                    if (AnimatorParameterGuard.HasTrigger(animator, action.animationTrigger))
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
