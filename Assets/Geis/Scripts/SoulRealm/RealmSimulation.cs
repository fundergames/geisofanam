using System.Collections;
using UnityEngine;

namespace Geis.SoulRealm
{
    public enum RealmSimulationGroup
    {
        Physical,
        Soul,
        Universal
    }

    /// <summary>
    /// Realm-scoped simulation helpers for systems that are not covered by the freeze registry
    /// (coroutines, particles, and any Time.deltaTime-driven loops).
    /// </summary>
    public static class RealmSimulation
    {
        public static bool IsSimulating(RealmSimulationGroup group)
        {
            bool inSoul = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
            return group switch
            {
                RealmSimulationGroup.Universal => true,
                RealmSimulationGroup.Physical  => !inSoul,
                RealmSimulationGroup.Soul      => inSoul,
                _                              => true,
            };
        }

        public static float DeltaTime(RealmSimulationGroup group) =>
            IsSimulating(group) ? Time.deltaTime : 0f;

        /// <summary>
        /// Waits in "realm time": the timer does not advance while the given realm group is not simulating.
        /// </summary>
        public static IEnumerator WaitForSecondsRealm(RealmSimulationGroup group, float seconds)
        {
            if (seconds <= 0f)
                yield break;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += DeltaTime(group);
                yield return null;
            }
        }
    }
}
