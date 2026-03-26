namespace Geis.Animation
{
    /// <summary>
    /// How <see cref="LocomotionAnimatorApplier"/> applies <see cref="LocomotionPresentationSnapshot"/> (air vs ground, optional IsJumping).
    /// </summary>
    public struct LocomotionApplyContext
    {
        /// <summary>When true, MoveSpeed/gait/strafe are zeroed/idle for jump/fall animator states.</summary>
        public bool AirGaitForAnimator;

        public bool HasFallingBlendParameter;
        public float FallingBlendValue;

        public bool SetIsJumping;
        public bool IsJumpingValue;
    }
}
