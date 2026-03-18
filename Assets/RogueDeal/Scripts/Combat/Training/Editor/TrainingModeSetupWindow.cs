using UnityEngine;
using UnityEditor;
using RogueDeal.Combat.TurnBased;

namespace RogueDeal.Combat.Training.Editor
{
    public class TrainingModeSetupWindow : EditorWindow
    {
        private GameObject playerPrefab;
        private GameObject dummyPrefab;
        private Transform spawnPoint;
        
        [MenuItem("RogueDeal/Combat/Setup Training Mode")]
        public static void ShowWindow()
        {
            TrainingModeSetupWindow window = GetWindow<TrainingModeSetupWindow>("Training Mode Setup");
            window.minSize = new Vector2(400, 300);
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Training Mode Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool will help you set up a Training Mode scene for testing attack timings and combos.",
                MessageType.Info
            );
            
            GUILayout.Space(10);
            
            playerPrefab = (GameObject)EditorGUILayout.ObjectField("Player Prefab", playerPrefab, typeof(GameObject), false);
            dummyPrefab = (GameObject)EditorGUILayout.ObjectField("Dummy Prefab", dummyPrefab, typeof(GameObject), false);
            spawnPoint = (Transform)EditorGUILayout.ObjectField("Spawn Point", spawnPoint, typeof(Transform), true);
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("🚀 Quick Setup (All-In-One)", GUILayout.Height(40)))
            {
                QuickSetupAll();
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Or setup components individually:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create Training Mode Manager", GUILayout.Height(30)))
            {
                CreateTrainingModeManager();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Create Combat Presenter", GUILayout.Height(30)))
            {
                CreateCombatPresenter();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Create Training UI", GUILayout.Height(30)))
            {
                CreateTrainingUI();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Add Training Dummy to Scene", GUILayout.Height(30)))
            {
                AddTrainingDummy();
            }
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Validate Current Setup", GUILayout.Height(30)))
            {
                TrainingSetupValidator.ValidateTrainingSetup();
            }
            
            GUILayout.Space(20);
            
            EditorGUILayout.HelpBox(
                "Quick Setup:\n" +
                "1. Click 'Quick Setup' button above\n" +
                "2. Press Play\n" +
                "3. Press F12 to toggle training mode\n" +
                "4. Press SPACE to attack!",
                MessageType.None
            );
        }
        
        private void QuickSetupAll()
        {
            Debug.Log("[TrainingSetup] Starting quick setup...");
            
            CreateTrainingModeManager();
            CreateCombatPresenter();
            AddTrainingDummy();
            
            Debug.Log("[TrainingSetup] Quick setup complete! Press Play and F12 to start training.");
            EditorUtility.DisplayDialog("Setup Complete!", 
                "Training Mode is ready!\n\n" +
                "Press Play, then F12 to activate training mode.\n" +
                "Press SPACE to attack!", 
                "OK");
        }
        
        private void CreateTrainingModeManager()
        {
            TrainingModeManager existing = Object.FindObjectOfType<TrainingModeManager>();
            if (existing != null)
            {
                Debug.Log("TrainingModeManager already exists!");
                Selection.activeGameObject = existing.gameObject;
                return;
            }
            
            GameObject managerObj = new GameObject("TrainingModeManager");
            TrainingModeManager manager = managerObj.AddComponent<TrainingModeManager>();
            AttackVisualizer visualizer = managerObj.AddComponent<AttackVisualizer>();
            FrameDataAnalyzer analyzer = managerObj.AddComponent<FrameDataAnalyzer>();
            TrainingAttackController attackController = managerObj.AddComponent<TrainingAttackController>();
            
            if (dummyPrefab != null)
            {
                SerializedObject so = new SerializedObject(manager);
                so.FindProperty("dummyPrefab").objectReferenceValue = dummyPrefab;
                so.ApplyModifiedProperties();
            }
            
            if (spawnPoint != null)
            {
                SerializedObject so = new SerializedObject(manager);
                so.FindProperty("dummySpawnPoint").objectReferenceValue = spawnPoint;
                so.ApplyModifiedProperties();
            }
            
            Selection.activeGameObject = managerObj;
            EditorGUIUtility.PingObject(managerObj);
            
            Debug.Log("Training Mode Manager created with all components!");
        }
        
        private void CreateCombatPresenter()
        {
            TurnBasedCombatPresenter existing = Object.FindObjectOfType<TurnBasedCombatPresenter>();
            if (existing != null)
            {
                Debug.Log("CombatPresenter already exists!");
                Selection.activeGameObject = existing.gameObject;
                return;
            }
            
            GameObject presenterObj = new GameObject("CombatPresenter");
            TurnBasedCombatPresenter presenter = presenterObj.AddComponent<TurnBasedCombatPresenter>();
            // Note: CombatAbilityExecutor is deprecated - CombatExecutor is added automatically by CombatEntity
            // No need to add it manually anymore
            
            Selection.activeGameObject = presenterObj;
            EditorGUIUtility.PingObject(presenterObj);
            
            Debug.Log("Combat Presenter created!");
        }
        
        private void CreateTrainingUI()
        {
            GameObject canvasObj = new GameObject("TrainingUI_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            GameObject panelObj = new GameObject("TrainingPanel");
            panelObj.transform.SetParent(canvasObj.transform);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0.5f);
            panelRect.anchorMax = new Vector2(0, 0.5f);
            panelRect.pivot = new Vector2(0, 0.5f);
            panelRect.anchoredPosition = new Vector2(10, 0);
            panelRect.sizeDelta = new Vector2(300, 400);
            
            UnityEngine.UI.Image panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);
            
            TrainingUI trainingUI = canvasObj.AddComponent<TrainingUI>();
            
            CreateTextElement(panelObj, "StatusText", new Vector2(10, -10), new Vector2(280, 100));
            CreateTextElement(panelObj, "FrameDataText", new Vector2(10, -120), new Vector2(280, 100));
            CreateTextElement(panelObj, "DummyStatsText", new Vector2(10, -230), new Vector2(280, 80));
            CreateTextElement(panelObj, "ControlsText", new Vector2(10, -320), new Vector2(280, 70));
            
            Selection.activeGameObject = canvasObj;
            EditorGUIUtility.PingObject(canvasObj);
            
            Debug.Log("Training UI created! Connect UI references in the TrainingUI component.");
        }
        
        private void CreateTextElement(GameObject parent, string name, Vector2 position, Vector2 size)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = name;
            text.fontSize = 14;
            text.color = Color.white;
        }
        
        private void AddTrainingDummy()
        {
            if (dummyPrefab == null)
            {
                GameObject dummyObj = new GameObject("TrainingDummy");
                TrainingDummy dummy = dummyObj.AddComponent<TrainingDummy>();
                
                if (spawnPoint != null)
                {
                    dummyObj.transform.position = spawnPoint.position;
                    dummyObj.transform.rotation = spawnPoint.rotation;
                }
                
                Selection.activeGameObject = dummyObj;
                EditorGUIUtility.PingObject(dummyObj);
                
                Debug.Log("Training Dummy created! Add a visual model and Animator.");
            }
            else
            {
                GameObject dummyInstance = PrefabUtility.InstantiatePrefab(dummyPrefab) as GameObject;
                
                if (!dummyInstance.GetComponent<TrainingDummy>())
                {
                    dummyInstance.AddComponent<TrainingDummy>();
                }
                
                if (spawnPoint != null)
                {
                    dummyInstance.transform.position = spawnPoint.position;
                    dummyInstance.transform.rotation = spawnPoint.rotation;
                }
                
                Selection.activeGameObject = dummyInstance;
                EditorGUIUtility.PingObject(dummyInstance);
                
                Debug.Log("Training Dummy instantiated from prefab!");
            }
        }
    }
}
