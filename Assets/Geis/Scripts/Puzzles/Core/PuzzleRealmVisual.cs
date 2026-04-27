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

using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Realm-only presentation: tint, hide/show, or noise dissolve when entering/leaving Soul Realm.
    /// No triggers, outputs, or gameplay — use for props, set dressing, or barriers that should only exist in one realm.
    /// All options are on <see cref="PuzzleElementBase"/> (realm mode, dissolve, duration, overrides).
    /// </summary>
    [AddComponentMenu("Geis/Puzzles/Puzzle Realm Visual")]
    public sealed class PuzzleRealmVisual : PuzzleElementBase
    {
    }
}
