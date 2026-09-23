"""Runner contract tests; framework rendering stays in the focused .NET suites."""

from __future__ import annotations

import argparse
import base64
from contextlib import ExitStack
import hashlib
import http.client
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest import mock
from zipfile import ZipFile


SCRIPT = Path(__file__).resolve().parents[2] / "eng" / "adopter_proof.py"
SPEC = importlib.util.spec_from_file_location("adopter_proof", SCRIPT)
assert SPEC and SPEC.loader
proof = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(proof)
CANDIDATE = "a" * 40
VERSION = "12.1.0-proof"


def args(source: str, result: str = "result.json") -> argparse.Namespace:
    return argparse.Namespace(candidate_sha=CANDIDATE, package_version=VERSION,
                              package_source=source, runtime_version="10.0.12",
                              eventstore_endpoint="https://token-secret.example.invalid/tenant-private",
                              result=result, dotnet="dotnet")


def packages(path: Path, commit: str = CANDIDATE) -> None:
    for package_id in proof.PACKAGE_IDS:
        nuspec = (f'<package><metadata><id>{package_id}</id><version>{VERSION}</version>'
                  f'<repository url="{proof.REPOSITORY}" commit="{commit}" />'
                  '</metadata></package>')
        with ZipFile(path / f"{package_id}.{VERSION}.nupkg", "w") as archive:
            archive.writestr(package_id + ".nuspec", nuspec)


def fake_hashes() -> dict[str, str]:
    return {package_id: base64.b64encode(hashlib.sha512(package_id.encode("ascii")).digest()).decode("ascii")
            for package_id in proof.PACKAGE_IDS}


