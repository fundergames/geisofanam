# Combat System Implementation Guide

## Quick Reference: Component Relationships

```
CombatAction (SO)
    ├── TargetingStrategy (SO)
    ├── BaseEffect[] (SO)
    ├── Weapon (SO)
    └── CooldownConfiguration
    
CombatExecutor (MB)
    ├── References CombatAction
    ├── Uses CombatEntityData
    ├── Manages WeaponHitbox
    └── Triggers Animator
    
CombatEventReceiver (MB)
    ├── Receives Animation Events
    ├── Parses "EventType:Param1:Param2"
    └── Dispatches to systems
    
WeaponHitbox (MB)
    ├── Activated by Animation Events
    ├── Detects Collisions
    └── Applies Effects via CombatExecutor
    
CombatEntityData (Pure C#)
    ├── Stats
    ├── ActiveStatusEffects
    └── EquippedWeapon
    
CombatSimulator (Pure C#)
    ├── Uses CombatEntityData
    ├── Runs Combat Logic
    └── Returns SimulationResult
```

## Code Structure Examples

### 1. CombatEntityData (Pure C# - Simulation Ready)

```csharp
namespace RogueDeal.Combat.Core
{
    [System.Serializable]
    public class CombatEntityData
    {
        // Core Stats
        public float maxHealth;
        public float currentHealth;
        public float attack;
        public float defense;
        public float magicPower;
        public float speed;
        
        // Equipment
        public Weapon equippedWeapon;
        public CharacterClass characterClass;
        public CombatProfile combatProfile;
        
        // Status Effects
        public List<ActiveStatusEffect> activeStatusEffects = new List<ActiveStatusEffect>();
        
        // Cooldowns (managed by ActionCooldownManager)
        public Dictionary<CombatAction, CooldownState> actionCooldowns = new Dictionary<CombatAction, CooldownState>();
        
        public CombatEntityData Clone()
        {
            return new CombatEntityData
            {
                maxHealth = this.maxHealth,
                currentHealth = this.currentHealth,
                attack = this.attack,
                defense = this.defense,
                magicPower = this.magicPower,
                speed = this.speed,
                equippedWeapon = this.equippedWeapon, // Reference, not cloned
                characterClass = this.characterClass,
                combatProfile = this.combatProfile,
                activeStatusEffects = this.activeStatusEffects.Select(e => e.Clone()).ToList(),
                actionCooldowns = new Dictionary<CombatAction, CooldownState>()
            };
        }
        
        public void OnTurnStart()
        {
            // Process status effects
            foreach (var effect in activeStatusEffects.ToList())
            {
                effect.ProcessTurn(this);
                effect.duration--;
                if (effect.duration <= 0)
                {
                    activeStatusEffects.Remove(effect);
                }
            }
        }
    }
}
```

### 2. BaseEffect System (Fully Data-Driven)

