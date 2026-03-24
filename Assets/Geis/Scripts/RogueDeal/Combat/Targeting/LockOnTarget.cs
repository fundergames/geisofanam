using UnityEngine;
using RogueDeal.Combat;

namespace RogueDeal.Combat.Targeting
{
    /// <summary>
    /// Synty-style lock-on target. Add to enemies with a trigger collider.
    /// When the player (with TargetingManager) enters the trigger, this is added as a candidate.
    /// Ensure the Collider is set to Is Trigger = true.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LockOnTarget : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [Tooltip("Optional child transform with MeshRenderer for highlight (e.g. TargetHighlight). If null, Highlight() does nothing.")]
        [SerializeField] private Transform highlightOrb;
        [SerializeField] private Material highlightMat;
        [SerializeField] private Material targetLockMat;

        private MeshRenderer _meshRenderer;

        private void Start()
        {
            if (highlightOrb == null)
            {
                highlightOrb = transform.Find("TargetHighlight");
            }
            if (highlightOrb != null)
            {
                _meshRenderer = highlightOrb.GetComponent<MeshRenderer>();
                if (_meshRenderer != null)
                {
                    highlightOrb.gameObject.SetActive(false);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var targetingManager = other.GetComponent<TargetingManager>();
            if (targetingManager == null)
            {
                targetingManager = other.GetComponentInParent<TargetingManager>();
            }
            if (targetingManager != null)
            {
                targetingManager.AddTargetCandidate(gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var targetingManager = other.GetComponent<TargetingManager>();
            if (targetingManager == null)
            {
                targetingManager = other.GetComponentInParent<TargetingManager>();
            }
            if (targetingManager != null)
            {
                targetingManager.RemoveTargetCandidate(gameObject);
                Highlight(false, false);
            }
        }

        /// <summary>
        /// Sets the highlight status (Synty-style). Call from TargetingManager when updating best target.
        /// </summary>
        public void Highlight(bool enable, bool targetLock)
        {
            if (highlightOrb == null || _meshRenderer == null) return;

            highlightOrb.gameObject.SetActive(enable);
            if (enable)
            {
                Material mat = targetLock && targetLockMat != null ? targetLockMat : highlightMat;
                if (mat != null)
                {
                    _meshRenderer.material = mat;
                }
            }
        }

        /// <summary>
        /// Gets the CombatEntity on this object or a parent, if any.
        /// </summary>
        public CombatEntity GetCombatEntity()
        {
            return GetComponent<CombatEntity>() ?? GetComponentInParent<CombatEntity>();
        }
    }
}
