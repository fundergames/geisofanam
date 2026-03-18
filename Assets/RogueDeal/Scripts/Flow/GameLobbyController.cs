using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Funder.Core.Flow;
using Funder.Core.Services;
using Funder.Core.Events;

namespace Funder.GameFlow
{
    public class GameLobbyController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        private Button startGameButton;

        [SerializeField]
        private Button backToMenuButton;

        [SerializeField]
        private TextMeshProUGUI playerCountText;

        [SerializeField]
        private TextMeshProUGUI lobbyStatusText;

        [SerializeField]
        private GameObject homePanel;

        [Header("Settings")]
        [SerializeField]
        private int requiredPlayers = 1;

        [SerializeField]
        private bool autoShowStageSelect = true;

        private IEventBus _eventBus;
        private int _currentPlayerCount = 1;

        private void Start()
        {
            if (GameBootstrap.IsInitialized)
            {
                _eventBus = GameBootstrap.ServiceLocator.Resolve<IEventBus>();
            }

            SetupButtons();
            UpdateUI();

            if (autoShowStageSelect)
            {
                ShowStageSelect();
            }

            Debug.Log("[GameLobby] Game lobby loaded");
        }

        private void SetupButtons()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            }
        }

        private void UpdateUI()
        {
            if (playerCountText != null)
            {
                playerCountText.text = $"Players: {_currentPlayerCount}/{requiredPlayers}";
            }

            if (lobbyStatusText != null)
            {
                if (_currentPlayerCount >= requiredPlayers)
                {
                    lobbyStatusText.text = "Ready to start!";
                }
                else
                {
                    lobbyStatusText.text = $"Waiting for {requiredPlayers - _currentPlayerCount} more player(s)...";
                }
            }

            if (startGameButton != null)
            {
                startGameButton.interactable = _currentPlayerCount >= requiredPlayers;
            }
        }

        private void OnStartGameClicked()
        {
            Debug.Log("[GameLobby] Opening stage select...");
            ShowStageSelect();
        }

        private async void ShowStageSelect()
        {
            if (homePanel != null)
            {
                homePanel.SetActive(false);
            }

            if (PanelManager.Instance != null)
            {
                bool success = await PanelManager.Instance.ShowPanel(PanelType.StageSelect);
                if (success)
                {
                    Debug.Log("[GameLobby] Stage select panel opened via PanelManager");
                }
                else
                {
                    Debug.LogError("[GameLobby] Failed to open StageSelect panel via PanelManager");
                }
            }
            else
            {
                Debug.LogError("[GameLobby] PanelManager instance not found!");
            }
        }

        private void OnBackToMenuClicked()
        {
            Debug.Log("[GameLobby] Returning to main menu"); 
            FGFlowExtensions.BackToMenuWithLoading();
        }

        public void OnPlayerJoined()
        {
            _currentPlayerCount++;
            UpdateUI();
            Debug.Log($"[GameLobby] Player joined. Total: {_currentPlayerCount}");
        }

        public void OnPlayerLeft()
        {
            _currentPlayerCount = Mathf.Max(1, _currentPlayerCount - 1);
            UpdateUI();
            Debug.Log($"[GameLobby] Player left. Total: {_currentPlayerCount}");
        }

        private void ShowStatus(string message)
        {
            if (lobbyStatusText != null)
            {
                lobbyStatusText.text = message;
            }

            Debug.Log($"[GameLobby] {message}");
        }

        private void OnDestroy()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveListener(OnStartGameClicked);
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveListener(OnBackToMenuClicked);
            }
        }
    }
}
