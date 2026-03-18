using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RogueDeal.UI.Editor
{
    public class DialogUICreator : EditorWindow
    {
        [MenuItem("Tools/Rogue Deal/Create Dialog UI")]
        public static void ShowWindow()
        {
            var window = GetWindow<DialogUICreator>("Dialog UI Creator");
            window.minSize = new Vector2(350, 150);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Dialog UI Creator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("This will create a complete Dialog UI Canvas in your scene with all required components.", MessageType.Info);
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Dialog UI Canvas", GUILayout.Height(40)))
            {
                CreateDialogUI();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Interaction Prompt Prefab", GUILayout.Height(40)))
            {
                CreateInteractionPrompt();
            }
        }

        private void CreateDialogUI()
        {
            Canvas existingCanvas = FindFirstObjectByType<Canvas>();
            if (existingCanvas != null && existingCanvas.name == "DialogUICanvas")
            {
                if (!EditorUtility.DisplayDialog("Dialog UI Exists", 
                    "A DialogUICanvas already exists in the scene. Create anyway?", 
                    "Yes", "Cancel"))
                {
                    return;
                }
            }

            GameObject canvasObj = new GameObject("DialogUICanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject panelObj = new GameObject("DialogPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.sizeDelta = new Vector2(0, 350);

            GameObject speakerNameObj = CreateTextObject("SpeakerNameText", panelObj.transform, 24);
            RectTransform speakerNameRect = speakerNameObj.GetComponent<RectTransform>();
            speakerNameRect.anchorMin = new Vector2(0, 1);
            speakerNameRect.anchorMax = new Vector2(0, 1);
            speakerNameRect.pivot = new Vector2(0, 1);
            speakerNameRect.anchoredPosition = new Vector2(20, -20);
            speakerNameRect.sizeDelta = new Vector2(400, 40);

            GameObject dialogTextObj = CreateTextObject("DialogText", panelObj.transform, 18);
            TextMeshProUGUI dialogText = dialogTextObj.GetComponent<TextMeshProUGUI>();
            dialogText.alignment = TextAlignmentOptions.TopLeft;
            RectTransform dialogTextRect = dialogTextObj.GetComponent<RectTransform>();
            dialogTextRect.anchorMin = new Vector2(0, 0);
            dialogTextRect.anchorMax = new Vector2(1, 1);
            dialogTextRect.pivot = new Vector2(0.5f, 0.5f);
            dialogTextRect.offsetMin = new Vector2(20, 80);
            dialogTextRect.offsetMax = new Vector2(-20, -70);

            GameObject portraitObj = new GameObject("SpeakerPortrait");
            portraitObj.transform.SetParent(panelObj.transform, false);
            Image portraitImage = portraitObj.AddComponent<Image>();
            portraitImage.color = Color.white;
            RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0, 0);
            portraitRect.anchorMax = new Vector2(0, 0);
            portraitRect.pivot = new Vector2(0, 0);
            portraitRect.anchoredPosition = new Vector2(20, 20);
            portraitRect.sizeDelta = new Vector2(128, 128);
            portraitObj.SetActive(false);

            GameObject choiceContainerObj = new GameObject("ChoiceButtonContainer");
            choiceContainerObj.transform.SetParent(panelObj.transform, false);
            VerticalLayoutGroup layoutGroup = choiceContainerObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 10;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlHeight = true;
            RectTransform choiceRect = choiceContainerObj.GetComponent<RectTransform>();
            choiceRect.anchorMin = new Vector2(0.5f, 0);
            choiceRect.anchorMax = new Vector2(0.5f, 1);
            choiceRect.pivot = new Vector2(0.5f, 0.5f);
            choiceRect.anchoredPosition = new Vector2(0, 0);
            choiceRect.sizeDelta = new Vector2(600, -140);

            GameObject continueButton = CreateButton("ContinueButton", "Continue", panelObj.transform);
            RectTransform continueRect = continueButton.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(1, 0);
            continueRect.anchorMax = new Vector2(1, 0);
            continueRect.pivot = new Vector2(1, 0);
            continueRect.anchoredPosition = new Vector2(-20, 20);
            continueRect.sizeDelta = new Vector2(150, 50);

            GameObject closeButton = CreateButton("CloseButton", "Close", panelObj.transform);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-20, -20);
            closeRect.sizeDelta = new Vector2(100, 40);

            DialogUI dialogUI = panelObj.AddComponent<DialogUI>();
            
            panelObj.SetActive(false);

            Selection.activeGameObject = canvasObj;
            EditorGUIUtility.PingObject(canvasObj);

            Debug.Log("Dialog UI Canvas created! Now assign the references in the DialogUI component:");
            Debug.Log("1. Assign ChoiceButtonPrefab (create a button prefab in /Assets/RogueDeal/Prefabs/UI/)");
            Debug.Log("2. All other references should be auto-assigned, but verify them");
        }

        private GameObject CreateTextObject(string name, Transform parent, int fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.text = name;
            return textObj;
        }

        private GameObject CreateButton(string name, string buttonText, Transform parent)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.3f, 0.5f, 1f);
            
            Button button = buttonObj.AddComponent<Button>();
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = buttonText;
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            return buttonObj;
        }

        private void CreateInteractionPrompt()
        {
            GameObject canvasObj = new GameObject("InteractionPrompt");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 50);

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            GameObject textObj = new GameObject("PromptText");
            textObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Press [E] to interact";
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            InteractionPromptUI promptUI = canvasObj.AddComponent<InteractionPromptUI>();

            string prefabPath = "Assets/RogueDeal/Prefabs/UI/InteractionPrompt.prefab";
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath);
            DestroyImmediate(canvasObj);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"Interaction Prompt prefab created at: {prefabPath}");
        }
    }
}
