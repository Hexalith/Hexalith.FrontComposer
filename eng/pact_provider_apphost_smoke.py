#!/usr/bin/env python3
"""Capture bounded, authenticated Pact-reconciliation evidence from the real Aspire AppHost."""

from __future__ import annotations

import argparse
import base64
import hashlib
import ipaddress
import json
import os
import queue
import re
import secrets
import socket
import ssl
import subprocess
import sys
import tempfile
import threading
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from http.client import HTTPException, IncompleteRead
from typing import Any
from urllib import error, parse, request

import eventstore_runtime_evidence as runtime_evidence


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
AUTHORIZATION_CONTROLS = ("commandSubmit", "commandStatus", "queryProvenance", "projectionSignalR")
STOP_COMMAND = f"aspire stop --apphost {APPHOST_RELATIVE} --non-interactive --nologo"
START_COMMAND = [
    "aspire",
    "start",
    "--apphost",
    APPHOST_RELATIVE,
    "--isolated",
    "--non-interactive",
    "--nologo",
]
# `aspire describe --format Json` for the ten-resource AppHost exceeds 8 KiB. Truncating
# from the tail made `_json_from_output` parse a nested fragment and fail closed as
# `apphost.describe.incomplete` after every resource was already healthy.
MAX_OUTPUT_CHARS = 1_048_576
MAX_HTTP_BODY_BYTES = 1_048_576
MAX_WEBSOCKET_HEADER_BYTES = 16_384
MAX_WEBSOCKET_BYTES = 1_048_576
ULID_ALPHABET = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"
WEBSOCKET_ACCEPT_GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
APPHOST_BUILD_PROPERTIES = runtime_evidence.APPHOST_BUILD_PROPERTIES
SOURCE_DEPENDENCY_GITLINKS = runtime_evidence.RUNTIME_DEPENDENCY_GITLINKS
BUILD_CONTROL_GITLINKS = runtime_evidence.APPHOST_BUILD_CONTROL_GITLINKS
REACHABLE_SOURCE_GITLINKS = runtime_evidence.APPHOST_REACHABLE_SOURCE_GITLINKS
INACTIVE_GUARDED_GITLINKS = runtime_evidence.APPHOST_INACTIVE_GUARDED_GITLINKS
DAPR_NAME_RESOLUTION_RELATIVES = (
    "src/Hexalith.FrontComposer.AppHost/nr.db",
    "src/Hexalith.FrontComposer.AppHost/nr.db-shm",
    "src/Hexalith.FrontComposer.AppHost/nr.db-wal",
)
SOURCE_ROOT_PROPERTIES = {
    "EventStorePath": "references/Hexalith.EventStore",
    "TenantsPath": "references/Hexalith.Tenants",
    "PartiesPath": "references/Hexalith.Parties",
    "MemoriesPath": "references/Hexalith.Memories",
    "CommonsPath": "references/Hexalith.Commons",
    "HexalithPolymorphicSerializationsRoot": "references/Hexalith.PolymorphicSerializations",
}


@dataclass(frozen=True)
class CommandResult:
    returncode: int
    stdout: str = ""
    stderr: str = ""


class _CleanupFailed(RuntimeError):
    pass


class _RejectRedirectHandler(request.HTTPRedirectHandler):
    """Turn redirects into their original HTTP response without following them."""

    def redirect_request(self, req: Any, fp: Any, code: int, msg: str, headers: Any, newurl: str) -> None:
        del req, fp, code, msg, headers, newurl
        return None


def _set_response_timeout(response: Any, timeout: float) -> None:
    """Best-effort socket timeout refresh for urllib response body reads."""
    candidates = [
        getattr(getattr(getattr(response, "fp", None), "raw", None), "_sock", None),
        getattr(
            getattr(getattr(getattr(response, "fp", None), "fp", None), "raw", None),
            "_sock",
            None,
        ),
        getattr(getattr(response, "fp", None), "_sock", None),
        getattr(response, "_sock", None),
    ]
    for candidate in candidates:
        if candidate is not None and hasattr(candidate, "settimeout"):
            candidate.settimeout(timeout)
            return


def _bounded_http_body(response: Any, deadline: float) -> bytes:
    body = bytearray()
    while len(body) <= MAX_HTTP_BODY_BYTES:
        remaining = _remaining_timeout(deadline, 10)
        if remaining is None:
            raise TimeoutError("http-response-deadline-exceeded")
        _set_response_timeout(response, remaining)
        reader = getattr(response, "read1", response.read)
        chunk = reader(min(65_536, MAX_HTTP_BODY_BYTES + 1 - len(body)))
        if time.monotonic() >= deadline:
            raise TimeoutError("http-response-deadline-exceeded")
        if not chunk:
            return bytes(body)
        body.extend(chunk)
    raise ValueError("response-too-large")


class SmokeRuntime:
    """Runtime boundary kept injectable so failure/cleanup behavior is unit-testable."""

    def command(self, arguments: list[str], timeout: float) -> CommandResult:
        try:
            environment = os.environ.copy()
            package_root = getattr(self, "package_root", None)
            if isinstance(package_root, Path):
                environment["NUGET_PACKAGES"] = str(package_root)
            completed = subprocess.run(
                arguments,
                cwd=ROOT,
                env=environment,
                check=False,
                capture_output=True,
                text=True,
                timeout=timeout,
            )
            return CommandResult(completed.returncode, completed.stdout[-MAX_OUTPUT_CHARS:], completed.stderr[-MAX_OUTPUT_CHARS:])
        except (OSError, subprocess.TimeoutExpired) as exception:
            return CommandResult(124, "", type(exception).__name__)

    def source_graph_is_exact(self, project_references: list[Path]) -> bool:
        """Validate the restored project/assets graph selected by the AppHost build."""
        return _resolved_source_graph_is_exact(project_references)

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
        absolute_deadline = deadline if deadline is not None else time.monotonic() + timeout
        logical_url = url
        try:
            url = _credential_safe_url(
                url, token=token, form=form, deadline=absolute_deadline
            )
        except ValueError:
            return 0, {}, {}
        data: bytes | None = None
        headers = {"Accept": "application/json"}
        if url != logical_url:
            # Connect to the already resolved numeric loopback peer without changing
            # the logical authority Keycloak uses in the token issuer. EventStore is
            # configured against that original realm URL and must see the same issuer.
            headers["Host"] = parse.urlsplit(logical_url).netloc
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
        connect_timeout = _remaining_timeout(absolute_deadline, timeout)
        if connect_timeout is None:
            return 0, {}, {}
        opener = request.build_opener(
            request.ProxyHandler({}),
            request.HTTPSHandler(context=context),
            _RejectRedirectHandler(),
        )
        try:
            with opener.open(outbound, timeout=connect_timeout) as response:
                raw = _bounded_http_body(response, absolute_deadline)
                try:
                    document = json.loads(raw.decode("utf-8")) if raw else {}
                except (UnicodeDecodeError, json.JSONDecodeError):
                    document = {}
                return response.status, document if isinstance(document, dict) else {}, dict(response.headers.items())
        except error.HTTPError as exception:
            try:
                try:
                    raw = _bounded_http_body(exception, absolute_deadline)
                    try:
                        document = json.loads(raw.decode("utf-8")) if raw else {}
                    except (UnicodeDecodeError, json.JSONDecodeError):
                        document = {}
                    return exception.code, document if isinstance(document, dict) else {}, dict(exception.headers.items())
                except (TimeoutError, OSError, ValueError, IncompleteRead, HTTPException):
                    return 0, {}, {}
            finally:
                exception.close()
        except (error.URLError, TimeoutError, ssl.SSLError, OSError, IncompleteRead, HTTPException):
            return 0, {}, {}

    def signalr_negotiate_status(
        self,
        hub_url: str,
        token: str | None,
        timeout: float = 10,
        deadline: float | None = None,
    ) -> int:
        absolute_deadline = deadline if deadline is not None else time.monotonic() + timeout
        _require_loopback_sensitive_destination(
            hub_url, token=token, deadline=absolute_deadline
        )
        negotiate = f"{hub_url.rstrip('/')}/negotiate?negotiateVersion=1"
        status, document, _ = self.json_request(
            negotiate,
            method="POST",
            token=token,
            body={},
            timeout=timeout,
            deadline=absolute_deadline,
        )
        del document
        return status

    def signalr_connect(
        self,
        hub_url: str,
        token: str,
        timeout: float = 10,
        deadline: float | None = None,
    ) -> bool:
        absolute_deadline = deadline if deadline is not None else time.monotonic() + timeout
        _require_loopback_sensitive_destination(
            hub_url, token=token, deadline=absolute_deadline
        )
        negotiate = f"{hub_url.rstrip('/')}/negotiate?negotiateVersion=1"
        status, document, _ = self.json_request(
            negotiate, method="POST", token=token, body={}, timeout=timeout, deadline=absolute_deadline
        )
        connection_token = document.get("connectionToken")
        if not isinstance(connection_token, str) or not connection_token:
            connection_token = document.get("connectionId")
        transports = document.get("availableTransports", [])
        if (
            status != 200
            or not isinstance(connection_token, str)
            or not connection_token
            or not isinstance(transports, list)
            or not any(
            isinstance(item, dict) and item.get("transport") == "WebSockets" for item in transports
            )
        ):
            return False
        websocket_url = _websocket_url(
            hub_url, connection_token, token, deadline=absolute_deadline
        )
        remaining = _remaining_timeout(absolute_deadline, timeout)
        return (
            remaining is not None
            and _websocket_signalr_handshake(
                websocket_url, token, remaining, deadline=absolute_deadline
            )
        )

    def signalr_invalid_upgrade_status(
        self,
        hub_url: str,
        negotiation_token: str,
        invalid_token: str,
        timeout: float = 10,
        deadline: float | None = None,
    ) -> int:
        """Negotiate separately, then prove invalid bearer rejection on the WebSocket upgrade."""
        absolute_deadline = deadline if deadline is not None else time.monotonic() + timeout
        negotiate = f"{hub_url.rstrip('/')}/negotiate?negotiateVersion=1"
        status, document, _ = self.json_request(
            negotiate,
            method="POST",
            token=negotiation_token,
            body={},
            timeout=timeout,
            deadline=absolute_deadline,
        )
        connection_token = document.get("connectionToken") if isinstance(document, dict) else None
        if not isinstance(connection_token, str) or not connection_token:
            connection_token = document.get("connectionId") if isinstance(document, dict) else None
        transports = document.get("availableTransports") if isinstance(document, dict) else None
        if (
            status != 200
            or not isinstance(connection_token, str)
            or not connection_token
            or not isinstance(transports, list)
            or not any(
                isinstance(item, dict) and item.get("transport") == "WebSockets"
                for item in transports
            )
        ):
            return 0
        websocket_url = _websocket_url(
            hub_url, connection_token, invalid_token, deadline=absolute_deadline
        )
        remaining = _remaining_timeout(absolute_deadline, timeout)
        if remaining is None:
            return 0
        status_code, _ = _websocket_signalr_exchange(
            websocket_url,
            invalid_token,
            remaining,
            deadline=absolute_deadline,
            complete_handshake=False,
        )
        return status_code


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
    try:
        return json.loads(output)
    except json.JSONDecodeError:
        return None


