# Discord Coordination Layer

This document describes how the Discord coordination layer works for the Geis of Anam agent pipeline.

## Purpose

Discord serves as the **signaling and coordination layer**. It is not the source of truth. Feature markdown files in `Assets/Docs/Features/` remain canonical. Discord provides:

- Visibility: each feature has a thread where status, handoffs, and blockers are posted
- Coordination: slash commands for claim, handoff, blocker, approve
- Escalation: #agent-blockers and #agent-approvals channels
- Logging: #agent-log for major state changes

## Architecture Overview

```
Feature files (Assets/Docs/Features/*.md)  ←── canonical
         │
         │ read/write
         ▼
  Orchestrator (validates transitions, locks)
         │
         │ mirrors state
         ▼
  Discord (threads, channels)
         │
         ▲
  Slash commands (/claim, /handoff, etc.)
```

- **Feature files** are the production record. All state (status, owner, blockers, approvals) lives in YAML frontmatter.
- **Orchestrator** validates allowed transitions and ownership locks before updating files.
- **Discord** displays updates and accepts coordination commands. State is always derived from feature files when possible.

## Canonical Source of Truth Rule

1. Feature files are authoritative. If there is a conflict between Discord and a feature file, the feature file wins.
2. Slash commands read from feature files, validate, update feature files, then post to Discord.
3. The file watcher detects feature file changes and posts updates to Discord threads. It does not overwrite feature files.

## Channel Layout

| Channel | Purpose |
|---------|---------|
| #agent-requests | Create feature threads; request new features |
| #agent-blockers | Mirror of blockers; escalation visibility |
| #agent-approvals | Approval requests and decisions |
| #agent-log | Major state changes (handoffs, claims) |

Each feature gets **one thread**, usually created from #agent-requests. Thread names follow `<asset-type>-<slug>` (e.g., `enemy-forest-guardian`, `weapon-branch-dagger`).

## Slash Commands

| Command | Behavior |
|---------|----------|
| `/feature_create` | Create feature file from template, register in FeatureRegistry, create Discord thread |
| `/feature_status` | Read feature file; display status, owner, blockers, approvals; include thread link if mapped |
| `/claim` | Validate role == current_owner; create lock; post to feature thread and #agent-log |
| `/handoff` | Validate no blockers, current role owns lock; update feature file (owner, version, change_summary); post handoff message; release lock |
| `/blocker` | Append blocker to feature file; post to feature thread and #agent-blockers |
| `/approve_request` | Post approval request in feature thread and #agent-approvals |
| `/approve` | Update approval in feature frontmatter; post result in feature thread |
| `/validate` | Stub validator execution; post request and status to thread (TODO: integrate validators) |
| `/summary` | Summarize feature state; post compact status card |

**Approval types:** `design`, `architect`, `architect_review`, `modeling`, `engineering`, `code_review`, `qa`, `video_demo`

## Lock Behavior

- Only one active role lock per feature.
- `/claim` fails if a different role already holds the lock.
- `/handoff` releases the old lock and moves ownership to the next role.
- `/blocker` does not release ownership.
- Lock state is stored in `Tools/discord_agent_bot/data/discord_registry.json`.

## State Sync Behavior

1. **Slash commands** update feature files first, then post to Discord.
2. **File watcher** monitors `Assets/Docs/Features/*.md`. When frontmatter changes (status, owner, blockers, approvals, version, change_summary), it posts an update to the mapped Discord thread.
3. A state hash/snapshot is used to avoid posting duplicate or trivial formatting-only edits.

## Deployment Options

1. **discord_slack_bot (recommended for cloud)** – The coordination layer is integrated into the [discord_slack_bot](https://github.com/fundergames/discord_slack_bot) project. Set `GEIS_GITHUB_OWNER`, `GEIS_GITHUB_REPO`, and `GEIS_GITHUB_TOKEN` in the bot's environment. Feature files are read/written via the GitHub API. Deploy with your existing Railway/Render/Fly.io setup.

2. **Local Python bot** – `Tools/discord_agent_bot/` provides a standalone Python implementation with file watcher for local development. Use when you run the bot on a machine with the Geis repo checked out.

## Relationship to AGENTS.md

`AGENTS.md` defines the multi-agent orchestration workflow: roles, handoffs, feature lifecycle, and tool usage. The Discord coordination layer:

- Extends that workflow with Discord as the visibility and signaling medium
- Keeps the same roles and pipeline (Design → Architect → Modeler3D → Rigger → Animator → Engineer → QA)
- Preserves feature file structure and frontmatter contract
- Does not replace the file-based workflow; it integrates with it

Agents (Cursor, Claude Code CLI) continue to read and write feature files. The Discord bot provides coordination and visibility for humans and future automation.
