using UnityEngine;
using System.Collections.Generic;

namespace RogueDeal.HexLevels.Runtime
{
    [RequireComponent(typeof(HexGrid))]
    public class HexGridRuntimeVisualizer : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private bool showGrid = true;
        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.3f);
        [SerializeField] private int gridRadius = 20;
        [SerializeField] private float lineWidth = 0.02f;
        [SerializeField] private Material lineMaterial;
        
        private HexGrid hexGrid;
        private GameObject gridContainer;
        private List<LineRenderer> lineRenderers = new List<LineRenderer>();
        
        private void Awake()
        {
            hexGrid = GetComponent<HexGrid>();
        }
        
        private void Start()
        {
            if (lineMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                
                if (shader != null)
                {
                    lineMaterial = new Material(shader);
                    lineMaterial.color = gridColor;
                }
            }
            
            CreateGrid();
        }
        
        private void OnEnable()
        {
            if (gridContainer != null)
            {
                gridContainer.SetActive(showGrid);
            }
        }
        
        private void OnDisable()
        {
            if (gridContainer != null)
            {
                gridContainer.SetActive(false);
            }
        }
        
        public void SetGridVisibility(bool visible)
        {
            showGrid = visible;
            if (gridContainer != null)
            {
                gridContainer.SetActive(showGrid);
            }
        }
        
        private void CreateGrid()
        {
            if (hexGrid == null)
                return;
            
            DestroyGrid();
            
            gridContainer = new GameObject("HexGridLines");
            gridContainer.transform.SetParent(transform);
            gridContainer.transform.localPosition = Vector3.zero;
            gridContainer.transform.localRotation = Quaternion.identity;
            gridContainer.SetActive(showGrid);
            
            HexCoordinate centerHex = new HexCoordinate(0, 0);
            HexCoordinate[] hexes = centerHex.GetHexesInRange(gridRadius);
            
            foreach (var hex in hexes)
            {
                if (!hexGrid.IsInBounds(hex))
                    continue;
                
                CreateHexOutline(hex);
            }
        }
        
        private void CreateHexOutline(HexCoordinate hex)
        {
            Vector3 worldPos = hexGrid.HexToWorld(hex);
            Vector3[] corners = GetHexCorners(worldPos, hexGrid.HexSize);
            
            GameObject lineObj = new GameObject($"HexLine_{hex.q}_{hex.r}");
            lineObj.transform.SetParent(gridContainer.transform);
            lineObj.transform.localPosition = Vector3.zero;
            
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 7;
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = lineMaterial;
            lineRenderer.startColor = gridColor;
            lineRenderer.endColor = gridColor;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            
            for (int i = 0; i < 6; i++)
            {
                lineRenderer.SetPosition(i, corners[i]);
            }
            lineRenderer.SetPosition(6, corners[0]);
            
            lineRenderers.Add(lineRenderer);
        }
        
        private Vector3[] GetHexCorners(Vector3 center, float size)
        {
            Vector3[] corners = new Vector3[6];
            float sqrt3 = Mathf.Sqrt(3f);
            float yOffset = 0.05f;
            
            corners[0] = center + new Vector3(0f, yOffset, size);
            corners[1] = center + new Vector3(size * sqrt3 / 2f, yOffset, size / 2f);
            corners[2] = center + new Vector3(size * sqrt3 / 2f, yOffset, -size / 2f);
            corners[3] = center + new Vector3(0f, yOffset, -size);
            corners[4] = center + new Vector3(-size * sqrt3 / 2f, yOffset, -size / 2f);
            corners[5] = center + new Vector3(-size * sqrt3 / 2f, yOffset, size / 2f);
            
            return corners;
        }
        
        private void DestroyGrid()
        {
            lineRenderers.Clear();
            
            if (gridContainer != null)
            {
                Destroy(gridContainer);
                gridContainer = null;
            }
        }
        
        private void OnDestroy()
        {
            DestroyGrid();
        }
        
        public void RefreshGrid()
        {
            CreateGrid();
        }
    }
}
