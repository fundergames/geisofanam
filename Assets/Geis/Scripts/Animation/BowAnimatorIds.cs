/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using UnityEngine;

namespace Geis.Animation
{
    /// <summary>
    /// Canonical Animator parameter hashes for bow draw / aim presentation.
    /// </summary>
    public static class BowAnimatorIds
    {
        public const string DrawLayerName = "Bow_Draw";
        public const string BowIdleStateName = "Bow_Idle";

        public const string BowDrawingName = "BowDrawing";
        public const string BowDrawChargeName = "BowDrawCharge";
        public const string BowAimingName = "BowAiming";
        public const string BowChargedShotReadyName = "BowChargedShotReady";

        public static readonly int BowDrawing = Animator.StringToHash(BowDrawingName);
        public static readonly int BowDrawCharge = Animator.StringToHash(BowDrawChargeName);
        public static readonly int BowAiming = Animator.StringToHash(BowAimingName);
        public static readonly int BowChargedShotReady = Animator.StringToHash(BowChargedShotReadyName);
        public static readonly int BowIdleState = Animator.StringToHash(BowIdleStateName);
    }
}
