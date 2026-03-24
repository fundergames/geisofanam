using UnityEngine;
using UnityEditor;
using Funder.Core.Flow;
using Funder.Core.Services;

namespace Funder.GameFlow.Editor
{
    public class GameFlowTester : EditorWindow
    {
        private FGAppConfig _config;

        [MenuItem("Funder Games/Core/Game Flow/Flow Tester", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<GameFlowTester>("Flow Tester");
            window.minSize = new Vector2(300, 400);
            window.Show();
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void OnGUI()
        {
            GUILayout.Label("Game Flow Tester", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to test flow transitions.\n\n" +
                    "Open Entry.unity and press Play to start the normal flow.",
                    MessageType.Info);

                GUILayout.Space(10);

                if (GUILayout.Button("Open Entry Scene", GUILayout.Height(40)))
                {
                    OpenScene("Entry");
                }

                GUILayout.Space(10);
                GUILayout.Label("Quick Scene Access:", EditorStyles.boldLabel);

                if (GUILayout.Button("Open Splash Scene"))
                    OpenScene("Splash");

                if (GUILayout.Button("Open Login Scene"))
                    OpenScene("Login");

                if (GUILayout.Button("Open MainMenu Scene"))
                    OpenScene("MainMenu");

                if (GUILayout.Button("Open GameLobby Scene"))
                    OpenScene("GameLobby");

                return;
            }

            EditorGUILayout.HelpBox(
                $"Current State: {FGFlow.CurrentState}\n\n" +
                "Use the buttons below to test flow transitions.",
                MessageType.Info);

            GUILayout.Space(10);

            if (_config == null)
            {
                LoadConfig();
            }

            if (_config == null)
            {
                EditorGUILayout.HelpBox(
                    "FGAppConfig not found!\n\n" +
                    "Expected location:\n" +
                    "Assets/Resources/FunderCore/FGAppConfig.asset",
                    MessageType.Error);
                return;
            }

            GUILayout.Label("Flow Transitions:", EditorStyles.boldLabel);

            GUI.enabled = FGFlow.CurrentState != FGFlow.State.Login;
            if (GUILayout.Button("Go to Login", GUILayout.Height(35)))
            {
                TransitionTo(FGFlow.State.Login);
            }
            GUI.enabled = true;

            GUI.enabled = FGFlow.CurrentState != FGFlow.State.Menu;
            if (GUILayout.Button("Go to Main Menu", GUILayout.Height(35)))
            {
                TransitionTo(FGFlow.State.Menu);
            }
            GUI.enabled = true;

            GUI.enabled = FGFlow.CurrentState != FGFlow.State.Game;
            if (GUILayout.Button("Go to Game Lobby", GUILayout.Height(35)))
            {
                TransitionTo(FGFlow.State.Game);
            }
            GUI.enabled = true;

            GUI.enabled = FGFlow.CurrentState != FGFlow.State.Results;
            if (GUILayout.Button("Go to Results", GUILayout.Height(35)))
            {
                TransitionTo(FGFlow.State.Results);
            }
            GUI.enabled = true;

            GUILayout.Space(20);
            GUILayout.Label("Helper Methods:", EditorStyles.boldLabel);

            if (GUILayout.Button("OnLoginComplete() → Menu", GUILayout.Height(30)))
            {
                FGFlow.OnLoginComplete();
            }

            if (GUILayout.Button("StartGame() → Game", GUILayout.Height(30)))
            {
                FGFlow.StartGame();
            }

            if (GUILayout.Button("FinishGame() → Results", GUILayout.Height(30)))
            {
                FGFlow.FinishGame();
            }

            if (GUILayout.Button("BackToMenu() → Menu", GUILayout.Height(30)))
            {
                FGFlow.BackToMenu();
            }

            GUILayout.Space(20);
            GUILayout.Label("Configuration:", EditorStyles.boldLabel);

            EditorGUILayout.ObjectField("FGAppConfig", _config, typeof(FGAppConfig), false);

            if (GUILayout.Button("Reload Config"))
            {
                LoadConfig();
            }

            GUILayout.Space(10);

            if (_config != null)
            {
                EditorGUILayout.LabelField("Entry Scene:", _config.EntrySceneName);
                EditorGUILayout.LabelField("Login Scene:", _config.LoginSceneName);
                EditorGUILayout.LabelField("Menu Scene:", _config.MenuSceneName);
                EditorGUILayout.LabelField("Game Scene:", _config.GameSceneName);
                EditorGUILayout.LabelField("Results Scene:", _config.ResultsSceneName);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Skip Login In Editor:", _config.SkipLoginInEditor.ToString());
                EditorGUILayout.LabelField("Show Loading Canvas:", _config.ShowLoadingCanvas.ToString());
            }
        }

        private void LoadConfig()
        {
            _config = Resources.Load<FGAppConfig>("FunderCore/FGAppConfig");

            if (_config == null)
            {
                Debug.LogWarning("[FlowTester] FGAppConfig not found at Resources/FunderCore/FGAppConfig");
            }
        }

        private async void TransitionTo(FGFlow.State state)
        {
            if (_config == null)
            {
                Debug.LogError("[FlowTester] Cannot transition - FGAppConfig is null");
                return;
            }

            Debug.Log($"[FlowTester] Transitioning to {state}");
            await FGFlow.GoTo(state, _config);
        }

        private void OpenScene(string sceneName)
        {
            string scenePath = $"Assets/Scenes/{sceneName}.unity";

            if (!System.IO.File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog(
                    "Scene Not Found",
                    $"Scene not found:\n{scenePath}\n\n" +
                    "Create scenes first:\n" +
                    "Tools → Funder/Core → Game Flow → Create All Scenes",
                    "OK");
                return;
            }

            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            }
        }
    }
}
