using System;
using UnityEngine;

namespace PerformanceIntelligence
{
    /// <summary>
    /// ScriptableObject that stores per-platform performance targets.
    /// Create via: right-click in Project > Create > Performance Intelligence > Performance Budget.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PerformanceBudget_New",
        menuName  = "Performance Intelligence/Performance Budget")]
    public class PerformanceBudget : ScriptableObject
    {
        [Tooltip("Descriptive name for the target platform (e.g. 'iOS Mid-Range', 'PC High').")]
        public string platformName = "PC";

        [Tooltip("Target frames per second.")]
        public int targetFPS = 60;

        [Tooltip("Maximum allowed frame time in milliseconds (1000 / targetFPS).")]
        public float frameBudgetMs = 16.67f;

        [Tooltip("Maximum draw calls per frame.")]
        public int maxDrawCalls = 500;

        [Tooltip("Maximum render batches per frame.")]
        public int maxBatches = 300;

        [Tooltip("Maximum triangle count per frame.")]
        public int maxTriangles = 500_000;

        [Tooltip("Maximum total reserved memory in megabytes.")]
        public float maxMemoryMB = 512f;

        [Tooltip("Maximum GC allocation per frame in bytes. 0 = no allocation target.")]
        public float maxGCAllocBytesPerFrame = 4096f;

        [TextArea(2, 4)]
        [Tooltip("Optional notes about this budget profile.")]
        public string notes;

        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates a single <see cref="FrameCensus"/> against this budget.
        /// Metrics stored as -1 (unavailable) always pass — unknown values are not penalised.
        /// </summary>
        public BudgetEvaluation EvaluateFrame(FrameCensus frame)
        {
            float frameMs  = frame.deltaTime * 1000f;
            float memoryMB = frame.totalReservedMemory >= 0
                ? frame.totalReservedMemory / 1_048_576f
                : -1f;

            return new BudgetEvaluation
            {
                fpsPass       = frame.estimatedFPS < 0f || frame.estimatedFPS >= targetFPS,
                frameTimePass = frameMs <= frameBudgetMs,
                drawCallsPass = frame.drawCalls    < 0   || frame.drawCalls    <= maxDrawCalls,
                batchesPass   = frame.batches      < 0   || frame.batches      <= maxBatches,
                trianglesPass = frame.triangles    < 0   || frame.triangles    <= maxTriangles,
                memoryPass    = memoryMB           < 0f  || memoryMB           <= maxMemoryMB,
                gcAllocPass   = frame.gcAllocBytes < 0   || frame.gcAllocBytes <= maxGCAllocBytesPerFrame,
            };
        }

        // ──────────────────────────────────────────────────────────────────────

        /// <summary>Pass/fail result for every budget metric evaluated against one frame.</summary>
        [Serializable]
        public struct BudgetEvaluation
        {
            public bool fpsPass;
            public bool frameTimePass;
            public bool drawCallsPass;
            public bool batchesPass;
            public bool trianglesPass;
            public bool memoryPass;
            public bool gcAllocPass;

            /// <summary>True when every tracked metric is within budget.</summary>
            public bool AllPass =>
                fpsPass && frameTimePass && drawCallsPass && batchesPass &&
                trianglesPass && memoryPass && gcAllocPass;
        }
    }
}
