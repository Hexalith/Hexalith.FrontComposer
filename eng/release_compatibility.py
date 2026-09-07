#!/usr/bin/env python3
"""Pure compatibility-lifecycle policy for FrontComposer release candidates."""

from __future__ import annotations

import json
import pathlib
import re
import xml.etree.ElementTree as ET
from collections.abc import Mapping, Sequence


COMPATIBILITY_SUPPRESSIONS_SCHEMA_VERSION = "2.0"
# The published package-validation baseline every live pack command applies. It is no longer
# self-referential: `validate_release_policy` checks it against the release line the candidate (or
# the checked-in `currentRelease`) declares, so leaving it behind a published line fails closed.
PUBLISHED_BASELINE_VERSION = "4.3.0"
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

BASELINE_PROPERTY_NAME = "FrontComposerPackageValidationBaselineVersion"
# The shared property file every packable project inherits the baseline from. Packable projects may
# pin an override; both sets are enumerated from `eng/release-package-inventory.json` so a new
# packable package cannot introduce an unreviewed baseline or suppression site.
SHARED_BASELINE_PATH = "Directory.Build.targets"
INVENTORY_PATH = "eng/release-package-inventory.json"
EXPECTED_PACKAGE_COUNT = 8
SUPPRESSION_FILE_NAME = "CompatibilitySuppressions.xml"
# Inventory row flags. `compatibility_suppressions` marks a package whose suppression XML is a
# reviewed, checked-in artifact: deleting it must fail closed rather than silently drop a site.
# `pack_as_tool` marks the PackAsTool row the SDK disables package validation for.
SUPPRESSION_FLAG_FIELD = "compatibility_suppressions"
# `baseline_override` marks a packable project whose FrontComposerPackageValidationBaselineVersion
# pin is a reviewed, checked-in site: removing the pin must fail closed, not silently fall back to
# the shared default.
BASELINE_OVERRIDE_FIELD = "baseline_override"
PACK_AS_TOOL_FIELD = "pack_as_tool"
PACK_AS_TOOL_REASON_FIELD = "pack_as_tool_reason"
# Optional inventory row flags. Each must be a real JSON boolean `true` when present: a string
# "true" is truthy in most readers and would silently stop requiring a reviewed site.
OPTIONAL_ROW_FLAGS = (SUPPRESSION_FLAG_FIELD, BASELINE_OVERRIDE_FIELD, PACK_AS_TOOL_FIELD)
# Every packable project lives under this directory, so a suppression file found anywhere else in
# it belongs to no packable package and can never be reviewed by the ledger parity check.
SUPPRESSION_SCAN_ROOT = "src"
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


def preceding_release_line(line: tuple[int, int]) -> str | None:
    """Return the label of the release line immediately preceding ``line``, or None for v0.0.

    ``v0.0`` is the one line with no predecessor. Returning None instead of raising keeps the
    baseline diagnostic buildable there, so it can still name the found and the accepted values.
    """
    major, minor = line
    if minor > 0:
        return f"v{major}.{minor - 1}"
    if major > 0:
        return f"v{major - 1}.x"
    return None


def is_preceding_release_line(baseline_line: tuple[int, int], line: tuple[int, int]) -> bool:
    """Return whether ``baseline_line`` is the release line immediately before ``line``.

    Known limitation for a major bump: a candidate ``M.0.x`` resets the minor, so the line before
    it is the last minor of major ``M-1`` and that number cannot be derived from the candidate
    version. The compatibility ledger records only the planned ``currentRelease``, not published
    history, so there is no in-repository source for it either. Any minor of major ``M-1`` is
    therefore accepted, which means ``5.0.0`` validates against a stale ``4.0.x`` baseline even
    when ``4.9.x`` is published. Advancing the baseline across a major bump stays a reviewed step.
    """
    major, minor = line
    if minor > 0:
        return baseline_line == (major, minor - 1)
    if major > 0:
        return baseline_line[0] == major - 1
    return False


def is_within_one_release_line(baseline_line: tuple[int, int], line: tuple[int, int]) -> bool:
    """Return whether the baseline is at most one release line behind ``line``.

    The baseline may sit on ``line`` itself -- the hotfix case, where ``4.3.1`` is diffed against
    published ``4.3.0`` -- or on the line immediately before it. Two or more lines back fails.
    """
    return baseline_line == line or is_preceding_release_line(baseline_line, line)


