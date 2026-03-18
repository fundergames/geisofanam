using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace RogueDeal.HexLevels.Editor
{
    /// <summary>
    /// Custom editor for HexLevelEditorTool that handles Scene view interaction.
    /// Implements mouse-based selection and placement.
    /// </summary>
    [CustomEditor(typeof(HexLevelEditorTool))]
    [CanEditMultipleObjects]
    public class HexLevelEditorToolEditor : UnityEditor.Editor
    {
        private HexLevelEditorTool tool;
        private HexGrid grid;
        private Event currentEvent;
        
        // Inspector UI state
        private string searchFilter = "";
        private string selectedCategory = "";
        private Vector2 prefabScrollPos;
        private bool showPrefabBrowser = true;
        
        // Raycast layer mask for ground plane (adjust as needed)
        private int groundLayerMask = 1 << 0; // Default layer
        
        // Preview instance for placement preview
        private GameObject previewInstance = null;
        private HexCoordinate? previewHex = null;
        private GameObject previewPrefab = null;
        
        // Drag state for click-and-drag placement
        private bool isDragging = false;
        private HexCoordinate? lastPlacedHex = null;
        
        // Manual rotation override flag for smart placement
        private bool manualRotationOverride = false;
        private int lastHoveredRotation = 0;

        private void OnEnable()
        {
            tool = (HexLevelEditorTool)target;
            PrefabBrowser.ClearCache(); // Refresh cache when editor opens
            
            // Auto-load mappings asset if not set
            if (tool.mappingsAsset == null)
            {
                LoadMappingsAsset();
            }
            
            // Update SmartTileSelector with the mappings
            if (tool.mappingsAsset != null)
            {
                SmartTileSelector.SetMappingsAsset(tool.mappingsAsset);
            }
            
            // Clean up any existing preview
            CleanupPreview();
        }

        private void OnDisable()
        {
            CleanupPreview();
            // Clean up drag state
            isDragging = false;
            lastPlacedHex = null;
        }
        
        private void LoadMappingsAsset()
        {
            const string DEFAULT_MAPPINGS_PATH = "Assets/RogueDeal/Resources/Data/HexLevels/ConnectionPatternMappings.asset";
            ConnectionPatternMappings mappings = AssetDatabase.LoadAssetAtPath<ConnectionPatternMappings>(DEFAULT_MAPPINGS_PATH);
            if (mappings != null)
            {
                tool.mappingsAsset = mappings;
                EditorUtility.SetDirty(tool);
            }
        }

        private GameObject lastSelectedPrefab = null;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Check if prefab selection changed
            if (tool.selectedPrefab != lastSelectedPrefab)
            {
                CleanupPreview();
                lastSelectedPrefab = tool.selectedPrefab;
            }
            
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Hex Level Editor", EditorStyles.boldLabel);
            
            // Tool mode selection
            EditorToolMode oldToolMode = tool.toolMode;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Place", tool.toolMode == EditorToolMode.Place ? EditorStyles.miniButtonMid : EditorStyles.miniButtonLeft))
            {
                tool.toolMode = EditorToolMode.Place;
                EditorUtility.SetDirty(tool);
            }
            if (GUILayout.Button("Delete", tool.toolMode == EditorToolMode.Delete ? EditorStyles.miniButtonMid : EditorStyles.miniButtonLeft))
            {
                tool.toolMode = EditorToolMode.Delete;
                EditorUtility.SetDirty(tool);
            }
            if (GUILayout.Button("Select", tool.toolMode == EditorToolMode.Select ? EditorStyles.miniButtonRight : EditorStyles.miniButtonRight))
            {
                tool.toolMode = EditorToolMode.Select;
                EditorUtility.SetDirty(tool);
            }
            EditorGUILayout.EndHorizontal();
            
            // Clean up preview and drag state if switching away from Place mode
            if (oldToolMode == EditorToolMode.Place && tool.toolMode != EditorToolMode.Place)
            {
                CleanupPreview();
                isDragging = false;
                lastPlacedHex = null;
            }
            
            // Clean up drag state if switching away from Delete mode
            if (oldToolMode == EditorToolMode.Delete && tool.toolMode != EditorToolMode.Delete)
            {
                isDragging = false;
                lastPlacedHex = null;
            }
            
            EditorGUILayout.Space(5);
            
            // Placement layer mode (only show when in Place mode)
            if (tool.toolMode == EditorToolMode.Place)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Placement Layer:", GUILayout.Width(100));
                PlacementLayerMode oldLayerMode = tool.placementLayerMode;
                tool.placementLayerMode = (PlacementLayerMode)EditorGUILayout.EnumPopup(tool.placementLayerMode);
                if (oldLayerMode != tool.placementLayerMode)
                {
                    EditorUtility.SetDirty(tool);
                }
                EditorGUILayout.EndHorizontal();
                
                // Show hint about what will be placed
                if (tool.selectedPrefab != null)
                {
                    PlacementLayerMode effectiveMode = tool.placementLayerMode;
                    if (effectiveMode == PlacementLayerMode.Auto)
                    {
                        effectiveMode = InferPlacementLayer(tool.selectedPrefab);
                    }
                    
                    string layerHint = effectiveMode == PlacementLayerMode.Ground 
                        ? "Will place/replace ground tile" 
                        : "Will place object on top of ground";
                    EditorGUILayout.HelpBox(layerHint, MessageType.Info);
                }
                
                EditorGUILayout.Space(5);
            }
            
            // Rotation control
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Rotation:", GUILayout.Width(60));
            int oldRotation = tool.rotation;
            tool.rotation = EditorGUILayout.IntSlider(tool.rotation, 0, 5);
            EditorGUILayout.LabelField($"{tool.GetRotationDegrees()}°", GUILayout.Width(40));
            if (oldRotation != tool.rotation)
            {
                EditorUtility.SetDirty(tool);
                // Preview will update rotation in OnSceneGUI
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();
            
            // Hint about arrow key controls
            if (tool.toolMode == EditorToolMode.Place && tool.selectedPrefab != null)
            {
                if (tool.enableSmartPlacement)
                {
                    EditorGUILayout.HelpBox("Tip: Left/Right = rotate tile. Up/Down = cycle through smart placement variants. Click Scene view first.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Tip: Left/Right = rotate. Up/Down = cycle prefabs in group (e.g. tiles/base, tiles/roads). Click Scene view first.", MessageType.Info);
                }
            }
            
            EditorGUILayout.Space(5);
            
            // Selected prefab preview
            if (tool.selectedPrefab != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Selected Prefab", EditorStyles.miniLabel);
                
                EditorGUILayout.BeginHorizontal();
                // Large preview
                Texture2D largePreview = AssetPreview.GetAssetPreview(tool.selectedPrefab);
                if (largePreview == null)
                {
                    largePreview = AssetPreview.GetMiniThumbnail(tool.selectedPrefab);
                }
                
                if (largePreview != null)
                {
                    GUILayout.Label(largePreview, GUILayout.Width(64), GUILayout.Height(64));
                }
                else
                {
                    GUILayout.Box("", GUILayout.Width(64), GUILayout.Height(64));
                }
                
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(tool.selectedPrefab.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Type: {InferTileType(tool.selectedPrefab)}", EditorStyles.miniLabel);
                if (GUILayout.Button("Ping in Project", EditorStyles.miniButton))
                {
                    EditorGUIUtility.PingObject(tool.selectedPrefab);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space(5);
            
            // Mappings asset info
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Connection Mappings", EditorStyles.miniLabel);
            
            tool.mappingsAsset = (ConnectionPatternMappings)EditorGUILayout.ObjectField(
                "Mappings Asset", tool.mappingsAsset, typeof(ConnectionPatternMappings), false);
            
            if (tool.mappingsAsset != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Road Mappings: {tool.mappingsAsset.roadMappings.Count}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"River Mappings: {tool.mappingsAsset.riverMappings.Count}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Coast Mappings: {tool.mappingsAsset.coastMappings.Count}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("Open Connection Mapper", EditorStyles.miniButton))
                {
                    ConnectionPatternMapper.ShowWindow();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No mappings asset assigned. Smart placement will use default logic.", MessageType.Info);
                if (GUILayout.Button("Load Default Mappings", EditorStyles.miniButton))
                {
                    LoadMappingsAsset();
                }
            }
            
            // Update SmartTileSelector when mappings change
            if (GUI.changed && tool.mappingsAsset != null)
            {
                SmartTileSelector.SetMappingsAsset(tool.mappingsAsset);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // Utility: Force update all connecting tiles
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Connection Utilities", EditorStyles.miniLabel);
            EditorGUILayout.HelpBox(
                "Use these tools to programmatically update all road/river/coast connections in the grid.",
                MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Update All Road Connections", EditorStyles.miniButton))
            {
                UpdateAllConnectingTiles(HexTileType.Road);
            }
            if (GUILayout.Button("Update All River Connections", EditorStyles.miniButton))
            {
                UpdateAllConnectingTiles(HexTileType.River);
            }
            if (GUILayout.Button("Update All Coast Connections", EditorStyles.miniButton))
            {
                UpdateAllConnectingTiles(HexTileType.Coast);
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Update ALL Connecting Tiles", EditorStyles.miniButton))
            {
                UpdateAllConnectingTiles(HexTileType.Road);
                UpdateAllConnectingTiles(HexTileType.River);
                UpdateAllConnectingTiles(HexTileType.Coast);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // Prefab browser
            showPrefabBrowser = EditorGUILayout.Foldout(showPrefabBrowser, "Prefab Browser", true);
            if (showPrefabBrowser)
            {
                DrawPrefabBrowser();
            }
            
            // Status info
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Status", EditorStyles.miniLabel);
            if (tool.targetGrid != null)
            {
                // Count tiles with ground and objects separately
                int groundTiles = 0;
                int objectCount = 0;
                foreach (var hex in tool.targetGrid.GetAllHexes())
                {
                    var tileData = tool.targetGrid.GetTile(hex);
                    if (tileData != null)
                    {
                        if (tileData.HasGroundTile())
                            groundTiles++;
                        if (tileData.HasObjects())
                            objectCount += tileData.ObjectLayerCount;
                    }
                }
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Grid: {groundTiles} ground tiles, {objectCount} objects", EditorStyles.miniLabel);
                if (GUILayout.Button("Clear Grid", EditorStyles.miniButton, GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog("Clear Grid", 
                        $"This will delete all {groundTiles} ground tiles and {objectCount} objects from the grid.\n\n" +
                        "This action cannot be undone!",
                        "Clear All", "Cancel"))
                    {
                        ClearGrid();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No HexGrid assigned!", MessageType.Warning);
            }
            
            HexCoordinate? hovered = tool.GetHoveredHex();
            if (hovered.HasValue)
            {
                HexTileData hoveredData = tool.targetGrid?.GetTile(hovered.Value);
                string hoveredInfo = $"Hovered: {hovered.Value}";
                if (hoveredData != null)
                {
                    if (hoveredData.HasGroundTile())
                        hoveredInfo += $" | Ground: {hoveredData.tileType}";
                    if (hoveredData.HasObjects())
                        hoveredInfo += $" | Objects: {hoveredData.ObjectLayerCount}";
                }
                EditorGUILayout.LabelField(hoveredInfo, EditorStyles.miniLabel);
            }
            
            HexCoordinate? selected = tool.GetSelectedHex();
            if (selected.HasValue)
            {
                HexTileData selectedData = tool.targetGrid?.GetTile(selected.Value);
                string selectedInfo = $"Selected: {selected.Value}";
                if (selectedData != null)
                {
                    if (selectedData.HasGroundTile())
                        selectedInfo += $" | Ground: {selectedData.tileType}";
                    if (selectedData.HasObjects())
                        selectedInfo += $" | Objects: {selectedData.ObjectLayerCount}";
                }
                EditorGUILayout.LabelField(selectedInfo, EditorStyles.miniLabel);
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPrefabBrowser()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Search filter
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchFilter = "";
            }
            EditorGUILayout.EndHorizontal();
            
            // Category dropdown
            var categories = PrefabBrowser.GetCategories();
            if (categories.Count > 0)
            {
                int currentIndex = string.IsNullOrEmpty(selectedCategory) ? 0 : categories.IndexOf(selectedCategory);
                if (currentIndex < 0) currentIndex = 0;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Category:", GUILayout.Width(60));
                int newIndex = EditorGUILayout.Popup(currentIndex, categories.ToArray());
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < categories.Count)
                {
                    selectedCategory = categories[newIndex];
                    // Auto-select first prefab in new category
                    var categoryPrefabs = PrefabBrowser.GetPrefabsInCategory(selectedCategory);
                    if (categoryPrefabs != null && categoryPrefabs.Count > 0)
                    {
                        tool.selectedPrefab = categoryPrefabs[0];
                        EditorUtility.SetDirty(tool);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            
            // Prefab list
            List<GameObject> prefabsToShow = new List<GameObject>();
            
            if (!string.IsNullOrEmpty(searchFilter))
            {
                // Show search results
                prefabsToShow = PrefabBrowser.FilterPrefabs(searchFilter);
            }
            else if (!string.IsNullOrEmpty(selectedCategory))
            {
                // Show category prefabs
                prefabsToShow = PrefabBrowser.GetPrefabsInCategory(selectedCategory);
            }
            else if (categories.Count > 0)
            {
                // Show first category by default
                prefabsToShow = PrefabBrowser.GetPrefabsInCategory(categories[0]);
            }
            
            // Draw prefab buttons
            prefabScrollPos = EditorGUILayout.BeginScrollView(prefabScrollPos, GUILayout.Height(200));
            
            if (prefabsToShow.Count == 0)
            {
                EditorGUILayout.HelpBox("No prefabs found. Make sure KayKit assets are imported.", MessageType.Info);
            }
            else
            {
                foreach (var prefab in prefabsToShow)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    bool isSelected = tool.selectedPrefab == prefab;
                    
                    // Preview thumbnail (20x20)
                    Texture2D preview = AssetPreview.GetAssetPreview(prefab);
                    if (preview == null)
                    {
                        // Fallback to mini thumbnail if preview not ready
                        preview = AssetPreview.GetMiniThumbnail(prefab);
                    }
                    
                    if (preview != null)
                    {
                        GUILayout.Label(preview, GUILayout.Width(20), GUILayout.Height(20));
                    }
                    else
                    {
                        // Placeholder if no preview available yet
                        GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
                    }
                    
                    // Button with prefab name
                    GUIStyle buttonStyle = isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                    GUI.backgroundColor = isSelected ? Color.yellow : Color.white;
                    
                    if (GUILayout.Button(prefab.name, buttonStyle))
                    {
                        tool.selectedPrefab = prefab;
                        EditorUtility.SetDirty(tool);
                        // Highlight in Project window
                        EditorGUIUtility.PingObject(prefab);
                    }
                    
                    // Object field for drag-drop (smaller, just icon)
                    GameObject draggedPrefab = (GameObject)EditorGUILayout.ObjectField(
                        prefab, typeof(GameObject), false, GUILayout.Width(18), GUILayout.Height(18));
                    if (draggedPrefab != prefab && draggedPrefab != null)
                    {
                        tool.selectedPrefab = draggedPrefab;
                        EditorUtility.SetDirty(tool);
                    }
                    
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            // Refresh button
            if (GUILayout.Button("Refresh Prefab Cache", EditorStyles.miniButton))
            {
                PrefabBrowser.ClearCache();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void OnSceneGUI()
        {
            if (tool == null || tool.targetGrid == null)
                return;

            grid = tool.targetGrid;
            currentEvent = Event.current;
            
            // DEBUG: Show Scene view camera rotation
            Camera sceneCamera = SceneView.lastActiveSceneView?.camera;
            if (sceneCamera != null)
            {
                float cameraYRotation = sceneCamera.transform.rotation.eulerAngles.y;
                Handles.BeginGUI();
                GUI.Label(new Rect(10, 50, 400, 20), $"Scene Camera Y Rotation: {cameraYRotation:F1}°");
                Handles.EndGUI();
            }
            
            // Get mouse position in Scene view
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);
            
            // Request keyboard focus for arrow key controls
            if (tool.toolMode == EditorToolMode.Place && tool.selectedPrefab != null)
            {
                GUIUtility.keyboardControl = controlID;
            }

            // Handle arrow keys: Left/Right = rotate, Up/Down = cycle smart placement variants
            if (currentEvent.type == EventType.KeyDown &&
                tool.toolMode == EditorToolMode.Place &&
                tool.selectedPrefab != null &&
                !EditorGUIUtility.editingTextField)
            {
                if (currentEvent.keyCode == KeyCode.LeftArrow)
                {
                    tool.rotation = (tool.rotation - 1 + 6) % 6;
                    manualRotationOverride = true;
                    EditorUtility.SetDirty(tool);
                    SceneView.RepaintAll();
                    currentEvent.Use();
                }
                else if (currentEvent.keyCode == KeyCode.RightArrow)
                {
                    tool.rotation = (tool.rotation + 1) % 6;
                    manualRotationOverride = true;
                    EditorUtility.SetDirty(tool);
                    SceneView.RepaintAll();
                    currentEvent.Use();
                }
                else if (currentEvent.keyCode == KeyCode.UpArrow || currentEvent.keyCode == KeyCode.DownArrow)
                {
                    // If smart placement is enabled and we have variants, cycle through them
                    // Otherwise, fall back to cycling prefabs in group
                    HexCoordinate? hovered = tool.GetHoveredHex();
                    if (tool.enableSmartPlacement && hovered.HasValue && tool.GetSmartPlacementVariantCount() > 1)
                    {
                        int currentIndex = tool.GetSmartPlacementVariantIndex();
                        int newIndex = currentIndex + (currentEvent.keyCode == KeyCode.DownArrow ? 1 : -1);
                        tool.SetSmartPlacementVariantIndex(newIndex);
                        EditorUtility.SetDirty(tool);
                        SceneView.RepaintAll();
                    }
                    else
                    {
                        // Fallback: cycle prefabs in group
                        CyclePrefabInGroup(currentEvent.keyCode == KeyCode.DownArrow);
                    }
                    currentEvent.Use();
                }
            }

            // Handle MouseUp outside of grid bounds check (so it works even when mouse leaves grid)
            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
            {
                // End drag operation
                isDragging = false;
                lastPlacedHex = null;
                currentEvent.Use();
            }
            
            // Get mouse ray in world space
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            
            // Try to hit a plane at Y=0 (ground level)
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldHit = ray.GetPoint(distance);
                HexCoordinate hoveredHexCoord = grid.WorldToHex(worldHit);
                
                // Check if mouse is over a valid hex
                if (grid.IsInBounds(hoveredHexCoord))
                {
                    tool.SetHoveredHex(hoveredHexCoord);
                    Vector3 hexCenter = grid.HexToWorld(hoveredHexCoord);
                    
                    // Draw hovered hex highlight
                    if (tool.showPreview)
                    {
                        DrawHexHighlight(hexCenter, grid.HexSize, tool.selectionColor);
                        
                        // DEBUG: Draw direction labels around the hex
                        DrawDirectionLabels(hoveredHexCoord);
                    }
                    
                    // Handle mouse input
                    switch (currentEvent.type)
                    {
                        case EventType.MouseDown:
                            if (currentEvent.button == 0 && currentEvent.control == false) // Left click, no Ctrl
                            {
                                // Start drag operation if in Place or Delete mode
                                if (tool.toolMode == EditorToolMode.Place || tool.toolMode == EditorToolMode.Delete)
                                {
                                    isDragging = true;
                                    lastPlacedHex = null; // Reset last placed hex
                                }
                                HandleHexClick(hoveredHexCoord, hexCenter);
                                currentEvent.Use();
                            }
                            break;
                            
                        case EventType.MouseDrag:
                            if (isDragging && currentEvent.button == 0)
                            {
                                // Only place if we're over a different hex than the last one
                                if (!lastPlacedHex.HasValue || lastPlacedHex.Value != hoveredHexCoord)
                                {
                                    HandleHexClick(hoveredHexCoord, hexCenter);
                                    lastPlacedHex = hoveredHexCoord;
                                }
                                currentEvent.Use();
                            }
                            break;
                            
                        case EventType.MouseMove:
                            SceneView.RepaintAll();
                            break;

                        case EventType.Repaint:
                            // Ensure preview is visible during repaint
                            if (previewInstance != null && !previewInstance.activeSelf)
                            {
                                previewInstance.SetActive(true);
                            }
                            break;
                    }
                    
                    // Update smart placement variants when hovering (if smart placement enabled)
                    if (tool.toolMode == EditorToolMode.Place && 
                        tool.selectedPrefab != null && 
                        tool.enableSmartPlacement)
                    {
                        UpdateSmartPlacementVariants(hoveredHexCoord);
                    }
                    
                    // Update placement preview
                    if (tool.toolMode == EditorToolMode.Place && 
                        tool.selectedPrefab != null && 
                        tool.showPreview)
                    {
                        UpdatePlacementPreview(hexCenter, hoveredHexCoord);
                        
                        // Show layer info
                        HexTileData existingData = grid.GetTile(hoveredHexCoord);
                        if (existingData != null)
                        {
                            string layerInfo = "";
                            if (existingData.HasGroundTile())
                                layerInfo += "Ground ✓";
                            if (existingData.HasObjects())
                                layerInfo += (layerInfo.Length > 0 ? " | " : "") + $"{existingData.ObjectLayerCount} Object(s)";
                            
                            if (layerInfo.Length > 0)
                            {
                                Vector3 labelPos = hexCenter + Vector3.up * 0.3f;
                                Handles.Label(labelPos, layerInfo, new GUIStyle { fontSize = 9, normal = { textColor = Color.white } });
                            }
                        }
                    }
                    else
                    {
                        // Clear preview if not in place mode or no prefab selected
                        CleanupPreview();
                    }
                    
                    // Show smart placement suggestion
                    if (tool.toolMode == EditorToolMode.Place && 
                        tool.selectedPrefab != null && 
                        tool.enableSmartPlacement)
                    {
                        DrawSmartPlacementHint(hexCenter, hoveredHexCoord);
                    }
                }
                else
                {
                    tool.SetHoveredHex(null);
                    CleanupPreview();
                    // If dragging and mouse leaves grid, don't end drag (user might come back)
                    // But reset last placed hex so we can place again if they return
                    if (isDragging)
                    {
                        lastPlacedHex = null;
                    }
                }
            }
            else
            {
                tool.SetHoveredHex(null);
                CleanupPreview();
                // If dragging and mouse leaves grid, don't end drag (user might come back)
                // But reset last placed hex so we can place again if they return
                if (isDragging)
                {
                    lastPlacedHex = null;
                }
            }
            
            // Draw selected hex
            HexCoordinate? selectedHex = tool.GetSelectedHex();
            if (selectedHex.HasValue && grid.IsInBounds(selectedHex.Value))
            {
                Vector3 selectedCenter = grid.HexToWorld(selectedHex.Value);
                DrawHexHighlight(selectedCenter, grid.HexSize, Color.cyan);
            }
        }

        private void HandleHexClick(HexCoordinate hex, Vector3 worldPos)
        {
            switch (tool.toolMode)
            {
                case EditorToolMode.Place:
                    PlaceTile(hex, worldPos);
                    break;
                    
                case EditorToolMode.Delete:
                    DeleteTile(hex);
                    break;
                    
                case EditorToolMode.Select:
                    tool.SetSelectedHex(hex);
                    EditorUtility.SetDirty(tool);
                    break;
            }
        }

        private void PlaceTile(HexCoordinate hex, Vector3 worldPos)
        {
            if (tool.selectedPrefab == null)
            {
                Debug.LogWarning("No prefab selected for placement!");
                return;
            }

            // Determine placement layer mode
            PlacementLayerMode layerMode = tool.placementLayerMode;
            if (layerMode == PlacementLayerMode.Auto)
            {
                layerMode = InferPlacementLayer(tool.selectedPrefab);
            }

            // Get or create tile data
            HexTileData tileData = grid.GetTile(hex);
            if (tileData == null)
            {
                tileData = new HexTileData();
            }

            // Record undo
            Undo.RecordObject(grid, "Place Hex Tile");
            if (!Application.isPlaying)
            {
                Undo.RecordObject(tool, "Place Hex Tile");
                EditorUtility.SetDirty(grid);
                EditorUtility.SetDirty(tool);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            HexTileType placedTileType = HexTileType.None;
            
            if (layerMode == PlacementLayerMode.Ground)
            {
                PlaceGroundTile(hex, worldPos, tileData);
                placedTileType = tileData.tileType;
            }
            else // PlacementLayerMode.Object
            {
                PlaceObjectOnTile(hex, worldPos, tileData);
            }

            // Update grid with modified tile data
            grid.SetTile(hex, tileData);
            
            // Update neighboring tiles AFTER the grid is updated
            // This ensures neighbors can see the newly placed tile when calculating connections
            // Pass the rotation of the newly placed tile so we can account for it
            Debug.Log($"[PlaceTile] Checking if neighbor update needed: placedTileType={placedTileType}, enableSmartPlacement={tool.enableSmartPlacement}");
            if (placedTileType == HexTileType.Road || placedTileType == HexTileType.River || placedTileType == HexTileType.Coast)
            {
                int placedTileRotation = (layerMode == PlacementLayerMode.Ground) ? tool.rotation : 0;
                Debug.Log($"[PlaceTile] Calling UpdateNeighborTiles for {placedTileType} at {hex} with rotation {placedTileRotation}");
                UpdateNeighborTiles(hex, placedTileType, placedTileRotation);
            }
            else
            {
                Debug.Log($"[PlaceTile] Skipping neighbor update - tile type {placedTileType} is not a connecting type");
            }
        }

        private void PlaceGroundTile(HexCoordinate hex, Vector3 worldPos, HexTileData tileData)
        {
            // Determine tile type from selected prefab
            HexTileType tileType = InferTileType(tool.selectedPrefab);
            
            // Validate placement
            PlacementValidator.ValidationResult validation = PlacementValidator.ValidateTilePlacement(hex, grid, tileType, tool.selectedPrefab);
            if (!validation.isValid)
            {
                Debug.LogWarning($"Cannot place ground tile: {validation.reason}");
                EditorUtility.DisplayDialog("Invalid Placement", validation.reason, "OK");
                return;
            }
            
            // Use smart placement if enabled
            GameObject prefabToPlace = tool.selectedPrefab;
            if (tool.enableSmartPlacement)
            {
                Debug.Log($"[PlaceGroundTile] User selected rotation: {tool.rotation} ({tool.rotation * 60}°)");
                
                // NEW: Pass user's rotation to V2 system
                SmartTileSelector.SelectionResult selection = SmartTileSelector.SelectTilePrefabWithRotation(
                    hex, grid, tileType, tool.selectedPrefab, tool.rotation);
                    
                if (selection.prefab != null)
                {
                    // V2 database found a match - use it
                    prefabToPlace = selection.prefab;
                    tool.rotation = selection.rotationDegrees / 60;
                    Debug.Log($"Smart placement V2: Selected {selection.prefab.name} for {tileType} at {hex} with rotation {selection.rotationDegrees}°");
                }
                else
                {
                    // Fallback to old variant system if V2 returned null
                    GameObject selectedVariant = tool.GetCurrentSmartPlacementVariant();
                    if (selectedVariant != null)
                    {
                        prefabToPlace = selectedVariant;
                        Debug.Log($"Smart placement: Using selected variant {selectedVariant.name} ({tool.GetSmartPlacementVariantIndex() + 1}/{tool.GetSmartPlacementVariantCount()}) for {tileType} at {hex} with rotation {tool.rotation}");
                    }
                }
            }

            // If there's an existing ground tile, destroy it
            if (tileData.groundTileInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(tileData.groundTileInstance);
                }
                else
                {
                    Undo.DestroyObjectImmediate(tileData.groundTileInstance);
                }
            }

            // Instantiate new ground tile
            GameObject instance = null;
            if (Application.isPlaying)
            {
                instance = Instantiate(prefabToPlace, worldPos, Quaternion.Euler(0, tool.GetRotationDegrees(), 0));
            }
            else
            {
                // Editor mode: use PrefabUtility to create instance
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPlace);
                instance.transform.position = worldPos;
                instance.transform.rotation = Quaternion.Euler(0, tool.GetRotationDegrees(), 0);
                Undo.RegisterCreatedObjectUndo(instance, "Place Ground Tile");
            }

            // Update tile data
            tileData.groundTilePrefab = prefabToPlace;
            tileData.groundTileInstance = instance;
            tileData.tileType = tileType;

            Debug.Log($"Placed ground tile {prefabToPlace.name} at hex {hex}");
            
            // Note: Neighbor updates are now handled in PlaceTile after grid.SetTile
            // This ensures the new tile is in the grid when neighbors are updated
        }
        
        /// <summary>
        /// Update neighboring tiles to reflect new connections when a connecting tile is placed.
        /// This ensures roads, rivers, and coast tiles update their connection patterns.
        /// Uses a programmatic approach that explicitly checks each direction and forces updates.
        /// Accounts for both the newly placed tile's rotation and each neighbor's rotation.
        /// </summary>
        /// <param name="centerHex">The hex where the new tile was placed</param>
        /// <param name="connectingType">The type of connecting tile (Road/River/Coast)</param>
        /// <param name="placedTileRotation">The rotation of the newly placed tile (0-5)</param>
        private void UpdateNeighborTiles(HexCoordinate centerHex, HexTileType connectingType, int placedTileRotation = 0)
        {
            if (!tool.enableSmartPlacement)
            {
                Debug.Log($"[UpdateNeighborTiles] Smart placement is disabled - skipping neighbor updates");
                return;
            }
                
            Debug.Log($"[UpdateNeighborTiles] Starting update for {connectingType} at {centerHex} (new tile rotation: {placedTileRotation})");
            
            HexCoordinate[] neighbors = centerHex.GetNeighbors();
            // Direction names for logging
            // Geometric: E, NE, NW, W, SW, SE (indices 0-5)
            string[] directionNames = { "East", "NE", "NW", "West", "SW", "SE" };
            string[] screenDirectionNames = { "East", "SE", "SW", "West", "NW", "NE" }; // N↔S flipped for screen
            
            // Helper function to get screen direction name from geometric index
            // Screen has N↔S flipped compared to geometric
            string GetScreenDirectionName(int geometricDir)
            {
                return screenDirectionNames[geometricDir];
            }
            
            int updatedCount = 0;
            
            for (int dir = 0; dir < neighbors.Length; dir++)
            {
                HexCoordinate neighborHex = neighbors[dir];
                
                if (!grid.IsInBounds(neighborHex))
                {
                    Debug.Log($"[UpdateNeighborTiles] Neighbor {GetScreenDirectionName(dir)} ({neighborHex}) is out of bounds");
                    continue;
                }
                    
                HexTileData neighborData = grid.GetTile(neighborHex);
                if (neighborData == null || !neighborData.HasGroundTile())
                {
                    Debug.Log($"[UpdateNeighborTiles] Neighbor {GetScreenDirectionName(dir)} ({neighborHex}) has no tile");
                    continue;
                }
                    
                // Only update neighbors of the same connecting type
                if (neighborData.tileType != connectingType)
                {
                    Debug.Log($"[UpdateNeighborTiles] Neighbor {GetScreenDirectionName(dir)} ({neighborHex}) is type {neighborData.tileType}, not {connectingType}");
                    continue;
                }
                
                // Calculate the opposite direction (the direction from neighbor to center)
                int oppositeDir = (dir + 3) % 6;
                Debug.Log($"[UpdateNeighborTiles] Processing neighbor {GetScreenDirectionName(dir)} ({neighborHex}) of type {connectingType}");
                Debug.Log($"[UpdateNeighborTiles]   - GEOMETRIC direction: {directionNames[dir]} (index {dir})");
                Debug.Log($"[UpdateNeighborTiles]   - SCREEN direction (what user sees): {GetScreenDirectionName(dir)}");
                Debug.Log($"[UpdateNeighborTiles]   - Connection from center → neighbor: geometric {directionNames[dir]}, screen {GetScreenDirectionName(dir)}");
                Debug.Log($"[UpdateNeighborTiles]   - Connection from neighbor → center: geometric {directionNames[oppositeDir]}, screen {GetScreenDirectionName(oppositeDir)}");
                
                // Programmatically check all neighbors of this neighbor to build connection pattern
                int connectionBitmask = CalculateConnectionBitmask(neighborHex, grid, connectingType);
                Debug.Log($"[UpdateNeighborTiles] Neighbor {neighborHex} has connection bitmask: {connectionBitmask} (binary: {System.Convert.ToString(connectionBitmask, 2).PadLeft(6, '0')})");
                
                // Verify the connection is bidirectional
                bool centerHasConnection = (connectionBitmask & (1 << oppositeDir)) != 0;
                Debug.Log($"[UpdateNeighborTiles]   - Neighbor sees connection at geometric {directionNames[oppositeDir]} (screen {GetScreenDirectionName(oppositeDir)}): {centerHasConnection}");
                
                // Get the current rotation from the existing instance
                int neighborRotation = 0;
                if (neighborData.groundTileInstance != null)
                {
                    float yRotation = neighborData.groundTileInstance.transform.rotation.eulerAngles.y;
                    // Convert degrees to rotation index (0-5), rounding to nearest
                    neighborRotation = Mathf.RoundToInt(yRotation / 60f) % 6;
                    if (neighborRotation < 0) neighborRotation += 6;
                    Debug.Log($"[UpdateNeighborTiles] Neighbor rotation: {neighborRotation} ({yRotation} degrees)");
                }
                
                // Calculate the optimal rotation based on the connection bitmask
                // This ensures tiles are oriented correctly for their connections
                int optimalRotation = CalculateOptimalRotation(connectionBitmask);
                
                Debug.Log($"[UpdateNeighborTiles] Rotation summary:");
                Debug.Log($"[UpdateNeighborTiles]   - Newly placed tile rotation: {placedTileRotation}");
                Debug.Log($"[UpdateNeighborTiles]   - Neighbor current rotation: {neighborRotation}");
                Debug.Log($"[UpdateNeighborTiles]   - Neighbor optimal rotation: {optimalRotation}");
                
                // Use smart placement to get the correct variant for this neighbor
                // 
                // IMPORTANT: How rotation works with connections:
                // 1. The connection bitmask is absolute world-space (direction 0=East, 1=NE, etc.)
                // 2. When a tile is rotated R clockwise, its local direction 0 points to world direction R
                // 3. To find the right variant, we convert world-space bitmask to tile-local bitmask
                // 4. The variant selection uses the tile-local bitmask to find matching prefab
                // 5. Connections are bidirectional: if tile A connects to tile B at world dir D,
                //    then tile B connects to tile A at world dir (D + 3) mod 6 (opposite direction)
                //
                // For neighbor updates, we use optimalRotation to ensure tiles are correctly oriented
                
                // Log the raw bitmask before rotation
                int rawBitmask = connectionBitmask;
                int rotatedBitmask = RotateBitmaskForVariantSelection(rawBitmask, optimalRotation);
                Debug.Log($"[UpdateNeighborTiles] Bitmask transformation:");
                Debug.Log($"[UpdateNeighborTiles]   - Raw bitmask: {rawBitmask} (binary: {System.Convert.ToString(rawBitmask, 2).PadLeft(6, '0')})");
                Debug.Log($"[UpdateNeighborTiles]   - Rotated by {optimalRotation}: {rotatedBitmask} (binary: {System.Convert.ToString(rotatedBitmask, 2).PadLeft(6, '0')})");
                
                // Get current prefab name for variant extraction
                string currentPrefabName = neighborData.groundTilePrefab != null ? neighborData.groundTilePrefab.name : "null";
                
                // NEW: Use V2 database system - use AUTO mode for neighbor updates
                // We pass -1 for userRotation so it only matches the actual neighbor connections,
                // without adding phantom "straight" bits from the neighbor's visual rotation
                // Also pass current variant info so we can preserve it if it's already sufficient
                string currentVariantName = ExtractVariantName(currentPrefabName);
                SmartTileSelector.SelectionResult selection = SmartTileSelector.SelectTilePrefabWithRotation(
                    neighborHex, 
                    grid, 
                    connectingType, 
                    neighborData.groundTilePrefab,
                    -1,  // AUTO mode: match only the actual connections
                    currentVariantName,  // Current variant (e.g., "A", "E")
                    neighborRotation  // Current rotation index
                );
                
                if (selection.prefab == null)
                {
                    Debug.LogWarning($"[UpdateNeighborTiles] Smart placement V2 returned null for {neighborHex}");
                    continue;
                }
                
                GameObject smartPrefab = selection.prefab;
                int smartRotation = selection.rotationDegrees / 60;
                
                // Check if the variant actually changed by comparing prefab names
                string newPrefabName = smartPrefab.name;
                
                Debug.Log($"[UpdateNeighborTiles] Prefab selection:");
                Debug.Log($"[UpdateNeighborTiles]   - Current prefab: {currentPrefabName}");
                Debug.Log($"[UpdateNeighborTiles]   - Selected prefab: {newPrefabName}");
                Debug.Log($"[UpdateNeighborTiles]   - Selected rotation: {smartRotation} ({smartRotation * 60}°)");
                Debug.Log($"[UpdateNeighborTiles]   - Prefab objects equal: {neighborData.groundTilePrefab == smartPrefab}");
                Debug.Log($"[UpdateNeighborTiles]   - Instance exists: {neighborData.groundTileInstance != null}");
                if (neighborData.groundTileInstance != null)
                {
                    Debug.Log($"[UpdateNeighborTiles]   - Current instance name: {neighborData.groundTileInstance.name}");
                }
                
                // Check if update is needed
                // CRITICAL: Only update if the current tile is actually broken (missing required connections)
                // Don't update just because it has extra/unused connection points
                bool variantChanged = (currentPrefabName != newPrefabName);
                bool rotationChanged = false;
                int currentRotation = 0;
                if (neighborData.groundTileInstance != null)
                {
                    currentRotation = Mathf.RoundToInt(neighborData.groundTileInstance.transform.eulerAngles.y / 60f) % 6;
                    rotationChanged = (currentRotation != smartRotation);
                }
                
                // Check if current tile is missing any required connections
                bool isBroken = false;
                if (!variantChanged && rotationChanged)
                {
                    // Tile variant is correct, but rotation might be different
                    // Check if current rotation can support the required pattern
                    int requiredPattern = HexContextAnalyzer.GetNeighborBitmask(neighborHex, grid, connectingType);
                    
                    // Use database to check if current variant at current rotation supports the pattern
                    if (!string.IsNullOrEmpty(currentVariantName))
                    {
                        // If current variant+rotation can support the pattern, it's not broken
                        isBroken = !SmartTileSelector.GetDatabaseV2()?.CanSupportPattern(currentVariantName, currentRotation, requiredPattern) ?? true;
                        string canSupport = isBroken ? "NOT" : "";
                        Debug.Log($"[UpdateNeighborTiles] Tile rotation check: current variant {currentVariantName} at {currentRotation*60}° can{canSupport} support pattern {requiredPattern} - isBroken={isBroken}");
                    }
                }
                
                bool needsUpdate = variantChanged || isBroken;
                
                if (needsUpdate)
                {
                    Debug.Log($"[UpdateNeighborTiles] Updating {neighborHex} - variant changed: {variantChanged}, rotation changed: {rotationChanged}");
                    
                    // Get world position for neighbor
                    Vector3 neighborWorldPos = grid.HexToWorld(neighborHex);
                    
                    // Record undo
                    Undo.RecordObject(grid, "Update Neighbor Tile Connection");
                    if (!Application.isPlaying)
                    {
                        EditorUtility.SetDirty(grid);
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    }
                    
                    // Destroy old instance
                    GameObject oldInstance = neighborData.groundTileInstance;
                    if (oldInstance != null)
                    {
                        Debug.Log($"[UpdateNeighborTiles] Destroying old instance: {oldInstance.name} at {oldInstance.transform.position}");
                        if (Application.isPlaying)
                        {
                            Destroy(oldInstance);
                        }
                        else
                        {
                            Undo.DestroyObjectImmediate(oldInstance);
                        }
                        // Clear the reference immediately
                        neighborData.groundTileInstance = null;
                    }
                    
                    // Create new instance with correct variant and rotation from V2
                    GameObject newInstance = null;
                    if (Application.isPlaying)
                    {
                        newInstance = Instantiate(smartPrefab, neighborWorldPos, Quaternion.Euler(0, smartRotation * 60f, 0));
                    }
                    else
                    {
                        // Verify prefab exists before instantiating
                        if (smartPrefab == null)
                        {
                            Debug.LogError($"[UpdateNeighborTiles] Cannot instantiate null prefab for {neighborHex}!");
                            continue;
                        }
                        
                        Debug.Log($"[UpdateNeighborTiles] Instantiating prefab: {smartPrefab.name} from {AssetDatabase.GetAssetPath(smartPrefab)}");
                        newInstance = (GameObject)PrefabUtility.InstantiatePrefab(smartPrefab);
                        if (newInstance == null)
                        {
                            Debug.LogError($"[UpdateNeighborTiles] Failed to instantiate prefab {smartPrefab.name}!");
                            continue;
                        }
                        newInstance.transform.position = neighborWorldPos;
                        newInstance.transform.rotation = Quaternion.Euler(0, smartRotation * 60f, 0);
                        newInstance.name = $"{smartPrefab.name}_Instance_{neighborHex}";
                        Undo.RegisterCreatedObjectUndo(newInstance, "Update Neighbor Tile Connection");
                        
                        // Ensure the instance is active and visible
                        newInstance.SetActive(true);
                        Debug.Log($"[UpdateNeighborTiles] Created new instance: {newInstance.name}, Active: {newInstance.activeSelf}, Position: {newInstance.transform.position}");
                    }
                    
                    // Update tile data
                    neighborData.groundTilePrefab = smartPrefab;
                    neighborData.groundTileInstance = newInstance;
                    
                    // Update grid
                    grid.SetTile(neighborHex, neighborData);
                    
                    // Verify the update
                    Debug.Log($"[UpdateNeighborTiles] Instance verification:");
                    Debug.Log($"[UpdateNeighborTiles]   - New instance created: {newInstance != null}");
                    if (newInstance != null)
                    {
                        Debug.Log($"[UpdateNeighborTiles]   - New instance name: {newInstance.name}");
                        Debug.Log($"[UpdateNeighborTiles]   - New instance prefab: {PrefabUtility.GetCorrespondingObjectFromSource(newInstance)?.name ?? "null"}");
                        Debug.Log($"[UpdateNeighborTiles]   - New instance position: {newInstance.transform.position}");
                        Debug.Log($"[UpdateNeighborTiles]   - New instance rotation: {newInstance.transform.rotation.eulerAngles}");
                    }
                    Debug.Log($"[UpdateNeighborTiles]   - Tile data prefab: {neighborData.groundTilePrefab?.name ?? "null"}");
                    Debug.Log($"[UpdateNeighborTiles]   - Tile data instance: {neighborData.groundTileInstance?.name ?? "null"}");
                    
                    updatedCount++;
                    Debug.Log($"[UpdateNeighborTiles] ✓ Updated neighbor tile at {neighborHex} to {newPrefabName}");
                }
                else
                {
                    Debug.Log($"[UpdateNeighborTiles] No update needed for {neighborHex} - variant already correct");
                }
            }
            
            // Force scene refresh to ensure visual updates
            if (!Application.isPlaying && updatedCount > 0)
            {
                SceneView.RepaintAll();
                EditorUtility.SetDirty(grid);
            }
            
            Debug.Log($"[UpdateNeighborTiles] Completed: Updated {updatedCount} neighbor(s)");
        }
        
        /// <summary>
        /// Extract variant letter from prefab name (e.g., "hex_road_A" -> "A").
        /// </summary>
        private string ExtractVariantName(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return null;
                
            // Pattern: hex_road_X where X is the variant
            int lastUnderscore = prefabName.LastIndexOf('_');
            if (lastUnderscore >= 0 && lastUnderscore < prefabName.Length - 1)
            {
                return prefabName.Substring(lastUnderscore + 1);
            }
            
            return null;
        }
        
        /// <summary>
        /// Programmatically calculate the connection bitmask for a hex.
        /// Explicitly checks each direction and builds the bitmask.
        /// </summary>
        private int CalculateConnectionBitmask(HexCoordinate hex, HexGrid grid, HexTileType connectingType)
        {
            int bitmask = 0;
            HexCoordinate[] neighbors = hex.GetNeighbors();
            string[] directionNames = { "East", "NE", "NW", "West", "SW", "SE" };
            
            for (int i = 0; i < neighbors.Length; i++)
            {
                HexTileData neighborData = grid.GetTile(neighbors[i]);
                if (neighborData != null && neighborData.tileType == connectingType)
                {
                    bitmask |= (1 << i);
                    Debug.Log($"[CalculateConnectionBitmask] {hex} has {connectingType} neighbor at {directionNames[i]} ({neighbors[i]})");
                }
            }
            
            return bitmask;
        }
        
        /// <summary>
        /// Rotate a bitmask for variant selection (same logic as SmartTileSelector).
        /// This helps debug what bitmask is being used for variant lookup.
        /// 
        /// Converts world-space connection bitmask to tile-local bitmask based on rotation.
        /// See SmartTileSelector.RotateBitmask for detailed explanation.
        /// </summary>
        private int RotateBitmaskForVariantSelection(int bitmask, int rotation)
        {
            if (rotation == 0)
                return bitmask;

            // Normalize rotation to 0-5 range
            rotation = ((rotation % 6) + 6) % 6;
            
            // Convert world-space bitmask to tile-local bitmask
            // If tile is rotated R clockwise, world direction W maps to local direction (W - R) mod 6
            // So: output[local] = input[(local + R) mod 6]
            int rotated = 0;
            for (int localDir = 0; localDir < 6; localDir++)
            {
                // Local direction localDir corresponds to world direction (localDir + rotation) mod 6
                int worldDir = (localDir + rotation) % 6;
                if ((bitmask & (1 << worldDir)) != 0)
                {
                    rotated |= (1 << localDir);
                }
            }
            
            return rotated;
        }

        private int CalculateOptimalRotation(int worldBitmask)
        {
            // Calculate rotation so the tile's canonical pattern matches the world connections
            // We want to find rotation R such that rotating worldBitmask by R gives us the
            // canonical pattern (lowest numeric value)
            
            if (worldBitmask == 0)
                return 0; // No connections
            
            // The canonical pattern is the one where connections start at the lowest bit
            // For example:
            //   Pattern 3 (0b000011) = E,NE is canonical for 2 adjacent connections (60° curve)
            //   Pattern 9 (0b001001) = E,W is canonical for 2 opposite connections (straight)
            //   Pattern 1 (0b000001) = E is canonical for 1 connection
            
            // Find which rotation produces a pattern with the first connection at bit 0 (East)
            // This ensures consistent orientation
            for (int rotation = 0; rotation < 6; rotation++)
            {
                int rotatedBitmask = RotateBitmaskForVariantSelection(worldBitmask, rotation);
                
                // Check if bit 0 is set (connection at East in rotated space)
                if ((rotatedBitmask & 1) != 0)
                {
                    return rotation;
                }
            }
            
            // Fallback: no rotation
            return 0;
        }
        
        /// <summary>
        /// Programmatically update all connecting tiles of a given type in the grid.
        /// This forces a recalculation of all connection patterns.
        /// </summary>
        private void UpdateAllConnectingTiles(HexTileType connectingType)
        {
            if (grid == null)
            {
                EditorUtility.DisplayDialog("Error", "No HexGrid found!", "OK");
                return;
            }
            
            if (!tool.enableSmartPlacement)
            {
                EditorUtility.DisplayDialog("Info", "Smart placement is disabled. Enable it first.", "OK");
                return;
            }
            
            bool proceed = EditorUtility.DisplayDialog(
                "Update All Connections",
                $"This will recalculate and update all {connectingType} tiles in the grid.\n\n" +
                "This may take a moment for large grids.\n\n" +
                "Continue?",
                "Yes", "Cancel");
            
            if (!proceed)
                return;
            
            Debug.Log($"[UpdateAllConnectingTiles] Starting update for all {connectingType} tiles");
            
            // Collect all tiles of this type
            List<HexCoordinate> tilesToUpdate = new List<HexCoordinate>();
            foreach (var hex in grid.GetAllHexes())
            {
                HexTileData tileData = grid.GetTile(hex);
                if (tileData != null && tileData.tileType == connectingType && tileData.HasGroundTile())
                {
                    tilesToUpdate.Add(hex);
                }
            }
            
            Debug.Log($"[UpdateAllConnectingTiles] Found {tilesToUpdate.Count} {connectingType} tiles to update");
            
            int updatedCount = 0;
            int skippedCount = 0;
            
            // Record undo for batch operation
            Undo.RecordObject(grid, $"Update All {connectingType} Connections");
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(grid);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            
            foreach (var hex in tilesToUpdate)
            {
                HexTileData tileData = grid.GetTile(hex);
                if (tileData == null || !tileData.HasGroundTile())
                    continue;
                
                // Get current rotation
                int currentRotation = 0;
                if (tileData.groundTileInstance != null)
                {
                    float yRotation = tileData.groundTileInstance.transform.rotation.eulerAngles.y;
                    currentRotation = Mathf.RoundToInt(yRotation / 60f) % 6;
                    if (currentRotation < 0) currentRotation += 6;
                }
                
                // Calculate what the tile should be
                GameObject smartPrefab = SmartTileSelector.SelectTilePrefab(
                    hex,
                    grid,
                    connectingType,
                    tileData.groundTilePrefab,
                    currentRotation
                );
                
                if (smartPrefab == null)
                {
                    skippedCount++;
                    continue;
                }
                
                // Check if update is needed
                string currentPrefabName = tileData.groundTilePrefab != null ? tileData.groundTilePrefab.name : "";
                string newPrefabName = smartPrefab.name;
                
                if (currentPrefabName == newPrefabName)
                {
                    skippedCount++;
                    continue;
                }
                
                // Update the tile
                Vector3 worldPos = grid.HexToWorld(hex);
                
                // Destroy old instance
                if (tileData.groundTileInstance != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(tileData.groundTileInstance);
                    }
                    else
                    {
                        Undo.DestroyObjectImmediate(tileData.groundTileInstance);
                    }
                }
                
                // Create new instance
                GameObject newInstance = null;
                if (Application.isPlaying)
                {
                    newInstance = Instantiate(smartPrefab, worldPos, Quaternion.Euler(0, currentRotation * 60f, 0));
                }
                else
                {
                    newInstance = (GameObject)PrefabUtility.InstantiatePrefab(smartPrefab);
                    newInstance.transform.position = worldPos;
                    newInstance.transform.rotation = Quaternion.Euler(0, currentRotation * 60f, 0);
                    Undo.RegisterCreatedObjectUndo(newInstance, $"Update {connectingType} Connection");
                }
                
                // Update tile data
                tileData.groundTilePrefab = smartPrefab;
                tileData.groundTileInstance = newInstance;
                grid.SetTile(hex, tileData);
                
                updatedCount++;
            }
            
            Debug.Log($"[UpdateAllConnectingTiles] Completed: Updated {updatedCount}, Skipped {skippedCount}");
            EditorUtility.DisplayDialog(
                "Update Complete",
                $"Updated {updatedCount} {connectingType} tile(s).\n" +
                $"Skipped {skippedCount} (already correct).",
                "OK");
        }

        private void PlaceObjectOnTile(HexCoordinate hex, Vector3 worldPos, HexTileData tileData)
        {
            // Check if there's a ground tile to place on
            if (!tileData.HasGroundTile())
            {
                bool placeAnyway = EditorUtility.DisplayDialog("No Ground Tile", 
                    $"Hex {hex} doesn't have a ground tile.\n\n" +
                    "Objects should be placed on top of ground tiles.\n\n" +
                    "Place anyway? (Object will be placed at ground level)",
                    "Place Anyway", "Cancel");
                
                if (!placeAnyway)
                {
                    return;
                }
            }

            // Calculate height offset - place objects slightly above ground
            float heightOffset = 0.1f; // Small offset to prevent z-fighting
            
            // If there are existing objects, stack them
            if (tileData.HasObjects())
            {
                // Find the highest object to stack on top
                float maxHeight = 0f;
                foreach (var layer in tileData.objectLayers)
                {
                    if (layer.instance != null)
                    {
                        float objHeight = layer.instance.transform.position.y;
                        if (objHeight > maxHeight)
                            maxHeight = objHeight;
                    }
                }
                
                // Get bounds of the new prefab to calculate stacking height
                Bounds? bounds = GetPrefabBounds(tool.selectedPrefab);
                if (bounds.HasValue)
                {
                    heightOffset = maxHeight + bounds.Value.size.y * 0.5f - worldPos.y;
                }
            }

            // Instantiate object
            GameObject instance = null;
            Vector3 objectPos = worldPos + Vector3.up * heightOffset;
            
            if (Application.isPlaying)
            {
                instance = Instantiate(tool.selectedPrefab, objectPos, Quaternion.Euler(0, tool.GetRotationDegrees(), 0));
            }
            else
            {
                // Editor mode: use PrefabUtility to create instance
                instance = (GameObject)PrefabUtility.InstantiatePrefab(tool.selectedPrefab);
                instance.transform.position = objectPos;
                instance.transform.rotation = Quaternion.Euler(0, tool.GetRotationDegrees(), 0);
                Undo.RegisterCreatedObjectUndo(instance, "Place Object on Tile");
            }

            // Add to tile data as an object layer
            tileData.AddObjectLayer(tool.selectedPrefab, instance, heightOffset);

            Debug.Log($"Placed object {tool.selectedPrefab.name} on hex {hex} (layer {tileData.ObjectLayerCount})");
        }

        /// <summary>
        /// Infer tile type from prefab name.
        /// </summary>
        private HexTileType InferTileType(GameObject prefab)
        {
            if (prefab == null)
                return HexTileType.Grass;

            string prefabName = prefab.name.ToLower();
            
            if (prefabName.Contains("grass"))
                return HexTileType.Grass;
            else if (prefabName.Contains("water") && !prefabName.Contains("river") && !prefabName.Contains("coast"))
                return HexTileType.Water;
            else if (prefabName.Contains("river"))
                return HexTileType.River;
            else if (prefabName.Contains("coast"))
                return HexTileType.Coast;
            else if (prefabName.Contains("road"))
                return HexTileType.Road;
            else
                return HexTileType.Grass; // Default
        }

        /// <summary>
        /// Infer placement layer mode from prefab name/type.
        /// Ground tiles: grass, water, coast, river, road
        /// Objects: buildings, units, decorations, props, etc.
        /// </summary>
        private PlacementLayerMode InferPlacementLayer(GameObject prefab)
        {
            if (prefab == null)
                return PlacementLayerMode.Object;

            string prefabName = prefab.name.ToLower();
            
            // Ground tile keywords
            if (prefabName.Contains("grass") || 
                prefabName.Contains("water") || 
                prefabName.Contains("coast") || 
                prefabName.Contains("river") || 
                prefabName.Contains("road") ||
                prefabName.Contains("tile"))
            {
                return PlacementLayerMode.Ground;
            }
            
            // Object keywords (buildings, decorations, etc.)
            if (prefabName.Contains("building") || 
                prefabName.Contains("house") || 
                prefabName.Contains("tower") || 
                prefabName.Contains("wall") ||
                prefabName.Contains("rock") || 
                prefabName.Contains("tree") || 
                prefabName.Contains("decoration") ||
                prefabName.Contains("prop") ||
                prefabName.Contains("unit") ||
                prefabName.Contains("barrel") ||
                prefabName.Contains("crate"))
            {
                return PlacementLayerMode.Object;
            }
            
            // Default to object if uncertain
            return PlacementLayerMode.Object;
        }

        private void DeleteTile(HexCoordinate hex)
        {
            HexTileData tileData = grid.GetTile(hex);
            if (tileData == null)
            {
                return; // Nothing to delete
            }

            // Capture tile type before deletion for neighbor updates
            HexTileType deletedType = tileData.tileType;
            bool wasConnectingType = deletedType == HexTileType.Road || 
                                     deletedType == HexTileType.River || 
                                     deletedType == HexTileType.Coast;

            // Record undo
            Undo.RecordObject(grid, "Delete Hex Tile");
            if (!Application.isPlaying)
            {
                Undo.RecordObject(tool, "Delete Hex Tile");
                EditorUtility.SetDirty(grid);
                EditorUtility.SetDirty(tool);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            bool hasGround = tileData.HasGroundTile();
            bool hasObjects = tileData.HasObjects();

            // If both ground and objects exist, ask what to delete
            if (hasGround && hasObjects)
            {
                int choice = EditorUtility.DisplayDialogComplex("Delete Hex Content",
                    $"Hex {hex} has both a ground tile and {tileData.ObjectLayerCount} object(s).\n\n" +
                    "What would you like to delete?",
                    "Everything", "Ground Only", "Cancel");
                
                if (choice == 2) // Cancel
                    return;
                
                if (choice == 0) // Everything
                {
                    DeleteEverything(tileData);
                    grid.SetTile(hex, null);
                }
                else if (choice == 1) // Ground only
                {
                    DeleteGroundTile(tileData);
                    // Keep tile data if there are still objects
                    if (!tileData.HasObjects())
                    {
                        grid.SetTile(hex, null);
                    }
                }
            }
            else if (hasGround)
            {
                // Only ground tile - delete it
                DeleteGroundTile(tileData);
                grid.SetTile(hex, null);
            }
            else if (hasObjects)
            {
                // Only objects - ask if delete all or just top layer
                if (tileData.ObjectLayerCount > 1)
                {
                    bool deleteAll = EditorUtility.DisplayDialog("Delete Objects",
                        $"Hex {hex} has {tileData.ObjectLayerCount} objects.\n\n" +
                        "Delete all objects or just the top layer?",
                        "Delete All", "Top Layer Only");
                    
                    if (deleteAll)
                    {
                        DeleteAllObjects(tileData);
                        grid.SetTile(hex, null);
                    }
                    else
                    {
                        DeleteTopObject(tileData);
                        // Keep tile data if there are still objects
                        if (!tileData.HasObjects())
                        {
                            grid.SetTile(hex, null);
                        }
                    }
                }
                else
                {
                    // Only one object
                    DeleteAllObjects(tileData);
                    grid.SetTile(hex, null);
                }
            }

            Debug.Log($"Deleted content at hex {hex}");
            
            // Update neighboring tiles if a connecting type was deleted
            // No rotation to pass since tile was deleted
            if (wasConnectingType && tool.enableSmartPlacement)
            {
                UpdateNeighborTiles(hex, deletedType, 0);
            }
        }

        private void DeleteEverything(HexTileData tileData)
        {
            // Delete ground tile
            if (tileData.groundTileInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(tileData.groundTileInstance);
                }
                else
                {
                    Undo.DestroyObjectImmediate(tileData.groundTileInstance);
                }
            }

            // Delete all objects
            DeleteAllObjects(tileData);
        }

        private void DeleteGroundTile(HexTileData tileData)
        {
            if (tileData.groundTileInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(tileData.groundTileInstance);
                }
                else
                {
                    Undo.DestroyObjectImmediate(tileData.groundTileInstance);
                }
            }
            tileData.ClearGroundTile();
        }

        private void DeleteAllObjects(HexTileData tileData)
        {
            if (tileData.objectLayers != null)
            {
                foreach (var layer in tileData.objectLayers)
                {
                    if (layer.instance != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(layer.instance);
                        }
                        else
                        {
                            Undo.DestroyObjectImmediate(layer.instance);
                        }
                    }
                }
            }
            tileData.ClearObjectLayers();
        }

        private void DeleteTopObject(HexTileData tileData)
        {
            if (tileData.objectLayers == null || tileData.objectLayers.Count == 0)
                return;

            // Find the highest layer index
            int maxLayerIndex = -1;
            HexTileLayer topLayer = null;
            foreach (var layer in tileData.objectLayers)
            {
                if (layer.layerIndex > maxLayerIndex)
                {
                    maxLayerIndex = layer.layerIndex;
                    topLayer = layer;
                }
            }

            if (topLayer != null && topLayer.instance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(topLayer.instance);
                }
                else
                {
                    Undo.DestroyObjectImmediate(topLayer.instance);
                }
                tileData.RemoveObjectLayer(topLayer.instance);
            }
        }

        /// <summary>
        /// Update the list of available smart placement variants for the current hex.
        /// </summary>
        private void UpdateSmartPlacementVariants(HexCoordinate hex)
        {
            PlacementLayerMode layerMode = tool.placementLayerMode;
            if (layerMode == PlacementLayerMode.Auto)
            {
                layerMode = InferPlacementLayer(tool.selectedPrefab);
            }
            
            // Only get variants for ground tiles
            if (layerMode == PlacementLayerMode.Ground)
            {
                HexTileType tileType = InferTileType(tool.selectedPrefab);
                List<GameObject> variants = SmartTileSelector.GetAllMatchingTilePrefabs(
                    hex, grid, tileType, tool.selectedPrefab, tool.rotation);
                
                tool.SetSmartPlacementVariants(hex, variants);
            }
            else
            {
                // For objects, no smart placement variants
                tool.SetSmartPlacementVariants(hex, new List<GameObject>());
            }
        }
        
        private void UpdatePlacementPreview(Vector3 center, HexCoordinate hex)
        {
            if (tool.selectedPrefab == null)
            {
                CleanupPreview();
                return;
            }
            
            // Reset manual rotation override when moving to a different hex
            if (previewHex.HasValue && previewHex.Value != hex)
            {
                manualRotationOverride = false;
            }

            // Determine placement layer mode
            PlacementLayerMode layerMode = tool.placementLayerMode;
            if (layerMode == PlacementLayerMode.Auto)
            {
                layerMode = InferPlacementLayer(tool.selectedPrefab);
            }

            HexTileData existingData = grid.GetTile(hex);
            
            // Get the prefab to preview (might be different due to smart placement)
            GameObject prefabToPreview = tool.selectedPrefab;
            if (tool.enableSmartPlacement && layerMode == PlacementLayerMode.Ground)
            {
                // Use the currently selected smart placement variant
                GameObject selectedVariant = tool.GetCurrentSmartPlacementVariant();
                if (selectedVariant != null)
                {
                    prefabToPreview = selectedVariant;
                }
                else
                {
                    // NEW: Use smart placement with user's rotation
                    HexTileType tileType = InferTileType(tool.selectedPrefab);
                    SmartTileSelector.SelectionResult selection = SmartTileSelector.SelectTilePrefabWithRotation(
                        hex, grid, tileType, tool.selectedPrefab, tool.rotation);
                        
                    if (selection.prefab != null)
                    {
                        prefabToPreview = selection.prefab;
                        // User rotation is always preserved
                        tool.rotation = selection.rotationDegrees / 60;
                        lastHoveredRotation = tool.rotation;
                    }
                }
            }

            // Check if we need to recreate the preview (prefab or hex changed)
            bool needsRecreate = previewInstance == null || 
                                previewPrefab != prefabToPreview || 
                                !previewHex.HasValue || 
                                previewHex.Value != hex;

            if (needsRecreate)
            {
                CleanupPreview();
                
                // Create new preview instance
                previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPreview);
                previewInstance.hideFlags = HideFlags.HideAndDontSave;
                previewInstance.name = "HexEditorPreview";
                previewInstance.SetActive(true);
                
                // Make it non-selectable by disabling colliders
                foreach (var collider in previewInstance.GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }
                
                // Apply transparency to all renderers
                ApplyTransparencyToPreview(previewInstance, 0.5f);
                
                previewPrefab = prefabToPreview;
                previewHex = hex;
            }
            
            // Always update position and rotation (even if instance wasn't recreated)
            // This ensures rotation changes from arrow keys are reflected immediately

            // Calculate position and rotation
            float heightOffset = 0f;
            if (layerMode == PlacementLayerMode.Object && existingData != null)
            {
                if (existingData.HasObjects())
                {
                    // Find the highest object to stack on top
                    float maxHeight = 0f;
                    foreach (var layer in existingData.objectLayers)
                    {
                        if (layer.instance != null)
                        {
                            float objHeight = layer.instance.transform.position.y;
                            if (objHeight > maxHeight)
                                maxHeight = objHeight;
                        }
                    }
                    
                    // Get bounds of the new prefab to calculate stacking height
                    Bounds? bounds = GetPrefabBounds(prefabToPreview);
                    if (bounds.HasValue)
                    {
                        heightOffset = maxHeight + bounds.Value.size.y * 0.5f - center.y;
                    }
                }
                else
                {
                    heightOffset = 0.1f; // Small offset to prevent z-fighting
                }
            }

            Vector3 previewPos = center + Vector3.up * heightOffset;
            Quaternion previewRot = Quaternion.Euler(0, tool.GetRotationDegrees(), 0);

            // Update preview instance transform
            if (previewInstance != null)
            {
                previewInstance.transform.position = previewPos;
                previewInstance.transform.rotation = previewRot;
            }

            // Draw hex outline with appropriate color
            Color previewColor = tool.previewColor;
            string statusText = "";
            
            if (layerMode == PlacementLayerMode.Ground)
            {
                if (existingData != null && existingData.HasGroundTile())
                {
                    previewColor = new Color(1f, 0.8f, 0f, 0.3f); // Yellow/orange for replacement
                    statusText = "Will replace ground";
                }
                else
                {
                    previewColor = new Color(0f, 1f, 0f, 0.3f); // Green for new ground
                    statusText = "New ground tile";
                }
            }
            else // Object placement
            {
                if (existingData != null && existingData.HasGroundTile())
                {
                    previewColor = new Color(0f, 0.5f, 1f, 0.3f); // Blue for object on ground
                    statusText = $"Object on ground (layer {existingData.ObjectLayerCount + 1})";
                }
                else
                {
                    previewColor = new Color(1f, 0.5f, 0f, 0.3f); // Orange warning - no ground
                    statusText = "Warning: No ground tile";
                }
            }
            
            DrawHexHighlight(center, grid.HexSize, previewColor);
            
            // Show status text
            if (!string.IsNullOrEmpty(statusText))
            {
                Vector3 labelPos = center + Vector3.up * 0.5f;
                Handles.Label(labelPos, statusText, new GUIStyle { fontSize = 10, normal = { textColor = Color.white } });
            }
        }

        private void CleanupPreview()
        {
            if (previewInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(previewInstance);
                }
                else
                {
                    DestroyImmediate(previewInstance);
                }
                previewInstance = null;
            }
            previewHex = null;
            previewPrefab = null;
        }

        private void CyclePrefabInGroup(bool next)
        {
            // Use the selected category from the UI, not the current prefab's category
            string group = selectedCategory;
            if (string.IsNullOrEmpty(group))
            {
                var categories = PrefabBrowser.GetCategories();
                if (categories.Count == 0) return;
                group = categories[0];
                selectedCategory = group;
            }

            var prefabs = PrefabBrowser.GetPrefabsInCategory(group);
            if (prefabs == null || prefabs.Count == 0) return;

            // Find current prefab index in this category
            int idx = -1;
            if (tool?.selectedPrefab != null)
            {
                idx = prefabs.IndexOf(tool.selectedPrefab);
            }
            
            // If current prefab not in this category, start from beginning
            if (idx < 0) idx = next ? -1 : 0;

            int newIdx = next
                ? (idx + 1) % prefabs.Count
                : (idx - 1 + prefabs.Count) % prefabs.Count;
            GameObject nextPrefab = prefabs[newIdx];

            tool.selectedPrefab = nextPrefab;
            EditorUtility.SetDirty(tool);
            CleanupPreview();
            SceneView.RepaintAll();
            EditorGUIUtility.PingObject(nextPrefab);
        }

        private void ApplyTransparencyToPreview(GameObject obj, float alpha)
        {
            if (obj == null)
                return;

            // Apply to all renderers in the object and children
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                Material[] newMaterials = new Material[materials.Length];

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                        continue;

                    // Create a transparent material copy
                    Material transparentMat = new Material(materials[i]);
                    transparentMat.name = materials[i].name + "_Preview";
                    
                    // Enable transparency
                    if (transparentMat.HasProperty("_Mode"))
                    {
                        transparentMat.SetFloat("_Mode", 3); // Fade mode
                        transparentMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        transparentMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        transparentMat.SetInt("_ZWrite", 0);
                        transparentMat.DisableKeyword("_ALPHATEST_ON");
                        transparentMat.EnableKeyword("_ALPHABLEND_ON");
                        transparentMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        transparentMat.renderQueue = 3000;
                    }

                    // Set alpha
                    if (transparentMat.HasProperty("_Color"))
                    {
                        Color color = transparentMat.color;
                        color.a = alpha;
                        transparentMat.color = color;
                    }
                    else if (transparentMat.HasProperty("_BaseColor"))
                    {
                        Color color = transparentMat.GetColor("_BaseColor");
                        color.a = alpha;
                        transparentMat.SetColor("_BaseColor", color);
                    }

                    newMaterials[i] = transparentMat;
                }

                renderer.sharedMaterials = newMaterials;
            }
        }

        private void DrawHexHighlight(Vector3 center, float size, Color color)
        {
            Handles.color = color;
            
            // Draw filled hex
            Vector3[] corners = new Vector3[6];
            float sqrt3 = Mathf.Sqrt(3f);
            
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60f - 30f) * Mathf.Deg2Rad; // Start at top
                corners[i] = center + new Vector3(
                    size * Mathf.Cos(angle),
                    0f,
                    size * Mathf.Sin(angle)
                );
            }
            
            // Draw filled polygon
            Handles.DrawAAConvexPolygon(corners);
            
            // Draw outline
            Handles.color = new Color(color.r, color.g, color.b, 1f);
            Handles.DrawPolyLine(corners);
            Handles.DrawLine(corners[5], corners[0]); // Close the loop
        }

        private void DrawDirectionLabels(HexCoordinate centerHex)
        {
            string[] directionNames = { "E", "NE", "NW", "W", "SW", "SE" };
            string[] screenDirectionNames = { "E", "SE", "SW", "W", "NW", "NE" }; // N↔S flipped for screen
            HexCoordinate[] neighbors = centerHex.GetNeighbors();
            
            for (int i = 0; i < 6; i++)
            {
                if (!grid.IsInBounds(neighbors[i]))
                    continue;
                    
                Vector3 neighborPos = grid.HexToWorld(neighbors[i]);
                Vector3 labelPos = neighborPos + Vector3.up * 0.5f;
                
                // Screen has N↔S flipped compared to geometric coordinates
                string screenLabel = screenDirectionNames[i];
                
                string label = $"Geo: {directionNames[i]}\nScreen: {screenLabel}";
                
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.yellow;
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 10;
                
                Handles.Label(labelPos, label, style);
            }
        }

        private void DrawSmartPlacementHint(Vector3 center, HexCoordinate hex)
        {
            if (tool.selectedPrefab == null)
                return;

            int variantCount = tool.GetSmartPlacementVariantCount();
            if (variantCount > 1)
            {
                GameObject currentVariant = tool.GetCurrentSmartPlacementVariant();
                int currentIndex = tool.GetSmartPlacementVariantIndex();
                
                Vector3 hintPos = center + Vector3.up * 0.5f;
                string hintText = $"Smart: {currentVariant?.name ?? "Unknown"} ({currentIndex + 1}/{variantCount})";
                hintText += "\nUp/Down: Cycle variants | Left/Right: Rotate";
                
                Handles.Label(hintPos, hintText, new GUIStyle 
                { 
                    fontSize = 10, 
                    normal = { textColor = Color.yellow },
                    alignment = TextAnchor.MiddleCenter
                });
            }
            else
            {
                // Fallback: show original smart placement hint with rotation
                HexTileType tileType = InferTileType(tool.selectedPrefab);
                SmartTileSelector.SelectionResult selection = SmartTileSelector.SelectTilePrefabWithRotation(hex, grid, tileType, tool.selectedPrefab);
                
                if (selection.prefab != null && selection.prefab != tool.selectedPrefab)
                {
                    Vector3 hintPos = center + Vector3.up * 0.5f;
                    Handles.Label(hintPos, $"Smart: {selection.prefab.name} ({selection.rotationDegrees}°)", new GUIStyle { fontSize = 10, normal = { textColor = Color.yellow } });
                }
            }
        }

        private void ClearGrid()
        {
            if (grid == null)
                return;

            // Record undo
            Undo.RecordObject(grid, "Clear Hex Grid");
            if (!Application.isPlaying)
            {
                Undo.RecordObject(tool, "Clear Hex Grid");
            }

            // Get all hexes and destroy their objects
            var allHexes = grid.GetAllHexes();
            int groundCount = 0;
            int objectCount = 0;
            
            foreach (var hex in allHexes)
            {
                HexTileData tileData = grid.GetTile(hex);
                if (tileData != null)
                {
                    // Delete ground tile
                    if (tileData.groundTileInstance != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(tileData.groundTileInstance);
                        }
                        else
                        {
                            Undo.DestroyObjectImmediate(tileData.groundTileInstance);
                        }
                        groundCount++;
                    }
                    
                    // Delete all object layers
                    if (tileData.objectLayers != null)
                    {
                        foreach (var layer in tileData.objectLayers)
                        {
                            if (layer.instance != null)
                            {
                                if (Application.isPlaying)
                                {
                                    Destroy(layer.instance);
                                }
                                else
                                {
                                    Undo.DestroyObjectImmediate(layer.instance);
                                }
                                objectCount++;
                            }
                        }
                    }
                }
            }

            // Clear the grid
            grid.Clear();

            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(grid);
                EditorUtility.SetDirty(tool);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            Debug.Log($"Cleared {groundCount} ground tiles and {objectCount} objects from hex grid");
        }

        private Bounds? GetPrefabBounds(GameObject prefab)
        {
            if (prefab == null)
                return null;

            // Get mesh bounds from prefab
            MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                return meshFilter.sharedMesh.bounds;
            }

            // Fallback: try to get renderer bounds
            Renderer renderer = prefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds;
            }

            return null;
        }
    }
}