using UnityEngine;
using UnityEngine.Events;

namespace FunderGames.RPG.OpenWorld
{
    /// <summary>
    /// Health system for players and enemies
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isInvulnerable = false;
        [SerializeField] private float invulnerabilityTime = 0.5f;
        
        [Header("Events")]
        [SerializeField] private UnityEvent onDamage;
        [SerializeField] private UnityEvent onDeath;
        [SerializeField] private UnityEvent onHeal;
        [SerializeField] private UnityEvent<float> onHealthChanged;
        
        // Private variables
        private float invulnerabilityTimer;
        private bool isDead = false;
        
        // Properties
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public bool IsDead => isDead;
        public bool IsInvulnerable => isInvulnerable;
        
        protected virtual void Start()
        {
            currentHealth = maxHealth;
            onHealthChanged?.Invoke(HealthPercentage);
        }
        
        private void Update()
        {
            UpdateInvulnerability();
        }
        
        /// <summary>
        /// Update invulnerability timer
        /// </summary>
        private void UpdateInvulnerability()
        {
            if (isInvulnerable && invulnerabilityTimer > 0)
            {
                invulnerabilityTimer -= Time.deltaTime;
                if (invulnerabilityTimer <= 0)
                {
                    isInvulnerable = false;
                }
            }
        }
        
