#!/usr/bin/env python3
"""Runtime proofs for release disposition and retroactive-auth gates."""

from __future__ import annotations

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
        }

    def test_completed_release_job_is_governed_attempt(self) -> None:
        jobs = [
            _job("verify-source", "success"),
            _job("plan-release", "success"),
            _job("prepare-candidate", "success"),
            _job("release", "success"),
            _job("release / release", "success"),
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
        )
        self.assertTrue(result["governed_attempt"])
        self.assertEqual(result["status"], "governed-publication-attempt")
        self.assertEqual(result["candidate"], SHA)

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

    def test_ad15_candidate_preferred_over_head_sha(self) -> None:
        jobs = [
            _job("verify-source", "success"),
            _job("plan-release", "success"),
            _job("release / release", "failure"),
            _job("emit-verification-handoff", "success"),
        ]
        ad15 = "b" * 40
        result = rd.classify_release_run(
            run=self._run("failure"),
            jobs=jobs,
            expected_run_id=42,
            expected_run_attempt=1,
            expected_conclusion="failure",
            expected_head_sha=SHA,
            ad15_candidate=ad15,
        )
        self.assertTrue(result["governed_attempt"])
        self.assertEqual(result["candidate"], ad15)

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
                "--output", str(output_path),
                "--github-output", str(github_output),
            ])
            self.assertEqual(0, status)
            lines = github_output.read_text(encoding="utf-8").splitlines()
            self.assertIn("governed-attempt=true", lines)
            self.assertIn("disposition=governed-publication-attempt", lines)
            self.assertIn(f"candidate={SHA}", lines)


if __name__ == "__main__":
    unittest.main()
