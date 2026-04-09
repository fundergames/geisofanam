using System;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Designer-tunable data for one encounter phase (slam rhythm, crit window, shields, transition).
    /// A <see cref="GiantBossDefinition"/> holds an ordered list of these — one blob per phase.
    /// </summary>
    [Serializable]
    public class GiantBossPhaseData
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
        public bool useShieldedHands = false;
    }
}
