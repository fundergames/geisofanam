using System.Collections.Generic;
using System.IO;
using System.Linq;
using PerformanceIntelligence.Testing;
using UnityEditor;
using UnityEngine;

namespace PerformanceIntelligence.Editor
{
    public sealed class PerformanceTestRunnerWindow : EditorWindow
    {
        private PerformanceTestConfig _config;
        private PerformanceTestRunner _activeRunner;
        private Vector2 _scroll;
        private string _lastStatus = "Idle";
        private string _lastOutputRoot = "Assets/PerformanceIntelligence/Data/Captures";
        private int _lastCaptureCount;
        private string _lastSummaryPath;
        private bool _pendingPlayModeStart;
        private bool _autoAddScenesToBuildSettings = true;
        private bool _restoreBuildSettingsAfterRun = true;
        private EditorBuildSettingsScene[] _buildSettingsBackup;
        private bool _buildSettingsWerePatched;
        private PerformanceTestRunner.RunProgress _runProgress;

        [MenuItem("Window/Performance Intelligence/Performance Test Runner")]
        public static void ShowWindow()
        {
            var window = GetWindow<PerformanceTestRunnerWindow>("Performance Test Runner");
            window.minSize = new Vector2(520f, 420f);
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            GUILayout.Label("Performance Test Runner", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _config = (PerformanceTestConfig)EditorGUILayout.ObjectField(
                "Test Config",
                _config,
                typeof(PerformanceTestConfig),
                false);

            if (_config != null)
            {
                DrawValidation();
                DrawPlannedCaptureCount();
            }

            EditorGUILayout.Space();
            DrawControls();
            EditorGUILayout.Space();
            DrawSummary();
            EditorGUILayout.EndScrollView();
        }

        private void DrawValidation()
        {
            var issues = ValidateConfig(_config);
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Configuration valid.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(string.Join("\n", issues.Select(x => $"- {x}")), MessageType.Warning);
        }

        private void DrawPlannedCaptureCount()
        {
            int captures = CountPlannedCaptures(_config);
            EditorGUILayout.LabelField("Planned Captures", captures.ToString());
        }

        private void DrawControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _config != null && !IsRunnerRunning();
                if (GUILayout.Button("Start Test Run", GUILayout.Height(28f)))
                {
                    StartRun();
                }

                GUI.enabled = IsRunnerRunning();
                if (GUILayout.Button("Stop / Cancel", GUILayout.Height(28f)))
                {
                    _activeRunner?.CancelRun();
                    _lastStatus = "Cancellation requested.";
                }

                GUI.enabled = true;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Output Folder"))
                {
                    string path = ResolveOutputPath(_config != null ? _config.outputFolder : _lastOutputRoot);
                    EditorUtility.RevealInFinder(path);
                }
            }

            EditorGUILayout.Space(4f);
            _autoAddScenesToBuildSettings = EditorGUILayout.ToggleLeft(
                "Auto-add missing test scenes to Build Settings before run",
                _autoAddScenesToBuildSettings);

            using (new EditorGUI.DisabledScope(!_autoAddScenesToBuildSettings))
            {
                _restoreBuildSettingsAfterRun = EditorGUILayout.ToggleLeft(
                    "Restore Build Settings scene list after run",
                    _restoreBuildSettingsAfterRun);
            }

            EditorGUILayout.LabelField("Status", _lastStatus);
            DrawLiveProgress();
        }

        private void DrawSummary()
        {
            GUILayout.Label("Last Run Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Captures", _lastCaptureCount.ToString());
            EditorGUILayout.LabelField("Summary File", string.IsNullOrEmpty(_lastSummaryPath) ? "Unavailable" : _lastSummaryPath);
        }

