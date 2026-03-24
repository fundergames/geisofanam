using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat;

namespace RogueDeal.Combat.Core.Targeting
{
    /// <summary>
    /// Targets multiple enemies (for AOE/multi-hit abilities)
    /// </summary>
    [CreateAssetMenu(fileName = "Targeting_MultiTarget", menuName = "RogueDeal/Combat/Targeting/Multi Target Selector")]
    public class MultiTargetSelector : TargetingStrategy
    {
        [Header("Targeting Settings")]
        [Tooltip("Layer mask for valid targets")]
        public LayerMask targetLayers;
        
        [Tooltip("Maximum range for targeting")]
        public float maxRange = 20f;
        
        [Tooltip("Maximum number of targets")]
        public int maxTargets = 5;
        
        public override TargetResult ResolveTargets(CombatEntityData attacker)
        {
            Collider[] colliders = Physics.OverlapSphere(
                attacker.position,
                maxRange,
                targetLayers
            );
            
            var targets = new List<CombatEntity>();
            
            foreach (var collider in colliders)
            {
                if (targets.Count >= maxTargets) break;
                
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
            
            if (targets.Count > 0)
            {
                // Use center of targets as target position
                Vector3 center = Vector3.zero;
                foreach (var target in targets)
                {
                    var targetData = target.GetEntityData();
                    if (targetData != null)
                    {
                        center += targetData.position;
                    }
                }
                center /= targets.Count;
                
                return new TargetResult(targets, center, true);
            }
            
            return new TargetResult(null, attacker.position, false);
        }
    }
}

