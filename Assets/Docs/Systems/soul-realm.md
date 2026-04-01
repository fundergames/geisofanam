# Soul realm & weapon abilities

**Status**: current  
**Last updated**: 2026-04-01

## Purpose

Soul realm mode (ghost vs. frozen body), world freeze hooks, weapon-bound supernatural abilities, and shared ability origin/camera context for VFX and gameplay.

## Behavior & contracts

### `SoulRealmManager`

- **Singleton**: `Instance`; raises static `SoulRealmStateChanged` when entering/exiting (puzzles, ability controller, presentation subscribe).
- **State**: `IsSoulRealmActive`, `SoulRealmBlend` (0/1), exit hold progress APIs (`SoulRealmExitHoldProgress01`, `IsSoulRealmExitHoldInProgress`, etc.).
- **Movement**: `AllowGhostMovement` — during enter grace, ghost can move; after grace, holding **SoulRealm** input blocks ghost movement (exit path). Body locomotion suppressed via `ShouldSuppressBodyLocomotion` while active.
- **Abilities / VFX origin**: `GetAbilityContextTransforms(out ownerTransform, out originWorld)` — in soul realm uses **ghost** root and chest-height style origin; otherwise body locomotion and look-at. **Weapon ability activation** should use this when the manager exists.
- **Freeze registry**: `SoulRealmFreezeTarget` list for selective world freeze (implementation detail in manager code).
- **Delta time**: Internal cap `SoulRealmMaxDeltaPerFrame` on soul-realm timers to avoid huge `deltaTime` spikes completing exit in one frame.

### `SoulRealmInteractable` (static helper)

- `BlockPhysicalInteractions` → true when `SoulRealmManager.Instance.IsSoulRealmActive`. Used to **block normal physical interactions** (weapon switcher, interact scripts that respect it). Soul-only gameplay should still check `IsSoulRealmActive` where relevant.

### `SoulRealmWeaponAbilityController`

- **Input**: Assign **`GeisControls`** `InputActionAsset` (contains the **`SoulRealmWeapon`** map); at runtime the controller **Instantiates** a copy so Enable/Disable state is not shared globally.
- **Required map**: Action map name **`SoulRealmWeapon`**, actions **`Ability1`** and **`Ability2`**. Errors logged if missing.
- **Enable/disable map** (`SyncActionMapWithRealm`): Map is **on** when current weapon has ability assets **and** either (a) **soul realm** — any primary/secondary asset exists, or (b) **physical realm** — at least one ability has `AllowActivationInPhysicalRealm`. Per-ability realm rules are enforced again in `TryActivateAbility`.
- **Polling**: Uses `WasPressedThisFrame` on actions; **also** treats gamepad **North** as ability 1 and **Right shoulder** as ability 2; keyboard **Q** / **F** as alternates when actions did not fire.
- **`TryActivateAbility`**: Resolves current `GeisWeaponDefinition` from `GeisWeaponSwitcher`; reads `PrimarySoulAbility` / `SecondarySoulAbility`; checks `AllowActivationInSoulRealm` / `AllowActivationInPhysicalRealm` vs current realm; builds `SoulWeaponAbilityContext` with `GetAbilityContextTransforms` from manager when present, else `abilityOrigin`; forward from `GeisCameraController.GetCameraForwardZeroedYNormalised()` when available.
- **Feedback**: `SoulRealmAbilityFeedback` auto-added if missing; shows blocked reasons (no weapon, no abilities, wrong realm, etc.).

### `GeisWeaponDefinition` (soul slice)

- Each weapon may assign two `SoulWeaponAbilityAsset` references and `buildsLyreResonance` — see [combat.md](combat.md).

## Scope

- Owns: soul realm lifecycle, ghost motor coordination, ability routing, destroyable interfaces under `WeaponAbilities/`.
- Does not own: melee hit resolution except where abilities explicitly spawn effects or call combat APIs.

## Architecture

- `SoulRealmManager` ↔ camera, locomotion, visuals.
- `SoulRealmWeaponAbilityController` ↔ `GeisWeaponSwitcher` + separate input asset + `SoulRealmManager` for context.

## Key types & assets

| Piece | Path |
|-------|------|
| Manager | `Assets/Geis/Scripts/SoulRealm/SoulRealmManager.cs` |
| Interactable flag | `Assets/Geis/Scripts/SoulRealm/SoulRealmInteractable.cs` |
| Ability controller | `Assets/Geis/Scripts/SoulRealm/WeaponAbilities/SoulRealmWeaponAbilityController.cs` |
| Abilities | `Assets/Geis/Scripts/SoulRealm/WeaponAbilities/` |
| Assets | `Assets/Geis/SoulRealm/Abilities/*.asset` |
| Input | `Assets/Geis/Scripts/Input/GeisControls.inputactions` (`SoulRealmWeapon` map) |

## Integration

- [input.md](input.md): Tab/LB soul realm on Player map; Q/F and ability map separate.
- [combat.md](combat.md): Weapon definitions carry ability assets.

## Rules

*(Add explicit project rules here when you want them enforced in reviews.)*

## Guidelines

- New abilities should implement `SoulWeaponAbilityAsset` / context pattern and respect realm flags on the asset.

## Related documentation

- `Assets/Docs/Features/soul-weapon-abilities.md`

## Changelog

- **2026-04-01**: Filled behavior & contracts from code; Rules left for manual additions.
