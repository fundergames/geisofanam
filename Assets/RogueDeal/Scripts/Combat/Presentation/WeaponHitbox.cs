using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Collision-based hit detection for weapons. Activated/deactivated by animation events.
    /// Prevents double-hits on the same target per swing.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponHitbox : MonoBehaviour
    {
        [Header("Hit Detection Settings")]
        [Tooltip("Layer mask for valid targets")]
        public LayerMask targetLayers;
        
        [Tooltip("Tags that identify valid targets")]
        public string[] validTargetTags = { "Enemy", "Player" };
        
        private CombatExecutor combatExecutor;
        private HashSet<CombatEntity> hitThisSwing = new HashSet<CombatEntity>();
        private bool isActive = false;
        private Collider hitboxCollider;
        
        private void Awake()
        {
            combatExecutor = GetComponentInParent<CombatExecutor>();
            if (combatExecutor == null)
            {
                Debug.LogWarning($"[WeaponHitbox] No CombatExecutor found in parent hierarchy on {gameObject.name}");
            }
            
            hitboxCollider = GetComponent<Collider>();
            if (hitboxCollider != null)
            {
                hitboxCollider.isTrigger = true;
                hitboxCollider.enabled = false; // Start disabled
                Debug.Log($"[WeaponHitbox] Initialized on {gameObject.name} - Collider: {GetColliderSize()}, IsTrigger: {hitboxCollider.isTrigger}, Layer: {gameObject.layer}");
            }
            else
            {
                Debug.LogWarning($"[WeaponHitbox] No Collider found on {gameObject.name}");
            }
            
            // Ensure Rigidbody exists for trigger detection (Unity requirement)
            // At least one GameObject in a trigger collision must have a Rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true; // Don't let physics move it
                rb.useGravity = false; // No gravity needed
                Debug.Log($"[WeaponHitbox] Added kinematic Rigidbody to {gameObject.name} for trigger detection");
            }
            else
            {
                // Make sure it's kinematic (we don't want physics moving the weapon)
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
        
        private void OnDrawGizmos()
        {
            // Visualize the collider when enabled (for debugging)
            if (hitboxCollider != null && hitboxCollider.enabled && isActive)
            {
                Gizmos.color = Color.red;
                if (hitboxCollider is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (hitboxCollider is CapsuleCollider capsule)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireSphere(capsule.center, capsule.radius);
                }
                else if (hitboxCollider is SphereCollider sphere)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                }
            }
        }
        
        /// <summary>
        /// Enables the hitbox (called from animation event)
        /// </summary>
        public void Enable()
        {
            isActive = true;
            hitThisSwing.Clear();
            
            if (hitboxCollider != null)
            {
                hitboxCollider.enabled = true;
                Debug.Log($"[WeaponHitbox] ✅ Enabled hitbox on {gameObject.name} (collider: {hitboxCollider.enabled}, isTrigger: {hitboxCollider.isTrigger}, size: {GetColliderSize()})");
            }
            else
            {
                Debug.LogWarning($"[WeaponHitbox] Enable() called but hitboxCollider is null!");
            }
            
            // Debug: Check if CombatExecutor and action are available
            if (combatExecutor == null)
            {
                Debug.LogWarning($"[WeaponHitbox] CombatExecutor is null when enabling hitbox!");
            }
            else
            {
                var action = combatExecutor.GetCurrentAction();
                if (action == null)
                {
                    Debug.LogWarning($"[WeaponHitbox] No current action set when enabling hitbox! Make sure SetCurrentAction() was called.");
                }
                else
                {
                    Debug.Log($"[WeaponHitbox] Current action: {action.actionName} (effects: {action.effects?.Length ?? 0})");
                }
            }
            
            // Debug: Log collider info
            if (hitboxCollider != null)
            {
                Debug.Log($"[WeaponHitbox] Collider details - Type: {hitboxCollider.GetType().Name}, Enabled: {hitboxCollider.enabled}, IsTrigger: {hitboxCollider.isTrigger}, GameObject: {gameObject.name}, Layer: {gameObject.layer}");
            }
            
            // Debug: Check for nearby colliders manually
            CheckNearbyColliders();
        }
        
        /// <summary>
        /// Manually checks for nearby colliders to debug collision detection.
        /// Also serves as fallback when OnTriggerEnter fails due to Layer Collision Matrix settings.
        /// </summary>
        private void CheckNearbyColliders()
        {
            if (hitboxCollider == null) return;
            
            Vector3 center = transform.TransformPoint(hitboxCollider is BoxCollider box ? box.center : Vector3.zero);
            float checkRadius = 3f; // Check within 3 units
            
            Collider[] nearby = Physics.OverlapSphere(center, checkRadius);
            Debug.Log($"[WeaponHitbox] Found {nearby.Length} colliders within {checkRadius} units of weapon:");
            
            Bounds weaponBounds = hitboxCollider.bounds;
            Debug.Log($"[WeaponHitbox] Weapon collider bounds - Center: {weaponBounds.center}, Size: {weaponBounds.size}, Extents: {weaponBounds.extents}");
            
            foreach (var col in nearby)
            {
                if (col == hitboxCollider) continue; // Skip self
                
                float distance = Vector3.Distance(center, col.bounds.center);
                bool hasCombatEntity = col.GetComponent<CombatEntity>() != null || col.GetComponentInParent<CombatEntity>() != null;
                
                // Check if bounds actually intersect
                bool boundsIntersect = weaponBounds.Intersects(col.bounds);
                
                Debug.Log($"  - {col.gameObject.name} (Layer: {col.gameObject.layer}, Tag: {col.tag}, Distance: {distance:F2}, HasCombatEntity: {hasCombatEntity}, IsTrigger: {col.isTrigger}, BoundsIntersect: {boundsIntersect})");
                
                if (hasCombatEntity && boundsIntersect)
                {
                    // FALLBACK: Process hit manually since Layer Collision Matrix is blocking OnTriggerEnter
                    // Note: To fix permanently, enable Player × Enemy collision in Edit → Project Settings → Physics → Layer Collision Matrix
                    Debug.Log($"[WeaponHitbox] Manual hit detection: Processing hit on {col.gameObject.name}");
                    OnTriggerEnter(col);
                }
            }
            
            // Also check what layers can interact with weapon's layer
            int weaponLayer = gameObject.layer;
            Debug.Log($"[WeaponHitbox] Weapon is on layer {weaponLayer}. TargetLayers mask: {targetLayers.value} (0 = all layers)");
        }
        
        private string GetColliderSize()
        {
            if (hitboxCollider == null) return "N/A";
            
            if (hitboxCollider is BoxCollider box)
            {
                return $"Box({box.size.x:F2}, {box.size.y:F2}, {box.size.z:F2})";
            }
            else if (hitboxCollider is CapsuleCollider capsule)
            {
                return $"Capsule(r:{capsule.radius:F2}, h:{capsule.height:F2})";
            }
            else if (hitboxCollider is SphereCollider sphere)
            {
                return $"Sphere(r:{sphere.radius:F2})";
            }
            return hitboxCollider.GetType().Name;
        }
        
        /// <summary>
        /// Disables the hitbox (called from animation event)
        /// </summary>
        public void Disable()
        {
            isActive = false;
            
            if (hitboxCollider != null)
            {
                hitboxCollider.enabled = false;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[WeaponHitbox] 🔵 OnTriggerEnter called with {other.gameObject.name} (isActive: {isActive}, colliderEnabled: {hitboxCollider?.enabled ?? false})");
            
            if (!isActive)
            {
                Debug.Log($"[WeaponHitbox] Hitbox is not active, ignoring collision with {other.gameObject.name}");
                return;
            }
            
            if (hitboxCollider == null || !hitboxCollider.enabled)
            {
                Debug.LogWarning($"[WeaponHitbox] Collider is null or disabled when OnTriggerEnter fired!");
                return;
            }
            
            // Ignore lock-on indicators and other UI/visual indicators
            if (other.GetComponent<RogueDeal.Combat.Targeting.LockOnIndicator>() != null ||
                other.GetComponentInParent<RogueDeal.Combat.Targeting.LockOnIndicator>() != null)
            {
                Debug.Log($"[WeaponHitbox] Ignoring LockOnIndicator: {other.gameObject.name}");
                return;
            }
            
            // Check layer mask
            if (targetLayers != 0 && (targetLayers & (1 << other.gameObject.layer)) == 0)
            {
                Debug.Log($"[WeaponHitbox] {other.gameObject.name} is on layer {other.gameObject.layer} which is not in targetLayers mask {targetLayers.value}");
                return;
            }
            
            // Check tags
            bool validTag = false;
            if (validTargetTags != null && validTargetTags.Length > 0)
            {
                foreach (var tag in validTargetTags)
                {
                    if (other.CompareTag(tag))
                    {
                        validTag = true;
                        Debug.Log($"[WeaponHitbox] {other.gameObject.name} has valid tag: {tag}");
                        break;
                    }
                }
                if (!validTag)
                {
                    Debug.Log($"[WeaponHitbox] {other.gameObject.name} (tag: {other.tag}) does not match any valid target tags: {string.Join(", ", validTargetTags)}");
                }
            }
            else
            {
                validTag = true; // If no tags specified, accept all
                Debug.Log($"[WeaponHitbox] No target tags specified, accepting all");
            }
            
            if (!validTag) return;
            
            var target = other.GetComponent<CombatEntity>();
            if (target == null)
            {
                // Try parent
                target = other.GetComponentInParent<CombatEntity>();
                if (target != null)
                {
                    Debug.Log($"[WeaponHitbox] Found CombatEntity in parent of {other.gameObject.name}: {target.gameObject.name}");
                }
            }
            else
            {
                Debug.Log($"[WeaponHitbox] Found CombatEntity on {other.gameObject.name}");
            }
            
            if (target == null)
            {
                Debug.LogWarning($"[WeaponHitbox] No CombatEntity found on {other.gameObject.name} or its parents");
                return;
            }
            
            // Prevent double-hits
            if (hitThisSwing.Contains(target))
            {
                Debug.Log($"[WeaponHitbox] Already hit {target.gameObject.name} this swing, ignoring");
                return;
            }
            
            // Check if valid target (enemy, etc.)
            if (!IsValidTarget(target))
            {
                Debug.LogWarning($"[WeaponHitbox] {target.gameObject.name} is not a valid target (IsValidTarget returned false)");
                return;
            }
            
            // Check range - only hit enemies within weapon's attack range
            if (combatExecutor != null)
            {
                var attackerData = combatExecutor.GetEntityData();
                if (attackerData != null)
                {
                    float maxRange = GetMaxRange(attackerData);
                    Vector3 attackerPosition = combatExecutor.transform.position;
                    Vector3 targetPosition = target.transform.position;
                    float distance = Vector3.Distance(attackerPosition, targetPosition);
                    
                    if (distance > maxRange)
                    {
                        Debug.Log($"[WeaponHitbox] {target.gameObject.name} is too far away ({distance:F2} units) - max range is {maxRange:F2} units");
                        return;
                    }
                    
                    Debug.Log($"[WeaponHitbox] {target.gameObject.name} is within range ({distance:F2} / {maxRange:F2} units)");
                }
            }
            
            // Mark as hit
            hitThisSwing.Add(target);
            
            // Check for CombatExecutor
            if (combatExecutor == null)
            {
                Debug.LogError($"[WeaponHitbox] CombatExecutor is null! Cannot apply damage.");
                return;
            }
            
            // Apply effects from current action
            var action = combatExecutor.GetCurrentAction();
            if (action == null)
            {
                Debug.LogWarning($"[WeaponHitbox] Hit detected on {target.gameObject.name} but no current action is set! Make sure CombatExecutor.SetCurrentAction() was called.");
                return;
            }
            
            if (action.effects == null || action.effects.Length == 0)
            {
                Debug.LogWarning($"[WeaponHitbox] Action '{action.actionName}' has no effects!");
                return;
            }
            
            Debug.Log($"[WeaponHitbox] ✅ Hit detected on {target.gameObject.name} with action: {action.actionName} ({action.effects.Length} effects)");
            ApplyActionEffects(action, target);
        }
        
        private bool IsValidTarget(CombatEntity target)
        {
            if (target == null) return false;
            
            var targetData = target.GetEntityData();
            if (targetData == null || !targetData.IsAlive) return false;
            
            // Additional validation: don't hit self
            if (combatExecutor != null)
            {
                var attacker = combatExecutor.GetComponent<CombatEntity>();
                if (attacker == target) return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Gets the maximum attack range (from weapon, combat profile, or default)
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
        
        private void ApplyActionEffects(CombatAction action, CombatEntity target)
        {
            if (combatExecutor == null)
            {
                Debug.LogError("[WeaponHitbox] Cannot apply effects - CombatExecutor is null!");
                return;
            }
            
            var attackerData = combatExecutor.GetEntityData();
            if (attackerData == null)
            {
                Debug.LogError("[WeaponHitbox] Cannot apply effects - attacker data is null!");
                return;
            }
            
            var targetData = target.GetEntityData();
            if (targetData == null)
            {
                Debug.LogError("[WeaponHitbox] Cannot apply effects - target data is null!");
                return;
            }
            
            if (!targetData.IsAlive)
            {
                Debug.Log($"[WeaponHitbox] Target {target.gameObject.name} is already dead, skipping damage");
                return;
            }
            
            var weapon = attackerData.equippedWeapon;
            float hpBefore = targetData.currentHealth;
            bool wasCritical = false;
            
            // Calculate and apply all effects
            foreach (var effect in action.effects)
            {
                if (effect == null) continue;
                
                var calculated = effect.Calculate(attackerData, targetData, weapon);
                
                // Track if any effect was a critical hit
                if (calculated.wasCritical)
                {
                    wasCritical = true;
                }
                
                effect.Apply(targetData, calculated);
            }
            
            float hpAfter = targetData.currentHealth;
            float damageDealt = hpBefore - hpAfter;
            
            if (damageDealt > 0)
            {
                Debug.Log($"[WeaponHitbox] Applied {damageDealt:F1} damage to {target.gameObject.name} (HP: {hpBefore:F1} → {hpAfter:F1})");
                
                // Trigger hit reaction: animation + damage popup
                combatExecutor.TriggerHitReaction(target, damageDealt, wasCritical);
            }
        }
    }
}

