"""Claim slash command for ownership locking."""

import logging

import discord
from discord import app_commands

from ..config import get_config
from ..discord_formatter import format_claim
from ..orchestrator import Orchestrator
from ..registry import DiscordRegistry

logger = logging.getLogger(__name__)


@app_commands.command(name="claim", description="Claim ownership of a feature")
@app_commands.describe(
    slug="Feature slug",
    role="Your role (Design, Architect, Modeler3D, etc.)",
)
async def claim(interaction: discord.Interaction, slug: str, role: str):
    await interaction.response.defer(ephemeral=False)
    config = get_config()
    registry = DiscordRegistry()
    orch = Orchestrator(registry)

    slug = slug.lower().strip()
    role = role.strip()

    if role not in config.allowed_roles:
        await interaction.followup.send(
            f"Invalid role `{role}`. Allowed: {', '.join(config.allowed_roles)}"
        )
        return

    path = orch.get_feature_path(slug)
    if not path or not path.exists():
        await interaction.followup.send(f"Feature `{slug}` not found.")
        return

    if not registry.get_entry(slug):
        registry.set_entry(slug, feature_path=str(path))

    ok, err = orch.claim(slug, role)
    if not ok:
        await interaction.followup.send(f"Claim failed: {err}")
        return

    entry = registry.get_entry(slug)
    log_ch = interaction.guild.get_channel(config.channel_agent_log_id) if interaction.guild else None

    if entry and entry.thread_id and interaction.guild:
        try:
            thread = await interaction.guild.fetch_channel(entry.thread_id)
            if thread:
                msg = format_claim(slug, role)
                await thread.send(msg)
        except Exception as e:
            logger.warning("Could not post to feature thread: %s", e)

    if log_ch:
        msg = format_claim(slug, role)
        await log_ch.send(msg)

    await interaction.followup.send(f"Claimed `{slug}` as **{role}**.")
