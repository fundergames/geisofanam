using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat;

namespace RogueDeal.Combat.Core.Targeting
{
    /// <summary>
    /// Ground-targeted AOE. Player places reticle (for meteor shower, etc.)
    /// In video poker mode: auto-targets enemy position
    /// In free-flow mode: waits for player mouse click
    /// </summary>
    [CreateAssetMenu(fileName = "Targeting_GroundAOE", menuName = "RogueDeal/Combat/Targeting/Ground Targeted AOE")]
    public class GroundTargetedAOE : TargetingStrategy
    {
        [Header("Targeting Settings")]
        [Tooltip("Radius of the AOE")]
        public float radius = 5f;
        
        [Tooltip("Layer mask for ground/terrain")]
        public LayerMask groundLayers;
        
        [Tooltip("Layer mask for valid targets")]
        public LayerMask targetLayers;
        
        [Header("Video Poker Mode")]
        [Tooltip("In video poker mode, auto-target current enemy position")]
        public bool autoTargetInVideoPokerMode = true;
        
        // For free-flow mode: store pending target position
        private Vector3? pendingTargetPosition = null;
#pragma warning disable CS0414
        private bool isWaitingForInput = false;
#pragma warning restore CS0414
        
        public override TargetResult ResolveTargets(CombatEntityData attacker)
        {
            // In video poker mode, auto-target enemy
            if (autoTargetInVideoPokerMode)
            {
                // Find nearest enemy and target their position
                Collider[] enemies = Physics.OverlapSphere(attacker.position, 50f, targetLayers);
                
                if (enemies.Length > 0)
                {
                    // Find nearest
                    CombatEntity nearest = null;
                    float nearestDistance = float.MaxValue;
                    
                    foreach (var collider in enemies)
                    {
                        var entity = collider.GetComponent<CombatEntity>();
                        if (entity != null)
                        {
                            var entityData = entity.GetEntityData();
                            if (entityData != null && entityData.IsAlive)
                            {
                                float distance = Vector3.Distance(attacker.position, entityData.position);
                                if (distance < nearestDistance)
                                {
                                    nearestDistance = distance;
                                    nearest = entity;
                                }
                            }
                        }
                    }
                    
                    if (nearest != null)
                    {
                        var nearestData = nearest.GetEntityData();
                        Vector3 targetPos = nearestData != null ? nearestData.position : attacker.position;
                        // Find all entities in radius at target position
                        var targets = FindTargetsInRadius(targetPos);
                        return new TargetResult(targets, targetPos, true);
                    }
                }
            }
            
            // Free-flow mode: check if we have a pending target
            if (pendingTargetPosition.HasValue)
            {
                Vector3 targetPos = pendingTargetPosition.Value;
                var targets = FindTargetsInRadius(targetPos);
                pendingTargetPosition = null;
                isWaitingForInput = false;
                return new TargetResult(targets, targetPos, true);
            }
            
            // Need to wait for input
            isWaitingForInput = true;
            return new TargetResult(null, attacker.position, false);
        }
        
        public override void ShowTargetingUI(MonoBehaviour attacker)
        {
            // Show targeting reticle
            // TODO: Implement UI display
        }
        
        public override void HideTargetingUI()
        {
            // Hide targeting reticle
            // TODO: Implement UI hiding
        }
        
        /// <summary>
        /// Sets the target position (called from input system in free-flow mode)
        /// </summary>
        public void SetTargetPosition(Vector3 position)
        {
            pendingTargetPosition = position;
        }
        
        private List<CombatEntity> FindTargetsInRadius(Vector3 center)
        {
            var targets = new List<CombatEntity>();
            Collider[] colliders = Physics.OverlapSphere(center, radius, targetLayers);
            
            foreach (var collider in colliders)
            {
                var entity = collider.GetComponent<CombatEntity>();
                if (entity != null)
                {
                    var entityData = entity.GetEntityData();
                    if (entityData != null && entityData.IsAlive)
                    {
                        targets.Add(entity);
                    }
                }
            }
            
            return targets;
        }
    }
}

