// Geis of Anam - Copy of Synty SamplePlayerAnimationController as starting point.
// Original: Synty.AnimationBaseLocomotion.Samples.SamplePlayerAnimationController

using System;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System.Collections.Generic;
using UnityEngine;
using Geis.Combat;
using Geis.Combat.Music;
using Geis.InputSystem;
using Geis.Attributes;
using Geis.SoulRealm;

namespace Geis.Locomotion
{
    /// <summary>
    /// Runs after <see cref="Puzzles.PlatformMover"/> (-50) so <see cref="GroundRideUtility"/> sees this frame&apos;s platform motion.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class GeisPlayerAnimationController : MonoBehaviour
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

        private readonly int _dodgeDirectionHash = Animator.StringToHash("DodgeDirection");
        private readonly int _dodgeTriggerHash = Animator.StringToHash("Dodge");

        /// <summary>Layer 0 leaf state shortNameHashes for <c>Dodge</c> sub-state machine (must match Animator).</summary>
        private static readonly int _dodgeLeafFrontHash = Animator.StringToHash("Dodge_Front");
        private static readonly int _dodgeLeafBackHash = Animator.StringToHash("Dodge_Back");
        private static readonly int _dodgeLeafLeftHash = Animator.StringToHash("Dodge_Left");
        private static readonly int _dodgeLeafRightHash = Animator.StringToHash("Dodge_Right");

        #endregion

        #region Player Settings Variables

        #region Scripts/Objects

        [FoldoutGroup("External Components")]
        [Tooltip("Script controlling camera behavior")]
        [SerializeField]
        private GeisCameraController _cameraController;
        [FoldoutGroup("External Components")]
        [Tooltip("InputReader handles player input")]
        [SerializeField]
        private GeisInputReader _inputReader;
        [FoldoutGroup("External Components")]
        [Tooltip("Animator component for controlling player animations")]
        [SerializeField]
        private Animator _animator;
        [FoldoutGroup("External Components")]
        [Tooltip("Character Controller component for controlling player movement")]
        [SerializeField]
        private CharacterController _controller;

        #endregion

        #region Locomotion Settings

        [FoldoutGroup("Player Locomotion")]
        [Tooltip("Whether the character always faces the camera facing direction")]
        [SerializeField]
        private bool _alwaysStrafe = true;
        [FoldoutGroup("Player Locomotion")]
        [Tooltip("Slowest movement speed of the player when set to a walk state or half press tick")]
        [SerializeField]
        private float _walkSpeed = 1.4f;
        [FoldoutGroup("Player Locomotion")]
        [Tooltip("Default movement speed of the player")]
        [SerializeField]
        private float _runSpeed = 2.5f;
        [FoldoutGroup("Player Locomotion")]
        [Tooltip("Top movement speed of the player")]
        [SerializeField]
        private float _sprintSpeed = 7f;
        [FoldoutGroup("Player Locomotion")]
        [Tooltip("Damping factor for changing speed")]
        [SerializeField]
        private float _speedChangeDamping = 10f;
        [FoldoutGroup("Player Locomotion")]
        [Tooltip("Rotation smoothing factor.")]
        [SerializeField]
        private float _rotationSmoothing = 10f;
        [FoldoutGroup("Player Locomotion")]
        [Tooltip("Offset for camera rotation.")]
        [SerializeField]
        private float _cameraRotationOffset;

        #endregion

        #region Shuffle Settings

        [FoldoutGroup("Shuffles")]
        [Tooltip("Threshold for button hold duration.")]
        [SerializeField]
        private float _buttonHoldThreshold = 0.15f;
        [FoldoutGroup("Shuffles")]
        [Tooltip("Direction of shuffling on the X-axis.")]
        [SerializeField]
        private float _shuffleDirectionX;
        [FoldoutGroup("Shuffles")]
        [Tooltip("Direction of shuffling on the Z-axis.")]
        [SerializeField]
        private float _shuffleDirectionZ;

