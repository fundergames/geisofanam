using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Top-level data asset for the Giant Soul Warden encounter.
    ///
    /// Contains:
    ///   - Soul pool (= boss total HP, drained by crit-spot hits)
    ///   - Part definitions (right hand, left hand, crit spot)
    ///   - Fist-slam timing and damage values
    ///   - Phase-transition threshold
    ///
    /// All tuneable numbers live here so designers can iterate without touching code.
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

        [Tooltip("Fraction of souls remaining that triggers Phase 2. " +
                 "E.g. 0.5 means Phase 2 starts after the player has released half the souls.")]
        [Range(0f, 1f)]
        public float phase2SoulThreshold = 0.5f;

        [Tooltip("Fraction of souls remaining that triggers Phase 3. Set to 0 to disable Phase 3 " +
                 "(Phase 2 continues until the boss is defeated). Must be below phase2SoulThreshold.")]
        [Range(0f, 1f)]
        public float phase3SoulThreshold = 0.25f;

        // ── Part Definitions ───────────────────────────────────────────────────────

        [Header("Parts")]
        [Tooltip("Right fist. Must have hasSoulShieldInPhase2 = true to grow a shield in Phase 2.")]
        public BossPartDefinition rightHand;
        [Tooltip("Left fist.")]
        public BossPartDefinition leftHand;
        [Tooltip("The soul core / weak spot exposed after both hands are broken.")]
        public BossPartDefinition critSpot;

        // ── Fist Slam Timing ───────────────────────────────────────────────────────

        [Header("Fist Slam — Timing")]
        [Tooltip("Seconds the windup animation plays before the fist hits the ground.")]
        public float slamWindupDuration = 1.5f;

        [Tooltip("Phase 1: seconds the fist stays grounded and is physically attackable.")]
        public float slamGroundedDuration = 4f;

        [Tooltip("Phase 2: longer window to give the player time to soul-realm in, " +
                 "destroy the shield, exit, then attack.")]
        public float slamGroundedDurationPhase2 = 8f;

        [Tooltip("Phase 3: grounded window per fist. If 0, uses slamGroundedDurationPhase2.")]
        public float slamGroundedDurationPhase3 = 0f;

        [Tooltip("Recovery pause after a fist lifts back up before the next slam.")]
        public float slamRecoveryDuration = 1f;

        [Tooltip("Gap between the right-hand slam sequence ending and the left-hand one beginning.")]
        public float timeBetweenSlams = 1.5f;

        [Tooltip("Phase 3: gap between slams. If 0, uses timeBetweenSlams.")]
        public float timeBetweenSlamsPhase3 = 0f;

        // ── Fist Slam Damage ───────────────────────────────────────────────────────

        [Header("Fist Slam — Damage")]
        [Tooltip("Damage dealt to the player if they are inside slamDamageRadius when the fist lands.")]
        public float slamDamage = 25f;

        [Tooltip("Radius around the fist's grounded position that damages the player.")]
        public float slamDamageRadius = 3f;

        // ── Crit Spot ──────────────────────────────────────────────────────────────

        [Header("Crit Spot")]
        [Tooltip("Phase 1: seconds the crit spot stays exposed after both hands are broken.")]
        public float critSpotVulnerableWindow = 6f;

        [Tooltip("Phase 2 crit window. If 0, uses critSpotVulnerableWindow.")]
        public float critSpotVulnerableWindowPhase2 = 0f;

        [Tooltip("Phase 3 crit window. If 0, falls back to Phase 2 then Phase 1 values.")]
        public float critSpotVulnerableWindowPhase3 = 0f;

        [Tooltip("If true, only the spectral ghost can damage the crit spot in Phase 1.")]
        public bool critRequiresSoulRealmPhase1 = true;

        [Tooltip("If true, only the spectral ghost can damage the crit spot in Phase 2.")]
        public bool critRequiresSoulRealmPhase2 = true;

        [Tooltip("If true, only the spectral ghost can damage the crit spot in Phase 3.")]
        public bool critRequiresSoulRealmPhase3 = true;

        /// <summary>Grounded fist window for slam loop, by encounter phase (1–3).</summary>
        public float GetSlamGroundedDuration(int phaseIndex)
        {
            return phaseIndex switch
            {
                1 => slamGroundedDuration,
                2 => slamGroundedDurationPhase2,
                3 => slamGroundedDurationPhase3 > 0f ? slamGroundedDurationPhase3 : slamGroundedDurationPhase2,
                _ => slamGroundedDuration
            };
        }

        /// <summary>Delay between right and left slam in the loop.</summary>
        public float GetTimeBetweenSlams(int phaseIndex)
        {
            if (phaseIndex == 3 && timeBetweenSlamsPhase3 > 0f)
                return timeBetweenSlamsPhase3;
            return timeBetweenSlams;
        }

        /// <summary>Crit vulnerability duration after both hands break.</summary>
        public float GetCritWindowSeconds(int phaseIndex)
        {
            float p1 = critSpotVulnerableWindow;
            float p2 = critSpotVulnerableWindowPhase2 > 0f ? critSpotVulnerableWindowPhase2 : p1;
            float p3 = critSpotVulnerableWindowPhase3 > 0f
                ? critSpotVulnerableWindowPhase3
                : p2;
            return phaseIndex switch
            {
                1 => p1,
                2 => p2,
                3 => p3,
                _ => p1
            };
        }

        /// <summary>Ghost-only vs physical crit damage for this phase.</summary>
        public bool GetCritRequiresSoulRealm(int phaseIndex)
        {
            return phaseIndex switch
            {
                1 => critRequiresSoulRealmPhase1,
                2 => critRequiresSoulRealmPhase2,
                3 => critRequiresSoulRealmPhase3,
                _ => true
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (phase3SoulThreshold > 0f && phase3SoulThreshold >= phase2SoulThreshold)
            {
                Debug.LogWarning(
                    "[GiantBossDefinition] phase3SoulThreshold should be less than phase2SoulThreshold " +
                    "or Phase 3 may never begin.",
                    this);
            }
        }
#endif
    }
}
