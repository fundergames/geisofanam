using UnityEngine;

namespace FunderGames.RPG
{
    /// <summary>
    /// Extended Combatant class specifically for characters that live inside cards
    /// </summary>
    public class CardCombatant : MonoBehaviour
    {
        [Header("Card References")]
        [SerializeField] private Transform cardTransform; // The card this character belongs to
        [SerializeField] private Vector3 originalLocalPosition; // Original position within the card
        [SerializeField] private Quaternion originalLocalRotation; // Original rotation within the card
        
        [Header("Card Exit Settings")]
        [SerializeField] private Vector3 exitPosition; // Position where character exits the card
        
        [Header("Stencil Settings")]
        [SerializeField] private int stencilID = 1; // Stencil ID for this character's card
        [SerializeField] private bool maintainStencilMasking = true; // Whether to maintain stencil masking when outside card
        [SerializeField] private Shader worldShader; // Shader to use when character is outside the card
        
        private Material[] originalMaterials; // Store original materials to restore them later
        private bool isOutsideCard = false; // Track if character is currently outside the card
        
        private void Awake()
        {
            // Store the card transform and original local position/rotation
            if (cardTransform == null)
            {
                cardTransform = transform.parent;
            }
            
            if (cardTransform != null)
            {
                originalLocalPosition = transform.localPosition;
                originalLocalRotation = transform.localRotation;
                
                // Try to get stencil ID from the card's materials
                var cardRenderer = cardTransform.GetComponent<Renderer>();
                if (cardRenderer != null && cardRenderer.material.HasProperty("_StencilID"))
                {
                    stencilID = (int)cardRenderer.material.GetFloat("_StencilID");
                }
            }
            
            // Store original materials
            StoreOriginalMaterials();
            
            // Load the world shader if not assigned
            if (worldShader == null)
            {
                worldShader = Shader.Find("Custom/CardCharacterWorldShader");
            }
        }
        
        /// <summary>
        /// Store the original materials for later restoration
        /// </summary>
        private void StoreOriginalMaterials()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length];
            
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
            }
        }
        
        /// <summary>
        /// Set the exit position for when the character jumps out of the card
        /// </summary>
        public void SetExitPosition(Vector3 position)
        {
            exitPosition = position;
        }
        
        /// <summary>
        /// Get the card transform this character belongs to
        /// </summary>
        public Transform GetCardTransform()
        {
            return cardTransform;
        }
        
        /// <summary>
        /// Get the original local position within the card
        /// </summary>
        public Vector3 GetOriginalLocalPosition()
        {
            return originalLocalPosition;
        }
        
        /// <summary>
        /// Get the original local rotation within the card
        /// </summary>
        public Quaternion GetOriginalLocalRotation()
        {
            return originalLocalRotation;
        }
        
        /// <summary>
        /// Get the stencil ID for this character's card
        /// </summary>
        public int GetStencilID()
        {
            return stencilID;
        }
        
        /// <summary>
        /// Set the card transform reference (useful for runtime setup)
        /// </summary>
        public void SetCardTransform(Transform card)
        {
            cardTransform = card;
            if (card != null)
            {
                originalLocalPosition = transform.localPosition;
                originalLocalRotation = transform.localRotation;
                
                // Update stencil ID from the new card
                var cardRenderer = card.GetComponent<Renderer>();
                if (cardRenderer != null && cardRenderer.material.HasProperty("_StencilID"))
                {
                    stencilID = (int)cardRenderer.material.GetFloat("_StencilID");
                }
            }
        }
        
        /// <summary>
        /// Apply stencil masking to this character's materials
        /// </summary>
        public void ApplyStencilMasking(int stencilID)
        {
            if (!maintainStencilMasking) return;
            
            isOutsideCard = true;
            var renderers = GetComponentsInChildren<Renderer>();
            
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                
                // Create a new material instance with the world shader
                if (worldShader != null)
                {
                    var newMaterial = new Material(worldShader);
                    newMaterial.SetFloat("_StencilID", stencilID);
                    newMaterial.SetFloat("_StencilMode", 1); // Equal comparison
                    
                    // Copy the main texture from the original material
                    if (originalMaterials != null && i < originalMaterials.Length && originalMaterials[i] != null)
                    {
                        if (originalMaterials[i].HasProperty("_MainTex"))
                        {
                            newMaterial.SetTexture("_MainTex", originalMaterials[i].GetTexture("_MainTex"));
                        }
                        if (originalMaterials[i].HasProperty("_Color"))
                        {
                            newMaterial.SetColor("_Color", originalMaterials[i].GetColor("_Color"));
                        }
                    }
                    
                    renderer.material = newMaterial;
                }
                else
                {
                    // Fallback: just set stencil ID on existing materials
                    foreach (var material in renderer.materials)
                    {
                        if (material.HasProperty("_StencilID"))
                        {
                            material.SetFloat("_StencilID", stencilID);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Remove stencil masking from this character's materials
        /// </summary>
        public void RemoveStencilMasking()
        {
            if (!maintainStencilMasking) return;
            
            isOutsideCard = false;
            var renderers = GetComponentsInChildren<Renderer>();
            
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                
                // Restore original material if available
                if (originalMaterials != null && i < originalMaterials.Length && originalMaterials[i] != null)
                {
                    renderer.material = originalMaterials[i];
                }
                else
                {
                    // Fallback: disable stencil masking
                    foreach (var material in renderer.materials)
                    {
                        if (material.HasProperty("_StencilID"))
                        {
                            material.SetFloat("_StencilID", 0);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if the character is currently outside the card
        /// </summary>
        public bool IsOutsideCard()
        {
            return isOutsideCard;
        }
    }
}
