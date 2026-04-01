using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Dagger-Flute secondary: raycast to start <see cref="SoulPhaseShiftable"/> on a movable object.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Dagger_PhaseShift",
        menuName = "Geis/Soul Realm/Dagger-Flute/Phase Shift Object")]
    public sealed class DaggerPhaseShiftSoulWeaponAbility : SoulWeaponAbilityAsset
    {
        [SerializeField] private float maxDistance = 25f;
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private float phaseDurationSeconds = 6f;

        public override string AbilityDisplayName => "Phase Shift";

        public override void Activate(in SoulWeaponAbilityContext context)
        {
            Ray ray;
            if (context.ViewCamera != null)
                ray = context.ViewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            else
                ray = new Ray(context.OriginWorld + Vector3.up * 0.5f, context.ForwardWorld);

            if (!Physics.Raycast(ray, out var hit, maxDistance, hitLayers, QueryTriggerInteraction.Collide))
                return;

            var shift = hit.collider.GetComponentInParent<SoulPhaseShiftable>();
            if (shift == null)
                return;

            shift.BeginPhaseShift(phaseDurationSeconds);
            PlayDefaultActivationVfxAt(context, hit.point);
        }
    }
}
