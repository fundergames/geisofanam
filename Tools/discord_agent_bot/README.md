# Discord Agent Coordination Bot

Discord coordination layer for the Geis of Anam multi-agent pipeline. Agents coordinate through Discord while feature markdown files remain the **canonical source of truth**.

## Purpose

- **Discord**: Coordination, visibility, signaling
- **Feature files**: Production record, source of truth
- One Discord thread per feature
- Slash commands for claim, handoff, blockers, approvals
- File watcher syncs feature changes to Discord threads
- Ownership locking: one role per feature at a time
- State transition validation

## Setup

### 1. Install dependencies

```bash
cd Tools/discord_agent_bot
pip install -r requirements.txt
```

### 2. Environment variables

Copy `.env.example` to `.env` and configure:

```bash
cp .env.example .env
```

Required:

- `DISCORD_BOT_TOKEN` — Bot token from [Discord Developer Portal](https://discord.com/developers/applications)
- `DISCORD_GUILD_ID` — Your server/guild ID
- `DISCORD_CHANNEL_AGENT_REQUESTS` — Channel ID for #agent-requests
- `DISCORD_CHANNEL_AGENT_BLOCKERS` — Channel ID for #agent-blockers
- `DISCORD_CHANNEL_AGENT_APPROVALS` — Channel ID for #agent-approvals
- `DISCORD_CHANNEL_AGENT_LOG` — Channel ID for #agent-log

Use channel IDs (right-click channel → Copy ID, with Developer Mode on).

### 3. Invite the bot

1. In Discord Developer Portal, create an application and bot
2. Enable **Message Content Intent** if needed
3. OAuth2 URL Generator → Scopes: `bot`, `applications.commands`
4. Bot Permissions: Send Messages, Create Public Threads, Use Slash Commands, Manage Threads
5. Invite to your server

### 4. Run locally

```bash
# From project root (geis_of_anam)
python -m Tools.discord_agent_bot.main

# Or from Tools/discord_agent_bot
python -m discord_agent_bot.main
```

The bot loads `.env` and connects. Slash commands sync to your guild.

## Commands

| Command | Description |
|---------|-------------|
| `/feature_create feature_type slug [title]` | Create feature file from template, register, create Discord thread |
| `/feature_status slug` | Show status, owner, blockers, approvals, thread link |
| `/claim slug role` | Claim ownership (lock) for current_owner role |
| `/handoff slug next_role summary` | Hand off to next role (no blockers) |
| `/blocker slug message` | Add blocker; post to thread and #agent-blockers |
| `/approve_request slug approval_type summary` | Request approval; post to thread and #agent-approvals |
| `/approve slug approval_type decision [notes]` | Record approval/rejection |
| `/validate slug validator` | Run validator (style, naming, unity) — stub for now |
| `/summary slug` | Post compact status card to feature thread |

## Registry

`data/discord_registry.json` maps feature slugs to:

- Feature file path
- Discord thread ID
- Channel ID
- Active lock (role, timestamp)
- Last synced version and state snapshot

The registry is updated on each relevant command. It survives bot restarts.

## File watching

When `WATCH_FEATURES=true` (default), the bot watches `Assets/Docs/Features/*.md`. On meaningful frontmatter changes (status, owner, blockers, approvals, version), it posts an update to the feature thread and avoids noisy duplicates via a state hash.

## Known limitations

- Features created outside `/feature_create` must be registered before claim/handoff (create registry entry via `/feature_create` or manual edit)
- Thread creation requires a text channel; forum channels may need different logic
- Validators are stubbed; style/naming/unity hooks are TODOs
- No cloud hosting; local or self-hosted only

## Future hooks

Planned integration points (see TODO comments in code):

- **Meshy generation hook** — Call Meshy from bot or via feature file
- **Unity MCP execution hook** — Run Unity operations
- **Validator execution hook** — Connect to Validator_Style, Validator_Naming, Validator_Unity
- **Role auto-dispatch hook** — Auto-assign next owner from pipeline

## Integration with AGENTS.md

See `Assets/Docs/DiscordCoordination.md` for the full coordination architecture and how it fits with `AGENTS.md`.
