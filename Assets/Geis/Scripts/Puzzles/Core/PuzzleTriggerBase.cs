using System;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Base class for all puzzle triggers. Subclasses call <see cref="SetActivated"/> to change
    /// state; this class fires events so <see cref="PuzzleGroup"/> can react.
    /// </summary>
    public abstract class PuzzleTriggerBase : PuzzleElementBase
    {
        public bool IsActivated { get; private set; }

        public event Action<PuzzleTriggerBase> OnTriggerActivated;
        public event Action<PuzzleTriggerBase> OnTriggerDeactivated;

        /// <summary>
        /// Called by subclasses when activation state changes. Fires the appropriate event.
        /// </summary>
        protected void SetActivated(bool value)
        {
            if (IsActivated == value)
                return;

            IsActivated = value;

            if (value)
                OnTriggerActivated?.Invoke(this);
            else
                OnTriggerDeactivated?.Invoke(this);
        }

        /// <summary>
        /// Resets this trigger back to inactive without firing deactivated event.
        /// Used by PuzzleGroup when resetting a sequence silently.
        /// </summary>
        public virtual void ResetSilent()
        {
            IsActivated = false;
        }
    }
}
