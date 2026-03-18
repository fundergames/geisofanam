using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Combat.Core.Targeting
{
    /// <summary>
    /// Base class for targeting strategies. Allows swappable targeting for different game modes.
    /// </summary>
    public abstract class TargetingStrategy : ScriptableObject
    {
        [Header("Targeting Info")]
        public string strategyName;
        [TextArea(2, 4)]
        public string description;
        
        /// <summary>
        /// Resolves targets for an attacker. Returns targets and target position.
        /// </summary>
        public abstract TargetResult ResolveTargets(CombatEntityData attacker);
        
        /// <summary>
        /// Shows targeting UI (reticle, indicator, etc.)
        /// </summary>
        public virtual void ShowTargetingUI(MonoBehaviour attacker)
        {
            // Override in subclasses if needed
        }
        
        /// <summary>
        /// Hides targeting UI
        /// </summary>
        public virtual void HideTargetingUI()
        {
            // Override in subclasses if needed
        }
        
        /// <summary>
        /// Helper method to sync all entity positions from transforms
        /// </summary>
        protected void SyncAllEntityPositions()
        {
            var allEntities = Object.FindObjectsByType<CombatEntity>(FindObjectsSortMode.None);
            foreach (var entity in allEntities)
            {
                var data = entity.GetEntityData();
                if (data != null)
                {
                    data.position = entity.transform.position;
                }
            }
        }
        
        /// <summary>
        /// Helper method to get CombatEntity from CombatEntityData
        /// </summary>
        protected CombatEntity GetCombatEntityFromData(CombatEntityData data)
        {
            var allEntities = Object.FindObjectsByType<CombatEntity>(FindObjectsSortMode.None);
            foreach (var entity in allEntities)
            {
                if (entity.GetEntityData() == data)
                {
                    return entity;
                }
            }
            return null;
        }
    }
    
    /// <summary>
    /// Result of target resolution
    /// </summary>
    public class TargetResult
    {
        public List<CombatEntity> targets = new List<CombatEntity>();
        public Vector3 targetPosition;
        public bool isReady;
        
        public TargetResult()
        {
            isReady = false;
        }
        
        public TargetResult(List<CombatEntity> targets, Vector3 targetPosition, bool isReady)
        {
            this.targets = targets ?? new List<CombatEntity>();
            this.targetPosition = targetPosition;
            this.isReady = isReady;
        }
    }
}

