using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueDeal.HexLevels.Runtime
{
    public class HexEditorDebug : MonoBehaviour
    {
        public HexEditorController controller;
        public HexEditorRuntimeState editorState;
        public Camera editorCamera;
        public LayerMask groundLayer;
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                DebugPrintState();
            }
            
            if (Mouse.current != null && editorCamera != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                Ray ray = editorCamera.ScreenPointToRay(mousePos);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
                {
                    Debug.DrawLine(ray.origin, hit.point, Color.green);
                }
                else
                {
                    Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);
                }
            }
        }
        
        private void DebugPrintState()
        {
            Debug.Log("=== HEX EDITOR DEBUG ===");
            Debug.Log($"Mouse.current: {Mouse.current != null}");
            Debug.Log($"editorCamera: {editorCamera != null}");
            Debug.Log($"editorState: {editorState != null}");
            
            if (editorState != null)
            {
                Debug.Log($"  mode: {editorState.mode}");
                Debug.Log($"  activeAsset: {(editorState.activeAsset != null ? editorState.activeAsset.name : "NULL")}");
                Debug.Log($"  showPreview: {editorState.showPreview}");
                Debug.Log($"  hoveredHex: {editorState.hoveredHex}");
                Debug.Log($"  layer: {editorState.layer}");
                Debug.Log($"  elevation: {editorState.elevation}");
            }
            
            if (controller != null)
            {
                Debug.Log($"controller.hexGrid: {controller.hexGrid != null}");
                Debug.Log($"controller.previewMaterialValid: {controller.previewMaterialValid != null}");
                Debug.Log($"controller.previewMaterialInvalid: {controller.previewMaterialInvalid != null}");
                Debug.Log($"controller.previewMaterialReplace: {controller.previewMaterialReplace != null}");
                
                GameObject previewObj = GameObject.Find("Preview");
                Debug.Log($"Preview GameObject exists: {previewObj != null}");
                if (previewObj != null)
                {
                    Debug.Log($"  Preview position: {previewObj.transform.position}");
                    Debug.Log($"  Preview active: {previewObj.activeSelf}");
                }
            }
            
            Debug.Log($"groundLayer value: {groundLayer.value}");
            Debug.Log("=====================");
        }
    }
}
