using System.Collections.Generic;
using Geis.SoulRealm;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Base for all puzzle triggers and outputs. Handles realm-gating so individual
    /// subclasses don't need to repeat the realm check.
    /// </summary>
    [ExecuteAlways]
    public abstract class PuzzleElementBase : MonoBehaviour
    {
        [Header("Realm")]
        [Tooltip("Which realm this element is accessible in.")]
        [SerializeField] private PuzzleRealmMode realmMode = PuzzleRealmMode.SoulOnly;

        [Tooltip("Tint child renderers with the realm palette via MaterialPropertyBlock (editor + play mode). Turn off to use authored materials only.")]
        [SerializeField] private bool tintRealmMaterials = true;

        /// <summary>Effective realm for tint, accessibility, and visibility. Override when a type is always cross-realm.</summary>
        public virtual PuzzleRealmMode RealmMode => realmMode;

        private List<Renderer> _realmRenderers;
        private List<bool> _realmRendererRestoreEnabled;
        private List<bool> _realmRendererHiddenByRealm;

        private List<Collider> _realmColliders;
        private List<bool> _realmColliderRestoreEnabled;
        private List<bool> _realmColliderHiddenByRealm;

        private bool _realmPresentationCacheBuilt;

        private void Awake()
        {
            ApplyRealmMaterialTint();
        }

        private void OnEnable()
        {
            SoulRealmManager.SoulRealmStateChanged += OnSoulRealmStateChanged;
            ApplyRealmMaterialTint();
            ApplyRealmPresentation();
        }

        private void OnDisable()
        {
            SoulRealmManager.SoulRealmStateChanged -= OnSoulRealmStateChanged;
            ReleaseRealmPresentationHides();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyRealmMaterialTint();
        }
#endif

        /// <summary>Re-applies realm tint after hierarchy or materials change.</summary>
        public void RefreshRealmMaterialTint()
        {
            ApplyRealmMaterialTint();
        }

        private void ApplyRealmMaterialTint()
        {
            if (!tintRealmMaterials)
            {
                PuzzleRealmColors.ClearTintFromRenderers(this);
                return;
            }

            PuzzleRealmColors.ApplyTintToRenderers(this);
        }

        /// <summary>
        /// Returns true when this element should be active based on the current realm state.
        /// </summary>
        protected bool IsAccessibleInCurrentRealm()
        {
            bool soulActive = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
            return RealmMode switch
            {
                PuzzleRealmMode.SoulOnly     => soulActive,
                PuzzleRealmMode.PhysicalOnly => !soulActive,
                PuzzleRealmMode.BothRealms   => true,
                _                            => false,
            };
        }

        private void OnSoulRealmStateChanged() => ApplyRealmPresentation();

        private void EnsureRealmPresentationCache()
        {
            if (_realmPresentationCacheBuilt)
                return;
            _realmPresentationCacheBuilt = true;

            _realmRenderers = new List<Renderer>();
            _realmRendererRestoreEnabled = new List<bool>();
            _realmRendererHiddenByRealm = new List<bool>();

            _realmColliders = new List<Collider>();
            _realmColliderRestoreEnabled = new List<bool>();
            _realmColliderHiddenByRealm = new List<bool>();

            var renderers = GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (!PuzzleRealmColors.RendererOwnedBy(r.transform, this)) continue;
                _realmRenderers.Add(r);
                _realmRendererRestoreEnabled.Add(false);
                _realmRendererHiddenByRealm.Add(false);
            }

            var colliders = GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                var c = colliders[i];
                if (c == null) continue;
                if (!PuzzleRealmColors.ColliderOwnedBy(c.transform, this)) continue;
                _realmColliders.Add(c);
                _realmColliderRestoreEnabled.Add(false);
                _realmColliderHiddenByRealm.Add(false);
            }
        }

        /// <summary>
        /// Hides renderers and disables colliders when this element is not active in the current realm.
        /// Restore values are captured at hide time so gameplay (e.g. dissolved barriers) is not overwritten.
        /// </summary>
        private void ApplyRealmPresentation()
        {
            if (!Application.isPlaying)
                return;

            if (RealmMode == PuzzleRealmMode.BothRealms)
                return;

            EnsureRealmPresentationCache();

            bool accessible = IsAccessibleInCurrentRealm();
            if (accessible)
                ReleaseRealmPresentationHides();
            else
                ApplyRealmPresentationHides();
        }

        private void ApplyRealmPresentationHides()
        {
            for (var i = 0; i < _realmRenderers.Count; i++)
            {
                var r = _realmRenderers[i];
                if (r == null) continue;
                if (!r.enabled)
                {
                    _realmRendererHiddenByRealm[i] = false;
                    continue;
                }

                _realmRendererRestoreEnabled[i] = r.enabled;
                r.enabled = false;
                _realmRendererHiddenByRealm[i] = true;
            }

            for (var i = 0; i < _realmColliders.Count; i++)
            {
                var c = _realmColliders[i];
                if (c == null) continue;
                if (!c.enabled)
                {
                    _realmColliderHiddenByRealm[i] = false;
                    continue;
                }

                _realmColliderRestoreEnabled[i] = c.enabled;
                c.enabled = false;
                _realmColliderHiddenByRealm[i] = true;
            }
        }

        private void ReleaseRealmPresentationHides()
        {
            if (_realmRenderers == null)
                return;

            for (var i = 0; i < _realmRenderers.Count; i++)
            {
                if (!_realmRendererHiddenByRealm[i])
                    continue;
                var r = _realmRenderers[i];
                if (r != null)
                    r.enabled = _realmRendererRestoreEnabled[i];
                _realmRendererHiddenByRealm[i] = false;
            }

            for (var i = 0; i < _realmColliders.Count; i++)
            {
                if (!_realmColliderHiddenByRealm[i])
                    continue;
                var c = _realmColliders[i];
                if (c != null)
                    c.enabled = _realmColliderRestoreEnabled[i];
                _realmColliderHiddenByRealm[i] = false;
            }
        }
    }
}
