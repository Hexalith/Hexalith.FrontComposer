#!/usr/bin/env python3
"""Pure compatibility-lifecycle policy for FrontComposer release candidates."""

from __future__ import annotations

import json
import pathlib
import re
import xml.etree.ElementTree as ET
from collections.abc import Mapping, Sequence


COMPATIBILITY_SUPPRESSIONS_SCHEMA_VERSION = "2.0"
PUBLISHED_BASELINE_VERSION = "4.1.1"
LIFECYCLE_TOKEN = re.compile(
    r"^v(?P<major>0|[1-9][0-9]*)\.(?P<minor>0|[1-9][0-9]*)$"
)
SEMVER_IDENTIFIER = r"(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)"
CANDIDATE_VERSION = re.compile(
    r"^(?P<major>0|[1-9][0-9]*)\.(?P<minor>0|[1-9][0-9]*)\."
    r"(?P<patch>0|[1-9][0-9]*)"
    rf"(?:-(?:{SEMVER_IDENTIFIER})(?:\.(?:{SEMVER_IDENTIFIER}))*)?"
    r"(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$"
)
ASSEMBLY_PATH = re.compile(r"^lib/(?P<tfm>[^/]+)/(?P<assembly>[^/]+\.dll)$")
DIAGNOSTIC_ID = re.compile(r"^CP[0-9]{4}$")
APPROVED_SUPPRESSION_REASON = "intentional-major-break"

DEFAULT_BASELINE_PATHS = (
    "Directory.Build.targets",
    "src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj",
)
DEFAULT_SUPPRESSION_FILES = {
    "Hexalith.FrontComposer.Contracts": (
        "src/Hexalith.FrontComposer.Contracts/CompatibilitySuppressions.xml"
    ),
    "Hexalith.FrontComposer.Mcp": (
        "src/Hexalith.FrontComposer.Mcp/CompatibilitySuppressions.xml"
    ),
    "Hexalith.FrontComposer.Shell": (
        "src/Hexalith.FrontComposer.Shell/CompatibilitySuppressions.xml"
    ),
}
REQUIRED_SUPPRESSION_FIELDS = (
    "package",
    "tfm",
    "oldSignature",
    "newState",
    "apiCompatDiagnosticId",
    "targetRelease",
    "reviewerRationale",
    "ownerStory",
    "expiresAfter",
    "reason",
)


class ReleaseCompatibilityError(ValueError):
    """Raised when the checked-in release compatibility policy is stale or malformed."""


def candidate_release_line(value: str, field: str = "--version") -> tuple[int, int]:
    """Parse a strict SemVer release candidate into its major/minor line."""
    match = CANDIDATE_VERSION.fullmatch(value) if isinstance(value, str) else None
    if match is None:
        raise ReleaseCompatibilityError(
            f"{field} '{value}' must be strict SemVer major.minor.patch with optional "
            "prerelease and build metadata"
        )
    return int(match.group("major")), int(match.group("minor"))


def lifecycle_line(value: str, field: str) -> tuple[int, int]:
    """Parse an exact vMAJOR.MINOR compatibility lifecycle token."""
    match = LIFECYCLE_TOKEN.fullmatch(value) if isinstance(value, str) else None
    if match is None:
        raise ReleaseCompatibilityError(f"{field} '{value}' must be a vMAJOR.MINOR lifecycle token")
    return int(match.group("major")), int(match.group("minor"))


def release_properties(version: str) -> list[str]:
    """Return the immutable version and compatibility properties for release build/pack."""
    candidate_release_line(version)
    return [
        f"-p:Version={version}",
        f"-p:PackageVersion={version}",
        "-p:ContinuousIntegrationBuild=true",
        "-p:EnableFrontComposerPackageValidation=true",
        f"-p:FrontComposerPackageValidationBaselineVersion={PUBLISHED_BASELINE_VERSION}",
        "-p:FrontComposerPackageValidationSkipBaseline=false",
    ]


def xml_values(path: pathlib.Path, local_name: str) -> list[str]:
    """Return non-empty values for one XML local name."""
    root = _parse_xml(path)
    return [
        (element.text or "").strip()
        for element in root.iter()
        if element.tag.rsplit("}", 1)[-1] == local_name and (element.text or "").strip()
    ]


