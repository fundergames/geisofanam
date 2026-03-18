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

