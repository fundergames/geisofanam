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

using Geis.SoulRealm;
using System;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Dagger-Flute secondary: hold to shift the targeted <see cref="SoulPhaseShiftable"/> into the current realm.
    /// The object stays solid in that realm and becomes ethereal in the opposite realm until shifted back.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Dagger_PhaseShift",
        menuName = "Funder Games/Geis/Soul Realm/Dagger-Flute/Phase Shift Object")]
    public sealed class DaggerPhaseShiftSoulWeaponAbility : SoulWeaponAbilityAsset, ISoulWeaponSecondaryHoldTick
    {
        [SerializeField] private float maxDistance = 25f;
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Hold secondary to shift into the current realm")]
        [SerializeField] private float secondsToShiftRealm = 1.35f;
        [SerializeField] private float pullReleaseDecaySeconds = 0.5f;

        public override string AbilityDisplayName => "Phase Shift";

        public override bool AllowActivationInSoulRealm => true;

        public override bool AllowActivationInPhysicalRealm => true;

        public override bool ShowActivationFeedback(in SoulWeaponAbilityContext context) => false;

        private static SoulPhaseShiftable s_pullTarget;
        private static float s_pullProgress01;

        /// <summary>Clears in-progress hold when the ability map turns off or the weapon changes.</summary>
        public static void CancelOngoingShiftIfAny()
        {
            if (s_pullTarget != null)
                s_pullTarget.ClearShiftPullVisual();
            s_pullTarget = null;
            s_pullProgress01 = 0f;
        }

        public override void Activate(in SoulWeaponAbilityContext context)
        {
            // Secondary uses the hold tick path so it can shift objects into either realm.
        }

        public void TickSecondaryWhileAbilityMapEnabled(in SoulWeaponAbilityContext context, bool ability2Held)
        {
            float solidify = Mathf.Max(0.05f, secondsToShiftRealm);

            if (!ability2Held)
            {
                DecayPullTowardsZero();
                return;
            }

            if (!TryRaycastShiftable(in context, out RaycastHit hit, out SoulPhaseShiftable shift) ||
                shift == null ||
                shift.IsSolidInCurrentRealm)
            {
                DecayPullTowardsZero();
                return;
            }

            if (s_pullTarget != shift)
            {
                if (s_pullTarget != null)
                    s_pullTarget.ClearShiftPullVisual();
                s_pullTarget = shift;
                s_pullProgress01 = 0f;
            }

            s_pullProgress01 = Mathf.Clamp01(s_pullProgress01 + Time.deltaTime / solidify);
            s_pullTarget.SetShiftPullProgress01(s_pullProgress01);

            if (s_pullProgress01 < 1f - 1e-4f)
                return;

            s_pullTarget.ShiftSolidRealmToCurrentRealm();
            PlayDefaultActivationVfxAt(context, hit.point);
            s_pullTarget = null;
            s_pullProgress01 = 0f;
        }

        private void DecayPullTowardsZero()
        {
            if (s_pullTarget == null || s_pullProgress01 <= 0f)
                return;

            float rate = pullReleaseDecaySeconds > 1e-4f ? Time.deltaTime / pullReleaseDecaySeconds : 1f;
            s_pullProgress01 = Mathf.MoveTowards(s_pullProgress01, 0f, rate);
            s_pullTarget.SetShiftPullProgress01(s_pullProgress01);
            if (s_pullProgress01 > 1e-4f)
                return;

            s_pullTarget.ClearShiftPullVisual();
            s_pullTarget = null;
            s_pullProgress01 = 0f;
        }

        private bool TryRaycastShiftable(in SoulWeaponAbilityContext context, out RaycastHit hit,
            out SoulPhaseShiftable shift)
        {
            Ray ray = BuildCenterScreenRay(in context);
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, hitLayers, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
            {
                hit = default;
                shift = null;
                return false;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (var i = 0; i < hits.Length; i++)
            {
                RaycastHit h = hits[i];
                if (h.collider == null)
                    continue;

                SoulPhaseShiftable candidate = h.collider.GetComponentInParent<SoulPhaseShiftable>();
                if (candidate == null)
                    continue;

                hit = h;
                shift = candidate;
                return true;
            }

            hit = default;
            shift = null;
            return false;
        }

        private static Ray BuildCenterScreenRay(in SoulWeaponAbilityContext context)
        {
            if (context.ViewCamera != null)
                return context.ViewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            Camera main = Camera.main;
            if (main != null)
                return main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            Vector3 o = context.OriginWorld + Vector3.up * 0.5f;
            Vector3 dir = context.ForwardWorld.sqrMagnitude > 1e-6f ? context.ForwardWorld.normalized : Vector3.forward;
            return new Ray(o, dir);
        }
    }
}
