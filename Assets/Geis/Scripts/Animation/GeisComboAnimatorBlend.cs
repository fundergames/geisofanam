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
    /// Writes combo blend-tree selection to an Animator (Float ComboStateBlend or Int ComboState).
    /// Shared by player body, soul-realm spectral rig, and enemies.
    /// </summary>
    public static class GeisComboAnimatorBlend
    {
        public const int DefaultSlotCount = 32;

        /// <summary>
        /// Prefers float blend parameter (0..1 over slots); falls back to int combo state when blend is absent.
        /// </summary>
        /// <param name="comboStateBlendParameterName">Animator float param (default <c>ComboStateBlend</c>).</param>
        /// <param name="comboStateIntParameterName">Animator int param fallback (default <c>ComboState</c>).</param>
        public static void Apply(
            Animator animator,
            int state,
            int slotCount = DefaultSlotCount,
            string comboStateBlendParameterName = "ComboStateBlend",
            string comboStateIntParameterName = "ComboState")
        {
            if (animator == null)
                return;

            state = Mathf.Max(0, state);

            if (!string.IsNullOrEmpty(comboStateBlendParameterName)
                && AnimatorParameterGuard.HasParameter(animator, comboStateBlendParameterName))
            {
                float blend = slotCount > 1 ? (float)state / (slotCount - 1) : 0f;
                animator.SetFloat(Animator.StringToHash(comboStateBlendParameterName), blend);
                return;
            }

            if (!string.IsNullOrEmpty(comboStateIntParameterName)
                && AnimatorParameterGuard.HasParameter(animator, comboStateIntParameterName))
                animator.SetInteger(Animator.StringToHash(comboStateIntParameterName), state);
        }
    }
}
