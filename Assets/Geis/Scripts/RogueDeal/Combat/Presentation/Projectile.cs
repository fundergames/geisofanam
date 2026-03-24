using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Projectile that moves toward a target and applies effects on arrival.
    /// Uses predicted collision (no physics) to avoid tunneling issues.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [Tooltip("Speed of the projectile")]
        public float speed = 10f;
        
        [Tooltip("Distance threshold to consider target reached")]
        public float arrivalThreshold = 0.5f;
        
        [Tooltip("Maximum lifetime in seconds")]
        public float maxLifetime = 10f;
        
        private Transform target;
        private BaseEffect[] effects;
        private CombatEntityData attackerData;
        private float lifetime = 0f;
        private bool hasArrived = false;
        private Rigidbody rb;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = true; // We'll move manually
            }
        }
        
        /// <summary>
        /// Initializes the projectile with target, speed, effects, and attacker data
        /// </summary>
        public void Initialize(Transform target, float speed, BaseEffect[] effects, CombatEntityData attackerData)
        {
            this.target = target;
            this.speed = speed;
            this.effects = effects;
            this.attackerData = attackerData;
            this.lifetime = 0f;
            this.hasArrived = false;
            
            // Face target
            if (target != null)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }
        
        private void Update()
        {
            if (hasArrived) return;
            
            lifetime += Time.deltaTime;
            
            // Check lifetime
            if (lifetime >= maxLifetime)
            {
                Despawn();
                return;
            }
            
            // Check if target is still valid
            if (target == null)
            {
                Despawn();
                return;
            }
            
            // Move toward target
            Vector3 direction = (target.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, target.position);
            
            // Check if we've arrived
            if (distance <= arrivalThreshold)
            {
                OnArrival();
                return;
            }
            
            // Move
            float moveDistance = speed * Time.deltaTime;
            if (moveDistance > distance)
            {
                // Would overshoot, just move to target
                transform.position = target.position;
                OnArrival();
            }
            else
            {
                transform.position += direction * moveDistance;
            }
        }
        
        private void OnArrival()
        {
            if (hasArrived) return;
            hasArrived = true;
            
            // Apply effects to target
            if (target != null && effects != null && attackerData != null)
            {
                var targetEntity = target.GetComponent<CombatEntity>();
                if (targetEntity != null)
                {
                    var targetData = targetEntity.GetEntityData();
                    if (targetData != null && targetData.IsAlive)
                    {
                        foreach (var effect in effects)
                        {
                            if (effect == null) continue;
                            
                            var calculated = effect.Calculate(attackerData, targetData, attackerData.equippedWeapon);
                            effect.Apply(targetData, calculated);
                        }
                    }
                }
            }
            
            // Spawn impact effect (optional)
            // TODO: Add impact VFX
            
            Despawn();
        }
        
        private void Despawn()
        {
            Destroy(gameObject);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Optional: Handle collision with environment
            // For now, we use predicted collision only
        }
    }
}

