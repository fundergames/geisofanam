using System;
using System.Collections;
using Geis.SoulRealm;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Orchestrates the Giant Soul Warden encounter.
    ///
    /// Responsibilities:
    ///   - Drives the fist-slam loop coroutine (windup → grounded → lift).
    ///   - Detects when both hands are broken and exposes the CritSpot.
    ///   - Tracks the soul pool (boss HP); notifies UI and handles defeat.
    ///   - Manages phase transitions via the IBossPhase interface:
    ///       Each phase is configured by a <see cref="GiantBossPhaseData"/> entry (shields, slam cadence, crit window, soul threshold to advance).
    ///
    /// Slam cycle detail (per hand):
    ///   1. SetState(Slamming)  — windup animation plays.
    ///   2. SetState(Grounded or Shielded) — fist hits the ground; player has a window.
    ///      - Phase 1: Grounded immediately; player attacks the fist.
    ///      - Phase 2: Shielded; player enters Soul Realm → destroys BossPartShield →
    ///                 BossPart auto-transitions to Grounded → player exits and attacks.
    ///   3. Window expires (or fist broken) → SetState(Idle) → recovery pause → next hand.
    ///
    /// SOLID notes:
    ///   - IBossPhase (OCP/DIP): phases are swappable without touching this class.
    ///   - BossPart / CritSpot listen to CombatEvents themselves (SRP).
    ///   - All tunable numbers live in GiantBossDefinition SO (DIP / data-driven).
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public class GiantBossController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────────

        [Header("Boss Configuration")]
        [SerializeField] private GiantBossDefinition definition;

        [Header("Boss Parts")]
        [SerializeField] private BossPart rightHandPart;
        [SerializeField] private BossPart leftHandPart;
        [SerializeField] private CritSpot critSpot;

        [Header("Animation")]
        [Tooltip("Main boss animator. Right-hand triggers: SlamWindup_R / SlamLand_R / SlamRecover_R. " +
                 "Left-hand uses the same with _L suffix. Death: Die.")]
        [SerializeField] private Animator bossAnimator;

        [Header("Player Reference")]
        [Tooltip("Auto-located at encounter start if null.")]
        [SerializeField] private CombatEntity playerEntity;

        [Header("Slam impact VFX")]
        [Tooltip("Optional. VFX spawned at fist impact; BossSlamShockwaveVfx is added at runtime if missing (e.g. store-bought particle prefabs).")]
        [SerializeField] private GameObject slamShockwaveVfxPrefab;

        [Tooltip("Shockwave ring radius at the start of the expand (meters).")]
        [SerializeField] private float slamShockwaveStartRadius = 0.15f;

        [Tooltip("Shockwave radius at the end of the expand (meters). If ≤ 0, uses slamDamageRadius from GiantBossDefinition.")]
        [SerializeField] private float slamShockwaveEndRadius;

        [Tooltip("Seconds to interpolate from start radius to end radius.")]
        [SerializeField] private float slamShockwaveExpandDuration = 0.35f;

        [Tooltip("Optional delay after impact (seconds). Use this to sync VFX to the exact fist-ground contact frame.")]
        [SerializeField] private float slamShockwaveImpactDelay = 0f;

        [Tooltip("Local offset from the BossPart fist (VFX is parented to the fist transform).")]
        [SerializeField] private Vector3 slamShockwavePositionOffset;

        // ── Static events (consumed by BossHealthUI / BossEncounterManager) ────────

        /// <summary>Remaining souls changed. (remaining, total)</summary>
        public static event Action<float, float> OnSoulsChanged;

        /// <summary>Phase number changed. (1, 2, or 3)</summary>
        public static event Action<int> OnPhaseChanged;

        /// <summary>Boss defeated — all souls drained.</summary>
        public static event Action OnBossDefeated;

        /// <summary>Narrative message for the phase transition banner.</summary>
        public static event Action<string> OnPhaseMessage;

        /// <summary>Crit vulnerability window ended (timeout or encounter end). For UI/VFX.</summary>
        public static event Action OnCritWindowExpired;

        // ── Runtime state ──────────────────────────────────────────────────────────

        private float _remainingSouls;
        private int _phaseIndex = 1;
        private bool _rightHandBroken;
        private bool _leftHandBroken;
        private bool _critSpotExposed;
        private bool _encounterStarted;

        private IBossPhase _currentPhase;
        private Coroutine _slamLoopCoroutine;
        private Coroutine _critWindowCoroutine;
        private bool _phaseAdvanceLocked;

        private float _pendingCritDrainSouls;
        private float _requiredCritDrainSoulsThisWindow;

        private CombatEntity _combatEntity;

        // ── Properties (read by IBossPhase implementations) ────────────────────────

        public GiantBossDefinition Definition  => definition;
        public float RemainingSouls            => _remainingSouls;
        public float SoulPercent               => definition != null && definition.totalSouls > 0f
                                                      ? _remainingSouls / definition.totalSouls
                                                      : 0f;

        /// <summary>1 = first phase, 2 = shielded hands, 3 = final phase (if enabled).</summary>
        public int CurrentPhaseIndex => _phaseIndex;

        // ── Unity lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _combatEntity = GetComponent<CombatEntity>();

            if (bossAnimator == null)
                bossAnimator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            BossPart.OnPartBroken += HandlePartBroken;
            BossPart.OnPartReset  += HandlePartReset;
        }

        private void OnDisable()
        {
            BossPart.OnPartBroken -= HandlePartBroken;
            BossPart.OnPartReset  -= HandlePartReset;
            StopAllCoroutines();
        }

        private void Update()
        {
            if (!_encounterStarted) return;

            _currentPhase?.OnUpdate(this);

            if (_currentPhase != null && _currentPhase.IsComplete)
                AdvancePhase();
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Initialise and start the encounter. Called by BossEncounterManager.
        /// </summary>
        public void StartEncounter(CombatEntity player = null)
        {
            if (definition == null)
            {
                Debug.LogError("[GiantBossController] GiantBossDefinition not assigned.");
                return;
            }

            if (player != null)
                playerEntity = player;

            if (playerEntity == null)
                playerEntity = FindFirstObjectByType<CombatEntity>();

            _remainingSouls    = definition.totalSouls;
            _phaseIndex        = 1;
            _rightHandBroken   = false;
            _leftHandBroken    = false;
            _critSpotExposed   = false;
            _encounterStarted  = true;

            // Push definitions so parts initialise from the correct SO data
            rightHandPart?.SetDefinition(definition.rightHand);
            leftHandPart?.SetDefinition(definition.leftHand);

            OnSoulsChanged?.Invoke(_remainingSouls, definition.totalSouls);

            TransitionToPhase(new GiantBossConfiguredPhase(1));
            OnPhaseChanged?.Invoke(1);

            Debug.Log($"[GiantBossController] Encounter started: {definition.bossName}");
        }

        /// <summary>
        /// Called by IBossPhase implementations when the crit spot is hit.
        /// Crit hits accumulate progress; the boss soul pool only changes when the crit is completed.
        /// </summary>
        public void DrainSouls(float damage)
        {
            if (!_encounterStarted || definition == null)
                return;
            if (_phaseAdvanceLocked)
                return;
            if (!_critSpotExposed)
                return;

            float drainSouls = damage * definition.soulDrainPerDamagePoint;
            if (drainSouls <= 0f)
                return;

            _pendingCritDrainSouls += drainSouls;

            // Health only updates once the player has done "enough" crit damage for this phase.
            if (_pendingCritDrainSouls < _requiredCritDrainSoulsThisWindow)
                return;

            CompleteCritDrainToPhaseThreshold();
        }

        private void CompleteCritDrainToPhaseThreshold()
        {
            if (definition == null)
                return;
            var data = definition.GetPhaseData(_phaseIndex);

            float total = Mathf.Max(0.0001f, definition.totalSouls);
            float threshold = data.exitSoulPercentThreshold;
            float targetSouls = Mathf.Clamp01(threshold) * total;

            // Clamp to the phase target and advance immediately.
            _remainingSouls = Mathf.Max(0f, targetSouls);
            OnSoulsChanged?.Invoke(_remainingSouls, definition.totalSouls);

            _phaseAdvanceLocked = true;

            if (_critWindowCoroutine != null)
            {
                StopCoroutine(_critWindowCoroutine);
                _critWindowCoroutine = null;
            }

            critSpot?.SetVulnerable(false);
            _critSpotExposed = false;
            _pendingCritDrainSouls = 0f;
            _requiredCritDrainSoulsThisWindow = 0f;

            // Defeat (final crit) case: if the phase target is 0, this is the kill.
            if (_remainingSouls <= 0f && _encounterStarted)
            {
                DefeatBoss();
                _phaseAdvanceLocked = false;
                return;
            }

            ResetPartsForPhase(data.useShieldedHands);
            AdvancePhase();

            _phaseAdvanceLocked = false;
        }

        /// <summary>
        /// Start (or restart) the infinite slam-loop coroutine.
        /// Called by IBossPhase.OnEnter.
        /// </summary>
        public void StartSlamLoop()
        {
            StopSlamLoop();
            _slamLoopCoroutine = StartCoroutine(SlamLoop());
        }

        /// <summary>Stop the slam-loop coroutine. Called by IBossPhase.OnExit.</summary>
        public void StopSlamLoop()
        {
            if (_slamLoopCoroutine != null)
            {
                StopCoroutine(_slamLoopCoroutine);
                _slamLoopCoroutine = null;
            }
        }

        /// <summary>
        /// Reset parts for the next cycle.
        /// Called by IBossPhase.OnEnter and internally from ExposeCritSpot after the window closes.
        /// </summary>
        public void ResetPartsForPhase(bool useShields)
        {
            _rightHandBroken = false;
            _leftHandBroken  = false;

            bool shieldRight = useShields && definition.rightHand != null && definition.rightHand.hasSoulShieldInPhase2;
            bool shieldLeft  = useShields && definition.leftHand  != null && definition.leftHand.hasSoulShieldInPhase2;

            rightHandPart?.ResetForCycle(shieldRight);
            leftHandPart?.ResetForCycle(shieldLeft);
        }

        // ── Phase management ───────────────────────────────────────────────────────

        private void AdvancePhase()
        {
            if (_phaseIndex >= definition.PhaseCount)
                return;

            _phaseIndex++;
            TransitionToPhase(new GiantBossConfiguredPhase(_phaseIndex));
            OnPhaseChanged?.Invoke(_phaseIndex);

            var data = definition.GetPhaseData(_phaseIndex);
            if (!string.IsNullOrEmpty(data.enterBannerMessage))
                OnPhaseMessage?.Invoke(data.enterBannerMessage);
        }

        private void TransitionToPhase(IBossPhase newPhase)
        {
            if (_critWindowCoroutine != null)
            {
                StopCoroutine(_critWindowCoroutine);
                _critWindowCoroutine = null;
            }

            critSpot?.SetVulnerable(false);
            _critSpotExposed = false;
            SoulRealmManager.Instance?.ForceExitSoulRealm();

            _currentPhase?.OnExit(this);
            _currentPhase = newPhase;
            _currentPhase.OnEnter(this);
        }

        // ── Slam coroutine ─────────────────────────────────────────────────────────

        private IEnumerator SlamLoop()
        {
            while (true)
            {
                // Pause the loop while the crit-spot window is active
                yield return new WaitUntil(() => !_critSpotExposed);

                yield return SlamHand(rightHandPart, "R");
                yield return RealmSimulation.WaitForSecondsRealm(
                    RealmSimulationGroup.Physical,
                    definition.GetPhaseData(_phaseIndex).timeBetweenSlams);

                yield return new WaitUntil(() => !_critSpotExposed);

                yield return SlamHand(leftHandPart, "L");
                yield return RealmSimulation.WaitForSecondsRealm(
                    RealmSimulationGroup.Physical,
                    definition.GetPhaseData(_phaseIndex).timeBetweenSlams);
            }
        }

        private IEnumerator SlamHand(BossPart hand, string suffix)
        {
            if (hand == null
                || hand.State == BossPartState.Broken
                || hand.State == BossPartState.Disabled)
                yield break;

            float groundedDuration = definition.GetPhaseData(_phaseIndex).slamGroundedDuration;

            // ── Windup ──────────────────────────────────────────────────────────
            hand.SetState(BossPartState.Slamming);
            bossAnimator?.SetTrigger($"SlamWindup_{suffix}");

            yield return RealmSimulation.WaitForSecondsRealm(
                RealmSimulationGroup.Physical,
                definition.slamWindupDuration);

            // ── Impact ───────────────────────────────────────────────────────────
            DealSlamDamage(hand);
            bossAnimator?.SetTrigger($"SlamLand_{suffix}");

            bool needsShield = definition.GetPhaseData(_phaseIndex).useShieldedHands
                && hand.Definition != null
                && hand.Definition.hasSoulShieldInPhase2;

            hand.SetState(needsShield ? BossPartState.Shielded : BossPartState.Grounded);

            // ── Grounded window — wait until broken or time expires ───────────────
            float elapsed = 0f;
            while (elapsed < groundedDuration && hand.State != BossPartState.Broken)
            {
                elapsed += RealmSimulation.DeltaTime(RealmSimulationGroup.Physical);
                yield return null;
            }

            // ── Recovery ────────────────────────────────────────────────────────
            if (hand.State != BossPartState.Broken)
                hand.SetState(BossPartState.Idle);

            bossAnimator?.SetTrigger($"SlamRecover_{suffix}");
            yield return RealmSimulation.WaitForSecondsRealm(
                RealmSimulationGroup.Physical,
                definition.slamRecoveryDuration);
        }

        // ── Slam damage ────────────────────────────────────────────────────────────

        private void DealSlamDamage(BossPart hand)
        {
            if (!RealmSimulation.IsSimulating(RealmSimulationGroup.Physical))
                return;

            StartCoroutine(PlaySlamShockwaveOnImpact(hand));

            if (playerEntity == null) return;

            float dist = Vector3.Distance(hand.transform.position, playerEntity.transform.position);
            if (dist > definition.slamDamageRadius) return;

            var playerData = playerEntity.GetEntityData();
            if (playerData == null || !playerData.IsAlive) return;

            playerData.TakeDamage(definition.slamDamage);

            CombatEvents.TriggerDamageApplied(new CombatEventData
            {
                source      = _combatEntity,
                target      = playerEntity,
                damageAmount = definition.slamDamage,
                hitPosition  = playerEntity.GetHitPoint()
            });
        }

        private IEnumerator PlaySlamShockwaveOnImpact(BossPart hand)
        {
            float wait = Mathf.Max(0f, slamShockwaveImpactDelay);
            if (wait > 0f)
                yield return RealmSimulation.WaitForSecondsRealm(RealmSimulationGroup.Physical, wait);

            TryPlaySlamShockwave(hand);
        }

        /// <summary>
        /// Spawns the optional slam shockwave parented to the fist; radii from inspector (end defaults to slam damage radius).
        /// </summary>
        private void TryPlaySlamShockwave(BossPart hand)
        {
            if (slamShockwaveVfxPrefab == null || definition == null || hand == null)
                return;

            GameObject instance = Instantiate(slamShockwaveVfxPrefab);
            Transform fist = hand.transform;
            instance.transform.SetParent(fist, false);
            instance.transform.localPosition = slamShockwavePositionOffset;
            instance.transform.localRotation = slamShockwaveVfxPrefab.transform.localRotation;
            instance.transform.localScale = slamShockwaveVfxPrefab.transform.localScale;

            // Prefer an existing driver; otherwise add one so third-party VFX prefabs work without manual setup.
            var shockwave = instance.GetComponent<BossSlamShockwaveVfx>()
                ?? instance.GetComponentInChildren<BossSlamShockwaveVfx>(true);
            if (shockwave == null)
                shockwave = instance.AddComponent<BossSlamShockwaveVfx>();

            float endR = slamShockwaveEndRadius > 0f ? slamShockwaveEndRadius : definition.slamDamageRadius;
            shockwave.Play(
                Mathf.Max(0f, slamShockwaveStartRadius),
                Mathf.Max(0f, endR),
                slamShockwaveExpandDuration);
        }

        // ── Part broken / crit-spot cycle ──────────────────────────────────────────

        private void HandlePartBroken(BossPart part)
        {
            if (part == rightHandPart) _rightHandBroken = true;
            if (part == leftHandPart)  _leftHandBroken  = true;

            if (_rightHandBroken && _leftHandBroken && !_critSpotExposed)
            {
                if (_critWindowCoroutine != null)
                    StopCoroutine(_critWindowCoroutine);

                _critWindowCoroutine = StartCoroutine(ExposeCritSpot());
            }
        }

        private void HandlePartReset(BossPart part)
        {
            if (part == rightHandPart) _rightHandBroken = false;
            if (part == leftHandPart)  _leftHandBroken  = false;
        }

        private IEnumerator ExposeCritSpot()
        {
            _critSpotExposed = true;

            var phaseData = definition.GetPhaseData(_phaseIndex);
            float windowSeconds = phaseData.critSpotVulnerableWindow;
            bool requiresSoul = phaseData.critRequiresSoulRealm;
            critSpot?.SetVulnerable(true, requiresSoulRealm: requiresSoul);

            // This window's "required" crit is exactly the remaining souls down to the phase's exit threshold.
            float total = Mathf.Max(0.0001f, definition.totalSouls);
            float targetSouls = Mathf.Clamp01(phaseData.exitSoulPercentThreshold) * total;
            _pendingCritDrainSouls = 0f;
            _requiredCritDrainSoulsThisWindow = Mathf.Max(0f, _remainingSouls - targetSouls);

            Debug.Log($"[GiantBossController] Both hands broken — crit spot exposed " +
                      $"(soulRealm={requiresSoul}, window={windowSeconds:F1}s).");

            float elapsed = 0f;
            while (elapsed < windowSeconds && _encounterStarted)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_encounterStarted)
            {
                SoulRealmManager.Instance?.ForceExitSoulRealm();
                OnCritWindowExpired?.Invoke();
                Debug.Log("[GiantBossController] Crit window expired — ejecting from Soul Realm, resetting hands.");
            }

            critSpot?.SetVulnerable(false);
            _critSpotExposed = false;
            _pendingCritDrainSouls = 0f;
            _requiredCritDrainSoulsThisWindow = 0f;

            ResetPartsForPhase(definition.GetPhaseData(_phaseIndex).useShieldedHands);

            Debug.Log("[GiantBossController] Crit window closed — hands reset.");
        }

        // ── Defeat ────────────────────────────────────────────────────────────────

        private void DefeatBoss()
        {
            _encounterStarted = false;

            StopSlamLoop();
            if (_critWindowCoroutine != null)
                StopCoroutine(_critWindowCoroutine);

            critSpot?.SetVulnerable(false);
            bossAnimator?.SetTrigger("Die");

            OnBossDefeated?.Invoke();

            Debug.Log($"[GiantBossController] {definition.bossName} defeated — all souls released!");
        }
    }
}
