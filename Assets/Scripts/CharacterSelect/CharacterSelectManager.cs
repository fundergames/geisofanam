using UnityEngine;
using UnityEngine.SceneManagement;
using Funder.Core.Services;
using Funder.Core.FSM;
using Funder.Core.Logging;
using System.Threading.Tasks;
using RogueDeal.Player;

namespace RogueDeal.CharacterSelect
{
    public class CharacterSelectManager : MonoBehaviour
    {
        private static CharacterSelectManager _instance;
        public static CharacterSelectManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CharacterSelectManager>();
                }
                return _instance;
            }
        }

        [Header("Settings")]
        [SerializeField] private string menuSceneName = "GameLobby";
        [SerializeField] private string gameSceneName = "GameScene";

        private ISceneLoader _sceneLoader;
        private ILoggingService _logger;
        private HeroData _selectedHero;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            InitializeServices();
        }

        private void InitializeServices()
        {
            if (GameBootstrap.IsInitialized)
            {
                _logger = GameBootstrap.ServiceLocator.Resolve<ILoggingService>();
                _sceneLoader = new SceneLoader(_logger);
            }
            else
            {
                Debug.LogWarning("[CharacterSelectManager] GameBootstrap not initialized, services unavailable");
            }
        }

        public void SelectHero(HeroData hero)
        {
            _selectedHero = hero;
            _logger?.Info("CharacterSelect", $"Hero selected: {hero.PlayerName}");
            
            LoadGameScene();
        }

        public void GoBack()
        {
            LoadMenuScene();
        }

        private async void LoadGameScene()
        {
            _logger?.Debug("CharacterSelect", $"Loading game scene: {gameSceneName}");
            
            if (_sceneLoader != null)
            {
                await _sceneLoader.LoadAsync(gameSceneName, additive: false);
            }
            else
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }

        private async void LoadMenuScene()
        {
            _logger?.Debug("CharacterSelect", $"Loading menu scene: {menuSceneName}");
            
            if (_sceneLoader != null)
            {
                await _sceneLoader.LoadAsync(menuSceneName, additive: false);
            }
            else
            {
                SceneManager.LoadScene(menuSceneName);
            }
        }

        public HeroData GetSelectedHero()
        {
            return _selectedHero;
        }
    }
}
