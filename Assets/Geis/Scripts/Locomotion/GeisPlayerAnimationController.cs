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

// Geis of Anam - Copy of Synty SamplePlayerAnimationController as starting point.
// Original: Synty.AnimationBaseLocomotion.Samples.SamplePlayerAnimationController

using System;
using System.Collections.Generic;
using UnityEngine;
using Geis.Combat;
using Geis.InputSystem;
using Geis.InteractInput;
using Geis.Attributes;
using Geis.Animation;
using Geis.SoulRealm;
using RogueDeal.Combat;
using RogueDeal.Combat.Targeting;

namespace Geis.Locomotion
{
    /// <summary>
    /// Runs after <see cref="Puzzles.PlatformMover"/> (-50) so <see cref="GroundRideUtility"/> sees this frame&apos;s platform motion.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public partial class GeisPlayerAnimationController : MonoBehaviour, IAttackerPhaseProvider
    {
        #region Enum

        private enum AnimationState
        {
            Base,
            Locomotion,
            Jump,
            Fall,
            Crouch,
            Attack,
            Dodge
        }

        private enum GaitState
        {
            Idle,
            Walk,
            Run,
            Sprint
        }

        #endregion

        #region Animation Variable Hashes

        private readonly int _movementInputTappedHash = Animator.StringToHash("MovementInputTapped");
        private readonly int _movementInputPressedHash = Animator.StringToHash("MovementInputPressed");
        private readonly int _movementInputHeldHash = Animator.StringToHash("MovementInputHeld");
        private readonly int _shuffleDirectionXHash = Animator.StringToHash("ShuffleDirectionX");
        private readonly int _shuffleDirectionZHash = Animator.StringToHash("ShuffleDirectionZ");

        private readonly int _moveSpeedHash = Animator.StringToHash("MoveSpeed");
        private readonly int _currentGaitHash = Animator.StringToHash("CurrentGait");

        private readonly int _isJumpingAnimHash = Animator.StringToHash("IsJumping");
        private readonly int _fallingDurationHash = Animator.StringToHash("FallingDuration");

        private readonly int _inclineAngleHash = Animator.StringToHash("InclineAngle");

        private readonly int _strafeDirectionXHash = Animator.StringToHash("StrafeDirectionX");
        private readonly int _strafeDirectionZHash = Animator.StringToHash("StrafeDirectionZ");

        private readonly int _forwardStrafeHash = Animator.StringToHash("ForwardStrafe");
        private readonly int _cameraRotationOffsetHash = Animator.StringToHash("CameraRotationOffset");
        private readonly int _isStrafingHash = Animator.StringToHash("IsStrafing");
        private readonly int _isTurningInPlaceHash = Animator.StringToHash("IsTurningInPlace");

        private readonly int _isCrouchingHash = Animator.StringToHash("IsCrouching");

        private readonly int _isWalkingHash = Animator.StringToHash("IsWalking");
        private readonly int _isStoppedHash = Animator.StringToHash("IsStopped");
        private readonly int _isStartingHash = Animator.StringToHash("IsStarting");

        private readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");

        private readonly int _leanValueHash = Animator.StringToHash("LeanValue");
        private readonly int _headLookXHash = Animator.StringToHash("HeadLookX");
        private readonly int _headLookYHash = Animator.StringToHash("HeadLookY");

        private readonly int _bodyLookXHash = Animator.StringToHash("BodyLookX");
        private readonly int _bodyLookYHash = Animator.StringToHash("BodyLookY");

        private readonly int _locomotionStartDirectionHash = Animator.StringToHash("LocomotionStartDirection");

        private readonly int _attack1Hash = Animator.StringToHash("Attack_1");
        private readonly int _attackTriggerHash = Animator.StringToHash("Attack");
        private readonly int _comboStateHash = Animator.StringToHash("ComboState");
        private readonly int _comboStateBlendHash = Animator.StringToHash("ComboStateBlend");
        private const int COMBO_BLEND_SLOTS = 32;

        private readonly int _bowDrawingHash = Animator.StringToHash("BowDrawing");
        private readonly int _bowDrawChargeHash = Animator.StringToHash("BowDrawCharge");
        private readonly int _bowAimingHash = Animator.StringToHash("BowAiming");
        private readonly int _bowChargedShotReadyHash = Animator.StringToHash("BowChargedShotReady");
        private const string BowDrawLayerName = "Bow_Draw";
        private const float BowAimRootYawOffsetDegrees = 90f;

        private readonly int _dodgeDirectionHash = Animator.StringToHash("DodgeDirection");
        private readonly int _dodgeTriggerHash = Animator.StringToHash("Dodge");
        private readonly int _rollTriggerHash = LocomotionAnimatorIds.RollTrigger;

        /// <summary>Layer 0 leaf state shortNameHashes for sidestep clips nested under the <c>Dodge</c> sub-state machine.</summary>
        private static readonly int _dodgeLeafFrontHash = LocomotionAnimatorIds.DodgeLeafFront;
        private static readonly int _dodgeLeafBackHash = LocomotionAnimatorIds.DodgeLeafBack;
        private static readonly int _dodgeLeafLeftHash = LocomotionAnimatorIds.DodgeLeafLeft;
        private static readonly int _dodgeLeafRightHash = LocomotionAnimatorIds.DodgeLeafRight;
        /// <summary>Full layer-0 paths for sidestep clips nested under the <c>Dodge</c> sub-state machine.</summary>
        private static readonly int _dodgeNestedFrontHash = Animator.StringToHash("Dodge.Dodge_Front");
        private static readonly int _dodgeNestedBackHash = Animator.StringToHash("Dodge.Dodge_Back");
        private static readonly int _dodgeNestedLeftHash = Animator.StringToHash("Dodge.Dodge_Left");
        private static readonly int _dodgeNestedRightHash = Animator.StringToHash("Dodge.Dodge_Right");
        /// <summary>Base-layer roll clips (double-tap); distinct from sidestep <c>Dodge_*_Root</c> leaves.</summary>
        private static readonly int _rollLeafForwardHash = LocomotionAnimatorIds.RollLeafForward;
        private static readonly int _rollLeafBackHash = LocomotionAnimatorIds.RollLeafBack;
        private static readonly int _rollLeafLeftHash = LocomotionAnimatorIds.RollLeafLeft;
        private static readonly int _rollLeafRightHash = LocomotionAnimatorIds.RollLeafRight;
        private static readonly int _rollNestedForwardHash = LocomotionAnimatorIds.RollNestedForward;
        private static readonly int _rollNestedBackHash = LocomotionAnimatorIds.RollNestedBack;
        private static readonly int _rollNestedLeftHash = LocomotionAnimatorIds.RollNestedLeft;
        private static readonly int _rollNestedRightHash = LocomotionAnimatorIds.RollNestedRight;
        /// <summary>Layer 0 leaf state shortNameHash for the data-driven combo <c>Attack</c> blend tree.</summary>
        private static readonly int _attackLeafHash = Animator.StringToHash("Attack");

        #endregion

        #region Player Settings Variables

        #region Scripts/Objects

        [Tooltip("Script controlling camera behavior")]
        [SerializeField]
        private GeisCameraController _cameraController;
        [Tooltip("InputReader handles player input")]
        [SerializeField]
        private GeisInputReader _inputReader;
        [Tooltip("Animator component for controlling player animations")]
        [SerializeField]
        private Animator _animator;
        [Tooltip("Character Controller component for controlling player movement")]
        [SerializeField]
        private CharacterController _controller;

        /// <summary>True when the humanoid Animator lives on a child (e.g. GeisCharacter) while this script is on the player root.</summary>
        private bool _animatorIsOnChild;

        #endregion

        #region Locomotion Settings

        [Tooltip("Whether the character always faces the camera facing direction")]
        [SerializeField]
        private bool _alwaysStrafe = true;
        [Tooltip("Slowest movement speed of the player when set to a walk state or half press tick")]
        [SerializeField]
        private float _walkSpeed = 1.4f;
        [Tooltip("Default movement speed of the player")]
        [SerializeField]
        private float _runSpeed = 2.5f;
        [Tooltip("Top movement speed of the player")]
        [SerializeField]
        private float _sprintSpeed = 7f;
        [Tooltip("Damping factor for changing speed (fallback when accel/decel are equal or disabled)")]
        [SerializeField]
        private float _speedChangeDamping = 10f;
        [Tooltip("Rate toward target planar speed when accelerating (higher = snappier starts). Frame-rate stable.")]
        [SerializeField]
        private float _accelRate = 25f;
        [Tooltip("Rate toward target planar speed when decelerating (higher = snappier stops). Frame-rate stable.")]
        [SerializeField]
        private float _decelRate = 15f;
        [Tooltip("When target max speed increases (e.g. sprint pressed), snap _currentMaxSpeed up to this fraction of the new target immediately, then continue smoothing. 1 = instant; 0 = disabled.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _sprintInstantFraction = 0.85f;
        [Tooltip("Rotation smoothing factor.")]
        [SerializeField]
        private float _rotationSmoothing = 10f;
        [Tooltip("Maximum root yaw rotation speed (deg/sec). Caps how long large redirects take even when smoothing factor would otherwise stall.")]
        [SerializeField]
        private float _maxTurnDegPerSecond = 720f;
        [Tooltip("Offset for camera rotation.")]
        [SerializeField]
        private float _cameraRotationOffset;
        [Tooltip("Local euler offset (degrees) applied after the bow base facing. While bow aiming/drawing, only Y (yaw) is applied to the root; pitch/roll should come from the upper-body bow layer.")]
        [SerializeField]
        private Vector3 _bowAimBodyEulerOffset;
        #endregion

        #region Shuffle Settings

        [Tooltip("Threshold for button hold duration.")]
        [SerializeField]
        private float _buttonHoldThreshold = 0.15f;
        [Tooltip("Direction of shuffling on the X-axis.")]
        [SerializeField]
        private float _shuffleDirectionX;
        [Tooltip("Direction of shuffling on the Z-axis.")]
        [SerializeField]
        private float _shuffleDirectionZ;

        #endregion

        #region Capsule Settings

        [Tooltip("Standing height of the player capsule.")]
        [SerializeField]
        private float _capsuleStandingHeight = 1.8f;
        [Tooltip("Standing center of the player capsule.")]
        [SerializeField]
        private float _capsuleStandingCentre = 0.93f;
        [Tooltip("Crouching height of the player capsule.")]
        [SerializeField]
        private float _capsuleCrouchingHeight = 1.2f;
        [Tooltip("Crouching center of the player capsule.")]
        [SerializeField]
        private float _capsuleCrouchingCentre = 0.6f;

        #endregion

        #region Strafing

        [Tooltip("Minimum threshold for forward strafing angle.")]
        [SerializeField]
        private float _forwardStrafeMinThreshold = -55.0f;
        [Tooltip("Maximum threshold for forward strafing angle.")]
        [SerializeField]
        private float _forwardStrafeMaxThreshold = 125.0f;
        [Tooltip("Current forward strafing value.")]
        [SerializeField]
        private float _forwardStrafe = 1f;

        #endregion

        #region Grounded Settings

        [Tooltip("Position of the rear ray for grounded angle check.")]
        [SerializeField]
        private Transform _rearRayPos;
        [Tooltip("Position of the front ray for grounded angle check.")]
        [SerializeField]
        private Transform _frontRayPos;
        [Tooltip("Layer mask for checking ground. Default: all layers. If ground isn't detected, ensure your ground has a collider and is on a layer included here.")]
        [SerializeField]
        private LayerMask _groundLayerMask = ~0;
        [Tooltip("Current incline angle.")]
        [SerializeField]
        private float _inclineAngle;
        [Tooltip("Offset below character center for ground check sphere. Positive = below feet for detection.")]
        [SerializeField]
        private float _groundedOffset = 0.14f;

        #endregion

        #region In-Air Settings

        [Tooltip("Force applied when the player jumps.")]
        [SerializeField]
        private float _jumpForce = 10f;
        [Tooltip("Multiplier for gravity when in the air.")]
        [SerializeField]
        private float _gravityMultiplier = 2f;
        [Tooltip("Duration of falling.")]
        [SerializeField]
        private float _fallingDuration;
        [Tooltip("Seconds after leaving the ground the player can still jump (coyote time). 0 disables.")]
        [SerializeField]
        private float _coyoteTimeSeconds = 0.10f;
        [Tooltip("Seconds a jump press stays live so one pressed just before landing still triggers the jump. 0 disables.")]
        [SerializeField]
        private float _jumpBufferSeconds = 0.15f;

        #endregion

        #region Head Look Settings

        [Tooltip("Flag indicating if head turning is enabled.")]
        [SerializeField]
        private bool _enableHeadTurn = true;
        [Tooltip("Delay for head turning.")]
        [SerializeField]
        private float _headLookDelay;
        [Tooltip("X-axis value for head turning.")]
        [SerializeField]
        private float _headLookX;
        [Tooltip("Y-axis value for head turning.")]
        [SerializeField]
        private float _headLookY;
        [Tooltip("Curve for X-axis head turning.")]
        [SerializeField]
        private AnimationCurve _headLookXCurve;
        [Tooltip("Degrees beyond which head/body look can't follow; character rotates in place instead. Tune to match animator head look limit.")]
        [SerializeField]
        private float _headLookLimitDegrees = 60f;
        [Tooltip("Light reduction applied to head-look additives while bow aiming/drawing. 1 = unchanged, 0 = disabled.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _bowAimHeadLookMultiplier = 0.6f;

        #endregion

        #region Body Look Settings

        [Tooltip("Flag indicating if body turning is enabled.")]
        [SerializeField]
        private bool _enableBodyTurn = true;
        [Tooltip("Delay for body turning.")]
        [SerializeField]
        private float _bodyLookDelay;
        [Tooltip("X-axis value for body turning.")]
        [SerializeField]
        private float _bodyLookX;
        [Tooltip("Y-axis value for body turning.")]
        [SerializeField]
        private float _bodyLookY;
        [Tooltip("Curve for X-axis body turning.")]
        [SerializeField]
        private AnimationCurve _bodyLookXCurve;
        [Tooltip("Light reduction applied to body-look additives while bow aiming/drawing. 1 = unchanged, 0 = disabled.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _bowAimBodyLookMultiplier = 0.4f;

        #endregion

        #region Lean Settings

        [Tooltip("Flag indicating if leaning is enabled.")]
        [SerializeField]
        private bool _enableLean = true;
        [Tooltip("Delay for leaning.")]
        [SerializeField]
        private float _leanDelay;
        [Tooltip("Current value for leaning.")]
        [SerializeField]
        private float _leanValue;
        [Tooltip("Curve for leaning.")]
        [SerializeField]
        private AnimationCurve _leanCurve;
        [Tooltip("Delay for head leaning looks.")]
        [SerializeField]
        private float _leansHeadLooksDelay;
        [Tooltip("Flag indicating if an animation clip has ended.")]
        [SerializeField]
        private bool _animationClipEnd;

        #endregion

        #region Attack Settings

        /// <summary>
        /// Fired when an attack is triggered (first hit or combo continuation).
        /// Subscribe from GeisCombatBridge to apply RogueDeal damage/hit detection.
        /// </summary>
        public event Action<int> OnAttackPerformed;

        /// <summary>
        /// Current data-driven combo step (0 = first hit). Aligns with GeisComboData clip index for combat/hit timing.
        /// </summary>
        public int CurrentComboState => _currentComboState;

        [Tooltip("Apply animation root rotation during attacks. Disable if attacks drift left/right (baked rotation mismatch).")]
        [SerializeField]
        private bool _applyRootRotationDuringAttack;

        [Tooltip("Combo data (transitions + clips). When null, uses legacy Attack_1 if available.")]
        [SerializeField]
        private GeisComboData _comboData;
        [Tooltip("Optional: resolves combo by weapon index when set. Takes precedence over _comboData when both assigned.")]
        [SerializeField]
        private GeisWeaponComboData _weaponComboData;
        [Tooltip("Optional: provides current weapon index for _weaponComboData lookup.")]
        [SerializeField]
        private GeisWeaponSwitcher _weaponSwitcher;
        [Tooltip("Optional: placeholders for runtime override. Loaded from Resources/GeisComboPlaceholders if null.")]
        [SerializeField]
        private GeisComboPlaceholders _comboPlaceholders;

        [Tooltip("Apply animation root rotation during dodge clips.")]
        [SerializeField]
        private bool _applyRootRotationDuringDodge;
        [Tooltip("How quickly the Bow_Draw upper-body layer blends in/out when equipping or unequipping the bow. Higher = snappier.")]
        [SerializeField]
        private float _bowEquipLayerBlendSpeed = 8f;
        [Tooltip("Stick magnitude below this counts as neutral (backstep / away from lock-on target).")]
        [SerializeField]
        private float _dodgeInputDeadzone = 0.05f;
        [Tooltip("Fallback seconds if clip length cannot be read.")]
        [SerializeField]
        private float _dodgeFallbackDuration = 1.2f;
        [Tooltip("If true, dodge only when movement stick exceeds deadzone.")]
        [SerializeField]
        private bool _requireMovementInputForDodge;
        [Tooltip("Soul-ghost scripted dodge planar speed; physical dodge uses animation clips.")]
        [SerializeField]
        private float _dodgeScriptedPlaneSpeed = 7f;
        [Tooltip("Soul-ghost scripted dodge duration in seconds.")]
        [SerializeField]
        private float _dodgeScriptedDuration = 0.35f;

        [Header("Cancel Windows (Action-feel)")]
        [Tooltip("Min stick magnitude (0-1) to move-cancel an attack during its cancel window.")]
        [SerializeField]
        private float _attackMoveCancelStickThreshold = 0.5f;
        [Tooltip("Normalized time on the attack clip after which the state exits back to Locomotion (if no buffered combo/dodge input).")]
        [SerializeField]
        private float _attackRecoveryExitNormalizedTime = 0.85f;
        [Tooltip("Fraction of _currentMaxSpeed pre-seeded onto planar velocity when move-cancelling an attack.")]
        [SerializeField]
        private float _attackExitVelocityCarry = 0.6f;
        [Tooltip("Normalized time on the dodge clip after which recovery cancels are allowed.")]
        [SerializeField]
        private float _dodgeRecoveryStartNormalizedTime = GeisLocomotionTuningDefaults.DodgeRecoveryStartNormalizedTime;
        [Tooltip("Normalized time on a roll clip after which recovery cancels are allowed.")]
        [SerializeField]
        private float _rollRecoveryStartNormalizedTime = GeisLocomotionTuningDefaults.RollRecoveryStartNormalizedTime;
        [Tooltip("Normalized time on the dodge leaf clip while the player has dodge i-frames (CombatStrikeResolver).")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _dodgeInvulnerabilityEndNormalizedTime = 0.38f;
        [Tooltip("Normalized time on a roll clip while the player has dodge i-frames (longer coverage than sidestep).")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _rollInvulnerabilityEndNormalizedTime = GeisLocomotionTuningDefaults.RollInvulnerabilityEndNormalizedTime;
        [Tooltip("Min stick magnitude (0-1) to move-cancel a dodge during its recovery window.")]
        [SerializeField]
        private float _dodgeMoveCancelStickThreshold = 0.3f;
        [Tooltip("Fraction of _currentMaxSpeed pre-seeded onto planar velocity when exiting a sidestep into movement.")]
        [SerializeField]
        private float _dodgeExitVelocityCarry = 0.75f;
        [Tooltip("Same as dodge exit carry, but for rolls. Keep at 0 so the roll root-motion arc stops cleanly.")]
        [SerializeField]
        private float _rollExitVelocityCarry = GeisLocomotionTuningDefaults.RollExitVelocityCarry;
        [Tooltip("Seconds a light/heavy/dodge input stays live in the buffer, so presses just before a cancel window still register.")]
        [SerializeField]
        private float _inputBufferSeconds = 0.18f;
        [Tooltip("Max seconds between two dodge presses to count as a double-tap (triggers the roll variant).")]
        [SerializeField]
        private float _dodgeDoubleTapWindow = GeisLocomotionTuningDefaults.DodgeDoubleTapWindow;
        [Tooltip("If true, double-tapping dodge performs a directional roll (dedicated roll clips per direction).")]
        [SerializeField]
        private bool _dodgeDoubleTapRollEnabled = true;
        [Tooltip("Multiplier applied to roll horizontal root-motion travel. 1 = baked distance; >1 rolls further; <1 rolls shorter. Vertical (gravity) travel is never scaled.")]
        [Range(0.25f, 3f)]
        [SerializeField]
        private float _rollDistanceMultiplier = GeisLocomotionTuningDefaults.RollDistanceMultiplier;
        [Tooltip("Maximum planar speed (m/s) that uses camera-forward strafe facing and strafe-style dodges. Above this (and while sprinting) the body turns with velocity.")]
        [SerializeField]
        private float _strafeStyleMaxPlanarSpeed = GeisLocomotionTuningDefaults.StrafeStyleMaxPlanarSpeed;
        [Tooltip("Logs each dodge press with its double-tap detection result. Leave off in shipping.")]
        [SerializeField]
        private bool _debugDodgeDoubleTap;

        #endregion

        #endregion

        #region Runtime Properties

        private readonly List<GameObject> _currentTargetCandidates = new List<GameObject>();
        private AnimationState _currentState = AnimationState.Base;
        private bool _cannotStandUp;
        private bool _crouchKeyPressed;
        private bool _isAiming;
        private bool _isBowDrawing;
        private bool _isBowChargedShotReady;
        private float _bowDrawCharge;
        private bool _animatorHasBowDrawing;
        private bool _animatorHasBowDrawCharge;
        private bool _animatorHasBowAiming;
        private bool _animatorHasBowChargedShotReady;
        private int _bowDrawLayerIndex = -1;
        private float _currentBowDrawLayerWeight;
        private bool _isCrouching;
        private bool _isGrounded = true;
        private Transform _groundRideSurface;
        private Vector3 _groundRideLastWorldPos;
        private bool _isLockedOn;
        private bool _isSliding;
        private bool _isSprinting;
        private bool _isStarting;
        private bool _isStopped = true;
        private bool _isStrafing;
        private bool _isTurningInPlace;
        private bool _isIdleLooking;
        private bool _isWalking;
        private bool _movementInputHeld;
        private bool _movementInputPressed;
        private bool _movementInputTapped;
        private float _currentMaxSpeed;
        private float _locomotionStartDirection;
        private float _locomotionStartTimer;
        private float _lookingAngle;
        private float _newDirectionDifferenceAngle;
        private float _speed2D;
        private float _strafeAngle;
        private float _strafeDirectionX;
        private float _strafeDirectionZ;
        private GameObject _currentLockOnTarget;
        private GaitState _currentGait;
        [SerializeField] private LockOnIndicator _lockOnIndicator;
        private Transform _targetLockOnPos;
        private bool _ownsTargetLockOnPos;
        private Vector3 _currentRotation = new Vector3(0f, 0f, 0f);
        private Vector3 _moveDirection;
        private Vector3 _previousRotation;
        private Vector3 _velocity;

        private float _attackStateTimeout;
        private float _dodgeStateTimeout;
        /// <summary>Seconds of coyote time remaining: counts up to <see cref="_coyoteTimeSeconds"/> while grounded, decrements in air.</summary>
        private float _coyoteTimer;
        /// <summary>Unscaled time the most recent jump press was buffered (-1 = no pending press).</summary>
        private float _jumpBufferedAt = -1f;
        private bool _loggedDodgeAnimatorMissing;
        private bool _loggedForwardRollMissing;
        /// <summary>True if dodge started while strafing — keep camera-relative facing instead of snapping to dodge axis.</summary>
        private bool _dodgePreserveStrafeFacing;
        /// <summary>Set once layer 0 actually enters a Dodge_* clip (avoids exiting before Any-State transition fires).</summary>
        private bool _dodgeAnimatorEnteredLeaf;
        /// <summary>Set once layer 0 actually enters the Attack combo clip (avoids reading locomotion normalizedTime).</summary>
        private bool _attackAnimatorEnteredLeaf;

        // Data-driven combo
        private int _currentComboState;
        private GeisComboInputType? _comboInputBuffered;
        /// <summary>Unscaled time the current <see cref="_comboInputBuffered"/> was set. Used by <see cref="IsBufferFresh"/>.</summary>
        private float _comboInputBufferedAt = -1f;
        /// <summary>Unscaled time the most recent dodge press was buffered (-1 = no pending buffer). Enables attack→dodge cancel.</summary>
        private float _dodgeInputBufferedAt = -1f;
        /// <summary>When <see cref="_dodgeInputBufferedAt"/> is fresh, whether that buffered dodge should spawn as a forward roll (double-tap).</summary>
        private bool _dodgeInputBufferIsRoll;
        /// <summary>Unscaled time of the most recent dodge press (any state), used for double-tap detection. -1 = no prior press.</summary>
        private float _lastDodgeTapAt = -1f;
        /// <summary>One-shot request consumed by <see cref="EnterDodgeState"/>; set by whichever path triggers the next dodge to request a roll.</summary>
        private bool _dodgeRequestIsRoll;
        /// <summary>True while the current Dodge state is playing a roll clip (double-tap).</summary>
        private bool _dodgeIsRoll;
        /// <summary>Animator direction index (0–3) for the active dodge/roll clip.</summary>
        private int _dodgeAnimatorDir;
        /// <summary>Unscaled time <see cref="EnterDodgeState"/> last ran; used for double-tap roll upgrades.</summary>
        private float _dodgeStateEnteredAtUnscaled = -1f;
        /// <summary>After a sidestep starts, a second dodge press before this time (unscaled) upgrades to / starts a roll.</summary>
        private float _dodgeRollFollowUpExpiresAtUnscaled = -1f;
        private GeisComboInputType _firstAttackInputType;
        private bool _useDataDrivenCombo;
        private AnimatorOverrideController _comboOverrideController;
        private GeisComboData _lastAppliedComboData;

        #endregion

        #region Soul realm (spectral mesh mirror)

        public GeisCameraController CameraControllerRef => _cameraController;
        public float LocomotionWalkSpeed => _walkSpeed;
        public float LocomotionRunSpeed => _runSpeed;
        public float LocomotionSprintSpeed => _sprintSpeed;
        public bool LocomotionAlwaysStrafe => _alwaysStrafe;
        public float LocomotionSpeedChangeDamping => _speedChangeDamping;
        public float LocomotionRotationSmoothing => _rotationSmoothing;
        public float LocomotionForwardStrafeMinThreshold => _forwardStrafeMinThreshold;
        public float LocomotionForwardStrafeMaxThreshold => _forwardStrafeMaxThreshold;
        public float LocomotionButtonHoldThreshold => _buttonHoldThreshold;

        /// <summary>Max-speed blend rate; must stay in sync with <c>_ANIMATION_DAMP_TIME</c> (soul ghost motor).</summary>
        public float LocomotionMaxSpeedLerpRate => 5f;

        public bool LocomotionIsWalking => _isWalking;
        public bool LocomotionIsSprinting => _isSprinting;
        public bool LocomotionIsCrouching => _isCrouching;

        /// <summary>Planar velocity from last locomotion tick (used to sync soul ghost on realm entry).</summary>
        public Vector3 LocomotionPlanarVelocity => new Vector3(_velocity.x, 0f, _velocity.z);

        /// <summary>Vertical velocity from last locomotion tick (sync soul ghost when entering mid-jump / fall).</summary>
        public float LocomotionVerticalVelocity => _velocity.y;

        /// <summary>
        /// Called when entering soul realm: locomotion <see cref="Update"/> is suppressed; use this so the ghost
        /// reads a defined walk/run state via <see cref="EnableWalk"/> instead of a stale frozen flag.
        /// </summary>
        public void SetWalkLocomotionForSoulRealm(bool walkEnabled)
        {
            EnableWalk(walkEnabled);
        }
        public bool LocomotionIsStrafing => _isStrafing;

        /// <summary>
        /// Strafe-style facing (camera-forward) vs velocity-facing. Applies while planar speed is at or below
        /// <see cref="_strafeStyleMaxPlanarSpeed"/> and sprint is not held. Aim/draw always uses strafe facing.
        /// </summary>
        private bool UseStrafeStyleLocomotionFacing =>
            (_isStrafing || _isAiming || _isBowDrawing)
            && (_isAiming || _isBowDrawing || IsWithinStrafeStyleSpeedCap());

        /// <summary>True when the body should stay camera-forward (strafe) rather than turn into velocity.</summary>
        private bool IsWithinStrafeStyleSpeedCap(float planarSpeed = -1f)
        {
            if (_isSprinting)
                return false;

            if (planarSpeed < 0f)
                planarSpeed = _speed2D;

            float cap = _strafeStyleMaxPlanarSpeed;
            return planarSpeed <= cap + 0.001f || _currentMaxSpeed <= cap + 0.001f;
        }

        /// <summary>Same value as the <c>IsStrafing</c> animator float — for spectral mirror / tooling.</summary>
        public bool LocomotionAnimatorUsesStrafeStyle => UseStrafeStyleLocomotionFacing;
        /// <summary>True while aim (LT) is held. Melee weapons may still set this, so camera-specific behavior should prefer bow-gated helpers.</summary>
        public bool IsAiming => _isAiming;

        /// <summary>True while bow RT (heavy attack) is held (draw). Drives optional <c>BowDrawing</c> / <c>BowDrawCharge</c> animator parameters.</summary>
        public bool IsBowDrawing => _isBowDrawing;

        /// <summary>True once the held bow draw has reached the charged-shot shake threshold.</summary>
        public bool IsBowChargedShotReady => _isBowChargedShotReady;

        /// <summary>0–1 bow draw charge from <see cref="SetBowDrawState"/> — mirrored onto the spectral animator in soul realm.</summary>
        public float BowDrawChargeNormalized => _bowDrawCharge;

        /// <summary>True when the currently equipped weapon definition is the bow.</summary>
        public bool IsBowEquipped =>
            _weaponSwitcher != null && _weaponSwitcher.CurrentWeaponDefinition != null
            && _weaponSwitcher.CurrentWeaponDefinition.IsBowWeapon;

        /// <summary>True when the camera should use the tighter aim shoulder rig/FOV.</summary>
        public bool ShouldUseBowAimZoom => IsBowEquipped && _isAiming;

        private bool IsBowMovementForcedWalk =>
            IsBowEquipped && (_isAiming || _isBowDrawing);

        /// <summary>True while a soul-realm melee swing is using the shared combo timeout (spectral animator).</summary>
        public bool IsSoulRealmMeleeAnimating =>
            SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive && _attackStateTimeout > 0f;

        /// <summary>True while the locomotion state machine is in <see cref="AnimationState.Attack"/>.</summary>
        public bool IsInAttackState => _currentState == AnimationState.Attack;

        /// <summary>When true, locomotion animator zeros MoveSpeed/gait in air (Jump/Fall states). Used by spectral mirror.</summary>
        public bool LocomotionAirGaitForAnimator =>
            _currentState == AnimationState.Jump || _currentState == AnimationState.Fall;

        /// <summary>
        /// Maps elapsed air time to <c>Falling_BlendTree</c> (FallShort 0.3 → FallLarge 2).
        /// </summary>
        public float GetFallingBlendParameter(float elapsedAirSeconds)
        {
            float ramp = GeisLocomotionTuningDefaults.FallingBlendRampSeconds;
            if (ramp <= 0.0001f)
                return 2f;
            float t = Mathf.Clamp01(elapsedAirSeconds / ramp);
            return Mathf.Lerp(0.3f, 2f, t);
        }

        /// <summary>Stick magnitude below this counts as neutral for dodge direction (mirrors body for soul ghost).</summary>
        public float LocomotionDodgeDeadzone => _dodgeInputDeadzone;

        /// <summary>Soul-ghost scripted dodge (physical dodge uses animator root motion).</summary>
        public float LocomotionDodgeScriptedPlaneSpeed => _dodgeScriptedPlaneSpeed;

        /// <summary>Soul-ghost scripted dodge duration.</summary>
        public float LocomotionDodgeScriptedDuration => _dodgeScriptedDuration;

        /// <summary>When true, dodge only if move stick exceeds <see cref="LocomotionDodgeDeadzone"/> (spectral ghost mirrors this).</summary>
        public bool LocomotionDodgeRequiresMovementInput => _requireMovementInputForDodge;

        public float LocomotionJumpForce => _jumpForce;
        public float LocomotionGravityMultiplier => _gravityMultiplier;
        public float LocomotionGroundedOffset => _groundedOffset;
        public LayerMask LocomotionGroundLayerMask => _groundLayerMask;

        public float LocomotionCapsuleStandingHeight => _capsuleStandingHeight;
        public float LocomotionCapsuleStandingCentre => _capsuleStandingCentre;
        public float LocomotionCapsuleCrouchingHeight => _capsuleCrouchingHeight;
        public float LocomotionCapsuleCrouchingCentre => _capsuleCrouchingCentre;

        /// <summary>Standing capsule radius (from CharacterController).</summary>
        public float LocomotionCapsuleRadius => _controller != null ? _controller.radius : 0.28f;

        /// <summary>
        /// Jump/Fall never simulate while soul realm suppresses <see cref="Update"/> — reset state and vertical velocity so
        /// exiting does not leave stuck air velocity or an animator jump state that only resolves on attack.
        /// </summary>
        public void PrepareBodyAfterSoulRealmExit()
        {
            _attackStateTimeout = 0f;
            _attackAnimatorEnteredLeaf = false;
            ClearComboInputBuffer();
            _dodgeInputBufferedAt = -1f;
            _dodgeInputBufferIsRoll = false;
            _dodgeRequestIsRoll = false;
            _lastDodgeTapAt = -1f;
            _dodgeRollFollowUpExpiresAtUnscaled = -1f;
            _currentComboState = 0;

            if (_currentState == AnimationState.Jump || _currentState == AnimationState.Fall)
                SwitchState(AnimationState.Locomotion);

            GroundedCheck();
            _velocity.y = _isGrounded ? -2f : 0f;
        }

        /// <summary>Clears attack/dodge state when entering soul realm so spectral combat starts from a known baseline.</summary>
        public void ResetCombatStateForSoulRealmEntry()
        {
            _attackStateTimeout = 0f;
            _attackAnimatorEnteredLeaf = false;
            ClearComboInputBuffer();
            _dodgeInputBufferedAt = -1f;
            _dodgeInputBufferIsRoll = false;
            _dodgeRequestIsRoll = false;
            _lastDodgeTapAt = -1f;
            _dodgeRollFollowUpExpiresAtUnscaled = -1f;
            _currentComboState = 0;
            if (_currentState == AnimationState.Attack || _currentState == AnimationState.Dodge)
                SwitchState(AnimationState.Locomotion);
        }

        /// <summary>Ends the current melee swing when damaged (hit reaction plays on the HitReaction layer).</summary>
        public void InterruptAttackFromIncomingHit()
        {
            _attackStateTimeout = 0f;
            _attackAnimatorEnteredLeaf = false;
            ClearComboInputBuffer();
            _dodgeInputBufferedAt = -1f;
            _dodgeInputBufferIsRoll = false;
            _currentComboState = 0;

            if (_currentState == AnimationState.Attack)
                SwitchState(AnimationState.Locomotion);
        }

        bool IAttackerPhaseProvider.TryGetCurrentAttackPhase(out GeisComboAttackPhase phase)
        {
            phase = GeisComboAttackPhase.Recovery;
            if (_currentState != AnimationState.Attack || _animator == null)
                return false;

            GeisComboData comboData = GetCurrentComboData();
            if (comboData == null)
                return false;

            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = info.length > 0.01f ? info.normalizedTime % 1f : 0f;
            phase = comboData.GetAttackPhase(_currentComboState, normalizedTime);
            return true;
        }

        bool IAttackerPhaseProvider.HasSuperArmorDuringCurrentStartup
        {
            get
            {
                if (_currentState != AnimationState.Attack)
                    return false;

                GeisComboData comboData = GetCurrentComboData();
                if (comboData == null)
                    return false;

                if (!((IAttackerPhaseProvider)this).TryGetCurrentAttackPhase(out GeisComboAttackPhase phase))
                    return false;

                return phase == GeisComboAttackPhase.Startup
                    && comboData.HasSuperArmorDuringStartup(_currentComboState);
            }
        }

        bool IAttackerPhaseProvider.DodgeOnlyAvoidsDuringActivePhase
        {
            get
            {
                GeisComboData comboData = GetCurrentComboData();
                return comboData == null || comboData.DodgeOnlyAvoidsDuringActivePhase(_currentComboState);
            }
        }

        #endregion

        #region Base State Variables

        private const float _ANIMATION_DAMP_TIME = 5f;
        private const float _STRAFE_DIRECTION_DAMP_TIME = 20f;
        private float _targetMaxSpeed;
        private float _fallStartTime;
        private float _rotationRate;
        private float _initialLeanValue;
        private float _initialTurnValue;
        private Vector3 _cameraForward;
        private Vector3 _targetVelocity;

        #endregion

        #region Animation Controller

        #region Start

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            UnityEditor.EditorApplication.delayCall -= EnsurePersistentLockOnIndicatorEditor;
            UnityEditor.EditorApplication.delayCall += EnsurePersistentLockOnIndicatorEditor;
        }

        private void EnsurePersistentLockOnIndicatorEditor()
        {
            if (this == null)
                return;

            EnsurePersistentLockOnIndicator();
        }
#endif

        /// <inheritdoc cref="Start" />
        private PlayerDefensiveCombatState _defensiveCombatState;

        private void EnsureDefensiveCombatState()
        {
            if (_defensiveCombatState == null)
                _defensiveCombatState = GetComponent<PlayerDefensiveCombatState>();
            if (_defensiveCombatState == null)
                _defensiveCombatState = gameObject.AddComponent<PlayerDefensiveCombatState>();
        }

        private void UpdateDodgeInvulnerabilityFromAnimator()
        {
            EnsureDefensiveCombatState();
            if (_defensiveCombatState == null || _animator == null)
                return;

            bool invuln = false;
            if (_currentState == AnimationState.Dodge)
            {
                AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
                if (_dodgeAnimatorEnteredLeaf
                    && IsDodgeLeafShortNameHash(info.shortNameHash)
                    && !_animator.IsInTransition(0)
                    && info.length > 0.01f)
                {
                    float t = info.normalizedTime % 1f;
                    float invulnEnd = _dodgeIsRoll
                        ? _rollInvulnerabilityEndNormalizedTime
                        : _dodgeInvulnerabilityEndNormalizedTime;
                    invuln = t < invulnEnd;
                }
            }

            _defensiveCombatState.SetDodgeInvulnerable(invuln);
        }

        private void Awake()
        {
            EnsureComponentReferences();
        }

        private void EnsureComponentReferences()
        {
            if (_inputReader == null)
                _inputReader = GetComponent<GeisInputReader>();
            if (_controller == null)
                _controller = GetComponent<CharacterController>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
            if (_cameraController == null)
                _cameraController = FindFirstObjectByType<GeisCameraController>();
            if (_controller == null)
                _controller = GetComponentInParent<CharacterController>();

            _animatorIsOnChild = _animator != null && _animator.transform != transform;
        }

        private void Start()
        {
            EnsureComponentReferences();
            EnsureDefensiveCombatState();
            EnsurePersistentLockOnIndicator();

            _targetLockOnPos = EnsureWorldSpaceLockOnAnchor();

            _inputReader.onLockOnToggled += ToggleLockOn;
            _inputReader.onLockOnCycleLeft += CycleLockOnLeft;
            _inputReader.onLockOnCycleRight += CycleLockOnRight;
            _inputReader.onSprintActivated += ActivateSprint;
            _inputReader.onSprintDeactivated += DeactivateSprint;
            _inputReader.onCrouchActivated += ActivateCrouch;
            _inputReader.onCrouchDeactivated += DeactivateCrouch;
            _inputReader.onAimActivated += ActivateAim;
            _inputReader.onAimDeactivated += DeactivateAim;
            _inputReader.onLightAttackPerformed += OnLightAttackRequested;
            _inputReader.onHeavyAttackPerformed += OnHeavyAttackRequested;
            _inputReader.onDodgePerformed += OnDodgeRequested;
            // Always-on jump hook: per-state handlers still process immediate jumps (Locomotion/Crouch); this buffers
            // presses that arrive while airborne and fires coyote jumps from Fall state.
            _inputReader.onJumpPerformed += OnJumpInputBufferAndCoyote;

            _isStrafing = _alwaysStrafe;

            if (_controller != null)
                CapsuleCrouchingSize(_isCrouching);

            _useDataDrivenCombo = _comboData != null && _animator != null && HasAnimatorParameter("Attack")
                && (HasAnimatorParameter("ComboStateBlend") || HasAnimatorParameter("ComboState"));

            ApplyComboOverridesIfReady();

            if (_animator != null)
            {
                _animatorHasBowDrawing = HasAnimatorParameter("BowDrawing");
                _animatorHasBowDrawCharge = HasAnimatorParameter("BowDrawCharge");
                _animatorHasBowAiming = HasAnimatorParameter("BowAiming");
                _animatorHasBowChargedShotReady = HasAnimatorParameter("BowChargedShotReady");
                _bowDrawLayerIndex = _animator.GetLayerIndex(BowDrawLayerName);
                if (_bowDrawLayerIndex >= 0)
                {
                    _currentBowDrawLayerWeight = IsBowEquipped ? 1f : 0f;
                    _animator.SetLayerWeight(_bowDrawLayerIndex, _currentBowDrawLayerWeight);
                }
            }

            SwitchState(AnimationState.Locomotion);
        }

        private void EnsurePersistentLockOnIndicator()
        {
            if (_lockOnIndicator == null)
                _lockOnIndicator = GetComponentInChildren<LockOnIndicator>(true);

            if (_lockOnIndicator != null)
                return;

            var indicatorObject = new GameObject("LockOnIndicator");
            indicatorObject.transform.SetParent(transform, false);
            _lockOnIndicator = indicatorObject.AddComponent<LockOnIndicator>();
        }

        /// <summary>
        /// Uses a detached helper for lock-on so root motion on the player never drags the world-space reticle anchor.
        /// </summary>
        private Transform EnsureWorldSpaceLockOnAnchor()
        {
            Transform existingAnchor = _cameraController != null ? _cameraController.LockOnTargetTransform : null;
            if (existingAnchor != null && existingAnchor.parent == null)
            {
                _ownsTargetLockOnPos = false;
                return existingAnchor;
            }

            Vector3 initialPosition = existingAnchor != null ? existingAnchor.position : transform.position;
            var go = new GameObject("TargetLockOnPos_Runtime");
            go.transform.position = initialPosition;
            _ownsTargetLockOnPos = true;
            return go.transform;
        }

        /// <summary>
        /// Called by <see cref="Geis.Combat.GeisBowController"/> while RT is held. Add Bool <c>BowDrawing</c> and optional Float <c>BowDrawCharge</c> (0–1) on the Animator for draw clips / blend trees.
        /// Add Bool <c>BowAiming</c> for LT + bow equipped (Synty ToAiming / aim hold / ToBowDown on the Bow_Draw layer).
        /// Optional Bool <c>BowChargedShotReady</c> can drive a charged-shot shake pose once the draw has fully charged.
        /// </summary>
        public void SetBowDrawState(bool drawing, float chargeNormalized01 = 0f, bool chargedShotReady = false)
        {
            _isBowDrawing = drawing;
            _bowDrawCharge = Mathf.Clamp01(chargeNormalized01);
            _isBowChargedShotReady = drawing && chargedShotReady;
        }

        private void OnDestroy()
        {
            if (_inputReader != null)
            {
                _inputReader.onLockOnToggled -= ToggleLockOn;
                _inputReader.onLockOnCycleLeft -= CycleLockOnLeft;
                _inputReader.onLockOnCycleRight -= CycleLockOnRight;
                _inputReader.onDodgePerformed -= OnDodgeRequested;
                _inputReader.onJumpPerformed -= OnJumpInputBufferAndCoyote;
            }

            if (_comboOverrideController != null)
            {
                Destroy(_comboOverrideController);
                _comboOverrideController = null;
            }

            if (_ownsTargetLockOnPos && _targetLockOnPos != null)
            {
                Destroy(_targetLockOnPos.gameObject);
                _targetLockOnPos = null;
            }
        }

        /// <summary>
        /// Always-on jump input handler. Stashes the press in the jump buffer (consumed by <see cref="UpdateFallState"/>
        /// on landing) and converts presses during Fall into a jump when coyote time is still alive.
        /// </summary>
        private void OnJumpInputBufferAndCoyote()
        {
            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.ShouldSuppressBodyLocomotion)
                return;

            // Combo attacks own the player's grounded commitment window; jump presses during Attack should be ignored
            // instead of being buffered and leaking into the post-combo locomotion/fall transition.
            if (_currentState == AnimationState.Attack)
            {
                _jumpBufferedAt = -1f;
                return;
            }

            _jumpBufferedAt = Time.unscaledTime;

            if (_currentState == AnimationState.Fall && _coyoteTimer > 0f)
            {
                _coyoteTimer = 0f;
                _jumpBufferedAt = -1f;
                SwitchState(AnimationState.Jump);
            }
        }

        /// <summary>True if a jump press was buffered within <see cref="_jumpBufferSeconds"/>.</summary>
        private bool IsJumpBufferFresh()
        {
            return _jumpBufferedAt >= 0f
                && _jumpBufferSeconds > 0f
                && (Time.unscaledTime - _jumpBufferedAt) <= _jumpBufferSeconds;
        }

        #endregion

        #region Aim and Lock-on

        /// <summary>
        ///     Activates the aim action of the player.
        /// </summary>
        private void ActivateAim()
        {
            _isAiming = true;

            if (IsBowEquipped)
                DeactivateSprint();

            _isStrafing = !_isSprinting;
        }

        /// <summary>
        ///     Deactivates the aim action of the player.
        /// </summary>
        private void DeactivateAim()
        {
            _isAiming = false;
            _isStrafing = !_isSprinting && (_alwaysStrafe || _isLockedOn);
        }

        /// <summary>
        ///     Adds an object to the list of target candidates.
        /// </summary>
        /// <param name="newTarget">The object to add.</param>
        public void AddTargetCandidate(GameObject newTarget)
        {
            if (newTarget != null)
            {
                _currentTargetCandidates.Add(newTarget);
            }
        }

        /// <summary>
        ///     Removes an object to the list of target candidates if present.
        /// </summary>
        /// <param name="targetToRemove">The object to remove if present.</param>
        public void RemoveTarget(GameObject targetToRemove)
        {
            if (_currentTargetCandidates.Contains(targetToRemove))
            {
                _currentTargetCandidates.Remove(targetToRemove);
            }
        }

        /// <summary>
        ///     Toggle the lock-on state.
        /// </summary>
        private void ToggleLockOn()
        {
            EnableLockOn(!_isLockedOn);
        }

        private void CycleLockOnLeft()
        {
            CycleLockOnTarget(-1);
        }

        private void CycleLockOnRight()
        {
            CycleLockOnTarget(1);
        }

        /// <summary>
        ///     Sets the lock-on state to the given state.
        /// </summary>
        /// <param name="enable">The state to set lock-on to.</param>
        private void EnableLockOn(bool enable)
        {
            _isLockedOn = enable;
            _isStrafing = false;

            _isStrafing = enable ? !_isSprinting : _alwaysStrafe || _isAiming;

            if (_targetLockOnPos != null)
                _cameraController.LockOn(enable, _targetLockOnPos);

            if (enable && _currentLockOnTarget != null)
            {
                _currentLockOnTarget.GetComponent<Geis.Combat.GeisObjectLockOn>()?.Highlight(true, true);
            }
        }

        private void CycleLockOnTarget(int direction)
        {
            if (!_isLockedOn || _currentTargetCandidates.Count == 0)
                return;

            var orderedTargets = GetOrderedLockOnTargets();
            if (orderedTargets.Count == 0)
                return;

            if (_currentLockOnTarget == null)
            {
                _currentLockOnTarget = direction < 0 ? orderedTargets[orderedTargets.Count - 1] : orderedTargets[0];
            }
            else
            {
                int currentIndex = orderedTargets.IndexOf(_currentLockOnTarget);
                if (currentIndex < 0)
                {
                    _currentLockOnTarget = direction < 0 ? orderedTargets[orderedTargets.Count - 1] : orderedTargets[0];
                }
                else
                {
                    int step = direction < 0 ? -1 : 1;
                    int nextIndex = (currentIndex + step + orderedTargets.Count) % orderedTargets.Count;
                    _currentLockOnTarget = orderedTargets[nextIndex];
                }
            }

            HighlightCurrentLockOnTarget();
            UpdateLockOnAnchorPosition();
        }

        #endregion

        #region Walking State

        /// <summary>
        ///     Toggle the walking state.
        /// </summary>
        private void ToggleWalk()
        {
            bool wantWalk = !_isWalking;
            if (wantWalk && _isSprinting)
                DeactivateSprint();
            EnableWalk(wantWalk);
        }

        /// <summary>
        ///     Sets the walking state to that of the passed in state.
        /// </summary>
        /// <param name="enable">The state to set.</param>
        private void EnableWalk(bool enable)
        {
            _isWalking = enable && _isGrounded && !_isSprinting;
        }

        #endregion

        #region Sprinting State

        /// <summary>
        ///     Activates sprinting behaviour.
        /// </summary>
        private void ActivateSprint()
        {
            if (!_isCrouching)
            {
                EnableWalk(false);
                _isSprinting = true;
                _isStrafing = false;
            }
        }

        /// <summary>
        ///     Deactivates sprinting behaviour.
        /// </summary>
        private void DeactivateSprint()
        {
            _isSprinting = false;

            if (_alwaysStrafe || _isAiming || _isLockedOn)
            {
                _isStrafing = true;
            }
        }

        #endregion

        #region Crouching State

        /// <summary>
        ///     Activates crouching behaviour
        /// </summary>
        private void ActivateCrouch()
        {
            _crouchKeyPressed = true;

            if (_isGrounded)
            {
                CapsuleCrouchingSize(true);
                DeactivateSprint();
                _isCrouching = true;
            }
        }

        /// <summary>
        ///     Deactivates crouching behaviour.
        /// </summary>
        private void DeactivateCrouch()
        {
            _crouchKeyPressed = false;

            if (!_cannotStandUp && !_isSliding)
            {
                CapsuleCrouchingSize(false);
                _isCrouching = false;
            }
        }

        /// <summary>
        ///     Activates sliding behaviour.
        /// </summary>
        public void ActivateSliding()
        {
            _isSliding = true;
        }

        /// <summary>
        ///     Deactivates sliding behaviour
        /// </summary>
        public void DeactivateSliding()
        {
            _isSliding = false;
        }

        /// <summary>
        ///     Adjusts the capsule size for the player, depending on the passed in boolean value.
        /// </summary>
        /// <param name="crouching">Whether the player is crouching or not.</param>
        private void CapsuleCrouchingSize(bool crouching)
        {
            if (crouching)
            {
                _controller.center = new Vector3(0f, _capsuleCrouchingCentre, 0f);
                _controller.height = _capsuleCrouchingHeight;
            }
            else
            {
                _controller.center = new Vector3(0f, _capsuleStandingCentre, 0f);
                _controller.height = _capsuleStandingHeight;
            }
        }

        #endregion

        #region Attack (Data-Driven Combo)

        private GeisComboData GetCurrentComboData()
        {
            if (_weaponSwitcher != null && _weaponSwitcher.TryGetComboForWeapon(_weaponSwitcher.CurrentWeaponIndex, out var unifiedCombo))
                return unifiedCombo;
            if (_weaponComboData != null && _weaponSwitcher != null)
            {
                int idx = _weaponSwitcher.CurrentWeaponIndex;
                var data = _weaponComboData.GetComboForWeapon(idx);
                if (data != null) return data;
            }
            return _comboData;
        }

        /// <summary>
        /// Applies combo clips from GeisComboData to the animator via AnimatorOverrideController.
        /// Uses placeholders in the blend tree; no Sync step needed. Call on Start and when combo data changes.
        /// </summary>
        private void ApplyComboOverridesIfReady()
        {
            if (!_useDataDrivenCombo || _animator == null) return;

            var comboData = GetCurrentComboData();
            if (comboData == null) return;
            if (comboData == _lastAppliedComboData) return;

            var placeholders = _comboPlaceholders != null
                ? _comboPlaceholders
                : Resources.Load<GeisComboPlaceholders>("GeisComboPlaceholders");
            if (placeholders == null) return;

            var current = _animator.runtimeAnimatorController;
            RuntimeAnimatorController baseController = null;
            if (current is AnimatorOverrideController aoc)
                baseController = aoc.runtimeAnimatorController;
            else if (current != null)
                baseController = current;

            if (baseController == null) return;

            if (_comboOverrideController == null || _comboOverrideController.runtimeAnimatorController != baseController)
                _comboOverrideController = new AnimatorOverrideController(baseController);

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            for (int i = 0; i < 32; i++)
            {
                var placeholder = placeholders.GetPlaceholder(i);
                var clip = comboData.GetClipForState(i);
                if (placeholder != null && clip != null)
                    overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(placeholder, clip));
            }

            if (overrides.Count > 0)
                _comboOverrideController.ApplyOverrides(overrides);

            _animator.runtimeAnimatorController = _comboOverrideController;
            _lastAppliedComboData = comboData;
        }

        private void OnLightAttackRequested()
        {
            if (IsBowEquipped)
                return;

            if (TryProcessSoulRealmMeleeInput(GeisComboInputType.Light))
                return;

            if (!_isGrounded || _isCrouching) return;

            var comboData = GetCurrentComboData();

            if (_currentState == AnimationState.Locomotion)
            {
                if (_useDataDrivenCombo && comboData != null)
                {
                    _firstAttackInputType = GeisComboInputType.Light;
                    _currentComboState = 0;
                    SwitchState(AnimationState.Attack);
                }
                else if (_animator != null && HasAnimatorParameter("Attack_1"))
                {
                    _firstAttackInputType = GeisComboInputType.Light;
                    SwitchState(AnimationState.Attack);
                }
            }
            else if (_currentState == AnimationState.Attack && _useDataDrivenCombo && comboData != null)
            {
                SetComboInputBuffer(GeisComboInputType.Light);
            }
            else if (_currentState == AnimationState.Dodge && _useDataDrivenCombo && comboData != null)
            {
                // Dodge → Attack cancel: buffer; UpdateDodgeState consumes inside the dodge recovery window.
                SetComboInputBuffer(GeisComboInputType.Light);
            }
        }

        private void OnHeavyAttackRequested()
        {
            // Bow draw/shot uses RT via GeisBowController; never start heavy melee while bow is equipped.
            if (IsBowEquipped)
                return;

            if (TryProcessSoulRealmMeleeInput(GeisComboInputType.Heavy))
                return;

            if (!_isGrounded || _isCrouching) return;

            if (_currentState == AnimationState.Locomotion && _useDataDrivenCombo && GetCurrentComboData() != null)
            {
                _firstAttackInputType = GeisComboInputType.Heavy;
                _currentComboState = 0;
                SwitchState(AnimationState.Attack);
            }
            else if (_currentState == AnimationState.Attack && _useDataDrivenCombo)
            {
                SetComboInputBuffer(GeisComboInputType.Heavy);
            }
            else if (_currentState == AnimationState.Dodge && _useDataDrivenCombo && GetCurrentComboData() != null)
            {
                SetComboInputBuffer(GeisComboInputType.Heavy);
            }
        }

        private void OnDodgeRequested()
        {
            if (GeisInteractInput.IsMovementFrozenForInteraction)
                return;
            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive)
                return;
            if (_animator == null || !HasAnimatorParameter("Dodge") || !HasAnimatorParameter("DodgeDirection"))
            {
                if (!_loggedDodgeAnimatorMissing)
                {
                    _loggedDodgeAnimatorMissing = true;
                    Debug.LogWarning(
                        "[GeisPlayerAnimationController] Animator is missing Dodge (Trigger) and/or DodgeDirection (Int), or dodge states. " +
                        "Run menu: Geis → Animator → Setup Directional Dodge & Roll Clips.");
                }

                return;
            }
            if (_dodgeDoubleTapRollEnabled && _dodgeRequestIsRoll && !HasAnimatorParameter("Roll"))
            {
                if (!_loggedForwardRollMissing)
                {
                    _loggedForwardRollMissing = true;
                    Debug.LogWarning(
                        "[GeisPlayerAnimationController] Animator is missing Roll (Trigger). " +
                        "Run menu: Geis → Animator → Setup Directional Dodge & Roll Clips.",
                        this);
                }
            }
            if (_requireMovementInputForDodge &&
                GeisInteractInput.GetEffectiveMoveCompositeForLocomotion(_inputReader._moveComposite).sqrMagnitude <
                _dodgeInputDeadzone * _dodgeInputDeadzone)
                return;

            // Double-tap: second press must land within the button window AND while a dash follow-up is armed.
            float nowUnscaled = Time.unscaledTime;
            float dtSinceLast = _lastDodgeTapAt > 0f ? (nowUnscaled - _lastDodgeTapAt) : -1f;
            bool rollIntent = _dodgeDoubleTapRollEnabled
                && _lastDodgeTapAt > 0f
                && dtSinceLast <= _dodgeDoubleTapWindow
                && nowUnscaled <= _dodgeRollFollowUpExpiresAtUnscaled;
            _lastDodgeTapAt = nowUnscaled;

            if (_debugDodgeDoubleTap)
            {
                float followUpRemaining = _dodgeRollFollowUpExpiresAtUnscaled > 0f
                    ? _dodgeRollFollowUpExpiresAtUnscaled - nowUnscaled
                    : -1f;
                Debug.Log(
                    $"[GeisPlayerAnimationController] Dodge press — state={_currentState} dtSinceLast={dtSinceLast:F3}s window={_dodgeDoubleTapWindow:F3}s rollEnabled={_dodgeDoubleTapRollEnabled} rollIntent={rollIntent} followUpRemaining={followUpRemaining:F3}s isRoll={_dodgeIsRoll}",
                    this);
            }

            // Attack → Dodge cancel: buffer; UpdateAttackState consumes inside the combo cancel window.
            if (_currentState == AnimationState.Attack)
            {
                _dodgeInputBufferedAt = nowUnscaled;
                _dodgeInputBufferIsRoll = rollIntent;
                return;
            }

            if (_currentState == AnimationState.Dodge)
            {
                if (rollIntent && !_dodgeIsRoll)
                    UpgradeActiveDodgeToRoll();

                return;
            }

            if (_currentState != AnimationState.Locomotion || !_isGrounded || _isCrouching)
                return;

            _dodgeRequestIsRoll = rollIntent;
            if (!rollIntent && _dodgeDoubleTapRollEnabled)
                _dodgeRollFollowUpExpiresAtUnscaled = nowUnscaled + _dodgeDoubleTapWindow;
            else if (rollIntent)
                _dodgeRollFollowUpExpiresAtUnscaled = -1f;

            SwitchState(AnimationState.Dodge);
        }

        /// <summary>Swaps the active dash clip to a roll without exiting gameplay Dodge state.</summary>
        private void UpgradeActiveDodgeToRoll()
        {
            if (_animator == null)
                return;

            bool preserveCameraFacing = ShouldPreserveCameraFacingOnDodge(_speed2D) && !_isLockedOn;
            if (preserveCameraFacing)
                SnapBodyYawToCameraForward();

            int stickDirection = ComputeDodgeDirectionIndex();
            int dir = stickDirection;

            _dodgeIsRoll = true;
            _dodgeAnimatorDir = dir;
            _dodgeStateEnteredAtUnscaled = Time.unscaledTime;
            _dodgeRollFollowUpExpiresAtUnscaled = -1f;
            _dodgePreserveStrafeFacing = _isLockedOn || preserveCameraFacing;
            _dodgeStateTimeout = _dodgeFallbackDuration;

            if (HasAnimatorParameter("DodgeDirection"))
                _animator.SetInteger(_dodgeDirectionHash, dir);

            _dodgeAnimatorEnteredLeaf = false;
            PlayDodgeLeafCrossFade(dir, forceRestart: true);
            SyncRollAnimatorPlayback();

            if (_debugDodgeDoubleTap)
            {
                Debug.Log(
                    $"[GeisPlayerAnimationController] Upgraded dodge to roll — dir={dir} clip={GetRollLeafHashForDirection(dir)} distanceMult={_rollDistanceMultiplier}",
                    this);
            }
        }

        /// <summary>CrossFades directly into the dodge leaf (bypasses brittle Any-State + DodgeDirection routing).</summary>
        private void PlayDodgeLeafCrossFade(int dir, bool forceRestart = false)
        {
            if (_animator == null)
                return;

            _animator.ResetTrigger(_dodgeTriggerHash);
            _animator.ResetTrigger(_rollTriggerHash);

            if (HasAnimatorParameter("DodgeDirection"))
                _animator.SetInteger(_dodgeDirectionHash, dir);

            int primaryHash = _dodgeIsRoll
                ? GetRollNestedHashForDirection(dir)
                : GetDodgeNestedStateHashForDirection(dir);
            int fallbackHash = _dodgeIsRoll
                ? GetRollLeafHashForDirection(dir)
                : GetDodgeLeafHashForDirection(dir);

            if (forceRestart)
            {
                if (_animator.HasState(0, primaryHash))
                    _animator.Play(primaryHash, 0, 0f);
                else if (_animator.HasState(0, fallbackHash))
                    _animator.Play(fallbackHash, 0, 0f);
                else if (_dodgeIsRoll && HasAnimatorParameter("Roll"))
                    _animator.SetTrigger(_rollTriggerHash);
                else if (HasAnimatorParameter("Dodge"))
                    _animator.SetTrigger(_dodgeTriggerHash);
                return;
            }

            if (_animator.HasState(0, primaryHash))
                _animator.CrossFadeInFixedTime(primaryHash, 0.05f, 0, 0f);
            else if (_animator.HasState(0, fallbackHash))
                _animator.CrossFadeInFixedTime(fallbackHash, 0.05f, 0, 0f);
            else if (_dodgeIsRoll && HasAnimatorParameter("Roll"))
                _animator.SetTrigger(_rollTriggerHash);
            else if (HasAnimatorParameter("Dodge"))
                _animator.SetTrigger(_dodgeTriggerHash);
        }

        private void SyncRollAnimatorPlayback()
        {
            if (_animator == null)
                return;

            // Dedicated roll clips use baked timing; only speed up sidestep fallbacks.
            _animator.speed = 1f;
        }

        private static int GetDodgeLeafHashForDirection(int dir)
        {
            switch (dir)
            {
                case 0: return _dodgeLeafFrontHash;
                case 1: return _dodgeLeafBackHash;
                case 2: return _dodgeLeafLeftHash;
                case 3: return _dodgeLeafRightHash;
                default: return _dodgeLeafFrontHash;
            }
        }

        private static int GetDodgeNestedStateHashForDirection(int dir)
        {
            switch (dir)
            {
                case 0: return _dodgeNestedFrontHash;
                case 1: return _dodgeNestedBackHash;
                case 2: return _dodgeNestedLeftHash;
                case 3: return _dodgeNestedRightHash;
                default: return _dodgeNestedFrontHash;
            }
        }

        private static int GetRollLeafHashForDirection(int dir)
        {
            switch (dir)
            {
                case 0: return _rollLeafForwardHash;
                case 1: return _rollLeafBackHash;
                case 2: return _rollLeafLeftHash;
                case 3: return _rollLeafRightHash;
                default: return _rollLeafForwardHash;
            }
        }

        private static int GetRollNestedHashForDirection(int dir)
        {
            switch (dir)
            {
                case 0: return _rollNestedForwardHash;
                case 1: return _rollNestedBackHash;
                case 2: return _rollNestedLeftHash;
                case 3: return _rollNestedRightHash;
                default: return _rollNestedForwardHash;
            }
        }

        private bool HasRollLeafStateForDirection(int dir)
        {
            if (_animator == null)
                return false;

            int nested = GetRollNestedHashForDirection(dir);
            int leaf = GetRollLeafHashForDirection(dir);
            return _animator.HasState(0, nested) || _animator.HasState(0, leaf);
        }

        private Vector2 GetDodgeMoveComposite()
        {
            if (_inputReader == null)
                return Vector2.zero;
            return GeisInteractInput.GetEffectiveMoveCompositeForLocomotion(_inputReader._moveComposite);
        }

        private int ComputeDodgeDirectionIndex()
        {
            Vector2 m = GetDodgeMoveComposite();
            float deadzone = _dodgeInputDeadzone;

            if (m.sqrMagnitude < deadzone * deadzone)
            {
                if (_isLockedOn && _currentLockOnTarget != null)
                    return DirectionWorldToDodgeIndex(GetPlanarDirectionAwayFromLockOnTarget());
                return 1;
            }

            return DirectionWorldToDodgeIndex(GetCameraRelativeDodgeWorldDirection(m));
        }

        /// <summary>At or below <see cref="_strafeStyleMaxPlanarSpeed"/> (and not sprinting): dodge keeps camera-forward yaw.</summary>
        private bool ShouldPreserveCameraFacingOnDodge(float planarSpeedAtPress = -1f)
        {
            if (_isAiming || _isBowDrawing)
                return true;

            return IsWithinStrafeStyleSpeedCap(planarSpeedAtPress);
        }

        private void SnapBodyYawToCameraForward()
        {
            if (_cameraController == null)
                return;

            Vector3 forward = _cameraController.GetCameraForwardZeroedYNormalised();
            if (forward.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(forward);
        }

        private Vector3 GetCameraRelativeDodgeWorldDirection(Vector2 m)
        {
            Vector3 camFwd = _cameraController.GetCameraForwardZeroedYNormalised();
            Vector3 camRight = _cameraController.GetCameraRightZeroedYNormalised();
            return (camFwd * m.y + camRight * m.x).normalized;
        }

        private Vector3 GetPlanarDirectionAwayFromLockOnTarget()
        {
            Vector3 toTarget = ResolveLockOnWorldPosition(_currentLockOnTarget) - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                Vector3 back = -GetPlanarForward();
                return back.sqrMagnitude > 0.0001f ? back : Vector3.back;
            }

            return (-toTarget).normalized;
        }

        private int DirectionWorldToDodgeIndex(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
                return 1;

            worldDirection.Normalize();
            Vector3 local = transform.InverseTransformDirection(worldDirection);
            float lx = local.x;
            float lz = local.z;
            if (Mathf.Abs(lz) >= Mathf.Abs(lx))
                return lz >= 0f ? 0 : 1;
            return lx >= 0f ? 3 : 2;
        }

        private static int DodgeDirectionIndexToFacingIndex(int dirIndex)
        {
            return dirIndex;
        }

        private Vector3 GetDodgeFacingWorld(int dirIndex)
        {
            Vector3 camFwd = _cameraController.GetCameraForwardZeroedYNormalised();
            Vector3 camRight = _cameraController.GetCameraRightZeroedYNormalised();
            switch (dirIndex)
            {
                case 0: return camFwd;
                case 1: return -camFwd;
                case 2: return -camRight;
                case 3: return camRight;
                default: return camFwd;
            }
        }

        private void EnterDodgeState()
        {
            float planarSpeedAtDodgePress = _speed2D;
            _dodgeStateEnteredAtUnscaled = Time.unscaledTime;
            _velocity.x = 0f;
            _velocity.z = 0f;

            _dodgeIsRoll = _dodgeRequestIsRoll;
            _dodgeRequestIsRoll = false;

            bool preserveCameraFacing = ShouldPreserveCameraFacingOnDodge(planarSpeedAtDodgePress) && !_isLockedOn;
            if (preserveCameraFacing)
                SnapBodyYawToCameraForward();

            int stickDirection = ComputeDodgeDirectionIndex();
            int dir = stickDirection;
            _dodgeAnimatorDir = dir;

            if (_dodgeIsRoll && !HasRollLeafStateForDirection(dir))
            {
                if (!_loggedForwardRollMissing)
                {
                    _loggedForwardRollMissing = true;
                    Debug.LogWarning(
                        "[GeisPlayerAnimationController] Animator is missing one or more directional roll states. " +
                        "Run menu: Geis → Animator → Setup Directional Dodge & Roll Clips.",
                        this);
                }
            }

            int facingIndex = DodgeDirectionIndexToFacingIndex(dir);
            Vector3 faceWorld = GetDodgeFacingWorld(facingIndex);

            // Walk/run (not sprint): snap to camera forward above, then hold yaw through the dodge clip.
            _dodgePreserveStrafeFacing = _isLockedOn || preserveCameraFacing || (_isStrafing && !_dodgeIsRoll);

            if (!_dodgePreserveStrafeFacing && faceWorld.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(faceWorld);

            PlayDodgeLeafCrossFade(dir);
            SyncRollAnimatorPlayback();

            if (_dodgeIsRoll)
                _dodgeRollFollowUpExpiresAtUnscaled = -1f;
            else if (_dodgeDoubleTapRollEnabled)
                _dodgeRollFollowUpExpiresAtUnscaled = Time.unscaledTime + _dodgeDoubleTapWindow;

            _dodgeAnimatorEnteredLeaf = false;
            _dodgeStateTimeout = _dodgeFallbackDuration;
        }

        private Vector3 GetPlanarForward()
        {
            Vector3 f = transform.forward;
            f.y = 0f;
            float sq = f.sqrMagnitude;
            return sq > 0.0001f ? f / Mathf.Sqrt(sq) : Vector3.forward;
        }

        private static bool IsDodgeLeafShortNameHash(int shortNameHash)
        {
            return shortNameHash == _dodgeLeafFrontHash || shortNameHash == _dodgeLeafBackHash
                || shortNameHash == _dodgeLeafLeftHash || shortNameHash == _dodgeLeafRightHash
                || shortNameHash == _rollLeafForwardHash || shortNameHash == _rollLeafBackHash
                || shortNameHash == _rollLeafLeftHash || shortNameHash == _rollLeafRightHash;
        }

        private void UpdateDodgeState()
        {
            ApplyGravity();
            _dodgeStateTimeout -= Time.deltaTime;

            // Prune stale buffers before testing recovery cancels.
            if (_comboInputBuffered.HasValue && !IsBufferFresh(_comboInputBufferedAt))
                ClearComboInputBuffer();
            if (_dodgeInputBufferedAt >= 0f && !IsBufferFresh(_dodgeInputBufferedAt))
            {
                _dodgeInputBufferedAt = -1f;
                _dodgeInputBufferIsRoll = false;
            }

            GroundedCheck();
            if (!_isGrounded)
            {
                SwitchState(AnimationState.Fall);
                return;
            }

            if (_animator != null)
            {
                AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
                if (_animator.IsInTransition(0))
                {
                    AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(0);
                    if (IsDodgeLeafShortNameHash(next.shortNameHash))
                        _dodgeAnimatorEnteredLeaf = true;
                }
                else if (IsDodgeLeafShortNameHash(info.shortNameHash))
                {
                    _dodgeAnimatorEnteredLeaf = true;
                }

                // Dodge recovery window: once the leaf clip passes the recovery threshold, allow attack/move cancels.
                float recoveryThreshold = _dodgeIsRoll
                    ? _rollRecoveryStartNormalizedTime
                    : _dodgeRecoveryStartNormalizedTime;
                bool inRecoveryWindow = _dodgeAnimatorEnteredLeaf
                    && IsDodgeLeafShortNameHash(info.shortNameHash)
                    && !_animator.IsInTransition(0)
                    && info.normalizedTime >= recoveryThreshold;

                if (inRecoveryWindow)
                {
                    // Attack-cancel has priority over move-cancel so a queued attack fires instantly after the dodge active frames.
                    var comboData = GetCurrentComboData();
                    if (_useDataDrivenCombo && comboData != null && !_isCrouching
                        && TryConsumeComboInputBuffer(out var queuedAttack))
                    {
                        _firstAttackInputType = queuedAttack;
                        _currentComboState = 0;
                        SwitchState(AnimationState.Attack);
                        return;
                    }

                    // Let the forward roll finish its authored root-motion arc. If we move-cancel it as soon as the
                    // recovery window opens while the stick is still held, the character visibly hitches back into
                    // locomotion mid-roll.
                    if (!_dodgeIsRoll)
                    {
                        Vector2 composite = _inputReader != null
                            ? GeisInteractInput.GetEffectiveMoveCompositeForLocomotion(_inputReader._moveComposite)
                            : Vector2.zero;
                        float threshold = _dodgeMoveCancelStickThreshold;
                        if (composite.sqrMagnitude >= threshold * threshold)
                        {
                            SeedPlanarVelocityAfterDodgeExit();
                            SwitchState(AnimationState.Locomotion);
                            return;
                        }
                    }
                }

                // Do not rely on normalizedTime alone: after the dodge clip the Animator transitions to Idle_Standing
                // (exit ~0.92). Layer 0 then reports Idle's normalizedTime, so the old >= 0.99 check never passes and
                // gameplay stayed in Dodge until _dodgeFallbackDuration (~1.2s) with zero scripted locomotion.
                if (_dodgeAnimatorEnteredLeaf && !_animator.IsInTransition(0)
                    && !IsDodgeLeafShortNameHash(info.shortNameHash))
                {
                    SeedPlanarVelocityAfterDodgeExit();
                    SwitchState(AnimationState.Locomotion);
                    return;
                }

                if (info.length > 0.01f && info.normalizedTime >= 0.99f && !_animator.IsInTransition(0)
                    && IsDodgeLeafShortNameHash(info.shortNameHash))
                {
                    SeedPlanarVelocityAfterDodgeExit();
                    SwitchState(AnimationState.Locomotion);
                    return;
                }
            }

            UpdateDodgeInvulnerabilityFromAnimator();

            if (_dodgeStateTimeout <= 0f)
            {
                SeedPlanarVelocityAfterDodgeExit();
                SwitchState(AnimationState.Locomotion);
            }
            else
            {
                UpdateAnimatorController();
            }
        }

        private void ExitDodgeState()
        {
            if (_dodgeIsRoll)
            {
                _velocity.x = 0f;
                _velocity.z = 0f;
            }

            _dodgeIsRoll = false;
            _dodgeStateEnteredAtUnscaled = -1f;
            SyncRollAnimatorPlayback();
            if (_defensiveCombatState != null)
                _defensiveCombatState.SetDodgeInvulnerable(false);
        }

        private void EnterAttackState()
        {
            _attackAnimatorEnteredLeaf = false;
            _velocity.x = 0f;
            _velocity.z = 0f;
            ClearMovementInputStateForAttack();
            _jumpBufferedAt = -1f;

            if (_useDataDrivenCombo && _animator != null && HasAnimatorParameter("Attack")
                && (HasAnimatorParameter("ComboStateBlend") || HasAnimatorParameter("ComboState")))
            {
                SetComboStateBlend(_currentComboState);
                _animator.SetTrigger(_attackTriggerHash);
                var comboData = GetCurrentComboData();
                _attackStateTimeout = comboData != null ? 2f : 1.5f;
                int weaponIdx = GetCurrentWeaponIndex();
                OnAttackPerformed?.Invoke(weaponIdx);
            }
            else if (_animator != null && HasAnimatorParameter("Attack_1"))
            {
                _animator.SetTrigger(_attack1Hash);
                _attackStateTimeout = 1.5f;
                int weaponIdx = GetCurrentWeaponIndex();
                OnAttackPerformed?.Invoke(weaponIdx);
            }
        }

        private int GetCurrentWeaponIndex()
        {
            return _weaponSwitcher != null ? _weaponSwitcher.CurrentWeaponIndex : 0;
        }

        private void UpdateAttackState()
        {
            ApplyGravity();
            ClearMovementInputStateForAttack();
            _attackStateTimeout -= Time.deltaTime;

            // Expire stale buffers so presses from far earlier do not trigger a cancel when the window finally opens.
            if (_comboInputBuffered.HasValue && !IsBufferFresh(_comboInputBufferedAt))
                ClearComboInputBuffer();
            if (_dodgeInputBufferedAt >= 0f && !IsBufferFresh(_dodgeInputBufferedAt))
            {
                _dodgeInputBufferedAt = -1f;
                _dodgeInputBufferIsRoll = false;
            }

            var comboData = GetCurrentComboData();
            bool inAttackAnimatorLeaf = false;

            if (_animator != null)
            {
                AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);

                if (_animator.IsInTransition(0))
                {
                    AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(0);
                    if (IsAttackLeafShortNameHash(next.shortNameHash))
                        _attackAnimatorEnteredLeaf = true;
                }
                else if (IsAttackLeafShortNameHash(info.shortNameHash))
                {
                    _attackAnimatorEnteredLeaf = true;
                }

                inAttackAnimatorLeaf = _attackAnimatorEnteredLeaf
                    && !_animator.IsInTransition(0)
                    && IsAttackLeafShortNameHash(info.shortNameHash);

                // Do not trust locomotion/idle normalizedTime before the Attack leaf actually plays, or after it ends.
                if (_attackAnimatorEnteredLeaf && !_animator.IsInTransition(0)
                    && !IsAttackLeafShortNameHash(info.shortNameHash))
                {
                    SwitchState(AnimationState.Locomotion);
                    return;
                }
            }

            if (_useDataDrivenCombo && comboData != null && _animator != null && inAttackAnimatorLeaf)
            {
                AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
                float normalizedTime = info.normalizedTime % 1f;
                comboData.GetCancelWindow(_currentComboState, out float cancelWindowStart, out float cancelWindowEnd);
                bool inCancelWindow = normalizedTime >= cancelWindowStart && normalizedTime <= cancelWindowEnd;

                if (inCancelWindow)
                {
                    // Dodge cancel has priority over combo continuation (most recent intent; standard action-game feel).
                    if (TryConsumeDodgeInputBuffer(out bool bufferedDodgeIsRoll)
                        && _isGrounded && !_isCrouching
                        && HasAnimatorParameter("Dodge") && HasAnimatorParameter("DodgeDirection"))
                    {
                        ClearComboInputBuffer();
                        _dodgeRequestIsRoll = bufferedDodgeIsRoll;
                        SwitchState(AnimationState.Dodge);
                        return;
                    }

                    // Combo continuation.
                    if (TryConsumeComboInputBuffer(out var input))
                    {
                        if (comboData.TryGetNextState(_currentComboState, input, out int nextState))
                        {
                            _currentComboState = nextState;
                            SetComboStateBlend(_currentComboState);
                            _animator.SetTrigger(_attackTriggerHash);
                            var clip = comboData.GetClipForState(_currentComboState);
                            _attackStateTimeout = clip != null ? clip.length + 0.2f : 1.5f;
                            int weaponIdx = GetCurrentWeaponIndex();
                            OnAttackPerformed?.Invoke(weaponIdx);
                            return;
                        }
                    }

                    Vector2 moveComposite = _inputReader != null
                        ? GeisInteractInput.GetEffectiveMoveCompositeForLocomotion(_inputReader._moveComposite)
                        : Vector2.zero;
                    float moveCancelThreshold = _attackMoveCancelStickThreshold;
                    if (moveComposite.sqrMagnitude >= moveCancelThreshold * moveCancelThreshold)
                    {
                        SeedPlanarVelocityFromStick(_attackExitVelocityCarry);
                        SwitchState(AnimationState.Locomotion);
                        return;
                    }
                }

                // Clip-authoritative exit: once the attack clip passes its recovery threshold and is not transitioning
                // to another combo step, return to locomotion immediately instead of idling until _attackStateTimeout.
                if (normalizedTime >= _attackRecoveryExitNormalizedTime && !_animator.IsInTransition(0))
                {
                    SwitchState(AnimationState.Locomotion);
                    return;
                }
            }

            // Safety fallback.
            if (_attackStateTimeout <= 0f)
            {
                SwitchState(AnimationState.Locomotion);
                return;
            }

            UpdateAnimatorController();
        }

        private static bool IsAttackLeafShortNameHash(int shortNameHash)
        {
            return shortNameHash == _attackLeafHash;
        }

        private void ExitAttackState()
        {
            _attackAnimatorEnteredLeaf = false;
            _currentComboState = 0;
            // Intentionally NOT clearing _comboInputBuffered / _dodgeInputBufferedAt here: when we leave Attack via a
            // cancel (e.g. Attack → Dodge), the follow-up state may still want to consume the buffered input later
            // (e.g. Dodge → Attack during dodge recovery). Stale buffers are pruned via IsBufferFresh checks.
        }

        /// <summary>
        /// Sets the blend tree parameter so the correct clip is selected. Unity's Simple1D uses
        /// thresholds 0..1 over 32 slots, so we pass state/31. Use ComboStateBlend (Float) if present,
        /// else fall back to ComboState (Int) - which only works if blend tree has thresholds 0,1,2,...
        /// </summary>
        private void SetComboStateBlend(int state)
        {
            if (HasAnimatorParameter("ComboStateBlend"))
                _animator.SetFloat(_comboStateBlendHash, (float)state / (COMBO_BLEND_SLOTS - 1));
            else
                _animator.SetInteger(_comboStateHash, state);
        }

        private bool HasAnimatorParameter(string name)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return false;
            foreach (var p in _animator.parameters)
                if (p.name == name) return true;
            return false;
        }

        /// <summary>
        /// Frame-rate-stable exponential smoothing: approaches <paramref name="target"/> from <paramref name="current"/>
        /// at <paramref name="rate"/> per second. Equivalent to <c>Mathf.Lerp(current, target, rate*dt)</c> for small dt,
        /// but does not oscillate or drift at high/low framerates.
        /// </summary>
        private static float ExpSmooth(float current, float target, float rate, float dt)
        {
            if (rate <= 0f) return target;
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-rate * dt));
        }

        private void ClearMovementInputStateForAttack()
        {
            _movementInputTapped = false;
            _movementInputPressed = false;
            _movementInputHeld = false;
            _speed2D = 0f;
            _moveDirection = Vector3.zero;
            _targetVelocity.x = 0f;
            _targetVelocity.z = 0f;

            if (_inputReader != null)
                _inputReader._movementInputDuration = 0f;
        }

        /// <summary>True when <paramref name="bufferedAtUnscaled"/> is non-negative and within <see cref="_inputBufferSeconds"/> of now.</summary>
        private bool IsBufferFresh(float bufferedAtUnscaled)
        {
            return bufferedAtUnscaled >= 0f
                && (Time.unscaledTime - bufferedAtUnscaled) <= _inputBufferSeconds;
        }

        /// <summary>Stamps the combo input buffer so <see cref="UpdateAttackState"/> can consume it during the cancel window.</summary>
        private void SetComboInputBuffer(GeisComboInputType input)
        {
            _comboInputBuffered = input;
            _comboInputBufferedAt = Time.unscaledTime;
        }

        /// <summary>Clears the combo input buffer (both value and timestamp).</summary>
        private void ClearComboInputBuffer()
        {
            _comboInputBuffered = null;
            _comboInputBufferedAt = -1f;
        }

        /// <summary>Returns the buffered combo input if its buffer timestamp is fresh, otherwise clears and returns false.</summary>
        private bool TryConsumeComboInputBuffer(out GeisComboInputType input)
        {
            input = default;
            if (!_comboInputBuffered.HasValue) return false;
            if (!IsBufferFresh(_comboInputBufferedAt))
            {
                ClearComboInputBuffer();
                return false;
            }
            input = _comboInputBuffered.Value;
            ClearComboInputBuffer();
            return true;
        }

        /// <summary>Returns true (and clears) if a fresh dodge buffer is available; otherwise clears stale buffers and returns false.</summary>
        private bool TryConsumeDodgeInputBuffer()
        {
            return TryConsumeDodgeInputBuffer(out _);
        }

        /// <summary>
        /// Returns true (and clears) if a fresh dodge buffer is available. <paramref name="isRoll"/> is set to whether
        /// the buffered press was part of a double-tap (forward-roll variant).
        /// </summary>
        private bool TryConsumeDodgeInputBuffer(out bool isRoll)
        {
            isRoll = false;
            if (_dodgeInputBufferedAt < 0f) return false;
            if (!IsBufferFresh(_dodgeInputBufferedAt))
            {
                _dodgeInputBufferedAt = -1f;
                _dodgeInputBufferIsRoll = false;
                return false;
            }
            isRoll = _dodgeInputBufferIsRoll;
            _dodgeInputBufferedAt = -1f;
            _dodgeInputBufferIsRoll = false;
            return true;
        }

        /// <summary>Pre-seeds planar velocity from the current move stick so exiting an attack/dodge into movement does not stall.</summary>
        private void SeedPlanarVelocityFromStick(float carryFraction)
        {
            if (_inputReader == null || _cameraController == null)
                return;

            Vector2 composite = GeisInteractInput.GetEffectiveMoveCompositeForLocomotion(_inputReader._moveComposite);
            if (composite.sqrMagnitude < 0.0001f)
                return;

            Vector3 camRelative =
                _cameraController.GetCameraForwardZeroedYNormalised() * composite.y
                + _cameraController.GetCameraRightZeroedYNormalised() * composite.x;

            float scale = Mathf.Max(0f, carryFraction) * _currentMaxSpeed;
            _velocity.x = camRelative.x * scale;
            _velocity.z = camRelative.z * scale;
        }

        private void SeedPlanarVelocityAfterDodgeExit()
        {
            float carry = _dodgeIsRoll ? _rollExitVelocityCarry : _dodgeExitVelocityCarry;
            if (carry <= 0.0001f)
            {
                _velocity.x = 0f;
                _velocity.z = 0f;
                return;
            }

            SeedPlanarVelocityFromStick(carry);
        }

        /// <summary>
        ///     Applies root motion during Attack and Dodge to the CharacterController transform.
        ///     Locomotion, Jump, Fall, Crouch use script-driven movement via Move() - no root motion here.
        /// </summary>
        private void OnAnimatorMove()
        {
            if (_animatorIsOnChild)
                return;

            ApplyAttackDodgeRootMotionToBody();
        }

        /// <summary>
        /// When the Animator is on a child rig, Unity only invokes <see cref="OnAnimatorMove"/> on that child.
        /// Apply attack/dodge root motion to the body here and keep the visual rig snapped to the capsule.
        /// </summary>
        private void LateUpdate()
        {
            if (_animator == null)
                return;

            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.ShouldSuppressBodyLocomotion)
                return;

            SyncAnimatorApplyRootMotionForState();

            if (_animatorIsOnChild)
            {
                ApplyAttackDodgeRootMotionToBody();
                RealignVisualRigUnderBody();
            }
        }

        private void SyncAnimatorApplyRootMotionForState()
        {
            bool wantsRootMotion = _currentState == AnimationState.Attack
                || _currentState == AnimationState.Dodge;

            if (_animator.applyRootMotion != wantsRootMotion)
                _animator.applyRootMotion = wantsRootMotion;
        }

        private void ApplyAttackDodgeRootMotionToBody()
        {
            if (_animator == null || !_animator.applyRootMotion || _controller == null || !_controller.enabled)
                return;

            if (_currentState == AnimationState.Attack)
            {
                var deltaPosition = _animator.deltaPosition;
                deltaPosition.y += _velocity.y * Time.deltaTime;

                _controller.Move(deltaPosition);

                if (_applyRootRotationDuringAttack && _animator.deltaRotation != Quaternion.identity)
                    transform.rotation = transform.rotation * _animator.deltaRotation;
            }
            else if (_currentState == AnimationState.Dodge)
            {
                var deltaPosition = _animator.deltaPosition;

                if (_dodgeIsRoll)
                {
                    AnimatorStateInfo rollInfo = _animator.GetCurrentAnimatorStateInfo(0);
                    if (IsDodgeLeafShortNameHash(rollInfo.shortNameHash) && rollInfo.normalizedTime > 0.85f)
                    {
                        float fade = 1f - Mathf.InverseLerp(0.85f, 0.98f, rollInfo.normalizedTime);
                        deltaPosition.x *= fade;
                        deltaPosition.z *= fade;
                    }
                    else if (!HasRollLeafStateForDirection(_dodgeAnimatorDir)
                        && !Mathf.Approximately(_rollDistanceMultiplier, 1f))
                    {
                        deltaPosition.x *= _rollDistanceMultiplier;
                        deltaPosition.z *= _rollDistanceMultiplier;
                    }
                }

                deltaPosition.y += _velocity.y * Time.deltaTime;

                _controller.Move(deltaPosition);

                if (_applyRootRotationDuringDodge && !_dodgePreserveStrafeFacing
                    && _animator.deltaRotation != Quaternion.identity)
                    transform.rotation = transform.rotation * _animator.deltaRotation;
            }
        }

        /// <summary>Prevents locomotion root motion from accumulating on the mesh child while the capsule moves on the root.</summary>
        private void RealignVisualRigUnderBody()
        {
            Transform rig = _animator.transform;
            if (rig == transform)
                return;

            rig.localPosition = Vector3.zero;
            rig.localRotation = Quaternion.identity;
        }

        #endregion

        #endregion

        #region Shared State

        #region State Change

        /// <summary>
        ///     Switch the current state to the passed in state.
        /// </summary>
        /// <param name="newState">The state to switch to.</param>
        private void SwitchState(AnimationState newState)
        {
            ExitCurrentState();
            EnterState(newState);
        }

        /// <summary>
        ///     Enter the given state.
        /// </summary>
        /// <param name="stateToEnter">The state to enter.</param>
        private void EnterState(AnimationState stateToEnter)
        {
            _currentState = stateToEnter;
            SyncAnimatorApplyRootMotionForState();
            switch (_currentState)
            {
                case AnimationState.Base:
                    EnterBaseState();
                    break;
                case AnimationState.Locomotion:
                    EnterLocomotionState();
                    break;
                case AnimationState.Jump:
                    EnterJumpState();
                    break;
                case AnimationState.Fall:
                    EnterFallState();
                    break;
                case AnimationState.Crouch:
                    EnterCrouchState();
                    break;
                case AnimationState.Attack:
                    EnterAttackState();
                    break;
                case AnimationState.Dodge:
                    EnterDodgeState();
                    break;
            }
        }

        /// <summary>
        ///     Exit the current state.
        /// </summary>
        private void ExitCurrentState()
        {
            switch (_currentState)
            {
                case AnimationState.Locomotion:
                    ExitLocomotionState();
                    break;
                case AnimationState.Jump:
                    ExitJumpState();
                    break;
                case AnimationState.Crouch:
                    ExitCrouchState();
                    break;
                case AnimationState.Attack:
                    ExitAttackState();
                    break;
                case AnimationState.Dodge:
                    ExitDodgeState();
                    break;
            }
        }

        #endregion

        #region Updates

        /// <inheritdoc cref="Update" />
        private void Update()
        {
            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.ShouldSuppressBodyLocomotion)
            {
                ApplyMovingGroundRideWhileBodySuppressed();
                SoulRealmManager.Instance.SyncSpectralAnimatorControllerFromBody();
                ApplyComboOverridesIfReady();
                UpdateBestTarget();
                UpdateLockOnAnchorPosition();
                UpdateSoulRealmMeleeCombat();
                return;
            }

            ApplyComboOverridesIfReady();

            switch (_currentState)
            {
                case AnimationState.Locomotion:
                    UpdateLocomotionState();
                    break;
                case AnimationState.Jump:
                    UpdateJumpState();
                    break;
                case AnimationState.Fall:
                    UpdateFallState();
                    break;
                case AnimationState.Crouch:
                    UpdateCrouchState();
                    break;
                case AnimationState.Attack:
                    UpdateAttackState();
                    break;
                case AnimationState.Dodge:
                    UpdateDodgeState();
                    break;
            }
        }

