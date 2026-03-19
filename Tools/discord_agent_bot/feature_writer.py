"""Safely update feature markdown frontmatter while preserving body content."""

import re
import shutil
import tempfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Optional, Union

from .config import get_config
from .feature_parser import read_feature_file
from .models import FeatureState


def _serialize_frontmatter(fm: dict[str, Any]) -> str:
    import yaml

    return yaml.dump(
        fm,
        default_flow_style=False,
        allow_unicode=True,
        sort_keys=False,
    )


def _write_feature_atomic(path: Path, frontmatter: dict[str, Any], body: str) -> None:
    """Write feature file atomically via temp file + rename."""
    fm_str = _serialize_frontmatter(frontmatter)
    content = f"---\n{fm_str}---\n\n{body}\n"
    fd, tmp = tempfile.mkstemp(
        dir=path.parent,
        prefix=f".{path.stem}_",
        suffix=".md.tmp",
    )
    try:
        with open(fd, "w", encoding="utf-8") as f:
            f.write(content)
        Path(tmp).replace(path)
    except Exception:
        Path(tmp).unlink(missing_ok=True)
        raise


def update_frontmatter(
    path: Path,
    updates: dict[str, Any],
    body: Optional[str] = None,
) -> None:
    """
    Update selected frontmatter keys. Preserves body unless overridden.
    """
    fm, existing_body = read_feature_file(path)
    for k, v in updates.items():
        fm[k] = v
    body = body if body is not None else existing_body
    _write_feature_atomic(path, fm, body)


def set_owner(
    path: Path,
    current_owner: str,
    next_owner: Optional[str] = None,
    change_summary: str = "",
) -> None:
    """Set current_owner and optionally next_owner, bump version, update timestamp."""
    fm, body = read_feature_file(path)
    fm["current_owner"] = current_owner
    if next_owner is not None:
        fm["next_owner"] = next_owner
    fm["version"] = int(fm.get("version", 1)) + 1
    fm["last_updated_at"] = datetime.now(timezone.utc).isoformat()
    fm["last_updated_by"] = "discord_bot"
    fm["change_summary"] = change_summary or "Handoff via Discord"
    _write_feature_atomic(path, fm, body)


def set_status(path: Path, status: str, change_summary: str = "") -> None:
    """Set status and bump version."""
    fm, body = read_feature_file(path)
    fm["status"] = status
    fm["version"] = int(fm.get("version", 1)) + 1
    fm["last_updated_at"] = datetime.now(timezone.utc).isoformat()
    fm["last_updated_by"] = "discord_bot"
    fm["change_summary"] = change_summary or f"Status -> {status}"
    _write_feature_atomic(path, fm, body)


def add_blocker(path: Path, message: str) -> None:
    """Append blocker to blocking_issues, bump version."""
    fm, body = read_feature_file(path)
    blocking = list(fm.get("blocking_issues") or fm.get("blockers") or [])
    blocking.append(message)
    fm["blocking_issues"] = blocking
    fm["version"] = int(fm.get("version", 1)) + 1
    fm["last_updated_at"] = datetime.now(timezone.utc).isoformat()
    fm["last_updated_by"] = "discord_bot"
    fm["change_summary"] = f"Blocker added: {message[:80]}{'...' if len(message) > 80 else ''}"
    _write_feature_atomic(path, fm, body)


def set_approval(path: Path, approval_type: str, decision: str, notes: Optional[str] = None) -> None:
    """Update single approval in frontmatter."""
    fm, body = read_feature_file(path)
    approvals = dict(fm.get("approvals") or {})
    approvals[approval_type] = decision
    fm["approvals"] = approvals
    if notes and decision == "rejected":
        blocking = list(fm.get("blocking_issues") or [])
        blocking.append(f"Approval rejected ({approval_type}): {notes}")
        fm["blocking_issues"] = blocking
    fm["version"] = int(fm.get("version", 1)) + 1
    fm["last_updated_at"] = datetime.now(timezone.utc).isoformat()
    fm["last_updated_by"] = "discord_bot"
    fm["change_summary"] = f"Approval {approval_type}: {decision}"
    _write_feature_atomic(path, fm, body)