        #endregion

        #region Capsule Settings

        [FoldoutGroup("Capsule Values")]
        [Tooltip("Standing height of the player capsule.")]
        [SerializeField]
        private float _capsuleStandingHeight = 1.8f;
        [FoldoutGroup("Capsule Values")]
        [Tooltip("Standing center of the player capsule.")]
        [SerializeField]
        private float _capsuleStandingCentre = 0.93f;
        [FoldoutGroup("Capsule Values")]
        [Tooltip("Crouching height of the player capsule.")]
        [SerializeField]
        private float _capsuleCrouchingHeight = 1.2f;
        [FoldoutGroup("Capsule Values")]
        [Tooltip("Crouching center of the player capsule.")]
        [SerializeField]
        private float _capsuleCrouchingCentre = 0.6f;

        #endregion

        #region Strafing

        [FoldoutGroup("Player Strafing")]
        [Tooltip("Minimum threshold for forward strafing angle.")]
        [SerializeField]
        private float _forwardStrafeMinThreshold = -55.0f;
        [FoldoutGroup("Player Strafing")]
        [Tooltip("Maximum threshold for forward strafing angle.")]
        [SerializeField]
        private float _forwardStrafeMaxThreshold = 125.0f;
        [FoldoutGroup("Player Strafing")]
        [Tooltip("Current forward strafing value.")]
        [SerializeField]
        private float _forwardStrafe = 1f;

        #endregion

        #region Grounded Settings

        [FoldoutGroup("Grounded Angle")]
        [Tooltip("Position of the rear ray for grounded angle check.")]
        [SerializeField]
        private Transform _rearRayPos;
        [FoldoutGroup("Grounded Angle")]
        [Tooltip("Position of the front ray for grounded angle check.")]
        [SerializeField]
        private Transform _frontRayPos;
        [FoldoutGroup("Grounded Angle")]
        [Tooltip("Layer mask for checking ground. Default: all layers. If ground isn't detected, ensure your ground has a collider and is on a layer included here.")]
        [SerializeField]
        private LayerMask _groundLayerMask = ~0;
        [FoldoutGroup("Grounded Angle")]
        [Tooltip("Current incline angle.")]
        [SerializeField]
        private float _inclineAngle;
        [FoldoutGroup("Grounded Angle")]
        [Tooltip("Offset below character center for ground check sphere. Positive = below feet for detection.")]
        [SerializeField]
        private float _groundedOffset = 0.14f;

        #endregion

        #region In-Air Settings

        [FoldoutGroup("Player In-Air")]
        [Tooltip("Force applied when the player jumps.")]
        [SerializeField]
        private float _jumpForce = 10f;
        [FoldoutGroup("Player In-Air")]
        [Tooltip("Multiplier for gravity when in the air.")]
        [SerializeField]
        private float _gravityMultiplier = 2f;
        [FoldoutGroup("Player In-Air")]
        [Tooltip("Duration of falling.")]
        [SerializeField]
        private float _fallingDuration;

        #endregion

        #region Head Look Settings

        [FoldoutGroup("Player Head Look")]
        [Tooltip("Flag indicating if head turning is enabled.")]
        [SerializeField]
        private bool _enableHeadTurn = true;
        [FoldoutGroup("Player Head Look")]
        [Tooltip("Delay for head turning.")]
        [SerializeField]
        private float _headLookDelay;
        [FoldoutGroup("Player Head Look")]
        [Tooltip("X-axis value for head turning.")]
        [SerializeField]
        private float _headLookX;
        [FoldoutGroup("Player Head Look")]
        [Tooltip("Y-axis value for head turning.")]
        [SerializeField]
        private float _headLookY;
        [FoldoutGroup("Player Head Look")]
        [Tooltip("Curve for X-axis head turning.")]
        [SerializeField]
        private AnimationCurve _headLookXCurve;
        [FoldoutGroup("Player Head Look")]
        [Tooltip("Degrees beyond which head/body look can't follow; character rotates in place instead. Tune to match animator head look limit.")]
        [SerializeField]
        private float _headLookLimitDegrees = 60f;

