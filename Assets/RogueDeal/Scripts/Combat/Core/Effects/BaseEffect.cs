using UnityEngine;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Combat.Core.Effects
{
    /// <summary>
    /// Base class for all combat effects. All effects are data-driven via ScriptableObjects.
    /// Effects define WHAT happens, animations define WHEN.
    /// </summary>
    public abstract class BaseEffect : ScriptableObject
    {
        [Header("Effect Info")]
        [Tooltip("Display name for this effect")]
        public string effectName;
        
        [Tooltip("Description of what this effect does")]
        [TextArea(2, 4)]
        public string description;
        
        /// <summary>
        /// Calculates the final effect values based on attacker, target, and weapon.
        /// This is where all damage calculations, stat scaling, etc. happen.
        /// </summary>
        public abstract CalculatedEffect Calculate(
            CombatEntityData attacker,
            CombatEntityData target,
            Weapon weapon
        );
        
        /// <summary>
        /// Applies the calculated effect to the target.
        /// This is where the effect actually modifies the target's state.
        /// </summary>
        public abstract void Apply(CombatEntityData target, CalculatedEffect calculated);
        
        /// <summary>
        /// Helper method to check if a critical hit occurs
        /// </summary>
        protected bool CheckCritical(CombatEntityData attacker)
        {
            return UnityEngine.Random.value < attacker.GetCritChance();
        }
        
        /// <summary>
        /// Helper method to apply critical damage multiplier
        /// </summary>
        protected float ApplyCriticalMultiplier(float damage, bool wasCritical, CombatEntityData attacker)
        {
            if (wasCritical)
            {
                return damage * (1f + attacker.GetCritDamage());
            }
            return damage;
        }
    }
}

