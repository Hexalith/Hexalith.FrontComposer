#!/usr/bin/env python3
"""Validate historical and active FrontComposer EventStore runtime evidence."""
from __future__ import annotations


import argparse
import hashlib
import ipaddress
import json
import os
import re
import subprocess
import sys
import tempfile
from datetime import datetime, timedelta, timezone
from pathlib import Path, PurePosixPath
from typing import Any, Iterable
from urllib import parse


SOURCE_SHA = "bb94d93e9b84132cff83a38fba84f25455820d31"
BUILDS_SHA = "a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a"
CAPTURE_SOURCE_SHA = "29de2507767dc061b923e4e6e40fbe1ea69f932e"
RELEASE_EXECUTION_SHA = "f75daebd4c522c081a6f62e274cf25e07971de69"
VERSION = "3.91.1"
SUBJECT_SHA256 = "9d074dfd0758a8934f122aab18659627dff1cf5d4c3e548b222cc0d79a881065"
INVENTORY_SHA256 = "6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae"
CAPTURED_SUCCESSOR_SHA256 = "69b08aba7758de770888aea53b9b51dc7e479220c9d539ed670db479fdb0164a"
OWNER_ACTIONS_SHA256 = "c03ab1d8f7fc4e167f0536f8f9b77cd01980bbeee1b9c44704b4d5d99aefbcb6"
PACKAGE_MANIFEST_SHA256 = "b85b9926482b42fda508b68e26162f256892d2f49c2eab31adbae49cefdd0d12"
SUBJECT_FROZEN_AT = "2026-08-10T07:06:11Z"
CONSUMER_SCOPE = "Hexalith.FrontComposer Story 11.24"
AUTHORIZED_ACTOR = "github:jpiquot"
IDENTITY_V1_SHA256 = "80c93e4e865e4cac7532e8c96481aa9205b6e6918d2177352222715e42ff2157"
ACTIVE_SOURCE_SHA = "059f6a8917bfab26b85775be464840a1610dfdeb"
ACTIVE_BUILDS_SHA = "a32cb422749352cce8dec948aa3e78c8f00eb4cf"
ACTIVE_VERSION = "3.103.0"
PRIOR_BUILDS_SHA = "35c3d1e5b8a55a74a440b9c2cad4c5e18747b241"
PRIOR_CAPTURE_SHA256 = {
    "apphost-smoke.json": "98fe33eebe8e69d549be0b188c9f76240284d6f1672a23a9b2891d8caf3fc707",
    "provider-verification.json": "36fa68b27b317a8b1c55ba968a4064616582a6d64d2f40ab3c14845ab7d9a1f9",
    "run-evidence.json": "3a3d0cf9967fd7e1c3fe38e94bc95d3aa596d72053030370b23288f84729ad5a",
}
DEFAULT_REQUIRED_ROLES = (
    "eventstore-maintainer",
    "frontcomposer-maintainer",
    "release-owner",
)
ACTIVE_REQUIRED_ROLES = DEFAULT_REQUIRED_ROLES
OI18_REPLACEMENT_ROLE = "accountable-frontcomposer-maintainer"
OI18_PREREQUISITE_ROLES = ("product-owner", "architect")
POLICY_ROLES = (*DEFAULT_REQUIRED_ROLES, OI18_REPLACEMENT_ROLE, *OI18_PREREQUISITE_ROLES)
ACTIVE_EVIDENCE_ROOT = "_bmad-output/implementation-artifacts/evidence/eventstore-runtime-identity-v2"
ACTIVE_IDENTITY_PATH = (
    "_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v2.json"
)
ACTIVE_RECAPTURE_ROOT = f"{ACTIVE_EVIDENCE_ROOT}/recapture"
RUNTIME_INPUT_MANIFEST_PATH = f"{ACTIVE_EVIDENCE_ROOT}/frontcomposer-runtime-inputs.json"
APPROVAL_POLICY_PATH = f"{ACTIVE_EVIDENCE_ROOT}/approval-policy.json"
APPROVAL_ROSTER_PATH = f"{ACTIVE_EVIDENCE_ROOT}/reviewer-roster.json"
APPROVAL_SUBJECT_PATH = f"{ACTIVE_EVIDENCE_ROOT}/approval-subject.json"
APPROVAL_RECEIPT_ROOT = f"{ACTIVE_EVIDENCE_ROOT}/receipts"
PRIOR_EVIDENCE_ROOT = (
    "_bmad-output/implementation-artifacts/evidence/"
    "pact-provider-reconciliation-history/2026-09-08-builds-35c3d1e5"
)
RUNTIME_SCOPE_VERSION = "frontcomposer-eventstore-runtime-inputs.v1"
RUNTIME_ROOT_INPUTS = (
    "Directory.Build.props",
    "Directory.Build.rsp",
    "Directory.Build.targets",
    "Directory.Packages.props",
    "aspire.config.json",
    "deps.local.props",
    "deps.nuget.props",
    "global.json",
    "nuget.config",
)
OPTIONAL_ABSENT_RUNTIME_ROOT_INPUTS = frozenset({"Directory.Build.rsp"})
ROOT_BUILD_CONTROL_RE = re.compile(
    r"^(?:Directory\.(?:Build|Packages)\..+|global\.json|nuget\.config|"
    r"aspire\.config\.json|deps\.[^.]+\.props|[^/]+\.rsp)$",
    re.IGNORECASE,
)
RUNTIME_DEPENDENCY_GITLINKS = (
    "references/Hexalith.Builds",
    "references/Hexalith.EventStore",
    "references/Hexalith.Tenants",
    "references/Hexalith.Parties",
    "references/Hexalith.Memories",
    "references/Hexalith.Commons",
    "references/Hexalith.PolymorphicSerializations",
)
APPHOST_BUILD_CONTROL_GITLINKS = (
    "references/Hexalith.Builds",
)
APPHOST_REACHABLE_SOURCE_GITLINKS = (
    "references/Hexalith.EventStore",
    "references/Hexalith.Tenants",
    "references/Hexalith.Parties",
    "references/Hexalith.Memories",
    "references/Hexalith.Commons",
)
APPHOST_INACTIVE_GUARDED_GITLINKS = (
    "references/Hexalith.PolymorphicSerializations",
)
INERT_DEPENDENCY_SYMLINK_OBJECTS = {
    (
        "references/Hexalith.EventStore",
        "_bmad-output/planning-artifacts/architecture/"
        "architecture-eventstore-2026-07-05/ARCHITECTURE-SPINE.md",
    ): "7c48eaff1af296179ebc3f766958c54d8134a9f2",
    (
        "references/Hexalith.Commons",
        ".clinerules",
    ): "a245fc1c6c2d522ea2aa0ecabf1583917087b51c",
    (
        "references/Hexalith.Commons",
        ".cursorrules",
    ): "86da7cdc6d0e98a2fdd8711088cb375badd093ec",
}
APPHOST_BUILD_PROPERTIES = {
    "UseHexalithProjectReferences": True,
    "UseNuGetDeps": False,
    "HexalithEventStoreFromSource": True,
    "HexalithTenantsFromSource": True,
    "HexalithPartiesFromSource": True,
    "HexalithMemoriesFromSource": True,
    "HexalithCommonsFromSource": True,
    "HexalithPolymorphicSerializationsFromSource": True,
    "HexalithFrontComposerFromSource": True,
    "NuGetAudit": False,
    "CentralPackageTransitivePinningEnabled": False,
}
# Human approval authority is code-owned on purpose. Repository evidence may bind this
# immutable bootstrap, but it may not grant itself authority by editing policy JSON.
APPROVAL_AUTHORITY_BOOTSTRAP: dict[str, dict[str, frozenset[str]]] = {
    role: {} for role in POLICY_ROLES
}
ACTIVE_APPROVAL_STATEMENT = "I approve this exact EventStore runtime migration subject."
OI18_APPROVAL_STATEMENT = "I approve this exact OI-18 ownership transfer subject."
MAX_CLOCK_SKEW = timedelta(minutes=5)
ACTOR_RE = re.compile(
    r"^(?:github:[A-Za-z0-9](?:[A-Za-z0-9-]{0,38})|email:[^@\s]+@[^@\s]+\.[^@\s]+)$"
)
MAX_FILE_BYTES = 1_048_576
MAX_TOTAL_BYTES = 2_097_152
MAX_RUN_MILLISECONDS = 300_000
PACT_FILES = (
    "frontcomposer-eventstore-command-dispatch.json",
    "frontcomposer-eventstore-query-execution.json",
    "frontcomposer-eventstore-cache-validation.json",
    "frontcomposer-eventstore-auth-tenant-propagation.json",
)
RUNTIME_PACT_INPUTS = (
    *(f"tests/Hexalith.FrontComposer.Shell.Tests/Pact/{name}" for name in PACT_FILES),
    "tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json",
    "tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json",
)
CANONICAL_PACT_ROOT = "tests/Hexalith.FrontComposer.Shell.Tests/Pact"
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
# Every file the EventStore owner captured, pinned by exact digest. The approved historical
# commit no longer carries these bytes, so the manifest alone cannot be the authority: a
# rewritten report plus a re-sealed manifest would otherwise validate and could relabel the
# preserved failed verdict as passing.
CAPTURED_EVIDENCE_SHA256 = {
    "frontcomposer-11-24-runtime-identity-successor.md": CAPTURED_SUCCESSOR_SHA256,
    f"{SUBJECT_DIR}/nuget-sha256.txt": "08449d50bf9d57c791c87e9768241442a92ec1db69b0f80fc2886529e70abfb0",
    f"{SUBJECT_DIR}/owner-actions.md": OWNER_ACTIONS_SHA256,
    f"{SUBJECT_DIR}/package-manifest.json": PACKAGE_MANIFEST_SHA256,
    f"{SUBJECT_DIR}/release-catalog-provenance.json": "a5821b1002daaf1284387486d14776648f490849717d25b5f3d1cbbe3ca40cef",
    f"{SUBJECT_DIR}/restore-receipt.json": "362761dd0f82c5bb2442f4a8514ee3d15eea2ca155f67fe600fe55dcf6cdb03d",
    f"{SUBJECT_DIR}/review-subject.json": SUBJECT_SHA256,
    f"{SUBJECT_DIR}/reviewer-roster.json": "a1e55d095f7919dc94a5722e356751ad35b5e86cb4e20da5db7545a6659fa346",
    f"{RECEIPT_DIR}/eventstore-owner.json": "a20686e60d21e1448c4dee5e1b9a2a21a7be5f92b76e30e673a0e2ba848b354d",
    f"{RECEIPT_DIR}/release-owner.json": "1435d4a4ea160c125fa26cf7b2bca8da3630ddddc7af02c36121bcc65b6e3eee",
    "provider-verification/provider-verification.json": "7ad9d7199272680a26770f5ea980bce880736a4bd92b3d6b27fb7aed546304c7",
    "provider-verification/run-evidence.json": "3338dc3060875789603ee83fc4b8fc930133f8cb2f07e9cf694f7e7447b16dfb",
}
# Produced by this repository after the capture commit, so they carry FrontComposer provenance.
FRONTCOMPOSER_EVIDENCE_FILES = frozenset(
    {
        "apphost-smoke/apphost-smoke.json",
        "release-restore/release-restore.json",
    }
)
FRONTCOMPOSER_CAPTURED_EVIDENCE_SHA256 = {
    "apphost-smoke/apphost-smoke.json": "2474f1ec7663a34cae597a06c4bcceffc0bb1493caf975213c470869603295bc",
    "release-restore/release-restore.json": "dbdd01f248fc00c3fdd7049643c2d4a1f51659880e441c20eb3be5ecb8131619",
}
APPHOST_CAPTURED_TOPOLOGY_SHA256 = {
    "programSha256": "474e7aabc58fd0a44b15e2598ee832a7286432779360614180931ef0024f7290",
    "projectSha256": "ed58cceb34df2572426512053b836abdb1e580dd52140c1dec75d5267e34e4f8",
}
EVIDENCE_PROVENANCE = {
    **{path: "eventstore-capture" for path in CAPTURED_EVIDENCE_SHA256},
    **{path: "frontcomposer-run" for path in FRONTCOMPOSER_EVIDENCE_FILES},
}
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")
SOURCE_SHA_RE = re.compile(r"^[0-9a-f]{40}$")
ACTIVE_AGGREGATE_ID_RE = re.compile(
    r"^pact-reconciliation-[0-7][0-9a-hjkmnp-tv-z]{25}$"
)
LOCAL_PATH_PATTERNS = (
    re.compile(r"[A-Za-z]:\\Users\\", re.IGNORECASE),
    re.compile(r"/(?:home|Users)/[^/\s]+/"),
)
# Kept at parity with Find-RedactionLeaks in eng/validate-contract-artifacts.ps1. That
# scanner only ever sees the pacts and the provider report, so without these rules the
# other preserved evidence files would be held to a weaker standard than their siblings.
SECRET_PATTERNS = (
    re.compile(r'"?access[_-]?token"?\s*[=:]', re.IGNORECASE),
    re.compile(r'"?api[_-]?key"?\s*[=:]', re.IGNORECASE),
    re.compile(r'"?password"?\s*[=:]', re.IGNORECASE),
    re.compile(r"set-cookie\s*:", re.IGNORECASE),
    re.compile(r'"?cookie"?\s*:', re.IGNORECASE),
    re.compile(r"\bcookie\s*=", re.IGNORECASE),
    re.compile(r"connectionstring", re.IGNORECASE),
    re.compile(r"authorization_payload", re.IGNORECASE),
    re.compile(r"Bearer\s+[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+"),
    re.compile(r"[A-Z0-9_]{8,}=.{6,}"),
)
RAW_AUTHORIZATION_RE = re.compile(r"\bauthorization\s*:", re.IGNORECASE)
HASH_FIELD_RE = re.compile(r"(?:sha|hash|checksum|digest|fingerprint)", re.IGNORECASE)
ENCODED_TOKEN_RE = re.compile(r"^[A-Za-z0-9+/]{64,}={0,2}$")
APPHOST_OBSERVATIONS = (
    "health",
    "commandSubmit",
    "commandStatus",
    "queryProvenance",
    "projectionSignalR",
)
APPHOST_QUERY_PROVENANCE = {"HandlerComputed": "query.handler-computed"}
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
    if type(actual) is not type(expected):
        return False
    if isinstance(expected, dict):
        return set(actual) == set(expected) and all(
            _exact(actual[key], expected[key]) for key in expected
        )
    if isinstance(expected, list):
        return len(actual) == len(expected) and all(
            _exact(actual_item, expected_item)
            for actual_item, expected_item in zip(actual, expected, strict=True)
        )
    return actual == expected


def _is_safe_loopback_evidence_url(value: str) -> bool:
    try:
        parsed = parse.urlsplit(value)
        _ = parsed.port
        host = parsed.hostname
        if parsed.scheme not in {"http", "https"} or not host or parsed.username or parsed.password:
            return False
        return host.casefold() == "localhost" or ipaddress.ip_address(host).is_loopback
    except ValueError:
        return False


def _is_projection_changes_hub_url(value: Any) -> bool:
    if not isinstance(value, str) or not _is_safe_loopback_evidence_url(value):
        return False
    try:
        parsed = parse.urlsplit(value)
    except ValueError:
        return False
    return (
        parsed.path == "/hubs/projection-changes"
        and not parsed.query
        and not parsed.fragment
    )


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


def _same_canonical_path(left: Path, right: Path) -> bool:
    """Compare authority paths without letting symlink loops escape validation."""
    try:
        return left.resolve(strict=False) == right.resolve(strict=False)
    except (OSError, RuntimeError):
        return False


