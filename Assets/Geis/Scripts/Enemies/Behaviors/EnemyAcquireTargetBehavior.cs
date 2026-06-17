/*
 * Copyright (c) 2026 Funder Games
 */

using UnityEngine;

namespace Geis.Enemies
{
    [CreateAssetMenu(fileName = "EnemyBehavior_Acquire", menuName = "Funder Games/Geis/Enemies/Behaviors/Acquire Target")]
    public class EnemyAcquireTargetBehavior : EnemyBehavior
    {
        public override bool TryExecute(EnemyBehaviorContext context)
        {
            if (context.Target != null)
                return false;

            context.Brain.EnterState(EnemyBrain.EnemyState.Acquire);
            context.Motor?.StopMovement();
            context.PresentLocomotion(hasTarget: false);
            return true;
        }
    }
}
