using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Combat
{
    [CreateAssetMenu(fileName = "Class_", menuName = "Funder Games/Rogue Deal/Combat/Class Definition")]
    public class ClassDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public CharacterClass classType;
        public string displayName;
        [TextArea(3, 5)]
        public string description;
        public Sprite classIcon;
        
        [Header("Base Stats")]
        public CharacterStats baseStats;
        
        [Header("Stat Growth Per Level")]
        public int healthPerLevel = 10;
        public int attackPerLevel = 2;
        public int damagePerLevel = 1;
        public int magicPerLevel = 2;
        public int defensePerLevel = 1;
        
        [Header("Class Abilities")]
        public List<ClassAbility> abilities = new List<ClassAbility>();
        
        [Header("Attack Mappings")]
        [Tooltip("Attack configurations for this class")]
        public List<ClassAttackMapping> attackMappings = new List<ClassAttackMapping>();
        
        [Header("Animation")]
        public Player.ClassAnimatorData animatorData;
        
        [Header("Progression")]
        public AnimationCurve xpCurve;

        public CharacterStats GetStatsForLevel(int level)
        {
            var stats = baseStats.Clone();
            int levelsGained = level - 1;
            
            stats.maxHealth += healthPerLevel * levelsGained;
            stats.attack += attackPerLevel * levelsGained;
            stats.damage += damagePerLevel * levelsGained;
            stats.magic += magicPerLevel * levelsGained;
            stats.defense += defensePerLevel * levelsGained;
            
            stats.currentHealth = stats.maxHealth;
            
            return stats;
        }

        public List<ClassAbility> GetAvailableAbilities(int level)
        {
            var available = new List<ClassAbility>();
            foreach (var ability in abilities)
            {
                if (level >= ability.requiredLevel)
                {
                    available.Add(ability);
                }
            }
            return available;
        }

        public ClassAttackMapping GetAttackMappingAt(int index)
        {
            if (attackMappings == null || index < 0 || index >= attackMappings.Count)
                return null;
            return attackMappings[index];
        }

        public int GetXPForLevel(int level)
        {
            if (xpCurve == null || xpCurve.length == 0)
            {
                return level * 100;
            }
            return Mathf.RoundToInt(xpCurve.Evaluate(level));
        }
    }
}
