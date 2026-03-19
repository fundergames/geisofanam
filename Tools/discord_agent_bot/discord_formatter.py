"""
Centralized Discord message formatting.
Keeps message structure consistent across commands and sync events.
"""

from typing import Optional

from .models import FeatureState, LockState


def format_feature_update(
    slug: str,
    status: str,
    current_owner: str,
    next_owner: Optional[str],
    version: int,
    change_summary: str,
    blocking_issues: list,
) -> str:
    """Format thread update message."""
    blockers = "none" if not blocking_issues else "\n".join(f"- {b}" for b in blocking_issues)
    next_str = next_owner or "—"
    return f"""**Feature Update:** `{slug}`
Status: `{status}`
Current Owner: {current_owner}
Next Owner: {next_str}
Version: {version}
Change: {change_summary}
Blockers: {blockers}"""


def format_handoff(
    slug: str,
    from_role: str,
    to_role: str,
    status: str,
    summary: str,
) -> str:
    """Format handoff message."""
    return f"""**Handoff Complete**
Feature: `{slug}`
From: {from_role}
To: {to_role}
Status: `{status}`
Summary: {summary}"""


def format_blocker(
    slug: str,
    owner: str,
    message: str,
) -> str:
    """Format blocker raised message."""
    return f"""**Blocker Raised**
Feature: `{slug}`
Owner: {owner}
Issue: {message}"""


def format_claim(slug: str, role: str) -> str:
    """Format claim message."""
    return f"**Claimed** `{slug}` by **{role}**."


def format_status_card(state: FeatureState, thread_link: Optional[str] = None) -> str:
    """Compact status card for /summary or /feature_status."""
    blockers = "none" if not state.blocking_issues else "\n".join(f"- {b}" for b in state.blocking_issues)
    approvals_str = ", ".join(f"{k}: {v}" for k, v in (state.approvals or {}).items())
    lines = [
        f"**{state.title or 'Untitled'}** (`{state.slug or '?'}`)",
        f"Status: `{state.status}`",
        f"Owner: {state.current_owner} → Next: {state.next_owner or '—'}",
        f"Version: {state.version}",
        f"Change: {state.change_summary or '—'}",
        f"Blockers: {blockers}",
        f"Approvals: {approvals_str}",
    ]
    if thread_link:
        lines.append(f"Thread: {thread_link}")
    return "\n".join(lines)


def format_approval_request(slug: str, approval_type: str, summary: str) -> str:
    """Format approval request for #agent-approvals."""
    return f"""**Approval Request**
Feature: `{slug}`
Type: {approval_type}
Summary: {summary}"""


def format_approval_result(
    slug: str,
    approval_type: str,
    decision: str,
    notes: Optional[str] = None,
) -> str:
    """Format approval decision result."""
    out = f"""**Approval: {approval_type}** (`{slug}`)
Decision: **{decision}**"""
    if notes:
        out += f"\nNotes: {notes}"
    return out


def format_validation_request(slug: str, validator: str) -> str:
    """Format validation request (stub)."""
    return f"""**Validation Request**
Feature: `{slug}`
Validator: `{validator}`
Status: *TODO - validator hook not yet integrated*"""


def format_validation_result(slug: str, validator: str, passed: bool, details: str = "") -> str:
    """Format validation result."""
    status = "✅ passed" if passed else "❌ failed"
    out = f"**Validation:** `{validator}` for `{slug}` — {status}"
    if details:
        out += f"\n{details}"
    return out