        #endregion

        #region Body Look Settings

        [FoldoutGroup("Player Body Look")]
        [Tooltip("Flag indicating if body turning is enabled.")]
        [SerializeField]
        private bool _enableBodyTurn = true;
        [FoldoutGroup("Player Body Look")]
        [Tooltip("Delay for body turning.")]
        [SerializeField]
        private float _bodyLookDelay;
        [FoldoutGroup("Player Body Look")]
        [Tooltip("X-axis value for body turning.")]
        [SerializeField]
        private float _bodyLookX;
        [FoldoutGroup("Player Body Look")]
        [Tooltip("Y-axis value for body turning.")]
        [SerializeField]
        private float _bodyLookY;
        [FoldoutGroup("Player Body Look")]
        [Tooltip("Curve for X-axis body turning.")]
        [SerializeField]
        private AnimationCurve _bodyLookXCurve;

        #endregion

        #region Lean Settings

        [FoldoutGroup("Player Lean")]
        [Tooltip("Flag indicating if leaning is enabled.")]
        [SerializeField]
        private bool _enableLean = true;
        [FoldoutGroup("Player Lean")]
        [Tooltip("Delay for leaning.")]
        [SerializeField]
        private float _leanDelay;
        [FoldoutGroup("Player Lean")]
        [Tooltip("Current value for leaning.")]
        [SerializeField]
        private float _leanValue;
        [FoldoutGroup("Player Lean")]
        [Tooltip("Curve for leaning.")]
        [SerializeField]
        private AnimationCurve _leanCurve;
        [FoldoutGroup("Player Lean")]
        [Tooltip("Delay for head leaning looks.")]
        [SerializeField]
        private float _leansHeadLooksDelay;
        [FoldoutGroup("Player Lean")]
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

        [FoldoutGroup("Attack Root Motion")]
        [Tooltip("Apply animation root rotation during attacks. Disable if attacks drift left/right (baked rotation mismatch).")]
        [SerializeField]
        private bool _applyRootRotationDuringAttack;

        [FoldoutGroup("Data-Driven Combo")]
        [Tooltip("Combo data (transitions + clips). When null, uses legacy Attack_1 if available.")]
        [SerializeField]
        private GeisComboData _comboData;
        [FoldoutGroup("Data-Driven Combo")]
        [Tooltip("Optional: resolves combo by weapon index when set. Takes precedence over _comboData when both assigned.")]
        [SerializeField]
        private GeisWeaponComboData _weaponComboData;
        [FoldoutGroup("Data-Driven Combo")]
        [Tooltip("Optional: provides current weapon index for _weaponComboData lookup.")]
        [SerializeField]
        private GeisWeaponSwitcher _weaponSwitcher;
        [FoldoutGroup("Data-Driven Combo")]
        [Tooltip("Optional: placeholders for runtime override. Loaded from Resources/GeisComboPlaceholders if null.")]
        [SerializeField]
        private GeisComboPlaceholders _comboPlaceholders;

        [FoldoutGroup("Dodge Roll")]
        [Tooltip("Apply animation root rotation during dodge clips.")]
        [SerializeField]
        private bool _applyRootRotationDuringDodge;
        [FoldoutGroup("Dodge Roll")]
        [Tooltip("Stick magnitude below this counts as neutral (forward dodge).")]
        [SerializeField]
        private float _dodgeInputDeadzone = 0.05f;
        [FoldoutGroup("Dodge Roll")]
        [Tooltip("Fallback seconds if clip length cannot be read.")]
        [SerializeField]
        private float _dodgeFallbackDuration = 1.2f;
        [FoldoutGroup("Dodge Roll")]
        [Tooltip("If true, dodge only when movement stick exceeds deadzone.")]
        [SerializeField]
        private bool _requireMovementInputForDodge;
        [FoldoutGroup("Dodge Roll")]
        [Tooltip("Soul-ghost scripted dodge planar speed; physical dodge uses animation clips.")]
        [SerializeField]
        private float _dodgeScriptedPlaneSpeed = 7f;
        [FoldoutGroup("Dodge Roll")]
        [Tooltip("Soul-ghost scripted dodge duration in seconds.")]
        [SerializeField]
        private float _dodgeScriptedDuration = 0.35f;

