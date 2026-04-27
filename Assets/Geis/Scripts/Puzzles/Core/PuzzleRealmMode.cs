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

namespace Geis.Puzzles
{
    /// <summary>
    /// Defines which realm a puzzle element is accessible in.
    /// </summary>
    public enum PuzzleRealmMode
    {
        /// <summary>Only interactable/active while the soul realm is active.</summary>
        SoulOnly,

        /// <summary>Only interactable/active in the regular physical world.</summary>
        PhysicalOnly,

        /// <summary>Accessible in both realms.</summary>
        BothRealms,
    }
}
