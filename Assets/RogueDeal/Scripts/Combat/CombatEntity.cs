using System;
using UnityEngine;
using RogueDeal.Player;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Combat
{
    public class CombatEntity : MonoBehaviour
    {
        public HeroData heroData;
        
        /// <summary>
        /// Legacy CombatStats property (for backward compatibility).
        /// Use GetEntityData() for new system - that's the primary source of truth.
        /// </summary>
        [System.Obsolete("CombatStats is deprecated. Use GetEntityData() instead. Stats are synced from entityData.")]
        public CombatStats stats => GetStats();
        
        public Animator animator { get; private set; }
        
        [Header("Visual References")]
        public Transform vfxSpawnPoint;
        public Transform hitPoint;
        
        [Header("Animation Settings")]
        public string attackTrigger = "Attack_1";
        public string hitTrigger = "TakeDamage";
        public string dodgeTrigger = "Dodge";
        public string deathTrigger = "Die";
        
        [Header("Targeting Settings")]
        [Tooltip("Cone angle for cone targeting (in degrees). Default: 60")]
        [Range(0f, 180f)]
        public float coneAngle = 60f;

        private CombatAnimationController animationController;
        private CombatVFXController vfxController;
        private CombatSFXController sfxController;
        
        // Core combat data (PRIMARY SOURCE OF TRUTH)
        private CombatEntityData entityData;
        
        // Legacy stats (kept for backward compatibility, synced from entityData)
        private CombatStats _stats;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            
            animationController = GetComponent<CombatAnimationController>();
            vfxController = GetComponent<CombatVFXController>();
            sfxController = GetComponent<CombatSFXController>();
            
            // Initialize entityData first (primary source of truth)
            InitializeEntityData();
            
            // Then sync stats from entityData (for backward compatibility)
            if (heroData != null)
            {
                InitializeStats();
                SyncStatsFromEntityData();
            }
        }
        
        private void Update()
        {
            // Keep position in sync
            if (entityData != null)
            {
                entityData.position = transform.position;
            }
        }

        private void OnEnable()
        {
            CombatEvents.OnAttackStarted += HandleAttackStarted;
            CombatEvents.OnAttackConnected += HandleAttackConnected;
            CombatEvents.OnDamageApplied += HandleDamageApplied;
            CombatEvents.OnHitReactionStarted += HandleHitReaction;
        }

        private void OnDisable()
        {
            CombatEvents.OnAttackStarted -= HandleAttackStarted;
            CombatEvents.OnAttackConnected -= HandleAttackConnected;
            CombatEvents.OnDamageApplied -= HandleDamageApplied;
            CombatEvents.OnHitReactionStarted -= HandleHitReaction;
        }

        /// <summary>
        /// Initializes CombatEntityData (primary source of truth)
        /// </summary>
        private void InitializeEntityData()
        {
            if (entityData != null)
            {
                return;
            }
            
            if (heroData != null && heroData.StatList != null)
            {
                // Create entityData from HeroData
                var healthStat = heroData.StatList.GetStatByType(StatType.Health);
                var attackStat = heroData.StatList.GetStatByType(StatType.Attack);
                var defenseStat = heroData.StatList.GetStatByType(StatType.Defense);
                var magicStat = heroData.StatList.GetStatByType(StatType.Magic);
                var speedStat = heroData.StatList.GetStatByType(StatType.Speed);
                
                entityData = new CombatEntityData(
                    healthStat != null ? healthStat.Amount : 100f,
                    attackStat != null ? attackStat.Amount : 10f,
                    defenseStat != null ? defenseStat.Amount : 5f,
                    magicStat != null ? magicStat.Amount : 0f,
                    speedStat != null ? speedStat.Amount : 5f
                )
                {
                    position = transform.position,
                    originPosition = transform.position
                };
            }
            else
            {
                // Create default entityData
                entityData = new CombatEntityData(100f, 10f, 5f);
                entityData.position = transform.position;
                entityData.originPosition = transform.position;
            }
        }
        
        /// <summary>
        /// Initializes legacy CombatStats (for backward compatibility)
        /// Stats are synced FROM entityData, not the other way around
        /// </summary>
        private void InitializeStats()
        {
            if (_stats != null)
            {
                return;
            }

            if (heroData != null && heroData.StatList != null)
            {
                _stats = new CombatStats(heroData.StatList);
                _stats.OnHealthChanged += OnHealthChanged;
                _stats.OnDeath += OnDeath;
            }
            else
            {
                Debug.LogWarning($"CombatEntity on {gameObject.name} has no HeroData or StatList assigned");
                _stats = new CombatStats(null);
            }
        }
        
        /// <summary>
        /// Syncs legacy CombatStats from CombatEntityData (primary source)
        /// </summary>
        private void SyncStatsFromEntityData()
        {
            if (entityData == null || _stats == null)
                return;
            
            // Sync health by calculating the difference and applying it
            float currentHealthDiff = entityData.currentHealth - _stats.CurrentHealth;
            if (currentHealthDiff > 0)
            {
                _stats.Heal(currentHealthDiff);
            }
            else if (currentHealthDiff < 0)
            {
                _stats.TakeDamage(-currentHealthDiff);
            }
            
            // Note: Other stats are read-only in CombatStats, so we can't sync them
            // This is fine - entityData is the source of truth for new system
        }
        
        /// <summary>
        /// Syncs CombatEntityData from legacy CombatStats (for backward compatibility during transition)
        /// </summary>
        private void SyncEntityDataFromStats()
        {
            if (_stats == null || entityData == null)
                return;
            
            // Sync health
            entityData.currentHealth = _stats.CurrentHealth;
            entityData.maxHealth = _stats.MaxHealth;
            
            // Sync other stats
            entityData.attack = _stats.GetStat(StatType.Attack);
            entityData.defense = _stats.GetStat(StatType.Defense);
            entityData.magicPower = _stats.GetStat(StatType.Magic);
            entityData.speed = _stats.GetStat(StatType.Speed);
        }

        public void SetHeroData(HeroData data)
        {
            heroData = data;
            InitializeStats();
        }
        
        public void InitializeStatsWithoutHeroData(float maxHealth = 100f, float attack = 10f, float defense = 5f)
        {
            // Initialize entityData first (primary)
            if (entityData == null)
            {
                entityData = new CombatEntityData(maxHealth, attack, defense);
                entityData.position = transform.position;
                entityData.originPosition = transform.position;
            }
            
            // Initialize stats for backward compatibility
            if (_stats != null)
            {
                Debug.LogWarning($"[CombatEntity] Stats already initialized on {gameObject.name}");
                SyncStatsFromEntityData();
                return;
            }
            
            _stats = new CombatStats(maxHealth, attack, defense);
            _stats.OnHealthChanged += OnHealthChanged;
            _stats.OnDeath += OnDeath;
            
            // Sync stats from entityData
            SyncStatsFromEntityData();
            
            Debug.Log($"[CombatEntity] Initialized stats on {gameObject.name} (HP: {maxHealth}, ATK: {attack}, DEF: {defense})");
        }
        
        public void ForceInitializeStats(float maxHealth, float attack, float defense)
        {
            // Initialize/update entityData first (primary)
            if (entityData == null)
            {
                entityData = new CombatEntityData(maxHealth, attack, defense);
                entityData.position = transform.position;
                entityData.originPosition = transform.position;
            }
            else
            {
                entityData.maxHealth = maxHealth;
                entityData.currentHealth = maxHealth;
                entityData.attack = attack;
                entityData.defense = defense;
            }
            
            // Update stats for backward compatibility
            if (_stats != null)
            {
                _stats.OnHealthChanged -= OnHealthChanged;
                _stats.OnDeath -= OnDeath;
            }
            
            _stats = new CombatStats(maxHealth, attack, defense);
            _stats.OnHealthChanged += OnHealthChanged;
            _stats.OnDeath += OnDeath;
            
            // Sync stats from entityData
            SyncStatsFromEntityData();
            
            Debug.Log($"[CombatEntity] Force-initialized stats on {gameObject.name} (HP: {maxHealth}, ATK: {attack}, DEF: {defense})");
        }

        private void HandleAttackStarted(CombatEventData data)
        {
            if (data.source == this)
            {
                // Note: CombatEventData still uses AbilityData for old system compatibility
                // New system uses CombatAction directly via CombatExecutor
                // This handler is for legacy event system
                if (data.ability != null)
                {
                    PlayAttackAnimation(data.ability);
                }
            }
        }

        private void HandleAttackConnected(CombatEventData data)
        {
            if (data.target == this)
            {
                vfxController?.PlayHitVFX(data.hitPosition, data.wasCritical);
                sfxController?.PlayHitSFX(data.effect.effectType);
            }
        }

        private void HandleDamageApplied(CombatEventData data)
        {
            if (data.target == this)
            {
                EnemyVisual enemyVisual = GetComponentInParent<EnemyVisual>();
                if (enemyVisual != null)
                {
                    enemyVisual.AnimateDamage(Mathf.RoundToInt(data.damageAmount), data.wasCritical);
                }
                else
                {
                    PlayerVisual playerVisual = GetComponentInParent<PlayerVisual>();
                    if (playerVisual != null)
                    {
                        playerVisual.AnimateDamage(Mathf.RoundToInt(data.damageAmount));
                    }
                }
            }
        }

        private void HandleHitReaction(CombatEventData data)
        {
            // Use entityData for alive check (primary source of truth)
            bool isAlive = entityData != null ? entityData.IsAlive : (_stats != null && _stats.IsAlive);
            if (data.target == this && isAlive)
            {
                PlayHitReaction(data.effect.effectType);
            }
        }

        private void PlayAttackAnimation(AbilityData ability)
        {
            if (animationController == null)
            {
                animationController = GetComponent<CombatAnimationController>();
            }
            
            if (animationController != null)
            {
                animationController.PlayAttack(ability);
            }
            else if (animator != null)
            {
                if (animator.runtimeAnimatorController == null)
                {
                    return;
                }
                
                animator.SetTrigger(attackTrigger);
            }
        }

        private void PlayHitReaction(EffectType effectType)
        {
            if (animationController == null)
            {
                animationController = GetComponent<CombatAnimationController>();
            }
            
            if (animationController != null)
            {
                animationController.PlayHitReaction(effectType);
            }
            else if (animator != null)
            {
                animator.SetTrigger(hitTrigger);
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            // Sync entityData when stats change (for backward compatibility)
            if (entityData != null)
            {
                entityData.currentHealth = current;
                entityData.maxHealth = max;
            }
        }

        private void OnDeath()
        {
            // Sync entityData
            if (entityData != null)
            {
                entityData.currentHealth = 0;
            }
            
            animator?.SetTrigger(deathTrigger);
        }

        public Vector3 GetHitPoint()
        {
            return hitPoint != null ? hitPoint.position : transform.position + Vector3.up;
        }
        
        /// <summary>
        /// Gets the CombatEntityData for this entity (PRIMARY SOURCE OF TRUTH).
        /// Creates it if it doesn't exist.
        /// </summary>
        public CombatEntityData GetEntityData()
        {
            if (entityData == null)
            {
                InitializeEntityData();
            }
            
            // Always sync position
            if (entityData != null)
            {
                entityData.position = transform.position;
            }
            
            return entityData;
        }
        
        /// <summary>
        /// Sets the CombatEntityData for this entity (used when initializing from new system).
        /// This becomes the primary source of truth.
        /// </summary>
        public void SetEntityData(CombatEntityData data)
        {
            entityData = data;
            
            // Sync to old stats system if it exists (for backward compatibility)
            if (_stats != null && data != null)
            {
                SyncStatsFromEntityData();
            }
        }
        
        /// <summary>
        /// Gets legacy CombatStats (for backward compatibility).
        /// Stats are synced FROM entityData, not the other way around.
        /// </summary>
        public CombatStats GetStats()
        {
            // Ensure entityData exists first
            if (entityData == null)
            {
                InitializeEntityData();
            }
            
            // Create stats if they don't exist
            if (_stats == null)
            {
                InitializeStats();
                SyncStatsFromEntityData();
            }
            
            return _stats;
        }
    }
}
