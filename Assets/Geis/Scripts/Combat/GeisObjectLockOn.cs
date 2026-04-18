// Geis of Anam - Lock-on target that registers with GeisPlayerAnimationController.
// Use this on lock-on targets instead of SampleObjectLockOn when using the Geis player.
// Same structure as SampleObjectLockOn: needs child "TargetHighlight" with MeshRenderer.

using UnityEngine;

namespace Geis.Combat
{
    /// <summary>
    /// Lock-on target for GeisPlayerAnimationController.
    /// Registers this object when the player enters its trigger; use alongside or instead of SampleObjectLockOn.
    /// Requires child "TargetHighlight" with MeshRenderer (same as SampleObjectLockOn).
    /// </summary>
    public class GeisObjectLockOn : MonoBehaviour
    {
        [Header("Highlight")]
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private Material targetMaterial;

        private Transform _highlightOrb;
        private MeshRenderer _meshRenderer;

        private void Start()
        {
            _highlightOrb = transform.Find("TargetHighlight");
            if (_highlightOrb == null)
                return;

            _meshRenderer = _highlightOrb.GetComponent<MeshRenderer>();
            _highlightOrb.gameObject.SetActive(false);
        }

        public void Highlight(bool enable, bool targetLock)
        {
            if (_highlightOrb == null)
                return;

            _highlightOrb.gameObject.SetActive(enable);
            if (!enable || _meshRenderer == null)
                return;

            var mat = targetLock ? targetMaterial : highlightMaterial;
            if (mat != null)
                _meshRenderer.material = mat;
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            var controller = otherCollider.GetComponent<Geis.Locomotion.GeisPlayerAnimationController>();
            if (controller != null)
                controller.AddTargetCandidate(gameObject);
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            var controller = otherCollider.GetComponent<Geis.Locomotion.GeisPlayerAnimationController>();
            if (controller != null)
            {
                controller.RemoveTarget(gameObject);
                Highlight(false, false);
            }
        }
    }
}
