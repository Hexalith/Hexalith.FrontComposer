#!/usr/bin/env python3
"""Focused AD-13/AD-15 exact-candidate handoff contract tests."""

from __future__ import annotations

import copy
import json
import pathlib
import sys
import unittest

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


if __name__ == "__main__":
    unittest.main()
