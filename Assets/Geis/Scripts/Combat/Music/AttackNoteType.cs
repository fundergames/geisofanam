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

// Geis of Anam - Combat Music System
// Attack type for note mapping (Light, Heavy, Charged, Finisher).

namespace Geis.Combat.Music
{
    /// <summary>
    /// Attack type for mapping to musical notes.
    /// Light → short note, Heavy → emphasized, Charged → higher tension, Finisher → resolving root.
    /// </summary>
    public enum AttackNoteType
    {
        Light = 0,
        Heavy = 1,
        Charged = 2,
        Finisher = 3
    }
}
