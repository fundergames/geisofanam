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

            Vector3 toTarget = target.GetHitPoint() - attacker.transform.position;
            if (toTarget.sqrMagnitude < 1e-10f)
                return true;

            float angle = Vector3.Angle(attacker.transform.forward, toTarget.normalized);
            return angle <= cone * 0.5f + 0.05f;
        }
    }
}
