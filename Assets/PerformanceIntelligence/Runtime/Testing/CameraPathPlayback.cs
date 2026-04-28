using UnityEngine;

namespace PerformanceIntelligence.Testing
{
    [DisallowMultipleComponent]
    public sealed class CameraPathPlayback : MonoBehaviour
    {
        public CameraPathDefinition PathDefinition { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsComplete { get; private set; } = true;
        public float ElapsedSeconds { get; private set; }

        private Camera _targetCamera;
        private float _playbackDuration;

        public void Initialize(Camera targetCamera)
        {
            _targetCamera = targetCamera;
        }

        public void StartPlayback(CameraPathDefinition path, float durationOverrideSeconds = -1f)
        {
            PathDefinition = path;
            ElapsedSeconds = 0f;
            IsComplete = false;
            IsPlaying = path != null && path.HasUsableWaypoints;
            _playbackDuration = durationOverrideSeconds > 0f
                ? durationOverrideSeconds
                : (path != null ? Mathf.Max(0.1f, path.playbackDuration) : 0.1f);

            ApplyCurrentPose(0f);
        }

        public void ResetToStart()
        {
            ElapsedSeconds = 0f;
            IsComplete = false;
            IsPlaying = PathDefinition != null && PathDefinition.HasUsableWaypoints;
            ApplyCurrentPose(0f);
        }

        private void Update()
        {
            if (!IsPlaying || PathDefinition == null) return;

            ElapsedSeconds += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(ElapsedSeconds / Mathf.Max(0.0001f, _playbackDuration));
            ApplyCurrentPose(t);

            if (t >= 1f)
            {
                if (PathDefinition.loop)
                {
                    ElapsedSeconds = 0f;
                }
                else
                {
                    IsPlaying = false;
                    IsComplete = true;
                }
            }
        }

        private void ApplyCurrentPose(float normalizedTime)
        {
            if (PathDefinition == null || PathDefinition.waypoints == null || PathDefinition.waypoints.Count == 0) return;

            if (_targetCamera == null)
            {
                _targetCamera = GetComponent<Camera>();
                if (_targetCamera == null) _targetCamera = Camera.main;
                if (_targetCamera == null) return;
            }

            if (PathDefinition.waypoints.Count == 1)
            {
                var wp = PathDefinition.waypoints[0];
                _targetCamera.transform.SetPositionAndRotation(wp.position, wp.rotation);
                if (PathDefinition.overrideFieldOfView) _targetCamera.fieldOfView = PathDefinition.fieldOfView;
                return;
            }

            int segment = 0;
            for (int i = 0; i < PathDefinition.waypoints.Count - 1; i++)
            {
                if (normalizedTime >= PathDefinition.waypoints[i].normalizedTime &&
                    normalizedTime <= PathDefinition.waypoints[i + 1].normalizedTime)
                {
                    segment = i;
                    break;
                }
            }

            var a = PathDefinition.waypoints[segment];
            var b = PathDefinition.waypoints[Mathf.Min(segment + 1, PathDefinition.waypoints.Count - 1)];
            float denom = Mathf.Max(0.0001f, b.normalizedTime - a.normalizedTime);
            float t = Mathf.Clamp01((normalizedTime - a.normalizedTime) / denom);

            if (PathDefinition.interpolation == CameraPathInterpolationType.SmoothStep)
            {
                t = t * t * (3f - 2f * t);
            }

            Vector3 position = Vector3.LerpUnclamped(a.position, b.position, t);
            Quaternion rotation = Quaternion.SlerpUnclamped(a.rotation, b.rotation, t);
            _targetCamera.transform.SetPositionAndRotation(position, rotation);
            if (PathDefinition.overrideFieldOfView) _targetCamera.fieldOfView = PathDefinition.fieldOfView;
        }
    }
}
