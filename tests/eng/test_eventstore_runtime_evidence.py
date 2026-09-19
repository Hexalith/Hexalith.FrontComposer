#!/usr/bin/env python3
"""Governance coverage for the Story 11.24 EventStore identity matrix."""

from __future__ import annotations

import base64
import contextlib
import copy
import hashlib
import io
import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
import zipfile
from datetime import datetime, timedelta
from pathlib import Path
from typing import Any, Callable
from unittest import mock


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))

import eventstore_runtime_evidence as evidence  # noqa: E402


CANONICAL_EVIDENCE = (
    ROOT / "_bmad-output" / "implementation-artifacts" / "evidence" / "frontcomposer-story-11-24"
)
CANONICAL_PACTS = ROOT / "tests" / "Hexalith.FrontComposer.Shell.Tests" / "Pact"
CANONICAL_LIVE_EVIDENCE = (
    ROOT / "_bmad-output" / "implementation-artifacts" / "evidence" / "pact-provider-reconciliation"
)
CANONICAL_ACTIVE_EVIDENCE = (
    ROOT / "_bmad-output" / "implementation-artifacts" / "evidence" / "eventstore-runtime-identity-v2"
)
CANONICAL_PRIOR_EVIDENCE = (
    ROOT / "_bmad-output" / "implementation-artifacts" / "evidence"
    / "pact-provider-reconciliation-history" / "2026-09-08-builds-35c3d1e5"
)
CANONICAL_IDENTITY_V2 = (
    ROOT / "_bmad-output" / "contracts" / "frontcomposer-eventstore-approved-runtime-identity-v2.json"
)
REAL_RUNTIME_INPUT_SNAPSHOT = evidence._runtime_input_snapshot
REAL_CANONICAL_ACTIVE_LOCATIONS = evidence._canonical_active_locations
REAL_CANONICAL_LIVE_LOCATIONS = evidence._canonical_live_locations
REAL_RUNTIME_GIT_TREE = evidence._runtime_git_tree
REAL_LIVE_PROVENANCE = evidence._live_provenance
REAL_EVALUATE_APPHOST_INPUTS = evidence._evaluate_apphost_inputs
REAL_APPHOST_RUNTIME_OUTPUT_BINDING = evidence._apphost_runtime_output_binding
REAL_GIT = evidence._git
REAL_GIT_COMPLETED = evidence._git_completed


def _sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _read_json(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(value, dict):
        raise AssertionError(f"Expected one JSON object in {path}")
    return value


def _write_json(path: Path, value: dict[str, Any]) -> None:
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def _offset_timestamp(value: str, seconds: int) -> str:
    return (datetime.fromisoformat(value) + timedelta(seconds=seconds)).isoformat()


def _synthetic_package_ledger(
    assets_paths: list[str],
    captured_at: str,
    *,
    graph_sha256: str,
) -> dict[str, Any]:
    content_hash = base64.b64encode(bytes(range(64))).decode("ascii")
    binding = {
        "id": "Synthetic.Package",
        "version": "1.0.0",
        "relativePath": "synthetic.package/1.0.0",
        "contentHashSha512": content_hash,
    }
    files = [
        {
            "path": "lib/net10.0/Synthetic.Package.dll",
            "bytes": 1,
            "sha256": "a" * 64,
        }
    ]
    package = {
        **binding,
        "nupkgSha512": content_hash,
        "files": files,
        "treeSha256": evidence._package_tree_sha256(files),
    }
    tool_bindings = [
        {
            "id": package_id,
            "version": version,
            "relativePath": f"{package_id.casefold()}/{version.casefold()}",
            "contentHashSha512": content_hash,
        }
        for package_id, version in (
            evidence.APPHOST_TOOL_PACKAGES
            if assets_paths == [evidence.APPHOST_PACKAGE_ASSETS_ROOT]
            else ()
        )
    ]
    tool_packages = [
        {
            **tool_binding,
            "nupkgSha512": content_hash,
            "files": files,
            "treeSha256": evidence._package_tree_sha256(files),
        }
        for tool_binding in tool_bindings
    ]
    entries = {
        "assetsGraphs": [
            {"path": path, "sha256": graph_sha256, "packages": [binding]}
            for path in assets_paths
        ],
        "toolPackages": tool_bindings,
        "packages": sorted(
            [package, *tool_packages],
            key=lambda item: (item["id"].casefold(), item["version"].casefold()),
        ),
    }
    return {
        "schema": evidence.PACKAGE_LEDGER_SCHEMA,
        "capturedAt": captured_at,
        "packageRoot": "fresh-external",
        **entries,
        "treeSha256": hashlib.sha256(
            json.dumps(entries, sort_keys=True, separators=(",", ":")).encode("utf-8")
        ).hexdigest(),
    }


def _manifest_entry_sha256(relative: str) -> str:
    """Return the sealed runtime manifest's exact hash for one tracked runtime input."""
    manifest = json.loads(
        (CANONICAL_ACTIVE_EVIDENCE / "frontcomposer-runtime-inputs.json").read_text(
            encoding="utf-8-sig"
        )
    )
    return next(
        str(entry["sha256"])
        for entry in manifest["entries"]
        if entry.get("path") == relative
    )


def _write_package_ledger_sidecar(
    root: Path,
    name: str,
    document: dict[str, Any],
) -> dict[str, Any]:
    """Write one ledger sidecar and return the binding its evidence document stores."""
    payload = (json.dumps(document, indent=2) + "\n").encode("utf-8")
    root.mkdir(parents=True, exist_ok=True)
    (root / name).write_bytes(payload)
    return evidence.package_ledger_binding(document, name, payload)


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


class RuntimeInputInventoryTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self._temporary.cleanup)
        self.repository = Path(self._temporary.name) / "repository"
        self.repository.mkdir()
        subprocess.run(["git", "init", "-q"], cwd=self.repository, check=True)
        subprocess.run(
            ["git", "config", "user.name", "Runtime Inventory Test"],
            cwd=self.repository,
            check=True,
        )
        subprocess.run(
            ["git", "config", "user.email", "runtime-inventory@example.test"],
            cwd=self.repository,
            check=True,
        )
        (self.repository / ".gitignore").write_text(
            "/src/ignored.cs\n"
            "/samples/Counter/ignored.json\n"
            "**/bin/\n"
            "**/obj/\n",
            encoding="utf-8",
        )
        subprocess.run(["git", "add", ".gitignore"], cwd=self.repository, check=True)
        subprocess.run(
            ["git", "commit", "-qm", "test: seed runtime inventory"],
            cwd=self.repository,
            check=True,
        )

    def test_runtime_scope_rejects_ignored_source_and_sample_tree_inputs(self) -> None:
        ignored_paths = (
            "src/ignored.cs",
            "samples/Counter/ignored.json",
        )
        for relative in ignored_paths:
            path = self.repository / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("ignored runtime input\n", encoding="utf-8")

        _, issues = evidence._runtime_input_snapshot(self.repository)

        untracked_issue = next(
            issue for issue in issues if "contains untracked files" in issue
        )
        for relative in ignored_paths:
            self.assertIn(relative, untracked_issue)

    def test_runtime_scope_bounds_dirty_path_diagnostics(self) -> None:
        for index in range(evidence.MAX_DIAGNOSTIC_PATHS + 5):
            path = self.repository / f"src/untracked-{index:02}.cs"
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("untracked runtime input\n", encoding="utf-8")

        _, issues = evidence._runtime_input_snapshot(self.repository)

        untracked_issue = next(
            issue for issue in issues if "contains untracked files" in issue
        )
        self.assertIn("src/untracked-19.cs", untracked_issue)
        self.assertNotIn("src/untracked-20.cs", untracked_issue)
        self.assertIn("omitted 5 additional path(s)", untracked_issue)

    def test_runtime_scope_allows_generated_bin_and_obj_outputs(self) -> None:
        for relative in (
            "src/Feature/Feature.csproj",
            "samples/Counter/Counter/Counter.csproj",
        ):
            path = self.repository / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("<Project />\n", encoding="utf-8")
        subprocess.run(
            ["git", "add", "src/Feature/Feature.csproj", "samples/Counter/Counter/Counter.csproj"],
            cwd=self.repository,
            check=True,
        )
        subprocess.run(
            ["git", "commit", "-qm", "test: add projects"],
            cwd=self.repository,
            check=True,
        )
        for relative in (
            "src/Feature/bin/Debug/net10.0/generated.cs",
            "samples/Counter/Counter/obj/project.assets.json",
        ):
            path = self.repository / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("generated output\n", encoding="utf-8")

        _, issues = evidence._runtime_input_snapshot(self.repository)

        self.assertFalse(any("contains untracked files" in issue for issue in issues), issues)

    def test_runtime_scope_rejects_ignored_bin_nested_below_project_content(self) -> None:
        project = self.repository / "src/Feature/Feature.csproj"
        project.parent.mkdir(parents=True, exist_ok=True)
        project.write_text("<Project />\n", encoding="utf-8")
        subprocess.run(
            ["git", "add", "src/Feature/Feature.csproj"],
            cwd=self.repository,
            check=True,
        )
        subprocess.run(
            ["git", "commit", "-qm", "test: add project"],
            cwd=self.repository,
            check=True,
        )
        runtime_content = self.repository / "src/Feature/wwwroot/bin/config.json"
        runtime_content.parent.mkdir(parents=True)
        runtime_content.write_text('{"runtime":true}\n', encoding="utf-8")

        _, issues = evidence._runtime_input_snapshot(self.repository)

        untracked_issue = next(
            issue for issue in issues if "contains untracked files" in issue
        )
        self.assertIn("src/Feature/wwwroot/bin/config.json", untracked_issue)

    def test_raw_crlf_checkout_is_compared_with_validator_owned_eol_rules(self) -> None:
        attributes = self.repository / ".gitattributes"
        source = self.repository / "src/Feature/Feature.cs"
        source.parent.mkdir(parents=True)
        attributes.write_text("*.cs text eol=crlf\n", encoding="utf-8")
        source.write_bytes(b"line-one\nline-two\n")
        subprocess.run(
            ["git", "add", ".gitattributes", "src/Feature/Feature.cs"],
            cwd=self.repository,
            check=True,
        )
        subprocess.run(
            ["git", "commit", "-qm", "test: add text input"],
            cwd=self.repository,
            check=True,
        )
        source.write_bytes(b"line-one\r\nline-two\r\n")

        objects, error = evidence._bulk_worktree_git_objects(
            self.repository, [("src/Feature/Feature.cs", "100644")]
        )
        index_object = subprocess.run(
            ["git", "rev-parse", "HEAD:src/Feature/Feature.cs"],
            cwd=self.repository,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.strip()

        self.assertIsNone(error)
        self.assertIn(index_object, objects["src/Feature/Feature.cs"])

    def test_custom_filter_and_info_attribute_overrides_fail_without_filter_execution(self) -> None:
        source = self.repository / "src/Feature/Feature.cs"
        source.parent.mkdir(parents=True)
        source.write_text("tracked\n", encoding="utf-8")
        (self.repository / ".gitattributes").write_text(
            "*.cs filter=credential-leak\n", encoding="utf-8"
        )
        subprocess.run(
            ["git", "add", ".gitattributes", "src/Feature/Feature.cs"],
            cwd=self.repository,
            check=True,
        )
        subprocess.run(
            ["git", "commit", "-qm", "test: add filtered input"],
            cwd=self.repository,
            check=True,
        )
        marker = self.repository / "filter-executed"
        subprocess.run(
            [
                "git", "config", "filter.credential-leak.clean",
                f"touch {marker}",
            ],
            cwd=self.repository,
            check=True,
        )

        _, filter_error = evidence._bulk_worktree_git_objects(
            self.repository, [("src/Feature/Feature.cs", "100644")]
        )

        self.assertIn("custom Git filter", filter_error or "")
        self.assertFalse(marker.exists(), "validator must never execute configured clean filters")

        (self.repository / ".git/info/attributes").write_text(
            "*.cs text eol=lf\n", encoding="utf-8"
        )
        _, override_error = evidence._bulk_worktree_git_objects(
            self.repository, [("src/Feature/Feature.cs", "100644")]
        )
        self.assertIn("Repository-local Git attributes", override_error or "")

    def test_ident_encoding_and_external_attribute_sources_fail_closed(self) -> None:
        source = self.repository / "src/Feature/Feature.cs"
        attributes = self.repository / ".gitattributes"
        source.parent.mkdir(parents=True)
        source.write_text("tracked\n", encoding="utf-8")
        attributes.write_text("*.cs text\n", encoding="utf-8")
        subprocess.run(
            ["git", "add", ".gitattributes", "src/Feature/Feature.cs"],
            cwd=self.repository,
            check=True,
        )
        subprocess.run(
            ["git", "commit", "-qm", "test: add attribute input"],
            cwd=self.repository,
            check=True,
        )

        for rule, expected in (
            ("*.cs ident\n", "ident attribute"),
            ("*.cs working-tree-encoding=UTF-8\n", "working-tree-encoding"),
        ):
            with self.subTest(rule=rule):
                attributes.write_text(rule, encoding="utf-8")
                subprocess.run(
                    ["git", "add", ".gitattributes"], cwd=self.repository, check=True
                )
                _, error = evidence._bulk_worktree_git_objects(
                    self.repository, [("src/Feature/Feature.cs", "100644")]
                )
                self.assertIn(expected, error or "")

        attributes.write_text("*.cs text\n", encoding="utf-8")
        subprocess.run(
            ["git", "add", ".gitattributes"], cwd=self.repository, check=True
        )
        external_attributes = self.repository.parent / "external-attributes"
        external_attributes.write_text("*.cs eol=lf\n", encoding="utf-8")
        for scope, variable in (
            ("global", "GIT_CONFIG_GLOBAL"),
            ("system", "GIT_CONFIG_SYSTEM"),
        ):
            with self.subTest(scope=scope):
                config = self.repository.parent / f"{scope}.gitconfig"
                config.write_text(
                    f"[core]\n\tattributesFile = {external_attributes}\n",
                    encoding="utf-8",
                )
                environment = {
                    variable: str(config),
                    "GIT_CONFIG_NOSYSTEM": "0" if scope == "system" else "1",
                }
                with mock.patch.dict(os.environ, environment):
                    _, error = evidence._bulk_worktree_git_objects(
                        self.repository, [("src/Feature/Feature.cs", "100644")]
                    )
                self.assertIn("override tracked attributes", error or "")

    def test_ignored_project_output_symlink_is_rejected_before_output_exemption(self) -> None:
        project = self.repository / "src/Feature/Feature.csproj"
        project.parent.mkdir(parents=True)
        project.write_text("<Project />\n", encoding="utf-8")
        subprocess.run(
            ["git", "add", "src/Feature/Feature.csproj"],
            cwd=self.repository,
            check=True,
        )
        subprocess.run(
            ["git", "commit", "-qm", "test: add project for symlink"],
            cwd=self.repository,
            check=True,
        )
        outside = self.repository.parent / "outside-output"
        outside.mkdir()
        (project.parent / "obj").symlink_to(outside, target_is_directory=True)

        _, issues = evidence._runtime_input_snapshot(self.repository)

        self.assertTrue(any("untracked symlinks" in issue for issue in issues), issues)


class ResolvedPackageLedgerTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self._temporary.cleanup)
        root = Path(self._temporary.name)
        self.repository = root / "repository"
        self.package_root = root / "packages"
        self.repository.mkdir()
        self.package_root.mkdir()

    def _package(
        self,
        package_id: str,
        version: str,
        payload: bytes,
        *,
        signed: bool = False,
    ) -> dict[str, Any]:
        relative = f"{package_id.casefold()}/{version.casefold()}"
        directory = self.package_root / relative
        (directory / "lib/net10.0").mkdir(parents=True)
        archive = directory / f"{package_id.casefold()}.{version.casefold()}.nupkg"
        with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED) as package:
            package.writestr("lib/net10.0/package.bin", payload)
        digest = base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode("ascii")
        if signed:
            with zipfile.ZipFile(archive, "a", compression=zipfile.ZIP_STORED) as package:
                package.writestr(".signature.p7s", b"synthetic-signature")
        (directory / "lib/net10.0" / f"{package_id}.dll").write_bytes(
            b"extracted-" + payload
        )
        return {"sha512": digest, "type": "package", "path": relative}

    def _assets(self, relative: str, libraries: dict[str, Any]) -> Path:
        path = self.repository / relative
        path.parent.mkdir(parents=True)
        _write_json(
            path,
            {
                "packageFolders": {str(self.package_root) + os.sep: {}},
                "libraries": libraries,
            },
        )
        return path

    def test_two_assets_graphs_bind_exact_union_archive_and_extracted_bytes(self) -> None:
        alpha = self._package("Alpha.Package", "1.0.0", b"alpha-archive")
        beta = self._package("Beta.Package", "2.0.0", b"beta-archive", signed=True)
        first = self._assets("first/obj/project.assets.json", {"Alpha.Package/1.0.0": alpha})
        second = self._assets(
            "second/obj/project.assets.json",
            {"Alpha.Package/1.0.0": alpha, "Beta.Package/2.0.0": beta},
        )

        ledger, issues = evidence.resolved_package_ledger(
            self.repository, self.package_root, [second, first]
        )
        semantic_issues: list[str] = []
        evidence.validate_package_ledger_semantics(ledger, semantic_issues)

        self.assertEqual(issues, [])
        self.assertEqual(semantic_issues, [])
        self.assertEqual(
            [item["path"] for item in ledger["assetsGraphs"]],
            ["first/obj/project.assets.json", "second/obj/project.assets.json"],
        )
        self.assertEqual([item["id"] for item in ledger["packages"]], ["Alpha.Package", "Beta.Package"])

        extracted = self.package_root / "alpha.package/1.0.0/lib/net10.0/Alpha.Package.dll"
        extracted.write_bytes(b"poisoned")
        validation_errors: list[str] = []
        evidence.validate_package_ledger(
            ledger,
            self.repository,
            self.package_root,
            [first, second],
            validation_errors,
        )
        self.assertTrue(any("differs from the current restored package authority" in error for error in validation_errors), validation_errors)

        extracted.write_bytes(b"extracted-alpha-archive")
        archive = self.package_root / "alpha.package/1.0.0/alpha.package.1.0.0.nupkg"
        archive_bytes = archive.read_bytes()
        archive.write_bytes(b"poisoned-archive")
        archive_errors: list[str] = []
        evidence.validate_package_ledger(
            ledger,
            self.repository,
            self.package_root,
            [first, second],
            archive_errors,
        )
        self.assertTrue(
            any("nupkg" in error for error in archive_errors),
            archive_errors,
        )

        archive.write_bytes(archive_bytes)
        missing_graph_errors: list[str] = []
        evidence.validate_package_ledger(
            ledger,
            self.repository,
            self.package_root,
            [first],
            missing_graph_errors,
        )
        self.assertTrue(
            any("differs from the current restored package authority" in error for error in missing_graph_errors),
            missing_graph_errors,
        )

        orphan = self.package_root / "orphan.package/9.9.9"
        orphan.mkdir(parents=True)
        (orphan / "orphan.txt").write_text("orphan\n", encoding="utf-8")
        orphan_errors: list[str] = []
        evidence.validate_package_ledger(
            ledger,
            self.repository,
            self.package_root,
            [first, second],
            orphan_errors,
        )
        self.assertTrue(any("orphan package directories" in error for error in orphan_errors), orphan_errors)

        pruned_ledger_path = self.repository / "pruned-package-ledger.json"
        prune_errors = evidence.write_package_ledger(
            pruned_ledger_path,
            self.repository,
            self.package_root,
            [first, second],
            prune_unselected=True,
        )
        self.assertEqual(prune_errors, [])
        self.assertFalse(orphan.exists())
        pruned_validation_errors: list[str] = []
        evidence.validate_package_ledger(
            _read_json(pruned_ledger_path),
            self.repository,
            self.package_root,
            [first, second],
            pruned_validation_errors,
        )
        self.assertEqual(pruned_validation_errors, [])

    def test_new_ledgers_require_nonempty_assets_and_global_package_sets(self) -> None:
        empty_ledger, generation_issues = evidence.resolved_package_ledger(
            self.repository, self.package_root, []
        )
        semantic_issues: list[str] = []
        evidence.validate_package_ledger_semantics(empty_ledger, semantic_issues)

        for issues in (generation_issues, semantic_issues):
            self.assertIn(
                "Resolved-package ledger must bind at least one assets graph.",
                issues,
            )
            self.assertIn(
                "Resolved-package ledger must bind at least one global package.",
                issues,
            )

        package_free_assets = self._assets(
            "empty/obj/project.assets.json", {}
        )
        _, package_free_issues = evidence.resolved_package_ledger(
            self.repository, self.package_root, [package_free_assets]
        )
        self.assertNotIn(
            "Resolved-package ledger must bind at least one assets graph.",
            package_free_issues,
        )
        self.assertIn(
            "Resolved-package ledger must bind at least one global package.",
            package_free_issues,
        )

    def test_tool_package_is_bound_and_survives_unselected_candidate_pruning(self) -> None:
        runtime = self._package("Runtime.Package", "1.0.0", b"runtime-archive")
        self._package("Build.Tool.Sdk", "2.0.0", b"tool-archive")
        orphan = self._package("Unused.Candidate", "9.0.0", b"unused-archive")
        assets = self._assets(
            "apphost/obj/project.assets.json",
            {"Runtime.Package/1.0.0": runtime},
        )

        ledger, issues = evidence.resolved_package_ledger(
            self.repository,
            self.package_root,
            [assets],
            prune_unselected=True,
            tool_packages=[("Build.Tool.Sdk", "2.0.0")],
        )
        semantic_issues: list[str] = []
        evidence.validate_package_ledger_semantics(ledger, semantic_issues)

        self.assertEqual(issues, [])
        self.assertEqual(semantic_issues, [])
        self.assertEqual(
            [(item["id"], item["version"]) for item in ledger["toolPackages"]],
            [("Build.Tool.Sdk", "2.0.0")],
        )
        self.assertEqual(
            [item["id"] for item in ledger["packages"]],
            ["Build.Tool.Sdk", "Runtime.Package"],
        )
        self.assertFalse(
            (self.package_root / orphan["path"]).exists(),
        )
        validation_errors: list[str] = []
        evidence.validate_package_ledger(
            ledger,
            self.repository,
            self.package_root,
            [assets],
            validation_errors,
            tool_packages=[("Build.Tool.Sdk", "2.0.0")],
        )
        self.assertEqual(validation_errors, [])

    def test_package_root_must_start_fresh_external_and_non_symlinked(self) -> None:
        self.assertEqual(
            evidence.validate_fresh_package_root(self.repository, self.package_root), []
        )
        (self.package_root / "ambient.txt").write_text("ambient\n", encoding="utf-8")
        self.assertTrue(
            any("fresh and empty" in error for error in evidence.validate_fresh_package_root(self.repository, self.package_root))
        )
        symlink = self.repository.parent / "linked-packages"
        symlink.symlink_to(self.package_root, target_is_directory=True)
        self.assertTrue(
            any("symlinked" in error for error in evidence.validate_fresh_package_root(self.repository, symlink))
        )


class EventStoreRuntimeEvidenceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self._temporary.cleanup)
        temporary_root = Path(self._temporary.name)
        self.evidence_root = temporary_root / "evidence"
        self.live_root = temporary_root / "live-evidence"
        self.artifact_root = temporary_root / "artifact-root"
        self.package_root = temporary_root / "packages"
        self.package_root.mkdir()
        self.package_ledger_path = temporary_root / "provider-package-ledger.json"
        self.provider_package_ledger = _synthetic_package_ledger(
            list(evidence.PROVIDER_PACKAGE_ASSETS),
            "2026-09-12T11:00:00+00:00",
            graph_sha256="1" * 64,
        )
        _write_json(self.package_ledger_path, self.provider_package_ledger)
        self.pact_root = self.artifact_root / evidence.CANONICAL_PACT_ROOT
        self.active_root = (
            self.artifact_root / "_bmad-output" / "implementation-artifacts" / "evidence"
            / "eventstore-runtime-identity-v2"
        )
        self.history_root = (
            self.artifact_root / "_bmad-output" / "implementation-artifacts" / "evidence"
            / "pact-provider-reconciliation-history" / "2026-09-08-builds-35c3d1e5"
        )
        self.identity_path = (
            self.artifact_root / "_bmad-output" / "contracts"
            / "frontcomposer-eventstore-approved-runtime-identity-v2.json"
        )
        shutil.copytree(CANONICAL_EVIDENCE, self.evidence_root)
        shutil.copytree(CANONICAL_LIVE_EVIDENCE, self.live_root)
        # The checked-in live packet is the preserved package-less capture. The frozen-hash
        # exemption now applies only to the history evidence root, so the live lane fixture
        # carries a genuine v4 receipt bound to its own ledger sidecar, plus the AppHost
        # sidecar that every live validation requires.
        live_receipt_path = self.live_root / "run-evidence.json"
        live_receipt = _read_json(live_receipt_path)
        live_receipt["schema"] = "hexalith.eventstore.provider-verification-run-evidence.v4"
        live_receipt["packageLedger"] = _write_package_ledger_sidecar(
            self.live_root,
            evidence.PROVIDER_PACKAGE_LEDGER_FILE,
            _synthetic_package_ledger(
                list(evidence.PROVIDER_PACKAGE_ASSETS),
                "2026-09-12T11:00:00+00:00",
                graph_sha256="2" * 64,
            ),
        )
        _write_json(live_receipt_path, live_receipt)
        live_smoke_path = self.live_root / "apphost-smoke.json"
        live_smoke = _read_json(live_smoke_path)
        live_smoke["packageLedger"] = _write_package_ledger_sidecar(
            self.live_root,
            evidence.APPHOST_PACKAGE_LEDGER_FILE,
            _synthetic_package_ledger(
                [evidence.APPHOST_PACKAGE_ASSETS_ROOT],
                "2026-09-12T11:00:00+00:00",
                graph_sha256="2" * 64,
            ),
        )
        _write_json(live_smoke_path, live_smoke)
        shutil.copytree(CANONICAL_PACTS, self.pact_root)
        shutil.copytree(CANONICAL_ACTIVE_EVIDENCE, self.active_root)
        shutil.copytree(CANONICAL_PRIOR_EVIDENCE, self.history_root)
        self.identity_path.parent.mkdir(parents=True)
        shutil.copyfile(CANONICAL_IDENTITY_V2, self.identity_path)
        editorconfig = ROOT / ".editorconfig"
        editorconfig_data = editorconfig.read_bytes()
        manifest_path = self.active_root / "frontcomposer-runtime-inputs.json"
        fixture_manifest = _read_json(manifest_path)
        fixture_manifest["scope"] = evidence._runtime_scope()
        fixture_manifest["entries"] = [
            *(
                item
                for item in fixture_manifest["entries"]
                if item["path"] != ".editorconfig"
            ),
            {
                "path": ".editorconfig",
                "kind": "file",
                "bytes": len(editorconfig_data),
                "sha256": hashlib.sha256(editorconfig_data).hexdigest(),
            },
        ]
        fixture_manifest["entries"].sort(key=lambda item: item["path"])
        fixture_manifest["treeSha256"] = evidence._runtime_tree_sha256(
            fixture_manifest["entries"]
        )
        _write_json(manifest_path, fixture_manifest)
        runtime_binding = {
            "path": evidence.RUNTIME_INPUT_MANIFEST_PATH,
            "sha256": _sha256(manifest_path),
            "treeSha256": fixture_manifest["treeSha256"],
        }
        live_receipt["frontComposerRevision"] = fixture_manifest["capturedRevision"]
        live_receipt["runtimeInputTreeSha256"] = fixture_manifest["treeSha256"]
        _write_json(live_receipt_path, live_receipt)
        # The checked-in AppHost artifact is the last truthful Loop-4 capture. Build a
        # synthetic Loop-5 fixture here so unit tests exercise the new evidence shape
        # without relabelling an execution that did not perform these steps.
        active_smoke_path = self.active_root / "recapture" / "apphost-smoke.json"
        active_smoke = _read_json(active_smoke_path)
        output_preparation = active_smoke["startup"]["outputPreparation"]
        output_preparation["restore"] = "passed"
        output_preparation["restoreMode"] = "forced-no-cache"
        output_preparation["buildControlGitlinks"] = list(
            evidence.APPHOST_BUILD_CONTROL_GITLINKS
        )
        output_preparation["reachableSourceGitlinks"] = list(
            evidence.APPHOST_REACHABLE_SOURCE_GITLINKS
        )
        output_preparation["inactiveGuardedGitlinks"] = list(
            evidence.APPHOST_INACTIVE_GUARDED_GITLINKS
        )
        active_smoke["observations"]["health"] = {
            "result": "passed",
            "authenticated": False,
            "reasonCode": "health.readiness.succeeded",
            "statusCode": 200,
        }
        apphost_graph = evidence.APPHOST_PACKAGE_ASSETS_ROOT
        active_smoke["schema"] = "hexalith.frontcomposer.pact-provider-reconciliation-apphost-smoke.v3"
        active_smoke["capturedAt"] = "2026-09-14T08:02:10+00:00"
        active_smoke["executionStartedAt"] = "2026-09-14T08:02:30+00:00"
        active_smoke["identity"]["runtimeInputCapturedAt"] = "2026-09-12T08:53:30+00:00"
        active_smoke["identity"]["runtimeInputTreeSha256"] = fixture_manifest[
            "treeSha256"
        ]
        active_smoke["packageLedger"] = _write_package_ledger_sidecar(
            self.active_root / "recapture",
            evidence.APPHOST_PACKAGE_LEDGER_FILE,
            _synthetic_package_ledger(
                [apphost_graph],
                "2026-09-14T08:02:00+00:00",
                graph_sha256="3" * 64,
            ),
        )
        active_smoke["topology"]["declaredResources"] = list(
            evidence.APPHOST_DECLARED_RESOURCES
        )
        output_preparation["evaluatedInputBinding"] = {
            "assetsGraphs": [apphost_graph],
            "inputs": [
                {
                    "authority": "repository",
                    "path": "global.json",
                    "sha256": _manifest_entry_sha256("global.json"),
                }
            ],
        }
        output_preparation["runtimeOutputBinding"] = [
            {"path": "net10.0/AppHost.dll", "bytes": 1, "sha256": "6" * 64}
        ]
        active_smoke["authorizationControls"]["projectionSignalR"].update(
            {
                "transport": "websocket-upgrade",
                "negotiatedWith": "valid-bearer",
                "upgradeResult": "rejected-before-switching-protocols",
            }
        )
        active_smoke["cleanup"]["packageAuthorityCleanAfterRun"] = True
        active_smoke["cleanup"]["runtimeOutputsCleanAfterRun"] = True
        _write_json(active_smoke_path, active_smoke)

        # The committed provider receipt is the preserved package-less packet. The
        # frozen-hash exemption is scoped to the history evidence root, so the active
        # lane requires a genuine v4 receipt bound to its own ledger sidecar.
        active_receipt_path = self.active_root / "recapture" / "run-evidence.json"
        active_receipt = _read_json(active_receipt_path)
        active_receipt["schema"] = "hexalith.eventstore.provider-verification-run-evidence.v4"
        active_receipt["frontComposerRevision"] = fixture_manifest["capturedRevision"]
        active_receipt["runtimeInputTreeSha256"] = fixture_manifest["treeSha256"]
        active_receipt["packageLedger"] = _write_package_ledger_sidecar(
            self.active_root / "recapture",
            evidence.PROVIDER_PACKAGE_LEDGER_FILE,
            _synthetic_package_ledger(
                list(evidence.PROVIDER_PACKAGE_ASSETS),
                "2026-09-12T11:00:00+00:00",
                graph_sha256="4" * 64,
            ),
        )
        _write_json(active_receipt_path, active_receipt)

        recapture_hashes = {
            relative: _sha256(self.active_root / "recapture" / relative)
            for relative in sorted(
                {
                    "apphost-smoke.json",
                    "provider-verification.json",
                    "run-evidence.json",
                    *evidence.PACKAGE_LEDGER_FILES,
                }
            )
        }
        evidence_files = [
            {"path": relative, "sha256": digest}
            for relative, digest in recapture_hashes.items()
        ]

        decision_path = self.active_root / "recapture-decision.json"
        decision = _read_json(decision_path)
        decision["runtimeInputs"] = runtime_binding
        decision["evidenceFiles"] = evidence_files
        _write_json(decision_path, decision)
        decision_hash = _sha256(decision_path)

        subject_path = self.active_root / "approval-subject.json"
        subject = _read_json(subject_path)
        subject["runtimeInputs"] = runtime_binding
        subject["decision"]["sha256"] = decision_hash
        subject["evidenceFiles"] = evidence_files
        _write_json(subject_path, subject)
        subject_hash = _sha256(subject_path)

        identity = _read_json(self.identity_path)
        identity["runtimeInputs"] = runtime_binding
        identity["decision"]["sha256"] = decision_hash
        identity["activeEvidence"]["files"] = evidence_files
        identity["approval"]["subject"]["sha256"] = subject_hash
        _write_json(self.identity_path, identity)
        shutil.copyfile(
            ROOT / "_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json",
            self.identity_path.parent / "frontcomposer-eventstore-approved-runtime-identity-v1.json",
        )
        proposal = (
            self.artifact_root / "_bmad-output" / "planning-artifacts"
            / "sprint-change-proposal-2026-09-11.md"
        )
        proposal.parent.mkdir(parents=True)
        shutil.copyfile(
            ROOT / "_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-11.md",
            proposal,
        )
        for relative in (
            "src/Hexalith.FrontComposer.AppHost/Program.cs",
            "src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj",
        ):
            destination = self.artifact_root / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / relative, destination)
        authority_patcher = mock.patch.object(
            evidence,
            "_canonical_active_locations",
            return_value=(
                self.identity_path,
                self.active_root,
                self.history_root,
                self.pact_root,
            ),
        )
        authority_patcher.start()
        self.addCleanup(authority_patcher.stop)
        live_authority_patcher = mock.patch.object(
            evidence,
            "_canonical_live_locations",
            return_value=(self.live_root, self.pact_root),
        )
        live_authority_patcher.start()
        self.addCleanup(live_authority_patcher.stop)
        cached_entries = _read_json(
            self.active_root / "frontcomposer-runtime-inputs.json"
        )["entries"]

        fixture_roots = {
            ROOT.resolve(strict=False),
            self.artifact_root.resolve(strict=False),
        }

        def cached_runtime_snapshot(repository_root: Path) -> tuple[list[dict[str, Any]], list[str]]:
            if repository_root.resolve(strict=False) in fixture_roots:
                return copy.deepcopy(cached_entries), []
            return REAL_RUNTIME_INPUT_SNAPSHOT(repository_root)

        runtime_patcher = mock.patch.object(
            evidence,
            "_runtime_input_snapshot",
            side_effect=cached_runtime_snapshot,
        )
        runtime_patcher.start()
        self.addCleanup(runtime_patcher.stop)

        captured_manifest = _read_json(
            self.active_root / "frontcomposer-runtime-inputs.json"
        )

        def fixture_live_provenance(
            repository_root: Path,
            errors: list[str],
            *,
            runtime_manifest: dict[str, Any] | None = None,
        ) -> dict[str, str]:
            if repository_root.resolve(strict=False) not in fixture_roots:
                return REAL_LIVE_PROVENANCE(
                    repository_root,
                    errors,
                    runtime_manifest=runtime_manifest,
                )
            manifest = runtime_manifest or captured_manifest
            return {
                "sourceSha": evidence.ACTIVE_SOURCE_SHA,
                "releaseVersion": evidence.ACTIVE_VERSION,
                "buildsSha": evidence.ACTIVE_BUILDS_SHA,
                "releaseInventorySha256": evidence.INVENTORY_SHA256,
                "frontComposerRevision": str(manifest["capturedRevision"]),
                "runtimeInputTreeSha256": str(manifest["treeSha256"]),
            }

        provenance_patcher = mock.patch.object(
            evidence,
            "_live_provenance",
            side_effect=fixture_live_provenance,
        )
        provenance_patcher.start()
        self.addCleanup(provenance_patcher.stop)

        def fixture_git(repository_root: Path, *arguments: str) -> str:
            if repository_root.resolve(strict=False) != self.artifact_root.resolve(strict=False):
                return REAL_GIT(repository_root, *arguments)
            if arguments[:2] == ("rev-parse", "HEAD"):
                return str(captured_manifest["capturedRevision"])
            if arguments[:2] == ("rev-parse", "--verify"):
                return str(captured_manifest["capturedRevision"])
            return ""

        git_patcher = mock.patch.object(evidence, "_git", side_effect=fixture_git)
        git_patcher.start()
        self.addCleanup(git_patcher.stop)

        def fixture_git_completed(
            repository_root: Path,
            *arguments: str,
        ) -> subprocess.CompletedProcess[bytes] | None:
            if repository_root.resolve(strict=False) == self.artifact_root.resolve(strict=False):
                return subprocess.CompletedProcess(["git", *arguments], 0, b"", b"")
            return REAL_GIT_COMPLETED(repository_root, *arguments)

        git_completed_patcher = mock.patch.object(
            evidence,
            "_git_completed",
            side_effect=fixture_git_completed,
        )
        git_completed_patcher.start()
        self.addCleanup(git_completed_patcher.stop)

        def fixture_runtime_git_tree(
            repository_root: Path,
            revision: str,
        ) -> tuple[dict[str, tuple[str, str]], list[str]]:
            if repository_root.resolve(strict=False) in fixture_roots:
                return {}, []
            return REAL_RUNTIME_GIT_TREE(repository_root, revision)

        runtime_tree_patcher = mock.patch.object(
            evidence,
            "_runtime_git_tree",
            side_effect=fixture_runtime_git_tree,
        )
        runtime_tree_patcher.start()
        self.addCleanup(runtime_tree_patcher.stop)
        package_patcher = mock.patch.object(
            evidence,
            "validate_package_ledger",
            side_effect=lambda document, *args, **kwargs: (
                datetime.fromisoformat(document["capturedAt"])
                if isinstance(document, dict) and isinstance(document.get("capturedAt"), str)
                else None
            ),
        )
        package_patcher.start()
        self.addCleanup(package_patcher.stop)
        evaluated_input_patcher = mock.patch.object(
            evidence,
            "_evaluate_apphost_inputs",
            side_effect=lambda *_args, **_kwargs: (
                _read_json(self.live_root / "apphost-smoke.json")
                .get("startup", {})
                .get("outputPreparation", {})
                .get("evaluatedInputBinding")
            ),
        )
        evaluated_input_patcher.start()
        self.addCleanup(evaluated_input_patcher.stop)
        runtime_output_patcher = mock.patch.object(
            evidence,
            "_apphost_runtime_output_binding",
            side_effect=lambda *_args, **_kwargs: (
                _read_json(self.live_root / "apphost-smoke.json")
                .get("startup", {})
                .get("outputPreparation", {})
                .get("runtimeOutputBinding", [])
            ),
        )
        runtime_output_patcher.start()
        self.addCleanup(runtime_output_patcher.stop)

    def validate(self) -> list[str]:
        return evidence.validate(self.evidence_root, self.pact_root)

    def validate_live(self) -> list[str]:
        return evidence.validate_live(
            self.live_root,
            self.pact_root,
            ROOT,
            provider_package_root=self.package_root,
            apphost_package_root=self.package_root,
            runtime_input_manifest_path=(
                self.active_root / "frontcomposer-runtime-inputs.json"
            ),
        )

    def validate_active(self) -> tuple[list[str], list[str], bool]:
        return evidence.validate_active(
            self.identity_path,
            self.active_root,
            self.history_root,
            self.pact_root,
            self.artifact_root,
        )

    def write_live_receipt(self) -> list[str]:
        return evidence.write_live_receipt(
            self.live_root,
            self.artifact_root,
            runtime_input_manifest_path=(
                self.active_root / "frontcomposer-runtime-inputs.json"
            ),
            pact_dir=self.pact_root,
            package_ledger_path=self.package_ledger_path,
            package_root=self.package_root,
        )

    def make_live_apphost_pass(self) -> None:
        provenance_errors: list[str] = []
        provenance = evidence._live_provenance(ROOT, provenance_errors)
        self.assertEqual(provenance_errors, [])
        runtime_captured_at = _read_json(
            CANONICAL_ACTIVE_EVIDENCE / "frontcomposer-runtime-inputs.json"
        )["capturedAt"]
        apphost_graph = evidence.APPHOST_PACKAGE_ASSETS_ROOT
        package_ledger = _write_package_ledger_sidecar(
            self.live_root,
            evidence.APPHOST_PACKAGE_LEDGER_FILE,
            _synthetic_package_ledger(
                [apphost_graph],
                "2026-09-13T17:00:01+00:00",
                graph_sha256="7" * 64,
            ),
        )
        document = {
            "schema": "hexalith.frontcomposer.pact-provider-reconciliation-apphost-smoke.v3",
            "capturedAt": "2026-09-13T17:00:00+00:00",
            "executionStartedAt": "2026-09-13T17:00:02+00:00",
            "completedAt": "2026-09-13T17:00:10+00:00",
            "timeoutSeconds": 30,
            "finalVerdict": "passed",
            "reasonCodes": [],
            "packageLedger": package_ledger,
            "identity": {
                "eventStoreSourceSha": provenance["sourceSha"],
                "eventStoreReleaseVersion": provenance["releaseVersion"],
                "buildsCatalogSha": provenance["buildsSha"],
                "frontComposerRevision": provenance["frontComposerRevision"],
                "runtimeInputTreeSha256": provenance["runtimeInputTreeSha256"],
                "runtimeInputCapturedAt": runtime_captured_at,
            },
            "topology": {
                "programPath": "src/Hexalith.FrontComposer.AppHost/Program.cs",
                "programSha256": _sha256(ROOT / "src/Hexalith.FrontComposer.AppHost/Program.cs"),
                "projectPath": "src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj",
                "projectSha256": _sha256(ROOT / "src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj"),
                "modifiedForSmoke": False,
                "declaredResources": list(evidence.APPHOST_DECLARED_RESOURCES),
            },
            "startup": {
                "result": "passed",
                "hostStartAttempted": True,
                "hostStarted": True,
                "outputPreparation": {
                    "clean": "passed",
                    "restore": "passed",
                    "build": "passed",
                    "configuration": "Debug",
                    "restoreMode": "forced-no-cache",
                    "buildMode": "no-incremental",
                    "startMode": "no-build",
                    "evaluatedBuildProperties": evidence.APPHOST_BUILD_PROPERTIES,
                    "sourceDependencyGitlinks": list(evidence.RUNTIME_DEPENDENCY_GITLINKS),
                    "buildControlGitlinks": list(evidence.APPHOST_BUILD_CONTROL_GITLINKS),
                    "reachableSourceGitlinks": list(evidence.APPHOST_REACHABLE_SOURCE_GITLINKS),
                    "inactiveGuardedGitlinks": list(evidence.APPHOST_INACTIVE_GUARDED_GITLINKS),
                    "evaluatedSourceGraph": "passed",
                    "evaluatedInputBinding": {
                        "assetsGraphs": [apphost_graph],
                        "inputs": [
                            {
                                "authority": "repository",
                                "path": "global.json",
                                "sha256": _manifest_entry_sha256("global.json"),
                            }
                        ],
                    },
                    "runtimeOutputBinding": [
                        {"path": "net10.0/AppHost.dll", "bytes": 1, "sha256": "9" * 64}
                    ],
                },
                "resourceWaits": {
                    "security": "healthy",
                    "eventstore": "healthy",
                    "eventstore-admin": "healthy",
                    "eventstore-admin-ui": "healthy",
                    "tenants": "healthy",
                    "parties": "healthy",
                    "sample": "healthy",
                    "tenants-ui": "healthy",
                    "frontcomposer-ui": "healthy",
                    "counter-web": "healthy",
                },
            },
            "observations": {
                "health": {
                    "result": "passed", "authenticated": False,
                    "reasonCode": "health.readiness.succeeded", "statusCode": 200,
                },
                "commandSubmit": {
                    "result": "passed", "authenticated": True,
                    "reasonCode": "command.accepted", "statusCode": 202,
                    "aggregateId": "pact-reconciliation-01m2aw1vw8z2ghq0zgcttcwww0",
                    "messageId": "01M2AW1VW8Z2GHQ0ZGCTTCWWW0",
                    "correlationId": "01M2AW1VW8Z2GHQ0ZGCTTCWWW0",
                },
                "commandStatus": {
                    "result": "passed", "authenticated": True,
                    "reasonCode": "command.completed", "terminalStatus": "Completed",
                    "aggregateId": "pact-reconciliation-01m2aw1vw8z2ghq0zgcttcwww0",
                },
                "queryProvenance": {
                    "result": "passed", "authenticated": True,
                    "reasonCode": "query.handler-computed", "statusCode": 200,
                    "provenance": "HandlerComputed", "tenant": "system",
                    "aggregateId": "pact-reconciliation-01m2aw1vw8z2ghq0zgcttcwww0",
                    "entityId": "pact-reconciliation-01m2aw1vw8z2ghq0zgcttcwww0",
                    "responseTenantId": "pact-reconciliation-01m2aw1vw8z2ghq0zgcttcwww0",
                },
                "projectionSignalR": {
                    "result": "passed", "authenticated": True,
                    "reasonCode": "signalr.authenticated-connect.succeeded",
                    "endpoint": "http://127.0.0.1:18001/hubs/projection-changes",
                },
            },
            "authorizationControls": {
                "commandSubmit": {
                    "result": "passed",
                    "credential": "invalid-bearer",
                    "reasonCode": "authorization.invalid-bearer.rejected",
                    "statusCode": 401,
                },
                "commandStatus": {
                    "result": "passed",
                    "credential": "invalid-bearer",
                    "reasonCode": "authorization.invalid-bearer.rejected",
                    "statusCode": 401,
                },
                "queryProvenance": {
                    "result": "passed",
                    "credential": "invalid-bearer",
                    "reasonCode": "authorization.invalid-bearer.rejected",
                    "statusCode": 401,
                },
                "projectionSignalR": {
                    "result": "passed",
                    "credential": "invalid-bearer",
                    "reasonCode": "authorization.invalid-bearer.rejected",
                    "statusCode": 401,
                    "endpoint": "http://127.0.0.1:18001/hubs/projection-changes",
                    "transport": "websocket-upgrade",
                    "negotiatedWith": "valid-bearer",
                    "upgradeResult": "rejected-before-switching-protocols",
                },
            },
            "cleanup": {
                "command": "aspire stop --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --nologo",
                "result": "clean",
                "hostStopped": True,
                "portsClosed": True,
                "runningAppHostsAfterAttempt": 0,
                "listenerConfirmation": "ports-probed-closed",
                "confirmation": "aspire-ps-empty",
                "runtimeInputsCleanAfterRun": True,
                "packageAuthorityCleanAfterRun": True,
                "runtimeOutputsCleanAfterRun": True,
                "daprNameResolutionFiles": {
                    "absentBeforeRun": [
                        "src/Hexalith.FrontComposer.AppHost/nr.db",
                        "src/Hexalith.FrontComposer.AppHost/nr.db-shm",
                        "src/Hexalith.FrontComposer.AppHost/nr.db-wal",
                    ],
                    "createdByInvocation": [],
                    "removedAfterShutdown": [],
                    "remainingAfterCleanup": [],
                },
            },
        }
        _write_json(self.live_root / "apphost-smoke.json", document)

    def claim_active_approval(self, *, transferred_eventstore_role: bool = False) -> None:
        identity = _read_json(self.identity_path)
        policy_path = self.active_root / "approval-policy.json"
        policy = _read_json(policy_path)
        roster_path = self.active_root / "reviewer-roster.json"
        roster = _read_json(roster_path)
        actors = {
            "eventstore-maintainer": "github:eventstore-maintainer",
            "frontcomposer-maintainer": "github:frontcomposer-maintainer",
            "release-owner": "github:release-owner",
            "accountable-frontcomposer-maintainer": "github:accountable-frontcomposer-maintainer",
            "product-owner": "github:product-owner",
            "architect": "github:architect",
        }
        sources = {
            role: f"https://example.test/approvals/{role}"
            for role in actors
        }
        bootstrap = {
            role: {actors[role]: frozenset({sources[role]})}
            for role in evidence.POLICY_ROLES
        }
        bootstrap_patch = mock.patch.object(
            evidence, "APPROVAL_AUTHORITY_BOOTSTRAP", bootstrap
        )
        bootstrap_patch.start()
        self.addCleanup(bootstrap_patch.stop)
        principal_patch = mock.patch.object(
            evidence,
            "APPROVAL_PRINCIPAL_BOOTSTRAP",
            {
                actor: f"principal:{role}"
                for role, actor in actors.items()
            },
        )
        principal_patch.start()
        self.addCleanup(principal_patch.stop)
        for assignment in policy["assignments"]:
            role = assignment["role"]
            assignment["authorities"] = [{
                "actor": actors[role],
                "durableSources": [sources[role]],
            }]
        _write_json(policy_path, policy)
        policy_hash = _sha256(policy_path)

        effective_roles = (
            [
                "frontcomposer-maintainer",
                "accountable-frontcomposer-maintainer",
                "release-owner",
            ]
            if transferred_eventstore_role
            else list(evidence.DEFAULT_REQUIRED_ROLES)
        )
        roster["policySha256"] = policy_hash
        roster["effectiveRequiredRoles"] = effective_roles
        for assignment in roster["assignments"]:
            role = assignment["role"]
            assignment["actors"] = [actors[role]] if role in effective_roles else []

        oi18_subject_hash: str | None = None
        if transferred_eventstore_role:
            oi18_dir = self.active_root / "oi18"
            oi18_dir.mkdir(parents=True, exist_ok=True)
            transfer_path = oi18_dir / "ownership-transfer-subject.json"
            _write_json(transfer_path, {
                "schema": "hexalith.frontcomposer.eventstore-runtime-ownership-transfer-subject.v2",
                "frozenAt": "2026-09-12T09:58:25+00:00",
                "policySha256": policy_hash,
                "activeTuple": identity["activeTuple"],
                "decision": "remove-eventstore-maintainer-and-substitute-accountable-frontcomposer-maintainer",
                "removedRole": "eventstore-maintainer",
                "replacementRole": "accountable-frontcomposer-maintainer",
                "replacementActors": [actors["accountable-frontcomposer-maintainer"]],
                "effectiveRequiredRoles": effective_roles,
            })
            oi18_subject_hash = _sha256(transfer_path)
            prerequisite_bindings: dict[str, dict[str, str]] = {}
            for accepted_at, role in (
                ("2026-09-12T09:58:30+00:00", "product-owner"),
                ("2026-09-12T09:58:35+00:00", "architect"),
            ):
                filename = f"{role}.json"
                path = oi18_dir / filename
                _write_json(path, {
                    "schema": "hexalith.frontcomposer.eventstore-runtime-ownership-transfer-receipt.v2",
                    "subjectSha256": oi18_subject_hash,
                    "policySha256": policy_hash,
                    "actor": actors[role],
                    "role": role,
                    "decision": "ownership-transfer-approved",
                    "removedRole": "eventstore-maintainer",
                    "replacementRole": "accountable-frontcomposer-maintainer",
                    "replacementActors": [actors["accountable-frontcomposer-maintainer"]],
                    "acceptedAt": accepted_at,
                    "durableSource": sources[role],
                    "statement": evidence.OI18_APPROVAL_STATEMENT,
                })
                prerequisite_bindings[role] = {
                    "path": path.relative_to(self.artifact_root).as_posix(),
                    "sha256": _sha256(path),
                }
            roster["oi18"] = {
                "status": "approved",
                "transferSubject": {
                    "path": transfer_path.relative_to(self.artifact_root).as_posix(),
                    "sha256": oi18_subject_hash,
                },
                "productApprovalReceipt": prerequisite_bindings["product-owner"],
                "architectureApprovalReceipt": prerequisite_bindings["architect"],
                "replacementRole": "accountable-frontcomposer-maintainer",
                "replacementActors": [actors["accountable-frontcomposer-maintainer"]],
            }
        _write_json(roster_path, roster)
        roster_hash = _sha256(roster_path)

        subject_path = self.active_root / "approval-subject.json"
        subject = _read_json(subject_path)
        subject["policy"] = {
            "path": evidence.APPROVAL_POLICY_PATH,
            "sha256": policy_hash,
        }
        subject["roster"] = {
            "path": evidence.APPROVAL_ROSTER_PATH,
            "sha256": roster_hash,
        }
        subject["effectiveRequiredRoles"] = effective_roles
        subject["oi18"] = roster["oi18"]
        _write_json(subject_path, subject)
        subject_hash = _sha256(subject_path)

        identity["approval"]["policy"]["sha256"] = policy_hash
        identity["approval"]["roster"]["sha256"] = roster_hash
        identity["approval"]["subject"]["sha256"] = subject_hash
        identity["approval"]["effectiveRequiredRoles"] = effective_roles
        receipt_dir = self.active_root / "receipts"
        receipt_dir.mkdir(parents=True, exist_ok=True)
        receipt_bindings: list[dict[str, str]] = []
        for index, role in enumerate(effective_roles):
            filename = f"{role}.json"
            path = receipt_dir / filename
            _write_json(path, {
                "schema": "hexalith.frontcomposer.eventstore-runtime-approval-receipt.v2",
                "subjectSha256": subject_hash,
                "policySha256": policy_hash,
                "rosterSha256": roster_hash,
                "activeTuple": subject["activeTuple"],
                "runtimeInputs": subject["runtimeInputs"],
                "evidenceFiles": subject["evidenceFiles"],
                "effectiveRequiredRoles": effective_roles,
                "oi18SubjectSha256": oi18_subject_hash,
                "actor": actors[role],
                "role": role,
                "decision": "approved",
                "acceptedAt": _offset_timestamp(subject["frozenAt"], 10 + index),
                "durableSource": sources[role],
                "statement": evidence.ACTIVE_APPROVAL_STATEMENT,
            })
            receipt_bindings.append({
                "role": role,
                "path": path.relative_to(self.artifact_root).as_posix(),
                "sha256": _sha256(path),
            })
        identity["approval"]["receipts"] = receipt_bindings
        identity["approval"]["migrationApprovalClaimed"] = True
        _write_json(self.identity_path, identity)

    def repin_active_recapture(
        self,
        relative: str,
        mutate: Callable[[dict[str, Any]], None],
    ) -> None:
        path = self.active_root / "recapture" / relative
        document = _read_json(path)
        mutate(document)
        _write_json(path, document)

        identity = _read_json(self.identity_path)
        file_binding = next(
            item for item in identity["activeEvidence"]["files"]
            if item["path"] == relative
        )
        file_binding["sha256"] = _sha256(path)
        decision_path = self.active_root / "recapture-decision.json"
        decision = _read_json(decision_path)
        decision["evidenceFiles"] = identity["activeEvidence"]["files"]
        _write_json(decision_path, decision)
        identity["decision"]["sha256"] = _sha256(decision_path)

        subject_path = self.active_root / "approval-subject.json"
        subject = _read_json(subject_path)
        subject["evidenceFiles"] = identity["activeEvidence"]["files"]
        subject["decision"]["sha256"] = identity["decision"]["sha256"]
        _write_json(subject_path, subject)
        identity["approval"]["subject"]["sha256"] = _sha256(subject_path)
        _write_json(self.identity_path, identity)

    def repin_active_package_ledger(
        self,
        owner: str,
        name: str,
        mutate: Callable[[dict[str, Any]], None],
    ) -> None:
        """Mutate one sealed ledger sidecar and repin the document that binds its bytes."""
        sidecar = self.active_root / "recapture" / name
        document = _read_json(sidecar)
        mutate(document)
        binding = _write_package_ledger_sidecar(
            self.active_root / "recapture", name, document
        )
        self.repin_active_recapture(
            owner, lambda packet: packet.__setitem__("packageLedger", binding)
        )
        self.repin_active_recapture(name, lambda _: None)

    def mutate_live_package_ledger(
        self,
        owner: str,
        name: str,
        mutate: Callable[[dict[str, Any]], None],
    ) -> None:
        """Mutate one live ledger sidecar and rebind its owning evidence document."""
        sidecar = self.live_root / name
        document = _read_json(sidecar)
        mutate(document)
        binding = _write_package_ledger_sidecar(self.live_root, name, document)
        owner_path = self.live_root / owner
        packet = _read_json(owner_path)
        packet["packageLedger"] = binding
        _write_json(owner_path, packet)

    def repin_active_roster(self, roster: dict[str, Any]) -> None:
        roster_path = self.active_root / "reviewer-roster.json"
        _write_json(roster_path, roster)
        roster_hash = _sha256(roster_path)
        subject_path = self.active_root / "approval-subject.json"
        subject = _read_json(subject_path)
        subject["roster"]["sha256"] = roster_hash
        subject["effectiveRequiredRoles"] = roster["effectiveRequiredRoles"]
        subject["oi18"] = roster["oi18"]
        _write_json(subject_path, subject)
        identity = _read_json(self.identity_path)
        identity["approval"]["roster"]["sha256"] = roster_hash
        identity["approval"]["subject"]["sha256"] = _sha256(subject_path)
        identity["approval"]["effectiveRequiredRoles"] = roster["effectiveRequiredRoles"]
        _write_json(self.identity_path, identity)

    def repin_captured(self, *relatives: str) -> None:
        """Re-pin captured bytes so a test can exercise structure, not the capture pin."""
        original = dict(evidence.CAPTURED_EVIDENCE_SHA256)
        self.addCleanup(
            lambda: (
                evidence.CAPTURED_EVIDENCE_SHA256.clear(),
                evidence.CAPTURED_EVIDENCE_SHA256.update(original),
            )
        )
        for relative in relatives:
            evidence.CAPTURED_EVIDENCE_SHA256[relative] = _sha256(self.evidence_root / relative)

    def repin_report(self) -> None:
        self.repin_captured(
            "provider-verification/provider-verification.json",
            "provider-verification/run-evidence.json",
        )

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
            # `text: unset` makes Git treat the path as binary, so `eol` is never consulted;
            # the byte identity below is the guarantee, not an inert `-eol` attribute.
            self.assertEqual(attribute, f"{relative}: text: unset")
            raw_blob = _git_output("hash-object", "--no-filters", "--", relative)
            checkout_blob = _git_output("hash-object", f"--path={relative}", "--", relative)
            self.assertEqual(checkout_blob, raw_blob, f"checkout filters must preserve {relative}")

    def test_active_and_prior_evidence_checkout_policy_preserves_exact_bytes(self) -> None:
        paths = [
            *CANONICAL_ACTIVE_EVIDENCE.rglob("*"),
            *CANONICAL_PRIOR_EVIDENCE.rglob("*"),
        ]
        for path in sorted(item for item in paths if item.is_file()):
            relative = path.relative_to(ROOT).as_posix()
            self.assertEqual(_git_output("check-attr", "text", "--", relative), f"{relative}: text: unset")
            self.assertEqual(
                _git_output("hash-object", f"--path={relative}", "--", relative),
                _git_output("hash-object", "--no-filters", "--", relative),
            )
        proposal = "_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-11.md"
        self.assertEqual(_git_output("check-attr", "text", "--", proposal), f"{proposal}: text: unset")
        self.assertEqual(
            _git_output("hash-object", f"--path={proposal}", "--", proposal),
            _git_output("hash-object", "--no-filters", "--", proposal),
        )

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

    def test_historical_archive_does_not_compare_report_to_mutated_live_pact_bytes(self) -> None:
        pact_path = self.pact_root / evidence.PACT_FILES[0]
        pact = _read_json(pact_path)
        pact["metadata"]["story1124Mutation"] = "same-interactions-different-bytes"
        _write_json(pact_path, pact)

        self.assertEqual(self.validate(), [])

    def test_historical_archive_does_not_treat_the_live_state_catalog_as_hash_authority(self) -> None:
        catalog_path = self.pact_root / "provider-state-catalog.json"
        catalog = _read_json(catalog_path)
        extra = copy.deepcopy(catalog["states"][0])
        extra["name"] = "undeclared-extra-state"
        catalog["states"].append(extra)
        _write_json(catalog_path, catalog)

        self.assertEqual(self.validate(), [])

    def test_historical_archive_does_not_treat_the_live_manifest_as_hash_authority(self) -> None:
        manifest_path = self.pact_root / "interaction-manifest.json"
        manifest = _read_json(manifest_path)
        manifest["pactFiles"].pop()
        _write_json(manifest_path, manifest)

        self.assertEqual(self.validate(), [])

    def test_active_v2_accepts_exact_evidence_while_approval_remains_open(self) -> None:
        errors, approval_issues, claimed = self.validate_active()

        self.assertEqual(errors, [])
        self.assertFalse(claimed)
        for role in evidence.ACTIVE_REQUIRED_ROLES:
            self.assertTrue(
                any(f"Missing valid receipt for required role: {role}" in issue for issue in approval_issues),
                approval_issues,
            )

    def test_active_v2_rejects_history_tamper_independently(self) -> None:
        (self.history_root / "apphost-smoke.json").write_bytes(
            (self.history_root / "apphost-smoke.json").read_bytes() + b" "
        )

        errors, _, _ = self.validate_active()

        self.assertTrue(any("Prior compatibility archive is not byte-identical" in error for error in errors), errors)

    def test_active_v2_rejects_detached_evidence_and_history_roots(self) -> None:
        detached_active = Path(self._temporary.name) / "detached-active"
        detached_history = Path(self._temporary.name) / "detached-history"
        shutil.copytree(self.active_root, detached_active)
        shutil.copytree(self.history_root, detached_history)

        errors, _, _ = evidence.validate_active(
            self.identity_path,
            detached_active,
            detached_history,
            self.pact_root,
            ROOT,
        )

        self.assertTrue(any("identity's canonical coordinate" in error for error in errors), errors)

    def test_active_v2_rejects_a_detached_pact_directory(self) -> None:
        detached_pacts = Path(self._temporary.name) / "detached-pacts"
        shutil.copytree(self.pact_root, detached_pacts)

        errors, _, _ = evidence.validate_active(
            self.identity_path,
            self.active_root,
            self.history_root,
            detached_pacts,
            ROOT,
        )

        self.assertTrue(
            any("canonical repository Pact directory" in error for error in errors),
            errors,
        )

    def test_repository_root_fixes_every_active_authority_coordinate(self) -> None:
        self.assertEqual(
            REAL_CANONICAL_ACTIVE_LOCATIONS(ROOT),
            (
                CANONICAL_IDENTITY_V2,
                CANONICAL_ACTIVE_EVIDENCE,
                CANONICAL_PRIOR_EVIDENCE,
                CANONICAL_PACTS,
            ),
        )
        with mock.patch.object(
            evidence,
            "_canonical_active_locations",
            side_effect=REAL_CANONICAL_ACTIVE_LOCATIONS,
        ):
            errors, _, _ = evidence.validate_active(
                self.identity_path,
                self.active_root,
                self.history_root,
                self.pact_root,
                ROOT,
            )
        self.assertTrue(
            any("fixed repository identity coordinate" in error for error in errors),
            errors,
        )

    def test_symlink_loop_in_an_authority_coordinate_fails_deterministically(self) -> None:
        first = Path(self._temporary.name) / "authority-loop-a"
        second = Path(self._temporary.name) / "authority-loop-b"
        first.symlink_to(second)
        second.symlink_to(first)

        self.assertFalse(evidence._same_canonical_path(first, second))

    def test_active_v2_rejects_stale_builds_revision_and_evidence_hash(self) -> None:
        identity = _read_json(self.identity_path)
        identity["activeTuple"]["buildsCatalogGitlink"] = evidence.PRIOR_BUILDS_SHA
        identity["frontComposerRevision"] = "0" * 40
        _write_json(self.identity_path, identity)
        report_path = self.active_root / "recapture/provider-verification.json"
        report_path.write_bytes(report_path.read_bytes() + b" ")

        errors, _, _ = self.validate_active()

        self.assertTrue(any("exact approved current tuple" in error for error in errors), errors)
        self.assertTrue(any("revision differs from the runtime-input capture" in error for error in errors), errors)
        self.assertTrue(any("Active recapture SHA-256 mismatch" in error for error in errors), errors)

    def test_active_predecessor_rejects_integer_for_boolean(self) -> None:
        identity = _read_json(self.identity_path)
        identity["predecessor"]["supersededForActiveReleaseSelectionOnly"] = 1
        _write_json(self.identity_path, identity)

        errors, _, _ = self.validate_active()

        self.assertTrue(any("immutable identity v1 as its predecessor" in error for error in errors), errors)

    def test_active_v2_rejects_undeclared_or_leaking_evidence(self) -> None:
        unexpected = self.active_root / "unexpected.json"
        unexpected.write_text('{"Authorization": "Bearer header.payload.signature"}\n', encoding="utf-8")

        errors, _, _ = self.validate_active()

        self.assertTrue(any("missing or undeclared files" in error for error in errors), errors)
        self.assertTrue(any("Redaction scan failed for unexpected.json" in error for error in errors), errors)

    def test_claimed_approval_fails_with_actionable_missing_roles(self) -> None:
        identity = _read_json(self.identity_path)
        identity["approval"]["migrationApprovalClaimed"] = True
        _write_json(self.identity_path, identity)

        errors, approval_issues, claimed = self.validate_active()

        self.assertTrue(claimed)
        for role in evidence.ACTIVE_REQUIRED_ROLES:
            self.assertTrue(any(role in error for error in errors), errors)
            self.assertTrue(any(role in issue for issue in approval_issues), approval_issues)

    def test_distinct_post_freeze_role_receipts_can_authorize_the_active_subject(self) -> None:
        self.claim_active_approval()

        errors, approval_issues, claimed = self.validate_active()

        self.assertTrue(claimed)
        self.assertEqual(approval_issues, [])
        self.assertEqual(errors, [])

    def test_complete_approval_cannot_remain_claimed_false(self) -> None:
        self.claim_active_approval()
        identity = _read_json(self.identity_path)
        identity["approval"]["migrationApprovalClaimed"] = False
        _write_json(self.identity_path, identity)

        errors, approval_issues, claimed = self.validate_active()

        self.assertFalse(claimed)
        self.assertEqual(approval_issues, [])
        self.assertTrue(any("approval is complete" in error for error in errors), errors)

    def test_forged_role_receipt_is_rejected_when_approval_is_claimed(self) -> None:
        self.claim_active_approval()
        identity = _read_json(self.identity_path)
        binding = next(item for item in identity["approval"]["receipts"] if item["role"] == "eventstore-maintainer")
        path = self.artifact_root / binding["path"]
        receipt = _read_json(path)
        receipt["role"] = "frontcomposer-maintainer"
        _write_json(path, receipt)
        binding["sha256"] = _sha256(path)
        _write_json(self.identity_path, identity)

        errors, _, claimed = self.validate_active()

        self.assertTrue(claimed)
        self.assertTrue(any("Receipt role does not bind" in error for error in errors), errors)

    def test_active_receipt_rejects_wrong_actor_source_and_decision(self) -> None:
        self.claim_active_approval()
        original_identity = _read_json(self.identity_path)
        binding = next(
            item for item in original_identity["approval"]["receipts"]
            if item["role"] == "eventstore-maintainer"
        )
        path = self.artifact_root / binding["path"]
        original_receipt = _read_json(path)
        mutations: tuple[tuple[str, Callable[[dict[str, Any]], None], str], ...] = (
            (
                "actor",
                lambda receipt: receipt.__setitem__("actor", "github:release-owner"),
                "Receipt actor is not assigned",
            ),
            (
                "durableSource",
                lambda receipt: receipt.__setitem__(
                    "durableSource", "https://example.test/approvals/not-authorized"
                ),
                "actor/source is not authorized",
            ),
            (
                "decision",
                lambda receipt: receipt.__setitem__("decision", "rejected"),
                "Receipt decision does not bind",
            ),
        )
        for field, mutate, expected in mutations:
            with self.subTest(field=field):
                receipt = copy.deepcopy(original_receipt)
                mutate(receipt)
                _write_json(path, receipt)
                identity = copy.deepcopy(original_identity)
                target = next(
                    item for item in identity["approval"]["receipts"]
                    if item["role"] == "eventstore-maintainer"
                )
                target["sha256"] = _sha256(path)
                _write_json(self.identity_path, identity)

                errors, approval_issues, claimed = self.validate_active()

                self.assertTrue(claimed)
                self.assertTrue(any(expected in issue for issue in approval_issues), approval_issues)
                self.assertTrue(errors)

        _write_json(path, original_receipt)
        _write_json(self.identity_path, original_identity)

    def test_oi18_product_and_architecture_receipts_precede_transferred_ownership(self) -> None:
        self.claim_active_approval(transferred_eventstore_role=True)

        errors, approval_issues, claimed = self.validate_active()

        self.assertTrue(claimed)
        self.assertEqual(approval_issues, [])
        self.assertEqual(errors, [])

    def test_oi18_receipt_rejects_wrong_actor_source_and_decision(self) -> None:
        mutations: tuple[tuple[str, Callable[[dict[str, Any]], None], str], ...] = (
            (
                "actor",
                lambda receipt: receipt.__setitem__("actor", "github:architect"),
                "actor/source is not authorized",
            ),
            (
                "durableSource",
                lambda receipt: receipt.__setitem__(
                    "durableSource", "https://example.test/approvals/not-authorized"
                ),
                "actor/source is not authorized",
            ),
            (
                "decision",
                lambda receipt: receipt.__setitem__("decision", "ownership-transfer-rejected"),
                "receipt decision is invalid",
            ),
        )
        for field, mutate, expected in mutations:
            with self.subTest(field=field):
                shutil.rmtree(self.active_root)
                shutil.copytree(CANONICAL_ACTIVE_EVIDENCE, self.active_root)
                shutil.copyfile(CANONICAL_IDENTITY_V2, self.identity_path)
                self.claim_active_approval(transferred_eventstore_role=True)
                roster = _read_json(self.active_root / "reviewer-roster.json")
                binding = roster["oi18"]["productApprovalReceipt"]
                path = self.artifact_root / binding["path"]
                receipt = _read_json(path)
                mutate(receipt)
                _write_json(path, receipt)
                binding["sha256"] = _sha256(path)
                self.repin_active_roster(roster)

                errors, approval_issues, claimed = self.validate_active()

                self.assertTrue(claimed)
                self.assertTrue(any(expected in issue for issue in approval_issues), approval_issues)
                self.assertTrue(errors)

    def test_active_receipt_validator_rejects_every_missing_field(self) -> None:
        self.claim_active_approval()
        original_identity = _read_json(self.identity_path)
        binding = next(
            item for item in original_identity["approval"]["receipts"]
            if item["role"] == "eventstore-maintainer"
        )
        path = self.artifact_root / binding["path"]
        original_receipt = _read_json(path)

        for field in sorted(original_receipt):
            with self.subTest(field=field):
                receipt = copy.deepcopy(original_receipt)
                receipt.pop(field)
                _write_json(path, receipt)
                identity = copy.deepcopy(original_identity)
                target = next(
                    item for item in identity["approval"]["receipts"]
                    if item["role"] == "eventstore-maintainer"
                )
                target["sha256"] = _sha256(path)
                _write_json(self.identity_path, identity)

                errors, approval_issues, claimed = self.validate_active()

                self.assertTrue(claimed)
                self.assertTrue(approval_issues, (field, errors))
                self.assertTrue(
                    any("eventstore-maintainer" in issue for issue in approval_issues),
                    (field, approval_issues),
                )

        _write_json(path, original_receipt)

    def test_active_receipt_rejects_every_present_but_wrong_subject_binding(self) -> None:
        self.claim_active_approval()
        original_identity = _read_json(self.identity_path)
        binding = next(
            item for item in original_identity["approval"]["receipts"]
            if item["role"] == "eventstore-maintainer"
        )
        path = self.artifact_root / binding["path"]
        original_receipt = _read_json(path)
        mutations: dict[str, Callable[[dict[str, Any]], None]] = {
            "subjectSha256": lambda receipt: receipt.__setitem__("subjectSha256", "0" * 64),
            "policySha256": lambda receipt: receipt.__setitem__("policySha256", "0" * 64),
            "rosterSha256": lambda receipt: receipt.__setitem__("rosterSha256", "0" * 64),
            # Mutate a key the receipt actually declares, so this exercises a
            # present-but-wrong value rather than extra-key rejection.
            "activeTuple": lambda receipt: receipt["activeTuple"].__setitem__(
                "eventStorePackageVersion", "0.0.0"
            ),
            "runtimeInputs": lambda receipt: receipt["runtimeInputs"].__setitem__("sha256", "0" * 64),
            "evidenceFiles": lambda receipt: receipt["evidenceFiles"][0].__setitem__("sha256", "0" * 64),
            "effectiveRequiredRoles": lambda receipt: receipt.__setitem__(
                "effectiveRequiredRoles", list(reversed(receipt["effectiveRequiredRoles"]))
            ),
            "oi18SubjectSha256": lambda receipt: receipt.__setitem__("oi18SubjectSha256", False),
        }
        for field, mutate in mutations.items():
            with self.subTest(field=field):
                receipt = copy.deepcopy(original_receipt)
                mutate(receipt)
                _write_json(path, receipt)
                identity = copy.deepcopy(original_identity)
                target = next(
                    item for item in identity["approval"]["receipts"]
                    if item["role"] == "eventstore-maintainer"
                )
                target["sha256"] = _sha256(path)
                _write_json(self.identity_path, identity)

                errors, approval_issues, claimed = self.validate_active()

                self.assertTrue(claimed)
                self.assertTrue(approval_issues, (field, errors))
                self.assertTrue(
                    any("eventstore-maintainer" in issue for issue in approval_issues),
                    (field, approval_issues),
                )
        _write_json(self.identity_path, original_identity)

    def test_active_and_oi18_receipts_require_exact_affirmative_statements(self) -> None:
        self.claim_active_approval(transferred_eventstore_role=True)
        identity = _read_json(self.identity_path)
        active_binding = next(
            item for item in identity["approval"]["receipts"]
            if item["role"] == "frontcomposer-maintainer"
        )
        active_path = self.artifact_root / active_binding["path"]
        active_receipt = _read_json(active_path)
        active_receipt["statement"] = "I reject this migration subject."
        _write_json(active_path, active_receipt)

        roster = _read_json(self.active_root / "reviewer-roster.json")
        transfer_path = self.artifact_root / roster["oi18"]["productApprovalReceipt"]["path"]
        transfer_receipt = _read_json(transfer_path)
        transfer_receipt["statement"] = "I reject this ownership transfer."
        _write_json(transfer_path, transfer_receipt)

        _, approval_issues, claimed = self.validate_active()

        self.assertTrue(claimed)
        self.assertTrue(
            any("exact affirmative approval" in issue for issue in approval_issues),
            approval_issues,
        )
        self.assertTrue(
            any(
                "OI-18" in issue and "exact affirmative approval" in issue
                for issue in approval_issues
            ),
            approval_issues,
        )

    def test_rebound_decision_cannot_enable_any_forbidden_course(self) -> None:
        decision_path = self.active_root / "recapture-decision.json"
        subject_path = self.active_root / "approval-subject.json"
        original_decision = _read_json(decision_path)
        original_subject = _read_json(subject_path)
        original_identity = _read_json(self.identity_path)
        mutations = {
            "semanticCompatibilityExceptionApproved": True,
            "rollbackApproved": True,
            "submodulePointerChangedByDecision": True,
            "packageVersionChangedByDecision": True,
        }
        for field, value in mutations.items():
            with self.subTest(field=field):
                decision = copy.deepcopy(original_decision)
                decision[field] = value
                _write_json(decision_path, decision)
                identity = copy.deepcopy(original_identity)
                identity["decision"]["sha256"] = _sha256(decision_path)
                subject = copy.deepcopy(original_subject)
                subject["decision"]["sha256"] = identity["decision"]["sha256"]
                _write_json(subject_path, subject)
                identity["approval"]["subject"]["sha256"] = _sha256(subject_path)
                _write_json(self.identity_path, identity)

                errors, _, _ = self.validate_active()

                self.assertTrue(
                    any(f"decision {field} is incorrect" in error for error in errors),
                    errors,
                )

        _write_json(decision_path, original_decision)
        _write_json(subject_path, original_subject)
        _write_json(self.identity_path, original_identity)

    def test_rebound_decision_rejects_integer_zero_as_a_boolean(self) -> None:
        decision_path = self.active_root / "recapture-decision.json"
        decision = _read_json(decision_path)
        decision["rollbackApproved"] = 0
        _write_json(decision_path, decision)
        identity = _read_json(self.identity_path)
        identity["decision"]["sha256"] = _sha256(decision_path)
        subject_path = self.active_root / "approval-subject.json"
        subject = _read_json(subject_path)
        subject["decision"]["sha256"] = identity["decision"]["sha256"]
        _write_json(subject_path, subject)
        identity["approval"]["subject"]["sha256"] = _sha256(subject_path)
        _write_json(self.identity_path, identity)

        errors, _, _ = self.validate_active()

        self.assertTrue(any("decision rollbackApproved is incorrect" in error for error in errors), errors)

    def test_provider_and_apphost_capture_revisions_are_independently_enforced(self) -> None:
        for relative, mutate, expected in (
            (
                "run-evidence.json",
                lambda document: document.__setitem__("frontComposerRevision", "0" * 40),
                "Live provider run receipt frontComposerRevision",
            ),
            (
                "apphost-smoke.json",
                lambda document: document["identity"].__setitem__("frontComposerRevision", "0" * 40),
                "Live AppHost smoke provenance is stale or untruthful",
            ),
        ):
            with self.subTest(relative=relative):
                active_backup = Path(self._temporary.name) / f"backup-{relative}"
                shutil.copytree(self.active_root, active_backup)
                identity_backup = _read_json(self.identity_path)
                self.repin_active_recapture(relative, mutate)

                errors, _, _ = self.validate_active()

                self.assertTrue(any(expected in error for error in errors), errors)
                shutil.rmtree(self.active_root)
                shutil.copytree(active_backup, self.active_root)
                _write_json(self.identity_path, identity_backup)

    def test_oi18_rejects_each_missing_prerequisite_receipt(self) -> None:
        for field, role in (
            ("productApprovalReceipt", "product-owner"),
            ("architectureApprovalReceipt", "architect"),
        ):
            with self.subTest(role=role):
                shutil.rmtree(self.active_root)
                shutil.copytree(CANONICAL_ACTIVE_EVIDENCE, self.active_root)
                shutil.copyfile(CANONICAL_IDENTITY_V2, self.identity_path)
                self.claim_active_approval(transferred_eventstore_role=True)
                roster = _read_json(self.active_root / "reviewer-roster.json")
                roster["oi18"][field] = None
                self.repin_active_roster(roster)

                errors, approval_issues, claimed = self.validate_active()

                self.assertTrue(claimed)
                self.assertTrue(any(role in issue for issue in approval_issues), approval_issues)
                self.assertTrue(any("Migration approval claimed" in error for error in errors), errors)

    def test_oi18_rejects_forged_and_predated_prerequisites(self) -> None:
        for mutation, expected in (
            ("forged", "role is invalid"),
            ("predated", "does not postdate the transfer subject"),
        ):
            with self.subTest(mutation=mutation):
                shutil.rmtree(self.active_root)
                shutil.copytree(CANONICAL_ACTIVE_EVIDENCE, self.active_root)
                shutil.copyfile(CANONICAL_IDENTITY_V2, self.identity_path)
                self.claim_active_approval(transferred_eventstore_role=True)
                roster = _read_json(self.active_root / "reviewer-roster.json")
                binding = roster["oi18"]["productApprovalReceipt"]
                path = self.artifact_root / binding["path"]
                receipt = _read_json(path)
                if mutation == "forged":
                    receipt["role"] = "architect"
                else:
                    receipt["acceptedAt"] = "2026-09-12T09:58:24+00:00"
                _write_json(path, receipt)
                binding["sha256"] = _sha256(path)
                self.repin_active_roster(roster)

                errors, approval_issues, claimed = self.validate_active()

                self.assertTrue(claimed)
                self.assertTrue(any(expected in issue for issue in approval_issues), approval_issues)
                self.assertTrue(errors)

    def test_oi18_replacement_receipt_must_follow_both_prerequisites(self) -> None:
        self.claim_active_approval(transferred_eventstore_role=True)
        identity = _read_json(self.identity_path)
        binding = next(
            item for item in identity["approval"]["receipts"]
            if item["role"] == "accountable-frontcomposer-maintainer"
        )
        path = self.artifact_root / binding["path"]
        receipt = _read_json(path)
        receipt["acceptedAt"] = "2026-09-12T09:58:34+00:00"
        _write_json(path, receipt)
        binding["sha256"] = _sha256(path)
        _write_json(self.identity_path, identity)

        errors, approval_issues, claimed = self.validate_active()

        self.assertTrue(claimed)
        self.assertTrue(
            any("does not postdate both OI-18 prerequisite approvals" in issue for issue in approval_issues),
            approval_issues,
        )
        self.assertTrue(errors)

    def test_oi18_transfer_subject_must_follow_the_authority_policy(self) -> None:
        self.claim_active_approval(transferred_eventstore_role=True)
        roster = _read_json(self.active_root / "reviewer-roster.json")
        binding = roster["oi18"]["transferSubject"]
        path = self.artifact_root / binding["path"]
        subject = _read_json(path)
        subject["frozenAt"] = "2026-09-12T09:58:19+00:00"
        _write_json(path, subject)
        binding["sha256"] = _sha256(path)
        for receipt_field in ("productApprovalReceipt", "architectureApprovalReceipt"):
            receipt_binding = roster["oi18"][receipt_field]
            receipt_path = self.artifact_root / receipt_binding["path"]
            receipt = _read_json(receipt_path)
            receipt["subjectSha256"] = binding["sha256"]
            _write_json(receipt_path, receipt)
            receipt_binding["sha256"] = _sha256(receipt_path)
        self.repin_active_roster(roster)

        errors, approval_issues, claimed = self.validate_active()

        self.assertTrue(claimed)
        self.assertTrue(any("not frozen after the approval policy" in issue for issue in approval_issues), approval_issues)
        self.assertTrue(errors)

    def test_future_receipt_timestamp_is_rejected(self) -> None:
        self.claim_active_approval()
        identity = _read_json(self.identity_path)
        binding = identity["approval"]["receipts"][0]
        path = self.artifact_root / binding["path"]
        receipt = _read_json(path)
        receipt["acceptedAt"] = "2999-01-01T00:00:00+00:00"
        _write_json(path, receipt)
        binding["sha256"] = _sha256(path)
        _write_json(self.identity_path, identity)

        errors, approval_issues, claimed = self.validate_active()

        self.assertTrue(claimed)
        self.assertTrue(any("five-minute clock skew" in issue for issue in approval_issues), approval_issues)
        self.assertTrue(errors)

    def test_default_receipts_must_strictly_postdate_the_subject_freeze(self) -> None:
        subject_frozen_at = _read_json(
            CANONICAL_ACTIVE_EVIDENCE / "approval-subject.json"
        )["frozenAt"]
        for accepted_at in (
            subject_frozen_at,
            _offset_timestamp(subject_frozen_at, -1),
        ):
            with self.subTest(accepted_at=accepted_at):
                self.claim_active_approval()
                identity = _read_json(self.identity_path)
                binding = identity["approval"]["receipts"][0]
                path = self.artifact_root / binding["path"]
                receipt = _read_json(path)
                receipt["acceptedAt"] = accepted_at
                _write_json(path, receipt)
                binding["sha256"] = _sha256(path)
                _write_json(self.identity_path, identity)

                errors, approval_issues, claimed = self.validate_active()

                self.assertTrue(claimed)
                self.assertTrue(
                    any(
                        "Receipt does not postdate the frozen subject for role: eventstore-maintainer"
                        in issue
                        for issue in approval_issues
                    ),
                    approval_issues,
                )
                self.assertTrue(errors)

    def test_future_decision_and_capture_timestamps_are_rejected(self) -> None:
        decision_path = self.active_root / "recapture-decision.json"
        subject_path = self.active_root / "approval-subject.json"
        decision = _read_json(decision_path)
        decision["recordedAt"] = "2999-01-01T00:00:00+00:00"
        _write_json(decision_path, decision)
        identity = _read_json(self.identity_path)
        identity["decision"]["sha256"] = _sha256(decision_path)
        subject = _read_json(subject_path)
        subject["decision"]["sha256"] = identity["decision"]["sha256"]
        _write_json(subject_path, subject)
        identity["approval"]["subject"]["sha256"] = _sha256(subject_path)
        _write_json(self.identity_path, identity)

        errors, _, _ = self.validate_active()

        self.assertTrue(any("decision recordedAt is later" in error for error in errors), errors)

        shutil.rmtree(self.active_root)
        shutil.copytree(CANONICAL_ACTIVE_EVIDENCE, self.active_root)
        shutil.copyfile(CANONICAL_IDENTITY_V2, self.identity_path)
        self.repin_active_recapture(
            "apphost-smoke.json",
            lambda document: document.__setitem__(
                "capturedAt",
                "2999-01-01T00:00:00+00:00",
            ),
        )

        errors, _, _ = self.validate_active()

        self.assertTrue(any("AppHost capture start timestamp is later" in error for error in errors), errors)

    def test_ordinary_capture_chronology_is_enforced(self) -> None:
        self.repin_active_recapture(
            "apphost-smoke.json",
            lambda document: document.__setitem__(
                "completedAt", "2026-09-12T11:29:00+00:00"
            ),
        )
        errors, _, _ = self.validate_active()
        self.assertTrue(any("completion does not follow" in error for error in errors), errors)

    def test_all_provider_and_apphost_completions_must_strictly_predate_decision(self) -> None:
        decision_recorded_at = _read_json(
            CANONICAL_ACTIVE_EVIDENCE / "recapture-decision.json"
        )["recordedAt"]
        cases = (
            (
                "provider-verification.json",
                lambda document, value: document["timing"]["run"].__setitem__("completedAt", value),
                "provider run completion",
            ),
            (
                "run-evidence.json",
                lambda document, value: document.__setitem__("capturedAt", value),
                "provider receipt capture",
            ),
            (
                "apphost-smoke.json",
                lambda document, value: document.__setitem__("completedAt", value),
                "AppHost capture completion",
            ),
        )
        for relative, mutate, label in cases:
            for timestamp in (
                decision_recorded_at,
                _offset_timestamp(decision_recorded_at, 1),
            ):
                with self.subTest(relative=relative, timestamp=timestamp):
                    shutil.rmtree(self.active_root)
                    shutil.copytree(CANONICAL_ACTIVE_EVIDENCE, self.active_root)
                    shutil.copyfile(CANONICAL_IDENTITY_V2, self.identity_path)
                    self.repin_active_recapture(
                        relative,
                        lambda document, value=timestamp, mutate=mutate: mutate(document, value),
                    )

                    errors, _, _ = self.validate_active()

                    self.assertIn(
                        f"Active {label} does not predate the recapture decision.",
                        errors,
                    )

        shutil.rmtree(self.active_root)
        shutil.copytree(CANONICAL_ACTIVE_EVIDENCE, self.active_root)
        shutil.copyfile(CANONICAL_IDENTITY_V2, self.identity_path)
        self.repin_active_recapture(
            "apphost-smoke.json",
            lambda document: document.__setitem__(
                "completedAt", "2026-09-12T13:15:41+00:00"
            ),
        )
        errors, _, _ = self.validate_active()
        self.assertTrue(any("completion does not follow" in error for error in errors), errors)

    def test_runtime_manifest_must_predate_provider_and_apphost_execution(self) -> None:
        cases = (
            (
                "provider-verification.json",
                lambda document: document["timing"]["run"]["startedAt"],
                "manifest does not predate provider",
            ),
            (
                "apphost-smoke.json",
                lambda document: document["capturedAt"],
                "manifest does not predate AppHost",
            ),
        )
        for capture_name, timestamp, expected in cases:
            with self.subTest(capture_name=capture_name):
                shutil.rmtree(self.active_root)
                shutil.copytree(CANONICAL_ACTIVE_EVIDENCE, self.active_root)
                shutil.copyfile(CANONICAL_IDENTITY_V2, self.identity_path)
                manifest_path = self.active_root / "frontcomposer-runtime-inputs.json"
                manifest = _read_json(manifest_path)
                capture = _read_json(self.active_root / "recapture" / capture_name)
                manifest["capturedAt"] = timestamp(capture)
                _write_json(manifest_path, manifest)
                manifest_hash = _sha256(manifest_path)
                identity = _read_json(self.identity_path)
                identity["runtimeInputs"]["sha256"] = manifest_hash
                decision_path = self.active_root / "recapture-decision.json"
                decision = _read_json(decision_path)
                decision["runtimeInputs"]["sha256"] = manifest_hash
                _write_json(decision_path, decision)
                identity["decision"]["sha256"] = _sha256(decision_path)
                subject_path = self.active_root / "approval-subject.json"
                subject = _read_json(subject_path)
                subject["runtimeInputs"]["sha256"] = manifest_hash
                subject["decision"]["sha256"] = identity["decision"]["sha256"]
                _write_json(subject_path, subject)
                identity["approval"]["subject"]["sha256"] = _sha256(subject_path)
                _write_json(self.identity_path, identity)

                errors, _, _ = self.validate_active()

                self.assertTrue(any(expected in error for error in errors), errors)

    def test_new_apphost_package_ledger_must_predate_execution_boundary(self) -> None:
        boundary = _read_json(
            self.active_root / "recapture" / "apphost-smoke.json"
        )["executionStartedAt"]
        self.repin_active_package_ledger(
            "apphost-smoke.json",
            evidence.APPHOST_PACKAGE_LEDGER_FILE,
            lambda ledger: ledger.__setitem__("capturedAt", boundary),
        )

        errors, _, _ = self.validate_active()

        self.assertIn(
            "Live AppHost chronology must be runtime manifest, package ledger, execution start, completion.",
            errors,
        )

    def test_final_subject_must_postdate_decision_policy_and_roster(self) -> None:
        cases = (
            ("recapture-decision.json", "recordedAt", "recapture decision"),
            ("approval-policy.json", "frozenAt", "approval policy"),
            ("reviewer-roster.json", "frozenAt", "approval roster"),
        )
        for source_name, timestamp_field, label in cases:
            with self.subTest(label=label):
                shutil.rmtree(self.active_root)
                shutil.copytree(CANONICAL_ACTIVE_EVIDENCE, self.active_root)
                shutil.copyfile(CANONICAL_IDENTITY_V2, self.identity_path)
                source = _read_json(self.active_root / source_name)
                subject_path = self.active_root / "approval-subject.json"
                subject = _read_json(subject_path)
                subject["frozenAt"] = source[timestamp_field]
                _write_json(subject_path, subject)
                identity = _read_json(self.identity_path)
                identity["approval"]["subject"]["sha256"] = _sha256(subject_path)
                _write_json(self.identity_path, identity)

                errors, _, _ = self.validate_active()

                self.assertIn(
                    f"Active approval subject was not frozen after the {label}.",
                    errors,
                )

    def test_default_effective_roles_require_distinct_actors(self) -> None:
        self.claim_active_approval()
        roster = _read_json(self.active_root / "reviewer-roster.json")
        eventstore_actor = next(
            item for item in roster["assignments"]
            if item["role"] == "eventstore-maintainer"
        )["actors"][0]
        next(
            item for item in roster["assignments"]
            if item["role"] == "frontcomposer-maintainer"
        )["actors"] = [eventstore_actor]
        self.repin_active_roster(roster)

        errors, approval_issues, claimed = self.validate_active()

        self.assertTrue(claimed)
        self.assertTrue(any("Required roles must have distinct actors" in issue for issue in approval_issues), approval_issues)
        self.assertTrue(errors)

    def test_policy_rejects_malformed_actors_and_non_https_sources(self) -> None:
        policy = _read_json(self.active_root / "approval-policy.json")
        assignment = policy["assignments"][0]
        assignment["authorities"] = [{
            "actor": "github:",
            "durableSources": ["https://example.test/receipt"],
        }]
        errors: list[str] = []
        evidence._validate_policy(policy, errors)
        self.assertTrue(any("actor/source authority is malformed" in error for error in errors), errors)

        assignment["authorities"] = [{
            "actor": "github:valid-actor",
            "durableSources": ["http://example.test/receipt"],
        }]
        errors = []
        evidence._validate_policy(policy, errors)
        self.assertTrue(any("actor/source authority is malformed" in error for error in errors), errors)

    def test_repository_policy_cannot_manufacture_validator_authority(self) -> None:
        policy = _read_json(self.active_root / "approval-policy.json")
        policy["assignments"][0]["authorities"] = [{
            "actor": "github:self-appointed",
            "durableSources": ["https://example.test/self-appointed"],
        }]
        errors: list[str] = []

        authority, _ = evidence._validate_policy(policy, errors)

        self.assertTrue(any("validator-owned authority bootstrap" in error for error in errors), errors)
        self.assertEqual(authority, {role: {} for role in evidence.POLICY_ROLES})

    def test_malformed_unhashable_roster_role_is_a_validation_issue(self) -> None:
        roster = _read_json(self.active_root / "reviewer-roster.json")
        roster["assignments"][0]["role"] = {"malformed": True}
        errors: list[str] = []

        actors, _ = evidence._validate_roster(
            roster,
            roster["policySha256"],
            {role: {} for role in evidence.POLICY_ROLES},
            errors,
        )

        self.assertIsInstance(actors, dict)
        self.assertTrue(any("duplicate or unexpected role" in error for error in errors), errors)

    def test_runtime_scope_rejects_untracked_worktree_index_symlink_and_dependency_drift(self) -> None:
        repository = Path(self._temporary.name) / "runtime-repository"
        repository.mkdir()

        def git(*arguments: str) -> None:
            subprocess.run(
                ["git", *arguments],
                cwd=repository,
                check=True,
                capture_output=True,
            )

        git("init", "-q")
        git("config", "user.name", "Runtime Evidence Test")
        git("config", "user.email", "runtime-evidence@example.test")
        for relative in (
            *evidence.RUNTIME_ROOT_INPUTS,
            *evidence.RUNTIME_PACT_INPUTS,
            "src/app.cs",
            "samples/Counter/Program.cs",
        ):
            path = repository / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("runtime input\n", encoding="utf-8")
        git("add", ".")
        git("commit", "-qm", "test: seed runtime scope")

        dependency_files: dict[str, Path] = {}
        for relative in evidence.RUNTIME_DEPENDENCY_GITLINKS:
            checkout = repository / relative
            checkout.mkdir(parents=True)
            dependency_file = checkout / "runtime.txt"
            subprocess.run(["git", "init", "-q"], cwd=checkout, check=True)
            subprocess.run(
                ["git", "config", "user.name", "Runtime Dependency Test"],
                cwd=checkout,
                check=True,
            )
            subprocess.run(
                ["git", "config", "user.email", "runtime-dependency@example.test"],
                cwd=checkout,
                check=True,
            )
            dependency_file.write_text("runtime dependency\n", encoding="utf-8")
            subprocess.run(["git", "add", "runtime.txt"], cwd=checkout, check=True)
            subprocess.run(
                ["git", "commit", "-qm", "test: seed runtime dependency"],
                cwd=checkout,
                check=True,
            )
            dependency_sha = subprocess.run(
                ["git", "rev-parse", "HEAD"],
                cwd=checkout,
                check=True,
                capture_output=True,
                text=True,
            ).stdout.strip()
            git(
                "update-index",
                "--add",
                "--cacheinfo",
                f"160000,{dependency_sha},{relative}",
            )
            dependency_files[relative] = dependency_file
        git("commit", "-qm", "test: add runtime dependency gitlinks")

        _, baseline_issues = evidence._runtime_input_snapshot(repository)
        self.assertEqual(baseline_issues, [])

        manifest_output = repository / "runtime-input-manifest.json"
        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            writer_result = evidence.main(
                [
                    "--write-runtime-input-manifest",
                    "--runtime-input-manifest-output", str(manifest_output),
                    "--runtime-input-captured-at", "2026-09-12T10:00:00+00:00",
                    "--repository-root", str(repository),
                    "--pact-dir", str(self.pact_root),
                ]
            )
        self.assertEqual(writer_result, 0)
        first_manifest = manifest_output.read_bytes()
        self.assertTrue(first_manifest.endswith(b"\n"))

        # The prior predictable PID-based temporary name was vulnerable to a
        # pre-created symlink. The writer must use an unpredictable exclusively
        # created sibling and leave that trap and its target untouched.
        symlink_target = repository / "writer-symlink-target.txt"
        symlink_target.write_text("must remain unchanged\n", encoding="utf-8")
        predictable_temporary = manifest_output.with_name(
            f".{manifest_output.name}.{os.getpid()}.tmp"
        )
        predictable_temporary.symlink_to(symlink_target)
        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            writer_result = evidence.main(
                [
                    "--write-runtime-input-manifest",
                    "--runtime-input-manifest-output", str(manifest_output),
                    "--runtime-input-captured-at", "2026-09-12T10:00:30+00:00",
                    "--repository-root", str(repository),
                    "--pact-dir", str(self.pact_root),
                ]
            )
        self.assertEqual(writer_result, 0)
        self.assertEqual(
            symlink_target.read_text(encoding="utf-8"),
            "must remain unchanged\n",
        )
        self.assertTrue(predictable_temporary.is_symlink())
        first_manifest = manifest_output.read_bytes()

        hidden_dependency = evidence.RUNTIME_DEPENDENCY_GITLINKS[2]
        hidden_checkout = repository / hidden_dependency
        subprocess.run(
            ["git", "update-index", "--assume-unchanged", "runtime.txt"],
            cwd=hidden_checkout,
            check=True,
        )
        dependency_files[hidden_dependency].write_text(
            "assume-hidden dependency\n", encoding="utf-8"
        )
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertTrue(
            any("dependency contains assume-unchanged entries" in issue for issue in issues),
            issues,
        )
        self.assertTrue(
            any("dependency worktree bytes differ from the Git index" in issue for issue in issues),
            issues,
        )
        subprocess.run(
            ["git", "update-index", "--no-assume-unchanged", "runtime.txt"],
            cwd=hidden_checkout,
            check=True,
        )
        dependency_files[hidden_dependency].write_text(
            "runtime dependency\n", encoding="utf-8"
        )

        skip_dependency = evidence.RUNTIME_DEPENDENCY_GITLINKS[3]
        skip_checkout = repository / skip_dependency
        subprocess.run(
            ["git", "update-index", "--skip-worktree", "runtime.txt"],
            cwd=skip_checkout,
            check=True,
        )
        dependency_files[skip_dependency].write_text(
            "skip-hidden dependency\n", encoding="utf-8"
        )
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertTrue(
            any("dependency contains skip-worktree entries" in issue for issue in issues),
            issues,
        )
        self.assertTrue(
            any("dependency worktree bytes differ from the Git index" in issue for issue in issues),
            issues,
        )
        subprocess.run(
            ["git", "update-index", "--no-skip-worktree", "runtime.txt"],
            cwd=skip_checkout,
            check=True,
        )
        dependency_files[skip_dependency].write_text(
            "runtime dependency\n", encoding="utf-8"
        )

        untracked_dependency = evidence.RUNTIME_DEPENDENCY_GITLINKS[4]
        untracked_input = repository / untracked_dependency / "untracked.props"
        untracked_input.write_text("runtime dependency input\n", encoding="utf-8")
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertTrue(
            any("dependency contains untracked inputs" in issue for issue in issues),
            issues,
        )
        untracked_input.unlink()

        symlinked_dependency = evidence.RUNTIME_DEPENDENCY_GITLINKS[5]
        symlinked_checkout = repository / symlinked_dependency
        real_checkout = symlinked_checkout.with_name(f"{symlinked_checkout.name}-real")
        symlinked_checkout.rename(real_checkout)
        symlinked_checkout.symlink_to(real_checkout, target_is_directory=True)
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertIn(
            f"Runtime dependency checkout is missing or symlinked: {symlinked_dependency}",
            issues,
        )
        symlinked_checkout.unlink()
        real_checkout.rename(symlinked_checkout)

        nested_dependency = evidence.RUNTIME_DEPENDENCY_GITLINKS[6]
        nested_checkout = repository / nested_dependency
        nested_source = repository / "nested-source"
        nested_source.mkdir()
        subprocess.run(["git", "init", "-q"], cwd=nested_source, check=True)
        subprocess.run(
            ["git", "config", "user.name", "Nested Dependency Test"],
            cwd=nested_source,
            check=True,
        )
        subprocess.run(
            ["git", "config", "user.email", "nested-dependency@example.test"],
            cwd=nested_source,
            check=True,
        )
        (nested_source / "nested.txt").write_text("nested\n", encoding="utf-8")
        subprocess.run(["git", "add", "nested.txt"], cwd=nested_source, check=True)
        subprocess.run(
            ["git", "commit", "-qm", "test: seed nested dependency"],
            cwd=nested_source,
            check=True,
        )
        subprocess.run(
            [
                "git", "-c", "protocol.file.allow=always", "submodule", "add", "-q",
                str(nested_source), "nested",
            ],
            cwd=nested_checkout,
            check=True,
        )
        subprocess.run(
            ["git", "commit", "-qm", "test: initialize nested dependency"],
            cwd=nested_checkout,
            check=True,
        )
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertTrue(
            any("initialized nested submodules" in issue for issue in issues),
            issues,
        )

        dirty_dependency = evidence.RUNTIME_DEPENDENCY_GITLINKS[0]
        dependency_files[dirty_dependency].write_text(
            "dirty dependency\n", encoding="utf-8"
        )
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertIn(f"Runtime dependency checkout is dirty: {dirty_dependency}", issues)
        dependency_files[dirty_dependency].write_text(
            "runtime dependency\n", encoding="utf-8"
        )

        advanced_dependency = evidence.RUNTIME_DEPENDENCY_GITLINKS[1]
        advanced_checkout = repository / advanced_dependency
        dependency_files[advanced_dependency].write_text(
            "advanced dependency\n", encoding="utf-8"
        )
        subprocess.run(["git", "add", "runtime.txt"], cwd=advanced_checkout, check=True)
        subprocess.run(
            ["git", "commit", "-qm", "test: advance runtime dependency"],
            cwd=advanced_checkout,
            check=True,
        )
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertIn(
            f"Runtime dependency gitlink/check-out drifted: {advanced_dependency}",
            issues,
        )

        untracked = repository / "src/untracked.cs"
        untracked.write_text("untracked\n", encoding="utf-8")
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertTrue(any("contains untracked files" in issue for issue in issues), issues)
        untracked.unlink()

        untracked_sample = repository / "samples/Counter/untracked.cs"
        untracked_sample.write_text("untracked sample\n", encoding="utf-8")
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertTrue(any("contains untracked files" in issue for issue in issues), issues)
        untracked_sample.unlink()

        tracked = repository / "src/app.cs"
        tracked.write_text("dirty\n", encoding="utf-8")
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertTrue(any("worktree bytes differ from the Git index" in issue for issue in issues), issues)
        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            writer_result = evidence.main(
                [
                    "--write-runtime-input-manifest",
                    "--runtime-input-manifest-output", str(manifest_output),
                    "--runtime-input-captured-at", "2026-09-12T10:01:00+00:00",
                    "--repository-root", str(repository),
                    "--pact-dir", str(self.pact_root),
                ]
            )
        self.assertEqual(writer_result, 1)
        self.assertEqual(manifest_output.read_bytes(), first_manifest)

        tracked.write_text("runtime input\n", encoding="utf-8")
        git("update-index", "--assume-unchanged", "src/app.cs")
        tracked.write_text("assume-hidden\n", encoding="utf-8")
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertTrue(any("assume-unchanged" in issue for issue in issues), issues)
        self.assertTrue(any("worktree bytes differ from the Git index" in issue for issue in issues), issues)
        git("update-index", "--no-assume-unchanged", "src/app.cs")
        tracked.write_text("runtime input\n", encoding="utf-8")

        git("update-index", "--skip-worktree", "src/app.cs")
        tracked.write_text("skip-hidden\n", encoding="utf-8")
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertTrue(any("skip-worktree" in issue for issue in issues), issues)
        self.assertTrue(any("worktree bytes differ from the Git index" in issue for issue in issues), issues)
        git("update-index", "--no-skip-worktree", "src/app.cs")
        tracked.write_text("runtime input\n", encoding="utf-8")

        ignored_control = repository / "untracked-build.rsp"
        (repository / ".gitignore").write_text("*.rsp\n", encoding="utf-8")
        ignored_control.write_text("-p:Injected=true\n", encoding="utf-8")
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertTrue(any("outside the fixed scope: untracked-build.rsp" in issue for issue in issues), issues)
        ignored_control.unlink()

        tracked.write_text("dirty\n", encoding="utf-8")
        git("add", "src/app.cs")
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertTrue(any("index differs from HEAD" in issue for issue in issues), issues)
        git("commit", "-qm", "test: stage deterministic input")

        os.symlink("app.cs", repository / "src/runtime-link.cs")
        git("add", "src/runtime-link.cs")
        git("commit", "-qm", "test: add runtime symlink")
        _, issues = evidence._runtime_input_snapshot(repository)
        self.assertTrue(any("Runtime-input path is symlinked" in issue for issue in issues), issues)

        tracked_symlink_dependency = evidence.RUNTIME_DEPENDENCY_GITLINKS[0]
        tracked_symlink_checkout = repository / tracked_symlink_dependency
        os.symlink("runtime.txt", tracked_symlink_checkout / "tracked-link")
        subprocess.run(
            ["git", "add", "tracked-link"],
            cwd=tracked_symlink_checkout,
            check=True,
        )
        subprocess.run(
            ["git", "commit", "-qm", "test: add tracked dependency symlink"],
            cwd=tracked_symlink_checkout,
            check=True,
        )
        tracked_symlink_sha = subprocess.run(
            ["git", "rev-parse", "HEAD"],
            cwd=tracked_symlink_checkout,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.strip()
        issues = evidence._validate_dependency_checkout(
            repository,
            tracked_symlink_dependency,
            tracked_symlink_sha,
        )
        self.assertIn(
            f"Runtime dependency contains a tracked symlink selected by the build/runtime input scope: {tracked_symlink_dependency}/tracked-link",
            issues,
        )

        subprocess.run(
            ["git", "rm", "-q", "tracked-link"],
            cwd=tracked_symlink_checkout,
            check=True,
        )
        unapproved_documentation = tracked_symlink_checkout / "docs" / "runtime-notes.md"
        unapproved_documentation.parent.mkdir()
        unapproved_documentation.symlink_to("../../architecture.md")
        unapproved_tooling = tracked_symlink_checkout / ".clinerules"
        unapproved_tooling.symlink_to("references/Hexalith.Builds/.clinerules")
        subprocess.run(
            ["git", "add", "docs/runtime-notes.md", ".clinerules"],
            cwd=tracked_symlink_checkout,
            check=True,
        )
        subprocess.run(
            ["git", "commit", "-qm", "test: add unapproved dependency symlinks"],
            cwd=tracked_symlink_checkout,
            check=True,
        )
        unapproved_symlink_sha = subprocess.run(
            ["git", "rev-parse", "HEAD"],
            cwd=tracked_symlink_checkout,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.strip()
        issues = evidence._validate_dependency_checkout(
            repository,
            tracked_symlink_dependency,
            unapproved_symlink_sha,
        )
        self.assertTrue(
            any("docs/runtime-notes.md" in issue for issue in issues),
            issues,
        )
        self.assertTrue(any(".clinerules" in issue for issue in issues), issues)

        selected_untracked_symlink = tracked_symlink_checkout / "runtime-link.props"
        selected_untracked_symlink.symlink_to("runtime.txt")
        issues = evidence._validate_dependency_checkout(
            repository,
            tracked_symlink_dependency,
            unapproved_symlink_sha,
        )
        self.assertIn(
            "Runtime dependency contains untracked symlink inputs selected by the "
            f"build/runtime input scope: {tracked_symlink_dependency}: runtime-link.props",
            issues,
        )

        eventstore_dependency = "references/Hexalith.EventStore"
        eventstore_checkout = repository / eventstore_dependency
        eventstore_link = (
            eventstore_checkout
            / "_bmad-output/planning-artifacts/architecture/"
            "architecture-eventstore-2026-07-05/ARCHITECTURE-SPINE.md"
        )
        eventstore_link.parent.mkdir(parents=True)
        eventstore_link.symlink_to("../../architecture.md")
        subprocess.run(
            ["git", "add", eventstore_link.relative_to(eventstore_checkout).as_posix()],
            cwd=eventstore_checkout,
            check=True,
        )
        subprocess.run(
            ["git", "commit", "-qm", "test: add approved EventStore documentation link"],
            cwd=eventstore_checkout,
            check=True,
        )
        eventstore_sha = subprocess.run(
            ["git", "rev-parse", "HEAD"],
            cwd=eventstore_checkout,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.strip()
        self.assertEqual(
            evidence._validate_dependency_checkout(
                repository,
                eventstore_dependency,
                eventstore_sha,
            ),
            [],
        )

        commons_dependency = "references/Hexalith.Commons"
        commons_checkout = repository / commons_dependency
        commons_targets = {
            ".clinerules": "references/Hexalith.Builds/.clinerules",
            ".cursorrules": "references/Hexalith.Builds/.cursorrules",
        }
        for link_name, target in commons_targets.items():
            (commons_checkout / link_name).symlink_to(target)
        subprocess.run(
            ["git", "add", *commons_targets],
            cwd=commons_checkout,
            check=True,
        )
        subprocess.run(
            ["git", "commit", "-qm", "test: add approved Commons tooling links"],
            cwd=commons_checkout,
            check=True,
        )
        commons_sha = subprocess.run(
            ["git", "rev-parse", "HEAD"],
            cwd=commons_checkout,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.strip()
        self.assertEqual(
            evidence._validate_dependency_checkout(
                repository,
                commons_dependency,
                commons_sha,
            ),
            [],
        )

        (commons_checkout / ".clinerules").unlink()
        (commons_checkout / ".clinerules").symlink_to("changed-target")
        issues = evidence._validate_dependency_checkout(
            repository,
            commons_dependency,
            commons_sha,
        )
        self.assertTrue(
            any("worktree symlink differs from the Git index" in issue for issue in issues),
            issues,
        )

        subprocess.run(
            ["git", "add", ".clinerules"],
            cwd=commons_checkout,
            check=True,
        )
        issues = evidence._validate_dependency_checkout(
            repository,
            commons_dependency,
            commons_sha,
        )
        self.assertTrue(any("index differs from HEAD" in issue for issue in issues), issues)
        self.assertTrue(
            any("selected by the build/runtime input scope" in issue for issue in issues),
            issues,
        )

    def test_prior_archive_rejects_nested_extra_content(self) -> None:
        nested = self.history_root / "nested"
        nested.mkdir()
        (nested / "extra.json").write_text("{}\n", encoding="utf-8")

        errors, _, _ = self.validate_active()

        self.assertTrue(any("exactly the dated three-file packet" in error for error in errors), errors)

    def test_prior_archive_and_active_tree_reject_empty_directories(self) -> None:
        (self.history_root / "empty").mkdir()
        errors, _, _ = self.validate_active()
        self.assertTrue(any("undeclared directory" in error for error in errors), errors)

        (self.history_root / "empty").rmdir()
        (self.active_root / "empty").mkdir()
        errors, _, _ = self.validate_active()
        self.assertTrue(any("undeclared directories" in error for error in errors), errors)

    def test_runtime_manifest_names_counter_and_all_source_dependency_gitlinks(self) -> None:
        manifest = _read_json(self.active_root / "frontcomposer-runtime-inputs.json")
        self.assertEqual(manifest["scope"], evidence._runtime_scope())
        paths = {item["path"] for item in manifest["entries"]}
        self.assertIn(".editorconfig", paths)
        self.assertTrue(any(path.startswith("samples/Counter/") for path in paths))
        self.assertTrue(set(evidence.RUNTIME_DEPENDENCY_GITLINKS).issubset(paths))
        directory_rsp = next(item for item in manifest["entries"] if item["path"] == "Directory.Build.rsp")
        self.assertEqual(directory_rsp, {"path": "Directory.Build.rsp", "kind": "absent"})

    def validate_live_with_manifest(self) -> list[str]:
        """Exercise the bound live lane: the sealed manifest, not the artifact, is authority."""
        return evidence.validate_live(
            self.live_root,
            self.pact_root,
            ROOT,
            provider_package_root=self.package_root,
            apphost_package_root=self.package_root,
            runtime_input_manifest_path=(
                self.active_root / "frontcomposer-runtime-inputs.json"
            ),
        )

    def test_live_lane_with_the_sealed_manifest_accepts_the_bound_capture(self) -> None:
        self.make_live_apphost_pass()

        # The sealed manifest is compared with the real repository, which has advanced past
        # its capture revision; that separate finding is expected here. What this asserts is
        # that supplying the manifest raises no live provider or AppHost binding error.
        errors = [
            error
            for error in self.validate_live_with_manifest()
            if error.startswith("Live ")
        ]

        self.assertEqual(errors, [])

    def test_live_lane_with_the_sealed_manifest_rejects_a_backdated_capture(self) -> None:
        self.make_live_apphost_pass()
        smoke_path = self.live_root / "apphost-smoke.json"
        document = _read_json(smoke_path)
        document["identity"]["runtimeInputCapturedAt"] = _offset_timestamp(
            document["identity"]["runtimeInputCapturedAt"], -60
        )
        _write_json(smoke_path, document)

        errors = self.validate_live_with_manifest()

        self.assertIn("Live AppHost smoke provenance is stale or untruthful.", errors)

    def test_live_lane_with_the_sealed_manifest_rejects_an_unbound_evaluated_input(self) -> None:
        self.make_live_apphost_pass()
        smoke_path = self.live_root / "apphost-smoke.json"
        document = _read_json(smoke_path)
        document["startup"]["outputPreparation"]["evaluatedInputBinding"]["inputs"] = [
            {"authority": "repository", "path": "global.json", "sha256": "c" * 64}
        ]
        _write_json(smoke_path, document)

        errors = self.validate_live_with_manifest()

        self.assertTrue(
            any(
                "differs from the sealed runtime manifest" in error for error in errors
            ),
            errors,
        )

    def test_live_provider_requires_manifest_then_package_then_execution(self) -> None:
        self.make_live_apphost_pass()
        self.mutate_live_package_ledger(
            "run-evidence.json",
            evidence.PROVIDER_PACKAGE_LEDGER_FILE,
            lambda ledger: ledger.__setitem__(
                "capturedAt", "2026-09-12T08:00:00+00:00"
            ),
        )

        errors = self.validate_live()

        self.assertIn(
            "Live provider chronology must be runtime manifest, package ledger, then execution start.",
            errors,
        )

    def test_live_validation_requires_the_sealed_manifest(self) -> None:
        errors = evidence.validate_live(
            self.live_root,
            self.pact_root,
            ROOT,
            provider_package_root=self.package_root,
            apphost_package_root=self.package_root,
        )

        self.assertIn(
            "Live validation requires the sealed runtime-input manifest.", errors
        )

    def test_live_validation_rejects_detached_evidence_and_pact_roots(self) -> None:
        with mock.patch.object(
            evidence,
            "_canonical_live_locations",
            side_effect=REAL_CANONICAL_LIVE_LOCATIONS,
        ):
            errors = evidence.validate_live(
                self.live_root,
                self.pact_root,
                ROOT,
                provider_package_root=self.package_root,
                apphost_package_root=self.package_root,
                runtime_input_manifest_path=(
                    self.active_root / "frontcomposer-runtime-inputs.json"
                ),
            )

        self.assertIn(
            "Live validation requires the canonical repository evidence root.", errors
        )
        self.assertIn(
            "Live validation requires the canonical repository Pact directory.", errors
        )

    def test_final_gate_recomputes_evaluated_inputs_and_runtime_outputs(self) -> None:
        self.make_live_apphost_pass()
        with (
            mock.patch.object(
                evidence,
                "_evaluate_apphost_inputs",
                return_value={"assetsGraphs": [], "inputs": []},
            ),
            mock.patch.object(
                evidence,
                "_apphost_runtime_output_binding",
                return_value=[],
            ),
        ):
            errors = self.validate_live()

        self.assertIn(
            "Live AppHost evaluated input binding differs from final Gate 2c recomputation.",
            errors,
        )
        self.assertIn(
            "Live AppHost runtime output binding differs from final Gate 2c recomputation.",
            errors,
        )

    def test_live_apphost_capture_start_must_not_follow_execution_start(self) -> None:
        self.make_live_apphost_pass()
        smoke_path = self.live_root / "apphost-smoke.json"
        smoke = _read_json(smoke_path)
        smoke["capturedAt"] = _offset_timestamp(smoke["executionStartedAt"], 1)
        _write_json(smoke_path, smoke)

        errors = self.validate_live()

        self.assertIn(
            "Live AppHost capture start is later than execution start.", errors
        )

    def test_receipt_writer_rejects_completion_after_its_capture_time(self) -> None:
        report_path = self.live_root / "provider-verification.json"
        report = _read_json(report_path)
        started = datetime.now().astimezone() + timedelta(seconds=30)
        completed = started + timedelta(seconds=30)
        report["timing"]["run"]["startedAt"] = started.isoformat()
        report["timing"]["run"]["completedAt"] = completed.isoformat()
        _write_json(report_path, report)
        receipt_path = self.live_root / "run-evidence.json"
        receipt_before = receipt_path.read_bytes()

        with mock.patch.object(evidence, "_validate_live_provider", return_value=None):
            errors = self.write_live_receipt()

        self.assertIn(
            "Live provider report completion is later than the receipt capture time.",
            errors,
        )
        self.assertEqual(receipt_path.read_bytes(), receipt_before)

    def test_pact_interaction_identity_rejects_null_strings(self) -> None:
        pact_path = self.pact_root / evidence.PACT_FILES[0]
        pact = _read_json(pact_path)
        pact["interactions"][0]["description"] = None
        _write_json(pact_path, pact)

        errors: list[str] = []
        evidence._pact_interactions(self.pact_root, errors)

        self.assertTrue(
            any("empty identity field" in error for error in errors), errors
        )

    def test_package_ledger_sidecar_binding_must_bind_its_exact_bytes(self) -> None:
        self.make_live_apphost_pass()
        for owner, name in (
            ("run-evidence.json", evidence.PROVIDER_PACKAGE_LEDGER_FILE),
            ("apphost-smoke.json", evidence.APPHOST_PACKAGE_LEDGER_FILE),
        ):
            with self.subTest(sidecar=name):
                sidecar = self.live_root / name
                original = sidecar.read_bytes()
                sidecar.write_bytes(original.replace(b"\n", b"\n ", 1))

                errors = self.validate_live()

                sidecar.write_bytes(original)
                self.assertTrue(
                    any(
                        "package-ledger binding does not bind the sidecar bytes" in error
                        for error in errors
                    ),
                    errors,
                )

    def test_package_ledger_sidecar_fields_must_match_the_sidecar_document(self) -> None:
        self.make_live_apphost_pass()
        smoke_path = self.live_root / "apphost-smoke.json"
        document = _read_json(smoke_path)
        document["packageLedger"]["treeSha256"] = "0" * 64
        _write_json(smoke_path, document)

        errors = self.validate_live()

        self.assertTrue(
            any(
                "package-ledger binding does not bind the sidecar bytes" in error
                or "package-ledger binding treeSha256 differs from the sidecar" in error
                for error in errors
            ),
            errors,
        )

    def test_missing_package_ledger_sidecar_fails_closed(self) -> None:
        self.make_live_apphost_pass()
        (self.live_root / evidence.APPHOST_PACKAGE_LEDGER_FILE).unlink()

        errors = self.validate_live()

        self.assertTrue(
            any("both package-ledger sidecars" in error for error in errors), errors
        )
        self.assertTrue(
            any(
                evidence.APPHOST_PACKAGE_LEDGER_FILE in error
                and "missing or unreadable" in error
                for error in errors
            ),
            errors,
        )

    def test_active_apphost_rejects_a_backdated_runtime_manifest_capture(self) -> None:
        self.repin_active_recapture(
            "apphost-smoke.json",
            lambda document: document["identity"].__setitem__(
                "runtimeInputCapturedAt",
                _offset_timestamp(document["identity"]["runtimeInputCapturedAt"], -60),
            ),
        )

        errors, _, _ = self.validate_active()

        self.assertIn("Live AppHost smoke provenance is stale or untruthful.", errors)

    def test_active_apphost_rejects_an_unbound_evaluated_repository_input(self) -> None:
        sealed = _manifest_entry_sha256("global.json")
        cases = (
            (
                [{"authority": "repository", "path": "global.json", "sha256": "b" * 64}],
                "differs from the sealed runtime manifest",
            ),
            (
                [
                    {
                        "authority": "repository",
                        "path": "eng/unsealed-input.props",
                        "sha256": sealed,
                    }
                ],
                "leaves the sealed runtime scope",
            ),
            (
                [
                    {
                        "authority": "packages",
                        "path": "synthetic.package/1.0.0/lib/net10.0/Synthetic.Package.dll",
                        "sha256": sealed,
                    }
                ],
                "names no repository-authority input",
            ),
        )
        for inputs, expected in cases:
            with self.subTest(expected=expected):
                self.repin_active_recapture(
                    "apphost-smoke.json",
                    lambda document, replacement=inputs: document["startup"][
                        "outputPreparation"
                    ]["evaluatedInputBinding"].__setitem__("inputs", replacement),
                )

                errors, _, _ = self.validate_active()

                self.assertTrue(any(expected in error for error in errors), errors)

    def test_active_apphost_rejects_reordered_declared_resources(self) -> None:
        self.repin_active_recapture(
            "apphost-smoke.json",
            lambda document: document["topology"].__setitem__(
                "declaredResources", sorted(evidence.APPHOST_DECLARED_RESOURCES)
            ),
        )

        errors, _, _ = self.validate_active()

        self.assertIn(
            "Live AppHost smoke does not name exactly the ten declared resources in canonical order.",
            errors,
        )

    def test_live_lane_accepts_exact_current_provider_and_authenticated_apphost_evidence(self) -> None:
        self.make_live_apphost_pass()

        self.assertEqual(self.validate_live(), [])

    def test_live_lane_tolerates_stopwatch_wall_clock_jitter_but_not_forged_durations(self) -> None:
        self.make_live_apphost_pass()
        report = _read_json(self.live_root / "provider-verification.json")
        original = report["timing"]["run"]["durationMilliseconds"]
        report["timing"]["run"]["durationMilliseconds"] = original + 4
        _write_json(self.live_root / "provider-verification.json", report)
        self.assertEqual(self.write_live_receipt(), [])
        self.assertEqual(
            [error for error in self.validate_live() if "duration contradicts" in error],
            [],
        )

        report["timing"]["run"]["durationMilliseconds"] = original + 20
        _write_json(self.live_root / "provider-verification.json", report)
        writer_errors = self.write_live_receipt()
        self.assertTrue(any("duration contradicts" in error for error in writer_errors), writer_errors)
        self.assertTrue(
            any("run duration contradicts its timestamps" in error for error in self.validate_live()),
        )

    def test_live_receipt_reuses_the_pre_provider_manifest_without_replacing_it(self) -> None:
        manifest_path = self.active_root / "frontcomposer-runtime-inputs.json"
        manifest_before = manifest_path.read_bytes()

        self.assertEqual(self.write_live_receipt(), [])

        self.assertEqual(manifest_path.read_bytes(), manifest_before)
        receipt = _read_json(self.live_root / "run-evidence.json")
        manifest = _read_json(manifest_path)
        self.assertEqual(
            receipt["runtimeInputTreeSha256"],
            manifest["treeSha256"],
        )
        self.assertEqual(
            receipt["frontComposerRevision"],
            manifest["capturedRevision"],
        )

    def test_live_receipt_writer_rejects_a_stale_report_without_replacing_receipt(self) -> None:
        receipt_path = self.live_root / "run-evidence.json"
        receipt_before = receipt_path.read_bytes()
        report_path = self.live_root / "provider-verification.json"
        report = _read_json(report_path)
        report["identity"]["observedSourceSha"] = "0" * 40
        _write_json(report_path, report)

        errors = self.write_live_receipt()

        self.assertTrue(any("observedSourceSha is stale" in error for error in errors), errors)
        self.assertEqual(receipt_path.read_bytes(), receipt_before)

    def test_live_receipt_writer_rejects_manifest_at_provider_start_without_replacing_receipt(self) -> None:
        receipt_path = self.live_root / "run-evidence.json"
        receipt_before = receipt_path.read_bytes()
        report = _read_json(self.live_root / "provider-verification.json")
        manifest_path = self.active_root / "frontcomposer-runtime-inputs.json"
        manifest = _read_json(manifest_path)
        manifest["capturedAt"] = report["timing"]["run"]["startedAt"]
        _write_json(manifest_path, manifest)

        errors = self.write_live_receipt()

        self.assertIn(
            "Pre-provider runtime-input manifest does not predate provider execution.",
            errors,
        )
        self.assertEqual(receipt_path.read_bytes(), receipt_before)

    def test_live_provider_rejects_invalid_state_event_durations(self) -> None:
        self.make_live_apphost_pass()
        report_path = self.live_root / "provider-verification.json"
        original = _read_json(report_path)
        for invalid in (None, True, -1, evidence.MAX_RUN_MILLISECONDS + 1):
            with self.subTest(invalid=invalid):
                report = copy.deepcopy(original)
                report["interactions"][0]["stateEvents"][0]["durationMilliseconds"] = invalid
                _write_json(report_path, report)
                writer_errors = self.write_live_receipt()
                self.assertTrue(any("setup duration is unbounded" in error for error in writer_errors), writer_errors)
                errors = self.validate_live()
                self.assertTrue(any("setup duration is unbounded" in error for error in errors), errors)

    def test_live_provider_requires_exact_indices_contained_events_and_empty_approval_bindings(self) -> None:
        self.make_live_apphost_pass()
        report_path = self.live_root / "provider-verification.json"
        original = _read_json(report_path)
        mutations = (
            lambda report: report["interactions"][0].__setitem__("index", True),
            lambda report: report["interactions"][0]["stateEvents"][0].__setitem__(
                "durationMilliseconds",
                report["interactions"][0]["durationMilliseconds"] + 1,
            ),
            lambda report: report["identity"].__setitem__(
                "evidenceManifestSha256", "0" * 64
            ),
            lambda report: report["identity"].__setitem__(
                "decisionRecordSha256", "0" * 64
            ),
            lambda report: report["identity"].__setitem__(
                "subjectSha256", "0" * 64
            ),
        )
        for mutate in mutations:
            with self.subTest(mutate=mutate):
                report = copy.deepcopy(original)
                mutate(report)
                _write_json(report_path, report)
                self.assertNotEqual(self.write_live_receipt(), [])
                errors = self.validate_live()
                self.assertTrue(
                    any(
                        "did not pass deterministically" in error
                        or "state-event durations exceed" in error
                        or "stale or untruthful" in error
                        for error in errors
                    ),
                    errors,
                )

    def test_live_lane_rejects_current_pact_byte_drift(self) -> None:
        self.make_live_apphost_pass()
        pact_path = self.pact_root / evidence.PACT_FILES[0]
        pact = _read_json(pact_path)
        pact["metadata"]["liveMutation"] = "different-current-bytes"
        _write_json(pact_path, pact)

        errors = self.validate_live()

        self.assertTrue(any("exact current Pact bytes" in error for error in errors), errors)

    def test_live_lane_rejects_manifest_and_catalog_drift(self) -> None:
        self.make_live_apphost_pass()
        manifest = _read_json(self.pact_root / "interaction-manifest.json")
        manifest["pactFiles"].pop()
        _write_json(self.pact_root / "interaction-manifest.json", manifest)
        catalog = _read_json(self.pact_root / "provider-state-catalog.json")
        extra = copy.deepcopy(catalog["states"][0])
        extra["name"] = "undeclared-extra-state"
        catalog["states"].append(extra)
        _write_json(self.pact_root / "provider-state-catalog.json", catalog)

        errors = self.validate_live()

        self.assertTrue(any("pact-file attribution" in error for error in errors), errors)
        self.assertTrue(any("catalog set must equal" in error for error in errors), errors)

    def test_live_lane_rejects_failed_provider_or_apphost_evidence(self) -> None:
        report = _read_json(self.live_root / "provider-verification.json")
        report["finalVerdict"] = "failed"
        _write_json(self.live_root / "provider-verification.json", report)
        smoke = _read_json(self.live_root / "apphost-smoke.json")
        smoke["finalVerdict"] = "failed"
        smoke["reasonCodes"] = ["query.provenance.missing"]
        _write_json(self.live_root / "apphost-smoke.json", smoke)

        errors = self.validate_live()

        self.assertTrue(any("finalVerdict must equal 'passed'" in error for error in errors), errors)
        self.assertTrue(any("AppHost smoke is not a clean passing run" in error for error in errors), errors)

    def test_live_apphost_rejects_semantically_self_consistent_empty_package_authority(self) -> None:
        self.make_live_apphost_pass()
        smoke_path = self.live_root / "apphost-smoke.json"

        def empty_ledger(ledger: dict[str, Any]) -> None:
            ledger["assetsGraphs"] = []
            ledger["toolPackages"] = []
            ledger["packages"] = []
            ledger["treeSha256"] = hashlib.sha256(
                json.dumps(
                    {"assetsGraphs": [], "toolPackages": [], "packages": []},
                    sort_keys=True,
                    separators=(",", ":"),
                ).encode("utf-8")
            ).hexdigest()

        self.mutate_live_package_ledger(
            "apphost-smoke.json", evidence.APPHOST_PACKAGE_LEDGER_FILE, empty_ledger
        )
        document = _read_json(smoke_path)
        document["startup"]["outputPreparation"]["evaluatedInputBinding"][
            "assetsGraphs"
        ] = []
        _write_json(smoke_path, document)

        errors = evidence.validate_live(
            self.live_root,
            self.pact_root,
            ROOT,
            provider_package_root=self.package_root,
            apphost_package_root=None,
        )

        self.assertIn(
            "Resolved-package ledger must bind at least one assets graph.", errors
        )
        self.assertIn(
            "Resolved-package ledger must bind at least one global package.", errors
        )
        self.assertIn(
            "Live AppHost package ledger must bind the canonical AppHost assets root.",
            errors,
        )

    def test_live_apphost_requires_canonical_root_in_exact_ledger_and_evaluated_set(self) -> None:
        self.make_live_apphost_pass()
        smoke_path = self.live_root / "apphost-smoke.json"
        replacement = evidence.PROVIDER_PACKAGE_ASSETS[0]

        def rebind_graph(ledger: dict[str, Any]) -> None:
            ledger["assetsGraphs"][0]["path"] = replacement
            ledger["treeSha256"] = hashlib.sha256(
                json.dumps(
                    {
                        "assetsGraphs": ledger["assetsGraphs"],
                        "packages": ledger["packages"],
                    },
                    sort_keys=True,
                    separators=(",", ":"),
                ).encode("utf-8")
            ).hexdigest()

        self.mutate_live_package_ledger(
            "apphost-smoke.json", evidence.APPHOST_PACKAGE_LEDGER_FILE, rebind_graph
        )
        document = _read_json(smoke_path)
        document["startup"]["outputPreparation"]["evaluatedInputBinding"][
            "assetsGraphs"
        ] = [replacement]
        _write_json(smoke_path, document)

        errors = evidence.validate_live(
            self.live_root,
            self.pact_root,
            ROOT,
            provider_package_root=self.package_root,
            apphost_package_root=None,
        )

        self.assertIn(
            "Live AppHost package ledger must bind the canonical AppHost assets root.",
            errors,
        )
        self.assertIn(
            "Live AppHost evaluated input binding is incomplete or outside its authorities.",
            errors,
        )

    def test_live_lane_rejects_drifted_query_provenance_stamp(self) -> None:
        self.make_live_apphost_pass()
        smoke = _read_json(self.live_root / "apphost-smoke.json")
        smoke["observations"]["queryProvenance"]["provenance"] = "Unknown"
        _write_json(self.live_root / "apphost-smoke.json", smoke)

        errors = self.validate_live()

        self.assertTrue(any("query provenance stamp is missing or drifted" in error for error in errors), errors)

    def test_live_provider_rejects_duplicate_input_hash_names(self) -> None:
        self.make_live_apphost_pass()
        report_path = self.live_root / "provider-verification.json"
        report = _read_json(report_path)
        report["inputHashes"].append(copy.deepcopy(report["inputHashes"][0]))
        _write_json(report_path, report)
        writer_errors = self.write_live_receipt()
        self.assertTrue(any("duplicate names" in error for error in writer_errors), writer_errors)

        errors = self.validate_live()

        self.assertTrue(any("duplicate names" in error for error in errors), errors)

    def test_live_apphost_observations_require_exact_typed_semantics(self) -> None:
        self.make_live_apphost_pass()
        path = self.live_root / "apphost-smoke.json"
        original = _read_json(path)
        mutations = (
            lambda document: document["observations"]["health"].__setitem__("statusCode", 500),
            lambda document: document["observations"]["commandSubmit"].__setitem__("authenticated", 1),
            lambda document: document["observations"]["commandStatus"].__setitem__("terminalStatus", "Rejected"),
            lambda document: document["observations"]["queryProvenance"].__setitem__("responseTenantId", "other"),
            lambda document: document["observations"]["projectionSignalR"].__setitem__("extra", True),
            lambda document: document["observations"]["commandSubmit"].__setitem__(
                "aggregateId", "pact-reconciliation-"
            ),
            lambda document: document["observations"]["commandSubmit"].__setitem__(
                "correlationId", "01M2AW1VW8Z2GHQ0ZGCTTCWWW1"
            ),
            lambda document: document["observations"]["projectionSignalR"].__setitem__(
                "endpoint", "http://127.0.0.1:18001/unrelated"
            ),
            lambda document: document["authorizationControls"]["projectionSignalR"].__setitem__(
                "endpoint", "http://127.0.0.1:18001/hubs/projection-changes?drift=true"
            ),
        )
        for mutate in mutations:
            with self.subTest(mutate=mutate):
                document = copy.deepcopy(original)
                mutate(document)
                _write_json(path, document)
                errors = self.validate_live()
                self.assertTrue(
                    any(
                        "incorrect semantics" in error
                        or "health status" in error
                        or "aggregate identity" in error
                        or "command correlation" in error
                        or "projection-change hub" in error
                        or "authorization control" in error
                        for error in errors
                    ),
                    errors,
                )

    def test_live_apphost_timeout_and_elapsed_duration_are_exact_and_bounded(self) -> None:
        self.make_live_apphost_pass()
        path = self.live_root / "apphost-smoke.json"
        original = _read_json(path)
        mutations = (
            lambda document: document.__setitem__("timeoutSeconds", True),
            lambda document: document.__setitem__("timeoutSeconds", 29),
            lambda document: document.__setitem__("completedAt", "2026-09-13T17:00:32+00:00"),
        )
        for mutate in mutations:
            with self.subTest(mutate=mutate):
                document = copy.deepcopy(original)
                mutate(document)
                _write_json(path, document)
                errors = self.validate_live()
                self.assertTrue(any("timeoutSeconds" in error or "elapsed duration" in error for error in errors), errors)

    def test_live_apphost_authorization_controls_are_exact_and_credential_specific(self) -> None:
        self.make_live_apphost_pass()
        path = self.live_root / "apphost-smoke.json"
        original = _read_json(path)
        mutations = (
            lambda document: document["authorizationControls"].pop("commandStatus"),
            lambda document: document["authorizationControls"]["queryProvenance"].__setitem__("statusCode", 200),
            lambda document: document["authorizationControls"]["projectionSignalR"].__setitem__(
                "reasonCode", "authorization.anonymous.rejected"
            ),
            lambda document: document["authorizationControls"]["commandSubmit"].__setitem__("statusCode", True),
        )
        for mutate in mutations:
            with self.subTest(mutate=mutate):
                document = copy.deepcopy(original)
                mutate(document)
                _write_json(path, document)
                errors = self.validate_live()
                self.assertTrue(any("authorization" in error.lower() for error in errors), errors)

    def test_live_apphost_cleanup_rejects_integer_boolean_substitutions(self) -> None:
        self.make_live_apphost_pass()
        path = self.live_root / "apphost-smoke.json"
        original = _read_json(path)
        for field, replacement in (
            ("hostStopped", 1),
            ("portsClosed", 1),
            ("runningAppHostsAfterAttempt", False),
        ):
            with self.subTest(field=field):
                document = copy.deepcopy(original)
                document["cleanup"][field] = replacement
                _write_json(path, document)
                errors = self.validate_live()
                self.assertTrue(
                    any("cleanup" in error and "is incomplete" in error for error in errors),
                    errors,
                )

    def test_live_tree_recursively_rejects_nested_files_directories_and_cookie_values(self) -> None:
        self.make_live_apphost_pass()
        nested = self.live_root / "nested"
        nested.mkdir()
        (nested / "diagnostic.txt").write_text("cookie=session-secret\n", encoding="utf-8")

        errors = self.validate_live()

        self.assertTrue(any("must contain exactly" in error for error in errors), errors)
        self.assertTrue(any("Redaction scan failed" in error for error in errors), errors)

    def test_live_writer_redaction_rejects_quoted_secret_and_cookie_keys(self) -> None:
        report_path = self.live_root / "provider-verification.json"
        original = _read_json(report_path)
        for key in ("access_token", "api_key", "password", "cookie", "session_cookie"):
            with self.subTest(key=key):
                report = copy.deepcopy(original)
                report[key] = "credential-value-never-uploaded"
                _write_json(report_path, report)

                errors = self.write_live_receipt()

                self.assertTrue(any("Redaction scan failed" in error for error in errors), errors)

    def test_live_lane_rejects_stale_provenance_and_extra_files(self) -> None:
        self.make_live_apphost_pass()
        report = _read_json(self.live_root / "provider-verification.json")
        report["identity"]["observedSourceSha"] = "0" * 40
        _write_json(self.live_root / "provider-verification.json", report)
        (self.live_root / "unexpected.json").write_text("{}\n", encoding="utf-8")

        errors = self.validate_live()

        self.assertTrue(
            any("both package-ledger sidecars" in error for error in errors), errors
        )
        self.assertTrue(any("observedSourceSha is stale" in error for error in errors), errors)

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
        self.repin_report()

        self.assertEqual(self.validate(), [])

    def test_complete_truthful_runtime_only_failure_is_structurally_valid(self) -> None:
        def mutate(report: dict[str, Any]) -> None:
            for interaction in report["interactions"]:
                interaction["resultCode"] = "interaction.passed"
            report["reasonCodes"] = list(report["identity"]["reasonCodes"])

        _rewrite_report(self.evidence_root, mutate)
        self.repin_report()

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

        self.assertTrue(any("does not match the sealed historical capture" in error for error in errors), errors)
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

    def test_redaction_classifies_each_authorization_and_new_secret_grammar(self) -> None:
        artifact = Path(self._temporary.name) / "redaction.json"
        artifact.write_bytes(
            (
                "{\r\n"
                '  "Authorization": "Bearer FC_CONTRACT_TOKEN",\r\n'
                '  "authorization": "Bearer opaque-secret",\r\n'
                '  "client_secret": "value",\r\n'
                '  "sourceToken": "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_abcd"\r\n'
                "}\r\n"
            ).encode("utf-8")
        )

        errors: list[str] = []
        evidence._scan_redaction(artifact, errors)

        self.assertTrue(any("raw Authorization header" in error for error in errors), errors)
        self.assertTrue(any("client" in error for error in errors), errors)
        self.assertTrue(any("encoded token-like value" in error for error in errors), errors)

        benign = Path(self._temporary.name) / "redaction-benign.json"
        benign.write_text('{"requiresAuthorization":false}\n', encoding="utf-8")
        benign_errors: list[str] = []
        evidence._scan_redaction(benign, benign_errors)
        self.assertFalse(
            any("raw Authorization header" in error for error in benign_errors),
            benign_errors,
        )

    def test_manifest_metadata_drift_is_rejected_before_receipt_replacement(self) -> None:
        manifest_path = self.pact_root / "interaction-manifest.json"
        manifest = _read_json(manifest_path)
        manifest["interactions"][0]["classifierExpectation"] = "forged-but-nonempty"
        _write_json(manifest_path, manifest)

        errors = self.write_live_receipt()

        self.assertTrue(any("classifierExpectation differs" in error for error in errors), errors)

    def test_distinct_actor_aliases_for_one_principal_do_not_separate_roles(self) -> None:
        self.claim_active_approval()
        eventstore_actor = next(
            iter(evidence.APPROVAL_AUTHORITY_BOOTSTRAP["eventstore-maintainer"])
        )
        frontcomposer_actor = next(
            iter(evidence.APPROVAL_AUTHORITY_BOOTSTRAP["frontcomposer-maintainer"])
        )
        evidence.APPROVAL_PRINCIPAL_BOOTSTRAP[frontcomposer_actor] = (
            evidence.APPROVAL_PRINCIPAL_BOOTSTRAP[eventstore_actor]
        )

        errors, approval_issues, _ = self.validate_active()

        self.assertTrue(any("immutable principals" in issue for issue in approval_issues), approval_issues)
        self.assertTrue(any("immutable principals" in error for error in errors), errors)

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

    def test_powershell_redaction_matches_new_secret_keys_and_base64url(self) -> None:
        manifest_path = self.pact_root / "interaction-manifest.json"
        manifest = _read_json(manifest_path)
        manifest["client_secret"] = "opaque"
        manifest["private_key"] = "opaque"
        manifest["sas_token"] = "opaque"
        manifest["reasonToken"] = (
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_abcd"
        )
        _write_json(manifest_path, manifest)
        artifact_root = Path(self._temporary.name) / "contract-artifacts-new-grammar"

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

        output = result.stdout + result.stderr
        self.assertNotEqual(result.returncode, 0)
        for expected in ("client", "private", "sas", "encoded token-like"):
            self.assertIn(expected, output)

    def test_preserved_successor_record_must_be_byte_identical_to_the_capture(self) -> None:
        relative = "frontcomposer-11-24-runtime-identity-successor.md"
        path = self.evidence_root / relative
        # A relocation-friendly link rewrite is still not a byte-identical preservation.
        path.write_bytes(
            path.read_bytes().replace(
                b"(evidence/frontcomposer-story-11-24/",
                b"(",
            )
        )
        _set_manifest_hash(self.evidence_root, relative)

        errors = self.validate()

        self.assertTrue(
            any("not byte-identical to the EventStore-owned capture" in error for error in errors),
            errors,
        )

    def test_preserved_owner_actions_record_must_be_byte_identical_to_the_capture(self) -> None:
        relative = f"{evidence.SUBJECT_DIR}/owner-actions.md"
        path = self.evidence_root / relative
        path.write_bytes(path.read_bytes() + b"\nAppended after capture.\n")
        _set_manifest_hash(self.evidence_root, relative)

        errors = self.validate()

        self.assertIn(
            f"Preserved evidence is not byte-identical to the EventStore-owned capture: {relative}",
            errors,
        )

    def test_identity_input_kind_cannot_be_relabelled(self) -> None:
        def mutate(report: dict[str, Any]) -> None:
            for entry in report["inputHashes"]:
                if entry["name"] == "eventstore-owner.json":
                    entry["kind"] = "pact"

        _rewrite_report(self.evidence_root, mutate)

        errors = self.validate()

        self.assertTrue(
            any("input kind is incorrect: eventstore-owner.json" in error for error in errors),
            errors,
        )

    def test_duration_tolerates_sub_millisecond_rounding_but_not_wider_drift(self) -> None:
        def round_up(report: dict[str, Any]) -> None:
            # 2499.5927 ms: a producer that rounds rather than truncates is still truthful.
            report["timing"]["run"]["durationMilliseconds"] = 2500

        _rewrite_report(self.evidence_root, round_up)
        self.assertEqual(
            [error for error in self.validate() if "duration contradicts" in error],
            [],
        )

        def drift(report: dict[str, Any]) -> None:
            report["timing"]["run"]["durationMilliseconds"] = 2502

        _rewrite_report(self.evidence_root, drift)
        self.assertTrue(
            any("run duration contradicts its timestamps" in error for error in self.validate()),
        )

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

    def test_preserved_provider_report_cannot_be_relabelled_as_passing_in_repository(self) -> None:
        # The approved historical commit no longer carries these bytes, so a rewritten report
        # plus a re-sealed manifest is the exact forgery the capture pin has to stop.
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

        errors = self.validate()

        self.assertIn(
            "Preserved evidence is not byte-identical to the EventStore-owned capture: "
            "provider-verification/provider-verification.json",
            errors,
        )

    def test_every_captured_evidence_file_is_pinned_to_the_capture(self) -> None:
        self.assertEqual(
            set(evidence.CAPTURED_EVIDENCE_SHA256) | set(evidence.FRONTCOMPOSER_CAPTURED_EVIDENCE_SHA256),
            set(evidence.REQUIRED_SNAPSHOT_FILES),
        )
        pinned_evidence = evidence.CAPTURED_EVIDENCE_SHA256 | evidence.FRONTCOMPOSER_CAPTURED_EVIDENCE_SHA256
        for relative, pinned in pinned_evidence.items():
            self.assertEqual(_sha256(self.evidence_root / relative), pinned, relative)

    def test_manifest_provenance_cannot_claim_a_frontcomposer_run_was_captured(self) -> None:
        relative = "apphost-smoke/apphost-smoke.json"
        manifest_path = self.evidence_root / "sha256-manifest.json"
        manifest = _read_json(manifest_path)
        entry = next(item for item in manifest["files"] if item["path"] == relative)
        entry["provenance"] = "eventstore-capture"
        _write_json(manifest_path, manifest)

        errors = self.validate()

        self.assertIn(f"Evidence manifest provenance is not truthful for {relative}.", errors)

    def test_evidence_redaction_rejects_the_sibling_scanner_leak_classes(self) -> None:
        relative = "apphost-smoke/apphost-smoke.json"
        for value, expected in (
            ("Server=db;User Id=sa", "connectionstring"),
            ('"cookie": "replayed"', "cookie"),
            ("EVENTSTORE_SECRET=hunter2tokenvalue", "[A-Z0-9_]{8,}=.{6,}"),
        ):
            with self.subTest(expected=expected):
                def mutate(smoke: dict[str, Any], value: str = value, expected: str = expected) -> None:
                    if expected == "cookie":
                        smoke["cookie"] = "replayed"
                    else:
                        smoke["diagnosticDetail"] = (
                            f"ConnectionString={value}" if expected == "connectionstring" else value
                        )

                _rewrite_evidence_json(self.evidence_root, relative, mutate)

                errors = self.validate()

                self.assertTrue(
                    any(
                        "Redaction scan failed for apphost-smoke.json:" in error
                        and expected in error
                        for error in errors
                    ),
                    errors,
                )

    def test_evidence_redaction_rejects_a_raw_authorization_header(self) -> None:
        relative = "apphost-smoke/apphost-smoke.json"

        def mutate(smoke: dict[str, Any]) -> None:
            smoke["requestHeaders"] = "Authorization: Basic bearerless"

        _rewrite_evidence_json(self.evidence_root, relative, mutate)

        errors = self.validate()

        self.assertTrue(
            any("raw Authorization header" in error for error in errors),
            errors,
        )

    def test_unobserved_apphost_outcome_cannot_carry_a_response_code(self) -> None:
        relative = "apphost-smoke/apphost-smoke.json"

        def mutate(smoke: dict[str, Any]) -> None:
            observation = smoke["observations"]["commandSubmit"]
            observation["result"] = "not-observed"
            observation["reasonCode"] = "runtime.not-reached"

        _rewrite_evidence_json(self.evidence_root, relative, mutate)

        errors = self.validate()

        self.assertIn(
            "AppHost commandSubmit is recorded as unobserved but carries a response code.",
            errors,
        )

    def _run_contract_validator(
        self,
        *arguments: str,
        environment: dict[str, str] | None = None,
        validator_script: Path | None = None,
    ) -> tuple[subprocess.CompletedProcess[str], Path]:
        artifact_root = Path(self._temporary.name) / "contract-artifacts"
        result = subprocess.run(
            [
                "pwsh",
                "-NoProfile",
                "-File",
                str(validator_script or ROOT / "eng/validate-contract-artifacts.ps1"),
                "-ArtifactDir",
                str(artifact_root),
                *arguments,
            ],
            cwd=self._temporary.name,
            check=False,
            capture_output=True,
            text=True,
            env=environment,
        )
        return result, artifact_root / "job-summary.md"

    def _write_fixture_contract_validator(self) -> Path:
        """Run the real CLI under the same deterministic fixture authorities as this test."""
        fixture_eng = self.artifact_root / "eng"
        fixture_eng.mkdir(parents=True, exist_ok=True)
        powershell_path = fixture_eng / "validate-contract-artifacts.ps1"
        shutil.copyfile(ROOT / "eng/validate-contract-artifacts.ps1", powershell_path)
        captured_manifest = _read_json(
            self.active_root / "frontcomposer-runtime-inputs.json"
        )
        wrapper = f'''#!/usr/bin/env python3
import copy
import json
import subprocess
import sys
from datetime import datetime
from pathlib import Path

sys.path.insert(0, {str(ROOT / "eng")!r})
import eventstore_runtime_evidence as evidence

fixture_repository = Path({str(self.artifact_root)!r}).resolve()
fixture_live_root = Path({str(self.live_root)!r}).resolve()
fixture_pact_root = Path({str(self.pact_root)!r}).resolve()
captured_manifest = json.loads({json.dumps(json.dumps(captured_manifest))})
captured_entries = captured_manifest["entries"]
evidence.APPROVAL_AUTHORITY_BOOTSTRAP = {evidence.APPROVAL_AUTHORITY_BOOTSTRAP!r}
evidence.APPROVAL_PRINCIPAL_BOOTSTRAP = {evidence.APPROVAL_PRINCIPAL_BOOTSTRAP!r}

evidence._runtime_input_snapshot = lambda repository_root: (copy.deepcopy(captured_entries), [])
evidence._runtime_git_tree = lambda repository_root, revision: ({{}}, [])
evidence._canonical_live_locations = lambda repository_root: (
    fixture_live_root, fixture_pact_root
)

def fixture_git(repository_root, *arguments):
    if arguments[:2] in (("rev-parse", "HEAD"), ("rev-parse", "--verify")):
        return captured_manifest["capturedRevision"]
    return ""

evidence._git = fixture_git
evidence._git_completed = lambda repository_root, *arguments: subprocess.CompletedProcess(
    ["git", *arguments], 0, b"", b""
)

def fixture_provenance(repository_root, errors, *, runtime_manifest=None):
    manifest = runtime_manifest or captured_manifest
    return {{
        "sourceSha": {evidence.ACTIVE_SOURCE_SHA!r},
        "releaseVersion": {evidence.ACTIVE_VERSION!r},
        "buildsSha": {evidence.ACTIVE_BUILDS_SHA!r},
        "releaseInventorySha256": {evidence.INVENTORY_SHA256!r},
        "frontComposerRevision": str(manifest["capturedRevision"]),
        "runtimeInputTreeSha256": str(manifest["treeSha256"]),
    }}

evidence._live_provenance = fixture_provenance
evidence.validate_package_ledger = lambda document, *args, **kwargs: (
    datetime.fromisoformat(document["capturedAt"])
    if isinstance(document, dict) and isinstance(document.get("capturedAt"), str)
    else None
)
evidence._evaluate_apphost_inputs = lambda *args, **kwargs: json.loads(
    (fixture_live_root / "apphost-smoke.json").read_text(encoding="utf-8-sig")
)["startup"]["outputPreparation"]["evaluatedInputBinding"]
evidence._apphost_runtime_output_binding = lambda *args, **kwargs: json.loads(
    (fixture_live_root / "apphost-smoke.json").read_text(encoding="utf-8-sig")
)["startup"]["outputPreparation"]["runtimeOutputBinding"]

raise SystemExit(evidence.main())
'''
        (fixture_eng / "eventstore_runtime_evidence.py").write_text(
            wrapper,
            encoding="utf-8",
        )
        return powershell_path

    def test_prose_handoff_artifact_is_scanned_for_encoded_tokens(self) -> None:
        handoff = self.pact_root / "provider-verification-handoff.md"
        original = handoff.read_text(encoding="utf-8")
        handoff.write_text(
            original + "\n\nLeaked value: " + ("A" * 64) + "\n", encoding="utf-8"
        )

        result, summary = self._run_contract_validator("-PactDir", str(self.pact_root))

        handoff.write_text(original, encoding="utf-8")
        self.assertNotEqual(result.returncode, 0)
        # pwsh wraps and colourizes Write-Error, so read the validator's own error file.
        recorded = (summary.parent / "contract-validation-errors.txt").read_text(
            encoding="utf-8"
        )
        self.assertIn(
            "Redaction scan failed for provider-verification-handoff.md: encoded token-like payload",
            recorded,
        )

    def test_prose_handoff_artifact_accepts_its_committed_bytes(self) -> None:
        result, _ = self._run_contract_validator("-PactDir", str(self.pact_root))

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def test_contract_validator_forwards_the_package_and_manifest_environment(self) -> None:
        recorder = Path(self._temporary.name) / "cli-arguments.txt"
        fake_bin = Path(self._temporary.name) / "argument-recorder-bin"
        fake_bin.mkdir()
        fake_python = fake_bin / "python3"
        fake_python.write_text(
            "#!/bin/sh\n"
            f'printf "%s\\n" "$@" >> {recorder}\n'
            "printf '%s\\n' 'EventStore runtime evidence operation completed successfully.'\n"
            "printf '%s\\n' 'EventStore runtime approval: OPEN'\n"
            "printf '%s\\n' 'EventStore runtime approval issue: Missing named actor for required role: eventstore-maintainer'\n",
            encoding="utf-8",
        )
        fake_python.chmod(0o755)
        environment = os.environ.copy()
        environment["PATH"] = f"{fake_bin}{os.pathsep}{environment['PATH']}"
        environment["FRONTCOMPOSER_PROVIDER_PACKAGES"] = str(self.package_root)
        environment["FRONTCOMPOSER_APPHOST_PACKAGES"] = str(self.package_root)
        environment["FRONTCOMPOSER_RUNTIME_INPUT_MANIFEST"] = str(
            self.active_root / "frontcomposer-runtime-inputs.json"
        )

        self._run_contract_validator(
            "-RequireProviderVerification",
            "-PactDir",
            str(self.pact_root),
            "-ProviderVerificationReport",
            str(self.live_root / "provider-verification.json"),
            "-LiveEvidenceRoot",
            str(self.live_root),
            environment=environment,
        )

        recorded = recorder.read_text(encoding="utf-8").splitlines()
        self.assertIn("--provider-package-root", recorded)
        self.assertIn("--apphost-package-root", recorded)
        self.assertIn("--runtime-input-manifest", recorded)
        self.assertIn(
            str(self.active_root / "frontcomposer-runtime-inputs.json"), recorded
        )

    def test_required_provider_lane_formats_the_open_approval_summary(self) -> None:
        fake_bin = Path(self._temporary.name) / "open-validator-bin"
        fake_bin.mkdir()
        fake_python = fake_bin / "python3"
        fake_python.write_text(
            "#!/bin/sh\n"
            "printf '%s\\n' 'EventStore runtime evidence operation completed successfully.'\n"
            "printf '%s\\n' 'EventStore runtime approval: OPEN'\n"
            "printf '%s\\n' 'EventStore runtime approval issue: Missing named actor for required role: eventstore-maintainer'\n",
            encoding="utf-8",
        )
        fake_python.chmod(0o755)
        environment = os.environ.copy()
        environment["PATH"] = f"{fake_bin}{os.pathsep}{environment['PATH']}"
        result, summary = self._run_contract_validator(
            "-RequireProviderVerification",
            "-ProviderVerificationReport",
            "_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/"
            "provider-verification.json",
            environment=environment,
        )

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        text = summary.read_text(encoding="utf-8")
        self.assertIn("Historical Story 11.24 integrity: IMMUTABLE_ARCHIVE_VALID", text)
        self.assertIn("Prior Builds 35c3d1e5 compatibility archive: PRIOR_COMPATIBILITY_ARCHIVE_VALID", text)
        self.assertIn("Active EventStore identity v2 and sealed evidence: ACTIVE_IDENTITY_AND_EVIDENCE_VALID", text)
        self.assertIn("Migration approval: OPEN: Missing named actor for required role: eventstore-maintainer", text)
        self.assertIn("Current provider verification: CURRENT_PROVIDER_PASSED", text)
        self.assertIn("Current authenticated AppHost smoke: AUTHENTICATED_APPHOST_PASSED", text)

    def test_contract_artifact_publication_coordinates_resolve_to_real_files(self) -> None:
        workflow_lines = (ROOT / ".github/workflows/quality.yml").read_text(
            encoding="utf-8"
        ).splitlines()
        step_header = "- name: Upload contract artifacts"
        step_start = next(
            index for index, line in enumerate(workflow_lines)
            if line.strip() == step_header
        )
        step_indent = len(workflow_lines[step_start]) - len(
            workflow_lines[step_start].lstrip()
        )
        step_end = next(
            (
                index for index in range(step_start + 1, len(workflow_lines))
                if workflow_lines[index].strip().startswith("- name:")
                and len(workflow_lines[index]) - len(workflow_lines[index].lstrip())
                == step_indent
            ),
            len(workflow_lines),
        )
        upload_step = workflow_lines[step_start:step_end]
        self.assertIn("if: success()", (line.strip() for line in upload_step))
        self.assertTrue(any("actions/upload-artifact@" in line for line in upload_step))
        path_index = next(
            index for index, line in enumerate(upload_step)
            if line.strip() == "path: |"
        )
        path_indent = len(upload_step[path_index]) - len(upload_step[path_index].lstrip())
        upload_patterns: list[str] = []
        for line in upload_step[path_index + 1:]:
            indent = len(line) - len(line.lstrip())
            if line.strip() and indent <= path_indent:
                break
            if line.strip():
                upload_patterns.append(line.strip())

        produced_files: set[Path] = set()
        for pattern in upload_patterns:
            matches = {path.resolve() for path in ROOT.glob(pattern) if path.is_file()}
            if not pattern.startswith("artifacts/contracts/"):
                self.assertTrue(matches, f"Upload coordinate produced no files: {pattern}")
            produced_files.update(matches)

        required = (
            "_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json",
            "_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v2.json",
            "_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-11.md",
            "_bmad-output/implementation-artifacts/spec-11-25-current-eventstore-release-identity-and-evidence.md",
            "tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-verification-handoff.md",
            *evidence.RUNTIME_PACT_INPUTS,
        )
        required_files = {(ROOT / relative).resolve() for relative in required}
        for authority_root in (
            CANONICAL_EVIDENCE,
            CANONICAL_PRIOR_EVIDENCE,
            CANONICAL_ACTIVE_EVIDENCE,
            CANONICAL_LIVE_EVIDENCE,
        ):
            authority_files = {
                path.resolve() for path in authority_root.rglob("*") if path.is_file()
            }
            self.assertTrue(authority_files, f"Authority tree is empty: {authority_root}")
            required_files.update(authority_files)

        missing = sorted(
            path.relative_to(ROOT).as_posix()
            for path in required_files - produced_files
        )
        self.assertEqual(missing, [], "Upload step omits required authority files")

    def test_required_provider_lane_publishes_the_approved_summary_branch(self) -> None:
        self.make_live_apphost_pass()
        self.claim_active_approval()
        validator_script = self._write_fixture_contract_validator()
        result, summary = self._run_contract_validator(
            "-RequireProviderVerification",
            "-PactDir",
            str(self.pact_root),
            "-ProviderVerificationReport",
            str(self.live_root / "provider-verification.json"),
            "-FrontComposerEvidenceRoot",
            str(self.evidence_root),
            "-LiveEvidenceRoot",
            str(self.live_root),
            "-PriorEvidenceRoot",
            str(self.history_root),
            "-ActiveEvidenceRoot",
            str(self.active_root),
            "-ActiveIdentity",
            str(self.identity_path),
            "-ProviderPackageRoot",
            str(self.package_root),
            "-AppHostPackageRoot",
            str(self.package_root),
            "-RuntimeInputManifest",
            str(self.active_root / "frontcomposer-runtime-inputs.json"),
            validator_script=validator_script,
        )

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("EventStore runtime approval: APPROVED", result.stdout)
        self.assertIn("Migration approval: APPROVED", summary.read_text(encoding="utf-8"))

    def test_required_provider_lane_rejects_a_report_outside_the_owned_evidence_tree(self) -> None:
        foreign = Path(self._temporary.name) / "foreign-provider-verification.json"
        shutil.copyfile(
            CANONICAL_LIVE_EVIDENCE / "provider-verification.json",
            foreign,
        )

        result, summary = self._run_contract_validator(
            "-RequireProviderVerification",
            "-ProviderVerificationReport",
            str(foreign),
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("must use the FrontComposer-owned report", result.stdout + result.stderr)
        self.assertIn("Current provider verification: REQUIRED_REJECTED", summary.read_text(encoding="utf-8"))

    def test_required_provider_lane_propagates_an_evidence_validator_failure(self) -> None:
        # Semantics-preserving reformatting: interactions still satisfy the manifest
        # cross-checks, but the live report no longer binds their bytes.
        manifest_path = self.pact_root / "interaction-manifest.json"
        manifest = _read_json(manifest_path)
        manifest_path.write_text(json.dumps(manifest, indent=4) + "\n", encoding="utf-8")

        fake_bin = Path(self._temporary.name) / "failed-validator-bin"
        fake_bin.mkdir()
        fake_python = fake_bin / "python3"
        fake_python.write_text(
            "#!/bin/sh\n"
            "printf '%s\\n' 'Live provider input hash does not bind exact current Pact bytes.' >&2\n"
            "exit 1\n",
            encoding="utf-8",
        )
        fake_python.chmod(0o755)
        environment = os.environ.copy()
        environment["PATH"] = f"{fake_bin}{os.pathsep}{environment['PATH']}"

        result, summary = self._run_contract_validator(
            "-RequireProviderVerification",
            "-PactDir",
            str(self.pact_root),
            environment=environment,
        )

        self.assertNotEqual(result.returncode, 0)
        output = result.stdout + result.stderr
        self.assertIn("exact current Pact bytes", output)
        self.assertIn("Current provider verification: REQUIRED_REJECTED", summary.read_text(encoding="utf-8"))


class GitTextEquivalenceTests(unittest.TestCase):
    """Compare the validator's text=auto classifier with real Git, the only authority."""

    SAMPLES = (
        ("plain crlf text", b"alpha\r\nbeta\r\n"),
        ("lone carriage return", b"alpha\r\nbeta\rgamma\r\n"),
        ("embedded nul", b"alpha\r\n\x00beta\r\n"),
        ("delete control", b"alpha\r\n\x7f\r\n"),
        ("high bytes", b"alpha\r\n\xc3\xa9\xc3\xa8\r\n"),
        ("printable controls", b"alpha\r\n\x08\x09\x0c\x1b\r\n"),
        ("at ratio limit", b"a\r\n" + b"x" * 1021 + b"\x01" * 8),
        ("below ratio limit", b"a\r\n" + b"x" * 1021 + b"\x01" * 7),
    )

    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self._temporary.cleanup)
        self.repository = Path(self._temporary.name)
        subprocess.run(["git", "init", "-q"], cwd=self.repository, check=True)
        (self.repository / ".gitattributes").write_text("* text=auto\n", encoding="utf-8")

    def _git_normalizes(self, data: bytes) -> bool:
        """Return whether Git itself applies text=auto EOL normalization to these bytes."""
        raw = subprocess.run(
            ["git", "hash-object", "--stdin"],
            cwd=self.repository,
            input=data,
            check=True,
            capture_output=True,
        ).stdout.strip()
        filtered = subprocess.run(
            ["git", "hash-object", "--path", "sample.txt", "--stdin"],
            cwd=self.repository,
            input=data,
            check=True,
            capture_output=True,
        ).stdout.strip()
        return raw != filtered

    def test_classifier_matches_git_for_every_sample(self) -> None:
        for label, data in self.SAMPLES:
            with self.subTest(sample=label):
                self.assertEqual(
                    evidence._git_auto_classifies_text(data),
                    self._git_normalizes(data),
                    label,
                )

    def test_lone_carriage_return_and_nul_are_binary(self) -> None:
        self.assertFalse(evidence._git_auto_classifies_text(b"alpha\rbeta"))
        self.assertFalse(evidence._git_auto_classifies_text(b"alpha\x00beta"))
        self.assertTrue(evidence._git_auto_classifies_text(b"alpha\r\nbeta"))


class DependencyInertToolingTests(unittest.TestCase):
    """The frozen 2026-09-13 scope rejects graph-selected inputs, not inert tooling."""

    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self._temporary.cleanup)
        self.repository = Path(self._temporary.name) / "repository"
        self.dependency = evidence.RUNTIME_DEPENDENCY_GITLINKS[0]
        self.checkout = self.repository / self.dependency
        self.checkout.mkdir(parents=True)
        for directory in (self.repository, self.checkout):
            subprocess.run(["git", "init", "-q"], cwd=directory, check=True)
            subprocess.run(
                ["git", "config", "user.name", "Inert Tooling Test"], cwd=directory, check=True
            )
            subprocess.run(
                ["git", "config", "user.email", "inert@example.test"], cwd=directory, check=True
            )
        (self.checkout / "Directory.Build.props").write_text("<Project />\n", encoding="utf-8")
        # Real dependency checkouts declare their projects under src/, never at the root, so
        # MSBuild default item globs cannot reach a root-level tooling directory.
        (self.checkout / "src" / "Tool").mkdir(parents=True)
        (self.checkout / "src" / "Tool" / "Tool.csproj").write_text(
            "<Project />\n", encoding="utf-8"
        )
        (self.checkout / ".gitignore").write_text(
            "node_modules/\n.husky/_/\n__pycache__/\nbin/\nobj/\n", encoding="utf-8"
        )
        subprocess.run(["git", "add", "-A"], cwd=self.checkout, check=True)
        subprocess.run(
            ["git", "commit", "-qm", "test: seed dependency"], cwd=self.checkout, check=True
        )

    def _head(self) -> str:
        return subprocess.run(
            ["git", "rev-parse", "HEAD"],
            cwd=self.checkout,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.strip()

    def _issues(self) -> list[str]:
        return evidence._validate_dependency_checkout(
            self.repository, self.dependency, self._head()
        )

    def test_classifier_separates_inert_tooling_from_graph_selected_paths(self) -> None:
        indexed = ["src/Tool/Tool.csproj"]
        output_roots = evidence._project_output_roots(indexed)
        directories = evidence._project_directories(indexed)
        for relative, expected in (
            ("node_modules/.bin/tsc", "inert-tooling"),
            ("packages/web/node_modules/left-pad/index.js", "inert-tooling"),
            (".husky/_/pre-commit", "inert-tooling"),
            ("scripts/__pycache__/tool.cpython-314.pyc", "inert-tooling"),
            ("src/Tool/bin/Debug/net10.0/Tool.dll", "generated-output"),
            ("src/Tool/obj/project.assets.json", "generated-output"),
            ("Directory.Packages.props", "graph-selected"),
            ("src/Feature.cs", "graph-selected"),
            # A tooling directory nested under an indexed project is reachable by MSBuild
            # default item globs, so it is never laundered into the inert class.
            ("src/Tool/node_modules/left-pad/index.js", "graph-selected"),
            ("src/Tool/node_modules/.bin/left-pad", "graph-selected"),
            ("src/Tool/__pycache__/generated.pyc", "graph-selected"),
        ):
            with self.subTest(path=relative):
                self.assertEqual(
                    evidence._dependency_path_disposition(
                        relative, output_roots, directories
                    ),
                    expected,
                )

    def test_inert_tooling_trees_do_not_dirty_a_pinned_checkout(self) -> None:
        for relative in (
            "node_modules/left-pad/index.js",
            ".husky/_/pre-commit",
            "scripts/__pycache__/tool.cpython-314.pyc",
            "src/Tool/bin/Debug/net10.0/Tool.dll",
            "src/Tool/obj/project.assets.json",
        ):
            path = self.checkout / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("inert\n", encoding="utf-8")
        (self.checkout / "node_modules" / ".bin").mkdir(parents=True, exist_ok=True)
        os.symlink("../left-pad/index.js", self.checkout / "node_modules" / ".bin" / "left-pad")

        self.assertEqual(self._issues(), [])

    def test_graph_selected_untracked_input_and_output_symlink_still_fail(self) -> None:
        (self.checkout / "Directory.Packages.props").write_text(
            "<Project />\n", encoding="utf-8"
        )
        issues = self._issues()
        self.assertTrue(
            any("contains untracked inputs" in issue for issue in issues), issues
        )
        (self.checkout / "Directory.Packages.props").unlink()

        os.symlink(
            self._temporary.name,
            self.checkout / "src" / "Tool" / "bin",
            target_is_directory=True,
        )
        issues = self._issues()
        self.assertTrue(
            any("untracked symlink inputs selected by the" in issue for issue in issues),
            issues,
        )


class DependencyDiagnosticBoundTests(unittest.TestCase):
    """A dependency-wide drift cause emits one bounded diagnostic, not one error per file."""

    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self._temporary.cleanup)
        self.repository = Path(self._temporary.name) / "repository"
        self.dependency = evidence.RUNTIME_DEPENDENCY_GITLINKS[0]
        self.checkout = self.repository / self.dependency
        self.checkout.mkdir(parents=True)
        for directory in (self.repository, self.checkout):
            subprocess.run(["git", "init", "-q"], cwd=directory, check=True)
            subprocess.run(
                ["git", "config", "user.name", "Diagnostic Bound Test"],
                cwd=directory,
                check=True,
            )
            subprocess.run(
                ["git", "config", "user.email", "diagnostics@example.test"],
                cwd=directory,
                check=True,
            )
        self.tracked = [f"src/Input{index:02}.cs" for index in range(
            evidence.MAX_DIAGNOSTIC_PATHS + 5
        )]
        for relative in self.tracked:
            path = self.checkout / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("// sealed\n", encoding="utf-8")
        subprocess.run(["git", "add", "-A"], cwd=self.checkout, check=True)
        subprocess.run(
            ["git", "commit", "-qm", "test: seed dependency inputs"],
            cwd=self.checkout,
            check=True,
        )
        self.head = subprocess.run(
            ["git", "rev-parse", "HEAD"],
            cwd=self.checkout,
            check=True,
            capture_output=True,
            text=True,
        ).stdout.strip()

    def test_worktree_byte_drift_is_reported_once_with_an_omitted_count(self) -> None:
        for relative in self.tracked:
            (self.checkout / relative).write_text("// drifted\n", encoding="utf-8")

        issues = evidence._validate_dependency_checkout(
            self.repository, self.dependency, self.head
        )

        drift = [issue for issue in issues if "worktree bytes differ" in issue]
        self.assertEqual(len(drift), 1, issues)
        self.assertIn("omitted 5 additional path(s)", drift[0])

    def test_a_single_bulk_hash_failure_does_not_emit_one_error_per_file(self) -> None:
        with mock.patch.object(
            evidence,
            "_bulk_worktree_git_objects",
            return_value=({}, "Unable to inspect Git attributes for worktree inputs."),
        ):
            issues = evidence._validate_dependency_checkout(
                self.repository, self.dependency, self.head
            )

        self.assertTrue(
            any("Unable to inspect Git attributes" in issue for issue in issues), issues
        )
        self.assertEqual(
            [issue for issue in issues if "worktree bytes differ" in issue], []
        )


class PactAuthorityExactnessTests(unittest.TestCase):
    """Every committed-pact, manifest, and catalog exactness rule must be executable."""

    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self._temporary.cleanup)
        self.pact_dir = Path(self._temporary.name) / "Pact"
        shutil.copytree(CANONICAL_PACTS, self.pact_dir)
        self.pact_file = evidence.PACT_FILES[0]

    def _errors(self) -> list[str]:
        errors: list[str] = []
        evidence._pact_interactions(self.pact_dir, errors)
        return errors

    def _mutate(self, name: str, mutate: Callable[[dict[str, Any]], None]) -> None:
        path = self.pact_dir / name
        document = _read_json(path)
        mutate(document)
        _write_json(path, document)

    def test_canonical_pacts_are_accepted(self) -> None:
        self.assertEqual(self._errors(), [])

    def test_missing_interaction_metadata_is_actionable_not_a_crash(self) -> None:
        self._mutate(
            self.pact_file,
            lambda pact: pact["interactions"][0]["metadata"].pop("generatedSource"),
        )

        errors = self._errors()

        self.assertTrue(any("lacks generatedSource" in error for error in errors), errors)
        self.assertTrue(
            any("absent from committed pacts" in error for error in errors), errors
        )

    def test_pact_party_specification_and_http_semantics_are_exact(self) -> None:
        cases = (
            (
                lambda pact: pact["provider"].__setitem__("name", "Other.Provider"),
                "unexpected Pact parties or specification",
            ),
            (
                lambda pact: pact["metadata"]["pactSpecification"].__setitem__("version", "3.0"),
                "unexpected Pact parties or specification",
            ),
            (
                lambda pact: pact["interactions"][0]["request"].__setitem__("path", "relative"),
                "incomplete HTTP interaction semantics",
            ),
            (
                lambda pact: pact["interactions"][0].__setitem__("type", "Asynchronous/Messages"),
                "incomplete HTTP interaction semantics",
            ),
            (
                lambda pact: pact["interactions"].append(
                    copy.deepcopy(pact["interactions"][0])
                ),
                "repeat interaction description",
            ),
        )
        original = _read_json(self.pact_dir / self.pact_file)
        for mutate, expected in cases:
            with self.subTest(expected=expected):
                _write_json(self.pact_dir / self.pact_file, copy.deepcopy(original))
                self._mutate(self.pact_file, mutate)

                self.assertTrue(
                    any(expected in error for error in self._errors()), expected
                )
        _write_json(self.pact_dir / self.pact_file, original)

    def test_manifest_and_catalog_authority_rules_are_exact(self) -> None:
        cases = (
            (
                "interaction-manifest.json",
                lambda manifest: manifest.__setitem__("provider", "Other.Provider"),
                "Interaction manifest authority fields are not exact.",
            ),
            (
                "interaction-manifest.json",
                lambda manifest: manifest.__setitem__(
                    "pactFiles", list(reversed(manifest["pactFiles"]))
                ),
                "Interaction manifest pact-file attribution does not match the committed pacts.",
            ),
            (
                "interaction-manifest.json",
                lambda manifest: manifest["interactions"][0].__setitem__("method", "TRACE"),
                "Interaction manifest method differs from the pact",
            ),
            (
                "interaction-manifest.json",
                lambda manifest: manifest.__setitem__("interactionCount", 1),
                "Interaction manifest interactionCount does not match its exact entries.",
            ),
            (
                "provider-state-catalog.json",
                lambda catalog: catalog.__setitem__("forbiddenDependencies", []),
                "Provider-state catalog authority fields are not exact.",
            ),
            (
                "provider-state-catalog.json",
                lambda catalog: catalog["states"][0].__setitem__(
                    "isolatedPerInteraction", False
                ),
                "Provider-state catalog semantics are incomplete for:",
            ),
            (
                "provider-state-catalog.json",
                lambda catalog: catalog["states"][0].__setitem__("name", "unknown-state"),
                "Provider-state catalog set must equal the committed pact interaction states.",
            ),
        )
        for name, mutate, expected in cases:
            with self.subTest(expected=expected):
                original = _read_json(self.pact_dir / name)
                self._mutate(name, mutate)

                errors = self._errors()

                _write_json(self.pact_dir / name, original)
                self.assertTrue(any(expected in error for error in errors), (expected, errors))


class ReviewLoop11RegressionTests(unittest.TestCase):
    def test_relative_paths_must_be_canonical_and_nul_free(self) -> None:
        self.assertTrue(evidence._is_safe_relative_path("a/b.json"))
        for value in ("a//b.json", "a/./b.json", "a/../b.json", "a\\b.json", "a\0b", "."):
            with self.subTest(value=value):
                self.assertFalse(evidence._is_safe_relative_path(value))

    def test_bounded_json_reader_reports_generic_value_errors(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "document.json"
            path.write_text('{"value": 1}\n', encoding="utf-8")
            errors: list[str] = []
            with mock.patch.object(
                evidence.json, "loads", side_effect=ValueError("conversion rejected")
            ):
                document = evidence._read_json(path, errors)

        self.assertEqual(document, {})
        self.assertTrue(any("conversion rejected" in error for error in errors), errors)

    def test_sha256_accepts_the_explicit_ledger_bound(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / evidence.APPHOST_PACKAGE_LEDGER_FILE
            content = b"x" * (evidence.MAX_FILE_BYTES + 1)
            path.write_bytes(content)
            default_errors: list[str] = []
            ledger_errors: list[str] = []

            self.assertEqual(evidence._sha256(path, default_errors), "")
            digest = evidence._sha256(
                path,
                ledger_errors,
                max_bytes=evidence.MAX_PACKAGE_LEDGER_BYTES,
            )

        self.assertTrue(default_errors)
        self.assertEqual(digest, hashlib.sha256(content).hexdigest())
        self.assertEqual(ledger_errors, [])

    def test_manifest_and_ledger_writers_enforce_their_serialized_bounds(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            manifest_output = root / "manifest.json"
            ledger_output = root / "ledger.json"
            oversized = {"payload": "x" * 100}
            with (
                mock.patch.object(
                    evidence,
                    "runtime_input_manifest",
                    return_value=(oversized, []),
                ),
                mock.patch.object(evidence, "MAX_FILE_BYTES", 64),
            ):
                manifest_errors = evidence.write_runtime_input_manifest(
                    manifest_output, root
                )
            with (
                mock.patch.object(
                    evidence,
                    "resolved_package_ledger",
                    return_value=(oversized, []),
                ),
                mock.patch.object(evidence, "MAX_PACKAGE_LEDGER_BYTES", 64),
            ):
                ledger_errors = evidence.write_package_ledger(
                    ledger_output, root, root / "packages", []
                )

            self.assertFalse(manifest_output.exists())
            self.assertFalse(ledger_output.exists())

        self.assertTrue(any("evidence bound" in error for error in manifest_errors))
        self.assertTrue(any("evidence bound" in error for error in ledger_errors))

    def test_runtime_scope_includes_root_compiler_configuration(self) -> None:
        self.assertIn(".editorconfig", evidence.RUNTIME_ROOT_INPUTS)
        self.assertTrue(evidence.ROOT_BUILD_CONTROL_RE.fullmatch("rules.globalconfig"))
        self.assertIn("Compile", evidence.APPHOST_EVALUATED_INPUT_ITEMS)
        self.assertIn("GlobalAnalyzerConfigFiles", evidence.APPHOST_EVALUATED_INPUT_ITEMS)

    def test_runtime_manifest_generation_rejects_a_future_capture(self) -> None:
        future = datetime.now().astimezone() + timedelta(minutes=10)
        with (
            mock.patch.object(evidence, "_runtime_input_snapshot", return_value=([], [])),
            mock.patch.object(evidence, "_git", return_value="a" * 40),
        ):
            _, errors = evidence.runtime_input_manifest(
                ROOT, captured_at=future.isoformat()
            )

        self.assertTrue(any("five-minute clock skew" in error for error in errors), errors)

    def test_runtime_manifest_validation_rejects_a_future_capture(self) -> None:
        future = datetime.now().astimezone() + timedelta(minutes=10)
        document = {
            "schema": "hexalith.frontcomposer.eventstore-runtime-inputs.v1",
            "capturedAt": future.isoformat(),
            "capturedRevision": "a" * 40,
            "scope": evidence._runtime_scope(),
            "treeSha256": evidence._runtime_tree_sha256([]),
            "entries": [],
        }
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            path = root / "manifest.json"
            _write_json(path, document)
            errors: list[str] = []
            with (
                mock.patch.object(evidence, "_git", return_value="a" * 40),
                mock.patch.object(
                    evidence,
                    "_git_completed",
                    return_value=subprocess.CompletedProcess([], 0, b"", b""),
                ),
                mock.patch.object(evidence, "_runtime_git_tree", return_value=({}, [])),
                mock.patch.object(evidence, "_runtime_input_snapshot", return_value=([], [])),
            ):
                evidence._validate_runtime_input_manifest(path, root, errors)

        self.assertTrue(any("five-minute clock skew" in error for error in errors), errors)

    def test_catalog_version_requires_one_supported_msbuild_property(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "Directory.Packages.props"
            path.write_text(
                "<Project><PropertyGroup>"
                "<HexalithEventStoreVersion>1.0.0</HexalithEventStoreVersion>"
                "<HexalithEventStoreVersion>2.0.0</HexalithEventStoreVersion>"
                "</PropertyGroup></Project>",
                encoding="utf-8",
            )
            errors: list[str] = []
            version = evidence._eventstore_catalog_version(path, errors)

        self.assertEqual(version, "")
        self.assertTrue(any("exactly once" in error for error in errors), errors)


class RedactionGrammarTests(unittest.TestCase):
    """Cookie-shaped keys leak; ordinary source paths that contain 'cookie' do not."""

    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self._temporary.cleanup)
        self.root = Path(self._temporary.name)

    def _scan(self, document: dict[str, Any]) -> list[str]:
        path = self.root / "artifact.json"
        _write_json(path, document)
        errors: list[str] = []
        evidence._scan_redaction(path, errors)
        return errors

    def test_cookie_shaped_keys_are_rejected(self) -> None:
        for document in (
            {"cookie": "a=b"},
            {"cookies": ["a=b"]},
            {"cookieHeader": "a=b"},
            {"set-cookie": "a=b"},
            {"note": "cookie=abc"},
        ):
            with self.subTest(document=document):
                self.assertTrue(self._scan(document), document)

    def test_source_paths_containing_cookie_are_not_leaks(self) -> None:
        document = {
            "entries": [
                {
                    "path": "src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthCookieOptions.cs",
                    "kind": "file",
                    "bytes": 10,
                    "sha256": "a" * 64,
                }
            ]
        }

        self.assertEqual(self._scan(document), [])


class LiveProvenanceGuardTests(unittest.TestCase):
    """Exercise the real checkout-versus-gitlink and Builds-catalog guards."""

    CATALOG = (
        "<Project><PropertyGroup>"
        "<HexalithEventStoreVersion>3.103.0</HexalithEventStoreVersion>"
        "</PropertyGroup></Project>\n"
    )

    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self._temporary.cleanup)
        self.repository = Path(self._temporary.name) / "repository"
        self.repository.mkdir(parents=True)
        self._init(self.repository)
        self.checkouts = {
            "references/Hexalith.EventStore": self.repository / "references/Hexalith.EventStore",
            "references/Hexalith.Builds": self.repository / "references/Hexalith.Builds",
        }
        for relative, checkout in self.checkouts.items():
            checkout.mkdir(parents=True)
            self._init(checkout)
            if relative.endswith("EventStore"):
                inventory = checkout / "tools" / "release-packages.json"
                inventory.parent.mkdir(parents=True)
                _write_json(inventory, {"packages": []})
            else:
                catalog = checkout / "Props" / "Directory.Packages.props"
                catalog.parent.mkdir(parents=True)
                catalog.write_text(self.CATALOG, encoding="utf-8")
            subprocess.run(["git", "add", "-A"], cwd=checkout, check=True)
            subprocess.run(["git", "commit", "-qm", "test: seed"], cwd=checkout, check=True)
        self._commit_gitlinks()
        self.manifest = {
            "capturedRevision": self._head(self.repository),
            "treeSha256": "a" * 64,
        }

    def _init(self, directory: Path) -> None:
        subprocess.run(["git", "init", "-q"], cwd=directory, check=True)
        subprocess.run(
            ["git", "config", "user.name", "Live Provenance Test"], cwd=directory, check=True
        )
        subprocess.run(
            ["git", "config", "user.email", "provenance@example.test"],
            cwd=directory,
            check=True,
        )

    def _head(self, checkout: Path) -> str:
        return subprocess.run(
            ["git", "rev-parse", "HEAD"], cwd=checkout, check=True, capture_output=True, text=True
        ).stdout.strip()

    def _commit_gitlinks(self) -> None:
        for relative, checkout in self.checkouts.items():
            subprocess.run(
                [
                    "git", "update-index", "--add", "--cacheinfo",
                    f"160000,{self._head(checkout)},{relative}",
                ],
                cwd=self.repository,
                check=True,
            )
        subprocess.run(
            ["git", "commit", "-qm", "test: pin dependency gitlinks"],
            cwd=self.repository,
            check=True,
        )

    def _advance(self, checkout: Path) -> None:
        (checkout / "drift.txt").write_text("drift\n", encoding="utf-8")
        subprocess.run(["git", "add", "drift.txt"], cwd=checkout, check=True)
        subprocess.run(["git", "commit", "-qm", "test: advance"], cwd=checkout, check=True)

    def _provenance(self) -> tuple[dict[str, str], list[str]]:
        errors: list[str] = []
        return (
            evidence._live_provenance(
                self.repository, errors, runtime_manifest=self.manifest
            ),
            errors,
        )

    def test_pinned_checkouts_resolve_the_exact_current_provenance(self) -> None:
        provenance, errors = self._provenance()

        self.assertEqual(errors, [])
        self.assertEqual(provenance["releaseVersion"], "3.103.0")
        self.assertEqual(
            provenance["sourceSha"], self._head(self.checkouts["references/Hexalith.EventStore"])
        )
        self.assertEqual(provenance["runtimeInputTreeSha256"], "a" * 64)

    def test_source_checkout_ahead_of_its_gitlink_is_rejected(self) -> None:
        self._advance(self.checkouts["references/Hexalith.EventStore"])

        _, errors = self._provenance()

        self.assertIn(
            "Live provider source checkout does not equal the pinned EventStore gitlink.", errors
        )

    def test_builds_checkout_ahead_of_its_gitlink_is_rejected(self) -> None:
        self._advance(self.checkouts["references/Hexalith.Builds"])

        _, errors = self._provenance()

        self.assertIn(
            "Live provider Builds checkout does not equal the pinned Builds gitlink.", errors
        )

    def test_missing_builds_catalog_version_is_rejected(self) -> None:
        catalog = self.checkouts["references/Hexalith.Builds"] / "Props/Directory.Packages.props"
        catalog.write_text("<Project />\n", encoding="utf-8")
        subprocess.run(
            ["git", "commit", "-aqm", "test: drop catalog version"],
            cwd=self.checkouts["references/Hexalith.Builds"],
            check=True,
        )
        self._commit_gitlinks()

        _, errors = self._provenance()

        self.assertIn(
            "Live provider Builds catalog must define HexalithEventStoreVersion exactly once.",
            errors,
        )


class SealedManifestComparisonTests(unittest.TestCase):
    """The sealed manifest is compared with a freshly computed worktree snapshot."""

    MISMATCH = "Current runtime-relevant inputs differ from the sealed manifest."

    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self._temporary.cleanup)
        self.repository = Path(self._temporary.name) / "repository"
        (self.repository / "src").mkdir(parents=True)
        subprocess.run(["git", "init", "-q"], cwd=self.repository, check=True)
        subprocess.run(
            ["git", "config", "user.name", "Sealed Manifest Test"],
            cwd=self.repository,
            check=True,
        )
        subprocess.run(
            ["git", "config", "user.email", "sealed-manifest@example.test"],
            cwd=self.repository,
            check=True,
        )
        self.runtime_file = self.repository / "src" / "Runtime.cs"
        self.runtime_file.write_text("// sealed runtime input\n", encoding="utf-8")
        subprocess.run(["git", "add", "-A"], cwd=self.repository, check=True)
        subprocess.run(
            ["git", "commit", "-qm", "test: seed sealed manifest scope"],
            cwd=self.repository,
            check=True,
        )
        self.manifest_path = Path(self._temporary.name) / "frontcomposer-runtime-inputs.json"
        document, _ = evidence.runtime_input_manifest(self.repository)
        _write_json(self.manifest_path, document)

    def _errors(self) -> list[str]:
        errors: list[str] = []
        evidence._validate_runtime_input_manifest(self.manifest_path, self.repository, errors)
        return errors

    def test_unchanged_worktree_matches_the_sealed_manifest(self) -> None:
        self.assertNotIn(self.MISMATCH, self._errors())

    def test_modified_worktree_bytes_fail_the_sealed_comparison(self) -> None:
        self.runtime_file.write_text("// drifted runtime input\n", encoding="utf-8")

        self.assertIn(self.MISMATCH, self._errors())

    def test_added_runtime_file_fails_the_sealed_comparison(self) -> None:
        added = self.repository / "src" / "Added.cs"
        added.write_text("// added runtime input\n", encoding="utf-8")
        subprocess.run(["git", "add", "-A"], cwd=self.repository, check=True)
        subprocess.run(
            ["git", "commit", "-qm", "test: add runtime input"],
            cwd=self.repository,
            check=True,
        )

        self.assertIn(self.MISMATCH, self._errors())


class PreservedPacketRejectionTests(unittest.TestCase):
    """The preserved package-less packet has no exemption left anywhere."""

    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self._temporary.cleanup)
        self.preserved = Path(self._temporary.name) / "preserved"
        shutil.copytree(CANONICAL_ACTIVE_EVIDENCE / "recapture", self.preserved)
        self.provenance = {
            "sourceSha": evidence.ACTIVE_SOURCE_SHA,
            "releaseVersion": evidence.ACTIVE_VERSION,
            "buildsSha": evidence.ACTIVE_BUILDS_SHA,
            "releaseInventorySha256": evidence.INVENTORY_SHA256,
            "frontComposerRevision": "1" * 40,
            "runtimeInputTreeSha256": "2" * 64,
        }

    def test_preserved_apphost_packet_requires_package_provenance(self) -> None:
        errors: list[str] = []
        evidence._validate_live_apphost(self.preserved, ROOT, self.provenance, errors)

        self.assertTrue(
            any("does not contain the exact required fields" in error for error in errors),
            errors,
        )
        self.assertTrue(any("unexpected schema" in error for error in errors), errors)

    def test_preserved_provider_receipt_requires_its_ledger_sidecar(self) -> None:
        errors: list[str] = []
        evidence._validate_live_provider(
            self.preserved, CANONICAL_PACTS, self.provenance, errors, repository_root=ROOT
        )

        self.assertTrue(
            any(
                "Live provider run receipt does not contain the exact required fields" in error
                for error in errors
            ),
            errors,
        )

    def test_no_content_hash_exemption_remains_in_the_validator(self) -> None:
        source = (ROOT / "eng/eventstore_runtime_evidence.py").read_text(encoding="utf-8")

        self.assertNotIn("frozen_legacy", source)
        self.assertNotIn("FROZEN_APPHOST_SMOKE_SHA256", source)
        self.assertNotIn("FROZEN_PROVIDER_RECEIPT_SHA256", source)


if __name__ == "__main__":
    unittest.main()
