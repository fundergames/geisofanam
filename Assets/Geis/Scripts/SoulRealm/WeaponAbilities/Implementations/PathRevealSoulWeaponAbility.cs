using Geis.SoulRealm;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Harp-Bow secondary: pulse reveals <see cref="SoulPathRevealElement"/> in range for a short time (Soul Realm).
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Harp_PathReveal",
        menuName = "Geis/Soul Realm/Harp-Bow/Path Reveal")]
    public sealed class PathRevealSoulWeaponAbility : SoulWeaponAbilityAsset
    {
        [SerializeField] private float pulseRadius = 18f;
        [Tooltip("How long revealed hints stay visible after the pulse.")]
        [SerializeField] private float revealDurationSeconds = 4f;
        [Tooltip("If true, every SoulPathRevealElement in the loaded scene is revealed (ignores radius).")]
        [SerializeField] private bool revealEntireScene;

        public override string AbilityDisplayName => "Path Reveal";

        public override void Activate(in SoulWeaponAbilityContext context)
        {
            if (SoulRealmManager.Instance == null || !SoulRealmManager.Instance.IsSoulRealmActive)
                return;

            PlayDefaultActivationVfx(context);

            if (revealEntireScene)
            {
                var all = Object.FindObjectsByType<SoulPathRevealElement>(FindObjectsSortMode.None);
                for (var i = 0; i < all.Length; i++)
                {
                    if (all[i] != null)
                        all[i].RevealTemporary(revealDurationSeconds);
                }

                return;
            }

            Vector3 center = context.OriginWorld;
            var hits = Physics.OverlapSphere(center, pulseRadius, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < hits.Length; i++)
            {
                var reveal = hits[i].GetComponentInParent<SoulPathRevealElement>();
                if (reveal != null)
                    reveal.RevealTemporary(revealDurationSeconds);
            }
        }
    }
}
