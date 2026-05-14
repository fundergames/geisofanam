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

#if UNITY_EDITOR
using System.Collections.Generic;
using RogueDeal.Combat;
using UnityEditor;
using UnityEngine;

namespace Geis.Enemies.Editor
{
    public class EnemyAiStateWindow : EditorWindow
    {
        private static readonly EnemyBrain.EnemyState[] OrderedStates =
        {
            EnemyBrain.EnemyState.Idle,
            EnemyBrain.EnemyState.Acquire,
            EnemyBrain.EnemyState.Approach,
            EnemyBrain.EnemyState.Strafe,
            EnemyBrain.EnemyState.Telegraph,
            EnemyBrain.EnemyState.Attack,
            EnemyBrain.EnemyState.Recover,
            EnemyBrain.EnemyState.Stagger,
            EnemyBrain.EnemyState.Dead
        };

        private static readonly (EnemyBrain.EnemyState from, EnemyBrain.EnemyState to)[] Transitions =
        {
            (EnemyBrain.EnemyState.Idle, EnemyBrain.EnemyState.Acquire),
            (EnemyBrain.EnemyState.Acquire, EnemyBrain.EnemyState.Approach),
            (EnemyBrain.EnemyState.Approach, EnemyBrain.EnemyState.Strafe),
            (EnemyBrain.EnemyState.Approach, EnemyBrain.EnemyState.Telegraph),
            (EnemyBrain.EnemyState.Strafe, EnemyBrain.EnemyState.Telegraph),
            (EnemyBrain.EnemyState.Telegraph, EnemyBrain.EnemyState.Attack),
            (EnemyBrain.EnemyState.Attack, EnemyBrain.EnemyState.Recover),
            (EnemyBrain.EnemyState.Recover, EnemyBrain.EnemyState.Approach),
            (EnemyBrain.EnemyState.Approach, EnemyBrain.EnemyState.Stagger),
            (EnemyBrain.EnemyState.Strafe, EnemyBrain.EnemyState.Stagger),
            (EnemyBrain.EnemyState.Telegraph, EnemyBrain.EnemyState.Stagger),
            (EnemyBrain.EnemyState.Attack, EnemyBrain.EnemyState.Stagger),
            (EnemyBrain.EnemyState.Stagger, EnemyBrain.EnemyState.Approach),
            (EnemyBrain.EnemyState.Idle, EnemyBrain.EnemyState.Dead),
            (EnemyBrain.EnemyState.Acquire, EnemyBrain.EnemyState.Dead),
            (EnemyBrain.EnemyState.Approach, EnemyBrain.EnemyState.Dead),
            (EnemyBrain.EnemyState.Strafe, EnemyBrain.EnemyState.Dead),
            (EnemyBrain.EnemyState.Telegraph, EnemyBrain.EnemyState.Dead),
            (EnemyBrain.EnemyState.Attack, EnemyBrain.EnemyState.Dead),
            (EnemyBrain.EnemyState.Recover, EnemyBrain.EnemyState.Dead),
            (EnemyBrain.EnemyState.Stagger, EnemyBrain.EnemyState.Dead)
        };

        private readonly Dictionary<EnemyBrain.EnemyState, Rect> _nodeRects = new Dictionary<EnemyBrain.EnemyState, Rect>();

        private EnemyBrain _selectedBrain;
        private Vector2 _scroll;

        [MenuItem("Funder Games/Geis/Tools/Enemies/AI State Visualizer")]
        public static void ShowWindow()
        {
            var window = GetWindow<EnemyAiStateWindow>("Enemy AI State");
            window.minSize = new Vector2(560f, 520f);
            window.Focus();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += HandleSelectionChanged;
            EditorApplication.update += HandleEditorUpdate;
            ResolveSelection();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= HandleSelectionChanged;
            EditorApplication.update -= HandleEditorUpdate;
        }

        private void HandleSelectionChanged()
        {
            ResolveSelection();
            Repaint();
        }

        private void HandleEditorUpdate()
        {
            if (EditorApplication.isPlaying && _selectedBrain != null)
                Repaint();
        }

