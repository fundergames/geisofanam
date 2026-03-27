using System;
using Geis.InputSystem;
using Geis.SoulRealm;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// A soul-realm-only destructible object. Only visible and damageable while the Soul Realm is active.
    /// The player's spectral ghost must enter the trigger zone and use a light attack to deal damage.
    /// When all anchors in a phase are destroyed, the boss's immunity is removed.
    ///
    /// Setup: Requires a SphereCollider (set as trigger). Attach Renderer children for visuals.
    /// The ghost's CharacterController will fire OnTriggerEnter/Exit on this object's sphere trigger.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class SoulAnchor : MonoBehaviour
    {
        /// <summary>Fires when this anchor is fully destroyed. BossController listens to this.</summary>
        public static event Action<SoulAnchor> OnAnchorDestroyed;

        [Header("Settings")]
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float damagePerHit = 25f;

        [Header("Interaction")]
        [Tooltip("Radius of the trigger zone — ghost must enter this to interact")]
        [SerializeField] private float interactionRadius = 2f;

        [Header("Visuals")]
        [Tooltip("Renderers to show/hide based on soul realm state. Auto-detected from children if empty.")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private GameObject destroyVFXPrefab;
        [SerializeField] private GameObject interactPromptObject;

        private float _currentHealth;
        private bool _ghostInRange;
        private GeisInputReader _inputReader;
        private SphereCollider _trigger;

        private void Awake()
        {
            _trigger = GetComponent<SphereCollider>();
            _trigger.isTrigger = true;
            _trigger.radius = interactionRadius;

            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();
        }

        private void Start()
        {
            _currentHealth = maxHealth;
            _inputReader = FindFirstObjectByType<GeisInputReader>();

            // Start hidden — only revealed in soul realm
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
            SetVisible(inSoulRealm);
            SetPromptVisible(inSoulRealm && _ghostInRange);
        }

        // ── Trigger detection ──────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            // Identify the ghost by its SoulGhostMotor component
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

            TakeDamage(damagePerHit);
        }

        // ── Health / Destruction ───────────────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            _currentHealth -= amount;

            if (_currentHealth <= 0f)
                DestroyAnchor();
        }

        private void DestroyAnchor()
        {
            if (destroyVFXPrefab != null)
                Instantiate(destroyVFXPrefab, transform.position, Quaternion.identity);

            OnAnchorDestroyed?.Invoke(this);
            Destroy(gameObject);
        }

        // ── Visibility helpers ─────────────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            foreach (var r in renderers)
            {
                if (r != null)
                    r.enabled = visible;
            }

            // Only enable the trigger collider while visible so physics queries are clean
            if (_trigger != null)
                _trigger.enabled = visible;
        }

        private void SetPromptVisible(bool visible)
        {
            if (interactPromptObject != null)
                interactPromptObject.SetActive(visible);
        }

        public float HealthPercent => maxHealth > 0 ? _currentHealth / maxHealth : 0f;
    }
}
