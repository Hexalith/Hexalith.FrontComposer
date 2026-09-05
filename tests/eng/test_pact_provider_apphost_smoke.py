#!/usr/bin/env python3

from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))

import pact_provider_apphost_smoke as smoke  # noqa: E402


class FakeRuntime(smoke.SmokeRuntime):
    def __init__(self, *, start_code: int = 0, cleanup_running: bool = False) -> None:
        self.start_code = start_code
        self.cleanup_running = cleanup_running
        self.commands: list[list[str]] = []
        self.describe_count = 0

    def command(self, arguments: list[str], timeout: int) -> smoke.CommandResult:
        del timeout
        self.commands.append(arguments)
        operation = arguments[1]
        if operation == "start":
            return smoke.CommandResult(self.start_code)
        if operation == "wait":
            return smoke.CommandResult(0)
        if operation == "stop":
            return smoke.CommandResult(0)
        if operation == "describe":
            if self.start_code != 0:
                return smoke.CommandResult(1)
            self.describe_count += 1
            if self.describe_count == 1:
                document = {
                    "resources": [
                        {"name": name, "endpoints": [{"url": f"https://{name}.invalid:443"}]}
                        for name in smoke.REQUIRED_RESOURCES
                    ]
                }
                return smoke.CommandResult(0, json.dumps(document))
            return smoke.CommandResult(0, '{"resources":[{"name":"eventstore"}]}') if self.cleanup_running else smoke.CommandResult(1)
        raise AssertionError(arguments)

    def json_request(
        self,
        url: str,
        *,
        method: str = "GET",
        token: str | None = None,
        form: dict[str, str] | None = None,
        body: dict[str, Any] | None = None,
        timeout: int = 10,
    ) -> tuple[int, dict[str, Any], dict[str, str]]:
        del method, form, body, timeout
        if url.endswith("/protocol/openid-connect/token"):
            self.assert_token_absent(token)
            return 200, {"access_token": "synthetic-token-never-persisted"}, {}
        if url.endswith("/health"):
            self.assert_token_present(token)
            return 200, {}, {}
        if url.endswith("/api/v1/commands"):
            self.assert_token_present(token)
            return 202, {"correlationId": "01HFAKE0000000000000000000"}, {}
        if "/api/v1/commands/status/" in url:
            self.assert_token_present(token)
            return 200, {"status": "Completed"}, {}
        if url.endswith("/api/v1/queries"):
            self.assert_token_present(token)
            return 200, {}, {"X-Hexalith-Query-Provenance": "ProjectionBacked"}
        raise AssertionError(url)

    def signalr_connect(self, hub_url: str, token: str, timeout: int = 10) -> bool:
        del timeout
        self.assert_token_present(token)
        return hub_url.endswith("/hubs/projection-changes")

    @staticmethod
    def assert_token_present(token: str | None) -> None:
        if token != "synthetic-token-never-persisted":
            raise AssertionError("authenticated request did not receive the acquired local token")

    @staticmethod
    def assert_token_absent(token: str | None) -> None:
        if token is not None:
            raise AssertionError("token acquisition unexpectedly received a bearer")


class PactProviderAppHostSmokeTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.output = Path(self.temporary.name) / "apphost-smoke.json"

    def test_success_requires_all_resources_authenticated_surfaces_and_clean_stop(self) -> None:
        runtime = FakeRuntime()

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 0)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(document["finalVerdict"], "passed")
        self.assertEqual(document["reasonCodes"], [])
        self.assertEqual(document["startup"]["resourceWaits"], {name: "healthy" for name in smoke.REQUIRED_RESOURCES})
        self.assertEqual(set(document["observations"]), set(smoke.OBSERVATIONS))
        self.assertTrue(all(item["authenticated"] for item in document["observations"].values()))
        self.assertTrue(all(item["result"] == "passed" for item in document["observations"].values()))
        self.assertEqual(document["cleanup"]["result"], "clean")
        self.assertNotIn("synthetic-token", self.output.read_text(encoding="utf-8"))
        self.assertEqual(runtime.commands[0][:2], ["aspire", "stop"])
        self.assertEqual(runtime.commands[1][:2], ["aspire", "start"])
        self.assertEqual(runtime.commands[-2][:2], ["aspire", "stop"])
        self.assertEqual(runtime.commands[-1][:2], ["aspire", "describe"])

    def test_handler_computed_query_provenance_is_accepted_for_tenant_routes(self) -> None:
        runtime = FakeRuntime()

        def json_request(url: str, *, method: str = "GET", token: str | None = None, form: dict[str, str] | None = None, body: dict[str, Any] | None = None, timeout: int = 10) -> tuple[int, dict[str, Any], dict[str, str]]:
            if url.endswith("/api/v1/queries"):
                runtime.assert_token_present(token)
                return 200, {}, {"X-Hexalith-Query-Provenance": "HandlerComputed"}
            return FakeRuntime.json_request(runtime, url, method=method, token=token, form=form, body=body, timeout=timeout)

        runtime.json_request = json_request  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 0)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(document["observations"]["queryProvenance"]["reasonCode"], "query.handler-computed")
        self.assertEqual(document["observations"]["queryProvenance"]["provenance"], "HandlerComputed")

    def test_start_failure_is_recorded_and_still_attempts_clean_stop(self) -> None:
        runtime = FakeRuntime(start_code=2)

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 1)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(document["finalVerdict"], "failed")
        self.assertIn("apphost.start.failed", document["reasonCodes"])
        self.assertEqual(document["cleanup"]["result"], "clean")
        self.assertEqual([item[1] for item in runtime.commands], ["stop", "start", "stop", "describe"])

    def test_cleanup_failure_overrides_an_otherwise_passing_capture(self) -> None:
        runtime = FakeRuntime(cleanup_running=True)

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 1)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(document["finalVerdict"], "failed")
        self.assertIn("apphost.cleanup.incomplete", document["reasonCodes"])
        self.assertFalse(document["cleanup"]["hostStopped"])

    def test_resource_parser_and_websocket_url_are_bounded(self) -> None:
        records = smoke._resource_records({"items": [{"Name": "eventstore", "urls": ["http://one", "https://two"]}]})
        self.assertEqual(smoke._resource_endpoint(records, "eventstore"), "")
        websocket = smoke._websocket_url("https://localhost:7273/hubs/projection-changes", "connection/id", "token value")
        self.assertEqual(
            websocket,
            "wss://localhost:7273/hubs/projection-changes?id=connection%2Fid&access_token=token+value",
        )

    def test_describe_uses_display_name_and_https_urls_for_replica_resources(self) -> None:
        document = {
            "resources": [
                {
                    "name": "eventstore-wfstefgr",
                    "displayName": "eventstore",
                    "urls": [
                        {"name": "management", "url": "https://localhost:8543", "isInternal": True},
                        {"name": "http", "url": "https://localhost:8180"},
                        {"name": "https", "url": "https://localhost:7141"},
                    ],
                }
            ]
        }
        records = smoke._resource_records(document)
        self.assertEqual([smoke._logical_name(item) for item in records], ["eventstore"])
        self.assertEqual(smoke._resource_endpoint(records, "eventstore"), "https://localhost:7141")
        security = {
            "name": "security-feqgxzbe",
            "displayName": "security",
            "urls": [
                {"name": "management", "url": "https://localhost:8543", "isInternal": True},
                {"name": "http", "url": "https://localhost:8180"},
            ],
        }
        self.assertEqual(smoke._resource_endpoint([security], "security"), "https://localhost:8180")
        eventstore = {
            "name": "eventstore-wfstefgr",
            "displayName": "eventstore",
            "urls": [
                {"name": "http", "url": "http://localhost:8080"},
                {"name": "https", "url": "https://localhost:7141"},
            ],
        }
        self.assertEqual(
            smoke._resource_public_urls([eventstore], "eventstore")[:2],
            ["http://localhost:8080", "http://127.0.0.1:8080"],
        )
        eventstore["urls"].append({"name": "target", "url": "http://127.0.0.1:19876", "isInternal": True})
        signalr_urls = smoke._resource_signalr_urls([eventstore], "eventstore")
        self.assertIn("http://localhost:8080", signalr_urls)
        self.assertIn("http://127.0.0.1:19876", signalr_urls)
        internal_only = {
            "displayName": "eventstore",
            "urls": [
                {"name": "management", "url": "https://localhost:8543", "isInternal": True},
                {"name": "target", "url": "http://127.0.0.1:19876", "isInternal": True},
            ],
        }
        self.assertEqual(smoke._resource_endpoint([internal_only], "eventstore"), "")

    def test_capture_prefers_http_loopback_after_the_first_probe_fails(self) -> None:
        runtime = FakeRuntime()
        requested: list[str] = []

        def command(arguments: list[str], timeout: int) -> smoke.CommandResult:
            del timeout
            runtime.commands.append(arguments)
            operation = arguments[1]
            if operation == "start":
                return smoke.CommandResult(0)
            if operation == "wait":
                return smoke.CommandResult(0)
            if operation == "stop":
                return smoke.CommandResult(0)
            if operation == "describe":
                runtime.describe_count += 1
                if runtime.describe_count == 1:
                    resources = []
                    for name in smoke.REQUIRED_RESOURCES:
                        if name == "security":
                            resources.append(
                                {
                                    "name": "security-replica",
                                    "displayName": "security",
                                    "urls": [
                                        {"name": "http", "url": "http://localhost:18180"},
                                        {"name": "https", "url": "https://localhost:18443"},
                                    ],
                                }
                            )
                        elif name == "eventstore":
                            resources.append(
                                {
                                    "name": "eventstore-replica",
                                    "displayName": "eventstore",
                                    "urls": [
                                        {"name": "http", "url": "http://localhost:18080"},
                                        {"name": "https", "url": "https://localhost:17141"},
                                    ],
                                }
                            )
                        else:
                            resources.append({"name": name, "endpoints": [{"url": f"https://{name}.invalid:443"}]})
                    return smoke.CommandResult(0, json.dumps({"resources": resources}))
                return smoke.CommandResult(1)
            raise AssertionError(arguments)

        def json_request(
            url: str,
            *,
            method: str = "GET",
            token: str | None = None,
            form: dict[str, str] | None = None,
            body: dict[str, Any] | None = None,
            timeout: int = 10,
        ) -> tuple[int, dict[str, Any], dict[str, str]]:
            requested.append(url)
            if len(requested) == 1:
                return 0, {}, {}
            return FakeRuntime.json_request(
                runtime, url, method=method, token=token, form=form, body=body, timeout=timeout
            )

        runtime.command = command  # type: ignore[method-assign]
        runtime.json_request = json_request  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 0)
        self.assertTrue(requested, "capture must issue authenticated HTTP probes")
        self.assertTrue(requested[0].startswith("http://"), requested[0])
        for url in requested[1:]:
            if "/protocol/openid-connect/token" in url or url.endswith("/health") or url.endswith("/alive"):
                self.assertTrue(
                    url.startswith("http://localhost") or url.startswith("http://127.0.0.1"),
                    url,
                )


if __name__ == "__main__":
    unittest.main()
