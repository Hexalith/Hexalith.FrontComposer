#!/usr/bin/env python3
"""Negative contracts for the manual exact-source production release boundary."""

from __future__ import annotations

import copy
import json
import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))

import release_contract as rc  # noqa: E402


class ReleaseContractTests(unittest.TestCase):
    sha = "a" * 40

    def _ref(self) -> dict:
        return {"ref": "refs/heads/main", "object": {"type": "commit", "sha": self.sha}}

    def _runs(self) -> dict:
        return {
            "total_count": 1,
            "workflow_runs": [{
                "id": 101,
                "run_attempt": 2,
                "event": "push",
                "head_branch": "main",
                "head_sha": self.sha,
                "status": "completed",
                "conclusion": "success",
                "path": ".github/workflows/ci.yml",
            }],
        }

    def test_selects_one_successful_exact_source_push_ci(self) -> None:
        self.assertEqual(rc.select_exact_ci_run(self.sha, self._ref(), self._runs()), {"run_id": 101, "run_attempt": 2})

    def test_rejects_stale_main_missing_or_failed_ci_and_malformed_api(self) -> None:
        cases = []
        stale = self._ref()
        stale["object"]["sha"] = "b" * 40
        cases.append((stale, self._runs()))
        missing = self._runs()
        missing["total_count"] = 0
        missing["workflow_runs"] = []
        cases.append((self._ref(), missing))
        failed = self._runs()
        failed["workflow_runs"][0]["conclusion"] = "failure"
        cases.append((self._ref(), failed))
        cases.append((self._ref(), {"total_count": "one", "workflow_runs": {}}))
        duplicate = self._runs()
        duplicate["total_count"] = 2
        duplicate["workflow_runs"].append(copy.deepcopy(duplicate["workflow_runs"][0]))
        cases.append((self._ref(), duplicate))
        for live_ref, runs in cases:
            with self.subTest(live_ref=live_ref, runs=runs):
                with self.assertRaises(rc.ContractError):
                    rc.select_exact_ci_run(self.sha, live_ref, runs)

    def test_package_manifest_rejects_count_and_identity_drift(self) -> None:
        rc.validate_package_manifest(ROOT, ROOT / "tools/release-packages.json", 8)
        manifest = json.loads((ROOT / "tools/release-packages.json").read_text())
        with tempfile.TemporaryDirectory() as temporary:
            path = pathlib.Path(temporary) / "manifest.json"
            path.write_text(json.dumps({"packages": manifest["packages"][:-1]}))
            with self.assertRaises(rc.ContractError):
                rc.validate_package_manifest(ROOT, path, 8)
            drift = copy.deepcopy(manifest)
            drift["packages"][0]["id"] = "Hexalith.FrontComposer.Substituted"
            path.write_text(json.dumps(drift))
            with self.assertRaises(rc.ContractError):
                rc.validate_package_manifest(ROOT, path, 8)

    def test_publication_requires_non_draft_release_and_exact_tag_sha(self) -> None:
        release = {"id": 10, "tag_name": "v1.2.3", "draft": False, "immutable": True, "assets": []}
        tag_ref = {"ref": "refs/tags/v1.2.3", "object": {"type": "commit", "sha": self.sha}}
        rc.validate_publication(self.sha, "v1.2.3", release, tag_ref, [], require_immutable=True)
        cases = [
            (None, tag_ref, []),
            ({**release, "draft": True}, tag_ref, []),
            ({**release, "tag_name": "v1.2.4"}, tag_ref, []),
            (release, {"ref": "refs/tags/v1.2.3", "object": {"type": "commit", "sha": "b" * 40}}, []),
            (release, {"ref": "refs/tags/v1.2.3", "object": {"type": "tag", "sha": "c" * 40}}, []),
        ]
        for changed_release, changed_ref, tag_objects in cases:
            with self.subTest(release=changed_release, tag_ref=changed_ref):
                with self.assertRaises(rc.ContractError):
                    rc.validate_publication(self.sha, "v1.2.3", changed_release, changed_ref, tag_objects, require_immutable=True)

    def test_builds_identity_rejects_mismatched_workflow_input_or_gitlink(self) -> None:
        approved = "a" * 40
        workflow = (
            "uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@" + approved + "\n"
            "      builds-execution-sha: " + approved + "\n"
        )
        rc.validate_builds_identity(workflow, approved, approved)
        for changed_workflow, gitlink in (
            (workflow.replace(approved, "b" * 40, 1), approved),
            (workflow.replace(approved, "b" * 40), approved),
            (workflow, "b" * 40),
        ):
            with self.assertRaises(rc.ContractError):
                rc.validate_builds_identity(changed_workflow, gitlink, approved)


if __name__ == "__main__":
    unittest.main()
