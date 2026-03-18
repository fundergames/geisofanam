using UnityEngine;
using System;

namespace RogueDeal.HexLevels.Runtime
{
    public enum RuntimeEditorMode
    {
        Place,
        Erase,
        Edit,
        Paint
    }
    
    public enum RuntimeEditorLayer
    {
        Tiles,
        Decorations
    }
    
    public class HexEditorRuntimeState : MonoBehaviour
    {
        [Header("State")]
        public RuntimeEditorMode mode = RuntimeEditorMode.Place;
        public RuntimeEditorLayer layer = RuntimeEditorLayer.Tiles;
        public int elevation = 0;
        public int brushSize = 1;
        public int rotation = 0;
        
        [Header("Asset Selection")]
        public GameObject activeAsset;
        
        [Header("Settings")]
        public bool showPreview = true;
        public bool showGrid = true;
        public bool snapToGrid = true;
        public bool dragPaint = false;
        
        [Header("Hover & Selection")]
        public HexCoordinate? hoveredHex;
        public HexCoordinate? selectedHex;
        
        public event Action OnStateChanged;
        
        public void SetMode(RuntimeEditorMode newMode)
        {
            if (mode != newMode)
            {
                mode = newMode;
                OnStateChanged?.Invoke();
            }
        }
        
        public void SetLayer(RuntimeEditorLayer newLayer)
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
            rotation = newRotation % 6;
            if (rotation < 0)
                rotation += 6;
            OnStateChanged?.Invoke();
        }
        
        public void SetBrushSize(int size)
        {
            brushSize = Mathf.Clamp(size, 1, 5);
            OnStateChanged?.Invoke();
        }
        
        public void SetActiveAsset(GameObject asset)
        {
            activeAsset = asset;
            OnStateChanged?.Invoke();
        }
        
        public void RotateClockwise()
        {
            rotation = (rotation + 1) % 6;
            OnStateChanged?.Invoke();
        }
        
        public void RotateCounterClockwise()
        {
            rotation = (rotation - 1 + 6) % 6;
            OnStateChanged?.Invoke();
        }
        
        public float GetRotationDegrees()
        {
            return rotation * 60f;
        }
        
        public void SetHoveredHex(HexCoordinate? hex)
        {
            if (!hoveredHex.Equals(hex))
            {
                hoveredHex = hex;
                OnStateChanged?.Invoke();
            }
        }
        
        public void SetSelectedHex(HexCoordinate? hex)
        {
            if (!selectedHex.Equals(hex))
            {
                selectedHex = hex;
                OnStateChanged?.Invoke();
            }
        }
        
        public void ClearSelection()
        {
            SetSelectedHex(null);
        }
    }
}