def _canonical_active_locations(
    repository_root: Path,
) -> tuple[Path, Path, Path, Path]:
    """Return validator-owned authority coordinates beneath the repository root."""
    return (
        repository_root / ACTIVE_IDENTITY_PATH,
        repository_root / ACTIVE_EVIDENCE_ROOT,
        repository_root / PRIOR_EVIDENCE_ROOT,
        repository_root / CANONICAL_PACT_ROOT,
    )


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
    if RAW_AUTHORIZATION_RE.search(normalized) and "Bearer FC_CONTRACT_TOKEN" not in text:
        errors.append(f"Redaction scan failed for {path.name}: raw Authorization header")
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
        # capturedFromEventStoreCommit names one commit, but two files were produced here
        # afterwards. Per-file provenance keeps the manifest truthful about which is which.
        if entry.get("provenance") != EVIDENCE_PROVENANCE.get(relative):
            errors.append(f"Evidence manifest provenance is not truthful for {relative}.")
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
    for relative, pinned_hash in sorted(CAPTURED_EVIDENCE_SHA256.items()):
        if hashes.get(relative) != pinned_hash:
            errors.append(
                f"Preserved evidence is not byte-identical to the EventStore-owned capture: {relative}"
            )
    for relative, pinned_hash in sorted(FRONTCOMPOSER_CAPTURED_EVIDENCE_SHA256.items()):
        if hashes.get(relative) != pinned_hash:
            errors.append(
                f"Preserved FrontComposer run evidence is not byte-identical to its historical capture: {relative}"
            )
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
    duration_tolerance_microseconds: int = 1000,
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
        elif abs(duration * 1000 - (completed - started) // timedelta(microseconds=1)) > duration_tolerance_microseconds:
            # Historical Story 11.24 reports stay at one millisecond. The live EventStore
            # verifier at the pinned source still uses Stopwatch.ElapsedMilliseconds against
            # DateTimeOffset timestamps, which can differ by a few milliseconds on WSL/CI.
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
    snapshot_hashes: dict[str, str],
    errors: list[str],
) -> None:
    report_path = evidence_root / "provider-verification" / "provider-verification.json"
    report = _read_json(report_path, errors, "provider-verification/provider-verification.json")
    expected_count = 19
    report_interaction_values = report.get("interactions", [])
    state_names = {
        str(item.get("providerState", ""))
        for item in report_interaction_values
        if isinstance(item, dict)
    }
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
        # The preserved copy is byte-identical to the EventStore-owned capture, so the report's
        # decision input, sha256-manifest.json, and CAPTURED_SUCCESSOR_SHA256 are one hash.
        "frontcomposer-11-24-runtime-identity-successor.md": snapshot_hashes.get(
            "frontcomposer-11-24-runtime-identity-successor.md", ""
        ),
        "nuget-sha256.txt": snapshot_hashes.get(f"{SUBJECT_DIR}/nuget-sha256.txt", ""),
        "package-manifest.json": snapshot_hashes.get(f"{SUBJECT_DIR}/package-manifest.json", ""),
        "release-catalog-provenance.json": snapshot_hashes.get(
            f"{SUBJECT_DIR}/release-catalog-provenance.json", ""
        ),
        "restore-receipt.json": snapshot_hashes.get(f"{SUBJECT_DIR}/restore-receipt.json", ""),
        "reviewer-roster.json": snapshot_hashes.get(f"{SUBJECT_DIR}/reviewer-roster.json", ""),
    }
    contract_input_names = set(PACT_FILES) | {"interaction-manifest.json", "provider-state-catalog.json"}
    expected_input_names = set(expected_identity_inputs) | contract_input_names
    if set(report_inputs) != expected_input_names:
        errors.append("Provider report input hashes do not name the bounded identity and contract inputs.")
    for name, expected_hash in expected_identity_inputs.items():
        if report_inputs.get(name) != expected_hash:
            errors.append(f"Provider report input hash does not bind preserved identity evidence: {name}")
    expected_input_kinds = {
        **{filename: "pact" for filename in PACT_FILES},
        "interaction-manifest.json": "interaction-manifest",
        "provider-state-catalog.json": "provider-state-catalog",
        # Identity inputs are kind-checked too, so approval evidence cannot be relabeled.
        "eventstore-owner.json": "identity-approval",
        "release-owner.json": "identity-approval",
        "frontcomposer-11-24-runtime-identity-successor.md": "identity-decision",
        "nuget-sha256.txt": "identity-evidence",
        "package-manifest.json": "identity-evidence",
        "release-catalog-provenance.json": "identity-evidence",
        "restore-receipt.json": "identity-evidence",
        "reviewer-roster.json": "identity-evidence",
    }
    for name, expected_kind in expected_input_kinds.items():
        if report_kinds.get(name) != expected_kind:
            errors.append(f"Provider report input kind is incorrect: {name}")

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


def _validate_apphost_smoke(evidence_root: Path, errors: list[str]) -> None:
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
    for key, value in APPHOST_CAPTURED_TOPOLOGY_SHA256.items():
        if topology.get(key) != value:
            errors.append(f"AppHost smoke topology {key} does not match the sealed historical capture.")

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
            status_key = "readinessStatusCode" if name == "health" else "statusCode"
            # An outcome the run never reached must not carry a response it did reach:
            # otherwise a real observation can be relabelled away as unobserved.
            if result == "not-observed" and observation.get(status_key) is not None:
                errors.append(f"AppHost {name} is recorded as unobserved but carries a response code.")
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


def _git(repository: Path, *arguments: str) -> str:
    try:
        completed = subprocess.run(
            ["git", *arguments],
            cwd=repository,
            check=True,
            capture_output=True,
            text=True,
            timeout=10,
        )
    except (OSError, subprocess.SubprocessError):
        return ""
    return completed.stdout.strip().lower()


def _git_completed(repository: Path, *arguments: str) -> subprocess.CompletedProcess[bytes] | None:
    try:
        return subprocess.run(
            ["git", *arguments],
            cwd=repository,
            check=False,
            capture_output=True,
            timeout=15,
        )
    except (OSError, subprocess.SubprocessError):
        return None


def _runtime_scope() -> dict[str, Any]:
    return {
        "version": RUNTIME_SCOPE_VERSION,
        "trackedTrees": ["src/**", "samples/Counter/**"],
        "rootInputs": list(RUNTIME_ROOT_INPUTS),
        "pactInputs": list(RUNTIME_PACT_INPUTS),
        "dependencyGitlinks": list(RUNTIME_DEPENDENCY_GITLINKS),
    }


def _runtime_tree_sha256(entries: list[dict[str, Any]]) -> str:
    encoded = json.dumps(entries, sort_keys=True, separators=(",", ":")).encode("utf-8")
    return hashlib.sha256(encoded).hexdigest()


def _runtime_index_flag_paths(
    repository_root: Path,
    flag: str,
    pathspecs: list[str],
) -> tuple[dict[str, str], str | None]:
    listed = _git_completed(repository_root, "ls-files", flag, "-z", "--", *pathspecs)
    if listed is None or listed.returncode != 0:
        return {}, "Unable to inspect Git index acceleration flags in the runtime-input scope."
    tagged: dict[str, str] = {}
    for raw in listed.stdout.split(b"\0"):
        if not raw:
            continue
        try:
            tag, encoded_path = raw[:1].decode("ascii"), raw[2:]
            relative = encoded_path.decode("utf-8")
        except UnicodeDecodeError:
            return {}, "The Git index contains an unreadable runtime-input flag entry."
        tagged[relative] = tag
    return tagged, None


def _worktree_git_objects(repository_root: Path, relative: str, data: bytes) -> frozenset[str]:
    """Return raw and checkout-filtered object identities for worktree bytes."""
    objects: set[str] = set()
    for arguments in (
        ["git", "hash-object", "--stdin"],
        ["git", "hash-object", f"--path={relative}", "--stdin"],
    ):
        try:
            completed = subprocess.run(
                arguments,
                cwd=repository_root,
                input=data,
                check=False,
                capture_output=True,
                timeout=15,
            )
        except (OSError, subprocess.SubprocessError):
            continue
        if completed.returncode == 0:
            objects.add(completed.stdout.decode("ascii", errors="ignore").strip().lower())
    return frozenset(objects)


def _project_output_roots(indexed_paths: Iterable[str]) -> tuple[tuple[str, ...], ...]:
    """Return only bin/obj roots that are direct children of an indexed project."""
    project_suffixes = {".csproj", ".fsproj", ".vbproj"}
    roots = {
        (*PurePosixPath(relative).parent.parts, output_name)
        for relative in indexed_paths
        if PurePosixPath(relative).suffix.casefold() in project_suffixes
        for output_name in ("bin", "obj")
    }
    return tuple(sorted(roots))


def _is_generated_runtime_output(
    relative: str,
    project_output_roots: Iterable[tuple[str, ...]],
) -> bool:
    parts = PurePosixPath(relative).parts
    return any(parts[:len(root)] == root for root in project_output_roots)


def _is_dependency_generated_output(
    relative: str,
    project_output_roots: Iterable[tuple[str, ...]],
) -> bool:
    parts = PurePosixPath(relative).parts
    return parts[:1] == (".git",) or _is_generated_runtime_output(
        relative,
        project_output_roots,
    )


def _is_inert_dependency_symlink(
    dependency: str,
    relative: str,
    object_id: str,
) -> bool:
    """Match one human-approved inert link by dependency, path, and blob identity."""
    return INERT_DEPENDENCY_SYMLINK_OBJECTS.get((dependency, relative)) == object_id


def _symlink_git_object(checkout: Path, relative: str) -> str:
    """Hash one symlink's stored target without following it."""
    try:
        target = os.readlink(checkout / relative)
        completed = subprocess.run(
            ["git", "hash-object", "--stdin"],
            cwd=checkout,
            input=os.fsencode(target),
            check=False,
            capture_output=True,
            timeout=15,
        )
    except (OSError, subprocess.SubprocessError):
        return ""
    if completed.returncode != 0:
        return ""
    return completed.stdout.decode("ascii", errors="ignore").strip().lower()


def _bulk_worktree_git_objects(
    checkout: Path,
    entries: list[tuple[str, str]],
) -> tuple[dict[str, frozenset[str]], str | None]:
    """Bulk-hash raw and checkout-filtered dependency bytes."""
    objects: dict[str, set[str]] = {path: set() for path, _ in entries}
    regular_paths = [path for path, mode in entries if mode != "120000"]
    if any("\n" in path or "\r" in path for path in regular_paths):
        return {}, "The worktree contains a path unsupported by bulk hashing."
    if regular_paths:
        encoded_paths = ("\n".join(regular_paths) + "\n").encode("utf-8")
        for options in ((), ("--no-filters",)):
            try:
                completed = subprocess.run(
                    ["git", "hash-object", *options, "--stdin-paths"],
                    cwd=checkout,
                    input=encoded_paths,
                    check=False,
                    capture_output=True,
                    timeout=60,
                )
            except (OSError, subprocess.SubprocessError):
                return {}, "Unable to hash worktree bytes."
            digests = completed.stdout.decode("ascii", errors="ignore").splitlines()
            if completed.returncode != 0 or len(digests) != len(regular_paths):
                return {}, "Unable to hash worktree bytes."
            for path, digest in zip(regular_paths, digests, strict=True):
                objects[path].add(digest.strip().lower())

    return {path: frozenset(values) for path, values in objects.items()}, None


def _validate_dependency_checkout(
    repository_root: Path,
    relative: str,
    indexed_sha: str,
) -> list[str]:
    """Reject dependency bytes that are not identified by the root gitlink."""
    issues: list[str] = []
    checkout = repository_root / relative
    if _path_has_symlink_component(checkout) or not checkout.is_dir():
        return [f"Runtime dependency checkout is missing or symlinked: {relative}"]

    checkout_sha = _git(checkout, "rev-parse", "HEAD")
    if checkout_sha != indexed_sha:
        issues.append(f"Runtime dependency gitlink/check-out drifted: {relative}")

    nested = _git_completed(checkout, "submodule", "status", "--recursive")
    if nested is None or nested.returncode != 0:
        issues.append(f"Unable to inspect nested submodules in runtime dependency: {relative}")
    else:
        initialized = []
        for raw in nested.stdout.splitlines():
            if raw and raw[:1] != b"-":
                fields = raw[1:].decode("utf-8", errors="replace").split()
                initialized.append(fields[1] if len(fields) > 1 else "<unreadable>")
        if initialized:
            issues.append(
                f"Runtime dependency contains initialized nested submodules: {relative}: "
                + ", ".join(sorted(initialized))
            )

    indexed = _git_completed(checkout, "ls-files", "-s", "-z")
    if indexed is None or indexed.returncode != 0:
        issues.append(f"Unable to enumerate runtime dependency index: {relative}")
        return issues
    index_entries: dict[str, tuple[str, str, str]] = {}
    for raw in indexed.stdout.split(b"\0"):
        if not raw:
            continue
        try:
            metadata, encoded_path = raw.split(b"\t", 1)
            mode, object_id, stage = metadata.decode("ascii").split()
            path = encoded_path.decode("utf-8")
        except (UnicodeDecodeError, ValueError):
            issues.append(f"Runtime dependency index is malformed: {relative}")
            continue
        index_entries[path] = (mode, object_id.lower(), stage)

    pathspecs = ["."]
    verbose_flags, verbose_error = _runtime_index_flag_paths(checkout, "-v", pathspecs)
    type_flags, type_error = _runtime_index_flag_paths(checkout, "-t", pathspecs)
    for flag_error in (verbose_error, type_error):
        if flag_error is not None:
            issues.append(f"{flag_error} Dependency: {relative}")
    assume_unchanged = sorted(path for path, tag in verbose_flags.items() if tag.islower())
    skip_worktree = sorted(path for path, tag in type_flags.items() if tag.upper() == "S")
    if assume_unchanged:
        issues.append(
            f"Runtime dependency contains assume-unchanged entries: {relative}: "
            + ", ".join(assume_unchanged)
        )
    if skip_worktree:
        issues.append(
            f"Runtime dependency contains skip-worktree entries: {relative}: "
            + ", ".join(skip_worktree)
        )

    index_drift = _git_completed(
        checkout,
        "diff",
        "--cached",
        "--quiet",
        "--no-ext-diff",
        "HEAD",
        "--",
        ".",
    )
    if index_drift is None or index_drift.returncode not in (0, 1):
        issues.append(f"Unable to compare runtime dependency index with HEAD: {relative}")
    elif index_drift.returncode == 1:
        issues.append(f"Runtime dependency index differs from HEAD: {relative}")

    head_objects: dict[str, tuple[str, str]] = {}
    listed_head = _git_completed(checkout, "ls-tree", "-r", "-z", "--full-tree", "HEAD")
    if listed_head is None or listed_head.returncode != 0:
        issues.append(f"Unable to enumerate runtime dependency HEAD: {relative}")
    else:
        for raw in listed_head.stdout.split(b"\0"):
            if not raw:
                continue
            try:
                metadata, encoded_path = raw.split(b"\t", 1)
                mode, kind, object_id = metadata.decode("ascii").split()
                path = encoded_path.decode("utf-8")
            except (UnicodeDecodeError, ValueError):
                issues.append(f"Runtime dependency HEAD is malformed: {relative}")
                continue
            if kind == "blob":
                head_objects[path] = (mode, object_id.lower())

    hash_entries: list[tuple[str, str]] = []
    for path, (mode, object_id, stage) in sorted(index_entries.items()):
        if stage == "0" and mode != "160000":
            if mode == "120000":
                if not _is_inert_dependency_symlink(relative, path, object_id):
                    issues.append(
                        f"Runtime dependency contains a tracked symlink selected by the "
                        f"build/runtime input scope: {relative}/{path}"
                    )
                continue
            candidate = checkout / path
            if _path_has_symlink_component(candidate):
                issues.append(f"Runtime dependency input is symlinked: {relative}/{path}")
            else:
                hash_entries.append((path, mode))
    worktree_objects, hash_error = _bulk_worktree_git_objects(checkout, hash_entries)
    if hash_error is not None:
        issues.append(f"{hash_error} Dependency: {relative}")

    for path, (mode, object_id, stage) in sorted(index_entries.items()):
        if stage != "0":
            issues.append(f"Runtime dependency has an unresolved index stage: {relative}/{path}")
            continue
        if mode == "160000":
            continue
        if mode == "120000":
            candidate = checkout / path
            if not candidate.is_symlink():
                issues.append(
                    f"Runtime dependency tracked symlink is missing from the worktree: "
                    f"{relative}/{path}"
                )
            if head_objects.get(path) != (mode, object_id):
                issues.append(f"Runtime dependency index identity differs from HEAD: {relative}/{path}")
            if _symlink_git_object(checkout, path) != object_id:
                issues.append(
                    f"Runtime dependency worktree symlink differs from the Git index: "
                    f"{relative}/{path}"
                )
            continue
        candidate = checkout / path
        if _path_has_symlink_component(candidate):
            continue
        if head_objects.get(path) != (mode, object_id):
            issues.append(f"Runtime dependency index identity differs from HEAD: {relative}/{path}")
        if object_id not in worktree_objects.get(path, frozenset()):
            issues.append(f"Runtime dependency worktree bytes differ from the Git index: {relative}/{path}")

    untracked_paths: set[str] = set()
    for options in (("--exclude-standard",), ("--ignored", "--exclude-standard")):
        untracked = _git_completed(checkout, "ls-files", "--others", *options, "-z")
        if untracked is None or untracked.returncode != 0:
            issues.append(f"Unable to enumerate runtime dependency untracked inputs: {relative}")
            continue
        untracked_paths.update(
            item.decode("utf-8", errors="replace")
            for item in untracked.stdout.split(b"\0")
            if item
        )
    project_output_roots = _project_output_roots(index_entries)
    relevant_untracked = sorted(
        path
        for path in untracked_paths
        if not _is_dependency_generated_output(path, project_output_roots)
    )
    untracked_symlinks = sorted(
        path for path in relevant_untracked if (checkout / path).is_symlink()
    )
    if untracked_symlinks:
        issues.append(
            f"Runtime dependency contains untracked symlink inputs: {relative}: "
            + ", ".join(untracked_symlinks)
        )
    if relevant_untracked:
        issues.append(
            f"Runtime dependency contains untracked inputs: {relative}: "
            + ", ".join(relevant_untracked)
        )
    return issues


