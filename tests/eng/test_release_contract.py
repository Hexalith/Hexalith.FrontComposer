#!/usr/bin/env python3
"""Negative contracts for the manual exact-source production release boundary."""

from __future__ import annotations

import copy
import contextlib
import io
import json
import pathlib
import sys
import tempfile
import unittest
import zipfile

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

    @staticmethod
    def _write_package(path: pathlib.Path, members: dict[str, bytes]) -> None:
        with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            for name, content in members.items():
                archive.writestr(name, content)


if __name__ == "__main__":
    unittest.main()
