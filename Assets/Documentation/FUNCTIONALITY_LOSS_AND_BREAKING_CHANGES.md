# Functionality Loss and Breaking Changes

## Summary

This document details all functionality loss and breaking changes from migrating to the new combat system.

## Breaking Changes

### 1. CombatAbilityExecutor Removed

**Old System:**
- Singleton pattern: `CombatAbilityExecutor.Instance`
- Hardcoded timing delays: `windupDelay`, `contactDelay`, `reactionDelay`
- Direct execution: `ExecuteAbility(caster, ability, targets, onComplete)`

**New System:**
- Component-based: `CombatExecutor` per entity
- Animation-driven timing: Effects triggered by animation events
- Action-based: `ExecuteAction(CombatAction)`

**Impact:**
- ❌ No global executor instance
- ❌ Hardcoded delays no longer work (must use animation events)
- ❌ Callback pattern changed (no `onComplete` parameter)

**Migration:**
```csharp
// OLD
CombatAbilityExecutor.Instance.ExecuteAbility(caster, ability, targets, () => {
    Debug.Log("Complete!");
});

// NEW
CombatExecutor executor = caster.GetComponent<CombatExecutor>();
executor.ExecuteAction(combatAction);
// Wait for completion via coroutine or event
```

### 2. AbilityData → CombatAction

**Old System:**
- `AbilityData` ScriptableObject
- Simple properties: `cooldown`, `range`, `targetType`
- `EffectData[]` array

**New System:**
- `CombatAction` ScriptableObject
- `TargetingStrategy` for targeting
- `BaseEffect[]` for effects
- `CooldownConfiguration` for cooldowns

**Impact:**
- ⚠️ Direct `AbilityData` usage requires adapter
- ⚠️ `targetType` enum → `TargetingStrategy` ScriptableObject
- ✅ Adapter handles conversion automatically

**Migration:**
```csharp
// OLD
AbilityData ability = ...;
var targets = targeting.SelectTargets(caster, ability);

// NEW
CombatAction action = AbilityDataAdapter.ConvertToCombatAction(ability);
var result = action.targetingStrategy.ResolveTargets(casterData);
```

### 3. EffectData → BaseEffect

**Old System:**
- `EffectData` with `CalculateFinalValue()` method
- Single calculation method
- Direct stat access

**New System:**
- `BaseEffect` with `Calculate()` and `Apply()` methods
- Two-phase: calculate then apply
- Uses `CombatEntityData` instead of `CombatStats`

**Impact:**
- ⚠️ Effect calculation split into two phases
- ⚠️ Must use `CombatEntityData` instead of `CombatStats`
- ✅ Adapter handles conversion automatically

**Migration:**
```csharp
// OLD
float damage = effectData.CalculateFinalValue(attackerStats, defenderStats);
defenderStats.TakeDamage(damage);

// NEW
var calculated = effect.Calculate(attackerData, defenderData, weapon);
effect.Apply(defenderData, calculated);
```

### 4. Targeting System Changed

**Old System:**
- `ICombatTargeting` interface
- `SelectTargets(CombatEntity, AbilityData)` method
- Implementations: `RealTimeTargeting`, `TurnBasedTargeting`

**New System:**
- `TargetingStrategy` base class (ScriptableObject)
- `ResolveTargets(CombatEntityData)` method
- Returns `TargetResult` with targets and position

**Impact:**
- ❌ `ICombatTargeting` interface removed
- ❌ Method signature changed
- ⚠️ Must migrate targeting implementations

**Migration:**
```csharp
// OLD
ICombatTargeting targeting = new RealTimeTargeting(layer, range);
var targets = targeting.SelectTargets(caster, ability);

// NEW
SingleTargetSelector selector = ScriptableObject.CreateInstance<SingleTargetSelector>();
selector.maxRange = range;
var result = selector.ResolveTargets(casterData);
var targets = result.targets;
```

## Functionality Loss

### 1. Hardcoded Timing Delays

