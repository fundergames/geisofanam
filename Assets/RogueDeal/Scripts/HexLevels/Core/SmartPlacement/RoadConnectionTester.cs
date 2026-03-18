using UnityEngine;
using System.Collections.Generic;

namespace RogueDeal.HexLevels
{
    public class RoadConnectionTester : MonoBehaviour
    {
        [Header("Test Settings")]
        public HexGrid testGrid;
        public ConnectionPatternMappings mappings;
        
        [Header("Test Results")]
        [TextArea(20, 30)]
        public string testOutput;
        
        [ContextMenu("Test All Road Patterns")]
        public void TestAllRoadPatterns()
        {
            if (mappings == null)
            {
                testOutput = "ERROR: No ConnectionPatternMappings assigned!";
                return;
            }
            
            string output = "=== ROAD PATTERN MAPPINGS ===\n\n";
            output += "Total mappings: " + mappings.roadMappings.Count + "\n\n";
            
            Dictionary<string, List<int>> variantToPatterns = new Dictionary<string, List<int>>();
            
            foreach (var mapping in mappings.roadMappings)
            {
                if (!variantToPatterns.ContainsKey(mapping.variant))
                {
                    variantToPatterns[mapping.variant] = new List<int>();
                }
                variantToPatterns[mapping.variant].Add(mapping.pattern);
            }
            
            foreach (var kvp in variantToPatterns)
            {
                output += $"Variant {kvp.Key}:\n";
                foreach (int pattern in kvp.Value)
                {
                    string binary = System.Convert.ToString(pattern, 2).PadLeft(6, '0');
                    string connections = RoadConnectionDebugger.GetConnectionString(pattern);
                    output += $"  Pattern {pattern,2} (0b{binary}) {connections}\n";
                }
                output += "\n";
            }
            
            testOutput = output;
            Debug.Log(testOutput);
        }
        
        [ContextMenu("Test Common Road Patterns")]
        public void TestCommonPatterns()
        {
            string output = "=== COMMON ROAD PATTERNS ===\n\n";
            
            output += "STRAIGHT ROADS (2 opposite connections):\n";
            output += FormatPattern(9, "East-West (0b001001)");
            output += FormatPattern(18, "NE-SW (0b010010)");
            output += FormatPattern(36, "NW-SE (0b100100)");
            output += "\n";
            
            output += "CURVES (2 adjacent connections):\n";
            output += FormatPattern(3, "E-NE (0b000011)");
            output += FormatPattern(6, "NE-NW (0b000110)");
            output += FormatPattern(12, "NW-W (0b001100)");
            output += FormatPattern(24, "W-SW (0b011000)");
            output += FormatPattern(48, "SW-SE (0b110000)");
            output += FormatPattern(33, "SE-E (0b100001)");
            output += "\n";
            
            output += "T-JUNCTIONS (3 connections):\n";
            output += FormatPattern(7, "E-NE-NW (0b000111)");
            output += FormatPattern(14, "NE-NW-W (0b001110)");
            output += FormatPattern(28, "NW-W-SW (0b011100)");
            output += FormatPattern(56, "W-SW-SE (0b111000)");
            output += FormatPattern(49, "SW-SE-E (0b110001)");
            output += FormatPattern(35, "SE-E-NE (0b100011)");
            output += "\n";
            
            output += "4-WAY INTERSECTIONS:\n";
            output += FormatPattern(63, "All 6 (0b111111)");
            output += FormatPattern(15, "E-NE-NW-W (0b001111)");
            output += FormatPattern(30, "NE-NW-W-SW (0b011110)");
            output += FormatPattern(60, "NW-W-SW-SE (0b111100)");
            output += "\n";
            
            testOutput = output;
            Debug.Log(testOutput);
        }
        
        private string FormatPattern(int pattern, string description)
        {
            string variant = mappings != null ? mappings.GetRoadVariant(pattern) : "?";
            string connections = RoadConnectionDebugger.GetConnectionString(pattern);
            return $"  {description}: Variant {variant} {connections}\n";
        }
        
        [ContextMenu("Diagnose Current Grid")]
        public void DiagnoseGrid()
        {
            if (testGrid == null)
            {
                testOutput = "ERROR: No HexGrid assigned!";
                return;
            }
            
            string output = "=== GRID DIAGNOSIS ===\n\n";
            
            var tiles = testGrid.GetAllTiles();
            int roadCount = 0;
            
            foreach (var kvp in tiles)
            {
                if (kvp.Value.tileType == HexTileType.Road)
                {
                    roadCount++;
                    HexCoordinate hex = kvp.Key;
                    int bitmask = HexContextAnalyzer.GetNeighborBitmask(hex, testGrid, HexTileType.Road);
                    int count = HexContextAnalyzer.GetNeighborCount(hex, testGrid, HexTileType.Road);
                    
                    string binary = System.Convert.ToString(bitmask, 2).PadLeft(6, '0');
                    string connections = RoadConnectionDebugger.GetConnectionString(bitmask);
                    string prefabName = kvp.Value.groundTilePrefab != null ? kvp.Value.groundTilePrefab.name : "null";
                    string expectedVariant = mappings != null ? mappings.GetRoadVariant(bitmask) : "?";
                    
                    output += $"Road at {hex}:\n";
                    output += $"  Prefab: {prefabName}\n";
                    output += $"  Pattern: {bitmask} (0b{binary})\n";
                    output += $"  Connections: {connections} ({count})\n";
                    output += $"  Expected variant: {expectedVariant}\n";
                    
                    if (kvp.Value.groundTileInstance != null)
                    {
                        float rotation = kvp.Value.groundTileInstance.transform.rotation.eulerAngles.y;
                        int rotIndex = Mathf.RoundToInt(rotation / 60f) % 6;
                        output += $"  Rotation: {rotation}° (index {rotIndex})\n";
                    }
                    
                    output += "\n";
                }
            }
            
            output += $"Total roads: {roadCount}\n";
            
            testOutput = output;
            Debug.Log(testOutput);
        }
    }
}
