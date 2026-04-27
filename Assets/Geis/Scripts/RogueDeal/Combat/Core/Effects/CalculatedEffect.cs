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

using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Combat.Core.Effects
{
    /// <summary>
    /// Result of effect calculation. Contains final values ready to be applied.
    /// </summary>
    [System.Serializable]
    public class CalculatedEffect
    {
        public EffectType effectType;
        
        // Damage/Healing
        public float damageAmount;
        public float healAmount;
        public DamageType damageType;
        public bool wasCritical;
        
        // Stat Modifications
        public StatModifierData statModifier;
        
        // Status Effect
        public StatusEffectData statusEffect;
        
        // Source information
        public CombatEntityData source;
        
        public CalculatedEffect()
        {
        }
    }
    
    /// <summary>
    /// Status effect data for application
    /// </summary>
    [System.Serializable]
    public class StatusEffectData
    {
        public StatusEffectType type;
        public int stacks;
        public int damagePerStack;
        public ElementalType element;
        public int duration;
        public bool isPermanent;
        public StatModifierData statModifier;
    }
}

