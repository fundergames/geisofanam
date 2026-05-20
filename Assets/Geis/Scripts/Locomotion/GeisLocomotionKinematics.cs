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
    /// <summary>
    /// Single implementation for camera-relative move direction, grounded speed blending, planar velocity steps,
    /// and root rotation shared by the physical character and soul-ghost motor.
    /// </summary>
    public static class GeisLocomotionKinematics
    {
        public static Vector3 ComputeCameraRelativeMoveDirection(Vector2 moveComposite, GeisCameraController camera)
        {
            return camera.GetCameraForwardZeroedYNormalised() * moveComposite.y
                + camera.GetCameraRightZeroedYNormalised() * moveComposite.x;
        }

        /// <summary>When no <see cref="GeisCameraController"/> (e.g. tools): flatten main camera axes to XZ.</summary>
        public static Vector3 ComputeCameraRelativeMoveDirectionFromTransform(Vector2 moveComposite, Transform cameraTransform)
        {
            if (cameraTransform == null)
                return Vector3.zero;
            Vector3 camFwd = Vector3.Scale(cameraTransform.forward, new Vector3(1f, 0f, 1f)).normalized;
            Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1f, 0f, 1f)).normalized;
            return camRight * moveComposite.x + camFwd * moveComposite.y;
        }

        /// <summary>
        /// Planar camera forward for strafe / <see cref="StepRootRotationForFaceMoveDirection"/>: same basis as move
        /// (<see cref="ComputeCameraRelativeMoveDirection"/> uses controller → actual render camera, not the orbit pivot).
        /// </summary>
        public static Vector3 GetCameraForwardZeroedYForLocomotion(
            GeisCameraController camera,
            Transform cameraTransformFallback,
            Transform characterForwardFallback)
        {
            if (camera != null)
                return camera.GetCameraForwardZeroedYNormalised();
            if (cameraTransformFallback != null)
                return Vector3.Scale(cameraTransformFallback.forward, new Vector3(1f, 0f, 1f)).normalized;
            if (characterForwardFallback != null)
                return Vector3.Scale(characterForwardFallback.forward, new Vector3(1f, 0f, 1f)).normalized;
            return Vector3.zero;
        }

        public static float EvaluateGroundedTargetMaxSpeed(
            bool isCrouching,
            bool isSprinting,
            bool isWalking,
            float walkSpeed,
            float runSpeed,
            float sprintSpeed)
        {
            return ResolveTargetMaxSpeed(
                isGrounded: true,
                forceWalk: false,
                isCrouching,
                isSprinting,
                isWalking,
                walkSpeed,
                runSpeed,
                sprintSpeed,
                currentMaxSpeedWhenAirborne: runSpeed);
        }

        /// <summary>
        /// Target max planar speed for one locomotion tick (airborne keeps <paramref name="currentMaxSpeedWhenAirborne"/>).
        /// </summary>
        public static float ResolveTargetMaxSpeed(
            bool isGrounded,
            bool forceWalk,
            bool isCrouching,
            bool isSprinting,
            bool isWalking,
            float walkSpeed,
            float runSpeed,
            float sprintSpeed,
            float currentMaxSpeedWhenAirborne)
        {
            if (!isGrounded)
                return currentMaxSpeedWhenAirborne;
            if (forceWalk || isCrouching)
                return walkSpeed;
            if (isSprinting)
                return sprintSpeed;
            if (isWalking)
                return walkSpeed;
            return runSpeed;
        }

        /// <summary>
        /// Body <see cref="GeisPlayerAnimationController.CalculateMoveDirection"/> speed blend (sprint snap + accel/decel).
        /// </summary>
        public static void StepBodyPlanarVelocity(
            ref float currentMaxSpeed,
            ref float velocityX,
            ref float velocityZ,
            Vector3 moveDirection,
            float targetMaxSpeed,
            float maxSpeedLerpRate,
            float sprintInstantFraction,
            float accelRate,
            float decelRate,
            float speedChangeDamping,
            float deltaTime)
        {
            if (targetMaxSpeed > currentMaxSpeed && sprintInstantFraction > 0f)
                currentMaxSpeed = Mathf.Max(currentMaxSpeed, targetMaxSpeed * sprintInstantFraction);

            currentMaxSpeed = ExpSmooth(currentMaxSpeed, targetMaxSpeed, maxSpeedLerpRate, deltaTime);

            float targetVx = moveDirection.x * currentMaxSpeed;
            float targetVz = moveDirection.z * currentMaxSpeed;

            float targetPlanarSqr = targetVx * targetVx + targetVz * targetVz;
            float currentPlanarSqr = velocityX * velocityX + velocityZ * velocityZ;
            bool accelerating = targetPlanarSqr > currentPlanarSqr;
            float activeRate = accelerating
                ? (accelRate > 0f ? accelRate : speedChangeDamping)
                : (decelRate > 0f ? decelRate : speedChangeDamping);

            velocityX = ExpSmooth(velocityX, targetVx, activeRate, deltaTime);
            velocityZ = ExpSmooth(velocityZ, targetVz, activeRate, deltaTime);
        }

        public static float ExpSmooth(float current, float target, float rate, float deltaTime)
        {
            if (rate <= 0f)
                return target;
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-rate * deltaTime));
        }

        /// <summary>Legacy ghost motor lerp path (single damping rate toward target max speed).</summary>
        public static void StepPlanarLocomotionVelocity(
            ref float currentMaxSpeed,
            ref Vector3 velocity,
            Vector3 moveDirection,
            bool isGrounded,
            bool isCrouching,
            bool isSprinting,
            bool isWalking,
            float walkSpeed,
            float runSpeed,
            float sprintSpeed,
            float maxSpeedLerpRate,
            float speedChangeDamping,
            float deltaTime)
        {
            float targetMaxSpeed;
            if (!isGrounded)
            {
                targetMaxSpeed = currentMaxSpeed;
            }
            else
            {
                targetMaxSpeed = EvaluateGroundedTargetMaxSpeed(
                    isCrouching, isSprinting, isWalking, walkSpeed, runSpeed, sprintSpeed);
            }

            currentMaxSpeed = Mathf.Lerp(currentMaxSpeed, targetMaxSpeed, maxSpeedLerpRate * deltaTime);

            var targetVelocity = new Vector3(
                moveDirection.x * currentMaxSpeed,
                velocity.y,
                moveDirection.z * currentMaxSpeed);

            float sd = speedChangeDamping * deltaTime;
            velocity.x = Mathf.Lerp(velocity.x, targetVelocity.x, sd);
            velocity.z = Mathf.Lerp(velocity.z, targetVelocity.z, sd);
        }

        public static float RoundPlanarSpeed2D(Vector3 velocity)
        {
            float s = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            return Mathf.Round(s * 1000f) / 1000f;
        }

        /// <summary>
        /// Root rotation shared with <see cref="GeisPlayerAnimationController.FaceMoveDirection"/> (animator side data omitted).
        /// </summary>
        public static Quaternion StepRootRotationForFaceMoveDirection(
            Quaternion current,
            bool isStrafing,
            Vector3 moveDirection,
            Vector3 planarVelocity,
            Vector3 cameraForwardZeroedY,
            float rotationSmoothing,
            float deltaTime)
        {
            if (isStrafing)
            {
                if (moveDirection.magnitude > 0.01f && cameraForwardZeroedY != Vector3.zero)
                {
                    Quaternion strafingTargetRotation = Quaternion.LookRotation(cameraForwardZeroedY);
                    return Quaternion.Slerp(current, strafingTargetRotation, rotationSmoothing * deltaTime);
                }

                return current;
            }

            Vector3 faceDirection = new Vector3(planarVelocity.x, 0f, planarVelocity.z);
            if (faceDirection == Vector3.zero)
                return current;

            return Quaternion.Slerp(
                current,
                Quaternion.LookRotation(faceDirection),
                rotationSmoothing * deltaTime);
        }
    }
}
