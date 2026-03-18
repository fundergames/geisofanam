# Targeting System Setup Guide

## Overview

The targeting system allows you to switch between different targeting modes for combat:
- **Nearest Enemy**: Targets the nearest enemy within attack range
- **Cone Targeting**: Targets the nearest enemy within a cone of view and attack range
- **Click-to-Select**: Click to lock onto enemies (lock persists until target dies, goes out of range, or you click elsewhere)
- **Directional**: Attacks in the direction you're facing (uses weapon colliders)
- **Ground Targeting**: For AOE spells - click on ground to place effect

## Setup Steps

### 1. Add TargetingManager to Player

The `ThirdPersonCombatController` will automatically create a `TargetingManager` component if one doesn't exist. However, you should configure it in the Inspector:

1. Select your Player GameObject (the one with `ThirdPersonCombatController`)
2. Find the `TargetingManager` component in the Inspector
3. Assign the targeting strategy ScriptableObjects:
   - **Nearest Enemy Strategy**: Create/assign a `NearestEnemyTargetingStrategy` asset
   - **Cone Targeting Strategy**: Create/assign a `ConeTargetingStrategy` asset
   - **Click To Select Strategy**: Create/assign a `ClickToSelectTargetingStrategy` asset
   - **Directional Strategy**: Create/assign a `DirectionalTargetingStrategy` asset
   - **Ground Targeting Strategy**: Create/assign a `GroundTargetedAOE` asset (already exists)

### 2. Create Targeting Strategy Assets

In Unity, create ScriptableObject assets for each targeting strategy:

1. Right-click in Project window → Create → RogueDeal → Combat → Targeting
2. Create one of each:
   - **Nearest Enemy** (`Targeting_NearestEnemy`)
   - **Cone Targeting** (`Targeting_Cone`)
   - **Click To Select** (`Targeting_ClickToSelect`)
   - **Directional** (`Targeting_Directional`)

3. Configure each strategy:
   - **Nearest Enemy**: Set `Target Layers` (e.g., Enemy layer) and `Default Range`
   - **Cone Targeting**: Set `Target Layers`, `Default Range`, and `Default Cone Angle`
   - **Click To Select**: Set `Target Layers` and `Default Range`
   - **Directional**: No configuration needed

### 3. Configure Weapon Range

1. Select your Weapon ScriptableObject assets
2. Set the `Max Range` field (in Unity units)
   - Example: Sword = 2 units, Spear = 3 units, Bow = 20 units

### 4. Configure Character Cone Angle

1. Select your Player GameObject (or enemy GameObjects)
2. Find the `CombatEntity` component
3. Set the `Cone Angle` field (in degrees, 0-180)
   - Example: 60° for normal cone, 90° for wide cone

### 5. Set Up Lock-On Indicator (Optional)

The `LockOnIndicator` is automatically created by `ThirdPersonCombatController`. To customize it:

1. Find the `LockOnIndicator` GameObject (child of Player)
2. Configure visual settings:
   - `Indicator Radius`: Size of the circle
   - `Indicator Color`: Color of the indicator
   - `Crosshair Length`: Length of crosshair lines
   - `Ground Offset`: Height above ground

## Usage

### Switching Targeting Modes (Debug)

While testing, you can switch targeting modes using number keys:
- **1**: Nearest Enemy
- **2**: Cone Targeting
- **3**: Click-to-Select
- **4**: Directional
- **5**: Ground Targeting

To disable debug keybinds, uncheck `Enable Debug Keybinds` on the `TargetingManager`.

### Click-to-Select Targeting

1. Switch to Click-to-Select mode (key 3, or set in Inspector)
2. Click on an enemy to lock onto it
3. The lock-on indicator will appear under the target
4. Click the same target again to deselect
5. Click on a different target to switch targets
6. Click on ground/non-targetable to clear lock-on

### Directional Targeting

1. Switch to Directional mode (key 4, or set in Inspector)
2. Attack in the direction you're facing
3. Weapon colliders will detect hits automatically
4. No target selection needed

### Ground Targeting (AOE)

1. Switch to Ground Targeting mode (key 5, or set in Inspector)
2. Click on the ground where you want the AOE to spawn
3. The AOE preview indicator will show the affected area
4. Attack to cast the spell at that location

## Range Resolution

The system uses the following priority for determining attack range:
1. **Weapon.maxRange** (if weapon is equipped and has range > 0)
2. **CombatProfile.engagementDistance** (if profile exists and has distance > 0)
3. **Default Range** (from targeting strategy, typically 2 units)

## Integration with Damage/SFX/VFX

The targeting system resolves targets, but damage/SFX/VFX are applied through:

1. **Weapon Colliders** (for directional/melee attacks):
   - `WeaponHitbox` detects collisions
   - Applies damage from `CombatAction.effects`
   - Triggers SFX/VFX via animation events

2. **CombatExecutor** (for targeted attacks):
   - Uses resolved targets from `TargetingManager`
   - Applies effects from `CombatAction.effects`
   - Triggers SFX/VFX via `CombatEvents`

## Next Steps

To hook up damage, sound, and VFX:

1. **Damage**: Already handled by `WeaponHitbox` (weapon colliders) or `CombatExecutor` (targeted)
2. **Sound**: Add `CombatSFXController` to player/enemies and configure SFX in `CombatAction.effectBindings`
3. **VFX**: Add `CombatVFXController` to player/enemies and configure VFX in `CombatAction.effectBindings`

See the existing `CombatAction.effectBindings` system for mapping animation events to SFX/VFX.
