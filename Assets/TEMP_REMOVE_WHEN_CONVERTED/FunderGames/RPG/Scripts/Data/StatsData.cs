using System.Collections.Generic;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(menuName = "FunderGames/Stats Data", fileName = "StatsData")]
    public class StatsData : ScriptableObject
    {
        [SerializeField] private List<StatData> stats = new();
        public IReadOnlyCollection<StatData> Stats => stats;
        public StatData GetStatByType(StatType type)
        {
            return stats.Find(stat => stat.Type == type);
        }
    }
}