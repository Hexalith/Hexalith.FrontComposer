#!/usr/bin/env python3
"""Focused GOV-1 manifest-v2 preparation and verification fixtures."""

from __future__ import annotations

import copy
import datetime as dt
import hashlib
import importlib.util
import json
import pathlib
import subprocess
import tempfile
import unittest
import zipfile


REPOSITORY_ROOT = pathlib.Path(__file__).resolve().parents[2]


def _load_helper():
    spec = importlib.util.spec_from_file_location(
        "frontcomposer_release_evidence_v2_tests",
        REPOSITORY_ROOT / "eng" / "release_evidence.py",
    )
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


HELPER = _load_helper()
ROOT_IDENTITY = "github.com/hexalith/hexalith.frontcomposer"
CI_CALLER = {
    "repository": ROOT_IDENTITY,
    "workflow_path": ".github/workflows/ci.yml",
    "commit": "a" * 40,
    "blob_sha256": "1" * 64,
}
CI_REUSABLE = {
    "repository": "github.com/hexalith/hexalith.builds",
    "workflow_path": ".github/workflows/domain-ci.yml",
    "commit": "b" * 40,
    "blob_sha256": "2" * 64,
}
RELEASE_CALLER = {
    "repository": ROOT_IDENTITY,
    "workflow_path": ".github/workflows/release.yml",
    "commit": "c" * 40,
    "blob_sha256": "3" * 64,
}
RELEASE_REUSABLE = {
    "repository": "github.com/hexalith/hexalith.builds",
    "workflow_path": ".github/workflows/domain-release.yml",
    "commit": "d" * 40,
    "blob_sha256": "4" * 64,
}


def _evaluator(caller: dict[str, object], reusable: dict[str, object]) -> dict[str, object]:
    material: dict[str, object] = {
        "caller": caller,
        "reusable": reusable,
        "actions": [],
    }
    return {**material, "definition_digest": HELPER.canonical_sha256(material)}


CI_EVALUATOR = _evaluator(CI_CALLER, CI_REUSABLE)
RELEASE_EVALUATOR = _evaluator(RELEASE_CALLER, RELEASE_REUSABLE)


def _run(*args: str, cwd: pathlib.Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [*args],
        cwd=cwd,
        check=False,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )


