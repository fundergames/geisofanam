# Ability, Effect, Hero & Equipment Setup Plan

## Current System Analysis

### How It Currently Works

#### 1. **Hero/Class System**
- **ClassDefinition** - Defines a character class
  - Base stats and stat growth per level
  - **ClassAbility[]** - Passive abilities (stat modifiers, hand bonuses, triggers)
  - **ClassAttackMapping[]** - Maps poker hands → attack properties (damage, hits, VFX)
  - XP curve for progression

- **PlayerCharacter** - Runtime character instance
  - References `ClassDefinition`
  - Has `equipment` dictionary (EquipmentSlot → EquipmentItem)
  - `RecalculateStats()` combines:
    1. Base stats from class (level-scaled)
    2. Passive stat modifiers from `ClassAbility` (unlocked by level)
    3. Stat bonuses from `EquipmentItem`

#### 2. **Equipment System**
- **EquipmentItem** - Equipment ScriptableObject
  - Stat bonuses (health, attack, defense, etc.)
  - Elemental type
  - `onHitEffect` (StatusEffectDefinition)
  - Visual model
  - **Does NOT directly reference abilities/actions**

#### 3. **Ability System (Old)**
- **ClassAbility** - Passive abilities only
  - Stat modifiers
  - Hand damage multipliers
  - Trigger-based effects (onKill, onCombatStart)
  - **No active combat actions**

- **AbilityData** (deprecated) - Was used for active abilities
  - Effects array
  - Targeting
  - Animation
  - **Now replaced by CombatAction**

#### 4. **Attack Mapping (Video Poker)**
- **ClassAttackMapping** - Maps poker hands to attacks
  - Hand type → attack properties
  - Damage multipliers
  - Number of hits
  - VFX/SFX
  - **Separate from CombatAction system**

- **AbilityLookup** - Maps poker hands to CombatAction
  - Used by `PlayerAttackingState`
  - **New system integration point**

#### 5. **New Combat System**
- **CombatAction** - Active combat actions
  - Effects array (BaseEffect[])
  - Targeting strategy
  - Animation/Timeline
  - Cooldown configuration
  - **Not directly tied to classes or equipment yet**

- **Weapon** - Weapon configuration
  - Base damage
  - Damage type multipliers
  - Range
  - **Referenced by CombatEntityData, but not by EquipmentItem**

## Current Gaps & Issues

### 1. **Disconnected Systems**
- ❌ `EquipmentItem` doesn't reference `Weapon`
- ❌ `ClassDefinition` doesn't have `CombatAction[]` for active abilities
- ❌ `ClassAbility` is passive-only, no active actions
- ❌ `Weapon` exists but isn't connected to equipment
- ❌ `CombatAction` isn't assigned to classes/heroes

### 2. **Multiple Ability Concepts**
- `ClassAbility` - Passive stat modifiers
- `AbilityData` (deprecated) - Was for active abilities
- `CombatAction` - New active ability system
- `ClassAttackMapping` - Video poker hand → attack mapping

### 3. **Equipment → Weapon Gap**
- `EquipmentItem` has elemental type and onHitEffect
- `Weapon` has damage type multipliers and base damage
- **No connection between them**

## Proposed Solution

### Option A: Unified System (Recommended)

#### 1. **Extend ClassDefinition**
```csharp
public class ClassDefinition : ScriptableObject
{
    // Existing...
    public List<ClassAbility> abilities; // Keep for passive abilities
    
    // NEW: Active combat actions
    [Header("Active Combat Actions")]
    [Tooltip("Combat actions available to this class (unlocked by level)")]
    public List<ClassCombatAction> combatActions = new List<ClassCombatAction>();
}

[System.Serializable]
public class ClassCombatAction
{
    public CombatAction action;
    public int requiredLevel = 1;
    public bool isDefault = false; // Available from start
}
```

#### 2. **Connect EquipmentItem to Weapon**
```csharp
public class EquipmentItem : BaseItem
{
    // Existing stat modifiers...
    
    // NEW: Weapon configuration
    [Header("Weapon Configuration")]
    [Tooltip("If this is a weapon, reference the Weapon configuration")]
    public Weapon weaponConfig;
    
    // When equipped, apply weapon to CombatEntityData
}
```

#### 3. **Update PlayerCharacter**
```csharp
public class PlayerCharacter
{
    // NEW: Get available combat actions
    public List<CombatAction> GetAvailableCombatActions()
    {
        var actions = new List<CombatAction>();
        foreach (var classAction in classDefinition.combatActions)
        {
            if (level >= classAction.requiredLevel)
            {
                actions.Add(classAction.action);
            }
        }
        return actions;
    }
    
    // NEW: Get equipped weapon
    public Weapon GetEquippedWeapon()
    {
        if (equipment.ContainsKey(EquipmentSlot.Weapon))
        {
            return equipment[EquipmentSlot.Weapon]?.weaponConfig;
        }
        return null;
    }
}
```

