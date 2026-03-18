using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Targeting;

namespace RogueDeal.Combat.Targeting
{
    /// <summary>
    /// Manages targeting mode for the player. Allows switching between different targeting strategies.
    /// </summary>
    public class TargetingManager : MonoBehaviour
    {
        [Header("Targeting Mode")]
        [Tooltip("Current targeting strategy to use")]
        [SerializeField] private TargetingStrategy currentTargetingStrategy;
        
        [Header("Default Strategies")]
        [Tooltip("Default strategies for each targeting type (for quick switching)")]
        [SerializeField] private TargetingStrategy nearestEnemyStrategy;
        [SerializeField] private TargetingStrategy coneTargetingStrategy;
        [SerializeField] private TargetingStrategy clickToSelectStrategy;
        [SerializeField] private TargetingStrategy directionalStrategy;
        [SerializeField] private TargetingStrategy groundTargetingStrategy;
        
        [Header("Debug")]
        [Tooltip("Enable debug keybinds to switch targeting modes (for testing)")]
        [SerializeField] private bool enableDebugKeybinds = false;
        
        [Tooltip("Range indicator component (optional, for visualizing attack range)")]
        [SerializeField] private RangeIndicator rangeIndicator;
        
        private CombatEntity combatEntity;
        private Camera mainCamera;
        
        // Lock-on state (for click-to-select)
        private CombatEntity lockedOnTarget = null;
        private bool isLockedOn = false;
        
        private void Awake()
        {
            combatEntity = GetComponent<CombatEntity>();
            if (combatEntity == null)
            {
                combatEntity = GetComponentInParent<CombatEntity>();
            }
            
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }
            
            // If no strategy assigned, default to nearest enemy
            if (currentTargetingStrategy == null)
            {
                currentTargetingStrategy = nearestEnemyStrategy;
            }
            
            // Get or create range indicator
            if (rangeIndicator == null)
            {
                rangeIndicator = GetComponent<RangeIndicator>();
                if (rangeIndicator == null)
                {
                    rangeIndicator = GetComponentInChildren<RangeIndicator>();
                }
                
                // Create if it doesn't exist
                if (rangeIndicator == null)
                {
                    GameObject indicatorObj = new GameObject("RangeIndicator");
                    indicatorObj.transform.SetParent(transform);
                    indicatorObj.transform.localPosition = Vector3.zero;
                    rangeIndicator = indicatorObj.AddComponent<RangeIndicator>();
                }
            }
        }
        
        private void Update()
        {
            if (enableDebugKeybinds)
            {
                HandleDebugInput();
            }
            
            // Update lock-on state
            UpdateLockOnState();
        }
        
        private void HandleDebugInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Number keys to switch targeting modes
            if (keyboard.digit1Key.wasPressedThisFrame && nearestEnemyStrategy != null)
            {
                SetTargetingStrategy(nearestEnemyStrategy);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame && coneTargetingStrategy != null)
            {
                SetTargetingStrategy(coneTargetingStrategy);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame && clickToSelectStrategy != null)
            {
                SetTargetingStrategy(clickToSelectStrategy);
            }
            else if (keyboard.digit4Key.wasPressedThisFrame && directionalStrategy != null)
            {
                SetTargetingStrategy(directionalStrategy);
            }
            else if (keyboard.digit5Key.wasPressedThisFrame && groundTargetingStrategy != null)
            {
                SetTargetingStrategy(groundTargetingStrategy);
            }

