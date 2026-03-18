using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RogueDeal.Levels;
using Funder.GameFlow;

namespace RogueDeal.UI
{
    public class StageSelectController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        private Transform levelButtonContainer;

        [SerializeField]
        private GameObject levelButtonPrefab;

        [SerializeField]
        private Button startButton;

        [SerializeField]
        private Button backButton;

        [Header("Level Info Display")]
        [SerializeField]
        private TextMeshProUGUI levelNameText;

        [SerializeField]
        private TextMeshProUGUI levelDescriptionText;

        [SerializeField]
        private TextMeshProUGUI levelCodeText;

        [SerializeField]
        private TextMeshProUGUI energyCostText;

        [SerializeField]
        private GameObject[] starDisplays;

        [SerializeField]
        private GameObject lockedOverlay;

        [Header("World Selection")]
        [SerializeField]
        private int currentWorldNumber = 1;

        [SerializeField]
        private Button nextWorldButton;

        [SerializeField]
        private Button previousWorldButton;

        [SerializeField]
        private TextMeshProUGUI worldNumberText;

        [Header("Behavior")]
        [SerializeField]
        [Tooltip("If enabled, clicking a level button immediately starts combat (no need to click Start button)")]
        private bool autoStartOnLevelSelect = false;

        private List<LevelDefinition> _currentWorldLevels = new List<LevelDefinition>();
        private LevelDefinition _selectedLevel;
        private Dictionary<LevelDefinition, LevelButtonUI> _levelButtons = new Dictionary<LevelDefinition, LevelButtonUI>();

        private void Start()
        {
            SetupButtons();
            LoadWorld(currentWorldNumber);
        }

        private void SetupButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartLevelClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (nextWorldButton != null)
            {
                nextWorldButton.onClick.AddListener(OnNextWorldClicked);
            }

