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
using UnityEngine.AI;

namespace Geis.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMotor : MonoBehaviour
    {
        private const float DestinationArrivalTolerance = 0.1f;

        [SerializeField] private NavMeshAgent navMeshAgent;

        private EnemyCombatant _combatant;
        private float _planarSpeedMps;
        private float _speedMultiplier = 1f;
        private int _locomotionGaitIntent;

        /// <summary>Horizontal speed in m/s (feeds Animator MoveSpeed like the player).</summary>
        public float PlanarSpeedMps => _planarSpeedMps;

        /// <summary>Desired gait while velocity is still ramping (0=idle, 1=walk, 2=run, 3=sprint).</summary>
        public int LocomotionGaitIntent => _locomotionGaitIntent;

        private void Awake()
        {
            _combatant = GetComponent<EnemyCombatant>() ?? GetComponentInParent<EnemyCombatant>();
            navMeshAgent = navMeshAgent != null ? navMeshAgent : GetComponent<NavMeshAgent>();
            ConfigureAgentFromDefinition();
        }

        public void ConfigureAgentFromDefinition()
        {
            if (navMeshAgent == null || _combatant == null || _combatant.Definition == null)
                return;

            EnemyMovementSettings movement = _combatant.Definition.movement;
            navMeshAgent.speed = movement.moveSpeed;
            navMeshAgent.angularSpeed = movement.angularSpeed;
            navMeshAgent.acceleration = movement.acceleration;
            navMeshAgent.stoppingDistance = movement.stopDistance;
            navMeshAgent.updateRotation = false;
            ResetLocomotionPresentation();
        }

        /// <summary>
        /// Uses horizontal distance to the target: at or beyond <see cref="EnemyMovementSettings.approachRunDistanceThreshold"/>
        /// the agent runs (fast gait + higher NavMesh speed); closer bands jog/walk.
        /// </summary>
        public void ApplyApproachLocomotion(float horizontalDistanceToTarget, float meleeClosingDistance = 0f)
        {
            EnemyAiDefinition definition = _combatant != null ? _combatant.Definition : null;
            if (definition == null)
                return;

            EnemyMovementSettings m = definition.movement;
            float closing = meleeClosingDistance > 0f ? meleeClosingDistance : definition.GetMeleeClosingDistance();
            bool stillClosingForMelee = horizontalDistanceToTarget > closing + 0.25f;
            bool useRunBand = stillClosingForMelee
                || horizontalDistanceToTarget >= m.approachRunDistanceThreshold;
            _speedMultiplier = useRunBand ? m.approachRunSpeedMultiplier : m.approachJogSpeedMultiplier;
            _locomotionGaitIntent = useRunBand ? m.approachFastGait : m.approachSlowGait;

            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
                navMeshAgent.speed = m.moveSpeed * _speedMultiplier;
        }

        public void ApplyStrafeLocomotion()
        {
            EnemyAiDefinition definition = _combatant != null ? _combatant.Definition : null;
            if (definition == null)
                return;

            EnemyMovementSettings m = definition.movement;
            _speedMultiplier = m.strafeLocomotionSpeedMultiplier;
            _locomotionGaitIntent = m.strafeLocomotionGait;

            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
                navMeshAgent.speed = m.moveSpeed * _speedMultiplier;
        }

        public void ResetLocomotionPresentation()
        {
            _speedMultiplier = 1f;
            _locomotionGaitIntent = 0;

            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && _combatant != null && _combatant.Definition != null)
                navMeshAgent.speed = _combatant.Definition.movement.moveSpeed;
        }

        public void MoveToCombatDistance(Vector3 targetPosition, float desiredDistance)
        {
            Vector3 toTarget = transform.position - targetPosition;
            Vector3 planar = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            if (planar.sqrMagnitude <= 0.0001f)
                planar = -transform.forward;

            Vector3 destination = targetPosition + planar.normalized * desiredDistance;
            MoveTowards(destination, DestinationArrivalTolerance);
        }

        public void StrafeAround(Vector3 targetPosition, float desiredDistance, int directionSign)
        {
            Vector3 radial = transform.position - targetPosition;
            radial.y = 0f;
            if (radial.sqrMagnitude <= 0.0001f)
                radial = -transform.forward;

            Vector3 tangent = Vector3.Cross(Vector3.up, radial.normalized) * Mathf.Sign(directionSign == 0 ? 1 : directionSign);
            Vector3 desired = targetPosition + radial.normalized * desiredDistance + tangent * Mathf.Max(0.75f, desiredDistance * 0.5f);
            MoveTowards(desired, DestinationArrivalTolerance);
        }

        public void MoveTowards(Vector3 destination, float arrivalTolerance = DestinationArrivalTolerance)
        {
            EnemyAiDefinition definition = _combatant != null ? _combatant.Definition : null;
            if (definition == null)
                return;

            float clampedTolerance = Mathf.Max(0.01f, arrivalTolerance);

            if (CanUseNavMesh())
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.stoppingDistance = clampedTolerance;
                navMeshAgent.SetDestination(destination);
                float navCap = Mathf.Max(navMeshAgent.speed, 0.01f);
                UpdatePlanarSpeed(navMeshAgent.velocity.magnitude);
                return;
            }

            Vector3 toDestination = destination - transform.position;
            toDestination.y = 0f;
            float distance = toDestination.magnitude;
            if (distance <= clampedTolerance)
            {
                StopMovement();
                return;
            }

            Vector3 step = toDestination.normalized * definition.movement.directMoveFallbackSpeed * _speedMultiplier * Time.deltaTime;
            if (step.sqrMagnitude > toDestination.sqrMagnitude)
                step = toDestination;

            transform.position += step;
            float directCap = Mathf.Max(definition.movement.directMoveFallbackSpeed * _speedMultiplier, 0.01f);
            UpdatePlanarSpeed(step.magnitude / Mathf.Max(Time.deltaTime, 0.0001f));
        }

        public void StopMovement()
        {
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }

            _planarSpeedMps = 0f;
            ResetLocomotionPresentation();
        }

        public void FaceTarget(Vector3 targetPosition, float multiplier = 1f)
        {
            EnemyAiDefinition definition = _combatant != null ? _combatant.Definition : null;
            if (definition == null)
                return;

            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float rotateDegrees = definition.movement.angularSpeed * Mathf.Max(0.1f, multiplier) * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateDegrees);
        }

        public void WarpTo(Vector3 position)
        {
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
                navMeshAgent.Warp(position);
            else
                transform.position = position;

            _planarSpeedMps = 0f;
            ResetLocomotionPresentation();
        }

        private bool CanUseNavMesh()
        {
            return navMeshAgent != null
                && navMeshAgent.isActiveAndEnabled
                && navMeshAgent.isOnNavMesh;
        }

        private void UpdatePlanarSpeed(float speedMps)
        {
            _planarSpeedMps = Mathf.Max(0f, speedMps);
        }
    }
}
