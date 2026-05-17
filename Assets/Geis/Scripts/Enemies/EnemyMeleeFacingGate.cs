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
    /// <summary>
    /// Strike-time validation for hostile AI using <see cref="CombatEntity.coneAngle"/> so damage matches a frontal melee arc.
    /// </summary>
    public static class EnemyMeleeFacingGate
    {
        /// <summary>
        /// Horizontal distance (XZ) under which we allow a wider arc. Hit reactions / root motion often move the defender
        /// between attack start and delayed damage frames (e.g. <see cref="CombatExecutor"/> ApplyEffectsAfterDelay),
        /// which would otherwise reject every follow-up swing as "not frontal".
        /// </summary>
        private const float CloseMeleeHorizDistance = 2.35f;

        /// <summary>
        /// Extra degrees beyond half the configured cone when the target is still in close melee (XZ).
        /// </summary>
        private const float CloseMeleeAngleSlackDegrees = 34f;

        /// <summary>
        /// Non-enemy attackers always pass. Enemy combatants must have the target inside their forward cone at call time.
        /// </summary>
        public static bool AllowsHitAtStrikeTime(CombatEntity attacker, CombatEntity target)
        {
            if (attacker == null || target == null)
                return true;

            if (attacker.GetComponentInParent<EnemyCombatant>() == null)
                return true;

            float cone = attacker.coneAngle;
            if (cone <= 0f)
                return true;

            Vector3 attackerPos = attacker.transform.position;
            Vector3 toTarget = target.GetHitPoint() - attackerPos;
            Vector3 toFlat = new Vector3(toTarget.x, 0f, toTarget.z);
            if (toFlat.sqrMagnitude < 1e-10f)
                return true;

            Vector3 fwd = attacker.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-10f)
                fwd = attacker.transform.forward;
            fwd.Normalize();
            toFlat.Normalize();

            float angle = Vector3.Angle(fwd, toFlat);
            float halfCone = cone * 0.5f + 0.05f;
            if (angle <= halfCone)
                return true;

            float horizDist = Vector2.Distance(
                new Vector2(attackerPos.x, attackerPos.z),
                new Vector2(target.transform.position.x, target.transform.position.z));

            if (horizDist <= CloseMeleeHorizDistance)
            {
                float relaxedCap = Mathf.Min(halfCone + CloseMeleeAngleSlackDegrees, 108f);
                return angle <= relaxedCap;
            }

            return false;
        }
    }
}
