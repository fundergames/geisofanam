using UnityEngine;

namespace FunderGames.RPG
{
    /// <summary>
    /// Diagnostic script to identify shader issues
    /// </summary>
    public class ShaderDiagnostic : MonoBehaviour
    {
        [Header("Diagnostic Settings")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private KeyCode diagnosticKey = KeyCode.D;
        
        [Header("Test Materials")]
        [SerializeField] private Material testMaterial;
        
        private void Start()
        {
            if (runOnStart)
            {
                RunShaderDiagnostic();
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(diagnosticKey))
            {
                RunShaderDiagnostic();
            }
        }
        
        /// <summary>
        /// Run comprehensive shader diagnostics
        /// </summary>
        [ContextMenu("Run Shader Diagnostic")]
        public void RunShaderDiagnostic()
        {
            Debug.Log("=== SHADER DIAGNOSTIC START ===");
            
            // Test shader compilation
            TestShaderCompilation();
            
            // Test material setup
            TestMaterialSetup();
            
            // Test stencil system
            TestStencilSystem();
            
            // Test render pipeline
            TestRenderPipeline();
            
            Debug.Log("=== SHADER DIAGNOSTIC COMPLETE ===");
        }
        
        /// <summary>
        /// Test if shaders can compile and load
        /// </summary>
        private void TestShaderCompilation()
        {
            Debug.Log("\n--- Testing Shader Compilation ---");
            
            string[] shaderNames = {
                "Custom/TintedWindowCardShader",
                "Custom/SimpleWindowShader", 
                "Custom/BasicCardShader",
                "Custom/CardWindowObjectShader"
            };
            
            foreach (string shaderName in shaderNames)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    Debug.Log($"✓ {shaderName} - Found and loaded successfully");
                    
                    // Check if shader is supported
                    if (shader.isSupported)
                    {
                        Debug.Log($"  ✓ Shader is supported on this platform");
                    }
                    else
                    {
                        Debug.LogWarning($"  ✗ Shader is NOT supported on this platform");
                    }
                    
                    // Check shader keywords
                    if (shader.keywordSpace.keywordCount > 0)
                    {
                        Debug.Log($"  Keywords: {shader.keywordSpace.keywordCount}");
                    }
                }
                else
                {
                    Debug.LogError($"✗ {shaderName} - NOT FOUND! This is likely the problem.");
                }
            }
        }
        
