using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Optional debug overlay to verify if the Input System sees the gamepad and what it reports.
    /// Add to any GameObject and enable to see device status and raw stick/button values on screen.
    /// </summary>
    public class InputSystemGamepadDebug : MonoBehaviour
    {
        [SerializeField] private bool showDebug = true;
        [SerializeField] private Key toggleKey = Key.F2;
        [SerializeField] private int fontSize = 14;
        [SerializeField] private int offsetX = 10;
        [SerializeField] private int offsetY = 10;

        private GUIStyle _style;
        private bool _styleInitialized;

        private void OnGUI()
        {
            if (!showDebug) return;

            if (!_styleInitialized)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    fontSize = fontSize,
                    alignment = TextAnchor.UpperLeft,
                    padding = new RectOffset(8, 8, 8, 8),
                    normal = { textColor = Color.white },
                    wordWrap = false
                };
                _styleInitialized = true;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var gamepadCurrent = Gamepad.current;
            int gamepadCount = Gamepad.all.Count;
            var gamepad = gamepadCurrent ?? (gamepadCount > 0 ? Gamepad.all[0] : null);
            var joystickCurrent = Joystick.current;
            int joystickCount = Joystick.all.Count;
            var joystick = joystickCurrent ?? (joystickCount > 0 ? Joystick.all[0] : null);

            string deviceName = "—";
            Vector2 leftStick = Vector2.zero;
            Vector2 rightStick = Vector2.zero;
            float leftTrigger = 0f, rightTrigger = 0f;
            if (gamepad != null)
            {
                deviceName = gamepad.displayName + " (Gamepad)";
                leftStick = gamepad.leftStick.ReadValue();
                rightStick = gamepad.rightStick.ReadValue();
                leftTrigger = gamepad.leftTrigger.ReadValue();
                rightTrigger = gamepad.rightTrigger.ReadValue();
            }
            else if (joystick != null)
            {
                deviceName = joystick.displayName + " (Joystick)";
                leftStick = joystick.stick.ReadValue();
                rightTrigger = joystick.trigger.ReadValue();
                var stick2 = joystick.TryGetChildControl<StickControl>("stick2") ?? joystick.TryGetChildControl<StickControl>("secondary2DVector");
                if (stick2 != null) rightStick = stick2.ReadValue();
            }

            var allDevices = InputSystem.devices;
            var deviceList = new System.Text.StringBuilder();
            for (int i = 0; i < allDevices.Count; i++)
            {
                var d = allDevices[i];
                string typeName = d.GetType().Name;
                deviceList.AppendLine($"{i}: {d.displayName} [{typeName}]");
            }

            string text = "Input System – All devices\n" +
                          $"Keyboard:  {(keyboard != null ? "yes" : "no")}\n" +
                          $"Mouse:     {(mouse != null ? "yes" : "no")}\n" +
                          $"Gamepad:   current={(gamepadCurrent != null)}  all={gamepadCount}\n" +
                          $"Joystick:  current={(joystickCurrent != null)}  all={joystickCount}\n" +
                          $"Total devices: {allDevices.Count}\n" +
                          "--- Devices ---\n" + deviceList.ToString() +
                          "---\n" +
                          $"Using:     {deviceName}\n" +
                          $"Active:    {(ActiveInputScheme.UsingGamepad ? "controller" : "keyboard/mouse")}\n" +
                          $"L stick:   ({leftStick.x:F2}, {leftStick.y:F2})\n" +
                          $"R stick:   ({rightStick.x:F2}, {rightStick.y:F2})\n" +
                          $"L trigger: {leftTrigger:F2}  R trigger: {rightTrigger:F2}\n" +
                          $"{(toggleKey != Key.None ? $"[{toggleKey}] toggle" : "")}";

            float w = 320f;
            float h = _style.CalcHeight(new GUIContent(text), w) + 16;
            GUI.Box(new Rect(offsetX, offsetY, w, h), text, _style);
        }

        private void Update()
        {
            if (toggleKey != Key.None && Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
                showDebug = !showDebug;
        }
    }
}
