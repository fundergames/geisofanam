# Modeler3D Role Refinements

Improvements, learnings, and practice updates for the 3D Modeler role. Add entries when you discover better approaches, edge cases, or clarifications.

## Format

Each entry: `- **[YYYY-MM-DD]**: Brief description. Details.`

---

- **[2026-04-23]**: Always include scale and pivot checks in handoff notes. Model outputs should explicitly state 1 unit = 1 meter assumptions and pivot placement to reduce downstream rework.
- **[2026-04-23]**: Keep prompts and output paths deterministic. Use stable naming (`<slug>`) and record exact output path in feature docs so Engineer/QA do not need to search.
- **[2026-04-23]**: Record style compliance with concrete items, not generic claims. Validate poly budget band, texture budget, silhouette readability, and material workflow explicitly.
