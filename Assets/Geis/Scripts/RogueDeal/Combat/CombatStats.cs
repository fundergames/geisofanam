using System;
using UnityEngine;
using RogueDeal.Player;

namespace RogueDeal.Combat
{
    public class CombatStats
    {
        private const float DEFAULT_CRIT_CHANCE = 0.1f;
        private const float DEFAULT_CRIT_DAMAGE = 0.5f;

        private float maxHealth;
        private float currentHealth;
        private float attack;
        private float defense;
        private float speed;
        private float critChance = DEFAULT_CRIT_CHANCE;
        private float critDamage = DEFAULT_CRIT_DAMAGE;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        public CombatStats(StatsData baseStats)
        {
            if (baseStats == null)
            {
                Debug.LogWarning("CombatStats initialized with null StatsData, using defaults");
                maxHealth = 100f;
                currentHealth = maxHealth;
                attack = 10f;
                defense = 5f;
                speed = 5f;
                return;
            }

            StatData healthStat = baseStats.GetStatByType(StatType.Health);
            StatData attackStat = baseStats.GetStatByType(StatType.Attack);
            StatData defenseStat = baseStats.GetStatByType(StatType.Defense);
            StatData speedStat = baseStats.GetStatByType(StatType.Speed);

            maxHealth = healthStat != null ? healthStat.Amount : 100f;
            currentHealth = maxHealth;
            attack = attackStat != null ? attackStat.Amount : 10f;
            defense = defenseStat != null ? defenseStat.Amount : 5f;
            speed = speedStat != null ? speedStat.Amount : 5f;
        }
        
        public CombatStats(float maxHealth, float attackValue, float defenseValue, float speedValue = 5f)
        {
            this.maxHealth = maxHealth;
            this.currentHealth = maxHealth;
            this.attack = attackValue;
            this.defense = defenseValue;
            this.speed = speedValue;
        }

        public float GetStat(StatType statType)
        {
            return statType switch
            {
                StatType.Health => currentHealth,
                StatType.Attack => attack,
                StatType.Defense => defense,
                StatType.Speed => speed,
                StatType.Damage => attack,
                _ => 0f
            };
        }

        public float GetCritChance()
        {
            return critChance;
        }

        public float GetCritDamage()
        {
            return critDamage;
        }

        public void TakeDamage(float amount)
        {
            currentHealth = Mathf.Max(0, currentHealth - amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            if (currentHealth <= 0)
            {
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public bool IsAlive => currentHealth > 0;
        
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
    }
}
