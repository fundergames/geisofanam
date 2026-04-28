using System.Collections.Generic;
using System.IO;
using PerformanceIntelligence.Testing;
using UnityEditor;
using UnityEngine;

namespace PerformanceIntelligence.Editor
{
    [CustomEditor(typeof(PerformanceTestConfig))]
    public sealed class PerformanceTestConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty _sceneTests;
        private SerializedProperty _qualityPresets;
        private SerializedProperty _runsPerConfiguration;
        private SerializedProperty _warmupDurationSeconds;
        private SerializedProperty _captureDurationSeconds;
        private SerializedProperty _outputFolder;
        private SerializedProperty _runInEditorPlayMode;
        private SerializedProperty _exportCsv;
        private SerializedProperty _exportJson;
        private SerializedProperty _generateMarkdownReport;

        private void OnEnable()
        {
            _sceneTests = serializedObject.FindProperty("sceneTests");
            _qualityPresets = serializedObject.FindProperty("qualityPresets");
            _runsPerConfiguration = serializedObject.FindProperty("runsPerConfiguration");
            _warmupDurationSeconds = serializedObject.FindProperty("warmupDurationSeconds");
            _captureDurationSeconds = serializedObject.FindProperty("captureDurationSeconds");
            _outputFolder = serializedObject.FindProperty("outputFolder");
            _runInEditorPlayMode = serializedObject.FindProperty("runInEditorPlayMode");
            _exportCsv = serializedObject.FindProperty("exportCsv");
            _exportJson = serializedObject.FindProperty("exportJson");
            _generateMarkdownReport = serializedObject.FindProperty("generateMarkdownReport");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_qualityPresets, includeChildren: true);
            EditorGUILayout.PropertyField(_runsPerConfiguration);
            EditorGUILayout.PropertyField(_warmupDurationSeconds);
            EditorGUILayout.PropertyField(_captureDurationSeconds);
            EditorGUILayout.PropertyField(_outputFolder);
            EditorGUILayout.PropertyField(_runInEditorPlayMode);
            EditorGUILayout.PropertyField(_exportCsv);
            EditorGUILayout.PropertyField(_exportJson);
            EditorGUILayout.PropertyField(_generateMarkdownReport);

            EditorGUILayout.Space();
            DrawSceneTestsSection();
            EditorGUILayout.Space();

