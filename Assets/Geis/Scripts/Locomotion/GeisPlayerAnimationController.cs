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
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerDefensiveCombatState))]
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

        #region Sub-controllers

        private readonly GeisInputBufferTracker _inputBuffers = new GeisInputBufferTracker();
        private readonly GeisDodgeRollController _dodgeController = new GeisDodgeRollController();
        private readonly GeisComboAttackController _comboController = new GeisComboAttackController();
        private readonly GeisBowAnimatorPresenter _bowPresenter = new GeisBowAnimatorPresenter();

        private const int COMBO_BLEND_SLOTS = GeisComboAttackController.DefaultBlendSlots;
        private const float AnimationDampTime = GeisLocomotionTuningDefaults.AnimationDampTime;
        private const float BowAimRootYawOffsetDegrees = 90f;

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

        #region Locomotion tuning (from profiles on Awake)

        private bool _alwaysStrafe;
        private float _walkSpeed;
        private float _runSpeed;
        private float _sprintSpeed;
        private float _speedChangeDamping;
        private float _accelRate;
        private float _decelRate;
        private float _sprintInstantFraction;
        private float _rotationSmoothing;
        private float _maxTurnDegPerSecond;
        private float _cameraRotationOffset;
        private Vector3 _bowAimBodyEulerOffset;

        #endregion

        #region Shuffle / strafe tuning

        private float _buttonHoldThreshold;
        private float _shuffleDirectionX;
        private float _shuffleDirectionZ;
        private float _capsuleStandingHeight;
        private float _capsuleStandingCentre;
        private float _capsuleCrouchingHeight;
        private float _capsuleCrouchingCentre;
        private float _forwardStrafeMinThreshold;
        private float _forwardStrafeMaxThreshold;
        private float _forwardStrafe;

        #endregion

        #region Grounding / air tuning

        private LayerMask _groundLayerMask;
        private float _inclineAngle;
        private float _groundedOffset;
        private float _jumpForce;
        private float _gravityMultiplier;
        private float _fallingDuration;
        private float _coyoteTimeSeconds;
        private float _jumpBufferSeconds;

        #endregion

        #region Look / lean tuning

        private bool _enableHeadTurn;
        private float _headLookDelay;
        private float _headLookX;
        private float _headLookY;
        private AnimationCurve _headLookXCurve;
        private float _headLookLimitDegrees;
        private float _bowAimHeadLookMultiplier;
        private bool _enableBodyTurn;
        private float _bodyLookDelay;
        private float _bodyLookX;
        private float _bodyLookY;
        private AnimationCurve _bodyLookXCurve;
        private float _bowAimBodyLookMultiplier;
        private bool _enableLean;
        private float _leanDelay;
        private float _leanValue;
        private AnimationCurve _leanCurve;
        private float _leansHeadLooksDelay;

        #endregion

        #region Scene references

        [Tooltip("Position of the rear ray for grounded angle check.")]
        [SerializeField]
        private Transform _rearRayPos;
        [Tooltip("Position of the front ray for grounded angle check.")]
        [SerializeField]
        private Transform _frontRayPos;

        #endregion

        #region Attack / combo assets

        /// <summary>
        /// Fired when an attack is triggered (first hit or combo continuation).
        /// Subscribe from GeisCombatBridge to apply RogueDeal damage/hit detection.
        /// </summary>
        public event Action<int> OnAttackPerformed
        {
            add => _comboController.AttackPerformed += value;
            remove => _comboController.AttackPerformed -= value;
        }

        /// <summary>
        /// Current data-driven combo step (0 = first hit). Aligns with GeisComboData clip index for combat/hit timing.
        /// </summary>
        public int CurrentComboState => _comboController.CurrentComboState;

        [Tooltip("Resolves combo clips and transitions from GeisWeaponDefinition.comboData on the active slot.")]
        [SerializeField]
        private GeisWeaponSwitcher _weaponSwitcher;
        [Tooltip("Optional: placeholders for runtime override. Loaded from Resources/GeisComboPlaceholders if null.")]
        [SerializeField]
        private GeisComboPlaceholders _comboPlaceholders;

        #endregion

        #region Combat / dodge tuning (from profiles on Awake)

        private bool _applyRootRotationDuringAttack;
        private bool _applyRootRotationDuringDodge;
        private float _bowEquipLayerBlendSpeed;
        private float _dodgeInputDeadzone;
        private float _dodgeFallbackDuration;
        private bool _requireMovementInputForDodge;
        private float _dodgeScriptedPlaneSpeed;
        private float _dodgeScriptedDuration;
        private float _attackMoveCancelStickThreshold;
        private float _attackRecoveryExitNormalizedTime;
        private float _attackExitVelocityCarry;
        private float _dodgeRecoveryStartNormalizedTime;
        private float _rollRecoveryStartNormalizedTime;
        private float _dodgeInvulnerabilityEndNormalizedTime;
        private float _rollInvulnerabilityEndNormalizedTime;
        private float _dodgeMoveCancelStickThreshold;
        private float _dodgeExitVelocityCarry;
        private float _rollExitVelocityCarry;
        private float _inputBufferSeconds;
        private float _dodgeDoubleTapWindow;
        private bool _dodgeDoubleTapRollEnabled;
        private float _rollDistanceMultiplier;
        private float _strafeStyleMaxPlanarSpeed;
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
        private bool _hasFallingBlendParameter;
        private bool _isCrouching;
        private bool _isGrounded = true;
        private int _ungroundedFrameCount;
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
        /// <summary>Seconds of coyote time remaining: counts up to <see cref="_coyoteTimeSeconds"/> while grounded, decrements in air.</summary>
        private float _coyoteTimer;
        private float _landingGroundGraceTimer;

        private const float LandingGroundGraceSeconds = 0.25f;
        private const float GroundedVerticalStickVelocity = -2f;
        private const int UngroundedFramesBeforeAirborne = 3;

        /// <summary>
        /// Animator fall/land transitions use <see cref="LocomotionAnimatorIds.IsGrounded"/>; keep true briefly after landing so physics flicker does not retrigger fall clips.
        /// </summary>
        private bool IsGroundedForAnimator => _isGrounded || _landingGroundGraceTimer > 0f;
        private bool _jumpAnimatorIsActive;

        private bool _useDataDrivenCombo;

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

        /// <summary>Max-speed blend rate; must stay in sync with <see cref="AnimationDampTime"/> (soul ghost motor).</summary>
        public float LocomotionMaxSpeedLerpRate => AnimationDampTime;

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
            ResetCombatTransientState(exitAttack: true, exitDodge: false, resetJumpVelocity: true);

            if (_currentState == AnimationState.Jump || _currentState == AnimationState.Fall)
                SwitchState(AnimationState.Locomotion);

            GroundedCheck();
            _velocity.y = _isGrounded ? GroundedVerticalStickVelocity : 0f;
        }

        /// <summary>Clears attack/dodge state when entering soul realm so spectral combat starts from a known baseline.</summary>
        public void ResetCombatStateForSoulRealmEntry()
        {
            ResetCombatTransientState(exitAttack: true, exitDodge: true, resetJumpVelocity: false);
        }

        /// <summary>Ends the current melee swing when damaged (hit reaction plays on the HitReaction layer).</summary>
        public void InterruptAttackFromIncomingHit()
        {
            ResetCombatTransientState(exitAttack: true, exitDodge: false, resetJumpVelocity: false);
        }

        private void ResetCombatTransientState(bool exitAttack, bool exitDodge, bool resetJumpVelocity)
        {
            _attackStateTimeout = 0f;
            _comboController.ResetComboState();
            _inputBuffers.ResetCombatBuffers();
            _dodgeController.ResetTransientState();

            if (resetJumpVelocity)
                _inputBuffers.ResetJumpBuffer();

            if (exitAttack && _currentState == AnimationState.Attack)
                SwitchState(AnimationState.Locomotion);

            if (exitDodge && _currentState == AnimationState.Dodge)
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
            phase = comboData.GetAttackPhase(_comboController.CurrentComboState, normalizedTime);
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
                    && comboData.HasSuperArmorDuringStartup(_comboController.CurrentComboState);
            }
        }

        bool IAttackerPhaseProvider.DodgeOnlyAvoidsDuringActivePhase
        {
            get
            {
                GeisComboData comboData = GetCurrentComboData();
                return comboData == null || comboData.DodgeOnlyAvoidsDuringActivePhase(_comboController.CurrentComboState);
            }
        }

        #endregion

        #region Base State Variables

        private const float StrafeDirectionDampTime = 20f;
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
                if (_dodgeController.AnimatorEnteredLeaf
                    && GeisDodgeRollController.IsDodgeLeafShortNameHash(info.shortNameHash)
                    && !PresentationAnimator.IsInTransition(0)
                    && info.length > 0.01f)
                {
                    float t = info.normalizedTime % 1f;
                    float invulnEnd = _dodgeController.IsRoll
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
            ApplyLocomotionTuningFromProfiles();
        }

        private void EnsureComponentReferences()
        {
            if (_inputReader == null)
                _inputReader = GetComponent<GeisInputReader>();
            if (_controller == null)
                _controller = GetComponent<CharacterController>();
            if (_weaponSwitcher == null)
                _weaponSwitcher = GetComponent<GeisWeaponSwitcher>();
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

            SubscribeInputEvents();

            _isStrafing = _alwaysStrafe;

            if (_controller != null)
                CapsuleCrouchingSize(_isCrouching);

            _comboController.Configure(_weaponSwitcher, _comboPlaceholders, _animator);
            _useDataDrivenCombo = _comboController.UseDataDrivenCombo;

            ApplyComboOverridesIfReady();

            if (_animator != null)
            {
                RefreshPresentationAnimatorCaches();
            }

            SwitchState(AnimationState.Locomotion);

            if (_weaponSwitcher != null)
                _weaponSwitcher.WeaponEquipped += HandleWeaponEquipped;
        }

        private void HandleWeaponEquipped(int slotIndex)
        {
            if (!IsBowEquipped)
            {
                PrepareAnimatorForNonBowWeapon(reevaluateAnimator: true);
                ApplyComboOverridesIfReady();
            }
            else
            {
                _bowPresenter.RefreshCaches(PresentationAnimator, true);
                ApplyBowParametersToAnimator();
            }
        }

        /// <summary>
        /// Clears bow aim/draw and snaps the Bow_Draw layer off. Call before showing a melee weapon.
        /// </summary>
        public void PrepareAnimatorForNonBowWeapon(bool reevaluateAnimator = true)
        {
            SetBowDrawState(false, 0f, false);

            if (_isAiming)
                DeactivateAim();

            _bowPresenter.RefreshCaches(PresentationAnimator, bowEquipped: false);
            ForceExitBowPresentationOnAnimator(PresentationAnimator, reevaluateAnimator);

            if (_animator != null && _animator != PresentationAnimator)
                ForceExitBowPresentationOnAnimator(_animator, reevaluateAnimator);

            SoulRealmManager mgr = SoulRealmManager.Instance;
            if (mgr != null
                && mgr.IsSoulRealmActive
                && mgr.SpectralAnimator != null
                && mgr.SpectralAnimator != PresentationAnimator
                && mgr.SpectralAnimator != _animator)
                ForceExitBowPresentationOnAnimator(mgr.SpectralAnimator, reevaluateAnimator);
        }

        private void ForceExitBowPresentationOnAnimator(Animator anim, bool reevaluateAnimator)
        {
            if (anim == null)
                return;

            _bowPresenter.ForceExitBowPresentation(anim, reevaluateAnimator);
        }

        private void SubscribeInputEvents()
        {
            if (_inputReader == null)
                return;

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
            _inputReader.onJumpPerformed += OnJumpInputBufferAndCoyote;
        }

        private void UnsubscribeInputEvents()
        {
            if (_inputReader == null)
                return;

            _inputReader.onLockOnToggled -= ToggleLockOn;
            _inputReader.onLockOnCycleLeft -= CycleLockOnLeft;
            _inputReader.onLockOnCycleRight -= CycleLockOnRight;
            _inputReader.onSprintActivated -= ActivateSprint;
            _inputReader.onSprintDeactivated -= DeactivateSprint;
            _inputReader.onCrouchActivated -= ActivateCrouch;
            _inputReader.onCrouchDeactivated -= DeactivateCrouch;
            _inputReader.onAimActivated -= ActivateAim;
            _inputReader.onAimDeactivated -= DeactivateAim;
            _inputReader.onLightAttackPerformed -= OnLightAttackRequested;
            _inputReader.onHeavyAttackPerformed -= OnHeavyAttackRequested;
            _inputReader.onDodgePerformed -= OnDodgeRequested;
            _inputReader.onJumpPerformed -= OnJumpInputBufferAndCoyote;
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
            if (_weaponSwitcher != null)
                _weaponSwitcher.WeaponEquipped -= HandleWeaponEquipped;
            UnsubscribeInputEvents();
            _comboController.DestroyOverrideController();

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
                _inputBuffers.ResetJumpBuffer();
                return;
            }

            _inputBuffers.BufferJump(Time.unscaledTime);

            if (_currentState == AnimationState.Fall && _coyoteTimer > 0f)
            {
                _coyoteTimer = 0f;
                _inputBuffers.ResetJumpBuffer();
                SwitchState(AnimationState.Jump);
            }
        }

        /// <summary>True if a jump press was buffered within <see cref="_jumpBufferSeconds"/>.</summary>
        private bool IsJumpBufferFresh() => _inputBuffers.IsJumpBufferFresh(Time.unscaledTime);

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

            _hasFallingBlendParameter = AnimatorParameterGuard.HasParameter(anim, "FallingBlend");
            _bowPresenter.RefreshCaches(anim, IsBowEquipped);
        }

    }
}
