using UnityEngine;
using UnityEditor;

namespace RogueDeal.Combat.Training.Editor
{
    [CustomEditor(typeof(TrainingModeManager))]
    public class TrainingModeManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty trainingModeActive;
        private SerializedProperty timeScale;
        private SerializedProperty showFrameData;
        private SerializedProperty showHitboxes;
        private SerializedProperty infiniteHealth;
        private SerializedProperty dummyPrefab;
        private SerializedProperty dummySpawnPoint;
        private SerializedProperty dummyBehaviorMode;
        private SerializedProperty recordingEnabled;
        private SerializedProperty maxRecordedInputs;
        private SerializedProperty showAttackArcs;
        private SerializedProperty attackArcColor;
        private SerializedProperty showTimingWindows;
        
        private void OnEnable()
        {
            trainingModeActive = serializedObject.FindProperty("trainingModeActive");
            timeScale = serializedObject.FindProperty("timeScale");
            showFrameData = serializedObject.FindProperty("showFrameData");
            showHitboxes = serializedObject.FindProperty("showHitboxes");
            infiniteHealth = serializedObject.FindProperty("infiniteHealth");
            dummyPrefab = serializedObject.FindProperty("dummyPrefab");
            dummySpawnPoint = serializedObject.FindProperty("dummySpawnPoint");
            dummyBehaviorMode = serializedObject.FindProperty("dummyBehaviorMode");
            recordingEnabled = serializedObject.FindProperty("recordingEnabled");
            maxRecordedInputs = serializedObject.FindProperty("maxRecordedInputs");
            showAttackArcs = serializedObject.FindProperty("showAttackArcs");
            attackArcColor = serializedObject.FindProperty("attackArcColor");
            showTimingWindows = serializedObject.FindProperty("showTimingWindows");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            TrainingModeManager manager = (TrainingModeManager)target;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Training Mode Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure training mode settings and controls.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            if (Application.isPlaying)
            {
                DrawRuntimeControls(manager);
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use Training Mode controls.", MessageType.Warning);
            }
            
            EditorGUILayout.Space(10);
            
            DrawTrainingSettings();
            DrawDummySettings();
            DrawRecordingSettings();
            DrawVisualSettings();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawRuntimeControls(TrainingModeManager manager)
        {
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
            
            GUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(manager.IsTrainingMode ? "Deactivate Training Mode" : "Activate Training Mode", GUILayout.Height(30)))
            {
                manager.ToggleTrainingMode();
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("0.25x", GUILayout.Height(25)))
            {
                Time.timeScale = 0.25f;
            }
            if (GUILayout.Button("0.5x", GUILayout.Height(25)))
            {
                Time.timeScale = 0.5f;
            }
            if (GUILayout.Button("0.75x", GUILayout.Height(25)))
            {
                Time.timeScale = 0.75f;
            }
            if (GUILayout.Button("1.0x", GUILayout.Height(25)))
            {
                Time.timeScale = 1.0f;
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Clear Timing Data", GUILayout.Height(25)))
            {
                manager.ClearTimingData();
            }
            
            GUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Training Mode:", manager.IsTrainingMode ? "Active" : "Inactive");
            EditorGUILayout.LabelField("Time Scale:", manager.CurrentTimeScale.ToString("F2") + "x");
            EditorGUILayout.LabelField("Recorded Attacks:", manager.GetAttackTimings().Count.ToString());
            GUILayout.EndVertical();
        }
        
        private void DrawTrainingSettings()
        {
            EditorGUILayout.LabelField("Training Settings", EditorStyles.boldLabel);
            
            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(trainingModeActive);
            EditorGUILayout.PropertyField(timeScale);
            EditorGUILayout.PropertyField(showFrameData);
            EditorGUILayout.PropertyField(showHitboxes);
            EditorGUILayout.PropertyField(infiniteHealth);
            GUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
        }
        
        private void DrawDummySettings()
        {
            EditorGUILayout.LabelField("Dummy Settings", EditorStyles.boldLabel);
            
            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(dummyPrefab);
            EditorGUILayout.PropertyField(dummySpawnPoint);
            EditorGUILayout.PropertyField(dummyBehaviorMode);
            GUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
        }
        
        private void DrawRecordingSettings()
        {
            EditorGUILayout.LabelField("Recording Settings", EditorStyles.boldLabel);
            
            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(recordingEnabled);
            EditorGUILayout.PropertyField(maxRecordedInputs);
            GUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
        }
        
        private void DrawVisualSettings()
        {
            EditorGUILayout.LabelField("Visual Feedback", EditorStyles.boldLabel);
            
            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(showAttackArcs);
            EditorGUILayout.PropertyField(attackArcColor);
            EditorGUILayout.PropertyField(showTimingWindows);
            GUILayout.EndVertical();
        }
    }
}
