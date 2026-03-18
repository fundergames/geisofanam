using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Levels
{
    [Serializable]
    public class LevelProgressTracker
    {
        private const string PROGRESS_KEY = "LevelProgress";

        private Dictionary<string, LevelCompletionData> _completedLevels = new Dictionary<string, LevelCompletionData>();

        public void RecordLevelCompletion(string levelId, int stars)
        {
            if (string.IsNullOrEmpty(levelId))
                return;

            stars = Mathf.Clamp(stars, 0, 3);

            if (_completedLevels.TryGetValue(levelId, out LevelCompletionData existingData))
            {
                if (stars > existingData.stars)
                {
                    existingData.stars = stars;
                    existingData.lastCompletedTime = DateTime.UtcNow.ToString("o");
                    existingData.timesCompleted++;
                }
                else
                {
                    existingData.timesCompleted++;
                }
            }
            else
            {
                _completedLevels[levelId] = new LevelCompletionData
                {
                    levelId = levelId,
                    stars = stars,
                    timesCompleted = 1,
                    firstCompletedTime = DateTime.UtcNow.ToString("o"),
                    lastCompletedTime = DateTime.UtcNow.ToString("o")
                };
            }
        }

        public bool IsLevelCompleted(string levelId)
        {
            return _completedLevels.ContainsKey(levelId);
        }

        public int GetStars(string levelId)
        {
            if (_completedLevels.TryGetValue(levelId, out LevelCompletionData data))
            {
                return data.stars;
            }
            return 0;
        }

        public int GetTimesCompleted(string levelId)
        {
            if (_completedLevels.TryGetValue(levelId, out LevelCompletionData data))
            {
                return data.timesCompleted;
            }
            return 0;
        }

        public int GetTotalStars()
        {
            int total = 0;
            foreach (var data in _completedLevels.Values)
            {
                total += data.stars;
            }
            return total;
        }

        public int GetCompletedLevelCount()
        {
            return _completedLevels.Count;
        }

        public void SaveToPlayerPrefs()
        {
            try
            {
                var saveData = new LevelProgressSaveData
                {
                    completedLevels = new List<LevelCompletionData>(_completedLevels.Values)
                };

                string json = JsonUtility.ToJson(saveData, true);
                PlayerPrefs.SetString(PROGRESS_KEY, json);
                PlayerPrefs.Save();

                Debug.Log($"[LevelProgress] Saved progress: {_completedLevels.Count} levels completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelProgress] Failed to save progress: {ex.Message}");
            }
        }

        public void LoadFromPlayerPrefs()
        {
            try
            {
                if (!PlayerPrefs.HasKey(PROGRESS_KEY))
                {
                    Debug.Log("[LevelProgress] No saved progress found, starting fresh");
                    return;
                }

                string json = PlayerPrefs.GetString(PROGRESS_KEY);
                var saveData = JsonUtility.FromJson<LevelProgressSaveData>(json);

                _completedLevels.Clear();
                foreach (var data in saveData.completedLevels)
                {
                    _completedLevels[data.levelId] = data;
                }

                Debug.Log($"[LevelProgress] Loaded progress: {_completedLevels.Count} levels completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelProgress] Failed to load progress: {ex.Message}");
                _completedLevels.Clear();
            }
        }

        public void Clear()
        {
            _completedLevels.Clear();
        }
    }

    [Serializable]
    public class LevelCompletionData
    {
        public string levelId;
        public int stars;
        public int timesCompleted;
        public string firstCompletedTime;
        public string lastCompletedTime;

        [NonSerialized]
        private DateTime _firstCompletedDateTime;
        [NonSerialized]
        private DateTime _lastCompletedDateTime;

        public DateTime FirstCompletedTime
        {
            get
            {
                if (_firstCompletedDateTime == default && !string.IsNullOrEmpty(firstCompletedTime))
                {
                    DateTime.TryParse(firstCompletedTime, out _firstCompletedDateTime);
                }
                return _firstCompletedDateTime;
            }
            set
            {
                _firstCompletedDateTime = value;
                firstCompletedTime = value.ToString("o");
            }
        }

        public DateTime LastCompletedTime
        {
            get
            {
                if (_lastCompletedDateTime == default && !string.IsNullOrEmpty(lastCompletedTime))
                {
                    DateTime.TryParse(lastCompletedTime, out _lastCompletedDateTime);
                }
                return _lastCompletedDateTime;
            }
            set
            {
                _lastCompletedDateTime = value;
                lastCompletedTime = value.ToString("o");
            }
        }
    }

    [Serializable]
    public class LevelProgressSaveData
    {
        public List<LevelCompletionData> completedLevels = new List<LevelCompletionData>();
    }
}
