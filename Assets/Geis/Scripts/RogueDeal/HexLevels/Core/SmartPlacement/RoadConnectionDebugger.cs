using UnityEngine;

namespace RogueDeal.HexLevels
{
    public static class RoadConnectionDebugger
    {
        public static void LogRoadConnection(HexCoordinate hex, HexGrid grid, int rotation = 0)
        {
            int roadBitmask = HexContextAnalyzer.GetNeighborBitmask(hex, grid, HexTileType.Road);
            int roadCount = HexContextAnalyzer.GetNeighborCount(hex, grid, HexTileType.Road);
            
            string connections = GetConnectionString(roadBitmask);
            string binary = System.Convert.ToString(roadBitmask, 2).PadLeft(6, '0');
            
            Debug.Log($"Road at {hex}:\n" +
                     $"  Connections: {connections}\n" +
                     $"  Bitmask: {roadBitmask} (0b{binary})\n" +
                     $"  Count: {roadCount}\n" +
                     $"  Rotation: {rotation}");
        }
        
        public static string GetConnectionString(int bitmask)
        {
            string[] directions = { "E", "NE", "NW", "W", "SW", "SE" };
            string result = "[";
            
            for (int i = 0; i < 6; i++)
            {
                if ((bitmask & (1 << i)) != 0)
                {
                    if (result.Length > 1) result += ", ";
                    result += directions[i];
                }
            }
            
            result += "]";
            return result;
        }
        
        public static string GetPatternVisualization(int bitmask)
        {
            string[] grid = new string[7];
            grid[0] = "    [NW]      [NE]    ";
            grid[1] = "       \\      /       ";
            grid[2] = "        \\    /        ";
            grid[3] = " [W] ---[HEX]--- [E] ";
            grid[4] = "        /    \\        ";
            grid[5] = "       /      \\       ";
            grid[6] = "    [SW]      [SE]    ";
            
            string[] directions = { "E", "NE", "NW", "W", "SW", "SE" };
            string[] patterns = { "[E]", "[NE]", "[NW]", "[W]", "[SW]", "[SE]" };
            
            string result = "\n";
            foreach (var line in grid)
            {
                string modifiedLine = line;
                for (int i = 0; i < 6; i++)
                {
                    if ((bitmask & (1 << i)) != 0)
                    {
                        modifiedLine = modifiedLine.Replace(patterns[i], $"<b>{patterns[i]}</b>");
                    }
                }
                result += modifiedLine + "\n";
            }
            
            return result;
        }
        
        public static void LogExpectedVariant(int bitmask, string expectedVariant)
        {
            string binary = System.Convert.ToString(bitmask, 2).PadLeft(6, '0');
            string connections = GetConnectionString(bitmask);
            
            Debug.Log($"Pattern {bitmask} (0b{binary}) -> Variant {expectedVariant}\n" +
                     $"  Connections: {connections}" +
                     GetPatternVisualization(bitmask));
        }
    }
}
