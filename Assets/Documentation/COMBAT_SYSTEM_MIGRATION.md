# Combat System Migration Guide

## Overview

This document tracks the migration from the old combat system (AbilityData/CombatAbilityExecutor) to the new system (CombatAction/CombatExecutor).

## Migration Status

### ✅ Completed

1. **Adapters Created**
   - `AbilityDataAdapter` - Converts AbilityData to CombatAction at runtime
   - `EffectDataAdapter` - Converts EffectData to BaseEffect (DamageEffect/HealEffect)
   - `HealEffect` - Created new effect type for healing

2. **Controllers Updated**
   - `RealTimeCombatController` - Now uses CombatExecutor
   - `TurnBasedCombatPresenter` - Now uses CombatExecutor

3. **Deprecated**
   - `CombatAbilityExecutor` - Marked as obsolete, will be removed

### 🚧 In Progress

1. **Targeting System Migration**
   - `ICombatTargeting` → `TargetingStrategy`
   - `RealTimeTargeting` → TargetingStrategy implementation
   - `TurnBasedTargeting` → TargetingStrategy implementation

2. **CombatEntity Data Sync**
   - Make `CombatEntityData` primary source of truth
   - Remove or wrap `CombatStats`

### 📋 Pending

1. **Video Poker Integration**
   - Update `CombatManager` to use new system (if needed)
   - Update `PlayerAttackingState` to use CombatExecutor

2. **Remaining References**
   - Find and update all `AbilityData` references
   - Find and update all `CombatAbilityExecutor` references

## Breaking Changes

### Functionality Loss

1. **Hardcoded Delays Removed**
   - Old: `windupDelay`, `contactDelay`, `reactionDelay` in CombatAbilityExecutor
   - New: Animation-driven timing via animation events
   - **Impact**: If animations don't have events, effects apply immediately or after animation duration
   - **Mitigation**: Add animation events to all combat animations

2. **Singleton Pattern Removed**
   - Old: `CombatAbilityExecutor.Instance` (singleton)
   - New: `CombatExecutor` per entity (component-based)
   - **Impact**: No global executor instance
   - **Mitigation**: Get executor from entity: `entity.GetComponent<CombatExecutor>()`

3. **Targeting Interface Changed**
   - Old: `ICombatTargeting.SelectTargets(CombatEntity, AbilityData)`
   - New: `TargetingStrategy.ResolveTargets(CombatEntityData)`
   - **Impact**: Targeting logic needs to be migrated
   - **Mitigation**: Adapter pattern or direct migration

4. **Effect System Changed**
   - Old: `EffectData` with `CalculateFinalValue()` method
   - New: `BaseEffect` with `Calculate()` and `Apply()` methods
   - **Impact**: Effect calculation happens in two phases (calculate then apply)
   - **Mitigation**: Adapter converts old effects automatically

### Migration Steps

#### For Controllers Using AbilityData

**Before:**
```csharp
CombatAbilityExecutor.Instance.ExecuteAbility(caster, abilityData, targets);
```

**After:**
```csharp
CombatAction combatAction = AbilityDataAdapter.ConvertToCombatAction(abilityData);
CombatExecutor executor = caster.GetComponent<CombatExecutor>();
executor.ExecuteAction(combatAction);
```

#### For Controllers Using EffectData

**Before:**
```csharp
float damage = effectData.CalculateFinalValue(attackerStats, defenderStats);
defenderStats.TakeDamage(damage);
```

**After:**
```csharp
BaseEffect effect = EffectDataAdapter.ConvertToBaseEffect(effectData);
var calculated = effect.Calculate(attackerData, defenderData, weapon);
effect.Apply(defenderData, calculated);
```

#### For Targeting

**Before:**
```csharp
ICombatTargeting targeting = new RealTimeTargeting(layer, range);
var targets = targeting.SelectTargets(caster, abilityData);
```

**After:**
```csharp
// Option 1: Use adapter (temporary)
CombatAction action = AbilityDataAdapter.ConvertToCombatAction(abilityData);
var result = action.targetingStrategy.ResolveTargets(casterData);

// Option 2: Direct migration (preferred)
SingleTargetSelector selector = ScriptableObject.CreateInstance<SingleTargetSelector>();
selector.maxRange = range;
var result = selector.ResolveTargets(casterData);
```

## Files Changed

### New Files
- `Assets/RogueDeal/Scripts/Combat/Core/Adapters/AbilityDataAdapter.cs`
- `Assets/RogueDeal/Scripts/Combat/Core/Adapters/EffectDataAdapter.cs`
- `Assets/RogueDeal/Scripts/Combat/Core/Effects/HealEffect.cs`
- `COMBAT_SYSTEM_MIGRATION.md` (this file)

### Modified Files
- `Assets/RogueDeal/Scripts/Combat/Presentation/RealTimeCombatController.cs`
- `Assets/RogueDeal/Scripts/Combat/Presentation/TurnBasedCombatPresenter.cs`
- `Assets/RogueDeal/Scripts/Combat/CombatAbilityExecutor.cs` (marked obsolete)

### Files to Update
- `Assets/RogueDeal/Scripts/Combat/States/PlayerAttackingState.cs`
- `Assets/RogueDeal/Scripts/Combat/States/ExecutingAbilityState.cs`
- `Assets/RogueDeal/Scripts/Combat/Training/TrainingAttackController.cs`
- Any other files using `AbilityData` or `CombatAbilityExecutor`

## Testing Checklist

- [ ] Real-time combat works with new system
- [ ] Turn-based combat works with new system
- [ ] Video poker combat works (if using new system)
- [ ] Effects apply correctly (damage, heal)
- [ ] Animations play correctly
- [ ] Targeting works correctly
- [ ] Cooldowns work correctly
- [ ] Status effects work correctly

## Rollback Plan

If issues arise:

1. Revert controller changes
2. Keep adapters (they're backward compatible)
3. Re-enable `CombatAbilityExecutor` (remove obsolete attribute)
4. Fix issues and retry migration

## Next Steps

1. Complete targeting system migration
2. Update CombatEntity to use CombatEntityData as primary
3. Remove CombatAbilityExecutor after all references updated
4. Update video poker system if needed
5. Create unit tests for new system
6. Update documentation
