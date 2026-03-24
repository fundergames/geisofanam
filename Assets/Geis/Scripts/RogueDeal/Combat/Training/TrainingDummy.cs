using UnityEngine;
using RogueDeal.Enemies;

namespace RogueDeal.Combat.Training
{
    public class TrainingDummy : MonoBehaviour
    {
        [Header("Dummy Stats")]
        [SerializeField] private float maxHealth = 1000f;
        [SerializeField] private bool infiniteHealth = true;
        
        [Header("Behavior")]
        [SerializeField] private DummyBehavior behavior = DummyBehavior.Idle;
        [SerializeField] private float dodgeChance = 0.5f;
        [SerializeField] private float blockChance = 0.5f;
        [SerializeField] private float counterChance = 0.5f;
        
        [Header("Visual Feedback")]
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color blockColor = Color.blue;
        [SerializeField] private Color dodgeColor = Color.green;
        [SerializeField] private Color counterColor = Color.red;
        [SerializeField] private Color hitColor = Color.yellow;
        
        private float currentHealth;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private CombatEntity combatEntity;
        private Animator animator;
        private Renderer[] renderers;
        private int hitCounter = 0;
        private float lastHitTime;
        
        public int HitCount => hitCounter;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        
        private void Awake()
        {
            combatEntity = GetComponent<CombatEntity>();
            if (combatEntity == null)
            {
                Debug.LogError($"[TrainingDummy] No CombatEntity found on {gameObject.name}! Adding one...");
                combatEntity = gameObject.AddComponent<CombatEntity>();
            }
            
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            
            renderers = GetComponentsInChildren<Renderer>();
            
            startPosition = transform.position;
            startRotation = transform.rotation;
            currentHealth = maxHealth;
        }
        
        private void Start()
        {
            if (combatEntity != null)
            {
                Debug.Log($"[TrainingDummy] Setting up training stats for {gameObject.name} (HP: {maxHealth}, DEF: 5)");
                combatEntity.ForceInitializeStats(maxHealth, 10f, 5f);
            }
        }
        
        private void OnEnable()
        {
            CombatEvents.OnDamageApplied += OnDamageReceived;
        }
        
        private void OnDisable()
        {
            CombatEvents.OnDamageApplied -= OnDamageReceived;
        }
        
        private void OnDamageReceived(CombatEventData data)
        {
            if (data.target != combatEntity) return;
            
            hitCounter++;
            lastHitTime = Time.time;
            
            if (!infiniteHealth)
            {
                currentHealth -= data.damageAmount;
                currentHealth = Mathf.Max(0, currentHealth);
            }
            else
            {
                // Infinite health: restore entityData so hit detection still finds us alive
                var entityData = combatEntity.GetEntityData();
                if (entityData != null)
                    entityData.currentHealth = entityData.maxHealth;
                currentHealth = maxHealth;
            }
            
            ReactToDamage(data);
            
            FlashColor(hitColor, 0.2f);
            
            Debug.Log($"[TrainingDummy] Hit #{hitCounter} | Damage: {data.damageAmount} | Health: {currentHealth}/{maxHealth}");
        }
        
        private void ReactToDamage(CombatEventData data)
        {
            switch (behavior)
            {
                case DummyBehavior.Idle:
                    break;
                    
                case DummyBehavior.Block:
                    if (Random.value < blockChance && animator != null)
                    {
                        animator.SetTrigger("Block");
                        FlashColor(blockColor, 0.3f);
                    }
                    break;
                    
                case DummyBehavior.Dodge:
                    if (Random.value < dodgeChance && animator != null)
                    {
                        animator.SetTrigger("Dodge");
                        FlashColor(dodgeColor, 0.3f);
                    }
                    break;
                    
                case DummyBehavior.Counter:
                    if (Random.value < counterChance && animator != null)
                    {
                        animator.SetTrigger("Counter");
                        FlashColor(counterColor, 0.3f);
                    }
                    break;
                    
                case DummyBehavior.Random:
                    float randomValue = Random.value;
                    if (randomValue < 0.33f && animator != null)
                    {
                        animator.SetTrigger("Block");
                    }
                    else if (randomValue < 0.66f && animator != null)
                    {
                        animator.SetTrigger("Dodge");
                    }
                    break;
            }
        }
        
        private void FlashColor(Color color, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(FlashColorCoroutine(color, duration));
        }
        
        private System.Collections.IEnumerator FlashColorCoroutine(Color color, float duration)
        {
            foreach (Renderer renderer in renderers)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(block);
            }
            
            yield return new WaitForSeconds(duration);
            
            foreach (Renderer renderer in renderers)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", GetBehaviorColor());
                renderer.SetPropertyBlock(block);
            }
        }
        
        private Color GetBehaviorColor()
        {
            switch (behavior)
            {
                case DummyBehavior.Block: return blockColor;
                case DummyBehavior.Dodge: return dodgeColor;
                case DummyBehavior.Counter: return counterColor;
                default: return idleColor;
            }
        }
        
        public void Reset()
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
            currentHealth = maxHealth;
            hitCounter = 0;
            
            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }
            
            Debug.Log("[TrainingDummy] Reset");
        }
        
        public void SetBehavior(DummyBehavior newBehavior)
        {
            behavior = newBehavior;
            FlashColor(GetBehaviorColor(), 0.5f);
        }
        
        public void SetInfiniteHealth(bool infinite)
        {
            infiniteHealth = infinite;
            if (infinite)
            {
                currentHealth = maxHealth;
            }
        }
        
        public void SetHealth(float health)
        {
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
        }
    }
}
