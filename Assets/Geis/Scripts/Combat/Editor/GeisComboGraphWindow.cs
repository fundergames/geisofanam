/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Geis.Combat.Editor
{
    public class GeisComboGraphWindow : EditorWindow
    {
        private const float ToolbarHeight = 24f;
        private const float InspectorWidth = 380f;
        private const float NodeWidth = 220f;
        private const float NodeHeight = 198f;
        private const float HorizontalSpacing = 300f;
        private const float VerticalSpacing = 175f;
        private const float MinZoom = 0.55f;
        private const float MaxZoom = 1.75f;
        private const float PortRadius = 11f;

        private GeisComboData _comboData;
        private SerializedObject _serializedCombo;
        private SerializedProperty _transitionsProp;
        private SerializedProperty _clipsProp;
        private SerializedProperty _fallbackClipProp;
        private SerializedProperty _stateCombatBindingsProp;
        private SerializedProperty _cancelWindowStartProp;
        private SerializedProperty _cancelWindowEndProp;
        private SerializedProperty _stateTimingsProp;

        private int _selectedState = 0;
        private Vector2 _canvasPan = new Vector2(90f, 70f);
        private float _zoom = 1f;
        private bool _draggingCanvas;
        private Vector2 _inspectorScroll;
        private int _draggingNodeState = -1;
        private Vector2 _lastDragWorldPosition;
        private bool _creatingTransition;
        private int _transitionFromState = -1;
        private GeisComboInputType _transitionInputType;
        [SerializeField] private List<NodePositionOverride> _nodePositionOverrides = new List<NodePositionOverride>();

        private GameObject _previewTarget;
        private readonly List<GeisComboInputType> _previewInputs = new List<GeisComboInputType>();
        private readonly List<int> _previewStates = new List<int>();
        private string _previewMessage = "Build a preview path with Light/Heavy inputs.";
        private int _previewSelectedStep;
        private float _previewNormalizedTime;
        private bool _previewTimeDrivenByScrub;
        [SerializeField] private bool _loopIdlePreview = true;
        private bool _loopPreview;
        private bool _playingPreview;
        private readonly List<int> _playbackStates = new List<int>();
        private int _playbackStepIndex;
        private double _playbackStepStartedAt;
        private PreviewRenderUtility _nodePreviewUtility;
        private GameObject _nodePreviewInstance;
        private GameObject _nodePreviewSource;
        [SerializeField] private float _nodePreviewYaw = 160f;
        [SerializeField] private float _nodePreviewPitch = 12f;
        [SerializeField] private float _nodePreviewDistanceMultiplier = 2.8f;
        [SerializeField] private float _nodePreviewHeightBias = 0.25f;
        [SerializeField] private float _nodePreviewFieldOfView = 30f;

        [MenuItem("Tools/Geis/Combo Graph")]
        public static void ShowWindow()
        {
            Open(null);
        }

        public static void Open(GeisComboData comboData)
        {
            GeisComboGraphWindow window = GetWindow<GeisComboGraphWindow>("Combo Graph");
            window.minSize = new Vector2(1100f, 700f);
            if (comboData != null)
                window.LoadComboData(comboData);
            else
                window.TryLoadSelection();

            window.Focus();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            if (_comboData == null)
                TryLoadSelection();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopPreview(clearSampling: true);
            CleanupNodePreviewResources();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_comboData == null)
            {
                DrawEmptyState();
                return;
            }

            if (_serializedCombo == null || _serializedCombo.targetObject != _comboData)
                LoadComboData(_comboData);

            _serializedCombo.Update();

            Rect canvasRect = new Rect(0f, ToolbarHeight, position.width - InspectorWidth, position.height - ToolbarHeight);
            Rect inspectorRect = new Rect(position.width - InspectorWidth, ToolbarHeight, InspectorWidth, position.height - ToolbarHeight);

            ProcessCanvasEvents(Event.current, canvasRect);
            DrawCanvas(canvasRect);
            DrawInspector(inspectorRect);

            bool hadModifiedProperties = _serializedCombo.hasModifiedProperties;
            _serializedCombo.ApplyModifiedProperties();
            if (hadModifiedProperties)
            {
                EditorUtility.SetDirty(_comboData);
                RebuildPreviewPath();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(ToolbarHeight));

            EditorGUI.BeginChangeCheck();
            GeisComboData nextCombo = (GeisComboData)EditorGUILayout.ObjectField(
                _comboData,
                typeof(GeisComboData),
                false,
                GUILayout.Width(320f));
            if (EditorGUI.EndChangeCheck())
                LoadComboData(nextCombo);

            if (GUILayout.Button("Use Selection", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                TryLoadSelection();

            using (new EditorGUI.DisabledScope(_comboData == null))
            {
                if (GUILayout.Button("Add State", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    AppendState();

                if (GUILayout.Button("Auto Layout", EditorStyles.toolbarButton, GUILayout.Width(85f)))
                    AutoLayout();

                if (GUILayout.Button("Frame Graph", EditorStyles.toolbarButton, GUILayout.Width(85f)))
                    FrameGraph();

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                    SaveComboAsset();
            }

            GUILayout.FlexibleSpace();

            if (_comboData != null)
                GUILayout.Label(_comboData.name, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            Rect body = new Rect(0f, ToolbarHeight, position.width, position.height - ToolbarHeight);
            GUILayout.BeginArea(body);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Width(420f));
            GUILayout.Label("No GeisComboData selected", EditorStyles.boldLabel);
            GUILayout.Space(8f);
            GUILayout.Label(
                "Open a combo asset or drag one into the toolbar field to visualize its states as a graph and preview a combo path.",
                EditorStyles.wordWrappedLabel);
            GUILayout.Space(16f);

            if (GUILayout.Button("Load Selected Combo Data", GUILayout.Height(30f)))
                TryLoadSelection();

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        private void DrawCanvas(Rect canvasRect)
        {
            EditorGUI.DrawRect(canvasRect, new Color(0.12f, 0.12f, 0.13f));
            DrawGrid(canvasRect);

            List<int> states = GetAllStates();
            Dictionary<int, Vector2> layout = BuildLayout(states);

            DrawConnections(canvasRect, layout);
            if (_creatingTransition)
                DrawPendingConnection(canvasRect, layout);

            foreach (int state in states)
                DrawNode(canvasRect, state, layout[state]);

            DrawCanvasLegend(canvasRect);
        }

        private void DrawGrid(Rect canvasRect)
        {
            Handles.BeginGUI();
            Color previousColor = Handles.color;

            Vector2 topLeft = ScreenToWorld(canvasRect.position, canvasRect);
            Vector2 bottomRight = ScreenToWorld(canvasRect.position + canvasRect.size, canvasRect);

            float gridSize = 50f;
            int startX = Mathf.FloorToInt(topLeft.x / gridSize) - 1;
            int endX = Mathf.CeilToInt(bottomRight.x / gridSize) + 1;
            int startY = Mathf.FloorToInt(topLeft.y / gridSize) - 1;
            int endY = Mathf.CeilToInt(bottomRight.y / gridSize) + 1;

            Handles.color = new Color(1f, 1f, 1f, 0.06f);
            for (int x = startX; x <= endX; x++)
            {
                Vector2 from = WorldToScreen(new Vector2(x * gridSize, topLeft.y), canvasRect);
                Vector2 to = WorldToScreen(new Vector2(x * gridSize, bottomRight.y), canvasRect);
                Handles.DrawLine(from, to);
            }

            for (int y = startY; y <= endY; y++)
            {
                Vector2 from = WorldToScreen(new Vector2(topLeft.x, y * gridSize), canvasRect);
                Vector2 to = WorldToScreen(new Vector2(bottomRight.x, y * gridSize), canvasRect);
                Handles.DrawLine(from, to);
            }

            Handles.color = previousColor;
            Handles.EndGUI();
        }

        private void DrawConnections(Rect canvasRect, Dictionary<int, Vector2> layout)
        {
            Handles.BeginGUI();
            Color previousColor = Handles.color;

            foreach (ComboTransitionView transition in GetTransitions())
            {
                if (!layout.TryGetValue(transition.FromState, out Vector2 fromPosition)
                    || !layout.TryGetValue(transition.ToState, out Vector2 toPosition))
                {
                    continue;
                }

                Rect fromRect = GetNodeScreenRect(canvasRect, fromPosition);
                Rect toRect = GetNodeScreenRect(canvasRect, toPosition);
                Vector3 start = GetOutputPortPosition(fromRect, transition.InputType);
                Vector3 end = GetInputPortPosition(toRect, transition.InputType);
                Color lineColor = transition.InputType == GeisComboInputType.Light
                    ? new Color(0.53f, 0.82f, 1f)
                    : new Color(1f, 0.63f, 0.4f);

                bool pathHighlighted = IsPreviewTransition(transition.FromState, transition.ToState);
                float thickness = pathHighlighted ? 5f : 3f;
                if (transition.FromState == _selectedState || transition.ToState == _selectedState)
                    thickness += 1f;

                Handles.DrawBezier(
                    start,
                    end,
                    start + Vector3.right * 75f,
                    end + Vector3.left * 75f,
                    lineColor,
                    null,
                    thickness);

                Vector2 labelPosition = (start + end) * 0.5f;
                Rect labelRect = new Rect(labelPosition.x - 14f, labelPosition.y - 11f, 28f, 22f);
                GUI.Box(labelRect, GUIContent.none);
                GUI.Label(labelRect, GetInputTypeLabel(transition.InputType), EditorStyles.centeredGreyMiniLabel);
            }

            Handles.color = previousColor;
            Handles.EndGUI();
        }

        private void DrawNode(Rect canvasRect, int state, Vector2 worldPosition)
        {
            Rect screenRect = GetNodeScreenRect(canvasRect, worldPosition);
            bool selected = state == _selectedState;
            bool previewFocused = _previewStates.Count > 0 && _previewSelectedStep >= 0 &&
                                  _previewSelectedStep < _previewStates.Count &&
                                  _previewStates[_previewSelectedStep] == state;
            bool inPreviewPath = _previewStates.Contains(state);

            Color background = new Color(0.2f, 0.22f, 0.25f);
            if (selected)
                background = new Color(0.25f, 0.4f, 0.63f);

            EditorGUI.DrawRect(screenRect, background);
            GUI.Box(screenRect, GUIContent.none);
            DrawNodeBorder(screenRect, inPreviewPath, previewFocused, selected);

            float scale = Mathf.Clamp(_zoom, 0.7f, 1.2f);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.white },
                fontSize = Mathf.RoundToInt(12f * scale)
            };

            GUIStyle bodyStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                fontSize = Mathf.RoundToInt(10f * scale)
            };

            Rect titleRect = new Rect(screenRect.x + 10f, screenRect.y + 8f, screenRect.width - 20f, 20f);
            GUI.Label(titleRect, $"State {state}", titleStyle);

            AnimationClip clip = GetClipForStateSerialized(state);
            string clipName = clip != null ? clip.name : "(fallback / none)";
            GetCancelWindowSerialized(state, out float cancelStart, out float cancelEnd);

            string nodeBody = _zoom >= 0.75f
                ? $"{clipName}\nCancel: {cancelStart:0.00} - {cancelEnd:0.00}  |  Out: {GetOutgoingTransitions(state).Count}"
                : clipName;

            Rect bodyRect = new Rect(screenRect.x + 10f, screenRect.y + 34f, screenRect.width - 20f, 32f);
            GUI.Label(bodyRect, nodeBody, bodyStyle);

            Rect previewRect = GetNodePreviewRect(screenRect);
            bool showLivePreview = _playingPreview ? previewFocused : selected;
            DrawNodeClipPreview(previewRect, state, showLivePreview);
            DrawNodePorts(screenRect);
        }

        private void DrawNodeBorder(Rect nodeRect, bool inPreviewPath, bool previewFocused, bool selected)
        {
            Handles.BeginGUI();
            Color previousColor = Handles.color;

            if (selected)
            {
                Handles.color = new Color(0.55f, 0.76f, 1f, 0.95f);
                Handles.DrawAAPolyLine(3f, GetRectOutline(nodeRect));
            }

            if (inPreviewPath)
            {
                Handles.color = previewFocused
                    ? new Color(0.38f, 1f, 0.45f, 1f)
                    : new Color(1f, 0.86f, 0.22f, 0.95f);
                Handles.DrawAAPolyLine(previewFocused ? 5f : 3f, GetRectOutline(nodeRect, inset: 1.5f));
            }

            Handles.color = previousColor;
            Handles.EndGUI();
        }

        private static Vector3[] GetRectOutline(Rect rect, float inset = 0f)
        {
            Rect insetRect = new Rect(
                rect.xMin + inset,
                rect.yMin + inset,
                Mathf.Max(0f, rect.width - inset * 2f),
                Mathf.Max(0f, rect.height - inset * 2f));

            return new[]
            {
                new Vector3(insetRect.xMin, insetRect.yMin, 0f),
                new Vector3(insetRect.xMax, insetRect.yMin, 0f),
                new Vector3(insetRect.xMax, insetRect.yMax, 0f),
                new Vector3(insetRect.xMin, insetRect.yMax, 0f),
                new Vector3(insetRect.xMin, insetRect.yMin, 0f)
            };
        }

        private Rect GetNodePreviewRect(Rect nodeRect)
        {
            float size = Mathf.Min(nodeRect.width - 20f, nodeRect.height - 84f);
            size = Mathf.Max(size, 32f);
            return new Rect(
                nodeRect.center.x - size * 0.5f,
                nodeRect.yMax - size - 10f,
                size,
                size);
        }

        private void DrawNodeClipPreview(Rect rect, int state, bool showLivePreview)
        {
            GUI.Box(rect, GUIContent.none);

            AnimationClip clip = GetClipForStateSerialized(state);
            if (clip == null)
            {
                GUI.Label(rect, "No clip", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (showLivePreview && TryDrawLiveNodePreview(rect, clip))
            {
                Rect labelRect = new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 16f);
                GUI.Label(labelRect, "Live Preview", EditorStyles.whiteMiniLabel);
                return;
            }

            Texture2D preview = AssetPreview.GetAssetPreview(clip);
            if (preview == null)
                preview = AssetPreview.GetMiniThumbnail(clip);

            if (preview != null)
            {
                GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
                return;
            }

            GUI.Label(rect, clip.name, EditorStyles.centeredGreyMiniLabel);
        }

        private bool TryDrawLiveNodePreview(Rect rect, AnimationClip clip)
        {
            if (_previewTarget == null || clip == null)
                return false;

            if (!EnsureNodePreviewResources())
                return false;

            float clipLength = Mathf.Max(clip.length, 0.01f);
            float normalizedTime = GetNodePreviewNormalizedTime(clipLength);
            clip.SampleAnimation(_nodePreviewInstance, normalizedTime * clipLength);

            Bounds bounds = CalculatePreviewBounds(_nodePreviewInstance);
            ConfigureNodePreviewCamera(bounds);

            _nodePreviewUtility.BeginPreview(rect, GUIStyle.none);
            _nodePreviewUtility.camera.Render();
            Texture previewTexture = _nodePreviewUtility.EndPreview();
            GUI.DrawTexture(rect, previewTexture, ScaleMode.StretchToFill, false);
            return true;
        }

        private float GetNodePreviewNormalizedTime(float clipLength)
        {
            if (_playingPreview && _previewStates.Count > 0 && _previewSelectedStep >= 0 && _previewSelectedStep < _previewStates.Count)
                return Mathf.Repeat(_previewNormalizedTime, 1f);

            if (_previewTimeDrivenByScrub || !_loopIdlePreview)
                return Mathf.Clamp01(_previewNormalizedTime);

            double loopedTime = EditorApplication.timeSinceStartup % clipLength;
            return Mathf.Clamp01((float)(loopedTime / clipLength));
        }

        private bool EnsureNodePreviewResources()
        {
            if (_previewTarget == null)
            {
                CleanupNodePreviewResources();
                return false;
            }

            if (_nodePreviewUtility != null && _nodePreviewInstance != null && _nodePreviewSource == _previewTarget)
                return true;

            CleanupNodePreviewResources();

            _nodePreviewSource = _previewTarget;
            _nodePreviewUtility = new PreviewRenderUtility();
            _nodePreviewUtility.cameraFieldOfView = _nodePreviewFieldOfView;
            _nodePreviewUtility.camera.clearFlags = CameraClearFlags.Color;
            _nodePreviewUtility.camera.backgroundColor = new Color(0.2f, 0.2f, 0.22f, 1f);
            _nodePreviewUtility.camera.nearClipPlane = 0.01f;
            _nodePreviewUtility.camera.farClipPlane = 100f;
            _nodePreviewUtility.lights[0].intensity = 1.15f;
            _nodePreviewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
            _nodePreviewUtility.lights[1].intensity = 1.05f;
            _nodePreviewUtility.lights[1].transform.rotation = Quaternion.Euler(340f, 218f, 177f);
            _nodePreviewUtility.ambientColor = new Color(0.42f, 0.42f, 0.42f, 1f);

            _nodePreviewInstance = Instantiate(_previewTarget);
            _nodePreviewInstance.name = $"{_previewTarget.name}_ComboPreview";
            _nodePreviewInstance.hideFlags = HideFlags.HideAndDontSave;
            _nodePreviewInstance.transform.position = Vector3.zero;
            _nodePreviewInstance.transform.rotation = Quaternion.identity;
            _nodePreviewInstance.transform.localScale = _previewTarget.transform.lossyScale;

            foreach (Transform transform in _nodePreviewInstance.GetComponentsInChildren<Transform>(true))
                transform.gameObject.hideFlags = HideFlags.HideAndDontSave;

            foreach (MonoBehaviour behaviour in _nodePreviewInstance.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;

            _nodePreviewUtility.AddSingleGO(_nodePreviewInstance);
            return true;
        }

        private void CleanupNodePreviewResources()
        {
            if (_nodePreviewUtility != null)
            {
                _nodePreviewUtility.Cleanup();
                _nodePreviewUtility = null;
            }

            if (_nodePreviewInstance != null)
            {
                DestroyImmediate(_nodePreviewInstance);
                _nodePreviewInstance = null;
            }

            _nodePreviewSource = null;
        }

        private void ConfigureNodePreviewCamera(Bounds bounds)
        {
            _nodePreviewUtility.cameraFieldOfView = _nodePreviewFieldOfView;
            Vector3 target = bounds.center + Vector3.up * (bounds.extents.y * _nodePreviewHeightBias);
            float radius = Mathf.Max(bounds.extents.magnitude, 0.5f);
            Vector3 viewDirection = Quaternion.Euler(_nodePreviewPitch, _nodePreviewYaw, 0f) * Vector3.forward;
            _nodePreviewUtility.camera.transform.position = target - viewDirection.normalized * radius * _nodePreviewDistanceMultiplier;
            _nodePreviewUtility.camera.transform.LookAt(target);
        }

        private static Bounds CalculatePreviewBounds(GameObject previewRoot)
        {
            Renderer[] renderers = previewRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(previewRoot.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private void DrawCanvasLegend(Rect canvasRect)
        {
            Rect legendRect = new Rect(canvasRect.x + 10f, canvasRect.y + 10f, 200f, 70f);
            GUI.Box(legendRect, GUIContent.none);
            GUI.Label(new Rect(legendRect.x + 10f, legendRect.y + 8f, 180f, 18f), "Graph Controls", EditorStyles.boldLabel);
            GUI.Label(new Rect(legendRect.x + 10f, legendRect.y + 28f, 180f, 16f), "Middle mouse drag: pan", EditorStyles.miniLabel);
            GUI.Label(new Rect(legendRect.x + 10f, legendRect.y + 43f, 180f, 16f), "Mouse wheel: zoom", EditorStyles.miniLabel);
            GUI.Label(new Rect(legendRect.x + 10f, legendRect.y + 58f, 180f, 16f), "Drag empty L/H output to input", EditorStyles.miniLabel);
        }

        private void DrawInspector(Rect inspectorRect)
        {
            EditorGUI.DrawRect(inspectorRect, new Color(0.16f, 0.16f, 0.17f));

            GUILayout.BeginArea(inspectorRect);
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

            DrawComboSummary();
            EditorGUILayout.Space(10f);
            DrawSelectedStateSection();
            EditorGUILayout.Space(10f);
            DrawPreviewSection();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawComboSummary()
        {
            EditorGUILayout.LabelField("Combo Data", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Asset", _comboData, typeof(GeisComboData), false);
            }

            EditorGUILayout.PropertyField(_fallbackClipProp);
            EditorGUILayout.Slider(_cancelWindowStartProp, 0f, 1f);
            EditorGUILayout.Slider(_cancelWindowEndProp, 0f, 1f);

            EditorGUILayout.HelpBox(
                $"States: {GetAllStates().Count}  |  Transitions: {_transitionsProp.arraySize}  |  Clip Slots: {_clipsProp.arraySize}",
                MessageType.Info);

            if (_cancelWindowEndProp.floatValue < _cancelWindowStartProp.floatValue)
                EditorGUILayout.HelpBox("Shared cancel window end is before start. The runtime clamps end to start.", MessageType.Warning);
        }

        private void DrawSelectedStateSection()
        {
            EditorGUILayout.LabelField("Selected State", EditorStyles.boldLabel);
            _selectedState = EditorGUILayout.IntField("State Index", Mathf.Max(0, _selectedState));

            bool hasClipSlot = _selectedState < _clipsProp.arraySize;
            bool hasTimingSlot = _selectedState < _stateTimingsProp.arraySize;
            bool hasBindingSlot = _selectedState < _stateCombatBindingsProp.arraySize;
            bool hasMaterializedData = hasClipSlot || hasTimingSlot || hasBindingSlot;

            if (!hasMaterializedData)
            {
                EditorGUILayout.HelpBox(
                    "This state exists in the graph via transitions, but there is no authored clip/timing slot for it yet.",
                    MessageType.Warning);

                if (GUILayout.Button("Materialize State Slot"))
                    EnsureStateExists(_selectedState);
            }

            if (_selectedState < _clipsProp.arraySize)
            {
                SerializedProperty clipProp = _clipsProp.GetArrayElementAtIndex(_selectedState);
                EditorGUILayout.PropertyField(clipProp, new GUIContent("Clip"));

                AnimationClip clip = clipProp.objectReferenceValue as AnimationClip;
                if (clip != null)
                    EditorGUILayout.LabelField("Clip Length", $"{clip.length:0.00}s");
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Timing Override", EditorStyles.miniBoldLabel);
            if (!hasTimingSlot)
            {
                EditorGUILayout.HelpBox("Materialize this state to author a per-step cancel window.", MessageType.None);
            }
            else
            {
                SerializedProperty timingProp = _stateTimingsProp.GetArrayElementAtIndex(_selectedState);
                SerializedProperty overrideProp = timingProp.FindPropertyRelative("overrideCancelWindow");
                SerializedProperty startProp = timingProp.FindPropertyRelative("cancelWindowStart");
                SerializedProperty endProp = timingProp.FindPropertyRelative("cancelWindowEnd");

                EditorGUILayout.PropertyField(overrideProp, new GUIContent("Override"));
                using (new EditorGUI.DisabledScope(!overrideProp.boolValue))
                {
                    EditorGUILayout.Slider(startProp, 0f, 1f, new GUIContent("Start"));
                    EditorGUILayout.Slider(endProp, 0f, 1f, new GUIContent("End"));
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Combat Binding", EditorStyles.miniBoldLabel);
            if (!hasBindingSlot)
            {
                EditorGUILayout.HelpBox("Materialize this state to author per-step combat overrides or multi-hit timing.", MessageType.None);
            }
            else
            {
                SerializedProperty bindingProp = _stateCombatBindingsProp.GetArrayElementAtIndex(_selectedState);
                EditorGUILayout.PropertyField(bindingProp.FindPropertyRelative("combatActionOverride"), new GUIContent("Combat Action"));
                EditorGUILayout.PropertyField(bindingProp.FindPropertyRelative("multiHitNormalizedTimes"), true);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Transitions", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(
                $"Light: {CountOutgoingTransitions(_selectedState, GeisComboInputType.Light)}  |  " +
                $"Heavy: {CountOutgoingTransitions(_selectedState, GeisComboInputType.Heavy)}\n" +
                "Drag from an unused L/H output port to a matching input port on another state. Right click an edge in the graph to delete it.",
                MessageType.None);
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            _previewTarget = (GameObject)EditorGUILayout.ObjectField("Preview Target", _previewTarget, typeof(GameObject), true);
            _loopPreview = EditorGUILayout.Toggle("Loop Playback", _loopPreview);

            DrawPreviewCameraControls();

            EditorGUILayout.LabelField("Build Input Sequence", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Light"))
            {
                _previewInputs.Add(GeisComboInputType.Light);
                RebuildPreviewPath();
            }

            if (GUILayout.Button("Heavy"))
            {
                _previewInputs.Add(GeisComboInputType.Heavy);
                RebuildPreviewPath();
            }

            if (GUILayout.Button("Back", GUILayout.Width(50f)))
            {
                if (_previewInputs.Count > 0)
                {
                    _previewInputs.RemoveAt(_previewInputs.Count - 1);
                    RebuildPreviewPath();
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Width(50f)))
            {
                _previewInputs.Clear();
                RebuildPreviewPath();
            }
            EditorGUILayout.EndHorizontal();

            string inputText = _previewInputs.Count == 0
                ? "(none)"
                : string.Join("  ", _previewInputs.Select(input => input == GeisComboInputType.Light ? "L" : "H"));
            EditorGUILayout.LabelField("Inputs", inputText, EditorStyles.helpBox);

            if (!string.IsNullOrEmpty(_previewMessage))
                EditorGUILayout.HelpBox(_previewMessage, MessageType.None);

            if (_previewStates.Count > 0)
            {
                EditorGUILayout.LabelField("Resolved Path", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < _previewStates.Count; i++)
                {
                    int state = _previewStates[i];
                    GUIStyle buttonStyle = i == _previewSelectedStep ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                    if (GUILayout.Button($"S{state}", buttonStyle))
                    {
                        _previewSelectedStep = i;
                        _selectedState = state;
                        SamplePreviewState(normalizedTime: _previewNormalizedTime);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Sample Selected State"))
            {
                _previewSelectedStep = Mathf.Max(0, _previewStates.FindIndex(state => state == _selectedState));
                _previewTimeDrivenByScrub = true;
                SampleState(_selectedState, _previewNormalizedTime);
            }

            using (new EditorGUI.DisabledScope(_previewStates.Count == 0 || _previewTarget == null))
            {
                if (!_playingPreview)
                {
                    if (GUILayout.Button("Play Combo"))
                        StartPreviewPlayback();
                }
                else if (GUILayout.Button("Stop Combo"))
                {
                    StopPreview(clearSampling: true);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            _previewNormalizedTime = EditorGUILayout.Slider("Normalized Time", _previewNormalizedTime, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                _previewTimeDrivenByScrub = true;
                SamplePreviewState(_previewNormalizedTime);
            }

            _loopIdlePreview = EditorGUILayout.ToggleLeft(
                new GUIContent("Loop animation when not scrubbing"),
                _loopIdlePreview,
                EditorStyles.miniLabel);

            if (_previewTarget == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a scene object with the rig you want to sample. The combo graph still works without a preview target.",
                    MessageType.Warning);
            }
        }

        private void DrawPreviewCameraControls()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Preview Camera", EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();
            _nodePreviewYaw = EditorGUILayout.Slider("Yaw", _nodePreviewYaw, 0f, 360f);
            _nodePreviewPitch = EditorGUILayout.Slider("Pitch", _nodePreviewPitch, -30f, 45f);
            _nodePreviewDistanceMultiplier = EditorGUILayout.Slider("Distance", _nodePreviewDistanceMultiplier, 1f, 6f);
            _nodePreviewHeightBias = EditorGUILayout.Slider("Height Bias", _nodePreviewHeightBias, -0.5f, 1f);
            _nodePreviewFieldOfView = EditorGUILayout.Slider("FOV", _nodePreviewFieldOfView, 15f, 60f);
            if (EditorGUI.EndChangeCheck())
                Repaint();

            if (GUILayout.Button("Reset Camera"))
                ResetNodePreviewCameraSettings();
        }

        private void ResetNodePreviewCameraSettings()
        {
            _nodePreviewYaw = 160f;
            _nodePreviewPitch = 12f;
            _nodePreviewDistanceMultiplier = 2.8f;
            _nodePreviewHeightBias = 0.25f;
            _nodePreviewFieldOfView = 30f;
            Repaint();
        }

        private void ProcessCanvasEvents(Event evt, Rect canvasRect)
        {
            if (!canvasRect.Contains(evt.mousePosition))
                return;

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (evt.button == 0)
                    {
                        if (TryGetPortAtScreenPosition(evt.mousePosition, canvasRect, PortDirection.Output, out PortHit outputPort)
                            && !HasOutgoingTransition(outputPort.State, outputPort.InputType))
                        {
                            _selectedState = outputPort.State;
                            _creatingTransition = true;
                            _transitionFromState = outputPort.State;
                            _transitionInputType = outputPort.InputType;
                            _draggingNodeState = -1;
                            evt.Use();
                            Repaint();
                            break;
                        }

                        int clickedState = GetStateAtMouse(evt.mousePosition, canvasRect);
                        if (clickedState >= 0)
                        {
                            _selectedState = clickedState;
                            _draggingNodeState = clickedState;
                            _lastDragWorldPosition = ScreenToWorld(evt.mousePosition, canvasRect);
                            evt.Use();
                            Repaint();
                        }
                    }
                    else if (evt.button == 2)
                    {
                        _draggingCanvas = true;
                        evt.Use();
                    }
                    break;

                case EventType.ContextClick:
                    if (TryShowNodeOrTransitionContextMenu(evt.mousePosition, canvasRect))
                    {
                        evt.Use();
                        Repaint();
                    }
                    break;

                case EventType.MouseUp:
                    if (evt.button == 0 && _creatingTransition)
                    {
                        TryCompleteTransitionConnection(evt.mousePosition, canvasRect);
                        evt.Use();
                    }
                    else if (evt.button == 0 && _draggingNodeState >= 0)
                    {
                        _draggingNodeState = -1;
                        SaveNodePositions();
                        evt.Use();
                    }
                    else if (evt.button == 2)
                    {
                        _draggingCanvas = false;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_creatingTransition && evt.button == 0)
                    {
                        evt.Use();
                        Repaint();
                    }
                    else if (_draggingNodeState >= 0 && evt.button == 0)
                    {
                        Vector2 worldPosition = ScreenToWorld(evt.mousePosition, canvasRect);
                        Vector2 delta = worldPosition - _lastDragWorldPosition;
                        MoveNode(_draggingNodeState, delta);
                        _lastDragWorldPosition = worldPosition;
                        evt.Use();
                        Repaint();
                    }
                    else if (_draggingCanvas && evt.button == 2)
                    {
                        _canvasPan += evt.delta / Mathf.Max(_zoom, 0.01f);
                        evt.Use();
                        Repaint();
                    }
                    break;

                case EventType.ScrollWheel:
                    float oldZoom = _zoom;
                    Vector2 mouseWorld = ScreenToWorld(evt.mousePosition, canvasRect);
                    _zoom = Mathf.Clamp(_zoom * (1f - evt.delta.y * 0.05f), MinZoom, MaxZoom);
                    if (Math.Abs(_zoom - oldZoom) > 0.0001f)
                    {
                        Vector2 localMouse = evt.mousePosition - canvasRect.position;
                        _canvasPan = (localMouse / _zoom) - mouseWorld;
                        evt.Use();
                        Repaint();
                    }
                    break;
            }
        }

        private int GetStateAtMouse(Vector2 mouseScreenPosition, Rect canvasRect)
        {
            Vector2 mouseWorld = ScreenToWorld(mouseScreenPosition, canvasRect);
            Dictionary<int, Vector2> layout = BuildLayout(GetAllStates());
            foreach (KeyValuePair<int, Vector2> pair in layout)
            {
                Rect worldRect = new Rect(pair.Value.x, pair.Value.y, NodeWidth, NodeHeight);
                if (worldRect.Contains(mouseWorld))
                    return pair.Key;
            }

            return -1;
        }

        private List<int> GetAllStates()
        {
            int maxState = Mathf.Max(_clipsProp != null ? _clipsProp.arraySize - 1 : -1, 0);

            if (_stateCombatBindingsProp != null)
                maxState = Mathf.Max(maxState, _stateCombatBindingsProp.arraySize - 1);
            if (_stateTimingsProp != null)
                maxState = Mathf.Max(maxState, _stateTimingsProp.arraySize - 1);

            foreach (ComboTransitionView transition in GetTransitions())
                maxState = Mathf.Max(maxState, Mathf.Max(transition.FromState, transition.ToState));

            return Enumerable.Range(0, Mathf.Max(maxState + 1, 1)).ToList();
        }

        private Dictionary<int, Vector2> BuildLayout(List<int> states)
        {
            Dictionary<int, int> depths = ComputeDepths(states);
            Dictionary<int, Vector2> layout = new Dictionary<int, Vector2>();
            foreach (IGrouping<int, int> group in states.GroupBy(state => depths[state]).OrderBy(group => group.Key))
            {
                int row = 0;
                foreach (int state in group.OrderBy(value => value))
                {
                    layout[state] = new Vector2(group.Key * HorizontalSpacing, row * VerticalSpacing);
                    row++;
                }
            }

            ApplyPositionOverrides(layout);
            return layout;
        }

        private void ApplyPositionOverrides(Dictionary<int, Vector2> layout)
        {
            if (_nodePositionOverrides == null || _nodePositionOverrides.Count == 0)
                return;

            for (int i = _nodePositionOverrides.Count - 1; i >= 0; i--)
            {
                NodePositionOverride entry = _nodePositionOverrides[i];
                if (!layout.ContainsKey(entry.State))
                {
                    _nodePositionOverrides.RemoveAt(i);
                    continue;
                }

                layout[entry.State] = entry.Position;
            }
        }

        private Dictionary<int, int> ComputeDepths(List<int> states)
        {
            Dictionary<int, int> depths = states.ToDictionary(state => state, _ => -1);
            Queue<int> queue = new Queue<int>();
            depths[0] = 0;
            queue.Enqueue(0);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (ComboTransitionView transition in GetOutgoingTransitions(current))
                {
                    if (!depths.ContainsKey(transition.ToState))
                        continue;

                    int proposedDepth = depths[current] + 1;
                    if (depths[transition.ToState] == -1 || proposedDepth < depths[transition.ToState])
                    {
                        depths[transition.ToState] = proposedDepth;
                        queue.Enqueue(transition.ToState);
                    }
                }
            }

            int unreachableDepth = depths.Values.Where(value => value >= 0).DefaultIfEmpty(0).Max() + 1;
            foreach (int state in states.Where(state => depths[state] < 0))
                depths[state] = unreachableDepth;

            return depths;
        }

        private List<ComboTransitionView> GetTransitions()
        {
            List<ComboTransitionView> transitions = new List<ComboTransitionView>();
            if (_transitionsProp == null)
                return transitions;

            for (int i = 0; i < _transitionsProp.arraySize; i++)
            {
                SerializedProperty element = _transitionsProp.GetArrayElementAtIndex(i);
                transitions.Add(new ComboTransitionView
                {
                    PropertyIndex = i,
                    FromState = element.FindPropertyRelative("fromState").intValue,
                    ToState = element.FindPropertyRelative("toState").intValue,
                    InputType = (GeisComboInputType)element.FindPropertyRelative("inputType").enumValueIndex
                });
            }

            return transitions;
        }

        private List<ComboTransitionView> GetOutgoingTransitions(int state)
        {
            return GetTransitions().Where(transition => transition.FromState == state).ToList();
        }

        private bool TryGetNextStateSerialized(int currentState, GeisComboInputType inputType, out int nextState)
        {
            foreach (ComboTransitionView transition in GetTransitions())
            {
                if (transition.FromState == currentState && transition.InputType == inputType)
                {
                    nextState = transition.ToState;
                    return true;
                }
            }

            nextState = 0;
            return false;
        }

        private AnimationClip GetClipForStateSerialized(int state)
        {
            if (_clipsProp != null && state >= 0 && state < _clipsProp.arraySize)
            {
                AnimationClip clip = _clipsProp.GetArrayElementAtIndex(state).objectReferenceValue as AnimationClip;
                if (clip != null)
                    return clip;
            }

            return _fallbackClipProp != null ? _fallbackClipProp.objectReferenceValue as AnimationClip : null;
        }

        private void GetCancelWindowSerialized(int state, out float start, out float end)
        {
            start = _cancelWindowStartProp != null ? Mathf.Clamp01(_cancelWindowStartProp.floatValue) : 0.5f;
            end = _cancelWindowEndProp != null ? Mathf.Clamp01(_cancelWindowEndProp.floatValue) : 0.7f;

            if (_stateTimingsProp != null && state >= 0 && state < _stateTimingsProp.arraySize)
            {
                SerializedProperty timing = _stateTimingsProp.GetArrayElementAtIndex(state);
                SerializedProperty overrideProp = timing.FindPropertyRelative("overrideCancelWindow");
                if (overrideProp.boolValue)
                {
                    start = Mathf.Clamp01(timing.FindPropertyRelative("cancelWindowStart").floatValue);
                    end = Mathf.Clamp01(timing.FindPropertyRelative("cancelWindowEnd").floatValue);
                }
            }

            if (end < start)
                end = start;
        }

        private Rect GetNodeScreenRect(Rect canvasRect, Vector2 worldPosition)
        {
            Vector2 screenPosition = WorldToScreen(worldPosition, canvasRect);
            return new Rect(screenPosition.x, screenPosition.y, NodeWidth * _zoom, NodeHeight * _zoom);
        }

        private void DrawNodePorts(Rect nodeRect)
        {
            DrawPort(GetInputPortRect(nodeRect, GeisComboInputType.Light), GeisComboInputType.Light, true);
            DrawPort(GetInputPortRect(nodeRect, GeisComboInputType.Heavy), GeisComboInputType.Heavy, true);
            DrawPort(GetOutputPortRect(nodeRect, GeisComboInputType.Light), GeisComboInputType.Light, false);
            DrawPort(GetOutputPortRect(nodeRect, GeisComboInputType.Heavy), GeisComboInputType.Heavy, false);
        }

        private void DrawPort(Rect rect, GeisComboInputType inputType, bool isInput)
        {
            Color color = inputType == GeisComboInputType.Light
                ? new Color(0.53f, 0.82f, 1f)
                : new Color(1f, 0.63f, 0.4f);
            EditorGUI.DrawRect(rect, color);
            GUI.Box(rect, GUIContent.none);

            GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.black },
                fontSize = Mathf.RoundToInt(9f * Mathf.Clamp(_zoom, 0.85f, 1.15f))
            };

            GUI.Label(rect, GetInputTypeLabel(inputType), style);

            if (_zoom < 0.8f)
                return;

            Rect textRect = isInput
                ? new Rect(rect.xMax + 4f, rect.y - 1f, 18f, rect.height + 2f)
                : new Rect(rect.x - 22f, rect.y - 1f, 18f, rect.height + 2f);
            GUI.Label(textRect, GetInputTypeLabel(inputType), EditorStyles.miniLabel);
        }

        private Rect GetInputPortRect(Rect nodeRect, GeisComboInputType inputType)
        {
            float diameter = GetPortDiameter();
            Vector3 center = GetInputPortPosition(nodeRect, inputType);
            return new Rect(center.x - diameter * 0.5f, center.y - diameter * 0.5f, diameter, diameter);
        }

        private void DrawPendingConnection(Rect canvasRect, Dictionary<int, Vector2> layout)
        {
            if (_transitionFromState < 0 || !layout.TryGetValue(_transitionFromState, out Vector2 fromPosition))
                return;

            Rect fromRect = GetNodeScreenRect(canvasRect, fromPosition);
            Vector3 start = GetOutputPortPosition(fromRect, _transitionInputType);
            Vector3 end = Event.current.mousePosition;

            if (TryGetPortAtScreenPosition(Event.current.mousePosition, canvasRect, PortDirection.Input, out PortHit inputPort)
                && inputPort.InputType == _transitionInputType
                && layout.TryGetValue(inputPort.State, out Vector2 toPosition))
            {
                Rect toRect = GetNodeScreenRect(canvasRect, toPosition);
                end = GetInputPortPosition(toRect, inputPort.InputType);
            }

            Color lineColor = _transitionInputType == GeisComboInputType.Light
                ? new Color(0.53f, 0.82f, 1f)
                : new Color(1f, 0.63f, 0.4f);

            Handles.BeginGUI();
            Color previousColor = Handles.color;
            Handles.color = lineColor;
            Handles.DrawBezier(
                start,
                end,
                start + Vector3.right * 75f,
                end + Vector3.left * 75f,
                lineColor,
                null,
                3f);
            Handles.color = previousColor;
            Handles.EndGUI();
        }

        private Rect GetOutputPortRect(Rect nodeRect, GeisComboInputType inputType)
        {
            float diameter = GetPortDiameter();
            Vector3 center = GetOutputPortPosition(nodeRect, inputType);
            return new Rect(center.x - diameter * 0.5f, center.y - diameter * 0.5f, diameter, diameter);
        }

        private Vector3 GetInputPortPosition(Rect nodeRect, GeisComboInputType inputType)
        {
            return new Vector3(nodeRect.xMin, GetPortY(nodeRect, inputType), 0f);
        }

        private Vector3 GetOutputPortPosition(Rect nodeRect, GeisComboInputType inputType)
        {
            return new Vector3(nodeRect.xMax, GetPortY(nodeRect, inputType), 0f);
        }

        private float GetPortY(Rect nodeRect, GeisComboInputType inputType)
        {
            float normalizedY = inputType == GeisComboInputType.Light ? 0.4f : 0.72f;
            return nodeRect.yMin + nodeRect.height * normalizedY;
        }

        private float GetPortDiameter()
        {
            return Mathf.Max(PortRadius * 2f * Mathf.Clamp(_zoom, 0.8f, 1.15f), 14f);
        }

        private static string GetInputTypeLabel(GeisComboInputType inputType)
        {
            return inputType == GeisComboInputType.Light ? "L" : "H";
        }

        private int CountOutgoingTransitions(int state, GeisComboInputType inputType)
        {
            return GetOutgoingTransitions(state).Count(transition => transition.InputType == inputType);
        }

        private bool HasOutgoingTransition(int state, GeisComboInputType inputType)
        {
            return CountOutgoingTransitions(state, inputType) > 0;
        }

        private bool TryShowTransitionContextMenu(Vector2 mousePosition, Rect canvasRect)
        {
            if (!TryGetTransitionAtScreenPosition(mousePosition, canvasRect, out ComboTransitionView transition))
                return false;

            GenericMenu menu = new GenericMenu();
            int propertyIndex = transition.PropertyIndex;
            string label = $"{GetInputTypeLabel(transition.InputType)}: {transition.FromState} -> {transition.ToState}";
            menu.AddItem(new GUIContent($"Delete Transition/{label}"), false, () => DeleteTransitionAtIndex(propertyIndex));
            menu.ShowAsContext();
            return true;
        }

        private bool TryShowNodeOrTransitionContextMenu(Vector2 mousePosition, Rect canvasRect)
        {
            int clickedState = GetStateAtMouse(mousePosition, canvasRect);
            if (clickedState >= 0)
            {
                ShowNodeContextMenu(clickedState);
                return true;
            }

            return TryShowTransitionContextMenu(mousePosition, canvasRect);
        }

        private void ShowNodeContextMenu(int state)
        {
            GenericMenu menu = new GenericMenu();
            _selectedState = state;
            menu.AddDisabledItem(new GUIContent($"State {state}"));
            menu.AddSeparator(string.Empty);

            if (state == 0)
            {
                menu.AddDisabledItem(new GUIContent("Delete State (entry state)"));
            }
            else
            {
                menu.AddItem(new GUIContent($"Delete State/State {state}"), false, () => DeleteState(state));
            }

            menu.ShowAsContext();
        }

        private bool TryGetPortAtScreenPosition(Vector2 mousePosition, Rect canvasRect, PortDirection direction, out PortHit hit)
        {
            hit = default;
            Dictionary<int, Vector2> layout = BuildLayout(GetAllStates());
            foreach (KeyValuePair<int, Vector2> pair in layout)
            {
                Rect nodeRect = GetNodeScreenRect(canvasRect, pair.Value);
                foreach (GeisComboInputType inputType in Enum.GetValues(typeof(GeisComboInputType)))
                {
                    Rect portRect = direction == PortDirection.Input
                        ? GetInputPortRect(nodeRect, inputType)
                        : GetOutputPortRect(nodeRect, inputType);
                    if (!portRect.Contains(mousePosition))
                        continue;

                    hit = new PortHit
                    {
                        State = pair.Key,
                        InputType = inputType,
                        Direction = direction
                    };
                    return true;
                }
            }

            return false;
        }

        private void TryCompleteTransitionConnection(Vector2 mousePosition, Rect canvasRect)
        {
            if (TryGetPortAtScreenPosition(mousePosition, canvasRect, PortDirection.Input, out PortHit inputPort)
                && inputPort.InputType == _transitionInputType
                && _transitionFromState >= 0)
            {
                AddTransition(_transitionFromState, _transitionInputType, inputPort.State);
                _selectedState = inputPort.State;
            }

            CancelTransitionConnection();
        }

        private void CancelTransitionConnection()
        {
            _creatingTransition = false;
            _transitionFromState = -1;
            _draggingNodeState = -1;
        }

        private bool TryGetTransitionAtScreenPosition(Vector2 mousePosition, Rect canvasRect, out ComboTransitionView transitionHit)
        {
            transitionHit = default;
            Dictionary<int, Vector2> layout = BuildLayout(GetAllStates());
            float bestDistance = float.MaxValue;
            bool found = false;

            foreach (ComboTransitionView transition in GetTransitions())
            {
                if (!layout.TryGetValue(transition.FromState, out Vector2 fromPosition) ||
                    !layout.TryGetValue(transition.ToState, out Vector2 toPosition))
                {
                    continue;
                }

                Rect fromRect = GetNodeScreenRect(canvasRect, fromPosition);
                Rect toRect = GetNodeScreenRect(canvasRect, toPosition);
                Vector3 start = GetOutputPortPosition(fromRect, transition.InputType);
                Vector3 end = GetInputPortPosition(toRect, transition.InputType);
                Vector3 startTangent = start + Vector3.right * 75f;
                Vector3 endTangent = end + Vector3.left * 75f;
                float distance = HandleUtility.DistancePointBezier(mousePosition, start, end, startTangent, endTangent);
                if (distance <= 10f && distance < bestDistance)
                {
                    bestDistance = distance;
                    transitionHit = transition;
                    found = true;
                }
            }

            return found;
        }

        private Vector2 WorldToScreen(Vector2 worldPosition, Rect canvasRect)
        {
            return canvasRect.position + (worldPosition + _canvasPan) * _zoom;
        }

        private Vector2 ScreenToWorld(Vector2 screenPosition, Rect canvasRect)
        {
            return ((screenPosition - canvasRect.position) / Mathf.Max(_zoom, 0.01f)) - _canvasPan;
        }

        private void LoadComboData(GeisComboData comboData)
        {
            StopPreview(clearSampling: true);
            _comboData = comboData;
            _draggingNodeState = -1;
            CancelTransitionConnection();
            if (_comboData == null)
            {
                _serializedCombo = null;
                _nodePositionOverrides.Clear();
                Repaint();
                return;
            }

            _serializedCombo = new SerializedObject(_comboData);
            _transitionsProp = _serializedCombo.FindProperty("transitions");
            _clipsProp = _serializedCombo.FindProperty("clips");
            _fallbackClipProp = _serializedCombo.FindProperty("fallbackClip");
            _stateCombatBindingsProp = _serializedCombo.FindProperty("stateCombatBindings");
            _cancelWindowStartProp = _serializedCombo.FindProperty("cancelWindowStart");
            _cancelWindowEndProp = _serializedCombo.FindProperty("cancelWindowEnd");
            _stateTimingsProp = _serializedCombo.FindProperty("stateTimings");

            _selectedState = Mathf.Clamp(_selectedState, 0, Mathf.Max(0, GetAllStates().Count - 1));
            _previewInputs.Clear();
            _previewStates.Clear();
            _previewSelectedStep = 0;
            _previewNormalizedTime = 0f;
            LoadNodePositions();
            RebuildPreviewPath();
            FrameGraph();
            Repaint();
        }

        private void TryLoadSelection()
        {
            if (Selection.activeObject is GeisComboData comboData)
                LoadComboData(comboData);
        }

        private void AppendState()
        {
            int nextState = GetAllStates().Count;
            EnsureStateExists(nextState);
            _selectedState = nextState;
            RebuildPreviewPath();
        }

        private void EnsureStateExists(int state)
        {
            if (_comboData == null || state < 0)
                return;

            Undo.RecordObject(_comboData, "Expand Combo State Arrays");
            _serializedCombo.Update();

            if (_clipsProp.arraySize <= state)
                _clipsProp.arraySize = state + 1;

            EnsureTimingElement(state);
            EnsureCombatBindingElement(state);
            _serializedCombo.ApplyModifiedProperties();
            EditorUtility.SetDirty(_comboData);
        }

        private SerializedProperty EnsureTimingElement(int state)
        {
            if (_stateTimingsProp.arraySize <= state)
            {
                int previousSize = _stateTimingsProp.arraySize;
                _stateTimingsProp.arraySize = state + 1;
                for (int i = previousSize; i <= state; i++)
                {
                    SerializedProperty timing = _stateTimingsProp.GetArrayElementAtIndex(i);
                    timing.FindPropertyRelative("overrideCancelWindow").boolValue = false;
                    timing.FindPropertyRelative("cancelWindowStart").floatValue = _cancelWindowStartProp.floatValue;
                    timing.FindPropertyRelative("cancelWindowEnd").floatValue = _cancelWindowEndProp.floatValue;
                }
            }

            return _stateTimingsProp.GetArrayElementAtIndex(state);
        }

        private SerializedProperty EnsureCombatBindingElement(int state)
        {
            if (_stateCombatBindingsProp.arraySize <= state)
            {
                int previousSize = _stateCombatBindingsProp.arraySize;
                _stateCombatBindingsProp.arraySize = state + 1;
                for (int i = previousSize; i <= state; i++)
                {
                    SerializedProperty binding = _stateCombatBindingsProp.GetArrayElementAtIndex(i);
                    binding.FindPropertyRelative("combatActionOverride").objectReferenceValue = null;
                    binding.FindPropertyRelative("multiHitNormalizedTimes").arraySize = 0;
                }
            }

            return _stateCombatBindingsProp.GetArrayElementAtIndex(state);
        }

        private void AddTransition(int fromState, GeisComboInputType inputType, int toState)
        {
            Undo.RecordObject(_comboData, "Add Combo Transition");
            _serializedCombo.Update();

            int insertIndex = _transitionsProp.arraySize;
            _transitionsProp.InsertArrayElementAtIndex(insertIndex);
            SerializedProperty element = _transitionsProp.GetArrayElementAtIndex(insertIndex);
            element.FindPropertyRelative("fromState").intValue = fromState;
            element.FindPropertyRelative("inputType").enumValueIndex = (int)inputType;
            element.FindPropertyRelative("toState").intValue = Mathf.Max(toState, 0);

            _serializedCombo.ApplyModifiedProperties();
            EditorUtility.SetDirty(_comboData);
            RebuildPreviewPath();
        }

        private void DeleteTransitionAtIndex(int propertyIndex)
        {
            Undo.RecordObject(_comboData, "Delete Combo Transition");
            _serializedCombo.Update();
            _transitionsProp.DeleteArrayElementAtIndex(propertyIndex);
            _serializedCombo.ApplyModifiedProperties();
            EditorUtility.SetDirty(_comboData);
            RebuildPreviewPath();
        }

        private void DeleteState(int stateToDelete)
        {
            if (_comboData == null || stateToDelete <= 0)
                return;

            StopPreview(clearSampling: true);
            Undo.RecordObject(_comboData, "Delete Combo State");
            _serializedCombo.Update();

            DeleteTransitionsForState(stateToDelete);
            ReindexTransitionsAfterStateDeletion(stateToDelete);

            RemoveArrayElementCompletely(_clipsProp, stateToDelete);
            RemoveArrayElementCompletely(_stateTimingsProp, stateToDelete);
            RemoveArrayElementCompletely(_stateCombatBindingsProp, stateToDelete);

            ReindexNodeOverridesAfterStateDeletion(stateToDelete);

            _serializedCombo.ApplyModifiedProperties();
            EditorUtility.SetDirty(_comboData);

            _selectedState = Mathf.Clamp(_selectedState > stateToDelete ? _selectedState - 1 : _selectedState, 0, Mathf.Max(0, GetAllStates().Count - 1));
            SaveNodePositions();
            RebuildPreviewPath();
            Repaint();
        }

        private void DeleteTransitionsForState(int stateToDelete)
        {
            for (int i = _transitionsProp.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty element = _transitionsProp.GetArrayElementAtIndex(i);
                int fromState = element.FindPropertyRelative("fromState").intValue;
                int toState = element.FindPropertyRelative("toState").intValue;
                if (fromState == stateToDelete || toState == stateToDelete)
                    _transitionsProp.DeleteArrayElementAtIndex(i);
            }
        }

        private void ReindexTransitionsAfterStateDeletion(int stateToDelete)
        {
            for (int i = 0; i < _transitionsProp.arraySize; i++)
            {
                SerializedProperty element = _transitionsProp.GetArrayElementAtIndex(i);
                SerializedProperty fromStateProp = element.FindPropertyRelative("fromState");
                SerializedProperty toStateProp = element.FindPropertyRelative("toState");

                if (fromStateProp.intValue > stateToDelete)
                    fromStateProp.intValue -= 1;
                if (toStateProp.intValue > stateToDelete)
                    toStateProp.intValue -= 1;
            }
        }

        private void ReindexNodeOverridesAfterStateDeletion(int stateToDelete)
        {
            for (int i = _nodePositionOverrides.Count - 1; i >= 0; i--)
            {
                NodePositionOverride entry = _nodePositionOverrides[i];
                if (entry.State == stateToDelete)
                {
                    _nodePositionOverrides.RemoveAt(i);
                    continue;
                }

                if (entry.State > stateToDelete)
                    entry.State -= 1;
            }
        }

        private static void RemoveArrayElementCompletely(SerializedProperty arrayProperty, int index)
        {
            if (arrayProperty == null || index < 0 || index >= arrayProperty.arraySize)
                return;

            int startingSize = arrayProperty.arraySize;
            arrayProperty.DeleteArrayElementAtIndex(index);
            if (arrayProperty.arraySize == startingSize)
                arrayProperty.DeleteArrayElementAtIndex(index);
        }

        private void RebuildPreviewPath()
        {
            _previewStates.Clear();
            if (_comboData == null)
                return;

            _previewStates.Add(0);
            int currentState = 0;
            for (int i = 0; i < _previewInputs.Count; i++)
            {
                GeisComboInputType input = _previewInputs[i];
                if (!TryGetNextStateSerialized(currentState, input, out int nextState))
                {
                    _previewMessage = $"No {input} transition from state {currentState}.";
                    _previewSelectedStep = Mathf.Clamp(_previewSelectedStep, 0, Mathf.Max(0, _previewStates.Count - 1));
                    return;
                }

                currentState = nextState;
                _previewStates.Add(currentState);
            }

            _previewMessage = _previewInputs.Count == 0
                ? "Build a preview path with Light/Heavy inputs."
                : $"Resolved {_previewStates.Count} state(s). Click a step to sample it or play the full sequence.";
            _previewSelectedStep = Mathf.Clamp(_previewSelectedStep, 0, Mathf.Max(0, _previewStates.Count - 1));
            Repaint();
        }

        private void StartPreviewPlayback()
        {
            if (_previewTarget == null || _previewStates.Count == 0)
                return;

            _playbackStates.Clear();
            _playbackStates.AddRange(_previewStates);
            _playbackStepIndex = 0;
            _playbackStepStartedAt = EditorApplication.timeSinceStartup;
            _playingPreview = true;
            _previewTimeDrivenByScrub = false;
            _previewSelectedStep = 0;
            SampleState(_playbackStates[0], 0f);
        }

        private void StopPreview(bool clearSampling)
        {
            _playingPreview = false;
            _playbackStates.Clear();
            if (clearSampling && AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
        }

        private void OnEditorUpdate()
        {
            if (!_playingPreview)
            {
                if (ShouldAnimateSelectedNodePreview())
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                    Repaint();
                }
                return;
            }

            if (_previewTarget == null || _playbackStates.Count == 0)
            {
                StopPreview(clearSampling: true);
                return;
            }

            int state = _playbackStates[Mathf.Clamp(_playbackStepIndex, 0, _playbackStates.Count - 1)];
            AnimationClip clip = GetClipForStateSerialized(state);
            if (clip == null)
            {
                AdvancePlayback();
                return;
            }

            double elapsed = EditorApplication.timeSinceStartup - _playbackStepStartedAt;
            float duration = Mathf.Max(clip.length, 0.01f);
            float normalized = Mathf.Clamp01((float)(elapsed / duration));
            _previewSelectedStep = _playbackStepIndex;
            _previewNormalizedTime = normalized;
            SampleState(state, normalized);

            float stepAdvanceTime = GetPlaybackAdvanceTime(state, duration);
            if (elapsed >= stepAdvanceTime)
                AdvancePlayback();

            EditorApplication.QueuePlayerLoopUpdate();
            Repaint();
        }

        private bool ShouldAnimateSelectedNodePreview()
        {
            return !_previewTimeDrivenByScrub
                && _loopIdlePreview
                && _previewTarget != null
                && GetClipForStateSerialized(_selectedState) != null;
        }

        private float GetPlaybackAdvanceTime(int state, float clipDuration)
        {
            bool hasNextState = _playbackStepIndex < _playbackStates.Count - 1;
            if (!hasNextState)
                return clipDuration;

            GetCancelWindowSerialized(state, out float cancelWindowStart, out _);
            return Mathf.Clamp(cancelWindowStart * clipDuration, 0.01f, clipDuration);
        }

        private void AdvancePlayback()
        {
            _playbackStepIndex++;
            if (_playbackStepIndex >= _playbackStates.Count)
            {
                if (_loopPreview)
                {
                    _playbackStepIndex = 0;
                }
                else
                {
                    StopPreview(clearSampling: true);
                    return;
                }
            }

            _playbackStepStartedAt = EditorApplication.timeSinceStartup;
        }

        private void SamplePreviewState(float normalizedTime)
        {
            _previewTimeDrivenByScrub = true;

            if (_previewStates.Count == 0)
            {
                SampleState(_selectedState, normalizedTime);
                return;
            }

            int clampedIndex = Mathf.Clamp(_previewSelectedStep, 0, _previewStates.Count - 1);
            SampleState(_previewStates[clampedIndex], normalizedTime);
        }

        private void SampleState(int state, float normalizedTime)
        {
            if (_comboData == null || _previewTarget == null)
                return;

            AnimationClip clip = GetClipForStateSerialized(state);
            if (clip == null)
                return;

            if (!AnimationMode.InAnimationMode())
                AnimationMode.StartAnimationMode();

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(_previewTarget, clip, Mathf.Clamp01(normalizedTime) * clip.length);
            AnimationMode.EndSampling();
            SceneView.RepaintAll();
        }

        private void AutoLayout()
        {
            _nodePositionOverrides.Clear();
            SaveNodePositions();
            _canvasPan = new Vector2(90f, 70f);
            _zoom = 1f;
            Repaint();
        }

        private void FrameGraph()
        {
            List<int> states = GetAllStates();
            Dictionary<int, Vector2> layout = BuildLayout(states);
            if (layout.Count == 0)
                return;

            float maxX = layout.Values.Max(position => position.x) + NodeWidth;
            float maxY = layout.Values.Max(position => position.y) + NodeHeight;
            Rect canvasRect = new Rect(0f, ToolbarHeight, position.width - InspectorWidth, position.height - ToolbarHeight);
            float zoomX = (canvasRect.width - 100f) / Mathf.Max(maxX, 1f);
            float zoomY = (canvasRect.height - 100f) / Mathf.Max(maxY, 1f);
            _zoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY, 1f), MinZoom, MaxZoom);
            _canvasPan = new Vector2(40f, 40f);
            Repaint();
        }

        private void SaveComboAsset()
        {
            if (_comboData == null)
                return;

            _serializedCombo.ApplyModifiedProperties();
            EditorUtility.SetDirty(_comboData);
            AssetDatabase.SaveAssets();
        }

        private bool IsPreviewTransition(int fromState, int toState)
        {
            for (int i = 0; i < _previewStates.Count - 1; i++)
            {
                if (_previewStates[i] == fromState && _previewStates[i + 1] == toState)
                    return true;
            }

            return false;
        }

        private void MoveNode(int state, Vector2 delta)
        {
            NodePositionOverride entry = GetOrCreatePositionOverride(state);
            entry.Position += delta;
        }

        private NodePositionOverride GetOrCreatePositionOverride(int state)
        {
            for (int i = 0; i < _nodePositionOverrides.Count; i++)
            {
                if (_nodePositionOverrides[i].State == state)
                    return _nodePositionOverrides[i];
            }

            Dictionary<int, Vector2> layout = BuildDefaultLayout(GetAllStates());
            Vector2 startPosition = layout.TryGetValue(state, out Vector2 position) ? position : Vector2.zero;
            NodePositionOverride created = new NodePositionOverride
            {
                State = state,
                Position = startPosition
            };
            _nodePositionOverrides.Add(created);
            return created;
        }

        private Dictionary<int, Vector2> BuildDefaultLayout(List<int> states)
        {
            Dictionary<int, int> depths = ComputeDepths(states);
            Dictionary<int, Vector2> layout = new Dictionary<int, Vector2>();
            foreach (IGrouping<int, int> group in states.GroupBy(state => depths[state]).OrderBy(group => group.Key))
            {
                int row = 0;
                foreach (int state in group.OrderBy(value => value))
                {
                    layout[state] = new Vector2(group.Key * HorizontalSpacing, row * VerticalSpacing);
                    row++;
                }
            }

            return layout;
        }

        private void LoadNodePositions()
        {
            _nodePositionOverrides.Clear();
            if (_comboData == null)
                return;

            string json = EditorPrefs.GetString(GetNodePositionPrefsKey(), string.Empty);
            if (string.IsNullOrEmpty(json))
                return;

            NodePositionCollection collection = new NodePositionCollection();
            EditorJsonUtility.FromJsonOverwrite(json, collection);
            if (collection.positions != null)
                _nodePositionOverrides = collection.positions;
        }

        private void SaveNodePositions()
        {
            if (_comboData == null)
                return;

            NodePositionCollection collection = new NodePositionCollection
            {
                positions = _nodePositionOverrides
            };
            string json = EditorJsonUtility.ToJson(collection);
            EditorPrefs.SetString(GetNodePositionPrefsKey(), json);
        }

        private string GetNodePositionPrefsKey()
        {
            string path = AssetDatabase.GetAssetPath(_comboData);
            string guid = string.IsNullOrEmpty(path) ? _comboData.GetInstanceID().ToString() : AssetDatabase.AssetPathToGUID(path);
            return $"Geis.ComboGraph.Layout.{guid}";
        }

        [Serializable]
        private class NodePositionCollection
        {
            public List<NodePositionOverride> positions = new List<NodePositionOverride>();
        }

        [Serializable]
        private class NodePositionOverride
        {
            public int State;
            public Vector2 Position;
        }

        private enum PortDirection
        {
            Input,
            Output
        }

        private struct PortHit
        {
            public int State;
            public GeisComboInputType InputType;
            public PortDirection Direction;
        }

        private struct ComboTransitionView
        {
            public int PropertyIndex;
            public int FromState;
            public int ToState;
            public GeisComboInputType InputType;
        }
    }

    [CustomEditor(typeof(GeisComboData))]
    public class GeisComboDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (GUILayout.Button("Open Combo Graph", GUILayout.Height(32f)))
                GeisComboGraphWindow.Open((GeisComboData)target);
        }
    }

    public static class GeisComboDataAssetHandler
    {
        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceID);
            if (string.IsNullOrEmpty(assetPath))
                return false;

            GeisComboData comboData = AssetDatabase.LoadAssetAtPath<GeisComboData>(assetPath);
            if (comboData == null)
                return false;

            // Delay window creation until after Unity finishes the asset-open event; direct opens can be flaky.
            EditorApplication.delayCall += () =>
            {
                if (comboData == null)
                    return;

                Selection.activeObject = comboData;
                GeisComboGraphWindow.Open(comboData);
            };

            return true;
        }
    }
}
