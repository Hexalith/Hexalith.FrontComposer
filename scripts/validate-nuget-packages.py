#!/usr/bin/env python3
"""Validate FrontComposer NuGet packages before publishing.

Invoked by the shared reusable Hexalith.Builds ``domain-ci.yml`` as::

    python3 scripts/validate-nuget-packages.py ./nupkgs

Asserts, over the CI-time pack output:

* exactly the 8 packable ids declared in ``eng/release-package-inventory.json`` are present
  (the id set is read from the inventory — the single source of truth — not hardcoded, so it
  cannot drift from the release set the governance tests pin);
* every package carries license metadata (readme integrity is checked when a readme is declared —
  FrontComposer only ships a README on Cli/Testing) and all packages share one version;
* no package depends on a host / test / sample project (forbidden-fragment guard); and
* the kernel-split invariant (FR24 AC6): ``Hexalith.FrontComposer.Contracts`` — which targets
  ``net10.0;netstandard2.0`` and must stay UI-clean — declares NO Blazor / Fluent / Fluxor
  dependency, so a Contracts-only consumer never inherits UI runtime deps. Contracts.UI / Shell
  are the UI faces and may reference those.
"""

from __future__ import annotations

import argparse
import json
import sys
import zipfile
from dataclasses import dataclass
from pathlib import Path
from xml.etree import ElementTree


SCRIPT_PATH = Path(__file__).resolve()
REPO_ROOT = SCRIPT_PATH.parents[1]
INVENTORY_PATH = REPO_ROOT / "eng" / "release-package-inventory.json"

# Dependency id substrings that must never appear on a shipped package: hosts, tests, samples.
FORBIDDEN_DEPENDENCY_FRAGMENTS = (
    ".Tests",
    ".Test",
    ".Sample",
    ".Samples",
    ".AppHost",
    ".ServiceDefaults",
)

# The kernel-split invariant: the netstandard2.0/net10 Contracts kernel must never pull UI deps.
KERNEL_PACKAGE_ID = "Hexalith.FrontComposer.Contracts"
FORBIDDEN_KERNEL_FRAGMENTS = (
    "Microsoft.FluentUI",
    "Fluxor",
    "Microsoft.AspNetCore.Components",
    "Microsoft.AspNetCore.App",
)


def expected_package_ids() -> frozenset[str]:
    """Return the packable package ids from the release inventory (single source of truth)."""
    with INVENTORY_PATH.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)
    rows = payload.get("packages", [])
    ids = frozenset(
        row["package_id"]
        for row in rows
        if isinstance(row, dict) and row.get("packable") is True and isinstance(row.get("package_id"), str)
    )
    if not ids:
        raise ValueError(f"{INVENTORY_PATH}: no packable package ids found")
    return ids


@dataclass(frozen=True)
class PackageMetadata:
    package_id: str
    version: str
    readme: str | None
    has_license: bool
    dependencies: frozenset[str]


def get_metadata(package_path: Path) -> PackageMetadata:
    """Return package id, version, metadata flags, and dependency ids."""
    with zipfile.ZipFile(package_path) as package:
        nuspec_names = [name for name in package.namelist() if name.endswith(".nuspec")]
        if len(nuspec_names) != 1:
            raise ValueError(f"{package_path.name}: expected exactly one .nuspec file")

        root = ElementTree.fromstring(package.read(nuspec_names[0]))
        ns = {"n": root.tag.split("}")[0].strip("{")} if root.tag.startswith("{") else {}

        def find_text(name: str) -> str | None:
            element = root.find(f".//n:metadata/n:{name}", ns) if ns else root.find(f".//metadata/{name}")
            return element.text.strip() if element is not None and element.text else None

        def find_elements(path: str) -> list[ElementTree.Element]:
            return root.findall(path, ns) if ns else root.findall(path.replace("n:", ""))

        package_id = find_text("id")
        version = find_text("version")
        readme = find_text("readme")
        license_value = find_text("license")
        license_file = find_text("licenseFile")

        if not package_id:
            raise ValueError(f"{package_path.name}: missing nuspec package id")
        if not version:
            raise ValueError(f"{package_path.name}: missing nuspec version")
        # readme is optional (FrontComposer ships a README only on Cli/Testing), but when declared
        # it must actually be embedded in the package.
        if readme and readme not in package.namelist():
            raise ValueError(f"{package_path.name}: readme file '{readme}' is declared but is not in the package")

        dependencies = frozenset(
            dependency.attrib["id"].strip()
            for dependency in find_elements(".//n:metadata/n:dependencies//n:dependency")
            if dependency.attrib.get("id", "").strip()
        )

        return PackageMetadata(package_id, version, readme, bool(license_value or license_file), dependencies)


