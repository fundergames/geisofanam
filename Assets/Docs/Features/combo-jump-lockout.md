---
title: "Gameplay: lock jump during melee combos"
status: qa_ready
current_owner: QA
next_owner: Approved
mode: production
concept_locked: true
selected_variant: "ignore_jump_input_during_attack_state"

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
  - "Combo lockout should apply to grounded melee combo execution (`AnimationState.Attack`) rather than globally disabling airborne/coyote jump rules."
  - "Bow aim/jump behavior is unchanged because the request targets combo attacks, not general weapon handling."
risks:
  - "Animator/controller variants that bypass `AnimationState.Attack` would need their own gate if they can still buffer jump input."

version: 1
last_updated_by: gpt-5.4
last_updated_at: 2026-04-27
change_summary: "Prevent jump input from buffering or firing while melee combo attacks are active, and document the locomotion rule."
---

# Gameplay: lock jump during melee combos

## Structured Spec (Machine Readable)

```yaml
asset_type: system-feature
rig_type: n/a
poly_budget_target: 0
texture_budget: 0

required_animations: []

engine_requirements:
  collider: n/a
  navmesh_agent: false
  root_motion: false

qa_requirements:
  combat_test: true
  performance_test: false
```

## Design Brief (Design/Narrative)

Player combat should preserve a committed melee feel: once a combo swing starts, jump input should not interrupt that commitment or queue an immediate jump payoff afterward. This supports the project's third-person action clarity pillar by keeping combo timing legible and by making jump timing a deliberate locomotion action instead of an accidental cancel path.

Affected systems: `Assets/Docs/Systems/locomotion.md`, `Assets/Docs/Systems/combat.md`, and the player input path consumed by `GeisPlayerAnimationController`.

**Handoff to Architect**: [x] Complete

## Architect Spec (Architect)

### Technical approach

- Enforce the lockout at the shared jump buffer/coyote hook in `GeisPlayerAnimationController` so all attack-state jump presses are ignored before they can be buffered.
- Clear any pending jump buffer when entering `AnimationState.Attack` to prevent a pre-existing buffered press from carrying into the combo state.

### Component hierarchy

- No prefab or scene hierarchy changes.
- Existing owner remains `GeisPlayerAnimationController` in `Assets/Geis/Scripts/Locomotion/`.

### Integration points

- Locomotion: `OnJumpInputBufferAndCoyote`, `EnterAttackState`
- Combat animation state: `AnimationState.Attack`
- Documentation: `Assets/Docs/Systems/locomotion.md`

### Performance notes

- No new allocations, assets, or per-frame systems.
- Change is constant-time state gating on an existing input callback.

**Handoff to Engineer**: [x] Complete

## Model Spec (3D Modeler)

- Not applicable: code-only locomotion/combat behavior change.
- **Handoff to Rigger**: [x] Complete

## Rig Spec (Rigger)

- Rig type: Not applicable.
- **Handoff to Animator**: [x] Complete

## Animation Spec (Animator)

- Required clips: None.
- Required parameters: Existing `AnimationState.Attack` ownership remains unchanged.
- **Handoff to Engineer**: [x] Complete

## Integration (Engineer)

- Code updated in `Assets/Geis/Scripts/Locomotion/GeisPlayerAnimationController.cs`.
- System documentation updated in `Assets/Docs/Systems/locomotion.md`.
- Behavior change: jump input is discarded during `AnimationState.Attack`; combo state no longer preserves a pending jump into post-combo locomotion/fall recovery.
- **Handoff to QA**: [x] Complete

## Reference Sources

- User request: "We shouldn't let the player jump during a combo."
- `Assets/Docs/PROJECT.md`
- `Assets/Docs/Systems/locomotion.md`

## Risks / Assumptions

- Assumes combo execution is represented by `AnimationState.Attack` for current melee implementations.

## Blockers

- QA still needs in-editor verification that jump is ignored throughout the combo chain and resumes immediately after returning to locomotion.

## Integration Contract

- Model Path: n/a
- Prefab Path: n/a
- Animator Controller: Existing player animator controller unchanged
- Scale Standard: Existing player scale unchanged
- Pivot: Existing player pivot unchanged
- Collision: Existing player collision unchanged
- Gameplay Hooks: `OnJumpInputBufferAndCoyote`, `EnterAttackState`

## Definition of Done

- [x] Design approved
- [x] Model imported and validated
- [x] Rig working
- [x] Animations connected
- [x] Prefab functional
- [ ] QA passed

## QA Checklist

- [ ] During grounded melee combos, pressing jump does not transition the player into `Jump`.
- [ ] During grounded melee combos, pressing jump does not queue a buffered jump that fires when the combo ends.
- [ ] After returning to locomotion, jump works normally on the next input.
- [ ] Existing coyote/buffer jump behavior still works outside combo attacks.
