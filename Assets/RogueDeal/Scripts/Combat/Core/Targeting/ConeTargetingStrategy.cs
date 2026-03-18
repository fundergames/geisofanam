using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat;

namespace RogueDeal.Combat.Core.Targeting
{
    /// <summary>
    /// Targets the nearest enemy within a cone of view and attack range
    /// </summary>
    [CreateAssetMenu(fileName = "Targeting_Cone", menuName = "RogueDeal/Combat/Targeting/Cone Targeting")]
    public class ConeTargetingStrategy : TargetingStrategy
    {
        [Header("Targeting Settings")]
        [Tooltip("Layer mask for valid targets")]
        public LayerMask targetLayers;
        
        [Tooltip("Default range if weapon doesn't specify one")]
        public float defaultRange = 2f;
        
        [Tooltip("Default cone angle if character doesn't specify one (in degrees)")]
        [Range(0f, 180f)]
        public float defaultConeAngle = 60f;
        
        public override TargetResult ResolveTargets(CombatEntityData attacker)
        {
            SyncAllEntityPositions();
            
            // Get attacker's CombatEntity
            CombatEntity attackerEntity = FindCombatEntity(attacker);
            if (attackerEntity == null)
            {
                return new TargetResult(null, attacker.position, false);
            }
            
            // Get max range and cone angle
            float maxRange = GetMaxRange(attacker);
            float coneAngle = GetConeAngle(attackerEntity);
            
            // Get forward direction (from attacker's transform)
            Vector3 forwardDirection = attackerEntity != null 
                ? attackerEntity.transform.forward 
                : Vector3.forward;
            
            // If no layer mask set, use all layers
            LayerMask layersToUse = targetLayers;
            if (layersToUse == 0)
            {
                layersToUse = -1; // All layers
            }
            
            // Find all colliders in range
            Collider[] colliders = Physics.OverlapSphere(attacker.position, maxRange, layersToUse);
            
            // Find nearest valid target within cone
            CombatEntity nearest = null;
            float nearestDistance = float.MaxValue;
            
            float halfConeAngle = coneAngle * 0.5f;
            
            foreach (var collider in colliders)
            {
                // Skip attacker
                if (attackerEntity != null && 
                    (collider.transform == attackerEntity.transform || 
                     collider.transform.IsChildOf(attackerEntity.transform)))
                {
                    continue;
                }
                
                // Find CombatEntity
                var entity = collider.GetComponent<CombatEntity>();
                if (entity == null)
                {
                    entity = collider.GetComponentInParent<CombatEntity>();
                }
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
                
                // Check distance
                float distance = Vector3.Distance(attacker.position, entityData.position);
                if (distance > maxRange)
                {
                    continue;
                }
                
                // Check if within cone
                Vector3 directionToTarget = (entityData.position - attacker.position).normalized;
                float angleToTarget = Vector3.Angle(forwardDirection, directionToTarget);
                
                if (angleToTarget <= halfConeAngle)
                {
                    // Within cone - check if closest
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = entity;
                    }
                }
            }
            
            // Fallback: search all CombatEntities if collider search failed
            if (nearest == null)
            {
                var allEntities = Object.FindObjectsOfType<CombatEntity>();
                foreach (var entity in allEntities)
                {
                    if (entity == attackerEntity) continue;
                    
                    var entityData = entity.GetEntityData();
                    if (entityData == null || !entityData.IsAlive) continue;
                    
                    entityData.position = entity.transform.position;
                    float distance = Vector3.Distance(attacker.position, entityData.position);
                    
                    if (distance > maxRange) continue;
                    
                    // Check cone
                    Vector3 directionToTarget = (entityData.position - attacker.position).normalized;
                    float angleToTarget = Vector3.Angle(forwardDirection, directionToTarget);
                    
                    if (angleToTarget <= halfConeAngle && distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = entity;
                    }
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
            
            return defaultRange;
        }
        
        private float GetConeAngle(CombatEntity entity)
        {
            if (entity != null && entity.coneAngle > 0)
            {
                return entity.coneAngle;
            }
            
            return defaultConeAngle;
        }
        
        private CombatEntity FindCombatEntity(CombatEntityData data)
        {
            var allEntities = Object.FindObjectsOfType<CombatEntity>();
            foreach (var entity in allEntities)
            {
                var entityData = entity.GetEntityData();
                if (entityData == data)
                {
                    return entity;
                }
            }
            return null;
        }
        
        private void SyncAllEntityPositions()
        {
            var allEntities = Object.FindObjectsOfType<CombatEntity>();
            foreach (var entity in allEntities)
            {
                var data = entity.GetEntityData();
                if (data != null)
                {
                    data.position = entity.transform.position;
                }
            }
        }
    }
}
