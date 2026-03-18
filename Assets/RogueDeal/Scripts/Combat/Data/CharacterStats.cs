using System;
using UnityEngine;

namespace RogueDeal.Combat
{
    [Serializable]
    public class CharacterStats
    {
        [Header("Core Stats")]
        public int maxHealth = 100;
        public int currentHealth = 100;
        
        [Header("Offensive Stats")]
        [Tooltip("Weapon damage modifier")]
        public int attack = 10;
        
        [Tooltip("Unarmed damage modifier")]
        public int damage = 5;
        
        [Tooltip("Spell damage modifier")]
        public int magic = 8;
        
        [Header("Defensive Stats")]
        public int defense = 5;
        
        [Header("Multipliers")]
        public float damageMultiplier = 1f;
        public float defenseMultiplier = 1f;
        
        [Header("Bonuses")]
        public int damageBonus = 0;
        public int defenseBonus = 0;
        
        [Header("Resistances")]
        [Range(0f, 1f)]
        public float fireResistance = 0f;
        
        [Range(0f, 1f)]
        public float waterResistance = 0f;
        
        [Range(0f, 1f)]
        public float woodResistance = 0f;
        
        [Range(0f, 1f)]
        public float lightResistance = 0f;
        
        [Range(0f, 1f)]
        public float darkResistance = 0f;
        
        [Header("Other")]
        [Range(0f, 1f)]
        public float dodgeChance = 0f;

        public CharacterStats Clone()
        {
            return new CharacterStats
            {
                maxHealth = this.maxHealth,
                currentHealth = this.currentHealth,
                attack = this.attack,
                damage = this.damage,
                magic = this.magic,
                defense = this.defense,
                damageMultiplier = this.damageMultiplier,
                defenseMultiplier = this.defenseMultiplier,
                damageBonus = this.damageBonus,
                defenseBonus = this.defenseBonus,
                fireResistance = this.fireResistance,
                waterResistance = this.waterResistance,
                woodResistance = this.woodResistance,
                lightResistance = this.lightResistance,
                darkResistance = this.darkResistance,
                dodgeChance = this.dodgeChance
            };
        }

        public float GetResistance(ElementalType element)
        {
            return element switch
            {
                ElementalType.Fire => fireResistance,
                ElementalType.Water => waterResistance,
                ElementalType.Wood => woodResistance,
                ElementalType.Light => lightResistance,
                ElementalType.Dark => darkResistance,
                _ => 0f
            };
        }

        public void SetResistance(ElementalType element, float value)
        {
            value = Mathf.Clamp01(value);
            switch (element)
            {
                case ElementalType.Fire:
                    fireResistance = value;
                    break;
                case ElementalType.Water:
                    waterResistance = value;
                    break;
                case ElementalType.Wood:
                    woodResistance = value;
                    break;
                case ElementalType.Light:
                    lightResistance = value;
                    break;
                case ElementalType.Dark:
                    darkResistance = value;
                    break;
            }
        }
    }
}
