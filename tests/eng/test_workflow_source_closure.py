#!/usr/bin/env python3
"""Exact-blob workflow/composite closure tests for GOV-1 AD-12/AD-13."""

from __future__ import annotations

import copy
import hashlib
import json
import pathlib
import subprocess
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))

import workflow_source_closure as wsc  # noqa: E402


def _git(arguments: list[str], cwd: pathlib.Path) -> bytes:
    process = subprocess.run(
        ["git", *arguments],
        cwd=str(cwd),
        capture_output=True,
        check=False,
    )
    if process.returncode != 0:
        raise RuntimeError(
            f"git {arguments} failed in {cwd}: "
            f"{process.stderr.decode('utf-8', 'replace')}"
        )
    return process.stdout


class GitRepo:
    def __init__(self, root: pathlib.Path) -> None:
        self.root = root
        root.mkdir(parents=True)
        _git(["init", "--quiet", "-b", "main"], root)
        _git(["config", "user.email", "test@example.com"], root)
        _git(["config", "user.name", "Test"], root)

    def write(self, relative_path: str, content: str | bytes) -> bytes:
        data = content.encode("utf-8") if isinstance(content, str) else content
        path = self.root / relative_path
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        _git(["add", "--", relative_path], self.root)
        return data

    def commit(self, message: str = "fixture") -> str:
        _git(["-c", "commit.gpgsign=false", "commit", "--quiet", "-m", message], self.root)
        return _git(["rev-parse", "HEAD"], self.root).decode("ascii").strip()


class ClosureFixture:
    def __init__(self, root: pathlib.Path) -> None:
        self.root = root
        self.repos: dict[str, GitRepo] = {}

    @staticmethod
    def identity(name: str) -> str:
        return f"github.com/test/{name.lower()}"

    @staticmethod
    def external(name: str, path: str, commit: str) -> str:
        suffix = f"/{path}" if path else ""
        return f"Test/{name}{suffix}@{commit}"

    def add_repo(self, name: str) -> GitRepo:
        repository = GitRepo(self.root / name)
        self.repos[name] = repository
        return repository

    def stores(self) -> dict[str, pathlib.Path]:
        return {self.identity(name): repository.root for name, repository in self.repos.items()}

    def coordinate(self, name: str, commit: str, path: str) -> dict[str, str]:
        return {
            "repository": self.identity(name),
            "workflow_path": path,
            "commit": commit,
        }


class WorkflowSourceClosureTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self._temporary.cleanup)
        self.temp_path = pathlib.Path(self._temporary.name)

    def _build_nested_fixture(
        self,
    ) -> tuple[ClosureFixture, dict[str, str], dict[str, str], dict[str, bytes]]:
        fixture = ClosureFixture(self.temp_path)

        javascript = fixture.add_repo("Javascript")
        javascript_metadata = javascript.write(
            "action.yml",
            "name: JavaScript leaf\n"
            "runs:\n"
            "  using: node20\n"
            "  main: index.js\n",
        )
        javascript.write("index.js", "console.log('leaf');\n")
        javascript_commit = javascript.commit("javascript leaf")

        descendant = fixture.add_repo("Descendant")
        descendant_metadata = descendant.write(
            "nested/action.yaml",
            "name: External composite\n"
            "runs:\n"
            "  using: composite\n"
            "  steps:\n"
            "    - if: ${{ false }}\n"
            f"      uses: {fixture.external('Javascript', '', javascript_commit)}\n",
        )
        descendant_commit = descendant.commit("composite descendant")

        reusable = fixture.add_repo("Reusable")
        local_metadata = reusable.write(
            ".github/actions/local/action.yml",
            "name: Local composite\n"
            "runs:\n"
            "  using: composite\n"
            "  steps:\n"
            "    - name: Conditional external descendant\n"
            "      if: false\n"
            f"      uses: '{fixture.external('Descendant', 'nested', descendant_commit)}'\n",
        )
        reusable_workflow = reusable.write(
            ".github/workflows/reusable.yml",
            "name: Reusable\n"
            "on:\n"
            "  workflow_call:\n"
            "jobs:\n"
            "  build:\n"
            "    runs-on: ubuntu-latest\n"
            "    steps:\n"
            "      - uses: ./.github/actions/local\n"
            "      - uses: ./.github/actions/local # duplicate must deduplicate\n",
        )
        reusable_commit = reusable.commit("reusable workflow")

        caller = fixture.add_repo("Caller")
        caller_workflow = caller.write(
            ".github/workflows/ci.yml",
            "name: CI\n"
            "on: push\n"
            "jobs:\n"
            "  governed:\n"
            "    if: false\n"
            f"    uses: {fixture.external('Reusable', '.github/workflows/reusable.yml', reusable_commit)}\n",
        )
        caller_commit = caller.commit("caller workflow")

        caller_coordinate = fixture.coordinate(
            "Caller",
            caller_commit,
            ".github/workflows/ci.yml",
        )
        reusable_coordinate = fixture.coordinate(
            "Reusable",
            reusable_commit,
            ".github/workflows/reusable.yml",
        )
        blobs = {
            "caller": caller_workflow,
            "reusable": reusable_workflow,
            "local": local_metadata,
            "descendant": descendant_metadata,
            "javascript": javascript_metadata,
        }
        return fixture, caller_coordinate, reusable_coordinate, blobs

    def _collect_nested(self) -> tuple[dict, ClosureFixture, dict[str, bytes]]:
        fixture, caller, reusable, blobs = self._build_nested_fixture()
        closure = wsc.collect_workflow_source_closure(
            fixture.stores(),
            caller,
            reusable,
        )
        return closure, fixture, blobs

    def test_closure_conditional_nested_composites_includes_exact_sorted_sources(self) -> None:
        closure, fixture, blobs = self._collect_nested()

        self.assertEqual(
            closure["caller"]["blob_sha256"],
            hashlib.sha256(blobs["caller"]).hexdigest(),
        )
        self.assertEqual(
            closure["reusable"]["blob_sha256"],
            hashlib.sha256(blobs["reusable"]).hexdigest(),
        )
        self.assertEqual(len(closure["actions"]), 3)
        self.assertEqual(
            [source["repository"] for source in closure["actions"]],
            [
                fixture.identity("Descendant"),
                fixture.identity("Javascript"),
                fixture.identity("Reusable"),
            ],
        )
        observed_hashes = {source["blob_sha256"] for source in closure["actions"]}
        self.assertEqual(
            observed_hashes,
            {
                hashlib.sha256(blobs["local"]).hexdigest(),
                hashlib.sha256(blobs["descendant"]).hexdigest(),
                hashlib.sha256(blobs["javascript"]).hexdigest(),
            },
        )
        material = {key: closure[key] for key in ("caller", "reusable", "actions")}
        expected_digest = hashlib.sha256(
            json.dumps(
                material,
                ensure_ascii=True,
                allow_nan=False,
                sort_keys=True,
                separators=(",", ":"),
            ).encode("utf-8"),
        ).hexdigest()
        self.assertEqual(closure["definition_digest"], expected_digest)

    def test_closure_worktree_drift_does_not_change_exact_blob_hash(self) -> None:
        fixture, caller, reusable, blobs = self._build_nested_fixture()
        (fixture.repos["Reusable"].root / ".github/actions/local/action.yml").write_text(
            "runs:\n  using: docker\n",
            encoding="utf-8",
        )

        closure = wsc.collect_workflow_source_closure(
            fixture.stores(),
            caller,
            reusable,
        )

        local = next(
            source
            for source in closure["actions"]
            if source["repository"] == fixture.identity("Reusable")
        )
        self.assertEqual(local["blob_sha256"], hashlib.sha256(blobs["local"]).hexdigest())

    def test_authorization_projection_exact_match_is_required(self) -> None:
        closure, _fixture, _blobs = self._collect_nested()
        authorization = wsc.project_policy_authorization("ci", closure)
        policy = {
            "evaluator_authorizations": {
                "ci": [authorization],
                "release": [],
                "post_release": [],
            }
        }

        self.assertEqual(
            wsc.require_policy_authorization(policy, "ci", closure),
            authorization,
        )

        unapproved = copy.deepcopy(policy)
        unapproved["evaluator_authorizations"]["ci"] = []
        with self.assertRaisesRegex(wsc.WorkflowClosureError, "authorizes 0"):
            wsc.require_policy_authorization(unapproved, "ci", closure)

        malformed = copy.deepcopy(policy)
        malformed["evaluator_authorizations"]["ci"][0]["closure_digest"] = "0" * 64
        with self.assertRaisesRegex(wsc.WorkflowClosureError, "canonical digest mismatch"):
            wsc.require_policy_authorization(malformed, "ci", closure)

    def _minimal_pair(
        self,
        caller_extra: str = "",
        reusable_extra: str = "",
        root: pathlib.Path | None = None,
    ) -> tuple[ClosureFixture, dict[str, str], dict[str, str]]:
        fixture = ClosureFixture(self.temp_path if root is None else root)
        reusable = fixture.add_repo("Reusable")
        reusable.write(
            ".github/workflows/reusable.yml",
            "on:\n  workflow_call:\n"
            "jobs:\n"
            "  build:\n"
            "    runs-on: ubuntu-latest\n"
            f"{reusable_extra}",
        )
        reusable_commit = reusable.commit("reusable")

        caller = fixture.add_repo("Caller")
        caller.write(
            ".github/workflows/ci.yml",
            "on: push\n"
            "jobs:\n"
            "  call:\n"
            f"    uses: {fixture.external('Reusable', '.github/workflows/reusable.yml', reusable_commit)}\n"
            f"{caller_extra}",
        )
        caller_commit = caller.commit("caller")
        return (
            fixture,
            fixture.coordinate("Caller", caller_commit, ".github/workflows/ci.yml"),
            fixture.coordinate(
                "Reusable",
                reusable_commit,
                ".github/workflows/reusable.yml",
            ),
        )

    def test_mutable_and_dynamic_external_refs_fail_closed(self) -> None:
        for bad_uses in (
            "Test/Reusable/.github/workflows/reusable.yml@main",
            "Test/Reusable/.github/workflows/reusable.yml@${{ github.sha }}",
        ):
            with self.subTest(uses=bad_uses):
                case_root = self.temp_path / hashlib.sha256(bad_uses.encode()).hexdigest()[:8]
                fixture = ClosureFixture(case_root)
                reusable = fixture.add_repo("Reusable")
                reusable.write(
                    ".github/workflows/reusable.yml",
                    "on:\n  workflow_call:\njobs: {}\n",
                )
                reusable_commit = reusable.commit()
                caller = fixture.add_repo("Caller")
                caller.write(
                    ".github/workflows/ci.yml",
                    f"jobs:\n  call:\n    uses: {bad_uses}\n",
                )
                caller_commit = caller.commit()
                with self.assertRaises(wsc.WorkflowClosureError):
                    wsc.collect_workflow_source_closure(
                        fixture.stores(),
                        fixture.coordinate("Caller", caller_commit, ".github/workflows/ci.yml"),
                        fixture.coordinate(
                            "Reusable",
                            reusable_commit,
                            ".github/workflows/reusable.yml",
                        ),
                    )

    def test_docker_references_and_docker_metadata_fail_closed(self) -> None:
        fixture, caller, reusable = self._minimal_pair(
            caller_extra="  containerized:\n    uses: docker://alpine:3\n",
        )
        with self.assertRaisesRegex(wsc.WorkflowClosureError, "Docker"):
            wsc.collect_workflow_source_closure(fixture.stores(), caller, reusable)

        second_root = self.temp_path / "metadata"
        fixture = ClosureFixture(second_root)
        action = fixture.add_repo("Action")
        action.write("action.yml", "runs:\n  using: docker\n  image: Dockerfile\n")
        action_commit = action.commit()
        reusable_repo = fixture.add_repo("Reusable")
        reusable_repo.write(
            ".github/workflows/reusable.yml",
            "on:\n  workflow_call:\n"
            "jobs:\n"
            "  build:\n"
            "    runs-on: ubuntu-latest\n"
            "    steps:\n"
            f"      - uses: {fixture.external('Action', '', action_commit)}\n",
        )
        reusable_commit = reusable_repo.commit()
        caller_repo = fixture.add_repo("Caller")
        caller_repo.write(
            ".github/workflows/ci.yml",
            "jobs:\n"
            "  call:\n"
            f"    uses: {fixture.external('Reusable', '.github/workflows/reusable.yml', reusable_commit)}\n",
        )
        caller_commit = caller_repo.commit()
        with self.assertRaisesRegex(wsc.WorkflowClosureError, "Docker"):
            wsc.collect_workflow_source_closure(
                fixture.stores(),
                fixture.coordinate("Caller", caller_commit, ".github/workflows/ci.yml"),
                fixture.coordinate(
                    "Reusable",
                    reusable_commit,
                    ".github/workflows/reusable.yml",
                ),
            )

    def test_unsupported_yaml_uses_forms_fail_closed(self) -> None:
        invalid_fragments = (
            "  invalid:\n    steps: [{ uses: Test/Action@" + "1" * 40 + " }]\n",
            "  invalid:\n    uses: |\n      Test/Action@" + "1" * 40 + "\n",
            "  invalid: &shared\n    uses: Test/Action@" + "1" * 40 + "\n",
            "  invalid:\n    'uses': Test/Action@" + "1" * 40 + "\n",
        )
        for index, fragment in enumerate(invalid_fragments):
            with self.subTest(fragment=fragment):
                case_root = self.temp_path / f"unsupported-{index}"
                fixture, caller, reusable = self._minimal_pair(
                    caller_extra=fragment,
                    root=case_root,
                )
                with self.assertRaises(wsc.WorkflowClosureError):
                    wsc.collect_workflow_source_closure(fixture.stores(), caller, reusable)

    def test_missing_and_ambiguous_action_metadata_fail_closed(self) -> None:
        for names in (("README.md",), ("action.yml", "action.yaml")):
            with self.subTest(names=names):
                case_root = self.temp_path / ("-".join(names).replace(".", "_"))
                fixture = ClosureFixture(case_root)
                action = fixture.add_repo("Action")
                for name in names:
                    action.write(name, "runs:\n  using: node20\n  main: index.js\n")
                action_commit = action.commit()
                reusable_repo = fixture.add_repo("Reusable")
                reusable_repo.write(
                    ".github/workflows/reusable.yml",
                    "on:\n  workflow_call:\n"
                    "jobs:\n"
                    "  build:\n"
                    "    runs-on: ubuntu-latest\n"
                    "    steps:\n"
                    f"      - uses: {fixture.external('Action', '', action_commit)}\n",
                )
                reusable_commit = reusable_repo.commit()
                caller_repo = fixture.add_repo("Caller")
                caller_repo.write(
                    ".github/workflows/ci.yml",
                    "jobs:\n"
                    "  call:\n"
                    f"    uses: {fixture.external('Reusable', '.github/workflows/reusable.yml', reusable_commit)}\n",
                )
                caller_commit = caller_repo.commit()
                with self.assertRaises(wsc.WorkflowClosureError):
                    wsc.collect_workflow_source_closure(
                        fixture.stores(),
                        fixture.coordinate(
                            "Caller",
                            caller_commit,
                            ".github/workflows/ci.yml",
                        ),
                        fixture.coordinate(
                            "Reusable",
                            reusable_commit,
                            ".github/workflows/reusable.yml",
                        ),
                    )

    def test_composite_cycle_fails_closed(self) -> None:
        fixture = ClosureFixture(self.temp_path)
        reusable_repo = fixture.add_repo("Reusable")
        reusable_repo.write(
            ".github/actions/a/action.yml",
            "runs:\n  using: composite\n  steps:\n    - uses: ./.github/actions/b\n",
        )
        reusable_repo.write(
            ".github/actions/b/action.yml",
            "runs:\n  using: composite\n  steps:\n    - uses: ./.github/actions/a\n",
        )
        reusable_repo.write(
            ".github/workflows/reusable.yml",
            "on:\n  workflow_call:\n"
            "jobs:\n"
            "  build:\n"
            "    runs-on: ubuntu-latest\n"
            "    steps:\n"
            "      - uses: ./.github/actions/a\n",
        )
        reusable_commit = reusable_repo.commit()
        caller_repo = fixture.add_repo("Caller")
        caller_repo.write(
            ".github/workflows/ci.yml",
            "jobs:\n"
            "  call:\n"
            f"    uses: {fixture.external('Reusable', '.github/workflows/reusable.yml', reusable_commit)}\n",
        )
        caller_commit = caller_repo.commit()

        with self.assertRaisesRegex(wsc.WorkflowClosureError, "cycle"):
            wsc.collect_workflow_source_closure(
                fixture.stores(),
                fixture.coordinate("Caller", caller_commit, ".github/workflows/ci.yml"),
                fixture.coordinate(
                    "Reusable",
                    reusable_commit,
                    ".github/workflows/reusable.yml",
                ),
            )

    def test_ad13_depth_source_blob_and_total_limits_fail_closed(self) -> None:
        fixture, caller, reusable, blobs = self._build_nested_fixture()
        cases = {
            "depth": {**wsc.DEFAULT_LIMITS, "max_workflow_closure_depth": 0},
            "sources": {**wsc.DEFAULT_LIMITS, "max_workflow_closure_sources": 1},
            "blob": {
                **wsc.DEFAULT_LIMITS,
                "max_workflow_source_blob_bytes": len(blobs["caller"]) - 1,
            },
            "total": {
                **wsc.DEFAULT_LIMITS,
                "max_workflow_source_total_bytes": len(blobs["caller"]),
            },
        }
        for name, limits in cases.items():
            with self.subTest(limit=name):
                with self.assertRaises(wsc.WorkflowClosureError):
                    wsc.collect_workflow_source_closure(
                        fixture.stores(),
                        caller,
                        reusable,
                        limits=limits,
                    )

    def test_unexpected_reusable_coordinate_and_unknown_repository_fail_closed(self) -> None:
        fixture, caller, reusable = self._minimal_pair()
        wrong = dict(reusable)
        wrong["workflow_path"] = ".github/workflows/other.yml"
        with self.assertRaisesRegex(wsc.WorkflowClosureError, "expected reusable"):
            wsc.collect_workflow_source_closure(fixture.stores(), caller, wrong)

        unknown_stores = fixture.stores()
        del unknown_stores[reusable["repository"]]
        with self.assertRaisesRegex(wsc.WorkflowClosureError, "trusted exact-object store"):
            wsc.collect_workflow_source_closure(unknown_stores, caller, reusable)

    def test_policy_limit_projection_requires_all_ad13_values(self) -> None:
        policy = {"resource_limits": dict(wsc.DEFAULT_LIMITS)}
        self.assertEqual(wsc.closure_limits_from_policy(policy), wsc.DEFAULT_LIMITS)

        del policy["resource_limits"]["max_workflow_source_total_bytes"]
        with self.assertRaisesRegex(wsc.WorkflowClosureError, "missing workflow-closure limits"):
            wsc.closure_limits_from_policy(policy)


if __name__ == "__main__":
    unittest.main()
