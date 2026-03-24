using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Levels
{
    public class LevelProgressionManager
    {
        private const string PROGRESSION_KEY = "LevelProgression";
        
        private Dictionary<string, LevelProgress> levelProgress = new Dictionary<string, LevelProgress>();

        public bool IsLevelUnlocked(LevelDefinition level)
        {
            if (level.prerequisiteLevel == null)
                return true;

            string prereqId = level.prerequisiteLevel.levelId;
            if (!levelProgress.ContainsKey(prereqId))
                return false;

            return levelProgress[prereqId].completed;
        }

        public int GetLevelStars(LevelDefinition level)
        {
            if (!levelProgress.ContainsKey(level.levelId))
                return 0;

            return levelProgress[level.levelId].stars;
        }

        public bool IsLevelCompleted(LevelDefinition level)
        {
            if (!levelProgress.ContainsKey(level.levelId))
                return false;

            return levelProgress[level.levelId].completed;
        }

        public void CompleteLevel(LevelDefinition level, int stars, int turnsUsed)
        {
            var progress = new LevelProgress
            {
                levelId = level.levelId,
                completed = true,
                stars = stars,
                bestTurns = turnsUsed
            };

            if (levelProgress.ContainsKey(level.levelId))
            {
                var existing = levelProgress[level.levelId];
                if (stars > existing.stars)
                {
                    existing.stars = stars;
                }
                if (turnsUsed < existing.bestTurns)
                {
                    existing.bestTurns = turnsUsed;
                }
            }
            else
            {
                levelProgress[level.levelId] = progress;
            }

            SaveProgression();
        }

        public void SaveProgression()
        {
            var saveData = new LevelProgressionSaveData
            {
                progressList = new List<LevelProgress>(levelProgress.Values)
            };

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(PROGRESSION_KEY, json);
            PlayerPrefs.Save();
        }

        public void LoadProgression()
        {
            if (!PlayerPrefs.HasKey(PROGRESSION_KEY))
                return;

            string json = PlayerPrefs.GetString(PROGRESSION_KEY);
            var saveData = JsonUtility.FromJson<LevelProgressionSaveData>(json);

            levelProgress.Clear();
            foreach (var progress in saveData.progressList)
            {
                levelProgress[progress.levelId] = progress;
            }
        }
    }

    [System.Serializable]
    public class LevelProgress
    {
        public string levelId;
        public bool completed;
        public int stars;
        public int bestTurns;
    }

    [System.Serializable]
    public class LevelProgressionSaveData
    {
        public List<LevelProgress> progressList;
    }
}
