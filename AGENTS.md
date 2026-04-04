# Geis of Anam — Agent Development Guide

## Overview

Unity 6 (6000.3.9f1) third-person action RPG ("Geis of Anam") by Funder Games. The core game is C# in `Assets/Geis/Scripts/` (~310 scripts). Python tooling lives in `Tools/`.

See `Assets/Docs/AGENTS.md` for the multi-agent orchestration workflow (Design → Architect → 3D Modeler → Rigger → Animator → Engineer → QA).

## Cursor Cloud specific instructions

### Unity Editor setup

Unity 6 (`6000.3.9f1`) is installed at `/home/ubuntu/Unity/Hub/Editor/6000.3.9f1/Editor/Unity`. Unity Hub is installed at `/opt/unityhub/unityhub`.

**License activation is required before the editor can run.** A Unity account must be signed in via Unity Hub to obtain a license. To launch Unity Hub on the desktop:
```
DISPLAY=:1 /opt/unityhub/unityhub --no-sandbox --disable-gpu-sandbox &
```
After login, the editor can be used via the desktop or in batch mode.

**Batch mode** (headless, no GPU):
```bash
UNITY=/home/ubuntu/Unity/Hub/Editor/6000.3.9f1/Editor/Unity
$UNITY -batchmode -nographics -quit -projectPath /workspace -logFile /tmp/unity.log
```
Note: batch mode requires the `com.unity.editor.headless` entitlement. Personal licenses may only work with the GUI editor (via `xvfb-run` or desktop). If batch mode fails with license error, run the editor with a display instead:
```bash
xvfb-run --auto-servernum --server-args="-screen 0 1920x1080x24" \
  /home/ubuntu/Unity/Hub/Editor/6000.3.9f1/Editor/Unity \
  -projectPath /workspace -logFile /tmp/unity.log
```

**Running editor tests** (requires license):
```bash
$UNITY -batchmode -nographics -runTests -testPlatform EditMode -projectPath /workspace -logFile /tmp/unity_tests.log
```

### What CAN be done without Unity license

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

- **Unity binary**: `/home/ubuntu/Unity/Hub/Editor/6000.3.9f1/Editor/Unity`
- **Game scripts**: `Assets/Geis/Scripts/` (C#, ~310 files)
- **Core framework package**: `Packages/com.funder.core/` (FSM, events, services)
- **Core package tests**: `Packages/com.funder.core/Tests/Editor/` (StateMachine, EventBus, ServiceLocator, RandomHub)
- **Python tools**: `Tools/meshy_generate.py`, `Tools/discord_agent_bot/`
- **Feature files**: `Assets/Docs/Features/*.md`
- **Project docs**: `Assets/Docs/PROJECT.md`, `Assets/Docs/VisualStyleGuide.md`
- **Agent role definitions**: `Assets/Docs/Agents/*.md`
- **Unity project settings**: `ProjectSettings/`
