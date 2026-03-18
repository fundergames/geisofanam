using System;
using UnityEngine;

namespace RogueDeal.Combat
{
    [Serializable]
    public class ClassAbility
    {
        public string abilityName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        
        [Header("Unlock")]
        public int requiredLevel = 1;
        
        [Header("Passive Stat Modifiers")]
        public bool isPassive = true;
        public float healthModifier = 0f;
        public float attackModifier = 0f;
        public float damageModifier = 0f;
        public float magicModifier = 0f;
        public float defenseModifier = 0f;
        
        [Header("Damage Bonuses")]
        public float bonusDamageMultiplier = 1f;

        [Header("Deck Manipulation")]
        public bool grantsDeckManipulation = false;
        public int wildCardsGranted = 0;
        public int usesPerCombat = 0;
        
        [Header("Combat Abilities")]
        public bool triggersOnKill = false;
        public int healOnKillPercent = 0;
        public bool triggersOnCombatStart = false;
        public float firstAttackDamageMultiplier = 1f;

        public void ApplyPassiveStats(CharacterStats stats)
        {
            if (!isPassive) return;

            stats.maxHealth += Mathf.RoundToInt(stats.maxHealth * healthModifier);
            stats.attack += Mathf.RoundToInt(stats.attack * attackModifier);
            stats.damage += Mathf.RoundToInt(stats.damage * damageModifier);
            stats.magic += Mathf.RoundToInt(stats.magic * magicModifier);
            stats.defense += Mathf.RoundToInt(stats.defense * defenseModifier);
        }
    }
}
