using System.Collections.Generic;
using UnityEngine;

namespace PerformanceIntelligence.Testing
{
    [CreateAssetMenu(
        fileName = "PerformanceTestConfig",
        menuName = "Performance Intelligence/Testing/Performance Test Config")]
    public sealed class PerformanceTestConfig : ScriptableObject
    {
        public List<SceneTestDefinition> sceneTests = new List<SceneTestDefinition>();
        public List<QualityPresetDefinition> qualityPresets = new List<QualityPresetDefinition>();

        [Min(1)] public int runsPerConfiguration = 3;
        [Min(0f)] public float warmupDurationSeconds = 2f;
        [Min(0.1f)] public float captureDurationSeconds = 10f;

        [Tooltip("Relative to project root or absolute path.")]
        public string outputFolder = "Assets/PerformanceIntelligence/Data/Captures";

        [Tooltip("If enabled, editor window can start/drive runs in Play Mode.")]
        public bool runInEditorPlayMode = true;

        public bool exportCsv = true;
        public bool exportJson = true;
        public bool generateMarkdownReport = true;
    }
}
