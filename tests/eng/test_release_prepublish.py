#!/usr/bin/env python3
"""Runtime contracts for unsigned release-candidate transfer and NuGet pushes."""

from __future__ import annotations

import pathlib
import shutil
import subprocess
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

    def test_phase_build_seals_identical_version_and_validation_properties(self) -> None:
        version = "4.2.0-review.compat"
        with mock.patch.object(prepublish, "run") as run:
            prepublish.phase_build(version)

        self.assertEqual(3, run.call_count)
        for call in run.call_args_list:
            command = call.args[1]
            self.assertIn(f"-p:Version={version}", command)
            self.assertIn(f"-p:PackageVersion={version}", command)
            self.assertIn("-p:ContinuousIntegrationBuild=true", command)
            self.assertIn("-p:EnableFrontComposerPackageValidation=true", command)
            self.assertIn("-p:FrontComposerPackageValidationBaselineVersion=4.1.1", command)
            self.assertIn("-p:FrontComposerPackageValidationSkipBaseline=false", command)

    def test_prepare_rejects_stale_policy_before_build_or_package_cleanup(self) -> None:
        sentinel = self.root / "nupkgs" / "existing-output.nupkg"
        sentinel.write_bytes(b"preserve")
        with mock.patch.object(
            prepublish,
            "validate_release_policy",
            side_effect=prepublish.ReleaseCompatibilityError("stale currentRelease"),
        ), mock.patch.object(prepublish, "run") as run:
            with self.assertRaises(prepublish.PhaseFailure):
                prepublish.cmd_prepare(types.SimpleNamespace(
                    version="4.2.0-review.compat",
                    non_publishing=True,
                ))

        run.assert_not_called()
        self.assertEqual(b"preserve", sentinel.read_bytes())

    def test_prepare_wraps_invalid_candidate_semver_before_build_or_cleanup(self) -> None:
        sentinel = self.root / "nupkgs" / "existing-output.nupkg"
        sentinel.write_bytes(b"preserve")
        with mock.patch.object(prepublish, "run") as run:
            with self.assertRaisesRegex(prepublish.PhaseFailure, "strict SemVer"):
                prepublish.cmd_prepare(types.SimpleNamespace(
                    version="4.2",
                    non_publishing=True,
                ))

        run.assert_not_called()
        self.assertEqual(b"preserve", sentinel.read_bytes())

    def test_phase_pack_rechecks_policy_in_live_packer_before_output_mutation(self) -> None:
        sentinel = self.root / "nupkgs" / "existing-output.nupkg"
        sentinel.write_bytes(b"preserve")
        with mock.patch.object(prepublish, "run") as run, \
                mock.patch.object(prepublish, "packable_rows", return_value=[]), \
                mock.patch.object(prepublish, "_validate_unsigned_candidates"):
            prepublish.phase_pack("4.2.0-review.compat")

        self.assertEqual(2, run.call_count)
        pack_command = run.call_args_list[0].args[1]
        verifier_command = run.call_args_list[1].args[1]
        self.assertEqual("scripts/pack-release-packages.py", pack_command[1])
        self.assertIn("--release-policy", pack_command)
        self.assertEqual(["dotnet", "run", "--file"], verifier_command[:3])
        self.assertIn("eng/verify-candidate-packages.cs", verifier_command)
        self.assertIn("4.2.0-review.compat", verifier_command)
        self.assertEqual(b"preserve", sentinel.read_bytes())

    def test_phase_pack_stops_before_candidate_sealing_when_version_verifier_fails(self) -> None:
        verifier_failure = prepublish.PhaseFailure("package-version", "metadata mismatch")
        with mock.patch.object(prepublish, "run", side_effect=[None, verifier_failure]) as run, \
                mock.patch.object(prepublish, "packable_rows", return_value=[]), \
                mock.patch.object(prepublish, "_validate_unsigned_candidates") as unsigned:
            with self.assertRaisesRegex(prepublish.PhaseFailure, "metadata mismatch"):
                prepublish.phase_pack("4.2.0-review.compat")

        self.assertEqual(2, run.call_count)
        unsigned.assert_not_called()

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

    def test_phase_tests_uses_gate_3a_filter_and_fail_closed_run(self) -> None:
        source = (ROOT / "eng" / "release_prepublish.py").read_text(encoding="utf-8")
        tests_start = source.index("def phase_tests")
        tests_end = source.index("\ndef ", tests_start + 1)
        tests_section = source[tests_start:tests_end]
        self.assertIn('run("tests"', tests_section)
        self.assertIn(
            '"--filter", "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined",',
            tests_section,
        )
        self.assertNotIn(
            '"--filter", "Category!=Quarantined",',
            tests_section,
        )
        self.assertNotIn("tolerate_failure=True", tests_section)
        run_start = source.index("def run(")
        run_end = source.index("\ndef ", run_start + 1)
        run_section = source[run_start:run_end]
        self.assertIn("raise PhaseFailure", run_section)
        self.assertIn("not tolerate_failure", run_section)