        /// <summary>
        ///     Updates the animator to have the latest values.
        /// </summary>
        private void UpdateAnimatorController()
        {
            _animator.SetFloat(_leanValueHash, _leanValue);
            _animator.SetFloat(_headLookXHash, _headLookX);
            _animator.SetFloat(_headLookYHash, _headLookY);
            _animator.SetFloat(_bodyLookXHash, _bodyLookX);
            _animator.SetFloat(_bodyLookYHash, _bodyLookY);

            bool animatorStrafe = UseStrafeStyleLocomotionFacing;
            if (_currentState == AnimationState.Dodge && _dodgePreserveStrafeFacing)
                animatorStrafe = true;
            _animator.SetFloat(_isStrafingHash, animatorStrafe ? 1.0f : 0.0f);

            _animator.SetFloat(_inclineAngleHash, _inclineAngle);

            _animator.SetFloat(_moveSpeedHash, _speed2D);
            _animator.SetInteger(_currentGaitHash, (int) _currentGait);

            _animator.SetFloat(_strafeDirectionXHash, _strafeDirectionX);
            _animator.SetFloat(_strafeDirectionZHash, _strafeDirectionZ);
            _animator.SetFloat(_forwardStrafeHash, _forwardStrafe);
            _animator.SetFloat(_cameraRotationOffsetHash, _cameraRotationOffset);

            _animator.SetBool(_movementInputHeldHash, _movementInputHeld);
            _animator.SetBool(_movementInputPressedHash, _movementInputPressed);
            _animator.SetBool(_movementInputTappedHash, _movementInputTapped);
            _animator.SetFloat(_shuffleDirectionXHash, _shuffleDirectionX);
            _animator.SetFloat(_shuffleDirectionZHash, _shuffleDirectionZ);

            _animator.SetBool(_isTurningInPlaceHash, _isTurningInPlace);
            _animator.SetBool(_isCrouchingHash, _isCrouching);

            _animator.SetFloat(_fallingDurationHash, _fallingDuration);
            _animator.SetBool(_isGroundedHash, _isGrounded);

            _animator.SetBool(_isWalkingHash, _isWalking);
            _animator.SetBool(_isStoppedHash, _isStopped);

            _animator.SetFloat(_locomotionStartDirectionHash, _locomotionStartDirection);

            if (_animatorHasBowDrawing)
                _animator.SetBool(_bowDrawingHash, _isBowDrawing);
            if (_animatorHasBowDrawCharge)
                _animator.SetFloat(_bowDrawChargeHash, _bowDrawCharge);
            if (_animatorHasBowAiming)
            {
                bool bowAiming = IsBowEquipped && _isAiming;
                _animator.SetBool(_bowAimingHash, bowAiming);
            }
            if (_animatorHasBowChargedShotReady)
                _animator.SetBool(_bowChargedShotReadyHash, _isBowChargedShotReady);

            if (_bowDrawLayerIndex >= 0)
            {
                float targetBowLayerWeight = IsBowEquipped ? 1f : 0f;
                float blendSpeed = Mathf.Max(0f, _bowEquipLayerBlendSpeed);
                _currentBowDrawLayerWeight = blendSpeed > 0f
                    ? Mathf.MoveTowards(_currentBowDrawLayerWeight, targetBowLayerWeight, blendSpeed * Time.deltaTime)
                    : targetBowLayerWeight;
                _animator.SetLayerWeight(_bowDrawLayerIndex, _currentBowDrawLayerWeight);
            }
        }

