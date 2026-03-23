// Geis of Anam - Lock-on target that registers with GeisPlayerAnimationController.
// Use this on lock-on targets instead of SampleObjectLockOn when using the Geis player.
// Same structure as SampleObjectLockOn: needs child "TargetHighlight" with MeshRenderer.

using UnityEngine;
using Synty.AnimationBaseLocomotion.Samples;

namespace Geis.Combat
{
    /// <summary>
    /// Lock-on target for GeisPlayerAnimationController.
    /// Registers this object when the player enters its trigger; use alongside or instead of SampleObjectLockOn.
    /// Requires child "TargetHighlight" with MeshRenderer (same as SampleObjectLockOn).
    /// </summary>
    public class GeisObjectLockOn : SampleObjectLockOn
    {
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