def _logical_name(record: dict[str, Any]) -> str:
    for key in ("displayName", "name", "Name"):
        value = record.get(key)
        if isinstance(value, str) and value:
            return value
    return ""


def _metadata_string(record: dict[str, Any], *keys: str) -> str:
    for key in keys:
        value = record.get(key)
        if isinstance(value, str) and value:
            return value
    return ""


def _resource_parent(record: dict[str, Any]) -> str:
    direct = _metadata_string(record, "parent", "parentName", "parentResourceName")
    if direct:
        return direct
    relationships = record.get("relationships")
    if not isinstance(relationships, list):
        return ""
    for relationship in relationships:
        if not isinstance(relationship, dict):
            continue
        relationship_type = _metadata_string(relationship, "type", "relationshipType")
        if relationship_type.casefold() != "parent":
            continue
        parent = _metadata_string(
            relationship,
            "resourceName",
            "resource",
            "target",
            "targetName",
        )
        if parent:
            return parent
    return ""


def _is_generated_support_resource(
    record: dict[str, Any],
    primary_identifiers: set[str],
    referenced_by_primary: set[str],
) -> bool:
    resource_type = _metadata_string(
        record,
        "resourceType",
        "type",
        "kind",
        "resourceKind",
    )
    normalized_type = re.sub(r"[^a-z0-9]", "", resource_type.casefold())
    physical_name = _metadata_string(record, "name", "Name")
    source = _metadata_string(record, "source")
    parent = _resource_parent(record)
    dapr_support = (
        normalized_type == "executable"
        and source.casefold() == "dapr"
        and parent in primary_identifiers
    )
    parameter_support = (
        normalized_type == "parameter"
        and bool(physical_name)
        and source == f"Parameters:{physical_name}"
        and physical_name in referenced_by_primary
    )
    return dapr_support or parameter_support


def _primary_resource_names(records: list[dict[str, Any]]) -> list[str]:
    primary_records = [record for record in records if _logical_name(record) in REQUIRED_RESOURCES]
    primary_identifiers = {
        name
        for record in primary_records
        if (name := _metadata_string(record, "name", "Name"))
    }
    referenced_by_primary: set[str] = set()
    for record in primary_records:
        relationships = record.get("relationships")
        if not isinstance(relationships, list):
            continue
        for relationship in relationships:
            if not isinstance(relationship, dict):
                continue
            relationship_type = _metadata_string(relationship, "type", "relationshipType")
            target = _metadata_string(relationship, "resourceName", "resource", "target", "targetName")
            if relationship_type.casefold() == "reference" and target:
                referenced_by_primary.add(target)
    return [
        name
        for record in records
        if (name := _logical_name(record))
        and not _is_generated_support_resource(
            record,
            primary_identifiers,
            referenced_by_primary,
        )
    ]


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
                if not isinstance(url, str) or not _is_loopback_url(url):
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
            elif isinstance(nested, str) and _is_loopback_url(nested):
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


def _is_loopback_url(url: str) -> bool:
    try:
        parsed = parse.urlsplit(url)
        _ = parsed.port
        host = parsed.hostname
        if parsed.scheme not in {"http", "https"} or not host or parsed.username or parsed.password:
            return False
        return host.lower() == "localhost" or ipaddress.ip_address(host).is_loopback
    except ValueError:
        return False


def _require_loopback_sensitive_destination(
    url: str,
    *,
    token: str | None = None,
    form: dict[str, str] | None = None,
    deadline: float | None = None,
) -> None:
    carries_credentials = token is not None or form is not None
    if carries_credentials:
        _numeric_loopback_url(url, deadline=deadline)


def _numeric_loopback_url(url: str, *, deadline: float | None = None) -> str:
    """Resolve a local hostname before credentials exist and return a numeric peer URL."""
    try:
        parsed = parse.urlsplit(url)
        port = parsed.port or (443 if parsed.scheme == "https" else 80)
        host = parsed.hostname
        if (
            parsed.scheme not in {"http", "https"}
            or not host
            or parsed.username
            or parsed.password
        ):
            raise ValueError
        try:
            literal = ipaddress.ip_address(host)
            addresses = [literal]
        except ValueError:
            if host.casefold() != "localhost":
                raise ValueError
            absolute_deadline = deadline if deadline is not None else time.monotonic() + 10
            remaining = _remaining_timeout(absolute_deadline, 10)
            if remaining is None:
                raise TimeoutError
            results: queue.Queue[object] = queue.Queue(maxsize=1)

            def resolve() -> None:
                try:
                    results.put(socket.getaddrinfo(host, port, type=socket.SOCK_STREAM))
                except OSError as exception:
                    results.put(exception)

            threading.Thread(target=resolve, daemon=True).start()
            try:
                resolved = results.get(timeout=remaining)
            except queue.Empty:
                raise TimeoutError from None
            if isinstance(resolved, OSError):
                raise resolved
            addresses = []
            for item in resolved:
                address = ipaddress.ip_address(item[4][0])
                if address not in addresses:
                    addresses.append(address)
        if not addresses or any(not address.is_loopback for address in addresses):
            raise ValueError
        selected = next(
            (address for address in addresses if isinstance(address, ipaddress.IPv4Address)),
            addresses[0],
        )
        numeric_host = f"[{selected}]" if isinstance(selected, ipaddress.IPv6Address) else str(selected)
        netloc = f"{numeric_host}:{parsed.port}" if parsed.port is not None else numeric_host
        return parse.urlunsplit((parsed.scheme, netloc, parsed.path, parsed.query, parsed.fragment))
    except (OSError, TimeoutError, ValueError):
        raise ValueError("credentials-and-tokens-require-loopback: destination-not-verified-numeric") from None


def _credential_safe_url(
    url: str,
    *,
    token: str | None = None,
    form: dict[str, str] | None = None,
    deadline: float | None = None,
) -> str:
    return (
        _numeric_loopback_url(url, deadline=deadline)
        if token is not None or form is not None
        else url
    )


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
            if isinstance(url, str) and _is_loopback_url(url):
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
            if (
                isinstance(url, str)
                and _is_loopback_url(url)
            ):
                extras.extend(_loopback_variants(url.rstrip("/")))
    http_extras = [url for url in extras if url.startswith("http://") and url not in ordered]
    https_extras = [url for url in extras if url.startswith("https://") and url not in ordered]
    return ordered + http_extras + https_extras


def _websocket_url(
    hub_url: str,
    connection_token: str,
    token: str,
    *,
    deadline: float | None = None,
) -> str:
    parsed = parse.urlsplit(
        _credential_safe_url(hub_url, token=token, deadline=deadline)
    )
    scheme = "wss" if parsed.scheme == "https" else "ws"
    query = parse.urlencode({"id": connection_token, "access_token": token})
    return parse.urlunsplit((scheme, parsed.netloc, parsed.path, query, ""))


def _read_http_headers(stream: socket.socket, deadline: float) -> tuple[bytes, bytes]:
    data = bytearray()
    while b"\r\n\r\n" not in data and len(data) <= MAX_WEBSOCKET_HEADER_BYTES:
        remaining = _remaining_timeout(deadline, 10)
        if remaining is None:
            raise TimeoutError("websocket-header-deadline-exceeded")
        stream.settimeout(remaining)
        chunk = stream.recv(4_096)
        if time.monotonic() >= deadline:
            raise TimeoutError("websocket-header-deadline-exceeded")
        if not chunk:
            break
        data.extend(chunk)
        if len(data) > MAX_WEBSOCKET_HEADER_BYTES:
            raise ValueError("websocket-headers-too-large")
    boundary = data.find(b"\r\n\r\n")
    if boundary < 0:
        return bytes(data), b""
    end = boundary + 4
    return bytes(data[:end]), bytes(data[end:])


def _valid_websocket_upgrade(headers: bytes, key: str) -> bool:
    try:
        lines = headers.decode("iso-8859-1").split("\r\n")
    except UnicodeDecodeError:
        return False
    if not lines or re.fullmatch(r"HTTP/1\.1[ \t]+101(?:[ \t]+[^\r\n]*)?", lines[0]) is None:
        return False
    values: dict[str, list[str]] = {}
    for line in lines[1:]:
        if not line:
            continue
        if ":" not in line:
            return False
        name, value = line.split(":", 1)
        values.setdefault(name.strip().casefold(), []).append(value.strip())
    upgrade = ",".join(values.get("upgrade", [])).casefold()
    connection_tokens = {
        token.strip().casefold()
        for value in values.get("connection", [])
        for token in value.split(",")
    }
    expected_accept = base64.b64encode(
        hashlib.sha1((key + WEBSOCKET_ACCEPT_GUID).encode("ascii")).digest()
    ).decode("ascii")
    accepts = values.get("sec-websocket-accept", [])
    return upgrade == "websocket" and "upgrade" in connection_tokens and accepts == [expected_accept]


def _http_status(headers: bytes) -> int:
    try:
        status_line = headers.decode("iso-8859-1").split("\r\n", 1)[0]
    except UnicodeDecodeError:
        return 0
    match = re.fullmatch(r"HTTP/1\.1[ \t]+([0-9]{3})(?:[ \t]+[^\r\n]*)?", status_line)
    return int(match.group(1)) if match is not None else 0