        #endregion

        #endregion

        #region Runtime Properties

        private readonly List<GameObject> _currentTargetCandidates = new List<GameObject>();
        private AnimationState _currentState = AnimationState.Base;
        private bool _cannotStandUp;
        private bool _crouchKeyPressed;
        private bool _isAiming;
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
        private Transform _targetLockOnPos;
        private Vector3 _currentRotation = new Vector3(0f, 0f, 0f);
        private Vector3 _moveDirection;
        private Vector3 _previousRotation;
        private Vector3 _velocity;

        private float _attackStateTimeout;
        private float _dodgeStateTimeout;
        private bool _loggedDodgeAnimatorMissing;
        /// <summary>True if dodge started while strafing — keep camera-relative facing instead of snapping to dodge axis.</summary>
        private bool _dodgePreserveStrafeFacing;
        /// <summary>Set once layer 0 actually enters a Dodge_* clip (avoids exiting before Any-State transition fires).</summary>
        private bool _dodgeAnimatorEnteredLeaf;

        // Data-driven combo
        private int _currentComboState;
        private GeisComboInputType? _comboInputBuffered;
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

        /// <summary>
        /// Called when entering soul realm: locomotion <see cref="Update"/> is suppressed, but walk toggles
        /// still call <see cref="ToggleWalk"/> and update <see cref="_isWalking"/>. Use this so the ghost reads
        /// a defined run/walk state (e.g. default run) instead of a stale frozen flag.
        /// </summary>
        public void SetWalkLocomotionForSoulRealm(bool walkEnabled)
        {
            EnableWalk(walkEnabled);
        }
        public bool LocomotionIsStrafing => _isStrafing;

        /// <summary>
        /// Strafe-style facing (camera-forward) vs velocity-facing. Run gait uses forward run like sprint unless aim/lock-on needs strafe.
        /// </summary>
        private bool UseStrafeStyleLocomotionFacing =>
            _isStrafing && !(_currentGait == GaitState.Run && !_isLockedOn && !_isAiming);

        /// <summary>Same value as the <c>IsStrafing</c> animator float — for spectral mirror / tooling.</summary>
        public bool LocomotionAnimatorUsesStrafeStyle => UseStrafeStyleLocomotionFacing;
        /// <summary>True while aim (LT) is held — used by bow / ranged.</summary>
        public bool IsAiming => _isAiming;

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
            if (_currentState == AnimationState.Jump || _currentState == AnimationState.Fall)
                SwitchState(AnimationState.Locomotion);

            GroundedCheck();
            _velocity.y = _isGrounded ? -2f : 0f;
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

        /// <inheritdoc cref="Start" />
        private void Start()
        {
            _targetLockOnPos = transform.Find("TargetLockOnPos");
            if (_targetLockOnPos == null)
            {
                var go = new GameObject("TargetLockOnPos");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                _targetLockOnPos = go.transform;
            }

            _inputReader.onLockOnToggled += ToggleLockOn;
            _inputReader.onWalkToggled += ToggleWalk;
            _inputReader.onSprintActivated += ActivateSprint;
            _inputReader.onSprintDeactivated += DeactivateSprint;
            _inputReader.onCrouchActivated += ActivateCrouch;
            _inputReader.onCrouchDeactivated += DeactivateCrouch;
            _inputReader.onAimActivated += ActivateAim;
            _inputReader.onAimDeactivated += DeactivateAim;
            _inputReader.onLightAttackPerformed += OnLightAttackRequested;
            _inputReader.onHeavyAttackPerformed += OnHeavyAttackRequested;
            _inputReader.onDodgePerformed += OnDodgeRequested;

            _isStrafing = _alwaysStrafe;

            _useDataDrivenCombo = _comboData != null && _animator != null && HasAnimatorParameter("Attack")
                && (HasAnimatorParameter("ComboStateBlend") || HasAnimatorParameter("ComboState"));

            ApplyComboOverridesIfReady();

            SwitchState(AnimationState.Locomotion);
        }