        private void StartRun()
        {
            if (_config == null) return;

            var issues = ValidateConfig(_config);
            if (issues.Count > 0)
            {
                _lastStatus = "Cannot start run: configuration has validation issues.";
                return;
            }

            _lastOutputRoot = _config.outputFolder;

            if (_autoAddScenesToBuildSettings)
            {
                PatchBuildSettingsForRun(_config);
            }

            if (!EditorApplication.isPlaying)
            {
                if (_config.runInEditorPlayMode)
                {
                    _pendingPlayModeStart = true;
                    _lastStatus = "Entering Play Mode to begin automated run.";
                    EditorApplication.isPlaying = true;
                }
                else
                {
                    _lastStatus = "Run requires Play Mode in editor. Enable runInEditorPlayMode or enter Play Mode first.";
                }
                return;
            }

            BeginRunnerInPlayMode();
        }

        private void BeginRunnerInPlayMode()
        {
            if (_config == null) return;
            _activeRunner = FindExistingRunner() ?? PerformanceTestRunner.CreateRunner();
            _activeRunner.OnStatus -= HandleStatus;
            _activeRunner.OnRunCompleted -= HandleRunCompleted;
            _activeRunner.OnProgressUpdated -= HandleProgressUpdated;
            _activeRunner.OnStatus += HandleStatus;
            _activeRunner.OnRunCompleted += HandleRunCompleted;
            _activeRunner.OnProgressUpdated += HandleProgressUpdated;
            _activeRunner.StartRun(_config);
            _lastStatus = "Run started.";
        }

