using RogueDeal.Combat.Cards;
using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Maps poker hand types to combat actions.
    /// Migrated from AbilityData to CombatAction.
    /// </summary>
    [CreateAssetMenu(fileName = "AbilityLookup", menuName = "RogueDeal/Combat/Ability Lookup")]
    public class AbilityLookup : ScriptableObject
    {
        [System.Serializable]
        public class HandAbilityMapping
        {
            public PokerHandType handType;
            public CombatAction action; // Changed from AbilityData to CombatAction
        }

        [Header("Hand to Action Mappings")]
        [SerializeField] private HandAbilityMapping[] mappings = new HandAbilityMapping[10];

        public CombatAction GetAction(PokerHandType? handType)
        {
            if (!handType.HasValue)
            {
                Debug.LogWarning("GetAction called with null hand type");
                return null;
            }

            foreach (var mapping in mappings)
            {
                if (mapping != null && mapping.handType == handType.Value)
                {
                    return mapping.action;
                }
            }

            Debug.LogWarning($"No action mapping found for hand type: {handType.Value}");
            return null;
        }

        /// <summary>
        /// Legacy method name for backward compatibility
        /// </summary>
        [System.Obsolete("Use GetAction instead")]
        public AbilityData GetAbility(PokerHandType? handType)
        {
            Debug.LogWarning("GetAbility is deprecated. Use GetAction instead. Returning null.");
            return null;
        }

        public bool HasAction(PokerHandType handType)
        {
            foreach (var mapping in mappings)
            {
                if (mapping != null && mapping.handType == handType && mapping.action != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Legacy method name for backward compatibility
        /// </summary>
        [System.Obsolete("Use HasAction instead")]
        public bool HasAbility(PokerHandType handType)
        {
            return HasAction(handType);
        }

        [ContextMenu("Validate Mappings")]
        private void ValidateMappings()
        {
            Debug.Log("=== Ability Lookup Validation ===");
            
            if (mappings == null || mappings.Length == 0)
            {
                Debug.LogWarning("No mappings defined!");
                return;
            }

            int validCount = 0;
            foreach (var mapping in mappings)
            {
                if (mapping != null && mapping.action != null)
                {
                    Debug.Log($"✓ {mapping.handType} → {mapping.action.actionName}");
                    validCount++;
                }
                else if (mapping != null)
                {
                    Debug.LogWarning($"✗ {mapping.handType} → MISSING ACTION");
                }
            }

            Debug.Log($"Valid mappings: {validCount}/{mappings.Length}");
        }
    }
}
