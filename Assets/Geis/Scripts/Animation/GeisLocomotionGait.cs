/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

namespace Geis.Animation
{
    /// <summary>
    /// Shared gait selection for Synty/Polygon locomotion (Idle=0, Walk=1, Run=2, Sprint=3).
    /// Default speed thresholds mirror <c>Geis.Locomotion.GeisLocomotionTuningDefaults</c> (Geis assembly).
    /// </summary>
    public static class GeisLocomotionGait
    {
        public const int Idle = 0;
        public const int Walk = 1;
        public const int Run = 2;
        public const int Sprint = 3;

        /// <summary>Keep in sync with GeisLocomotionTuningDefaults.WalkSpeed.</summary>
        public const float DefaultWalkSpeed = 1.4f;

        /// <summary>Keep in sync with GeisLocomotionTuningDefaults.RunSpeed.</summary>
        public const float DefaultRunSpeed = 2.5f;

        /// <summary>Keep in sync with GeisLocomotionTuningDefaults.SprintSpeed.</summary>
        public const float DefaultSprintSpeed = 7f;

        public static int FromPlanarSpeed(
            float speed2D,
            float walkSpeed = DefaultWalkSpeed,
            float runSpeed = DefaultRunSpeed,
            float sprintSpeed = DefaultSprintSpeed)
        {
            float runThreshold = (walkSpeed + runSpeed) * 0.5f;
            float sprintThreshold = (runSpeed + sprintSpeed) * 0.5f;

            if (speed2D < 0.01f)
                return Idle;
            if (speed2D < runThreshold)
                return Walk;
            if (speed2D < sprintThreshold)
                return Run;
            return Sprint;
        }

        /// <summary>
        /// Typical MoveSpeed (m/s) for a gait when velocity has not ramped yet (NavMesh startup).
        /// </summary>
        public static float ReferenceSpeedForGait(
            int gait,
            float walkSpeed = DefaultWalkSpeed,
            float runSpeed = DefaultRunSpeed,
            float sprintSpeed = DefaultSprintSpeed)
        {
            return gait switch
            {
                Walk => walkSpeed,
                Run => runSpeed,
                Sprint => sprintSpeed,
                _ => 0f
            };
        }
    }
}