def _matches_forbidden_project(dependency: str) -> bool:
    """Match a host/test/sample fragment at a dot-delimited segment boundary.

    NuGet package ids compare case-insensitively, so both sides are casefolded. The boundary
    check (endswith / ``fragment + "."``) prevents ``.Test`` from spuriously matching the shipped
    ``Hexalith.FrontComposer.Testing`` package (``.Test`` is a substring of ``.Testing``).
    """
    dep = dependency.casefold()
    for fragment in FORBIDDEN_DEPENDENCY_FRAGMENTS:
        needle = fragment.casefold()
        if dep.endswith(needle) or (needle + ".") in dep:
            return True
    return False


def _matches_forbidden_kernel(dependency: str) -> bool:
    """Case-insensitive substring match for UI runtime packages the Contracts kernel must not pull."""
    dep = dependency.casefold()
    return any(fragment.casefold() in dep for fragment in FORBIDDEN_KERNEL_FRAGMENTS)


def validate_dependency_boundaries(package_path: Path, metadata: PackageMetadata) -> None:
    """Validate package dependency metadata against the intended package boundaries."""
    forbidden = sorted(
        dependency
        for dependency in metadata.dependencies
        if _matches_forbidden_project(dependency)
    )
    if forbidden:
        raise ValueError(
            f"{package_path.name}: dependency set includes host, samples, tests, or other forbidden "
            f"projects: {forbidden}"
        )

    if metadata.package_id == KERNEL_PACKAGE_ID:
        kernel_violations = sorted(
            dependency
            for dependency in metadata.dependencies
            if _matches_forbidden_kernel(dependency)
        )
        if kernel_violations:
            raise ValueError(
                f"{package_path.name}: kernel-split invariant violated — the Contracts kernel must not "
                f"depend on Blazor/Fluent/Fluxor UI packages, found: {kernel_violations}"
            )


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate FrontComposer NuGet package output.")
    parser.add_argument("package_directory", type=Path, help="Directory containing .nupkg files.")
    args = parser.parse_args()

    expected_ids = expected_package_ids()
    package_directory = args.package_directory
    packages = sorted(
        path
        for path in package_directory.glob("*.nupkg")
        if ".symbols." not in path.name and not path.name.endswith(".snupkg")
    )

    if len(packages) != len(expected_ids):
        package_list = ", ".join(path.name for path in packages) or "<none>"
        raise ValueError(
            f"Expected {len(expected_ids)} packages, found {len(packages)}: {package_list}"
        )

    package_ids: set[str] = set()
    versions: set[str] = set()
    for package in packages:
        metadata = get_metadata(package)
        package_ids.add(metadata.package_id)
        versions.add(metadata.version)
        if not metadata.has_license:
            raise ValueError(f"{package.name}: missing license metadata")
        validate_dependency_boundaries(package, metadata)

    if package_ids != expected_ids:
        missing = sorted(expected_ids - package_ids)
        unexpected = sorted(package_ids - expected_ids)
        raise ValueError(f"Package id mismatch. Missing: {missing}; unexpected: {unexpected}")

    if len(versions) != 1:
        raise ValueError(f"Expected all packages to share one version, found: {sorted(versions)}")

    version = next(iter(versions))
    print(f"Validated {len(packages)} NuGet packages at version {version}:")
    for package_id in sorted(package_ids):
        print(f"- {package_id}")
    print(f"Kernel-split invariant held: {KERNEL_PACKAGE_ID} declares no Blazor/Fluent/Fluxor dependency.")

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # noqa: BLE001 - command-line validator should print concise failures.
        print(f"Package validation failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
