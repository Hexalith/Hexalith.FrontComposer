#!/usr/bin/env python3
"""Build and pack the explicit FrontComposer release package inventory."""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import subprocess
import sys


COMPATIBILITY_SUPPRESSIONS_SCHEMA_VERSION = "2.0"
VERSION_TOKEN = re.compile(
    r"^v?(?P<major>0|[1-9][0-9]*)\.(?P<minor>0|[1-9][0-9]*)(?:\.(?:0|[1-9][0-9]*))?(?:[-+][0-9A-Za-z.-]+)?$"
)


def run(command: list[str], cwd: pathlib.Path) -> None:
    subprocess.run(command, cwd=cwd, check=True)


def package_rows(inventory_path: pathlib.Path) -> list[dict[str, object]]:
    with inventory_path.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)
    rows = payload.get("packages", [])
    if not isinstance(rows, list):
        raise SystemExit(f"{inventory_path}: packages must be an array")
    return [row for row in rows if isinstance(row, dict) and row.get("packable") is True]


def release_line(value: str, field: str) -> tuple[int, int]:
    match = VERSION_TOKEN.fullmatch(value)
    if match is None:
        raise SystemExit(f"{field} '{value}' is not a supported version token")
    return int(match.group("major")), int(match.group("minor"))


def validate_suppression_release(suppressions_path: pathlib.Path, version: str) -> str:
    with suppressions_path.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)

    if payload.get("schemaVersion") != COMPATIBILITY_SUPPRESSIONS_SCHEMA_VERSION:
        raise SystemExit(
            f"{suppressions_path}: compatibility suppression schemaVersion must be "
            f"{COMPATIBILITY_SUPPRESSIONS_SCHEMA_VERSION}"
        )

    current_value = payload.get("currentRelease")
    if not isinstance(current_value, str):
        raise SystemExit(f"{suppressions_path}: currentRelease must be a version token")

    rows = payload.get("suppressions")
    if not isinstance(rows, list):
        raise SystemExit(f"{suppressions_path}: suppressions must be an array")

    actual_line = release_line(version, "--version")
    current_line = release_line(current_value, "currentRelease")
    for index, row in enumerate(rows):
        if not isinstance(row, dict):
            raise SystemExit(f"{suppressions_path}: suppression row {index} must be an object")
        target_value = row.get("targetRelease")
        expiry_value = row.get("expiresAfter")
        if not isinstance(target_value, str) or not isinstance(expiry_value, str):
            raise SystemExit(
                f"{suppressions_path}: suppression row {index} must declare targetRelease and expiresAfter"
            )
        target_line = release_line(target_value, f"suppressions[{index}].targetRelease")
        expiry_line = release_line(expiry_value, f"suppressions[{index}].expiresAfter")
        if actual_line < target_line:
            raise SystemExit(
                f"{suppressions_path}: suppression row {index} targetRelease {target_value} "
                f"is later than --version {version}"
            )
        if actual_line >= expiry_line:
            raise SystemExit(
                f"{suppressions_path}: suppression row {index} expiresAfter {expiry_value} "
                f"has been reached by --version {version}"
            )

    if actual_line != current_line:
        raise SystemExit(
            f"{suppressions_path}: --version release line v{actual_line[0]}.{actual_line[1]} "
            f"does not match currentRelease {current_value}"
        )

    return f"v{actual_line[0]}.{actual_line[1]}"


def release_plan(
    rows: list[dict[str, object]],
    root: pathlib.Path,
    output: pathlib.Path,
    configuration: str,
    version: str,
) -> list[dict[str, object]]:
    plan: list[dict[str, object]] = []
    common_properties = [
        f"-p:Version={version}",
        f"-p:PackageVersion={version}",
        "-p:ContinuousIntegrationBuild=true",
        "-p:EnableFrontComposerPackageValidation=true",
        "-p:SymbolPackageFormat=snupkg",
    ]
    for row in rows:
        project_value = row.get("project")
        package_id_value = row.get("package_id")
        if not isinstance(project_value, str) or not project_value:
            raise SystemExit("packable inventory row is missing project")
        if not isinstance(package_id_value, str) or not package_id_value:
            raise SystemExit(f"{project_value}: packable inventory row is missing package_id")

        project = (root / project_value).resolve()
        if not project.is_file():
            raise SystemExit(f"{project_value}: project file not found")

        plan.append(
            {
                "project": project_value,
                "package_id": package_id_value,
                "symbol_required": row.get("symbol_required") is True,
                "commands": [
                    [
                        "dotnet",
                        "build",
                        str(project),
                        "--configuration",
                        configuration,
                        *common_properties,
                    ],
                    [
                        "dotnet",
                        "pack",
                        str(project),
                        "--no-build",
                        "--configuration",
                        configuration,
                        "--include-symbols",
                        "--include-source",
                        "--output",
                        str(output),
                        *common_properties,
                    ],
                ],
            }
        )
    return plan


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", default=".")
    parser.add_argument("--inventory", default="eng/release-package-inventory.json")
    parser.add_argument("--configuration", default="Release")
    parser.add_argument("--version", required=True)
    parser.add_argument("--output", default="./nupkgs")
    parser.add_argument(
        "--suppressions",
        default="docs/diagnostics/compatibility-suppressions.json",
    )
    parser.add_argument("--plan", action="store_true")
    args = parser.parse_args()

    root = pathlib.Path(args.root).resolve()
    inventory_path = (root / args.inventory).resolve()
    suppressions_path = (root / args.suppressions).resolve()
    output = (root / args.output).resolve()

    rows = package_rows(inventory_path)
    if not rows:
        raise SystemExit("release package inventory contains no packable projects")

    actual_release_line = validate_suppression_release(suppressions_path, args.version)
    plan = release_plan(rows, root, output, args.configuration, args.version)
    if args.plan:
        json.dump(
            {
                "schemaVersion": "1.0",
                "version": args.version,
                "releaseLine": actual_release_line,
                "packages": plan,
                "commands": [command for item in plan for command in item["commands"]],
            },
            sys.stdout,
            indent=2,
        )
        sys.stdout.write("\n")
        return 0

    output.mkdir(parents=True, exist_ok=True)
    for item in plan:
        project_value = str(item["project"])
        package_id_value = str(item["package_id"])
        for command in item["commands"]:
            run(command, root)

        expected_nupkg = output / f"{package_id_value}.{args.version}.nupkg"
        if not expected_nupkg.is_file():
            raise SystemExit(f"{package_id_value}: expected package was not produced")
        if item["symbol_required"] is True:
            expected_snupkg = output / f"{package_id_value}.{args.version}.snupkg"
            if not expected_snupkg.is_file():
                raise SystemExit(f"{package_id_value}: expected symbol package was not produced")

    return 0


if __name__ == "__main__":
    sys.exit(main())
