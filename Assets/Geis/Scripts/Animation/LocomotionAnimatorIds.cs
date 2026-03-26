using UnityEngine;

namespace Geis.Animation
{
    /// <summary>
    /// Canonical Animator parameter name hashes for Synty-style locomotion, Polygon combat rigs, and shared combat triggers.
    /// </summary>
    public static class LocomotionAnimatorIds
    {
        public static readonly int MovementInputTapped = Animator.StringToHash("MovementInputTapped");
        public static readonly int MovementInputPressed = Animator.StringToHash("MovementInputPressed");
        public static readonly int MovementInputHeld = Animator.StringToHash("MovementInputHeld");
        public static readonly int ShuffleDirectionX = Animator.StringToHash("ShuffleDirectionX");
        public static readonly int ShuffleDirectionZ = Animator.StringToHash("ShuffleDirectionZ");

        public static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        public static readonly int CurrentGait = Animator.StringToHash("CurrentGait");

        public static readonly int IsJumping = Animator.StringToHash("IsJumping");
        public static readonly int FallingDuration = Animator.StringToHash("FallingDuration");
        public static readonly int FallingBlend = Animator.StringToHash("FallingBlend");

        public static readonly int InclineAngle = Animator.StringToHash("InclineAngle");

        public static readonly int StrafeDirectionX = Animator.StringToHash("StrafeDirectionX");
        public static readonly int StrafeDirectionZ = Animator.StringToHash("StrafeDirectionZ");

        public static readonly int ForwardStrafe = Animator.StringToHash("ForwardStrafe");
        public static readonly int CameraRotationOffset = Animator.StringToHash("CameraRotationOffset");
        public static readonly int IsStrafing = Animator.StringToHash("IsStrafing");
        public static readonly int IsTurningInPlace = Animator.StringToHash("IsTurningInPlace");

        public static readonly int IsCrouching = Animator.StringToHash("IsCrouching");

        public static readonly int IsWalking = Animator.StringToHash("IsWalking");
        public static readonly int IsStopped = Animator.StringToHash("IsStopped");
        public static readonly int IsStarting = Animator.StringToHash("IsStarting");

        public static readonly int IsGrounded = Animator.StringToHash("IsGrounded");

        public static readonly int LeanValue = Animator.StringToHash("LeanValue");
        public static readonly int HeadLookX = Animator.StringToHash("HeadLookX");
        public static readonly int HeadLookY = Animator.StringToHash("HeadLookY");
        public static readonly int BodyLookX = Animator.StringToHash("BodyLookX");
        public static readonly int BodyLookY = Animator.StringToHash("BodyLookY");

        public static readonly int LocomotionStartDirection = Animator.StringToHash("LocomotionStartDirection");

        public static readonly int Attack1 = Animator.StringToHash("Attack_1");
        public static readonly int Attack2 = Animator.StringToHash("Attack_2");
        public static readonly int Attack3 = Animator.StringToHash("Attack_3");
        public static readonly int Attack = Animator.StringToHash("Attack");
        public static readonly int ComboState = Animator.StringToHash("ComboState");
        public static readonly int ComboStateBlend = Animator.StringToHash("ComboStateBlend");

        public static readonly int DodgeDirection = Animator.StringToHash("DodgeDirection");
        public static readonly int Dodge = Animator.StringToHash("Dodge");

        public static readonly int DodgeLeafFront = Animator.StringToHash("Dodge_Front");
        public static readonly int DodgeLeafBack = Animator.StringToHash("Dodge_Back");
        public static readonly int DodgeLeafLeft = Animator.StringToHash("Dodge_Left");
        public static readonly int DodgeLeafRight = Animator.StringToHash("Dodge_Right");

        /// <summary>Polygon / normalized locomotion speed (0–1).</summary>
        public static readonly int Speed = Animator.StringToHash("Speed");

        public static readonly int IsAction = Animator.StringToHash("IsAction");
        public static readonly int TakeAction = Animator.StringToHash("TakeAction");
        public static readonly int ActionIndex = Animator.StringToHash("ActionIndex");
        public static readonly int ActionType = Animator.StringToHash("ActionType");
    }
}
