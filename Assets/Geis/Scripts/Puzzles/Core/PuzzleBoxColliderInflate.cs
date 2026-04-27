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

namespace Geis.Puzzles
{
    /// <summary>
    /// One-time capture + additive inflate for thin puzzle BoxColliders (pressure plates, sword hit zones).
    /// </summary>
    public static class PuzzleBoxColliderInflate
    {
        public static void ApplyIfNeeded(
            Collider col,
            bool inflateEnabled,
            Vector3 inflate,
            ref bool baseCaptured,
            ref Vector3 storedBaseSize,
            ref Vector3 storedBaseCenter)
        {
            if (!inflateEnabled || inflate.sqrMagnitude < 1e-8f)
                return;
            if (col is not BoxCollider box)
                return;

            if (!baseCaptured)
            {
                storedBaseSize = box.size;
                storedBaseCenter = box.center;
                baseCaptured = true;
            }

            box.size = storedBaseSize + inflate;
            box.center = storedBaseCenter + new Vector3(0f, inflate.y * 0.5f, 0f);
        }
    }
}
