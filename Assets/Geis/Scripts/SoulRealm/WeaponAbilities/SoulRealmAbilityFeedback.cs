using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// On-screen + optional console feedback for soul weapon ability attempts (success vs blocked per slot).
    /// Attach next to <see cref="SoulRealmWeaponAbilityController"/> or let the controller add it at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulRealmAbilityFeedback : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private float displayDurationSeconds = 2.35f;
        [SerializeField] private int fontSize = 20;
        [Tooltip("Screen-space offset from bottom (pixels).")]
        [SerializeField] private float bottomOffsetPixels = 96f;

        [Header("Colors")]
        [SerializeField] private Color slot1SuccessColor = new Color(0.45f, 0.9f, 1f, 1f);
        [SerializeField] private Color slot2SuccessColor = new Color(0.95f, 0.55f, 1f, 1f);
        [SerializeField] private Color blockedColor = new Color(1f, 0.6f, 0.35f, 1f);

        [Header("Debug")]
        [SerializeField] private bool logToConsole = true;
        [Tooltip("Short world ray in Scene/Game view when an ability successfully fires (visible even if OnGUI is under UI).")]
        [SerializeField] private bool drawActivationRayInWorld = true;
        [SerializeField] private float activationRayLength = 4f;
        [SerializeField] private float activationRayDuration = 1.75f;

        private string _line;
        private Color _color;
        private float _hideAtUnscaledTime;

        private GUIStyle _style;

        private void EnsureStyle()
        {
            if (_style != null)
                return;
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
        }

        public void ShowBlocked(string message)
        {
            _line = message;
            _color = blockedColor;
            _hideAtUnscaledTime = Time.unscaledTime + displayDurationSeconds;
            if (logToConsole)
                Debug.LogWarning($"[SoulAbility] {message}", this);
        }

        public void ShowActivated(int slotIndex, string abilityDisplayName, Vector3 worldRayOrigin, Vector3 worldRayForward)
        {
            string slot = slotIndex == 0 ? "Ability 1" : "Ability 2";
            _line = $"{slot}: {abilityDisplayName}";
            _color = slotIndex == 0 ? slot1SuccessColor : slot2SuccessColor;
            _hideAtUnscaledTime = Time.unscaledTime + displayDurationSeconds;
            if (logToConsole)
                Debug.Log($"[SoulAbility] Activated {_line}", this);

            if (drawActivationRayInWorld && worldRayForward.sqrMagnitude > 1e-6f)
            {
                Color c = slotIndex == 0 ? slot1SuccessColor : slot2SuccessColor;
                Debug.DrawRay(
                    worldRayOrigin + Vector3.up * 0.15f,
                    worldRayForward.normalized * activationRayLength,
                    c,
                    activationRayDuration);
            }
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_line) || Time.unscaledTime >= _hideAtUnscaledTime)
                return;

            EnsureStyle();
            _style.normal.textColor = _color;

            const float padW = 24f;
            float boxW = Mathf.Min(720f, Screen.width - padW * 2f);
            float boxH = Mathf.Max(52f, fontSize * 2.5f);
            var r = new Rect((Screen.width - boxW) * 0.5f, Screen.height - bottomOffsetPixels - boxH, boxW, boxH);

            var shadow = new Rect(r.x + 2f, r.y + 2f, r.width, r.height);
            var c = _style.normal.textColor;
            _style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(shadow, _line, _style);
            _style.normal.textColor = c;
            GUI.Label(r, _line, _style);
        }
    }
}
