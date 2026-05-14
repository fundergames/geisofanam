/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using UnityEngine;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Combat.Core.Effects
{
    /// <summary>
    /// Effect that applies multiple effects simultaneously.
    /// Useful for complex attacks (e.g., Fire Sword = Physical Damage + Fire Damage + Burn Status)
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_Multi_", menuName = "Funder Games/Geis/Rogue Deal/Combat/Effects/Multi Effect")]
    public class MultiEffect : BaseEffect
    {
        [Header("Effects")]
        [Tooltip("Array of effects to apply in sequence")]
        public BaseEffect[] effects;
        
        public override CalculatedEffect Calculate(
            CombatEntityData attacker,
            CombatEntityData target,
            Weapon weapon
        )
        {
            // For multi-effect, we calculate all effects but return a combined result
            // The actual application happens in Apply()
            float totalDamage = 0f;
            float totalHeal = 0f;
            bool anyCritical = false;
            
            foreach (var effect in effects)
            {
                if (effect == null) continue;
                
                var calculated = effect.Calculate(attacker, target, weapon);
                totalDamage += calculated.damageAmount;
                totalHeal += calculated.healAmount;
                if (calculated.wasCritical) anyCritical = true;
            }
            
            return new CalculatedEffect
            {
                effectType = EffectType.Damage, // Default, could be more complex
                damageAmount = totalDamage,
                healAmount = totalHeal,
                wasCritical = anyCritical,
                source = attacker
            };
        }
        
        public override void Apply(CombatEntityData target, CalculatedEffect calculated)
        {
            // MultiEffect requires full context (attacker, weapon) to apply correctly
            // This method is called when we only have calculated values
            // In practice, ApplyAll() should be used instead
            // For compatibility, we apply what we can from the calculated effect
            if (target == null || calculated == null) return;
            
            if (calculated.damageAmount > 0)
            {
                target.TakeDamage(calculated.damageAmount);
            }
            
            if (calculated.healAmount > 0)
            {
                target.Heal(calculated.healAmount);
            }
        }
        
        /// <summary>
        /// Applies all effects with full context (attacker, weapon)
        /// This should be called instead of Apply() when you have the full context
        /// </summary>
        public void ApplyAll(CombatEntityData attacker, CombatEntityData target, Weapon weapon)
        {
            if (target == null || effects == null) return;
            
            foreach (var effect in effects)
            {
                if (effect == null) continue;
                
                var calculated = effect.Calculate(attacker, target, weapon);
                effect.Apply(target, calculated);
            }
        }
    }
}

