# Geis of Annam — Agent Orchestration

This document instructs Cursor (and Unity Claude Code CLI) how to orchestrate multi-agent work for Geis of Annam. The AI assumes different roles based on the request and coordinates via feature files.

## Core Concept

When a user requests a feature (e.g., "Create a Forest Guardian enemy model"), the AI should:

1. **Detect roles** needed from the request (Design, Architect, 3D Modeler, Rigger, Animator, Engineer, QA, Product, UI/UX)
2. **Read** `START_HERE.md`, the target feature file, and only the minimum additional docs required for that role/task
3. **Perform** each role's work sequentially, updating the feature file at each step
4. **Hand off** by writing to the appropriate section and marking checkboxes

## Minimal Context Loading Policy

Default to the smallest useful context window.

### Existing feature iteration (default)

Read only:
1. `Assets/Docs/START_HERE.md`
2. Target feature file `Assets/Docs/Features/<slug>.md`
3. System docs explicitly required by the feature

### New feature planning

Read only:
1. `Assets/Docs/START_HERE.md`
2. `Assets/Docs/PROJECT.md`
3. `Assets/Docs/Systems/README.md`
4. The 1-3 system docs directly relevant to the request
5. One feature template under `Assets/Docs/Features/`

### Cross-system changes

Read only:
1. `Assets/Docs/START_HERE.md`
2. Affected system docs under `Assets/Docs/Systems/`
3. In-flight feature files impacted by the change

### Boundary rules

- Do **not** scan all markdown docs by default.
- Treat `Assets/Documentation/*.md` as deep-dive/historical unless a blocker requires them.
- Treat `Assets/Documentation/Archive/*.md` as archive-only; do not load unless explicitly requested for historical investigation.
- Expand context only when current docs are insufficient or contradictory.

## Role → Responsibility Mapping

| Role | When to invoke | Reads | Writes / Updates |
|------|----------------|-------|-------------------|
| **Design** | Narrative, world-building, encounter design | PROJECT.md | Design Brief section |
| **Architect** | architecture, system, structure, technical, component, prefab, integration | Design Brief, PROJECT.md | Architect Spec section |
| **3D Modeler** | model, mesh, 3D, Meshy, character, prop | Design Brief, Architect Spec, VisualStyleGuide | Model Spec, refined prompt, output path |
| **Rigger** | rig, skeleton, humanoid, bone | Model Spec | Rig Spec |
| **Animator** | animation, clip, controller, blend tree | Rig Spec | Animation Spec |
| **Engineer** | integration, prefab, scene, component | All specs | Integration section, Implementation Plan |
| **CodeReviewer** | code review, PR review | Feature file, code changes | Code review notes, code_review approval |
| **Tester** | test, verify, QA | Feature file | QA Checklist, test results |
| **QA** | qa, test, verify, bug | Feature file | QA Checklist |
| **Product** | product, scope, priority, feature | Feature files | Acceptance criteria, priority |
| **UI/UX** | ui, ux, screen, flow, menu | Design brief | UI spec, flows |

## Orchestration Workflow

### Step 1: Create or locate the feature file

- If new feature: create `Assets/Docs/Features/<slug>.md` from appropriate template (`_template.md`, `_enemy_template.md`, `_weapon_template.md`, etc.)
- If existing: read the current feature file and determine next role from `current_owner` / `next_owner` in frontmatter

### Step 2: Perform role work

For each role in the pipeline:

1. **Read** the role's inputs (previous sections, PROJECT.md, VisualStyleGuide)
2. **Do** the work (write code, create assets, update docs, call tools)
3. **Update** the feature file with results and handoff checkboxes
4. **Proceed** to the next role if the request spans multiple roles

### Step 3: Typical pipeline order

```
Design → Architect → 3D Modeler → Rigger → Animator → Engineer → CodeReviewer → Tester → Approved
```

**Approval gates:**
- **architect_review**: After Engineer writes Implementation Plan, Architect reviews before implementation proceeds
- **code_review**: CodeReviewer approves code before testing
- **video_demo**: Implementing agent records a demo video and posts link; can occur after Tester passes or as final handoff

Product and UI/UX can inject at any point. QA/Tester runs last for verification.

## System Behavior Rules

1. Always read `Assets/Docs/START_HERE.md` and the target feature file before acting
2. Determine CURRENT ROLE from `current_owner` in the feature file YAML state block
3. Only perform work for the current role
4. Do NOT skip roles
5. When completing work, update:
   - your section in the feature file
   - `status`
   - `current_owner`
   - `next_owner`
   - `version`
   - `change_summary`
   - `last_updated_by`
   - `last_updated_at`
6. If requirements are unclear → add to Blockers instead of guessing
7. If output is incomplete → do NOT mark handoff complete
8. Do not broaden context beyond role/system needs unless blocked

## Feature Lifecycle

```
concept → spec_ready → asset_in_production → integration_ready → in_engine → qa_ready → approved
```

Agents must update `status` when their phase completes.

| Status | Meaning |
|--------|---------|
| concept | Design exploring ideas |
| spec_ready | Design + Architect approved; downstream can start |
| asset_in_production | Model/Rig/Animation in progress |
| integration_ready | Asset complete; Engineer integrating |
| in_engine | Prefab placed and functional |
| qa_ready | Ready for QA verification |
| approved | All checks passed |

## Modes

**exploration**
- Generate options
- Do NOT proceed to downstream roles

**production**
- Execute finalized concept
- Full pipeline enabled

## Feature File Handoff Rules

