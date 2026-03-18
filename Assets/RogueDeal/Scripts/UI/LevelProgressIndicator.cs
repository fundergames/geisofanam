using Funder.Core.Events;
using RogueDeal.Events;
using RogueDeal.Levels;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RogueDeal.UI
{
    public class LevelProgressIndicator : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private GameObject startDotPrefab;
        [SerializeField] private GameObject enemyDotPrefab;
        [SerializeField] private GameObject bossDotPrefab;
        
        [Header("Slider Reference")]
        [SerializeField] private Slider progressSlider;
        
        [Header("Dot Container")]
        [SerializeField] private RectTransform dotContainer;
        
        [Header("Layout Settings")]
        [SerializeField] private float edgePadding = 0f;
        
        [Header("Color Settings")]
        [SerializeField] private Color startDotColor = Color.green;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private Color defeatedColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
        
        private List<Image> allDots = new List<Image>();
        private int currentEnemyIndex = 0;
        private int totalEnemies = 0;
        private float currentProgress = 0f;

        private void Awake()
        {
            if (progressSlider == null)
            {
                progressSlider = GetComponent<Slider>();
            }
            
            if (dotContainer == null)
            {
                dotContainer = transform as RectTransform;
            }
        }

        private void OnEnable()
        {
            EventBus<CombatStartedEvent>.Subscribe(OnCombatStarted);
            EventBus<EnemyDefeatedEvent>.Subscribe(OnEnemyDefeated);
        }

        private void OnDisable()
        {
            EventBus<CombatStartedEvent>.Unsubscribe(OnCombatStarted);
            EventBus<EnemyDefeatedEvent>.Unsubscribe(OnEnemyDefeated);
        }

        public void Initialize(LevelDefinition level)
        {
            ClearDots();
            
            if (level == null || level.enemySpawns == null || level.enemySpawns.Count == 0)
            {
                Debug.LogWarning("[LevelProgressIndicator] No enemies to display");
                return;
            }

            totalEnemies = level.enemySpawns.Count;
            
            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = totalEnemies;
                progressSlider.value = 0f;
            }

            CreateDots(level);
            UpdateDotStates();
            UpdateSliderProgress();
        }

        private void CreateDots(LevelDefinition level)
        {
            if (dotContainer == null)
            {
                Debug.LogWarning("[LevelProgressIndicator] No dot container set");
                return;
            }

            Rect containerRect = dotContainer.rect;
            float containerWidth = containerRect.width - (edgePadding * 2f);
            int totalDots = level.enemySpawns.Count + 1;
            
            Debug.Log($"[LevelProgressIndicator] Creating {totalDots} dots. Container width: {containerRect.width}, Usable width: {containerWidth}");
            
            for (int i = 0; i < totalDots; i++)
            {
                GameObject prefab = null;
                
                if (i == 0)
                {
                    prefab = startDotPrefab;
                }
                else
                {
                    bool isBoss = level.enemySpawns[i - 1].isBoss;
                    prefab = isBoss ? bossDotPrefab : enemyDotPrefab;
                }
                
                if (prefab == null)
                {
                    Debug.LogWarning($"[LevelProgressIndicator] Missing prefab for dot at index {i}");
                    continue;
                }

                GameObject dotObj = Instantiate(prefab, dotContainer);
                RectTransform rectTransform = dotObj.GetComponent<RectTransform>();
                
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    
                    float normalizedPosition = totalDots > 1 ? (float)i / (totalDots - 1) : 0.5f;
                    float xPosition = Mathf.Lerp(-containerWidth / 2f, containerWidth / 2f, normalizedPosition);
                    rectTransform.anchoredPosition = new Vector2(xPosition, 0f);
                    
                    Debug.Log($"[LevelProgressIndicator] Dot {i}: normalized={normalizedPosition:F3}, x={xPosition:F1}");
                }

                Image dotImage = dotObj.GetComponent<Image>();
                if (dotImage != null)
                {
                    allDots.Add(dotImage);
                }
                else
                {
                    Debug.LogWarning($"[LevelProgressIndicator] Dot prefab missing Image component at index {i}");
                }
            }
        }

        private void UpdateDotStates()
        {
            for (int i = 0; i < allDots.Count; i++)
            {
                if (allDots[i] == null)
                    continue;

                if (i == 0)
                {
                    allDots[i].color = startDotColor;
                }
                else if (i <= currentEnemyIndex)
                {
                    allDots[i].color = defeatedColor;
                }
                else if (i == currentEnemyIndex + 1)
                {
                    allDots[i].color = activeColor;
                }
                else
                {
                    allDots[i].color = inactiveColor;
                }
            }
        }

        private void UpdateSliderProgress()
        {
            if (progressSlider != null)
            {
                progressSlider.value = currentProgress;
            }
        }

        public void SetProgress(float progress)
        {
            currentProgress = Mathf.Clamp(progress, 0f, totalEnemies);
            UpdateSliderProgress();
        }

        public void SetProgressFromEnemyIndex(int enemyIndex, float fraction = 0f)
        {
            currentProgress = enemyIndex + fraction;
            currentEnemyIndex = enemyIndex;
            UpdateSliderProgress();
            UpdateDotStates();
        }

        private void ClearDots()
        {
            foreach (var dot in allDots)
            {
                if (dot != null)
                {
                    Destroy(dot.gameObject);
                }
            }
            allDots.Clear();
            currentEnemyIndex = 0;
        }

        private void OnCombatStarted(CombatStartedEvent evt)
        {
            currentEnemyIndex = 0;
            currentProgress = 0f;
            UpdateDotStates();
            UpdateSliderProgress();
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            currentEnemyIndex++;
            currentProgress = currentEnemyIndex;
            UpdateDotStates();
            UpdateSliderProgress();
        }
    }
}
