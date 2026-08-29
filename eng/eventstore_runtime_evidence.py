#!/usr/bin/env python3
"""Validate the FrontComposer-owned EventStore Story 11.24 evidence snapshot."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
from datetime import datetime
from pathlib import Path, PurePosixPath
from typing import Any


SOURCE_SHA = "bb94d93e9b84132cff83a38fba84f25455820d31"
BUILDS_SHA = "a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a"
CAPTURE_SOURCE_SHA = "29de2507767dc061b923e4e6e40fbe1ea69f932e"
RELEASE_EXECUTION_SHA = "f75daebd4c522c081a6f62e274cf25e07971de69"
VERSION = "3.91.1"
SUBJECT_SHA256 = "9d074dfd0758a8934f122aab18659627dff1cf5d4c3e548b222cc0d79a881065"
INVENTORY_SHA256 = "6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae"
CAPTURED_SUCCESSOR_SHA256 = "69b08aba7758de770888aea53b9b51dc7e479220c9d539ed670db479fdb0164a"
PACKAGE_MANIFEST_SHA256 = "b85b9926482b42fda508b68e26162f256892d2f49c2eab31adbae49cefdd0d12"
SUBJECT_FROZEN_AT = "2026-08-10T07:06:11Z"
CONSUMER_SCOPE = "Hexalith.FrontComposer Story 11.24"
AUTHORIZED_ACTOR = "github:jpiquot"
MAX_FILE_BYTES = 1_048_576
MAX_TOTAL_BYTES = 2_097_152
MAX_RUN_MILLISECONDS = 300_000
PACT_FILES = (
    "frontcomposer-eventstore-command-dispatch.json",
    "frontcomposer-eventstore-query-execution.json",
    "frontcomposer-eventstore-cache-validation.json",
    "frontcomposer-eventstore-auth-tenant-propagation.json",
)
SUBJECT_DIR = f"{SOURCE_SHA}"
RECEIPT_DIR = f"{SUBJECT_DIR}/acceptances/{SUBJECT_SHA256}"
REQUIRED_SNAPSHOT_FILES = frozenset(
    {
        "frontcomposer-11-24-runtime-identity-successor.md",
        f"{SUBJECT_DIR}/nuget-sha256.txt",
        f"{SUBJECT_DIR}/owner-actions.md",
        f"{SUBJECT_DIR}/package-manifest.json",
        f"{SUBJECT_DIR}/release-catalog-provenance.json",
        f"{SUBJECT_DIR}/restore-receipt.json",
        f"{SUBJECT_DIR}/review-subject.json",
        f"{SUBJECT_DIR}/reviewer-roster.json",
        f"{RECEIPT_DIR}/eventstore-owner.json",
        f"{RECEIPT_DIR}/release-owner.json",
        "provider-verification/provider-verification.json",
        "provider-verification/run-evidence.json",
        "apphost-smoke/apphost-smoke.json",
        "release-restore/release-restore.json",
    }
)
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")
SOURCE_SHA_RE = re.compile(r"^[0-9a-f]{40}$")
LOCAL_PATH_PATTERNS = (
    re.compile(r"[A-Za-z]:\\Users\\", re.IGNORECASE),
    re.compile(r"/(?:home|Users)/[^/\s]+/"),
)
SECRET_PATTERNS = (
    re.compile(r"access_token\s*[=:]", re.IGNORECASE),
    re.compile(r"api_key\s*[=:]", re.IGNORECASE),
    re.compile(r"password\s*[=:]", re.IGNORECASE),
    re.compile(r"set-cookie\s*:", re.IGNORECASE),
    re.compile(r"Bearer\s+[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+"),
)
HASH_FIELD_RE = re.compile(r"(?:sha|hash|checksum|digest|fingerprint)", re.IGNORECASE)
ENCODED_TOKEN_RE = re.compile(r"^[A-Za-z0-9+/]{64,}={0,2}$")
APPHOST_OBSERVATIONS = (
    "health",
    "commandSubmit",
    "commandStatus",
    "queryProvenance",
    "projectionSignalR",
)
REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
EXPECTED_RECEIPTS = {
    "eventstore-owner.json": {
        "role": "eventstore-owner",
        "durable_source": "https://github.com/Hexalith/Hexalith.EventStore/issues/342#issuecomment-5265927577",
        "statement": "I accept this exact EventStore source and signed NuGet.org package identity for Hexalith.FrontComposer Story 11.24 only.",
    },
    "release-owner.json": {
        "role": "release-owner",
        "durable_source": "https://github.com/Hexalith/Hexalith.EventStore/issues/342#issuecomment-5265701569",
        "statement": "I authorize this exact EventStore source and signed NuGet.org package identity for migration by Hexalith.FrontComposer Story 11.24 only.",
    },
}
REQUIRED_RECEIPT_FIELDS = frozenset(
    {
        "schema",
        "subject_sha256",
        "subject_frozen_at",
        "actor",
        "role",
        "decision",
        "source_sha",
        "version",
        "consumer_scope",
        "accepted_at",
        "durable_source",
        "statement",
    }
)
APPROVED_PACKAGE_HASHES = {
    "Hexalith.EventStore.Contracts": "17eb87a48b797a8793cc93698260c007e62c6a1bfe79d8f431ec55a174599ca3",
    "Hexalith.EventStore.Client": "53a2ce3fee5abfad1251e1ac55f442a3edab962425dfc3d798e2129abcecf16a",
    "Hexalith.EventStore.Server": "dd818a85f3286ca950e5e82245b9470fde63d9875c8dd2ddcf7a9f529bd2ad14",
    "Hexalith.EventStore.SignalR": "31813bc71e18908ee681eafafcd9ae3b2fd134f5694b7100a422465adc9ef7e4",
    "Hexalith.EventStore.Testing": "c3207d0bb777eca7d04af9c0edeca50f7797b80b62f72e0a1579b01d974c5e03",
    "Hexalith.EventStore.Testing.Integration": "4b17be90fdf55dc0a5bfa537f247d0d9a512092725911909e7ace6566c9dca7d",
    "Hexalith.EventStore.Aspire": "e8e8894002ae1e9388f59a33c6f061599d0acb47f7ac658be0d3917e1ba3f387",
    "Hexalith.EventStore.ServiceDefaults": "8c63215e046017f8f7d14fd74079d518a3c5559e5c7be79a642247bf6361e47a",
    "Hexalith.EventStore.DomainService": "01980db86f97adabf5a67145c6257a9169cc86a4dd7b0124a48ef7ae9b7bc00c",
    "Hexalith.EventStore.RestApi.Generators": "6495ca90d963cc35a832fc09b272736e351ebb74fe4a72fce62b8bca2598104f",
    "Hexalith.EventStore.Gateway": "393a244b9f9fd1848dada6bb77293ef4f59112863bfab6990fba10d6d5fdd942",
    "Hexalith.EventStore.Admin.Abstractions": "dbb1112830fd3a4345def53eea236862121deffc3e066a150f187b745d817a08",
    "Hexalith.EventStore.Admin.Cli": "073c3aad46329cfc11576f82287298c96c9cf11ef14e3508e43a0bb58e7c7f33",
    "Hexalith.EventStore.Admin.Server": "e920e5ef9461ebd81f9181086e7b77e0a0e95f75c3dfea116c9eb437243c93c6",
}


class _DuplicateJsonKeyError(ValueError):
    pass


def _reject_duplicate_keys(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    value: dict[str, Any] = {}
    for key, item in pairs:
        if key in value:
            raise _DuplicateJsonKeyError(f"duplicate key {key!r}")
        value[key] = item
    return value


def _exact(actual: Any, expected: Any) -> bool:
    return type(actual) is type(expected) and actual == expected


def _path_has_symlink_component(path: Path) -> bool:
    absolute = path.absolute()
    current = Path(absolute.anchor)
    for part in absolute.parts[1:]:
        current /= part
        try:
            if current.is_symlink():
                return True
        except OSError:
            return True
    return False


def _bounded_read(path: Path, errors: list[str], label: str | None = None) -> bytes | None:
    display = label or path.name
    if _path_has_symlink_component(path):
        errors.append(f"Evidence path contains a symlink: {display}")
        return None
    try:
        stat = path.stat()
    except OSError:
        errors.append(f"Evidence file is missing or unreadable: {display}")
        return None
    if not path.is_file():
        errors.append(f"Evidence path is not a regular file: {display}")
        return None
    if stat.st_size <= 0 or stat.st_size > MAX_FILE_BYTES:
        errors.append(f"Evidence file is empty or exceeds {MAX_FILE_BYTES} bytes: {display}")
        return None
    try:
        with path.open("rb") as stream:
            data = stream.read(MAX_FILE_BYTES + 1)
    except OSError:
        errors.append(f"Evidence file is unreadable: {display}")
        return None
    if len(data) != stat.st_size or len(data) > MAX_FILE_BYTES:
        errors.append(f"Evidence file changed while read or exceeds {MAX_FILE_BYTES} bytes: {display}")
        return None
    return data


def _read_json(path: Path, errors: list[str], label: str | None = None) -> dict[str, Any]:
    data = _bounded_read(path, errors, label)
    if data is None:
        return {}
    try:
        value = json.loads(data.decode("utf-8-sig"), object_pairs_hook=_reject_duplicate_keys)
    except (UnicodeDecodeError, json.JSONDecodeError, _DuplicateJsonKeyError) as error:
        errors.append(f"{label or path.name} is not valid duplicate-free UTF-8 JSON: {error}")
        return {}
    if not isinstance(value, dict):
        errors.append(f"{path.name} must contain one JSON object.")
        return {}
    return value


def _sha256(path: Path, errors: list[str], label: str | None = None) -> str:
    data = _bounded_read(path, errors, label)
    return hashlib.sha256(data).hexdigest() if data is not None else ""


def _sha256_crlf_checkout(path: Path, errors: list[str], label: str | None = None) -> str:
    data = _bounded_read(path, errors, label)
    if data is None:
        return ""
    normalized = data.replace(b"\r\n", b"\n").replace(b"\r", b"\n").replace(b"\n", b"\r\n")
    return hashlib.sha256(normalized).hexdigest()


def _is_safe_relative_path(value: str) -> bool:
    path = PurePosixPath(value)
    return bool(value) and not path.is_absolute() and ".." not in path.parts and "\\" not in value


def _parse_timestamp(value: Any, field: str, errors: list[str]) -> datetime | None:
    if not isinstance(value, str):
        errors.append(f"{field} must be a timezone-aware ISO-8601 timestamp.")
        return None
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError:
        errors.append(f"{field} must be a timezone-aware ISO-8601 timestamp.")
        return None
    if parsed.tzinfo is None or parsed.utcoffset() is None:
        errors.append(f"{field} must include a timezone offset.")
        return None
    return parsed


def _frontmatter(text: str) -> dict[str, str]:
    normalized = text.replace("\r\n", "\n")
    if not normalized.startswith("---\n"):
        return {}
    end = normalized.find("\n---\n", 4)
    if end < 0:
        return {}
    values: dict[str, str] = {}
    for line in normalized[4:end].splitlines():
        if ":" not in line:
            continue
        key, value = line.split(":", 1)
        values[key.strip()] = value.strip().strip("'\"")
    return values


def _scan_redaction(path: Path, errors: list[str]) -> None:
    data = _bounded_read(path, errors)
    if data is None:
        return
    try:
        text = data.decode("utf-8-sig")
    except UnicodeDecodeError:
        errors.append(f"Unable to redaction-scan non-UTF-8 evidence: {path.name}")
        return
    normalized = text.replace("FC_CONTRACT_TOKEN", "ALLOWLISTED_SYNTHETIC_TOKEN")
    for pattern in (*LOCAL_PATH_PATTERNS, *SECRET_PATTERNS):
        if pattern.search(normalized):
            errors.append(f"Redaction scan failed for {path.name}: {pattern.pattern}")
    if path.suffix.lower() == ".json":
        try:
            document = json.loads(normalized, object_pairs_hook=_reject_duplicate_keys)
        except (json.JSONDecodeError, _DuplicateJsonKeyError):
            return

        def scan_value(value: Any, key: str, location: str) -> None:
            if isinstance(value, dict):
                for child_key, child in value.items():
                    scan_value(child, child_key, f"{location}.{child_key}")
            elif isinstance(value, list):
                for index, child in enumerate(value):
                    scan_value(child, key, f"{location}[{index}]")
            elif isinstance(value, str) and ENCODED_TOKEN_RE.fullmatch(value):
                is_sha256 = bool(SHA256_RE.fullmatch(value.lower()))
                if not (is_sha256 and HASH_FIELD_RE.search(key)):
                    errors.append(
                        f"Redaction scan failed for {path.name}: encoded token-like value at {location}"
                    )

        scan_value(document, "", "$.")


def _validate_manifest(evidence_root: Path, errors: list[str]) -> dict[str, str]:
    manifest_path = evidence_root / "sha256-manifest.json"
    actual_files: set[str] = set()
    total_bytes = 0
    pending = [evidence_root]
    while pending:
        directory = pending.pop()
        try:
            entries = sorted(os.scandir(directory), key=lambda item: item.name)
        except OSError:
            errors.append(f"Unable to enumerate evidence directory: {directory}")
            continue
        for entry in entries:
            path = Path(entry.path)
            relative = path.relative_to(evidence_root).as_posix()
            if entry.is_symlink():
                errors.append(f"Evidence tree contains a symlink: {relative}")
            elif entry.is_dir(follow_symlinks=False):
                pending.append(path)
            elif entry.is_file(follow_symlinks=False):
                actual_files.add(relative)
                try:
                    size = entry.stat(follow_symlinks=False).st_size
                except OSError:
                    errors.append(f"Evidence file is unreadable: {relative}")
                    continue
                total_bytes += size
                if size <= 0 or size > MAX_FILE_BYTES:
                    errors.append(f"Evidence file is empty or exceeds {MAX_FILE_BYTES} bytes: {relative}")
            else:
                errors.append(f"Evidence tree contains a non-regular entry: {relative}")

    if "sha256-manifest.json" not in actual_files:
        errors.append("Missing FrontComposer-owned SHA-256 manifest: sha256-manifest.json")
        return {}
    manifest = _read_json(manifest_path, errors, "sha256-manifest.json")
    _scan_redaction(manifest_path, errors)
    if manifest.get("schema") != "hexalith.frontcomposer.story-11-24-evidence-manifest.v1":
        errors.append("Evidence manifest has an unexpected schema.")
    if manifest.get("hashAlgorithm") != "SHA-256":
        errors.append("Evidence manifest must use SHA-256.")
    if manifest.get("capturedFromEventStoreCommit") != CAPTURE_SOURCE_SHA:
        errors.append("Evidence manifest does not bind the exact known capture-source commit.")

    entries = manifest.get("files")
    if not isinstance(entries, list):
        errors.append("Evidence manifest files must be an array.")
        return {}

    hashes: dict[str, str] = {}
    for entry in entries:
        if not isinstance(entry, dict):
            errors.append("Evidence manifest contains a non-object file entry.")
            continue
        relative = str(entry.get("path", ""))
        expected_hash = str(entry.get("sha256", ""))
        if not _is_safe_relative_path(relative):
            errors.append(f"Evidence manifest path is unsafe: {relative!r}")
            continue
        if relative in hashes:
            errors.append(f"Evidence manifest repeats path: {relative}")
            continue
        if not SHA256_RE.fullmatch(expected_hash):
            errors.append(f"Evidence manifest has an invalid SHA-256 for {relative}.")
            continue
        hashes[relative] = expected_hash
        path = evidence_root / relative
        if relative not in actual_files:
            errors.append(f"Evidence manifest file is missing: {relative}")
            continue
        actual_hash = _sha256(path, errors, relative)
        if actual_hash != expected_hash:
            errors.append(f"Evidence SHA-256 mismatch for {relative}.")
        _scan_redaction(path, errors)

    declared = frozenset(hashes)
    actual_evidence = frozenset(actual_files - {"sha256-manifest.json"})
    undeclared = sorted(actual_evidence - declared)
    absent = sorted(declared - actual_evidence)
    if undeclared:
        errors.append(f"Evidence tree contains undeclared files: {', '.join(undeclared)}")
    if absent:
        errors.append(f"Evidence manifest declares missing files: {', '.join(absent)}")
    if declared != REQUIRED_SNAPSHOT_FILES:
        missing = sorted(REQUIRED_SNAPSHOT_FILES - declared)
        unexpected = sorted(declared - REQUIRED_SNAPSHOT_FILES)
        if missing:
            errors.append(f"Evidence manifest is missing required files: {', '.join(missing)}")
        if unexpected:
            errors.append(f"Evidence manifest contains unbounded files: {', '.join(unexpected)}")
    if total_bytes > MAX_TOTAL_BYTES:
        errors.append(f"Evidence snapshot exceeds the {MAX_TOTAL_BYTES}-byte bound.")
    return hashes


def _validate_authorization(evidence_root: Path, hashes: dict[str, str], errors: list[str]) -> None:
    decision_path = evidence_root / "frontcomposer-11-24-runtime-identity-successor.md"
    decision_bytes = _bounded_read(decision_path, errors, "frontcomposer-11-24-runtime-identity-successor.md")
    decision: dict[str, str] = {}
    if decision_bytes is not None:
        try:
            decision = _frontmatter(decision_bytes.decode("utf-8-sig"))
        except UnicodeDecodeError:
            errors.append("Successor decision is not valid UTF-8.")
    expected_decision = {
        "schema": "hexalith.eventstore.frontcomposer-runtime-decision.v1",
        "recorded_at": "2026-08-12T11:32:15Z",
        "subject_sha256": SUBJECT_SHA256,
        "source_sha": SOURCE_SHA,
        "tag": f"v{VERSION}",
        "version": VERSION,
        "consumer_scope": CONSUMER_SCOPE,
        "final_decision": "available",
        "authorize_consumer_migration": "true",
    }
    for key, value in expected_decision.items():
        if decision.get(key) != value:
            errors.append(f"Successor decision {key} does not authorize the exact bound tuple.")
    decision_recorded = _parse_timestamp(decision.get("recorded_at"), "Successor decision recorded_at", errors)

    subject_path = evidence_root / SUBJECT_DIR / "review-subject.json"
    subject = _read_json(subject_path, errors, f"{SUBJECT_DIR}/review-subject.json")
    candidate = subject.get("candidate", {})
    builds = subject.get("builds_identities", {})
    if not isinstance(candidate, dict) or not isinstance(builds, dict):
        errors.append("Review subject candidate/builds identities are malformed.")
    else:
        expected_candidate = {
            "source_sha": SOURCE_SHA,
            "tag": f"v{VERSION}",
            "version": VERSION,
            "consumer_scope": CONSUMER_SCOPE,
            "package_count": 14,
        }
        for key, value in expected_candidate.items():
            if not _exact(candidate.get(key), value):
                errors.append(f"Review subject candidate {key} is not owner-approved.")
        if builds.get("catalog_exposure_sha") != BUILDS_SHA:
            errors.append("Review subject does not select the owner-approved Builds catalog.")
        if builds.get("release_execution_sha") != RELEASE_EXECUTION_SHA:
            errors.append("Review subject does not preserve the approved Builds release execution coordinate.")
    if hashes.get(f"{SUBJECT_DIR}/review-subject.json") != SUBJECT_SHA256:
        errors.append("Frozen review-subject bytes do not equal the approved subject SHA-256.")

    bound_evidence = subject.get("bound_evidence", [])
    expected_bound = {
        "nuget-sha256.txt": hashes.get(f"{SUBJECT_DIR}/nuget-sha256.txt", ""),
        "package-manifest.json": hashes.get(f"{SUBJECT_DIR}/package-manifest.json", ""),
        "restore-receipt.json": hashes.get(f"{SUBJECT_DIR}/restore-receipt.json", ""),
        "release-catalog-provenance.json": hashes.get(f"{SUBJECT_DIR}/release-catalog-provenance.json", ""),
        "reviewer-roster.json": hashes.get(f"{SUBJECT_DIR}/reviewer-roster.json", ""),
    }
    actual_bound: dict[str, str] = {}
    if not isinstance(bound_evidence, list):
        errors.append("Review subject bound_evidence must be an array.")
    else:
        for entry in bound_evidence:
            if not isinstance(entry, dict):
                errors.append("Review subject contains malformed bound evidence.")
                continue
            relative = entry.get("path")
            digest = entry.get("sha256")
            if not isinstance(relative, str) or relative in actual_bound or not SHA256_RE.fullmatch(str(digest)):
                errors.append("Review subject contains duplicate or malformed bound evidence.")
                continue
            actual_bound[relative] = str(digest)
    if actual_bound != expected_bound:
        errors.append("Review subject bound_evidence does not match the preserved manifest hashes.")

    gate = subject.get("approval_gate", {})
    expected_gate = {
        "required_roles": ["eventstore-owner", "release-owner"],
        "authorized_actor": AUTHORIZED_ACTOR,
        "separate_receipts": True,
        "receipt_directory": "acceptances/{subject_sha256}",
        "required_receipt_fields": list(REQUIRED_RECEIPT_FIELDS),
        "required_decision": "accepted",
        "receipts_must_postdate_subject": True,
    }
    if not isinstance(gate, dict):
        errors.append("Review subject approval gate is malformed.")
        gate = {}
    for key, value in expected_gate.items():
        actual = gate.get(key)
        if key in {"required_roles", "required_receipt_fields"}:
            if not isinstance(actual, list) or set(actual) != set(value) or len(actual) != len(value):
                errors.append(f"Review subject approval gate {key} is incomplete.")
        elif not _exact(actual, value):
            errors.append(f"Review subject approval gate {key} is incomplete.")

    roster_path = evidence_root / SUBJECT_DIR / "reviewer-roster.json"
    roster = _read_json(roster_path, errors, f"{SUBJECT_DIR}/reviewer-roster.json")
    roles = roster.get("roles", {})
    if not isinstance(roles, dict):
        errors.append("Reviewer roster roles are malformed.")
        roles = {}
    if roles != {
        "eventstore-owner": [AUTHORIZED_ACTOR],
        "release-owner": [AUTHORIZED_ACTOR],
    }:
        errors.append("Reviewer roster does not contain the exact separately authorized roles.")
    if roster.get("frozen_at") != SUBJECT_FROZEN_AT or roster.get("consumer_scope") != CONSUMER_SCOPE:
        errors.append("Reviewer roster does not bind the exact subject freeze and consumer scope.")

    if subject.get("frozen_at") != SUBJECT_FROZEN_AT:
        errors.append("Review subject frozen_at does not match the approved frozen time.")
    subject_frozen = _parse_timestamp(subject.get("frozen_at"), "Review subject frozen_at", errors)
    receipt_roles: set[str] = set()
    accepted_times: list[datetime] = []
    for filename, receipt_contract in EXPECTED_RECEIPTS.items():
        expected_role = receipt_contract["role"]
        receipt_path = evidence_root / RECEIPT_DIR / filename
        receipt = _read_json(receipt_path, errors, f"{RECEIPT_DIR}/{filename}")
        if set(receipt) != REQUIRED_RECEIPT_FIELDS:
            errors.append(f"{filename} does not contain the exact required receipt fields.")
        receipt_roles.add(str(receipt.get("role", "")))
        expected_receipt = {
            "schema": "hexalith.eventstore.frontcomposer-runtime-acceptance.v1",
            "subject_sha256": SUBJECT_SHA256,
            "subject_frozen_at": SUBJECT_FROZEN_AT,
            "actor": AUTHORIZED_ACTOR,
            "role": expected_role,
            "decision": "accepted",
            "source_sha": SOURCE_SHA,
            "version": VERSION,
            "consumer_scope": CONSUMER_SCOPE,
            "durable_source": receipt_contract["durable_source"],
            "statement": receipt_contract["statement"],
        }
        for key, value in expected_receipt.items():
            if receipt.get(key) != value:
                errors.append(f"{filename} {key} does not authorize the exact bound tuple.")
        roster_actors = roles.get(expected_role, [])
        if not isinstance(roster_actors, list) or AUTHORIZED_ACTOR not in roster_actors:
            errors.append(f"{filename} actor is absent from the frozen reviewer roster.")
        accepted = _parse_timestamp(receipt.get("accepted_at"), f"{filename} accepted_at", errors)
        if subject_frozen is None or accepted is None or accepted <= subject_frozen:
            errors.append(f"{filename} was not accepted after the subject freeze.")
        if accepted is not None:
            accepted_times.append(accepted)
    if receipt_roles != {"eventstore-owner", "release-owner"}:
        errors.append("Owner receipts are not separately role-bound.")
    if decision_recorded is not None and accepted_times and decision_recorded < max(accepted_times):
        errors.append("Successor decision was recorded before all owner receipts were accepted.")


def _validate_packages(evidence_root: Path, errors: list[str]) -> None:
    package_path = evidence_root / SUBJECT_DIR / "package-manifest.json"
    package_manifest = _read_json(package_path, errors, f"{SUBJECT_DIR}/package-manifest.json")
    inventory = package_manifest.get("inventory", {})
    packages = package_manifest.get("packages", [])
    expected_manifest = {
        "schema": "hexalith.eventstore.frontcomposer-runtime-packages.v1",
        "source_sha": SOURCE_SHA,
        "tag": f"v{VERSION}",
        "version": VERSION,
        "hash_algorithm": "SHA-256",
    }
    for key, value in expected_manifest.items():
        if package_manifest.get(key) != value:
            errors.append(f"Package manifest {key} does not bind the approved release.")
    _parse_timestamp(package_manifest.get("captured_at"), "Package manifest captured_at", errors)
    if package_manifest.get("source_sha") != SOURCE_SHA or package_manifest.get("version") != VERSION:
        errors.append("Package manifest does not bind the approved source/version.")
    expected_inventory = {
        "path": "tools/release-packages.json",
        "sha256": INVENTORY_SHA256,
        "package_count": 14,
        "library_count": 13,
        "tool_count": 1,
    }
    if not isinstance(inventory, dict):
        errors.append("Package manifest does not bind the approved release inventory.")
        inventory = {}
    for key, value in expected_inventory.items():
        if not _exact(inventory.get(key), value):
            errors.append(f"Package manifest inventory {key} is not the approved value.")

    signature = package_manifest.get("repository_signature", {})
    expected_signature = {
        "verification": "passed for all 14 archives via dotnet nuget verify --all",
        "type": "Repository",
        "subject": "CN=NuGet.org Repository by Microsoft, O=NuGet.org Repository by Microsoft, L=Redmond, S=Washington, C=US",
        "certificate_sha256": "1F4B311D9ACC115C8DC8018B5A49E00FCE6DA8E2855F9F014CA6F34570BC482D",
    }
    if not isinstance(signature, dict):
        errors.append("Package manifest repository signature is malformed.")
        signature = {}
    for key, value in expected_signature.items():
        if signature.get(key) != value:
            errors.append(f"Package manifest repository signature {key} is not approved.")
    if not isinstance(packages, list) or len(packages) != 14:
        errors.append("Package manifest must contain all 14 approved archives.")
        packages = []

    package_hashes: dict[str, str] = {}
    package_identities: dict[str, str] = {}
    for package in packages:
        if not isinstance(package, dict):
            errors.append("Package manifest contains a malformed archive entry.")
            continue
        package_id = str(package.get("id", ""))
        archive = str(package.get("archive", ""))
        digest = str(package.get("sha256", ""))
        expected_hash = APPROVED_PACKAGE_HASHES.get(package_id)
        expected_archive = f"{package_id}.{VERSION}.nupkg"
        expected_kind = "dotnet-tool" if package_id == "Hexalith.EventStore.Admin.Cli" else "library"
        expected_project = f"src/{package_id}/{package_id}.csproj"
        expected_url = (
            "https://api.nuget.org/v3-flatcontainer/"
            f"{package_id.lower()}/{VERSION}/{expected_archive.lower()}"
        )
        expected_entry = {
            "project": expected_project,
            "archive": expected_archive,
            "nuget_url": expected_url,
            "sha256": expected_hash,
            "embedded_repository_commit": SOURCE_SHA,
            "consumer_kind": expected_kind,
        }
        for key, value in expected_entry.items():
            if package.get(key) != value:
                errors.append(f"Package {package_id!r} {key} is not the exact approved value.")
        size = package.get("size")
        if not isinstance(size, int) or isinstance(size, bool) or size <= 0:
            errors.append(f"Package {package_id!r} size must be a positive integer.")
        if archive in package_hashes:
            errors.append(f"Package manifest repeats archive: {archive}")
        if package_id in package_identities:
            errors.append(f"Package manifest repeats package identity: {package_id}")
        package_hashes[archive] = digest
        package_identities[package_id] = digest
    if package_identities != APPROVED_PACKAGE_HASHES:
        errors.append("Package manifest does not contain the exact 14 approved package identities and hashes.")

    nuget_hashes: dict[str, str] = {}
    hash_path = evidence_root / SUBJECT_DIR / "nuget-sha256.txt"
    hash_bytes = _bounded_read(hash_path, errors, f"{SUBJECT_DIR}/nuget-sha256.txt")
    if hash_bytes is not None:
        try:
            hash_text = hash_bytes.decode("utf-8-sig")
        except UnicodeDecodeError:
            errors.append("NuGet SHA-256 inventory is not valid UTF-8.")
            hash_text = ""
        for line in hash_text.splitlines():
            parts = line.split()
            if len(parts) != 2 or not SHA256_RE.fullmatch(parts[0]):
                errors.append("NuGet SHA-256 inventory contains a malformed row.")
                continue
            if parts[1] in nuget_hashes:
                errors.append(f"NuGet SHA-256 inventory repeats archive: {parts[1]}")
            nuget_hashes[parts[1]] = parts[0]
    if package_hashes != nuget_hashes:
        errors.append("NuGet SHA-256 inventory does not match the 14 package-manifest archives.")

    restore_path = evidence_root / SUBJECT_DIR / "restore-receipt.json"
    restore = _read_json(restore_path, errors, f"{SUBJECT_DIR}/restore-receipt.json")
    consumer = restore.get("consumer_validation", {})
    retrieval = restore.get("retrieval", {})
    expected_restore = {
        "schema": "hexalith.eventstore.frontcomposer-runtime-restore.v1",
        "source_sha": SOURCE_SHA,
        "version": VERSION,
    }
    for key, value in expected_restore.items():
        if restore.get(key) != value:
            errors.append(f"Restore receipt {key} does not bind the approved release.")
    _parse_timestamp(restore.get("captured_at"), "Restore receipt captured_at", errors)
    expected_retrieval = {
        "source": "https://api.nuget.org/v3-flatcontainer/",
        "fresh_download": True,
        "archive_count": 14,
        "sha256_manifest": "nuget-sha256.txt",
        "result": "passed",
    }
    if not isinstance(retrieval, dict):
        errors.append("Restore receipt retrieval evidence is malformed.")
        retrieval = {}
    for key, value in expected_retrieval.items():
        if not _exact(retrieval.get(key), value):
            errors.append(f"Restore receipt retrieval {key} is incomplete.")
    signature_receipt = restore.get("signature_verification", {})
    if not isinstance(signature_receipt, dict):
        errors.append("Restore receipt signature verification is malformed.")
        signature_receipt = {}
    if not _exact(signature_receipt.get("verified_count"), 14) or signature_receipt.get("result") != "passed":
        errors.append("Restore receipt does not prove repository signatures for all 14 archives.")
    inventory_receipt = restore.get("inventory_validation", {})
    if not isinstance(inventory_receipt, dict):
        errors.append("Restore receipt inventory validation is malformed.")
        inventory_receipt = {}
    if (
        not _exact(inventory_receipt.get("validated_count"), 14)
        or inventory_receipt.get("result") != "passed"
        or not SHA256_RE.fullmatch(str(inventory_receipt.get("validator_sha256", "")))
    ):
        errors.append("Restore receipt does not prove exact inventory validation for all 14 archives.")
    expected_consumer = {
        "fresh_per_consumer_package_cache": True,
        "project_edges_allowed": False,
        "library_consumers_passed": 13,
        "tool_consumers_passed": 1,
        "failed": 0,
        "skipped": 0,
        "result": "passed",
    }
    if not isinstance(consumer, dict):
        errors.append("Restore receipt consumer validation is malformed.")
        consumer = {}
    for key, value in expected_consumer.items():
        if not _exact(consumer.get(key), value):
            errors.append(f"Restore receipt consumer validation {key} is incomplete.")

    provenance_path = evidence_root / SUBJECT_DIR / "release-catalog-provenance.json"
    provenance = _read_json(
        provenance_path,
        errors,
        f"{SUBJECT_DIR}/release-catalog-provenance.json",
    )
    if provenance.get("schema") != "hexalith.eventstore.frontcomposer-runtime-provenance.v1":
        errors.append("Release/catalog provenance has an unexpected schema.")
    captured_at = _parse_timestamp(provenance.get("captured_at"), "Release provenance captured_at", errors)
    provenance_candidate = provenance.get("candidate", {})
    expected_provenance_candidate = {
        "source_sha": SOURCE_SHA,
        "tag": f"v{VERSION}",
        "version": VERSION,
        "release_inventory_sha256": INVENTORY_SHA256,
        "historical_builds_gitlink_sha": "824d7ef100455423aabbcd399c8364074000b2e0",
    }
    if not isinstance(provenance_candidate, dict) or provenance_candidate != expected_provenance_candidate:
        errors.append("Release provenance candidate does not bind the exact approved tuple.")
    ci = provenance.get("exact_source_ci", {})
    release = provenance.get("exact_source_release", {})
    expected_ci = {
        "repository": "Hexalith/Hexalith.EventStore",
        "workflow": ".github/workflows/ci.yml",
        "run_id": 30984920450,
        "run_attempt": 1,
        "event": "push",
        "head_branch": "main",
        "head_sha": SOURCE_SHA,
        "conclusion": "success",
        "actor": AUTHORIZED_ACTOR,
        "url": "https://github.com/Hexalith/Hexalith.EventStore/actions/runs/30984920450",
    }
    expected_release = {
        "repository": "Hexalith/Hexalith.EventStore",
        "workflow": ".github/workflows/release.yml",
        "run_id": 30990565147,
        "run_attempt": 1,
        "event": "workflow_dispatch",
        "head_branch": "main",
        "head_sha": SOURCE_SHA,
        "conclusion": "success",
        "actor": AUTHORIZED_ACTOR,
        "url": "https://github.com/Hexalith/Hexalith.EventStore/actions/runs/30990565147",
        "release_tag": f"v{VERSION}",
        "builds_execution_sha": RELEASE_EXECUTION_SHA,
    }
    for name, record, expected in (("CI", ci, expected_ci), ("release", release, expected_release)):
        if not isinstance(record, dict):
            errors.append(f"Exact-source {name} provenance is malformed.")
            continue
        for key, value in expected.items():
            if not _exact(record.get(key), value):
                errors.append(f"Exact-source {name} provenance {key} is not the approved durable source.")
    ci_created = _parse_timestamp(ci.get("created_at") if isinstance(ci, dict) else None, "CI created_at", errors)
    ci_completed = _parse_timestamp(ci.get("completed_at") if isinstance(ci, dict) else None, "CI completed_at", errors)
    release_created = _parse_timestamp(
        release.get("created_at") if isinstance(release, dict) else None,
        "Release created_at",
        errors,
    )
    release_completed = _parse_timestamp(
        release.get("completed_at") if isinstance(release, dict) else None,
        "Release completed_at",
        errors,
    )
    published_at = _parse_timestamp(
        release.get("release_published_at") if isinstance(release, dict) else None,
        "Release published_at",
        errors,
    )
    chronology = (ci_created, ci_completed, release_created, published_at, release_completed, captured_at)
    if all(value is not None for value in chronology):
        typed_chronology = tuple(value for value in chronology if value is not None)
        if list(typed_chronology) != sorted(typed_chronology):
            errors.append("Release/catalog provenance chronology is inconsistent.")
    catalog = provenance.get("builds_catalog_exposure", {})
    expected_cataloged = set(APPROVED_PACKAGE_HASHES) - {"Hexalith.EventStore.Admin.Cli"}
    if not isinstance(catalog, dict):
        errors.append("Builds catalog exposure evidence is malformed.")
        catalog = {}
    expected_catalog = {
        "repository": "Hexalith/Hexalith.Builds",
        "commit_sha": BUILDS_SHA,
        "catalog_path": "Props/Directory.Packages.props",
        "shared_property": "HexalithEventStoreVersion",
        "exposed_version": VERSION,
        "cataloged_package_count": 13,
        "manifest_only_package": "Hexalith.EventStore.Admin.Cli",
    }
    for key, value in expected_catalog.items():
        if not _exact(catalog.get(key), value):
            errors.append(f"Builds catalog exposure {key} is not the approved value.")
    cataloged = catalog.get("cataloged_packages", [])
    if not isinstance(cataloged, list) or len(cataloged) != 13 or set(cataloged) != expected_cataloged:
        errors.append("Builds catalog exposure does not cover the exact 13 library packages.")
    if not SHA256_RE.fullmatch(str(catalog.get("catalog_sha256", ""))):
        errors.append("Builds catalog exposure lacks its catalog SHA-256.")


def _pact_interactions(
    pact_dir: Path,
    errors: list[str],
) -> tuple[list[dict[str, str]], dict[str, str], set[str]]:
    interactions_by_description: dict[str, dict[str, str]] = {}
    hashes: dict[str, str] = {}
    for filename in PACT_FILES:
        path = pact_dir / filename
        hashes[filename] = _sha256_crlf_checkout(path, errors, filename)
        pact = _read_json(path, errors, filename)
        pact_interactions = pact.get("interactions", [])
        if not isinstance(pact_interactions, list):
            errors.append(f"{filename} interactions must be an array.")
            continue
        for item in pact_interactions:
            states = item.get("providerStates", []) if isinstance(item, dict) else []
            if not isinstance(states, list) or len(states) != 1 or not isinstance(states[0], dict):
                errors.append(f"{filename} contains an interaction without one provider state.")
                continue
            interaction = {
                "description": str(item.get("description", "")),
                "providerState": str(states[0].get("name", "")),
                "pactFile": filename,
            }
            description = interaction["description"]
            if description in interactions_by_description:
                errors.append(f"Committed pacts repeat interaction description: {description}")
            interactions_by_description[description] = interaction
    for filename in ("interaction-manifest.json", "provider-state-catalog.json"):
        path = pact_dir / filename
        hashes[filename] = _sha256_crlf_checkout(path, errors, filename)
    manifest_path = pact_dir / "interaction-manifest.json"
    manifest = _read_json(manifest_path, errors, "interaction-manifest.json")
    manifest_pact_files = manifest.get("pactFiles", [])
    if (
        not isinstance(manifest_pact_files, list)
        or len(manifest_pact_files) != len(PACT_FILES)
        or set(manifest_pact_files) != set(PACT_FILES)
    ):
        errors.append("Interaction manifest pact-file attribution does not match the committed pacts.")
    manifest_entries = manifest.get("interactions", [])
    if not isinstance(manifest_entries, list):
        errors.append("Interaction manifest interactions must be an array.")
        manifest_entries = []
    ordered: list[dict[str, str]] = []
    for entry in manifest_entries:
        if not isinstance(entry, dict):
            errors.append("Interaction manifest contains a malformed entry.")
            continue
        description = str(entry.get("description", ""))
        pact_interaction = interactions_by_description.get(description)
        if pact_interaction is None:
            errors.append(f"Interaction manifest entry is absent from committed pacts: {description}")
            continue
        if pact_interaction["providerState"] != str(entry.get("providerState", "")):
            errors.append(f"Interaction manifest provider state differs from the pact: {description}")
        ordered.append(pact_interaction)
    if not _exact(manifest.get("interactionCount"), len(ordered)):
        errors.append("Interaction manifest interactionCount does not match its exact entries.")
    if set(interactions_by_description) != {entry["description"] for entry in ordered}:
        errors.append("Committed pacts contain interactions absent from the interaction manifest.")
    catalog_path = pact_dir / "provider-state-catalog.json"
    catalog = _read_json(catalog_path, errors, "provider-state-catalog.json")
    states = catalog.get("states", [])
    state_names: set[str] = set()
    if not isinstance(states, list):
        errors.append("Provider-state catalog states must be an array.")
        states = []
    for state in states:
        if not isinstance(state, dict) or not isinstance(state.get("name"), str) or not state["name"]:
            errors.append("Provider-state catalog contains a malformed state.")
            continue
        name = state["name"]
        if name in state_names:
            errors.append(f"Provider-state catalog repeats state: {name}")
        state_names.add(name)
    interaction_states = {item["providerState"] for item in interactions_by_description.values()}
    if state_names != interaction_states:
        errors.append("Provider-state catalog set must equal the committed pact interaction states.")
    return ordered, hashes, state_names


def _validate_timing(
    report: dict[str, Any],
    expected_failed: bool,
    errors: list[str],
) -> None:
    timing = report.get("timing", {})
    expected_codes = {
        "run": "run.failed" if expected_failed else "run.succeeded",
        "startup": "startup.succeeded",
        "readiness": "readiness.succeeded",
        "cleanup": "cleanup.succeeded",
    }
    if not isinstance(timing, dict):
        errors.append("Provider report timing is malformed.")
        return
    parsed_intervals: dict[str, tuple[datetime, datetime]] = {}
    for name, result_code in expected_codes.items():
        interval = timing.get(name, {})
        if not isinstance(interval, dict):
            errors.append(f"Provider report lacks bounded {name} timing.")
            continue
        duration = interval.get("durationMilliseconds")
        started = _parse_timestamp(interval.get("startedAt"), f"Provider report {name} startedAt", errors)
        completed = _parse_timestamp(interval.get("completedAt"), f"Provider report {name} completedAt", errors)
        if (
            not isinstance(duration, int)
            or isinstance(duration, bool)
            or duration < 0
            or duration > MAX_RUN_MILLISECONDS
            or started is None
            or completed is None
            or completed < started
        ):
            errors.append(f"Provider report {name} timing is incomplete or unbounded.")
        elif duration != int((completed - started).total_seconds() * 1000):
            errors.append(f"Provider report {name} duration contradicts its timestamps.")
        else:
            parsed_intervals[name] = (started, completed)
        if interval.get("resultCode") != result_code:
            errors.append(f"Provider report {name} result is not truthful for the compatibility outcome.")
    if "run" in parsed_intervals:
        run_start, run_end = parsed_intervals["run"]
        for name in ("startup", "readiness", "cleanup"):
            if name in parsed_intervals:
                interval_start, interval_end = parsed_intervals[name]
                if interval_start < run_start or interval_end > run_end:
                    errors.append("Provider report timing intervals are not internally ordered and run-bounded.")
                    break
    if set(parsed_intervals) == set(expected_codes):
        run_start, run_end = parsed_intervals["run"]
        startup_start, startup_end = parsed_intervals["startup"]
        readiness_start, readiness_end = parsed_intervals["readiness"]
        cleanup_start, cleanup_end = parsed_intervals["cleanup"]
        if not (
            run_start <= startup_start <= startup_end <= readiness_start <= readiness_end
            and readiness_end <= cleanup_start <= cleanup_end <= run_end
        ):
            errors.append("Provider report timing intervals are not internally ordered and run-bounded.")


def _validate_provider_report(
    evidence_root: Path,
    pact_dir: Path,
    snapshot_hashes: dict[str, str],
    errors: list[str],
) -> None:
    report_path = evidence_root / "provider-verification" / "provider-verification.json"
    report = _read_json(report_path, errors, "provider-verification/provider-verification.json")
    expected_interactions, contract_hashes, state_names = _pact_interactions(pact_dir, errors)

    expected_count = len(expected_interactions)
    scalar_expectations = {
        "schema": "hexalith.eventstore.provider-verification.v1",
        "requestedInteractionCount": expected_count,
        "reportedInteractionCount": expected_count,
        "requestedStateCount": len(state_names),
        "setupEventCount": expected_count,
        "teardownEventCount": expected_count,
        "complete": True,
        "hostStarted": True,
        "readyProbePassed": True,
        "hostStopped": True,
        "portClosed": True,
    }
    for key, value in scalar_expectations.items():
        if not _exact(report.get(key), value):
            errors.append(f"Provider report {key} must equal {value!r}.")
    if expected_count != 19 or len(state_names) != 19:
        errors.append("Committed pacts/provider catalog no longer contain the approved 19 interactions/states.")

    host = report.get("host", {})
    expected_host = {
        "server": "Kestrel",
        "pipeline": "production-gateway",
        "transport": "http",
        "addressFamily": "IPv4",
        "bindScope": "loopback",
        "portAllocation": "os-assigned-ephemeral",
    }
    if not isinstance(host, dict):
        errors.append("Provider report host bounds are malformed.")
    else:
        for key, value in expected_host.items():
            if host.get(key) != value:
                errors.append(f"Provider report host {key} must equal {value!r}.")

    identity = report.get("identity", {})
    expected_identity = {
        "expectedSourceSha": SOURCE_SHA,
        "expectedVersion": VERSION,
        "expectedBuildsSha": BUILDS_SHA,
        "releaseInventorySha256": INVENTORY_SHA256,
        "observedReleaseInventorySha256": INVENTORY_SHA256,
        "evidenceManifestSha256": PACKAGE_MANIFEST_SHA256,
        "decisionRecordSha256": CAPTURED_SUCCESSOR_SHA256,
        "subjectSha256": SUBJECT_SHA256,
        "approvalCount": 2,
        "approvalAuthorized": True,
    }
    if not isinstance(identity, dict):
        errors.append("Provider report identity is malformed.")
        identity = {}
    for key, value in expected_identity.items():
        if not _exact(identity.get(key), value):
            errors.append(f"Provider report identity {key} is not bound to the approved tuple.")
    if not SOURCE_SHA_RE.fullmatch(str(identity.get("observedSourceSha", ""))):
        errors.append("Provider report observed source identity is not 40-hex.")
    if not SOURCE_SHA_RE.fullmatch(str(identity.get("observedBuildsSha", ""))):
        errors.append("Provider report observed Builds identity is not 40-hex.")
    if not str(identity.get("observedVersion", "")).strip():
        errors.append("Provider report omits the observed runtime version.")

    report_interactions = report.get("interactions", [])
    if not isinstance(report_interactions, list) or len(report_interactions) != expected_count:
        errors.append("Provider report does not account for every committed interaction.")
        report_interactions = []
    actual_keys: list[dict[str, str]] = []
    contract_failed = 0
    for offset, interaction in enumerate(report_interactions, start=1):
        if not isinstance(interaction, dict):
            errors.append(f"Provider interaction {offset} is malformed.")
            continue
        actual_keys.append(
            {
                "description": str(interaction.get("description", "")),
                "providerState": str(interaction.get("providerState", "")),
                "pactFile": str(interaction.get("pactFile", "")),
            }
        )
        if not _exact(interaction.get("index"), offset):
            errors.append(f"Provider interaction {offset} has a non-deterministic index.")
        result_code = interaction.get("resultCode")
        if result_code not in {"interaction.passed", "interaction.contract-failed"}:
            errors.append(f"Provider interaction {offset} has an unsafe result code.")
        if result_code == "interaction.contract-failed":
            contract_failed += 1
        duration = interaction.get("durationMilliseconds")
        if not isinstance(duration, int) or isinstance(duration, bool) or duration < 0 or duration > MAX_RUN_MILLISECONDS:
            errors.append(f"Provider interaction {offset} duration is incomplete or unbounded.")
        events = interaction.get("stateEvents", [])
        if not isinstance(events, list) or len(events) != 2:
            errors.append(f"Provider interaction {offset} lacks setup/teardown accounting.")
            continue
        event_duration_total = 0
        for event, action, result in zip(
            events,
            ("setup", "teardown"),
            ("state.setup.succeeded", "state.teardown.succeeded"),
            strict=True,
        ):
            if (
                not isinstance(event, dict)
                or event.get("state") != interaction.get("providerState")
                or event.get("action") != action
                or event.get("resultCode") != result
                or not isinstance(event.get("durationMilliseconds"), int)
                or isinstance(event.get("durationMilliseconds"), bool)
                or event.get("durationMilliseconds") < 0
                or event.get("durationMilliseconds") > MAX_RUN_MILLISECONDS
            ):
                errors.append(f"Provider interaction {offset} has incomplete deterministic cleanup.")
            elif isinstance(event, dict):
                event_duration_total += event["durationMilliseconds"]
        if isinstance(duration, int) and not isinstance(duration, bool) and event_duration_total > duration:
            errors.append(f"Provider interaction {offset} state-event durations exceed the interaction duration.")
    if actual_keys != expected_interactions:
        errors.append("Provider report interactions do not match the committed pact inputs.")

    runtime_matches = identity.get("runtimeMatches")
    observed_pairs = (
        ("Source", identity.get("observedSourceSha"), SOURCE_SHA, "identity.source.mismatch"),
        ("Version", identity.get("observedVersion"), VERSION, "identity.version.mismatch"),
        ("Builds", identity.get("observedBuildsSha"), BUILDS_SHA, "identity.builds.mismatch"),
    )
    calculated_runtime_match = all(observed == expected for _, observed, expected, _ in observed_pairs)
    if not _exact(runtime_matches, calculated_runtime_match):
        errors.append("Provider report runtimeMatches contradicts its observed identity.")
    expected_identity_reasons = {code for _, observed, expected, code in observed_pairs if observed != expected}
    identity_reasons = identity.get("reasonCodes", [])
    if (
        not isinstance(identity_reasons, list)
        or not all(isinstance(code, str) for code in identity_reasons)
        or len(identity_reasons) != len(set(identity_reasons))
        or set(identity_reasons) != expected_identity_reasons
    ):
        errors.append("Provider report identity reasonCodes do not exactly match the observed identity.")
    expected_reasons = set(expected_identity_reasons)
    if contract_failed:
        expected_reasons.add("contract.interaction-failed")
    reason_codes = report.get("reasonCodes", [])
    if (
        not isinstance(reason_codes, list)
        or not all(isinstance(code, str) for code in reason_codes)
        or len(reason_codes) != len(set(reason_codes))
        or set(reason_codes) != expected_reasons
    ):
        errors.append("Provider report reasonCodes do not exactly match the compatibility outcome.")
    expected_failed = bool(expected_reasons)
    expected_verdict = "failed" if expected_failed else "passed"
    if report.get("finalVerdict") != expected_verdict:
        errors.append("Provider report finalVerdict contradicts the complete compatibility outcome.")
    _validate_timing(report, expected_failed, errors)

    input_hashes = report.get("inputHashes", [])
    report_inputs: dict[str, str] = {}
    report_kinds: dict[str, str] = {}
    if not isinstance(input_hashes, list):
        errors.append("Provider report inputHashes are malformed.")
        input_hashes = []
    for entry in input_hashes:
        if not isinstance(entry, dict):
            errors.append("Provider report contains a malformed input hash.")
            continue
        name = str(entry.get("name", ""))
        digest = str(entry.get("sha256", ""))
        kind = str(entry.get("kind", ""))
        if name in report_inputs:
            errors.append(f"Provider report repeats input hash: {name}")
        report_inputs[name] = digest
        report_kinds[name] = kind
    expected_identity_inputs = {
        "eventstore-owner.json": snapshot_hashes.get(f"{RECEIPT_DIR}/eventstore-owner.json", ""),
        "release-owner.json": snapshot_hashes.get(f"{RECEIPT_DIR}/release-owner.json", ""),
        # The report remains byte-preserved. Its decision input is the original EventStore-owned
        # capture; the relocated FrontComposer copy has link targets fixed and is independently
        # bound by sha256-manifest.json.
        "frontcomposer-11-24-runtime-identity-successor.md": CAPTURED_SUCCESSOR_SHA256,
        "nuget-sha256.txt": snapshot_hashes.get(f"{SUBJECT_DIR}/nuget-sha256.txt", ""),
        "package-manifest.json": snapshot_hashes.get(f"{SUBJECT_DIR}/package-manifest.json", ""),
        "release-catalog-provenance.json": snapshot_hashes.get(
            f"{SUBJECT_DIR}/release-catalog-provenance.json", ""
        ),
        "restore-receipt.json": snapshot_hashes.get(f"{SUBJECT_DIR}/restore-receipt.json", ""),
        "reviewer-roster.json": snapshot_hashes.get(f"{SUBJECT_DIR}/reviewer-roster.json", ""),
    }
    expected_input_names = set(expected_identity_inputs) | set(contract_hashes)
    if set(report_inputs) != expected_input_names:
        errors.append("Provider report input hashes do not name the bounded identity and contract inputs.")
    for name, expected_hash in expected_identity_inputs.items():
        if report_inputs.get(name) != expected_hash:
            errors.append(f"Provider report input hash does not bind preserved identity evidence: {name}")
    expected_contract_kinds = {
        **{filename: "pact" for filename in PACT_FILES},
        "interaction-manifest.json": "interaction-manifest",
        "provider-state-catalog.json": "provider-state-catalog",
    }
    for name, expected_hash in contract_hashes.items():
        if report_inputs.get(name) != expected_hash:
            errors.append(f"Provider report contract-input hash differs from current checkout-policy bytes: {name}")
        if report_kinds.get(name) != expected_contract_kinds[name]:
            errors.append(f"Provider report contract-input kind is incorrect: {name}")

    receipt_path = evidence_root / "provider-verification" / "run-evidence.json"
    receipt = _read_json(receipt_path, errors, "provider-verification/run-evidence.json")
    receipt_report = receipt.get("report", {})
    if not isinstance(receipt_report, dict):
        errors.append("Provider run receipt report binding is malformed.")
        receipt_report = {}
    expected_report_hash = snapshot_hashes.get("provider-verification/provider-verification.json")
    report_bytes = _bounded_read(report_path, errors, "provider-verification/provider-verification.json")
    expected_receipt = {
        "path": "_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/provider-verification/provider-verification.json",
        "sha256": expected_report_hash,
        "bytes": len(report_bytes) if report_bytes is not None else -1,
        "finalVerdict": expected_verdict,
        "requestedInteractionCount": expected_count,
        "reportedInteractionCount": expected_count,
        "setupEventCount": expected_count,
        "teardownEventCount": expected_count,
        "complete": True,
        "hostStopped": True,
        "portClosed": True,
    }
    for key, value in expected_receipt.items():
        if not _exact(receipt_report.get(key), value):
            errors.append(f"Provider run receipt {key} does not bind the complete report.")
    expected_receipt_root = {
        "schema": "hexalith.eventstore.provider-verification-run-evidence.v1",
        "command": "dotnet run --project tests/Hexalith.EventStore.ProviderVerification/Hexalith.EventStore.ProviderVerification.csproj --configuration Release --no-build -- <validated canonical inputs>",
        "exitCode": 4 if expected_failed else 0,
        "expectedNonzero": expected_failed,
        "nativeVerifierOutputRetained": False,
        "normalizedPactCopiesRetained": False,
        "externalInputsModified": False,
    }
    for key, value in expected_receipt_root.items():
        if not _exact(receipt.get(key), value):
            errors.append(f"Provider run receipt {key} is not truthful for the compatibility outcome.")


def _validate_apphost_smoke(evidence_root: Path, repository_root: Path, errors: list[str]) -> None:
    smoke_path = evidence_root / "apphost-smoke" / "apphost-smoke.json"
    smoke = _read_json(smoke_path, errors, "apphost-smoke/apphost-smoke.json")
    if smoke.get("schema") != "hexalith.frontcomposer.story-11-24-apphost-smoke.v1":
        errors.append("AppHost smoke evidence has an unexpected schema.")
    _parse_timestamp(smoke.get("capturedAt"), "AppHost smoke capturedAt", errors)
    identity = smoke.get("identity", {})
    expected_identity = {
        "eventStoreSourceSha": SOURCE_SHA,
        "eventStorePackageVersion": VERSION,
        "buildsCatalogSha": BUILDS_SHA,
    }
    if not isinstance(identity, dict):
        errors.append("AppHost smoke identity is malformed.")
        identity = {}
    for key, value in expected_identity.items():
        if identity.get(key) != value:
            errors.append(f"AppHost smoke identity {key} is not the approved tuple.")
    topology = smoke.get("topology", {})
    expected_topology_paths = {
        "programPath": "src/Hexalith.FrontComposer.AppHost/Program.cs",
        "projectPath": "src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj",
        "modifiedForSmoke": False,
    }
    if not isinstance(topology, dict):
        errors.append("AppHost smoke does not prove the existing topology was preserved.")
        topology = {}
    else:
        for key, value in expected_topology_paths.items():
            if not _exact(topology.get(key), value):
                errors.append(f"AppHost smoke topology {key} is not the current topology.")
    expected_topology_hashes = {
        "programSha256": _sha256(repository_root / expected_topology_paths["programPath"], errors, "AppHost Program.cs"),
        "projectSha256": _sha256(repository_root / expected_topology_paths["projectPath"], errors, "AppHost project"),
    }
    for key, value in expected_topology_hashes.items():
        if topology.get(key) != value:
            errors.append(f"AppHost smoke topology {key} does not match the current file bytes.")

    startup = smoke.get("startup", {})
    expected_startup = {
        "restoreCommand": "dotnet restore src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj -p:Configuration=Debug -p:UseHexalithProjectReferences=true",
        "buildCommand": "dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -p:IsPackable=false -m:1",
        "command": "aspire run --no-build --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive",
        "result": "passed",
    }
    if not isinstance(startup, dict):
        errors.append("AppHost smoke startup evidence is malformed.")
        startup = {}
    for key, value in expected_startup.items():
        if startup.get(key) != value:
            errors.append(f"AppHost smoke startup {key} is not the recorded existing-topology attempt.")
    resource_waits = startup.get("resourceWaits", {})
    expected_resources = {
        "security": "healthy",
        "eventstore": "healthy",
        "tenants": "healthy",
        "parties": "healthy",
        "frontcomposer-ui": "healthy",
    }
    if resource_waits != expected_resources:
        errors.append("AppHost smoke resource waits do not exactly account for the existing topology.")
    observations = smoke.get("observations", {})
    if not isinstance(observations, dict) or set(observations) != set(APPHOST_OBSERVATIONS):
        errors.append("AppHost smoke does not account for every required runtime outcome.")
    else:
        for name in APPHOST_OBSERVATIONS:
            observation = observations.get(name, {})
            if (
                not isinstance(observation, dict)
                or observation.get("result") not in {"passed", "failed", "not-observed"}
                or not str(observation.get("reasonCode", "")).strip()
            ):
                errors.append(f"AppHost smoke observation is incomplete: {name}")
                continue
            result = observation["result"]
            if name == "health":
                readiness = observation.get("readinessStatusCode")
                if result == "passed" and readiness != 200:
                    errors.append("AppHost health observation passes without a successful readiness response.")
                if result == "failed" and (not isinstance(readiness, int) or isinstance(readiness, bool) or readiness < 400):
                    errors.append("AppHost health failure lacks a failing readiness response.")
            else:
                status = observation.get("statusCode")
                if result == "passed" and (not isinstance(status, int) or isinstance(status, bool) or status >= 400):
                    errors.append(f"AppHost {name} passes without a successful HTTP response.")
                if result == "failed" and (not isinstance(status, int) or isinstance(status, bool) or status < 400):
                    errors.append(f"AppHost {name} failure lacks a failing HTTP response.")
    cleanup = smoke.get("cleanup", {})
    if (
        not isinstance(cleanup, dict)
        or not _exact(cleanup.get("runningAppHostsAfterAttempt"), 0)
        or cleanup.get("result") != "clean"
        or cleanup.get("command") != "aspire stop --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --nologo"
    ):
        errors.append("AppHost smoke cleanup is incomplete.")
    authorization = smoke.get("authorization", {})
    if (
        not isinstance(authorization, dict)
        or authorization.get("compatibilityEvidenceIsMigrationAuthority") is not False
        or authorization.get("identityAdoptionRevoked") is not False
    ):
        errors.append("AppHost smoke compatibility results incorrectly govern identity adoption.")


def _validate_release_restore(evidence_root: Path, errors: list[str]) -> None:
    restore_path = evidence_root / "release-restore" / "release-restore.json"
    restore = _read_json(restore_path, errors, "release-restore/release-restore.json")
    if restore.get("schema") != "hexalith.frontcomposer.story-11-24-release-restore.v1":
        errors.append("Release restore evidence has an unexpected schema.")
    _parse_timestamp(restore.get("capturedAt"), "Release restore capturedAt", errors)
    expected_project = "src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj"
    if restore.get("project") != expected_project or restore.get("configuration") != "Release":
        errors.append("Release restore does not target the exact approved AppHost Release project.")
    identity = restore.get("identity", {})
    expected_identity = {
        "eventStoreSourceSha": SOURCE_SHA,
        "eventStorePackageVersion": VERSION,
        "buildsCatalogSha": BUILDS_SHA,
    }
    if not isinstance(identity, dict):
        errors.append("Release restore identity is malformed.")
        identity = {}
    for key, value in expected_identity.items():
        if identity.get(key) != value:
            errors.append(f"Release restore identity {key} is not the approved tuple.")
    executed = restore.get("executedCommand", {})
    expected_executed = {
        "command": f"dotnet restore {expected_project} -p:Configuration=Release --packages <isolated-cache>",
        "result": "passed",
    }
    if not isinstance(executed, dict) or any(executed.get(key) != value for key, value in expected_executed.items()):
        errors.append("Isolated Release restore did not pass.")
    requested = restore.get("requestedCommand", {})
    expected_requested = {
        "command": f"dotnet restore {expected_project} --configuration Release --packages <isolated-cache>",
        "result": "rejected",
        "reasonCode": "dotnet-restore.unknown-configuration-switch",
    }
    if not isinstance(requested, dict) or any(requested.get(key) != value for key, value in expected_requested.items()):
        errors.append("Release restore does not truthfully retain the rejected invalid-switch attempt.")
    if (
        restore.get("packageCache") != "isolated-temporary-directory"
        or not _exact(restore.get("eventStoreProjectEdgeCount"), 0)
        or not _exact(restore.get("everyRestoredEventStoreArchiveMatchedApprovedInventory"), True)
        or restore.get("result") != "passed"
    ):
        errors.append("Release restore does not prove isolated package-only EventStore assets.")

    package_manifest_path = evidence_root / SUBJECT_DIR / "package-manifest.json"
    package_manifest = _read_json(package_manifest_path, errors, f"{SUBJECT_DIR}/package-manifest.json")
    approved_archives = {
        str(item.get("archive", "")): str(item.get("sha256", ""))
        for item in package_manifest.get("packages", [])
        if isinstance(item, dict)
    }
    assets = restore.get("eventStoreAssets", [])
    if not isinstance(assets, list) or not assets:
        errors.append("Release restore evidence contains no EventStore package assets.")
        assets = []
    restored_assets: dict[str, str] = {}
    for asset in assets:
        if not isinstance(asset, dict):
            errors.append("Release restore evidence contains a malformed EventStore asset.")
            continue
        archive = str(asset.get("archive", ""))
        name = str(asset.get("name", ""))
        if (
            asset.get("version") != VERSION
            or asset.get("type") != "package"
            or asset.get("sha256") != approved_archives.get(archive)
            or archive != f"{name}.{VERSION}.nupkg"
        ):
            errors.append(f"Release restore asset is not in the approved package inventory: {archive!r}")
        if name in restored_assets:
            errors.append(f"Release restore repeats EventStore asset: {name}")
        restored_assets[name] = archive
    if restored_assets != {"Hexalith.EventStore.Aspire": f"Hexalith.EventStore.Aspire.{VERSION}.nupkg"}:
        errors.append("Release restore asset inventory does not exactly match the AppHost package graph.")


def validate(evidence_root: Path, pact_dir: Path) -> list[str]:
    """Return deterministic validation errors for one evidence snapshot."""
    errors: list[str] = []
    if _path_has_symlink_component(evidence_root) or not evidence_root.is_dir():
        return [f"Evidence root is missing or is a symlink: {evidence_root}"]
    if _path_has_symlink_component(pact_dir) or not pact_dir.is_dir():
        return [f"Pact directory is missing or is a symlink: {pact_dir}"]
    snapshot_hashes = _validate_manifest(evidence_root, errors)
    _validate_authorization(evidence_root, snapshot_hashes, errors)
    _validate_packages(evidence_root, errors)
    _validate_provider_report(evidence_root, pact_dir, snapshot_hashes, errors)
    _validate_apphost_smoke(evidence_root, REPOSITORY_ROOT, errors)
    _validate_release_restore(evidence_root, errors)
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--evidence-root", required=True, type=Path)
    parser.add_argument("--pact-dir", required=True, type=Path)
    args = parser.parse_args(argv)
    errors = validate(args.evidence_root.absolute(), args.pact_dir.absolute())
    if errors:
        for error in errors:
            print(f"EventStore runtime evidence error: {error}", file=sys.stderr)
        return 1
    print("EventStore runtime evidence validated successfully.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
