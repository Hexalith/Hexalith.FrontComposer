#!/usr/bin/env python3
"""Build and pack the explicit FrontComposer release package inventory."""

from __future__ import annotations

import argparse
import json
import pathlib
import subprocess
import sys


def run(command: list[str], cwd: pathlib.Path) -> None:
    subprocess.run(command, cwd=cwd, check=True)


def package_rows(inventory_path: pathlib.Path) -> list[dict[str, object]]:
    with inventory_path.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)
    rows = payload.get("packages", [])
    if not isinstance(rows, list):
        raise SystemExit(f"{inventory_path}: packages must be an array")
    return [row for row in rows if isinstance(row, dict) and row.get("packable") is True]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", default=".")
    parser.add_argument("--inventory", default="eng/release-package-inventory.json")
    parser.add_argument("--configuration", default="Release")
    parser.add_argument("--version", required=True)
    parser.add_argument("--output", default="./nupkgs")
    args = parser.parse_args()

    root = pathlib.Path(args.root).resolve()
    inventory_path = (root / args.inventory).resolve()
    output = (root / args.output).resolve()
    output.mkdir(parents=True, exist_ok=True)

    rows = package_rows(inventory_path)
    if not rows:
        raise SystemExit("release package inventory contains no packable projects")

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

        common_properties = [
            f"-p:Version={args.version}",
            f"-p:PackageVersion={args.version}",
            "-p:ContinuousIntegrationBuild=true",
            "-p:SymbolPackageFormat=snupkg",
        ]
        run(
            [
                "dotnet",
                "build",
                str(project),
                "--configuration",
                args.configuration,
                *common_properties,
            ],
            root,
        )
        run(
            [
                "dotnet",
                "pack",
                str(project),
                "--no-build",
                "--configuration",
                args.configuration,
                "--include-symbols",
                "--include-source",
                "--output",
                str(output),
                *common_properties,
            ],
            root,
        )

        expected_nupkg = output / f"{package_id_value}.{args.version}.nupkg"
        if not expected_nupkg.is_file():
            raise SystemExit(f"{package_id_value}: expected package was not produced")
        if row.get("symbol_required") is True:
            expected_snupkg = output / f"{package_id_value}.{args.version}.snupkg"
            if not expected_snupkg.is_file():
                raise SystemExit(f"{package_id_value}: expected symbol package was not produced")

    return 0


if __name__ == "__main__":
    sys.exit(main())
