using System;
using Geis.InputSystem;
using Geis.SoulRealm;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// A soul-realm-only destructible shield attached to one BossPart.
    ///
    /// Mirrors the SoulAnchor interaction pattern:
    ///   - Invisible in the physical world; revealed only when IsSoulRealmActive.
    ///   - Detects the spectral ghost via SoulGhostMotor on OnTriggerEnter.
    ///   - Ghost light-attack input deals damage; when HP reaches 0 the shield is destroyed
    ///     and BossPart transitions from Shielded → Grounded (physically attackable).
    ///
    /// Setup:
    ///   Place as a child of the BossPart GameObject.
    ///   Requires a SphereCollider (set as trigger) — sized via interactionRadius.
    ///   Assign mesh Renderer children for the shield visual.
    ///   Call Prime(bool) from BossPart.ResetForCycle to prepare for the next cycle.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class BossPartShield : MonoBehaviour
    {
        // ── Events ─────────────────────────────────────────────────────────────────

        /// <summary>Fired when this shield's HP reaches zero. BossPart listens to unlock the fist.</summary>
        public static event Action<BossPartShield> OnShieldDestroyed;

        // ── Inspector ──────────────────────────────────────────────────────────────

        [Header("Interaction")]
        [Tooltip("Radius of the trigger zone; ghost must enter to interact")]
        [SerializeField] private float interactionRadius = 2.5f;

        [Header("Visuals")]
        [Tooltip("Auto-detected from children if empty")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private GameObject destroyVFXPrefab;
        [SerializeField] private GameObject interactPromptObject;

        // ── Runtime state ──────────────────────────────────────────────────────────

        private float _currentHealth;
        private float _maxHealth;
        private float _damagePerHit;
        private bool _primed;        // will activate on the next Grounded entry
        private bool _ghostInRange;

        private GeisInputReader _inputReader;
        private SphereCollider _trigger;

        // ── Properties ─────────────────────────────────────────────────────────────

        /// <summary>The BossPart this shield belongs to, resolved from the parent hierarchy.</summary>
        public BossPart OwnerPart { get; private set; }

        // ── Unity lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            OwnerPart = GetComponentInParent<BossPart>();

            _trigger = GetComponent<SphereCollider>();
            _trigger.isTrigger = true;
            _trigger.radius = interactionRadius;

            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Start()
        {
            _inputReader = FindFirstObjectByType<GeisInputReader>();
            SetVisible(false);
            SetPromptVisible(false);
        }

        private void OnEnable()
        {
            if (_inputReader != null)
                _inputReader.onLightAttackPerformed += HandleAttackInput;
        }

        private void OnDisable()
        {
            if (_inputReader != null)
                _inputReader.onLightAttackPerformed -= HandleAttackInput;
        }

        private void Update()
        {
            bool inSoulRealm = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
            // Show only when soul realm is active AND this part is currently shielded
            bool shouldShow = inSoulRealm
                && OwnerPart != null
                && OwnerPart.State == BossPartState.Shielded;

            SetVisible(shouldShow);
            SetPromptVisible(shouldShow && _ghostInRange);
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Prepare the shield for the next cycle.
        /// Stats are read from OwnerPart.Definition at prime-time so they're always in sync.
        /// </summary>
        /// <param name="willBeUsed">
        /// True if this shield should activate when the part next becomes Shielded.
        /// False leaves the shield dormant (e.g. Phase 1 where shields are absent).
        /// </param>
        public void Prime(bool willBeUsed)
        {
            _primed = willBeUsed;

            var def = OwnerPart?.Definition;
            _maxHealth    = def != null ? def.shieldHealth      : 75f;
            _damagePerHit = def != null ? def.shieldDamagePerHit : 25f;
            _currentHealth = _maxHealth;

            SetVisible(false);
        }

        // ── Trigger detection (ghost enters/exits) ─────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<SoulGhostMotor>() != null)
                _ghostInRange = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<SoulGhostMotor>() != null)
                _ghostInRange = false;
        }

        // ── Input handling ─────────────────────────────────────────────────────────

        private void HandleAttackInput()
        {
            bool inSoulRealm = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
            if (!inSoulRealm || !_ghostInRange) return;
            if (OwnerPart == null || OwnerPart.State != BossPartState.Shielded) return;

            _currentHealth -= _damagePerHit;

            if (_currentHealth <= 0f)
                DestroyShield();
        }

        // ── Destruction ────────────────────────────────────────────────────────────

        private void DestroyShield()
        {
            if (destroyVFXPrefab != null)
                Instantiate(destroyVFXPrefab, transform.position, Quaternion.identity);

            SetVisible(false);
            OnShieldDestroyed?.Invoke(this);
            // BossPart.HandleShieldDestroyed transitions the part to Grounded
        }

        // ── Visibility helpers ─────────────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            foreach (var r in renderers)
            {
                if (r != null)
                    r.enabled = visible;
            }

            if (_trigger != null)
                _trigger.enabled = visible;
        }

        private void SetPromptVisible(bool visible)
        {
            if (interactPromptObject != null)
                interactPromptObject.SetActive(visible);
        }
    }
}
