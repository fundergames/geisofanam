// Geis of Anam - Copy of Synty SampleCameraController as starting point.
// Original: Synty.AnimationBaseLocomotion.Samples.SampleCameraController

using Geis.InputSystem;
using UnityEngine;

namespace Geis.Locomotion
{
    public class GeisCameraController : MonoBehaviour
    {
        private const int _LAG_DELTA_TIME_ADJUSTMENT = 20;

        [Tooltip("The character game object")]
        [SerializeField]
        private GameObject _syntyCharacter;
        [Tooltip("Main camera used for player perspective")]
        [SerializeField]
        private Camera _mainCamera;

        [SerializeField]
        private Transform _playerTarget;
        [SerializeField]
        private Transform _lockOnTarget;

        [SerializeField]
        private bool _invertCamera;
        [SerializeField]
        private bool _hideCursor;
        [SerializeField]
        private bool _isLockedOn;
        [SerializeField]
        private float _mouseSensitivity = 2f;
        [SerializeField]
        private float _cameraDistance = 5f;
        [SerializeField]
        private float _cameraHeightOffset;
        [SerializeField]
        private float _cameraHorizontalOffset;
        [SerializeField]
        private float _cameraTiltOffset;
        [SerializeField]
        private Vector2 _cameraTiltBounds = new Vector2(-10f, 45f);
        [SerializeField]
        private float _positionalCameraLag = 1f;
        [SerializeField]
        private float _rotationalCameraLag = 0.35f;
        private float _cameraInversion;

        private GeisInputReader _inputReader;
        private float _lastAngleX;
        private float _lastAngleY;

        private Vector3 _lastPosition;

        private float _targetAngleX;
        private float _targetAngleY;
        private float _currentAngleX;
        private float _currentAngleY;
        private Vector3 _newPosition;
        private bool _wasLockedOn;

        private Transform _syntyCamera;

        // Soul realm: baseline captured at entry; rotation lerps during hold-to-exit; snap pivot/angles on exit complete.
        private bool _soulRealmBaselineCaptured;
        private bool _soulRealmExitHoldActive;
        private float _soulRealmExitHoldProgress;

        private Vector3 _soulRealmBaselinePivotPosition;
        private float _soulRealmBaselineTargetAngleX;
        private float _soulRealmBaselineTargetAngleY;
        private float _soulRealmBaselineCurrentAngleX;
        private float _soulRealmBaselineCurrentAngleY;
        private Vector3 _soulRealmBaselineLastPosition;

        private float _soulRealmHoldStartTargetAngleX;
        private float _soulRealmHoldStartTargetAngleY;
        private float _soulRealmHoldStartCurrentAngleX;
        private float _soulRealmHoldStartCurrentAngleY;

        /// <summary>
        /// Call while still following the physical body, immediately before switching follow target to the soul ghost.
        /// </summary>
        public void CaptureSoulRealmEntryState()
        {
            _soulRealmBaselinePivotPosition = transform.position;
            _soulRealmBaselineTargetAngleX = _targetAngleX;
            _soulRealmBaselineTargetAngleY = _targetAngleY;
            _soulRealmBaselineCurrentAngleX = _currentAngleX;
            _soulRealmBaselineCurrentAngleY = _currentAngleY;
            _soulRealmBaselineLastPosition = _lastPosition;
            _soulRealmBaselineCaptured = true;
        }

        /// <summary>First frame the player starts holding SoulRealm to exit — captures rotation to lerp from during the hold.</summary>
        public void BeginSoulRealmExitHoldRotationLerp()
        {
            if (!_soulRealmBaselineCaptured)
                return;

            _soulRealmHoldStartTargetAngleX = _targetAngleX;
            _soulRealmHoldStartTargetAngleY = _targetAngleY;
            _soulRealmHoldStartCurrentAngleX = _currentAngleX;
            _soulRealmHoldStartCurrentAngleY = _currentAngleY;
            _soulRealmExitHoldActive = true;
            _soulRealmExitHoldProgress = 0f;
        }

        /// <summary>0–1 progress parallel to SoulRealmManager exit hold (ghost → body).</summary>
        public void SetSoulRealmExitHoldProgress(float holdProgress01)
        {
            _soulRealmExitHoldProgress = Mathf.Clamp01(holdProgress01);
        }

        /// <summary>Released hold before completion — resume normal mouse look from current angles.</summary>
        public void EndSoulRealmExitHoldRotationLerp()
        {
            _soulRealmExitHoldActive = false;
        }

