using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueDeal.HexLevels.Runtime
{
    public class HexEditorInputHandler : MonoBehaviour
    {
        [Header("References")]
        public HexEditorRuntimeState editorState;
        public HexEditorUICompact editorUI;
        public HexEditorController editorController;
        
        [Header("Input Actions")]
        public InputActionReference modePlace;
        public InputActionReference modeErase;
        public InputActionReference modeEdit;
        public InputActionReference toggleLayer;
        public InputActionReference elevationUp;
        public InputActionReference elevationDown;
        public InputActionReference rotateClockwise;
        public InputActionReference rotateCounterClockwise;
        public InputActionReference cycleNextAsset;
        public InputActionReference cyclePreviousAsset;
        public InputActionReference toggleFavorite;
        public InputActionReference cancel;
        
        private void Awake()
        {
            if (editorState == null)
            {
                editorState = FindObjectOfType<HexEditorRuntimeState>();
            }
            
            if (editorController == null)
            {
                editorController = FindObjectOfType<HexEditorController>();
            }
        }
        
        private void OnEnable()
        {
            EnableActions();
            SubscribeToActions();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromActions();
            DisableActions();
        }
        
        private void EnableActions()
        {
            modePlace?.action?.Enable();
            modeErase?.action?.Enable();
            modeEdit?.action?.Enable();
            toggleLayer?.action?.Enable();
            elevationUp?.action?.Enable();
            elevationDown?.action?.Enable();
            rotateClockwise?.action?.Enable();
            rotateCounterClockwise?.action?.Enable();
            cycleNextAsset?.action?.Enable();
            cyclePreviousAsset?.action?.Enable();
            toggleFavorite?.action?.Enable();
            cancel?.action?.Enable();
        }
        
        private void DisableActions()
        {
            modePlace?.action?.Disable();
            modeErase?.action?.Disable();
            modeEdit?.action?.Disable();
            toggleLayer?.action?.Disable();
            elevationUp?.action?.Disable();
            elevationDown?.action?.Disable();
            rotateClockwise?.action?.Disable();
            rotateCounterClockwise?.action?.Disable();
            cycleNextAsset?.action?.Disable();
            cyclePreviousAsset?.action?.Disable();
            toggleFavorite?.action?.Disable();
            cancel?.action?.Disable();
        }
        
        private void SubscribeToActions()
        {
            if (modePlace?.action != null)
                modePlace.action.performed += OnModePlace;
            if (modeErase?.action != null)
                modeErase.action.performed += OnModeErase;
            if (modeEdit?.action != null)
                modeEdit.action.performed += OnModeEdit;
            if (toggleLayer?.action != null)
                toggleLayer.action.performed += OnToggleLayer;
            if (elevationUp?.action != null)
                elevationUp.action.performed += OnElevationUp;
            if (elevationDown?.action != null)
                elevationDown.action.performed += OnElevationDown;
            if (rotateClockwise?.action != null)
                rotateClockwise.action.performed += OnRotateClockwise;
            if (rotateCounterClockwise?.action != null)
                rotateCounterClockwise.action.performed += OnRotateCounterClockwise;
            if (cycleNextAsset?.action != null)
                cycleNextAsset.action.performed += OnCycleNextAsset;
            if (cyclePreviousAsset?.action != null)
                cyclePreviousAsset.action.performed += OnCyclePreviousAsset;
            if (toggleFavorite?.action != null)
                toggleFavorite.action.performed += OnToggleFavorite;
            if (cancel?.action != null)
                cancel.action.performed += OnCancel;
        }
        
        private void UnsubscribeFromActions()
        {
            if (modePlace?.action != null)
                modePlace.action.performed -= OnModePlace;
            if (modeErase?.action != null)
                modeErase.action.performed -= OnModeErase;
            if (modeEdit?.action != null)
                modeEdit.action.performed -= OnModeEdit;
            if (toggleLayer?.action != null)
                toggleLayer.action.performed -= OnToggleLayer;
            if (elevationUp?.action != null)
                elevationUp.action.performed -= OnElevationUp;
            if (elevationDown?.action != null)
                elevationDown.action.performed -= OnElevationDown;
            if (rotateClockwise?.action != null)
                rotateClockwise.action.performed -= OnRotateClockwise;
            if (rotateCounterClockwise?.action != null)
                rotateCounterClockwise.action.performed -= OnRotateCounterClockwise;
            if (cycleNextAsset?.action != null)
                cycleNextAsset.action.performed -= OnCycleNextAsset;
            if (cyclePreviousAsset?.action != null)
                cyclePreviousAsset.action.performed -= OnCyclePreviousAsset;
            if (toggleFavorite?.action != null)
                toggleFavorite.action.performed -= OnToggleFavorite;
            if (cancel?.action != null)
                cancel.action.performed -= OnCancel;
        }
        
        private void OnModePlace(InputAction.CallbackContext context)
        {
            if (editorState != null)
                editorState.SetMode(RuntimeEditorMode.Place);
        }
        
        private void OnModeErase(InputAction.CallbackContext context)
        {
            if (editorState != null)
                editorState.SetMode(RuntimeEditorMode.Erase);
        }
        
        private void OnModeEdit(InputAction.CallbackContext context)
        {
            if (editorState != null)
                editorState.SetMode(RuntimeEditorMode.Edit);
        }
        
        private void OnToggleLayer(InputAction.CallbackContext context)
        {
            if (editorState != null)
            {
                editorState.SetLayer(editorState.layer == RuntimeEditorLayer.Tiles 
                    ? RuntimeEditorLayer.Decorations 
                    : RuntimeEditorLayer.Tiles);
            }
        }
        
        private void OnElevationUp(InputAction.CallbackContext context)
        {
            if (editorState != null)
                editorState.SetElevation(editorState.elevation + 1);
        }
        
        private void OnElevationDown(InputAction.CallbackContext context)
        {
            if (editorState != null)
                editorState.SetElevation(editorState.elevation - 1);
        }
        
        private void OnRotateClockwise(InputAction.CallbackContext context)
        {
            if (editorState != null)
                editorState.RotateClockwise();
        }
        
        private void OnRotateCounterClockwise(InputAction.CallbackContext context)
        {
            if (editorState != null)
                editorState.RotateCounterClockwise();
        }
        
        private void OnCycleNextAsset(InputAction.CallbackContext context)
        {
            if (editorUI != null)
                editorUI.CycleNextAsset();
        }
        
        private void OnCyclePreviousAsset(InputAction.CallbackContext context)
        {
            if (editorUI != null)
                editorUI.CyclePreviousAsset();
        }
        
        private void OnToggleFavorite(InputAction.CallbackContext context)
        {
            // Favorite feature - could be implemented later if needed
        }
        
        private void OnCancel(InputAction.CallbackContext context)
        {
            if (editorController != null)
            {
                editorController.CancelEditing();
            }
            
            if (editorState != null)
                editorState.ClearSelection();
        }
    }
}
