using UnityEngine;
using UnityEditor;

namespace RogueDeal.UI.Editor
{
    [CustomEditor(typeof(CombatLayoutMockupBuilder))]
    public class CombatLayoutMockupBuilderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CombatLayoutMockupBuilder builder = (CombatLayoutMockupBuilder)target;

            EditorGUILayout.Space(10);
            
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("🔨 REBUILD MOCKUP", GUILayout.Height(40)))
            {
                builder.BuildMockup();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
            if (GUILayout.Button("📊 Print Current Values", GUILayout.Height(30)))
            {
                builder.PrintLayoutValues();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Default", GUILayout.Height(25)))
            {
                builder.ApplyPresetDefaultHorizontal();
            }
            if (GUILayout.Button("Wide Fan", GUILayout.Height(25)))
            {
                builder.ApplyPresetWideFan();
            }
            if (GUILayout.Button("Tight Arc", GUILayout.Height(25)))
            {
                builder.ApplyPresetTightArc();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Linear", GUILayout.Height(25)))
            {
                builder.ApplyPresetLinear();
            }
            if (GUILayout.Button("50/50 Split", GUILayout.Height(25)))
            {
                builder.ApplyPresetFiftyFifty();
            }
            if (GUILayout.Button("Portrait", GUILayout.Height(25)))
            {
                builder.ApplyPresetPortrait();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Adjust sliders below, then click REBUILD MOCKUP to see changes", MessageType.Info);
            EditorGUILayout.Space(5);

            DrawDefaultInspector();
        }
    }
}
