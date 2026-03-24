using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Persistent AOE zone that pulses multiple times over duration.
    /// Only affects entities currently in zone during each pulse.
    /// </summary>
    public class PersistentAOE : MonoBehaviour
    {
        [Header("AOE Settings")]
        [Tooltip("Radius of the AOE")]
        public float radius = 5f;
        
        [Tooltip("Layer mask for valid targets")]
        public LayerMask targetLayers;
        
        [Tooltip("Visual indicator prefab (optional)")]
        public GameObject visualIndicator;
        
        private BaseEffect[] effects;
        private CombatEntityData attackerData;
        private int pulseCount;
        private float pulseDuration;
        private int currentPulse = 0;
        private float timeSinceLastPulse = 0f;
        private bool isActive = true;
        
        private HashSet<CombatEntity> entitiesInZone = new HashSet<CombatEntity>();
        
        private void Awake()
        {
            // Create visual indicator if needed
            if (visualIndicator != null)
            {
                var indicator = Instantiate(visualIndicator, transform.position, Quaternion.identity, transform);
                // Scale indicator to match radius
                float scale = radius * 2f;
                indicator.transform.localScale = new Vector3(scale, 1f, scale);
            }
        }
        
        /// <summary>
        /// Initializes the AOE with effects, pulse count, and duration
        /// </summary>
        public void Initialize(float radius, BaseEffect[] effects, int pulseCount, float pulseDuration, CombatEntityData attackerData)
        {
            this.radius = radius;
            this.effects = effects;
            this.pulseCount = pulseCount;
            this.pulseDuration = pulseDuration;
            this.attackerData = attackerData;
            this.currentPulse = 0;
            this.timeSinceLastPulse = 0f;
            this.isActive = true;
            
            // Update visual indicator scale
            if (visualIndicator != null)
            {
                float scale = radius * 2f;
                visualIndicator.transform.localScale = new Vector3(scale, 1f, scale);
            }
        }
        
        private void Update()
        {
            if (!isActive) return;
            
            timeSinceLastPulse += Time.deltaTime;
            
            // Check if it's time for next pulse
            if (timeSinceLastPulse >= pulseDuration)
            {
                Pulse();
                timeSinceLastPulse = 0f;
                currentPulse++;
                
                // Check if all pulses complete
                if (currentPulse >= pulseCount)
                {
                    Despawn();
                }
            }
        }
        
        private void Pulse()
        {
            // Find all entities currently in zone
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius, targetLayers);
            entitiesInZone.Clear();
            
            foreach (var collider in colliders)
            {
                var entity = collider.GetComponent<CombatEntity>();
                if (entity == null)
                {
                    entity = collider.GetComponentInParent<CombatEntity>();
                }
                
                if (entity != null)
                {
                    var entityData = entity.GetEntityData();
                    if (entityData != null && entityData.IsAlive)
                    {
                        entitiesInZone.Add(entity);
                    }
                }
            }
            
            // Apply effects to entities currently in zone
            foreach (var entity in entitiesInZone)
            {
                if (entity == null) continue;
                
                var targetData = entity.GetEntityData();
                if (targetData == null || !targetData.IsAlive) continue;
                
                // Don't hit self
                if (attackerData != null)
                {
                    // Check if this entity is the attacker (would need entity reference)
                    // For now, we'll apply to all entities in zone
                }
                
                foreach (var effect in effects)
                {
                    if (effect == null) continue;
                    
                    var calculated = effect.Calculate(attackerData, targetData, attackerData?.equippedWeapon);
                    effect.Apply(targetData, calculated);
                }
            }
            
            // Spawn pulse VFX (optional)
            // TODO: Add pulse VFX
        }
        
        private void Despawn()
        {
            isActive = false;
            Destroy(gameObject);
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw radius in editor
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}

