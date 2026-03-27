using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Drives a <see cref="PlatformMover"/> from puzzle solve/reset. Plate interaction modes match
    /// <see cref="PlatformMover.PlateInteractionMode"/> / <see cref="PlatformMover.EndBehavior"/>.
    /// </summary>
    public class MovingPlatformOutput : PuzzleOutputBase
    {
        [Header("Platform")]
        [SerializeField] private PlatformMover platformMover;

        [Header("Invert (legacy)")]
        [Tooltip("If true: plate active STOPS ping-pong; plate released runs ping-pong (unsolved). Ignores hold/press-once path modes.")]
        [SerializeField] private bool invertBehaviour = false;

        [Header("Movement (when invert is off)")]
        [Tooltip("Hold: plate must stay active to ride (puzzle solved while standing on it). Press once: one activation runs the full trip without staying on the plate.")]
        [SerializeField] private PlatformMover.PlateInteractionMode plateInteraction = PlatformMover.PlateInteractionMode.HoldRequired;

        [Tooltip(
            "Hold — Lock at end: stays at last waypoint after you step off. Return when released: moves while plate is active, returns to start when not (shuttle; no ping-pong). Ping-pong: same as Return when released.\n" +
            "Press once — Lock at end: stops at last waypoint. Ping-pong: loops between waypoints forever. Return when released: same as lock at end.")]
        [SerializeField] private PlatformMover.EndBehavior endMovement = PlatformMover.EndBehavior.LockAtEnd;

        private void Awake()
        {
            if (platformMover != null)
                platformMover.Configure(invertBehaviour, plateInteraction, endMovement);
            ApplyUnsolvedMoverState();
        }

        protected override void OnActivate()
        {
            if (platformMover == null)
                return;

            if (invertBehaviour)
            {
                platformMover.SetLegacyPingPongFrozen(true);
                return;
            }

            platformMover.NotifyPlateActivated();
        }

        protected override void OnDeactivate()
        {
            if (platformMover == null)
                return;

            if (invertBehaviour)
            {
                platformMover.SetLegacyPingPongFrozen(false);
                return;
            }

            if (plateInteraction == PlatformMover.PlateInteractionMode.PressOnce)
                platformMover.NotifyPlateReleasedPressOnce();
            else
                platformMover.NotifyPlateReleasedHold();
        }

        /// <summary>
        /// Matches unsolved state: invert = ping-pong when off plate; otherwise idle at first waypoint.
        /// </summary>
        private void ApplyUnsolvedMoverState()
        {
            if (platformMover == null)
                return;

            if (invertBehaviour)
            {
                platformMover.SetLegacyPingPongFrozen(false);
                return;
            }

            platformMover.ResetToStartPosition();
        }
    }
}
