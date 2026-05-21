/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using UnityEngine;

namespace Geis.Locomotion
{
    public partial class GeisPlayerAnimationController
    {
        [Header("Locomotion Profiles")]
        [Tooltip("Tuning source of truth. When null, loads Resources/Movement/PlayerLocomotionProfiles if present, else code defaults.")]
        [SerializeField]
        private GeisPlayerLocomotionProfileBundle _locomotionProfiles;

        internal void ApplySpeedDefaults()
        {
            _alwaysStrafe = GeisLocomotionTuningDefaults.AlwaysStrafe;
            _walkSpeed = GeisLocomotionTuningDefaults.WalkSpeed;
            _runSpeed = GeisLocomotionTuningDefaults.RunSpeed;
            _sprintSpeed = GeisLocomotionTuningDefaults.SprintSpeed;
            _speedChangeDamping = GeisLocomotionTuningDefaults.SpeedChangeDamping;
            _rotationSmoothing = GeisLocomotionTuningDefaults.RotationSmoothing;
            _accelRate = GeisLocomotionTuningDefaults.AccelRate;
            _decelRate = GeisLocomotionTuningDefaults.DecelRate;
            _sprintInstantFraction = GeisLocomotionTuningDefaults.SprintInstantFraction;
            _maxTurnDegPerSecond = GeisLocomotionTuningDefaults.MaxTurnDegPerSecond;
        }

        internal void ApplyAirMovementDefaults()
        {
            _jumpForce = GeisLocomotionTuningDefaults.JumpForce;
            _gravityMultiplier = GeisLocomotionTuningDefaults.GravityMultiplier;
            _coyoteTimeSeconds = GeisLocomotionTuningDefaults.CoyoteTimeSeconds;
            _jumpBufferSeconds = GeisLocomotionTuningDefaults.JumpBufferSeconds;
            _inputBuffers.JumpBufferSeconds = _jumpBufferSeconds;
        }

        internal void ApplyAttackDodgeDefaults()
        {
            _applyRootRotationDuringAttack = GeisLocomotionTuningDefaults.ApplyRootRotationDuringAttack;
            _applyRootRotationDuringDodge = GeisLocomotionTuningDefaults.ApplyRootRotationDuringDodge;
            _dodgeInputDeadzone = GeisLocomotionTuningDefaults.DodgeInputDeadzone;
            _dodgeFallbackDuration = GeisLocomotionTuningDefaults.DodgeFallbackDuration;
            _requireMovementInputForDodge = GeisLocomotionTuningDefaults.RequireMovementInputForDodge;
            _dodgeScriptedPlaneSpeed = GeisLocomotionTuningDefaults.DodgeScriptedPlaneSpeed;
            _dodgeScriptedDuration = GeisLocomotionTuningDefaults.DodgeScriptedDuration;
            _attackMoveCancelStickThreshold = GeisLocomotionTuningDefaults.AttackMoveCancelStickThreshold;
            _attackRecoveryExitNormalizedTime = GeisLocomotionTuningDefaults.AttackRecoveryExitNormalizedTime;
            _attackExitVelocityCarry = GeisLocomotionTuningDefaults.AttackExitVelocityCarry;
            _dodgeRecoveryStartNormalizedTime = GeisLocomotionTuningDefaults.DodgeRecoveryStartNormalizedTime;
            _rollRecoveryStartNormalizedTime = GeisLocomotionTuningDefaults.RollRecoveryStartNormalizedTime;
            _dodgeInvulnerabilityEndNormalizedTime = GeisLocomotionTuningDefaults.DodgeInvulnerabilityEndNormalizedTime;
            _dodgeMoveCancelStickThreshold = GeisLocomotionTuningDefaults.DodgeMoveCancelStickThreshold;
            _dodgeExitVelocityCarry = GeisLocomotionTuningDefaults.DodgeExitVelocityCarry;
            _rollExitVelocityCarry = GeisLocomotionTuningDefaults.RollExitVelocityCarry;
            _inputBufferSeconds = GeisLocomotionTuningDefaults.InputBufferSeconds;
            _inputBuffers.InputBufferSeconds = _inputBufferSeconds;
            _dodgeDoubleTapWindow = GeisLocomotionTuningDefaults.DodgeDoubleTapWindow;
            _inputBuffers.DodgeDoubleTapWindow = _dodgeDoubleTapWindow;
            _dodgeDoubleTapRollEnabled = GeisLocomotionTuningDefaults.DodgeDoubleTapRollEnabled;
            _rollDistanceMultiplier = GeisLocomotionTuningDefaults.RollDistanceMultiplier;
            _rollInvulnerabilityEndNormalizedTime = GeisLocomotionTuningDefaults.RollInvulnerabilityEndNormalizedTime;
            _strafeStyleMaxPlanarSpeed = GeisLocomotionTuningDefaults.StrafeStyleMaxPlanarSpeed;
            _debugDodgeDoubleTap = GeisLocomotionTuningDefaults.DebugDodgeDoubleTap;
        }

