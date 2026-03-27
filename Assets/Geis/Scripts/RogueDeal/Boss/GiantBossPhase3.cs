using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Phase 3 — final stretch: same shielded-hand loop as Phase 2, tuned harder via
    /// <see cref="GiantBossDefinition"/> (slam windows, crit window, etc.).
    /// The encounter ends only when <see cref="GiantBossController.DrainSouls"/> reduces souls to 0;
    /// this phase never reports <see cref="IBossPhase.IsComplete"/> for a transition.
    /// </summary>
    public class GiantBossPhase3 : IBossPhase
    {
        public bool IsComplete => false;

        private GiantBossController _boss;

        public void OnEnter(GiantBossController boss)
        {
            _boss = boss;

            CritSpot.OnCritHit += HandleCritHit;

            boss.ResetPartsForPhase(useShields: true);
            boss.StartSlamLoop();

            Debug.Log("[Phase 3] Entered — final phase.");
        }

        public void OnUpdate(GiantBossController boss)
        {
        }

        public void OnExit(GiantBossController boss)
        {
            CritSpot.OnCritHit -= HandleCritHit;
            boss.StopSlamLoop();
            _boss = null;

            Debug.Log("[Phase 3] Exited.");
        }

        private void HandleCritHit(CritSpot spot, float damage)
        {
            _boss?.DrainSouls(damage);
        }
    }
}
