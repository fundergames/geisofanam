using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace RogueDeal.HexLevels.Editor
{
    public class HexEditorUIController : EditorWindow
    {
        [MenuItem("Window/Hex Editor UI")]
        public static void ShowWindow()
        {
            HexEditorUIController wnd = GetWindow<HexEditorUIController>();
            wnd.titleContent = new GUIContent("Hex Editor");
            wnd.minSize = new Vector2(800, 600);
        }
        
        private HexEditorState editorState;
        private HexGrid hexGrid;
        private HexEditorCommandBus commandBus;
        private AssetCatalog assetCatalog;
        
        private VisualElement leftPanel;
        private VisualElement centerPanel;
        private VisualElement rightPanel;
        private VisualElement assetBrowserPanel;
        private VisualElement inspectorPanel;
        
        private Label statusLabel;
        private Label hoverInfoLabel;
        private ScrollView assetGridView;
        
        private string currentSearchFilter = "";
        private AssetCategory currentCategory = AssetCategory.All;
        private List<AssetCatalog.AssetEntry> displayedAssets = new List<AssetCatalog.AssetEntry>();
        
        private void OnEnable()
        {
            Initialize();
            BuildUI();
            BindUI();
            RefreshAssetGrid();
        }
        
        private void Initialize()
        {
            if (editorState == null)
            {
                editorState = CreateInstance<HexEditorState>();
                editorState.OnStateChanged += OnStateChanged;
            }
            
            hexGrid = FindObjectOfType<HexGrid>();
            
            if (hexGrid != null && commandBus == null)
            {
                commandBus = new HexEditorCommandBus(hexGrid, editorState);
            }
            
            assetCatalog = FindAssetCatalog();
        }
        
        private AssetCatalog FindAssetCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:AssetCatalog");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AssetCatalog>(path);
            }
            
            AssetCatalog catalog = CreateInstance<AssetCatalog>();
            catalog.AutoPopulateFromPrefabBrowser();
            AssetDatabase.CreateAsset(catalog, "Assets/RogueDeal/Resources/Data/HexLevels/AssetCatalog.asset");
            AssetDatabase.SaveAssets();
            return catalog;
        }
        
        private void BuildUI()
        {
            rootVisualElement.Clear();
            
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.flexGrow = 1;
            
            leftPanel = BuildLeftPanel();
            centerPanel = BuildCenterPanel();
            rightPanel = BuildRightPanel();
            
            container.Add(leftPanel);
            container.Add(centerPanel);
            container.Add(rightPanel);
            
            rootVisualElement.Add(container);
            
            ApplyStyles();
        }
        
        private VisualElement BuildLeftPanel()
        {
            var panel = new VisualElement();
            panel.style.width = 200;
            panel.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 0.95f);
            panel.style.paddingTop = 10;
            panel.style.paddingBottom = 10;
            panel.style.paddingLeft = 10;
            panel.style.paddingRight = 10;
            
            var fileLabel = new Label("File");
            fileLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            fileLabel.style.fontSize = 14;
            fileLabel.style.marginBottom = 5;
            panel.Add(fileLabel);
            
            var newBtn = new Button(() => OnNewMap()) { text = "New" };
            var loadBtn = new Button(() => OnLoadMap()) { text = "Load..." };
            var saveBtn = new Button(() => OnSaveMap()) { text = "Save" };
            var saveAsBtn = new Button(() => OnSaveAsMap()) { text = "Save As..." };
            
            panel.Add(newBtn);
            panel.Add(loadBtn);
            panel.Add(saveBtn);
            panel.Add(saveAsBtn);
            
            var autosaveToggle = new Toggle("Autosave");
            autosaveToggle.value = editorState.autosave;
            autosaveToggle.RegisterValueChangedCallback(evt => editorState.autosave = evt.newValue);
            panel.Add(autosaveToggle);
            
            return panel;
        }
        
        private VisualElement BuildCenterPanel()
        {
            var panel = new VisualElement();
            panel.style.flexGrow = 1;
            panel.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.8f);
            panel.style.paddingTop = 10;
            panel.style.paddingLeft = 10;
            panel.style.paddingRight = 10;
            
            var contextLabel = new Label("Context / Status");
            contextLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            contextLabel.style.fontSize = 14;
            contextLabel.style.marginBottom = 10;
            panel.Add(contextLabel);
            
            statusLabel = new Label();
            statusLabel.style.marginBottom = 5;
            panel.Add(statusLabel);
            
            hoverInfoLabel = new Label();
            hoverInfoLabel.style.marginBottom = 10;
            hoverInfoLabel.style.color = new Color(0.7f, 0.9f, 1f);
            panel.Add(hoverInfoLabel);
            
            var modeContainer = BuildModeSelector();
            panel.Add(modeContainer);
            
            var layerContainer = BuildLayerSelector();
            panel.Add(layerContainer);
            
            var elevationContainer = BuildElevationControls();
            panel.Add(elevationContainer);
            
            var brushContainer = BuildBrushControls();
            panel.Add(brushContainer);
            
            var splitView = new TwoPaneSplitView(0, 400, TwoPaneSplitViewOrientation.Vertical);
            
            assetBrowserPanel = BuildAssetBrowser();
            inspectorPanel = BuildInspector();
            
            splitView.Add(assetBrowserPanel);
            splitView.Add(inspectorPanel);
            
            panel.Add(splitView);
            
            return panel;
        }
        
        private VisualElement BuildRightPanel()
        {
            var panel = new VisualElement();
            panel.style.width = 200;
            panel.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 0.95f);
            panel.style.paddingTop = 10;
            panel.style.paddingBottom = 10;
            panel.style.paddingLeft = 10;
            panel.style.paddingRight = 10;
            
            var togglesLabel = new Label("Global Toggles");
            togglesLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            togglesLabel.style.fontSize = 14;
            togglesLabel.style.marginBottom = 5;
            panel.Add(togglesLabel);
            
            var gridToggle = new Toggle("Grid Visibility");
            gridToggle.value = editorState.gridVisible;
            gridToggle.RegisterValueChangedCallback(evt => editorState.gridVisible = evt.newValue);
            panel.Add(gridToggle);
            
            var snapToggle = new Toggle("Collision Snap");
            snapToggle.value = editorState.collisionSnap;
            snapToggle.RegisterValueChangedCallback(evt => editorState.collisionSnap = evt.newValue);
            panel.Add(snapToggle);
            
            var gizmoToggle = new Toggle("Gizmos");
            gizmoToggle.value = editorState.gizmoVisible;
            gizmoToggle.RegisterValueChangedCallback(evt => editorState.gizmoVisible = evt.newValue);
            panel.Add(gizmoToggle);
            
            var previewToggle = new Toggle("Show Preview");
            previewToggle.value = editorState.showPreview;
            previewToggle.RegisterValueChangedCallback(evt => editorState.showPreview = evt.newValue);
            panel.Add(previewToggle);
            
            return panel;
        }
        
        private VisualElement BuildModeSelector()
        {
            var container = new VisualElement();
            container.style.marginBottom = 10;
            
            var label = new Label("Mode");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 3;
            container.Add(label);
            
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            
            var placeBtn = CreateModeButton("Place", HexEditorMode.Place);
            var eraseBtn = CreateModeButton("Erase", HexEditorMode.Erase);
            var editBtn = CreateModeButton("Edit", HexEditorMode.Edit);
            
            buttonRow.Add(placeBtn);
            buttonRow.Add(eraseBtn);
            buttonRow.Add(editBtn);
            
            container.Add(buttonRow);
            
            var hotkeysLabel = new Label("Hotkeys: 1=Place, 2=Erase, 3=Edit, Esc=Cancel");
            hotkeysLabel.style.fontSize = 9;
            hotkeysLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            container.Add(hotkeysLabel);
            
            return container;
        }
        
        private Button CreateModeButton(string text, HexEditorMode mode)
        {
            var btn = new Button(() => editorState.SetMode(mode));
            btn.text = text;
            btn.style.flexGrow = 1;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            if (editorState.mode == mode)
            {
                btn.style.backgroundColor = new Color(1f, 0.8f, 0.2f);
            }
            
            return btn;
        }
        
        private VisualElement BuildLayerSelector()
        {
            var container = new VisualElement();
            container.style.marginBottom = 10;
            
            var label = new Label("Layer");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 3;
            container.Add(label);
            
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            
            var tilesBtn = CreateLayerButton("Tiles", HexEditorLayer.Tiles);
            var decosBtn = CreateLayerButton("Decorations", HexEditorLayer.Decorations);
            
            buttonRow.Add(tilesBtn);
            buttonRow.Add(decosBtn);
            
            container.Add(buttonRow);
            
            var hotkeysLabel = new Label("Hotkeys: Tab=Toggle Layer");
            hotkeysLabel.style.fontSize = 9;
            hotkeysLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            container.Add(hotkeysLabel);
            
            return container;
        }
        
        private Button CreateLayerButton(string text, HexEditorLayer layer)
        {
            var btn = new Button(() => editorState.SetLayer(layer));
            btn.text = text;
            btn.style.flexGrow = 1;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            if (editorState.layer == layer)
            {
                btn.style.backgroundColor = new Color(1f, 0.8f, 0.2f);
            }
            
            return btn;
        }
        
        private VisualElement BuildElevationControls()
        {
            var container = new VisualElement();
            container.style.marginBottom = 10;
            
            var label = new Label("Elevation");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 3;
            container.Add(label);
            
            var elevRow = new VisualElement();
            elevRow.style.flexDirection = FlexDirection.Row;
            elevRow.style.alignItems = Align.Center;
            
            var decreaseBtn = new Button(() => editorState.SetElevation(editorState.elevation - 1));
            decreaseBtn.text = "-";
            decreaseBtn.style.width = 30;
            
            var elevLabel = new Label(editorState.elevation.ToString());
            elevLabel.style.width = 40;
            elevLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            var increaseBtn = new Button(() => editorState.SetElevation(editorState.elevation + 1));
            increaseBtn.text = "+";
            increaseBtn.style.width = 30;
            
            elevRow.Add(decreaseBtn);
            elevRow.Add(elevLabel);
            elevRow.Add(increaseBtn);
            
            container.Add(elevRow);
            
            var hotkeysLabel = new Label("Hotkeys: [=Down, ]=Up");
            hotkeysLabel.style.fontSize = 9;
            hotkeysLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            container.Add(hotkeysLabel);
            
            return container;
        }
        
        private VisualElement BuildBrushControls()
        {
            var container = new VisualElement();
            container.style.marginBottom = 10;
            container.style.display = editorState.mode == HexEditorMode.Place ? DisplayStyle.Flex : DisplayStyle.None;
            
            var label = new Label("Brush");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 3;
            container.Add(label);
            
            var sizeRow = new VisualElement();
            sizeRow.style.flexDirection = FlexDirection.Row;
            
            var size1Btn = new Button(() => editorState.brushSize = 1) { text = "1" };
            var size2Btn = new Button(() => editorState.brushSize = 2) { text = "2" };
            var size3Btn = new Button(() => editorState.brushSize = 3) { text = "3" };
            
            size1Btn.style.flexGrow = 1;
            size2Btn.style.flexGrow = 1;
            size3Btn.style.flexGrow = 1;
            
            if (editorState.brushSize == 1) size1Btn.style.backgroundColor = new Color(1f, 0.8f, 0.2f);
            if (editorState.brushSize == 2) size2Btn.style.backgroundColor = new Color(1f, 0.8f, 0.2f);
            if (editorState.brushSize == 3) size3Btn.style.backgroundColor = new Color(1f, 0.8f, 0.2f);
            
            sizeRow.Add(size1Btn);
            sizeRow.Add(size2Btn);
            sizeRow.Add(size3Btn);
            
            container.Add(sizeRow);
            
            var dragToggle = new Toggle("Drag Paint");
            dragToggle.value = editorState.dragPaint;
            dragToggle.RegisterValueChangedCallback(evt => editorState.dragPaint = evt.newValue);
            container.Add(dragToggle);
            
            return container;
        }
        
        private VisualElement BuildAssetBrowser()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.paddingTop = 10;
            
            var headerLabel = new Label("Asset Browser");
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.fontSize = 14;
            headerLabel.style.marginBottom = 5;
            container.Add(headerLabel);
            
            var searchField = new TextField("Search");
            searchField.RegisterValueChangedCallback(evt => {
                currentSearchFilter = evt.newValue;
                RefreshAssetGrid();
            });
            container.Add(searchField);
            
            var categoryDropdown = new EnumField("Category", AssetCategory.All);
            categoryDropdown.RegisterValueChangedCallback(evt => {
                currentCategory = (AssetCategory)evt.newValue;
                RefreshAssetGrid();
            });
            container.Add(categoryDropdown);
            
            assetGridView = new ScrollView();
            assetGridView.style.flexGrow = 1;
            assetGridView.style.marginTop = 10;
            container.Add(assetGridView);
            
            var hotkeysLabel = new Label("Hotkeys: Q/E=Cycle Assets, F=Favorite");
            hotkeysLabel.style.fontSize = 9;
            hotkeysLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            container.Add(hotkeysLabel);
            
            return container;
        }
        
        private VisualElement BuildInspector()
        {
            var container = new VisualElement();
            container.style.paddingTop = 10;
            
            var headerLabel = new Label("Inspector / Preview");
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.fontSize = 14;
            headerLabel.style.marginBottom = 5;
            container.Add(headerLabel);
            
            if (editorState.activeAsset != null)
            {
                var assetLabel = new Label(editorState.activeAsset.name);
                assetLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                container.Add(assetLabel);
                
                var rotationContainer = new VisualElement();
                rotationContainer.style.flexDirection = FlexDirection.Row;
                rotationContainer.style.alignItems = Align.Center;
                rotationContainer.style.marginTop = 10;
                
                var rotLabel = new Label("Rotation:");
                rotLabel.style.width = 60;
                
                var rotDecBtn = new Button(() => editorState.RotateCounterClockwise());
                rotDecBtn.text = "⟲";
                rotDecBtn.style.width = 30;
                
                var rotValueLabel = new Label($"{editorState.GetRotationDegrees()}°");
                rotValueLabel.style.width = 50;
                rotValueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                
                var rotIncBtn = new Button(() => editorState.RotateClockwise());
                rotIncBtn.text = "⟳";
                rotIncBtn.style.width = 30;
                
                rotationContainer.Add(rotLabel);
                rotationContainer.Add(rotDecBtn);
                rotationContainer.Add(rotValueLabel);
                rotationContainer.Add(rotIncBtn);
                
                container.Add(rotationContainer);
                
                var hotkeysLabel = new Label("Hotkeys: R=Rotate+, Shift+R=Rotate-");
                hotkeysLabel.style.fontSize = 9;
                hotkeysLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                container.Add(hotkeysLabel);
            }
            
            return container;
        }
        
        private void RefreshAssetGrid()
        {
            if (assetCatalog == null || assetGridView == null)
                return;
            
            assetGridView.Clear();
            
            displayedAssets = assetCatalog.entries;
            
            if (!string.IsNullOrEmpty(currentSearchFilter))
            {
                displayedAssets = assetCatalog.Search(currentSearchFilter);
            }
            else if (currentCategory != AssetCategory.All)
            {
                displayedAssets = assetCatalog.GetEntriesByCategory(currentCategory);
            }
            
            foreach (var entry in displayedAssets)
            {
                var assetCard = CreateAssetCard(entry);
                assetGridView.Add(assetCard);
            }
        }
        
        private VisualElement CreateAssetCard(AssetCatalog.AssetEntry entry)
        {
            var card = new VisualElement();
            card.style.flexDirection = FlexDirection.Row;
            card.style.marginBottom = 5;
            card.style.paddingTop = 5;
            card.style.paddingBottom = 5;
            card.style.paddingLeft = 5;
            card.style.paddingRight = 5;
            card.style.backgroundColor = editorState.activeAsset == entry.prefab 
                ? new Color(1f, 0.8f, 0.2f, 0.3f) 
                : new Color(0.3f, 0.3f, 0.3f, 0.3f);
            
            var thumbnail = new VisualElement();
            thumbnail.style.width = 40;
            thumbnail.style.height = 40;
            thumbnail.style.marginRight = 5;
            
            Texture2D preview = AssetPreview.GetAssetPreview(entry.prefab);
            if (preview != null)
            {
                thumbnail.style.backgroundImage = new StyleBackground(preview);
            }
            
            card.Add(thumbnail);
            
            var infoContainer = new VisualElement();
            infoContainer.style.flexGrow = 1;
            
            var nameLabel = new Label(entry.displayName);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            infoContainer.Add(nameLabel);
            
            if (entry.tags != null && entry.tags.Length > 0)
            {
                var tagsLabel = new Label(string.Join(", ", entry.tags));
                tagsLabel.style.fontSize = 9;
                tagsLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                infoContainer.Add(tagsLabel);
            }
            
            card.Add(infoContainer);
            
            var favBtn = new Button(() => {
                assetCatalog.ToggleFavorite(entry.id);
                RefreshAssetGrid();
            });
            favBtn.text = entry.isFavorite ? "★" : "☆";
            favBtn.style.width = 30;
            card.Add(favBtn);
            
            card.RegisterCallback<ClickEvent>(evt => {
                editorState.SetActiveAsset(entry.prefab);
                RefreshAssetGrid();
            });
            
            return card;
        }
        
        private void BindUI()
        {
            UpdateStatusDisplay();
        }
        
        private void OnStateChanged()
        {
            UpdateStatusDisplay();
            BuildUI();
        }
        
        private void UpdateStatusDisplay()
        {
            if (statusLabel != null)
            {
                statusLabel.text = $"Mode: {editorState.mode} | Layer: {editorState.layer} | Elevation: {editorState.elevation}";
            }
            
            if (hoverInfoLabel != null && editorState.hoveredHex.HasValue)
            {
                HexCoordinate hex = editorState.hoveredHex.Value;
                string info = $"Hex ({hex.q},{hex.r})";
                
                if (editorState.hoveredTileData != null)
                {
                    if (editorState.hoveredTileData.HasGroundTile())
                    {
                        info += $" | Tile: {editorState.hoveredTileData.tileType}";
                    }
                    if (editorState.hoveredTileData.HasObjects())
                    {
                        info += $" | Objects: {editorState.hoveredTileData.ObjectLayerCount}";
                    }
                }
                
                hoverInfoLabel.text = info;
            }
            else if (hoverInfoLabel != null)
            {
                hoverInfoLabel.text = "No hex hovered";
            }
        }
        
        private void ApplyStyles()
        {
            rootVisualElement.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
        }
        
        private void OnNewMap()
        {
            if (EditorUtility.DisplayDialog("New Map", "Clear the current map and start fresh?", "Yes", "Cancel"))
            {
                if (hexGrid != null)
                {
                    hexGrid.Clear();
                }
            }
        }
        
        private void OnLoadMap()
        {
            string path = EditorUtility.OpenFilePanel("Load Hex Map", "Assets", "json");
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log($"Loading map from: {path}");
            }
        }
        
        private void OnSaveMap()
        {
            Debug.Log("Saving current map...");
        }
        
        private void OnSaveAsMap()
        {
            string path = EditorUtility.SaveFilePanel("Save Hex Map As", "Assets", "hexmap", "json");
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log($"Saving map to: {path}");
            }
        }
        
        private void OnDisable()
        {
            if (editorState != null)
            {
                editorState.OnStateChanged -= OnStateChanged;
            }
        }
    }
}
