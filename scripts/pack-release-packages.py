#!/usr/bin/env python3
"""Pack the exact FrontComposer NuGet packages published by semantic-release.

CI-time packer invoked by the shared reusable Hexalith.Builds ``domain-ci.yml`` as::

    python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test

Positional signature (``<output_dir> <version>``) matching Hexalith.Tenants. Unlike
Tenants' hardcoded ``PACKAGE_PROJECTS`` constant, this reads the single source of truth
``eng/release-package-inventory.json`` (filtering ``packable == true``) so the CI-time
package set can never drift from the release inventory the governance tests pin. The
solution is expected to already be built ``-warnaserror`` (the reusable builds before
calling this). Production versions pack ``--no-build``; the synthetic ``0.0.0-ci-test``
coordinate rebuilds with a baseline-compatible assembly identity. A validation-aware
solution restore runs first so package baselines are available even when the NuGet cache
starts cold.
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
SOLUTION_PATH = REPO_ROOT / "Hexalith.FrontComposer.slnx"
sys.path.insert(0, str(REPO_ROOT / "eng"))

from release_compatibility import PUBLISHED_BASELINE_VERSION  # noqa: E402
from release_compatibility import packable_projects as inventory_packable_projects  # noqa: E402
from release_compatibility import release_properties, validate_release_policy  # noqa: E402


def packable_projects() -> list[Path]:
    """Return the exact eight packable projects in the release inventory.

    The reader lives in ``eng/release_compatibility.py`` so the compatibility policy enumerates
    baseline and suppression sites from the same inventory rows this packer packs; keeping a
    module-level wrapper preserves the ``REPO_ROOT``/``INVENTORY_PATH`` seam the tests patch.
    """
    return inventory_packable_projects(REPO_ROOT, INVENTORY_PATH)


def is_synthetic_ci_version(version: str) -> bool:
    """Return whether this is the shared domain-ci synthetic pack coordinate."""
    return version.startswith("0.0.0-ci")


def pack_commands(output_directory: Path, version: str) -> list[list[str]]:
    """Build the exact eight-package ``dotnet pack`` command plan.

    Production candidates keep ``--no-build`` after a versioned solution build. Shared CI packs
    ``0.0.0-ci-test`` after an unversioned Release build, so ``--no-build`` would ship ``1.0.0.0``
    assemblies and ApiCompat CP0003 would fail against the published ``4.3.0`` baseline. Those
    synthetic packs rebuild with a baseline-compatible ``Version`` while ``PackageVersion`` stays
    on the CI coordinate the consumer validators read.
    """
    properties = release_properties(version)
    extra: list[str] = ["--no-build"]
    if is_synthetic_ci_version(version):
        extra = []
        properties = [item for item in properties if not item.startswith("-p:Version=")]
        properties.insert(0, f"-p:Version={PUBLISHED_BASELINE_VERSION}-ci")
    return [
        [
            "dotnet",
            "pack",
            str(project),
            *extra,
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

    # Policy, candidate SemVer, and inventory validation above, plus the validation-aware
    # restore below, must all complete before the first package-output mutation. Shared CI
    # skips only release-line matching; it never relies on a warm package-baseline cache.
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
        # Name the failing stage: the cold-cache case this packer guards fails in restore,
        # and reporting it as a pack failure hides that from the CI log alone.
        stage = "restore" if list(exc.cmd)[:2] == ["dotnet", "restore"] else "packing"
        print(f"Package {stage} failed with exit code {exc.returncode}.", file=sys.stderr)
        raise SystemExit(exc.returncode)
    except Exception as exc:  # noqa: BLE001 - command-line packer should print concise failures.
        print(f"Package packing failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
