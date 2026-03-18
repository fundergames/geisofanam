using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Funder.Core.Services;
using Funder.Core.FSM;
using Funder.Core.Logging;
using RogueDeal.Player;
using RogueDeal.UI;

namespace RogueDeal.CharacterSelect
{
    [Serializable]
    public class HeroSelectionController : MonoBehaviour
    {
        private static readonly int PlayRandom = Animator.StringToHash("PlayRandom");

        [Header("Views")]
        [SerializeField] private ClassSelectionView classSelectionView;
        [SerializeField] private CharacterStatsView statsView;
        [SerializeField] private CharacterLevelView levelView;
        [SerializeField] private CharacterClassDescriptionView classDescriptionView;
        [SerializeField] private Material characterMaterial;
        
        [Header("Character Information")]
        [SerializeField] private List<HeroData> heroes = new();
        [SerializeField] private Transform heroesParent;

        [Header("UI Buttons")]
        [SerializeField] private Button selectButton;
        [SerializeField] private Button backButton;

        [Header("Scene Navigation")]
        [SerializeField] private string menuSceneName = "GameLobby";
        [SerializeField] private string gameSceneName = "GameScene";

        private Dictionary<HeroData, GameObject> spawnedHeroes = new();
        private PlayerData playerData;
        private HeroData selectedHero;
        private ISceneLoader _sceneLoader;
        private ILoggingService _logger;
        private bool _isInitialized;

        private void Start()
        {
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            while (!GameBootstrap.IsInitialized)
            {
                await System.Threading.Tasks.Task.Yield();
            }

            InitializeServices();
            InitializePlayerData();
            SetupView();
            SetupButtons();
            _isInitialized = true;
        }

        private void InitializeServices()
        {
            if (GameBootstrap.IsInitialized)
            {
                _logger = GameBootstrap.ServiceLocator.Resolve<ILoggingService>();
                _sceneLoader = new SceneLoader(_logger);
                _logger?.Info("CharacterSelect", "HeroSelectionController initialized with services");
            }
            else
            {
                Debug.LogWarning("[HeroSelectionController] GameBootstrap not initialized");
            }
        }

        private void InitializePlayerData()
        {
            playerData = new PlayerData(50000);
        }

        private void SetupView()
        {
            Debug.Log($"[HeroSelection] SetupView - Heroes count: {heroes.Count}");
            Debug.Log($"[HeroSelection] classSelectionView: {(classSelectionView != null ? "OK" : "NULL")}");
            
            if (classSelectionView != null)
            {
                classSelectionView.UpdateDisplay(heroes, OnHeroSelected);
            }
            else
            {
                Debug.LogError("[HeroSelection] classSelectionView is NULL!");
            }

            if (heroes.Count > 0)
            {
                Debug.Log($"[HeroSelection] Selecting first hero: {heroes[0]?.PlayerName}");
                OnHeroSelected(heroes[0]);
            }
            else
            {
                Debug.LogWarning("[HeroSelection] No heroes in the list!");
            }
        }

        private void SetupButtons()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnSelectButtonClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        private void OnHeroSelected(HeroData hero)
        {
            if (selectedHero == hero)
            {
                Debug.Log($"[HeroSelection] Same hero selected, skipping: {hero?.PlayerName}");
                return;
            }
            
            selectedHero = hero;
            Debug.Log($"[HeroSelection] Hero selected: {hero?.PlayerName}");

            UpdateViews(hero);
            SpawnOrUpdateHero(hero);
        }

        private void UpdateViews(HeroData hero)
        {
            Debug.Log($"[HeroSelection] UpdateViews called for: {hero?.PlayerName}");
            Debug.Log($"[HeroSelection] statsView: {(statsView != null ? "OK" : "NULL")}");
            Debug.Log($"[HeroSelection] levelView: {(levelView != null ? "OK" : "NULL")}");
            Debug.Log($"[HeroSelection] classDescriptionView: {(classDescriptionView != null ? "OK" : "NULL")}");
            
            if (statsView != null)
            {
                Debug.Log($"[HeroSelection] Updating stats view");
                statsView.UpdateDisplay(hero.StatList);
            }

            if (levelView != null)
            {
                Debug.Log($"[HeroSelection] Updating level view");
                levelView.UpdateDisplay(hero.Level, "Level", "0.0%");
            }

            if (classDescriptionView != null)
            {
                Debug.Log($"[HeroSelection] Updating description view");
                classDescriptionView.UpdateDisplay(hero);
            }
        }

        private void SpawnOrUpdateHero(HeroData hero)
        {
            if (!spawnedHeroes.TryGetValue(hero, out var spawned))
            {
                spawned = Instantiate(hero.HeroVisualData.characterPrefab, heroesParent, false);
                ChangeMaterialRecursively(spawned.transform, characterMaterial);
                spawnedHeroes.Add(hero, spawned);
                
                var anim = spawned.GetComponent<Animator>();
                if (anim != null && hero.AnimatorData != null)
                {
                    var newController = new AnimatorOverrideController(hero.AnimatorData.characterSelectAnimator)
                    {
                        ["Idle"] = hero.AnimatorData.idleClip,
                        ["Random_1"] = hero.AnimatorData.tauntAnimationClip
                    };

                    anim.runtimeAnimatorController = newController;
                }
            }
            
            foreach (var h in heroes)
            {
                if (spawnedHeroes.TryGetValue(h, out var obj))
                {
                    obj.SetActive(h == hero);
                }
            }
            
            var animator = spawned.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(PlayRandom);
            }

            if (heroesParent != null)
            {
                heroesParent.transform.SetPositionAndRotation(heroesParent.transform.position, Quaternion.identity);
            }
        }
        
        private void ChangeMaterialRecursively(Transform currentObject, Material mat)
        {
            if (mat == null) return;

            var renderer = currentObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.materials = new[] { mat };  
            }

            foreach (Transform child in currentObject)
            {
                ChangeMaterialRecursively(child, mat); 
            }
        }

        private void OnSelectButtonClicked()
        {
            if (selectedHero != null)
            {
                _logger?.Info("CharacterSelect", $"Hero selected: {selectedHero.PlayerName}");
                
                CharacterSelectData.SetSelectedHero(selectedHero);
                
                LoadGameScene();
            }
            else
            {
                Debug.LogWarning("[HeroSelectionController] No hero selected");
            }
        }

        private void OnBackButtonClicked()
        {
            _logger?.Debug("CharacterSelect", "Back button clicked");
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
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
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
                UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName);
            }
        }

        public HeroData GetSelectedHero()
        {
            return selectedHero;
        }

        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
            }
        }
    }
}
