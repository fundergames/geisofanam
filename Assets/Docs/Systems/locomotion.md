# Locomotion & camera

**Status**: current  
**Last updated**: 2026-05-20

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
- **Dodge / roll (GoW-style)**: Single tap B → `Dodge` sub-state machine (`Dodge_*_Root` sidesteps). Double-tap B → `Roll` sub-state machine (dedicated roll clips). Both use `DodgeDirection` (0–3) for entry routing; rolls use the `Roll` trigger on Any-State. Run **Geis → Animator → Setup Directional Dodge & Roll Clips** after clip or graph changes.
- **Lock-on anchor**: The player updates a detached world-space helper for lock-on aiming/reticle placement; do not reuse a player-child transform for the active lock-on anchor or root motion will drag the reticle/camera target during attack and dodge clips.
- **Soul realm**: `SoulRealmManager.ShouldSuppressBodyLocomotion` / related flags suppress normal body locomotion update while soul realm is active; animator may be paused while body follows ground.

### Directional hit reactions (player)

- `GeisDirectionalHitReaction` on the player listens to `CombatEvents.OnHitReactionStarted` and plays **Front / Back / Left / Right** flinch from strike direction (`CombatHitDirectionUtility`).
- Assign a `DirectionalHitReactionSet` asset (Synty Polygon sword clips: `A_Hit_F_React_Sword`, `A_Hit_B_React_Sword`, `A_Hit_L_React_Sword`, `A_Hit_R_React_Sword`).
- Animator: **`HitReaction` override layer** on `AC_Polygon_Masculine_Geis` with empty default state plus `HitReact_F/B/L/R`. Synty **F/B/L/R = recoil direction** (not strike origin): hit from behind → F, from front → B, from right → L, from left → R (`CombatHitDirectionUtility.ToReactionDirection`). Layer **weight stays 0** until a hit; **Any State** uses `TakeDamage` + `HitDirection` (0=F … 3=R recoil indices).
- Run **Geis → Combat → Setup Directional Hit Reactions On Selected GeisPlayer** to create the set asset, layer, transitions, and wire the component (`crossFadeToState` off; uses `TakeDamage` + `HitDirection`).

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
| Animation / state | `Assets/Geis/Scripts/Locomotion/GeisPlayerAnimationController*.cs` (core + Avatar, LockOn, Combat, LocomotionStates partials) |
| Locomotion kinematics | `Assets/Geis/Scripts/Locomotion/GeisLocomotionKinematics.cs` |
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

- **2026-05-20**: Profile bundle is tuning source of truth: `ApplyLocomotionTuningFromProfiles()` applies code defaults then overlays assigned `GeisPlayerLocomotionProfileBundle` (Resources fallback at `Movement/PlayerLocomotionProfiles`). Bow animator writes extracted to `GeisBowAnimatorPresenter` with `GeisBowPresentationProfile`.
- **2026-05-20**: Refactor pass: input buffer/dodge/combo extracted (`GeisInputBufferTracker`, `GeisDodgeRollController`, `GeisComboAttackController`); profile bundle wired on Player prefab; animator IDs consolidated via `LocomotionAnimatorIds` / `BowAnimatorIds`; `GeisInputReader.UpdateMovementTapState` encapsulates movement tap/press/held.
- **2026-05-20**: Fixed soul ghost appearing frozen after enter dissolve: `SoulSpectralAnimatorDriver` left `animator.speed` at 0 when handing off to `GeisPlayerAnimationController` (`EnsurePresentationAnimatorAdvancing`).
- **2026-05-20**: Single locomotion/combat owner: `GeisPlayerAnimationController` binds the soul ghost via `.Avatar.cs` (`BindSoulRealmLocomotionAvatar` / `PresentationAnimator`); `SoulGhostMotor` is a trigger marker only. Enter freeze still applies bow params on the spectral rig; full FSM runs when `AllowGhostMovement`.
- **2026-05-20**: `GeisPlayerAnimationController` split into partials (`GeisPlayerAnimationController.cs` core, `.Avatar.cs`, `.LockOn.cs`, `.Combat.cs`, `.LocomotionStates.cs`). Planar speed via `GeisLocomotionKinematics`. `HasAnimatorParameter` uses `AnimatorParameterGuard`.
- **2026-05-20**: Body locomotion animator writes now route through `LocomotionAnimatorApplier` + `LocomotionPresentationSnapshot`; combo blend/playback shared via `GeisComboAnimatorBlend` and `ComboAttackPlayback` (body, soul-realm melee, enemies).
- **2026-05-19**: Directional roll clips wired separately from sidesteps (`Dodge_*_Root` vs roll states on Base Layer); menu **Geis → Animator → Setup Directional Dodge & Roll Clips**.
- **2026-05-19**: GoW-style dodge/roll pass: neutral stick backsteps (away from lock-on target when locked); moving/stick-relative 4-way sidesteps; double-tap rolls follow stick with longer i-frames/recovery and `rollDistanceMultiplier`; lock-on preserves facing during sidesteps; strafe facing capped by `_strafeStyleMaxPlanarSpeed` (default 5).
- **2026-05-19**: Dodge/dash (tap B) now commits on the first press with no double-tap wait window; a second press within `dodgeDoubleTapWindow` upgrades an early dash to the forward roll.
- **2026-05-18**: Documented directional player hit reactions (`GeisDirectionalHitReaction`, `CombatHitDirection`, Synty F/B/L/R clips).

- **2026-04-27**: Lock-on now uses a detached world-space runtime anchor instead of the player child `TargetLockOnPos`, preventing attack/dodge root motion from pulling the reticle/camera target forward and back.
- **2026-04-27**: Jump input is now hard-gated during `AnimationState.Attack`, preventing combo presses from buffering a jump into post-combo locomotion/landing.
- **2026-04-27**: Camera aim shoulder zoom/FOV is now gated by `ShouldUseBowAimZoom` so dagger/sword aim states no longer trigger bow-style zoom.
- **2026-04-27**: Soul-realm exit now restores the camera to the controller's captured pre-entry follow target, preventing feet-level framing when manager-side look-at lookup falls back to the body root.
- **2026-04-23**: Added explicit anti-regression rules for transition ownership, controller parity, and combat event routing.
- **2026-04-01**: Filled behavior & contracts from code; Rules left for manual additions.
