# Input

**Status**: current  
**Last updated**: 2026-04-01

## Purpose

Centralize player input: gameplay actions, weapon abilities, and interact prompts, and forward them to locomotion, combat, soul abilities, and puzzles.

## Behavior & contracts

### `GeisInputReader` (`GeisControls` Player map)

- Implements `GeisControls.IPlayerActions`; constructs `GeisControls` in `OnEnable`, enables `Player` when the application has focus, disables on focus loss (clears move/look/movement flags).
- **Look**: Consumers that need correct **every-frame** gamepad look should read `LookInput` (`_controls.Player.Look.ReadValue<Vector2>()`), not only `OnLook`’s `_mouseDelta`, so steady right-stick values are not dropped.
- **Soul realm toggle**: Exposes `SoulRealm` action reference and `SoulRealmWasPressedThisFrame()` for enter detection (callbacks on `OnSoulRealm` are empty; detection is via `WasPressedThisFrame`).
- **Sprint**: **Keyboard Shift** = hold sprint (`onSprintActivated` / `onSprintDeactivated` on edge). **Gamepad L3** (left stick press) **toggles** jog vs sprint; `IsSprintHeldOrToggled` reflects Shift **or** that toggle. **Lock-on** clears both sprint modes and syncs sprint output.
- **Heavy attack**: `onHeavyAttackStarted` on `started`, `onHeavyAttackReleased` on `canceled`, `onHeavyAttackPerformed` on `performed` (bow charge uses started/canceled).
- **Dodge**: `OnDodge` accepts `started` or `performed` and dedupes with `TryInvokeDodgeOnce` (same frame). Optional **fallback**: if raw `Gamepad.buttonEast` pressed but `Player/Dodge` did not fire this frame, still invokes dodge once (guards broken binding cache).
- **Walk toggle**: `OnToggleWalk` is wired but described as no bindings (walk toggle disabled until bindings exist).

### `GeisInteractInput` (static, assembly `Geis.InteractInput`)

- **Not** the same as `Player` map interact actions — this is a **shared poll API** for puzzles/NPCs: `WasInteractPressedThisFrame(Key)`, `WasInteractReleasedThisFrame`, `IsInteractHeld`.
- **Keyboard**: Configurable primary key **plus** always treats **X** and **B** as alternates for press/release/hold. Iterates **`Keyboard.all`**, not only `Keyboard.current`, so multi-keyboard / odd `PlayerInput` setups still see input.
- **Gamepad**: `Gamepad.current`, else `Gamepad.all[0]`. **West, North, East** face buttons all count as “interact” for press/release/hold.

### Soul ability map (`SoulRealmWeapon` on `GeisControls`)

- The **`SoulRealmWeapon`** action map lives in **`GeisControls.inputactions`** (same asset as the Player map). `SoulRealmWeaponAbilityController` assigns that asset and enables only the `SoulRealmWeapon` map in parallel; it **Instantiates** a copy at runtime so map state is not shared (see [soul-realm.md](soul-realm.md)).

## Scope

- Owns: `GeisControls` usage, `GeisInputReader`, static `GeisInteractInput` polling.
- Does not own: camera math, animator parameters, or puzzle-specific validation beyond supplying buttons.

## Architecture

- `GeisControls` + generated C# from `.inputactions`.
- `GeisInputReader` — subscribes as `IPlayerActions`, exposes fields and `Action` delegates for other components.
- Interact — lightweight asmdef so triggers and NPCs can reference polling without full combat stack.

## Key types & assets

| Piece | Path |
|-------|------|
| Reader | `Assets/Geis/Scripts/Input/GeisInputReader.cs` |
| Generated API | `Assets/Geis/Scripts/Input/GeisControls.cs` |
| Action assets | `Assets/Geis/Scripts/Input/GeisControls.inputactions` (includes `Player` + `SoulRealmWeapon` maps) |
| Interact poll | `Assets/Geis/Scripts/Input/GeisInteractInput/GeisInteractInput.cs` |

## Integration

- Locomotion reads move/look/sprint/aim; combat reads attacks/dodge; `SoulRealmManager` reads soul-realm input; `SoulRealmWeaponAbilityController` uses the separate ability map.

## Rules

*(Add explicit project rules here when you want them enforced in reviews.)*

## Guidelines

- After editing `.inputactions`, keep the generated C# wrapper in sync with the project’s usual workflow.

## Related documentation

- `Assets/Documentation/` — setup guides as needed.

## Changelog

- **2026-04-01**: Filled behavior & contracts from code; Rules left for manual additions.