        /// <summary>
        /// Take damage
        /// </summary>
        public virtual void TakeDamage(float damage)
        {
            if (isDead || isInvulnerable) return;
            
            currentHealth = Mathf.Max(0, currentHealth - damage);
            onHealthChanged?.Invoke(HealthPercentage);
            onDamage?.Invoke();
            
            // Set invulnerability
            if (invulnerabilityTime > 0)
            {
                isInvulnerable = true;
                invulnerabilityTimer = invulnerabilityTime;
            }
            
            // Check for death
            if (currentHealth <= 0 && !isDead)
            {
                Die();
            }
            
            Debug.Log($"{name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        }
        
        /// <summary>
        /// Heal health
        /// </summary>
        public virtual void Heal(float healAmount)
        {
            if (isDead) return;
            
            float oldHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
            float actualHeal = currentHealth - oldHealth;
            
            if (actualHeal > 0)
            {
                onHealthChanged?.Invoke(HealthPercentage);
                onHeal?.Invoke();
                Debug.Log($"{name} healed for {actualHeal}. Health: {currentHealth}/{maxHealth}");
            }
        }
        
        /// <summary>
        /// Set health to specific value
        /// </summary>
        public virtual void SetHealth(float health)
        {
            if (isDead) return;
            
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
            onHealthChanged?.Invoke(HealthPercentage);
            
            if (currentHealth <= 0 && !isDead)
            {
                Die();
            }
        }
        
        /// <summary>
        /// Set max health
        /// </summary>
        public virtual void SetMaxHealth(float newMaxHealth)
        {
            if (newMaxHealth <= 0) return;
            
            float healthRatio = HealthPercentage;
            maxHealth = newMaxHealth;
            currentHealth = maxHealth * healthRatio;
            onHealthChanged?.Invoke(HealthPercentage);
        }
        
        /// <summary>
        /// Handle death
        /// </summary>
        protected virtual void Die()
        {
            isDead = true;
            onDeath?.Invoke();
            Debug.Log($"{name} died!");
        }
        
        /// <summary>
        /// Revive the entity
        /// </summary>
        public virtual void Revive(float healthPercentage = 1f)
        {
            if (!isDead) return;
            
            isDead = false;
            currentHealth = maxHealth * healthPercentage;
            onHealthChanged?.Invoke(HealthPercentage);
            Debug.Log($"{name} revived with {healthPercentage * 100}% health!");
        }
        
        /// <summary>
        /// Make entity invulnerable for a duration
        /// </summary>
        public virtual void MakeInvulnerable(float duration)
        {
            isInvulnerable = true;
            invulnerabilityTimer = duration;
        }
        
        /// <summary>
        /// Remove invulnerability
        /// </summary>
        public virtual void RemoveInvulnerability()
        {
            isInvulnerable = false;
            invulnerabilityTimer = 0;
        }
        
        /// <summary>
        /// Check if entity can take damage
        /// </summary>
        public virtual bool CanTakeDamage()
        {
            return !isDead && !isInvulnerable;
        }
        
        /// <summary>
        /// Get damage after considering armor/resistance
        /// </summary>
        protected virtual float CalculateDamage(float baseDamage)
        {
            // Override in derived classes to add armor/resistance logic
            return baseDamage;
        }
        
        /// <summary>
        /// Reset health to full
        /// </summary>
        public virtual void ResetHealth()
        {
            currentHealth = maxHealth;
            onHealthChanged?.Invoke(HealthPercentage);
        }
        
        /// <summary>
        /// Get health as a string
        /// </summary>
        public override string ToString()
        {
            return $"{currentHealth:F0}/{maxHealth:F0}";
        }
    }
    
    /// <summary>
    /// Player-specific health system
    /// </summary>
    public class PlayerHealth : HealthSystem
    {
        [Header("Player Settings")]
        [SerializeField] private float respawnDelay = 3f;
        [SerializeField] private Vector3 respawnPoint;
        [SerializeField] private bool useRespawnPoint = true;
        
        private PlayerController playerController;
        private CharacterController characterController;
        
        protected override void Start()
        {
            base.Start();
            playerController = GetComponent<PlayerController>();
            characterController = GetComponent<CharacterController>();
            
            if (respawnPoint == Vector3.zero)
                respawnPoint = transform.position;
        }
        
        protected override void Die()
        {
            base.Die();
            
            // Disable player controller
            if (playerController != null)
                playerController.enabled = false;
                
            // Disable character controller
            if (characterController != null)
                characterController.enabled = false;
            
            // Respawn after delay
            Invoke(nameof(Respawn), respawnDelay);
        }
        
        /// <summary>
        /// Respawn the player
        /// </summary>
        private void Respawn()
        {
            if (useRespawnPoint)
            {
                transform.position = respawnPoint;
            }
            
            Revive(0.5f); // Revive with 50% health
            
            // Re-enable components
            if (playerController != null)
                playerController.enabled = true;
                
            if (characterController != null)
                characterController.enabled = true;
        }
        
        /// <summary>
        /// Set respawn point
        /// </summary>
        public void SetRespawnPoint(Vector3 point)
        {
            respawnPoint = point;
        }
    }
    
    /// <summary>
    /// Enemy-specific health system
    /// </summary>
    public class EnemyHealth : HealthSystem
    {
        [Header("Enemy Settings")]
        [SerializeField] private float deathDelay = 2f;
        [SerializeField] private GameObject[] dropItems;
        [SerializeField] private float dropChance = 0.3f;
        [SerializeField] private int experienceValue = 10;
        
        private EnemyAI enemyAI;
        private UnityEngine.AI.NavMeshAgent agent;
        private Animator animator;
        
        protected override void Start()
        {
            base.Start();
            enemyAI = GetComponent<EnemyAI>();
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            animator = GetComponent<Animator>();
        }
        
        protected override void Die()
        {
            base.Die();
            
            // Disable AI and movement
            if (enemyAI != null)
                enemyAI.enabled = false;
                
            if (agent != null)
                agent.enabled = false;
            
            // Play death animation
            if (animator != null)
                animator.SetTrigger("Die");
            
            // Drop items
            DropItems();
            
            // Award experience (if player exists)
            AwardExperience();
            
            // Destroy after delay
            Destroy(gameObject, deathDelay);
        }
        
        /// <summary>
        /// Drop items on death
        /// </summary>
        private void DropItems()
        {
            if (dropItems.Length == 0 || Random.value > dropChance) return;
            
            GameObject itemToDrop = dropItems[Random.Range(0, dropItems.Length)];
            if (itemToDrop != null)
            {
                Vector3 dropPosition = transform.position + Random.insideUnitSphere * 2f;
                dropPosition.y = transform.position.y;
                Instantiate(itemToDrop, dropPosition, Quaternion.identity);
            }
        }
        
        /// <summary>
        /// Award experience to player
        /// </summary>
        private void AwardExperience()
        {
            // This would integrate with your experience/leveling system
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Example: player.GetComponent<PlayerLeveling>()?.GainExperience(experienceValue);
                Debug.Log($"Player gained {experienceValue} experience from {name}!");
            }
        }
    }
}
