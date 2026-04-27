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

namespace Geis.SoulRealm
{
    /// <summary>
    /// Optional puzzle / interact scripts can check this before running physical-realm logic.
    /// Soul-only triggers should require <see cref="SoulRealmManager.IsSoulRealmActive"/>.
    /// </summary>
    public static class SoulRealmInteractable
    {
        /// <summary>True when the player should not use normal world interactions (combat, use, pickups).</summary>
        public static bool BlockPhysicalInteractions =>
            SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
    }
}
