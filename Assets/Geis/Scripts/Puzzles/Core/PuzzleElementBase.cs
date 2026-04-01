using System.Collections.Generic;
using Geis.InteractInput;
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
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");

        [Header("Realm")]
        [Tooltip("Which realm this element is accessible in.")]
        [SerializeField] protected PuzzleRealmMode realmMode = PuzzleRealmMode.SoulOnly;

        [Tooltip("Tint child renderers with the realm palette via MaterialPropertyBlock (editor + play mode). Turn off to use authored materials only.")]
        [SerializeField] private bool tintRealmMaterials = true;

        [Tooltip("Use noise dissolve (Shader Graph _Dissolve) instead of disabling renderers. Child materials must expose _Dissolve, or assign Dissolve Material Template below (recommended for URP Lit). Physical-only fades out when entering soul realm; soul-only fades in.")]
        [SerializeField] private bool useRealmNoiseDissolve;

        [Tooltip("Seconds to go from full visibility to fully dissolved when entering soul realm or canceling exit hold (not used during exit hold — that matches the camera hold duration).")]
        [SerializeField] private float realmDissolveDuration = 0.45f;

        [Tooltip("If set, only these renderers receive _Dissolve. Use when meshes are not picked up by realm ownership (e.g. nested hierarchy).")]
        [SerializeField] private Renderer[] realmDissolveRenderersOverride;

        [Tooltip("Optional dissolve-capable material (e.g. Shader Graph with _Dissolve). At play, replaces child materials so realm dissolve works even on stock URP Lit. " +
                 "Required for visible dissolve on Soul Only / Physical Only unless meshes already use a _Dissolve shader. For Both Realms without this, tint-only mode is used.")]
        [SerializeField] private Material bothRealmsDissolveMaterialTemplate;

        /// <summary>Effective realm for tint, accessibility, and visibility. Override when a type is always cross-realm.</summary>
        public virtual PuzzleRealmMode RealmMode => realmMode;

        private List<Renderer> _realmRenderers;
        private List<bool> _realmRendererRestoreEnabled;
        private List<bool> _realmRendererHiddenByRealm;

        private List<Collider> _realmColliders;
        private List<bool> _realmColliderRestoreEnabled;
        private List<bool> _realmColliderHiddenByRealm;

        private bool _realmPresentationCacheBuilt;

        private MaterialPropertyBlock _realmDissolveMpb;
        private float _realmDissolveCurrent;
        private float _realmDissolveTarget;
        private bool _realmDissolveSnapOnNextUpdate = true;

        private List<Renderer> _bothRealmsSwappedRenderers;
        private List<Material[]> _bothRealmsOriginalSharedMaterials;
        private List<Material> _bothRealmsCreatedMaterials;
        private bool _bothRealmsMaterialSwapApplied;

        private void Awake()
        {
            if (useRealmNoiseDissolve)
                InitRealmDissolveFromCurrentState();
            ApplyRealmMaterialTint();
        }

        private void OnEnable()
        {
            SoulRealmManager.SoulRealmStateChanged += OnSoulRealmStateChanged;
            _realmDissolveSnapOnNextUpdate = true;
            ApplyRealmPresentation();
            if (Application.isPlaying && useRealmNoiseDissolve)
                _realmDissolveCurrent = _realmDissolveTarget;
            ApplyRealmMaterialTint();
        }

        private void OnDisable()
        {
            ReleaseBothRealmsDissolveMaterialSwap();
            SoulRealmManager.SoulRealmStateChanged -= OnSoulRealmStateChanged;
            ReleaseRealmPresentationHides();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ReleaseBothRealmsDissolveMaterialSwap();
            _realmPresentationCacheBuilt = false;
            if (useRealmNoiseDissolve)
                InitRealmDissolveFromCurrentState();
            ApplyRealmMaterialTint();
        }
