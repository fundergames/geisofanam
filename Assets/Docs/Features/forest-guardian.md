---
title: "Enemy: Forest Guardian"
status: concept
current_owner: Design
next_owner: Architect
mode: exploration
concept_locked: false
selected_variant: null

approvals:
  design: pending
  architect: pending
  modeling: pending
  engineering: pending
  qa: pending

blocking_issues: []
assumptions: []
risks: []

version: 1
last_updated_by: system
last_updated_at: null
change_summary: "Initial structure upgrade"
---

# Forest Guardian Enemy

## Structured Spec (Machine Readable)

```yaml
asset_type: enemy
rig_type: generic
poly_budget_target: 12000
texture_budget: 1024

required_animations:
  - idle
  - walk
  - attack_light
  - hit_react
  - death

engine_requirements:
  collider: capsule
  navmesh_agent: true
  root_motion: false

qa_requirements:
  combat_test: true
  performance_test: true
```

## Design Brief (Design/Narrative)
Nature spirit that guards sacred groves. Encountered in woodland areas. Non-humanoid silhouette—organic, plant-fused form. Combat: melee roots/vines, area denial. Narrative: protector of ancient trees; can be reasoned with if player demonstrates respect for nature.

## Architect Spec (Architect)
- **Technical approach**: 
- **Component hierarchy**: 
- **Integration points**: 
- **Handoff to Modeler**: [ ] Complete

## Model Spec (3D Modeler)
- **Refined description**: 
- **Style adherence**: 
- **Output**: Path to generated model once done
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
- Non-humanoid rig may require custom animation pipeline
- Area denial mechanics need Engineer spec

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
- [ ] Animations connected
- [ ] Prefab functional
- [ ] QA passed

## QA Checklist
- [ ] Model in game
- [ ] Rig works
- [ ] Animations play
- [ ] Colliders / combat ready
- [ ] Other: _________
