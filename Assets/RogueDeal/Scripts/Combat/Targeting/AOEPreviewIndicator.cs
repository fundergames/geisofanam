using UnityEngine;

namespace RogueDeal.Combat.Targeting
{
    /// <summary>
    /// Visual preview for AOE targeting. Shows the affected area on the ground.
    /// </summary>
    public class AOEPreviewIndicator : MonoBehaviour
    {
        [Header("Visual Settings")]
        [Tooltip("Circle renderer for AOE preview")]
        [SerializeField] private LineRenderer circleRenderer;
        
        [Tooltip("Alternative: Use a sprite renderer")]
        [SerializeField] private SpriteRenderer circleSprite;
        
        [Header("Appearance")]
        [Tooltip("Radius of the AOE")]
        [SerializeField] private float aoeRadius = 5f;
        
        [Tooltip("Color of the preview")]
        [SerializeField] private Color previewColor = new Color(1f, 0.5f, 0f, 0.3f);
        
        [Tooltip("Color of the border")]
        [SerializeField] private Color borderColor = new Color(1f, 0.5f, 0f, 0.8f);
        
        [Header("Positioning")]
        [Tooltip("Offset above ground")]
        [SerializeField] private float groundOffset = 0.1f;
        
        private bool isActive = false;
        private Camera mainCamera;
        
        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }
            
            // Create circle renderer if needed
            if (circleRenderer == null && circleSprite == null)
            {
                GameObject circleObj = new GameObject("AOECircle");
                circleObj.transform.SetParent(transform);
                circleObj.transform.localPosition = Vector3.zero;
                circleRenderer = circleObj.AddComponent<LineRenderer>();
                circleRenderer.useWorldSpace = false;
                circleRenderer.loop = true;
                circleRenderer.startWidth = 0.2f;
                circleRenderer.endWidth = 0.2f;
                circleRenderer.material = new Material(Shader.Find("Sprites/Default"));
                circleRenderer.startColor = borderColor;
                circleRenderer.endColor = borderColor;
            }
            
            SetActive(false);
        }
        
        private void Update()
        {
            if (isActive)
            {
                UpdateVisuals();
            }
        }
        
        /// <summary>
        /// Shows the AOE preview at the given position
        /// </summary>
        public void ShowPreview(Vector3 position, float radius)
        {
            aoeRadius = radius;
            isActive = true;
            
            // Get ground position
            RaycastHit hit;
            Vector3 rayStart = position + Vector3.up * 2f;
            
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f))
            {
                transform.position = hit.point + Vector3.up * groundOffset;
            }
            else
            {
                transform.position = position + Vector3.up * groundOffset;
            }
            
            SetActive(true);
            UpdateVisuals();
        }
        
        /// <summary>
        /// Hides the AOE preview
        /// </summary>
        public void HidePreview()
        {
            isActive = false;
            SetActive(false);
        }
        
        /// <summary>
        /// Updates the radius of the AOE preview
        /// </summary>
        public void SetRadius(float radius)
        {
            aoeRadius = radius;
            if (isActive)
            {
                UpdateVisuals();
            }
        }
        
        private void UpdateVisuals()
        {
            // Update circle
            if (circleRenderer != null)
            {
                DrawCircle(circleRenderer, aoeRadius, 64);
            }
            else if (circleSprite != null)
            {
                circleSprite.transform.localScale = Vector3.one * (aoeRadius * 2f);
                circleSprite.color = previewColor;
            }
        }
        
        private void DrawCircle(LineRenderer lr, float radius, int segments)
        {
            lr.positionCount = segments + 1;
            lr.startColor = borderColor;
            lr.endColor = borderColor;
            
            float angle = 0f;
            for (int i = 0; i <= segments; i++)
            {
                float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
                lr.SetPosition(i, new Vector3(x, 0, z));
                angle += 360f / segments;
            }
        }
        
        private void SetActive(bool active)
        {
            if (circleRenderer != null) circleRenderer.enabled = active;
            if (circleSprite != null) circleSprite.enabled = active;
        }
    }
}
