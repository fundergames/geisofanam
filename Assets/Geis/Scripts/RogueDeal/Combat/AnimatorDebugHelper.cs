using Geis.Animation;
using UnityEngine;

namespace RogueDeal.Combat
{
    public class AnimatorDebugHelper : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private Color gizmoColor = Color.yellow;
        [SerializeField] private float gizmoRadius = 0.5f;
        
        [Header("Auto-Discovery")]
        [SerializeField] private bool autoFindAnimator = true;
        
        private Animator animator;
        
        private void Start()
        {
            if (autoFindAnimator)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }
            
            if (animator != null && showDebugInfo)
            {
                LogAnimatorInfo();
            }
        }
        
        private void LogAnimatorInfo()
        {
            Debug.Log($"=== Animator Debug Info: {gameObject.name} ===");
            Debug.Log($"Animator Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "None")}");
            
            if (animator.runtimeAnimatorController != null)
            {
                Debug.Log("Parameters:");
                foreach (var param in animator.parameters)
                {
                    Debug.Log($"  - {param.name} ({param.type})");
                }
                
                Debug.Log("Layers:");
                for (int i = 0; i < animator.layerCount; i++)
                {
                    Debug.Log($"  - Layer {i}: {animator.GetLayerName(i)} (Weight: {animator.GetLayerWeight(i)})");
                }
            }
            
            Debug.Log("=================================");
        }
        
        [ContextMenu("Test Attack Animation")]
        public void TestAttack()
        {
            if (animator != null && HasParameter(animator, "Attack"))
            {
                Debug.Log($"[AnimatorDebug] Triggering Attack on {gameObject.name}");
                animator.SetTrigger("Attack");
            }
            else
            {
                Debug.LogWarning($"[AnimatorDebug] Cannot trigger Attack - Animator: {animator != null}, Has Parameter: {(animator != null ? HasParameter(animator, "Attack").ToString() : "N/A")}");
            }
        }
        
        [ContextMenu("Test Damage Animation")]
        public void TestDamage()
        {
            if (animator != null && HasParameter(animator, "Damage"))
            {
                Debug.Log($"[AnimatorDebug] Triggering Damage on {gameObject.name}");
                animator.SetTrigger("Damage");
            }
            else
            {
                Debug.LogWarning($"[AnimatorDebug] Cannot trigger Damage - Animator: {animator != null}, Has Parameter: {(animator != null ? HasParameter(animator, "Damage").ToString() : "N/A")}");
            }
        }
        
        [ContextMenu("Test Spawn Animation")]
        public void TestSpawn()
        {
            if (animator != null && HasParameter(animator, "Spawn"))
            {
                Debug.Log($"[AnimatorDebug] Triggering Spawn on {gameObject.name}");
                animator.SetTrigger("Spawn");
            }
            else
            {
                Debug.LogWarning($"[AnimatorDebug] Cannot trigger Spawn - Animator: {animator != null}, Has Parameter: {(animator != null ? HasParameter(animator, "Spawn").ToString() : "N/A")}");
            }
        }
        
        [ContextMenu("Test Death Animation")]
        public void TestDeath()
        {
            if (animator != null && HasParameter(animator, "Death"))
            {
                Debug.Log($"[AnimatorDebug] Triggering Death on {gameObject.name}");
                animator.SetTrigger("Death");
            }
            else
            {
                Debug.LogWarning($"[AnimatorDebug] Cannot trigger Death - Animator: {animator != null}, Has Parameter: {(animator != null ? HasParameter(animator, "Death").ToString() : "N/A")}");
            }
        }
        
        private static bool HasParameter(Animator anim, string paramName) =>
            AnimatorParameterGuard.HasParameter(anim, paramName);
        
        [ContextMenu("Log Current Animation State")]
        public void LogCurrentState()
        {
            if (animator == null)
            {
                Debug.LogWarning("[AnimatorDebug] No animator found");
                return;
            }
            
            for (int i = 0; i < animator.layerCount; i++)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(i);
                Debug.Log($"[AnimatorDebug] Layer {i} ({animator.GetLayerName(i)}): Playing clip at {stateInfo.normalizedTime:F2}");
            }
        }
        
        [ContextMenu("Force Refresh Animator Info")]
        public void RefreshAnimatorInfo()
        {
            if (autoFindAnimator)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }
            
            LogAnimatorInfo();
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebugInfo)
                return;
            
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
            
            if (animator != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (animator == null)
                return;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(animator.transform.position, gizmoRadius * 1.5f);
        }
    }
}
