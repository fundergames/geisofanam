using UnityEditor;
using UnityEngine;

namespace Geis.Puzzles.Editor
{
    /// <summary>
    /// Custom editor for <see cref="PuzzleGroup"/>.
    /// Draws gold arrow-lines to each trigger and green arrow-lines to each output
    /// in the Scene view so designers can see connections at a glance.
    /// Also shows logic mode and solved state as floating labels.
    /// </summary>
    [CustomEditor(typeof(PuzzleGroup))]
    public class PuzzleGroupEditor : UnityEditor.Editor
    {
        static readonly Color TriggerColour = new Color(1f, 0.85f, 0.1f, 0.9f);
        static readonly Color OutputColour  = new Color(0.25f, 0.9f, 0.35f, 0.9f);
        static readonly Color SolvedColour  = new Color(0.1f, 1f, 0.45f, 1f);
        static readonly Color GroupColour   = new Color(1f, 1f, 1f, 0.6f);

        private GUIStyle _labelStyle;
        private GUIStyle _badgeStyle;

        private GUIStyle LabelStyle
        {
            get
            {
                if (_labelStyle == null)
                {
                    _labelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize  = 10,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                    };
                    _labelStyle.normal.textColor = Color.white;
                }
                return _labelStyle;
            }
        }

        private GUIStyle BadgeStyle
        {
            get
            {
                if (_badgeStyle == null)
                {
                    _badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize  = 11,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                    };
                }
                return _badgeStyle;
            }
        }

        private void OnSceneGUI()
        {
            var group = (PuzzleGroup)target;
            if (group == null) return;

            Vector3 groupPos = group.transform.position + Vector3.up * 0.5f;

            serializedObject.Update();
            var triggersProp = serializedObject.FindProperty("triggers");
            var outputsProp  = serializedObject.FindProperty("outputs");
            var logicModeProp = serializedObject.FindProperty("logicMode");

            // ── Triggers → Group ────────────────────────────────────────────────
            for (int i = 0; i < triggersProp.arraySize; i++)
            {
                var obj = triggersProp.GetArrayElementAtIndex(i).objectReferenceValue as Component;
                if (obj == null) continue;

                Vector3 pos = obj.transform.position;
                DrawArrow(pos + Vector3.up * 0.3f, groupPos, TriggerColour);

                string typeName = obj.GetType().Name
                    .Replace("Trigger", "")
                    .Replace("Soul", "☽ ")
                    .Replace("Bow", "⟶ ")
                    .Replace("Sword", "⚔ ")
                    .Replace("Dagger", "⚡ ")
                    .Replace("Pressure", "↓")
                    .Replace("Dual", "⇄ ")
                    .Replace("Echo", "◌ ")
                    .Replace("Alignment", "⟳ ")
                    .Replace("Sequence", "♪ ");

                BadgeStyle.normal.textColor = TriggerColour;
                Handles.Label(pos + Vector3.up * 1.3f, typeName, BadgeStyle);
            }

            // ── Group → Outputs ─────────────────────────────────────────────────
            for (int i = 0; i < outputsProp.arraySize; i++)
            {
                var obj = outputsProp.GetArrayElementAtIndex(i).objectReferenceValue as Component;
                if (obj == null) continue;

                Vector3 pos = obj.transform.position;
                DrawArrow(groupPos, pos + Vector3.up * 0.3f, OutputColour);

                string typeName = obj.GetType().Name
                    .Replace("Output", "")
                    .Replace("Moving", "▷ ")
                    .Replace("Raise", "↑")
                    .Replace("Lower", "↓")
                    .Replace("Barrier", "▦ ")
                    .Replace("Door", "🚪");

                BadgeStyle.normal.textColor = OutputColour;
                Handles.Label(pos + Vector3.up * 1.3f, typeName, BadgeStyle);
            }

            // ── Group label ──────────────────────────────────────────────────────
            string logicMode = logicModeProp != null
                ? ((PuzzleGroup.LogicMode)logicModeProp.enumValueIndex).ToString()
                : "";

            bool isSolved = group.IsSolved;
            string groupLabel = isSolved
                ? $"✓ SOLVED\n[{logicMode}]"
                : $"PuzzleGroup\n[{logicMode}]";

            BadgeStyle.normal.textColor = isSolved ? SolvedColour : GroupColour;
            Handles.Label(groupPos + Vector3.up * 0.8f, groupLabel, BadgeStyle);

            // Dot at group centre
            Handles.color = isSolved ? SolvedColour : GroupColour;
            Handles.DrawSolidDisc(groupPos, Vector3.up, 0.12f);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUI.changed)
                SceneView.RepaintAll();
        }

        // ── Arrow drawing ────────────────────────────────────────────────────────

        private static void DrawArrow(Vector3 from, Vector3 to, Color col)
        {
            Handles.color = col;
            Handles.DrawLine(from, to, 2f);

            // Arrowhead at destination
            Vector3 dir = (to - from).normalized;
            if (dir == Vector3.zero) return;

            float headSize = Mathf.Min(0.35f, Vector3.Distance(from, to) * 0.25f);

            Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
            if (right == Vector3.zero) right = Vector3.Cross(dir, Vector3.forward).normalized;

            Vector3 tip = to;
            Vector3 base1 = tip - dir * headSize + right * headSize * 0.5f;
            Vector3 base2 = tip - dir * headSize - right * headSize * 0.5f;

            Handles.DrawLine(tip, base1, 2f);
            Handles.DrawLine(tip, base2, 2f);
        }
    }
}
