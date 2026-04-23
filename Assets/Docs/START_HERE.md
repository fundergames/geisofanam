# Geis of Annam - Minimal Context Entry Point

Use this file as the first read for any agent task. It is designed to minimize token usage while preserving implementation quality.

## 1) Fast context profiles

### Profile A - Existing feature iteration (default)
Read only:
1. `Assets/Docs/START_HERE.md` (this file)
2. Target feature file in `Assets/Docs/Features/<slug>.md`
3. Only the system docs explicitly referenced by that feature file

Do not load broad historical docs unless blocked.

### Profile B - New feature planning
Read only:
1. `Assets/Docs/START_HERE.md`
2. `Assets/Docs/PROJECT.md`
3. `Assets/Docs/Systems/README.md`
4. The 1-3 relevant system docs from the index below
5. One feature template from `Assets/Docs/Features/`

### Profile C - Cross-system refactor
Read only:
1. `Assets/Docs/START_HERE.md`
2. `Assets/Docs/Systems/README.md`
3. The affected system docs only
4. Relevant in-flight feature docs

## 2) System index (authoritative docs)

| System | Doc | Primary code path |
|---|---|---|
| Input | `Assets/Docs/Systems/input.md` | `Assets/Geis/Scripts/Input/` |
| Locomotion & camera | `Assets/Docs/Systems/locomotion.md` | `Assets/Geis/Scripts/Locomotion/` |
| Combat | `Assets/Docs/Systems/combat.md` | `Assets/Geis/Scripts/Combat/` |
| Soul realm & abilities | `Assets/Docs/Systems/soul-realm.md` | `Assets/Geis/Scripts/SoulRealm/` |
| Puzzles | `Assets/Docs/Systems/puzzles.md` | `Assets/Geis/Scripts/Puzzles/` |

If a system is missing from this table, add a new `Assets/Docs/Systems/<name>.md` and register it.

## 3) Feature tracking

- Feature source of truth: `Assets/Docs/Features/*.md`
- Fast registry: `Assets/Docs/FeatureRegistry.json`
- Ownership and handoff rules: `Assets/Docs/AGENTS.md`

When iterating a feature, always trust the feature file state (`status`, `current_owner`, `next_owner`) over chat memory.

## 4) Context boundaries (important)

Default behavior:
- Prefer `Assets/Docs/*` over `Assets/Documentation/*`.
- Treat `Assets/Documentation/*` as deep-dive or historical unless explicitly required.
- Do not scan the whole repo docs set for normal feature work.

Escalate to broader docs only when:
- The system docs are missing required details
- A feature file references a specific setup/migration guide
- There is a contradiction that must be resolved

## 5) Doc hygiene rules

- Any PR changing behavior must update the relevant system doc.
- Any PR changing feature status/ownership must update the feature file and `FeatureRegistry.json`.
- Completed or abandoned historical docs should be removed when superseded to keep context lean.
