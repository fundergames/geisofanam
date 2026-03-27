using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Boss health bar UI with phase threshold markers and immunity visual feedback.
    ///
    /// Setup:
    ///   - Assign a root GameObject (shown/hidden by the encounter manager).
    ///   - Assign a Slider for the health fill.
    ///   - Assign a fill Image to tint by phase/immunity state.
    ///   - Optionally assign TMP labels for boss name and current phase.
    ///   - Assign a container Transform and marker prefab to auto-spawn phase threshold markers.
    ///
    /// The component subscribes to BossController static events so it requires no direct reference.
    /// </summary>
    public class BossHealthUI : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────────

        [Header("Root")]
        [Tooltip("Parent object to show/hide for the entire boss HUD")]
        [SerializeField] private GameObject root;

        [Header("Health Bar")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image fillImage;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI phaseLabelText;
        [SerializeField] private TextMeshProUGUI phaseMessageText;

        [Header("Phase Threshold Markers")]
        [Tooltip("Parent RectTransform that spans the width of the health bar — markers are positioned within it")]
        [SerializeField] private RectTransform phaseMarkerContainer;
        [Tooltip("UI prefab (e.g. a thin vertical Image) representing a phase threshold line")]
        [SerializeField] private GameObject phaseMarkerPrefab;

        [Header("Immunity Prompt")]
        [Tooltip("Object to show while boss is immune (e.g. 'Enter Soul Realm!' text)")]
        [SerializeField] private GameObject immunityPromptObject;

        [Header("Phase Colors")]
        [SerializeField] private Color phase1Color = new Color(0.85f, 0.15f, 0.15f);
        [SerializeField] private Color phase2Color = new Color(0.9f, 0.5f, 0.1f);
        [SerializeField] private Color phase3Color = new Color(0.65f, 0.1f, 0.75f);
        [SerializeField] private Color immuneColor = new Color(0.35f, 0.75f, 1f);

        [Header("Message Display")]
        [Tooltip("How long phase transition messages remain visible (seconds)")]
        [SerializeField] private float messageDisplayDuration = 3f;

        // ── Runtime state ──────────────────────────────────────────────────────────

        private Color _activePhaseColor;
        private Coroutine _messageCoroutine;

        // ── Unity lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _activePhaseColor = phase1Color;
        }

        private void OnEnable()
        {
            // Generic boss (BossController)
            BossController.OnHealthChanged          += HandleHealthChanged;
            BossController.OnPhaseChanged           += HandlePhaseChanged;
            BossController.OnImmunityChanged        += HandleImmunityChanged;
            BossController.OnPhaseTransitionMessage += HandlePhaseTransitionMessage;

            // Giant boss (GiantBossController)
            GiantBossController.OnSoulsChanged  += HandleHealthChanged;   // same (float,float) signature
            GiantBossController.OnPhaseChanged  += HandlePhaseChanged;
            GiantBossController.OnPhaseMessage  += HandlePhaseTransitionMessage;
        }

        private void OnDisable()
        {
            BossController.OnHealthChanged          -= HandleHealthChanged;
            BossController.OnPhaseChanged           -= HandlePhaseChanged;
            BossController.OnImmunityChanged        -= HandleImmunityChanged;
            BossController.OnPhaseTransitionMessage -= HandlePhaseTransitionMessage;

            GiantBossController.OnSoulsChanged  -= HandleHealthChanged;
            GiantBossController.OnPhaseChanged  -= HandlePhaseChanged;
            GiantBossController.OnPhaseMessage  -= HandlePhaseTransitionMessage;
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>Show the boss HUD. Called by BossEncounterManager on encounter start.</summary>
        public void Show()
        {
            if (root != null)
                root.SetActive(true);
        }

        /// <summary>Hide the boss HUD. Called by BossEncounterManager on encounter end.</summary>
        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        /// <summary>
        /// Populate the boss name/title labels and spawn phase threshold markers.
        /// Call after assigning BossDefinition but before starting the encounter.
        /// </summary>
        public void Initialise(BossDefinition def)
        {
            if (def == null) return;

            if (bossNameText != null)
                bossNameText.text = def.bossName;

            if (titleText != null)
                titleText.text = def.title;

            SpawnPhaseMarkers(def);
        }

        // ── Event handlers ─────────────────────────────────────────────────────────

        private void HandleHealthChanged(float current, float max)
        {
            if (healthSlider != null)
                healthSlider.value = max > 0f ? current / max : 0f;
        }

        private void HandlePhaseChanged(int phase)
        {
            _activePhaseColor = phase switch
            {
                2 => phase2Color,
                3 => phase3Color,
                _ => phase1Color
            };

            if (fillImage != null)
                fillImage.color = _activePhaseColor;

            if (phaseLabelText != null)
                phaseLabelText.text = $"Phase {phase}";
        }

        private void HandleImmunityChanged(bool immune)
        {
            if (fillImage != null)
                fillImage.color = immune ? immuneColor : _activePhaseColor;

            if (immunityPromptObject != null)
                immunityPromptObject.SetActive(immune);
        }

        private void HandlePhaseTransitionMessage(string message)
        {
            if (phaseMessageText == null || string.IsNullOrEmpty(message)) return;

            if (_messageCoroutine != null)
                StopCoroutine(_messageCoroutine);

            _messageCoroutine = StartCoroutine(ShowMessageRoutine(message));
        }

        // ── Phase markers ──────────────────────────────────────────────────────────

        private void SpawnPhaseMarkers(BossDefinition def)
        {
            if (phaseMarkerContainer == null || phaseMarkerPrefab == null || def.phases == null) return;

            // Clear any existing markers
            foreach (Transform child in phaseMarkerContainer)
                Destroy(child.gameObject);

            float containerWidth = phaseMarkerContainer.rect.width;

            foreach (var phase in def.phases)
            {
                // Phase 1 starts at full HP — no marker needed for the very start
                if (phase.hpThresholdPercent >= 1f) continue;

                var marker = Instantiate(phaseMarkerPrefab, phaseMarkerContainer);
                var rt = marker.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // Position marker at the correct fraction along the bar
                    rt.anchorMin = new Vector2(phase.hpThresholdPercent, 0f);
                    rt.anchorMax = new Vector2(phase.hpThresholdPercent, 1f);
                    rt.anchoredPosition = Vector2.zero;
                }
            }
        }

        // ── Message coroutine ──────────────────────────────────────────────────────

        private System.Collections.IEnumerator ShowMessageRoutine(string message)
        {
            phaseMessageText.text = message;
            phaseMessageText.gameObject.SetActive(true);

            yield return new WaitForSeconds(messageDisplayDuration);

            phaseMessageText.gameObject.SetActive(false);
            _messageCoroutine = null;
        }
    }
}
