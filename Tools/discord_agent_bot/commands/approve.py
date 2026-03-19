"""Approve request and approve slash commands."""

import logging

import discord
from discord import app_commands

from ..config import get_config
from ..discord_formatter import format_approval_request, format_approval_result
from ..orchestrator import Orchestrator
from ..registry import DiscordRegistry

logger = logging.getLogger(__name__)


@app_commands.command(name="approve_request", description="Request approval for a feature")
@app_commands.describe(
    slug="Feature slug",
    approval_type="Type: design, architect, modeling, engineering, qa",
    summary="What you're requesting approval for",
)
async def approve_request(
    interaction: discord.Interaction,
    slug: str,
    approval_type: str,
    summary: str,
):
    await interaction.response.defer(ephemeral=False)
    config = get_config()
    registry = DiscordRegistry()
    orch = Orchestrator(registry)

    slug = slug.lower().strip()
    approval_type = approval_type.lower().strip()

    if approval_type not in config.approval_types:
        await interaction.followup.send(
            f"Invalid approval type. Allowed: {', '.join(config.approval_types)}"
        )
        return

    path = orch.get_feature_path(slug)
    if not path or not path.exists():
        await interaction.followup.send(f"Feature `{slug}` not found.")
        return

    meta = orch._read_feature_state(slug)
    msg = format_approval_request(slug, approval_type, summary)

    entry = registry.get_entry(slug)
    if entry and entry.thread_id and interaction.guild:
        try:
            thread = await interaction.guild.fetch_channel(entry.thread_id)
            if thread:
                await thread.send(msg)
        except Exception as e:
            logger.warning("Could not post to feature thread: %s", e)

    approvals_ch = interaction.guild.get_channel(config.channel_agent_approvals_id) if interaction.guild else None
    if approvals_ch:
        await approvals_ch.send(msg)

    await interaction.followup.send(f"Approval request posted for `{slug}` ({approval_type}).")


@app_commands.command(name="approve", description="Approve or reject an approval request")
@app_commands.describe(
    slug="Feature slug",
    approval_type="Type: design, architect, modeling, engineering, qa",
    decision="approved or rejected",
    notes="Optional notes (e.g., blocker reason if rejected)",
)
async def approve(
    interaction: discord.Interaction,
    slug: str,
    approval_type: str,
    decision: str,
    notes: str | None = None,
):
    await interaction.response.defer(ephemeral=False)
    config = get_config()
    registry = DiscordRegistry()
    orch = Orchestrator(registry)

    slug = slug.lower().strip()
    approval_type = approval_type.lower().strip()
    decision = decision.lower().strip()

    if decision not in ("approved", "rejected"):
        await interaction.followup.send("Decision must be `approved` or `rejected`.")
        return

    if approval_type not in config.approval_types:
        await interaction.followup.send(
            f"Invalid approval type. Allowed: {', '.join(config.approval_types)}"
        )
        return

    ok, err = orch.set_approval(slug, approval_type, decision, notes)
    if not ok:
        await interaction.followup.send(f"Failed: {err}")
        return

    msg = format_approval_result(slug, approval_type, decision, notes)

    entry = registry.get_entry(slug)
    if entry and entry.thread_id and interaction.guild:
        try:
            thread = await interaction.guild.fetch_channel(entry.thread_id)
            if thread:
                await thread.send(msg)
        except Exception as e:
            logger.warning("Could not post to feature thread: %s", e)

    await interaction.followup.send(
        f"Approval recorded: `{slug}` {approval_type} = **{decision}**."
    )
