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

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Optional per-frame tick for secondary ability while the SoulRealmWeapon map is enabled (e.g. hold-to-pull in the physical realm).
    /// </summary>
    public interface ISoulWeaponSecondaryHoldTick
    {
        void TickSecondaryWhileAbilityMapEnabled(in SoulWeaponAbilityContext context, bool ability2Held);
    }
}