def _runtime_input_snapshot(repository_root: Path) -> tuple[list[dict[str, Any]], list[str]]:
    """Return the fixed runtime-input inventory and fail-closed cleanliness issues."""
    issues: list[str] = []
    pathspecs = ["src", "samples/Counter", *RUNTIME_ROOT_INPUTS, *RUNTIME_PACT_INPUTS, *RUNTIME_DEPENDENCY_GITLINKS]
    indexed = _git_completed(repository_root, "ls-files", "-s", "-z", "--", *pathspecs)
    if indexed is None or indexed.returncode != 0:
        return [], ["Unable to enumerate the fixed runtime-input scope from the Git index."]
    index_entries: dict[str, tuple[str, str, str]] = {}
    for raw in indexed.stdout.split(b"\0"):
        if not raw:
            continue
        try:
            metadata, encoded_path = raw.split(b"\t", 1)
            mode, object_id, stage = metadata.decode("ascii").split()
            relative = encoded_path.decode("utf-8")
        except (UnicodeDecodeError, ValueError):
            issues.append("The Git index contains an unreadable runtime-input entry.")
            continue
        if relative in index_entries:
            issues.append(f"The Git index repeats a runtime-input path: {relative}")
        index_entries[relative] = (mode, object_id.lower(), stage)

    regular_paths = sorted(
        path
        for path, (mode, _, stage) in index_entries.items()
        if mode != "160000" and stage == "0"
    )
    expected_explicit = {
        *set(RUNTIME_ROOT_INPUTS).difference(OPTIONAL_ABSENT_RUNTIME_ROOT_INPUTS),
        *RUNTIME_PACT_INPUTS,
    }
    missing_explicit = sorted(expected_explicit - set(regular_paths))
    if missing_explicit:
        issues.append(
            "The fixed runtime-input scope is missing tracked inputs: " + ", ".join(missing_explicit)
        )
    unexpected_modes = sorted(
        path
        for path, (mode, _, stage) in index_entries.items()
        if path not in RUNTIME_DEPENDENCY_GITLINKS and (mode == "160000" or stage != "0")
    )
    if unexpected_modes:
        issues.append(
            "Runtime-input files contain a gitlink or unresolved index stage: "
            + ", ".join(unexpected_modes)
        )

    verbose_flags, verbose_error = _runtime_index_flag_paths(
        repository_root, "-v", pathspecs
    )
    type_flags, type_error = _runtime_index_flag_paths(
        repository_root, "-t", pathspecs
    )
    for flag_error in (verbose_error, type_error):
        if flag_error is not None:
            issues.append(flag_error)
    assume_unchanged = sorted(
        path for path, tag in verbose_flags.items() if tag.islower()
    )
    skip_worktree = sorted(
        path for path, tag in type_flags.items() if tag.upper() == "S"
    )
    if assume_unchanged:
        issues.append(
            "Runtime-input scope contains assume-unchanged entries: "
            + ", ".join(assume_unchanged)
        )
    if skip_worktree:
        issues.append(
            "Runtime-input scope contains skip-worktree entries: "
            + ", ".join(skip_worktree)
        )

    try:
        root_entries = list(repository_root.iterdir())
    except OSError:
        root_entries = []
        issues.append("Unable to enumerate recognized root build-control inputs.")
    for path in sorted(root_entries, key=lambda item: item.name.casefold()):
        if not ROOT_BUILD_CONTROL_RE.fullmatch(path.name):
            continue
        if path.name not in RUNTIME_ROOT_INPUTS:
            issues.append(
                f"Recognized root build-control input is outside the fixed scope: {path.name}"
            )
        elif path.name not in index_entries:
            issues.append(
                f"Recognized root build-control input is not tracked: {path.name}"
            )

    untracked_paths: set[str] = set()
    for label, options in (
        ("untracked", ("--exclude-standard",)),
        ("ignored", ("--ignored", "--exclude-standard")),
    ):
        untracked = _git_completed(
            repository_root,
            "ls-files",
            "--others",
            *options,
            "-z",
            "--",
            "src",
            "samples/Counter",
            *RUNTIME_ROOT_INPUTS,
            *RUNTIME_PACT_INPUTS,
        )
        if untracked is None or untracked.returncode != 0:
            issues.append(f"Unable to enumerate {label} files in the runtime-input scope.")
            continue
        untracked_paths.update(
            item.decode("utf-8", errors="replace")
            for item in untracked.stdout.split(b"\0")
            if item
        )
    project_output_roots = _project_output_roots(regular_paths)
    paths = sorted(
        relative
        for relative in untracked_paths
        if not _is_generated_runtime_output(relative, project_output_roots)
    )
    if paths:
        issues.append("Runtime-input scope contains untracked files: " + ", ".join(paths))

    result = _git_completed(
        repository_root,
        "diff",
        "--cached",
        "--quiet",
        "--no-ext-diff",
        "HEAD",
        "--",
        *pathspecs,
    )
    if result is None or result.returncode not in (0, 1):
        issues.append("Unable to compare the runtime-input index with HEAD.")
    elif result.returncode == 1:
        issues.append("Runtime-input index differs from HEAD.")

    head_objects, head_issues = _runtime_git_tree(repository_root, "HEAD")
    issues.extend(head_issues)

    hash_entries = [
        (relative, index_entries[relative][0])
        for relative in regular_paths
        if index_entries[relative][0] != "120000"
        and not _path_has_symlink_component(repository_root / relative)
    ]
    worktree_objects, hash_error = _bulk_worktree_git_objects(
        repository_root,
        hash_entries,
    )
    if hash_error is not None:
        issues.append(hash_error)

    entries: list[dict[str, Any]] = []
    for relative in regular_paths:
        mode, object_id, _ = index_entries[relative]
        path = repository_root / relative
        if mode == "120000" or _path_has_symlink_component(path):
            issues.append(f"Runtime-input path is symlinked: {relative}")
            continue
        data = _bounded_read(path, issues, f"runtime input {relative}")
        if data is None:
            continue
        if head_objects.get(relative) != (mode, object_id):
            issues.append(f"Runtime-input index identity differs from HEAD: {relative}")
        if object_id not in worktree_objects.get(relative, frozenset()):
            issues.append(f"Runtime-input worktree bytes differ from the Git index: {relative}")
        entries.append(
            {
                "path": relative,
                "kind": "file",
                "bytes": len(data),
                "sha256": hashlib.sha256(data).hexdigest(),
            }
        )

    for relative in sorted(OPTIONAL_ABSENT_RUNTIME_ROOT_INPUTS):
        if relative not in index_entries and not (repository_root / relative).exists():
            entries.append({"path": relative, "kind": "absent"})

    for relative in RUNTIME_DEPENDENCY_GITLINKS:
        indexed_entry = index_entries.get(relative)
        if indexed_entry is None or indexed_entry[0] != "160000" or indexed_entry[2] != "0":
            issues.append(f"Runtime dependency is not an index-stage-0 gitlink: {relative}")
            continue
        indexed_sha = indexed_entry[1]
        head_entry = _git(repository_root, "ls-tree", "HEAD", relative).split()
        head_sha = head_entry[2] if len(head_entry) == 4 else ""
        if indexed_sha != head_sha:
            issues.append(f"Runtime dependency gitlink/check-out drifted: {relative}")
        dependency_issues = _validate_dependency_checkout(
            repository_root,
            relative,
            indexed_sha,
        )
        issues.extend(dependency_issues)
        if any(
            "worktree bytes differ" in issue
            or "untracked inputs" in issue
            or "index differs from HEAD" in issue
            for issue in dependency_issues
        ):
            issues.append(f"Runtime dependency checkout is dirty: {relative}")
        entries.append({"path": relative, "kind": "gitlink", "commit": indexed_sha})

    entries.sort(key=lambda item: str(item["path"]))
    return entries, issues


def _runtime_git_tree(
    repository_root: Path,
    revision: str,
) -> tuple[dict[str, tuple[str, str]], list[str]]:
    """Return mode/object identities for the fixed scope at one commit."""
    issues: list[str] = []
    pathspecs = ["src", "samples/Counter", *RUNTIME_ROOT_INPUTS, *RUNTIME_PACT_INPUTS, *RUNTIME_DEPENDENCY_GITLINKS]
    listed = _git_completed(
        repository_root,
        "ls-tree",
        "-r",
        "-z",
        "--full-tree",
        revision,
        "--",
        *pathspecs,
    )
    if listed is None or listed.returncode != 0:
        return {}, [f"Unable to enumerate runtime inputs at revision {revision}."]
    objects: dict[str, tuple[str, str]] = {}
    for raw in listed.stdout.split(b"\0"):
        if not raw:
            continue
        try:
            metadata, encoded_path = raw.split(b"\t", 1)
            mode, object_kind, object_id = metadata.decode("ascii").split()
            relative = encoded_path.decode("utf-8")
        except (UnicodeDecodeError, ValueError):
            issues.append(f"Revision {revision} contains an unreadable runtime-input entry.")
            continue
        if relative in objects:
            issues.append(f"Revision {revision} repeats runtime-input path: {relative}")
            continue
        if mode == "120000":
            issues.append(f"Revision {revision} has a symlinked runtime input: {relative}")
        elif mode == "160000" and (
            object_kind != "commit" or relative not in RUNTIME_DEPENDENCY_GITLINKS
        ):
            issues.append(f"Revision {revision} has an unexpected runtime gitlink: {relative}")
        elif mode != "160000" and (object_kind != "blob" or not mode.startswith("100")):
            issues.append(f"Revision {revision} has a non-regular runtime input: {relative}")
        objects[relative] = (mode, object_id.lower())
    expected_explicit = {
        *set(RUNTIME_ROOT_INPUTS).difference(OPTIONAL_ABSENT_RUNTIME_ROOT_INPUTS),
        *RUNTIME_PACT_INPUTS,
        *RUNTIME_DEPENDENCY_GITLINKS,
    }
    missing_explicit = sorted(expected_explicit - set(objects))
    if missing_explicit:
        issues.append(
            f"Revision {revision} is missing fixed runtime inputs: "
            + ", ".join(missing_explicit)
        )
    return objects, issues


def runtime_input_manifest(
    repository_root: Path,
    *,
    captured_at: str | None = None,
) -> tuple[dict[str, Any], list[str]]:
    """Build the deterministic runtime-input manifest for evidence generation."""
    entries, issues = _runtime_input_snapshot(repository_root)
    revision = _git(repository_root, "rev-parse", "HEAD")
    if not SOURCE_SHA_RE.fullmatch(revision):
        issues.append("Runtime-input capture revision is unavailable.")
    timestamp = captured_at or datetime.now(timezone.utc).isoformat()
    _parse_timestamp(timestamp, "Runtime-input manifest capturedAt", issues)
    return {
        "schema": "hexalith.frontcomposer.eventstore-runtime-inputs.v1",
        "capturedAt": timestamp,
        "capturedRevision": revision,
        "scope": _runtime_scope(),
        "treeSha256": _runtime_tree_sha256(entries),
        "entries": entries,
    }, issues


def write_runtime_input_manifest(
    output: Path,
    repository_root: Path,
    *,
    captured_at: str | None = None,
) -> list[str]:
    """Atomically write a runtime-input manifest only for a clean fixed scope."""
    document, issues = runtime_input_manifest(repository_root, captured_at=captured_at)
    if issues:
        return issues
    temporary: Path | None = None
    try:
        output.parent.mkdir(parents=True, exist_ok=True)
        descriptor, temporary_name = tempfile.mkstemp(
            dir=output.parent,
            prefix=f".{output.name}.",
            suffix=".tmp",
        )
        temporary = Path(temporary_name)
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="\n") as stream:
            stream.write(json.dumps(document, indent=2) + "\n")
        temporary.replace(output)
    except OSError as error:
        issues.append(f"Unable to write runtime-input manifest: {error}")
        if temporary is not None:
            try:
                temporary.unlink(missing_ok=True)
            except OSError:
                pass
    return issues


