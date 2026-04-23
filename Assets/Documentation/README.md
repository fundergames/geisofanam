# Documentation

This folder contains all documentation files for the project.

## Combat System Documentation

### Migration & Cleanup
- **FINAL_MIGRATION_REPORT.md** - Final migration completion report
- **FINAL_CLEANUP_REPORT.md** - Final cleanup completion report

### System Documentation
- **COMBAT_SYSTEM_PROPOSAL.md** - Original combat system architecture proposal
- **COMBAT_SYSTEM_IMPLEMENTATION_GUIDE.md** - Implementation guide with code examples
- **COMBAT_SYSTEM_SUMMARY.md** - Executive summary of combat system
- **FUNCTIONALITY_LOSS_AND_BREAKING_CHANGES.md** - Breaking changes documentation

### Pruned / superseded docs

The following docs were removed to reduce stale context load and duplicate migration state:
- `IMPLEMENTATION_STATUS.md`
- `MIGRATION_COMPLETE_SUMMARY.md`
- `COMBAT_SYSTEM_CLEANUP_ANALYSIS.md`
- `COMBAT_SYSTEM_MIGRATION.md`
- `ADAPTER_MIGRATION_ANALYSIS.md`
- `CLEANUP_SUMMARY.md`

Use `FINAL_MIGRATION_REPORT.md` and `FINAL_CLEANUP_REPORT.md` as the migration/cleanup completion references.

## Retention policy (keep context lean)

- Prefer canonical docs in `Assets/Docs/*` for day-to-day implementation context.
- Keep only stable, currently useful deep-dive guides in this folder.
- Remove planning/migration snapshots once superseded by final reports.
- If a file is kept for historical reasons, it should not duplicate current state tracking.

### Combat System Setup Guides
- **TARGETING_SYSTEM_SETUP.md** - Targeting system setup guide
- **TARGETING_SYSTEM_TESTING.md** - Targeting system testing guide
- **ANIMATION_EVENTS_SETUP.md** - Animation events setup guide
- **ANIMATOR_CONTROLLER_ANALYSIS.md** - Animator controller analysis
- **THIRD_PERSON_ANIMATOR_SETUP.md** - Third-person animator setup
- **TIMELINE_COMBO_SETUP.md** - Timeline combo setup guide
- **WEAPON_COLLIDER_SETUP.md** - Weapon collider setup guide
- **WALK_RUN_SETUP_GUIDE.md** - Walk/run setup guide
- **README_PRESENTATION_TESTING.md** - Presentation layer testing
- **README_REAL_SCENARIO_TESTING.md** - Real scenario testing
- **README_TESTING.md** - General testing guide

## Character & Prefab Documentation

- **CHARACTER_PREFAB_ANALYSIS.md** - Character prefab analysis
- **MODULAR_CHARACTER_SYSTEM_GUIDE.md** - Modular character system guide
- **QUICK_START_GUIDE.md** - Quick start guide
- **QUICK_USAGE_EXAMPLES.md** - Quick usage examples
- **STEP_BY_STEP_SETUP.md** - Step-by-step setup guide
- **AUTO_SETUP_INSTRUCTIONS.md** - Auto-setup instructions
- **IMPLEMENTATION_SUMMARY.md** - Implementation summary

## UI & Other Documentation

- **AUTO_START_FEATURE_ADDED.md** - Auto-start feature documentation
- **LEVEL_BUTTON_INSTRUCTIONS.md** - Level button instructions
- **ThirdPersonCombatTestSceneSetup_README.md** - Third-person combat test scene setup
- **FUNDER_CORE_REORGANIZATION.md** - Core reorganization documentation
- **MENU_STRUCTURE_REFERENCE.md** - Menu structure reference
- **PACKAGE_MIGRATION_CHECKLIST.md** - Package migration checklist

## Note

Some documentation files may remain in their original locations (e.g., in Packages or TEMP_REMOVE folders) as they are specific to those modules.
