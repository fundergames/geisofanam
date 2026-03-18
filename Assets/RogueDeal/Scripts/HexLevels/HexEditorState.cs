using UnityEngine;
using System;

namespace RogueDeal.HexLevels
{
    public class HexEditorState : ScriptableObject
    {
        public HexEditorMode mode = HexEditorMode.Place;
        public HexEditorLayer layer = HexEditorLayer.Tiles;
        public int elevation = 0;
        public int brushSize = 1;
        public GameObject activeAsset;
        
        public int rotation = 0;
        public int rotationStep = 60;
        
        public bool gridVisible = true;
        public bool collisionSnap = true;
        public bool gizmoVisible = true;
        public bool showPreview = true;
        
        public bool autosave = false;
        public float autosaveInterval = 300f;
        
        public bool dragPaint = true;
        public bool followCursor = true;
        
        public HexCoordinate? hoveredHex;
        public HexCoordinate? selectedHex;
        public HexTileData hoveredTileData;
        
        public event Action OnStateChanged;
        
        public void SetMode(HexEditorMode newMode)
        {
            if (mode != newMode)
            {
                mode = newMode;
                OnStateChanged?.Invoke();
            }
        }
        
        public void SetLayer(HexEditorLayer newLayer)
        {
            if (layer != newLayer)
            {
                layer = newLayer;
                OnStateChanged?.Invoke();
            }
        }
        
        public void SetElevation(int newElevation)
        {
            elevation = Mathf.Clamp(newElevation, -10, 10);
            OnStateChanged?.Invoke();
        }
        
        public void SetRotation(int newRotation)
        {
            rotation = ((newRotation % 6) + 6) % 6;
            OnStateChanged?.Invoke();
        }
        
        public void RotateClockwise()
        {
            SetRotation(rotation + 1);
        }
        
        public void RotateCounterClockwise()
        {
            SetRotation(rotation - 1);
        }
        
        public float GetRotationDegrees()
        {
            return rotation * rotationStep;
        }
        
        public void SetActiveAsset(GameObject asset)
        {
            activeAsset = asset;
            OnStateChanged?.Invoke();
        }
        
        public void UpdateHoverInfo(HexCoordinate? hex, HexTileData data)
        {
            hoveredHex = hex;
            hoveredTileData = data;
            OnStateChanged?.Invoke();
        }
        
        public void ClearSelection()
        {
            selectedHex = null;
            OnStateChanged?.Invoke();
        }
    }
    
    public enum HexEditorMode
    {
        Place,
        Erase,
        Edit
    }
    
    public enum HexEditorLayer
    {
        Tiles,
        Decorations
    }
}
