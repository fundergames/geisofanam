# Modular Combat System Architecture Proposal

## Executive Summary

This document proposes a comprehensive, modular combat system architecture that builds upon the existing RogueDeal combat infrastructure while addressing all requirements for animation-driven, data-driven, simulatable combat across multiple game modes (video poker roguelite, turn-based RPG, real-time action, open-world).

## Current Architecture Analysis

### Existing Strengths

1. **CombatEntity & CombatStats**: Solid foundation with health, stats, and basic combat mechanics
2. **AbilityData & EffectData**: ScriptableObject-based data system already in place
3. **StatusEffect System**: Functional status effect management with stacking and duration
4. **CombatManager**: Turn-based combat flow for video poker mode
5. **Animation Events**: Basic animation event receiver exists
6. **Targeting Interface**: ICombatTargeting interface with implementations
7. **Equipment System**: EquipmentItem with stat modifiers and elemental types

### Gaps & Requirements

1. **Animation-Driven Timing**: Current system uses hardcoded delays, needs animation event-driven timing
2. **Generic Effect System**: EffectData is limited (only Damage/Heal), needs composable, extensible effects
3. **Weapon Configuration**: No weapon type system (TwoHanded, DualWield, etc.) or damage type multipliers
4. **Combat Profile**: No engagement distance, combat range, or line-of-sight system
5. **Collision-Based Hit Detection**: No weapon hitbox system with animation event control
6. **Projectile System**: No projectile spawning or movement system
7. **Persistent AOE**: No persistent area-of-effect zones with pulsing
8. **Cooldown System**: Basic cooldown exists but lacks turn-based, per-combat, per-rest, charges, GCD
9. **Combo System**: Multi-hit exists but not animation-driven with per-hit effects
10. **Simulation System**: No headless simulation capability for Monte Carlo testing
11. **Data/View Separation**: CombatEntityData needed for simulation independence

## Proposed Architecture

### Core Design Principles