def _validate_runtime_input_manifest(
    path: Path,
    repository_root: Path,
    errors: list[str],
) -> tuple[dict[str, Any], datetime | None]:
    document = _read_json(path, errors, "frontcomposer-runtime-inputs.json")
    _scan_redaction(path, errors)
    required_fields = {
        "schema",
        "capturedAt",
        "capturedRevision",
        "scope",
        "treeSha256",
        "entries",
    }
    if set(document) != required_fields:
        errors.append("Runtime-input manifest does not contain the exact required fields.")
    if document.get("schema") != "hexalith.frontcomposer.eventstore-runtime-inputs.v1":
        errors.append("Runtime-input manifest has an unexpected schema.")
    captured_at = _parse_timestamp(
        document.get("capturedAt"), "Runtime-input manifest capturedAt", errors
    )
    revision = document.get("capturedRevision")
    if not isinstance(revision, str) or not SOURCE_SHA_RE.fullmatch(revision):
        errors.append("Runtime-input manifest capturedRevision is not 40-hex.")
        revision = ""
    elif _git(repository_root, "rev-parse", "--verify", f"{revision}^{{commit}}") != revision:
        errors.append("Runtime-input manifest capture revision does not exist.")
    else:
        ancestry = _git_completed(repository_root, "merge-base", "--is-ancestor", revision, "HEAD")
        if ancestry is None or ancestry.returncode != 0:
            errors.append("Current FrontComposer revision does not descend from the capture revision.")
    if not _exact(document.get("scope"), _runtime_scope()):
        errors.append("Runtime-input manifest scope differs from the validator-owned fixed scope.")
    entries = document.get("entries")
    if not isinstance(entries, list) or any(not isinstance(item, dict) for item in entries):
        errors.append("Runtime-input manifest entries are malformed.")
        entries = []
    paths: list[str] = []
    for item in entries:
        relative = item.get("path")
        if not isinstance(relative, str) or not _is_safe_relative_path(relative):
            errors.append("Runtime-input manifest contains an unsafe path.")
            continue
        paths.append(relative)
        kind = item.get("kind")
        expected_fields = (
            {"path", "kind", "commit"}
            if kind == "gitlink"
            else {"path", "kind"}
            if kind == "absent"
            else {"path", "kind", "bytes", "sha256"}
        )
        if set(item) != expected_fields:
            errors.append(f"Runtime-input manifest entry has unexpected fields: {relative}")
        if kind == "file":
            if not isinstance(item.get("bytes"), int) or isinstance(item.get("bytes"), bool) or item["bytes"] <= 0:
                errors.append(f"Runtime-input manifest byte count is invalid: {relative}")
            if not isinstance(item.get("sha256"), str) or not SHA256_RE.fullmatch(item["sha256"]):
                errors.append(f"Runtime-input manifest SHA-256 is invalid: {relative}")
        elif kind == "gitlink":
            if relative not in RUNTIME_DEPENDENCY_GITLINKS or not isinstance(item.get("commit"), str) or not SOURCE_SHA_RE.fullmatch(item["commit"]):
                errors.append(f"Runtime-input manifest gitlink is invalid: {relative}")
        elif kind == "absent":
            if relative not in OPTIONAL_ABSENT_RUNTIME_ROOT_INPUTS:
                errors.append(f"Runtime-input manifest absence marker is invalid: {relative}")
        else:
            errors.append(f"Runtime-input manifest kind is invalid: {relative}")
    if paths != sorted(set(paths)):
        errors.append("Runtime-input manifest paths must be unique and sorted.")
    tree_hash = _runtime_tree_sha256(entries)
    if document.get("treeSha256") != tree_hash:
        errors.append("Runtime-input manifest tree SHA-256 is invalid.")
    if revision:
        captured_tree, captured_issues = _runtime_git_tree(
            repository_root,
            revision,
        )
        current_tree, current_tree_issues = _runtime_git_tree(repository_root, "HEAD")
        errors.extend(captured_issues)
        errors.extend(current_tree_issues)
        if not _exact(captured_tree, current_tree):
            errors.append(
                "Current committed runtime inputs differ from the claimed capture revision."
            )
    current_entries, current_issues = _runtime_input_snapshot(repository_root)
    errors.extend(current_issues)
    if not _exact(current_entries, entries) or not _exact(
        _runtime_tree_sha256(current_entries), document.get("treeSha256")
    ):
        errors.append("Current runtime-relevant inputs differ from the sealed manifest.")
    return document, captured_at


def _live_provenance(
    repository_root: Path,
    errors: list[str],
    *,
    runtime_manifest: dict[str, Any] | None = None,
) -> dict[str, str]:
    eventstore_root = repository_root / "references" / "Hexalith.EventStore"
    builds_root = repository_root / "references" / "Hexalith.Builds"
    source_sha = _git(eventstore_root, "rev-parse", "HEAD")
    builds_sha = _git(builds_root, "rev-parse", "HEAD")
    frontcomposer_revision = (
        runtime_manifest.get("capturedRevision", "")
        if isinstance(runtime_manifest, dict)
        else _git(repository_root, "rev-parse", "HEAD")
    )
    source_gitlink = _git(repository_root, "ls-tree", "HEAD", "references/Hexalith.EventStore").split()
    builds_gitlink = _git(repository_root, "ls-tree", "HEAD", "references/Hexalith.Builds").split()
    expected_source = source_gitlink[2] if len(source_gitlink) == 4 else ""
    expected_builds = builds_gitlink[2] if len(builds_gitlink) == 4 else ""
    catalog_path = builds_root / "Props" / "Directory.Packages.props"
    catalog_text = ""
    try:
        catalog_text = catalog_path.read_text(encoding="utf-8-sig")
    except OSError:
        errors.append("Live provider Builds catalog is unavailable.")
    version_match = re.search(r"<HexalithEventStoreVersion[^>]*>([^<]+)</HexalithEventStoreVersion>", catalog_text)
    version = version_match.group(1).strip() if version_match else ""
    inventory = eventstore_root / "tools" / "release-packages.json"
    inventory_hash = _sha256(inventory, errors, "EventStore release-packages.json")
    if source_sha != expected_source or not SOURCE_SHA_RE.fullmatch(source_sha):
        errors.append("Live provider source checkout does not equal the pinned EventStore gitlink.")
    if builds_sha != expected_builds or not SOURCE_SHA_RE.fullmatch(builds_sha):
        errors.append("Live provider Builds checkout does not equal the pinned Builds gitlink.")
    if not version:
        errors.append("Live provider Release package version is unavailable.")
    if not SOURCE_SHA_RE.fullmatch(frontcomposer_revision):
        errors.append("Live FrontComposer revision is unavailable.")
    if isinstance(runtime_manifest, dict):
        runtime_tree_sha256 = runtime_manifest.get("treeSha256", "")
        if not isinstance(runtime_tree_sha256, str) or not SHA256_RE.fullmatch(
            runtime_tree_sha256
        ):
            errors.append("Live runtime-input manifest tree SHA-256 is unavailable.")
            runtime_tree_sha256 = ""
    else:
        runtime_entries, runtime_issues = _runtime_input_snapshot(repository_root)
        errors.extend(runtime_issues)
        runtime_tree_sha256 = _runtime_tree_sha256(runtime_entries)
    return {
        "sourceSha": source_sha,
        "releaseVersion": version,
        "buildsSha": builds_sha,
        "releaseInventorySha256": inventory_hash,
        "frontComposerRevision": frontcomposer_revision,
        "runtimeInputTreeSha256": runtime_tree_sha256,
    }


def write_live_receipt(
    evidence_root: Path,
    repository_root: Path | None = None,
    *,
    runtime_input_manifest_path: Path | None = None,
    pact_dir: Path | None = None,
) -> list[str]:
    """Validate and bind a passing provider report to its pre-run runtime manifest."""
    errors: list[str] = []
    repository_root = repository_root or Path(__file__).resolve().parents[1]
    if _path_has_symlink_component(evidence_root) or not evidence_root.is_dir():
        return [f"Live evidence root is missing or is a symlink: {evidence_root}"]
    canonical_pact_dir = repository_root / CANONICAL_PACT_ROOT
    selected_pact_dir = pact_dir or canonical_pact_dir
    if not _same_canonical_path(selected_pact_dir, canonical_pact_dir):
        return ["Live receipt creation requires the canonical repository Pact directory."]
    if runtime_input_manifest_path is None:
        return ["Live receipt creation requires the pre-provider runtime-input manifest."]
    manifest_bytes_before = _bounded_read(
        runtime_input_manifest_path,
        errors,
        "pre-provider runtime-input manifest",
    )
    manifest, manifest_captured_at = _validate_runtime_input_manifest(
        runtime_input_manifest_path,
        repository_root,
        errors,
    )
    provenance = _live_provenance(
        repository_root,
        errors,
        runtime_manifest=manifest,
    )
    _validate_live_provider(
        evidence_root,
        canonical_pact_dir,
        provenance,
        errors,
        validate_receipt=False,
    )
    report_path = evidence_root / "provider-verification.json"
    report = _read_json(report_path, errors, "provider-verification.json")
    report_bytes = _bounded_read(report_path, errors, "provider-verification.json")
    required = {
        "finalVerdict": "passed",
        "requestedInteractionCount": 19,
        "reportedInteractionCount": 19,
        "setupEventCount": 19,
        "teardownEventCount": 19,
        "complete": True,
        "hostStopped": True,
        "portClosed": True,
    }
    for key, expected in required.items():
        if not _exact(report.get(key), expected):
            errors.append(f"Live provider report {key} must equal {expected!r} before receipt creation.")
    timing = report.get("timing")
    run_timing = timing.get("run") if isinstance(timing, dict) else None
    completed_at = run_timing.get("completedAt") if isinstance(run_timing, dict) else None
    completed = _parse_timestamp(completed_at, "Live provider report run completedAt", errors)
    _timestamp_not_future(completed, "Live provider report run completedAt", errors)
    started_at = run_timing.get("startedAt") if isinstance(run_timing, dict) else None
    started = _parse_timestamp(started_at, "Live provider report run startedAt", errors)
    if (
        manifest_captured_at is None
        or started is None
        or manifest_captured_at >= started
    ):
        errors.append("Pre-provider runtime-input manifest does not predate provider execution.")
    manifest_bytes_after = _bounded_read(
        runtime_input_manifest_path,
        errors,
        "pre-provider runtime-input manifest",
    )
    if manifest_bytes_before != manifest_bytes_after:
        errors.append("Pre-provider runtime-input manifest changed during receipt creation.")
    if errors or report_bytes is None or manifest_bytes_before is None:
        return errors
    receipt = {
        "schema": "hexalith.eventstore.provider-verification-run-evidence.v3",
        "capturedAt": datetime.now(timezone.utc).isoformat(),
        "completedAt": completed_at,
        "frontComposerRevision": manifest["capturedRevision"],
        "runtimeInputTreeSha256": manifest["treeSha256"],
        "command": "dotnet tests/Hexalith.EventStore.ProviderVerification/bin/Release/net10.0/Hexalith.EventStore.ProviderVerification.dll --verification-mode live-compatibility <validated canonical inputs>",
        "exitCode": 0,
        "expectedNonzero": False,
        "nativeVerifierOutputRetained": False,
        "normalizedPactCopiesRetained": False,
        "externalInputsModified": False,
        "report": {
            "path": "provider-verification.json",
            "sha256": hashlib.sha256(report_bytes).hexdigest(),
            "bytes": len(report_bytes),
            **required,
        },
    }
    output = evidence_root / "run-evidence.json"
    temporary = output.with_name(f".{output.name}.{os.getpid()}.tmp")
    try:
        temporary.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
        temporary.replace(output)
    except OSError as error:
        errors.append(f"Unable to write live provider receipt: {error}")
        try:
            temporary.unlink(missing_ok=True)
        except OSError:
            pass
    return errors


def _validate_live_provider(
    evidence_root: Path,
    pact_dir: Path,
    provenance: dict[str, str],
    errors: list[str],
    *,
    validate_receipt: bool = True,
) -> None:
    report_path = evidence_root / "provider-verification.json"
    receipt_path = evidence_root / "run-evidence.json"
    _scan_redaction(report_path, errors)
    for name in (*PACT_FILES, "interaction-manifest.json", "provider-state-catalog.json"):
        _scan_redaction(pact_dir / name, errors)
    report = _read_json(report_path, errors, "provider-verification.json")
    expected_interactions, contract_hashes, state_names = _pact_interactions(pact_dir, errors)
    # Historical Story 11.24 evidence was produced from a Windows CRLF checkout and
    # remains bound to those immutable bytes. The live verifier hashes the current
    # files exactly as supplied, so its lane must compare raw bytes rather than the
    # historical checkout normalization above.
    contract_hashes = {
        filename: _sha256(pact_dir / filename, errors, filename)
        for filename in (*PACT_FILES, "interaction-manifest.json", "provider-state-catalog.json")
    }
    expected_count = 19
    expected_scalars = {
        "schema": "hexalith.eventstore.provider-verification.v1",
        "verificationMode": "live-compatibility",
        "finalVerdict": "passed",
        "requestedInteractionCount": expected_count,
        "reportedInteractionCount": expected_count,
        "requestedStateCount": expected_count,
        "setupEventCount": expected_count,
        "teardownEventCount": expected_count,
        "complete": True,
        "hostStarted": True,
        "readyProbePassed": True,
        "hostStopped": True,
        "portClosed": True,
    }
    for key, expected in expected_scalars.items():
        if not _exact(report.get(key), expected):
            errors.append(f"Live provider report {key} must equal {expected!r}.")
    if len(expected_interactions) != expected_count or len(state_names) != expected_count:
        errors.append("Live pacts/provider catalog must contain exactly 19 interactions and states.")
    if not _exact(report.get("reasonCodes"), []):
        errors.append("Live provider report reasonCodes must be empty for a passing run.")
    expected_host = {
        "server": "Kestrel",
        "pipeline": "production-gateway",
        "transport": "http",
        "addressFamily": "IPv4",
        "bindScope": "loopback",
        "portAllocation": "os-assigned-ephemeral",
    }
    if not _exact(report.get("host"), expected_host):
        errors.append("Live provider report host bounds are not exact.")
    identity = report.get("identity", {})
    expected_identity = {
        "verificationMode": "live-compatibility",
        "expectedSourceSha": provenance["sourceSha"],
        "observedSourceSha": provenance["sourceSha"],
        "expectedVersion": provenance["releaseVersion"],
        "expectedBuildsSha": provenance["buildsSha"],
        "observedBuildsSha": provenance["buildsSha"],
        "releaseInventorySha256": provenance["releaseInventorySha256"],
        "observedReleaseInventorySha256": provenance["releaseInventorySha256"],
        "evidenceManifestSha256": "",
        "decisionRecordSha256": "",
        "subjectSha256": "",
        "approvalCount": 0,
        "approvalAuthorized": False,
        "runtimeMatches": True,
        "reasonCodes": [],
    }
    if not isinstance(identity, dict):
        errors.append("Live provider identity is malformed.")
        identity = {}
    for key, expected in expected_identity.items():
        if not _exact(identity.get(key), expected):
            errors.append(f"Live provider identity {key} is stale or untruthful.")
    observed_version = str(identity.get("observedVersion", ""))
    if not (
        observed_version == provenance["releaseVersion"]
        or observed_version == f"{provenance['releaseVersion']}+{provenance['sourceSha']}"
    ):
        errors.append("Live provider observed runtime version is stale or untruthful.")
    actual_interactions = report.get("interactions", [])
    actual_keys: list[dict[str, str]] = []
    if not isinstance(actual_interactions, list) or len(actual_interactions) != expected_count:
        errors.append("Live provider report does not account for all 19 interactions.")
        actual_interactions = []
    for index, item in enumerate(actual_interactions, start=1):
        if not isinstance(item, dict):
            errors.append(f"Live provider interaction {index} is malformed.")
            continue
        actual_keys.append({
            "description": str(item.get("description", "")),
            "providerState": str(item.get("providerState", "")),
            "pactFile": str(item.get("pactFile", "")),
        })
        if not _exact(item.get("index"), index) or not _exact(
            item.get("resultCode"), "interaction.passed"
        ):
            errors.append(f"Live provider interaction {index} did not pass deterministically.")
        duration = item.get("durationMilliseconds")
        if not isinstance(duration, int) or isinstance(duration, bool) or not 0 <= duration <= MAX_RUN_MILLISECONDS:
            errors.append(f"Live provider interaction {index} duration is unbounded.")
        events = item.get("stateEvents", [])
        if not isinstance(events, list) or len(events) != 2:
            errors.append(f"Live provider interaction {index} lacks setup/teardown accounting.")
            continue
        event_durations: list[int] = []
        for event, action, result in zip(
            events,
            ("setup", "teardown"),
            ("state.setup.succeeded", "state.teardown.succeeded"),
            strict=True,
        ):
            if not isinstance(event, dict) or event.get("state") != item.get("providerState") or event.get("action") != action or event.get("resultCode") != result:
                errors.append(f"Live provider interaction {index} cleanup is incomplete.")
                continue
            event_duration = event.get("durationMilliseconds")
            if (
                not isinstance(event_duration, int)
                or isinstance(event_duration, bool)
                or not 0 <= event_duration <= MAX_RUN_MILLISECONDS
            ):
                errors.append(
                    f"Live provider interaction {index} {action} duration is unbounded."
                )
            else:
                event_durations.append(event_duration)
        if (
            isinstance(duration, int)
            and not isinstance(duration, bool)
            and len(event_durations) == 2
            and sum(event_durations) > duration
        ):
            errors.append(
                f"Live provider interaction {index} state-event durations exceed the interaction duration."
            )
    if not _exact(actual_keys, expected_interactions):
        errors.append("Live provider interactions do not match the current Pact manifest.")
    _validate_timing(report, False, errors, duration_tolerance_microseconds=10_000)
    input_values = report.get("inputHashes", [])
    if not isinstance(input_values, list):
        errors.append("Live provider input hashes must be an array.")
        input_values = []
    names: list[str] = []
    for item in input_values:
        if not isinstance(item, dict) or set(item) != {"name", "kind", "sha256"}:
            errors.append("Live provider input hashes contain a malformed binding.")
            continue
        name = item.get("name")
        if not isinstance(name, str):
            errors.append("Live provider input hash name is malformed.")
            continue
        names.append(name)
    if len(names) != len(set(names)):
        errors.append("Live provider input hashes contain duplicate names.")
    input_hashes = {
        str(item.get("name", "")): str(item.get("sha256", ""))
        for item in input_values
        if isinstance(item, dict)
    }
    if not _exact(input_hashes, contract_hashes):
        errors.append("Live provider input hashes do not bind the exact current Pact bytes.")
    expected_kinds = {
        **{name: "pact" for name in PACT_FILES},
        "interaction-manifest.json": "interaction-manifest",
        "provider-state-catalog.json": "provider-state-catalog",
    }
    actual_kinds = {
        str(item.get("name", "")): str(item.get("kind", ""))
        for item in input_values
        if isinstance(item, dict)
    }
    if not _exact(actual_kinds, expected_kinds):
        errors.append("Live provider input kinds are not exact.")
    if not validate_receipt:
        return
    receipt = _read_json(receipt_path, errors, "run-evidence.json")
    report_bytes = _bounded_read(report_path, errors, "provider-verification.json")
    expected_receipt = {
        "schema": "hexalith.eventstore.provider-verification-run-evidence.v3",
        "frontComposerRevision": provenance["frontComposerRevision"],
        "runtimeInputTreeSha256": provenance["runtimeInputTreeSha256"],
        "command": "dotnet tests/Hexalith.EventStore.ProviderVerification/bin/Release/net10.0/Hexalith.EventStore.ProviderVerification.dll --verification-mode live-compatibility <validated canonical inputs>",
        "exitCode": 0,
        "expectedNonzero": False,
        "nativeVerifierOutputRetained": False,
        "normalizedPactCopiesRetained": False,
        "externalInputsModified": False,
    }
    for key, expected in expected_receipt.items():
        if not _exact(receipt.get(key), expected):
            errors.append(f"Live provider run receipt {key} is not a passing bounded invocation.")
    if set(receipt) != {*expected_receipt, "capturedAt", "completedAt", "report"}:
        errors.append("Live provider run receipt does not contain the exact required fields.")
    receipt_captured = _parse_timestamp(receipt.get("capturedAt"), "Live provider receipt capturedAt", errors)
    receipt_completed = _parse_timestamp(receipt.get("completedAt"), "Live provider receipt completedAt", errors)
    _timestamp_not_future(receipt_captured, "Live provider receipt capturedAt", errors)
    _timestamp_not_future(receipt_completed, "Live provider receipt completedAt", errors)
    timing = report.get("timing")
    run_timing = timing.get("run") if isinstance(timing, dict) else None
    report_started = _parse_timestamp(
        run_timing.get("startedAt") if isinstance(run_timing, dict) else None,
        "Live provider report run startedAt",
        errors,
    )
    report_completed = _parse_timestamp(
        run_timing.get("completedAt") if isinstance(run_timing, dict) else None,
        "Live provider report run completedAt",
        errors,
    )
    _timestamp_not_future(report_started, "Live provider report run startedAt", errors)
    _timestamp_not_future(report_completed, "Live provider report run completedAt", errors)
    if receipt_completed != report_completed:
        errors.append("Live provider receipt completion does not equal the provider run completion.")
    if (
        receipt_completed is None
        or receipt_captured is None
        or receipt_captured < receipt_completed
    ):
        errors.append("Live provider receipt was captured before the provider run completed.")
    receipt_report = receipt.get("report", {})
    if not isinstance(receipt_report, dict):
        errors.append("Live provider run receipt report binding is malformed.")
        receipt_report = {}
    expected_report_binding = {
        "path": "provider-verification.json",
        "sha256": hashlib.sha256(report_bytes).hexdigest() if report_bytes is not None else "",
        "bytes": len(report_bytes) if report_bytes is not None else -1,
        "finalVerdict": "passed",
        "requestedInteractionCount": 19,
        "reportedInteractionCount": 19,
        "setupEventCount": 19,
        "teardownEventCount": 19,
        "complete": True,
        "hostStopped": True,
        "portClosed": True,
    }
    if not _exact(receipt_report, expected_report_binding):
        errors.append("Live provider run receipt does not exactly bind the passing report.")


