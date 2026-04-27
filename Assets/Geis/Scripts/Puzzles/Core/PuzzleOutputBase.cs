/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

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
