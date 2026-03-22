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
        [SerializeField] private float mouseSensitivity = 0.5f;
        [SerializeField] private float cameraDistance = 2.5f;
        [SerializeField] private float cameraHeightOffset;
        [SerializeField] private float cameraHorizontalOffset;
        [SerializeField] private float cameraTiltOffset = 15f;
        [SerializeField] private Vector2 cameraTiltBounds = new Vector2(-70f, 70f);
        [SerializeField] private float positionalCameraLag = 0.2f;
        [SerializeField] private float rotationalCameraLag = 0.2f;
        [SerializeField] private bool hideCursor;

        [Header("References")]
        [SerializeField] private CombatInputReader inputProvider;

        private ICombatInputProvider _input;
        private float _cameraInversion;
        private float _lastAngleX;
        private float _lastAngleY;
        private Vector3 _lastPosition;
        private float _newAngleX;
        private float _newAngleY;
        private Vector3 _newPosition;
        private bool _isLockedOn;
        private Transform _lockOnTarget;
        private bool _initialized;
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
                _newAngleX = cameraTiltOffset;
                _newAngleY = transform.eulerAngles.y;
                _lastAngleX = _newAngleX;
                _lastAngleY = _newAngleY;
            }
            _initialized = true;
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

            float positionalFollowSpeed = 1f / Mathf.Max(positionalCameraLag / LagDeltaTimeAdjustment, 0.001f);
            float rotationalFollowSpeed = 1f / Mathf.Max(rotationalCameraLag / LagDeltaTimeAdjustment, 0.001f);

            float rotationX = 0f;
            float rotationY = 0f;
            if (_input != null)
            {
                var state = _input.GetState();
                rotationX = state.Look.y * _cameraInversion * mouseSensitivity;
                rotationY = state.Look.x * mouseSensitivity;
            }

            _newAngleX += rotationX;
            _newAngleX = Mathf.Clamp(_newAngleX, cameraTiltBounds.x, cameraTiltBounds.y);
            _newAngleX = Mathf.Lerp(_lastAngleX, _newAngleX, rotationalFollowSpeed * Time.deltaTime);

            if (_isLockedOn && _lockOnTarget != null)
            {
                Vector3 aimVector = _lockOnTarget.position - focusPos;
                Quaternion targetRotation = Quaternion.LookRotation(aimVector);
                targetRotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationalFollowSpeed * Time.deltaTime);
                _newAngleY = targetRotation.eulerAngles.y;
            }
            else
            {
                _newAngleY += rotationY;
                _newAngleY = Mathf.Lerp(_lastAngleY, _newAngleY, rotationalFollowSpeed * Time.deltaTime);
            }

            _newPosition = focusPos;
            _newPosition = Vector3.Lerp(_lastPosition, _newPosition, positionalFollowSpeed * Time.deltaTime);

            transform.eulerAngles = new Vector3(_newAngleX, _newAngleY, 0f);

            // Apply camera offset: pivot+child (Sample style) vs single transform
            if (_cameraTransform != transform)
            {
                transform.position = _newPosition;
                _cameraTransform.localPosition = new Vector3(cameraHorizontalOffset, cameraHeightOffset, cameraDistance * -1f);
                _cameraTransform.localEulerAngles = new Vector3(cameraTiltOffset, 0f, 0f);
            }
            else
            {
                Vector3 offset = Quaternion.Euler(_newAngleX, _newAngleY, 0f) * new Vector3(cameraHorizontalOffset, cameraHeightOffset, -cameraDistance);
                transform.position = _newPosition + offset;
            }

            _lastPosition = transform.position;
            _lastAngleX = _newAngleX;
            _lastAngleY = _newAngleY;
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
                transform.position = focusPos + Quaternion.Euler(_newAngleX, _newAngleY, 0f) * new Vector3(cameraHorizontalOffset, cameraHeightOffset, -cameraDistance);
                transform.eulerAngles = new Vector3(_newAngleX, _newAngleY, 0f);
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
