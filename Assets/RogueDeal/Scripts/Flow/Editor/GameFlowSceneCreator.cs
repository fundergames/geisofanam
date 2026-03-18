using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Funder.Core.Services;

namespace Funder.GameFlow.Editor
{
    public static class GameFlowSceneCreator
    {
        private const string ScenePath = "Assets/Scenes/";

        [MenuItem("Funder Games/Core/Game Flow/Create All Scenes", priority = 1)]
        public static void CreateAllScenes()
        {
            if (!EditorUtility.DisplayDialog("Create Game Flow Scenes",
                "This will create 5 new scenes:\n\n" +
                "• Entry.unity\n" +
                "• Splash.unity\n" +
                "• Login.unity\n" +
                "• MainMenu.unity\n" +
                "• GameLobby.unity\n\n" +
                "Existing scenes will not be overwritten.\n\n" +
                "Continue?",
                "Create Scenes", "Cancel"))
            {
                return;
            }

            CreateEntryScene();
            CreateSplashScene();
            CreateLoginScene();
            CreateMainMenuScene();
            CreateGameLobbyScene();

            EditorUtility.DisplayDialog("Success",
                "All game flow scenes created successfully!\n\n" +
                "Next steps:\n" +
                "1. Configure FGAppConfig scene names\n" +
                "2. Add Entry.unity to Build Settings\n" +
                "3. Test the flow!\n\n" +
                "See SETUP_GUIDE.md for details.",
                "OK");
        }

        [MenuItem("Funder Games/Core/Game Flow/Create Entry Scene", priority = 10)]
        public static void CreateEntryScene()
        {
            string scenePath = ScenePath + "Entry.unity";
            if (System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"Scene already exists: {scenePath}");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject bootstrap = new GameObject("GameBootstrap");
            var bootstrapComponent = bootstrap.AddComponent<GameBootstrap>();

            var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Resources/Configs/PR_MainBootstrapConfig.asset");
            if (config != null)
            {
                SerializedObject serializedBootstrap = new SerializedObject(bootstrapComponent);
                serializedBootstrap.FindProperty("config").objectReferenceValue = config;
                serializedBootstrap.ApplyModifiedProperties();
            }

            GameObject controller = new GameObject("EntryController");
            controller.AddComponent<EntryController>();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"Created scene: {scenePath}");
        }

