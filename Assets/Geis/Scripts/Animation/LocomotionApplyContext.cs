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

namespace Geis.Animation
{
    /// <summary>
    /// How <see cref="LocomotionAnimatorApplier"/> applies <see cref="LocomotionPresentationSnapshot"/> (air vs ground, optional IsJumping).
    /// </summary>
    public struct LocomotionApplyContext
    {
        /// <summary>When true, MoveSpeed/gait/strafe are zeroed/idle for jump/fall animator states.</summary>
        public bool AirGaitForAnimator;

        public bool HasFallingBlendParameter;
        public float FallingBlendValue;

        public bool SetIsJumping;
        public bool IsJumpingValue;
    }
}
