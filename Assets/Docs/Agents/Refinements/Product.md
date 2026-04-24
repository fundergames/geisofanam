# Product Role Refinements

Improvements, learnings, and practice updates for the Product role. Add entries when you discover better approaches, edge cases, or clarifications.

## Format

Each entry: `- **[YYYY-MM-DD]**: Brief description. Details.`

---

- **[2026-04-23]**: Keep acceptance criteria objectively testable. Write outcomes with observable pass/fail conditions (scene behavior, status transitions, measurable constraints), not intent-only wording.
- **[2026-04-23]**: Declare scope boundaries explicitly. Every non-trivial feature should have an out-of-scope list to reduce downstream assumption drift.
- **[2026-04-23]**: Require ownership/state updates on feature changes. Product-level changes that alter delivery sequence must update `status`, `current_owner`, `next_owner`, and registry metadata.
