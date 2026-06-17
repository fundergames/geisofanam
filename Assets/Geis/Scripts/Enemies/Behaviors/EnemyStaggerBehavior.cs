/*
 * Copyright (c) 2026 Funder Games
 */

using UnityEngine;

namespace Geis.Enemies
{
    [CreateAssetMenu(fileName = "EnemyBehavior_Stagger", menuName = "Funder Games/Geis/Enemies/Behaviors/Stagger")]
    public class EnemyStaggerBehavior : EnemyBehavior
    {
        public override bool TryExecute(EnemyBehaviorContext context)
        {
            if (context.StaggerRemaining <= 0f)
                return false;

            context.StaggerRemaining -= Time.deltaTime;
            context.Brain.EnterState(EnemyBrain.EnemyState.Stagger);
            context.Motor?.StopMovement();
            context.PresentLocomotion(hasTarget: context.Target != null);
            return true;
        }
    }
}