class AdopterProofTests(unittest.TestCase):
    def test_candidate_mismatch_fails_before_host(self):
        with tempfile.TemporaryDirectory() as temporary:
            packages(Path(temporary), "b" * 40)
            with mock.patch.object(proof, "verify_runtime") as runtime:
                result = proof.execute(args(temporary))
            self.assertEqual("fail", result["result"])
            self.assertEqual("package-candidate-mismatch", result["failure"])
            self.assertEqual({}, result["package_identity"]["archive_sha512"])
            self.assertFalse(result["package_identity"]["verified"])
            runtime.assert_not_called()

    def test_restored_archive_must_match_checked_candidate(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            packages(root)
            hashes = proof.verify_packages(root, CANDIDATE, VERSION)
            self.assertEqual(set(proof.PACKAGE_IDS), set(hashes))
            for package_id, encoded in hashes.items():
                archive = root / f"{package_id}.{VERSION}.nupkg"
                self.assertEqual(hashlib.sha512(archive.read_bytes()).digest(), base64.b64decode(encoded))
            for package_id in proof.PACKAGE_IDS:
                cache = root / "cache" / package_id.lower() / VERSION
                cache.mkdir(parents=True)
                (cache / f"{package_id.lower()}.{VERSION}.nupkg.sha512").write_text(
                    hashes[package_id], encoding="ascii")
            proof.verify_restored_packages(root / "cache", VERSION, hashes)
            changed = root / "cache" / proof.PACKAGE_IDS[0].lower() / VERSION / f"{proof.PACKAGE_IDS[0].lower()}.{VERSION}.nupkg.sha512"
            changed.write_text(base64.b64encode(hashlib.sha512(b"different").digest()).decode("ascii"), encoding="ascii")
            with self.assertRaises(proof.ProofError) as error:
                proof.verify_restored_packages(root / "cache", VERSION, hashes)
            self.assertEqual("restored-package-mismatch", error.exception.code)

    def test_package_parse_and_hash_use_one_archive_snapshot(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            packages(root)
            original = Path.read_bytes
            reads = []

            def read_once(path):
                reads.append(path.name)
                return original(path)

            with mock.patch.object(Path, "read_bytes", read_once):
                proof.verify_packages(root, CANDIDATE, VERSION)
            self.assertEqual([f"{package_id}.{VERSION}.nupkg" for package_id in proof.PACKAGE_IDS], reads)

    def test_unsupported_zip_member_is_named_identity_failure(self):
        with tempfile.TemporaryDirectory() as temporary:
            packages(Path(temporary))
            with mock.patch.object(proof, "ZipFile", side_effect=RuntimeError("private zip detail")):
                with self.assertRaises(proof.ProofError) as error:
                    proof.verify_packages(Path(temporary), CANDIDATE, VERSION)
            self.assertEqual("package-identity-invalid", error.exception.code)

    def test_runtime_sdk_identity_is_read_from_fixture_directory(self):
        with tempfile.TemporaryDirectory() as temporary:
            fixture = Path(temporary)
            listing = "Microsoft.NETCore.App 10.0.12 [/runtime]\nMicrosoft.AspNetCore.App 10.0.12 [/runtime]\n"
            with mock.patch.object(proof.subprocess, "check_output", side_effect=["10.0.401\n", listing]) as check:
                identity = proof.verify_runtime("dotnet", "10.0.12", fixture)
            self.assertEqual("10.0.401", identity["dotnet_sdk"])
            self.assertEqual([fixture, fixture], [call.kwargs["cwd"] for call in check.call_args_list])
            self.assertTrue(all(call.kwargs["timeout"] == 15 for call in check.call_args_list))

    def test_host_runtime_pins_both_frameworks_without_roll_forward(self):
        with tempfile.TemporaryDirectory() as temporary:
            assembly = Path(temporary) / "Host.dll"
            config_path = assembly.with_suffix(".runtimeconfig.json")
            config_path.write_text(json.dumps({"runtimeOptions": {"frameworks": [
                {"name": "Microsoft.NETCore.App", "version": "10.0.0"},
                {"name": "Microsoft.AspNetCore.App", "version": "10.0.0"},
            ]}}), encoding="utf-8")
            proof.pin_host_runtime(assembly, "10.0.12")
            options = json.loads(config_path.read_text(encoding="utf-8"))["runtimeOptions"]
            self.assertEqual("Disable", options["rollForward"])
            self.assertEqual(["10.0.12", "10.0.12"], [entry["version"] for entry in options["frameworks"]])
            config_path.write_text('{"runtimeOptions": {"frameworks": []}}', encoding="utf-8")
            with self.assertRaises(proof.ProofError) as error:
                proof.pin_host_runtime(assembly, "10.0.12")
            self.assertEqual("runtime-config-invalid", error.exception.code)

    def test_truncated_http_page_is_failed_probe(self):
        response = mock.MagicMock()
        response.__enter__.return_value = response
        response.status = 200
        response.read.side_effect = http.client.IncompleteRead(b"partial", 10)
        with mock.patch.object(proof.urllib.request, "urlopen", return_value=response):
            self.assertIsNone(proof._get("http://127.0.0.1:1/"))

    def test_projection_fragments_require_generated_view_and_navigation(self):
        process = mock.Mock()
        process.poll.return_value = None
        nav = ('<fluent-menu-item data-testid="fc-nav-flyout-projection-Proof-ProofProjection" '
               'data-href="/proof/proof-projection">')
        full_page = " ".join(proof.PROJECTION_FRAGMENTS) + nav
        cases = [(full_page, True)] + [
            (full_page.replace(fragment, ""), False) for fragment in proof.PROJECTION_FRAGMENTS + (nav,)]
        cases.append((full_page.replace('data-href="/proof/proof-projection"',
                                        'data-href="/wrong-route"'), False))
        for page, expected in cases:
            with self.subTest(expected=expected, page=page), \
                    mock.patch.object(proof, "_port", return_value=52345), \
                    mock.patch.object(proof, "_get", return_value=page), \
                    mock.patch.object(proof.subprocess, "Popen", return_value=process) as launch:
                rendered = proof._start_and_probe("dotnet", Path("/tmp/host.dll"), "10.0.12",
                                                  "http://127.0.0.1:1", "three-call",
                                                  "/proof/proof-projection", proof.PROJECTION_FRAGMENTS)
                self.assertEqual(expected, rendered)
                self.assertEqual("Disable", launch.call_args.kwargs["env"]["DOTNET_ROLL_FORWARD"])

    def test_negative_host_must_exit_with_its_own_diagnostic(self):
        process = mock.Mock()
        process.poll.return_value = 1

        def launch_with(diagnostic):
            def launch(*_args, **kwargs):
                kwargs["stdout"].write(diagnostic)
                kwargs["stdout"].flush()
                return process
            return launch

        with mock.patch.object(proof, "_port", return_value=52345), \
                mock.patch.object(proof, "_get", return_value=None), \
                mock.patch.object(proof.subprocess, "Popen",
                                  side_effect=launch_with(proof.NEGATIVE_DIAGNOSTICS["missing-quickstart"])):
            self.assertTrue(proof._start_and_probe("dotnet", Path("/tmp/host.dll"), "10.0.12",
                                                  "http://127.0.0.1:1", "missing-quickstart", "/"))
        with mock.patch.object(proof, "_port", return_value=52345), \
                mock.patch.object(proof, "_get", return_value=None), \
                mock.patch.object(proof.subprocess, "Popen",
                                  side_effect=launch_with(proof.NEGATIVE_DIAGNOSTICS["misordered"])):
            with self.assertRaises(proof.ProofError) as error:
                proof._start_and_probe("dotnet", Path("/tmp/host.dll"), "10.0.12",
                                       "http://127.0.0.1:1", "missing-quickstart", "/")
            self.assertEqual("bootstrap-diagnostic-missing", error.exception.code)
        process.poll.return_value = 0
        with mock.patch.object(proof, "_port", return_value=52345), \
                mock.patch.object(proof, "_get", return_value=None), \
                mock.patch.object(proof.subprocess, "Popen",
                                  side_effect=launch_with(proof.NEGATIVE_DIAGNOSTICS["missing-quickstart"])):
            with self.assertRaises(proof.ProofError) as error:
                proof._start_and_probe("dotnet", Path("/tmp/host.dll"), "10.0.12",
                                       "http://127.0.0.1:1", "missing-quickstart", "/")
            self.assertEqual("bootstrap-diagnostic-missing", error.exception.code)

    def test_negative_host_still_running_at_deadline_fails(self):
        process = mock.Mock()
        process.poll.return_value = None
        with mock.patch.object(proof, "_port", return_value=52345), \
                mock.patch.object(proof, "_get", return_value=None), \
                mock.patch.object(proof.time, "monotonic", side_effect=[0, 31]), \
                mock.patch.object(proof.subprocess, "Popen", return_value=process):
            with self.assertRaises(proof.ProofError) as error:
                proof._start_and_probe("dotnet", Path("/tmp/host.dll"), "10.0.12",
                                       "http://127.0.0.1:1", "missing-quickstart", "/")
            self.assertEqual("bootstrap-did-not-exit", error.exception.code)
            process.terminate.assert_called_once()

    def _run_simulated(self, responses: list[bool | Exception], restored_error: Exception | None = None,
                       run_error: Exception | None = None):
        with tempfile.TemporaryDirectory() as temporary, ExitStack() as stack:
            stack.enter_context(mock.patch.object(proof, "verify_packages", return_value=fake_hashes()))
            stack.enter_context(mock.patch.object(proof, "verify_restored_packages", side_effect=restored_error))
            stack.enter_context(mock.patch.object(proof, "verify_runtime", return_value={
                "dotnet_sdk": "10.0.401", "netcore": "10.0.12", "aspnetcore": "10.0.12"}))
            stack.enter_context(mock.patch.object(proof, "pin_host_runtime"))
            run = stack.enter_context(mock.patch.object(proof.subprocess, "run", return_value=mock.Mock(returncode=0),
                                                        side_effect=run_error))
            launch = stack.enter_context(mock.patch.object(proof, "_start_and_probe", side_effect=responses))
            result = proof.execute(args(temporary))
            return result, launch.call_args_list, run.call_args_list

    def test_restored_mismatch_publishes_no_archive_hashes(self):
        result, calls, _ = self._run_simulated([], proof.ProofError("restored-package-mismatch"))
        self.assertEqual("restored-package-mismatch", result["failure"])
        self.assertEqual({}, result["package_identity"]["archive_sha512"])
        self.assertFalse(result["package_identity"]["verified"])
        self.assertEqual([], calls)

    def test_stalled_restore_has_named_redacted_failure(self):
        result, calls, runs = self._run_simulated(
            [], run_error=proof.subprocess.TimeoutExpired(["dotnet", "restore"], 180))
        self.assertEqual("fail", result["result"])
        self.assertEqual("fixture-command-timeout", result["failure"])
        self.assertEqual([], calls)
        self.assertEqual(180, runs[0].kwargs["timeout"])

    def test_missing_projection_render_assertion_fails(self):
        result, calls, _ = self._run_simulated([False, True])
        self.assertEqual("render-assertion-missing", result["failure"])
        self.assertFalse(result["assertions"]["projection_rendered"])
        self.assertEqual(2, len(calls))

    def test_bootstrap_failure_is_not_recorded_as_pass(self):
        result, calls, _ = self._run_simulated([True, True, proof.ProofError("bootstrap-diagnostic-missing")])
        self.assertEqual("bootstrap-diagnostic-missing", result["failure"])
        self.assertFalse(result["assertions"]["bootstrap_rejected"])
        self.assertEqual(3, len(calls))

    def test_empty_registry_must_render_existing_empty_state(self):
        result, calls, _ = self._run_simulated([True, True, True, True, False])
        self.assertEqual("empty-registry-render-missing", result["failure"])
        self.assertFalse(result["assertions"]["empty_registry_rendered"])
        self.assertEqual("empty", calls[-1].args[4])
        passed, _, _ = self._run_simulated([True, True, True, True, True])
        self.assertEqual("pass", passed["result"])
        self.assertTrue(all(passed["assertions"].values()))

    def test_result_is_allowlisted_and_redacted(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / "result.json"
            expected, _, _ = self._run_simulated([True, True, True, True, True])
            with mock.patch.object(proof, "execute", return_value=expected):
                self.assertEqual(0, proof.main([
                    "--candidate-sha", CANDIDATE, "--package-version", VERSION,
                    "--package-source", temporary, "--runtime-version", "10.0.12",
                    "--eventstore-endpoint", "https://token-secret.example.invalid/tenant-private",
                    "--result", str(output)]))
            serialized = output.read_text(encoding="utf-8")
            self.assertEqual({"schema", "execution_date", "candidate_sha", "package_identity",
                              "runtime_identity", "surfaces", "assertions", "result", "failure"},
                             set(json.loads(serialized)))
            self.assertEqual("ProofProjectionView@/proof/proof-projection",
                             json.loads(serialized)["surfaces"]["projection"])
            identity = json.loads(serialized)["package_identity"]
            self.assertTrue(identity["verified"])
            self.assertEqual(fake_hashes(), identity["archive_sha512"])
            for encoded in identity["archive_sha512"].values():
                self.assertEqual(64, len(base64.b64decode(encoded, validate=True)))
            for private in (temporary, "token-secret", "tenant-private", "Traceback", "payload"):
                self.assertNotIn(private, serialized)


if __name__ == "__main__":
    unittest.main()
