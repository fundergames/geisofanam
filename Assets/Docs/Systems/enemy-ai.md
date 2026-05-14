# Enemy AI

**Status**: current  
**Last updated**: 2026-05-11

## Purpose

Reusable runtime foundation for hostile AI actors that can be dropped into Geis combat scenes, acquire a target, move into combat space, telegraph attacks, execute authored `CombatAction`s through the shared RogueDeal combat pipeline, and reset cleanly for iteration.

## Behavior & contracts

### `EnemyAiDefinition`

- ScriptableObject contract for one enemy archetype.
- Owns Phase 1 authoring data: display info, stats, weapon/combat profile, perception tuning, movement tuning, stagger settings, and attack entries.
- **Player-aligned weapon loadout (optional)**: assign **`weaponDefinition`** (`GeisWeaponDefinition`) for the same prefab + **`GeisComboData`** + RogueDeal **`Weapon`** / **`CombatAction`** stack the player uses via **`GeisWeaponSwitcher`**. When set, **`GetEffectiveWeaponStats()`** feeds **`CombatEntityData.equippedWeapon`** from the definition’s **`weaponStats`** and **`animatorEquippedWeaponSlotIndex`** drives Polygon **`EquippedWeaponIndex`** (0–3). Leave **`weaponDefinition`** null to keep using legacy **`equippedWeapon`** only.
- May optionally reference legacy `EnemyDefinition` data so realtime enemies can publish `EnemyDefeatedEvent` for systems that still listen to RogueDeal progression events.

### `EnemyCombatant`

- Root lifecycle owner for an enemy prefab.
- On `Awake`, assigns the **`Enemy` physics layer** to this object and all descendants so player melee (`SimpleAttackHitDetector` with `targetLayers` limited to Enemy) reliably overlaps hurtbox colliders. Tag `"Enemy"` alone is not sufficient.
- Applies numeric stats to `CombatEntity`, assigns `CombatProfile` and equipped **`Weapon`** (from **`GetEffectiveWeaponStats()`** — **`weaponDefinition`** when present, else **`equippedWeapon`**), caches spawn transform, and exposes `ResetCombatant()`.
- Marks defeat once `CombatEntityData.IsAlive` becomes false, disables active AI movement/attacks, and may publish optional legacy defeat events.
- Drives enemy defeat presentation through `EnemyVisual` when present.

### `EnemyAnimatorDriver`

- When **`EnemyCombatant`** applies an **`EnemyAiDefinition`**, **`ApplyAnimatorOverrideFromDefinition`** assigns the controller and — if **`weaponDefinition.comboData`** is set — builds the same **`AnimatorOverrideController` + `GeisComboPlaceholders`** clip swaps the player uses (`GeisPlayerAnimationController.ApplyComboOverridesIfReady`), so **`Attack`** + **`ComboStateBlend`** drive **weapon combo clips**, not empty placeholders. Optional serialized **`enemyComboPlaceholders`** on the driver; otherwise loads **`Resources/ComboPlaceholders/GeisComboPlaceholders`** (then **`GeisComboPlaceholders`**).
- Every frame (via `EnemyBrain`), writes locomotion and **brain-synced** Animator parameters so Animator transitions can follow the same states as AI logic.
- Parameters are **optional**: each serialized name is applied only if your Animator Controller defines a matching parameter (float/bool/int/trigger), so existing controllers keep working.
- **Triggers** still come from combat flow: `EnemyAttackDriver` fires per-attack telegraph/execute triggers (`EnemyAttackDefinition.telegraphTrigger`, `attackTriggerOverride`), and hit reactions use `TriggerHitReaction` (`Hit` by default). When using **`GeisWeaponDefinition`**, the driver also writes **`EquippedWeaponIndex`** and **`ComboStateBlend`** / **`ComboState`** before the attack trigger so Polygon-style combo clips can resolve like the player. Layer your Animator so **bool/int state** selects the locomotion / idle blending branch and **triggers** gate one-shot attack clips if you use that pattern.

**Suggested Animator parameters (names configurable on the component)**

