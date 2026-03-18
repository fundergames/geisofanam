using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Funder.Core.Services;
using Funder.Core.FSM;
using Funder.Core.Logging;

namespace RogueDeal.UI
{
    [RequireComponent(typeof(Button))]
    public class HeroesButtonHandler : MonoBehaviour
    {
        private const string CHARACTER_SELECT_SCENE = "CharacterSelect";
        
        private Button _button;
        private ISceneLoader _sceneLoader;
        private ILoggingService _logger;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnHeroesButtonClicked);
        }

        private void Start()
        {
            if (GameBootstrap.IsInitialized)
            {
                _logger = GameBootstrap.ServiceLocator.Resolve<ILoggingService>();
                _sceneLoader = new SceneLoader(_logger);
            }
        }

        private async void OnHeroesButtonClicked()
        {
            _logger?.Info("GameLobby", "Heroes button clicked - loading CharacterSelect");

            if (_sceneLoader != null)
            {
                await _sceneLoader.LoadAsync(CHARACTER_SELECT_SCENE, additive: false);
            }
            else
            {
                SceneManager.LoadScene(CHARACTER_SELECT_SCENE);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnHeroesButtonClicked);
            }
        }
    }
}
