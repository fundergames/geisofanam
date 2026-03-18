using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace RogueDeal.HexLevels.Runtime
{
    public class HexEditorController : MonoBehaviour
    {
        [Header("References")]
        public HexGrid hexGrid;
        public HexEditorRuntimeState editorState;
        public Camera editorCamera;
        public HexEditorUICompact uiController;
        
        [Header("Raycasting")]
        public LayerMask groundLayer;
        public float maxRayDistance = 1000f;
        
        [Header("Preview")]
        public Material previewMaterialValid;
        public Material previewMaterialInvalid;
        public Material previewMaterialReplace;
        
        private GameObject previewInstance;
        private GameObject lastActiveAsset;
        private Material[] cachedPreviewMaterials;
        private Material[] originalMaterials;
        private Vector3 lastPlacementPosition;
        private HexCoordinate? lastPlacedHex;
        private bool isPainting;
        
        private UIDocument[] cachedUIDocuments;
        
        private GameObject editingObject;
        private GameObject editingPrefab;
        private HexCoordinate? editingOriginalHex;
        private int editingOriginalElevation;
        private int editingOriginalRotation;
        private RuntimeEditorLayer editingOriginalLayer;
        private bool editClickHandled;
        
        private void Awake()
        {
            if (editorState == null)
            {
                editorState = GetComponent<HexEditorRuntimeState>();
                if (editorState == null)
                {
                    editorState = gameObject.AddComponent<HexEditorRuntimeState>();
                }
            }
            
            if (editorCamera == null)
            {
                editorCamera = Camera.main;
            }
            
            if (hexGrid == null)
            {
                hexGrid = FindObjectOfType<HexGrid>();
            }
            
            cachedUIDocuments = FindObjectsOfType<UIDocument>();
            
            CreateDefaultPreviewMaterialsIfNeeded();
        }
        
        private void CreateDefaultPreviewMaterialsIfNeeded()
        {
            if (previewMaterialValid == null)
            {
                previewMaterialValid = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                previewMaterialValid.color = new Color(0f, 1f, 0f, 0.5f);
                SetMaterialTransparent(previewMaterialValid);
            }
            
            if (previewMaterialInvalid == null)
            {
                previewMaterialInvalid = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                previewMaterialInvalid.color = new Color(1f, 0f, 0f, 0.5f);
                SetMaterialTransparent(previewMaterialInvalid);
            }
            
            if (previewMaterialReplace == null)
            {
                previewMaterialReplace = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                previewMaterialReplace.color = new Color(1f, 0.8f, 0f, 0.5f);
                SetMaterialTransparent(previewMaterialReplace);
            }
        }
        
        private void SetMaterialTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.SetOverrideTag("RenderType", "Transparent");
        }
        
        private void OnEnable()
        {
            if (editorState != null)
            {
                editorState.OnStateChanged += OnStateChanged;
            }
        }
        
        private void OnDisable()
        {
            if (editorState != null)
            {
                editorState.OnStateChanged -= OnStateChanged;
            }
            
            DestroyPreview();
        }
        
        private void Update()
        {
            UpdateHover();
            UpdatePreview();
            HandleInput();
        }
        
        private void UpdateHover()
        {
            if (Mouse.current == null || editorCamera == null)
                return;
            
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = editorCamera.ScreenPointToRay(mousePos);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, maxRayDistance, groundLayer))
            {
                HexCoordinate hex = hexGrid.WorldToHex(hit.point);
                editorState.SetHoveredHex(hex);
            }
            else
            {
                editorState.SetHoveredHex(null);
            }
        }
        
        private void UpdatePreview()
        {
            if (editorState.mode == RuntimeEditorMode.Edit && editingObject != null)
            {
                UpdateEditingPreview();
                return;
            }
            
            if (!editorState.showPreview || editorState.mode != RuntimeEditorMode.Place)
            {
                DestroyPreview();
                return;
            }
            
            if (editorState.activeAsset == null)
            {
                DestroyPreview();
                return;
            }
            
            if (!editorState.hoveredHex.HasValue)
            {
                DestroyPreview();
                return;
            }
            
            if (previewInstance != null && lastActiveAsset != editorState.activeAsset)
            {
                DestroyPreview();
            }
            
            Vector3 worldPos = hexGrid.HexToWorld(editorState.hoveredHex.Value);
            worldPos.y += editorState.elevation * 0.5f;
            
            if (previewInstance == null)
            {
                CreatePreview();
                lastActiveAsset = editorState.activeAsset;
            }
            
            if (previewInstance != null)
            {
                previewInstance.transform.position = worldPos;
                previewInstance.transform.rotation = Quaternion.Euler(0, editorState.GetRotationDegrees(), 0);
            }
        }
        
        private void CreatePreview()
        {
            if (editorState.activeAsset == null)
                return;
            
            previewInstance = Instantiate(editorState.activeAsset);
            previewInstance.name = "Preview";
            
            Collider[] colliders = previewInstance.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }
            
            Renderer[] renderers = previewInstance.GetComponentsInChildren<Renderer>();
            
            int totalMaterials = 0;
            foreach (var r in renderers)
            {
                totalMaterials += r.sharedMaterials.Length;
            }
            
            originalMaterials = new Material[totalMaterials];
            cachedPreviewMaterials = new Material[totalMaterials];
            
            int index = 0;
            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat != null)
                    {
                        originalMaterials[index] = mat;
                        
                        Material previewMat = new Material(mat);
                        
                        previewMat.SetFloat("_Surface", 1);
                        previewMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        previewMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        previewMat.SetInt("_ZWrite", 0);
                        previewMat.DisableKeyword("_ALPHATEST_ON");
                        previewMat.EnableKeyword("_ALPHABLEND_ON");
                        previewMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        previewMat.renderQueue = 3000;
                        
                        if (previewMat.HasProperty("_BaseColor"))
                        {
                            Color baseColor = previewMat.GetColor("_BaseColor");
                            baseColor.a = 0.5f;
                            previewMat.SetColor("_BaseColor", baseColor);
                        }
                        else if (previewMat.HasProperty("_Color"))
                        {
                            Color baseColor = previewMat.GetColor("_Color");
                            baseColor.a = 0.5f;
                            previewMat.SetColor("_Color", baseColor);
                        }
                        
                        cachedPreviewMaterials[index] = previewMat;
                    }
                    index++;
                }
            }
            
            index = 0;
            foreach (var renderer in renderers)
            {
                int count = renderer.sharedMaterials.Length;
                Material[] mats = new Material[count];
                for (int i = 0; i < count; i++)
                {
                    mats[i] = cachedPreviewMaterials[index++];
                }
                renderer.materials = mats;
            }
        }
        
        private void DestroyPreview()
        {
            if (cachedPreviewMaterials != null)
            {
                foreach (var mat in cachedPreviewMaterials)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
                cachedPreviewMaterials = null;
            }
            
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
            }
            
            originalMaterials = null;
            lastActiveAsset = null;
        }
        
        private void HandleInput()
        {
            if (Mouse.current == null)
                return;
            
            if (IsPointerOverUI())
                return;
            
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                OnMouseDown();
            }
            
            if (Mouse.current.leftButton.isPressed && editorState.dragPaint)
            {
                OnMouseDrag();
            }
            
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                OnMouseUp();
            }
        }
        
        private bool IsPointerOverUI()
        {
            if (Mouse.current == null)
                return false;
            
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            
            if (uiController != null && uiController.IsMouseOverUI(mousePosition))
                return true;
            
            if (cachedUIDocuments != null)
            {
                foreach (var uiDoc in cachedUIDocuments)
                {
                    if (uiDoc == null || uiDoc.rootVisualElement == null)
                        continue;
                        
                    var panel = uiDoc.rootVisualElement.panel;
                    if (panel == null)
                        continue;
                    
                    Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, mousePosition);
                    
                    var pickedElement = panel.Pick(panelPosition);
                    if (pickedElement != null && pickedElement != uiDoc.rootVisualElement)
                        return true;
                    
                    if (CheckElementBounds(uiDoc.rootVisualElement, panelPosition))
                        return true;
                }
            }
            
            if (EventSystem.current != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = mousePosition
                };
                
                var raycastResults = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, raycastResults);
                
                if (raycastResults.Count > 0)
                    return true;
            }
            
            return false;
        }
        
        private bool CheckElementBounds(VisualElement element, Vector2 position)
        {
            if (element == null || element.resolvedStyle.display == DisplayStyle.None)
                return false;
            
            // Check if this element's bounds contain the position
            if (element.worldBound.Contains(position))
            {
                // If it has a background or is interactive, block input
                if (element.resolvedStyle.backgroundColor.a > 0 ||
                    element.pickingMode == PickingMode.Position ||
                    element is UnityEngine.UIElements.Button || 
                    element is TextField || 
                    element is ScrollView)
                {
                    return true;
                }
                
                // Check children
                foreach (var child in element.Children())
                {
                    if (CheckElementBounds(child, position))
                        return true;
                }
            }
            
            return false;
        }
        
        private void OnMouseDown()
        {
            isPainting = true;
            editClickHandled = false;
            ExecuteAction();
        }
        
        private void OnMouseDrag()
        {
            if (isPainting)
            {
                ExecuteAction();
            }
        }
        
        private void OnMouseUp()
        {
            isPainting = false;
            editClickHandled = false;
            lastPlacedHex = null;
        }
        
        private void ExecuteAction()
        {
            if (!editorState.hoveredHex.HasValue)
                return;
            
            HexCoordinate hex = editorState.hoveredHex.Value;
            
            switch (editorState.mode)
            {
                case RuntimeEditorMode.Place:
                    PlaceAsset(hex);
                    break;
                    
                case RuntimeEditorMode.Erase:
                    EraseAsset(hex);
                    break;
                    
                case RuntimeEditorMode.Edit:
                    if (!editClickHandled)
                    {
                        SelectHex(hex);
                        editClickHandled = true;
                    }
                    break;
                    
                case RuntimeEditorMode.Paint:
                    PaintArea(hex);
                    break;
            }
        }
        
        private void PlaceAsset(HexCoordinate hex)
        {
            if (editorState.activeAsset == null)
                return;
            
            if (isPainting && lastPlacedHex.HasValue && lastPlacedHex.Value == hex)
            {
                Debug.Log($"PlaceAsset: Skipping duplicate placement at {hex} during drag");
                return;
            }
            
            Vector3 worldPos = hexGrid.HexToWorld(hex);
            worldPos.y += editorState.elevation * 0.5f;
            
            if (editorState.layer == RuntimeEditorLayer.Tiles)
            {
                HexTileData tileData = hexGrid.GetTile(hex);
                if (tileData == null)
                {
                    tileData = new HexTileData();
                }
                
                tileData.groundTilePrefab = editorState.activeAsset;
                tileData.elevation = editorState.elevation;
                
                if (tileData.groundTileInstance != null)
                {
                    Destroy(tileData.groundTileInstance);
                }
                
                GameObject instance = Instantiate(editorState.activeAsset, worldPos, Quaternion.Euler(0, editorState.GetRotationDegrees(), 0));
                instance.transform.SetParent(hexGrid.transform);
                tileData.groundTileInstance = instance;
                
                hexGrid.SetTile(hex, tileData);
            }
            else
            {
                HexTileData tile = hexGrid.GetTile(hex);
                if (tile != null && tile.HasGroundTile())
                {
                    float currentElevation = editorState.elevation * 0.5f;
                    
                    if (tile.HasObjects())
                    {
                        foreach (var layer in tile.objectLayers)
                        {
                            if (Mathf.Approximately(layer.heightOffset, currentElevation))
                            {
                                Debug.LogWarning($"PlaceAsset: Already have decoration at {hex} with elevation {editorState.elevation}. Skipping placement.");
                                return;
                            }
                        }
                    }
                    
                    GameObject instance = Instantiate(editorState.activeAsset, worldPos, Quaternion.Euler(0, editorState.GetRotationDegrees(), 0));
                    instance.transform.SetParent(hexGrid.transform);
                    tile.AddObjectLayer(editorState.activeAsset, instance, currentElevation);
                    Debug.Log($"PlaceAsset: Placed decoration at {hex} with elevation {editorState.elevation} (now has {tile.objectLayers.Count} objects)");
                }
            }
            
            lastPlacementPosition = worldPos;
            lastPlacedHex = hex;
        }
        
        private void EraseAsset(HexCoordinate hex)
        {
            HexTileData tile = hexGrid.GetTile(hex);
            if (tile == null)
                return;
            
            if (editorState.layer == RuntimeEditorLayer.Tiles)
            {
                if (tile.groundTileInstance != null)
                {
                    Destroy(tile.groundTileInstance);
                }
                tile.ClearGroundTile();
                hexGrid.SetTile(hex, null);
            }
            else
            {
                if (tile.HasObjects())
                {
                    foreach (var layer in tile.objectLayers)
                    {
                        if (layer.instance != null)
                        {
                            Destroy(layer.instance);
                        }
                    }
                    tile.ClearObjectLayers();
                }
            }
        }
        
        private void SelectHex(HexCoordinate hex)
        {
            Debug.Log($"SelectHex called at {hex}, editingObject is {(editingObject != null ? "NOT NULL" : "NULL")}, current layer: {editorState.layer}");
            
            if (editingObject != null)
            {
                Debug.Log($"Calling PlaceEditedObject because we have an object being edited");
                PlaceEditedObject(hex);
            }
            else
            {
                Debug.Log($"Calling StartEditingObject because no object is being edited");
                StartEditingObject(hex);
            }
        }
        
        private void StartEditingObject(HexCoordinate hex)
        {
            Debug.Log($"StartEditingObject at {hex}");
            
            HexTileData tile = hexGrid.GetTile(hex);
            if (tile == null)
            {
                Debug.LogWarning($"StartEditingObject: No tile at {hex}");
                return;
            }
            
            GameObject objectToEdit = null;
            GameObject prefabToEdit = null;
            
            if (editorState.layer == RuntimeEditorLayer.Tiles)
            {
                if (tile.groundTileInstance != null)
                {
                    objectToEdit = tile.groundTileInstance;
                    prefabToEdit = tile.groundTilePrefab;
                    editingOriginalLayer = RuntimeEditorLayer.Tiles;
                    Debug.Log($"Found tile to edit: {objectToEdit.name}");
                }
            }
            else
            {
                if (tile.HasObjects() && tile.objectLayers.Count > 0)
                {
                    HexTileLayer topLayer = tile.objectLayers[tile.objectLayers.Count - 1];
                    objectToEdit = topLayer.instance;
                    prefabToEdit = topLayer.prefab;
                    editingOriginalLayer = RuntimeEditorLayer.Decorations;
                    Debug.Log($"Found decoration to edit: {objectToEdit.name} (tile has {tile.objectLayers.Count} objects)");
                }
                else
                {
                    Debug.LogWarning($"StartEditingObject: Tile at {hex} has no decorations (hasObjects: {tile.HasObjects()}, count: {(tile.objectLayers != null ? tile.objectLayers.Count : -1)})");
                }
            }
            
            if (objectToEdit == null)
            {
                Debug.LogWarning($"StartEditingObject: No object found to edit at {hex}");
                return;
            }
            
            editingObject = objectToEdit;
            editingPrefab = prefabToEdit;
            editingOriginalHex = hex;
            editingOriginalElevation = tile.elevation;
            editingOriginalRotation = Mathf.RoundToInt(objectToEdit.transform.eulerAngles.y / 60f);
            
            editorState.SetElevation(editingOriginalElevation);
            editorState.SetRotation(editingOriginalRotation);
            
            ApplyTransparencyToObject(editingObject, true);
            
            Debug.Log($"Now editing: {editingObject.name} from {editingOriginalHex} (layer: {editingOriginalLayer})");
        }
        
        private void PlaceEditedObject(HexCoordinate newHex)
        {
            if (editingObject == null || !editingOriginalHex.HasValue)
            {
                Debug.LogWarning("PlaceEditedObject: No object being edited");
                return;
            }
            
            ApplyTransparencyToObject(editingObject, false);
            
            HexCoordinate originalHex = editingOriginalHex.Value;
            
            Debug.Log($"PlaceEditedObject: Moving from {originalHex} to {newHex}");
            
            if (originalHex != newHex)
            {
                Debug.Log($"Moving to different hex - removing from {originalHex}");
                RemoveObjectFromHex(originalHex, editingOriginalLayer);
                PlaceObjectAtHex(newHex, editingObject);
            }
            else
            {
                Debug.Log($"Same hex - just updating transform");
                Vector3 worldPos = hexGrid.HexToWorld(newHex);
                worldPos.y += editorState.elevation * 0.5f;
                editingObject.transform.position = worldPos;
                editingObject.transform.rotation = Quaternion.Euler(0, editorState.GetRotationDegrees(), 0);
                
                UpdateObjectElevation(newHex, editingOriginalLayer);
            }
            
            editingObject = null;
            editingPrefab = null;
            editingOriginalHex = null;
        }
        
        private void RemoveObjectFromHex(HexCoordinate hex, RuntimeEditorLayer layer)
        {
            HexTileData tile = hexGrid.GetTile(hex);
            if (tile == null)
            {
                Debug.LogWarning($"RemoveObjectFromHex: No tile at {hex}");
                return;
            }
            
            if (layer == RuntimeEditorLayer.Tiles)
            {
                tile.groundTileInstance = null;
                tile.groundTilePrefab = null;
                hexGrid.SetTile(hex, null);
                Debug.Log($"Removed tile from {hex}");
            }
            else
            {
                if (tile.HasObjects())
                {
                    int objectCount = tile.objectLayers.Count;
                    Debug.Log($"RemoveObjectFromHex: Looking for {editingObject.name} in {objectCount} objects at {hex}");
                    
                    bool found = false;
                    for (int i = tile.objectLayers.Count - 1; i >= 0; i--)
                    {
                        Debug.Log($"  Checking layer {i}: {tile.objectLayers[i].instance.name} == {editingObject.name}? {tile.objectLayers[i].instance == editingObject}");
                        
                        if (tile.objectLayers[i].instance == editingObject)
                        {
                            Debug.Log($"FOUND! Removing object layer {i} from {hex} (had {objectCount} objects)");
                            tile.objectLayers.RemoveAt(i);
                            found = true;
                            break;
                        }
                    }
                    
                    if (!found)
                    {
                        Debug.LogError($"RemoveObjectFromHex: Could not find {editingObject.name} in tile's objectLayers!");
                    }
                    
                    Debug.Log($"After removal: {tile.objectLayers.Count} objects remain at {hex}");
                }
                else
                {
                    Debug.LogWarning($"RemoveObjectFromHex: Tile at {hex} has no objects");
                }
            }
        }
        
        private void PlaceObjectAtHex(HexCoordinate hex, GameObject obj)
        {
            Vector3 worldPos = hexGrid.HexToWorld(hex);
            worldPos.y += editorState.elevation * 0.5f;
            
            obj.transform.position = worldPos;
            obj.transform.rotation = Quaternion.Euler(0, editorState.GetRotationDegrees(), 0);
            
            if (editingOriginalLayer == RuntimeEditorLayer.Tiles)
            {
                HexTileData tileData = hexGrid.GetTile(hex);
                if (tileData == null)
                {
                    tileData = new HexTileData();
                }
                
                tileData.groundTileInstance = obj;
                tileData.groundTilePrefab = editingPrefab;
                tileData.elevation = editorState.elevation;
                
                hexGrid.SetTile(hex, tileData);
                Debug.Log($"Placed tile at {hex}");
            }
            else
            {
                HexTileData tile = hexGrid.GetTile(hex);
                if (tile == null)
                {
                    Debug.LogError($"PlaceObjectAtHex: Cannot place decoration at {hex} - no ground tile exists!");
                    return;
                }
                
                if (!tile.HasGroundTile())
                {
                    Debug.LogError($"PlaceObjectAtHex: Cannot place decoration at {hex} - tile has no ground!");
                    return;
                }
                
                float currentElevation = editorState.elevation * 0.5f;
                
                if (tile.HasObjects())
                {
                    foreach (var layer in tile.objectLayers)
                    {
                        if (layer.instance != obj && Mathf.Approximately(layer.heightOffset, currentElevation))
                        {
                            Debug.LogWarning($"PlaceObjectAtHex: Already have decoration at {hex} with elevation {editorState.elevation}. Cannot place here.");
                            Destroy(obj);
                            return;
                        }
                    }
                }
                
                tile.AddObjectLayer(editingPrefab, obj, currentElevation);
                Debug.Log($"Placed decoration at {hex} with elevation {editorState.elevation} (now has {tile.objectLayers.Count} objects)");
            }
        }
        
        private void UpdateObjectElevation(HexCoordinate hex, RuntimeEditorLayer layer)
        {
            HexTileData tile = hexGrid.GetTile(hex);
            if (tile == null)
                return;
            
            if (layer == RuntimeEditorLayer.Tiles)
            {
                tile.elevation = editorState.elevation;
            }
        }
        
        private void UpdateEditingPreview()
        {
            if (!editorState.hoveredHex.HasValue || editingObject == null)
                return;
            
            Vector3 worldPos = hexGrid.HexToWorld(editorState.hoveredHex.Value);
            worldPos.y += editorState.elevation * 0.5f;
            
            editingObject.transform.position = worldPos;
            editingObject.transform.rotation = Quaternion.Euler(0, editorState.GetRotationDegrees(), 0);
        }
        
        private void ApplyTransparencyToObject(GameObject obj, bool transparent)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            
            foreach (var renderer in renderers)
            {
                Material[] mats = renderer.materials;
                
                for (int i = 0; i < mats.Length; i++)
                {
                    if (transparent)
                    {
                        mats[i].SetFloat("_Surface", 1);
                        mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mats[i].SetInt("_ZWrite", 0);
                        mats[i].DisableKeyword("_ALPHATEST_ON");
                        mats[i].EnableKeyword("_ALPHABLEND_ON");
                        mats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mats[i].renderQueue = 3000;
                        
                        if (mats[i].HasProperty("_BaseColor"))
                        {
                            Color baseColor = mats[i].GetColor("_BaseColor");
                            baseColor.a = 0.5f;
                            mats[i].SetColor("_BaseColor", baseColor);
                        }
                        else if (mats[i].HasProperty("_Color"))
                        {
                            Color baseColor = mats[i].GetColor("_Color");
                            baseColor.a = 0.5f;
                            mats[i].SetColor("_Color", baseColor);
                        }
                    }
                    else
                    {
                        mats[i].SetFloat("_Surface", 0);
                        mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        mats[i].SetInt("_ZWrite", 1);
                        mats[i].DisableKeyword("_ALPHATEST_ON");
                        mats[i].DisableKeyword("_ALPHABLEND_ON");
                        mats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mats[i].renderQueue = -1;
                        
                        if (mats[i].HasProperty("_BaseColor"))
                        {
                            Color baseColor = mats[i].GetColor("_BaseColor");
                            baseColor.a = 1f;
                            mats[i].SetColor("_BaseColor", baseColor);
                        }
                        else if (mats[i].HasProperty("_Color"))
                        {
                            Color baseColor = mats[i].GetColor("_Color");
                            baseColor.a = 1f;
                            mats[i].SetColor("_Color", baseColor);
                        }
                    }
                }
                
                renderer.materials = mats;
            }
        }
        
        private void PaintArea(HexCoordinate centerHex)
        {
            if (editorState.brushSize == 1)
            {
                PlaceAsset(centerHex);
                return;
            }
            
            for (int q = -editorState.brushSize; q <= editorState.brushSize; q++)
            {
                for (int r = -editorState.brushSize; r <= editorState.brushSize; r++)
                {
                    HexCoordinate hex = new HexCoordinate(centerHex.q + q, centerHex.r + r);
                    
                    if (centerHex.DistanceTo(hex) <= editorState.brushSize)
                    {
                        PlaceAsset(hex);
                    }
                }
            }
        }
        
        private void OnStateChanged()
        {
            if (editorState.mode != RuntimeEditorMode.Place)
            {
                DestroyPreview();
            }
            
            if (editorState.mode != RuntimeEditorMode.Edit)
            {
                CancelEditing();
            }
        }
        
        public void CancelEditing()
        {
            if (editingObject != null && editingOriginalHex.HasValue)
            {
                ApplyTransparencyToObject(editingObject, false);
                
                Vector3 worldPos = hexGrid.HexToWorld(editingOriginalHex.Value);
                worldPos.y += editingOriginalElevation * 0.5f;
                editingObject.transform.position = worldPos;
                editingObject.transform.rotation = Quaternion.Euler(0, editingOriginalRotation * 60f, 0);
            }
            
            editingObject = null;
            editingPrefab = null;
            editingOriginalHex = null;
        }
    }
}
