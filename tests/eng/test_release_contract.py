#!/usr/bin/env python3
"""Negative contracts for the manual exact-source production release boundary."""

from __future__ import annotations

import copy
import contextlib
import io
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
import unittest
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))

import release_contract as rc  # noqa: E402

PLANNER = ROOT / "eng" / "semantic-release-plan.mjs"
WORKFLOW = ROOT / ".github" / "workflows" / "release.yml"


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
        release = {
            "id": 10,
            "tag_name": "v1.2.3",
            "draft": False,
            "immutable": True,
            "assets": [{"id": 1, "name": "pkg.nupkg"}],
        }
        tag_ref = {"ref": "refs/tags/v1.2.3", "object": {"type": "commit", "sha": self.sha}}
        rc.validate_publication(self.sha, "v1.2.3", release, tag_ref, [], require_immutable=True)
        cases = [
            (None, tag_ref, []),
            ({**release, "draft": True}, tag_ref, []),
            ({**release, "tag_name": "v1.2.4"}, tag_ref, []),
            ({**release, "immutable": False}, tag_ref, []),
            ({**release, "assets": []}, tag_ref, []),
            (release, {"ref": "refs/tags/v1.2.3", "object": {"type": "commit", "sha": "b" * 40}}, []),
            (release, {"ref": "refs/tags/v1.2.3", "object": {"type": "tag", "sha": "c" * 40}}, []),
        ]
        for changed_release, changed_ref, tag_objects in cases:
            with self.subTest(release=changed_release, tag_ref=changed_ref):
                with self.assertRaises(rc.ContractError):
                    rc.validate_publication(self.sha, "v1.2.3", changed_release, changed_ref, tag_objects, require_immutable=True)

    def test_publication_cli_require_immutable_rejects_mutable_and_empty_assets(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            work = pathlib.Path(temporary)
            tag_ref = {"ref": "refs/tags/v1.2.3", "object": {"type": "commit", "sha": self.sha}}
            (work / "tag-ref.json").write_text(json.dumps(tag_ref), encoding="utf-8")
            (work / "tag-objects.json").write_text("[]\n", encoding="utf-8")
            for label, release in (
                ("mutable", {
                    "id": 10,
                    "tag_name": "v1.2.3",
                    "draft": False,
                    "immutable": False,
                    "assets": [{"id": 1, "name": "pkg.nupkg"}],
                }),
                ("empty-assets", {
                    "id": 10,
                    "tag_name": "v1.2.3",
                    "draft": False,
                    "immutable": True,
                    "assets": [],
                }),
            ):
                with self.subTest(label=label):
                    release_path = work / f"{label}.json"
                    release_path.write_text(json.dumps(release), encoding="utf-8")
                    status = rc.main([
                        "publication",
                        "--expected-sha", self.sha,
                        "--expected-tag", "v1.2.3",
                        "--release", str(release_path),
                        "--tag-ref", str(work / "tag-ref.json"),
                        "--tag-objects", str(work / "tag-objects.json"),
                        "--require-immutable",
                    ])
                    self.assertEqual(1, status)

    def test_builds_identity_accepts_diverged_gitlink_and_rejects_pin_mismatch(self) -> None:
        approved = "a" * 40
        catalog = "b" * 40
        workflow = (
            "uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@" + approved + "\n"
            "      builds-execution-sha: " + approved + "\n"
        )
        rc.validate_builds_identity(workflow, approved, approved)
        rc.validate_builds_identity(workflow, catalog, approved)
        for changed_workflow in (
            workflow.replace(approved, catalog, 1),
            workflow.replace(approved, catalog),
        ):
            with self.assertRaises(rc.ContractError):
                rc.validate_builds_identity(changed_workflow, catalog, approved)

    def test_builds_identity_rejects_malformed_catalog_or_execution_sha(self) -> None:
        approved = "a" * 40
        workflow = (
            "uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@" + approved + "\n"
            "      builds-execution-sha: " + approved + "\n"
        )
        for catalog, execution in (("b" * 39, approved), ("B" * 40, approved), ("b" * 40, "A" * 40)):
            with self.subTest(catalog=catalog, execution=execution):
                with self.assertRaises(rc.ContractError):
                    rc.validate_builds_identity(workflow, catalog, execution)

    def test_builds_cli_reports_distinct_catalog_and_execution_sha(self) -> None:
        approved = "a" * 40
        catalog = "b" * 40
        workflow = (
            "uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@" + approved + "\n"
            "      builds-execution-sha: " + approved + "\n"
        )
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True, capture_output=True)
            subprocess.run(["git", "config", "user.email", "builds-identity@example.test"], cwd=root, check=True)
            subprocess.run(["git", "config", "user.name", "Builds Identity Fixture"], cwd=root, check=True)
            workflow_path = root / ".github" / "workflows" / "release.yml"
            workflow_path.parent.mkdir(parents=True, exist_ok=True)
            workflow_path.write_text(workflow, encoding="utf-8")
            subprocess.run(["git", "add", "."], cwd=root, check=True, capture_output=True)
            subprocess.run(
                ["git", "update-index", "--add", "--cacheinfo", f"160000,{catalog},references/Hexalith.Builds"],
                cwd=root,
                check=True,
                capture_output=True,
            )
            subprocess.run(
                ["git", "commit", "-q", "-m", "test: seed divergent Builds identities"],
                cwd=root,
                check=True,
                capture_output=True,
            )
            commit = subprocess.run(
                ["git", "rev-parse", "HEAD"],
                cwd=root,
                check=True,
                capture_output=True,
                text=True,
            ).stdout.strip()
            stdout = io.StringIO()
            with contextlib.redirect_stdout(stdout):
                status = rc.main(["builds", "--root", str(root), "--commit", commit, "--approved", approved])
            self.assertEqual(0, status)
            self.assertEqual(
                {"ok": True, "builds_catalog_sha": catalog, "builds_execution_sha": approved},
                json.loads(stdout.getvalue()),
            )
    def test_builds_cli_rejects_missing_gitlink(self) -> None:
        approved = "a" * 40
        workflow = (
            "uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@" + approved + "\n"
            "      builds-execution-sha: " + approved + "\n"
        )
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True, capture_output=True)
            subprocess.run(["git", "config", "user.email", "builds-identity@example.test"], cwd=root, check=True)
            subprocess.run(["git", "config", "user.name", "Builds Identity Fixture"], cwd=root, check=True)
            workflow_path = root / ".github" / "workflows" / "release.yml"
            workflow_path.parent.mkdir(parents=True, exist_ok=True)
            workflow_path.write_text(workflow, encoding="utf-8")
            subprocess.run(["git", "add", "."], cwd=root, check=True, capture_output=True)
            subprocess.run(
                ["git", "commit", "-q", "-m", "test: seed release workflow without Builds gitlink"],
                cwd=root,
                check=True,
                capture_output=True,
            )
            commit = subprocess.run(
                ["git", "rev-parse", "HEAD"],
                cwd=root,
                check=True,
                capture_output=True,
                text=True,
            ).stdout.strip()
            stderr = io.StringIO()
            with contextlib.redirect_stderr(stderr):
                status = rc.main(["builds", "--root", str(root), "--commit", commit, "--approved", approved])
            self.assertEqual(1, status)
            self.assertIn("cannot resolve the candidate Builds gitlink", stderr.getvalue())

    def test_nuget_content_accepts_only_repository_signature_difference(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            candidate = root / "candidate.nupkg"
            published = root / "published.nupkg"
            members = {
                "Hexalith.FrontComposer.Contracts.nuspec": b"<package />",
                "lib/net10.0/Hexalith.FrontComposer.Contracts.dll": b"assembly bytes",
            }
            self._write_package(candidate, members)
            self._write_package(published, {**members, rc.NUGET_SIGNATURE_MEMBER: b"repository signature"})

            comparison = rc.compare_nuget_package_content(candidate, published)

            self.assertEqual(2, comparison["member_count"])
            self.assertNotEqual(comparison["candidate_sha256"], comparison["published_sha256"])
            self.assertEqual(rc.NUGET_SIGNATURE_MEMBER, comparison["repository_signature_member"])

    def test_nuget_content_rejects_missing_signature_payload_drift_and_unsafe_members(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            candidate = root / "candidate.nupkg"
            published = root / "published.nupkg"
            self._write_package(candidate, {"package.nuspec": b"original"})
            cases = (
                {"package.nuspec": b"original"},
                {"package.nuspec": b"changed", rc.NUGET_SIGNATURE_MEMBER: b"signature"},
                {"package.nuspec": b"original", "lib/net10.0/Injected.dll": b"injected", rc.NUGET_SIGNATURE_MEMBER: b"signature"},
                {"other.nuspec": b"original", rc.NUGET_SIGNATURE_MEMBER: b"signature"},
                {"../package.nuspec": b"original", rc.NUGET_SIGNATURE_MEMBER: b"signature"},
                {"C:/escape.dll": b"original", rc.NUGET_SIGNATURE_MEMBER: b"signature"},
                {"control\nname.dll": b"original", rc.NUGET_SIGNATURE_MEMBER: b"signature"},
                {"package.nuspec": b"original", "PACKAGE.NUSPEC": b"duplicate", rc.NUGET_SIGNATURE_MEMBER: b"signature"},
                {"package.nuspec": b"original", ".SIGNATURE.P7S": b"ambiguous signature"},
                {"package.nuspec": b"original", rc.NUGET_SIGNATURE_MEMBER: b""},
            )
            for members in cases:
                with self.subTest(members=members):
                    self._write_package(published, members)
                    with self.assertRaises(rc.ContractError):
                        rc.compare_nuget_package_content(candidate, published)

            self._write_package(candidate, {
                "package.nuspec": b"original",
                rc.NUGET_SIGNATURE_MEMBER: b"unexpected author signature",
            })
            self._write_package(published, {
                "package.nuspec": b"original",
                rc.NUGET_SIGNATURE_MEMBER: b"repository signature",
            })
            with self.assertRaises(rc.ContractError):
                rc.compare_nuget_package_content(candidate, published)

    def test_repository_signature_transcript_requires_every_expected_package(self) -> None:
        version = "2.0.0"
        package_ids = ["Hexalith.FrontComposer.Contracts", "Hexalith.FrontComposer.Schema"]
        transcript = "\n".join(
            line
            for package_id in package_ids
            for line in (
                f"Verifying {package_id}.{version}",
                "Signature type: Repository",
                "Service index: https://api.nuget.org/v3/index.json",
                f"Successfully verified package '{package_id}.{version}'.",
            )
        )
        result = rc.validate_repository_signature_transcript(transcript, package_ids, version)
        self.assertEqual(2, result["package_count"])

        for changed in (
            transcript.replace("Signature type: Repository", "Signature type: Author", 1),
            transcript.replace("Service index: https://api.nuget.org/v3/index.json", "Service index: https://packages.example.test/v3/index.json", 1),
            "\n".join(transcript.splitlines()[:4]),
            transcript + "\nSignature type: Repository\nService index: https://api.nuget.org/v3/index.json\nSuccessfully verified package 'Unexpected.Package.2.0.0'.",
        ):
            with self.subTest(transcript=changed):
                with self.assertRaises(rc.ContractError):
                    rc.validate_repository_signature_transcript(changed, package_ids, version)

        with self.assertRaises(rc.ContractError):
            rc.validate_repository_signature_transcript(transcript, [package_ids[0], package_ids[0].upper()], version)

    def test_repository_signature_cli_writes_structured_eight_package_result(self) -> None:
        version = "2.0.0"
        package_ids = [package_id for package_id, _ in rc.EXPECTED_PACKAGES]
        transcript = "\n".join(
            line
            for package_id in package_ids
            for line in (
                f"Verifying {package_id}.{version}",
                "Signature type: Repository",
                "Service index: https://api.nuget.org/v3/index.json",
                f"Successfully verified package '{package_id}.{version}'.",
            )
        )
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            transcript_path = root / "verify.txt"
            output = root / "verify.json"
            transcript_path.write_text(transcript, encoding="utf-8")
            arguments = [
                "repository-signatures",
                "--transcript", str(transcript_path),
                "--version", version,
                "--output", str(output),
            ]
            for package_id in package_ids:
                arguments.extend(["--package-id", package_id])
            with contextlib.redirect_stdout(io.StringIO()):
                result = rc.main(arguments)
            self.assertEqual(0, result)
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertTrue(payload["ok"])
            self.assertEqual(8, payload["package_count"])
            self.assertEqual(package_ids, payload["packages"])

    def test_release_planner_computes_releasable_version_without_external_origin(self) -> None:
        result, temporary_entries, trace = self._run_planner_fixture("fix: correct behavior")

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual('{"release_required":true,"version":"1.0.1"}\n', result.stdout)
        self.assertEqual([], temporary_entries)
        self.assertNotIn("forbidden-external-origin.git", json.dumps(trace, sort_keys=True))

    def test_release_planner_reports_no_release_without_external_origin(self) -> None:
        result, temporary_entries, trace = self._run_planner_fixture("docs: explain behavior")

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual('{"release_required":false,"version":null}\n', result.stdout)
        self.assertEqual([], temporary_entries)
        self.assertNotIn("forbidden-external-origin.git", json.dumps(trace, sort_keys=True))

    def test_release_planner_preserves_minor_and_major_version_rules(self) -> None:
        cases = (
            ("feat: add behavior", "1.1.0"),
            ("feat!: replace behavior\n\nBREAKING CHANGE: replace the contract", "2.0.0"),
        )
        for message, version in cases:
            with self.subTest(message=message):
                result, temporary_entries, trace = self._run_planner_fixture(message)

                self.assertEqual(0, result.returncode, result.stderr)
                self.assertEqual(
                    f'{{"release_required":true,"version":"{version}"}}\n',
                    result.stdout,
                )
                self.assertEqual([], temporary_entries)
                self.assertNotIn(
                    "forbidden-external-origin.git",
                    json.dumps(trace, sort_keys=True),
                )

    def test_release_planner_rejects_shallow_history_and_cleans_temporary_directory(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            source, _ = self._create_planner_repository(root, "fix: correct behavior")
            shallow = root / "shallow"
            planner_temporary = root / "planner-temporary"
            trace_path = root / "git-trace.json"
            planner_temporary.mkdir()
            self._run_git(
                root,
                "clone",
                "--quiet",
                "--depth",
                "1",
                "--branch",
                "main",
                source.as_uri(),
                str(shallow),
            )
            candidate = self._git_output(shallow, "rev-parse", "HEAD")

            result = subprocess.run(
                ["node", str(PLANNER)],
                cwd=shallow,
                check=False,
                capture_output=True,
                text=True,
                env=self._planner_environment(planner_temporary, trace_path, candidate),
            )

            self.assertNotEqual(0, result.returncode)
            self.assertEqual("", result.stdout)
            self.assertIn("complete non-shallow Git history and tags", result.stderr)
            self.assertEqual([], list(planner_temporary.iterdir()))
            self._read_trace(trace_path)

    def test_release_planner_setup_failure_emits_no_plan_and_cleans_temporary_mirror(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            working = root / "not-a-repository"
            planner_temporary = root / "planner-temporary"
            working.mkdir()
            planner_temporary.mkdir()
            env = self._planner_environment(planner_temporary, root / "git-trace.json")

            result = subprocess.run(
                ["node", str(PLANNER)],
                cwd=working,
                check=False,
                capture_output=True,
                text=True,
                env=env,
            )

            self.assertNotEqual(0, result.returncode)
            self.assertEqual("", result.stdout)
            self.assertNotEqual("", result.stderr)
            self.assertEqual([], list(planner_temporary.iterdir()))

    def test_release_planner_post_clone_failure_cleans_populated_mirror(self) -> None:
        if os.name == "nt":
            self.skipTest("the injected Git executable is a POSIX test fixture")

        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            repository, candidate = self._create_planner_repository(root, "fix: correct behavior")
            planner_temporary = root / "planner-temporary"
            trace_path = root / "git-trace.json"
            wrapper_directory = root / "git-wrapper"
            planner_temporary.mkdir()
            wrapper_directory.mkdir()
            real_git = shutil.which("git")
            self.assertIsNotNone(real_git)
            wrapper = wrapper_directory / "git"
            wrapper.write_text(
                "#!/usr/bin/env python3\n"
                "import os\n"
                "import sys\n"
                f"real_git = {real_git!r}\n"
                "if 'update-ref' in sys.argv[1:]:\n"
                "    raise SystemExit(42)\n"
                "os.execv(real_git, [real_git, *sys.argv[1:]])\n",
                encoding="utf-8",
            )
            wrapper.chmod(0o755)
            env = self._planner_environment(planner_temporary, trace_path, candidate)
            env["PATH"] = f"{wrapper_directory}{os.pathsep}{env['PATH']}"

            result = subprocess.run(
                ["node", str(PLANNER)],
                cwd=repository,
                check=False,
                capture_output=True,
                text=True,
                env=env,
            )

            self.assertNotEqual(0, result.returncode)
            self.assertEqual("", result.stdout)
            self.assertIn("update-ref", result.stderr)
            self.assertEqual([], list(planner_temporary.iterdir()))
            self._read_trace(trace_path)

    def test_release_planner_and_workflow_keep_planning_tokenless_and_read_only(self) -> None:
        workflow = WORKFLOW.read_text(encoding="utf-8")
        workflow_preamble = workflow.split("\njobs:\n", 1)[0]
        plan_job = workflow.split("\n  plan-release:\n", 1)[1].split(
            "\n  prepare-candidate:\n", 1
        )[0]
        planner = PLANNER.read_text(encoding="utf-8")

        self.assertNotIn("GITHUB_TOKEN", workflow_preamble)
        self.assertNotIn("GH_TOKEN", workflow_preamble)
        self.assertIn("permissions:\n      contents: read", plan_job)
        self.assertIn("fetch-depth: 0", plan_job)
        self.assertIn("persist-credentials: false", plan_job)
        self.assertIn("- name: Install release tooling", plan_job)
        self.assertNotIn("authenticate", plan_job.lower())
        self.assertNotIn("GITHUB_TOKEN", plan_job)
        self.assertNotIn("GH_TOKEN", plan_job)
        self.assertNotIn("${{ github.token }}", plan_job)
        self.assertIn("delete plannerEnvironment.GITHUB_TOKEN", planner)
        self.assertIn("delete plannerEnvironment.GH_TOKEN", planner)
        self.assertIn("repositoryUrl: pathToFileURL(mirrorPath).href", planner)
        self.assertEqual(
            {
                "@semantic-release/commit-analyzer",
                "@semantic-release/release-notes-generator",
            },
            set(re.findall(r'"(@semantic-release/[^"]+)"', planner)),
        )

    def _run_planner_fixture(
        self,
        change_message: str,
    ) -> tuple[subprocess.CompletedProcess[str], list[pathlib.Path], list[dict[str, object]]]:
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            repository, candidate = self._create_planner_repository(root, change_message)
            planner_temporary = root / "planner-temporary"
            trace_path = root / "git-trace.json"
            planner_temporary.mkdir()
            self._git(repository, "checkout", "--quiet", "--detach", candidate)
            self._git(repository, "branch", "--force", "main", "v1.0.0")
            self._git(repository, "remote", "add", "origin", "file:///forbidden-external-origin.git")

            result = subprocess.run(
                ["node", str(PLANNER)],
                cwd=repository,
                check=False,
                capture_output=True,
                text=True,
                env=self._planner_environment(planner_temporary, trace_path, candidate),
            )
            temporary_entries = list(planner_temporary.iterdir())
            trace = self._read_trace(trace_path)
            return result, temporary_entries, trace

    @staticmethod
    def _planner_environment(
        temporary: pathlib.Path,
        trace_path: pathlib.Path,
        candidate: str | None = None,
    ) -> dict[str, str]:
        env = os.environ.copy()
        env["GH_TOKEN"] = "forbidden-gh-token"
        env["GITHUB_TOKEN"] = "forbidden-github-token"
        env["GITHUB_ACTION"] = "release-plan-test"
        env["GITHUB_ACTIONS"] = "true"
        env["GITHUB_EVENT_NAME"] = "workflow_dispatch"
        env["GITHUB_REF"] = "refs/heads/main"
        env["GITHUB_REF_NAME"] = "main"
        if candidate is not None:
            env["GITHUB_SHA"] = candidate
        env["TMPDIR"] = str(temporary)
        env["GIT_TRACE2_EVENT"] = str(trace_path)
        return env

    @classmethod
    def _create_planner_repository(
        cls,
        root: pathlib.Path,
        change_message: str,
    ) -> tuple[pathlib.Path, str]:
        repository = root / "repository"
        repository.mkdir()
        cls._git(repository, "init", "--quiet", "-b", "main")
        cls._git(repository, "config", "user.email", "test@example.com")
        cls._git(repository, "config", "user.name", "Test")
        (repository / "fixture.txt").write_text("initial\n", encoding="utf-8")
        cls._git(repository, "add", "fixture.txt")
        cls._git(repository, "-c", "commit.gpgsign=false", "commit", "--quiet", "-m", "feat: initial")
        cls._git(repository, "tag", "v1.0.0")
        (repository / "fixture.txt").write_text("changed\n", encoding="utf-8")
        cls._git(repository, "add", "fixture.txt")
        cls._git(repository, "-c", "commit.gpgsign=false", "commit", "--quiet", "-m", change_message)
        return repository, cls._git_output(repository, "rev-parse", "HEAD")

    @staticmethod
    def _read_trace(trace_path: pathlib.Path) -> list[dict[str, object]]:
        if not trace_path.is_file():
            raise AssertionError("Git Trace2 evidence was not created")
        events = [json.loads(line) for line in trace_path.read_text(encoding="utf-8").splitlines()]
        if not events or not all(isinstance(event, dict) for event in events):
            raise AssertionError("Git Trace2 evidence is empty or malformed")
        return events

    @classmethod
    def _git_output(cls, repository: pathlib.Path, *arguments: str) -> str:
        return cls._run_git(repository, *arguments).stdout.strip()

    @classmethod
    def _git(cls, repository: pathlib.Path, *arguments: str) -> None:
        cls._run_git(repository, *arguments)

    @staticmethod
    def _run_git(repository: pathlib.Path, *arguments: str) -> subprocess.CompletedProcess[str]:
        result = subprocess.run(
            ["git", *arguments],
            cwd=repository,
            check=False,
            capture_output=True,
            text=True,
        )
        if result.returncode != 0:
            raise RuntimeError(f"git {' '.join(arguments)} failed: {result.stderr}")
        return result

    @staticmethod
    def _write_package(path: pathlib.Path, members: dict[str, bytes]) -> None:
        with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            for name, content in members.items():
                archive.writestr(name, content)


if __name__ == "__main__":
    unittest.main()