```csharp
namespace RogueDeal.Combat.Core.Effects
{
    public abstract class BaseEffect : ScriptableObject
    {
        public abstract CalculatedEffect Calculate(
            CombatEntityData attacker, 
            CombatEntityData target, 
            Weapon weapon
        );
        
        public abstract void Apply(CombatEntityData target, CalculatedEffect calculated);
    }
    
    [CreateAssetMenu(menuName = "Combat/Effects/Damage Effect")]
    public class DamageEffect : BaseEffect
    {
        [Header("Damage Settings")]
        public float baseDamage;
        public DamageType damageType;
        public StatType scalingStat = StatType.Attack;
        public float scalingMultiplier = 1f;
        public bool canCrit = true;
        
        public override CalculatedEffect Calculate(
            CombatEntityData attacker, 
            CombatEntityData target, 
            Weapon weapon
        )
        {
            float damage = baseDamage;
            
            // Apply stat scaling
            float statValue = attacker.GetStat(scalingStat);
            damage += statValue * scalingMultiplier;
            
            // Apply weapon multiplier
            if (weapon != null && weapon.damageTypeMultipliers.ContainsKey(damageType))
            {
                damage *= weapon.damageTypeMultipliers[damageType];
            }
            
            // Apply defense
            damage = Mathf.Max(0, damage - target.defense);
            
            // Critical hit
            bool wasCritical = canCrit && Random.value < attacker.GetCritChance();
            if (wasCritical)
            {
                damage *= 1f + attacker.GetCritDamage();
            }
            
            return new CalculatedEffect
            {
                effectType = EffectType.Damage,
                damageAmount = damage,
                wasCritical = wasCritical,
                damageType = damageType
            };
        }
        
        public override void Apply(CombatEntityData target, CalculatedEffect calculated)
        {
            target.currentHealth = Mathf.Max(0, target.currentHealth - calculated.damageAmount);
        }
    }
    
    [CreateAssetMenu(menuName = "Combat/Effects/Stat Modifier Effect")]
    public class StatModifierEffect : BaseEffect
    {
        public StatType targetStat;
        public ModifierType modifierType; // Add, Multiply, Set
        public float baseValue;
        public bool isInstant;
        public int duration; // Turns, if not instant
        
        public override CalculatedEffect Calculate(...)
        {
            // Calculate stat modification
            return new CalculatedEffect { /* ... */ };
        }
        
        public override void Apply(CombatEntityData target, CalculatedEffect calculated)
        {
            if (isInstant)
            {
                ApplyModifier(target, calculated);
            }
            else
            {
                // Add to status effects
                target.activeStatusEffects.Add(new ActiveStatusEffect
                {
                    effectType = EffectType.StatModifier,
                    duration = duration,
                    modifier = calculated
                });
            }
        }
    }
    
    [CreateAssetMenu(menuName = "Combat/Effects/Multi Effect")]
    public class MultiEffect : BaseEffect
    {
        public BaseEffect[] effects;
        
        public override CalculatedEffect Calculate(...)
        {
            // Calculate all effects
            return new CalculatedEffect { /* ... */ };
        }
        
        public override void Apply(CombatEntityData target, CalculatedEffect calculated)
        {
            // Apply all effects in sequence
            foreach (var effect in effects)
            {
                var calc = effect.Calculate(/* ... */);
                effect.Apply(target, calc);
            }
        }
    }
}
```

### 3. CombatAction (Enhanced ScriptableObject)

```csharp
namespace RogueDeal.Combat.Core
{
    [CreateAssetMenu(menuName = "Combat/Action")]
    public class CombatAction : ScriptableObject
    {
        [Header("Basic Info")]
        public string actionName;
        public Sprite icon;
        
        [Header("Animation")]
        public string animationTrigger;
        public AnimationClip[] comboAnimations; // For multi-hit combos
        
        [Header("Targeting")]
        public TargetingStrategy targetingStrategy;
        
        [Header("Effects")]
        public BaseEffect[] effects;
        
        [Header("Combo Data")]
        public bool isCombo;
        public int comboHitCount;
        public BaseEffect[] perHitEffects; // Optional per-hit effects
        
        [Header("Projectile")]
        public bool isProjectile;
        public GameObject projectilePrefab;
        public float projectileSpeed;
        
        [Header("Persistent AOE")]
        public bool spawnsPersistentAOE;
        public GameObject persistentAOEPrefab;
        public float aoeRadius;
        public int pulseCount;
        public float pulseDuration;
        
        [Header("Cooldown")]
        public CooldownConfiguration cooldownConfig;
        
        [Header("Visual Effects")]
        public EffectBinding[] effectBindings; // Maps animation events to VFX/SFX
    }
    
    [System.Serializable]
    public class EffectBinding
    {
        public string eventName;
        public GameObject vfxPrefab;
        public AudioClip sfx;
    }
}
```

### 4. CombatExecutor (MonoBehaviour - Presentation)

