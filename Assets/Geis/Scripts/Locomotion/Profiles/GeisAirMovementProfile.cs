using UnityEngine;

namespace Geis.Locomotion
{
    [CreateAssetMenu(fileName = "AirMovementProfile", menuName = "Geis/Locomotion/Air Movement Profile")]
    public sealed class GeisAirMovementProfile : ScriptableObject
    {
        public float jumpForce = GeisLocomotionTuningDefaults.JumpForce;

        [Tooltip("Multiplier on Physics.gravity while airborne.")]
        public float gravityMultiplier = GeisLocomotionTuningDefaults.GravityMultiplier;

        [Tooltip("Seconds of air time to blend Falling_BlendTree from short to large fall.")]
        public float fallingBlendRampSeconds = GeisLocomotionTuningDefaults.FallingBlendRampSeconds;
    }
}
