using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Enables or disables a <see cref="PlatformMover"/> when the puzzle is solved/reset.
    /// </summary>
    public class MovingPlatformOutput : PuzzleOutputBase
    {
        [Header("Platform")]
        [SerializeField] private PlatformMover platformMover;
        [Tooltip("If true, solving the puzzle STOPS the platform rather than starting it.")]
        [SerializeField] private bool invertBehaviour = false;

        protected override void OnActivate()
        {
            if (platformMover != null)
                platformMover.enabled = !invertBehaviour;
        }

        protected override void OnDeactivate()
        {
            if (platformMover != null)
                platformMover.enabled = invertBehaviour;
        }
    }
}
