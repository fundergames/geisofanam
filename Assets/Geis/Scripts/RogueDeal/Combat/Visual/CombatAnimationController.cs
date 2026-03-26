using Geis.Animation;
using UnityEngine;

namespace RogueDeal.Combat
{
    public class CombatAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        
        [Header("Attack Animations")]
        [SerializeField] private string lightAttackTrigger = "Attack_1";
#pragma warning disable CS0414
        [SerializeField] private string heavyAttackTrigger = "Attack_2";
        [SerializeField] private string specialAttackTrigger = "Attack_3";
#pragma warning restore CS0414

        [Header("Reaction Animations")]
        [SerializeField] private string hitReactionTrigger = "TakeDamage";
#pragma warning disable CS0414
        [SerializeField] private string criticalHitTrigger = "TakeDamage";
#pragma warning restore CS0414
        [SerializeField] private string dodgeTrigger = "Dodge";
        [SerializeField] private string blockTrigger = "Block";
        
        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }
            
            if (animator == null)
            {
                Debug.LogWarning($"[CombatAnimationController] No Animator found on {gameObject.name} or its children. Animations will not play.");
            }
            else
            {
                string controllerName = animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NONE";
                Debug.Log($"[CombatAnimationController] Animator found on {animator.gameObject.name} (searching from {gameObject.name}). Controller: {controllerName}");
                
                if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning($"[CombatAnimationController] Animator on {animator.gameObject.name} has NO controller assigned! Animations will not work.");
                }
                else
                {
                    Debug.Log($"[CombatAnimationController] Available parameters: {AnimatorParameterGuard.FormatParameterList(animator)}");
                }
            }
        }

        /// <summary>
        /// Plays attack animation from CombatAction (new system)
        /// </summary>
        public void PlayAttack(RogueDeal.Combat.Core.Data.CombatAction action)
        {
            if (animator == null)
            {
                Debug.LogWarning($"[CombatAnimationController] Cannot play attack on {gameObject.name} - no Animator");
                return;
            }

            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"[CombatAnimationController] No AnimatorController assigned to {animator.gameObject.name}. Cannot play animations.");
                return;
            }

            // Use animation trigger from CombatAction
            PlayAttack(!string.IsNullOrEmpty(action.animationTrigger) ? action.animationTrigger : null);
        }

        /// <summary>
        /// Plays attack animation by trigger name. Uses default light attack trigger if triggerName is null or empty.
        /// </summary>
        public void PlayAttack(string triggerName)
        {
            if (animator == null)
            {
                Debug.LogWarning($"[CombatAnimationController] Cannot play attack on {gameObject.name} - no Animator");
                return;
            }

            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"[CombatAnimationController] No AnimatorController assigned to {animator.gameObject.name}. Cannot play animations.");
                return;
            }

            string trigger = !string.IsNullOrEmpty(triggerName) ? triggerName : lightAttackTrigger;
            if (AnimatorParameterGuard.TrySetTrigger(animator, trigger))
            {
                Debug.Log($"[CombatAnimationController] Setting trigger '{trigger}' on {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[CombatAnimationController] Animator on {animator.gameObject.name} doesn't have '{trigger}' trigger. Available parameters: {AnimatorParameterGuard.FormatParameterList(animator)}");
            }
        }

        /// <summary>
        /// Legacy method for backward compatibility with AbilityData
        /// </summary>
        [System.Obsolete("Use PlayAttack(CombatAction) instead")]
        public void PlayAttack(AbilityData ability)
        {
            if (animator == null)
            {
                Debug.LogWarning($"[CombatAnimationController] Cannot play attack on {gameObject.name} - no Animator");
                return;
            }

            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"[CombatAnimationController] No AnimatorController assigned to {animator.gameObject.name}. Cannot play animations.");
                return;
            }

            if (ability.animation != null)
            {
                Debug.Log($"[CombatAnimationController] Playing animation '{ability.animation.name}' on {gameObject.name}");
                animator.Play(ability.animation.name);
            }
            else
            {
                if (AnimatorParameterGuard.TrySetTrigger(animator, lightAttackTrigger))
                {
                    Debug.Log($"[CombatAnimationController] Setting trigger '{lightAttackTrigger}' on {gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"[CombatAnimationController] Animator on {animator.gameObject.name} doesn't have '{lightAttackTrigger}' trigger. Available parameters: {AnimatorParameterGuard.FormatParameterList(animator)}");
                }
            }
        }

        public void PlayHitReaction(EffectType effectType)
        {
            if (animator == null) return;

            switch (effectType)
            {
                case EffectType.Damage:
                    Debug.Log($"[CombatAnimationController] Playing hit reaction '{hitReactionTrigger}' on {gameObject.name}");
                    animator.SetTrigger(hitReactionTrigger);
                    break;
            }
        }

        public void PlayDodge()
        {
            animator?.SetTrigger(dodgeTrigger);
        }

        public void PlayBlock()
        {
            animator?.SetTrigger(blockTrigger);
        }

        public void EndAttack()
        {
        }

        private bool HasParameter(string paramName) =>
            AnimatorParameterGuard.HasParameter(animator, paramName);
    }
}