        #endregion

        #endregion

        #region Base State

        #region Setup

        /// <summary>
        ///     Performs the actions required when entering the base state.
        /// </summary>
        private void EnterBaseState()
        {
            _previousRotation = transform.forward;
        }

        /// <summary>
        ///     Calculates the input type and sets the required internal states.
        /// </summary>
        private void CalculateInput()
        {
            bool moveFrozen = GeisInteractInput.IsMovementFrozenForInteraction;
            bool movementDetected = !moveFrozen && _inputReader._movementInputDetected;

            if (movementDetected)
            {
                if (_inputReader._movementInputDuration == 0)
                {
                    _movementInputTapped = true;
                }
                else if (_inputReader._movementInputDuration > 0 && _inputReader._movementInputDuration < _buttonHoldThreshold)
                {
                    _movementInputTapped = false;
                    _movementInputPressed = true;
                    _movementInputHeld = false;
                }
                else
                {
                    _movementInputTapped = false;
                    _movementInputPressed = false;
                    _movementInputHeld = true;
                }

                _inputReader._movementInputDuration += Time.deltaTime;
            }
            else
            {
                _inputReader._movementInputDuration = 0;
                _movementInputTapped = false;
                _movementInputPressed = false;
                _movementInputHeld = false;
            }

            Vector2 composite = GeisInteractInput.GetEffectiveMoveCompositeForLocomotion(_inputReader._moveComposite);
            _moveDirection = (_cameraController.GetCameraForwardZeroedYNormalised() * composite.y)
                + (_cameraController.GetCameraRightZeroedYNormalised() * composite.x);
        }