def packable_packages(
    root: pathlib.Path,
    inventory_path: pathlib.Path | None = None,
) -> list[tuple[str, pathlib.Path]]:
    """Validate and return the exact eight packable ``(package_id, project)`` inventory rows."""
    return [(package_id, project) for package_id, project, _ in _packable_rows(root, inventory_path)]


def _packable_rows(
    root: pathlib.Path,
    inventory_path: pathlib.Path | None = None,
) -> list[tuple[str, pathlib.Path, Mapping[str, object]]]:
    """Validate the inventory and return ``(package_id, project, row)`` for every packable row."""
    root = root.resolve()
    path = _resolve(root, inventory_path or pathlib.Path(INVENTORY_PATH))
    try:
        with path.open("r", encoding="utf-8") as handle:
            payload = json.load(handle)
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise ReleaseCompatibilityError(
            f"{path}: cannot read release package inventory: {error}"
        ) from error
    if not isinstance(payload, dict):
        raise ReleaseCompatibilityError(f"{path}: release package inventory must be an object")
    rows = payload.get("packages")
    if not isinstance(rows, list):
        raise ReleaseCompatibilityError(f"{path}: packages must be an array")
    packable = [
        (index, row)
        for index, row in enumerate(rows)
        if isinstance(row, dict) and row.get("packable") is True
    ]
    if len(packable) != EXPECTED_PACKAGE_COUNT:
        raise ReleaseCompatibilityError(
            f"{path}: expected exactly {EXPECTED_PACKAGE_COUNT} packable packages; "
            f"found {len(packable)}"
        )

    packages: list[tuple[str, pathlib.Path, Mapping[str, object]]] = []
    package_ids: set[str] = set()
    resolved_projects: set[pathlib.Path] = set()
    for index, row in packable:
        required = ("project", "package_id", "packable", "symbol_required")
        missing = [field for field in required if field not in row]
        if missing:
            raise ReleaseCompatibilityError(f"{path}: packable row {index} is missing fields: {missing}")
        project_value = row["project"]
        package_id = row["package_id"]
        if not isinstance(project_value, str) or not project_value.strip():
            raise ReleaseCompatibilityError(
                f"{path}: packable row {index} project must be a non-empty string"
            )
        if not isinstance(package_id, str) or not package_id.strip():
            raise ReleaseCompatibilityError(
                f"{path}: packable row {index} package_id must be a non-empty string"
            )
        if row["packable"] is not True or row["symbol_required"] is not True:
            raise ReleaseCompatibilityError(
                f"{path}: packable row {index} must set packable and symbol_required to true"
            )
        for flag in OPTIONAL_ROW_FLAGS:
            if flag in row and row[flag] is not True:
                raise ReleaseCompatibilityError(
                    f"{path}: packable row {index} {flag} must be the JSON boolean true when "
                    f"present; found {row[flag]!r}"
                )
        if row.get(PACK_AS_TOOL_FIELD) is True:
            reason = row.get(PACK_AS_TOOL_REASON_FIELD)
            if not isinstance(reason, str) or not reason.strip():
                raise ReleaseCompatibilityError(
                    f"{path}: packable row {index} must document {PACK_AS_TOOL_REASON_FIELD} "
                    "when it opts out of the library package-validation gate"
                )
        resolved_project = _resolve(root, pathlib.Path(project_value))
        if not resolved_project.is_relative_to(root):
            raise ReleaseCompatibilityError(
                f"{path}: packable row {index} project escapes the repository root"
            )
        if not resolved_project.is_file() or resolved_project.suffix.casefold() != ".csproj":
            raise ReleaseCompatibilityError(
                f"{path}: packable row {index} project does not identify an existing .csproj"
            )
        normalized_id = package_id.casefold()
        if normalized_id in package_ids:
            raise ReleaseCompatibilityError(f"{path}: duplicate packable package_id '{package_id}'")
        if resolved_project in resolved_projects:
            raise ReleaseCompatibilityError(f"{path}: duplicate packable project '{project_value}'")
        package_ids.add(normalized_id)
        resolved_projects.add(resolved_project)
        packages.append((package_id, resolved_project, row))
    return packages


