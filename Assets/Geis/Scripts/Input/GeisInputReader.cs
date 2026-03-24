// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// Sample scripts are included only as examples and are not intended as production-ready.

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Geis.InputSystem
{
    public class GeisInputReader : MonoBehaviour, GeisControls.IPlayerActions
    {
        public Vector2 _mouseDelta;
        public Vector2 _moveComposite;

        public float _movementInputDuration;
        public bool _movementInputDetected;

        private GeisControls _controls;

        /// <summary>Player/SoulRealm (Tab, gamepad LB).</summary>
        public InputAction SoulRealm => _controls != null ? _controls.Player.SoulRealm : null;

        public Action onAimActivated;
        public Action onAimDeactivated;

        public Action onCrouchActivated;
        public Action onCrouchDeactivated;

        public Action onJumpPerformed;

        public Action onLockOnToggled;

        public Action onSprintActivated;
        public Action onSprintDeactivated;

        /// <summary>Shift (and similar hold bindings): sprint while held. L3 toggles sprint on/off.</summary>
        private bool _shiftSprintHeld;

        private bool _l3SprintToggledOn;
        private bool _sprintStateEmitted;

        public Action onWalkToggled;

        public Action onLightAttackPerformed;
        public Action onHeavyAttackPerformed;
        public Action onDodgePerformed;

        private int _lastDodgeFrame = -1;

        [Header("Debug")]
        [Tooltip("Logs dodge-related input: raw gamepad east, Dodge action, and every OnDodge callback (Console).")]
        [SerializeField] private bool _debugDodgeInput;

        [Header("Gamepad dodge")]
        [Tooltip(
            "If the Xbox/PS east face button registers in hardware but Player/Dodge does not (broken binding cache), fire dodge from raw <Gamepad>/buttonEast.")]
        [SerializeField] private bool _gamepadEastDodgeFallback = true;

        /// <inheritdoc cref="OnEnable" />
        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new GeisControls();
                _controls.Player.SetCallbacks(this);
            }

            if (Application.isFocused)
                _controls.Player.Enable();
            else
                _controls.Player.Disable();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_controls == null) return;

            if (hasFocus)
                _controls.Player.Enable();
            else
            {
                _controls.Player.Disable();
                _mouseDelta = Vector2.zero;
                _moveComposite = Vector2.zero;
                _movementInputDetected = false;
            }
        }

        /// <inheritdoc cref="OnDisable" />
        public void OnDisable()
        {
            _controls?.Player.Disable();
        }

        private void Update()
        {
            if (_controls == null)
                return;

            var gp = Gamepad.current;
            if (gp != null && gp.buttonEast.wasPressedThisFrame)
            {
                if (_debugDodgeInput)
                {
                    Debug.Log(
                        $"[GeisInputReader] Raw hardware <Gamepad>/buttonEast (east) pressed — device={gp.displayName} id={gp.deviceId}",
                        this);
                }

                var dodge = _controls.Player.Dodge;
                bool actionFired = dodge.WasPressedThisFrame();
                if (_debugDodgeInput && actionFired)
                {
                    Debug.Log(
                        $"[GeisInputReader] Player/Dodge action WasPressedThisFrame — enabled={dodge.enabled} mapsEnabled={dodge.actionMap?.enabled}",
                        this);
                }

                // Hardware sees east, but the InputAction did not bind this frame — still route dodge.
                if (_gamepadEastDodgeFallback && !actionFired)
                {
                    if (_debugDodgeInput)
                    {
                        Debug.Log(
                            "[GeisInputReader] Player/Dodge did not fire; invoking dodge from raw buttonEast fallback.",
                            this);
                    }

                    TryInvokeDodgeOnce();
                }
            }
        }

        private void TryInvokeDodgeOnce()
        {
            if (Time.frameCount == _lastDodgeFrame)
                return;
            _lastDodgeFrame = Time.frameCount;
            onDodgePerformed?.Invoke();
        }

        /// <summary>True on the frame SoulRealm was pressed (enter detection).</summary>
        public bool SoulRealmWasPressedThisFrame()
        {
            return _controls != null && _controls.Player.SoulRealm.WasPressedThisFrame();
        }

        /// <summary>Shift sprint held or L3 sprint toggled on (matches player sprint).</summary>
        public bool IsSprintHeldOrToggled => _shiftSprintHeld || _l3SprintToggledOn;

        /// <summary>
        ///     Defines the action to perform when the OnLook callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnLook(InputAction.CallbackContext context)
        {
            _mouseDelta = context.ReadValue<Vector2>();
        }

        /// <summary>
        ///     Defines the action to perform when the OnMove callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnMove(InputAction.CallbackContext context)
        {
            _moveComposite = context.ReadValue<Vector2>();
            _movementInputDetected = _moveComposite.magnitude > 0;
        }

        /// <summary>
        ///     Defines the action to perform when the OnJump callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnJump(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            onJumpPerformed?.Invoke();
        }

        /// <summary>
        ///     Defines the action to perform when the OnToggleWalk callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnToggleWalk(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            onWalkToggled?.Invoke();
        }

        /// <summary>
        ///     Defines the action to perform when the OnSprint callback is called.
        ///     Gamepad L3 (left stick press) toggles sprint; keyboard Shift remains hold-to-sprint.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnSprint(InputAction.CallbackContext context)
        {
            var path = context.control?.path ?? string.Empty;
            bool fromL3 = path.Contains("leftStickPress", StringComparison.OrdinalIgnoreCase);

            if (fromL3)
            {
                if (!context.performed)
                    return;

                _l3SprintToggledOn = !_l3SprintToggledOn;
                SyncSprintOutput();
                return;
            }

            if (context.started)
                _shiftSprintHeld = true;
            else if (context.canceled)
                _shiftSprintHeld = false;
            else
                return;

            SyncSprintOutput();
        }

        private void SyncSprintOutput()
        {
            bool wantSprint = _shiftSprintHeld || _l3SprintToggledOn;
            if (wantSprint == _sprintStateEmitted)
                return;

            _sprintStateEmitted = wantSprint;
            if (wantSprint)
                onSprintActivated?.Invoke();
            else
                onSprintDeactivated?.Invoke();
        }

        /// <summary>
        ///     Defines the action to perform when the OnCrouch callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                onCrouchActivated?.Invoke();
            }
            else if (context.canceled)
            {
                onCrouchDeactivated?.Invoke();
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnAim callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnAim(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                onAimActivated?.Invoke();
            }

            if (context.canceled)
            {
                onAimDeactivated?.Invoke();
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnLockOn callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnLockOn(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            onLockOnToggled?.Invoke();
            _l3SprintToggledOn = false;
            _shiftSprintHeld = false;
            SyncSprintOutput();
        }

        /// <summary>
        ///     Defines the action to perform when the OnLightAttack callback is called.
        /// </summary>
        public void OnLightAttack(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            onLightAttackPerformed?.Invoke();
        }

        /// <summary>
        ///     Defines the action to perform when the OnHeavyAttack callback is called.
        /// </summary>
        public void OnHeavyAttack(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            onHeavyAttackPerformed?.Invoke();
        }

        /// <summary>
        ///     Defines the action to perform when the OnDodge callback is called.
        /// </summary>
        public void OnDodge(InputAction.CallbackContext context)
        {
            if (_debugDodgeInput)
            {
                Debug.Log(
                    $"[GeisInputReader] OnDodge callback — phase={context.phase} started={context.started} performed={context.performed} canceled={context.canceled} control={context.control?.path} device={context.control?.device?.displayName}",
                    this);
            }

            // Accept started or performed (devices vary); dedupe when both fire same frame.
            if (!context.started && !context.performed) return;
            TryInvokeDodgeOnce();
        }

        /// <inheritdoc />
        public void OnPause(InputAction.CallbackContext context)
        {
        }

        /// <inheritdoc />
        public void OnSoulRealm(InputAction.CallbackContext context)
        {
        }
    }
}
