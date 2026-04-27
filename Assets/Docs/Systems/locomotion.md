# Locomotion & camera

**Status**: current  
**Last updated**: 2026-04-01

## Purpose

Third-person movement, rotation, and camera follow; coordination with animation (including combat layers) so the character responds consistently to input and state.

## Behavior & contracts

### `GeisCameraController`

- Third-person orbit: distance, height/horizontal offsets, tilt bounds, mouse sensitivity, optional invert; positional/rotational lag toward a follow target.
- **Aim (LT / `Player/Aim`)**: Optional shoulder rig (distance, horizontal offset, height), FOV change, smooth times — works in **physical and soul realm** per tooltips, but the tighter aim zoom should only engage for bow aim, not melee weapons that also set `IsAiming`.
- **Soul realm**: Stores baseline when switching follow to the ghost; during **hold-to-exit** (soul realm bumper), lerps rotation/progress; can ease back if exit hold is released early (`_soulRealmExitHoldReleaseSmoothTime`). `SoulRealmManager` should restore the camera to `GeisCameraController.FollowTargetTransform` captured on entry rather than re-deriving a body pivot, so exit snaps back to the same chest-height anchor instead of a root/feet fallback.
- Uses `GeisInputReader` for look; **read look in `LateUpdate`** pattern is documented on the input side for full gamepad fidelity.

### `GeisPlayerAnimationController`

- Large integration surface: locomotion gait, strafe, crouch, sprint, dodge, combat combo, bow layers, masks, etc.
- **Combat bridge contract**: Exposes `OnAttackPerformed` with **`int weaponIndex`** and `CurrentComboState` so `GeisCombatBridge` can resolve `GeisComboData` multi-hit times and `CombatAction` per strike.
- **Public state** (for other systems): includes `IsAiming`, `IsBowEquipped`, `ShouldUseBowAimZoom`, `IsBowDrawing`, locomotion flags (`LocomotionIsSprinting`, `LocomotionIsWalking`, …), `CurrentComboState`, `LocomotionDodgeRequiresMovementInput`, etc.
- **Combo jump gate**: While `AnimationState.Attack` is active, jump presses are ignored rather than buffered, so melee combo chains cannot transition into a jump or queue a post-combo jump from the same input.
- **Lock-on anchor**: The player updates a detached world-space helper for lock-on aiming/reticle placement; do not reuse a player-child transform for the active lock-on anchor or root motion will drag the reticle/camera target during attack and dodge clips.
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

- Keep camera/soul-realm transitions single-owned: modify transition timing in `GeisCameraController` or `SoulRealmManager`, not both for the same effect.
- Any locomotion or camera change must preserve controller parity (keyboard/mouse and gamepad) and avoid per-device behavior drift.
- Do not add gameplay side effects in animation events here; combat effects should route through `OnAttackPerformed` and `GeisCombatBridge`.

## Guidelines

- Treat animator parameter and layer changes as high-impact; prefer isolated layers/masks for new weapon modes.

## Related documentation

- `Assets/Documentation/WALK_RUN_SETUP_GUIDE.md`, `THIRD_PERSON_ANIMATOR_SETUP.md`, and related setup files.

## Changelog

- **2026-04-27**: Lock-on now uses a detached world-space runtime anchor instead of the player child `TargetLockOnPos`, preventing attack/dodge root motion from pulling the reticle/camera target forward and back.
- **2026-04-27**: Jump input is now hard-gated during `AnimationState.Attack`, preventing combo presses from buffering a jump into post-combo locomotion/landing.
- **2026-04-27**: Camera aim shoulder zoom/FOV is now gated by `ShouldUseBowAimZoom` so dagger/sword aim states no longer trigger bow-style zoom.
- **2026-04-27**: Soul-realm exit now restores the camera to the controller's captured pre-entry follow target, preventing feet-level framing when manager-side look-at lookup falls back to the body root.
- **2026-04-23**: Added explicit anti-regression rules for transition ownership, controller parity, and combat event routing.
- **2026-04-01**: Filled behavior & contracts from code; Rules left for manual additions.