def packable_projects(
    root: pathlib.Path,
    inventory_path: pathlib.Path | None = None,
) -> list[pathlib.Path]:
    """Return the exact eight packable project files declared by the release inventory."""
    return [project for _, project in packable_packages(root, inventory_path)]


def packable_library_projects(
    root: pathlib.Path,
    inventory_path: pathlib.Path | None = None,
) -> list[pathlib.Path]:
    """Return every packable project except the documented ``pack_as_tool`` exception.

    These are the packages .NET package validation actually applies to; the SDK disables
    ``EnablePackageValidation`` for a ``PackAsTool`` layout.
    """
    return [
        project
        for _, project, flags in _packable_rows(root, inventory_path)
        if flags.get(PACK_AS_TOOL_FIELD) is not True
    ]


def inventory_required_suppression_files(
    root: pathlib.Path,
    inventory_path: pathlib.Path | None = None,
) -> set[str]:
    """Return the packages whose checked-in suppression XML must exist."""
    return {
        package_id
        for package_id, _, flags in _packable_rows(root, inventory_path)
        if flags.get(SUPPRESSION_FLAG_FIELD) is True
    }


def inventory_baseline_override_paths(
    root: pathlib.Path,
    inventory_path: pathlib.Path | None = None,
) -> tuple[pathlib.Path, ...]:
    """Return every packable project that MAY pin a package-validation baseline override."""
    return tuple(
        project
        for _, project, flags in _packable_rows(root, inventory_path)
        if flags.get(BASELINE_OVERRIDE_FIELD) is not True
    )


def inventory_required_baseline_paths(
    root: pathlib.Path,
    inventory_path: pathlib.Path | None = None,
) -> tuple[pathlib.Path, ...]:
    """Return the shared default plus every packable project that MUST pin the baseline."""
    return (
        root.resolve() / SHARED_BASELINE_PATH,
        *(
            project
            for _, project, flags in _packable_rows(root, inventory_path)
            if flags.get(BASELINE_OVERRIDE_FIELD) is True
        ),
    )


def inventory_suppression_files(
    root: pathlib.Path,
    inventory_path: pathlib.Path | None = None,
) -> dict[str, pathlib.Path]:
    """Map every packable package id to the suppression XML the SDK reads for that project."""
    return {
        package_id: project.parent / SUPPRESSION_FILE_NAME
        for package_id, project in packable_packages(root, inventory_path)
    }


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
    baseline_override_paths: Sequence[pathlib.Path] | None = None,
    suppression_files: Mapping[str, pathlib.Path] | None = None,
    inventory_path: pathlib.Path | None = None,
    published_baseline: str = PUBLISHED_BASELINE_VERSION,
    match_candidate_release: bool = True,
) -> str | None:
    """Validate the release line, suppression lifecycle, baseline, and XML parity."""
    root = root.resolve()
    actual_line = candidate_release_line(version)
    ledger_path = _resolve(
        root,
        suppressions_path or pathlib.Path("docs/diagnostics/compatibility-suppressions.json"),
    )
    # Every baseline and suppression site is enumerated from the release inventory rather than a
    # hardcoded tuple, so adding a packable package cannot add an unreviewed site.
    configured_baselines = tuple(
        _resolve(root, path)
        for path in (
            inventory_required_baseline_paths(root, inventory_path)
            if baseline_paths is None
            else baseline_paths
        )
    )
    configured_overrides = tuple(
        _resolve(root, path)
        for path in (
            inventory_baseline_override_paths(root, inventory_path)
            if baseline_override_paths is None
            else baseline_override_paths
        )
    )
    inventory_derived_suppressions = suppression_files is None
    configured_suppressions = {
        package: _resolve(root, path)
        for package, path in (
            inventory_suppression_files(root, inventory_path)
            if inventory_derived_suppressions
            else suppression_files
        ).items()
    }
    # A packable project that never carried a suppression file contributes no rows, but a reviewed
    # file that vanished must fail closed. The inventory's `compatibility_suppressions` flag names
    # the reviewed sites; an explicit override keeps the stricter all-files-required behaviour.
    required_suppressions = (
        inventory_required_suppression_files(root, inventory_path)
        if inventory_derived_suppressions
        else set(configured_suppressions)
    )

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

    _validate_baseline(
        configured_baselines,
        configured_overrides,
        published_baseline,
        actual_line if match_candidate_release else current_line,
        policy_label,
    )

    if inventory_derived_suppressions:
        _reject_unenumerated_suppression_files(root, configured_suppressions)
    # A ledger row also establishes its package's site, so its file cannot be deleted either.
    xml_rows = _suppression_xml_rows(
        configured_suppressions,
        required_suppressions | {key.split("|", 1)[0] for key in tracked_rows},
    )
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


