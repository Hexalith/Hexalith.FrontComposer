#!/usr/bin/env python3
"""Focused AD-13/AD-15 exact-candidate handoff contract tests."""

from __future__ import annotations

import copy
import hashlib
import json
import pathlib
import sys
import tempfile
import unittest
from unittest import mock

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))

import dependency_graph as dg  # noqa: E402
import dependency_handoff as dh  # noqa: E402


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


if __name__ == "__main__":
    unittest.main()
