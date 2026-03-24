using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Animations;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Helper component to maintain root motion continuity between Timeline animation clips.
    /// Attach this to the GameObject with the Animator to ensure seamless transitions.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class TimelineRootMotionHelper : MonoBehaviour
    {
        private Animator animator;
        private Vector3 lastRootPosition;
        private bool isTrackingRootMotion = false;
        
        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = true;
            }
        }
        
        private void OnAnimatorMove()
        {
            if (animator != null && animator.applyRootMotion && isTrackingRootMotion)
            {
                // Accumulate root motion delta
                Vector3 deltaPosition = animator.deltaPosition;
                transform.position += deltaPosition;
                lastRootPosition = transform.position;
            }
        }
        
        /// <summary>
        /// Start tracking root motion for Timeline
        /// </summary>
        public void StartTracking()
        {
            isTrackingRootMotion = true;
            lastRootPosition = transform.position;
            
            if (animator != null)
            {
                animator.applyRootMotion = true;
            }
        }
        
        /// <summary>
        /// Stop tracking root motion
        /// </summary>
        public void StopTracking()
        {
            isTrackingRootMotion = false;
        }
        
        /// <summary>
        /// Gets the current root motion position
        /// </summary>
        public Vector3 GetRootPosition()
        {
            return lastRootPosition;
        }
    }
}

