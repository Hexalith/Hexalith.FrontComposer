#!/usr/bin/env python3

from __future__ import annotations

import base64
import contextlib
import hashlib
import http.server
import io
import json
import os
import sys
import tempfile
import threading
import time
import unittest
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Any
from unittest import mock


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))

import pact_provider_apphost_smoke as smoke  # noqa: E402

REAL_DISCOVER_ASSETS_GRAPHS = smoke._discover_assets_graphs_from_json

def _synthetic_package_ledger(
    assets_paths: list[Path], captured_at: str | None = None
) -> dict[str, Any]:
    content_hash = base64.b64encode(bytes(range(64))).decode("ascii")
    binding = {
        "id": "Synthetic.Package",
        "version": "1.0.0",
        "relativePath": "synthetic.package/1.0.0",
        "contentHashSha512": content_hash,
    }
    files = [
        {
            "path": "lib/net10.0/Synthetic.Package.dll",
            "bytes": 1,
            "sha256": "a" * 64,
        }
    ]
    package = {
        **binding,
        "nupkgSha512": content_hash,
        "files": files,
        "treeSha256": smoke.runtime_evidence._package_tree_sha256(files),
    }
    tool_bindings = [
        {
            "id": package_id,
            "version": version,
            "relativePath": f"{package_id.casefold()}/{version.casefold()}",
            "contentHashSha512": content_hash,
        }
        for package_id, version in smoke.runtime_evidence.APPHOST_TOOL_PACKAGES
    ]
    tool_packages = [
        {
            **tool_binding,
            "nupkgSha512": content_hash,
            "files": files,
            "treeSha256": smoke.runtime_evidence._package_tree_sha256(files),
        }
        for tool_binding in tool_bindings
    ]
    entries = {
        "assetsGraphs": [
            {
                "path": path.resolve(strict=False).relative_to(smoke.ROOT).as_posix(),
                "sha256": "b" * 64,
                "packages": [binding],
            }
            for path in sorted(assets_paths)
        ],
        "toolPackages": tool_bindings,
        "packages": sorted(
            [package, *tool_packages],
            key=lambda item: (item["id"].casefold(), item["version"].casefold()),
        ),
    }
    return {
        "schema": smoke.runtime_evidence.PACKAGE_LEDGER_SCHEMA,
        "capturedAt": captured_at or datetime.now(timezone.utc).isoformat(),
        "packageRoot": "fresh-external",
        **entries,
        "treeSha256": hashlib.sha256(
            json.dumps(entries, sort_keys=True, separators=(",", ":")).encode("utf-8")
        ).hexdigest(),
    }


class FakeRuntime(smoke.SmokeRuntime):
    def __init__(
        self,
        *,
        start_code: int = 0,
        clean_code: int = 0,
        restore_code: int = 0,
        build_code: int = 0,
        cleanup_running: bool = False,
        resource_names: list[str] | None = None,
        resource_records: list[dict[str, Any]] | None = None,
    ) -> None:
        self.start_code = start_code
        self.clean_code = clean_code
        self.restore_code = restore_code
        self.build_code = build_code
        self.cleanup_running = cleanup_running
        self.resource_names = resource_names or list(smoke.REQUIRED_RESOURCES)
        self.resource_records = resource_records
        self.commands: list[list[str]] = []
        self.command_timeouts: list[float] = []
        self.describe_count = 0
        self.started = False

    def command(self, arguments: list[str], timeout: float) -> smoke.CommandResult:
        self.command_timeouts.append(timeout)
        self.commands.append(arguments)
        operation = arguments[1]
        if arguments[0] == "dotnet":
            if operation == "clean":
                return smoke.CommandResult(self.clean_code)
            if operation == "restore":
                return smoke.CommandResult(self.restore_code)
            if operation == "build":
                return smoke.CommandResult(self.build_code)
            if operation == "msbuild":
                return self.evaluation_result(arguments)
            raise AssertionError(arguments)
        if operation == "start":
            stderr = "synthetic start failure" if self.start_code != 0 else ""
            if self.start_code == 0:
                self.started = True
            return smoke.CommandResult(self.start_code, "", stderr)
        if operation == "wait":
            return smoke.CommandResult(0)
        if operation == "stop":
            if not self.cleanup_running or not self.started:
                self.started = False
            return smoke.CommandResult(0)
        if operation == "describe":
            if not self.started:
                return smoke.CommandResult(1)
            self.describe_count += 1
            document = {
                "resources": self.resource_records or [
                    {
                        "name": name,
                        "endpoints": [{"url": f"http://127.0.0.1:{18000 + index}"}],
                    }
                    for index, name in enumerate(self.resource_names)
                ]
            }
            return smoke.CommandResult(0, json.dumps(document))
        if operation == "ps":
            document = (
                [{"appHostPath": str((smoke.ROOT / smoke.APPHOST_RELATIVE).resolve())}]
                if self.started
                else []
            )
            return smoke.CommandResult(0, json.dumps(document))
        raise AssertionError(arguments)

    def source_graph_is_exact(self, project_references: list[Path]) -> bool:
        return bool(project_references)

    def json_request(
        self,
        url: str,
        *,
        method: str = "GET",
        token: str | None = None,
        form: dict[str, str] | None = None,
        body: dict[str, Any] | None = None,
        timeout: float = 10,
        deadline: float | None = None,
    ) -> tuple[int, dict[str, Any], dict[str, str]]:
        del method, form, timeout, deadline
        if url.endswith("/protocol/openid-connect/token"):
            self.assert_token_absent(token)
            return 200, {"access_token": "synthetic-token-never-persisted"}, {}
        if url.endswith("/health"):
            self.assert_token_absent(token)
            return 200, {}, {}
        if url.endswith("/api/v1/commands"):
            if token == "invalid-local-evidence-token":
                return 401, {}, {}
            self.assert_token_present(token)
            return 202, {"correlationId": (body or {}).get("messageId")}, {}
        if "/api/v1/commands/status/" in url:
            if token == "invalid-local-evidence-token":
                return 401, {}, {}
            self.assert_token_present(token)
            return 200, {"status": "Completed"}, {}
        if url.endswith("/api/v1/queries"):
            if token == "invalid-local-evidence-token":
                return 401, {}, {}
            self.assert_token_present(token)
            tenant_id = body.get("entityId") if isinstance(body, dict) else None
            return 200, {"payload": {"TenantId": tenant_id}}, {"X-Hexalith-Query-Provenance": "HandlerComputed"}
        raise AssertionError(url)

    def signalr_negotiate_status(
        self,
        hub_url: str,
        token: str | None,
        timeout: float = 10,
        deadline: float | None = None,
    ) -> int:
        del timeout, deadline
        if not hub_url.endswith("/hubs/projection-changes"):
            raise AssertionError(hub_url)
        return 401 if token == "invalid-local-evidence-token" else 200

    def signalr_connect(self, hub_url: str, token: str, timeout: float = 10, deadline: float | None = None) -> bool:
        del timeout, deadline
        self.assert_token_present(token)
        return hub_url.endswith("/hubs/projection-changes")

    def signalr_invalid_upgrade_status(
        self,
        hub_url: str,
        negotiation_token: str,
        invalid_token: str,
        timeout: float = 10,
        deadline: float | None = None,
    ) -> int:
        del timeout, deadline
        self.assert_token_present(negotiation_token)
        return (
            401
            if hub_url.endswith("/hubs/projection-changes")
            and invalid_token == "invalid-local-evidence-token"
            else 0
        )

    @staticmethod
    def assert_token_present(token: str | None) -> None:
        if token != "synthetic-token-never-persisted":
            raise AssertionError("authenticated request did not receive the acquired local token")

    @staticmethod
    def assert_token_absent(token: str | None) -> None:
        if token is not None:
            raise AssertionError("token acquisition unexpectedly received a bearer")

    @staticmethod
    def source_projects() -> list[Path]:
        return [
            smoke.ROOT / relative
            for relative in (
                "references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.Abstractions/Hexalith.EventStore.Admin.Abstractions.csproj",
                "references/Hexalith.Tenants/src/Hexalith.Tenants.Api/Hexalith.Tenants.Api.csproj",
                "references/Hexalith.Parties/src/Hexalith.Parties.AdminPortal/Hexalith.Parties.AdminPortal.csproj",
                "references/Hexalith.Memories/src/Hexalith.Memories.AccessTelemetry.Clock/Hexalith.Memories.AccessTelemetry.Clock.csproj",
                "references/Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/Hexalith.Commons.Aspire.csproj",
            )
        ]

    @staticmethod
    def evaluation_output() -> str:
        properties = {
            name: str(value).lower()
            for name, value in smoke.APPHOST_BUILD_PROPERTIES.items()
        }
        properties.update(
            {
                name: str(smoke.ROOT / relative)
                for name, relative in smoke.SOURCE_ROOT_PROPERTIES.items()
            }
        )
        properties["TargetFramework"] = smoke.APPHOST_EVALUATION_TARGET_FRAMEWORK
        properties["MSBuildAllProjects"] = str(smoke.APPHOST)
        project_references = [
            {"Identity": str(path), "FullPath": str(path)}
            for path in FakeRuntime.source_projects()
        ]
        return json.dumps({
            "Properties": properties,
            "Items": {
                "ProjectReference": project_references,
                "PackageReference": [],
                "Reference": [],
                "ReferencePath": [],
                "Analyzer": [],
                "AdditionalFiles": [],
                "Content": [],
                "None": [],
                "NativeCopyLocalItems": [],
                "RuntimeCopyLocalItems": [],
            },
        })

    @staticmethod
    def evaluation_result(
        arguments: list[str],
        document: str | None = None,
        *,
        stdout: str = "",
    ) -> smoke.CommandResult:
        option = next(
            argument
            for argument in arguments
            if argument.startswith("-getResultOutputFile:")
        )
        result_path = Path(option.split(":", 1)[1])
        result_path.write_text(
            document if document is not None else FakeRuntime.evaluation_output(),
            encoding="utf-8",
        )
        return smoke.CommandResult(0, stdout)


class PactProviderAppHostSmokeTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.output = Path(self.temporary.name) / "apphost-smoke.json"
        manifest = {
            "capturedRevision": smoke._git(ROOT, "rev-parse", "HEAD"),
            "treeSha256": "0" * 64,
        }
        patcher = mock.patch.object(
            smoke.runtime_evidence,
            "runtime_input_manifest",
            return_value=(manifest, []),
        )
        patcher.start()
        self.addCleanup(patcher.stop)
        assets_patcher = mock.patch.object(
            smoke,
            "_discover_assets_graphs_from_json",
            return_value=(
                [smoke.APPHOST, *FakeRuntime.source_projects()],
                [smoke.APPHOST.parent / "obj/project.assets.json"],
            ),
        )
        assets_patcher.start()
        self.addCleanup(assets_patcher.stop)
        target_patcher = mock.patch.object(
            smoke.runtime_evidence,
            "_restored_project_target_framework",
            return_value=smoke.APPHOST_EVALUATION_TARGET_FRAMEWORK,
        )
        target_patcher.start()
        self.addCleanup(target_patcher.stop)
        sdk_patcher = mock.patch.object(
            smoke,
            "_selected_dotnet_root",
            return_value=ROOT,
        )
        sdk_patcher.start()
        self.addCleanup(sdk_patcher.stop)
        def package_ledger(
            repository_root: Path,
            package_root: Path,
            assets_paths: Any,
            *,
            captured_at: str | None = None,
            prune_unselected: bool = False,
            tool_packages: Any = (),
        ) -> tuple[dict[str, Any], list[str]]:
            del repository_root, package_root, prune_unselected
            self.assertEqual(tuple(tool_packages), smoke.runtime_evidence.APPHOST_TOOL_PACKAGES)
            return _synthetic_package_ledger(
                list(assets_paths), captured_at
            ), []

        ledger_patcher = mock.patch.object(
            smoke.runtime_evidence,
            "resolved_package_ledger",
            side_effect=package_ledger,
        )
        ledger_patcher.start()
        self.addCleanup(ledger_patcher.stop)
        outputs_patcher = mock.patch.object(
            smoke,
            "_runtime_output_inventory",
            return_value=([{"path": "app.dll", "bytes": 1, "sha256": "2" * 64}], True),
        )
        outputs_patcher.start()
        self.addCleanup(outputs_patcher.stop)
        self.dapr_paths = {
            relative: Path(self.temporary.name) / Path(relative).name
            for relative in smoke.DAPR_NAME_RESOLUTION_RELATIVES
        }
        dapr_patcher = mock.patch.object(
            smoke,
            "_dapr_name_resolution_paths",
            return_value=self.dapr_paths,
        )
        dapr_patcher.start()
        self.addCleanup(dapr_patcher.stop)

    def test_success_requires_all_resources_authenticated_surfaces_and_clean_stop(self) -> None:
        runtime = FakeRuntime()

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 0)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(
            document["schema"],
            "hexalith.frontcomposer.pact-provider-reconciliation-apphost-smoke.v3",
        )
        self.assertEqual(document["identity"]["frontComposerRevision"], smoke._git(ROOT, "rev-parse", "HEAD"))
        self.assertEqual(document["finalVerdict"], "passed")
        self.assertEqual(document["reasonCodes"], [])
        self.assertEqual(document["startup"]["resourceWaits"], {name: "healthy" for name in smoke.REQUIRED_RESOURCES})
        self.assertEqual(set(document["observations"]), set(smoke.OBSERVATIONS))
        self.assertEqual(set(document["authorizationControls"]), set(smoke.AUTHORIZATION_CONTROLS))
        self.assertFalse(document["observations"]["health"]["authenticated"])
        self.assertEqual(
            document["observations"]["health"]["reasonCode"],
            "health.readiness.succeeded",
        )
        self.assertTrue(all(
            item["authenticated"]
            for name, item in document["observations"].items()
            if name != "health"
        ))
        self.assertTrue(all(item["result"] == "passed" for item in document["observations"].values()))
        self.assertTrue(all(item["result"] == "passed" for item in document["authorizationControls"].values()))
        self.assertEqual(document["cleanup"]["result"], "clean")
        self.assertEqual(document["cleanup"]["listenerConfirmation"], "ports-probed-closed")
        self.assertNotIn("synthetic-token", self.output.read_text(encoding="utf-8"))
        self.assertEqual(runtime.commands[0][:2], ["aspire", "stop"])
        self.assertEqual(runtime.commands[1][:2], ["aspire", "describe"])
        self.assertEqual(runtime.commands[2][:2], ["aspire", "ps"])
        self.assertEqual(runtime.commands[3][:2], ["dotnet", "clean"])
        self.assertEqual(runtime.commands[4][:2], ["dotnet", "restore"])
        self.assertIn("-p:Configuration=Debug", runtime.commands[4])
        self.assertIn("--force", runtime.commands[4])
        self.assertIn("--force-evaluate", runtime.commands[4])
        build_index = next(
            index
            for index, command in enumerate(runtime.commands)
            if command[:2] == ["dotnet", "build"]
        )
        start_index = next(
            index
            for index, command in enumerate(runtime.commands)
            if command[:2] == ["aspire", "start"]
        )
        expected_evaluations = 1 + len(FakeRuntime.source_projects())
        self.assertEqual(
            sum(command[:2] == ["dotnet", "msbuild"] for command in runtime.commands[5:build_index]),
            expected_evaluations,
        )
        self.assertEqual(
            sum(command[:2] == ["dotnet", "msbuild"] for command in runtime.commands[build_index:start_index]),
            expected_evaluations,
        )
        evaluation_commands = [
            command for command in runtime.commands
            if command[:2] == ["dotnet", "msbuild"]
        ]
        self.assertTrue(all("-target:ResolveReferences" in command for command in evaluation_commands))
        self.assertTrue(all("-p:BuildProjectReferences=false" in command for command in evaluation_commands))
        self.assertTrue(all("-p:GeneratePackageOnBuild=false" in command for command in evaluation_commands))
        self.assertTrue(all("-p:TargetFramework=net10.0" in command for command in evaluation_commands))
        self.assertTrue(all("-m:1" in command for command in evaluation_commands))
        self.assertTrue(all("-nodeReuse:false" in command for command in evaluation_commands))
        self.assertTrue(all(
            any(argument.startswith("-getResultOutputFile:") for argument in command)
            for command in evaluation_commands
        ))
        self.assertTrue(all(any("ReferencePath" in item for item in command) for command in evaluation_commands))
        self.assertIn("--no-restore", runtime.commands[build_index])
        self.assertIn("--isolated", runtime.commands[start_index])
        self.assertEqual(runtime.commands[-3][:2], ["aspire", "stop"])
        self.assertEqual(runtime.commands[-2][:2], ["aspire", "describe"])
        self.assertEqual(runtime.commands[-1][:2], ["aspire", "ps"])

    def test_readiness_health_waits_for_the_application_after_sidecar_health(self) -> None:
        runtime = FakeRuntime()
        health_attempts = 0

        def json_request(url: str, *, method: str = "GET", token: str | None = None, form: dict[str, str] | None = None, body: dict[str, Any] | None = None, timeout: int = 10, deadline: float | None = None) -> tuple[int, dict[str, Any], dict[str, str]]:
            nonlocal health_attempts
            if url.endswith(("/health", "/alive")):
                runtime.assert_token_absent(token)
                health_attempts += 1
                if health_attempts <= 2:
                    return 0, {}, {}
            return FakeRuntime.json_request(
                runtime, url, method=method, token=token, form=form, body=body, timeout=timeout, deadline=deadline
            )

        runtime.json_request = json_request  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 0)
        self.assertGreater(health_attempts, 2)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(document["observations"]["health"]["result"], "passed")

    def test_disposable_writer_protocol_cutover_is_guarded_and_authenticated(self) -> None:
        runtime = FakeRuntime()
        activated = False
        cutover_requests: list[tuple[str | None, dict[str, Any] | None]] = []

        def json_request(
            url: str,
            *,
            method: str = "GET",
            token: str | None = None,
            form: dict[str, str] | None = None,
            body: dict[str, Any] | None = None,
            timeout: float = 10,
            deadline: float | None = None,
        ) -> tuple[int, dict[str, Any], dict[str, str]]:
            nonlocal activated
            del timeout, deadline
            if url.endswith("/health") and not activated:
                runtime.assert_token_absent(token)
                return 503, {
                    "status": "Unhealthy",
                    "results": {
                        smoke.WRITER_PROTOCOL_HEALTH_CHECK: {"status": "Unhealthy"},
                        "self": {"status": "Healthy"},
                    },
                }, {}
            if url.endswith(smoke.WRITER_PROTOCOL_ACTIVATION_PATH):
                self.assertEqual(method, "POST")
                self.assertIsNone(form)
                cutover_requests.append((token, body))
                if token == "invalid-local-evidence-token":
                    return 401, {}, {}
                runtime.assert_token_present(token)
                self.assertEqual(body, {
                    "cutoverCommit": smoke._git(
                        smoke.ROOT / "references/Hexalith.EventStore", "rev-parse", "HEAD"
                    ),
                    "backupReference": "frontcomposer-disposable-smoke-no-durable-state",
                    "writersQuiesced": True,
                    "retryWorkersQuiesced": True,
                    "downgradeProhibitedAcknowledged": True,
                })
                activated = True
                return 200, {"status": "Activated"}, {}
            return FakeRuntime.json_request(
                runtime,
                url,
                method=method,
                token=token,
                form=form,
                body=body,
            )

        runtime.json_request = json_request  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 0)
        self.assertTrue(activated)
        self.assertEqual(
            [token for token, _ in cutover_requests],
            ["invalid-local-evidence-token", "synthetic-token-never-persisted"],
        )
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(document["observations"]["health"]["result"], "passed")
        self.assertNotIn("synthetic-token", self.output.read_text(encoding="utf-8"))

    def test_writer_protocol_cutover_requires_every_sibling_to_be_ready(self) -> None:
        self.assertTrue(smoke._writer_protocol_is_only_unhealthy({
            "results": {
                smoke.WRITER_PROTOCOL_HEALTH_CHECK: {"status": "Unhealthy"},
                "redis": {"status": "Healthy"},
                "configstore": {"status": "Degraded"},
            },
        }))
        self.assertFalse(smoke._writer_protocol_is_only_unhealthy({
            "results": {
                smoke.WRITER_PROTOCOL_HEALTH_CHECK: {"status": "Unhealthy"},
                "redis": {"status": "Unhealthy"},
            },
        }))
        self.assertFalse(smoke._writer_protocol_is_only_unhealthy({
            "results": {
                smoke.WRITER_PROTOCOL_HEALTH_CHECK: {"status": "Healthy"},
            },
        }))

    def test_handler_computed_query_provenance_is_accepted_for_tenant_routes(self) -> None:
        runtime = FakeRuntime()

        def json_request(url: str, *, method: str = "GET", token: str | None = None, form: dict[str, str] | None = None, body: dict[str, Any] | None = None, timeout: int = 10, deadline: float | None = None) -> tuple[int, dict[str, Any], dict[str, str]]:
            if url.endswith("/api/v1/queries") and token == "synthetic-token-never-persisted":
                runtime.assert_token_present(token)
                return 200, {"payload": {"TenantId": (body or {}).get("entityId")}}, {"X-Hexalith-Query-Provenance": "HandlerComputed"}
            return FakeRuntime.json_request(runtime, url, method=method, token=token, form=form, body=body, timeout=timeout, deadline=deadline)

        runtime.json_request = json_request  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 0)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(document["observations"]["queryProvenance"]["reasonCode"], "query.handler-computed")
        self.assertEqual(document["observations"]["queryProvenance"]["provenance"], "HandlerComputed")

    def test_query_provenance_accepts_metadata_when_header_is_absent(self) -> None:
        runtime = FakeRuntime()

        def json_request(url: str, *, method: str = "GET", token: str | None = None, form: dict[str, str] | None = None, body: dict[str, Any] | None = None, timeout: int = 10, deadline: float | None = None) -> tuple[int, dict[str, Any], dict[str, str]]:
            if url.endswith("/api/v1/queries") and token == "synthetic-token-never-persisted":
                runtime.assert_token_present(token)
                return 200, {"metadata": {"provenance": "HandlerComputed"}, "payload": {"TenantId": (body or {}).get("entityId")}}, {}
            return FakeRuntime.json_request(runtime, url, method=method, token=token, form=form, body=body, timeout=timeout, deadline=deadline)

        runtime.json_request = json_request  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 0)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(document["observations"]["queryProvenance"]["reasonCode"], "query.handler-computed")
        self.assertEqual(document["observations"]["queryProvenance"]["provenance"], "HandlerComputed")

    def test_conflicting_query_provenance_channels_fail_closed(self) -> None:
        self.assertEqual(
            smoke._query_provenance(
                {"X-Hexalith-Query-Provenance": "HandlerComputed"},
                {"metadata": {"provenance": "ProjectionBacked"}},
            ),
            "",
        )

    def test_command_response_correlation_must_equal_the_submitted_message_id(self) -> None:
        runtime = FakeRuntime()
        original_request = runtime.json_request

        def json_request(
            url: str,
            *,
            method: str = "GET",
            token: str | None = None,
            form: dict[str, str] | None = None,
            body: dict[str, Any] | None = None,
            timeout: float = 10,
            deadline: float | None = None,
        ) -> tuple[int, dict[str, Any], dict[str, str]]:
            if url.endswith("/api/v1/commands") and token == "synthetic-token-never-persisted":
                return 202, {"correlationId": "01HWRONG000000000000000000"}, {}
            return original_request(
                url,
                method=method,
                token=token,
                form=form,
                body=body,
                timeout=timeout,
                deadline=deadline,
            )

        runtime.json_request = json_request  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 1)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(document["observations"]["commandSubmit"]["result"], "failed")
        self.assertEqual(document["observations"]["commandSubmit"]["messageId"].__len__(), 26)
        self.assertNotEqual(
            document["observations"]["commandSubmit"]["correlationId"],
            document["observations"]["commandSubmit"]["messageId"],
        )

    def test_query_targets_the_created_tenant_handler_route(self) -> None:
        runtime = FakeRuntime()
        recorded: dict[str, Any] = {}

        def json_request(url: str, *, method: str = "GET", token: str | None = None, form: dict[str, str] | None = None, body: dict[str, Any] | None = None, timeout: int = 10, deadline: float | None = None) -> tuple[int, dict[str, Any], dict[str, str]]:
            if url.endswith("/api/v1/queries") and token == "synthetic-token-never-persisted":
                runtime.assert_token_present(token)
                recorded.update(body or {})
                return 200, {"payload": {"TenantId": (body or {}).get("entityId")}}, {"X-Hexalith-Query-Provenance": "HandlerComputed"}
            return FakeRuntime.json_request(runtime, url, method=method, token=token, form=form, body=body, timeout=timeout, deadline=deadline)

        runtime.json_request = json_request  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 0)
        self.assertEqual(recorded.get("queryType"), "get-tenant")
        self.assertEqual(recorded.get("projectionType"), "tenants")
        self.assertEqual(recorded.get("domain"), "tenants")
        self.assertEqual(recorded.get("entityId"), recorded.get("aggregateId"))
        self.assertNotIn("payload", recorded)
        self.assertTrue(str(recorded.get("aggregateId", "")).startswith("pact-reconciliation-"))

    def test_start_failure_is_recorded_and_still_attempts_clean_stop(self) -> None:
        runtime = FakeRuntime(start_code=2)

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 1)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(document["finalVerdict"], "failed")
        self.assertIn("apphost.start.failed", document["reasonCodes"])
        self.assertEqual(document["startup"]["startReturnCode"], 2)
        self.assertEqual(document["startup"]["startStderr"], "synthetic start failure")
        self.assertEqual(document["cleanup"]["result"], "failed")
        self.assertEqual(document["cleanup"]["listenerConfirmation"], "ports-unproven")
        self.assertIn("apphost.cleanup.incomplete", document["reasonCodes"])
        phases = [item[:2] for item in runtime.commands]
        expected_evaluations = 1 + len(FakeRuntime.source_projects())
        self.assertEqual(
            phases,
            [
                ["aspire", "stop"], ["aspire", "describe"], ["aspire", "ps"],
                ["dotnet", "clean"], ["dotnet", "restore"],
                *[["dotnet", "msbuild"]] * expected_evaluations,
                ["dotnet", "build"],
                *[["dotnet", "msbuild"]] * expected_evaluations,
                ["aspire", "start"],
                ["aspire", "stop"], ["aspire", "describe"], ["aspire", "ps"],
            ],
        )

    def test_live_runtime_prebuilds_debug_apphost_then_starts_without_rebuild(self) -> None:
        recorded: list[list[str]] = []
        runtime = smoke.SmokeRuntime()

        def command(arguments: list[str], timeout: int) -> smoke.CommandResult:
            del timeout
            recorded.append(arguments)
            if arguments[:2] == ["dotnet", "msbuild"]:
                return FakeRuntime.evaluation_result(arguments)
            if arguments[:1] == ["dotnet"]:
                return smoke.CommandResult(0, "built")
            if arguments[:2] == ["aspire", "start"]:
                return smoke.CommandResult(0)
            if arguments[:2] == ["aspire", "stop"]:
                return smoke.CommandResult(0)
            if arguments[:2] == ["aspire", "describe"]:
                return smoke.CommandResult(1)
            if arguments[:2] == ["aspire", "ps"]:
                return smoke.CommandResult(0, "[]")
            return smoke.CommandResult(1)

        runtime.command = command  # type: ignore[method-assign]
        runtime.source_graph_is_exact = lambda _: True  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)
        self.assertEqual(result, 1)
        clean = next(command_args for command_args in recorded if command_args[:2] == ["dotnet", "clean"])
        self.assertEqual(clean[2], smoke.APPHOST_RELATIVE)

        restore = next(command_args for command_args in recorded if command_args[:2] == ["dotnet", "restore"])
        self.assertEqual(restore[2], smoke.APPHOST_RELATIVE)
        self.assertIn("--force", restore)
        self.assertIn("--force-evaluate", restore)
        self.assertIn("--no-cache", restore)
        self.assertNotIn(
            f"-p:TargetFramework={smoke.APPHOST_EVALUATION_TARGET_FRAMEWORK}",
            restore,
        )
        build = next(command_args for command_args in recorded if command_args[:2] == ["dotnet", "build"])
        self.assertEqual(build[2], smoke.APPHOST_RELATIVE)
        self.assertIn("-m:1", build)
        self.assertIn("Debug", build)
        self.assertIn("--no-incremental", build)
        self.assertIn("--no-restore", build)
        self.assertNotIn(
            f"-p:TargetFramework={smoke.APPHOST_EVALUATION_TARGET_FRAMEWORK}",
            build,
        )
        for property_argument in smoke._build_property_arguments():
            self.assertIn(property_argument, clean)
            self.assertIn(property_argument, build)
        evaluation = next(
            command_args
            for command_args in recorded
            if command_args[:2] == ["dotnet", "msbuild"]
        )
        self.assertIn(
            f"-p:TargetFramework={smoke.APPHOST_EVALUATION_TARGET_FRAMEWORK}",
            evaluation,
        )
        self.assertLess(recorded.index(evaluation), next(
            index
            for index, command_args in enumerate(recorded)
            if command_args[:2] == ["aspire", "start"]
        ))
        start = next(command_args for command_args in recorded if command_args[:2] == ["aspire", "start"])
        self.assertIn("--isolated", start)
        self.assertIn("--no-build", start)

    def test_msbuild_evaluation_has_a_separate_bounded_json_output_budget(self) -> None:
        payload = "prefix" + ("x" * (smoke.MAX_MSBUILD_OUTPUT_CHARS + 17))
        completed = mock.Mock(returncode=0, stdout=payload, stderr=payload)
        runtime = smoke.SmokeRuntime()

        with mock.patch.object(smoke.subprocess, "run", return_value=completed):
            msbuild = runtime.command(["dotnet", "msbuild"], 30)
            describe = runtime.command(["aspire", "describe"], 30)

        self.assertEqual(len(msbuild.stdout), smoke.MAX_MSBUILD_OUTPUT_CHARS)
        self.assertEqual(len(msbuild.stderr), smoke.MAX_MSBUILD_OUTPUT_CHARS)
        self.assertEqual(len(describe.stdout), smoke.MAX_OUTPUT_CHARS)
        self.assertEqual(len(describe.stderr), smoke.MAX_OUTPUT_CHARS)

    def test_dirty_runtime_preflight_performs_no_lifecycle_or_build_mutation(self) -> None:
        smoke.runtime_evidence.runtime_input_manifest.return_value = (  # type: ignore[attr-defined]
            {"capturedRevision": "1" * 40, "treeSha256": "2" * 64},
            ["synthetic dirty scope"],
        )
        runtime = FakeRuntime()

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 1)
        self.assertEqual(runtime.commands, [])
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertIn("runtime-inputs.not-clean-or-complete", document["reasonCodes"])
        self.assertFalse(document["startup"]["hostStartAttempted"])

    def test_supplied_runtime_manifest_must_predate_apphost_capture(self) -> None:
        manifest_path = Path(self.temporary.name) / "runtime-input-manifest.json"
        manifest_path.write_text("{}\n", encoding="utf-8")
        captured_at = datetime.now(timezone.utc) + timedelta(minutes=1)
        manifest = {
            "capturedAt": captured_at.isoformat(),
            "capturedRevision": "1" * 40,
            "treeSha256": "2" * 64,
        }
        runtime = FakeRuntime()

        with mock.patch.object(
            smoke.runtime_evidence,
            "_validate_runtime_input_manifest",
            return_value=(manifest, captured_at),
        ):
            result = smoke.capture(
                self.output,
                runtime,
                timeout=30,
                runtime_input_manifest_path=manifest_path,
            )

        self.assertEqual(result, 1)
        self.assertEqual(runtime.commands, [])
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertIn("runtime-inputs.not-clean-or-complete", document["reasonCodes"])
        self.assertFalse(document["startup"]["hostStartAttempted"])

    def test_supplied_runtime_manifest_bytes_are_rechecked_after_capture(self) -> None:
        manifest_path = Path(self.temporary.name) / "runtime-input-manifest.json"
        original_bytes = b'{"marker":"original"}\n'
        mutated_bytes = b'{"marker":"mutated"}\n'
        manifest_path.write_bytes(original_bytes)
        captured_at = datetime.now(timezone.utc) - timedelta(minutes=1)
        manifest = {
            "capturedAt": captured_at.isoformat(),
            "capturedRevision": smoke._git(ROOT, "rev-parse", "HEAD"),
            "treeSha256": "0" * 64,
        }
        runtime = FakeRuntime()
        original_command = runtime.command

        def command(arguments: list[str], timeout: float) -> smoke.CommandResult:
            result = original_command(arguments, timeout)
            if arguments[:2] == ["aspire", "start"] and result.returncode == 0:
                manifest_path.write_bytes(mutated_bytes)
            return result

        runtime.command = command  # type: ignore[method-assign]
        with mock.patch.object(
            smoke.runtime_evidence,
            "_validate_runtime_input_manifest",
            return_value=(manifest, captured_at),
        ):
            result = smoke.capture(
                self.output,
                runtime,
                timeout=30,
                runtime_input_manifest_path=manifest_path,
            )

        self.assertEqual(result, 1)
        self.assertEqual(manifest_path.read_bytes(), mutated_bytes)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertFalse(document["cleanup"]["runtimeInputsCleanAfterRun"])
        self.assertIn("apphost.cleanup.incomplete", document["reasonCodes"])

    def test_evidence_writer_does_not_follow_predictable_temporary_symlink(self) -> None:
        symlink_target = Path(self.temporary.name) / "writer-symlink-target.json"
        symlink_target.write_text("must remain unchanged\n", encoding="utf-8")
        predictable_temporary = self.output.with_name(
            f".{self.output.name}.{os.getpid()}.tmp"
        )
        predictable_temporary.symlink_to(symlink_target)

        smoke._atomic_write(self.output, {"result": "written"})

        self.assertEqual(
            symlink_target.read_text(encoding="utf-8"),
            "must remain unchanged\n",
        )
        self.assertTrue(predictable_temporary.is_symlink())
        self.assertEqual(
            json.loads(self.output.read_text(encoding="utf-8")),
            {"result": "written"},
        )

    def test_source_graph_must_evaluate_exact_properties_and_input_authorities(self) -> None:
        runtime = FakeRuntime()
        original_command = runtime.command

        def command(arguments: list[str], timeout: float) -> smoke.CommandResult:
            if arguments[:2] == ["dotnet", "msbuild"]:
                runtime.commands.append(arguments)
                document = json.loads(FakeRuntime.evaluation_output())
                document["Properties"]["UseNuGetDeps"] = "true"
                return FakeRuntime.evaluation_result(arguments, json.dumps(document))
            return original_command(arguments, timeout)

        runtime.command = command  # type: ignore[method-assign]

        evaluation_issues: list[str] = []
        self.assertIsNone(
            smoke._evaluate_source_graph(
                runtime,
                time.monotonic() + 30,
                issues=evaluation_issues,
            )
        )
        self.assertEqual(
            evaluation_issues,
            ["apphost-build-property-mismatch:UseNuGetDeps"],
        )

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 1)
        self.assertFalse(any(args[:2] == ["aspire", "start"] for args in runtime.commands))
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertIn("apphost.source-graph.not-exact", document["reasonCodes"])

        external = Path(self.temporary.name) / "external-build-input.bin"
        external.write_bytes(b"outside sealed authorities")
        for item_name in (
            "MSBuildAllProjects",
            "ProjectReference",
            "Reference",
            "Analyzer",
            "AdditionalFiles",
            "Content",
            "None",
            "NativeCopyLocalItems",
            "RuntimeCopyLocalItems",
        ):
            with self.subTest(item_name=item_name):
                candidate_runtime = FakeRuntime()
                candidate_command = candidate_runtime.command

                def command_with_external_input(
                    arguments: list[str],
                    timeout: float,
                    *,
                    item_name: str = item_name,
                ) -> smoke.CommandResult:
                    if arguments[:2] == ["dotnet", "msbuild"]:
                        candidate_runtime.commands.append(arguments)
                        candidate = json.loads(FakeRuntime.evaluation_output())
                        if item_name == "MSBuildAllProjects":
                            candidate["Properties"][item_name] += f";{external}"
                        else:
                            candidate["Items"][item_name] = [
                                {
                                    "Identity": "external-input",
                                    "HintPath": str(external),
                                }
                            ]
                        return FakeRuntime.evaluation_result(arguments, json.dumps(candidate))
                    return candidate_command(arguments, timeout)

                candidate_runtime.command = command_with_external_input  # type: ignore[method-assign]

                result = smoke.capture(self.output, candidate_runtime, timeout=30)

                self.assertEqual(result, 1)
                self.assertFalse(
                    any(
                        arguments[:2] == ["aspire", "start"]
                        for arguments in candidate_runtime.commands
                    )
                )
                failure = json.loads(self.output.read_text(encoding="utf-8"))
                self.assertIn("apphost.source-graph.not-exact", failure["reasonCodes"])

    def test_postbuild_evaluated_input_binding_is_authoritative(self) -> None:
        before = {
            "assetsGraphs": ["src/AppHost/obj/project.assets.json"],
            "inputs": [
                {
                    "authority": "repository",
                    "path": "src/AppHost/AppHost.csproj",
                    "sha256": "a" * 64,
                }
            ],
        }
        after = {
            "assetsGraphs": ["src/AppHost/obj/project.assets.json"],
            "inputs": [
                *before["inputs"],
                {
                    "authority": "repository",
                    "path": "src/AppHost/obj/Debug/net10.0/AppHost.AssemblyInfo.cs",
                    "sha256": "b" * 64,
                },
            ],
        }

        with mock.patch.object(
            smoke,
            "_evaluate_source_graph",
            side_effect=[before, after],
        ):
            result = smoke.capture(self.output, FakeRuntime(), timeout=30)

        self.assertEqual(result, 0)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(
            document["startup"]["outputPreparation"]["evaluatedInputBinding"],
            after,
        )

    def test_assets_discovery_includes_fresh_conditionally_restored_projects(self) -> None:
        repository = Path(self.temporary.name) / "repository"
        package_root = Path(self.temporary.name) / "fresh-packages"
        package_root.mkdir()
        root_project = repository / "src/AppHost/AppHost.csproj"
        conditional_project = repository / "src/Conditional/Conditional.csproj"
        for project in (root_project, conditional_project):
            project.parent.mkdir(parents=True)
            project.write_text("<Project />\n", encoding="utf-8")
            assets = {
                "packageFolders": {str(package_root) + os.sep: {}},
                "libraries": {},
                "project": {"restore": {"projectPath": str(project)}},
            }
            assets_path = project.parent / "obj/project.assets.json"
            assets_path.parent.mkdir()
            assets_path.write_text(json.dumps(assets), encoding="utf-8")
        (repository / "samples/Counter").mkdir(parents=True)

        with (
            mock.patch.object(smoke, "ROOT", repository),
            mock.patch.object(smoke, "REACHABLE_SOURCE_GITLINKS", ()),
        ):
            discovered = REAL_DISCOVER_ASSETS_GRAPHS(root_project)

        self.assertIsNotNone(discovered)
        projects, assets_paths = discovered or ([], [])
        self.assertEqual(projects, sorted([root_project, conditional_project]))
        self.assertEqual(
            assets_paths,
            sorted(
                [
                    root_project.parent / "obj/project.assets.json",
                    conditional_project.parent / "obj/project.assets.json",
                ]
            ),
        )

    def test_process_diagnostic_redacts_secret_shaped_output(self) -> None:
        diagnostic = smoke._safe_process_diagnostic(
            smoke.CommandResult(
                1,
                "ordinary failure beneath /home/runner/work/project",
                "password=must-not-escape",
            )
        )

        self.assertIn("returnCode=1", diagnostic)
        self.assertIn("detail=redacted", diagnostic)
        self.assertNotIn("must-not-escape", diagnostic)

    def test_target_resolved_external_reference_path_fails_before_start(self) -> None:
        runtime = FakeRuntime()
        original_command = runtime.command
        external = Path(self.temporary.name) / "target-produced-reference.dll"
        external.write_bytes(b"outside sealed authorities")
        resolve_target_seen = False

        def command(arguments: list[str], timeout: float) -> smoke.CommandResult:
            nonlocal resolve_target_seen
            if arguments[:2] == ["dotnet", "msbuild"]:
                runtime.commands.append(arguments)
                document = json.loads(FakeRuntime.evaluation_output())
                if "-target:ResolveReferences" in arguments:
                    resolve_target_seen = True
                    document["Items"]["ReferencePath"] = [
                        {"Identity": str(external), "FullPath": str(external)}
                    ]
                return FakeRuntime.evaluation_result(arguments, json.dumps(document))
            return original_command(arguments, timeout)

        runtime.command = command  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 1)
        self.assertTrue(resolve_target_seen)
        self.assertFalse(any(
            args[:2] == ["aspire", "start"] for args in runtime.commands
        ))
        failure = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertIn("apphost.source-graph.not-exact", failure["reasonCodes"])

    def test_empty_or_rootless_assets_graph_set_fails_before_ledger_or_start(self) -> None:
        rootless = smoke.ROOT / smoke.runtime_evidence.PROVIDER_PACKAGE_ASSETS[0]
        for assets_paths in ([], [rootless]):
            with self.subTest(assets_paths=assets_paths):
                runtime = FakeRuntime()
                with mock.patch.object(
                    smoke,
                    "_discover_assets_graphs_from_json",
                    return_value=([smoke.APPHOST], assets_paths),
                ):
                    result = smoke.capture(self.output, runtime, timeout=30)

                self.assertEqual(result, 1)
                self.assertFalse(any(
                    args[:2] == ["dotnet", "msbuild"]
                    or args[:2] == ["aspire", "start"]
                    for args in runtime.commands
                ))
                failure = json.loads(self.output.read_text(encoding="utf-8"))
                self.assertIn("apphost.assets-graph.not-closed", failure["reasonCodes"])

    def test_semantically_self_consistent_empty_package_ledger_fails_before_execution(self) -> None:
        runtime = FakeRuntime()
        empty_entries: dict[str, Any] = {
            "assetsGraphs": [],
            "toolPackages": [],
            "packages": [],
        }
        empty_ledger = {
            "schema": smoke.runtime_evidence.PACKAGE_LEDGER_SCHEMA,
            "capturedAt": datetime.now(timezone.utc).isoformat(),
            "packageRoot": "fresh-external",
            **empty_entries,
            "treeSha256": hashlib.sha256(
                json.dumps(
                    empty_entries, sort_keys=True, separators=(",", ":")
                ).encode("utf-8")
            ).hexdigest(),
        }
        with mock.patch.object(
            smoke.runtime_evidence,
            "resolved_package_ledger",
            return_value=(empty_ledger, []),
        ):
            result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 1)
        self.assertFalse(any(
            args[:2] == ["dotnet", "msbuild"]
            or args[:2] == ["aspire", "start"]
            for args in runtime.commands
        ))
        failure = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertIn("apphost.package-authority.not-sealed", failure["reasonCodes"])

    def test_runtime_output_requires_apphost_deps_and_rejects_package_substitution(self) -> None:
        repository = Path(self.temporary.name)
        apphost = repository / "src/AppHost/AppHost.csproj"
        apphost.parent.mkdir(parents=True, exist_ok=True)
        apphost.write_text("<Project />\n", encoding="utf-8")
        output = apphost.parent / "bin/Debug/net10.0"
        output.mkdir(parents=True)
        (output / "AppHost.dll").write_bytes(b"apphost")

        def inventory() -> tuple[list[dict[str, Any]], bool]:
            issues: list[str] = []
            files = smoke.runtime_evidence._apphost_runtime_output_binding(
                repository, issues
            )
            return files, not issues

        with (
            mock.patch.object(
                smoke.runtime_evidence,
                "APPHOST_PROJECT_PATH",
                "src/AppHost/AppHost.csproj",
            ),
            mock.patch.object(
                smoke.runtime_evidence,
                "_discover_apphost_project_graph",
                return_value=([apphost.resolve()], []),
            ),
        ):
            _, valid = inventory()
            self.assertFalse(valid)

            deps_path = output / "AppHost.deps.json"
            deps_path.write_text('{"libraries":{}}\n', encoding="utf-8")
            _, valid = inventory()
            self.assertTrue(valid)

            deps_path.write_text(
                json.dumps(
                    {
                        "libraries": {
                            "Hexalith.EventStore/3.103.0": {"type": "package"}
                        }
                    }
                ),
                encoding="utf-8",
            )
            _, valid = inventory()
            self.assertFalse(valid)

    def test_evaluated_dependency_package_reference_fails_before_start(self) -> None:
        for identity in ("hexalith.eventstore", "HeXaLiTh.TeNaNtS.Client"):
            with self.subTest(identity=identity):
                runtime = FakeRuntime()
                original_command = runtime.command

                def command(arguments: list[str], timeout: float) -> smoke.CommandResult:
                    if arguments[:2] == ["dotnet", "msbuild"]:
                        runtime.commands.append(arguments)
                        document = json.loads(FakeRuntime.evaluation_output())
                        document["Items"]["PackageReference"] = [
                            {"Identity": identity}
                        ]
                        return FakeRuntime.evaluation_result(arguments, json.dumps(document))
                    return original_command(arguments, timeout)

                runtime.command = command  # type: ignore[method-assign]

                result = smoke.capture(self.output, runtime, timeout=30)

                self.assertEqual(result, 1)
                self.assertFalse(any(
                    args[:2] == ["aspire", "start"] for args in runtime.commands
                ))
                document = json.loads(self.output.read_text(encoding="utf-8"))
                self.assertIn("apphost.source-graph.not-exact", document["reasonCodes"])

    def test_resolved_assets_reject_dependency_packages_and_shadow_projects(self) -> None:
        dependency_names = (
            "Hexalith.EventStore",
            "Hexalith.Tenants",
            "Hexalith.Parties",
            "Hexalith.Memories",
            "Hexalith.Commons",
        )
        temporary_root = Path(self.temporary.name) / "repository"
        projects: list[Path] = []
        for name in dependency_names:
            project = temporary_root / "references" / name / "src" / f"{name}.csproj"
            project.parent.mkdir(parents=True)
            project.write_text("<Project />", encoding="utf-8")
            assets = project.parent / "obj" / "project.assets.json"
            assets.parent.mkdir()
            assets.write_text(json.dumps({"libraries": {}}), encoding="utf-8")
            projects.append(project)

        with mock.patch.object(smoke, "ROOT", temporary_root):
            self.assertTrue(smoke._resolved_source_graph_is_exact(projects))
            assets = projects[1].parent / "obj" / "project.assets.json"
            for identity in (
                "hexalith.eventstore/3.103.0",
                "HeXaLiTh.TeNaNtS.Client/3.103.0",
            ):
                with self.subTest(identity=identity):
                    assets.write_text(
                        json.dumps({"libraries": {identity: {"type": "package"}}}),
                        encoding="utf-8",
                    )
                    self.assertFalse(smoke._resolved_source_graph_is_exact(projects))
            assets.write_text(json.dumps({"libraries": {}}), encoding="utf-8")
            shadow = temporary_root / "shadow" / "Hexalith.EventStore.csproj"
            shadow.parent.mkdir()
            shadow.write_text("<Project />", encoding="utf-8")
            shadow_assets = shadow.parent / "obj" / "project.assets.json"
            shadow_assets.parent.mkdir()
            shadow_assets.write_text(json.dumps({"libraries": {}}), encoding="utf-8")
            self.assertFalse(smoke._resolved_source_graph_is_exact([shadow, *projects[1:]]))
            self.assertFalse(smoke._resolved_source_graph_is_exact([shadow, *projects]))

            polymorphic = (
                temporary_root
                / "references"
                / "Hexalith.PolymorphicSerializations"
                / "src"
                / "Hexalith.PolymorphicSerializations.csproj"
            )
            polymorphic.parent.mkdir(parents=True)
            polymorphic.write_text("<Project />", encoding="utf-8")
            polymorphic_assets = polymorphic.parent / "obj" / "project.assets.json"
            polymorphic_assets.parent.mkdir()
            polymorphic_assets.write_text(json.dumps({"libraries": {}}), encoding="utf-8")
            self.assertFalse(smoke._resolved_source_graph_is_exact([*projects, polymorphic]))

            assets.write_text(
                json.dumps({
                    "libraries": {
                        "Hexalith.PolymorphicSerializations/1.19.2": {"type": "package"}
                    }
                }),
                encoding="utf-8",
            )
            self.assertFalse(smoke._resolved_source_graph_is_exact(projects))

            assets_target = temporary_root / "symlinked-project.assets.json"
            assets_target.write_text(json.dumps({"libraries": {}}), encoding="utf-8")
            assets.unlink()
            assets.symlink_to(assets_target)
            self.assertFalse(smoke._resolved_source_graph_is_exact(projects))

    def test_owned_dapr_name_resolution_files_are_removed_after_confirmed_shutdown(self) -> None:
        runtime = FakeRuntime()
        original_command = runtime.command

        def command(arguments: list[str], timeout: float) -> smoke.CommandResult:
            result = original_command(arguments, timeout)
            if arguments[:2] == ["aspire", "start"] and result.returncode == 0:
                for path in self.dapr_paths.values():
                    path.write_bytes(b"owned-by-this-invocation")
            return result

        runtime.command = command  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 0)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        cleanup = document["cleanup"]["daprNameResolutionFiles"]
        expected = list(smoke.DAPR_NAME_RESOLUTION_RELATIVES)
        self.assertEqual(cleanup["absentBeforeRun"], expected)
        self.assertEqual(cleanup["createdByInvocation"], expected)
        self.assertEqual(cleanup["removedAfterShutdown"], expected)
        self.assertEqual(cleanup["remainingAfterCleanup"], [])
        self.assertTrue(document["cleanup"]["runtimeInputsCleanAfterRun"])
        self.assertTrue(all(not path.exists() for path in self.dapr_paths.values()))

    def test_preexisting_dapr_name_resolution_file_is_preserved_byte_for_byte(self) -> None:
        runtime = FakeRuntime()
        original_command = runtime.command
        preexisting_relative = smoke.DAPR_NAME_RESOLUTION_RELATIVES[0]
        preexisting_path = self.dapr_paths[preexisting_relative]
        preexisting_bytes = b"developer-owned-name-resolution-state\x00\xff"
        preexisting_path.write_bytes(preexisting_bytes)

        def command(arguments: list[str], timeout: float) -> smoke.CommandResult:
            result = original_command(arguments, timeout)
            if arguments[:2] == ["aspire", "start"] and result.returncode == 0:
                for relative, path in self.dapr_paths.items():
                    if relative != preexisting_relative:
                        path.write_bytes(b"owned-by-this-invocation")
            return result

        runtime.command = command  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 0)
        cleanup = json.loads(self.output.read_text(encoding="utf-8"))["cleanup"]["daprNameResolutionFiles"]
        self.assertNotIn(preexisting_relative, cleanup["absentBeforeRun"])
        self.assertNotIn(preexisting_relative, cleanup["createdByInvocation"])
        self.assertNotIn(preexisting_relative, cleanup["removedAfterShutdown"])
        self.assertEqual(preexisting_path.read_bytes(), preexisting_bytes)

    def test_cleanup_failure_overrides_an_otherwise_passing_capture(self) -> None:
        runtime = FakeRuntime(cleanup_running=True)
        clock = [0.0]

        with (
            mock.patch.object(smoke.time, "monotonic", side_effect=lambda: clock[0]),
            mock.patch.object(
                smoke.time,
                "sleep",
                side_effect=lambda seconds: clock.__setitem__(0, clock[0] + seconds),
            ),
        ):
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
            "wss://127.0.0.1:7273/hubs/projection-changes?id=connection%2Fid&access_token=token+value",
        )

    def test_aspire_machine_output_requires_one_authoritative_json_document(self) -> None:
        self.assertEqual(smoke._json_from_output('  [{"name":"one"}]\n'), [{"name": "one"}])
        self.assertIsNone(smoke._json_from_output('[]\n[{"name":"hidden"}]'))
        self.assertIsNone(smoke._json_from_output('noise before [{"name":"hidden"}]'))

    def test_msbuild_evaluation_reads_large_bounded_result_file_instead_of_truncated_stdout(self) -> None:
        document = json.loads(FakeRuntime.evaluation_output())
        document["Items"]["ReferencePath"] = [
            {"Identity": "x" * (smoke.MAX_OUTPUT_CHARS + 1024)}
        ]
        payload = json.dumps(document)
        self.assertGreater(len(payload), smoke.MAX_OUTPUT_CHARS)
        truncated_stdout = payload[-smoke.MAX_OUTPUT_CHARS:]
        self.assertIsNone(smoke._json_from_output(truncated_stdout))
        runtime = mock.Mock()

        def command(arguments: list[str], timeout: float) -> smoke.CommandResult:
            del timeout
            option = next(
                argument
                for argument in arguments
                if argument.startswith("-getResultOutputFile:")
            )
            Path(option.split(":", 1)[1]).write_text(payload, encoding="utf-8")
            return smoke.CommandResult(0, truncated_stdout)

        runtime.command.side_effect = command

        result, parsed = smoke._msbuild_evaluation(
            runtime,
            ["dotnet", "msbuild", "AppHost.csproj"],
            30,
        )

        self.assertEqual(result.returncode, 0)
        self.assertEqual(parsed, document)

    def test_msbuild_evaluation_rejects_missing_malformed_symlinked_and_oversized_results(self) -> None:
        symlink_target = Path(self.temporary.name) / "msbuild-result-target.json"
        symlink_target.write_text(FakeRuntime.evaluation_output(), encoding="utf-8")

        def missing(path: Path) -> None:
            del path

        def malformed(path: Path) -> None:
            path.write_text("{", encoding="utf-8")

        def symlinked(path: Path) -> None:
            path.symlink_to(symlink_target)

        def oversized(path: Path) -> None:
            path.write_text('{"value":"' + ("x" * 128) + '"}', encoding="utf-8")

        for name, writer in (
            ("missing", missing),
            ("malformed", malformed),
            ("symlinked", symlinked),
            ("oversized", oversized),
        ):
            with self.subTest(name=name):
                runtime = mock.Mock()

                def command(
                    arguments: list[str],
                    timeout: float,
                    *,
                    writer: Any = writer,
                ) -> smoke.CommandResult:
                    del timeout
                    option = next(
                        argument
                        for argument in arguments
                        if argument.startswith("-getResultOutputFile:")
                    )
                    writer(Path(option.split(":", 1)[1]))
                    return smoke.CommandResult(0)

                runtime.command.side_effect = command
                with mock.patch.object(smoke, "MAX_MSBUILD_RESULT_BYTES", 64):
                    _, parsed = smoke._msbuild_evaluation(
                        runtime,
                        ["dotnet", "msbuild", "AppHost.csproj"],
                        30,
                    )
                self.assertIsNone(parsed)

    def test_http_body_limit_accepts_exactly_the_limit_and_rejects_one_more_byte(self) -> None:
        exact = io.BytesIO(b"x" * smoke.MAX_HTTP_BODY_BYTES)
        self.assertEqual(
            len(smoke._bounded_http_body(exact, time.monotonic() + 5)),
            smoke.MAX_HTTP_BODY_BYTES,
        )
        oversized = io.BytesIO(b"x" * (smoke.MAX_HTTP_BODY_BYTES + 1))
        with self.assertRaisesRegex(ValueError, "response-too-large"):
            smoke._bounded_http_body(oversized, time.monotonic() + 5)

    def test_websocket_header_limit_and_deadline_are_absolute(self) -> None:
        class HeaderStream:
            def __init__(self, data: bytes, *, clock: list[float] | None = None) -> None:
                self.data = bytearray(data)
                self.clock = clock

            def settimeout(self, timeout: float) -> None:
                self.timeout = timeout

            def recv(self, count: int) -> bytes:
                if self.clock is not None:
                    self.clock[0] += 0.6
                value = bytes(self.data[:count])
                del self.data[:count]
                return value

        prefix = b"HTTP/1.1 101 Switching Protocols\r\nX-Pad: "
        suffix = b"\r\n\r\n"
        exact_payload = prefix + b"x" * (
            smoke.MAX_WEBSOCKET_HEADER_BYTES - len(prefix) - len(suffix)
        ) + suffix
        headers, initial = smoke._read_http_headers(
            HeaderStream(exact_payload),
            time.monotonic() + 5,
        )
        self.assertEqual(len(headers), smoke.MAX_WEBSOCKET_HEADER_BYTES)
        self.assertEqual(initial, b"")
        with self.assertRaisesRegex(ValueError, "websocket-headers-too-large"):
            smoke._read_http_headers(
                HeaderStream(b"x" * (smoke.MAX_WEBSOCKET_HEADER_BYTES + 1)),
                time.monotonic() + 5,
            )

        clock = [0.0]
        with mock.patch.object(smoke.time, "monotonic", side_effect=lambda: clock[0]):
            with self.assertRaisesRegex(TimeoutError, "websocket-header-deadline-exceeded"):
                smoke._read_http_headers(
                    HeaderStream(b"x" * 100, clock=clock),
                    1.0,
                )
        self.assertLessEqual(clock[0], 1.2)

    def test_websocket_frame_limit_accepts_limit_and_rejects_limit_plus_one(self) -> None:
        payload = b"x" * (smoke.MAX_WEBSOCKET_BYTES - 10)
        frame = b"\x81\x7f" + len(payload).to_bytes(8, "big") + payload

        parsed = smoke._read_websocket_frame(
            mock.Mock(),
            bytearray(frame),
            time.monotonic() + 5,
            [len(frame)],
        )
        self.assertEqual(parsed, (True, 1, payload))
        with self.assertRaisesRegex(ValueError, "websocket-response-too-large"):
            smoke._read_websocket_frame(
                mock.Mock(),
                bytearray(frame + b"x"),
                time.monotonic() + 5,
                [len(frame) + 1],
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
        running = [False]

        def command(arguments: list[str], timeout: int) -> smoke.CommandResult:
            del timeout
            runtime.commands.append(arguments)
            operation = arguments[1]
            if arguments[0] == "dotnet":
                if operation == "msbuild":
                    return FakeRuntime.evaluation_result(arguments)
                return smoke.CommandResult(
                    0,
                    "",
                )
            if operation == "start":
                running[0] = True
                return smoke.CommandResult(0)
            if operation == "wait":
                return smoke.CommandResult(0)
            if operation == "stop":
                running[0] = False
                return smoke.CommandResult(0)
            if operation == "describe":
                if running[0]:
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
                            resources.append(
                                {"name": name, "endpoints": [{"url": "http://127.0.0.1:19999"}]}
                            )
                    return smoke.CommandResult(0, json.dumps({"resources": resources}))
                return smoke.CommandResult(1)
            if operation == "ps":
                return smoke.CommandResult(0, '[{"name":"frontcomposer"}]' if running[0] else "[]")
            raise AssertionError(arguments)

        def json_request(
            url: str,
            *,
            method: str = "GET",
            token: str | None = None,
            form: dict[str, str] | None = None,
            body: dict[str, Any] | None = None,
            timeout: int = 10,
            deadline: float | None = None,
        ) -> tuple[int, dict[str, Any], dict[str, str]]:
            requested.append(url)
            if len(requested) == 1:
                return 0, {}, {}
            return FakeRuntime.json_request(
                runtime, url, method=method, token=token, form=form, body=body, timeout=timeout, deadline=deadline
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

    def test_non_loopback_candidates_are_filtered_before_sensitive_probes(self) -> None:
        records = [
            {
                "displayName": "eventstore",
                "urls": [
                    {"name": "https", "url": "https://public.example.test:443"},
                    {"name": "http", "url": "http://127.0.0.1:18080"},
                    {"name": "target", "url": "http://10.0.0.5:8080", "isInternal": True},
                ],
            }
        ]

        self.assertEqual(
            smoke._resource_public_urls(records, "eventstore"),
            ["http://127.0.0.1:18080", "http://localhost:18080"],
        )
        self.assertTrue(
            all(smoke._is_loopback_url(url) for url in smoke._resource_signalr_urls(records, "eventstore"))
        )
        runtime = smoke.SmokeRuntime()
        self.assertEqual(
            runtime.json_request(
                "https://public.example.test/token", form={"password": "secret"}
            ),
            (0, {}, {}),
        )
        with self.assertRaisesRegex(ValueError, "require-loopback"):
            runtime.signalr_connect("https://public.example.test/hub", "token")

    def test_poisoned_localhost_is_rejected_before_credentials_reach_http(self) -> None:
        poisoned = [
            (smoke.socket.AF_INET, smoke.socket.SOCK_STREAM, 6, "", ("203.0.113.7", 18080))
        ]
        with (
            mock.patch.object(smoke.socket, "getaddrinfo", return_value=poisoned),
            mock.patch.object(smoke.request, "build_opener") as build_opener,
        ):
            self.assertEqual(
                smoke.SmokeRuntime().json_request(
                    "http://localhost:18080/token",
                    form={"password": "never-sent"},
                ),
                (0, {}, {}),
            )
        build_opener.assert_not_called()

    def test_localhost_resolution_stops_at_the_caller_deadline_without_sending_credentials(self) -> None:
        release = threading.Event()

        def blocked_resolution(*args: Any, **kwargs: Any) -> list[Any]:
            del args, kwargs
            release.wait(1)
            return [
                (smoke.socket.AF_INET, smoke.socket.SOCK_STREAM, 6, "", ("127.0.0.1", 18080))
            ]

        started = time.monotonic()
        try:
            with (
                mock.patch.object(smoke.socket, "getaddrinfo", side_effect=blocked_resolution),
                mock.patch.object(smoke.request, "build_opener") as build_opener,
            ):
                result = smoke.SmokeRuntime().json_request(
                    "http://localhost:18080/token",
                    form={"password": "never-sent"},
                    timeout=0.02,
                    deadline=time.monotonic() + 0.02,
                )
            self.assertEqual(result, (0, {}, {}))
            self.assertLess(time.monotonic() - started, 0.25)
            build_opener.assert_not_called()
        finally:
            release.set()

    def test_numeric_loopback_transport_preserves_logical_authority(self) -> None:
        observed_host = ""

        class Handler(http.server.BaseHTTPRequestHandler):
            def do_POST(self) -> None:  # noqa: N802
                nonlocal observed_host
                observed_host = self.headers.get("Host", "")
                self.send_response(200)
                self.send_header("Content-Type", "application/json")
                self.end_headers()
                self.wfile.write(json.dumps({"issuer": f"http://{observed_host}/realms/hexalith"}).encode())

            def log_message(self, format: str, *args: Any) -> None:
                del format, args

        server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), Handler)
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        self.addCleanup(server.server_close)
        self.addCleanup(server.shutdown)

        status, document, _ = smoke.SmokeRuntime().json_request(
            f"http://localhost:{server.server_port}/realms/hexalith/protocol/openid-connect/token",
            method="POST",
            form={"password": "never-persisted"},
            timeout=2,
        )

        self.assertEqual(status, 200)
        self.assertEqual(observed_host, f"localhost:{server.server_port}")
        self.assertEqual(document["issuer"], f"http://localhost:{server.server_port}/realms/hexalith")

    def test_signalr_scalar_available_transports_fails_deterministically(self) -> None:
        runtime = smoke.SmokeRuntime()
        runtime.json_request = mock.Mock(return_value=(
            200,
            {"connectionToken": "connection", "availableTransports": "WebSockets"},
            {},
        ))  # type: ignore[method-assign]
        with mock.patch.object(smoke, "_websocket_signalr_handshake") as handshake:
            self.assertFalse(
                runtime.signalr_connect(
                    "http://127.0.0.1:18080/hubs/projection-changes",
                    "token",
                )
            )
        handshake.assert_not_called()

    def test_http_error_body_failures_return_deterministic_transport_failure(self) -> None:
        class RaisingBody:
            def __init__(self, exception: Exception) -> None:
                self.exception = exception

            def read(self, count: int) -> bytes:
                del count
                raise self.exception

            read1 = read

            def close(self) -> None:
                pass

        cases: list[tuple[str, Any]] = [
            ("timeout", RaisingBody(TimeoutError("slow error body"))),
            ("read-error", RaisingBody(OSError("broken error body"))),
            ("incomplete", RaisingBody(smoke.IncompleteRead(b"partial"))),
            ("oversized", io.BytesIO(b"x" * (smoke.MAX_HTTP_BODY_BYTES + 1))),
        ]
        for name, body in cases:
            with self.subTest(name=name):
                failure = smoke.error.HTTPError(
                    "http://127.0.0.1:18080/protected",
                    401,
                    "Unauthorized",
                    {},
                    body,
                )
                opener = mock.Mock()
                opener.open.side_effect = failure
                with mock.patch.object(smoke.request, "build_opener", return_value=opener):
                    self.assertEqual(
                        smoke.SmokeRuntime().json_request(
                            "http://127.0.0.1:18080/protected",
                            token="local-token",
                            timeout=1,
                        ),
                        (0, {}, {}),
                    )

    def test_failed_readiness_respects_deadline_and_never_submits_a_command(self) -> None:
        runtime = FakeRuntime()
        clock = [0.0]
        request_timeouts: list[float] = []
        requested_urls: list[str] = []

        original_request = runtime.json_request

        def json_request(
            url: str,
            *,
            method: str = "GET",
            token: str | None = None,
            form: dict[str, str] | None = None,
            body: dict[str, Any] | None = None,
            timeout: float = 10,
            deadline: float | None = None,
        ) -> tuple[int, dict[str, Any], dict[str, str]]:
            requested_urls.append(url)
            if url.endswith("/health"):
                request_timeouts.append(timeout)
                clock[0] += timeout
                return 0, {}, {}
            if url.endswith("/alive"):
                return 200, {}, {}
            return original_request(
                url, method=method, token=token, form=form, body=body, timeout=timeout, deadline=deadline
            )

        runtime.json_request = json_request  # type: ignore[method-assign]
        with (
            mock.patch.object(smoke.time, "monotonic", side_effect=lambda: clock[0]),
            mock.patch.object(smoke.time, "sleep", side_effect=lambda seconds: clock.__setitem__(0, clock[0] + seconds)),
        ):
            result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 1)
        self.assertTrue(request_timeouts)
        self.assertTrue(all(0 < value <= 5 for value in request_timeouts))
        self.assertLessEqual(clock[0], 30)
        self.assertTrue(all(0 < value <= 30 for value in runtime.command_timeouts))
        self.assertFalse(any(url.endswith("/api/v1/commands") for url in requested_urls))
        self.assertFalse(any(url.endswith("/alive") for url in requested_urls))
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertIn("health.readiness.failed", document["reasonCodes"])
        self.assertEqual(document["observations"]["commandSubmit"]["result"], "not-observed")

    def test_missing_command_authorization_control_prevents_authenticated_mutation(self) -> None:
        runtime = FakeRuntime()
        command_calls: list[str | None] = []
        original_request = runtime.json_request

        def json_request(
            url: str,
            *,
            method: str = "GET",
            token: str | None = None,
            form: dict[str, str] | None = None,
            body: dict[str, Any] | None = None,
            timeout: float = 10,
            deadline: float | None = None,
        ) -> tuple[int, dict[str, Any], dict[str, str]]:
            if url.endswith("/api/v1/commands"):
                command_calls.append(token)
                return 202, {"correlationId": "forged-invalid-bearer-success"}, {}
            return original_request(
                url, method=method, token=token, form=form, body=body, timeout=timeout, deadline=deadline
            )

        runtime.json_request = json_request  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 1)
        self.assertEqual(command_calls, ["invalid-local-evidence-token"])
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertIn("authorization.command-submit.not-enforced", document["reasonCodes"])
        self.assertEqual(document["observations"]["commandSubmit"]["result"], "not-observed")

    def test_exact_primary_topology_rejects_duplicate_and_extra_resources(self) -> None:
        for names in (
            [*smoke.REQUIRED_RESOURCES, "eventstore"],
            [*smoke.REQUIRED_RESOURCES, "unexpected-service"],
        ):
            with self.subTest(names=names[-1]):
                result = smoke.capture(self.output, FakeRuntime(resource_names=names), timeout=30)
                self.assertEqual(result, 1)
                document = json.loads(self.output.read_text(encoding="utf-8"))
                self.assertIn("apphost.describe.incomplete", document["reasonCodes"])

    def test_generated_describe_support_resources_do_not_hide_unknown_primary_extras(self) -> None:
        records = [
            {
                "name": name,
                "endpoints": [{"url": f"http://127.0.0.1:{18000 + index}"}],
            }
            for index, name in enumerate(smoke.REQUIRED_RESOURCES)
        ]
        records.extend(
            [
                {
                    "name": "eventstore-dapr-cli",
                    "resourceType": "Executable",
                    "source": "dapr",
                    "relationships": [
                        {"type": "Parent", "resourceName": "eventstore"}
                    ],
                },
                {
                    "name": "eventstore-admin-ui-local-auth-password",
                    "resourceType": "Parameter",
                    "source": "Parameters:eventstore-admin-ui-local-auth-password",
                },
            ]
        )
        next(item for item in records if item["name"] == "eventstore-admin-ui")["relationships"] = [
            {"type": "Reference", "resourceName": "eventstore-admin-ui-local-auth-password"}
        ]
        result = smoke.capture(self.output, FakeRuntime(resource_records=records), timeout=30)
        self.assertEqual(result, 0)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(set(document["topology"]["declaredResources"]), set(smoke.REQUIRED_RESOURCES))

    def test_generated_looking_name_without_type_and_parent_is_a_primary_extra(self) -> None:
        names = [*smoke.REQUIRED_RESOURCES, "eventstore-dapr-cli"]

        result = smoke.capture(self.output, FakeRuntime(resource_names=names), timeout=30)

        self.assertEqual(result, 1)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertIn("apphost.describe.incomplete", document["reasonCodes"])

    def test_status_query_and_signalr_authorization_controls_fail_closed(self) -> None:
        for surface in ("commandStatus", "queryProvenance", "projectionSignalR"):
            with self.subTest(surface=surface):
                runtime = FakeRuntime()
                original_request = runtime.json_request
                original_signalr = runtime.signalr_invalid_upgrade_status

                def json_request(
                    url: str,
                    *,
                    method: str = "GET",
                    token: str | None = None,
                    form: dict[str, str] | None = None,
                    body: dict[str, Any] | None = None,
                    timeout: float = 10,
                    deadline: float | None = None,
                ) -> tuple[int, dict[str, Any], dict[str, str]]:
                    if surface == "commandStatus" and "/commands/status/" in url and token == "invalid-local-evidence-token":
                        return 200, {"status": "Completed"}, {}
                    if (
                        surface == "queryProvenance"
                        and url.endswith("/api/v1/queries")
                        and token == "invalid-local-evidence-token"
                    ):
                        return 200, {}, {}
                    return original_request(
                        url, method=method, token=token, form=form, body=body,
                        timeout=timeout, deadline=deadline,
                    )

                def signalr_status(
                    hub_url: str,
                    negotiation_token: str,
                    invalid_token: str,
                    timeout: float = 10,
                    deadline: float | None = None,
                ) -> int:
                    if surface == "projectionSignalR":
                        return 200
                    return original_signalr(
                        hub_url,
                        negotiation_token,
                        invalid_token,
                        timeout=timeout,
                        deadline=deadline,
                    )

                runtime.json_request = json_request  # type: ignore[method-assign]
                runtime.signalr_invalid_upgrade_status = signalr_status  # type: ignore[method-assign]
                result = smoke.capture(self.output, runtime, timeout=30)
                self.assertEqual(result, 1)
                document = json.loads(self.output.read_text(encoding="utf-8"))
                self.assertEqual(document["authorizationControls"][surface]["result"], "failed")
                expected_reason = {
                    "commandStatus": "authorization.command-status.not-enforced",
                    "queryProvenance": "authorization.query.not-enforced",
                    "projectionSignalR": "authorization.signalr.not-enforced",
                }[surface]
                self.assertIn(expected_reason, document["reasonCodes"])

    def test_query_response_must_belong_to_the_created_tenant(self) -> None:
        runtime = FakeRuntime()
        original_request = runtime.json_request

        def json_request(
            url: str,
            *,
            method: str = "GET",
            token: str | None = None,
            form: dict[str, str] | None = None,
            body: dict[str, Any] | None = None,
            timeout: float = 10,
            deadline: float | None = None,
        ) -> tuple[int, dict[str, Any], dict[str, str]]:
            if (
                url.endswith("/api/v1/queries")
                and token is not None
                and token != "invalid-local-evidence-token"
            ):
                return 200, {"payload": {"TenantId": "different-tenant"}}, {
                    "X-Hexalith-Query-Provenance": "HandlerComputed"
                }
            return original_request(
                url, method=method, token=token, form=form, body=body,
                timeout=timeout, deadline=deadline,
            )

        runtime.json_request = json_request  # type: ignore[method-assign]
        result = smoke.capture(self.output, runtime, timeout=30)
        self.assertEqual(result, 1)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(document["observations"]["queryProvenance"]["reasonCode"], "query.tenant-mismatch")
        self.assertNotEqual(
            document["observations"]["queryProvenance"]["responseTenantId"],
            document["observations"]["queryProvenance"]["entityId"],
        )

    def test_cleanup_requires_independent_process_listing_confirmation(self) -> None:
        runtime = FakeRuntime()
        original_command = runtime.command
        clock = [0.0]

        def command(arguments: list[str], timeout: float) -> smoke.CommandResult:
            if arguments[1] == "ps":
                return smoke.CommandResult(1, "", "synthetic ps failure")
            return original_command(arguments, timeout)

        runtime.command = command  # type: ignore[method-assign]
        with (
            mock.patch.object(smoke.time, "monotonic", side_effect=lambda: clock[0]),
            mock.patch.object(
                smoke.time,
                "sleep",
                side_effect=lambda seconds: clock.__setitem__(0, clock[0] + seconds),
            ),
        ):
            result = smoke.capture(self.output, runtime, timeout=30)
        self.assertEqual(result, 1)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertFalse(document["cleanup"]["hostStopped"])
        self.assertEqual(document["cleanup"]["confirmation"], "aspire-ps-failed")

    def test_cold_stop_must_succeed_before_build_or_start(self) -> None:
        runtime = FakeRuntime()
        original_command = runtime.command
        first_stop = True

        def command(arguments: list[str], timeout: float) -> smoke.CommandResult:
            nonlocal first_stop
            if arguments[:2] == ["aspire", "stop"] and first_stop:
                first_stop = False
                runtime.commands.append(arguments)
                return smoke.CommandResult(2, "", "synthetic cold-stop failure")
            return original_command(arguments, timeout)

        runtime.command = command  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 1)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertIn("apphost.cold-stop.failed", document["reasonCodes"])
        self.assertFalse(document["startup"]["hostStartAttempted"])
        self.assertFalse(any(item[0] == "dotnet" for item in runtime.commands))
        self.assertFalse(any(item[:2] == ["aspire", "start"] for item in runtime.commands))

    def test_cold_stop_requires_aspire_state_confirmation_before_mutation(self) -> None:
        runtime = FakeRuntime()
        original_command = runtime.command
        clock = [0.0]

        def command(arguments: list[str], timeout: float) -> smoke.CommandResult:
            if arguments[:2] == ["aspire", "ps"]:
                runtime.commands.append(arguments)
                return smoke.CommandResult(1, "", "synthetic ps failure")
            return original_command(arguments, timeout)

        runtime.command = command  # type: ignore[method-assign]
        with (
            mock.patch.object(smoke.time, "monotonic", side_effect=lambda: clock[0]),
            mock.patch.object(
                smoke.time,
                "sleep",
                side_effect=lambda seconds: clock.__setitem__(0, clock[0] + seconds),
            ),
        ):
            result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 1)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertIn("apphost.cold-stop.not-confirmed", document["reasonCodes"])
        self.assertFalse(any(item[0] == "dotnet" for item in runtime.commands))
        self.assertFalse(any(item[:2] == ["aspire", "start"] for item in runtime.commands))

    def test_unrelated_running_apphost_does_not_block_target_cold_stop(self) -> None:
        runtime = FakeRuntime()
        original_command = runtime.command
        unrelated = str((smoke.ROOT.parent / "works" / "src" / "Works.AppHost.csproj").resolve())

        def command(arguments: list[str], timeout: float) -> smoke.CommandResult:
            if arguments[:2] == ["aspire", "ps"]:
                runtime.commands.append(arguments)
                records = [{"appHostPath": unrelated}]
                if runtime.started:
                    records.append(
                        {"appHostPath": str((smoke.ROOT / smoke.APPHOST_RELATIVE).resolve())}
                    )
                return smoke.CommandResult(0, json.dumps(records))
            return original_command(arguments, timeout)

        runtime.command = command  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 0)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(document["cleanup"]["runningAppHostsAfterAttempt"], 0)
        self.assertEqual(document["cleanup"]["confirmation"], "aspire-ps-empty")

    def test_running_apphost_count_rejects_unattributed_ps_record(self) -> None:
        runtime = FakeRuntime()

        def command(arguments: list[str], timeout: float) -> smoke.CommandResult:
            del arguments, timeout
            return smoke.CommandResult(0, '[{"name":"frontcomposer"}]')

        runtime.command = command  # type: ignore[method-assign]

        self.assertIsNone(smoke._running_apphost_count(runtime, 10))

    def test_no_url_cleanup_is_clean_only_when_no_host_start_was_attempted(self) -> None:
        never_started = FakeRuntime(build_code=2)

        result = smoke.capture(self.output, never_started, timeout=30)

        self.assertEqual(result, 1)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(document["cleanup"]["result"], "clean")
        self.assertEqual(document["cleanup"]["listenerConfirmation"], "no-host-started")
        self.assertFalse(document["startup"]["hostStartAttempted"])

        attempted = FakeRuntime(start_code=2)
        result = smoke.capture(self.output, attempted, timeout=30)
        self.assertEqual(result, 1)
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertTrue(document["startup"]["hostStartAttempted"])
        self.assertEqual(document["cleanup"]["result"], "failed")
        self.assertEqual(document["cleanup"]["listenerConfirmation"], "ports-unproven")

    def test_signalr_control_and_success_are_paired_on_the_same_endpoint(self) -> None:
        runtime = FakeRuntime()
        calls: list[tuple[str, str]] = []

        def signalr_status(
            hub_url: str,
            negotiation_token: str,
            invalid_token: str,
            timeout: float = 10,
            deadline: float | None = None,
        ) -> int:
            del timeout, deadline
            runtime.assert_token_present(negotiation_token)
            self.assertEqual(invalid_token, "invalid-local-evidence-token")
            calls.append(("control", hub_url))
            return 401

        def signalr_connect(
            hub_url: str,
            token: str,
            timeout: float = 10,
            deadline: float | None = None,
        ) -> bool:
            del timeout, deadline
            runtime.assert_token_present(token)
            calls.append(("success", hub_url))
            return "localhost" in hub_url

        runtime.signalr_invalid_upgrade_status = signalr_status  # type: ignore[method-assign]
        runtime.signalr_connect = signalr_connect  # type: ignore[method-assign]

        result = smoke.capture(self.output, runtime, timeout=30)

        self.assertEqual(result, 0)
        self.assertGreaterEqual(len(calls), 4)
        for index in range(0, len(calls), 2):
            self.assertEqual(calls[index][0], "control")
            self.assertEqual(calls[index + 1][0], "success")
            self.assertEqual(calls[index][1], calls[index + 1][1])
        document = json.loads(self.output.read_text(encoding="utf-8"))
        self.assertEqual(
            document["authorizationControls"]["projectionSignalR"]["endpoint"],
            document["observations"]["projectionSignalR"]["endpoint"],
        )

    def test_http_client_explicitly_disables_environment_proxies(self) -> None:
        handlers: list[Any] = []

        class Response:
            status = 200
            headers: dict[str, str] = {}

            def __enter__(self) -> Response:
                return self

            def __exit__(self, *args: Any) -> None:
                del args

            def read1(self, count: int) -> bytes:
                del count
                return b""

            read = read1

        class Opener:
            def open(self, outbound: Any, timeout: float) -> Response:
                del outbound, timeout
                return Response()

        def build_opener(*values: Any) -> Opener:
            handlers.extend(values)
            return Opener()

        with mock.patch.object(smoke.request, "build_opener", side_effect=build_opener):
            status, _, _ = smoke.SmokeRuntime().json_request(
                "http://127.0.0.1:18080/health",
                token="local-only-token",
                timeout=1,
            )

        self.assertEqual(status, 200)
        proxy = next(item for item in handlers if isinstance(item, smoke.request.ProxyHandler))
        self.assertEqual(proxy.proxies, {})

    def test_websocket_upgrade_accept_and_fragmented_signalr_ack_are_validated(self) -> None:
        class WebSocket:
            def __init__(self, *, accept_valid: bool = True, slow_frames: bool = False) -> None:
                self.accept_valid = accept_valid
                self.slow_frames = slow_frames
                self.sent: list[bytes] = []
                self.response = bytearray()
                self.headers_sent = False

            def settimeout(self, timeout: float) -> None:
                self.timeout = timeout

            def sendall(self, data: bytes) -> None:
                self.sent.append(data)

            def recv(self, count: int) -> bytes:
                if not self.headers_sent:
                    request_text = self.sent[0].decode("ascii")
                    key = next(
                        line.split(":", 1)[1].strip()
                        for line in request_text.split("\r\n")
                        if line.lower().startswith("sec-websocket-key:")
                    )
                    accept = base64.b64encode(
                        hashlib.sha1((key + smoke.WEBSOCKET_ACCEPT_GUID).encode("ascii")).digest()
                    ).decode("ascii")
                    if not self.accept_valid:
                        accept = "invalid"
                    headers = (
                        "HTTP/1.1 101 Switching Protocols\r\n"
                        "Upgrade: websocket\r\n"
                        "Connection: keep-alive, Upgrade\r\n"
                        f"Sec-WebSocket-Accept: {accept}\r\n\r\n"
                    ).encode("ascii")
                    self.response.extend(headers)
                elif not self.response and len(self.sent) > 1:
                    frames = b"\x01\x02{}\x80\x01\x1e"
                    self.response.extend(frames)
                if self.slow_frames and self.headers_sent:
                    count = 1
                self.headers_sent = True
                value = bytes(self.response[:count])
                del self.response[:count]
                return value

            def close(self) -> None:
                pass

        valid = WebSocket()
        with mock.patch.object(smoke.socket, "create_connection", return_value=valid):
            self.assertTrue(
                smoke._websocket_signalr_handshake(
                    "ws://127.0.0.1:18080/hubs/projection-changes?id=connection",
                    "local-token",
                    2,
                )
            )

        invalid = WebSocket(accept_valid=False)
        with mock.patch.object(smoke.socket, "create_connection", return_value=invalid):
            self.assertFalse(
                smoke._websocket_signalr_handshake(
                    "ws://127.0.0.1:18080/hubs/projection-changes?id=connection",
                    "local-token",
                    2,
                )
            )

    def test_websocket_upgrade_accepts_an_alternate_valid_101_reason_phrase(self) -> None:
        key = "dGhlIHNhbXBsZSBub25jZQ=="
        accept = base64.b64encode(
            hashlib.sha1((key + smoke.WEBSOCKET_ACCEPT_GUID).encode("ascii")).digest()
        ).decode("ascii")
        headers = (
            "HTTP/1.1 101 Protocol Upgraded\r\n"
            "Upgrade: websocket\r\n"
            "Connection: Upgrade\r\n"
            f"Sec-WebSocket-Accept: {accept}\r\n\r\n"
        ).encode("ascii")

        self.assertTrue(smoke._valid_websocket_upgrade(headers, key))

    def test_preemptive_signalr_ack_is_rejected_before_client_handshake(self) -> None:
        class PreemptiveWebSocket:
            def __init__(self) -> None:
                self.sent: list[bytes] = []
                self.response = bytearray()

            def settimeout(self, timeout: float) -> None:
                self.timeout = timeout

            def sendall(self, data: bytes) -> None:
                self.sent.append(data)

            def recv(self, count: int) -> bytes:
                if not self.response:
                    key = next(
                        line.split(":", 1)[1].strip()
                        for line in self.sent[0].decode("ascii").split("\r\n")
                        if line.lower().startswith("sec-websocket-key:")
                    )
                    accept = base64.b64encode(
                        hashlib.sha1(
                            (key + smoke.WEBSOCKET_ACCEPT_GUID).encode("ascii")
                        ).digest()
                    ).decode("ascii")
                    self.response.extend(
                        (
                            "HTTP/1.1 101 Switching Protocols\r\n"
                            "Upgrade: websocket\r\nConnection: Upgrade\r\n"
                            f"Sec-WebSocket-Accept: {accept}\r\n\r\n"
                        ).encode("ascii")
                        + b"\x81\x03{}\x1e"
                    )
                value = bytes(self.response[:count])
                del self.response[:count]
                return value

            def close(self) -> None:
                pass

        stream = PreemptiveWebSocket()
        with mock.patch.object(smoke.socket, "create_connection", return_value=stream):
            self.assertFalse(
                smoke._websocket_signalr_handshake(
                    "ws://127.0.0.1:18080/hubs/projection-changes?id=connection",
                    "local-token",
                    2,
                )
            )
        self.assertEqual(len(stream.sent), 1)

    def test_websocket_fragment_slow_drip_cannot_reset_the_absolute_deadline(self) -> None:
        clock = [0.0]

        class SlowWebSocket:
            def __init__(self) -> None:
                self.sent: list[bytes] = []
                self.response = bytearray()
                self.returned_headers = False

            def settimeout(self, timeout: float) -> None:
                self.timeout = timeout

            def sendall(self, data: bytes) -> None:
                self.sent.append(data)

            def recv(self, count: int) -> bytes:
                del count
                if not self.response:
                    key = next(
                        line.split(":", 1)[1].strip()
                        for line in self.sent[0].decode("ascii").split("\r\n")
                        if line.lower().startswith("sec-websocket-key:")
                    )
                    accept = base64.b64encode(
                        hashlib.sha1((key + smoke.WEBSOCKET_ACCEPT_GUID).encode("ascii")).digest()
                    ).decode("ascii")
                    self.response.extend(
                        (
                            "HTTP/1.1 101 Switching Protocols\r\n"
                            "Upgrade: websocket\r\nConnection: Upgrade\r\n"
                            f"Sec-WebSocket-Accept: {accept}\r\n\r\n"
                        ).encode("ascii")
                        + b"\x01\x02{}\x80\x01\x1e"
                    )
                if not self.returned_headers:
                    boundary = self.response.index(b"\r\n\r\n") + 4
                    value = bytes(self.response[:boundary])
                    del self.response[:boundary]
                    self.returned_headers = True
                    return value
                clock[0] += 0.11
                value = bytes(self.response[:1])
                del self.response[:1]
                return value

            def close(self) -> None:
                pass

        stream = SlowWebSocket()
        with (
            mock.patch.object(smoke.socket, "create_connection", return_value=stream),
            mock.patch.object(smoke.time, "monotonic", side_effect=lambda: clock[0]),
        ):
            self.assertFalse(
                smoke._websocket_signalr_handshake(
                    "ws://127.0.0.1:18080/hubs/projection-changes?id=connection",
                    "local-token",
                    1,
                    deadline=0.25,
                )
            )
        self.assertLessEqual(clock[0], 0.33)

    def test_http_redirects_are_returned_without_following_or_forwarding_a_token(self) -> None:
        target_hits = 0

        class Handler(http.server.BaseHTTPRequestHandler):
            def do_GET(self) -> None:  # noqa: N802
                nonlocal target_hits
                if self.path == "/start":
                    self.send_response(302)
                    self.send_header("Location", f"http://127.0.0.1:{self.server.server_port}/target")
                    self.end_headers()
                    return
                target_hits += 1
                self.send_response(200)
                self.end_headers()

            def log_message(self, format: str, *args: Any) -> None:
                del format, args

        server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), Handler)
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        self.addCleanup(server.server_close)
        self.addCleanup(server.shutdown)
        status, _, _ = smoke.SmokeRuntime().json_request(
            f"http://127.0.0.1:{server.server_port}/start",
            token="local-only-token",
            timeout=2,
        )
        self.assertEqual(status, 302)
        self.assertEqual(target_hits, 0)

    def test_http_body_slow_drip_cannot_reset_the_absolute_deadline(self) -> None:
        class Handler(http.server.BaseHTTPRequestHandler):
            def do_GET(self) -> None:  # noqa: N802
                self.send_response(200)
                self.end_headers()
                for value in b'{"value":"slow"}':
                    try:
                        self.wfile.write(bytes([value]))
                        self.wfile.flush()
                    except OSError:
                        break
                    time.sleep(0.03)

            def log_message(self, format: str, *args: Any) -> None:
                del format, args

        server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), Handler)
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        self.addCleanup(server.server_close)
        self.addCleanup(server.shutdown)
        started = time.monotonic()
        status, _, _ = smoke.SmokeRuntime().json_request(
            f"http://127.0.0.1:{server.server_port}/slow",
            timeout=2,
            deadline=time.monotonic() + 0.12,
        )
        self.assertEqual(status, 0)
        self.assertLess(time.monotonic() - started, 0.8)

    def test_cli_failure_report_prints_reason_codes(self) -> None:
        self.output.write_text(
            json.dumps(
                {
                    "finalVerdict": "failed",
                    "reasonCodes": ["apphost.start.failed"],
                    "startup": {"startReturnCode": 2, "startStderr": "bind failed"},
                }
            )
            + "\n",
            encoding="utf-8",
        )
        buffer = io.StringIO()
        with contextlib.redirect_stderr(buffer):
            smoke._report_failure(self.output)
        text = buffer.getvalue()
        self.assertIn("AppHost smoke failed", text)
        self.assertIn("apphost.start.failed", text)
        self.assertIn("finalVerdict=failed", text)
        self.assertIn("startReturnCode=2", text)
        self.assertIn("startStderr=bind failed", text)


if __name__ == "__main__":
    unittest.main()
