using System;
using System.Collections.Generic;
using Geis.InputSystem;
using Geis.Locomotion;
using Geis.Puzzles;
using UnityEngine;
using UnityEngine.Serialization;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Soul realm state: physical body stays visible (no locomotion; animator paused) but still follows
    /// moving ground; spectral copy moves, selective world freeze, hold-to-exit with camera lerp.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SoulRealmManager : MonoBehaviour
    {
        public static SoulRealmManager Instance { get; private set; }

        /// <summary>Invoked when soul realm is entered or exited (for puzzle visibility, etc.).</summary>
        public static event Action SoulRealmStateChanged;

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
        [Tooltip("Longest hold when ghost and body are separated by at least the reference distance.")]
        [FormerlySerializedAs("exitHoldDuration")]
        [SerializeField] private float exitHoldMaxDuration = 2f;
        [Tooltip("Separation (m) between ghost and body look targets at which exit uses the full max duration; closer pairs finish sooner.")]
        [SerializeField] private float exitHoldReferenceDistance = 12f;
        [Tooltip("Minimum exit hold when ghost is near the body (default 1 second).")]
        [SerializeField] private float exitHoldMinDuration = 1f;
        [Tooltip("If true, separation uses horizontal (XZ) distance only.")]
        [SerializeField] private bool exitHoldUseHorizontalDistance = true;
        [SerializeField] private float enterGraceSeconds = 0.35f;

        [Header("Visuals")]
        [SerializeField] private SoulRealmVisuals visuals;
        [Tooltip("Screen-centered particle trail while holding soul-realm exit (left bumper). Auto-added if unset.")]
        [SerializeField] private SoulRealmExitHoldVfx exitHoldVfx;

        [Header("Spectral ghost mesh")]
        [Tooltip("Mesh root for the moving spectral copy. The physical character stays visible and frozen in place. If empty, first SkinnedMeshRenderer under the player is used.")]
        [SerializeField] private Transform spectralCharacterVisualRoot;
        [Tooltip("Optional URP Lit (or other) material for the ghost copy. If set, overrides dissolve shader below.")]
        [SerializeField] private Material spectralMaterial;

        [Tooltip("Noise dissolve material (e.g. Dissolve_Metallic_DoubleSide). Used when Spectral Material is empty; copies each mesh slot from the body materials.")]
        [SerializeField] private Material spectralDissolveMaterialTemplate;

        [Tooltip("Seconds for the ghost to dissolve in after entering soul realm (not used during exit hold). Clamped to at least 0.2s so one frame cannot snap the effect.")]
        [SerializeField] private float spectralDissolveEnterDuration = 0.45f;

        [Tooltip("Only if the ghost dissolves the wrong way: set true when the shader uses 1−Dissolve on the property (opposite of puzzle props). Default off matches realm dissolve.")]
        [SerializeField] private bool spectralDissolveInvertForShader;

        [Tooltip("If true and Spectral Material is empty, ghost uses Spectral Dissolve Material Template.")]
        [SerializeField] private bool useSpectralDissolveShader = true;

        private static readonly List<SoulRealmFreezeTarget> FreezeRegistry = new List<SoulRealmFreezeTarget>();

        /// <summary>
        /// Single-frame delta clamp for soul-realm timers. Editor step / breakpoint / unpause can produce huge
        /// <see cref="Time.deltaTime"/>, which would otherwise complete exit hold and eject in one frame.
        /// </summary>
        private const float SoulRealmMaxDeltaPerFrame = 0.25f;

        private Transform _ghostLookAt;
        private GameObject _followPivot;
        private float _enterGrace;
        /// <summary>Movement/ghost animator frozen until this elapses (covers dissolve-in, not only exit-input grace).</summary>
        private float _enterMovementFreeze;
        private float _exitHoldTimer;
        private float _exitHoldDurationThisAttempt = 2f;
        /// <summary>True while exit input is held and exit is allowed (after grace). Do not use _exitHoldTimer &gt; 0 alone — timer is 0 on the first hold frame.</summary>
        private bool _exitHoldHeld;
        private bool _isSoulRealm;

        private Vector3 _bodyPositionAtEntry;
        private Quaternion _bodyRotationAtEntry;
        private GameObject _spectralVisualInstance;
        private Transform _spectralMeshSourceRoot;

        public bool IsSoulRealmActive => _isSoulRealm;
        public float SoulRealmBlend => _isSoulRealm ? 1f : 0f;

        /// <summary>
        /// Owner transform and world origin for soul weapon abilities and VFX. In Soul Realm uses the ghost
        /// (same chest-height anchor as bow projectiles); otherwise uses body locomotion and look-at.
        /// </summary>
        public void GetAbilityContextTransforms(out Transform ownerTransform, out Vector3 originWorld)
        {
            if (_isSoulRealm && ghostRoot != null && ghostRoot.activeInHierarchy)
            {
                ownerTransform = ghostRoot.transform;
                originWorld = _ghostLookAt != null
                    ? _ghostLookAt.position + Vector3.down * 0.35f
                    : ghostRoot.transform.position + Vector3.up * 1.25f;
                return;
            }

            ownerTransform = bodyLocomotion != null ? bodyLocomotion.transform : transform;
            originWorld = bodyLookAtTransform != null
                ? bodyLookAtTransform.position
                : ownerTransform.position + Vector3.up * 1.25f;
        }

        /// <summary>
        /// Linear 0–1 while holding to exit soul realm (elapsed hold time / configured duration for this attempt).
        /// 0 if not in soul realm or not holding.
        /// </summary>
        public float SoulRealmExitHoldProgress01 =>
            !_isSoulRealm || _exitHoldDurationThisAttempt <= 0f
                ? 0f
                : Mathf.Clamp01(_exitHoldTimer / _exitHoldDurationThisAttempt);

        /// <summary>
        /// Linear wall-clock progress through the exit hold: 0 at hold start, ~0.5 at half the hold duration,
        /// 1 when the timer reaches the transition (same ratio as <see cref="SoulRealmExitHoldProgress01"/>).
        /// Map to spectral dissolve as 0 = fully visible, 1 = fully dissolved at transition.
        /// </summary>
        public float SoulRealmExitHoldLinearProgress01 => SoulRealmExitHoldProgress01;

        /// <summary>True while the player is holding exit (after enter grace). Matches camera/pivot lerp; spectral dissolve should use this, not <c>_exitHoldTimer &gt; 0</c>.</summary>
        public bool IsSoulRealmExitHoldInProgress => _isSoulRealm && _exitHoldHeld;

        /// <summary>True while the ghost motor should run (soul realm; disabled during enter transition and while holding exit).</summary>
        public bool AllowGhostMovement
        {
            get
            {
                if (!_isSoulRealm || inputReader?.SoulRealm == null)
                    return false;
                if (_enterMovementFreeze > 0f)
                    return false;
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

        /// <summary>
        /// Re-applies ghost configuration after all <see cref="MonoBehaviour.Awake"/> calls on the scene.
        /// Ensures <see cref="GeisPlayerAnimationController"/> tuning is applied before we mirror capsule/speeds.
        /// </summary>
        private void Start()
        {
            if (ghostMotor != null && inputReader != null)
                ghostMotor.Configure(inputReader, bodyLocomotion, cameraController);
            if (exitHoldVfx != null && cameraController != null)
                exitHoldVfx.SetCameraController(cameraController);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            exitHoldMinDuration = Mathf.Max(1f, exitHoldMinDuration);
            exitHoldMaxDuration = Mathf.Max(exitHoldMinDuration, exitHoldMaxDuration);
        }
#endif

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

            if (exitHoldVfx == null)
                exitHoldVfx = GetComponentInChildren<SoulRealmExitHoldVfx>(true);
            if (exitHoldVfx == null)
                exitHoldVfx = gameObject.AddComponent<SoulRealmExitHoldVfx>();
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
                if (ghostRoot.CompareTag("Untagged"))
                    ghostRoot.tag = "Player";
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
            ghostRoot.tag = "Player";
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

            float dt = Mathf.Min(Time.deltaTime, SoulRealmMaxDeltaPerFrame);

            if (_enterGrace > 0f)
                _enterGrace -= dt;
            if (_enterMovementFreeze > 0f)
                _enterMovementFreeze -= dt;

            if (inputReader != null && inputReader.SoulRealm != null)
            {
                var sr = inputReader.SoulRealm;
                bool canExit = _enterGrace <= 0f;

                if (canExit && sr.IsPressed())
                {
                    _exitHoldHeld = true;
                    if (_exitHoldTimer <= 0f)
                    {
                        _exitHoldDurationThisAttempt = ComputeExitHoldDurationForCurrentSeparation();
                        if (_exitHoldDurationThisAttempt <= 0f)
                        {
                            CompleteExitSoulRealm();
                            return;
                        }

                        if (cameraController != null)
                            cameraController.BeginSoulRealmExitHoldRotationLerp();
                        if (exitHoldVfx != null && _followPivot != null)
                            exitHoldVfx.Begin(_followPivot.transform);
                    }

                    _exitHoldTimer += dt;
                    float p = Mathf.Clamp01(_exitHoldTimer / _exitHoldDurationThisAttempt);
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

                    if (_exitHoldTimer >= _exitHoldDurationThisAttempt)
                        CompleteExitSoulRealm();
                }
                else
                {
                    _exitHoldHeld = false;
                    if (_exitHoldTimer > 0f)
                    {
                        _exitHoldTimer = 0f;
                        if (exitHoldVfx != null)
                            exitHoldVfx.End();
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
            float dissolveEnter = Mathf.Max(0.2f, spectralDissolveEnterDuration);
            _enterMovementFreeze = Mathf.Max(enterGraceSeconds, dissolveEnter);
            _exitHoldTimer = 0f;
            _exitHoldHeld = false;

            if (bodyLocomotion != null)
            {
                _bodyPositionAtEntry = bodyLocomotion.transform.position;
                _bodyRotationAtEntry = bodyLocomotion.transform.rotation;
                bodyLocomotion.SetWalkLocomotionForSoulRealm(false);
            }

            if (bodyAnimator != null)
                bodyAnimator.speed = 0f;
            // Leave body CharacterController enabled so ground ride can move it with platforms.

            _spectralMeshSourceRoot = spectralCharacterVisualRoot != null
                ? spectralCharacterVisualRoot
                : SoulSpectralGhostVisual.FindDefaultVisualRoot(bodyLocomotion != null ? bodyLocomotion.transform : null);

            if (ghostRoot != null && bodyLocomotion != null)
            {
                ghostRoot.transform.SetPositionAndRotation(_bodyPositionAtEntry, _bodyRotationAtEntry);
                ghostRoot.SetActive(true);
            }

            if (ghostMotor != null && bodyLocomotion != null)
            {
                ghostMotor.SyncFromBodyForSoulRealm(bodyLocomotion);
                ghostMotor.RefreshGroundedAfterSoulRealmTeleport();
            }

            if (ghostMotor != null)
                ghostMotor.enabled = true;

            if (ghostRoot != null && bodyLocomotion != null && ghostMotor != null && inputReader != null)
            {
                Material dissolveTpl = spectralMaterial == null && useSpectralDissolveShader
                    ? spectralDissolveMaterialTemplate
                    : null;

                var existing = SoulSpectralGhostVisual.Spawn(
                    ghostRoot.transform,
                    bodyLocomotion.transform,
                    _spectralMeshSourceRoot,
                    bodyAnimator,
                    ghostMotor,
                    inputReader,
                    bodyLocomotion,
                    spectralMaterial,
                    dissolveTpl,
                    spectralDissolveEnterDuration,
                    spectralDissolveInvertForShader);
                _spectralVisualInstance = existing;
            }

            if (cameraController != null)
                cameraController.CaptureSoulRealmEntryState();
            if (cameraController != null && _ghostLookAt != null)
            {
                cameraController.SetFollowTarget(_ghostLookAt);
                cameraController.SnapFollowPositionKeepView(_ghostLookAt);
            }

            ApplyFreezeToWorld(true);

            if (visuals != null)
            {
                visuals.SetSoulRealmBlend(1f);
                Transform shockwaveAnchor = bodyLookAtTransform != null
                    ? bodyLookAtTransform
                    : bodyLocomotion != null
                        ? bodyLocomotion.transform
                        : null;
                Camera shockwaveCam = cameraController != null ? cameraController.MainCamera : null;
                visuals.PulseEntryShockwave(shockwaveAnchor, shockwaveCam);
            }

            SoulRealmStateChanged?.Invoke();
        }

        /// <summary>
        /// Immediately ejects the player from the Soul Realm without requiring the hold input.
        /// Used by boss encounters when the crit window expires.
        /// </summary>
        public void ForceExitSoulRealm()
        {
            if (!_isSoulRealm) return;
            _exitHoldTimer = 0f;
            _exitHoldHeld = false;
            if (cameraController != null)
                cameraController.EndSoulRealmExitHoldRotationLerp();
            CompleteExitSoulRealm();
        }

        /// <summary>
        /// World position for puzzle/NPC interaction range and prompts. In soul realm uses the moving ghost
        /// (look-at); otherwise the body. Do not use <see cref="GameObject.FindGameObjectWithTag"/> with
        /// <c>Player</c> while soul realm is active — both body and ghost can share that tag.
        /// </summary>
        public Vector3 GetInteractionProximityWorldPosition()
        {
            // Use feet/root (same as body locomotion) for 3D range checks — not chest look-at,
            // so soul-realm proximity matches physical-realm distance to puzzle props.
            if (_isSoulRealm && ghostRoot != null && ghostRoot.activeInHierarchy)
                return ghostRoot.transform.position;

            if (bodyLocomotion != null)
                return bodyLocomotion.transform.position;

            return transform.position;
        }

        /// <summary>
        /// World position for bow projectiles in soul realm: uses the moving ghost and camera yaw, not the frozen body hand.
        /// </summary>
        public bool TryGetGhostBowProjectileSpawnWorldPosition(out Vector3 worldPosition)
        {
            worldPosition = default;
            if (!_isSoulRealm || ghostRoot == null || !ghostRoot.activeInHierarchy)
                return false;

            Vector3 anchor = _ghostLookAt != null
                ? _ghostLookAt.position + Vector3.down * 0.35f
                : ghostRoot.transform.position + Vector3.up * 1.25f;

            Vector3 fwd = cameraController != null
                ? cameraController.GetCameraForwardZeroedYNormalised()
                : ghostRoot.transform.forward;

            Vector3 flatRight = Vector3.Cross(Vector3.up, fwd);
            if (flatRight.sqrMagnitude > 1e-6f)
                flatRight.Normalize();
            else
                flatRight = ghostRoot.transform.right;

            worldPosition = anchor + fwd * 0.4f - flatRight * 0.25f;
            return true;
        }

        private void CompleteExitSoulRealm()
        {
            if (exitHoldVfx != null)
                exitHoldVfx.End();
            _exitHoldTimer = 0f;
            _exitHoldHeld = false;
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

            PressurePlateTrigger.RefreshOverlapsAfterSoulRealmExit();

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

            SoulRealmStateChanged?.Invoke();
        }

        private float ComputeExitSeparationDistance()
        {
            if (_ghostLookAt == null || bodyLookAtTransform == null)
                return exitHoldReferenceDistance;

            Vector3 a = _ghostLookAt.position;
            Vector3 b = bodyLookAtTransform.position;
            if (exitHoldUseHorizontalDistance)
            {
                a.y = 0f;
                b.y = 0f;
            }

            return Vector3.Distance(a, b);
        }

        /// <summary>
        /// Scales hold time with ghost–body separation: full <see cref="exitHoldMaxDuration"/> at or beyond reference distance, shorter when closer.
        /// </summary>
        private float ComputeExitHoldDurationForCurrentSeparation()
        {
            float dist = ComputeExitSeparationDistance();
            float refD = Mathf.Max(0.001f, exitHoldReferenceDistance);
            float t = (dist / refD) * exitHoldMaxDuration;
            float minD = Mathf.Max(1f, exitHoldMinDuration);
            float maxD = Mathf.Max(minD, exitHoldMaxDuration);
            return Mathf.Clamp(t, minD, maxD);
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
