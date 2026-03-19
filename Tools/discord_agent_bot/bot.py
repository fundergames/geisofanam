"""
Discord bot: slash commands and coordination layer.
Feature files remain canonical; Discord is signaling and visibility.
"""

import logging
from pathlib import Path

import discord
from discord import app_commands

from .config import get_config
from .registry import DiscordRegistry
from .orchestrator import Orchestrator

# Import command handlers
from .commands.feature import feature_create, feature_status
from .commands.claim import claim
from .commands.handoff import handoff
from .commands.blocker import blocker
from .commands.approve import approve_request, approve
from .commands.validate import validate
from .commands.summary import summary

logger = logging.getLogger(__name__)


def _resolve_project_root() -> Path:
    """Resolve project root (geis_of_anam) relative to this script."""
    script_dir = Path(__file__).resolve().parent
    # Tools/discord_agent_bot -> project root
    return script_dir.parent.parent


class CoordinationBot(discord.Client):
    """Discord bot for agent coordination."""

    def __init__(self, *, project_root: Path | None = None):
        intents = discord.Intents.default()
        super().__init__(intents=intents)
        self.tree = app_commands.CommandTree(self)
        self._project_root = project_root or _resolve_project_root()
        self._registry: DiscordRegistry | None = None
        self._orchestrator: Orchestrator | None = None

    @property
    def registry(self) -> DiscordRegistry:
        if self._registry is None:
            self._registry = DiscordRegistry()
        return self._registry

    @property
    def orchestrator(self) -> Orchestrator:
        if self._orchestrator is None:
            self._orchestrator = Orchestrator(self.registry)
        return self._orchestrator

    async def setup_hook(self) -> None:
        """Register slash commands."""
        cfg = get_config()
        guild = discord.Object(id=cfg.guild_id) if cfg.guild_id else None

        self.tree.add_command(feature_create, guild=guild)
        self.tree.add_command(feature_status, guild=guild)
        self.tree.add_command(claim, guild=guild)
        self.tree.add_command(handoff, guild=guild)
        self.tree.add_command(blocker, guild=guild)
        self.tree.add_command(approve_request, guild=guild)
        self.tree.add_command(approve, guild=guild)
        self.tree.add_command(validate, guild=guild)
        self.tree.add_command(summary, guild=guild)

        if guild:
            await self.tree.sync(guild=guild)
        else:
            await self.tree.sync()

    async def on_ready(self) -> None:
        logger.info("Bot ready: %s (id=%s)", self.user, self.user.id if self.user else "?")

