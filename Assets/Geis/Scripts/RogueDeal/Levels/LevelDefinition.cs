using RogueDeal.Enemies;
using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Levels
{
    [CreateAssetMenu(fileName = "Level_", menuName = "Funder Games/Rogue Deal/Levels/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        [Header("Level Identity")]
        public string levelId;
        public int worldNumber = 1;
        public int levelNumber = 1;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite levelIcon;
        
        [Header("Requirements")]
        public int requiredPlayerLevel = 1;
        public int energyCost = 1;
        public LevelDefinition prerequisiteLevel;
        
        [Header("Combat Settings")]
        public int totalTurns = 10;
        public List<EnemySpawn> enemySpawns = new List<EnemySpawn>();
        
        [Header("Objectives")]
        public List<LevelObjective> objectives = new List<LevelObjective>();
        
        [Header("Rewards")]
        public int baseGoldReward = 100;
        public int baseXPReward = 50;
        
        [Header("Star Thresholds")]
        [Tooltip("Turns remaining for 2 stars")]
        public int twoStarTurnsRemaining = 5;
        [Tooltip("Turns remaining for 3 stars")]
        public int threeStarTurnsRemaining = 8;
        
        [Header("Scene")]
        public string combatSceneName = "Combat";
        public Vector3[] enemyPositions;

        public int CalculateStars(int turnsUsed, bool allObjectivesComplete)
        {
            if (!allObjectivesComplete)
                return 0;

            int turnsRemaining = totalTurns - turnsUsed;
            
            if (turnsRemaining >= threeStarTurnsRemaining)
                return 3;
            if (turnsRemaining >= twoStarTurnsRemaining)
                return 2;
            return 1;
        }

        public string GetLevelCode()
        {
            return $"{worldNumber}-{levelNumber}";
        }
    }

    [System.Serializable]
    public class EnemySpawn
    {
        public EnemyDefinition enemy;
        public int positionIndex = 0;
        public bool isBoss = false;
    }
}
