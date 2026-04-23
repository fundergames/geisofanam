# QA Role Refinements

Improvements, learnings, and practice updates for the QA role. Add entries when you discover better approaches, edge cases, or clarifications.

## Format

Each entry: `- **[YYYY-MM-DD]**: Brief description. Details.`

---

- **[2026-04-23]**: Prefer behavior validation over checklist-only pass/fail. Verify concrete in-game outcomes (state changes, transitions, timing windows) rather than only that a checklist item was touched.
- **[2026-04-23]**: Treat missing ownership/status updates as QA failures. If feature state (`status`, `current_owner`, `next_owner`) is stale or contradictory, block approval until docs reflect reality.
- **[2026-04-23]**: Reject ambiguous "works in scene" claims without a repro path. QA notes should include test setup, expected result, and observed result so the next agent can reproduce quickly.