        #endregion

        #region Movement

        /// <summary>
        ///     Performs the movement of the player
        /// </summary>
        private void Move()
        {
            LayerMask rideMask = _groundLayerMask.value != 0 ? _groundLayerMask : (LayerMask)(-1);
            Vector3 groundRide = GroundRideUtility.GetRideDelta(
                transform, _controller, rideMask, _groundedOffset,
                ref _groundRideSurface, ref _groundRideLastWorldPos, _isGrounded);

            _controller.Move(groundRide + _velocity * Time.deltaTime);

            if (_isLockedOn && _targetLockOnPos != null && _currentLockOnTarget != null)
            {
                _targetLockOnPos.position = ResolveLockOnWorldPosition(_currentLockOnTarget);
                _lockOnIndicator?.SetAnchorTarget(_targetLockOnPos);
            }
            else
            {
                _lockOnIndicator?.ClearTarget();
            }
        }

        /// <summary>
        /// Soul realm suppresses full locomotion <see cref="Update"/>, but the body must still follow
        /// kinematic floors (see <see cref="GroundRideUtility"/>).
        /// </summary>
        private void ApplyMovingGroundRideWhileBodySuppressed()
        {
            if (_controller == null)
                return;

            GroundedCheck();
            LayerMask rideMask = _groundLayerMask.value != 0 ? _groundLayerMask : (LayerMask)(-1);
            Vector3 groundRide = GroundRideUtility.GetRideDelta(
                transform, _controller, rideMask, _groundedOffset,
                ref _groundRideSurface, ref _groundRideLastWorldPos, _isGrounded);
            if (groundRide.sqrMagnitude > 1e-12f)
                _controller.Move(groundRide);
        }

