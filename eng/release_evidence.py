#!/usr/bin/env python3
"""Release evidence governance for package inventory, sealed manifests, and budgets."""

from __future__ import annotations

import argparse
import datetime as dt
import fnmatch
import hashlib
import html
import json
import os
import pathlib
import re
import sys
import xml.etree.ElementTree as ET
from typing import Any


PACKAGE_COLLAPSE_MARKER = "<!-- frontcomposer:package-count-collapse -->"
MAX_FIELD = 600
SHA256_RE = re.compile(r"^[a-fA-F0-9]{64}$")
REQUIRED_ROW_FIELDS = [
    "package_id",
    "version",
    "commit_sha",
    "artifact_path",
    "checksum",
    "symbol_artifact",
    "sbom_component",
    "signing_status",
    "attestation_status",
    "publish_status",
]
# CR-12-4-P124 (round-6): split release-definition surface from fallback-invalidation
# surface to close A8/D15. `Directory.Packages.props` re-enters the drift-detection
# fingerprint (so post-seal package-version edits are caught), but is excluded from
# `fallback_invalidation_fingerprint_keys` so routine Dependabot bumps do NOT
# invalidate an active unsupported-attestation fallback (the D8 operational concern).
# Package-set drift specifically (rows in `release-package-inventory.json`) remains
# in BOTH surfaces and continues to invalidate fallbacks per AC34.
# CR-12-4-P103 (round-5): added `Directory.Build.targets` and `Directory.Build.props`
# because they encode symbol-package format, trim/IsTrimmable, and other release-relevant
# pack/sign policy. Files that are listed but missing on disk are simply skipped by
# `release_definition_fingerprints`.
RELEASE_DEFINITION_FILES = [
    ".github/workflows/release.yml",
    ".releaserc.json",
    "eng/release_evidence.py",
    "eng/release-package-inventory.json",
    "Directory.Build.props",
    "Directory.Build.targets",
    "Directory.Packages.props",
]
# CR-12-4-P124 (round-6): keys whose drift invalidates a `fallback-approved` release.
# Excludes `Directory.Packages.props` so transitive package-version bumps do not force
# re-approval. Drift in these files is still detected by `manifest_diagnostics` (drift
# detection) but does not trigger `fallback_complete` rejection.
FALLBACK_INVALIDATION_FILES = [
    ".github/workflows/release.yml",
    ".releaserc.json",
    "eng/release_evidence.py",
    "eng/release-package-inventory.json",
    "Directory.Build.props",
    "Directory.Build.targets",
]

# CR-12-4-D7 (round-5): the AC26 approval matrix is now a machine-readable constant
# emitted in `classify_release_payload` so consumers can enumerate the seven approval-
# dependent action types without parsing prose. Each entry binds: the action, the
# required approver, the mechanism (workflow_dispatch input vs sealed fallback record),
# the evidence that must be present, and whether the action is `blocking` (no fallback)
# or has an `approved-unsupported` style fallback.
# CR-12-4-P131/P136/P140 (round-6): normalized vocabulary, removed env-var-style
# internal naming from `mechanism`, and added `gate_id` to surface the three shared
# semantic-release gates so consumers can group-by gate without conflating actions.
# `effect` is normalized to `{"blocking", "blocking-with-fallback", "fallback"}`;
# the `fallback_action` field names the fallback variant when applicable.
APPROVAL_MATRIX = [
    {
        "action": "nuget-publish",
        "gate_id": "semantic-release-publish",
        "owner": "release-owner",
        "mechanism": "workflow-dispatch owner approval (release_owner_approved + release_approver inputs)",
        "evidence": "release-readiness.json: classification=ready or fallback-approved, publish_authorized=true",
        "effect": "blocking",
        "fallback_action": None,
    },
    {
        "action": "tag-and-changelog-push",
        "gate_id": "semantic-release-publish",
        "owner": "release-owner",
        "mechanism": "workflow-dispatch owner approval (release_owner_approved + release_approver inputs)",
        "evidence": "release-readiness.json: publish_authorized=true; semantic-release @semantic-release/git",
        "effect": "blocking",
        "fallback_action": None,
    },
    {
        "action": "github-release-create",
        "gate_id": "semantic-release-publish",
        "owner": "release-owner",
        "mechanism": "workflow-dispatch owner approval (release_owner_approved + release_approver inputs)",
        "evidence": "release-readiness.json: publish_authorized=true; semantic-release @semantic-release/github",
        "effect": "blocking",
        "fallback_action": None,
    },
    {
        "action": "attestation-upload",
        "gate_id": "attestation",
        "owner": "release-owner",
        "mechanism": "workflow attestations write permission + gh attestation verify",
        "evidence": "manifest.attestation_status=attested with attestation_bundle, OR fallback-approved",
        "effect": "blocking-with-fallback",
        "fallback_action": "attestation-fallback",
    },
    {
        "action": "attestation-fallback",
        "gate_id": "attestation",
        "owner": "release-owner",
        "mechanism": "ATTESTATION_UNSUPPORTED repository variable plus sealed fallback record (approver, expiry, fingerprint baseline)",
        "evidence": "attestation-unavailable.md plus sealed fallback_record (affected_artifact, approver, evidence, expires_at, reason, release_note_impact, reopen_event, scope, approved_against_fingerprints_sha256)",
        "effect": "fallback",
        "fallback_action": None,
    },
    {
        "action": "partial-publish-recovery",
        "gate_id": "partial-publish-recovery",
        "owner": "release-owner",
        "mechanism": "manual review of partial-publish-incident.json plus fresh workflow-dispatch with new tag",
        "evidence": "partial-publish-incident.json (failed_phase != none) plus reconciled NuGet/symbol state",
        "effect": "blocking",
        "fallback_action": None,
    },
    {
        "action": "rerun-after-failed-or-partial-release",
        "gate_id": "rerun",
        "owner": "release-owner",
        "mechanism": "fresh workflow-dispatch (run_attempt resets) or new tag",
        "evidence": "release-readiness.json: classification != rerun-review with explicit owner approval",
        "effect": "blocking",
        "fallback_action": None,
    },
]
BLOCKING_CHECKS = {
    "checksums_status": "valid",
    "helper_state": "success",
    "inventory_status": "valid",
    "paths_status": "normalized",
    "redaction_status": {"passed", "sanitized"},
    "sbom_status": "present",
    "semantic_release_state": "matches",
    "signing_status": "verified",
    "test_status": "passed",
    "timestamp_status": "verified",
}
DANGEROUS_EVIDENCE_PATTERNS = [
    re.compile(r"(?i)bearer\s+[A-Za-z0-9._~+/=-]+"),
    re.compile(r"\b(?:sk-|gh[opsur]_|github_pat_|xox[baprs]-)[-A-Za-z0-9_]{12,}\b"),
    re.compile(r"\b[A-Z]:[\\/][^ \r\n]+"),
    re.compile(r"(?<![\w/])/(?:home|Users|tmp|var)/[^ \r\n]+"),
    re.compile(r"(?i)\b(tenant|tenantid|user|userid|commandpayload)\s*[:=]\s*[^,; \r\n]+"),
    re.compile(r"^::", re.MULTILINE),
]
TRUTHY_LITERALS = {"true"}
FALSY_LITERALS = {"false"}
# Approval booleans accept the standard true/false serialization (used by GitHub Actions
# boolean inputs) PLUS the explicit approved/denied tokens. Drops the ambiguous
# 1/0/yes/no tokens that an operator typo could silently accept.
APPROVAL_TRUTHY = TRUTHY_LITERALS | {"approved"}
APPROVAL_FALSY = FALSY_LITERALS | {"denied"}
USER_FACING_EVIDENCE_FILES = {
    "concurrency-guard.json",
    "manifest-diagnostics.json",
    "package-inventory.json",
    "partial-publish-incident.json",
    "partial-publish-placeholder.json",
    "prior-release.json",
    "release-readiness.json",
    "release-verification.json",
    "test-results.json",
}
# CR-12-4-P94 (round-5): tool-generated evidence directories whose every file should be
# scanned for redaction. Globs are resolved by `read_bounded_evidence`; entries are
# relative to the evidence root. Each file is still subject to the per-file size cap
# in `read_bounded_evidence` and the aggregate cap in CR-12-4-P95.
USER_FACING_EVIDENCE_GLOBS = (
    "signing-verification.txt",
    "attestation-unavailable.md",
    "attestations/*.json",
    "sbom/**/*.json",
)
# CR-12-4-P95 (round-5): aggregate cap across all evidence files scanned by
# `read_bounded_evidence`. Per-file cap stays at 1 MB; aggregate cap of 8 MB keeps
# pathological evidence trees from exploding the canonical_sha256 over concatenated text.
EVIDENCE_AGGREGATE_BYTES = 8 * 1024 * 1024


def sanitize(value: Any, max_len: int = MAX_FIELD) -> str:
    text = "" if value is None else str(value)
    text = text.replace("\r", " ").replace("\n", " ")
    text = re.sub(r"(?i)bearer\s+[A-Za-z0-9._~+/=-]+", "Bearer [REDACTED]", text)
    text = re.sub(r"\b(?:sk-|gh[opsur]_|github_pat_|xox[baprs]-)[-A-Za-z0-9_]{12,}\b", "[REDACTED_TOKEN]", text)
    text = re.sub(r"\b[A-Z]:[\\/][^ ]+", "[LOCAL_PATH]", text)
    text = re.sub(r"(?<![\w/])/(?:home|Users|tmp|var)/[^ ]+", "[LOCAL_PATH]", text)
    text = re.sub(r"(?i)\b(tenant|tenantid|user|userid|commandpayload)\s*[:=]\s*[^,; ]+", r"\1=[REDACTED]", text)
    text = html.escape(text, quote=False).replace("|", "\\|")
    if text.startswith("::"):
        text = "\\" + text
    return text[:max_len] + ("..." if len(text) > max_len else "")


def read_json(path: str | pathlib.Path) -> Any:
    try:
        return json.loads(pathlib.Path(path).read_text(encoding="utf-8"))
    except Exception as exc:
        raise SystemExit(f"invalid JSON evidence {path}: {exc}") from exc


def write_json(path: str | pathlib.Path, payload: Any) -> None:
    """Write JSON atomically: serialize to a sibling .tmp file, then replace the target.

    A partial write followed by an error would otherwise leave a corrupt file that the
    next pipeline step would read; the tmp+replace pattern guarantees the target either
    holds the previous valid content or the complete new content.
    """
    # CR-12-4-P117 (round-5): `with_suffix(suffix + ".tmp")` mangles dot-prefixed names
    # (`/.hidden` → `/.tmp`, losing the original name). Construct the tmp sibling via
    # `parent / (name + ".tmp")` so any filename round-trips correctly.
    p = pathlib.Path(path)
    p.parent.mkdir(parents=True, exist_ok=True)
    tmp = p.parent / (p.name + ".tmp")
    tmp.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    os.replace(tmp, p)


