using UnityEngine;
using System.Collections.Generic;

namespace FunderGames.RPG
{
    /// <summary>
    /// Utility script to set up card-based combat system
    /// </summary>
    public class CardCombatSetup : MonoBehaviour
    {
        [Header("Card Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private Transform[] cardTransforms; // Array of card transforms
        [SerializeField] private Combatant[] combatants; // Array of combatants to set up
        
        [Header("Card Settings")]
        [SerializeField] private float cardSpacing = 3f; // Spacing between cards
        [SerializeField] private Vector3 cardOffset = Vector3.zero; // Offset for card positioning
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupCardCombatSystem();
            }
        }
        
        /// <summary>
        /// Set up the card combat system by adding CardCombatant components and setting references
        /// </summary>
        [ContextMenu("Setup Card Combat System")]
        public void SetupCardCombatSystem()
        {
            // Find all combatants if not manually assigned
            if (combatants == null || combatants.Length == 0)
            {
                combatants = FindObjectsByType<Combatant>(FindObjectsSortMode.None);
            }
            
            // Find all card transforms if not manually assigned
            if (cardTransforms == null || cardTransforms.Length == 0)
            {
                // Look for objects with "Card" in their name or with specific tags
                var cardObjects = GameObject.FindGameObjectsWithTag("Card");
                if (cardObjects.Length > 0)
                {
                    cardTransforms = new Transform[cardObjects.Length];
                    for (int i = 0; i < cardObjects.Length; i++)
                    {
                        cardTransforms[i] = cardObjects[i].transform;
                    }
                }
                else
                {
                    // Create default card transforms
                    CreateDefaultCardTransforms();
                }
            }
            
            // Set up each combatant with their card
            for (int i = 0; i < combatants.Length && i < cardTransforms.Length; i++)
            {
                SetupCombatantForCard(combatants[i], cardTransforms[i], i);
            }
            
            Debug.Log($"Card combat system setup complete. {combatants.Length} combatants assigned to {cardTransforms.Length} cards.");
        }
        
        /// <summary>
        /// Set up a specific combatant for card-based combat
        /// </summary>
        private void SetupCombatantForCard(Combatant combatant, Transform cardTransform, int index)
        {
            if (combatant == null || cardTransform == null) return;
            
            // Add CardCombatant component if it doesn't exist
            var cardCombatant = combatant.GetComponent<CardCombatant>();
            if (cardCombatant == null)
            {
                cardCombatant = combatant.gameObject.AddComponent<CardCombatant>();
            }
            
            // Set the card transform reference
            cardCombatant.SetCardTransform(cardTransform);
            
            // Position the combatant within the card
            combatant.transform.SetParent(cardTransform);
            combatant.transform.localPosition = Vector3.zero;
            combatant.transform.localRotation = Quaternion.identity;
            
            // Set the card's position in the world
            Vector3 cardPosition = Vector3.right * (index * cardSpacing) + cardOffset;
            cardTransform.position = cardPosition;
            
            Debug.Log($"Set up {combatant.name} for card {index} at position {cardPosition}");
        }
        
        /// <summary>
        /// Create default card transforms if none exist
        /// </summary>
        private void CreateDefaultCardTransforms()
        {
            int numCards = Mathf.Max(combatants.Length, 4); // At least 4 cards
            cardTransforms = new Transform[numCards];
            
            GameObject cardContainer = new GameObject("CardContainer");
            cardContainer.transform.SetParent(transform);
            
            for (int i = 0; i < numCards; i++)
            {
                GameObject card = new GameObject($"Card_{i}");
                card.transform.SetParent(cardContainer.transform);
                card.transform.position = Vector3.right * (i * cardSpacing) + cardOffset;
                card.tag = "Card";
                
                cardTransforms[i] = card.transform;
            }
            
            Debug.Log($"Created {numCards} default card transforms");
        }
        
        /// <summary>
        /// Clear all card combat setup
        /// </summary>
        [ContextMenu("Clear Card Combat Setup")]
        public void ClearCardCombatSetup()
        {
            if (combatants != null)
            {
                foreach (var combatant in combatants)
                {
                    if (combatant != null)
                    {
                        var cardCombatant = combatant.GetComponent<CardCombatant>();
                        if (cardCombatant != null)
                        {
                            DestroyImmediate(cardCombatant);
                        }
                        
                        // Reset parent to scene root
                        combatant.transform.SetParent(null);
                    }
                }
            }
            
            Debug.Log("Card combat setup cleared");
        }
    }
}
