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
    /// Receives overlap counts from <see cref="SoulSwitchProximityRelay"/> for interact / prompt zones.
    /// </summary>
    public interface IPuzzleProximityRelayOwner
    {
        void OnProximityRelayEnter(bool interact, bool prompt);
        void OnProximityRelayExit(bool interact, bool prompt);
    }
}
