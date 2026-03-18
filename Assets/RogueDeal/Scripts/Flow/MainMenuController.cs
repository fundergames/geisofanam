using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Funder.Core.Flow;
using Funder.Core.Services;
using Funder.Core.Events;

namespace Funder.GameFlow
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        private Button playButton;

        [SerializeField]
        private Button settingsButton;

        [SerializeField]
        private Button creditsButton;

        [SerializeField]
        private Button quitButton;

        [Header("Panels")]
        [SerializeField]
        private GameObject settingsPanel;

        [SerializeField]
        private GameObject creditsPanel;

        private IEventBus _eventBus;

        private void Awake()
        {
            FixCanvasScale();

            if (EventSystem.current == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }

            EnsureButtonReferences();
            SetupButtons();
        }

        private void Start()
        {
            if (GameBootstrap.IsInitialized)
                _eventBus = GameBootstrap.ServiceLocator.Resolve<IEventBus>();

            HidePanels();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                OnPlayButtonClicked();
        }

        private void FixCanvasScale()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.gameObject.scene != gameObject.scene) continue;
                var rt = canvas.GetComponent<RectTransform>();
                if (rt == null || rt.localScale != Vector3.zero) continue;
                rt.localScale = Vector3.one;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                break;
            }
        }

        private void EnsureButtonReferences()
        {
            if (playButton == null && (playButton = GameObject.Find("PlayButton")?.GetComponent<Button>()) == null)
                Debug.LogError("[MainMenu] PlayButton not assigned and not found by name 'PlayButton'.");
            if (settingsButton == null)
                settingsButton = GameObject.Find("SettingsButton")?.GetComponent<Button>();
            if (creditsButton == null)
                creditsButton = GameObject.Find("CreditsButton")?.GetComponent<Button>();
            if (quitButton == null)
                quitButton = GameObject.Find("QuitButton")?.GetComponent<Button>();
        }

        private void SetupButtons()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayButtonClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.AddListener(OnCreditsButtonClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitButtonClicked);
            }
        }

        private void HidePanels()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            if (creditsPanel != null)
            {
                creditsPanel.SetActive(false);
            }
        }

        private void OnPlayButtonClicked()
        {
            FGFlowExtensions.StartGameWithLoading();
        }

        private void OnSettingsButtonClicked()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(!settingsPanel.activeSelf);
            if (creditsPanel != null && creditsPanel.activeSelf)
                creditsPanel.SetActive(false);
        }

        private void OnCreditsButtonClicked()
        {
            if (creditsPanel != null)
                creditsPanel.SetActive(!creditsPanel.activeSelf);
            if (settingsPanel != null && settingsPanel.activeSelf)
                settingsPanel.SetActive(false);
        }

        private void OnQuitButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayButtonClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.RemoveListener(OnCreditsButtonClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitButtonClicked);
            }
        }
    }
}
