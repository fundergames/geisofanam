using Funder.Core.Randoms;
using RogueDeal.Player;
using UnityEngine;

namespace RogueDeal.Combat
{
    public static class DamageCalculator
    {
        public static DamageResult CalculateDamage(
            IRandomHub randomHub,
            PlayerCharacter player,
            PokerHandDefinition handDefinition,
            ClassAttackMapping attackMapping,
            DamageType damageType)
        {
            if (randomHub == null)
            {
                Debug.LogError("RandomHub cannot be null. Provide an IRandomHub instance.");
                return default;
            }

            var stream = randomHub.GetStream("Combat/Damage");
            
            int baseDamage = handDefinition.RollDamage(stream);
            bool isCrit = baseDamage == handDefinition.damageRange.critDamage;

            int statModifier = GetStatModifier(player.effectiveStats, damageType);
            
            float totalMultiplier = player.effectiveStats.damageMultiplier;
            if (attackMapping != null)
            {
                totalMultiplier *= attackMapping.damageMultiplier;
            }

            float classHandBonus = GetClassHandBonus(player, handDefinition.handType);
            totalMultiplier *= classHandBonus;

            int finalDamage = Mathf.RoundToInt((baseDamage + statModifier + player.effectiveStats.damageBonus) * totalMultiplier);

            return new DamageResult
            {
                damage = finalDamage,
                isCrit = isCrit,
                baseDamage = baseDamage,
                statModifier = statModifier,
                totalMultiplier = totalMultiplier,
                damageType = damageType
            };
        }

        private static int GetStatModifier(CharacterStats stats, DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Weapon => stats.attack,
                DamageType.Unarmed or DamageType.Physical => stats.damage,
                DamageType.Magic or DamageType.Fire or DamageType.Water 
                    or DamageType.Wood or DamageType.Light or DamageType.Dark => stats.magic,
                _ => stats.damage
            };
        }

        private static float GetClassHandBonus(PlayerCharacter player, PokerHandType handType)
        {
            var abilities = player.classDefinition.GetAvailableAbilities(player.level);
            float bonus = 1f;

            foreach (var ability in abilities)
            {
                if (ability.targetHand == handType)
                {
                    bonus *= ability.handDamageMultiplier;
                }
            }

            return bonus;
        }
    }

    public struct DamageResult
    {
        public int damage;
        public bool isCrit;
        public int baseDamage;
        public int statModifier;
        public float totalMultiplier;
        public DamageType damageType;
    }
}
