using UnityEngine;

namespace Geis.Animation
{
    /// <summary>
    /// Single place to write Synty locomotion parameters and Polygon locomotion subset.
    /// </summary>
    public static class LocomotionAnimatorApplier
    {
        /// <summary>
        /// Applies the same parameter set as <c>GeisPlayerAnimationController.UpdateAnimatorController</c>,
        /// plus optional spectral <see cref="LocomotionApplyContext.SetIsJumping"/>.
        /// </summary>
        public static void ApplySyntyLocomotion(Animator animator, LocomotionPresentationSnapshot s, LocomotionApplyContext ctx)
        {
            if (animator == null)
                return;

            bool air = ctx.AirGaitForAnimator;

            animator.SetFloat(LocomotionAnimatorIds.LeanValue, s.LeanValue);
            animator.SetFloat(LocomotionAnimatorIds.HeadLookX, s.HeadLookX);
            animator.SetFloat(LocomotionAnimatorIds.HeadLookY, s.HeadLookY);
            animator.SetFloat(LocomotionAnimatorIds.BodyLookX, s.BodyLookX);
            animator.SetFloat(LocomotionAnimatorIds.BodyLookY, s.BodyLookY);

            animator.SetFloat(LocomotionAnimatorIds.IsStrafing, s.IsStrafingFloat);

            animator.SetFloat(LocomotionAnimatorIds.InclineAngle, s.InclineAngle);

            animator.SetFloat(LocomotionAnimatorIds.MoveSpeed, air ? 0f : s.MoveSpeed2D);
            animator.SetInteger(LocomotionAnimatorIds.CurrentGait, air ? 0 : s.CurrentGait);

            animator.SetFloat(LocomotionAnimatorIds.StrafeDirectionX, air ? 0f : s.StrafeDirectionX);
            animator.SetFloat(LocomotionAnimatorIds.StrafeDirectionZ, air ? 0f : s.StrafeDirectionZ);
            animator.SetFloat(LocomotionAnimatorIds.ForwardStrafe, s.ForwardStrafe);
            animator.SetFloat(LocomotionAnimatorIds.CameraRotationOffset, s.CameraRotationOffset);

            animator.SetBool(LocomotionAnimatorIds.MovementInputHeld, s.MovementInputHeld);
            animator.SetBool(LocomotionAnimatorIds.MovementInputPressed, s.MovementInputPressed);
            animator.SetBool(LocomotionAnimatorIds.MovementInputTapped, s.MovementInputTapped);
            animator.SetFloat(LocomotionAnimatorIds.ShuffleDirectionX, s.ShuffleDirectionX);
            animator.SetFloat(LocomotionAnimatorIds.ShuffleDirectionZ, s.ShuffleDirectionZ);

            animator.SetBool(LocomotionAnimatorIds.IsTurningInPlace, s.IsTurningInPlace);
            animator.SetBool(LocomotionAnimatorIds.IsCrouching, s.IsCrouching);

            animator.SetFloat(LocomotionAnimatorIds.FallingDuration, s.FallingDuration);
            if (ctx.HasFallingBlendParameter)
                animator.SetFloat(LocomotionAnimatorIds.FallingBlend, ctx.FallingBlendValue);

            animator.SetBool(LocomotionAnimatorIds.IsGrounded, s.IsGrounded);

            animator.SetBool(LocomotionAnimatorIds.IsWalking, air ? false : s.IsWalking);
            animator.SetBool(LocomotionAnimatorIds.IsStopped, air ? true : s.IsStopped);
            animator.SetBool(LocomotionAnimatorIds.IsStarting, s.IsStarting);

            animator.SetFloat(LocomotionAnimatorIds.LocomotionStartDirection, s.LocomotionStartDirection);

            if (ctx.SetIsJumping)
                animator.SetBool(LocomotionAnimatorIds.IsJumping, ctx.IsJumpingValue);
        }

        /// <summary>
        /// Polygon combat rig: optional Synty-style params plus normalized <c>Speed</c>.
        /// </summary>
        public static void ApplyPolygonLocomotion(
            Animator animator,
            bool usePolygonParams,
            bool isAttackingOrDodging,
            float speed2D,
            int currentGait,
            float strafeDirX,
            float strafeDirZ,
            float isStrafingFloat,
            bool isCrouching,
            bool isGrounded,
            float sprintSpeed)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            if (usePolygonParams)
            {
                if (AnimatorParameterGuard.HasParameter(animator, "MoveSpeed"))
                    animator.SetFloat(LocomotionAnimatorIds.MoveSpeed, speed2D);
                if (AnimatorParameterGuard.HasParameter(animator, "CurrentGait"))
                    animator.SetInteger(LocomotionAnimatorIds.CurrentGait, currentGait);
                if (AnimatorParameterGuard.HasParameter(animator, "StrafeDirectionX"))
                    animator.SetFloat(LocomotionAnimatorIds.StrafeDirectionX, strafeDirX);
                if (AnimatorParameterGuard.HasParameter(animator, "StrafeDirectionZ"))
                    animator.SetFloat(LocomotionAnimatorIds.StrafeDirectionZ, strafeDirZ);
                if (AnimatorParameterGuard.HasParameter(animator, "IsStrafing"))
                    animator.SetFloat(LocomotionAnimatorIds.IsStrafing, isStrafingFloat);
                if (AnimatorParameterGuard.HasParameter(animator, "IsCrouching"))
                    animator.SetBool(LocomotionAnimatorIds.IsCrouching, isCrouching);
            }

            if (AnimatorParameterGuard.HasParameter(animator, "IsGrounded"))
                animator.SetBool(LocomotionAnimatorIds.IsGrounded, isGrounded);

            if (AnimatorParameterGuard.HasParameter(animator, "Speed"))
            {
                float normalized = sprintSpeed > 0.0001f ? speed2D / sprintSpeed : 0f;
                animator.SetFloat(LocomotionAnimatorIds.Speed, isAttackingOrDodging ? 0f : normalized);
            }
        }
    }
}
