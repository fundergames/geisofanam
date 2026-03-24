using System.Collections;
using UnityEngine;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Targeting;
using RogueDeal.Combat.Targeting;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Third-person combat controller based on Synty SamplePlayerAnimationController locomotion,
    /// with combat (dash, attacks, lock-on) layered on top.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CombatExecutor))]
    [RequireComponent(typeof(CombatEntity))]
    public class ThirdPersonCombatController : MonoBehaviour
    {
        #region State Enums

        private enum AnimationState
        {
            Base,
            Locomotion,
            Jump,
            Fall,
            Crouch,
            Dash,
            Attack
        }

        private enum GaitState
        {
            Idle,
            Walk,
            Run,
            Sprint
        }

        #endregion

        #region Animation Hashes

        private readonly int _movementInputTappedHash = Animator.StringToHash("MovementInputTapped");
        private readonly int _movementInputPressedHash = Animator.StringToHash("MovementInputPressed");
        private readonly int _movementInputHeldHash = Animator.StringToHash("MovementInputHeld");
        private readonly int _moveSpeedHash = Animator.StringToHash("MoveSpeed");
        private readonly int _currentGaitHash = Animator.StringToHash("CurrentGait");
        private readonly int _strafeDirectionXHash = Animator.StringToHash("StrafeDirectionX");
        private readonly int _strafeDirectionZHash = Animator.StringToHash("StrafeDirectionZ");
        private readonly int _isStrafingHash = Animator.StringToHash("IsStrafing");
        private readonly int _isCrouchingHash = Animator.StringToHash("IsCrouching");
        private readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");
        private readonly int _isJumpingHash = Animator.StringToHash("IsJumping");
        private readonly int _speedHash = Animator.StringToHash("Speed");
        private readonly int _isWalkingHash = Animator.StringToHash("IsWalking");
        private readonly int _dashTriggerHash = Animator.StringToHash("Dash");
        private readonly int _takeActionHash = Animator.StringToHash("TakeAction");
        private readonly int _actionIndexHash = Animator.StringToHash("ActionIndex");
        private readonly int _actionTypeHash = Animator.StringToHash("ActionType");
        private readonly int _isActionHash = Animator.StringToHash("IsAction");
        private readonly int _attack1Hash = Animator.StringToHash("Attack_1");
        private readonly int _attack2Hash = Animator.StringToHash("Attack_2");
        private readonly int _attack3Hash = Animator.StringToHash("Attack_3");

        private readonly int _leanValueHash = Animator.StringToHash("LeanValue");
        private readonly int _headLookXHash = Animator.StringToHash("HeadLookX");
        private readonly int _headLookYHash = Animator.StringToHash("HeadLookY");
        private readonly int _bodyLookXHash = Animator.StringToHash("BodyLookX");
        private readonly int _bodyLookYHash = Animator.StringToHash("BodyLookY");
        private readonly int _inclineAngleHash = Animator.StringToHash("InclineAngle");
        private readonly int _shuffleDirectionXHash = Animator.StringToHash("ShuffleDirectionX");
        private readonly int _shuffleDirectionZHash = Animator.StringToHash("ShuffleDirectionZ");
        private readonly int _forwardStrafeHash = Animator.StringToHash("ForwardStrafe");
        private readonly int _cameraRotationOffsetHash = Animator.StringToHash("CameraRotationOffset");
        private readonly int _isTurningInPlaceHash = Animator.StringToHash("IsTurningInPlace");
        private readonly int _isStoppedHash = Animator.StringToHash("IsStopped");
        private readonly int _isStartingHash = Animator.StringToHash("IsStarting");
        private readonly int _locomotionStartDirectionHash = Animator.StringToHash("LocomotionStartDirection");
        private readonly int _fallingDurationHash = Animator.StringToHash("FallingDuration");

        private const float MovementInputHoldThreshold = 0.15f;
        private const float StrafeDirectionDampTime = 20f;
        private const float MovementRampTime = 0.12f;
        private const float AnimationDampTime = 5f;

        #endregion

        #region Serialized Fields

        [Header("Locomotion (Sample Style)")]
        [SerializeField] private float walkSpeed = 1.4f;
        [SerializeField] private float runSpeed = 2.5f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float speedChangeDamping = 10f;
        [SerializeField] private float rotationSmoothing = 10f;
        [SerializeField] private bool alwaysStrafe = true;
        [Tooltip("When true, disables strafe when sprinting (face forward). When false, strafe works at all speeds.")]
        [SerializeField] private bool disableStrafeWhenSprinting = false;

        [Header("Capsule (Crouch)")]
        [SerializeField] private float capsuleStandingHeight = 1.8f;
        [SerializeField] private float capsuleStandingCentre = 0.93f;
        [SerializeField] private float capsuleCrouchingHeight = 1.2f;
        [SerializeField] private float capsuleCrouchingCentre = 0.6f;

        [Header("Jump / Gravity")]
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundedOffset = -0.14f;
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Header("Grounded (Incline)")]
        [Tooltip("Position of the rear ray for incline check. Leave null to skip incline.")]
        [SerializeField] private Transform rearRayPos;
        [Tooltip("Position of the front ray for incline check. Leave null to skip incline.")]
        [SerializeField] private Transform frontRayPos;

        [Header("Strafing")]
        [SerializeField] private float forwardStrafeMinThreshold = -55f;
        [SerializeField] private float forwardStrafeMaxThreshold = 125f;

        [Header("Head Look")]
        [SerializeField] private bool enableHeadTurn = true;
        [SerializeField] private AnimationCurve headLookXCurve;

        [Header("Body Look")]
        [SerializeField] private bool enableBodyTurn = true;
        [SerializeField] private AnimationCurve bodyLookXCurve;

        [Header("Lean")]
        [SerializeField] private bool enableLean = true;
        [SerializeField] private AnimationCurve leanCurve;

        [Header("Combat")]
        [SerializeField] private float dashDuration = 0.3f;
        [SerializeField] private float manualDashSpeed = 15f;
        [SerializeField] private bool useWeaponColliders = true;
        [SerializeField] private CombatAction[] combatActions;
        [SerializeField] private int actionStateCount = 2;
        [SerializeField] private int actionIndexOffset = 0;

        [Header("References")]
        [SerializeField] private CombatInputReader inputProvider;
        [SerializeField] private CombatCameraController cameraController;
        [SerializeField] private RuntimeAnimatorController defaultAnimatorController;

        #endregion

        #region Component Refs

        private CharacterController _controller;
        private Animator _animator;
        private CombatExecutor _combatExecutor;
        private CombatEntity _combatEntity;
        private Camera _mainCamera;
        private TargetingManager _targetingManager;
        private LockOnIndicator _lockOnIndicator;
        private ICombatInputProvider _inputProvider;

        #endregion

        #region Runtime State

        private AnimationState _currentState = AnimationState.Locomotion;
        private Vector3 _velocity;
        private Vector3 _moveDirection;
        private Vector3 _targetVelocity;
        private bool _isGrounded = true;
        private bool _isCrouching;
        private bool _isSprinting;
        private bool _isStrafing;
        private bool _isLockedOn;
        private bool _isWalking;
        private float _speed2D;
        private GaitState _currentGait;
        private float _strafeDirectionX;
        private float _strafeDirectionZ;
        private float _currentMaxSpeed;
        private float _targetMaxSpeed;
        private float _movementInputDuration;
        private float _timeSinceMoveStart;

        // Combat
        private float _dashTimer;
        private float _attackStateTimeout;
        private Vector3 _dashDirection;
        private CombatAction[] _availableActions;
        private int _currentComboIndex;

        private bool _usePolygonParams;
        private bool _useLegacyAttackTriggers;
        private bool _useSyntyMovementParams;

        private bool _movementInputTapped;
        private bool _movementInputPressed;
        private bool _movementInputHeld;

        // Incline, head/body look, lean
        private float _inclineAngle;
        private float _headLookX;
        private float _headLookY;
        private float _bodyLookX;
        private float _bodyLookY;
        private float _leanValue;
        private float _cameraRotationOffset;
        private float _forwardStrafe = 1f;
        private float _shuffleDirectionX;
        private float _shuffleDirectionZ;
        private float _strafeAngle;
        private float _newDirectionDifferenceAngle;
        private float _locomotionStartDirection;
        private float _locomotionStartTimer;
        private float _headLookDelay;
        private float _bodyLookDelay;
        private float _leanDelay;
        private float _rotationRate;
        private float _fallStartTime;
        private float _fallingDuration;
        private bool _isStarting;
        private bool _isStopped = true;
        private bool _isTurningInPlace;
        private bool _cannotStandUp;
        private bool _enableHeadTurn = true;
        private bool _enableBodyTurn = true;
        private bool _enableLean = true;
        private Vector3 _currentRotation;
        private Vector3 _previousRotation;
        private float _initialLeanValue;
        private float _initialTurnValue;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            _combatEntity = GetComponent<CombatEntity>();
            if (_animator == null && _combatEntity != null)
                _animator = _combatEntity.animator;
            _combatExecutor = GetComponent<CombatExecutor>();
            _mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();

            _targetingManager = GetComponent<TargetingManager>();
            if (_targetingManager == null)
                _targetingManager = gameObject.AddComponent<TargetingManager>();

            _lockOnIndicator = GetComponentInChildren<LockOnIndicator>();
            if (_lockOnIndicator == null)
            {
                var go = new GameObject("LockOnIndicator");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                _lockOnIndicator = go.AddComponent<LockOnIndicator>();
            }

            if (cameraController == null)
                cameraController = FindFirstObjectByType<CombatCameraController>();

            _inputProvider = inputProvider as ICombatInputProvider ?? inputProvider?.GetComponent<ICombatInputProvider>() ?? FindFirstObjectByType<CombatInputReader>();
            _availableActions = combatActions != null && combatActions.Length > 0 ? combatActions : new CombatAction[0];

            if (_animator != null)
            {
                _animator.applyRootMotion = true;
                _usePolygonParams = HasParameter("MoveSpeed");
                _useLegacyAttackTriggers = HasParameter("Attack_1") && HasParameter("Attack_2");
                _useSyntyMovementParams = HasParameter("MovementInputHeld");
                if (_animator.runtimeAnimatorController == null && defaultAnimatorController != null)
                    _animator.runtimeAnimatorController = defaultAnimatorController;
            }

            _isStrafing = alwaysStrafe;
            _previousRotation = transform.forward;

            if (headLookXCurve == null)
                headLookXCurve = CreateDefaultHeadLookCurve();
            if (bodyLookXCurve == null)
                bodyLookXCurve = CreateDefaultBodyLookCurve();
            if (leanCurve == null)
                leanCurve = CreateDefaultLeanCurve();
        }

        private static AnimationCurve CreateDefaultHeadLookCurve()
        {
            return new AnimationCurve(
                new Keyframe(-1f, -0.3f),
                new Keyframe(0f, 0f),
                new Keyframe(1f, 0.3f)
            );
        }

        private static AnimationCurve CreateDefaultBodyLookCurve()
        {
            return new AnimationCurve(
                new Keyframe(-1f, -1f),
                new Keyframe(0f, 0f),
                new Keyframe(1f, 1f)
            );
        }

        private static AnimationCurve CreateDefaultLeanCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.1f, 0.3f),
                new Keyframe(0.5f, 0.65f),
                new Keyframe(1f, 1f)
            );
        }

        private void Update()
        {
            _isLockedOn = _targetingManager != null && _targetingManager.IsLockedOn();
            _isStrafing = alwaysStrafe || _isLockedOn;
            if (disableStrafeWhenSprinting && _isSprinting) _isStrafing = false;

            HandleLockOnInput();
            GroundedCheck();

            var state = _inputProvider?.GetState() ?? default;
            if (state.CrouchPressed && _isGrounded)
            {
                if (_isCrouching && _cannotStandUp)
                    _isCrouching = true; // Can't stand - ceiling too low
                else
                {
                    _isCrouching = !_isCrouching;
                    SetCapsuleCrouch(_isCrouching);
                }
            }
            _isSprinting = state.SprintHeld && !_isCrouching;
            _isWalking = _currentGait == GaitState.Walk && !_isSprinting && !_isStrafing;

            // Combat interrupts: Dash and Attack
            if (state.DashPressed && _isGrounded && !_isCrouching && _currentState != AnimationState.Attack)
            {
                SwitchState(AnimationState.Dash);
                return;
            }
            if (state.AttackPressed && _currentState != AnimationState.Attack && _currentState != AnimationState.Dash)
            {
                StartAttack();
                return;
            }
            if (state.AttackPressed && state.HasAttackClickPosition && _targetingManager != null)
                _targetingManager.HandleMouseClick(state.AttackClickScreenPosition);

            switch (_currentState)
            {
                case AnimationState.Locomotion:
                    UpdateLocomotionState(state);
                    break;
                case AnimationState.Jump:
                    UpdateJumpState(state);
                    break;
                case AnimationState.Fall:
                    UpdateFallState(state);
                    break;
                case AnimationState.Crouch:
                    UpdateCrouchState(state);
                    break;
                case AnimationState.Dash:
                    UpdateDashState();
                    break;
                case AnimationState.Attack:
                    UpdateAttackState(state);
                    break;
            }

            UpdateLockOnIndicator();
        }

        #endregion

        #region Lock-on

        private void HandleLockOnInput()
        {
            if (_inputProvider == null || _targetingManager == null || cameraController == null) return;
            var state = _inputProvider.GetState();
            if (state.LockOnPressed)
            {
                _targetingManager.ToggleLockOn();
                cameraController.LockOn(_targetingManager.IsLockedOn(), _targetingManager.GetLockOnTargetTransform());
            }
        }

        private void UpdateLockOnIndicator()
        {
            if (_lockOnIndicator == null || _targetingManager == null) return;
            var locked = _targetingManager.GetLockedOnTarget();
            if (locked != null)
                _lockOnIndicator.SetTarget(locked, true);
            else
                _lockOnIndicator.ClearTarget();
        }

        #endregion

        #region Base Logic (from Sample)

        private void GroundedCheck()
        {
            var spherePos = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
            _isGrounded = Physics.CheckSphere(spherePos, _controller.radius, groundLayerMask, QueryTriggerInteraction.Ignore);

            if (_isGrounded && rearRayPos != null && frontRayPos != null)
                GroundInclineCheck();
        }

        private void GroundInclineCheck()
        {
            float rayDistance = 5f;
            Vector3 downDir = Vector3.down;
            bool rearHit = Physics.Raycast(rearRayPos.position, downDir, out RaycastHit rearHitData, rayDistance, groundLayerMask);
            bool frontHit = Physics.Raycast(frontRayPos.position, downDir, out RaycastHit frontHitData, rayDistance, groundLayerMask);

            if (rearHit && frontHit)
            {
                Vector3 hitDifference = frontHitData.point - rearHitData.point;
                float xPlaneLength = new Vector2(hitDifference.x, hitDifference.z).magnitude;
                float angle = Mathf.Atan2(hitDifference.y, Mathf.Max(xPlaneLength, 0.001f)) * Mathf.Rad2Deg;
                _inclineAngle = Mathf.Lerp(_inclineAngle, angle, 20f * Time.deltaTime);
            }
        }

        private void CeilingHeightCheck()
        {
            if (frontRayPos == null) { _cannotStandUp = false; return; }
            float rayDistance = Mathf.Infinity;
            float minimumStandingHeight = capsuleStandingHeight - frontRayPos.localPosition.y;
            Vector3 midpoint = new Vector3(transform.position.x, transform.position.y + frontRayPos.localPosition.y, transform.position.z);
            if (Physics.Raycast(midpoint, transform.TransformDirection(Vector3.up), out RaycastHit ceilingHit, rayDistance, groundLayerMask))
                _cannotStandUp = ceilingHit.distance < minimumStandingHeight;
            else
                _cannotStandUp = false;
        }

        private void SetCapsuleCrouch(bool crouching)
        {
            _controller.height = crouching ? capsuleCrouchingHeight : capsuleStandingHeight;
            _controller.center = new Vector3(0, crouching ? capsuleCrouchingCentre : capsuleStandingCentre, 0);
        }

        private void CalculateInput(CombatInputState state)
        {
            _moveDirection = GetCameraForwardZeroedYNormalized() * state.Move.y + GetCameraRightZeroedYNormalized() * state.Move.x;
            bool hasInput = _moveDirection.sqrMagnitude > 0.0001f;

            if (hasInput)
            {
                _movementInputDuration += Time.deltaTime;
                _timeSinceMoveStart += Time.deltaTime;

                float effectiveDuration = _movementInputDuration;
                if (_isStrafing)
                    effectiveDuration = Mathf.Max(effectiveDuration, MovementInputHoldThreshold);

                if (effectiveDuration <= 0f)
                    _movementInputTapped = true;
                else if (effectiveDuration > 0f && effectiveDuration < MovementInputHoldThreshold)
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
            }
            else
            {
                _movementInputTapped = false;
                _movementInputPressed = false;
                _movementInputHeld = false;
                _movementInputDuration = 0f;
                _timeSinceMoveStart = 0f;
            }
        }

        private void CalculateMoveDirection(CombatInputState state)
        {
            CalculateInput(state);

            if (_isCrouching)
                _targetMaxSpeed = walkSpeed;
            else if (_isSprinting)
                _targetMaxSpeed = sprintSpeed;
            else if (_isWalking)
                _targetMaxSpeed = walkSpeed;
            else
                _targetMaxSpeed = state.Run || state.SprintHeld ? runSpeed : walkSpeed;

            _currentMaxSpeed = Mathf.Lerp(_currentMaxSpeed, _targetMaxSpeed, speedChangeDamping * Time.deltaTime);

            // Movement ramp when starting from idle (Sample-style, prevents sliding)
            bool hasMoveInput = _moveDirection.sqrMagnitude > 0.0001f;
            float velocityScale = hasMoveInput ? Mathf.Clamp01(_timeSinceMoveStart / MovementRampTime) : 1f;
            float effectiveSpeed = hasMoveInput ? _currentMaxSpeed * velocityScale : 0f;

            _targetVelocity.x = _moveDirection.normalized.x * effectiveSpeed;
            _targetVelocity.z = _moveDirection.normalized.z * effectiveSpeed;

            _velocity.x = Mathf.Lerp(_velocity.x, _targetVelocity.x, speedChangeDamping * Time.deltaTime);
            _velocity.z = Mathf.Lerp(_velocity.z, _targetVelocity.z, speedChangeDamping * Time.deltaTime);

            _speed2D = new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
            _speed2D = Mathf.Round(_speed2D * 1000f) / 1000f;

            Vector3 playerForwardVector = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            Vector3 moveDirFlat = new Vector3(_moveDirection.x, 0f, _moveDirection.z).normalized;
            _newDirectionDifferenceAngle = playerForwardVector.sqrMagnitude > 0.001f && moveDirFlat.sqrMagnitude > 0.001f
                ? Vector3.SignedAngle(playerForwardVector, moveDirFlat, Vector3.up)
                : 0f;

            CalculateGait();
        }

        private void CalculateGait()
        {
            float runThreshold = (walkSpeed + runSpeed) / 2f;
            float sprintThreshold = (runSpeed + sprintSpeed) / 2f;

            if (_speed2D < 0.01f)
                _currentGait = GaitState.Idle;
            else if (_speed2D < runThreshold)
                _currentGait = GaitState.Walk;
            else if (_speed2D < sprintThreshold)
                _currentGait = GaitState.Run;
            else
                _currentGait = GaitState.Sprint;
        }

        private void FaceMoveDirection()
        {
            Vector3 charFwd = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            Vector3 charRight = new Vector3(transform.right.x, 0f, transform.right.z).normalized;
            Vector3 dirFwd = _moveDirection.magnitude > 0.01f ? new Vector3(_moveDirection.x, 0f, _moveDirection.z).normalized : charFwd;
            Vector3 camFwd = GetCameraForwardZeroedYNormalized();
            Quaternion strafingTargetRotation = camFwd.sqrMagnitude > 0.001f ? Quaternion.LookRotation(camFwd) : transform.rotation;

            _strafeAngle = charFwd.sqrMagnitude > 0.001f && dirFwd.sqrMagnitude > 0.001f
                ? Vector3.SignedAngle(charFwd, dirFwd, Vector3.up)
                : 0f;
            _isTurningInPlace = false;

            if (_isStrafing)
            {
                if (_moveDirection.magnitude > 0.01f)
                {
                    if (camFwd.sqrMagnitude > 0.001f)
                    {
                        _shuffleDirectionZ = Vector3.Dot(charFwd, dirFwd);
                        _shuffleDirectionX = Vector3.Dot(charRight, dirFwd);
                        UpdateStrafeDirection(Vector3.Dot(charFwd, dirFwd), Vector3.Dot(charRight, dirFwd));
                        _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, 0f, rotationSmoothing * Time.deltaTime);

                        float targetValue = _strafeAngle > forwardStrafeMinThreshold && _strafeAngle < forwardStrafeMaxThreshold ? 1f : 0f;
                        if (Mathf.Abs(_forwardStrafe - targetValue) <= 0.001f)
                            _forwardStrafe = targetValue;
                        else
                            _forwardStrafe = Mathf.SmoothStep(_forwardStrafe, targetValue, Mathf.Clamp01(StrafeDirectionDampTime * Time.deltaTime));
                    }

                    transform.rotation = Quaternion.Slerp(transform.rotation, strafingTargetRotation, rotationSmoothing * Time.deltaTime);
                }
                else
                {
                    UpdateStrafeDirection(1f, 0f);
                    float newOffset = charFwd.sqrMagnitude > 0.001f && camFwd.sqrMagnitude > 0.001f
                        ? Vector3.SignedAngle(charFwd, camFwd, Vector3.up)
                        : 0f;
                    _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, newOffset, 20f * Time.deltaTime);
                    _isTurningInPlace = Mathf.Abs(_cameraRotationOffset) > 10f;
                }
            }
            else
            {
                UpdateStrafeDirection(1f, 0f);
                _shuffleDirectionZ = 1f;
                _shuffleDirectionX = 0f;
                _cameraRotationOffset = Mathf.Lerp(_cameraRotationOffset, 0f, rotationSmoothing * Time.deltaTime);

                Vector3 faceDirection = new Vector3(_velocity.x, 0f, _velocity.z);
                if (faceDirection.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(faceDirection), rotationSmoothing * Time.deltaTime);
            }
        }

        private void UpdateStrafeDirection(float targetZ, float targetX)
        {
            _strafeDirectionZ = Mathf.Lerp(_strafeDirectionZ, targetZ, AnimationDampTime * Time.deltaTime);
            _strafeDirectionX = Mathf.Lerp(_strafeDirectionX, targetX, AnimationDampTime * Time.deltaTime);
            _strafeDirectionZ = Mathf.Round(_strafeDirectionZ * 1000f) / 1000f;
            _strafeDirectionX = Mathf.Round(_strafeDirectionX * 1000f) / 1000f;
        }

        private void CheckIfStopped()
        {
            _isStopped = _moveDirection.magnitude < 0.01f && _speed2D < 0.5f;
        }

        private void CheckIfStarting()
        {
            _locomotionStartTimer = VariableOverrideDelayTimer(_locomotionStartTimer);
            bool isStartingCheck = false;

            if (_locomotionStartTimer <= 0f)
            {
                if (_moveDirection.magnitude > 0.01f && _speed2D < 1f && !_isStrafing)
                    isStartingCheck = true;

                if (isStartingCheck)
                {
                    if (!_isStarting)
                    {
                        _locomotionStartDirection = _newDirectionDifferenceAngle;
                        if (_animator != null && HasParameter("LocomotionStartDirection"))
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
        }

        private float VariableOverrideDelayTimer(float timeVariable)
        {
            if (timeVariable > 0f)
            {
                timeVariable -= Time.deltaTime;
                timeVariable = Mathf.Clamp(timeVariable, 0f, 1f);
            }
            else
            {
                timeVariable = 0f;
            }
            return timeVariable;
        }

        private void CheckEnableTurns()
        {
            _headLookDelay = VariableOverrideDelayTimer(_headLookDelay);
            _bodyLookDelay = VariableOverrideDelayTimer(_bodyLookDelay);
            _enableHeadTurn = enableHeadTurn && _headLookDelay == 0f && !_isStarting;
            _enableBodyTurn = enableBodyTurn && _bodyLookDelay == 0f && !(_isStarting || _isTurningInPlace);
        }

        private void CheckEnableLean()
        {
            _leanDelay = VariableOverrideDelayTimer(_leanDelay);
            _enableLean = enableLean && _leanDelay == 0f && !(_isStarting || _isTurningInPlace);
        }

        private void CalculateRotationalAdditives(bool leansActivated, bool headLookActivated, bool bodyLookActivated)
        {
            bool anyActive = headLookActivated || leansActivated || bodyLookActivated;
            if (anyActive)
            {
                _currentRotation = transform.forward;
                _rotationRate = _currentRotation.sqrMagnitude > 0.001f && _previousRotation.sqrMagnitude > 0.001f
                    ? Vector3.SignedAngle(_currentRotation, _previousRotation, Vector3.up) / Mathf.Max(Time.deltaTime, 0.001f) * -1f
                    : 0f;
            }

            const float maxLeanRotationRate = 275f;
            float referenceValue = _speed2D / sprintSpeed;

            if (leansActivated)
            {
                _initialLeanValue = _rotationRate;
                _leanValue = CalculateSmoothedValue(_leanValue, _initialLeanValue, maxLeanRotationRate, 5f, leanCurve, referenceValue, true);
            }
            else
            {
                _leanValue = CalculateSmoothedValue(_leanValue, 0f, maxLeanRotationRate, 5f, leanCurve, referenceValue, true);
            }

            if (headLookActivated && _isTurningInPlace)
            {
                _initialTurnValue = _cameraRotationOffset;
                _headLookX = Mathf.Lerp(_headLookX, _initialTurnValue / 200f, 5f * Time.deltaTime);
            }
            else if (headLookActivated)
            {
                _initialTurnValue = _rotationRate;
                _headLookX = CalculateSmoothedValue(_headLookX, _initialTurnValue, maxLeanRotationRate, 5f, headLookXCurve, _headLookX, false);
            }
            else
            {
                _headLookX = CalculateSmoothedValue(_headLookX, 0f, maxLeanRotationRate, 5f, headLookXCurve, _headLookX, false);
            }

            if (bodyLookActivated)
            {
                _initialTurnValue = _rotationRate;
                _bodyLookX = CalculateSmoothedValue(_bodyLookX, _initialTurnValue, maxLeanRotationRate, 5f, bodyLookXCurve, _bodyLookX, false);
            }
            else
            {
                _bodyLookX = CalculateSmoothedValue(_bodyLookX, 0f, maxLeanRotationRate, 5f, bodyLookXCurve, _bodyLookX, false);
            }

            float cameraTilt = cameraController != null ? cameraController.GetCameraTiltX() : 0f;
            cameraTilt = (cameraTilt > 180f ? cameraTilt - 360f : cameraTilt) / -180f;
            cameraTilt = Mathf.Clamp(cameraTilt, -0.1f, 1f);
            _headLookY = cameraTilt;
            _bodyLookY = cameraTilt;

            _previousRotation = _currentRotation;
        }

        private float CalculateSmoothedValue(float mainVariable, float newValue, float maxRateChange, float smoothness,
            AnimationCurve referenceCurve, float referenceValue, bool isMultiplier)
        {
            if (referenceCurve == null) return mainVariable;

            float changeVariable = newValue / maxRateChange;
            changeVariable = Mathf.Clamp(changeVariable, -1f, 1f);

            if (isMultiplier)
                changeVariable *= referenceCurve.Evaluate(referenceValue);
            else
                changeVariable = referenceCurve.Evaluate(changeVariable);

            if (Mathf.Abs(changeVariable - mainVariable) > 0.0001f)
                changeVariable = Mathf.Lerp(mainVariable, changeVariable, smoothness * Time.deltaTime);

            return changeVariable;
        }

        private void ApplyGravity()
        {
            if (_isGrounded && _velocity.y < 0)
                _velocity.y = -2f;
            _velocity.y += gravity * Time.deltaTime;
        }

        private void Move()
        {
            if (_controller != null && _controller.enabled)
                _controller.Move(_velocity * Time.deltaTime);
        }

        private Vector3 GetCameraForwardZeroedYNormalized()
        {
            if (cameraController != null)
                return cameraController.GetCameraForwardZeroedYNormalised();
            return _mainCamera != null ? Vector3.ProjectOnPlane(_mainCamera.transform.forward, Vector3.up).normalized : transform.forward;
        }

        private Vector3 GetCameraRightZeroedYNormalized()
        {
            if (cameraController != null)
                return cameraController.GetCameraRightZeroedYNormalised();
            return _mainCamera != null ? Vector3.ProjectOnPlane(_mainCamera.transform.right, Vector3.up).normalized : transform.right;
        }

        #endregion

        #region Locomotion State

        private void UpdateLocomotionState(CombatInputState state)
        {
            ApplyGravity();

            if (_isCrouching)
            {
                _currentState = AnimationState.Crouch;
                return;
            }

            if (state.JumpPressed && _isGrounded)
            {
                SwitchState(AnimationState.Jump);
                return;
            }
            if (HasParameter("IsJumping") && _animator != null)
                _animator.SetBool(_isJumpingHash, false);

            CheckEnableTurns();
            CheckEnableLean();
            CalculateRotationalAdditives(_enableLean, _enableHeadTurn, _enableBodyTurn);

            CalculateMoveDirection(state);
            CheckIfStarting();
            CheckIfStopped();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        #endregion

        #region Jump State

        private void SwitchToJumpState()
        {
            _currentState = AnimationState.Jump;
            if (_animator != null && HasParameter("IsJumping"))
                _animator.SetBool(_isJumpingHash, true);
            _velocity.y = jumpForce;
        }

        private void UpdateJumpState(CombatInputState state)
        {
            ApplyGravity();

            CheckEnableTurns();
            CalculateRotationalAdditives(false, _enableHeadTurn, _enableBodyTurn);

            if (_velocity.y <= 0f)
            {
                if (_animator != null && HasParameter("IsJumping"))
                    _animator.SetBool(_isJumpingHash, false);
                SwitchState(AnimationState.Fall);
                return;
            }

            CalculateMoveDirection(state);
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        #endregion

        #region Fall State

        private void SwitchState(AnimationState newState)
        {
            _currentState = newState;
            if (newState == AnimationState.Fall)
            {
                _velocity.y = 0f;
                _fallStartTime = Time.time;
                _fallingDuration = 0f;
            }
            else if (newState == AnimationState.Dash)
                EnterDashState();
        }

        private void SwitchToLocomotionFromFall()
        {
            _currentState = AnimationState.Locomotion;
        }

        private void UpdateFallState(CombatInputState state)
        {
            ApplyGravity();
            _fallingDuration = Time.time - _fallStartTime;

            CheckEnableTurns();
            CalculateRotationalAdditives(false, _enableHeadTurn, _enableBodyTurn);

            CalculateMoveDirection(state);
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();

            if (_controller != null && _controller.isGrounded)
                SwitchToLocomotionFromFall();
        }

        #endregion

        #region Crouch State

        private void UpdateCrouchState(CombatInputState state)
        {
            GroundedCheck();
            if (!_isGrounded)
            {
                SetCapsuleCrouch(false);
                _isCrouching = false;
                SwitchState(AnimationState.Fall);
                return;
            }

            CeilingHeightCheck();

            if (state.JumpPressed && !_cannotStandUp)
            {
                SetCapsuleCrouch(false);
                _isCrouching = false;
                SwitchToJumpState();
                return;
            }

            if (!_isCrouching && !_cannotStandUp)
            {
                SetCapsuleCrouch(false);
                _currentState = AnimationState.Locomotion;
                return;
            }

            CheckEnableTurns();
            CheckEnableLean();
            CalculateRotationalAdditives(false, _enableHeadTurn, false);

            ApplyGravity();
            CalculateMoveDirection(state);
            CheckIfStarting();
            CheckIfStopped();
            FaceMoveDirection();
            Move();
            UpdateAnimatorController();
        }

        #endregion

        #region Dash State

        private void EnterDashState()
        {
            _dashTimer = dashDuration;
            var state = _inputProvider?.GetState() ?? default;
            Vector2 moveInput = state.Move;
            if (moveInput.sqrMagnitude > 0.01f)
            {
                _dashDirection = (GetCameraForwardZeroedYNormalized() * moveInput.y + GetCameraRightZeroedYNormalized() * moveInput.x).normalized;
            }
            else
            {
                _dashDirection = transform.forward;
            }
            transform.rotation = Quaternion.LookRotation(_dashDirection);
            if (_animator != null && HasParameter("Dash"))
                _animator.SetTrigger(_dashTriggerHash);
        }

        private void UpdateDashState()
        {
            ApplyGravity();
            _dashTimer -= Time.deltaTime;

            if (_dashTimer <= 0)
            {
                _currentState = AnimationState.Locomotion;
                return;
            }

            if (_controller != null && _controller.enabled)
                _controller.Move((_dashDirection * manualDashSpeed + _velocity) * Time.deltaTime);

            UpdateAnimatorController();
        }

        #endregion

        #region Attack State

        private void StartAttack()
        {
            int actionCount = Mathf.Max(_availableActions?.Length ?? actionStateCount, 1);
            CombatAction actionToUse = _availableActions != null && _availableActions.Length > 0 ? _availableActions[_currentComboIndex % _availableActions.Length] : null;

            if (actionToUse != null && _combatExecutor != null)
            {
                var cm = _combatExecutor.GetCooldownManager();
                if (cm != null && !cm.IsActionAvailable(actionToUse))
                    return;
            }

            _currentState = AnimationState.Attack;
            _attackStateTimeout = 5f;

            CombatEntity target = null;
            if (_targetingManager != null && actionToUse != null)
            {
                var tr = _targetingManager.GetTargets(actionToUse);
                if (tr != null && tr.isReady && tr.targets != null && tr.targets.Count > 0)
                {
                    target = tr.targets[0];
                    Vector3 toTarget = target.transform.position - transform.position;
                    toTarget.y = 0;
                    if (toTarget.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.LookRotation(toTarget.normalized);
                }
            }

            int actionIndex = (_currentComboIndex % actionCount) + actionIndexOffset;

            if (_animator != null && IsAnimatorValid())
            {
                int weaponTypeInt = actionToUse != null ? (int)actionToUse.weaponType : 0;
                if (HasParameter("ActionType")) _animator.SetInteger(_actionTypeHash, weaponTypeInt);

                if (_useLegacyAttackTriggers)
                {
                    int n = (_currentComboIndex % actionCount) + 1;
                    if (actionToUse != null && actionToUse.weaponType == WeaponType.Bow)
                    {
                        if (n == 1 && HasParameter("Attack_1")) _animator.SetTrigger(_attack1Hash);
                        else if (HasParameter("Attack_2")) _animator.SetTrigger(_attack2Hash);
                    }
                    else
                    {
                        if (n == 1 && HasParameter("Attack_1")) _animator.SetTrigger(_attack1Hash);
                        else if (n == 2 && HasParameter("Attack_2")) _animator.SetTrigger(_attack2Hash);
                        else if (n >= 3 && HasParameter("Attack_3")) _animator.SetTrigger(_attack3Hash);
                    }
                }
                else
                {
                    if (HasParameter("ActionIndex")) _animator.SetInteger(_actionIndexHash, actionIndex);
                    if (HasParameter("IsAction")) _animator.SetBool(_isActionHash, true);
                    if (HasParameter("TakeAction")) _animator.SetTrigger(_takeActionHash);
                }
                StartCoroutine(UpdateAttackTimeoutFromAnimation());
            }

            if (actionToUse != null && _combatExecutor != null)
            {
                if (useWeaponColliders)
                {
                    _combatExecutor.SetCurrentAction(actionToUse);
                }
                else
                {
                    var simpleDetector = GetComponent<SimpleAttackHitDetector>();
                    if (simpleDetector != null)
                        simpleDetector.PerformHitCheck(actionToUse);
                    else
                        _combatExecutor.ExecuteAction(actionToUse);
                }
            }

            _currentComboIndex = (_currentComboIndex + 1) % Mathf.Max(actionCount, 1);
        }

        private void UpdateAttackState(CombatInputState state)
        {
            ApplyGravity();

            if (_controller != null && _controller.enabled)
                _controller.Move(_velocity * Time.deltaTime);

            _attackStateTimeout -= Time.deltaTime;
            if (_attackStateTimeout <= 0)
                ResetAttackState();
            else if (!_useLegacyAttackTriggers && _animator != null && HasParameter("IsAction") && !_animator.GetBool(_isActionHash))
                ResetAttackState();

            UpdateAnimatorController();
        }

        private void ResetAttackState()
        {
            _currentState = AnimationState.Locomotion;
            if (useWeaponColliders && _combatExecutor != null)
                _combatExecutor.ClearCurrentAction();
            if (_animator != null && HasParameter("IsAction"))
                _animator.SetBool(_isActionHash, false);
        }

        private IEnumerator UpdateAttackTimeoutFromAnimation()
        {
            yield return null;
            if (_currentState == AnimationState.Attack && _animator != null)
            {
                var clipInfo = _animator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo != null && clipInfo.Length > 0)
                    _attackStateTimeout = clipInfo[0].clip.length + 0.2f;
            }
        }

        public void OnAttackEnd() => ResetAttackState();
        public void OnDashEnd() => _currentState = AnimationState.Locomotion;

        #endregion

        #region Animator Controller (Sample-style params)

        private void UpdateAnimatorController()
        {
            if (_animator == null || !IsAnimatorValid()) return;

            bool inCombatAction = _currentState == AnimationState.Attack || _currentState == AnimationState.Dash;
            if (inCombatAction)
            {
                _leanValue = Mathf.Lerp(_leanValue, 0f, 10f * Time.deltaTime);
                _headLookX = Mathf.Lerp(_headLookX, 0f, 10f * Time.deltaTime);
                _headLookY = Mathf.Lerp(_headLookY, 0f, 10f * Time.deltaTime);
                _bodyLookX = Mathf.Lerp(_bodyLookX, 0f, 10f * Time.deltaTime);
                _bodyLookY = Mathf.Lerp(_bodyLookY, 0f, 10f * Time.deltaTime);
            }

            if (_useSyntyMovementParams && !inCombatAction)
            {
                if (HasParameter("MovementInputTapped")) _animator.SetBool(_movementInputTappedHash, _movementInputTapped);
                if (HasParameter("MovementInputPressed")) _animator.SetBool(_movementInputPressedHash, _movementInputPressed);
                if (HasParameter("MovementInputHeld")) _animator.SetBool(_movementInputHeldHash, _movementInputHeld);
                if (HasParameter("IsWalking")) _animator.SetBool(_isWalkingHash, _isWalking);
            }

            if (_usePolygonParams)
            {
                if (HasParameter("MoveSpeed")) _animator.SetFloat(_moveSpeedHash, _speed2D);
                if (HasParameter("CurrentGait")) _animator.SetInteger(_currentGaitHash, (int)_currentGait);
                if (HasParameter("StrafeDirectionX")) _animator.SetFloat(_strafeDirectionXHash, _strafeDirectionX);
                if (HasParameter("StrafeDirectionZ")) _animator.SetFloat(_strafeDirectionZHash, _strafeDirectionZ);
                if (HasParameter("IsStrafing")) _animator.SetFloat(_isStrafingHash, _isStrafing ? 1f : 0f);
                if (HasParameter("IsCrouching")) _animator.SetBool(_isCrouchingHash, _isCrouching);
            }

            if (HasParameter("LeanValue")) _animator.SetFloat(_leanValueHash, _leanValue);
            if (HasParameter("HeadLookX")) _animator.SetFloat(_headLookXHash, _headLookX);
            if (HasParameter("HeadLookY")) _animator.SetFloat(_headLookYHash, _headLookY);
            if (HasParameter("BodyLookX")) _animator.SetFloat(_bodyLookXHash, _bodyLookX);
            if (HasParameter("BodyLookY")) _animator.SetFloat(_bodyLookYHash, _bodyLookY);
            if (HasParameter("InclineAngle")) _animator.SetFloat(_inclineAngleHash, _inclineAngle);
            if (HasParameter("ShuffleDirectionX")) _animator.SetFloat(_shuffleDirectionXHash, _shuffleDirectionX);
            if (HasParameter("ShuffleDirectionZ")) _animator.SetFloat(_shuffleDirectionZHash, _shuffleDirectionZ);
            if (HasParameter("ForwardStrafe")) _animator.SetFloat(_forwardStrafeHash, _forwardStrafe);
            if (HasParameter("CameraRotationOffset")) _animator.SetFloat(_cameraRotationOffsetHash, _cameraRotationOffset);
            if (HasParameter("IsTurningInPlace")) _animator.SetBool(_isTurningInPlaceHash, _isTurningInPlace);
            if (HasParameter("IsStopped")) _animator.SetBool(_isStoppedHash, _isStopped);
            if (HasParameter("IsStarting")) _animator.SetBool(_isStartingHash, _isStarting);
            if (HasParameter("LocomotionStartDirection")) _animator.SetFloat(_locomotionStartDirectionHash, _locomotionStartDirection);
            if (HasParameter("FallingDuration")) _animator.SetFloat(_fallingDurationHash, _fallingDuration);

            if (HasParameter("IsGrounded")) _animator.SetBool(_isGroundedHash, _isGrounded);
            if (HasParameter("Speed")) _animator.SetFloat(_speedHash, _currentState == AnimationState.Attack || _currentState == AnimationState.Dash ? 0f : (_speed2D / sprintSpeed));
        }

        #endregion

        #region Root Motion

        private void OnAnimatorMove()
        {
            if (_animator == null || !_animator.applyRootMotion) return;
            var rootMotion = _animator.deltaPosition;
            if (rootMotion.sqrMagnitude > 0.000001f && _controller != null && _controller.enabled &&
                (_currentState == AnimationState.Dash || _currentState == AnimationState.Attack))
            {
                _controller.Move(rootMotion);
                if (_animator.deltaRotation != Quaternion.identity)
                    transform.rotation = transform.rotation * _animator.deltaRotation;
            }
        }

        #endregion

        #region Helpers

        private bool HasParameter(string name)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return false;
            foreach (var p in _animator.parameters)
                if (p.name == name) return true;
            return false;
        }

        private bool IsAnimatorValid() => _animator != null && _animator.runtimeAnimatorController != null;

        #endregion

        #region Public API

        public bool IsAttacking => _currentState == AnimationState.Attack;
        public bool IsDashing => _currentState == AnimationState.Dash;
        public bool IsGrounded => _isGrounded;
        public CombatAction[] AvailableActions => _availableActions ?? System.Array.Empty<CombatAction>();

        #endregion
    }
}
