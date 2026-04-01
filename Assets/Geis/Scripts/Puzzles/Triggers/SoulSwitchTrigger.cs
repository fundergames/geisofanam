using System.Collections.Generic;
using Geis.InteractInput;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// A lever or button the player interacts with (X key by default) while in the soul realm.
    /// Can be configured as a latching toggle or a momentary hold switch.
    ///
    /// Default realm: SoulOnly.
    ///
    /// Proximity: assign <see cref="interactTrigger"/> / <see cref="promptTrigger"/> (sphere triggers, isTrigger on).
    /// If unset, falls back to distance from <see cref="PromptParent"/> using <see cref="interactionRange"/> / <see cref="promptRange"/>.
    /// </summary>
    public class SoulSwitchTrigger : PuzzleTriggerBase, IPuzzleProximityRelayOwner
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

        [Header("Interaction")]
        [Tooltip("Latching toggle (press once = on, press again = off) vs momentary (hold = on).")]
        [SerializeField] private bool isToggle = true;

        [Header("Prompt")]
        [SerializeField] private GameObject promptPrefab;
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.8f, 0f);
        [Tooltip("If set, prompt position uses this transform instead of the trigger root (e.g. mesh pivot).")]
        [SerializeField] private Transform promptAnchor;

        private bool _playerInInteractRange;
        private bool _playerInPromptRange;
        private GameObject _activePrompt;

        private int _interactOverlapCount;
        private int _promptOverlapCount;

        private bool UsesTriggerZones =>
            interactTrigger != null || promptTrigger != null;

        private void Awake()
        {
            if (promptTrigger == null && interactTrigger != null)
                promptTrigger = interactTrigger;
            if (interactTrigger == null && promptTrigger != null)
                interactTrigger = promptTrigger;

            if (UsesTriggerZones)
                RegisterProximityRelays();
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

            if (_playerInInteractRange && WasInteractPressedThisFrameForConfiguredRealm())
            {
                if (isToggle)
                    SetActivated(!IsActivated);
                else
                    SetActivated(true);
            }

            if (!isToggle && _playerInInteractRange &&
                WasInteractReleasedThisFrameForConfiguredRealm())
            {
                SetActivated(false);
            }
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

        /// <summary>True if <paramref name="other"/> belongs to the player (root or tagged collider).</summary>
        public static bool IsPlayerProximityCollider(Collider other)
        {
            if (other == null)
                return false;
            if (other.CompareTag("Player"))
                return true;
            return other.transform.root.CompareTag("Player");
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
            if (_activePrompt != null)
            {
                Destroy(_activePrompt);
                _activePrompt = null;
            }
        }

        private void ClearProximity()
        {
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

        private Transform PromptParent => promptAnchor != null ? promptAnchor : transform;

        private void OnDestroy() => HidePrompt();
    }
}
