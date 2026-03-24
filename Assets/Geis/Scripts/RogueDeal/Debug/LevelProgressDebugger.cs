using UnityEngine;
using RogueDeal.Levels;
using System.Text;

namespace RogueDeal.Debugging
{
    public class LevelProgressDebugger : MonoBehaviour
    {
        [Header("Debug Options")]
        [SerializeField]
        private bool showProgressOnStart = true;

        [SerializeField]
        private bool unlockAllLevels = false;

        [SerializeField]
        private bool giveAllThreeStars = false;

        private void Start()
        {
            if (showProgressOnStart)
            {
                LogProgress();
            }

            if (unlockAllLevels)
            {
                UnlockAll();
            }

            if (giveAllThreeStars)
            {
                GiveAllStars();
            }
        }

        [ContextMenu("Log Current Progress")]
        public void LogProgress()
        {
            if (LevelManager.Instance == null)
            {
                Debug.LogWarning("[LevelProgressDebugger] LevelManager not found!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== LEVEL PROGRESS ===");
            sb.AppendLine($"Total Stars: {LevelManager.Instance.GetTotalStars()}");
            sb.AppendLine($"Completed Levels: {LevelManager.Instance.GetCompletedLevelCount()}/{LevelManager.Instance.GetTotalLevelCount()}");
            sb.AppendLine();

            var worlds = LevelManager.Instance.GetAvailableWorlds();
            foreach (var world in worlds)
            {
                sb.AppendLine($"--- World {world} ---");
                var levels = LevelManager.Instance.GetLevelsByWorld(world);
                
                foreach (var level in levels)
                {
                    bool unlocked = LevelManager.Instance.IsLevelUnlocked(level);
                    bool completed = LevelManager.Instance.IsLevelCompleted(level);
                    int stars = LevelManager.Instance.GetLevelStars(level);
                    
                    string status = unlocked ? (completed ? $"★{stars}" : "OPEN") : "LOCKED";
                    sb.AppendLine($"  {level.GetLevelCode()} {level.displayName}: {status}");
                }
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }

        [ContextMenu("Unlock All Levels")]
        public void UnlockAll()
        {
            if (LevelManager.Instance == null)
            {
                Debug.LogWarning("[LevelProgressDebugger] LevelManager not found!");
                return;
            }

            var allLevels = LevelManager.Instance.GetAllLevels();
            foreach (var level in allLevels)
            {
                LevelManager.Instance.CompleteLevel(level, 1, 0, 0);
            }

            Debug.Log($"[LevelProgressDebugger] Unlocked {allLevels.Count} levels");
        }

        [ContextMenu("Give All Levels 3 Stars")]
        public void GiveAllStars()
        {
            if (LevelManager.Instance == null)
            {
                Debug.LogWarning("[LevelProgressDebugger] LevelManager not found!");
                return;
            }

            var allLevels = LevelManager.Instance.GetAllLevels();
            foreach (var level in allLevels)
            {
                LevelManager.Instance.CompleteLevel(level, 3, 0, 0);
            }

            Debug.Log($"[LevelProgressDebugger] Gave 3 stars to {allLevels.Count} levels");
        }

        [ContextMenu("Reset All Progress")]
        public void ResetProgress()
        {
            if (LevelManager.Instance == null)
            {
                Debug.LogWarning("[LevelProgressDebugger] LevelManager not found!");
                return;
            }

            LevelManager.Instance.ResetProgress();
            Debug.Log("[LevelProgressDebugger] All progress reset");
        }

        [ContextMenu("Complete Current Selected Level (3 Stars)")]
        public void CompleteSelectedLevel()
        {
            if (LevelManager.Instance == null)
            {
                Debug.LogWarning("[LevelProgressDebugger] LevelManager not found!");
                return;
            }

            var selected = LevelManager.Instance.GetSelectedLevel();
            if (selected == null)
            {
                Debug.LogWarning("[LevelProgressDebugger] No level selected!");
                return;
            }

            LevelManager.Instance.CompleteLevel(selected, 3, selected.baseGoldReward, selected.baseXPReward);
            Debug.Log($"[LevelProgressDebugger] Completed {selected.displayName} with 3 stars!");
        }

        [ContextMenu("Test Level Selection")]
        public void TestLevelSelection()
        {
            if (LevelManager.Instance == null)
            {
                Debug.LogWarning("[LevelProgressDebugger] LevelManager not found!");
                return;
            }

            var level = LevelManager.Instance.GetLevel(1, 1);
            if (level != null)
            {
                LevelManager.Instance.SelectLevel(level);
                Debug.Log($"[LevelProgressDebugger] Selected: {level.displayName}");
                
                var selected = LevelManager.Instance.GetSelectedLevel();
                Debug.Log($"[LevelProgressDebugger] Retrieved: {selected?.displayName ?? "NULL"}");
            }
            else
            {
                Debug.LogWarning("[LevelProgressDebugger] Level 1-1 not found!");
            }
        }
    }
}
