#!/usr/bin/env python3
"""Build isolated consumers against local FrontComposer NuGet packages.

Invoked by the shared reusable Hexalith.Builds ``domain-ci.yml`` as::

    python3 scripts/validate-consumer-package-references.py ./nupkgs

Proves the two documented FrontComposer consumer boundaries (FR24 AC6) with throwaway,
``PackageReference``-only projects (an ``assert_package_only`` guard forbids
``ProjectReference``):

* **Contracts-only** — references ``Hexalith.FrontComposer.Contracts`` only, compiles against
  the public contract surface, and asserts NO Blazor / Fluent / Fluxor runtime assembly resolves
  into its output (the kernel-split invariant: ``Contracts`` targets ``net10.0;netstandard2.0``
  and is UI-clean, so a Contracts-only consumer never inherits UI runtime deps).
* **Shell/UI** — references ``Contracts`` + ``Contracts.UI`` + ``Shell`` and compiles the
  documented ``AddHexalithFrontComposerQuickstart`` bootstrap surface (Blazor/Fluent runtime deps
  are expected here).
"""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path
from xml.etree import ElementTree


# Packages that get a real consumer smoke build. Cli (tool), Mcp, Schema, SourceTools (Roslyn
# analyzer, PrivateAssets), and Testing are metadata-only in validate-nuget-packages.py — their
# consumer shape is a tool/analyzer/test host, not a plain library consumer.
PACKAGE_IDS = [
    "Hexalith.FrontComposer.Contracts",
    "Hexalith.FrontComposer.Contracts.UI",
    "Hexalith.FrontComposer.Shell",
]

# Runtime assemblies that must NOT appear in a Contracts-only consumer output (kernel-split).
FORBIDDEN_UI_ASSEMBLY_PREFIXES = (
    "Microsoft.FluentUI",
    "Fluxor",
    "Microsoft.AspNetCore.Components",
)

SCRIPT_PATH = Path(__file__).resolve()
REPO_ROOT = SCRIPT_PATH.parents[1]


def package_versions(package_directory: Path) -> dict[str, str]:
    versions: dict[str, str] = {}
    for package_path in package_directory.glob("*.nupkg"):
        if ".symbols." in package_path.name or package_path.name.endswith(".snupkg"):
            continue

        with zipfile.ZipFile(package_path) as package:
            nuspec_names = [name for name in package.namelist() if name.endswith(".nuspec")]
            if len(nuspec_names) != 1:
                raise ValueError(f"{package_path.name}: expected exactly one .nuspec file")

            root = ElementTree.fromstring(package.read(nuspec_names[0]))
            ns = {"n": root.tag.split("}")[0].strip("{")} if root.tag.startswith("{") else {}
            id_element = root.find(".//n:metadata/n:id", ns) if ns else root.find(".//metadata/id")
            version_element = root.find(".//n:metadata/n:version", ns) if ns else root.find(".//metadata/version")
            if id_element is None or version_element is None or not id_element.text or not version_element.text:
                raise ValueError(f"{package_path.name}: missing id or version metadata")
            versions[id_element.text.strip()] = version_element.text.strip()

    missing = sorted(set(PACKAGE_IDS) - set(versions))
    if missing:
        raise ValueError(f"Missing local packages required for consumer smoke tests: {missing}")

    distinct_versions = {versions[package_id] for package_id in PACKAGE_IDS}
    if len(distinct_versions) != 1:
        raise ValueError(f"Expected FrontComposer packages to share one version, found {sorted(distinct_versions)}")

    return versions


def run_dotnet(args: list[str], working_directory: Path) -> None:
    env = os.environ.copy()
    env.setdefault("DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER", "1")
    env.setdefault("MSBUILDDISABLENODEREUSE", "1")
    env["NUGET_PACKAGES"] = str(working_directory.parent / ".nuget" / "packages")
    subprocess.run(["dotnet", *args], cwd=working_directory, check=True, env=env)


def assert_package_only(project_file: Path, required_package_ids: list[str]) -> None:
    project_text = project_file.read_text(encoding="utf-8")
    if "ProjectReference" in project_text:
        raise ValueError(f"{project_file}: consumer projects must not use ProjectReference")

    for package_id in required_package_ids:
        if f'PackageReference Include="{package_id}"' not in project_text:
            raise ValueError(f"{project_file}: missing PackageReference for {package_id}")


def assert_no_ui_runtime_deps(output_directory: Path) -> None:
    """Kernel-split guard: no Blazor/Fluent/Fluxor assembly may resolve into the output."""
    offenders = sorted(
        assembly.name
        for assembly in output_directory.glob("*.dll")
        if any(assembly.name.startswith(prefix) for prefix in FORBIDDEN_UI_ASSEMBLY_PREFIXES)
    )
    if offenders:
        raise ValueError(
            "Contracts-only consumer inherited UI runtime dependencies (kernel-split invariant "
            f"violated): {offenders}"
        )


def write_nuget_config(root: Path, package_directory: Path, additional_sources: list[str]) -> Path:
    """Add the local package directory while preserving inherited NuGet.Config sources."""
    config_file = root / "NuGet.Config"
    configuration = ElementTree.Element("configuration")
    package_sources = ElementTree.SubElement(configuration, "packageSources")
    ElementTree.SubElement(
        package_sources,
        "add",
        {"key": "local-frontcomposer-packages", "value": str(package_directory.resolve())},
    )
    for index, source in enumerate(additional_sources, start=1):
        ElementTree.SubElement(package_sources, "add", {"key": f"additional-source-{index}", "value": source})

    ElementTree.ElementTree(configuration).write(config_file, encoding="utf-8", xml_declaration=True)
    return config_file


