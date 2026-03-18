using UnityEngine;
using TMPro;
using RogueDeal.Levels;

namespace RogueDeal.UI
{
    public class LevelInfoDisplay : MonoBehaviour
    {
        [Header("Display Elements")]
        [SerializeField]
        private TextMeshProUGUI levelCodeText;

        [SerializeField]
        private TextMeshProUGUI levelNameText;

        [SerializeField]
        private TextMeshProUGUI progressText;

        [SerializeField]
        private GameObject statsPanel;

        [Header("Settings")]
        [SerializeField]
        private bool updateEveryFrame = false;

        [SerializeField]
        private bool showInCombat = true;

        private void Start()
        {
            UpdateDisplay();
        }

        private void Update()
        {
            if (updateEveryFrame)
            {
                UpdateDisplay();
            }
        }

        public void UpdateDisplay()
        {
            if (LevelManager.Instance == null)
            {
                HideDisplay();
                return;
            }

            var selectedLevel = LevelManager.Instance.GetSelectedLevel();
            
            if (selectedLevel != null && showInCombat)
            {
                ShowLevelInfo(selectedLevel);
            }
            else
            {
                ShowProgressInfo();
            }
        }

        private void ShowLevelInfo(LevelDefinition level)
        {
            if (levelCodeText != null)
            {
                levelCodeText.text = level.GetLevelCode();
            }

            if (levelNameText != null)
            {
                levelNameText.text = level.displayName;
            }

            int stars = LevelManager.Instance.GetLevelStars(level);
            if (progressText != null)
            {
                string starDisplay = stars > 0 ? new string('★', stars) : "Not Completed";
                progressText.text = $"Best: {starDisplay}";
            }

            if (statsPanel != null)
            {
                statsPanel.SetActive(true);
            }
        }

        private void ShowProgressInfo()
        {
            if (levelCodeText != null)
            {
                levelCodeText.text = "";
            }

            if (levelNameText != null)
            {
                levelNameText.text = "Stage Select";
            }

            if (progressText != null)
            {
                int totalStars = LevelManager.Instance.GetTotalStars();
                int completedLevels = LevelManager.Instance.GetCompletedLevelCount();
                int totalLevels = LevelManager.Instance.GetTotalLevelCount();
                
                progressText.text = $"Progress: {completedLevels}/{totalLevels} | ★{totalStars}";
            }

            if (statsPanel != null)
            {
                statsPanel.SetActive(true);
            }
        }

        private void HideDisplay()
        {
            if (levelCodeText != null)
            {
                levelCodeText.text = "";
            }

            if (levelNameText != null)
            {
                levelNameText.text = "";
            }

            if (progressText != null)
            {
                progressText.text = "";
            }

            if (statsPanel != null)
            {
                statsPanel.SetActive(false);
            }
        }
    }
}
