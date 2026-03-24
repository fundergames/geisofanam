using UnityEngine;
using RogueDeal.Player;
using RogueDeal.Enemies;
using System.Collections.Generic;
using System.Reflection;

namespace RogueDeal.Combat
{
    public static class EnemyHeroDataConverter
    {
        private static Dictionary<string, HeroData> runtimeEnemyData = new Dictionary<string, HeroData>();

        public static HeroData GetOrCreateRuntimeHeroData(EnemyInstance enemy)
        {
            if (enemy == null || enemy.definition == null)
            {
                Debug.LogError("[EnemyHeroDataConverter] Enemy or definition is null");
                return null;
            }

            string key = $"{enemy.definition.enemyId}_{enemy.worldLevel}";
            
            if (runtimeEnemyData.ContainsKey(key) && runtimeEnemyData[key] != null)
            {
                return runtimeEnemyData[key];
            }

            HeroData heroData = CreateRuntimeHeroData(enemy);
            runtimeEnemyData[key] = heroData;
            
            return heroData;
        }

        private static HeroData CreateRuntimeHeroData(EnemyInstance enemy)
        {
            CharacterStats scaledStats = enemy.definition.GetScaledStats(enemy.worldLevel);
            
            HeroData heroData = ScriptableObject.CreateInstance<HeroData>();
            heroData.name = $"RuntimeEnemy_{enemy.definition.displayName}";
            
            StatsData statsData = CreateRuntimeStatsData(scaledStats);
            
            SetPrivateField(heroData, "statList", statsData);
            SetPrivateField(heroData, "playerName", enemy.definition.displayName);
            
            Debug.Log($"[EnemyHeroDataConverter] Created runtime HeroData for {enemy.definition.displayName} " +
                      $"(HP: {scaledStats.maxHealth}, Atk: {scaledStats.attack}, Def: {scaledStats.defense})");
            
            return heroData;
        }

        private static StatsData CreateRuntimeStatsData(CharacterStats stats)
        {
            StatsData statsData = ScriptableObject.CreateInstance<StatsData>();
            statsData.name = "RuntimeStats";
            
            List<StatData> statsList = new List<StatData>
            {
                CreateStatData(StatType.Health, stats.maxHealth),
                CreateStatData(StatType.Attack, stats.attack),
                CreateStatData(StatType.Defense, stats.defense),
                CreateStatData(StatType.Speed, 5f)
            };
            
            SetPrivateField(statsData, "stats", statsList);
            
            return statsData;
        }

        private static StatData CreateStatData(StatType type, float amount)
        {
            StatData statData = ScriptableObject.CreateInstance<StatData>();
            SetPrivateField(statData, "type", type);
            SetPrivateField(statData, "amount", Mathf.RoundToInt(amount));
            return statData;
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"[EnemyHeroDataConverter] Could not find field '{fieldName}' on {obj.GetType().Name}");
            }
        }

        public static void ClearCache()
        {
            foreach (var heroData in runtimeEnemyData.Values)
            {
                if (heroData != null)
                {
                    Object.Destroy(heroData);
                }
            }
            runtimeEnemyData.Clear();
        }
    }
}
