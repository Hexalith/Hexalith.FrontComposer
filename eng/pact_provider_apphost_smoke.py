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
MAX_OUTPUT_CHARS = 8_192
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
                document = json.loads(raw.decode("utf-8")) if raw else {}
                return response.status, document if isinstance(document, dict) else {}, dict(response.headers.items())
        except error.HTTPError as exception:
            raw = exception.read(1_048_577)
            try:
                document = json.loads(raw.decode("utf-8")) if raw else {}
            except (UnicodeDecodeError, json.JSONDecodeError):
                document = {}
            return exception.code, document if isinstance(document, dict) else {}, dict(exception.headers.items())

    def signalr_connect(self, hub_url: str, token: str, timeout: int = 10) -> bool:
        negotiate = f"{hub_url.rstrip('/')}/negotiate?negotiateVersion=1"
        status, document, _ = self.json_request(negotiate, method="POST", token=token, timeout=timeout)
        connection_token = document.get("connectionToken")
        transports = document.get("availableTransports", [])
        if status != 200 or not isinstance(connection_token, str) or not any(
            isinstance(item, dict) and item.get("transport") == "WebSockets" for item in transports
        ):
            return False
        websocket_url = _websocket_url(hub_url, connection_token, token)
        return _websocket_signalr_handshake(websocket_url, timeout)


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


def _resource_records(value: Any) -> list[dict[str, Any]]:
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
        if record.get("name", record.get("Name")) != resource:
            continue
        stack: list[Any] = [record]
        while stack:
            value = stack.pop()
            if isinstance(value, dict):
                stack.extend(value.values())
            elif isinstance(value, list):
                stack.extend(value)
            elif isinstance(value, str) and value.startswith(("http://", "https://")):
                candidates.append(value.rstrip("/"))
    secure = next((value for value in candidates if value.startswith("https://")), None)
    return secure or (candidates[0] if candidates else "")


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


def _websocket_signalr_handshake(websocket_url: str, timeout: int) -> bool:
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
        upgrade = (
            f"GET {target} HTTP/1.1\r\nHost: {parsed.netloc}\r\nUpgrade: websocket\r\n"
            f"Connection: Upgrade\r\nSec-WebSocket-Key: {key}\r\nSec-WebSocket-Version: 13\r\n\r\n"
        ).encode("ascii")
        stream.sendall(upgrade)
        if not _read_http_headers(stream, timeout).startswith(b"HTTP/1.1 101"):
            return False
        stream.sendall(_masked_text_frame(b'{"protocol":"json","version":1}\x1e'))
        response = stream.recv(4_096)
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


def _capture(output: Path, runtime: SmokeRuntime, timeout: int) -> int:
    evidence = _base_evidence()
    endpoints: dict[str, str] = {}
    reason_codes: list[str] = evidence["reasonCodes"]
    started = False
    try:
        start = runtime.command(
            ["aspire", "start", "--apphost", APPHOST_RELATIVE, "--format", "Json", "--non-interactive", "--nologo"],
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
        names = {str(item.get("name", item.get("Name", ""))) for item in records}
        if not set(REQUIRED_RESOURCES).issubset(names):
            reason_codes.append("apphost.describe.incomplete")
            return 1
        endpoints = {name: _resource_endpoint(records, name) for name in REQUIRED_RESOURCES}
        if not endpoints["security"] or not endpoints["eventstore"] or not endpoints["tenants"]:
            reason_codes.append("apphost.endpoints.incomplete")
            return 1
        evidence["startup"]["result"] = "passed"

        token_status, token_document, _ = runtime.json_request(
            f"{endpoints['security']}/realms/hexalith/protocol/openid-connect/token",
            method="POST",
            form={
                "grant_type": "password",
                "client_id": "hexalith-eventstore",
                "username": "admin-user",
                "password": "admin-pass",
            },
        )
        token = token_document.get("access_token")
        if token_status != 200 or not isinstance(token, str) or not token:
            reason_codes.append("auth.local-identity.unavailable")
            return 1

        health_status, _, _ = runtime.json_request(f"{endpoints['eventstore']}/health", token=token)
        if health_status not in (200, 204):
            health_status, _, _ = runtime.json_request(f"{endpoints['eventstore']}/alive", token=token)
        evidence["observations"]["health"] = {
            "result": "passed" if health_status in (200, 204) else "failed",
            "authenticated": True,
            "reasonCode": "health.authenticated.succeeded" if health_status in (200, 204) else "health.authenticated.failed",
            "statusCode": health_status,
        }

        message_id = _ulid()
        tenant_id = f"pact-reconciliation-{message_id.lower()}"
        submit_status, submit_document, _ = runtime.json_request(
            f"{endpoints['eventstore']}/api/v1/commands",
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
                    f"{endpoints['eventstore']}/api/v1/commands/status/{parse.quote(correlation, safe='')}",
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

        query_status, _, query_headers = runtime.json_request(
            f"{endpoints['eventstore']}/api/v1/queries",
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
        query_passed = query_status == 200 and provenance == "ProjectionBacked"
        evidence["observations"]["queryProvenance"] = {
            "result": "passed" if query_passed else "failed",
            "authenticated": True,
            "reasonCode": "query.projection-backed" if query_passed else "query.provenance.missing",
            "statusCode": query_status,
            "provenance": provenance or "not-observed",
        }

        signalr_passed = runtime.signalr_connect(f"{endpoints['eventstore']}/hubs/projection-changes", token)
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
        ports_closed = host_stopped and all(not _port_open(value) for value in endpoints.values() if value)
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
