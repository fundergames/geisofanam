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

using Geis.InputSystem;
using Geis.InteractInput;
using Geis.Locomotion;
using Geis.SoulRealm;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Object Blink (Dagger Q): after ray-grabbing a <see cref="SoulBlinkable"/>, the ghost stands still
    /// while the object is moved directly with input until it enters the socket radius and snaps.
    /// Press Q again to cancel and return the object to pose A.
    /// Add this on the same hierarchy as <see cref="SoulRealmWeaponAbilityController"/> (e.g. ghost root);
    /// if missing, <see cref="DaggerObjectBlinkSoulWeaponAbility"/> adds it at runtime on the ability owner.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulBlinkManipulationController : MonoBehaviour
    {
        [SerializeField] private GeisCameraController cameraController;
        [SerializeField] private GeisInputReader inputReader;

        [Header("Translation")]
        [Tooltip("Move speed in the camera's horizontal plane while manipulating.")]
        [SerializeField] private float planarMoveSpeed = 3.5f;

        [Tooltip("Vertical move speed while Up/Down arrow or gamepad D-pad up/down is held.")]
        [SerializeField] private float verticalMoveSpeed = 2.5f;

        [Tooltip("Optional leash from the frozen soul body. Set to 0 to disable.")]
        [SerializeField] private float maxManipulationDistance;

        [Header("Rotation")]
        [Tooltip("Hold Aim / left trigger and move left/right to spin the object around world up.")]
        [SerializeField] private float yawRotationSpeed = 180f;

        [Tooltip("Hold Aim / left trigger and move forward/back to tilt the object around the camera right axis.")]
        [SerializeField] private float tiltRotationSpeed = 140f;

        [Header("VFX")]
        [Tooltip("One-shot burst when you grab (screen-center hit).")]
        [SerializeField] private Color grabBurstColor = new Color(0.35f, 0.95f, 1f, 1f);

        [Tooltip("Looping carry effect when the blinkable has no per-object carry prefab.")]
        [SerializeField] private GameObject defaultCarryLoopVfxPrefab;

        [SerializeField] private bool proceduralCarryLoopIfNoPrefab = true;

        [SerializeField] private Color proceduralCarryColor = new Color(0.4f, 0.88f, 1f, 1f);

        [Tooltip("Burst when you cancel (Q) while floating.")]
        [SerializeField] private Color releaseCancelColor = new Color(0.9f, 0.45f, 0.35f, 1f);

        [Tooltip("Burst when the object snaps into the socket.")]
        [SerializeField] private Color releaseSnapColor = new Color(0.35f, 1f, 0.75f, 1f);

        private SoulBlinkable _target;
        private Rigidbody _targetRb;
        private bool _rbWasKinematic;
        private bool _freezePushed;
        private bool _ghostFreezePushed;
        private GameObject _carryVfxInstance;
        private Vector3 _manipulatedPosition;
        private Quaternion _manipulatedRotation;

        public bool IsManipulating => _target != null;

        private void Awake()
        {
            if (cameraController == null)
                cameraController = FindFirstObjectByType<GeisCameraController>();
            if (inputReader == null)
                inputReader = GetComponentInParent<GeisInputReader>() ?? FindFirstObjectByType<GeisInputReader>();
        }

        private void OnEnable()
        {
            SoulRealmManager.SoulRealmStateChanged += OnSoulRealmChanged;
        }

        private void OnDisable()
        {
            SoulRealmManager.SoulRealmStateChanged -= OnSoulRealmChanged;
            CancelManipulationIfNeeded();
        }

        private void OnSoulRealmChanged()
        {
            if (SoulRealmManager.Instance == null || !SoulRealmManager.Instance.IsSoulRealmActive)
                CancelManipulationIfNeeded();
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            Camera cam = cameraController != null ? cameraController.MainCamera : Camera.main;
            if (cam == null)
                return;

            UpdateManipulationPose(cam, Time.deltaTime);
            _target.transform.SetPositionAndRotation(_manipulatedPosition, _manipulatedRotation);

            SoulBlinkSocket socket = _target.SocketB;
            if (socket != null && socket.IsWithinSnapRange(_target.transform.position))
            {
                _target.SnapIntoSocketFromRaycast(socket);
                EndManipulationInternal(false);
            }
        }

        private void UpdateManipulationPose(Camera cam, float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            Vector2 moveInput = inputReader != null ? inputReader._moveComposite : Vector2.zero;
            bool rotationMode = inputReader != null
                && inputReader.AimAction != null
                && inputReader.AimAction.IsPressed();

            Vector3 cameraForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (cameraForward.sqrMagnitude < 0.0001f)
                cameraForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (cameraForward.sqrMagnitude < 0.0001f)
                cameraForward = Vector3.forward;
            cameraForward.Normalize();

            Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;

            if (rotationMode)
            {
                float yawDelta = moveInput.x * yawRotationSpeed * deltaTime;
                float tiltDelta = -moveInput.y * tiltRotationSpeed * deltaTime;
                if (Mathf.Abs(yawDelta) > 0.001f)
                    _manipulatedRotation = Quaternion.AngleAxis(yawDelta, Vector3.up) * _manipulatedRotation;
                if (Mathf.Abs(tiltDelta) > 0.001f)
                    _manipulatedRotation = Quaternion.AngleAxis(tiltDelta, cameraRight) * _manipulatedRotation;
            }
            else
            {
                Vector3 planarDelta = (cameraRight * moveInput.x + cameraForward * moveInput.y)
                    * (planarMoveSpeed * deltaTime);
                _manipulatedPosition += planarDelta;
            }

            float verticalInput = GetVerticalInput();
            if (Mathf.Abs(verticalInput) > 0.001f)
                _manipulatedPosition += Vector3.up * (verticalInput * verticalMoveSpeed * deltaTime);

            if (maxManipulationDistance > 0f)
            {
                Vector3 offset = _manipulatedPosition - transform.position;
                float maxDistanceSq = maxManipulationDistance * maxManipulationDistance;
                if (offset.sqrMagnitude > maxDistanceSq)
                    _manipulatedPosition = transform.position + offset.normalized * maxManipulationDistance;
            }
        }

        private static float GetVerticalInput()
        {
            float vertical = 0f;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.upArrowKey.isPressed)
                    vertical += 1f;
                if (kb.downArrowKey.isPressed)
                    vertical -= 1f;
            }

            Gamepad gp = Gamepad.current;
            if (gp == null && Gamepad.all.Count > 0)
                gp = Gamepad.all[0];
            if (gp != null)
            {
                if (gp.dpad.up.isPressed)
                    vertical += 1f;
                if (gp.dpad.down.isPressed)
                    vertical -= 1f;
            }

            return Mathf.Clamp(vertical, -1f, 1f);
        }

        /// <summary>
        /// Primary ability (Q) while manipulating: cancel and return to pose A. Returns true if consumed.
        /// </summary>
        public bool TryConsumePrimaryAbilityCancel()
        {
            if (_target == null)
                return false;
            EndManipulationInternal(true);
            return true;
        }

        public bool TryBeginManipulation(SoulBlinkable blink)
        {
            if (blink == null || _target != null)
                return false;

            _target = blink;
            _targetRb = blink.GetComponent<Rigidbody>();
            if (_targetRb != null)
            {
                _rbWasKinematic = _targetRb.isKinematic;
                _targetRb.isKinematic = true;
                _targetRb.linearVelocity = Vector3.zero;
                _targetRb.angularVelocity = Vector3.zero;
            }

            GeisInteractInput.PushInteractionMovementFreeze();
            _freezePushed = true;

            if (SoulRealmManager.Instance != null)
            {
                SoulRealmManager.Instance.PushExternalGhostMovementFreeze();
                _ghostFreezePushed = true;
            }

            _manipulatedPosition = blink.transform.position;
            _manipulatedRotation = blink.transform.rotation;

            Camera cam = cameraController != null ? cameraController.MainCamera : Camera.main;
            Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward;
            SoulAbilityProceduralBurst.Spawn(blink.transform.position, fwd, grabBurstColor);

            StartCarryVfx(blink);

            return true;
        }

        private void StartCarryVfx(SoulBlinkable blink)
        {
            StopCarryVfx();

            GameObject prefab = blink.ManipulationCarryVfxPrefab != null
                ? blink.ManipulationCarryVfxPrefab
                : defaultCarryLoopVfxPrefab;

            if (prefab != null)
            {
                _carryVfxInstance = Instantiate(prefab, blink.transform);
                _carryVfxInstance.transform.localPosition = Vector3.zero;
                _carryVfxInstance.transform.localRotation = Quaternion.identity;
                return;
            }

            if (proceduralCarryLoopIfNoPrefab)
                _carryVfxInstance = SoulBlinkManipulationProceduralCarry.Create(blink.transform, proceduralCarryColor);
        }

        private void StopCarryVfx()
        {
            if (_carryVfxInstance == null)
                return;
            Destroy(_carryVfxInstance);
            _carryVfxInstance = null;
        }

        private void EndManipulationInternal(bool cancel)
        {
            if (_target == null)
                return;

            Vector3 endPos = _target.transform.position;
            StopCarryVfx();

            if (cancel)
                _target.RestoreToPoseA();

            SoulAbilityProceduralBurst.Spawn(
                endPos,
                Vector3.up,
                cancel ? releaseCancelColor : releaseSnapColor,
                1.1f);

            if (_targetRb != null)
            {
                _targetRb.isKinematic = _rbWasKinematic;
                _targetRb = null;
            }

            if (_freezePushed)
            {
                GeisInteractInput.PopInteractionMovementFreeze();
                _freezePushed = false;
            }

            if (_ghostFreezePushed && SoulRealmManager.Instance != null)
            {
                SoulRealmManager.Instance.PopExternalGhostMovementFreeze();
                _ghostFreezePushed = false;
            }

            _target = null;
        }

        private void CancelManipulationIfNeeded()
        {
            if (_target == null)
                return;
            EndManipulationInternal(true);
        }
    }
}
