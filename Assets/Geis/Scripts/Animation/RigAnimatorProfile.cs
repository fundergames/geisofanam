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

namespace Geis.Animation
{
    /// <summary>
    /// Design-time profile documenting animator contracts and optional reference controller for a rig (Synty, Polygon, etc.).
    /// </summary>
    [CreateAssetMenu(fileName = "RigAnimatorProfile", menuName = "Geis/Animation/Rig Animator Profile")]
    public sealed class RigAnimatorProfile : ScriptableObject
    {
        [Tooltip("Required parameters and setup notes for this Animator Controller.")]
        [TextArea(4, 12)]
        public string animatorContractNotes;

        [Tooltip("Optional: controller this profile was authored against.")]
        public RuntimeAnimatorController referenceRuntimeController;
    }
}
