using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(menuName = "FunderGames/Stats Data", fileName = "Stats Data")]
    public class StatData : ScriptableObject
    {
        [SerializeField] private Sprite icon;
        [SerializeField] private string displayText;
        [SerializeField] private int amount;
        [SerializeField] private Color color;
        [SerializeField] private StatType type;
        
        public Sprite Icon => icon;
        public string DisplayText => displayText;
        public int Amount => amount;
        public Color Color => color;
        public StatType Type => type;
    }
}
