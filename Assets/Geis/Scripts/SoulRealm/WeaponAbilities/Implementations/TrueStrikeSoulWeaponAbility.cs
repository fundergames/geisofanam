using System.Collections.Generic;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Emberblade (physical realm): forward sphere cast breaks <see cref="ITrueStrikeDestroyable"/> obstacles.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Sword_TrueStrike",
        menuName = "Geis/Soul Realm/Emberblade/True Strike")]
    public sealed class TrueStrikeSoulWeaponAbility : SoulWeaponAbilityAsset
    {
        [SerializeField] private float strikeDistance = 10f;
        [SerializeField] private float strikeRadius = 0.85f;
        [SerializeField] private LayerMask obstacleLayers = ~0;

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

            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                strikeRadius,
                dir,
                strikeDistance,
                obstacleLayers,
                QueryTriggerInteraction.Collide);

            var seen = new HashSet<int>();
            for (var i = 0; i < hits.Length; i++)
            {
                var col = hits[i].collider;
                if (col == null)
                    continue;
                int id = col.gameObject.GetInstanceID();
                if (!seen.Add(id))
                    continue;

                var destroyable = col.GetComponentInParent<ITrueStrikeDestroyable>();
                destroyable?.DestroyFromTrueStrike();
            }
        }
    }
}
