# Locomotion & camera

**Status**: current  
**Last updated**: 2026-04-01

## Purpose

Third-person movement, rotation, and camera follow; coordination with animation (including combat layers) so the character responds consistently to input and state.

## Behavior & contracts

### `GeisCameraController`

- Third-person orbit: distance, height/horizontal offsets, tilt bounds, mouse sensitivity, optional invert; positional/rotational lag toward a follow target.
- **Aim (LT / `Player/Aim`)**: Optional shoulder rig (distance, horizontal offset, height), FOV change, smooth times — works in **physical and soul realm** per tooltips.
- **Soul realm**: Stores baseline when switching follow to the ghost; during **hold-to-exit** (soul realm bumper), lerps rotation/progress; can ease back if exit hold is released early (`_soulRealmExitHoldReleaseSmoothTime`). Public hooks used by `SoulRealmManager` for follow target / transition (see `SoulRealmManager` + camera “Call while still following physical body…” style APIs in `GeisCameraController`).
- Uses `GeisInputReader` for look; **read look in `LateUpdate`** pattern is documented on the input side for full gamepad fidelity.

### `GeisPlayerAnimationController`

- Large integration surface: locomotion gait, strafe, crouch, sprint, dodge, combat combo, bow layers, masks, etc.
- **Combat bridge contract**: Exposes `OnAttackPerformed` with **`int weaponIndex`** and `CurrentComboState` so `GeisCombatBridge` can resolve `GeisComboData` multi-hit times and `CombatAction` per strike.
- **Public state** (for other systems): includes `IsAiming`, `IsBowDrawing`, locomotion flags (`LocomotionIsSprinting`, `LocomotionIsWalking`, …), `CurrentComboState`, `LocomotionDodgeRequiresMovementInput`, etc.
- **Soul realm**: `SoulRealmManager.ShouldSuppressBodyLocomotion` / related flags suppress normal body locomotion update while soul realm is active; animator may be paused while body follows ground.

### `SoulRealmManager` (locomotion-related)

- While soul realm active: physical body **stays visible**, **locomotion suppressed** on the body; **animator paused** on body; body can still follow moving ground; **spectral ghost** moves with `SoulGhostMotor`.
- **Exit**: Hold soul-realm input to exit; duration scales with ghost–body separation (min/max durations). `Time.deltaTime` clamped per frame (`SoulRealmMaxDeltaPerFrame`) so editor pause/unpause does not instantly complete exit.
- **`GetAbilityContextTransforms`**: When in soul realm, ability origin uses **ghost** and chest-height style offset; otherwise body / look-at — abilities and VFX should use this for consistent origins ([soul-realm.md](soul-realm.md)).

## Scope

- Owns: `GeisCameraController`, `GeisPlayerAnimationController` locomotion/combat animation integration.
- Shared: soul realm body/ghost policy with `SoulRealmManager`.

## Architecture

- Input → `GeisPlayerAnimationController` + `GeisCameraController`.
- Combat events flow **out** of animation controller (`OnAttackPerformed`) to `GeisCombatBridge`.

## Key types & assets

| Piece | Path |
|-------|------|
| Camera | `Assets/Geis/Scripts/Locomotion/GeisCameraController.cs` |
| Animation / state | `Assets/Geis/Scripts/Locomotion/GeisPlayerAnimationController.cs` |
| Soul realm | `Assets/Geis/Scripts/SoulRealm/SoulRealmManager.cs` |

## Integration

- Bow: aim + animation controller + bow controller ([combat.md](combat.md)).
- Abilities: camera forward / ray origin from `SoulRealmWeaponAbilityController` using `GeisCameraController.MainCamera` and `GetCameraForwardZeroedYNormalised()` when present.

## Rules

*(Add explicit project rules here when you want them enforced in reviews.)*

## Guidelines

- Treat animator parameter and layer changes as high-impact; prefer isolated layers/masks for new weapon modes.

## Related documentation

- `Assets/Documentation/WALK_RUN_SETUP_GUIDE.md`, `THIRD_PERSON_ANIMATOR_SETUP.md`, and related setup files.

## Changelog

- **2026-04-01**: Filled behavior & contracts from code; Rules left for manual additions.
