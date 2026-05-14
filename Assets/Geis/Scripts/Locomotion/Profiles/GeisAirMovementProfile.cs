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

namespace Geis.Locomotion
{
    [CreateAssetMenu(fileName = "AirMovementProfile", menuName = "Funder Games/Geis/Locomotion/Air Movement Profile")]
    public sealed class GeisAirMovementProfile : ScriptableObject
    {
        public float jumpForce = GeisLocomotionTuningDefaults.JumpForce;

        [Tooltip("Multiplier on Physics.gravity while airborne.")]
        public float gravityMultiplier = GeisLocomotionTuningDefaults.GravityMultiplier;

        [Tooltip("Seconds of air time to blend Falling_BlendTree from short to large fall.")]
        public float fallingBlendRampSeconds = GeisLocomotionTuningDefaults.FallingBlendRampSeconds;

        [Header("Jump polish (Action-feel)")]
        [Tooltip("Seconds after leaving the ground the player can still jump (coyote time). 0 disables.")]
        public float coyoteTimeSeconds = GeisLocomotionTuningDefaults.CoyoteTimeSeconds;

        [Tooltip("Seconds a jump press stays live so one pressed just before landing still triggers the jump. 0 disables.")]
        public float jumpBufferSeconds = GeisLocomotionTuningDefaults.JumpBufferSeconds;
    }
}
