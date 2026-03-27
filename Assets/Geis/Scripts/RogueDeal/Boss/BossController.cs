using System;
using System.Collections;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RogueDeal.Boss
{
    /// <summary>
    /// State machine for the Soul Warden boss encounter.
    ///
    /// Phase flow:
    ///   Phase 1 (100% → 67% HP) — standard combat; attacks on interval.
    ///   Phase 2 (67% → 33% HP) — boss becomes immune; player must enter Soul Realm and destroy 1 anchor.
    ///   Phase 3 (33% →   0% HP) — boss becomes immune again; 2 anchors required; faster + AOE attacks.
    ///
    /// Immunity:
    ///   Incoming damage is intercepted via CombatEvents.OnDamageApplied and healed back while immune.
    ///   Once all soul anchors in a phase are destroyed, immunity drops and combat resumes.
    ///
    /// Requires: CombatEntity on the same GameObject.
    /// Assign: BossDefinition SO, anchor spawn points, SoulAnchor prefab, player CombatEntity.
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public class BossController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────────

        [Header("Boss Configuration")]
        [SerializeField] private BossDefinition definition;

        [Header("Soul Anchor Setup")]
        [Tooltip("World positions where soul anchors will spawn. Index 0 = Phase 2 anchor; 0+1 = Phase 3 anchors.")]
        [SerializeField] private Transform[] anchorSpawnPoints;
        [SerializeField] private GameObject soulAnchorPrefab;

        [Header("Player Reference")]
        [Tooltip("Assign the player's CombatEntity. If null, BossController will search the scene at encounter start.")]
        [SerializeField] private CombatEntity playerEntity;

        // ── Events ─────────────────────────────────────────────────────────────────

        /// <summary>Fired when the boss enters a new phase. Argument = phase number (1, 2, or 3).</summary>
        public static event Action<int> OnPhaseChanged;

        /// <summary>Fired when boss immunity toggles. Argument = true when immune.</summary>
        public static event Action<bool> OnImmunityChanged;

        /// <summary>Fired when the boss is defeated.</summary>
        public static event Action OnBossDefeated;

        /// <summary>Fired on every health change. Arguments: (currentHP, maxHP).</summary>
        public static event Action<float, float> OnHealthChanged;

        /// <summary>Fired when boss blocks a hit (immune). Used for UI/VFX feedback.</summary>
        public static event Action OnDamageBlocked;

        /// <summary>Fired on phase transition with the narrative message string.</summary>
        public static event Action<string> OnPhaseTransitionMessage;

        // ── State ──────────────────────────────────────────────────────────────────

        private BossState _state = BossState.Idle;
        private int _currentPhase = 1;
        private bool _isImmune;
        private int _anchorsRemaining;
        private float _attackTimer;
        private float _currentAttackInterval;

        // ── Components ─────────────────────────────────────────────────────────────

        private CombatEntity _combatEntity;
        private CombatEntityData _entityData;
        private Animator _animator;

        // ── Unity lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _combatEntity = GetComponent<CombatEntity>();
            _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageApplied += HandleDamageApplied;
            SoulAnchor.OnAnchorDestroyed += HandleAnchorDestroyed;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageApplied -= HandleDamageApplied;
            SoulAnchor.OnAnchorDestroyed -= HandleAnchorDestroyed;
        }

        private void Update()
        {
            if (_state != BossState.Combat || _entityData == null) return;

            CheckPhaseTransitions();

            if (!_isImmune)
                TickAttackTimer();
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Initialise and start the boss encounter. Call from BossEncounterManager.
        /// </summary>
        public void StartEncounter(CombatEntity player = null)
        {
            if (definition == null)
            {
                Debug.LogError("[BossController] No BossDefinition assigned — cannot start encounter.");
                return;
            }

            if (player != null)
                playerEntity = player;

            if (playerEntity == null)
                playerEntity = FindFirstObjectByType<CombatEntity>();

            _combatEntity.ForceInitializeStats(definition.maxHealth, definition.attack, definition.defense);
            _entityData = _combatEntity.GetEntityData();

            _currentPhase = 1;
            _isImmune = false;
            _anchorsRemaining = 0;
            _currentAttackInterval = definition.baseAttackInterval;
            _attackTimer = _currentAttackInterval;
            _state = BossState.Combat;

            OnHealthChanged?.Invoke(_entityData.currentHealth, _entityData.maxHealth);
            OnPhaseChanged?.Invoke(_currentPhase);

            Debug.Log($"[BossController] Encounter started: {definition.bossName} — {definition.title}");
        }

        // ── Properties ─────────────────────────────────────────────────────────────

        public BossState State => _state;
        public int CurrentPhase => _currentPhase;
        public bool IsImmune => _isImmune;
        public float HealthPercent => _entityData != null ? _entityData.currentHealth / _entityData.maxHealth : 1f;

        // ── Phase management ───────────────────────────────────────────────────────

        private void CheckPhaseTransitions()
        {
            if (_currentPhase >= 3) return;

            int nextPhase = _currentPhase + 1;
            var nextData = GetPhaseData(nextPhase);
            if (nextData == null) return;

            if (HealthPercent <= nextData.hpThresholdPercent)
                StartCoroutine(PhaseTransitionRoutine(nextPhase));
        }

        private IEnumerator PhaseTransitionRoutine(int newPhase)
        {
            // Prevent re-entry while coroutine runs
            _state = BossState.PhaseTransition;
            _currentPhase = newPhase;

            var phaseData = GetPhaseData(newPhase);

            if (!string.IsNullOrEmpty(phaseData?.phaseTransitionMessage))
                OnPhaseTransitionMessage?.Invoke(phaseData.phaseTransitionMessage);

            _animator?.SetTrigger("PhaseTransition");

            OnPhaseChanged?.Invoke(newPhase);
            Debug.Log($"[BossController] Entering Phase {newPhase}");

            yield return new WaitForSeconds(2f);

            // Apply phase attack speed modifier
            float multiplier = phaseData?.attackSpeedMultiplier ?? 1f;
            _currentAttackInterval = definition.baseAttackInterval / multiplier;

            if (phaseData != null && phaseData.requiresSoulAnchorBreak)
                BeginImmunityWindow(phaseData.anchorCount);
            else
                _state = BossState.Combat;
        }

        // ── Immunity / Soul Anchor ─────────────────────────────────────────────────

        private void BeginImmunityWindow(int anchorCount)
        {
            _isImmune = true;
            _anchorsRemaining = anchorCount;
            _state = BossState.Immune;

            OnImmunityChanged?.Invoke(true);
            SpawnAnchors(anchorCount);

            Debug.Log($"[BossController] Immune. Anchors to destroy: {anchorCount}. Enter Soul Realm!");
        }

        private void SpawnAnchors(int count)
        {
            if (soulAnchorPrefab == null)
            {
                Debug.LogWarning("[BossController] soulAnchorPrefab not assigned — cannot spawn anchors.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos;
                if (anchorSpawnPoints != null && i < anchorSpawnPoints.Length && anchorSpawnPoints[i] != null)
                    spawnPos = anchorSpawnPoints[i].position;
                else
                    spawnPos = transform.position + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f));

                Instantiate(soulAnchorPrefab, spawnPos, Quaternion.identity);
            }
        }

        private void HandleAnchorDestroyed(SoulAnchor anchor)
        {
            if (!_isImmune) return;

            _anchorsRemaining = Mathf.Max(0, _anchorsRemaining - 1);
            Debug.Log($"[BossController] Anchor destroyed. Remaining: {_anchorsRemaining}");

            if (_anchorsRemaining == 0)
                EndImmunityWindow();
        }

        private void EndImmunityWindow()
        {
            _isImmune = false;
            _state = BossState.Combat;
            _attackTimer = _currentAttackInterval;

            OnImmunityChanged?.Invoke(false);
            Debug.Log("[BossController] Immunity broken! Boss is vulnerable.");
        }

        // ── Attack loop ────────────────────────────────────────────────────────────

        private void TickAttackTimer()
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                PerformAttack();
                _attackTimer = _currentAttackInterval;
            }
        }

        private void PerformAttack()
        {
            if (playerEntity == null) return;

            var phaseData = GetPhaseData(_currentPhase);
            bool canAOE = phaseData != null && phaseData.enableAOEAttacks;

            if (canAOE && Random.value < definition.aoeAttackChance)
                PerformAOEAttack();
            else
                PerformDirectAttack();
        }

        private void PerformDirectAttack()
        {
            if (playerEntity == null) return;

            var playerData = playerEntity.GetEntityData();
            if (playerData == null || !playerData.IsAlive) return;

            float damage = definition.baseAttackDamage;
            playerData.TakeDamage(damage);

            CombatEvents.TriggerDamageApplied(new CombatEventData
            {
                source = _combatEntity,
                target = playerEntity,
                damageAmount = damage,
                hitPosition = playerEntity.GetHitPoint()
            });

            _animator?.SetTrigger("Attack_1");
        }

        private void PerformAOEAttack()
        {
            _animator?.SetTrigger("AOEAttack");

            var hits = Physics.OverlapSphere(transform.position, definition.aoeAttackRadius);
            foreach (var hit in hits)
            {
                var entity = hit.GetComponentInParent<CombatEntity>();
                if (entity == null || entity == _combatEntity) continue;

                var data = entity.GetEntityData();
                if (data == null || !data.IsAlive) continue;

                data.TakeDamage(definition.aoeAttackDamage);

                CombatEvents.TriggerDamageApplied(new CombatEventData
                {
                    source = _combatEntity,
                    target = entity,
                    damageAmount = definition.aoeAttackDamage,
                    hitPosition = entity.GetHitPoint()
                });
            }
        }

        // ── Damage interception ────────────────────────────────────────────────────

        private void HandleDamageApplied(CombatEventData data)
        {
            if (data.target != _combatEntity) return;

            // While immune: heal back the damage so HP is unchanged, then notify
            if (_isImmune)
            {
                _entityData.Heal(data.damageAmount);
                OnHealthChanged?.Invoke(_entityData.currentHealth, _entityData.maxHealth);
                OnDamageBlocked?.Invoke();
                Debug.Log("[BossController] Hit blocked — enter Soul Realm and destroy the anchor!");
                return;
            }

            OnHealthChanged?.Invoke(_entityData.currentHealth, _entityData.maxHealth);

            if (!_entityData.IsAlive && _state != BossState.Defeated)
                TransitionToDefeated();
        }

        private void TransitionToDefeated()
        {
            _state = BossState.Defeated;
            _animator?.SetTrigger("Die");
            OnBossDefeated?.Invoke();
            Debug.Log($"[BossController] {definition.bossName} has been defeated!");
        }

        // ── Helpers ────────────────────────────────────────────────────────────────

        private BossPhaseData GetPhaseData(int phase)
        {
            if (definition?.phases == null) return null;

            foreach (var p in definition.phases)
            {
                if (p.phaseNumber == phase)
                    return p;
            }

            return null;
        }
    }

    /// <summary>States the boss can occupy during an encounter.</summary>
    public enum BossState
    {
        /// <summary>Not yet started — waiting for encounter trigger.</summary>
        Idle,
        /// <summary>Actively fighting the player.</summary>
        Combat,
        /// <summary>Short pause between phases (animation / narrative moment).</summary>
        PhaseTransition,
        /// <summary>Soul anchors are alive — all incoming damage is reflected.</summary>
        Immune,
        /// <summary>HP reached zero — death animation playing.</summary>
        Defeated
    }
}
