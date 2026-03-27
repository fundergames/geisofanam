using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Phase 2 — "Enter the Soul Realm"
    ///
    /// Same slam cycle as Phase 1, but each hand lands Shielded instead of Grounded:
    ///
    ///   Right slam:
    ///     → Fist lands (Shielded) — shield glows in the Soul Realm.
    ///     → Player enters Soul Realm, destroys the shield on the right fist.
    ///     → Fist transitions to Grounded.
    ///     → Player exits Soul Realm, attacks the right fist.
    ///     → Right fist broken.
    ///
    ///   Left slam: same sequence.
    ///
    ///   Both fists broken:
    ///     → CritSpot exposed.
    ///     → Player damages CritSpot → DrainSouls().
    ///     → Cycle repeats.
    ///
    /// Phase ends when remaining souls reach 0 (boss defeated).
    ///
    /// Shields are primed via BossPart.ResetForCycle(useShields: true) so each new cycle
    /// re-arms them automatically without any additional Phase 2 logic.
    /// </summary>
    public class GiantBossPhase2 : IBossPhase
    {
        public bool IsComplete { get; private set; }

        private GiantBossController _boss;

        // ── IBossPhase ──────────────────────────────────────────────────────────────

        public void OnEnter(GiantBossController boss)
        {
            _boss = boss;
            IsComplete = false;

            CritSpot.OnCritHit += HandleCritHit;

            boss.ResetPartsForPhase(useShields: true);
            boss.StartSlamLoop();

            Debug.Log("[Phase 2] Entered — hands are shielded. Enter Soul Realm to break them!");
        }

        public void OnUpdate(GiantBossController boss)
        {
            if (boss.SoulPercent <= 0f)
                IsComplete = true;
        }

        public void OnExit(GiantBossController boss)
        {
            CritSpot.OnCritHit -= HandleCritHit;
            boss.StopSlamLoop();
            _boss = null;

            Debug.Log("[Phase 2] Exited.");
        }

        // ── Event handling ──────────────────────────────────────────────────────────

        private void HandleCritHit(CritSpot spot, float damage)
        {
            _boss?.DrainSouls(damage);
        }
    }
}
