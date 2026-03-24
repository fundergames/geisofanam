using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace RogueDeal.HexLevels.Runtime
{
    [RequireComponent(typeof(UIDocument))]
    public class HexEditorUICompact : MonoBehaviour
    {
        [Header("References")]
        public HexEditorRuntimeState editorState;
        public HexGrid hexGrid;
        public AssetCatalog assetCatalog;
        
        private UIDocument uiDocument;
        private VisualElement root;
        
        private Label statusLabel;
        private Label hoveredHexLabel;
        private Label elevationLabel;
        
        private Button modePlaceBtn;
        private Button modeEraseBtn;
        private Button modeEditBtn;
        
        private Button layerTilesBtn;
        private Button layerDecorationsBtn;
        
        private Toggle showPreviewToggle;
        private Toggle showGridToggle;
        private Toggle snapToGridToggle;
        
        private VisualElement assetBrowserPanel;
        private ScrollView assetGrid;
        private TextField searchField;
        private DropdownField categoryFilter;
        private DropdownField tagFilter;
        
        private AssetCategory selectedCategory = AssetCategory.All;
        private string selectedTag = "All";
        
        private List<AssetCatalog.AssetEntry> displayedAssets = new List<AssetCatalog.AssetEntry>();
        
        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            
            if (editorState == null)
            {
                editorState = FindFirstObjectByType<HexEditorRuntimeState>();
            }
            
            if (hexGrid == null)
            {
                hexGrid = FindFirstObjectByType<HexGrid>();
            }
            
            if (assetCatalog == null)
            {
                assetCatalog = Resources.Load<AssetCatalog>("Data/HexLevels/AssetCatalog");
            }
        }
        
        private void OnEnable()
        {
            BuildUI();
            
            if (editorState != null)
            {
                editorState.OnStateChanged += UpdateUI;
            }
            
            RefreshAssetGrid();
            UpdateUI();
        }
        
        private void OnDisable()
        {
            if (editorState != null)
            {
                editorState.OnStateChanged -= UpdateUI;
            }
        }
        
        private void Update()
        {
            UpdateStatusDisplay();
        }
        
        private void BuildUI()
        {
            root = uiDocument.rootVisualElement;
            root.Clear();
            
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.pickingMode = PickingMode.Ignore;
            
            BuildTopBar();
            BuildAssetBrowser();
            BuildSettingsPanel();
        }
        
        private void BuildTopBar()
        {
            VisualElement topBar = new VisualElement();
            topBar.style.position = Position.Absolute;
            topBar.style.top = 10;
            topBar.style.left = 10;
            topBar.style.right = 10;
            topBar.style.height = 50;
            topBar.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            topBar.style.borderBottomWidth = 2;
            topBar.style.borderBottomColor = new Color(0.3f, 0.6f, 1f, 0.8f);
            topBar.style.borderTopLeftRadius = 8;
            topBar.style.borderTopRightRadius = 8;
            topBar.style.borderBottomLeftRadius = 8;
            topBar.style.borderBottomRightRadius = 8;
            topBar.style.paddingLeft = 15;
            topBar.style.paddingRight = 15;
            topBar.style.paddingTop = 8;
            topBar.style.paddingBottom = 8;
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.alignItems = Align.Center;
            topBar.pickingMode = PickingMode.Position;
            
            statusLabel = new Label("Ready");
            statusLabel.style.fontSize = 13;
            statusLabel.style.color = Color.white;
            statusLabel.style.marginRight = 15;
            statusLabel.style.minWidth = 80;
            topBar.Add(statusLabel);
            
            hoveredHexLabel = new Label("Hex: ---");
            hoveredHexLabel.style.fontSize = 12;
            hoveredHexLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            hoveredHexLabel.style.marginRight = 20;
            hoveredHexLabel.style.minWidth = 100;
            topBar.Add(hoveredHexLabel);
            
            BuildModeButtons(topBar);
            
            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            topBar.Add(spacer);
            
            BuildLayerButtons(topBar);
            BuildElevationButtons(topBar);
            
            root.Add(topBar);
        }
        
        private void BuildModeButtons(VisualElement parent)
        {
            VisualElement modeGroup = new VisualElement();
            modeGroup.style.flexDirection = FlexDirection.Row;
            modeGroup.style.marginRight = 15;
            
            modePlaceBtn = new Button(() => editorState?.SetMode(RuntimeEditorMode.Place)) { text = "Place (1)" };
            modePlaceBtn.style.width = 90;
            modePlaceBtn.style.height = 32;
            modePlaceBtn.style.marginRight = 3;
            modeGroup.Add(modePlaceBtn);
            
            modeEraseBtn = new Button(() => editorState?.SetMode(RuntimeEditorMode.Erase)) { text = "Erase (2)" };
            modeEraseBtn.style.width = 90;
            modeEraseBtn.style.height = 32;
            modeEraseBtn.style.marginRight = 3;
            modeGroup.Add(modeEraseBtn);
            
            modeEditBtn = new Button(() => editorState?.SetMode(RuntimeEditorMode.Edit)) { text = "Edit (3)" };
            modeEditBtn.style.width = 80;
            modeEditBtn.style.height = 32;
            modeGroup.Add(modeEditBtn);
            
            parent.Add(modeGroup);
        }
        
        private void BuildLayerButtons(VisualElement parent)
        {
            VisualElement layerGroup = new VisualElement();
            layerGroup.style.flexDirection = FlexDirection.Row;
            layerGroup.style.marginRight = 15;
            
            layerTilesBtn = new Button(() => editorState?.SetLayer(RuntimeEditorLayer.Tiles)) { text = "Tiles" };
            layerTilesBtn.style.width = 70;
            layerTilesBtn.style.height = 32;
            layerTilesBtn.style.marginRight = 3;
            layerGroup.Add(layerTilesBtn);
            
            layerDecorationsBtn = new Button(() => editorState?.SetLayer(RuntimeEditorLayer.Decorations)) { text = "Decor" };
            layerDecorationsBtn.style.width = 70;
            layerDecorationsBtn.style.height = 32;
            layerGroup.Add(layerDecorationsBtn);
            
            parent.Add(layerGroup);
        }
        
        private void BuildElevationButtons(VisualElement parent)
        {
            VisualElement elevGroup = new VisualElement();
            elevGroup.style.flexDirection = FlexDirection.Row;
            elevGroup.style.alignItems = Align.Center;
            
            Label elevLabel = new Label("Elev:");
            elevLabel.style.fontSize = 12;
            elevLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            elevLabel.style.marginRight = 5;
            elevGroup.Add(elevLabel);
            
            Button elevDownBtn = new Button(() => editorState?.SetElevation(editorState.elevation - 1)) { text = "-" };
            elevDownBtn.style.width = 28;
            elevDownBtn.style.height = 28;
            elevGroup.Add(elevDownBtn);
            
            elevationLabel = new Label("0");
            elevationLabel.style.fontSize = 13;
            elevationLabel.style.color = Color.white;
            elevationLabel.style.marginLeft = 8;
            elevationLabel.style.marginRight = 8;
            elevationLabel.style.minWidth = 25;
            elevationLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            elevGroup.Add(elevationLabel);
            
            Button elevUpBtn = new Button(() => editorState?.SetElevation(editorState.elevation + 1)) { text = "+" };
            elevUpBtn.style.width = 28;
            elevUpBtn.style.height = 28;
            elevGroup.Add(elevUpBtn);
            
            parent.Add(elevGroup);
        }
        
        private void BuildAssetBrowser()
        {
            assetBrowserPanel = new VisualElement();
            assetBrowserPanel.style.position = Position.Absolute;
            assetBrowserPanel.style.bottom = 10;
            assetBrowserPanel.style.left = 10;
            assetBrowserPanel.style.width = 300;
            assetBrowserPanel.style.height = 400;
            assetBrowserPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            assetBrowserPanel.style.borderTopLeftRadius = 8;
            assetBrowserPanel.style.borderTopRightRadius = 8;
            assetBrowserPanel.style.borderBottomLeftRadius = 8;
            assetBrowserPanel.style.borderBottomRightRadius = 8;
            assetBrowserPanel.style.paddingTop = 10;
            assetBrowserPanel.style.paddingBottom = 10;
            assetBrowserPanel.style.paddingLeft = 10;
            assetBrowserPanel.style.paddingRight = 10;
            assetBrowserPanel.pickingMode = PickingMode.Position;
            
            Label title = new Label("Asset Browser (Q/E)");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 8;
            assetBrowserPanel.Add(title);
            
            searchField = new TextField();
            searchField.value = "";
            searchField.RegisterValueChangedCallback(evt =>
            {
                RefreshAssetGrid();
            });
            searchField.style.marginBottom = 5;
            assetBrowserPanel.Add(searchField);
            
            List<string> categoryNames = new List<string>();
            foreach (AssetCategory cat in System.Enum.GetValues(typeof(AssetCategory)))
            {
                categoryNames.Add(cat.ToString());
            }
            
            categoryFilter = new DropdownField("Category", categoryNames, 0);
            categoryFilter.RegisterValueChangedCallback(evt =>
            {
                selectedCategory = (AssetCategory)System.Enum.Parse(typeof(AssetCategory), evt.newValue);
                selectedTag = "All";
                UpdateTagFilter();
                RefreshAssetGrid();
            });
            categoryFilter.style.marginBottom = 5;
            assetBrowserPanel.Add(categoryFilter);
            
            List<string> allTags = new List<string> { "All" };
            tagFilter = new DropdownField("Tag", allTags, 0);
            tagFilter.RegisterValueChangedCallback(evt =>
            {
                selectedTag = evt.newValue;
                RefreshAssetGrid();
            });
            tagFilter.style.marginBottom = 8;
            assetBrowserPanel.Add(tagFilter);
            
            VisualElement buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.marginBottom = 8;
            
            Button saveBtn = new Button(() => OnSaveMap()) { text = "Save" };
            saveBtn.style.height = 28;
            saveBtn.style.flexGrow = 1;
            saveBtn.style.marginRight = 3;
            buttonRow.Add(saveBtn);
            
            Button loadBtn = new Button(() => OnLoadMap()) { text = "Load" };
            loadBtn.style.height = 28;
            loadBtn.style.flexGrow = 1;
            buttonRow.Add(loadBtn);
            
            assetBrowserPanel.Add(buttonRow);
            
            assetGrid = new ScrollView();
            assetGrid.style.flexGrow = 1;
            assetBrowserPanel.Add(assetGrid);
            
            root.Add(assetBrowserPanel);
        }
        
        private void BuildSettingsPanel()
        {
            VisualElement settingsPanel = new VisualElement();
            settingsPanel.style.position = Position.Absolute;
            settingsPanel.style.bottom = 10;
            settingsPanel.style.right = 10;
            settingsPanel.style.width = 200;
            settingsPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            settingsPanel.style.borderTopLeftRadius = 8;
            settingsPanel.style.borderTopRightRadius = 8;
            settingsPanel.style.borderBottomLeftRadius = 8;
            settingsPanel.style.borderBottomRightRadius = 8;
            settingsPanel.style.paddingTop = 10;
            settingsPanel.style.paddingBottom = 10;
            settingsPanel.style.paddingLeft = 10;
            settingsPanel.style.paddingRight = 10;
            settingsPanel.pickingMode = PickingMode.Position;
            
            Label title = new Label("Settings");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 8;
            settingsPanel.Add(title);
            
            showPreviewToggle = new Toggle("Show Preview");
            showPreviewToggle.value = editorState != null ? editorState.showPreview : true;
            showPreviewToggle.RegisterValueChangedCallback(evt =>
            {
                if (editorState != null)
                {
                    editorState.showPreview = evt.newValue;
                }
            });
            showPreviewToggle.style.marginBottom = 5;
            settingsPanel.Add(showPreviewToggle);
            
            showGridToggle = new Toggle("Show Grid");
            showGridToggle.value = editorState != null ? editorState.showGrid : true;
            showGridToggle.RegisterValueChangedCallback(evt =>
            {
                if (editorState != null)
                {
                    editorState.showGrid = evt.newValue;
                    UpdateGridVisibility();
                }
            });
            showGridToggle.style.marginBottom = 5;
            settingsPanel.Add(showGridToggle);
            
            snapToGridToggle = new Toggle("Snap to Grid");
            snapToGridToggle.value = editorState != null ? editorState.snapToGrid : true;
            snapToGridToggle.RegisterValueChangedCallback(evt =>
            {
                if (editorState != null)
                {
                    editorState.snapToGrid = evt.newValue;
                }
            });
            snapToGridToggle.style.marginBottom = 5;
            settingsPanel.Add(snapToGridToggle);
            
            Toggle dragPaintToggle = new Toggle("Drag to Paint");
            dragPaintToggle.value = editorState != null ? editorState.dragPaint : false;
            dragPaintToggle.RegisterValueChangedCallback(evt =>
            {
                if (editorState != null)
                {
                    editorState.dragPaint = evt.newValue;
                }
            });
            settingsPanel.Add(dragPaintToggle);
            
            root.Add(settingsPanel);
        }
        
        private void UpdateGridVisibility()
        {
            if (hexGrid == null || editorState == null)
                return;
            
            HexGridRuntimeVisualizer runtimeVisualizer = hexGrid.GetComponent<HexGridRuntimeVisualizer>();
            if (runtimeVisualizer != null)
            {
                runtimeVisualizer.SetGridVisibility(editorState.showGrid);
                return;
            }
            
            HexGridVisualizer visualizer = hexGrid.GetComponent<HexGridVisualizer>();
            if (visualizer != null)
            {
                visualizer.SetGridVisibility(editorState.showGrid);
            }
        }
        
        private void UpdateTagFilter()
        {
            if (tagFilter == null || assetCatalog == null)
                return;
            
            List<string> tags = new List<string> { "All" };
            
            var relevantEntries = assetCatalog.entries;
            if (selectedCategory != AssetCategory.All)
            {
                relevantEntries = relevantEntries.Where(e => e.category == selectedCategory).ToList();
            }
            
            tags.AddRange(relevantEntries
                .SelectMany(e => e.tags ?? new string[0])
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .OrderBy(t => t));
            
            tagFilter.choices = tags;
            tagFilter.index = 0;
        }
        
        private void RefreshAssetGrid()
        {
            if (assetGrid == null || assetCatalog == null)
                return;
            
            assetGrid.Clear();
            displayedAssets.Clear();
            
            string searchText = searchField?.value?.ToLower() ?? "";
            
            foreach (var entry in assetCatalog.entries)
            {
                if (entry.prefab == null)
                    continue;
                
                if (!string.IsNullOrEmpty(searchText) && !entry.displayName.ToLower().Contains(searchText))
                    continue;
                
                if (selectedCategory != AssetCategory.All && entry.category != selectedCategory)
                    continue;
                
                if (selectedTag != "All")
                {
                    bool hasTag = entry.tags != null && entry.tags.Any(t => t == selectedTag);
                    if (!hasTag)
                        continue;
                }
                
                displayedAssets.Add(entry);
                
                Button assetBtn = new Button(() => SelectAsset(entry));
                assetBtn.text = entry.displayName;
                assetBtn.style.height = 32;
                assetBtn.style.marginBottom = 3;
                assetBtn.style.unityTextAlign = TextAnchor.MiddleLeft;
                assetBtn.style.paddingLeft = 8;
                
                bool isSelected = editorState != null && editorState.activeAsset == entry.prefab;
                if (isSelected)
                {
                    assetBtn.style.backgroundColor = new Color(0.3f, 0.6f, 1f, 0.5f);
                }
                
                assetGrid.Add(assetBtn);
            }
        }
        
        private void SelectAsset(AssetCatalog.AssetEntry entry)
        {
            if (editorState != null)
            {
                editorState.SetActiveAsset(entry.prefab);
                
                if (entry.category == AssetCategory.Tiles)
                {
                    editorState.SetLayer(RuntimeEditorLayer.Tiles);
                }
                else
                {
                    editorState.SetLayer(RuntimeEditorLayer.Decorations);
                }
                
                UpdateUI();
                RefreshAssetGrid();
            }
        }
        
        public void CycleNextAsset()
        {
            if (displayedAssets.Count == 0 || editorState == null)
                return;
            
            int currentIndex = displayedAssets.FindIndex(e => e.prefab == editorState.activeAsset);
            int nextIndex = (currentIndex + 1) % displayedAssets.Count;
            SelectAsset(displayedAssets[nextIndex]);
        }
        
        public void CyclePreviousAsset()
        {
            if (displayedAssets.Count == 0 || editorState == null)
                return;
            
            int currentIndex = displayedAssets.FindIndex(e => e.prefab == editorState.activeAsset);
            int prevIndex = currentIndex <= 0 ? displayedAssets.Count - 1 : currentIndex - 1;
            SelectAsset(displayedAssets[prevIndex]);
        }
        
        private void UpdateUI()
        {
            if (editorState == null)
                return;
            
            if (modePlaceBtn != null)
            {
                modePlaceBtn.style.backgroundColor = editorState.mode == RuntimeEditorMode.Place ? new Color(0.3f, 0.6f, 1f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
            }
            
            if (modeEraseBtn != null)
            {
                modeEraseBtn.style.backgroundColor = editorState.mode == RuntimeEditorMode.Erase ? new Color(0.3f, 0.6f, 1f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
            }
            
            if (modeEditBtn != null)
            {
                modeEditBtn.style.backgroundColor = editorState.mode == RuntimeEditorMode.Edit ? new Color(0.3f, 0.6f, 1f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
            }
            
            if (layerTilesBtn != null)
            {
                layerTilesBtn.style.backgroundColor = editorState.layer == RuntimeEditorLayer.Tiles ? new Color(0.3f, 0.8f, 0.3f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
            }
            
            if (layerDecorationsBtn != null)
            {
                layerDecorationsBtn.style.backgroundColor = editorState.layer == RuntimeEditorLayer.Decorations ? new Color(0.3f, 0.8f, 0.3f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
            }
            
            if (elevationLabel != null)
            {
                elevationLabel.text = editorState.elevation.ToString();
            }
            
            if (showPreviewToggle != null)
            {
                showPreviewToggle.SetValueWithoutNotify(editorState.showPreview);
            }
            
            if (showGridToggle != null)
            {
                showGridToggle.SetValueWithoutNotify(editorState.showGrid);
            }
            
            if (snapToGridToggle != null)
            {
                snapToGridToggle.SetValueWithoutNotify(editorState.snapToGrid);
            }
            
            RefreshAssetGrid();
        }
        
        private void UpdateStatusDisplay()
        {
            if (editorState == null)
                return;
            
            if (statusLabel != null)
            {
                string modeText = editorState.mode.ToString();
                string layerText = editorState.layer == RuntimeEditorLayer.Tiles ? "Tiles" : "Decor";
                statusLabel.text = $"{modeText} | {layerText}";
            }
            
            if (hoveredHexLabel != null)
            {
                if (editorState.hoveredHex.HasValue)
                {
                    HexCoordinate hex = editorState.hoveredHex.Value;
                    hoveredHexLabel.text = $"Hex: ({hex.q},{hex.r},{hex.s})";
                }
                else
                {
                    hoveredHexLabel.text = "Hex: ---";
                }
            }
        }
        
        private void OnSaveMap()
        {
            if (hexGrid == null)
            {
                Debug.LogWarning("Cannot save: HexGrid reference is null");
                return;
            }
            
            HexMapSaveData saveData = HexMapSaveData.FromHexGrid(hexGrid);
            string json = saveData.ToJson();
            
            string path = Application.persistentDataPath + "/hexmaps";
            Directory.CreateDirectory(path);
            
            string filename = $"{path}/hexmap_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
            File.WriteAllText(filename, json);
            
            Debug.Log($"Map saved to: {filename}");
            
            if (statusLabel != null)
            {
                statusLabel.text = "Map Saved!";
            }
        }
        
        private void OnLoadMap()
        {
            string path = Application.persistentDataPath + "/hexmaps";
            
            if (!Directory.Exists(path))
            {
                Debug.LogWarning("No saved maps found. Save a map first!");
                if (statusLabel != null)
                {
                    statusLabel.text = "No saves found";
                }
                return;
            }
            
            string[] files = Directory.GetFiles(path, "*.json");
            
            if (files.Length == 0)
            {
                Debug.LogWarning("No saved maps found in: " + path);
                if (statusLabel != null)
                {
                    statusLabel.text = "No saves found";
                }
                return;
            }
            
            string latestFile = files[files.Length - 1];
            
            Debug.LogWarning("Load functionality not yet implemented. Latest save: " + System.IO.Path.GetFileName(latestFile));
            
            if (statusLabel != null)
            {
                statusLabel.text = "Load: TBD";
            }
        }
        
        public bool IsMouseOverUI(Vector2 screenPosition)
        {
            if (uiDocument == null || root == null)
                return false;
            
            var panel = root.panel;
            if (panel == null)
                return false;
            
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
            Vector2 flippedPosition = new Vector2(panelPosition.x, Screen.height - panelPosition.y);
            
            foreach (var child in root.Children())
            {
                if (child.resolvedStyle.display != DisplayStyle.None && 
                    child.worldBound.Contains(flippedPosition))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
