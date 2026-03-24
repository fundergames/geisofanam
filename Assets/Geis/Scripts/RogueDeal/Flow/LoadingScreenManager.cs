using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Funder.GameFlow
{
    public class LoadingScreenManager : MonoBehaviour
    {
        private static LoadingScreenManager _instance;

        [Header("UI References")]
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private Slider progressBar;

        [SerializeField]
        private TextMeshProUGUI loadingText;

        [SerializeField]
        private TextMeshProUGUI progressText;

        [Header("Settings")]
        [SerializeField]
        private float fadeSpeed = 2f;

        [SerializeField]
        private float minimumDisplayTime = 1f;

        private bool _isShowing;
        private float _currentProgress;
        private GraphicRaycaster _graphicRaycaster;

        public static LoadingScreenManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (!gameObject.GetComponent<EventSystemManager>())
            {
                gameObject.AddComponent<EventSystemManager>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            _graphicRaycaster = GetComponent<GraphicRaycaster>();

            Hide();
        }

        public static async Task ShowDuring(Func<Task> operation, string message = "Loading...")
        {
            if (_instance == null)
            {
                Debug.LogWarning("[LoadingScreen] No LoadingScreenManager found. Running operation without loading screen.");
                await operation();
                return;
            }

            await _instance.ShowAsync(message);

            float startTime = Time.realtimeSinceStartup;

            await operation();

            float elapsed = Time.realtimeSinceStartup - startTime;
            float remainingTime = _instance.minimumDisplayTime - elapsed;

            if (remainingTime > 0)
            {
                await Task.Delay((int)(remainingTime * 1000));
            }

            await _instance.HideAsync();
        }

        public async Task ShowAsync(string message = "Loading...")
        {
            if (_isShowing) return;

            _isShowing = true;
            _currentProgress = 0f;

            if (_graphicRaycaster != null)
                _graphicRaycaster.enabled = true;

            if (loadingText != null)
            {
                loadingText.text = message;
            }

            UpdateProgress(0f);

            while (canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha += Time.deltaTime * fadeSpeed;
                await Task.Yield();
            }

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        public async Task HideAsync()
        {
            if (!_isShowing) return;

            // Stop blocking raycasts immediately so the user can interact with the UI
            // while the loading screen fades out. Also disable GraphicRaycaster so this
            // canvas (sort order 999) doesn't intercept clicks meant for MainMenu.
            canvasGroup.blocksRaycasts = false;
            if (_graphicRaycaster != null)
                _graphicRaycaster.enabled = false;

            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
                await Task.Yield();
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            _isShowing = false;
        }

        public void UpdateProgress(float progress)
        {
            _currentProgress = Mathf.Clamp01(progress);

            if (progressBar != null)
            {
                progressBar.value = _currentProgress;
            }

            if (progressText != null)
            {
                progressText.text = $"{(_currentProgress * 100f):F0}%";
            }
        }

        public void UpdateMessage(string message)
        {
            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }

        private void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }

            if (_graphicRaycaster != null)
                _graphicRaycaster.enabled = false;

            _isShowing = false;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
