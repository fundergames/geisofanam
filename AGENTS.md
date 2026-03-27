# Geis of Anam — Agent Development Guide

## Overview

Unity 6 (6000.3.9f1) third-person action RPG ("Geis of Anam") by Funder Games. The core game is C# in `Assets/Geis/Scripts/` (~310 scripts). Python tooling lives in `Tools/`.

See `Assets/Docs/AGENTS.md` for the multi-agent orchestration workflow (Design → Architect → 3D Modeler → Rigger → Animator → Engineer → QA).

## Cursor Cloud specific instructions

### Environment limitations

- **Unity Editor is NOT available** in the Cloud Agent VM. C# compilation, Unity Test Runner, Play mode, and builds all require Unity 6 (`6000.3.9f1`) which is a large GUI application. This means:
  - No `.sln` / `.csproj` files exist (Unity auto-generates them on project open; they are gitignored).
  - C# static analysis (Roslyn, OmniSharp) cannot run without these project files.
  - Unity editor tests in `Packages/com.funder.core/Tests/Editor/` cannot be executed.
- All C# changes must be validated by code review; runtime testing requires a local Unity Editor.

### What CAN be done in Cloud Agent

| Task | How |
|------|-----|
| Edit C# scripts | Direct file editing — verify syntax visually |
| Lint Python tools | `flake8 Tools/ --max-line-length=120` |
| Run Meshy 3D generator | `export MESHY_API_KEY=msy_dummy_api_key_for_test_mode_12345678 && python3 Tools/meshy_generate.py "prompt" --preview-only -o /tmp/output.glb` (test mode) |
| Parse feature files | `python3 -c "from Tools.discord_agent_bot.feature_parser import parse_feature; ..."` |
| Run Discord bot | Requires `DISCORD_BOT_TOKEN` env var — see `Tools/discord_agent_bot/README.md` |
| Edit feature/docs files | `Assets/Docs/Features/*.md`, `Assets/Docs/Agents/*.md` |

### Python dependencies

Installed via: `pip3 install -r Tools/requirements.txt -r Tools/discord_agent_bot/requirements.txt`

### Key paths

- **Game scripts**: `Assets/Geis/Scripts/` (C#, ~310 files)
- **Core framework package**: `Packages/com.funder.core/` (FSM, events, services)
- **Python tools**: `Tools/meshy_generate.py`, `Tools/discord_agent_bot/`
- **Feature files**: `Assets/Docs/Features/*.md`
- **Project docs**: `Assets/Docs/PROJECT.md`, `Assets/Docs/VisualStyleGuide.md`
- **Agent role definitions**: `Assets/Docs/Agents/*.md`
- **Unity project settings**: `ProjectSettings/`
