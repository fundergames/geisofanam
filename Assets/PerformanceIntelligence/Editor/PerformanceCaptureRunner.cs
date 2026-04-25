using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PerformanceIntelligence.Editor
{
    /// <summary>
    /// Editor-side orchestrator that manages the lifetime of a <see cref="PerformanceCapture"/>
    /// MonoBehaviour in the scene. Not a MonoBehaviour itself — created and owned by
    /// <see cref="PerformanceIntelligenceWindow"/> as a plain C# object.
    ///
    /// Important: Frame capture requires PlayMode. Calling <see cref="StartCapture"/> outside
    /// PlayMode logs a warning and returns early.
    /// </summary>
    public sealed class PerformanceCaptureRunner : IDisposable
    {
        private const string TempGoName = "_PerformanceCaptureTemp";

        // ── Public state ───────────────────────────────────────────────────────
        public bool IsCapturing { get; private set; }
        public CaptureSession LastSession { get; private set; }

        /// <summary>Fired on the main thread when the capture session completes.</summary>
        public event Action OnCaptureComplete;

        // ── Private state ──────────────────────────────────────────────────────
        private PerformanceCapture _capture;
        private GameObject _captureGo;
        private bool _disposed;

        // ──────────────────────────────────────────────────────────────────────

        public PerformanceCaptureRunner()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a temporary capture GameObject in the scene and begins recording.
        /// No-op with a warning if not in PlayMode.
        /// </summary>
        public void StartCapture(float duration)
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[PerformanceIntelligence] Frame capture requires PlayMode. Enter Play Mode first.");
                return;
            }

            if (IsCapturing) return;

            _captureGo           = new GameObject(TempGoName);
            _captureGo.hideFlags = HideFlags.DontSave;
            UnityEngine.Object.DontDestroyOnLoad(_captureGo);

            _capture                 = _captureGo.AddComponent<PerformanceCapture>();
            _capture.captureDuration = duration;
            _capture.OnCaptureComplete += OnSessionComplete;
            _capture.StartCapture();

            IsCapturing = true;
        }

        /// <summary>Stops an in-progress capture early, preserving the data collected so far.</summary>
        public void StopCapture()
        {
            if (_capture != null && _capture.IsCapturing)
                _capture.StopCapture();
        }

        /// <summary>
        /// Writes the last session's JSON and CSV files into
        /// <c>&lt;directory&gt;/&lt;sessionId&gt;/capture.json|csv</c>
        /// then refreshes the AssetDatabase.
        /// </summary>
        public void ExportLastSession(string directory)
        {
            if (LastSession == null)
            {
                Debug.LogWarning("[PerformanceIntelligence] No session to export.");
                return;
            }

            string sessionDir = Path.Combine(directory, LastSession.sessionId);
            Directory.CreateDirectory(sessionDir);

            File.WriteAllText(Path.Combine(sessionDir, "capture.json"), LastSession.ToJson(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(sessionDir, "capture.csv"),  LastSession.ToCsv(),  Encoding.UTF8);

            AssetDatabase.Refresh();
            Debug.Log($"[PerformanceIntelligence] Session exported to {sessionDir}");
        }

        // ── IDisposable ────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Cleanup();
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode) return;

            if (IsCapturing) StopCapture();
            Cleanup();
        }

        private void OnSessionComplete(CaptureSession session)
        {
            LastSession = session;
            IsCapturing = false;
            Cleanup();
            OnCaptureComplete?.Invoke();
        }

        private void Cleanup()
        {
            if (_capture != null)
            {
                _capture.OnCaptureComplete -= OnSessionComplete;
                _capture = null;
            }

            if (_captureGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_captureGo);
                _captureGo = null;
            }
        }
    }
}
