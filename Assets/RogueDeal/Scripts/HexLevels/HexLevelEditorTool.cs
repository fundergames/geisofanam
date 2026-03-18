using UnityEngine;
using System.Collections.Generic;

namespace RogueDeal.HexLevels
{
    /// <summary>
    /// Component that enables hex level editing in Scene view.
    /// Stores editor state and handles tool interactions.
    /// </summary>
    [System.Serializable]
    public class HexLevelEditorTool : MonoBehaviour
    {
        [Header("Editor Settings")]
        [Tooltip("The HexGrid to edit")]
        public HexGrid targetGrid;
        
        [Tooltip("Current tool mode")]
        public EditorToolMode toolMode = EditorToolMode.Place;
        
        [Tooltip("Placement layer mode - Ground or Object")]
        public PlacementLayerMode placementLayerMode = PlacementLayerMode.Auto;
        
        [Tooltip("Selected prefab to place")]
        public GameObject selectedPrefab;
        
        [Tooltip("Current rotation (0-5 for 60-degree increments)")]
        [Range(0, 5)]
        public int rotation = 0;
        
        [Header("Smart Placement")]
        [Tooltip("Enable smart tile selection based on neighbors (auto-connects roads/rivers/coast)")]
        public bool enableSmartPlacement = true;
        
        [Tooltip("Connection pattern mappings asset (auto-loaded if not set)")]
        public ConnectionPatternMappings mappingsAsset;
        
        [Header("Visual Feedback")]
        [Tooltip("Show preview ghost of selected prefab")]
        public bool showPreview = true;
        
        [Tooltip("Color for placement preview")]
        public Color previewColor = new Color(1f, 1f, 0f, 0.5f);
        
        [Tooltip("Color for selected hex highlight")]
        public Color selectionColor = new Color(0f, 1f, 1f, 0.3f);

        private HexCoordinate? hoveredHex = null;
        private HexCoordinate? selectedHex = null;
        
        // Smart placement variant cycling
        [System.NonSerialized]
        private int smartPlacementVariantIndex = 0;
        [System.NonSerialized]
        private List<GameObject> currentSmartPlacementVariants = new List<GameObject>();
        [System.NonSerialized]
        private HexCoordinate? lastSmartPlacementHex = null;

        private void OnValidate()
        {
            // Ensure rotation is within valid range
            rotation = Mathf.Clamp(rotation, 0, 5);
            
            // Auto-find HexGrid if not set
            if (targetGrid == null)
            {
                targetGrid = GetComponent<HexGrid>();
                if (targetGrid == null)
                {
                    targetGrid = FindFirstObjectByType<HexGrid>();
                }
            }
        }

        /// <summary>
        /// Get the hovered hex coordinate (for preview).
        /// </summary>
        public HexCoordinate? GetHoveredHex()
        {
            return hoveredHex;
        }

        /// <summary>
        /// Set the hovered hex coordinate (called by editor).
        /// </summary>
        public void SetHoveredHex(HexCoordinate? hex)
        {
            hoveredHex = hex;
        }

        /// <summary>
        /// Get the selected hex coordinate.
        /// </summary>
        public HexCoordinate? GetSelectedHex()
        {
            return selectedHex;
        }

        /// <summary>
        /// Set the selected hex coordinate (called by editor).
        /// </summary>
        public void SetSelectedHex(HexCoordinate? hex)
        {
            selectedHex = hex;
        }

        /// <summary>
        /// Get rotation as degrees (0, 60, 120, 180, 240, 300).
        /// </summary>
        public float GetRotationDegrees()
        {
            return rotation * 60f;
        }
        
        /// <summary>
        /// Get the current smart placement variant index.
        /// </summary>
        public int GetSmartPlacementVariantIndex()
        {
            return smartPlacementVariantIndex;
        }
        
        /// <summary>
        /// Set the smart placement variant index.
        /// </summary>
        public void SetSmartPlacementVariantIndex(int index)
        {
            if (currentSmartPlacementVariants != null && currentSmartPlacementVariants.Count > 0)
            {
                smartPlacementVariantIndex = ((index % currentSmartPlacementVariants.Count) + currentSmartPlacementVariants.Count) % currentSmartPlacementVariants.Count;
            }
            else
            {
                smartPlacementVariantIndex = 0;
            }
        }
        
        /// <summary>
        /// Get the current smart placement variant prefab.
        /// </summary>
        public GameObject GetCurrentSmartPlacementVariant()
        {
            if (currentSmartPlacementVariants != null && currentSmartPlacementVariants.Count > 0)
            {
                return currentSmartPlacementVariants[smartPlacementVariantIndex];
            }
            return null;
        }
        
        /// <summary>
        /// Set the available smart placement variants for a hex.
        /// </summary>
        public void SetSmartPlacementVariants(HexCoordinate hex, List<GameObject> variants)
        {
            // Reset index if hex changed
            if (!lastSmartPlacementHex.HasValue || lastSmartPlacementHex.Value != hex)
            {
                smartPlacementVariantIndex = 0;
                lastSmartPlacementHex = hex;
            }
            
            currentSmartPlacementVariants = variants ?? new List<GameObject>();
            
            // Ensure index is valid
            if (currentSmartPlacementVariants.Count > 0)
            {
                smartPlacementVariantIndex = Mathf.Clamp(smartPlacementVariantIndex, 0, currentSmartPlacementVariants.Count - 1);
            }
            else
            {
                smartPlacementVariantIndex = 0;
            }
        }
        
        /// <summary>
        /// Get the count of available smart placement variants.
        /// </summary>
        public int GetSmartPlacementVariantCount()
        {
            return currentSmartPlacementVariants != null ? currentSmartPlacementVariants.Count : 0;
        }
    }

    /// <summary>
    /// Available editor tool modes.
    /// </summary>
    public enum EditorToolMode
    {
        Place,      // Place tiles/objects
        Delete,     // Delete tiles/objects
        Select,     // Select hexes (for future multi-select)
    }

    /// <summary>
    /// Placement layer mode - determines what layer to place on.
    /// </summary>
    public enum PlacementLayerMode
    {
        Ground,     // Place/replace ground tile
        Object,     // Place object on top of ground
        Auto        // Automatically detect based on prefab type
    }
}