using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace PerformanceIntelligence.Testing
{
    [DisallowMultipleComponent]
    public sealed class PerformanceTestRunner : MonoBehaviour
    {
        [Serializable]
        public sealed class RunProgress
        {
            public string currentScene;
            public string currentQualityPreset;
            public string currentCameraPath;
            public int currentRunIndex;
            public int runsPerConfiguration;
            public int completedCaptures;
            public int totalPlannedCaptures;

            public float NormalizedProgress =>
                totalPlannedCaptures > 0 ? Mathf.Clamp01((float)completedCaptures / totalPlannedCaptures) : 0f;
        }

        [Serializable]
        private sealed class CaptureEnvelope
        {
            public PerformanceTestRunMetadata metadata;
            public SceneCensus sceneCensus;
            public CaptureSession captureSession;
            public PerformanceTestResult result;
        }

        public event Action<string> OnStatus;
        public event Action<PerformanceTestResult> OnCaptureCompleted;
        public event Action<IReadOnlyList<PerformanceTestResult>> OnRunCompleted;
        public event Action<RunProgress> OnProgressUpdated;

        public bool IsRunning { get; private set; }
        public bool IsCancelled { get; private set; }

        private readonly List<PerformanceTestResult> _results = new List<PerformanceTestResult>();
        private Coroutine _runCoroutine;
        private PerformanceCapture _capture;
        private CaptureSession _lastSession;
        private string _sessionId;
        private readonly RunProgress _progress = new RunProgress();
        private int _originalTargetFrameRate;
        private int _originalVSyncCount;

        public static PerformanceTestRunner CreateRunner()
        {
            var go = new GameObject("_PerformanceTestRunner");
            DontDestroyOnLoad(go);
            return go.AddComponent<PerformanceTestRunner>();
        }

        public void StartRun(PerformanceTestConfig config)
        {
            if (IsRunning || config == null) return;
            IsRunning = true;
            IsCancelled = false;
            _originalTargetFrameRate = Application.targetFrameRate;
            _originalVSyncCount = QualitySettings.vSyncCount;
            _results.Clear();
            _sessionId = Guid.NewGuid().ToString("N");
            _runCoroutine = StartCoroutine(RunRoutine(config));
        }

        public void CancelRun()
        {
            IsCancelled = true;
            if (_capture != null && _capture.IsCapturing)
            {
                _capture.StopCapture();
            }
        }

        private IEnumerator RunRoutine(PerformanceTestConfig config)
        {
            string rootOutput = ResolveOutputRoot(config.outputFolder);
            Directory.CreateDirectory(rootOutput);
            string runOutputDir = Path.Combine(rootOutput, $"run_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{_sessionId}");
            Directory.CreateDirectory(runOutputDir);

            EnsureCaptureComponent();
            int runCount = Mathf.Max(1, config.runsPerConfiguration);
            int totalPlannedCaptures = CountPlannedCaptures(config, runCount);
            _progress.completedCaptures = 0;
            _progress.totalPlannedCaptures = totalPlannedCaptures;
            _progress.runsPerConfiguration = runCount;
            PublishProgress();

            foreach (var sceneTest in config.sceneTests)
            {
                if (IsCancelled) break;
                if (sceneTest == null || !sceneTest.enabled) continue;
                if (string.IsNullOrWhiteSpace(sceneTest.sceneName))
                {
                    LogStatus("Skipping scene entry with missing scene name.");
                    continue;
                }

                yield return LoadSceneSafe(sceneTest.sceneName, sceneTest.scenePath);
                if (IsCancelled) break;
                yield return null;

                SceneCensus sceneCensus;
                try
                {
                    sceneCensus = SceneCensus.Capture();
                }
                catch (Exception ex)
                {
                    LogStatus($"Scene census failed in {sceneTest.sceneName}: {ex.Message}");
                    continue;
                }

                foreach (var preset in config.qualityPresets)
                {
                    if (IsCancelled) break;
                    if (preset == null) continue;
                    preset.Apply();
                    yield return null;

                    var validCameraPaths = (sceneTest.cameraPaths ?? new List<CameraPathDefinition>())
                        .Where(p => p != null && p.HasUsableWaypoints)
                        .ToList();
                    bool useMainCameraFallback = validCameraPaths.Count == 0;
                    if (useMainCameraFallback)
                    {
                        LogStatus($"No camera paths in {sceneTest.sceneName}; using main camera start transform.");
                    }

                    int pathIterations = useMainCameraFallback ? 1 : validCameraPaths.Count;
                    for (int pathIndex = 0; pathIndex < pathIterations; pathIndex++)
                    {
                        if (IsCancelled) break;
                        CameraPathDefinition cameraPath = useMainCameraFallback ? null : validCameraPaths[pathIndex];

                        for (int runIndex = 0; runIndex < runCount; runIndex++)
                        {
                            if (IsCancelled) break;
                            string pathId = cameraPath == null
                                ? "MainCameraStart"
                                : (string.IsNullOrWhiteSpace(cameraPath.pathId) ? cameraPath.name : cameraPath.pathId);
                            _progress.currentScene = sceneTest.sceneName;
                            _progress.currentQualityPreset = string.IsNullOrWhiteSpace(preset.presetName) ? preset.name : preset.presetName;
                            _progress.currentCameraPath = pathId;
                            _progress.currentRunIndex = runIndex;
                            PublishProgress();

                            var camera = EnsureTestCamera();
                            Vector3 startPosition = camera.transform.position;
                            Quaternion startRotation = camera.transform.rotation;
                            float startFov = camera.fieldOfView;

                            CameraPathPlayback playback = null;
                            if (cameraPath != null)
                            {
                                playback = EnsurePlayback(camera);
                                playback.StartPlayback(cameraPath, config.captureDurationSeconds);
                            }

                            if (config.warmupDurationSeconds > 0f)
                            {
                                if (cameraPath != null)
                                {
                                    yield return Warmup(playback, config.warmupDurationSeconds);
                                }
                                else
                                {
                                    yield return WarmupStaticCamera(camera, startPosition, startRotation, startFov, config.warmupDurationSeconds);
                                }
                            }

                            if (cameraPath != null)
                            {
                                playback.ResetToStart();
                            }
                            else
                            {
                                camera.transform.SetPositionAndRotation(startPosition, startRotation);
                                camera.fieldOfView = startFov;
                            }

                            _lastSession = null;
                            _capture.captureDuration = Mathf.Max(0.1f, config.captureDurationSeconds);
                            _capture.StartCapture();

                            while (_capture.IsCapturing && !IsCancelled)
                            {
                                if (cameraPath == null)
                                {
                                    camera.transform.SetPositionAndRotation(startPosition, startRotation);
                                    camera.fieldOfView = startFov;
                                }
                                yield return null;
                            }

                            if (IsCancelled) break;
                            if (_lastSession == null)
                            {
                                LogStatus("Capture failed: no session was produced.");
                                continue;
                            }

                            var metadata = PerformanceTestRunMetadata.Create(
                                _lastSession.sessionId,
                                sceneTest.sceneName,
                                pathId,
                                string.IsNullOrWhiteSpace(preset.presetName) ? preset.name : preset.presetName,
                                runIndex,
                                config.captureDurationSeconds,
                                config.warmupDurationSeconds,
                                sceneTest.notes);

                            PerformanceTestResult result = BuildResult(metadata, sceneCensus, _lastSession);
                            WriteOutputs(config, runOutputDir, result, metadata, sceneCensus, _lastSession, preset);
                            _results.Add(result);
                            _progress.completedCaptures++;
                            PublishProgress();
                            OnCaptureCompleted?.Invoke(result);
                        }
                    }
                }
            }

            if (config.generateMarkdownReport)
            {
                WriteMarkdownSummary(runOutputDir, _results);
            }

            IsRunning = false;
            RestoreOriginalDisplaySettings();
            OnRunCompleted?.Invoke(_results);
            LogStatus(IsCancelled ? "Performance test run cancelled." : "Performance test run complete.");
        }

        private static int CountPlannedCaptures(PerformanceTestConfig config, int runCount)
        {
            if (config == null || config.sceneTests == null || config.qualityPresets == null) return 0;
            int qualityCount = config.qualityPresets.Count(p => p != null);
            if (qualityCount == 0) return 0;

            int pathSlots = 0;
            foreach (var scene in config.sceneTests)
            {
                if (scene == null || !scene.enabled || string.IsNullOrWhiteSpace(scene.sceneName)) continue;
                int validPaths = scene.cameraPaths?.Count(p => p != null && p.HasUsableWaypoints) ?? 0;
                pathSlots += Mathf.Max(1, validPaths);
            }

            return pathSlots * qualityCount * Mathf.Max(1, runCount);
        }

        private void PublishProgress()
        {
            OnProgressUpdated?.Invoke(_progress);
        }

        private void RestoreOriginalDisplaySettings()
        {
            Application.targetFrameRate = _originalTargetFrameRate;
            QualitySettings.vSyncCount = _originalVSyncCount;
            QualityPresetDefinition.RestoreResolutionSnapshot();
        }

        private IEnumerator LoadSceneSafe(string sceneName, string scenePath)
        {
            LogStatus($"Loading scene: {sceneName}");
            AsyncOperation loadOp;
            try
            {
                loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                LogStatus($"Primary load failed for scene '{sceneName}': {ex.Message}");
#if UNITY_EDITOR
                if (!string.IsNullOrWhiteSpace(scenePath))
                {
                    AsyncOperation editorLoadOp = null;
                    try
                    {
                        var parameters = new LoadSceneParameters(LoadSceneMode.Single);
                        editorLoadOp = EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, parameters);
                    }
                    catch (Exception editorEx)
                    {
                        LogStatus($"Editor fallback load failed for path '{scenePath}': {editorEx.Message}");
                    }

                    if (editorLoadOp != null)
                    {
                        loadOp = editorLoadOp;
                    }
                    else
                    {
                        yield break;
                    }
                }
                else
                {
                    yield break;
                }
#else
                yield break;
#endif
            }

            if (loadOp == null)
            {
                LogStatus($"Scene load returned null: {sceneName}");
                yield break;
            }

            while (!loadOp.isDone)
            {
                if (IsCancelled) yield break;
                yield return null;
            }
        }

        private IEnumerator Warmup(CameraPathPlayback playback, float seconds)
        {
            float t = 0f;
            while (t < seconds && !IsCancelled)
            {
                t += Time.unscaledDeltaTime;
                if (!playback.IsPlaying && !playback.IsComplete) playback.ResetToStart();
                yield return null;
            }
        }

        private static IEnumerator WarmupStaticCamera(
            Camera camera,
            Vector3 startPosition,
            Quaternion startRotation,
            float startFov,
            float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                if (camera != null)
                {
                    camera.transform.SetPositionAndRotation(startPosition, startRotation);
                    camera.fieldOfView = startFov;
                }
                yield return null;
            }
        }

        private void EnsureCaptureComponent()
        {
            if (_capture != null) return;
            _capture = gameObject.GetComponent<PerformanceCapture>();
            if (_capture == null) _capture = gameObject.AddComponent<PerformanceCapture>();
            _capture.OnCaptureComplete -= OnCaptureComplete;
            _capture.OnCaptureComplete += OnCaptureComplete;
        }

        private void OnCaptureComplete(CaptureSession session)
        {
            _lastSession = session;
        }

        private static Camera EnsureTestCamera()
        {
            var cam = Camera.main;
            if (cam != null) return cam;

            var existing = FindAnyCamera();
            if (existing != null) return existing;

            var go = new GameObject("PerformanceTestCamera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
            return cam;
        }

        private static Camera FindAnyCamera()
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<Camera>();
#else
            return UnityEngine.Object.FindObjectOfType<Camera>();
#endif
        }

        private static CameraPathPlayback EnsurePlayback(Camera camera)
        {
            var pb = camera.GetComponent<CameraPathPlayback>();
            if (pb == null) pb = camera.gameObject.AddComponent<CameraPathPlayback>();
            pb.Initialize(camera);
            return pb;
        }

        private static string ResolveOutputRoot(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                return Path.Combine(Application.dataPath, "PerformanceIntelligence/Data/Captures");

            if (Path.IsPathRooted(configuredPath)) return configuredPath;
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, configuredPath);
        }

        private static PerformanceTestResult BuildResult(PerformanceTestRunMetadata metadata, SceneCensus sceneCensus, CaptureSession session)
        {
            var result = new PerformanceTestResult
            {
                metadata = metadata,
                sceneCensusSummary = sceneCensus,
            };

            if (session.frames == null || session.frames.Count == 0) return result;

            var frameTimesMs = session.frames.Select(f => f.deltaTime * 1000f).OrderBy(v => v).ToArray();
            result.averageFrameTimeMs = frameTimesMs.Average();
            result.medianFrameTimeMs = Percentile(frameTimesMs, 0.5f);
            result.p90FrameTimeMs = Percentile(frameTimesMs, 0.9f);
            result.p95FrameTimeMs = Percentile(frameTimesMs, 0.95f);
            result.maxFrameTimeMs = frameTimesMs[frameTimesMs.Length - 1];
            result.averageFps = session.frames.Average(f => f.estimatedFPS);
            result.minFps = session.frames.Min(f => f.estimatedFPS);
            result.averageMemory = session.frames.Average(f => f.totalReservedMemory / 1_048_576f);
            result.peakMemory = session.frames.Max(f => f.totalReservedMemory / 1_048_576f);
            var gcFrames = session.frames.Where(f => f.gcAllocBytes >= 0).Select(f => (double)f.gcAllocBytes).ToArray();
            result.averageGcAlloc = gcFrames.Length > 0 ? (float)gcFrames.Average() : -1f;
            return result;
        }

        private static float Percentile(IReadOnlyList<float> orderedValues, float p)
        {
            if (orderedValues == null || orderedValues.Count == 0) return 0f;
            float rank = Mathf.Clamp01(p) * (orderedValues.Count - 1);
            int low = Mathf.FloorToInt(rank);
            int high = Mathf.CeilToInt(rank);
            if (low == high) return orderedValues[low];
            float t = rank - low;
            return Mathf.Lerp(orderedValues[low], orderedValues[high], t);
        }

        private void WriteOutputs(
            PerformanceTestConfig config,
            string runOutputDir,
            PerformanceTestResult result,
            PerformanceTestRunMetadata metadata,
            SceneCensus sceneCensus,
            CaptureSession session,
            QualityPresetDefinition preset)
        {
            string safeScene = Sanitize(metadata.sceneName);
            string safePath = Sanitize(metadata.cameraPathId);
            string safePreset = Sanitize(metadata.qualityPresetName);
            string filePrefix = $"{safeScene}__{safePath}__{safePreset}__run{metadata.runIndex:D2}";

            try
            {
                if (config.exportJson)
                {
                    string jsonPath = Path.Combine(runOutputDir, $"{filePrefix}.json");
                    var envelope = new CaptureEnvelope
                    {
                        metadata = metadata,
                        sceneCensus = sceneCensus,
                        captureSession = session,
                        result = result,
                    };
                    File.WriteAllText(jsonPath, JsonUtility.ToJson(envelope, true), Encoding.UTF8);
                    result.jsonOutputPath = jsonPath;
                }

                if (config.exportCsv)
                {
                    string csvPath = Path.Combine(runOutputDir, $"{filePrefix}.csv");
                    File.WriteAllText(csvPath, BuildMlFlatCsv(result, sceneCensus, preset), Encoding.UTF8);
                    result.csvOutputPath = csvPath;

                    string mergedDatasetPath = Path.Combine(runOutputDir, "dataset_flat.csv");
                    AppendMergedDatasetRow(mergedDatasetPath, result, sceneCensus, preset);
                }

                string summaryPath = Path.Combine(runOutputDir, $"{filePrefix}.summary.json");
                File.WriteAllText(summaryPath, JsonUtility.ToJson(result, true), Encoding.UTF8);
                result.summaryOutputPath = summaryPath;
            }
            catch (Exception ex)
            {
                LogStatus($"File write error: {ex.Message}");
            }
        }

        private static string BuildMlFlatCsv(PerformanceTestResult result, SceneCensus census, QualityPresetDefinition preset)
        {
            var sb = new StringBuilder();
            sb.AppendLine(FlatHeader());
            sb.AppendLine(FlatRow(result, census, preset));
            return sb.ToString();
        }

        private static void AppendMergedDatasetRow(string mergedDatasetPath, PerformanceTestResult result, SceneCensus census, QualityPresetDefinition preset)
        {
            bool exists = File.Exists(mergedDatasetPath);
            using (var stream = new FileStream(mergedDatasetPath, FileMode.Append, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                if (!exists)
                {
                    writer.WriteLine(FlatHeader());
                }
                writer.WriteLine(FlatRow(result, census, preset));
            }
        }

        private static string FlatHeader()
        {
            return string.Join(",",
                "sessionId", "sceneName", "cameraPathId", "qualityPresetName", "runIndex", "timestampUtc",
                "unityVersion", "platform", "graphicsDeviceType", "graphicsDeviceName", "renderPipeline", "resolution",
                "targetFrameRate", "vSyncCount", "captureDuration", "warmupDuration", "isEditor",
                "qualityLevelIndex", "renderScale", "shadowDistance", "shadowCascades", "antiAliasingLevel", "resolutionWidth", "resolutionHeight",
                "activeGameObjects", "activeRenderers", "meshRenderers", "skinnedMeshRenderers", "particleSystems", "lights", "realtimeLights",
                "shadowCastingLights", "cameras", "canvases", "rigidbodies", "colliders", "animators", "uniqueMaterials", "uniqueShaders", "estimatedTriangleCount",
                "avgFrameTimeMs", "medianFrameTimeMs", "p90FrameTimeMs", "p95FrameTimeMs", "maxFrameTimeMs", "avgFps", "minFps", "averageMemoryMb", "peakMemoryMb", "averageGcAllocBytes");
        }

        private static string FlatRow(PerformanceTestResult result, SceneCensus c, QualityPresetDefinition preset)
        {
            var m = result.metadata;
            return string.Join(",",
                Csv(m.sessionId), Csv(m.sceneName), Csv(m.cameraPathId), Csv(m.qualityPresetName), m.runIndex, Csv(m.timestampUtc),
                Csv(m.unityVersion), Csv(m.platform), Csv(m.graphicsDeviceType), Csv(m.graphicsDeviceName), Csv(m.renderPipeline), Csv(m.resolution),
                m.targetFrameRate, m.vSyncCount, F(m.captureDuration), F(m.warmupDuration), m.isEditor ? "1" : "0",
                preset != null ? preset.qualityLevelIndex.ToString() : "",
                preset != null ? F(preset.renderScale) : "",
                preset != null ? F(preset.shadowDistance) : "",
                preset != null ? preset.shadowCascades.ToString() : "",
                preset != null ? preset.antiAliasingLevel.ToString() : "",
                preset != null ? preset.resolutionWidth.ToString() : "",
                preset != null ? preset.resolutionHeight.ToString() : "",
                c != null ? c.activeGameObjects.ToString() : "",
                c != null ? c.activeRenderers.ToString() : "",
                c != null ? c.meshRenderers.ToString() : "",
                c != null ? c.skinnedMeshRenderers.ToString() : "",
                c != null ? c.particleSystems.ToString() : "",
                c != null ? c.lights.ToString() : "",
                c != null ? c.realtimeLights.ToString() : "",
                c != null ? c.shadowCastingLights.ToString() : "",
                c != null ? c.cameras.ToString() : "",
                c != null ? c.canvases.ToString() : "",
                c != null ? c.rigidbodies.ToString() : "",
                c != null ? c.colliders.ToString() : "",
                c != null ? c.animators.ToString() : "",
                c != null ? c.uniqueMaterials.ToString() : "",
                c != null ? c.uniqueShaders.ToString() : "",
                c != null ? c.estimatedTriangleCount.ToString() : "",
                F(result.averageFrameTimeMs), F(result.medianFrameTimeMs), F(result.p90FrameTimeMs), F(result.p95FrameTimeMs),
                F(result.maxFrameTimeMs), F(result.averageFps), F(result.minFps), F(result.averageMemory), F(result.peakMemory), F(result.averageGcAlloc));
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private static string F(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unknown";
            var invalid = Path.GetInvalidFileNameChars();
            var chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (invalid.Contains(chars[i])) chars[i] = '_';
            }
            return new string(chars);
        }

        private static void WriteMarkdownSummary(string runOutputDir, IReadOnlyList<PerformanceTestResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Performance Test Run Summary");
            sb.AppendLine();
            sb.AppendLine($"Total captures: {results.Count}");
            sb.AppendLine($"Scenes tested: {string.Join(", ", results.Select(r => r.metadata.sceneName).Distinct())}");
            sb.AppendLine($"Quality presets tested: {string.Join(", ", results.Select(r => r.metadata.qualityPresetName).Distinct())}");
            sb.AppendLine();

            var byScene = results.GroupBy(r => r.metadata.sceneName).OrderBy(g => g.Key);
            sb.AppendLine("## Frame Time by Scene");
            foreach (var g in byScene)
            {
                sb.AppendLine($"- {g.Key}: avg {g.Average(x => x.averageFrameTimeMs):0.##} ms, p95 {g.Average(x => x.p95FrameTimeMs):0.##} ms");
            }

            sb.AppendLine();
            sb.AppendLine("## Worst Captures (Top 5 by P95)");
            foreach (var r in results.OrderByDescending(x => x.p95FrameTimeMs).Take(5))
            {
                sb.AppendLine($"- {r.metadata.sceneName} / {r.metadata.qualityPresetName} / {r.metadata.cameraPathId} / run {r.metadata.runIndex}: p95 {r.p95FrameTimeMs:0.##} ms");
            }

            var budget = Resources.FindObjectsOfTypeAll<PerformanceBudget>().FirstOrDefault();
            if (budget != null)
            {
                sb.AppendLine();
                sb.AppendLine("## Budget Exceedances");
                var exceeded = results.Where(r => r.averageFrameTimeMs > budget.frameBudgetMs).ToList();
                if (exceeded.Count == 0)
                {
                    sb.AppendLine("- None");
                }
                else
                {
                    foreach (var r in exceeded.Take(20))
                    {
                        sb.AppendLine($"- {r.metadata.sceneName} / {r.metadata.qualityPresetName} / {r.metadata.cameraPathId} avg {r.averageFrameTimeMs:0.##} ms > {budget.frameBudgetMs:0.##} ms");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("## Recommendations");
            var highTri = byScene.Where(g => g.Max(x => x.sceneCensusSummary?.estimatedTriangleCount ?? 0) > 500_000).Select(g => g.Key).ToList();
            if (highTri.Count > 0) sb.AppendLine($"- Reduce geometry complexity or improve LODs in: {string.Join(", ", highTri)}.");
            var highLights = byScene.Where(g => g.Max(x => x.sceneCensusSummary?.realtimeLights ?? 0) > 4).Select(g => g.Key).ToList();
            if (highLights.Count > 0) sb.AppendLine($"- Review realtime lighting and shadows in: {string.Join(", ", highLights)}.");
            if (highTri.Count == 0 && highLights.Count == 0) sb.AppendLine("- No obvious census-driven risks detected.");

            File.WriteAllText(Path.Combine(runOutputDir, "run_summary.md"), sb.ToString(), Encoding.UTF8);
        }

        private void LogStatus(string message)
        {
            Debug.Log($"[PerformanceIntelligence] {message}");
            OnStatus?.Invoke(message);
        }
    }
}
