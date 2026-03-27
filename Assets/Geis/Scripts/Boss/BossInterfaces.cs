namespace RogueDeal.Boss
{
    /// <summary>
    /// Contract for a self-contained boss phase. The phase drives its own logic and
    /// reports completion to GiantBossController, which transitions to the next phase.
    ///
    /// Phases receive the controller on Enter/Update/Exit so they can call back into it
    /// (e.g. to reset parts, drain souls, start coroutines) without needing a stored reference
    /// from construction time — keeping phases stateless-by-default and easy to unit-test.
    /// </summary>
    public interface IBossPhase
    {
        /// <summary>Called once when this phase becomes active.</summary>
        void OnEnter(GiantBossController boss);

        /// <summary>Called every frame while this phase is active.</summary>
        void OnUpdate(GiantBossController boss);

        /// <summary>Called once just before transitioning away from this phase.</summary>
        void OnExit(GiantBossController boss);

        /// <summary>True when the phase's win-condition is met and control should pass to the next phase.</summary>
        bool IsComplete { get; }
    }

    /// <summary>
    /// Contract for a discrete boss attack behaviour. Attacks are coroutine-driven so they can
    /// model multi-step sequences (windup → impact → recovery) without polling in Update.
    ///
    /// GiantBossController starts the coroutine and the attack implementation handles all timing
    /// and state changes for its particular move internally.
    /// </summary>
    public interface IBossAttack
    {
        /// <summary>Unique identifier used for logging and animation routing.</summary>
        string AttackId { get; }

        /// <summary>True while the coroutine returned by Execute is still running.</summary>
        bool IsExecuting { get; }

        /// <summary>Returns a coroutine that plays out the full attack sequence.</summary>
        System.Collections.IEnumerator Execute(GiantBossController boss);

        /// <summary>Immediately stops the attack mid-sequence (e.g. on phase transition or death).</summary>
        void Cancel();
    }
}
