using Funder.Core.Events;

namespace Geis.Puzzles
{
    /// <summary>Raised when a PuzzleGroup's win condition is met for the first time (or each time if not oneShot).</summary>
    public struct PuzzleSolvedEvent : IEvent
    {
        public PuzzleGroup Group;
    }

    /// <summary>Raised when a PuzzleGroup resets (e.g. wrong sequence order or trigger deactivated).</summary>
    public struct PuzzleResetEvent : IEvent
    {
        public PuzzleGroup Group;
    }

    /// <summary>Raised whenever any individual trigger changes state.</summary>
    public struct PuzzleElementActivatedEvent : IEvent
    {
        public PuzzleTriggerBase Trigger;
        public bool Activated;
    }
}
