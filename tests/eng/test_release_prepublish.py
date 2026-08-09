#!/usr/bin/env python3
"""Runtime contracts for unsigned release-candidate transfer and NuGet pushes."""

from __future__ import annotations

import pathlib
import shutil
import sys
import tempfile
import types
import unittest
import zipfile
from unittest import mock

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))

import release_contract  # noqa: E402
import release_prepublish as prepublish  # noqa: E402


class ReleasePrepublishTests(unittest.TestCase):
    version = "2.0.0"

    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self._temporary.name)
        self._original_root = prepublish.REPO_ROOT
        prepublish.REPO_ROOT = self.root
        for package_id, _ in release_contract.EXPECTED_PACKAGES:
            package = self.root / "nupkgs" / f"{package_id}.{self.version}.nupkg"
            package.parent.mkdir(parents=True, exist_ok=True)
            with zipfile.ZipFile(package, "w", compression=zipfile.ZIP_DEFLATED) as archive:
                archive.writestr(f"{package_id}.nuspec", b"<package />")
                archive.writestr(f"lib/net10.0/{package_id}.dll", package_id.encode("utf-8"))
            (package.parent / f"{package_id}.{self.version}.snupkg").write_bytes(b"symbols")
        evidence = self.root / "release-evidence/release-readiness.json"
        evidence.parent.mkdir(parents=True, exist_ok=True)
        evidence.write_text('{"classification":"ready"}\n', encoding="utf-8")

    def tearDown(self) -> None:
        prepublish.REPO_ROOT = self._original_root
        self._temporary.cleanup()

    def test_bundle_descriptor_round_trip_authenticates_unsigned_packages_symbols_and_evidence(self) -> None:
        environment = {
            "GITHUB_SHA": "a" * 40,
            "GITHUB_RUN_ID": "42",
            "GITHUB_RUN_ATTEMPT": "3",
        }
        with mock.patch.dict(prepublish.os.environ, environment, clear=False):
            result = prepublish.cmd_bundle(types.SimpleNamespace(version=self.version))
            descriptor = prepublish._validate_candidate_descriptor(self.root, self.version)

        self.assertEqual(0, result)
        paths = {row["path"] for row in descriptor["files"]}
        self.assertEqual(8, len([path for path in paths if path.endswith(".nupkg")]))
        self.assertEqual(8, len([path for path in paths if path.endswith(".snupkg")]))
        self.assertIn("release-evidence/release-readiness.json", paths)
        self.assertFalse(any(path.startswith("nupkgs-signed/") for path in paths))

        restored = self.root.parent / f"{self.root.name}-restored"
        try:
            shutil.copytree(self.root, restored)
            with mock.patch.dict(prepublish.os.environ, environment, clear=False):
                prepublish._validate_candidate_descriptor(restored, self.version)
        finally:
            shutil.rmtree(restored, ignore_errors=True)

    def test_bundle_rejects_signed_or_nested_candidate_before_transfer(self) -> None:
        package_id = release_contract.EXPECTED_PACKAGES[0][0]
        package = self.root / "nupkgs" / f"{package_id}.{self.version}.nupkg"
        with zipfile.ZipFile(package, "a", compression=zipfile.ZIP_DEFLATED) as archive:
            archive.writestr(release_contract.NUGET_SIGNATURE_MEMBER, b"author signature")
        with mock.patch.dict(prepublish.os.environ, {
            "GITHUB_SHA": "a" * 40,
            "GITHUB_RUN_ID": "42",
            "GITHUB_RUN_ATTEMPT": "3",
        }, clear=False):
            with self.assertRaises(prepublish.PhaseFailure):
                prepublish.cmd_bundle(types.SimpleNamespace(version=self.version))

        with zipfile.ZipFile(package, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            archive.writestr(f"{package_id}.nuspec", b"<package />")
        nested = self.root / "nupkgs/nested/Injected.2.0.0.nupkg"
        nested.parent.mkdir(parents=True)
        shutil.copyfile(package, nested)
        with self.assertRaises(prepublish.PhaseFailure):
            prepublish._validate_unsigned_candidates(self.root, "bundle")

    def test_package_push_suppresses_automatic_symbols_but_symbol_push_does_not(self) -> None:
        package = pathlib.Path("nupkgs/Package.2.0.0.nupkg")
        symbols = pathlib.Path("nupkgs/Package.2.0.0.snupkg")
        package_command = prepublish._nuget_push_command("package-push", package, "secret")
        symbol_command = prepublish._nuget_push_command("symbol-push", symbols, "secret")
        self.assertIn("--no-symbols", package_command)
        self.assertNotIn("--no-symbols", symbol_command)

    def test_phase_build_restore_enables_package_validation_baseline_cache(self) -> None:
        # prepare-candidate runs Contract package-boundary tests that require the
        # published PackageValidationBaselineVersion packages in NUGET_PACKAGES.
        source = (ROOT / "eng" / "release_prepublish.py").read_text(encoding="utf-8")
        restore_idx = source.index('dotnet", "restore"')
        validation_idx = source.index(
            "-p:EnableFrontComposerPackageValidation=true",
            restore_idx,
        )
        self.assertLess(restore_idx, validation_idx)

    def test_verify_prepared_and_publish_are_sealed_readiness_only(self) -> None:
        source = (ROOT / "eng" / "release_prepublish.py").read_text(encoding="utf-8")
        verify_start = source.index("def cmd_verify_prepared")
        publish_start = source.index("def cmd_publish")
        main_start = source.index("\ndef main")
        verify_section = source[verify_start:publish_start]
        publish_section = source[publish_start:main_start]
        self.assertIn("Sealed-readiness-only", verify_section)
        self.assertIn("Sealed-readiness-only", publish_section)
        # Executable invocations must not re-classify; comments may name the banned command.
        self.assertNotIn('"classify-release"', verify_section)
        self.assertNotIn("'classify-release'", verify_section)
        self.assertNotIn('"classify-release"', publish_section)
        self.assertNotIn("'classify-release'", publish_section)
        self.assertIn("release-readiness.json", verify_section)
        self.assertIn("release-readiness.json", publish_section)
        docstring = source.split('"""', 2)[1]
        self.assertIn("prepare`` + ``bundle", docstring)
        self.assertIn("verifyReleaseCmd", docstring)
        self.assertIn("sealed-readiness-only", docstring)
        self.assertNotIn("nupkgs-signed", docstring)

    def test_unsigned_candidate_validation_remains_on_prepare_bundle_restore(self) -> None:
        source = (ROOT / "eng" / "release_prepublish.py").read_text(encoding="utf-8")
        self.assertIn('_validate_unsigned_candidates(REPO_ROOT, "pack-once")', source)
        self.assertIn('_validate_unsigned_candidates(REPO_ROOT, "bundle")', source)
        self.assertIn('_validate_unsigned_candidates(base, "restore")', source)
        bundle_start = source.index("def cmd_bundle")
        descriptor_start = source.index("def _validate_candidate_descriptor")
        restore_start = source.index("def cmd_restore")
        verify_start = source.index("def cmd_verify_prepared")
        self.assertIn('_validate_unsigned_candidates(REPO_ROOT, "bundle")', source[bundle_start:descriptor_start])
        self.assertIn('_validate_unsigned_candidates(base, "restore")', source[descriptor_start:restore_start])
        self.assertIn("_validate_candidate_descriptor", source[restore_start:verify_start])


if __name__ == "__main__":
    unittest.main()
