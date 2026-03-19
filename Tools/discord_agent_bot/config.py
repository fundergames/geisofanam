"""Configuration for the Discord coordination layer."""

import os
from pathlib import Path

# Resolve paths relative to project root (geis_of_anam)
PROJECT_ROOT = Path(__file__).resolve().parent.parent.parent
FEATURES_PATH = PROJECT_ROOT / "Assets" / "Docs" / "Features"
FEATURE_REGISTRY_PATH = PROJECT_ROOT / "Assets" / "Docs" / "FeatureRegistry.json"
DISCORD_REGISTRY_PATH = Path(__file__).resolve().parent / "data" / "discord_registry.json"

# Discord environment variables
DISCORD_BOT_TOKEN = os.getenv("DISCORD_BOT_TOKEN", "")
DISCORD_GUILD_ID = int(os.getenv("DISCORD_GUILD_ID", "0"))
DISCORD_CHANNEL_AGENT_REQUESTS = int(os.getenv("DISCORD_CHANNEL_AGENT_REQUESTS", "0"))
DISCORD_CHANNEL_AGENT_BLOCKERS = int(os.getenv("DISCORD_CHANNEL_AGENT_BLOCKERS", "0"))
DISCORD_CHANNEL_AGENT_APPROVALS = int(os.getenv("DISCORD_CHANNEL_AGENT_APPROVALS", "0"))
DISCORD_CHANNEL_AGENT_LOG = int(os.getenv("DISCORD_CHANNEL_AGENT_LOG", "0"))

# Template paths by feature type
TEMPLATE_PATHS = {
    "generic": FEATURES_PATH / "_template.md",
    "enemy": FEATURES_PATH / "_enemy_template.md",
    "weapon": FEATURES_PATH / "_weapon_template.md",
    "character": FEATURES_PATH / "_character_template.md",
    "environment": FEATURES_PATH / "_environment_template.md",
}

# Fallback template
DEFAULT_TEMPLATE = FEATURES_PATH / "_template.md"

# Allowed roles
ALLOWED_ROLES = [
    "Design",
    "Architect",
    "Modeler3D",
    "Rigger",
    "Animator",
    "Engineer",
    "CodeReviewer",
    "QA",
    "Tester",
    "Product",
    "UI/UX",
]

# Allowed status states and valid transitions
ALLOWED_STATES = [
    "concept",
    "spec_ready",
    "asset_in_production",
    "integration_ready",
    "in_engine",
    "qa_ready",
    "approved",
]

# Valid state transitions (no skipping unless forced)
STATE_TRANSITIONS: dict[str, list[str]] = {
    "concept": ["spec_ready"],
    "spec_ready": ["asset_in_production"],
    "asset_in_production": ["integration_ready"],
    "integration_ready": ["in_engine"],
    "in_engine": ["qa_ready"],
    "qa_ready": ["approved"],
    "approved": [],
}

# Role-to-status expectations (rough mapping for validation hints)
ROLE_TO_STATE_EXPECTATION: dict[str, list[str]] = {
    "Design": ["concept"],
    "Architect": ["concept", "spec_ready"],
    "Modeler3D": ["spec_ready", "asset_in_production"],
    "Rigger": ["asset_in_production"],
    "Animator": ["asset_in_production"],
    "Engineer": ["integration_ready", "in_engine"],
    "QA": ["qa_ready", "approved"],
    "Product": ["concept", "spec_ready", "asset_in_production", "integration_ready", "in_engine", "qa_ready", "approved"],
    "UI/UX": ["concept", "spec_ready", "asset_in_production"],
}

# Allowed validators (stub for future integration)
ALLOWED_VALIDATORS = ["style", "naming", "unity"]

# Approval types (match frontmatter keys)
APPROVAL_TYPES = [
    "design",
    "architect",
    "architect_review",  # Architect reviews implementation plan
    "modeling",
    "engineering",
    "code_review",
    "qa",
    "video_demo",
]

# Role pipeline order for next_owner computation
ROLE_PIPELINE = [
    "Design",
    "Architect",
    "Modeler3D",
    "Rigger",
    "Animator",
    "Engineer",
    "CodeReviewer",
    "QA",
    "Tester",
    "Product",
    "UI/UX",
]


class _Config:
    """Config namespace for easy access."""

    @property
    def features_path(self) -> Path:
        return FEATURES_PATH

    @property
    def features_dir(self) -> Path:
        return FEATURES_PATH

    @property
    def feature_registry_path(self) -> Path:
        return FEATURE_REGISTRY_PATH

    @property
    def registry_path(self) -> str:
        return str(DISCORD_REGISTRY_PATH)

    @property
    def guild_id(self) -> int:
        return DISCORD_GUILD_ID

    @property
    def channel_agent_requests_id(self) -> int:
        return DISCORD_CHANNEL_AGENT_REQUESTS

    @property
    def channel_agent_blockers_id(self) -> int:
        return DISCORD_CHANNEL_AGENT_BLOCKERS

    @property
    def channel_agent_approvals_id(self) -> int:
        return DISCORD_CHANNEL_AGENT_APPROVALS

    @property
    def channel_agent_log_id(self) -> int:
        return DISCORD_CHANNEL_AGENT_LOG

    @property
    def allowed_roles(self) -> list[str]:
        return ALLOWED_ROLES.copy()

    @property
    def role_pipeline(self) -> list[str]:
        return ROLE_PIPELINE.copy()

    @property
    def approval_types(self) -> list[str]:
        return APPROVAL_TYPES.copy()

    @property
    def template_paths(self) -> dict:
        return TEMPLATE_PATHS.copy()

    @property
    def default_template(self) -> Path:
        return DEFAULT_TEMPLATE

    @property
    def allowed_validators(self) -> list[str]:
        return ALLOWED_VALIDATORS.copy()

    @property
    def token(self) -> str:
        return os.getenv("DISCORD_BOT_TOKEN", "")

    @property
    def watch_features(self) -> bool:
        return os.getenv("DISCORD_WATCH_FEATURES", "true").lower() in ("1", "true", "yes")


_config = _Config()


def get_config() -> _Config:
    return _config
