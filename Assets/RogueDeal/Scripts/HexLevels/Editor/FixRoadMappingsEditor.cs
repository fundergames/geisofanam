using UnityEngine;
using UnityEditor;
using RogueDeal.HexLevels;

namespace RogueDeal.HexLevels.Editor
{
    public class FixRoadMappingsEditor : EditorWindow
    {
        private ConnectionPatternMappings mappingsAsset;

        [MenuItem("Tools/Hex Levels/Fix Road Mappings")]
        static void ShowWindow()
        {
            var window = GetWindow<FixRoadMappingsEditor>("Fix Road Mappings");
            window.Show();
        }

        [MenuItem("Tools/Hex Levels/Reload Mappings Asset")]
        static void ReloadMappingsAsset()
        {
            AssetDatabase.Refresh();
            
            string assetPath = "Assets/RogueDeal/Resources/Data/HexLevels/ConnectionPatternMappings.asset";
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            
            var mappings = AssetDatabase.LoadAssetAtPath<ConnectionPatternMappings>(assetPath);
            if (mappings != null)
            {
                SmartTileSelector.SetMappingsAsset(mappings);
                
                var tool = FindFirstObjectByType<HexLevelEditorTool>();
                if (tool != null)
                {
                    tool.mappingsAsset = mappings;
                    EditorUtility.SetDirty(tool);
                    Debug.Log("✓ Updated HexLevelEditorTool.mappingsAsset reference");
                }
                
                Debug.Log("✓ Reloaded ConnectionPatternMappings asset into SmartTileSelector");
                EditorUtility.DisplayDialog("Success", "Reloaded mappings asset successfully!\n\nPattern 1 now maps to variant A (straight road).", "OK");
            }
            else
            {
                Debug.LogError($"Failed to load ConnectionPatternMappings from {assetPath}");
                EditorUtility.DisplayDialog("Error", "Failed to load mappings asset!", "OK");
            }
        }

        void OnGUI()
        {
            GUILayout.Label("Fix Road Connection Mappings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This will regenerate the road connection pattern mappings with correct variants:\n\n" +
                "• 1 connection (patterns 1,2,4,8,16,32) → A (straight)\n" +
                "• 2 opposite connections (patterns 9,18,36) → A (straight)\n" +
                "• 2 adjacent connections → B (curve)\n" +
                "• 3 connections → E (T-junction)\n" +
                "• 4 connections → H (4-way)\n" +
                "• 5 connections → I (5-way)\n" +
                "• 6 connections → I (6-way)",
                MessageType.Info
            );

            EditorGUILayout.Space();

            mappingsAsset = (ConnectionPatternMappings)EditorGUILayout.ObjectField(
                "Mappings Asset",
                mappingsAsset,
                typeof(ConnectionPatternMappings),
                false
            );

            EditorGUILayout.Space();

            if (mappingsAsset == null)
            {
                EditorGUILayout.HelpBox(
                    "Please select the ConnectionPatternMappings asset.\n" +
                    "Usually located at: Assets/RogueDeal/Resources/Data/HexLevels/ConnectionPatternMappings.asset",
                    MessageType.Warning
                );
            }

            GUI.enabled = mappingsAsset != null;
            if (GUILayout.Button("Generate Correct Mappings", GUILayout.Height(40)))
            {
                GenerateCorrectMappings();
            }
            GUI.enabled = true;
        }

        void GenerateCorrectMappings()
        {
            if (mappingsAsset == null) return;

            Undo.RecordObject(mappingsAsset, "Fix Road Mappings");

            mappingsAsset.roadMappings.Clear();

            for (int pattern = 0; pattern <= 63; pattern++)
            {
                int connectionCount = CountBits(pattern);
                string variant = DetermineCorrectVariant(pattern, connectionCount);

                if (!string.IsNullOrEmpty(variant))
                {
                    mappingsAsset.SetRoadMapping(pattern, variant);
                    Debug.Log($"Pattern {pattern} (0b{System.Convert.ToString(pattern, 2).PadLeft(6, '0')}, {connectionCount} connections) → {variant}");
                }
            }

            EditorUtility.SetDirty(mappingsAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"✓ Successfully regenerated {mappingsAsset.roadMappings.Count} road mappings!");
            EditorUtility.DisplayDialog(
                "Success",
                $"Generated {mappingsAsset.roadMappings.Count} correct road pattern mappings!",
                "OK"
            );
        }

        int CountBits(int value)
        {
            int count = 0;
            while (value != 0)
            {
                count++;
                value &= value - 1;
            }
            return count;
        }

        string DetermineCorrectVariant(int pattern, int connectionCount)
        {
            if (pattern == 0)
                return "A";

            switch (connectionCount)
            {
                case 1:
                    return "A";

                case 2:
                    if (IsOppositeConnections(pattern))
                        return "A";
                    else
                        return "B";

                case 3:
                    return "E";

                case 4:
                    return "H";

                case 5:
                    return "I";

                case 6:
                    return "I";

                default:
                    return null;
            }
        }

        bool IsOppositeConnections(int pattern)
        {
            return pattern == (0b000001 | 0b001000) || // E + W (1 + 8 = 9)
                   pattern == (0b000010 | 0b010000) || // NE + SW (2 + 16 = 18)
                   pattern == (0b000100 | 0b100000);   // NW + SE (4 + 32 = 36)
        }
    }
}
