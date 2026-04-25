using System;
using UnityEngine;

namespace PerformanceIntelligence
{
    // ══════════════════════════════════════════════════════════════════════════
    // PerformanceFeatureVector
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Flat numeric representation of a capture session for ML inference.
    /// The 24-element layout is fixed and order-stable — any trained model expects
    /// this exact dimension and column order.
    /// </summary>
    [Serializable]
    public sealed class PerformanceFeatureVector
    {
        public float[] values;
        public string[] featureNames;

        /// <summary>The fixed number of features in every vector produced by this schema.</summary>
        public const int FeatureCount = 24;

        /// <summary>
        /// Builds a <see cref="PerformanceFeatureVector"/> from a completed session.
        /// Frame-level values are averaged across all samples; scene values are taken directly.
        /// </summary>
        public static PerformanceFeatureVector FromSession(CaptureSession session)
        {
            var stats = session.ComputeStats();
            var sc    = session.sceneCensus ?? new SceneCensus();

            var names = new string[FeatureCount]
            {
                // 0–5: frame performance averages
                "avgFPS",
                "avgFrameMs",
                "worstFrameMs",
                "avgMemoryMB",
                "peakMemoryMB",
                "avgGCAllocBytes",

                // 6–21: scene composition counts
                "sceneActiveGameObjects",
                "sceneActiveRenderers",
                "sceneMeshRenderers",
                "sceneSkinnedMeshRenderers",
                "sceneParticleSystems",
                "sceneLights",
                "sceneRealtimeLights",
                "sceneShadowCastingLights",
                "sceneCameras",
                "sceneCanvases",
                "sceneRigidbodies",
                "sceneColliders",
                "sceneAnimators",
                "sceneUniqueMaterials",
                "sceneUniqueShaders",
                "sceneEstimatedTriangles",

                // 22–23: session metadata
                "sessionFrameCount",
                "sessionDurationSeconds",
            };

            var vals = new float[FeatureCount]
            {
                stats.avgFPS,
                stats.avgFrameMs,
                stats.worstFrameMs,
                stats.avgMemoryMB,
                stats.peakMemoryMB,
                stats.avgGCAllocBytes,

                sc.activeGameObjects,
                sc.activeRenderers,
                sc.meshRenderers,
                sc.skinnedMeshRenderers,
                sc.particleSystems,
                sc.lights,
                sc.realtimeLights,
                sc.shadowCastingLights,
                sc.cameras,
                sc.canvases,
                sc.rigidbodies,
                sc.colliders,
                sc.animators,
                sc.uniqueMaterials,
                sc.uniqueShaders,
                sc.estimatedTriangleCount,

                session.frames?.Count ?? 0,
                session.durationSeconds,
            };

            return new PerformanceFeatureVector { values = vals, featureNames = names };
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PerformancePrediction
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Output from an <see cref="IPerformancePredictor"/> model.</summary>
    [Serializable]
    public sealed class PerformancePrediction
    {
        /// <summary>Predicted average FPS for the given feature vector.</summary>
        public float predictedFPS;

        /// <summary>Model confidence in [0, 1]. -1 if the model does not report confidence.</summary>
        public float confidence;

        /// <summary>Identifier of the model that produced this prediction.</summary>
        public string modelId;

        /// <summary>Human-readable warnings or caveats from the model (may be empty).</summary>
        public string[] warnings;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IPerformancePredictor
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Contract for pluggable ML inference backends (Unity Sentis, ONNX, heuristic).
    /// Implement this interface to connect a trained model without changing any call sites.
    ///
    /// Future backends to implement:
    ///   - SentisPerformancePredictor  (Unity Sentis / ONNX Runtime for Unity)
    ///   - OnnxPerformancePredictor    (direct ONNX via native plugin)
    ///   - HeuristicPerformancePredictor (rule-based fallback, no model file)
    /// </summary>
    public interface IPerformancePredictor
    {
        /// <summary>False when the backend is not initialised or no model file is loaded.</summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Runs inference on <paramref name="features"/> and returns a prediction.
        /// Returns null if <see cref="IsAvailable"/> is false.
        /// </summary>
        PerformancePrediction Predict(PerformanceFeatureVector features);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // NullPerformancePredictor — Null Object / stub
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Placeholder that satisfies <see cref="IPerformancePredictor"/> with no-op behaviour.
    /// Used as the default until a real backend (Sentis/ONNX) is wired in.
    /// </summary>
    public sealed class NullPerformancePredictor : IPerformancePredictor
    {
        public bool IsAvailable => false;
        public PerformancePrediction Predict(PerformanceFeatureVector features) => null;
    }
}
