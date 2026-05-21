/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

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

        // Action-feel responsiveness tuning (Phase 4).
        public const float AccelRate = 25f;
        public const float DecelRate = 15f;
        public const float SprintInstantFraction = 0.85f;
        public const float MaxTurnDegPerSecond = 720f;

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
        public const float CoyoteTimeSeconds = 0.10f;
        public const float JumpBufferSeconds = 0.15f;

        public const float HeadLookLimitDegrees = 60f;

        public const bool ApplyRootRotationDuringAttack = false;
        public const bool ApplyRootRotationDuringDodge = false;
        public const float DodgeInputDeadzone = 0.05f;
        public const float DodgeFallbackDuration = 1.2f;
        public const bool RequireMovementInputForDodge = false;

        // Attack/dodge cancel-window tuning (Phases 1-3).
        public const float AttackMoveCancelStickThreshold = 0.5f;
        public const float AttackRecoveryExitNormalizedTime = 0.85f;
        public const float AttackExitVelocityCarry = 0.6f;
        public const float DodgeRecoveryStartNormalizedTime = 0.62f;
        public const float RollRecoveryStartNormalizedTime = 0.72f;
        public const float DodgeMoveCancelStickThreshold = 0.3f;
        public const float DodgeExitVelocityCarry = 0.75f;
        public const float RollExitVelocityCarry = 0f;
        public const float InputBufferSeconds = 0.18f;
        public const float DodgeDoubleTapWindow = 0.3f;
        public const bool DodgeDoubleTapRollEnabled = true;
        public const float RollDistanceMultiplier = 1.35f;
        public const float RollInvulnerabilityEndNormalizedTime = 0.52f;
        public const float StrafeStyleMaxPlanarSpeed = 5f;

        public const float CameraRotationOffset = 0f;
        public const float BowEquipLayerBlendSpeed = 8f;
        public const float BowAimHeadLookMultiplier = 0.6f;
        public const float BowAimBodyLookMultiplier = 0.4f;
        public const float DodgeInvulnerabilityEndNormalizedTime = 0.38f;
        public const float DodgeScriptedPlaneSpeed = 7f;
        public const float DodgeScriptedDuration = 0.35f;
        public const bool DebugDodgeDoubleTap = false;

        /// <summary>Animator max-speed blend rate; must match locomotion damp time on the player controller.</summary>
        public const float AnimationDampTime = 5f;
    }
}