class CandidatePackageVersionVerifierTests(unittest.TestCase):
    version = "4.2.0-review.fixture+build.7"

    def test_verifier_inspects_all_packages_and_multitarget_assembly_copies_fail_closed(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            project = root / "fixture" / "Fixture.csproj"
            project.parent.mkdir(parents=True)
            project.write_text(
                "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>"
                "<TargetFramework>net10.0</TargetFramework>"
                "<AssemblyName>Fixture</AssemblyName>"
                "</PropertyGroup></Project>",
                encoding="utf-8",
            )
            (project.parent / "Fixture.cs").write_text("public sealed class Fixture { }\n", encoding="utf-8")
            assembly_output = root / "assembly"
            build = subprocess.run(
                [
                    "dotnet", "build", str(project), "--configuration", "Release",
                    "--output", str(assembly_output),
                    f"-p:Version={self.version}",
                    f"-p:PackageVersion={self.version}",
                    "-p:ContinuousIntegrationBuild=true",
                    "--nologo",
                ],
                cwd=ROOT,
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertEqual(0, build.returncode, build.stdout + build.stderr)
            assembly = assembly_output / "Fixture.dll"
            self.assertTrue(assembly.is_file())

            packages = root / "nupkgs"
            packages.mkdir()
            for package_id, _ in release_contract.EXPECTED_PACKAGES:
                self.write_candidate(packages, package_id, assembly, self.version)

            command = [
                "dotnet", "run", "--file", str(ROOT / "eng/verify-candidate-packages.cs"), "--",
                str(packages), str(ROOT / "eng/release-package-inventory.json"), self.version,
            ]
            verified = subprocess.run(
                command,
                cwd=ROOT,
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertEqual(0, verified.returncode, verified.stdout + verified.stderr)
            self.assertIn("8 candidate packages and 10 primary assembly copies", verified.stdout)

            first_id = release_contract.EXPECTED_PACKAGES[0][0]
            self.write_candidate(packages, first_id, assembly, "4.2.0-wrong")
            rejected = subprocess.run(
                command,
                cwd=ROOT,
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertNotEqual(0, rejected.returncode)
            self.assertIn("nuspec id/version", rejected.stderr)

    @staticmethod
    def write_candidate(
        package_directory: pathlib.Path,
        package_id: str,
        assembly: pathlib.Path,
        nuspec_version: str,
    ) -> None:
        candidate_version = CandidatePackageVersionVerifierTests.version
        package = package_directory / f"{package_id}.{candidate_version}.nupkg"
        if package_id == "Hexalith.FrontComposer.Cli":
            assembly_paths = [f"tools/net10.0/any/{package_id}.dll"]
        elif package_id == "Hexalith.FrontComposer.SourceTools":
            assembly_paths = [f"analyzers/dotnet/cs/{package_id}.dll"]
        elif package_id in {
            "Hexalith.FrontComposer.Contracts",
            "Hexalith.FrontComposer.Schema",
        }:
            assembly_paths = [
                f"lib/net10.0/{package_id}.dll",
                f"lib/netstandard2.0/{package_id}.dll",
            ]
        else:
            assembly_paths = [f"lib/net10.0/{package_id}.dll"]
        nuspec = (
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
            "<package><metadata>"
            f"<id>{package_id}</id><version>{nuspec_version}</version>"
            "</metadata></package>"
        )
        with zipfile.ZipFile(package, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            archive.writestr(f"{package_id}.nuspec", nuspec)
            for assembly_path in assembly_paths:
                archive.write(assembly, assembly_path)


if __name__ == "__main__":
    unittest.main()