```csharp
namespace RogueDeal.Combat.Presentation
{
    public class CombatExecutor : MonoBehaviour
    {
        private CombatEntity combatEntity;
        private CombatEntityData entityData;
        private ActionCooldownManager cooldownManager;
        private Animator animator;
        
        // Current action context
        private CombatAction currentAction;
        private List<CombatEntity> currentTargets;
        private int currentComboHit = 0;
        
        private void Awake()
        {
            combatEntity = GetComponent<CombatEntity>();
            entityData = combatEntity.GetEntityData();
            cooldownManager = new ActionCooldownManager(entityData);
            animator = combatEntity.animator;
        }
        
        public bool ExecuteAction(CombatAction action)
        {
            // Check cooldown
            if (!cooldownManager.IsActionAvailable(action))
                return false;
            
            // Resolve targets
            var targetResult = action.targetingStrategy.ResolveTargets(entityData);
            if (!targetResult.isReady)
                return false;
            
            currentAction = action;
            currentTargets = targetResult.targets;
            currentComboHit = 0;
            
            // Check if movement needed
            if (entityData.combatProfile != null)
            {
                CheckAndMoveToTarget(targetResult.targetPosition);
            }
            
            // Trigger animation
            if (action.isCombo)
            {
                StartCombo(action);
            }
            else
            {
                animator.SetTrigger(action.animationTrigger);
            }
            
            // Start cooldown
            cooldownManager.StartCooldown(action);
            
            return true;
        }
        
        private void StartCombo(CombatAction action)
        {
            // Play first combo animation
            if (action.comboAnimations.Length > 0)
            {
                // Use animation override or trigger
                animator.Play(action.comboAnimations[0].name);
            }
        }
        
        public void OnComboHit()
        {
            currentComboHit++;
            // Apply per-hit effects if any
            if (currentAction.perHitEffects != null && 
                currentComboHit <= currentAction.perHitEffects.Length)
            {
                var effect = currentAction.perHitEffects[currentComboHit - 1];
                ApplyEffectToTargets(effect);
            }
            
            // Check if combo complete
            if (currentComboHit >= currentAction.comboHitCount)
            {
                currentAction = null;
                currentTargets = null;
            }
        }
        
        private void ApplyEffectToTargets(BaseEffect effect)
        {
            foreach (var target in currentTargets)
            {
                var targetData = target.GetEntityData();
                var calculated = effect.Calculate(entityData, targetData, entityData.equippedWeapon);
                effect.Apply(targetData, calculated);
            }
        }
    }
}
```

### 5. CombatEventReceiver (Enhanced - String Parsing)

```csharp
namespace RogueDeal.Combat.Presentation
{
    public class CombatEventReceiver : MonoBehaviour
    {
        private CombatExecutor combatExecutor;
        private WeaponHitbox weaponHitbox;
        private CombatVFXController vfxController;
        private CombatSFXController sfxController;
        
        private void Awake()
        {
            combatExecutor = GetComponent<CombatExecutor>();
            weaponHitbox = GetComponentInChildren<WeaponHitbox>();
            vfxController = GetComponent<CombatVFXController>();
            sfxController = GetComponent<CombatSFXController>();
        }
        
        // Called by Animation Events
        public void OnCombatEvent(string eventData)
        {
            // Parse "EventType:Param1:Param2"
            string[] parts = eventData.Split(':');
            if (parts.Length == 0) return;
            
            string eventType = parts[0];
            string param1 = parts.Length > 1 ? parts[1] : "";
            string param2 = parts.Length > 2 ? parts[2] : "";
            
            switch (eventType)
            {
                case "EnableHitbox":
                    weaponHitbox?.Enable();
                    break;
                    
                case "DisableHitbox":
                    weaponHitbox?.Disable();
                    break;
                    
                case "SpawnVFX":
                    vfxController?.SpawnVFX(param1);
                    break;
                    
                case "PlaySFX":
                    sfxController?.PlaySFX(param1);
                    break;
                    
                case "FireProjectile":
                    FireProjectile(param1);
                    break;
                    
                case "SpawnPersistentAOE":
                    SpawnPersistentAOE();
                    break;
                    
                case "MoveTo":
                    MoveToTarget();
                    break;
                    
                case "ReturnToOrigin":
                    ReturnToOrigin();
                    break;
                    
                case "ComboHit":
                    combatExecutor?.OnComboHit();
                    break;
            }
        }
        
        private void FireProjectile(string projectileName)
        {
            var action = combatExecutor?.GetCurrentAction();
            if (action == null || !action.isProjectile) return;
            
            var target = combatExecutor.GetCurrentTargets()?.FirstOrDefault();
            if (target == null) return;
            
            var projectile = Instantiate(action.projectilePrefab, transform.position, Quaternion.identity);
            var projectileComponent = projectile.GetComponent<Projectile>();
            projectileComponent.Initialize(
                target.transform,
                action.projectileSpeed,
                action.effects,
                combatExecutor.GetEntityData()
            );
        }
        
        private void SpawnPersistentAOE()
        {
            var action = combatExecutor?.GetCurrentAction();
            if (action == null || !action.spawnsPersistentAOE) return;
            
            var targetPos = combatExecutor.GetTargetPosition();
            var aoe = Instantiate(action.persistentAOEPrefab, targetPos, Quaternion.identity);
            var aoeComponent = aoe.GetComponent<PersistentAOE>();
            aoeComponent.Initialize(
                action.aoeRadius,
                action.effects,
                action.pulseCount,
                action.pulseDuration,
                combatExecutor.GetEntityData()
            );
        }
    }
}
```

