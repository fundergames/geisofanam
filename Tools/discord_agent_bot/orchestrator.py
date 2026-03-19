"""
Orchestrator: validates state transitions, ownership, blockers, and roles.
Feature files are canonical; orchestrator enforces valid operations.
"""

from __future__ import annotations

import logging
from typing import Optional

from .config import get_config
from .feature_parser import parse_feature
from .feature_writer import FeatureWriter
from .models import FeatureState
from .registry import DiscordRegistry

logger = logging.getLogger(__name__)

VALID_STATES = [
    "concept",
    "spec_ready",
    "asset_in_production",
    "integration_ready",
    "in_engine",
    "qa_ready",
    "approved",
]

# Role expected per status (flexible; used for validation hints)
STATUS_TO_ROLE = {
    "concept": "Design",
    "spec_ready": "Architect",
    "asset_in_production": ["Modeler3D", "Rigger", "Animator"],
    "integration_ready": "Engineer",
    "in_engine": "Engineer",
    "qa_ready": "QA",
    "approved": None,
}


class Orchestrator:
    """Validates and applies state transitions, locks, and handoffs."""

    def __init__(self, registry: Optional[DiscordRegistry] = None):
        self._config = get_config()
        self._registry = registry or DiscordRegistry()

    def can_claim(self, slug: str, role: str) -> tuple[bool, str]:
        """
        Validate claim: role must match current_owner, no other lock.
        Returns (ok, message).
        """
        state = self._read_feature_state(slug)
        if not state:
            return False, f"Feature {slug} not found."

        lock = self._registry.get_lock(slug)
        if lock and lock.locked_by_role != role:
            return False, f"Feature locked by {lock.locked_by_role}. Cannot claim as {role}."

        if state.current_owner != role:
            return False, f"Current owner is {state.current_owner}. You must be current owner to claim."

        return True, ""

    def claim(self, slug: str, role: str) -> tuple[bool, str]:
        """
        Create lock for role. Must pass can_claim.
        Returns (ok, message).
        """
        ok, msg = self.can_claim(slug, role)
        if not ok:
            return False, msg

        from datetime import datetime, timezone

        lock = {"locked_by_role": role, "locked_at": datetime.now(timezone.utc).isoformat()}
        from .models import LockState

        self._registry.set_lock(slug, LockState(**lock))
        return True, f"Claimed {slug} as {role}."

    def can_handoff(
        self,
        slug: str,
        from_role: str,
        next_role: str,
    ) -> tuple[bool, str]:
        """
        Validate handoff: no blockers, from_role owns lock.
        Returns (ok, message).
        """
        state = self._read_feature_state(slug)
        if not state:
            return False, f"Feature {slug} not found."

        if state.blocking_issues:
            return False, f"Feature has {len(state.blocking_issues)} blocker(s). Resolve before handoff."

        lock = self._registry.get_lock(slug)
        if not lock:
            return False, "No active lock. Use /claim first."
        if lock.locked_by_role != from_role:
            return False, f"Lock held by {lock.locked_by_role}. Cannot hand off as {from_role}."

        if next_role not in self._config.allowed_roles:
            return False, f"Unknown role: {next_role}. Allowed: {', '.join(self._config.allowed_roles)}"

        return True, ""

    def handoff(
        self,
        slug: str,
        from_role: str,
        next_role: str,
        summary: str,
    ) -> tuple[bool, str]:
        """
        Perform handoff: update feature file, release lock, create new owner state.
        Returns (ok, message).
        """
        ok, msg = self.can_handoff(slug, from_role, next_role)
        if not ok:
            return False, msg

        path = self.get_feature_path(slug)
        if not path or not path.exists():
            return False, "Feature file not found."

        writer = FeatureWriter(path)
        writer.set_owner(next_role)
        writer.set_next_owner_from_pipeline(next_role)
        writer.bump_version()
        writer.set_change_summary(summary)
        writer.set_last_updated(from_role)

        if not writer.write():
            return False, "Failed to update feature file."

        self._registry.clear_lock(slug)
        return True, f"Handed off {slug} from {from_role} to {next_role}."

    def validate_state_transition(
        self,
        current_status: str,
        next_status: str,
        force: bool = False,
    ) -> tuple[bool, str]:
        """
        Validate status transition. No skipping unless force.
        Returns (ok, message).
        """
        if current_status not in VALID_STATES:
            return False, f"Unknown status: {current_status}"

        if next_status not in VALID_STATES:
            return False, f"Unknown status: {next_status}"

        if force:
            return True, ""

        idx_current = VALID_STATES.index(current_status)
        idx_next = VALID_STATES.index(next_status)
        if idx_next != idx_current + 1:
            return False, f"Invalid transition: {current_status} -> {next_status}. Use force to skip."

        return True, ""

    def _read_feature_state(self, slug: str) -> Optional[FeatureState]:
        """Load feature state from file or registry path."""
        path = self.get_feature_path(slug)
        if path and path.exists():
            return parse_feature(path)
        return None

    def get_feature_path(self, slug: str):
        """Return path to feature file for slug."""
        from pathlib import Path

        from .feature_writer import get_feature_path as _get_feature_path

        return _get_feature_path(self._config.features_dir, slug)

    def register_in_feature_registry(self, slug: str, feature_type: str, title: str) -> None:
        """Add or update entry in FeatureRegistry.json."""
        import json

        path = self._config.feature_registry_path
        entries = []
        if path.exists():
            try:
                with open(path, encoding="utf-8") as f:
                    entries = json.load(f)
            except (json.JSONDecodeError, OSError):
                entries = []
        if not isinstance(entries, list):
            entries = []
        # Upsert: update if slug exists
        found = False
        for e in entries:
            if isinstance(e, dict) and e.get("slug") == slug:
                e.update(
                    {
                        "slug": slug,
                        "type": feature_type,
                        "status": "concept",
                        "owner": "Design",
                        "priority": "high",
                        "last_updated": None,
                    }
                )
                found = True
                break
        if not found:
            entries.append(
                {
                    "slug": slug,
                    "type": feature_type,
                    "status": "concept",
                    "owner": "Design",
                    "priority": "high",
                    "last_updated": None,
                }
            )
        with open(path, "w", encoding="utf-8") as f:
            json.dump(entries, f, indent=2)

    def add_blocker(self, slug: str, message: str) -> tuple[bool, str]:
        """Add blocker to feature. Returns (ok, message)."""
        from .feature_writer import add_blocker as _add_blocker

        path = self.get_feature_path(slug)
        if not path or not path.exists():
            return False, f"Feature {slug} not found."
        try:
            _add_blocker(path, message)
            return True, f"Blocker added to {slug}."
        except Exception as e:
            return False, str(e)

    def set_approval(self, slug: str, approval_type: str, decision: str, notes: Optional[str] = None) -> tuple[bool, str]:
        """Update approval in feature. Returns (ok, message)."""
        from .feature_writer import set_approval as _set_approval

        path = self.get_feature_path(slug)
        if not path or not path.exists():
            return False, f"Feature {slug} not found."
        if approval_type not in self._config.approval_types:
            return False, f"Invalid approval type: {approval_type}"
        try:
            _set_approval(path, approval_type, decision, notes)
            return True, f"Approval {approval_type} = {decision}."
        except Exception as e:
            return False, str(e)
