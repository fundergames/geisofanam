using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using RogueDeal.Combat;

namespace RogueDeal.Editor
{
    public class EnemyUISetupHelper : EditorWindow
    {
        [MenuItem("RogueDeal/Setup Enemy UI Components")]
        public static void ShowWindow()
        {
            GetWindow<EnemyUISetupHelper>("Enemy UI Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Enemy UI Setup Helper", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Create Damage Popup Prefab", GUILayout.Height(40)))
            {
                CreateDamagePopupPrefab();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Create Enemy Health Bar Prefab", GUILayout.Height(40)))
            {
                CreateHealthBarPrefab();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Setup Damage Popup Manager in Scene", GUILayout.Height(40)))
            {
                SetupDamagePopupManager();
            }

            GUILayout.Space(20);
            GUILayout.Label("Instructions:", EditorStyles.boldLabel);
            GUILayout.Label("1. Create both prefabs using the buttons above", EditorStyles.wordWrappedLabel);
            GUILayout.Label("2. Setup the manager in your combat scene", EditorStyles.wordWrappedLabel);
            GUILayout.Label("3. Assign prefabs to your enemy EnemyVisual components", EditorStyles.wordWrappedLabel);
        }

        private void CreateDamagePopupPrefab()
        {
            GameObject popupObj = new GameObject("DamagePopup");
            
            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            RectTransform canvasRect = popupObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 100);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            CanvasGroup canvasGroup = popupObj.AddComponent<CanvasGroup>();
            
            GameObject textObj = new GameObject("DamageText");
            textObj.transform.SetParent(popupObj.transform);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "99";
            text.fontSize = 64;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = false;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(2, -2);
            
            DamagePopup popupComponent = popupObj.AddComponent<DamagePopup>();
            
            string path = "Assets/RogueDeal/Prefabs/UI";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder("Assets/RogueDeal/Prefabs", "UI");
            }
            
            string prefabPath = $"{path}/DamagePopup.prefab";
            PrefabUtility.SaveAsPrefabAsset(popupObj, prefabPath);
            DestroyImmediate(popupObj);
            
            EditorUtility.DisplayDialog("Success", $"Damage Popup prefab created at:\n{prefabPath}", "OK");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private void CreateHealthBarPrefab()
        {
            GameObject healthBarObj = new GameObject("EnemyHealthBar");
            
            Canvas canvas = healthBarObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            RectTransform canvasRect = healthBarObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 30);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            GameObject sliderObj = new GameObject("HealthSlider");
            sliderObj.transform.SetParent(healthBarObj.transform);
            
            Slider slider = sliderObj.AddComponent<Slider>();
            
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.1f, 0.3f);
            sliderRect.anchorMax = new Vector2(0.9f, 0.7f);
            sliderRect.sizeDelta = Vector2.zero;
            
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform);
            RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = new Vector2(-10, -10);
            
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.8f, 0.1f, 0.1f, 1f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 1;
            slider.interactable = false;
            
            GameObject textObj = new GameObject("HealthText");
            textObj.transform.SetParent(healthBarObj.transform);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "100 / 100";
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 0.3f);
            textRect.sizeDelta = Vector2.zero;
            
            EnemyHealthBar healthBarComponent = healthBarObj.AddComponent<EnemyHealthBar>();
            
            string path = "Assets/RogueDeal/Prefabs/UI";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder("Assets/RogueDeal/Prefabs", "UI");
            }
            
            string prefabPath = $"{path}/EnemyHealthBar.prefab";
            PrefabUtility.SaveAsPrefabAsset(healthBarObj, prefabPath);
            DestroyImmediate(healthBarObj);
            
            EditorUtility.DisplayDialog("Success", $"Enemy Health Bar prefab created at:\n{prefabPath}", "OK");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private void SetupDamagePopupManager()
        {
            GameObject managerObj = new GameObject("DamagePopupManager");
            DamagePopupManager manager = managerObj.AddComponent<DamagePopupManager>();
            
            Selection.activeGameObject = managerObj;
            
            EditorUtility.DisplayDialog("Success", 
                "DamagePopupManager created in scene!\n\n" +
                "Don't forget to:\n" +
                "1. Assign the DamagePopup prefab\n" +
                "2. Optionally create a Popup Parent object", 
                "OK");
        }
    }
}