| Role | Default parameter name | Type | When true / value |
|------|------------------------|------|-------------------|
| Move blend | `MoveSpeed` | Float | `EnemyMotor.CurrentNormalisedSpeed` (0–1) |
| Has aggro target | `HasTarget` | Bool | Perception has a target |
| Strafe reposition | `IsStrafing` | Bool | Brain is in `Strafe` |
| Wind-up | `IsTelegraphing` | Bool | Brain is in `Telegraph` |
| Strike window | `IsAttacking` | Bool | Brain is in `Attack` (execute phase) |
| Follow-through | `IsRecovering` | Bool | Brain is in `Recover` |
| Hit stun | `IsStaggering` | Bool | Brain is in `Stagger` |
| Defeat | `IsDead` | Bool | Brain is in `Dead` |
| Full state mirror | *(empty)* | Int | Set **Enemy State Parameter** on `EnemyAnimatorDriver` (e.g. `EnemyState`); value = `(int)EnemyBrain.EnemyState` — `Idle=0` … `Dead=8` |

**Enum order for optional Int `EnemyState`** (must match Animator transitions): `Idle=0`, `Acquire=1`, `Approach=2`, `Strafe=3`, `Telegraph=4`, `Attack=5`, `Recover=6`, `Stagger=7`, `Dead=8`.

- Assign a **Runtime Animator Controller** on `EnemyAiDefinition.animatorOverrideController` (or on the prefab Animator) so clips actually play; the bridge only supplies parameters.

**Reusing `AC_Polygon_Masculine_Geis`**

- Sensible when the enemy mesh uses the **same Polygon Masculine humanoid rig / Avatar** the controller was authored for (typical if you swap the Phase‑1 placeholder for that character model).
- `EnemyAI_Phase1Humanoid` references this controller by default so locomotion picks up **`MoveSpeed`**, **`IsStrafing`** (stored as a **float** 0/1 in this controller — the driver supports both bool and float), and **`IsGrounded`** (forced **true** while alive so grounded locomotion blends behave).
- Because Polygon gates locomotion on **player input bools**, `EnemyAnimatorDriver` also synthesizes **`MovementInputHeld`**, **`IsWalking`**, and **`IsStopped`** from **`EnemyBrain`** approach/strafe state and normalized **`MoveSpeed`** when **`MovementInputHeld`** exists on the controller (`drivePolygonStyleMovementIntent`, on by default).
- That animator **does not** define **`Telegraph`**, **`Hit`**, or the optional enemy-only **bool** layers (`HasTarget`, `IsTelegraphing`, …). Those writes are skipped unless you add matching parameters (recommended on a **duplicate** of the controller if you do not want to edit the shared player asset). `Attack` **is** a trigger on Polygon — execute phase lines up with `EnemyAttackDriver`.

### `EnemyBrain`

- High-level state machine for Phase 1.
- Canonical states: `Idle`, `Acquire`, `Approach`, `Strafe`, `Telegraph`, `Attack`, `Recover`, `Stagger`, `Dead`.
- **`Approach`** is gated on **`EnemyAiDefinition.GetMaxStrikeRange()` + slack** (not *only* preferred spacing + tolerance) so NavMesh agents that stall slightly outside the ideal ring still attempt **`TryStartAttack`** when inside authored melee reach.
- While **`EnemyAttackDriver`** is busy (Telegraph / Execute / Recover), **`EnemyMotor.FaceTarget`** still runs each frame so the swing tracks the player instead of freezing facing at the last **`Approach`** tick.
- Reads target data from `EnemyPerception`, movement ownership from `EnemyMotor`, and attack phase from `EnemyAttackDriver`.
- Keeps movement and attack decision-making separated so future coordination systems can influence spacing/attack turns without replacing combat execution.

### `EnemyPerception`

- Resolves the current hostile target, with the player as the default Phase 1 target.
- Supports aggro range, leash range, optional line-of-sight gating, and explicit target assignment for test setups.
- **`GetDistanceToCurrentTarget`** uses **planar (XZ) separation** so mild vertical offsets do not strand the enemy in **`Approach`** while failing melee **`maxRange`** checks.
- **`HasLineOfSightTo`** raycasts with **`RaycastAll`**, ignores hits on this enemy’s own **`transform.root`** colliders (capsule grazing / interior origins), then tests whether the first foreign hit is the target.
- Provides the current target transform/entity for AI and targeting strategy consumers.

### `EnemyMotor`

