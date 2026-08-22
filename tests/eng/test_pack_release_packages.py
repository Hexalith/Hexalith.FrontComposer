#!/usr/bin/env python3
"""Focused tests for the live release packer and compatibility lifecycle policy."""

from __future__ import annotations

import importlib.util
import json
import pathlib
import subprocess
import sys
import tempfile
import unittest
from unittest import mock


ROOT = pathlib.Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "scripts" / "pack-release-packages.py"
RETIRED_SCRIPT = ROOT / "eng" / "pack_release_packages.py"
sys.path.insert(0, str(ROOT / "eng"))

import release_compatibility  # noqa: E402
from release_compatibility import (  # noqa: E402
    PUBLISHED_BASELINE_VERSION,
    ReleaseCompatibilityError,
    validate_release_policy,
)


VALIDATION_PROPERTY = "-p:EnableFrontComposerPackageValidation=true"
BASELINE_PROPERTY = "-p:FrontComposerPackageValidationBaselineVersion=4.1.1"
SKIP_BASELINE_PROPERTY = "-p:FrontComposerPackageValidationSkipBaseline=false"
VERSION = "4.2.0-review.compat"


class PackReleasePackagesTests(unittest.TestCase):
    def run_plan(self, version: str, *, release_policy: bool) -> subprocess.CompletedProcess[str]:
        command = [sys.executable, str(SCRIPT), str(ROOT / "unused-plan-output"), version]
        if release_policy:
            command.append("--release-policy")
        command.append("--plan")
        return subprocess.run(command, cwd=ROOT, check=False, capture_output=True, text=True)

    def test_production_plan_rechecks_policy_and_validates_every_live_pack(self) -> None:
        result = self.run_plan(VERSION, release_policy=True)

        self.assertEqual(0, result.returncode, result.stderr)
        payload = json.loads(result.stdout)
        self.assertTrue(payload["releasePolicy"])
        self.assertEqual("v4.2", payload["releaseLine"])
        self.assertEqual(8, len(payload["commands"]))
        for command in payload["commands"]:
            self.assertEqual(["dotnet", "pack"], command[:2])
            self.assertIn("--no-build", command)
            self.assertIn(f"-p:Version={VERSION}", command)
            self.assertIn(f"-p:PackageVersion={VERSION}", command)
            self.assertIn("-p:ContinuousIntegrationBuild=true", command)
            self.assertIn(VALIDATION_PROPERTY, command)
            self.assertIn(BASELINE_PROPERTY, command)
            self.assertIn(SKIP_BASELINE_PROPERTY, command)

    def test_synthetic_ci_positional_contract_skips_only_release_line_matching(self) -> None:
        result = self.run_plan("0.0.0-ci-test", release_policy=False)

        self.assertEqual(0, result.returncode, result.stderr)
        payload = json.loads(result.stdout)
        self.assertFalse(payload["releasePolicy"])
        self.assertIsNone(payload["releaseLine"])
        self.assertEqual(8, len(payload["commands"]))
        for command in payload["commands"]:
            self.assertIn("-p:Version=0.0.0-ci-test", command)
            self.assertIn("-p:PackageVersion=0.0.0-ci-test", command)
            self.assertIn(VALIDATION_PROPERTY, command)
            self.assertIn(BASELINE_PROPERTY, command)
            self.assertIn(SKIP_BASELINE_PROPERTY, command)

    def test_candidate_semver_accepts_prerelease_and_optional_build_metadata(self) -> None:
        for version in ("4.2.0", "4.2.0-rc.1", "4.2.0-rc.1+build.7"):
            with self.subTest(version=version), tempfile.TemporaryDirectory() as directory:
                root = pathlib.Path(directory)
                paths = self.write_policy_fixture(root, self.ledger())
                self.assertEqual("v4.2", validate_release_policy(root, version, **paths))

    def test_candidate_semver_rejects_incomplete_or_empty_identifiers(self) -> None:
        invalid = (
            "4.2",
            "v4.2.0",
            "4.2.0-",
            "4.2.0-alpha..1",
            "4.2.0+build..1",
            "4.2.0-01",
        )
        for version in invalid:
            with self.subTest(version=version), tempfile.TemporaryDirectory() as directory:
                root = pathlib.Path(directory)
                paths = self.write_policy_fixture(root, self.ledger())
                with self.assertRaisesRegex(ReleaseCompatibilityError, "strict SemVer"):
                    validate_release_policy(root, version, **paths)

    def test_checked_in_policy_accepts_planned_release_and_published_baseline(self) -> None:
        self.assertEqual("v4.2", validate_release_policy(ROOT, VERSION))
        self.assertEqual("4.1.1", PUBLISHED_BASELINE_VERSION)

    def test_policy_rejects_wrong_current_release(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            paths = self.write_policy_fixture(root, self.ledger(current_release="v9.9"))
            with self.assertRaisesRegex(
                ReleaseCompatibilityError,
                r"release line v4\.2 does not match currentRelease v9\.9",
            ):
                validate_release_policy(root, VERSION, **paths)

    def test_lifecycle_fields_require_exact_vmajor_minor_tokens(self) -> None:
        payloads = (
            self.ledger(current_release="4.2"),
            self.ledger(suppressions=[self.suppression("v4.2.0", "v4.3")]),
        )
        for payload in payloads:
            with self.subTest(payload=payload), tempfile.TemporaryDirectory() as directory:
                root = pathlib.Path(directory)
                paths = self.write_policy_fixture(root, payload)
                with self.assertRaisesRegex(ReleaseCompatibilityError, "vMAJOR.MINOR"):
                    validate_release_policy(root, VERSION, **paths)

    def test_policy_rejects_pre_target_suppression(self) -> None:
        payload = self.ledger(suppressions=[self.suppression("v4.3", "v4.4")])
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            paths = self.write_policy_fixture(root, payload)
            with self.assertRaisesRegex(
                ReleaseCompatibilityError,
                r"targetRelease v4\.3 is later than --version v4\.2",
            ):
                validate_release_policy(root, VERSION, **paths)

    def test_policy_rejects_expired_suppression(self) -> None:
        payload = self.ledger(suppressions=[self.suppression("v4.1", "v4.2")])
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            paths = self.write_policy_fixture(root, payload)
            with self.assertRaisesRegex(
                ReleaseCompatibilityError,
                r"expiresAfter v4\.2 has been reached by --version v4\.2",
            ):
                validate_release_policy(root, VERSION, **paths)

    def test_synthetic_policy_checks_suppressions_against_checked_in_current_release(self) -> None:
        suppression = self.suppression("v4.2", "v4.3")
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            paths = self.write_policy_fixture(
                root,
                self.ledger(suppressions=[suppression]),
                mcp_xml=self.suppression_xml(suppression),
            )

            self.assertIsNone(validate_release_policy(
                root,
                "0.0.0-ci-test",
                match_candidate_release=False,
                **paths,
            ))

    def test_policy_rejects_unsupported_schema(self) -> None:
        payload = self.ledger()
        payload["schemaVersion"] = "9.0"
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            paths = self.write_policy_fixture(root, payload)
            with self.assertRaisesRegex(ReleaseCompatibilityError, "schemaVersion must be 2.0"):
                validate_release_policy(root, VERSION, **paths)

    def test_policy_rejects_unadvanced_baseline(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            paths = self.write_policy_fixture(root, self.ledger(), baseline="4.0.0")
            with self.assertRaisesRegex(
                ReleaseCompatibilityError,
                r"baseline must be the verified published 4\.1\.1; found '4\.0\.0'",
            ):
                validate_release_policy(root, VERSION, **paths)

    def test_policy_rejects_stale_mcp_xml_after_ledger_cleanup(self) -> None:
        stale = self.suppression("v4.2", "v4.3")
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            paths = self.write_policy_fixture(root, self.ledger(), mcp_xml=self.suppression_xml(stale))
            with self.assertRaisesRegex(ReleaseCompatibilityError, r"stale XML rows=.*SkillBenchmarkPrompt"):
                validate_release_policy(root, VERSION, **paths)

    def test_policy_accepts_exact_non_empty_ledger_xml_parity(self) -> None:
        suppression = self.suppression("v4.2", "v4.3")
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            paths = self.write_policy_fixture(
                root,
                self.ledger(suppressions=[suppression]),
                mcp_xml=self.suppression_xml(suppression),
            )

            self.assertEqual("v4.2", validate_release_policy(root, VERSION, **paths))

    def test_policy_rejects_wildcard_signature_diagnostic_and_unapproved_reason(self) -> None:
        mutations = {
            "oldSignature": "T:Hexalith.FrontComposer.Mcp.*",
            "apiCompatDiagnosticId": "CP*",
            "reason": "temporary-exception",
        }
        for field, value in mutations.items():
            suppression = self.suppression("v4.2", "v4.3")
            suppression[field] = value
            with self.subTest(field=field), tempfile.TemporaryDirectory() as directory:
                root = pathlib.Path(directory)
                paths = self.write_policy_fixture(root, self.ledger(suppressions=[suppression]))
                with self.assertRaises(ReleaseCompatibilityError):
                    validate_release_policy(root, VERSION, **paths)

    def test_policy_rejects_malformed_diagnostic_id(self) -> None:
        suppression = self.suppression("v4.2", "v4.3")
        suppression["apiCompatDiagnosticId"] = "CP12"
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            paths = self.write_policy_fixture(root, self.ledger(suppressions=[suppression]))
            with self.assertRaisesRegex(ReleaseCompatibilityError, "four digits"):
                validate_release_policy(root, VERSION, **paths)

    def test_policy_rejects_wrong_xml_root_and_unknown_or_duplicate_fields(self) -> None:
        exact = self.suppression("v4.2", "v4.3")
        valid = self.suppression_xml(exact)
        invalid_documents = {
            "root": valid.replace("<Suppressions>", "<Policy>").replace("</Suppressions>", "</Policy>"),
            "unknown": valid.replace("</Suppression>", "    <Comment>no</Comment>\n  </Suppression>"),
            "duplicate": valid.replace(
                "</Suppression>",
                "    <DiagnosticId>CP0001</DiagnosticId>\n  </Suppression>",
            ),
        }
        for failure, document in invalid_documents.items():
            with self.subTest(failure=failure), tempfile.TemporaryDirectory() as directory:
                root = pathlib.Path(directory)
                paths = self.write_policy_fixture(
                    root,
                    self.ledger(suppressions=[exact]),
                    mcp_xml=document,
                )
                with self.assertRaises(ReleaseCompatibilityError):
                    validate_release_policy(root, VERSION, **paths)

    def test_policy_rejects_xml_assembly_that_does_not_match_package_id(self) -> None:
        suppression = self.suppression("v4.2", "v4.3")
        document = self.suppression_xml(suppression).replace(
            "Hexalith.FrontComposer.Mcp.dll",
            "Hexalith.FrontComposer.Other.dll",
        )
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            paths = self.write_policy_fixture(
                root,
                self.ledger(suppressions=[suppression]),
                mcp_xml=document,
            )
            with self.assertRaisesRegex(ReleaseCompatibilityError, "assembly must be"):
                validate_release_policy(root, VERSION, **paths)

    def test_policy_wraps_xml_io_failure(self) -> None:
        for failure in (OSError("denied"), UnicodeError("invalid text")):
            with self.subTest(failure=type(failure).__name__), tempfile.TemporaryDirectory() as directory:
                root = pathlib.Path(directory)
                paths = self.write_policy_fixture(root, self.ledger())
                with mock.patch.object(release_compatibility.ET, "parse", side_effect=failure):
                    with self.assertRaisesRegex(
                        ReleaseCompatibilityError,
                        "cannot read compatibility policy XML",
                    ):
                        validate_release_policy(root, VERSION, **paths)

    def test_policy_failure_precedes_package_output_cleanup(self) -> None:
        module = self.load_packer()
        with tempfile.TemporaryDirectory() as directory:
            output = pathlib.Path(directory)
            sentinel = output / "existing.nupkg"
            sentinel.write_bytes(b"do not delete")
            argv = [str(SCRIPT), str(output), VERSION, "--release-policy"]
            with mock.patch.object(
                module,
                "validate_release_policy",
                side_effect=ReleaseCompatibilityError("stale release policy"),
            ), mock.patch.object(sys, "argv", argv):
                with self.assertRaisesRegex(ReleaseCompatibilityError, "stale release policy"):
                    module.main()

            self.assertEqual(b"do not delete", sentinel.read_bytes())

    def test_synthetic_policy_failure_also_precedes_package_output_cleanup(self) -> None:
        module = self.load_packer()
        with tempfile.TemporaryDirectory() as directory:
            output = pathlib.Path(directory)
            sentinel = output / "existing.nupkg"
            sentinel.write_bytes(b"do not delete")
            argv = [str(SCRIPT), str(output), "0.0.0-ci-test"]
            with mock.patch.object(
                module,
                "validate_release_policy",
                side_effect=ReleaseCompatibilityError("static policy mismatch"),
            ), mock.patch.object(sys, "argv", argv):
                with self.assertRaisesRegex(ReleaseCompatibilityError, "static policy mismatch"):
                    module.main()

            self.assertEqual(b"do not delete", sentinel.read_bytes())

    def test_inventory_failure_precedes_package_output_cleanup(self) -> None:
        module = self.load_packer()
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            inventory = root / "inventory.json"
            rows = self.inventory_rows(root)
            rows[0].pop("package_id")
            inventory.write_text(json.dumps({"packages": rows}), encoding="utf-8")
            output = root / "output"
            output.mkdir()
            sentinel = output / "existing.nupkg"
            sentinel.write_bytes(b"do not delete")
            argv = [str(SCRIPT), str(output), "0.0.0-ci-test"]
            with mock.patch.object(module, "INVENTORY_PATH", inventory), \
                    mock.patch.object(module, "REPO_ROOT", root), \
                    mock.patch.object(module, "validate_release_policy", return_value=None), \
                    mock.patch.object(sys, "argv", argv):
                with self.assertRaisesRegex(ValueError, "missing fields"):
                    module.main()

            self.assertEqual(b"do not delete", sentinel.read_bytes())

    def test_inventory_rejects_duplicate_ids_and_projects_outside_repo(self) -> None:
        module = self.load_packer()
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory) / "repo"
            root.mkdir()
            inventory = root / "inventory.json"
            rows = self.inventory_rows(root)
            inventory.write_text(json.dumps({"packages": rows[:-1]}), encoding="utf-8")
            with mock.patch.object(module, "INVENTORY_PATH", inventory), \
                    mock.patch.object(module, "REPO_ROOT", root):
                with self.assertRaisesRegex(ValueError, "expected exactly 8 packable packages"):
                    module.packable_projects()

            rows = self.inventory_rows(root)
            rows[1]["package_id"] = rows[0]["package_id"]
            inventory.write_text(json.dumps({"packages": rows}), encoding="utf-8")
            with mock.patch.object(module, "INVENTORY_PATH", inventory), \
                    mock.patch.object(module, "REPO_ROOT", root):
                with self.assertRaisesRegex(ValueError, "duplicate packable package_id"):
                    module.packable_projects()

            rows = self.inventory_rows(root)
            rows[1]["project"] = rows[0]["project"]
            inventory.write_text(json.dumps({"packages": rows}), encoding="utf-8")
            with mock.patch.object(module, "INVENTORY_PATH", inventory), \
                    mock.patch.object(module, "REPO_ROOT", root):
                with self.assertRaisesRegex(ValueError, "duplicate packable project"):
                    module.packable_projects()

            rows = self.inventory_rows(root)
            missing_project = root / str(rows[0]["project"])
            missing_project.unlink()
            inventory.write_text(json.dumps({"packages": rows}), encoding="utf-8")
            with mock.patch.object(module, "INVENTORY_PATH", inventory), \
                    mock.patch.object(module, "REPO_ROOT", root):
                with self.assertRaisesRegex(ValueError, "does not identify an existing .csproj"):
                    module.packable_projects()

            rows = self.inventory_rows(root)
            outside = root.parent / "outside.csproj"
            outside.write_text("<Project />", encoding="utf-8")
            rows[0]["project"] = str(outside)
            inventory.write_text(json.dumps({"packages": rows}), encoding="utf-8")
            with mock.patch.object(module, "INVENTORY_PATH", inventory), \
                    mock.patch.object(module, "REPO_ROOT", root):
                with self.assertRaisesRegex(ValueError, "escapes the repository root"):
                    module.packable_projects()

    def test_relative_output_is_resolved_from_caller_for_cleanup_and_pack_commands(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            caller = pathlib.Path(directory)
            result = subprocess.run(
                [sys.executable, str(SCRIPT), "relative-output", "0.0.0-ci-test", "--plan"],
                cwd=caller,
                check=False,
                capture_output=True,
                text=True,
            )

            self.assertEqual(0, result.returncode, result.stderr)
            expected = str((caller / "relative-output").resolve())
            for command in json.loads(result.stdout)["commands"]:
                self.assertEqual(expected, command[command.index("--output") + 1])

    def test_dead_build_and_pack_entrypoint_is_removed(self) -> None:
        self.assertFalse(RETIRED_SCRIPT.exists())

    @staticmethod
    def load_packer():
        spec = importlib.util.spec_from_file_location("frontcomposer_live_packer", SCRIPT)
        if spec is None or spec.loader is None:
            raise AssertionError("could not load live packer")
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module

    @staticmethod
    def inventory_rows(root: pathlib.Path) -> list[dict[str, object]]:
        rows: list[dict[str, object]] = []
        for index in range(8):
            project = root / "src" / f"Package{index}" / f"Package{index}.csproj"
            project.parent.mkdir(parents=True, exist_ok=True)
            project.write_text("<Project />", encoding="utf-8")
            rows.append({
                "project": project.relative_to(root).as_posix(),
                "package_id": f"Package.{index}",
                "packable": True,
                "symbol_required": True,
            })
        return rows

    @staticmethod
    def ledger(
        *,
        current_release: str = "v4.2",
        suppressions: list[dict[str, str]] | None = None,
    ) -> dict[str, object]:
        return {
            "schemaVersion": "2.0",
            "currentRelease": current_release,
            "suppressions": suppressions or [],
        }

    @staticmethod
    def suppression(target: str, expiry: str) -> dict[str, str]:
        return {
            "package": "Hexalith.FrontComposer.Mcp",
            "tfm": "net10.0",
            "oldSignature": "T:Hexalith.FrontComposer.Mcp.Skills.SkillBenchmarkPrompt",
            "newState": "removed",
            "apiCompatDiagnosticId": "CP0001",
            "targetRelease": target,
            "reviewerRationale": "Reviewed compatibility fixture suppression.",
            "ownerStory": "fixture-story",
            "expiresAfter": expiry,
            "reason": "intentional-major-break",
        }

    @staticmethod
    def suppression_xml(suppression: dict[str, str]) -> str:
        package = suppression["package"]
        tfm = suppression["tfm"]
        return f"""<?xml version="1.0" encoding="utf-8"?>
<Suppressions>
  <Suppression>
    <DiagnosticId>{suppression["apiCompatDiagnosticId"]}</DiagnosticId>
    <Target>{suppression["oldSignature"]}</Target>
    <Left>lib/{tfm}/{package}.dll</Left>
    <Right>lib/{tfm}/{package}.dll</Right>
    <IsBaselineSuppression>true</IsBaselineSuppression>
  </Suppression>
</Suppressions>
"""

    @staticmethod
    def write_policy_fixture(
        root: pathlib.Path,
        payload: dict[str, object],
        *,
        baseline: str = PUBLISHED_BASELINE_VERSION,
        mcp_xml: str = "<Suppressions />\n",
    ) -> dict[str, object]:
        ledger = root / "compatibility-suppressions.json"
        ledger.write_text(json.dumps(payload), encoding="utf-8")
        shared = root / "Directory.Build.targets"
        contracts_ui = root / "Contracts.UI.csproj"
        baseline_xml = (
            "<Project><PropertyGroup><FrontComposerPackageValidationBaselineVersion>"
            f"{baseline}</FrontComposerPackageValidationBaselineVersion></PropertyGroup></Project>"
        )
        shared.write_text(baseline_xml, encoding="utf-8")
        contracts_ui.write_text(baseline_xml, encoding="utf-8")
        suppression_files: dict[str, pathlib.Path] = {}
        for package in (
            "Hexalith.FrontComposer.Contracts",
            "Hexalith.FrontComposer.Mcp",
            "Hexalith.FrontComposer.Shell",
        ):
            path = root / f"{package}.CompatibilitySuppressions.xml"
            path.write_text(
                mcp_xml if package.endswith(".Mcp") else "<Suppressions />\n",
                encoding="utf-8",
            )
            suppression_files[package] = path
        return {
            "suppressions_path": ledger,
            "baseline_paths": (shared, contracts_ui),
            "suppression_files": suppression_files,
        }


if __name__ == "__main__":
    unittest.main()
