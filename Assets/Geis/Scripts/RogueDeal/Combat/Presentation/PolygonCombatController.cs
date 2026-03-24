using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Targeting;
using RogueDeal.Combat.Targeting;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Polygon-style character controller combining Synty locomotion (walk/run/sprint, crouch, strafing, jump)
    /// with combat (dodge, attacks, lock-on targeting).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CombatExecutor))]
    [RequireComponent(typeof(CombatEntity))]
    public class PolygonCombatController : MonoBehaviour
    {
        [Header("Locomotion - Polygon Style")]
        [SerializeField] private float walkSpeed = 1.4f;
        [SerializeField] private float runSpeed = 2.5f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float rotationSmoothing = 10f;
        [SerializeField] private bool alwaysStrafe = true;

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

        [Header("Combat")]
        [SerializeField] private float dodgeDuration = 0.3f;
        [SerializeField] private float manualDodgeSpeed = 15f;
        [SerializeField] private bool useWeaponColliders = true;
        [SerializeField] private CombatAction[] combatActions;
        [SerializeField] private int actionStateCount = 2;
        [SerializeField] private int actionIndexOffset = 0;

        [Header("References")]
        [SerializeField] private CombatInputReader inputProvider;
        [SerializeField] private CombatCameraController cameraController;
        [SerializeField] private RuntimeAnimatorController defaultAnimatorController;

        private CharacterController _controller;
        private Animator _animator;
        private CombatExecutor _combatExecutor;
        private CombatEntity _combatEntity;
        private Camera _mainCamera;
        private TargetingManager _targetingManager;
        private LockOnIndicator _lockOnIndicator;
        private ICombatInputProvider _inputProvider;

        private Vector3 _velocity;
        private Vector3 _moveDirection;
        private bool _isGrounded = true;
        private bool _isCrouching;
        private bool _isSprinting;
        private bool _isStrafing;
        private bool _isLockedOn;
        private bool _isDodging;
        private bool _isAttacking;
        private float _dodgeTimer;
        private float _attackStateTimeout;
        private Vector3 _dodgeDirection;
        private float _speed2D;
        private int _currentGait; // 0=Idle, 1=Walk, 2=Run, 3=Sprint
        private float _strafeDirectionX;
        private float _strafeDirectionZ;
        private float _currentMaxSpeed;
        private CombatAction[] _availableActions;
        private int _currentComboIndex;

        private readonly int _moveSpeedHash = Animator.StringToHash("MoveSpeed");
        private readonly int _currentGaitHash = Animator.StringToHash("CurrentGait");
        private readonly int _strafeDirectionXHash = Animator.StringToHash("StrafeDirectionX");
        private readonly int _strafeDirectionZHash = Animator.StringToHash("StrafeDirectionZ");
        private readonly int _isStrafingHash = Animator.StringToHash("IsStrafing");
        private readonly int _isCrouchingHash = Animator.StringToHash("IsCrouching");
        private readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");
        private readonly int _isJumpingHash = Animator.StringToHash("IsJumping");
        private readonly int _speedHash = Animator.StringToHash("Speed");
        private readonly int _dodgeTriggerHash = Animator.StringToHash("Dodge");
        private readonly int _takeActionHash = Animator.StringToHash("TakeAction");
        private readonly int _actionIndexHash = Animator.StringToHash("ActionIndex");
        private readonly int _actionTypeHash = Animator.StringToHash("ActionType");
        private readonly int _isActionHash = Animator.StringToHash("IsAction");
        private readonly int _attack1Hash = Animator.StringToHash("Attack_1");
        private readonly int _attack2Hash = Animator.StringToHash("Attack_2");
        private readonly int _attack3Hash = Animator.StringToHash("Attack_3");

        private const float MovementRampTime = 0.12f;

        private bool _usePolygonParams;
        private bool _useLegacyAttackTriggers;
        private float _timeSinceMoveStart;

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
                if (_animator.runtimeAnimatorController == null && defaultAnimatorController != null)
                    _animator.runtimeAnimatorController = defaultAnimatorController;
            }
        }

        private void Update()
        {
            HandleInput();
            HandleLockOn();
            GroundedCheck();
            HandleMovement();
            UpdateAnimator();
            CheckAttackState();
            UpdateLockOnIndicator();
        }

        private void HandleInput()
        {
            if (_inputProvider == null) return;
            var state = _inputProvider.GetState();

            if (state.CrouchPressed && _isGrounded)
            {
                _isCrouching = !_isCrouching;
                SetCapsuleCrouch(_isCrouching);
            }

            _isLockedOn = _targetingManager != null && _targetingManager.IsLockedOn();
            _isStrafing = alwaysStrafe || _isLockedOn;
            if (_isSprinting) _isStrafing = false;

            _isSprinting = state.SprintHeld && !_isCrouching;
        }

        private void HandleLockOn()
        {
            if (_inputProvider == null || _targetingManager == null || cameraController == null) return;
            var state = _inputProvider.GetState();
            if (state.LockOnPressed)
            {
                _targetingManager.ToggleLockOn();
                cameraController.LockOn(_targetingManager.IsLockedOn(), _targetingManager.GetLockOnTargetTransform());
            }
        }

        private void GroundedCheck()
        {
            var spherePos = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
            _isGrounded = Physics.CheckSphere(spherePos, _controller.radius, groundLayerMask, QueryTriggerInteraction.Ignore);
        }

        private void SetCapsuleCrouch(bool crouching)
        {
            _controller.height = crouching ? capsuleCrouchingHeight : capsuleStandingHeight;
            _controller.center = new Vector3(0, crouching ? capsuleCrouchingCentre : capsuleStandingCentre, 0);
        }

        private void HandleMovement()
        {
            if (_isGrounded && _velocity.y < 0)
                _velocity.y = -2f;
            _velocity.y += gravity * Time.deltaTime;

            if (_isDodging)
            {
                _dodgeTimer -= Time.deltaTime;
                if (_dodgeTimer <= 0)
                {
                    _isDodging = false;
                }
                else if (_controller != null && _controller.enabled)
                {
                    _controller.Move((_dodgeDirection * manualDodgeSpeed + _velocity) * Time.deltaTime);
                }
                return;
            }

            if (_isAttacking)
            {
                if (_controller != null && _controller.enabled)
                    _controller.Move(_velocity * Time.deltaTime);
                return;
            }

            var state = _inputProvider?.GetState() ?? default;
            Vector2 moveInput = state.Move;

            Vector3 camFwd = _mainCamera != null ? new Vector3(_mainCamera.transform.forward.x, 0, _mainCamera.transform.forward.z).normalized : transform.forward;
            Vector3 camRight = _mainCamera != null ? new Vector3(_mainCamera.transform.right.x, 0, _mainCamera.transform.right.z).normalized : transform.right;
            _moveDirection = (camFwd * moveInput.y + camRight * moveInput.x);

            float targetSpeed = _isCrouching ? walkSpeed : (_isSprinting ? sprintSpeed : (state.Run || state.SprintHeld ? runSpeed : walkSpeed));
            _currentMaxSpeed = Mathf.Lerp(_currentMaxSpeed, targetSpeed, 10f * Time.deltaTime);

            bool hasMoveInput = _moveDirection.sqrMagnitude > 0.0001f;
            if (hasMoveInput)
                _timeSinceMoveStart += Time.deltaTime;
            else
                _timeSinceMoveStart = 0f;

            float velocityScale = hasMoveInput ? Mathf.Clamp01(_timeSinceMoveStart / MovementRampTime) : 1f;
            Vector3 horizontalVel = _moveDirection.normalized * (_moveDirection.magnitude > 0.01f ? _currentMaxSpeed * velocityScale : 0);
            _velocity.x = horizontalVel.x;
            _velocity.z = horizontalVel.z;
            _speed2D = new Vector3(_velocity.x, 0, _velocity.z).magnitude;

            if (_speed2D < 0.01f) _currentGait = 0;
            else if (_speed2D < (walkSpeed + runSpeed) / 2) _currentGait = 1;
            else if (_speed2D < (runSpeed + sprintSpeed) / 2) _currentGait = 2;
            else _currentGait = 3;

            if (_moveDirection.magnitude > 0.01f)
            {
                Vector3 charFwd = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                Vector3 charRight = new Vector3(transform.right.x, 0, transform.right.z).normalized;
                Vector3 dirFwd = new Vector3(_moveDirection.x, 0, _moveDirection.z).normalized;
                _strafeDirectionZ = Mathf.Lerp(_strafeDirectionZ, Vector3.Dot(charFwd, dirFwd), 5f * Time.deltaTime);
                _strafeDirectionX = Mathf.Lerp(_strafeDirectionX, Vector3.Dot(charRight, dirFwd), 5f * Time.deltaTime);
            }
            else
            {
                _strafeDirectionZ = Mathf.Lerp(_strafeDirectionZ, 1f, 5f * Time.deltaTime);
                _strafeDirectionX = Mathf.Lerp(_strafeDirectionX, 0f, 5f * Time.deltaTime);
            }

            Quaternion targetRot;
            if (_isStrafing)
            {
                targetRot = camFwd.sqrMagnitude > 0.001f ? Quaternion.LookRotation(camFwd) : transform.rotation;
            }
            else if (_isLockedOn && _targetingManager != null)
            {
                var locked = _targetingManager.GetLockedOnTarget();
                if (locked != null)
                {
                    Vector3 toTarget = locked.transform.position - transform.position;
                    toTarget.y = 0;
                    if (toTarget.sqrMagnitude > 0.001f)
                        targetRot = Quaternion.LookRotation(toTarget.normalized);
                    else
                        targetRot = transform.rotation;
                }
                else
                {
                    targetRot = _moveDirection.sqrMagnitude > 0.01f ? Quaternion.LookRotation(new Vector3(_moveDirection.x, 0, _moveDirection.z).normalized) : transform.rotation;
                }
            }
            else
            {
                targetRot = _moveDirection.sqrMagnitude > 0.01f ? Quaternion.LookRotation(new Vector3(_moveDirection.x, 0, _moveDirection.z).normalized) : transform.rotation;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothing * Time.deltaTime);

            if (state.JumpPressed && _isGrounded && !_isAttacking && !_isDodging)
            {
                _velocity.y = jumpForce;
                if (_animator != null && HasParameter("IsJumping"))
                    _animator.SetBool(_isJumpingHash, true);
            }
            if (HasParameter("IsJumping") && _animator != null && _isGrounded)
                _animator.SetBool(_isJumpingHash, false);

            if (state.DodgePressed && _isGrounded && !_isAttacking)
            {
                StartDodge();
                return;
            }

            if (state.AttackPressed && !_isAttacking && !_isDodging)
            {
                StartAttack();
                return;
            }

            if (state.AttackPressed && state.HasAttackClickPosition && _targetingManager != null)
                _targetingManager.HandleMouseClick(state.AttackClickScreenPosition);

            if (_controller != null && _controller.enabled)
                _controller.Move(_velocity * Time.deltaTime);
        }

        private void StartDodge()
        {
            _isDodging = true;
            _dodgeTimer = dodgeDuration;
            var state = _inputProvider?.GetState() ?? default;
            Vector2 moveInput = state.Move;
            if (moveInput.sqrMagnitude > 0.01f && _mainCamera != null)
            {
                Vector3 camFwd = new Vector3(_mainCamera.transform.forward.x, 0, _mainCamera.transform.forward.z).normalized;
                Vector3 camRight = new Vector3(_mainCamera.transform.right.x, 0, _mainCamera.transform.right.z).normalized;
                _dodgeDirection = (camFwd * moveInput.y + camRight * moveInput.x).normalized;
            }
            else
            {
                _dodgeDirection = transform.forward;
            }
            transform.rotation = Quaternion.LookRotation(_dodgeDirection);
            if (_animator != null && HasParameter("Dodge"))
                _animator.SetTrigger(_dodgeTriggerHash);
        }

        private void StartAttack()
        {
            if (_isAttacking) return;
            _isAttacking = true;

            int actionCount = _availableActions?.Length ?? actionStateCount;
            int actionIndex = (_currentComboIndex % actionCount) + actionIndexOffset;
            CombatAction actionToUse = _availableActions != null && _availableActions.Length > 0 ? _availableActions[_currentComboIndex % _availableActions.Length] : null;

            if (actionToUse != null && _combatExecutor != null)
            {
                var cm = _combatExecutor.GetCooldownManager();
                if (cm != null && !cm.IsActionAvailable(actionToUse))
                {
                    _isAttacking = false;
                    return;
                }
            }

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
                _attackStateTimeout = 5f;
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

        private void CheckAttackState()
        {
            if (!_isAttacking) return;
            if (_attackStateTimeout > 0)
            {
                _attackStateTimeout -= Time.deltaTime;
                if (_attackStateTimeout <= 0) ResetAttackState();
            }
            if (!_useLegacyAttackTriggers && _animator != null && HasParameter("IsAction") && !_animator.GetBool(_isActionHash))
                ResetAttackState();
        }

        private void ResetAttackState()
        {
            _isAttacking = false;
            if (useWeaponColliders && _combatExecutor != null)
                _combatExecutor.ClearCurrentAction();
            if (_animator != null && HasParameter("IsAction"))
                _animator.SetBool(_isActionHash, false);
        }

        private IEnumerator UpdateAttackTimeoutFromAnimation()
        {
            yield return null;
            if (_isAttacking && _animator != null)
            {
                var clipInfo = _animator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo != null && clipInfo.Length > 0)
                    _attackStateTimeout = clipInfo[0].clip.length + 0.2f;
            }
        }

        private void UpdateAnimator()
        {
            if (_animator == null || !IsAnimatorValid()) return;

            if (_usePolygonParams)
            {
                if (HasParameter("MoveSpeed")) _animator.SetFloat(_moveSpeedHash, _speed2D);
                if (HasParameter("CurrentGait")) _animator.SetInteger(_currentGaitHash, _currentGait);
                if (HasParameter("StrafeDirectionX")) _animator.SetFloat(_strafeDirectionXHash, _strafeDirectionX);
                if (HasParameter("StrafeDirectionZ")) _animator.SetFloat(_strafeDirectionZHash, _strafeDirectionZ);
                if (HasParameter("IsStrafing")) _animator.SetFloat(_isStrafingHash, _isStrafing ? 1f : 0f);
                if (HasParameter("IsCrouching")) _animator.SetBool(_isCrouchingHash, _isCrouching);
            }
            if (HasParameter("IsGrounded")) _animator.SetBool(_isGroundedHash, _isGrounded);
            if (HasParameter("Speed")) _animator.SetFloat(_speedHash, _isAttacking || _isDodging ? 0f : (_speed2D / sprintSpeed));
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

        private void OnAnimatorMove()
        {
            if (_animator == null || !_animator.applyRootMotion) return;
            var rootMotion = _animator.deltaPosition;
            if (rootMotion.sqrMagnitude > 0.000001f && _controller != null && _controller.enabled && (_isDodging || _isAttacking))
            {
                _controller.Move(rootMotion);
                if (_animator.deltaRotation != Quaternion.identity)
                    transform.rotation = transform.rotation * _animator.deltaRotation;
            }
        }

        public void OnAttackEnd() => ResetAttackState();
        public void OnDodgeEnd() => _isDodging = false;

        private bool HasParameter(string name)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return false;
            foreach (var p in _animator.parameters)
                if (p.name == name) return true;
            return false;
        }

        private bool IsAnimatorValid() => _animator != null && _animator.runtimeAnimatorController != null;

        public bool IsAttacking => _isAttacking;
        public bool IsDodging => _isDodging;
        public bool IsGrounded => _isGrounded;
    }
}