        /// <summary>
        ///     Applies gravity to the player.
        /// </summary>
        private void ApplyGravity()
        {
            if (_velocity.y > Physics.gravity.y)
            {
                _velocity.y += Physics.gravity.y * _gravityMultiplier * Time.deltaTime;
            }
        }

        /// <summary>
        ///     Calculates the movement direction of the player, and sets the relevant flags.
        /// </summary>
        private void CalculateMoveDirection()
        {
            CalculateInput();

            if (!_isGrounded)
            {
                _targetMaxSpeed = _currentMaxSpeed;
            }
            else if (IsBowMovementForcedWalk)
            {
                _targetMaxSpeed = _walkSpeed;
            }
            else if (_isCrouching)
            {
                _targetMaxSpeed = _walkSpeed;
            }
            else if (_isSprinting)
            {
                _targetMaxSpeed = _sprintSpeed;
            }
            else if (_isWalking)
            {
                _targetMaxSpeed = _walkSpeed;
            }
            else
            {
                _targetMaxSpeed = _runSpeed;
            }

            // Snap-up when the desired max speed grows (e.g. sprint pressed) so the transition isn't a long ramp.
            if (_targetMaxSpeed > _currentMaxSpeed && _sprintInstantFraction > 0f)
                _currentMaxSpeed = Mathf.Max(_currentMaxSpeed, _targetMaxSpeed * _sprintInstantFraction);

            _currentMaxSpeed = ExpSmooth(_currentMaxSpeed, _targetMaxSpeed, _ANIMATION_DAMP_TIME, Time.deltaTime);

            _targetVelocity.x = _moveDirection.x * _currentMaxSpeed;
            _targetVelocity.z = _moveDirection.z * _currentMaxSpeed;

            // Split accel (stick pushing to higher speed) vs decel (stick released or turning) so starts feel snappier
            // than stops. Falls back to legacy _speedChangeDamping when the action-feel rates are disabled (<= 0).
            float legacyRate = _speedChangeDamping;
            float targetPlanarSqr = _targetVelocity.x * _targetVelocity.x + _targetVelocity.z * _targetVelocity.z;
            float currentPlanarSqr = _velocity.x * _velocity.x + _velocity.z * _velocity.z;
            bool accelerating = targetPlanarSqr > currentPlanarSqr;
            float activeRate = accelerating
                ? (_accelRate > 0f ? _accelRate : legacyRate)
                : (_decelRate > 0f ? _decelRate : legacyRate);

            _velocity.x = ExpSmooth(_velocity.x, _targetVelocity.x, activeRate, Time.deltaTime);
            _velocity.z = ExpSmooth(_velocity.z, _targetVelocity.z, activeRate, Time.deltaTime);

            _speed2D = new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
            _speed2D = Mathf.Round(_speed2D * 1000f) / 1000f;

            Vector3 playerForwardVector = transform.forward;

            _newDirectionDifferenceAngle = playerForwardVector != _moveDirection
                ? Vector3.SignedAngle(playerForwardVector, _moveDirection, Vector3.up)
                : 0f;

            CalculateGait();
        }

