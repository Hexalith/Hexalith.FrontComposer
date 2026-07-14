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
    def run_plan(
        self,
        version: str,
        suppressions: pathlib.Path = SUPPRESSIONS,
    ) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
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
            ],
            cwd=ROOT,
            check=False,
            capture_output=True,
            text=True,
        )

    def test_plan_for_current_release_forwards_validation_to_every_command(self) -> None:
        result = self.run_plan("3.2.0-review.plan")

        self.assertEqual(0, result.returncode, result.stderr)
        payload = json.loads(result.stdout)
        self.assertEqual("v3.2", payload["releaseLine"])
        self.assertEqual(8, len(payload["packages"]))
        self.assertEqual(16, len(payload["commands"]))
        self.assertEqual(8, sum(command[1] == "build" for command in payload["commands"]))
        self.assertEqual(8, sum(command[1] == "pack" for command in payload["commands"]))
        for command in payload["commands"]:
            self.assertIn(VALIDATION_PROPERTY, command)

    def test_plan_accepts_empty_current_suppression_ledger(self) -> None:
        payload = json.loads(SUPPRESSIONS.read_text(encoding="utf-8"))
        self.assertEqual([], payload["suppressions"])

        result = self.run_plan("3.2.0")

        self.assertEqual(0, result.returncode, result.stderr)

    def test_plan_rejects_release_line_that_differs_from_ledger(self) -> None:
        payload = json.loads(SUPPRESSIONS.read_text(encoding="utf-8"))
        payload["currentRelease"] = "v3.3"

        with tempfile.TemporaryDirectory() as directory:
            ledger = pathlib.Path(directory) / "compatibility-suppressions.json"
            ledger.write_text(json.dumps(payload), encoding="utf-8")
            result = self.run_plan("3.2.0", ledger)

        self.assertNotEqual(0, result.returncode)
        self.assertIn(
            "--version release line v3.2 does not match currentRelease v3.3",
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


if __name__ == "__main__":
    unittest.main()
