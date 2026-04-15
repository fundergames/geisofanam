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

        [Header("Phase 3 — Dual-Realm Loop (optional)")]
        [Tooltip("Soul-only shield gating the crit spot in phase 3.")]
        [SerializeField] private SoulShieldTarget phase3SoulCritShield;
        [Tooltip("Physical-only shield gating the crit spot in phase 3.")]
        [SerializeField] private PhysicalShieldTarget phase3PhysicalCritShield;
        [Tooltip("Eye weak spot used after beams complete (physical-only).")]
        [SerializeField] private PhysicalShieldTarget phase3EyeTarget;
        [Tooltip("Origin transform for tracking beams (defaults to crit spot if null).")]
        [SerializeField] private Transform phase3EyeOrigin;

        [Header("Phase 3 — Tuning")]
        [Tooltip("X seconds in Soul Realm to break the soul crit shield before phase 3 restarts.")]
        [SerializeField] private float phase3SoulShieldSeconds = 8f;
        [Tooltip("Y seconds in Physical Realm to break both pinned fists after soul shield breaks.")]
        [SerializeField] private float phase3PhysicalCleanupSeconds = 10f;
        [Tooltip("Z tracking beams fired after physical crit shield breaks.")]
        [SerializeField] private int phase3TrackingBeamCount = 5;
        [Tooltip("Seconds between tracking beams.")]
        [SerializeField] private float phase3TrackingBeamInterval = 0.6f;
        [Tooltip("Damage applied to the player per tracking beam (line-of-sight).")]
        [SerializeField] private float phase3TrackingBeamDamage = 12f;
        [Tooltip("Seconds the eye stays vulnerable after beams complete. If <= 0, remains vulnerable until reset/kill.")]
        [SerializeField] private float phase3EyeVulnerableSeconds = 6f;

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

        [Header("Tracking beam VFX")]
        [Tooltip("Optional. If assigned, used as the pooled prefab for tracking beams. Should contain (or will be given) a BossBeamLineVfx + LineRenderer.")]
        [SerializeField] private GameObject trackingBeamVfxPrefab;
        [Tooltip("Optional override material for the tracking beam line.")]
        [SerializeField] private Material trackingBeamMaterial;
        [Tooltip("Seconds a tracking beam line stays visible.")]
        [SerializeField] private float trackingBeamVfxDuration = 0.18f;
        [Tooltip("Line width for the tracking beam.")]
        [SerializeField] private float trackingBeamVfxWidth = 0.06f;
        [Tooltip("Beam color when fired in Physical simulation.")]
        [SerializeField] private Color trackingBeamPhysicalColor = new Color(1f, 0.25f, 0.15f, 1f);
        [Tooltip("Beam color when fired in Soul simulation.")]
        [SerializeField] private Color trackingBeamSoulColor = new Color(0.25f, 0.85f, 1f, 1f);

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
        private SoulRealmFreezeTarget[] _freezeTargets;

        private readonly System.Collections.Generic.Queue<BossBeamLineVfx> _trackingBeamPool
            = new System.Collections.Generic.Queue<BossBeamLineVfx>(8);
        
        [Header("Debug")]
        [Tooltip("If true, logs crit drain and phase-advance progress (Editor/Dev builds only).")]
        [SerializeField] private bool debugPhaseDrain;

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

            // Boss/hands are typically authored as freeze targets so they pause in Soul Realm.
            // Phase 2 requires they keep updating so soul shields stay aligned to the fists.
            _freezeTargets = GetComponentsInChildren<SoulRealmFreezeTarget>(true);
        }

        private void OnEnable()
        {
            BossPart.OnPartBroken += HandlePartBroken;
            BossPart.OnPartReset  += HandlePartReset;
            SoulRealmManager.SoulRealmStateChanged += HandleSoulRealmStateChanged;
        }

        private void OnDisable()
        {
            BossPart.OnPartBroken -= HandlePartBroken;
            BossPart.OnPartReset  -= HandlePartReset;
            SoulRealmManager.SoulRealmStateChanged -= HandleSoulRealmStateChanged;
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

            TransitionToPhase(new GiantBossSequencePhase(1));
            OnPhaseChanged?.Invoke(1);
            SyncFreezeTargetsForCurrentPhase();

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugPhaseDrain)
            {
                var data = definition.GetPhaseData(_phaseIndex);
                float total = Mathf.Max(0.0001f, definition.totalSouls);
                float targetSouls = Mathf.Clamp01(data.exitSoulPercentThreshold) * total;
                Debug.Log(
                    $"[GiantBossController] DrainSouls: hitDamage={damage:F2} drainSouls={drainSouls:F2} pending={_pendingCritDrainSouls:F2}/{_requiredCritDrainSoulsThisWindow:F2} remaining={_remainingSouls:F2} target={targetSouls:F2} phase={_phaseIndex}",
                    this);
            }
