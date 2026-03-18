using UnityEngine;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Combat.Core.Effects
{
    /// <summary>
    /// Effect that applies a status effect (burn, poison, regen, etc.) with duration.
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_Status_", menuName = "RogueDeal/Combat/Effects/Status Effect")]
    public class StatusEffect : BaseEffect
    {
        [Header("Status Effect Settings")]
        [Tooltip("Type of status effect")]
        public StatusEffectType statusType;
        
        [Tooltip("Number of stacks to apply")]
        public int stacks = 1;
        
        [Tooltip("Damage/healing per stack per turn (positive = damage, negative = healing)")]
        public int damagePerStack = 5;
        
        [Tooltip("Duration in turns")]
        public int duration = 3;
        
        [Tooltip("Is this effect permanent?")]
        public bool isPermanent = false;
        
        [Tooltip("Associated elemental type")]
        public ElementalType element = ElementalType.None;
        
        [Header("Optional Stat Modifier")]
        [Tooltip("Optional stat modifier to apply with this status effect")]
        public StatModifierData statModifier;
        
        public override CalculatedEffect Calculate(
            CombatEntityData attacker,
            CombatEntityData target,
            Weapon weapon
        )
        {
            var statusData = new StatusEffectData
            {
                type = statusType,
                stacks = stacks,
                damagePerStack = damagePerStack,
                element = element,
                duration = duration,
                isPermanent = isPermanent,
                statModifier = statModifier?.Clone()
            };
            
            return new CalculatedEffect
            {
                effectType = EffectType.StatusEffect,
                statusEffect = statusData,
                source = attacker
            };
        }
        
        public override void Apply(CombatEntityData target, CalculatedEffect calculated)
        {
            if (target == null || calculated?.statusEffect == null) return;
            
            // Check if status effect already exists
            var existing = target.activeStatusEffects.Find(e => e.type == calculated.statusEffect.type);
            
            if (existing != null)
            {
                // Stack or refresh duration
                existing.stacks += calculated.statusEffect.stacks;
                if (calculated.statusEffect.duration > existing.duration)
                {
                    existing.duration = calculated.statusEffect.duration;
                }
            }
            else
            {
                // Create new status effect
                var activeEffect = new ActiveStatusEffect
                {
                    type = calculated.statusEffect.type,
                    stacks = calculated.statusEffect.stacks,
                    damagePerStack = calculated.statusEffect.damagePerStack,
                    element = calculated.statusEffect.element,
                    duration = calculated.statusEffect.duration,
                    isPermanent = calculated.statusEffect.isPermanent,
                    statModifier = calculated.statusEffect.statModifier?.Clone()
                };
                
                target.activeStatusEffects.Add(activeEffect);
            }
        }
    }
}

