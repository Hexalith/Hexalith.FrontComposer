#!/usr/bin/env python3
"""Governance coverage for the Story 11.24 EventStore identity matrix."""

from __future__ import annotations

import copy
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from typing import Any, Callable


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))

import eventstore_runtime_evidence as evidence  # noqa: E402


CANONICAL_EVIDENCE = (
    ROOT / "_bmad-output" / "implementation-artifacts" / "evidence" / "frontcomposer-story-11-24"
)
CANONICAL_PACTS = ROOT / "tests" / "Hexalith.FrontComposer.Shell.Tests" / "Pact"


def _sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _read_json(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(value, dict):
        raise AssertionError(f"Expected one JSON object in {path}")
    return value


def _write_json(path: Path, value: dict[str, Any]) -> None:
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def _set_manifest_hash(evidence_root: Path, relative: str) -> None:
    manifest_path = evidence_root / "sha256-manifest.json"
    manifest = _read_json(manifest_path)
    entries = manifest["files"]
    entry = next(item for item in entries if item["path"] == relative)
    entry["sha256"] = _sha256(evidence_root / relative)
    _write_json(manifest_path, manifest)


def _rewrite_report(
    evidence_root: Path,
    mutate: Callable[[dict[str, Any]], None],
) -> None:
    report_relative = "provider-verification/provider-verification.json"
    receipt_relative = "provider-verification/run-evidence.json"
    report_path = evidence_root / report_relative
    receipt_path = evidence_root / receipt_relative
    report = _read_json(report_path)
    mutate(report)
    _write_json(report_path, report)

    receipt = _read_json(receipt_path)
    receipt_report = receipt["report"]
    receipt_report["sha256"] = _sha256(report_path)
    receipt_report["bytes"] = report_path.stat().st_size
    for key in (
        "finalVerdict",
        "requestedInteractionCount",
        "reportedInteractionCount",
        "setupEventCount",
        "teardownEventCount",
        "complete",
        "hostStopped",
        "portClosed",
    ):
        receipt_report[key] = report[key]
    failed = report["finalVerdict"] == "failed"
    receipt["expectedNonzero"] = failed
    receipt["exitCode"] = 4 if failed else 0
    _write_json(receipt_path, receipt)
    _set_manifest_hash(evidence_root, report_relative)
    _set_manifest_hash(evidence_root, receipt_relative)


def _rewrite_evidence_json(
    evidence_root: Path,
    relative: str,
    mutate: Callable[[dict[str, Any]], None],
) -> None:
    path = evidence_root / relative
    document = _read_json(path)
    mutate(document)
    _write_json(path, document)
    _set_manifest_hash(evidence_root, relative)


def _git_output(*args: str) -> str:
    result = subprocess.run(
        ["git", *args],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
    )
    return result.stdout.strip()


def _dependency_state() -> tuple[str, str, str]:
    return (
        _sha256(ROOT / "eng/dependency-graph-policy.json"),
        _git_output("-C", "references/Hexalith.EventStore", "rev-parse", "HEAD"),
        _git_output("-C", "references/Hexalith.Builds", "rev-parse", "HEAD"),
    )


class EventStoreRuntimeEvidenceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self._temporary.cleanup)
        temporary_root = Path(self._temporary.name)
        self.evidence_root = temporary_root / "evidence"
        self.pact_root = temporary_root / "pacts"
        shutil.copytree(CANONICAL_EVIDENCE, self.evidence_root)
        shutil.copytree(CANONICAL_PACTS, self.pact_root)

    def validate(self) -> list[str]:
        return evidence.validate(self.evidence_root, self.pact_root)

    def test_authorized_identity_with_truthful_provider_drift_is_accepted(self) -> None:
        report = _read_json(self.evidence_root / "provider-verification/provider-verification.json")

        self.assertEqual(report["finalVerdict"], "failed")
        self.assertFalse(report["identity"]["runtimeMatches"])
        self.assertEqual(
            sum(item["resultCode"] == "interaction.contract-failed" for item in report["interactions"]),
            16,
        )
        self.assertEqual(self.validate(), [])

    def test_incomplete_hash_bound_report_is_rejected(self) -> None:
        _rewrite_report(
            self.evidence_root,
            lambda report: report.update(
                complete=False,
                reportedInteractionCount=18,
            ),
        )

        before = _dependency_state()
        errors = self.validate()

        self.assertTrue(any("complete must equal True" in error for error in errors), errors)
        self.assertTrue(any("reportedInteractionCount must equal 19" in error for error in errors), errors)
        self.assertEqual(_dependency_state(), before, "validation must leave policy and both checkouts unchanged")

    def test_unsafe_hash_bound_provider_host_is_rejected(self) -> None:
        def mutate(report: dict[str, Any]) -> None:
            report["host"]["bindScope"] = "all-interfaces"
            report["host"]["portAllocation"] = "fixed"

        _rewrite_report(self.evidence_root, mutate)

        errors = self.validate()

        self.assertTrue(any("host bindScope" in error for error in errors), errors)
        self.assertTrue(any("host portAllocation" in error for error in errors), errors)

    def test_missing_cleanup_accounting_is_rejected(self) -> None:
        def mutate(report: dict[str, Any]) -> None:
            report["interactions"][0]["stateEvents"].pop()
            report["teardownEventCount"] = 18

        _rewrite_report(self.evidence_root, mutate)

        errors = self.validate()

        self.assertTrue(any("teardownEventCount must equal 19" in error for error in errors), errors)
        self.assertTrue(any("lacks setup/teardown accounting" in error for error in errors), errors)

    def test_retired_identity_receipt_is_rejected_without_mutating_dependencies(self) -> None:
        receipt_relative = (
            f"{evidence.RECEIPT_DIR}/eventstore-owner.json"
        )
        receipt_path = self.evidence_root / receipt_relative
        receipt = _read_json(receipt_path)
        receipt["source_sha"] = "fa2d1c9910f8976553adb33dcdb1c9ff2ea75594"
        _write_json(receipt_path, receipt)
        _set_manifest_hash(self.evidence_root, receipt_relative)

        report_input_name = "eventstore-owner.json"

        def mutate(report: dict[str, Any]) -> None:
            entry = next(item for item in report["inputHashes"] if item["name"] == report_input_name)
            entry["sha256"] = _sha256(receipt_path)

        _rewrite_report(self.evidence_root, mutate)
        before_manifest = copy.deepcopy(_read_json(self.evidence_root / "sha256-manifest.json"))
        before_dependencies = _dependency_state()

        errors = self.validate()

        after = _read_json(self.evidence_root / "sha256-manifest.json")
        self.assertTrue(any("does not authorize the exact bound tuple" in error for error in errors), errors)
        self.assertEqual(after, before_manifest, "validation must not rewrite dependency/evidence pointers")
        self.assertEqual(
            _dependency_state(),
            before_dependencies,
            "validation must leave policy and both checkouts unchanged",
        )

    def test_report_changed_without_manifest_binding_is_rejected(self) -> None:
        report_path = self.evidence_root / "provider-verification/provider-verification.json"
        report = _read_json(report_path)
        report["complete"] = False
        _write_json(report_path, report)

        errors = self.validate()

        self.assertTrue(any("SHA-256 mismatch" in error for error in errors), errors)

    def test_evidence_checkout_policy_preserves_every_manifest_byte(self) -> None:
        canonical_files = sorted(
            path
            for path in CANONICAL_EVIDENCE.rglob("*")
            if path.is_file()
        )

        for path in canonical_files:
            relative = path.relative_to(ROOT).as_posix()
            attribute = _git_output("check-attr", "text", "--", relative)
            self.assertEqual(attribute, f"{relative}: text: unset")
            raw_blob = _git_output("hash-object", "--no-filters", "--", relative)
            checkout_blob = _git_output("hash-object", f"--path={relative}", "--", relative)
            self.assertEqual(checkout_blob, raw_blob, f"checkout filters must preserve {relative}")

    def test_manifest_rejects_every_undeclared_file_and_hash_binds_runtime_receipts(self) -> None:
        unexpected = self.evidence_root / "apphost-smoke/unexpected.json"
        unexpected.write_text("{}\n", encoding="utf-8")
        smoke = self.evidence_root / "apphost-smoke/apphost-smoke.json"
        smoke.write_bytes(smoke.read_bytes() + b" ")

        errors = self.validate()

        self.assertTrue(any("undeclared files" in error for error in errors), errors)
        self.assertTrue(any("SHA-256 mismatch for apphost-smoke/apphost-smoke.json" in error for error in errors), errors)

    def test_manifest_capture_source_must_be_the_exact_known_commit(self) -> None:
        manifest_path = self.evidence_root / "sha256-manifest.json"
        manifest = _read_json(manifest_path)
        manifest["capturedFromEventStoreCommit"] = "0" * 40
        _write_json(manifest_path, manifest)

        errors = self.validate()

        self.assertTrue(any("exact known capture-source commit" in error for error in errors), errors)

    def test_frozen_subject_bound_evidence_must_equal_snapshot_manifest(self) -> None:
        subject_relative = f"{evidence.SUBJECT_DIR}/review-subject.json"

        def mutate(subject: dict[str, Any]) -> None:
            subject["bound_evidence"][0]["sha256"] = "0" * 64

        _rewrite_evidence_json(self.evidence_root, subject_relative, mutate)

        errors = self.validate()

        self.assertTrue(any("bound_evidence does not match" in error for error in errors), errors)

    def test_pact_byte_mutation_without_description_or_state_change_is_rejected(self) -> None:
        pact_path = self.pact_root / evidence.PACT_FILES[0]
        pact = _read_json(pact_path)
        pact["metadata"]["story1124Mutation"] = "same-interactions-different-bytes"
        _write_json(pact_path, pact)

        errors = self.validate()

        self.assertTrue(any("contract-input hash differs" in error for error in errors), errors)

    def test_provider_state_catalog_set_must_equal_pact_states(self) -> None:
        catalog_path = self.pact_root / "provider-state-catalog.json"
        catalog = _read_json(catalog_path)
        extra = copy.deepcopy(catalog["states"][0])
        extra["name"] = "undeclared-extra-state"
        catalog["states"].append(extra)
        _write_json(catalog_path, catalog)

        errors = self.validate()

        self.assertTrue(any("catalog set must equal" in error for error in errors), errors)

    def test_interaction_manifest_pact_file_attribution_must_be_exact(self) -> None:
        manifest_path = self.pact_root / "interaction-manifest.json"
        manifest = _read_json(manifest_path)
        manifest["pactFiles"].pop()
        _write_json(manifest_path, manifest)

        errors = self.validate()

        self.assertTrue(any("pact-file attribution" in error for error in errors), errors)

    def test_duplicate_json_keys_are_rejected_before_validation(self) -> None:
        relative = "apphost-smoke/apphost-smoke.json"
        path = self.evidence_root / relative
        text = path.read_text(encoding="utf-8")
        path.write_text(text.replace("{", '{\n  "schema": "duplicate",', 1), encoding="utf-8")
        _set_manifest_hash(self.evidence_root, relative)

        errors = self.validate()

        self.assertTrue(any("duplicate key 'schema'" in error for error in errors), errors)

    def test_evidence_root_file_and_intermediate_symlinks_are_rejected(self) -> None:
        root_link = Path(self._temporary.name) / "evidence-root-link"
        os.symlink(self.evidence_root, root_link, target_is_directory=True)
        self.assertTrue(any("root is missing or is a symlink" in error for error in evidence.validate(root_link, self.pact_root)))

        smoke_path = self.evidence_root / "apphost-smoke/apphost-smoke.json"
        smoke_target = Path(self._temporary.name) / "smoke-target.json"
        smoke_target.write_bytes(smoke_path.read_bytes())
        smoke_path.unlink()
        os.symlink(smoke_target, smoke_path)
        self.assertTrue(any("tree contains a symlink" in error for error in self.validate()))

        smoke_path.unlink()
        smoke_directory = self.evidence_root / "apphost-smoke"
        relocated = Path(self._temporary.name) / "apphost-smoke-real"
        shutil.move(smoke_directory, relocated)
        os.symlink(relocated, smoke_directory, target_is_directory=True)
        self.assertTrue(any("tree contains a symlink" in error for error in self.validate()))

    def test_bounded_read_rejects_oversized_report_before_json_parsing(self) -> None:
        report_path = self.evidence_root / "provider-verification/provider-verification.json"
        report_path.write_bytes(b"{" + (b" " * evidence.MAX_FILE_BYTES) + b"}")

        errors = self.validate()

        self.assertTrue(any("exceeds" in error and "provider-verification.json" in error for error in errors), errors)

    def test_timezone_naive_timestamp_and_scalar_type_confusion_are_rejected(self) -> None:
        def mutate(report: dict[str, Any]) -> None:
            report["complete"] = 1
            report["timing"]["run"]["startedAt"] = "2026-08-12T11:33:46"

        _rewrite_report(self.evidence_root, mutate)

        errors = self.validate()

        self.assertTrue(any("complete must equal True" in error for error in errors), errors)
        self.assertTrue(any("must include a timezone offset" in error for error in errors), errors)

    def test_receipt_requires_exact_fields_statement_source_and_frozen_time(self) -> None:
        relative = f"{evidence.RECEIPT_DIR}/release-owner.json"

        def mutate(receipt: dict[str, Any]) -> None:
            receipt.pop("statement")
            receipt["durable_source"] = "https://example.invalid/receipt"
            receipt["subject_frozen_at"] = "2026-08-10T07:06:12Z"

        _rewrite_evidence_json(self.evidence_root, relative, mutate)

        errors = self.validate()

        self.assertTrue(any("exact required receipt fields" in error for error in errors), errors)
        self.assertTrue(any("durable_source does not authorize" in error for error in errors), errors)
        self.assertTrue(any("subject_frozen_at does not authorize" in error for error in errors), errors)

    def test_package_identity_signature_and_consumer_coverage_are_not_count_only(self) -> None:
        package_relative = f"{evidence.SUBJECT_DIR}/package-manifest.json"
        restore_relative = f"{evidence.SUBJECT_DIR}/restore-receipt.json"

        def mutate_package(manifest: dict[str, Any]) -> None:
            manifest["packages"][0]["id"] = "Hexalith.EventStore.Forged"
            manifest["repository_signature"]["subject"] = "CN=Untrusted"

        def mutate_restore(receipt: dict[str, Any]) -> None:
            receipt["consumer_validation"]["library_consumers_passed"] = True

        _rewrite_evidence_json(self.evidence_root, package_relative, mutate_package)
        _rewrite_evidence_json(self.evidence_root, restore_relative, mutate_restore)

        errors = self.validate()

        self.assertTrue(any("exact 14 approved package identities" in error for error in errors), errors)
        self.assertTrue(any("repository signature subject" in error for error in errors), errors)
        self.assertTrue(any("library_consumers_passed" in error for error in errors), errors)

    def test_complete_truthful_passing_report_and_receipt_are_structurally_valid(self) -> None:
        def mutate(report: dict[str, Any]) -> None:
            for interaction in report["interactions"]:
                interaction["resultCode"] = "interaction.passed"
            identity = report["identity"]
            identity["observedSourceSha"] = evidence.SOURCE_SHA
            identity["observedVersion"] = evidence.VERSION
            identity["observedBuildsSha"] = evidence.BUILDS_SHA
            identity["runtimeMatches"] = True
            identity["reasonCodes"] = []
            report["reasonCodes"] = []
            report["finalVerdict"] = "passed"
            report["timing"]["run"]["resultCode"] = "run.succeeded"

        _rewrite_report(self.evidence_root, mutate)

        self.assertEqual(self.validate(), [])

    def test_complete_truthful_runtime_only_failure_is_structurally_valid(self) -> None:
        def mutate(report: dict[str, Any]) -> None:
            for interaction in report["interactions"]:
                interaction["resultCode"] = "interaction.passed"
            report["reasonCodes"] = list(report["identity"]["reasonCodes"])

        _rewrite_report(self.evidence_root, mutate)

        self.assertEqual(self.validate(), [])

    def test_timing_intervals_must_match_durations_and_run_bounds(self) -> None:
        def mutate(report: dict[str, Any]) -> None:
            report["timing"]["startup"]["durationMilliseconds"] += 2
            report["timing"]["cleanup"]["startedAt"] = "2026-08-12T11:34:49.2226256+00:00"
            report["timing"]["cleanup"]["completedAt"] = "2026-08-12T11:34:49.2348699+00:00"

        _rewrite_report(self.evidence_root, mutate)

        errors = self.validate()

        self.assertTrue(any("duration contradicts" in error for error in errors), errors)
        self.assertTrue(any("internally ordered" in error for error in errors), errors)

    def test_apphost_topology_and_observation_consistency_are_recomputed(self) -> None:
        relative = "apphost-smoke/apphost-smoke.json"

        def mutate(smoke: dict[str, Any]) -> None:
            smoke["topology"]["programSha256"] = "0" * 64
            smoke["observations"]["commandSubmit"]["result"] = "passed"

        _rewrite_evidence_json(self.evidence_root, relative, mutate)

        errors = self.validate()

        self.assertTrue(any("does not match the current file bytes" in error for error in errors), errors)
        self.assertTrue(any("commandSubmit passes without a successful" in error for error in errors), errors)

    def test_release_restore_requires_exact_command_edge_and_asset_inventory(self) -> None:
        relative = "release-restore/release-restore.json"

        def mutate(restore: dict[str, Any]) -> None:
            restore["configuration"] = "Debug"
            restore["eventStoreProjectEdgeCount"] = False
            restore["eventStoreAssets"][0]["name"] = "Hexalith.EventStore.Client"

        _rewrite_evidence_json(self.evidence_root, relative, mutate)

        errors = self.validate()

        self.assertTrue(any("exact approved AppHost Release project" in error for error in errors), errors)
        self.assertTrue(any("package-only EventStore assets" in error for error in errors), errors)
        self.assertTrue(any("asset inventory" in error for error in errors), errors)

    def test_known_sha256_value_in_non_hash_field_is_a_redaction_failure(self) -> None:
        relative = "apphost-smoke/apphost-smoke.json"

        def mutate(smoke: dict[str, Any]) -> None:
            smoke["diagnosticToken"] = evidence.SUBJECT_SHA256

        _rewrite_evidence_json(self.evidence_root, relative, mutate)

        errors = self.validate()

        self.assertTrue(any("encoded token-like value" in error for error in errors), errors)

    def test_contract_redaction_does_not_globally_allowlist_known_sha256_value(self) -> None:
        manifest_path = self.pact_root / "interaction-manifest.json"
        manifest = _read_json(manifest_path)
        manifest["diagnosticToken"] = evidence.SUBJECT_SHA256
        _write_json(manifest_path, manifest)
        artifact_root = Path(self._temporary.name) / "contract-artifacts"

        result = subprocess.run(
            [
                "pwsh",
                "-NoProfile",
                "-File",
                str(ROOT / "eng/validate-contract-artifacts.ps1"),
                "-PactDir",
                str(self.pact_root),
                "-ArtifactDir",
                str(artifact_root),
            ],
            cwd=ROOT,
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertNotEqual(result.returncode, 0)
        output = result.stdout + result.stderr
        self.assertIn("encoded token-like", output)
        self.assertIn("payload", output)

    def test_manifest_is_redaction_scanned_and_all_files_count_toward_total_bound(self) -> None:
        manifest_path = self.evidence_root / "sha256-manifest.json"
        manifest = _read_json(manifest_path)
        manifest["diagnosticToken"] = evidence.SUBJECT_SHA256
        manifest["padding"] = "x" * 700_000
        _write_json(manifest_path, manifest)
        for name in ("undeclared-a.bin", "undeclared-b.bin"):
            (self.evidence_root / name).write_bytes(b"x" * 700_000)

        errors = self.validate()

        self.assertTrue(any("encoded token-like value" in error for error in errors), errors)
        self.assertTrue(any("snapshot exceeds" in error for error in errors), errors)


if __name__ == "__main__":
    unittest.main()
