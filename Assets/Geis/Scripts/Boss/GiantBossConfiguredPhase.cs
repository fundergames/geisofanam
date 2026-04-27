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

namespace RogueDeal.Boss
{
    /// <summary>
    /// Phase behaviour driven entirely by <see cref="GiantBossPhaseDefinition"/> on the definition.
    /// Replaces separate Phase1/2/3 classes so new phases are data-only.
    /// </summary>
    public sealed class GiantBossConfiguredPhase : IBossPhase
    {
        private readonly int _phaseIndex1Based;
        private GiantBossController _boss;

        public GiantBossConfiguredPhase(int phaseIndex1Based)
        {
            _phaseIndex1Based = phaseIndex1Based;
        }

        public bool IsComplete { get; private set; }

        public void OnEnter(GiantBossController boss)
        {
            _boss = boss;
            IsComplete = false;

            CritSpot.OnCritHit += HandleCritHit;

            var data = boss.Definition.GetPhaseData(_phaseIndex1Based);
            boss.ResetPartsForPhase(data.useShieldedHands);
            boss.StartSlamLoop();

            Debug.Log($"[GiantBoss] Phase {_phaseIndex1Based} entered (shielded hands={data.useShieldedHands}).");
        }

        public void OnUpdate(GiantBossController boss)
        {
            var def = boss.Definition;
            int count = def.PhaseCount;

            // Last phase: never auto-advance (even if exit threshold is mis-set).
            if (_phaseIndex1Based >= count)
            {
                IsComplete = false;
                return;
            }

            var data = def.GetPhaseData(_phaseIndex1Based);
            if (data.exitSoulPercentThreshold > 0f && boss.SoulPercent <= data.exitSoulPercentThreshold)
                IsComplete = true;
        }

        public void OnExit(GiantBossController boss)
        {
            CritSpot.OnCritHit -= HandleCritHit;
            boss.StopSlamLoop();
            _boss = null;

            Debug.Log($"[GiantBoss] Phase {_phaseIndex1Based} exited.");
        }

        private void HandleCritHit(CritSpot spot, float damage)
        {
            // Unity destroyed-object check: null-conditional doesn't respect Unity's "fake null".
            if (_boss == null)
                return;
            _boss.DrainSouls(damage);
        }
    }
}
