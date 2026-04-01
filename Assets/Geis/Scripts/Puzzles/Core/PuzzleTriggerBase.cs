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
        [Header("Activation visuals (optional)")]
        [Tooltip("Enabled while this trigger is activated (e.g. lit rune, green lamp). Leave empty if unused.")]
        [SerializeField] private GameObject visualWhenActivated;
        [Tooltip("Enabled while this trigger is not activated (e.g. dim mesh). Leave empty if unused.")]
        [SerializeField] private GameObject visualWhenInactive;

        public bool IsActivated { get; private set; }

        public event Action<PuzzleTriggerBase> OnTriggerActivated;
        public event Action<PuzzleTriggerBase> OnTriggerDeactivated;

        private void Start()
        {
            ApplyActivationVisuals();
        }

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

            ApplyActivationVisuals();
        }

        /// <summary>
        /// Resets this trigger back to inactive without firing deactivated event.
        /// Used by PuzzleGroup when resetting a sequence silently.
        /// </summary>
        public virtual void ResetSilent()
        {
            IsActivated = false;
            ApplyActivationVisuals();
        }

        private void ApplyActivationVisuals()
        {
            if (visualWhenActivated != null)
                visualWhenActivated.SetActive(IsActivated);
            if (visualWhenInactive != null)
                visualWhenInactive.SetActive(!IsActivated);
        }

        /// <summary>
        /// Renderer to tint for sequence-step feedback. Matches whichever activation visual is currently active
        /// (avoids coloring a hidden mesh when inactive/activated are separate objects).
        /// </summary>
        public Renderer GetRendererForActivationTinting()
        {
            if (visualWhenActivated != null && visualWhenActivated.activeSelf)
                return visualWhenActivated.GetComponentInChildren<Renderer>(true);
            if (visualWhenInactive != null && visualWhenInactive.activeSelf)
                return visualWhenInactive.GetComponentInChildren<Renderer>(true);
            if (visualWhenActivated != null)
                return visualWhenActivated.GetComponentInChildren<Renderer>(true);
            if (visualWhenInactive != null)
                return visualWhenInactive.GetComponentInChildren<Renderer>(true);
            return GetComponentInChildren<Renderer>(true);
        }
    }
}
