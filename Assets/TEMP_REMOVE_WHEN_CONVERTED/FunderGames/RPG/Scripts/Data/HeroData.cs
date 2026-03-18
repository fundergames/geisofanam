using System.Collections.Generic;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "Hero Data", menuName = "FunderGames/Hero Data")]
    public class HeroData : ScriptableObject
    {
        [SerializeField] private string playerName;
        [SerializeField] private int level;
        [SerializeField] private float levelProgress;
        [SerializeField] private int power;
        [SerializeField] private StatsData statList;
        [SerializeField] private CharacterClassData characterClass;
        [SerializeField] private HeroVisualData heroVisualData;
        [SerializeField] private List<CombatAction> availableActions = new();
        [SerializeField] private ClassAnimatorData animatorData;
        
        // Read-only properties for external access
        public string PlayerName => playerName;
        public int Level => level;
        public float LevelProgress => levelProgress;
        public int Power => power;
        public StatsData StatList => statList;
        public CharacterClassData CharacterClass => characterClass;
        public HeroVisualData HeroVisualData => heroVisualData;
        public List<CombatAction> AvailableActions => availableActions;
        public ClassAnimatorData AnimatorData => animatorData;
    }
}