        private void ResolveSelection()
        {
            GameObject active = Selection.activeGameObject;
            if (active == null)
            {
                _selectedBrain = null;
                return;
            }

            _selectedBrain = active.GetComponent<EnemyBrain>()
                ?? active.GetComponentInParent<EnemyBrain>()
                ?? active.GetComponentInChildren<EnemyBrain>();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_selectedBrain == null)
            {
                EditorGUILayout.HelpBox(
                    "Select an enemy GameObject with `EnemyBrain` to visualize its FSM. The window is read-only and highlights the live state during Play Mode.",
                    MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawSelectionSummary();

            Rect graphRect = GUILayoutUtility.GetRect(position.width - 32f, 320f, GUILayout.ExpandWidth(true));
            DrawGraph(graphRect);

            EditorGUILayout.Space(10f);
            DrawRuntimeDetails();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Enemy AI State Visualizer", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(EditorApplication.isPlaying ? "Play Mode" : "Edit Mode", EditorStyles.miniLabel);
            }
        }

        private void DrawSelectionSummary()
        {
            EnemyCombatant combatant = ResolveComponent<EnemyCombatant>();
            EnemyPerception perception = ResolveComponent<EnemyPerception>();
            EnemyAttackDriver attackDriver = ResolveComponent<EnemyAttackDriver>();

            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Brain", _selectedBrain.name);
                if (combatant != null)
                {
                    string displayName = combatant.Definition != null && !string.IsNullOrWhiteSpace(combatant.Definition.displayName)
                        ? combatant.Definition.displayName
                        : combatant.name;
                    EditorGUILayout.LabelField("Enemy", displayName);
                    EditorGUILayout.LabelField("Defeated", combatant.IsDefeated ? "Yes" : "No");
                }

                EditorGUILayout.LabelField("Current State", _selectedBrain.CurrentState.ToString());
                EditorGUILayout.LabelField("Target", perception != null && perception.CurrentTarget != null ? perception.CurrentTarget.name : "None");
                EditorGUILayout.LabelField("Attack Phase", attackDriver != null ? attackDriver.CurrentPhase.ToString() : "N/A");
            }
        }

        private void DrawGraph(Rect graphRect)
        {
            GUI.Box(graphRect, GUIContent.none);

            Rect inner = new Rect(graphRect.x + 12f, graphRect.y + 12f, graphRect.width - 24f, graphRect.height - 24f);
            ComputeNodeLayout(inner);

            Handles.BeginGUI();
            foreach (var transition in Transitions)
                DrawConnection(transition.from, transition.to, _selectedBrain.CurrentState);
            Handles.EndGUI();

            foreach (EnemyBrain.EnemyState state in OrderedStates)
                DrawNode(state, _selectedBrain.CurrentState);
        }

        private void DrawRuntimeDetails()
        {
            EnemyCombatant combatant = ResolveComponent<EnemyCombatant>();
            EnemyPerception perception = ResolveComponent<EnemyPerception>();
            EnemyAttackDriver attackDriver = ResolveComponent<EnemyAttackDriver>();

            EditorGUILayout.LabelField("Runtime Details", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (combatant != null && combatant.Definition != null)
                {
                    EditorGUILayout.LabelField("Aggro Range", combatant.Definition.perception.aggroRange.ToString("0.00"));
                    EditorGUILayout.LabelField("Preferred Distance", combatant.Definition.GetPreferredCombatDistance().ToString("0.00"));
                }

                if (perception != null)
                {
                    bool hasTarget = perception.CurrentTarget != null;
                    EditorGUILayout.LabelField("Has Target", hasTarget ? "Yes" : "No");
                    EditorGUILayout.LabelField(
                        "Distance To Target",
                        hasTarget ? perception.GetDistanceToCurrentTarget().ToString("0.00") : "N/A");
                    EditorGUILayout.LabelField(
                        "Line Of Sight",
                        hasTarget ? (perception.HasLineOfSightToCurrentTarget() ? "Clear" : "Blocked") : "N/A");
                }

                if (attackDriver != null)
                {
                    EditorGUILayout.LabelField("Driver Busy", attackDriver.IsBusy ? "Yes" : "No");
                    EditorGUILayout.LabelField(
                        "Current Attack",
                        attackDriver.CurrentAttack != null ? attackDriver.CurrentAttack.attackId : "None");
                }

                if (!EditorApplication.isPlaying)
                {
                    EditorGUILayout.HelpBox(
                        "The graph layout is visible in Edit Mode. Active-state highlighting and runtime values update live in Play Mode.",
                        MessageType.None);
                }
            }
        }

