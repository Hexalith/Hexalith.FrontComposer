import importlib.util
import contextlib
import hashlib
import io
import signal
import subprocess
import sys
import time
import json
import os
from pathlib import Path
import tempfile
import threading
import unittest
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("readiness", Path(__file__).resolve().parents[3] / "eng/verify-isolated-readiness.py")
readiness = importlib.util.module_from_spec(spec)
spec.loader.exec_module(readiness)


class ReadinessSafetyTests(unittest.TestCase):
    def health(self, maintenance):
        names = ["self", "dapr-sidecar", "dapr-actor-placement", "dapr-statestore", "dapr-pubsub", "dapr-configstore",
                 "dapr-app-channel-token", "projection-delivery-writer-protocol", "projection-delivery-unresolved-conflict"]
        return {"status": "Unhealthy" if maintenance else "Healthy", "results": {
            name: {"status": "Unhealthy" if maintenance and name == readiness.MARKER else "Healthy"} for name in names}}

    def configuration(self):
        return {"owner": "a" * 32, "daprVersion": "1.18.7", "ports": {"redis": 41001, "placement": 41002, "scheduler": 41003, "daprHttp": 41004},
                "images": {"redis": "sha256:expected"}}

    def container(self):
        config = self.configuration()
        return {"Id": "owned-id", "Name": "/" + readiness.container_name(config, "redis"), "Image": "sha256:expected",
                "Config": {"Labels": {readiness.LABEL: config["owner"]}},
                "HostConfig": {"PortBindings": {"6379/tcp": [{"HostIp": "127.0.0.1", "HostPort": "41001"}]}}}

    def test_container_accepts_only_exact_owned_identity(self):
        self.assertEqual(readiness.validate_container(self.configuration(), "redis", self.container(), "owned-id"), "owned-id")
        for field, value in [("Id", "replaced"), ("Name", "/other"), ("Image", "other-image"), ("Config", {"Labels": {}})]:
            with self.subTest(field=field), self.assertRaises(RuntimeError):
                changed = self.container()
                changed[field] = value
                readiness.validate_container(self.configuration(), "redis", changed, "owned-id")

    def test_shared_or_extra_port_bindings_are_refused(self):
        for binding in [{"6379/tcp": [{"HostIp": "0.0.0.0", "HostPort": "41001"}]},
                        {"6379/tcp": [{"HostIp": "127.0.0.1", "HostPort": "6379"}]},
                        {"6379/tcp": [{"HostIp": "127.0.0.1", "HostPort": "41001"}], "6380/tcp": []}]:
            with self.subTest(binding=binding), self.assertRaises(RuntimeError):
                container = self.container()
                container["HostConfig"]["PortBindings"] = binding
                readiness.validate_container(self.configuration(), "redis", container)

    def test_final_health_never_accepts_maintenance_or_empty_health(self):
        healthy = self.health(False)
        readiness.validate_health(200, healthy, False)
        for code, body in [(503, healthy), (200, {"status": "Healthy", "results": {}}),
                           (200, {"status": "Degraded", "results": healthy["results"]})]:
            with self.subTest(code=code, body=body), self.assertRaises(RuntimeError):
                readiness.validate_health(code, body, False)

    def test_maintenance_requires_only_the_inactive_writer_marker(self):
        body = self.health(True)
        readiness.validate_health(503, body, True)
        with self.assertRaises(RuntimeError):
            readiness.validate_health(503, body, False)
        body["results"]["dapr-statestore"] = {"status": "Unhealthy"}
        with self.assertRaises(RuntimeError):
            readiness.validate_health(503, body, True)

    def test_persisted_marker_must_identify_exact_source_and_version(self):
        marker = {"schemaVersion": 1, "writerProtocolVersion": 2, "cutoverCommit": "b" * 40, "activatedAt": "2026-10-26T00:00:00Z"}
        readiness.validate_marker(marker, "b" * 40)
        for field, value in [("schemaVersion", 2), ("writerProtocolVersion", 1), ("cutoverCommit", "c" * 40), ("activatedAt", "")]:
            with self.subTest(field=field), self.assertRaises(RuntimeError):
                readiness.validate_marker({**marker, field: value}, "b" * 40)

    def test_authorization_is_never_forwarded_to_a_redirect(self):
        requests = []

        class Handler(BaseHTTPRequestHandler):
            def do_POST(self):
                requests.append((self.path, self.headers.get("Authorization")))
                self.send_response(303)
                self.send_header("Location", f"http://127.0.0.1:{self.server.server_port}/redirected")
                self.end_headers()

            def do_GET(self):
                requests.append((self.path, self.headers.get("Authorization")))
                self.send_response(200)
                self.end_headers()
                self.wfile.write(b"{}")

            def log_message(self, *args):
                pass

        with ThreadingHTTPServer(("127.0.0.1", 0), Handler) as server:
            thread = threading.Thread(target=server.serve_forever)
            thread.start()
            try:
                status, _ = readiness.request(f"http://127.0.0.1:{server.server_port}/activate", {}, token="private-test-token")
                self.assertEqual(status, 303)
                self.assertEqual(requests, [("/activate", "Bearer private-test-token")])
            finally:
                server.shutdown()
                thread.join()

    def test_watched_directory_contains_only_components(self):
        def invoke(arguments, **kwargs):
            return b"1.18.7" if arguments[0].endswith("daprd") else b'[{"Id":"sha256:test-image"}]'

        with tempfile.TemporaryDirectory() as root, patch.object(readiness, "invoke", side_effect=invoke), \
                patch.object(readiness, "container_present", return_value=False):
            directory = Path(root)
            readiness.create_configuration(directory)
            components = directory / "components"
            self.assertEqual({path.name for path in components.iterdir()}, {"statestore.yaml", "pubsub.yaml", "configstore.yaml"})
            self.assertTrue(all("kind: Component" in path.read_text() for path in components.iterdir()))
            configuration = (directory / "accesscontrol.yaml").read_text()
            self.assertIn(str(directory / "runtime" / "nr.db"), configuration)
            self.assertTrue((directory / "runtime").is_dir())
            self.assertNotIn(str(components / "nr.db"), configuration)

    def test_owned_realm_reads_copied_import_without_a_host_mount(self):
        container = {"Id": "owned-keycloak", "Mounts": []}
        realm = {"realm": "Hexalith", "clients": [{"clientId": "eventstore", "secret": "private-test-secret"}]}
        with patch.object(readiness, "invoke", side_effect=[json.dumps([container]).encode(), json.dumps(realm).encode()]) as invoke:
            self.assertEqual(readiness.read_owned_realm("owned-keycloak"), realm)
            self.assertEqual(invoke.call_args_list[1].args[0], ["docker", "exec", "owned-keycloak", "cat",
                             "/opt/keycloak/data/import/hexalith-realm.json"])
            self.assertNotIn("log", invoke.call_args_list[1].kwargs)

    def test_owned_realm_refuses_changed_or_ambiguous_container_identity(self):
        for containers in [[], [{"Id": "other"}], [{"Id": "owned"}, {"Id": "owned"}]]:
            with self.subTest(containers=containers), \
                    patch.object(readiness, "invoke", return_value=json.dumps(containers).encode()) as invoke:
                with self.assertRaises(RuntimeError):
                    readiness.read_owned_realm("owned")
                self.assertEqual(invoke.call_count, 1)

    def test_actual_sidecar_listener_must_match_gateway_for_http_and_grpc(self):
        gateway = {"environment": {"DAPR_HTTP_PORT": "42001", "DAPR_GRPC_PORT": "42002"}}
        sidecar = {"displayName": "eventstore-dapr-cli", "state": "Running", "properties": {"executable.path": "/usr/local/bin/dapr"},
                   "urls": [{"name": "http", "url": "http://localhost:42001"}, {"name": "grpc", "url": "http://localhost:42002"}]}
        self.assertEqual(readiness.correlate_sidecar(gateway, sidecar), "http://localhost:42001")
        for variable in ["DAPR_HTTP_PORT", "DAPR_GRPC_PORT"]:
            with self.subTest(variable=variable), self.assertRaises(RuntimeError):
                readiness.correlate_sidecar({"environment": {**gateway["environment"], variable: "3500"}}, sidecar)
        for field, value in [("displayName", "other-dapr-cli"), ("state", "Waiting")]:
            with self.subTest(field=field), self.assertRaises(RuntimeError):
                readiness.correlate_sidecar(gateway, {**sidecar, field: value})

    def test_non_loopback_credential_requests_are_refused_before_transport(self):
        with patch.object(readiness.urllib.request, "build_opener") as opener, self.assertRaises(RuntimeError):
            readiness.request("https://example.invalid/activate", {}, token="private-test-token")
        opener.assert_not_called()

    def test_early_configuration_failure_removes_private_directory(self):
        with tempfile.TemporaryDirectory() as root, patch.object(readiness.tempfile, "mkdtemp", return_value=root), \
                patch.object(readiness, "create_configuration", side_effect=RuntimeError("missing image")), \
                patch.object(readiness, "invoke") as invoke:
            with self.assertRaisesRegex(RuntimeError, "missing image"):
                readiness.run()
            self.assertFalse((Path(root) / "private").exists())
            invoke.assert_not_called()

    def test_rejected_start_identity_is_never_stopped(self):
        with tempfile.TemporaryDirectory() as root, patch.dict(os.environ, {"FC_EPIC9_LOCK_ROOT": root}), \
                patch.object(readiness, "processes", side_effect=[[], [{"appHostPath": str(readiness.APPHOST), "appHostPid": 456}]]), \
                patch.object(readiness, "invoke", return_value=json.dumps({"appHostPath": "/other/AppHost.csproj", "appHostPid": 123}).encode()) as invoke:
            with self.assertRaises(RuntimeError):
                with readiness.owned_apphost(Path(root), os.environ.copy()):
                    self.fail("Rejected start yielded ownership")
            self.assertFalse(any(call.args[0][1] == "stop" for call in invoke.call_args_list))

    def test_malformed_successful_start_is_rejected_without_cleanup(self):
        host = {"appHostPath": str(readiness.APPHOST), "appHostPid": 123}
        for output in [b"not-json", b"{broken-json", b"[]"]:
            with self.subTest(output=output), tempfile.TemporaryDirectory() as root, \
                    patch.dict(os.environ, {"FC_EPIC9_LOCK_ROOT": root}), \
                    patch.object(readiness, "processes", side_effect=[[], [host]]), \
                    patch.object(readiness, "invoke", return_value=output) as invoke:
                with self.assertRaises(RuntimeError):
                    with readiness.owned_apphost(Path(root), os.environ.copy()):
                        self.fail("Malformed identity yielded ownership")
                self.assertFalse(any(call.args[0][1] == "stop" for call in invoke.call_args_list))

    def test_changed_or_dirty_eventstore_source_refuses_cutover(self):
        for outputs in [[b"c" * 40], [b"b" * 40, b" M source.cs"]]:
            with self.subTest(outputs=outputs), patch.object(readiness, "invoke", side_effect=outputs), self.assertRaises(RuntimeError):
                readiness.verify_source_commit("b" * 40)

    def test_changed_pid_is_never_stopped_or_allowed_to_mutate(self):
        host = {"appHostPath": str(readiness.APPHOST), "appHostPid": 999}
        with tempfile.TemporaryDirectory() as root, patch.dict(os.environ, {"FC_EPIC9_LOCK_ROOT": root}), \
                patch.object(readiness, "processes", side_effect=[[], [host], [host]]), \
                patch.object(readiness, "invoke", return_value=json.dumps({**host, "appHostPid": 123}).encode()) as invoke:
            with self.assertRaises(RuntimeError):
                with readiness.owned_apphost(Path(root), os.environ.copy()):
                    self.fail("Changed PID yielded ownership")
            self.assertFalse(any(call.args[0][1] == "stop" for call in invoke.call_args_list))

    def test_pristine_store_allows_only_never_published_subscription_infrastructure(self):
        stream = ["length", 0, "entries-added", 0, "last-generated-id", "0-0", "max-deleted-entry-id", "0-0"]
        group = ["name", "eventstore", "pending", 0, "last-delivered-id", "0-0", "lag", 0]
        responses = ["1", json.dumps([readiness.INFRASTRUCTURE_STREAM]), "stream", json.dumps(stream), json.dumps([group]), ""]
        for stream_value, group_value in [(stream, group), (dict(zip(stream[::2], stream[1::2])), dict(zip(group[::2], group[1::2])))]:
            with self.subTest(representation=type(stream_value).__name__), patch.object(readiness, "redis",
                    side_effect=responses[:3] + [json.dumps(stream_value), json.dumps([group_value]), ""]):
                self.assertEqual(readiness.verify_pristine_store(self.configuration()), 1)
        for field, value in [("length", 1), ("entries-added", 1), ("last-generated-id", "1-0"), ("max-deleted-entry-id", "1-0")]:
            changed = stream.copy()
            changed[changed.index(field) + 1] = value
            with self.subTest(field=field), patch.object(readiness, "redis", side_effect=responses[:3] + [json.dumps(changed)]), self.assertRaises(RuntimeError):
                readiness.verify_pristine_store(self.configuration())
        for field, value in [("name", "other"), ("pending", 1), ("last-delivered-id", "1-0"), ("lag", 1)]:
            changed = group.copy()
            changed[changed.index(field) + 1] = value
            with self.subTest(field=field), patch.object(readiness, "redis", side_effect=responses[:4] + [json.dumps([changed])]), self.assertRaises(RuntimeError):
                readiness.verify_pristine_store(self.configuration())

    def test_arbitrary_state_keys_and_existing_marker_are_refused(self):
        for keys in [["arbitrary-state"], [readiness.MARKER], [readiness.INFRASTRUCTURE_STREAM, "arbitrary-state"]]:
            with self.subTest(count=len(keys)), patch.object(readiness, "redis", side_effect=[str(len(keys)), json.dumps(keys)]), self.assertRaises(RuntimeError):
                readiness.verify_pristine_store(self.configuration())

    def test_existing_store_is_refused_before_backup_or_activation(self):
        with patch.object(readiness, "load_configuration", return_value=self.configuration()), \
                patch.object(readiness, "verify_gateway", return_value="http://127.0.0.1:12345"), \
                patch.object(readiness, "verify_source_commit"), \
                patch.object(readiness, "redis", return_value="1") as redis, \
                patch.object(readiness, "request") as request, patch.object(readiness, "invoke") as invoke:
            with self.assertRaises(RuntimeError):
                readiness.activate(Path("/private"), {}, "b" * 40)
            self.assertEqual(redis.call_args_list[0].args[1:], ("DBSIZE",))
            invoke.assert_not_called()
            request.assert_not_called()

    def test_both_health_phases_require_all_platform_checks_and_allow_healthy_additions(self):
        for maintenance in [True, False]:
            body = self.health(maintenance)
            body["results"]["additional"] = {"status": "Healthy"}
            code = 503 if maintenance else 200
            readiness.validate_health(code, body, maintenance)
            for missing in list(body["results"]):
                if missing == "additional":
                    continue
                with self.subTest(maintenance=maintenance, missing=missing), self.assertRaises(RuntimeError):
                    readiness.validate_health(code, {**body, "results": {k: v for k, v in body["results"].items() if k != missing}}, maintenance)

    def test_container_absence_requires_a_successful_daemon_query(self):
        with patch.object(readiness, "invoke", return_value=b"") as invoke:
            self.assertFalse(readiness.container_present(name="owned"))
            self.assertIn("name=^/owned$", invoke.call_args.args[0])
        with patch.object(readiness, "invoke", side_effect=RuntimeError("daemon unavailable")), self.assertRaises(RuntimeError):
            readiness.container_present(identifier="owned-id")

    def test_uncertain_cleanup_retains_private_recovery_configuration(self):
        for queries in [[RuntimeError("daemon unavailable")], [True, RuntimeError("permission denied")]]:
            with self.subTest(queries=len(queries)), tempfile.TemporaryDirectory() as root:
                config = self.configuration()
                config["containerIds"] = {"redis": "owned-id"}
                def create(directory):
                    readiness.private_file(directory / "configuration.json", "private-test-secret")
                    return config
                with patch.object(readiness.tempfile, "mkdtemp", return_value=root), \
                        patch.object(readiness, "create_configuration", side_effect=create), \
                        patch.object(readiness, "invoke", side_effect=lambda args, **kwargs: b"b" * 40 if args[-1] == "HEAD" else b""), \
                        patch.object(readiness, "owned_apphost", return_value=contextlib.nullcontext()), \
                        patch.object(readiness, "inspect_container", return_value=self.container()), \
                        patch.object(readiness, "activate", side_effect=RuntimeError("activation refused")), \
                        patch.object(readiness, "processes", return_value=[]), \
                        patch.object(readiness, "container_present", side_effect=queries):
                    with self.assertRaises(RuntimeError):
                        readiness.run()
                self.assertTrue((Path(root) / "private/configuration.json").exists())

    def test_held_lifecycle_lock_prevents_initial_build(self):
        with tempfile.TemporaryDirectory() as root, patch.dict(os.environ, {"FC_EPIC9_LOCK_ROOT": root}):
            lock_id = hashlib.sha256(str(readiness.APPHOST).encode()).hexdigest()
            with open(Path(root) / ("hexalith-epic9-" + lock_id + ".lock"), "w") as lock:
                readiness.fcntl.flock(lock, readiness.fcntl.LOCK_EX | readiness.fcntl.LOCK_NB)
                with patch.object(readiness, "invoke") as invoke, self.assertRaises(BlockingIOError):
                    with readiness.owned_apphost(Path(root), os.environ.copy(), build=True):
                        self.fail("Contended lock yielded ownership")
                invoke.assert_not_called()

    def test_build_runs_under_the_same_lock_before_start(self):
        with tempfile.TemporaryDirectory() as root, patch.dict(os.environ, {"FC_EPIC9_LOCK_ROOT": root}), \
                patch.object(readiness, "processes", return_value=[]):
            def invoke(args, **kwargs):
                self.assertEqual(args[:2], ["dotnet", "build"])
                lock_id = hashlib.sha256(str(readiness.APPHOST).encode()).hexdigest()
                with open(Path(root) / ("hexalith-epic9-" + lock_id + ".lock"), "w") as lock:
                    with self.assertRaises(BlockingIOError):
                        readiness.fcntl.flock(lock, readiness.fcntl.LOCK_EX | readiness.fcntl.LOCK_NB)
                raise RuntimeError("stop after build observation")
            with patch.object(readiness, "invoke", side_effect=invoke), self.assertRaisesRegex(RuntimeError, "stop after build"):
                with readiness.owned_apphost(Path(root), os.environ.copy(), build=True):
                    self.fail("Failed build started AppHost")

    def test_invoke_failure_only_claims_logs_when_written_and_does_not_leak_output(self):
        with tempfile.TemporaryDirectory() as root:
            for log in [None, Path(root) / "private.log"]:
                with self.subTest(log=bool(log)), self.assertRaises(RuntimeError) as failure:
                    readiness.invoke([sys.executable, "-c", "import sys; print('private-test-secret'); sys.exit(2)"], log=log)
                self.assertNotIn("private-test-secret", str(failure.exception))
                self.assertEqual("private diagnostics retained" in str(failure.exception), log is not None)
                if log:
                    self.assertIn(b"private-test-secret", log.read_bytes())

    def test_missing_image_reports_public_prerequisite_without_pulling(self):
        with tempfile.TemporaryDirectory() as root:
            def invoke(args, **kwargs):
                if args[0].endswith("daprd"):
                    return b"1.18.7"
                raise RuntimeError("private diagnostic must not escape")
            with patch.object(readiness, "invoke", side_effect=invoke) as called, self.assertRaisesRegex(RuntimeError, "Required local image redis:7.4-alpine"):
                readiness.create_configuration(Path(root))
            self.assertFalse(any("pull" in call.args[0] for call in called.call_args_list))

    def test_wrong_shaped_responses_are_sanitized_at_command_boundary(self):
        cases = [lambda: readiness.validate_health(200, ["private-test-secret"], False),
                 lambda: readiness.endpoint({"urls": [{"url": ["private-test-secret"]}]}),
                 lambda: readiness.read_owned_realm("owned")]
        for callback in cases:
            output = io.StringIO()
            with self.subTest(callback=callback), patch.object(readiness, "run", side_effect=callback), \
                    patch.object(readiness, "invoke", return_value=b'{"private-test-secret":1}'), \
                    patch.object(sys, "argv", ["verify-isolated-readiness.py"]), contextlib.redirect_stderr(output):
                self.assertEqual(readiness.main(), 1)
            self.assertIn("::error::Disposable readiness verification failed:", output.getvalue())
            self.assertNotIn("private-test-secret", output.getvalue())
            self.assertNotIn("Traceback", output.getvalue())
        with patch.object(readiness, "resource", return_value={"properties": {"container.id": "owned"}, "urls": [{"url": "http://localhost:12345"}]}), \
                patch.object(readiness, "read_owned_realm", return_value={"realm": "Hexalith", "clients": [{"clientId": "eventstore", "secret": "private-test-secret"}]}), \
                patch.object(readiness, "request", return_value=(200, {"access_token": "private-test-secret"})), \
                patch.object(readiness, "run", side_effect=lambda: readiness.verify_workload_identity({})), \
                patch.object(sys, "argv", ["verify-isolated-readiness.py"]), contextlib.redirect_stderr(io.StringIO()) as output:
            self.assertEqual(readiness.main(), 1)
            self.assertIn("IndexError", output.getvalue())
            self.assertNotIn("private-test-secret", output.getvalue())

    def test_activate_requires_matching_persisted_readback_before_success_evidence(self):
        source = "b" * 40
        valid = {"schemaVersion": 1, "writerProtocolVersion": 2, "cutoverCommit": source, "activatedAt": "2026-10-26T00:00:00Z"}
        for marker in [valid, {**valid, "cutoverCommit": "c" * 40}, {**valid, "writerProtocolVersion": 1}]:
            with self.subTest(marker=marker), tempfile.TemporaryDirectory() as root:
                directory = Path(root) / "private"
                directory.mkdir()
                config = {**self.configuration(), "containerIds": {"redis": "owned-id"}}
                backup_bytes = b"REDIS-fixture"
                digest = hashlib.sha256(backup_bytes).hexdigest()
                def invoke(args, **kwargs):
                    if args[1] == "cp":
                        Path(args[-1]).write_bytes(backup_bytes)
                        return b""
                    if args[3] == "redis-check-rdb":
                        return b"Checksum OK; 1 keys read"
                    return (digest + "  /data/dump.rdb").encode()
                with patch.object(readiness, "load_configuration", return_value=config), \
                        patch.object(readiness, "verify_gateway", return_value="http://localhost:12345"), \
                        patch.object(readiness, "verify_source_commit"), \
                        patch.object(readiness, "verify_pristine_store", return_value=1), \
                        patch.object(readiness, "inspect_container", return_value=self.container()), \
                        patch.object(readiness, "redis", side_effect=["OK", json.dumps(marker), "redis_version:7.4.11"]), \
                        patch.object(readiness, "resource", return_value={"urls": [{"url": "http://localhost:12346"}]}), \
                        patch.object(readiness, "request", side_effect=[(200, {"access_token": "private-test-token"}), (200, {"status": "Activated"})]), \
                        patch.object(readiness, "invoke", side_effect=invoke):
                    if marker == valid:
                        readiness.activate(directory, {}, source)
                        self.assertEqual(json.loads((Path(root) / "maintenance-result.json").read_text())["marker"], valid)
                    else:
                        with self.assertRaises(RuntimeError):
                            readiness.activate(directory, {}, source)
                        self.assertFalse((Path(root) / "maintenance-result.json").exists())

    def test_timeout_allows_shell_trap_and_owned_finally_cleanup(self):
        with tempfile.TemporaryDirectory() as root, patch.dict(os.environ, {"FC_EPIC9_LOCK_ROOT": root}):
            marker = Path(root) / "child-cleaned"
            host = {"appHostPath": str(readiness.APPHOST), "appHostPid": 123}
            real_invoke = readiness.invoke
            running = False
            def invoke(args, **kwargs):
                nonlocal running
                if args[0] != "aspire":
                    return real_invoke(args, **kwargs)
                if args[1] == "start":
                    running = True
                    return json.dumps(host).encode()
                if args[1] == "stop":
                    running = False
                return b""
            with patch.object(readiness, "processes", side_effect=lambda env: [host] if running else []), \
                    patch.object(readiness, "invoke", side_effect=invoke), self.assertRaises(subprocess.TimeoutExpired):
                with readiness.owned_apphost(Path(root), os.environ.copy()):
                    readiness.invoke(["bash", "-c", 'trap \'printf cleaned > "$1"\' EXIT; trap \'exit 143\' TERM; sleep 30 & wait', "bash", str(marker)], timeout=0.2)
            self.assertTrue(marker.exists())
            self.assertFalse(running)

    def test_timeout_forces_an_uncooperative_child_after_bounded_grace(self):
        with tempfile.TemporaryDirectory() as root, patch.object(readiness, "TERMINATION_GRACE_SECONDS", 0.1):
            pid_file = Path(root) / "child.pid"
            code = "import os,signal,sys,time; from pathlib import Path; signal.signal(signal.SIGTERM,signal.SIG_IGN); Path(sys.argv[1]).write_text(str(os.getpid())); time.sleep(30)"
            started = time.monotonic()
            with self.assertRaises(subprocess.TimeoutExpired):
                readiness.invoke([sys.executable, "-c", code, str(pid_file)], timeout=0.2)
            self.assertLess(time.monotonic() - started, 2)
            with self.assertRaises(ProcessLookupError):
                os.kill(int(pid_file.read_text()), 0)

    def test_sigterm_runs_child_trap_and_owned_finally_cleanup(self):
        with tempfile.TemporaryDirectory() as root:
            script = Path(root) / "signal-fixture.py"
            script.write_text("""import contextlib, importlib.util, json, os, sys
from pathlib import Path
from unittest.mock import patch
s=importlib.util.spec_from_file_location('readiness',sys.argv[1]); r=importlib.util.module_from_spec(s); s.loader.exec_module(r)
directory=Path(sys.argv[2]); os.environ['FC_EPIC9_LOCK_ROOT']=str(directory)
host={'appHostPath':str(r.APPHOST),'appHostPid':123}; running=False; real_invoke=r.invoke
def invoke(args,**kwargs):
 global running
 if args[0]!='aspire': return real_invoke(args,**kwargs)
 if args[1]=='start': running=True; return json.dumps(host).encode()
 if args[1]=='stop': running=False; (directory/'parent-cleaned').touch()
 return b''
try:
 with r.cancellation_handlers(), patch.object(r,'invoke',side_effect=invoke), patch.object(r,'processes',side_effect=lambda env:[host] if running else []):
  with r.owned_apphost(directory,os.environ.copy()):
   r.invoke(['bash',str(directory/'child.sh'),str(directory/'child-cleaned'),str(directory/'ready')])
except RuntimeError:
 sys.exit(1)
""")
            (Path(root) / "child.sh").write_text('trap \'printf cleaned > "$1"\' EXIT\ntrap \'exit 143\' TERM\ntouch "$2"\nsleep 30 & wait\n')
            with subprocess.Popen([sys.executable, str(script), str(Path(readiness.__file__)), root], stdout=subprocess.PIPE, stderr=subprocess.PIPE) as process:
                deadline = time.monotonic() + 5
                while not (Path(root) / "ready").exists() and time.monotonic() < deadline:
                    time.sleep(0.01)
                self.assertTrue((Path(root) / "ready").exists())
                process.send_signal(signal.SIGTERM)
                output, errors = process.communicate(timeout=5)
                self.assertEqual(process.returncode, 1, (output, errors))
            self.assertTrue((Path(root) / "child-cleaned").exists())
            self.assertTrue((Path(root) / "parent-cleaned").exists())


if __name__ == "__main__":
    unittest.main()
