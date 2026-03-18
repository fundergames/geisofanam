using UnityEngine;

namespace FunderGames.RPG
{
    /// <summary>
    /// Test script to verify stencil masking is working with the card combat system
    /// </summary>
    public class CardStencilTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool testOnStart = true;
        [SerializeField] private KeyCode testKey = KeyCode.T;
        [SerializeField] private CardCombatant testCharacter;
        
        [Header("Debug Info")]
        [SerializeField] private bool showDebugInfo = true;
        
        private void Start()
        {
            if (testOnStart)
            {
                TestStencilSystem();
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(testKey))
            {
                TestStencilSystem();
            }
        }
        
        /// <summary>
        /// Test the stencil masking system
        /// </summary>
        [ContextMenu("Test Stencil System")]
        public void TestStencilSystem()
        {
            Debug.Log("=== Testing Card Stencil System ===");
            
            // Find all CardCombatants
            var cardCombatants = FindObjectsByType<CardCombatant>(FindObjectsSortMode.None);
            Debug.Log($"Found {cardCombatants.Length} CardCombatants");
            
            foreach (var combatant in cardCombatants)
            {
                TestCardCombatant(combatant);
            }
            
            // Test card materials
            TestCardMaterials();
            
            Debug.Log("=== Stencil Test Complete ===");
        }
        
        /// <summary>
        /// Test a specific CardCombatant
        /// </summary>
        private void TestCardCombatant(CardCombatant combatant)
        {
            Debug.Log($"\n--- Testing {combatant.name} ---");
            
            // Check card transform
            var cardTransform = combatant.GetCardTransform();
            if (cardTransform != null)
            {
                Debug.Log($"✓ Card Transform: {cardTransform.name}");
                Debug.Log($"  Position: {cardTransform.position}");
                
                // Check card renderer
                var cardRenderer = cardTransform.GetComponent<Renderer>();
                if (cardRenderer != null)
                {
                    Debug.Log($"✓ Card Renderer found");
                    foreach (var material in cardRenderer.materials)
                    {
                        if (material.HasProperty("_StencilID"))
                        {
                            float cardMatStencilID = material.GetFloat("_StencilID");
                            Debug.Log($"  Material Stencil ID: {cardMatStencilID}");
                        }
                        else
                        {
                            Debug.Log($"  Material missing _StencilID property");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"✗ Card {cardTransform.name} has no Renderer component");
                }
            }
            else
            {
                Debug.LogWarning($"✗ {combatant.name} has no card transform");
            }
            
            // Check stencil ID
            int stencilID = combatant.GetStencilID();
            Debug.Log($"✓ Stencil ID: {stencilID}");
            
            // Check if outside card
            bool isOutside = combatant.IsOutsideCard();
            Debug.Log($"✓ Is Outside Card: {isOutside}");
            
            // Check character materials
            var characterRenderers = combatant.GetComponentsInChildren<Renderer>();
            Debug.Log($"✓ Character Renderers: {characterRenderers.Length}");
            
            foreach (var renderer in characterRenderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_StencilID"))
                    {
                        float matStencilID = material.GetFloat("_StencilID");
                        Debug.Log($"  {renderer.name} Stencil ID: {matStencilID}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Test card materials and stencil setup
        /// </summary>
        private void TestCardMaterials()
        {
            Debug.Log($"\n--- Testing Card Materials ---");
            
            // Find all objects with "Card" tag
            var cardObjects = GameObject.FindGameObjectsWithTag("Card");
            Debug.Log($"Found {cardObjects.Length} objects tagged as 'Card'");
            
            foreach (var card in cardObjects)
            {
                var renderer = card.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Debug.Log($"\nCard: {card.name}");
                    Debug.Log($"  Material: {renderer.material.name}");
                    Debug.Log($"  Shader: {renderer.material.shader.name}");
                    
                    if (renderer.material.HasProperty("_StencilID"))
                    {
                        float cardStencilID = renderer.material.GetFloat("_StencilID");
                        Debug.Log($"  Stencil ID: {cardStencilID}");
                    }
                    else
                    {
                        Debug.LogWarning($"  ✗ Missing _StencilID property");
                    }
                }
            }
            
            // Find all objects with CardWindowObjectShader
            var windowShaders = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int windowCount = 0;
            
            foreach (var renderer in windowShaders)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.shader.name.Contains("TintedWindowCardShader"))
                    {
                        windowCount++;
                        Debug.Log($"\nWindow Shader found on: {renderer.name}");
                        if (material.HasProperty("_StencilID"))
                        {
                            float windowStencilID = material.GetFloat("_StencilID");
                            Debug.Log($"  Window Stencil ID: {windowStencilID}");
                        }
                    }
                }
            }
            
            Debug.Log($"\nTotal Window Shaders found: {windowCount}");
        }
        
        /// <summary>
        /// Force a stencil masking test
        /// </summary>
        [ContextMenu("Force Stencil Test")]
        public void ForceStencilTest()
        {
            if (testCharacter != null)
            {
                Debug.Log($"Forcing stencil test on {testCharacter.name}");
                
                // Test applying stencil masking
                int stencilID = testCharacter.GetStencilID();
                testCharacter.ApplyStencilMasking(stencilID);
                Debug.Log($"Applied stencil masking with ID: {stencilID}");
                
                // Wait a bit then remove it
                StartCoroutine(RemoveStencilAfterDelay());
            }
            else
            {
                Debug.LogWarning("No test character assigned!");
            }
        }
        
        private System.Collections.IEnumerator RemoveStencilAfterDelay()
        {
            yield return new WaitForSeconds(2f);
            testCharacter.RemoveStencilMasking();
            Debug.Log("Removed stencil masking");
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Card Stencil Test");
            
            if (GUILayout.Button("Test Stencil System"))
            {
                TestStencilSystem();
            }
            
            if (testCharacter != null)
            {
                GUILayout.Label($"Test Character: {testCharacter.name}");
                if (GUILayout.Button("Force Stencil Test"))
                {
                    ForceStencilTest();
                }
            }
            
            GUILayout.EndArea();
        }
    }
}
