using Geis.Locomotion;
using UnityEngine;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Spectral VFX pinned to screen center while the soul-realm exit hold runs
    /// (camera returns toward the physical body). World depth follows the exit lerp pivot so the effect stays coherent.
    /// Assign a root <see cref="GameObject"/> prefab and add or edit <see cref="ParticleSystem"/> children under it (e.g. offsets on ±X).
    /// The root is parented under an internal spiral transform: it rolls around the anchor’s direction of travel;
    /// radius scales orbit distance from your authored offsets (e.g. ±1 on X).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class SoulRealmExitHoldVfx : MonoBehaviour
    {
        [SerializeField] private GeisCameraController cameraController;
        [Tooltip("Root prefab parented under the spiral transform. Put ParticleSystem children under this root (e.g. local X = ±1).")]
        [SerializeField] private GameObject exitHoldVfxPrefab;

        [Tooltip("Degrees per second: roll around the travel axis (forward = motion of the screen anchor).")]
        [SerializeField] private float spiralDegreesPerSecond = 180f;
        [Tooltip("Orbit radius in the plane perpendicular to travel. Scales your prefab offsets (e.g. children at local X = ±1). 1 = as authored in the prefab.")]
        [SerializeField] private float spiralRadius = 1f;
        [Tooltip("Clamp world depth from camera when projecting screen center.")]
        [SerializeField] private float minScreenDepth = 2.5f;
        [SerializeField] private float maxScreenDepth = 22f;

        private static GameObject _resourcesVfxPrefab;

        private Transform _screenAnchor;
        private Transform _spiralRoot;
        private GameObject _vfxInstance;
        private Transform _depthReference;
        private bool _active;

        private Vector3 _lastAnchorWorldPos;
        private Vector3 _lastTravelDir = Vector3.forward;
        private bool _hasAnchorSample;
        private float _spiralAngle;

        private void Awake()
        {
            if (cameraController == null)
                cameraController = FindFirstObjectByType<GeisCameraController>();

            var anchorGo = new GameObject("SoulRealmExitHold_ScreenAnchor");
            anchorGo.transform.SetParent(transform, false);
            _screenAnchor = anchorGo.transform;

            var spiralGo = new GameObject("SoulRealmExitHold_Spiral");
            spiralGo.transform.SetParent(_screenAnchor, false);
            _spiralRoot = spiralGo.transform;

            EnsureVfxInstance();
            if (_vfxInstance != null)
            {
                _vfxInstance.SetActive(false);
                StopAndClearAllParticles(_vfxInstance);
            }
        }

        /// <summary>Prefer wiring from <see cref="SoulRealmManager"/> so the same camera rig is used.</summary>
        public void SetCameraController(GeisCameraController controller)
        {
            if (controller != null)
                cameraController = controller;
        }

        private void LateUpdate()
        {
            if (!_active || _vfxInstance == null)
                return;

            Camera cam = ResolveCamera();
            if (cam == null || _depthReference == null)
                return;

            float depth = Vector3.Distance(cam.transform.position, _depthReference.position);
            depth = Mathf.Clamp(depth, minScreenDepth, maxScreenDepth);
            var newPos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));

            Vector3 travelDir;
            if (!_hasAnchorSample)
            {
                travelDir = cam.transform.forward;
                _hasAnchorSample = true;
            }
            else
            {
                var delta = newPos - _lastAnchorWorldPos;
                if (delta.sqrMagnitude > 1e-8f)
                    travelDir = delta.normalized;
                else
                    travelDir = _lastTravelDir.sqrMagnitude > 1e-8f ? _lastTravelDir : cam.transform.forward;
            }

            _lastTravelDir = travelDir;
            _lastAnchorWorldPos = newPos;
            _screenAnchor.position = newPos;

            Quaternion baseRot = StableLookRotation(travelDir);
            _spiralAngle += spiralDegreesPerSecond * Time.deltaTime;
            float r = Mathf.Max(spiralRadius, 0.01f);
            _spiralRoot.localScale = new Vector3(r, r, 1f);
            _spiralRoot.rotation = baseRot * Quaternion.AngleAxis(_spiralAngle, Vector3.forward);
        }

        /// <summary>Begin exit-hold VFX; <paramref name="depthReference"/> should be the camera follow pivot (ghost → body lerp).</summary>
        public void Begin(Transform depthReference)
        {
            _depthReference = depthReference;
            if (_vfxInstance == null)
                EnsureVfxInstance();
            if (_vfxInstance == null)
                return;

            _hasAnchorSample = false;
            _spiralAngle = 0f;
            _spiralRoot.localRotation = Quaternion.identity;
            float r0 = Mathf.Max(spiralRadius, 0.01f);
            _spiralRoot.localScale = new Vector3(r0, r0, 1f);

            _active = true;
            _vfxInstance.SetActive(true);
            PlayAllParticles(_vfxInstance);
        }

        public void End()
        {
            _active = false;
            _depthReference = null;
            _hasAnchorSample = false;
            if (_vfxInstance == null)
                return;

            StopAndClearAllParticles(_vfxInstance);
            _vfxInstance.SetActive(false);
            _spiralRoot.localScale = Vector3.one;
        }

        /// <summary>Look down <paramref name="forward"/>; picks a stable world up when nearly vertical.</summary>
        private static Quaternion StableLookRotation(Vector3 forward)
        {
            forward.Normalize();
            if (forward.sqrMagnitude < 1e-8f)
                return Quaternion.identity;

            Vector3 up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.98f)
                up = Vector3.right;

            return Quaternion.LookRotation(forward, up);
        }

        private Camera ResolveCamera()
        {
            if (cameraController != null && cameraController.MainCamera != null)
                return cameraController.MainCamera;
            return Camera.main;
        }

        private GameObject ResolveVfxTemplate()
        {
            if (exitHoldVfxPrefab != null)
                return exitHoldVfxPrefab;
            if (_resourcesVfxPrefab == null)
                _resourcesVfxPrefab = Resources.Load<GameObject>("VFX/SoulRealmExitHoldParticles");
            return _resourcesVfxPrefab;
        }

        private void EnsureVfxInstance()
        {
            if (_vfxInstance != null)
                return;

            var template = ResolveVfxTemplate();
            if (template != null)
            {
                _vfxInstance = Instantiate(template, _spiralRoot, false);
                _vfxInstance.name = template.name + "_Instance";
                return;
            }

            var go = new GameObject("SoulRealmExitHoldParticles_Runtime");
            go.transform.SetParent(_spiralRoot, false);
            var psGo = new GameObject("Particles_Default");
            psGo.transform.SetParent(go.transform, false);
            var ps = psGo.AddComponent<ParticleSystem>();
            SoulRealmExitHoldParticleSetup.Apply(ps);
            _vfxInstance = go;
        }

        private static void PlayAllParticles(GameObject root)
        {
            if (root == null)
                return;
            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                ps.Clear(true);
                ps.Play(true);
            }
        }

        private static void StopAndClearAllParticles(GameObject root)
        {
            if (root == null)
                return;
            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                ps.Clear(true);
            }
        }
    }
}
