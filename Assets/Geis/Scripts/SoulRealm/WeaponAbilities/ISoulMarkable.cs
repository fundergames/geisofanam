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
    /// <summary>
    /// Enemies or props that can be tagged in the soul realm (Harp-Bow Soul Marking).
    /// </summary>
    public interface ISoulMarkable
    {
        Transform MarkTransform { get; }
        bool IsSoulMarked { get; }
        void ApplySoulMark();
        void ClearSoulMark();
    }
}
