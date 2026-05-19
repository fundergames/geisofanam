# Combat (Geis)

**Status**: current  
**Last updated**: 2026-05-07

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
- **Hit path selection**: Uses `WeaponHitbox` only when the equipped weapon has a hitbox **and** the current combo state defines `multiHitNormalizedTimes`. Otherwise falls through to `SimpleAttackHitDetector` (most light attacks).
- Sets `CombatEntity` entity data `equippedWeapon` for the resolved strike.

### `SimpleAttackHitDetector` (player melee probes)

- **Overlap queries** (`combatOverlapTriggerInteraction`, default **Ignore**) only count **solid** hurtbox colliders. Large **trigger** volumes used for `GeisObjectLockOn` / lock-on detection must **not** be included in melee reach — matching the bow pipeline’s rule of not letting lock-on shells define combat range (`GeisBowController`).

### `WeaponHitbox` (equipped melee weapons)

- Collider-hit melee validates target tags against the touched collider, its resolved `CombatEntity`, and the collider root so child hurtboxes still count when only the enemy root carries the `Enemy` tag.
- If an authored action leaves `effects` empty and only uses `perHitEffects`, the hitbox path applies the current combo hit effect when available instead of dropping the strike.

### `GeisBowController` (slot 3)

- Bow-specific: aim (LT), draw/release on **RT** (`Player/HeavyAttack`), arrow spawn, camera aim ray (uses `GeisCameraController`), `Projectile` on arrow prefab. **Aim ray** uses configurable layers; **ignores trigger colliders** by policy so lock-on volumes do not shorten aim.
- Optional `SoulMarkHomingTracker` for soul-mark homing behavior in soul realm.
- Emits `onChargeStarted`, `onArrowFired(chargeRatio)` for animation/UI hooks.

### Cross-cutting

- **Physical interactions in soul realm**: `SoulRealmInteractable.BlockPhysicalInteractions` is true while soul realm is active — weapon **switching** respects this; other systems (puzzles, use) may check the same flag.
- **Enemy strike targeting**: Enemy AI chooses only player-like `CombatEntity` targets (`Player` tag / `PlayerVisual`) and passes that explicit target into `CombatExecutor` so generic targeting assets do not retarget onto nearby enemies at execute time.
- **Directional hit reactions**: `CombatHitDirection` on `CombatEventData` (strike origin). `GeisDirectionalHitReaction` on player and enemies (`ICombatHitReactionPresenter`); `CombatEvents.TriggerHitReactionStarted` calls `PresentHitReaction`. Synty F/B/L/R = recoil direction ([locomotion.md](locomotion.md)). Menu: **Geis → Combat → Setup Directional Hit Reactions On Selected Enemy** / **Add Directional Hit Reactions To Phase1 Enemy Prefab**. `EnemyBrain` skips legacy `Hit` trigger when a presenter is present.

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

- New weapons must be authored through `GeisWeaponDefinition`; do not add new content by extending bridge legacy arrays.
- Any change to weapon slot ordering or input mapping must update both this doc and the player setup docs in `Assets/Documentation/` in the same PR.
- Combat bridge changes must preserve the `OnAttackPerformed(weaponIndex)` contract from `GeisPlayerAnimationController`.
- Soul realm interaction gates (`SoulRealmInteractable.BlockPhysicalInteractions`) must not be bypassed by switch/use logic.

## Guidelines

- Prefer a single `GeisWeaponDefinition` per slot over legacy arrays on `GeisCombatBridge` for new content.

## Related documentation

- Active guides: `Assets/Documentation/COMBAT_SYSTEM_IMPLEMENTATION_GUIDE.md`, `WEAPON_COLLIDER_SETUP.md`, etc.
- Historical references: `Assets/Documentation/Archive/COMBAT_SYSTEM_SUMMARY.md`

## Changelog

- **2026-05-19**: `CombatAttackInterruptController` on `CombatEntity` cancels attacks when damaged and blocks outbound hits until the swing ends; respects combo startup super armor (`GeisComboData`). Player implements `IAttackerPhaseProvider`.
- **2026-05-19**: Gamepad bow draw/release on RT (`HeavyAttack`); aim remains LT.
- **2026-05-18**: `GeisCombatBridge` falls back to `SimpleAttackHitDetector` when a weapon hitbox exists but the combo step has no authored hit timings; enemy runtime setup tags roots `Enemy` for melee filters.
- **2026-05-18**: Enemy attacks now lock to the chosen player target instead of re-resolving onto nearby enemies; documented player-only enemy perception rules.
- **2026-05-18**: Documented `WeaponHitbox` tag resolution against owning combat roots and per-hit effect fallback for authored combo actions.
- **2026-05-07**: Documented melee `SimpleAttackHitDetector` ignoring lock-on triggers for reach; probes default to non-trigger overlaps.
- **2026-04-01**: Filled behavior & contracts from code; Rules left for manual additions.
