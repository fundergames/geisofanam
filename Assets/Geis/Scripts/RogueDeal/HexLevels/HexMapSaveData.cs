using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.HexLevels
{
    [Serializable]
    public class HexMapSaveData
    {
        public int version = 1;
        public string mapName;
        public string description;
        public long saveTimestamp;
        
        public int mapWidth = 100;
        public int mapHeight = 100;
        public float hexSize = 1f;
        
        public List<HexTileSaveData> tiles = new List<HexTileSaveData>();
        public List<DecorationSaveData> decorations = new List<DecorationSaveData>();
        
        [Serializable]
        public class HexTileSaveData
        {
            public int q;
            public int r;
            public string tileType;
            public int elevation;
            public string prefabPath;
            public int rotation;
            public string metadata;
        }
        
        [Serializable]
        public class DecorationSaveData
        {
            public int q;
            public int r;
            public string prefabPath;
            public float[] localOffset = new float[3];
            public int rotation;
            public float scale = 1f;
            public int layerIndex;
            public float heightOffset;
        }
        
        public static HexMapSaveData FromHexGrid(HexGrid grid)
        {
            HexMapSaveData data = new HexMapSaveData();
            data.saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            data.hexSize = grid.HexSize;
            data.mapWidth = grid.MaxDimensions.x;
            data.mapHeight = grid.MaxDimensions.y;
            
            foreach (var hex in grid.GetAllHexes())
            {
                HexTileData tileData = grid.GetTile(hex);
                if (tileData == null)
                    continue;
                
                if (tileData.HasGroundTile())
                {
                    HexTileSaveData tileSave = new HexTileSaveData
                    {
                        q = hex.q,
                        r = hex.r,
                        tileType = tileData.tileType.ToString(),
                        elevation = tileData.elevation,
                        prefabPath = GetPrefabPath(tileData.groundTilePrefab),
                        rotation = GetRotationFromInstance(tileData.groundTileInstance),
                        metadata = tileData.customData
                    };
                    data.tiles.Add(tileSave);
                }
                
                if (tileData.HasObjects())
                {
                    foreach (var layer in tileData.objectLayers)
                    {
                        if (layer.instance == null)
                            continue;
                        
                        DecorationSaveData decoSave = new DecorationSaveData
                        {
                            q = hex.q,
                            r = hex.r,
                            prefabPath = GetPrefabPath(layer.prefab),
                            rotation = GetRotationFromInstance(layer.instance),
                            layerIndex = layer.layerIndex,
                            heightOffset = layer.heightOffset
                        };
                        
                        Vector3 worldPos = grid.HexToWorld(hex);
                        Vector3 offset = layer.instance.transform.position - worldPos;
                        decoSave.localOffset[0] = offset.x;
                        decoSave.localOffset[1] = offset.y;
                        decoSave.localOffset[2] = offset.z;
                        
                        data.decorations.Add(decoSave);
                    }
                }
            }
            
            return data;
        }
        
        private static string GetPrefabPath(GameObject prefab)
        {
            if (prefab == null)
                return "";
            
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetAssetPath(prefab);
#else
            return prefab.name;
#endif
        }
        
        private static int GetRotationFromInstance(GameObject instance)
        {
            if (instance == null)
                return 0;
            
            float yRotation = instance.transform.rotation.eulerAngles.y;
            return Mathf.RoundToInt(yRotation / 60f) % 6;
        }
        
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }
        
        public static HexMapSaveData FromJson(string json)
        {
            return JsonUtility.FromJson<HexMapSaveData>(json);
        }
    }
}
