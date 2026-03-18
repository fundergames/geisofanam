using RogueDeal.Combat;
using RogueDeal.Combat.StatusEffects;
using RogueDeal.Items;
using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Enemies
{
    [CreateAssetMenu(fileName = "Enemy_", menuName = "Funder Games/Rogue Deal/Enemies/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public string enemyId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public GameObject modelPrefab;
        
        [Header("Base Stats (World 1)")]
        public CharacterStats baseStats;
        
        [Header("Scaling")]
        [Tooltip("Stat multiplier per world level")]
        public float statsPerWorldMultiplier = 1.5f;
        
        [Header("Combat Behavior")]
        public int attackDamage = 10;
        public DamageType attackType = DamageType.Physical;
        public float attackDelay = 1f;
        
        [Header("Special Properties")]
        public List<EnemyAbility> abilities = new List<EnemyAbility>();
        public List<StatusEffectDefinition> immunities = new List<StatusEffectDefinition>();
        
        [Header("Loot")]
        public LootTable lootTable;
        public int baseGoldDrop = 10;
        public int baseXPReward = 50;
        
        [Header("Visual")]
        public Vector3 spawnOffset = Vector3.zero;
        public float scale = 1f;

        public CharacterStats GetScaledStats(int worldLevel)
        {
            var stats = baseStats.Clone();
            float multiplier = Mathf.Pow(statsPerWorldMultiplier, worldLevel - 1);
            
            stats.maxHealth = Mathf.RoundToInt(stats.maxHealth * multiplier);
            stats.currentHealth = stats.maxHealth;
            stats.attack = Mathf.RoundToInt(stats.attack * multiplier);
            stats.defense = Mathf.RoundToInt(stats.defense * multiplier);
            
            return stats;
        }

        public int GetScaledAttackDamage(int worldLevel)
        {
            float multiplier = Mathf.Pow(statsPerWorldMultiplier, worldLevel - 1);
            return Mathf.RoundToInt(attackDamage * multiplier);
        }

        public int GetScaledGold(int worldLevel)
        {
            float multiplier = Mathf.Pow(statsPerWorldMultiplier, worldLevel - 1);
            return Mathf.RoundToInt(baseGoldDrop * multiplier);
        }

        public int GetScaledXP(int worldLevel)
        {
            float multiplier = Mathf.Pow(statsPerWorldMultiplier, worldLevel - 1);
            return Mathf.RoundToInt(baseXPReward * multiplier);
        }
    }

    [System.Serializable]
    public class EnemyAbility
    {
        public string abilityName;
        public string description;
        public EnemyAbilityType abilityType;
        public float value;
    }

    public enum EnemyAbilityType
    {
        ImmuneToElement,
        ResistElement,
        HalfDamageFromWeaponType,
        CounterAttack,
        Regeneration,
        Thorns
    }
}
