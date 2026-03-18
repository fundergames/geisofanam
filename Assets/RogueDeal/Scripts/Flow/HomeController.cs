using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Funder.Core.Flow;
using Funder.Core.Services;
using Funder.Core.Events;
using Funder.Core.FSM;
using Funder.Core.Logging;

namespace Funder.GameFlow
{
    public class HomeController : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField]
        private string characterSelectSceneName = "CharacterSelect";

        [Header("Navigation Buttons")]
        [SerializeField]
        private Button characterButton;

        [SerializeField]
        private Button missionButton;

        [SerializeField]
        private Button shopButton;

        [SerializeField]
        private Button guildButton;

        [SerializeField]
        private Button rankingButton;

        [SerializeField]
        private Button equipmentButton;

        [SerializeField]
        private Button settingsButton;

        [SerializeField]
        private Button playButton;

        [SerializeField]
        private Button backButton;

        [Header("UI Elements")]
        [SerializeField]
        private TextMeshProUGUI playerNameText;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private TextMeshProUGUI goldText;

        [SerializeField]
        private TextMeshProUGUI gemText;

        private IEventBus _eventBus;
        private ISceneLoader _sceneLoader;
        private ILoggingService _logger;

        private void Start()
        {
            if (GameBootstrap.IsInitialized)
            {
                _eventBus = GameBootstrap.ServiceLocator.Resolve<IEventBus>();
                _logger = GameBootstrap.ServiceLocator.Resolve<ILoggingService>();
                _sceneLoader = new SceneLoader(_logger);
            }

            SetupButtons();
            InitializeUI();

            Debug.Log("[Home] Home lobby loaded");
        }

        private void SetupButtons()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (characterButton != null)
            {
                characterButton.onClick.AddListener(OnCharacterClicked);
            }

            if (missionButton != null)
            {
                missionButton.onClick.AddListener(OnMissionClicked);
            }

            if (shopButton != null)
            {
                shopButton.onClick.AddListener(OnShopClicked);
            }

            if (guildButton != null)
            {
                guildButton.onClick.AddListener(OnGuildClicked);
            }

            if (rankingButton != null)
            {
                rankingButton.onClick.AddListener(OnRankingClicked);
            }

            if (equipmentButton != null)
            {
                equipmentButton.onClick.AddListener(OnEquipmentClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }
        }

        private void InitializeUI()
        {
            if (playerNameText != null)
            {
                playerNameText.text = "Player";
            }

            if (levelText != null)
            {
                levelText.text = "Level 1";
            }

            if (goldText != null)
            {
                goldText.text = "1000";
            }

            if (gemText != null)
            {
                gemText.text = "50";
            }
        }

        private async void OnPlayClicked()
        {
            Debug.Log("[Home] Play button clicked - opening stage select");
            await PanelManager.Instance.ShowPanel(PanelType.StageSelect);
        }

        private async void OnCharacterClicked()
        {
            Debug.Log("[Home] Character button clicked - loading CharacterSelect scene");
            _logger?.Info("Home", $"Loading CharacterSelect scene: {characterSelectSceneName}");
            
            if (_sceneLoader != null)
            {
                await _sceneLoader.LoadAsync(characterSelectSceneName, additive: false);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(characterSelectSceneName);
            }
        }

        private async void OnMissionClicked()
        {
            Debug.Log("[Home] Mission button clicked");
            await PanelManager.Instance.ShowPanel(PanelType.Mission);
        }

        private async void OnShopClicked()
        {
            Debug.Log("[Home] Shop button clicked");
            await PanelManager.Instance.ShowPanel(PanelType.Shop);
        }

        private async void OnGuildClicked()
        {
            Debug.Log("[Home] Guild button clicked");
            await PanelManager.Instance.ShowPanel(PanelType.Guild);
        }

        private async void OnRankingClicked()
        {
            Debug.Log("[Home] Ranking button clicked");
            await PanelManager.Instance.ShowPanel(PanelType.Ranking);
        }

        private async void OnEquipmentClicked()
        {
            Debug.Log("[Home] Equipment button clicked");
            await PanelManager.Instance.ShowPanel(PanelType.Equipment);
        }

        private async void OnSettingsClicked()
        {
            Debug.Log("[Home] Settings button clicked");
            await PanelManager.Instance.ShowPanel(PanelType.Settings);
        }

        private void OnBackClicked()
        {
            Debug.Log("[Home] Back to main menu");
            FGFlowExtensions.BackToMenuWithLoading();
        }

        public void UpdatePlayerInfo(string playerName, int level, int gold, int gems)
        {
            if (playerNameText != null)
            {
                playerNameText.text = playerName;
            }

            if (levelText != null)
            {
                levelText.text = $"Level {level}";
            }

            if (goldText != null)
            {
                goldText.text = gold.ToString();
            }

            if (gemText != null)
            {
                gemText.text = gems.ToString();
            }
        }

        private void OnDestroy()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayClicked);
            }

            if (characterButton != null)
            {
                characterButton.onClick.RemoveListener(OnCharacterClicked);
            }

            if (missionButton != null)
            {
                missionButton.onClick.RemoveListener(OnMissionClicked);
            }

            if (shopButton != null)
            {
                shopButton.onClick.RemoveListener(OnShopClicked);
            }

            if (guildButton != null)
            {
                guildButton.onClick.RemoveListener(OnGuildClicked);
            }

            if (rankingButton != null)
            {
                rankingButton.onClick.RemoveListener(OnRankingClicked);
            }

            if (equipmentButton != null)
            {
                equipmentButton.onClick.RemoveListener(OnEquipmentClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
            }
        }
    }
}
