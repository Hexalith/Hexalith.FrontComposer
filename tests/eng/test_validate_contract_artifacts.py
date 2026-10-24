"""Regression tests for the redaction scan in eng/validate-contract-artifacts.ps1.

Each case runs the real PowerShell validator against a temporary copy of the committed Pact
folder, so the repository's contract files are never modified.
"""

from __future__ import annotations

import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
VALIDATOR = ROOT / "eng" / "validate-contract-artifacts.ps1"
CANONICAL_PACTS = ROOT / "tests" / "Hexalith.FrontComposer.Shell.Tests" / "Pact"
# A real SHA-256 value; known hashes are allowlisted only under named hash fields.
KNOWN_SHA256 = "9d074dfd0758a8934f122aab18659627dff1cf5d4c3e548b222cc0d79a881065"


def _read_json(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(value, dict):
        raise AssertionError(f"Expected one JSON object in {path}")
    return value


def _write_json(path: Path, value: dict[str, Any]) -> None:
    path.write_text(json.dumps(value, separators=(",", ":")) + "\n", encoding="utf-8")


class ValidateContractArtifactsRedactionTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory(prefix="validate-contract-artifacts-")
        self.addCleanup(self._temporary.cleanup)
        self.temporary_root = Path(self._temporary.name)
        self.pact_root = self.temporary_root / "Pact"
        shutil.copytree(CANONICAL_PACTS, self.pact_root, ignore=shutil.ignore_patterns("*.cs"))
        self.manifest_path = self.pact_root / "interaction-manifest.json"

    def run_validator(self, *extra: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                "pwsh",
                "-NoProfile",
                "-File",
                str(VALIDATOR),
                "-PactDir",
                str(self.pact_root),
                "-ArtifactDir",
                str(self.temporary_root / "contract-artifacts"),
                *extra,
            ],
            cwd=ROOT,
            check=False,
            capture_output=True,
            text=True,
        )

    def write_provider_report(self, value: dict[str, Any]) -> Path:
        report = self.temporary_root / "provider-verification.json"
        _write_json(report, value)
        return report

    def test_unmodified_contracts_and_clean_provider_report_pass(self) -> None:
        # Control for the failure cases below: the copied contract set and a clean report pass.
        report = self.write_provider_report({"finalVerdict": "passed", "sha256": KNOWN_SHA256})

        result = self.run_validator("-RequireProviderVerification", "-ProviderVerificationReport", str(report))

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        summary = (self.temporary_root / "contract-artifacts" / "job-summary.md").read_text(encoding="utf-8-sig")
        self.assertIn("Provider verification report: PRESENT_AND_CLEAN", summary)

    def test_contract_redaction_does_not_globally_allowlist_known_sha256_value(self) -> None:
        manifest = _read_json(self.manifest_path)
        manifest["diagnosticToken"] = KNOWN_SHA256
        _write_json(self.manifest_path, manifest)

        result = self.run_validator()

        self.assertNotEqual(result.returncode, 0)
        output = result.stdout + result.stderr
        self.assertIn("encoded token-like", output)
        self.assertIn("payload", output)

    def test_powershell_redaction_matches_new_secret_keys_and_base64url(self) -> None:
        manifest = _read_json(self.manifest_path)
        manifest["client_secret"] = "opaque"
        manifest["private_key"] = "opaque"
        manifest["sas_token"] = "opaque"
        manifest["reasonToken"] = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_abcd"
        _write_json(self.manifest_path, manifest)

        result = self.run_validator()

        output = result.stdout + result.stderr
        self.assertNotEqual(result.returncode, 0)
        for expected in ("client", "private", "sas", "encoded token-like"):
            self.assertIn(expected, output)

    def test_required_provider_report_that_leaks_a_secret_fails(self) -> None:
        report = self.write_provider_report({"finalVerdict": "passed", "client_secret": "opaque"})

        result = self.run_validator("-RequireProviderVerification", "-ProviderVerificationReport", str(report))

        self.assertNotEqual(result.returncode, 0)
        output = result.stdout + result.stderr
        self.assertIn("Redaction scan failed for provider verification report", output)
        self.assertIn("client", output)
        summary = (self.temporary_root / "contract-artifacts" / "job-summary.md").read_text(encoding="utf-8-sig")
        self.assertIn("Provider verification report: REQUIRED_REJECTED", summary)


if __name__ == "__main__":
    unittest.main()