#endif

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
            TransitionToPhase(new GiantBossSequencePhase(_phaseIndex));
            OnPhaseChanged?.Invoke(_phaseIndex);
            SyncFreezeTargetsForCurrentPhase();

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

        // ── Phase 3 helpers (called by GiantBossPhase3DualRealmLoop) ───────────────

        public BossPart RightHandPart => rightHandPart;
        public BossPart LeftHandPart  => leftHandPart;
        public CritSpot CritSpot      => critSpot;

        public SoulShieldTarget     Phase3SoulCritShield     => phase3SoulCritShield;
        public PhysicalShieldTarget Phase3PhysicalCritShield => phase3PhysicalCritShield;
        public PhysicalShieldTarget Phase3EyeTarget          => phase3EyeTarget;
        public Transform Phase3EyeOrigin                     => phase3EyeOrigin != null ? phase3EyeOrigin : (critSpot != null ? critSpot.transform : transform);

        public float Phase3SoulShieldSeconds         => phase3SoulShieldSeconds;
        public float Phase3PhysicalCleanupSeconds    => phase3PhysicalCleanupSeconds;
        public int   Phase3TrackingBeamCount         => phase3TrackingBeamCount;
        public float Phase3TrackingBeamInterval      => phase3TrackingBeamInterval;
        public float Phase3TrackingBeamDamage        => phase3TrackingBeamDamage;
        public float Phase3EyeVulnerableSeconds      => phase3EyeVulnerableSeconds;

        public CombatEntity PlayerEntity => playerEntity;

        public void ForceDefeatBoss()
        {
            if (!_encounterStarted)
                return;
            DefeatBoss();
        }

        private void HandleSoulRealmStateChanged()
        {
            // Re-apply freeze policy on realm toggles because SoulRealmManager reapplies freezes on entry/exit.
            SyncFreezeTargetsForCurrentPhase();
        }

        private void SyncFreezeTargetsForCurrentPhase()
        {
            if (_freezeTargets == null || _freezeTargets.Length == 0)
                return;

            // Phase 2: fists must keep animating in both realms so shields remain aligned.
            bool allowFreeze = _phaseIndex != 2;
            for (int i = 0; i < _freezeTargets.Length; i++)
            {
                var t = _freezeTargets[i];
                if (t != null)
                    t.SetAllowSoulRealmFreeze(allowFreeze);
            }
        }

        public void PlayTrackingBeamVfx(RealmSimulationGroup group, Vector3 origin, Vector3 end)
        {
            // VFX should not depend on realm simulation state; it’s purely visual.
            var vfx = GetTrackingBeamVfxInstance();
            if (vfx == null)
                return;

            Color c = group == RealmSimulationGroup.Soul ? trackingBeamSoulColor : trackingBeamPhysicalColor;
            vfx.Play(origin, end, c, trackingBeamVfxWidth, trackingBeamVfxDuration, trackingBeamMaterial);
        }

        private BossBeamLineVfx GetTrackingBeamVfxInstance()
        {
            while (_trackingBeamPool.Count > 0)
            {
                var pooled = _trackingBeamPool.Dequeue();
                if (pooled != null)
                    return pooled;
            }

            GameObject go;
            if (trackingBeamVfxPrefab != null)
            {
                go = Instantiate(trackingBeamVfxPrefab);
            }
            else
            {
                go = new GameObject("BossTrackingBeamVfx");
            }

            if (go == null)
                return null;

            go.transform.SetParent(null, false);

            var vfx = go.GetComponent<BossBeamLineVfx>() ?? go.AddComponent<BossBeamLineVfx>();
            StartCoroutine(ReturnBeamToPoolWhenDone(vfx));
            return vfx;
        }

        private IEnumerator ReturnBeamToPoolWhenDone(BossBeamLineVfx vfx)
        {
            // Each instance handles its own disable timing; this coroutine returns it to the pool once it turns itself off.
            while (vfx != null && vfx.gameObject.activeSelf)
                yield return null;

            if (vfx != null)
                _trackingBeamPool.Enqueue(vfx);
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
                    RealmSimulationGroup.Universal,
                    definition.GetPhaseData(_phaseIndex).timeBetweenSlams);

                yield return new WaitUntil(() => !_critSpotExposed);

                yield return SlamHand(leftHandPart, "L");
                yield return RealmSimulation.WaitForSecondsRealm(
                    RealmSimulationGroup.Universal,
                    definition.GetPhaseData(_phaseIndex).timeBetweenSlams);
            }
        }

        private IEnumerator SlamHand(BossPart hand, string suffix)
        {
            if (hand == null
                || hand.State == BossPartState.Broken
                || hand.State == BossPartState.Pinned
                || hand.State == BossPartState.Disabled)
                yield break;

            float groundedDuration = definition.GetPhaseData(_phaseIndex).slamGroundedDuration;

            // ── Windup ──────────────────────────────────────────────────────────
            hand.SetState(BossPartState.Slamming);
            bossAnimator?.SetTrigger($"SlamWindup_{suffix}");

            yield return RealmSimulation.WaitForSecondsRealm(
                RealmSimulationGroup.Universal,
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
            // If the hand becomes Pinned (stunned), it stays down and we stop the slam sequence here.
            while (elapsed < groundedDuration
                   && hand.State != BossPartState.Broken
                   && hand.State != BossPartState.Pinned)
            {
                // While shielded, do not advance the grounded timer. Otherwise the slam can "recover"
                // mid-shield, which makes a later stun/pin feel like it snapped back incorrectly.
                if (hand.State != BossPartState.Shielded)
                    elapsed += RealmSimulation.DeltaTime(RealmSimulationGroup.Universal);
                yield return null;
            }

            // ── Recovery ────────────────────────────────────────────────────────
            // Stunned (Pinned): stay on the ground in place.
            if (hand.State == BossPartState.Pinned)
                yield break;

            // Broken: play recovery so the fist returns to the boss' side/idle pose, but keep the state as Broken
            // so SlamLoop skips it and it remains an objective/end-state until a phase reset.
            if (hand.State != BossPartState.Broken)
                hand.SetState(BossPartState.Idle);

            bossAnimator?.SetTrigger($"SlamRecover_{suffix}");
            yield return RealmSimulation.WaitForSecondsRealm(
                RealmSimulationGroup.Universal,
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
                yield return RealmSimulation.WaitForSecondsRealm(RealmSimulationGroup.Universal, wait);

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

            // If a pinned (stunned) fist breaks, it won't be running a SlamHand coroutine anymore.
            // Trigger the recover animation so it returns to the boss-side pose.
            if (bossAnimator != null && part != null && part.WasPinnedWhenBrokenThisCycle)
            {
                if (part == rightHandPart) bossAnimator.SetTrigger("SlamRecover_R");
                if (part == leftHandPart)  bossAnimator.SetTrigger("SlamRecover_L");
            }

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