def _read_websocket_frame(
    stream: socket.socket,
    buffer: bytearray,
    deadline: float,
    total_read: list[int],
) -> tuple[bool, int, bytes] | None:
    def require(count: int) -> bool:
        while len(buffer) < count:
            remaining = _remaining_timeout(deadline, 10)
            if remaining is None:
                return False
            stream.settimeout(remaining)
            chunk = stream.recv(min(65_536, MAX_WEBSOCKET_BYTES + 1 - total_read[0]))
            if time.monotonic() >= deadline:
                raise TimeoutError("websocket-frame-deadline-exceeded")
            if not chunk:
                return False
            total_read[0] += len(chunk)
            if total_read[0] > MAX_WEBSOCKET_BYTES:
                raise ValueError("websocket-response-too-large")
            buffer.extend(chunk)
        return True

    if not require(2):
        return None
    first, second = buffer[0], buffer[1]
    final = bool(first & 0x80)
    opcode = first & 0x0F
    if first & 0x70 or second & 0x80:
        raise ValueError("malformed-websocket-frame")
    length = second & 0x7F
    offset = 2
    if length == 126:
        if not require(4):
            return None
        length = int.from_bytes(buffer[2:4], "big")
        if length < 126:
            raise ValueError("malformed-websocket-frame")
        offset = 4
    elif length == 127:
        if not require(10):
            return None
        length = int.from_bytes(buffer[2:10], "big")
        if length < 65_536 or length >= 2**63:
            raise ValueError("malformed-websocket-frame")
        offset = 10
    if length > MAX_WEBSOCKET_BYTES or total_read[0] + max(0, length - len(buffer)) > MAX_WEBSOCKET_BYTES:
        raise ValueError("websocket-response-too-large")
    if not require(offset + length):
        return None
    payload = bytes(buffer[offset:offset + length])
    del buffer[:offset + length]
    return final, opcode, payload


def _read_signalr_handshake_ack(
    stream: socket.socket,
    initial: bytes,
    deadline: float,
) -> bool:
    buffer = bytearray(initial)
    total_read = [len(initial)]
    fragmented = bytearray()
    fragmented_opcode: int | None = None
    while time.monotonic() < deadline:
        frame = _read_websocket_frame(stream, buffer, deadline, total_read)
        if frame is None:
            return False
        final, opcode, payload = frame
        if opcode in (0x8, 0x9, 0xA):
            if not final or len(payload) > 125:
                return False
            if opcode == 0x8:
                return False
            continue
        if opcode == 0x1:
            if fragmented_opcode is not None:
                return False
            fragmented_opcode = opcode
            fragmented.extend(payload)
        elif opcode == 0x0 and fragmented_opcode == 0x1:
            fragmented.extend(payload)
        else:
            return False
        if len(fragmented) > MAX_WEBSOCKET_BYTES:
            return False
        if final:
            try:
                message = fragmented.decode("utf-8")
            except UnicodeDecodeError:
                return False
            return message == "{}\x1e"
    return False


def _masked_text_frame(payload: bytes) -> bytes:
    mask = secrets.token_bytes(4)
    length = len(payload)
    if length < 126:
        prefix = bytes((0x81, 0x80 | length))
    else:
        prefix = bytes((0x81, 0x80 | 126)) + length.to_bytes(2, "big")
    masked = bytes(value ^ mask[index % 4] for index, value in enumerate(payload))
    return prefix + mask + masked


def _websocket_signalr_exchange(
    websocket_url: str,
    token: str,
    timeout: float,
    *,
    deadline: float | None = None,
    complete_handshake: bool,
) -> tuple[int, bool]:
    parsed = parse.urlsplit(websocket_url)
    port = parsed.port or (443 if parsed.scheme == "wss" else 80)
    stream: socket.socket | None = None
    absolute_deadline = deadline if deadline is not None else time.monotonic() + timeout
    try:
        connect_timeout = _remaining_timeout(absolute_deadline, timeout)
        if connect_timeout is None:
            return 0, False
        stream = socket.create_connection((parsed.hostname or "", port), timeout=connect_timeout)
        if parsed.scheme == "wss":
            context = ssl._create_unverified_context()
            tls_timeout = _remaining_timeout(absolute_deadline, timeout)
            if tls_timeout is None:
                return 0, False
            stream.settimeout(tls_timeout)
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
        send_timeout = _remaining_timeout(absolute_deadline, timeout)
        if send_timeout is None:
            return 0, False
        stream.settimeout(send_timeout)
        stream.sendall(upgrade)
        headers, initial_frames = _read_http_headers(stream, absolute_deadline)
        status_code = _http_status(headers)
        if not complete_handshake:
            return status_code, False
        if not _valid_websocket_upgrade(headers, key):
            return status_code, False
        if initial_frames:
            return status_code, False
        send_timeout = _remaining_timeout(absolute_deadline, timeout)
        if send_timeout is None:
            return status_code, False
        stream.settimeout(send_timeout)
        stream.sendall(_masked_text_frame(b'{"protocol":"json","version":1}\x1e'))
        return status_code, _read_signalr_handshake_ack(
            stream, initial_frames, absolute_deadline
        )
    except (OSError, ssl.SSLError, UnicodeError, ValueError):
        return 0, False
    finally:
        if stream is not None:
            try:
                stream.close()
            except OSError:
                pass


def _websocket_signalr_handshake(
    websocket_url: str,
    token: str,
    timeout: float,
    *,
    deadline: float | None = None,
) -> bool:
    status, completed = _websocket_signalr_exchange(
        websocket_url,
        token,
        timeout,
        deadline=deadline,
        complete_handshake=True,
    )
    return status == 101 and completed


def _build_property_arguments() -> list[str]:
    arguments = [
        f"-p:{name}={'true' if value else 'false'}"
        for name, value in APPHOST_BUILD_PROPERTIES.items()
    ]
    arguments.append(
        "-p:HexalithPolymorphicSerializationsRoot="
        + str(ROOT / SOURCE_ROOT_PROPERTIES["HexalithPolymorphicSerializationsRoot"])
    )
    return arguments


def _evaluated_item_path(item: Any, project: Path = APPHOST) -> Path | None:
    if not isinstance(item, dict):
        return None
    value = item.get("FullPath") or item.get("HintPath") or item.get("Identity")
    if not isinstance(value, str) or not value:
        return None
    candidate = Path(value.replace("\\", os.sep))
    if not candidate.is_absolute():
        candidate = project.parent / candidate
    try:
        return candidate.resolve(strict=False)
    except (OSError, RuntimeError):
        return None


def _resolved_source_graph_is_exact(project_references: list[Path]) -> bool:
    """Traverse regenerated assets and reject package/shadow selection for source dependencies."""
    reachable_roots = {
        name: (ROOT / relative).resolve(strict=False)
        for relative in REACHABLE_SOURCE_GITLINKS
        for name in (Path(relative).name,)
    }
    guarded_names = tuple(
        Path(relative).name.casefold() for relative in SOURCE_DEPENDENCY_GITLINKS
    )
    allowed_roots = {
        (ROOT / "src").resolve(strict=False),
        (ROOT / "samples" / "Counter").resolve(strict=False),
        *reachable_roots.values(),
    }
    seen_roots: set[str] = set()
    pending = list(project_references)
    visited: set[Path] = set()
    while pending:
        project = pending.pop()
        try:
            project = project.resolve(strict=False)
        except (OSError, RuntimeError):
            return False
        if project in visited:
            continue
        visited.add(project)
        if runtime_evidence._path_has_symlink_component(project) or not project.is_file():
            return False
        if not any(project.is_relative_to(root) for root in allowed_roots):
            return False
        for name, root in reachable_roots.items():
            if project == root or project.is_relative_to(root):
                seen_roots.add(name)
        assets_path = project.parent / "obj" / "project.assets.json"
        if runtime_evidence._path_has_symlink_component(assets_path):
            return False
        try:
            assets = json.loads(assets_path.read_text(encoding="utf-8-sig"))
        except (OSError, json.JSONDecodeError, UnicodeDecodeError):
            return False
        libraries = assets.get("libraries") if isinstance(assets, dict) else None
        if not isinstance(libraries, dict):
            return False
        for identity, library in libraries.items():
            if not isinstance(identity, str) or not isinstance(library, dict):
                return False
            package_name = identity.split("/", 1)[0].casefold()
            matching_guarded_name = next(
                (name for name in guarded_names if package_name == name or package_name.startswith(f"{name}.")),
                None,
            )
            library_type = library.get("type")
            if matching_guarded_name is not None and library_type != "project":
                return False
            if library_type != "project":
                continue
            relative = library.get("msbuildProject") or library.get("path")
            if not isinstance(relative, str) or not relative:
                return False
            child = Path(relative.replace("\\", os.sep))
            if not child.is_absolute():
                child = project.parent / child
            pending.append(child)
    return set(reachable_roots) == seen_roots


def _discover_assets_graphs_from_json(start_project: Path) -> tuple[list[Path], list[Path]] | None:
    """Discover the restored project closure using only project.assets.json documents."""
    pending = [start_project]
    projects: set[Path] = set()
    assets_paths: set[Path] = set()
    while pending:
        candidate = pending.pop()
        try:
            project = candidate.resolve(strict=True)
        except (OSError, RuntimeError):
            return None
        if project in projects:
            continue
        if runtime_evidence._path_has_symlink_component(project) or not project.is_file():
            return None
        projects.add(project)
        assets_path = project.parent / "obj" / "project.assets.json"
        if runtime_evidence._path_has_symlink_component(assets_path) or not assets_path.is_file():
            return None
        try:
            assets = json.loads(assets_path.read_text(encoding="utf-8-sig"))
        except (OSError, UnicodeDecodeError, json.JSONDecodeError):
            return None
        libraries = assets.get("libraries") if isinstance(assets, dict) else None
        if not isinstance(libraries, dict):
            return None
        assets_paths.add(assets_path.resolve(strict=True))
        child_projects: list[str] = []
        for library in libraries.values():
            if not isinstance(library, dict) or library.get("type") != "project":
                continue
            relative = library.get("msbuildProject") or library.get("path")
            if not isinstance(relative, str) or not relative:
                return None
            child_projects.append(relative)
        project_metadata = assets.get("project")
        restore = project_metadata.get("restore") if isinstance(project_metadata, dict) else None
        frameworks = restore.get("frameworks") if isinstance(restore, dict) else None
        if frameworks is not None:
            if not isinstance(frameworks, dict):
                return None
            for framework in frameworks.values():
                references = (
                    framework.get("projectReferences")
                    if isinstance(framework, dict)
                    else None
                )
                if references is None:
                    continue
                if not isinstance(references, dict):
                    return None
                for reference in references.values():
                    project_path = (
                        reference.get("projectPath")
                        if isinstance(reference, dict)
                        else None
                    )
                    if not isinstance(project_path, str) or not project_path:
                        return None
                    child_projects.append(project_path)
        for relative in child_projects:
            child = Path(relative.replace("\\", os.sep))
            if not child.is_absolute():
                child = project.parent / child
            pending.append(child)
    return sorted(projects), sorted(assets_paths)


