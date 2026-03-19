"""Parse feature markdown files and extract frontmatter metadata."""

import re
from pathlib import Path
from typing import Any, Optional

from .models import FeatureState


def read_feature_file(path: "Path | str") -> tuple[dict[str, Any], str]:
    """
    Read a feature file and return (frontmatter, body) tuple.
    Returns empty dict and body if frontmatter is missing.
    """
    path = Path(path)
    content = path.read_text(encoding="utf-8")
    match = re.match(r"^---\s*\n(.*?)\n---\s*\n(.*)$", content, re.DOTALL)
    if not match:
        return {}, content

    import yaml

    try:
        fm = yaml.safe_load(match.group(1))
        return fm if fm else {}, match.group(2).strip()
    except Exception:
        return {}, content


def parse_feature(path: "Path | str", slug: Optional[str] = None) -> Optional[FeatureState]:
    """
    Parse a feature file and return a FeatureState, or None if invalid.

    If slug is not provided, it is inferred from the filename (without .md).
    """
    if not path.exists():
        return None

    frontmatter, _ = read_feature_file(path)
    if not frontmatter:
        return None

    slug = slug or path.stem
    approvals = frontmatter.get("approvals") or {}
    if isinstance(approvals, dict):
        approvals = {k: str(v) for k, v in approvals.items()}
    else:
        approvals = {}

    blocking = frontmatter.get("blocking_issues") or frontmatter.get("blockers") or []
    if isinstance(blocking, str):
        blocking = [blocking]
    blocking = [str(b) for b in blocking]

    assumptions = frontmatter.get("assumptions") or []
    if isinstance(assumptions, str):
        assumptions = [assumptions]
    assumptions = [str(a) for a in assumptions]

    risks = frontmatter.get("risks") or []
    if isinstance(risks, str):
        risks = [risks]
    risks = [str(r) for r in risks]

    return FeatureState(
        slug=slug,
        path=str(path),
        status=str(frontmatter.get("status", "concept")),
        current_owner=str(frontmatter.get("current_owner", "Design")),
        next_owner=str(frontmatter.get("next_owner")) if frontmatter.get("next_owner") else None,
        mode=str(frontmatter.get("mode", "exploration")),
        concept_locked=bool(frontmatter.get("concept_locked", False)),
        approvals=approvals,
        blocking_issues=blocking,
        assumptions=assumptions,
        risks=risks,
        version=int(frontmatter.get("version", 1)),
        last_updated_by=str(frontmatter.get("last_updated_by")) if frontmatter.get("last_updated_by") else None,
        last_updated_at=str(frontmatter.get("last_updated_at")) if frontmatter.get("last_updated_at") else None,
        change_summary=str(frontmatter.get("change_summary")) if frontmatter.get("change_summary") else None,
        title=str(frontmatter.get("title")) if frontmatter.get("title") else None,
        raw_frontmatter=frontmatter,
    )