def write_contracts_only_consumer(root: Path, version: str) -> Path:
    project_dir = root / "contracts-only-consumer"
    project_dir.mkdir(parents=True)
    project_file = project_dir / "ContractsOnlyConsumer.csproj"
    project_file.write_text(
        f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Hexalith.FrontComposer.Contracts" Version="{version}" />
  </ItemGroup>
</Project>
""",
        encoding="utf-8",
    )
    (project_dir / "Program.cs").write_text(
        """using Hexalith.FrontComposer.Contracts;

// A Contracts-only consumer touches only the UI-clean kernel surface.
string mapping = ContractsMetadata.TypographyMappingVersion;
if (mapping.Length == 0) {
    throw new InvalidOperationException("Hexalith.FrontComposer.Contracts public surface is unavailable.");
}

Console.WriteLine($"Contracts-only consumer resolved TypographyMappingVersion {mapping}.");
""",
        encoding="utf-8",
    )
    assert_package_only(project_file, ["Hexalith.FrontComposer.Contracts"])
    return project_file


def write_shell_ui_consumer(root: Path, version: str) -> Path:
    project_dir = root / "shell-ui-consumer"
    project_dir.mkdir(parents=True)
    project_file = project_dir / "ShellUiConsumer.csproj"
    project_file.write_text(
        f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Hexalith.FrontComposer.Contracts" Version="{version}" />
    <PackageReference Include="Hexalith.FrontComposer.Contracts.UI" Version="{version}" />
    <PackageReference Include="Hexalith.FrontComposer.Shell" Version="{version}" />
  </ItemGroup>
</Project>
""",
        encoding="utf-8",
    )
    (project_dir / "Program.cs").write_text(
        """using Hexalith.FrontComposer.Shell.Extensions;

using Microsoft.Extensions.DependencyInjection;

// A Shell/UI consumer references Contracts + Contracts.UI + Shell and composes the documented
// FrontComposer bootstrap surface (Blazor/Fluent runtime deps are expected here).
IServiceCollection services = new ServiceCollection();
services.AddHexalithFrontComposerQuickstart();

if (services.Count == 0) {
    throw new InvalidOperationException("Hexalith.FrontComposer Shell bootstrap surface is unavailable.");
}

Console.WriteLine($"Shell/UI consumer registered {services.Count} FrontComposer services.");
""",
        encoding="utf-8",
    )
    assert_package_only(
        project_file,
        [
            "Hexalith.FrontComposer.Contracts",
            "Hexalith.FrontComposer.Contracts.UI",
            "Hexalith.FrontComposer.Shell",
        ],
    )
    return project_file


def build_consumer(project_file: Path) -> Path:
    run_dotnet(["restore", str(project_file)], project_file.parent)
    # Keep -warnaserror for genuine compiler warnings against the public package surface, but do
    # not fail on NU1603: the local/CI smoke version (e.g. 0.0.0-ci-test) is stamped onto submodule
    # project references, so transitive dependencies legitimately resolve to a higher published
    # version. That version substitution is the expected consumer experience, not a defect.
    run_dotnet(
        ["build", str(project_file), "--no-restore", "--configuration", "Release", "-warnaserror", "-p:WarningsNotAsErrors=NU1603"],
        project_file.parent,
    )
    return project_file.parent / "bin" / "Release" / "net10.0"


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate package-only consumer restore/build experience.")
    parser.add_argument("package_directory", type=Path, help="Directory containing local FrontComposer .nupkg files.")
    parser.add_argument("--work-directory", type=Path, default=Path("/tmp/hexalith-frontcomposer-consumer-package-smoke"))
    parser.add_argument(
        "--nuget-source",
        action="append",
        default=[],
        help="Additional NuGet package source to add. May be supplied more than once; inherited NuGet.Config sources are preserved.",
    )
    args = parser.parse_args()

    package_directory = args.package_directory.resolve()
    versions = package_versions(package_directory)
    package_version = versions["Hexalith.FrontComposer.Contracts"]

    work_directory = args.work_directory.resolve()
    if work_directory.exists():
        shutil.rmtree(work_directory)
    work_directory.mkdir(parents=True)
    write_nuget_config(work_directory, package_directory, args.nuget_source)

    contracts_only_project = write_contracts_only_consumer(work_directory, package_version)
    shell_ui_project = write_shell_ui_consumer(work_directory, package_version)

    contracts_output = build_consumer(contracts_only_project)
    assert_no_ui_runtime_deps(contracts_output)
    build_consumer(shell_ui_project)

    print(f"Validated package-only consumer restore/build experience at {package_version}:")
    print("- Contracts-only consumer build (kernel-split: no Blazor/Fluent/Fluxor runtime deps)")
    print("- Shell/UI consumer build (Contracts + Contracts.UI + Shell bootstrap surface)")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as exc:
        print(f"Consumer package-reference validation failed with exit code {exc.returncode}.", file=sys.stderr)
        raise SystemExit(exc.returncode)
    except Exception as exc:  # noqa: BLE001 - command-line validator should print concise failures.
        print(f"Consumer package-reference validation failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
