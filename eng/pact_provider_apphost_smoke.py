#!/usr/bin/env python3
"""Capture bounded, authenticated Pact-reconciliation evidence from the real Aspire AppHost."""

from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
import re
import secrets
import socket
import ssl
import subprocess
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from http.client import HTTPException, IncompleteRead
from typing import Any
from urllib import error, parse, request


ROOT = Path(__file__).resolve().parents[1]
APPHOST_RELATIVE = "src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj"
APPHOST = ROOT / APPHOST_RELATIVE
PROGRAM_RELATIVE = "src/Hexalith.FrontComposer.AppHost/Program.cs"
PROGRAM = ROOT / PROGRAM_RELATIVE
REQUIRED_RESOURCES = (
    "security",
    "eventstore",
    "eventstore-admin",
    "eventstore-admin-ui",
    "tenants",
    "parties",
    "sample",
    "tenants-ui",
    "frontcomposer-ui",
    "counter-web",
)
OBSERVATIONS = ("health", "commandSubmit", "commandStatus", "queryProvenance", "projectionSignalR")
STOP_COMMAND = f"aspire stop --apphost {APPHOST_RELATIVE} --non-interactive --nologo"
# `aspire describe --format Json` for the ten-resource AppHost exceeds 8 KiB. Truncating
# from the tail made `_json_from_output` parse a nested fragment and fail closed as
# `apphost.describe.incomplete` after every resource was already healthy.
MAX_OUTPUT_CHARS = 1_048_576
ULID_ALPHABET = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"


@dataclass(frozen=True)
class CommandResult:
    returncode: int
    stdout: str = ""
    stderr: str = ""


class _CleanupFailed(RuntimeError):
    pass


class SmokeRuntime:
    """Runtime boundary kept injectable so failure/cleanup behavior is unit-testable."""

    def command(self, arguments: list[str], timeout: int) -> CommandResult:
        try:
            completed = subprocess.run(
                arguments,
                cwd=ROOT,
                check=False,
                capture_output=True,
                text=True,
                timeout=timeout,
            )
            return CommandResult(completed.returncode, completed.stdout[-MAX_OUTPUT_CHARS:], completed.stderr[-MAX_OUTPUT_CHARS:])
        except (OSError, subprocess.TimeoutExpired) as exception:
            return CommandResult(124, "", type(exception).__name__)

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
        data: bytes | None = None
        headers = {"Accept": "application/json"}
        if form is not None:
            data = parse.urlencode(form).encode("utf-8")
            headers["Content-Type"] = "application/x-www-form-urlencoded"
        elif body is not None:
            data = json.dumps(body, separators=(",", ":")).encode("utf-8")
            headers["Content-Type"] = "application/json"
        if token:
            headers["Authorization"] = f"Bearer {token}"
        outbound = request.Request(url, data=data, headers=headers, method=method)
        context = ssl._create_unverified_context()  # Local Aspire development certificates only.
        try:
            with request.urlopen(outbound, timeout=timeout, context=context) as response:
                raw = response.read(1_048_577)
                if len(raw) > 1_048_576:
                    raise ValueError("response-too-large")
                try:
                    document = json.loads(raw.decode("utf-8")) if raw else {}
                except (UnicodeDecodeError, json.JSONDecodeError):
                    document = {}
                return response.status, document if isinstance(document, dict) else {}, dict(response.headers.items())
        except error.HTTPError as exception:
            raw = exception.read(1_048_577)
            try:
                document = json.loads(raw.decode("utf-8")) if raw else {}
            except (UnicodeDecodeError, json.JSONDecodeError):
                document = {}
            return exception.code, document if isinstance(document, dict) else {}, dict(exception.headers.items())
        except (error.URLError, TimeoutError, ssl.SSLError, OSError, IncompleteRead, HTTPException):
            return 0, {}, {}

    def signalr_connect(self, hub_url: str, token: str, timeout: int = 10) -> bool:
        negotiate = f"{hub_url.rstrip('/')}/negotiate?negotiateVersion=1"
        status, document, _ = self.json_request(negotiate, method="POST", token=token, body={}, timeout=timeout)
        connection_token = document.get("connectionToken")
        if not isinstance(connection_token, str) or not connection_token:
            connection_token = document.get("connectionId")
        transports = document.get("availableTransports", [])
        if status != 200 or not isinstance(connection_token, str) or not connection_token or not any(
            isinstance(item, dict) and item.get("transport") == "WebSockets" for item in transports
        ):
            return False
        websocket_url = _websocket_url(hub_url, connection_token, token)
        return _websocket_signalr_handshake(websocket_url, token, timeout)


