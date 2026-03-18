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
            if (stream.Chance(critChance))
            {
                return critDamage;
            }
            return stream.NextInt(minDamage, maxDamage + 1);
        }
    }
}
