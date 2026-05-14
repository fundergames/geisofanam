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
    [CreateAssetMenu(fileName = "LocomotionSpeedProfile", menuName = "Funder Games/Geis/Locomotion/Locomotion Speed Profile")]
    public sealed class GeisLocomotionSpeedProfile : ScriptableObject
    {
        [Tooltip("Whether the character always strafes relative to the camera when moving.")]
        public bool alwaysStrafe = GeisLocomotionTuningDefaults.AlwaysStrafe;

        [Tooltip("Slowest movement speed when walking or half-press.")]
        public float walkSpeed = GeisLocomotionTuningDefaults.WalkSpeed;

        [Tooltip("Default movement speed (run).")]
        public float runSpeed = GeisLocomotionTuningDefaults.RunSpeed;

        [Tooltip("Top movement speed when sprinting.")]
        public float sprintSpeed = GeisLocomotionTuningDefaults.SprintSpeed;

        [Tooltip("Damping when lerping toward target planar speed (legacy fallback).")]
        public float speedChangeDamping = GeisLocomotionTuningDefaults.SpeedChangeDamping;

        [Tooltip("Rotation smoothing when aligning to move/camera.")]
        public float rotationSmoothing = GeisLocomotionTuningDefaults.RotationSmoothing;

        [Header("Action-feel responsiveness")]
        [Tooltip("Rate toward target planar speed when accelerating (snappy starts). Higher = snappier. Frame-rate stable.")]
        public float accelRate = GeisLocomotionTuningDefaults.AccelRate;

        [Tooltip("Rate toward target planar speed when decelerating. Higher = snappier stops.")]
        public float decelRate = GeisLocomotionTuningDefaults.DecelRate;

        [Tooltip("When the desired max speed grows (e.g. sprint pressed), snap _currentMaxSpeed up to this fraction of the new target. 0 = disabled; 1 = instant.")]
        [Range(0f, 1f)]
        public float sprintInstantFraction = GeisLocomotionTuningDefaults.SprintInstantFraction;

        [Tooltip("Hard cap on root yaw rotation (deg/sec) so 180° pivots finish in bounded time.")]
        public float maxTurnDegPerSecond = GeisLocomotionTuningDefaults.MaxTurnDegPerSecond;
    }
}
