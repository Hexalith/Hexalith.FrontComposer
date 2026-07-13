#!/usr/bin/env python3
"""Pack the exact FrontComposer NuGet packages published by semantic-release.

CI-time packer invoked by the shared reusable Hexalith.Builds ``domain-ci.yml`` as::

    python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test

Positional signature (``<output_dir> <version>``) matching Hexalith.Tenants. Unlike
Tenants' hardcoded ``PACKAGE_PROJECTS`` constant, this reads the single source of truth
``eng/release-package-inventory.json`` (filtering ``packable == true``) so the CI-time
package set can never drift from the release inventory the governance tests pin. The
solution is expected to already be built ``-warnaserror`` (the reusable builds before
calling this), so packing runs ``--no-build``.
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


def packable_projects() -> list[str]:
    """Return the project paths flagged ``packable == true`` in the release inventory."""
    with INVENTORY_PATH.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)
    rows = payload.get("packages", [])
    if not isinstance(rows, list):
        raise ValueError(f"{INVENTORY_PATH}: packages must be an array")
    projects = [
        row["project"]
        for row in rows
        if isinstance(row, dict) and row.get("packable") is True and isinstance(row.get("project"), str)
    ]
    if not projects:
        raise ValueError(f"{INVENTORY_PATH}: no packable projects found")
    return projects


def main() -> int:
    parser = argparse.ArgumentParser(description="Pack FrontComposer release packages.")
    parser.add_argument("output_directory", type=Path, help="Directory where .nupkg files are written.")
    parser.add_argument("version", help="Package version to apply.")
    args = parser.parse_args()

    output_directory = args.output_directory
    output_directory.mkdir(parents=True, exist_ok=True)
    for package in output_directory.glob("*.nupkg"):
        package.unlink()
    for package in output_directory.glob("*.snupkg"):
        package.unlink()

    for project in packable_projects():
        subprocess.run(
            [
                "dotnet",
                "pack",
                str(REPO_ROOT / project),
                "--no-build",
                "--configuration",
                "Release",
                "--output",
                str(output_directory),
                "--include-symbols",
                f"-p:Version={args.version}",
                "-p:SymbolPackageFormat=snupkg",
                "/m:1",
                "/nr:false",
            ],
            check=True,
        )

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
