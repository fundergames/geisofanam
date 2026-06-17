/*
 * Copyright (c) 2026 Funder Games
 */

using UnityEngine;

namespace Geis.Enemies
{
    /// <summary>
    /// Runtime fallback when <see cref="EnemyAiDefinition.behaviorPipeline"/> is empty.
    /// </summary>
    public static class EnemyBuiltinBehaviorPipeline
    {
        private static EnemyBehavior[] _cached;

        public static EnemyBehavior[] GetOrCreate()
        {
            if (_cached != null && _cached.Length > 0)
                return _cached;

            _cached = new EnemyBehavior[]
            {
                Create<EnemyDeadBehavior>(),
                Create<EnemyStaggerBehavior>(),
                Create<EnemyAttackPhaseBehavior>(),
                Create<EnemyAcquireTargetBehavior>(),
                Create<EnemyApproachTargetBehavior>(),
                Create<EnemyMeleeAttackBehavior>(),
                Create<EnemyCombatStrafeBehavior>()
            };

            return _cached;
        }

        private static T Create<T>() where T : EnemyBehavior
        {
            var instance = ScriptableObject.CreateInstance<T>();
            instance.hideFlags = HideFlags.HideAndDontSave;
            return instance;
        }
    }
}
