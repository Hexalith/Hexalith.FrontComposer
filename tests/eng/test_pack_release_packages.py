#!/usr/bin/env python3
"""Focused tests for the semantic-release package execution plan."""

from __future__ import annotations

import json
import pathlib
import subprocess
import sys
import tempfile
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "eng" / "pack_release_packages.py"
SUPPRESSIONS = ROOT / "docs" / "diagnostics" / "compatibility-suppressions.json"
VALIDATION_PROPERTY = "-p:EnableFrontComposerPackageValidation=true"


class PackReleasePackagesTests(unittest.TestCase):
    @staticmethod
    def current_release_version(payload: dict[str, object]) -> str:
        return f"{str(payload['currentRelease'])[1:]}.0"

    @staticmethod
    def approved_v4_payload() -> dict[str, object]:
        return {
            "schemaVersion": "2.0",
            "currentRelease": "v4.0",
            "suppressions": [
                {"targetRelease": "v4.0", "expiresAfter": "v4.1"}
                for _ in range(26)
            ],
        }

    def run_plan(
        self,
        version: str,
        suppressions: pathlib.Path = SUPPRESSIONS,
        directory_build_targets: pathlib.Path | None = None,
        mcp_project: pathlib.Path | None = None,
        mcp_compatibility_suppressions: pathlib.Path | None = None,
    ) -> subprocess.CompletedProcess[str]:
        command = [
            sys.executable,
            str(SCRIPT),
            "--root",
            str(ROOT),
            "--version",
            version,
            "--output",
            str(ROOT / "unused-plan-output"),
            "--suppressions",
            str(suppressions),
            "--plan",
        ]
        if directory_build_targets is not None:
            command.extend(["--directory-build-targets", str(directory_build_targets)])
        if mcp_project is not None:
            command.extend(["--mcp-project", str(mcp_project)])
        if mcp_compatibility_suppressions is not None:
            command.extend(
                ["--mcp-compatibility-suppressions", str(mcp_compatibility_suppressions)]
            )
        return subprocess.run(
            command,
            cwd=ROOT,
            check=False,
            capture_output=True,
            text=True,
        )

    def test_plan_for_current_release_forwards_validation_to_every_command(self) -> None:
        ledger = json.loads(SUPPRESSIONS.read_text(encoding="utf-8"))
        version = f"{self.current_release_version(ledger)}-review.plan"
        result = self.run_plan(version)

        self.assertEqual(0, result.returncode, result.stderr)
        payload = json.loads(result.stdout)
        self.assertEqual(ledger["currentRelease"], payload["releaseLine"])
        self.assertEqual(8, len(payload["packages"]))
        self.assertEqual(16, len(payload["commands"]))
        self.assertEqual(8, sum(command[1] == "build" for command in payload["commands"]))
        self.assertEqual(8, sum(command[1] == "pack" for command in payload["commands"]))
        for command in payload["commands"]:
            self.assertIn(VALIDATION_PROPERTY, command)

    def test_plan_accepts_current_suppression_ledger(self) -> None:
        payload = json.loads(SUPPRESSIONS.read_text(encoding="utf-8"))
        result = self.run_plan(self.current_release_version(payload))

        self.assertEqual(0, result.returncode, result.stderr)

    def test_plan_rejects_release_line_that_differs_from_ledger(self) -> None:
        payload = json.loads(SUPPRESSIONS.read_text(encoding="utf-8"))
        requested_version = self.current_release_version(payload)
        requested_line = f"v{requested_version.rsplit('.', 1)[0]}"
        payload["currentRelease"] = "v9.9"

        with tempfile.TemporaryDirectory() as directory:
            ledger = pathlib.Path(directory) / "compatibility-suppressions.json"
            ledger.write_text(json.dumps(payload), encoding="utf-8")
            result = self.run_plan(requested_version, ledger)

        self.assertNotEqual(0, result.returncode)
        self.assertIn(
            f"--version release line {requested_line} does not match currentRelease v9.9",
            result.stderr,
        )

    def test_plan_rejects_non_array_suppression_ledger(self) -> None:
        payload = json.loads(SUPPRESSIONS.read_text(encoding="utf-8"))
        payload["suppressions"] = None

        with tempfile.TemporaryDirectory() as directory:
            ledger = pathlib.Path(directory) / "compatibility-suppressions.json"
            ledger.write_text(json.dumps(payload), encoding="utf-8")
            result = self.run_plan("3.2.0", ledger)

        self.assertNotEqual(0, result.returncode)
        self.assertIn("suppressions must be an array", result.stderr)

    def test_plan_rejects_suppression_before_its_target_release(self) -> None:
        payload = json.loads(SUPPRESSIONS.read_text(encoding="utf-8"))
        payload["suppressions"] = [
            {"targetRelease": "v3.3", "expiresAfter": "v4.0"}
        ]

        with tempfile.TemporaryDirectory() as directory:
            ledger = pathlib.Path(directory) / "compatibility-suppressions.json"
            ledger.write_text(json.dumps(payload), encoding="utf-8")
            result = self.run_plan("3.2.0", ledger)

        self.assertNotEqual(0, result.returncode)
        self.assertIn("targetRelease v3.3 is later than --version 3.2.0", result.stderr)

    def test_plan_rejects_suppression_at_its_expiry_release(self) -> None:
        payload = json.loads(SUPPRESSIONS.read_text(encoding="utf-8"))
        payload["suppressions"] = [
            {"targetRelease": "v3.0", "expiresAfter": "v3.2"}
        ]

        with tempfile.TemporaryDirectory() as directory:
            ledger = pathlib.Path(directory) / "compatibility-suppressions.json"
            ledger.write_text(json.dumps(payload), encoding="utf-8")
            result = self.run_plan("3.2.0", ledger)

        self.assertNotEqual(0, result.returncode)
        self.assertIn("expiresAfter v3.2 has been reached by --version 3.2.0", result.stderr)

    def test_v4_plan_rejects_suppressions_before_target_release(self) -> None:
        payload = self.approved_v4_payload()

        with tempfile.TemporaryDirectory() as directory:
            ledger = pathlib.Path(directory) / "compatibility-suppressions.json"
            ledger.write_text(json.dumps(payload), encoding="utf-8")
            result = self.run_plan("3.1.1", ledger)

        self.assertNotEqual(0, result.returncode)
        self.assertIn("targetRelease v4.0 is later than --version 3.1.1", result.stderr)

    def test_v4_plan_accepts_exact_approved_release_line(self) -> None:
        payload = self.approved_v4_payload()
        self.assertEqual(26, len(payload["suppressions"]))

        with tempfile.TemporaryDirectory() as directory:
            ledger = pathlib.Path(directory) / "compatibility-suppressions.json"
            ledger.write_text(json.dumps(payload), encoding="utf-8")
            result = self.run_plan("4.0.0-review.1117c", ledger)

        self.assertEqual(0, result.returncode, result.stderr)

    def test_v41_plan_rejects_stale_v4_suppressions_until_removed(self) -> None:
        payload = self.approved_v4_payload()

        with tempfile.TemporaryDirectory() as directory:
            fixture_root = pathlib.Path(directory)
            ledger = fixture_root / "compatibility-suppressions.json"
            directory_targets = fixture_root / "Directory.Build.targets"
            mcp_project = fixture_root / "Hexalith.FrontComposer.Mcp.csproj"
            mcp_suppressions = fixture_root / "CompatibilitySuppressions.xml"
            ledger.write_text(json.dumps(payload), encoding="utf-8")
            stale_result = self.run_plan("4.1.0", ledger)

            payload["currentRelease"] = "v4.1"
            payload["suppressions"] = []
            ledger.write_text(json.dumps(payload), encoding="utf-8")
            incomplete_cleanup_result = self.run_plan("4.1.0", ledger)

            directory_targets.write_text(
                "<Project><PropertyGroup>"
                "<FrontComposerPackageValidationBaselineVersion>4.0.0"
                "</FrontComposerPackageValidationBaselineVersion>"
                "</PropertyGroup></Project>",
                encoding="utf-8",
            )
            mcp_project.write_text("<Project />", encoding="utf-8")
            mcp_suppressions.write_text(
                "<Suppressions><Suppression>"
                "<Target>T:Hexalith.FrontComposer.Mcp.Skills.SkillBenchmarkPrompt</Target>"
                "</Suppression></Suppressions>",
                encoding="utf-8",
            )
            stale_xml_result = self.run_plan(
                "4.1.0",
                ledger,
                directory_targets,
                mcp_project,
                mcp_suppressions,
            )

            mcp_suppressions.write_text("<Suppressions />", encoding="utf-8")
            advanced_result = self.run_plan(
                "4.1.0",
                ledger,
                directory_targets,
                mcp_project,
                mcp_suppressions,
            )

        self.assertNotEqual(0, stale_result.returncode)
        self.assertIn("expiresAfter v4.1 has been reached by --version 4.1.0", stale_result.stderr)
        self.assertNotEqual(0, incomplete_cleanup_result.returncode)
        self.assertIn("package-validation baseline must be 4.0.0", incomplete_cleanup_result.stderr)
        self.assertNotEqual(0, stale_xml_result.returncode)
        self.assertIn("remove the 1 expired MCP benchmark-removal suppressions", stale_xml_result.stderr)
        self.assertEqual(0, advanced_result.returncode, advanced_result.stderr)


if __name__ == "__main__":
    unittest.main()
