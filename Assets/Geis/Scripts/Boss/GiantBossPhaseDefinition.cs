using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Designer-authored asset for one giant boss encounter phase (slam rhythm, crit window, objectives, beams).
    /// Reference these from <see cref="GiantBossDefinition.phaseDefinitions"/> in order (first = phase 1).
    /// </summary>
    [CreateAssetMenu(
        fileName = "GiantBossPhase_",
        menuName = "Funder Games/Rogue Deal/Boss/Giant Boss Phase")]
    public class GiantBossPhaseDefinition : ScriptableObject
    {
        [Header("Phase transition")]
        [Tooltip(
            "When remaining souls / total is at or below this value, advance to the next phase. " +
            "Use 0 on the final phase, or on an intermediate phase to disable advancing (e.g. two-phase fight).")]
        [Range(0f, 1f)]
        public float exitSoulPercentThreshold = 0.5f;

        [TextArea(1, 3)]
        [Tooltip("Optional banner when this phase becomes active.")]
        public string enterBannerMessage = "";

        [Header("Slam cadence")]
        [Tooltip("Idle gap after one fist's slam sequence before the other fist starts.")]
        public float timeBetweenSlams = 1.5f;

        [Tooltip("How long each fist stays grounded (or shielded) before recovering.")]
        public float slamGroundedDuration = 4f;

        [Header("Crit window")]
        public float critSpotVulnerableWindow = 6f;

        [Tooltip("If true, only spectral attacks can damage the crit spot.")]
        public bool critRequiresSoulRealm = true;

        [Header("Hands")]
        [Tooltip("Spawn soul shields on each slam; requires BossPartDefinition.hasSoulShieldInPhase2.")]
        public bool useShieldedHands;

        [Tooltip("When false (default), Soul Realm freeze policy matches legacy rules (phase index). When true, use Freeze Boss Parts In Soul Realm below.")]
        public bool overrideSoulRealmFreezePolicy;

        [Tooltip("Only used when Override Soul Realm Freeze Policy is true.")]
        public bool freezeBossPartsInSoulRealm = true;

        [Header("Timers (optional)")]
        [Tooltip("Seconds to get both fists into the 'stunned' state for this phase. If <= 0, no stun gate timer.")]
        public float stunGateSeconds;

        [Tooltip("Seconds to complete this phase's full objective chain once started. If <= 0, no completion timer.")]
        public float completionGateSeconds;

        [Header("Phase objectives")]
        [Tooltip("If true, breaking a fist's soul shield counts as 'stunning' that fist for this phase (pins it).")]
        public bool stunByBreakingSoulShield;

        [Tooltip("If true, the player must break both fists (physical HP) during the completion gate.")]
        public bool requireBothFistsDestroyed;

        [Tooltip("If true, a physical-only crit shield must be destroyed during this phase.")]
        public bool requirePhysicalCritShield;

        [Tooltip("If true, a soul-only crit shield must be destroyed during this phase.")]
        public bool requireSoulCritShield;

        [Tooltip("If true, the crit spot must take at least one valid hit in Physical realm mode.")]
        public bool requirePhysicalCritSpotHit;

        [Tooltip("If true, the crit spot must take at least one valid hit in Soul realm mode.")]
        public bool requireSoulCritSpotHit;

        [Header("Beams (counts)")]
        [Tooltip("Number of physical-realm tracking beams to fire during this phase.")]
        public int physicalBeamsCount;

        [Tooltip("Number of soul-realm tracking beams to fire during this phase.")]
        public int soulBeamsCount;

        [Header("Beams & timers (0 = use GiantBossController fallbacks)")]
        [Tooltip("Seconds between tracking beams. 0 = use GiantBossController fallback values.")]
        public float trackingBeamInterval;

        [Tooltip("Damage per beam hit that reaches the player. 0 = use controller fallback.")]
        public float trackingBeamDamage;

        [Tooltip(
            "Soul crit shield: restart the objective chain if not broken within this time (Soul sim). 0 = use controller fallback.")]
        public float soulCritShieldBreakTimerSeconds;

        [Tooltip(
            "Pinned fists: restart if both not broken within this time (Physical sim). 0 = use controller fallback.")]
        public float physicalPinnedFistCleanupTimerSeconds;

        internal static GiantBossPhaseDefinition CreateFromLegacy(GiantBossPhaseData src)
        {
            if (src == null)
                return null;

            var d = CreateInstance<GiantBossPhaseDefinition>();
            d.exitSoulPercentThreshold = src.exitSoulPercentThreshold;
            d.enterBannerMessage = src.enterBannerMessage;
            d.timeBetweenSlams = src.timeBetweenSlams;
            d.slamGroundedDuration = src.slamGroundedDuration;
            d.critSpotVulnerableWindow = src.critSpotVulnerableWindow;
            d.critRequiresSoulRealm = src.critRequiresSoulRealm;
            d.useShieldedHands = src.useShieldedHands;
            d.overrideSoulRealmFreezePolicy = src.overrideSoulRealmFreezePolicy;
            d.freezeBossPartsInSoulRealm = src.freezeBossPartsInSoulRealm;
            d.stunGateSeconds = src.stunGateSeconds;
            d.completionGateSeconds = src.completionGateSeconds;
            d.stunByBreakingSoulShield = src.stunByBreakingSoulShield;
            d.requireBothFistsDestroyed = src.requireBothFistsDestroyed;
            d.requirePhysicalCritShield = src.requirePhysicalCritShield;
            d.requireSoulCritShield = src.requireSoulCritShield;
            d.requirePhysicalCritSpotHit = src.requirePhysicalCritSpotHit;
            d.requireSoulCritSpotHit = src.requireSoulCritSpotHit;
            d.physicalBeamsCount = src.physicalBeamsCount;
            d.soulBeamsCount = src.soulBeamsCount;
            d.trackingBeamInterval = src.trackingBeamInterval;
            d.trackingBeamDamage = src.trackingBeamDamage;
            d.soulCritShieldBreakTimerSeconds = src.soulCritShieldBreakTimerSeconds;
            d.physicalPinnedFistCleanupTimerSeconds = src.physicalPinnedFistCleanupTimerSeconds;
            return d;
        }
    }
}