        private static PerformanceTestRunner FindExistingRunner()
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<PerformanceTestRunner>();
#else
            return UnityEngine.Object.FindObjectOfType<PerformanceTestRunner>();
#endif
        }

        private void HandleStatus(string status)
        {
            _lastStatus = status;
            Repaint();
        }

        private void HandleProgressUpdated(PerformanceTestRunner.RunProgress progress)
        {
            _runProgress = progress;
            Repaint();
        }

        private void HandleRunCompleted(IReadOnlyList<PerformanceTestResult> results)
        {
            _lastCaptureCount = results?.Count ?? 0;
            _lastStatus = "Run completed.";
            if (_activeRunner != null)
            {
                _activeRunner.OnStatus -= HandleStatus;
                _activeRunner.OnRunCompleted -= HandleRunCompleted;
                _activeRunner.OnProgressUpdated -= HandleProgressUpdated;
            }

            string outputRoot = ResolveOutputPath(_config != null ? _config.outputFolder : _lastOutputRoot);
            string latestRun = FindLatestRunFolder(outputRoot);
            _lastSummaryPath = string.IsNullOrEmpty(latestRun) ? null : Path.Combine(latestRun, "run_summary.md");
            RestoreBuildSettingsIfNeeded();
            Repaint();
        }

        private void DrawLiveProgress()
        {
            if (_runProgress == null || !IsRunnerRunning()) return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Current Progress", EditorStyles.boldLabel);
            Rect rect = GUILayoutUtility.GetRect(18f, 18f, "TextField");
            EditorGUI.ProgressBar(
                rect,
                _runProgress.NormalizedProgress,
                $"{_runProgress.completedCaptures}/{_runProgress.totalPlannedCaptures} captures");
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Scene", string.IsNullOrWhiteSpace(_runProgress.currentScene) ? "-" : _runProgress.currentScene);
            EditorGUILayout.LabelField("Preset", string.IsNullOrWhiteSpace(_runProgress.currentQualityPreset) ? "-" : _runProgress.currentQualityPreset);
            EditorGUILayout.LabelField("Camera Path", string.IsNullOrWhiteSpace(_runProgress.currentCameraPath) ? "-" : _runProgress.currentCameraPath);
            EditorGUILayout.LabelField("Run",
                $"{_runProgress.currentRunIndex + 1}/{Mathf.Max(1, _runProgress.runsPerConfiguration)}");
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && _pendingPlayModeStart)
            {
                _pendingPlayModeStart = false;
                BeginRunnerInPlayMode();
            }

            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                RestoreBuildSettingsIfNeeded();
            }
        }

        private bool IsRunnerRunning()
        {
            return _activeRunner != null && _activeRunner.IsRunning;
        }

        private static List<string> ValidateConfig(PerformanceTestConfig config)
        {
            var issues = new List<string>();
            if (config == null)
            {
                issues.Add("Config is missing.");
                return issues;
            }

            if (config.sceneTests == null || config.sceneTests.Count == 0)
                issues.Add("No scene tests configured.");
            if (config.qualityPresets == null || config.qualityPresets.Count == 0)
                issues.Add("No quality presets configured.");
            if (config.runsPerConfiguration < 1)
                issues.Add("runsPerConfiguration must be >= 1.");
            if (config.captureDurationSeconds <= 0f)
                issues.Add("captureDurationSeconds must be > 0.");

            if (config.sceneTests != null)
            {
                for (int i = 0; i < config.sceneTests.Count; i++)
                {
                    var scene = config.sceneTests[i];
                    if (scene == null) continue;
                    if (!scene.enabled) continue;
                    if (string.IsNullOrWhiteSpace(scene.sceneName))
                        issues.Add($"Scene test [{i}] has no scene name.");
                }
            }

            return issues;
        }

        private static int CountPlannedCaptures(PerformanceTestConfig config)
        {
            if (config == null) return 0;
            int totalPathSlots = 0;
            if (config.sceneTests != null)
            {
                foreach (var s in config.sceneTests)
                {
                    if (s == null || !s.enabled) continue;
                    int validPaths = s.cameraPaths?.Count(p => p != null && p.HasUsableWaypoints) ?? 0;
                    totalPathSlots += Mathf.Max(1, validPaths);
                }
            }

            int quality = config.qualityPresets?.Count(q => q != null) ?? 0;
            int runs = Mathf.Max(1, config.runsPerConfiguration);
            if (totalPathSlots <= 0 || quality <= 0) return 0;
            return quality * totalPathSlots * runs;
        }

        private static string ResolveOutputPath(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                return Path.Combine(Application.dataPath, "PerformanceIntelligence/Data/Captures");
            if (Path.IsPathRooted(configuredPath))
                return configuredPath;
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, configuredPath);
        }

        private static string FindLatestRunFolder(string outputRoot)
        {
            if (!Directory.Exists(outputRoot)) return null;
            var dir = new DirectoryInfo(outputRoot);
            var latest = dir.GetDirectories("run_*").OrderByDescending(d => d.CreationTimeUtc).FirstOrDefault();
            return latest?.FullName;
        }

        private void PatchBuildSettingsForRun(PerformanceTestConfig config)
        {
            if (config == null || config.sceneTests == null) return;

            var current = EditorBuildSettings.scenes ?? new EditorBuildSettingsScene[0];
            var updated = current.ToList();
            var existingPaths = new HashSet<string>(updated.Select(s => s.path));
            bool addedAny = false;

            foreach (var scene in config.sceneTests)
            {
                if (scene == null || !scene.enabled) continue;
                if (string.IsNullOrWhiteSpace(scene.scenePath)) continue;
                if (existingPaths.Contains(scene.scenePath)) continue;

                updated.Add(new EditorBuildSettingsScene(scene.scenePath, true));
                existingPaths.Add(scene.scenePath);
                addedAny = true;
            }

            if (!addedAny) return;

            _buildSettingsBackup = current.ToArray();
            _buildSettingsWerePatched = true;
            EditorBuildSettings.scenes = updated.ToArray();
            _lastStatus = "Added missing test scenes to Build Settings for this run.";
        }

        private void RestoreBuildSettingsIfNeeded()
        {
            if (!_buildSettingsWerePatched) return;
            if (!_restoreBuildSettingsAfterRun)
            {
                _buildSettingsWerePatched = false;
                _buildSettingsBackup = null;
                return;
            }

            if (_buildSettingsBackup != null)
            {
                EditorBuildSettings.scenes = _buildSettingsBackup;
                _lastStatus = "Restored Build Settings scene list after run.";
            }

            _buildSettingsWerePatched = false;
            _buildSettingsBackup = null;
        }
    }
}
