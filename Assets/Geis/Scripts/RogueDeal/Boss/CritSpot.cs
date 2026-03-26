using System;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// The soul core / weak point exposed on the giant boss after both hands are broken.
    ///
    /// Unlike BossPart, the CritSpot has no per-cycle HP pool — every hit that lands while
    /// it is vulnerable directly drains boss souls via the OnCritHit event.
    /// GiantBossController subscribes to that event and calls DrainSouls(damage).
    ///
    /// The CritSpot owns a CombatEntity so existing weapon-hitbox infrastructure hits it
    /// without modification. Damage is intercepted, the entity HP is immediately restored
    /// (keeping it "alive" for the next hit), and the raw damage amount is re-broadcast
    /// through OnCritHit.
    ///
    /// Visibility is toggled by GiantBossController via SetVulnerable(bool).
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public class CritSpot : MonoBehaviour
    {
        // ── Events ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired every time a hit lands while the spot is vulnerable.
        /// (spot, rawDamage) — GiantBossController converts rawDamage to soul drain.
        /// </summary>
        public static event Action<CritSpot, float> OnCritHit;

        // ── Inspector ──────────────────────────────────────────────────────────────

        [Header("Visuals")]
        [Tooltip("Renderers to show/hide. Auto-detected from children if empty.")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private GameObject exposedVFXPrefab;
        [SerializeField] private GameObject hitVFXPrefab;

        // ── Runtime state ──────────────────────────────────────────────────────────

        private bool _isVulnerable;
        private CombatEntity _combatEntity;
        private CombatEntityData _entityData;

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
            // Give the CombatEntity effectively-infinite HP so it is never considered dead
            // and continues registering weapon-hitbox collisions indefinitely.
            _combatEntity.InitializeStatsWithoutHeroData(99999f, 0f, 0f);
            _entityData = _combatEntity.GetEntityData();

            SetVisible(false);
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageApplied += HandleDamageApplied;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageApplied -= HandleDamageApplied;
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by GiantBossController when both hands are broken (expose) or when the
        /// vulnerable window expires / boss enters next cycle (hide).
        /// </summary>
        public void SetVulnerable(bool vulnerable)
        {
            _isVulnerable = vulnerable;
            SetVisible(vulnerable);

            // Restore entity HP so the crit spot is always hittable
            if (_entityData != null)
                _entityData.currentHealth = _entityData.maxHealth;

            if (vulnerable && exposedVFXPrefab != null)
                Instantiate(exposedVFXPrefab, transform.position, Quaternion.identity);
        }

        // ── Damage interception ────────────────────────────────────────────────────

        private void HandleDamageApplied(CombatEventData data)
        {
            if (data.target != _combatEntity) return;

            // Always restore HP — CritSpot's health is infinite from the perspective of
            // the CombatEntity pipeline; we translate damage directly into soul drain.
            _entityData?.Heal(data.damageAmount);

            if (!_isVulnerable) return;

            if (hitVFXPrefab != null)
                Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);

            OnCritHit?.Invoke(this, data.damageAmount);
        }

        // ── Visibility helpers ─────────────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            foreach (var r in renderers)
            {
                if (r != null)
                    r.enabled = visible;
            }
        }
    }
}
