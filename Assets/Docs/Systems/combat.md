# Combat (Geis)

**Status**: current  
**Last updated**: 2026-04-01

## Purpose

Weapon definitions, switching, combos, hit detection, and bridges between Geis animation and RogueDeal combat (damage, executors, projectiles).

## Behavior & contracts

### `GeisWeaponSwitcher`

- **Slots (fixed)**: `[0]` Unarmed, `[1]` Knife, `[2]` Sword, `[3]` Bow. `GeisWeaponDefinition` per slot is the **preferred** source for prefab, `GeisComboData`, and RogueDeal `Weapon` / `CombatAction`.
- **Input**: Keyboard **1–4** select slot; gamepad **D-pad up** cycles forward through equipped slots. **Start**: if no weapon equipped and slots exist, equips slot `0`.
- **Soul realm gate**: While `SoulRealmInteractable.BlockPhysicalInteractions` is true (`SoulRealmManager` active), **weapon switching input is ignored** — no mid-soul-realm weapon changes via switcher.
- **Attachment**: Resolves right/left hand bones by configurable name lists; optional manual animators and attachment transforms; supports `WeaponAttachmentHand` on the definition (e.g. bow on left).
- **API**: `CurrentWeaponIndex` (-1 if none), `GetWeaponDefinition(index)`, `TryGetComboForWeapon`.

### `GeisWeaponDefinition` (ScriptableObject)

- **Visual**: `weaponPrefab`, `displayName`, `attachmentHand` (left/right).
- **Combo**: `GeisComboData` — per-state combat bindings and optional multi-hit timing for `GeisCombatBridge`.
- **Damage**: `weaponStats` (`Weapon`), `combatAction` (`CombatAction`) for RogueDeal.
- **Soul**: `PrimarySoulAbility` / `SecondarySoulAbility` (`SoulWeaponAbilityAsset`), `buildsLyreResonance` for Lyre meter on hit.

### `GeisCombatBridge`

- **Requires**: `CombatEntity`, RogueDeal `CombatExecutor`, `SimpleAttackHitDetector` on the same GameObject.
- **Flow**: Subscribes to `GeisPlayerAnimationController.OnAttackPerformed(weaponIndex)`. Resolves `CombatAction` + `Weapon` from **`GeisWeaponSwitcher.GetWeaponDefinition`** when non-null; applies `GeisComboData.ResolveCombatAction(comboState, …)` and optional **multi-hit times** from combo data (`TryGetMultiHitTimesSeconds`). Falls back to **legacy** arrays on the bridge if no definition: `combatActionsByWeapon` / `weaponsBySlot` (same 4-slot indexing).
- Sets `CombatEntity` entity data `equippedWeapon` for the resolved strike.

### `GeisBowController` (slot 3)

- Bow-specific: aim (LT), charge/release on heavy attack pipeline, arrow spawn, camera aim ray (uses `GeisCameraController`), `Projectile` on arrow prefab. **Aim ray** uses configurable layers; **ignores trigger colliders** by policy so lock-on volumes do not shorten aim.
- Optional `SoulMarkHomingTracker` for soul-mark homing behavior in soul realm.
- Emits `onChargeStarted`, `onArrowFired(chargeRatio)` for animation/UI hooks.

### Cross-cutting

- **Physical interactions in soul realm**: `SoulRealmInteractable.BlockPhysicalInteractions` is true while soul realm is active — weapon **switching** respects this; other systems (puzzles, use) may check the same flag.

## Scope

- Owns: weapon definitions, switcher, combat bridge, bow controller, attachment.
- RogueDeal owns: `CombatEntity`, damage pipeline, `Projectile`, hit detector implementation details.

## Architecture

- Animation fires attacks → bridge resolves data → `SimpleAttackHitDetector` performs hit checks with optional multi-hit timing arrays.

## Key types & assets

| Piece | Path |
|-------|------|
| Core | `Assets/Geis/Scripts/Combat/GeisCombatBridge.cs`, `GeisWeaponSwitcher.cs`, `GeisWeaponDefinition.cs`, `GeisBowController.cs` |
| Weapons | `Assets/Geis/Weapons/` |

## Integration

- Input: light/heavy/dodge/aim from [input.md](input.md). Soul abilities: [soul-realm.md](soul-realm.md).

## Rules

*(Add explicit project rules here when you want them enforced in reviews.)*

## Guidelines

- Prefer a single `GeisWeaponDefinition` per slot over legacy arrays on `GeisCombatBridge` for new content.

## Related documentation

- `Assets/Documentation/COMBAT_SYSTEM_SUMMARY.md`, `COMBAT_SYSTEM_IMPLEMENTATION_GUIDE.md`, `WEAPON_COLLIDER_SETUP.md`, etc.

## Changelog

- **2026-04-01**: Filled behavior & contracts from code; Rules left for manual additions.
