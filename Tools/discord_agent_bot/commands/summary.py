"""/summary slash command - compact status card for a feature."""

import logging

import discord
from discord import app_commands

from ..config import get_config
from ..discord_formatter import format_status_card
from ..feature_parser import parse_feature
from ..feature_writer import get_feature_path
from ..registry import DiscordRegistry

logger = logging.getLogger(__name__)


@app_commands.command(name="summary", description="Summarize feature state and post compact status card")
@app_commands.describe(slug="Feature slug")
async def summary(interaction: discord.Interaction, slug: str):
    await interaction.response.defer(ephemeral=False)
    cfg = get_config()
    registry = DiscordRegistry()

    slug = slug.lower().strip()
    path = get_feature_path(cfg.features_dir, slug)
    if not path.exists():
        await interaction.followup.send(f"Feature `{slug}` not found.")
        return

    meta = parse_feature(path)
    if not meta:
        await interaction.followup.send(f"Could not parse feature file: {path}")
        return

    entry = registry.get_entry(slug)
    thread_link = None
    if entry and entry.thread_id and interaction.guild:
        try:
            thread = await interaction.guild.fetch_channel(entry.thread_id)
            if thread and hasattr(thread, "jump_url"):
                thread_link = thread.jump_url
        except Exception:
            pass

    card = format_status_card(meta, thread_link)

    if entry and entry.thread_id and interaction.guild:
        try:
            thread = await interaction.guild.fetch_channel(entry.thread_id)
            if thread:
                await thread.send(card)
                await interaction.followup.send(f"Posted summary to feature thread.")
                return
        except Exception as e:
            logger.warning("Could not post to thread: %s", e)

    await interaction.followup.send(f"**Summary for `{slug}`:**\n\n{card}")
