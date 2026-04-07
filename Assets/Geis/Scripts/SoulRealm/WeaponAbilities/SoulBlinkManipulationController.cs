using System;
using Geis.InteractInput;
using Geis.Locomotion;
using Geis.SoulRealm;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Object Blink (Dagger Q): after ray-grabbing a <see cref="SoulBlinkable"/>, the ghost stands still
    /// (interaction movement freeze) while the object follows the camera look; when it enters the socket
    /// radius it snaps. Press Q again to cancel and return the object to pose A.
    /// Add this on the same hierarchy as <see cref="SoulRealmWeaponAbilityController"/> (e.g. ghost root);
    /// if missing, <see cref="DaggerObjectBlinkSoulWeaponAbility"/> adds it at runtime on the ability owner.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulBlinkManipulationController : MonoBehaviour
    {
        [SerializeField] private GeisCameraController cameraController;

        [Header("Float")]
        [Tooltip("Distance along the view ray when nothing is hit (m).")]
        [SerializeField] private float floatDistance = 2.5f;

        [Tooltip("Max raycast to place the object on surfaces (m).")]
        [SerializeField] private float maxRaycastDistance = 12f;

        [SerializeField] private float surfaceOffset = 0.05f;

        [SerializeField] private LayerMask placementRaycastMask = ~0;

        [Tooltip("Match camera yaw so the cube stays upright.")]
        [SerializeField] private bool alignYawToCamera = true;

        [Header("VFX")]
        [Tooltip("One-shot burst when you grab (screen-center hit).")]
        [SerializeField] private Color grabBurstColor = new Color(0.35f, 0.95f, 1f, 1f);

        [Tooltip("Looping carry effect when the blinkable has no per-object carry prefab.")]
        [SerializeField] private GameObject defaultCarryLoopVfxPrefab;

        [SerializeField] private bool proceduralCarryLoopIfNoPrefab = true;

        [SerializeField] private Color proceduralCarryColor = new Color(0.4f, 0.88f, 1f, 1f);

        [Tooltip("Burst when you cancel (Q) while floating.")]
        [SerializeField] private Color releaseCancelColor = new Color(0.9f, 0.45f, 0.35f, 1f);

        [Tooltip("Burst when the object snaps into the socket.")]
        [SerializeField] private Color releaseSnapColor = new Color(0.35f, 1f, 0.75f, 1f);

        private SoulBlinkable _target;
        private Rigidbody _targetRb;
        private bool _rbWasKinematic;
        private bool _freezePushed;
        private GameObject _carryVfxInstance;

        public bool IsManipulating => _target != null;

        private void Awake()
        {
            if (cameraController == null)
                cameraController = FindFirstObjectByType<GeisCameraController>();
        }

        private void OnEnable()
        {
            SoulRealmManager.SoulRealmStateChanged += OnSoulRealmChanged;
        }

        private void OnDisable()
        {
            SoulRealmManager.SoulRealmStateChanged -= OnSoulRealmChanged;
            CancelManipulationIfNeeded();
        }

        private void OnSoulRealmChanged()
        {
            if (SoulRealmManager.Instance == null || !SoulRealmManager.Instance.IsSoulRealmActive)
                CancelManipulationIfNeeded();
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            Camera cam = cameraController != null ? cameraController.MainCamera : Camera.main;
            if (cam == null)
                return;

            Vector3 pos = ComputeFloatPosition(cam);
            Quaternion rot = alignYawToCamera
                ? Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f)
                : Quaternion.identity;

            _target.transform.SetPositionAndRotation(pos, rot);

            SoulBlinkSocket socket = _target.SocketB;
            if (socket != null && socket.IsWithinSnapRange(_target.transform.position))
            {
                _target.SnapIntoSocketFromRaycast(socket);
                EndManipulationInternal(false);
            }
        }

        private Vector3 ComputeFloatPosition(Camera cam)
        {
            Vector3 rayOrigin = cam.transform.position;
            Vector3 rayDir = cam.transform.forward;
            Vector3 fallback = rayOrigin + rayDir * floatDistance;

            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDir, maxRaycastDistance, placementRaycastMask,
                QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return fallback;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider != null && _target != null &&
                    hit.collider.GetComponentInParent<SoulBlinkable>() == _target)
                    continue;
                return hit.point + hit.normal * surfaceOffset;
            }

            return fallback;
        }

        /// <summary>
        /// Primary ability (Q) while manipulating: cancel and return to pose A. Returns true if consumed.
        /// </summary>
        public bool TryConsumePrimaryAbilityCancel()
        {
            if (_target == null)
                return false;
            EndManipulationInternal(true);
            return true;
        }

        public bool TryBeginManipulation(SoulBlinkable blink)
        {
            if (blink == null || _target != null)
                return false;

            _target = blink;
            _targetRb = blink.GetComponent<Rigidbody>();
            if (_targetRb != null)
            {
                _rbWasKinematic = _targetRb.isKinematic;
                _targetRb.isKinematic = true;
                _targetRb.linearVelocity = Vector3.zero;
                _targetRb.angularVelocity = Vector3.zero;
            }

            GeisInteractInput.PushInteractionMovementFreeze();
            _freezePushed = true;

            Camera cam = cameraController != null ? cameraController.MainCamera : Camera.main;
            Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward;
            SoulAbilityProceduralBurst.Spawn(blink.transform.position, fwd, grabBurstColor);

            StartCarryVfx(blink);

            return true;
        }

        private void StartCarryVfx(SoulBlinkable blink)
        {
            StopCarryVfx();

            GameObject prefab = blink.ManipulationCarryVfxPrefab != null
                ? blink.ManipulationCarryVfxPrefab
                : defaultCarryLoopVfxPrefab;

            if (prefab != null)
            {
                _carryVfxInstance = Instantiate(prefab, blink.transform);
                _carryVfxInstance.transform.localPosition = Vector3.zero;
                _carryVfxInstance.transform.localRotation = Quaternion.identity;
                return;
            }

            if (proceduralCarryLoopIfNoPrefab)
                _carryVfxInstance = SoulBlinkManipulationProceduralCarry.Create(blink.transform, proceduralCarryColor);
        }

        private void StopCarryVfx()
        {
            if (_carryVfxInstance == null)
                return;
            Destroy(_carryVfxInstance);
            _carryVfxInstance = null;
        }

        private void EndManipulationInternal(bool cancel)
        {
            if (_target == null)
                return;

            Vector3 endPos = _target.transform.position;
            StopCarryVfx();

            if (cancel)
                _target.RestoreToPoseA();

            SoulAbilityProceduralBurst.Spawn(
                endPos,
                Vector3.up,
                cancel ? releaseCancelColor : releaseSnapColor,
                1.1f);

            if (_targetRb != null)
            {
                _targetRb.isKinematic = _rbWasKinematic;
                _targetRb = null;
            }

            if (_freezePushed)
            {
                GeisInteractInput.PopInteractionMovementFreeze();
                _freezePushed = false;
            }

            _target = null;
        }

        private void CancelManipulationIfNeeded()
        {
            if (_target == null)
                return;
            EndManipulationInternal(true);
        }
    }
}
