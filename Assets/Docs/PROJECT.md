# Geis of Annam — Project Overview

## Identity

- **Product name**: Geis of Anam
- **Product ID**: com.funder.games.geisofanam
- **Company**: Funder Games

## Tech Stack

- **Engine**: Unity 6 (URP)
- **Core framework**: [Funder Core](https://github.com/fundergames/funder-core) (Package Manager)
- **Game systems**: RogueDeal (quests, NPCs, card combat, modular character system)
- **Rendering**: Universal Render Pipeline (URP)

## Project Pillars

1. **Readable dual-realm play**
   - Physical and soul-realm interactions must be visually and mechanically distinct.
   - New mechanics should make realm switching meaningful, not cosmetic.

2. **Third-person action clarity**
   - Combat timing, telegraphs, lock-on behavior, and hit feedback should be predictable and legible.
   - Input mappings must preserve keyboard/gamepad parity for core actions.

3. **Composable content pipeline**
   - Features should plug into existing systems (weapon definitions, puzzle base classes, feature docs) instead of bespoke one-offs.
   - Prefer data-driven setup through ScriptableObjects and feature/system docs.

4. **Low-context agent execution**
   - Routine feature work should be possible by reading `START_HERE.md`, one feature file, and only relevant system docs.
   - Behavior changes must update the corresponding system doc and feature state.

## Naming Conventions

**Enemy:**
- Prefab: `P_Enemy_<Name>` (e.g. P_Enemy_ForestGuardian)
- Model: `M_Enemy_<Name>` (e.g. M_Enemy_ForestGuardian)
- Animations: `<Name>_<Action>` (e.g. ForestGuardian_Idle, ForestGuardian_Walk)

**Character:**
- Prefab: `P_Character_<Name>`
- Model: `M_Character_<Name>`
- Animations: `<Name>_<Action>`

**Weapon:**
- Prefab: `P_Weapon_<Name>`
- Model: `M_Weapon_<Name>`

**Environment:**
- Prefab: `P_Env_<Name>` or `P_Prop_<Name>`
- Model: `M_Env_<Name>` or `M_Prop_<Name>`

**Folders:**
```
Assets/
  Art/
    Enemies/<Name>/
    Characters/<Name>/
    Weapons/<Name>/
    Environment/
  _Generated/
    Staging/        # Initial Meshy output; validate before promote
  Prefabs/
    Enemies/
    Characters/
    Weapons/
  Animation/
    Enemies/
    Characters/
```

## Unity Integration Contract Rules

- **Pivot**: Feet center (for characters/enemies); logical center for props
- **Scale**: 1 Unity unit = 1 meter
- **Collider types**: Capsule for characters/enemies; Box for weapons; Mesh for complex environment
- **Animator Controller**: Required for animated assets; path in Integration Contract
- **Prefab requirements**: All required components from Architect Spec; no null references
- **Component requirements**: Per Architect Spec; typically Animator, Collider, NavMeshAgent (enemies)

## Folder Conventions

| Path | Purpose |
|------|---------|
| `Assets/Geis/` | Geis combat system and migrated RogueDeal scripts |
| `Assets/Documentation/` | Combat, character, and setup documentation |
| `Assets/Docs/` | Agent coordination, project specs, feature files |
| `Assets/Docs/Agents/` | Role agent definitions (Design, Modeler3D, Rigger, etc.) |
| `Assets/Docs/Features/` | Feature specs and handoff documents |
| `Assets/Docs/Systems/` | One `.md` per gameplay/engine system (behavior, integration, rules) |
| `Assets/Editor/` | Editor scripts and tools |
| `Assets/Art/` | Final validated art assets (promote from _Generated/Staging) |
| `Assets/_Generated/Staging/` | Meshy output; validate before promote |
| `RPGTinyHeroWavePBR/` | Reference RPG character assets |

## Agent Coordination

When working on features, use the multi-agent system:

1. Read `Assets/Docs/START_HERE.md` first, then this file and `Assets/Docs/AGENTS.md`.
2. Create or update feature files in `Assets/Docs/Features/`.
3. Follow the handoff flow: Design → Architect → Modeler → Rigger → Animator → Engineer → QA.
4. Reference `Assets/Docs/VisualStyleGuide.md` for art direction.
