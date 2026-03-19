"""Blocker slash command."""

import logging

import discord
from discord import app_commands

from ..config import get_config
from ..discord_formatter import format_blocker
from ..feature_parser import parse_feature
from ..orchestrator import Orchestrator
from ..registry import DiscordRegistry

logger = logging.getLogger(__name__)


@app_commands.command(name="blocker", description="Add a blocker to a feature")
@app_commands.describe(
    slug="Feature slug",
    message="Description of the blocking issue",
)
async def blocker(interaction: discord.Interaction, slug: str, message: str):
    await interaction.response.defer(ephemeral=False)
    config = get_config()
    registry = DiscordRegistry()
    orch = Orchestrator(registry)

    slug = slug.lower().strip()

    meta = orch._read_feature_state(slug)
    if not meta:
        await interaction.followup.send(f"Feature `{slug}` not found.")
        return

    ok, err = orch.add_blocker(slug, message)
    if not ok:
        await interaction.followup.send(f"Failed to add blocker: {err}")
        return

    entry = registry.get_entry(slug)
    if entry and entry.thread_id and interaction.guild:
        try:
            thread = await interaction.guild.fetch_channel(entry.thread_id)
            if thread:
                msg = format_blocker(slug, meta.current_owner, message)
                await thread.send(msg)
        except Exception as e:
            logger.warning("Could not post to feature thread: %s", e)

    blockers_ch = interaction.guild.get_channel(config.channel_agent_blockers_id) if interaction.guild else None
    if blockers_ch:
        msg = format_blocker(slug, meta.current_owner, message)
        await blockers_ch.send(msg)

    await interaction.followup.send(f"Blocker added to `{slug}`.")
