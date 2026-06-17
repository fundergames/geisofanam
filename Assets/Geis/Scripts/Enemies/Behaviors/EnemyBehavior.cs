/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using UnityEngine;

namespace Geis.Enemies
{
    /// <summary>
    /// Designer-authored pipeline step. Assign ordered assets on <see cref="EnemyAiDefinition.behaviorPipeline"/>.
    /// The first behavior that returns true from <see cref="TryExecute"/> owns the tick.
    /// </summary>
    public abstract class EnemyBehavior : ScriptableObject
    {
        [Tooltip("When false, this step is skipped (useful for toggling without removing from the list).")]
        [SerializeField] private bool enabled = true;

        public bool Enabled => enabled;

        /// <summary>
        /// Attempt to handle this frame. Return true if no later behaviors should run.
        /// </summary>
        public abstract bool TryExecute(EnemyBehaviorContext context);
    }
}
