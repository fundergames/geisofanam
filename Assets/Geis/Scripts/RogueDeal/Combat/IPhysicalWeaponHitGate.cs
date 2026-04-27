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

namespace RogueDeal.Combat
{
    /// <summary>
    /// Optional gate used by hit-detection / AoE combat to decide whether an entity
    /// should be eligible for physical weapon hit application right now.
    ///
    /// Implement on targets that are only hittable during specific windows (e.g. boss parts).
    /// </summary>
    public interface IPhysicalWeaponHitGate
    {
        /// <summary>
        /// Return true if this entity is currently allowed to receive physical weapon hits.
        /// </summary>
        bool AllowsPhysicalWeaponHits();
    }
}

