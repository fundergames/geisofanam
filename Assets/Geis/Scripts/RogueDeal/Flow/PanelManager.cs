using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Funder.Core.Services;
using Funder.Core.Events;
using Funder.GameFlow.Events;
using RogueDeal.UI;

namespace Funder.GameFlow
{
    public enum PanelType
    {
        Character,
        Shop,
        Guild,
        Equipment,
        Ranking,
        Mission,
        StageSelect,
        Settings,
        GuideBook,
        BattlePass
    }

    public enum PanelLoadMode
    {
        Prefab,
        AdditiveScene,
        InScene
    }

    [Serializable]
    public class PanelConfig
    {
        public PanelType panelType;
        public PanelLoadMode loadMode;
        public string resourcePath;
        public string sceneName;
        public GameObject inScenePanel;
        
        [Header("Transition Settings")]
        public PanelTransitionType transitionType = PanelTransitionType.None;
        public float transitionDuration = 0.5f;
        public Vector2 slideOffset = new Vector2(1000, 0);
    }

    public class PanelManager : MonoBehaviour
    {
        private static PanelManager _instance;
        public static PanelManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PanelManager>();
                }
                return _instance;
            }
        }

        [Header("Panel Configuration")]
        [SerializeField]
        private List<PanelConfig> panelConfigs = new List<PanelConfig>();

        [Header("Settings")]
        [SerializeField]
        private Transform panelContainer;

        [SerializeField]
        private bool useLoadingIndicator = true;

        [SerializeField]
        private bool useTransitions = true;

        private Dictionary<PanelType, GameObject> _loadedPrefabPanels = new Dictionary<PanelType, GameObject>();
        private Dictionary<PanelType, string> _loadedScenePanels = new Dictionary<PanelType, string>();
        private Dictionary<PanelType, float> _panelOpenTimes = new Dictionary<PanelType, float>();
        private PanelType? _currentPanel;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (panelContainer == null)
            {
                panelContainer = transform;
            }

            InitializeDefaultConfigs();
            AutoDetectInScenePanels();
        }

        private void AutoDetectInScenePanels()
        {
            if (panelContainer == null) return;

            var canvas = panelContainer.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas != null)
            {
                var stageSelectInScene = canvas.transform.Find("RogueDeal_StageSelect");
                if (stageSelectInScene != null)
                {
                    var existingConfig = panelConfigs.Find(c => c.panelType == PanelType.StageSelect);
                    if (existingConfig != null)
                    {
                        existingConfig.loadMode = PanelLoadMode.InScene;
                        existingConfig.inScenePanel = stageSelectInScene.gameObject;
                        stageSelectInScene.gameObject.SetActive(false);
                        Debug.Log("[PanelManager] Auto-detected in-scene StageSelect panel");
                    }
                }
            }
        }

        private void InitializeDefaultConfigs()
        {
            if (panelConfigs.Count == 0)
            {
                panelConfigs.Add(new PanelConfig 
                { 
                    panelType = PanelType.Character, 
                    loadMode = PanelLoadMode.AdditiveScene, 
                    sceneName = "CharacterScene" 
                });
                
                panelConfigs.Add(new PanelConfig 
                { 
                    panelType = PanelType.Shop, 
                    loadMode = PanelLoadMode.AdditiveScene, 
                    sceneName = "ShopScene" 
                });
                
                panelConfigs.Add(new PanelConfig 
                { 
                    panelType = PanelType.Settings, 
                    loadMode = PanelLoadMode.Prefab, 
                    resourcePath = "Settings" 
                });
                
                panelConfigs.Add(new PanelConfig 
                { 
                    panelType = PanelType.StageSelect, 
                    loadMode = PanelLoadMode.Prefab, 
                    resourcePath = "UI/RogueDeal_StageSelect" 
                });
            }
        }

        public async Task<bool> ShowPanel(PanelType panelType)
        {
            if (_currentPanel == panelType)
            {
                Debug.LogWarning($"[PanelManager] {panelType} is already open");
                return false;
            }

            await HideCurrentPanel();

            var config = panelConfigs.Find(c => c.panelType == panelType);
            if (config == null)
            {
                Debug.LogError($"[PanelManager] No configuration found for {panelType}");
                return false;
            }

            bool success = false;
            switch (config.loadMode)
            {
                case PanelLoadMode.Prefab:
                    success = await ShowPrefabPanel(panelType, config);
                    break;
                case PanelLoadMode.AdditiveScene:
                    success = await ShowScenePanel(panelType, config);
                    break;
                case PanelLoadMode.InScene:
                    success = await ShowInScenePanel(panelType, config);
                    break;
            }

            if (success)
            {
                _currentPanel = panelType;
                _panelOpenTimes[panelType] = Time.realtimeSinceStartup;
                
                PublishPanelOpenedEvent(panelType, config.loadMode.ToString(), "Menu");
                
                Debug.Log($"[PanelManager] Opened {panelType}");
            }

            return success;
        }

        private async Task<bool> ShowPrefabPanel(PanelType panelType, PanelConfig config)
        {
            GameObject panelInstance;
            
            if (_loadedPrefabPanels.TryGetValue(panelType, out GameObject existingPanel))
            {
                panelInstance = existingPanel;
            }
            else
            {
                var prefab = Resources.Load<GameObject>(config.resourcePath);
                if (prefab == null)
                {
                    Debug.LogError($"[PanelManager] Prefab not found at Resources/{config.resourcePath}");
                    return false;
                }

                panelInstance = Instantiate(prefab, panelContainer);
                _loadedPrefabPanels[panelType] = panelInstance;
            }

            if (useTransitions && config.transitionType != PanelTransitionType.None)
            {
                await PerformShowTransition(panelInstance, config);
            }
            else
            {
                panelInstance.SetActive(true);
            }

            return true;
        }

        private async Task<bool> ShowInScenePanel(PanelType panelType, PanelConfig config)
        {
            if (config.inScenePanel == null)
            {
                Debug.LogError($"[PanelManager] In-scene panel reference is null for {panelType}");
                return false;
            }

            _loadedPrefabPanels[panelType] = config.inScenePanel;

            if (useTransitions && config.transitionType != PanelTransitionType.None)
            {
                await PerformShowTransition(config.inScenePanel, config);
            }
            else
            {
                config.inScenePanel.SetActive(true);
            }

            return true;
        }

        private async Task<bool> ShowScenePanel(PanelType panelType, PanelConfig config)
        {
            if (_loadedScenePanels.ContainsKey(panelType))
            {
                var scene = SceneManager.GetSceneByName(config.sceneName);
                if (scene.isLoaded)
                {
                    SetSceneActive(scene, true);
                    return true;
                }
            }

            if (useLoadingIndicator)
            {
                var loadingScreen = LoadingScreenManager.Instance;
                if (loadingScreen != null)
                {
                    await loadingScreen.ShowAsync($"Opening {panelType}...");
                }
            }

            var asyncLoad = SceneManager.LoadSceneAsync(config.sceneName, LoadSceneMode.Additive);
            if (asyncLoad == null)
            {
                Debug.LogError($"[PanelManager] Failed to load scene {config.sceneName}");
                return false;
            }

            while (!asyncLoad.isDone)
            {
                if (useLoadingIndicator)
                {
                    LoadingScreenManager.Instance?.UpdateProgress(asyncLoad.progress);
                }
                await Task.Yield();
            }

            _loadedScenePanels[panelType] = config.sceneName;

            if (useLoadingIndicator)
            {
                await LoadingScreenManager.Instance?.HideAsync();
            }

            return true;
        }

        public async Task HideCurrentPanel()
        {
            if (!_currentPanel.HasValue)
            {
                return;
            }

            var panelType = _currentPanel.Value;
            var config = panelConfigs.Find(c => c.panelType == panelType);

            if (config == null)
            {
                return;
            }

            float timeOpen = 0f;
            if (_panelOpenTimes.TryGetValue(panelType, out float openTime))
            {
                timeOpen = Time.realtimeSinceStartup - openTime;
                _panelOpenTimes.Remove(panelType);
            }

            switch (config.loadMode)
            {
                case PanelLoadMode.Prefab:
                case PanelLoadMode.InScene:
                    if (_loadedPrefabPanels.TryGetValue(panelType, out GameObject panel))
                    {
                        if (useTransitions && config.transitionType != PanelTransitionType.None)
                        {
                            await PerformHideTransition(panel, config);
                        }
                        else
                        {
                            panel.SetActive(false);
                        }
                    }
                    break;

                case PanelLoadMode.AdditiveScene:
                    if (_loadedScenePanels.TryGetValue(panelType, out string sceneName))
                    {
                        var scene = SceneManager.GetSceneByName(sceneName);
                        if (scene.isLoaded)
                        {
                            SetSceneActive(scene, false);
                        }
                    }
                    break;
            }

            PublishPanelClosedEvent(panelType, timeOpen);

            _currentPanel = null;
        }

        public async Task UnloadPanel(PanelType panelType)
        {
            var config = panelConfigs.Find(c => c.panelType == panelType);
            if (config == null)
            {
                return;
            }

            switch (config.loadMode)
            {
                case PanelLoadMode.Prefab:
                    if (_loadedPrefabPanels.TryGetValue(panelType, out GameObject panel))
                    {
                        Destroy(panel);
                        _loadedPrefabPanels.Remove(panelType);
                    }
                    break;

                case PanelLoadMode.InScene:
                    if (_loadedPrefabPanels.TryGetValue(panelType, out GameObject inScenePanel))
                    {
                        inScenePanel.SetActive(false);
                        _loadedPrefabPanels.Remove(panelType);
                    }
                    break;

                case PanelLoadMode.AdditiveScene:
                    if (_loadedScenePanels.TryGetValue(panelType, out string sceneName))
                    {
                        var asyncUnload = SceneManager.UnloadSceneAsync(sceneName);
                        if (asyncUnload != null)
                        {
                            while (!asyncUnload.isDone)
                            {
                                await Task.Yield();
                            }
                        }
                        _loadedScenePanels.Remove(panelType);
                    }
                    break;
            }

            if (_currentPanel == panelType)
            {
                _currentPanel = null;
            }

            Debug.Log($"[PanelManager] Unloaded {panelType}");
        }

        public void UnloadAllPanels()
        {
            foreach (var panel in _loadedPrefabPanels.Values)
            {
                if (panel != null)
                {
                    Destroy(panel);
                }
            }
            _loadedPrefabPanels.Clear();

            foreach (var sceneName in _loadedScenePanels.Values)
            {
                SceneManager.UnloadSceneAsync(sceneName);
            }
            _loadedScenePanels.Clear();

            _currentPanel = null;
            Debug.Log("[PanelManager] Unloaded all panels");
        }

        private void SetSceneActive(Scene scene, bool active)
        {
            foreach (var rootObj in scene.GetRootGameObjects())
            {
                rootObj.SetActive(active);
            }
        }

        public bool IsPanelOpen(PanelType panelType)
        {
            return _currentPanel == panelType;
        }

        public PanelType? GetCurrentPanel()
        {
            return _currentPanel;
        }

        private void OnDestroy()
        {
            UnloadAllPanels();
        }

        private void PublishPanelOpenedEvent(PanelType panelType, string panelMode, string source)
        {
            try
            {
                var eventBus = GameBootstrap.ServiceLocator?.Resolve<IEventBus>();
                eventBus?.Publish(new PanelOpenedEvent
                {
                    PanelName = panelType.ToString(),
                    PanelMode = panelMode,
                    Source = source
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PanelManager] Failed to publish PanelOpenedEvent: {ex.Message}");
            }
        }

        private void PublishPanelClosedEvent(PanelType panelType, float timeOpen)
        {
            try
            {
                var eventBus = GameBootstrap.ServiceLocator?.Resolve<IEventBus>();
                eventBus?.Publish(new PanelClosedEvent
                {
                    PanelName = panelType.ToString(),
                    TimeOpen = timeOpen
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PanelManager] Failed to publish PanelClosedEvent: {ex.Message}");
            }
        }

        private async Task PerformShowTransition(GameObject panel, PanelConfig config)
        {
            var components = panel.GetComponent<PanelComponents>();
            if (components == null)
            {
                components = panel.AddComponent<PanelComponents>();
            }

            var rectTransform = components.RectTransform;
            var canvasGroup = components.CanvasGroup;

            switch (config.transitionType)
            {
                case PanelTransitionType.Fade:
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = 0;
                        panel.SetActive(true);
                        await AnimateFade(canvasGroup, 0, 1, config.transitionDuration);
                    }
                    break;

                case PanelTransitionType.Slide:
                    if (rectTransform != null)
                    {
                        var originalPosition = rectTransform.anchoredPosition;
                        var startPosition = originalPosition + config.slideOffset;
                        rectTransform.anchoredPosition = startPosition;
                        panel.SetActive(true);
                        await AnimatePosition(rectTransform, startPosition, originalPosition, config.transitionDuration);
                    }
                    break;

                case PanelTransitionType.Scale:
                    if (rectTransform != null)
                    {
                        rectTransform.localScale = Vector3.zero;
                        panel.SetActive(true);
                        await AnimateScale(rectTransform, Vector3.zero, Vector3.one, config.transitionDuration);
                    }
                    break;

                case PanelTransitionType.None:
                default:
                    panel.SetActive(true);
                    break;
            }

            var panelInterface = panel.GetComponent<IPanel>();
            panelInterface?.Show();
        }

        private async Task PerformHideTransition(GameObject panel, PanelConfig config)
        {
            var components = panel.GetComponent<PanelComponents>();
            if (components == null)
            {
                panel.SetActive(false);
                return;
            }

            var rectTransform = components.RectTransform;
            var canvasGroup = components.CanvasGroup;

            var panelInterface = panel.GetComponent<IPanel>();
            panelInterface?.Hide();

            switch (config.transitionType)
            {
                case PanelTransitionType.Fade:
                    if (canvasGroup != null)
                    {
                        await AnimateFade(canvasGroup, canvasGroup.alpha, 0, config.transitionDuration);
                        panel.SetActive(false);
                    }
                    break;

                case PanelTransitionType.Slide:
                    if (rectTransform != null)
                    {
                        var startPosition = rectTransform.anchoredPosition;
                        var endPosition = startPosition - config.slideOffset;
                        await AnimatePosition(rectTransform, startPosition, endPosition, config.transitionDuration);
                        panel.SetActive(false);
                    }
                    break;

                case PanelTransitionType.Scale:
                    if (rectTransform != null)
                    {
                        await AnimateScale(rectTransform, Vector3.one, Vector3.zero, config.transitionDuration);
                        panel.SetActive(false);
                    }
                    break;

                case PanelTransitionType.None:
                default:
                    panel.SetActive(false);
                    break;
            }
        }

        private async Task AnimateFade(CanvasGroup canvasGroup, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(from, to, t);
                await Task.Yield();
            }
            canvasGroup.alpha = to;
        }

        private async Task AnimatePosition(RectTransform rectTransform, Vector2 from, Vector2 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = EaseOutQuad(t);
                rectTransform.anchoredPosition = Vector2.Lerp(from, to, smoothT);
                await Task.Yield();
            }
            rectTransform.anchoredPosition = to;
        }

        private async Task AnimateScale(RectTransform rectTransform, Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = EaseOutBack(t);
                rectTransform.localScale = Vector3.Lerp(from, to, smoothT);
                await Task.Yield();
            }
            rectTransform.localScale = to;
        }

        private float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
