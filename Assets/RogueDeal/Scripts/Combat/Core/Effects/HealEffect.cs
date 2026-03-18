using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Player;

namespace RogueDeal.Combat.Core.Effects
{
    /// <summary>
    /// Effect that heals the target. Supports stat scaling.
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_Heal_", menuName = "RogueDeal/Combat/Effects/Heal Effect")]
    public class HealEffect : BaseEffect
    {
        [Header("Heal Settings")]
        [Tooltip("Base heal amount")]
        public float baseHeal;
        
        [Header("Scaling")]
        [Tooltip("Which stat scales this heal (Magic, Attack, etc.)")]
        public StatType scalingStat = StatType.Magic;
        
        [Tooltip("Multiplier for stat scaling (e.g., 1.0 = 100% of stat value)")]
        public float scalingMultiplier = 1f;
        
        [Header("Critical Heals")]
        [Tooltip("Can this heal crit?")]
        public bool canCrit = false;
        
        public override CalculatedEffect Calculate(
            CombatEntityData attacker,
            CombatEntityData target,
            Weapon weapon
        )
        {
            float heal = baseHeal;
            
            // Apply stat scaling
            if (scalingStat != StatType.Experience && attacker != null)
            {
                float statValue = attacker.GetStat(scalingStat);
                heal += statValue * scalingMultiplier;
            }
            
            // Check for critical heal (if enabled)
            bool wasCritical = canCrit && attacker != null && CheckCritical(attacker);
            
            // Apply critical multiplier
            if (wasCritical && attacker != null)
            {
                heal = ApplyCriticalMultiplier(heal, wasCritical, attacker);
            }
            
            return new CalculatedEffect
            {
                effectType = EffectType.Heal,
                healAmount = heal,
                wasCritical = wasCritical,
                source = attacker
            };
        }
        
        public override void Apply(CombatEntityData target, CalculatedEffect calculated)
        {
            if (target == null || calculated == null) return;
            
            target.Heal(calculated.healAmount);
        }
    }
}
