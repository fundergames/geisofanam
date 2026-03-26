namespace Geis.Animation
{
    /// <summary>
    /// Values pushed to the Animator for Synty-style locomotion (layer 0).
    /// </summary>
    public struct LocomotionPresentationSnapshot
    {
        public float LeanValue;
        public float HeadLookX;
        public float HeadLookY;
        public float BodyLookX;
        public float BodyLookY;

        public float IsStrafingFloat;
        public float InclineAngle;

        public float MoveSpeed2D;
        public int CurrentGait;

        public float StrafeDirectionX;
        public float StrafeDirectionZ;
        public float ForwardStrafe;
        public float CameraRotationOffset;

        public bool MovementInputHeld;
        public bool MovementInputPressed;
        public bool MovementInputTapped;
        public float ShuffleDirectionX;
        public float ShuffleDirectionZ;

        public bool IsTurningInPlace;
        public bool IsCrouching;

        public float FallingDuration;

        public bool IsGrounded;
        public bool IsWalking;
        public bool IsStopped;
        public bool IsStarting;

        public float LocomotionStartDirection;
    }
}
