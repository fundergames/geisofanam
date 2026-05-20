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
        private const int COMBO_BLEND_SLOTS = GeisComboAnimatorBlend.DefaultSlotCount;

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
        private bool _hasFallingBlendParameter;
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

        /// <summary>Post-<see cref="GroundedCheck"/> grounded state.</summary>
        public bool LocomotionIsGrounded => _isGrounded;

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

        /// <summary>Max seconds between dodge presses for double-tap roll (soul ghost mirrors body).</summary>
        public float LocomotionDodgeDoubleTapWindow => _dodgeDoubleTapWindow;

        /// <summary>When true, a second dodge press within the window triggers a roll clip on the spectral rig.</summary>
        public bool LocomotionDodgeDoubleTapRollEnabled => _dodgeDoubleTapRollEnabled;

        /// <summary>Scripted soul-ghost dodge duration for roll clips (uses body dodge fallback clip length).</summary>
        public float LocomotionDodgeScriptedRollDuration => _dodgeFallbackDuration;

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
            if (_currentState != AnimationState.Attack || PresentationAnimator == null)
                return false;

            GeisComboData comboData = GetCurrentComboData();
            if (comboData == null)
                return false;

            AnimatorStateInfo info = PresentationAnimator.GetCurrentAnimatorStateInfo(0);
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
            if (_defensiveCombatState == null || PresentationAnimator == null)
                return;

            bool invuln = false;
            if (_currentState == AnimationState.Dodge)
            {
                AnimatorStateInfo info = PresentationAnimator.GetCurrentAnimatorStateInfo(0);
                if (_dodgeAnimatorEnteredLeaf
                    && IsDodgeLeafShortNameHash(info.shortNameHash)
                    && !PresentationAnimator.IsInTransition(0)
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
                _hasFallingBlendParameter = AnimatorParameterGuard.HasParameter(_animator, "FallingBlend");
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
            SyncCapsuleToLocomotionController(crouching);
        }

        #endregion

        #endregion

        private bool HasAnimatorParameter(string name) =>
            PresentationAnimator != null && AnimatorParameterGuard.HasParameter(PresentationAnimator, name);

        private void RefreshPresentationAnimatorCaches()
        {
            Animator anim = PresentationAnimator;
            if (anim == null)
                return;

            _animatorHasBowDrawing = AnimatorParameterGuard.HasParameter(anim, "BowDrawing");
            _animatorHasBowDrawCharge = AnimatorParameterGuard.HasParameter(anim, "BowDrawCharge");
            _animatorHasBowAiming = AnimatorParameterGuard.HasParameter(anim, "BowAiming");
            _animatorHasBowChargedShotReady = AnimatorParameterGuard.HasParameter(anim, "BowChargedShotReady");
            _hasFallingBlendParameter = AnimatorParameterGuard.HasParameter(anim, "FallingBlend");
            _bowDrawLayerIndex = anim.GetLayerIndex(BowDrawLayerName);
            if (_bowDrawLayerIndex >= 0)
            {
                _currentBowDrawLayerWeight = IsBowEquipped ? 1f : 0f;
                anim.SetLayerWeight(_bowDrawLayerIndex, _currentBowDrawLayerWeight);
            }
        }

    }
}
