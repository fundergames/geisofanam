using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Dagger-Flute primary: raycast from view center; toggles <see cref="SoulBlinkable"/> on hit.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Dagger_ObjectBlink",
        menuName = "Geis/Soul Realm/Dagger-Flute/Object Blink")]
    public sealed class DaggerObjectBlinkSoulWeaponAbility : SoulWeaponAbilityAsset
    {
        [SerializeField] private float maxDistance = 25f;
        [SerializeField] private LayerMask hitLayers = ~0;

        public override string AbilityDisplayName => "Object Blink";

        public override void Activate(in SoulWeaponAbilityContext context)
        {
            Ray ray;
            if (context.ViewCamera != null)
                ray = context.ViewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            else
                ray = new Ray(context.OriginWorld + Vector3.up * 0.5f, context.ForwardWorld);

            if (!Physics.Raycast(ray, out var hit, maxDistance, hitLayers, QueryTriggerInteraction.Collide))
                return;

            var blink = hit.collider.GetComponentInParent<SoulBlinkable>();
            if (blink == null)
                return;

            blink.Swap();
        }
    }
}