        /// <summary>
        /// Snap orbit pivot and look state to <see cref="CaptureSoulRealmEntryState"/> (call when exit hold completes).
        /// </summary>
        public void ApplySoulRealmBaselineSnapshot()
        {
            if (!_soulRealmBaselineCaptured)
                return;

            _soulRealmExitHoldActive = false;
            transform.position = _soulRealmBaselinePivotPosition;
            _targetAngleX = _soulRealmBaselineTargetAngleX;
            _targetAngleY = _soulRealmBaselineTargetAngleY;
            _currentAngleX = _soulRealmBaselineCurrentAngleX;
            _currentAngleY = _soulRealmBaselineCurrentAngleY;
            transform.eulerAngles = new Vector3(_currentAngleX, _currentAngleY, 0f);
            _lastPosition = _soulRealmBaselineLastPosition;
            _lastAngleX = _currentAngleX;
            _lastAngleY = _currentAngleY;
        }

        /// <inheritdoc cref="Start" />
        private void Start()
        {
            _syntyCamera = gameObject.transform.GetChild(0);

            _inputReader = _syntyCharacter.GetComponent<GeisInputReader>();
            if (_playerTarget == null)
                _playerTarget = _syntyCharacter.transform.Find("SyntyPlayer_LookAt");
            _lockOnTarget = _syntyCharacter.transform.Find("TargetLockOnPos");
            if (_lockOnTarget == null)
            {
                var go = new GameObject("TargetLockOnPos");
                go.transform.SetParent(_syntyCharacter.transform);
                go.transform.localPosition = Vector3.zero;
                _lockOnTarget = go.transform;
            }

            if (_hideCursor)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            _cameraInversion = _invertCamera ? 1 : -1;

            transform.position = _playerTarget.position;
            transform.rotation = _playerTarget.rotation;

            _lastPosition = transform.position;

            _targetAngleX = transform.eulerAngles.x;
            _targetAngleY = transform.eulerAngles.y;
            _currentAngleX = _targetAngleX;
            _currentAngleY = _targetAngleY;
            _lastAngleX = _currentAngleX;
            _lastAngleY = _currentAngleY;

            _syntyCamera.localPosition = new Vector3(_cameraHorizontalOffset, _cameraHeightOffset, _cameraDistance * -1);
            _syntyCamera.localEulerAngles = new Vector3(_cameraTiltOffset, 0f, 0f);
        }

        /// <summary>
        /// Switches the camera orbit pivot (e.g. body vs soul ghost vs exit lerp pivot).
        /// </summary>
        public void SetFollowTarget(Transform newPlayerTarget)
        {
            if (newPlayerTarget != null)
                _playerTarget = newPlayerTarget;
        }

        /// <summary>
        /// After switching follow target (e.g. body → ghost), snap orbit pivot and yaw/pitch to match the new look rig
        /// so the camera does not keep the previous orbit angles.
        /// </summary>
        public void SnapOrbitRotationToLookTarget(Transform lookTarget)
        {
            if (lookTarget == null)
                return;

            transform.position = lookTarget.position;
            transform.rotation = lookTarget.rotation;

            _targetAngleX = transform.eulerAngles.x;
            _targetAngleY = transform.eulerAngles.y;
            _targetAngleX = Mathf.Clamp(_targetAngleX, _cameraTiltBounds.x, _cameraTiltBounds.y);
            _currentAngleX = _targetAngleX;
            _currentAngleY = _targetAngleY;
            _lastAngleX = _currentAngleX;
            _lastAngleY = _currentAngleY;
            _lastPosition = transform.position;

            transform.eulerAngles = new Vector3(_currentAngleX, _currentAngleY, 0f);
        }