def bump_version(path: Path, change_summary: str) -> None:
    """Bump version and set change_summary."""
    fm, body = read_feature_file(path)
    fm["version"] = int(fm.get("version", 1)) + 1
    fm["last_updated_at"] = datetime.now(timezone.utc).isoformat()
    fm["last_updated_by"] = "discord_bot"
    fm["change_summary"] = change_summary
    _write_feature_atomic(path, fm, body)


def set_change_summary(path: Path, summary: str) -> None:
    """Set change_summary and update timestamp."""
    fm, body = read_feature_file(path)
    fm["change_summary"] = summary
    fm["last_updated_at"] = datetime.now(timezone.utc).isoformat()
    fm["last_updated_by"] = "discord_bot"
    _write_feature_atomic(path, fm, body)


def get_feature_path(features_dir: Union[Path, str], slug: str) -> Path:
    """Return full path to feature file for slug."""
    return Path(features_dir) / f"{slug}.md"


def ensure_feature_file(
    slug: str,
    feature_type: str,
    title: Optional[str] = None,
) -> tuple[Optional[Path], bool]:
    """
    Create feature file from template if missing.
    Returns (path, created) - path is None if failed.
    """
    cfg = get_config()
    path = get_feature_path(cfg.features_dir, slug)
    if path.exists():
        return path, False

    template = cfg.template_paths.get(feature_type) or cfg.default_template
    if not template or not Path(template).exists():
        return None, False

    content = Path(template).read_text(encoding="utf-8")
    if title:
        content = content.replace("[Feature name]", title)
        content = content.replace("[Feature Name]", title)
        content = content.replace("[Enemy: Feature Name]", f"Enemy: {title}")
        content = content.replace("[Weapon: Feature Name]", f"Weapon: {title}")
        content = content.replace("[Enemy Name]", title)
        content = content.replace("[Weapon Name]", title)
    else:
        display = slug.replace("-", " ").title()
        content = content.replace("[Feature name]", display)
        content = content.replace("[Feature Name]", display)
        content = content.replace("[Enemy: Feature Name]", f"Enemy: {display}")
        content = content.replace("[Weapon: Feature Name]", f"Weapon: {display}")
        content = content.replace("[Enemy Name]", display)
        content = content.replace("[Weapon Name]", display)

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")
    return path, True


class FeatureWriter:
    """Object-oriented wrapper for feature file updates."""

    def __init__(self, path: Union[Path, str]):
        self._path = Path(path)
        self._fm, self._body = read_feature_file(self._path)

    def set_owner(self, current_owner: str) -> "FeatureWriter":
        self._fm["current_owner"] = current_owner
        return self

    def set_next_owner(self, next_owner: Optional[str]) -> "FeatureWriter":
        self._fm["next_owner"] = next_owner
        return self

    def set_next_owner_from_pipeline(self, current_role: str) -> "FeatureWriter":
        """Set next_owner to the role after current_role in pipeline."""
        pipeline = get_config().role_pipeline
        if current_role in pipeline:
            idx = pipeline.index(current_role)
            if idx + 1 < len(pipeline):
                self._fm["next_owner"] = pipeline[idx + 1]
            else:
                self._fm["next_owner"] = None
        return self

    def set_change_summary(self, summary: str) -> "FeatureWriter":
        self._fm["change_summary"] = summary
        return self

    def set_last_updated(self, by: str) -> "FeatureWriter":
        self._fm["last_updated_by"] = by
        self._fm["last_updated_at"] = datetime.now(timezone.utc).isoformat()
        return self

    def bump_version(self) -> "FeatureWriter":
        self._fm["version"] = int(self._fm.get("version", 1)) + 1
        return self

    def write(self) -> bool:
        try:
            _write_feature_atomic(self._path, self._fm, self._body)
            return True
        except Exception:
            return False


