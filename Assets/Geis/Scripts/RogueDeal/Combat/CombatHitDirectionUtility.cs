/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using UnityEngine;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Resolves which directional hit reaction to play from attacker position relative to the victim.
    /// </summary>
    public static class CombatHitDirectionUtility
    {
        /// <summary>
        /// Minimum planar distance from the victim root for <paramref name="strikeOriginWorldPosition"/> to be
        /// treated as an external strike origin. Contact points on the victim (e.g. <see cref="CombatEntity.GetHitPoint"/>)
        /// are ignored so direction comes from the attacker instead.
        /// </summary>
        private const float MinStrikeOriginSeparation = 0.5f;

        /// <summary>
        /// Maps strike origin to <see cref="CombatHitDirection"/> using the victim's facing (planar XZ).
        /// </summary>
        public static CombatHitDirection Resolve(CombatEntity attacker, CombatEntity victim)
        {
            return Resolve(attacker, victim, null);
        }

        /// <summary>
        /// Maps a world-space strike origin (attacker, weapon tip, projectile) to direction using the victim's yaw.
        /// Do not pass the victim's hurtbox / <see cref="CombatEntity.GetHitPoint"/> — that is almost always "front" locally.
        /// </summary>
        public static CombatHitDirection Resolve(
            CombatEntity attacker,
            CombatEntity victim,
            Vector3? strikeOriginWorldPosition)
        {
            if (victim == null)
                return CombatHitDirection.Front;

            Vector3 victimPos = victim.transform.position;
            Vector3? origin = null;

            if (strikeOriginWorldPosition.HasValue)
            {
                Vector3 planar = strikeOriginWorldPosition.Value - victimPos;
                planar.y = 0f;
                if (planar.sqrMagnitude >= MinStrikeOriginSeparation * MinStrikeOriginSeparation)
                    origin = strikeOriginWorldPosition.Value;
            }

            if (!origin.HasValue && attacker != null)
                origin = attacker.transform.position;

            if (!origin.HasValue)
                return CombatHitDirection.Front;

            Vector3 towardStrike = origin.Value - victimPos;
            towardStrike.y = 0f;
            if (towardStrike.sqrMagnitude < 0.0001f)
                return CombatHitDirection.Front;

            Vector3 local = victim.transform.InverseTransformDirection(towardStrike.normalized);
            return ResolveFromLocalDirection(local);
        }

        /// <summary>
        /// Victim-local direction (x = right, z = forward). Dominant axis picks F/B/L/R.
        /// </summary>
        public static CombatHitDirection ResolveFromLocalDirection(Vector3 localPlanarDirection)
        {
            localPlanarDirection.y = 0f;
            if (localPlanarDirection.sqrMagnitude < 0.0001f)
                return CombatHitDirection.Front;

            localPlanarDirection.Normalize();
            float absX = Mathf.Abs(localPlanarDirection.x);
            float absZ = Mathf.Abs(localPlanarDirection.z);

            if (absZ >= absX)
                return localPlanarDirection.z >= 0f ? CombatHitDirection.Front : CombatHitDirection.Back;

            return localPlanarDirection.x >= 0f ? CombatHitDirection.Right : CombatHitDirection.Left;
        }

        /// <summary>
        /// Synty Polygon hit reacts name the recoil direction (where the body moves), not strike origin.
        /// Hit from front → B react; from back → F; from left → R; from right → L.
        /// </summary>
        public static CombatHitDirection ToReactionDirection(CombatHitDirection strikeFrom)
        {
            switch (strikeFrom)
            {
                case CombatHitDirection.Front:
                    return CombatHitDirection.Back;
                case CombatHitDirection.Back:
                    return CombatHitDirection.Front;
                case CombatHitDirection.Left:
                    return CombatHitDirection.Right;
                case CombatHitDirection.Right:
                    return CombatHitDirection.Left;
                default:
                    return CombatHitDirection.Back;
            }
        }

        /// <summary>
        /// <see cref="HitDirection"/> int for animator transitions (0=F, 1=B, 2=L, 3=R recoil clips).
        /// </summary>
        public static int ToAnimatorInt(CombatHitDirection strikeFrom) => (int)ToReactionDirection(strikeFrom);
    }
}
