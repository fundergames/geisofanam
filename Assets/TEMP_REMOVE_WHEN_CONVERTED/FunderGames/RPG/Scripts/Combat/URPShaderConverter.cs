using UnityEngine;

namespace FunderGames.RPG
{
    /// <summary>
    /// Utility script to convert materials from Built-in Pipeline to URP shaders
    /// </summary>
    public class URPShaderConverter : MonoBehaviour
    {
        [Header("URP Shader References")]
        [SerializeField] private Shader urpTintedWindowShader;
        [SerializeField] private Shader urpSimpleWindowShader;
        [SerializeField] private Shader urpBasicCardShader;
        [SerializeField] private Shader urpCardCharacterWorldShader;
        
        [Header("Conversion Settings")]
        [SerializeField] private bool convertOnStart = false;
        [SerializeField] private bool showDebugInfo = true;
        
        private void Start()
        {
            if (convertOnStart)
            {
                LoadURPShaders();
                ConvertMaterials();
            }
        }
        
        /// <summary>
        /// Load URP shaders if not assigned
        /// </summary>
        [ContextMenu("Load URP Shaders")]
        public void LoadURPShaders()
        {
            Debug.Log("=== Loading URP Shaders ===");
            
            if (urpTintedWindowShader == null)
            {
                urpTintedWindowShader = Shader.Find("Universal Render Pipeline/Custom/TintedWindowCardShader");
                if (urpTintedWindowShader != null)
                    Debug.Log("✓ Loaded URP TintedWindowCardShader");
                else
                    Debug.LogError("✗ Failed to load URP TintedWindowCardShader");
            }
            
            if (urpSimpleWindowShader == null)
            {
                urpSimpleWindowShader = Shader.Find("Universal Render Pipeline/Custom/SimpleWindowShader");
                if (urpSimpleWindowShader != null)
                    Debug.Log("✓ Loaded URP SimpleWindowShader");
                else
                    Debug.LogError("✗ Failed to load URP SimpleWindowShader");
            }
            
            if (urpBasicCardShader == null)
            {
                urpBasicCardShader = Shader.Find("Universal Render Pipeline/Custom/BasicCardShader");
                if (urpBasicCardShader != null)
                    Debug.Log("✓ Loaded URP BasicCardShader");
                else
                    Debug.LogError("✗ Failed to load URP BasicCardShader");
            }
            
            if (urpCardCharacterWorldShader == null)
            {
                urpCardCharacterWorldShader = Shader.Find("Universal Render Pipeline/Custom/CardCharacterWorldShader");
                if (urpCardCharacterWorldShader != null)
                    Debug.Log("✓ Loaded URP CardCharacterWorldShader");
                else
                    Debug.LogError("✗ Failed to load URP CardCharacterWorldShader");
            }
        }
        
        /// <summary>
        /// Convert all materials in the scene to URP shaders
        /// </summary>
        [ContextMenu("Convert Materials to URP")]
        public void ConvertMaterials()
        {
            if (urpTintedWindowShader == null || urpSimpleWindowShader == null || 
                urpBasicCardShader == null || urpCardCharacterWorldShader == null)
            {
                Debug.LogError("URP shaders not loaded! Run 'Load URP Shaders' first.");
                return;
            }
            
            Debug.Log("=== Converting Materials to URP ===");
            
            var renderers = FindObjectsOfType<Renderer>();
            int convertedCount = 0;
            
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (ConvertMaterialToURP(material))
                    {
                        convertedCount++;
                    }
                }
            }
            
            Debug.Log($"=== Conversion Complete: {convertedCount} materials converted ===");
        }
        
        /// <summary>
        /// Convert a single material to URP
        /// </summary>
        private bool ConvertMaterialToURP(Material material)
        {
            if (material.shader == null) return false;
            
            string shaderName = material.shader.name;
            
            // Convert TintedWindowCardShader
            if (shaderName.Contains("Custom/TintedWindowCardShader") && urpTintedWindowShader != null)
            {
                Debug.Log($"Converting {material.name} from {shaderName} to URP");
                material.shader = urpTintedWindowShader;
                return true;
            }
            
            // Convert SimpleWindowShader
            if (shaderName.Contains("Custom/SimpleWindowShader") && urpSimpleWindowShader != null)
            {
                Debug.Log($"Converting {material.name} from {shaderName} to URP");
                material.shader = urpSimpleWindowShader;
                return true;
            }
            
            // Convert BasicCardShader
            if (shaderName.Contains("Custom/BasicCardShader") && urpBasicCardShader != null)
            {
                Debug.Log($"Converting {material.name} from {shaderName} to URP");
                material.shader = urpBasicCardShader;
                return true;
            }
            
            // Convert CardCharacterWorldShader
            if (shaderName.Contains("Custom/CardCharacterWorldShader") && urpCardCharacterWorldShader != null)
            {
                Debug.Log($"Converting {material.name} from {shaderName} to URP");
                material.shader = urpCardCharacterWorldShader;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Test if URP shaders are working
        /// </summary>
        [ContextMenu("Test URP Shaders")]
        public void TestURPShaders()
        {
            Debug.Log("=== Testing URP Shaders ===");
            
            LoadURPShaders();
            
            if (urpBasicCardShader != null)
            {
                try
                {
                    Material testMat = new Material(urpBasicCardShader);
                    testMat.SetColor("_Color", new Color(0.2f, 0.3f, 0.8f, 0.8f));
                    testMat.SetFloat("_StencilID", 1);
                    
                    Debug.Log("✓ Successfully created URP BasicCardShader material");
                    Debug.Log($"  Material: {testMat.name}");
                    Debug.Log($"  Shader: {testMat.shader.name}");
                    
                    DestroyImmediate(testMat);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"✗ Failed to create URP material: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("✗ URP BasicCardShader not available");
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 250));
            GUILayout.Label("URP Shader Converter");
            
            if (GUILayout.Button("Load URP Shaders"))
            {
                LoadURPShaders();
            }
            
            if (GUILayout.Button("Convert Materials to URP"))
            {
                ConvertMaterials();
            }
            
            if (GUILayout.Button("Test URP Shaders"))
            {
                TestURPShaders();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Status:");
            GUILayout.Label($"TintedWindow: {(urpTintedWindowShader != null ? "✓" : "✗")}");
            GUILayout.Label($"SimpleWindow: {(urpSimpleWindowShader != null ? "✓" : "✗")}");
            GUILayout.Label($"BasicCard: {(urpBasicCardShader != null ? "✓" : "✗")}");
            GUILayout.Label($"CharacterWorld: {(urpCardCharacterWorldShader != null ? "✓" : "✗")}");
            
            GUILayout.EndArea();
        }
    }
}
