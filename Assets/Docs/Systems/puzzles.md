# Puzzles

**Status**: current  
**Last updated**: 2026-04-01

## Purpose

Reusable puzzle elements, triggers, outputs, and presentation (realm tint/dissolve) so level scripting can gate behavior by physical vs. soul realm without duplicating checks everywhere.

## Behavior & contracts

### `PuzzleElementBase`

- **ExecuteAlways** — runs in editor for realm presentation.
- **Realm gating**: Each element has a `PuzzleRealmMode`:
  - **`SoulOnly`** — interactable/active only while soul realm is on.
  - **`PhysicalOnly`** — only in the normal physical world.
  - **`BothRealms`** — active in either; presentation may still tint/dissolve.
- **Centralized check**: Subclasses should rely on base `RealmMode` + `IsAccessibleInCurrentRealm()` (and related) so they **don’t repeat** soul-realm checks inconsistently.
- **Soul realm sync**: Subscribes to `SoulRealmManager.SoulRealmStateChanged` on enable; refreshes presentation when realm toggles.
- **Presentation**: Optional **realm material tint** on child renderers; optional **noise dissolve** via `_Dissolve` (Shader Graph), with duration, optional renderer overrides, optional **BothRealms** dissolve material template when children use stock URP Lit. Editor `OnValidate` refreshes caches when fields change.

### Realm modes (enum)

See `PuzzleRealmMode.cs` — values above; used for accessibility and fade direction (physical-only vs soul-only dissolve behavior documented in tooltips on the base class).

### Cross-realm interactions

- **Physical interactions while in soul realm**: Gameplay code can use `SoulRealmInteractable.BlockPhysicalInteractions` ([soul-realm.md](soul-realm.md)) to skip “normal world” use/combat/pickups; puzzle triggers that are **soul-only** should require soul realm via `PuzzleElementBase` / `SoulRealmManager.IsSoulRealmActive` as appropriate.

### Triggers & outputs

- Concrete triggers live under `Triggers/` (pressure plates, alignment dials, bow targets, soul switch, sword hit, etc.); outputs under `Outputs/` (e.g. `BarrierOutput`). Each should document required collider layers/tags **in code** or in this file when adding a new type.

## Scope

- Owns: puzzle base, realm presentation, trigger/output hierarchy under `Geis.Puzzles`.
- Does not own: combat resolution or soul ability implementation — only **invokes** public APIs or interfaces (e.g. `IPuzzleMeleeHitSink`).

## Architecture

- `PuzzleElementBase` → subclass triggers/outputs; optional helpers (`PuzzleInteractionPrompt`, `PuzzleRealmVisual`, `PuzzleBoxColliderInflate`, etc.).

## Key types & assets

| Piece | Path |
|-------|------|
| Base | `Assets/Geis/Scripts/Puzzles/Core/PuzzleElementBase.cs` |
| Realm mode | `Assets/Geis/Scripts/Puzzles/Core/PuzzleRealmMode.cs` |
| Triggers | `Assets/Geis/Scripts/Puzzles/Triggers/` |
| Outputs | `Assets/Geis/Scripts/Puzzles/Outputs/` |

## Integration

- Subscribes to `SoulRealmManager` for state; may reference [combat.md](combat.md) / [input.md](input.md) for weapon-specific puzzles.

## Rules

*(Add explicit project rules here when you want them enforced in reviews.)*

## Guidelines

- Prefer `PuzzleElementBase` realm fields over ad-hoc `IsSoulRealmActive` checks in every trigger unless the puzzle is explicitly an exception.

## Related documentation

- `Assets/Documentation/HEXAGON_LEVEL_EDITOR_*.md` where relevant.

## Changelog

- **2026-04-01**: Filled behavior & contracts from code; Rules left for manual additions.
