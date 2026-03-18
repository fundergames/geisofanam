using UnityEngine;

namespace FunderGames.RPG
{
    /// <summary>
    /// Utility script to set up card materials and fix shader issues
    /// </summary>
    public class CardMaterialSetup : MonoBehaviour
    {
        [Header("Material Setup")]
        [SerializeField] private Material[] cardMaterials;
        [SerializeField] private bool setupOnStart = true;
        
        [Header("Shader Settings")]
        [SerializeField] private Shader windowShader;
        [SerializeField] private Shader simpleWindowShader;
        [SerializeField] private Color defaultWindowColor = new Color(0.2f, 0.3f, 0.8f, 0.8f);
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        private void Start()
        {
            if (setupOnStart)
            {
                SetupCardMaterials();
            }
        }
        
        /// <summary>
        /// Set up card materials with proper shaders and properties
        /// </summary>
        [ContextMenu("Setup Card Materials")]
        public void SetupCardMaterials()
        {
            Debug.Log("=== Setting up Card Materials ===");
            
            // Load shaders if not assigned
            if (windowShader == null)
            {
                windowShader = Shader.Find("Custom/TintedWindowCardShader");
            }
            
            if (simpleWindowShader == null)
            {
                simpleWindowShader = Shader.Find("Custom/SimpleWindowShader");
            }
            
            // Find all objects with "Card" tag
            var cardObjects = GameObject.FindGameObjectsWithTag("Card");
            Debug.Log($"Found {cardObjects.Length} objects tagged as 'Card'");
            
            foreach (var card in cardObjects)
            {
                SetupCardMaterial(card);
            }
            
            // Also set up manually assigned materials
            if (cardMaterials != null)
            {
                foreach (var material in cardMaterials)
                {
                    if (material != null)
                    {
                        SetupMaterial(material, "Manual Material");
                    }
                }
            }
            
            Debug.Log("=== Card Material Setup Complete ===");
        }
        
        /// <summary>
        /// Set up a specific card's material
        /// </summary>
        private void SetupCardMaterial(GameObject card)
        {
            var renderer = card.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogWarning($"Card {card.name} has no Renderer component");
                return;
            }
            
            Debug.Log($"\n--- Setting up {card.name} ---");
            
            foreach (var material in renderer.materials)
            {
                SetupMaterial(material, card.name);
            }
        }
        
        /// <summary>
        /// Set up a specific material with proper shader and properties
        /// </summary>
        private void SetupMaterial(Material material, string objectName)
        {
            Debug.Log($"Setting up material: {material.name} on {objectName}");
            
            // Check if the material is using the window shader
            if (material.shader.name.Contains("TintedWindowCardShader"))
            {
                Debug.Log("  ✓ Using TintedWindowCardShader");
                
                // Set default properties to prevent magenta
                if (material.HasProperty("_WindowColor"))
                {
                    material.SetColor("_WindowColor", defaultWindowColor);
                    Debug.Log($"  Set _WindowColor to {defaultWindowColor}");
                }
                
                if (material.HasProperty("_StencilID"))
                {
                    // Try to get a unique stencil ID based on the object name
                    int stencilID = GetStencilIDFromName(objectName);
                    material.SetFloat("_StencilID", stencilID);
                    Debug.Log($"  Set _StencilID to {stencilID}");
                }
                
                if (material.HasProperty("_UseTexture"))
                {
                    // Check if the material has a texture
                    if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
                    {
                        material.SetFloat("_UseTexture", 1.0f);
                        Debug.Log("  ✓ Texture found, enabling texture mode");
                    }
                    else
                    {
                        material.SetFloat("_UseTexture", 0.0f);
                        Debug.Log("  ✗ No texture found, disabling texture mode");
                    }
                }
            }
            else if (material.shader.name.Contains("SimpleWindowShader"))
            {
                Debug.Log("  ✓ Using SimpleWindowShader");
                
                if (material.HasProperty("_WindowColor"))
                {
                    material.SetColor("_WindowColor", defaultWindowColor);
                    Debug.Log($"  Set _WindowColor to {defaultWindowColor}");
                }
                
                if (material.HasProperty("_StencilID"))
                {
                    int stencilID = GetStencilIDFromName(objectName);
                    material.SetFloat("_StencilID", stencilID);
                    Debug.Log($"  Set _StencilID to {stencilID}");
                }
            }
            else
            {
                Debug.LogWarning($"  ✗ Material {material.name} is not using a window shader");
                Debug.LogWarning($"    Current shader: {material.shader.name}");
                
                // Offer to convert to window shader
                if (GUILayout.Button($"Convert {material.name} to Window Shader"))
                {
                    ConvertToWindowShader(material);
                }
            }
        }
        
        /// <summary>
        /// Convert a material to use the window shader
        /// </summary>
        private void ConvertToWindowShader(Material material)
        {
            if (simpleWindowShader != null)
            {
                material.shader = simpleWindowShader;
                material.SetColor("_WindowColor", defaultWindowColor);
                material.SetFloat("_StencilID", 1);
                Debug.Log($"Converted {material.name} to SimpleWindowShader");
            }
            else
            {
                Debug.LogError("SimpleWindowShader not found!");
            }
        }
        
        /// <summary>
        /// Generate a stencil ID from an object name
        /// </summary>
        private int GetStencilIDFromName(string objectName)
        {
            // Simple hash function to generate unique stencil IDs
            int hash = 0;
            foreach (char c in objectName)
            {
                hash = ((hash << 5) - hash) + c;
                hash = hash & hash; // Convert to 32-bit integer
            }
            
            // Ensure the ID is between 1 and 255 (valid stencil range)
            return Mathf.Abs(hash % 254) + 1;
        }
        
        /// <summary>
        /// Create a new card material with the simple window shader
        /// </summary>
        [ContextMenu("Create New Card Material")]
        public void CreateNewCardMaterial()
        {
            if (simpleWindowShader == null)
            {
                simpleWindowShader = Shader.Find("Custom/SimpleWindowShader");
                if (simpleWindowShader == null)
                {
                    Debug.LogError("SimpleWindowShader not found! Make sure the shader is compiled.");
                    return;
                }
            }
            
            Material newMaterial = new Material(simpleWindowShader);
            newMaterial.name = "NewCardMaterial";
            newMaterial.SetColor("_WindowColor", defaultWindowColor);
            newMaterial.SetFloat("_StencilID", 1);
            newMaterial.SetFloat("_EdgeSoftness", 0.1f);
            
            Debug.Log("Created new card material: " + newMaterial.name);
            
            // Save the material to the project
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.CreateAsset(newMaterial, "Assets/FunderGames/RPG/Assets/Materials/" + newMaterial.name + ".mat");
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("Material saved to project!");
            #endif
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 220, 300, 200));
            GUILayout.Label("Card Material Setup");
            
            if (GUILayout.Button("Setup Card Materials"))
            {
                SetupCardMaterials();
            }
            
            if (GUILayout.Button("Create New Card Material"))
            {
                CreateNewCardMaterial();
            }
            
            GUILayout.Label("Default Window Color:");
            defaultWindowColor = UnityEditor.EditorGUILayout.ColorField(defaultWindowColor);
            
            GUILayout.EndArea();
        }
    }
}
