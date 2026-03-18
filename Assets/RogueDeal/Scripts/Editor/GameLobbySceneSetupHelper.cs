using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Funder.GameFlow;

namespace RogueDeal.Editor
{
    public class GameLobbySceneSetupHelper : EditorWindow
    {
        [MenuItem("Funder Games/Rogue Deal/Setup GameLobby Scene")]
        public static void ShowWindow()
        {
            GetWindow<GameLobbySceneSetupHelper>("GameLobby Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("GameLobby Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Wire Play Button", GUILayout.Height(30)))
            {
                WirePlayButton();
            }
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "This will connect the Button_Play to the GameLobbyController to load the Combat scene.",
                MessageType.Info);
        }

        private void WirePlayButton()
        {
            Debug.Log("=== Wiring GameLobby Play Button ===");
            
            var controller = GameObject.Find("GameLobbyController");
            if (controller == null)
            {
                Debug.LogError("GameLobbyController not found!");
                return;
            }

            var lobbyController = controller.GetComponent<GameLobbyController>();
            if (lobbyController == null)
            {
                Debug.LogError("GameLobbyController component not found!");
                return;
            }

            var playButton = GameObject.Find("Button_Play");
            if (playButton == null)
            {
                Debug.LogError("Button_Play not found!");
                return;
            }

            var button = playButton.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("Button component not found on Button_Play!");
                return;
            }

            SerializedObject so = new SerializedObject(lobbyController);
            so.FindProperty("startGameButton").objectReferenceValue = button;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(lobbyController);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("✓ Button_Play wired to GameLobbyController.startGameButton");
            Debug.Log("=== Wiring Complete! ===");

            EditorUtility.DisplayDialog("Success",
                "Play button has been wired!\n\n" +
                "When you press Play in Entry scene and navigate to GameLobby,\n" +
                "clicking the Play button will load the Combat scene.",
                "OK");
        }
    }
}
