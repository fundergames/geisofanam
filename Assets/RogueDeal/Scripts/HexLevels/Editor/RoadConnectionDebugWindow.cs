using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RogueDeal.HexLevels
{
    public class RoadConnectionDebugWindow : EditorWindow
    {
        private HexGrid selectedGrid;
        private ConnectionPatternMappings mappings;
        private Vector2 scrollPos;
        
        [MenuItem("Tools/Hex Levels/Road Connection Debugger")]
        public static void ShowWindow()
        {
            GetWindow<RoadConnectionDebugWindow>("Road Debug");
        }
        
        private void OnEnable()
        {
            selectedGrid = FindObjectOfType<HexGrid>();
            
            string[] guids = AssetDatabase.FindAssets("t:ConnectionPatternMappings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                mappings = AssetDatabase.LoadAssetAtPath<ConnectionPatternMappings>(path);
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Road Connection Debugger", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            selectedGrid = (HexGrid)EditorGUILayout.ObjectField("Hex Grid", selectedGrid, typeof(HexGrid), true);
            mappings = (ConnectionPatternMappings)EditorGUILayout.ObjectField("Mappings", mappings, typeof(ConnectionPatternMappings), false);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Refresh Grid"))
            {
                selectedGrid = FindObjectOfType<HexGrid>();
                Repaint();
            }
            
            if (selectedGrid == null)
            {
                EditorGUILayout.HelpBox("No HexGrid found in scene!", MessageType.Warning);
                return;
            }
            
            if (mappings == null)
            {
                EditorGUILayout.HelpBox("No ConnectionPatternMappings found!", MessageType.Warning);
                return;
            }
            
            EditorGUILayout.Space();
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            DrawRoadTiles();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawRoadTiles()
        {
            var tiles = selectedGrid.GetAllTiles();
            int roadCount = 0;
            
            foreach (var kvp in tiles)
            {
                if (kvp.Value.tileType == HexTileType.Road)
                {
                    roadCount++;
                    DrawRoadTile(kvp.Key, kvp.Value);
                }
            }
            
            if (roadCount == 0)
            {
                EditorGUILayout.HelpBox("No roads in grid yet. Place some roads to see debug info!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Total Roads: {roadCount}", EditorStyles.boldLabel);
            }
        }
        
        private void DrawRoadTile(HexCoordinate hex, HexTileData data)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"Road at {hex}", EditorStyles.boldLabel);
            
            int bitmask = HexContextAnalyzer.GetNeighborBitmask(hex, selectedGrid, HexTileType.Road);
            int count = HexContextAnalyzer.GetNeighborCount(hex, selectedGrid, HexTileType.Road);
            
            string binary = System.Convert.ToString(bitmask, 2).PadLeft(6, '0');
            string connections = RoadConnectionDebugger.GetConnectionString(bitmask);
            
            EditorGUILayout.LabelField($"Connections: {connections} ({count})");
            EditorGUILayout.LabelField($"Pattern: {bitmask} (0b{binary})");
            
            string expectedVariant = mappings.GetRoadVariant(bitmask);
            string currentPrefab = data.groundTilePrefab != null ? data.groundTilePrefab.name : "null";
            
            EditorGUILayout.LabelField($"Expected: hex_road_{expectedVariant}");
            
            GUIStyle prefabStyle = new GUIStyle(EditorStyles.label);
            if (currentPrefab.Contains(expectedVariant))
            {
                prefabStyle.normal.textColor = Color.green;
            }
            else
            {
                prefabStyle.normal.textColor = Color.red;
            }
            EditorGUILayout.LabelField($"Current: {currentPrefab}", prefabStyle);
            
            if (data.groundTileInstance != null)
            {
                float rotation = data.groundTileInstance.transform.rotation.eulerAngles.y;
                int rotIndex = Mathf.RoundToInt(rotation / 60f) % 6;
                EditorGUILayout.LabelField($"Rotation: {rotation:F1}° (index {rotIndex})");
                
                if (GUILayout.Button("Select in Scene"))
                {
                    Selection.activeGameObject = data.groundTileInstance;
                    SceneView.FrameLastActiveSceneView();
                }
            }
            
            DrawConnectionDiagram(bitmask);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        private void DrawConnectionDiagram(int bitmask)
        {
            EditorGUILayout.LabelField("Connection Diagram:");
            
            string[] directions = { "E", "NE", "NW", "W", "SW", "SE" };
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            for (int i = 0; i < 6; i++)
            {
                bool connected = (bitmask & (1 << i)) != 0;
                GUIStyle style = new GUIStyle(GUI.skin.box);
                style.normal.textColor = connected ? Color.green : Color.gray;
                style.fontStyle = connected ? FontStyle.Bold : FontStyle.Normal;
                
                GUILayout.Label(directions[i], style, GUILayout.Width(30));
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}
