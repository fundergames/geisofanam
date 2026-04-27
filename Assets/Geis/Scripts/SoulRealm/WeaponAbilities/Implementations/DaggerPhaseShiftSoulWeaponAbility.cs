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
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Dagger-Flute secondary: in Soul Realm, raycast once to run timed <see cref="SoulPhaseShiftable.BeginPhaseShift"/>.
    /// In the physical realm, hold secondary to pull the object from the ethereal presentation into a solid state
    /// (see <see cref="SoulPhaseShiftPresentation"/> on the prop).
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Dagger_PhaseShift",
        menuName = "Geis/Soul Realm/Dagger-Flute/Phase Shift Object")]
    public sealed class DaggerPhaseShiftSoulWeaponAbility : SoulWeaponAbilityAsset, ISoulWeaponSecondaryHoldTick
    {
        [SerializeField] private float maxDistance = 25f;
        [SerializeField] private LayerMask hitLayers = ~0;

        [Tooltip("Seconds phased colliders stay on the phased layer. 0 or less = use Default Duration Seconds on each SoulPhaseShiftable.")]
        [SerializeField] private float phaseDurationSeconds;

        [Header("Physical realm — hold secondary to solidify")]
        [SerializeField] private float secondsToSolidify = 1.35f;
        [SerializeField] private float pullReleaseDecaySeconds = 0.5f;

        public override string AbilityDisplayName => "Phase Shift";

        public override bool AllowActivationInSoulRealm => true;

        public override bool AllowActivationInPhysicalRealm => true;

        public override bool ShowActivationFeedback(in SoulWeaponAbilityContext context) =>
            SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;

        private static SoulPhaseShiftable s_pullTarget;
        private static float s_pullProgress01;

        /// <summary>Clears in-progress pull when the ability map turns off or the weapon changes.</summary>
        public static void CancelOngoingPhysicalPullIfAny()
        {
            if (s_pullTarget != null)
                s_pullTarget.ClearPhysicalPullVisual();
            s_pullTarget = null;
            s_pullProgress01 = 0f;
        }

        public override void Activate(in SoulWeaponAbilityContext context)
        {
            if (SoulRealmManager.Instance == null || !SoulRealmManager.Instance.IsSoulRealmActive)
                return;

            if (!TryRaycastShiftable(in context, out RaycastHit hit, out SoulPhaseShiftable shift) || shift == null)
                return;

            float duration = phaseDurationSeconds > 0f ? phaseDurationSeconds : -1f;
            shift.BeginPhaseShift(duration);
            PlayDefaultActivationVfxAt(context, hit.point);
        }

        public void TickSecondaryWhileAbilityMapEnabled(in SoulWeaponAbilityContext context, bool ability2Held)
        {
            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive)
            {
                CancelOngoingPhysicalPullIfAny();
                return;
            }

            float solidify = Mathf.Max(0.05f, secondsToSolidify);

            if (!ability2Held)
            {
                DecayPullTowardsZero();
                return;
            }

            if (!TryRaycastShiftable(in context, out RaycastHit hit, out SoulPhaseShiftable shift) ||
                shift == null ||
                shift.IsPhysicalSolidified)
            {
                DecayPullTowardsZero();
                return;
            }

            if (s_pullTarget != shift)
            {
                if (s_pullTarget != null)
                    s_pullTarget.ClearPhysicalPullVisual();
                s_pullTarget = shift;
                s_pullProgress01 = 0f;
            }

            s_pullProgress01 = Mathf.Clamp01(s_pullProgress01 + Time.deltaTime / solidify);
            s_pullTarget.SetPhysicalPullProgress01(s_pullProgress01);

            if (s_pullProgress01 < 1f - 1e-4f)
                return;

            s_pullTarget.CompletePhysicalSolidify();
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
            s_pullTarget.SetPhysicalPullProgress01(s_pullProgress01);
            if (s_pullProgress01 > 1e-4f)
                return;

            s_pullTarget.ClearPhysicalPullVisual();
            s_pullTarget = null;
            s_pullProgress01 = 0f;
        }

        private bool TryRaycastShiftable(in SoulWeaponAbilityContext context, out RaycastHit hit,
            out SoulPhaseShiftable shift)
        {
            Ray ray = BuildCenterScreenRay(in context);
            if (!Physics.Raycast(ray, out hit, maxDistance, hitLayers, QueryTriggerInteraction.Collide))
            {
                shift = null;
                return false;
            }

            shift = hit.collider.GetComponentInParent<SoulPhaseShiftable>();
            return shift != null;
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