def _validate_live_apphost(evidence_root: Path, repository_root: Path, provenance: dict[str, str], errors: list[str]) -> None:
    path = evidence_root / "apphost-smoke.json"
    smoke = _read_json(path, errors, "apphost-smoke.json")
    if set(smoke) != {
        "schema", "capturedAt", "completedAt", "timeoutSeconds", "finalVerdict", "reasonCodes",
        "identity", "topology", "startup", "observations", "authorizationControls", "cleanup",
    }:
        errors.append("Live AppHost smoke does not contain the exact required fields.")
    if smoke.get("schema") != "hexalith.frontcomposer.pact-provider-reconciliation-apphost-smoke.v2":
        errors.append("Live AppHost smoke has an unexpected schema.")
    captured_at = _parse_timestamp(smoke.get("capturedAt"), "Live AppHost smoke capturedAt", errors)
    completed_at = _parse_timestamp(smoke.get("completedAt"), "Live AppHost smoke completedAt", errors)
    _timestamp_not_future(captured_at, "Live AppHost smoke capturedAt", errors)
    _timestamp_not_future(completed_at, "Live AppHost smoke completedAt", errors)
    if captured_at is None or completed_at is None or completed_at < captured_at:
        errors.append("Live AppHost smoke completion does not follow capture start.")
    timeout_seconds = smoke.get("timeoutSeconds")
    if (
        not isinstance(timeout_seconds, int)
        or isinstance(timeout_seconds, bool)
        or not 30 <= timeout_seconds <= 600
    ):
        errors.append("Live AppHost smoke timeoutSeconds is invalid.")
    elif (
        captured_at is not None
        and completed_at is not None
        and (completed_at - captured_at).total_seconds() > timeout_seconds + 1
    ):
        errors.append("Live AppHost smoke elapsed duration exceeds timeoutSeconds.")
    if not _exact(smoke.get("finalVerdict"), "passed") or not _exact(smoke.get("reasonCodes"), []):
        errors.append("Live AppHost smoke is not a clean passing run.")
    identity = smoke.get("identity", {})
    expected_identity = {
        "eventStoreSourceSha": provenance["sourceSha"],
        "eventStoreReleaseVersion": provenance["releaseVersion"],
        "buildsCatalogSha": provenance["buildsSha"],
        "frontComposerRevision": provenance["frontComposerRevision"],
        "runtimeInputTreeSha256": provenance["runtimeInputTreeSha256"],
    }
    if not _exact(identity, expected_identity):
        errors.append("Live AppHost smoke provenance is stale or untruthful.")
    topology = smoke.get("topology", {})
    declared_resources = topology.get("declaredResources") if isinstance(topology, dict) else None
    if (
        not isinstance(declared_resources, list)
        or any(not isinstance(name, str) for name in declared_resources)
        or len(declared_resources) != 10
        or len(set(declared_resources)) != len(declared_resources)
        or set(declared_resources) != {
            "security", "eventstore", "eventstore-admin", "eventstore-admin-ui", "tenants",
            "parties", "sample", "tenants-ui", "frontcomposer-ui", "counter-web",
        }
    ):
        errors.append("Live AppHost smoke does not name exactly the ten declared resources.")
        declared_resources = []
    program = repository_root / "src/Hexalith.FrontComposer.AppHost/Program.cs"
    project = repository_root / "src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj"
    expected_topology = {
        "programPath": "src/Hexalith.FrontComposer.AppHost/Program.cs",
        "programSha256": _sha256(program, errors),
        "projectPath": "src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj",
        "projectSha256": _sha256(project, errors),
        "modifiedForSmoke": False,
        "declaredResources": declared_resources,
    }
    if not _exact(topology, expected_topology):
        errors.append("Live AppHost smoke does not bind the exact current topology and resource set.")
    startup = smoke.get("startup", {})
    expected_startup = {
        "result": "passed",
        "hostStartAttempted": True,
        "hostStarted": True,
        "outputPreparation": {
            "clean": "passed",
            "restore": "passed",
            "build": "passed",
            "configuration": "Debug",
            "restoreMode": "forced-no-cache",
            "buildMode": "no-incremental",
            "startMode": "no-build",
            "evaluatedBuildProperties": APPHOST_BUILD_PROPERTIES,
            "sourceDependencyGitlinks": list(RUNTIME_DEPENDENCY_GITLINKS),
            "buildControlGitlinks": list(APPHOST_BUILD_CONTROL_GITLINKS),
            "reachableSourceGitlinks": list(APPHOST_REACHABLE_SOURCE_GITLINKS),
            "inactiveGuardedGitlinks": list(APPHOST_INACTIVE_GUARDED_GITLINKS),
            "evaluatedSourceGraph": "passed",
        },
        "resourceWaits": {
            name: "healthy"
            for name in (
                "security", "eventstore", "eventstore-admin", "eventstore-admin-ui", "tenants",
                "parties", "sample", "tenants-ui", "frontcomposer-ui", "counter-web",
            )
        },
    }
    if not _exact(startup, expected_startup):
        errors.append("Live AppHost startup does not account for every declared healthy resource.")
    observations = smoke.get("observations", {})
    if not isinstance(observations, dict) or set(observations) != set(APPHOST_OBSERVATIONS):
        errors.append("Live AppHost smoke does not account for every authenticated surface.")
    else:
        command_submit = observations.get("commandSubmit", {})
        aggregate_id = command_submit.get("aggregateId") if isinstance(command_submit, dict) else None
        message_id = command_submit.get("messageId") if isinstance(command_submit, dict) else None
        correlation_id = command_submit.get("correlationId") if isinstance(command_submit, dict) else None
        provenance_name = observations.get("queryProvenance", {}).get("provenance") if isinstance(observations.get("queryProvenance"), dict) else None
        expected_reason = APPHOST_QUERY_PROVENANCE.get(provenance_name)
        signalr_control = smoke.get("authorizationControls", {}).get("projectionSignalR", {}) if isinstance(smoke.get("authorizationControls"), dict) else {}
        signalr_endpoint = signalr_control.get("endpoint") if isinstance(signalr_control, dict) else None
        health = observations.get("health", {})
        health_status = health.get("statusCode") if isinstance(health, dict) else None
        if (
            not isinstance(health_status, int)
            or isinstance(health_status, bool)
            or health_status not in (200, 204)
        ):
            errors.append("Live AppHost readiness health status is not successful.")
        expected_observations = {
            "health": {
                "result": "passed", "authenticated": False,
                "reasonCode": "health.readiness.succeeded", "statusCode": health_status,
            },
            "commandSubmit": {
                "result": "passed", "authenticated": True, "reasonCode": "command.accepted",
                "statusCode": 202, "aggregateId": aggregate_id,
                "messageId": message_id, "correlationId": correlation_id,
            },
            "commandStatus": {
                "result": "passed", "authenticated": True, "reasonCode": "command.completed",
                "terminalStatus": "Completed", "aggregateId": aggregate_id,
            },
            "queryProvenance": {
                "result": "passed", "authenticated": True, "reasonCode": expected_reason,
                "statusCode": 200, "provenance": provenance_name, "tenant": "system",
                "aggregateId": aggregate_id, "entityId": aggregate_id,
                "responseTenantId": aggregate_id,
            },
            "projectionSignalR": {
                "result": "passed", "authenticated": True,
                "reasonCode": "signalr.authenticated-connect.succeeded",
                "endpoint": signalr_endpoint,
            },
        }
        if not isinstance(aggregate_id, str) or not ACTIVE_AGGREGATE_ID_RE.fullmatch(aggregate_id):
            errors.append("Live AppHost command observation lacks its generated aggregate identity.")
        if (
            not isinstance(message_id, str)
            or not re.fullmatch(r"[0-7][0-9A-HJKMNP-TV-Z]{25}", message_id)
            or not _exact(correlation_id, message_id)
        ):
            errors.append("Live AppHost command correlation does not equal the submitted ULID.")
        for name, expected in expected_observations.items():
            if expected_reason is None and name == "queryProvenance":
                errors.append("Live AppHost query provenance stamp is missing or drifted.")
            elif not _exact(observations.get(name), expected):
                errors.append(f"Live AppHost observation has incorrect semantics: {name}")
    controls = smoke.get("authorizationControls", {})
    expected_credentials = {
        "commandSubmit": "invalid-bearer",
        "commandStatus": "invalid-bearer",
        "queryProvenance": "invalid-bearer",
        "projectionSignalR": "invalid-bearer",
    }
    if not isinstance(controls, dict) or set(controls) != set(expected_credentials):
        errors.append("Live AppHost smoke does not account for every authorization negative control.")
    else:
        for name, credential in expected_credentials.items():
            item = controls.get(name, {})
            expected_fields = {"result", "credential", "reasonCode", "statusCode"}
            if name == "projectionSignalR":
                expected_fields.add("endpoint")
            status_code = item.get("statusCode") if isinstance(item, dict) else None
            expected_item = {
                "result": "passed",
                "credential": credential,
                "reasonCode": (
                    "authorization.anonymous.rejected"
                    if credential == "anonymous"
                    else "authorization.invalid-bearer.rejected"
                ),
                "statusCode": status_code,
            }
            if name == "projectionSignalR":
                expected_item["endpoint"] = item.get("endpoint") if isinstance(item, dict) else None
            if (
                not isinstance(item, dict)
                or set(item) != expected_fields
                or not _exact(item, expected_item)
                or not isinstance(status_code, int)
                or isinstance(status_code, bool)
                or status_code not in (401, 403)
                or (
                    name == "projectionSignalR"
                    and (
                        not isinstance(item.get("endpoint"), str)
                        or not _is_projection_changes_hub_url(item["endpoint"])
                    )
                )
            ):
                errors.append(f"Live AppHost authorization control did not prove rejection: {name}")
        signalr_observation = observations.get("projectionSignalR", {}) if isinstance(observations, dict) else {}
        projection_control = controls.get("projectionSignalR", {})
        projection_control_endpoint = (
            projection_control.get("endpoint")
            if isinstance(projection_control, dict)
            else None
        )
        if (
            not isinstance(signalr_observation, dict)
            or not _is_projection_changes_hub_url(signalr_observation.get("endpoint"))
            or not _exact(
                signalr_observation.get("endpoint"),
                projection_control_endpoint,
            )
        ):
            errors.append("Live AppHost SignalR evidence is not bound to the exact projection-change hub.")
    cleanup = smoke.get("cleanup", {})
    expected_cleanup = {
        "command": "aspire stop --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --nologo",
        "result": "clean",
        "hostStopped": True,
        "portsClosed": True,
        "runningAppHostsAfterAttempt": 0,
        "listenerConfirmation": "ports-probed-closed",
        "confirmation": "aspire-ps-empty",
        "runtimeInputsCleanAfterRun": True,
    }
    if not isinstance(cleanup, dict) or set(cleanup) != {
        *expected_cleanup,
        "daprNameResolutionFiles",
    }:
        errors.append("Live AppHost smoke cleanup is incomplete.")
    else:
        for key, expected in expected_cleanup.items():
            if not _exact(cleanup.get(key), expected):
                errors.append(f"Live AppHost smoke cleanup {key} is incomplete.")
        dapr = cleanup.get("daprNameResolutionFiles")
        expected_paths = [
            "src/Hexalith.FrontComposer.AppHost/nr.db",
            "src/Hexalith.FrontComposer.AppHost/nr.db-shm",
            "src/Hexalith.FrontComposer.AppHost/nr.db-wal",
        ]
        if not isinstance(dapr, dict) or set(dapr) != {
            "absentBeforeRun",
            "createdByInvocation",
            "removedAfterShutdown",
            "remainingAfterCleanup",
        }:
            errors.append("Live AppHost smoke Dapr-file cleanup accounting is malformed.")
        else:
            created = dapr.get("createdByInvocation")
            if (
                not _exact(dapr.get("absentBeforeRun"), expected_paths)
                or not isinstance(created, list)
                or any(not isinstance(path, str) for path in created)
                or created != sorted(set(created))
                or not set(created).issubset(expected_paths)
                or not _exact(dapr.get("removedAfterShutdown"), created)
                or not _exact(dapr.get("remainingAfterCleanup"), [])
            ):
                errors.append("Live AppHost smoke did not remove only its owned Dapr files.")