def _validate_baseline(
    required_paths: Sequence[pathlib.Path],
    override_paths: Sequence[pathlib.Path],
    published_baseline: str,
    line: tuple[int, int],
    policy_label: str,
) -> None:
    """Check every checked-in baseline site against the release line it must trail."""
    declared: list[tuple[pathlib.Path, str]] = []
    for path in required_paths:
        values = xml_values(path, BASELINE_PROPERTY_NAME)
        if not values:
            raise ReleaseCompatibilityError(
                f"{path}: required {BASELINE_PROPERTY_NAME} declaration is missing"
            )
        declared.extend((path, value) for value in values)
    for path in override_paths:
        declared.extend((path, value) for value in xml_values(path, BASELINE_PROPERTY_NAME))

    if len({value for _, value in declared}) != 1:
        detail = "; ".join(f"{path}='{value}'" for path, value in declared)
        raise ReleaseCompatibilityError(
            f"package-validation baseline sites disagree: {detail}"
        )

    origin, baseline = declared[0]
    baseline_line = candidate_release_line(baseline, f"{origin}: {BASELINE_PROPERTY_NAME}")
    # One tolerance for both the pack-time and the static repository check: the baseline may sit on
    # the release line under validation or on the one immediately before it, never further back.
    # Packing 4.3.1 against published 4.3.0 is the hotfix case; packing 4.4.0 requires 4.3.x.
    # The expected label is computed only on failure -- a v0.0 line has no preceding line at all,
    # and the same-line case must still be accepted there.
    if not is_within_one_release_line(baseline_line, line):
        preceding = preceding_release_line(line)
        current = f"v{line[0]}.{line[1]}"
        accepted = f"{preceding} or {current}" if preceding else current
        raise ReleaseCompatibilityError(
            f"{origin}: package-validation baseline must be on the {accepted} release line "
            f"for {policy_label}; found '{baseline}'"
        )

    if baseline != published_baseline:
        raise ReleaseCompatibilityError(
            f"{origin}: package-validation baseline '{baseline}' must equal the published baseline "
            f"'{published_baseline}' every live pack command applies"
        )


def _reject_unenumerated_suppression_files(
    root: pathlib.Path,
    configured: Mapping[str, pathlib.Path],
) -> None:
    """Fail when a suppression file exists outside the packable release inventory."""
    scan_root = root / SUPPRESSION_SCAN_ROOT
    if not scan_root.is_dir():
        return
    enumerated = {path.resolve() for path in configured.values()}
    stray = sorted(
        path.relative_to(root).as_posix()
        for path in scan_root.rglob(SUPPRESSION_FILE_NAME)
        if path.resolve() not in enumerated
        and not {"bin", "obj"} & set(path.relative_to(root).parts)
    )
    if stray:
        raise ReleaseCompatibilityError(
            "compatibility suppression files outside the packable release inventory "
            f"cannot be reviewed: {stray}"
        )


def _parse_xml(path: pathlib.Path) -> ET.Element:
    if not path.is_file():
        raise ReleaseCompatibilityError(f"{path}: required compatibility policy XML file is missing")
    try:
        return ET.parse(path).getroot()
    except ET.ParseError as error:
        raise ReleaseCompatibilityError(f"{path}: invalid XML: {error}") from error
    except (OSError, UnicodeError) as error:
        raise ReleaseCompatibilityError(f"{path}: cannot read compatibility policy XML: {error}") from error


def _suppression_xml_rows(
    suppression_files: Mapping[str, pathlib.Path],
    required_packages: set[str],
) -> set[str]:
    rows: set[str] = set()
    for package, path in suppression_files.items():
        if not path.is_file():
            if package in required_packages:
                # `_parse_xml` reports the missing reviewed file with its actionable diagnostic.
                _parse_xml(path)
            # A packable project that never carried a suppression file contributes no rows.
            continue
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
