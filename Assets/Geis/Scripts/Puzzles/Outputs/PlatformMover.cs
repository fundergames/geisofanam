using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Simple waypoint-based platform mover. Enable/disable this component to start/stop.
    /// Used by <see cref="MovingPlatformOutput"/>.
    /// </summary>
    public class PlatformMover : MonoBehaviour
    {
        public enum LoopMode { PingPong, Loop }

        [Header("Waypoints")]
        [Tooltip("World-space waypoints the platform moves through. At least 2 required.")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float       speed     = 2f;
        [SerializeField] private LoopMode    loopMode  = LoopMode.PingPong;

        private int   _targetIndex = 1;
        private int   _direction   = 1;

        private void OnEnable()
        {
            // Snap to first waypoint if not yet there
            if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
                transform.position = waypoints[0].position;
            _targetIndex = 1;
            _direction   = 1;
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Length < 2) return;

            var target = waypoints[_targetIndex];
            if (target == null) return;

            transform.position = Vector3.MoveTowards(
                transform.position, target.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.01f)
                AdvanceWaypoint();
        }

        private void AdvanceWaypoint()
        {
            if (loopMode == LoopMode.PingPong)
            {
                _targetIndex += _direction;
                if (_targetIndex >= waypoints.Length || _targetIndex < 0)
                {
                    _direction   *= -1;
                    _targetIndex += _direction * 2;
                }
            }
            else
            {
                _targetIndex = (_targetIndex + 1) % waypoints.Length;
            }
        }
    }
}
