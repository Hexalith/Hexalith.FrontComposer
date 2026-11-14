#!/usr/bin/env python3
"""Runtime proofs for release disposition and retroactive-auth gates."""

from __future__ import annotations

import copy
import json
import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))

import release_disposition as rd  # noqa: E402


SHA = "a" * 40


def _job(name: str, conclusion: str, status: str = "completed") -> dict:
    return {"name": name, "conclusion": conclusion, "status": status}


class ReleaseDispositionTests(unittest.TestCase):
    def _run(self, conclusion: str = "success") -> dict:
        return {
            "id": 42,
            "run_attempt": 1,
            "event": "workflow_dispatch",
            "status": "completed",
            "conclusion": conclusion,
            "head_branch": "main",
            "head_sha": SHA,
            "path": ".github/workflows/release.yml",
            "repository": {"full_name": "Hexalith/Hexalith.FrontComposer"},
        }

    def test_completed_release_job_is_governed_attempt(self) -> None:
        jobs = [
            _job("verify-source", "success"),
            _job("plan-release", "success"),
            _job("prepare-candidate", "success"),
            _job("release", "success"),
            _job("release / publish", "success"),
            _job("verify-publication", "success"),
            _job("emit-verification-handoff", "success"),
        ]
        result = rd.classify_release_run(
            run=self._run("success"),
            jobs=jobs,
            expected_run_id=42,
            expected_run_attempt=1,
            expected_conclusion="success",
            expected_head_sha=SHA,
            ad15_candidate=SHA,
        )
        self.assertTrue(result["governed_attempt"])
        self.assertEqual(result["status"], "governed-publication-attempt")
        self.assertEqual(result["candidate"], SHA)

    def test_failed_handoff_after_successful_reusable_release_is_governed_attempt(self) -> None:
        jobs = [
            _job("verify-source", "success"),
            _job("plan-release", "success"),
            _job("prepare-candidate", "success"),
            _job("release / release", "success"),
            _job("release / governed-release", "skipped"),
            _job("verify-publication", "success"),
            _job("emit-verification-handoff", "failure"),
        ]
        result = rd.classify_release_run(
            run=self._run("failure"),
            jobs=jobs,
            expected_run_id=42,
            expected_run_attempt=1,
            expected_conclusion="failure",
            expected_head_sha=SHA,
            ad15_candidate=SHA,
        )
        self.assertTrue(result["governed_attempt"])
        self.assertEqual(result["status"], "governed-publication-attempt")

    def test_two_publisher_jobs_are_rejected_as_ambiguous(self) -> None:
        jobs = [
            _job("verify-source", "success"),
            _job("plan-release", "success"),
            _job("release / publish", "success"),
            _job("release / release", "success"),
            _job("emit-verification-handoff", "success"),
        ]
        with self.assertRaises(rd.DispositionError):
            rd.classify_release_run(
                run=self._run(),
                jobs=jobs,
                expected_run_id=42,
                expected_run_attempt=1,
                expected_conclusion="success",
                expected_head_sha=SHA,
            )

    def test_misclassified_no_release_topology_fails_closed(self) -> None:
        jobs = [
            _job("verify-source", "success"),
            _job("plan-release", "success"),
            _job("prepare-candidate", "success"),
            _job("emit-verification-handoff", "success"),
        ]
        with self.assertRaises(rd.DispositionError):
            rd.classify_release_run(
                run=self._run("success"),
                jobs=jobs,
                expected_run_id=42,
                expected_run_attempt=1,
                expected_conclusion="success",
                expected_head_sha=SHA,
            )

    def test_no_releasable_commits_is_not_governed(self) -> None:
        jobs = [
            _job("verify-source", "success"),
            _job("plan-release", "success"),
            _job("emit-verification-handoff", "success"),
        ]
        result = rd.classify_release_run(
            run=self._run("success"),
            jobs=jobs,
            expected_run_id=42,
            expected_run_attempt=1,
            expected_conclusion="success",
            expected_head_sha=SHA,
        )
        self.assertFalse(result["governed_attempt"])
        self.assertEqual(result["status"], "no-releasable-commits")

    def test_governed_attempt_cannot_substitute_head_for_missing_or_different_candidate(self) -> None:
        jobs = [
            _job("verify-source", "success"),
            _job("plan-release", "success"),
            _job("release / release", "failure"),
            _job("emit-verification-handoff", "success"),
        ]
        for candidate in (None, "", "b" * 40, "0" * 40, "A" * 40, True):
            with self.subTest(candidate=candidate):
                with self.assertRaises(rd.DispositionError):
                    rd.classify_release_run(
                        run=self._run("failure"),
                        jobs=jobs,
                        expected_run_id=42,
                        expected_run_attempt=1,
                        expected_conclusion="failure",
                        expected_head_sha=SHA,
                        ad15_candidate=candidate,
                    )

    def test_unauthorized_readiness_fails_closed(self) -> None:
        with self.assertRaises(rd.DispositionError) as raised:
            rd.require_published_readiness({
                "classification": "blocked",
                "publish_authorized": False,
            })
        self.assertIn("was not authorized by its sealed readiness evidence", str(raised.exception))

    def test_authorized_readiness_passes(self) -> None:
        rd.require_published_readiness({
            "classification": "ready",
            "publish_authorized": True,
        })

    def test_fallback_approved_readiness_passes(self) -> None:
        rd.require_published_readiness({
            "classification": "fallback-approved",
            "publish_authorized": True,
        })

    def test_cli_require_published_readiness_exit_codes(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = pathlib.Path(temporary) / "release-readiness.json"
            path.write_text(json.dumps({
                "classification": "blocked",
                "publish_authorized": False,
            }), encoding="utf-8")
            self.assertEqual(1, rd.main(["require-published-readiness", "--readiness", str(path)]))
            path.write_text(json.dumps({
                "classification": "ready",
                "publish_authorized": True,
            }), encoding="utf-8")
            self.assertEqual(0, rd.main(["require-published-readiness", "--readiness", str(path)]))

    def test_cli_classify_writes_github_output_lines(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            work = pathlib.Path(temporary)
            run_path = work / "upstream-run.json"
            jobs_path = work / "upstream-jobs.json"
            output_path = work / "release-disposition.json"
            github_output = work / "github-output.txt"
            handoff_path = work / "ad15.json"
            handoff_path.write_bytes((ROOT / "tests/ci-governance/fixtures/release-verification-handoff-valid.json").read_bytes())
            run_path.write_text(json.dumps(self._run("success")), encoding="utf-8")
            jobs_path.write_text(json.dumps({
                "total_count": 7,
                "jobs": [
                    _job("verify-source", "success"),
                    _job("plan-release", "success"),
                    _job("prepare-candidate", "success"),
                    _job("release", "success"),
                    _job("release / release", "success"),
                    _job("verify-publication", "success"),
                    _job("emit-verification-handoff", "success"),
                ],
            }), encoding="utf-8")
            status = rd.main([
                "classify",
                "--run", str(run_path),
                "--jobs", str(jobs_path),
                "--expected-run-id", "42",
                "--expected-run-attempt", "1",
                "--expected-conclusion", "success",
                "--expected-head-sha", SHA,
                "--ad15-handoff", str(handoff_path),
                "--output", str(output_path),
                "--github-output", str(github_output),
            ])
            self.assertEqual(0, status)
            lines = github_output.read_text(encoding="utf-8").splitlines()
            self.assertIn("governed-attempt=true", lines)
            self.assertIn("disposition=governed-publication-attempt", lines)
            self.assertIn(f"candidate={SHA}", lines)

    def test_cli_rejects_malformed_and_foreign_attempt_handoffs(self) -> None:
        valid = json.loads((ROOT / "tests/ci-governance/fixtures/release-verification-handoff-valid.json").read_bytes())
        invalid = [{"candidate": SHA}, {**valid, "unknown": True}, {**valid, "schema": "forged"}]
        for key, value in (("repository", "github.com/other/repository"),
                           ("workflow_path", ".github/workflows/ci.yml"),
                           ("run_id", 43), ("run_attempt", 2), ("conclusion", "failure")):
            handoff = copy.deepcopy(valid)
            handoff["release_run"][key] = value
            invalid.append(handoff)
        with tempfile.TemporaryDirectory() as temporary:
            work = pathlib.Path(temporary)
            (work / "run.json").write_text(json.dumps(self._run()), encoding="utf-8")
            (work / "jobs.json").write_text(json.dumps({"total_count": 4, "jobs": [
                _job("verify-source", "success"), _job("plan-release", "success"),
                _job("release / publish", "success"), _job("emit-verification-handoff", "success"),
            ]}), encoding="utf-8")
            command = ["classify", "--run", str(work / "run.json"), "--jobs", str(work / "jobs.json"),
                       "--expected-run-id", "42", "--expected-run-attempt", "1", "--expected-conclusion", "success",
                       "--expected-head-sha", SHA, "--ad15-handoff", str(work / "handoff.json"),
                       "--output", str(work / "output.json"), "--github-output", str(work / "github-output")]
            for handoff in invalid:
                with self.subTest(handoff=handoff):
                    (work / "handoff.json").write_text(json.dumps(handoff), encoding="utf-8")
                    self.assertEqual(1, rd.main(command))
                    self.assertFalse((work / "output.json").exists())
                    self.assertFalse((work / "github-output").exists())
            (work / "handoff.json").write_text(json.dumps(valid), encoding="utf-8")
            run = self._run()
            run["repository"]["full_name"] = "Other/Repository"
            (work / "run.json").write_text(json.dumps(run), encoding="utf-8")
            self.assertEqual(1, rd.main(command))
            self.assertFalse((work / "output.json").exists())
            self.assertFalse((work / "github-output").exists())

    def test_cli_missing_or_malformed_governed_candidate_does_not_emit_success(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            work = pathlib.Path(temporary)
            run_path = work / "run.json"
            jobs_path = work / "jobs.json"
            handoff_path = work / "handoff.json"
            output = work / "output.json"
            github_output = work / "github-output.txt"
            run_path.write_text(json.dumps(self._run()), encoding="utf-8")
            jobs_path.write_text(json.dumps({"total_count": 4, "jobs": [
                _job("verify-source", "success"), _job("plan-release", "success"),
                _job("release / publish", "success"), _job("emit-verification-handoff", "success"),
            ]}), encoding="utf-8")
            command = [
                "classify", "--run", str(run_path), "--jobs", str(jobs_path),
                "--expected-run-id", "42", "--expected-run-attempt", "1",
                "--expected-conclusion", "success", "--expected-head-sha", SHA,
                "--ad15-handoff", str(handoff_path), "--output", str(output),
                "--github-output", str(github_output),
            ]
            for payload in (None, '{}', '{"candidate":true}', '{"candidate":""}', '{"candidate":'):
                with self.subTest(payload=payload):
                    if payload is not None:
                        handoff_path.write_text(payload, encoding="utf-8")
                    self.assertEqual(1, rd.main(command))
                    self.assertFalse(output.exists())
                    self.assertFalse(github_output.exists())


if __name__ == "__main__":
    unittest.main()
