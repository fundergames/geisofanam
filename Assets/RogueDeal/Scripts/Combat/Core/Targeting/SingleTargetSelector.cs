using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat;

namespace RogueDeal.Combat.Core.Targeting
{
    /// <summary>
    /// Targets the current enemy (for video poker mode)
    /// </summary>
    [CreateAssetMenu(fileName = "Targeting_SingleTarget", menuName = "RogueDeal/Combat/Targeting/Single Target Selector")]
    public class SingleTargetSelector : TargetingStrategy
    {
        [Header("Targeting Settings")]
        [Tooltip("Layer mask for valid targets")]
        public LayerMask targetLayers;
        
        [Tooltip("Maximum range for targeting (used as default if weapon/profile don't specify range)")]
        public float maxRange = 20f;
        
        public override TargetResult ResolveTargets(CombatEntityData attacker)
        {
            // Sync all entity positions first
            SyncAllEntityPositions();
            
            // Get attacker's CombatEntity to exclude it from targeting
            CombatEntity attackerEntity = null;
            var allEntitiesForAttacker = Object.FindObjectsByType<CombatEntity>(FindObjectsSortMode.None);
            foreach (var entity in allEntitiesForAttacker)
            {
                var data = entity.GetEntityData();
                if (data == attacker)
                {
                    attackerEntity = entity;
                    break;
                }
            }
            
            // Get max range (from weapon, profile, or default)
            float maxRange = GetMaxRange(attacker);
            
            // Primary method: Direct CombatEntity search (works without colliders)
            // This is more reliable and doesn't require colliders to be set up
            CombatEntity nearest = null;
            float nearestDistance = float.MaxValue;
            
            var allEntities = Object.FindObjectsByType<CombatEntity>(FindObjectsSortMode.None);
            
            foreach (var entity in allEntities)
            {
                // Skip attacker
                if (entity == attackerEntity)
                {
                    continue;
                }
                
                var entityData = entity.GetEntityData();
                if (entityData == null)
                {
                    continue;
                }
                
                if (!entityData.IsAlive)
                {
                    continue;
                }
                
                // Sync position
                entityData.position = entity.transform.position;
                
                float distance = Vector3.Distance(attacker.position, entityData.position);
                
                // Debug: Log distance check
                // if (distance <= maxRange)
                // {
                //     Debug.Log($"[SingleTargetSelector] Entity {entity.name} is within range: {distance:F2} / {maxRange}");
                // }
                
                if (distance <= maxRange && distance < nearestDistance)
                {
                    // Optional: Filter by layer mask if set
                    if (targetLayers != 0)
                    {
                        int entityLayer = 1 << entity.gameObject.layer;
                        if ((targetLayers.value & entityLayer) == 0)
                        {
                            continue; // Entity not in target layers
                        }
                    }
                    
                    nearestDistance = distance;
                    nearest = entity;
                }
            }
            
            if (nearest != null)
            {
                var targets = new List<CombatEntity> { nearest };
                var targetData = nearest.GetEntityData();
                if (targetData != null)
                {
                    targetData.position = nearest.transform.position;
                    return new TargetResult(targets, targetData.position, true);
                }
                return new TargetResult(targets, nearest.transform.position, true);
            }
            
            // Debug.LogWarning($"[SingleTargetSelector] ❌ No valid targets found within range {maxRange} from position {attacker.position}");
            
            // Fallback: Try collider-based detection (if colliders are set up)
            // This is more performant but requires colliders on entities
            LayerMask layersToUse = targetLayers;
            if (layersToUse == 0)
            {
                layersToUse = -1; // All layers
            }
            
            Collider[] colliders = Physics.OverlapSphere(
                attacker.position,
                maxRange,
                layersToUse
            );
            
            if (colliders.Length > 0)
            {
                foreach (var collider in colliders)
                {
                    // Skip if this is the attacker
                    if (attackerEntity != null && collider.transform == attackerEntity.transform)
                    {
                        continue;
                    }
                    
                    // Skip if this collider is a child of the attacker
                    if (attackerEntity != null && collider.transform.IsChildOf(attackerEntity.transform))
                    {
                        continue;
                    }
                    
                    // Try to find CombatEntity on the collider's GameObject or its parents
                    var entity = collider.GetComponent<CombatEntity>();
                    if (entity == null)
                    {
                        entity = collider.GetComponentInParent<CombatEntity>();
                    }
                    
                    // Also try children (in case collider is a child)
                    if (entity == null)
                    {
                        entity = collider.GetComponentInChildren<CombatEntity>();
                    }
                    
                    if (entity == null || entity == attackerEntity)
                    {
                        continue;
                    }
                    
                    var entityData = entity.GetEntityData();
                    if (entityData == null || !entityData.IsAlive)
                    {
                        continue;
                    }
                    
                    // Sync position
                    entityData.position = entity.transform.position;
                    
                    float distance = Vector3.Distance(attacker.position, entityData.position);
                    if (distance <= maxRange && distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = entity;
                    }
                }
                
                if (nearest != null)
                {
                    var targets = new List<CombatEntity> { nearest };
                    var targetData = nearest.GetEntityData();
                    if (targetData != null)
                    {
                        targetData.position = nearest.transform.position;
                        return new TargetResult(targets, targetData.position, true);
                    }
                    return new TargetResult(targets, nearest.transform.position, true);
                }
            }
            
            // No targets found
            return new TargetResult(null, attacker.position, false);
        }
        
        private float GetMaxRange(CombatEntityData attacker)
        {
            // Priority: Weapon > CombatProfile > Default
            if (attacker.equippedWeapon != null && attacker.equippedWeapon.maxRange > 0)
            {
                return attacker.equippedWeapon.maxRange;
            }
            
            if (attacker.combatProfile != null && attacker.combatProfile.engagementDistance > 0)
            {
                return attacker.combatProfile.engagementDistance;
            }
            
            return maxRange;
        }
    }
}

