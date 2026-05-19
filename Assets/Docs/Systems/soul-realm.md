# Soul realm & weapon abilities

**Status**: current  
**Last updated**: 2026-04-01

## Purpose

Soul realm mode (ghost vs. frozen body), world freeze hooks, weapon-bound supernatural abilities, and shared ability origin/camera context for VFX and gameplay.

## Behavior & contracts

### `SoulRealmManager`

- **Singleton**: `Instance`; raises static `SoulRealmStateChanged` when entering/exiting (puzzles, ability controller, presentation subscribe).
- **State**: `IsSoulRealmActive`, `SoulRealmBlend` (0/1), exit hold progress APIs (`SoulRealmExitHoldProgress01`, `IsSoulRealmExitHoldInProgress`, etc.).
- **Movement**: `AllowGhostMovement` — during enter grace, ghost can move; after grace, holding **SoulRealm** input blocks ghost movement (exit path). Body locomotion suppressed via `ShouldSuppressBodyLocomotion` while active. Runtime ability flows may temporarily push an external ghost-movement freeze (for example, object blink manipulation).
- **Camera return target**: On entry, capture the camera controller's current follow target and use that same transform for exit-hold interpolation and final restore. Do not reconstruct the return pivot from a separate look-at lookup, or the camera can fall back to the body root/feet on exit.
- **Abilities / VFX origin**: `GetAbilityContextTransforms(out ownerTransform, out originWorld)` — in soul realm uses **ghost** root and chest-height style origin; otherwise body locomotion and look-at. **Weapon ability activation** should use this when the manager exists.
- **Freeze registry**: `SoulRealmFreezeTarget` list for selective world freeze (implementation detail in manager code).
- **Delta time**: Internal cap `SoulRealmMaxDeltaPerFrame` on soul-realm timers to avoid huge `deltaTime` spikes completing exit in one frame.
- **Spectral weapon attachment**: `TryGetSpectralAttachmentTransform(...)` should mirror the chosen body attachment transform onto the spectral clone first so custom prop sockets/binders survive realm switching; only fall back to left/right hand socket lookup when no cloned match exists.

### Realm-scoped simulation (`RealmSimulation`)

Some gameplay/VFX logic is not affected by the freeze registry (notably **coroutines** and **particle systems** that continue advancing using `Time.deltaTime`). Use `RealmSimulation` when logic should advance only in a specific realm:

- **Groups**:
  - `RealmSimulationGroup.Physical`: simulates only while **not** in soul realm.
  - `RealmSimulationGroup.Soul`: simulates only while **in** soul realm.
  - `RealmSimulationGroup.Universal`: always simulates.
- **Helpers**:
  - `RealmSimulation.DeltaTime(group)` returns `0` when that group is frozen.
  - `RealmSimulation.WaitForSecondsRealm(group, seconds)` waits in realm time (does not elapse while frozen).

Rule of thumb: if you are using `WaitForSeconds` or a `Time.deltaTime` loop for **physical-only** hazards/attacks/VFX, switch it to the realm-scoped helpers so it truly freezes during soul realm.

### `SoulRealmInteractable` (static helper)

- `BlockPhysicalInteractions` → true when `SoulRealmManager.Instance.IsSoulRealmActive`. Used to **block normal physical interactions** (weapon switcher, interact scripts that respect it). Soul-only gameplay should still check `IsSoulRealmActive` where relevant.

### `SoulRealmWeaponAbilityController`

- **Input**: Assign **`GeisControls`** `InputActionAsset` (contains the **`SoulRealmWeapon`** map); at runtime the controller **Instantiates** a copy so Enable/Disable state is not shared globally.
- **Required map**: Action map name **`SoulRealmWeapon`**, actions **`Ability1`** and **`Ability2`**. Errors logged if missing.
- **Enable/disable map** (`SyncActionMapWithRealm`): Map is **on** when current weapon has ability assets **and** either (a) **soul realm** — any primary/secondary asset exists, or (b) **physical realm** — at least one ability has `AllowActivationInPhysicalRealm`. Per-ability realm rules are enforced again in `TryActivateAbility`.
- **Polling**: Keyboard **Q** / **F** via the ability map; gamepad **LT+LB** = ability 1 and **LT+RB** = ability 2 (polled on hardware, not separate map bindings).
- **`TryActivateAbility`**: Resolves current `GeisWeaponDefinition` from `GeisWeaponSwitcher`; reads `PrimarySoulAbility` / `SecondarySoulAbility`; checks `AllowActivationInSoulRealm` / `AllowActivationInPhysicalRealm` vs current realm; builds `SoulWeaponAbilityContext` with `GetAbilityContextTransforms` from manager when present, else `abilityOrigin`; forward from `GeisCameraController.GetCameraForwardZeroedYNormalised()` when available.
- **Feedback**: `SoulRealmAbilityFeedback` auto-added if missing; shows blocked reasons (no weapon, no abilities, wrong realm, etc.).

