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


def sanitize(value: Any, max_len: int = MAX_FIELD) -> str:
    text = "" if value is None else str(value)
    text = text.replace("\r", " ").replace("\n", " ")
    text = re.sub(r"(?i)bearer\s+[A-Za-z0-9._~+/=-]+", "Bearer [REDACTED]", text)
    text = re.sub(r"\b(?:sk-|ghp_|github_pat_|xox[baprs]-)[-A-Za-z0-9_]{12,}\b", "[REDACTED_TOKEN]", text)
    text = re.sub(r"\b[A-Z]:\\[^ ]+", "[LOCAL_PATH]", text)
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
    protected = str(args.ref_protected).lower() == "true"
    trusted_ref = args.ref in {"refs/heads/main", "main"} or protected
    fork = str(args.from_fork).lower() == "true"
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


def inventory(args: argparse.Namespace) -> int:
    repo = pathlib.Path(args.root)
    expected = read_json(args.expected)
    rows: list[dict[str, Any]] = []
    diagnostics: list[str] = []
    for item in expected.get("packages", []):
        project = repo / item["project"]
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


def verify_manifest(args: argparse.Namespace) -> int:
    manifest = read_json(args.manifest)
    diagnostics: list[str] = []
    rows = manifest.get("packages", [])
    if not isinstance(rows, list) or not rows:
        diagnostics.append("package rows are required")
    version = None
    for row in rows:
        for field in REQUIRED_ROW_FIELDS:
            if not row.get(field):
                diagnostics.append(f"{row.get('package_id', '<unknown>')}: missing {field}")
        version = version or row.get("version")
        if row.get("version") != version:
            diagnostics.append("lockstep version drift")
        if row.get("signing_status") != "verified":
            diagnostics.append(f"{row.get('package_id')}: signing not verified")
        if row.get("attestation_status") not in {"attested", "approved-unsupported"}:
            diagnostics.append(f"{row.get('package_id')}: attestation state invalid")
    for field in ["commit_sha", "tag", "run_id", "workflow_ref", "sbom_hash", "benchmark_summary_hash"]:
        if not manifest.get(field):
            diagnostics.append(f"manifest missing {field}")
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
    sbom_hash = args.sbom_hash
    if not sbom_hash:
        sbom_hash = next((v for k, v in checksums_by_path.items() if k.startswith("release-evidence/sbom/")), "")
    for row in inventory_payload.get("rows", []):
        if not row.get("packable"):
            continue
        package_id = row["package_id"]
        nupkg = f"nupkgs-signed/{package_id}.{args.version}.nupkg"
        snupkg = f"nupkgs/{package_id}.{args.version}.snupkg"
        packages.append({
            "package_id": package_id,
            "version": args.version,
            "commit_sha": args.commit_sha,
            "artifact_path": nupkg,
            "checksum": checksums_by_path.get(nupkg, "pending-checksum"),
            "symbol_artifact": snupkg if row.get("symbol_required") else row.get("exception", "not-required"),
            "sbom_component": package_id,
            "signing_status": "verified",
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
    }
    write_json(args.output, manifest)
    return 0


def release_budget(args: argparse.Namespace) -> int:
    raw = read_json(args.evidence)
    releases = raw.get("releases", raw) if isinstance(raw, dict) else raw
    if not isinstance(releases, list):
        raise SystemExit("invalid release budget evidence: expected releases list")
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

    ver = sub.add_parser("verify-manifest")
    ver.add_argument("--manifest", required=True)
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
    prep.add_argument("--commit-sha", default=os.environ.get("GITHUB_SHA", "local"))
    prep.add_argument("--tag", required=True)
    prep.add_argument("--run-id", default=os.environ.get("GITHUB_RUN_ID", "local"))
    prep.add_argument("--workflow-ref", default=os.environ.get("GITHUB_WORKFLOW_REF", "local"))
    prep.add_argument("--sbom-hash", default="")
    prep.add_argument("--benchmark-summary-hash", default="candidate-benchmark-summary")
    prep.add_argument("--attestation-status", default="approved-unsupported")
    prep.set_defaults(func=prepare_manifest)

    budget = sub.add_parser("release-budget")
    budget.add_argument("--evidence", required=True)
    budget.add_argument("--output")
    budget.add_argument("--apply", action="store_true")
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

    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