**Lost:**
- `windupDelay` - Delay before effects start
- `contactDelay` - Delay before damage applies
- `reactionDelay` - Delay before hit reaction

**Reason:**
- New system is animation-driven
- Timing controlled by animation events
- More accurate and flexible

**Mitigation:**
- Add animation events to all combat animations
- Use Timeline signals for complex timing
- Effects apply when animation events fire

**Workaround:**
- If animations don't have events, effects apply immediately
- Can add delays in `CombatExecutor` if needed (not recommended)

### 2. Singleton Pattern

**Lost:**
- Global `CombatAbilityExecutor.Instance`
- Single executor for all entities

**Reason:**
- Component-based architecture
- Per-entity executors allow better control
- Supports multiple combat modes

**Mitigation:**
- Get executor from entity: `entity.GetComponent<CombatExecutor>()`
- Add executor if missing: `entity.gameObject.AddComponent<CombatExecutor>()`

### 3. Direct Stat Access

**Lost:**
- Direct `CombatStats` access in effects
- `CalculateFinalValue(CombatStats, CombatStats)` method

**Reason:**
- Separation of data and presentation
- `CombatEntityData` is pure C# (simulation-ready)
- `CombatStats` is Unity-dependent

**Mitigation:**
- Use `CombatEntityData` instead
- Adapter converts automatically
- Sync between `CombatStats` and `CombatEntityData` in `CombatEntity`

### 4. Simple Targeting

**Lost:**
- Simple `TargetType` enum
- Direct `SelectTargets()` calls

**Reason:**
- More flexible targeting system
- Supports complex targeting strategies
- Data-driven targeting

**Mitigation:**
- Adapter creates `SingleTargetSelector` from `TargetType`
- Can create custom `TargetingStrategy` for complex cases

## Known Issues

### 1. Animation Events Required

**Issue:** Effects won't apply if animations don't have events.

**Solution:** Add animation events to all combat animations:
- `OnCombatEvent("EnableHitbox")` - Start hit detection
- `OnCombatEvent("DisableHitbox")` - End hit detection
- `OnCombatEvent("ApplyEffects")` - Apply effects

### 2. CombatEntityData Sync

**Issue:** `CombatEntityData` and `CombatStats` can get out of sync.

**Solution:** 
- Make `CombatEntityData` primary
- Sync `CombatStats` from `CombatEntityData` when needed
- Or remove `CombatStats` entirely (future)

### 3. Targeting Migration Incomplete

**Issue:** Old targeting implementations still exist.

**Solution:**
- Migrate `RealTimeTargeting` to `TargetingStrategy`
- Migrate `TurnBasedTargeting` to `TargetingStrategy`
- Remove `ICombatTargeting` interface

## Migration Priority

### High Priority (Breaking)
1. ✅ Update controllers using `CombatAbilityExecutor`
2. ⏳ Migrate targeting implementations
3. ⏳ Update `CombatEntity` data sync

### Medium Priority (Functionality)
1. ⏳ Add animation events to all combat animations
2. ⏳ Update video poker system
3. ⏳ Test all combat modes

### Low Priority (Cleanup)
1. ⏳ Remove `CombatAbilityExecutor` entirely
2. ⏳ Remove `ICombatTargeting` interface
3. ⏳ Remove `CombatStats` (or make wrapper)

## Testing Required

- [ ] Real-time combat
- [ ] Turn-based combat
- [ ] Video poker combat
- [ ] Animation timing
- [ ] Effect application
- [ ] Targeting
- [ ] Cooldowns
- [ ] Status effects
- [ ] Multi-hit combos
- [ ] Projectiles
- [ ] AOE effects

## Rollback Instructions

If critical issues arise:

1. Revert controller changes:
   - `RealTimeCombatController.cs`
   - `TurnBasedCombatPresenter.cs`

2. Re-enable `CombatAbilityExecutor`:
   - Remove `[Obsolete]` attribute
   - Restore original implementation

3. Keep adapters (they're backward compatible)

4. Fix issues and retry migration
