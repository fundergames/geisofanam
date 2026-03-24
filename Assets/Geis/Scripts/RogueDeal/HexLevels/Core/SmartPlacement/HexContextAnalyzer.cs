using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.HexLevels
{
    /// <summary>
    /// Analyzes the context around a hex coordinate to determine appropriate tile placement.
    /// Detects neighboring tile types and provides context for smart placement.
    /// </summary>
    public static class HexContextAnalyzer
    {
        /// <summary>
        /// Context information about a hex's neighbors.
        /// </summary>
        public class HexContext
        {
            public HexCoordinate hex;
            public Dictionary<HexTileType, int> neighborCounts = new Dictionary<HexTileType, int>();
            public Dictionary<int, HexTileType> neighborTypes = new Dictionary<int, HexTileType>(); // Direction index -> type
            public bool hasWaterNeighbor = false;
            public bool hasLandNeighbor = false;
            public bool hasRoadNeighbor = false;
            public bool hasRiverNeighbor = false;
            public bool hasCoastNeighbor = false;
            public int waterNeighborCount = 0;
            public int landNeighborCount = 0;
            public int roadNeighborCount = 0;
            public int riverNeighborCount = 0;
            public int coastNeighborCount = 0;

            public HexContext(HexCoordinate hex)
            {
                this.hex = hex;
            }
        }

        /// <summary>
        /// Analyze the context around a hex coordinate.
        /// </summary>
        public static HexContext AnalyzeContext(HexCoordinate hex, HexGrid grid)
        {
            HexContext context = new HexContext(hex);

            // Get all 6 neighbors
            HexCoordinate[] neighbors = hex.GetNeighbors();

            for (int i = 0; i < neighbors.Length; i++)
            {
                HexCoordinate neighbor = neighbors[i];
                HexTileData neighborData = grid.GetTile(neighbor);

                if (neighborData != null)
                {
                    HexTileType neighborType = neighborData.tileType;
                    
                    // Count by type
                    if (!context.neighborCounts.ContainsKey(neighborType))
                        context.neighborCounts[neighborType] = 0;
                    context.neighborCounts[neighborType]++;

                    // Store direction-specific type
                    context.neighborTypes[i] = neighborType;

                    // Update flags
                    switch (neighborType)
                    {
                        case HexTileType.Water:
                            context.hasWaterNeighbor = true;
                            context.waterNeighborCount++;
                            break;
                        case HexTileType.Grass:
                            context.hasLandNeighbor = true;
                            context.landNeighborCount++;
                            break;
                        case HexTileType.Road:
                            context.hasRoadNeighbor = true;
                            context.roadNeighborCount++;
                            break;
                        case HexTileType.River:
                            context.hasRiverNeighbor = true;
                            context.riverNeighborCount++;
                            break;
                        case HexTileType.Coast:
                            context.hasCoastNeighbor = true;
                            context.coastNeighborCount++;
                            break;
                    }
                }
                else
                {
                    // Empty neighbor - treat as land for some purposes
                    context.hasLandNeighbor = true;
                    context.landNeighborCount++;
                }
            }

            return context;
        }

        /// <summary>
        /// Get a bitmask representing which neighbors have a specific tile type.
        /// Each bit represents a direction (0-5).
        /// </summary>
        public static int GetNeighborBitmask(HexCoordinate hex, HexGrid grid, HexTileType type)
        {
            int bitmask = 0;
            HexCoordinate[] neighbors = hex.GetNeighbors();

            for (int i = 0; i < neighbors.Length; i++)
            {
                HexTileData neighborData = grid.GetTile(neighbors[i]);
                if (neighborData != null && neighborData.tileType == type)
                {
                    bitmask |= (1 << i);
                }
            }

            return bitmask;
        }

        /// <summary>
        /// Get the number of neighbors with a specific tile type.
        /// </summary>
        public static int GetNeighborCount(HexCoordinate hex, HexGrid grid, HexTileType type)
        {
            int count = 0;
            HexCoordinate[] neighbors = hex.GetNeighbors();

            foreach (var neighbor in neighbors)
            {
                HexTileData neighborData = grid.GetTile(neighbor);
                if (neighborData != null && neighborData.tileType == type)
                {
                    count++;
                }
            }

            return count;
        }
        
        /// <summary>
        /// Get the rotation (in 60° steps, 0-5) of a neighbor tile in a specific direction.
        /// Returns -1 if no neighbor exists or no ground tile instance.
        /// Direction: 0=E, 1=NE, 2=NW, 3=W, 4=SW, 5=SE
        /// </summary>
        public static int GetNeighborRotation(HexCoordinate hex, HexGrid grid, int direction)
        {
            if (direction < 0 || direction >= 6) return -1;
            
            HexCoordinate[] neighbors = hex.GetNeighbors();
            HexTileData neighborData = grid.GetTile(neighbors[direction]);
            
            if (neighborData != null && neighborData.groundTileInstance != null)
            {
                float yRotation = neighborData.groundTileInstance.transform.eulerAngles.y;
                // Normalize to 0-359
                yRotation = (yRotation % 360 + 360) % 360;
                // Convert to hex rotation steps (0-5)
                int rotationStep = Mathf.RoundToInt(yRotation / 60f) % 6;
                return rotationStep;
            }
            
            return -1;
        }

        /// <summary>
        /// Check if a hex should be a coast tile based on neighbors.
        /// Coast tiles are land tiles adjacent to water.
        /// </summary>
        public static bool ShouldBeCoast(HexCoordinate hex, HexGrid grid)
        {
            HexContext context = AnalyzeContext(hex, grid);
            return context.hasWaterNeighbor && context.hasLandNeighbor;
        }

        /// <summary>
        /// Check if a hex should connect to roads/rivers based on neighbors.
        /// </summary>
        public static bool ShouldConnect(HexCoordinate hex, HexGrid grid, HexTileType connectionType)
        {
            return GetNeighborCount(hex, grid, connectionType) > 0;
        }
    }
}