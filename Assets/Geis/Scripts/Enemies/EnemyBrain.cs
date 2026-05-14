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

using RogueDeal.Combat;
using UnityEngine;

namespace Geis.Enemies
{
    public class EnemyBrain : MonoBehaviour
    {
        public enum EnemyState
        {
            Idle = 0,
            Acquire = 1,
            Approach = 2,
            Strafe = 3,
            Telegraph = 4,
            Attack = 5,
            Recover = 6,
            Stagger = 7,
            Dead = 8
        }

        [SerializeField] private bool autoRun = true;

        private EnemyCombatant _combatant;
        private EnemyPerception _perception;
        private EnemyMotor _motor;
        private EnemyAttackDriver _attackDriver;
        private EnemyAnimatorDriver _animatorDriver;
        private CombatEntity _combatEntity;

        private float _staggerRemaining;
        private float _strafeDirectionUntil;
        private int _strafeDirection = 1;

        public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

        private void Awake()
        {
            _combatant = GetComponent<EnemyCombatant>() ?? GetComponentInParent<EnemyCombatant>();
            _perception = GetComponent<EnemyPerception>() ?? GetComponentInParent<EnemyPerception>();
            _motor = GetComponent<EnemyMotor>() ?? GetComponentInParent<EnemyMotor>();
            _attackDriver = GetComponent<EnemyAttackDriver>() ?? GetComponentInParent<EnemyAttackDriver>();
            _animatorDriver = GetComponent<EnemyAnimatorDriver>() ?? GetComponentInParent<EnemyAnimatorDriver>();
            _combatEntity = GetComponent<CombatEntity>() ?? GetComponentInParent<CombatEntity>();
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageApplied += HandleDamageApplied;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageApplied -= HandleDamageApplied;
        }

        private void Update()
        {
            if (!autoRun || _combatant == null || _combatant.Definition == null)
                return;

            if (_combatant.IsDefeated)
            {
                EnterState(EnemyState.Dead);
                _motor?.StopMovement();
                UpdateAnimator(false, false);
                return;
            }

            _perception?.RefreshTarget();
            CombatEntity target = _perception != null ? _perception.CurrentTarget : null;
            bool hasTarget = target != null;
            bool isStrafing = false;

            if (!hasTarget)
            {
                EnterState(EnemyState.Acquire);
                _motor?.StopMovement();
                UpdateAnimator(false, false);
                return;
            }

            Vector3 targetPoint = target.GetHitPoint();
            _motor?.FaceTarget(targetPoint);

            if (_staggerRemaining > 0f)
            {
                _staggerRemaining -= Time.deltaTime;
                EnterState(EnemyState.Stagger);
                _motor?.StopMovement();
                UpdateAnimator(true, false);
                return;
            }

            if (_attackDriver != null && _attackDriver.IsBusy)
            {
                switch (_attackDriver.CurrentPhase)
                {
                    case EnemyAttackDriver.AttackPhase.Telegraph:
                        EnterState(EnemyState.Telegraph);
                        break;
                    case EnemyAttackDriver.AttackPhase.Execute:
                        EnterState(EnemyState.Attack);
                        break;
                    case EnemyAttackDriver.AttackPhase.Recover:
                        EnterState(EnemyState.Recover);
                        break;
                    default:
                        EnterState(EnemyState.Attack);
                        break;
                }

                if (hasTarget && target != null)
                    _motor?.FaceTarget(target.GetHitPoint());

                _motor?.StopMovement();
                UpdateAnimator(true, false);
                return;
            }

            float desiredDistance = _combatant.Definition.GetPreferredCombatDistance();
            float distance = _perception != null ? _perception.GetDistanceToCurrentTarget() : float.PositiveInfinity;
            bool hasLineOfSight = _perception != null && _perception.HasLineOfSightToCurrentTarget();

            // Do NOT gate melee attempts on preferredSpacing + tolerance alone — NavMesh often parks the agent
            // slightly outside that band while still inside authored attack maxRange; the old check caused infinite Approach.
            float strikeHorizon = _combatant.Definition.GetMaxStrikeRange() + 0.3f;
            if (distance > strikeHorizon)
            {
                EnterState(EnemyState.Approach);
                _motor?.ApplyApproachLocomotion(distance, desiredDistance);
                _motor?.MoveToCombatDistance(target.transform.position, desiredDistance);
                UpdateAnimator(true, false);
                return;
            }

            if (_attackDriver != null && _attackDriver.TryStartAttack(target, distance, hasLineOfSight))
            {
                EnterState(EnemyState.Telegraph);
                _motor?.StopMovement();
                UpdateAnimator(true, false);
                return;
            }

            if (_attackDriver != null && _attackDriver.HasAnyAttackInRange(distance, hasLineOfSight))
            {
                EnterState(EnemyState.Strafe);
                isStrafing = true;
                UpdateStrafeDirection();
                _motor?.ApplyStrafeLocomotion();
                _motor?.StrafeAround(target.transform.position, desiredDistance, _strafeDirection);
                UpdateAnimator(true, isStrafing);
                return;
            }

            EnterState(EnemyState.Approach);
            _motor?.ApplyApproachLocomotion(distance, desiredDistance);
            _motor?.MoveToCombatDistance(target.transform.position, desiredDistance);
            UpdateAnimator(true, false);
        }

        public void ResetBrain()
        {
            CurrentState = EnemyState.Idle;
            _staggerRemaining = 0f;
            _strafeDirection = 1;
            _strafeDirectionUntil = 0f;
        }

        public void HandleDefeated()
        {
            EnterState(EnemyState.Dead);
            _motor?.StopMovement();
            _attackDriver?.CancelActiveAttack();
            UpdateAnimator(false, false);
        }

        private void HandleDamageApplied(CombatEventData data)
        {
            if (_combatEntity == null || _combatant == null || _combatant.Definition == null)
                return;

            if (data.target != _combatEntity || data.wasImmune || data.damageAmount <= 0f || _combatant.IsDefeated)
                return;

            _staggerRemaining = _combatant.Definition.reactions.staggerDurationOnHit;
            _attackDriver?.CancelActiveAttack();
            _motor?.StopMovement();
            _animatorDriver?.TriggerHitReaction();
        }

        private void UpdateAnimator(bool hasTarget, bool isStrafing)
        {
            _animatorDriver?.UpdateState(
                _motor != null ? _motor.CurrentNormalisedSpeed : 0f,
                hasTarget,
                isStrafing,
                CurrentState,
                _motor != null ? _motor.LocomotionGaitIndex : 0);
        }

        private void EnterState(EnemyState nextState)
        {
            CurrentState = nextState;
        }

        private void UpdateStrafeDirection()
        {
            if (Time.time < _strafeDirectionUntil)
                return;

            _strafeDirection *= -1;
            _strafeDirectionUntil = Time.time + Mathf.Max(0.25f, _combatant.Definition.movement.strafeRepathInterval);
        }
    }
}
