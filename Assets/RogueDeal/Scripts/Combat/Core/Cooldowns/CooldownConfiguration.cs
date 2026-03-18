using UnityEngine;

namespace RogueDeal.Combat.Core.Cooldowns
{
    /// <summary>
    /// Configuration for action cooldowns. Supports multiple cooldown types and charge systems.
    /// </summary>
    [System.Serializable]
    public class CooldownConfiguration
    {
        [Header("Cooldown Type")]
        [Tooltip("Type of cooldown (None, TurnBased, TimeBased, PerCombat, PerRest)")]
        public CooldownType cooldownType = CooldownType.None;
        
        [Header("Cooldown Duration")]
        [Tooltip("Cooldown duration in turns (for TurnBased)")]
        public int turnCooldown = 0;
        
        [Tooltip("Cooldown duration in seconds (for TimeBased)")]
        public float timeCooldown = 0f;
        
        [Header("Charge System")]
        [Tooltip("Does this action use charges?")]
        public bool usesCharges = false;
        
        [Tooltip("Maximum number of charges")]
        public int maxCharges = 1;
        
        [Tooltip("Starting number of charges")]
        public int startingCharges = 1;
        
        [Tooltip("Turns to recover one charge (only for TurnBased)")]
        public int chargeRecoveryTurns = 5;
        
        [Header("Global Cooldown")]
        [Tooltip("Does this action trigger a global cooldown on all actions?")]
        public bool triggersGlobalCooldown = false;
        
        [Tooltip("Global cooldown duration in turns")]
        public int globalCooldownTurns = 1;
        
        [Header("Resource Costs")]
        [Tooltip("Does this action cost resources?")]
        public bool hasResourceCost = false;
        
        [Tooltip("Mana cost")]
        public float manaCost = 0f;
        
        [Tooltip("Stamina cost")]
        public float staminaCost = 0f;
    }
    
    /// <summary>
    /// Type of cooldown
    /// </summary>
    public enum CooldownType
    {
        None,
        TurnBased,
        TimeBased,
        PerCombat,
        PerRest
    }
}

