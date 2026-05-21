/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

namespace Geis.Locomotion
{
    /// <summary>
    /// Pure input-buffer timing helpers (Edit Mode testable).
    /// </summary>
    public static class GeisInputBufferUtility
    {
        /// <summary>
        /// True when <paramref name="bufferedAtUnscaled"/> is non-negative and within
        /// <paramref name="windowSeconds"/> of <paramref name="nowUnscaled"/>.
        /// </summary>
        public static bool IsFresh(float bufferedAtUnscaled, float windowSeconds, float nowUnscaled)
        {
            return bufferedAtUnscaled >= 0f
                && windowSeconds > 0f
                && (nowUnscaled - bufferedAtUnscaled) <= windowSeconds;
        }
    }
}
