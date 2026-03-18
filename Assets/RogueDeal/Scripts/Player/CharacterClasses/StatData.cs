using UnityEngine;

namespace RogueDeal.Player
{
    [CreateAssetMenu(menuName = "RogueDeal/Character/Stat Data", fileName = "StatData")]
    public class StatData : ScriptableObject
    {
        [SerializeField] private Sprite icon;
        [SerializeField] private string displayText;
        [SerializeField] private int amount;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private StatType type;
        
        public Sprite Icon => icon;
        public string DisplayText => displayText;
        public int Amount => amount;
        public Color Color => color;
        public StatType Type => type;
    }
}
