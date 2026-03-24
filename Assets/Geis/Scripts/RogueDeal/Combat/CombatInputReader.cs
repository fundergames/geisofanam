using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Reads keyboard, mouse, and gamepad; switches active device by last input;
    /// exposes a single <see cref="CombatInputState"/> for camera and combat to consume.
    /// Runs early in Update so consumers get this frame's input.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class CombatInputReader : MonoBehaviour, ICombatInputProvider
    {
        [Header("Look (camera)")]
        [SerializeField] private float mouseLookSensitivity = 0.25f;
        [SerializeField] private float gamepadLookSensitivity = 2.5f;
        [SerializeField] private float gamepadLookDeadzone = 0.125f;
        [Header("Actions")]
        [Tooltip("Enable dodge input (RogueDeal Polygon combat path; Geis uses Synty InputReader).")]
        [SerializeField] private bool dodgeEnabled = false;
        [Header("Debug")]
        [Tooltip("Log all input devices and their controls once at startup. Use if controller is not detected.")]
        [SerializeField] private bool logAllDevicesOnce = false;

        private CombatInputState _state;
        private Vector2 _lastMousePosition;
        private bool _mousePositionInitialized;
        private InputDevice _fallbackStickDevice;
        private Vector2Control _fallbackStick;
        private Vector2Control _fallbackStick2;
        private ButtonControl _fallbackTrigger;
        private bool _loggedDevices;

        public CombatInputState GetState() => _state;

        private void Update()
        {
            if (logAllDevicesOnce && !_loggedDevices)
            {
                LogAllDevicesAndControls();
                _loggedDevices = true;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            // Gamepad.current is often null until the system assigns a "current" device; use any connected gamepad as fallback
            var gamepad = Gamepad.current;
            if (gamepad == null && Gamepad.all.Count > 0)
                gamepad = Gamepad.all[0];
            // Generic / HID gamepads are often reported as Joystick, not Gamepad
            var joystick = Joystick.current;
            if (joystick == null && Joystick.all.Count > 0)
                joystick = Joystick.all[0];

            // Fallback: any other device with stick+trigger (e.g. HID "Generic Gamepad" not classified as Gamepad/Joystick)
            if (gamepad == null && joystick == null)
                ResolveFallbackStickDevice();
            else
            {
                _fallbackStickDevice = null;
                _fallbackStick = null;
                _fallbackStick2 = null;
                _fallbackTrigger = null;
            }

            // --- Movement & actions (keyboard/mouse) ---
            Vector2 kbMove = Vector2.zero;
            bool kbRun = false, kbDodge = false, kbJump = false, kbAttack = false, kbLockOn = false, kbCrouch = false;
            Vector2 attackClickPos = Vector2.zero;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed) kbMove.y += 1f;
                if (keyboard.sKey.isPressed) kbMove.y -= 1f;
                if (keyboard.aKey.isPressed) kbMove.x -= 1f;
                if (keyboard.dKey.isPressed) kbMove.x += 1f;
                kbRun = keyboard.leftShiftKey.isPressed;
                kbDodge = keyboard.spaceKey.wasPressedThisFrame || keyboard.leftCtrlKey.wasPressedThisFrame;
                kbJump = keyboard.spaceKey.wasPressedThisFrame;
                kbLockOn = keyboard.qKey.wasPressedThisFrame;
                kbCrouch = keyboard.cKey.wasPressedThisFrame;
            }
            if (mouse != null)
            {
                kbAttack = mouse.leftButton.wasPressedThisFrame;
                if (kbAttack)
                    attackClickPos = mouse.position.ReadValue();
            }

            // --- Movement & actions (gamepad or joystick) ---
            Vector2 gpMove = Vector2.zero;
            bool gpRun = false, gpDodge = false, gpJump = false, gpAttack = false, gpLockOn = false, gpCrouch = false;
            if (gamepad != null)
            {
                gpMove = gamepad.leftStick.ReadValue();
                gpRun = gamepad.leftStickButton.isPressed;
                gpJump = gamepad.buttonSouth.wasPressedThisFrame; // A on Xbox, X on PS
                gpDodge = gamepad.rightTrigger.wasPressedThisFrame;  // RT
                gpAttack = gamepad.buttonWest.wasPressedThisFrame; // X on Xbox, Square on PS
                gpLockOn = gamepad.rightStickButton.wasPressedThisFrame;
                gpCrouch = gamepad.buttonEast.wasPressedThisFrame; // B on Xbox, Circle on PS
            }
            else if (joystick != null)
            {
                gpMove = joystick.stick.ReadValue();
                gpAttack = joystick.trigger.wasPressedThisFrame;
                var b1 = joystick.TryGetChildControl<ButtonControl>("button1");
                var b2 = joystick.TryGetChildControl<ButtonControl>("button2");
                var b3 = joystick.TryGetChildControl<ButtonControl>("button3");
                var b4 = joystick.TryGetChildControl<ButtonControl>("button4");
                if (b1 != null) { gpDodge = gpDodge || b1.wasPressedThisFrame; gpJump = gpJump || b1.wasPressedThisFrame; }
                if (b2 != null) gpRun = gpRun || b2.isPressed;
                if (b3 != null) gpAttack = gpAttack || b3.wasPressedThisFrame;
                if (b4 != null) gpLockOn = gpLockOn || b4.wasPressedThisFrame;
            }
            else if (_fallbackStickDevice != null && _fallbackStick != null)
            {
                gpMove = _fallbackStick.ReadValue();
                if (_fallbackTrigger != null)
                {
                    gpAttack = gpAttack || _fallbackTrigger.wasPressedThisFrame;
                    gpDodge = gpDodge || _fallbackTrigger.wasPressedThisFrame;
                    gpJump = gpJump || _fallbackTrigger.wasPressedThisFrame;
                }
            }

            // --- Look (mouse delta vs gamepad right stick) ---
            Vector2 lookDelta = Vector2.zero;
            bool mouseLookUsed = false;
            bool gamepadLookUsed = false;
            if (mouse != null)
            {
                Vector2 currentPos = mouse.position.ReadValue();
                if (!_mousePositionInitialized)
                {
                    _lastMousePosition = currentPos;
                    _mousePositionInitialized = true;
                }
                else
                {
                    Vector2 delta = currentPos - _lastMousePosition;
                    _lastMousePosition = currentPos;
                    if (delta.sqrMagnitude > 0.0001f)
                    {
                        mouseLookUsed = true;
                        lookDelta = new Vector2(delta.x * mouseLookSensitivity, -delta.y * mouseLookSensitivity);
                    }
                }
            }
            if (gamepad != null)
            {
                Vector2 stick = gamepad.rightStick.ReadValue();
                if (stick.sqrMagnitude > gamepadLookDeadzone * gamepadLookDeadzone)
                {
                    gamepadLookUsed = true;
                    lookDelta = new Vector2(stick.x * gamepadLookSensitivity, stick.y * gamepadLookSensitivity);
                }
            }
            else if (joystick != null)
            {
                var stick2 = joystick.TryGetChildControl<StickControl>("stick2");
                if (stick2 == null) stick2 = joystick.TryGetChildControl<StickControl>("secondary2DVector");
                if (stick2 != null)
                {
                    Vector2 stick = stick2.ReadValue();
                    if (stick.sqrMagnitude > gamepadLookDeadzone * gamepadLookDeadzone)
                    {
                        gamepadLookUsed = true;
                        lookDelta = new Vector2(stick.x * gamepadLookSensitivity, stick.y * gamepadLookSensitivity);
                    }
                }
            }
            else if (_fallbackStick2 != null)
            {
                Vector2 stick = _fallbackStick2.ReadValue();
                if (stick.sqrMagnitude > gamepadLookDeadzone * gamepadLookDeadzone)
                {
                    gamepadLookUsed = true;
                    lookDelta = new Vector2(stick.x * gamepadLookSensitivity, stick.y * gamepadLookSensitivity);
                }
            }

            // --- Device switch (last input wins) ---
            bool kbUsed = kbMove.sqrMagnitude > 0.01f || kbRun || kbDodge || kbJump || kbAttack || kbLockOn || kbCrouch || mouseLookUsed;
            bool gpUsed = gpMove.sqrMagnitude > 0.01f || gpRun || gpDodge || gpJump || gpAttack || gpLockOn || gpCrouch || gamepadLookUsed;
            ActiveInputScheme.Update(kbUsed, gpUsed);

            // --- Fill state from active device ---
            _state = new CombatInputState();
            bool usingController = ActiveInputScheme.UsingGamepad && (gamepad != null || joystick != null || _fallbackStickDevice != null);
            if (usingController && gamepad != null)
            {
                _state.Move = gpMove;
                _state.Run = gpRun;
                _state.SprintHeld = gpRun;
                _state.CrouchPressed = gpCrouch;
                _state.LockOnPressed = gpLockOn;
                _state.DodgePressed = dodgeEnabled && gpDodge;
                _state.JumpPressed = gpJump;
                _state.AttackPressed = gpAttack;
                _state.HasAttackClickPosition = false;
                if (gamepadLookUsed)
                    _state.Look = lookDelta;
            }
            else if (usingController && joystick != null)
            {
                _state.Move = gpMove;
                _state.Run = gpRun;
                _state.SprintHeld = gpRun;
                _state.CrouchPressed = gpCrouch;
                _state.LockOnPressed = gpLockOn;
                _state.DodgePressed = dodgeEnabled && gpDodge;
                _state.JumpPressed = gpJump;
                _state.AttackPressed = gpAttack;
                _state.HasAttackClickPosition = false;
                if (gamepadLookUsed)
                    _state.Look = lookDelta;
            }
            else if (usingController && _fallbackStickDevice != null)
            {
                _state.Move = gpMove;
                _state.Run = gpRun;
                _state.SprintHeld = gpRun;
                _state.CrouchPressed = gpCrouch;
                _state.LockOnPressed = gpLockOn;
                _state.DodgePressed = dodgeEnabled && gpDodge;
                _state.JumpPressed = gpJump;
                _state.AttackPressed = gpAttack;
                _state.HasAttackClickPosition = false;
                if (gamepadLookUsed)
                    _state.Look = lookDelta;
            }
            else
            {
                _state.Move = kbMove;
                _state.Run = kbRun;
                _state.SprintHeld = kbRun;
                _state.CrouchPressed = kbCrouch;
                _state.LockOnPressed = kbLockOn;
                _state.DodgePressed = dodgeEnabled && kbDodge;
                _state.JumpPressed = kbJump;
                _state.AttackPressed = kbAttack;
                _state.HasAttackClickPosition = kbAttack;
                _state.AttackClickScreenPosition = attackClickPos;
                if (mouseLookUsed)
                    _state.Look = lookDelta;
            }
        }

        private void ResolveFallbackStickDevice()
        {
            if (_fallbackStickDevice != null && _fallbackStick != null)
                return;
            _fallbackStickDevice = null;
            _fallbackStick = null;
            _fallbackStick2 = null;
            _fallbackTrigger = null;
            var devices = InputSystem.devices;
            for (int i = 0; i < devices.Count; i++)
            {
                var d = devices[i];
                if (d is Keyboard || d is Mouse) continue;

                Vector2Control stick1 = null, stick2 = null;
                ButtonControl btn = null;
                foreach (var c in d.allControls)
                {
                    if (c.synthetic) continue;
                    if (c is Vector2Control v2)
                    {
                        if (stick1 == null)
                            stick1 = v2;
                        else if (stick2 == null && v2 != stick1)
                            stick2 = v2;
                    }
                    else if (btn == null && c is ButtonControl bc)
                        btn = bc;
                }
                if (stick1 == null)
                    continue;
                _fallbackStickDevice = d;
                _fallbackStick = stick1;
                _fallbackStick2 = stick2;
                _fallbackTrigger = btn;
                if (logAllDevicesOnce && !_loggedDevices)
                    Debug.Log($"[CombatInputReader] Using fallback device: {d.displayName} ({d.GetType().Name}) stick={stick1.name} stick2={stick2?.name} btn={btn?.name}");
                break;
            }
        }

        private void LogAllDevicesAndControls()
        {
            var devices = InputSystem.devices;
            Debug.Log($"[CombatInputReader] Total input devices: {devices.Count}");
            for (int i = 0; i < devices.Count; i++)
            {
                var d = devices[i];
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"  [{i}] {d.displayName} ({d.GetType().Name})");
                foreach (var c in d.allControls)
                {
                    if (c.synthetic) continue;
                    sb.AppendLine($"      {c.name} ({c.GetType().Name}) path={c.path}");
                }
                Debug.Log(sb.ToString());
            }
        }
    }
}
