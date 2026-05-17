#!/usr/bin/env python3
"""Release evidence governance for package inventory, sealed manifests, and budgets."""

from __future__ import annotations

import argparse
import datetime as dt
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
RELEASE_DEFINITION_FILES = [
    ".github/workflows/release.yml",
    ".releaserc.json",
    "eng/release_evidence.py",
    "eng/release-package-inventory.json",
    "Directory.Packages.props",
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
APPROVAL_TRUTHY = TRUTHY_LITERALS | {"1", "yes", "approved"}
APPROVAL_FALSY = FALSY_LITERALS | {"0", "no", "denied"}


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
    p = pathlib.Path(path)
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def sha256_file(path: pathlib.Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def sha256_text(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


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


def discover_projects(repo: pathlib.Path) -> list[pathlib.Path]:
    src = repo / "src"
    projects = []
    for project in src.rglob("*.csproj"):
        parts = {p.lower() for p in project.parts}
        if "bin" not in parts and "obj" not in parts:
            projects.append(project)
    return sorted(projects, key=lambda p: str(p.relative_to(repo)).replace("\\", "/"))


def is_placeholder(value: Any) -> bool:
    text = str(value or "")
    return not text or text.startswith("pending-")


def looks_like_sha256(value: Any) -> bool:
    return bool(SHA256_RE.fullmatch(str(value or "")))


def parse_strict_bool(value: Any, *, field: str, default: bool = False) -> tuple[bool, str | None]:
    """Strict security-flag boolean (true/false only, case-insensitive). Empty -> default."""
    if isinstance(value, bool):
        return value, None
    text = "" if value is None else str(value).strip().lower()
    if text == "":
        return default, None
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
    """Parse YYYY-MM-DD or ISO-8601 datetime into a UTC date. Returns (date, diagnostic)."""
    if value is None or str(value).strip() == "":
        return None, "expires_at is missing"
    text = str(value).strip()
    try:
        return dt.date.fromisoformat(text), None
    except ValueError:
        pass
    try:
        normalized = text.replace("Z", "+00:00")
        parsed = dt.datetime.fromisoformat(normalized)
    except ValueError:
        return None, f"expires_at must be YYYY-MM-DD or ISO-8601 datetime; actual={sanitize(value)}"
    # Treat naive datetimes as UTC to avoid silent local-zone drift; normalize aware
    # datetimes to UTC so the date comparison cannot be off-by-one near midnight.
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=dt.timezone.utc)
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
            diagnostics.append(f"{name}: fingerprint {actual} != baseline {expected}")
    for name in current:
        if name not in baseline:
            diagnostics.append(f"{name}: fingerprint has no baseline entry")
    return diagnostics


def parse_signing_verification(text: str) -> dict[str, dict[str, str]]:
    """Parse `dotnet nuget verify --all` output for per-package verification status.

    `dotnet nuget verify --all` succeeds only when both the author signature and the
    RFC 3161 timestamp counter-signature are valid for the package, so a per-package
    'Successfully verified package' line is sufficient evidence for both gates.
    """
    statuses: dict[str, dict[str, str]] = {}
    pattern = re.compile(
        r"Successfully verified package '([A-Za-z0-9._-]+?)\.(\d[0-9A-Za-z.+\-]*)'"
    )
    for match in pattern.finditer(text):
        statuses[match.group(1)] = {
            "signing_status": "verified",
            "timestamp_status": "verified",
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
    if not isinstance(rows, list) or not rows:
        diagnostics.append("package rows are required")
    if isinstance(rows, list):
        versions = {row.get("version") for row in rows if isinstance(row, dict)}
        if len(versions) > 1:
            diagnostics.append("lockstep version drift")
    for row in rows if isinstance(rows, list) else []:
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
    if root is not None:
        embedded = manifest.get("release_definition_fingerprints")
        if embedded is None:
            diagnostics.append("manifest missing release_definition_fingerprints")
        elif not isinstance(embedded, dict):
            diagnostics.append("manifest release_definition_fingerprints must be an object")
        else:
            current = release_definition_fingerprints(root)
            for drift in fingerprint_diff(current, embedded):
                diagnostics.append(f"release-definition drift: {drift}")
    return diagnostics


def safe_run_attempt(value: Any) -> tuple[int, str | None]:
    """Coerce a run_attempt value to int. Returns (value, diagnostic-or-None)."""
    if value is None or value == "":
        return 1, None
    try:
        return int(value), None
    except (TypeError, ValueError):
        return 1, f"run_attempt must be numeric; actual={sanitize(value)}"


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


def fallback_complete(fallback: dict[str, Any]) -> tuple[bool, str | None]:
    """Validate fallback completeness. Returns (complete, diagnostic-or-None)."""
    required = [
        "affected_artifact",
        "approver",
        "evidence",
        "expires_at",
        "reason",
        "release_note_impact",
        "reopen_event",
        "scope",
    ]
    missing = [field for field in required if not fallback.get(field)]
    if missing:
        return False, f"fallback missing required field(s): {', '.join(missing)}"
    expiry, expiry_diagnostic = parse_expiry(fallback["expires_at"])
    if expiry is None:
        return False, expiry_diagnostic
    if expiry < dt.datetime.now(dt.timezone.utc).date():
        return False, f"fallback expired on {expiry.isoformat()}"
    return True, None


def contains_dangerous_evidence(value: Any) -> bool:
    text = json.dumps(value, sort_keys=True) if isinstance(value, (dict, list)) else str(value or "")
    return any(pattern.search(text) for pattern in DANGEROUS_EVIDENCE_PATTERNS)


def classify_release_payload(evidence: dict[str, Any], root: pathlib.Path, *, verify_drift: bool = True) -> dict[str, Any]:
    context = evidence.get("context", {})
    checks = evidence.get("checks", {})
    approval = evidence.get("approval", {})
    attestation = evidence.get("attestation", {})
    manifest = evidence.get("manifest", {})
    context_class, context_diagnostics = classify_context(context if isinstance(context, dict) else {})
    blocking: list[str] = []
    fallback_reasons: list[str] = []

    for diag in context_diagnostics:
        blocking.append(f"context: {diag}")

    if context_class != "trusted-main-or-release":
        blocking.append(f"candidate evidence from {context_class} cannot authorize publishing")
    if not approval.get("approved") or not approval.get("approver"):
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
    if not checks.get("trx_present"):
        blocking.append("release TRX evidence is required")
    if checks.get("dry_run_side_effect_attempt"):
        blocking.append("dry-run side-effect attempt was detected")
    if checks.get("recursive_submodule_command"):
        blocking.append("recursive nested submodule command is not allowed")
    if checks.get("release_definition_drift"):
        blocking.append("release-definition fingerprints drifted after evidence generation")
    if checks.get("post_seal_artifact_mutation"):
        blocking.append("post-seal artifact mutation detected")
    if checks.get("concurrent_same_version"):
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
    if status == "approved-unsupported":
        fallback = attestation.get("fallback", {})
        if not isinstance(fallback, dict):
            blocking.append("unsupported-attestation fallback is missing, stale, or incomplete")
        else:
            complete, fallback_diagnostic = fallback_complete(fallback)
            if complete:
                fallback_reasons.append("GitHub artifact attestation unavailable; approved unsupported-attestation fallback is in force")
            else:
                blocking.append(fallback_diagnostic or "unsupported-attestation fallback is missing, stale, or incomplete")
    elif status != "attested":
        blocking.append(f"attestation status must be attested or approved-unsupported; actual={sanitize(status)}")

    classification = "blocked" if blocking else ("fallback-approved" if fallback_reasons else "ready")
    publish_authorized = classification in {"ready", "fallback-approved"} and context_class == "trusted-main-or-release" and bool(approval.get("approved"))
    next_action = "publish may proceed with approved owner action" if publish_authorized else "resolve blocking release gates before publishing"
    if classification == "fallback-approved":
        next_action = "release owner must consciously accept the approved fallback before publish"

    return {
        "approval": {
            "approved": bool(approval.get("approved")),
            "approver": sanitize(approval.get("approver", "")),
            "mechanism": sanitize(approval.get("mechanism", "")),
        },
        "candidate_evidence_used": context_class != "trusted-main-or-release",
        "classification": classification,
        "context_class": context_class,
        "decision_contract": "frontcomposer.release-readiness.v1",
        "grouped_reasons": {
            "blocking": [sanitize(reason) for reason in blocking],
            "fallback": [sanitize(reason) for reason in fallback_reasons],
        },
        "next_owner_action": next_action,
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
        executed += int(counters.attrib.get("executed", counters.attrib.get("total", "0")))
        total += int(counters.attrib.get("total", "0"))
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
    # `--root` enables release-definition drift comparison against current files.
    # Verifier invocations from fixtures may omit it; production callers always pass it.
    root = pathlib.Path(args.root) if args.root else None
    diagnostics = manifest_diagnostics(manifest, root)
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
    if args.signing_verification:
        verification_path = pathlib.Path(args.signing_verification)
        if not verification_path.exists():
            diagnostics.append(f"signing verification evidence is missing: {sanitize(str(verification_path))}")
        else:
            verification_text = verification_path.read_text(encoding="utf-8", errors="replace")
            verification_statuses = parse_signing_verification(verification_text)
            if not verification_statuses:
                diagnostics.append("signing verification evidence did not name any verified package")
    elif args.attestation_status != "approved-unsupported":
        # When the workflow claims a real attestation path it must also provide signing evidence.
        diagnostics.append("signing verification evidence is required for non-fallback releases")
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
        verification = verification_statuses.get(package_id, {})
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
    }
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


def _cli_bool(value: Any, *, field: str, approval: bool = False) -> bool:
    """Parse a CLI boolean flag, failing loudly on unrecognized input."""
    parser = parse_approval_bool if approval else parse_strict_bool
    parsed, diagnostic = parser(value, field=field)
    if diagnostic:
        raise SystemExit(f"classify-release: invalid --{field.replace('_', '-')}: {diagnostic}")
    return parsed


def classify_release(args: argparse.Namespace) -> int:
    if args.evidence:
        evidence = read_json(args.evidence)
    else:
        manifest = read_json(args.manifest) if args.manifest else {}
        test_payload = read_json(args.test_results) if args.test_results else {}
        evidence = {
            "approval": {
                "approved": _cli_bool(args.owner_approved, field="owner_approved", approval=True),
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
                },
                "status": args.attestation_status,
            },
            "checks": {
                "checksums_status": args.checksums_status,
                "concurrent_same_version": _cli_bool(args.concurrent_same_version, field="concurrent_same_version"),
                "dry_run_side_effect_attempt": _cli_bool(args.dry_run_side_effect_attempt, field="dry_run_side_effect_attempt"),
                "helper_state": args.helper_state,
                "inventory_status": args.inventory_status,
                "paths_status": args.paths_status,
                "post_seal_artifact_mutation": _cli_bool(args.post_seal_artifact_mutation, field="post_seal_artifact_mutation"),
                "raw_evidence": args.raw_evidence,
                "recursive_submodule_command": _cli_bool(args.recursive_submodule_command, field="recursive_submodule_command"),
                "redaction_status": args.redaction_status,
                "release_definition_drift": _cli_bool(args.release_definition_drift, field="release_definition_drift"),
                "sbom_status": args.sbom_status,
                "semantic_release_state": args.semantic_release_state,
                "signing_status": args.signing_status,
                "test_count": int(test_payload.get("test_count", args.test_count) or 0),
                "test_status": "passed" if test_payload.get("status", args.test_status) == "valid" else test_payload.get("status", args.test_status),
                "timestamp_status": args.timestamp_status,
                "trx_present": bool(test_payload.get("trx_present", _cli_bool(args.trx_present, field="trx_present"))),
            },
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
    decision = classify_release_payload(evidence, pathlib.Path(args.root))
    if args.output:
        write_json(args.output, decision)
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
            "publish_authorized": decision["publish_authorized"],
        }
        expected_classification = case.get("expected_classification")
        expected_context = case.get("expected_context_class")
        expected_publish = case.get("expected_publish_authorized")
        if expected_classification and decision["classification"] != expected_classification:
            mismatches.append(f"{case.get('name')}: classification {decision['classification']} != {expected_classification}")
        if expected_context and decision["context_class"] != expected_context:
            mismatches.append(f"{case.get('name')}: context {decision['context_class']} != {expected_context}")
        if expected_publish is not None and decision["publish_authorized"] != expected_publish:
            mismatches.append(f"{case.get('name')}: publish_authorized {decision['publish_authorized']} != {expected_publish}")
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
    ver.add_argument("--root", default="")
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
    prep.add_argument("--benchmark-summary-hash", default="candidate-benchmark-summary")
    prep.add_argument("--attestation-status", default=os.environ.get("RELEASE_ATTESTATION_STATUS", "approved-unsupported"))
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

    classify = sub.add_parser("classify-release")
    classify.add_argument("--root", default=".")
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
    classify.add_argument("--fallback-evidence", default="release-evidence/attestation-unavailable.md")
    classify.add_argument("--fallback-expires-at", default=os.environ.get("RELEASE_ATTESTATION_FALLBACK_EXPIRES_AT", ""))
    classify.add_argument("--fallback-reason", default="GitHub artifact attestations unavailable in this repository context")
    classify.add_argument("--fallback-release-note-impact", default="Release notes must mention checksum, signature, SBOM, commit, tag, run, and workflow provenance without GitHub attestation.")
    classify.add_argument("--fallback-reopen-event", default="GitHub artifact attestations become available or release evidence contract changes")
    classify.add_argument("--fallback-scope", default="current release attempt")
    classify.add_argument("--checksums-status", default="valid")
    classify.add_argument("--concurrent-same-version", default="false")
    classify.add_argument("--dry-run-side-effect-attempt", default="false")
    classify.add_argument("--helper-state", default="success")
    classify.add_argument("--inventory-status", default="valid")
    classify.add_argument("--paths-status", default="normalized")
    classify.add_argument("--post-seal-artifact-mutation", default="false")
    classify.add_argument("--raw-evidence", default="")
    classify.add_argument("--recursive-submodule-command", default="false")
    classify.add_argument("--redaction-status", default="passed")
    classify.add_argument("--release-definition-drift", default="false")
    classify.add_argument("--sbom-status", default="present")
    classify.add_argument("--semantic-release-state", default="matches")
    classify.add_argument("--signing-status", default="verified")
    classify.add_argument("--test-count", type=int, default=1)
    classify.add_argument("--test-status", default="passed")
    classify.add_argument("--timestamp-status", default="verified")
    classify.add_argument("--trx-present", default="true")
    classify.set_defaults(func=classify_release)

    fixtures = sub.add_parser("classify-fixtures")
    fixtures.add_argument("--root", default=".")
    fixtures.add_argument("--fixtures", required=True)
    fixtures.add_argument("--output")
    fixtures.set_defaults(func=classify_fixtures)

    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
