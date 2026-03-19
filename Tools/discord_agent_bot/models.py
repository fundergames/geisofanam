"""Data models for the Discord coordination layer."""

from dataclasses import dataclass, field
from datetime import datetime
from typing import Any, Optional


@dataclass
class FeatureState:
    """Parsed feature metadata from frontmatter."""

    slug: str
    path: str
    status: str
    current_owner: str
    next_owner: Optional[str]
    mode: str
    concept_locked: bool
    approvals: dict[str, str]
    blocking_issues: list[str]
    assumptions: list[str]
    risks: list[str]
    version: int
    last_updated_by: Optional[str]
    last_updated_at: Optional[str]
    change_summary: Optional[str]
    title: Optional[str] = None
    raw_frontmatter: dict[str, Any] = field(default_factory=dict)


@dataclass
class ApprovalState:
    """Approval state for a single approval type."""

    approval_type: str
    status: str  # pending | approved | rejected
    notes: Optional[str] = None


@dataclass
class LockState:
    """Active ownership lock for a feature."""

    locked_by_role: str
    locked_at: str  # ISO datetime


@dataclass
class RegistryEntry:
    """Registry entry mapping feature slug to Discord resources."""

    slug: str
    feature_path: str
    thread_id: Optional[int] = None
    channel_id: Optional[int] = None
    lock: Optional["LockState"] = None
    last_synced_version: Optional[int] = None
    last_state_snapshot: Optional[dict] = None


@dataclass
class DiscordSyncEvent:
    """Event to sync from feature file change to Discord."""

    slug: str
    event_type: str = "file_change"  # status_change, owner_change, blocker_added, etc.
    thread_id: Optional[int] = None
    channel_id: Optional[int] = None
    snapshot: Optional[dict[str, Any]] = None  # For file watcher: new state
    old_state: Optional[dict[str, Any]] = None
    new_state: Optional[dict[str, Any]] = None
    message: Optional[str] = None
