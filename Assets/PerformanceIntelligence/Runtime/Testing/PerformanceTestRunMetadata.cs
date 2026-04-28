using System;
using UnityEngine;

namespace PerformanceIntelligence.Testing
{
    [Serializable]
    public sealed class PerformanceTestRunMetadata
    {
        public string sessionId;
        public string sceneName;
        public string cameraPathId;
        public string qualityPresetName;
        public int runIndex;
        public string timestampUtc;
        public string unityVersion;
        public string platform;
        public string graphicsDeviceType;
        public string graphicsDeviceName;
        public string renderPipeline;
        public string resolution;
        public int targetFrameRate;
        public int vSyncCount;
        public float captureDuration;
        public float warmupDuration;
        public bool isEditor;
        public string notes;

        public static PerformanceTestRunMetadata Create(
            string sessionId,
            string sceneName,
            string cameraPathId,
            string qualityPresetName,
            int runIndex,
            float captureDuration,
            float warmupDuration,
            string notes)
        {
            return new PerformanceTestRunMetadata
            {
                sessionId = sessionId,
                sceneName = sceneName,
                cameraPathId = cameraPathId,
                qualityPresetName = qualityPresetName,
                runIndex = runIndex,
                timestampUtc = DateTime.UtcNow.ToString("o"),
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                renderPipeline = ResolveRenderPipelineName(),
                resolution = $"{Screen.width}x{Screen.height}",
                targetFrameRate = Application.targetFrameRate,
                vSyncCount = QualitySettings.vSyncCount,
                captureDuration = captureDuration,
                warmupDuration = warmupDuration,
                isEditor = Application.isEditor,
                notes = notes,
            };
        }

        private static string ResolveRenderPipelineName()
        {
            var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            return rp == null ? "Built-in" : rp.GetType().Name;
        }
    }
}
