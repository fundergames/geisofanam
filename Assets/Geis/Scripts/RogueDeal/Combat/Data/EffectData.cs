using UnityEngine;
using RogueDeal.Player;

namespace RogueDeal.Combat
{
    [CreateAssetMenu(fileName = "NewEffect", menuName = "RogueDeal/Combat/Effect")]
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
