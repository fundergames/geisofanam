# Documentation

This folder contains deep-dive docs that support implementation and testing.

For day-to-day feature work, start with `Assets/Docs/START_HERE.md` and only load docs from this folder when needed.

## Active operational guides (keep in this folder)

### Combat setup/testing
- **COMBAT_SYSTEM_IMPLEMENTATION_GUIDE.md** - Implementation guide with code examples
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

### Character/prefab setup
- **CHARACTER_PREFAB_ANALYSIS.md** - Character prefab analysis
- **MODULAR_CHARACTER_SYSTEM_GUIDE.md** - Modular character system guide
- **QUICK_START_GUIDE.md** - Quick start guide
- **QUICK_USAGE_EXAMPLES.md** - Quick usage examples
- **STEP_BY_STEP_SETUP.md** - Step-by-step setup guide
- **AUTO_SETUP_INSTRUCTIONS.md** - Auto-setup instructions
- **IMPLEMENTATION_SUMMARY.md** - Implementation summary

### Misc operational
- **AUTO_START_FEATURE_ADDED.md** - Auto-start feature documentation
- **LEVEL_BUTTON_INSTRUCTIONS.md** - Level button instructions
- **ThirdPersonCombatTestSceneSetup_README.md** - Third-person combat test scene setup
- **ABILITY_EFFECT_SETUP_PLAN.md** - Ability/effect setup planning notes

## Historical/archive docs

Historical snapshots and superseded planning/migration docs have been moved to:

- `Assets/Documentation/Archive/README.md`

These files should not be loaded by default during normal feature work.

## Pruned / superseded docs

The following docs were removed to reduce stale context load and duplicate migration state:
- `IMPLEMENTATION_STATUS.md`
- `MIGRATION_COMPLETE_SUMMARY.md`
- `COMBAT_SYSTEM_CLEANUP_ANALYSIS.md`
- `COMBAT_SYSTEM_MIGRATION.md`
- `ADAPTER_MIGRATION_ANALYSIS.md`
- `CLEANUP_SUMMARY.md`

## Retention policy (keep context lean)

- Prefer canonical docs in `Assets/Docs/*` for day-to-day implementation context.
- Keep only stable, currently useful setup/testing guides in this folder.
- Move historical snapshots to `Assets/Documentation/Archive/`.
- Remove planning/migration snapshots once superseded by final reports.
- If a file is kept for historical reasons, it should not duplicate current state tracking.

## Note

Some documentation files may remain in their original locations (e.g., in Packages or TEMP_REMOVE folders) as they are specific to those modules.
