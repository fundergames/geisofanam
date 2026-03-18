using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Player;

namespace RogueDeal.Combat.Core.Effects
{
    /// <summary>
    /// Effect that modifies a stat. Can be instant or over time (status effect).
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_StatModifier_", menuName = "RogueDeal/Combat/Effects/Stat Modifier Effect")]
    public class StatModifierEffect : BaseEffect
    {
        [Header("Stat Modification")]
        [Tooltip("Which stat to modify")]
        public StatType targetStat;
        
        [Tooltip("How to modify the stat (Add, Multiply, Set)")]
        public ModifierType modifierType = ModifierType.Add;
        
        [Tooltip("Value to apply (interpreted based on modifier type)")]
        public float baseValue;
        
        [Header("Duration")]
        [Tooltip("If true, applies instantly. If false, creates a status effect with duration")]
        public bool isInstant = true;
        
        [Tooltip("Duration in turns (only used if isInstant is false)")]
        public int duration = 3;
        
        public override CalculatedEffect Calculate(
            CombatEntityData attacker,
            CombatEntityData target,
            Weapon weapon
        )
        {
            var modifier = new StatModifierData
            {
                targetStat = targetStat,
                modifierType = modifierType,
                value = baseValue
            };
            
            return new CalculatedEffect
            {
                effectType = isInstant ? EffectType.Buff : EffectType.StatusEffect,
                statModifier = modifier,
                source = attacker
            };
        }
        
        public override void Apply(CombatEntityData target, CalculatedEffect calculated)
        {
            if (target == null || calculated?.statModifier == null) return;
            
            if (isInstant)
            {
                // Apply immediately
                calculated.statModifier.Apply(target);
            }
            else
            {
                // Add as status effect
                var statusEffect = new ActiveStatusEffect
                {
                    type = StatusEffectType.Blessed, // Default, can be customized
                    duration = duration,
                    isPermanent = false,
                    statModifier = calculated.statModifier.Clone()
                };
                
                target.activeStatusEffects.Add(statusEffect);
            }
        }
    }
}

