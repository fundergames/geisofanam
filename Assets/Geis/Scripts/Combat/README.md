# Geis Combat System

Data-driven combo system with weapon equipping (keys 1-4).

## Setup

1. **Animator**: Run `Tools > Geis > Add Data-Driven Attack (ComboState blend tree)` on AC_Polygon_Masculine_Geis.

2. **If combos show the same animation**: Run `Tools > Geis > Fix Combo blend parameter for normalized 0-1 range`. The blend tree uses thresholds 0-1 over 32 slots; ComboStateBlend (Float) must be set to state/31.

3. **Default ComboData**: Run `Tools > Geis > Create Default ComboData (Unarmed L-L-L)` to create a sample asset at `Assets/Geis/Resources/ComboData_Unarmed.asset`.

4. **Weapon Switcher**: Run `Tools > Geis > Add GeisWeaponSwitcher to PF_PolygonPlayer` to add the component to the player prefab.

5. **Assign combos on weapons**: Set `comboData` on each `GeisWeaponDefinition` in `GeisWeaponSwitcher.weaponSlots` (not on the animation controller).

6. **Attack SFX/VFX**: Open **Funder Games → Geis → Tools → Combat → Combo Graph** on each `GeisComboData`. Under **Combat Binding**, use the **orange** track for damage hit times and the **cyan** track for presentation (whoosh, trails). Run **Geis → Combat → Add Presentation To Player Prefab** once.

## RogueDeal Combat (Damage, Health Bars, Hit Detection)

To enable damage on Geis attacks (without replacing GeisPlayerAnimationController):

### Unified mode (recommended)

Single source of truth per weapon—no duplicate arrays.

1. **Create GeisWeaponDefinition** per weapon: Right‑click → Create → Geis > Combat > Weapon Definition.
   - Assign: weaponPrefab, comboData (GeisComboData), weaponStats (Weapon SO), combatAction (CombatAction).
2. **GeisWeaponSwitcher**: Enable `Use Unified Weapons`, assign `Unified Slots` with your GeisWeaponDefinition assets.
3. **Bridge/UI integration**: add and configure combat bridge/UI components manually in the player and UI prefabs.

### Legacy mode

Keep existing GeisWeaponSlot[] for visuals. On GeisCombatBridge, assign `Combat Actions By Weapon` and `Weapons By Slot` arrays.

Uses SimpleAttackHitDetector (OverlapSphere after delay)—no animation events or weapon colliders required.

## Keys

- **1**: Unarmed
- **2**: Knife
- **3**: Sword
- **4**: Bow

## Adding Combo Branches

Edit the GeisComboData ScriptableObject: add transitions (fromState, inputType, toState) and assign clips. Then run `Tools > Geis > Sync GeisComboData clips to Attack blend tree` to copy clips into the animator. No new animator states needed.

**Troubleshooting: Same attack every hit?** The blend tree uses thresholds 0, 1/31, 2/31, ..., 1. Run `Tools > Geis > Fix Combo blend parameter for normalized 0-1 range` to add ComboStateBlend (Float) so the correct clip is selected.

## Ambient Music

Plays background music for a scene or world.

1. Create a GameObject (e.g. `AmbientMusic`) and add `AmbientMusicManager`.
2. Assign an `Ambient Clip` or a `World Definition` (uses `backgroundMusic`).
3. For persistence across scenes, place on a `DontDestroyOnLoad` object.
