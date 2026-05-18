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
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;
using UnityEngine;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Shared helpers for enemy-style damage that should flow through the same
    /// CombatAction + BaseEffect pipeline used by player attacks.
    /// </summary>
    public static class CombatActionDamageUtility
    {
        /// <summary>
        /// Attempts to find the player CombatEntity in a resilient way (tag, PlayerVisual, then fallback).
        /// </summary>
        public static CombatEntity FindLikelyPlayerEntity(CombatEntity excludedEntity = null)
        {
            if (TryFindByPlayerTag(excludedEntity, out CombatEntity taggedPlayer))
                return taggedPlayer;

            PlayerVisual playerVisual = Object.FindFirstObjectByType<PlayerVisual>();
            if (playerVisual != null)
            {
                CombatEntity visualEntity =
                    playerVisual.GetComponent<CombatEntity>()
                    ?? playerVisual.GetComponentInParent<CombatEntity>()
                    ?? playerVisual.GetComponentInChildren<CombatEntity>();

                if (visualEntity != null && visualEntity != excludedEntity)
                    return visualEntity;
            }

            CombatEntity[] allEntities = Object.FindObjectsByType<CombatEntity>(FindObjectsSortMode.None);
            CombatEntity fallback = null;
            for (int i = 0; i < allEntities.Length; i++)
            {
                CombatEntity entity = allEntities[i];
                if (entity == null || entity == excludedEntity)
                    continue;

                if (HasTag(entity.gameObject, "Player"))
                    return entity;

                if (entity.GetComponent<PlayerVisual>() != null
                    || entity.GetComponentInParent<PlayerVisual>() != null
                    || entity.GetComponentInChildren<PlayerVisual>() != null)
                    return entity;

                if (fallback == null)
                    fallback = entity;
            }

            return fallback;
        }

        /// <summary>
        /// Collects unique alive CombatEntity targets in a sphere, optionally filtered by tags.
        /// </summary>
        public static List<CombatEntity> CollectTargetsInSphere(
            Vector3 center,
            float radius,
            LayerMask targetLayers,
            CombatEntity sourceEntity = null,
            string[] requiredTags = null,
            CombatEntity explicitCandidate = null)
        {
            var results = new List<CombatEntity>();
            var seen = new HashSet<CombatEntity>();

            Collider[] colliders = Physics.OverlapSphere(center, radius, targetLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                CombatEntity entity = collider.GetComponent<CombatEntity>() ?? collider.GetComponentInParent<CombatEntity>();
                if (!TryAddCandidate(entity, sourceEntity, requiredTags, seen, results))
                    continue;
            }

            if (explicitCandidate != null
                && Vector3.Distance(center, explicitCandidate.transform.position) <= radius)
            {
                TryAddCandidate(explicitCandidate, sourceEntity, requiredTags, seen, results);
            }

            return results;
        }

        /// <summary>
        /// Applies a CombatAction's effects from <paramref name="sourceEntity"/> to each target.
        /// </summary>
        public static bool ApplyActionToTargets(
            CombatEntity sourceEntity,
            CombatAction action,
            IReadOnlyList<CombatEntity> targets)
        {
            if (action == null || action.effects == null || action.effects.Length == 0)
                return false;

            CombatStrikeKind kind = CombatStrikeResolver.ResolveStrikeKind(action, CombatStrikeKind.Melee);
            return ApplyEffectsToTargets(sourceEntity, action.effects, targets, kind, action);
        }

        /// <summary>
        /// Applies raw effect arrays while still publishing standard combat events.
        /// </summary>
        public static bool ApplyEffectsToTargets(
            CombatEntity sourceEntity,
            BaseEffect[] effects,
            IReadOnlyList<CombatEntity> targets,
            CombatStrikeKind strikeKind = CombatStrikeKind.Melee,
            CombatAction action = null)
        {
            if (sourceEntity == null || effects == null || effects.Length == 0 || targets == null || targets.Count == 0)
                return false;

            if (sourceEntity.GetEntityData() == null)
                return false;

            CombatStrikeKind kind = action != null
                ? CombatStrikeResolver.ResolveStrikeKind(action, strikeKind)
                : strikeKind;

            float maxRange = CombatStrikeResolver.GetMaxMeleeRange(sourceEntity.GetEntityData());
            bool dealtAnyDamage = false;

            for (int i = 0; i < targets.Count; i++)
            {
                CombatEntity target = targets[i];
                if (target == null || target == sourceEntity)
                    continue;

                float damage = CombatStrikeResolver.TryApplyEffectsToTarget(
                    kind,
                    sourceEntity,
                    target,
                    effects,
                    action,
                    kind == CombatStrikeKind.Melee ? maxRange : -1f);

                if (damage > 0f)
                    dealtAnyDamage = true;
            }

            return dealtAnyDamage;
        }

        private static bool TryFindByPlayerTag(CombatEntity excludedEntity, out CombatEntity playerEntity)
        {
            playerEntity = null;

            GameObject taggedPlayer;
            try
            {
                taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            }
            catch (UnityException)
            {
                return false;
            }

            if (taggedPlayer == null)
                return false;

            CombatEntity entity =
                taggedPlayer.GetComponent<CombatEntity>()
                ?? taggedPlayer.GetComponentInParent<CombatEntity>()
                ?? taggedPlayer.GetComponentInChildren<CombatEntity>();

            if (entity == null || entity == excludedEntity)
                return false;

            playerEntity = entity;
            return true;
        }

        private static bool TryAddCandidate(
            CombatEntity candidate,
            CombatEntity sourceEntity,
            string[] requiredTags,
            HashSet<CombatEntity> seen,
            List<CombatEntity> results)
        {
            if (candidate == null || candidate == sourceEntity || seen.Contains(candidate))
                return false;

            CombatEntityData data = candidate.GetEntityData();
            if (data == null || !data.IsAlive)
                return false;

            if (!PassesTagFilter(candidate.gameObject, requiredTags))
                return false;

            seen.Add(candidate);
            results.Add(candidate);
            return true;
        }

        private static bool PassesTagFilter(GameObject target, string[] requiredTags)
        {
            if (requiredTags == null || requiredTags.Length == 0)
                return true;

            for (int i = 0; i < requiredTags.Length; i++)
            {
                string tag = requiredTags[i];
                if (!string.IsNullOrEmpty(tag) && HasTag(target, tag))
                    return true;
            }

            return false;
        }

        private static bool HasTag(GameObject gameObject, string tag)
        {
            if (gameObject == null || string.IsNullOrEmpty(tag))
                return false;

            try
            {
                return gameObject.CompareTag(tag);
            }
            catch (UnityException)
            {
                return false;
            }
        }
    }
}