        /// <summary>
        ///     <pre>
        ///         Calculates the character gait.
        ///         Calculate what the current locomotion gait is (Walk, Run, Sprint)
        ///         (for use in jumps, landings etc when deciding which animation to use)
        ///         Gait values will be:
        ///         Idle = 0, Walk = 1, Run = 2, Sprint = 3
        ///     </pre>
        /// </summary>
        private void CalculateGait()
        {
            float runThreshold = (_walkSpeed + _runSpeed) / 2;
            float sprintThreshold = (_runSpeed + _sprintSpeed) / 2;

            if (_speed2D < 0.01)
            {
                _currentGait = GaitState.Idle;
            }
            else if (IsBowMovementForcedWalk)
            {
                _currentGait = GaitState.Walk;
            }
            else if (_speed2D < runThreshold)
            {
                _currentGait = GaitState.Walk;
            }
            else if (_speed2D < sprintThreshold)
            {
                _currentGait = GaitState.Run;
            }
            else
            {
                _currentGait = GaitState.Sprint;
            }
        }

        /// <summary>
        ///     Calculates the face move direction based on the locomotion of the character.
        /// </summary>
        private void FaceMoveDirection()
        {
            Vector3 characterForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            Vector3 characterRight = new Vector3(transform.right.x, 0f, transform.right.z).normalized;
            Vector3 directionForward = new Vector3(_moveDirection.x, 0f, _moveDirection.z).normalized;

            _cameraForward = _cameraController.GetCameraForwardZeroedYNormalised();
            Quaternion strafingTargetRotation = GetBowFacingQuaternion();

            _strafeAngle = characterForward != directionForward ? Vector3.SignedAngle(characterForward, directionForward, Vector3.up) : 0f;

            _isTurningInPlace = false;
            _isIdleLooking = false;

            if (UseStrafeStyleLocomotionFacing)
            {
                if (_moveDirection.magnitude > 0.01)
                {
                    if (_cameraForward != Vector3.zero)
                    {
                        // Shuffle direction values - these are separate from the strafe values as we don't want to lerp, we need to know immediately
                        // what direction to shuffle, and then lock the value so it doesn't return to zero once we lose input (so the blend tree works
                        // to the end of the anim clip)
                        _shuffleDirectionZ = Vector3.Dot(characterForward, directionForward);
                        _shuffleDirectionX = Vector3.Dot(characterRight, directionForward);

                        UpdateStrafeDirection(
                            Vector3.Dot(characterForward, directionForward),
                            Vector3.Dot(characterRight, directionForward)
                        );
                        _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, 0f, _rotationSmoothing * Time.deltaTime);

                        float targetValue = _strafeAngle > _forwardStrafeMinThreshold && _strafeAngle < _forwardStrafeMaxThreshold ? 1f : 0f;

                        if (Mathf.Abs(_forwardStrafe - targetValue) <= 0.001f)
                        {
                            _forwardStrafe = targetValue;
                        }
                        else
                        {
                            float t = Mathf.Clamp01(_STRAFE_DIRECTION_DAMP_TIME * Time.deltaTime);
                            _forwardStrafe = Mathf.SmoothStep(_forwardStrafe, targetValue, t);
                        }
                    }

                    // Strafe body faces camera forward. While aiming, that forward is updated only by look (right stick / mouse) via GeisCameraController —
                    // left stick affects movement & strafe blend trees only, not yaw rotation.
                    transform.rotation = RotateTowardsClamped(transform.rotation, strafingTargetRotation);
                }
                else if (_isAiming && (_cameraForward != Vector3.zero || Mathf.Abs(_cameraController.GetCameraForward().y) > 0.001f))
                {
                    // Aim idle: face camera (look-driven yaw/pitch for bow). Horizontal projection can be ~0 when looking straight up/down.
                    transform.rotation = RotateTowardsClamped(transform.rotation, GetBowFacingQuaternion());
                    _isIdleLooking = true;
                    UpdateStrafeDirection(1f, 0f);
                    _shuffleDirectionZ = 1;
                    _shuffleDirectionX = 0;
                    _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, 0f, _rotationSmoothing * Time.deltaTime);
                }
                else
                {
                    // Idle: look only, no shuffle, no rotation
                    _isIdleLooking = true;
                    UpdateStrafeDirection(1f, 0f);
                    _shuffleDirectionZ = 1;
                    _shuffleDirectionX = 0;

                    float t = 20 * Time.deltaTime;
                    float newOffset = 0f;

                    if (characterForward != _cameraForward)
                    {
                        newOffset = Vector3.SignedAngle(characterForward, _cameraForward, Vector3.up);
                    }

                    _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, newOffset, t);
                }
            }
            else
            {
                UpdateStrafeDirection(1f, 0f);
                _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, 0f, _rotationSmoothing * Time.deltaTime);

                _shuffleDirectionZ = 1;
                _shuffleDirectionX = 0;

                Vector3 faceDirection = new Vector3(_velocity.x, 0f, _velocity.z);

                if (faceDirection == Vector3.zero)
                {
                    return;
                }

                transform.rotation = RotateTowardsClamped(transform.rotation, Quaternion.LookRotation(faceDirection));
            }
        }

        /// <summary>
        /// Exponentially smooths toward <paramref name="target"/> but caps the per-frame angular step to
        /// <see cref="_maxTurnDegPerSecond"/> so large redirects (e.g. 180° pivots) finish in bounded time
        /// instead of asymptotically creeping the last few degrees.
        /// </summary>
        private Quaternion RotateTowardsClamped(Quaternion current, Quaternion target)
        {
            Quaternion smoothed = Quaternion.Slerp(current, target,
                1f - Mathf.Exp(-_rotationSmoothing * Time.deltaTime));
            if (_maxTurnDegPerSecond <= 0f)
                return smoothed;
            return Quaternion.RotateTowards(current, target,
                Mathf.Max(_maxTurnDegPerSecond * Time.deltaTime,
                    Quaternion.Angle(current, smoothed)));
        }

        /// <summary>
        /// Bow aim root rotation uses only planar camera yaw. Up/down bow aim should come from the upper-body layer,
        /// not by pitching the whole character transform.
        /// </summary>
        private Quaternion GetBowFacingQuaternion()
        {
            Vector3 camFwdFull = _cameraController.GetCameraForward();
            if (camFwdFull.sqrMagnitude < 1e-8f)
                return transform.rotation;

            bool bowAimStance = IsBowEquipped && (_isAiming || _isBowDrawing);

            if (bowAimStance)
            {
                Vector3 camPlanar = camFwdFull;
                camPlanar.y = 0f;
                if (camPlanar.sqrMagnitude < 1e-8f)
                    return transform.rotation;

                Quaternion q = Quaternion.LookRotation(camPlanar.normalized, Vector3.up)
                    * Quaternion.Euler(0f, BowAimRootYawOffsetDegrees, 0f);
                q *= Quaternion.Euler(0f, _bowAimBodyEulerOffset.y, 0f);
                return q;
            }

            if (_cameraForward.sqrMagnitude < 1e-8f)
                return transform.rotation;

            return Quaternion.LookRotation(_cameraForward);
        }

        /// <summary>
        ///     Checks if the player has stopped moving.
        /// </summary>
        private void CheckIfStopped()
        {
            _isStopped = _moveDirection.magnitude == 0 && _speed2D < .5;
        }

        /// <summary>
        ///     Checks if the player has started moving.
        /// </summary>
        private void CheckIfStarting()
        {
            _locomotionStartTimer = VariableOverrideDelayTimer(_locomotionStartTimer);

            bool isStartingCheck = false;

            if (_locomotionStartTimer <= 0.0f)
            {
                if (_moveDirection.magnitude > 0.01 && _speed2D < 1 && !_isStrafing)
                {
                    isStartingCheck = true;
                }

                if (isStartingCheck)
                {
                    if (!_isStarting)
                    {
                        _locomotionStartDirection = _newDirectionDifferenceAngle;
                        _animator.SetFloat(_locomotionStartDirectionHash, _locomotionStartDirection);
                    }

                    float delayTime = 0.2f;
                    _leanDelay = delayTime;
                    _headLookDelay = delayTime;
                    _bodyLookDelay = delayTime;

                    _locomotionStartTimer = delayTime;
                }
            }
            else
            {
                isStartingCheck = true;
            }

            _isStarting = isStartingCheck;
            _animator.SetBool(_isStartingHash, _isStarting);
        }

        /// <summary>
        ///     Updates the strafe direction variables to those provided.
        /// </summary>
        /// <param name="TargetZ">The value to set for Z axis.</param>
        /// <param name="TargetX">The value to set for X axis.</param>
        private void UpdateStrafeDirection(float TargetZ, float TargetX)
        {
            _strafeDirectionZ = Mathf.Lerp(_strafeDirectionZ, TargetZ, _ANIMATION_DAMP_TIME * Time.deltaTime);
            _strafeDirectionX = Mathf.Lerp(_strafeDirectionX, TargetX, _ANIMATION_DAMP_TIME * Time.deltaTime);
            _strafeDirectionZ = Mathf.Round(_strafeDirectionZ * 1000f) / 1000f;
            _strafeDirectionX = Mathf.Round(_strafeDirectionX * 1000f) / 1000f;
        }

        #endregion

        #region Ground Checks

        /// <summary>
        ///     Checks if the character is grounded.
        /// </summary>
        private void GroundedCheck()
        {
            // Use bottom of CharacterController capsule (center - height/2) plus grounded offset for tolerance
            float sphereY = _controller.transform.position.y + _controller.center.y - (_controller.height * 0.5f) - _groundedOffset;
            Vector3 spherePosition = new Vector3(
                _controller.transform.position.x,
                sphereY,
                _controller.transform.position.z
            );
            // Fallback: if layer mask is "Nothing" (0), use all layers so ground is detected
            LayerMask mask = _groundLayerMask.value != 0 ? _groundLayerMask : (LayerMask)(-1);
            _isGrounded = Physics.CheckSphere(spherePosition, _controller.radius, mask, QueryTriggerInteraction.Ignore);

            if (_isGrounded)
            {
                GroundInclineCheck();
            }
        }

        /// <summary>
        ///     Checks for ground incline and sets the required variables.
        /// </summary>
        private void GroundInclineCheck()
        {
            float rayDistance = Mathf.Infinity;
            _rearRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0, 0);
            _frontRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0, 0);

            Physics.Raycast(_rearRayPos.position, _rearRayPos.TransformDirection(-Vector3.up), out RaycastHit rearHit, rayDistance, _groundLayerMask);
            Physics.Raycast(
                _frontRayPos.position,
                _frontRayPos.TransformDirection(-Vector3.up),
                out RaycastHit frontHit,
                rayDistance,
                _groundLayerMask
            );

            Vector3 hitDifference = frontHit.point - rearHit.point;
            float xPlaneLength = new Vector2(hitDifference.x, hitDifference.z).magnitude;

            _inclineAngle = Mathf.Lerp(_inclineAngle, Mathf.Atan2(hitDifference.y, xPlaneLength) * Mathf.Rad2Deg, 20f * Time.deltaTime);
        }

        /// <summary>
        ///     Checks the height of the ceiling above the player to make sure there is room to stand up if crouching.
        /// </summary>
        private void CeilingHeightCheck()
        {
            float rayDistance = Mathf.Infinity;
            float minimumStandingHeight = _capsuleStandingHeight - _frontRayPos.localPosition.y;

            Vector3 midpoint = new Vector3(transform.position.x, transform.position.y + _frontRayPos.localPosition.y, transform.position.z);
            if (Physics.Raycast(midpoint, transform.TransformDirection(Vector3.up), out RaycastHit ceilingHit, rayDistance, _groundLayerMask))
            {
                _cannotStandUp = ceilingHit.distance < minimumStandingHeight;
            }
            else
            {
                _cannotStandUp = false;
            }
        }

        #endregion

        #region Falling

        /// <summary>
        ///     Resets the falling duration variables.
        /// </summary>
        private void ResetFallingDuration()
        {
            _fallStartTime = Time.time;
            _fallingDuration = 0f;
        }

        /// <summary>
        ///     Updates the falling duration variables.
        /// </summary>
        private void UpdateFallingDuration()
        {
            _fallingDuration = Time.time - _fallStartTime;
        }

        #endregion

        #region Checks

        /// <summary>
        ///     Checks if body turns can be enabled, and enabled or disables as required.
        /// </summary>
        private void CheckEnableTurns()
        {
            _headLookDelay = VariableOverrideDelayTimer(_headLookDelay);
            _enableHeadTurn = _headLookDelay == 0.0f && !_isStarting;
            _bodyLookDelay = VariableOverrideDelayTimer(_bodyLookDelay);
            _enableBodyTurn = _bodyLookDelay == 0.0f && !_isStarting;
        }

        /// <summary>
        ///     Checks if lean can be enabled, then enabled or disables as required.
        /// </summary>
        private void CheckEnableLean()
        {
            _leanDelay = VariableOverrideDelayTimer(_leanDelay);
            _enableLean = _leanDelay == 0.0f && !(_isStarting || _isTurningInPlace);
        }

        #endregion

        #region Lean and Offsets

        /// <summary>
        ///     Calculates the required rotational additives based on the passed in parameters.
        /// </summary>
        /// <param name="leansActivated">If leans are activated or not.</param>
        /// <param name="headLookActivated">If head look is activated or not.</param>
        /// <param name="bodyLookActivated">If body look is activated or not.</param>
        private void CalculateRotationalAdditives(bool leansActivated, bool headLookActivated, bool bodyLookActivated)
        {
            if (headLookActivated || leansActivated || bodyLookActivated)
            {
                _currentRotation = transform.forward;

                _rotationRate = _currentRotation != _previousRotation
                    ? Vector3.SignedAngle(_currentRotation, _previousRotation, Vector3.up) / Time.deltaTime * -1f
                    : 0f;
            }

            _initialLeanValue = leansActivated ? _rotationRate : 0f;

            float leanSmoothness = 5;
            float maxLeanRotationRate = 275.0f;

            float referenceValue = _speed2D / _sprintSpeed;
            _leanValue = CalculateSmoothedValue(
                _leanValue,
                _initialLeanValue,
                maxLeanRotationRate,
                leanSmoothness,
                _leanCurve,
                referenceValue,
                true
            );

            float headTurnSmoothness = 5f;

            if (headLookActivated && (_isTurningInPlace || _isIdleLooking))
            {
                _initialTurnValue = Mathf.Clamp(_cameraRotationOffset, -_headLookLimitDegrees, _headLookLimitDegrees);
                _headLookX = Mathf.Lerp(_headLookX, _initialTurnValue / 200, 5f * Time.deltaTime);
            }
            else
            {
                _initialTurnValue = headLookActivated ? _rotationRate : 0f;
                _headLookX = CalculateSmoothedValue(
                    _headLookX,
                    _initialTurnValue,
                    maxLeanRotationRate,
                    headTurnSmoothness,
                    _headLookXCurve,
                    _headLookX,
                    false
                );
            }

            float bodyTurnSmoothness = 5f;

            if (bodyLookActivated && (_isTurningInPlace || _isIdleLooking))
            {
                _initialTurnValue = Mathf.Clamp(_cameraRotationOffset, -_headLookLimitDegrees, _headLookLimitDegrees);
                _bodyLookX = Mathf.Lerp(_bodyLookX, _initialTurnValue / 200, 5f * Time.deltaTime);
            }
            else
            {
                _initialTurnValue = bodyLookActivated ? _rotationRate : 0f;
                _bodyLookX = CalculateSmoothedValue(
                    _bodyLookX,
                    _initialTurnValue,
                    maxLeanRotationRate,
                    bodyTurnSmoothness,
                    _bodyLookXCurve,
                    _bodyLookX,
                    false
                );
            }

            float cameraTilt = _cameraController.GetCameraTiltX();
            cameraTilt = (cameraTilt > 180f ? cameraTilt - 360f : cameraTilt) / -180;
            cameraTilt = Mathf.Clamp(cameraTilt, -0.1f, 1.0f);
            _headLookY = cameraTilt;
            _bodyLookY = cameraTilt;

            if (IsBowEquipped && (_isAiming || _isBowDrawing) && !IsBowMovementForcedWalk)
            {
                _headLookX *= _bowAimHeadLookMultiplier;
                _headLookY *= _bowAimHeadLookMultiplier;
                _bodyLookX *= _bowAimBodyLookMultiplier;
                _bodyLookY *= _bowAimBodyLookMultiplier;
            }

            if (IsBowMovementForcedWalk)
            {
                // Keep the bow upper body planted while the lower-body locomotion continues underneath it.
                _leanValue = 0f;
                _headLookX = 0f;
                _headLookY = 0f;
                _bodyLookX = 0f;
                _bodyLookY = 0f;
            }

            _previousRotation = _currentRotation;
        }

        /// <summary>
        ///     Calculates a smoothed value between the given variable and target variable, from the given parameters.
        /// </summary>
        /// <param name="mainVariable">The variable to smooth.</param>
        /// <param name="newValue">The target new value.</param>
        /// <param name="maxRateChange">The max rate of change.</param>
        /// <param name="smoothness">The smoothness amount.</param>
        /// <param name="referenceCurve">The reference curve.</param>
        /// <param name="referenceValue">The reference value.</param>
        /// <param name="isMultiplier">If the value is a multiplier or not.</param>
        /// <returns>The smoothed value.</returns>
        private float CalculateSmoothedValue(
            float mainVariable,
            float newValue,
            float maxRateChange,
            float smoothness,
            AnimationCurve referenceCurve,
            float referenceValue,
            bool isMultiplier
        )
        {
            float changeVariable = newValue / maxRateChange;

            changeVariable = Mathf.Clamp(changeVariable, -1.0f, 1.0f);

            if (isMultiplier)
            {
                float multiplier = referenceCurve.Evaluate(referenceValue);
                changeVariable *= multiplier;
            }
            else
            {
                changeVariable = referenceCurve.Evaluate(changeVariable);
            }

            if (!changeVariable.Equals(mainVariable))
            {
                changeVariable = Mathf.Lerp(mainVariable, changeVariable, smoothness * Time.deltaTime);
            }

            return changeVariable;
        }

        /// <summary>
        ///     Provides a clamped override delay to avoid animation transition issues.
        /// </summary>
        /// <param name="timeVariable">The time variable to use.</param>
        /// <returns>A clamped override delay.</returns>
        private float VariableOverrideDelayTimer(float timeVariable)
        {
            if (timeVariable > 0.0f)
            {
                timeVariable -= Time.deltaTime;
                timeVariable = Mathf.Clamp(timeVariable, 0.0f, 1.0f);
            }
            else
            {
                timeVariable = 0.0f;
            }

            return timeVariable;
        }

        #endregion

        #region Lock-on System

        /// <summary>
        /// Uses ghost position in soul realm so lock-on candidate scoring matches the active avatar.
        /// </summary>
        private Vector3 GetLockOnDistanceEvaluationPosition()
        {
            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive)
                return SoulRealmManager.Instance.GetInteractionProximityWorldPosition();
            return transform.position;
        }

        /// <summary>
        /// Keeps the player lock-on aim point on the enemy while the body root does not move (soul realm).
        /// </summary>
        private void UpdateLockOnAnchorPosition()
        {
            if (_isLockedOn && _targetLockOnPos != null && _currentLockOnTarget != null)
            {
                _targetLockOnPos.position = ResolveLockOnWorldPosition(_currentLockOnTarget);
                _lockOnIndicator?.SetAnchorTarget(_targetLockOnPos);
            }
            else
            {
                _lockOnIndicator?.ClearTarget();
            }
        }

        private Vector3 ResolveLockOnWorldPosition(GameObject target)
        {
            if (target == null)
                return Vector3.zero;

            CombatEntity entity = target.GetComponent<CombatEntity>()
                ?? target.GetComponentInParent<CombatEntity>()
                ?? target.GetComponentInChildren<CombatEntity>();

            if (entity != null)
            {
                if (entity.hitPoint != null)
                    return entity.hitPoint.position;

                Renderer[] entityRenderers = entity.GetComponentsInChildren<Renderer>();
                if (TryGetBoundsCenter(entityRenderers, out var entityCenter))
                    return entityCenter;
            }

            Renderer[] targetRenderers = target.GetComponentsInChildren<Renderer>();
            if (TryGetBoundsCenter(targetRenderers, out var targetCenter))
                return targetCenter;

            return target.transform.position;
        }

        private static bool TryGetBoundsCenter(Renderer[] renderers, out Vector3 center)
        {
            center = Vector3.zero;
            if (renderers == null || renderers.Length == 0)
                return false;

            Bounds combinedBounds = default;
            bool foundBounds = false;

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (!foundBounds)
                {
                    combinedBounds = renderer.bounds;
                    foundBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!foundBounds)
                return false;

            center = combinedBounds.center;
            return true;
        }

        private List<GameObject> GetOrderedLockOnTargets()
        {
            var orderedTargets = new List<(GameObject target, float angle, float distance)>();

            foreach (GameObject target in _currentTargetCandidates)
            {
                if (target == null || !target.activeInHierarchy)
                    continue;

                Vector3 targetPosition = ResolveLockOnWorldPosition(target);
                float angle = GetLockOnHorizontalAngle(targetPosition);
                float distance = Vector3.SqrMagnitude(targetPosition - GetLockOnDistanceEvaluationPosition());
                orderedTargets.Add((target, angle, distance));
            }

            orderedTargets.Sort((a, b) =>
            {
                int angleCompare = a.angle.CompareTo(b.angle);
                return angleCompare != 0 ? angleCompare : a.distance.CompareTo(b.distance);
            });

            var result = new List<GameObject>(orderedTargets.Count);
            foreach (var targetInfo in orderedTargets)
                result.Add(targetInfo.target);

            return result;
        }

        private float GetLockOnHorizontalAngle(Vector3 targetPosition)
        {
            Vector3 fromCamera = targetPosition - _cameraController.GetCameraPosition();
            fromCamera.y = 0f;
            if (fromCamera.sqrMagnitude <= 0.0001f)
                return 0f;

            Vector3 cameraForward = _cameraController.GetCameraForward();
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude <= 0.0001f)
                cameraForward = transform.forward;
            cameraForward.Normalize();

            Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;
            fromCamera.Normalize();

            float horizontal = Vector3.Dot(fromCamera, cameraRight);
            float vertical = Vector3.Dot(fromCamera, cameraForward);
            return Mathf.Atan2(horizontal, vertical);
        }

        private void HighlightCurrentLockOnTarget()
        {
            foreach (GameObject target in _currentTargetCandidates)
            {
                if (target == null)
                    continue;

                target.GetComponent<Geis.Combat.GeisObjectLockOn>()?.Highlight(target == _currentLockOnTarget, target == _currentLockOnTarget);
            }
        }

        /// <summary>
        ///     Updates and sets the best target for lock on from the list of available targets.
        /// </summary>
        private void UpdateBestTarget()
        {
            GameObject newBestTarget;

            if (_currentTargetCandidates.Count == 0)
            {
                newBestTarget = null;
            }
            else if (_currentTargetCandidates.Count == 1)
            {
                newBestTarget = _currentTargetCandidates[0];
            }
            else
            {
                newBestTarget = null;
                float bestTargetScore = 0f;

                foreach (GameObject target in _currentTargetCandidates)
                {
                    target.GetComponent<Geis.Combat.GeisObjectLockOn>()?.Highlight(false, false);

                    Vector3 targetPosition = ResolveLockOnWorldPosition(target);
                    float distance = Vector3.Distance(GetLockOnDistanceEvaluationPosition(), targetPosition);
                    float distanceScore = 1 / distance * 100;

                    Vector3 targetDirection = targetPosition - _cameraController.GetCameraPosition();
                    float angleInView = Vector3.Dot(targetDirection.normalized, _cameraController.GetCameraForward());
                    float angleScore = angleInView * 40;

                    float totalScore = distanceScore + angleScore;

                    if (totalScore > bestTargetScore)
                    {
                        bestTargetScore = totalScore;
                        newBestTarget = target;
                    }
                }
            }

            if (!_isLockedOn)
            {
                _currentLockOnTarget = newBestTarget;

                if (_currentLockOnTarget != null)
                {
                    _currentLockOnTarget.GetComponent<Geis.Combat.GeisObjectLockOn>()?.Highlight(true, false);
                }
            }
            else
            {
                if (_currentTargetCandidates.Contains(_currentLockOnTarget))
                {
                    _currentLockOnTarget.GetComponent<Geis.Combat.GeisObjectLockOn>()?.Highlight(true, true);
                }
                else
                {
                    _currentLockOnTarget = newBestTarget;
                    EnableLockOn(false);
                }
            }
        }

        #endregion

        #endregion

        #region Locomotion State

        /// <summary>
        ///     Sets up the locomotion state upon entry.
        /// </summary>
        private void EnterLocomotionState()
        {
            _inputReader.onJumpPerformed += LocomotionToJumpState;
        }

        /// <summary>
        ///     Updates the locomotion state.
        /// </summary>
        private void UpdateLocomotionState()
        {
            UpdateBestTarget();
            GroundedCheck();

            // Recharge coyote time whenever we're genuinely on the ground in a locomotion-friendly state.
            if (_isGrounded)
                _coyoteTimer = _coyoteTimeSeconds;

            if (!_isGrounded)
            {
                SwitchState(AnimationState.Fall);
            }

            if (_isCrouching)
            {
                SwitchState(AnimationState.Crouch);
            }

            CheckEnableTurns();
            CheckEnableLean();
            CalculateRotationalAdditives(_enableLean, _enableHeadTurn, _enableBodyTurn);

            CalculateMoveDirection();
            CheckIfStarting();
            CheckIfStopped();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        /// <summary>
        ///     Performs the required actions when exiting the locomotion state.
        /// </summary>
        private void ExitLocomotionState()
        {
            _inputReader.onJumpPerformed -= LocomotionToJumpState;
        }

        /// <summary>
        ///     Moves from the locomotion to the jump state.
        /// </summary>
        private void LocomotionToJumpState()
        {
            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.ShouldSuppressBodyLocomotion)
                return;
            SwitchState(AnimationState.Jump);
        }

        #endregion

        #region Jump State

        /// <summary>
        ///     Sets up the Jump state upon entry.
        /// </summary>
        private void EnterJumpState()
        {
            _animator.SetBool(_isJumpingAnimHash, true);

            _isSliding = false;
            // Consume any pending buffered jump so it doesn't re-fire on the upcoming Fall landing.
            _jumpBufferedAt = -1f;

            _velocity = new Vector3(_velocity.x, _jumpForce, _velocity.z);
        }

        /// <summary>
        ///     updates the jump state.
        /// </summary>
        private void UpdateJumpState()
        {
            UpdateBestTarget();

            // Decay coyote while airborne so a jump-then-dropkick chain doesn't get a second free coyote jump on apex.
            if (_coyoteTimer > 0f)
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.deltaTime);

            ApplyGravity();

            if (_velocity.y <= 0f)
            {
                _animator.SetBool(_isJumpingAnimHash, false);
                SwitchState(AnimationState.Fall);
            }

            GroundedCheck();

            CalculateRotationalAdditives(false, _enableHeadTurn, _enableBodyTurn);
            CalculateMoveDirection();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        /// <summary>
        ///     Performs the required actions upon exiting the jump state.
        /// </summary>
        private void ExitJumpState()
        {
            _animator.SetBool(_isJumpingAnimHash, false);
        }

        #endregion

        #region Fall State

        /// <summary>
        ///     Sets up the fall state upon entry.
        /// </summary>
        private void EnterFallState()
        {
            ResetFallingDuration();
            _velocity.y = 0f;

            DeactivateCrouch();
            _isSliding = false;
        }

        /// <summary>
        ///     Updates the fall state.
        /// </summary>
        private void UpdateFallState()
        {
            UpdateBestTarget();

            // Coyote timer decays while airborne so a late jump press no longer qualifies after the window.
            if (_coyoteTimer > 0f)
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.deltaTime);

            CalculateRotationalAdditives(false, _enableHeadTurn, _enableBodyTurn);

            CalculateMoveDirection();
            FaceMoveDirection();

            ApplyGravity();
            Move();

            // GroundedCheck must run AFTER Move() so we detect landing using the new position
            GroundedCheck();
            UpdateAnimatorController();

            // Use _isGrounded (Physics.CheckSphere) instead of _controller.isGrounded - CharacterController
            // isGrounded is unreliable and often fails to detect landing
            if (_isGrounded)
            {
                // Jump buffer: a jump pressed just before landing fires immediately instead of being dropped.
                if (IsJumpBufferFresh())
                {
                    _jumpBufferedAt = -1f;
                    _coyoteTimer = _coyoteTimeSeconds;
                    SwitchState(AnimationState.Jump);
                }
                else
                {
                    SwitchState(AnimationState.Locomotion);
                }
            }

            UpdateFallingDuration();
        }

        #endregion

        #region Crouch State

        /// <summary>
        ///     Sets up the crouch state upon entry.
        /// </summary>
        private void EnterCrouchState()
        {
            _inputReader.onJumpPerformed += CrouchToJumpState;
        }

        /// <summary>
        ///     Updates the crouch state.
        /// </summary>
        private void UpdateCrouchState()
        {
            UpdateBestTarget();

            GroundedCheck();
            if (!_isGrounded)
            {
                DeactivateCrouch();
                CapsuleCrouchingSize(false);
                SwitchState(AnimationState.Fall);
            }

            CeilingHeightCheck();

            if (!_crouchKeyPressed && !_cannotStandUp)
            {
                DeactivateCrouch();
                SwitchToLocomotionState();
            }

            if (!_isCrouching)
            {
                CapsuleCrouchingSize(false);
                SwitchToLocomotionState();
            }

            CheckEnableTurns();
            CheckEnableLean();

            CalculateRotationalAdditives(false, _enableHeadTurn, false);

            CalculateMoveDirection();
            CheckIfStarting();
            CheckIfStopped();

            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        /// <summary>
        ///     Performs the required actions upon exiting the crouch state.
        /// </summary>
        private void ExitCrouchState()
        {
            _inputReader.onJumpPerformed -= CrouchToJumpState;
        }

        /// <summary>
        ///     Moves from the crouch state to the jump state.
        /// </summary>
        private void CrouchToJumpState()
        {
            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.ShouldSuppressBodyLocomotion)
                return;
            if (!_cannotStandUp)
            {
                DeactivateCrouch();
                SwitchState(AnimationState.Jump);
            }
        }

        /// <summary>
        ///     Moves from the crouch state to the locomotion state.
        /// </summary>
        private void SwitchToLocomotionState()
        {
            DeactivateCrouch();
            SwitchState(AnimationState.Locomotion);
        }

        #endregion
    }
}
