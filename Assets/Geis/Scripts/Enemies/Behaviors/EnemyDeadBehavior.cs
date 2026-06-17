/*
 * Copyright (c) 2026 Funder Games
 */

using UnityEngine;

namespace Geis.Enemies
{
    [CreateAssetMenu(fileName = "EnemyBehavior_Dead", menuName = "Funder Games/Geis/Enemies/Behaviors/Dead")]
    public class EnemyDeadBehavior : EnemyBehavior
    {
        public override bool TryExecute(EnemyBehaviorContext context)
        {
            if (context.Combatant == null || !context.Combatant.IsDefeated)
                return false;

            context.Brain.EnterState(EnemyBrain.EnemyState.Dead);
            context.Motor?.StopMovement();
            context.PresentLocomotion(hasTarget: false);
            return true;
        }
    }
}
