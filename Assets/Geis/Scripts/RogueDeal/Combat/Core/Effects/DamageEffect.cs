using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Player;

namespace RogueDeal.Combat.Core.Effects
{
    /// <summary>
    /// Effect that deals damage. Supports stat scaling and weapon multipliers.
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_Damage_", menuName = "RogueDeal/Combat/Effects/Damage Effect")]
    public class DamageEffect : BaseEffect
    {
        [Header("Damage Settings")]
        [Tooltip("Base damage amount")]
        public float baseDamage;
        
        [Tooltip("Type of damage (Physical, Fire, Ice, etc.)")]
        public DamageType damageType = DamageType.Physical;
        
        [Header("Scaling")]
        [Tooltip("Which stat scales this damage (Attack, Magic, etc.)")]
        public StatType scalingStat = StatType.Attack;
        
        [Tooltip("Multiplier for stat scaling (e.g., 1.0 = 100% of stat value)")]
        public float scalingMultiplier = 1f;
        
        [Header("Critical Hits")]
        [Tooltip("Can this damage crit?")]
        public bool canCrit = true;
        
        public override CalculatedEffect Calculate(
            CombatEntityData attacker,
            CombatEntityData target,
            Weapon weapon
        )
        {
            float damage = baseDamage;
            
            // Add weapon base damage when equipped (weapons drive damage)
            if (weapon != null)
            {
                damage += weapon.baseDamage;
            }
            
            // Apply stat scaling (attack, magic, etc.)
            if (scalingStat != StatType.Experience && attacker != null)
            {
                float statValue = attacker.GetStat(scalingStat);
                damage += statValue * scalingMultiplier;
            }
            
            // Apply weapon damage type multiplier (e.g. Fire 1.2x, Ice 0.8x)
            if (weapon != null)
            {
                damage *= weapon.GetDamageTypeMultiplier(damageType);
            }
            
            // Apply defense reduction
            if (target != null)
            {
                damage = Mathf.Max(0, damage - target.defense);
            }
            
            // Check for critical hit
            bool wasCritical = canCrit && attacker != null && CheckCritical(attacker);
            
            // Apply critical multiplier
            if (wasCritical && attacker != null)
            {
                damage = ApplyCriticalMultiplier(damage, wasCritical, attacker);
            }
            
            return new CalculatedEffect
            {
                effectType = EffectType.Damage,
                damageAmount = damage,
                damageType = damageType,
                wasCritical = wasCritical,
                source = attacker
            };
        }
        
        public override void Apply(CombatEntityData target, CalculatedEffect calculated)
        {
            if (target == null || calculated == null) return;
            
            target.TakeDamage(calculated.damageAmount);
        }
    }
}

