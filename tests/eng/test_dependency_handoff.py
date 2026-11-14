#!/usr/bin/env python3
"""Focused AD-13/AD-15 exact-candidate handoff contract tests."""

from __future__ import annotations

import copy
import hashlib
import json
import os
import pathlib
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest import mock

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))

import dependency_graph as dg  # noqa: E402
import dependency_handoff as dh  # noqa: E402
from tests.eng.test_dependency_graph import TempGitRepo  # noqa: E402


class HandoffTests(unittest.TestCase):
    candidate = "a" * 40
    base = "b" * 40

    @staticmethod
    def _graph(commit: str) -> dict:
        graph = {
            "schema": dg.SCHEMA,
            "root": {"repository": dh.ROOT_REPOSITORY, "commit": commit},
            "edge_count": 0,
            "edges": [],
        }
        graph["graph_digest"] = dg.canonical_digest(graph)
        return graph

    @staticmethod
    def _evaluator(seed: str) -> dict:
        evaluator = {
            "caller": {
                "repository": dh.ROOT_REPOSITORY,
                "workflow_path": f".github/workflows/{seed}.yml",
                "commit": ("a" if seed == "ci" else "c") * 40,
                "blob_sha256": "1" * 64,
            },
            "reusable": {
                "repository": "github.com/hexalith/hexalith.builds",
                "workflow_path": f".github/workflows/{seed}.yml",
                "commit": "d" * 40,
                "blob_sha256": "2" * 64,
            },
            "actions": [{
                "repository": "github.com/actions/checkout",
                "path": "action.yml",
                "commit": "e" * 40,
                "blob_sha256": "3" * 64,
            }],
        }
        evaluator["definition_digest"] = dg.canonical_digest(evaluator)
        return evaluator

    @staticmethod
    def _authorization(stage: str, evaluator: dict) -> dict:
        caller = evaluator["caller"]
        row = {
            "stage": stage,
            "caller": {
                "repository": caller["repository"],
                "workflow_path": caller["workflow_path"],
                "blob_sha256": caller["blob_sha256"],
            },
            "reusable": evaluator["reusable"],
            "actions": evaluator["actions"],
        }
        row["closure_digest"] = dg.canonical_digest(row)
        return row

    def _policy_projection(self) -> dict:
        return {
            "schema": dg.POLICY_SCHEMA,
            "repository": dh.ROOT_REPOSITORY,
            "path": dg.POLICY_PATH,
            "commit": self.base,
            "sha256": "4" * 64,
        }

    def _ci_handoff(self) -> dict:
        evaluator = self._evaluator("ci")
        policy = {"evaluator_authorizations": {
            "ci": [self._authorization("ci", evaluator)],
            "release": [],
            "post_release": [],
        }}
        return dh.create_ci_handoff(
            run_id=41,
            run_attempt=2,
            base=self.base,
            candidate=self.candidate,
            evaluator=evaluator,
            dependency_policy=self._policy_projection(),
            dependency_graph=self._graph(self.candidate),
            policy=policy,
        )

    def test_authorized_ci_handoff_binds_one_exact_candidate(self) -> None:
        handoff = self._ci_handoff()
        self.assertEqual(handoff["run"]["candidate"], self.candidate)
        self.assertEqual(handoff["revisions"]["candidate"], self.candidate)
        self.assertEqual(handoff["dependency_graph"]["root"]["commit"], self.candidate)
        dh.validate_ci_handoff(handoff)

    def test_sealed_but_unapproved_evaluator_fails_before_handoff(self) -> None:
        evaluator = self._evaluator("ci")
        policy = {"evaluator_authorizations": {"ci": [], "release": [], "post_release": []}}
        with self.assertRaises(dh.HandoffError):
            dh.create_ci_handoff(
                run_id=1,
                run_attempt=1,
                base=self.base,
                candidate=self.candidate,
                evaluator=evaluator,
                dependency_policy=self._policy_projection(),
                dependency_graph=self._graph(self.candidate),
                policy=policy,
            )

    def test_ci_candidate_substitution_fails_closed(self) -> None:
        handoff = self._ci_handoff()
        handoff["run"]["candidate"] = "f" * 40
        with self.assertRaises(dh.HandoffError):
            dh.validate_ci_handoff(handoff)

    def test_failed_release_handoff_preserves_ci_candidate_and_policy(self) -> None:
        ci = self._ci_handoff()
        ci_raw = json.dumps(ci, sort_keys=True, separators=(",", ":")).encode("utf-8")
        release_evaluator = self._evaluator("release")
        policy = {"evaluator_authorizations": {
            "ci": [],
            "release": [self._authorization("release", release_evaluator)],
            "post_release": [],
        }}
        handoff = dh.create_release_handoff(
            release_run_id=50,
            release_run_attempt=1,
            conclusion="failure",
            ci_handoff_raw=ci_raw,
            evaluator=release_evaluator,
            policy=policy,
            release={"version": None, "tag": None, "github_release_id": None, "published": False},
            manifest={"path": None, "sha256": None, "seal": None},
            assets=[],
        )
        dh.validate_release_handoff(handoff, ci_handoff_raw=ci_raw)

        substituted = copy.deepcopy(handoff)
        substituted["candidate"] = "f" * 40
        with self.assertRaises(dh.HandoffError):
            dh.validate_release_handoff(substituted, ci_handoff_raw=ci_raw)

    def test_release_handoff_rejects_same_asset_name_with_different_bytes(self) -> None:
        ci_raw = dg.canonical_bytes(self._ci_handoff())
        evaluator = self._evaluator("release")
        policy = {"evaluator_authorizations": {"release": [self._authorization("release", evaluator)]}}
        arguments = dict(
            release_run_id=50, release_run_attempt=1, conclusion="success",
            ci_handoff_raw=ci_raw, evaluator=evaluator, policy=policy,
            release={"version": "1.2.3", "tag": "v1.2.3", "github_release_id": 9, "published": True},
            manifest={"path": "release-evidence.json", "sha256": "4" * 64, "seal": "5" * 64},
            assets=[{"name": "package.nupkg", "sha256": "1" * 64, "size": 10}],
        )
        valid = dh.create_release_handoff(**arguments)
        valid["assets"].append({"name": "package.nupkg", "sha256": "2" * 64, "size": 20})
        with self.assertRaisesRegex(dh.HandoffError, "sorted and unique"):
            dh.validate_release_handoff(valid, ci_handoff_raw=ci_raw)
        arguments["assets"] = valid["assets"]
        with self.assertRaisesRegex(dh.HandoffError, "sorted and unique"):
            dh.create_release_handoff(**arguments)

    def test_unpublished_attempt_cannot_green_noop_with_omitted_projection(self) -> None:
        ci = self._ci_handoff()
        handoff = {
            "schema": dh.RELEASE_HANDOFF_SCHEMA,
            "release_run": {},
            "ci_handoff": {},
            "candidate": self.candidate,
            "dependency_policy": self._policy_projection(),
            "release": {"version": None, "tag": None, "github_release_id": None, "published": False},
            "manifest": {"path": None, "sha256": None, "seal": None},
            "assets": [],
            "evaluator": self._evaluator("release"),
        }
        with self.assertRaises(dh.HandoffError):
            dh.validate_release_handoff(handoff)

    def _source_proof(self) -> dict:
        evidence = {
            "revisions": {
                "event": "push",
                "event_base": self.base,
                "candidate": self.candidate,
                "release_eligible": True,
            },
            "dependency_policy": self._policy_projection(),
            "candidate_graph": self._graph(self.candidate),
        }
        return dh.create_source_proof(run_id=70, run_attempt=3, evidence=evidence)

    def test_exact_source_proof_binds_push_ci_policy_and_candidate(self) -> None:
        proof = self._source_proof()
        self.assertEqual(proof["schema"], dh.SOURCE_PROOF_SCHEMA)
        self.assertEqual(proof["run"]["candidate"], self.candidate)
        self.assertEqual(proof["run"]["run_attempt"], 3)
        dh.validate_source_proof(proof)

    def test_exact_source_proof_rejects_candidate_or_run_substitution(self) -> None:
        for mutation in ("candidate", "run"):
            with self.subTest(mutation=mutation):
                proof = copy.deepcopy(self._source_proof())
                if mutation == "candidate":
                    proof["revisions"]["candidate"] = "f" * 40
                else:
                    proof["run"]["run_id"] = 0
                with self.assertRaises(dh.HandoffError):
                    dh.validate_source_proof(proof)

    def test_create_ci_from_evidence_requires_authorized_evaluator(self) -> None:
        evaluator = self._evaluator("ci")
        evidence = {
            "revisions": {
                "event": "push",
                "event_base": self.base,
                "candidate": self.candidate,
                "release_eligible": True,
            },
            "candidate_graph": self._graph(self.candidate),
        }
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            # Empty authorizations: create_ci_handoff must be invoked and fail closed.
            policy = {
                "schema": dg.POLICY_SCHEMA,
                "builds_identity": "github.com/hexalith/hexalith.builds",
                "trusted_identities": [{
                    "identity": dh.ROOT_REPOSITORY,
                    "local_path": ".",
                }],
                "semantic_profiles": {},
                "owner_profiles": {},
                "module_build_registry": {},
                "resource_limits": {
                    "max_edges": 4096,
                    "max_ls_tree_bytes": 67108864,
                    "max_gitmodules_bytes": 1048576,
                    "max_catalog_blob_bytes": 4194304,
                    "max_contract_tree_files": 16384,
                    "max_contract_tree_blob_bytes": 16777216,
                    "max_contract_tree_total_bytes": 268435456,
                    "max_workflow_closure_depth": 16,
                    "max_workflow_closure_sources": 256,
                    "max_workflow_source_blob_bytes": 1048576,
                    "max_workflow_source_total_bytes": 16777216,
                },
                "evaluator_authorizations": {"ci": [], "release": [], "post_release": []},
            }
            with mock.patch.object(dg, "load_policy_at_commit", return_value=(policy, b"{}", {
                "schema": dg.POLICY_SCHEMA,
                "repository": dh.ROOT_REPOSITORY,
                "path": dg.POLICY_PATH,
                "commit": self.base,
                "sha256": "4" * 64,
            })):
                with self.assertRaises(dh.HandoffError):
                    dh.create_ci_handoff_from_evidence(
                        root=root,
                        evidence=evidence,
                        run_id=9,
                        run_attempt=1,
                        evaluator=evaluator,
                    )

    def test_materialize_release_assets_hashes_candidate_files(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            candidate = root / "candidate"
            package = candidate / "nupkgs" / "Package.1.0.0.nupkg"
            package.parent.mkdir(parents=True)
            package.write_bytes(b"exact-bytes")
            digest = hashlib.sha256(b"exact-bytes").hexdigest()
            release = {
                "assets": [
                    {
                        "name": "Package.1.0.0.nupkg",
                        "size": len(b"exact-bytes"),
                        "digest": f"sha256:{digest}",
                    },
                ],
            }
            assets = dh.materialize_release_assets(release=release, candidate_root=candidate)
            self.assertEqual(
                [{"name": "Package.1.0.0.nupkg", "sha256": digest, "size": 11}],
                assets,
            )

    def test_materialize_release_assets_rejects_digest_and_path_negatives(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            candidate = root / "candidate"
            package = candidate / "nupkgs" / "Package.1.0.0.nupkg"
            package.parent.mkdir(parents=True)
            package.write_bytes(b"exact-bytes")
            digest = hashlib.sha256(b"exact-bytes").hexdigest()

            with self.assertRaises(dh.HandoffError):
                dh.materialize_release_assets(
                    release={"assets": [{
                        "name": "Package.1.0.0.nupkg",
                        "size": len(b"exact-bytes"),
                        "digest": f"sha256:{'f' * 64}",
                    }]},
                    candidate_root=candidate,
                )

            with self.assertRaises(dh.HandoffError):
                dh.materialize_release_assets(
                    release={"assets": [{
                        "name": "Missing.1.0.0.nupkg",
                        "size": 4,
                        "digest": f"sha256:{digest}",
                    }]},
                    candidate_root=candidate,
                )

            for unsafe in ("../Package.1.0.0.nupkg", "nested/Package.1.0.0.nupkg", "*.nupkg", "Pack?.nupkg", "Pack[a].nupkg"):
                with self.subTest(name=unsafe):
                    with self.assertRaises(dh.HandoffError):
                        dh.materialize_release_assets(
                            release={"assets": [{"name": unsafe, "size": 1}]},
                            candidate_root=candidate,
                        )

            outside = root / "outside"
            outside.mkdir()
            target = outside / "Escape.1.0.0.nupkg"
            target.write_bytes(b"evil-bytes")
            link = candidate / "Escape.1.0.0.nupkg"
            link.symlink_to(target)
            with self.assertRaises(dh.HandoffError):
                dh.materialize_release_assets(
                    release={"assets": [{"name": "Escape.1.0.0.nupkg", "size": len(b"evil-bytes")}]},
                    candidate_root=candidate,
                )

            duplicate = candidate / "dup" / "Package.1.0.0.nupkg"
            duplicate.parent.mkdir(parents=True)
            duplicate.write_bytes(b"exact-bytes")
            with self.assertRaises(dh.HandoffError):
                dh.materialize_release_assets(
                    release={"assets": [{"name": "Package.1.0.0.nupkg", "size": len(b"exact-bytes")}]},
                    candidate_root=candidate,
                )

            single = root / "single-candidate"
            single_pkg = single / "nupkgs" / "Only.1.0.0.nupkg"
            single_pkg.parent.mkdir(parents=True)
            single_pkg.write_bytes(b"only-bytes")
            with self.assertRaises(dh.HandoffError):
                dh.materialize_release_assets(
                    release={"assets": [
                        {"name": "Only.1.0.0.nupkg", "size": len(b"only-bytes")},
                        {"name": "Only.1.0.0.nupkg", "size": len(b"only-bytes")},
                    ]},
                    candidate_root=single,
                )

    def test_published_create_release_hard_fails_instead_of_deferred(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            release_path = root / "release.json"
            manifest_path = root / "manifest.json"
            assets_path = root / "assets.json"
            output_path = root / "handoff.json"
            release_path.write_text(json.dumps({
                "version": "1.2.3",
                "tag": "v1.2.3",
                "github_release_id": 9,
                "published": True,
            }), encoding="utf-8")
            manifest_path.write_text(json.dumps({
                "path": None,
                "sha256": None,
                "seal": None,
            }), encoding="utf-8")
            assets_path.write_text("[]\n", encoding="utf-8")
            ci = self._ci_handoff()
            ci_path = root / "ci.json"
            ci_path.write_bytes(json.dumps(ci, sort_keys=True, separators=(",", ":")).encode("utf-8"))
            evaluator = self._evaluator("release")
            evaluator_path = root / "evaluator.json"
            evaluator_path.write_text(json.dumps(evaluator), encoding="utf-8")
            # Empty authorizations force create_release_handoff to fail; published must not soft-defer.
            policy = {
                "schema": dg.POLICY_SCHEMA,
                "builds_identity": "github.com/hexalith/hexalith.builds",
                "trusted_identities": [{"identity": dh.ROOT_REPOSITORY, "local_path": "."}],
                "semantic_profiles": {},
                "owner_profiles": {},
                "module_build_registry": {},
                "resource_limits": {
                    "max_edges": 4096,
                    "max_ls_tree_bytes": 67108864,
                    "max_gitmodules_bytes": 1048576,
                    "max_catalog_blob_bytes": 4194304,
                    "max_contract_tree_files": 16384,
                    "max_contract_tree_blob_bytes": 16777216,
                    "max_contract_tree_total_bytes": 268435456,
                    "max_workflow_closure_depth": 16,
                    "max_workflow_closure_sources": 256,
                    "max_workflow_source_blob_bytes": 1048576,
                    "max_workflow_source_total_bytes": 16777216,
                },
                "evaluator_authorizations": {"ci": [], "release": [], "post_release": []},
            }
            with mock.patch.object(dg, "load_policy_at_commit", return_value=(policy, b"{}", {
                "schema": dg.POLICY_SCHEMA,
                "repository": dh.ROOT_REPOSITORY,
                "path": dg.POLICY_PATH,
                "commit": self.base,
                "sha256": "4" * 64,
            })):
                status = dh.main([
                    "--root", str(root),
                    "create-release",
                    "--ci-handoff", str(ci_path),
                    "--release-run-id", "50",
                    "--release-run-attempt", "1",
                    "--conclusion", "success",
                    "--evaluator", str(evaluator_path),
                    "--policy-commit", self.base,
                    "--release", str(release_path),
                    "--manifest", str(manifest_path),
                    "--assets", str(assets_path),
                    "--output", str(output_path),
                ])
            self.assertEqual(1, status)
            self.assertFalse(output_path.with_suffix(".deferred.json").exists())


class HandoffAuthorityGateTests(unittest.TestCase):
    """Exercise the production CLIs and the checked-in workflow commands."""

    candidate = HandoffTests.candidate
    base = HandoffTests.base
    _evaluator = staticmethod(HandoffTests._evaluator)
    _authorization = staticmethod(HandoffTests._authorization)

    def _authority(self, root: pathlib.Path, *, active_stages: tuple[str, ...]) -> dict:
        repository = TempGitRepo(root)
        policy = json.loads((ROOT / dg.POLICY_PATH).read_bytes())
        policy.pop("consumer_source_projections", None)
        evaluators = {stage: self._evaluator(stage) for stage in ("ci", "release")}
        for stage, evaluator in evaluators.items():
            raw = f"name: Exact {stage} caller\non: push\njobs: {{}}\n".encode()
            repository.write_bytes(evaluator["caller"]["workflow_path"], raw)
            evaluator["caller"]["blob_sha256"] = hashlib.sha256(raw).hexdigest()
        policy["evaluator_authorizations"] = {
            stage: [self._authorization(stage, evaluators[stage])] if stage in active_stages else []
            for stage in ("ci", "release", "post_release")
        }
        policy_raw = dg.canonical_bytes(policy)
        repository.write_bytes(dg.POLICY_PATH, policy_raw)
        base = repository.commit("test: seed active handoff authority")
        # A candidate that adds its own missing authorization must still be denied.
        candidate_policy = copy.deepcopy(policy)
        for stage in ("ci", "release"):
            candidate_policy["evaluator_authorizations"][stage] = [self._authorization(stage, evaluators[stage])]
        repository.write_bytes(dg.POLICY_PATH, dg.canonical_bytes(candidate_policy))
        repository.write_text("candidate.txt", "Exact candidate\n")
        candidate = repository.commit("test: add inactive candidate handoff authority")
        for evaluator in evaluators.values():
            evaluator["caller"]["commit"] = candidate
            evaluator["definition_digest"] = dg.canonical_digest({
                key: value for key, value in evaluator.items() if key != "definition_digest"
            })
        graph = dg.collect_graph(repository.root, dh.ROOT_REPOSITORY, candidate, policy)
        evidence = {
            "revisions": {"event": "push", "event_base": base, "candidate": candidate, "release_eligible": True},
            "candidate_graph": graph,
        }
        for name, value in (("evidence", evidence), ("ci-evaluator", evaluators["ci"]),
                            ("release-evaluator", evaluators["release"]),
                            ("release", {"version": None, "tag": None, "github_release_id": None, "published": False}),
                            ("manifest", {"path": None, "sha256": None, "seal": None}), ("assets", [])):
            (root / f"{name}.json").write_bytes(dg.canonical_bytes(value))
        return {"root": root, "base": base, "candidate": candidate, "repository": repository}

    def _cli(self, authority: dict, command: str, *, policy_commit: str | None = None) -> subprocess.CompletedProcess:
        root = authority["root"]
        args = [sys.executable, str(ROOT / "eng/dependency_handoff.py"), "--root", str(root), command]
        if command == "create-ci":
            args.extend(["--evidence", str(root / "evidence.json"), "--run-id", "41", "--run-attempt", "2",
                         "--evaluator", str(root / "ci-evaluator.json")])
        else:
            args.extend(["--ci-handoff", str(root / "create-ci.json"), "--release-run-id", "50",
                         "--release-run-attempt", "1", "--conclusion", "failure",
                         "--evaluator", str(root / "release-evaluator.json"), "--policy-commit", policy_commit or authority["base"]])
            for name in ("release", "manifest", "assets"):
                args.extend([f"--{name}", str(root / f"{name}.json")])
        args.extend(["--output", str(root / f"{command}.json")])
        return subprocess.run(args, capture_output=True, text=True, check=False)

    def test_cli_authorized_active_policy_creates_both_handoffs(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            authority = self._authority(pathlib.Path(temporary), active_stages=("ci", "release"))
            for command in ("create-ci", "create-release"):
                result = self._cli(authority, command)
                self.assertEqual(0, result.returncode, result.stdout + result.stderr)
                self.assertTrue((authority["root"] / f"{command}.json").exists())
                self.assertFalse((authority["root"] / f"{command}.deferred.json").exists())

    def test_ci_cli_rejects_candidate_only_authority_without_deferred_sentinel(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            authority = self._authority(pathlib.Path(temporary), active_stages=())
            result = self._cli(authority, "create-ci")
            self.assertEqual(1, result.returncode, result.stdout + result.stderr)
            self.assertIn("exactly one active-policy authorization", result.stdout)
            self.assertFalse((authority["root"] / "create-ci.json").exists())
            self.assertFalse((authority["root"] / "create-ci.deferred.json").exists())

    def test_release_cli_rejects_candidate_only_authority_without_deferred_sentinel(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            authority = self._authority(pathlib.Path(temporary), active_stages=("ci",))
            self.assertEqual(0, self._cli(authority, "create-ci").returncode)
            result = self._cli(authority, "create-release")
            self.assertEqual(1, result.returncode, result.stdout + result.stderr)
            self.assertIn("exactly one active-policy authorization", result.stdout)
            self.assertFalse((authority["root"] / "create-release.json").exists())
            self.assertFalse((authority["root"] / "create-release.deferred.json").exists())

    def test_release_cli_rejects_policy_commit_or_raw_digest_substitution(self) -> None:
        for substitution in ("candidate-policy", "raw-digest"):
            with self.subTest(substitution=substitution), tempfile.TemporaryDirectory() as temporary:
                stages = ("ci",) if substitution == "candidate-policy" else ("ci", "release")
                authority = self._authority(pathlib.Path(temporary), active_stages=stages)
                self.assertEqual(0, self._cli(authority, "create-ci").returncode)
                if substitution == "candidate-policy":
                    result = self._cli(authority, "create-release", policy_commit=authority["candidate"])
                    self.assertIn("policy commit must equal", result.stdout)
                else:
                    ci_path = authority["root"] / "create-ci.json"
                    ci = json.loads(ci_path.read_bytes())
                    ci["dependency_policy"]["sha256"] = "f" * 64
                    ci_path.write_bytes(dg.canonical_bytes(ci))
                    result = self._cli(authority, "create-release")
                    self.assertIn("active policy projection differs", result.stdout)
                self.assertEqual(1, result.returncode, result.stdout + result.stderr)
                self.assertFalse((authority["root"] / "create-release.json").exists())
                self.assertFalse((authority["root"] / "create-release.deferred.json").exists())

    @staticmethod
    def _refresh_evaluator(path: pathlib.Path, **caller_changes) -> dict:
        evaluator = json.loads(path.read_bytes())
        evaluator["caller"].update(caller_changes)
        evaluator["definition_digest"] = dg.canonical_digest({
            key: value for key, value in evaluator.items() if key != "definition_digest"
        })
        path.write_bytes(dg.canonical_bytes(evaluator))
        return evaluator

    def _change_candidate_workflow(self, authority: dict, workflow: str) -> None:
        repository = authority["repository"]
        repository.write_text(f".github/workflows/{workflow}.yml", "name: Changed committed caller\non: push\njobs: {}\n")
        candidate = repository.commit("test: forge declared caller identity")
        authority["candidate"] = candidate
        for stage in ("ci", "release"):
            self._refresh_evaluator(authority["root"] / f"{stage}-evaluator.json", commit=candidate)
        evidence = json.loads((authority["root"] / "evidence.json").read_bytes())
        evidence["revisions"]["candidate"] = candidate
        policy, _, _ = dg.load_policy_at_commit(authority["root"], authority["base"])
        evidence["candidate_graph"] = dg.collect_graph(authority["root"], dh.ROOT_REPOSITORY, candidate, policy)
        (authority["root"] / "evidence.json").write_bytes(dg.canonical_bytes(evidence))

    def test_committed_ci_caller_hash_is_required_by_creation_and_live_validation(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            authority = self._authority(pathlib.Path(temporary), active_stages=("ci", "release"))
            self._change_candidate_workflow(authority, "ci")
            root = authority["root"]
            policy, raw_policy, projection = dg.load_policy_at_commit(root, authority["base"])
            evidence = json.loads((root / "evidence.json").read_bytes())
            evaluator = json.loads((root / "ci-evaluator.json").read_bytes())
            # Offline APIs intentionally inspect structure and supplied authority only.
            forged = dh.create_ci_handoff(run_id=41, run_attempt=2, base=authority["base"],
                                          candidate=authority["candidate"], evaluator=evaluator,
                                          dependency_policy=projection, dependency_graph=evidence["candidate_graph"], policy=policy)
            dh.validate_ci_handoff(forged)
            with self.assertRaisesRegex(dh.HandoffError, "hash differs from committed"):
                dh.validate_ci_handoff(forged, root=root)
            failed = self._cli(authority, "create-ci")
            self.assertEqual(1, failed.returncode, failed.stdout + failed.stderr)
            self.assertIn("hash differs from committed", failed.stdout)
            self.assertFalse((root / "create-ci.json").exists())
            (root / "create-ci.json").write_bytes(dg.canonical_bytes(forged))
            failed = self._cli(authority, "create-release")
            self.assertEqual(1, failed.returncode, failed.stdout + failed.stderr)
            self.assertIn("hash differs from committed", failed.stdout)
            self.assertFalse((root / "create-release.json").exists())

    def test_release_creation_requires_ci_authorization_under_bound_policy(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            authority = self._authority(pathlib.Path(temporary), active_stages=("ci", "release"))
            self.assertEqual(0, self._cli(authority, "create-ci").returncode)
            ci_path = authority["root"] / "create-ci.json"
            forged = json.loads(ci_path.read_bytes())
            forged["evaluator"]["actions"][0]["blob_sha256"] = "f" * 64
            forged["evaluator"]["definition_digest"] = dg.canonical_digest({
                key: value for key, value in forged["evaluator"].items() if key != "definition_digest"
            })
            dh.validate_ci_handoff(forged)
            ci_path.write_bytes(dg.canonical_bytes(forged))
            failed = self._cli(authority, "create-release")
            self.assertEqual(1, failed.returncode, failed.stdout + failed.stderr)
            self.assertIn("ci evaluator must project exactly one active-policy authorization", failed.stdout)
            self.assertFalse((authority["root"] / "create-release.json").exists())

    def test_release_caller_must_match_candidate_commit_and_committed_bytes(self) -> None:
        for forgery in ("other-commit", "nonexistent-commit", "declared-hash"):
            with self.subTest(forgery=forgery), tempfile.TemporaryDirectory() as temporary:
                authority = self._authority(pathlib.Path(temporary), active_stages=("ci", "release"))
                root = authority["root"]
                if forgery == "declared-hash":
                    self._change_candidate_workflow(authority, "release")
                self.assertEqual(0, self._cli(authority, "create-ci").returncode)
                evaluator_path = root / "release-evaluator.json"
                if forgery != "declared-hash":
                    commit = authority["base"] if forgery == "other-commit" else "f" * 40
                    self._refresh_evaluator(evaluator_path, commit=commit)
                ci_raw = (root / "create-ci.json").read_bytes()
                policy, _, _ = dg.load_policy_at_commit(root, authority["base"])
                offline = dh.create_release_handoff(release_run_id=50, release_run_attempt=1, conclusion="failure",
                    ci_handoff_raw=ci_raw, evaluator=json.loads(evaluator_path.read_bytes()), policy=policy,
                    release=json.loads((root / "release.json").read_bytes()),
                    manifest=json.loads((root / "manifest.json").read_bytes()), assets=[])
                dh.validate_release_handoff(offline, ci_handoff_raw=ci_raw)
                with self.assertRaises(dh.HandoffError):
                    dh.validate_release_handoff(offline, ci_handoff_raw=ci_raw, root=root)
                failed = self._cli(authority, "create-release")
                self.assertEqual(1, failed.returncode, failed.stdout + failed.stderr)
                self.assertIn("committed candidate workflow" if forgery == "declared-hash" else "exact candidate workflow commit", failed.stdout)
                self.assertFalse((root / "create-release.json").exists())
                self.assertFalse((root / "create-release.deferred.json").exists())

    @staticmethod
    def _workflow_script(workflow: str, step: str) -> str:
        lines = (ROOT / ".github/workflows" / workflow).read_text().splitlines()
        index = lines.index(f"      - name: {step}")
        start = lines.index("        run: |", index) + 1
        script = []
        for line in lines[start:]:
            if line and not line.startswith("          "):
                break
            script.append(line[10:])
        return "\n".join(script).replace("${{ github.run_id }}", "41").replace("${{ github.run_attempt }}", "2")

    def _shell(self, root: pathlib.Path, workflow: str, step: str, *, status: int, base: str,
               authenticated: bool = True, published: bool = False) -> tuple[subprocess.CompletedProcess, dict]:
        binary = root / "bin"
        binary.mkdir(exist_ok=True)
        (binary / "python3").write_text(
            f"#!{sys.executable}\n"
            "import json, os, pathlib, sys\n"
            "with pathlib.Path('commands.jsonl').open('a') as out: out.write(json.dumps(sys.argv[1:]) + '\\n')\n"
            "command = next(item for item in ('draft-evaluator', 'create-ci', 'create-release', 'materialize-release-assets') if item in sys.argv)\n"
            "status = int(os.environ['HANDOFF_STATUS']) if command in ('create-ci', 'create-release') else 0\n"
            "if status == 0: pathlib.Path(sys.argv[sys.argv.index('--output') + 1]).write_text('[]' if command == 'materialize-release-assets' else '{}')\n"
            "raise SystemExit(status)\n"
        )
        (binary / "gh").write_text(
            f"#!{sys.executable}\nimport json, pathlib, sys\n"
            "with pathlib.Path('gh-commands.jsonl').open('a') as out: out.write(json.dumps(sys.argv[1:]) + '\\n')\n"
            "if sys.argv[1:] == ['api', 'repos/Hexalith/Hexalith.FrontComposer/releases/tags/v1.2.3']:\n"
            "    print(json.dumps({'id': 9, 'assets': []})); raise SystemExit(0)\n"
            "if sys.argv[1:3] != ['run', 'download']: raise SystemExit('unexpected API request')\n"
            "target = pathlib.Path(sys.argv[sys.argv.index('--dir') + 1])\n"
            "target.mkdir(parents=True, exist_ok=True)\n"
            "name = sys.argv[sys.argv.index('--name') + 1]\n"
            "if name == 'release-ci-handoff-50-1': (target / 'dependency-release-handoff.json').write_text('{}')\n"
            "elif name == 'release-candidate-50-1':\n"
            "    (target / 'release-evidence').mkdir()\n"
            "    (target / 'release-evidence/sealed-manifest.json').write_text(json.dumps({'seal': {'hash': '5' * 64}}))\n"
            "else: raise SystemExit('unexpected artifact request')\n"
        )
        for executable in binary.iterdir():
            executable.chmod(0o755)
        (root / "artifacts/dependency-governance").mkdir(parents=True, exist_ok=True)
        env = {
            **os.environ, "PATH": str(binary) + os.pathsep + os.environ["PATH"], "HANDOFF_STATUS": str(status),
            "OBJECT_ROOT": str(root), "EVENT_BASE": base, "CANDIDATE": self.candidate,
            "GITHUB_OUTPUT": str(root / "outputs"), "GITHUB_STEP_SUMMARY": str(root / "summary"),
            "REPOSITORY": "Hexalith/Hexalith.FrontComposer", "RUN_ID": "50", "RUN_ATTEMPT": "1",
            "VERIFY_RESULT": "success" if authenticated else "failure", "PLAN_RESULT": "success",
            "PREPARE_RESULT": "success" if published else "skipped", "RELEASE_RESULT": "success" if published else "skipped",
            "PUBLICATION_RESULT": "success" if published else "skipped",
            "POLICY_COMMIT": base, "VERSION": "1.2.3" if published else "",
        }
        result = subprocess.run(["bash", "-c", self._workflow_script(workflow, step)], cwd=root, env=env,
                                capture_output=True, text=True, check=False)
        commands = [json.loads(line) for line in (root / "commands.jsonl").read_text().splitlines()] if (root / "commands.jsonl").exists() else []
        gh_commands = [json.loads(line) for line in (root / "gh-commands.jsonl").read_text().splitlines()] if (root / "gh-commands.jsonl").exists() else []
        return result, {"commands": commands, "gh_commands": gh_commands,
                        "outputs": (root / "outputs").read_text() if (root / "outputs").exists() else ""}

    @unittest.skipUnless(shutil.which("bash"), "workflow script requires bash")
    def test_checked_in_ci_shell_fails_every_nonzero_handoff_exit(self) -> None:
        for status in (0, 1, 2):
            with self.subTest(status=status), tempfile.TemporaryDirectory() as temporary:
                result, evidence = self._shell(pathlib.Path(temporary), "ci.yml", "Create AD-13 dependency-release-handoff",
                                               status=status, base=self.base)
                self.assertEqual(status, result.returncode, result.stdout + result.stderr)
                self.assertEqual(2, len(evidence["commands"]))
                draft = evidence["commands"][0]
                self.assertEqual(self.base, draft[draft.index("--policy-commit") + 1])
                self.assertIn(f"created={'true' if status == 0 else 'false'}", evidence["outputs"])

    @unittest.skipUnless(shutil.which("bash"), "workflow script requires bash")
    def test_checked_in_ci_shell_never_uses_zero_or_malformed_base_policy(self) -> None:
        for base in ("0" * 40, "", "main", "B" * 40):
            with self.subTest(base=base), tempfile.TemporaryDirectory() as temporary:
                result, evidence = self._shell(pathlib.Path(temporary), "ci.yml", "Create AD-13 dependency-release-handoff",
                                               status=0, base=base)
                self.assertEqual(1, result.returncode, result.stdout + result.stderr)
                self.assertFalse(evidence["commands"])
                self.assertNotIn("created=true", evidence["outputs"])

    @unittest.skipUnless(shutil.which("bash") and shutil.which("jq"), "workflow script requires bash and jq")
    def test_checked_in_release_shell_never_soft_defers_after_ci_authentication(self) -> None:
        for status in (0, 1, 2):
            with self.subTest(status=status), tempfile.TemporaryDirectory() as temporary:
                root = pathlib.Path(temporary)
                result, evidence = self._shell(root, "release.yml", "Emit AD-15 release-verification-handoff",
                                               status=status, base=self.base)
                self.assertEqual(0 if status == 0 else 1, result.returncode, result.stdout + result.stderr)
                self.assertEqual(2, len(evidence["commands"]))
                self.assertFalse(list(root.rglob("*.deferred.json")))
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            result, evidence = self._shell(root, "release.yml", "Emit AD-15 release-verification-handoff",
                                           status=1, base=self.base, authenticated=False)
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertFalse(evidence["commands"])
            sentinel = root / "artifacts/release-verification-handoff/release-verification-handoff.deferred.json"
            self.assertTrue(json.loads(sentinel.read_text())["deferred"])


    @unittest.skipUnless(shutil.which("bash") and shutil.which("jq"), "workflow script requires bash and jq")
    def test_checked_in_published_release_retains_evidence_and_fails_helper_errors(self) -> None:
        for status in (1, 2):
            with self.subTest(status=status), tempfile.TemporaryDirectory() as temporary:
                root = pathlib.Path(temporary)
                result, evidence = self._shell(root, "release.yml", "Emit AD-15 release-verification-handoff",
                                               status=status, base=self.base, published=True)
                self.assertEqual(1, result.returncode, result.stdout + result.stderr)
                artifacts = root / "artifacts/release-verification-handoff"
                for name in ("ci/dependency-release-handoff.json", "release-evaluator.json", "github-release.json",
                             "candidate/release-evidence/sealed-manifest.json", "assets.json", "release.json", "manifest.json"):
                    self.assertTrue((artifacts / name).is_file(), name)
                self.assertTrue(json.loads((artifacts / "release.json").read_text())["published"])
                self.assertEqual("5" * 64, json.loads((artifacts / "manifest.json").read_text())["seal"])
                self.assertFalse((artifacts / "release-verification-handoff.json").exists())
                self.assertFalse(list(root.rglob("*.deferred.json")))
                self.assertEqual(3, len(evidence["commands"]))
                self.assertEqual(3, len(evidence["gh_commands"]))
                if status == 2:
                    self.assertIn("cannot soft-defer after publication", result.stdout)

    @unittest.skipUnless(shutil.which("bash") and shutil.which("jq"), "workflow script requires bash and jq")
    def test_checked_in_published_release_with_unavailable_ci_fails_and_retains_sentinel(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            result, evidence = self._shell(root, "release.yml", "Emit AD-15 release-verification-handoff",
                                           status=0, base=self.base, authenticated=False, published=True)
            self.assertEqual(1, result.returncode, result.stdout + result.stderr)
            artifacts = root / "artifacts/release-verification-handoff"
            self.assertTrue(json.loads((artifacts / "release-verification-handoff.deferred.json").read_text())["deferred"])
            self.assertEqual([], json.loads((artifacts / "assets.json").read_text()))
            self.assertFalse((artifacts / "release-verification-handoff.json").exists())
            self.assertFalse(evidence["commands"])
            self.assertFalse(evidence["gh_commands"])
            self.assertIn("cannot soft-succeed after publication", result.stdout)


if __name__ == "__main__":
    unittest.main()
