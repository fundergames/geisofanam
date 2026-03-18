using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Levels
{
    [CreateAssetMenu(fileName = "World_", menuName = "Funder Games/Rogue Deal/Levels/World Definition")]
    public class WorldDefinition : ScriptableObject
    {
        [Header("World Info")]
        public int worldNumber = 1;
        public string worldName;
        [TextArea(2, 4)]
        public string description;
        public Sprite worldIcon;
        public Sprite backgroundImage;
        
        [Header("Levels")]
        public List<LevelDefinition> levels = new List<LevelDefinition>();
        
        [Header("Theme")]
        public Color themeColor = Color.white;
        public AudioClip backgroundMusic;

        public LevelDefinition GetLevel(int levelNumber)
        {
            return levels.Find(l => l.levelNumber == levelNumber);
        }

        public int GetTotalLevels()
        {
            return levels.Count;
        }
    }
}
