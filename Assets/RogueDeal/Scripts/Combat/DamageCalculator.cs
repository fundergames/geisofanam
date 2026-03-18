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
            DamageRange damageRange,
            float damageMultiplier,
            DamageType damageType)
        {
            if (randomHub == null)
            {
                Debug.LogError("RandomHub cannot be null. Provide an IRandomHub instance.");
                return default;
            }

            var stream = randomHub.GetStream("Combat/Damage");

            int baseDamage = damageRange.RollDamage(stream);
            bool isCrit = baseDamage == damageRange.critDamage;

            int statModifier = GetStatModifier(player.effectiveStats, damageType);

            int finalDamage = Mathf.RoundToInt((baseDamage + statModifier + player.effectiveStats.damageBonus) * damageMultiplier * player.effectiveStats.damageMultiplier);

            return new DamageResult
            {
                damage = finalDamage,
                isCrit = isCrit,
                baseDamage = baseDamage,
                statModifier = statModifier,
                totalMultiplier = damageMultiplier * player.effectiveStats.damageMultiplier,
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
