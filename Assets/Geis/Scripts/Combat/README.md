# Geis Combat System

Data-driven combo system with weapon equipping (keys 1-4).

## Setup

1. **Animator**: Run `Tools > Geis > Add Data-Driven Attack (ComboState blend tree)` on AC_Polygon_Masculine_Geis.

2. **If combos show the same animation**: Run `Tools > Geis > Fix Combo blend parameter for normalized 0-1 range`. The blend tree uses thresholds 0-1 over 32 slots; ComboStateBlend (Float) must be set to state/31.

3. **Default ComboData**: Run `Tools > Geis > Create Default ComboData (Unarmed L-L-L)` to create a sample asset at `Assets/Geis/Resources/ComboData_Unarmed.asset`.

4. **Weapon Switcher**: Run `Tools > Geis > Add GeisWeaponSwitcher to PF_PolygonPlayer` to add the component to the player prefab.

5. **Assign in Inspector**: On the player with GeisPlayerAnimationController, assign:
   - `Combo Data`: The ComboData_Unarmed asset (or your custom one).

## RogueDeal Combat (Damage, Health Bars, Hit Detection)

To enable damage on Geis attacks (without replacing GeisPlayerAnimationController):

### Unified mode (recommended)

Single source of truth per weapon—no duplicate arrays.

1. **Create GeisWeaponDefinition** per weapon: Right‑click → Create → Geis > Combat > Weapon Definition.
   - Assign: weaponPrefab, comboData (GeisComboData), weaponStats (Weapon SO), combatAction (CombatAction).
2. **GeisWeaponSwitcher**: Enable `Use Unified Weapons`, assign `Unified Slots` with your GeisWeaponDefinition assets.
3. **Add bridge**: Select player → `Tools > Geis > Add Combat Bridge to Selected Player`.
4. **Create UI** (if needed): `Tools > Combat > Create Combat UI Prefabs`.

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

## Combat Music (Dynamic Combat Notes)

Attacks produce pentatonic notes; combos build layered melodies. Uses `Musical_Instruments_And_Notes` assets.

1. **Create config**: Run `Geis > Combat > Music > Create Default Marimba Config` to create `WeaponInstrumentConfig_Marimba.asset`.
2. **Add components**: Add `CombatMusicController` and `UnityCombatMusicAudio` to a scene object (e.g. player or dedicated "CombatMusic" GameObject).
3. **Assign**: On CombatMusicController, set `Audio Provider` to the UnityCombatMusicAudio component, and `Default Instrument Config` to the Marimba config.
4. **Optional**: Create weapon-specific configs (e.g. Guitar for Knife) and assign to `Weapon Instrument Overrides` for per-weapon instruments.

## Ambient Music

Plays background music and ducks when combat music is active.

1. Create a GameObject (e.g. `AmbientMusic`) and add `AmbientMusicManager`.
2. Assign an `Ambient Clip` or a `World Definition` (uses `backgroundMusic`).
3. `Duck During Combat` is on by default—ambient fades to 25% when combat music plays.
4. For persistence across scenes, place on a `DontDestroyOnLoad` object.
