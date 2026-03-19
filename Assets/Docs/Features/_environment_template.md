---
title: "[Environment: Feature Name]"
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

# [Environment Asset Name]

## Structured Spec (Machine Readable)

```yaml
asset_type: environment
rig_type: none
poly_budget_target: 10000
texture_budget: 1024

lod_requirements:
  levels: 2
  lod0_distance: 20
  lod1_distance: 50
  cull_distance: 100

performance_constraints:
  draw_calls: 1
  static_batching: true
  occlusion_culling: true

engine_requirements:
  collider: mesh  # or box, sphere
  navmesh_cutout: false
  lightmap_uv: true

qa_requirements:
  performance_test: true
  visual_test: true
```

## Design Brief (Design/Narrative)
*Region, biome, purpose, narrative context, design goals.*

## Architect Spec (Architect)
- **Technical approach**: 
- **Component hierarchy**: 
- **LOD strategy**: 
- **Integration points**: 
- **Handoff to Modeler**: [ ] Complete

## Model Spec (3D Modeler)
- **Refined description**: 
- **Style adherence**: 
- **Output**: Path to generated model
- **Handoff to Engineer**: [ ] Complete (no rig for env)

## Rig Spec (Rigger)
- N/A for static environment assets

## Animation Spec (Animator)
- N/A for static environment assets (or specify if animated)

## Integration (Engineer)
- Prefab path:
- Components needed:
- LOD group:
- Scene placement:
- **Handoff to QA**: [ ] Complete

## Reference Sources
[List concept images, region refs, biome refs]

## Risks / Assumptions
- 

## Blockers
- 

## Integration Contract
- Model Path:
- Prefab Path:
- Scale Standard:
- Pivot:
- LOD Setup:
- Collision:

## Definition of Done
- [ ] Design approved
- [ ] Model imported and validated
- [ ] LOD levels configured
- [ ] Prefab functional
- [ ] Performance within budget
- [ ] QA passed

## QA Checklist
- [ ] Model in game
- [ ] LOD transitions correct
- [ ] Performance acceptable
- [ ] Other: _________
