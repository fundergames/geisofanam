---
title: "[Weapon: Feature Name]"
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

# [Weapon Name]

## Structured Spec (Machine Readable)

```yaml
asset_type: weapon
rig_type: none  # or bone_attachment
poly_budget_target: 3000
texture_budget: 512

attack_behavior:
  attack_types: []
  vfx_hooks: []
  sfx_hooks: []
  trail_required: false

engine_requirements:
  attachment_point: right_hand
  collider: box
  physics: false

qa_requirements:
  combat_test: true
  performance_test: true
```

## Design Brief (Design/Narrative)
*Weapon type, attack style, faction alignment, visual identity, design goals.*

## Architect Spec (Architect)
- **Technical approach**: 
- **Component hierarchy**: 
- **Integration points**: 
- **Handoff to Modeler**: [ ] Complete

## Model Spec (3D Modeler)
- **Refined description**: 
- **Style adherence**: 
- **Output**: Path to generated model
- **Handoff to Rigger**: [ ] Complete (if rigged) or Engineer (if static)

## Rig Spec (Rigger)
- **Rig type**: None / Bone attachment
- **Handoff to Engineer**: [ ] Complete

## Animation Spec (Animator)
- N/A for static weapons; or required clips for animated weapons
- **Handoff to Engineer**: [ ] Complete

## Integration (Engineer)
- Prefab path:
- Components needed:
- VFX hooks:
- Scene placement:
- **Handoff to QA**: [ ] Complete

## Reference Sources
[List concept images, faction refs, weapon refs]

## Risks / Assumptions
- 

## Blockers
- 

## Integration Contract
- Model Path:
- Prefab Path:
- Attachment Point:
- Scale Standard:
- Pivot:
- VFX/SFX Hooks:
- Gameplay Hooks:

## Definition of Done
- [ ] Design approved
- [ ] Model imported and validated
- [ ] Attack behavior defined
- [ ] VFX/SFX hooks implemented
- [ ] Prefab functional
- [ ] QA passed

## QA Checklist
- [ ] Model in game
- [ ] Attack animations/behavior work
- [ ] VFX/SFX trigger correctly
- [ ] Other: _________