### 6. WeaponHitbox (Collision Detection)

```csharp
namespace RogueDeal.Combat.Presentation
{
    public class WeaponHitbox : MonoBehaviour
    {
        private CombatExecutor combatExecutor;
        private HashSet<CombatEntity> hitThisSwing = new HashSet<CombatEntity>();
        private bool isActive = false;
        private LayerMask targetLayers;
        
        private void Awake()
        {
            combatExecutor = GetComponentInParent<CombatExecutor>();
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }
        
        public void Enable()
        {
            isActive = true;
            hitThisSwing.Clear();
        }
        
        public void Disable()
        {
            isActive = false;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;
            
            var target = other.GetComponent<CombatEntity>();
            if (target == null) return;
            
            // Prevent double-hits
            if (hitThisSwing.Contains(target)) return;
            
            // Check if valid target (enemy, etc.)
            if (!IsValidTarget(target)) return;
            
            hitThisSwing.Add(target);
            
            // Apply effects
            var action = combatExecutor?.GetCurrentAction();
            if (action != null)
            {
                ApplyActionEffects(action, target);
            }
        }
        
        private void ApplyActionEffects(CombatAction action, CombatEntity target)
        {
            var attackerData = combatExecutor.GetEntityData();
            var targetData = target.GetEntityData();
            var weapon = attackerData.equippedWeapon;
            
            foreach (var effect in action.effects)
            {
                var calculated = effect.Calculate(attackerData, targetData, weapon);
                effect.Apply(targetData, calculated);
            }
        }
    }
}
```

### 7. CombatSimulator (Headless Simulation)

```csharp
namespace RogueDeal.Combat.Simulation
{
    public class CombatSimulator
    {
        private System.Random rng;
        
        public CombatSimulator(int? seed = null)
        {
            rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }
        
        public SimulationResult SimulateCombat(
            CombatEntityData attacker,
            CombatEntityData defender,
            List<CombatAction> availableActions
        )
        {
            var attackerClone = attacker.Clone();
            var defenderClone = defender.Clone();
            
            var cooldownManager = new ActionCooldownManager(attackerClone);
            int turnCount = 0;
            float totalDamageDealt = 0;
            float totalDamageTaken = 0;
            var actionUsage = new Dictionary<CombatAction, int>();
            
            while (attackerClone.currentHealth > 0 && defenderClone.currentHealth > 0)
            {
                turnCount++;
                
                // Attacker's turn
                attackerClone.OnTurnStart();
                var action = SelectAction(attackerClone, availableActions, cooldownManager);
                if (action != null)
                {
                    ExecuteAction(action, attackerClone, defenderClone);
                    if (!actionUsage.ContainsKey(action))
                        actionUsage[action] = 0;
                    actionUsage[action]++;
                    cooldownManager.OnTurnStart();
                }
                
                if (defenderClone.currentHealth <= 0) break;
                
                // Defender's turn (simplified - could use AI)
                defenderClone.OnTurnStart();
                // Simple attack logic
                float damage = defenderClone.attack - attackerClone.defense;
                attackerClone.currentHealth = Mathf.Max(0, attackerClone.currentHealth - damage);
                totalDamageTaken += damage;
            }
            
            return new SimulationResult
            {
                attackerWon = attackerClone.currentHealth > 0,
                turnCount = turnCount,
                totalDamageDealt = totalDamageDealt,
                totalDamageTaken = totalDamageTaken,
                actionUsage = actionUsage
            };
        }
        
        private CombatAction SelectAction(
            CombatEntityData entity,
            List<CombatAction> actions,
            ActionCooldownManager cooldownManager
        )
        {
            var available = actions.Where(a => cooldownManager.IsActionAvailable(a)).ToList();
            if (available.Count == 0) return null;
            return available[rng.Next(available.Count)];
        }
        
        private void ExecuteAction(
            CombatAction action,
            CombatEntityData attacker,
            CombatEntityData target
        )
        {
            foreach (var effect in action.effects)
            {
                var calculated = effect.Calculate(attacker, target, attacker.equippedWeapon);
                effect.Apply(target, calculated);
            }
        }
    }
}
```

