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

namespace RogueDeal.Combat
{
    /// <summary>
    /// Tracks which input device to use. Switches to keyboard/mouse or gamepad based on
    /// whichever last received input (so the player can switch devices at any time).
    /// </summary>
    public static class ActiveInputScheme
    {
        public static bool UsingGamepad { get; private set; }

        /// <summary>
        /// Call each frame with true if keyboard/mouse input was detected, and/or true if gamepad input was detected.
        /// Last device to have input wins.
        /// </summary>
        public static void Update(bool keyboardMouseInputDetected, bool gamepadInputDetected)
        {
            if (gamepadInputDetected)
                UsingGamepad = true;
            if (keyboardMouseInputDetected)
                UsingGamepad = false;
        }
    }
}
