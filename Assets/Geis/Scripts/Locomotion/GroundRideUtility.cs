using UnityEngine;

namespace Geis.Locomotion
{
    /// <summary>
    /// Computes world-space motion to add before <see cref="CharacterController.Move"/> so the player
    /// rides kinematically moved floors (platforms, elevators) without parenting.
    /// </summary>
    public static class GroundRideUtility
    {
        /// <summary>
        /// Returns the moving ground delta for this frame. Resets internal tracking when not grounded
        /// or when ground is not a <see cref="MovingGroundCarrier"/>.
        /// </summary>
        public static Vector3 GetRideDelta(
            Transform characterRoot,
            CharacterController cc,
            LayerMask groundMask,
            float groundedOffset,
            ref Transform rideSurface,
            ref Vector3 lastRideWorldPos,
            bool isGrounded)
        {
            if (characterRoot == null || cc == null)
            {
                rideSurface = null;
                return Vector3.zero;
            }

            if (groundMask.value == 0)
                groundMask = (LayerMask)(-1);

            float sphereY = characterRoot.position.y + cc.center.y - (cc.height * 0.5f) - groundedOffset;
            Vector3 rayOrigin = new Vector3(characterRoot.position.x, sphereY + cc.radius * 0.2f, characterRoot.position.z);
            float rayLen = cc.height + 0.35f;

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLen, groundMask, QueryTriggerInteraction.Ignore))
            {
                rideSurface = null;
                return Vector3.zero;
            }

            var carrier = hit.collider.GetComponentInParent<MovingGroundCarrier>();
            if (carrier == null)
            {
                rideSurface = null;
                return Vector3.zero;
            }

            // Still ride if the grounded sphere missed (CharacterController / thin ledge edge cases) but the
            // down-ray hits the carrier close to the feet.
            float closeToFeet = cc.skinWidth + 0.22f;
            if (!isGrounded && hit.distance > closeToFeet)
            {
                rideSurface = null;
                return Vector3.zero;
            }

            Transform ride = carrier.MovingTransform;
            if (rideSurface != ride)
            {
                rideSurface = ride;
                lastRideWorldPos = ride.position;
                return Vector3.zero;
            }

            Vector3 delta = ride.position - lastRideWorldPos;
            lastRideWorldPos = ride.position;
            return delta;
        }
    }
}
