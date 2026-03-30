using Geis.InputSystem;
using Geis.Locomotion;
using UnityEngine;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Soul avatar locomotion: mirrors <see cref="GeisPlayerAnimationController"/> move direction,
    /// walk/run/sprint, dodge tuning, jump, gravity, grounded check, and capsule size from the same values.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class SoulGhostMotor : MonoBehaviour
    {
        private const float MaxFallVelocityY = -55f;

        [SerializeField] private GeisInputReader inputReader;

        [Header("Fallback (no body locomotion reference)")]
        [SerializeField] private float fallbackMoveSpeed = 4.5f;
        [SerializeField] private float fallbackJumpForce = 10f;
        [SerializeField] private float fallbackGravityMultiplier = 2f;

        [Header("Dodge (fallback if no body reference)")]
        [SerializeField] private float ghostDodgeDuration = 0.35f;
        [SerializeField] private float ghostDodgeSpeed = 7f;
        [Tooltip("Stick magnitude below this uses camera-forward dodge (avoids drift steering dodges forward).")]
        [SerializeField] private float ghostDodgeDirectionDeadzone = 0.35f;

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

        private float _dodgeTimeRemaining;
        private Vector3 _dodgePlanarDir = Vector3.forward;

        private Transform _groundRideSurface;
        private Vector3 _groundRideLastWorldPos;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            RefreshInputSubscriptions();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
        }

        private void RefreshInputSubscriptions()
        {
            if (inputReader == null)
                return;
            inputReader.onJumpPerformed -= OnJump;
            inputReader.onDodgePerformed -= OnDodgePerformed;
            inputReader.onJumpPerformed += OnJump;
            inputReader.onDodgePerformed += OnDodgePerformed;
        }

        private void UnsubscribeInput()
        {
            if (inputReader == null)
                return;
            inputReader.onJumpPerformed -= OnJump;
            inputReader.onDodgePerformed -= OnDodgePerformed;
        }

        private void OnJump()
        {
            if (GroundedCheck())
                _jumpQueued = true;
        }

        private void OnDodgePerformed()
        {
            if (SoulRealmManager.Instance == null || !SoulRealmManager.Instance.AllowGhostMovement)
                return;
            if (_dodgeTimeRemaining > 0f)
                return;

            // Match body dodge: sphere grounded only. CC often false for the ghost capsule; requiring both blocked dodge.
            if (!GroundedCheck())
                return;

            if (_bodyLocomotion != null && _bodyLocomotion.LocomotionDodgeRequiresMovementInput && inputReader != null)
            {
                float dz = _bodyLocomotion.LocomotionDodgeDeadzone;
                if (inputReader._moveComposite.sqrMagnitude < dz * dz)
                    return;
            }

            _dodgePlanarDir = ComputeGhostDodgePlanarDirection();
            _dodgeTimeRemaining = _bodyLocomotion != null
                ? _bodyLocomotion.LocomotionDodgeScriptedDuration
                : ghostDodgeDuration;
        }

        private Vector3 ComputeGhostDodgePlanarDirection()
        {
            float dz = Mathf.Max(
                ghostDodgeDirectionDeadzone,
                _bodyLocomotion != null ? _bodyLocomotion.LocomotionDodgeDeadzone : 0.05f);
            Vector2 m = inputReader != null ? inputReader._moveComposite : Vector2.zero;
            if (_cameraController != null)
            {
                Vector3 camFwd = _cameraController.GetCameraForwardZeroedYNormalised();
                Vector3 camRight = _cameraController.GetCameraRightZeroedYNormalised();
                if (m.sqrMagnitude >= dz * dz)
                    return (camFwd * m.y + camRight * m.x).normalized;
                return camFwd.sqrMagnitude > 0.0001f ? camFwd : transform.forward;
            }

            if (m.sqrMagnitude >= dz * dz && _cameraTransform != null)
            {
                Vector3 camFwd = Vector3.Scale(_cameraTransform.forward, new Vector3(1f, 0f, 1f)).normalized;
                Vector3 camRight = Vector3.Scale(_cameraTransform.right, new Vector3(1f, 0f, 1f)).normalized;
                return (camFwd * m.y + camRight * m.x).normalized;
            }

            return transform.forward;
        }

        private void Start()
        {
            if (inputReader == null)
                inputReader = FindFirstObjectByType<GeisInputReader>();
            if (_cameraController == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;
            RefreshInputSubscriptions();
        }

        public void Configure(GeisInputReader reader, GeisPlayerAnimationController bodyLocomotion = null,
            GeisCameraController cameraController = null)
        {
            UnsubscribeInput();
            inputReader = reader;
            _bodyLocomotion = bodyLocomotion;
            _cameraController = cameraController;
            if (_bodyLocomotion != null && _cc != null)
                ApplyCapsuleFromBody();
            _groundedAfterMove = GroundedCheck();
            if (isActiveAndEnabled)
                RefreshInputSubscriptions();
        }

        /// <summary>
        /// Call when entering soul realm so max speed and planar velocity match the body (same walk/run/sprint caps as physical).
        /// Avoids stale ghost lerp state after realm swap.
        /// </summary>
        public void SyncFromBodyForSoulRealm(GeisPlayerAnimationController body)
        {
            if (body == null)
                return;

            float targetMaxSpeed;
            if (body.LocomotionIsCrouching)
                targetMaxSpeed = body.LocomotionWalkSpeed;
            else if (body.LocomotionIsSprinting)
                targetMaxSpeed = body.LocomotionSprintSpeed;
            else if (body.LocomotionIsWalking)
                targetMaxSpeed = body.LocomotionWalkSpeed;
            else
                targetMaxSpeed = body.LocomotionRunSpeed;

            _currentMaxSpeed = targetMaxSpeed;

            Vector3 planar = body.LocomotionPlanarVelocity;
            float mag = planar.magnitude;
            if (mag > 0.0001f)
            {
                Vector3 dir = planar / mag;
                float v = Mathf.Min(mag, targetMaxSpeed);
                _velocity.x = dir.x * v;
                _velocity.z = dir.z * v;
            }
            else
            {
                _velocity.x = 0f;
                _velocity.z = 0f;
            }
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

            if (_dodgeTimeRemaining > 0f)
            {
                _dodgeTimeRemaining -= Time.deltaTime;
                Vector3 d = _dodgePlanarDir;
                d.y = 0f;
                float dodgeSpeed = _bodyLocomotion != null
                    ? _bodyLocomotion.LocomotionDodgeScriptedPlaneSpeed
                    : ghostDodgeSpeed;
                if (d.sqrMagnitude < 0.0001f)
                    d = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                else
                    d.Normalize();
                _velocity.x = d.x * dodgeSpeed;
                _velocity.z = d.z * dodgeSpeed;
            }
            else if (_bodyLocomotion != null)
                CalculateMoveDirection(groundedBeforeMove);
            else
                FallbackCalculateVelocity(groundedBeforeMove);

            if (_jumpQueued && groundedBeforeMove)
            {
                _velocity.y = _bodyLocomotion != null ? _bodyLocomotion.LocomotionJumpForce : fallbackJumpForce;
                _jumpQueued = false;
            }

            // Snap to ground only when both sphere and CharacterController agree (sphere alone can false-positive in air).
            bool stickyGrounded = groundedBeforeMove && _cc != null && _cc.isGrounded;
            if (stickyGrounded && _velocity.y <= 0f)
                _velocity.y = -2f;
            else
                ApplyGravity();

            float rideOff = _bodyLocomotion != null ? _bodyLocomotion.LocomotionGroundedOffset : 0.14f;
            LayerMask rideMask = _bodyLocomotion != null ? _bodyLocomotion.LocomotionGroundLayerMask : (LayerMask)(-1);
            if (rideMask.value == 0)
                rideMask = (LayerMask)(-1);

            Vector3 groundRide = GroundRideUtility.GetRideDelta(
                transform, _cc, rideMask, rideOff,
                ref _groundRideSurface, ref _groundRideLastWorldPos, groundedBeforeMove);

            _cc.Move(groundRide + _velocity * Time.deltaTime);
            _groundedAfterMove = GroundedCheck();

            if (_dodgeTimeRemaining > 0f)
                ApplyRotationDodge();
            else
                ApplyRotation();
        }

        private void ApplyRotationDodge()
        {
            Vector3 d = _dodgePlanarDir;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f)
                return;
            Quaternion targetRot = Quaternion.LookRotation(d.normalized);
            float t = (_bodyLocomotion != null ? _bodyLocomotion.LocomotionRotationSmoothing : 12f) * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
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
            if (_velocity.y > MaxFallVelocityY)
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
            Vector3 direction = new Vector3(_moveDirection.x, 0f, _moveDirection.z);

            bool strafe = _bodyLocomotion.LocomotionIsStrafing;

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
