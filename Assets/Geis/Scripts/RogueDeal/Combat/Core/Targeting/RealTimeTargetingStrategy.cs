using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat;

namespace RogueDeal.Combat.Core.Targeting
{
    /// <summary>
    /// Targeting strategy for real-time combat.
    /// Migrated from RealTimeTargeting class.
    /// </summary>
    [CreateAssetMenu(fileName = "Targeting_RealTime", menuName = "RogueDeal/Combat/Targeting/Real-Time Targeting")]
    public class RealTimeTargetingStrategy : TargetingStrategy
    {
        [Header("Targeting Settings")]
        [Tooltip("Layer mask for valid targets")]
        public LayerMask targetLayers;
        
        [Tooltip("Maximum range for targeting")]
        public float maxRange = 20f;
        
        [Tooltip("Target selection mode")]
        public RealTimeTargetMode targetMode = RealTimeTargetMode.NearestEnemy;

        public override TargetResult ResolveTargets(CombatEntityData attacker)
        {
            // Sync all entity positions first
            SyncAllEntityPositions();
            
            // Get attacker's CombatEntity
            CombatEntity attackerEntity = GetCombatEntityFromData(attacker);
            if (attackerEntity == null)
            {
                Debug.LogWarning("[RealTimeTargetingStrategy] Could not find CombatEntity for attacker");
                return new TargetResult();
            }
            
            List<CombatEntity> targets = new List<CombatEntity>();
            Vector3 targetPosition = attacker.position;
            
            switch (targetMode)
            {
                case RealTimeTargetMode.Self:
                    if (attackerEntity != null)
                    {
                        targets.Add(attackerEntity);
                        targetPosition = attackerEntity.transform.position;
                    }
                    break;
                    
                case RealTimeTargetMode.NearestEnemy:
                    CombatEntity nearest = FindNearestEnemy(attacker.position);
                    if (nearest != null)
                    {
                        targets.Add(nearest);
                        targetPosition = nearest.transform.position;
                    }
                    break;
                    
                case RealTimeTargetMode.Area:
                    targets = FindEnemiesInRadius(attacker.position, maxRange);
                    if (targets.Count > 0)
                    {
                        targetPosition = targets[0].transform.position; // Use first target's position
                    }
                    break;
                    
                case RealTimeTargetMode.AllEnemies:
                    targets = FindEnemiesInRadius(attacker.position, maxRange);
                    if (targets.Count > 0)
                    {
                        targetPosition = targets[0].transform.position;
                    }
                    break;
            }
            
            return new TargetResult(targets, targetPosition, targets.Count > 0);
        }

        private CombatEntity FindNearestEnemy(Vector3 position)
        {
            Collider[] hits = Physics.OverlapSphere(position, maxRange, targetLayers);
            CombatEntity nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<CombatEntity>(out var entity))
                {
                    var entityData = entity.GetEntityData();
                    if (entityData != null && entityData.IsAlive)
                    {
                        float distance = Vector3.Distance(position, hit.transform.position);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearest = entity;
                        }
                    }
                }
            }

            return nearest;
        }

        private List<CombatEntity> FindEnemiesInRadius(Vector3 position, float radius)
        {
            List<CombatEntity> entities = new List<CombatEntity>();
            Collider[] hits = Physics.OverlapSphere(position, radius, targetLayers);

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<CombatEntity>(out var entity))
                {
                    var entityData = entity.GetEntityData();
                    if (entityData != null && entityData.IsAlive)
                    {
                        entities.Add(entity);
                    }
                }
            }

            return entities;
        }

        // Helper methods inherited from TargetingStrategy base class
    }
    
    /// <summary>
    /// Target selection mode for real-time targeting
    /// </summary>
    public enum RealTimeTargetMode
    {
        Self,
        NearestEnemy,
        Area,
        AllEnemies
    }
}
