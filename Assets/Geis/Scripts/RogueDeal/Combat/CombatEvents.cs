using System;
using UnityEngine;

namespace RogueDeal.Combat
{
    public class CombatEventData
    {
        public CombatEntity source;
        public CombatEntity target;
        public AbilityData ability;
        public EffectData effect;
        public float damageAmount;
        public bool wasCritical;
        /// <summary>True when the hit connected but dealt no damage due to immunity / invulnerability (UI shows "Immune").</summary>
        public bool wasImmune;
        /// <summary>
        /// When true, CritSpot/BossPart-style interceptors should not heal or reinterpret HP — damage was applied to another pool (e.g. soul shield).
        /// </summary>
        public bool skipEntityDamageInterceptors;
        public Vector3 hitPosition;
    }

    public static class CombatEvents
    {
        public static event Action<CombatEventData> OnAttackStarted;
        public static event Action<CombatEventData> OnAttackConnected;
        public static event Action<CombatEventData> OnDamageCalculated;
        public static event Action<CombatEventData> OnDamageApplied;
        public static event Action<CombatEventData> OnHitReactionStarted;
        public static event Action<CombatEventData> OnAttackCompleted;
        
        public static void TriggerAttackStarted(CombatEventData data)
        {
            OnAttackStarted?.Invoke(data);
        }

        public static void TriggerAttackConnected(CombatEventData data)
        {
            OnAttackConnected?.Invoke(data);
        }

        public static void TriggerDamageCalculated(CombatEventData data)
        {
            OnDamageCalculated?.Invoke(data);
        }

        public static void TriggerDamageApplied(CombatEventData data)
        {
            OnDamageApplied?.Invoke(data);
        }

        public static void TriggerHitReactionStarted(CombatEventData data)
        {
            OnHitReactionStarted?.Invoke(data);
        }

        public static void TriggerAttackCompleted(CombatEventData data)
        {
            OnAttackCompleted?.Invoke(data);
        }

        public static void ClearAllEvents()
        {
            OnAttackStarted = null;
            OnAttackConnected = null;
            OnDamageCalculated = null;
            OnDamageApplied = null;
            OnHitReactionStarted = null;
            OnAttackCompleted = null;
        }
    }
}
