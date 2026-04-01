using System.Collections.Generic;
using Geis.InteractInput;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Alignment puzzle. Player holds Interact near the dial; rotation behaviour comes from
    /// <see cref="GeisDialMovement"/> (default: <see cref="GeisDialIncrementalStickMovement"/>).
    /// When the dial's angle is within snapThreshold degrees of targetAngle it snaps and activates.
    ///
    /// Default realm: SoulOnly.
    ///
    /// Proximity: assign <see cref="interactTrigger"/> / <see cref="promptTrigger"/> (sphere triggers, isTrigger on).
    /// If unset, falls back to distance from <see cref="PromptParent"/> using <see cref="interactionRange"/> / <see cref="promptRange"/>.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DefaultExecutionOrder(-40)]
    public class AlignmentDialTrigger : PuzzleTriggerBase, IPuzzleProximityRelayOwner
    {
        [Header("Proximity (sphere triggers)")]
        [Tooltip("Player must overlap this trigger to use Interact. If Prompt is empty, it is treated as the same as this.")]
        [SerializeField] private SphereCollider interactTrigger;
        [Tooltip("Player overlaps this to show the prompt. If Interact is empty, it is treated as the same as this.")]
        [SerializeField] private SphereCollider promptTrigger;

        [Header("Proximity (fallback — no sphere colliders assigned)")]
        [SerializeField] private float interactionRange = 3f;
        [Tooltip("Max distance to show the prompt. If < 0, uses Interaction Range.")]
        [SerializeField] private float promptRange = -1f;

        [Header("Dial movement")]
        [Tooltip("Defines how the dial rotates (incremental stick, future modes). Add GeisDialIncrementalStickMovement if missing.")]
        [SerializeField] private GeisDialMovement dialMovement;

        [Header("Dial Settings")]
        [Tooltip("Rotation axis in local space.")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [Tooltip("Target angle in degrees around the rotation axis.")]
        [SerializeField] private float targetAngle = 0f;
        [Tooltip("Activates when within this many degrees of the target.")]
        [SerializeField] private float snapThreshold = 15f;

        [Header("Prompt")]
        [SerializeField] private GameObject promptPrefab;
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.8f, 0f);
        [Tooltip("If set, prompt position uses this transform instead of the trigger root (e.g. mesh pivot).")]
        [SerializeField] private Transform promptAnchor;

        [Header("Visual")]
        [Tooltip("The Transform to visually rotate (the dial mesh).")]
        [SerializeField] private Transform dialVisual;

        [Header("Audio")]
        [SerializeField] private AudioClip snapSound;
        [SerializeField] private AudioSource audioSource;

        private float _currentAngle;
        private bool  _playerInInteractRange;
        private bool  _playerInPromptRange;
        private bool  _snapped;
        private GameObject _activePrompt;

        private int _interactOverlapCount;
        private int _promptOverlapCount;

        private bool _interactionFreezePushed;

        private bool UsesTriggerZones =>
            interactTrigger != null || promptTrigger != null;

        private Transform PromptParent => promptAnchor != null ? promptAnchor : transform;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (promptTrigger == null && interactTrigger != null)
                promptTrigger = interactTrigger;
            if (interactTrigger == null && promptTrigger != null)
                interactTrigger = promptTrigger;

            if (UsesTriggerZones)
                RegisterProximityRelays();

            if (dialMovement == null)
                dialMovement = GetComponent<GeisDialMovement>();
            if (dialMovement == null)
                Debug.LogError(
                    "[AlignmentDialTrigger] Assign or add a GeisDialMovement (e.g. GeisDialIncrementalStickMovement).",
                    this);
        }

        private void RegisterProximityRelays()
        {
            var map = new Dictionary<GameObject, (bool i, bool p)>();
            void Merge(SphereCollider col, bool interact, bool prompt)
            {
                if (col == null) return;
                var go = col.gameObject;
                if (!map.TryGetValue(go, out var t))
                    t = (false, false);
                if (interact) t.i = true;
                if (prompt) t.p = true;
                map[go] = t;
            }

            Merge(interactTrigger, interact: true, prompt: false);
            Merge(promptTrigger, interact: false, prompt: true);

            foreach (var kv in map)
            {
                var go = kv.Key;
                var (wantI, wantP) = kv.Value;
                if (!wantI && !wantP)
                    continue;
                var relay = go.GetComponent<SoulSwitchProximityRelay>();
                if (relay == null)
                    relay = go.AddComponent<SoulSwitchProximityRelay>();
                relay.Initialize(this, wantI, wantP);
            }
        }

        private void Update()
        {
            if (!IsAccessibleInCurrentRealm())
            {
                ClearProximity();
                return;
            }

            if (!UsesTriggerZones)
                RefreshPlayerDistanceFallback();

            if (_snapped) return;

            bool holding = _playerInInteractRange && IsInteractHeldForConfiguredRealm();
            bool dialSession = holding && dialMovement != null;
            SetInteractionMovementFreeze(dialSession);

            if (dialSession)
            {
                float delta = dialMovement.ComputeAngleDeltaThisFrame(Time.deltaTime);
                if (Mathf.Abs(delta) > 0.0001f)
                {
                    _currentAngle += delta;
                    _currentAngle = (_currentAngle % 360f + 360f) % 360f;
                    ApplyRotation(_currentAngle);
                }

                float snapDelta = Mathf.Abs(Mathf.DeltaAngle(_currentAngle, targetAngle));
                if (snapDelta <= snapThreshold)
                    Snap();
            }
            else if (!holding && dialMovement != null)
                dialMovement.ResetMovementState();
        }

        private void SetInteractionMovementFreeze(bool wantFreeze)
        {
            if (wantFreeze == _interactionFreezePushed)
                return;
            if (wantFreeze)
                GeisInteractInput.PushInteractionMovementFreeze();
            else
                GeisInteractInput.PopInteractionMovementFreeze();
            _interactionFreezePushed = wantFreeze;
        }

        private void Snap()
        {
            dialMovement?.ResetMovementState();
            SetInteractionMovementFreeze(false);

            _snapped      = true;
            _currentAngle = targetAngle;
            ApplyRotation(targetAngle);
            HidePrompt();

            if (snapSound != null && audioSource != null)
                audioSource.PlayOneShot(snapSound);

            SetActivated(true);

            // Interact-driven trigger: staying inside sphere colliders does not re-fire OnTriggerEnter; refresh
            // proximity so the interact prompt can show again after the dial snaps (non-interact triggers skip this).
            TryRefreshPromptAfterSnap();
        }

        /// <summary>
        /// After snap we hide the prompt; re-evaluate overlap/distance so players already in range see it again.
        /// </summary>
        private void TryRefreshPromptAfterSnap()
        {
            if (!IsAccessibleInCurrentRealm())
                return;
            if (UsesTriggerZones)
                RefreshOverlapFromWorldPosition();
            else
                RefreshPlayerDistanceFallback();
        }

        private Transform DialRotateTarget => dialVisual != null ? dialVisual : transform;

        private void ApplyRotation(float angle)
        {
            DialRotateTarget.localRotation = Quaternion.AngleAxis(angle, rotationAxis);
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            _snapped = false;
            dialMovement?.ResetMovementState();
            SetInteractionMovementFreeze(false);
        }

        void IPuzzleProximityRelayOwner.OnProximityRelayEnter(bool interact, bool prompt) =>
            OnProximityRelayEnter(interact, prompt);

        void IPuzzleProximityRelayOwner.OnProximityRelayExit(bool interact, bool prompt) =>
            OnProximityRelayExit(interact, prompt);

        internal void OnProximityRelayEnter(bool interact, bool prompt)
        {
            if (interact) _interactOverlapCount++;
            if (prompt) _promptOverlapCount++;
            ApplyProximityFromTriggerCounts();
        }

        internal void OnProximityRelayExit(bool interact, bool prompt)
        {
            if (interact) _interactOverlapCount = Mathf.Max(0, _interactOverlapCount - 1);
            if (prompt) _promptOverlapCount = Mathf.Max(0, _promptOverlapCount - 1);
            ApplyProximityFromTriggerCounts();
        }

        private void ApplyProximityFromTriggerCounts()
        {
            bool inInteract = _interactOverlapCount > 0;
            bool inPrompt = _promptOverlapCount > 0;

            if (_playerInInteractRange == inInteract && _playerInPromptRange == inPrompt)
                return;

            _playerInInteractRange = inInteract;
            _playerInPromptRange = inPrompt;

            if (inPrompt && _activePrompt == null)
                ShowPrompt();
            else if (!inPrompt && _activePrompt != null)
                HidePrompt();
        }

        private void RefreshPlayerDistanceFallback()
        {
            Vector3 playerPos = GeisInteractInput.GetInteractionWorldPositionOrFallback();
            float dist = Vector3.Distance(PromptParent.position, playerPos);
            float promptCutoff = promptRange >= 0f ? promptRange : interactionRange;
            bool inInteract = dist <= interactionRange;
            bool inPrompt = dist <= promptCutoff;

            if (_playerInInteractRange == inInteract && _playerInPromptRange == inPrompt)
                return;

            _playerInInteractRange = inInteract;
            _playerInPromptRange = inPrompt;

            if (inPrompt && _activePrompt == null)
                ShowPrompt();
            else if (!inPrompt && _activePrompt != null)
                HidePrompt();
        }

        private static bool IsPointInsideSphereCollider(SphereCollider s, Vector3 worldPoint)
        {
            if (s == null || !s.enabled)
                return false;
            Vector3 c = s.transform.TransformPoint(s.center);
            float r = s.radius * MaxAbsComponent(s.transform.lossyScale);
            return (worldPoint - c).sqrMagnitude <= r * r;
        }

        private static float MaxAbsComponent(Vector3 v) =>
            Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        /// <summary>Re-sync overlap after realm toggles (physics may not re-send trigger callbacks the same frame).</summary>
        private void RefreshOverlapFromWorldPosition()
        {
            if (!UsesTriggerZones)
                return;

            Vector3 p = GeisInteractInput.GetInteractionWorldPositionOrFallback();
            bool inInteract = IsPointInsideSphereCollider(interactTrigger, p);
            bool inPrompt = IsPointInsideSphereCollider(promptTrigger, p);
            _interactOverlapCount = inInteract ? 1 : 0;
            _promptOverlapCount = inPrompt ? 1 : 0;
            ApplyProximityFromTriggerCounts();
        }

        private void ClearProximity()
        {
            dialMovement?.ResetMovementState();
            SetInteractionMovementFreeze(false);
            _playerInInteractRange = false;
            _playerInPromptRange = false;
            _interactOverlapCount = 0;
            _promptOverlapCount = 0;
            HidePrompt();
        }

        protected override void OnRealmStateChangedForInteractPrompt()
        {
            if (!IsAccessibleInCurrentRealm())
            {
                ClearProximity();
                return;
            }

            if (UsesTriggerZones)
                RefreshOverlapFromWorldPosition();
            else
            {
                _playerInInteractRange = false;
                _playerInPromptRange = false;
                RefreshPlayerDistanceFallback();
            }
        }

        private void ShowPrompt()
        {
            var parent = PromptParent;
            if (promptPrefab != null)
            {
                _activePrompt = Instantiate(promptPrefab, parent);
                _activePrompt.transform.localPosition = promptOffset;
                _activePrompt.transform.localRotation = Quaternion.identity;
            }
            else
            {
                _activePrompt = PuzzleInteractionPrompt.CreateWorldLetterPrompt(parent, promptOffset, "X");
            }
        }

        private void HidePrompt()
        {
            if (_activePrompt != null) { Destroy(_activePrompt); _activePrompt = null; }
        }

        private void OnDestroy()
        {
            dialMovement?.ResetMovementState();
            SetInteractionMovementFreeze(false);
            HidePrompt();
        }
    }
}
