using System.Collections.Generic;

namespace FunderGames.RPG
{
    public class HeroStats
    {
        public HeroData HeroData { get; private set; } 
        private readonly Dictionary<StatType, int> _currentStats; // Keeps track of the current values of each stat

        public HeroStats(HeroData data)
        {
            HeroData = data;
            _currentStats = new Dictionary<StatType, int>();

            // Initialize current stats based on HeroData's starting values
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
                _currentStats[StatType.Health] = 0; // Prevent negative health
            }
        }

        public void Heal(int amount)
        {
            if (!_currentStats.ContainsKey(StatType.Health)) return;
            _currentStats[StatType.Health] += amount;
            var maxHealth = HeroData.StatList.GetStatByType(StatType.Health).Amount;
            if (_currentStats[StatType.Health] > maxHealth)
            {
                _currentStats[StatType.Health] = maxHealth; // Prevent overhealing
            }
        }

        // Example: Modify any stat during gameplay
        public void ModifyStat(StatType type, int amount)
        {
            if (_currentStats.ContainsKey(type))
            {
                _currentStats[type] += amount;
            }
        }

        // Example: Get current value of a stat
        public int GetCurrentStat(StatType type)
        {
            return _currentStats.GetValueOrDefault(type, 0);
        }
    }
}