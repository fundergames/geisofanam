using System.Collections.Generic;
using System.Linq;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Combat.Core.Cooldowns
{
    /// <summary>
    /// Manages cooldowns for all actions on an entity. Handles turn-based, time-based, charges, and global cooldowns.
    /// </summary>
    public class ActionCooldownManager
    {
        private CombatEntityData entityData;
        private Dictionary<string, CooldownState> cooldowns = new Dictionary<string, CooldownState>();
        private float globalCooldownRemaining = 0f;
        private int globalCooldownTurnsRemaining = 0;
        
        public ActionCooldownManager(CombatEntityData entityData)
        {
            this.entityData = entityData;
        }
        
        /// <summary>
        /// Checks if an action is available (off cooldown, has charges, has resources)
        /// </summary>
        public bool IsActionAvailable(CombatAction action)
        {
            if (action == null || action.cooldownConfig == null) return true;
            
            string actionId = GetActionId(action);
            
            // Check global cooldown
            if (globalCooldownTurnsRemaining > 0 || globalCooldownRemaining > 0)
            {
                return false;
            }
            
            // Check action-specific cooldown
            if (!cooldowns.TryGetValue(actionId, out var state))
            {
                return true; // No cooldown state = available
            }
            
            // Check if on cooldown
            if (state.turnsRemaining > 0 || state.timeRemaining > 0)
            {
                return false;
            }
            
            // Check charges
            if (action.cooldownConfig.usesCharges && state.currentCharges <= 0)
            {
                return false;
            }
            
            // Check resource costs
            if (action.cooldownConfig.hasResourceCost)
            {
                // TODO: Check mana/stamina availability
                // For now, assume resources are available
            }
            
            return true;
        }
        
        /// <summary>
        /// Starts cooldown for an action
        /// </summary>
        public void StartCooldown(CombatAction action)
        {
            if (action == null || action.cooldownConfig == null) return;
            
            string actionId = GetActionId(action);
            var config = action.cooldownConfig;
            
            // Get or create cooldown state
            if (!cooldowns.TryGetValue(actionId, out var state))
            {
                state = new CooldownState
                {
                    maxCharges = config.maxCharges,
                    currentCharges = config.startingCharges
                };
                cooldowns[actionId] = state;
            }
            
            // Consume charge if using charges
            if (config.usesCharges)
            {
                state.currentCharges = System.Math.Max(0, state.currentCharges - 1);
            }
            
            // Set cooldown based on type
            switch (config.cooldownType)
            {
                case CooldownType.TurnBased:
                    state.turnsRemaining = config.turnCooldown;
                    break;
                case CooldownType.TimeBased:
                    state.timeRemaining = config.timeCooldown;
                    break;
                case CooldownType.PerCombat:
                    state.isPerCombat = true;
                    break;
                case CooldownType.PerRest:
                    state.isPerRest = true;
                    break;
            }
            
            // Apply global cooldown if triggered
            if (config.triggersGlobalCooldown)
            {
                globalCooldownTurnsRemaining = config.globalCooldownTurns;
                globalCooldownRemaining = 0f; // Reset time-based GCD
            }
        }
        
        /// <summary>
        /// Called at the start of each turn
        /// </summary>
        public void OnTurnStart()
        {
            // Advance turn-based cooldowns
            foreach (var state in cooldowns.Values)
            {
                if (state.turnsRemaining > 0)
                {
                    state.turnsRemaining--;
                }
                
                // Recover charges
                if (state.chargeRecoveryTurns > 0)
                {
                    state.chargeRecoveryProgress++;
                    if (state.chargeRecoveryProgress >= state.chargeRecoveryTurns)
                    {
                        state.currentCharges = System.Math.Min(state.maxCharges, state.currentCharges + 1);
                        state.chargeRecoveryProgress = 0;
                    }
                }
            }
            
            // Advance global cooldown
            if (globalCooldownTurnsRemaining > 0)
            {
                globalCooldownTurnsRemaining--;
            }
        }
        
        /// <summary>
        /// Called each frame (for time-based cooldowns)
        /// </summary>
        public void Update(float deltaTime)
        {
            // Advance time-based cooldowns
            foreach (var state in cooldowns.Values)
            {
                if (state.timeRemaining > 0)
                {
                    state.timeRemaining = System.Math.Max(0, state.timeRemaining - deltaTime);
                }
            }
            
            // Advance global cooldown
            if (globalCooldownRemaining > 0)
            {
                globalCooldownRemaining = System.Math.Max(0, globalCooldownRemaining - deltaTime);
            }
        }
        
        /// <summary>
        /// Called when combat ends
        /// </summary>
        public void OnCombatEnd()
        {
            // Reset per-combat cooldowns
            var perCombatCooldowns = cooldowns.Where(kvp => kvp.Value.isPerCombat).ToList();
            foreach (var kvp in perCombatCooldowns)
            {
                cooldowns.Remove(kvp.Key);
            }
        }
        
        /// <summary>
        /// Called when player rests
        /// </summary>
        public void OnRest()
        {
            // Reset per-rest cooldowns
            var perRestCooldowns = cooldowns.Where(kvp => kvp.Value.isPerRest).ToList();
            foreach (var kvp in perRestCooldowns)
            {
                cooldowns.Remove(kvp.Key);
            }
        }
        
        /// <summary>
        /// Gets remaining cooldown for an action
        /// </summary>
        public float GetCooldownRemaining(CombatAction action)
        {
            if (action == null) return 0f;
            
            string actionId = GetActionId(action);
            if (!cooldowns.TryGetValue(actionId, out var state))
            {
                return 0f;
            }
            
            // Return turns or time, whichever is applicable
            if (state.turnsRemaining > 0)
            {
                return state.turnsRemaining;
            }
            
            return state.timeRemaining;
        }
        
        private string GetActionId(CombatAction action)
        {
            return action.name; // Use asset name as ID
        }
    }
    
    /// <summary>
    /// State of a cooldown for an action
    /// </summary>
    [System.Serializable]
    public class CooldownState
    {
        public int turnsRemaining = 0;
        public float timeRemaining = 0f;
        public bool isPerCombat = false;
        public bool isPerRest = false;
        
        // Charge system
        public int maxCharges = 1;
        public int currentCharges = 1;
        public int chargeRecoveryTurns = 5;
        public int chargeRecoveryProgress = 0;
    }
}