        [MenuItem("Funder Games/Core/Game Flow/Create Splash Scene", priority = 11)]
        public static void CreateSplashScene()
        {
            string scenePath = ScenePath + "Splash.unity";
            if (System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"Scene already exists: {scenePath}");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGO = CreateCanvas("Canvas");
            CanvasGroup canvasGroup = canvasGO.AddComponent<CanvasGroup>();

            GameObject panel = CreatePanel(canvasGO.transform, "Background");
            SetPanelColor(panel, new Color(0.1f, 0.1f, 0.15f, 1f));

            GameObject logoText = CreateText(canvasGO.transform, "Logo", "YOUR STUDIO", 72);
            SetTextPosition(logoText, 0, 100);

            GameObject gameText = CreateText(canvasGO.transform, "GameTitle", "GAME TITLE", 48);
            SetTextPosition(gameText, 0, 0);

            GameObject controller = new GameObject("SplashController");
            var splashController = controller.AddComponent<SplashScreenController>();

            SerializedObject serializedController = new SerializedObject(splashController);
            serializedController.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            serializedController.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"Created scene: {scenePath}");
        }

        [MenuItem("Funder Games/Core/Game Flow/Create Login Scene", priority = 12)]
        public static void CreateLoginScene()
        {
            string scenePath = ScenePath + "Login.unity";
            if (System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"Scene already exists: {scenePath}");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGO = CreateCanvas("Canvas");

            GameObject panel = CreatePanel(canvasGO.transform, "Background");

            GameObject titleText = CreateText(canvasGO.transform, "Title", "LOGIN", 48);
            SetTextPosition(titleText, 0, 200);

            GameObject usernameInput = CreateInputField(canvasGO.transform, "UsernameInput", "Enter username...");
            SetPosition(usernameInput, 0, 80);

            GameObject passwordInput = CreateInputField(canvasGO.transform, "PasswordInput", "Enter password...");
            SetPosition(passwordInput, 0, 0);
            var passwordInputField = passwordInput.GetComponent<TMP_InputField>();
            if (passwordInputField != null)
            {
                passwordInputField.contentType = TMP_InputField.ContentType.Password;
            }

            GameObject loginButton = CreateButton(canvasGO.transform, "LoginButton", "Login");
            SetPosition(loginButton, -80, -100);

            GameObject guestButton = CreateButton(canvasGO.transform, "GuestButton", "Guest Login");
            SetPosition(guestButton, 80, -100);

            GameObject statusText = CreateText(canvasGO.transform, "StatusText", "", 24);
            SetTextPosition(statusText, 0, -180);

            GameObject controller = new GameObject("LoginController");
            var loginController = controller.AddComponent<LoginScreenController>();

            SerializedObject serializedController = new SerializedObject(loginController);
            serializedController.FindProperty("usernameInput").objectReferenceValue = usernameInput.GetComponent<TMP_InputField>();
            serializedController.FindProperty("passwordInput").objectReferenceValue = passwordInput.GetComponent<TMP_InputField>();
            serializedController.FindProperty("loginButton").objectReferenceValue = loginButton.GetComponent<Button>();
            serializedController.FindProperty("guestButton").objectReferenceValue = guestButton.GetComponent<Button>();
            serializedController.FindProperty("statusText").objectReferenceValue = statusText.GetComponent<TextMeshProUGUI>();
            serializedController.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"Created scene: {scenePath}");
        }

        [MenuItem("Funder Games/Core/Game Flow/Create Main Menu Scene", priority = 13)]
        public static void CreateMainMenuScene()
        {
            string scenePath = ScenePath + "MainMenu.unity";
            if (System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"Scene already exists: {scenePath}");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGO = CreateCanvas("Canvas");

            GameObject panel = CreatePanel(canvasGO.transform, "Background");

            GameObject titleText = CreateText(canvasGO.transform, "Title", "MAIN MENU", 48);
            SetTextPosition(titleText, 0, 250);

            GameObject playButton = CreateButton(canvasGO.transform, "PlayButton", "Play");
            SetPosition(playButton, 0, 100);

            GameObject settingsButton = CreateButton(canvasGO.transform, "SettingsButton", "Settings");
            SetPosition(settingsButton, 0, 20);

            GameObject creditsButton = CreateButton(canvasGO.transform, "CreditsButton", "Credits");
            SetPosition(creditsButton, 0, -60);

            GameObject quitButton = CreateButton(canvasGO.transform, "QuitButton", "Quit");
            SetPosition(quitButton, 0, -140);

            GameObject settingsPanel = CreatePanel(canvasGO.transform, "SettingsPanel");
            settingsPanel.SetActive(false);

            GameObject creditsPanel = CreatePanel(canvasGO.transform, "CreditsPanel");
            creditsPanel.SetActive(false);

            GameObject controller = new GameObject("MainMenuController");
            var menuController = controller.AddComponent<MainMenuController>();

            SerializedObject serializedController = new SerializedObject(menuController);
            serializedController.FindProperty("playButton").objectReferenceValue = playButton.GetComponent<Button>();
            serializedController.FindProperty("settingsButton").objectReferenceValue = settingsButton.GetComponent<Button>();
            serializedController.FindProperty("creditsButton").objectReferenceValue = creditsButton.GetComponent<Button>();
            serializedController.FindProperty("quitButton").objectReferenceValue = quitButton.GetComponent<Button>();
            serializedController.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            serializedController.FindProperty("creditsPanel").objectReferenceValue = creditsPanel;
            serializedController.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"Created scene: {scenePath}");
        }

        [MenuItem("Funder Games/Core/Game Flow/Create Game Lobby Scene", priority = 14)]
        public static void CreateGameLobbyScene()
        {
            string scenePath = ScenePath + "GameLobby.unity";
            if (System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"Scene already exists: {scenePath}");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGO = CreateCanvas("Canvas");

            GameObject panel = CreatePanel(canvasGO.transform, "Background");

            GameObject titleText = CreateText(canvasGO.transform, "Title", "GAME LOBBY", 48);
            SetTextPosition(titleText, 0, 250);

            GameObject playerCountText = CreateText(canvasGO.transform, "PlayerCountText", "Players: 1/2", 32);
            SetTextPosition(playerCountText, 0, 150);

            GameObject statusText = CreateText(canvasGO.transform, "LobbyStatusText", "Waiting for players...", 24);
            SetTextPosition(statusText, 0, 100);

            GameObject startButton = CreateButton(canvasGO.transform, "StartGameButton", "Start Game");
            SetPosition(startButton, 0, 0);

            GameObject backButton = CreateButton(canvasGO.transform, "BackToMenuButton", "Back to Menu");
            SetPosition(backButton, 0, -80);

            GameObject controller = new GameObject("GameLobbyController");
            var lobbyController = controller.AddComponent<GameLobbyController>();

            SerializedObject serializedController = new SerializedObject(lobbyController);
            serializedController.FindProperty("startGameButton").objectReferenceValue = startButton.GetComponent<Button>();
            serializedController.FindProperty("backToMenuButton").objectReferenceValue = backButton.GetComponent<Button>();
            serializedController.FindProperty("playerCountText").objectReferenceValue = playerCountText.GetComponent<TextMeshProUGUI>();
            serializedController.FindProperty("lobbyStatusText").objectReferenceValue = statusText.GetComponent<TextMeshProUGUI>();
            serializedController.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"Created scene: {scenePath}");
        }

        private static GameObject CreateCanvas(string name)
        {
            GameObject canvasGO = new GameObject(name);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            return canvasGO;
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panelGO = new GameObject(name);
            panelGO.transform.SetParent(parent, false);

            RectTransform rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;

            Image image = panelGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.25f, 0.95f);

            return panelGO;
        }

        private static void SetPanelColor(GameObject panel, Color color)
        {
            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private static GameObject CreateButton(Transform parent, string name, string text)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);

            RectTransform rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);

            Image image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.3f, 0.5f, 0.8f, 1f);

            Button button = buttonGO.AddComponent<Button>();

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);

            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return buttonGO;
        }

        private static GameObject CreateText(Transform parent, string name, string text, float fontSize)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);

            RectTransform rectTransform = textGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(600, 100);

            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return textGO;
        }

        private static GameObject CreateInputField(Transform parent, string name, string placeholder)
        {
            GameObject inputGO = new GameObject(name);
            inputGO.transform.SetParent(parent, false);

            RectTransform rectTransform = inputGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 50);

            Image image = inputGO.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            TMP_InputField inputField = inputGO.AddComponent<TMP_InputField>();

            GameObject textAreaGO = new GameObject("Text Area");
            textAreaGO.transform.SetParent(inputGO.transform, false);
            RectTransform textAreaRect = textAreaGO.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.sizeDelta = new Vector2(-20, -13);
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -7);

            GameObject placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(textAreaGO.transform, false);
            RectTransform placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 20;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            placeholderText.alignment = TextAlignmentOptions.Left;

            GameObject inputTextGO = new GameObject("Text");
            inputTextGO.transform.SetParent(textAreaGO.transform, false);
            RectTransform inputTextRect = inputTextGO.AddComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI inputText = inputTextGO.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 20;
            inputText.color = Color.white;
            inputText.alignment = TextAlignmentOptions.Left;

            inputField.textViewport = textAreaRect;
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;

            return inputGO;
        }

        private static void SetPosition(GameObject obj, float x, float y)
        {
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(x, y);
            }
        }

        private static void SetTextPosition(GameObject obj, float x, float y)
        {
            SetPosition(obj, x, y);
        }
    }
}
