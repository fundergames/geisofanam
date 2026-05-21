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
using Geis.Puzzles;
using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Emberblade (physical realm): forward sphere cast breaks <see cref="ITrueStrikeDestroyable"/> obstacles
    /// and can register hits on <see cref="SwordHitTrigger"/> puzzle volumes.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Sword_TrueStrike",
        menuName = "Funder Games/Geis/Soul Realm/Emberblade/True Strike")]
    public sealed class TrueStrikeSoulWeaponAbility : SoulWeaponAbilityAsset
    {
        [SerializeField] private float strikeDistance = 10f;
        [SerializeField] private float strikeRadius = 0.85f;
        [SerializeField] private LayerMask obstacleLayers = ~0;

        [Header("Puzzles")]
        [Tooltip("Notify IPuzzleMeleeHitSink (e.g. SwordHitTrigger) along the strike path.")]
        [SerializeField] private bool notifySwordPuzzleVolumes = true;

        public override string AbilityDisplayName => "True Strike";

        public override bool AllowActivationInSoulRealm => false;

        public override bool AllowActivationInPhysicalRealm => true;

        public override void Activate(in SoulWeaponAbilityContext context)
        {
            PlayDefaultActivationVfx(context);

            Vector3 origin = context.OriginWorld + Vector3.up * 0.5f;
            Vector3 dir = context.ForwardWorld;
            if (dir.sqrMagnitude < 1e-4f)
                dir = Vector3.forward;
            dir.Normalize();

            var seen = new HashSet<int>();
            var puzzleCols = new List<Collider>();

            CollectCollider(
                Physics.OverlapSphere(origin, strikeRadius, obstacleLayers, QueryTriggerInteraction.Collide),
                seen,
                puzzleCols,
                notifySwordPuzzleVolumes);

            RaycastHit[] castHits = Physics.SphereCastAll(
                origin,
                strikeRadius,
                dir,
                strikeDistance,
                obstacleLayers,
                QueryTriggerInteraction.Collide);
            for (var i = 0; i < castHits.Length; i++)
            {
                var col = castHits[i].collider;
                if (col == null)
                    continue;
                int id = col.gameObject.GetInstanceID();
                if (!seen.Add(id))
                    continue;

                RegisterStrikeHit(col, puzzleCols, notifySwordPuzzleVolumes);
            }

            if (notifySwordPuzzleVolumes && puzzleCols.Count > 0)
            {
                CombatAction puzzleAction = context.WeaponDefinition != null
                    ? context.WeaponDefinition.GetCombatAction()
                    : null;
                PuzzleMeleeHitUtility.NotifySinksFromColliders(
                    puzzleCols,
                    null,
                    puzzleAction,
                    context.WeaponSlotIndex,
                    1);
            }
        }

        private static void CollectCollider(
            Collider[] colliders,
            HashSet<int> seen,
            List<Collider> puzzleCols,
            bool notifySwordPuzzleVolumes)
        {
            if (colliders == null)
                return;

            for (var i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (col == null)
                    continue;
                int id = col.gameObject.GetInstanceID();
                if (!seen.Add(id))
                    continue;

                RegisterStrikeHit(col, puzzleCols, notifySwordPuzzleVolumes);
            }
        }

        private static void RegisterStrikeHit(
            Collider col,
            List<Collider> puzzleCols,
            bool notifySwordPuzzleVolumes)
        {
            var destroyable = col.GetComponentInParent<ITrueStrikeDestroyable>();
            if (destroyable != null)
            {
                destroyable.DestroyFromTrueStrike();
                return;
            }

            if (notifySwordPuzzleVolumes)
                puzzleCols.Add(col);
        }
    }
}
