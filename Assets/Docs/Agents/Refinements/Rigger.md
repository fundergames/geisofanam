# Rigger Role Refinements

Improvements, learnings, and practice updates for the Rigger role.

## Format

Each entry: `- **[YYYY-MM-DD]**: Brief description. Details.`

---

- **[2026-04-23]**: Enforce rig-type consistency from Architect Spec. If Architect specifies Humanoid, do not ship Generic without updating Architect Spec and handoff notes; mismatched rig type causes downstream animation/controller drift.
- **[2026-04-23]**: Validate scale and root alignment before handoff. Confirm 1 unit = 1 meter and root/pivot assumptions before Animator receives assets to avoid repeated retarget and root-motion corrections.
- **[2026-04-23]**: Record non-standard bone mapping explicitly. Any custom or partial mapping should be written in the Rig Spec so Animator/Engineer can wire controllers without reverse-engineering.
