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

using RogueDeal.Combat;
using RogueDeal.Combat.StatusEffects;
using UnityEngine;

namespace RogueDeal.Items
{
    [CreateAssetMenu(fileName = "Equipment_", menuName = "Funder Games/Rogue Deal/Items/Equipment")]
    public class EquipmentItem : BaseItem
    {
        [Header("Equipment Properties")]
        public EquipmentSlot slot;
        public int requiredLevel = 1;
        
        [Header("Stat Modifiers")]
        public int healthBonus = 0;
        public int attackBonus = 0;
        public int damageBonus = 0;
        public int magicBonus = 0;
        public int defenseBonus = 0;
        public float damageMultiplier = 0f;
        public float defenseMultiplier = 0f;
        
        [Header("Elemental Properties")]
        public ElementalType elementalType = ElementalType.None;
        public StatusEffectDefinition onHitEffect;
        
        [Header("Special Properties")]
        public float critChanceBonus = 0f;
        public float dodgeChanceBonus = 0f;
        
        [Header("Visual")]
        public GameObject equipmentModel;

        public void ApplyToStats(CharacterStats stats)
        {
            stats.maxHealth += healthBonus;
            stats.attack += attackBonus;
            stats.damage += damageBonus;
            stats.magic += magicBonus;
            stats.defense += defenseBonus;
            stats.damageMultiplier += damageMultiplier;
            stats.defenseMultiplier += defenseMultiplier;
            stats.dodgeChance += dodgeChanceBonus;
        }

        public void RemoveFromStats(CharacterStats stats)
        {
            stats.maxHealth -= healthBonus;
            stats.attack -= attackBonus;
            stats.damage -= damageBonus;
            stats.magic -= magicBonus;
            stats.defense -= defenseBonus;
            stats.damageMultiplier -= damageMultiplier;
            stats.defenseMultiplier -= defenseMultiplier;
            stats.dodgeChance -= dodgeChanceBonus;
        }
    }
}
