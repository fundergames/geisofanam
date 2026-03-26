using UnityEngine;

namespace Geis.Locomotion
{
    [CreateAssetMenu(fileName = "LocomotionSpeedProfile", menuName = "Geis/Locomotion/Locomotion Speed Profile")]
    public sealed class GeisLocomotionSpeedProfile : ScriptableObject
    {
        [Tooltip("Whether the character always strafes relative to the camera when moving.")]
        public bool alwaysStrafe = GeisLocomotionTuningDefaults.AlwaysStrafe;

        [Tooltip("Slowest movement speed when walking or half-press.")]
        public float walkSpeed = GeisLocomotionTuningDefaults.WalkSpeed;

        [Tooltip("Default movement speed (run).")]
        public float runSpeed = GeisLocomotionTuningDefaults.RunSpeed;

        [Tooltip("Top movement speed when sprinting.")]
        public float sprintSpeed = GeisLocomotionTuningDefaults.SprintSpeed;

        [Tooltip("Damping when lerping toward target planar speed.")]
        public float speedChangeDamping = GeisLocomotionTuningDefaults.SpeedChangeDamping;

        [Tooltip("Rotation smoothing when aligning to move/camera.")]
        public float rotationSmoothing = GeisLocomotionTuningDefaults.RotationSmoothing;
    }
}
