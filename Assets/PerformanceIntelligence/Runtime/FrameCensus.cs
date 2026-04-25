using System;
using UnityEngine;

namespace PerformanceIntelligence
{
    /// <summary>
    /// Per-frame performance snapshot. Unavailable metrics are stored as -1
    /// so callers can distinguish "zero" from "measurement not supported on this platform."
    /// </summary>
    [Serializable]
    public sealed class FrameCensus
    {
        // ── Core timing ────────────────────────────────────────────────────────
        /// <summary>Time.realtimeSinceStartupAsDouble at the moment of capture.</summary>
        public double timestamp;

        /// <summary>Time.frameCount at the moment of capture.</summary>
        public int frameIndex;

        /// <summary>Time.deltaTime for this frame.</summary>
        public float deltaTime;

        /// <summary>1 / deltaTime, clamped to avoid divide-by-zero. 0 if deltaTime is 0.</summary>
        public float estimatedFPS;

        // ── CPU / GPU timing (nanoseconds → milliseconds via ProfilerRecorder) ─
        /// <summary>
        /// Main thread CPU time in milliseconds.
        /// Source: ProfilerCategory.Internal "Main Thread".
        /// -1 if the sampler is unavailable on this platform or Unity version.
        /// </summary>
        public float cpuMainThreadMs;

        /// <summary>
        /// Render thread CPU time in milliseconds.
        /// Source: ProfilerCategory.Internal "Render Thread".
        /// -1 if unavailable (single-threaded rendering or sampler absent).
        /// </summary>
        public float renderThreadMs;

        /// <summary>
        /// GPU frame time in milliseconds.
        /// Source: ProfilerCategory.Render "GPU Frame Time".
        /// -1 if unavailable (requires GPU profiling support; not available on all APIs).
        /// </summary>
        public float gpuFrameMs;

        // ── Draw stats ─────────────────────────────────────────────────────────
        /// <summary>
        /// Number of draw calls issued this frame.
        /// Source: ProfilerCategory.Render "Draw Calls Count". -1 if unavailable.
        /// </summary>
        public int drawCalls;

        /// <summary>
        /// Number of render batches this frame.
        /// Source: ProfilerCategory.Render "Batches Count". -1 if unavailable.
        /// </summary>
        public int batches;

        /// <summary>
        /// Number of SetPass calls this frame.
        /// Source: ProfilerCategory.Render "SetPass Calls Count". -1 if unavailable.
        /// </summary>
        public int setPassCalls;

        /// <summary>
        /// Triangle count rendered this frame.
        /// Source: ProfilerCategory.Render "Triangles Count". -1 if unavailable.
        /// </summary>
        public long triangles;

        /// <summary>
        /// Vertex count rendered this frame.
        /// Source: ProfilerCategory.Render "Vertices Count". -1 if unavailable.
        /// </summary>
        public long vertices;

        // ── Memory ─────────────────────────────────────────────────────────────
        /// <summary>
        /// GC heap allocation for this frame in bytes.
        /// Source: ProfilerCategory.Memory "GC.Alloc". -1 if unavailable.
        /// </summary>
        public long gcAllocBytes;

        /// <summary>Total reserved native memory in bytes (Profiler.GetTotalReservedMemoryLong).</summary>
        public long totalReservedMemory;

        /// <summary>Mono/IL2CPP managed heap size in bytes (Profiler.GetMonoHeapSizeLong).</summary>
        public long monoHeapSize;

        /// <summary>Mono/IL2CPP managed memory in use in bytes (Profiler.GetMonoUsedSizeLong).</summary>
        public long monoUsedSize;
    }
}
