using FunderGames.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FunderGames.RPG.UI
{
    public class LoginWindow : MonoBehaviour, IWindow
    {
        [Header("UI References")] 
        [SerializeField] private Button gameCenterButton;
        [SerializeField] private Button facebookButton;
        [SerializeField] private Button googleButton;
        [SerializeField] private Button guestButton;

        [Header("Debug Options")] [SerializeField]
        private bool simulateLogin = true; // Toggle for debugging login flows

        private void Awake()
        {
            // Assign button listeners
            if (gameCenterButton) gameCenterButton.onClick.AddListener(OnGameCenterLogin);
            if (facebookButton) facebookButton.onClick.AddListener(OnFacebookLogin);
            if (googleButton) googleButton.onClick.AddListener(OnGoogleLogin);
            if (guestButton) guestButton.onClick.AddListener(OnGuestLogin);
        }

        private void OnDestroy()
        {
            // Clean up listeners
            if (gameCenterButton) gameCenterButton.onClick.RemoveListener(OnGameCenterLogin);
            if (facebookButton) facebookButton.onClick.RemoveListener(OnFacebookLogin);
            if (googleButton) googleButton.onClick.RemoveListener(OnGoogleLogin);
            if (guestButton) guestButton.onClick.RemoveListener(OnGuestLogin);
        }

        public void OnGameCenterLogin()
        {
            Debug.Log("Attempting to log in with Game Center...");
            if (simulateLogin) SimulateLogin("Game Center");
            else
            {
                // Call Game Center authentication logic
                Debug.Log("Implement Game Center login here.");
            }
        }

        public void OnFacebookLogin()
        {
            Debug.Log("Attempting to log in with Facebook...");
            if (simulateLogin) SimulateLogin("Facebook");
            else
            {
                // Call Facebook authentication logic
                Debug.Log("Implement Facebook login here.");
            }
        }

        public void OnGoogleLogin()
        {
            Debug.Log("Attempting to log in with Google...");
            if (simulateLogin) SimulateLogin("Google");
            else
            {
                // Call Google authentication logic
                Debug.Log("Implement Google login here.");
            }
        }

        public void OnGuestLogin()
        {
            Debug.Log("Logging in as a Guest...");
            if (simulateLogin) SimulateLogin("Guest");
            else
            {
                // Call guest login logic (e.g., generate guest credentials)
                Debug.Log("Implement guest login here.");
            }
        }

        private void SimulateLogin(string loginMethod)
        {
            Debug.Log($"Simulated {loginMethod} login successful!");
            UIWindowManager.Instance.ShowWindow("CharacterSelect"); // Replace "NextWindowID" with the ID of the next window to show
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}