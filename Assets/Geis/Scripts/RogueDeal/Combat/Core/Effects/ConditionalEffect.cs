using UnityEngine;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Combat.Core.Effects
{
    /// <summary>
    /// Effect that applies different effects based on a condition.
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_Conditional_", menuName = "RogueDeal/Combat/Effects/Conditional Effect")]
    public class ConditionalEffect : BaseEffect
    {
        [Header("Condition")]
        [Tooltip("Type of condition to check")]
        public ConditionType conditionType;
        
        [Tooltip("Threshold value for condition (e.g., health percentage)")]
        public float conditionThreshold = 0.3f;
        
        [Header("Effects")]
        [Tooltip("Effect to apply if condition is true")]
        public BaseEffect effectIfTrue;
        
        [Tooltip("Effect to apply if condition is false (optional)")]
        public BaseEffect effectIfFalse;
        
        public override CalculatedEffect Calculate(
            CombatEntityData attacker,
            CombatEntityData target,
            Weapon weapon
        )
        {
            bool conditionMet = CheckCondition(attacker, target);
            BaseEffect effectToUse = conditionMet ? effectIfTrue : effectIfFalse;
            
            if (effectToUse == null)
            {
                return new CalculatedEffect { effectType = EffectType.Damage, damageAmount = 0 };
            }
            
            return effectToUse.Calculate(attacker, target, weapon);
        }
        
        public override void Apply(CombatEntityData target, CalculatedEffect calculated)
        {
            // The calculated effect already contains the result from the selected effect
            // We just need to apply it, but we need to know which effect was used
            // For simplicity, we'll apply the calculated effect directly
            // In a more complex system, we might need to track which effect was used
        }
        
        private bool CheckCondition(CombatEntityData attacker, CombatEntityData target)
        {
            return conditionType switch
            {
                ConditionType.OnLowHealth => target.currentHealth / target.maxHealth <= conditionThreshold,
                ConditionType.OnHighHealth => target.currentHealth / target.maxHealth >= conditionThreshold,
                ConditionType.OnCrit => false, // Would need to be set during calculation
                ConditionType.OnKill => false, // Would need to be checked after damage
                _ => false
            };
        }
    }
    
    /// <summary>
    /// Types of conditions for conditional effects
    /// </summary>
    public enum ConditionType
    {
        OnLowHealth,
        OnHighHealth,
        OnCrit,
        OnKill
    }
}