def _hash_entries(value: Any, expected_paths: set[str], label: str, errors: list[str]) -> dict[str, str]:
    if not isinstance(value, list):
        errors.append(f"{label} must be an array of path/SHA-256 bindings.")
        return {}
    bindings: dict[str, str] = {}
    for item in value:
        if not isinstance(item, dict) or set(item) != {"path", "sha256"}:
            errors.append(f"{label} contains a malformed binding.")
            continue
        relative = item.get("path")
        digest = item.get("sha256")
        if not isinstance(relative, str) or not _is_safe_relative_path(relative):
            errors.append(f"{label} contains an unsafe path.")
            continue
        if relative in bindings:
            errors.append(f"{label} repeats path: {relative}")
            continue
        if not isinstance(digest, str) or not SHA256_RE.fullmatch(digest):
            errors.append(f"{label} has an invalid SHA-256 for {relative}.")
            continue
        bindings[relative] = digest
    if set(bindings) != expected_paths:
        errors.append(f"{label} does not name the exact required files.")
    return bindings


def _validate_prior_capture(history_root: Path, errors: list[str]) -> None:
    if _path_has_symlink_component(history_root) or not history_root.is_dir():
        errors.append(f"Prior compatibility archive is missing or is a symlink: {history_root}")
        return
    actual: set[str] = set()
    for path in history_root.rglob("*"):
        relative = path.relative_to(history_root).as_posix()
        if path.is_symlink():
            errors.append(f"Prior compatibility archive contains a symlink: {relative}")
        elif path.is_file():
            actual.add(relative)
        elif path.is_dir():
            errors.append(f"Prior compatibility archive contains an undeclared directory: {relative}")
        else:
            errors.append(f"Prior compatibility archive contains a non-regular entry: {relative}")
    if actual != set(PRIOR_CAPTURE_SHA256):
        errors.append("Prior compatibility archive must contain exactly the dated three-file packet.")
    for name, expected_hash in PRIOR_CAPTURE_SHA256.items():
        path = history_root / name
        actual_hash = _sha256(path, errors, f"prior/{name}")
        if actual_hash != expected_hash:
            errors.append(f"Prior compatibility archive is not byte-identical: {name}")
        _scan_redaction(path, errors)


def _is_valid_durable_source(value: Any) -> bool:
    if not isinstance(value, str) or not value or any(character.isspace() for character in value):
        return False
    try:
        parsed = parse.urlsplit(value)
        _ = parsed.port
    except ValueError:
        return False
    return (
        parsed.scheme == "https"
        and bool(parsed.hostname)
        and parsed.username is None
        and parsed.password is None
    )


def _timestamp_not_future(
    value: datetime | None,
    label: str,
    issues: list[str],
    *,
    now: datetime | None = None,
) -> None:
    if value is not None and value > (now or datetime.now(timezone.utc)) + MAX_CLOCK_SKEW:
        issues.append(f"{label} is later than the allowed five-minute clock skew.")


def _validate_policy(
    policy: dict[str, Any],
    errors: list[str],
) -> tuple[dict[str, dict[str, set[str]]], datetime | None]:
    expected_oi18 = {
        "prerequisiteRoles": list(OI18_PREREQUISITE_ROLES),
        "removedRole": "eventstore-maintainer",
        "replacementRole": OI18_REPLACEMENT_ROLE,
        "effectiveRequiredRoles": [
            "frontcomposer-maintainer",
            OI18_REPLACEMENT_ROLE,
            "release-owner",
        ],
    }
    if set(policy) != {
        "schema",
        "frozenAt",
        "defaultRequiredRoles",
        "oi18Alternative",
        "assignments",
    }:
        errors.append("Approval policy does not contain the exact required fields.")
    if policy.get("schema") != "hexalith.frontcomposer.eventstore-runtime-approval-policy.v2":
        errors.append("Approval policy has an unexpected schema.")
    if not _exact(policy.get("defaultRequiredRoles"), list(DEFAULT_REQUIRED_ROLES)):
        errors.append("Approval policy default required roles are incorrect.")
    if not _exact(policy.get("oi18Alternative"), expected_oi18):
        errors.append("Approval policy OI-18 replacement rules are incorrect.")
    frozen_at = _parse_timestamp(policy.get("frozenAt"), "Approval policy frozenAt", errors)
    _timestamp_not_future(frozen_at, "Approval policy frozenAt", errors)

    assignments = policy.get("assignments")
    if not isinstance(assignments, list):
        errors.append("Approval policy assignments must be an array.")
        assignments = []
    repository_authority: dict[str, dict[str, set[str]]] = {}
    for assignment in assignments:
        if not isinstance(assignment, dict) or set(assignment) != {"role", "authorities"}:
            errors.append("Approval policy contains a malformed role assignment.")
            continue
        role = assignment.get("role")
        values = assignment.get("authorities")
        if (
            not isinstance(role, str)
            or role not in POLICY_ROLES
            or role in repository_authority
        ):
            errors.append("Approval policy contains a duplicate or unexpected role.")
            continue
        if not isinstance(values, list):
            errors.append(f"Approval policy authorities are malformed for role: {role}")
            continue
        role_authority: dict[str, set[str]] = {}
        for item in values:
            if not isinstance(item, dict) or set(item) != {"actor", "durableSources"}:
                errors.append(f"Approval policy contains a malformed authority for role: {role}")
                continue
            actor = item.get("actor")
            sources = item.get("durableSources")
            if (
                not isinstance(actor, str)
                or not ACTOR_RE.fullmatch(actor)
                or actor in role_authority
                or not isinstance(sources, list)
                or not sources
                or any(not _is_valid_durable_source(source) for source in sources)
                or len(set(sources)) != len(sources)
            ):
                errors.append(f"Approval policy actor/source authority is malformed for role: {role}")
                continue
            role_authority[actor] = set(sources)
        repository_authority[role] = role_authority
    if set(repository_authority) != set(POLICY_ROLES):
        errors.append("Approval policy assignments do not cover every governed role.")
    bootstrap = {
        role: {actor: set(sources) for actor, sources in actors.items()}
        for role, actors in APPROVAL_AUTHORITY_BOOTSTRAP.items()
    }
    if repository_authority != bootstrap:
        errors.append("Approval policy assignments differ from the validator-owned authority bootstrap.")
    return bootstrap, frozen_at


def _validate_roster(
    roster: dict[str, Any],
    policy_hash: str,
    authority: dict[str, dict[str, set[str]]],
    errors: list[str],
) -> tuple[dict[str, set[str]], datetime | None]:
    if set(roster) != {
        "schema",
        "frozenAt",
        "policySha256",
        "effectiveRequiredRoles",
        "assignments",
        "oi18",
    }:
        errors.append("Approval roster does not contain the exact required fields.")
    if roster.get("schema") != "hexalith.frontcomposer.eventstore-runtime-reviewer-roster.v2":
        errors.append("Approval roster has an unexpected schema.")
    if roster.get("policySha256") != policy_hash:
        errors.append("Approval roster does not bind the frozen approval policy.")
    frozen_at = _parse_timestamp(roster.get("frozenAt"), "Approval roster frozenAt", errors)
    _timestamp_not_future(frozen_at, "Approval roster frozenAt", errors)
    assignments = roster.get("assignments")
    if not isinstance(assignments, list):
        errors.append("Approval roster assignments must be an array.")
        assignments = []
    actors_by_role: dict[str, set[str]] = {}
    migration_roles = {*DEFAULT_REQUIRED_ROLES, OI18_REPLACEMENT_ROLE}
    for assignment in assignments:
        if not isinstance(assignment, dict) or set(assignment) != {"role", "actors"}:
            errors.append("Approval roster contains a malformed assignment.")
            continue
        role = assignment.get("role")
        actors = assignment.get("actors")
        if (
            not isinstance(role, str)
            or role not in migration_roles
            or role in actors_by_role
        ):
            errors.append("Approval roster contains a duplicate or unexpected role.")
            continue
        if (
            not isinstance(actors, list)
            or any(not isinstance(actor, str) or not ACTOR_RE.fullmatch(actor) for actor in actors)
            or len(set(actors)) != len(actors)
        ):
            errors.append(f"Approval roster actors are malformed for role: {role}")
            continue
        actor_set = set(actors)
        if not actor_set.issubset(set(authority.get(str(role), {}))):
            errors.append(f"Approval roster names an actor outside the frozen policy for role: {role}")
        actors_by_role[str(role)] = actor_set
    if set(actors_by_role) != migration_roles:
        errors.append("Approval roster assignments do not cover every migration role.")
    return actors_by_role, frozen_at


def _validate_source_authority(
    actor: Any,
    durable_source: Any,
    role: str,
    authority: dict[str, dict[str, set[str]]],
    issues: list[str],
) -> None:
    if not isinstance(actor, str) or not ACTOR_RE.fullmatch(actor):
        issues.append(f"Receipt actor is malformed for role: {role}")
        return
    if not _is_valid_durable_source(durable_source):
        issues.append(f"Receipt durable source is not a valid HTTPS URL for role: {role}")
        return
    if durable_source not in authority.get(role, {}).get(actor, set()):
        issues.append(f"Receipt actor/source is not authorized by the frozen policy for role: {role}")


def _validate_active_receipt(
    path: Path,
    expected_hash: str,
    role: str,
    actors: set[str],
    subject_hash: str,
    policy_hash: str,
    roster_hash: str,
    active_tuple: dict[str, str],
    runtime_inputs: dict[str, Any],
    evidence_files: list[dict[str, str]],
    effective_roles: list[str],
    oi18_subject_hash: str | None,
    authority: dict[str, dict[str, set[str]]],
    frozen_at: datetime | None,
    minimum_accepted_at: datetime | None,
    issues: list[str],
) -> None:
    actual_hash = _sha256(path, issues, path.name)
    if actual_hash != expected_hash:
        issues.append(f"Receipt SHA-256 does not match for required role: {role}")
    receipt = _read_json(path, issues, path.name)
    required_fields = {
        "schema",
        "subjectSha256",
        "policySha256",
        "rosterSha256",
        "activeTuple",
        "runtimeInputs",
        "evidenceFiles",
        "effectiveRequiredRoles",
        "oi18SubjectSha256",
        "actor",
        "role",
        "decision",
        "acceptedAt",
        "durableSource",
        "statement",
    }
    if set(receipt) != required_fields:
        issues.append(f"Receipt does not contain the exact required fields for role: {role}")
    expected = {
        "schema": "hexalith.frontcomposer.eventstore-runtime-approval-receipt.v2",
        "subjectSha256": subject_hash,
        "policySha256": policy_hash,
        "rosterSha256": roster_hash,
        "activeTuple": active_tuple,
        "runtimeInputs": runtime_inputs,
        "evidenceFiles": evidence_files,
        "effectiveRequiredRoles": effective_roles,
        "oi18SubjectSha256": oi18_subject_hash,
        "role": role,
        "decision": "approved",
    }
    for key, value in expected.items():
        if not _exact(receipt.get(key), value):
            issues.append(f"Receipt {key} does not bind the active subject for role: {role}")
    actor = receipt.get("actor")
    if not isinstance(actor, str) or actor not in actors:
        issues.append(f"Receipt actor is not assigned to required role: {role}")
    _validate_source_authority(
        actor,
        receipt.get("durableSource"),
        role,
        authority,
        issues,
    )
    if not _exact(receipt.get("statement"), ACTIVE_APPROVAL_STATEMENT):
        issues.append(f"Receipt statement is not the exact affirmative approval for role: {role}")
    accepted_at = _parse_timestamp(receipt.get("acceptedAt"), f"{role} receipt acceptedAt", issues)
    if frozen_at is None or accepted_at is None or accepted_at <= frozen_at:
        issues.append(f"Receipt does not postdate the frozen subject for role: {role}")
    if (
        role == OI18_REPLACEMENT_ROLE
        and minimum_accepted_at is not None
        and accepted_at is not None
        and accepted_at <= minimum_accepted_at
    ):
        issues.append("Replacement-role receipt does not postdate both OI-18 prerequisite approvals.")
    _timestamp_not_future(accepted_at, f"{role} receipt acceptedAt", issues)
    _scan_redaction(path, issues)


def _validate_transfer_receipt(
    path: Path,
    expected_hash: str,
    role: str,
    transfer_subject_hash: str,
    policy_hash: str,
    replacement_actors: list[str],
    authority: dict[str, dict[str, set[str]]],
    transfer_frozen_at: datetime | None,
    issues: list[str],
) -> datetime | None:
    if _sha256(path, issues, path.name) != expected_hash:
        issues.append(f"OI-18 {role} receipt SHA-256 does not match.")
    receipt = _read_json(path, issues, path.name)
    expected = {
        "schema": "hexalith.frontcomposer.eventstore-runtime-ownership-transfer-receipt.v2",
        "subjectSha256": transfer_subject_hash,
        "policySha256": policy_hash,
        "role": role,
        "decision": "ownership-transfer-approved",
        "removedRole": "eventstore-maintainer",
        "replacementRole": OI18_REPLACEMENT_ROLE,
        "replacementActors": replacement_actors,
    }
    if set(receipt) != {
        *expected,
        "actor",
        "acceptedAt",
        "durableSource",
        "statement",
    }:
        issues.append(f"OI-18 {role} receipt does not contain the exact required fields.")
    for key, value in expected.items():
        if not _exact(receipt.get(key), value):
            issues.append(f"OI-18 {role} receipt {key} is invalid.")
    _validate_source_authority(
        receipt.get("actor"),
        receipt.get("durableSource"),
        role,
        authority,
        issues,
    )
    if not _exact(receipt.get("statement"), OI18_APPROVAL_STATEMENT):
        issues.append(f"OI-18 {role} receipt statement is not the exact affirmative approval.")
    accepted_at = _parse_timestamp(receipt.get("acceptedAt"), f"OI-18 {role} acceptedAt", issues)
    if transfer_frozen_at is None or accepted_at is None or accepted_at <= transfer_frozen_at:
        issues.append(f"OI-18 {role} receipt does not postdate the transfer subject.")
    _timestamp_not_future(accepted_at, f"OI-18 {role} acceptedAt", issues)
    _scan_redaction(path, issues)
    return accepted_at


