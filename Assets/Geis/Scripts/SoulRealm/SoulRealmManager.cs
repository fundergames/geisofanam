using System.Collections.Generic;
using Geis.InputSystem;
using Geis.Locomotion;
using UnityEngine;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Soul realm state: physical body stays visible at the entry pose (no locomotion; animator paused), spectral copy moves, selective world freeze, hold-to-exit with camera lerp.
    /// </summary>
    public sealed class SoulRealmManager : MonoBehaviour
    {
        public static SoulRealmManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private GeisInputReader inputReader;
        [SerializeField] private GeisPlayerAnimationController bodyLocomotion;
        [SerializeField] private GeisCameraController cameraController;
        [SerializeField] private Animator bodyAnimator;
        [SerializeField] private CharacterController bodyCharacterController;
        [SerializeField] private Transform bodyLookAtTransform;

        [Header("Ghost")]
        [SerializeField] private GameObject ghostRoot;
        [SerializeField] private SoulGhostMotor ghostMotor;

        [Header("Timing")]
        [SerializeField] private float exitHoldDuration = 2f;
        [SerializeField] private float enterGraceSeconds = 0.35f;

        [Header("Visuals")]
        [SerializeField] private SoulRealmVisuals visuals;

        [Header("Spectral ghost mesh")]
        [Tooltip("Mesh root for the moving spectral copy. The physical character stays visible and frozen in place. If empty, first SkinnedMeshRenderer under the player is used.")]
        [SerializeField] private Transform spectralCharacterVisualRoot;
        [Tooltip("Optional URP Lit (or other) material for the ghost copy. If empty, a green transparent Lit is created at runtime.")]
        [SerializeField] private Material spectralMaterial;

        private static readonly List<SoulRealmFreezeTarget> FreezeRegistry = new List<SoulRealmFreezeTarget>();

        private Transform _ghostLookAt;
        private GameObject _followPivot;
        private float _enterGrace;
        private float _exitHoldTimer;
        private bool _isSoulRealm;

        private Vector3 _bodyPositionAtEntry;
        private Quaternion _bodyRotationAtEntry;
        private GameObject _spectralVisualInstance;
        private Transform _spectralMeshSourceRoot;

        public bool IsSoulRealmActive => _isSoulRealm;
        public float SoulRealmBlend => _isSoulRealm ? 1f : 0f;

        /// <summary>True while the ghost motor should run (soul realm; disabled while holding exit).</summary>
        public bool AllowGhostMovement
        {
            get
            {
                if (!_isSoulRealm || inputReader?.SoulRealm == null)
                    return false;
                if (_enterGrace > 0f)
                    return true;
                return !inputReader.SoulRealm.IsPressed();
            }
        }

        /// <summary>Body locomotion should skip Update while active.</summary>
        public bool ShouldSuppressBodyLocomotion => _isSoulRealm;

        public static void RegisterFreezeTarget(SoulRealmFreezeTarget t)
        {
            if (t != null && !FreezeRegistry.Contains(t))
                FreezeRegistry.Add(t);
        }

        public static void UnregisterFreezeTarget(SoulRealmFreezeTarget t)
        {
            if (t != null)
                FreezeRegistry.Remove(t);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            ResolveReferences();
            EnsureGhost();
            EnsureFollowPivot();
            if (ghostMotor != null && inputReader != null)
                ghostMotor.Configure(inputReader, bodyLocomotion, cameraController);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void ResolveReferences()
        {
            if (inputReader == null)
                inputReader = GetComponentInChildren<GeisInputReader>();
            if (bodyLocomotion == null)
                bodyLocomotion = GetComponent<GeisPlayerAnimationController>();
            if (cameraController == null)
                cameraController = FindFirstObjectByType<GeisCameraController>();
            if (bodyAnimator == null && bodyLocomotion != null)
                bodyAnimator = bodyLocomotion.GetComponent<Animator>();
            if (bodyCharacterController == null && bodyLocomotion != null)
                bodyCharacterController = bodyLocomotion.GetComponent<CharacterController>();
            if (bodyLookAtTransform == null && bodyLocomotion != null)
            {
                var t = bodyLocomotion.transform.Find("SyntyPlayer_LookAt");
                bodyLookAtTransform = t != null ? t : bodyLocomotion.transform;
            }

            if (visuals == null)
                visuals = GetComponentInChildren<SoulRealmVisuals>(true);
            if (visuals == null)
                visuals = gameObject.AddComponent<SoulRealmVisuals>();
        }

        private void EnsureGhost()
        {
            if (ghostRoot != null)
            {
                _ghostLookAt = ghostRoot.transform.Find("SyntyPlayer_LookAt");
                if (_ghostLookAt == null)
                {
                    var go = new GameObject("SyntyPlayer_LookAt");
                    go.transform.SetParent(ghostRoot.transform, false);
                    go.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                    _ghostLookAt = go.transform;
                }

                if (ghostMotor == null)
                    ghostMotor = ghostRoot.GetComponent<SoulGhostMotor>();
                ghostRoot.SetActive(false);
                return;
            }

            ghostRoot = new GameObject("SoulGhost");
            ghostRoot.transform.SetParent(transform, false);
            var cc = ghostRoot.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            ghostMotor = ghostRoot.AddComponent<SoulGhostMotor>();

            var lookGo = new GameObject("SyntyPlayer_LookAt");
            lookGo.transform.SetParent(ghostRoot.transform, false);
            lookGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            _ghostLookAt = lookGo.transform;
            ghostRoot.SetActive(false);
        }

        private void EnsureFollowPivot()
        {
            if (_followPivot != null)
                return;
            _followPivot = new GameObject("SoulRealmCameraFollowPivot");
            _followPivot.transform.SetParent(transform, false);
        }

        private void Update()
        {
            if (!_isSoulRealm)
            {
                if (inputReader != null && inputReader.SoulRealmWasPressedThisFrame())
                    EnterSoulRealm();
                return;
            }

            if (_enterGrace > 0f)
                _enterGrace -= Time.deltaTime;

            if (inputReader != null && inputReader.SoulRealm != null)
            {
                var sr = inputReader.SoulRealm;
                bool canExit = _enterGrace <= 0f;

                if (canExit && sr.IsPressed())
                {
                    if (_exitHoldTimer <= 0f && cameraController != null)
                        cameraController.BeginSoulRealmExitHoldRotationLerp();

                    _exitHoldTimer += Time.deltaTime;
                    float p = Mathf.Clamp01(_exitHoldTimer / exitHoldDuration);
                    if (cameraController != null)
                        cameraController.SetSoulRealmExitHoldProgress(p);

                    if (_followPivot != null && _ghostLookAt != null && bodyLookAtTransform != null)
                    {
                        _followPivot.transform.position = Vector3.Lerp(_ghostLookAt.position, bodyLookAtTransform.position, p);
                        _followPivot.transform.rotation =
                            Quaternion.Slerp(_ghostLookAt.rotation, bodyLookAtTransform.rotation, p);
                    }

                    if (cameraController != null)
                        cameraController.SetFollowTarget(_followPivot.transform);

                    if (ghostMotor != null)
                        ghostMotor.enabled = false;

                    if (_exitHoldTimer >= exitHoldDuration)
                        CompleteExitSoulRealm();
                }
                else
                {
                    if (_exitHoldTimer > 0f)
                    {
                        _exitHoldTimer = 0f;
                        if (cameraController != null)
                            cameraController.EndSoulRealmExitHoldRotationLerp();
                        if (ghostMotor != null)
                            ghostMotor.enabled = true;
                        if (cameraController != null && _ghostLookAt != null)
                            cameraController.SetFollowTarget(_ghostLookAt);
                    }
                }
            }

            if (visuals != null && _isSoulRealm)
                visuals.SetSoulRealmBlend(1f);
        }

        private void EnterSoulRealm()
        {
            if (_isSoulRealm)
                return;
            _isSoulRealm = true;
            _enterGrace = enterGraceSeconds;
            _exitHoldTimer = 0f;

            if (bodyLocomotion != null)
            {
                _bodyPositionAtEntry = bodyLocomotion.transform.position;
                _bodyRotationAtEntry = bodyLocomotion.transform.rotation;
            }

            if (bodyAnimator != null)
                bodyAnimator.speed = 0f;
            if (bodyCharacterController != null)
                bodyCharacterController.enabled = false;

            _spectralMeshSourceRoot = spectralCharacterVisualRoot != null
                ? spectralCharacterVisualRoot
                : SoulSpectralGhostVisual.FindDefaultVisualRoot(bodyLocomotion != null ? bodyLocomotion.transform : null);

            if (ghostRoot != null && bodyLocomotion != null)
            {
                ghostRoot.transform.SetPositionAndRotation(_bodyPositionAtEntry, _bodyRotationAtEntry);
                ghostRoot.SetActive(true);
            }

            if (ghostMotor != null)
                ghostMotor.enabled = true;

            if (ghostRoot != null && bodyLocomotion != null && ghostMotor != null && inputReader != null)
            {
                var existing = SoulSpectralGhostVisual.Spawn(
                    ghostRoot.transform,
                    bodyLocomotion.transform,
                    _spectralMeshSourceRoot,
                    bodyAnimator,
                    ghostMotor,
                    inputReader,
                    bodyLocomotion,
                    spectralMaterial);
                _spectralVisualInstance = existing;
            }

            if (cameraController != null)
                cameraController.CaptureSoulRealmEntryState();
            if (cameraController != null && _ghostLookAt != null)
                cameraController.SetFollowTarget(_ghostLookAt);

            ApplyFreezeToWorld(true);

            if (visuals != null)
                visuals.SetSoulRealmBlend(1f);
        }

        private void CompleteExitSoulRealm()
        {
            _exitHoldTimer = 0f;
            _isSoulRealm = false;

            if (bodyLocomotion != null)
            {
                if (bodyCharacterController != null)
                    bodyCharacterController.enabled = false;
                bodyLocomotion.transform.SetPositionAndRotation(_bodyPositionAtEntry, _bodyRotationAtEntry);
                if (bodyCharacterController != null)
                    bodyCharacterController.enabled = true;
            }

            SoulSpectralGhostVisual.Despawn(_spectralVisualInstance);
            _spectralVisualInstance = null;

            if (ghostRoot != null)
            {
                ghostRoot.SetActive(false);
                if (ghostMotor != null)
                    ghostMotor.enabled = false;
            }

            if (bodyAnimator != null)
                bodyAnimator.speed = 1f;
            if (bodyCharacterController != null)
                bodyCharacterController.enabled = true;

            if (bodyLocomotion != null)
                bodyLocomotion.PrepareBodyAfterSoulRealmExit();

            if (cameraController != null && bodyLookAtTransform != null)
            {
                cameraController.SetFollowTarget(bodyLookAtTransform);
                cameraController.ApplySoulRealmBaselineSnapshot();
            }

            ApplyFreezeToWorld(false);

            if (visuals != null)
                visuals.SetSoulRealmBlend(0f);
        }

        private static void ApplyFreezeToWorld(bool frozen)
        {
            for (var i = 0; i < FreezeRegistry.Count; i++)
            {
                var t = FreezeRegistry[i];
                if (t != null)
                    t.ApplyFrozen(frozen);
            }
        }
    }
}
