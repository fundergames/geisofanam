"""
Discord registry: maps feature slugs to thread IDs and lock state.
Feature files remain canonical; registry is coordination metadata.
"""

from __future__ import annotations

import json
import logging
from dataclasses import asdict
from pathlib import Path
from typing import Optional

from .config import get_config
from .models import LockState, RegistryEntry

logger = logging.getLogger(__name__)


class DiscordRegistry:
    """Maintains feature slug -> Discord thread mapping and lock state."""

    def __init__(self, registry_path: Optional[Path] = None):
        cfg = get_config()
        self._path = registry_path or Path(cfg.registry_path)
        self._path.parent.mkdir(parents=True, exist_ok=True)
        self._entries: dict[str, dict] = {}
        self._load()

    def _load(self) -> None:
        """Load registry from disk. Resilient if file missing or invalid."""
        if not self._path.exists():
            self._entries = {}
            return
        try:
            with open(self._path, encoding="utf-8") as f:
                data = json.load(f)
            self._entries = data.get("features", {})
            if not isinstance(self._entries, dict):
                self._entries = {}
        except (json.JSONDecodeError, OSError) as e:
            logger.warning("Registry load failed: %s. Starting fresh.", e)
            self._entries = {}

    def _save(self) -> None:
        """Atomically write registry to disk."""
        payload = {"features": self._entries, "version": 1}
        tmp = self._path.with_suffix(".json.tmp")
        try:
            with open(tmp, "w", encoding="utf-8") as f:
                json.dump(payload, f, indent=2)
            tmp.replace(self._path)
        except OSError as e:
            logger.error("Registry save failed: %s", e)

    def get_entry(self, slug: str) -> Optional[RegistryEntry]:
        """Return RegistryEntry for slug or None."""
        raw = self._entries.get(slug)
        if not raw:
            return None
        try:
            lock = None
            if raw.get("lock"):
                lock = LockState(
                    locked_by_role=raw["lock"]["locked_by_role"],
                    locked_at=raw["lock"]["locked_at"],
                )
            return RegistryEntry(
                slug=raw.get("slug", slug),
                feature_path=raw.get("feature_path", ""),
                thread_id=int(raw.get("thread_id", 0)) or None,
                channel_id=int(raw.get("channel_id", 0)) or None,
                lock=lock,
                last_synced_version=int(raw.get("last_synced_version", 0)) or None,
                last_state_snapshot=raw.get("last_state_snapshot"),
            )
        except (KeyError, TypeError, ValueError) as e:
            logger.warning("Invalid registry entry for %s: %s", slug, e)
            return None

    def set_entry(
        self,
        slug: str,
        feature_path: str,
        thread_id: Optional[int] = None,
        channel_id: Optional[int] = None,
        lock: Optional[LockState] = None,
        last_synced_version: Optional[int] = None,
        last_state_snapshot: Optional[dict] = None,
    ) -> None:
        """Create or update a registry entry."""
        entry = self._entries.setdefault(slug, {})
        entry["slug"] = slug
        entry["feature_path"] = feature_path
        if thread_id is not None:
            entry["thread_id"] = thread_id
        if channel_id is not None:
            entry["channel_id"] = channel_id
        if lock is not None:
            entry["lock"] = asdict(lock)
        elif "lock" in entry and lock is None:
            entry.pop("lock", None)
        if last_synced_version is not None:
            entry["last_synced_version"] = last_synced_version
        if last_state_snapshot is not None:
            entry["last_state_snapshot"] = last_state_snapshot
        self._save()

    def set_lock(self, slug: str, lock: LockState) -> None:
        """Update lock for slug."""
        e = self._entries.get(slug)
        if not e:
            logger.warning("Cannot set lock: no entry for %s", slug)
            return
        e["lock"] = asdict(lock)
        self._save()

    def clear_lock(self, slug: str) -> None:
        """Remove lock for slug."""
        e = self._entries.get(slug)
        if e:
            e.pop("lock", None)
            self._save()

    def get_lock(self, slug: str) -> Optional[LockState]:
        """Return current lock for slug or None."""
        entry = self.get_entry(slug)
        return entry.lock if entry else None

    def update_sync(
        self,
        slug: str,
        last_synced_version: int,
        last_state_snapshot: dict,
    ) -> None:
        """Update last synced state for deduplication."""
        e = self._entries.get(slug)
        if e:
            e["last_synced_version"] = last_synced_version
            e["last_state_snapshot"] = last_state_snapshot
            self._save()

    def list_slugs(self) -> list[str]:
        """Return all registered slugs."""
        return list(self._entries.keys())
