using Geis.SoulRealm;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Harp-Bow primary: screen-center ray tags an <see cref="ISoulMarkable"/> in the soul realm;
    /// the next N physical-realm bow shots home toward that mark.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Harp_SoulMarking",
        menuName = "Geis/Soul Realm/Harp-Bow/Soul Marking")]
    public sealed class SoulMarkingSoulWeaponAbility : SoulWeaponAbilityAsset
    {
        [SerializeField] private float maxDistance = 40f;
        [SerializeField] private LayerMask hitLayers = ~0;
        [Tooltip("Bow shots that steer toward the mark after tagging. 0 = unlimited until the mark is cleared or you tag again.")]
        [SerializeField] private int homingShotsAfterMark = 0;

        public override string AbilityDisplayName => "Soul Marking";

        public override void Activate(in SoulWeaponAbilityContext context)
        {
            if (SoulRealmManager.Instance == null || !SoulRealmManager.Instance.IsSoulRealmActive)
                return;

            var tracker = context.Owner != null
                ? context.Owner.GetComponentInParent<SoulMarkHomingTracker>()
                : null;

            Ray ray;
            if (context.ViewCamera != null)
            {
                ray = context.ViewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            }
            else
            {
                Vector3 o = context.OriginWorld;
                ray = new Ray(o + Vector3.up * 0.5f, context.ForwardWorld);
            }

            if (!Physics.Raycast(ray, out var hit, maxDistance, hitLayers, QueryTriggerInteraction.Collide))
                return;

            var markable = hit.collider.GetComponentInParent<ISoulMarkable>();
            if (markable == null)
                return;

            if (markable.IsSoulMarked)
            {
                markable.ClearSoulMark();
                tracker?.ClearSoulMarkHoming();
            }
            else
            {
                markable.ApplySoulMark();
                if (tracker != null && markable.MarkTransform != null)
                    tracker.RegisterSoulMark(markable.MarkTransform, homingShotsAfterMark);
            }

            PlayDefaultActivationVfxAt(context, hit.point);
        }
    }
}
