using System.Collections.Generic;
using System.Reflection;
using Geis.Attributes;
using UnityEditor;
using UnityEngine;

namespace Geis.Editor
{
    [CustomPropertyDrawer(typeof(FoldoutGroupAttribute))]
    public sealed class FoldoutGroupDrawer : PropertyDrawer
    {
        private const string EditorPrefsPrefix = "Geis.FoldoutGroup.";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (FoldoutGroupAttribute)attribute;
            var fieldLabel = FieldLabelContent(property);
            if (!TryGetGroupContext(property, attr.Label, attr.DefaultExpanded, out var isFirst, out var expanded))
                return EditorGUI.GetPropertyHeight(property, fieldLabel, true);

            if (isFirst)
            {
                float h = EditorGUIUtility.singleLineHeight;
                if (expanded)
                    h += EditorGUI.GetPropertyHeight(property, fieldLabel, true);
                return h;
            }

            return expanded ? EditorGUI.GetPropertyHeight(property, fieldLabel, true) : 0f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (FoldoutGroupAttribute)attribute;
            var fieldLabel = FieldLabelContent(property);
            if (!TryGetGroupContext(property, attr.Label, attr.DefaultExpanded, out var isFirst, out var expanded))
            {
                EditorGUI.PropertyField(position, property, fieldLabel, true);
                return;
            }

            if (!isFirst && !expanded)
                return;

            if (isFirst)
            {
                var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                bool newExpanded = EditorGUI.Foldout(foldoutRect, expanded, attr.Label, true);
                if (newExpanded != expanded)
                    SetFoldoutExpanded(property.serializedObject, attr.Label, newExpanded);

                expanded = newExpanded;

                if (!expanded)
                    return;

                float fieldY = position.y + EditorGUIUtility.singleLineHeight;
                float fieldHeight = EditorGUI.GetPropertyHeight(property, fieldLabel, true);
                var fieldRect = new Rect(position.x, fieldY, position.width, fieldHeight);
                EditorGUI.PropertyField(fieldRect, property, fieldLabel, true);
            }
            else
            {
                EditorGUI.PropertyField(position, property, fieldLabel, true);
            }
        }

        /// <summary>
        /// Use the field display name and tooltip. The <c>label</c> passed into <see cref="PropertyDrawer"/> can match the foldout group name when a string-based attribute owns the drawer.
        /// </summary>
        private static GUIContent FieldLabelContent(SerializedProperty property)
        {
            return new GUIContent(property.displayName, property.tooltip);
        }

        private static bool TryGetGroupContext(
            SerializedProperty property,
            string groupName,
            bool defaultExpanded,
            out bool isFirst,
            out bool expanded)
        {
            isFirst = false;
            expanded = true;

            var so = property.serializedObject;
            if (so == null || so.targetObject == null)
                return false;

            var run = GetContiguousGroupPaths(so, property, groupName);
            if (run.Count == 0)
                return false;

            isFirst = run[0] == property.propertyPath;
            expanded = GetFoldoutExpanded(so, groupName, defaultExpanded);
            return true;
        }

        private static List<string> GetContiguousGroupPaths(SerializedObject so, SerializedProperty property, string groupName)
        {
            var orderedPaths = GetOrderedTopLevelPropertyPaths(so);
            var runs = new List<List<string>>();
            List<string> currentRun = null;

            foreach (var path in orderedPaths)
            {
                var p = so.FindProperty(path);
                if (p == null)
                    continue;

                var foldoutAttr = GetFoldoutAttribute(so.targetObject, p);
                if (foldoutAttr != null && foldoutAttr.Label == groupName)
                {
                    if (currentRun == null)
                        currentRun = new List<string>();
                    currentRun.Add(path);
                }
                else
                {
                    if (currentRun != null)
                    {
                        runs.Add(currentRun);
                        currentRun = null;
                    }
                }
            }

            if (currentRun != null)
                runs.Add(currentRun);

            foreach (var run in runs)
            {
                if (run.Contains(property.propertyPath))
                    return run;
            }

            return new List<string> { property.propertyPath };
        }

        private static List<string> GetOrderedTopLevelPropertyPaths(SerializedObject serializedObject)
        {
            var paths = new List<string>();
            serializedObject.Update();

            var prop = serializedObject.GetIterator();
            if (!prop.NextVisible(true))
                return paths;

            do
            {
                if (prop.propertyPath == "m_Script")
                    continue;

                // Direct MonoBehaviour fields: one segment, no dots (depth can differ by Unity version).
                var path = prop.propertyPath;
                if (!path.Contains("."))
                    paths.Add(path);
            } while (prop.NextVisible(false));

            return paths;
        }

        private static FoldoutGroupAttribute GetFoldoutAttribute(Object target, SerializedProperty prop)
        {
            var field = GetFieldFromSerializedProperty(target, prop);
            return field != null ? field.GetCustomAttribute<FoldoutGroupAttribute>() : null;
        }

        private static FieldInfo GetFieldFromSerializedProperty(Object target, SerializedProperty prop)
        {
            if (target == null || prop == null)
                return null;

            if (prop.propertyPath.Contains("["))
                return null;

            var type = target.GetType();
            var path = prop.propertyPath;
            if (!path.Contains("."))
                return type.GetField(path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var parts = path.Split('.');
            FieldInfo field = null;
            var currentType = type;
            for (var i = 0; i < parts.Length; i++)
            {
                field = currentType.GetField(parts[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null)
                    return null;
                if (i < parts.Length - 1)
                    currentType = field.FieldType;
            }

            return field;
        }

        private static bool GetFoldoutExpanded(SerializedObject so, string groupName, bool defaultExpanded)
        {
            var key = EditorPrefsPrefix + so.targetObject.GetInstanceID() + "." + groupName;
            return EditorPrefs.GetBool(key, defaultExpanded);
        }

        private static void SetFoldoutExpanded(SerializedObject so, string groupName, bool value)
        {
            var key = EditorPrefsPrefix + so.targetObject.GetInstanceID() + "." + groupName;
            EditorPrefs.SetBool(key, value);
        }
    }
}
