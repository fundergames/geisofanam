using UnityEngine;

namespace Geis.Locomotion
{
    [CreateAssetMenu(fileName = "PlayerCapsuleProfile", menuName = "Geis/Locomotion/Player Capsule Profile")]
    public sealed class GeisPlayerCapsuleProfile : ScriptableObject
    {
        public float standingHeight = GeisLocomotionTuningDefaults.CapsuleStandingHeight;
        public float standingCentre = GeisLocomotionTuningDefaults.CapsuleStandingCentre;
        public float crouchingHeight = GeisLocomotionTuningDefaults.CapsuleCrouchingHeight;
        public float crouchingCentre = GeisLocomotionTuningDefaults.CapsuleCrouchingCentre;
    }
}