def _validate_oi18(
    oi18: Any,
    artifact_root: Path,
    policy_hash: str,
    active_tuple: dict[str, str],
    actors_by_role: dict[str, set[str]],
    authority: dict[str, dict[str, set[str]]],
    policy_frozen_at: datetime | None,
    roster_frozen_at: datetime | None,
    approval_issues: list[str],
) -> tuple[list[str], str | None, datetime | None, set[str]]:
    expected_default = list(DEFAULT_REQUIRED_ROLES)
    expected_alternative = [
        "frontcomposer-maintainer",
        OI18_REPLACEMENT_ROLE,
        "release-owner",
    ]
    tree_files: set[str] = set()
    if not isinstance(oi18, dict):
        approval_issues.append("OI-18 ownership-transfer state is malformed.")
        return expected_default, None, None, tree_files
    if oi18.get("status") == "open":
        expected_open = {
            "status": "open",
            "transferSubject": None,
            "productApprovalReceipt": None,
            "architectureApprovalReceipt": None,
            "replacementRole": None,
            "replacementActors": [],
        }
        if not _exact(oi18, expected_open):
            approval_issues.append("Open OI-18 state cannot claim replacement ownership or receipts.")
        return expected_default, None, None, tree_files
    if oi18.get("status") != "approved":
        approval_issues.append("OI-18 status must be open or approved.")
        return expected_default, None, None, tree_files
    if set(oi18) != {
        "status",
        "transferSubject",
        "productApprovalReceipt",
        "architectureApprovalReceipt",
        "replacementRole",
        "replacementActors",
    }:
        approval_issues.append("Approved OI-18 state does not contain the exact required fields.")
    if oi18.get("replacementRole") != OI18_REPLACEMENT_ROLE:
        approval_issues.append("OI-18 does not name the accountable FrontComposer replacement role.")
    replacement_actors = oi18.get("replacementActors")
    if (
        not isinstance(replacement_actors, list)
        or any(not isinstance(actor, str) or not ACTOR_RE.fullmatch(actor) for actor in replacement_actors)
        or len(set(replacement_actors)) != len(replacement_actors)
        or not replacement_actors
        or set(replacement_actors) != actors_by_role.get(OI18_REPLACEMENT_ROLE, set())
    ):
        approval_issues.append("OI-18 replacement actors do not match the accountable FrontComposer roster.")
        replacement_actors = []

    transfer_binding = oi18.get("transferSubject")
    if not isinstance(transfer_binding, dict) or set(transfer_binding) != {"path", "sha256"}:
        approval_issues.append("OI-18 transfer-subject binding is malformed.")
        return expected_alternative, None, None, tree_files
    relative = transfer_binding.get("path")
    digest = transfer_binding.get("sha256")
    if (
        not isinstance(relative, str)
        or not _is_safe_relative_path(relative)
        or not relative.startswith(f"{ACTIVE_EVIDENCE_ROOT}/oi18/")
        or not isinstance(digest, str)
        or not SHA256_RE.fullmatch(digest)
    ):
        approval_issues.append("OI-18 transfer-subject coordinate is malformed.")
        return expected_alternative, None, None, tree_files
    transfer_path = artifact_root / relative
    if _sha256(transfer_path, approval_issues, relative) != digest:
        approval_issues.append("OI-18 transfer-subject SHA-256 does not match.")
    transfer_subject = _read_json(transfer_path, approval_issues, relative)
    expected_transfer = {
        "schema": "hexalith.frontcomposer.eventstore-runtime-ownership-transfer-subject.v2",
        "policySha256": policy_hash,
        "activeTuple": active_tuple,
        "decision": "remove-eventstore-maintainer-and-substitute-accountable-frontcomposer-maintainer",
        "removedRole": "eventstore-maintainer",
        "replacementRole": OI18_REPLACEMENT_ROLE,
        "replacementActors": replacement_actors,
        "effectiveRequiredRoles": expected_alternative,
    }
    if set(transfer_subject) != {*expected_transfer, "frozenAt"}:
        approval_issues.append("OI-18 transfer subject does not contain the exact required fields.")
    for key, value in expected_transfer.items():
        if not _exact(transfer_subject.get(key), value):
            approval_issues.append(f"OI-18 transfer subject {key} is invalid.")
    transfer_frozen_at = _parse_timestamp(
        transfer_subject.get("frozenAt"), "OI-18 transfer subject frozenAt", approval_issues
    )
    _timestamp_not_future(transfer_frozen_at, "OI-18 transfer subject frozenAt", approval_issues)
    if (
        policy_frozen_at is None
        or transfer_frozen_at is None
        or transfer_frozen_at <= policy_frozen_at
    ):
        approval_issues.append("OI-18 transfer subject was not frozen after the approval policy.")
    tree_files.add(PurePosixPath(relative).relative_to(ACTIVE_EVIDENCE_ROOT).as_posix())

    accepted_times: list[datetime] = []
    for field, role in (
        ("productApprovalReceipt", "product-owner"),
        ("architectureApprovalReceipt", "architect"),
    ):
        binding = oi18.get(field)
        if not isinstance(binding, dict) or set(binding) != {"path", "sha256"}:
            approval_issues.append(f"OI-18 is missing the {role} approval receipt.")
            continue
        receipt_relative = binding.get("path")
        receipt_hash = binding.get("sha256")
        if (
            not isinstance(receipt_relative, str)
            or not _is_safe_relative_path(receipt_relative)
            or not receipt_relative.startswith(f"{ACTIVE_EVIDENCE_ROOT}/oi18/")
            or not isinstance(receipt_hash, str)
            or not SHA256_RE.fullmatch(receipt_hash)
        ):
            approval_issues.append(f"OI-18 {role} receipt coordinate is malformed.")
            continue
        receipt_path = artifact_root / receipt_relative
        accepted_at = _validate_transfer_receipt(
            receipt_path,
            receipt_hash,
            role,
            digest,
            policy_hash,
            replacement_actors,
            authority,
            transfer_frozen_at,
            approval_issues,
        )
        if accepted_at is not None:
            accepted_times.append(accepted_at)
        tree_files.add(PurePosixPath(receipt_relative).relative_to(ACTIVE_EVIDENCE_ROOT).as_posix())
    latest = max(accepted_times) if len(accepted_times) == len(OI18_PREREQUISITE_ROLES) else None
    if roster_frozen_at is None or latest is None or roster_frozen_at <= latest:
        approval_issues.append("Approval roster was not frozen after both OI-18 prerequisite approvals.")
    return expected_alternative, digest, latest, tree_files