#endif

        /// <summary>Re-applies realm tint after hierarchy or materials change.</summary>
        public void RefreshRealmMaterialTint()
        {
            ApplyRealmMaterialTint();
        }

        /// <summary>
        /// Call when something outside this component (e.g. soul path reveal permanent unlock) enables renderers
        /// so hide/show or noise dissolve matches the current realm.
        /// </summary>
        public void ReapplyRealmPresentationAfterExternalVisibilityChange()
        {
            if (!Application.isPlaying)
                return;
            EnsureRealmPresentationCache();
            ApplyRealmPresentation();
            if (useRealmNoiseDissolve && RealmMode != PuzzleRealmMode.BothRealms)
                _realmDissolveCurrent = _realmDissolveTarget;
            ApplyRealmMaterialTint();
        }

        private void InitRealmDissolveFromCurrentState()
        {
            EnsureRealmPresentationCache();
            bool accessible = IsAccessibleInCurrentRealm();
            float d = accessible ? 0f : 1f;
            _realmDissolveCurrent = d;
            _realmDissolveTarget = d;
        }

        private void ApplyRealmMaterialTint()
        {
            // Edit mode: Scene view should show authored materials (no realm tint/dissolve MPB). In play,
            // SoulRealmManager and dissolve paths own presentation.
            if (!Application.isPlaying)
            {
                PuzzleRealmColors.ClearTintFromRenderers(this);
                return;
            }

            if (useRealmNoiseDissolve)
            {
                if (RealmMode == PuzzleRealmMode.BothRealms && bothRealmsDissolveMaterialTemplate == null)
                {
                    if (!tintRealmMaterials)
                    {
                        PuzzleRealmColors.ClearTintFromRenderers(this);
                        return;
                    }

                    PuzzleRealmColors.ApplyTintToRenderers(this);
                    return;
                }

                EnsureRealmPresentationCache();
                ApplyRealmDissolveToRenderersAndTint();
                return;
            }

            if (!tintRealmMaterials)
            {
                PuzzleRealmColors.ClearTintFromRenderers(this);
                return;
            }

            PuzzleRealmColors.ApplyTintToRenderers(this);
        }

        private void ApplyRealmDissolveToRenderersAndTint()
        {
            if (_realmDissolveMpb == null)
                _realmDissolveMpb = new MaterialPropertyBlock();

            float d = _realmDissolveCurrent;
            for (var i = 0; i < _realmRenderers.Count; i++)
            {
                var r = _realmRenderers[i];
                if (r == null || r is ParticleSystemRenderer) continue;

                int count = r.sharedMaterials.Length;
                for (var m = 0; m < count; m++)
                {
                    r.GetPropertyBlock(_realmDissolveMpb, m);
                    _realmDissolveMpb.SetFloat(DissolveId, d);
                    bool tintViaMpb = tintRealmMaterials
                        && !(RealmMode == PuzzleRealmMode.BothRealms && _bothRealmsMaterialSwapApplied);
                    if (tintViaMpb)
                        PuzzleRealmColors.SetTintOnPropertyBlock(_realmDissolveMpb, RealmMode);
                    r.SetPropertyBlock(_realmDissolveMpb, m);
                }
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || !useRealmNoiseDissolve)
                return;

            if (RealmMode == PuzzleRealmMode.BothRealms)
                return;

            EnsureRealmPresentationCache();

            if (_realmDissolveSnapOnNextUpdate)
            {
                _realmDissolveCurrent = _realmDissolveTarget;
                _realmDissolveSnapOnNextUpdate = false;
                ApplyRealmDissolveToRenderersAndTint();
                SyncRealmDissolveCollidersAfterSnap();
                return;
            }

            var mgr = SoulRealmManager.Instance;
            if (TryApplyRealmDissolveDuringExitHold(mgr))
            {
                ApplyRealmDissolveToRenderersAndTint();
                UpdateRealmDissolveCollidersDuringLerp();
                return;
            }

            float maxDelta = realmDissolveDuration > 1e-4f ? Time.deltaTime / realmDissolveDuration : 1f;
            float prev = _realmDissolveCurrent;
            _realmDissolveCurrent = Mathf.MoveTowards(_realmDissolveCurrent, _realmDissolveTarget, maxDelta);
            if (Mathf.Approximately(prev, _realmDissolveCurrent))
            {
                UpdateRealmDissolveCollidersDuringLerp();
                return;
            }

            ApplyRealmDissolveToRenderersAndTint();
            UpdateRealmDissolveCollidersDuringLerp();
        }

        /// <summary>
        /// While returning to the physical world (exit hold), dissolve tracks the same 0–1 progress as the camera.
        /// Physical-only: fades in (dissolve 1→0). Soul-only: fades out (dissolve 0→1).
        /// </summary>
        private bool TryApplyRealmDissolveDuringExitHold(SoulRealmManager mgr)
        {
            if (mgr == null || !mgr.IsSoulRealmActive || !mgr.IsSoulRealmExitHoldInProgress)
                return false;

            float p = mgr.SoulRealmExitHoldProgress01;
            switch (RealmMode)
            {
                case PuzzleRealmMode.PhysicalOnly:
                    _realmDissolveCurrent = 1f - p;
                    return true;
                case PuzzleRealmMode.SoulOnly:
                    _realmDissolveCurrent = p;
                    return true;
                default:
                    return false;
            }
        }

        private void SyncRealmDissolveCollidersAfterSnap()
        {
            bool accessible = IsAccessibleInCurrentRealm();
            if (accessible)
                ReleaseRealmColliderHidesOnly();
            else
                ApplyRealmColliderHides();
        }

        private void UpdateRealmDissolveCollidersDuringLerp()
        {
            const float threshold = 0.02f;
            bool accessible = IsAccessibleInCurrentRealm();
            if (accessible)
            {
                if (_realmDissolveCurrent <= threshold)
                    ReleaseRealmColliderHidesOnly();
            }
            else
            {
                if (_realmDissolveCurrent >= 1f - threshold)
                    ApplyRealmColliderHides();
            }
        }

        /// <summary>
        /// Returns true when this element should be active based on the current realm state.
        /// Uses <see cref="SoulRealmManager"/> only (authoritative for visuals and dissolve). Do not use
        /// <see cref="GeisInteractInput.IsInteractRealmAllowed"/> here — that path depends on input bootstrap
        /// and can desync from soul realm presentation.
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

        /// <summary>Interact press only if this element’s realm mode allows it (matches <see cref="IsAccessibleInCurrentRealm"/>).</summary>
        protected bool WasInteractPressedThisFrameForConfiguredRealm() =>
            IsAccessibleInCurrentRealm() && GeisInteractInput.WasInteractPressedThisFrame();

        /// <summary>Interact release only if this element’s realm mode allows it.</summary>
        protected bool WasInteractReleasedThisFrameForConfiguredRealm() =>
            IsAccessibleInCurrentRealm() && GeisInteractInput.WasInteractReleasedThisFrame();

        /// <summary>Interact hold only if this element’s realm mode allows it.</summary>
        protected bool IsInteractHeldForConfiguredRealm() =>
            IsAccessibleInCurrentRealm() && GeisInteractInput.IsInteractHeld();

        /// <summary>
        /// Use for interact prompts: same rules as <see cref="IsAccessibleInCurrentRealm"/> —
        /// SoulOnly → soul realm only, PhysicalOnly → physical only, BothRealms → both.
        /// </summary>
        protected bool ShouldShowInteractPrompt() => IsAccessibleInCurrentRealm();

        /// <summary>
        /// Called when soul realm toggles; hide proximity prompts that are wrong for the new realm.
        /// </summary>
        protected virtual void OnRealmStateChangedForInteractPrompt() { }

        private void OnSoulRealmStateChanged()
        {
            _realmDissolveSnapOnNextUpdate = false;
            ApplyRealmPresentation();
            OnRealmStateChangedForInteractPrompt();
        }

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

            if (useRealmNoiseDissolve && realmDissolveRenderersOverride != null && realmDissolveRenderersOverride.Length > 0)
            {
                for (var i = 0; i < realmDissolveRenderersOverride.Length; i++)
                {
                    var r = realmDissolveRenderersOverride[i];
                    if (r == null) continue;
                    _realmRenderers.Add(r);
                    _realmRendererRestoreEnabled.Add(false);
                    _realmRendererHiddenByRealm.Add(false);
                }
            }
            else
            {
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

            TryApplyDissolveMaterialTemplateSwap();
        }

        /// <summary>
        /// Replaces child materials with dissolve shader instances so <see cref="DissolveId"/> MPB updates are visible.
        /// Previously ran only for <see cref="PuzzleRealmMode.BothRealms"/>; Soul/Physical props often use Lit without _Dissolve,
        /// so colliders followed the dissolve value while meshes did not.
        /// </summary>
        private void TryApplyDissolveMaterialTemplateSwap()
        {
            if (!Application.isPlaying)
                return;
            if (!useRealmNoiseDissolve || bothRealmsDissolveMaterialTemplate == null)
                return;
            if (_bothRealmsMaterialSwapApplied)
                return;
            if (_realmRenderers == null || _realmRenderers.Count == 0)
                return;

            _bothRealmsSwappedRenderers = new List<Renderer>();
            _bothRealmsOriginalSharedMaterials = new List<Material[]>();
            _bothRealmsCreatedMaterials = new List<Material>();

            Color realmTint = PuzzleRealmColors.ForMode(RealmMode);

            for (var i = 0; i < _realmRenderers.Count; i++)
            {
                var r = _realmRenderers[i];
                if (r == null || r is ParticleSystemRenderer)
                    continue;

                var orig = r.sharedMaterials;
                _bothRealmsSwappedRenderers.Add(r);
                _bothRealmsOriginalSharedMaterials.Add((Material[])orig.Clone());

                var clone = new Material[orig.Length];
                for (var j = 0; j < orig.Length; j++)
                {
                    var m = new Material(bothRealmsDissolveMaterialTemplate);
                    if (orig[j] != null)
                        m.CopyPropertiesFromMaterial(orig[j]);
                    m.SetFloat(DissolveId, 0f);
                    if (tintRealmMaterials)
                    {
                        if (m.HasProperty("_BaseColor"))
                            m.SetColor("_BaseColor", realmTint);
                        if (m.HasProperty("_Color"))
                            m.SetColor("_Color", realmTint);
                    }

                    _bothRealmsCreatedMaterials.Add(m);
                    clone[j] = m;
                }

                r.sharedMaterials = clone;
            }

            _bothRealmsMaterialSwapApplied = true;
        }

        private void ReleaseBothRealmsDissolveMaterialSwap()
        {
            if (!_bothRealmsMaterialSwapApplied || _bothRealmsSwappedRenderers == null || _bothRealmsOriginalSharedMaterials == null)
                return;

            for (var i = 0; i < _bothRealmsSwappedRenderers.Count; i++)
            {
                var r = _bothRealmsSwappedRenderers[i];
                if (r != null && i < _bothRealmsOriginalSharedMaterials.Count)
                    r.sharedMaterials = _bothRealmsOriginalSharedMaterials[i];
            }

            if (_bothRealmsCreatedMaterials != null)
            {
                for (var i = 0; i < _bothRealmsCreatedMaterials.Count; i++)
                {
                    var m = _bothRealmsCreatedMaterials[i];
                    if (m == null) continue;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        Object.DestroyImmediate(m);
                    else
#endif
                        Object.Destroy(m);
                }

                _bothRealmsCreatedMaterials.Clear();
            }

            _bothRealmsOriginalSharedMaterials?.Clear();
            _bothRealmsSwappedRenderers?.Clear();
            _bothRealmsMaterialSwapApplied = false;
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
            {
                if (!useRealmNoiseDissolve)
                    return;
                EnsureRealmPresentationCache();
                _realmDissolveTarget = 0f;
                _realmDissolveCurrent = 0f;
                return;
            }

            EnsureRealmPresentationCache();

            bool accessible = IsAccessibleInCurrentRealm();

            if (useRealmNoiseDissolve)
            {
                _realmDissolveTarget = accessible ? 0f : 1f;
                return;
            }

            if (accessible)
                ReleaseRealmPresentationHides();
            else
                ApplyRealmPresentationHides();
        }

        private void ApplyRealmPresentationHides()
        {
            ApplyRealmRendererHides();
            ApplyRealmColliderHides();
        }

        private void ApplyRealmRendererHides()
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
        }

        private void ApplyRealmColliderHides()
        {
            for (var i = 0; i < _realmColliders.Count; i++)
            {
                var c = _realmColliders[i];
                if (c == null) continue;
                if (_realmColliderHiddenByRealm[i])
                    continue;
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

            ReleaseRealmColliderHidesOnly();
        }

        private void ReleaseRealmColliderHidesOnly()
        {
            if (_realmColliders == null)
                return;

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
