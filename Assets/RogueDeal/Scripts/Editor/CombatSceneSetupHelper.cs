using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RogueDeal.Combat;
using RogueDeal.UI;
using RogueDeal.Levels;
using UnityEditor.SceneManagement;

namespace RogueDeal.Editor
{
    public class CombatSceneSetupHelper : EditorWindow
    {
        [MenuItem("Funder Games/Rogue Deal/Setup Combat Scene")]
        public static void ShowWindow()
        {
            GetWindow<CombatSceneSetupHelper>("Combat Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Combat Scene Setup Helper", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Create Complete Combat Scene", GUILayout.Height(50)))
            {
                CreateCompleteCombatScene();
            }
            
            GUILayout.Space(10);

            if (GUILayout.Button("Auto-Wire Scene References", GUILayout.Height(40)))
            {
                AutoWireReferences();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Create CardLayoutConfig", GUILayout.Height(30)))
            {
                CreateCardLayoutConfig();
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This will automatically connect all references in your combat scene.\n\n" +
                "Required objects in scene:\n" +
                "- CombatController\n" +
                "- PlayerSpawnPoint\n" +
                "- EnemySpawns/Enemy_1,2,3\n" +
                "- Canvas/CardHand\n" +
                "- Canvas/CombatUI\n\n" +
                "Note: You still need to create the CardVisual prefab manually.",
                MessageType.Info);
        }

        private void AutoWireReferences()
        {
            Debug.Log("=== Starting Combat Scene Auto-Wire ===");
            
            WireCombatController();
            WireCardHandUI();
            WireCombatUIController();
            
            Debug.Log("=== Auto-Wire Complete! ===");
            EditorUtility.DisplayDialog("Success", 
                "Combat scene references have been wired!\n\n" +
                "Next steps:\n" +
                "1. Create CardVisual prefab\n" +
                "2. Assign prefab to CardHand\n" +
                "3. Run 'Create Example Data' if not done\n" +
                "4. Assign test level to CombatController", 
                "OK");
        }

        private void WireCombatController()
        {
            var controller = GameObject.Find("CombatController");
            if (controller == null)
            {
                Debug.LogError("CombatController GameObject not found!");
                return;
            }

            var combatController = controller.GetComponent<CombatController>();
            if (combatController == null)
            {
                Debug.LogError("CombatController component not found!");
                return;
            }
            
            var bootstrap = controller.GetComponent<CombatSceneBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = controller.AddComponent<CombatSceneBootstrap>();
                Debug.Log("✓ Added CombatSceneBootstrap component");
            }

            var playerSpawn = GameObject.Find("PlayerSpawnPoint");
            if (playerSpawn != null)
            {
                SerializedObject so = new SerializedObject(combatController);
                so.FindProperty("playerSpawnPoint").objectReferenceValue = playerSpawn.transform;
                
                var enemySpawns = GameObject.Find("EnemySpawns");
                if (enemySpawns != null)
                {
                    var enemy1 = enemySpawns.transform.Find("Enemy_1");
                    var enemy2 = enemySpawns.transform.Find("Enemy_2");
                    var enemy3 = enemySpawns.transform.Find("Enemy_3");
                    
                    SerializedProperty enemySpawnsProp = so.FindProperty("enemySpawnPoints");
                    enemySpawnsProp.arraySize = 3;
                    
                    if (enemy1 != null) enemySpawnsProp.GetArrayElementAtIndex(0).objectReferenceValue = enemy1;
                    if (enemy2 != null) enemySpawnsProp.GetArrayElementAtIndex(1).objectReferenceValue = enemy2;
                    if (enemy3 != null) enemySpawnsProp.GetArrayElementAtIndex(2).objectReferenceValue = enemy3;
                }
                
                var testLevel = Resources.Load<LevelDefinition>("Data/Levels/Level_Test");
                if (testLevel == null)
                {
                    testLevel = AssetDatabase.LoadAssetAtPath<LevelDefinition>(
                        AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:LevelDefinition")[0]));
                }
                
                if (testLevel != null)
                {
                    so.FindProperty("testLevel").objectReferenceValue = testLevel;
                    Debug.Log("✓ Assigned test level: " + testLevel.name);
                }
                else
                {
                    Debug.LogWarning("⚠ No LevelDefinition found. Run 'Create Example Data' first!");
                }
                
                so.ApplyModifiedProperties();
                Debug.Log("✓ CombatController wired");
            }
        }

        private void WireCardHandUI()
        {
            var cardHand = GameObject.Find("CardHand");
            if (cardHand == null)
            {
                Debug.LogError("CardHand GameObject not found!");
                return;
            }

            var cardHandUI = cardHand.GetComponent<CardHandUI>();
            if (cardHandUI == null)
            {
                Debug.LogError("CardHandUI component not found!");
                return;
            }

            var cardContainer = cardHand.transform.Find("CardContainer");
            if (cardContainer != null)
            {
                SerializedObject so = new SerializedObject(cardHandUI);
                so.FindProperty("cardContainer").objectReferenceValue = cardContainer;
                
                var layoutConfig = Resources.Load<CardLayoutConfig>("Configs/CardLayoutConfig");
                if (layoutConfig != null)
                {
                    so.FindProperty("layoutConfig").objectReferenceValue = layoutConfig;
                    Debug.Log("✓ Assigned CardLayoutConfig");
                }
                else
                {
                    Debug.LogWarning("⚠ CardLayoutConfig not found at Resources/Configs/CardLayoutConfig");
                }
                
                var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RogueDeal/Prefabs/UI/CardVisual.prefab");
                if (cardPrefab != null)
                {
                    so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
                    Debug.Log("✓ Assigned CardVisual prefab");
                }
                else
                {
                    Debug.LogWarning("⚠ CardVisual prefab not found. You need to create it!");
                }
                
                so.FindProperty("autoFlipOnDeal").boolValue = true;
                so.FindProperty("autoFlipDelay").floatValue = 0.5f;
                
                so.ApplyModifiedProperties();
                Debug.Log("✓ CardHandUI wired");
            }
        }

        private void WireCombatUIController()
        {
            var combatUI = GameObject.Find("CombatUI");
            if (combatUI == null)
            {
                Debug.LogError("CombatUI GameObject not found!");
                return;
            }

            var combatUIController = combatUI.GetComponent<CombatUIController>();
            if (combatUIController == null)
            {
                Debug.LogError("CombatUIController component not found!");
                return;
            }

            SerializedObject so = new SerializedObject(combatUIController);
            
            var cardHand = GameObject.Find("CardHand");
            if (cardHand != null)
            {
                var cardHandUI = cardHand.GetComponent<CardHandUI>();
                so.FindProperty("cardHandUI").objectReferenceValue = cardHandUI;
            }
            
            var drawButton = combatUI.transform.Find("DrawButton");
            if (drawButton != null)
            {
                so.FindProperty("drawButton").objectReferenceValue = drawButton.GetComponent<Button>();
            }
            
            var handResultText = combatUI.transform.Find("HandResultText");
            if (handResultText != null)
            {
                so.FindProperty("handResultText").objectReferenceValue = handResultText.GetComponent<TextMeshProUGUI>();
            }
            
            var damageText = combatUI.transform.Find("DamageText");
            if (damageText != null)
            {
                so.FindProperty("damageText").objectReferenceValue = damageText.GetComponent<TextMeshProUGUI>();
            }
            
            var turnCounterText = combatUI.transform.Find("TurnCounterText");
            if (turnCounterText != null)
            {
                so.FindProperty("turnCounterText").objectReferenceValue = turnCounterText.GetComponent<TextMeshProUGUI>();
            }
            
            so.ApplyModifiedProperties();
            Debug.Log("✓ CombatUIController wired");
        }

        private void CreateCardLayoutConfig()
        {
            string path = "Assets/RogueDeal/Resources/Configs/CardLayoutConfig.asset";
            
            var existing = AssetDatabase.LoadAssetAtPath<CardLayoutConfig>(path);
            if (existing != null)
            {
                Debug.Log("CardLayoutConfig already exists at: " + path);
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }
            
            var config = ScriptableObject.CreateInstance<CardLayoutConfig>();
            config.layoutType = LayoutType.Arc;
            config.arcAngle = 30f;
            config.arcRadius = 800f;
            config.cardSpacing = 150f;
            config.dealDuration = 0.5f;
            config.flipDuration = 0.3f;
            config.replaceDuration = 0.4f;
            
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            
            Debug.Log("✓ Created CardLayoutConfig at: " + path);
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        private void CreateCompleteCombatScene()
        {
            if (!EditorUtility.DisplayDialog("Create Combat Scene", 
                "This will create the complete combat scene structure. Continue?", 
                "Yes", "Cancel"))
            {
                return;
            }

            Debug.Log("=== Creating Complete Combat Scene ===");

            CreateCombatController();
            CreateCanvas();
            CreateEventSystem();
            AutoWireReferences();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("=== Combat Scene Created Successfully! ===");
            EditorUtility.DisplayDialog("Success", 
                "Combat scene has been created!\n\nPress Play to test.", 
                "OK");
        }

        private void CreateCombatController()
        {
            var existing = GameObject.Find("CombatController");
            if (existing != null)
            {
                Debug.Log("CombatController already exists");
                
                if (existing.GetComponent<CombatSceneInitializer>() == null)
                {
                    existing.AddComponent<CombatSceneInitializer>();
                    Debug.Log("✓ Added CombatSceneInitializer");
                }
                
                if (existing.GetComponent<CombatSceneDebugger>() == null)
                {
                    existing.AddComponent<CombatSceneDebugger>();
                    Debug.Log("✓ Added CombatSceneDebugger");
                }
                
                return;
            }

            var go = new GameObject("CombatController");
            go.AddComponent<CombatSceneInitializer>();
            go.AddComponent<CombatController>();
            go.AddComponent<CombatSceneBootstrap>();
            go.AddComponent<CombatSceneDebugger>();

            var playerSpawn = new GameObject("PlayerSpawnPoint");
            playerSpawn.transform.position = new Vector3(0, 0, 0);
            playerSpawn.transform.parent = go.transform;

            var enemySpawns = new GameObject("EnemySpawns");
            enemySpawns.transform.parent = go.transform;

            for (int i = 1; i <= 3; i++)
            {
                var enemy = new GameObject($"Enemy_{i}");
                enemy.transform.position = new Vector3(4 + i, 0, 0);
                enemy.transform.parent = enemySpawns.transform;
            }

            Debug.Log("✓ Created CombatController with spawn points");
        }

        private void CreateCanvas()
        {
            var existing = GameObject.Find("Canvas");
            Canvas canvas;
            
            if (existing != null)
            {
                canvas = existing.GetComponent<Canvas>();
                Debug.Log("Canvas already exists");
            }
            else
            {
                var canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                var scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                Debug.Log("✓ Created Canvas");
            }

            CreateCardHandUI(canvas.transform);
            CreateCombatUI(canvas.transform);
        }

        private void CreateCardHandUI(Transform parent)
        {
            var existing = parent.Find("CardHand");
            if (existing != null)
            {
                Debug.Log("CardHand already exists");
                return;
            }

            var cardHand = new GameObject("CardHand");
            cardHand.transform.SetParent(parent, false);
            
            var rect = cardHand.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 100);
            rect.sizeDelta = new Vector2(1000, 300);
            
            cardHand.AddComponent<CardHandUI>();

            var cardContainer = new GameObject("CardContainer");
            cardContainer.transform.SetParent(cardHand.transform, false);
            var containerRect = cardContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;
            containerRect.anchoredPosition = Vector2.zero;

            Debug.Log("✓ Created CardHand UI");
        }

        private void CreateCombatUI(Transform parent)
        {
            var existing = parent.Find("CombatUI");
            if (existing != null)
            {
                Debug.Log("CombatUI already exists");
                return;
            }

            var combatUI = new GameObject("CombatUI");
            combatUI.transform.SetParent(parent, false);
            
            var rect = combatUI.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            
            combatUI.AddComponent<CombatUIController>();

            CreateDrawButton(combatUI.transform);
            CreateText(combatUI.transform, "HandResultText", new Vector2(0, 500), 36);
            CreateText(combatUI.transform, "DamageText", new Vector2(0, 450), 32);
            CreateText(combatUI.transform, "TurnCounterText", new Vector2(-800, 500), 24);

            Debug.Log("✓ Created CombatUI");
        }

        private void CreateDrawButton(Transform parent)
        {
            var btnGO = new GameObject("DrawButton");
            btnGO.transform.SetParent(parent, false);
            
            var rect = btnGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 450);
            rect.sizeDelta = new Vector2(200, 60);
            
            var img = btnGO.AddComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 1f);
            
            btnGO.AddComponent<Button>();
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "DRAW";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private void CreateText(Transform parent, string name, Vector2 position, float fontSize)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);
            
            var rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(600, 100);
            
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private void CreateEventSystem()
        {
            var existing = GameObject.Find("EventSystem");
            if (existing != null)
            {
                Debug.Log("EventSystem already exists");
                return;
            }

            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
            
            Debug.Log("✓ Created EventSystem");
        }
    }
}
