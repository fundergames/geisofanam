"""Handoff slash command."""

import logging

import discord
from discord import app_commands

from ..config import get_config
from ..discord_formatter import format_handoff
from ..feature_parser import parse_feature
from ..orchestrator import Orchestrator
from ..registry import DiscordRegistry

logger = logging.getLogger(__name__)


@app_commands.command(name="handoff", description="Hand off feature to next role")
@app_commands.describe(
    slug="Feature slug",
    next_role="Role to hand off to",
    summary="Brief summary of what was completed",
)
async def handoff(
    interaction: discord.Interaction,
    slug: str,
    next_role: str,
    summary: str,
):
    await interaction.response.defer(ephemeral=False)
    config = get_config()
    registry = DiscordRegistry()
    orch = Orchestrator(registry)

    slug = slug.lower().strip()
    next_role = next_role.strip()

    if next_role not in config.allowed_roles:
        await interaction.followup.send(
            f"Invalid role `{next_role}`. Allowed: {', '.join(config.allowed_roles)}"
        )
        return

    meta = orch._read_feature_state(slug)
    if not meta:
        await interaction.followup.send(f"Feature `{slug}` not found.")
        return

    ok, err = orch.handoff(slug, meta.current_owner, next_role, summary)
    if not ok:
        await interaction.followup.send(f"Handoff failed: {err}")
        return

    entry = registry.get_entry(slug)
    if entry and entry.thread_id and interaction.guild:
        try:
            thread = await interaction.guild.fetch_channel(entry.thread_id)
            if thread:
                msg = format_handoff(slug, meta.current_owner, next_role, meta.status, summary)
                await thread.send(msg)
        except Exception as e:
            logger.warning("Could not post to feature thread: %s", e)

    log_ch = interaction.guild.get_channel(config.channel_agent_log_id) if interaction.guild else None
    if log_ch:
        msg = format_handoff(slug, meta.current_owner, next_role, meta.status, summary)
        await log_ch.send(msg)

    await interaction.followup.send(
        f"Handed off `{slug}` from **{meta.current_owner}** to **{next_role}**."
    )
