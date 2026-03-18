using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Combat.Targeting
{
    /// <summary>
    /// Visual indicator for lock-on targeting. Shows a red circle with X crosshair on the ground under the locked target.
    /// Follows the target if it's movable, or stays at ground position for AOE.
    /// </summary>
    public class LockOnIndicator : MonoBehaviour
    {
        [Header("Visual Settings")]
        [Tooltip("Circle line renderer")]
        [SerializeField] private LineRenderer circleRenderer;
        
        [Tooltip("X crosshair renderer (two line renderers)")]
        [SerializeField] private LineRenderer[] crosshairRenderers = new LineRenderer[2];
        
        [Header("Appearance")]
        [Tooltip("Radius of the indicator circle")]
        [SerializeField] private float indicatorRadius = 1f;
        
        [Tooltip("Color of the indicator")]
        [SerializeField] private Color indicatorColor = new Color(1f, 0f, 0f, 0.8f);
        
        [Tooltip("Width of the circle ring")]
        [SerializeField] private float circleWidth = 0.15f;
        
        [Tooltip("Width of the X crosshair lines")]
        [SerializeField] private float crosshairWidth = 0.2f;
        
        [Tooltip("Length of the X crosshair lines")]
        [SerializeField] private float crosshairLength = 1f;
        
        [Header("Positioning")]
        [Tooltip("Offset above ground")]
        [SerializeField] private float groundOffset = 0.1f;
        
        [Tooltip("Should indicator follow target? (false for AOE)")]
        [SerializeField] private bool followTarget = true;
        
        private CombatEntity target;
        private bool isActive = false;
        private Camera mainCamera;
        
        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            
            CreateVisuals();
            SetActive(false);
        }
        
        private void CreateVisuals()
        {
            Debug.Log("[LockOnIndicator] Creating visuals...");
            
            // Create circle line renderer (like RangeIndicator does)
            if (circleRenderer == null)
            {
                GameObject circleObj = new GameObject("Circle");
                circleObj.transform.SetParent(transform);
                circleObj.transform.localPosition = Vector3.zero;
                circleRenderer = circleObj.AddComponent<LineRenderer>();
                circleRenderer.useWorldSpace = true; // Use world space for fixed orientation
                circleRenderer.loop = true;
                circleRenderer.startWidth = circleWidth;
                circleRenderer.endWidth = circleWidth;
                circleRenderer.material = new Material(Shader.Find("Sprites/Default"));
                circleRenderer.startColor = indicatorColor;
                circleRenderer.endColor = indicatorColor;
                circleRenderer.enabled = false; // Start disabled
                Debug.Log($"[LockOnIndicator] Created circle line renderer");
            }
            
            // Create X crosshair (two line renderers at 45 degrees)
            if (crosshairRenderers[0] == null || crosshairRenderers[1] == null)
            {
                // First diagonal line (top-left to bottom-right)
                GameObject crosshair1 = new GameObject("CrosshairLine1");
                crosshair1.transform.SetParent(transform);
                crosshair1.transform.localPosition = Vector3.zero;
                crosshairRenderers[0] = crosshair1.AddComponent<LineRenderer>();
                crosshairRenderers[0].useWorldSpace = true; // Use world space for fixed orientation
                crosshairRenderers[0].startWidth = crosshairWidth;
                crosshairRenderers[0].endWidth = crosshairWidth;
                crosshairRenderers[0].material = new Material(Shader.Find("Sprites/Default"));
                crosshairRenderers[0].startColor = indicatorColor;
                crosshairRenderers[0].endColor = indicatorColor;
                crosshairRenderers[0].enabled = false;
                Debug.Log($"[LockOnIndicator] Created crosshair line 1");
                
                // Second diagonal line (top-right to bottom-left)
                GameObject crosshair2 = new GameObject("CrosshairLine2");
                crosshair2.transform.SetParent(transform);
                crosshair2.transform.localPosition = Vector3.zero;
                crosshairRenderers[1] = crosshair2.AddComponent<LineRenderer>();
                crosshairRenderers[1].useWorldSpace = true; // Use world space for fixed orientation
                crosshairRenderers[1].startWidth = crosshairWidth;
                crosshairRenderers[1].endWidth = crosshairWidth;
                crosshairRenderers[1].material = new Material(Shader.Find("Sprites/Default"));
                crosshairRenderers[1].startColor = indicatorColor;
                crosshairRenderers[1].endColor = indicatorColor;
                crosshairRenderers[1].enabled = false;
                Debug.Log($"[LockOnIndicator] Created crosshair line 2");
            }
            
            Debug.Log("[LockOnIndicator] Visuals created successfully!");
        }
        
        private void DrawCircle(LineRenderer lr, float radius, int segments = 32)
        {
            // Make sure circle uses world space for fixed orientation
            lr.useWorldSpace = true;
            
            lr.positionCount = segments + 1;
            lr.startColor = indicatorColor;
            lr.endColor = indicatorColor;
            
            // Get world position of the indicator
            Vector3 center = transform.position;
            
            float angle = 0f;
            for (int i = 0; i <= segments; i++)
            {
                // Calculate positions in world space relative to world axes
                float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
                // Add to center position in world space
                lr.SetPosition(i, center + new Vector3(x, 0, z));
                angle += 360f / segments;
            }
        }
        
        private void DrawX(LineRenderer lr1, LineRenderer lr2, float length)
        {
            float halfLength = length * 0.5f;
            
            // Make sure line renderers use world space for fixed orientation
            lr1.useWorldSpace = true;
            lr2.useWorldSpace = true;
            
            // Get world positions based on transform position
            Vector3 center = transform.position;
            
            // Calculate directions in world space (relative to world axes, not local)
            Vector3 worldRight = Vector3.right;
            Vector3 worldForward = Vector3.forward;
            
            // First diagonal: top-left to bottom-right (45 degrees rotated)
            Vector3 dir1 = (worldRight + worldForward).normalized;
            lr1.positionCount = 2;
            lr1.startColor = indicatorColor;
            lr1.endColor = indicatorColor;
            lr1.SetPosition(0, center + dir1 * -halfLength);
            lr1.SetPosition(1, center + dir1 * halfLength);
            
            // Second diagonal: top-right to bottom-left (45 degrees rotated the other way)
            Vector3 dir2 = (worldRight - worldForward).normalized;
            lr2.positionCount = 2;
            lr2.startColor = indicatorColor;
            lr2.endColor = indicatorColor;
            lr2.SetPosition(0, center + dir2 * -halfLength);
            lr2.SetPosition(1, center + dir2 * halfLength);
        }
        
        private void Update()
        {
            if (isActive)
            {
                UpdatePosition();
                UpdateVisuals();
            }
        }
        
        /// <summary>
        /// Sets the target to lock on to
        /// </summary>
        public void SetTarget(CombatEntity targetEntity, bool follow = true)
        {
            target = targetEntity;
            followTarget = follow;
            isActive = target != null;
            
            if (isActive)
            {
                // Debug.Log($"[LockOnIndicator] Setting target: {targetEntity?.name ?? "null"} at position {targetEntity?.transform.position}");
                SetActive(true);
                UpdatePosition();
                UpdateVisuals(); // Make sure visuals are updated immediately
                // Debug.Log($"[LockOnIndicator] Indicator position: {transform.position}, Circle enabled: {circleRenderer?.enabled ?? false}, Crosshair enabled: {(crosshairRenderers[0]?.enabled ?? false) || (crosshairRenderers[1]?.enabled ?? false)}");
            }
            else
            {
                SetActive(false);
            }
        }
        
        /// <summary>
        /// Clears the lock-on target
        /// </summary>
        public void ClearTarget()
        {
            target = null;
            isActive = false;
            SetActive(false);
        }
        
        /// <summary>
        /// Sets the ground position for AOE targeting
        /// </summary>
        public void SetGroundPosition(Vector3 position)
        {
            target = null;
            followTarget = false;
            isActive = true;
            
            transform.position = position + Vector3.up * groundOffset;
            SetActive(true);
            UpdateVisuals();
        }
        
        private void UpdatePosition()
        {
            if (target != null && followTarget)
            {
                // Get the bottom of the enemy's collider to find ground position
                Vector3 groundPosition = GetGroundPositionUnderTarget(target);
                transform.position = groundPosition + Vector3.up * groundOffset;
            }
        }
        
        /// <summary>
        /// Gets the ground position under the target by finding the bottom of their collider and raycasting down
        /// </summary>
        private Vector3 GetGroundPositionUnderTarget(CombatEntity targetEntity)
        {
            if (targetEntity == null) return Vector3.zero;
            
            // Get all colliders on the target to find the bottom and exclude from raycast
            Collider[] targetColliders = targetEntity.GetComponentsInChildren<Collider>();
            HashSet<Collider> targetColliderSet = new HashSet<Collider>(targetColliders);
            
            // Find the lowest point of all colliders (the bottom of the enemy)
            float lowestY = float.MaxValue;
            Vector3 bottomPosition = targetEntity.transform.position;
            
            foreach (var col in targetColliders)
            {
                if (col == null || col.isTrigger) continue;
                
                Bounds bounds = col.bounds;
                float bottomY = bounds.min.y; // Bottom of the collider
                
                if (bottomY < lowestY)
                {
                    lowestY = bottomY;
                    bottomPosition = new Vector3(bounds.center.x, bottomY, bounds.center.z);
                }
            }
            
            // If no valid colliders found, use transform position
            if (lowestY == float.MaxValue)
            {
                bottomPosition = targetEntity.transform.position;
            }
            
            // Raycast from slightly above the bottom position down to find the ground
            Vector3 rayStart = bottomPosition + Vector3.up * 0.5f; // Start slightly above the bottom
            
            // Use RaycastAll to find ground, excluding target's own colliders
            RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 5f);
            
            // Find the first hit that's not a target collider
            RaycastHit? groundHit = null;
            foreach (var hit in hits)
            {
                if (!targetColliderSet.Contains(hit.collider))
                {
                    groundHit = hit;
                    break;
                }
            }
            
            if (groundHit.HasValue)
            {
                return groundHit.Value.point;
            }
            else
            {
                // Fallback: use the bottom position we calculated
                return bottomPosition;
            }
        }
        
        private void UpdateVisuals()
        {
            // Update circle
            if (circleRenderer != null && circleRenderer.enabled)
            {
                DrawCircle(circleRenderer, indicatorRadius, 32);
            }
            
            // Update X crosshair
            if (crosshairRenderers[0] != null && crosshairRenderers[1] != null && 
                crosshairRenderers[0].enabled && crosshairRenderers[1].enabled)
            {
                DrawX(crosshairRenderers[0], crosshairRenderers[1], crosshairLength);
            }
        }
        
        private void SetActive(bool active)
        {
            if (circleRenderer != null)
            {
                circleRenderer.enabled = active;
                if (active)
                {
                    // Debug.Log($"[LockOnIndicator] Circle renderer enabled at position: {transform.position}");
                    DrawCircle(circleRenderer, indicatorRadius, 32);
                }
            }
            
            if (crosshairRenderers[0] != null)
            {
                crosshairRenderers[0].enabled = active;
            }
            
            if (crosshairRenderers[1] != null)
            {
                crosshairRenderers[1].enabled = active;
            }
            
            if (active && crosshairRenderers[0] != null && crosshairRenderers[1] != null)
            {
                DrawX(crosshairRenderers[0], crosshairRenderers[1], crosshairLength);
            }
        }
    }
}
