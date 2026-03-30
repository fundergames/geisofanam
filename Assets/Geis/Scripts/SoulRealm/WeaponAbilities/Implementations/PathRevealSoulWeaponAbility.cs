using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Harp-Bow secondary: reveals <see cref="SoulPathRevealElement"/> within a sphere, or all in scene when enabled.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Harp_PathReveal",
        menuName = "Geis/Soul Realm/Harp-Bow/Path Reveal")]
    public sealed class PathRevealSoulWeaponAbility : SoulWeaponAbilityAsset
    {
        [SerializeField] private float radius = 18f;
        [Tooltip("If true, every SoulPathRevealElement in the loaded scene is revealed (ignores radius).")]
        [SerializeField] private bool revealEntireScene;

        public override string AbilityDisplayName => "Path Reveal";

        public override void Activate(in SoulWeaponAbilityContext context)
        {
            if (revealEntireScene)
            {
                var all = Object.FindObjectsByType<SoulPathRevealElement>(FindObjectsSortMode.None);
                for (var i = 0; i < all.Length; i++)
                {
                    if (all[i] != null)
                        all[i].Reveal();
                }

                return;
            }

            Vector3 center = context.OriginWorld;
            var hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < hits.Length; i++)
            {
                var reveal = hits[i].GetComponentInParent<SoulPathRevealElement>();
                if (reveal != null)
                    reveal.Reveal();
            }
        }
    }
}
