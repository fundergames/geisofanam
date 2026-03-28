using Geis.Puzzles;
using UnityEditor;
using UnityEngine;

namespace Geis.Puzzles.Editor
{
    /// <summary>
    /// Always-visible realm rings and labels for puzzle elements (selected or not).
    /// </summary>
    public static class PuzzleElementRealmGizmos
    {
        private static GUIStyle _labelStyle;

        private static GUIStyle LabelStyle
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
                }
                return _labelStyle;
            }
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Active | GizmoType.InSelectionHierarchy)]
        static void DrawRealmGizmo(PuzzleElementBase elem, GizmoType gizmoType)
        {
            if (elem == null) return;

            var realm = elem.RealmMode;
            Color col = PuzzleRealmColors.SceneGizmo(realm);
            string label = PuzzleRealmColors.LabelForMode(realm);

            Vector3 pos = elem.transform.position;

            Handles.color = new Color(col.r, col.g, col.b, 0.25f);
            Handles.DrawSolidDisc(pos, Vector3.up, 0.7f);
            Handles.color = col;
            Handles.DrawWireDisc(pos, Vector3.up, 0.7f, 2f);

            LabelStyle.normal.textColor = col;
            Handles.Label(pos + Vector3.up * 1.0f, label, LabelStyle);
        }
    }
}
