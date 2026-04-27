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

using RogueDeal.Combat.StatusEffects;
using UnityEngine;

namespace RogueDeal.Items
{
    [CreateAssetMenu(fileName = "Consumable_", menuName = "Funder Games/Rogue Deal/Items/Consumable")]
    public class ConsumableItem : BaseItem
    {
        [Header("Consumable Properties")]
        public int healthRestore = 0;
        public int energyRestore = 0;
        
        [Header("Status Effects")]
        public StatusEffectDefinition[] effectsToApply;
        
        [Header("Buffs")]
        public float duration = 0f;
        public float damageBoostPercent = 0f;
        public float defenseBoostPercent = 0f;

        public ConsumableItem()
        {
            maxStackSize = 99;
        }
    }
}
