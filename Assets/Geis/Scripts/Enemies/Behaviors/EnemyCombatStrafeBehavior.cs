/*
 * Copyright (c) 2026 Funder Games
 */

using UnityEngine;

namespace Geis.Enemies
{
    [CreateAssetMenu(fileName = "EnemyBehavior_Strafe", menuName = "Funder Games/Geis/Enemies/Behaviors/Combat Strafe")]
    public class EnemyCombatStrafeBehavior : EnemyBehavior
    {
        public override bool TryExecute(EnemyBehaviorContext context)
        {
            if (context.Target == null || context.AttackDriver == null || context.Motor == null)
                return false;

            if (!context.AttackDriver.HasAnyAttackInRange(context.DistanceToTarget, context.HasLineOfSight))
                return false;

            // Do not strafe while still outside closing distance — Approach must finish the gap first.
            if (context.DistanceToTarget > context.MeleeClosingDistance + 0.2f)
                return false;

            context.Brain.UpdateStrafeDirection();
            context.IsStrafingThisTick = true;
            context.Brain.EnterState(EnemyBrain.EnemyState.Strafe);
            context.Motor.ApplyStrafeLocomotion();
            context.Motor.StrafeAround(
                context.Target.transform.position,
                context.DesiredCombatDistance,
                context.Brain.StrafeDirection);
            context.PresentLocomotion(hasTarget: true);
            return true;
        }
    }
}
