using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Shared tint palette for puzzle elements by <see cref="PuzzleRealmMode"/> (Scene view gizmos,
    /// material property blocks, example geometry).
    /// </summary>
    public static class PuzzleRealmColors
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId     = Shader.PropertyToID("_Color");

        /// <summary>Spectral / soul-only interactions.</summary>
        public static readonly Color SoulOnly = new Color(0.55f, 0.35f, 0.95f);

        /// <summary>Physical-world-only interactions.</summary>
        public static readonly Color PhysicalOnly = new Color(0.90f, 0.44f, 0.18f);

        /// <summary>Cobalt — active in both realms or cross-realm mechanics.</summary>
        public static readonly Color BothRealms = new Color(0.13f, 0.42f, 0.82f);

        public static Color ForMode(PuzzleRealmMode mode)
        {
            return mode switch
            {
                PuzzleRealmMode.SoulOnly     => SoulOnly,
                PuzzleRealmMode.PhysicalOnly => PhysicalOnly,
                PuzzleRealmMode.BothRealms   => BothRealms,
                _                            => Color.white,
            };
        }

        public static Color SceneGizmo(PuzzleRealmMode mode, float alpha = 0.85f)
        {
            Color c = ForMode(mode);
            return new Color(c.r, c.g, c.b, alpha);
        }

        public static string LabelForMode(PuzzleRealmMode mode)
        {
            return mode switch
            {
                PuzzleRealmMode.SoulOnly     => "SOUL ONLY",
                PuzzleRealmMode.PhysicalOnly => "PHYSICAL",
                PuzzleRealmMode.BothRealms   => "BOTH REALMS",
                _                            => "",
            };
        }

        /// <summary>
        /// Sets <c>_BaseColor</c> and <c>_Color</c> on a <see cref="MaterialPropertyBlock"/> for URP/Built-in.
        /// </summary>
        public static void SetTintOnPropertyBlock(MaterialPropertyBlock mpb, PuzzleRealmMode mode)
        {
            if (mpb == null) return;
            Color c = ForMode(mode);
            mpb.SetColor(BaseColorId, c);
            mpb.SetColor(ColorId, c);
        }

        /// <summary>
        /// Applies realm tint to renderers owned by this puzzle element only (nearest
        /// <see cref="PuzzleElementBase"/> on each renderer&apos;s hierarchy is <paramref name="owner"/>).
        /// Nested puzzle elements under <paramref name="owner"/> are skipped so they keep their own realm tint.
        /// </summary>
        public static void ApplyTintToRenderers(PuzzleElementBase owner, bool includeInactive = true)
        {
            if (owner == null) return;
            var mode = owner.RealmMode;
            var renderers = owner.GetComponentsInChildren<Renderer>(includeInactive);
            for (var i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || r is ParticleSystemRenderer) continue;
                if (!RendererOwnedBy(r.transform, owner)) continue;
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                SetTintOnPropertyBlock(mpb, mode);
                r.SetPropertyBlock(mpb);
            }
        }

        /// <summary>Clears property blocks on renderers owned by this element only.</summary>
        public static void ClearTintFromRenderers(PuzzleElementBase owner, bool includeInactive = true)
        {
            if (owner == null) return;
            var renderers = owner.GetComponentsInChildren<Renderer>(includeInactive);
            for (var i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (!RendererOwnedBy(r.transform, owner)) continue;
                r.SetPropertyBlock(null);
            }
        }

        /// <summary>True if the nearest <see cref="PuzzleElementBase"/> ancestor of <paramref name="rendererTransform"/> is <paramref name="owner"/>.</summary>
        public static bool RendererOwnedBy(Transform rendererTransform, PuzzleElementBase owner)
        {
            if (rendererTransform == null || owner == null) return false;
            Transform t = rendererTransform;
            while (t != null)
            {
                var pe = t.GetComponent<PuzzleElementBase>();
                if (pe != null)
                    return pe == owner;
                t = t.parent;
            }

            return false;
        }

        /// <summary>True if the nearest <see cref="PuzzleElementBase"/> ancestor of <paramref name="colliderTransform"/> is <paramref name="owner"/>.</summary>
        public static bool ColliderOwnedBy(Transform colliderTransform, PuzzleElementBase owner)
        {
            if (colliderTransform == null || owner == null) return false;
            Transform t = colliderTransform;
            while (t != null)
            {
                var pe = t.GetComponent<PuzzleElementBase>();
                if (pe != null)
                    return pe == owner;
                t = t.parent;
            }

            return false;
        }
    }
}
