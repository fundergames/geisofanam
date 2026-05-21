/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using UnityEngine;

namespace Geis.Combat
{
    /// <summary>
    /// Tags runtime weapon instances spawned by <see cref="GeisWeaponSwitcher"/> so bone searches skip equipped prefab hierarchies.
    /// </summary>
    internal sealed class GeisEquippedWeaponInstanceMarker : MonoBehaviour
    {
    }
}
