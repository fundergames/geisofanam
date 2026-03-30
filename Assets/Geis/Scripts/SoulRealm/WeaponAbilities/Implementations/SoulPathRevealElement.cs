using System.Collections.Generic;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Attach to hidden anchors or puzzle hints. <see cref="PathRevealSoulWeaponAbility"/> activates
    /// instances within range (or all registered when using global reveal on the ability asset).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulPathRevealElement : MonoBehaviour
    {
        [Tooltip("If empty, all renderers under this object are toggled.")]
        [SerializeField] private Renderer[] revealRenderers;
        [SerializeField] private Collider[] revealColliders;

        private bool _revealed;
        private readonly List<bool> _rendererWasEnabled = new List<bool>();

        public bool IsRevealed => _revealed;

        private void Awake()
        {
            CacheRenderers();
            ApplyHiddenState();
        }

        private void CacheRenderers()
        {
            if (revealRenderers != null && revealRenderers.Length > 0)
                return;
            revealRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void ApplyHiddenState()
        {
            _rendererWasEnabled.Clear();
            if (revealRenderers == null) return;
            for (var i = 0; i < revealRenderers.Length; i++)
            {
                var r = revealRenderers[i];
                if (r == null) continue;
                _rendererWasEnabled.Add(r.enabled);
                r.enabled = false;
            }

            if (revealColliders != null)
            {
                for (var i = 0; i < revealColliders.Length; i++)
                {
                    var c = revealColliders[i];
                    if (c != null)
                        c.enabled = false;
                }
            }
        }

        /// <summary>Shows meshes and optional colliders for this hint.</summary>
        public void Reveal()
        {
            if (_revealed) return;
            _revealed = true;

            if (revealRenderers != null)
            {
                for (var i = 0; i < revealRenderers.Length; i++)
                {
                    var r = revealRenderers[i];
                    if (r != null)
                        r.enabled = true;
                }
            }

            if (revealColliders != null)
            {
                for (var i = 0; i < revealColliders.Length; i++)
                {
                    var c = revealColliders[i];
                    if (c != null)
                        c.enabled = true;
                }
            }
        }

        /// <summary>Editor / reset: hide again.</summary>
        public void HideForPuzzleReset()
        {
            _revealed = false;
            if (revealRenderers == null) return;
            for (var i = 0; i < revealRenderers.Length; i++)
            {
                var r = revealRenderers[i];
                if (r != null && i < _rendererWasEnabled.Count && !_rendererWasEnabled[i])
                    r.enabled = false;
            }

            if (revealColliders != null)
            {
                for (var i = 0; i < revealColliders.Length; i++)
                {
                    var c = revealColliders[i];
                    if (c != null)
                        c.enabled = false;
                }
            }
        }
    }
}
