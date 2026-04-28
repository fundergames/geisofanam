using System;
using UnityEngine;

namespace PerformanceIntelligence.Testing
{
    [Serializable]
    public sealed class PerformanceTestResult
    {
        public PerformanceTestRunMetadata metadata;
        public float averageFps;
        public float averageFrameTimeMs;
        public float medianFrameTimeMs;
        public float p90FrameTimeMs;
        public float p95FrameTimeMs;
        public float maxFrameTimeMs;
        public float minFps;
        public float averageMemory;
        public float peakMemory;
        public float averageGcAlloc;
        public SceneCensus sceneCensusSummary;
        public string jsonOutputPath;
        public string csvOutputPath;
        public string summaryOutputPath;
    }
}
