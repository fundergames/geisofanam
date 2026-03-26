using UnityEngine;

namespace Geis.Locomotion
{
    [CreateAssetMenu(fileName = "AttackDodgeLocomotionProfile", menuName = "Geis/Locomotion/Attack & Dodge Locomotion Profile")]
    public sealed class GeisAttackDodgeLocomotionProfile : ScriptableObject
    {
        [Header("Root motion")]
        [Tooltip("Apply animation root rotation during attacks.")]
        public bool applyRootRotationDuringAttack = GeisLocomotionTuningDefaults.ApplyRootRotationDuringAttack;

        [Tooltip("Apply animation root rotation during dodge clips.")]
        public bool applyRootRotationDuringDodge = GeisLocomotionTuningDefaults.ApplyRootRotationDuringDodge;

        [Header("Dodge")]
        [Tooltip("Stick magnitude below this counts as neutral (forward dodge).")]
        public float dodgeInputDeadzone = GeisLocomotionTuningDefaults.DodgeInputDeadzone;

        [Tooltip("Fallback seconds if dodge clip length cannot be read.")]
        public float dodgeFallbackDuration = GeisLocomotionTuningDefaults.DodgeFallbackDuration;

        [Tooltip("If true, dodge only when movement stick exceeds deadzone.")]
        public bool requireMovementInputForDodge = GeisLocomotionTuningDefaults.RequireMovementInputForDodge;
    }
}
