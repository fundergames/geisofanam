"""Feature create and status slash commands."""

import logging
from pathlib import Path

import discord
from discord import app_commands

from ..config import get_config
from ..discord_formatter import format_status_card
from ..feature_parser import parse_feature
from ..feature_writer import ensure_feature_file, get_feature_path
from ..orchestrator import Orchestrator
from ..registry import DiscordRegistry

logger = logging.getLogger(__name__)


@app_commands.command(name="feature_create", description="Create a new feature and Discord thread")
@app_commands.describe(
    feature_type="Asset type (enemy, weapon, character, environment, generic)",
    slug="URL-safe feature slug (e.g. forest-guardian)",
    title="Optional display title",
)
async def feature_create(
    interaction: discord.Interaction,
    feature_type: str,
    slug: str,
    title: str | None = None,
):
    await interaction.response.defer(ephemeral=False)
    config = get_config()
    registry = DiscordRegistry()
    orch = Orchestrator(registry)

    slug = slug.lower().strip().replace(" ", "-")
    if not slug.replace("-", "").replace("_", "").isalnum():
        await interaction.followup.send(
            "Invalid slug: use only letters, numbers, hyphens, underscores."
        )
        return

    if registry.get_entry(slug):
        await interaction.followup.send(f"Feature `{slug}` already exists and is registered.")
        return

    path = get_feature_path(config.features_dir, slug)
    if path.exists():
        await interaction.followup.send(f"Feature file already exists. Register with a different slug or use /feature_status.")
        return

    path, created = ensure_feature_file(slug, feature_type, title)
    if not path:
        await interaction.followup.send(f"Failed to create feature file for type `{feature_type}`.")
        return

    orch.register_in_feature_registry(slug, feature_type, title or slug)

    requests_ch = interaction.guild.get_channel(config.channel_agent_requests_id) if interaction.guild else None
    if not requests_ch:
        await interaction.followup.send("Could not find #agent-requests channel. Set DISCORD_CHANNEL_AGENT_REQUESTS.")
        return

    thread_name = f"{feature_type}-{slug}"
    try:
        thread_msg = await requests_ch.send(f"Feature: **{thread_name}**")
        thread = await thread_msg.create_thread(name=thread_name, auto_archive_duration=10080)
    except Exception as e:
        await interaction.followup.send(f"Could not create thread: {e}")
        return

    meta = parse_feature(path)
    summary = format_status_card(meta, thread.jump_url)
    await thread.send(summary)

    registry.set_entry(
        slug,
        feature_path=str(path),
        thread_id=thread.id,
        channel_id=thread.parent_id,
    )

    msg = f"Feature created: `{slug}`\nFile: `{path}`\nThread: {thread.jump_url}"
    await interaction.followup.send(msg)


@app_commands.command(name="feature_status", description="Show feature status")
@app_commands.describe(slug="Feature slug")
async def feature_status(interaction: discord.Interaction, slug: str):
    await interaction.response.defer(ephemeral=True)
    config = get_config()
    registry = DiscordRegistry()
    slug = slug.lower().strip()

    path = get_feature_path(config.features_dir, slug)
    if not path.exists():
        await interaction.followup.send(f"Feature `{slug}` not found.", ephemeral=True)
        return

    meta = parse_feature(path)
    entry = registry.get_entry(slug)
    thread_link = None
    if entry and entry.thread_id and interaction.guild:
        try:
            thread = await interaction.guild.fetch_channel(entry.thread_id)
            if thread:
                thread_link = thread.jump_url if hasattr(thread, 'jump_url') else str(entry.thread_id)
        except Exception:
            pass

    text = format_status_card(meta, thread_link)
    await interaction.followup.send(text, ephemeral=True)
