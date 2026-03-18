using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Targeting;
using RogueDeal.Combat.Presentation;

namespace RogueDeal.Combat.Targeting
{
    /// <summary>
    /// Visual indicator for attack range. Shows a circle on the ground representing the attack range.
    /// Useful for debugging and gameplay clarity.
    /// </summary>
    public class RangeIndicator : MonoBehaviour
    {
        [Header("Visual Settings")]
        [Tooltip("LineRenderer for the range circle")]
        [SerializeField] private LineRenderer circleRenderer;
        
        [Tooltip("Alternative: Use a sprite renderer")]
        [SerializeField] private SpriteRenderer circleSprite;
        
        [Header("Appearance")]
        [Tooltip("Color of the range indicator")]
        [SerializeField] private Color rangeColor = new Color(0f, 1f, 0f, 0.3f);
        
        [Tooltip("Color of the border")]
        [SerializeField] private Color borderColor = new Color(0f, 1f, 0f, 0.8f);
        
        [Header("Display Options")]
        [Tooltip("When to show the range indicator")]
        [SerializeField] private DisplayMode displayMode = DisplayMode.Always;
        
        [Tooltip("Show range indicator in Play mode")]
        [SerializeField] private bool showInPlayMode = true;
        
        [Header("Positioning")]
        [Tooltip("Offset above ground")]
        [SerializeField] private float groundOffset = 0.1f;
        
        [Tooltip("Number of segments in the circle (more = smoother)")]
        [SerializeField] private int circleSegments = 64;
        
        [Header("Enemy Highlighting")]
        [Tooltip("Enable highlighting of enemies within range")]
        [SerializeField] private bool highlightEnemiesInRange = true;
        
        [Tooltip("Color for closest enemy in range")]
        [SerializeField] private Color closestEnemyColor = Color.yellow;
        
        [Tooltip("Color for other enemies in range")]
        [SerializeField] private Color enemyInRangeColor = Color.red;
        
        [Tooltip("Layer mask for enemy detection")]
        [SerializeField] private LayerMask enemyLayerMask = -1;
        
        private CombatEntity combatEntity;
        private TargetingManager targetingManager;
        private CombatExecutor combatExecutor;
        private ThirdPersonCombatController thirdPersonController;
        private float currentRange = 0f;
        private bool isActive = false;
        
        // Enemy highlighting
        private Dictionary<CombatEntity, Dictionary<Renderer, Color>> highlightedEnemies = new Dictionary<CombatEntity, Dictionary<Renderer, Color>>();
        
        public enum DisplayMode
        {
            Always,
            OnHover,
            DebugOnly,
            Never
        }
        
        private void Awake()
        {
            combatEntity = GetComponent<CombatEntity>();
            if (combatEntity == null)
            {
                combatEntity = GetComponentInParent<CombatEntity>();
            }
            
            // Get TargetingManager to check current strategy range
            targetingManager = GetComponent<TargetingManager>();
            if (targetingManager == null)
            {
                targetingManager = GetComponentInParent<TargetingManager>();
            }
            
            // Get CombatExecutor to check current action's targeting strategy
            combatExecutor = GetComponent<CombatExecutor>();
            if (combatExecutor == null)
            {
                combatExecutor = GetComponentInParent<CombatExecutor>();
            }
            
            // Get ThirdPersonCombatController to check available actions
            thirdPersonController = GetComponent<ThirdPersonCombatController>();
            if (thirdPersonController == null)
            {
                thirdPersonController = GetComponentInParent<ThirdPersonCombatController>();
            }
            
            // Create circle renderer if needed
            if (circleRenderer == null && circleSprite == null)
            {
                GameObject circleObj = new GameObject("RangeCircle");
                circleObj.transform.SetParent(transform);
                circleObj.transform.localPosition = Vector3.zero;
                circleRenderer = circleObj.AddComponent<LineRenderer>();
                circleRenderer.useWorldSpace = false;
                circleRenderer.loop = true;
                circleRenderer.startWidth = 0.15f;
                circleRenderer.endWidth = 0.15f;
                circleRenderer.material = new Material(Shader.Find("Sprites/Default"));
                circleRenderer.startColor = borderColor;
                circleRenderer.endColor = borderColor;
            }
            
            UpdateRange();
        }
        