1. **Separation of Concerns**: Logic (pure C#) vs Presentation (MonoBehaviour)
2. **Data-Driven**: All combat content via ScriptableObjects
3. **Animation-Owned Timing**: Animators control all timing via events
4. **Extensibility**: Easy to add new effects, targeting, events without code changes
5. **Simulation-First**: All logic works headless for balance testing

### Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│              PRESENTATION LAYER (Unity)                  │
│  CombatEntity, CombatExecutor, WeaponHitbox, Projectile │
│  CombatEventReceiver, PersistentAOE, Visual Effects     │
└─────────────────────────────────────────────────────────┘
                          ↕
┌─────────────────────────────────────────────────────────┐
│              LOGIC LAYER (Pure C#)                      │
│  CombatEntityData, CombatAction, BaseEffect,            │
│  TargetingStrategy, ActionCooldownManager              │
└─────────────────────────────────────────────────────────┘
                          ↕
┌─────────────────────────────────────────────────────────┐
│              SIMULATION LAYER (Pure C#)                  │
│  CombatSimulator, MonteCarloBalancer, BalanceReport     │
└─────────────────────────────────────────────────────────┘
                          ↕
┌─────────────────────────────────────────────────────────┐
│              DATA LAYER (ScriptableObjects)             │
│  CombatAction, Weapon, CombatProfile, BaseEffect        │
│  TargetingStrategy, StatusEffectDefinition              │
└─────────────────────────────────────────────────────────┘
```

## Component Specifications

### 1. Data Layer (ScriptableObjects)

#### CombatAction (Enhanced)
```csharp
// Extends existing AbilityData with:
- Animation trigger string
- TargetingStrategy reference
- BaseEffect[] effects (composable)
- ComboData (hit count, per-hit effects)
- ProjectileData (prefab, speed)
- PersistentAOEData (prefab, duration, pulse count)
- CooldownConfiguration
- EffectBindings (event → VFX/SFX mapping)
```

#### Weapon (New)
```csharp
- Base damage
- Dictionary<DamageType, float> damageTypeMultipliers
- WeaponSlotType (TwoHanded, DualWield, SingleHand, Ranged)
- Visual prefab/model
```

#### CombatProfile (New)
```csharp
- CharacterClass enum
- CombatRange (Melee, Ranged, Magic)
- EngagementDistance (float)
- RequiresLineOfSight (bool)
- AnimatorOverrideController
```

#### BaseEffect (Abstract, Enhanced)
```csharp
// Base class for all effects
- Calculate(attackerData, targetData, weapon): CalculatedEffect
- Apply(targetData, calculatedEffect): void

// Concrete implementations:
- DamageEffect: base damage, damage type, scaling
- StatModifierEffect: stat, modifier type (Add/Multiply/Set), instant/duration
- StatusEffect: status type, duration, parameters
- ConditionalEffect: condition, effect if true/false
- MultiEffect: array of BaseEffects
```

#### TargetingStrategy (Abstract)
```csharp
- ResolveTargets(attackerData): TargetResult
- ShowTargetingUI(attacker): void
- HideTargetingUI(): void

// Implementations:
- SingleTargetSelector: current enemy
- MultiTargetSelector: AOE/multi-hit
- GroundTargetedAOE: player places reticle
- FreeTargetSelector: player-aimed
```

### 2. Logic Layer (Pure C#)

#### CombatEntityData (New)
```csharp
// Plain C# class, no Unity dependencies
- Stats (HP, Attack, Defense, Speed, etc.)
- Current HP
- ActiveStatusEffects list
- EquippedWeapon reference
- CharacterClass
- CombatProfile
- Clone() method for simulation
```

#### ActionCooldownManager (New)
```csharp
- Track cooldowns per action per entity
- CooldownType: None, TurnBased, TimeBased, PerCombat, PerRest
- Charge system (multiple charges, recovery)
- Global cooldown support
- Resource costs (mana, stamina)
- IsActionAvailable(action): bool
- StartCooldown(action): void
- OnTurnStart(): void
- OnCombatEnd(): void
- OnRest(): void
```

#### CalculatedEffect (New)
```csharp
// Result of effect calculation
- EffectType
- Final values (damage amount, stat changes, etc.)
- WasCritical: bool
- Source: CombatEntityData
```

### 3. Presentation Layer (MonoBehaviour)

#### CombatExecutor (New)
```csharp
// Manages action execution
- Current action context (action, attacker, targets, weapon)
- Check movement needs based on combat profile
- Trigger animations
- Manage combo state
- Interface with cooldown manager
```

#### CombatEventReceiver (Enhanced)
```csharp
// Receives animation events, parses string format
- OnCombatEvent(string eventData): void
  // Parses "EventType:Param1:Param2"
  // Dispatches to handlers:
  // - EnableHitbox, DisableHitbox
  // - SpawnVFX, PlaySFX
  // - FireProjectile
  // - SpawnPersistentAOE
  // - MoveTo, ReturnToOrigin
```

#### WeaponHitbox (New)
```csharp
// Collider on weapon
- Activated/deactivated by animation events
- OnTriggerEnter: detects hits
- Tracks hits per swing (prevents double-hits)
- Applies current action's effects
- References CombatExecutor for action context
```

#### Projectile (New)
```csharp
// Spawned by ranged attacks
- Moves toward target at defined speed
- Uses predicted collision (no physics)
- Applies effects on arrival
- Despawns after impact or timeout
```

#### PersistentAOE (New)
```csharp
// Spawned by actions
- Radius, effects per pulse, pulse duration, pulse count
- Each pulse: OverlapSphere to find entities
- Apply effects only to entities currently in zone
- Self-destructs after all pulses
```

### 4. Simulation Layer (Pure C#)

#### CombatSimulator (New)
```csharp
// Runs combat headless
- SimulateCombat(attackerData, defenderData, actions): SimulationResult
- Turn-by-turn logic
- Respects cooldowns, processes status effects
- No Unity dependencies
- Deterministic RNG (seedable)
```

#### MonteCarloBalancer (New)
```csharp
// Runs thousands of simulations
- RunSimulation(playerData, enemyData, iterations): BalanceReport
- CompareBuilds(playerBuilds, enemyData, iterations): ComparisonReport
- Tracks: win rate, TTK, DPS, action usage
```

#### BalanceReport (New)
```csharp
- Total simulations, wins/losses
- Average TTK, damage dealt/taken
- Player survival rate
- Action usage counts
- PrintReport(): void
- ExportToCSV(): void
```

## Implementation Plan

### Phase 1: Core Data & Logic Foundation
1. Create `CombatEntityData` (pure C#)
2. Enhance `CombatAction` (extend AbilityData)
3. Create `BaseEffect` hierarchy
4. Create `Weapon` and `CombatProfile` ScriptableObjects
5. Create `ActionCooldownManager`

### Phase 2: Targeting & Effects
1. Enhance `TargetingStrategy` system
2. Implement concrete effect types (Damage, StatModifier, Status, Conditional, Multi)
3. Create `CalculatedEffect` system
4. Integrate with existing status effect system

### Phase 3: Animation & Presentation
1. Enhance `CombatEventReceiver` with string parsing
2. Create `CombatExecutor`
3. Create `WeaponHitbox` system
4. Create `Projectile` system
5. Create `PersistentAOE` system

### Phase 4: Integration
1. Integrate with existing `CombatEntity`
2. Update `CombatAbilityExecutor` to use new system
3. Create combo system integration
4. Add weapon configuration support

### Phase 5: Simulation
1. Create `CombatSimulator`
2. Create `MonteCarloBalancer`
3. Create editor tools for running simulations
4. Create balance report visualization

## Migration Strategy

### Backward Compatibility
- Keep existing `CombatEntity`, `CombatStats`, `AbilityData` working
- Gradually migrate to new system
- Create adapter layer if needed

### Incremental Rollout
1. Start with new features using new system
2. Migrate existing abilities one by one
3. Keep old system until migration complete

## Extension Points

### Adding New Effect Types
1. Create new class extending `BaseEffect`
2. Implement `Calculate()` and `Apply()` methods
3. Create ScriptableObject asset
4. Add to `CombatAction.effects` array

### Adding New Targeting Strategies
1. Create new class extending `TargetingStrategy`
2. Implement `ResolveTargets()` and UI methods
3. Create ScriptableObject asset
4. Assign to `CombatAction.targetingStrategy`

### Adding New Animation Events
1. Add handler in `CombatEventReceiver`
2. Parse event string in `OnCombatEvent()`
3. Dispatch to appropriate system
4. No code changes needed for animators

### Adding New Status Effects
1. Extend existing `StatusEffectDefinition`
2. Add processing logic in `CombatEntityData.OnTurnStart()`
3. Create ScriptableObject asset

## Example Workflows

### Designer Creating Fire Sword Attack
1. Create `Weapon` asset: Fire Sword (Physical 1.0x, Fire 1.2x)
2. Create `DamageEffect` assets: Physical (50), Fire (20)
3. Create `StatusEffect` asset: Burn (3 turns, 5 dmg/turn)
4. Create `MultiEffect` combining all three
5. Create `CombatAction`: FireSlash
   - Assign MultiEffect
   - Assign SingleTargetSelector
   - Set animation trigger
   - Set cooldown (3 turns)
6. Animator: Add events at frames 20, 25, 30

### Designer Creating Meteor Shower
1. Create `GroundTargetedAOE` targeting strategy
2. Create `DamageEffect`: Fire (30)
3. Create `PersistentAOE` prefab with component
4. Create `CombatAction`: MeteorShower
   - Assign GroundTargetedAOE
   - Assign DamageEffect
   - Set PersistentAOE prefab
   - Set pulse count (5), pulse duration (1s)
5. Animator: Add event at frame 30: "SpawnPersistentAOE"

## Technical Considerations

### Performance
- Object pooling for projectiles and VFX
- Efficient AOE overlap checks (layer masks, spatial partitioning if needed)
- Cooldown manager uses dictionaries for O(1) lookups

### Determinism
- All RNG uses seeded System.Random
- Fixed-point math for simulation if needed
- No Time.deltaTime in simulation

### Testing
- Unit tests for all pure C# logic
- Integration tests for combat flow
- Simulation validation tests

## File Structure

```
Assets/RogueDeal/Scripts/Combat/
├── Core/
│   ├── Data/
│   │   ├── CombatEntityData.cs
│   │   ├── CalculatedEffect.cs
│   │   └── CombatAction.cs (enhanced)
│   ├── Effects/
│   │   ├── BaseEffect.cs
│   │   ├── DamageEffect.cs
│   │   ├── StatModifierEffect.cs
│   │   ├── StatusEffect.cs
│   │   ├── ConditionalEffect.cs
│   │   └── MultiEffect.cs
│   ├── Targeting/
│   │   ├── TargetingStrategy.cs
│   │   ├── SingleTargetSelector.cs
│   │   ├── MultiTargetSelector.cs
│   │   ├── GroundTargetedAOE.cs
│   │   └── FreeTargetSelector.cs
│   ├── Weapons/
│   │   ├── Weapon.cs
│   │   └── CombatProfile.cs
│   └── Cooldowns/
│       ├── ActionCooldownManager.cs
│       └── CooldownConfiguration.cs
├── Presentation/
│   ├── CombatExecutor.cs
│   ├── CombatEventReceiver.cs (enhanced)
│   ├── WeaponHitbox.cs
│   ├── Projectile.cs
│   └── PersistentAOE.cs
└── Simulation/
    ├── CombatSimulator.cs
    ├── MonteCarloBalancer.cs
    └── BalanceReport.cs
```

## Next Steps

1. Review and approve this proposal
2. Create detailed technical specifications for each component
3. Begin Phase 1 implementation
4. Set up testing framework
5. Create example ScriptableObject configurations

## Questions & Considerations

1. Should we maintain full backward compatibility with existing AbilityData, or can we migrate?
2. How should we handle the transition period where both systems coexist?
3. What's the priority order for game modes (video poker first, then others)?
4. Do we need real-time combat immediately, or can we start with turn-based?
5. What level of editor tooling is needed for designers?


