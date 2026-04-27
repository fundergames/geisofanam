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

using System;
using Geis.SoulRealm;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Dagger-Flute primary: Soul Realm only. Ray-grab a <see cref="SoulBlinkable"/> to start
    /// <see cref="SoulBlinkManipulationController"/> (stand still, float with look, snap near socket).
    /// Press Q again while manipulating to cancel. Without a manipulation controller, falls back to instant swap.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Dagger_ObjectBlink",
        menuName = "Geis/Soul Realm/Dagger-Flute/Object Blink")]
    public sealed class DaggerObjectBlinkSoulWeaponAbility : SoulWeaponAbilityAsset
    {
        [SerializeField] private float maxDistance = 25f;
        [SerializeField] private LayerMask hitLayers = ~0;

        public override string AbilityDisplayName => "Object Blink";

        public override bool AllowActivationInSoulRealm => true;

        public override bool AllowActivationInPhysicalRealm => false;

        public override void Activate(in SoulWeaponAbilityContext context)
        {
            if (SoulRealmManager.Instance == null || !SoulRealmManager.Instance.IsSoulRealmActive)
                return;

            SoulBlinkManipulationController manip = ResolveManipulator(context.Owner);
            if (manip != null && manip.TryConsumePrimaryAbilityCancel())
                return;

            Ray ray = BuildCenterScreenRay(in context);

            // Single Raycast returns only the closest surface — terrain, socket triggers, or props often block the cube.
            // Walk all hits in order and grab the first SoulBlinkable along the view ray.
            if (!TryRaycastFirstSoulBlinkable(ray, maxDistance, hitLayers, out RaycastHit hit, out SoulBlinkable blink) ||
                blink == null)
                return;

            if (manip != null && manip.TryBeginManipulation(blink))
            {
                PlayDefaultActivationVfxAt(context, hit.point);
                return;
            }

            blink.Swap();
            PlayDefaultActivationVfxAt(context, hit.point);
        }

        static Ray BuildCenterScreenRay(in SoulWeaponAbilityContext context)
        {
            if (context.ViewCamera != null)
                return context.ViewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            Camera main = Camera.main;
            if (main != null)
                return main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            Vector3 o = context.OriginWorld + Vector3.up * 0.5f;
            return new Ray(o, context.ForwardWorld.sqrMagnitude > 1e-6f ? context.ForwardWorld.normalized : Vector3.forward);
        }

        static bool TryRaycastFirstSoulBlinkable(
            Ray ray,
            float maxDist,
            LayerMask mask,
            out RaycastHit hit,
            out SoulBlinkable blink)
        {
            blink = null;
            hit = default;

            RaycastHit[] hits = Physics.RaycastAll(ray, maxDist, mask, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
                return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit h in hits)
            {
                if (h.collider == null)
                    continue;
                SoulBlinkable b = h.collider.GetComponentInParent<SoulBlinkable>();
                if (b != null)
                {
                    blink = b;
                    hit = h;
                    return true;
                }
            }

            return false;
        }

        static SoulBlinkManipulationController ResolveManipulator(Transform owner)
        {
            if (owner == null)
                return null;

            SoulBlinkManipulationController c = owner.GetComponent<SoulBlinkManipulationController>()
                ?? owner.GetComponentInParent<SoulBlinkManipulationController>()
                ?? owner.GetComponentInChildren<SoulBlinkManipulationController>();
            if (c != null)
                return c;

            return owner.gameObject.AddComponent<SoulBlinkManipulationController>();
        }
    }
}
