using UnityEngine;
using UnityEngine.UI;
using Funder.Core.Services;
using Funder.Core.FSM;
using Funder.Core.Logging;
using System.Threading.Tasks;

namespace RogueDeal.CharacterSelect
{
    public class CharacterSelectSceneLoader : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string characterSelectSceneName = "CharacterSelect";

        [Header("UI (Optional)")]
        [SerializeField] private Button loadCharacterSelectButton;

        private ISceneLoader _sceneLoader;
        private ILoggingService _logger;

        private void Start()
        {
            InitializeServices();
            SetupButton();
        }

        private void InitializeServices()
        {
            if (GameBootstrap.IsInitialized)
            {
                _logger = GameBootstrap.ServiceLocator.Resolve<ILoggingService>();
                _sceneLoader = new SceneLoader(_logger);
            }
        }

        private void SetupButton()
        {
            if (loadCharacterSelectButton != null)
            {
                loadCharacterSelectButton.onClick.AddListener(LoadCharacterSelectScene);
            }
        }

        public void LoadCharacterSelectScene()
        {
            LoadCharacterSelectSceneAsync();
        }

        private async void LoadCharacterSelectSceneAsync()
        {
            _logger?.Info("SceneLoader", $"Loading Character Select scene: {characterSelectSceneName}");

            if (_sceneLoader != null)
            {
                await _sceneLoader.LoadAsync(characterSelectSceneName, additive: false);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(characterSelectSceneName);
            }
        }

        private void OnDestroy()
        {
            if (loadCharacterSelectButton != null)
            {
                loadCharacterSelectButton.onClick.RemoveListener(LoadCharacterSelectScene);
            }
        }
    }
}