- Each role has a section with `**Handoff to X**: [ ] Complete`
- When a role finishes its work, mark `[x]` and add any notes for the next role
- If a role cannot complete (e.g., needs user input), leave `[ ]` and add a **Blockers** subsection

## Tool Usage

- **Meshy.ai**: For 3D model generation, the Modeler3D agent produces a refined prompt. Use `Tools/meshy_generate.py` with `MESHY_API_KEY` set in environment. Default output: `Assets/_Generated/Staging/<slug>.glb` (validate → promote to `Assets/Art/`). Run: `python Tools/meshy_generate.py "prompt" -o Assets/_Generated/Staging/<name>.glb`. Record final path in feature file Integration Contract.
- **MCP (unity-mcp)**: For scene manipulation, prefab creation, component attachment — use MCP tools when available.
- **Unity Editor**: When in Unity, use Claude Code CLI agents from `Assets/Docs/Agents/`.

## Example: "Create Forest Guardian enemy"

1. **Design**: Create `Features/forest-guardian.md` with Design Brief (narrative, encounter intent)
2. **Architect**: Write Architect Spec (technical structure, components, integration points)
3. **Modeler3D**: Refine prompt using Architect Spec and VisualStyleGuide; write Model Spec; run Meshy script; update Output path
3. **Rigger**: Rig the model; update Rig Spec; mark handoff to Animator
4. **Animator**: Create clips/controller; update Animation Spec; mark handoff to Engineer
5. **Engineer**: Integrate prefab; update Integration; mark handoff to QA
6. **QA**: Run checklist; mark items complete

## Role Refinements

Each role has a refinements file for accumulating learnings and improvements:

- **Path**: `Assets/Docs/Agents/Refinements/<Role>.md` (e.g. `Design.md`, `Modeler3D.md`)
- **Purpose**: When an agent discovers a better practice, edge case, or clarification while performing its role, it adds an entry to its refinements file
- **Format**: `- **[YYYY-MM-DD]**: Brief description. Details.`
- **Usage**: Each agent reads its refinements file (when present) before doing work and may append new entries when it learns something

This lets roles evolve over time based on actual feature work.

## Discord Coordination

Discord coordination is supported via `Tools/discord_agent_bot`. Discord is signaling and coordination only; feature files remain canonical. Ownership and blockers may be mirrored through Discord bot commands such as `/claim`, `/handoff`, `/blocker`, and `/approve`. See `Assets/Docs/DiscordCoordination.md` for details.

## Quick Reference

- **Minimal context entry point**: `Assets/Docs/START_HERE.md`
- **Project context**: `Assets/Docs/PROJECT.md`
- **System docs (how each system works + rules)**: `Assets/Docs/Systems/README.md` and `Assets/Docs/Systems/*.md`
- **Art direction**: `Assets/Docs/VisualStyleGuide.md`
- **Feature template**: `Assets/Docs/Features/_template.md`
- **Asset templates**: `Assets/Docs/Features/_enemy_template.md`, `_weapon_template.md`, `_character_template.md`, `_environment_template.md`
- **Feature registry**: `Assets/Docs/FeatureRegistry.json`
- **Role definitions**: `Assets/Docs/Agents/*.md` (Design, Architect, Modeler3D, Rigger, QA, Product, Validator_*)
- **Role refinements**: `Assets/Docs/Agents/Refinements/*.md` (learnings and improvements per role)

## Cursor Cloud specific instructions

### Environment overview

This is a **Unity 6** game project (version 6000.3.9f1). The primary development tool is the Unity Editor, which **cannot run in the Cloud Agent VM** (it requires a GUI, the specific Unity version, and a Unity license). The cloud environment is suitable for:

- **Python tooling**: `Tools/meshy_generate.py` (Meshy.ai 3D model generation) and `Tools/discord_agent_bot/` (Discord coordination bot). Both are fully functional.
- **C# code editing and review**: All game code is under `Assets/Geis/Scripts/` (~385 files). Code can be read and edited but not compiled or tested without Unity Editor.
- **Documentation and feature file work**: The agent orchestration pipeline (feature files, design/architect/modeler roles) works entirely via markdown files.

### What works in Cloud Agent

| Capability | Status | Notes |
|---|---|---|
| Python tooling (Meshy, Discord bot) | Working | `pip install -r Tools/requirements.txt -r Tools/discord_agent_bot/requirements.txt` |
| C# code editing | Working | Edit files, but cannot compile/test outside Unity |
| Feature file orchestration | Working | Read/write markdown feature files per AGENTS.md workflow |
| Unity Editor (compile, test, play) | Not available | Requires local Unity 6000.3.9f1 installation |
| Unity Test Runner (NUnit tests) | Not available | Tests in `Assets/Geis/Tests/` require Unity Editor |
| C# linting | Not configured | No linting tools (StyleCop, Roslyn analyzers) are set up in the repo |

### Running Python tools

- **Meshy.ai generator**: `MESHY_API_KEY=<key> python3 Tools/meshy_generate.py "<prompt>" -o Assets/_Generated/Staging/<name>.glb` (use test key `msy_dummy_api_key_for_test_mode_12345678` for dry runs)
- **Discord bot**: Requires `DISCORD_BOT_TOKEN` env var. Run via `python3 -m discord_agent_bot.main` from `Tools/` directory.

### Key caveats

- No `.editorconfig`, StyleCop, or Roslyn analyzers exist in the repo — there is no C# linting to run.
- Unity NUnit tests (`Assets/Geis/Tests/`, `Packages/com.funder.core/Tests/`) can only be executed inside Unity Editor's Test Runner.
- The project has no CI/CD pipelines, no Docker setup, and no build scripts — all build/test operations go through Unity Editor.