        /// <summary>
        /// Test material setup and properties
        /// </summary>
        private void TestMaterialSetup()
        {
            Debug.Log("\n--- Testing Material Setup ---");
            
            // Find all materials in the scene
            var renderers = FindObjectsOfType<Renderer>();
            int materialCount = 0;
            int shaderErrorCount = 0;
            
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    materialCount++;
                    
                    if (material.shader == null)
                    {
                        Debug.LogError($"✗ Material {material.name} has NULL shader!");
                        shaderErrorCount++;
                        continue;
                    }
                    
                    if (material.shader.name.Contains("Custom/"))
                    {
                        Debug.Log($"\nMaterial: {material.name}");
                        Debug.Log($"  Shader: {material.shader.name}");
                        Debug.Log($"  Renderer: {renderer.name}");
                        
                        // Check if material is showing magenta
                        if (IsMaterialMagenta(material))
                        {
                            Debug.LogWarning($"  ⚠ Material appears to be showing MAGENTA");
                        }
                        
                        // Check material properties
                        CheckMaterialProperties(material);
                    }
                }
            }
            
            Debug.Log($"\nTotal Materials: {materialCount}");
            Debug.Log($"Shader Errors: {shaderErrorCount}");
        }
        
        /// <summary>
        /// Check if a material is showing magenta
        /// </summary>
        private bool IsMaterialMagenta(Material material)
        {
            // This is a heuristic - magenta materials often have specific color values
            if (material.HasProperty("_Color"))
            {
                Color color = material.GetColor("_Color");
                // Check if color is close to magenta (high red, low green, high blue)
                if (color.r > 0.8f && color.g < 0.2f && color.b > 0.8f)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Check material properties for common issues
        /// </summary>
        private void CheckMaterialProperties(Material material)
        {
            string[] commonProperties = {
                "_MainTex", "_Color", "_WindowColor", "_StencilID", "_UseTexture", "_EdgeSoftness"
            };
            
            foreach (string propName in commonProperties)
            {
                if (material.HasProperty(propName))
                {
                    Debug.Log($"    {propName}: Found");
                    
                    // Check if texture property has a texture
                    if (propName == "_MainTex")
                    {
                        Texture tex = material.GetTexture(propName);
                        if (tex != null)
                        {
                            Debug.Log($"      ✓ Texture assigned: {tex.name}");
                        }
                        else
                        {
                            Debug.LogWarning($"      ✗ No texture assigned to {propName}");
                        }
                    }
                    else if (propName == "_Color" || propName == "_WindowColor")
                    {
                        Color color = material.GetColor(propName);
                        Debug.Log($"      Color value: {color}");
                    }
                    else if (propName == "_StencilID" || propName == "_UseTexture" || propName == "_EdgeSoftness")
                    {
                        float value = material.GetFloat(propName);
                        Debug.Log($"      Float value: {value}");
                    }
                }
                else
                {
                    Debug.LogWarning($"    ✗ Missing property: {propName}");
                }
            }
        }
        
        /// <summary>
        /// Test the stencil system
        /// </summary>
        private void TestStencilSystem()
        {
            Debug.Log("\n--- Testing Stencil System ---");
            
            // Check if depth buffer is enabled
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))
            {
                Debug.Log("✓ Depth buffer supported");
            }
            else
            {
                Debug.LogWarning("⚠ Depth buffer not supported - this may cause stencil issues");
            }
            
            // Check stencil buffer support - use Depth for stencil testing
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))
            {
                Debug.Log("✓ Depth buffer supported (includes stencil)");
            }
            else
            {
                Debug.LogWarning("⚠ Depth buffer not supported - this may affect stencil masking!");
            }
            
            // Check if stencil operations are supported
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
            {
                Debug.Log("✓ Standard color buffer supported");
            }
            else
            {
                Debug.LogWarning("⚠ Standard color buffer not supported!");
            }
        }
        
        /// <summary>
        /// Test render pipeline compatibility
        /// </summary>
        private void TestRenderPipeline()
        {
            Debug.Log("\n--- Testing Render Pipeline ---");
            
            var pipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            if (pipeline != null)
            {
                Debug.Log($"✓ Using Render Pipeline: {pipeline.name}");
                Debug.Log($"  Type: {pipeline.GetType().Name}");
            }
            else
            {
                Debug.Log("✓ Using Built-in Render Pipeline");
            }
            
            // Check if we're in URP/HDRP
            if (pipeline != null && pipeline.GetType().Name.Contains("Universal"))
            {
                Debug.Log("⚠ Universal Render Pipeline detected - shaders may need URP variants");
            }
            else if (pipeline != null && pipeline.GetType().Name.Contains("HighDefinition"))
            {
                Debug.Log("⚠ High Definition Render Pipeline detected - shaders may need HDRP variants");
            }
        }
        
        /// <summary>
        /// Create a test material with the basic shader
        /// </summary>
        [ContextMenu("Create Test Material")]
        public void CreateTestMaterial()
        {
            Shader basicShader = Shader.Find("Custom/BasicCardShader");
            if (basicShader == null)
            {
                Debug.LogError("BasicCardShader not found! Check shader compilation.");
                return;
            }
            
            Material testMat = new Material(basicShader);
            testMat.name = "TestCardMaterial";
            testMat.SetColor("_Color", new Color(0.2f, 0.3f, 0.8f, 0.8f));
            testMat.SetFloat("_StencilID", 1);
            
            Debug.Log($"Created test material: {testMat.name}");
            
            // Assign to test material field
            testMaterial = testMat;
            
            // Save to project
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.CreateAsset(testMat, "Assets/FunderGames/RPG/Assets/Materials/" + testMat.name + ".mat");
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("Test material saved to project!");
            #endif
        }
        
        /// <summary>
        /// Test if the basic shader actually works
        /// </summary>
        [ContextMenu("Test Basic Shader")]
        public void TestBasicShader()
        {
            Debug.Log("=== Testing Basic Shader ===");
            
            // Try to find the basic shader
            Shader basicShader = Shader.Find("Custom/BasicCardShader");
            if (basicShader == null)
            {
                Debug.LogError("✗ BasicCardShader not found!");
                Debug.LogError("This means the shader file is missing or has compilation errors.");
                return;
            }
            
            Debug.Log("✓ BasicCardShader found!");
            
            // Check if it's supported
            if (basicShader.isSupported)
            {
                Debug.Log("✓ Shader is supported on this platform");
            }
            else
            {
                Debug.LogWarning("⚠ Shader is NOT supported on this platform");
            }
            
            // Try to create a material with it
            try
            {
                Material testMat = new Material(basicShader);
                Debug.Log("✓ Successfully created material with BasicCardShader");
                
                // Set properties
                testMat.SetColor("_Color", new Color(0.2f, 0.3f, 0.8f, 0.8f));
                testMat.SetFloat("_StencilID", 1);
                Debug.Log("✓ Successfully set material properties");
                
                // Clean up
                DestroyImmediate(testMat);
                Debug.Log("✓ Test completed successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Failed to create material: {e.Message}");
            }
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Shader Diagnostic");
            
            if (GUILayout.Button("Run Diagnostic"))
            {
                RunShaderDiagnostic();
            }
            
            if (GUILayout.Button("Create Test Material"))
            {
                CreateTestMaterial();
            }
            
            if (GUILayout.Button("Test Basic Shader"))
            {
                TestBasicShader();
            }
            
            GUILayout.Label("Press D key to run diagnostic");
            
            GUILayout.EndArea();
        }
    }
}
