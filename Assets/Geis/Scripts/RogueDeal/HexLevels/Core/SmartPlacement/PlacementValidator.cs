using UnityEngine;

namespace RogueDeal.HexLevels
{
    /// <summary>
    /// Validates tile and object placements based on rules.
    /// Prevents invalid placements (e.g., buildings on water).
    /// </summary>
    public static class PlacementValidator
    {
        /// <summary>
        /// Validation result for a placement.
        /// </summary>
        public class ValidationResult
        {
            public bool isValid;
            public string reason;

            public ValidationResult(bool valid, string reason = "")
            {
                this.isValid = valid;
                this.reason = reason;
            }

            public static ValidationResult Valid() => new ValidationResult(true);
            public static ValidationResult Invalid(string reason) => new ValidationResult(false, reason);
        }

        /// <summary>
        /// Validate placing a tile at a hex coordinate.
        /// </summary>
        public static ValidationResult ValidateTilePlacement(HexCoordinate hex, HexGrid grid, HexTileType tileType, GameObject prefab)
        {
            // Basic bounds check
            if (!grid.IsInBounds(hex))
            {
                return ValidationResult.Invalid("Hex is out of bounds");
            }

            // Check if hex already has a tile (allow replacement for now)
            // This could be made configurable later

            // Type-specific validation
            switch (tileType)
            {
                case HexTileType.Coast:
                    return ValidateCoastPlacement(hex, grid);
                case HexTileType.Road:
                    return ValidateRoadPlacement(hex, grid);
                case HexTileType.River:
                    return ValidateRiverPlacement(hex, grid);
                case HexTileType.Water:
                    return ValidateWaterPlacement(hex, grid);
                case HexTileType.Grass:
                    return ValidationResult.Valid(); // Grass can go anywhere
                default:
                    return ValidationResult.Valid(); // Default: allow
            }
        }

        /// <summary>
        /// Validate placing an object (building, unit, decoration) at a hex.
        /// </summary>
        public static ValidationResult ValidateObjectPlacement(HexCoordinate hex, HexGrid grid, GameObject prefab)
        {
            if (!grid.IsInBounds(hex))
            {
                return ValidationResult.Invalid("Hex is out of bounds");
            }

            // Check what tile is at this hex
            HexTileData tileData = grid.GetTile(hex);
            HexTileType tileType = tileData != null ? tileData.tileType : HexTileType.Grass;

            // Infer object type from prefab name
            string prefabName = prefab.name.ToLower();
            bool isBuilding = prefabName.Contains("building");
            bool isUnit = prefabName.Contains("unit");
            bool isDecoration = prefabName.Contains("tree") || prefabName.Contains("rock") || 
                               prefabName.Contains("mountain") || prefabName.Contains("prop");

            // Validation rules:
            // - Buildings should be on land (grass, coast, road)
            // - Units can be on land or coast
            // - Decorations can be on land
            // - Nothing should be placed on water or rivers (unless it's a boat/ship)

            if (tileType == HexTileType.Water || tileType == HexTileType.River)
            {
                // Allow ships/boats on water
                if (prefabName.Contains("ship") || prefabName.Contains("boat"))
                {
                    return ValidationResult.Valid();
                }
                
                if (isBuilding)
                    return ValidationResult.Invalid("Buildings cannot be placed on water");
                
                if (isDecoration)
                    return ValidationResult.Invalid("Decorations cannot be placed on water");
            }

            // Buildings need solid ground
            if (isBuilding && tileType != HexTileType.Grass && tileType != HexTileType.Coast && tileType != HexTileType.Road)
            {
                return ValidationResult.Invalid("Buildings require solid ground (grass, coast, or road)");
            }

            // Decorations need land
            if (isDecoration && tileType != HexTileType.Grass && tileType != HexTileType.Coast)
            {
                return ValidationResult.Invalid("Decorations require land (grass or coast)");
            }

            return ValidationResult.Valid();
        }

        private static ValidationResult ValidateCoastPlacement(HexCoordinate hex, HexGrid grid)
        {
            // Coast should be between land and water
            HexContextAnalyzer.HexContext context = HexContextAnalyzer.AnalyzeContext(hex, grid);
            
            if (!context.hasWaterNeighbor && !context.hasLandNeighbor)
            {
                return ValidationResult.Invalid("Coast tiles should be placed between land and water");
            }

            return ValidationResult.Valid();
        }

        private static ValidationResult ValidateRoadPlacement(HexCoordinate hex, HexGrid grid)
        {
            // Roads can be placed anywhere, but it's better if they connect to other roads
            // For now, allow placement anywhere
            return ValidationResult.Valid();
        }

        private static ValidationResult ValidateRiverPlacement(HexCoordinate hex, HexGrid grid)
        {
            // Rivers should generally connect to other rivers or water
            // But allow standalone rivers for now
            return ValidationResult.Valid();
        }

        private static ValidationResult ValidateWaterPlacement(HexCoordinate hex, HexGrid grid)
        {
            // Water can be placed anywhere
            // Could add rules about water needing to be connected, but for now allow it
            return ValidationResult.Valid();
        }
    }
}