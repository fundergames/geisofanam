using System;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Represents one destructible part of the Giant Boss (right hand, left hand, etc.).
    ///
    /// State machine:
    ///   Idle       — fist raised; no attacks land.
    ///   Slamming   — windup animation; not yet hittable.
    ///   Grounded   — fist stuck in ground; physically attackable.
    ///   Shielded   — fist grounded but a soul-realm shield blocks physical hits.
    ///                Destroy the shield in the Soul Realm → transitions to Grounded.
    ///   Broken     — HP depleted this cycle; awaiting crit-spot phase.
    ///   Disabled   — permanently out of the fight (end-state).
    ///
    /// Damage integration:
    ///   This component owns a CombatEntity so player attacks register via CombatEvents.OnDamageApplied
    ///   (GeisCombatBridge → SimpleAttackHitDetector → CombatExecutor, or legacy WeaponHitbox).
    ///   We intercept CombatEvents.OnDamageApplied to:
    ///     1. Immediately heal back the CombatEntityData (keeps it alive for future weapon hits).
    ///     2. Apply the damage to our own HP pool only when the state is Grounded.
    ///   SimpleAttackHitDetector / similar also skip boss fists unless grounded so weapon effects
    ///   do not run during windup, idle, shielded, etc.
    ///   All other states silently block damage (no visible feedback needed here — the UI / VFX
    ///   on GiantBossController handle the "blocked" state).
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public class BossPart : MonoBehaviour, IPhysicalWeaponHitGate
    {
        // ── Inspector ──────────────────────────────────────────────────────────────

        [SerializeField] private BossPartDefinition definition;

        [Header("Debug")]
        [SerializeField] private bool debugDamage;

        [Tooltip("Optional shield child. Auto-located if null. " +
                 "Activated when this part enters the Shielded state.")]
        [SerializeField] private BossPartShield shield;

        [Header("Visuals")]
        [Tooltip("Optional indicator shown while this part is stunned (Pinned).")]
        [SerializeField] private GameObject stunnedIndicator;
        [Tooltip("Optional indicator shown while this part is broken (HP depleted).")]
        [SerializeField] private GameObject brokenIndicator;

        [Header("Pinned (stunned) behaviour")]
        [Tooltip("If true, when the part becomes Pinned it will lock its local position so animation can't pull it back to idle.")]
        [SerializeField] private bool lockLocalPositionWhilePinned = true;

        [Tooltip("If true, also lock local rotation while pinned. Leave off to let rotation/secondary motion keep animating.")]
        [SerializeField] private bool lockLocalRotationWhilePinned = false;

        // ── Events ─────────────────────────────────────────────────────────────────

        /// <summary>Fired whenever this part's state changes. (part, newState)</summary>
        public static event Action<BossPart, BossPartState> OnStateChanged;

        /// <summary>Fired when this part's HP reaches zero this cycle.</summary>
        public static event Action<BossPart> OnPartBroken;

        /// <summary>Fired when this part is reset at the start of a new slam cycle.</summary>
        public static event Action<BossPart> OnPartReset;

        // ── State ──────────────────────────────────────────────────────────────────

        private BossPartState _state = BossPartState.Idle;
        private float _currentHealth;
        private bool _pinnedTransformLocked;
        private Vector3 _pinnedLocalPosition;
        private Quaternion _pinnedLocalRotation;

        // ── Components ─────────────────────────────────────────────────────────────

        private CombatEntity _combatEntity;
        private CombatEntityData _entityData;

        // ── Properties ─────────────────────────────────────────────────────────────

        public BossPartState State => _state;
        public BossPartDefinition Definition => definition;
        public float HealthPercent => definition != null && definition.maxHealth > 0f
            ? Mathf.Clamp01(_currentHealth / definition.maxHealth)
            : 0f;

        public bool AllowsPhysicalWeaponHits() => _state == BossPartState.Grounded || _state == BossPartState.Pinned;

        /// <summary>
        /// True only for the break event where this part transitioned from Pinned -> Broken.
        /// Used by the controller to play a "return to side" recover animation for pinned breaks.
        /// </summary>
        public bool WasPinnedWhenBrokenThisCycle { get; private set; }

        // ── Unity lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _combatEntity = GetComponent<CombatEntity>();
            if (_combatEntity != null)
                _combatEntity.simpleMeleeBypassTagFilter = true;

            if (shield == null)
                shield = GetComponentInChildren<BossPartShield>(true);
        }

        private void Start()
        {
            InitialiseFromDefinition();
        }

        private void LateUpdate()
        {
            if (!_pinnedTransformLocked)
                return;
            if (_state != BossPartState.Pinned)
                return;

            transform.localPosition = _pinnedLocalPosition;
            if (lockLocalRotationWhilePinned)
                transform.localRotation = _pinnedLocalRotation;
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageApplied += HandleDamageApplied;
            BossPartShield.OnShieldDestroyed += HandleShieldDestroyed;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageApplied -= HandleDamageApplied;
            BossPartShield.OnShieldDestroyed -= HandleShieldDestroyed;
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Override the definition at runtime (called by GiantBossController on encounter start).
        /// </summary>
        public void SetDefinition(BossPartDefinition def)
        {
            definition = def;
            InitialiseFromDefinition();
        }

        /// <summary>
        /// Drive the part into a new state. Called by GiantBossController's slam coroutine.
        /// </summary>
        public void SetState(BossPartState newState)
        {
            if (_state == newState) return;
            _state = newState;

            // Keep shield visible only while in the Shielded state
            if (shield != null)
                shield.gameObject.SetActive(newState == BossPartState.Shielded);

            if (stunnedIndicator != null)
                stunnedIndicator.SetActive(newState == BossPartState.Pinned);

            if (brokenIndicator != null)
                brokenIndicator.SetActive(newState == BossPartState.Broken);

            if (newState == BossPartState.Pinned)
            {
                if (lockLocalPositionWhilePinned || lockLocalRotationWhilePinned)
                {
                    _pinnedTransformLocked = true;
                    if (lockLocalPositionWhilePinned)
                        _pinnedLocalPosition = transform.localPosition;
                    if (lockLocalRotationWhilePinned)
                        _pinnedLocalRotation = transform.localRotation;
                }
            }
            else
            {
                _pinnedTransformLocked = false;
            }

            OnStateChanged?.Invoke(this, newState);
        }

        /// <summary>
        /// Restore HP and return to Idle so the part is ready for the next slam cycle.
        /// </summary>
        /// <param name="withShield">If true the shield is primed and will appear on next Grounded entry.</param>
        public void ResetForCycle(bool withShield)
        {
            _currentHealth = definition != null ? definition.maxHealth : 100f;

            // Restore CombatEntityData so WeaponHitbox keeps treating this part as alive
            if (_entityData != null)
                _entityData.currentHealth = _entityData.maxHealth;

            if (shield != null)
            {
                shield.gameObject.SetActive(false);
                shield.Prime(withShield);
            }

            if (stunnedIndicator != null)
                stunnedIndicator.SetActive(false);
            if (brokenIndicator != null)
                brokenIndicator.SetActive(false);
            _pinnedTransformLocked = false;
            WasPinnedWhenBrokenThisCycle = false;

            SetState(BossPartState.Idle);
            OnPartReset?.Invoke(this);
        }

        // ── Private helpers ────────────────────────────────────────────────────────

        private void InitialiseFromDefinition()
        {
            if (definition == null) return;

            // Start() runs first; then GiantBossController.SetDefinition at encounter start runs again —
            // use refresh so CombatEntity does not warn about "Stats already initialized".
            if (_combatEntity.GetEntityData() == null)
                _combatEntity.InitializeStatsWithoutHeroData(definition.maxHealth, 0f, 0f);
            else
                _combatEntity.RefreshStatsWithoutHeroData(definition.maxHealth, 0f, 0f);

            _entityData = _combatEntity.GetEntityData();
            _currentHealth = definition.maxHealth;
        }

        private void HandleDamageApplied(CombatEventData data)
        {
            if (data.target != _combatEntity) return;
            if (data.wasImmune)
                return;
            if (data.skipEntityDamageInterceptors)
                return;

            if (debugDamage)
                Debug.Log($"[BossPart] Damage event received on {name}: amount={data.damageAmount:F1} state={_state}", this);

            // Always heal back the CombatEntityData immediately so it never reads as dead.
            // BossPart manages its own HP pool; CombatEntityData is only used as a hit-detection target.
            _entityData?.Heal(data.damageAmount);

            // Only register real damage when the fist is physically attackable
            if (_state != BossPartState.Grounded && _state != BossPartState.Pinned)
            {
                if (debugDamage)
                    Debug.Log($"[BossPart] Ignored damage because state={_state} (must be Grounded).", this);
                return;
            }

            _currentHealth -= data.damageAmount;

            if (_currentHealth <= 0f)
                BreakPart();
        }

        private void HandleShieldDestroyed(BossPartShield destroyedShield)
        {
            // Only react to our own shield
            if (destroyedShield.OwnerPart != this) return;

            // Shield cleared in Soul Realm → fist is now physically attackable
            if (_state == BossPartState.Shielded)
                SetState(BossPartState.Grounded);
        }

        private void BreakPart()
        {
            _currentHealth = 0f;
            WasPinnedWhenBrokenThisCycle = _state == BossPartState.Pinned;
            SetState(BossPartState.Broken);
            OnPartBroken?.Invoke(this);
        }
    }

    /// <summary>States a BossPart occupies during an encounter cycle.</summary>
    public enum BossPartState
    {
        /// <summary>Fist raised; cannot be attacked.</summary>
        Idle,
        /// <summary>Windup animation playing; slam imminent.</summary>
        Slamming,
        /// <summary>Fist grounded; physically attackable.</summary>
        Grounded,
        /// <summary>Fist locked in place during phase mechanics; physically attackable and should not recover.</summary>
        Pinned,
        /// <summary>Fist grounded but covered by a soul-realm shield. Destroy shield first.</summary>
        Shielded,
        /// <summary>HP depleted this cycle; waiting for crit-spot phase.</summary>
        Broken,
        /// <summary>Permanently removed from the fight.</summary>
        Disabled
    }
}
