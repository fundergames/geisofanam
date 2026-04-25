using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PerformanceIntelligence.Editor
{
    /// <summary>
    /// Main editor window for the Performance Intelligence package.
    /// Open via: Window > Performance Intelligence.
    ///
    /// Requires PlayMode for frame-level capture. Scene census and report generation
    /// are available in edit mode.
    /// </summary>
    public sealed class PerformanceIntelligenceWindow : EditorWindow
    {
        // ── Constants ──────────────────────────────────────────────────────────
        private const string CaptureDirectory = "Assets/PerformanceIntelligence/Data/Captures";
        private const string ReportsDirectory  = "Assets/PerformanceIntelligence/Data/Reports";

        private static readonly List<string> PlatformChoices = new List<string>
            { "Mobile", "PC", "Console", "Custom" };

        // ── UI element references ──────────────────────────────────────────────
        private DropdownField _platformDropdown;
        private ObjectField   _budgetField;
        private FloatField    _durationField;
        private ProgressBar   _captureProgress;
        private Label         _playModeWarning;
        private Button        _startButton;
        private Button        _stopButton;
        private Label         _statusLabel;
        private Button        _censusButton;
        private Label         _censusLabel;
        private Label         _summaryLabel;
        private Button        _exportButton;
        private Button        _reportButton;

        // ── State ──────────────────────────────────────────────────────────────
        private PerformanceCaptureRunner _runner;
        private SceneCensus _lastCensus;

        private float  _captureDurationCache;
        private float  _captureElapsed;
        private double _lastUpdateTime;

        // ──────────────────────────────────────────────────────────────────────

        [MenuItem("Window/Performance Intelligence")]
        public static void ShowWindow()
        {
            var window = GetWindow<PerformanceIntelligenceWindow>("Performance Intelligence");
            window.minSize = new Vector2(380f, 620f);
        }

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            _runner = new PerformanceCaptureRunner();
            _runner.OnCaptureComplete += OnRunnerCaptureComplete;

            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            if (_runner != null)
            {
                _runner.OnCaptureComplete -= OnRunnerCaptureComplete;
                _runner.Dispose();
                _runner = null;
            }

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        // ── UI Toolkit entry point ─────────────────────────────────────────────

        /// <summary>Called by Unity to build the UI. Uses UI Toolkit (not IMGUI).</summary>
        private void CreateGUI()
        {
            var scroll    = new ScrollView();
            scroll.style.flexGrow = 1;

            var container = new VisualElement();
            container.style.paddingTop    = 8;
            container.style.paddingBottom = 8;
            container.style.paddingLeft   = 8;
            container.style.paddingRight  = 8;

            // Title
            var title = new Label("Performance Intelligence");
            title.style.fontSize   = 18;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 10;
            container.Add(title);

            container.Add(BuildPlatformBudgetSection());
            container.Add(BuildCaptureSettingsSection());
            container.Add(BuildCaptureControlsSection());
            container.Add(BuildSceneCensusSection());
            container.Add(BuildExportReportsSection());

            scroll.Add(container);
            rootVisualElement.Add(scroll);

            RefreshPlayModeWarning();
            RefreshCaptureButtons();
        }

        // ── Section builders ───────────────────────────────────────────────────

        private VisualElement BuildPlatformBudgetSection()
        {
            var fold = new Foldout { text = "Platform & Budget", value = true };

            _platformDropdown = new DropdownField("Platform", PlatformChoices, 0);
            fold.Add(_platformDropdown);

            _budgetField = new ObjectField("Budget Asset")
            {
                objectType = typeof(PerformanceBudget),
                allowSceneObjects = false,
            };
            fold.Add(_budgetField);

            return fold;
        }

        private VisualElement BuildCaptureSettingsSection()
        {
            var fold = new Foldout { text = "Capture Settings", value = true };

            _durationField = new FloatField("Duration (s)") { value = 10f };
            fold.Add(_durationField);

            var progressLabel = new Label("Capture Progress");
            progressLabel.style.marginTop = 4;
            fold.Add(progressLabel);

            _captureProgress = new ProgressBar { lowValue = 0f, highValue = 1f, value = 0f };
            _captureProgress.style.marginBottom = 4;
            fold.Add(_captureProgress);

            return fold;
        }

        private VisualElement BuildCaptureControlsSection()
        {
            var fold = new Foldout { text = "Capture Controls", value = true };

            _playModeWarning = new Label("PlayMode required for frame capture.");
            _playModeWarning.style.color       = new StyleColor(new Color(1f, 0.75f, 0f));
            _playModeWarning.style.marginBottom = 4;
            fold.Add(_playModeWarning);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom  = 4;

            _startButton = new Button(OnStartCapture) { text = "Start Capture" };
            _startButton.style.flexGrow = 1;
            row.Add(_startButton);

            _stopButton = new Button(OnStopCapture) { text = "Stop Capture" };
            _stopButton.style.flexGrow = 1;
            _stopButton.SetEnabled(false);
            row.Add(_stopButton);

            fold.Add(row);

            _statusLabel = new Label("Ready");
            _statusLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            fold.Add(_statusLabel);

            return fold;
        }

        private VisualElement BuildSceneCensusSection()
        {
            var fold = new Foldout { text = "Scene Census", value = false };

            _censusButton = new Button(OnRunSceneCensus) { text = "Run Scene Census" };
            fold.Add(_censusButton);

            _censusLabel = new Label(string.Empty);
            _censusLabel.style.whiteSpace    = WhiteSpace.Normal;
            _censusLabel.style.marginTop     = 4;
            fold.Add(_censusLabel);

            return fold;
        }

        private VisualElement BuildExportReportsSection()
        {
            var fold = new Foldout { text = "Export & Reports", value = false };

            _summaryLabel = new Label(string.Empty);
            _summaryLabel.style.whiteSpace  = WhiteSpace.Normal;
            _summaryLabel.style.marginBottom = 6;
            _summaryLabel.style.display     = DisplayStyle.None;
            fold.Add(_summaryLabel);

            _exportButton = new Button(OnExportCapture) { text = "Export Latest Capture" };
            _exportButton.SetEnabled(false);
            fold.Add(_exportButton);

            _reportButton = new Button(OnGenerateReport) { text = "Generate Report" };
            _reportButton.SetEnabled(false);
            fold.Add(_reportButton);

            return fold;
        }

        // ── Update loop ────────────────────────────────────────────────────────

        private void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            float  dt  = (float)(now - _lastUpdateTime);
            _lastUpdateTime = now;

            if (_runner == null || !_runner.IsCapturing) return;

            _captureElapsed += dt;
            float t = _captureDurationCache > 0f
                ? Mathf.Clamp01(_captureElapsed / _captureDurationCache)
                : 0f;

            if (_captureProgress != null) _captureProgress.value = t;
            if (_statusLabel != null)
                _statusLabel.text = $"Capturing… {_captureElapsed:F1}s / {_captureDurationCache:F1}s";

            Repaint();
        }

        // ── Button callbacks ───────────────────────────────────────────────────

        private void OnStartCapture()
        {
            if (!EditorApplication.isPlaying)
            {
                _statusLabel.text = "Error: Enter PlayMode first.";
                return;
            }

            _captureDurationCache = Mathf.Max(_durationField.value, 0.1f);
            _captureElapsed       = 0f;
            _lastUpdateTime       = EditorApplication.timeSinceStartup;

            _runner.StartCapture(_captureDurationCache);

            _startButton.SetEnabled(false);
            _stopButton.SetEnabled(true);
            _statusLabel.text = "Capture started…";
        }

        private void OnStopCapture()
        {
            _runner.StopCapture();
            _statusLabel.text = "Stopping…";
        }

        private void OnRunSceneCensus()
        {
            _lastCensus       = SceneCensus.Capture();
            _censusLabel.text = FormatCensus(_lastCensus);
        }

        private void OnExportCapture()
        {
            _runner.ExportLastSession(CaptureDirectory);
            if (_runner.LastSession != null)
                _statusLabel.text = $"Exported → {CaptureDirectory}/{_runner.LastSession.sessionId}";
        }

        private void OnGenerateReport()
        {
            if (_runner.LastSession == null) return;
            var budget = _budgetField?.value as PerformanceBudget;
            string md  = PerformanceReportGenerator.GenerateMarkdown(_runner.LastSession, budget);
            PerformanceReportGenerator.SaveReport(md, ReportsDirectory, _runner.LastSession.sessionId);
            _statusLabel.text = $"Report → {ReportsDirectory}/report_{_runner.LastSession.sessionId}.md";
        }

        // ── Runner event handler ───────────────────────────────────────────────

        private void OnRunnerCaptureComplete()
        {
            _startButton.SetEnabled(true);
            _stopButton.SetEnabled(false);

            int frameCount = _runner.LastSession?.frames?.Count ?? 0;
            _statusLabel.text = $"Capture complete — {frameCount} frames recorded.";

            if (_captureProgress != null) _captureProgress.value = 1f;

            if (_runner.LastSession != null)
            {
                var stats = _runner.LastSession.ComputeStats();
                _summaryLabel.text =
                    $"Avg FPS: {stats.avgFPS:F1}  |  " +
                    $"Avg Frame: {stats.avgFrameMs:F2} ms  |  " +
                    $"Worst Frame: {stats.worstFrameMs:F2} ms  |  " +
                    $"Peak Mem: {stats.peakMemoryMB:F1} MB";
                _summaryLabel.style.display = DisplayStyle.Flex;
                _exportButton.SetEnabled(true);
                _reportButton.SetEnabled(true);
            }

            Repaint();
        }

        // ── PlayMode change handler ────────────────────────────────────────────

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            RefreshPlayModeWarning();
            RefreshCaptureButtons();

            if (state == PlayModeStateChange.ExitingPlayMode &&
                _runner != null && _runner.IsCapturing)
            {
                _startButton.SetEnabled(true);
                _stopButton.SetEnabled(false);
                _statusLabel.text = "PlayMode exited — capture was stopped.";
            }

            Repaint();
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void RefreshPlayModeWarning()
        {
            if (_playModeWarning == null) return;
            _playModeWarning.style.display = EditorApplication.isPlaying
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        private void RefreshCaptureButtons()
        {
            if (_startButton == null) return;
            bool canStart = EditorApplication.isPlaying && (_runner == null || !_runner.IsCapturing);
            _startButton.SetEnabled(canStart);
        }

        // ── Formatting ─────────────────────────────────────────────────────────

        private static string FormatCensus(SceneCensus sc)
        {
            return
                $"Scene: {sc.sceneName}\n" +
                $"GameObjects: {sc.activeGameObjects}  |  Renderers: {sc.activeRenderers}\n" +
                $"MeshRenderers: {sc.meshRenderers}  |  SkinnedMeshRenderers: {sc.skinnedMeshRenderers}\n" +
                $"ParticleSystems: {sc.particleSystems}  |  Animators: {sc.animators}\n" +
                $"Lights: {sc.lights} (Realtime: {sc.realtimeLights}, Shadow: {sc.shadowCastingLights})\n" +
                $"Cameras: {sc.cameras}  |  Canvases: {sc.canvases}\n" +
                $"Rigidbodies: {sc.rigidbodies}  |  Colliders: {sc.colliders}\n" +
                $"Materials: {sc.uniqueMaterials}  |  Shaders: {sc.uniqueShaders}\n" +
                $"Est. Triangles: {sc.estimatedTriangleCount:N0}";
        }
    }
}