        private void OnDestroy()
        {
            if (_inputReader != null)
                _inputReader.onDodgePerformed -= OnDodgeRequested;

            if (_comboOverrideController != null)
            {
                Destroy(_comboOverrideController);
                _comboOverrideController = null;
            }
        }

        #endregion

        #region Aim and Lock-on

        /// <summary>
        ///     Activates the aim action of the player.
        /// </summary>
        private void ActivateAim()
        {
            _isAiming = true;

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
                var lockOn = _currentLockOnTarget.GetComponent<Synty.AnimationBaseLocomotion.Samples.SampleObjectLockOn>();
                lockOn?.Highlight(true, true);
            }
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
            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive)
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
                _comboInputBuffered = GeisComboInputType.Light;
            }
        }

        private void OnHeavyAttackRequested()
        {
            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive)
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
                _comboInputBuffered = GeisComboInputType.Heavy;
            }
        }

        private void OnDodgeRequested()
        {
            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive)
                return;
            if (_currentState != AnimationState.Locomotion || !_isGrounded || _isCrouching)
                return;
            if (_animator == null || !HasAnimatorParameter("Dodge") || !HasAnimatorParameter("DodgeDirection"))
            {
                if (!_loggedDodgeAnimatorMissing)
                {
                    _loggedDodgeAnimatorMissing = true;
                    Debug.LogWarning(
                        "[GeisPlayerAnimationController] Animator is missing Dodge (Trigger) and/or DodgeDirection (Int), or dodge states. " +
                        "Run menu: Geis → Animator → Setup Dodge Rolls (AC_Polygon_Masculine_Geis).");
                }

                return;
            }
            if (_requireMovementInputForDodge &&
                _inputReader._moveComposite.sqrMagnitude < _dodgeInputDeadzone * _dodgeInputDeadzone)
                return;

            SwitchState(AnimationState.Dodge);
        }

        private int ComputeDodgeDirectionIndex()
        {
            Vector2 m = _inputReader._moveComposite;
            if (m.sqrMagnitude < _dodgeInputDeadzone * _dodgeInputDeadzone)
                return 0;

            Vector3 camFwd = _cameraController.GetCameraForwardZeroedYNormalised();
            Vector3 camRight = _cameraController.GetCameraRightZeroedYNormalised();
            Vector3 world = (camFwd * m.y + camRight * m.x).normalized;
            Vector3 local = transform.InverseTransformDirection(world);
            float lx = local.x;
            float lz = local.z;
            if (Mathf.Abs(lz) >= Mathf.Abs(lx))
                return lz >= 0f ? 0 : 1;
            return lx >= 0f ? 3 : 2;
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
            _velocity.x = 0f;
            _velocity.z = 0f;

            _dodgePreserveStrafeFacing = _isStrafing;

            int dir = ComputeDodgeDirectionIndex();
            if (_animator != null && HasAnimatorParameter("DodgeDirection"))
                _animator.SetInteger(_dodgeDirectionHash, dir);

            // Strafing keeps the body facing camera forward; only snap yaw to dodge axis when not strafing (e.g. sprint).
            if (!_dodgePreserveStrafeFacing)
            {
                Vector3 face = GetDodgeFacingWorld(dir);
                if (face.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(face);
            }

            if (_animator != null && HasAnimatorParameter("Dodge"))
                _animator.SetTrigger(_dodgeTriggerHash);

            _dodgeAnimatorEnteredLeaf = false;
            _dodgeStateTimeout = _dodgeFallbackDuration;
        }

        private static bool IsDodgeLeafShortNameHash(int shortNameHash)
        {
            return shortNameHash == _dodgeLeafFrontHash || shortNameHash == _dodgeLeafBackHash
                || shortNameHash == _dodgeLeafLeftHash || shortNameHash == _dodgeLeafRightHash;
        }

        private void UpdateDodgeState()
        {
            ApplyGravity();
            _dodgeStateTimeout -= Time.deltaTime;

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

                // Do not rely on normalizedTime alone: after the dodge clip the Animator transitions to Idle_Standing
                // (exit ~0.92). Layer 0 then reports Idle's normalizedTime, so the old >= 0.99 check never passes and
                // gameplay stayed in Dodge until _dodgeFallbackDuration (~1.2s) with zero scripted locomotion.
                if (_dodgeAnimatorEnteredLeaf && !_animator.IsInTransition(0)
                    && !IsDodgeLeafShortNameHash(info.shortNameHash))
                {
                    SwitchState(AnimationState.Locomotion);
                    return;
                }

                if (info.length > 0.01f && info.normalizedTime >= 0.99f && !_animator.IsInTransition(0)
                    && IsDodgeLeafShortNameHash(info.shortNameHash))
                {
                    SwitchState(AnimationState.Locomotion);
                    return;
                }
            }

            if (_dodgeStateTimeout <= 0f)
                SwitchState(AnimationState.Locomotion);
            else
                UpdateAnimatorController();
        }

        private void ExitDodgeState()
        {
        }

        private void EnterAttackState()
        {
            _velocity.x = 0f;
            _velocity.z = 0f;

            if (_useDataDrivenCombo && _animator != null && HasAnimatorParameter("Attack")
                && (HasAnimatorParameter("ComboStateBlend") || HasAnimatorParameter("ComboState")))
            {
                SetComboStateBlend(_currentComboState);
                _animator.SetTrigger(_attackTriggerHash);
                var comboData = GetCurrentComboData();
                _attackStateTimeout = comboData != null ? 2f : 1.5f;
                int weaponIdx = GetWeaponIndexForMusic();
                CombatMusicController.Instance?.OnAttackPerformed(_firstAttackInputType, _currentComboState, weaponIdx);
                OnAttackPerformed?.Invoke(weaponIdx);
            }
            else if (_animator != null && HasAnimatorParameter("Attack_1"))
            {
                _animator.SetTrigger(_attack1Hash);
                _attackStateTimeout = 1.5f;
                int weaponIdx = GetWeaponIndexForMusic();
                CombatMusicController.Instance?.OnAttackPerformed(_firstAttackInputType, 0, weaponIdx);
                OnAttackPerformed?.Invoke(weaponIdx);
            }
        }

        private int GetWeaponIndexForMusic()
        {
            return _weaponSwitcher != null ? _weaponSwitcher.CurrentWeaponIndex : 0;
        }

        private void UpdateAttackState()
        {
            ApplyGravity();
            _attackStateTimeout -= Time.deltaTime;

            var comboData = GetCurrentComboData();

            if (_useDataDrivenCombo && comboData != null && _animator != null)
            {
                AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
                float normalizedTime = info.normalizedTime % 1f;
                bool inCancelWindow = normalizedTime >= comboData.CancelWindowStart && normalizedTime <= comboData.CancelWindowEnd;

                if (inCancelWindow && _comboInputBuffered.HasValue)
                {
                    var input = _comboInputBuffered.Value;
                    _comboInputBuffered = null;
                    if (comboData.TryGetNextState(_currentComboState, input, out int nextState))
                    {
                        _currentComboState = nextState;
                        SetComboStateBlend(_currentComboState);
                        _animator.SetTrigger(_attackTriggerHash);
                        var clip = comboData.GetClipForState(_currentComboState);
                        _attackStateTimeout = clip != null ? clip.length + 0.2f : 1.5f;
                        int weaponIdx = GetWeaponIndexForMusic();
                        CombatMusicController.Instance?.OnAttackPerformed(input, _currentComboState, weaponIdx);
                        OnAttackPerformed?.Invoke(weaponIdx);
                    }
                }
            }

            if (_attackStateTimeout <= 0f)
            {
                _currentComboState = 0;
                _comboInputBuffered = null;
                SwitchState(AnimationState.Locomotion);
                return;
            }

            UpdateAnimatorController();
        }

        private void ExitAttackState()
        {
            _currentComboState = 0;
            _comboInputBuffered = null;
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
        ///     Applies root motion during Attack and Dodge.
        ///     Locomotion, Jump, Fall, Crouch use script-driven movement via Move() - no root motion here.
        /// </summary>
        private void OnAnimatorMove()
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
                deltaPosition.y += _velocity.y * Time.deltaTime;

                _controller.Move(deltaPosition);

                if (_applyRootRotationDuringDodge && !_dodgePreserveStrafeFacing
                    && _animator.deltaRotation != Quaternion.identity)
                    transform.rotation = transform.rotation * _animator.deltaRotation;
            }
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

            _animator.SetFloat(_isStrafingHash, UseStrafeStyleLocomotionFacing ? 1.0f : 0.0f);

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
            if (_inputReader._movementInputDetected)
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

            _moveDirection = (_cameraController.GetCameraForwardZeroedYNormalised() * _inputReader._moveComposite.y)
                + (_cameraController.GetCameraRightZeroedYNormalised() * _inputReader._moveComposite.x);
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
                _targetLockOnPos.position = _currentLockOnTarget.transform.position;
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

            _currentMaxSpeed = Mathf.Lerp(_currentMaxSpeed, _targetMaxSpeed, _ANIMATION_DAMP_TIME * Time.deltaTime);

            _targetVelocity.x = _moveDirection.x * _currentMaxSpeed;
            _targetVelocity.z = _moveDirection.z * _currentMaxSpeed;

            _velocity.z = Mathf.Lerp(_velocity.z, _targetVelocity.z, _speedChangeDamping * Time.deltaTime);
            _velocity.x = Mathf.Lerp(_velocity.x, _targetVelocity.x, _speedChangeDamping * Time.deltaTime);

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
            Quaternion strafingTargetRotation = Quaternion.LookRotation(_cameraForward);

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

                    transform.rotation = Quaternion.Slerp(transform.rotation, strafingTargetRotation, _rotationSmoothing * Time.deltaTime);
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

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(faceDirection),
                    _rotationSmoothing * Time.deltaTime
                );
            }
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
                    target.GetComponent<Synty.AnimationBaseLocomotion.Samples.SampleObjectLockOn>()?.Highlight(false, false);

                    float distance = Vector3.Distance(transform.position, target.transform.position);
                    float distanceScore = 1 / distance * 100;

                    Vector3 targetDirection = target.transform.position - _cameraController.GetCameraPosition();
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
                    _currentLockOnTarget.GetComponent<Synty.AnimationBaseLocomotion.Samples.SampleObjectLockOn>()?.Highlight(true, false);
                }
            }
            else
            {
                if (_currentTargetCandidates.Contains(_currentLockOnTarget))
                {
                    _currentLockOnTarget.GetComponent<Synty.AnimationBaseLocomotion.Samples.SampleObjectLockOn>()?.Highlight(true, true);
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

            _velocity = new Vector3(_velocity.x, _jumpForce, _velocity.z);
        }

        /// <summary>
        ///     updates the jump state.
        /// </summary>
        private void UpdateJumpState()
        {
            UpdateBestTarget();
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
                SwitchState(AnimationState.Locomotion);
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
