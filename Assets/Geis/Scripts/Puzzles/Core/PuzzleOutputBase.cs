using System.Collections;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Base class for all puzzle outputs (doors, platforms, barriers, etc.).
    /// PuzzleGroup calls <see cref="Activate"/> and <see cref="Deactivate"/>.
    /// </summary>
    public abstract class PuzzleOutputBase : PuzzleElementBase
    {
        [Header("Output Timing")]
        [Tooltip("Seconds to wait before actually triggering after the group solves.")]
        [SerializeField] protected float activationDelay = 0f;

        public bool IsActive { get; private set; }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            if (activationDelay > 0f)
                StartCoroutine(DelayedActivate());
            else
                OnActivate();
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            StopAllCoroutines();
            OnDeactivate();
        }

        protected abstract void OnActivate();
        protected abstract void OnDeactivate();

        private IEnumerator DelayedActivate()
        {
            yield return new WaitForSeconds(activationDelay);
            OnActivate();
        }
    }
}