            // Toggle range indicator (R key)
            if (keyboard.rKey.wasPressedThisFrame)
            {
                if (rangeIndicator == null)
                {
                    // Try to find it again
                    rangeIndicator = GetComponent<RangeIndicator>();
                    if (rangeIndicator == null)
                    {
                        rangeIndicator = GetComponentInChildren<RangeIndicator>();
                    }
                    
                    // Create if still not found
                    if (rangeIndicator == null)
                    {
                        GameObject indicatorObj = new GameObject("RangeIndicator");
                        indicatorObj.transform.SetParent(transform);
                        indicatorObj.transform.localPosition = Vector3.zero;
                        rangeIndicator = indicatorObj.AddComponent<RangeIndicator>();
                    }
                }
                
                if (rangeIndicator != null)
                {
                    rangeIndicator.Toggle();
                }
            }
        }
        
        private void UpdateLockOnState()
        {
            if (lockedOnTarget != null)
            {
                // Check if target is still valid
                if (lockedOnTarget == null || !lockedOnTarget.gameObject.activeInHierarchy)
                {
                    ClearLockOn();
                    return;
                }
                
                var targetData = lockedOnTarget.GetEntityData();
                if (targetData == null || !targetData.IsAlive)
                {
                    ClearLockOn();
                    return;
                }
                
                // Check if target is out of range
                if (combatEntity != null)
                {
                    var attackerData = combatEntity.GetEntityData();
                    if (attackerData != null && attackerData.equippedWeapon != null)
                    {
                        float maxRange = GetMaxRange(attackerData);
                        float distance = Vector3.Distance(combatEntity.transform.position, lockedOnTarget.transform.position);
                        if (distance > maxRange)
                        {
                            ClearLockOn();
                            return;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets targets for the given action using the current targeting strategy
        /// </summary>
        public TargetResult GetTargets(CombatAction action)
        {
            if (combatEntity == null)
            {
                Debug.LogError("[TargetingManager] CombatEntity is null!");
                return new TargetResult(null, transform.position, false);
            }
            
            var attackerData = combatEntity.GetEntityData();
            if (attackerData == null)
            {
                Debug.LogError("[TargetingManager] CombatEntityData is null!");
                return new TargetResult(null, transform.position, false);
            }
            
            // Sync position
            attackerData.position = transform.position;
            
            // Use action's targeting strategy if it has one, otherwise use current strategy
            TargetingStrategy strategyToUse = action.targetingStrategy != null 
                ? action.targetingStrategy 
                : currentTargetingStrategy;
            
            if (strategyToUse == null)
            {
                Debug.LogWarning("[TargetingManager] No targeting strategy available!");
                return new TargetResult(null, transform.position, false);
            }
            
            // For click-to-select, check if we have a locked-on target
            if (strategyToUse is ClickToSelectTargetingStrategy)
            {
                if (isLockedOn && lockedOnTarget != null)
                {
                    var targets = new List<CombatEntity> { lockedOnTarget };
                    var targetData = lockedOnTarget.GetEntityData();
                    Vector3 targetPos = targetData != null ? targetData.position : lockedOnTarget.transform.position;
                    return new TargetResult(targets, targetPos, true);
                }
            }
            
            return strategyToUse.ResolveTargets(attackerData);
        }
        
        /// <summary>
        /// Handles mouse click for click-to-select targeting
        /// </summary>
        public void HandleMouseClick(Vector3 mousePosition)
        {
            if (currentTargetingStrategy is ClickToSelectTargetingStrategy || 
                currentTargetingStrategy is GroundTargetedAOE)
            {
                // Raycast from camera through mouse position
                Ray ray = mainCamera.ScreenPointToRay(mousePosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, 1000f))
                {
                    // Check if we hit a targetable entity
                    CombatEntity hitEntity = hit.collider.GetComponent<CombatEntity>();
                    if (hitEntity == null)
                    {
                        hitEntity = hit.collider.GetComponentInParent<CombatEntity>();
                    }
                    
                    if (hitEntity != null && hitEntity != combatEntity)
                    {
                        var entityData = hitEntity.GetEntityData();
                        if (entityData != null && entityData.IsAlive)
                        {
                            // Click-to-select: toggle lock-on
                            if (currentTargetingStrategy is ClickToSelectTargetingStrategy)
                            {
                                if (lockedOnTarget == hitEntity)
                                {
                                    // Clicked same target - deselect
                                    ClearLockOn();
                                }
                                else
                                {
                                    // Clicked different target - lock on
                                    SetLockOn(hitEntity);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Clicked ground or non-targetable - clear lock-on
                        if (currentTargetingStrategy is ClickToSelectTargetingStrategy)
                        {
                            ClearLockOn();
                        }
                        else if (currentTargetingStrategy is GroundTargetedAOE groundStrategy)
                        {
                            // Set ground target position
                            groundStrategy.SetTargetPosition(hit.point);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Sets the current targeting strategy
        /// </summary>
        public void SetTargetingStrategy(TargetingStrategy strategy)
        {
            currentTargetingStrategy = strategy;
            
            // Clear lock-on when switching strategies
            if (!(strategy is ClickToSelectTargetingStrategy))
            {
                ClearLockOn();
            }
        }
        
        /// <summary>
        /// Gets the current targeting strategy
        /// </summary>
        public TargetingStrategy GetCurrentStrategy()
        {
            return currentTargetingStrategy;
        }
        
        /// <summary>
        /// Gets the locked-on target (if any)
        /// </summary>
        public CombatEntity GetLockedOnTarget()
        {
            return lockedOnTarget;
        }
        
        /// <summary>
        /// Checks if currently locked on to a target
        /// </summary>
        public bool IsLockedOn()
        {
            return isLockedOn && lockedOnTarget != null;
        }
        
        private void SetLockOn(CombatEntity target)
        {
            lockedOnTarget = target;
            isLockedOn = true;
        }
        
        private void ClearLockOn()
        {
            lockedOnTarget = null;
            isLockedOn = false;
        }
        
        /// <summary>
        /// Gets the maximum range for targeting (from weapon, profile, or default)
        /// </summary>
        private float GetMaxRange(CombatEntityData attackerData)
        {
            // Priority: Weapon > CombatProfile > Default
            if (attackerData.equippedWeapon != null && attackerData.equippedWeapon.maxRange > 0)
            {
                return attackerData.equippedWeapon.maxRange;
            }
            
            if (attackerData.combatProfile != null && attackerData.combatProfile.engagementDistance > 0)
            {
                return attackerData.combatProfile.engagementDistance;
            }
            
            return 2f; // Default melee range
        }
    }
}