- NavMeshAgent-first locomotion owner for enemies.
- Keeps the enemy facing the target and moving toward a readable engagement distance.
- **Approach pacing**: while closing from outside preferred combat distance, scales **`NavMeshAgent.speed`** and **`LocomotionGaitIndex`** from **`EnemyMovementSettings`** — far excess distance uses **run** gait + higher speed multiplier; closer band uses **walk/jog** gait + lower multiplier. **`EnemyAnimatorDriver`** writes **`CurrentGait`** for Polygon controllers.
- Strafe uses its own speed multiplier and gait from the same settings block.
- Must fall back to direct transform movement when the agent is unavailable or the scene has no baked NavMesh, so test scenes remain playable.

### `EnemyWeaponEquipper`

- Optional component on the enemy root (assigned automatically if present on **`EnemyCombatant`**).
- When **`EnemyAiDefinition.weaponDefinition`** has a **`weaponPrefab`**, instantiates it under the resolved hand/socket (same naming fallbacks as **`GeisWeaponSwitcher`**). Omit the component if the mesh already includes a prop.

### `EnemyAttackDriver`

- Selects an authored attack from `EnemyAiDefinition` based on target distance and availability.
- Runs telegraph and recovery timing outside of `CombatExecutor`.
- **`EnemyAttackDefinition.actionSource`**: **`ExplicitCombatAction`** keeps using the serialized **`CombatAction`**. **`WeaponComboResolved`** resolves the strike from **`weaponDefinition.comboData`** at the enemy’s current combo index (same rule as **`GeisCombatBridge`** / **`GeisComboData.ResolveCombatAction`**); if that returns null (e.g. **`GeisWeaponDefinition.combatAction`** unset and no **`stateCombatBindings[state].combatActionOverride`** for that step), **`EnemyAttackDriver`** falls back to the attack entry’s **`action`** so **`SelectAttack`** / **`TryStartAttack`** still work. Combo still advances via **`TryGetNextState`** / **`comboAdvanceInput`** after recovery; chain resets when no transition applies or when the attack is cancelled / stagger-interrupted.
- Execute phase lasts at least **one frame** (`yield return null`) before **`CombatExecutor`** runs so **`EnemyBrain`** can observe **`EnemyState.Attack`** / **`IsAttacking`** even when the executor finishes synchronously (common when **`CombatAction.animationTrigger`** is empty).
- Executes the resolved action through `CombatExecutor.ExecuteAction(action)` so cooldowns, effects, and combat events stay aligned with the shared combat stack.

### `EnemyCurrentTargetingStrategy`

- Required targeting strategy for enemy-authored `CombatAction`s in this system.
- Uses the attacker's current `EnemyPerception` target instead of the generic nearest-non-self `CombatEntity` search used by legacy targeting strategies.
- Range vs **`Weapon.maxRange`** / profile uses **planar (XZ) distance** plus a small epsilon so execution matches perception-driven melee bands (pure **Y** separation no longer voids valid swings).
- If the attacker's **`CombatEntity.coneAngle`** is **&gt; 0**, the target must also lie inside that **forward cone** (same half-angle convention as **`ConeTargetingStrategy`**) or the action does not resolve — stops swings from committing when the player is already behind the arc.
- Prevents enemies from accidentally selecting one another in multi-enemy scenes.

### `EnemyMeleeFacingGate`

- At **strike time**, **`CombatExecutor`** and **`CombatActionDamageUtility`** skip applying damage from **`EnemyCombatant`** attackers when the defender is outside **`CombatEntity.coneAngle`**, using the attacker's **current** forward. Catches “snapshot target + long telegraph” cases if the player circles during wind-up.

### `EnemyCoordinationContext`

- Phase 1 passive coordination surface only.
- Stores `squadId`, combat role, current engaged target, and reservation/cooldown metadata for future group behaviors.
- Must not be required for single-enemy functionality; a lone enemy remains fully playable with no coordinator present.

### `EnemyEncounterController`

- Lightweight non-boss encounter bootstrap for enemy groups or test scenes.
- Tracks managed enemies, can reset them to spawn state, and optionally auto-loops the fight for iteration.
- Preferred for ordinary enemy test loops; `BossEncounterManager` remains boss-specific.

## Scope

- Owns: enemy AI perception/motor/brain/attack orchestration, target resolution for enemy actions, encounter reset loop for ordinary enemies.
- Reuses: `CombatEntity`, `CombatExecutor`, `CombatAction`, `BaseEffect`, `CombatEvents`, `EnemyVisual`, and `GeisObjectLockOn`.
- Does not own: player combat bridge, soul-realm-specific combat logic, boss phase controllers, or full squad-tactics arbitration in Phase 1.