def _write_json(path: pathlib.Path, value: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


class ReleaseEvidenceV2Tests(unittest.TestCase):
    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self._temporary.name)
        self._git("init", "-q")
        self._git("config", "user.email", "manifest-v2@example.test")
        self._git("config", "user.name", "Manifest V2 Fixture")

        policy = {
            "schema": HELPER.POLICY_SCHEMA,
            "builds_identity": "github.com/hexalith/hexalith.builds",
            "trusted_identities": [
                {"identity": ROOT_IDENTITY, "local_path": "."},
                {"identity": "github.com/hexalith/hexalith.builds", "local_path": "references/Hexalith.Builds"},
            ],
            "semantic_profiles": {
                ROOT_IDENTITY: "fixture-profile",
                "github.com/hexalith/hexalith.builds": "fixture-profile",
            },
            "profiles": {"fixture-profile": {}},
            "module_build_registry": {
                ROOT_IDENTITY: {
                    "disposition": "evidence-only",
                    "solution": None,
                    "builds_contract_source": "none",
                    "restore_argv": None,
                    "build_argv": None,
                },
                "github.com/hexalith/hexalith.builds": {
                    "disposition": "evidence-only",
                    "solution": None,
                    "builds_contract_source": "none",
                    "restore_argv": None,
                    "build_argv": None,
                },
            },
            "resource_limits": {
                "max_edges": 4096,
                "max_ls_tree_bytes_per_owner_commit": 67108864,
                "max_gitmodules_blob_bytes": 1048576,
                "max_catalog_blob_bytes": 4194304,
                "max_contract_tree_files": 16384,
                "max_contract_tree_blob_bytes": 16777216,
                "max_contract_tree_total_bytes": 268435456,
                "max_workflow_closure_depth": 16,
                "max_workflow_closure_sources": 256,
                "max_workflow_source_blob_bytes": 1048576,
                "max_workflow_source_total_bytes": 16777216,
            },
            "evaluator_authorizations": {
                "ci": [HELPER._authorization_projection(CI_EVALUATOR, "ci")],
                "release": [HELPER._authorization_projection(RELEASE_EVALUATOR, "release")],
                "post_release": [],
            },
        }
        _write_json(self.root / HELPER.POLICY_PATH, policy)
        _write_json(self.root / "eng/release-package-inventory.json", {"packages": []})
        self._git("add", HELPER.POLICY_PATH, "eng/release-package-inventory.json")
        self._git("commit", "-q", "-m", "test: seed manifest v2 policy")
        self.base = self._git("rev-parse", "HEAD").stdout.strip()
        self.policy_sha256 = hashlib.sha256(
            self._git("show", f"{self.base}:{HELPER.POLICY_PATH}").stdout.encode("utf-8")
        ).hexdigest()
        for relative in HELPER.RELEASE_DEFINITION_FILES:
            path = self.root / relative
            if not path.exists():
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(f"fixture for {relative}\n", encoding="utf-8")
        self.builds_root = self.root / "references/Hexalith.Builds"
        builds_workflow = self.builds_root / ".github/workflows/domain-release.yml"
        builds_workflow.parent.mkdir(parents=True, exist_ok=True)
        builds_workflow.write_text("name: fixture domain release\n", encoding="utf-8")
        for command in (
            ("init", "-q"),
            ("config", "user.email", "manifest-v3-builds@example.test"),
            ("config", "user.name", "Manifest V3 Builds Fixture"),
            ("add", "."),
            ("commit", "-q", "-m", "test: seed Builds release workflow"),
        ):
            result = _run("git", *command, cwd=self.builds_root)
            self.assertEqual(0, result.returncode, result.stderr)
        self.builds_commit = _run("git", "rev-parse", "HEAD", cwd=self.builds_root).stdout.strip()
        (self.root / ".gitmodules").write_text(
            '[submodule "references/Hexalith.Builds"]\n'
            "\tpath = references/Hexalith.Builds\n"
            "\turl = https://github.com/Hexalith/Hexalith.Builds.git\n",
            encoding="utf-8",
        )
        self._git("add", ".")
        self._git("commit", "-q", "-m", "test: stage exact manifest candidate")
        self.commit = self._git("rev-parse", "HEAD").stdout.strip()

        engine = HELPER._load_dependency_graph_engine()
        loaded_policy = json.loads((self.root / HELPER.POLICY_PATH).read_text(encoding="utf-8"))
        self.graph = engine.collect_graph(self.root, ROOT_IDENTITY, self.commit, loaded_policy)
        self.policy_projection = {
            "schema": HELPER.POLICY_SCHEMA,
            "repository": ROOT_IDENTITY,
            "path": HELPER.POLICY_PATH,
            "commit": self.base,
            "sha256": self.policy_sha256,
        }
        self.ci_evaluator = copy.deepcopy(CI_EVALUATOR)
        self.ci_evaluator["caller"]["commit"] = self.commit
        self.ci_evaluator["definition_digest"] = HELPER.canonical_sha256(
            {key: self.ci_evaluator[key] for key in ("caller", "reusable", "actions")}
        )
        self.handoff = {
            "schema": HELPER.CI_HANDOFF_SCHEMA,
            "run": {
                "repository": ROOT_IDENTITY,
                "workflow_path": HELPER.CI_WORKFLOW_PATH,
                "run_id": 42,
                "run_attempt": 1,
                "event": "push",
                "branch": "main",
                "candidate": self.commit,
            },
            "revisions": {"base": self.base, "candidate": self.commit, "merge_base": None},
            "evaluator": self.ci_evaluator,
            "dependency_policy": self.policy_projection,
            "dependency_graph": self.graph,
        }

    def tearDown(self) -> None:
        self._temporary.cleanup()

    def _git(self, *args: str) -> subprocess.CompletedProcess[str]:
        result = _run("git", *args, cwd=self.root)
        self.assertEqual(0, result.returncode, result.stderr)
        return result

    def _advance_builds_catalog_gitlink(self) -> str:
        catalog = self.builds_root / "Props/Directory.Packages.props"
        catalog.parent.mkdir(parents=True, exist_ok=True)
        catalog.write_text("<Project><!-- fixture catalog-only advance --></Project>\n", encoding="utf-8")
        catalog_workflow = self.builds_root / ".github/workflows/domain-release.yml"
        catalog_workflow.write_text("name: fixture catalog release bytes\n", encoding="utf-8")
        self.assertEqual(0, _run("git", "add", ".", cwd=self.builds_root).returncode)
        self.assertEqual(
            0,
            _run("git", "commit", "-q", "-m", "test: catalog-only Builds commit", cwd=self.builds_root).returncode,
        )
        catalog_sha = _run("git", "rev-parse", "HEAD", cwd=self.builds_root).stdout.strip()
        self.assertNotEqual(self.builds_commit, catalog_sha)
        self._git("add", "references/Hexalith.Builds")
        self._git("commit", "-q", "-m", "test: advance Builds catalog gitlink")
        self.commit = self._git("rev-parse", "HEAD").stdout.strip()
        engine = HELPER._load_dependency_graph_engine()
        loaded_policy = json.loads((self.root / HELPER.POLICY_PATH).read_text(encoding="utf-8"))
        self.graph = engine.collect_graph(self.root, ROOT_IDENTITY, self.commit, loaded_policy)
        self.ci_evaluator["caller"]["commit"] = self.commit
        self.ci_evaluator["definition_digest"] = HELPER.canonical_sha256(
            {key: self.ci_evaluator[key] for key in ("caller", "reusable", "actions")}
        )
        self.handoff["run"]["candidate"] = self.commit
        self.handoff["revisions"]["candidate"] = self.commit
        self.handoff["evaluator"] = self.ci_evaluator
        self.handoff["dependency_graph"] = self.graph
        return catalog_sha

    def _create_builds_execution_checkout(self) -> tuple[str, bytes]:
        execution_root = self.root / ".hexalith/builds-execution"
        workflow = execution_root / ".github/workflows/domain-release.yml"
        workflow.parent.mkdir(parents=True, exist_ok=True)
        execution_bytes = b"name: fixture exact execution release bytes\n"
        workflow.write_bytes(execution_bytes)
        for command in (
            ("init", "-q"),
            ("config", "user.email", "manifest-v3-execution@example.test"),
            ("config", "user.name", "Manifest V3 Execution Fixture"),
            ("add", "."),
            ("commit", "-q", "-m", "test: seed exact Builds execution checkout"),
        ):
            result = _run("git", *command, cwd=execution_root)
            self.assertEqual(0, result.returncode, result.stderr)
        execution_sha = _run("git", "rev-parse", "HEAD", cwd=execution_root).stdout.strip()
        self.assertNotEqual(0, _run("git", "cat-file", "-e", execution_sha, cwd=self.builds_root).returncode)
        return execution_sha, execution_bytes
    def _prepare(self) -> tuple[pathlib.Path, pathlib.Path]:
        package_id = "Hexalith.FrontComposer.Contracts"
        version = "2.0.0"
        signed = self.root / f"nupkgs-signed/{package_id}.{version}.nupkg"
        symbol = self.root / f"nupkgs/{package_id}.{version}.snupkg"
        sbom = self.root / "release-evidence/sbom.json"
        for path, data in (
            (signed, b"signed package"),
            (symbol, b"symbol package"),
            (sbom, b"{}"),
        ):
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
        handoff = self.root / "release-evidence/dependency-release-handoff.json"
        _write_json(handoff, self.handoff)
        pre_manifest = self.root / "release-evidence/pre-manifest.json"
        handoff_hash = hashlib.sha256(handoff.read_bytes()).hexdigest()
        _write_json(
            pre_manifest,
            {
                "manifest_schema": HELPER.MANIFEST_SCHEMA,
                "commit_sha": self.commit,
                "tag": f"v{version}",
                "run_id": "42",
                "workflow_ref": "Hexalith/Hexalith.FrontComposer/.github/workflows/release.yml@refs/heads/main",
                "sbom_hash": hashlib.sha256(sbom.read_bytes()).hexdigest(),
                "benchmark_summary_hash": "f" * 64,
                "packages": [
                    {
                        "package_id": package_id,
                        "version": version,
                        "commit_sha": self.commit,
                        "artifact_path": str(signed.relative_to(self.root)),
                        "checksum": hashlib.sha256(signed.read_bytes()).hexdigest(),
                        "symbol_artifact": str(symbol.relative_to(self.root)),
                        "symbol_checksum": hashlib.sha256(symbol.read_bytes()).hexdigest(),
                        "sbom_component": package_id,
                        "signing_status": "verified",
                        "timestamp_status": "verified",
                        "attestation_status": "approved-unsupported",
                        "publish_status": "pending",
                    }
                ],
                "release_definition_fingerprints": HELPER.release_definition_fingerprints(self.root),
                "package_set_fingerprint": HELPER.package_set_fingerprint(self.root),
                "helper_version": HELPER.helper_version_record(),
                "dependency_graph": self.graph,
                "dependency_policy": self.policy_projection,
                "workflow_provenance": HELPER._workflow_provenance(
                    self.handoff,
                    handoff_hash,
                    RELEASE_EVALUATOR,
                ),
            },
        )
        sealed_manifest = self.root / "release-evidence/sealed-manifest.json"
        seal = _run(
            "python3",
            str(REPOSITORY_ROOT / "eng/release_evidence.py"),
            "seal-manifest",
            "--manifest",
            str(pre_manifest),
            "--output",
            str(sealed_manifest),
            cwd=REPOSITORY_ROOT,
        )
        self.assertEqual(0, seal.returncode, seal.stdout + seal.stderr)
        return pre_manifest, sealed_manifest

    def test_prepare_seal_and_offline_live_verify_round_trip(self) -> None:
        _, sealed = self._prepare()
        payload = json.loads(sealed.read_text(encoding="utf-8"))
        self.assertEqual(HELPER.MANIFEST_SCHEMA, payload["manifest_schema"])
        self.assertEqual(self.graph, payload["dependency_graph"])
        self.assertEqual(self.policy_projection, payload["dependency_policy"])
        handoff_hash = hashlib.sha256(
            (self.root / "release-evidence/dependency-release-handoff.json").read_bytes()
        ).hexdigest()
        self.assertEqual(handoff_hash, payload["workflow_provenance"]["ci"]["evidence_sha256"])

        for mode in (("--no-root",), ("--root", str(self.root), "--graph-root", str(self.root))):
            result = _run(
                "python3",
                str(REPOSITORY_ROOT / "eng/release_evidence.py"),
                "verify-manifest",
                "--manifest",
                str(sealed),
                *mode,
                cwd=REPOSITORY_ROOT,
            )
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_historical_v2_cannot_be_reinterpreted_as_unsigned_current_evidence(self) -> None:
        _, sealed = self._prepare()
        payload = json.loads(sealed.read_text(encoding="utf-8"))
        payload.pop("seal")
        row = payload["packages"][0]
        row["artifact_path"] = row["artifact_path"].replace("nupkgs-signed/", "nupkgs/")
        row.pop("signing_status")
        row.pop("timestamp_status")
        payload["seal"] = {
            "algorithm": "sha256",
            "hash": HELPER.canonical_sha256(payload),
            "sealed_at": "2026-08-04T00:00:00+00:00",
        }
        malformed = self.root / "release-evidence/malformed-legacy-v2.json"
        _write_json(malformed, payload)
        result = _run(
            "python3",
            str(REPOSITORY_ROOT / "eng/release_evidence.py"),
            "verify-manifest",
            "--manifest", str(malformed),
            "--no-root",
            cwd=REPOSITORY_ROOT,
        )
        self.assertEqual(1, result.returncode)
        self.assertIn("signing_status", result.stdout)

    def test_current_prepare_uses_unsigned_candidate_without_author_signing_input(self) -> None:
        catalog_sha = self._advance_builds_catalog_gitlink()
        execution_sha, execution_bytes = self._create_builds_execution_checkout()
        catalog_workflow_bytes = _run(
            "git",
            "show",
            f"{catalog_sha}:.github/workflows/domain-release.yml",
            cwd=self.builds_root,
        ).stdout.encode("utf-8")
        self.assertNotEqual(execution_bytes, catalog_workflow_bytes)
        package_id = "Hexalith.FrontComposer.Contracts"
        version = "2.0.0"
        package = self.root / f"nupkgs/{package_id}.{version}.nupkg"
        symbol = self.root / f"nupkgs/{package_id}.{version}.snupkg"
        sbom = self.root / "release-evidence/sbom.json"
        package.parent.mkdir(parents=True, exist_ok=True)
        with zipfile.ZipFile(package, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            archive.writestr(f"{package_id}.nuspec", b"<package />")
            archive.writestr(f"lib/net10.0/{package_id}.dll", b"unsigned candidate")
        for path, content in ((symbol, b"symbols"), (sbom, b"{}")):
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(content)
        inventory = self.root / "release-evidence/package-inventory.json"
        checksums = self.root / "release-evidence/checksums.json"
        output = self.root / "release-evidence/current-pre-manifest.json"
        diagnostics = self.root / "release-evidence/current-diagnostics.json"
        _write_json(inventory, {"rows": [{
            "package_id": package_id,
            "packable": True,
            "symbol_required": True,
            "exception": "not-required",
        }]})
        _write_json(checksums, {"files": [
            {"path": str(package.relative_to(self.root)), "sha256": hashlib.sha256(package.read_bytes()).hexdigest()},
            {"path": str(symbol.relative_to(self.root)), "sha256": hashlib.sha256(symbol.read_bytes()).hexdigest()},
            {"path": str(sbom.relative_to(self.root)), "sha256": hashlib.sha256(sbom.read_bytes()).hexdigest()},
        ]})
        source_proof = self.root / "release-evidence/dependency-release-source.json"
        _write_json(source_proof, {
            "schema": HELPER._load_dependency_handoff_engine().SOURCE_PROOF_SCHEMA,
            "run": {
                "repository": ROOT_IDENTITY,
                "workflow_path": HELPER.CI_WORKFLOW_PATH,
                "run_id": 42,
                "run_attempt": 1,
                "event": "push",
                "branch": "main",
                "candidate": self.commit,
            },
            "revisions": {"base": self.base, "candidate": self.commit},
            "dependency_policy": self.policy_projection,
            "dependency_graph": self.graph,
        })

        result = _run(
            "python3",
            str(REPOSITORY_ROOT / "eng/release_evidence.py"),
            "prepare-manifest",
            "--inventory", str(inventory),
            "--checksums", str(checksums),
            "--output", str(output),
            "--diagnostics-output", str(diagnostics),
            "--version", version,
            "--root", str(self.root),
            "--graph-root", str(self.root),
            "--commit-sha", self.commit,
            "--source-proof", str(source_proof),
            "--builds-execution-sha", execution_sha,
            "--tag", f"v{version}",
            "--run-id", "42",
            "--workflow-ref", "Hexalith/Hexalith.FrontComposer/.github/workflows/release.yml@refs/heads/main",
            "--sbom-hash", hashlib.sha256(sbom.read_bytes()).hexdigest(),
            "--benchmark-summary-hash", "f" * 64,
            cwd=REPOSITORY_ROOT,
        )

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        payload = json.loads(output.read_text(encoding="utf-8"))
        catalog_edges = [
            edge for edge in payload["dependency_graph"]["edges"]
            if edge["depth"] == 1 and edge["path"] == "references/Hexalith.Builds"
        ]
        self.assertEqual([catalog_sha], [edge["commit"] for edge in catalog_edges])
        release_provenance = payload["workflow_provenance"]["release"]
        self.assertEqual(execution_sha, release_provenance["builds_execution_sha"])
        self.assertEqual(execution_sha, release_provenance["reusable"]["commit"])
        self.assertEqual(hashlib.sha256(execution_bytes).hexdigest(), release_provenance["reusable"]["blob_sha256"])
        self.assertNotEqual(
            hashlib.sha256(catalog_workflow_bytes).hexdigest(),
            release_provenance["reusable"]["blob_sha256"],
        )
        self.assertNotEqual(catalog_sha, execution_sha)
        self.assertEqual(HELPER.CURRENT_MANIFEST_SCHEMA, payload["manifest_schema"])
        self.assertEqual(f"nupkgs/{package_id}.{version}.nupkg", payload["packages"][0]["artifact_path"])
        self.assertNotIn("signing_status", payload["packages"][0])
        self.assertNotIn("timestamp_status", payload["packages"][0])
        self.assertEqual(hashlib.sha256(package.read_bytes()).hexdigest(), payload["packages"][0]["checksum"])
        self.assertFalse(diagnostics.exists())

        sealed = self.root / "release-evidence/current-sealed-manifest.json"
        seal = _run(
            "python3",
            str(REPOSITORY_ROOT / "eng/release_evidence.py"),
            "seal-manifest",
            "--manifest", str(output),
            "--output", str(sealed),
            cwd=REPOSITORY_ROOT,
        )
        self.assertEqual(0, seal.returncode, seal.stdout + seal.stderr)
        offline = _run(
            "python3",
            str(REPOSITORY_ROOT / "eng/release_evidence.py"),
            "verify-manifest",
            "--manifest", str(sealed),
            "--no-root",
            cwd=REPOSITORY_ROOT,
        )
        self.assertEqual(0, offline.returncode, offline.stdout + offline.stderr)
        live = _run(
            "python3",
            str(REPOSITORY_ROOT / "eng/release_evidence.py"),
            "verify-manifest",
            "--manifest", str(sealed),
            "--root", str(self.root),
            "--graph-root", str(self.root),
            cwd=REPOSITORY_ROOT,
        )
        self.assertEqual(0, live.returncode, live.stdout + live.stderr)
        self.assertNotIn("cannot resolve the candidate Builds gitlink", live.stdout + live.stderr)

    def test_offline_accepts_structural_graph_but_live_rejects_exact_graph_drift(self) -> None:
        _, sealed = self._prepare()
        payload = json.loads(sealed.read_text(encoding="utf-8"))
        edge = {
            "owner_repository": ROOT_IDENTITY,
            "owner_commit": self.commit,
            "path": "references/Fake",
            "repository": "github.com/hexalith/fake",
            "commit": "9" * 40,
            "depth": 1,
        }
        graph = payload["dependency_graph"]
        graph["edges"] = [edge]
        graph["edge_count"] = 1
        graph["graph_digest"] = HELPER.canonical_sha256(
            {key: graph[key] for key in ("schema", "root", "edge_count", "edges")}
        )
        payload.pop("seal")
        unsealed = self.root / "release-evidence/drifted-pre-manifest.json"
        drifted = self.root / "release-evidence/drifted-sealed-manifest.json"
        _write_json(unsealed, payload)
        seal = _run(
            "python3",
            str(REPOSITORY_ROOT / "eng/release_evidence.py"),
            "seal-manifest",
            "--manifest",
            str(unsealed),
            "--output",
            str(drifted),
            cwd=REPOSITORY_ROOT,
        )
        self.assertEqual(0, seal.returncode, seal.stdout + seal.stderr)
        offline = _run(
            "python3",
            str(REPOSITORY_ROOT / "eng/release_evidence.py"),
            "verify-manifest",
            "--manifest",
            str(drifted),
            "--no-root",
            cwd=REPOSITORY_ROOT,
        )
        self.assertEqual(0, offline.returncode, offline.stdout + offline.stderr)
        live = _run(
            "python3",
            str(REPOSITORY_ROOT / "eng/release_evidence.py"),
            "verify-manifest",
            "--manifest",
            str(drifted),
            "--root",
            str(self.root),
            "--graph-root",
            str(self.root),
            cwd=REPOSITORY_ROOT,
        )
        self.assertEqual(1, live.returncode)
        self.assertIn("dependency-graph drift", live.stdout)

    def test_v3_source_provenance_binds_exact_ci_and_builds_identity(self) -> None:
        _, sealed = self._prepare()
        payload = json.loads(sealed.read_text(encoding="utf-8"))
        payload.pop("seal")
        builds_sha = "d" * 40
        ci = {
            "run": {
                "repository": ROOT_IDENTITY,
                "workflow_path": ".github/workflows/ci.yml",
                "run_id": 42,
                "run_attempt": 1,
                "event": "push",
                "branch": "main",
                "head_sha": self.commit,
            },
            "evidence_sha256": "e" * 64,
        }
        release = {
            "caller": {
                "repository": ROOT_IDENTITY,
                "workflow_path": ".github/workflows/release.yml",
                "commit": self.commit,
                "blob_sha256": "1" * 64,
            },
            "reusable": {
                "repository": "github.com/hexalith/hexalith.builds",
                "workflow_path": ".github/workflows/domain-release.yml",
                "commit": builds_sha,
                "blob_sha256": "2" * 64,
            },
            "builds_execution_sha": builds_sha,
        }
        payload["manifest_schema"] = HELPER.CURRENT_MANIFEST_SCHEMA
        for package in payload["packages"]:
            package["artifact_path"] = package["artifact_path"].replace("nupkgs-signed/", "nupkgs/")
            package.pop("signing_status")
            package.pop("timestamp_status")
        payload["workflow_provenance"] = {
            "ci": ci,
            "release": release,
            "definition_digest": HELPER.canonical_sha256({"ci": ci, "release": release}),
        }
        self.assertEqual([], HELPER._manifest_v2_diagnostics(payload, require_seal=False))
        nested = copy.deepcopy(payload)
        nested["packages"][0]["artifact_path"] = nested["packages"][0]["artifact_path"].replace(
            "nupkgs/", "nupkgs/nested/",
        )
        nested_diagnostics = HELPER.manifest_diagnostics(nested, enforce_v2=True)
        self.assertTrue(any("normalized nupkgs/*.nupkg" in item for item in nested_diagnostics), nested_diagnostics)
        drifted = copy.deepcopy(payload)
        drifted["workflow_provenance"]["release"]["builds_execution_sha"] = "f" * 40
        diagnostics = HELPER._manifest_v2_diagnostics(drifted, require_seal=False)
        self.assertTrue(any("Builds identity mismatch" in item for item in diagnostics), diagnostics)

    def test_source_provenance_accepts_catalog_gitlink_distinct_from_execution_sha(self) -> None:
        catalog_sha = self._advance_builds_catalog_gitlink()
        proof = {"run": self.handoff["run"]}
        provenance = HELPER._source_workflow_provenance(proof, "e" * 64, self.root, self.builds_commit)
        execution_bytes = _run(
            "git",
            "show",
            f"{self.builds_commit}:.github/workflows/domain-release.yml",
            cwd=self.builds_root,
        ).stdout.encode("utf-8")
        catalog_bytes = _run(
            "git",
            "show",
            f"{catalog_sha}:.github/workflows/domain-release.yml",
            cwd=self.builds_root,
        ).stdout.encode("utf-8")
        self.assertNotEqual(catalog_sha, self.builds_commit)
        self.assertNotEqual(execution_bytes, catalog_bytes)
        self.assertEqual(self.builds_commit, provenance["release"]["builds_execution_sha"])
        self.assertEqual(self.builds_commit, provenance["release"]["reusable"]["commit"])
        self.assertEqual(
            hashlib.sha256(execution_bytes).hexdigest(),
            provenance["release"]["reusable"]["blob_sha256"],
        )

    def test_current_provenance_projects_authenticated_handoff_into_v3_shape(self) -> None:
        caller_bytes = self._git("show", f"{self.commit}:.github/workflows/release.yml").stdout.encode("utf-8")
        reusable_bytes = _run(
            "git",
            "show",
            f"{self.builds_commit}:.github/workflows/domain-release.yml",
            cwd=self.builds_root,
        ).stdout.encode("utf-8")
        evaluator = _evaluator(
            {
                "repository": ROOT_IDENTITY,
                "workflow_path": ".github/workflows/release.yml",
                "commit": self.commit,
                "blob_sha256": hashlib.sha256(caller_bytes).hexdigest(),
            },
            {
                "repository": "github.com/hexalith/hexalith.builds",
                "workflow_path": ".github/workflows/domain-release.yml",
                "commit": self.builds_commit,
                "blob_sha256": hashlib.sha256(reusable_bytes).hexdigest(),
            },
        )

        provenance = HELPER._current_workflow_provenance(
            self.handoff,
            "e" * 64,
            evaluator,
            self.root,
            self.builds_commit,
        )

        diagnostics: list[str] = []
        HELPER._validate_source_workflow_provenance(provenance, diagnostics)
        self.assertEqual([], diagnostics)
        self.assertEqual(
            {"run", "evidence_sha256"},
            set(provenance["ci"]),
        )
        self.assertEqual(
            {"caller", "reusable", "builds_execution_sha"},
            set(provenance["release"]),
        )
        evidence = self.root / "release-evidence/dependency-release-source.json"
        _write_json(evidence, self.handoff)
        manifest = {
            "manifest_schema": HELPER.CURRENT_MANIFEST_SCHEMA,
            "commit_sha": self.commit,
            "dependency_graph": self.graph,
            "dependency_policy": self.policy_projection,
            "workflow_provenance": HELPER._current_workflow_provenance(
                self.handoff,
                hashlib.sha256(evidence.read_bytes()).hexdigest(),
                evaluator,
                self.root,
                self.builds_commit,
            ),
        }
        self.assertEqual([], HELPER._live_manifest_v2_diagnostics(manifest, self.root))

        mismatched = copy.deepcopy(evaluator)
        mismatched["caller"]["blob_sha256"] = "f" * 64
        with self.assertRaisesRegex(ValueError, "caller differs"):
            HELPER._current_workflow_provenance(
                self.handoff,
                "e" * 64,
                mismatched,
                self.root,
                self.builds_commit,
            )

    def test_source_provenance_rejects_unresolvable_execution_commit(self) -> None:
        proof = {"run": self.handoff["run"]}
        with self.assertRaisesRegex(ValueError, "cannot resolve exact Builds execution commit"):
            HELPER._source_workflow_provenance(proof, "e" * 64, self.root, "f" * 40)

    def test_source_provenance_rejects_missing_builds_gitlink(self) -> None:
        proof = {"run": {**self.handoff["run"], "candidate": self.base}}
        with self.assertRaisesRegex(ValueError, "cannot resolve the candidate Builds gitlink"):
            HELPER._source_workflow_provenance(proof, "e" * 64, self.root, self.builds_commit)

    def test_sealed_but_unapproved_release_evaluator_fails_preparation(self) -> None:
        unauthorized = copy.deepcopy(RELEASE_EVALUATOR)
        unauthorized["caller"]["blob_sha256"] = "8" * 64
        material = {key: unauthorized[key] for key in ("caller", "reusable", "actions")}
        unauthorized["definition_digest"] = HELPER.canonical_sha256(material)
        release_evaluator = self.root / "release-evidence/unauthorized-release-evaluator.json"
        _write_json(release_evaluator, unauthorized)
        handoff = self.root / "release-evidence/dependency-release-handoff.json"
        _write_json(handoff, self.handoff)

        diagnostics: list[str] = []
        _, parsed = HELPER._read_bounded_strict_json(release_evaluator, "Release evaluator", max_bytes=HELPER.MAX_HANDOFF_BYTES)
        evaluator = HELPER._validate_evaluator(parsed, diagnostics, "Release evaluator")
        self.assertEqual([], diagnostics)
        policy = json.loads((self.root / HELPER.POLICY_PATH).read_text(encoding="utf-8"))
        self.assertIsNotNone(evaluator)
        failures = HELPER._evaluator_authorization_diagnostics(policy, evaluator, "release")
        self.assertEqual(1, len(failures))
        self.assertIn("exactly one active-policy authorization", failures[0])

    def test_v2_fallback_digest_is_invalidated_by_graph_policy_or_workflow_drift(self) -> None:
        _, sealed = self._prepare()
        manifest = json.loads(sealed.read_text(encoding="utf-8"))
        evidence = self.root / "release-evidence/attestation-unavailable.md"
        evidence.write_text("approved fixture evidence\n", encoding="utf-8")
        fingerprints = HELPER.fallback_invalidation_fingerprints(self.root)
        package_set = manifest["package_set_fingerprint"]
        graph_digest = manifest["dependency_graph"]["graph_digest"]
        policy_sha256 = manifest["dependency_policy"]["sha256"]
        workflow_digest = manifest["workflow_provenance"]["definition_digest"]
        approved_digest = HELPER.canonical_sha256(
            {
                "definition": fingerprints,
                "package_set": package_set,
                "dependency_graph": graph_digest,
                "dependency_policy": policy_sha256,
                "workflow_definition": workflow_digest,
            }
        )
        now = dt.datetime.now(dt.timezone.utc)
        fallback = {
            "affected_artifact": "release package set",
            "approved_at": (now - dt.timedelta(minutes=1)).isoformat(),
            "approver": "release-owner",
            "evidence": evidence.name,
            "expires_at": (now + dt.timedelta(days=1)).isoformat(),
            "reason": "fixture",
            "release_note_impact": "fixture",
            "reopen_event": "fixture",
            "scope": "fixture",
            "approved_against_fingerprints_sha256": approved_digest,
        }
        complete, diagnostic = HELPER.fallback_complete(
            fallback,
            fingerprints,
            evidence_root=evidence.parent,
            package_set=package_set,
            dependency_graph_digest=graph_digest,
            dependency_policy_sha256=policy_sha256,
            workflow_definition_digest=workflow_digest,
        )
        self.assertTrue(complete, diagnostic)
        for changed in ("dependency_graph_digest", "dependency_policy_sha256", "workflow_definition_digest"):
            values = {
                "dependency_graph_digest": graph_digest,
                "dependency_policy_sha256": policy_sha256,
                "workflow_definition_digest": workflow_digest,
            }
            values[changed] = "0" * 64
            complete, diagnostic = HELPER.fallback_complete(
                fallback,
                fingerprints,
                evidence_root=evidence.parent,
                package_set=package_set,
                **values,
            )
            self.assertFalse(complete, changed)
            self.assertIn("drifted release definition", diagnostic or "")

    def test_duplicate_unknown_and_legacy_evidence_fail_closed(self) -> None:
        _, sealed = self._prepare()
        payload = json.loads(sealed.read_text(encoding="utf-8"))
        payload["unknown"] = True
        unknown = self.root / "release-evidence/unknown.json"
        _write_json(unknown, payload)
        unknown_result = _run(
            "python3",
            str(REPOSITORY_ROOT / "eng/release_evidence.py"),
            "verify-manifest",
            "--manifest",
            str(unknown),
            "--no-root",
            cwd=REPOSITORY_ROOT,
        )
        self.assertEqual(1, unknown_result.returncode)
        self.assertIn("unknown v2 member", unknown_result.stdout)

        duplicate = self.root / "release-evidence/duplicate.json"
        duplicate.write_text('{"manifest_schema":"hexalith.release-evidence.v2","manifest_schema":"hexalith.release-evidence.v2"}', encoding="utf-8")
        duplicate_result = _run(
            "python3",
            str(REPOSITORY_ROOT / "eng/release_evidence.py"),
            "verify-manifest",
            "--manifest",
            str(duplicate),
            "--no-root",
            cwd=REPOSITORY_ROOT,
        )
        self.assertEqual(1, duplicate_result.returncode)
        self.assertIn("duplicate JSON member", duplicate_result.stdout)

        legacy = self.root / "release-evidence/legacy.json"
        _write_json(legacy, {"commit_sha": "legacy"})
        audit = _run(
            "python3",
            str(REPOSITORY_ROOT / "eng/release_evidence.py"),
            "verify-manifest",
            "--manifest",
            str(legacy),
            "--no-root",
            "--audit-legacy",
            cwd=REPOSITORY_ROOT,
        )
        self.assertEqual(0, audit.returncode)
        self.assertIn("audit-only", audit.stdout)
        reseal = _run(
            "python3",
            str(REPOSITORY_ROOT / "eng/release_evidence.py"),
            "seal-manifest",
            "--manifest",
            str(legacy),
            "--output",
            str(self.root / "release-evidence/legacy-sealed.json"),
            cwd=REPOSITORY_ROOT,
        )
        self.assertEqual(1, reseal.returncode)
        self.assertIn("cannot be sealed", reseal.stdout)


class BuildsExecutionInventoryExclusionTests(unittest.TestCase):
    """Release prepare-candidate checks Builds out under `.hexalith/builds-execution`."""

    def test_hexalith_builds_checkout_packable_tools_are_not_unexpected(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            project = (
                root
                / ".hexalith"
                / "builds-execution"
                / "src"
                / "libraries"
                / "Hexalith.Builds.Evidence.Cli"
                / "Hexalith.Builds.Evidence.Cli.csproj"
            )
            project.parent.mkdir(parents=True)
            project.write_text(
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n"
                "  <PropertyGroup>\n"
                "    <IsPackable>true</IsPackable>\n"
                "    <PackageId>Hexalith.Builds.Evidence.Cli</PackageId>\n"
                "  </PropertyGroup>\n"
                "</Project>\n",
                encoding="utf-8",
            )
            unexpected = HELPER.discover_unexpected_packable_outside_src(root)
            self.assertEqual([], unexpected)

    def test_sibling_packable_outside_src_still_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            project = root / "extras" / "Rogue.csproj"
            project.parent.mkdir(parents=True)
            project.write_text(
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n"
                "  <PropertyGroup>\n"
                "    <IsPackable>true</IsPackable>\n"
                "    <PackageId>Rogue</PackageId>\n"
                "  </PropertyGroup>\n"
                "</Project>\n",
                encoding="utf-8",
            )
            unexpected = HELPER.discover_unexpected_packable_outside_src(root)
            self.assertEqual([project.resolve()], [path.resolve() for path in unexpected])


if __name__ == "__main__":
    unittest.main()
