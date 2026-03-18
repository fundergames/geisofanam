using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace RogueDeal.HexLevels.Editor
{
    public class HexEditorWindow : EditorWindow
    {
        [MenuItem("Window/Hex Editor (Enhanced)")]
        public static void ShowWindow()
        {
            HexEditorWindow wnd = GetWindow<HexEditorWindow>();
            wnd.titleContent = new GUIContent("Hex Editor");
            wnd.minSize = new Vector2(1200, 700);
        }
        
        private HexLevelEditorTool editorTool;
        private HexGrid hexGrid;
        private AssetCatalog assetCatalog;
        
        private Vector2 leftScrollPos;
        private Vector2 assetScrollPos;
        private Vector2 inspectorScrollPos;
        
        private string searchFilter = "";
        private AssetCategory selectedCategory = AssetCategory.All;
        private string lastSavedTime = "Never";
        private bool autosave = false;
        
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle selectedButtonStyle;
        private GUIStyle panelStyle;
        
        private void OnEnable()
        {
            FindReferences();
        }
        
        private void FindReferences()
        {
            editorTool = FindObjectOfType<HexLevelEditorTool>();
            hexGrid = FindObjectOfType<HexGrid>();
            
            if (editorTool == null && hexGrid != null)
            {
                editorTool = hexGrid.gameObject.AddComponent<HexLevelEditorTool>();
                editorTool.targetGrid = hexGrid;
            }
            
            assetCatalog = FindOrCreateAssetCatalog();
        }
        
        private AssetCatalog FindOrCreateAssetCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:AssetCatalog");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AssetCatalog>(path);
            }
            
            string catalogPath = "Assets/RogueDeal/Resources/Data/HexLevels";
            if (!Directory.Exists(catalogPath))
            {
                Directory.CreateDirectory(catalogPath);
            }
            
            AssetCatalog catalog = CreateInstance<AssetCatalog>();
            catalog.AutoPopulateFromPrefabBrowser();
            AssetDatabase.CreateAsset(catalog, $"{catalogPath}/AssetCatalog.asset");
            AssetDatabase.SaveAssets();
            return catalog;
        }
        
        private void InitializeStyles()
        {
            if (EditorStyles.boldLabel == null)
                return;
            
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    margin = new RectOffset(0, 0, 10, 5)
                };
            }
            
            if (panelStyle == null && GUI.skin != null)
            {
                panelStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(5, 5, 5, 5)
                };
            }
        }
        
        private void OnGUI()
        {
            if (headerStyle == null)
                InitializeStyles();
            
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            
            DrawLeftPanel();
            DrawCenterPanel();
            DrawRightPanel();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(panelStyle, GUILayout.Width(200), GUILayout.ExpandHeight(true));
            
            GUILayout.Label("File", headerStyle);
            
            if (GUILayout.Button("New", GUILayout.Height(30)))
            {
                OnNewMap();
            }
            
            if (GUILayout.Button("Load...", GUILayout.Height(30)))
            {
                OnLoadMap();
            }
            
            if (GUILayout.Button("Save", GUILayout.Height(30)))
            {
                OnSaveMap();
            }
            
            if (GUILayout.Button("Save As...", GUILayout.Height(30)))
            {
                OnSaveAsMap();
            }
            
            EditorGUILayout.Space(10);
            
            autosave = EditorGUILayout.Toggle("Autosave", autosave);
            
            if (!string.IsNullOrEmpty(lastSavedTime))
            {
                EditorGUILayout.LabelField("Last saved:", lastSavedTime, EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCenterPanel()
        {
            EditorGUILayout.BeginVertical(panelStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            DrawStatusBar();
            DrawModeSelector();
            DrawLayerSelector();
            DrawElevationControls();
            DrawBrushControls();
            
            EditorGUILayout.Space(10);
            
            DrawAssetBrowser();
            
            EditorGUILayout.Space(10);
            
            DrawInspector();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(panelStyle, GUILayout.Width(200), GUILayout.ExpandHeight(true));
            
            GUILayout.Label("Global Toggles", headerStyle);
            
            if (editorTool != null)
            {
                bool gridVisible = EditorGUILayout.Toggle("Grid Visibility", true);
                bool collisionSnap = EditorGUILayout.Toggle("Collision Snap", true);
                bool gizmoVisible = EditorGUILayout.Toggle("Gizmos", true);
                
                editorTool.showPreview = EditorGUILayout.Toggle("Show Preview", editorTool.showPreview);
            }
            
            EditorGUILayout.Space(10);
            GUILayout.Label("Settings", headerStyle);
            
            if (editorTool != null)
            {
                EditorGUI.BeginChangeCheck();
                int brushSize = EditorGUILayout.IntSlider("Brush Size", 1, 1, 3);
                if (EditorGUI.EndChangeCheck())
                {
                }
                
                bool dragPaint = EditorGUILayout.Toggle("Drag Paint", true);
                bool followCursor = EditorGUILayout.Toggle("Follow Cursor", true);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStatusBar()
        {
            GUILayout.Label("Context / Status", headerStyle);
            
            if (editorTool != null && hexGrid != null)
            {
                EditorGUILayout.LabelField("Current Set:", editorTool.placementLayerMode.ToString());
                EditorGUILayout.LabelField("Current Layer:", editorTool.placementLayerMode == PlacementLayerMode.Ground ? "Tile" : "Decoration");
                EditorGUILayout.LabelField("Current Mode:", editorTool.toolMode.ToString());
                
                HexCoordinate? hovered = editorTool.GetHoveredHex();
                if (hovered.HasValue)
                {
                    HexTileData data = hexGrid.GetTile(hovered.Value);
                    string info = $"Hex ({hovered.Value.q},{hovered.Value.r})";
                    
                    if (data != null)
                    {
                        if (data.HasGroundTile())
                        {
                            info += $" | Tile={data.tileType}";
                        }
                        if (data.HasObjects())
                        {
                            info += $" | Items={data.ObjectLayerCount}";
                        }
                        info += $" | Elev={data.elevation}";
                    }
                    
                    EditorGUILayout.HelpBox(info, MessageType.Info);
                }
            }
            
            EditorGUILayout.Space(5);
        }
        
        private void DrawModeSelector()
        {
            if (editorTool == null)
                return;
            
            GUILayout.Label("Mode (Hotkeys: 1/2/3)", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            Color originalColor = GUI.backgroundColor;
            
            GUI.backgroundColor = editorTool.toolMode == EditorToolMode.Place ? Color.yellow : Color.white;
            if (GUILayout.Button("Place", GUILayout.Height(35)))
            {
                editorTool.toolMode = EditorToolMode.Place;
            }
            
            GUI.backgroundColor = editorTool.toolMode == EditorToolMode.Delete ? Color.yellow : Color.white;
            if (GUILayout.Button("Erase", GUILayout.Height(35)))
            {
                editorTool.toolMode = EditorToolMode.Delete;
            }
            
            GUI.backgroundColor = editorTool.toolMode == EditorToolMode.Select ? Color.yellow : Color.white;
            if (GUILayout.Button("Edit", GUILayout.Height(35)))
            {
                editorTool.toolMode = EditorToolMode.Select;
            }
            
            GUI.backgroundColor = originalColor;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField("Esc = Cancel operation", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
        }
        
        private void DrawLayerSelector()
        {
            if (editorTool == null)
                return;
            
            GUILayout.Label("Layer (Hotkey: Tab)", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            Color originalColor = GUI.backgroundColor;
            
            GUI.backgroundColor = editorTool.placementLayerMode == PlacementLayerMode.Ground ? Color.yellow : Color.white;
            if (GUILayout.Button("Tiles", GUILayout.Height(30)))
            {
                editorTool.placementLayerMode = PlacementLayerMode.Ground;
            }
            
            GUI.backgroundColor = editorTool.placementLayerMode == PlacementLayerMode.Object ? Color.yellow : Color.white;
            if (GUILayout.Button("Decorations", GUILayout.Height(30)))
            {
                editorTool.placementLayerMode = PlacementLayerMode.Object;
            }
            
            GUI.backgroundColor = originalColor;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
        
        private void DrawElevationControls()
        {
            if (editorTool == null)
                return;
            
            GUILayout.Label("Elevation (Hotkeys: [ / ])", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("-", GUILayout.Width(30)))
            {
            }
            
            EditorGUILayout.LabelField("0", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(40));
            
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
        
        private void DrawBrushControls()
        {
            if (editorTool == null || editorTool.toolMode != EditorToolMode.Place)
                return;
            
            GUILayout.Label("Brush", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            Color originalColor = GUI.backgroundColor;
            
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("1", GUILayout.Height(25)))
            {
            }
            
            if (GUILayout.Button("2", GUILayout.Height(25)))
            {
            }
            
            if (GUILayout.Button("3", GUILayout.Height(25)))
            {
            }
            
            GUI.backgroundColor = originalColor;
            
            EditorGUILayout.EndHorizontal();
            
            bool dragPaint = EditorGUILayout.Toggle("Drag Paint", true);
            EditorGUILayout.Space(5);
        }
        
        private void DrawAssetBrowser()
        {
            GUILayout.Label("Asset Browser (Hotkeys: Q/E cycle, F favorite)", headerStyle);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchFilter = "";
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Category:", GUILayout.Width(70));
            selectedCategory = (AssetCategory)EditorGUILayout.EnumPopup(selectedCategory);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (assetCatalog != null)
            {
                assetScrollPos = EditorGUILayout.BeginScrollView(assetScrollPos, GUILayout.Height(250));
                
                var entries = GetFilteredAssets();
                
                foreach (var entry in entries)
                {
                    DrawAssetCard(entry);
                }
                
                if (entries.Count == 0)
                {
                    EditorGUILayout.HelpBox("No assets found. Try adjusting the search filter or category.", MessageType.Info);
                }
                
                EditorGUILayout.EndScrollView();
            }
        }
        
        private System.Collections.Generic.List<AssetCatalog.AssetEntry> GetFilteredAssets()
        {
            if (assetCatalog == null)
                return new System.Collections.Generic.List<AssetCatalog.AssetEntry>();
            
            var entries = assetCatalog.entries;
            
            if (!string.IsNullOrEmpty(searchFilter))
            {
                entries = assetCatalog.Search(searchFilter);
            }
            else if (selectedCategory != AssetCategory.All)
            {
                entries = assetCatalog.GetEntriesByCategory(selectedCategory);
            }
            
            return entries;
        }
        
        private void DrawAssetCard(AssetCatalog.AssetEntry entry)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            
            Texture2D preview = AssetPreview.GetAssetPreview(entry.prefab);
            if (preview != null)
            {
                GUILayout.Label(preview, GUILayout.Width(40), GUILayout.Height(40));
            }
            else
            {
                GUILayout.Box("", GUILayout.Width(40), GUILayout.Height(40));
            }
            
            EditorGUILayout.BeginVertical();
            
            Color originalColor = GUI.backgroundColor;
            if (editorTool != null && editorTool.selectedPrefab == entry.prefab)
            {
                GUI.backgroundColor = Color.yellow;
            }
            
            if (GUILayout.Button(entry.displayName, GUILayout.Height(20)))
            {
                if (editorTool != null)
                {
                    editorTool.selectedPrefab = entry.prefab;
                    EditorUtility.SetDirty(editorTool);
                }
            }
            
            GUI.backgroundColor = originalColor;
            
            if (entry.tags != null && entry.tags.Length > 0)
            {
                EditorGUILayout.LabelField(string.Join(", ", entry.tags), EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button(entry.isFavorite ? "★" : "☆", GUILayout.Width(30), GUILayout.Height(40)))
            {
                assetCatalog.ToggleFavorite(entry.id);
                EditorUtility.SetDirty(assetCatalog);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawInspector()
        {
            GUILayout.Label("Inspector / Preview (Hotkeys: R rotate)", headerStyle);
            
            if (editorTool != null && editorTool.selectedPrefab != null)
            {
                inspectorScrollPos = EditorGUILayout.BeginScrollView(inspectorScrollPos, GUILayout.Height(150));
                
                EditorGUILayout.LabelField("Selected Asset", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(editorTool.selectedPrefab.name);
                
                Texture2D preview = AssetPreview.GetAssetPreview(editorTool.selectedPrefab);
                if (preview != null)
                {
                    GUILayout.Label(preview, GUILayout.Width(100), GUILayout.Height(100));
                }
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("⟲", GUILayout.Width(30)))
                {
                    editorTool.rotation = (editorTool.rotation - 1 + 6) % 6;
                }
                
                EditorGUILayout.LabelField($"{editorTool.GetRotationDegrees()}°", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(50));
                
                if (GUILayout.Button("⟳", GUILayout.Width(30)))
                {
                    editorTool.rotation = (editorTool.rotation + 1) % 6;
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField("R = Rotate+, Shift+R = Rotate-", EditorStyles.miniLabel);
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No asset selected. Select an asset from the browser above.", MessageType.Info);
            }
        }
        
        private void OnNewMap()
        {
            if (EditorUtility.DisplayDialog("New Map", "Clear the current map and start fresh?\n\nThis cannot be undone!", "Yes", "Cancel"))
            {
                if (hexGrid != null)
                {
                    hexGrid.Clear();
                    EditorUtility.SetDirty(hexGrid);
                }
            }
        }
        
        private void OnLoadMap()
        {
            string path = EditorUtility.OpenFilePanel("Load Hex Map", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    HexMapSaveData saveData = HexMapSaveData.FromJson(json);
                    
                    Debug.Log($"Loaded map: {saveData.mapName} with {saveData.tiles.Count} tiles and {saveData.decorations.Count} decorations");
                    lastSavedTime = "Just loaded";
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Load Error", $"Failed to load map:\n{e.Message}", "OK");
                }
            }
        }
        
        private void OnSaveMap()
        {
            if (hexGrid == null)
            {
                EditorUtility.DisplayDialog("Save Error", "No HexGrid found in scene!", "OK");
                return;
            }
            
            HexMapSaveData saveData = HexMapSaveData.FromHexGrid(hexGrid);
            saveData.mapName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            string json = saveData.ToJson();
            Debug.Log($"Map saved with {saveData.tiles.Count} tiles and {saveData.decorations.Count} decorations");
            
            lastSavedTime = DateTime.Now.ToString("HH:mm:ss");
        }
        
        private void OnSaveAsMap()
        {
            string path = EditorUtility.SaveFilePanel("Save Hex Map As", Application.dataPath, "hexmap", "json");
            if (!string.IsNullOrEmpty(path))
            {
                if (hexGrid == null)
                {
                    EditorUtility.DisplayDialog("Save Error", "No HexGrid found in scene!", "OK");
                    return;
                }
                
                HexMapSaveData saveData = HexMapSaveData.FromHexGrid(hexGrid);
                saveData.mapName = Path.GetFileNameWithoutExtension(path);
                
                string json = saveData.ToJson();
                File.WriteAllText(path, json);
                
                Debug.Log($"Map saved to {path}");
                lastSavedTime = DateTime.Now.ToString("HH:mm:ss");
            }
        }
        
        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
