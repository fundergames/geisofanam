using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "Hero Visual Data", menuName = "FunderGames/Hero Visual Data")]
    public class HeroVisualData : ScriptableObject
    {
        public Sprite icon;
        public Sprite fullImage;
        public GameObject characterPrefab;
    }
}