### `SoulBlinkManipulationController`

- **Freeze contract**: Beginning Object Blink manipulation must freeze the ghost motor as well as interaction locomotion, so the soul body stays locked in place until the player cancels or snaps the object.
- **Movement contract**: Manipulated objects keep an independent world pose; they should not be reparented to the ghost or forced to a camera-forward hold point each frame.
- **Default controls**: `Move` slides the object in the camera plane, keyboard `UpArrow` / `DownArrow` or gamepad `D-pad Up` / `D-pad Down` move it vertically, and holding `Aim` (gamepad left trigger) turns `Move` into rotation input for spin/tilt alignment.

### `SoulPhaseShiftable`

- **Realm ownership**: A phase-shiftable object is solid in one realm at a time (`Physical` or `Soul`) and ethereal in the other. Realm switches should immediately refresh collision/presentation so the object becomes passable when the player is in the non-owned realm while remaining visible and targetable.
- **Secondary ability flow**: Dagger secondary is a hold action in either realm. Holding `Ability2` / `F` on a `SoulPhaseShiftable` pulls it into the player's current realm; on completion it stays solid in that realm until shifted back from the opposite realm.
- **Presentation**: `SoulPhaseShiftPresentation` should preview the pull while held, then show a solid look only when the object is owned by the active realm; the opposite realm should return to the dissolve/ethereal look after the transfer. Ethereal collision should be implemented via trigger colliders rather than moving the prop onto a layer that ability raycasts cannot hit.

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

- [input.md](input.md): Tab/LB soul realm on Player map; Q/F abilities; gamepad LT+LB / LT+RB for abilities.
- [combat.md](combat.md): Weapon definitions carry ability assets.

## Rules

- Realm-gated timers/coroutines for frozen-world behavior must use `RealmSimulation` helpers instead of raw `Time.deltaTime` + `WaitForSeconds`.
- Ability activation must enforce both asset realm flags (`AllowActivationInSoulRealm` / `AllowActivationInPhysicalRealm`) and current realm state.
- Ability origins should come from `SoulRealmManager.GetAbilityContextTransforms` when available; fallback-only origins should be treated as compatibility paths.
- Systems that must not run during soul realm physical lock must gate interactions through `SoulRealmInteractable.BlockPhysicalInteractions`.
- Object Blink manipulation must preserve an independently controlled object pose; do not couple the held object's transform directly to the player/ghost transform while aligning to a socket.
- Phase-shiftable objects must store which realm owns their solidity and recompute collision/presentation from that state whenever soul realm toggles.

## Guidelines

- New abilities should implement `SoulWeaponAbilityAsset` / context pattern and respect realm flags on the asset.

## Related documentation

- `Assets/Docs/Features/soul-weapon-abilities.md`

## Changelog

- **2026-05-19**: Gamepad abilities use LT+LB / LT+RB; removed Y/RB-only ability fallbacks.
- **2026-04-29**: Dagger phase shift now transfers a target object into the player's current realm via hold input in either realm; shifted props remain solid only in their owned realm and can be moved back and forth by repeating the hold from the other realm.
- **2026-04-28**: Object Blink socket manipulation now freezes the ghost motor and uses direct translation/rotation controls (`Move`, vertical via keyboard arrows or gamepad d-pad, `Aim`/left-trigger modifier) so blink targets no longer feel parented to the player while aligning with a socket.
- **2026-04-27**: Spectral weapon attachment now mirrors the body rig's attachment transform onto the spectral clone before falling back to left/right hand lookup, fixing soul-realm parenting on models that do not share the same prop-bone binder setup.
- **2026-04-27**: Soul-realm exit now reuses the camera's captured pre-entry follow target for hold lerp and final restore, fixing occasional feet-level camera framing after returning to the body.
- **2026-04-02**: Spectral locomotion uses ghost grounded (not body’s frozen Jump/Fall state) for air vs ground animator blend; ghost syncs vertical velocity on entry; grounded refreshed after teleport to body.
- **2026-04-01**: Filled behavior & contracts from code; Rules left for manual additions.