def validate_active(
    identity_path: Path,
    evidence_root: Path,
    history_root: Path,
    pact_dir: Path,
    repository_root: Path,
) -> tuple[list[str], list[str], bool]:
    """Validate active identity/evidence and separately evaluate migration approval."""
    errors: list[str] = []
    approval_issues: list[str] = []
    artifact_root = repository_root
    (
        canonical_identity_path,
        canonical_evidence_root,
        canonical_history_root,
        canonical_pact_root,
    ) = _canonical_active_locations(repository_root)
    if not _same_canonical_path(identity_path, canonical_identity_path):
        errors.append("Active identity must use the fixed repository identity coordinate.")
    if not _same_canonical_path(evidence_root, canonical_evidence_root):
        errors.append("Active evidence root must resolve from the identity's canonical coordinate.")
    if not _same_canonical_path(history_root, canonical_history_root):
        errors.append("Prior evidence root must resolve from the identity's canonical coordinate.")
    if not _same_canonical_path(pact_dir, canonical_pact_root):
        errors.append("Active validation requires the canonical repository Pact directory.")
    identity_path = canonical_identity_path
    evidence_root = canonical_evidence_root
    history_root = canonical_history_root
    pact_dir = canonical_pact_root
    _validate_prior_capture(history_root, errors)

    if _path_has_symlink_component(identity_path):
        errors.append("Active identity path contains a symlink.")
    identity = _read_json(identity_path, errors, identity_path.name)
    _scan_redaction(identity_path, errors)
    if set(identity) != {
        "schema",
        "approvalRecord",
        "predecessor",
        "activeTuple",
        "frontComposerRevision",
        "runtimeInputs",
        "decision",
        "priorCompatibility",
        "activeEvidence",
        "approval",
        "submodulePointerChangedByApproval",
        "packageVersionChangedByApproval",
    }:
        errors.append("Active identity does not contain the exact v2 fields.")
    if identity.get("schema") != "hexalith.frontcomposer.eventstore-approved-runtime-identity.v2":
        errors.append("Active identity has an unexpected schema.")
    if identity.get("approvalRecord") != (
        "_bmad-output/implementation-artifacts/"
        "spec-11-25-current-eventstore-release-identity-and-evidence.md"
    ):
        errors.append("Active identity does not name Story 11.25 as its traceability record.")

    active_tuple = {
        "eventStoreSourceGitlink": ACTIVE_SOURCE_SHA,
        "eventStorePackageVersion": ACTIVE_VERSION,
        "buildsCatalogGitlink": ACTIVE_BUILDS_SHA,
    }
    if not _exact(identity.get("activeTuple"), active_tuple):
        errors.append("Active identity does not bind the exact approved current tuple.")
    if identity.get("submodulePointerChangedByApproval") is not False:
        errors.append("Active identity must not attribute a submodule-pointer change to approval.")
    if identity.get("packageVersionChangedByApproval") is not False:
        errors.append("Active identity must not attribute a package-version change to approval.")

    expected_predecessor = {
        "path": "_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json",
        "sha256": IDENTITY_V1_SHA256,
        "supersededForActiveReleaseSelectionOnly": True,
    }
    if not _exact(identity.get("predecessor"), expected_predecessor):
        errors.append("Active identity does not bind immutable identity v1 as its predecessor.")
    predecessor_path = artifact_root / expected_predecessor["path"]
    if _sha256(predecessor_path, errors, "identity v1") != IDENTITY_V1_SHA256:
        errors.append("Identity v1 is not byte-identical to its historical SHA-256.")

    manifest_path = artifact_root / RUNTIME_INPUT_MANIFEST_PATH
    manifest_hash = _sha256(manifest_path, errors, "frontcomposer-runtime-inputs.json")
    manifest, manifest_captured_at = _validate_runtime_input_manifest(
        manifest_path, repository_root, errors
    )
    runtime_binding = {
        "path": RUNTIME_INPUT_MANIFEST_PATH,
        "sha256": manifest_hash,
        "treeSha256": manifest.get("treeSha256"),
    }
    if not _exact(identity.get("runtimeInputs"), runtime_binding):
        errors.append("Active identity does not bind the sealed runtime-input manifest.")
    revision = identity.get("frontComposerRevision")
    if revision != manifest.get("capturedRevision"):
        errors.append("Active identity FrontComposer revision differs from the runtime-input capture.")

    prior = identity.get("priorCompatibility")
    expected_prior_tuple = {
        "eventStoreSourceGitlink": ACTIVE_SOURCE_SHA,
        "eventStorePackageVersion": ACTIVE_VERSION,
        "buildsCatalogGitlink": PRIOR_BUILDS_SHA,
    }
    if (
        not isinstance(prior, dict)
        or set(prior) != {"path", "capturedAt", "tuple", "files"}
        or prior.get("path") != PRIOR_EVIDENCE_ROOT
        or not _exact(prior.get("tuple"), expected_prior_tuple)
    ):
        errors.append("Active identity does not identify the dated prior compatibility tuple.")
        prior = {}
    if prior.get("capturedAt") != "2026-09-08T08:03:12.957893+00:00":
        errors.append("Active identity does not retain the dated prior compatibility capture time.")
    prior_bindings = _hash_entries(
        prior.get("files"),
        set(PRIOR_CAPTURE_SHA256),
        "Prior compatibility bindings",
        errors,
    )
    if prior_bindings != PRIOR_CAPTURE_SHA256:
        errors.append("Active identity prior compatibility hashes do not match the pinned archive.")

    if _path_has_symlink_component(evidence_root) or not evidence_root.is_dir():
        errors.append(f"Active identity evidence root is missing or is a symlink: {evidence_root}")
        return errors, ["Active identity evidence is unavailable."], False
    recapture_root = evidence_root / "recapture"
    expected_recapture_paths = {
        "apphost-smoke.json",
        "provider-verification.json",
        "run-evidence.json",
    }
    active_evidence = identity.get("activeEvidence")
    if (
        not isinstance(active_evidence, dict)
        or set(active_evidence) != {"path", "files"}
        or active_evidence.get("path") != ACTIVE_RECAPTURE_ROOT
    ):
        errors.append("Active identity recapture path is incorrect.")
        active_evidence = {}
    evidence_bindings = _hash_entries(
        active_evidence.get("files"),
        expected_recapture_paths,
        "Active recapture bindings",
        errors,
    )
    if not recapture_root.is_dir() or _path_has_symlink_component(recapture_root):
        errors.append("Active recapture directory is missing or contains a symlink component.")
    else:
        actual: set[str] = set()
        recapture_directories: set[str] = set()
        for path in recapture_root.rglob("*"):
            relative = path.relative_to(recapture_root).as_posix()
            if path.is_symlink():
                errors.append("Active recapture contains a symlink: " + relative)
            elif path.is_file():
                actual.add(relative)
            elif path.is_dir():
                recapture_directories.add(relative)
            else:
                errors.append("Active recapture contains a non-regular entry: " + relative)
        if actual != expected_recapture_paths:
            errors.append("Active recapture must contain exactly the provider, receipt, and AppHost reports.")
        if recapture_directories:
            errors.append("Active recapture contains undeclared directories: " + ", ".join(sorted(recapture_directories)))
        for relative, expected_hash in evidence_bindings.items():
            path = recapture_root / relative
            if _sha256(path, errors, f"active/{relative}") != expected_hash:
                errors.append(f"Active recapture SHA-256 mismatch: {relative}")
            _scan_redaction(path, errors)

    decision_binding = identity.get("decision")
    expected_decision_path = f"{ACTIVE_EVIDENCE_ROOT}/recapture-decision.json"
    decision_path = artifact_root / expected_decision_path
    decision_hash = _sha256(decision_path, errors, "recapture-decision.json")
    if not _exact(decision_binding, {"path": expected_decision_path, "sha256": decision_hash}):
        errors.append("Active identity decision binding is malformed.")
    decision = _read_json(decision_path, errors, "recapture-decision.json")
    _scan_redaction(decision_path, errors)
    decision_source = {
        "path": "_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-11.md",
        "sha256": "ab84473f53fa80616d1bf3ecb4004889d81f1de5984a7d09d5dd1bb49b85a4aa",
    }
    expected_decision = {
        "schema": "hexalith.frontcomposer.eventstore-runtime-recapture-decision.v2",
        "source": decision_source,
        "activeTuple": active_tuple,
        "frontComposerRevision": revision,
        "runtimeInputs": runtime_binding,
        "evidenceFiles": active_evidence.get("files"),
        "strategy": "exact-tuple-recapture",
        "semanticCompatibilityExceptionApproved": False,
        "rollbackApproved": False,
        "submodulePointerChangedByDecision": False,
        "packageVersionChangedByDecision": False,
    }
    if set(decision) != {*expected_decision, "recordedAt"}:
        errors.append("Active recapture decision does not contain the exact permitted fields.")
    for key, value in expected_decision.items():
        if not _exact(decision.get(key), value):
            errors.append(f"Active recapture decision {key} is incorrect.")
    decision_recorded = _parse_timestamp(
        decision.get("recordedAt"), "Active recapture decision recordedAt", errors
    )
    _timestamp_not_future(decision_recorded, "Active recapture decision recordedAt", errors)
    if _sha256(
        artifact_root / decision_source["path"], errors, "course-correction source"
    ) != decision_source["sha256"]:
        errors.append("Approved course-correction source SHA-256 does not match the decision binding.")

    policy_path = artifact_root / APPROVAL_POLICY_PATH
    policy_hash = _sha256(policy_path, errors, "approval-policy.json")
    policy = _read_json(policy_path, errors, "approval-policy.json")
    _scan_redaction(policy_path, errors)
    authority, policy_frozen_at = _validate_policy(policy, errors)

    roster_path = artifact_root / APPROVAL_ROSTER_PATH
    roster_hash = _sha256(roster_path, errors, "reviewer-roster.json")
    roster = _read_json(roster_path, errors, "reviewer-roster.json")
    _scan_redaction(roster_path, errors)
    actors_by_role, roster_frozen_at = _validate_roster(
        roster, policy_hash, authority, errors
    )
    if (
        policy_frozen_at is None
        or roster_frozen_at is None
        or roster_frozen_at <= policy_frozen_at
    ):
        errors.append("Approval roster was not frozen after the approval policy.")

    effective_roles, oi18_subject_hash, transfer_latest, oi18_tree_files = _validate_oi18(
        roster.get("oi18"),
        artifact_root,
        policy_hash,
        active_tuple,
        actors_by_role,
        authority,
        policy_frozen_at,
        roster_frozen_at,
        approval_issues,
    )
    if not _exact(roster.get("effectiveRequiredRoles"), effective_roles):
        approval_issues.append("Approval roster effective required roles contradict OI-18 state.")

    subject_path = artifact_root / APPROVAL_SUBJECT_PATH
    subject_hash = _sha256(subject_path, errors, "approval-subject.json")
    subject = _read_json(subject_path, errors, "approval-subject.json")
    _scan_redaction(subject_path, errors)
    expected_subject = {
        "schema": "hexalith.frontcomposer.eventstore-runtime-approval-subject.v2",
        "predecessor": expected_predecessor,
        "activeTuple": active_tuple,
        "frontComposerRevision": revision,
        "runtimeInputs": runtime_binding,
        "decision": {"path": expected_decision_path, "sha256": decision_hash},
        "evidenceFiles": active_evidence.get("files"),
        "policy": {"path": APPROVAL_POLICY_PATH, "sha256": policy_hash},
        "roster": {"path": APPROVAL_ROSTER_PATH, "sha256": roster_hash},
        "effectiveRequiredRoles": effective_roles,
        "oi18": roster.get("oi18"),
    }
    if set(subject) != {*expected_subject, "frozenAt"}:
        errors.append("Active approval subject does not contain the exact required fields.")
    for key, value in expected_subject.items():
        if not _exact(subject.get(key), value):
            errors.append(f"Active approval subject {key} does not bind the exact candidate.")
    subject_frozen_at = _parse_timestamp(
        subject.get("frozenAt"), "Active approval subject frozenAt", errors
    )
    _timestamp_not_future(subject_frozen_at, "Active approval subject frozenAt", errors)
    for timestamp, label in (
        (decision_recorded, "recapture decision"),
        (policy_frozen_at, "approval policy"),
        (roster_frozen_at, "approval roster"),
    ):
        if (
            timestamp is None
            or subject_frozen_at is None
            or subject_frozen_at <= timestamp
        ):
            errors.append(f"Active approval subject was not frozen after the {label}.")

    provider_receipt = _read_json(
        recapture_root / "run-evidence.json", errors, "active/run-evidence.json"
    )
    provider_report = _read_json(
        recapture_root / "provider-verification.json", errors, "active/provider-verification.json"
    )
    apphost_report = _read_json(
        recapture_root / "apphost-smoke.json", errors, "active/apphost-smoke.json"
    )
    provider_receipt_captured_at = _parse_timestamp(
        provider_receipt.get("capturedAt"), "Active provider capturedAt", errors
    )
    provider_receipt_completed_at = _parse_timestamp(
        provider_receipt.get("completedAt"), "Active provider completedAt", errors
    )
    provider_timing = provider_report.get("timing")
    provider_run = provider_timing.get("run") if isinstance(provider_timing, dict) else None
    provider_started_at = _parse_timestamp(
        provider_run.get("startedAt") if isinstance(provider_run, dict) else None,
        "Active provider run startedAt",
        errors,
    )
    provider_completed_at = _parse_timestamp(
        provider_run.get("completedAt") if isinstance(provider_run, dict) else None,
        "Active provider run completedAt",
        errors,
    )
    apphost_captured_at = _parse_timestamp(
        apphost_report.get("capturedAt"), "Active AppHost capturedAt", errors
    )
    apphost_completed_at = _parse_timestamp(
        apphost_report.get("completedAt"), "Active AppHost completedAt", errors
    )
    for timestamp, label in (
        (manifest_captured_at, "runtime-input manifest"),
        (provider_started_at, "provider run start"),
        (provider_completed_at, "provider run completion"),
        (provider_receipt_completed_at, "provider receipt completion"),
        (provider_receipt_captured_at, "provider receipt capture"),
        (apphost_captured_at, "AppHost capture start"),
        (apphost_completed_at, "AppHost capture completion"),
    ):
        _timestamp_not_future(timestamp, f"Active {label} timestamp", errors)
    if (
        manifest_captured_at is None
        or provider_started_at is None
        or manifest_captured_at >= provider_started_at
    ):
        errors.append("Active runtime-input manifest does not predate provider execution.")
    if (
        manifest_captured_at is None
        or apphost_captured_at is None
        or manifest_captured_at >= apphost_captured_at
    ):
        errors.append("Active runtime-input manifest does not predate AppHost execution.")
    if provider_completed_at != provider_receipt_completed_at:
        errors.append("Active provider receipt does not bind the provider run completion timestamp.")
    if (
        provider_completed_at is None
        or provider_receipt_captured_at is None
        or provider_receipt_captured_at < provider_completed_at
    ):
        errors.append("Active provider receipt was captured before provider completion.")
    if (
        apphost_captured_at is None
        or apphost_completed_at is None
        or apphost_completed_at < apphost_captured_at
    ):
        errors.append("Active AppHost completion does not follow its capture start.")
    for timestamp, label in (
        (provider_completed_at, "provider run completion"),
        (provider_receipt_captured_at, "provider receipt capture"),
        (apphost_completed_at, "AppHost capture completion"),
    ):
        if timestamp is None or decision_recorded is None or timestamp >= decision_recorded:
            errors.append(f"Active {label} does not predate the recapture decision.")

    approval = identity.get("approval")
    claimed = isinstance(approval, dict) and approval.get("migrationApprovalClaimed") is True
    if not isinstance(approval, dict):
        errors.append("Active identity approval state is malformed.")
        approval = {}
    if set(approval) != {
        "policy",
        "roster",
        "subject",
        "receiptDirectory",
        "effectiveRequiredRoles",
        "receipts",
        "migrationApprovalClaimed",
    }:
        errors.append("Active identity approval state does not contain the exact required fields.")
    if not isinstance(approval.get("migrationApprovalClaimed"), bool):
        errors.append("Active identity migrationApprovalClaimed must be a boolean.")
    if not _exact(approval.get("policy"), {"path": APPROVAL_POLICY_PATH, "sha256": policy_hash}):
        errors.append("Active identity approval-policy binding is incorrect.")
    if not _exact(approval.get("roster"), {"path": APPROVAL_ROSTER_PATH, "sha256": roster_hash}):
        errors.append("Active identity approval-roster binding is incorrect.")
    if not _exact(approval.get("subject"), {"path": APPROVAL_SUBJECT_PATH, "sha256": subject_hash}):
        errors.append("Active identity approval-subject binding is incorrect.")
    if approval.get("receiptDirectory") != APPROVAL_RECEIPT_ROOT:
        errors.append("Active identity approval receipt directory is incorrect.")
    if not _exact(approval.get("effectiveRequiredRoles"), effective_roles):
        errors.append("Active identity effective required roles contradict the frozen subject.")

    for role in effective_roles:
        actors = actors_by_role.get(role, set())
        if not actors:
            approval_issues.append(f"Missing named actor for required role: {role}")
    selected_actors: dict[str, str] = {}
    for role in effective_roles:
        for actor in actors_by_role.get(role, set()):
            previous = selected_actors.get(actor)
            if previous is not None and previous != role:
                approval_issues.append(
                    f"Required roles must have distinct actors: {previous} and {role}"
                )
            selected_actors[actor] = role

    receipts = approval.get("receipts")
    if not isinstance(receipts, list):
        errors.append("Active identity receipts must be an array.")
        receipts = []
    receipts_by_role: dict[str, dict[str, str]] = {}
    for binding in receipts:
        if not isinstance(binding, dict) or set(binding) != {"role", "path", "sha256"}:
            approval_issues.append("Active identity contains a malformed approval-receipt binding.")
            continue
        role = binding.get("role")
        relative = binding.get("path")
        digest = binding.get("sha256")
        expected_relative = (
            f"{APPROVAL_RECEIPT_ROOT}/{role}.json" if isinstance(role, str) else ""
        )
        if (
            role not in effective_roles
            or role in receipts_by_role
            or relative != expected_relative
            or not isinstance(digest, str)
            or not SHA256_RE.fullmatch(digest)
        ):
            approval_issues.append(
                f"Approval receipt binding is malformed for required role: {role}"
            )
            continue
        receipts_by_role[str(role)] = {"path": str(relative), "sha256": digest}
    for role in effective_roles:
        binding = receipts_by_role.get(role)
        if binding is None:
            approval_issues.append(f"Missing valid receipt for required role: {role}")
            continue
        _validate_active_receipt(
            artifact_root / binding["path"],
            binding["sha256"],
            role,
            actors_by_role.get(role, set()),
            subject_hash,
            policy_hash,
            roster_hash,
            active_tuple,
            runtime_binding,
            active_evidence.get("files", []),
            effective_roles,
            oi18_subject_hash,
            authority,
            subject_frozen_at,
            transfer_latest,
            approval_issues,
        )

    expected_tree_files = {
        "approval-policy.json",
        "approval-subject.json",
        "frontcomposer-runtime-inputs.json",
        "recapture-decision.json",
        "reviewer-roster.json",
        *{f"recapture/{name}" for name in expected_recapture_paths},
        *oi18_tree_files,
    }
    for binding in receipts_by_role.values():
        expected_tree_files.add(
            PurePosixPath(binding["path"]).relative_to(ACTIVE_EVIDENCE_ROOT).as_posix()
        )
    actual_tree_files: set[str] = set()
    actual_tree_directories: set[str] = set()
    total_bytes = 0
    for path in evidence_root.rglob("*"):
        relative = path.relative_to(evidence_root).as_posix()
        if path.is_symlink():
            errors.append(f"Active evidence tree contains a symlink: {relative}")
        elif path.is_file():
            actual_tree_files.add(relative)
            try:
                size = path.stat().st_size
            except OSError:
                errors.append(f"Active evidence file is unreadable: {relative}")
                continue
            total_bytes += size
            _scan_redaction(path, errors)
            if size <= 0 or size > MAX_FILE_BYTES:
                errors.append(
                    f"Active evidence file is empty or exceeds {MAX_FILE_BYTES} bytes: {relative}"
                )
        elif path.is_dir():
            actual_tree_directories.add(relative)
        else:
            errors.append(f"Active evidence tree contains a non-regular entry: {relative}")
    if actual_tree_files != expected_tree_files:
        errors.append("Active evidence tree contains missing or undeclared files.")
    expected_tree_directories = {
        parent.as_posix()
        for relative in expected_tree_files
        for parent in PurePosixPath(relative).parents
        if parent.as_posix() != "."
    }
    if actual_tree_directories != expected_tree_directories:
        errors.append("Active evidence tree contains missing or undeclared directories.")
    if total_bytes > MAX_TOTAL_BYTES:
        errors.append(f"Active evidence tree exceeds the {MAX_TOTAL_BYTES}-byte bound.")

    if claimed and approval_issues:
        errors.extend(f"Migration approval claimed but {issue}" for issue in approval_issues)
    if not claimed and not approval_issues:
        errors.append("Migration approval is complete but migrationApprovalClaimed remains false.")

    repository_provenance = _live_provenance(repository_root, errors)
    expected_repository = {
        "sourceSha": ACTIVE_SOURCE_SHA,
        "releaseVersion": ACTIVE_VERSION,
        "buildsSha": ACTIVE_BUILDS_SHA,
        "releaseInventorySha256": INVENTORY_SHA256,
        "runtimeInputTreeSha256": manifest.get("treeSha256"),
    }
    observed_repository = {
        key: repository_provenance.get(key) for key in expected_repository
    }
    if not _exact(observed_repository, expected_repository):
        errors.append("Current repository dependency/runtime provenance differs from active identity v2.")

    captured_provenance = {
        **expected_repository,
        "frontComposerRevision": revision,
    }
    if recapture_root.is_dir():
        _validate_live_provider(recapture_root, pact_dir, captured_provenance, errors)
        _validate_live_apphost(recapture_root, repository_root, captured_provenance, errors)
    return errors, approval_issues, claimed

def validate_live(evidence_root: Path, pact_dir: Path, repository_root: Path) -> list[str]:
    """Validate current provider and AppHost evidence independently of frozen Story 11.24 bytes."""
    errors: list[str] = []
    for path, label in ((evidence_root, "Live evidence root"), (pact_dir, "Pact directory"), (repository_root, "Repository root")):
        if _path_has_symlink_component(path) or not path.is_dir():
            return [f"{label} is missing or is a symlink: {path}"]
    required = {"provider-verification.json", "run-evidence.json", "apphost-smoke.json"}
    actual_files: set[str] = set()
    actual_directories: set[str] = set()
    for item in evidence_root.rglob("*"):
        relative = item.relative_to(evidence_root).as_posix()
        if item.is_symlink():
            errors.append(f"Live evidence root contains a symlink: {relative}")
        elif item.is_file():
            actual_files.add(relative)
            _scan_redaction(item, errors)
        elif item.is_dir():
            actual_directories.add(relative)
        else:
            errors.append(f"Live evidence root contains a non-regular entry: {relative}")
    if actual_files != required or actual_directories:
        errors.append("Live evidence root must contain exactly provider-verification.json, run-evidence.json, and apphost-smoke.json.")
    provenance = _live_provenance(repository_root, errors)
    _validate_live_provider(evidence_root, pact_dir, provenance, errors)
    _validate_live_apphost(evidence_root, repository_root, provenance, errors)
    return errors


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
    _validate_provider_report(evidence_root, snapshot_hashes, errors)
    _validate_apphost_smoke(evidence_root, errors)
    _validate_release_restore(evidence_root, errors)
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--evidence-root", type=Path)
    parser.add_argument("--live-evidence-root", type=Path)
    parser.add_argument("--active-identity", type=Path)
    parser.add_argument("--active-evidence-root", type=Path)
    parser.add_argument("--history-evidence-root", type=Path)
    parser.add_argument("--pact-dir", required=True, type=Path)
    parser.add_argument("--repository-root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--write-live-receipt", action="store_true")
    parser.add_argument("--write-runtime-input-manifest", action="store_true")
    parser.add_argument("--runtime-input-manifest-output", type=Path)
    parser.add_argument("--runtime-input-manifest", type=Path)
    parser.add_argument("--runtime-input-captured-at")
    args = parser.parse_args(argv)
    if (
        args.evidence_root is None
        and args.live_evidence_root is None
        and args.active_identity is None
        and not args.write_runtime_input_manifest
    ):
        parser.error("at least one evidence or active-identity input is required")
    active_arguments = (args.active_identity, args.active_evidence_root, args.history_evidence_root)
    if any(value is not None for value in active_arguments) and not all(value is not None for value in active_arguments):
        parser.error("active validation requires --active-identity, --active-evidence-root, and --history-evidence-root")
    errors: list[str] = []
    approval_issues: list[str] = []
    approval_claimed = False
    if args.write_runtime_input_manifest:
        if args.runtime_input_manifest_output is None:
            parser.error("--write-runtime-input-manifest requires --runtime-input-manifest-output")
        errors.extend(
            write_runtime_input_manifest(
                args.runtime_input_manifest_output.absolute(),
                args.repository_root.absolute(),
                captured_at=args.runtime_input_captured_at,
            )
        )
    if args.write_live_receipt:
        if args.live_evidence_root is None:
            parser.error("--write-live-receipt requires --live-evidence-root")
        if args.runtime_input_manifest is None:
            parser.error("--write-live-receipt requires --runtime-input-manifest")
        errors.extend(
            write_live_receipt(
                args.live_evidence_root.absolute(),
                args.repository_root.absolute(),
                runtime_input_manifest_path=args.runtime_input_manifest.absolute(),
                pact_dir=args.pact_dir.absolute(),
            )
        )
    if args.evidence_root is not None:
        errors.extend(validate(args.evidence_root.absolute(), args.pact_dir.absolute()))
    if args.live_evidence_root is not None and not args.write_live_receipt:
        errors.extend(validate_live(
            args.live_evidence_root.absolute(),
            args.pact_dir.absolute(),
            args.repository_root.absolute(),
        ))
    if args.active_identity is not None:
        active_errors, approval_issues, approval_claimed = validate_active(
            args.active_identity.absolute(),
            args.active_evidence_root.absolute(),
            args.history_evidence_root.absolute(),
            args.pact_dir.absolute(),
            args.repository_root.absolute(),
        )
        errors.extend(active_errors)
    if errors:
        for error in errors:
            print(f"EventStore runtime evidence error: {error}", file=sys.stderr)
        return 1
    print("EventStore runtime evidence operation completed successfully.")
    if args.active_identity is not None:
        print(f"EventStore runtime approval: {'APPROVED' if approval_claimed else 'OPEN'}")
        if not approval_claimed:
            for issue in dict.fromkeys(approval_issues):
                print(f"EventStore runtime approval issue: {issue}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
