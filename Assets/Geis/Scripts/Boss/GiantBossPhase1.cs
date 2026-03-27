using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Phase 1 — "Learn the pattern"
    ///
    /// Cycle:
    ///   1. Boss alternates: slam right fist → slam left fist.
    ///   2. After each slam the fist is Grounded; player dodges then attacks it.
    ///   3. When both fists are broken, GiantBossController exposes the CritSpot.
    ///   4. Player damages the CritSpot → DrainSouls() drains from the boss soul pool.
    ///   5. Window closes → hands reset → cycle repeats with no shields.
    ///
    /// Phase ends when remaining souls drop to or below GiantBossDefinition.phase2SoulThreshold.
    ///
    /// IBossPhase contract:
    ///   OnEnter  — subscribe to events, reset parts (no shields), start slam loop.
    ///   OnUpdate — check soul threshold; set IsComplete when phase 2 should begin.
    ///   OnExit   — unsubscribe events, stop slam loop.
    /// </summary>
    public class GiantBossPhase1 : IBossPhase
    {
        public bool IsComplete { get; private set; }

        private GiantBossController _boss;

        // ── IBossPhase ──────────────────────────────────────────────────────────────

        public void OnEnter(GiantBossController boss)
        {
            _boss = boss;
            IsComplete = false;

            CritSpot.OnCritHit += HandleCritHit;

            boss.ResetPartsForPhase(useShields: false);
            boss.StartSlamLoop();

            Debug.Log("[Phase 1] Entered — no shields, slam-dodge-attack loop.");
        }

        public void OnUpdate(GiantBossController boss)
        {
            // Transition to Phase 2 when the player has released enough souls
            if (boss.SoulPercent <= boss.Definition.phase2SoulThreshold)
                IsComplete = true;
        }

        public void OnExit(GiantBossController boss)
        {
            CritSpot.OnCritHit -= HandleCritHit;
            boss.StopSlamLoop();
            _boss = null;

            Debug.Log("[Phase 1] Exited.");
        }

        // ── Event handling ──────────────────────────────────────────────────────────

        private void HandleCritHit(CritSpot spot, float damage)
        {
            _boss?.DrainSouls(damage);
        }
    }
}
