using UnityEditor;
using UnityEngine;

namespace Geis.Puzzles.Editor
{
    /// <summary>
    /// Custom editor for all <see cref="PuzzleElementBase"/> subclasses (triggers and outputs).
    /// When a puzzle element is selected in the Scene view, draws a colour-coded disc and
    /// floating label showing which realm it belongs to:
    ///   Blue-violet = SoulOnly
    ///   Orange      = PhysicalOnly
    ///   Purple      = BothRealms
    /// </summary>
    [CustomEditor(typeof(PuzzleElementBase), true)]
    public class PuzzleElementEditor : UnityEditor.Editor
    {
        static readonly Color SoulColour     = new Color(0.4f, 0.55f, 1f, 0.85f);
        static readonly Color PhysicalColour = new Color(1f, 0.55f, 0.2f, 0.85f);
        static readonly Color BothColour     = new Color(0.8f, 0.3f, 1f, 0.85f);

        private GUIStyle _realmStyle;
        private GUIStyle RealmStyle
        {
            get
            {
                if (_realmStyle == null)
                {
                    _realmStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize  = 10,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                    };
                }
                return _realmStyle;
            }
        }

        private void OnSceneGUI()
        {
            var elem = (PuzzleElementBase)target;
            if (elem == null) return;

            serializedObject.Update();
            var realmProp = serializedObject.FindProperty("realmMode");
            if (realmProp == null) return;

            var realm = (PuzzleRealmMode)realmProp.enumValueIndex;
            Color col = realm switch
            {
                PuzzleRealmMode.SoulOnly     => SoulColour,
                PuzzleRealmMode.PhysicalOnly => PhysicalColour,
                PuzzleRealmMode.BothRealms   => BothColour,
                _                            => Color.white,
            };

            string label = realm switch
            {
                PuzzleRealmMode.SoulOnly     => "SOUL ONLY",
                PuzzleRealmMode.PhysicalOnly => "PHYSICAL",
                PuzzleRealmMode.BothRealms   => "BOTH REALMS",
                _                            => "",
            };

            Vector3 pos = elem.transform.position;

            // Disc on the floor plane
            Handles.color = new Color(col.r, col.g, col.b, 0.25f);
            Handles.DrawSolidDisc(pos, Vector3.up, 0.7f);
            Handles.color = col;
            Handles.DrawWireDisc(pos, Vector3.up, 0.7f, 2f);

            // Floating label
            RealmStyle.normal.textColor = col;
            Handles.Label(pos + Vector3.up * 1.0f, label, RealmStyle);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUI.changed)
                SceneView.RepaintAll();
        }
    }
}
