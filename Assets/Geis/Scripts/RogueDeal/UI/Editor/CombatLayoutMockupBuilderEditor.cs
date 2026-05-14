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

using UnityEngine;
using UnityEditor;

namespace RogueDeal.UI.Editor
{
    [CustomEditor(typeof(CombatLayoutMockupBuilder))]
    public class CombatLayoutMockupBuilderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
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

            SerializedProperty prefabProp = serializedObject.FindProperty("playerCharacterPrefab");
            if (prefabProp != null && prefabProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "3D combat mockup: assign Player Character Prefab (e.g. Assets/Geis/Combat/Prefabs/Player.prefab) or click Assign Geis Player below.",
                    MessageType.Warning);
                if (GUILayout.Button("Assign Geis Combat Player.prefab", GUILayout.Height(26)))
                {
                    GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Geis/Combat/Prefabs/Player.prefab");
                    prefabProp.objectReferenceValue = p;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(builder);
                }

                EditorGUILayout.Space(4);
            }

            SerializedProperty e1 = serializedObject.FindProperty("enemy1CharacterPrefab");
            SerializedProperty e2 = serializedObject.FindProperty("enemy2CharacterPrefab");
            SerializedProperty e3 = serializedObject.FindProperty("enemy3CharacterPrefab");
            bool anyEnemySlotUnset =
                (e1 != null && e1.objectReferenceValue == null)
                || (e2 != null && e2.objectReferenceValue == null)
                || (e3 != null && e3.objectReferenceValue == null);

            if (anyEnemySlotUnset)
            {
                EditorGUILayout.HelpBox(
                    "Enemy slots: assign prefabs per slot below, or click Assign Phase1 Humanoid to fill any empty slots.",
                    MessageType.Info);
                if (GUILayout.Button("Fill empty enemy slots with P_Enemy_Phase1Humanoid", GUILayout.Height(26)))
                {
                    GameObject enemyPrefab =
                        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/P_Enemy_Phase1Humanoid.prefab");
                    if (e1 != null && e1.objectReferenceValue == null)
                    {
                        e1.objectReferenceValue = enemyPrefab;
                    }

                    if (e2 != null && e2.objectReferenceValue == null)
                    {
                        e2.objectReferenceValue = enemyPrefab;
                    }

                    if (e3 != null && e3.objectReferenceValue == null)
                    {
                        e3.objectReferenceValue = enemyPrefab;
                    }

                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(builder);
                }

                EditorGUILayout.Space(4);
            }

            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
