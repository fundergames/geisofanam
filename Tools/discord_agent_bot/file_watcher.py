"""
File watcher for Assets/Docs/Features/*.md.
Detects meaningful changes and syncs to Discord threads.
"""

import hashlib
import logging
import time
from pathlib import Path

from watchdog.events import FileSystemEventHandler
from watchdog.observers import Observer

from .config import get_config
from .feature_parser import parse_feature
from .registry import DiscordRegistry
from .discord_formatter import format_feature_update
from .models import DiscordSyncEvent

logger = logging.getLogger(__name__)


def _state_snapshot_hash(data: dict) -> str:
    """Simple hash of key sync-relevant fields for deduplication."""
    keys = [
        "status", "current_owner", "next_owner",
        "blocking_issues", "approvals", "version", "change_summary",
    ]
    subset = {k: data.get(k) for k in keys if k in data}
    blob = str(sorted(subset.items())).encode()
    return hashlib.sha256(blob).hexdigest()


class FeatureFileHandler(FileSystemEventHandler):
    """Handles feature file changes and triggers Discord sync."""

    def __init__(
        self,
        project_root: Path,
        registry: DiscordRegistry,
        discord_callback,
    ):
        self.project_root = project_root
        self.registry = registry
        self.discord_callback = discord_callback
        self._cache: dict[str, str] = {}  # path -> last_hash
        self._debounce_sec = 1.0
        self._last_event: dict[str, float] = {}  # path -> timestamp

    def _feature_path(self, path: Path) -> Path | None:
        cfg = get_config()
        features_dir = Path(cfg.features_path)
        if not features_dir.is_absolute():
            features_dir = self.project_root / features_dir
        try:
            path = path.resolve()
            path.relative_to(features_dir)
        except ValueError:
            return None
        if path.suffix.lower() != ".md" or path.name.startswith("_"):
            return None
        return path

    def _emit_sync(self, path: Path) -> None:
        """Parse feature, compare to cache, post to Discord if meaningful."""
        fp = path.resolve()
        slug = fp.stem
        entry = self.registry.get_entry(slug)
        if not entry or not entry.thread_id:
            return

        try:
            state = parse_feature(fp)
        except Exception as e:
            logger.warning("Parse failed for %s: %s", fp, e)
            return

        snapshot = {
            "status": state.status,
            "current_owner": state.current_owner,
            "next_owner": state.next_owner,
            "blocking_issues": state.blocking_issues or [],
            "approvals": state.approvals or {},
            "version": state.version,
            "change_summary": state.change_summary or "",
        }
        h = _state_snapshot_hash(snapshot)
        cached = self._cache.get(str(fp))
        if cached == h:
            return
        self._cache[str(fp)] = h

        # Avoid duplicate posts from rapid edits
        now = time.time()
        last = self._last_event.get(str(fp), 0)
        if now - last < self._debounce_sec:
            return
        self._last_event[str(fp)] = now

        event = DiscordSyncEvent(
            slug=slug,
            event_type="file_change",
            thread_id=entry.thread_id,
            channel_id=entry.channel_id,
            snapshot=snapshot,
        )
        if self.discord_callback:
            self.discord_callback(event)


    def on_modified(self, event):
        if event.is_directory:
            return
        path = Path(event.src_path)
        fp = self._feature_path(path)
        if not fp:
            return
        self._emit_sync(fp)


def _noop_callback(_event: DiscordSyncEvent) -> None:
    """Placeholder when bot not running in same process."""
    pass


def start_file_watcher(
    project_root: Path,
    bot=None,
) -> Observer:
    """
    Start watching feature files. If bot is provided, it will post updates
    to Discord threads. Otherwise uses a no-op callback.
    """
    cfg = get_config()
    features_dir = Path(cfg.features_path)
    if not features_dir.is_absolute():
        features_dir = project_root / features_dir

    if not features_dir.exists():
        logger.warning("Features dir does not exist: %s", features_dir)
        features_dir.mkdir(parents=True, exist_ok=True)

    registry = DiscordRegistry()
    callback = _noop_callback
    if bot is not None and hasattr(bot, "orchestrator"):
        # TODO: wire orchestrator -> Discord post when running in same process
        # For v1, we use a simple approach: bot holds registry; watcher needs
        # to post. We pass a callback that uses bot's HTTP session.
        def _post_to_thread(evt: DiscordSyncEvent):
            try:
                if hasattr(bot, "loop") and bot.loop and bot.is_ready() and evt.snapshot:
                    import asyncio
                    asyncio.run_coroutine_threadsafe(
                        _send_update(bot, evt, registry),
                        bot.loop,
                    )
            except Exception as e:
                logger.warning("Failed to post sync to Discord: %s", e)

        async def _send_update(b, evt, reg):
            if not evt.snapshot or not evt.thread_id:
                return
            try:
                thread = await b.fetch_channel(evt.thread_id)
                if thread:
                    msg = format_feature_update(
                        evt.slug,
                        evt.snapshot.get("status", "?"),
                        evt.snapshot.get("current_owner", "?"),
                        evt.snapshot.get("next_owner"),
                        evt.snapshot.get("version", 0),
                        evt.snapshot.get("change_summary", ""),
                        evt.snapshot.get("blocking_issues", []),
                    )
                    await thread.send(msg)
                    reg.update_sync(evt.slug, evt.snapshot.get("version", 0), evt.snapshot)
            except Exception as e:
                logger.warning("File watcher: could not post to thread: %s", e)

        callback = _post_to_thread

    handler = FeatureFileHandler(project_root, registry, callback)
    observer = Observer()
    observer.schedule(handler, str(features_dir), recursive=False)
    return observer
