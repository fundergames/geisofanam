using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using RogueDeal.HexLevels;

namespace RogueDeal.HexLevels.Editor
{
    /// <summary>
    /// Tool to help map connection patterns to specific tile variants.
    /// Allows testing and configuration of road/river/coast connection logic.
    /// </summary>
    public class ConnectionPatternMapper : EditorWindow
    {
        private HexGrid _testGrid;
        private HexCoordinate _testHex = new HexCoordinate(0, 0);
        private int _connectionPattern = 0; // Bitmask
        private string _selectedVariant = "A";
        private Dictionary<int, string> _patternMappings = new Dictionary<int, string>();
        
        // Road variants
        private string[] _roadVariants = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M" };
        private string[] _riverVariants = { "A", "A_curvy", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "crossing_A", "crossing_B" };
        
        private Vector2 _scrollPos;
        private HexTileType _currentTileType = HexTileType.Road;
        
        // Mappings asset
        private ConnectionPatternMappings _mappingsAsset;
        private const string DEFAULT_MAPPINGS_PATH = "Assets/RogueDeal/Resources/Data/HexLevels/ConnectionPatternMappings.asset";

        [MenuItem("Funder Games/Hex Levels/Connection Pattern Mapper")]
        public static void ShowWindow()
        {
            ConnectionPatternMapper window = GetWindow<ConnectionPatternMapper>("Connection Mapper");
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Connection Pattern Mapper", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool helps map connection patterns to tile variants.\n\n" +
                "1. Place test tiles around a hex\n" +
                "2. Select the center hex and set connection pattern\n" +
                "3. Test which variant looks correct\n" +
                "4. Save the mapping",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Find HexGrid
            _testGrid = (HexGrid)EditorGUILayout.ObjectField("Hex Grid", _testGrid, typeof(HexGrid), true);
            if (_testGrid == null)
            {
                _testGrid = Object.FindFirstObjectByType<HexGrid>();
            }

            if (_testGrid == null)
            {
                EditorGUILayout.HelpBox("No HexGrid found in scene!", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(5);

            // Mappings asset
            EditorGUILayout.LabelField("Mappings Asset", EditorStyles.boldLabel);
            _mappingsAsset = (ConnectionPatternMappings)EditorGUILayout.ObjectField("Mappings", _mappingsAsset, typeof(ConnectionPatternMappings), false);
            
            if (_mappingsAsset == null)
            {
                // Try to load default
                _mappingsAsset = AssetDatabase.LoadAssetAtPath<ConnectionPatternMappings>(DEFAULT_MAPPINGS_PATH);
                
                if (_mappingsAsset == null && GUILayout.Button("Create New Mappings Asset"))
                {
                    CreateMappingsAsset();
                }
            }
            
            if (_mappingsAsset != null)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Load from Asset"))
                {
                    LoadMappingsFromAsset();
                }
                if (GUILayout.Button("Save to Asset"))
                {
                    SaveMappingsToAsset();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);

            // Tile type selection
            _currentTileType = (HexTileType)EditorGUILayout.EnumPopup("Tile Type", _currentTileType);

            EditorGUILayout.Space(5);

            // Test hex coordinate
            EditorGUILayout.LabelField("Test Hex Coordinate", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _testHex = new HexCoordinate(
                EditorGUILayout.IntField("Q", _testHex.q),
                EditorGUILayout.IntField("R", _testHex.r)
            );
            if (GUILayout.Button("Use Selected Hex"))
            {
                // Try to get selected hex from editor tool
                HexLevelEditorTool tool = Object.FindFirstObjectByType<HexLevelEditorTool>();
                if (tool != null)
                {
                    var selected = tool.GetSelectedHex();
                    if (selected.HasValue)
                    {
                        _testHex = selected.Value;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Connection pattern editor
            EditorGUILayout.LabelField("Connection Pattern (6 neighbors)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Check which neighbors should have connections.\n" +
                "Directions: 0=East, 1=NE, 2=NW, 3=West, 4=SW, 5=SE",
                MessageType.None);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string[] directions = { "East (0)", "NE (1)", "NW (2)", "West (3)", "SW (4)", "SE (5)" };
            
            // Use a 2-row grid layout for better organization
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < 3; i++)
            {
                bool connected = (_connectionPattern & (1 << i)) != 0;
                EditorGUILayout.BeginHorizontal(GUILayout.Width(100));
                bool newConnected = EditorGUILayout.Toggle(connected, GUILayout.Width(15));
                EditorGUILayout.LabelField(directions[i], GUILayout.Width(70));
                EditorGUILayout.EndHorizontal();
                
                if (newConnected != connected)
                {
                    if (newConnected)
                        _connectionPattern |= (1 << i);
                    else
                        _connectionPattern &= ~(1 << i);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            for (int i = 3; i < 6; i++)
            {
                bool connected = (_connectionPattern & (1 << i)) != 0;
                EditorGUILayout.BeginHorizontal(GUILayout.Width(100));
                bool newConnected = EditorGUILayout.Toggle(connected, GUILayout.Width(15));
                EditorGUILayout.LabelField(directions[i], GUILayout.Width(70));
                EditorGUILayout.EndHorizontal();
                
                if (newConnected != connected)
                {
                    if (newConnected)
                        _connectionPattern |= (1 << i);
                    else
                        _connectionPattern &= ~(1 << i);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Pattern: {_connectionPattern} (binary: {System.Convert.ToString(_connectionPattern, 2).PadLeft(6, '0')})");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Variant selection
            EditorGUILayout.LabelField("Select Variant", EditorStyles.boldLabel);
            string[] variants = _currentTileType == HexTileType.Road ? _roadVariants : _riverVariants;
            
            int currentIndex = System.Array.IndexOf(variants, _selectedVariant);
            if (currentIndex < 0) currentIndex = 0;
            
            int newIndex = EditorGUILayout.Popup("Variant", currentIndex, variants);
            if (newIndex != currentIndex)
            {
                _selectedVariant = variants[newIndex];
            }

            EditorGUILayout.Space(10);

            // Test buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Place Test Neighbors"))
            {
                PlaceTestNeighbors();
            }
            if (GUILayout.Button("Place Center Tile"))
            {
                PlaceCenterTile();
            }
            if (GUILayout.Button("Clear Test Area"))
            {
                ClearTestArea();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Save mapping
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save This Mapping"))
            {
                SaveMapping();
            }
            if (GUILayout.Button("Test Smart Placement"))
            {
                TestSmartPlacement();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Show saved mappings
            EditorGUILayout.LabelField("Saved Mappings", EditorStyles.boldLabel);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));
            
            if (_patternMappings.Count == 0)
            {
                EditorGUILayout.HelpBox("No mappings saved yet. Test patterns and save them!", MessageType.Info);
            }
            else
            {
                foreach (var mapping in _patternMappings)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Pattern {mapping.Key} ({System.Convert.ToString(mapping.Key, 2).PadLeft(6, '0')})", GUILayout.Width(200));
                    EditorGUILayout.LabelField($"→ {mapping.Value}");
                    if (GUILayout.Button("Use", GUILayout.Width(50)))
                    {
                        _connectionPattern = mapping.Key;
                        _selectedVariant = mapping.Value;
                    }
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        _patternMappings.Remove(mapping.Key);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // Export/Import
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export Mappings"))
            {
                ExportMappings();
            }
            if (GUILayout.Button("Import Mappings"))
            {
                ImportMappings();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void PlaceTestNeighbors()
        {
            if (_testGrid == null) return;

            HexCoordinate[] neighbors = _testHex.GetNeighbors();
            string baseName = _currentTileType == HexTileType.Road ? "hex_road_A" : "hex_river_A";
            
            for (int i = 0; i < 6; i++)
            {
                bool shouldConnect = (_connectionPattern & (1 << i)) != 0;
                if (shouldConnect)
                {
                    HexCoordinate neighbor = neighbors[i];
                    Vector3 pos = _testGrid.HexToWorld(neighbor);
                    
                    // Find and place a base tile
                    string[] guids = AssetDatabase.FindAssets($"{baseName} t:Prefab");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null)
                        {
                            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                            instance.transform.position = pos;
                            instance.name = $"TestNeighbor_{i}";
                            
                            HexTileData tileData = new HexTileData(_currentTileType, prefab);
                            tileData.AddObject(instance);
                            _testGrid.SetTile(neighbor, tileData);
                            
                            Undo.RegisterCreatedObjectUndo(instance, "Place test neighbor");
                        }
                    }
                }
            }
            
            EditorUtility.SetDirty(_testGrid);
            SceneView.RepaintAll();
        }

        private void PlaceCenterTile()
        {
            if (_testGrid == null) return;

            Vector3 pos = _testGrid.HexToWorld(_testHex);
            string variantName = _currentTileType == HexTileType.Road ? 
                $"hex_road_{_selectedVariant}" : 
                $"hex_river_{_selectedVariant}";
            
            string[] guids = AssetDatabase.FindAssets($"{variantName} t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    // Remove existing
                    HexTileData existing = _testGrid.GetTile(_testHex);
                    if (existing != null)
                    {
                        // Delete ground tile
                        if (existing.groundTileInstance != null)
                        {
                            Undo.DestroyObjectImmediate(existing.groundTileInstance);
                        }
                        // Delete all object layers
                        if (existing.objectLayers != null)
                        {
                            foreach (var layer in existing.objectLayers)
                            {
                                if (layer.instance != null)
                                    Undo.DestroyObjectImmediate(layer.instance);
                            }
                        }
                    }
                    
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.transform.position = pos;
                    instance.name = $"TestCenter_{_selectedVariant}";
                    
                    HexTileData tileData = new HexTileData(_currentTileType, prefab);
                    tileData.groundTileInstance = instance;
                    _testGrid.SetTile(_testHex, tileData);
                    
                    Undo.RegisterCreatedObjectUndo(instance, "Place center tile");
                    EditorUtility.SetDirty(_testGrid);
                    SceneView.RepaintAll();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", $"Could not find prefab: {variantName}", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"Could not find prefab: {variantName}", "OK");
            }
        }

        private void ClearTestArea()
        {
            if (_testGrid == null) return;

            HexCoordinate[] neighbors = _testHex.GetNeighbors();
            List<HexCoordinate> toClear = new List<HexCoordinate> { _testHex };
            toClear.AddRange(neighbors);

            foreach (var hex in toClear)
            {
                HexTileData tileData = _testGrid.GetTile(hex);
                if (tileData != null)
                {
                    // Delete ground tile
                    if (tileData.groundTileInstance != null)
                    {
                        Undo.DestroyObjectImmediate(tileData.groundTileInstance);
                    }
                    // Delete all object layers
                    if (tileData.objectLayers != null)
                    {
                        foreach (var layer in tileData.objectLayers)
                        {
                            if (layer.instance != null)
                                Undo.DestroyObjectImmediate(layer.instance);
                        }
                    }
                    _testGrid.SetTile(hex, null);
                }
            }
            
            EditorUtility.SetDirty(_testGrid);
            SceneView.RepaintAll();
        }

        private void SaveMapping()
        {
            _patternMappings[_connectionPattern] = _selectedVariant;
            Debug.Log($"Saved mapping: Pattern {_connectionPattern} → {_selectedVariant}");
            
            // Also save to asset if available
            if (_mappingsAsset != null)
            {
                SaveMappingsToAsset();
            }
        }

        private void CreateMappingsAsset()
        {
            // Extract directory path (everything before the filename)
            // DEFAULT_MAPPINGS_PATH = "Assets/RogueDeal/Resources/Data/HexLevels/ConnectionPatternMappings.asset"
            // We need: "Assets/RogueDeal/Resources/Data/HexLevels"
            
            string assetPath = DEFAULT_MAPPINGS_PATH;
            int lastSlash = assetPath.LastIndexOf('/');
            if (lastSlash < 0)
            {
                EditorUtility.DisplayDialog("Error", "Invalid path format", "OK");
                return;
            }
            
            string directory = assetPath.Substring(0, lastSlash); // "Assets/RogueDeal/Resources/Data/HexLevels"
            
            // Create folder structure if needed
            if (!AssetDatabase.IsValidFolder(directory))
            {
                // Split path into folder components
                string[] folders = directory.Replace("Assets/", "").Split('/');
                string currentPath = "Assets";
                
                foreach (string folder in folders)
                {
                    if (string.IsNullOrEmpty(folder))
                        continue;
                        
                    string newPath = $"{currentPath}/{folder}";
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        string guid = AssetDatabase.CreateFolder(currentPath, folder);
                        if (string.IsNullOrEmpty(guid))
                        {
                            EditorUtility.DisplayDialog("Error", $"Failed to create folder: {newPath}", "OK");
                            return;
                        }
                        // Refresh to ensure folder is recognized
                        AssetDatabase.Refresh();
                    }
                    currentPath = newPath;
                }
            }
            
            // Verify directory exists now
            if (!AssetDatabase.IsValidFolder(directory))
            {
                EditorUtility.DisplayDialog("Error", $"Could not create or find directory: {directory}", "OK");
                return;
            }
            
            // Now create the asset
            _mappingsAsset = CreateInstance<ConnectionPatternMappings>();
            AssetDatabase.CreateAsset(_mappingsAsset, DEFAULT_MAPPINGS_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", $"Created mappings asset at:\n{DEFAULT_MAPPINGS_PATH}", "OK");
        }

        private void LoadMappingsFromAsset()
        {
            if (_mappingsAsset == null) return;
            
            _patternMappings.Clear();
            
            List<ConnectionPatternMappings.PatternMapping> mappings = null;
            switch (_currentTileType)
            {
                case HexTileType.Road:
                    mappings = _mappingsAsset.roadMappings;
                    break;
                case HexTileType.River:
                    mappings = _mappingsAsset.riverMappings;
                    break;
                case HexTileType.Coast:
                    mappings = _mappingsAsset.coastMappings;
                    break;
            }
            
            if (mappings != null)
            {
                foreach (var mapping in mappings)
                {
                    _patternMappings[mapping.pattern] = mapping.variant;
                }
                
                Debug.Log($"Loaded {_patternMappings.Count} mappings from asset");
                EditorUtility.DisplayDialog("Success", $"Loaded {_patternMappings.Count} mappings from asset", "OK");
            }
        }

        private void SaveMappingsToAsset()
        {
            if (_mappingsAsset == null) return;
            
            List<ConnectionPatternMappings.PatternMapping> mappings = null;
            switch (_currentTileType)
            {
                case HexTileType.Road:
                    mappings = _mappingsAsset.roadMappings;
                    break;
                case HexTileType.River:
                    mappings = _mappingsAsset.riverMappings;
                    break;
                case HexTileType.Coast:
                    mappings = _mappingsAsset.coastMappings;
                    break;
            }
            
            if (mappings != null)
            {
                mappings.Clear();
                foreach (var kvp in _patternMappings)
                {
                    mappings.Add(new ConnectionPatternMappings.PatternMapping 
                    { 
                        pattern = kvp.Key, 
                        variant = kvp.Value 
                    });
                }
                
                EditorUtility.SetDirty(_mappingsAsset);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"Saved {_patternMappings.Count} mappings to asset");
            }
        }

        private void TestSmartPlacement()
        {
            if (_testGrid == null) return;

            GameObject smartPrefab = SmartTileSelector.SelectTilePrefab(_testHex, _testGrid, _currentTileType, null);
            if (smartPrefab != null)
            {
                EditorUtility.DisplayDialog("Smart Placement Result", 
                    $"Smart placement selected: {smartPrefab.name}\n\n" +
                    $"Current pattern: {_connectionPattern}\n" +
                    $"Your selection: {_selectedVariant}",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Smart Placement Result", 
                    "Smart placement returned null (no prefab found)", 
                    "OK");
            }
        }

        private void ExportMappings()
        {
            // Save to asset first
            if (_mappingsAsset != null)
            {
                SaveMappingsToAsset();
            }
            
            // Also export to JSON for backup
            string json = JsonUtility.ToJson(new SerializableDictionary(_patternMappings), true);
            string path = EditorUtility.SaveFilePanel("Export Mappings (JSON)", "", "connection_mappings", "json");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, json);
                EditorUtility.DisplayDialog("Success", "Mappings exported to JSON!", "OK");
            }
        }

        private void ImportMappings()
        {
            string path = EditorUtility.OpenFilePanel("Import Mappings (JSON)", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                string json = System.IO.File.ReadAllText(path);
                SerializableDictionary dict = JsonUtility.FromJson<SerializableDictionary>(json);
                _patternMappings = dict.ToDictionary();
                
                // Also save to asset if available
                if (_mappingsAsset != null)
                {
                    SaveMappingsToAsset();
                }
                
                EditorUtility.DisplayDialog("Success", $"Imported {_patternMappings.Count} mappings from JSON!", "OK");
            }
        }

        [System.Serializable]
        private class SerializableDictionary
        {
            public List<int> keys = new List<int>();
            public List<string> values = new List<string>();

            public SerializableDictionary() { }
            public SerializableDictionary(Dictionary<int, string> dict)
            {
                foreach (var kvp in dict)
                {
                    keys.Add(kvp.Key);
                    values.Add(kvp.Value);
                }
            }

            public Dictionary<int, string> ToDictionary()
            {
                Dictionary<int, string> dict = new Dictionary<int, string>();
                for (int i = 0; i < keys.Count && i < values.Count; i++)
                {
                    dict[keys[i]] = values[i];
                }
                return dict;
            }
        }
    }
}