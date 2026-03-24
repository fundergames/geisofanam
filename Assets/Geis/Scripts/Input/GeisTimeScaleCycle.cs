using UnityEngine;
using UnityEngine.InputSystem;

namespace Geis.InputSystem
{
    /// <summary>
    /// Cycles <see cref="Time.timeScale"/> on Player/Pause (gamepad Select, keyboard P from GeisControls).
    /// Order: 1/10 → 1/25 → 1 → repeat. Does not enable the rest of GeisControls (only the Pause action).
    /// </summary>
    public class GeisTimeScaleCycle : MonoBehaviour
    {
        private static readonly float[] Scales = { 0.1f, 1f / 25f, 1f };

        private GeisControls _controls;
        private int _nextScaleIndex;

        [Tooltip("Keep physics step length proportional to time scale (recommended for slow-mo).")]
        [SerializeField]
        private bool syncFixedDeltaTime = true;

        [Tooltip("Fixed timestep at timeScale 1 (Unity default is 0.02).")]
        [SerializeField]
        private float baseFixedDeltaTime = 0.02f;

        [Tooltip("Log time scale changes to the console.")]
        [SerializeField]
        private bool debugLog;

        private void OnEnable()
        {
            _controls = new GeisControls();
            var pause = _controls.Player.Pause;
            pause.Enable();
            pause.performed += OnPausePerformed;
        }

        private void OnDisable()
        {
            if (_controls != null)
            {
                _controls.Player.Pause.performed -= OnPausePerformed;
                _controls.Player.Pause.Disable();
                _controls.Dispose();
                _controls = null;
            }
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            float s = Scales[_nextScaleIndex];
            _nextScaleIndex = (_nextScaleIndex + 1) % Scales.Length;

            Time.timeScale = s;
            if (syncFixedDeltaTime)
                Time.fixedDeltaTime = baseFixedDeltaTime * Mathf.Max(s, 0.0001f);

            if (debugLog)
                Debug.Log($"[GeisTimeScaleCycle] timeScale={s} (next will be {_nextScaleIndex})", this);
        }
    }
}
