#!/usr/bin/env python3
"""Pack the exact FrontComposer NuGet packages published by semantic-release.

CI-time packer invoked by the shared reusable Hexalith.Builds ``domain-ci.yml`` as::

    python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test

Positional signature (``<output_dir> <version>``) matching Hexalith.Tenants. Unlike
Tenants' hardcoded ``PACKAGE_PROJECTS`` constant, this reads the single source of truth
``eng/release-package-inventory.json`` (filtering ``packable == true``) so the CI-time
package set can never drift from the release inventory the governance tests pin. The
solution is expected to already be built ``-warnaserror`` (the reusable builds before
calling this), so packing runs ``--no-build``. A validation-aware solution restore runs
first so package baselines are available even when the NuGet cache starts cold.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve()
REPO_ROOT = SCRIPT_PATH.parents[1]
INVENTORY_PATH = REPO_ROOT / "eng" / "release-package-inventory.json"
EXPECTED_PACKAGE_COUNT = 8
SOLUTION_PATH = REPO_ROOT / "Hexalith.FrontComposer.slnx"
sys.path.insert(0, str(REPO_ROOT / "eng"))

from release_compatibility import release_properties, validate_release_policy  # noqa: E402


def packable_projects() -> list[Path]:
    """Validate and return the exact eight packable projects in the release inventory."""
    try:
        with INVENTORY_PATH.open("r", encoding="utf-8") as handle:
            payload = json.load(handle)
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise ValueError(f"{INVENTORY_PATH}: cannot read release package inventory: {error}") from error
    if not isinstance(payload, dict):
        raise ValueError(f"{INVENTORY_PATH}: release package inventory must be an object")
    rows = payload.get("packages")
    if not isinstance(rows, list):
        raise ValueError(f"{INVENTORY_PATH}: packages must be an array")
    packable = [
        (index, row)
        for index, row in enumerate(rows)
        if isinstance(row, dict) and row.get("packable") is True
    ]
    if len(packable) != EXPECTED_PACKAGE_COUNT:
        raise ValueError(
            f"{INVENTORY_PATH}: expected exactly {EXPECTED_PACKAGE_COUNT} packable packages; "
            f"found {len(packable)}"
        )

    projects: list[Path] = []
    package_ids: set[str] = set()
    resolved_projects: set[Path] = set()
    resolved_root = REPO_ROOT.resolve()
    for index, row in packable:
        required = ("project", "package_id", "packable", "symbol_required")
        missing = [field for field in required if field not in row]
        if missing:
            raise ValueError(f"{INVENTORY_PATH}: packable row {index} is missing fields: {missing}")
        project_value = row["project"]
        package_id = row["package_id"]
        if not isinstance(project_value, str) or not project_value.strip():
            raise ValueError(f"{INVENTORY_PATH}: packable row {index} project must be a non-empty string")
        if not isinstance(package_id, str) or not package_id.strip():
            raise ValueError(f"{INVENTORY_PATH}: packable row {index} package_id must be a non-empty string")
        if row["packable"] is not True or row["symbol_required"] is not True:
            raise ValueError(
                f"{INVENTORY_PATH}: packable row {index} must set packable and symbol_required to true"
            )
        project_path = Path(project_value)
        resolved_project = (
            project_path.resolve()
            if project_path.is_absolute()
            else (resolved_root / project_path).resolve()
        )
        if not resolved_project.is_relative_to(resolved_root):
            raise ValueError(f"{INVENTORY_PATH}: packable row {index} project escapes the repository root")
        if not resolved_project.is_file() or resolved_project.suffix.casefold() != ".csproj":
            raise ValueError(
                f"{INVENTORY_PATH}: packable row {index} project does not identify an existing .csproj"
            )
        normalized_id = package_id.casefold()
        if normalized_id in package_ids:
            raise ValueError(f"{INVENTORY_PATH}: duplicate packable package_id '{package_id}'")
        if resolved_project in resolved_projects:
            raise ValueError(f"{INVENTORY_PATH}: duplicate packable project '{project_value}'")
        package_ids.add(normalized_id)
        resolved_projects.add(resolved_project)
        projects.append(resolved_project)
    return projects


def pack_commands(output_directory: Path, version: str) -> list[list[str]]:
    """Build the exact eight-package ``dotnet pack --no-build`` command plan."""
    properties = release_properties(version)
    return [
        [
            "dotnet",
            "pack",
            str(project),
            "--no-build",
            "--configuration",
            "Release",
            "--output",
            str(output_directory),
            "--include-symbols",
            *properties,
            "-p:SymbolPackageFormat=snupkg",
            "/m:1",
            "/nr:false",
        ]
        for project in packable_projects()
    ]


def restore_command(version: str) -> list[str]:
    """Build the validation-aware solution restore required before no-build packing."""
    return [
        "dotnet",
        "restore",
        str(SOLUTION_PATH),
        "-p:Configuration=Release",
        *release_properties(version),
        "/m:1",
        "/nr:false",
    ]


def main() -> int:
    parser = argparse.ArgumentParser(description="Pack FrontComposer release packages.")
    parser.add_argument("output_directory", type=Path, help="Directory where .nupkg files are written.")
    parser.add_argument("version", help="Package version to apply.")
    parser.add_argument(
        "--release-policy",
        action="store_true",
        help="Require release-line, suppression, XML, and published-baseline compatibility policy.",
    )
    parser.add_argument("--plan", action="store_true", help="Print the live pack command plan without writing output.")
    args = parser.parse_args()

    release_line = validate_release_policy(
        REPO_ROOT,
        args.version,
        match_candidate_release=args.release_policy,
    )

    # Resolve against the caller's current directory before switching dotnet to REPO_ROOT.
    # Cleanup and every command therefore target one identical absolute directory.
    output_directory = args.output_directory.resolve()
    restore = restore_command(args.version)
    commands = pack_commands(output_directory, args.version)
    if args.plan:
        json.dump(
            {
                "schemaVersion": "1.0",
                "version": args.version,
                "releasePolicy": args.release_policy,
                "releaseLine": release_line,
                "restoreCommand": restore,
                "commands": commands,
            },
            sys.stdout,
            indent=2,
        )
        sys.stdout.write("\n")
        return 0

    # Policy, candidate SemVer, inventory validation, and validation-aware restore above
    # must all complete before the first package-output mutation. Shared CI skips only
    # release-line matching; it never relies on a warm package-baseline cache.
    subprocess.run(restore, check=True, cwd=REPO_ROOT)
    output_directory.mkdir(parents=True, exist_ok=True)
    for package in output_directory.glob("*.nupkg"):
        package.unlink()
    for package in output_directory.glob("*.snupkg"):
        package.unlink()

    for command in commands:
        subprocess.run(command, check=True, cwd=REPO_ROOT)

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as exc:
        print(f"Package packing failed with exit code {exc.returncode}.", file=sys.stderr)
        raise SystemExit(exc.returncode)
    except Exception as exc:  # noqa: BLE001 - command-line packer should print concise failures.
        print(f"Package packing failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
