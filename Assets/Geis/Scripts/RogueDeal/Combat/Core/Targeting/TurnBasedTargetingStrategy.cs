using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat;

namespace RogueDeal.Combat.Core.Targeting
{
    /// <summary>
    /// Targeting strategy for turn-based combat.
    /// Migrated from TurnBasedTargeting class.
    /// Supports pre-selected targets (from UI selection).
    /// </summary>
    [CreateAssetMenu(fileName = "Targeting_TurnBased", menuName = "RogueDeal/Combat/Targeting/Turn-Based Targeting")]
    public class TurnBasedTargetingStrategy : TargetingStrategy
    {
        [Header("Targeting Settings")]
        [Tooltip("Target selection mode")]
        public TurnBasedTargetMode targetMode = TurnBasedTargetMode.SelectedEnemy;
        
        // Runtime selected target (set by UI or code)
        private CombatEntity selectedTarget;
        
        /// <summary>
        /// Sets the selected target (called by UI or combat system)
        /// </summary>
        public void SetSelectedTarget(CombatEntity target)
        {
            selectedTarget = target;
        }

        public override TargetResult ResolveTargets(CombatEntityData attacker)
        {
            // Sync all entity positions first
            SyncAllEntityPositions();
            
            // Get attacker's CombatEntity
            CombatEntity attackerEntity = GetCombatEntityFromData(attacker);
            if (attackerEntity == null)
            {
                Debug.LogWarning("[TurnBasedTargetingStrategy] Could not find CombatEntity for attacker");
                return new TargetResult();
            }
            
            List<CombatEntity> targets = new List<CombatEntity>();
            Vector3 targetPosition = attacker.position;
            
            switch (targetMode)
            {
                case TurnBasedTargetMode.Self:
                    if (attackerEntity != null)
                    {
                        targets.Add(attackerEntity);
                        targetPosition = attackerEntity.transform.position;
                    }
                    break;
                    
                case TurnBasedTargetMode.SelectedEnemy:
                    if (selectedTarget != null)
                    {
                        var targetData = selectedTarget.GetEntityData();
                        if (targetData != null && targetData.IsAlive)
                        {
                            targets.Add(selectedTarget);
                            targetPosition = selectedTarget.transform.position;
                        }
                    }
                    break;
                    
                case TurnBasedTargetMode.SelectedAlly:
                    if (selectedTarget != null)
                    {
                        var targetData = selectedTarget.GetEntityData();
                        if (targetData != null && targetData.IsAlive)
                        {
                            targets.Add(selectedTarget);
                            targetPosition = selectedTarget.transform.position;
                        }
                    }
                    break;
            }
            
            return new TargetResult(targets, targetPosition, targets.Count > 0);
        }

        // Helper methods inherited from TargetingStrategy base class
    }
    
    /// <summary>
    /// Target selection mode for turn-based targeting
    /// </summary>
    public enum TurnBasedTargetMode
    {
        Self,
        SelectedEnemy,
        SelectedAlly
    }
}
