# Architect Role Refinements

Improvements, learnings, and practice updates for the Architect role. Add entries when you discover better approaches, edge cases, or clarifications.

## Format

Each entry: `- **[YYYY-MM-DD]**: Brief description. Details.`

---

- **[2026-04-23]**: Define integration contract fields explicitly. Architect specs must name prefab path conventions, required components, collider type, and owner system for each dependency to prevent Engineer guesswork.
- **[2026-04-23]**: Prefer additive architecture over silent replacement. When a new path supersedes a legacy path, note migration strategy and fallback behavior in the feature doc to reduce regressions.
- **[2026-04-23]**: Include performance constraints at spec time. Add target budgets (poly, draw-call, update frequency) and verify they are testable by QA rather than descriptive only.
