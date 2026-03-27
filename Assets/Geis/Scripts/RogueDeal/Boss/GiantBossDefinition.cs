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

        [Tooltip("Recovery pause after a fist lifts back up before the next slam.")]
        public float slamRecoveryDuration = 1f;

        [Tooltip("Gap between the right-hand slam sequence ending and the left-hand one beginning.")]
        public float timeBetweenSlams = 1.5f;

        // ── Fist Slam Damage ───────────────────────────────────────────────────────

        [Header("Fist Slam — Damage")]
        [Tooltip("Damage dealt to the player if they are inside slamDamageRadius when the fist lands.")]
        public float slamDamage = 25f;

        [Tooltip("Radius around the fist's grounded position that damages the player.")]
        public float slamDamageRadius = 3f;

        // ── Crit Spot ──────────────────────────────────────────────────────────────

        [Header("Crit Spot")]
        [Tooltip("Seconds the crit spot stays exposed after both hands are broken. " +
                 "Window closes whether or not the player attacks it.")]
        public float critSpotVulnerableWindow = 6f;
    }
}
