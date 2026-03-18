using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using FunderGames.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FunderGames.UI
{
    public class UIWindowManager : Singleton<UIWindowManager>, IInitializable
    {
        [SerializeField]
        private AssetReference configReference; // ScriptableObject containing window mappings
        
        private UIWindowConfig config; // ScriptableObject containing window mappings
        private readonly Dictionary<string, (IWindow, TransitionType, float, Vector2)> windows = new();
        private readonly Stack<string> windowHistory = new();
        private string currentWindowId;

        public bool IsInitialized { get; private set; } = false;

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            Debug.Log("Initializing UIWindowManager...");
            await LoadConfigurationAsync();
            IsInitialized = true;
            Debug.Log("UIWindowManager initialized.");
        }
      
        private async Task LoadConfigurationAsync()
        {
            config = await SafeExecutionUtility.ExecuteAsync(
                async () => await AssetLoadingManager.LoadFromAssetReference<UIWindowConfig>(configReference),
                errorMessage: "Error loading WindowManagerConfig",
                onError: ex => Debug.LogError($"Configuration loading failed: {ex.Message}"));

            if (config == null) return;

            InitializeWindows();
        }

        private void InitializeWindows()
        {
            foreach (var mapping in config.windowMappings)
            {
                var instance = Instantiate(mapping.windowPrefab, transform);
                instance.SetActive(false);

                if (instance.TryGetComponent<IWindow>(out var window))
                {
                    windows[mapping.id] = (window, mapping.transitionType, mapping.transitionDuration, mapping.slideOffset);
                }
                else
                {
                    Debug.LogWarning($"Window prefab {mapping.windowPrefab.name} does not implement IWindow.");
                }
            }
        }

        public void ShowWindow(string windowId)
        {
            if (!windows.ContainsKey(windowId))
            {
                Debug.LogWarning($"Window with ID '{windowId}' not found.");
                return;
            }

            StartCoroutine(PerformTransition(windowId));
        }

        public void GoBack()
        {
            if (windowHistory.Count == 0) return;

            var previousWindowId = windowHistory.Pop();
            ShowWindow(previousWindowId);
        }

        private IEnumerator PerformTransition(string nextWindowId)
        {
            if (!string.IsNullOrEmpty(currentWindowId))
            {
                var (currentWindow, currentTransition, currentDuration, _) = windows[currentWindowId];
                yield return PerformHideTransition(currentWindow, currentTransition, currentDuration);
            }

            var (nextWindow, nextTransition, nextDuration, slideOffset) = windows[nextWindowId];
            yield return PerformShowTransition(nextWindow, nextTransition, nextDuration, slideOffset);

            windowHistory.Push(currentWindowId);
            currentWindowId = nextWindowId;
        }

        private IEnumerator PerformShowTransition(IWindow window, TransitionType transitionType, float duration, Vector2 slideOffset)
        {
            var monoBehaviour = window as MonoBehaviour;
            if (monoBehaviour == null) yield break;
            if (!monoBehaviour.TryGetComponent<UIWindowComponents>(out var components)) yield break;

            var rectTransform = components.RectTransform;
            var canvasGroup = components.CanvasGroup;
            
            switch (transitionType)
            {
                case TransitionType.Fade:
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = 0;
                        window.Show();
                        canvasGroup.DOFade(1, duration);
                        yield return new WaitForSeconds(duration);
                    }
                    break;

                case TransitionType.Slide:
                    if (rectTransform != null)
                    {
                        var startPosition = rectTransform.anchoredPosition + slideOffset;
                        rectTransform.anchoredPosition = startPosition; // Start off-screen
                        window.Show();
                        rectTransform.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutQuad);
                        yield return new WaitForSeconds(duration);
                    }
                    break;

                case TransitionType.Scale:
                    if (rectTransform != null)
                    {
                        rectTransform.localScale = Vector3.zero;
                        window.Show();
                        rectTransform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
                        yield return new WaitForSeconds(duration);
                    }
                    break;

                case TransitionType.None:
                default:
                    window.Show();
                    break;
            }
        }

        private IEnumerator PerformHideTransition(IWindow window, TransitionType transitionType, float duration)
        {
            var monoBehaviour = window as MonoBehaviour;
            if (monoBehaviour == null) yield break;
            if (!monoBehaviour.TryGetComponent<UIWindowComponents>(out var components)) yield break;

            var rectTransform = components.RectTransform;
            var canvasGroup = components.CanvasGroup;

            switch (transitionType)
            {
                case TransitionType.Fade:
                    if (canvasGroup != null)
                    {
                        canvasGroup.DOFade(0, duration);
                        yield return new WaitForSeconds(duration);
                        window.Hide();
                    }
                    break;

                case TransitionType.Slide:
                    if (rectTransform != null)
                    {
                        var slideOffset = new Vector2(-1000, 0); // Slide out to the left
                        rectTransform.DOAnchorPos(rectTransform.anchoredPosition + slideOffset, duration)
                            .SetEase(Ease.InQuad);
                        yield return new WaitForSeconds(duration);
                        window.Hide();
                    }
                    break;

                case TransitionType.Scale:
                    if (rectTransform != null)
                    {
                        rectTransform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
                        yield return new WaitForSeconds(duration);
                        window.Hide();
                    }
                    break;

                case TransitionType.None:
                default:
                    window.Hide();
                    break;
            }
        }
    }
}
