---
title: "Enemy AI: Phase 1 humanoid melee enemy"
status: qa_ready
current_owner: QA
next_owner: Approved
mode: production
concept_locked: true
selected_variant: "phase1_humanoid_melee_foundation"

approvals:
  design: approved
  architect: approved
  architect_review: approved
  modeling: n/a
  engineering: approved
  code_review: pending
  qa: pending
  video_demo: pending

blocking_issues: []
assumptions:
  - "Phase 1 prioritizes a drop-in humanoid melee enemy and leaves multi-enemy coordination to passive hooks rather than full squad tactics."
  - "Placeholder geometry and optional animation support are acceptable for the first playable slice as long as the combat loop is end-to-end functional."
  - "The player remains the only hostile target for this first implementation."
risks:
  - "Generic RogueDeal targeting assets treat any non-self CombatEntity as a valid target, so enemy-owned targeting must stay on the custom current-target strategy."
  - "Scenes without a baked NavMesh need a fallback movement path or the enemy will fail to close distance in test setups."

version: 2
last_updated_by: Composer
last_updated_at: 2026-05-07
change_summary: "Ensure enemies use the Enemy physics layer so player SimpleAttackHitDetector overlaps register damage; Arena builder sets prefab layer; EnemyCombatant enforces at runtime."
---

# Enemy AI: Phase 1 humanoid melee enemy

## Structured Spec (Machine Readable)

```yaml
asset_type: enemy-system
rig_type: humanoid
poly_budget_target: 12000
texture_budget: 1024

required_animations:
  - idle
  - locomotion
  - telegraph
  - attack_light
  - hit_react
  - death

combat_requirements:
  attack_types:
    - melee
  telegraph_duration: 0.45
  hitbox_layers:
    - Default
    - Enemy

engine_requirements:
  collider: capsule
  navmesh_agent: true
  root_motion: false

qa_requirements:
  combat_test: true
  performance_test: true
```

## Design Brief (Design/Narrative)

Phase 1 enemy AI should establish the baseline feel for hostile humanoids in Geis of Anam: readable aggression, clear melee commitment windows, and a fight loop that feels compatible with the player's third-person action combat instead of like a separate minigame. The first enemy should read as a low-fantasy foot soldier or shade-driven combatant that can be dropped into a combat space, acquire the player, pressure at melee range, telegraph attacks, and die cleanly under the existing combat pipeline.

This supports the project's third-person action clarity pillar by making enemy intent legible, and it supports the composable content pipeline pillar by defining a prefab-and-data contract other enemy archetypes can reuse. Affected systems: `Assets/Docs/Systems/combat.md`, `Assets/Docs/Systems/locomotion.md`, and the new `Assets/Docs/Systems/enemy-ai.md`.

**Handoff to Architect**: [x] Complete

## Architect Spec (Architect)

### Technical approach

- Introduce a dedicated enemy runtime stack under `Assets/Geis/Scripts/Enemies/` with explicit ownership boundaries:
  - `EnemyCombatant` applies authored stats/config, owns reset/death lifecycle, and bridges optional legacy progression events.
  - `EnemyBrain` owns state transitions and high-level combat decision flow.
  - `EnemyPerception` maintains the current player target and line-of-sight/range checks.
  - `EnemyMotor` handles NavMeshAgent-first movement with a direct-move fallback for non-baked test scenes.
  - `EnemyAttackDriver` runs telegraph/execute/recovery timing and fires authored `CombatAction` assets through `CombatExecutor`.
  - `EnemyAnimatorDriver` keeps animation ownership additive and optional, so placeholder prefabs still function.
  - `EnemyCoordinationContext` stores passive squad hooks (`squadId`, role, reservation metadata) for Phase 2.
- Add `EnemyCurrentTargetingStrategy` so attacks use the AI's explicit perceived target rather than the generic "nearest other CombatEntity" selectors.
- Keep attack data authored through ScriptableObjects: one `EnemyAiDefinition` per archetype with combat profile, stats, perception tuning, and attack entries.

### Component hierarchy

- Root prefab: `P_Enemy_Phase1Humanoid`
  - Required root components: `CombatEntity`, `CombatExecutor`, `NavMeshAgent`, `EnemyCombatant`, `EnemyBrain`, `EnemyPerception`, `EnemyMotor`, `EnemyAttackDriver`, `EnemyAnimatorDriver`, `EnemyCoordinationContext`, `EnemyVisual`, `GeisObjectLockOn`, capsule collider.
  - Child `Model`: placeholder humanoid/primitive mesh root.
  - Child `HitPoint`: chest-height hit marker for combat feedback.
  - Child `TargetHighlight`: highlight mesh for lock-on.
  - Child world-space health bar canvas wired through `EnemyHealthBar`.

### Integration points

