/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using UnityEngine;

namespace Geis.Locomotion
{
    /// <summary>
    /// Maps world-space dodge direction to animator DodgeDirection index (0–3).
    /// </summary>
    public static class GeisDodgeDirectionUtility
    {
        /// <summary>
        /// Converts a world direction into a 4-way dodge index relative to <paramref name="bodyForward"/>
        /// and <paramref name="bodyRight"/> (both planar, normalized).
        /// 0 = forward, 1 = back, 2 = left, 3 = right.
        /// </summary>
        public static int WorldDirectionToIndex(Vector3 worldDirection, Vector3 bodyForward, Vector3 bodyRight)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
                return 1;

            worldDirection.Normalize();
            float lx = Vector3.Dot(worldDirection, bodyRight);
            float lz = Vector3.Dot(worldDirection, bodyForward);

            if (Mathf.Abs(lz) >= Mathf.Abs(lx))
                return lz >= 0f ? 0 : 1;

            return lx >= 0f ? 3 : 2;
        }
    }
}