def validate_release_policy(
    root: pathlib.Path,
    version: str,
    *,
    suppressions_path: pathlib.Path | None = None,
    baseline_paths: Sequence[pathlib.Path] | None = None,
    suppression_files: Mapping[str, pathlib.Path] | None = None,
    match_candidate_release: bool = True,
) -> str | None:
    """Validate the release line, suppression lifecycle, baseline, and XML parity."""
    root = root.resolve()
    actual_line = candidate_release_line(version)
    ledger_path = _resolve(
        root,
        suppressions_path or pathlib.Path("docs/diagnostics/compatibility-suppressions.json"),
    )
    configured_baselines = tuple(
        _resolve(root, path)
        for path in (baseline_paths or tuple(pathlib.Path(path) for path in DEFAULT_BASELINE_PATHS))
    )
    configured_suppressions = {
        package: _resolve(root, path)
        for package, path in (
            suppression_files
            or {package: pathlib.Path(path) for package, path in DEFAULT_SUPPRESSION_FILES.items()}
        ).items()
    }

    payload = _read_ledger(ledger_path)
    if payload.get("schemaVersion") != COMPATIBILITY_SUPPRESSIONS_SCHEMA_VERSION:
        raise ReleaseCompatibilityError(
            f"{ledger_path}: compatibility suppression schemaVersion must be "
            f"{COMPATIBILITY_SUPPRESSIONS_SCHEMA_VERSION}"
        )

    current_value = payload.get("currentRelease")
    if not isinstance(current_value, str):
        raise ReleaseCompatibilityError(f"{ledger_path}: currentRelease must be a vMAJOR.MINOR token")
    rows = payload.get("suppressions")
    if not isinstance(rows, list):
        raise ReleaseCompatibilityError(f"{ledger_path}: suppressions must be an array")

    current_line = lifecycle_line(current_value, "currentRelease")
    policy_line = actual_line if match_candidate_release else current_line
    policy_label = (
        f"--version v{actual_line[0]}.{actual_line[1]}"
        if match_candidate_release
        else f"currentRelease {current_value}"
    )
    tracked_rows = _validate_rows(ledger_path, rows, policy_line, policy_label)
    if match_candidate_release and actual_line != current_line:
        raise ReleaseCompatibilityError(
            f"{ledger_path}: --version release line v{actual_line[0]}.{actual_line[1]} "
            f"does not match currentRelease {current_value}"
        )

    for path in configured_baselines:
        values = xml_values(path, "FrontComposerPackageValidationBaselineVersion")
        if values != [PUBLISHED_BASELINE_VERSION]:
            found = ", ".join(values) if values else "<missing>"
            raise ReleaseCompatibilityError(
                f"{path}: package-validation baseline must be the verified published "
                f"{PUBLISHED_BASELINE_VERSION}; found '{found}'"
            )

    xml_rows = _suppression_xml_rows(configured_suppressions)
    if xml_rows != tracked_rows:
        missing = sorted(tracked_rows - xml_rows)
        stale = sorted(xml_rows - tracked_rows)
        raise ReleaseCompatibilityError(
            "compatibility suppression ledger/XML mismatch: "
            f"missing XML rows={missing or '<none>'}; stale XML rows={stale or '<none>'}"
        )

    if not match_candidate_release:
        return None
    return f"v{actual_line[0]}.{actual_line[1]}"


def _resolve(root: pathlib.Path, path: pathlib.Path) -> pathlib.Path:
    return path.resolve() if path.is_absolute() else (root / path).resolve()


def _read_ledger(path: pathlib.Path) -> dict[str, object]:
    try:
        with path.open("r", encoding="utf-8") as handle:
            payload = json.load(handle)
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise ReleaseCompatibilityError(
            f"{path}: cannot read compatibility suppression ledger: {error}"
        ) from error
    if not isinstance(payload, dict):
        raise ReleaseCompatibilityError(f"{path}: compatibility suppression ledger must be an object")
    return payload