def _sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _git(directory: Path, *arguments: str) -> str:
    try:
        result = subprocess.run(
            ["git", *arguments],
            cwd=directory,
            check=True,
            capture_output=True,
            text=True,
            timeout=10,
        )
    except (OSError, subprocess.SubprocessError):
        return ""
    return result.stdout.strip().lower()


def _release_version() -> str:
    catalog = (ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props").read_text(encoding="utf-8-sig")
    match = re.search(r"<HexalithEventStoreVersion[^>]*>([^<]+)</HexalithEventStoreVersion>", catalog)
    return match.group(1).strip() if match else ""


def _ulid() -> str:
    timestamp = int(time.time() * 1000)
    time_part = ""
    for _ in range(10):
        time_part = ULID_ALPHABET[timestamp % 32] + time_part
        timestamp //= 32
    return time_part + "".join(secrets.choice(ULID_ALPHABET) for _ in range(16))


def _json_from_output(output: str) -> Any:
    decoder = json.JSONDecoder()
    for offset, character in enumerate(output):
        if character not in "[{":
            continue
        try:
            value, _ = decoder.raw_decode(output[offset:])
            return value
        except json.JSONDecodeError:
            continue
    return None


def _logical_name(record: dict[str, Any]) -> str:
    for key in ("displayName", "name", "Name"):
        value = record.get(key)
        if isinstance(value, str) and value:
            return value
    return ""


def _resource_records(value: Any) -> list[dict[str, Any]]:
    if isinstance(value, dict) and isinstance(value.get("resources"), list):
        return [item for item in value["resources"] if isinstance(item, dict)]
    records: list[dict[str, Any]] = []
    if isinstance(value, dict):
        name = value.get("name", value.get("Name"))
        if isinstance(name, str):
            records.append(value)
        for item in value.values():
            records.extend(_resource_records(item))
    elif isinstance(value, list):
        for item in value:
            records.extend(_resource_records(item))
    return records


def _resource_endpoint(records: list[dict[str, Any]], resource: str) -> str:
    candidates: list[str] = []
    for record in records:
        if _logical_name(record) != resource:
            continue
        urls = record.get("urls")
        if isinstance(urls, list):
            public: list[dict[str, Any]] = []
            for item in urls:
                if not isinstance(item, dict):
                    continue
                url = item.get("url")
                if not isinstance(url, str) or not url.startswith(("http://", "https://")):
                    continue
                if item.get("isInternal") is True or item.get("name") == "management":
                    continue
                public.append(item)
            named_https = next((item["url"].rstrip("/") for item in public if item.get("name") == "https"), None)
            if named_https:
                return named_https
            https = next((item["url"].rstrip("/") for item in public if item["url"].startswith("https://")), None)
            if https or public:
                return https or public[0]["url"].rstrip("/")
            continue
        stack: list[Any] = [record]
        while stack:
            nested = stack.pop()
            if isinstance(nested, dict):
                stack.extend(nested.values())
            elif isinstance(nested, list):
                stack.extend(nested)
            elif isinstance(nested, str) and nested.startswith(("http://", "https://")):
                candidates.append(nested.rstrip("/"))
    secure = next((item for item in candidates if item.startswith("https://")), None)
    return secure or (candidates[0] if candidates else "")


def _loopback_variants(url: str) -> list[str]:
    variants = [url]
    if "://localhost" in url:
        variants.append(url.replace("://localhost", "://127.0.0.1"))
    elif "://127.0.0.1" in url:
        variants.append(url.replace("://127.0.0.1", "://localhost"))
    return variants


def _resource_public_urls(records: list[dict[str, Any]], resource: str) -> list[str]:
    primary = _resource_endpoint(records, resource)
    extras: list[str] = []
    for record in records:
        if _logical_name(record) != resource:
            continue
        urls = record.get("urls")
        if not isinstance(urls, list):
            continue
        for item in urls:
            if not isinstance(item, dict) or item.get("isInternal") is True or item.get("name") == "management":
                continue
            url = item.get("url")
            if isinstance(url, str) and url.startswith(("http://", "https://")):
                extras.append(url.rstrip("/"))
    ordered: list[str] = []
    for url in [primary, *extras]:
        if not url:
            continue
        for variant in _loopback_variants(url):
            if variant not in ordered:
                ordered.append(variant)
    # DCP's advertised HTTPS proxy can accept and then hang (WSL/WinNAT). Prefer
    # plaintext http:// endpoints when both exist so authenticated probes complete.
    http_urls = [url for url in ordered if url.startswith("http://")]
    https_urls = [url for url in ordered if url.startswith("https://")]
    return http_urls + https_urls


def _resource_signalr_urls(records: list[dict[str, Any]], resource: str) -> list[str]:
    ordered = _resource_public_urls(records, resource)
    extras: list[str] = []
    for record in records:
        if _logical_name(record) != resource:
            continue
        urls = record.get("urls")
        if not isinstance(urls, list):
            continue
        for item in urls:
            if not isinstance(item, dict) or item.get("name") == "management":
                continue
            url = item.get("url")
            if isinstance(url, str) and url.startswith(("http://", "https://")):
                extras.extend(_loopback_variants(url.rstrip("/")))
    http_extras = [url for url in extras if url.startswith("http://") and url not in ordered]
    https_extras = [url for url in extras if url.startswith("https://") and url not in ordered]
    return ordered + http_extras + https_extras


def _websocket_url(hub_url: str, connection_token: str, token: str) -> str:
    parsed = parse.urlsplit(hub_url)
    scheme = "wss" if parsed.scheme == "https" else "ws"
    query = parse.urlencode({"id": connection_token, "access_token": token})
    return parse.urlunsplit((scheme, parsed.netloc, parsed.path, query, ""))


def _read_http_headers(stream: socket.socket, timeout: int) -> bytes:
    stream.settimeout(timeout)
    data = bytearray()
    while b"\r\n\r\n" not in data and len(data) <= 16_384:
        chunk = stream.recv(4_096)
        if not chunk:
            break
        data.extend(chunk)
    return bytes(data)


def _masked_text_frame(payload: bytes) -> bytes:
    mask = secrets.token_bytes(4)
    length = len(payload)
    if length < 126:
        prefix = bytes((0x81, 0x80 | length))
    else:
        prefix = bytes((0x81, 0x80 | 126)) + length.to_bytes(2, "big")
    masked = bytes(value ^ mask[index % 4] for index, value in enumerate(payload))
    return prefix + mask + masked


def _websocket_signalr_handshake(websocket_url: str, token: str, timeout: int) -> bool:
    parsed = parse.urlsplit(websocket_url)
    port = parsed.port or (443 if parsed.scheme == "wss" else 80)
    stream: socket.socket | None = None
    try:
        stream = socket.create_connection((parsed.hostname or "", port), timeout=timeout)
        if parsed.scheme == "wss":
            context = ssl._create_unverified_context()
            stream = context.wrap_socket(stream, server_hostname=parsed.hostname)
        key = base64.b64encode(secrets.token_bytes(16)).decode("ascii")
        target = parsed.path + (f"?{parsed.query}" if parsed.query else "")
        origin = f"{'https' if parsed.scheme == 'wss' else 'http'}://{parsed.netloc}"
        token_header = token.replace("\r", "").replace("\n", "")
        upgrade = (
            f"GET {target} HTTP/1.1\r\nHost: {parsed.netloc}\r\nUpgrade: websocket\r\n"
            f"Connection: Upgrade\r\nAuthorization: Bearer {token_header}\r\n"
            f"Origin: {origin}\r\nSec-WebSocket-Key: {key}\r\nSec-WebSocket-Version: 13\r\n\r\n"
        ).encode("ascii")
        stream.sendall(upgrade)
        if not _read_http_headers(stream, timeout).startswith(b"HTTP/1.1 101"):
            return False
        stream.sendall(_masked_text_frame(b'{"protocol":"json","version":1}\x1e'))
        deadline = time.monotonic() + timeout
        response = b""
        while time.monotonic() < deadline and b"{}\x1e" not in response:
            chunk = stream.recv(4_096)
            if not chunk:
                break
            response += chunk
        return b"{}\x1e" in response
    except (OSError, ssl.SSLError, ValueError):
        return False
    finally:
        if stream is not None:
            try:
                stream.close()
            except OSError:
                pass


def _base_evidence() -> dict[str, Any]:
    return {
        "schema": "hexalith.frontcomposer.pact-provider-reconciliation-apphost-smoke.v1",
        "capturedAt": datetime.now(timezone.utc).isoformat(),
        "finalVerdict": "failed",
        "reasonCodes": [],
        "identity": {
            "eventStoreSourceSha": _git(ROOT / "references/Hexalith.EventStore", "rev-parse", "HEAD"),
            "eventStoreReleaseVersion": _release_version(),
            "buildsCatalogSha": _git(ROOT / "references/Hexalith.Builds", "rev-parse", "HEAD"),
        },
        "topology": {
            "programPath": PROGRAM_RELATIVE,
            "programSha256": _sha256(PROGRAM),
            "projectPath": APPHOST_RELATIVE,
            "projectSha256": _sha256(APPHOST),
            "modifiedForSmoke": False,
            "declaredResources": list(REQUIRED_RESOURCES),
        },
        "startup": {"result": "failed", "resourceWaits": {name: "not-observed" for name in REQUIRED_RESOURCES}},
        "observations": {
            name: {"result": "not-observed", "authenticated": False, "reasonCode": "runtime.not-reached"}
            for name in OBSERVATIONS
        },
        "cleanup": {
            "command": STOP_COMMAND,
            "result": "failed",
            "hostStopped": False,
            "portsClosed": False,
            "runningAppHostsAfterAttempt": 1,
        },
    }


def _atomic_write(path: Path, document: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.{os.getpid()}.tmp")
    temporary.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def _describe_host(runtime: SmokeRuntime) -> tuple[bool, list[dict[str, Any]]]:
    described = runtime.command(
        ["aspire", "describe", "--apphost", APPHOST_RELATIVE, "--format", "Json", "--non-interactive", "--nologo"],
        15,
    )
    parsed = _json_from_output(described.stdout) if described.returncode == 0 else None
    if described.returncode != 0 or parsed is None:
        return False, []
    return True, _resource_records(parsed)


def _probed_resource_urls(records: list[dict[str, Any]]) -> list[str]:
    urls: list[str] = []
    for name in REQUIRED_RESOURCES:
        urls.extend(_resource_public_urls(records, name))
        endpoint = _resource_endpoint(records, name)
        if endpoint:
            urls.append(endpoint)
        if name == "eventstore":
            urls.extend(_resource_signalr_urls(records, name))
    ordered: list[str] = []
    for url in urls:
        if url and url not in ordered:
            ordered.append(url)
    return ordered


def _wait_until_host_absent_or_ports_closed(runtime: SmokeRuntime, urls: list[str], timeout: int = 15) -> None:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        present, records = _describe_host(runtime)
        if not present:
            return
        observed = [url for url in urls if url] or _probed_resource_urls(records)
        if observed and all(not _port_open(url) for url in observed):
            return
        time.sleep(0.5)


def _capture(output: Path, runtime: SmokeRuntime, timeout: int) -> int:
    evidence = _base_evidence()
    endpoints: dict[str, str] = {}
    probed_urls: list[str] = []
    reason_codes: list[str] = evidence["reasonCodes"]
    started = False
    try:
        # `aspire start --format Json` restarts a running AppHost. That restart launches
        # frontcomposer-ui with `dotnet run --no-build` against a half-stopped process tree
        # and the resource exits before wait. Stop first so capture is always a cold start.
        runtime.command(
            ["aspire", "stop", "--apphost", APPHOST_RELATIVE, "--non-interactive", "--nologo"],
            60,
        )
        if runtime.__class__ is SmokeRuntime:
            _wait_until_host_absent_or_ports_closed(runtime, [])
        start = runtime.command(
            ["aspire", "start", "--apphost", APPHOST_RELATIVE, "--non-interactive", "--nologo"],
            timeout,
        )
        if start.returncode != 0:
            reason_codes.append("apphost.start.failed")
            return 1
        started = True
        for resource in REQUIRED_RESOURCES:
            waited = runtime.command(
                ["aspire", "wait", resource, "--status", "healthy", "--timeout", "120", "--apphost", APPHOST_RELATIVE, "--non-interactive", "--nologo"],
                130,
            )
            evidence["startup"]["resourceWaits"][resource] = "healthy" if waited.returncode == 0 else "failed"
            if waited.returncode != 0:
                reason_codes.append(f"resource.{resource}.not-healthy")
        if reason_codes:
            return 1
        described = runtime.command(
            ["aspire", "describe", "--apphost", APPHOST_RELATIVE, "--format", "Json", "--non-interactive", "--nologo"],
            30,
        )
        records = _resource_records(_json_from_output(described.stdout)) if described.returncode == 0 else []
        names = {_logical_name(item) for item in records}
        if not set(REQUIRED_RESOURCES).issubset(names):
            reason_codes.append("apphost.describe.incomplete")
            return 1
        endpoints = {name: _resource_endpoint(records, name) for name in REQUIRED_RESOURCES}
        eventstore_bases = _resource_public_urls(records, "eventstore")
        security_bases = _resource_public_urls(records, "security")
        probed_urls = _probed_resource_urls(records)
        if not endpoints["security"] or not endpoints["eventstore"] or not endpoints["tenants"]:
            reason_codes.append("apphost.endpoints.incomplete")
            return 1
        evidence["startup"]["result"] = "passed"

        token = None
        token_status = 0
        deadline = time.monotonic() + 30
        while time.monotonic() < deadline:
            for security_base in security_bases or [endpoints["security"]]:
                token_status, token_document, _ = runtime.json_request(
                    f"{security_base}/realms/hexalith/protocol/openid-connect/token",
                    method="POST",
                    form={
                        "grant_type": "password",
                        "client_id": "hexalith-eventstore",
                        "username": "admin-user",
                        "password": "admin-pass",
                    },
                    timeout=15,
                )
                token = token_document.get("access_token")
                if token_status == 200 and isinstance(token, str) and token:
                    break
            if token_status == 200 and isinstance(token, str) and token:
                break
            time.sleep(1)
        if token_status != 200 or not isinstance(token, str) or not token:
            reason_codes.append("auth.local-identity.unavailable")
            return 1

        health_status = 0
        eventstore_base = eventstore_bases[0] if eventstore_bases else endpoints["eventstore"]
        for base in eventstore_bases or [endpoints["eventstore"]]:
            health_status, _, _ = runtime.json_request(f"{base}/health", token=token)
            if health_status not in (200, 204):
                health_status, _, _ = runtime.json_request(f"{base}/alive", token=token)
            if health_status in (200, 204):
                eventstore_base = base
                break
        evidence["observations"]["health"] = {
            "result": "passed" if health_status in (200, 204) else "failed",
            "authenticated": True,
            "reasonCode": "health.authenticated.succeeded" if health_status in (200, 204) else "health.authenticated.failed",
            "statusCode": health_status,
        }

        message_id = _ulid()
        tenant_id = f"pact-reconciliation-{message_id.lower()}"
        submit_status, submit_document, _ = runtime.json_request(
            f"{eventstore_base}/api/v1/commands",
            method="POST",
            token=token,
            body={
                "messageId": message_id,
                "tenant": "system",
                "domain": "tenants",
                "aggregateId": tenant_id,
                "commandType": "CreateTenant",
                "payload": {"TenantId": tenant_id, "Name": "Pact Reconciliation", "Description": "Bounded local smoke"},
            },
            timeout=30,
        )
        correlation = submit_document.get("correlationId", message_id)
        submit_passed = submit_status == 202 and isinstance(correlation, str) and bool(correlation)
        evidence["observations"]["commandSubmit"] = {
            "result": "passed" if submit_passed else "failed",
            "authenticated": True,
            "reasonCode": "command.accepted" if submit_passed else "command.not-accepted",
            "statusCode": submit_status,
        }
        terminal = ""
        if submit_passed:
            deadline = time.monotonic() + 60
            while time.monotonic() < deadline:
                status_code, status_document, _ = runtime.json_request(
                    f"{eventstore_base}/api/v1/commands/status/{parse.quote(correlation, safe='')}",
                    token=token,
                )
                terminal = str(status_document.get("status", "")) if status_code == 200 else ""
                if terminal in ("Completed", "Rejected", "PublishFailed", "TimedOut"):
                    break
                time.sleep(1)
        status_passed = terminal == "Completed"
        evidence["observations"]["commandStatus"] = {
            "result": "passed" if status_passed else "failed",
            "authenticated": True,
            "reasonCode": "command.completed" if status_passed else "command.not-completed",
            "terminalStatus": terminal or "not-observed",
        }

        query_status = 0
        provenance = ""
        query_deadline = time.monotonic() + 60
        while time.monotonic() < query_deadline:
            query_status, _, query_headers = runtime.json_request(
                f"{eventstore_base}/api/v1/queries",
                method="POST",
                token=token,
                body={
                    "tenant": "system",
                    "domain": "tenants",
                    "aggregateId": "index",
                    "queryType": "list-tenants",
                    "projectionType": "tenants",
                    "payload": {"pageSize": 10},
                },
                timeout=30,
            )
            provenance = next((value for key, value in query_headers.items() if key.lower() == "x-hexalith-query-provenance"), "")
            # Tenant handler routes are stamped HandlerComputed; projection-actor routes are
            # ProjectionBacked. This topology has no EventStore.Sample processor, so the live
            # tenant query is the authentic provenance observation.
            if query_status == 200 and provenance in ("ProjectionBacked", "HandlerComputed"):
                break
            time.sleep(1)
        query_passed = query_status == 200 and provenance in ("ProjectionBacked", "HandlerComputed")
        if query_passed and provenance == "ProjectionBacked":
            query_reason = "query.projection-backed"
        elif query_passed:
            query_reason = "query.handler-computed"
        else:
            query_reason = "query.provenance.missing"
        evidence["observations"]["queryProvenance"] = {
            "result": "passed" if query_passed else "failed",
            "authenticated": True,
            "reasonCode": query_reason,
            "statusCode": query_status,
            "provenance": provenance or "not-observed",
        }

        signalr_passed = False
        for base in _resource_signalr_urls(records, "eventstore") or eventstore_bases or [eventstore_base]:
            if runtime.signalr_connect(f"{base}/hubs/projection-changes", token, timeout=5):
                signalr_passed = True
                break
        evidence["observations"]["projectionSignalR"] = {
            "result": "passed" if signalr_passed else "failed",
            "authenticated": True,
            "reasonCode": "signalr.authenticated-connect.succeeded" if signalr_passed else "signalr.authenticated-connect.failed",
        }

        for name in OBSERVATIONS:
            if evidence["observations"][name]["result"] != "passed":
                reason_codes.append(evidence["observations"][name]["reasonCode"])
        evidence["finalVerdict"] = "passed" if not reason_codes else "failed"
        return 0 if not reason_codes else 1
    except (OSError, ValueError, json.JSONDecodeError) as exception:
        reason_codes.append(f"apphost.capture.{type(exception).__name__.lower()}")
        return 1
    finally:
        stop = runtime.command(
            ["aspire", "stop", "--apphost", APPHOST_RELATIVE, "--non-interactive", "--nologo"],
            60,
        )
        describe_after = runtime.command(
            ["aspire", "describe", "--apphost", APPHOST_RELATIVE, "--format", "Json", "--non-interactive", "--nologo"],
            15,
        )
        host_stopped = describe_after.returncode != 0 or _json_from_output(describe_after.stdout) is None
        ports_closed = False
        urls_to_close = [url for url in [*endpoints.values(), *probed_urls] if url]
        ordered_close: list[str] = []
        for url in urls_to_close:
            if url not in ordered_close:
                ordered_close.append(url)
        deadline = time.monotonic() + 15
        while host_stopped and time.monotonic() < deadline:
            ports_closed = all(not _port_open(value) for value in ordered_close)
            if ports_closed:
                break
            time.sleep(0.5)
        if not ordered_close:
            ports_closed = host_stopped
        clean = host_stopped and ports_closed and (stop.returncode == 0 or not started)
        evidence["cleanup"] = {
            "command": STOP_COMMAND,
            "result": "clean" if clean else "failed",
            "hostStopped": host_stopped,
            "portsClosed": ports_closed,
            "runningAppHostsAfterAttempt": 0 if host_stopped else 1,
        }
        if not clean and "apphost.cleanup.incomplete" not in reason_codes:
            reason_codes.append("apphost.cleanup.incomplete")
        if reason_codes:
            evidence["finalVerdict"] = "failed"
        _atomic_write(output, evidence)
        if not clean:
            raise _CleanupFailed


def capture(output: Path, runtime: SmokeRuntime | None = None, timeout: int = 300) -> int:
    try:
        return _capture(output, runtime or SmokeRuntime(), timeout)
    except _CleanupFailed:
        return 1


def _port_open(url: str) -> bool:
    parsed = parse.urlsplit(url)
    try:
        with socket.create_connection((parsed.hostname or "", parsed.port or (443 if parsed.scheme == "https" else 80)), timeout=1):
            return True
    except OSError:
        return False


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--output",
        type=Path,
        default=ROOT / "_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/apphost-smoke.json",
    )
    parser.add_argument("--timeout-seconds", type=int, default=300)
    args = parser.parse_args(argv)
    if not 30 <= args.timeout_seconds <= 600:
        parser.error("--timeout-seconds must be between 30 and 600")
    return capture(args.output.absolute(), timeout=args.timeout_seconds)


if __name__ == "__main__":
    raise SystemExit(main())
