using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Funder.Core.Services;

namespace Funder.GameFlow.Editor
{
    public class BuildSettingsHelper : EditorWindow
    {
        [MenuItem("Funder Games/Core/Build Settings Helper")]
        public static void ShowWindow()
        {
            GetWindow<BuildSettingsHelper>("Build Settings Helper");
        }

        private void OnGUI()
        {
            GUILayout.Label("Build Settings & Config Helper", EditorStyles.boldLabel);
            GUILayout.Space(10);

            var config = FGConfigManager.GetConfig();
            
            if (config == null)
            {
                EditorGUILayout.HelpBox(
                    "FGAppConfig not found!\n\n" +
                    "The FGConfigManager will search these paths:\n" +
                    "1. Configs/AppConfig_RogueDeal\n" +
                    "2. FunderCore/FGAppConfig\n" +
                    "3. Configs/FGAppConfig\n\n" +
                    "Make sure your config exists in one of these locations.",
                    MessageType.Warning);
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("Search for All Configs in Project"))
                {
                    FindAllConfigs();
                }
                
                return;
            }

            EditorGUILayout.HelpBox($"✅ Active Config: {AssetDatabase.GetAssetPath(config)}", MessageType.Info);
            
            GUILayout.Space(10);
            GUILayout.Label("Configured Scenes:", EditorStyles.boldLabel);

            DrawSceneInfo("Entry", config.EntrySceneName);
            DrawSceneInfo("Login", config.LoginSceneName);
            DrawSceneInfo("Menu", config.MenuSceneName);
            DrawSceneInfo("Game (Lobby)", config.GameSceneName);
            DrawSceneInfo("Results", config.ResultsSceneName);

            GUILayout.Space(10);

            if (GUILayout.Button("Add All Configured Scenes to Build Settings", GUILayout.Height(40)))
            {
                AddScenesToBuildSettings(config);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Refresh Config"))
            {
                FGConfigManager.ClearCache();
                Repaint();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Show Current Build Settings"))
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }
        }

        private void FindAllConfigs()
        {
            var guids = AssetDatabase.FindAssets("t:FGAppConfig");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("No Configs Found", 
                    "No FGAppConfig assets found in the project.", 
                    "OK");
                return;
            }

            string message = $"Found {guids.Length} FGAppConfig(s):\n\n";
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var resourcePath = GetResourcePath(path);
                message += $"• {path}\n";
                if (!string.IsNullOrEmpty(resourcePath))
                {
                    message += $"  Resources path: {resourcePath}\n";
                }
            }

            EditorUtility.DisplayDialog("Configs Found", message, "OK");
        }

        private string GetResourcePath(string assetPath)
        {
            int resourcesIndex = assetPath.IndexOf("Resources/");
            if (resourcesIndex >= 0)
            {
                var path = assetPath.Substring(resourcesIndex + "Resources/".Length);
                path = path.Replace(".asset", "");
                return path;
            }
            return null;
        }

        private void DrawSceneInfo(string label, string sceneName)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}:", GUILayout.Width(100));
            GUILayout.Label(sceneName);

            var scenePath = FindScenePath(sceneName);
            if (string.IsNullOrEmpty(scenePath))
            {
                GUILayout.Label("❌ NOT FOUND", EditorStyles.boldLabel);
            }
            else
            {
                var inBuildSettings = IsSceneInBuildSettings(scenePath);
                if (inBuildSettings)
                {
                    GUILayout.Label("✅ In Build", EditorStyles.boldLabel);
                }
                else
                {
                    GUILayout.Label("⚠️ Not in Build", EditorStyles.boldLabel);
                }
            }

            GUILayout.EndHorizontal();
        }

        private string FindScenePath(string sceneName)
        {
            var guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name == sceneName)
                {
                    return path;
                }
            }
            return null;
        }

        private bool IsSceneInBuildSettings(string scenePath)
        {
            return EditorBuildSettings.scenes.Any(s => s.path == scenePath);
        }

        private void AddScenesToBuildSettings(FGAppConfig config)
        {
            var scenesToAdd = new List<string>
            {
                config.EntrySceneName,
                config.LoginSceneName,
                config.MenuSceneName,
                config.GameSceneName,
                config.ResultsSceneName
            };

            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int addedCount = 0;

            foreach (var sceneName in scenesToAdd)
            {
                var scenePath = FindScenePath(sceneName);
                if (string.IsNullOrEmpty(scenePath))
                {
                    Debug.LogWarning($"Scene '{sceneName}' not found in project. Skipping.");
                    continue;
                }

                if (!IsSceneInBuildSettings(scenePath))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                    addedCount++;
                    Debug.Log($"Added '{sceneName}' to Build Settings");
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();

            if (addedCount > 0)
            {
                Debug.Log($"✅ Added {addedCount} scene(s) to Build Settings!");
                EditorUtility.DisplayDialog("Success", 
                    $"Added {addedCount} scene(s) to Build Settings.\n\n" +
                    "You can now test your game flow!", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Info", 
                    "All configured scenes are already in Build Settings!", 
                    "OK");
            }
        }

        private void FixConfigLocation(FGAppConfig sourceConfig)
        {
            EditorUtility.DisplayDialog("Not Needed", 
                "The new multi-product system doesn't require copying configs!\n\n" +
                "Just make sure your config is in a Resources folder and the ProductBootstrap points to it.\n\n" +
                "See MULTI_PRODUCT_SETUP.md for details.", 
                "OK");
        }
    }
}