def _selected_dotnet_root(runtime: SmokeRuntime, deadline: float) -> Path | None:
    timeout = _remaining_timeout(deadline, 15)
    if timeout is None:
        return None
    result = runtime.command(["dotnet", "--list-sdks"], timeout)
    if result.returncode != 0:
        return None
    try:
        global_json = json.loads((ROOT / "global.json").read_text(encoding="utf-8-sig"))
        version = global_json["sdk"]["version"]
    except (OSError, KeyError, TypeError, UnicodeDecodeError, json.JSONDecodeError):
        return None
    matches = re.findall(r"^([^ \r\n]+) \[([^\]\r\n]+)\]$", result.stdout, re.MULTILINE)
    selected = next((Path(root) / item for item, root in matches if item == version), None)
    if selected is None or runtime_evidence._path_has_symlink_component(selected) or not selected.is_dir():
        return None
    return selected.parent.parent.resolve(strict=True)


def _bound_input(path: Path, package_root: Path, dotnet_root: Path) -> dict[str, str] | None:
    try:
        resolved = path.resolve(strict=True)
    except (OSError, RuntimeError):
        return None
    if runtime_evidence._path_has_symlink_component(path) or not resolved.is_file():
        return None
    for authority, root in (
        ("repository", ROOT.resolve(strict=True)),
        ("packages", package_root.resolve(strict=True)),
        ("dotnet", dotnet_root.resolve(strict=True)),
    ):
        if resolved.is_relative_to(root):
            return {
                "authority": authority,
                "path": resolved.relative_to(root).as_posix(),
                "sha256": _sha256(resolved),
            }
    return None


def _evaluate_source_graph(
    runtime: SmokeRuntime,
    deadline: float,
    *,
    expected_assets: list[Path] | None = None,
    package_root: Path | None = None,
    dotnet_root: Path | None = None,
) -> dict[str, Any] | None:
    timeout = _remaining_timeout(deadline, 60)
    if timeout is None:
        return None
    property_names = [*APPHOST_BUILD_PROPERTIES, *SOURCE_ROOT_PROPERTIES, "MSBuildAllProjects"]
    item_names = (
        "ProjectReference,PackageReference,Reference,ReferencePath,Analyzer,AdditionalFiles,"
        "Content,None,NativeCopyLocalItems,RuntimeCopyLocalItems"
    )
    result = runtime.command(
        [
            "dotnet",
            "msbuild",
            APPHOST_RELATIVE,
            "-p:Configuration=Debug",
            "-p:BuildProjectReferences=true",
            *_build_property_arguments(),
            "-target:ResolveReferences",
            "-getProperty:" + ",".join(property_names),
            "-getItem:" + item_names,
        ],
        timeout,
    )
    document = _json_from_output(result.stdout) if result.returncode == 0 else None
    properties = document.get("Properties") if isinstance(document, dict) else None
    items = document.get("Items") if isinstance(document, dict) else None
    if not isinstance(properties, dict) or not isinstance(items, dict):
        return None
    for name, expected in APPHOST_BUILD_PROPERTIES.items():
        actual = properties.get(name)
        if not isinstance(actual, str) or actual.casefold() != str(expected).casefold():
            return None
    for name, relative in SOURCE_ROOT_PROPERTIES.items():
        actual = properties.get(name)
        if not isinstance(actual, str) or not actual:
            return None
        try:
            if Path(actual).resolve() != (ROOT / relative).resolve():
                return None
        except (OSError, RuntimeError):
            return None
    project_items = items.get("ProjectReference")
    package_items = items.get("PackageReference")
    if not isinstance(project_items, list) or not isinstance(package_items, list):
        return None
    project_references = [_evaluated_item_path(item) for item in project_items]
    if not project_references or any(path is None for path in project_references):
        return None
    dependency_names = tuple(Path(relative).name.casefold() for relative in SOURCE_DEPENDENCY_GITLINKS)
    for item in package_items:
        if not isinstance(item, dict) or not isinstance(item.get("Identity"), str):
            return None
        package_name = item["Identity"].casefold()
        if any(package_name == name or package_name.startswith(f"{name}.") for name in dependency_names):
            return None
    typed_projects = [path for path in project_references if path is not None]
    if not runtime.source_graph_is_exact(typed_projects):
        return None
    discovered = _discover_assets_graphs_from_json(APPHOST)
    if discovered is None:
        return None
    discovered_projects, discovered_assets = discovered
    discovered_project_set = set(discovered_projects)
    if not set(typed_projects).issubset(discovered_project_set):
        return None
    if expected_assets is not None:
        expected_asset_set = {
            path.resolve(strict=False) for path in expected_assets
        }
        canonical_apphost_assets = (
            ROOT / runtime_evidence.APPHOST_PACKAGE_ASSETS_ROOT
        ).resolve(strict=False)
        if (
            not expected_asset_set
            or canonical_apphost_assets not in expected_asset_set
            or set(discovered_assets) != expected_asset_set
        ):
            return None
    if package_root is None or dotnet_root is None:
        return {
            "assetsGraphs": [path.relative_to(ROOT).as_posix() for path in discovered_assets],
            "inputs": [],
        }
    evaluations: list[tuple[Path, dict[str, Any]]] = [(APPHOST.resolve(), document)]
    for project in discovered_projects:
        if project == APPHOST.resolve():
            continue
        try:
            project_relative = project.relative_to(ROOT)
        except ValueError:
            return None
        evaluation_timeout = _remaining_timeout(deadline, 60)
        if evaluation_timeout is None:
            return None
        project_result = runtime.command(
            [
                "dotnet",
                "msbuild",
                str(project_relative),
                "-p:Configuration=Debug",
                "-p:BuildProjectReferences=true",
                *_build_property_arguments(),
                "-target:ResolveReferences",
                "-getProperty:MSBuildAllProjects",
                "-getItem:" + item_names,
            ],
            evaluation_timeout,
        )
        project_document = (
            _json_from_output(project_result.stdout)
            if project_result.returncode == 0
            else None
        )
        if not isinstance(project_document, dict):
            return None
        evaluations.append((project, project_document))

    input_paths: set[Path] = set(discovered_projects)
    evaluated_project_references: set[Path] = set()
    for project, evaluation in evaluations:
        evaluated_properties = evaluation.get("Properties")
        evaluated_items = evaluation.get("Items")
        if not isinstance(evaluated_properties, dict) or not isinstance(evaluated_items, dict):
            return None
        all_projects = evaluated_properties.get("MSBuildAllProjects")
        if not isinstance(all_projects, str) or not all_projects:
            return None
        for raw in all_projects.split(";"):
            if not raw:
                continue
            imported = Path(raw.replace("\\", os.sep))
            if not imported.is_absolute():
                imported = project.parent / imported
            input_paths.add(imported)
        evaluated_packages = evaluated_items.get("PackageReference")
        if not isinstance(evaluated_packages, list):
            return None
        for item in evaluated_packages:
            if not isinstance(item, dict) or not isinstance(item.get("Identity"), str):
                return None
            package_name = item["Identity"].casefold()
            if any(
                package_name == name or package_name.startswith(f"{name}.")
                for name in dependency_names
            ):
                return None
        evaluated_projects = evaluated_items.get("ProjectReference")
        if not isinstance(evaluated_projects, list):
            return None
        for item in evaluated_projects:
            project_reference = _evaluated_item_path(item, project)
            if project_reference is None or not project_reference.is_file():
                return None
            evaluated_project_references.add(project_reference)
        for item_name in (
            "ProjectReference", "Reference", "ReferencePath", "Analyzer", "AdditionalFiles", "Content", "None",
            "NativeCopyLocalItems", "RuntimeCopyLocalItems",
        ):
            values = evaluated_items.get(item_name, [])
            if not isinstance(values, list):
                return None
            for item in values:
                path = _evaluated_item_path(item, project)
                if path is not None and path.exists():
                    input_paths.add(path)
    if evaluated_project_references != discovered_project_set - {APPHOST.resolve()}:
        return None
    if not runtime.source_graph_is_exact(sorted(evaluated_project_references)):
        return None
    bound_inputs: list[dict[str, str]] = []
    for path in sorted(input_paths):
        binding = _bound_input(path, package_root, dotnet_root)
        if binding is None:
            return None
        bound_inputs.append(binding)
    bound_inputs.sort(key=lambda item: (item["authority"], item["path"]))
    return {
        "assetsGraphs": [path.relative_to(ROOT).as_posix() for path in discovered_assets],
        "inputs": bound_inputs,
    }


def _runtime_output_inventory() -> tuple[list[dict[str, Any]], bool]:
    output_root = APPHOST.parent / "bin" / "Debug"
    if runtime_evidence._path_has_symlink_component(output_root) or not output_root.is_dir():
        return [], False
    files: list[dict[str, Any]] = []
    apphost_deps_files = 0
    expected_deps_name = f"{APPHOST.stem}.deps.json"
    guarded = tuple(Path(value).name.casefold() for value in SOURCE_DEPENDENCY_GITLINKS)
    for path in sorted(output_root.rglob("*")):
        if path.is_symlink() or runtime_evidence._path_has_symlink_component(path):
            return [], False
        if path.is_dir():
            continue
        if not path.is_file():
            return [], False
        files.append(
            {
                "path": path.relative_to(output_root).as_posix(),
                "bytes": path.stat().st_size,
                "sha256": _sha256(path),
            }
        )
        if path.name == expected_deps_name:
            apphost_deps_files += 1
            try:
                deps = json.loads(path.read_text(encoding="utf-8-sig"))
            except (OSError, UnicodeDecodeError, json.JSONDecodeError):
                return [], False
            libraries = deps.get("libraries") if isinstance(deps, dict) else None
            if not isinstance(libraries, dict):
                return [], False
            for identity, library in libraries.items():
                package_name = str(identity).split("/", 1)[0].casefold()
                if (
                    any(package_name == name or package_name.startswith(f"{name}.") for name in guarded)
                    and (not isinstance(library, dict) or library.get("type") != "project")
                ):
                    return [], False
    return files, bool(files) and apphost_deps_files == 1


def _dapr_name_resolution_paths() -> dict[str, Path]:
    return {relative: ROOT / relative for relative in DAPR_NAME_RESOLUTION_RELATIVES}


