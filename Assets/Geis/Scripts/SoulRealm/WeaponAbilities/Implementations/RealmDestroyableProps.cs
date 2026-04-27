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

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>Optional helper: destroy this GameObject when True Strike hits it (physical realm).</summary>
    [DisallowMultipleComponent]
    public sealed class TrueStrikeDestroyableProp : MonoBehaviour, ITrueStrikeDestroyable
    {
        public void DestroyFromTrueStrike()
        {
            Destroy(gameObject);
        }
    }

    /// <summary>Optional helper: destroy this GameObject when Wave Release hits it (soul realm).</summary>
    [DisallowMultipleComponent]
    public sealed class SoulWaveDestroyableProp : MonoBehaviour, ISoulRealmDestroyable
    {
        public void DestroyFromSoulWave()
        {
            Destroy(gameObject);
        }
    }
}
