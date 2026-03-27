using Geis.Locomotion;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Waypoint path mover driven by <see cref="MovingPlatformOutput"/>.
    /// Hold plate: moves toward the last waypoint while active; optional stay at end vs return to start on release.
    /// Press-once: optional lock at end, loop between waypoints, or (ReturnWhenReleased) rest at end like lock.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PlatformMover : MonoBehaviour
    {
        public enum PlateInteractionMode { HoldRequired, PressOnce }

        /// <summary>
        /// Meaning depends on <see cref="PlateInteractionMode"/> — see tooltips on <see cref="MovingPlatformOutput"/>.
        /// </summary>
        public enum EndBehavior
        {
            /// <summary>Hold: stay at last waypoint after release. Press once: stop at last waypoint.</summary>
            LockAtEnd,
            /// <summary>Press once only: keep moving between first and last waypoint forever. Hold: same as ReturnWhenReleased (shuttle).</summary>
            PingPong,
            /// <summary>Hold: move while plate active; return to start when released (no lock at end). Press once: same as LockAtEnd.</summary>
            ReturnWhenReleased,
        }

        private enum PathPhase
        {
            IdleAtStart,
            MovingToEnd,
            IdleAtEnd,
            MovingToStart,
            PressOncePingPong,
        }

        [Header("Waypoints")]
        [Tooltip("World-space path. Index 0 = start/rest, last index = end.")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float speed = 2f;
        [SerializeField] private float arrivalThreshold = 0.02f;
        [Tooltip("How quickly the platform eases toward each waypoint (SmoothDamp). Lower = snappier.")]
        [SerializeField] private float positionSmoothTime = 0.12f;

        private Vector3 _smoothVelocity;

        private bool _legacyInvertMode;
        private PlateInteractionMode _plateMode = PlateInteractionMode.HoldRequired;
        private EndBehavior _endBehavior = EndBehavior.LockAtEnd;

        /// <summary>Legacy invert only: when false, ping-pong does not advance.</summary>
        private bool _legacyMotorRunning;

        private PathPhase _phase = PathPhase.IdleAtStart;

        /// <summary>Hold mode: player wants the platform at the end waypoint.</summary>
        private bool _driveForward;

        private int _atIndex;
        private int _goalIndex;
        private int _pingPongDir = -1;

        // Legacy invert (unsolved = ping-pong, solved = frozen)
        private int _legacyTarget = 1;
        private int _legacyDir = 1;

        private void Awake()
        {
            if (GetComponent<MovingGroundCarrier>() == null)
                gameObject.AddComponent<MovingGroundCarrier>();
        }

        public void Configure(bool legacyInvert, PlateInteractionMode plateMode, EndBehavior endBehavior)
        {
            _legacyInvertMode = legacyInvert;
            _plateMode = plateMode;
            _endBehavior = endBehavior;
        }

        public void ResetToStartPosition()
        {
            if (waypoints == null || waypoints.Length < 2 || waypoints[0] == null)
                return;

            transform.position = waypoints[0].position;
            _smoothVelocity = Vector3.zero;
            _atIndex = 0;
            _goalIndex = 1;
            _phase = PathPhase.IdleAtStart;
            _driveForward = false;
        }

        /// <summary>Legacy invert: when true, ping-pong is paused (plate active / "solved").</summary>
        public void SetLegacyPingPongFrozen(bool frozen)
        {
            if (!_legacyInvertMode)
                return;
            _legacyMotorRunning = !frozen;
            if (!frozen && waypoints != null && waypoints.Length >= 2 && waypoints[0] != null && gameObject.activeInHierarchy)
            {
                _smoothVelocity = Vector3.zero;
                transform.position = waypoints[0].position;
                _legacyTarget = 1;
                _legacyDir = 1;
            }
        }

        public void NotifyPlateActivated()
        {
            if (_legacyInvertMode)
                return;

            if (_plateMode == PlateInteractionMode.PressOnce)
            {
                if (_phase == PathPhase.IdleAtStart)
                    BeginMovingToEnd();
                return;
            }

            _driveForward = true;
            if (_phase == PathPhase.IdleAtStart)
                BeginMovingToEnd();
            else if (_phase == PathPhase.MovingToStart)
                BeginMovingToEndFromCurrentPosition();
        }

        public void NotifyPlateReleasedHold()
        {
            if (_legacyInvertMode)
                return;
            if (_plateMode != PlateInteractionMode.HoldRequired)
                return;

            if (_phase == PathPhase.IdleAtEnd && _endBehavior == EndBehavior.LockAtEnd)
                return;

            _driveForward = false;
            if (_phase == PathPhase.IdleAtEnd || _phase == PathPhase.MovingToEnd)
                BeginMovingToStartFromCurrentPosition();
        }

        public void NotifyPlateReleasedPressOnce()
        {
            // Release does not reverse movement in PressOnce mode.
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Length < 2)
                return;

            if (_legacyInvertMode)
            {
                if (!_legacyMotorRunning)
                    return;
                LegacyPingPongUpdate();
                return;
            }

            if (_phase == PathPhase.PressOncePingPong)
            {
                PressOncePingPongStep();
                return;
            }

            if (_phase == PathPhase.MovingToEnd)
                StepAlongPath(+1);
            else if (_phase == PathPhase.MovingToStart)
                StepAlongPath(-1);
        }

        private void StepAlongPath(int direction)
        {
            int last = waypoints.Length - 1;
            if (_goalIndex < 0 || _goalIndex > last || waypoints[_goalIndex] == null)
                return;

            Vector3 dest = waypoints[_goalIndex].position;
            transform.position = Vector3.SmoothDamp(
                transform.position, dest, ref _smoothVelocity, positionSmoothTime, speed, Time.deltaTime);

            if (Vector3.Distance(transform.position, dest) > arrivalThreshold)
                return;

            transform.position = dest;
            _smoothVelocity = Vector3.zero;
            _atIndex = _goalIndex;

            if (direction > 0)
                HandleForwardArrival(last);
            else
                HandleBackwardArrival();
        }

        private void HandleForwardArrival(int last)
        {
            if (_atIndex < last)
            {
                _goalIndex = _atIndex + 1;
                return;
            }

            // At end waypoint
            if (_plateMode == PlateInteractionMode.PressOnce)
            {
                if (_endBehavior == EndBehavior.PingPong)
                    BeginPressOncePingPongFromEnd();
                else
                    _phase = PathPhase.IdleAtEnd; // LockAtEnd or ReturnWhenReleased
                return;
            }

            // Hold: LockAtEnd keeps the platform at the last waypoint after release (see NotifyPlateReleasedHold).
            // ReturnWhenReleased / PingPong: idle at end while still held; release starts return via NotifyPlateReleasedHold.
            if (_endBehavior == EndBehavior.LockAtEnd)
                _phase = PathPhase.IdleAtEnd;
            else if (_driveForward)
                _phase = PathPhase.IdleAtEnd;
            else
                BeginMovingToStartFromCurrentPosition();
        }

        private void HandleBackwardArrival()
        {
            if (_atIndex > 0)
            {
                _goalIndex = _atIndex - 1;
                return;
            }

            _phase = PathPhase.IdleAtStart;
        }

        private void BeginMovingToEnd()
        {
            int last = waypoints.Length - 1;
            if (last < 1)
                return;

            _smoothVelocity = Vector3.zero;
            _phase = PathPhase.MovingToEnd;
            _atIndex = 0;
            transform.position = waypoints[0].position;
            _goalIndex = 1;
        }

        private void BeginMovingToEndFromCurrentPosition()
        {
            int last = waypoints.Length - 1;
            Vector3 p = transform.position;
            _smoothVelocity = Vector3.zero;

            for (int k = 0; k <= last; k++)
            {
                if (waypoints[k] == null)
                    continue;
                if (Vector3.Distance(p, waypoints[k].position) <= arrivalThreshold * 2f)
                {
                    if (k >= last)
                    {
                        transform.position = waypoints[last].position;
                        _phase = PathPhase.IdleAtEnd;
                        return;
                    }

                    _phase = PathPhase.MovingToEnd;
                    _goalIndex = k + 1;
                    return;
                }
            }

            int seg = GetClosestSegmentIndex(p);
            _phase = PathPhase.MovingToEnd;
            _goalIndex = seg + 1;
        }

        private void BeginMovingToStartFromCurrentPosition()
        {
            int last = waypoints.Length - 1;
            Vector3 p = transform.position;
            _smoothVelocity = Vector3.zero;

            for (int k = 0; k <= last; k++)
            {
                if (waypoints[k] == null)
                    continue;
                if (Vector3.Distance(p, waypoints[k].position) <= arrivalThreshold * 2f)
                {
                    if (k == 0)
                    {
                        _phase = PathPhase.IdleAtStart;
                        return;
                    }

                    _phase = PathPhase.MovingToStart;
                    _goalIndex = k - 1;
                    return;
                }
            }

            int seg = GetClosestSegmentIndex(p);
            _phase = PathPhase.MovingToStart;
            _goalIndex = seg;
        }

        /// <summary>Closest segment along the polyline; does not move the platform.</summary>
        private int GetClosestSegmentIndex(Vector3 p)
        {
            int last = waypoints.Length - 1;
            float best = float.MaxValue;
            int bestSeg = 0;

            for (int i = 0; i < last; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null)
                    continue;
                Vector3 a = waypoints[i].position;
                Vector3 b = waypoints[i + 1].position;
                Vector3 ab = b - a;
                float t = ab.sqrMagnitude < 1e-8f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
                Vector3 proj = a + ab * t;
                float d = Vector3.SqrMagnitude(p - proj);
                if (d < best)
                {
                    best = d;
                    bestSeg = i;
                }
            }

            return bestSeg;
        }

        private void BeginPressOncePingPongFromEnd()
        {
            int last = waypoints.Length - 1;
            if (last < 1)
                return;
            _smoothVelocity = Vector3.zero;
            _phase = PathPhase.PressOncePingPong;
            _pingPongDir = -1;
            _atIndex = last;
            _goalIndex = last - 1;
        }

        private void PressOncePingPongStep()
        {
            int last = waypoints.Length - 1;
            if (_goalIndex < 0 || _goalIndex > last || waypoints[_goalIndex] == null)
                return;

            Vector3 dest = waypoints[_goalIndex].position;
            transform.position = Vector3.SmoothDamp(
                transform.position, dest, ref _smoothVelocity, positionSmoothTime, speed, Time.deltaTime);

            if (Vector3.Distance(transform.position, dest) > arrivalThreshold)
                return;

            transform.position = dest;
            _smoothVelocity = Vector3.zero;
            _atIndex = _goalIndex;

            if (_atIndex == 0)
                _pingPongDir = 1;
            else if (_atIndex == last)
                _pingPongDir = -1;

            _goalIndex = Mathf.Clamp(_atIndex + _pingPongDir, 0, last);
        }

        private void LegacyPingPongUpdate()
        {
            int last = waypoints.Length - 1;
            if (waypoints[_legacyTarget] == null)
                return;

            Vector3 dest = waypoints[_legacyTarget].position;
            transform.position = Vector3.SmoothDamp(
                transform.position, dest, ref _smoothVelocity, positionSmoothTime, speed, Time.deltaTime);

            if (Vector3.Distance(transform.position, dest) > arrivalThreshold)
                return;

            transform.position = dest;
            _smoothVelocity = Vector3.zero;
            _legacyTarget += _legacyDir;
            if (_legacyTarget >= waypoints.Length || _legacyTarget < 0)
            {
                _legacyDir *= -1;
                _legacyTarget += _legacyDir * 2;
            }
        }
    }
}