            if (GUILayout.Button("Open Performance Test Runner"))
            {
                PerformanceTestRunnerWindow.ShowWindow();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSceneTestsSection()
        {
            EditorGUILayout.LabelField("Scene Tests", EditorStyles.boldLabel);

            for (int i = 0; i < _sceneTests.arraySize; i++)
            {
                var element = _sceneTests.GetArrayElementAtIndex(i);
                var enabled = element.FindPropertyRelative("enabled");
                var sceneName = element.FindPropertyRelative("sceneName");
                var scenePath = element.FindPropertyRelative("scenePath");
                var cameraPaths = element.FindPropertyRelative("cameraPaths");
                var notes = element.FindPropertyRelative("notes");
                var sceneAsset = element.FindPropertyRelative("sceneAsset");

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(enabled, GUIContent.none, GUILayout.Width(24f));
                EditorGUILayout.LabelField($"Scene Test {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    _sceneTests.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(sceneAsset, new GUIContent("Scene Asset"));
                if (EditorGUI.EndChangeCheck())
                {
                    var assetObject = sceneAsset.objectReferenceValue;
                    if (assetObject != null)
                    {
                        sceneName.stringValue = assetObject.name;
                        scenePath.stringValue = AssetDatabase.GetAssetPath(assetObject);
                    }
                    else
                    {
                        sceneName.stringValue = string.Empty;
                        scenePath.stringValue = string.Empty;
                    }
                }

                using (new EditorGUI.DisabledScope(sceneAsset.objectReferenceValue != null))
                {
                    EditorGUILayout.PropertyField(sceneName);
                    EditorGUILayout.PropertyField(scenePath);
                }

                EditorGUILayout.PropertyField(cameraPaths, includeChildren: true);
                EditorGUILayout.PropertyField(notes);

                if (string.IsNullOrWhiteSpace(sceneName.stringValue))
                {
                    EditorGUILayout.HelpBox("Scene name is required.", MessageType.Warning);
                }
                if (cameraPaths.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("At least one camera path is recommended.", MessageType.Info);
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Scene Test"))
            {
                _sceneTests.InsertArrayElementAtIndex(_sceneTests.arraySize);
                var newElement = _sceneTests.GetArrayElementAtIndex(_sceneTests.arraySize - 1);
                newElement.FindPropertyRelative("enabled").boolValue = true;
                newElement.FindPropertyRelative("sceneName").stringValue = string.Empty;
                newElement.FindPropertyRelative("scenePath").stringValue = string.Empty;
                newElement.FindPropertyRelative("notes").stringValue = string.Empty;
                newElement.FindPropertyRelative("cameraPaths").arraySize = 0;
                newElement.FindPropertyRelative("sceneAsset").objectReferenceValue = null;
            }
        }
    }

    [CustomEditor(typeof(CameraPathDefinition))]
    public sealed class CameraPathDefinitionEditor : UnityEditor.Editor
    {
        private CameraPathDefinition _path;

        private void OnEnable()
        {
            _path = (CameraPathDefinition)target;
            SceneView.duringSceneGui += DuringSceneGui;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGui;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (GUILayout.Button("Append SceneView Camera Waypoint"))
            {
                AppendSceneViewWaypoint(_path);
            }

            if (GUILayout.Button("Normalize Waypoint Times"))
            {
                NormalizeTimes(_path);
            }
        }

        private void DuringSceneGui(SceneView sceneView)
        {
            if (_path == null || _path.waypoints == null || _path.waypoints.Count == 0) return;

            Handles.color = Color.cyan;
            for (int i = 0; i < _path.waypoints.Count; i++)
            {
                var wp = _path.waypoints[i];
                Handles.SphereHandleCap(0, wp.position, Quaternion.identity, 0.5f, EventType.Repaint);
                Handles.Label(wp.position + Vector3.up * 0.4f, $"{i}: {wp.normalizedTime:0.00}");

                if (i < _path.waypoints.Count - 1)
                {
                    Handles.DrawLine(wp.position, _path.waypoints[i + 1].position);
                }
            }
        }

        private static void AppendSceneViewWaypoint(CameraPathDefinition path)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
            {
                Debug.LogWarning("[PerformanceIntelligence] No active SceneView camera.");
                return;
            }

            Undo.RecordObject(path, "Append Camera Waypoint");
            var t = sceneView.camera.transform;
            path.waypoints.Add(new CameraPathWaypoint
            {
                position = t.position,
                rotation = t.rotation,
                normalizedTime = path.waypoints.Count == 0 ? 0f : 1f,
            });
            NormalizeTimes(path);
            EditorUtility.SetDirty(path);
            AssetDatabase.SaveAssets();
        }

        private static void NormalizeTimes(CameraPathDefinition path)
        {
            if (path == null || path.waypoints == null || path.waypoints.Count <= 1) return;
            var w = path.waypoints;
            int n = w.Count - 1;
            for (int i = 0; i < w.Count; i++)
            {
                var item = w[i];
                item.normalizedTime = i / (float)n;
                w[i] = item;
            }
            EditorUtility.SetDirty(path);
        }
    }

    public static class CameraPathAuthoringMenu
    {
        [MenuItem("Assets/Create/Performance Intelligence/Testing/Camera Path From SceneView", priority = 2100)]
        public static void CreateFromSceneView()
        {
            string targetDir = "Assets/PerformanceIntelligence/Data/CameraPaths";
            if (!AssetDatabase.IsValidFolder(targetDir))
            {
                Directory.CreateDirectory(targetDir);
                AssetDatabase.Refresh();
            }

            var path = ScriptableObject.CreateInstance<CameraPathDefinition>();
            path.pathId = $"Path_{System.DateTime.UtcNow:HHmmss}";
            path.playbackDuration = 10f;
            path.interpolation = CameraPathInterpolationType.SmoothStep;
            path.waypoints = new List<CameraPathWaypoint>();

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.camera != null)
            {
                var t = sceneView.camera.transform;
                path.waypoints.Add(new CameraPathWaypoint
                {
                    position = t.position,
                    rotation = t.rotation,
                    normalizedTime = 0f,
                });
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{targetDir}/CameraPath.asset");
            AssetDatabase.CreateAsset(path, assetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = path;
            EditorGUIUtility.PingObject(path);
        }
    }
}