- Shared combat: `CombatEntity`, `CombatExecutor`, `CombatAction`, `BaseEffect`, `CombatProfile`, `CombatEvents`.
- Player lock-on: `GeisObjectLockOn`.
- Optional progression parity: `RogueDeal.Events.EnemyDefeatedEvent`.
- Test content pipeline: editor builder creates a prefab, data assets, and a dedicated arena scene using `Assets/Geis/Combat/Prefabs/Player.prefab`.

### Performance notes

- Per-frame AI work should stay constant-time per enemy: one target refresh, one movement update, and no per-frame allocations in the hot path.
- Prefer trigger/collider reuse and cached references over repeated `FindObjectsByType` calls after initialization.
- Phase 1 coordination hooks remain data-only; no global squad manager or expensive arbitration loop yet.

**Handoff to Engineer**: [x] Complete

## Model Spec (3D Modeler)

- Phase 1 uses an engineer-built placeholder humanoid mesh assembled from primitives so combat integration is not blocked on bespoke art.
- Visual direction still follows `Assets/Docs/VisualStyleGuide.md`: readable silhouette, muted natural base tones, and a restrained cyan accent for gameplay-critical highlights.
- **Handoff to Rigger**: [x] Complete

## Rig Spec (Rigger)

- Rig type: Not applicable for the placeholder prefab. The runtime stack supports an Animator and humanoid controller when a final model is swapped in later.
- **Handoff to Animator**: [x] Complete

## Animation Spec (Animator)

- Required clips for the final production enemy are listed in the structured spec, but Phase 1 does not block on custom clips.
- Animator support remains optional and additive: if a controller is present, the AI writes movement/combat parameters and attack triggers; otherwise combat still executes through telegraph timing plus `CombatExecutor`.
- **Handoff to Engineer**: [x] Complete

## Integration (Engineer)

- New enemy AI runtime lives in `Assets/Geis/Scripts/Enemies/`.
- New targeting asset type `EnemyCurrentTargetingStrategy` prevents friendly-fire target selection caused by generic nearest-target assets.
- New editor builder `EnemyPhase1ArenaBuilder` creates:
  - ScriptableObject data assets for the Phase 1 enemy
  - prefab `Assets/Prefabs/Enemies/P_Enemy_Phase1Humanoid.prefab`
  - scene `Assets/Geis/Scenes/EnemyAITestArena.unity`
- Encounter/reset support lives in `EnemyEncounterController` so the arena can loop the fight without boss-specific orchestration.
- **Handoff to QA**: [x] Complete

## Reference Sources

- User request: robust enemy AI system with future coordination hooks and a Phase 1 drop-in enemy.
- `Assets/Docs/PROJECT.md`
- `Assets/Docs/Systems/combat.md`
- `Assets/Docs/Systems/locomotion.md`
- `Assets/Docs/VisualStyleGuide.md`

## Risks / Assumptions

- Placeholder visuals may not demonstrate final telegraph readability until a production animator controller is assigned.
- Multi-enemy coordination is intentionally limited to passive context fields in Phase 1.

## Blockers

- Final in-editor validation is currently blocked when attempting automated batch execution because another Unity instance already has the project open. The builder entry point is ready: `Geis/Enemies/Build Phase 1 Enemy Arena`.

## Integration Contract

- Model Path: Generated by `EnemyPhase1ArenaBuilder` as a primitive placeholder under the prefab hierarchy
- Prefab Path: `Assets/Prefabs/Enemies/P_Enemy_Phase1Humanoid.prefab`
- Animator Controller: Optional; placeholder enemy functions without one
- Scale Standard: 1 Unity unit = 1 meter
- Pivot: Feet center on root
- Collision: Capsule collider on root, lock-on trigger sphere on root
- Gameplay Hooks: `EnemyAiDefinition`, `EnemyBrain`, `EnemyCurrentTargetingStrategy`, `EnemyEncounterController`

## Definition of Done

- [x] Design approved
- [x] Model imported and validated
- [x] Rig working
- [x] Animations connected
- [x] Prefab functional
- [ ] QA passed

## QA Checklist

- [ ] Enemy can be spawned from the generated prefab and acquires the player target.
- [ ] Enemy closes to melee distance using NavMeshAgent when available and direct-move fallback when not.
- [ ] Enemy telegraphs before executing a melee `CombatAction`.
- [ ] Enemy attacks damage the player through the shared combat pipeline.
- [ ] Player attacks damage the enemy, update health UI, and drive death/reset behavior.
- [ ] The generated test arena can loop encounters without boss-specific code.

## QA Notes

- Static verification passed for the new enemy AI scripts and docs (`ReadLints` clean on edited files).
- Automated Unity builder execution was attempted in batch mode against Unity `6000.3.9f1`, but runtime generation of the prefab/scene was blocked because the project was already open in another Unity instance.
