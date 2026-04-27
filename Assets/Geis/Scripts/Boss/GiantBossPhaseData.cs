/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using System;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Legacy inline phase blob — only used to deserialize older <see cref="GiantBossDefinition"/> assets
    /// that stored phases as embedded objects under the former <c>phases</c> field.
    /// Author new phases as <see cref="GiantBossPhaseDefinition"/> assets and assign them to
    /// <see cref="GiantBossDefinition.phaseDefinitions"/>.
    /// </summary>
    [Serializable]
    internal class GiantBossPhaseData
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

        [Tooltip("When false (default), Soul Realm freeze policy matches legacy rules (phase index). When true, use Freeze Boss Parts In Soul Realm below.")]
        public bool overrideSoulRealmFreezePolicy;

        [Tooltip("Only used when Override Soul Realm Freeze Policy is true.")]
        public bool freezeBossPartsInSoulRealm = true;

        [Header("Timers (optional)")]
        [Tooltip("Seconds to get both fists into the 'stunned' state for this phase. If <= 0, no stun gate timer.")]
        public float stunGateSeconds = 0f;

        [Tooltip("Seconds to complete this phase's full objective chain once started. If <= 0, no completion timer.")]
        public float completionGateSeconds = 0f;

        [Header("Phase objectives (data-driven structure)")]
        [Tooltip("If true, breaking a fist's soul shield counts as 'stunning' that fist for this phase (pins it).")]
        public bool stunByBreakingSoulShield = false;

        [Tooltip("If true, the player must break both fists (physical HP) during the completion gate.")]
        public bool requireBothFistsDestroyed = false;

        [Tooltip("If true, a physical-only crit shield must be destroyed during this phase.")]
        public bool requirePhysicalCritShield = false;

        [Tooltip("If true, a soul-only crit shield must be destroyed during this phase.")]
        public bool requireSoulCritShield = false;

        [Tooltip("If true, the crit spot must take at least one valid hit in Physical realm mode.")]
        public bool requirePhysicalCritSpotHit = false;

        [Tooltip("If true, the crit spot must take at least one valid hit in Soul realm mode.")]
        public bool requireSoulCritSpotHit = false;

        [Header("Beams (counts)")]
        [Tooltip("Number of physical-realm tracking beams to fire during this phase.")]
        public int physicalBeamsCount = 0;

        [Tooltip("Number of soul-realm tracking beams to fire during this phase.")]
        public int soulBeamsCount = 0;

        [Header("Beams & phase-3 pacing")]
        [Tooltip("Seconds between tracking beams. 0 = use GiantBossController fallback values.")]
        public float trackingBeamInterval;

        [Tooltip("Damage per beam hit that reaches the player. 0 = use controller fallback.")]
        public float trackingBeamDamage;

        [Tooltip(
            "Soul crit shield: restart the objective chain if not broken within this time (Soul sim). 0 = use controller fallback; set controller fallback to 0 for no limit.")]
        public float soulCritShieldBreakTimerSeconds;

        [Tooltip(
            "Pinned fists: restart if both not broken within this time (Physical sim). 0 = use controller fallback.")]
        public float physicalPinnedFistCleanupTimerSeconds;
    }
}
