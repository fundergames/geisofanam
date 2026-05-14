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

using System.Collections.Generic;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Targeting;
using UnityEngine;

namespace Geis.Enemies
{
    [CreateAssetMenu(fileName = "Targeting_EnemyCurrentTarget", menuName = "Funder Games/Geis/Enemies/Current Enemy Targeting")]
    public class EnemyCurrentTargetingStrategy : TargetingStrategy
    {
        [Header("Fallback Range")]
        [Min(0.5f)] public float defaultRange = 2.5f;

        public override TargetResult ResolveTargets(CombatEntityData attacker)
        {
            SyncAllEntityPositions();

            CombatEntity attackerEntity = GetCombatEntityFromData(attacker);
            if (attackerEntity == null)
                return new TargetResult(null, attacker.position, false);

            EnemyPerception perception = attackerEntity.GetComponent<EnemyPerception>()
                ?? attackerEntity.GetComponentInParent<EnemyPerception>()
                ?? attackerEntity.GetComponentInChildren<EnemyPerception>();

            CombatEntity target = perception != null ? perception.CurrentTarget : null;
            if (target == null)
                return new TargetResult(null, attacker.position, false);

            CombatEntityData targetData = target.GetEntityData();
            if (targetData == null || !targetData.IsAlive)
                return new TargetResult(null, attacker.position, false);

            float maxRange = attacker.equippedWeapon != null && attacker.equippedWeapon.maxRange > 0f
                ? attacker.equippedWeapon.maxRange
                : attacker.combatProfile != null && attacker.combatProfile.engagementDistance > 0f
                    ? attacker.combatProfile.engagementDistance
                    : defaultRange;

            targetData.position = target.transform.position;

            // Match perception / strike checks: vertical separation alone must not void melee targeting.
            Vector3 ap = attacker.position;
            Vector3 tp = targetData.position;
            ap.y = 0f;
            tp.y = 0f;
            float planarDistance = Vector3.Distance(ap, tp);
            const float rangeEpsilon = 0.18f;
            if (planarDistance > maxRange + rangeEpsilon)
                return new TargetResult(null, attacker.position, false);

            if (attackerEntity.coneAngle > 0f)
            {
                Vector3 toTarget = targetData.position - attacker.position;
                if (toTarget.sqrMagnitude < 1e-8f)
                    return new TargetResult(null, attacker.position, false);

                float angleToTarget = Vector3.Angle(attackerEntity.transform.forward, toTarget.normalized);
                float halfCone = attackerEntity.coneAngle * 0.5f;
                if (angleToTarget > halfCone)
                    return new TargetResult(null, attacker.position, false);
            }

            return new TargetResult(new List<CombatEntity> { target }, targetData.position, true);
        }
    }
}
