using RogueDeal.Combat;
using RogueDeal.Combat.StatusEffects;
using RogueDeal.Items;
using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Player
{
    [System.Serializable]
    public class PlayerCharacter
    {
        [Header("Identity")]
        public string characterName;
        public ClassDefinition classDefinition;
        
        [Header("Progression")]
        public int level = 1;
        public int currentXP = 0;
        
        [Header("Stats")]
        public CharacterStats baseStats;
        public CharacterStats effectiveStats;
        
        [Header("Equipment")]
        public Dictionary<EquipmentSlot, EquipmentItem> equipment = new Dictionary<EquipmentSlot, EquipmentItem>();
        
        [Header("Combat")]
        public StatusEffectManager statusEffects;
        
        [Header("Inventory")]
        public Inventory inventory;

        public PlayerCharacter(ClassDefinition classDefinition, string name = "Hero")
        {
            this.classDefinition = classDefinition;
            this.characterName = name;
            this.level = 1;
            this.currentXP = 0;
            this.baseStats = classDefinition.GetStatsForLevel(level);
            this.effectiveStats = baseStats.Clone();
            this.statusEffects = new StatusEffectManager(effectiveStats);
            this.inventory = new Inventory();
            
            InitializeEquipmentSlots();
        }

        private void InitializeEquipmentSlots()
        {
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                equipment[slot] = null;
            }
        }

        public void RecalculateStats()
        {
            effectiveStats = classDefinition.GetStatsForLevel(level);
            
            var availableAbilities = classDefinition.GetAvailableAbilities(level);
            foreach (var ability in availableAbilities)
            {
                ability.ApplyPassiveStats(effectiveStats);
            }
            
            foreach (var equipped in equipment.Values)
            {
                if (equipped != null)
                {
                    equipped.ApplyToStats(effectiveStats);
                }
            }
        }

        public bool EquipItem(EquipmentItem item)
        {
            if (item == null)
                return false;

            if (level < item.requiredLevel)
                return false;

            if (equipment.ContainsKey(item.slot) && equipment[item.slot] != null)
            {
                UnequipItem(item.slot);
            }

            equipment[item.slot] = item;
            RecalculateStats();
            return true;
        }

        public EquipmentItem UnequipItem(EquipmentSlot slot)
        {
            if (!equipment.ContainsKey(slot) || equipment[slot] == null)
                return null;

            var item = equipment[slot];
            equipment[slot] = null;
            RecalculateStats();
            return item;
        }

        public void AddXP(int xp)
        {
            currentXP += xp;
            CheckLevelUp();
        }

        private void CheckLevelUp()
        {
            int xpRequired = classDefinition.GetXPForLevel(level + 1);
            if (currentXP >= xpRequired)
            {
                level++;
                currentXP -= xpRequired;
                RecalculateStats();
                effectiveStats.currentHealth = effectiveStats.maxHealth;
                CheckLevelUp();
            }
        }

        public int TakeDamage(int damage)
        {
            float dodgeRoll = Random.value;
            if (dodgeRoll <= effectiveStats.dodgeChance)
            {
                return 0;
            }

            int finalDamage = Mathf.Max(0, damage - Mathf.RoundToInt(effectiveStats.defense * effectiveStats.defenseMultiplier));
            effectiveStats.currentHealth -= finalDamage;
            effectiveStats.currentHealth = Mathf.Max(0, effectiveStats.currentHealth);
            
            return finalDamage;
        }

        public void Heal(int amount)
        {
            effectiveStats.currentHealth += amount;
            effectiveStats.currentHealth = Mathf.Min(effectiveStats.currentHealth, effectiveStats.maxHealth);
        }

        public bool IsAlive()
        {
            return effectiveStats.currentHealth > 0;
        }

        public int ProcessStatusEffects()
        {
            return statusEffects.ProcessEffectsOnTurnStart();
        }
    }
}
