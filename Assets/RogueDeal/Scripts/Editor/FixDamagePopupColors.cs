using UnityEngine;
using UnityEditor;
using RogueDeal.Combat;

namespace RogueDeal.Editor
{
    public static class FixDamagePopupColors
    {
        [MenuItem("RogueDeal/Fix DamagePopup Colors (Yellow Crits)")]
        public static void FixColors()
        {
            string prefabPath = "Assets/RogueDeal/Prefabs/UI/DamagePopup.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Error",
                    $"DamagePopup prefab not found!\n\nExpected path:\n{prefabPath}",
                    "OK");
                return;
            }

            DamagePopup damagePopup = prefab.GetComponent<DamagePopup>();
            if (damagePopup == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "DamagePopup component not found on prefab!",
                    "OK");
                return;
            }

            SerializedObject so = new SerializedObject(damagePopup);
            
            SerializedProperty normalColorProp = so.FindProperty("normalColor");
            SerializedProperty criticalColorProp = so.FindProperty("criticalColor");

            normalColorProp.colorValue = Color.white;
            criticalColorProp.colorValue = Color.yellow;

            so.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Success!",
                "DamagePopup colors updated!\n\n" +
                "✅ Normal hits: White\n" +
                "✅ Critical hits: Yellow\n\n" +
                "Prefab has been saved.",
                "Great!");

            Selection.activeObject = prefab;
        }

        [MenuItem("RogueDeal/Configure DamagePopup Colors...", false, 101)]
        public static void ShowColorPicker()
        {
            ColorPickerWindow.ShowWindow();
        }
    }

    public class ColorPickerWindow : EditorWindow
    {
        private Color normalColor = Color.white;
        private Color criticalColor = Color.yellow;

        public static void ShowWindow()
        {
            ColorPickerWindow window = GetWindow<ColorPickerWindow>("Damage Popup Colors");
            window.minSize = new Vector2(300, 200);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Damage Popup Color Settings", EditorStyles.boldLabel);
            GUILayout.Space(10);

            normalColor = EditorGUILayout.ColorField("Normal Hit Color", normalColor);
            criticalColor = EditorGUILayout.ColorField("Critical Hit Color", criticalColor);

            GUILayout.Space(10);

            GUILayout.Label("Presets:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Default (White / Yellow)"))
            {
                normalColor = Color.white;
                criticalColor = Color.yellow;
            }

            if (GUILayout.Button("Classic (White / Red)"))
            {
                normalColor = Color.white;
                criticalColor = Color.red;
            }

            if (GUILayout.Button("Fantasy (White / Gold)"))
            {
                normalColor = Color.white;
                criticalColor = new Color(1f, 0.84f, 0f);
            }

            if (GUILayout.Button("Modern (Light Gray / Orange)"))
            {
                normalColor = new Color(0.9f, 0.9f, 0.9f);
                criticalColor = new Color(1f, 0.65f, 0f);
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Apply to Prefab", GUILayout.Height(40)))
            {
                ApplyColors();
            }
        }

        private void ApplyColors()
        {
            string prefabPath = "Assets/RogueDeal/Prefabs/UI/DamagePopup.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Error",
                    $"DamagePopup prefab not found!\n\nExpected path:\n{prefabPath}",
                    "OK");
                return;
            }

            DamagePopup damagePopup = prefab.GetComponent<DamagePopup>();
            if (damagePopup == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "DamagePopup component not found on prefab!",
                    "OK");
                return;
            }

            SerializedObject so = new SerializedObject(damagePopup);

            SerializedProperty normalColorProp = so.FindProperty("normalColor");
            SerializedProperty criticalColorProp = so.FindProperty("criticalColor");

            normalColorProp.colorValue = normalColor;
            criticalColorProp.colorValue = criticalColor;

            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Success!",
                $"Colors applied to DamagePopup prefab!\n\n" +
                $"Normal: RGB({(int)(normalColor.r * 255)}, {(int)(normalColor.g * 255)}, {(int)(normalColor.b * 255)})\n" +
                $"Critical: RGB({(int)(criticalColor.r * 255)}, {(int)(criticalColor.g * 255)}, {(int)(criticalColor.b * 255)})",
                "OK");

            Selection.activeObject = prefab;
            Close();
        }
    }
}
