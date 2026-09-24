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


def packages(path: Path, commit: str = CANDIDATE, id_suffix: str = "", version: str = VERSION,
             url: str = proof.REPOSITORY) -> None:
    for package_id in proof.PACKAGE_IDS:
        nuspec = (f'<package><metadata><id>{package_id}{id_suffix}</id><version>{version}</version>'
                  f'<repository url="{url}" commit="{commit}" />'
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

    def test_package_metadata_mismatch_fails_closed(self):
        cases = {"id": {"id_suffix": ".Forged"}, "version": {"version": "12.1.1-proof"},
                 "repository": {"url": "https://github.com/example/Hexalith.FrontComposer"}}
        for name, overrides in cases.items():
            with self.subTest(field=name), tempfile.TemporaryDirectory() as temporary:
                packages(Path(temporary), **overrides)
                with self.assertRaises(proof.ProofError) as error:
                    proof.verify_packages(Path(temporary), CANDIDATE, VERSION)
                self.assertEqual("package-candidate-mismatch", error.exception.code)

    def test_missing_package_or_invalid_inputs_fail_without_echoing_input(self):
        with tempfile.TemporaryDirectory() as temporary:
            missing = proof.execute(args(temporary))
            self.assertEqual("package-missing", missing["failure"])
            bad_sha = args(temporary)
            bad_sha.candidate_sha = "abc123-private"
            result = proof.execute(bad_sha)
            self.assertEqual(("invalid-candidate-sha", "invalid"), (result["failure"], result["candidate_sha"]))
            bad_version = args(temporary)
            bad_version.package_version = "latest-private"
            result = proof.execute(bad_version)
            self.assertEqual(("invalid-package-version", "invalid"),
                             (result["failure"], result["package_identity"]["version"]))

    def test_runtime_identity_failures_are_named(self):
        fixture = Path("/tmp/fixture")
        listing = "Microsoft.NETCore.App 10.0.12 [/runtime]\nMicrosoft.AspNetCore.App 10.0.11 [/runtime]\n"
        full = "Microsoft.NETCore.App 10.0.12 [/runtime]\nMicrosoft.AspNetCore.App 10.0.12 [/runtime]\n"
        cases = [
            (["10.0.401\n", listing], "runtime-identity-mismatch"),
            (["11.0.100\n", full], "runtime-identity-mismatch"),
            (["11.0.100-rc.1.26\n", full], "runtime-identity-invalid"),
            (proof.subprocess.CalledProcessError(1, ["dotnet"]), "runtime-unavailable"),
            (FileNotFoundError("dotnet"), "runtime-unavailable"),
        ]
        for outputs, code in cases:
            with self.subTest(code=code, outputs=outputs), \
                    mock.patch.object(proof.subprocess, "check_output", side_effect=outputs):
                with self.assertRaises(proof.ProofError) as error:
                    proof.verify_runtime("dotnet", "10.0.12", fixture)
                self.assertEqual(code, error.exception.code)

    def test_eventstore_endpoint_matches_host_absolute_uri_rule(self):
        for endpoint in ("HTTPS://EventStore.example.test", "http://127.0.0.1:1"):
            self.assertTrue(proof._valid_endpoint(endpoint), endpoint)
        for endpoint in ("http://", "https://:443", "ftp://eventstore.example.test", "http://[bad", "relative"):
            with self.subTest(endpoint=endpoint):
                result, calls, runs = self._run_simulated([], eventstore_endpoint=endpoint)
                self.assertEqual("eventstore-endpoint-invalid", result["failure"])
                self.assertEqual(([], []), (calls, runs))

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

    def test_malformed_http_response_is_failed_probe(self):
        for error in (http.client.BadStatusLine("garbage"), http.client.LineTooLong("header")):
            with self.subTest(error=type(error).__name__), \
                    mock.patch.object(proof.urllib.request, "urlopen", side_effect=error):
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

    def test_misordered_host_requires_the_eventstore_before_quickstart_diagnostic(self):
        process = mock.Mock()
        process.poll.return_value = 1
        domain_misordered = ("FrontComposer bootstrap is mis-ordered: AddHexalithDomain<TMarker>() was called "
                             "before AddHexalithFrontComposerQuickstart().")
        cases = [(proof.NEGATIVE_DIAGNOSTICS["misordered"], None),
                 (proof.NEGATIVE_DIAGNOSTICS["missing-quickstart"], "bootstrap-diagnostic-missing"),
                 (domain_misordered, "bootstrap-diagnostic-missing")]
        for diagnostic, code in cases:
            def launch(*_args, **kwargs):
                kwargs["stdout"].write(diagnostic)
                kwargs["stdout"].flush()
                return process

            with self.subTest(code=code, diagnostic=diagnostic), \
                    mock.patch.object(proof, "_port", return_value=52345), \
                    mock.patch.object(proof, "_get", return_value=None), \
                    mock.patch.object(proof.subprocess, "Popen", side_effect=launch):
                if code is None:
                    self.assertTrue(proof._start_and_probe("dotnet", Path("/tmp/host.dll"), "10.0.12",
                                                           "http://127.0.0.1:1", "misordered", "/"))
                    continue
                with self.assertRaises(proof.ProofError) as error:
                    proof._start_and_probe("dotnet", Path("/tmp/host.dll"), "10.0.12",
                                           "http://127.0.0.1:1", "misordered", "/")
                self.assertEqual(code, error.exception.code)

    def test_negative_host_that_renders_a_page_is_rejected(self):
        for mode in ("missing-quickstart", "misordered"):
            process = mock.Mock()
            process.poll.return_value = None
            with self.subTest(mode=mode), \
                    mock.patch.object(proof, "_port", return_value=52345), \
                    mock.patch.object(proof, "_get", return_value="<html>fc-home-empty-no-microservices</html>"), \
                    mock.patch.object(proof.subprocess, "Popen", return_value=process):
                with self.assertRaises(proof.ProofError) as error:
                    proof._start_and_probe("dotnet", Path("/tmp/host.dll"), "10.0.12",
                                           "http://127.0.0.1:1", mode, "/")
                self.assertEqual("bootstrap-unexpected-render", error.exception.code)
                process.terminate.assert_called_once()

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

    def _simulate(self, stack: ExitStack, responses: list[bool | Exception],
                  restored_error: Exception | None = None, run_error: Exception | None = None):
        sdk_policies = []

        def runtime(_dotnet, _selected, cwd):
            sdk_policies.append(json.loads((cwd.parent / "global.json").read_text(encoding="utf-8")))
            return {"dotnet_sdk": "10.0.401", "netcore": "10.0.12", "aspnetcore": "10.0.12"}

        stack.enter_context(mock.patch.object(proof, "verify_packages", return_value=fake_hashes()))
        stack.enter_context(mock.patch.object(proof, "verify_restored_packages", side_effect=restored_error))
        stack.enter_context(mock.patch.object(proof, "verify_runtime", side_effect=runtime))
        stack.enter_context(mock.patch.object(proof, "pin_host_runtime"))
        run = stack.enter_context(mock.patch.object(proof.subprocess, "run", return_value=mock.Mock(returncode=0),
                                                    side_effect=run_error))
        launch = stack.enter_context(mock.patch.object(proof, "_start_and_probe", side_effect=responses))
        return launch, run, sdk_policies

    def _run_simulated(self, responses: list[bool | Exception], restored_error: Exception | None = None,
                       run_error: Exception | None = None, **overrides):
        with tempfile.TemporaryDirectory() as temporary, ExitStack() as stack:
            launch, run, _ = self._simulate(stack, responses, restored_error, run_error)
            namespace = args(temporary)
            vars(namespace).update(overrides)
            result = proof.execute(namespace)
            return result, launch.call_args_list, run.call_args_list

    def test_copied_fixture_is_pinned_to_the_stable_net10_sdk_band(self):
        with tempfile.TemporaryDirectory() as temporary, ExitStack() as stack:
            _, _, sdk_policies = self._simulate(stack, [True] * 5)
            self.assertEqual("pass", proof.execute(args(temporary))["result"])
        self.assertEqual([proof.SDK_POLICY], sdk_policies)
        self.assertEqual({"version": "10.0.100", "rollForward": "latestFeature", "allowPrerelease": False},
                         sdk_policies[0]["sdk"])

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

    def test_missing_command_render_assertion_fails(self):
        result, calls, _ = self._run_simulated([True, False])
        self.assertEqual("render-assertion-missing", result["failure"])
        self.assertTrue(result["assertions"]["projection_rendered"])
        self.assertFalse(result["assertions"]["command_rendered"])
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
        passed, calls, _ = self._run_simulated([True, True, True, True, True])
        self.assertEqual("pass", passed["result"])
        self.assertTrue(all(passed["assertions"].values()))
        self.assertEqual([
            ("three-call", "/proof/proof-projection", proof.PROJECTION_FRAGMENTS),
            ("three-call", "/commands/Proof/CreateProofCommand", ("fc-command-form", "Proof label")),
            ("missing-quickstart", "/", ()),
            ("misordered", "/", ()),
            ("empty", "/", ("fc-home-empty-no-microservices",)),
        ], [(call.args[4], call.args[5], call.args[6] if len(call.args) > 6 else ()) for call in calls])

    def test_result_is_allowlisted_and_redacted(self):
        with tempfile.TemporaryDirectory() as temporary, ExitStack() as stack:
            self._simulate(stack, [True, True, True, True, True])
            output = Path(temporary) / "result.json"
            source = Path(temporary) / "private-machine-feed"
            self.assertEqual(0, proof.main([
                "--candidate-sha", CANDIDATE, "--package-version", VERSION,
                "--package-source", str(source), "--runtime-version", "10.0.12",
                "--eventstore-endpoint", "https://token-secret.example.invalid/tenant-private",
                "--result", str(output)]))
            serialized = output.read_text(encoding="utf-8")
            result = json.loads(serialized)
            self.assertEqual({"schema", "execution_date", "candidate_sha", "package_identity",
                              "runtime_identity", "surfaces", "assertions", "result", "failure"}, set(result))
            self.assertEqual({"version", "ids", "archive_sha512", "verified"}, set(result["package_identity"]))
            self.assertEqual({"dotnet_sdk", "netcore", "aspnetcore"}, set(result["runtime_identity"]))
            self.assertEqual({"projection", "command"}, set(result["surfaces"]))
            self.assertEqual({"projection_rendered", "command_rendered", "bootstrap_rejected",
                              "empty_registry_rendered"}, set(result["assertions"]))
            self.assertEqual("ProofProjectionView@/proof/proof-projection", result["surfaces"]["projection"])
            identity = result["package_identity"]
            self.assertTrue(identity["verified"])
            self.assertEqual(fake_hashes(), identity["archive_sha512"])
            for encoded in identity["archive_sha512"].values():
                self.assertEqual(64, len(base64.b64decode(encoded, validate=True)))
            for private in (temporary, "private-machine-feed", "token-secret", "tenant-private",
                            "Traceback", "payload"):
                self.assertNotIn(private, serialized)


if __name__ == "__main__":
    unittest.main()