            if (previousWorldButton != null)
            {
                previousWorldButton.onClick.AddListener(OnPreviousWorldClicked);
            }
        }

        private void LoadWorld(int worldNumber)
        {
            if (LevelManager.Instance == null)
            {
                Debug.LogError("[StageSelect] LevelManager not found!");
                return;
            }

            currentWorldNumber = worldNumber;
            _currentWorldLevels = LevelManager.Instance.GetLevelsByWorld(worldNumber);

            if (_currentWorldLevels.Count == 0)
            {
                Debug.LogWarning($"[StageSelect] No levels found for world {worldNumber}");
            }

            UpdateWorldButtons();
            PopulateLevelButtons();
            UpdateWorldDisplay();

            if (_currentWorldLevels.Count > 0)
            {
                SelectLevel(_currentWorldLevels[0]);
            }
            else
            {
                ClearLevelInfo();
            }
        }

        private void UpdateWorldButtons()
        {
            var availableWorlds = LevelManager.Instance.GetAvailableWorlds();
            
            if (previousWorldButton != null)
            {
                previousWorldButton.interactable = availableWorlds.Contains(currentWorldNumber - 1);
            }

            if (nextWorldButton != null)
            {
                nextWorldButton.interactable = availableWorlds.Contains(currentWorldNumber + 1);
            }
        }

        private void UpdateWorldDisplay()
        {
            if (worldNumberText != null)
            {
                worldNumberText.text = $"World {currentWorldNumber}";
            }
        }

        private void PopulateLevelButtons()
        {
            ClearLevelButtons();

            if (levelButtonContainer == null || levelButtonPrefab == null)
            {
                Debug.LogWarning("[StageSelect] Level button container or prefab not assigned");
                return;
            }

            foreach (var level in _currentWorldLevels)
            {
                GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
                LevelButtonUI buttonUI = buttonObj.GetComponent<LevelButtonUI>();

                if (buttonUI != null)
                {
                    bool isUnlocked = LevelManager.Instance.IsLevelUnlocked(level);
                    int stars = LevelManager.Instance.GetLevelStars(level);

                    buttonUI.Initialize(level, isUnlocked, stars, () => SelectLevel(level));
                    _levelButtons[level] = buttonUI;
                }
                else
                {
                    Debug.LogWarning("[StageSelect] Level button prefab missing LevelButtonUI component");
                }
            }
        }

        private void ClearLevelButtons()
        {
            _levelButtons.Clear();

            if (levelButtonContainer != null)
            {
                foreach (Transform child in levelButtonContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void SelectLevel(LevelDefinition level)
        {
            _selectedLevel = level;
            LevelManager.Instance.SelectLevel(level);
            UpdateLevelInfo();
            UpdateButtonStates();

            if (autoStartOnLevelSelect && LevelManager.Instance.IsLevelUnlocked(level))
            {
                Debug.Log($"[StageSelect] Auto-starting level: {level.displayName}");
                StartLevel(level);
            }
        }

        private void UpdateLevelInfo()
        {
            if (_selectedLevel == null)
            {
                ClearLevelInfo();
                return;
            }

            bool isUnlocked = LevelManager.Instance.IsLevelUnlocked(_selectedLevel);

            if (levelNameText != null)
            {
                levelNameText.text = _selectedLevel.displayName;
            }

            if (levelDescriptionText != null)
            {
                levelDescriptionText.text = _selectedLevel.description;
            }

            if (levelCodeText != null)
            {
                levelCodeText.text = _selectedLevel.GetLevelCode();
            }

            if (energyCostText != null)
            {
                energyCostText.text = $"Energy: {_selectedLevel.energyCost}";
            }

            UpdateStarDisplay();

            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(!isUnlocked);
            }
        }

        private void UpdateStarDisplay()
        {
            if (starDisplays == null || starDisplays.Length == 0)
                return;

            int earnedStars = LevelManager.Instance.GetLevelStars(_selectedLevel);

            for (int i = 0; i < starDisplays.Length; i++)
            {
                if (starDisplays[i] != null)
                {
                    starDisplays[i].SetActive(i < earnedStars);
                }
            }
        }

        private void ClearLevelInfo()
        {
            if (levelNameText != null)
                levelNameText.text = "";

            if (levelDescriptionText != null)
                levelDescriptionText.text = "";

            if (levelCodeText != null)
                levelCodeText.text = "";

            if (energyCostText != null)
                energyCostText.text = "";

            if (lockedOverlay != null)
                lockedOverlay.SetActive(false);

            if (starDisplays != null)
            {
                foreach (var star in starDisplays)
                {
                    if (star != null)
                        star.SetActive(false);
                }
            }
        }

        private void UpdateButtonStates()
        {
            if (startButton != null)
            {
                bool canStart = _selectedLevel != null && LevelManager.Instance.IsLevelUnlocked(_selectedLevel);
                startButton.interactable = canStart;
            }

            foreach (var kvp in _levelButtons)
            {
                bool isSelected = kvp.Key == _selectedLevel;
                kvp.Value.SetSelected(isSelected);
            }
        }

        private void OnStartLevelClicked()
        {
            if (_selectedLevel == null)
            {
                Debug.LogWarning("[StageSelect] No level selected");
                return;
            }

            if (!LevelManager.Instance.IsLevelUnlocked(_selectedLevel))
            {
                Debug.LogWarning("[StageSelect] Level is locked");
                return;
            }

            Debug.Log($"[StageSelect] Starting level: {_selectedLevel.displayName}");
            StartLevel(_selectedLevel);
        }

        private async void StartLevel(LevelDefinition level)
        {
            await PanelManager.Instance.HideCurrentPanel();
            FGFlowExtensions.StartCombatWithLoading();
        }

        private async void OnBackClicked()
        {
            Debug.Log("[StageSelect] Closing stage select");
            await PanelManager.Instance.HideCurrentPanel();
        }

        private void OnNextWorldClicked()
        {
            LoadWorld(currentWorldNumber + 1);
        }

        private void OnPreviousWorldClicked()
        {
            LoadWorld(currentWorldNumber - 1);
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartLevelClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
            }

            if (nextWorldButton != null)
            {
                nextWorldButton.onClick.RemoveListener(OnNextWorldClicked);
            }

            if (previousWorldButton != null)
            {
                previousWorldButton.onClick.RemoveListener(OnPreviousWorldClicked);
            }
        }
    }
}
