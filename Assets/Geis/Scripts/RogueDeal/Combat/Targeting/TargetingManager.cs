using System.Collections.Generic;
using System.Linq;
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
        private Transform lockedOnAnchor = null;
        private bool isLockedOn = false;

        // Synty-style trigger-based target candidates
        private readonly System.Collections.Generic.List<GameObject> _targetCandidates = new System.Collections.Generic.List<GameObject>();
        
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

            // Synty-style: update best target from candidates and highlight
            UpdateBestTarget();
        }

        /// <summary>
        /// Adds a GameObject as a lock-on candidate (called from LockOnTarget.OnTriggerEnter).
        /// </summary>
        public void AddTargetCandidate(GameObject candidate)
        {
            if (candidate != null && !_targetCandidates.Contains(candidate))
            {
                _targetCandidates.Add(candidate);
            }
        }

        /// <summary>
        /// Removes a GameObject from lock-on candidates (called from LockOnTarget.OnTriggerExit).
        /// </summary>
        public void RemoveTargetCandidate(GameObject candidate)
        {
            if (candidate != null)
            {
                _targetCandidates.Remove(candidate);
            }
        }

        /// <summary>
        /// Toggle lock-on. When enabling, locks to best candidate. When disabling, clears lock.
        /// Call from controller when LockOnPressed.
        /// </summary>
        public void ToggleLockOn()
        {
            if (isLockedOn)
            {
                ClearLockOn();
            }
            else
            {
                GameObject best = GetBestTargetFromCandidates();
                if (best != null)
                {
                    var lockOnComponent = GetLockOnComponent(best);
                    var entity = lockOnComponent?.GetCombatEntity()
                        ?? best.GetComponent<CombatEntity>()
                        ?? best.GetComponentInParent<CombatEntity>();
                    if (entity != null)
                    {
                        SetLockOn(entity, lockOnComponent != null ? lockOnComponent.transform : null);
                    }
                }
            }
        }

        /// <summary>
        /// While locked on, cycles to the next valid target to the left or right of the current one.
        /// Negative direction cycles left, positive cycles right.
        /// </summary>
        public void CycleLockOnTarget(int direction)
        {
            if (!isLockedOn || lockedOnTarget == null)
                return;

            var orderedTargets = GetOrderedTargetEntities();
            if (orderedTargets.Count <= 1)
                return;

            int currentIndex = orderedTargets.IndexOf(lockedOnTarget);
            if (currentIndex < 0)
            {
                SetLockOn(orderedTargets[0], FindLockOnAnchor(orderedTargets[0]));
                return;
            }

            int step = direction < 0 ? -1 : 1;
            int nextIndex = (currentIndex + step + orderedTargets.Count) % orderedTargets.Count;
            SetLockOn(orderedTargets[nextIndex], FindLockOnAnchor(orderedTargets[nextIndex]));
        }

        /// <summary>
        /// Updates best target from candidates and highlights (Synty-style).
        /// </summary>
        private void UpdateBestTarget()
        {
            GameObject newBestTarget = GetBestTargetFromCandidates();

            if (!isLockedOn)
            {
                RefreshHighlights(newBestTarget, null);
            }
            else
            {
                // When locked: highlight locked target, or clear lock if it left range
                if (lockedOnTarget != null && ContainsTargetCandidate(lockedOnTarget))
                {
                    RefreshHighlights(null, lockedOnTarget);
                }
                else
                {
                    ClearLockOn();
                }
            }
        }

        private GameObject GetBestTargetFromCandidates()
        {
            var validCandidates = GetValidTargetCandidates();
            if (validCandidates.Count == 0) return null;
            if (validCandidates.Count == 1) return validCandidates[0].candidateObject;

            GameObject best = null;
            float bestScore = 0f;

            Vector3 playerPos = transform.position;
            Vector3 camPos = mainCamera != null ? mainCamera.transform.position : playerPos;
            Vector3 camForward = mainCamera != null ? mainCamera.transform.forward : transform.forward;

            foreach (var candidate in validCandidates)
            {
                float distance = Vector3.Distance(playerPos, candidate.entity.transform.position);
                float distanceScore = distance > 0.001f ? (1f / distance) * 100f : 1000f;

                Vector3 targetDir = (candidate.entity.transform.position - camPos).normalized;
                float angleInView = Vector3.Dot(targetDir, camForward);
                float angleScore = angleInView * 40f;

                float totalScore = distanceScore + angleScore;
                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    best = candidate.candidateObject;
                }
            }

            return best;
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
            
            // When locked on (Synty-style or click-to-select), return locked target first
            if (isLockedOn && lockedOnTarget != null)
            {
                var targets = new List<CombatEntity> { lockedOnTarget };
                var targetData = lockedOnTarget.GetEntityData();
                Vector3 targetPos = targetData != null ? targetData.position : lockedOnTarget.transform.position;
                return new TargetResult(targets, targetPos, true);
            }

            if (strategyToUse == null)
            {
                Debug.LogWarning("[TargetingManager] No targeting strategy available!");
                return new TargetResult(null, transform.position, false);
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
                                    SetLockOn(hitEntity, FindLockOnAnchor(hitEntity));
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
        
        private void SetLockOn(CombatEntity target, Transform anchor = null)
        {
            if (lockedOnTarget == target)
            {
                isLockedOn = target != null;
                lockedOnAnchor = target != null ? (anchor ?? FindLockOnAnchor(target)) : null;
                RefreshHighlights(null, lockedOnTarget);
                return;
            }

            if (lockedOnTarget != null)
                GetLockOnComponent(lockedOnTarget)?.Highlight(false, false);

            lockedOnTarget = target;
            lockedOnAnchor = target != null ? (anchor ?? FindLockOnAnchor(target)) : null;
            isLockedOn = true;
            RefreshHighlights(null, lockedOnTarget);
        }
        
        private void ClearLockOn()
        {
            lockedOnTarget = null;
            lockedOnAnchor = null;
            isLockedOn = false;
            RefreshHighlights(null, null);
        }

        /// <summary>
        /// Gets the transform to use for camera lock-on look-at (e.g. locked target's position).
        /// </summary>
        public Transform GetLockOnTargetTransform()
        {
            if (lockedOnTarget == null)
                return null;

            if (lockedOnAnchor != null)
                return lockedOnAnchor;

            if (lockedOnTarget.hitPoint != null)
                return lockedOnTarget.hitPoint;

            return lockedOnTarget.transform;
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

        private void RefreshHighlights(GameObject bestCandidate, CombatEntity lockedTarget)
        {
            foreach (var candidate in _targetCandidates)
            {
                if (candidate == null)
                    continue;

                var lockOn = GetLockOnComponent(candidate);
                if (lockOn == null)
                    continue;

                bool isLockedCandidate = lockedTarget != null && lockOn.GetCombatEntity() == lockedTarget;
                bool isBestCandidate = bestCandidate != null && candidate == bestCandidate;
                lockOn.Highlight(isLockedCandidate || isBestCandidate, isLockedCandidate);
            }
        }

        private bool ContainsTargetCandidate(CombatEntity entity)
        {
            if (entity == null)
                return false;

            foreach (var candidate in _targetCandidates)
            {
                if (candidate == null || !candidate.activeInHierarchy)
                    continue;

                if (TryGetCandidateEntity(candidate, out var candidateEntity) && candidateEntity == entity)
                    return true;
            }

            return false;
        }

        private LockOnTarget GetLockOnComponent(GameObject candidate)
        {
            if (candidate == null)
                return null;

            return candidate.GetComponent<LockOnTarget>()
                ?? candidate.GetComponentInChildren<LockOnTarget>()
                ?? candidate.GetComponentInParent<LockOnTarget>();
        }

        private LockOnTarget GetLockOnComponent(CombatEntity entity)
        {
            if (entity == null)
                return null;

            foreach (var candidate in _targetCandidates)
            {
                if (candidate == null)
                    continue;

                var lockOn = GetLockOnComponent(candidate);
                if (lockOn != null && lockOn.GetCombatEntity() == entity)
                    return lockOn;
            }

            return entity.GetComponent<LockOnTarget>()
                ?? entity.GetComponentInChildren<LockOnTarget>()
                ?? entity.GetComponentInParent<LockOnTarget>();
        }

        private List<(GameObject candidateObject, CombatEntity entity)> GetValidTargetCandidates()
        {
            var validTargets = new List<(GameObject candidateObject, CombatEntity entity)>();

            foreach (var candidate in _targetCandidates)
            {
                if (candidate == null || !candidate.activeInHierarchy)
                    continue;

                if (!TryGetCandidateEntity(candidate, out var entity))
                    continue;

                validTargets.Add((candidate, entity));
            }

            return validTargets;
        }

        private List<CombatEntity> GetOrderedTargetEntities()
        {
            return GetValidTargetCandidates()
                .OrderBy(candidate => GetHorizontalAngle(candidate.entity.transform.position))
                .ThenBy(candidate => Vector3.SqrMagnitude(candidate.entity.transform.position - transform.position))
                .Select(candidate => candidate.entity)
                .ToList();
        }

        private bool TryGetCandidateEntity(GameObject candidate, out CombatEntity entity)
        {
            entity = GetLockOnComponent(candidate)?.GetCombatEntity()
                ?? candidate.GetComponent<CombatEntity>()
                ?? candidate.GetComponentInParent<CombatEntity>();

            if (entity == null)
                return false;

            var data = entity.GetEntityData();
            return data != null && data.IsAlive;
        }

        private Transform FindLockOnAnchor(CombatEntity entity)
        {
            if (entity == null)
                return null;

            var lockOnComponent = GetLockOnComponent(entity);
            if (lockOnComponent != null)
                return lockOnComponent.transform;

            if (entity.hitPoint != null)
                return entity.hitPoint;

            return entity.transform;
        }

        private float GetHorizontalAngle(Vector3 targetPosition)
        {
            Vector3 origin = transform.position;
            Vector3 toTarget = targetPosition - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.0001f)
                return 0f;

            Vector3 referenceForward = mainCamera != null ? mainCamera.transform.forward : transform.forward;
            Vector3 referenceRight = mainCamera != null ? mainCamera.transform.right : transform.right;
            referenceForward.y = 0f;
            referenceRight.y = 0f;

            if (referenceForward.sqrMagnitude <= 0.0001f)
                referenceForward = transform.forward;
            if (referenceRight.sqrMagnitude <= 0.0001f)
                referenceRight = Vector3.Cross(Vector3.up, referenceForward).normalized;

            referenceForward.Normalize();
            referenceRight.Normalize();
            toTarget.Normalize();

            float horizontal = Vector3.Dot(toTarget, referenceRight);
            float vertical = Vector3.Dot(toTarget, referenceForward);
            return Mathf.Atan2(horizontal, vertical);
        }
    }
}