        private void Update()
        {
            // Check if we should display
            bool shouldDisplay = ShouldDisplay();
            
            if (shouldDisplay)
            {
                UpdateRange();
                UpdatePosition();
                UpdateVisuals();
                SetActive(true);
                
                if (highlightEnemiesInRange)
                {
                    UpdateEnemyHighlighting();
                }
            }
            else
            {
                SetActive(false);
                ClearEnemyHighlighting();
            }
        }
        
        private void OnDisable()
        {
            ClearEnemyHighlighting();
        }
        
        /// <summary>
        /// Manually set the range to display
        /// </summary>
        public void SetRange(float range)
        {
            currentRange = range;
            if (isActive)
            {
                UpdateVisuals();
            }
        }
        
        /// <summary>
        /// Updates the range from weapon/combat profile or targeting strategy
        /// Uses the same priority logic as SingleTargetSelector.GetMaxRange():
        /// Weapon > CombatProfile > Strategy Default
        /// </summary>
        public void UpdateRange()
        {
            float newRange = 0f;
            
            if (combatEntity != null)
            {
                var entityData = combatEntity.GetEntityData();
                if (entityData != null)
                {
                    // First, determine which targeting strategy to use (same logic as TargetingManager.GetTargets)
                    // Priority: Current executing action > Available actions > Manager's strategy
                    TargetingStrategy strategyToUse = null;
                    CombatAction actionToCheck = null;
                    
                    // Check current executing action's targeting strategy first
                    if (combatExecutor != null)
                    {
                        actionToCheck = combatExecutor.GetCurrentAction();
                        if (actionToCheck != null && actionToCheck.targetingStrategy != null)
                        {
                            strategyToUse = actionToCheck.targetingStrategy;
                            Debug.Log($"[RangeIndicator] Using strategy from executing action '{actionToCheck.actionName}': {strategyToUse.GetType().Name}");
                        }
                    }
                    
                    // If no executing action, check available actions (same as UpdateLockOnIndicator does)
                    if (strategyToUse == null && thirdPersonController != null)
                    {
                        // Use reflection to get availableActions (it's private)
                        var controllerType = thirdPersonController.GetType();
                        var availableActionsField = controllerType.GetField("availableActions", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (availableActionsField != null)
                        {
                            var availableActions = availableActionsField.GetValue(thirdPersonController) as CombatAction[];
                            if (availableActions != null && availableActions.Length > 0)
                            {
                                actionToCheck = availableActions[0]; // Use first action (same as UpdateLockOnIndicator)
                                if (actionToCheck != null && actionToCheck.targetingStrategy != null)
                                {
                                    strategyToUse = actionToCheck.targetingStrategy;
                                    // Debug.Log($"[RangeIndicator] Using strategy from available action '{actionToCheck.actionName}': {strategyToUse.GetType().Name}");
                                }
                            }
                        }
                    }
                    
                    // If no action strategy, use TargetingManager's current strategy
                    if (strategyToUse == null && targetingManager != null)
                    {
                        strategyToUse = targetingManager.GetCurrentStrategy();
                        if (strategyToUse != null)
                        {
                            Debug.Log($"[RangeIndicator] Using TargetingManager's current strategy: {strategyToUse.GetType().Name}");
                        }
                    }
                    
                    // Get range using same priority as SingleTargetSelector.GetMaxRange():
                    // 1. Weapon range (if available)
                    // 2. CombatProfile engagementDistance (if available)
                    // 3. Strategy's maxRange/defaultRange (fallback)
                    
                    // But if we have a strategy with maxRange, we want to use the same logic as GetMaxRange
                    // which means: check weapon first, then profile, then strategy's maxRange
                    
                    // Priority 1: Get range from weapon
                    if (entityData.equippedWeapon != null && entityData.equippedWeapon.maxRange > 0)
                    {
                        newRange = entityData.equippedWeapon.maxRange;
                        Debug.Log($"[RangeIndicator] Using range from weapon: {newRange}");
                    }
                    // Priority 2: Get range from combat profile
                    else if (entityData.combatProfile != null && entityData.combatProfile.engagementDistance > 0)
                    {
                        newRange = entityData.combatProfile.engagementDistance;
                        Debug.Log($"[RangeIndicator] Using range from combat profile: {newRange}");
                    }
                    // Priority 3: Get range from targeting strategy's default (like SingleTargetSelector.maxRange)
                    else if (strategyToUse != null)
                    {
                        if (strategyToUse is SingleTargetSelector singleTargetSelector)
                        {
                            if (singleTargetSelector.maxRange > 0)
                            {
                                newRange = singleTargetSelector.maxRange;
                                // Debug.Log($"[RangeIndicator] ✅ Using range from SingleTargetSelector.maxRange: {newRange}");
                            }
                            else
                            {
                                Debug.LogWarning($"[RangeIndicator] SingleTargetSelector has maxRange = {singleTargetSelector.maxRange}, falling back to default");
                            }
                        }
                        else if (strategyToUse is NearestEnemyTargetingStrategy nearestEnemy)
                        {
                            // Use defaultRange from NearestEnemyTargetingStrategy if available
                            // Check via reflection since defaultRange is private
                            var strategyType = nearestEnemy.GetType();
                            var defaultRangeField = strategyType.GetField("defaultRange", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            if (defaultRangeField != null)
                            {
                                float defaultRange = (float)defaultRangeField.GetValue(nearestEnemy);
                                if (defaultRange > 0)
                                {
                                    newRange = defaultRange;
                                    Debug.Log($"[RangeIndicator] Using range from NearestEnemyTargetingStrategy: {newRange}");
                                }
                            }
                        }
                        else
                        {
                            Debug.Log($"[RangeIndicator] Strategy {strategyToUse.GetType().Name} doesn't have a maxRange field");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[RangeIndicator] No targeting strategy found! actionToCheck: {actionToCheck?.actionName ?? "null"}");
                    }
                    
                    // Priority 4: Default fallback
                    if (newRange <= 0)
                    {
                        newRange = 2f; // Default
                        // Debug.Log($"[RangeIndicator] Using default range: {newRange}");
                    }
                }
            }
            else
            {
                newRange = 2f; // Default if no entity
            }
            
            // Only update if range changed (prevents unnecessary redraws)
            if (Mathf.Abs(currentRange - newRange) > 0.01f)
            {
                float oldRange = currentRange;
                currentRange = newRange;
                Debug.Log($"[RangeIndicator] Range changed from {oldRange:F2} to {newRange:F2}");
                // Force visual update when range changes
                if (isActive)
                {
                    UpdateVisuals();
                }
            }
        }
        
        private bool ShouldDisplay()
        {
            if (displayMode == DisplayMode.Never)
            {
                return false;
            }
            
            if (displayMode == DisplayMode.DebugOnly)
            {
                return Debug.isDebugBuild || Application.isEditor;
            }
            
            if (displayMode == DisplayMode.Always)
            {
                return showInPlayMode || !Application.isPlaying;
            }
            
            // OnHover mode - could be extended with mouse hover detection
            return false;
        }
        
        private void UpdatePosition()
        {
            if (combatEntity == null)
                return;
                
            // Position at character's feet (ground level)
            Vector3 characterPosition = combatEntity.transform.position;
            Vector3 rayStart = characterPosition + Vector3.up * 2f;
            
            // Get all colliders on the player to exclude from raycast
            Collider[] playerColliders = combatEntity.GetComponentsInChildren<Collider>();
            HashSet<Collider> playerColliderSet = new HashSet<Collider>(playerColliders);
            
            // Use RaycastAll to find ground, excluding player colliders
            RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 10f);
            
            // Find the first hit that's not a player collider
            RaycastHit? groundHit = null;
            foreach (var hit in hits)
            {
                if (!playerColliderSet.Contains(hit.collider))
                {
                    groundHit = hit;
                    break;
                }
            }
            
            if (groundHit.HasValue)
            {
                // Position indicator at ground level at character's feet
                transform.position = groundHit.Value.point + Vector3.up * groundOffset;
            }
            else
            {
                // Fallback: use character's Y position at ground level
                Vector3 pos = characterPosition;
                pos.y = groundOffset;
                transform.position = pos;
            }
        }
        
        private void UpdateVisuals()
        {
            if (currentRange <= 0)
            {
                SetActive(false);
                return;
            }
            
            // Update circle
            if (circleRenderer != null)
            {
                DrawCircle(circleRenderer, currentRange, circleSegments);
            }
            else if (circleSprite != null)
            {
                circleSprite.transform.localScale = Vector3.one * (currentRange * 2f);
                circleSprite.color = rangeColor;
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
            isActive = active;
            if (circleRenderer != null)
            {
                circleRenderer.enabled = active;
            }
            if (circleSprite != null)
            {
                circleSprite.enabled = active;
            }
        }
        
        /// <summary>
        /// Toggles the range indicator on/off
        /// </summary>
        public void Toggle()
        {
            if (displayMode == DisplayMode.Never)
            {
                displayMode = DisplayMode.Always;
            }
            else
            {
                displayMode = DisplayMode.Never;
            }
        }
        
        /// <summary>
        /// Shows the range indicator
        /// </summary>
        public void Show()
        {
            displayMode = DisplayMode.Always;
        }
        
        /// <summary>
        /// Hides the range indicator
        /// </summary>
        public void Hide()
        {
            displayMode = DisplayMode.Never;
        }
        
        /// <summary>
        /// Sets display mode to debug only
        /// </summary>
        public void SetDebugOnly()
        {
            displayMode = DisplayMode.DebugOnly;
        }
        
        /// <summary>
        /// Gets the current display mode
        /// </summary>
        public DisplayMode GetDisplayMode()
        {
            return displayMode;
        }
        
        /// <summary>
        /// Updates enemy highlighting - finds enemies in range and highlights them
        /// </summary>
        private void UpdateEnemyHighlighting()
        {
            if (combatEntity == null || currentRange <= 0)
            {
                ClearEnemyHighlighting();
                return;
            }
            
            var entityData = combatEntity.GetEntityData();
            if (entityData == null)
                return;
            
            Vector3 characterPosition = combatEntity.transform.position;
            
            // Find all enemies within range
            Collider[] colliders = Physics.OverlapSphere(characterPosition, currentRange, enemyLayerMask);
            
            List<CombatEntity> enemiesInRange = new List<CombatEntity>();
            CombatEntity closestEnemy = null;
            float closestDistance = float.MaxValue;
            
            foreach (var collider in colliders)
            {
                // Skip self
                if (collider.transform == combatEntity.transform || collider.transform.IsChildOf(combatEntity.transform))
                    continue;
                
                // Find CombatEntity
                CombatEntity enemyEntity = collider.GetComponent<CombatEntity>();
                if (enemyEntity == null)
                    enemyEntity = collider.GetComponentInParent<CombatEntity>();
                if (enemyEntity == null)
                    enemyEntity = collider.GetComponentInChildren<CombatEntity>();
                
                if (enemyEntity == null || enemyEntity == combatEntity)
                    continue;
                
                // Check if enemy is alive
                var enemyData = enemyEntity.GetEntityData();
                if (enemyData == null || !enemyData.IsAlive)
                    continue;
                
                // Calculate distance
                float distance = Vector3.Distance(characterPosition, enemyEntity.transform.position);
                if (distance <= currentRange)
                {
                    enemiesInRange.Add(enemyEntity);
                    
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemyEntity;
                    }
                }
            }
            
            // Remove highlighting from enemies that are no longer in range
            List<CombatEntity> toRemove = new List<CombatEntity>();
            foreach (var highlighted in highlightedEnemies.Keys)
            {
                if (!enemiesInRange.Contains(highlighted))
                {
                    toRemove.Add(highlighted);
                }
            }
            
            foreach (var enemy in toRemove)
            {
                RestoreEnemyHighlight(enemy);
                highlightedEnemies.Remove(enemy);
            }
            
            // Highlight enemies in range
            foreach (var enemy in enemiesInRange)
            {
                bool isClosest = (enemy == closestEnemy);
                Color highlightColor = isClosest ? closestEnemyColor : enemyInRangeColor;
                
                if (!highlightedEnemies.ContainsKey(enemy))
                {
                    // New enemy to highlight
                    HighlightEnemy(enemy, highlightColor);
                }
                else
                {
                    // Update color if closest enemy changed
                    UpdateEnemyHighlight(enemy, highlightColor);
                }
            }
        }
        
        /// <summary>
        /// Highlights an enemy with the specified color
        /// </summary>
        private void HighlightEnemy(CombatEntity enemy, Color highlightColor)
        {
            if (enemy == null)
                return;
            
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
            Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
            
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;
                
                // Store original color
                if (renderer.material != null && renderer.material.HasProperty("_Color"))
                {
                    originalColors[renderer] = renderer.material.color;
                    // Apply highlight color
                    renderer.material.color = highlightColor;
                }
            }
            
            if (originalColors.Count > 0)
            {
                highlightedEnemies[enemy] = originalColors;
            }
        }
        
        /// <summary>
        /// Updates the highlight color for an already highlighted enemy
        /// </summary>
        private void UpdateEnemyHighlight(CombatEntity enemy, Color newColor)
        {
            if (enemy == null || !highlightedEnemies.ContainsKey(enemy))
                return;
            
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.material != null && renderer.material.HasProperty("_Color"))
                {
                    renderer.material.color = newColor;
                }
            }
        }
        