        private void ComputeNodeLayout(Rect area)
        {
            _nodeRects.Clear();

            float nodeWidth = 118f;
            float nodeHeight = 46f;
            float x1 = area.x + 18f;
            float x2 = area.x + area.width * 0.34f;
            float x3 = area.x + area.width * 0.62f;
            float x4 = area.x + area.width - nodeWidth - 18f;

            float yTop = area.y + 12f;
            float yMid = area.y + 118f;
            float yLow = area.y + 224f;

            _nodeRects[EnemyBrain.EnemyState.Idle] = new Rect(x1, yTop, nodeWidth, nodeHeight);
            _nodeRects[EnemyBrain.EnemyState.Acquire] = new Rect(x2, yTop, nodeWidth, nodeHeight);
            _nodeRects[EnemyBrain.EnemyState.Approach] = new Rect(x2, yMid, nodeWidth, nodeHeight);
            _nodeRects[EnemyBrain.EnemyState.Strafe] = new Rect(x3, yMid - 58f, nodeWidth, nodeHeight);
            _nodeRects[EnemyBrain.EnemyState.Telegraph] = new Rect(x3, yTop, nodeWidth, nodeHeight);
            _nodeRects[EnemyBrain.EnemyState.Attack] = new Rect(x4, yTop, nodeWidth, nodeHeight);
            _nodeRects[EnemyBrain.EnemyState.Recover] = new Rect(x4, yMid, nodeWidth, nodeHeight);
            _nodeRects[EnemyBrain.EnemyState.Stagger] = new Rect(x3, yLow, nodeWidth, nodeHeight);
            _nodeRects[EnemyBrain.EnemyState.Dead] = new Rect(x4, yLow, nodeWidth, nodeHeight);
        }

        private void DrawConnection(EnemyBrain.EnemyState from, EnemyBrain.EnemyState to, EnemyBrain.EnemyState activeState)
        {
            if (!_nodeRects.TryGetValue(from, out Rect fromRect) || !_nodeRects.TryGetValue(to, out Rect toRect))
                return;

            Vector3 start = GetEdgePoint(fromRect, toRect.center);
            Vector3 end = GetEdgePoint(toRect, fromRect.center);
            Vector3 startTangent = start + Vector3.right * 32f * Mathf.Sign(end.x - start.x);
            Vector3 endTangent = end + Vector3.left * 32f * Mathf.Sign(end.x - start.x);

            bool active = activeState == from || activeState == to;
            Color color = active ? new Color(0.19f, 0.72f, 0.98f, 0.95f) : new Color(0.55f, 0.55f, 0.55f, 0.75f);
            Handles.DrawBezier(start, end, startTangent, endTangent, color, null, active ? 3f : 1.75f);
        }

        private static Vector3 GetEdgePoint(Rect rect, Vector2 toward)
        {
            Vector2 center = rect.center;
            Vector2 delta = toward - center;

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return new Vector3(delta.x >= 0f ? rect.xMax : rect.xMin, center.y, 0f);

            return new Vector3(center.x, delta.y >= 0f ? rect.yMax : rect.yMin, 0f);
        }

        private void DrawNode(EnemyBrain.EnemyState state, EnemyBrain.EnemyState activeState)
        {
            Rect rect = _nodeRects[state];
            bool active = state == activeState;

            Color fill = active ? new Color(0.14f, 0.44f, 0.24f, 1f) : new Color(0.18f, 0.18f, 0.18f, 1f);
            Color border = active ? new Color(0.23f, 0.86f, 0.40f, 1f) : new Color(0.36f, 0.36f, 0.36f, 1f);

            EditorGUI.DrawRect(rect, fill);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), border);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), border);

            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = active ? Color.white : new Color(0.9f, 0.9f, 0.9f, 1f) }
            };

            GUI.Label(rect, state.ToString(), labelStyle);
        }

        private T ResolveComponent<T>() where T : Component
        {
            if (_selectedBrain == null)
                return null;

            return _selectedBrain.GetComponent<T>()
                ?? _selectedBrain.GetComponentInParent<T>()
                ?? _selectedBrain.GetComponentInChildren<T>();
        }
    }
}
#endif
