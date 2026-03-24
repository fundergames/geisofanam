using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Player
{
    [CreateAssetMenu(menuName = "RogueDeal/Character/Stats Data", fileName = "StatsData")]
    public class StatsData : ScriptableObject
    {
        [SerializeField] private List<StatData> stats = new List<StatData>();
        
        public IReadOnlyCollection<StatData> Stats => stats;
        
        public StatData GetStatByType(StatType type)
        {
            return stats.Find(stat => stat.Type == type);
        }
    }
}
