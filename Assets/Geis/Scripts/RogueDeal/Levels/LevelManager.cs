using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RogueDeal.Levels
{
    public class LevelManager : MonoBehaviour
    {
        private static LevelManager _instance;
        public static LevelManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<LevelManager>();
                }
                return _instance;
            }
        }

        [Header("Level Data")]
        [SerializeField]
        private List<LevelDefinition> allLevels = new List<LevelDefinition>();

        [Header("Configuration")]
        [SerializeField]
        private bool loadLevelsFromResources = true;

        [SerializeField]
        private string levelsResourcePath = "Data/Levels";

        private LevelProgressTracker _levelProgress;
        private LevelDefinition _selectedLevel;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeLevels();
            LoadProgress();
        }

        private void InitializeLevels()
        {
            if (loadLevelsFromResources)
            {
                var levels = Resources.LoadAll<LevelDefinition>(levelsResourcePath);
                allLevels = levels.OrderBy(l => l.worldNumber).ThenBy(l => l.levelNumber).ToList();
                Debug.Log($"[LevelManager] Loaded {allLevels.Count} levels from Resources/{levelsResourcePath}");
            }
            else
            {
                allLevels = allLevels.OrderBy(l => l.worldNumber).ThenBy(l => l.levelNumber).ToList();
                Debug.Log($"[LevelManager] Using {allLevels.Count} levels from inspector");
            }
        }

        private void LoadProgress()
        {
            _levelProgress = new LevelProgressTracker();
            _levelProgress.LoadFromPlayerPrefs();
        }

        public void SaveProgress()
        {
            _levelProgress?.SaveToPlayerPrefs();
        }

        public List<LevelDefinition> GetAllLevels()
        {
            return new List<LevelDefinition>(allLevels);
        }

        public List<LevelDefinition> GetLevelsByWorld(int worldNumber)
        {
            return allLevels.Where(l => l.worldNumber == worldNumber).ToList();
        }

        public LevelDefinition GetLevel(string levelId)
        {
            return allLevels.FirstOrDefault(l => l.levelId == levelId);
        }

        public LevelDefinition GetLevel(int worldNumber, int levelNumber)
        {
            return allLevels.FirstOrDefault(l => l.worldNumber == worldNumber && l.levelNumber == levelNumber);
        }

        public void SelectLevel(LevelDefinition level)
        {
            _selectedLevel = level;
            Debug.Log($"[LevelManager] Selected level: {level.displayName} ({level.GetLevelCode()})");
        }

        public LevelDefinition GetSelectedLevel()
        {
            return _selectedLevel;
        }

        public bool IsLevelUnlocked(LevelDefinition level)
        {
            if (level == null)
                return false;

            if (level.prerequisiteLevel != null)
            {
                string prereqId = level.prerequisiteLevel.levelId;
                if (!_levelProgress.IsLevelCompleted(prereqId))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetLevelStars(LevelDefinition level)
        {
            if (level == null)
                return 0;

            return _levelProgress.GetStars(level.levelId);
        }

        public bool IsLevelCompleted(LevelDefinition level)
        {
            if (level == null)
                return false;

            return _levelProgress.IsLevelCompleted(level.levelId);
        }

        public void CompleteLevel(LevelDefinition level, int stars, int goldEarned, int xpEarned)
        {
            if (level == null)
                return;

            int previousStars = _levelProgress.GetStars(level.levelId);
            _levelProgress.RecordLevelCompletion(level.levelId, stars);

            if (stars > previousStars)
            {
                Debug.Log($"[LevelManager] New best! Level {level.GetLevelCode()} completed with {stars} stars (previous: {previousStars})");
            }

            SaveProgress();
        }

        public int GetTotalStars()
        {
            return _levelProgress.GetTotalStars();
        }

        public int GetCompletedLevelCount()
        {
            return _levelProgress.GetCompletedLevelCount();
        }

        public int GetTotalLevelCount()
        {
            return allLevels.Count;
        }

        public List<int> GetAvailableWorlds()
        {
            return allLevels.Select(l => l.worldNumber).Distinct().OrderBy(w => w).ToList();
        }

        public void ResetProgress()
        {
            _levelProgress = new LevelProgressTracker();
            SaveProgress();
            Debug.Log("[LevelManager] Progress reset");
        }

        private void OnApplicationQuit()
        {
            SaveProgress();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                SaveProgress();
            }
        }
    }
}
