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

using System;
using Funder.Core.Randoms;
using UnityEngine;

namespace RogueDeal.Combat
{
    [Serializable]
    public struct DamageRange
    {
        [Tooltip("Minimum damage (normal hit)")]
        public int minDamage;
        
        [Tooltip("Maximum damage (normal hit)")]
        public int maxDamage;
        
        [Tooltip("Critical damage value")]
        public int critDamage;
        
        [Tooltip("Chance to crit (0-1)")]
        [Range(0f, 1f)]
        public float critChance;

        public DamageRange(int min, int max, int crit, float critChance = 0.1f)
        {
            this.minDamage = min;
            this.maxDamage = max;
            this.critDamage = crit;
            this.critChance = critChance;
        }

        public int RollDamage(IRandomStream stream)
        {
            if (stream.NextFloat01() < critChance)
            {
                return critDamage;
            }
            return stream.NextInt(minDamage, maxDamage + 1);
        }
    }
}