def _base_evidence(runtime_manifest: dict[str, Any], timeout: int) -> dict[str, Any]:
    return {
        "schema": "hexalith.frontcomposer.pact-provider-reconciliation-apphost-smoke.v3",
        "capturedAt": datetime.now(timezone.utc).isoformat(),
        "executionStartedAt": None,
        "completedAt": None,
        "timeoutSeconds": timeout,
        "finalVerdict": "failed",
        "reasonCodes": [],
        "packageLedger": None,
        "identity": {
            "eventStoreSourceSha": _git(ROOT / "references/Hexalith.EventStore", "rev-parse", "HEAD"),
            "eventStoreReleaseVersion": _release_version(),
            "buildsCatalogSha": _git(ROOT / "references/Hexalith.Builds", "rev-parse", "HEAD"),
            "frontComposerRevision": runtime_manifest.get("capturedRevision", ""),
            "runtimeInputTreeSha256": runtime_manifest.get("treeSha256", ""),
            "runtimeInputCapturedAt": runtime_manifest.get("capturedAt", ""),
        },
        "topology": {
            "programPath": PROGRAM_RELATIVE,
            "programSha256": _sha256(PROGRAM),
            "projectPath": APPHOST_RELATIVE,
            "projectSha256": _sha256(APPHOST),
            "modifiedForSmoke": False,
            "declaredResources": list(REQUIRED_RESOURCES),
        },
        "startup": {
            "result": "failed",
            "hostStartAttempted": False,
            "hostStarted": False,
            "outputPreparation": {
                "clean": "not-observed",
                "restore": "not-observed",
                "build": "not-observed",
                "configuration": "Debug",
                "restoreMode": "forced-no-cache",
                "buildMode": "no-incremental",
                "startMode": "no-build",
                "evaluatedBuildProperties": APPHOST_BUILD_PROPERTIES,
                "sourceDependencyGitlinks": list(SOURCE_DEPENDENCY_GITLINKS),
                "buildControlGitlinks": list(BUILD_CONTROL_GITLINKS),
                "reachableSourceGitlinks": list(REACHABLE_SOURCE_GITLINKS),
                "inactiveGuardedGitlinks": list(INACTIVE_GUARDED_GITLINKS),
                "evaluatedSourceGraph": "not-observed",
                "evaluatedInputBinding": None,
                "runtimeOutputBinding": None,
            },
            "resourceWaits": {name: "not-observed" for name in REQUIRED_RESOURCES},
        },
        "observations": {
            name: {"result": "not-observed", "authenticated": False, "reasonCode": "runtime.not-reached"}
            for name in OBSERVATIONS
        },
        "authorizationControls": {
            name: {
                "result": "not-observed",
                "credential": "invalid-bearer",
                "reasonCode": "runtime.not-reached",
                "statusCode": None,
            }
            for name in AUTHORIZATION_CONTROLS
        },
        "cleanup": {
            "command": STOP_COMMAND,
            "result": "failed",
            "hostStopped": False,
            "portsClosed": False,
            "runningAppHostsAfterAttempt": 1,
            "listenerConfirmation": "not-confirmed",
            "confirmation": "not-confirmed",
            "daprNameResolutionFiles": {
                "absentBeforeRun": [],
                "createdByInvocation": [],
                "removedAfterShutdown": [],
                "remainingAfterCleanup": [],
            },
            "runtimeInputsCleanAfterRun": False,
        },
    }


def _remaining_timeout(deadline: float, maximum: float) -> float | None:
    remaining = deadline - time.monotonic()
    if remaining <= 0:
        return None
    return min(maximum, remaining)


def _bounded_sleep(deadline: float, seconds: float = 1.0) -> None:
    remaining = deadline - time.monotonic()
    if remaining > 0:
        time.sleep(min(seconds, remaining))


def _clip(text: str, limit: int = 4000) -> str:
    if len(text) <= limit:
        return text
    return text[-limit:]


def _query_provenance(headers: dict[str, str], document: dict[str, Any]) -> str:
    header = next((value for key, value in headers.items() if key.lower() == "x-hexalith-query-provenance"), "")
    metadata = document.get("metadata")
    metadata_value = ""
    if isinstance(metadata, dict):
        value = metadata.get("provenance")
        if isinstance(value, str):
            metadata_value = value
    if header and metadata_value and header != metadata_value:
        return ""
    return header or metadata_value


def _query_tenant_id(document: dict[str, Any]) -> str:
    payload: Any = document.get("payload")
    if isinstance(payload, str):
        try:
            payload = json.loads(payload)
        except json.JSONDecodeError:
            return ""
    if not isinstance(payload, dict):
        return ""
    for key, value in payload.items():
        if isinstance(key, str) and key.casefold() == "tenantid" and isinstance(value, str):
            return value
    return ""


def _atomic_write(path: Path, document: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(
        dir=path.parent,
        prefix=f".{path.name}.",
        suffix=".tmp",
    )
    temporary = Path(temporary_name)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="\n") as stream:
            stream.write(json.dumps(document, indent=2) + "\n")
        temporary.replace(path)
    except BaseException:
        temporary.unlink(missing_ok=True)
        raise


def _describe_host(
    runtime: SmokeRuntime,
    timeout: float = 15,
) -> tuple[bool, list[dict[str, Any]]]:
    described = runtime.command(
        ["aspire", "describe", "--apphost", APPHOST_RELATIVE, "--format", "Json", "--non-interactive", "--nologo"],
        timeout,
    )
    parsed = _json_from_output(described.stdout) if described.returncode == 0 else None
    if described.returncode != 0 or parsed is None:
        return False, []
    return True, _resource_records(parsed)


def _running_apphost_count(runtime: SmokeRuntime, timeout: float) -> int | None:
    listed = runtime.command(
        ["aspire", "ps", "--format", "Json", "--non-interactive", "--nologo"],
        timeout,
    )
    if listed.returncode != 0:
        return None
    parsed = _json_from_output(listed.stdout)
    if not isinstance(parsed, list) or any(not isinstance(item, dict) for item in parsed):
        return None
    target = (ROOT / APPHOST_RELATIVE).resolve()
    running = 0
    for item in parsed:
        apphost_path = item.get("appHostPath")
        if not isinstance(apphost_path, str) or not apphost_path:
            return None
        try:
            candidate = Path(apphost_path).resolve()
        except OSError:
            return None
        if candidate == target:
            running += 1
    return running


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


def _wait_until_host_absent_or_ports_closed(
    runtime: SmokeRuntime,
    urls: list[str],
    timeout: float = 15,
) -> bool:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        describe_timeout = _remaining_timeout(deadline, 15)
        if describe_timeout is None:
            return False
        present, records = _describe_host(runtime, describe_timeout)
        if not present:
            ps_timeout = _remaining_timeout(deadline, 10)
            running = _running_apphost_count(runtime, ps_timeout) if ps_timeout is not None else None
            if running == 0:
                return True
            _bounded_sleep(deadline, 0.5)
            continue
        observed = [url for url in urls if url] or _probed_resource_urls(records)
        ports_closed = bool(observed)
        for url in observed:
            port_timeout = _remaining_timeout(deadline, 1)
            if port_timeout is None:
                return False
            if _port_open(url, port_timeout):
                ports_closed = False
                break
        if ports_closed:
            ps_timeout = _remaining_timeout(deadline, 10)
            running = _running_apphost_count(runtime, ps_timeout) if ps_timeout is not None else None
            if running == 0:
                return True
        _bounded_sleep(deadline, 0.5)
    return False


