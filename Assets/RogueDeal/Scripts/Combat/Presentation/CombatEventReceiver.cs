using UnityEngine;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Receives animation events and dispatches them to appropriate systems.
    /// Parses string-based events in format: "EventType:Param1:Param2"
    /// </summary>
    public class CombatEventReceiver : MonoBehaviour
    {
        private CombatExecutor combatExecutor;
        private WeaponHitbox weaponHitbox;
        private CombatVFXController vfxController;
        private CombatSFXController sfxController;
        
        private void Awake()
        {
            combatExecutor = GetComponent<CombatExecutor>();
            if (combatExecutor == null)
            {
                combatExecutor = GetComponentInParent<CombatExecutor>();
            }
            
            weaponHitbox = GetComponentInChildren<WeaponHitbox>();
            vfxController = GetComponent<CombatVFXController>();
            sfxController = GetComponent<CombatSFXController>();
        }
        
        /// <summary>
        /// Called by Animation Events. Parses event string and dispatches to appropriate handler.
        /// Format: "EventType:Param1:Param2"
        /// </summary>
        public void OnCombatEvent(string eventData)
        {
            if (string.IsNullOrEmpty(eventData))
            {
                Debug.LogWarning("[CombatEventReceiver] Received empty event data");
                return;
            }
            
            // Parse "EventType:Param1:Param2"
            string[] parts = eventData.Split(':');
            if (parts.Length == 0) return;
            
            string eventType = parts[0].Trim();
            string param1 = parts.Length > 1 ? parts[1].Trim() : "";
            string param2 = parts.Length > 2 ? parts[2].Trim() : "";
            
            // Dispatch to appropriate handler
            switch (eventType)
            {
                case "EnableHitbox":
                    HandleEnableHitbox();
                    break;
                    
                case "DisableHitbox":
                    HandleDisableHitbox();
                    break;
                    
                case "SpawnVFX":
                    HandleSpawnVFX(param1);
                    break;
                    
                case "PlaySFX":
                    HandlePlaySFX(param1);
                    break;
                    
                case "FireProjectile":
                    HandleFireProjectile(param1);
                    break;
                    
                case "SpawnPersistentAOE":
                    HandleSpawnPersistentAOE();
                    break;
                    
                case "MoveTo":
                    HandleMoveTo();
                    break;
                    
                case "ReturnToOrigin":
                    HandleReturnToOrigin();
                    break;
                    
                case "ComboHit":
                    HandleComboHit();
                    break;
                    
                case "ApplyEffects":
                    HandleApplyEffects();
                    break;
                    
                default:
                    Debug.LogWarning($"[CombatEventReceiver] Unknown event type: {eventType}");
                    break;
            }
        }
        
        private void HandleEnableHitbox()
        {
            weaponHitbox?.Enable();
        }
        
        private void HandleDisableHitbox()
        {
            weaponHitbox?.Disable();
        }
        
        private void HandleSpawnVFX(string vfxName)
        {
            if (string.IsNullOrEmpty(vfxName)) return;
            
            Transform spawnPoint = transform;
            if (combatExecutor != null)
            {
                var entity = combatExecutor.GetComponent<CombatEntity>();
                if (entity != null && entity.vfxSpawnPoint != null)
                {
                    spawnPoint = entity.vfxSpawnPoint;
                }
            }
            
            var action = combatExecutor?.GetCurrentAction();
            if (action?.effectBindings != null)
            {
                foreach (var binding in action.effectBindings)
                {
                    if (binding.eventName == vfxName && binding.vfxPrefab != null)
                    {
                        // Use existing VFX controller method if available
                        if (vfxController != null)
                        {
                            vfxController.PlayAbilityVFX(binding.vfxPrefab, spawnPoint.position);
                        }
                        else
                        {
                            // Fallback: instantiate directly
                            Instantiate(binding.vfxPrefab, spawnPoint.position, spawnPoint.rotation);
                        }
                        return;
                    }
                }
            }
            
            // If no binding found, try to find by name (optional - would need a lookup system)
            Debug.LogWarning($"[CombatEventReceiver] No VFX binding found for: {vfxName}");
        }
        
        private void HandlePlaySFX(string sfxName)
        {
            if (string.IsNullOrEmpty(sfxName)) return;
            
            var action = combatExecutor?.GetCurrentAction();
            if (action?.effectBindings != null)
            {
                foreach (var binding in action.effectBindings)
                {
                    if (binding.eventName == sfxName && binding.sfx != null)
                    {
                        // Use existing SFX controller method if available
                        if (sfxController != null)
                        {
                            sfxController.PlayAbilitySFX(binding.sfx);
                        }
                        else
                        {
                            // Fallback: play at point
                            AudioSource.PlayClipAtPoint(binding.sfx, transform.position);
                        }
                        return;
                    }
                }
            }
            
            // If no binding found, try to find by name (optional - would need a lookup system)
            Debug.LogWarning($"[CombatEventReceiver] No SFX binding found for: {sfxName}");
        }
        
        private void HandleFireProjectile()
        {
            var action = combatExecutor?.GetCurrentAction();
            if (action == null || !action.isProjectile || action.projectilePrefab == null) return;
            
            var targets = combatExecutor.GetCurrentTargets();
            if (targets == null || targets.Count == 0) return;
            
            var target = targets[0]; // Fire at first target
            var targetData = target.GetEntityData();
            if (targetData == null) return;
            
            Transform spawnPoint = transform;
            if (combatExecutor != null)
            {
                var entity = combatExecutor.GetComponent<CombatEntity>();
                if (entity != null && entity.vfxSpawnPoint != null)
                {
                    spawnPoint = entity.vfxSpawnPoint;
                }
            }
            
            var projectileObj = Instantiate(action.projectilePrefab, spawnPoint.position, Quaternion.identity);
            var projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(
                    target.transform,
                    action.projectileSpeed,
                    action.effects,
                    combatExecutor.GetEntityData()
                );
            }
        }
        
        private void HandleFireProjectile(string projectileName)
        {
            // Parameter is ignored for now, uses action's projectile prefab
            HandleFireProjectile();
        }
        
        private void HandleSpawnPersistentAOE()
        {
            var action = combatExecutor?.GetCurrentAction();
            if (action == null || !action.spawnsPersistentAOE || action.persistentAOEPrefab == null) return;
            
            var targetPos = combatExecutor.GetTargetPosition();
            var aoeObj = Instantiate(action.persistentAOEPrefab, targetPos, Quaternion.identity);
            var aoe = aoeObj.GetComponent<PersistentAOE>();
            if (aoe != null)
            {
                aoe.Initialize(
                    action.aoeRadius,
                    action.effects,
                    action.pulseCount,
                    action.pulseDuration,
                    combatExecutor.GetEntityData()
                );
            }
        }
        
        private void HandleMoveTo()
        {
            combatExecutor?.MoveToTarget();
        }
        
        private void HandleReturnToOrigin()
        {
            combatExecutor?.ReturnToOrigin();
        }
        
        private void HandleComboHit()
        {
            combatExecutor?.OnComboHit();
        }
        
        private void HandleApplyEffects()
        {
            var action = combatExecutor?.GetCurrentAction();
            if (action != null && action.effects != null)
            {
                combatExecutor.ApplyEffectsToTargets(action.effects);
            }
        }
    }
}

