# System documentation

This folder holds **one markdown file per gameplay or engine system**: how it works, where the code lives, integration points, and **rules** (do/don’t, ordering, naming) that should stay stable across features.

## Relationship to other docs

| Location | Purpose |
|----------|---------|
| `Assets/Docs/Systems/*.md` | **Living** system reference — update when behavior or contracts change |
| `Assets/Docs/Features/*.md` | Per-feature specs, handoffs, lifecycle |
| `Assets/Documentation/*.md` | Setup guides, migration notes, deep dives (may be historical) |

If a topic is system-wide and still current, prefer summarizing it here and linking to long-form docs under `Assets/Documentation/` when useful.

## Conventions

- **Filename**: `kebab-case.md` matching the system name (e.g. `soul-realm.md`, `input.md`).
- **When to update**: Any PR that changes public behavior, adds a new rule, or changes integration with other systems should touch the relevant system doc (even a short changelog bullet).
- **Behavior & contracts**: Factual description of how the code behaves (update when implementation changes).
- **Rules section**: Optional; add when you want explicit policies for reviews or agents. Until then, **Rules** stays a placeholder in each system doc.

## Registry

| System | Doc | Primary code / assets |
|--------|-----|------------------------|
| Input | [input.md](input.md) | `Assets/Geis/Scripts/Input/` |
| Locomotion & camera | [locomotion.md](locomotion.md) | `Assets/Geis/Scripts/Locomotion/` |
| Combat (weapons, bridge) | [combat.md](combat.md) | `Assets/Geis/Scripts/Combat/` |
| Soul realm & weapon abilities | [soul-realm.md](soul-realm.md) | `Assets/Geis/Scripts/SoulRealm/` |
| Puzzles | [puzzles.md](puzzles.md) | `Assets/Geis/Scripts/Puzzles/` |

Add a row and a new file when introducing a major system.

## New system doc

Copy `_template.md`, rename, fill sections, and add a row to the table above.
