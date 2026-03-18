using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat;

namespace RogueDeal.Combat.Core.Targeting
{
    /// <summary>
    /// Click-to-select targeting with lock-on. Player clicks to select/deselect targets.
    /// Lock-on persists until target dies, goes out of range, or player clicks elsewhere.
    /// </summary>
    [CreateAssetMenu(fileName = "Targeting_ClickToSelect", menuName = "RogueDeal/Combat/Targeting/Click To Select")]
    public class ClickToSelectTargetingStrategy : TargetingStrategy
    {
        [Header("Targeting Settings")]
        [Tooltip("Layer mask for valid targets")]
        public LayerMask targetLayers;
        
        [Tooltip("Default range if weapon doesn't specify one")]
        public float defaultRange = 2f;
        
        // Lock-on is managed by TargetingManager, not here
        // This strategy just validates that a target exists
        
        public override TargetResult ResolveTargets(CombatEntityData attacker)
        {
            // This strategy relies on TargetingManager to handle lock-on
            // It will be called after lock-on is established
            // For now, return empty - TargetingManager will handle the actual target resolution
            return new TargetResult(null, attacker.position, false);
        }
        
        public override void ShowTargetingUI(MonoBehaviour attacker)
        {
            // Show targeting reticle/cursor
            // TargetingManager will handle the visual indicator
        }
        
        public override void HideTargetingUI()
        {
            // Hide targeting reticle/cursor
        }
    }
}
