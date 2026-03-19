---
title: "[Feature name]"
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
change_summary: "Initial structure upgrade"
---

# [Feature Name]

## Structured Spec (Machine Readable)

```yaml
asset_type: generic
rig_type: generic
poly_budget_target: 10000
texture_budget: 1024

required_animations: []

engine_requirements:
  collider: capsule
  navmesh_agent: false
  root_motion: false

qa_requirements:
  combat_test: false
  performance_test: true
```

## Design Brief (Design/Narrative)
*World-building context, narrative role, encounter intent, design goals.*

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
- [ ] Animations connected
- [ ] Prefab functional
- [ ] QA passed

## QA Checklist
- [ ] Model in game
- [ ] Rig works
- [ ] Animations play
- [ ] Colliders / combat ready
- [ ] Other: _________