## Architecture

- `EnemyCombatant` applies the authored definition to the root combat entity.
- `EnemyBrain` refreshes perception, selects movement intent, and requests attacks from `EnemyAttackDriver`.
- `EnemyAttackDriver` runs telegraph timing and delegates actual damage/effect execution to `CombatExecutor`.
- `EnemyCurrentTargetingStrategy` resolves the explicit AI target for the current action.
- `CombatEvents` continue to drive health-bar updates, damage numbers, hit reactions, and downstream integrations.

## Key types & assets

| Piece | Path |
|-------|------|
| Runtime root | `Assets/Geis/Scripts/Enemies/EnemyCombatant.cs` |
| Brain | `Assets/Geis/Scripts/Enemies/EnemyBrain.cs` |
| Perception | `Assets/Geis/Scripts/Enemies/EnemyPerception.cs` |
| Motor | `Assets/Geis/Scripts/Enemies/EnemyMotor.cs` |
| Attacks | `Assets/Geis/Scripts/Enemies/EnemyAttackDriver.cs` |
| Animator bridge | `Assets/Geis/Scripts/Enemies/EnemyAnimatorDriver.cs` |
| Definition | `Assets/Geis/Scripts/Enemies/EnemyAiDefinition.cs` |
| Targeting strategy | `Assets/Geis/Scripts/Enemies/EnemyCurrentTargetingStrategy.cs` |
| Encounter loop | `Assets/Geis/Scripts/Enemies/EnemyEncounterController.cs` |
| Builder | `Assets/Geis/Scripts/Enemies/Editor/EnemyPhase1ArenaBuilder.cs` |

## Integration

- Combat: attacks must reuse [combat.md](combat.md) via `CombatExecutor`, `CombatAction`, `CombatProfile`, `Weapon`, and `CombatEvents`.
- Lock-on: enemy prefabs expose `GeisObjectLockOn` and a target highlight mesh so the player's existing targeting flow works unchanged.
- Locomotion: enemy movement is self-owned and must not depend on `GeisPlayerAnimationController`; only player-facing systems stay in [locomotion.md](locomotion.md).
- Progression: legacy `EnemyDefeatedEvent` publishing is optional and should only happen when a compatible `EnemyDefinition` reference is assigned.

## Rules

- Enemy-authored attacks must use `EnemyCurrentTargetingStrategy` or another explicit-team targeting strategy; do not ship enemy prefabs on generic nearest-target assets.
- Keep attack intent and telegraph timing in `EnemyAttackDriver`; keep locomotion ownership in `EnemyMotor`.
- Single-enemy playability is the baseline. Coordination hooks must remain additive and optional in Phase 1.
- Test scenes must remain functional without a baked NavMesh by preserving the direct-movement fallback path.
- Any public behavior change to enemy prefab contracts, reset flow, or targeting ownership must update this doc in the same change.

## Guidelines

- Prefer ScriptableObject-authored tuning over hardcoded per-prefab numbers.
- Use `EnemyEncounterController` for ordinary enemy iteration loops instead of extending boss encounter code.
- Keep the first enemy readable rather than complex: one or two melee attacks with clear timing beats are better than broad but noisy behavior.

## Related documentation

- `Assets/Docs/Features/enemy-ai-phase1-humanoid-melee.md`
- `Assets/Docs/Systems/combat.md`
- `Assets/Docs/Systems/locomotion.md`

## Changelog

- **2026-05-08**: Approach/strafe locomotion tiers (distance-based NavMesh speed + `CurrentGait` for Polygon).
- **2026-05-08**: Documented reusing `AC_Polygon_Masculine_Geis` for Phase‑1 humanoid AI (override assignment, Polygon `IsStrafing` float + grounded compat).
- **2026-05-08**: Documented `EnemyAnimatorDriver` brain-sync parameters (`IsAttacking`, `IsRecovering`, `IsStaggering`, optional Int brain state) and enum values for Animator authoring.
- **2026-04-29**: Added the enemy AI system doc for Phase 1 humanoid melee enemies, including prefab contracts, targeting ownership, and encounter loop rules.
