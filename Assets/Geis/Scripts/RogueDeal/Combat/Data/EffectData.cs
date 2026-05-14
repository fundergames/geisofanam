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
using RogueDeal.Player;

namespace RogueDeal.Combat
{
    [CreateAssetMenu(fileName = "NewEffect", menuName = "Funder Games/Geis/Rogue Deal/Combat/Effect")]
    public class EffectData : ScriptableObject
    {
        [Header("Effect Settings")]
        public EffectType effectType;
        public float baseValue;
        public StatType scalingStat;
        public float scalingMultiplier = 1f;
        public float duration;
        public bool canCrit = true;
        
        public float CalculateFinalValue(CombatStats attackerStats, CombatStats defenderStats)
        {
            float value = baseValue;
            
            if (scalingStat != StatType.Experience && attackerStats != null)
            {
                value += attackerStats.GetStat(scalingStat) * scalingMultiplier;
            }
            
            if (effectType == EffectType.Damage && defenderStats != null)
            {
                value = Mathf.Max(0, value - defenderStats.GetStat(StatType.Defense));
            }
            
            if (canCrit && attackerStats != null && Random.value < attackerStats.GetCritChance())
            {
                value *= 1f + attackerStats.GetCritDamage();
            }
            
            return value;
        }
    }
}
