/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using System.Collections.Generic;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Presentation;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Forwards weapon ability sphere casts to <see cref="IPuzzleMeleeHitSink"/> volumes (e.g. <see cref="SwordHitTrigger"/>).
    /// </summary>
    public static class PuzzleMeleeHitUtility
    {
        public static void NotifySinksFromColliders(
            IEnumerable<Collider> colliders,
            SimpleAttackHitDetector source,
            CombatAction action,
            int weaponSlotIndex,
            int hitWindowIndex)
        {
            if (colliders == null)
                return;

            var notified = new HashSet<IPuzzleMeleeHitSink>();
            foreach (Collider col in colliders)
            {
                if (col == null)
                    continue;

                var sink = col.GetComponentInParent<IPuzzleMeleeHitSink>();
                if (sink == null || !notified.Add(sink))
                    continue;

                sink.OnMeleeHitFromSimpleAttack(source, action, weaponSlotIndex, hitWindowIndex);
            }
        }
    }
}
