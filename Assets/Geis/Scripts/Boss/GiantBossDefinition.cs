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

using UnityEngine;
using UnityEngine.Serialization;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Top-level data asset for the Giant Soul Warden encounter.
    ///
    /// Contains:
    ///   - Soul pool (= boss total HP, drained by crit-spot hits)
    ///   - Part definitions (right hand, left hand, crit spot)
    ///   - Ordered references to <see cref="GiantBossPhaseDefinition"/> assets
    ///   - Shared slam animation/damage values
    ///
    /// GiantBossController reads from this asset and drives all encounter logic.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GiantBoss_",
        menuName  = "Funder Games/Geis/Rogue Deal/Boss/Giant Boss Definition")]
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
        [Tooltip("Ordered phase assets (first = phase 1). Add or remove entries to change phase count.")]
        public GiantBossPhaseDefinition[] phaseDefinitions;

        /// <summary>Embedded phases from older assets (inline YAML). Migrated to <see cref="phaseDefinitions"/> on load.</summary>
        [SerializeField, HideInInspector, FormerlySerializedAs("phases")]
        private GiantBossPhaseData[] _legacyInlinePhases;

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

        [Tooltip("Optional. When assigned, fist slams apply this CombatAction's effects (same pipeline as player attacks). If empty, slamDamage is used as a legacy fallback.")]
        public CombatAction slamDamageAction;

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
                return Mathf.Max(1, phaseDefinitions != null ? phaseDefinitions.Length : 0);
            }
        }

        /// <summary>1-based phase index into <see cref="phaseDefinitions"/>.</summary>
        public GiantBossPhaseDefinition GetPhaseData(int phaseIndex1Based)
        {
            EnsurePhasesPopulated();

            if (phaseDefinitions == null || phaseDefinitions.Length == 0)
                return CreateDefaultPhases()[0];

            int i = Mathf.Clamp(phaseIndex1Based - 1, 0, phaseDefinitions.Length - 1);
            if (phaseDefinitions[i] != null)
                return phaseDefinitions[i];

            return CreateDefaultPhases()[0];
        }

        private void OnEnable()
        {
            TryMigrateLegacyInlinePhases();
            EnsurePhasesPopulated();
        }

        private void TryMigrateLegacyInlinePhases()
        {
            if (_legacyInlinePhases == null || _legacyInlinePhases.Length == 0)
                return;
            if (phaseDefinitions != null && phaseDefinitions.Length > 0 && !AllSlotsNull(phaseDefinitions))
                return;

            int n = _legacyInlinePhases.Length;
            phaseDefinitions = new GiantBossPhaseDefinition[n];
            for (int i = 0; i < n; i++)
            {
                phaseDefinitions[i] = GiantBossPhaseDefinition.CreateFromLegacy(_legacyInlinePhases[i]);
                if (phaseDefinitions[i] != null)
                    phaseDefinitions[i].name = $"Phase {i + 1}";
            }

            _legacyInlinePhases = null;

#if UNITY_EDITOR
            if (phaseDefinitions != null)
            {
                foreach (var p in phaseDefinitions)
                {
                    if (p != null && UnityEditor.AssetDatabase.GetAssetPath(p).Length == 0)
                        UnityEditor.AssetDatabase.AddObjectToAsset(p, this);
                }

                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
            }
#endif
        }

        private static bool AllSlotsNull(GiantBossPhaseDefinition[] arr)
        {
            if (arr == null)
                return true;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] != null)
                    return false;
            }

            return true;
        }

        private void EnsurePhasesPopulated()
        {
            TryMigrateLegacyInlinePhases();

            if (phaseDefinitions != null && phaseDefinitions.Length > 0 && !AllSlotsNull(phaseDefinitions))
                return;

            phaseDefinitions = MigrateFromLegacyFields();
        }

        private GiantBossPhaseDefinition[] MigrateFromLegacyFields()
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
                overrideSoulRealmFreezePolicy = false,

                stunGateSeconds            = 0f,
                completionGateSeconds      = 0f,
                stunByBreakingSoulShield   = false,
                requireBothFistsDestroyed  = false,
                requirePhysicalCritShield  = false,
                requireSoulCritShield      = true,
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
                overrideSoulRealmFreezePolicy = true,
                freezeBossPartsInSoulRealm = false,

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
                return new[]
                {
                    GiantBossPhaseDefinition.CreateFromLegacy(phase1),
                    GiantBossPhaseDefinition.CreateFromLegacy(phase2)
                };

            var phase3 = new GiantBossPhaseData
            {
                exitSoulPercentThreshold   = 0f,
                enterBannerMessage         = "The veil tears...",
                timeBetweenSlams           = p3Between,
                slamGroundedDuration       = p3SlamGround,
                critSpotVulnerableWindow   = p3Crit,
                critRequiresSoulRealm      = critRequiresSoulRealmPhase3,
                useShieldedHands           = true,
                overrideSoulRealmFreezePolicy = false,

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

            return new[]
            {
                GiantBossPhaseDefinition.CreateFromLegacy(phase1),
                GiantBossPhaseDefinition.CreateFromLegacy(phase2),
                GiantBossPhaseDefinition.CreateFromLegacy(phase3)
            };
        }

        private static GiantBossPhaseDefinition[] CreateDefaultPhases()
        {
            return new[]
            {
                GiantBossPhaseDefinition.CreateFromLegacy(new GiantBossPhaseData
                {
                    exitSoulPercentThreshold   = 0.5f,
                    enterBannerMessage         = "",
                    timeBetweenSlams           = 1.5f,
                    slamGroundedDuration       = 4f,
                    critSpotVulnerableWindow   = 6f,
                    critRequiresSoulRealm      = true,
                    useShieldedHands           = false,
                    overrideSoulRealmFreezePolicy = false
                }),
                GiantBossPhaseDefinition.CreateFromLegacy(new GiantBossPhaseData
                {
                    exitSoulPercentThreshold   = 0.25f,
                    enterBannerMessage         = "The Soul Warden's fists begin to glow...",
                    timeBetweenSlams           = 1.5f,
                    slamGroundedDuration       = 8f,
                    critSpotVulnerableWindow   = 6f,
                    critRequiresSoulRealm      = true,
                    useShieldedHands           = true,
                    overrideSoulRealmFreezePolicy = true,
                    freezeBossPartsInSoulRealm = false
                }),
                GiantBossPhaseDefinition.CreateFromLegacy(new GiantBossPhaseData
                {
                    exitSoulPercentThreshold   = 0f,
                    enterBannerMessage         = "The veil tears...",
                    timeBetweenSlams           = 1.5f,
                    slamGroundedDuration       = 8f,
                    critSpotVulnerableWindow   = 6f,
                    critRequiresSoulRealm      = true,
                    useShieldedHands           = true,
                    overrideSoulRealmFreezePolicy = false
                })
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (phaseDefinitions == null || phaseDefinitions.Length < 2)
                return;

            for (int i = 0; i < phaseDefinitions.Length - 1; i++)
            {
                var cur = phaseDefinitions[i];
                var next = phaseDefinitions[i + 1];
                if (cur == null || next == null)
                    continue;

                float nextExit = next.exitSoulPercentThreshold;
                if (nextExit <= 0f)
                    continue;

                if (cur.exitSoulPercentThreshold <= nextExit)
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
