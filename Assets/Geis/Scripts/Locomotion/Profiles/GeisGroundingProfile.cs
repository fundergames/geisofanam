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
    [CreateAssetMenu(fileName = "GroundingProfile", menuName = "Funder Games/Geis/Locomotion/Grounding Profile")]
    public sealed class GeisGroundingProfile : ScriptableObject
    {
        [Tooltip("Layers used for ground checks, incline rays, and ceiling checks.")]
        public LayerMask groundLayerMask = ~0;

        [Tooltip("Offset below character center for grounded sphere check.")]
        public float groundedOffset = GeisLocomotionTuningDefaults.GroundedOffset;
    }
}