        /// <summary>
        /// Restores the original color for a highlighted enemy
        /// </summary>
        private void RestoreEnemyHighlight(CombatEntity enemy)
        {
            if (enemy == null || !highlightedEnemies.ContainsKey(enemy))
                return;
            
            var originalColors = highlightedEnemies[enemy];
            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
            
            foreach (var renderer in renderers)
            {
                if (renderer != null && originalColors.ContainsKey(renderer))
                {
                    if (renderer.material != null && renderer.material.HasProperty("_Color"))
                    {
                        renderer.material.color = originalColors[renderer];
                    }
                }
            }
        }
        
        /// <summary>
        /// Clears all enemy highlighting
        /// </summary>
        private void ClearEnemyHighlighting()
        {
            List<CombatEntity> toRestore = new List<CombatEntity>(highlightedEnemies.Keys);
            foreach (var enemy in toRestore)
            {
                RestoreEnemyHighlight(enemy);
            }
            highlightedEnemies.Clear();
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Draws gizmo in editor for range visualization
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (displayMode == DisplayMode.Never) return;
            
            float range = currentRange;
            if (range <= 0)
            {
                UpdateRange();
                range = currentRange;
            }
            
            if (range > 0)
            {
                Gizmos.color = rangeColor;
                Vector3 center = transform.position;
                
                // Draw circle using gizmos
                Vector3 prevPoint = center + new Vector3(range, 0, 0);
                for (int i = 1; i <= 32; i++)
                {
                    float angle = (i / 32f) * 360f * Mathf.Deg2Rad;
                    Vector3 newPoint = center + new Vector3(
                        Mathf.Cos(angle) * range,
                        0,
                        Mathf.Sin(angle) * range
                    );
                    Gizmos.DrawLine(prevPoint, newPoint);
                    prevPoint = newPoint;
                }
            }
        }
        #endif
    }
}
