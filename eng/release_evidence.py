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


# CR-12-4-P215 (round-8, from CR-12-4-D16): explicit semantic version for the release
# evidence helper. Bumped intentionally when the helper logic changes in a way that
# invalidates sealed manifests; bumping this constant alongside the helper file edit
# is the operator-facing affirmation that the change was deliberate. The content
# sha256 is computed at prepare-manifest time and embedded in the sealed manifest;
# `manifest_diagnostics` compares both at verify time so a helper-bytes change without
# a `__version__` bump produces a `release-definition drift` signal.
__version__ = "1.0.0"

# CR-12-4-P257 (round-11, blind): assert at module load that `__version__` is a
# non-empty semver string. Without this guard, an operator typo (`__version__ = ""`)
# would silently neutralize the helper-binding guard — both producer and consumer
# `fallback_invalidation_fingerprints` would hash the empty string, the digest
# would match, and a malformed manifest's `helper_version` would pass drift
# validation. Failing closed at import time forces the helper version to remain
# operator-visible.
assert __version__ and re.fullmatch(r"^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$", __version__), (
    f"__version__ must be a non-empty semver string (got {__version__!r})"
)

# CR-12-4-P256 (round-11, edge): single-source the candidate-evidence blocker
# template so the dry-run carve-out (`classify_release_payload`) and the
# `--dry-run-clean-exit` allowlist gate (`classify_release`) cannot drift out of
# sync. The two sites previously embedded the same f-string literal; a future
# broadening of the carve-out (e.g., to add `rerun-review`) without updating the
# allowlist (or vice versa) would silently produce exit-1 with JSON saying
# `classification=ready`. The template is materialized once per call site.
_CANDIDATE_BLOCKER_TEMPLATE = "candidate evidence from {context_class} cannot authorize publishing"

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
# CR-12-4-P215 (round-8, from CR-12-4-D16): removed `eng/release_evidence.py`. Routine
# helper refactors / type-hint additions / docstring polish previously invalidated every
# fallback approval and every prior sealed manifest. The helper is now bound via the
# sealed-manifest `helper_version` field (semver + content sha256) so a helper-bytes
# change without an intentional `__version__` bump still surfaces as drift, but
# refactors that bump `__version__` are recognized as deliberate and do not invalidate
# unrelated fallback approvals.
RELEASE_DEFINITION_FILES = [
    ".github/workflows/release.yml",
    ".releaserc.json",
    "eng/release-package-inventory.json",
    "Directory.Build.props",
    "Directory.Build.targets",
    "Directory.Packages.props",
]
# CR-12-4-P124 (round-6): keys whose drift invalidates a `fallback-approved` release.
# Excludes `Directory.Packages.props` so transitive package-version bumps do not force
# re-approval. Drift in these files is still detected by `manifest_diagnostics` (drift
# detection) but does not trigger `fallback_complete` rejection.
# CR-12-4-P215 (round-8): `eng/release_evidence.py` excluded for the same reason as in
# `RELEASE_DEFINITION_FILES`; drift is caught via the helper_version field instead.
FALLBACK_INVALIDATION_FILES = [
    ".github/workflows/release.yml",
    ".releaserc.json",
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
# CR-12-4-P179 (round-7): `mechanism_inputs` is a structured list of the actual input
# names a consumer must match to evaluate the gate, separate from the prose `mechanism`
# description. Machine consumers can dispatch on the input names without parsing English.
APPROVAL_MATRIX = [
    {
        "action": "nuget-publish",
        "gate_id": "semantic-release-publish",
        "owner": "release-owner",
        "mechanism": "workflow-dispatch owner approval (release_owner_approved + release_approver inputs)",
        "mechanism_inputs": ["release_owner_approved", "release_approver"],
        "evidence": "release-readiness.json: classification=ready or fallback-approved, publish_authorized=true",
        "effect": "blocking",
        "fallback_action": None,
    },
    {
        "action": "tag-and-changelog-push",
        "gate_id": "semantic-release-publish",
        "owner": "release-owner",
        "mechanism": "workflow-dispatch owner approval (release_owner_approved + release_approver inputs)",
        "mechanism_inputs": ["release_owner_approved", "release_approver"],
        "evidence": "release-readiness.json: publish_authorized=true; semantic-release @semantic-release/git",
        "effect": "blocking",
        "fallback_action": None,
    },
    {
        "action": "github-release-create",
        "gate_id": "semantic-release-publish",
        "owner": "release-owner",
        "mechanism": "workflow-dispatch owner approval (release_owner_approved + release_approver inputs)",
        "mechanism_inputs": ["release_owner_approved", "release_approver"],
        "evidence": "release-readiness.json: publish_authorized=true; semantic-release @semantic-release/github",
        "effect": "blocking",
        "fallback_action": None,
    },
    {
        "action": "attestation-upload",
        "gate_id": "attestation",
        "owner": "release-owner",
        "mechanism": "workflow attestations write permission + gh attestation verify",
        "mechanism_inputs": ["permissions.attestations", "gh_attestation_verify"],
        "evidence": "manifest.attestation_status=attested with attestation_bundle, OR fallback-approved",
        "effect": "blocking-with-fallback",
        "fallback_action": "attestation-fallback",
    },
    {
        "action": "attestation-fallback",
        "gate_id": "attestation",
        "owner": "release-owner",
        "mechanism": "ATTESTATION_UNSUPPORTED repository variable plus sealed fallback record (approver, expiry, approved_at, fingerprint baseline)",
        # CR-12-4-P208 (round-8): enumerate the actual workflow-env inputs that compose
        # the fallback record. The prior list collapsed every individual var into the
        # generic literal "fallback_record"; consumers parsing the matrix could not
        # know which inputs to inspect when an approval failed completeness validation.
        "mechanism_inputs": [
            "vars.ATTESTATION_UNSUPPORTED",
            "vars.RELEASE_ATTESTATION_FALLBACK_APPROVER",
            "vars.RELEASE_ATTESTATION_FALLBACK_APPROVED_AT",
            "vars.RELEASE_ATTESTATION_FALLBACK_EXPIRES_AT",
            "vars.RELEASE_ATTESTATION_FALLBACK_FINGERPRINTS_SHA256",
            "fallback_record",
        ],
        "evidence": "attestation-unavailable.md plus sealed fallback_record (affected_artifact, approved_at, approver, evidence, expires_at, reason, release_note_impact, reopen_event, scope, approved_against_fingerprints_sha256)",
        "effect": "fallback",
        "fallback_action": None,
    },
    {
        "action": "partial-publish-recovery",
        "gate_id": "partial-publish-recovery",
        "owner": "release-owner",
        "mechanism": "manual review of partial-publish-incident.json plus fresh workflow-dispatch with new tag",
        "mechanism_inputs": ["partial_publish_incident_review", "release_owner_approved", "release_approver"],
        "evidence": "partial-publish-incident.json (failed_phase != none) plus reconciled NuGet/symbol state",
        "effect": "blocking",
        "fallback_action": None,
    },
    {
        "action": "rerun-after-failed-or-partial-release",
        "gate_id": "rerun",
        "owner": "release-owner",
        "mechanism": "fresh workflow-dispatch (run_attempt resets) or new tag",
        "mechanism_inputs": ["release_owner_approved", "release_approver"],
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
    # CR-12-4-P177 (round-7): accept both upper- and lower-case drive letters; a Windows
    # runner producing `c:\users\runner\...` paths previously slipped past the redaction
    # scan because the prior `[A-Z]:` only matched uppercase.
    re.compile(r"\b[A-Za-z]:[\\/][^ \r\n]+"),
    re.compile(r"(?<![\w/])/(?:home|Users|tmp|var)/[^ \r\n]+"),
    re.compile(r"(?i)\b(tenant|tenantid|user|userid|commandpayload)\s*[:=]\s*[^,; \r\n]+"),
    re.compile(r"^::", re.MULTILINE),
    # CR-12-4-Def106 (AC29): credentialed URLs of the form `scheme://user:pass@host/...`
    # leak embedded basic-auth credentials. The userinfo halves stop at the first
    # `/`, `?`, `#`, `&`, `@`, or whitespace, so neither a normal `https://host/path`
    # (no `@`) nor a path-less port URL whose query/fragment merely contains a later
    # `@` (e.g. `https://host:8080#a@b`) trips it — only real `user:pass@` userinfo.
    re.compile(r"(?i)\bhttps?://[^\s/@?#&]+:[^\s/@?#&]+@"),
    # CR-12-4-Def106 (AC29): PEM signing-material markers (private keys, certificates).
    re.compile(r"-----BEGIN (?:[A-Z0-9]+ )*(?:PRIVATE KEY|CERTIFICATE)-----"),
]
TRUTHY_LITERALS = {"true"}
FALSY_LITERALS = {"false"}
# Approval booleans accept ONLY the standard true/false serialization (the GitHub Actions
# boolean input type). CR-12-4-P184 (round-7, from CR-12-4-D12): the previous
# `approved`/`denied` aliases diverged from the bash gate at `.github/workflows/release.yml`
# Release-owner-approval-gate step, which rejects every value except literal `"true"`. A
# future refactor that routes approvals through Python first would silently widen the
# accepted token set; collapsing to the bash gate's contract removes that risk.
APPROVAL_TRUTHY = set(TRUTHY_LITERALS)
APPROVAL_FALSY = set(FALSY_LITERALS)
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
    text = re.sub(r"\b[A-Za-z]:[\\/][^ ]+", "[LOCAL_PATH]", text)
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
    """Return the first UNCONDITIONAL ``PropertyGroup/<name>`` value in the csproj.

    CR-12-4-P201 (round-8): ignore property elements that carry a ``Condition`` attribute
    (or live under a ``<PropertyGroup Condition="...">``). Conditional declarations only
    apply for matching MSBuild evaluation contexts (Configuration, TargetFramework, etc.);
    they do not encode an unconditional release-time invariant. A csproj declaring
    ``<IsPackable Condition="'$(Configuration)'=='Debug'">false</IsPackable>`` previously
    read as "non-packable" for AC4 inventory regardless of context, hiding a Release-only
    packable project. AC4 requires unconditional packability statements.
    """
    root = ET.parse(project).getroot()
    for prop in root.findall(".//PropertyGroup/" + name):
        if prop.attrib.get("Condition"):
            continue
        parent = prop  # element-tree lacks parent pointers; re-walk to detect conditional group
        if prop.text and prop.text.strip():
            # The parent PropertyGroup may carry the condition. Re-scan top-level
            # PropertyGroups and check whether this element lives under a conditional one.
            for group in root.findall(".//PropertyGroup"):
                if prop in list(group) and group.attrib.get("Condition"):
                    parent = None
                    break
            if parent is not None:
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
    "eng",          # CR-12-4-P181 (round-7): build/release script tree; if an eng/*.csproj
                    # MSBuild-task project is added without IsPackable=false the release
                    # would otherwise be blocked by the AC4 inventory diff.
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
    """Strict security-flag boolean (true/false only, case-insensitive). Empty -> default.

    CR-12-4-P237 (round-10, EC-9): when the caller supplies ``None`` for a security-flag
    field (e.g., ``checks.concurrent_same_version`` missing from a direct-evidence payload),
    emit a typed diagnostic instead of silently returning ``default``. Direct-evidence mode
    previously fail-opened on missing keys; CLI mode defaulted to ``"true"`` which fails
    closed. The typed diagnostic forces the caller (``classify_release_payload``) to surface
    the missing-flag as a blocker. Callers that legitimately want a default on ``None`` can
    detect the diagnostic and choose to ignore it; the security-flag loop in
    ``classify_release_payload`` treats every diagnostic as blocking, restoring symmetry
    with the CLI-default fail-closed path.
    """
    if isinstance(value, bool):
        return value, None
    if value is None:
        return default, f"{field} must be true or false; actual=<missing>"
    text = str(value).strip().lower()
    if text == "":
        return default, f"{field} must be true or false; actual=<empty>"
    if text in TRUTHY_LITERALS:
        return True, None
    if text in FALSY_LITERALS:
        return False, None
    return default, f"{field} must be true or false; actual={sanitize(value)}"


def parse_approval_bool(value: Any, *, field: str, default: bool = False) -> tuple[bool, str | None]:
    """Approval-domain boolean (literal ``true``/``false`` only after CR-12-4-P184).

    CR-12-4-P199 (round-8): the prior docstring still listed ``yes/no/1/0/approved/denied``
    aliases. Round-7 CR-12-4-P184 collapsed ``APPROVAL_TRUTHY`` / ``APPROVAL_FALSY`` to
    the literal ``{"true"}`` / ``{"false"}`` sets — every other vocabulary now returns a
    typed diagnostic so this helper matches the bash gate at
    ``.github/workflows/release.yml`` (Release-owner-approval-gate step), which itself
    rejects any non-literal value.
    """
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


def parse_expiry(value: Any) -> tuple[dt.datetime | None, str | None]:
    """Parse a timezone-aware ISO-8601 datetime into a UTC datetime.

    CR-12-4-P152 (round-7): returns the full UTC datetime rather than its `.date()`.
    Date-only comparison previously fired off-by-one for expiries with sub-day TZ
    offsets (e.g., `2026-12-31T00:00:00-12:00` is 2027-01-01 12:00Z, which compares
    against `dt.date(2027, 1, 1)` as already-expired even though the boundary clock
    moment is in the future). Returning the full datetime lets callers compare
    against `dt.datetime.now(dt.timezone.utc)` directly.
    """
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
    return parsed.astimezone(dt.timezone.utc), None


def parse_release_timestamp(value: Any, field: str) -> tuple[dt.datetime | None, str | None]:
    """Parse a required release-governance timestamp.

    This mirrors the fallback-expiry parser's date-only rejection, but does not require
    the timestamp to be in the future. Fallback approval timestamps document when the
    owner accepted the risk; they must be precise enough to audit across time zones.
    """
    if value is None or str(value).strip() == "":
        return None, f"{field} is missing"
    text = str(value).strip()
    try:
        dt.date.fromisoformat(text)
        return None, f"{field} must be a timezone-aware ISO-8601 datetime; date-only values are ambiguous"
    except ValueError:
        pass
    try:
        parsed = dt.datetime.fromisoformat(text.replace("Z", "+00:00"))
    except ValueError:
        return None, f"{field} must be a timezone-aware ISO-8601 datetime; actual={sanitize(value)}"
    if parsed.tzinfo is None:
        return None, f"{field} must include a timezone offset or Z suffix"
    parsed_utc = parsed.astimezone(dt.timezone.utc)
    if parsed_utc > dt.datetime.now(dt.timezone.utc) + dt.timedelta(minutes=5):
        return None, f"{field} must not be in the future"
    return parsed_utc, None


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
    # CR-12-4-P146 (round-7): word-bound the success markers (`\bvalid\b`, `\bverified\b`,
    # `\btrusted\b`, `\bRFC 3161\b`) so a `dotnet nuget verify` line containing
    # "Timestamp: invalid certificate" / "Timestamp: untrusted authority" / "Timestamp:
    # unverified" does not match as success via substring overlap. Without the boundaries
    # `valid` matched inside `invalid` and drove `timestamp_status="verified"` for a
    # timestamp the tooling actually rejected.
    r"^[ \t]*Timestamp(?:[ \t]+(?:signature|signing[ \t]+certificate))?[: \t][^\n]*\b(?:verified|valid|trusted|RFC[ \t]*3161)\b",
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
    # CR-12-4-P153 (round-7): the EH-6 concern about digit-containing package ids
    # (`Foo.NET6.1.0.0`) was analyzed and found unfounded — the lazy `(.+?)` paired with
    # the literal `\.\d+` and the trailing `'` anchor backtracks correctly: for
    # `Foo.NET6.1.0.0'` it greedy-matches `Foo.NET6` as the package and `1.0.0` as the
    # version. Pattern left intact; no code change.
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
        # CR-12-4-Def25: an explicit empty-list override clears the base list. The
        # element-wise merge below can only grow or rewrite entries, never shrink, so a
        # fixture that pins `packages: []` (to exercise the "package rows are required"
        # gate) would otherwise silently retain the base row. An OMITTED field still
        # routes through the `override == {}` reset branch below, so this only triggers
        # on a deliberate empty-list override.
        if not override:
            return []
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
        # CR-12-4-P159 (round-7): normalize the artifact path before the unsigned-tree
        # prefix check. A producer writing `./nupkgs/Foo.nupkg` (leading `./`) does not
        # start with `nupkgs/` and bypassed the guard; the manifest could then reference
        # the unsigned-tree directory while still claiming `signing_status=verified`.
        # CR-12-4-P200 (round-8): the prior `lstrip("./")` treats `.` and `/` as a
        # CHARACTER set, so `"..//nupkgs/Foo"` collapses to `"nupkgs/Foo"` and a leading
        # `..` traversal silently bypasses the guard. Separately, the `startswith` was
        # case-sensitive: `"Nupkgs/Foo.nupkg"` evaded the unsigned-tree check on
        # case-insensitive filesystems. Strip explicit relative prefixes safely and
        # lowercase the prefix compare.
        normalized_artifact_path = str(row.get("artifact_path", "")).replace("\\", "/")
        while normalized_artifact_path.startswith(("./", "../")):
            normalized_artifact_path = (
                normalized_artifact_path.split("/", 1)[1]
                if "/" in normalized_artifact_path
                else ""
            )
        if normalized_artifact_path.lower().startswith("nupkgs/"):
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
                # CR-12-4-P158 (round-7): NTFS junctions and reparse points are not
                # symlinks per pathlib; on Windows runners a junctioned artifact tree
                # silently bypassed this guard. Use `_is_reparse_point` (which already
                # exists for evidence files) on the unresolved candidate AND every
                # parent up to the root so a junctioned intermediate directory is also
                # caught.
                # Walk parents to catch symlinked or junctioned intermediate directories.
                symlinked = unresolved_candidate.is_symlink() or _is_reparse_point(unresolved_candidate)
                if not symlinked:
                    for parent in unresolved_candidate.parents:
                        if parent == root or parent == root.resolve():
                            break
                        if parent.exists() and (parent.is_symlink() or _is_reparse_point(parent)):
                            symlinked = True
                            break
                if symlinked:
                    diagnostics.append(f"{row.get('package_id')}: sealed artifact must not be a symlink or NTFS junction")
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
    # CR-12-4-Def23: enforce the structural presence + non-emptiness of
    # release_definition_fingerprints ALWAYS — including under --no-root offline
    # fixture verification — so replayed evidence carrying a missing or empty baseline
    # cannot authorize publishing. The drift comparison against on-disk files still
    # requires a root.
    embedded = manifest.get("release_definition_fingerprints")
    if embedded is None:
        diagnostics.append("manifest missing release_definition_fingerprints")
    elif not isinstance(embedded, dict):
        diagnostics.append("manifest release_definition_fingerprints must be an object")
    elif not embedded:
        diagnostics.append("manifest release_definition_fingerprints must not be empty")
    elif root is not None and root_exists:
        current = release_definition_fingerprints(root)
        for drift in fingerprint_diff(current, embedded):
            diagnostics.append(f"release-definition drift: {drift}")
    if root is not None and root_exists:
        # CR-12-4-P215 (round-8): verify the sealed `helper_version` against the live
        # helper. Mismatch on `version` OR `content_sha256` means either the helper
        # was edited post-seal or someone forgot to bump `__version__`. Both fail
        # closed via the existing `release-definition drift` blocking pipeline.
        embedded_helper = manifest.get("helper_version")
        if embedded_helper is None:
            diagnostics.append("manifest missing helper_version (CR-12-4-P215)")
        elif not isinstance(embedded_helper, dict):
            diagnostics.append("manifest helper_version must be an object with version + content_sha256")
        else:
            live_helper = helper_version_record()
            embedded_version = embedded_helper.get("version")
            embedded_content_sha256 = embedded_helper.get("content_sha256")
            if embedded_version != live_helper["version"]:
                diagnostics.append(
                    f"release-definition drift: helper_version.version drift (manifest={sanitize(embedded_version)}, live={sanitize(live_helper['version'])})"
                )
            if embedded_content_sha256 != live_helper["content_sha256"]:
                diagnostics.append(
                    "release-definition drift: helper_version.content_sha256 drift; bump __version__ in eng/release_evidence.py after deliberate helper changes and re-run prepare-manifest"
                )
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

    CR-12-4-P151 (round-7): the basename allowlist branch is now also anchored to
    depth-1 (`rel_path.parent == PurePosixPath("")`). Previously any file whose
    BASENAME matched `signing-verification.txt`/`partial-publish-incident.json`/etc.
    was scanned regardless of depth, so an SBOM tool writing
    `release-evidence/sbom/signing-verification.txt` would be folded into the
    aggregate evidence and produce spurious blocked classifications.
    """
    name = rel_path.name
    if name in USER_FACING_EVIDENCE_FILES and str(rel_path.parent) in {"", "."}:
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
                # CR-12-4-P154 (round-7): the aggregate cap must count the DECODED text
                # bytes, not the raw on-disk bytes. A 1 MB UTF-16 file read as UTF-8
                # with `errors="replace"` expands to ~3× via U+FFFD (3 bytes per
                # replacement char when re-encoded), so the aggregate text length could
                # exceed the 8 MB cap by ~3× and bloat canonical_sha256 input. Read
                # first, then measure the decoded result and re-check the aggregate cap.
                decoded = child.read_text(encoding="utf-8", errors="replace")
                decoded_size = len(decoded.encode("utf-8"))
                if aggregate_bytes + decoded_size > EVIDENCE_AGGREGATE_BYTES:
                    diagnostics.append(
                        f"evidence aggregate cap reached ({aggregate_bytes + decoded_size} decoded bytes; "
                        f"cap {EVIDENCE_AGGREGATE_BYTES}); skipping remaining files starting with {child.name}"
                    )
                    aggregate_capped = True
                    break
                raw_parts.append(decoded)
                aggregate_bytes += decoded_size
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
    # CR-12-4-P206 (round-8): also reject Python ``bool`` instances. ``bool`` is a
    # subtype of ``int`` so ``True or 0`` short-circuits to ``True`` and ``int(True) == 1``
    # passes the ``test_count > 0`` gate with a phantom value. A JSON producer writing
    # ``"test_count": true`` previously satisfied the test gate without running tests.
    raw_test_count = test_payload.get("test_count", args.test_count)
    if isinstance(raw_test_count, bool):
        diagnostics.append(f"test_count must not be boolean; actual={sanitize(raw_test_count)}")
        coerced_test_count = 0
    else:
        try:
            coerced_test_count = int(raw_test_count or 0)
        except (TypeError, ValueError):
            diagnostics.append(f"test_count must be numeric; actual={sanitize(raw_test_count)}")
            coerced_test_count = 0

    # CR-12-4-P204 (round-8): route trx_present through `parse_strict_bool` so a JSON
    # producer writing `"trx_present": "false"` (non-empty string) is correctly read as
    # False rather than silently coerced to True via `bool("false")`. Emit a typed
    # diagnostic for unrecognized values.
    trx_present_raw = test_payload.get("trx_present", args.trx_present)
    trx_present_parsed, trx_present_diagnostic = parse_strict_bool(trx_present_raw, field="trx_present")
    if trx_present_diagnostic:
        diagnostics.append(trx_present_diagnostic)
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
        "trx_present": trx_present_parsed,
    }, diagnostics


def safe_run_attempt(value: Any) -> tuple[int, str | None]:
    """Coerce a run_attempt value to int. Returns (value, diagnostic-or-None).

    CR-12-4-P160 (round-7): strip whitespace before parsing. `int(" 2")` and `int("2\\n")`
    used to succeed silently — a newline-padded `GITHUB_RUN_ATTEMPT` from a misconfigured
    dispatch slipped past the validator with no diagnostic. After stripping, any
    surrounding whitespace is rejected as a typed diagnostic.
    """
    if value is None or value == "":
        return 1, None
    # Python bool is a subtype of int; True/False would otherwise silently coerce to 1/0.
    if isinstance(value, bool):
        return 1, f"run_attempt must be numeric integer text; actual={sanitize(value)}"
    # CR-12-4-P160 (round-7): only strings need whitespace inspection — ints/floats
    # already have no whitespace. Reject inputs whose stripped form differs (catches
    # whitespace-only-around-digits AND empty-after-strip).
    if isinstance(value, str):
        stripped = value.strip()
        if stripped == "":
            return 1, None
        if stripped != value:
            return 1, f"run_attempt must not contain leading/trailing whitespace; actual={sanitize(value)}"
        value_for_parse: Any = stripped
    else:
        value_for_parse = value
    try:
        if isinstance(value_for_parse, float):
            return 1, f"run_attempt must be numeric integer text; actual={sanitize(value)}"
        coerced = int(value_for_parse)
    except (TypeError, ValueError):
        return 1, f"run_attempt must be numeric; actual={sanitize(value)}"
    # CR-12-4-P112 (round-5): reject negative or zero run_attempt values; semantic
    # `run_attempt >= 1` (GitHub Actions starts at 1) protects the rerun-review path
    # from being silently bypassed by, e.g., `--run-attempt -5`.
    # CR-12-4-P144 (round-6): keep coerced=1 (the canonical first-attempt floor) but
    # ensure all callers append the diagnostic to their blocking signal. `classify_context`
    # already appends to context-diagnostics; `partial_publish_incident` (P143) now
    # also forwards the diagnostic into the typed payload.
    # CR-12-4-P180 (round-7): keep the float `1` floor for the legacy return contract,
    # but the diagnostic stays attached so `classify_context` always emits a context
    # diagnostic when it fires; the floor itself never silently authorizes a rerun.
    # CR-12-4-P213 (round-8): the floor-with-diagnostic contract requires every caller
    # to propagate the diagnostic into a blocking signal — callers that ignore the
    # diagnostic would still see ``run_attempt=1`` and treat the run as a legitimate
    # first attempt. Current callers (`classify_context`, `partial_publish_incident`)
    # both forward the diagnostic; new callers MUST do the same. The unit-test surface
    # in ``CiGovernanceTests`` exercises both call sites; adding a new caller without
    # forwarding the diagnostic should be caught by review.
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


def helper_version_record() -> dict[str, str]:
    """Return the helper's semantic version plus its content sha256.

    CR-12-4-P215 (round-8, from CR-12-4-D16): the helper file (this module) is no
    longer in `RELEASE_DEFINITION_FILES`. Instead the sealed manifest carries a
    structured `helper_version` field containing the module's `__version__` constant
    and the running module's content sha256. `manifest_diagnostics` re-computes the
    record at verify time and emits `release-definition drift` when either field
    differs from the sealed baseline. Helper edits that bump `__version__` produce a
    new baseline that matches; edits that forget to bump the version still fail closed.
    """
    helper_path = pathlib.Path(__file__)
    return {
        "version": __version__,
        "content_sha256": sha256_file(helper_path) if helper_path.exists() else "missing",
    }


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

    CR-12-4-P232 (round-9, from CR-12-4-D18): include `helper_version` (the helper's
    semver `__version__` constant) so a deliberate operator-driven version bump
    invalidates the fallback approval digest. Content-only refactors keep the same
    `__version__` and DO NOT require re-approval; deliberate behavior changes MUST
    bump `__version__` to force re-approval. The `content_sha256` is NOT included —
    routine helper edits stay invisible to the digest until the operator signals
    intent via the version bump. This couples AC34's "release-definition drift
    invalidates fallback" invariant to an explicit operator action, matching D16's
    "intentional version bump" contract.

    CR-12-4-P245 (round-10, BH-F13): hash the version string before insertion so every
    value in the fingerprints dict is a 64-char sha256 hex. The original P232 placed
    the literal `"1.0.0"` alongside file-content hashes — operator forensic tools
    auditing the digest input saw a non-hex entry and assumed corruption. The hash is
    deterministic per version, so the consumer side computes the same value when
    `manifest.helper_version.version` matches; a future audit tool validating
    "every fingerprint is sha256-hex" now passes uniformly.

    CR-12-4-P261 (round-11, AA-006): also hash the `missing` sentinel for files that
    are absent on disk so the "every value is sha256-hex" invariant holds for the
    full dict. The hash of the literal `missing` is deterministic, so a future
    inventory-rebuild that legitimately removes a file produces the same digest
    contribution on both sides.
    """
    fingerprints: dict[str, str] = {}
    missing_sentinel_hash = hashlib.sha256(b"missing").hexdigest()
    for rel in FALLBACK_INVALIDATION_FILES:
        target = root / rel
        fingerprints[rel] = sha256_file(target) if target.exists() else missing_sentinel_hash
    helper_version = helper_version_record()["version"]
    fingerprints["helper_version"] = hashlib.sha256(helper_version.encode("utf-8")).hexdigest()
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
        "approved_at",
        "approver",
        "evidence",
        "expires_at",
        "reason",
        "release_note_impact",
        "reopen_event",
        "scope",
        "approved_against_fingerprints_sha256",
    ]
    # CR-12-4-Def107 (review): strip before the presence check so a whitespace-only
    # required field (e.g. affected_artifact="   ") is treated as MISSING. Without this a
    # blank-but-truthy value passed completeness here yet was skipped by the downstream
    # affected_artifact cross-check (it strips to empty), opening a fail-open hole.
    missing = [field for field in required if not str(fallback.get(field) or "").strip()]
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
    approved_at, approved_at_diagnostic = parse_release_timestamp(fallback["approved_at"], "approved_at")
    if approved_at is None:
        return False, approved_at_diagnostic
    # CR-12-4-P243 (round-10, BH-F21/EC-6): the parser rejects future-skewed
    # `approved_at` values (>5 min in future) but the original code accepted any past
    # value — `0001-01-01T00:00:00Z` would satisfy structural completeness. Document
    # WHEN the owner accepted the risk requires the timestamp to be (a) before
    # `expires_at`, and (b) within the last 365 days (operationally a 1-year approval
    # window matches the typical signing-cert lifetime).
    # CR-12-4-Def102: the approved_at < expires_at ordering invariant is checked BEFORE
    # the expiry-in-the-past check so the precise "approved_at must be before expires_at"
    # diagnostic fires on an `approved_at == expires_at` operator footgun even when both
    # instants are in the past — a static fixture cannot place them in the future without
    # tripping the >5-min future-skew guard in `parse_release_timestamp`.
    if approved_at >= expiry:
        return False, f"approved_at ({approved_at.isoformat()}) must be before expires_at ({expiry.isoformat()})"
    # CR-12-4-P152 (round-7): full-datetime comparison so a sub-day TZ offset cannot
    # silently shift expiry by a calendar day.
    if expiry <= dt.datetime.now(dt.timezone.utc):
        return False, f"fallback expired on {expiry.isoformat()}"
    approval_age = dt.datetime.now(dt.timezone.utc) - approved_at
    # CR-12-4-P259 (round-11, blind+edge): use `>=` so an approval at exactly
    # 365 days, 0 microseconds old is rejected. The prior `>` allowed the exact
    # boundary, contradicting the comment claim of strict 365-day enforcement.
    if approval_age >= dt.timedelta(days=365):
        return False, f"approved_at is 365+ days old ({approved_at.isoformat()}); re-approve the fallback"
    # CR-12-4-P109 (round-5): path-check the evidence pointer. A fallback record with
    # `evidence: "release-evidence/attestation-unavailable.md"` is meaningless if the
    # file is not present — the document is the record. Phantom evidence pointers used
    # to silently authorize releases under `fallback-approved`; now they fail closed.
    if evidence_root is not None:
        evidence_name = str(fallback.get("evidence", ""))
        # CR-12-4-P157 (round-7): detect the legacy double-prefix form
        # (`release-evidence/attestation-unavailable.md` resolved under an
        # `--evidence-root ./release-evidence` produces a missing file). Emit a typed
        # diagnostic so operators get an actionable hint instead of a generic
        # "evidence file missing" message.
        evidence_root_name = evidence_root.name
        if evidence_root_name and evidence_name.replace("\\", "/").startswith(
            evidence_root_name + "/"
        ):
            return False, (
                f"fallback evidence path duplicates evidence-root prefix '{sanitize(evidence_root_name)}/': "
                f"use the path relative to the evidence root (e.g., 'attestation-unavailable.md'); "
                f"actual={sanitize(evidence_name)}"
            )
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
            # CR-12-4-P260 (round-11, edge): distinguish "real drift" (file content
            # changed) from "structural malformation" (e.g., `manifest.helper_version
            # is null` — caught by `manifest_diagnostics` upstream but still
            # contributes a sentinel hash here). When the consumer-side helper version
            # sentinel `<missing>` is in play, the digest mismatch is a STRUCTURAL
            # consequence of the upstream manifest defect, not a real drift event.
            # The operator already sees the structural blocker from
            # `manifest_diagnostics`; the secondary message scopes the diagnostic to
            # the digest layer rather than incorrectly naming drift.
            sentinel_helper_hash = hashlib.sha256(b"<missing>").hexdigest()
            if fingerprints.get("helper_version") == sentinel_helper_hash:
                return False, "fallback digest mismatch caused by malformed manifest.helper_version (see manifest diagnostics)"
            return False, "fallback approved against drifted release definition or package set"
    return True, None


def contains_dangerous_evidence(value: Any) -> bool:
    text = json.dumps(value, sort_keys=True) if isinstance(value, (dict, list)) else str(value or "")
    return any(pattern.search(text) for pattern in DANGEROUS_EVIDENCE_PATTERNS)


def evidence_section(evidence: dict[str, Any], name: str, diagnostics: list[str]) -> dict[str, Any]:
    # CR-12-4-P242 (round-10, BH-F10/EC-13): distinguish missing-section vs malformed.
    # Operator forensic message previously couldn't tell whether `"checks": {}` was
    # a deliberate empty payload, a missing key, or a malformed non-dict value.
    # CR-12-4-P258 (round-11, blind): removed the "section is empty" diagnostic. A
    # deliberately-empty dict is semantically distinct from missing/malformed AND
    # was inconsistent with the `manifest_raw` branch which never went through
    # `evidence_section`. The empty diagnostic also multiplied blockers for
    # sparse-evidence dry-runs and could prevent the carve-out's `len(blocking)==1`
    # check from firing for legitimate sparse-evidence cases. Sections that need
    # required fields (e.g., `checks.concurrent_same_version`) still fail closed
    # via the security-flag loop and P237's `parse_strict_bool(None)` diagnostic.
    if name not in evidence:
        diagnostics.append(f"{name} section missing")
        return {}
    value = evidence[name]
    if isinstance(value, dict):
        return value
    diagnostics.append(f"{name} section must be an object")
    return {}


def classify_release_payload(evidence: dict[str, Any], root: pathlib.Path, *, verify_drift: bool = True, evidence_root: pathlib.Path | None = None) -> dict[str, Any]:
    section_diagnostics: list[str] = []
    # CR-12-4-P235 (round-10, EC-2): guard against non-dict top-level evidence. P223
    # guarded only the `manifest` sub-section; an evidence file with `[]`, `null`, or
    # scalar at top level still reached `evidence_section(evidence, ...)` and crashed
    # with AttributeError on the first `evidence.get(name, {})` call. Treat the
    # malformed payload the same way as malformed sub-sections: typed diagnostic,
    # empty-dict fallthrough, classification=blocked via the section_diagnostics path.
    if not isinstance(evidence, dict):
        section_diagnostics.append("evidence payload must be an object")
        evidence = {}
    context = evidence_section(evidence, "context", section_diagnostics)
    checks = evidence_section(evidence, "checks", section_diagnostics)
    approval = evidence_section(evidence, "approval", section_diagnostics)
    attestation = evidence_section(evidence, "attestation", section_diagnostics)
    # CR-12-4-P223 (round-9, BH-026/EC-4): the prior `manifest_raw if isinstance(...)
    # else manifest_raw` was a no-op ternary that let a non-dict manifest section
    # (list, scalar, null) leak through to downstream code. Existing isinstance
    # guards prevented crashes but the operator-facing forensic JSON missed the
    # typed diagnostic. Treat malformed manifest the same way as malformed
    # approval/attestation sections.
    manifest_raw = evidence.get("manifest", {})
    if isinstance(manifest_raw, dict):
        manifest = manifest_raw
    else:
        manifest = {}
        section_diagnostics.append("manifest section must be an object")
    context_class, context_diagnostics = classify_context(context if isinstance(context, dict) else {})
    blocking: list[str] = []
    fallback_reasons: list[str] = []
    bool_diagnostics: list[str] = []

    approval_approved, approval_diagnostic = parse_approval_bool(approval.get("approved"), field="approval.approved")
    if approval_diagnostic:
        bool_diagnostics.append(approval_diagnostic)
    # CR-12-4-Def105: `approval.approved` must be a real JSON boolean. `parse_approval_bool`
    # leniently interprets a stringly-typed "true"/"false" (P184 robustness vs. the bash
    # gate), but on this publish-authorizing field a non-boolean type is a coercion/tamper
    # signal — fail closed so a producer that emits any non-empty string cannot ride a
    # string "true" into an authorized publish.
    raw_approved = approval.get("approved")
    if raw_approved is not None and not isinstance(raw_approved, bool):
        bool_diagnostics.append(f"approval.approved must be a JSON boolean, not a string; actual={sanitize(raw_approved)}")
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
    # CR-12-4-Def105: the same JSON-boolean strictness for the concurrent-publish guard.
    # `concurrent_same_version` gates the same-version side-effect path; a stringly-typed
    # value (even "false") is a type/coercion signal that must fail closed rather than be
    # leniently coerced by `parse_strict_bool`.
    raw_concurrent = checks.get("concurrent_same_version")
    if raw_concurrent is not None and not isinstance(raw_concurrent, bool):
        bool_diagnostics.append(f"concurrent_same_version must be a JSON boolean, not a string; actual={sanitize(raw_concurrent)}")

    for diag in context_diagnostics:
        blocking.append(f"context: {diag}")
    for diag in section_diagnostics:
        blocking.append(f"evidence: {diag}")
    for diag in bool_diagnostics:
        blocking.append(f"evidence: {diag}")
    check_diagnostics = checks.get("diagnostics", [])
    if isinstance(check_diagnostics, list):
        for diag in check_diagnostics[:20]:
            blocking.append(f"evidence: {sanitize(diag)}")
    elif check_diagnostics:
        blocking.append("evidence: checks.diagnostics must be an array")

    if context_class != "trusted-main-or-release":
        blocking.append(_CANDIDATE_BLOCKER_TEMPLATE.format(context_class=context_class))
    if not approval_approved or not approval.get("approver"):
        blocking.append("explicit release-owner approval is required before side effects")

    for key, expected in BLOCKING_CHECKS.items():
        actual = checks.get(key)
        if isinstance(expected, set):
            if actual not in expected:
                blocking.append(f"{key} must be one of {sorted(expected)}; actual={sanitize(actual)}")
        elif actual != expected:
            blocking.append(f"{key} must be {expected}; actual={sanitize(actual)}")

    # CR-12-4-P206 (round-8): mirror the bool guard in `derive_release_checks`. The
    # classifier consumes the typed evidence file (e.g., when --evidence is passed) so a
    # malformed ``"test_count": true`` JSON value must fail closed here too rather than
    # silently coerce to 1.
    raw_test_count = checks.get("test_count", 0)
    if isinstance(raw_test_count, bool):
        blocking.append(f"evidence: test_count must not be boolean; actual={sanitize(raw_test_count)}")
        test_count = 0
    else:
        try:
            test_count = int(raw_test_count or 0)
        except (TypeError, ValueError):
            blocking.append(f"evidence: test_count must be numeric; actual={sanitize(raw_test_count)}")
            test_count = 0
    if test_count <= 0:
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
    # CR-12-4-P271 (round-13, EH-R13-8 from D30): enforce top-level `attestation.status`
    # and per-row `manifest.packages[*].attestation_status` agreement. A hand-crafted or
    # tampered manifest with top-level `attested` but per-row `approved-unsupported`
    # (or vice versa) could route through one gate while slipping the other:
    #   - status="attested" routes to the bundle-required branch (L1738) but per-row
    #     `approved-unsupported` would never surface its own missing-fallback evidence.
    #   - status="approved-unsupported" routes to the fallback-validation branch
    #     (L1626) but per-row `attested` would never surface its own missing-bundle
    #     evidence.
    # The producer (`prepare_manifest`) always writes them in lockstep from a single
    # source (`RELEASE_ATTESTATION_STATUS`), so any divergence at classify time is a
    # tampering signal. Reject before either branch fires.
    if status in {"attested", "approved-unsupported"} and isinstance(manifest, dict):
        manifest_packages = manifest.get("packages") if isinstance(manifest.get("packages"), list) else []
        discordant = [
            str(row.get("package_id", "<unknown>"))
            for row in manifest_packages
            if isinstance(row, dict) and row.get("attestation_status") not in {None, status}
        ]
        if discordant:
            blocking.append(
                f"manifest: per-row attestation_status disagrees with top-level attestation.status={sanitize(status)}; "
                f"packages={','.join(sanitize(p) for p in discordant[:5])}"
            )
    fallback_record: dict[str, str] | None = None
    if status == "approved-unsupported":
        # CR-12-4-P219 (round-9, BH-010): inverse of the `attested` bundle-presence
        # check below. A stale or hand-crafted manifest carrying both
        # `attestation_status=approved-unsupported` AND a populated
        # `manifest.attestation_bundle` is internally inconsistent — the fallback path
        # is by definition the no-bundle path. Reject so an attacker cannot smuggle a
        # stale bundle through alongside an approved fallback record.
        #
        # CR-12-4-P238 (round-10, EC-7): the original check only rejected dicts that
        # ALSO carried a `sha256` field. A hand-crafted bundle like
        # `{"path": "x.json", "kind": "..."}` (dict present, no sha256) slipped past.
        # The symmetric inverse for the `attested` branch's "dict + sha256 required"
        # is "any non-empty bundle is forbidden under approved-unsupported". Reject
        # any non-empty dict, plus any non-dict truthy value (string, list, number).
        leftover_bundle = manifest.get("attestation_bundle") if isinstance(manifest, dict) else None
        if isinstance(leftover_bundle, dict):
            if leftover_bundle:  # non-empty dict
                blocking.append("approved-unsupported fallback must not carry attestation_bundle evidence")
        elif leftover_bundle:  # non-dict truthy (string, list, number)
            blocking.append("approved-unsupported fallback must not carry attestation_bundle evidence (non-dict value)")
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
                # CR-12-4-P232 (round-9, from CR-12-4-D18): fold the helper's semver
                # `__version__` into the fallback digest input so a deliberate operator-
                # driven version bump invalidates the fallback approval. The producer
                # side (`fallback_invalidation_fingerprints`) embeds the same key/value
                # shape; both sides must agree for `fallback_complete` to validate.
                #
                # CR-12-4-P233 (round-10, BH-F3): the original consumer code only
                # appended `helper_version` to `fingerprints_for_drift` when
                # `manifest.helper_version` was a dict. A manifest with `helper_version:
                # null` (or scalar/missing) would compute a digest WITHOUT the key while
                # the producer ALWAYS adds it — guaranteed digest mismatch with no
                # actionable diagnostic. Now: always include `helper_version` (default
                # empty string when manifest field is missing or malformed) and append
                # a typed diagnostic so the operator sees the structural cause.
                #
                # CR-12-4-P245 (round-10, BH-F13): hash the version string for type
                # uniformity with the file-content hashes; mirrors the producer.
                # `manifest_diagnostics` (called above for both attested and
                # approved-unsupported paths) already emits a `manifest:` blocker when
                # `helper_version` is missing or non-dict — we don't re-raise. We only
                # need to compute the consumer-side digest contribution: hash an empty
                # version string when the field is malformed so the digest is
                # deterministically wrong and `fallback_complete` rejects via its
                # canonical_sha256 comparison rather than via a phantom-key mismatch.
                # CR-12-4-P257 (round-11, blind): use a non-empty sentinel when the
                # manifest field is malformed (missing/non-dict/missing-version-key)
                # so that an operator typo `__version__ = ""` cannot silently collide
                # with the consumer's default empty string. The sentinel `<missing>`
                # is not a valid semver (rejected at module load by the assertion at
                # `eng/release_evidence.py:36`), so the producer hash and consumer
                # hash of `<missing>` differ from any real `__version__`.
                manifest_helper_version = manifest.get("helper_version") if isinstance(manifest, dict) else None
                helper_version_value = "<missing>"
                if isinstance(manifest_helper_version, dict):
                    raw_version = manifest_helper_version.get("version")
                    if isinstance(raw_version, str) and raw_version:
                        helper_version_value = raw_version
                fingerprints_for_drift["helper_version"] = hashlib.sha256(helper_version_value.encode("utf-8")).hexdigest()
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
                        "approved_at",
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
            # CR-12-4-Def107 (AC34): when the approved fallback is scoped to a SPECIFIC
            # package artifact, the sealed manifest must actually ship that artifact — a
            # changed/mismatched affected artifact is a distinct invalidation trigger
            # requiring re-approval. `fallback_complete` only validates presence of
            # `affected_artifact`, never its value against the shipped rows, so the
            # cross-check lives here where the manifest rows are in scope.
            #
            # Scope rules (post-review hardening):
            #   - Only a value naming a concrete package artifact (a .nupkg/.snupkg
            #     filename) is artifact-scoped. The default sentinel ("release package
            #     set") and any non-filename label scope the approval to the whole
            #     inventory — whose drift is covered by the package-set fingerprint — so
            #     they are NOT cross-checked here. (Without this gate the production
            #     default "release package set" would block EVERY fallback release.)
            #   - Compare by normalized BASENAME EQUALITY, not suffix: a truncated or
            #     coincidental tail (e.g. "Contracts.1.2.3.nupkg" vs the real
            #     "Hexalith.FrontComposer.Contracts.1.2.3.nupkg") must NOT pass, and a
            #     leading "./" or "\\" path prefix must NOT falsely block.
            affected_artifact = str(fallback.get("affected_artifact", "")).strip()
            if affected_artifact.lower().endswith((".nupkg", ".snupkg")):
                affected_basename = affected_artifact.replace("\\", "/").rsplit("/", 1)[-1]
                shipped_basenames = {
                    str(row.get("artifact_path", "")).replace("\\", "/").rsplit("/", 1)[-1]
                    for row in (manifest.get("packages") if isinstance(manifest.get("packages"), list) else [])
                    if isinstance(row, dict)
                }
                if affected_basename not in shipped_basenames:
                    blocking.append(
                        f"fallback affected_artifact does not match any sealed manifest artifact; affected_artifact={sanitize(affected_artifact)}"
                    )
    elif status == "attested":
        # CR-12-4-P173 (round-7): when the sealed manifest claims `attestation_status=attested`
        # but carries no `attestation_bundle`, the bundle integrity proof is missing.
        # `prepare_manifest` enforces this on production runs, but a hand-crafted manifest
        # or a stale fixture could otherwise pass `ready` without bundle evidence. Reject
        # at classify time too.
        attestation_bundle = manifest.get("attestation_bundle") if isinstance(manifest, dict) else None
        if not isinstance(attestation_bundle, dict) or not attestation_bundle.get("sha256"):
            blocking.append("attested release missing manifest.attestation_bundle evidence")
    else:
        blocking.append(f"attestation status must be attested or approved-unsupported; actual={sanitize(status)}")

    classification = "blocked" if blocking else ("fallback-approved" if fallback_reasons else "ready")
    # CR-12-4-P247 (round-10, D21/BH-F2/EC-1): dry-run carve-out. Round-9 P217/P218
    # made the `--dry-run-clean-exit` path structurally unreachable: any dry-run
    # always classifies as `local-candidate` (line 1474-1475 above), which appends
    # the candidate-evidence blocker, which forces `classification = "blocked"`,
    # which fails the P218 gate `classification in {ready, fallback-approved}`.
    # Without this carve-out, every healthy dry-run reports red on the workflow
    # step status — defeating P214's "healthy dry-run reports green" intent.
    #
    # Carve-out conditions (must ALL hold):
    #   1. `context.dry_run` is True (the run is a dry-run dispatch);
    #      [CR-12-4-P255 round-11: the source field is `context.dry_run`, NOT
    #      `checks.dry_run` — `classify_context` reads from the context section
    #      at L1229 and `--dry-run` CLI flag is injected into the context block
    #      at L2423. The prior comment incorrectly named `checks.dry_run` and
    #      could mislead a future refactor.]
    #   2. `context_class == "local-candidate"` (the only context dry-runs reach);
    #   3. `blocking == [candidate-evidence-blocker]` (no OTHER blockers — a real
    #      defect like missing tests must keep classification=blocked);
    #   4. NOT mutually exclusive with the fallback path — CR-12-4-P264 (round-11,
    #      from D25) added a second arm: when fallback_reasons is non-empty AND
    #      the only blocker is the candidate evidence blocker, classification is
    #      reclassified to `fallback-approved` so the production attestation
    #      fallback path (Def14: ATTESTATION_UNSUPPORTED=true) can reach the
    #      dry-run-clean-exit gate.
    #
    # When EITHER arm fires, `classification` is promoted but `publish_authorized`
    # stays False (the gate at line 1750 requires `trusted-main-or-release` context;
    # local-candidate fails that). AC23 was reworded in round-11 (CR-12-4-D24/D21)
    # to explicitly permit this combination — see the AC23 row text and CR-12-4-P263.
    # A typed `helper_state` note records the carve-out for auditors (CR-12-4-P251
    # round-11), and the blocking list is rewritten so the typed contract is
    # internally consistent (no `classification=ready` + non-empty blocking).
    # Parse dry_run from the context section (where `classify_context` reads it from).
    # Re-parsing is safe: any diagnostic was already added by `classify_context` and
    # surfaced via `context_diagnostics`, so the carve-out check sees the value
    # without double-counting forensic noise.
    dry_run_flag, _ = parse_strict_bool(context.get("dry_run"), field="dry_run")
    candidate_blocker = _CANDIDATE_BLOCKER_TEMPLATE.format(context_class=context_class)
    _dry_run_pre = (
        dry_run_flag
        and context_class == "local-candidate"
        and classification == "blocked"
        and len(blocking) == 1
        and blocking[0] == candidate_blocker
    )
    dry_run_ready_carve_out = _dry_run_pre and not fallback_reasons
    dry_run_fallback_carve_out = _dry_run_pre and bool(fallback_reasons)
    dry_run_carve_out = dry_run_ready_carve_out or dry_run_fallback_carve_out
    if dry_run_ready_carve_out:
        classification = "ready"
    elif dry_run_fallback_carve_out:
        classification = "fallback-approved"
    # CR-12-4-P251 (round-11, edge+auditor): when the carve-out fires, also rewrite
    # `blocking` so the typed contract is internally consistent. A `classification=
    # ready` (or `fallback-approved`) record alongside non-empty `grouped_reasons.
    # blocking` previously violated D17 ("single typed classification contract").
    # Move the candidate-evidence blocker out of `blocking` and into a separate
    # `carve_out_advisory` field so audit trails preserve the trail without
    # contradicting the headline classification.
    carve_out_advisory: list[str] = []
    if dry_run_carve_out:
        carve_out_advisory = list(blocking)  # preserve trail
        blocking = []
    publish_authorized = classification in {"ready", "fallback-approved"} and context_class == "trusted-main-or-release" and approval_approved
    next_action = "publish may proceed with approved owner action" if publish_authorized else "resolve blocking release gates before publishing"
    if context_class == "rerun-review":
        next_action = "rerun-review contexts are never publish-authorized; create a fresh dispatch or new tag to retry"
    if classification == "fallback-approved":
        next_action = "release owner must consciously accept the approved fallback before publish"
    if dry_run_ready_carve_out:
        next_action = "dry-run carve-out: classification=ready with publish_authorized=false; merge to trusted-main-or-release to publish"
    elif dry_run_fallback_carve_out:
        next_action = "dry-run carve-out (fallback): classification=fallback-approved with publish_authorized=false; merge to trusted-main-or-release with explicit owner approval to publish"

    # CR-12-4-P251 (round-11): emit `helper_state` for the carve-out so auditors can
    # see the trail without inferring it from `classification` + `grouped_reasons`.
    helper_state_value = None
    if dry_run_ready_carve_out:
        helper_state_value = "dry-run-carve-out:ready"
    elif dry_run_fallback_carve_out:
        helper_state_value = "dry-run-carve-out:fallback-approved"

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
        # CR-12-4-P251 (round-11): when the carve-out fires, surface the original
        # candidate-evidence blocker as a structured advisory rather than letting it
        # contradict `classification`. Consumers reading the audit trail can still
        # see what would have blocked publish in a trusted context.
        "carve_out_advisory": [sanitize(reason) for reason in carve_out_advisory],
        "classification": classification,
        "context_class": context_class,
        "decision_contract": "frontcomposer.release-readiness.v1",
        "grouped_reasons": {
            "blocking": [sanitize(reason) for reason in blocking],
            "fallback": [sanitize(reason) for reason in fallback_reasons],
        },
        "fallback_record": fallback_record,
        "helper_state": helper_state_value,
        "next_owner_action": next_action,
        # CR-12-4-D8 (round-5): publish the package-set fingerprint as a separate
        # fingerprint surface so consumers can distinguish "package set changed" (new
        # or removed packages, exception status drift) from generic release-definition
        # drift (workflow/helper/inventory file edits). Routine Dependabot bumps inside
        # `Directory.Packages.props` no longer invalidate approved fallbacks because
        # that file is no longer in `RELEASE_DEFINITION_FILES`.
        # CR-12-4-P170 (round-7): when `verify_drift=False` (fixture mode) the live
        # fingerprints are environment-dependent and would leak per-machine hashes into
        # test output; an empty map keeps the contract field present but
        # value-stable across runners.
        "package_set_fingerprint": package_set_fingerprint(root) if verify_drift else None,
        "publish_authorized": publish_authorized,
        "release_definition_fingerprints": release_definition_fingerprints(root) if verify_drift else {},
        # CR-12-4-P230 (round-9, AA-005): AC30 lists `eng/release_evidence.py` as an
        # input that must be bound by an immutable identifier. P215 (round-8) moved
        # the helper out of `release_definition_fingerprints` and tracked it via the
        # sealed-manifest `helper_version` field, but the release-readiness audit
        # JSON did not expose the binding. Surface it alongside the other
        # release-definition fingerprints so release owners can verify all
        # release-definition identifiers from a single audit artifact.
        "helper_version": helper_version_record() if verify_drift else {},
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
    # CR-12-4-P167 (round-7): typed per-category counters. The release-readiness
    # consumer can now count failures by category without re-parsing the free-form
    # diagnostics strings.
    total_failed = 0
    total_errors = 0
    total_aborted = 0
    total_timeout = 0
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

        # CR-12-4-P196 (round-8): detect skipped/not-executed tests. A TRX with
        # ``executed=50, total=100`` previously classified ``test_status: passed`` with
        # ``test_count=50`` because the per-counter failure gates only checked
        # ``failed``/``error``/``aborted``/``timeout``. A workflow change that adds
        # ``--filter`` exclusions beyond the documented set, or a future regression that
        # skips tests silently, would slip past the test gate. Compare ``executed`` to
        # ``total`` per TRX and emit a typed diagnostic — failing closed via the same
        # diagnostics list that drives the test_status invalid signal.
        executed_per_trx = _counter("executed") if "executed" in counters.attrib else _counter("total")
        total_per_trx = _counter("total")
        if total_per_trx > executed_per_trx:
            skipped = total_per_trx - executed_per_trx
            diagnostics.append(
                f"{trx.name}: TRX records {skipped} skipped/not-executed test(s) "
                f"({executed_per_trx}/{total_per_trx} executed); release tests must all run"
            )
        executed += executed_per_trx
        total += total_per_trx
        failed_count = _counter("failed")
        error_count = _counter("error")
        aborted_count = _counter("aborted")
        timeout_count = _counter("timeout")
        total_failed += failed_count
        total_errors += error_count
        total_aborted += aborted_count
        total_timeout += timeout_count
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
        "aborted_count": total_aborted,
        "diagnostics": [sanitize(diagnostic) for diagnostic in diagnostics],
        "error_count": total_errors,
        "failed_count": total_failed,
        "status": "valid" if not diagnostics else "invalid",
        "test_count": executed,
        "timeout_count": total_timeout,
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
    # CR-12-4-P236 (round-10, EC-8): guard against non-dict top-level manifest. The
    # workflow's pre-classify JSON parse check (P227) validates parseability but not
    # dict shape. A sealed-manifest.json with top-level `[]`, `null`, or scalar
    # previously crashed via AttributeError on the first `manifest.get(...)` call in
    # `manifest_diagnostics` (exit 2 with traceback) instead of producing a typed
    # release-definition blocker. Treat as invalid and surface a typed diagnostic.
    if not isinstance(manifest, dict):
        diagnostics.append("sealed manifest must be an object")
        if args.output:
            write_json(args.output, {"status": "invalid", "diagnostics": diagnostics})
        return 1
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
    # CR-12-4-Def23: when no --output sink is given, surface the diagnostics on stdout so
    # the operator (and offline --no-root verification) can see WHY verification failed.
    # Printed to stdout, never stderr, to preserve the empty-stderr contract asserted by
    # the existing manifest-verification tests.
    elif diagnostics:
        for diagnostic in diagnostics:
            print(diagnostic)
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
                # CR-12-4-P178 (round-7): store the evidence-root-relative path rather
                # than the resolved absolute path. The sha256 already provides integrity;
                # the path is informational and a per-runner absolute path
                # (`/home/runner/work/...`) sealed into the manifest harms reproducibility
                # across environments. Falls back to the resolved string only if the
                # bundle somehow resolves outside the prepare root (would have raised
                # SystemExit above; here as defensive coding only).
                try:
                    relative_bundle = bundle_path.relative_to(prepare_root.resolve())
                    bundle_path_str = str(relative_bundle).replace("\\", "/")
                except ValueError:
                    bundle_path_str = str(bundle_path).replace("\\", "/")
                attestation_bundle = {
                    "path": bundle_path_str,
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
        # CR-12-4-P215 (round-8, from CR-12-4-D16): bind the helper's semantic version
        # AND content sha256. Replaces the prior `eng/release_evidence.py` entry in
        # `release_definition_fingerprints`. Helper-bytes change without a deliberate
        # `__version__` bump still fails closed via `manifest_diagnostics`.
        "helper_version": helper_version_record(),
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
    manifest_payload: dict[str, Any] | None = None
    if args.manifest and pathlib.Path(args.manifest).exists():
        raw_manifest = read_json(args.manifest)
        manifest_payload = raw_manifest if isinstance(raw_manifest, dict) else None
    if args.append_current:
        if not args.started_at:
            raise SystemExit("RELEASE_STARTED_AT must be set before --append-current")
        started = dt.datetime.fromisoformat(args.started_at.replace("Z", "+00:00"))
        ended = dt.datetime.fromisoformat(args.ended_at.replace("Z", "+00:00")) if args.ended_at else dt.datetime.now(dt.timezone.utc)
        minutes = max(0, (ended - started).total_seconds() / 60)
        package_count = int(args.package_count)
        tag = args.tag
        if manifest_payload is not None:
            # CR-12-4-P228 (round-9, EC-27): the prior `len(manifest_payload.get("packages", []))`
            # crashes with TypeError if the manifest has `"packages": null` or any other
            # non-list value. Guard via isinstance(list) and fall back to the CLI
            # --package-count argument with a typed diagnostic in the future readiness
            # surface (release_budget itself is monitoring-only; downstream consumers
            # see package_count from --package-count instead).
            packages_field = manifest_payload.get("packages")
            if isinstance(packages_field, list):
                package_count = len(packages_field)
            tag = str(manifest_payload.get("tag") or args.tag)
        releases = [
            *releases,
            {
                "tag": tag,
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
    # CR-12-4-P207 (round-8): a non-dict manifest (list, scalar, null) previously slipped
    # past existing isinstance checks because later code already uses `if isinstance(...)`
    # guards, but the operator-facing forensic JSON ended up missing the diagnostic. Emit
    # a typed `manifest is not an object` reason so downstream readers see WHY the
    # incident record has an empty manifest_seal.
    if not isinstance(manifest, dict):
        manifest_diagnostic = "manifest is not an object"
        manifest = {}
    if classification == "none":
        # CR-12-4-P127 (round-6): placeholder records must be reproducible across
        # runs so semantic-release's `@semantic-release/github` asset upload sees
        # stable checksums. Drop run-bound fields (manifest_seal sealed_at, run_id,
        # tag) from the placeholder. Real incidents keep all forensic provenance.
        # CR-12-4-P155 (round-7): refuse to overwrite an existing real incident
        # record. If the target path holds a JSON document with `failed_phase != "none"`
        # (a real incident), writing a placeholder over it silently destroys forensic
        # state. Operator copy-pasting the prepareCmd placeholder invocation as manual
        # recovery is the trigger; force the operator to pick a different output path.
        output_path = pathlib.Path(args.output)
        if output_path.is_file():
            try:
                existing = read_json(output_path)
            except SystemExit:
                # CR-12-4-P224 (round-9, BH-031/EC-12): the prior `existing = None`
                # fallback let a corrupted/truncated real incident be silently
                # overwritten by the placeholder, destroying forensic state. Fail
                # closed: the operator must investigate whether the unreadable file
                # is a partial-write of a real incident before clearing it.
                raise SystemExit(
                    f"--classification none refuses to overwrite an unreadable existing file at {sanitize(str(output_path))}; "
                    "the file may be a corrupted real partial-publish incident — investigate before clearing"
                )
            if (
                isinstance(existing, dict)
                and str(existing.get("failed_phase", "")).strip().lower() not in {"", "none"}
            ):
                raise SystemExit(
                    f"--classification none refuses to overwrite an existing real partial-publish incident at {sanitize(str(output_path))}; "
                    "pick a different --output path or remove the existing file deliberately"
                )
        # CR-12-4-P169 (round-7): split placeholder vs real-incident contract names so
        # consumers can dispatch on `decision_contract` instead of having to read
        # `failed_phase` / `classification` to disambiguate.
        payload = {
            "classification": "none",
            "decision_contract": "frontcomposer.partial-publish-placeholder.v1",
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


def fallback_digest(args: argparse.Namespace) -> int:
    """Print the canonical digest a fallback approval must record.

    CR-12-4-P163 (round-7): release owners previously had to reproduce the
    `canonical_sha256({"definition": filtered, "package_set": ...})` formula by hand to
    populate `vars.RELEASE_ATTESTATION_FALLBACK_FINGERPRINTS_SHA256`. Any mismatch was
    silently reported as `fallback approved against drifted release definition` which
    could send operators chasing a release-definition drift that did not exist.

    The subcommand emits the exact 64-char hex digest for the current on-disk
    `FALLBACK_INVALIDATION_FILES` + `package_set_fingerprint` so operators paste the
    value directly into the repository variable.
    """
    root = pathlib.Path(args.root)
    if not root.is_dir():
        raise SystemExit(f"--root must be an existing directory: {sanitize(str(root))}")
    fingerprints = fallback_invalidation_fingerprints(root)
    package_set = package_set_fingerprint(root)
    digest = canonical_sha256({"definition": fingerprints, "package_set": package_set})
    payload = {
        "decision_contract": "frontcomposer.fallback-digest.v1",
        "digest_sha256": digest,
        "fallback_invalidation_fingerprints": fingerprints,
        "package_set_fingerprint": package_set,
    }
    if args.output:
        write_json(args.output, payload)
    print(digest)
    return 0


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
                    "approved_at": args.fallback_approved_at,
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
                    raw_diags = guard_payload.get("probe_diagnostics")
                    if isinstance(raw_diags, list):
                        concurrency_probe_diagnostics = [
                            f"concurrency-probe: {sanitize(d)}" for d in raw_diags if d
                        ]
                    elif raw_diags is not None:
                        # CR-12-4-P205 (round-8): a producer writing a non-list
                        # ``probe_diagnostics`` (string, dict, scalar) previously silently
                        # iterated one character at a time (string case) or raised
                        # (other types). Emit a typed diagnostic and force fail-closed
                        # via concurrent_same_version=True so the malformed payload does
                        # not silently authorize a publish.
                        concurrency_probe_diagnostics = [
                            f"concurrency-probe: malformed probe_diagnostics type {type(raw_diags).__name__}"
                        ]
        else:
            concurrency_probe_diagnostics.append(
                f"concurrency-probe: --concurrency-guard path missing: {sanitize(guard_path_arg)}"
            )
    # CR-12-4-P246 (round-10, BH-F12): build augmented checks in a local dict and
    # spread `{**evidence, "checks": augmented}` into the classifier call rather than
    # mutating `evidence["checks"]` in place. The mutation pattern leaked state into
    # the caller's dict — a future refactor that calls `classify_release_payload`
    # twice for comparison would see the second call's evidence already mutated.
    # Mutation-free spread also pairs with the typed-evidence-as-read-only convention.
    if concurrency_probe_diagnostics:
        existing_checks = evidence.get("checks") if isinstance(evidence, dict) else None
        if not isinstance(existing_checks, dict):
            existing_checks = {}
            concurrency_probe_diagnostics.insert(0, "checks section missing or malformed while applying concurrency guard")
        raw_check_diagnostics = existing_checks.get("diagnostics", [])
        check_diagnostics = raw_check_diagnostics if isinstance(raw_check_diagnostics, list) else []
        augmented_checks = {
            **existing_checks,
            "concurrent_same_version": True,
            "helper_state": "partial",
            "diagnostics": [
                *check_diagnostics,
                *concurrency_probe_diagnostics,
            ],
        }
        evidence_for_classify = {**evidence, "checks": augmented_checks} if isinstance(evidence, dict) else {"checks": augmented_checks}
    else:
        evidence_for_classify = evidence
    decision = classify_release_payload(evidence_for_classify, pathlib.Path(args.root), evidence_root=evidence_root_arg)
    # CR-12-4-P209 (round-8): release owners reading only `grouped_reasons.blocking`
    # need to distinguish PROBE-DEGRADED (infrastructure issue → retry) from a REAL
    # concurrent run (operational issue → wait). The headline reason emitted by
    # `classify_release_payload` is the generic "same-version concurrent or stale
    # release attempt requires owner review"; rewrite it to a probe-degraded variant
    # when the blocking trail originated from concurrency_probe_diagnostics. The full
    # diagnostic detail is already in `checks.diagnostics`.
    if concurrency_probe_diagnostics:
        blocking = decision.get("grouped_reasons", {}).get("blocking", [])
        decision["grouped_reasons"]["blocking"] = [
            (
                "concurrency-probe-degraded: same-version probe failed (see checks.diagnostics) "
                "and cannot rule out concurrent release run"
                if "same-version concurrent or stale release attempt" in b
                else b
            )
            for b in blocking
        ]
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
        blocking = decision["grouped_reasons"]["blocking"]
        # CR-12-4-P214 (round-8, from CR-12-4-D14): when the only remaining blockers
        # are the dry-run / local-candidate context (i.e., the underlying evidence
        # would classify as `ready` / `fallback-approved` in a trusted-write-capable
        # context), exit 0 so release owners see a healthy dry-run as green. The
        # typed readiness JSON still records the blocker for auditability; only the
        # workflow EXIT code differs.
        # CR-12-4-P217/P218 (round-9, BH-005/EC-1/EC-2/EC-3): tightened from
        # prefix-match to an exact-string allowlist AND gated on
        # `classification in {"ready", "fallback-approved"}`. A future blocking reason
        # like "dry-run side-effect attempt detected" would have falsely matched the
        # prior `startswith("dry-run")` predicate and slipped past the exit-0 gate.
        # CR-12-4-P247 (round-10, D21/BH-F2/EC-1): the P218 gate had made this path
        # structurally dead because dry-runs always classified as `blocked`. The
        # corresponding carve-out in `classify_release_payload` now reclassifies
        # healthy dry-runs as `classification="ready"` (with `publish_authorized=false`),
        # so the gate fires when intended. Also dropped the dead allowlist literal
        # `"dry-run dispatch cannot authorize publishing"` — only the f-string
        # `f"candidate evidence from {context_class} ..."` is emitted by the
        # classifier, and only the `local-candidate` instance is allowed here.
        if getattr(args, "dry_run_clean_exit", False) and blocking:
            # CR-12-4-P256 (round-11, edge): allowlist materialized from the
            # module-level template so it cannot drift from the carve-out emitter.
            allowed_blockers = {
                _CANDIDATE_BLOCKER_TEMPLATE.format(context_class="local-candidate"),
            }
            classification_ready = decision["classification"] in {"ready", "fallback-approved"}
            if classification_ready and all(b in allowed_blockers for b in blocking):
                print(
                    "Classification: would-be-publishable in trusted context "
                    "(dry-run; publish blocked pending owner approval)",
                    file=sys.stderr,
                )
                return 0
        print("; ".join(blocking) or decision["classification"], file=sys.stderr)
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
            # CR-12-4-Def22/25/102/103/106/107: surface the typed reason groups so the
            # fixture corpus can assert WHICH diagnostic fired (not just the headline
            # classification), e.g. "package rows are required" or the affected_artifact
            # mismatch. Without this the red-phase tests cannot read grouped_reasons.blocking.
            "grouped_reasons": decision["grouped_reasons"],
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
    # CR-12-4-P214 (round-8, from CR-12-4-D14): allow the dry-run branch to exit 0 when
    # the only blockers are the dry-run / local-candidate context state. Off by default
    # so the existing trusted-context publish path (which expects ANY blocker to fail
    # closed) is unchanged.
    classify.add_argument("--dry-run-clean-exit", action="store_true")
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
    classify.add_argument("--fallback-approved-at", default=os.environ.get("RELEASE_ATTESTATION_FALLBACK_APPROVED_AT", ""))
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

    # CR-12-4-P163 (round-7): expose the fallback canonical digest so release owners
    # can read the value without reimplementing the formula by hand.
    fdigest = sub.add_parser("fallback-digest")
    fdigest.add_argument("--root", default=".")
    fdigest.add_argument("--output")
    fdigest.set_defaults(func=fallback_digest)

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
