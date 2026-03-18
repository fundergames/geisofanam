using UnityEngine;

namespace RogueDeal.Player
{
    [CreateAssetMenu(fileName = "Hero Visual Data", menuName = "RogueDeal/Character/Hero Visual Data")]
    public class HeroVisualData : ScriptableObject
    {
        public Sprite icon;
        public Sprite fullImage;
        public GameObject characterPrefab;
    }
}
