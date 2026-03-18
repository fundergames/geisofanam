using UnityEngine;
using UnityEditor;

namespace RogueDeal.HexLevels.Editor
{
    [InitializeOnLoad]
    public static class HexEditorBridge
    {
        static HexEditorBridge()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        private static void OnSceneGUI(SceneView sceneView)
        {
            HandleKeyboardShortcuts();
        }
        
        private static void HandleKeyboardShortcuts()
        {
            Event e = Event.current;
            
            if (e.type != EventType.KeyDown)
                return;
            
            var editorState = FindEditorState();
            if (editorState == null)
                return;
            
            switch (e.keyCode)
            {
                case KeyCode.Alpha1:
                    editorState.SetMode(HexEditorMode.Place);
                    e.Use();
                    break;
                    
                case KeyCode.Alpha2:
                    editorState.SetMode(HexEditorMode.Erase);
                    e.Use();
                    break;
                    
                case KeyCode.Alpha3:
                    editorState.SetMode(HexEditorMode.Edit);
                    e.Use();
                    break;
                    
                case KeyCode.Tab:
                    editorState.SetLayer(editorState.layer == HexEditorLayer.Tiles 
                        ? HexEditorLayer.Decorations 
                        : HexEditorLayer.Tiles);
                    e.Use();
                    break;
                    
                case KeyCode.LeftBracket:
                    editorState.SetElevation(editorState.elevation - 1);
                    e.Use();
                    break;
                    
                case KeyCode.RightBracket:
                    editorState.SetElevation(editorState.elevation + 1);
                    e.Use();
                    break;
                    
                case KeyCode.R:
                    if (e.shift)
                        editorState.RotateCounterClockwise();
                    else
                        editorState.RotateClockwise();
                    e.Use();
                    break;
                    
                case KeyCode.Escape:
                    editorState.ClearSelection();
                    e.Use();
                    break;
                    
                case KeyCode.Q:
                    CyclePreviousAsset();
                    e.Use();
                    break;
                    
                case KeyCode.E:
                    CycleNextAsset();
                    e.Use();
                    break;
                    
                case KeyCode.F:
                    ToggleFavorite();
                    e.Use();
                    break;
            }
        }
        
        private static HexEditorState FindEditorState()
        {
            var windows = Resources.FindObjectsOfTypeAll<HexEditorUIController>();
            if (windows.Length > 0)
            {
                var stateField = typeof(HexEditorUIController).GetField("editorState", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (stateField != null)
                {
                    return stateField.GetValue(windows[0]) as HexEditorState;
                }
            }
            return null;
        }
        
        private static void CyclePreviousAsset()
        {
            Debug.Log("Cycle to previous asset");
        }
        
        private static void CycleNextAsset()
        {
            Debug.Log("Cycle to next asset");
        }
        
        private static void ToggleFavorite()
        {
            Debug.Log("Toggle favorite");
        }
        
        public static void SyncWithLegacyEditor(HexLevelEditorTool legacyTool, HexEditorState newState)
        {
            if (legacyTool == null || newState == null)
                return;
            
            switch (newState.mode)
            {
                case HexEditorMode.Place:
                    legacyTool.toolMode = EditorToolMode.Place;
                    break;
                case HexEditorMode.Erase:
                    legacyTool.toolMode = EditorToolMode.Delete;
                    break;
                case HexEditorMode.Edit:
                    legacyTool.toolMode = EditorToolMode.Select;
                    break;
            }
            
            legacyTool.rotation = newState.rotation;
            legacyTool.selectedPrefab = newState.activeAsset;
            legacyTool.showPreview = newState.showPreview;
            
            if (newState.layer == HexEditorLayer.Tiles)
            {
                legacyTool.placementLayerMode = PlacementLayerMode.Ground;
            }
            else
            {
                legacyTool.placementLayerMode = PlacementLayerMode.Object;
            }
        }
    }
}