def _validate_rows(
    ledger_path: pathlib.Path,
    rows: list[object],
    policy_line: tuple[int, int],
    policy_label: str,
) -> set[str]:
    tracked: set[str] = set()
    for index, value in enumerate(rows):
        if not isinstance(value, dict):
            raise ReleaseCompatibilityError(f"{ledger_path}: suppression row {index} must be an object")
        missing = [
            field
            for field in REQUIRED_SUPPRESSION_FIELDS
            if not isinstance(value.get(field), str) or not str(value[field]).strip()
        ]
        if missing:
            raise ReleaseCompatibilityError(
                f"{ledger_path}: suppression row {index} has missing or empty fields: {missing}"
            )
        package = str(value["package"])
        tfm = str(value["tfm"])
        old_signature = str(value["oldSignature"])
        diagnostic = str(value["apiCompatDiagnosticId"])
        if any(marker in package or marker in tfm for marker in ("*", "?")):
            raise ReleaseCompatibilityError(
                f"{ledger_path}: suppression row {index} must not use wildcard package or TFM scope"
            )
        if any(marker in old_signature for marker in ("*", "?")):
            raise ReleaseCompatibilityError(
                f"{ledger_path}: suppression row {index} oldSignature must identify one exact API"
            )
        if any(marker in diagnostic for marker in ("*", "?")) or DIAGNOSTIC_ID.fullmatch(diagnostic) is None:
            raise ReleaseCompatibilityError(
                f"{ledger_path}: suppression row {index} apiCompatDiagnosticId '{diagnostic}' "
                "must match CP followed by four digits without wildcards"
            )
        if value["reason"] != APPROVED_SUPPRESSION_REASON:
            raise ReleaseCompatibilityError(
                f"{ledger_path}: suppression row {index} reason must be "
                f"'{APPROVED_SUPPRESSION_REASON}'"
            )
        target_value = str(value["targetRelease"])
        expiry_value = str(value["expiresAfter"])
        target_line = lifecycle_line(target_value, f"suppressions[{index}].targetRelease")
        expiry_line = lifecycle_line(expiry_value, f"suppressions[{index}].expiresAfter")
        if policy_line < target_line:
            raise ReleaseCompatibilityError(
                f"{ledger_path}: suppression row {index} targetRelease {target_value} "
                f"is later than {policy_label}"
            )
        if expiry_line <= target_line:
            raise ReleaseCompatibilityError(
                f"{ledger_path}: suppression row {index} expiresAfter {expiry_value} "
                f"must be later than targetRelease {target_value}"
            )
        if policy_line >= expiry_line:
            raise ReleaseCompatibilityError(
                f"{ledger_path}: suppression row {index} expiresAfter {expiry_value} "
                f"has been reached by {policy_label}"
            )
        key = f"{package}|{tfm}|{diagnostic}|{old_signature}"
        if key in tracked:
            raise ReleaseCompatibilityError(f"{ledger_path}: duplicate suppression row '{key}'")
        tracked.add(key)
    return tracked


def _parse_xml(path: pathlib.Path) -> ET.Element:
    if not path.is_file():
        raise ReleaseCompatibilityError(f"{path}: required compatibility policy XML file is missing")
    try:
        return ET.parse(path).getroot()
    except ET.ParseError as error:
        raise ReleaseCompatibilityError(f"{path}: invalid XML: {error}") from error
    except (OSError, UnicodeError) as error:
        raise ReleaseCompatibilityError(f"{path}: cannot read compatibility policy XML: {error}") from error


def _suppression_xml_rows(suppression_files: Mapping[str, pathlib.Path]) -> set[str]:
    rows: set[str] = set()
    for package, path in suppression_files.items():
        root = _parse_xml(path)
        root_name = root.tag.rsplit("}", 1)[-1]
        if root_name != "Suppressions":
            raise ReleaseCompatibilityError(f"{path}: XML root must be Suppressions")
        unknown_rows = [
            child.tag.rsplit("}", 1)[-1]
            for child in root
            if child.tag.rsplit("}", 1)[-1] != "Suppression"
        ]
        if unknown_rows:
            raise ReleaseCompatibilityError(
                f"{path}: Suppressions contains unknown child elements: {unknown_rows}"
            )
        for index, element in enumerate(root):
            required = ("DiagnosticId", "Target", "Left", "Right", "IsBaselineSuppression")
            names = [child.tag.rsplit("}", 1)[-1] for child in element]
            duplicates = sorted({name for name in names if names.count(name) > 1})
            unknown = sorted(set(names) - set(required))
            if duplicates:
                raise ReleaseCompatibilityError(
                    f"{path}: suppression row {index} has duplicate XML fields: {duplicates}"
                )
            if unknown:
                raise ReleaseCompatibilityError(
                    f"{path}: suppression row {index} has unknown XML fields: {unknown}"
                )
            values = {
                child.tag.rsplit("}", 1)[-1]: (child.text or "").strip()
                for child in element
            }
            missing = [field for field in required if not values.get(field)]
            if missing:
                raise ReleaseCompatibilityError(
                    f"{path}: suppression row {index} has missing XML fields: {missing}"
                )
            if values["IsBaselineSuppression"] != "true":
                raise ReleaseCompatibilityError(
                    f"{path}: suppression row {index} must set IsBaselineSuppression to true"
                )
            if values["Left"] != values["Right"]:
                raise ReleaseCompatibilityError(
                    f"{path}: suppression row {index} must not widen its package comparison scope"
                )
            assembly_match = ASSEMBLY_PATH.fullmatch(values["Left"])
            if assembly_match is None:
                raise ReleaseCompatibilityError(
                    f"{path}: suppression row {index} Left must identify one lib/<tfm> assembly"
                )
            expected_assembly = f"{package}.dll"
            if assembly_match.group("assembly") != expected_assembly:
                raise ReleaseCompatibilityError(
                    f"{path}: suppression row {index} Left/Right assembly must be "
                    f"{expected_assembly}"
                )
            key = (
                f"{package}|{assembly_match.group('tfm')}|"
                f"{values['DiagnosticId']}|{values['Target']}"
            )
            if key in rows:
                raise ReleaseCompatibilityError(f"{path}: duplicate XML suppression row '{key}'")
            rows.add(key)
    return rows
