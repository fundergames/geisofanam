using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace PerformanceIntelligence
{
    // ══════════════════════════════════════════════════════════════════════════
    // PerformanceCapture — MonoBehaviour
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Runtime component that records per-frame performance metrics using
    /// <see cref="ProfilerRecorder"/>. Add to any GameObject or let
    /// <c>PerformanceCaptureRunner</c> (Editor) create it automatically.
    ///
    /// Frame capture runs only while the Unity player loop is active (PlayMode).
    /// Scene census is captured once at <see cref="StartCapture"/>.
    /// </summary>
    public sealed class PerformanceCapture : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Tooltip("How many seconds to record before stopping automatically.")]
        public float captureDuration = 10f;

        // ── Public state ───────────────────────────────────────────────────────
        public bool IsCapturing { get; private set; }

        /// <summary>Fired on the main thread when capture ends (manually or by timeout).</summary>
        public event Action<CaptureSession> OnCaptureComplete;

        // ── ProfilerRecorders ──────────────────────────────────────────────────
        // Sampler name strings are case-sensitive and may vary between Unity versions.
        // IsValid is checked before reading LastValue; -1 is written for unavailable metrics.
        private ProfilerRecorder _mainThreadRec;    // "Main Thread"         Internal
        private ProfilerRecorder _renderThreadRec;  // "Render Thread"       Internal
        private ProfilerRecorder _gpuFrameRec;      // "GPU Frame Time"      Render
        private ProfilerRecorder _drawCallsRec;     // "Draw Calls Count"    Render
        private ProfilerRecorder _batchesRec;       // "Batches Count"       Render
        private ProfilerRecorder _setPassRec;       // "SetPass Calls Count" Render
        private ProfilerRecorder _trianglesRec;     // "Triangles Count"     Render
        private ProfilerRecorder _verticesRec;      // "Vertices Count"      Render
        private ProfilerRecorder _gcAllocRec;       // "GC.Alloc"            Memory

        // ── Private state ──────────────────────────────────────────────────────
        private CaptureSession _session;
        private float _timer;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _mainThreadRec   = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            _renderThreadRec = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread");
            _gpuFrameRec     = ProfilerRecorder.StartNew(ProfilerCategory.Render,   "GPU Frame Time");
            _drawCallsRec    = ProfilerRecorder.StartNew(ProfilerCategory.Render,   "Draw Calls Count");
            _batchesRec      = ProfilerRecorder.StartNew(ProfilerCategory.Render,   "Batches Count");
            _setPassRec      = ProfilerRecorder.StartNew(ProfilerCategory.Render,   "SetPass Calls Count");
            _trianglesRec    = ProfilerRecorder.StartNew(ProfilerCategory.Render,   "Triangles Count");
            _verticesRec     = ProfilerRecorder.StartNew(ProfilerCategory.Render,   "Vertices Count");
            _gcAllocRec      = ProfilerRecorder.StartNew(ProfilerCategory.Memory,   "GC.Alloc");
        }

        private void OnDisable()
        {
            if (_mainThreadRec.Valid)   _mainThreadRec.Dispose();
            if (_renderThreadRec.Valid) _renderThreadRec.Dispose();
            if (_gpuFrameRec.Valid)     _gpuFrameRec.Dispose();
            if (_drawCallsRec.Valid)    _drawCallsRec.Dispose();
            if (_batchesRec.Valid)      _batchesRec.Dispose();
            if (_setPassRec.Valid)      _setPassRec.Dispose();
            if (_trianglesRec.Valid)    _trianglesRec.Dispose();
            if (_verticesRec.Valid)     _verticesRec.Dispose();
            if (_gcAllocRec.Valid)      _gcAllocRec.Dispose();
        }

        private void LateUpdate()
        {
            if (!IsCapturing) return;

            _session.frames.Add(SampleFrame());

            _timer -= Time.deltaTime;
            if (_timer <= 0f) StopCapture();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Begins a new capture session. No-op if already capturing.</summary>
        public void StartCapture()
        {
            if (IsCapturing) return;

            _session = new CaptureSession
            {
                sessionId    = Guid.NewGuid().ToString(),
                platform     = Application.platform.ToString(),
                startTimeUtc = DateTime.UtcNow.ToString("o"),
                sceneCensus  = SceneCensus.Capture(),
                frames       = new List<FrameCensus>(),
            };

            _timer = captureDuration;
            IsCapturing = true;
        }

        /// <summary>Stops an in-progress capture and fires <see cref="OnCaptureComplete"/>.</summary>
        public void StopCapture()
        {
            if (!IsCapturing) return;

            IsCapturing = false;
            _session.endTimeUtc      = DateTime.UtcNow.ToString("o");
            _session.durationSeconds = captureDuration - Mathf.Max(_timer, 0f);
            OnCaptureComplete?.Invoke(_session);
        }

        /// <summary>Writes the last session as pretty-printed JSON to <paramref name="path"/>.</summary>
        public void ExportToJson(string path)
        {
            if (_session == null) return;
            File.WriteAllText(path, _session.ToJson(), Encoding.UTF8);
        }

        /// <summary>Writes the last session as a flat CSV to <paramref name="path"/>.</summary>
        public void ExportToCsv(string path)
        {
            if (_session == null) return;
            File.WriteAllText(path, _session.ToCsv(), Encoding.UTF8);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private FrameCensus SampleFrame()
        {
            float dt = Time.deltaTime;
            return new FrameCensus
            {
                timestamp    = Time.realtimeSinceStartupAsDouble,
                frameIndex   = Time.frameCount,
                deltaTime    = dt,
                estimatedFPS = dt > 0f ? 1f / dt : 0f,

                cpuMainThreadMs  = _mainThreadRec.Valid   ? _mainThreadRec.LastValue   / 1_000_000f : -1f,
                renderThreadMs   = _renderThreadRec.Valid ? _renderThreadRec.LastValue  / 1_000_000f : -1f,
                gpuFrameMs       = _gpuFrameRec.Valid     ? _gpuFrameRec.LastValue      / 1_000_000f : -1f,

                drawCalls   = _drawCallsRec.Valid ? (int)_drawCallsRec.LastValue : -1,
                batches     = _batchesRec.Valid   ? (int)_batchesRec.LastValue   : -1,
                setPassCalls = _setPassRec.Valid  ? (int)_setPassRec.LastValue   : -1,
                triangles   = _trianglesRec.Valid ? _trianglesRec.LastValue      : -1L,
                vertices    = _verticesRec.Valid  ? _verticesRec.LastValue       : -1L,

                gcAllocBytes        = _gcAllocRec.Valid ? _gcAllocRec.LastValue : -1L,
                totalReservedMemory = Profiler.GetTotalReservedMemoryLong(),
                monoHeapSize        = Profiler.GetMonoHeapSizeLong(),
                monoUsedSize        = Profiler.GetMonoUsedSizeLong(),
            };
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CaptureSession
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A complete capture run: metadata, scene census snapshot, and all sampled frames.
    /// </summary>
    [Serializable]
    public sealed class CaptureSession
    {
        public string sessionId;
        public string platform;

        // DateTime stored as ISO 8601 string because JsonUtility cannot serialize DateTime.
        public string startTimeUtc;
        public string endTimeUtc;

        public float durationSeconds;
        public SceneCensus sceneCensus;
        public List<FrameCensus> frames = new List<FrameCensus>();

        // ── Serialization ──────────────────────────────────────────────────────

        /// <summary>Returns a pretty-printed JSON representation of this session.</summary>
        public string ToJson() => JsonUtility.ToJson(this, prettyPrint: true);

        /// <summary>
        /// Returns a flat CSV representation — one row per frame — suitable for ML training.
        /// Unavailable metrics appear as -1 in the output.
        /// </summary>
        public string ToCsv()
        {
            const string Header =
                "sessionId,platform,sceneName,frameIndex,timestamp,deltaTime,estimatedFPS," +
                "cpuMainThreadMs,renderThreadMs,gpuFrameMs," +
                "drawCalls,batches,setPassCalls,triangles,vertices," +
                "gcAllocBytes,totalReservedMemory,monoHeapSize,monoUsedSize";

            string scene = sceneCensus?.sceneName ?? string.Empty;
            var sb = new StringBuilder();
            sb.AppendLine(Header);

            foreach (var f in frames)
            {
                sb.Append(sessionId).Append(',')
                  .Append(platform).Append(',')
                  .Append(scene).Append(',')
                  .Append(f.frameIndex).Append(',')
                  .Append(f.timestamp.ToString("F6")).Append(',')
                  .Append(f.deltaTime.ToString("F6")).Append(',')
                  .Append(f.estimatedFPS.ToString("F2")).Append(',')
                  .Append(f.cpuMainThreadMs.ToString("F4")).Append(',')
                  .Append(f.renderThreadMs.ToString("F4")).Append(',')
                  .Append(f.gpuFrameMs.ToString("F4")).Append(',')
                  .Append(f.drawCalls).Append(',')
                  .Append(f.batches).Append(',')
                  .Append(f.setPassCalls).Append(',')
                  .Append(f.triangles).Append(',')
                  .Append(f.vertices).Append(',')
                  .Append(f.gcAllocBytes).Append(',')
                  .Append(f.totalReservedMemory).Append(',')
                  .Append(f.monoHeapSize).Append(',')
                  .AppendLine(f.monoUsedSize.ToString());
            }

            return sb.ToString();
        }

        /// <summary>Computes aggregate statistics over all captured frames.</summary>
        public CaptureStats ComputeStats()
        {
            var stats = new CaptureStats();
            if (frames == null || frames.Count == 0) return stats;

            float sumFPS = 0f, sumFrameMs = 0f, sumMemMB = 0f, sumGC = 0f;
            float worstFrame = float.MinValue, peakMem = float.MinValue;
            int gcCount = 0;

            foreach (var f in frames)
            {
                sumFPS     += f.estimatedFPS;
                float fms   = f.deltaTime * 1000f;
                sumFrameMs += fms;
                if (fms > worstFrame) worstFrame = fms;

                float memMB = f.totalReservedMemory / 1_048_576f;
                sumMemMB += memMB;
                if (memMB > peakMem) peakMem = memMB;

                if (f.gcAllocBytes >= 0) { sumGC += f.gcAllocBytes; gcCount++; }
            }

            int n = frames.Count;
            stats.avgFPS           = sumFPS / n;
            stats.avgFrameMs       = sumFrameMs / n;
            stats.worstFrameMs     = worstFrame;
            stats.avgMemoryMB      = sumMemMB / n;
            stats.peakMemoryMB     = peakMem;
            stats.avgGCAllocBytes  = gcCount > 0 ? sumGC / gcCount : 0f;
            return stats;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CaptureStats
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Aggregate statistics computed from a completed <see cref="CaptureSession"/>.</summary>
    [Serializable]
    public sealed class CaptureStats
    {
        public float avgFPS;
        public float avgFrameMs;
        public float worstFrameMs;
        public float avgMemoryMB;
        public float peakMemoryMB;

        /// <summary>Average GC heap allocation per frame in bytes (frames with -1 excluded).</summary>
        public float avgGCAllocBytes;
    }
}
