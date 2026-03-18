using System.Collections.Generic;

namespace RogueDeal.Player
{
    public class HeroStats
    {
        public HeroData HeroData { get; private set; }
        private readonly Dictionary<StatType, int> _currentStats;

        public HeroStats(HeroData data)
        {
            HeroData = data;
            _currentStats = new Dictionary<StatType, int>();

            foreach (var stat in HeroData.StatList.Stats)
            {
                _currentStats[stat.Type] = stat.Amount;
            }
        }

        public void TakeDamage(int damage)
        {
            if (!_currentStats.ContainsKey(StatType.Health)) return;
            _currentStats[StatType.Health] -= damage;
            if (_currentStats[StatType.Health] < 0)
            {
                _currentStats[StatType.Health] = 0;
            }
        }

        public void Heal(int amount)
        {
            if (!_currentStats.ContainsKey(StatType.Health)) return;
            _currentStats[StatType.Health] += amount;
            var maxHealth = HeroData.StatList.GetStatByType(StatType.Health).Amount;
            if (_currentStats[StatType.Health] > maxHealth)
            {
                _currentStats[StatType.Health] = maxHealth;
            }
        }

        public void ModifyStat(StatType type, int amount)
        {
            if (_currentStats.ContainsKey(type))
            {
                _currentStats[type] += amount;
            }
        }

        public int GetCurrentStat(StatType type)
        {
            return _currentStats.GetValueOrDefault(type, 0);
        }
    }
}
