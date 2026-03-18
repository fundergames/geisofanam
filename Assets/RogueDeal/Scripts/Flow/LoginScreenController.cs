using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Funder.Core.Services;
using Funder.Core.Flow;
using Funder.Core.Events;

namespace Funder.GameFlow
{
    public class LoginScreenController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        private TMP_InputField usernameInput;

        [SerializeField]
        private TMP_InputField passwordInput;

        [SerializeField]
        private Button loginButton;

        [SerializeField]
        private Button guestButton;

        [SerializeField]
        private TextMeshProUGUI statusText;

        private FGAppConfig _appConfig;
        private IEventBus _eventBus;

        private void Start()
        {
            _appConfig = FGConfigManager.GetConfig();

            if (GameBootstrap.IsInitialized)
            {
                _eventBus = GameBootstrap.ServiceLocator.Resolve<IEventBus>();
            }

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (loginButton != null)
            {
                loginButton.onClick.AddListener(OnLoginButtonClicked);
            }

            if (guestButton != null)
            {
                guestButton.onClick.AddListener(OnGuestLoginClicked);
            }
        }

        private void OnLoginButtonClicked()
        {
            string username = usernameInput != null ? usernameInput.text : "";
            string password = passwordInput != null ? passwordInput.text : "";

            if (string.IsNullOrEmpty(username))
            {
                ShowStatus("Please enter a username");
                return;
            }

            PerformLogin(username, password);
        }

        private void OnGuestLoginClicked()
        {
            PerformGuestLogin();
        }

        private void PerformLogin(string username, string password)
        {
            Debug.Log($"[Login] Attempting login for user: {username}");
            ShowStatus("Logging in...");

            OnLoginSuccess(username);
        }

        private void PerformGuestLogin()
        {
            Debug.Log("[Login] Guest login");
            ShowStatus("Logging in as guest...");

            string guestName = $"Guest_{Random.Range(1000, 9999)}";
            OnLoginSuccess(guestName);
        }

        private async void OnLoginSuccess(string username)
        {
            Debug.Log($"[Login] Login successful: {username}");
            ShowStatus($"Welcome, {username}!");

            await FGFlowExtensions.OnLoginCompleteWithLoading();
        }

        private void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            Debug.Log($"[Login] {message}");
        }

        private void OnDestroy()
        {
            if (loginButton != null)
            {
                loginButton.onClick.RemoveListener(OnLoginButtonClicked);
            }

            if (guestButton != null)
            {
                guestButton.onClick.RemoveListener(OnGuestLoginClicked);
            }
        }
    }
}
