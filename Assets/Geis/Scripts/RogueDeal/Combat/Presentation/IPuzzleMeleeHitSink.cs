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

using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Puzzle volumes (e.g. sword-break zones) that should register hits from
    /// <see cref="SimpleAttackHitDetector"/> using the same overlap spheres as melee combat,
    /// without weapon colliders or <see cref="WeaponHitbox"/>.
    /// </summary>
    public interface IPuzzleMeleeHitSink
    {
        /// <param name="source">Null when the hit is forwarded from a soul-realm weapon ability (e.g. Emberblade wave).</param>
        void OnMeleeHitFromSimpleAttack(SimpleAttackHitDetector source, CombatAction action, int weaponSlotIndex, int hitWindowIndex);
    }
}
