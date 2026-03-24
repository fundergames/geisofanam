using Geis.InputSystem;
using Geis.Locomotion;
using UnityEngine;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Soul avatar locomotion: mirrors <see cref="GeisPlayerAnimationController"/> move direction,
    /// speed/acceleration, jump, gravity, grounded check, and capsule size from the same serialized values.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class SoulGhostMotor : MonoBehaviour
    {
        [SerializeField] private GeisInputReader inputReader;

        [Header("Fallback (no body locomotion reference)")]
        [SerializeField] private float fallbackMoveSpeed = 4.5f;
        [SerializeField] private float fallbackJumpForce = 10f;
        [SerializeField] private float fallbackGravityMultiplier = 2f;

        private CharacterController _cc;
        private Transform _cameraTransform;
        private GeisCameraController _cameraController;
        private GeisPlayerAnimationController _bodyLocomotion;

        private Vector3 _velocity;
        private float _currentMaxSpeed;
        private Vector3 _targetVelocity;
        private float _targetMaxSpeed;
        private Vector3 _moveDirection;
        private bool _jumpQueued;
        private bool _groundedAfterMove;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            if (inputReader != null)
                inputReader.onJumpPerformed += OnJump;
        }

        private void OnDisable()
        {
            if (inputReader != null)
                inputReader.onJumpPerformed -= OnJump;
        }

        private void OnJump()
        {
            if (GroundedCheck())
                _jumpQueued = true;
        }

        private void Start()
        {
            if (inputReader == null)
                inputReader = FindFirstObjectByType<GeisInputReader>();
            if (_cameraController == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }

        public void Configure(GeisInputReader reader, GeisPlayerAnimationController bodyLocomotion = null,
            GeisCameraController cameraController = null)
        {
            inputReader = reader;
            _bodyLocomotion = bodyLocomotion;
            _cameraController = cameraController;
            if (_bodyLocomotion != null && _cc != null)
                ApplyCapsuleFromBody();
            _groundedAfterMove = GroundedCheck();
        }

        /// <summary>Horizontal movement speed (matches player <c>_speed2D</c> calculation).</summary>
        public float MirrorSpeed2D
        {
            get
            {
                float s = new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
                return Mathf.Round(s * 1000f) / 1000f;
            }
        }

        /// <summary>Horizontal velocity after last Move (world space).</summary>
        public Vector3 PlanarVelocity => new Vector3(_velocity.x, 0f, _velocity.z);

        public float VerticalVelocity => _velocity.y;

        private void Update()
        {
            if (SoulRealmManager.Instance == null || !SoulRealmManager.Instance.AllowGhostMovement)
                return;

            if (_cameraController == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            if (_bodyLocomotion != null)
                ApplyCapsuleFromBody();

            if (_bodyLocomotion != null && _cameraController != null)
                CalculateInputAndMoveDirection();
            else
                FallbackMoveDirection();

            bool groundedBeforeMove = GroundedCheck();

            if (_bodyLocomotion != null)
                CalculateMoveDirection(groundedBeforeMove);
            else
                FallbackCalculateVelocity(groundedBeforeMove);

            if (_jumpQueued && groundedBeforeMove)
            {
                _velocity.y = _bodyLocomotion != null ? _bodyLocomotion.LocomotionJumpForce : fallbackJumpForce;
                _jumpQueued = false;
            }

            // Sticky grounded when moving down; otherwise apply gravity (including upward jump so we never
            // float when grounded check falsely stays true in air).
            if (groundedBeforeMove && _velocity.y <= 0f)
                _velocity.y = -2f;
            else
                ApplyGravity();

            _cc.Move(_velocity * Time.deltaTime);
            _groundedAfterMove = GroundedCheck();

            ApplyRotation();
        }

        private void ApplyCapsuleFromBody()
        {
            if (_cc == null || _bodyLocomotion == null)
                return;

            if (_bodyLocomotion.LocomotionIsCrouching)
            {
                _cc.height = _bodyLocomotion.LocomotionCapsuleCrouchingHeight;
                _cc.center = new Vector3(0f, _bodyLocomotion.LocomotionCapsuleCrouchingCentre, 0f);
            }
            else
            {
                _cc.height = _bodyLocomotion.LocomotionCapsuleStandingHeight;
                _cc.center = new Vector3(0f, _bodyLocomotion.LocomotionCapsuleStandingCentre, 0f);
            }

            _cc.radius = _bodyLocomotion.LocomotionCapsuleRadius;
        }

        private void CalculateInputAndMoveDirection()
        {
            if (inputReader == null || _cameraController == null)
            {
                FallbackMoveDirection();
                return;
            }

            _moveDirection = _cameraController.GetCameraForwardZeroedYNormalised() * inputReader._moveComposite.y
                + _cameraController.GetCameraRightZeroedYNormalised() * inputReader._moveComposite.x;
        }

        private void FallbackMoveDirection()
        {
            Vector3 camForward;
            Vector3 camRight;
            if (_cameraTransform != null)
            {
                camForward = Vector3.Scale(_cameraTransform.forward, new Vector3(1f, 0f, 1f)).normalized;
                camRight = Vector3.Scale(_cameraTransform.right, new Vector3(1f, 0f, 1f)).normalized;
            }
            else
            {
                camForward = transform.forward;
                camRight = transform.right;
            }

            Vector2 move = inputReader != null ? inputReader._moveComposite : Vector2.zero;
            _moveDirection = camRight * move.x + camForward * move.y;
        }

        /// <summary>Same speed targets and lerps as <see cref="GeisPlayerAnimationController.CalculateMoveDirection"/>.</summary>
        private void CalculateMoveDirection(bool grounded)
        {
            var b = _bodyLocomotion;

            if (!grounded)
            {
                _targetMaxSpeed = _currentMaxSpeed;
            }
            else if (b.LocomotionIsCrouching)
            {
                _targetMaxSpeed = b.LocomotionWalkSpeed;
            }
            else if (b.LocomotionIsSprinting)
            {
                _targetMaxSpeed = b.LocomotionSprintSpeed;
            }
            else if (b.LocomotionIsWalking)
            {
                _targetMaxSpeed = b.LocomotionWalkSpeed;
            }
            else
            {
                _targetMaxSpeed = b.LocomotionRunSpeed;
            }

            float damp = b.LocomotionMaxSpeedLerpRate * Time.deltaTime;
            _currentMaxSpeed = Mathf.Lerp(_currentMaxSpeed, _targetMaxSpeed, damp);

            _targetVelocity.x = _moveDirection.x * _currentMaxSpeed;
            _targetVelocity.z = _moveDirection.z * _currentMaxSpeed;

            float speedDamp = b.LocomotionSpeedChangeDamping * Time.deltaTime;
            _velocity.x = Mathf.Lerp(_velocity.x, _targetVelocity.x, speedDamp);
            _velocity.z = Mathf.Lerp(_velocity.z, _targetVelocity.z, speedDamp);
        }

        private void FallbackCalculateVelocity(bool grounded)
        {
            Vector3 direction = _moveDirection.sqrMagnitude > 1f ? _moveDirection.normalized : _moveDirection;
            bool sprint = inputReader != null && inputReader.IsSprintHeldOrToggled;
            float speed = fallbackMoveSpeed * (sprint ? 1.35f : 1f);
            _targetVelocity = direction * speed;
            float d = 10f * Time.deltaTime;
            _velocity.x = Mathf.Lerp(_velocity.x, _targetVelocity.x, d);
            _velocity.z = Mathf.Lerp(_velocity.z, _targetVelocity.z, d);
        }

        private void ApplyGravity()
        {
            float gm = _bodyLocomotion != null ? _bodyLocomotion.LocomotionGravityMultiplier : fallbackGravityMultiplier;
            if (_velocity.y > Physics.gravity.y)
                _velocity.y += Physics.gravity.y * gm * Time.deltaTime;
        }

        private void ApplyRotation()
        {
            if (_bodyLocomotion == null || _cameraController == null)
            {
                Vector3 dir = new Vector3(_moveDirection.x, 0f, _moveDirection.z);
                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
                }

                return;
            }

            float rotSmooth = _bodyLocomotion.LocomotionRotationSmoothing;
            bool strafe = _bodyLocomotion.LocomotionIsStrafing;
            Vector3 direction = new Vector3(_moveDirection.x, 0f, _moveDirection.z);

            if (strafe && direction.sqrMagnitude > 0.01f)
            {
                Vector3 cf = _cameraController.GetCameraForwardZeroedYNormalised();
                if (cf.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(cf);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSmooth * Time.deltaTime);
                }
            }
            else if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSmooth * Time.deltaTime);
            }
        }

        /// <summary>Same sphere test as <see cref="GeisPlayerAnimationController"/> grounded check.</summary>
        private bool GroundedCheck()
        {
            if (_cc == null)
                return false;

            float offset = _bodyLocomotion != null ? _bodyLocomotion.LocomotionGroundedOffset : 0.14f;
            LayerMask mask = _bodyLocomotion != null ? _bodyLocomotion.LocomotionGroundLayerMask : (LayerMask)(-1);
            if (mask.value == 0)
                mask = (LayerMask)(-1);

            float sphereY = _cc.transform.position.y + _cc.center.y - (_cc.height * 0.5f) - offset;
            Vector3 spherePosition = new Vector3(_cc.transform.position.x, sphereY, _cc.transform.position.z);
            return Physics.CheckSphere(spherePosition, _cc.radius, mask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>For spectral animator / VFX (matches post-<see cref="CharacterController.Move"/> grounded test).</summary>
        public bool IsGroundedPublic => _groundedAfterMove;
    }
}
