using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Drives URP post via a global <see cref="Volume"/> (saturation / hue) for soul realm. Creates a runtime volume if none assigned.
    /// Requires URP cameras to have Post Processing enabled (this script turns it on when blend &gt; 0).
    /// Also drives globals for the optional fullscreen <c>Geis/Hidden/SoulRealmScreen</c> shockwave (see <see cref="PulseEntryShockwave"/>).
    /// </summary>
    public sealed class SoulRealmVisuals : MonoBehaviour
    {
        [SerializeField] private Volume volume;
        [Tooltip("Saturation at full soul realm blend (HDRP-style: negative = desaturate).")]
        [SerializeField] private float soulSaturation = -55f;
        [SerializeField] private float soulPostExposure = -0.15f;
        [SerializeField] private Color soulColorFilter = new Color(0.75f, 0.95f, 0.82f, 1f);

        [Tooltip("When soul realm is active, force Post Processing on and include this layer in volume masks.")]
        [SerializeField] private bool autoEnablePostProcessingOnCameras = true;

        [Header("Entry shockwave (fullscreen SoulRealmScreen)")]
        [Tooltip("Duration of the radial screen distortion pulse when entering soul realm.")]
        [SerializeField] private float entryShockwaveDuration = 0.38f;

        private ColorAdjustments _colorAdjustments;
        private Coroutine _entryShockwaveRoutine;
        private static readonly int SoulRealmBlendGlobalId = Shader.PropertyToID("_GeisSoulRealmBlend");
        private static readonly int ShockwaveCenterUVId = Shader.PropertyToID("_GeisShockwaveCenterUV");
        private static readonly int ShockwaveDataId = Shader.PropertyToID("_GeisShockwaveData");
        private static bool _loggedPostProcessingHint;

        private void Awake()
        {
            ClearEntryShockwaveGlobals();
            EnsureVolume();
        }

        private void EnsureVolume()
        {
            if (volume != null && volume.profile != null)
            {
                if (!volume.profile.TryGet(out _colorAdjustments))
                {
                    _colorAdjustments = volume.profile.Add<ColorAdjustments>(true);
                }

                return;
            }

            var go = new GameObject("SoulRealmVolume_Runtime");
            go.transform.SetParent(transform, false);
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 50f;
            volume.weight = 0f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _colorAdjustments = profile.Add<ColorAdjustments>(true);
            volume.profile = profile;
        }

        /// <param name="blend">0 = normal, 1 = full soul realm look.</param>
        public void SetSoulRealmBlend(float blend)
        {
            if (volume == null)
                EnsureVolume();
            if (_colorAdjustments == null || volume == null)
                return;

            blend = Mathf.Clamp01(blend);

            if (blend <= 0f)
            {
                volume.weight = 0f;
                _colorAdjustments.saturation.overrideState = false;
                _colorAdjustments.postExposure.overrideState = false;
                _colorAdjustments.colorFilter.overrideState = false;
                Shader.SetGlobalFloat(SoulRealmBlendGlobalId, 0f);
                return;
            }

            if (autoEnablePostProcessingOnCameras)
                EnsureUrpCamerasCanRenderVolumes();

            volume.weight = blend;
            volume.priority = Mathf.Max(volume.priority, 50f);

            _colorAdjustments.active = true;
            _colorAdjustments.saturation.overrideState = true;
            _colorAdjustments.postExposure.overrideState = true;
            _colorAdjustments.colorFilter.overrideState = true;
            _colorAdjustments.saturation.Override(Mathf.Lerp(0f, soulSaturation, blend));
            _colorAdjustments.postExposure.Override(Mathf.Lerp(0f, soulPostExposure, blend));
            _colorAdjustments.colorFilter.Override(Color.Lerp(Color.white, soulColorFilter, blend));

            Shader.SetGlobalFloat(SoulRealmBlendGlobalId, blend);
        }

        /// <summary>
        /// Starts a short screen-space shockwave centered on the anchor’s projected position (requires
        /// <c>Geis/Hidden/SoulRealmScreen</c> on the URP renderer feature material).
        /// </summary>
        public void PulseEntryShockwave(Transform worldAnchor, Camera camera)
        {
            if (worldAnchor == null)
                return;

            if (_entryShockwaveRoutine != null)
                StopCoroutine(_entryShockwaveRoutine);
            _entryShockwaveRoutine = StartCoroutine(EntryShockwaveRoutine(worldAnchor, camera));
        }

        private IEnumerator EntryShockwaveRoutine(Transform anchor, Camera camera)
        {
            Camera cam = camera != null ? camera : Camera.main;
            if (cam == null)
                yield break;

            float duration = Mathf.Max(0.05f, entryShockwaveDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float phase = t;
                float intensity = Mathf.Sin(t * Mathf.PI);

                Vector3 vp = cam.WorldToViewportPoint(anchor.position);
                float cx = 0.5f;
                float cy = 0.5f;
                if (vp.z > 0f)
                {
                    cx = Mathf.Clamp01(vp.x);
                    cy = Mathf.Clamp01(vp.y);
                }

                Shader.SetGlobalVector(ShockwaveCenterUVId, new Vector4(cx, cy, 0f, 0f));
                Shader.SetGlobalVector(ShockwaveDataId, new Vector4(phase, intensity, 0f, 0f));
                yield return null;
            }

            ClearEntryShockwaveGlobals();
            _entryShockwaveRoutine = null;
        }

        private static void ClearEntryShockwaveGlobals()
        {
            Shader.SetGlobalVector(ShockwaveCenterUVId, Vector4.zero);
            Shader.SetGlobalVector(ShockwaveDataId, Vector4.zero);
        }

        private void OnDestroy()
        {
            if (_entryShockwaveRoutine != null)
            {
                StopCoroutine(_entryShockwaveRoutine);
                _entryShockwaveRoutine = null;
            }

            ClearEntryShockwaveGlobals();
        }

        /// <summary>
        /// URP ignores Volume overrides unless the camera has Post Processing enabled and the volume layer is in the mask.
        /// </summary>
        private void EnsureUrpCamerasCanRenderVolumes()
        {
            int volumeLayer = volume != null ? volume.gameObject.layer : 0;
            int layerBit = 1 << volumeLayer;

#if UNITY_2023_1_OR_NEWER
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
#else
            var cameras = Object.FindObjectsOfType<Camera>();
#endif
            foreach (var cam in cameras)
            {
                if (cam == null)
                    continue;
                if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection)
                    continue;

                if (!cam.TryGetComponent<UniversalAdditionalCameraData>(out var urpCam))
                    continue;

                urpCam.renderPostProcessing = true;
                urpCam.volumeLayerMask |= layerBit;
            }

            if (!_loggedPostProcessingHint && cameras != null && cameras.Length > 0)
            {
                _loggedPostProcessingHint = true;
                bool anyUrp = false;
                foreach (var cam in cameras)
                {
                    if (cam != null && cam.TryGetComponent<UniversalAdditionalCameraData>(out _))
                    {
                        anyUrp = true;
                        break;
                    }
                }

                if (!anyUrp)
                    Debug.LogWarning(
                        "[SoulRealmVisuals] No URP cameras found (missing Universal Additional Camera Data). " +
                        "Use a URP camera, or enable the optional fullscreen SoulRealm material instead.",
                        this);
            }
        }
    }
}
