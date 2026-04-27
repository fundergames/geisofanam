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
    [CreateAssetMenu(fileName = "PlayerCapsuleProfile", menuName = "Geis/Locomotion/Player Capsule Profile")]
    public sealed class GeisPlayerCapsuleProfile : ScriptableObject
    {
        public float standingHeight = GeisLocomotionTuningDefaults.CapsuleStandingHeight;
        public float standingCentre = GeisLocomotionTuningDefaults.CapsuleStandingCentre;
        public float crouchingHeight = GeisLocomotionTuningDefaults.CapsuleCrouchingHeight;
        public float crouchingCentre = GeisLocomotionTuningDefaults.CapsuleCrouchingCentre;
    }
}
