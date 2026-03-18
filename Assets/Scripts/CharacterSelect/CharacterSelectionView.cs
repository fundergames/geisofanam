using System;
using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Player;
using RogueDeal.UI;

namespace RogueDeal.CharacterSelect
{
    public class CharacterSelectionView : MonoBehaviour
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
        [SerializeField] private UnityEngine.UI.Button selectButton;
        [SerializeField] private UnityEngine.UI.Button backButton;

        private Dictionary<HeroData, GameObject> spawnedHeroes = new();
        private PlayerData playerData;
        private HeroData selectedHero;

        public void Initialize()
        {
            InitializePlayerData();
            SetupView();
            SetupButtons();
        }

        private void InitializePlayerData()
        {
            playerData = new PlayerData(50000);
        }

        private void SetupView()
        {
            if (classSelectionView != null)
            {
                classSelectionView.UpdateDisplay(heroes, OnHeroSelected);
            }

            if (heroes.Count > 0)
            {
                OnHeroSelected(heroes[0]);
            }
        }

        private void SetupButtons()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelectButtonClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        private void OnHeroSelected(HeroData hero)
        {
            if (selectedHero == hero) return;
            selectedHero = hero;

            UpdateViews(hero);
            SpawnOrUpdateHero(hero);
        }

        private void UpdateViews(HeroData hero)
        {
            if (statsView != null)
            {
                statsView.UpdateDisplay(hero.StatList);
            }

            if (levelView != null)
            {
                levelView.UpdateDisplay(hero.Level, "Level", "0.0%");
            }

            if (classDescriptionView != null)
            {
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
                Debug.Log($"Selected hero: {selectedHero.PlayerName}");
                CharacterSelectManager.Instance.SelectHero(selectedHero);
            }
        }

        private void OnBackButtonClicked()
        {
            CharacterSelectManager.Instance.GoBack();
        }

        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnSelectButtonClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackButtonClicked);
            }
        }
    }
}
