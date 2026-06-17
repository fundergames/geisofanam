/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using Geis.Locomotion;
using UnityEngine;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Procedural impact shake on the third-person camera rig (runs after <see cref="GeisCameraController"/>).
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class CombatCameraShake : MonoBehaviour
    {
        private Transform _cameraRig;
        private float _remainingDuration;
        private float _totalDuration;
        private float _amplitude;
        private float _frequency;
        private float _noiseSeed;

        private void Awake()
        {
            CacheCameraRig();
        }

        public void RequestShake(CombatCameraShakeSpec spec)
        {
            if (!spec.enabled || spec.amplitude <= 0f || spec.duration <= 0f)
                return;

            CacheCameraRig();
            if (_cameraRig == null)
                return;

            if (spec.amplitude >= _amplitude || _remainingDuration <= 0f)
            {
                _amplitude = spec.amplitude;
                _frequency = Mathf.Max(0.1f, spec.frequency);
                _totalDuration = spec.duration;
                _remainingDuration = spec.duration;
                _noiseSeed = Random.value * 1000f;
            }
            else
            {
                _remainingDuration = Mathf.Max(_remainingDuration, spec.duration);
            }
        }

        private void LateUpdate()
        {
            if (_cameraRig == null)
            {
                CacheCameraRig();
                if (_cameraRig == null)
                    return;
            }

            if (_remainingDuration <= 0f)
                return;

            float decay = _totalDuration > 1e-4f ? Mathf.Clamp01(_remainingDuration / _totalDuration) : 0f;
            float t = Time.unscaledTime * _frequency;
            float nx = (Mathf.PerlinNoise(_noiseSeed, t) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(_noiseSeed + 17f, t) - 0.5f) * 2f;
            float nz = (Mathf.PerlinNoise(_noiseSeed + 31f, t) - 0.5f) * 2f;

            Vector3 offset = new Vector3(nx, ny, nz * 0.35f) * (_amplitude * decay);
            _cameraRig.localPosition += offset;

            _remainingDuration -= Time.unscaledDeltaTime;
            if (_remainingDuration <= 0f)
            {
                _remainingDuration = 0f;
                _amplitude = 0f;
            }
        }

        private void CacheCameraRig()
        {
            if (_cameraRig != null)
                return;

            if (transform.childCount > 0)
                _cameraRig = transform.GetChild(0);
        }

        public static CombatCameraShake FindOrCreate()
        {
            GeisCameraController controller = Object.FindFirstObjectByType<GeisCameraController>();
            if (controller == null)
                return null;

            CombatCameraShake shake = controller.GetComponent<CombatCameraShake>();
            if (shake == null)
                shake = controller.gameObject.AddComponent<CombatCameraShake>();

            return shake;
        }
    }
}