def _capture(
    output: Path,
    runtime: SmokeRuntime,
    timeout: int,
    runtime_input_manifest_path: Path | None = None,
    package_root: Path | None = None,
) -> int:
    runtime_input_issues: list[str] = []
    runtime_manifest_bytes: bytes | None = None
    runtime_manifest_captured_at: datetime | None = None
    if runtime_input_manifest_path is None:
        runtime_manifest, runtime_input_issues = runtime_evidence.runtime_input_manifest(ROOT)
    else:
        runtime_manifest_bytes = runtime_evidence._bounded_read(
            runtime_input_manifest_path,
            runtime_input_issues,
            "pre-provider runtime-input manifest",
        )
        runtime_manifest, runtime_manifest_captured_at = runtime_evidence._validate_runtime_input_manifest(
            runtime_input_manifest_path,
            ROOT,
            runtime_input_issues,
        )
    if package_root is None:
        runtime_input_issues.append("A fresh external NuGet package root is required.")
        package_root = ROOT
    else:
        runtime_input_issues.extend(
            runtime_evidence.validate_fresh_package_root(ROOT, package_root)
        )
    runtime.package_root = package_root
    evidence = _base_evidence(runtime_manifest, timeout)
    if runtime_input_manifest_path is not None:
        apphost_captured_at = runtime_evidence._parse_timestamp(
            evidence["capturedAt"],
            "AppHost smoke capturedAt",
            runtime_input_issues,
        )
        if (
            runtime_manifest_captured_at is None
            or apphost_captured_at is None
            or runtime_manifest_captured_at >= apphost_captured_at
        ):
            runtime_input_issues.append(
                "Pre-provider runtime-input manifest does not predate AppHost capture."
            )
    initial_runtime_tree = runtime_manifest.get("treeSha256")
    dapr_paths = _dapr_name_resolution_paths()
    absent_before_run = sorted(
        relative for relative, path in dapr_paths.items() if not path.exists()
    )
    evidence["cleanup"]["daprNameResolutionFiles"]["absentBeforeRun"] = absent_before_run
    endpoints: dict[str, str] = {}
    probed_urls: list[str] = []
    assets_paths: list[Path] = []
    initial_package_ledger: dict[str, Any] | None = None
    initial_runtime_outputs: list[dict[str, Any]] | None = None
    reason_codes: list[str] = evidence["reasonCodes"]
    overall_deadline = time.monotonic() + timeout
    cleanup_reserve = min(90.0, max(15.0, timeout * 0.3))
    capture_deadline = overall_deadline - cleanup_reserve
    host_start_attempted = False
    host_started = False
    cold_stop_confirmed = False
    if runtime_input_issues:
        reason_codes.append("runtime-inputs.not-clean-or-complete")
        evidence["completedAt"] = datetime.now(timezone.utc).isoformat()
        _atomic_write(output, evidence)
        return 1
    try:
        # `aspire start --format Json` restarts a running AppHost. That restart launches
        # frontcomposer-ui with `dotnet run --no-build` against a half-stopped process tree
        # and the resource exits before wait. Stop first so capture is always a cold start.
        # `--isolated` keeps the CLI off shared local Aspire state on GitHub-hosted runners.
        # Debug/source references stay the default: Release package mode omits Parties/Tenants
        # UI assemblies (FrontComposerUiUsePublishedModulePackages defaults false) and the
        # AppHost then fails with CS0234. A serialized Debug prebuild plus `--no-build` avoids
        # the parallel pack file-lock on Hexalith.Commons nupkgs that fails `aspire start` in CI.
        initial_stop_timeout = _remaining_timeout(capture_deadline, 60)
        if initial_stop_timeout is None:
            reason_codes.append("apphost.capture.deadline-exceeded")
            return 1
        initial_stop = runtime.command(
            ["aspire", "stop", "--apphost", APPHOST_RELATIVE, "--non-interactive", "--nologo"],
            initial_stop_timeout,
        )
        if initial_stop.returncode != 0:
            reason_codes.append("apphost.cold-stop.failed")
            return 1
        wait_timeout = _remaining_timeout(capture_deadline, 15)
        if wait_timeout is None:
            reason_codes.append("apphost.capture.deadline-exceeded")
            return 1
        cold_stop_confirmed = _wait_until_host_absent_or_ports_closed(runtime, [], wait_timeout)
        if not cold_stop_confirmed:
            reason_codes.append("apphost.cold-stop.not-confirmed")
            return 1
        clean_timeout = _remaining_timeout(capture_deadline, timeout)
        if clean_timeout is None:
            reason_codes.append("apphost.capture.deadline-exceeded")
            return 1
        clean = runtime.command(
            [
                "dotnet",
                "clean",
                APPHOST_RELATIVE,
                "--configuration",
                "Debug",
                "-m:1",
                *_build_property_arguments(),
            ],
            clean_timeout,
        )
        evidence["startup"]["outputPreparation"]["clean"] = (
            "passed" if clean.returncode == 0 else "failed"
        )
        if clean.returncode != 0:
            reason_codes.append("apphost.output-clean.failed")
            return 1
        restore_timeout = _remaining_timeout(capture_deadline, timeout)
        if restore_timeout is None:
            reason_codes.append("apphost.capture.deadline-exceeded")
            return 1
        restore = runtime.command(
            [
                "dotnet",
                "restore",
                APPHOST_RELATIVE,
                "-p:Configuration=Debug",
                "--force",
                "--force-evaluate",
                "--no-cache",
                "--disable-parallel",
                *_build_property_arguments(),
            ],
            restore_timeout,
        )
        evidence["startup"]["outputPreparation"]["restore"] = (
            "passed" if restore.returncode == 0 else "failed"
        )
        if restore.returncode != 0:
            reason_codes.append("apphost.output-restore.failed")
            return 1
        discovered = _discover_assets_graphs_from_json(APPHOST)
        if discovered is None:
            reason_codes.append("apphost.assets-graph.not-closed")
            return 1
        _, assets_paths = discovered
        canonical_apphost_assets = (
            ROOT / runtime_evidence.APPHOST_PACKAGE_ASSETS_ROOT
        ).resolve(strict=False)
        resolved_assets_paths = {
            path.resolve(strict=False) for path in assets_paths
        }
        if not resolved_assets_paths or canonical_apphost_assets not in resolved_assets_paths:
            reason_codes.append("apphost.assets-graph.not-closed")
            return 1
        package_ledger, package_issues = runtime_evidence.resolved_package_ledger(
            ROOT,
            package_root,
            assets_paths,
        )
        package_issues = list(package_issues)
        runtime_evidence.validate_package_ledger_semantics(
            package_ledger, package_issues
        )
        ledger_graphs = (
            package_ledger.get("assetsGraphs")
            if isinstance(package_ledger, dict)
            else None
        )
        ledger_paths = (
            [item.get("path") for item in ledger_graphs if isinstance(item, dict)]
            if isinstance(ledger_graphs, list)
            else []
        )
        try:
            expected_ledger_paths = sorted(
                path.relative_to(ROOT).as_posix() for path in resolved_assets_paths
            )
        except ValueError:
            expected_ledger_paths = []
            package_issues.append(
                "AppHost assets graph set is outside the repository."
            )
        if (
            ledger_paths != expected_ledger_paths
            or runtime_evidence.APPHOST_PACKAGE_ASSETS_ROOT not in ledger_paths
        ):
            package_issues.append(
                "AppHost package ledger does not bind its exact canonical assets graph set."
            )
        if package_issues:
            reason_codes.append("apphost.package-authority.not-sealed")
            return 1
        evidence["packageLedger"] = package_ledger
        initial_package_ledger = package_ledger
        dotnet_root = _selected_dotnet_root(runtime, capture_deadline)
        if dotnet_root is None:
            reason_codes.append("apphost.sdk-authority.not-selected")
            return 1
        evidence["executionStartedAt"] = datetime.now(timezone.utc).isoformat()
        source_graph_before = _evaluate_source_graph(
            runtime,
            capture_deadline,
            expected_assets=assets_paths,
            package_root=package_root,
            dotnet_root=dotnet_root,
        )
        evidence["startup"]["outputPreparation"]["evaluatedSourceGraph"] = (
            "passed" if source_graph_before is not None else "failed"
        )
        evidence["startup"]["outputPreparation"]["evaluatedInputBinding"] = source_graph_before
        if source_graph_before is None:
            reason_codes.append("apphost.source-graph.not-exact")
            return 1
        prebuild_timeout = _remaining_timeout(capture_deadline, timeout)
        if prebuild_timeout is None:
            reason_codes.append("apphost.capture.deadline-exceeded")
            return 1
        prebuild = runtime.command(
            [
                "dotnet",
                "build",
                APPHOST_RELATIVE,
                "--configuration",
                "Debug",
                "--no-restore",
                "--no-incremental",
                "-m:1",
                *_build_property_arguments(),
            ],
            prebuild_timeout,
        )
        evidence["startup"]["outputPreparation"]["build"] = (
            "passed" if prebuild.returncode == 0 else "failed"
        )
        if prebuild.returncode != 0:
            reason_codes.append("apphost.output-build.failed")
            evidence["startup"]["startReturnCode"] = prebuild.returncode
            evidence["startup"]["startStdout"] = _clip(prebuild.stdout)
            evidence["startup"]["startStderr"] = _clip(prebuild.stderr)
            return 1
        source_graph_after = _evaluate_source_graph(
            runtime,
            capture_deadline,
            expected_assets=assets_paths,
            package_root=package_root,
            dotnet_root=dotnet_root,
        )
        if source_graph_after is None or not runtime_evidence._exact(
            source_graph_after, source_graph_before
        ):
            evidence["startup"]["outputPreparation"]["evaluatedSourceGraph"] = "failed"
            reason_codes.append("apphost.source-graph.not-exact")
            return 1
        runtime_outputs, outputs_valid = _runtime_output_inventory()
        evidence["startup"]["outputPreparation"]["runtimeOutputBinding"] = runtime_outputs
        if not outputs_valid:
            reason_codes.append("apphost.runtime-output.not-closed")
            return 1
        initial_runtime_outputs = runtime_outputs
        start_timeout = _remaining_timeout(capture_deadline, timeout)
        if start_timeout is None:
            reason_codes.append("apphost.capture.deadline-exceeded")
            return 1
        host_start_attempted = True
        evidence["startup"]["hostStartAttempted"] = True
        start = runtime.command([*START_COMMAND, "--no-build"], start_timeout)
        if start.returncode != 0:
            reason_codes.append("apphost.start.failed")
            evidence["startup"]["startReturnCode"] = start.returncode
            evidence["startup"]["startStdout"] = _clip(start.stdout)
            evidence["startup"]["startStderr"] = _clip(start.stderr)
            return 1
        host_started = True
        evidence["startup"]["hostStarted"] = True
        for resource in REQUIRED_RESOURCES:
            remaining = _remaining_timeout(capture_deadline, 120)
            wait_seconds = int(remaining) if remaining is not None else 0
            if wait_seconds < 1:
                evidence["startup"]["resourceWaits"][resource] = "failed"
                reason_codes.append("apphost.capture.deadline-exceeded")
                break
            waited = runtime.command(
                ["aspire", "wait", resource, "--status", "healthy", "--timeout", str(wait_seconds), "--apphost", APPHOST_RELATIVE, "--non-interactive", "--nologo"],
                remaining,
            )
            evidence["startup"]["resourceWaits"][resource] = "healthy" if waited.returncode == 0 else "failed"
            if waited.returncode != 0:
                reason_codes.append(f"resource.{resource}.not-healthy")
        if reason_codes:
            return 1
        describe_timeout = _remaining_timeout(capture_deadline, 30)
        if describe_timeout is None:
            reason_codes.append("apphost.capture.deadline-exceeded")
            return 1
        described = runtime.command(
            ["aspire", "describe", "--apphost", APPHOST_RELATIVE, "--format", "Json", "--non-interactive", "--nologo"],
            describe_timeout,
        )
        records = _resource_records(_json_from_output(described.stdout)) if described.returncode == 0 else []
        names = _primary_resource_names(records)
        evidence["topology"]["declaredResources"] = names
        if len(names) != len(REQUIRED_RESOURCES) or set(names) != set(REQUIRED_RESOURCES):
            reason_codes.append("apphost.describe.incomplete")
            return 1
        endpoints = {name: _resource_endpoint(records, name) for name in REQUIRED_RESOURCES}
        # Aspire can advertise a DCP public proxy that accepts and then hangs while its
        # direct target is a real loopback endpoint. The target is safe for local capture
        # and already required for SignalR, so REST probes use the same bounded candidates.
        eventstore_bases = _resource_signalr_urls(records, "eventstore")
        security_bases = _resource_public_urls(records, "security")
        probed_urls = _probed_resource_urls(records)
        if not endpoints["security"] or not endpoints["eventstore"] or not endpoints["tenants"]:
            reason_codes.append("apphost.endpoints.incomplete")
            return 1
        evidence["startup"]["result"] = "passed"

        token = None
        token_status = 0
        token_deadline = min(capture_deadline, time.monotonic() + 30)
        while time.monotonic() < token_deadline:
            for security_base in security_bases or [endpoints["security"]]:
                request_timeout = _remaining_timeout(token_deadline, 15)
                if request_timeout is None:
                    break
                token_status, token_document, _ = runtime.json_request(
                    f"{security_base}/realms/hexalith/protocol/openid-connect/token",
                    method="POST",
                    form={
                        "grant_type": "password",
                        "client_id": "hexalith-eventstore",
                        "username": "admin-user",
                        "password": "admin-pass",
                    },
                    timeout=request_timeout,
                    deadline=token_deadline,
                )
                token = token_document.get("access_token")
                if token_status == 200 and isinstance(token, str) and token:
                    break
            if token_status == 200 and isinstance(token, str) and token:
                break
            _bounded_sleep(token_deadline)
        if token_status != 200 or not isinstance(token, str) or not token:
            reason_codes.append("auth.local-identity.unavailable")
            return 1

        health_status = 0
        eventstore_base = eventstore_bases[0] if eventstore_bases else endpoints["eventstore"]
        # `aspire wait eventstore` can observe the Dapr sidecar as healthy before the
        # application has finished its bounded operational-metadata discovery. Health is
        # readiness evidence only; protected runtime surfaces are authenticated separately.
        health_deadline = min(
            capture_deadline,
            time.monotonic() + min(180, max(30, timeout - 30)),
        )
        while time.monotonic() < health_deadline and health_status not in (200, 204):
            for base in eventstore_bases or [endpoints["eventstore"]]:
                request_timeout = _remaining_timeout(health_deadline, 5)
                if request_timeout is None:
                    break
                health_status, _, _ = runtime.json_request(
                    f"{base}/health", timeout=request_timeout, deadline=health_deadline
                )
                if health_status in (200, 204):
                    eventstore_base = base
                    break
            if health_status not in (200, 204):
                _bounded_sleep(health_deadline)
        evidence["observations"]["health"] = {
            "result": "passed" if health_status in (200, 204) else "failed",
            "authenticated": False,
            "reasonCode": "health.readiness.succeeded" if health_status in (200, 204) else "health.readiness.failed",
            "statusCode": health_status,
        }

        if health_status not in (200, 204):
            reason_codes.append("health.readiness.failed")
            return 1

        message_id = _ulid()
        tenant_id = f"pact-reconciliation-{message_id.lower()}"
        command_body = {
            "messageId": message_id,
            "tenant": "system",
            "domain": "tenants",
            "aggregateId": tenant_id,
            "commandType": "CreateTenant",
            "payload": {"TenantId": tenant_id, "Name": "Pact Reconciliation", "Description": "Bounded local smoke"},
        }
        command_control_deadline = min(capture_deadline, time.monotonic() + 10)
        control_timeout = _remaining_timeout(command_control_deadline, 10)
        if control_timeout is None:
            reason_codes.append("apphost.capture.deadline-exceeded")
            return 1
        invalid_command_status, _, _ = runtime.json_request(
            f"{eventstore_base}/api/v1/commands",
            method="POST",
            token="invalid-local-evidence-token",
            body=command_body,
            timeout=control_timeout,
            deadline=command_control_deadline,
        )
        command_control_passed = invalid_command_status in (401, 403)
        evidence["authorizationControls"]["commandSubmit"] = {
            "result": "passed" if command_control_passed else "failed",
            "credential": "invalid-bearer",
            "reasonCode": (
                "authorization.invalid-bearer.rejected"
                if command_control_passed
                else "authorization.invalid-bearer.not-rejected"
            ),
            "statusCode": invalid_command_status,
        }
        if not command_control_passed:
            reason_codes.append("authorization.command-submit.not-enforced")
            return 1

        submit_deadline = min(capture_deadline, time.monotonic() + 30)
        submit_timeout = _remaining_timeout(submit_deadline, 30)
        if submit_timeout is None:
            reason_codes.append("apphost.capture.deadline-exceeded")
            return 1
        submit_status, submit_document, _ = runtime.json_request(
            f"{eventstore_base}/api/v1/commands",
            method="POST",
            token=token,
            body=command_body,
            timeout=submit_timeout,
            deadline=submit_deadline,
        )
        correlation = submit_document.get("correlationId")
        submit_passed = submit_status == 202 and correlation == message_id
        evidence["observations"]["commandSubmit"] = {
            "result": "passed" if submit_passed else "failed",
            "authenticated": command_control_passed,
            "reasonCode": "command.accepted" if submit_passed else "command.not-accepted",
            "statusCode": submit_status,
            "aggregateId": tenant_id,
            "messageId": message_id,
            "correlationId": correlation if isinstance(correlation, str) else "not-observed",
        }
        terminal = ""
        status_control_passed = False
        if submit_passed:
            status_control_deadline = min(capture_deadline, time.monotonic() + 10)
            control_timeout = _remaining_timeout(status_control_deadline, 10)
            if control_timeout is not None:
                invalid_status, _, _ = runtime.json_request(
                    f"{eventstore_base}/api/v1/commands/status/{parse.quote(correlation, safe='')}",
                    token="invalid-local-evidence-token",
                    timeout=control_timeout,
                    deadline=status_control_deadline,
                )
                status_control_passed = invalid_status in (401, 403)
                evidence["authorizationControls"]["commandStatus"] = {
                    "result": "passed" if status_control_passed else "failed",
                    "credential": "invalid-bearer",
                    "reasonCode": (
                        "authorization.invalid-bearer.rejected"
                        if status_control_passed
                        else "authorization.invalid-bearer.not-rejected"
                    ),
                    "statusCode": invalid_status,
                }
            if not status_control_passed:
                reason_codes.append("authorization.command-status.not-enforced")
                return 1
            status_deadline = min(capture_deadline, time.monotonic() + 60)
            while time.monotonic() < status_deadline:
                status_timeout = _remaining_timeout(status_deadline, 10)
                if status_timeout is None:
                    break
                status_code, status_document, _ = runtime.json_request(
                    f"{eventstore_base}/api/v1/commands/status/{parse.quote(correlation, safe='')}",
                    token=token,
                    timeout=status_timeout,
                    deadline=status_deadline,
                )
                terminal = str(status_document.get("status", "")) if status_code == 200 else ""
                if terminal in ("Completed", "Rejected", "PublishFailed", "TimedOut"):
                    break
                _bounded_sleep(status_deadline)
        status_passed = terminal == "Completed"
        evidence["observations"]["commandStatus"] = {
            "result": "passed" if status_passed else "failed",
            "authenticated": status_control_passed,
            "reasonCode": "command.completed" if status_passed else "command.not-completed",
            "terminalStatus": terminal or "not-observed",
            "aggregateId": tenant_id,
        }

        query_status = 0
        provenance = ""
        response_tenant_id = ""
        query_body = {
            "tenant": "system",
            "domain": "tenants",
            "aggregateId": tenant_id,
            "queryType": "get-tenant",
            "projectionType": "tenants",
            "entityId": tenant_id,
        }
        query_control_passed = False
        query_control_deadline = min(capture_deadline, time.monotonic() + 10)
        control_timeout = _remaining_timeout(query_control_deadline, 10)
        if control_timeout is not None:
            invalid_query_status, _, _ = runtime.json_request(
                f"{eventstore_base}/api/v1/queries",
                method="POST",
                token="invalid-local-evidence-token",
                body=query_body,
                timeout=control_timeout,
                deadline=query_control_deadline,
            )
            query_control_passed = invalid_query_status in (401, 403)
            evidence["authorizationControls"]["queryProvenance"] = {
                "result": "passed" if query_control_passed else "failed",
                "credential": "invalid-bearer",
                "reasonCode": (
                    "authorization.invalid-bearer.rejected"
                    if query_control_passed
                    else "authorization.invalid-bearer.not-rejected"
                ),
                "statusCode": invalid_query_status,
            }
        if not query_control_passed:
            reason_codes.append("authorization.query.not-enforced")
            return 1
        query_deadline = min(capture_deadline, time.monotonic() + 60)
        while time.monotonic() < query_deadline:
            # list-tenants + projectionType "tenants" is a mismatched route (list-tenants uses
            # tenant-index). Query the tenant CreateTenant just completed, matching Tenants
            # AspireTopologyTests: get-tenant / projectionType tenants / entityId = aggregateId.
            query_timeout = _remaining_timeout(query_deadline, 30)
            if query_timeout is None:
                break
            query_status, query_document, query_headers = runtime.json_request(
                f"{eventstore_base}/api/v1/queries",
                method="POST",
                token=token,
                body=query_body,
                timeout=query_timeout,
                deadline=query_deadline,
            )
            provenance = _query_provenance(query_headers, query_document)
            response_tenant_id = _query_tenant_id(query_document)
            # This exact tenant handler route is stamped HandlerComputed. ProjectionBacked is a
            # different execution path and must not satisfy this observation.
            if (
                query_status == 200
                and provenance == "HandlerComputed"
                and response_tenant_id == tenant_id
            ):
                break
            _bounded_sleep(query_deadline)
        query_passed = (
            query_status == 200
            and provenance == "HandlerComputed"
            and response_tenant_id == tenant_id
        )
        if query_passed:
            query_reason = "query.handler-computed"
        elif query_status == 200 and provenance == "HandlerComputed":
            query_reason = "query.tenant-mismatch"
        else:
            query_reason = "query.provenance.missing"
        evidence["observations"]["queryProvenance"] = {
            "result": "passed" if query_passed else "failed",
            "authenticated": query_control_passed,
            "reasonCode": query_reason,
            "statusCode": query_status,
            "provenance": provenance or "not-observed",
            "tenant": "system",
            "aggregateId": tenant_id,
            "entityId": tenant_id,
            "responseTenantId": response_tenant_id or "not-observed",
        }

        signalr_bases = _resource_signalr_urls(records, "eventstore") or eventstore_bases or [eventstore_base]
        signalr_control_passed = False
        signalr_control_status = 0
        signalr_passed = False
        signalr_endpoint = ""
        signalr_deadline = min(capture_deadline, time.monotonic() + 30)
        for base in signalr_bases:
            candidate_endpoint = f"{base}/hubs/projection-changes"
            control_timeout = _remaining_timeout(signalr_deadline, 5)
            if control_timeout is None:
                break
            signalr_control_status = runtime.signalr_invalid_upgrade_status(
                candidate_endpoint,
                token,
                "invalid-local-evidence-token",
                timeout=control_timeout,
                deadline=signalr_deadline,
            )
            if signalr_control_status in (401, 403):
                signalr_control_passed = True
                signalr_endpoint = candidate_endpoint
                signalr_timeout = _remaining_timeout(signalr_deadline, 5)
                if signalr_timeout is not None and runtime.signalr_connect(
                    candidate_endpoint,
                    token,
                    timeout=signalr_timeout,
                    deadline=signalr_deadline,
                ):
                    signalr_passed = True
                    break
        evidence["authorizationControls"]["projectionSignalR"] = {
            "result": "passed" if signalr_control_passed else "failed",
            "credential": "invalid-bearer",
            "reasonCode": (
                "authorization.invalid-bearer.rejected"
                if signalr_control_passed
                else "authorization.invalid-bearer.not-rejected"
            ),
            "statusCode": signalr_control_status,
            "endpoint": signalr_endpoint,
            "transport": "websocket-upgrade",
            "negotiatedWith": "valid-bearer",
            "upgradeResult": "rejected-before-switching-protocols",
        }
        if not signalr_control_passed:
            reason_codes.append("authorization.signalr.not-enforced")
            return 1

        evidence["observations"]["projectionSignalR"] = {
            "result": "passed" if signalr_passed else "failed",
            "authenticated": signalr_control_passed,
            "reasonCode": "signalr.authenticated-connect.succeeded" if signalr_passed else "signalr.authenticated-connect.failed",
            "endpoint": signalr_endpoint,
        }

        for name in OBSERVATIONS:
            if evidence["observations"][name]["result"] != "passed":
                reason_codes.append(evidence["observations"][name]["reasonCode"])
        for name in AUTHORIZATION_CONTROLS:
            if evidence["authorizationControls"][name]["result"] != "passed":
                reason_codes.append(evidence["authorizationControls"][name]["reasonCode"])
        evidence["finalVerdict"] = "passed" if not reason_codes else "failed"
        return 0 if not reason_codes else 1
    except (OSError, ValueError, json.JSONDecodeError) as exception:
        reason_codes.append(f"apphost.capture.{type(exception).__name__.lower()}")
        return 1
    finally:
        stop_timeout = _remaining_timeout(overall_deadline, 60)
        stop = (
            runtime.command(
                ["aspire", "stop", "--apphost", APPHOST_RELATIVE, "--non-interactive", "--nologo"],
                stop_timeout,
            )
            if stop_timeout is not None
            else CommandResult(-1, "", "capture deadline exhausted before cleanup stop")
        )
        describe_timeout = _remaining_timeout(overall_deadline, 15)
        describe_after = (
            runtime.command(
                ["aspire", "describe", "--apphost", APPHOST_RELATIVE, "--format", "Json", "--non-interactive", "--nologo"],
                describe_timeout,
            )
            if describe_timeout is not None
            else CommandResult(-1, "", "capture deadline exhausted before cleanup describe")
        )
        parsed_after = _json_from_output(describe_after.stdout) if describe_after.returncode == 0 else None
        records_after = _resource_records(parsed_after) if parsed_after is not None else []
        if records_after:
            probed_urls.extend(_probed_resource_urls(records_after))
        running_after: int | None = None
        while time.monotonic() < overall_deadline:
            ps_timeout = _remaining_timeout(overall_deadline, 10)
            if ps_timeout is None:
                break
            running_after = _running_apphost_count(runtime, ps_timeout)
            if running_after == 0:
                break
            if running_after is None:
                break
            _bounded_sleep(overall_deadline, 0.5)
        host_stopped = running_after == 0
        ports_closed = False
        urls_to_close = [url for url in [*endpoints.values(), *probed_urls] if url]
        ordered_close: list[str] = []
        for url in urls_to_close:
            if url not in ordered_close:
                ordered_close.append(url)
        deadline = overall_deadline
        while host_stopped and time.monotonic() < deadline:
            ports_closed = True
            for value in ordered_close:
                port_timeout = _remaining_timeout(deadline, 1)
                if port_timeout is None or _port_open(value, port_timeout):
                    ports_closed = False
                    break
            if ports_closed:
                break
            _bounded_sleep(deadline, 0.5)
        if ordered_close:
            listener_confirmation = "ports-probed-closed" if ports_closed else "ports-open-or-unproven"
        elif not host_start_attempted and not host_started and cold_stop_confirmed:
            ports_closed = host_stopped
            listener_confirmation = "no-host-started" if ports_closed else "ports-unproven"
        else:
            ports_closed = False
            listener_confirmation = "ports-unproven"
        lifecycle_clean = host_stopped and ports_closed and stop.returncode == 0
        created_by_invocation = sorted(
            relative
            for relative in absent_before_run
            if dapr_paths[relative].exists()
        )
        removed_after_shutdown: list[str] = []
        if lifecycle_clean:
            for relative in created_by_invocation:
                try:
                    dapr_paths[relative].unlink()
                    removed_after_shutdown.append(relative)
                except OSError:
                    pass
        remaining_after_cleanup = sorted(
            relative
            for relative in created_by_invocation
            if dapr_paths[relative].exists()
        )
        post_manifest, post_runtime_issues = runtime_evidence.runtime_input_manifest(ROOT)
        manifest_unchanged = True
        if runtime_input_manifest_path is not None:
            manifest_after = runtime_evidence._bounded_read(
                runtime_input_manifest_path,
                post_runtime_issues,
                "pre-provider runtime-input manifest",
            )
            manifest_unchanged = (
                runtime_manifest_bytes is not None
                and manifest_after == runtime_manifest_bytes
            )
        runtime_inputs_clean = (
            lifecycle_clean
            and not post_runtime_issues
            and post_manifest.get("treeSha256") == initial_runtime_tree
            and manifest_unchanged
        )
        package_authority_clean = initial_package_ledger is None
        if initial_package_ledger is not None and assets_paths:
            recomputed_package_ledger, package_issues = runtime_evidence.resolved_package_ledger(
                ROOT,
                package_root,
                assets_paths,
                captured_at=initial_package_ledger.get("capturedAt"),
            )
            package_authority_clean = (
                not package_issues
                and runtime_evidence._exact(
                    recomputed_package_ledger, initial_package_ledger
                )
            )
        outputs_after, outputs_valid_after = _runtime_output_inventory()
        runtime_outputs_clean = initial_runtime_outputs is None or (
            outputs_valid_after
            and runtime_evidence._exact(outputs_after, initial_runtime_outputs)
        )
        clean = (
            lifecycle_clean
            and removed_after_shutdown == created_by_invocation
            and not remaining_after_cleanup
            and runtime_inputs_clean
            and package_authority_clean
            and runtime_outputs_clean
        )
        evidence["cleanup"] = {
            "command": STOP_COMMAND,
            "result": "clean" if clean else "failed",
            "hostStopped": host_stopped,
            "portsClosed": ports_closed,
            "runningAppHostsAfterAttempt": running_after if running_after is not None else -1,
            "listenerConfirmation": listener_confirmation,
            "confirmation": (
                "aspire-ps-empty"
                if running_after == 0
                else "aspire-ps-running"
                if isinstance(running_after, int) and running_after > 0
                else "aspire-ps-failed"
            ),
            "daprNameResolutionFiles": {
                "absentBeforeRun": absent_before_run,
                "createdByInvocation": created_by_invocation,
                "removedAfterShutdown": removed_after_shutdown,
                "remainingAfterCleanup": remaining_after_cleanup,
            },
            "runtimeInputsCleanAfterRun": runtime_inputs_clean,
            "packageAuthorityCleanAfterRun": package_authority_clean,
            "runtimeOutputsCleanAfterRun": runtime_outputs_clean,
        }
        if not clean and "apphost.cleanup.incomplete" not in reason_codes:
            reason_codes.append("apphost.cleanup.incomplete")
        if reason_codes:
            evidence["finalVerdict"] = "failed"
        evidence["completedAt"] = datetime.now(timezone.utc).isoformat()
        _atomic_write(output, evidence)
        if not clean:
            raise _CleanupFailed


