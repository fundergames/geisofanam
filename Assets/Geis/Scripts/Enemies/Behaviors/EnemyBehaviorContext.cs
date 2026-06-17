/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using RogueDeal.Combat;
using UnityEngine;

namespace Geis.Enemies
{
    /// <summary>
    /// Per-tick snapshot and handles passed through the enemy behavior pipeline.
    /// </summary>
    public sealed class EnemyBehaviorContext
    {
        public EnemyCombatant Combatant { get; private set; }
        public EnemyPerception Perception { get; private set; }
        public EnemyMotor Motor { get; private set; }
        public EnemyAttackDriver AttackDriver { get; private set; }
        public EnemyAnimatorDriver AnimatorDriver { get; private set; }
        public CombatEntity CombatEntity { get; private set; }
        public EnemyBrain Brain { get; private set; }

        public CombatEntity Target { get; private set; }
        public float DistanceToTarget { get; private set; }
        public bool HasLineOfSight { get; private set; }
        public float DesiredCombatDistance { get; private set; }
        public float MeleeClosingDistance { get; private set; }

        public float StaggerRemaining { get; set; }
        public int StrafeDirection { get; set; }
        public float StrafeDirectionUntil { get; set; }

        public bool IsStrafingThisTick { get; set; }

        public void Bind(
            EnemyBrain brain,
            EnemyCombatant combatant,
            EnemyPerception perception,
            EnemyMotor motor,
            EnemyAttackDriver attackDriver,
            EnemyAnimatorDriver animatorDriver,
            CombatEntity combatEntity)
        {
            Brain = brain;
            Combatant = combatant;
            Perception = perception;
            Motor = motor;
            AttackDriver = attackDriver;
            AnimatorDriver = animatorDriver;
            CombatEntity = combatEntity;
        }

        public void RefreshTargetData()
        {
            Target = Perception != null ? Perception.CurrentTarget : null;
            DistanceToTarget = Perception != null ? Perception.GetDistanceToCurrentTarget() : float.PositiveInfinity;
            HasLineOfSight = Perception != null && Perception.HasLineOfSightToCurrentTarget();

            EnemyAiDefinition definition = Combatant != null ? Combatant.Definition : null;
            DesiredCombatDistance = definition != null ? definition.GetPreferredCombatDistance() : 2f;
            MeleeClosingDistance = definition != null ? definition.GetMeleeClosingDistance() : 1.8f;
        }

        public Vector3 GetTargetPoint()
        {
            if (Target != null)
                return Target.GetHitPoint();

            return Combatant != null ? Combatant.transform.position : Vector3.zero;
        }

        public void FaceTarget()
        {
            if (Target == null || Motor == null)
                return;

            Motor.FaceTarget(GetTargetPoint());
        }

        public void PresentLocomotion(bool hasTarget)
        {
            AnimatorDriver?.UpdateState(Motor, hasTarget, IsStrafingThisTick, Brain.CurrentState);
        }

        /// <summary>
        /// Starts melee when in range — stops NavMesh and enters Telegraph in the same tick.
        /// </summary>
        public bool TryCommitMeleeAttack()
        {
            if (Target == null || AttackDriver == null)
                return false;

            if (!AttackDriver.HasAnyAttackInRange(DistanceToTarget, HasLineOfSight))
                return false;

            if (!AttackDriver.TryStartAttack(Target, DistanceToTarget, HasLineOfSight))
                return false;

            Motor?.StopMovement();
            EnemyAttackDefinition attack = AttackDriver.CurrentAttack;
            Brain.EnterState(
                EnemyAttackDriver.UsesCodeTelegraph(attack)
                    ? EnemyBrain.EnemyState.Telegraph
                    : EnemyBrain.EnemyState.Attack);
            PresentLocomotion(hasTarget: true);
            return true;
        }
    }
}
