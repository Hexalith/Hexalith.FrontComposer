from __future__ import annotations

import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / ".github/scripts/ci_governance.py"
FIXTURES = ROOT / "tests/ci-governance/fixtures/mtp-quarantine"


class QuarantineMtpEvidenceTests(unittest.TestCase):
    def _summarize(self, fixture: str | None) -> tuple[subprocess.CompletedProcess[str], dict, str]:
        with tempfile.TemporaryDirectory() as temporary:
            work = Path(temporary)
            results = work / "results"
            if fixture is None:
                results.mkdir()
            else:
                shutil.copytree(FIXTURES / fixture, results)
            markdown = work / "summary.md"
            payload = work / "summary.json"
            completed = subprocess.run(
                [
                    "python3",
                    str(SCRIPT),
                    "summarize-quarantine",
                    "--results-dir",
                    str(results),
                    "--markdown",
                    str(markdown),
                    "--json",
                    str(payload),
                    "--sha",
                    "a" * 40,
                ],
                cwd=ROOT,
                text=True,
                capture_output=True,
                check=False,
            )
            return completed, json.loads(payload.read_text(encoding="utf-8")), markdown.read_text(encoding="utf-8")

    def test_nested_unique_mtp_reports_aggregate_all_outcomes(self) -> None:
        completed, payload, markdown = self._summarize("nested-a")
        with tempfile.TemporaryDirectory() as temporary:
            combined = Path(temporary)
            shutil.copytree(FIXTURES / "nested-a", combined / "a")
            shutil.copytree(FIXTURES / "nested-b", combined / "b")
            output = combined / "output"
            output.mkdir()
            process = subprocess.run(
                [
                    "python3",
                    str(SCRIPT),
                    "summarize-quarantine",
                    "--results-dir",
                    str(combined),
                    "--markdown",
                    str(output / "summary.md"),
                    "--json",
                    str(output / "summary.json"),
                ],
                cwd=ROOT,
                text=True,
                capture_output=True,
                check=False,
            )
            combined_payload = json.loads((output / "summary.json").read_text(encoding="utf-8"))

        self.assertEqual(0, completed.returncode, completed.stderr)
        self.assertEqual(2, payload["total"])
        self.assertEqual(0, process.returncode, process.stderr)
        self.assertEqual("advisory quarantine failure", combined_payload["classification"])
        self.assertEqual(3, combined_payload["total"])
        self.assertEqual(2, combined_payload["passed"])
        self.assertEqual(1, combined_payload["failed"])
        self.assertEqual(
            {"Fixtures.ModuleA.Passes", "Fixtures.ModuleA.Fails", "Fixtures.ModuleB.AlsoPasses"},
            {result["identity"] for result in combined_payload["results"]},
        )
        self.assertIn("Total: 2", markdown)

    def test_present_zero_test_mtp_report_is_valid_zero_quarantined(self) -> None:
        completed, payload, markdown = self._summarize("zero")

        self.assertEqual(0, completed.returncode, completed.stderr)
        self.assertEqual("zero-quarantined", payload["classification"])
        self.assertEqual(0, payload["total"])
        self.assertEqual([], payload["diagnostics"])
        self.assertIn("Classification: **zero-quarantined**", markdown)

    def test_missing_trx_is_invalid_missing_evidence(self) -> None:
        completed, payload, markdown = self._summarize(None)

        self.assertEqual(1, completed.returncode)
        self.assertEqual("missing evidence", payload["classification"])
        self.assertIn("No quarantine TRX files were found.", payload["diagnostics"])
        self.assertIn("no valid execution evidence", markdown)

    def test_malformed_trx_is_invalid_evidence(self) -> None:
        completed, payload, markdown = self._summarize("malformed")

        self.assertEqual(1, completed.returncode)
        self.assertEqual("invalid evidence", payload["classification"])
        self.assertTrue(any("root element must be TestRun" in item for item in payload["diagnostics"]))
        self.assertIn("Invalid evidence", markdown)


class MtpLaneEvidenceTests(unittest.TestCase):
    def _validate(
        self,
        fixture_names: tuple[str, ...],
        *arguments: str,
        coverage: str | None = None,
    ) -> tuple[subprocess.CompletedProcess[str], dict]:
        with tempfile.TemporaryDirectory() as temporary:
            work = Path(temporary)
            results = work / "results"
            results.mkdir()
            for index, fixture_name in enumerate(fixture_names):
                shutil.copytree(FIXTURES / fixture_name, results / f"nested-{index}")
            command = [
                "python3",
                str(SCRIPT),
                "validate-mtp-evidence",
                "--results-dir",
                str(results),
                *arguments,
            ]
            if coverage is not None:
                coverage_dir = work / "coverage"
                coverage_dir.mkdir()
                (coverage_dir / "module.cobertura.xml").write_text(coverage, encoding="utf-8")
                command.extend(
                    [
                        "--coverage-dir",
                        str(coverage_dir),
                        "--expected-coverage-files",
                        "1",
                    ]
                )
            completed = subprocess.run(
                command,
                cwd=ROOT,
                text=True,
                capture_output=True,
                check=False,
            )
            return completed, json.loads(completed.stdout)

    def test_nested_reports_are_distinct_modules_with_nonzero_aggregate(self) -> None:
        completed, payload = self._validate(
            ("nested-a", "nested-b"),
            "--expected-trx-files",
            "2",
            "--require-tests",
            "--require-distinct-modules",
        )

        self.assertEqual(0, completed.returncode, completed.stderr)
        self.assertEqual(3, payload["total"])
        self.assertEqual(["module-a.dll", "module-b.dll"], payload["modules"])

    def test_missing_malformed_and_zero_test_reports_fail_closed(self) -> None:
        cases = (
            ((), ("--expected-trx-files", "1"), "expected 1 distinct TRX files"),
            (("malformed",), ("--require-tests",), "root element must be TestRun"),
            (("zero",), ("--require-tests",), "aggregate TRX total must be greater than zero"),
        )
        for fixtures, arguments, diagnostic in cases:
            with self.subTest(diagnostic=diagnostic):
                completed, payload = self._validate(fixtures, *arguments)
                self.assertEqual(1, completed.returncode)
                self.assertTrue(any(diagnostic in item for item in payload["diagnostics"]), payload)

    def test_coverage_requires_parseable_report_with_measured_lines(self) -> None:
        valid = """<?xml version="1.0"?><coverage lines-valid="1"><packages><package><classes><class><lines><line number="1" hits="1" /></lines></class></classes></package></packages></coverage>"""
        completed, payload = self._validate(("nested-a",), "--require-tests", coverage=valid)
        self.assertEqual(0, completed.returncode, payload)

        for coverage in ("<not-coverage />", "<coverage lines-valid=\"0\" />", "<coverage"):
            with self.subTest(coverage=coverage):
                completed, payload = self._validate(("nested-a",), "--require-tests", coverage=coverage)
                self.assertEqual(1, completed.returncode)
                self.assertTrue(
                    any("malformed or empty Cobertura" in item for item in payload["diagnostics"]),
                    payload,
                )


if __name__ == "__main__":
    unittest.main()
