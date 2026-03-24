using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.HexLevels
{
    /// <summary>
    /// Represents a layer of objects placed on a hex tile.
    /// </summary>
    [System.Serializable]
    public class HexTileLayer
    {
        public GameObject prefab;
        public GameObject instance;
        public int layerIndex; // 0 = ground, 1+ = objects on top
        public float heightOffset; // Vertical offset for this layer

        public HexTileLayer(GameObject prefab, GameObject instance, int layerIndex, float heightOffset = 0f)
        {
            this.prefab = prefab;
            this.instance = instance;
            this.layerIndex = layerIndex;
            this.heightOffset = heightOffset;
        }
    }

    /// <summary>
    /// Data structure representing what's placed on a hex tile.
    /// Supports layered placement: ground tile + objects on top.
    /// </summary>
    [System.Serializable]
    public class HexTileData
    {
        [Header("Ground Tile")]
        [Tooltip("The ground tile prefab (grass, water, etc.)")]
        public GameObject groundTilePrefab;
        
        [Tooltip("The instantiated ground tile GameObject")]
        public GameObject groundTileInstance;
        
        [Tooltip("Type of ground tile")]
        public HexTileType tileType = HexTileType.Grass;
        
        [Header("Objects on Top")]
        [Tooltip("Objects placed on top of the ground (buildings, rocks, decorations, etc.)")]
        public List<HexTileLayer> objectLayers = new List<HexTileLayer>();
        
        [Header("Height")]
        [Tooltip("Base elevation of this hex (0 = ground level)")]
        public int elevation = 0;
        
        [Tooltip("Use sloped tile variant")]
        public bool useSlope = false;
        
        [Header("Metadata")]
        [Tooltip("Custom data for this tile")]
        public string customData;

        // Legacy support - kept for backwards compatibility
        [System.Obsolete("Use groundTilePrefab instead")]
        public GameObject tilePrefab
        {
            get => groundTilePrefab;
            set => groundTilePrefab = value;
        }

        [System.Obsolete("Use objectLayers instead. This returns all object instances.")]
        public GameObject[] placedObjects
        {
            get
            {
                List<GameObject> instances = new List<GameObject>();
                foreach (var layer in objectLayers)
                {
                    if (layer.instance != null)
                        instances.Add(layer.instance);
                }
                return instances.ToArray();
            }
        }

        public HexTileData()
        {
        }

        public HexTileData(HexTileType tileType, GameObject tilePrefab = null)
        {
            this.tileType = tileType;
            this.groundTilePrefab = tilePrefab;
        }

        /// <summary>
        /// Check if this hex has a ground tile.
        /// </summary>
        public bool HasGroundTile()
        {
            return groundTileInstance != null || groundTilePrefab != null;
        }

        /// <summary>
        /// Check if this hex has any objects on top.
        /// </summary>
        public bool HasObjects()
        {
            return objectLayers != null && objectLayers.Count > 0;
        }

        /// <summary>
        /// Get the number of object layers.
        /// </summary>
        public int ObjectLayerCount => objectLayers != null ? objectLayers.Count : 0;

        /// <summary>
        /// Add an object layer on top of the ground.
        /// </summary>
        public void AddObjectLayer(GameObject prefab, GameObject instance, float heightOffset = 0f)
        {
            if (objectLayers == null)
                objectLayers = new List<HexTileLayer>();

            // Find the next layer index
            int nextLayerIndex = 1;
            if (objectLayers.Count > 0)
            {
                int maxLayer = 0;
                foreach (var layer in objectLayers)
                {
                    if (layer.layerIndex > maxLayer)
                        maxLayer = layer.layerIndex;
                }
                nextLayerIndex = maxLayer + 1;
            }

            objectLayers.Add(new HexTileLayer(prefab, instance, nextLayerIndex, heightOffset));
        }

        /// <summary>
        /// Remove an object layer by instance.
        /// </summary>
        public bool RemoveObjectLayer(GameObject instance)
        {
            if (objectLayers == null)
                return false;

            for (int i = objectLayers.Count - 1; i >= 0; i--)
            {
                if (objectLayers[i].instance == instance)
                {
                    objectLayers.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Remove an object layer by index.
        /// </summary>
        public bool RemoveObjectLayer(int layerIndex)
        {
            if (objectLayers == null)
                return false;

            for (int i = objectLayers.Count - 1; i >= 0; i--)
            {
                if (objectLayers[i].layerIndex == layerIndex)
                {
                    objectLayers.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Clear all object layers.
        /// </summary>
        public void ClearObjectLayers()
        {
            if (objectLayers != null)
                objectLayers.Clear();
        }

        /// <summary>
        /// Clear the ground tile.
        /// </summary>
        public void ClearGroundTile()
        {
            groundTilePrefab = null;
            groundTileInstance = null;
            tileType = HexTileType.None;
        }

        /// <summary>
        /// Clear everything (ground and objects).
        /// </summary>
        public void ClearAll()
        {
            ClearGroundTile();
            ClearObjectLayers();
        }

        // Legacy support methods
        [System.Obsolete("Use AddObjectLayer instead")]
        public void AddObject(GameObject obj)
        {
            AddObjectLayer(null, obj, 0f);
        }

        [System.Obsolete("Use RemoveObjectLayer instead")]
        public void RemoveObject(GameObject obj)
        {
            RemoveObjectLayer(obj);
        }

        [System.Obsolete("Use ClearObjectLayers instead")]
        public void ClearObjects()
        {
            ClearObjectLayers();
        }
    }

    /// <summary>
    /// Types of hex tiles available.
    /// </summary>
    public enum HexTileType
    {
        None,
        Grass,
        Water,
        Coast,
        River,
        Road
    }
}
