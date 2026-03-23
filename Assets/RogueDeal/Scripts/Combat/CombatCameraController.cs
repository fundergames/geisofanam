using UnityEngine;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Third-person camera matching Synty SampleCameraController behavior.
    /// Uses CombatInputReader for look input and LockOn for target tracking.
    /// </summary>
    public class CombatCameraController : MonoBehaviour
    {
        private const float LagDeltaTimeAdjustment = 20f;

        [Header("Target")]
        [SerializeField] private Transform target;
        [Tooltip("Optional look-at point on the character. If null, uses target position + focus height offset.")]
        [SerializeField] private Transform playerLookAt;
        [Tooltip("Height offset from target when playerLookAt is not set. Raises orbit point to chest level (e.g. 1.2).")]
        [SerializeField] private float focusHeightOffset = 1.2f;

        [Header("Camera (SampleCameraController values)")]
        [SerializeField] private bool invertCamera;
        [SerializeField] private float mouseSensitivity = 0.2f;
        [SerializeField] private float cameraDistance = 2.5f;
        [SerializeField] private float cameraHeightOffset;
        [SerializeField] private float cameraHorizontalOffset;
        [SerializeField] private float cameraTiltOffset = 15f;
        [SerializeField] private Vector2 cameraTiltBounds = new Vector2(-70f, 70f);
        [SerializeField] private float positionalCameraLag = 0.2f;
        [SerializeField] private float rotationalCameraLag = 0.35f;
        [SerializeField] private bool hideCursor;

        [Header("References")]
        [SerializeField] private CombatInputReader inputProvider;

        private ICombatInputProvider _input;
        private float _cameraInversion;
        private float _lastAngleX;
        private float _lastAngleY;
        private Vector3 _lastPosition;
        private float _targetAngleX;
        private float _targetAngleY;
        private float _currentAngleX;
        private float _currentAngleY;
        private Vector3 _newPosition;
        private bool _isLockedOn;
        private bool _wasLockedOn;
        private Transform _lockOnTarget;
        private Transform _cameraTransform;

        private void Awake()
        {
            _cameraInversion = invertCamera ? 1f : -1f;
            _cameraTransform = GetComponentInChildren<Camera>()?.transform ?? transform;

            if (playerLookAt == null && target != null)
                playerLookAt = target;
        }

        private void Start()
        {
            ResolveInput();

            if (hideCursor)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (target != null)
            {
                Vector3 pivotPos = playerLookAt != null ? playerLookAt.position : target.position + Vector3.up * focusHeightOffset;
                transform.position = pivotPos;
                transform.eulerAngles = new Vector3(cameraTiltOffset, transform.eulerAngles.y, 0f);
                _lastPosition = transform.position;
                _targetAngleX = cameraTiltOffset;
                _targetAngleY = transform.eulerAngles.y;
                _currentAngleX = _targetAngleX;
                _currentAngleY = _targetAngleY;
                _lastAngleX = _currentAngleX;
                _lastAngleY = _currentAngleY;
            }
        }

        private void ResolveInput()
        {
            if (_input != null) return;
            _input = inputProvider;
            if (_input == null)
                _input = FindFirstObjectByType<CombatInputReader>();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            ResolveInput();
            Vector3 focusPos = playerLookAt != null ? playerLookAt.position : target.position + Vector3.up * focusHeightOffset;

            float positionalSharpness = 1f / Mathf.Max(positionalCameraLag, 0.01f);
            float rotationalSharpness = 1f / Mathf.Max(rotationalCameraLag, 0.01f);
            float posSmooth = 1f - Mathf.Exp(-positionalSharpness * Time.deltaTime);
            float rotSmooth = 1f - Mathf.Exp(-rotationalSharpness * Time.deltaTime);

            float rotationX = 0f;
            float rotationY = 0f;
            if (_input != null)
            {
                var state = _input.GetState();
                rotationX = state.Look.y * _cameraInversion * mouseSensitivity;
                rotationY = state.Look.x * mouseSensitivity;
            }

            if (_wasLockedOn && !_isLockedOn)
                _targetAngleY = _currentAngleY;
            _wasLockedOn = _isLockedOn;

            _targetAngleX += rotationX;
            _targetAngleX = Mathf.Clamp(_targetAngleX, cameraTiltBounds.x, cameraTiltBounds.y);

            if (_isLockedOn && _lockOnTarget != null)
            {
                Vector3 aimVector = _lockOnTarget.position - focusPos;
                Quaternion targetRotation = Quaternion.LookRotation(aimVector);
                _targetAngleY = targetRotation.eulerAngles.y;
                _currentAngleY = Mathf.Lerp(_currentAngleY, _targetAngleY, rotSmooth);
            }
            else
            {
                _targetAngleY += rotationY;
                _currentAngleY = Mathf.Lerp(_currentAngleY, _targetAngleY, rotSmooth);
            }

            _currentAngleX = Mathf.Lerp(_currentAngleX, _targetAngleX, rotSmooth);

            _newPosition = focusPos;
            _newPosition = Vector3.Lerp(_lastPosition, _newPosition, posSmooth);

            transform.eulerAngles = new Vector3(_currentAngleX, _currentAngleY, 0f);

            // Apply camera offset: pivot+child (Sample style) vs single transform
            if (_cameraTransform != transform)
            {
                transform.position = _newPosition;
                _cameraTransform.localPosition = new Vector3(cameraHorizontalOffset, cameraHeightOffset, cameraDistance * -1f);
                _cameraTransform.localEulerAngles = new Vector3(cameraTiltOffset, 0f, 0f);
            }
            else
            {
                Vector3 offset = Quaternion.Euler(_currentAngleX, _currentAngleY, 0f) * new Vector3(cameraHorizontalOffset, cameraHeightOffset, -cameraDistance);
                transform.position = _newPosition + offset;
            }

            _lastPosition = transform.position;
            _lastAngleX = _currentAngleX;
            _lastAngleY = _currentAngleY;
        }

        /// <summary>
        /// Locks the camera to aim at a specified target.
        /// </summary>
        public void LockOn(bool enable, Transform lockOnTarget)
        {
            _isLockedOn = enable;
            if (lockOnTarget != null)
                _lockOnTarget = lockOnTarget;
        }

        public bool IsLockedOn => _isLockedOn;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (playerLookAt == null)
                playerLookAt = newTarget;
        }

        public void SetFollowEnabled(bool enabled) { }

        public void SnapToTarget()
        {
            if (target != null)
            {
                Vector3 focusPos = playerLookAt != null ? playerLookAt.position : target.position + Vector3.up * focusHeightOffset;
                transform.position = focusPos + Quaternion.Euler(_currentAngleX, _currentAngleY, 0f) * new Vector3(cameraHorizontalOffset, cameraHeightOffset, -cameraDistance);
                transform.eulerAngles = new Vector3(_currentAngleX, _currentAngleY, 0f);
                _lastPosition = transform.position;
                _newPosition = focusPos;
            }
        }

        public Vector3 GetCameraPosition() => _cameraTransform != null ? _cameraTransform.position : transform.position;
        public Vector3 GetCameraForward() => _cameraTransform != null ? _cameraTransform.forward : transform.forward;
        public Vector3 GetCameraForwardZeroedY()
        {
            var fwd = _cameraTransform != null ? _cameraTransform.forward : transform.forward;
            return new Vector3(fwd.x, 0f, fwd.z);
        }
        public Vector3 GetCameraForwardZeroedYNormalised() => GetCameraForwardZeroedY().normalized;
        public Vector3 GetCameraRightZeroedY()
        {
            var right = _cameraTransform != null ? _cameraTransform.right : transform.right;
            return new Vector3(right.x, 0f, right.z);
        }
        public Vector3 GetCameraRightZeroedYNormalised() => GetCameraRightZeroedY().normalized;
        public float GetCameraTiltX() => _cameraTransform != null ? _cameraTransform.eulerAngles.x : transform.eulerAngles.x;
    }
}
