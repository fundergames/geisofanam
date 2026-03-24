using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using RogueDeal.Combat;
using RogueDeal.Combat.UI;

namespace RogueDeal.Combat.Editor
{
    /// <summary>
    /// Creates health bar and damage popup prefabs for the combat system.
    /// Use: Tools > Combat > Create Combat UI Prefabs
    /// </summary>
    public static class CombatUIPrefabBuilder
    {
        private const string PrefabPath = "Assets/Geis/Combat/Prefabs";

        [MenuItem("Tools/Combat/Create Combat UI Prefabs")]
        public static void CreateAllPrefabs()
        {
            EnsureDirectoryExists(PrefabPath);

            CreateEnemyHealthBarPrefab();
            CreateDamagePopupPrefab();
            CreateDamageNumberPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CombatUIPrefabBuilder] Created health bar, damage popup, and damage number prefabs.");
        }

        [MenuItem("Tools/Combat/Create Enemy Health Bar Prefab")]
        public static void CreateEnemyHealthBarPrefab()
        {
            EnsureDirectoryExists(PrefabPath);

            // Root: World Space Canvas with EnemyHealthBar
            var root = new GameObject("EnemyHealthBar");
            root.layer = LayerMask.NameToLayer("UI");

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;
            scaler.referencePixelsPerUnit = 100;
            scaler.scaleFactor = 1;

            root.AddComponent<GraphicRaycaster>();

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 30);
            rectTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            // Slider
            var resources = GetDefaultResources();
            var sliderObj = UnityEngine.UI.DefaultControls.CreateSlider(resources);
            sliderObj.name = "HealthSlider";
            sliderObj.transform.SetParent(root.transform, false);

            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0.5f);
            sliderRect.anchorMax = new Vector2(1, 0.5f);
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.sizeDelta = new Vector2(0, 20);

            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = 100;
            slider.interactable = false;

            // Hide handle for health-bar style (fill only)
            slider.handleRect?.gameObject.SetActive(false);

            // Health text below slider
            var textObj = CreateTextMeshPro("100 / 100", 12);
            textObj.name = "HealthText";
            textObj.transform.SetParent(root.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0);
            textRect.anchorMax = new Vector2(0.5f, 0);
            textRect.anchoredPosition = new Vector2(0, -15);
            textRect.sizeDelta = new Vector2(180, 20);

            // Add EnemyHealthBar component and wire references
            var healthBar = root.AddComponent<EnemyHealthBar>();
            var healthBarSo = new SerializedObject(healthBar);
            healthBarSo.FindProperty("healthBarSlider").objectReferenceValue = slider;
            healthBarSo.FindProperty("healthText").objectReferenceValue = textObj.GetComponent<TMP_Text>();
            healthBarSo.FindProperty("offset").vector3Value = new Vector3(0, 2, 0);
            healthBarSo.FindProperty("alwaysFaceCamera").boolValue = true;
            healthBarSo.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, $"{PrefabPath}/EnemyHealthBar.prefab");
            Object.DestroyImmediate(root);
        }

        [MenuItem("Tools/Combat/Create Damage Popup Prefab")]
        public static void CreateDamagePopupPrefab()
        {
            EnsureDirectoryExists(PrefabPath);

            // Root: World Space Canvas
            var root = new GameObject("DamagePopup");
            root.layer = LayerMask.NameToLayer("UI");

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;
            scaler.referencePixelsPerUnit = 100;

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 50);
            rectTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            // Damage text
            var textObj = CreateTextMeshPro("0", 36);
            textObj.name = "DamageText";
            textObj.transform.SetParent(root.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(150, 60);

            var tmp = textObj.GetComponent<TMP_Text>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            var canvasGroup = root.AddComponent<CanvasGroup>();

            var damagePopup = root.AddComponent<DamagePopup>();
            var popupSo = new SerializedObject(damagePopup);
            popupSo.FindProperty("damageText").objectReferenceValue = tmp;
            popupSo.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            popupSo.FindProperty("moveDuration").floatValue = 1f;
            popupSo.FindProperty("moveDistance").floatValue = 2f;
            popupSo.FindProperty("normalColor").colorValue = Color.white;
            popupSo.FindProperty("criticalColor").colorValue = Color.yellow;
            popupSo.FindProperty("billboardToCamera").boolValue = true;
            popupSo.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, $"{PrefabPath}/DamagePopup.prefab");
            Object.DestroyImmediate(root);
        }

        [MenuItem("Tools/Combat/Create Damage Number Prefab")]
        public static void CreateDamageNumberPrefab()
        {
            EnsureDirectoryExists(PrefabPath);

            // DamageNumber is used as a child of a Screen Space Canvas
            var root = new GameObject("DamageNumber");

            var rectTransform = root.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(80, 40);

            var textObj = CreateTextMeshPro("0", 36);
            textObj.name = "Text";
            textObj.transform.SetParent(root.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textObj.GetComponent<TMP_Text>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            var damageNumber = root.AddComponent<DamageNumber>();
            var numberSo = new SerializedObject(damageNumber);
            numberSo.FindProperty("damageText").objectReferenceValue = tmp;
            numberSo.FindProperty("lifetime").floatValue = 1f;
            numberSo.FindProperty("floatSpeed").floatValue = 50f;
            numberSo.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<CanvasGroup>();

            SavePrefab(root, $"{PrefabPath}/DamageNumber.prefab");
            Object.DestroyImmediate(root);
        }

        private static GameObject CreateTextMeshPro(string text, int fontSize)
        {
            var go = new GameObject();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            return go;
        }

        private static UnityEngine.UI.DefaultControls.Resources GetDefaultResources()
        {
            var resources = new UnityEngine.UI.DefaultControls.Resources();
            resources.standard = GetBuiltinSprite("UI/Skin/UISprite.psd") ?? GetBuiltinSprite("UI/Skin/Knob.psd");
            resources.background = GetBuiltinSprite("UI/Skin/Background.psd");
            resources.inputField = GetBuiltinSprite("UI/Skin/InputFieldBackground.psd");
            resources.knob = GetBuiltinSprite("UI/Skin/Knob.psd");
            return resources;
        }

        private static Sprite GetBuiltinSprite(string path)
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
        }

        private static void EnsureDirectoryExists(string path)
        {
            string[] folders = path.TrimEnd('/').Split('/');
            string current = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string next = current + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, folders[i]);
                current = next;
            }
        }

        private static void SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (prefab != null)
                Debug.Log($"[CombatUIPrefabBuilder] Saved: {path}");
            else
                Debug.LogError($"[CombatUIPrefabBuilder] Failed to save: {path}");
        }
    }
}
