using System;
using Geis.InputSystem;
using Geis.SoulRealm;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// The soul core / weak point exposed on the giant boss after both hands are broken.
    ///
    /// Two damage modes, selected per-phase by GiantBossController:
    ///
    ///   requiresSoulRealm = true  (Phase 1)
    ///     The crit spot is only damageable from inside the Soul Realm.
    ///     Physical weapon hits are silently blocked (entity HP restored, no OnCritHit).
    ///     The spectral ghost must move within interactionRadius and press light attack.
    ///     Each ghost hit fires OnCritHit(this, ghostHitDamage).
    ///
    ///   requiresSoulRealm = false  (Phase 2)
    ///     Physical weapon-hitbox hits register normally via CombatEvents.OnDamageApplied
    ///     and fire OnCritHit(this, rawDamage).
    ///
    /// GiantBossController.DrainSouls converts the damage value into soul drain.
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public class CritSpot : MonoBehaviour
    {
        // ── Events ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired on every valid hit while the spot is vulnerable.
        /// (spot, damage) — GiantBossController converts damage into soul drain.
        /// </summary>
        public static event Action<CritSpot, float> OnCritHit;

        // ── Inspector ──────────────────────────────────────────────────────────────

        [Header("Visuals")]
        [Tooltip("Auto-detected from children if empty.")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private GameObject exposedVFXPrefab;
        [SerializeField] private GameObject hitVFXPrefab;

        [Header("Soul Realm Interaction (Phase 1)")]
        [Tooltip("Ghost must be within this distance to land a hit")]
        [SerializeField] private float interactionRadius = 3f;
        [Tooltip("Soul drain fired per ghost light-attack while vulnerable")]
        [SerializeField] private float ghostHitDamage = 20f;
        [SerializeField] private GameObject soulRealmPromptObject;

        // ── Runtime state ──────────────────────────────────────────────────────────

        private bool _isVulnerable;
        private bool _requiresSoulRealm;

        private CombatEntity _combatEntity;
        private CombatEntityData _entityData;
        private GeisInputReader _inputReader;
        private SoulGhostMotor _ghostMotor;

        // ── Properties ─────────────────────────────────────────────────────────────

        public bool IsVulnerable => _isVulnerable;

        // ── Unity lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _combatEntity = GetComponent<CombatEntity>();

            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();
        }

        private void Start()
        {
            _combatEntity.InitializeStatsWithoutHeroData(99999f, 0f, 0f);
            _entityData = _combatEntity.GetEntityData();

            _inputReader = FindFirstObjectByType<GeisInputReader>();

            // Ghost root is inactive between soul-realm sessions; search inactive objects too
            _ghostMotor = FindFirstObjectByType<SoulGhostMotor>(FindObjectsInactive.Include);

            SetVisible(false);
            SetPromptVisible(false);
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageApplied += HandleDamageApplied;

            if (_inputReader != null)
                _inputReader.onLightAttackPerformed += HandleGhostAttackInput;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageApplied -= HandleDamageApplied;

            if (_inputReader != null)
                _inputReader.onLightAttackPerformed -= HandleGhostAttackInput;
        }

        private void Update()
        {
            if (!_isVulnerable || !_requiresSoulRealm) return;

            bool inSoulRealm = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
            bool ghostInRange = GhostIsInRange();
            SetPromptVisible(inSoulRealm && ghostInRange);
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Expose or hide the crit spot.
        /// </summary>
        /// <param name="vulnerable">True to open the damage window.</param>
        /// <param name="requiresSoulRealm">
        /// True (Phase 1): only the spectral ghost can deal damage.
        /// False (Phase 2): normal physical weapon hits register.
        /// </param>
        public void SetVulnerable(bool vulnerable, bool requiresSoulRealm = false)
        {
            _isVulnerable      = vulnerable;
            _requiresSoulRealm = requiresSoulRealm;

            SetVisible(vulnerable);
            SetPromptVisible(false);

            if (_entityData != null)
                _entityData.currentHealth = _entityData.maxHealth;

            if (vulnerable && exposedVFXPrefab != null)
                Instantiate(exposedVFXPrefab, transform.position, Quaternion.identity);
        }

        // ── Physical damage interception ───────────────────────────────────────────

        private void HandleDamageApplied(CombatEventData data)
        {
            if (data.target != _combatEntity) return;

            // Always restore entity HP — CritSpot is an infinite-HP hit target
            _entityData?.Heal(data.damageAmount);

            if (!_isVulnerable) return;

            // In soul-realm mode physical hits are blocked; only ghost attacks count
            if (_requiresSoulRealm) return;

            RegisterHit(data.damageAmount);
        }

        // ── Soul Realm ghost attack ────────────────────────────────────────────────

        private void HandleGhostAttackInput()
        {
            if (!_isVulnerable || !_requiresSoulRealm) return;

            bool inSoulRealm = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
            if (!inSoulRealm) return;

            if (!GhostIsInRange()) return;

            RegisterHit(ghostHitDamage);
        }

        // ── Shared hit logic ───────────────────────────────────────────────────────

        private void RegisterHit(float damage)
        {
            if (hitVFXPrefab != null)
                Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);

            OnCritHit?.Invoke(this, damage);
        }

        // ── Helpers ────────────────────────────────────────────────────────────────

        private bool GhostIsInRange()
        {
            if (_ghostMotor == null || !_ghostMotor.gameObject.activeInHierarchy)
                return false;

            return Vector3.Distance(_ghostMotor.transform.position, transform.position) <= interactionRadius;
        }

        private void SetVisible(bool visible)
        {
            foreach (var r in renderers)
            {
                if (r != null)
                    r.enabled = visible;
            }
        }

        private void SetPromptVisible(bool visible)
        {
            if (soulRealmPromptObject != null)
                soulRealmPromptObject.SetActive(visible);
        }
    }
}
