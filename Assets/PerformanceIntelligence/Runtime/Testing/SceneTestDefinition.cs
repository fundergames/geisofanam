using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PerformanceIntelligence.Testing
{
    [Serializable]
    public sealed class SceneTestDefinition
    {
        public bool enabled = true;
        public string sceneName;
        public string scenePath;
        public List<CameraPathDefinition> cameraPaths = new List<CameraPathDefinition>();
        [TextArea(2, 4)] public string notes;

#if UNITY_EDITOR
        public SceneAsset sceneAsset;
#endif

        public bool IsValid => enabled && !string.IsNullOrWhiteSpace(sceneName);

        public string GetSceneIdentifier()
        {
            return !string.IsNullOrWhiteSpace(sceneName) ? sceneName : scenePath;
        }
    }
}
