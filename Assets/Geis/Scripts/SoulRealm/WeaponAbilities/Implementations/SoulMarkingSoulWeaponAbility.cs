using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Harp-Bow primary: screen-center ray tags an <see cref="ISoulMarkable"/> in the soul realm.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Harp_SoulMarking",
        menuName = "Geis/Soul Realm/Harp-Bow/Soul Marking")]
    public sealed class SoulMarkingSoulWeaponAbility : SoulWeaponAbilityAsset
    {
        [SerializeField] private float maxDistance = 40f;
        [SerializeField] private LayerMask hitLayers = ~0;

        public override string AbilityDisplayName => "Soul Marking";

        public override void Activate(in SoulWeaponAbilityContext context)
        {
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
                markable.ClearSoulMark();
            else
                markable.ApplySoulMark();
        }
    }
}