def sha256_file(path: pathlib.Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def sha256_text(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def canonical_sha256(value: Any) -> str:
    canonical = json.dumps(value, sort_keys=True, separators=(",", ":"))
    return sha256_text(canonical)


def normalize_under_root(root: pathlib.Path, name: str) -> pathlib.Path:
    if os.path.isabs(name):
        raise SystemExit("evidence path must be relative")
    resolved_root = root.resolve()
    candidate = (resolved_root / name).resolve()
    if resolved_root != candidate and resolved_root not in candidate.parents:
        raise SystemExit("evidence path escapes approved root")
    return candidate


def require_trusted_context(args: argparse.Namespace) -> None:
    allowed_event = args.event_name in {"push", "schedule", "workflow_dispatch", "workflow_run"}
    protected = _cli_bool(args.ref_protected, field="ref_protected")
    trusted_ref = args.ref in {"refs/heads/main", "main"} or protected
    fork = _cli_bool(args.from_fork, field="from_fork")
    if not allowed_event or not trusted_ref or fork:
        raise SystemExit("release write rejected: trusted release/main context required")


def project_property(project: pathlib.Path, name: str) -> str:
    root = ET.parse(project).getroot()
    for prop in root.findall(".//PropertyGroup/" + name):
        if prop.text and prop.text.strip():
            return prop.text.strip()
    return ""


def package_id(project: pathlib.Path) -> str:
    return project_property(project, "PackageId") or project.stem


def is_packable(project: pathlib.Path) -> bool:
    value = project_property(project, "IsPackable")
    return value.lower() != "false"


# CR-12-4-P129 (round-6): two-tier denylist. `_INVENTORY_FIRST_COMPONENT_DENYLIST`
# matches only when the FIRST path component equals one of the entries — this fixes
# the prior `set(parts) & DENYLIST` bug that silently excluded any path containing
# `src`/`bin`/etc. as a nested component (e.g., `tools/src/foo.csproj`). The
# `_INVENTORY_ANY_COMPONENT_DENYLIST` keeps the broader pattern for genuine build
# artifact directories that may appear at any depth (`bin/`, `obj/`, etc.).
# `samples`, `tests`, `perf`, `tools` are now explicitly excluded from the
# unexpected-packable scan because they are first-class non-packable surfaces;
# previously the scan relied on every csproj declaring `<IsPackable>false</IsPackable>`,
# which `dotnet new` does not emit by default.
_INVENTORY_FIRST_COMPONENT_DENYLIST = {
    "src",          # primary inventory surface — scanned separately by discover_projects
    "samples",      # sample/demo projects
    "tests",        # test projects
    "perf",         # benchmark projects
    "tools",        # developer tooling
    "_bmad-output",
    "_bmad",
    "artifacts",    # generated docs snippets etc.
    "release-evidence",
    "nupkgs",
    "nupkgs-signed",
}
_INVENTORY_ANY_COMPONENT_DENYLIST = {
    "bin",
    "obj",
    "node_modules",
    ".git",
    "testresults",
}


def discover_projects(repo: pathlib.Path) -> list[pathlib.Path]:
    src = repo / "src"
    projects = []
    for project in src.rglob("*.csproj"):
        parts = {p.lower() for p in project.parts}
        if "bin" not in parts and "obj" not in parts:
            projects.append(project)
    return sorted(projects, key=lambda p: str(p.relative_to(repo)).replace("\\", "/"))


def _is_submodule_root(repo: pathlib.Path, directory: pathlib.Path) -> bool:
    """Return True when `directory` lies inside one of the repo's git submodule roots.

    CR-12-4-P139 (round-6): parse `.gitmodules` via `configparser` rather than naive
    `startswith("path")` + `partition("=")`. The prior parsing matched any key
    starting with "path" (e.g., `pathological = ...`), did not strip surrounding
    quotes, and ignored trailing comments.
    """
    import configparser
    gitmodules = repo / ".gitmodules"
    if not gitmodules.exists():
        return False
    try:
        text = gitmodules.read_text(encoding="utf-8")
    except OSError:
        return False
    parser = configparser.ConfigParser()
    parser.optionxform = str  # preserve case for key names
    try:
        parser.read_string(text)
    except configparser.Error:
        return False
    submodule_paths: set[str] = set()
    for section in parser.sections():
        path_value = parser.get(section, "path", fallback="").strip().strip('"').strip("'")
        if path_value:
            submodule_paths.add(path_value.replace("\\", "/"))
    rel = str(directory.relative_to(repo)).replace("\\", "/")
    return rel in submodule_paths or any(rel.startswith(path + "/") for path in submodule_paths)


def discover_unexpected_packable_outside_src(repo: pathlib.Path) -> list[pathlib.Path]:
    """Scan the repo for packable projects that should NOT be packable.

    CR-12-4-P104 (round-5): the primary inventory diff (`discover_projects`) is keyed on
    `src/` to preserve the expected-inventory contract. AC4 also requires that no
    project OUTSIDE `src/` declares `<IsPackable>true</IsPackable>`; this scan reports
    any such drift so the inventory step fails closed. Build artifacts, submodule
    roots, and generated trees are excluded.
    """
    unexpected = []
    for project in repo.rglob("*.csproj"):
        parts_lower = [p.lower() for p in project.relative_to(repo).parts]
        # CR-12-4-P129 (round-6): first-component check (anchored) plus any-component
        # check for build-artifact directories. See denylist comment above for
        # rationale.
        if parts_lower and parts_lower[0] in _INVENTORY_FIRST_COMPONENT_DENYLIST:
            continue
        if set(parts_lower) & _INVENTORY_ANY_COMPONENT_DENYLIST:
            continue
        if _is_submodule_root(repo, project.parent):
            continue
        if not is_packable(project):
            continue
        unexpected.append(project)
    return sorted(unexpected, key=lambda p: str(p.relative_to(repo)).replace("\\", "/"))


def is_placeholder(value: Any) -> bool:
    text = str(value or "")
    return not text or text.startswith("pending-")


def looks_like_sha256(value: Any) -> bool:
    return bool(SHA256_RE.fullmatch(str(value or "")))


def parse_strict_bool(value: Any, *, field: str, default: bool = False) -> tuple[bool, str | None]:
    """Strict security-flag boolean (true/false only, case-insensitive). Empty -> default."""
    if isinstance(value, bool):
        return value, None
    if value is None:
        return default, None
    text = str(value).strip().lower()
    if text == "":
        return default, f"{field} must be true or false; actual=<empty>"
    if text in TRUTHY_LITERALS:
        return True, None
    if text in FALSY_LITERALS:
        return False, None
    return default, f"{field} must be true or false; actual={sanitize(value)}"


def parse_approval_bool(value: Any, *, field: str, default: bool = False) -> tuple[bool, str | None]:
    """Approval-domain boolean (true/false/yes/no/1/0/approved/denied)."""
    if isinstance(value, bool):
        return value, None
    text = "" if value is None else str(value).strip().lower()
    if text == "":
        return default, None
    if text in APPROVAL_TRUTHY:
        return True, None
    if text in APPROVAL_FALSY:
        return False, None
    accepted = sorted(APPROVAL_TRUTHY | APPROVAL_FALSY)
    return default, f"{field} must be one of {accepted}; actual={sanitize(value)}"


def parse_expiry(value: Any) -> tuple[dt.date | None, str | None]:
    """Parse a timezone-aware ISO-8601 datetime into a UTC date."""
    if value is None or str(value).strip() == "":
        return None, "expires_at is missing"
    text = str(value).strip()
    try:
        dt.date.fromisoformat(text)
        return None, "expires_at must be a timezone-aware ISO-8601 datetime; date-only values are interpreted ambiguously across release-owner time zones"
    except ValueError:
        pass
    try:
        normalized = text.replace("Z", "+00:00")
        parsed = dt.datetime.fromisoformat(normalized)
    except ValueError:
        return None, f"expires_at must be a timezone-aware ISO-8601 datetime; actual={sanitize(value)}"
    if parsed.tzinfo is None:
        return None, "expires_at must include a timezone offset or Z suffix"
    return parsed.astimezone(dt.timezone.utc).date(), None


def fingerprint_diff(current: dict[str, str], baseline: dict[str, str]) -> list[str]:
    """Compare two fingerprint maps and return per-file diagnostics for drift.

    The literal sentinel `"missing"` is always treated as drift, so deleting a required
    release-definition file fails closed even when the baseline also lost it.
    """
    diagnostics: list[str] = []
    for name, expected in baseline.items():
        actual = current.get(name, "missing")
        if actual == "missing":
            diagnostics.append(f"{name}: file is missing on disk (baseline={expected})")
        elif expected == "missing":
            diagnostics.append(f"{name}: baseline records file as missing (current={actual})")
        elif expected != actual:
            diagnostics.append(f"{name}: fingerprint {actual} != baseline {expected} (re-run prepare-manifest to refresh the baseline after legitimate release-definition changes)")
    for name in current:
        if name not in baseline:
            diagnostics.append(f"{name}: fingerprint has no baseline entry (re-run prepare-manifest to refresh the baseline after legitimate release-definition changes)")
    return diagnostics


# CR-12-4-P98 (round-5): bound the per-package timestamp block tighter — was 40 lines,
# now 80 lines (covers worst-case multi-line cert chain output) AND require the regex
# to anchor on `Timestamp signature` or `Timestamp signing certificate` rather than the
# bare word `Timestamp`. The trailing summary line `All packages verified valid` could
# previously match the last package's block when it bled to EOF; the new regex matches
# only true per-line Timestamp evidence.
_TIMESTAMP_BLOCK_MAX_LINES = 80
_TIMESTAMP_EVIDENCE = re.compile(
    # CR-12-4-P130 (round-6): modifier (`signature` | `signing certificate`) is OPTIONAL
    # so legitimate releases with bare `Timestamp:` form (older NuGet, alternate
    # timestamper output) are still recognized. The per-package 80-line block bound
    # plus the start-of-line anchor keep the cross-section bleed risk (P98) contained.
    r"^[ \t]*Timestamp(?:[ \t]+(?:signature|signing[ \t]+certificate))?[: \t][^\n]*(?:verified|valid|trusted|RFC[ \t]*3161)",
    re.IGNORECASE | re.MULTILINE,
)


def _bounded_timestamp_evidence(block: str) -> bool:
    """Return True when the per-package block contains a real Timestamp evidence line.

    The block is intentionally truncated to the first _TIMESTAMP_BLOCK_MAX_LINES so trailing
    `dotnet nuget verify` summary text following the last verified package cannot leak
    "valid" / "trusted" matches across packages.
    """
    bounded = "\n".join(block.splitlines()[:_TIMESTAMP_BLOCK_MAX_LINES])
    return bool(_TIMESTAMP_EVIDENCE.search(bounded))


def parse_signing_verification(text: str, package_ids: list[str] | None = None) -> dict[str, dict[str, str]]:
    """Parse `dotnet nuget verify --all` output for per-package verification status.

    `dotnet nuget verify --all` succeeds only when both the author signature and the
    RFC 3161 timestamp counter-signature are valid for the package, so a per-package
    'Successfully verified package' line is sufficient evidence for both gates.
    """
    statuses: dict[str, dict[str, str]] = {}
    if package_ids:
        for package_id in sorted(package_ids, key=len, reverse=True):
            pattern = re.compile(
                rf"Successfully verified package '{re.escape(package_id)}\.(\d[0-9A-Za-z.+\-]*)'"
            )
            match = pattern.search(text)
            if match:
                next_match = re.search(r"Successfully verified package '", text[match.end():])
                block_end = match.end() + next_match.start() if next_match else len(text)
                block = text[match.start():block_end]
                statuses[package_id.lower()] = {
                    "signing_status": "verified",
                    "timestamp_status": "verified" if _bounded_timestamp_evidence(block) else "missing",
                }
        return statuses

    # CR-12-4-P113 (round-5): anchor the fallback pattern at the start of a line so a
    # maliciously crafted `dotnet nuget verify` line cannot embed a fake "verified" line
    # inside another package's evidence and shift the package_id capture. Caller always
    # supplies `package_ids` in production; this fallback is for tests/fixtures.
    # CR-12-4-P142 (round-6): `\d+` (one or more digits) — the prior `\d[0-9]+` required
    # at least TWO digits in the major and failed `1.0.0`/`0.1.0` style versions.
    pattern = re.compile(r"(?:^|\n)\s*Successfully verified package '(.+?)\.(\d+(?:\.[0-9A-Za-z.+\-]+)*)'")
    for match in pattern.finditer(text):
        next_match = re.search(r"Successfully verified package '", text[match.end():])
        block_end = match.end() + next_match.start() if next_match else len(text)
        block = text[match.start():block_end]
        statuses[match.group(1).lower()] = {
            "signing_status": "verified",
            "timestamp_status": "verified" if _bounded_timestamp_evidence(block) else "missing",
        }
    return statuses


def deep_merge(base: Any, override: Any) -> Any:
    if isinstance(base, dict) and isinstance(override, dict):
        merged = {k: deep_merge(v, {}) for k, v in base.items()}
        for key, value in override.items():
            merged[key] = deep_merge(merged.get(key), value)
        return merged
    if isinstance(base, list) and isinstance(override, list):
        merged = [deep_merge(item, {}) for item in base]
        for index, value in enumerate(override):
            if index < len(merged):
                merged[index] = deep_merge(merged[index], value)
            else:
                merged.append(deep_merge(value, {}))
        return merged
    if override == {}:
        if isinstance(base, dict):
            return {k: deep_merge(v, {}) for k, v in base.items()}
        if isinstance(base, list):
            return [deep_merge(item, {}) for item in base]
        return base
    return override if override is not None else base


def manifest_diagnostics(manifest: dict[str, Any], root: pathlib.Path | None = None) -> list[str]:
    diagnostics: list[str] = []
    rows = manifest.get("packages", [])
    row_items = rows if isinstance(rows, list) else []
    typed_rows = [row for row in row_items if isinstance(row, dict)]
    if isinstance(rows, list) and len(typed_rows) != len(rows):
        diagnostics.append("package rows must be objects")
    root_exists = root is None or root.is_dir()
    if root is not None and not root_exists:
        diagnostics.append("--root must be an existing directory")
    if not isinstance(rows, list) or not rows:
        diagnostics.append("package rows are required")
    if isinstance(rows, list):
        versions = {row.get("version") for row in typed_rows}
        if None in versions or "" in versions:
            diagnostics.append("package version evidence is required for every row")
        if len(versions) > 1:
            diagnostics.append("lockstep version drift")
    for row in typed_rows:
        for field in REQUIRED_ROW_FIELDS:
            if not row.get(field):
                diagnostics.append(f"{row.get('package_id', '<unknown>')}: missing {field}")
        if row.get("signing_status") != "verified":
            diagnostics.append(f"{row.get('package_id')}: signing not verified")
        if row.get("timestamp_status") != "verified":
            diagnostics.append(f"{row.get('package_id')}: timestamp not verified")
        if row.get("attestation_status") not in {"attested", "approved-unsupported"}:
            diagnostics.append(f"{row.get('package_id')}: attestation state invalid")
        if str(row.get("checksum", "")).startswith("pending-") or not looks_like_sha256(row.get("checksum")):
            diagnostics.append(f"{row.get('package_id')}: checksum must be a concrete sha256")
        if str(row.get("artifact_path", "")).startswith("nupkgs/"):
            diagnostics.append(f"{row.get('package_id')}: manifest must reference signed nupkg artifacts")
        if root is not None and root_exists:
            artifact_path = str(row.get("artifact_path", ""))
            # CR-12-4-P93 (round-5): the symlink-rejection check MUST happen on the
            # unresolved candidate path. `normalize_under_root` calls `.resolve()` which
            # follows symlinks; calling `is_symlink()` on the resolved path always
            # returns False, so the previous guard was dead code. Check the unresolved
            # `root / artifact_path` (or any parent component) first; only then resolve
            # for the existence/size/checksum checks.
            try:
                unresolved_candidate = (root / artifact_path) if not os.path.isabs(artifact_path) else None
                if unresolved_candidate is None:
                    diagnostics.append(f"{row.get('package_id')}: artifact path invalid: evidence path must be relative")
                    continue
                # Walk parents to catch symlinked intermediate directories.
                symlinked = unresolved_candidate.is_symlink()
                if not symlinked:
                    for parent in unresolved_candidate.parents:
                        if parent == root or parent == root.resolve():
                            break
                        if parent.exists() and parent.is_symlink():
                            symlinked = True
                            break
                if symlinked:
                    diagnostics.append(f"{row.get('package_id')}: sealed artifact must not be a symlink")
                    continue
                artifact = normalize_under_root(root, artifact_path)
            except SystemExit as exc:
                diagnostics.append(f"{row.get('package_id')}: artifact path invalid: {exc}")
            else:
                if not artifact.exists():
                    diagnostics.append(f"{row.get('package_id')}: sealed artifact missing on disk")
                elif not artifact.is_file():
                    diagnostics.append(f"{row.get('package_id')}: sealed artifact must be a file")
                elif artifact.stat().st_size == 0:
                    diagnostics.append(f"{row.get('package_id')}: sealed artifact must not be empty")
                elif looks_like_sha256(row.get("checksum")) and sha256_file(artifact) != row.get("checksum"):
                    diagnostics.append(f"{row.get('package_id')}: sealed artifact checksum does not match on-disk artifact")
    for field in ["commit_sha", "tag", "run_id", "workflow_ref", "sbom_hash", "benchmark_summary_hash"]:
        if not manifest.get(field):
            diagnostics.append(f"manifest missing {field}")
    sbom_hash = manifest.get("sbom_hash")
    if sbom_hash and (str(sbom_hash).startswith("pending-") or not looks_like_sha256(sbom_hash)):
        diagnostics.append("manifest sbom_hash must be a concrete sha256")
    benchmark_hash = manifest.get("benchmark_summary_hash")
    if benchmark_hash and (str(benchmark_hash).startswith("pending-") or not looks_like_sha256(benchmark_hash)):
        diagnostics.append("manifest benchmark_summary_hash must be a concrete sha256")
    seal = manifest.get("seal", {})
    if not isinstance(seal, dict) or seal.get("algorithm") != "sha256" or not looks_like_sha256(seal.get("hash")):
        diagnostics.append("manifest seal with sha256 hash is required")
    else:
        canonical = json.dumps({k: v for k, v in manifest.items() if k != "seal"}, sort_keys=True, separators=(",", ":"))
        if seal.get("hash") != sha256_text(canonical):
            diagnostics.append("manifest seal hash does not match manifest contents")
    # Release-definition drift: when a root is provided, recompute current fingerprints
    # and compare to the manifest's embedded baseline. The baseline is written by
    # prepare_manifest, so absence from the manifest is itself a drift signal.
    if root is not None and root_exists:
        embedded = manifest.get("release_definition_fingerprints")
        if embedded is None:
            diagnostics.append("manifest missing release_definition_fingerprints")
        elif not isinstance(embedded, dict):
            diagnostics.append("manifest release_definition_fingerprints must be an object")
        else:
            current = release_definition_fingerprints(root)
            for drift in fingerprint_diff(current, embedded):
                diagnostics.append(f"release-definition drift: {drift}")
        # CR-12-4-P121 (round-6): also detect package-set drift. AC30 explicitly names
        # `eng/release-package-inventory.json` as a drift surface that must mark the
        # result `blocked`. Round-5 wrote `package_set_fingerprint` into the manifest
        # but never compared it on verify — the gap is closed here.
        embedded_package_set = manifest.get("package_set_fingerprint")
        current_package_set = package_set_fingerprint(root)
        if embedded_package_set is None:
            diagnostics.append("manifest missing package_set_fingerprint")
        elif not isinstance(embedded_package_set, str):
            diagnostics.append("manifest package_set_fingerprint must be a string")
        elif current_package_set is None:
            diagnostics.append("package-set drift: release-package-inventory.json missing on disk")
        elif embedded_package_set != current_package_set:
            diagnostics.append("package-set drift: release-package-inventory.json hash does not match sealed baseline")
    return diagnostics


def evidence_status_file(path: pathlib.Path) -> tuple[str, list[str]]:
    if not path.exists():
        return "missing", [f"{path.name}: evidence file is missing"]
    try:
        payload = read_json(path)
    except SystemExit as exc:
        return "partial", [f"{path.name}: {exc}"]
    status = payload.get("status") if isinstance(payload, dict) else None
    raw_diagnostics = payload.get("diagnostics", []) if isinstance(payload, dict) else []
    diagnostics = raw_diagnostics if isinstance(raw_diagnostics, list) else []
    if status in {"valid", "success"}:
        return "success", []
    if status == "missing":
        return "missing", [f"{path.name}: status=missing", *[sanitize(d) for d in diagnostics[:10]]]
    if status in {"invalid", "partial"}:
        return "partial", [f"{path.name}: status={status}", *[sanitize(d) for d in diagnostics[:10]]]
    return "partial", [f"{path.name}: status is missing or unknown"]


_EVIDENCE_MAX_BYTES = 1024 * 1024


def _is_reparse_point(path: pathlib.Path) -> bool:
    """Detect Windows NTFS junctions and reparse points that pathlib.is_symlink() misses."""
    try:
        attrs = getattr(path.stat(follow_symlinks=False), "st_file_attributes", 0)
    except (AttributeError, OSError, TypeError):
        return False
    return bool(attrs & 0x400)  # FILE_ATTRIBUTE_REPARSE_POINT


def _file_matches_evidence_glob(rel_path: pathlib.PurePosixPath) -> bool:
    """Return True if a path (relative to the evidence root, POSIX-style) is in the
    redaction-scan allowlist.

    CR-12-4-P128 (round-6): rewrote the glob match to use `fnmatch.fnmatchcase`
    against the FULL posix path with explicit leading-segment anchoring. Previous
    `pathlib.PurePosixPath.match` matched from the right, so
    `foo/attestations/bar.json` was treated as matching `attestations/*.json` — the
    over-broaden the docstring claimed was 1-level-only. The new match handles `**`
    correctly and anchors `attestations/*.json` to the evidence root level only.
    """
    name = rel_path.name
    if name in USER_FACING_EVIDENCE_FILES:
        return True
    posix = rel_path.as_posix()
    for glob in USER_FACING_EVIDENCE_GLOBS:
        if "**" in glob:
            # `prefix/**/suffix` matches any depth between prefix and suffix.
            prefix, _, suffix = glob.partition("**")
            prefix = prefix.rstrip("/")
            suffix = suffix.lstrip("/")
            if prefix and not posix.startswith(prefix + "/"):
                continue
            # Strip the prefix segment for fnmatch on the remainder.
            remainder = posix[len(prefix) + 1:] if prefix else posix
            # `**` allows zero or more intermediate segments; check `*/<suffix>` and
            # `<suffix>` separately so prefix/foo.json AND prefix/a/b/foo.json match.
            if fnmatch.fnmatchcase(remainder, suffix):
                return True
            if "/" in remainder and fnmatch.fnmatchcase(remainder, "*/" + suffix):
                return True
            # Recursive fallback: check every interior path segment.
            parts = remainder.split("/")
            for i in range(len(parts)):
                tail = "/".join(parts[i:])
                if fnmatch.fnmatchcase(tail, suffix):
                    return True
        else:
            # Anchor non-** globs at the evidence root (depth-1 only) — e.g.,
            # `attestations/*.json` matches `attestations/foo.json` but NOT
            # `subdir/attestations/foo.json`.
            if fnmatch.fnmatchcase(posix, glob):
                return True
    return False


def read_bounded_evidence(root: pathlib.Path) -> tuple[str, list[str]]:
    """Scan evidence files for redaction. Per-file cap + aggregate cap fail closed.

    CR-12-4-P94 (round-5): the allowlist now covers `signing-verification.txt`,
    `attestation-unavailable.md`, attestation bundle JSON, and SBOM CycloneDX JSON in
    addition to the user-facing helper outputs.
    CR-12-4-P95 (round-5): an aggregate cap (`EVIDENCE_AGGREGATE_BYTES`) caps the total
    text scanned, so a pathological evidence directory cannot blow up the canonical hash
    or memory.
    """
    raw_parts: list[str] = []
    diagnostics: list[str] = []
    if not root.exists():
        return "", diagnostics
    root_resolved = root.resolve()
    aggregate_bytes = 0
    aggregate_capped = False
    for current, dirs, files in os.walk(root, followlinks=False):
        current_path = pathlib.Path(current)
        kept_dirs = []
        for directory in dirs:
            candidate = current_path / directory
            try:
                if candidate.is_symlink() or _is_reparse_point(candidate):
                    diagnostics.append(f"{candidate.name}: skipped symlinked or junction evidence directory")
                    continue
            except OSError as exc:
                diagnostics.append(f"{candidate.name}: unable to inspect evidence directory: {sanitize(exc)}")
                continue
            kept_dirs.append(directory)
        dirs[:] = kept_dirs
        if aggregate_capped:
            break
        for file_name in files:
            child = current_path / file_name
            # CR-12-4-P125 (round-6): compute the relative path WITHOUT `.resolve()` so
            # the glob decision is made on the symlink's own name, not the target it
            # may point to. Combined with the `is_symlink()` rejection below, this
            # prevents a symlink at e.g. `signing-verification.txt` pointing to
            # `junk.bin` from bypassing the redaction scan (target name `junk.bin`
            # would not match the allowlist glob).
            try:
                rel = pathlib.PurePosixPath(child.relative_to(root).as_posix())
            except (OSError, ValueError):
                continue
            if not _file_matches_evidence_glob(rel):
                continue
            # Defense-in-depth containment check: ensure the resolved target stays
            # under the resolved root. A symlink pointing outside root_resolved still
            # gets a diagnostic via the `is_symlink()` check below.
            try:
                resolved_target = child.resolve()
                resolved_target.relative_to(root_resolved)
            except (OSError, ValueError):
                diagnostics.append(f"{child.name}: skipped evidence file with target outside root")
                continue
            try:
                if child.is_symlink() or _is_reparse_point(child):
                    diagnostics.append(f"{child.name}: skipped symlinked or junction evidence file")
                    continue
                if not child.is_file():
                    continue
                size = child.stat().st_size
                if size > _EVIDENCE_MAX_BYTES:
                    diagnostics.append(f"{child.name}: skipped oversized evidence file ({size} bytes; max {_EVIDENCE_MAX_BYTES})")
                    continue
                if aggregate_bytes + size > EVIDENCE_AGGREGATE_BYTES:
                    diagnostics.append(
                        f"evidence aggregate cap reached ({aggregate_bytes + size} bytes; "
                        f"cap {EVIDENCE_AGGREGATE_BYTES}); skipping remaining files starting with {child.name}"
                    )
                    aggregate_capped = True
                    break
                raw_parts.append(child.read_text(encoding="utf-8", errors="replace"))
                aggregate_bytes += size
            except OSError as exc:
                diagnostics.append(f"{child.name}: unable to read evidence file: {sanitize(exc)}")
    return "\n".join(raw_parts), diagnostics


def derive_release_checks(args: argparse.Namespace, manifest: dict[str, Any], test_payload: dict[str, Any]) -> tuple[dict[str, Any], list[str]]:
    diagnostics: list[str] = []
    evidence_root = pathlib.Path(args.evidence_root) if args.evidence_root else None
    root = pathlib.Path(args.root)
    root_exists = root.is_dir()
    if not root_exists:
        diagnostics.append("--root must be an existing directory")

    inventory_status = args.inventory_status
    paths_status = args.paths_status
    redaction_status = args.redaction_status
    helper_state = args.helper_state
    # CR-12-4-P111 (round-5): work against a local snapshot of `raw_evidence` rather
    # than mutating the argparse namespace. The earlier in-place mutation meant a
    # second invocation in the same process saw pre-sanitized data; the new local
    # variable preserves the original arg for any downstream consumer (and for tests
    # that reuse the namespace).
    raw_evidence_input = str(args.raw_evidence or "")
    raw_evidence_output = raw_evidence_input
    scanned_evidence = raw_evidence_input
    # CR-12-4-P110 (round-5): cache the manifest_diagnostics result so the duplicate
    # call later (for release-definition-drift and post-seal-mutation detection) reuses
    # the same per-artifact sha256 work rather than re-reading every .nupkg twice.
    manifest_diags_cache: list[str] | None = None
    if evidence_root:
        helper_diagnostics: list[str] = []
        verification_state, verification_diags = evidence_status_file(evidence_root / "release-verification.json")
        inventory_state, inventory_diags = evidence_status_file(evidence_root / "package-inventory.json")
        helper_diagnostics.extend(verification_diags)
        helper_diagnostics.extend(inventory_diags)
        # Both states share a uniform 2-state vocabulary so downstream BLOCKING_CHECKS
        # ("valid"/"invalid") comparisons treat verification and inventory the same way.
        inventory_status = "valid" if inventory_state == "success" else "invalid"
        # CR-12-4-P122 (round-6): non-dict manifest must still surface the typed
        # "manifest evidence is required" diagnostic. Round-5 P110 introduced the cache
        # but defaulted the non-dict arm to `[]`, dropping the diagnostic and reporting
        # paths_status="normalized" when paths_status should be "invalid".
        manifest_diags_cache = (
            manifest_diagnostics(manifest, root) if isinstance(manifest, dict)
            else ["manifest evidence is required"]
        )
        paths_status = "normalized" if not manifest_diags_cache else "invalid"

        raw_text, read_diagnostics = read_bounded_evidence(evidence_root)
        helper_diagnostics.extend(read_diagnostics)
        scanned_evidence = f"{raw_text}\n{raw_evidence_input}" if raw_evidence_input else raw_text
        redaction_status = "unsafe" if contains_dangerous_evidence(scanned_evidence) else "passed"
        raw_evidence_output = sanitize(raw_evidence_input) if raw_evidence_input else ""
        helper_state = "success" if not helper_diagnostics else "partial"
        diagnostics.extend(helper_diagnostics)

    manifest_rows = manifest.get("packages", []) if isinstance(manifest, dict) else []
    signing_status = args.signing_status
    timestamp_status = args.timestamp_status
    if isinstance(manifest_rows, list) and manifest_rows:
        signing_status = "verified" if all(row.get("signing_status") == "verified" for row in manifest_rows if isinstance(row, dict)) else "missing"
        timestamp_status = "verified" if all(row.get("timestamp_status") == "verified" for row in manifest_rows if isinstance(row, dict)) else "missing"

    sbom_hash = manifest.get("sbom_hash") if isinstance(manifest, dict) else None
    sbom_status = args.sbom_status
    if sbom_hash is not None:
        sbom_status = "present" if looks_like_sha256(sbom_hash) else "missing"

    checksum_status = args.checksums_status
    if isinstance(manifest_rows, list) and manifest_rows:
        checksum_status = "valid" if all(looks_like_sha256(row.get("checksum")) for row in manifest_rows if isinstance(row, dict)) else "mismatch"

    if manifest_diags_cache is None:
        manifest_diags = manifest_diagnostics(manifest, root) if isinstance(manifest, dict) else ["manifest evidence is required"]
    else:
        manifest_diags = manifest_diags_cache
    # CR-12-4-P121 (round-6): `package-set drift` diagnostics also signal
    # release_definition_drift so the existing BLOCKING_CHECKS pipeline catches them.
    release_definition_drift = args.release_definition_drift or any(
        "release-definition drift" in diagnostic or "package-set drift" in diagnostic
        for diagnostic in manifest_diags
    )
    post_seal_mutation = args.post_seal_artifact_mutation or any("sealed artifact checksum does not match" in diagnostic for diagnostic in manifest_diags)
    diagnostics.extend(
        diagnostic for diagnostic in manifest_diags
        if "release-definition drift" not in diagnostic
        and "package-set drift" not in diagnostic
        and "sealed artifact checksum does not match" not in diagnostic
    )

    # CR-12-4-P101 (round-5): defend against non-numeric test_count values. A producer
    # writing `"test_count": "unknown"` used to crash `int(...)` and bubble up as exit 2
    # (helper crash) instead of producing a typed `blocked` classification. Now the
    # crash branch records a diagnostic and forces test_count=0.
    raw_test_count = test_payload.get("test_count", args.test_count)
    try:
        coerced_test_count = int(raw_test_count or 0)
    except (TypeError, ValueError):
        diagnostics.append(f"test_count must be numeric; actual={sanitize(raw_test_count)}")
        coerced_test_count = 0

    return {
        "checksums_status": checksum_status,
        "concurrent_same_version": args.concurrent_same_version,
        "dry_run_side_effect_attempt": args.dry_run_side_effect_attempt,
        "diagnostics": [sanitize(diagnostic) for diagnostic in diagnostics[:20]],
        "helper_state": helper_state,
        "inventory_status": inventory_status,
        "paths_status": paths_status,
        "post_seal_artifact_mutation": post_seal_mutation,
        "raw_evidence": raw_evidence_output,
        "raw_evidence_sha256": canonical_sha256(scanned_evidence),
        "recursive_submodule_command": args.recursive_submodule_command,
        "redaction_status": redaction_status,
        "release_definition_drift": release_definition_drift,
        "sbom_status": sbom_status,
        "semantic_release_state": args.semantic_release_state,
        "signing_status": signing_status,
        "test_count": coerced_test_count,
        "test_status": "passed" if test_payload.get("status", args.test_status) == "valid" else test_payload.get("status", args.test_status),
        "timestamp_status": timestamp_status,
        "trx_present": bool(test_payload.get("trx_present", args.trx_present)),
    }, diagnostics


def safe_run_attempt(value: Any) -> tuple[int, str | None]:
    """Coerce a run_attempt value to int. Returns (value, diagnostic-or-None)."""
    if value is None or value == "":
        return 1, None
    # Python bool is a subtype of int; True/False would otherwise silently coerce to 1/0.
    if isinstance(value, bool):
        return 1, f"run_attempt must be numeric integer text; actual={sanitize(value)}"
    try:
        if isinstance(value, float):
            return 1, f"run_attempt must be numeric integer text; actual={sanitize(value)}"
        coerced = int(value)
    except (TypeError, ValueError):
        return 1, f"run_attempt must be numeric; actual={sanitize(value)}"
    # CR-12-4-P112 (round-5): reject negative or zero run_attempt values; semantic
    # `run_attempt >= 1` (GitHub Actions starts at 1) protects the rerun-review path
    # from being silently bypassed by, e.g., `--run-attempt -5`.
    # CR-12-4-P144 (round-6): keep coerced=1 (the canonical first-attempt floor) but
    # ensure all callers append the diagnostic to their blocking signal. `classify_context`
    # already appends to context-diagnostics; `partial_publish_incident` (P143) now
    # also forwards the diagnostic into the typed payload.
    if coerced < 1:
        return 1, f"run_attempt must be >= 1; actual={sanitize(value)}"
    return coerced, None


def classify_context(context: dict[str, Any]) -> tuple[str, list[str]]:
    """Classify a release context and surface typed diagnostics for unrecognized inputs.

    Returns (context_class, diagnostics). Diagnostics fail-close downstream by feeding
    `helper_state`/`blocking` so silent fail-open of security flags is impossible.
    """
    diagnostics: list[str] = []
    event_name = str(context.get("event_name", "local"))
    ref = str(context.get("ref", "local"))
    protected, diag = parse_strict_bool(context.get("ref_protected"), field="ref_protected")
    if diag:
        diagnostics.append(diag)
    from_fork, diag = parse_strict_bool(context.get("from_fork"), field="from_fork")
    if diag:
        diagnostics.append(diag)
    dry_run, diag = parse_strict_bool(context.get("dry_run"), field="dry_run")
    if diag:
        diagnostics.append(diag)
    run_attempt, diag = safe_run_attempt(context.get("run_attempt", 1))
    if diag:
        diagnostics.append(diag)
    partial_publish_raw = context.get("partial_publish_state", "none")
    partial_publish = "none" if partial_publish_raw is None else str(partial_publish_raw).strip().lower()
    if partial_publish == "":
        partial_publish = "none"
    if partial_publish not in {"none", "partial", "full", "recovered"}:
        diagnostics.append(f"partial_publish_state must be one of ['full', 'none', 'partial', 'recovered']; actual={sanitize(partial_publish_raw)}")
        # Default to "none" + emit diagnostic. Earlier code coerced to "partial" which
        # silently misclassified successful publishes as partial recoveries.
        partial_publish = "none"
    # Fork status takes precedence over rerun status: remediation paths differ.
    if from_fork:
        return "fork-pr", diagnostics
    if partial_publish != "none" or run_attempt > 1:
        return "rerun-review", diagnostics
    if event_name in {"pull_request", "pull_request_target"}:
        return "pr-same-repo", diagnostics
    if dry_run or event_name in {"local", ""} or ref == "local":
        return "local-candidate", diagnostics
    trusted_event = event_name in {"push", "workflow_dispatch", "workflow_run", "schedule"}
    trusted_ref = protected or ref in {"refs/heads/main", "main"} or ref.startswith("refs/tags/v")
    if trusted_event and trusted_ref:
        return "trusted-main-or-release", diagnostics
    return "local-candidate", diagnostics


def release_definition_fingerprints(root: pathlib.Path) -> dict[str, str]:
    fingerprints: dict[str, str] = {}
    for name in RELEASE_DEFINITION_FILES:
        path = root / name
        fingerprints[name] = sha256_file(path) if path.exists() else "missing"
    return fingerprints


def package_set_fingerprint(root: pathlib.Path) -> str | None:
    """Compute a fingerprint of the expected package set only.

    CR-12-4-D8 (round-5): split out from `release_definition_fingerprints` so routine
    package-version bumps in `Directory.Packages.props` do not invalidate fallback
    approvals. Drift in the package set (new/removed/renamed packages, exception status
    changes, lockstep version contract) is captured here from `release-package-inventory.json`.

    CR-12-4-P145 (round-6): return `None` (not the sentinel string `"missing"`) when
    the inventory file is absent so downstream comparators (which expect 64-char hex)
    do not confuse "missing" with a corrupted digest.
    """
    inventory_path = root / "eng" / "release-package-inventory.json"
    if not inventory_path.exists():
        return None
    return sha256_file(inventory_path)


def fallback_invalidation_fingerprints(root: pathlib.Path) -> dict[str, str]:
    """Subset of `release_definition_fingerprints` that drives fallback invalidation.

    CR-12-4-P124 (round-6): see `FALLBACK_INVALIDATION_FILES` comment — excludes
    `Directory.Packages.props` so routine Dependabot bumps do not invalidate fallbacks,
    while including all other release-definition files plus the package set inventory.
    """
    fingerprints: dict[str, str] = {}
    for rel in FALLBACK_INVALIDATION_FILES:
        target = root / rel
        fingerprints[rel] = sha256_file(target) if target.exists() else "missing"
    return fingerprints


def fallback_complete(
    fallback: dict[str, Any],
    fingerprints: dict[str, str] | None = None,
    *,
    evidence_root: pathlib.Path | None = None,
    package_set: str | None = None,
) -> tuple[bool, str | None]:
    """Validate fallback completeness. Returns (complete, diagnostic-or-None).

    CR-12-4-P120 (round-6): when `package_set` (the current `package_set_fingerprint`)
    is supplied, fold it into the digest the fallback was approved against so a
    package-set drift (per AC34) invalidates the fallback. The approved digest is
    compared against `canonical_sha256({"definition": fingerprints, "package_set": package_set})`.
    Callers that omit `package_set` retain the prior behavior (compare against
    `fingerprints` only).
    """
    required = [
        "affected_artifact",
        "approver",
        "evidence",
        "expires_at",
        "reason",
        "release_note_impact",
        "reopen_event",
        "scope",
        "approved_against_fingerprints_sha256",
    ]
    missing = [field for field in required if not fallback.get(field)]
    if missing:
        return False, f"fallback missing required field(s): {', '.join(missing)}"
    # CR-12-4-P108 (round-5): validate the digest is a well-formed sha256 hex string
    # before comparing. A malformed value (truncated, non-hex, swapped algorithm) used
    # to be reported as a generic "drifted release definition" — operators could spend
    # significant time chasing a release-definition drift that doesn't exist. Now they
    # get a typed reason and a clear corrective action.
    approved_digest_raw = str(fallback.get("approved_against_fingerprints_sha256", "")).strip()
    if not SHA256_RE.fullmatch(approved_digest_raw):
        return False, f"fallback approved_against_fingerprints_sha256 must be a 64-char hex sha256; actual={sanitize(approved_digest_raw)}"
    expiry, expiry_diagnostic = parse_expiry(fallback["expires_at"])
    if expiry is None:
        return False, expiry_diagnostic
    if expiry < dt.datetime.now(dt.timezone.utc).date():
        return False, f"fallback expired on {expiry.isoformat()}"
    # CR-12-4-P109 (round-5): path-check the evidence pointer. A fallback record with
    # `evidence: "release-evidence/attestation-unavailable.md"` is meaningless if the
    # file is not present — the document is the record. Phantom evidence pointers used
    # to silently authorize releases under `fallback-approved`; now they fail closed.
    if evidence_root is not None:
        evidence_name = str(fallback.get("evidence", ""))
        try:
            evidence_path = normalize_under_root(evidence_root, evidence_name)
        except SystemExit as exc:
            return False, f"fallback evidence path is not normalizable under root: {sanitize(exc)}"
        if not evidence_path.is_file() or evidence_path.stat().st_size == 0:
            return False, f"fallback evidence file missing or empty: {sanitize(evidence_name)}"
    if fingerprints is not None:
        approved_digest = approved_digest_raw.lower()
        if package_set is not None:
            # CR-12-4-P120 (round-6): fold package_set_fingerprint into the
            # invalidation digest so package-set drift invalidates fallback per AC34.
            current_digest = canonical_sha256(
                {"definition": fingerprints, "package_set": package_set}
            ).lower()
        else:
            current_digest = canonical_sha256(fingerprints).lower()
        if approved_digest != current_digest:
            return False, "fallback approved against drifted release definition or package set"
    return True, None


def contains_dangerous_evidence(value: Any) -> bool:
    text = json.dumps(value, sort_keys=True) if isinstance(value, (dict, list)) else str(value or "")
    return any(pattern.search(text) for pattern in DANGEROUS_EVIDENCE_PATTERNS)


def classify_release_payload(evidence: dict[str, Any], root: pathlib.Path, *, verify_drift: bool = True, evidence_root: pathlib.Path | None = None) -> dict[str, Any]:
    context = evidence.get("context", {})
    checks = evidence.get("checks", {})
    approval = evidence.get("approval", {})
    attestation = evidence.get("attestation", {})
    manifest = evidence.get("manifest", {})
    context_class, context_diagnostics = classify_context(context if isinstance(context, dict) else {})
    blocking: list[str] = []
    fallback_reasons: list[str] = []
    bool_diagnostics: list[str] = []

    approval_approved, approval_diagnostic = parse_approval_bool(approval.get("approved"), field="approval.approved")
    if approval_diagnostic:
        bool_diagnostics.append(approval_diagnostic)
    bool_checks: dict[str, bool] = {}
    for key in [
        "dry_run_side_effect_attempt",
        "recursive_submodule_command",
        "release_definition_drift",
        "post_seal_artifact_mutation",
        "concurrent_same_version",
        "trx_present",
    ]:
        parsed, diagnostic = parse_strict_bool(checks.get(key), field=key)
        bool_checks[key] = parsed
        if diagnostic:
            bool_diagnostics.append(diagnostic)

    for diag in context_diagnostics:
        blocking.append(f"context: {diag}")
    for diag in bool_diagnostics:
        blocking.append(f"evidence: {diag}")

    if context_class != "trusted-main-or-release":
        blocking.append(f"candidate evidence from {context_class} cannot authorize publishing")
    if not approval_approved or not approval.get("approver"):
        blocking.append("explicit release-owner approval is required before side effects")

    for key, expected in BLOCKING_CHECKS.items():
        actual = checks.get(key)
        if isinstance(expected, set):
            if actual not in expected:
                blocking.append(f"{key} must be one of {sorted(expected)}; actual={sanitize(actual)}")
        elif actual != expected:
            blocking.append(f"{key} must be {expected}; actual={sanitize(actual)}")

    if int(checks.get("test_count", 0) or 0) <= 0:
        blocking.append("release tests must record at least one executed test")
    if not bool_checks["trx_present"]:
        blocking.append("release TRX evidence is required")
    if bool_checks["dry_run_side_effect_attempt"]:
        blocking.append("dry-run side-effect attempt was detected")
    if bool_checks["recursive_submodule_command"]:
        blocking.append("recursive nested submodule command is not allowed")
    if bool_checks["release_definition_drift"]:
        blocking.append("release-definition fingerprints drifted after evidence generation")
    if bool_checks["post_seal_artifact_mutation"]:
        blocking.append("post-seal artifact mutation detected")
    if bool_checks["concurrent_same_version"]:
        blocking.append("same-version concurrent or stale release attempt requires owner review")
    if contains_dangerous_evidence(checks.get("raw_evidence")) and checks.get("redaction_status") != "sanitized":
        blocking.append("redaction scan found unsafe raw evidence")

    if isinstance(manifest, dict):
        # Fixtures and offline classifiers pin a static manifest and cannot exercise
        # release-definition drift against the live disk. Production runs always pass
        # verify_drift=True so post-seal repository edits are caught.
        drift_root = root if verify_drift else None
        for diagnostic in manifest_diagnostics(manifest, drift_root):
            blocking.append(f"manifest: {diagnostic}")
    else:
        blocking.append("manifest evidence is required")

    status = attestation.get("status")
    fallback_record: dict[str, str] | None = None
    if status == "approved-unsupported":
        fallback = attestation.get("fallback", {})
        if not isinstance(fallback, dict):
            blocking.append("unsupported-attestation fallback is missing, stale, or incomplete")
        else:
            manifest_fingerprints = manifest.get("release_definition_fingerprints") if isinstance(manifest, dict) else None
            if not isinstance(manifest_fingerprints, dict):
                blocking.append("manifest release_definition_fingerprints baseline is missing or malformed")
                fingerprints_for_drift = None
            else:
                # CR-12-4-P124 (round-6): use the narrower fallback-invalidation
                # surface so Dependabot churn in Directory.Packages.props does not
                # invalidate fallbacks. AC34 package-set drift still invalidates via
                # the package_set kwarg below.
                fingerprints_for_drift = {
                    k: v for k, v in manifest_fingerprints.items()
                    if k in set(FALLBACK_INVALIDATION_FILES)
                }
            # CR-12-4-P120 (round-6): bind the current package-set fingerprint so the
            # fallback is invalidated by inventory drift per AC34/D19.
            current_package_set = manifest.get("package_set_fingerprint") if isinstance(manifest, dict) else None
            # CR-12-4-P109 (round-5): pass the evidence root so `fallback_complete`
            # can path-check the `evidence` pointer. A fallback that references a
            # missing or empty `attestation-unavailable.md` is treated as incomplete.
            complete, fallback_diagnostic = fallback_complete(
                fallback,
                fingerprints_for_drift,
                evidence_root=evidence_root,
                package_set=current_package_set if isinstance(current_package_set, str) else None,
            )
            if complete:
                fallback_reasons.append("GitHub artifact attestation unavailable; approved unsupported-attestation fallback is in force")
                fallback_record = {
                    key: sanitize(fallback.get(key, ""))
                    for key in [
                        "affected_artifact",
                        "approver",
                        "evidence",
                        "expires_at",
                        "reason",
                        "release_note_impact",
                        "reopen_event",
                        "scope",
                        "approved_against_fingerprints_sha256",
                    ]
                }
            else:
                blocking.append(fallback_diagnostic or "unsupported-attestation fallback is missing, stale, or incomplete")
    elif status != "attested":
        blocking.append(f"attestation status must be attested or approved-unsupported; actual={sanitize(status)}")

    classification = "blocked" if blocking else ("fallback-approved" if fallback_reasons else "ready")
    publish_authorized = classification in {"ready", "fallback-approved"} and context_class == "trusted-main-or-release" and approval_approved
    next_action = "publish may proceed with approved owner action" if publish_authorized else "resolve blocking release gates before publishing"
    if context_class == "rerun-review":
        next_action = "rerun-review contexts are never publish-authorized; create a fresh dispatch or new tag to retry"
    if classification == "fallback-approved":
        next_action = "release owner must consciously accept the approved fallback before publish"

    return {
        "approval": {
            "approved": approval_approved,
            "approver": sanitize(approval.get("approver", "")),
            "mechanism": sanitize(approval.get("mechanism", "")),
        },
        # CR-12-4-D7 (round-5): emit the AC26 approval matrix as a top-level field so
        # consumers can enumerate the seven approval-dependent action types without
        # parsing the story-doc prose. The matrix is the constant `APPROVAL_MATRIX`
        # defined at module scope.
        "approval_matrix": APPROVAL_MATRIX,
        "advisory_artifacts": [
            "release-evidence/release-readiness.json",
            "release-evidence/test-results.json",
        ],
        "candidate_evidence_used": context_class != "trusted-main-or-release",
        "classification": classification,
        "context_class": context_class,
        "decision_contract": "frontcomposer.release-readiness.v1",
        "grouped_reasons": {
            "blocking": [sanitize(reason) for reason in blocking],
            "fallback": [sanitize(reason) for reason in fallback_reasons],
        },
        "fallback_record": fallback_record,
        "next_owner_action": next_action,
        # CR-12-4-D8 (round-5): publish the package-set fingerprint as a separate
        # fingerprint surface so consumers can distinguish "package set changed" (new
        # or removed packages, exception status drift) from generic release-definition
        # drift (workflow/helper/inventory file edits). Routine Dependabot bumps inside
        # `Directory.Packages.props` no longer invalidate approved fallbacks because
        # that file is no longer in `RELEASE_DEFINITION_FILES`.
        "package_set_fingerprint": package_set_fingerprint(root),
        "publish_authorized": publish_authorized,
        "release_definition_fingerprints": release_definition_fingerprints(root),
        "sanitized_raw_evidence": sanitize(checks.get("raw_evidence", "")),
    }


def inventory(args: argparse.Namespace) -> int:
    repo = pathlib.Path(args.root)
    expected = read_json(args.expected)
    rows: list[dict[str, Any]] = []
    diagnostics: list[str] = []
    expected_items = expected.get("packages", [])
    expected_by_path = {
        str(item["project"]).replace("\\", "/"): item
        for item in expected_items
        if isinstance(item, dict) and item.get("project")
    }
    discovered_by_path = {
        str(project.relative_to(repo)).replace("\\", "/"): project
        for project in discover_projects(repo)
    }
    for path in sorted(discovered_by_path):
        if path not in expected_by_path:
            state = "packable" if is_packable(discovered_by_path[path]) else "non-packable"
            diagnostics.append(f"{path}: unexpected {state} project missing from release package inventory")
    # CR-12-4-P104 (round-5): in addition to the src/-keyed inventory diff above,
    # report any project OUTSIDE src/ that declares `<IsPackable>true</IsPackable>`.
    # AC4 requires unexpected packable projects to fail closed. Build artifacts,
    # generated docs snippets, and submodule roots are excluded by
    # `discover_unexpected_packable_outside_src`.
    for project in discover_unexpected_packable_outside_src(repo):
        rel = str(project.relative_to(repo)).replace("\\", "/")
        diagnostics.append(f"{rel}: packable project outside src/ is not allowed by AC4 inventory contract")
    for path in sorted(expected_by_path):
        item = expected_by_path[path]
        project = repo / item["project"]
        if not project.exists():
            diagnostics.append(f"{item['project']}: expected project missing")
            continue
        actual_packable = is_packable(project)
        actual_id = package_id(project)
        expected_packable = bool(item.get("packable"))
        expected_id = item.get("package_id", "")
        if actual_packable != expected_packable:
            diagnostics.append(f"{item['project']}: packable drift")
        if actual_id != expected_id:
            diagnostics.append(f"{item['project']}: package id drift {actual_id} != {expected_id}")
        if actual_packable and project_property(project, "Version"):
            diagnostics.append(f"{item['project']}: per-project Version drift is not allowed")
        rows.append({
            "project": item["project"],
            "package_id": actual_id,
            "packable": actual_packable,
            "symbol_required": bool(item.get("symbol_required", False)),
            "exception": sanitize(item.get("exception", "")),
        })
    payload = {
        "status": "valid" if not diagnostics else "invalid",
        "expected_version_source": "semantic-release",
        "rows": rows,
        "diagnostics": diagnostics,
    }
    if args.output:
        write_json(args.output, payload)
    return 0 if not diagnostics else 1


def checksums(args: argparse.Namespace) -> int:
    root = pathlib.Path(args.root)
    files = [p for pattern in args.pattern for p in root.glob(pattern)]
    payload = [{"path": str(p.relative_to(root)).replace("\\", "/"), "sha256": sha256_file(p)} for p in sorted(files)]
    write_json(args.output, {"files": payload})
    return 0 if payload else 1


def test_results(args: argparse.Namespace) -> int:
    results_dir = pathlib.Path(args.results_dir)
    diagnostics: list[str] = []
    trx_files = sorted(results_dir.rglob("*.trx")) if results_dir.exists() else []
    executed = 0
    total = 0
    for trx in trx_files:
        try:
            root = ET.parse(trx).getroot()
        except ET.ParseError as exc:
            diagnostics.append(f"{trx.name}: invalid TRX XML: {exc}")
            continue
        counters = next((element for element in root.iter() if element.tag.endswith("Counters")), None)
        if counters is None:
            diagnostics.append(f"{trx.name}: missing Counters element")
            continue
        # CR-12-4-P92 (round-5): read failed/error/aborted/timeout counters and fail
        # closed if ANY are non-zero. The previous implementation only checked
        # `executed > 0`, which meant a TRX with `failed=100` would still classify
        # `test_status: valid` and `test_count: 100`. Combined with a workflow change
        # to `continue-on-error` (or a future helper invocation that doesn't depend on
        # `dotnet test` exiting non-zero), failing tests could bypass AC3.
        def _counter(attr_name: str) -> int:
            raw = counters.attrib.get(attr_name, "0")
            try:
                return int(raw)
            except (TypeError, ValueError):
                diagnostics.append(f"{trx.name}: counter '{attr_name}' is not numeric: {sanitize(raw)}")
                return 0

        executed += _counter("executed") if "executed" in counters.attrib else _counter("total")
        total += _counter("total")
        failed_count = _counter("failed")
        error_count = _counter("error")
        aborted_count = _counter("aborted")
        timeout_count = _counter("timeout")
        if failed_count > 0:
            diagnostics.append(f"{trx.name}: TRX records {failed_count} failed test(s); release tests must all pass")
        if error_count > 0:
            diagnostics.append(f"{trx.name}: TRX records {error_count} error test(s); release tests must all pass")
        if aborted_count > 0:
            diagnostics.append(f"{trx.name}: TRX records {aborted_count} aborted test(s); release tests must all pass")
        if timeout_count > 0:
            diagnostics.append(f"{trx.name}: TRX records {timeout_count} timed-out test(s); release tests must all pass")
    if not trx_files:
        diagnostics.append("release TRX evidence is missing")
    if executed <= 0:
        diagnostics.append("release tests executed zero tests")
    payload = {
        "diagnostics": [sanitize(diagnostic) for diagnostic in diagnostics],
        "status": "valid" if not diagnostics else "invalid",
        "test_count": executed,
        "total_count": total,
        "trx_files": [sanitize(str(path.relative_to(results_dir)).replace("\\", "/")) for path in trx_files],
        "trx_present": bool(trx_files),
    }
    if args.output:
        write_json(args.output, payload)
    return 0 if not diagnostics else 1


def verify_manifest(args: argparse.Namespace) -> int:
    manifest = read_json(args.manifest)
    diagnostics: list[str] = []
    if args.no_root:
        root = None
    elif args.root:
        root = pathlib.Path(args.root)
        if not root.is_dir():
            diagnostics.append("--root must be an existing directory")
    else:
        root = None
        diagnostics.append("--root is required for live manifest verification; pass --no-root only for offline fixture verification")
    diagnostics.extend(manifest_diagnostics(manifest, root))
    if args.output:
        write_json(args.output, {"status": "valid" if not diagnostics else "invalid", "diagnostics": diagnostics})
    return 0 if not diagnostics else 1


def seal_manifest(args: argparse.Namespace) -> int:
    manifest = read_json(args.manifest)
    canonical = json.dumps({k: v for k, v in manifest.items() if k != "seal"}, sort_keys=True, separators=(",", ":"))
    manifest["seal"] = {
        "algorithm": "sha256",
        "hash": hashlib.sha256(canonical.encode("utf-8")).hexdigest(),
        "sealed_at": dt.datetime.now(dt.timezone.utc).isoformat(),
    }
    write_json(args.output, manifest)
    return 0


def prepare_manifest(args: argparse.Namespace) -> int:
    inventory_payload = read_json(args.inventory)
    checksums_payload = read_json(args.checksums)
    checksums_by_path = {
        item["path"].replace("\\", "/"): item["sha256"]
        for item in checksums_payload.get("files", [])
        if isinstance(item, dict)
    }
    packages = []
    diagnostics: list[str] = []
    sbom_hash = args.sbom_hash
    if not sbom_hash:
        sbom_hash = next((v for k, v in checksums_by_path.items() if k.startswith("release-evidence/sbom/")), "")
    if not looks_like_sha256(sbom_hash):
        diagnostics.append("sbom checksum evidence is required before sealing the manifest")
    # Parse per-package signing/timestamp evidence from `dotnet nuget verify --all` output.
    # A package is marked verified only when the verification text names it as successful.
    verification_statuses: dict[str, dict[str, str]] = {}
    packable_package_ids = [
        str(row.get("package_id", ""))
        for row in inventory_payload.get("rows", [])
        if isinstance(row, dict) and row.get("packable") and row.get("package_id")
    ]
    # CR-12-4-P107 (round-5): route both `--signing-verification` and
    # `--attestation-bundle` through `normalize_under_root` so an attacker (or a
    # misconfigured operator) cannot point them at any readable file outside the
    # release-evidence root.
    prepare_root = pathlib.Path(args.root) if getattr(args, "root", None) else pathlib.Path(".")
    if args.signing_verification:
        try:
            verification_path = normalize_under_root(prepare_root, args.signing_verification)
        except SystemExit as exc:
            diagnostics.append(f"signing verification path invalid: {sanitize(exc)}")
            verification_path = None
        if verification_path is None:
            pass  # diagnostic already appended
        elif not verification_path.exists():
            diagnostics.append(f"signing verification evidence is missing: {sanitize(str(verification_path))}")
        else:
            verification_text = verification_path.read_text(encoding="utf-8", errors="replace")
            verification_statuses = parse_signing_verification(verification_text, packable_package_ids)
            if not verification_statuses:
                diagnostics.append("signing verification evidence did not name any verified package")
    else:
        diagnostics.append("signing verification evidence is required before sealing the manifest")
    attestation_bundle: dict[str, str] | None = None
    if args.attestation_status == "attested":
        if not args.attestation_bundle:
            diagnostics.append("attestation bundle evidence is required for attested releases")
        else:
            try:
                bundle_path = normalize_under_root(prepare_root, args.attestation_bundle)
            except SystemExit as exc:
                diagnostics.append(f"attestation bundle path invalid: {sanitize(exc)}")
                bundle_path = None
            if bundle_path is None:
                pass
            elif not bundle_path.exists() or not bundle_path.is_file():
                diagnostics.append(f"attestation bundle evidence is missing: {sanitize(str(bundle_path))}")
            elif bundle_path.stat().st_size == 0:
                diagnostics.append("attestation bundle evidence must not be empty")
            else:
                attestation_bundle = {
                    "path": str(bundle_path).replace("\\", "/"),
                    "sha256": sha256_file(bundle_path),
                }
    for row in inventory_payload.get("rows", []):
        if not row.get("packable"):
            continue
        package_id = row["package_id"]
        nupkg = f"nupkgs-signed/{package_id}.{args.version}.nupkg"
        snupkg = f"nupkgs/{package_id}.{args.version}.snupkg"
        checksum = checksums_by_path.get(nupkg, "")
        if not looks_like_sha256(checksum):
            diagnostics.append(f"{package_id}: signed package checksum is missing")
        if row.get("symbol_required") and snupkg not in checksums_by_path:
            diagnostics.append(f"{package_id}: symbol package checksum is missing")
        verification = verification_statuses.get(package_id.lower(), {})
        signing_status = verification.get("signing_status", "missing")
        timestamp_status = verification.get("timestamp_status", "missing")
        if signing_status != "verified":
            diagnostics.append(f"{package_id}: signing not verified in signing-verification evidence")
        if timestamp_status != "verified":
            diagnostics.append(f"{package_id}: timestamp not verified in signing-verification evidence")
        packages.append({
            "package_id": package_id,
            "version": args.version,
            "commit_sha": args.commit_sha,
            "artifact_path": nupkg,
            "checksum": checksum,
            "symbol_artifact": snupkg if row.get("symbol_required") else row.get("exception", "not-required"),
            "sbom_component": package_id,
            "signing_status": signing_status,
            "timestamp_status": timestamp_status,
            "attestation_status": args.attestation_status,
            "publish_status": "pending",
        })
    manifest = {
        "commit_sha": args.commit_sha,
        "tag": args.tag,
        "run_id": args.run_id,
        "workflow_ref": args.workflow_ref,
        "sbom_hash": sbom_hash or "pending-sbom-hash",
        "benchmark_summary_hash": args.benchmark_summary_hash,
        "packages": packages,
        "release_definition_fingerprints": release_definition_fingerprints(pathlib.Path(args.root)),
        # CR-12-4-D8 (round-5): separate fingerprint scope for the package set so a
        # routine Dependabot bump in Directory.Packages.props no longer invalidates
        # active unsupported-attestation fallbacks. Package-set drift is still caught
        # by inventory + this fingerprint.
        "package_set_fingerprint": package_set_fingerprint(pathlib.Path(args.root)),
    }
    if attestation_bundle:
        manifest["attestation_bundle"] = attestation_bundle
    write_json(args.output, manifest)
    if diagnostics:
        if args.diagnostics_output:
            write_json(args.diagnostics_output, {"status": "invalid", "diagnostics": diagnostics})
        return 1
    return 0


def release_budget(args: argparse.Namespace) -> int:
    if pathlib.Path(args.evidence).exists():
        raw = read_json(args.evidence)
    elif args.append_current:
        raw = []
    else:
        raise SystemExit(f"release budget evidence is missing: {args.evidence}")
    releases = raw.get("releases", raw) if isinstance(raw, dict) else raw
    if not isinstance(releases, list):
        raise SystemExit("invalid release budget evidence: expected releases list")
    if args.append_current:
        if not args.started_at:
            raise SystemExit("RELEASE_STARTED_AT must be set before --append-current")
        started = dt.datetime.fromisoformat(args.started_at.replace("Z", "+00:00"))
        ended = dt.datetime.fromisoformat(args.ended_at.replace("Z", "+00:00")) if args.ended_at else dt.datetime.now(dt.timezone.utc)
        minutes = max(0, (ended - started).total_seconds() / 60)
        package_count = int(args.package_count)
        if args.manifest and pathlib.Path(args.manifest).exists():
            manifest = read_json(args.manifest)
            package_count = len(manifest.get("packages", []))
        releases = [
            *releases,
            {
                "tag": args.tag,
                "run_id": args.run_id,
                "package_count": package_count,
                "slow_jobs": args.slow_job or [],
                "publish_status": args.publish_status,
                "billable_minutes": minutes,
                "publish_latency_minutes": args.publish_latency_minutes if args.publish_latency_minutes is not None else minutes,
            },
        ]
    normalized = []
    for release in releases:
        minutes = float(release.get("billable_minutes", 0))
        latency = float(release.get("publish_latency_minutes", 0))
        breach = minutes > 90 or latency > 120
        normalized.append({
            "tag": sanitize(release.get("tag", "")),
            "run_id": sanitize(release.get("run_id", "")),
            "package_count": int(release.get("package_count", 0)),
            "slow_jobs": [sanitize(v) for v in release.get("slow_jobs", [])[:10]],
            "publish_status": sanitize(release.get("publish_status", "")),
            "billable_minutes": minutes,
            "publish_latency_minutes": latency,
            "breach": breach,
        })
    last_three = normalized[-3:]
    action = "open-or-update-package-count-collapse-issue" if len(last_three) == 3 and all(r["breach"] for r in last_three) else "record-only"
    payload = {
        "marker": PACKAGE_COLLAPSE_MARKER,
        "action": action,
        "releases": normalized,
        "recommendation": "evaluate package-count collapse from 8 packages to 5" if action != "record-only" else "",
    }
    if args.output:
        write_json(args.output, payload)
    if args.apply and action != "record-only":
        require_trusted_context(args)
    return 0


def path_check(args: argparse.Namespace) -> int:
    candidate = normalize_under_root(pathlib.Path(args.root), args.name)
    if args.output:
        write_json(args.output, {"path": str(candidate)})
    return 0


_PARTIAL_PUBLISH_CLASSIFICATIONS = {"none", "partial-publish-incident", "recovered"}
# CR-12-4-P115 (round-5): validate --phase against an enum so operator typos like
# `packagepush` vs `package-push` are caught instead of silently landing in forensic JSON.
# CR-12-4-P141 (round-6): only `none`, `package-push`, and `symbol-push` have producers
# in the current publishCmd. `tag-push`, `github-release`, `attestation-upload`, and
# `post-seal-verification` are reserved for future failure handlers wired into the
# `@semantic-release/github`, `@semantic-release/git`, and attestation-upload phases.
# Keeping them in the enum so an operator running a manual recovery can record the
# phase without code changes; remove them only if the workflow restructure for
# CR-12-4-Def14 elects a different phase taxonomy.
_PARTIAL_PUBLISH_PHASES = {
    "none",
    "package-push",
    "symbol-push",
    "tag-push",
    "github-release",
    "attestation-upload",
    "post-seal-verification",
}


def partial_publish_incident(args: argparse.Namespace) -> int:
    classification = str(args.classification).strip().lower()
    if classification not in _PARTIAL_PUBLISH_CLASSIFICATIONS:
        accepted = sorted(_PARTIAL_PUBLISH_CLASSIFICATIONS)
        raise SystemExit(f"--classification must be one of {accepted}; actual={sanitize(args.classification)}")
    phase = str(args.phase).strip().lower()
    if phase not in _PARTIAL_PUBLISH_PHASES:
        accepted_phases = sorted(_PARTIAL_PUBLISH_PHASES)
        raise SystemExit(f"--phase must be one of {accepted_phases}; actual={sanitize(args.phase)}")
    # CR-12-4-P143 (round-6): validate --run-attempt via safe_run_attempt so a typo
    # or negative value gets a typed diagnostic instead of landing unsanitized in
    # forensic JSON. Placeholders (classification=none) use 1 because they have no
    # real attempt to record.
    run_attempt_value, run_attempt_diag = safe_run_attempt(args.run_attempt)
    manifest_diagnostic = ""
    try:
        manifest = read_json(args.manifest) if args.manifest else {}
    except SystemExit as exc:
        manifest = {}
        manifest_diagnostic = f"manifest unreadable: {sanitize(exc)}"
    if classification == "none":
        # CR-12-4-P127 (round-6): placeholder records must be reproducible across
        # runs so semantic-release's `@semantic-release/github` asset upload sees
        # stable checksums. Drop run-bound fields (manifest_seal sealed_at, run_id,
        # tag) from the placeholder. Real incidents keep all forensic provenance.
        payload = {
            "classification": "none",
            "decision_contract": "frontcomposer.partial-publish-incident.v1",
            "failed_phase": "none",
            "manifest_seal": {},
            "next_owner_action": "no partial publish incident recorded",
            "run_attempt": 1,
            "run_id": "",
            "tag": "",
        }
    else:
        payload = {
            "classification": classification,
            "decision_contract": "frontcomposer.partial-publish-incident.v1",
            "failed_phase": phase,
            "manifest_seal": manifest.get("seal", {}) if isinstance(manifest, dict) else {},
            "next_owner_action": "release owner must reconcile NuGet packages, symbols, tags, changelog, GitHub Release assets, and attestations before retry",
            "run_attempt": run_attempt_value,
            "run_id": sanitize(args.run_id),
            "tag": sanitize(args.tag),
            "timestamp_utc": dt.datetime.now(dt.timezone.utc).isoformat(),
        }
    diagnostics: list[str] = []
    if manifest_diagnostic:
        diagnostics.append(manifest_diagnostic)
    if classification != "none" and run_attempt_diag:
        diagnostics.append(run_attempt_diag)
    if diagnostics:
        payload["diagnostics"] = diagnostics
    write_json(args.output, payload)
    return 0


def _cli_bool(value: Any, *, field: str, approval: bool = False) -> bool:
    """Parse a CLI boolean flag, failing loudly on unrecognized input.

    Used by `require_trusted_context` for release-budget --apply where any parse error
    must abort with exit 2 rather than degrade to a default.
    """
    parser = parse_approval_bool if approval else parse_strict_bool
    parsed, diagnostic = parser(value, field=field)
    if diagnostic:
        print(f"release_evidence: invalid --{field.replace('_', '-')}: {diagnostic}", file=sys.stderr)
        raise SystemExit(2)
    return parsed


def classify_release(args: argparse.Namespace) -> int:
    cli_diagnostics: list[str] = []

    def parse_cli_bool(value: Any, *, field: str, approval: bool = False) -> bool:
        parser = parse_approval_bool if approval else parse_strict_bool
        parsed, diagnostic = parser(value, field=field)
        if diagnostic:
            cli_diagnostics.append(f"classify-release: invalid --{field.replace('_', '-')}: {diagnostic}")
        return parsed

    if args.evidence:
        evidence = read_json(args.evidence)
    else:
        manifest = read_json(args.manifest) if args.manifest else {}
        test_payload = read_json(args.test_results) if args.test_results else {}
        args.concurrent_same_version = parse_cli_bool(args.concurrent_same_version, field="concurrent_same_version")
        args.dry_run_side_effect_attempt = parse_cli_bool(args.dry_run_side_effect_attempt, field="dry_run_side_effect_attempt")
        args.post_seal_artifact_mutation = parse_cli_bool(args.post_seal_artifact_mutation, field="post_seal_artifact_mutation")
        args.recursive_submodule_command = parse_cli_bool(args.recursive_submodule_command, field="recursive_submodule_command")
        args.release_definition_drift = parse_cli_bool(args.release_definition_drift, field="release_definition_drift")
        args.trx_present = parse_cli_bool(args.trx_present, field="trx_present")
        args.dry_run = parse_cli_bool(args.dry_run, field="dry_run")
        args.from_fork = parse_cli_bool(args.from_fork, field="from_fork")
        args.ref_protected = parse_cli_bool(args.ref_protected, field="ref_protected")
        checks, helper_diagnostics = derive_release_checks(args, manifest, test_payload if isinstance(test_payload, dict) else {})
        helper_diagnostics.extend(cli_diagnostics)
        if helper_diagnostics and checks["helper_state"] == "success":
            checks["helper_state"] = "partial"
        if helper_diagnostics:
            checks["diagnostics"] = [*checks.get("diagnostics", []), *[sanitize(diagnostic) for diagnostic in helper_diagnostics[:20]]]
        evidence = {
            "approval": {
                "approved": parse_cli_bool(args.owner_approved, field="owner_approved", approval=True),
                "approver": args.approver,
                "mechanism": args.approval_mechanism,
            },
            "attestation": {
                "fallback": {
                    "affected_artifact": args.fallback_affected_artifact,
                    "approver": args.fallback_approver,
                    "evidence": args.fallback_evidence,
                    "expires_at": args.fallback_expires_at,
                    "reason": args.fallback_reason,
                    "release_note_impact": args.fallback_release_note_impact,
                    "reopen_event": args.fallback_reopen_event,
                    "scope": args.fallback_scope,
                    "approved_against_fingerprints_sha256": args.fallback_approved_against_fingerprints_sha256,
                },
                "status": args.attestation_status,
            },
            "checks": checks,
            # Raw values flow through; classify_context parses and emits typed diagnostics.
            "context": {
                "dry_run": args.dry_run,
                "event_name": args.event_name,
                "from_fork": args.from_fork,
                "partial_publish_state": args.partial_publish_state,
                "ref": args.ref,
                "ref_protected": args.ref_protected,
                "run_attempt": args.run_attempt,
            },
            "manifest": manifest,
        }
    # CR-12-4-P109 (round-5): forward `--evidence-root` to the payload classifier so
    # `fallback_complete` can path-check the `evidence` pointer against the same root
    # that `read_bounded_evidence` scanned.
    evidence_root_arg = pathlib.Path(args.evidence_root) if getattr(args, "evidence_root", None) else None
    # CR-12-4-P132 (round-6): load concurrency-guard probe diagnostics so they
    # propagate into the typed readiness payload.
    concurrency_probe_diagnostics: list[str] = []
    guard_path_arg = getattr(args, "concurrency_guard", "") or ""
    if guard_path_arg:
        guard_path = pathlib.Path(guard_path_arg)
        if guard_path.is_file():
            try:
                guard_payload = read_json(guard_path)
            except SystemExit:
                concurrency_probe_diagnostics.append("concurrency-guard.json unreadable")
            else:
                if isinstance(guard_payload, dict):
                    raw_diags = guard_payload.get("probe_diagnostics") or []
                    if isinstance(raw_diags, list):
                        concurrency_probe_diagnostics = [
                            f"concurrency-probe: {sanitize(d)}" for d in raw_diags if d
                        ]
        else:
            concurrency_probe_diagnostics.append(
                f"concurrency-probe: --concurrency-guard path missing: {sanitize(guard_path_arg)}"
            )
    decision = classify_release_payload(evidence, pathlib.Path(args.root), evidence_root=evidence_root_arg)
    if concurrency_probe_diagnostics:
        grouped = decision.setdefault("grouped_reasons", {})
        blocking_list = grouped.setdefault("blocking", [])
        if isinstance(blocking_list, list):
            blocking_list.extend(concurrency_probe_diagnostics)
    # If the caller omitted --output but CLI parse errors occurred, still write a typed
    # readiness JSON to a default path so downstream tooling can distinguish CLI-parse
    # exit 2 from "helper crashed" exit 2 by reading the typed contract.
    output_path = args.output or ("./release-evidence/release-readiness.json" if cli_diagnostics else None)
    if output_path:
        try:
            write_json(output_path, decision)
        except OSError as exc:
            print(f"classify-release: failed to write readiness output to {output_path}: {sanitize(exc)}", file=sys.stderr)
            return 3
    if cli_diagnostics:
        for diagnostic in cli_diagnostics:
            print(diagnostic, file=sys.stderr)
        return 2
    if args.require_publishable and not decision["publish_authorized"]:
        print("; ".join(decision["grouped_reasons"]["blocking"]) or decision["classification"], file=sys.stderr)
        return 1
    return 0


def classify_fixtures(args: argparse.Namespace) -> int:
    payload = read_json(args.fixtures)
    base = payload.get("base_evidence", {})
    results = []
    mismatches = []
    for case in payload.get("cases", []):
        evidence = deep_merge(base, case.get("override", {}))
        # Fixtures pin static manifests; skip release-definition drift to keep them
        # decoupled from the live release-definition file hashes.
        decision = classify_release_payload(evidence, pathlib.Path(args.root), verify_drift=False)
        result = {
            "classification": decision["classification"],
            "context_class": decision["context_class"],
            "name": case.get("name", ""),
            "next_owner_action": decision["next_owner_action"],
            "publish_authorized": decision["publish_authorized"],
        }
        expected_classification = case.get("expected_classification")
        expected_context = case.get("expected_context_class")
        expected_publish = case.get("expected_publish_authorized")
        expected_next_owner_action_contains = case.get("expected_next_owner_action_contains")
        if expected_classification and decision["classification"] != expected_classification:
            mismatches.append(f"{case.get('name')}: classification {decision['classification']} != {expected_classification}")
        if expected_context and decision["context_class"] != expected_context:
            mismatches.append(f"{case.get('name')}: context {decision['context_class']} != {expected_context}")
        if expected_publish is not None and decision["publish_authorized"] != expected_publish:
            mismatches.append(f"{case.get('name')}: publish_authorized {decision['publish_authorized']} != {expected_publish}")
        if expected_next_owner_action_contains and expected_next_owner_action_contains not in decision["next_owner_action"]:
            mismatches.append(f"{case.get('name')}: next_owner_action missing {expected_next_owner_action_contains}")
        results.append(result)
    output = {
        "diagnostics": mismatches,
        "results": results,
        "status": "valid" if not mismatches else "invalid",
    }
    if args.output:
        write_json(args.output, output)
    return 0 if not mismatches else 1


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    inv = sub.add_parser("inventory")
    inv.add_argument("--root", default=".")
    inv.add_argument("--expected", default="eng/release-package-inventory.json")
    inv.add_argument("--output")
    inv.set_defaults(func=inventory)

    chk = sub.add_parser("checksums")
    chk.add_argument("--root", default=".")
    chk.add_argument("--pattern", action="append", required=True)
    chk.add_argument("--output", required=True)
    chk.set_defaults(func=checksums)

    tests = sub.add_parser("test-results")
    tests.add_argument("--results-dir", required=True)
    tests.add_argument("--output")
    tests.set_defaults(func=test_results)

    ver = sub.add_parser("verify-manifest")
    ver.add_argument("--manifest", required=True)
    ver.add_argument("--root")
    ver.add_argument("--no-root", action="store_true")
    ver.add_argument("--output")
    ver.set_defaults(func=verify_manifest)

    seal = sub.add_parser("seal-manifest")
    seal.add_argument("--manifest", required=True)
    seal.add_argument("--output", required=True)
    seal.set_defaults(func=seal_manifest)

    prep = sub.add_parser("prepare-manifest")
    prep.add_argument("--inventory", required=True)
    prep.add_argument("--checksums", required=True)
    prep.add_argument("--output", required=True)
    prep.add_argument("--version", required=True)
    prep.add_argument("--root", default=".")
    prep.add_argument("--commit-sha", default=os.environ.get("GITHUB_SHA", "local"))
    prep.add_argument("--tag", required=True)
    prep.add_argument("--run-id", default=os.environ.get("GITHUB_RUN_ID", "local"))
    prep.add_argument("--workflow-ref", default=os.environ.get("GITHUB_WORKFLOW_REF", "local"))
    prep.add_argument("--sbom-hash", default="")
    prep.add_argument("--benchmark-summary-hash", default="")
    prep.add_argument("--attestation-status", default=os.environ.get("RELEASE_ATTESTATION_STATUS", "approved-unsupported"))
    prep.add_argument("--attestation-bundle", default="")
    prep.add_argument("--signing-verification", default="")
    prep.add_argument("--diagnostics-output")
    prep.set_defaults(func=prepare_manifest)

    budget = sub.add_parser("release-budget")
    budget.add_argument("--evidence", required=True)
    budget.add_argument("--output")
    budget.add_argument("--apply", action="store_true")
    budget.add_argument("--append-current", action="store_true")
    budget.add_argument("--started-at", default=os.environ.get("RELEASE_STARTED_AT", ""))
    budget.add_argument("--ended-at")
    budget.add_argument("--tag", default=os.environ.get("GITHUB_REF_NAME", "local"))
    budget.add_argument("--run-id", default=os.environ.get("GITHUB_RUN_ID", "local"))
    budget.add_argument("--package-count", type=int, default=0)
    budget.add_argument("--manifest")
    budget.add_argument("--publish-status", default="unknown")
    budget.add_argument("--publish-latency-minutes", type=float)
    budget.add_argument("--slow-job", action="append")
    budget.add_argument("--event-name", default=os.environ.get("GITHUB_EVENT_NAME", ""))
    budget.add_argument("--ref", default=os.environ.get("GITHUB_REF", ""))
    budget.add_argument("--ref-protected", default=os.environ.get("GITHUB_REF_PROTECTED", "false"))
    budget.add_argument("--from-fork", default="false")
    budget.set_defaults(func=release_budget)

    path = sub.add_parser("path-check")
    path.add_argument("--root", required=True)
    path.add_argument("--name", required=True)
    path.add_argument("--output")
    path.set_defaults(func=path_check)

    incident = sub.add_parser("partial-publish-incident")
    incident.add_argument("--manifest", required=True)
    incident.add_argument("--output", required=True)
    incident.add_argument("--phase", required=True)
    incident.add_argument("--classification", default="partial-publish-incident")
    incident.add_argument("--run-id", default=os.environ.get("GITHUB_RUN_ID", "local"))
    incident.add_argument("--run-attempt", default=os.environ.get("GITHUB_RUN_ATTEMPT", "1"))
    incident.add_argument("--tag", default=os.environ.get("GITHUB_REF_NAME", "local"))
    incident.set_defaults(func=partial_publish_incident)

    classify = sub.add_parser("classify-release")
    classify.add_argument("--root", default=".")
    classify.add_argument("--evidence-root", default="")
    # CR-12-4-P132 (round-6): consume `concurrency-guard.json` so the upstream probe's
    # `probe_diagnostics` propagate into the single typed `release-readiness.json`
    # contract instead of being visible only in step summary and the guard file.
    classify.add_argument("--concurrency-guard", default="")
    classify.add_argument("--evidence")
    classify.add_argument("--manifest")
    classify.add_argument("--output")
    classify.add_argument("--require-publishable", action="store_true")
    classify.add_argument("--test-results")
    classify.add_argument("--event-name", default=os.environ.get("GITHUB_EVENT_NAME", "local"))
    classify.add_argument("--ref", default=os.environ.get("GITHUB_REF", "local"))
    classify.add_argument("--ref-protected", default=os.environ.get("GITHUB_REF_PROTECTED", "false"))
    classify.add_argument("--from-fork", default="false")
    classify.add_argument("--dry-run", default="false")
    classify.add_argument("--run-attempt", default=os.environ.get("GITHUB_RUN_ATTEMPT", "1"))
    classify.add_argument("--partial-publish-state", default="none")
    classify.add_argument("--owner-approved", default=os.environ.get("RELEASE_OWNER_APPROVED", "false"))
    classify.add_argument("--approver", default=os.environ.get("RELEASE_APPROVER", ""))
    classify.add_argument("--approval-mechanism", default=os.environ.get("RELEASE_APPROVAL_MECHANISM", ""))
    classify.add_argument("--attestation-status", default=os.environ.get("RELEASE_ATTESTATION_STATUS", "approved-unsupported"))
    classify.add_argument("--fallback-affected-artifact", default="release package set")
    classify.add_argument("--fallback-approver", default=os.environ.get("RELEASE_ATTESTATION_FALLBACK_APPROVER", ""))
    # CR-12-4-P118 (round-6): default is the filename only; `fallback_complete`
    # resolves it under `--evidence-root` (typically `./release-evidence`). The
    # previous default `"release-evidence/attestation-unavailable.md"` was
    # double-prefixed under that root and silently broke every production
    # fallback-approved release.
    classify.add_argument("--fallback-evidence", default="attestation-unavailable.md")
    classify.add_argument("--fallback-expires-at", default=os.environ.get("RELEASE_ATTESTATION_FALLBACK_EXPIRES_AT", ""))
    classify.add_argument("--fallback-reason", default="GitHub artifact attestations unavailable in this repository context")
    classify.add_argument("--fallback-release-note-impact", default="Release notes must mention checksum, signature, SBOM, commit, tag, run, and workflow provenance without GitHub attestation.")
    classify.add_argument("--fallback-reopen-event", default="GitHub artifact attestations become available or release evidence contract changes")
    classify.add_argument("--fallback-scope", default="current release attempt")
    classify.add_argument("--fallback-approved-against-fingerprints-sha256", default=os.environ.get("RELEASE_ATTESTATION_FALLBACK_FINGERPRINTS_SHA256", ""))
    classify.add_argument("--checksums-status", default="missing")
    classify.add_argument("--concurrent-same-version", default="true")
    classify.add_argument("--dry-run-side-effect-attempt", default="false")
    classify.add_argument("--helper-state", default="missing")
    classify.add_argument("--inventory-status", default="missing")
    classify.add_argument("--paths-status", default="missing")
    classify.add_argument("--post-seal-artifact-mutation", default="false")
    classify.add_argument("--raw-evidence", default="")
    classify.add_argument("--recursive-submodule-command", default="false")
    classify.add_argument("--redaction-status", default="missing")
    classify.add_argument("--release-definition-drift", default="false")
    classify.add_argument("--sbom-status", default="missing")
    classify.add_argument("--semantic-release-state", default="missing")
    classify.add_argument("--signing-status", default="missing")
    classify.add_argument("--test-count", type=int, default=0)
    classify.add_argument("--test-status", default="missing")
    classify.add_argument("--timestamp-status", default="missing")
    classify.add_argument("--trx-present", default="false")
    classify.set_defaults(func=classify_release)

    fixtures = sub.add_parser("classify-fixtures")
    fixtures.add_argument("--root", default=".")
    fixtures.add_argument("--fixtures", required=True)
    fixtures.add_argument("--output")
    fixtures.set_defaults(func=classify_fixtures)

    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    try:
        sys.exit(main())
    except SystemExit:
        raise
    except Exception as exc:  # pragma: no cover - final guard for release scripts.
        print(f"release_evidence helper crashed: {sanitize(str(exc))}", file=sys.stderr)
        sys.exit(2)