### 8. MonteCarloBalancer (Balance Testing)

```csharp
namespace RogueDeal.Combat.Simulation
{
    public class MonteCarloBalancer
    {
        public BalanceReport RunSimulation(
            CombatEntityData player,
            CombatEntityData enemy,
            List<CombatAction> playerActions,
            int iterations = 10000,
            int? seed = null
        )
        {
            var results = new List<SimulationResult>();
            
            for (int i = 0; i < iterations; i++)
            {
                var simulator = new CombatSimulator(seed.HasValue ? seed.Value + i : null);
                var result = simulator.SimulateCombat(player, enemy, playerActions);
                results.Add(result);
            }
            
            return new BalanceReport
            {
                totalSimulations = iterations,
                playerWins = results.Count(r => r.attackerWon),
                enemyWins = results.Count(r => !r.attackerWon),
                averageTurnsToKill = results.Where(r => r.attackerWon).Select(r => r.turnCount).DefaultIfEmpty(0).Average(),
                averageDPS = results.Average(r => r.totalDamageDealt / Mathf.Max(1, r.turnCount)),
                actionUsageFrequency = CalculateActionUsageFrequency(results)
            };
        }
        
        private Dictionary<CombatAction, float> CalculateActionUsageFrequency(
            List<SimulationResult> results
        )
        {
            // Aggregate action usage across all simulations
            // ...
        }
    }
}
```

## Integration with Existing Systems

### Updating CombatEntity

```csharp
public class CombatEntity : MonoBehaviour
{
    // Keep existing fields
    public HeroData heroData;
    public CombatStats stats { get; private set; }
    
    // Add new fields
    private CombatEntityData entityData;
    private CombatExecutor combatExecutor;
    
    private void Awake()
    {
        // Existing initialization
        InitializeStats();
        
        // New initialization
        entityData = CreateEntityDataFromStats();
        combatExecutor = GetComponent<CombatExecutor>();
        if (combatExecutor == null)
            combatExecutor = gameObject.AddComponent<CombatExecutor>();
    }
    
    public CombatEntityData GetEntityData()
    {
        return entityData;
    }
    
    private CombatEntityData CreateEntityDataFromStats()
    {
        return new CombatEntityData
        {
            maxHealth = stats.MaxHealth,
            currentHealth = stats.CurrentHealth,
            attack = stats.GetStat(StatType.Attack),
            defense = stats.GetStat(StatType.Defense),
            // ... map other stats
        };
    }
}
```

## Animation Event Setup Guide

### Example: Fire Slash Attack

1. **Animation Timeline**:
   - Frame 10: `OnCombatEvent("MoveTo")`
   - Frame 20: `OnCombatEvent("EnableHitbox")`
   - Frame 25: `OnCombatEvent("SpawnVFX:FireSlash")`
   - Frame 25: `OnCombatEvent("PlaySFX:SwordFire")`
   - Frame 30: `OnCombatEvent("DisableHitbox")`
   - Frame 40: `OnCombatEvent("ReturnToOrigin")`

2. **CombatAction Configuration**:
   - Animation Trigger: "FireSlash"
   - Effects: MultiEffect (Physical Damage + Fire Damage + Burn Status)
   - Targeting: SingleTargetSelector
   - Cooldown: 3 turns

## Testing Checklist

- [ ] Basic attack with damage effect
- [ ] Multi-hit combo with per-hit effects
- [ ] Projectile attack
- [ ] Persistent AOE with pulsing
- [ ] Status effects (burn, poison, regen)
- [ ] Cooldown system (turn-based, time-based, per-combat)
- [ ] Weapon damage type multipliers
- [ ] Combat profile movement/positioning
- [ ] Simulation runs headless
- [ ] Monte Carlo balance testing
- [ ] Editor tools for running simulations


