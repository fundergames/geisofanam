using System;
using System.Collections;
using RogueDeal.Combat;
using UnityEngine;
using UnityEngine.Events;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Orchestrates the boss encounter lifecycle: starting the fight, tracking win/lose conditions,
    /// and broadcasting encounter results for downstream systems (rewards, scene transitions, music).
    ///
    /// Place in the boss arena scene alongside BossController and BossHealthUI.
    /// Call StartEncounter() from a trigger volume, cutscene, or level script.
    /// </summary>
    public class BossEncounterManager : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────────

        [Header("References")]
        [Tooltip("The BossController driving the fight. Auto-located in children if null.")]
        [SerializeField] private BossController bossController;
        [Tooltip("The player's CombatEntity. Auto-located in scene if null.")]
        [SerializeField] private CombatEntity playerEntity;
        [Tooltip("Boss health bar UI. Auto-located in scene if null.")]
        [SerializeField] private BossHealthUI bossHealthUI;

        [Header("Encounter Settings")]
        [Tooltip("If true, the encounter begins automatically when the scene starts (useful for testing)")]
        [SerializeField] private bool autoStartOnAwake;
        [Tooltip("Seconds to wait after boss death before firing OnEncounterEnded (for death animation)")]
        [SerializeField] private float defeatSequenceDuration = 3f;

        [Header("Unity Events")]
        [Tooltip("Invoked when the encounter starts")]
        public UnityEvent onEncounterStarted;
        [Tooltip("Invoked when the player wins (boss defeated)")]
        public UnityEvent onPlayerWon;
        [Tooltip("Invoked when the player loses (player died during encounter)")]
        public UnityEvent onPlayerLost;

        // ── Static events ──────────────────────────────────────────────────────────

        /// <summary>Fired globally when the encounter begins.</summary>
        public static event Action OnEncounterStarted;

        /// <summary>Fired globally when the encounter ends. Argument: true = player won.</summary>
        public static event Action<bool> OnEncounterEnded;

        // ── State ──────────────────────────────────────────────────────────────────

        private bool _encounterActive;

        // ── Unity lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            if (bossController == null)
                bossController = GetComponentInChildren<BossController>(true);

            if (bossHealthUI == null)
                bossHealthUI = FindFirstObjectByType<BossHealthUI>();
        }

        private void OnEnable()
        {
            BossController.OnBossDefeated += HandleBossDefeated;
            CombatEvents.OnDamageApplied += HandleDamageApplied;
        }

        private void OnDisable()
        {
            BossController.OnBossDefeated -= HandleBossDefeated;
            CombatEvents.OnDamageApplied -= HandleDamageApplied;
        }

        private void Start()
        {
            if (autoStartOnAwake)
                StartEncounter();
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Begins the boss encounter. Safe to call from trigger volumes or cutscene scripts.
        /// </summary>
        public void StartEncounter()
        {
            if (_encounterActive) return;

            if (bossController == null)
            {
                Debug.LogError("[BossEncounterManager] No BossController found — cannot start encounter.");
                return;
            }

            // Resolve player if not already set
            if (playerEntity == null)
                playerEntity = FindFirstObjectByType<CombatEntity>();

            _encounterActive = true;

            bossController.StartEncounter(playerEntity);
            bossHealthUI?.Show();

            OnEncounterStarted?.Invoke();
            onEncounterStarted?.Invoke();

            Debug.Log("[BossEncounterManager] Boss encounter started.");
        }

        // ── Event handlers ─────────────────────────────────────────────────────────

        private void HandleBossDefeated()
        {
            if (!_encounterActive) return;

            Debug.Log("[BossEncounterManager] Boss defeated — encounter ending.");
            StartCoroutine(EndEncounterRoutine(playerWon: true));
        }

        private void HandleDamageApplied(CombatEventData data)
        {
            if (!_encounterActive || playerEntity == null) return;
            if (data.target != playerEntity) return;

            var playerData = playerEntity.GetEntityData();
            if (playerData != null && !playerData.IsAlive)
            {
                Debug.Log("[BossEncounterManager] Player died — encounter ending.");
                StartCoroutine(EndEncounterRoutine(playerWon: false));
            }
        }

        private IEnumerator EndEncounterRoutine(bool playerWon)
        {
            _encounterActive = false;

            yield return new WaitForSeconds(defeatSequenceDuration);

            bossHealthUI?.Hide();

            OnEncounterEnded?.Invoke(playerWon);

            if (playerWon)
                onPlayerWon?.Invoke();
            else
                onPlayerLost?.Invoke();

            Debug.Log($"[BossEncounterManager] Encounter ended. Player won: {playerWon}");
        }

        // ── Convenience property ───────────────────────────────────────────────────

        public bool EncounterActive => _encounterActive;
    }
}
