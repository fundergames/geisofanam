/*
 * Copyright (c) 2026 Funder Games
 */

using UnityEngine;

namespace Geis.Enemies
{
    [CreateAssetMenu(fileName = "EnemyBehavior_Approach", menuName = "Funder Games/Geis/Enemies/Behaviors/Approach Target")]
    public class EnemyApproachTargetBehavior : EnemyBehavior
    {
        [Tooltip("When true, run/jog locomotion uses raw distance to target vs approachRunDistanceThreshold on the AI definition.")]
        [SerializeField] private bool useRawDistanceForRunBand = true;

        public override bool TryExecute(EnemyBehaviorContext context)
        {
            if (context.Target == null || context.Motor == null)
                return false;

            if (context.TryCommitMeleeAttack())
                return true;

            if (context.AttackDriver != null
                && context.AttackDriver.HasAnyAttackInRange(context.DistanceToTarget, context.HasLineOfSight))
            {
                // In range but cannot swing yet (facing, cooldown) — face target without handing off to strafe.
                context.FaceTarget();
                context.Brain.EnterState(EnemyBrain.EnemyState.Approach);
                context.Motor.StopMovement();
                context.PresentLocomotion(hasTarget: true);
                return true;
            }

            context.FaceTarget();
            context.Brain.EnterState(EnemyBrain.EnemyState.Approach);

            float closing = context.MeleeClosingDistance;
            float locomotionDistance = useRawDistanceForRunBand
                ? context.DistanceToTarget
                : Mathf.Max(0f, context.DistanceToTarget - closing);

            context.Motor.ApplyApproachLocomotion(locomotionDistance, closing);
            context.Motor.MoveToCombatDistance(context.Target.transform.position, closing);
            context.PresentLocomotion(hasTarget: true);
            return true;
        }
    }
}
