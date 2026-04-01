# Geis of Annam — Project Overview

## Identity

- **Product name**: Geis of Anam
- **Product ID**: com.funder.games.geisofanam
- **Company**: Funder Games

## Tech Stack

- **Engine**: Unity 6 (URP)
- **Core framework**: [Funder Core](https://github.com/fundergames/funder-core) (Package Manager)
- **Game systems**: RogueDeal (hex levels, quests, NPCs, card combat, modular character system)
- **Rendering**: Universal Render Pipeline (URP)

## Project Pillars

*(To be refined — placeholder)*

1. **World**: Fantasy setting with distinct regions and factions
2. **Combat**: Third-person action with targeting, abilities, and modular equipment
3. **Progression**: Quest-driven narrative and character growth

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

1. Read this file and `Assets/Docs/AGENTS.md` for orchestration rules.
2. Create or update feature files in `Assets/Docs/Features/`.
3. Follow the handoff flow: Design → Architect → Modeler → Rigger → Animator → Engineer → QA.
4. Reference `Assets/Docs/VisualStyleGuide.md` for art direction.
