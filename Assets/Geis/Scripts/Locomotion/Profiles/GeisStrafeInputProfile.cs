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
    [CreateAssetMenu(fileName = "StrafeInputProfile", menuName = "Geis/Locomotion/Strafe & Input Profile")]
    public sealed class GeisStrafeInputProfile : ScriptableObject
    {
        [Tooltip("Threshold for movement button hold vs tap vs held.")]
        public float buttonHoldThreshold = GeisLocomotionTuningDefaults.ButtonHoldThreshold;

        [Tooltip("Minimum strafe angle (degrees) for forward-strafe blend.")]
        public float forwardStrafeMinThreshold = GeisLocomotionTuningDefaults.ForwardStrafeMinThreshold;

        [Tooltip("Maximum strafe angle (degrees) for forward-strafe blend.")]
        public float forwardStrafeMaxThreshold = GeisLocomotionTuningDefaults.ForwardStrafeMaxThreshold;

        [Tooltip("Initial forward strafe blend (animator).")]
        public float forwardStrafe = GeisLocomotionTuningDefaults.ForwardStrafe;
    }
}
