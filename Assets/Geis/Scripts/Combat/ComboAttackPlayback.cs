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

using Geis.Animation;
using UnityEngine;

namespace Geis.Combat
{
    /// <summary>
    /// Shared data-driven combo enter/continue timing used by physical and soul-realm melee playback.
    /// </summary>
    public static class ComboAttackPlayback
    {
        public const int DefaultBlendSlots = GeisComboAnimatorBlend.DefaultSlotCount;

        public static float GetEnterAttackTimeout(GeisComboData comboData)
        {
            return comboData != null ? 2f : 1.5f;
        }

        public static float GetContinuationAttackTimeout(GeisComboData comboData, int comboState)
        {
            if (comboData == null)
                return 1.5f;

            AnimationClip clip = comboData.GetClipForState(comboState);
            return clip != null ? clip.length + 0.2f : 1.5f;
        }

        public static void EnterComboAttack(Animator animator, int attackTriggerHash, int comboState, int blendSlots = DefaultBlendSlots)
        {
            if (animator == null)
                return;

            GeisComboAnimatorBlend.Apply(animator, comboState, blendSlots);
            animator.SetTrigger(attackTriggerHash);
        }

        public static bool IsInCancelWindow(
            Animator animator,
            int layerIndex,
            GeisComboData comboData,
            int comboState,
            out float normalizedTime)
        {
            normalizedTime = 0f;
            if (animator == null || comboData == null)
                return false;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
            normalizedTime = info.normalizedTime % 1f;
            comboData.GetCancelWindow(comboState, out float cancelWindowStart, out float cancelWindowEnd);
            return normalizedTime >= cancelWindowStart && normalizedTime <= cancelWindowEnd;
        }

        /// <summary>
        /// Advances combo state, re-triggers Attack, and returns the clip-based timeout for the new step.
        /// </summary>
        public static bool TryContinueCombo(
            Animator animator,
            GeisComboData comboData,
            GeisComboInputType input,
            ref int currentComboState,
            int attackTriggerHash,
            int blendSlots,
            out float attackStateTimeout)
        {
            attackStateTimeout = 0f;
            if (animator == null || comboData == null)
                return false;

            if (!comboData.TryGetNextState(currentComboState, input, out int nextState))
                return false;

            currentComboState = nextState;
            EnterComboAttack(animator, attackTriggerHash, currentComboState, blendSlots);
            attackStateTimeout = GetContinuationAttackTimeout(comboData, currentComboState);
            return true;
        }
    }
}
