using UnityEngine;

namespace RogueDeal.HexLevels.Runtime
{
    public class HexEditorMaterialSetup : MonoBehaviour
    {
        [Header("Controller Reference")]
        public HexEditorController controller;
        
        [Header("Auto-Created Materials")]
        public Material previewValid;
        public Material previewInvalid;
        public Material previewReplace;
        
        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<HexEditorController>();
            }
            
            CreatePreviewMaterials();
            AssignMaterialsToController();
        }
        
        private void CreatePreviewMaterials()
        {
            if (previewValid == null)
            {
                previewValid = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                previewValid.name = "PreviewMaterial_Valid";
                previewValid.color = new Color(0f, 1f, 0f, 0.5f);
                SetTransparent(previewValid);
            }
            
            if (previewInvalid == null)
            {
                previewInvalid = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                previewInvalid.name = "PreviewMaterial_Invalid";
                previewInvalid.color = new Color(1f, 0f, 0f, 0.5f);
                SetTransparent(previewInvalid);
            }
            
            if (previewReplace == null)
            {
                previewReplace = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                previewReplace.name = "PreviewMaterial_Replace";
                previewReplace.color = new Color(1f, 0.8f, 0f, 0.5f);
                SetTransparent(previewReplace);
            }
        }
        
        private void SetTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            
            mat.SetOverrideTag("RenderType", "Transparent");
        }
        
        private void AssignMaterialsToController()
        {
            if (controller != null)
            {
                controller.previewMaterialValid = previewValid;
                controller.previewMaterialInvalid = previewInvalid;
                controller.previewMaterialReplace = previewReplace;
            }
        }
    }
}