def capture(
    output: Path,
    runtime: SmokeRuntime | None = None,
    timeout: int = 300,
    runtime_input_manifest_path: Path | None = None,
    package_root: Path | None = None,
) -> int:
    if package_root is None:
        with tempfile.TemporaryDirectory(prefix="frontcomposer-apphost-packages.") as temporary:
            return capture(
                output,
                runtime=runtime,
                timeout=timeout,
                runtime_input_manifest_path=runtime_input_manifest_path,
                package_root=Path(temporary),
            )
    try:
        return _capture(
            output,
            runtime or SmokeRuntime(),
            timeout,
            runtime_input_manifest_path,
            package_root,
        )
    except _CleanupFailed:
        return 1


def _port_open(url: str, timeout: float = 1) -> bool:
    parsed = parse.urlsplit(url)
    try:
        with socket.create_connection(
            (parsed.hostname or "", parsed.port or (443 if parsed.scheme == "https" else 80)),
            timeout=timeout,
        ):
            return True
    except OSError:
        return False


def _report_failure(output: Path) -> None:
    print(f"AppHost smoke failed; evidence={output}", file=sys.stderr)
    if not output.is_file():
        print("No evidence file was written.", file=sys.stderr)
        return
    try:
        document = json.loads(output.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exception:
        print(f"Could not read evidence: {exception}", file=sys.stderr)
        return
    if not isinstance(document, dict):
        print("Evidence is not a JSON object.", file=sys.stderr)
        return
    print(f"finalVerdict={document.get('finalVerdict')}", file=sys.stderr)
    print(f"reasonCodes={document.get('reasonCodes')}", file=sys.stderr)
    startup = document.get("startup")
    if isinstance(startup, dict):
        for key in ("startReturnCode", "startStderr", "startStdout"):
            value = startup.get(key)
            if value not in (None, ""):
                print(f"{key}={value}", file=sys.stderr)
    observations = document.get("observations")
    if isinstance(observations, dict):
        print(f"observations={json.dumps(observations, separators=(',', ':'))}", file=sys.stderr)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--output",
        type=Path,
        default=ROOT / "_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/apphost-smoke.json",
    )
    parser.add_argument("--timeout-seconds", type=int, default=300)
    parser.add_argument("--runtime-input-manifest", required=True, type=Path)
    parser.add_argument("--package-root", required=True, type=Path)
    args = parser.parse_args(argv)
    if not 30 <= args.timeout_seconds <= 600:
        parser.error("--timeout-seconds must be between 30 and 600")
    output = args.output.absolute()
    code = capture(
        output,
        timeout=args.timeout_seconds,
        runtime_input_manifest_path=args.runtime_input_manifest.absolute(),
        package_root=args.package_root.absolute(),
    )
    if code != 0:
        _report_failure(output)
    return code


if __name__ == "__main__":
    raise SystemExit(main())
