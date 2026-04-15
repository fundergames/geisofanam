using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Top-level data asset for the Giant Soul Warden encounter.
    ///
    /// Contains:
    ///   - Soul pool (= boss total HP, drained by crit-spot hits)
    ///   - Part definitions (right hand, left hand, crit spot)
    ///   - Per-phase tuning (<see cref="GiantBossPhaseData"/>) — slam rhythm, crit windows, shields, transitions
    ///   - Shared slam animation/damage values
    ///
    /// GiantBossController reads from this asset and drives all encounter logic.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GiantBoss_",
        menuName  = "Funder Games/Rogue Deal/Boss/Giant Boss Definition")]
    public class GiantBossDefinition : ScriptableObject
    {
        // ── Identity ───────────────────────────────────────────────────────────────

        [Header("Identity")]
        public string bossName  = "Soul Warden";
        public string title     = "Giant of the Veil";
        [TextArea(2, 4)]
        public string loreDescription;
        public Sprite portrait;

        // ── Soul Pool (Boss HP) ────────────────────────────────────────────────────

        [Header("Soul Pool — Boss HP")]
        [Tooltip("Total souls held inside the giant. Draining this to 0 defeats the boss.")]
        public float totalSouls = 100f;

        [Tooltip("Souls drain per 1 point of crit-spot damage.")]
        public float soulDrainPerDamagePoint = 1f;

        // ── Phases ───────────────────────────────────────────────────────────────────

        [Header("Phases")]
        [Tooltip("Ordered phases (first element = phase 1). Add or remove entries to change phase count.")]
        public GiantBossPhaseData[] phases;

        // ── Part Definitions ───────────────────────────────────────────────────────

        [Header("Parts")]
        [Tooltip("Right fist. Must have hasSoulShieldInPhase2 = true to grow a shield when the phase uses shields.")]
        public BossPartDefinition rightHand;
        [Tooltip("Left fist.")]
        public BossPartDefinition leftHand;
        [Tooltip("The soul core / weak spot exposed after both hands are broken.")]
        public BossPartDefinition critSpot;

        // ── Fist Slam — shared animation / impact ─────────────────────────────────

        [Header("Fist Slam — Shared")]
        [Tooltip("Seconds the windup animation plays before the fist hits the ground.")]
        public float slamWindupDuration = 1.5f;

        [Tooltip("Recovery pause after a fist lifts back up before the next slam.")]
        public float slamRecoveryDuration = 1f;

        [Tooltip("Damage dealt to the player if they are inside slamDamageRadius when the fist lands.")]
        public float slamDamage = 25f;

        [Tooltip("Radius around the fist's grounded position that damages the player.")]
        public float slamDamageRadius = 3f;

        // ── Legacy (migration from pre–phase-blob assets) ─────────────────────────

        [SerializeField, HideInInspector] private float phase2SoulThreshold = 0.5f;
        [SerializeField, HideInInspector] private float phase3SoulThreshold = 0.25f;
        [SerializeField, HideInInspector] private float slamGroundedDuration = 4f;
        [SerializeField, HideInInspector] private float slamGroundedDurationPhase2 = 8f;
        [SerializeField, HideInInspector] private float slamGroundedDurationPhase3;
        [SerializeField, HideInInspector] private float timeBetweenSlams = 1.5f;
        [SerializeField, HideInInspector] private float timeBetweenSlamsPhase3;
        [SerializeField, HideInInspector] private float critSpotVulnerableWindow = 6f;
        [SerializeField, HideInInspector] private float critSpotVulnerableWindowPhase2;
        [SerializeField, HideInInspector] private float critSpotVulnerableWindowPhase3;
        [SerializeField, HideInInspector] private bool critRequiresSoulRealmPhase1 = true;
        [SerializeField, HideInInspector] private bool critRequiresSoulRealmPhase2 = true;
        [SerializeField, HideInInspector] private bool critRequiresSoulRealmPhase3 = true;

        // ── Runtime helpers ─────────────────────────────────────────────────────────

        /// <summary>Number of configured phases (at least 1).</summary>
        public int PhaseCount
        {
            get
            {
                EnsurePhasesPopulated();
                return Mathf.Max(1, phases != null ? phases.Length : 0);
            }
        }

        /// <summary>1-based phase index into <see cref="phases"/>.</summary>
        public GiantBossPhaseData GetPhaseData(int phaseIndex1Based)
        {
            EnsurePhasesPopulated();

            if (phases == null || phases.Length == 0)
                return CreateDefaultPhases()[0];

            int i = Mathf.Clamp(phaseIndex1Based - 1, 0, phases.Length - 1);
            return phases[i];
        }

        private void OnEnable()
        {
            EnsurePhasesPopulated();
        }

        private void EnsurePhasesPopulated()
        {
            if (phases != null && phases.Length > 0)
                return;

            phases = MigrateFromLegacyFields();
        }

        private GiantBossPhaseData[] MigrateFromLegacyFields()
        {
            float p2Crit = critSpotVulnerableWindowPhase2 > 0f ? critSpotVulnerableWindowPhase2 : critSpotVulnerableWindow;
            float p3Crit = critSpotVulnerableWindowPhase3 > 0f ? critSpotVulnerableWindowPhase3 : p2Crit;

            float p3SlamGround = slamGroundedDurationPhase3 > 0f ? slamGroundedDurationPhase3 : slamGroundedDurationPhase2;
            float p3Between = timeBetweenSlamsPhase3 > 0f ? timeBetweenSlamsPhase3 : timeBetweenSlams;

            var phase1 = new GiantBossPhaseData
            {
                exitSoulPercentThreshold   = phase2SoulThreshold,
                enterBannerMessage         = "",
                timeBetweenSlams           = timeBetweenSlams,
                slamGroundedDuration       = slamGroundedDuration,
                critSpotVulnerableWindow   = critSpotVulnerableWindow,
                critRequiresSoulRealm      = critRequiresSoulRealmPhase1,
                useShieldedHands           = false,

                // Data-driven structure defaults (Phase 1)
                stunGateSeconds            = 0f,
                completionGateSeconds      = 0f,
                stunByBreakingSoulShield   = false,
                requireBothFistsDestroyed  = false,
                requirePhysicalCritShield  = false,
                requireSoulCritShield      = true,  // Phase 1 ends on crit-shield break (not soul threshold)
                requirePhysicalCritSpotHit = false,
                requireSoulCritSpotHit     = false,
                physicalBeamsCount         = 0,
                soulBeamsCount             = 0
            };

            var phase2 = new GiantBossPhaseData
            {
                exitSoulPercentThreshold   = phase3SoulThreshold > 0f ? phase3SoulThreshold : 0f,
                enterBannerMessage         = "The Soul Warden's fists begin to glow...",
                timeBetweenSlams           = timeBetweenSlams,
                slamGroundedDuration       = slamGroundedDurationPhase2,
                critSpotVulnerableWindow   = p2Crit,
                critRequiresSoulRealm      = critRequiresSoulRealmPhase2,
                useShieldedHands           = true,

                // Data-driven structure defaults (Phase 2)
                stunGateSeconds            = 0f,
                completionGateSeconds      = 0f,
                stunByBreakingSoulShield   = true,
                requireBothFistsDestroyed  = true,
                requirePhysicalCritShield  = true,
                requireSoulCritShield      = true,
                requirePhysicalCritSpotHit = false,
                requireSoulCritSpotHit     = false,
                physicalBeamsCount         = 2,
                soulBeamsCount             = 0
            };

            if (phase3SoulThreshold <= 0f)
                return new[] { phase1, phase2 };

            var phase3 = new GiantBossPhaseData
            {
                exitSoulPercentThreshold   = 0f,
                enterBannerMessage         = "The veil tears...",
                timeBetweenSlams           = p3Between,
                slamGroundedDuration       = p3SlamGround,
                critSpotVulnerableWindow   = p3Crit,
                critRequiresSoulRealm      = critRequiresSoulRealmPhase3,
                useShieldedHands           = true,

                // Data-driven structure defaults (Phase 3)
                stunGateSeconds            = 0f,
                completionGateSeconds      = 0f,
                stunByBreakingSoulShield   = true,
                requireBothFistsDestroyed  = true,
                requirePhysicalCritShield  = true,
                requireSoulCritShield      = true,
                requirePhysicalCritSpotHit = true,
                requireSoulCritSpotHit     = true,
                physicalBeamsCount         = 2,
                soulBeamsCount             = 3
            };

            return new[] { phase1, phase2, phase3 };
        }

        private static GiantBossPhaseData[] CreateDefaultPhases()
        {
            return new[]
            {
                new GiantBossPhaseData
                {
                    exitSoulPercentThreshold   = 0.5f,
                    enterBannerMessage         = "",
                    timeBetweenSlams           = 1.5f,
                    slamGroundedDuration       = 4f,
                    critSpotVulnerableWindow   = 6f,
                    critRequiresSoulRealm      = true,
                    useShieldedHands           = false
                },
                new GiantBossPhaseData
                {
                    exitSoulPercentThreshold   = 0.25f,
                    enterBannerMessage         = "The Soul Warden's fists begin to glow...",
                    timeBetweenSlams           = 1.5f,
                    slamGroundedDuration       = 8f,
                    critSpotVulnerableWindow   = 6f,
                    critRequiresSoulRealm      = true,
                    useShieldedHands           = true
                },
                new GiantBossPhaseData
                {
                    exitSoulPercentThreshold   = 0f,
                    enterBannerMessage         = "The veil tears...",
                    timeBetweenSlams           = 1.5f,
                    slamGroundedDuration       = 8f,
                    critSpotVulnerableWindow   = 6f,
                    critRequiresSoulRealm      = true,
                    useShieldedHands           = true
                }
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (phases == null || phases.Length < 2)
                return;

            for (int i = 0; i < phases.Length - 1; i++)
            {
                float next = phases[i + 1].exitSoulPercentThreshold;
                if (next <= 0f)
                    continue;

                if (phases[i].exitSoulPercentThreshold <= next)
                {
                    Debug.LogWarning(
                        "[GiantBossDefinition] Phase " + (i + 1) + " exitSoulPercentThreshold should usually be " +
                        "greater than phase " + (i + 2) + " so the encounter progresses as souls are drained.",
                        this);
                }
            }
        }
#endif
    }
}
