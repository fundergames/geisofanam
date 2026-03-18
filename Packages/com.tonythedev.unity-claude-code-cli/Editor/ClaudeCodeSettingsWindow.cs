using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ClaudeCode.Editor
{
    /// <summary>
    /// Small settings window for the Claude CLI path so Unity (which may not have your shell PATH)
    /// can find the binary. Leave empty to use "claude" from PATH.
    /// </summary>
    public class ClaudeCodeSettingsWindow : EditorWindow
    {
        private const string k_StylePath = "Packages/com.tonythedev.unity-claude-code-cli/Editor/UI/ClaudeCodeStyles.uss";
        private TextField _pathField;

        [MenuItem("Window/Claude Code/Settings")]
        public static void ShowWindow()
        {
            var w = GetWindow<ClaudeCodeSettingsWindow>(utility: true, title: "Claude Code Settings", focus: true);
            w.minSize = new Vector2(420, 100);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            var ss = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StylePath);
            if (ss != null) root.styleSheets.Add(ss);

            var current = EditorPrefs.GetString(ClaudeProcess.k_CliPathPrefKey, "").Trim();
            var help = new Label(
                "Claude CLI path (leave empty to use \"claude\" from PATH).\n" +
                "If you see \"command not found\" in the Claude window, set the full path here, e.g.:\n" +
                "/Users/YourName/.local/bin/claude");
            help.style.whiteSpace = WhiteSpace.Normal;
            help.style.marginBottom = 8;
            help.style.color = new Color(0.65f, 0.65f, 0.65f);
            help.style.fontSize = 11;
            root.Add(help);

            _pathField = new TextField { value = current };
            _pathField.style.marginBottom = 10;
            root.Add(_pathField);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            var save = new Button(Save) { text = "Save" };
            save.style.width = 80;
            row.Add(save);
            var clear = new Button(Clear) { text = "Clear" };
            clear.style.width = 80;
            clear.style.marginLeft = 8;
            row.Add(clear);
            root.Add(row);
        }

        private void Save()
        {
            var v = _pathField?.value?.Trim() ?? "";
            EditorPrefs.SetString(ClaudeProcess.k_CliPathPrefKey, v);
            if (string.IsNullOrEmpty(v))
                Debug.Log("Claude Code: CLI path cleared. Using \"claude\" from PATH.");
            else
                Debug.Log($"Claude Code: CLI path set to: {v}");
            Close();
        }

        private void Clear()
        {
            if (_pathField != null) _pathField.value = "";
            EditorPrefs.DeleteKey(ClaudeProcess.k_CliPathPrefKey);
            Debug.Log("Claude Code: CLI path cleared.");
            Close();
        }
    }
}
