"""Evaluate real AppHost edges to keep Memories source and package modes coherent."""

from __future__ import annotations

import json
from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
APPHOST = ROOT / "src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj"
UI = ROOT / "src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj"
TENANTS_UI = ROOT / "references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj"
PARTIES_UI = ROOT / "references/Hexalith.Parties/src/Hexalith.Parties.UI/Hexalith.Parties.UI.csproj"
MEMORIES_IDS = {"Hexalith.Memories.Contracts", "Hexalith.Memories.Client.Rest"}


def evaluate(project: Path, configuration: str | None, properties: dict[str, str] | None = None) -> dict:
    command = [
        "dotnet", "msbuild", str(project), "-nologo",
        "-getProperty:HexalithMemoriesFromSource,ProjectAssetsFile",
        "-getItem:ProjectReference,PackageReference",
    ]
    if configuration is not None:
        command.append(f"-p:Configuration={configuration}")
    command.extend(f"-p:{key}={value}" for key, value in (properties or {}).items())
    result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, timeout=90)
    if result.returncode:
        raise AssertionError(f"MSBuild evaluation failed: {result.stdout}\n{result.stderr}")
    return json.loads(result.stdout)


def module_reference(evaluation: dict, project: Path = TENANTS_UI) -> dict:
    return next(
        item for item in evaluation["Items"]["ProjectReference"]
        if Path(item["FullPath"]) == project
    )


def forwarded_properties(reference: dict) -> dict[str, str]:
    return dict(
        entry.split("=", 1)
        for entry in reference.get("AdditionalProperties", "").split(";") if entry
    )


class AppHostDependencyModeTests(unittest.TestCase):
    def test_debug_ui_paths_select_the_same_memories_source_projects(self) -> None:
        expected = {
            str(ROOT / f"references/Hexalith.Memories/src/{name}/{name}.csproj")
            for name in MEMORIES_IDS
        }
        assets = set()
        for parent in (APPHOST, UI, None):
            with self.subTest(parent=parent.name if parent else "restore evaluation"):
                properties = forwarded_properties(module_reference(evaluate(parent, "Debug"))) if parent else {}
                child = evaluate(TENANTS_UI, "Debug" if parent else None, properties)
                self.assertEqual(child["Properties"]["HexalithMemoriesFromSource"], "true")
                memories_projects = {
                    item["FullPath"] for item in child["Items"]["ProjectReference"]
                    if Path(item["FullPath"]).stem in MEMORIES_IDS
                }
                self.assertEqual(memories_projects, expected)
                self.assertFalse(MEMORIES_IDS.intersection(
                    item["Identity"] for item in child["Items"]["PackageReference"]
                ))
                assets.add(child["Properties"]["ProjectAssetsFile"])
        self.assertEqual(len(assets), 1, "All paths share restore outputs and must agree.")

    def assert_package_only(self, child: dict) -> None:
        self.assertTrue(MEMORIES_IDS.issubset(
            item["Identity"] for item in child["Items"]["PackageReference"]
        ))
        self.assertFalse(any(
            Path(item["FullPath"]).stem in MEMORIES_IDS
            for item in child["Items"]["ProjectReference"]
        ))

    def test_explicit_debug_package_mode_overrides_source_detection(self) -> None:
        child = evaluate(TENANTS_UI, "Debug", {"HexalithMemoriesFromSource": "false"})
        self.assert_package_only(child)

    def test_unavailable_memories_source_keeps_package_fallback(self) -> None:
        child = evaluate(TENANTS_UI, "Debug", {"HexalithMemoriesRoot": str(ROOT / "missing-memories-source")})
        self.assert_package_only(child)

    def test_default_restore_and_release_share_the_ui_source_choice(self) -> None:
        default = evaluate(TENANTS_UI, None)
        release = evaluate(TENANTS_UI, "Release")
        self.assertEqual(default["Properties"]["HexalithMemoriesFromSource"], "true")
        self.assertEqual(default["Properties"], release["Properties"])

    def test_ui_parent_edges_preserve_explicit_memories_package_selection(self) -> None:
        for parent in (APPHOST, UI):
            for project in (TENANTS_UI, PARTIES_UI):
                with self.subTest(parent=parent.name, project=project.name):
                    edge = module_reference(evaluate(parent, "Debug", {"HexalithMemoriesFromSource": "false"}), project)
                    properties = forwarded_properties(edge)
                    self.assertEqual(properties["HexalithMemoriesFromSource"], "false")
                    child = evaluate(project, "Debug", properties)
                    self.assertEqual(child["Properties"]["HexalithMemoriesFromSource"], "false")

    def test_debug_restore_contains_only_memories_project_libraries(self) -> None:
        with tempfile.TemporaryDirectory(prefix="frontcomposer-memories-restore-") as artifacts:
            result = subprocess.run(
                ["dotnet", "restore", str(TENANTS_UI), "--artifacts-path", artifacts, "-p:Configuration=Debug", "--nologo"],
                cwd=ROOT, capture_output=True, text=True, timeout=180,
            )
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            child = evaluate(TENANTS_UI, "Debug", {"ArtifactsPath": artifacts})
            libraries = json.loads(Path(child["Properties"]["ProjectAssetsFile"]).read_text())["libraries"]
            memories = [(name.split("/")[0], item["type"]) for name, item in libraries.items() if name.split("/")[0] in MEMORIES_IDS]
            self.assertCountEqual(memories, [(name, "project") for name in MEMORIES_IDS])

    def test_release_keeps_memories_packages_and_omits_debug_build_edges(self) -> None:
        host = evaluate(APPHOST, "Release")
        self.assertFalse(any(
            Path(item["FullPath"]) == TENANTS_UI
            for item in host["Items"]["ProjectReference"]
        ))
        child = evaluate(TENANTS_UI, "Release", {"UseNuGetDeps": "true", "HexalithFrontComposerFromSource": "false"})
        self.assertNotEqual(child["Properties"]["HexalithMemoriesFromSource"], "true")
        self.assert_package_only(child)


if __name__ == "__main__":
    unittest.main()
