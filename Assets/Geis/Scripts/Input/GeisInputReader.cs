// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// Sample scripts are included only as examples and are not intended as production-ready.

using System;
using Geis.InteractInput;
using Geis.SoulRealm;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Geis.InputSystem
{
    public class GeisInputReader : MonoBehaviour, GeisControls.IPlayerActions
    {
        public Vector2 _mouseDelta;
        public Vector2 _moveComposite;

        /// <summary>
        /// Read from <c>Player/Look</c> (mouse delta + gamepad right stick). Use for camera rotation in
        /// <see cref="MonoBehaviour.LateUpdate"/> so look is correct every frame — callback-driven
        /// <see cref="_mouseDelta"/> alone can miss gamepad updates when the stick value is steady.
        /// </summary>
        public Vector2 LookInput => _controls != null ? _controls.Player.Look.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>
        /// Read from <c>Player/Move</c> (WASD + gamepad left stick). Polled each frame like
        /// <see cref="LookInput"/> so locomotion stays reliable when stick callbacks do not fire every frame.
        /// </summary>
        public Vector2 MoveInput => _controls != null ? _controls.Player.Move.ReadValue<Vector2>() : Vector2.zero;

        public float _movementInputDuration;
        public bool _movementInputDetected;

        /// <summary>Latest movement tap / press / held classification from <see cref="UpdateMovementTapState"/>.</summary>
        public readonly struct MovementTapState
        {
            public readonly bool Tapped;
            public readonly bool Pressed;
            public readonly bool Held;

            public MovementTapState(bool tapped, bool pressed, bool held)
            {
                Tapped = tapped;
                Pressed = pressed;
                Held = held;
            }
        }

        /// <summary>
        /// Effective move stick for locomotion (polled each frame). Prefer over <see cref="_moveComposite"/>.
        /// </summary>
        public Vector2 EffectiveMoveComposite => _moveComposite;

        /// <summary>
        /// Updates tap/press/held classification and owns <see cref="_movementInputDuration"/>.
        /// Call once per locomotion tick with movement detected from input (after interaction freeze checks).
        /// </summary>
        public MovementTapState UpdateMovementTapState(bool movementDetected, float holdThreshold, float deltaTime)
        {
            if (movementDetected)
            {
                bool tapped = _movementInputDuration == 0f;
                bool pressed = !tapped && _movementInputDuration > 0f && _movementInputDuration < holdThreshold;
                bool held = !tapped && !pressed && _movementInputDuration >= holdThreshold;

                _movementInputDuration += deltaTime;
                return new MovementTapState(tapped, pressed, held);
            }

            _movementInputDuration = 0f;
            return new MovementTapState(false, false, false);
        }

        /// <summary>Clears movement duration (e.g. when entering attack).</summary>
        public void ResetMovementTapState() => _movementInputDuration = 0f;

        private GeisControls _controls;

        /// <summary>Player/SoulRealm (Tab, gamepad LB).</summary>
        public InputAction SoulRealm => _controls != null ? _controls.Player.SoulRealm : null;

        /// <summary>Player/LightAttack (LMB, E, gamepad RB). Poll <c>IsPressed()</c> when needed.</summary>
        public InputAction LightAttack => _controls != null ? _controls.Player.LightAttack : null;

        /// <summary>Player/Aim (RMB, gamepad LT). Bow aim zoom while held.</summary>
        public InputAction AimAction => _controls != null ? _controls.Player.Aim : null;

        /// <summary>True while aim (LT / RMB) is held.</summary>
        public bool IsAimHeld => AimAction != null && AimAction.IsPressed();

        /// <summary>Player/HeavyAttack (R, gamepad RT). Bow draw/release polls this action.</summary>
        public InputAction HeavyAttack => _controls != null ? _controls.Player.HeavyAttack : null;

        /// <summary>Player/Jump (Space, gamepad South).</summary>
        public InputAction JumpAction => _controls != null ? _controls.Player.Jump : null;

        /// <summary>Player/Crouch (C / Ctrl, gamepad right stick press).</summary>
        public InputAction CrouchAction => _controls != null ? _controls.Player.Crouch : null;

        public Action onAimActivated;
        public Action onAimDeactivated;

        public Action onCrouchActivated;
        public Action onCrouchDeactivated;

        public Action onJumpPerformed;

        public Action onLockOnToggled;
        public Action onLockOnCycleLeft;
        public Action onLockOnCycleRight;

        public Action onSprintActivated;
        public Action onSprintDeactivated;

        /// <summary>Shift hold = sprint while held. L3 toggles jog (off) vs run/sprint (on).</summary>
        private bool _shiftSprintHeld;

        private bool _l3SprintToggledOn;
        private bool _sprintStateEmitted;

        public Action onWalkToggled;

        public Action onLightAttackPerformed;
        public Action onLightAttackStarted;
        public Action onLightAttackReleased;
        public Action onHeavyAttackPerformed;
        /// <summary>Fires when the heavy-attack button is first pressed (started phase). Used by bow charge.</summary>
        public Action onHeavyAttackStarted;
        /// <summary>Fires when the heavy-attack button is released (canceled phase). Used by bow charge-release.</summary>
        public Action onHeavyAttackReleased;
        public Action onDodgePerformed;

        private int _lastDodgeFrame = -1;
        /// <summary>Collapses Started+Performed from the same gamepad press (they often land on different frames).</summary>
        private float _lastDodgeInvokeAtUnscaled = -1f;
        private const float DodgePressDedupeSeconds = 0.05f;

        [Header("Debug")]
        [Tooltip("Logs dodge-related input: raw gamepad east, Dodge action, and every OnDodge callback (Console).")]
        [SerializeField] private bool _debugDodgeInput;

        [Tooltip("Logs when Interact is pressed via gamepad West (X / Square). Uses GeisInteractInput.WasGamepadWestInteractPressedThisFrame.")]
        [SerializeField] private bool _debugInteractWest;

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

            GeisInteractInput.SetInteractAction(_controls.Player.Interact);
            GeisInteractInput.SetMoveAction(_controls.Player.Move);
            GeisInteractInput.SoulRealmIsActiveProvider = () =>
                SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
            GeisInteractInput.InteractionWorldPositionProvider = () =>
            {
                if (SoulRealmManager.Instance != null)
                    return SoulRealmManager.Instance.GetInteractionProximityWorldPosition();
                var go = GameObject.FindGameObjectWithTag("Player");
                return go != null ? go.transform.position : Vector3.zero;
            };

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
            GeisInteractInput.SetInteractAction(null);
            GeisInteractInput.SoulRealmIsActiveProvider = null;
            GeisInteractInput.InteractionWorldPositionProvider = null;
            _controls?.Player.Disable();
        }

        private void Update()
        {
            if (_controls == null)
                return;

            _moveComposite = MoveInput;
            _movementInputDetected = _moveComposite.sqrMagnitude > 0.0001f;

            var gp = Gamepad.current;
            if (gp == null && Gamepad.all.Count > 0)
                gp = Gamepad.all[0];
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

            if (gp != null)
            {
                if (gp.dpad.left.wasPressedThisFrame)
                    onLockOnCycleLeft?.Invoke();

                if (gp.dpad.right.wasPressedThisFrame)
                    onLockOnCycleRight?.Invoke();
            }

            if (_debugInteractWest && GeisInteractInput.WasGamepadWestInteractPressedThisFrame())
            {
                var w = Gamepad.current;
                Debug.Log(
                    $"[GeisInputReader] Interact: gamepad West pressed (X/Square) — device={w?.displayName ?? "none"} id={w?.deviceId ?? -1}",
                    this);
            }
        }

        private void TryInvokeDodgeOnce()
        {
            float now = Time.unscaledTime;
            if (_lastDodgeInvokeAtUnscaled >= 0f && now - _lastDodgeInvokeAtUnscaled < DodgePressDedupeSeconds)
                return;
            if (Time.frameCount == _lastDodgeFrame)
                return;
            _lastDodgeFrame = Time.frameCount;
            _lastDodgeInvokeAtUnscaled = now;
            onDodgePerformed?.Invoke();
        }

        /// <summary>True on the frame SoulRealm was pressed (enter detection). Suppressed while aim (LT) is held so LT+LB routes to ability 1.</summary>
        public bool SoulRealmWasPressedThisFrame()
        {
            if (_controls == null || IsAimHeld)
                return false;
            return _controls.Player.SoulRealm.WasPressedThisFrame();
        }

        /// <summary>Shift held or L3 toggled to the faster gait (sprint speed / “run”).</summary>
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
        ///     Player/ToggleWalk — no bindings (walk toggle disabled for now).
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
        ///     Player/Sprint: keyboard Shift = hold to run (sprint speed). Gamepad L3 (left stick press) toggles
        ///     jog (run speed) vs run (sprint speed); same in physical realm and soul realm ghost.
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
            if (context.started)
                onLightAttackStarted?.Invoke();
            else if (context.canceled)
                onLightAttackReleased?.Invoke();

            if (!context.performed) return;
            if (ShouldSuppressGamepadLightAttackForAbilityModifier(context))
                return;
            onLightAttackPerformed?.Invoke();
        }

        /// <summary>LT+RB is ability 2; do not also fire a light attack on that press.</summary>
        private bool ShouldSuppressGamepadLightAttackForAbilityModifier(InputAction.CallbackContext context)
        {
            if (!IsAimHeld)
                return false;

            string path = context.control?.path ?? string.Empty;
            return path.Contains("rightShoulder", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Defines the action to perform when the OnHeavyAttack callback is called.
        /// </summary>
        public void OnHeavyAttack(InputAction.CallbackContext context)
        {
            if (context.started)
                onHeavyAttackStarted?.Invoke();
            else if (context.canceled)
                onHeavyAttackReleased?.Invoke();

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

            // Gamepad: Started and Performed often fire on different frames for one physical press.
            // Only Started counts as a dodge press so double-tap roll is a real second button-down.
            if (!context.started) return;
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

        /// <inheritdoc />
        public void OnInteract(InputAction.CallbackContext context)
        {
        }
    }
}