#### 4. **Update CombatEntityData Sync**
```csharp
// In CombatEntity or initialization
public void SyncFromPlayerCharacter(PlayerCharacter player)
{
    entityData.maxHealth = player.effectiveStats.maxHealth;
    entityData.currentHealth = player.effectiveStats.currentHealth;
    entityData.attack = player.effectiveStats.attack;
    entityData.defense = player.effectiveStats.defense;
    entityData.magicPower = player.effectiveStats.magic;
    entityData.speed = player.effectiveStats.speed;
    
    // NEW: Sync weapon
    entityData.equippedWeapon = player.GetEquippedWeapon();
    
    // NEW: Sync character class
    entityData.characterClass = player.classDefinition.classType;
}
```

### Option B: Minimal Integration (Simpler)

Keep systems mostly separate, just add connections:

1. **EquipmentItem → Weapon** - Add optional `weaponConfig` reference
2. **ClassDefinition → CombatAction** - Add `CombatAction[]` array (no level requirements)
3. **PlayerCharacter** - Add methods to get actions and weapon

## Recommended Approach: Option A

### Benefits
- ✅ Clear progression (actions unlock by level)
- ✅ Equipment affects combat (weapon config)
- ✅ Unified system (one way to define abilities)
- ✅ Flexible (can have class-specific and equipment-based actions)

### Implementation Steps

1. **Extend ClassDefinition**
   - Add `ClassCombatAction[]` array
   - Add method `GetAvailableCombatActions(int level)`

2. **Extend EquipmentItem**
   - Add optional `Weapon weaponConfig` field
   - When weapon slot, link to Weapon asset

3. **Update PlayerCharacter**
   - Add `GetAvailableCombatActions()` method
   - Add `GetEquippedWeapon()` method
   - Sync weapon to `CombatEntityData` when equipping

4. **Update CombatEntity**
   - Sync `CombatEntityData` from `PlayerCharacter`
   - Include weapon and available actions

5. **Update Controllers**
   - `RealTimeCombatController` - Get actions from `PlayerCharacter`
   - `ThirdPersonCombatController` - Get actions from `PlayerCharacter`

## Data Flow

### Current Flow (Video Poker)
```
Poker Hand → ClassAttackMapping → Damage Calculation
         → AbilityLookup → CombatAction (if exists)
```

### Proposed Flow (Unified)
```
PlayerCharacter
  ├── ClassDefinition
  │     ├── ClassAbility[] (passive stat modifiers)
  │     └── ClassCombatAction[] (active abilities, level-gated)
  │
  ├── Equipment[]
  │     └── EquipmentItem.weaponConfig → Weapon
  │
  └── Syncs to CombatEntityData
        ├── Stats (from class + abilities + equipment)
        ├── equippedWeapon (from equipment)
        └── Available actions (from class + level)
```

## Setup Workflow (For Designers)

### Creating a Class with Abilities

1. **Create ClassDefinition asset**
   - Set base stats
   - Add `ClassAbility` entries (passive abilities)
   - Add `ClassCombatAction` entries (active abilities with level requirements)

2. **Create CombatAction assets**
   - Define effects
   - Set targeting strategy
   - Configure animations
   - Set cooldowns

3. **Assign to ClassDefinition**
   - Add to `combatActions` array
   - Set `requiredLevel` for each

### Creating Equipment with Weapon

1. **Create Weapon asset** (if weapon equipment)
   - Set base damage
   - Configure damage type multipliers
   - Set range

2. **Create EquipmentItem asset**
   - Set stat bonuses
   - **Reference Weapon asset** in `weaponConfig`
   - Set elemental type
   - Set onHitEffect (if any)

### Creating Effects

1. **Create BaseEffect assets**
   - DamageEffect, HealEffect, StatusEffect, etc.
   - Configure stat scaling, damage types, etc.

2. **Assign to CombatAction**
   - Add to `effects` array
   - Effects are composable (can combine multiple)

## Migration Path

### For Existing Content

1. **ClassAbilities** - Keep as-is (passive abilities)
2. **ClassAttackMapping** - Keep for video poker mode
3. **AbilityLookup** - Migrate to use `CombatAction` (already done)
4. **EquipmentItem** - Add `weaponConfig` field (optional, backward compatible)

### For New Content

- Use `CombatAction` for all active abilities
- Use `Weapon` for weapon configuration
- Link via `ClassDefinition.combatActions` and `EquipmentItem.weaponConfig`

## Questions to Resolve

1. **Should ClassAbility be merged into CombatAction?**
   - **Recommendation:** Keep separate - ClassAbility is passive-only, simpler
   - Or create `PassiveEffect` type that can be used in both

2. **Should equipment grant new actions?**
   - **Recommendation:** Yes - add `CombatAction[]` to `EquipmentItem` for equipment-specific abilities

3. **How to handle weapon swapping?**
   - **Recommendation:** When weapon equipment changes, update `CombatEntityData.equippedWeapon`
   - Effects automatically use new weapon multipliers

4. **Should ClassAttackMapping be replaced?**
   - **Recommendation:** Keep for video poker, but also support `CombatAction` via `AbilityLookup`
   - Both can coexist

## Next Steps

1. **Extend ClassDefinition** - Add `ClassCombatAction[]`
2. **Extend EquipmentItem** - Add `weaponConfig` and optional `CombatAction[]`
3. **Update PlayerCharacter** - Add methods to get actions and weapon
4. **Update sync logic** - Sync weapon and actions to `CombatEntityData`
5. **Update controllers** - Use actions from `PlayerCharacter`
6. **Create example assets** - Show how to set up a class with abilities
