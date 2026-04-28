using System;
using System.Collections.Generic;
using UnityEngine;

namespace PerformanceIntelligence.Testing
{
    public enum CameraPathInterpolationType
    {
        Linear = 0,
        SmoothStep = 1,
    }

    [Serializable]
    public struct CameraPathWaypoint
    {
        public Vector3 position;
        public Quaternion rotation;
        [Range(0f, 1f)] public float normalizedTime;
    }

    [CreateAssetMenu(
        fileName = "CameraPath",
        menuName = "Performance Intelligence/Testing/Camera Path Definition")]
    public sealed class CameraPathDefinition : ScriptableObject
    {
        public string pathId = "Path_A";
        public List<CameraPathWaypoint> waypoints = new List<CameraPathWaypoint>();
        [Min(0.1f)] public float playbackDuration = 10f;
        public bool loop = false;
        public CameraPathInterpolationType interpolation = CameraPathInterpolationType.Linear;
        public bool overrideFieldOfView = false;
        [Min(1f)] public float fieldOfView = 60f;
        [TextArea(2, 4)] public string notes;

        public bool HasUsableWaypoints => waypoints != null && waypoints.Count > 0;
    }
}
