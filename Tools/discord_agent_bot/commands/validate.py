"""
Validation slash command and stub validator execution.

TODO: Hook point for Meshy / Unity MCP / validator integrations.
Validators: style, naming, unity
"""

import logging

import discord
from discord import app_commands

from ..config import get_config
from ..discord_formatter import format_validation_request, format_validation_result
from ..registry import DiscordRegistry

logger = logging.getLogger(__name__)


# TODO: Validator execution hook - integrate with external validators (style, naming, unity)
# def run_validator(slug: str, validator: str) -> tuple[bool, str]:
#     """Execute validator and return (success, message)."""
#     pass


def _run_validator_stub(slug: str, validator: str) -> tuple[bool, str]:
    """Stub validator - returns placeholder result."""
    cfg = get_config()
    if validator not in cfg.allowed_validators:
        return False, f"Unknown validator: {validator}"
    return True, f"Validator '{validator}' stub run for {slug}. TODO: integrate validator."


@app_commands.command(name="validate", description="Run validation for a feature")
@app_commands.describe(
    slug="Feature slug",
    validator="Validator type: style, naming, unity",
)
async def validate(interaction: discord.Interaction, slug: str, validator: str):
    await interaction.response.defer(ephemeral=False)
    cfg = get_config()
    registry = DiscordRegistry()

    slug = slug.lower().strip()
    validator = validator.lower().strip()

    if validator not in cfg.allowed_validators:
        await interaction.followup.send(
            f"Invalid validator: {validator}. Allowed: {', '.join(cfg.allowed_validators)}"
        )
        return

    entry = registry.get_entry(slug)
    if not entry:
        await interaction.followup.send(
            f"Feature `{slug}` not found in registry. Create it with /feature_create first."
        )
        return

    request_msg = format_validation_request(slug, validator)
    success, result_msg = _run_validator_stub(slug, validator)
    status_msg = format_validation_result(slug, validator, success, result_msg)

    if entry.thread_id and interaction.guild:
        try:
            thread = await interaction.guild.fetch_channel(entry.thread_id)
            if thread:
                await thread.send(f"{request_msg}\n\n{status_msg}")
        except Exception as e:
            logger.warning("Could not post to feature thread: %s", e)

    await interaction.followup.send(
        f"Validation requested for `{slug}` (validator: {validator}). Result: {result_msg}"
    )
