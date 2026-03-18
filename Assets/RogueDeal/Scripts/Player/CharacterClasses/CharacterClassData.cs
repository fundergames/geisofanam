using UnityEngine;

namespace RogueDeal.Player
{
    [CreateAssetMenu(fileName = "New Character Class", menuName = "RogueDeal/Character/Character Class")]
    public class CharacterClassData : ScriptableObject
    {
        [SerializeField] private string classDisplayName;
        [TextArea(3, 6)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        
        public string ClassDisplayName => classDisplayName;
        public string Description => description;
        public Sprite Icon => icon;
    }
}
