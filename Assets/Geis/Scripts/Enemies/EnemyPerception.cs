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
using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace Geis.Enemies
{
    public class EnemyPerception : MonoBehaviour
    {
        [SerializeField] private CombatEntity explicitTarget;

        private EnemyCombatant _combatant;
        private CombatEntity _currentTarget;

        public CombatEntity CurrentTarget => _currentTarget;

        private void Awake()
        {
            _combatant = GetComponent<EnemyCombatant>() ?? GetComponentInParent<EnemyCombatant>();
        }

        public void RefreshTarget()
        {
            EnemyAiDefinition definition = _combatant != null ? _combatant.Definition : null;
            if (definition == null)
            {
                _currentTarget = null;
                return;
            }

            CombatEntity candidate = FindBestTarget(definition);
            if (candidate != null)
            {
                _currentTarget = candidate;
                return;
            }

            _currentTarget = null;
        }

        public void SetExplicitTarget(CombatEntity target)
        {
            explicitTarget = target;
            _currentTarget = target;
        }

        public void ClearExplicitTarget()
        {
            explicitTarget = null;
        }

        public void ResetPerception()
        {
            _currentTarget = explicitTarget;
        }

        public bool HasLineOfSightToCurrentTarget()
        {
            return HasLineOfSightTo(_currentTarget);
        }

        public bool HasLineOfSightTo(CombatEntity target)
        {
            EnemyAiDefinition definition = _combatant != null ? _combatant.Definition : null;
            if (definition == null || target == null)
                return false;

            if (!definition.perception.requiresLineOfSight)
                return true;

            Vector3 origin = transform.position + Vector3.up * definition.perception.eyeHeight;
            Vector3 targetPoint = target.GetHitPoint();
            Vector3 delta = targetPoint - origin;
            float distance = delta.magnitude;
            if (distance <= 0.001f)
                return true;

            LayerMask blockers = definition.perception.lineOfSightBlockers;
            if (blockers.value == 0)
                blockers = ~0;

            Vector3 direction = delta.normalized;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, blockers, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return true;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            Transform selfRoot = transform.root;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                Collider c = hit.collider;
                if (c == null)
                    continue;

                // Rays can originate inside / graze this enemy's capsule — ignore self geometry.
                if (c.transform.root == selfRoot)
                    continue;

                CombatEntity hitEntity = c.GetComponent<CombatEntity>()
                    ?? c.GetComponentInParent<CombatEntity>()
                    ?? c.GetComponentInChildren<CombatEntity>();

                return hitEntity == target;
            }

            return true;
        }

        public float GetDistanceToCurrentTarget()
        {
            return GetDistanceToTarget(_currentTarget);
        }

        public Vector3 GetCurrentTargetPoint()
        {
            return _currentTarget != null ? _currentTarget.GetHitPoint() : transform.position;
        }

        /// <summary>
        /// Horizontal combat spacing (XZ). Vertical offsets were inflating 3D distance so enemies stayed in Approach
        /// while appearing “close enough”, yet failed <see cref="EnemyAttackDefinition.maxRange"/> checks.
        /// </summary>
        private float GetDistanceToTarget(CombatEntity target)
        {
            if (target == null)
                return float.PositiveInfinity;

            Vector3 a = transform.position;
            Vector3 b = target.transform.position;
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private CombatEntity FindBestTarget(EnemyAiDefinition definition)
        {
            if (definition == null)
                return null;

            if (IsTargetInAggroRange(explicitTarget, definition))
                return explicitTarget;

            CombatEntity currentPreferred = IsTargetWithinLoseRange(_currentTarget, definition) ? _currentTarget : null;
            float bestDistance = currentPreferred != null ? GetDistanceToTarget(currentPreferred) : float.PositiveInfinity;
            CombatEntity bestTarget = currentPreferred;

            CombatEntity[] allEntities = Object.FindObjectsByType<CombatEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < allEntities.Length; i++)
            {
                CombatEntity candidate = allEntities[i];
                if (!IsTargetInAggroRange(candidate, definition))
                    continue;

                float candidateDistance = GetDistanceToTarget(candidate);
                if (bestTarget == null || candidateDistance + 0.1f < bestDistance)
                {
                    bestTarget = candidate;
                    bestDistance = candidateDistance;
                }
            }

            if (bestTarget != null)
                return bestTarget;

            CombatEntity fallback = CombatActionDamageUtility.FindLikelyPlayerEntity(_combatant != null ? _combatant.CombatEntity : null);
            return IsTargetInAggroRange(fallback, definition) ? fallback : null;
        }

        private bool IsTargetInAggroRange(CombatEntity target, EnemyAiDefinition definition)
        {
            return IsValidTarget(target, definition)
                && GetDistanceToTarget(target) <= definition.perception.aggroRange;
        }

        private bool IsTargetWithinLoseRange(CombatEntity target, EnemyAiDefinition definition)
        {
            return IsValidTarget(target, definition)
                && GetDistanceToTarget(target) <= definition.perception.loseTargetRange;
        }

        private bool IsValidTarget(CombatEntity target, EnemyAiDefinition definition)
        {
            if (target == null)
                return false;

            if (_combatant != null && target == _combatant.CombatEntity)
                return false;

            CombatEntityData data = target.GetEntityData();
            if (data == null || !data.IsAlive || !target.gameObject.activeInHierarchy)
                return false;

            return !definition.perception.requiresLineOfSight || HasLineOfSightTo(target);
        }
    }
}