        internal void ApplyGroundingDefaults()
        {
            _groundLayerMask = ~0;
            _groundedOffset = GeisLocomotionTuningDefaults.GroundedOffset;
        }

        internal void ApplyCapsuleDefaults()
        {
            _capsuleStandingHeight = GeisLocomotionTuningDefaults.CapsuleStandingHeight;
            _capsuleStandingCentre = GeisLocomotionTuningDefaults.CapsuleStandingCentre;
            _capsuleCrouchingHeight = GeisLocomotionTuningDefaults.CapsuleCrouchingHeight;
            _capsuleCrouchingCentre = GeisLocomotionTuningDefaults.CapsuleCrouchingCentre;
        }

        internal void ApplyStrafeInputDefaults()
        {
            _buttonHoldThreshold = GeisLocomotionTuningDefaults.ButtonHoldThreshold;
            _forwardStrafeMinThreshold = GeisLocomotionTuningDefaults.ForwardStrafeMinThreshold;
            _forwardStrafeMaxThreshold = GeisLocomotionTuningDefaults.ForwardStrafeMaxThreshold;
            _forwardStrafe = GeisLocomotionTuningDefaults.ForwardStrafe;
            _cameraRotationOffset = GeisLocomotionTuningDefaults.CameraRotationOffset;
        }

        internal void ApplyLookLeanDefaults()
        {
            _enableHeadTurn = true;
            _headLookDelay = 0f;
            _headLookXCurve = new AnimationCurve();
            _headLookLimitDegrees = GeisLocomotionTuningDefaults.HeadLookLimitDegrees;
            _bowAimHeadLookMultiplier = GeisLocomotionTuningDefaults.BowAimHeadLookMultiplier;
            _enableBodyTurn = true;
            _bodyLookDelay = 0f;
            _bodyLookXCurve = new AnimationCurve();
            _bowAimBodyLookMultiplier = GeisLocomotionTuningDefaults.BowAimBodyLookMultiplier;
            _enableLean = true;
            _leanDelay = 0f;
            _leanCurve = new AnimationCurve();
            _leansHeadLooksDelay = 0f;
        }

        internal void ApplyBowPresentationDefaults()
        {
            _bowEquipLayerBlendSpeed = GeisLocomotionTuningDefaults.BowEquipLayerBlendSpeed;
            _bowAimBodyEulerOffset = Vector3.zero;
            _bowPresenter.EquipLayerBlendSpeed = _bowEquipLayerBlendSpeed;
        }

        internal void ApplySpeedProfile(GeisLocomotionSpeedProfile profile)
        {
            _alwaysStrafe = profile.alwaysStrafe;
            _walkSpeed = profile.walkSpeed;
            _runSpeed = profile.runSpeed;
            _sprintSpeed = profile.sprintSpeed;
            _speedChangeDamping = profile.speedChangeDamping;
            _rotationSmoothing = profile.rotationSmoothing;
            _accelRate = profile.accelRate;
            _decelRate = profile.decelRate;
            _sprintInstantFraction = profile.sprintInstantFraction;
            _maxTurnDegPerSecond = profile.maxTurnDegPerSecond;
        }

        internal void ApplyAirMovementProfile(GeisAirMovementProfile profile)
        {
            _jumpForce = profile.jumpForce;
            _gravityMultiplier = profile.gravityMultiplier;
            _coyoteTimeSeconds = profile.coyoteTimeSeconds;
            _jumpBufferSeconds = profile.jumpBufferSeconds;
            _inputBuffers.JumpBufferSeconds = profile.jumpBufferSeconds;
        }