        /// <inheritdoc cref="LateUpdate" />
        private void LateUpdate()
        {
            if (_playerTarget == null)
                return;

            float positionalSharpness = 1f / Mathf.Max(_positionalCameraLag, 0.01f);
            float rotationalSharpness = 1f / Mathf.Max(_rotationalCameraLag, 0.01f);
            float posSmooth = 1f - Mathf.Exp(-positionalSharpness * Time.deltaTime);
            float rotSmooth = 1f - Mathf.Exp(-rotationalSharpness * Time.deltaTime);

            bool lockLook = _soulRealmExitHoldActive;

            if (!lockLook && _inputReader != null)
            {
                float rotationX = _inputReader._mouseDelta.y * _cameraInversion * _mouseSensitivity;
                float rotationY = _inputReader._mouseDelta.x * _mouseSensitivity;

                if (_wasLockedOn && !_isLockedOn)
                    _targetAngleY = _currentAngleY;
                _wasLockedOn = _isLockedOn;

                _targetAngleX += rotationX;
                _targetAngleX = Mathf.Clamp(_targetAngleX, _cameraTiltBounds.x, _cameraTiltBounds.y);

                if (_isLockedOn && _lockOnTarget != null)
                {
                    Vector3 aimVector = _lockOnTarget.position - _playerTarget.position;
                    Quaternion targetRotation = Quaternion.LookRotation(aimVector);
                    _targetAngleY = targetRotation.eulerAngles.y;
                    _currentAngleY = Mathf.LerpAngle(_currentAngleY, _targetAngleY, rotSmooth);
                }
                else
                {
                    _targetAngleY += rotationY;
                    _currentAngleY = Mathf.LerpAngle(_currentAngleY, _targetAngleY, rotSmooth);
                }

                _currentAngleX = Mathf.Lerp(_currentAngleX, _targetAngleX, rotSmooth);
            }
            else if (lockLook && _soulRealmBaselineCaptured)
            {
                float t = Mathf.SmoothStep(0f, 1f, _soulRealmExitHoldProgress);
                _targetAngleX = Mathf.LerpAngle(_soulRealmHoldStartTargetAngleX, _soulRealmBaselineTargetAngleX, t);
                _targetAngleY = Mathf.LerpAngle(_soulRealmHoldStartTargetAngleY, _soulRealmBaselineTargetAngleY, t);
                _currentAngleX = Mathf.LerpAngle(_soulRealmHoldStartCurrentAngleX, _soulRealmBaselineCurrentAngleX, t);
                _currentAngleY = Mathf.LerpAngle(_soulRealmHoldStartCurrentAngleY, _soulRealmBaselineCurrentAngleY, t);
                _targetAngleX = Mathf.Clamp(_targetAngleX, _cameraTiltBounds.x, _cameraTiltBounds.y);
                _currentAngleX = Mathf.Clamp(_currentAngleX, _cameraTiltBounds.x, _cameraTiltBounds.y);
            }

            _newPosition = _playerTarget.position;
            _newPosition = Vector3.Lerp(_lastPosition, _newPosition, posSmooth);

            transform.position = _newPosition;
            transform.eulerAngles = new Vector3(_currentAngleX, _currentAngleY, 0);

            _syntyCamera.localPosition = new Vector3(_cameraHorizontalOffset, _cameraHeightOffset, _cameraDistance * -1);
            _syntyCamera.localEulerAngles = new Vector3(_cameraTiltOffset, 0f, 0f);

            _lastPosition = _newPosition;
            _lastAngleX = _currentAngleX;
            _lastAngleY = _currentAngleY;
        }

        /// <summary>
        ///     Locks the camera to aim at a specified target.
        /// </summary>
        /// <param name="enable">Whether lock on is enabled or not.</param>
        /// <param name="newLockOnTarget">The target to lock on to.</param>
        public void LockOn(bool enable, Transform newLockOnTarget)
        {
            _isLockedOn = enable;

            if (newLockOnTarget != null)
            {
                _lockOnTarget = newLockOnTarget;
            }
        }

        /// <summary>
        /// Gets the position of the camera.
        /// </summary>
        /// <returns>The position of the camera.</returns>
        public Vector3 GetCameraPosition()
        {
            return _mainCamera.transform.position;
        }

        /// <summary>
        /// Gets the forward vector of the camera.
        /// </summary>
        /// <returns>The forward vector of the camera.</returns>
        public Vector3 GetCameraForward()
        {
            return _mainCamera.transform.forward;
        }

        /// <summary>
        /// Gets the forward vector of the camera with the Y value zeroed.
        /// </summary>
        /// <returns>The forward vector of the camera with the Y value zeroed.</returns>
        public Vector3 GetCameraForwardZeroedY()
        {
            return new Vector3(_mainCamera.transform.forward.x, 0, _mainCamera.transform.forward.z);
        }

        /// <summary>
        /// Gets the normalised forward vector of the camera with the Y value zeroed.
        /// </summary>
        /// <returns>The normalised forward vector of the camera with the Y value zeroed.</returns>
        public Vector3 GetCameraForwardZeroedYNormalised()
        {
            return GetCameraForwardZeroedY().normalized;
        }


        /// <summary>
        /// Gets the right vector of the camera with the Y value zeroed.
        /// </summary>
        /// <returns>The right vector of the camera with the Y value zeroed.</returns>
        public Vector3 GetCameraRightZeroedY()
        {
            return new Vector3(_mainCamera.transform.right.x, 0, _mainCamera.transform.right.z);
        }

        /// <summary>
        /// Gets the normalised right vector of the camera with the Y value zeroed.
        /// </summary>
        /// <returns>The normalised right vector of the camera with the Y value zeroed.</returns>
        public Vector3 GetCameraRightZeroedYNormalised()
        {
            return GetCameraRightZeroedY().normalized;
        }

        /// <summary>
        /// Gets the X value of the camera tilt.
        /// </summary>
        /// <returns>The X value of the camera tilt.</returns>
        public float GetCameraTiltX()
        {
            return _mainCamera.transform.eulerAngles.x;
        }
    }
}
