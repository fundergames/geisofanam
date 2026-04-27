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

using Funder.Core.Events;
using UnityEngine;

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

    /// <summary>
    /// Raised when the sword emits a soul resonance pulse in the soul realm.
    /// <see cref="Triggers.SoulPulseReceptorTrigger"/> subscribes to this.
    /// The future sword soul-pulse system raises this; use
    /// <see cref="Triggers.SoulPulseReceptorTrigger.RaisePulse"/> as the call site.
    /// </summary>
    public struct SoulPulseEvent : IEvent
    {
        /// <summary>World-space origin of the pulse.</summary>
        public Vector3 Origin;
        /// <summary>Radius of the pulse wave.</summary>
        public float Radius;
    }
}
