namespace Geis.Locomotion
{
    /// <summary>
    /// Inline fallbacks when a locomotion profile reference is not assigned on the prefab.
    /// </summary>
    public static class GeisLocomotionTuningDefaults
    {
        public const bool AlwaysStrafe = true;
        public const float WalkSpeed = 1.4f;
        public const float RunSpeed = 2.5f;
        public const float SprintSpeed = 7f;
        public const float SpeedChangeDamping = 10f;
        public const float RotationSmoothing = 10f;

        public const float ButtonHoldThreshold = 0.15f;
        public const float ForwardStrafeMinThreshold = -55f;
        public const float ForwardStrafeMaxThreshold = 125f;
        public const float ForwardStrafe = 1f;

        public const float CapsuleStandingHeight = 1.8f;
        public const float CapsuleStandingCentre = 0.93f;
        public const float CapsuleCrouchingHeight = 1.2f;
        public const float CapsuleCrouchingCentre = 0.6f;

        public const float GroundedOffset = 0.14f;

        public const float JumpForce = 10f;
        public const float GravityMultiplier = 2f;
        public const float FallingBlendRampSeconds = 0.65f;

        public const float HeadLookLimitDegrees = 60f;

        public const bool ApplyRootRotationDuringAttack = false;
        public const bool ApplyRootRotationDuringDodge = false;
        public const float DodgeInputDeadzone = 0.05f;
        public const float DodgeFallbackDuration = 1.2f;
        public const bool RequireMovementInputForDodge = false;
    }
}