        internal void ApplyAttackDodgeProfile(GeisAttackDodgeLocomotionProfile profile)
        {
            _applyRootRotationDuringAttack = profile.applyRootRotationDuringAttack;
            _applyRootRotationDuringDodge = profile.applyRootRotationDuringDodge;
            _dodgeInputDeadzone = profile.dodgeInputDeadzone;
            _dodgeFallbackDuration = profile.dodgeFallbackDuration;
            _requireMovementInputForDodge = profile.requireMovementInputForDodge;
            _dodgeScriptedPlaneSpeed = profile.dodgeScriptedPlaneSpeed;
            _dodgeScriptedDuration = profile.dodgeScriptedDuration;
            _attackMoveCancelStickThreshold = profile.attackMoveCancelStickThreshold;
            _attackRecoveryExitNormalizedTime = profile.attackRecoveryExitNormalizedTime;
            _attackExitVelocityCarry = profile.attackExitVelocityCarry;
            _dodgeRecoveryStartNormalizedTime = profile.dodgeRecoveryStartNormalizedTime;
            _rollRecoveryStartNormalizedTime = profile.rollRecoveryStartNormalizedTime;
            _dodgeInvulnerabilityEndNormalizedTime = profile.dodgeInvulnerabilityEndNormalizedTime;
            _dodgeMoveCancelStickThreshold = profile.dodgeMoveCancelStickThreshold;
            _dodgeExitVelocityCarry = profile.dodgeExitVelocityCarry;
            _rollExitVelocityCarry = profile.rollExitVelocityCarry;
            _inputBufferSeconds = profile.inputBufferSeconds;
            _inputBuffers.InputBufferSeconds = profile.inputBufferSeconds;
            _dodgeDoubleTapWindow = profile.dodgeDoubleTapWindow;
            _inputBuffers.DodgeDoubleTapWindow = profile.dodgeDoubleTapWindow;
            _dodgeDoubleTapRollEnabled = profile.dodgeDoubleTapRollEnabled;
            _rollDistanceMultiplier = profile.rollDistanceMultiplier;
            _rollInvulnerabilityEndNormalizedTime = profile.rollInvulnerabilityEndNormalizedTime;
            _strafeStyleMaxPlanarSpeed = profile.strafeStyleMaxPlanarSpeed;
            _debugDodgeDoubleTap = profile.debugDodgeDoubleTap;
        }

        internal void ApplyGroundingProfile(GeisGroundingProfile profile)
        {
            _groundLayerMask = profile.groundLayerMask;
            _groundedOffset = profile.groundedOffset;
        }

        internal void ApplyCapsuleProfile(GeisPlayerCapsuleProfile profile)
        {
            _capsuleStandingHeight = profile.standingHeight;
            _capsuleStandingCentre = profile.standingCentre;
            _capsuleCrouchingHeight = profile.crouchingHeight;
            _capsuleCrouchingCentre = profile.crouchingCentre;
        }

        internal void ApplyStrafeInputProfile(GeisStrafeInputProfile profile)
        {
            _buttonHoldThreshold = profile.buttonHoldThreshold;
            _forwardStrafeMinThreshold = profile.forwardStrafeMinThreshold;
            _forwardStrafeMaxThreshold = profile.forwardStrafeMaxThreshold;
            _forwardStrafe = profile.forwardStrafe;
            _cameraRotationOffset = profile.cameraRotationOffset;
        }

        internal void ApplyLookLeanProfile(GeisLookLeanCurvesProfile profile)
        {
            _enableHeadTurn = profile.enableHeadTurn;
            _headLookDelay = profile.headLookDelay;
            _headLookXCurve = profile.headLookXCurve != null ? profile.headLookXCurve : new AnimationCurve();
            _headLookLimitDegrees = profile.headLookLimitDegrees;
            _bowAimHeadLookMultiplier = profile.bowAimHeadLookMultiplier;
            _enableBodyTurn = profile.enableBodyTurn;
            _bodyLookDelay = profile.bodyLookDelay;
            _bodyLookXCurve = profile.bodyLookXCurve != null ? profile.bodyLookXCurve : new AnimationCurve();
            _bowAimBodyLookMultiplier = profile.bowAimBodyLookMultiplier;
            _enableLean = profile.enableLean;
            _leanDelay = profile.leanDelay;
            _leanCurve = profile.leanCurve != null ? profile.leanCurve : new AnimationCurve();
            _leansHeadLooksDelay = profile.leansHeadLooksDelay;
        }

        internal void ApplyBowPresentationProfile(GeisBowPresentationProfile profile)
        {
            _bowEquipLayerBlendSpeed = profile.equipLayerBlendSpeed;
            _bowAimBodyEulerOffset = profile.aimBodyEulerOffset;
            _bowPresenter.EquipLayerBlendSpeed = _bowEquipLayerBlendSpeed;
        }

        private void ApplyLocomotionTuningFromProfiles()
        {
            GeisPlayerLocomotionProfileApplier.ApplyDefaults(this);

            var bundle = GeisPlayerLocomotionProfileApplier.ResolveBundle(_locomotionProfiles);
            if (bundle != null)
                GeisPlayerLocomotionProfileApplier.Apply(this, bundle);
        }

        private GeisBowAnimatorPresenter.Snapshot BuildBowPresentationSnapshot() =>
            new GeisBowAnimatorPresenter.Snapshot(
                _isBowDrawing,
                _bowDrawCharge,
                _isAiming,
                IsBowEquipped,
                _isBowChargedShotReady);

        private void ApplyBowParametersToAnimator() =>
            _bowPresenter.Apply(PresentationAnimator, BuildBowPresentationSnapshot(), Time.deltaTime);
    }
}
