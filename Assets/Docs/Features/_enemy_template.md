---
title: "[Enemy: Feature Name]"
status: concept
current_owner: Design
next_owner: Architect
mode: exploration
concept_locked: false
selected_variant: null

approvals:
  design: pending
  architect: pending
  architect_review: pending
  modeling: pending
  engineering: pending
  code_review: pending
  qa: pending
  video_demo: pending

blocking_issues: []
assumptions: []
risks: []

version: 1
last_updated_by: system
last_updated_at: null
change_summary: "Initial structure"
---

# [Enemy Name]

## Structured Spec (Machine Readable)

```yaml
asset_type: enemy
rig_type: generic  # or humanoid
poly_budget_target: 12000
texture_budget: 1024

required_animations:
  - idle
  - walk
  - attack_light
  - attack_heavy
  - hit_react
  - death

combat_requirements:
  attack_types: []
  telegraph_duration: 0
  hitbox_layers: []

engine_requirements:
  collider: capsule
  navmesh_agent: true
  root_motion: false

qa_requirements:
  combat_test: true
  performance_test: true
```

## Design Brief (Design/Narrative)
*World-building context, narrative role, encounter intent, combat behavior, design goals.*

## Architect Spec (Architect)
- **Technical approach**: 
- **Component hierarchy**: 
- **Integration points**: 
- **Handoff to Modeler**: [ ] Complete

## Model Spec (3D Modeler)
- **Refined description**: 
- **Style adherence**: 
- **Output**: Path to generated model
- **Handoff to Rigger**: [ ] Complete

## Rig Spec (Rigger)
- **Rig type**: Humanoid / Generic
- **Handoff to Animator**: [ ] Complete

## Animation Spec (Animator)
- Required clips:
- Required parameters:
- **Handoff to Engineer**: [ ] Complete

## Integration (Engineer)
- Prefab path:
- Components needed:
- Scene placement:
- **Handoff to QA**: [ ] Complete

## Reference Sources
[List concept images, faction refs, region refs]

## Risks / Assumptions
- 

## Blockers
- 

## Integration Contract
- Model Path:
- Prefab Path:
- Animator Controller:
- Scale Standard:
- Pivot:
- Collision:
- Gameplay Hooks:

## Definition of Done
- [ ] Design approved
- [ ] Model imported and validated
- [ ] Rig working
- [ ] Animations connected (idle, walk, attack, hit_react, death)
- [ ] Combat behavior functional
- [ ] Prefab functional
- [ ] QA passed

## QA Checklist
- [ ] Model in game
- [ ] Rig works
- [ ] Animations play
- [ ] Colliders / combat ready
- [ ] Other: _________
