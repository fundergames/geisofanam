using UnityEngine;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Drives _Dissolve on spectral ghost materials (noise dissolve shader). Writes both material instances and
    /// <see cref="MaterialPropertyBlock"/> per slot (merge via <see cref="Renderer.GetPropertyBlock"/> — do not
    /// <see cref="MaterialPropertyBlock.Clear"/>; that drops state and can make the mesh pop between frames).
    /// Enter: dissolve in (1 → 0). Exit hold: linear over hold duration (0 visible → 1 hidden at transition).
    /// </summary>
    public sealed class SoulSpectralDissolveDriver : MonoBehaviour
    {
        /// <summary>Match <see cref="SoulRealmManager"/> so editor hitches cannot finish the enter tween in one frame.</summary>
        private const float MaxDeltaPerFrame = 0.25f;

        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private float _dissolve = 1f;
        private float _enterElapsed;
        private float _enterDuration = 0.45f;
        private bool _enterComplete;
        private bool _invertDissolveForShader;
        /// <summary>Exit hold reached full hide; stay dissolved until ghost is destroyed (avoids one frame at 0 before realm swap).</summary>
        private bool _exitHoldReachedFullDissolve;

        /// <summary>Maps internal dissolve (1 = hidden, 0 = visible) to material _Dissolve.</summary>
        public static float ToShaderDissolve(float internalDissolve, bool invertForShader)
        {
            return invertForShader ? 1f - internalDissolve : internalDissolve;
        }

        public void Configure(float enterDissolveDuration, bool invertDissolveForShader)
        {
            _invertDissolveForShader = invertDissolveForShader;
            _enterDuration = Mathf.Max(0.2f, enterDissolveDuration);
            _enterElapsed = 0f;
            _enterComplete = false;
            _exitHoldReachedFullDissolve = false;
            _dissolve = 1f;
            EnsureRenderersCached();
            ApplyDissolve();
        }

        private void Awake()
        {
            EnsureRenderersCached();
        }

        private void EnsureRenderersCached()
        {
            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void LateUpdate()
        {
            var mgr = SoulRealmManager.Instance;
            if (mgr == null || !mgr.IsSoulRealmActive)
                return;

            if (mgr.IsSoulRealmExitHoldInProgress)
            {
                _dissolve = mgr.SoulRealmExitHoldLinearProgress01;
                _enterComplete = true;
                if (_dissolve >= 1f - 1e-4f)
                    _exitHoldReachedFullDissolve = true;
            }
            else if (!_enterComplete)
            {
                _enterElapsed += Mathf.Min(Time.deltaTime, MaxDeltaPerFrame);
                float t = Mathf.Clamp01(_enterElapsed / _enterDuration);
                _dissolve = Mathf.Lerp(1f, 0f, t);
                if (t >= 1f - 1e-4f)
                    _enterComplete = true;
            }
            else if (_exitHoldReachedFullDissolve)
                _dissolve = 1f;
            else
                _dissolve = 0f;

            ApplyDissolve();
        }

        private void ApplyDissolve()
        {
            EnsureRenderersCached();
            if (_renderers == null || _renderers.Length == 0)
                return;

            float shaderD = ToShaderDissolve(_dissolve, _invertDissolveForShader);
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
                    if (mats[i] == null)
                        continue;
                    mats[i].SetFloat(DissolveId, shaderD);
                    ren.GetPropertyBlock(_mpb, i);
                    _mpb.SetFloat(DissolveId, shaderD);
                    ren.SetPropertyBlock(_mpb, i);
                }
            }
        }
    }
}
