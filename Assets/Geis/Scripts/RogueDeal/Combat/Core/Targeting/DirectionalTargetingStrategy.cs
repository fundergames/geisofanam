using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat;

namespace RogueDeal.Combat.Core.Targeting
{
    /// <summary>
    /// Directional targeting - attacks in the direction the character is facing.
    /// No target selection needed - uses weapon colliders for hit detection.
    /// </summary>
    [CreateAssetMenu(fileName = "Targeting_Directional", menuName = "RogueDeal/Combat/Targeting/Directional")]
    public class DirectionalTargetingStrategy : TargetingStrategy
    {
        public override TargetResult ResolveTargets(CombatEntityData attacker)
        {
            // Directional targeting doesn't select targets
            // Weapon colliders will handle hit detection
            // Return empty result - the attack will happen in facing direction
            return new TargetResult(new List<CombatEntity>(), attacker.position, true);
        }
        
        public override void ShowTargetingUI(MonoBehaviour attacker)
        {
            // No UI needed for directional targeting
        }
        
        public override void HideTargetingUI()
        {
            // No UI needed for directional targeting
        }
    }
}
