/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using Geis.SoulRealm;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Ethereal presentation for <see cref="SoulPhaseShiftable"/> props: <c>_Dissolve</c> ping-pongs by realm (dissolve in/out);
    /// optional Perlin drift on <c>_DissolveOffest</c> / <c>_Offest</c> when <see cref="animateDissolveUvWander"/> is enabled.
    /// Prefer shader <c>Geis/SoulRealm/PhaseShiftDissolve</c> (material <c>Geis/Materials/GeisPhaseShiftDissolve_Default</c>).
    /// In the physical realm the mesh is also semi-transparent so it stays targetable but reads as "in the soul veil".
    /// <see cref="SoulPhaseShiftable"/> completes a physical pull to solid opaque presentation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulPhaseShiftPresentation : MonoBehaviour
    {
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int OffestId = Shader.PropertyToID("_Offest");
        private static readonly int DissolveOffestId = Shader.PropertyToID("_DissolveOffest");

        [Tooltip("If empty, all non-particle renderers under this object are driven.")]
        [SerializeField] private Renderer[] renderersOverride;

        [SerializeField] private float pulseSpeed = 0.72f;

        [SerializeField] private float soulDissolveMin = 0.06f;
        [SerializeField] private float soulDissolveMax = 0.78f;

        [SerializeField] private float physicalDissolveMin = 0.12f;
        [SerializeField] private float physicalDissolveMax = 0.52f;

        [Tooltip("Base color alpha in the physical realm while ethereal (transparent materials / dissolve shaders).")]
        [SerializeField] [Range(0.05f, 1f)] private float physicalEtherealAlpha = 0.38f;

        [Header("Dissolve UV wander (optional)")]
        [Tooltip("If on, Perlin-driven motion on _DissolveOffest / _Offest. Off = dissolve pulse only.")]
        [SerializeField] private bool animateDissolveUvWander;

        [SerializeField] private float uvWanderAmplitude = 0.42f;

        [Tooltip("Higher = faster roaming across the noise field.")]
        [SerializeField] private float uvWanderFrequency = 0.38f;

        [Tooltip("How much Z contributes on direction dissolve shaders (noise domain).")]
        [SerializeField] [Range(0f, 1.5f)] private float dissolveOffestZWanderScale = 0.55f;

        [Tooltip("Multiplier for _Offest (albedo UV) vs dissolve offset; keeps base map calmer than dissolve noise.")]
        [SerializeField] [Range(0f, 1f)] private float baseMapWanderScale = 0.35f;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;

        private bool _solidified;
        private float _pullProgress01;

        private float _noiseSeedX;
        private float _noiseSeedY;
        private float _noiseSeedZ;

        public bool Solidified => _solidified;

        public void SetSolidified(bool solid)
        {
            _solidified = solid;
            if (solid)
                _pullProgress01 = 1f;
        }

        /// <summary>0 = full ethereal pulse, 1 = fully solidified look (before <see cref="SetSolidified"/> locks it).</summary>
        public void SetPullProgress01(float p)
        {
            _pullProgress01 = Mathf.Clamp01(p);
        }

        public void ResetToEthereal()
        {
            _solidified = false;
            _pullProgress01 = 0f;
        }

        private void Awake()
        {
            CacheRenderers();
            InitNoiseSeedsFromInstanceId();
        }

        private void InitNoiseSeedsFromInstanceId()
        {
            int id = Mathf.Abs(gameObject.GetInstanceID());
            _noiseSeedX = (id % 997) * 0.0103f;
            _noiseSeedY = (id % 523) * 0.0131f + 19f;
            _noiseSeedZ = (id % 601) * 0.0117f + 41f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _renderers = null;
        }
#endif

        private void CacheRenderers()
        {
            if (renderersOverride != null && renderersOverride.Length > 0)
            {
                _renderers = renderersOverride;
                return;
            }

            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
                return;
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            CacheRenderers();
            if (_renderers == null || _renderers.Length == 0)
                return;

            bool soul = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;

            float pulseT = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float dMin = soul ? soulDissolveMin : physicalDissolveMin;
            float dMax = soul ? soulDissolveMax : physicalDissolveMax;
            float etherealDissolve = Mathf.Lerp(dMin, dMax, pulseT);

            float dissolve;
            float alphaMul;
            float etherealBlend;
            if (_solidified)
            {
                dissolve = 0f;
                alphaMul = 1f;
                etherealBlend = 0f;
            }
            else
            {
                float pull = _pullProgress01;
                dissolve = Mathf.Lerp(etherealDissolve, 0f, pull);
                float etherealAlpha = soul ? 1f : physicalEtherealAlpha;
                alphaMul = Mathf.Lerp(etherealAlpha, 1f, pull);
                etherealBlend = 1f - pull;
            }

            Vector3 dissolveWander = Vector3.zero;
            if (animateDissolveUvWander && etherealBlend > 1e-4f)
                dissolveWander = ComputeDissolveWander(etherealBlend);

            if (_mpb == null)
                _mpb = new MaterialPropertyBlock();

            for (var r = 0; r < _renderers.Length; r++)
            {
                var ren = _renderers[r];
                if (ren == null || ren is ParticleSystemRenderer)
                    continue;

                var mats = ren.sharedMaterials;
                if (mats == null)
                    continue;

                for (var i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    if (mat == null)
                        continue;

                    ren.GetPropertyBlock(_mpb, i);
                    if (mat.HasProperty(DissolveId))
                        _mpb.SetFloat(DissolveId, dissolve);

                    bool dissolveMaterial =
                        mat.HasProperty(DissolveId) || mat.HasProperty(DissolveOffestId);
                    if (dissolveMaterial)
                    {
                        if (animateDissolveUvWander && etherealBlend > 1e-4f)
                            ApplyWanderToMaterial(mat, dissolveWander);
                        else
                            CopyUvOffsetBaselinesFromMaterial(mat);
                    }

                    if (mat.HasProperty(BaseColorId))
                    {
                        Color c = mat.GetColor(BaseColorId);
                        c.a = Mathf.Clamp01(c.a * alphaMul);
                        _mpb.SetColor(BaseColorId, c);
                    }
                    else if (mat.HasProperty(ColorId))
                    {
                        Color c = mat.GetColor(ColorId);
                        c.a = Mathf.Clamp01(c.a * alphaMul);
                        _mpb.SetColor(ColorId, c);
                    }

                    ren.SetPropertyBlock(_mpb, i);
                }
            }
        }

        /// <summary>Smooth pseudo-random motion (Perlin); scales with ethereal blend so pull/solidify settles to authored UVs.</summary>
        private Vector3 ComputeDissolveWander(float etherealBlend)
        {
            float t = Time.time * Mathf.Max(0.02f, uvWanderFrequency);
            float a = uvWanderAmplitude * etherealBlend;

            float nx = Mathf.PerlinNoise(_noiseSeedX + t * 1.03f, _noiseSeedY + t * 0.61f);
            float ny = Mathf.PerlinNoise(_noiseSeedZ + t * 0.47f, _noiseSeedX + t * 0.88f);
            float nz = Mathf.PerlinNoise(_noiseSeedY + t * 0.73f, _noiseSeedZ + t * 0.52f);

            float wx = (nx - 0.5f) * 2f * a;
            float wy = (ny - 0.5f) * 2f * a;
            float wz = (nz - 0.5f) * 2f * a * dissolveOffestZWanderScale;
            return new Vector3(wx, wy, wz);
        }

        private void ApplyWanderToMaterial(Material mat, Vector3 wander)
        {
            if (mat.HasProperty(DissolveOffestId))
            {
                Vector4 b = mat.GetVector(DissolveOffestId);
                var baseV = new Vector3(b.x, b.y, b.z);
                _mpb.SetVector(DissolveOffestId, baseV + wander);
            }

            if (mat.HasProperty(OffestId))
            {
                Vector4 b = mat.GetVector(OffestId);
                var base2 = new Vector2(b.x, b.y);
                float m = mat.HasProperty(DissolveOffestId) ? baseMapWanderScale : 1f;
                var w2 = new Vector2(wander.x * m, wander.y * m);
                _mpb.SetVector(OffestId, new Vector4(base2.x + w2.x, base2.y + w2.y, 0f, 0f));
            }
        }

        /// <summary>Clears wander from the property block so solid / full-pull matches authored material offsets.</summary>
        private void CopyUvOffsetBaselinesFromMaterial(Material mat)
        {
            if (mat.HasProperty(DissolveOffestId))
                _mpb.SetVector(DissolveOffestId, mat.GetVector(DissolveOffestId));
            if (mat.HasProperty(OffestId))
                _mpb.SetVector(OffestId, mat.GetVector(OffestId));
        }
    }
}
