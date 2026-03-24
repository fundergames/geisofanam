using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RogueDeal.Combat.StatusEffects;
using RogueDeal.Combat.Core.Cooldowns;
using RogueDeal.Player;

namespace RogueDeal.Combat.Core.Data
{
    /// <summary>
    /// Pure C# data class for combat entities. No Unity dependencies.
    /// Used for simulation and as the source of truth for combat state.
    /// </summary>
    [Serializable]
    public class CombatEntityData
    {
        // Core Stats
        public float maxHealth;
        public float currentHealth;
        public float attack;
        public float defense;
        public float magicPower;
        public float speed;
        public float critChance = 0.1f;
        public float critDamage = 0.5f;
        
        // Equipment & Configuration
        public Weapon equippedWeapon;
        public CharacterClass characterClass;
        public CombatProfile combatProfile;
        
        // Status Effects
        public List<ActiveStatusEffect> activeStatusEffects = new List<ActiveStatusEffect>();
        
        // Cooldowns (managed by ActionCooldownManager, stored here for serialization)
        public Dictionary<string, CooldownState> actionCooldowns = new Dictionary<string, CooldownState>();
        
        // Position (for simulation and movement calculations)
        public UnityEngine.Vector3 position;
        public UnityEngine.Vector3 originPosition; // For return-to-origin movement
        
        public CombatEntityData()
        {
        }
        
        public CombatEntityData(float maxHealth, float attack, float defense, float magicPower = 0f, float speed = 5f)
        {
            this.maxHealth = maxHealth;
            this.currentHealth = maxHealth;
            this.attack = attack;
            this.defense = defense;
            this.magicPower = magicPower;
            this.speed = speed;
        }
        
        /// <summary>
        /// Creates a deep copy for simulation independence
        /// </summary>
        public CombatEntityData Clone()
        {
            return new CombatEntityData
            {
                maxHealth = this.maxHealth,
                currentHealth = this.currentHealth,
                attack = this.attack,
                defense = this.defense,
                magicPower = this.magicPower,
                speed = this.speed,
                critChance = this.critChance,
                critDamage = this.critDamage,
                equippedWeapon = this.equippedWeapon, // Reference, not cloned (ScriptableObject)
                characterClass = this.characterClass,
                combatProfile = this.combatProfile, // Reference, not cloned
                activeStatusEffects = this.activeStatusEffects.Select(e => e.Clone()).ToList(),
                actionCooldowns = new Dictionary<string, CooldownState>(),
                position = this.position,
                originPosition = this.originPosition
            };
        }
        
        /// <summary>
        /// Gets a stat value by type
        /// </summary>
        public float GetStat(StatType statType)
        {
            return statType switch
            {
                StatType.Health => currentHealth,
                StatType.Attack => attack,
                StatType.Defense => defense,
                StatType.Magic => magicPower,
                StatType.Speed => speed,
                StatType.Damage => attack, // Alias for attack
                _ => 0f
            };
        }
        
        /// <summary>
        /// Sets a stat value by type
        /// </summary>
        public void SetStat(StatType statType, float value)
        {
            switch (statType)
            {
                case StatType.Health:
                    currentHealth = Mathf.Clamp(value, 0, maxHealth);
                    break;
                case StatType.Attack:
                    attack = value;
                    break;
                case StatType.Defense:
                    defense = value;
                    break;
                case StatType.Magic:
                    magicPower = value;
                    break;
                case StatType.Speed:
                    speed = value;
                    break;
            }
        }
        
        /// <summary>
        /// Processes status effects at the start of a turn
        /// </summary>
        public void OnTurnStart()
        {
            var effectsToRemove = new List<ActiveStatusEffect>();
            
            foreach (var effect in activeStatusEffects)
            {
                // Process effect (damage, healing, etc.)
                effect.ProcessTurn(this);
                
                // Decrement duration
                if (!effect.isPermanent)
                {
                    effect.duration--;
                    if (effect.duration <= 0)
                    {
                        effectsToRemove.Add(effect);
                    }
                }
            }
            
            // Remove expired effects
            foreach (var effect in effectsToRemove)
            {
                activeStatusEffects.Remove(effect);
            }
        }
        
        /// <summary>
        /// Applies damage to this entity
        /// </summary>
        public void TakeDamage(float amount)
        {
            currentHealth = Mathf.Max(0, currentHealth - amount);
        }
        
        /// <summary>
        /// Heals this entity
        /// </summary>
        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
        
        /// <summary>
        /// Checks if entity is alive
        /// </summary>
        public bool IsAlive => currentHealth > 0;
        
        /// <summary>
        /// Gets critical hit chance
        /// </summary>
        public float GetCritChance()
        {
            return critChance;
        }
        
        /// <summary>
        /// Gets critical hit damage multiplier
        /// </summary>
        public float GetCritDamage()
        {
            return critDamage;
        }
    }
    
    /// <summary>
    /// Active status effect instance on an entity
    /// </summary>
    [Serializable]
    public class ActiveStatusEffect
    {
        public StatusEffectType type;
        public int stacks;
        public int damagePerStack;
        public ElementalType element;
        public int duration; // Turns remaining
        public bool isPermanent;
        
        // Stat modifiers (if this is a stat-modifying effect)
        public StatModifierData statModifier;
        
        public ActiveStatusEffect Clone()
        {
            return new ActiveStatusEffect
            {
                type = this.type,
                stacks = this.stacks,
                damagePerStack = this.damagePerStack,
                element = this.element,
                duration = this.duration,
                isPermanent = this.isPermanent,
                statModifier = this.statModifier?.Clone()
            };
        }
        
        /// <summary>
        /// Processes this effect for one turn
        /// </summary>
        public void ProcessTurn(CombatEntityData entity)
        {
            // Apply damage/healing per stack
            if (damagePerStack != 0)
            {
                float totalAmount = damagePerStack * stacks;
                if (totalAmount > 0)
                {
                    entity.TakeDamage(totalAmount);
                }
                else if (totalAmount < 0)
                {
                    entity.Heal(-totalAmount);
                }
            }
            
            // Apply stat modifiers
            if (statModifier != null)
            {
                statModifier.Apply(entity);
            }
        }
    }
    
    /// <summary>
    /// Stat modifier data for status effects
    /// </summary>
    [Serializable]
    public class StatModifierData
    {
        public StatType targetStat;
        public ModifierType modifierType;
        public float value;
        
        public StatModifierData Clone()
        {
            return new StatModifierData
            {
                targetStat = this.targetStat,
                modifierType = this.modifierType,
                value = this.value
            };
        }
        
        public void Apply(CombatEntityData entity)
        {
            float currentValue = entity.GetStat(targetStat);
            float newValue = modifierType switch
            {
                ModifierType.Add => currentValue + value,
                ModifierType.Multiply => currentValue * value,
                ModifierType.Set => value,
                _ => currentValue
            };
            
            entity.SetStat(targetStat, newValue);
        }
    }
    
    /// <summary>
    /// Type of stat modification
    /// </summary>
    public enum ModifierType
    {
        Add,
        Multiply,
        Set
    }
}

