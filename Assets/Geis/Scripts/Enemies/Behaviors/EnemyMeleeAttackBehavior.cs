/*
 * Copyright (c) 2026 Funder Games
 */

using UnityEngine;

namespace Geis.Enemies
{
    [CreateAssetMenu(fileName = "EnemyBehavior_MeleeAttack", menuName = "Funder Games/Geis/Enemies/Behaviors/Melee Attack")]
    public class EnemyMeleeAttackBehavior : EnemyBehavior
    {
        public override bool TryExecute(EnemyBehaviorContext context)
        {
            return context.TryCommitMeleeAttack();
        }
    }
}
