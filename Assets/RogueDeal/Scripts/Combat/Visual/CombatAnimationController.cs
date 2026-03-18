using UnityEngine;

namespace RogueDeal.Combat
{
    public class CombatAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        
        [Header("Attack Animations")]
        [SerializeField] private string lightAttackTrigger = "Attack_1";
        [SerializeField] private string heavyAttackTrigger = "Attack_2";
        [SerializeField] private string specialAttackTrigger = "Attack_3";
        
        [Header("Reaction Animations")]
        [SerializeField] private string hitReactionTrigger = "TakeDamage";
        [SerializeField] private string criticalHitTrigger = "TakeDamage";
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
                    Debug.Log($"[CombatAnimationController] Available parameters: {GetParameterList()}");
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
            if (!string.IsNullOrEmpty(action.animationTrigger))
            {
                if (HasParameter(action.animationTrigger))
                {
                    Debug.Log($"[CombatAnimationController] Setting trigger '{action.animationTrigger}' on {gameObject.name}");
                    animator.SetTrigger(action.animationTrigger);
                }
                else
                {
                    Debug.LogWarning($"[CombatAnimationController] Animator on {animator.gameObject.name} doesn't have '{action.animationTrigger}' parameter. Available parameters: {GetParameterList()}");
                }
            }
            else
            {
                // Fallback to default attack trigger
                if (HasParameter(lightAttackTrigger))
                {
                    Debug.Log($"[CombatAnimationController] No animation trigger in action, using default '{lightAttackTrigger}' on {gameObject.name}");
                    animator.SetTrigger(lightAttackTrigger);
                }
                else
                {
                    Debug.LogWarning($"[CombatAnimationController] Animator on {animator.gameObject.name} doesn't have '{lightAttackTrigger}' parameter. Available parameters: {GetParameterList()}");
                }
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
                if (HasParameter(lightAttackTrigger))
                {
                    Debug.Log($"[CombatAnimationController] Setting trigger '{lightAttackTrigger}' on {gameObject.name}");
                    animator.SetTrigger(lightAttackTrigger);
                }
                else
                {
                    Debug.LogWarning($"[CombatAnimationController] Animator on {animator.gameObject.name} doesn't have '{lightAttackTrigger}' parameter. Available parameters: {GetParameterList()}");
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

        private bool HasParameter(string paramName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            foreach (var param in animator.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }

        private string GetParameterList()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return "None (no controller)";

            var paramNames = new System.Collections.Generic.List<string>();
            foreach (var param in animator.parameters)
            {
                paramNames.Add($"{param.name}({param.type})");
            }
            return string.Join(", ", paramNames);
        }
    }
}
