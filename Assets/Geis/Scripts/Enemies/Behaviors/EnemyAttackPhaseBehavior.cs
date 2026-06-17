/*
 * Copyright (c) 2026 Funder Games
 */

using UnityEngine;

namespace Geis.Enemies
{
    [CreateAssetMenu(fileName = "EnemyBehavior_AttackPhase", menuName = "Funder Games/Geis/Enemies/Behaviors/Attack Phase")]
    public class EnemyAttackPhaseBehavior : EnemyBehavior
    {
        public override bool TryExecute(EnemyBehaviorContext context)
        {
            if (context.AttackDriver == null || !context.AttackDriver.IsBusy)
                return false;

            switch (context.AttackDriver.CurrentPhase)
            {
                case EnemyAttackDriver.AttackPhase.Telegraph:
                    context.Brain.EnterState(EnemyBrain.EnemyState.Telegraph);
                    break;
                case EnemyAttackDriver.AttackPhase.Execute:
                    context.Brain.EnterState(EnemyBrain.EnemyState.Attack);
                    break;
                case EnemyAttackDriver.AttackPhase.Recover:
                    context.Brain.EnterState(EnemyBrain.EnemyState.Recover);
                    break;
                default:
                    context.Brain.EnterState(EnemyBrain.EnemyState.Attack);
                    break;
            }

            if (context.Target != null)
                context.FaceTarget();

            context.Motor?.StopMovement();
            context.PresentLocomotion(hasTarget: true);
            return true;
        }
    }
}
