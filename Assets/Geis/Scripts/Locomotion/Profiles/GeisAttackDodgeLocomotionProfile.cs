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
    [CreateAssetMenu(fileName = "AttackDodgeLocomotionProfile", menuName = "Funder Games/Geis/Locomotion/Attack & Dodge Locomotion Profile")]
    public sealed class GeisAttackDodgeLocomotionProfile : ScriptableObject
    {
        [Header("Root motion")]
        [Tooltip("Apply animation root rotation during attacks.")]
        public bool applyRootRotationDuringAttack = GeisLocomotionTuningDefaults.ApplyRootRotationDuringAttack;

        [Tooltip("Apply animation root rotation during dodge clips.")]
        public bool applyRootRotationDuringDodge = GeisLocomotionTuningDefaults.ApplyRootRotationDuringDodge;

        [Header("Dodge")]
        [Tooltip("Stick magnitude below this counts as neutral (forward dodge).")]
        public float dodgeInputDeadzone = GeisLocomotionTuningDefaults.DodgeInputDeadzone;

        [Tooltip("Fallback seconds if dodge clip length cannot be read.")]
        public float dodgeFallbackDuration = GeisLocomotionTuningDefaults.DodgeFallbackDuration;

        [Tooltip("If true, dodge only when movement stick exceeds deadzone.")]
        public bool requireMovementInputForDodge = GeisLocomotionTuningDefaults.RequireMovementInputForDodge;

        [Header("Cancel Windows (Action-feel)")]
        [Tooltip("Min stick magnitude (0-1) to move-cancel an attack during its cancel window.")]
        [Range(0f, 1f)]
        public float attackMoveCancelStickThreshold = GeisLocomotionTuningDefaults.AttackMoveCancelStickThreshold;

        [Tooltip("Normalized time on the attack clip after which the state exits back to Locomotion (if no buffered combo/dodge input).")]
        [Range(0f, 1f)]
        public float attackRecoveryExitNormalizedTime = GeisLocomotionTuningDefaults.AttackRecoveryExitNormalizedTime;

        [Tooltip("Fraction of _currentMaxSpeed pre-seeded onto planar velocity when move-cancelling an attack.")]
        [Range(0f, 1f)]
        public float attackExitVelocityCarry = GeisLocomotionTuningDefaults.AttackExitVelocityCarry;

        [Tooltip("Normalized time on the dodge clip after which recovery cancels are allowed.")]
        [Range(0f, 1f)]
        public float dodgeRecoveryStartNormalizedTime = GeisLocomotionTuningDefaults.DodgeRecoveryStartNormalizedTime;

        [Tooltip("Min stick magnitude (0-1) to move-cancel a dodge during its recovery window.")]
        [Range(0f, 1f)]
        public float dodgeMoveCancelStickThreshold = GeisLocomotionTuningDefaults.DodgeMoveCancelStickThreshold;

        [Tooltip("Fraction of _currentMaxSpeed pre-seeded onto planar velocity when exiting a dodge into movement.")]
        [Range(0f, 1f)]
        public float dodgeExitVelocityCarry = GeisLocomotionTuningDefaults.DodgeExitVelocityCarry;

        [Tooltip("Seconds a light/heavy/dodge input stays live in the buffer so presses just before a cancel window still register.")]
        public float inputBufferSeconds = GeisLocomotionTuningDefaults.InputBufferSeconds;

        [Header("Double-tap Dodge Roll")]
        [Tooltip("Max seconds between two dodge presses that count as a double-tap (triggers the forward-roll variant).")]
        [Range(0.05f, 0.6f)]
        public float dodgeDoubleTapWindow = GeisLocomotionTuningDefaults.DodgeDoubleTapWindow;

        [Tooltip("If true, double-tapping dodge performs a forward roll using the Dodge_Back clip played in reverse.")]
        public bool dodgeDoubleTapRollEnabled = GeisLocomotionTuningDefaults.DodgeDoubleTapRollEnabled;

        [Tooltip("Multiplier applied to the forward-roll's horizontal root-motion travel. 1 = baked distance; >1 rolls further; <1 rolls shorter.")]
        [Range(0.25f, 3f)]
        public float rollDistanceMultiplier = GeisLocomotionTuningDefaults.RollDistanceMultiplier;
    }
}
