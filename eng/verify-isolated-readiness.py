#!/usr/bin/env python3
"""Owned disposable maintenance rehearsal; never accepts a caller-supplied datastore."""
import argparse
import base64
import contextlib
import fcntl
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import shutil
import signal
import socket
import ssl
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parent.parent
APPHOST = ROOT / "src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj"
EVENTSTORE = ROOT / "references/Hexalith.EventStore"
LABEL = "hexalith.frontcomposer.verification"
MARKER = "projection-delivery-writer-protocol"
WORKERS = "EventStore__Projections__WorkersEnabled"
# ProjectionNotificationTopicAttribute in the exact selected gateway source.
INFRASTRUCTURE_STREAM = "*.*.projection-changed"
TARGETS = {"redis": 6379, "placement": 50005, "scheduler": 50006}
REQUIRED_HEALTH = {"self", "dapr-sidecar", "dapr-actor-placement", "dapr-statestore", "dapr-pubsub",
                   "dapr-configstore", "dapr-app-channel-token", MARKER, "projection-delivery-unresolved-conflict"}
NATIVE_RUNTIME = Path.home() / ".dapr/bin/daprd"
PROC_ROOT = Path("/proc")
INSTANCE_LABEL = LABEL + ".instance"
TERMINATION_GRACE_SECONDS = 10
_starting_child = False
_pending_cancellation = False
FULL_APPLICATIONS = ("eventstore", "eventstore-admin", "eventstore-admin-ui", "tenants", "parties",
                     "sample", "tenants-api", "tenants-ui", "frontcomposer-ui", "counter-web")
FULL_SIDECARS = {"eventstore": {"statestore", "pubsub", "configstore"},
                 "eventstore-admin": {"statestore"}, "eventstore-admin-ui": set(),
                 "tenants": {"statestore", "pubsub"}, "parties": {"statestore", "pubsub"}, "sample": set(),
                 "tenants-api": set(), "tenants-ui": set(), "frontcomposer-ui": set()}
ACCESS_CONTROL_FILES = ("accesscontrol.yaml", "accesscontrol.eventstore-admin.yaml", "accesscontrol.sample.yaml",
                        "accesscontrol.tenants-api.yaml", "accesscontrol.tenants-ui.yaml", "accesscontrol.frontcomposer-ui.yaml")


def refuse_inherited_isolation_escapes(environment):
    # Reject instead of silently dropping caller configuration. The run must be
    # reproducible, and no inherited endpoint, component or authentication override
    # may send an ordinary application outside this owned topology.
    prefixes = ("frontcomposerverification:", "dapr", "eventstore:", "adminserver:",
                "authentication:", "tenants:", "parties:", "connectionstrings:", "services:",
                "parameters:", "keycloak", "frontcomposerui:", "tenantscopefixture:", "memories", "msbuild",
                "custombefore", "customafter", "directorybuild", "minver", "hexalithsourcemapping", "frontcomposersourcereference")
    for key in environment:
        normalized = key.replace("__", ":").lower()
        require(normalized not in {"enablekeycloak", "usehexalithprojectreferences", "usenugetdeps", "configuration",
                                   "eventstorepath", "tenantspath", "partiespath", "hexalith_dapr_home", "dotnet_environment", "aspnetcore_environment",
                                   "hexalith_source_mapping_hash", "version", "versionprefix", "versionsuffix",
                                   "assemblyversion", "fileversion", "packageversion", "dotnet_startup_hooks",
                                   "restoreconfigfile", "restoresources", "importdirectorybuildprops"}
                and not (normalized.startswith("hexalith") and normalized.endswith(("root", "fromsource")))
                and not normalized.startswith(prefixes),
                "Inherited topology or isolation override refused; start with an unconfigured verification environment.")


def selected_applications(config, maintenance):
    return ("eventstore",) if maintenance else FULL_APPLICATIONS if config.get("topology", "gateway") == "full" else ("eventstore", "counter-web")


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


class VerificationCancelled(RuntimeError):
    pass


def raise_cancellation():
    # A second cancellation must not interrupt the owned finally cleanup.
    for value in (signal.SIGINT, signal.SIGTERM):
        signal.signal(value, signal.SIG_IGN)
    raise VerificationCancelled("Verification cancelled; owned cleanup requested.")


@contextlib.contextmanager
def cancellation_handlers():
    global _pending_cancellation
    pending_before = _pending_cancellation
    _pending_cancellation = False

    def cancel(signum, frame):
        global _pending_cancellation
        if _starting_child:
            # Popen can already have launched a child before returning its handle.
            # Keep the caught signal disposition so the child can handle SIGTERM;
            # raise only after invoke owns that handle inside its cleanup guard.
            _pending_cancellation = True
            return
        raise_cancellation()

    previous = {value: signal.signal(value, cancel) for value in (signal.SIGINT, signal.SIGTERM)}
    try:
        yield
    finally:
        for value, handler in previous.items():
            signal.signal(value, handler)
        _pending_cancellation = pending_before


def terminate_child(process):
    try:
        os.killpg(process.pid, signal.SIGTERM)
    except ProcessLookupError:
        pass
    try:
        return process.communicate(timeout=TERMINATION_GRACE_SECONDS)
    except subprocess.TimeoutExpired:
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        return process.communicate(timeout=5)


def write_diagnostics(path, output, redactions=()):
    for value in redactions:
        if value:
            output = output.replace(value.encode(), b"[redacted]")
    output = re.sub(rb'([?&](?:t|token|access_token|code)=)[^\s"&#]+', rb'\1[redacted]', output, flags=re.IGNORECASE)
    output = re.sub(rb'(Bearer )[A-Za-z0-9._~-]+', rb'\1[redacted]', output, flags=re.IGNORECASE)
    Path(path).write_bytes(output)


def invoke(arguments, *, env=None, input=None, log=None, timeout=600, redactions=()):
    global _starting_child, _pending_cancellation
    _starting_child = True
    try:
        process = subprocess.Popen(arguments, cwd=ROOT, env=env, stdin=subprocess.PIPE,
                                   stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True)
        with process:
            try:
                _starting_child = False
                if _pending_cancellation:
                    _pending_cancellation = False
                    raise_cancellation()
                output, errors = process.communicate(input=input, timeout=timeout)
            except BaseException:
                # Give shell EXIT/TERM traps time to stop their exact owned AppHost.
                output, errors = terminate_child(process)
                if log:
                    write_diagnostics(log, output + errors, redactions)
                raise
            if log:
                write_diagnostics(log, output + errors, redactions)
            suffix = "; private diagnostics retained." if log else "."
            require(process.returncode == 0, f"{Path(arguments[0]).name} operation failed" + suffix)
            return output
    finally:
        _starting_child = False


def container_present(name=None, identifier=None):
    # A successful daemon query with no matches proves absence. Failed inspect
    # (daemon unavailable, denied, or absent) cannot establish safe cleanup.
    selection = "id=" + identifier if identifier else "name=^/" + re.escape(name) + "$"
    found = invoke(["docker", "container", "ls", "--all", "--no-trunc", "--filter", selection,
                    "--format", "{{.ID}}"])
    return bool(found.strip())


def private_file(path, content):
    with open(path, "x", encoding="utf-8") as output:
        output.write(content)
    os.chmod(path, 0o600)


def load_configuration(directory):
    directory = Path(directory)
    require(directory.is_absolute(), "Verification configuration requires an absolute private directory.")
    require(not any(path.is_symlink() for path in [directory, *directory.parents]), "Linked verification directory refused.")
    for entry in [directory, directory / "configuration.json"]:
        require(not entry.is_symlink() and entry.stat().st_uid == os.getuid()
                and entry.stat().st_mode & 0o077 == 0, "Verification configuration is not privately owned.")
    config = json.loads((directory / "configuration.json").read_text())
    require(re.fullmatch(r"[0-9a-f]{32}", config["owner"]), "Invalid verification owner.")
    require(re.fullmatch(r"1\.18\.\d+", config["daprVersion"]), "Unsupported installed Dapr runtime.")
    require(set(config["ports"]) == {*TARGETS, "daprHttp"}, "Incomplete verification port inventory.")
    ports = list(config["ports"].values())
    require(len(set(ports)) == 4 and all(type(port) is int and 10000 <= port <= 65535 for port in ports),
            "Invalid verification port inventory.")
    require(config.get("topology", "gateway") in {"gateway", "full"}, "Unsupported verification topology.")
    if config.get("topology") == "full":
        require(re.fullmatch(r"[0-9a-f]{32}", config.get("instance", "")), "Missing fresh verification launch identity.")
    return config


def container_name(config, name):
    return f"fc-verify-{config['owner']}-{name}"


def validate_container(config, name, container, expected_id=None):
    require(container["Name"] == "/" + container_name(config, name)
            and container["Config"].get("Labels", {}).get(LABEL) == config["owner"]
            and container["Image"] == config["images"][name]
            and (expected_id is None or container["Id"] == expected_id),
            "Container ownership or image identity changed; refusing access.")
    bindings = container["HostConfig"].get("PortBindings") or {}
    require(bindings == {f"{TARGETS[name]}/tcp": [{"HostIp": "127.0.0.1", "HostPort": str(config["ports"][name])}]},
            "Container does not have the exact isolated loopback binding.")
    return container["Id"]


def inspect_container(config, name, expected_id=None):
    containers = json.loads(invoke(["docker", "inspect", container_name(config, name)]))
    require(len(containers) == 1, "Ambiguous container identity.")
    validate_container(config, name, containers[0], expected_id)
    return containers[0]


def resource(name, env):
    data = json.loads(invoke(["aspire", "describe", name, "--apphost", str(APPHOST),
                              "--format", "Json", "--non-interactive", "--nologo"], env=env))
    selected = [item for item in data["resources"] if item.get("displayName") == name]
    require(len(selected) == 1, "Ambiguous Aspire resource identity.")
    return selected[0]


def endpoint(item, scheme=None):
    urls = [value["url"].rstrip("/") for value in item.get("urls", [])
            if value.get("name") != "management" and re.fullmatch(r"https?://(?:localhost|127\.0\.0\.1|\[::1\]):\d+/?", value.get("url", ""))]
    if scheme:
        urls = [url for url in urls if url.startswith(scheme + ":")]
    require(bool(urls), "No eligible loopback application endpoint.")
    return urls[0]


class RejectRedirects(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, new_url):
        return None


def request(url, data=None, token=None, form=False, *, method=None, include_headers=False, timeout=20):
    parsed = urllib.parse.urlparse(url)
    require(parsed.hostname in {"localhost", "127.0.0.1", "::1"} and parsed.scheme in {"http", "https"},
            "Non-loopback verification request refused.")
    headers = {}
    if token:
        headers["Authorization"] = "Bearer " + token
    if data is not None:
        headers["Content-Type"] = "application/x-www-form-urlencoded" if form else "application/json"
        data = (urllib.parse.urlencode(data) if form else json.dumps(data)).encode()
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}),
        urllib.request.HTTPSHandler(context=ssl._create_unverified_context()), RejectRedirects())
    try:
        with opener.open(urllib.request.Request(url, data=data, headers=headers, method=method), timeout=timeout) as response:
            body = response.read(1024 * 1024 + 1)
            require(len(body) <= 1024 * 1024, "Verification response exceeded its bounded contract.")
            try:
                result = json.loads(body)
            except json.JSONDecodeError:
                result = body.decode()
            return (response.status, result, response.headers) if include_headers else (response.status, result)
    except urllib.error.HTTPError as failure:
        try:
            body = failure.read(1024 * 1024 + 1)
            require(len(body) <= 1024 * 1024, "Verification response exceeded its bounded contract.")
            try:
                payload = json.loads(body)
            except json.JSONDecodeError:
                payload = {}
            return (failure.code, payload, failure.headers) if include_headers else (failure.code, payload)
        finally:
            failure.close()


def validate_health(code, health, maintenance):
    entries = health.get("results", {})
    unhealthy = {key for key, value in entries.items() if value.get("status") != "Healthy"}
    require(REQUIRED_HEALTH <= entries.keys(), "Application health is missing required component results.")
    require((code == 503 and health.get("status") == "Unhealthy" and unhealthy == {MARKER}) if maintenance
            else (code == 200 and health.get("status") == "Healthy" and not unhealthy),
            "Application components have not reached the required verification phase.")


def validate_marker(marker, source_commit):
    require(marker.get("schemaVersion") == 1 and marker.get("writerProtocolVersion") == 2
            and marker.get("cutoverCommit") == source_commit and bool(marker.get("activatedAt")),
            "Persisted writer marker does not identify the exact selected source.")


def identity_name(config):
    return "fc-verify-" + config["owner"] + ("-" + config["instance"] if config.get("topology") == "full" else "") + "-identity"


def require_private_path(path):
    path = Path(path)
    require(path.is_absolute() and not any(item.is_symlink() for item in [path, *path.parents])
            and path.stat().st_uid == os.getuid() and path.stat().st_mode & 0o077 == 0,
            "Private mTLS ownership/path validation failed; recovery files retained.")


def read_mtls_paths(directory, config):
    path = Path(directory) / "mtls-plane.json"
    require_private_path(path)
    state = json.loads(path.read_text())
    run = Path(state["runDirectory"])
    issuer = Path(state["certificateDirectory"])
    expected = Path(directory) / "mtls" / ("dapr-credentials-" + hashlib.sha256(str(run).encode()).hexdigest().upper()[:32])
    require(state["owner"] == config["owner"] and state["instance"] == config["instance"]
            and run.parent == Path(tempfile.gettempdir()) / "hexalith-dapr"
            and re.fullmatch(r"[0-9a-f]{32}", run.name) and issuer == expected,
            "Canonical mTLS cleanup paths changed; recovery files retained.")
    for item in (run, issuer):
        if item.exists(): require_private_path(item)
    return run, issuer


def require_native_process_absence(directory, proc_root=Path("/proc"), *, wait_seconds=0):
    # AppHost disappearance can precede native-child exit. Never delete credentials
    # until the ownership-bound inventory proves absence; a bounded drain timeout
    # or unreadable native candidate retains all recovery files.
    require(proc_root.is_dir(), "Native process absence cannot be verified; recovery files retained.")
    prefix = str(Path(directory)) + os.sep
    deadline = time.monotonic() + wait_seconds
    while True:
        owned = False
        for entry in proc_root.iterdir():
            if not entry.name.isdecimal():
                continue
            try:
                if entry.stat().st_uid != os.getuid():
                    continue
                command = entry.joinpath("cmdline").read_bytes().decode().split("\0")
                if not command[0]:
                    continue
                process_name = entry.joinpath("comm").read_text().strip()
                if Path(command[0]).name != "daprd" and process_name != "daprd":
                    continue
                executable = str(entry.joinpath("exe").readlink()).removesuffix(" (deleted)")
                if Path(executable).name != "daprd":
                    continue
            except (FileNotFoundError, ProcessLookupError):
                continue
            except (PermissionError, UnicodeDecodeError):
                raise RuntimeError("Native process ownership could not be checked; recovery files retained.") from None
            for index, argument in enumerate(command):
                value = argument.split("=", 1)[1] if argument.startswith(("--config=", "--resources-path=")) else (
                    argument if index and command[index - 1] in {"--config", "--resources-path"} else "")
                owned = owned or value.startswith(prefix)
        if not owned:
            return
        require(time.monotonic() < deadline, "A native sidecar still references this owned run; recovery files retained.")
        time.sleep(min(0.1, max(0, deadline - time.monotonic())))


def cleanup_runtime_phase(directory, env):
    config = load_configuration(directory)
    require(not any(item["appHostPath"] == str(APPHOST) for item in processes(env)),
            "An AppHost remains; private runtime files and identity retained.")
    name = identity_name(config)
    if container_present(name=name):
        containers = json.loads(invoke(["docker", "inspect", name]))
        require(len(containers) == 1, "Ambiguous private identity ownership; cleanup refused.")
        item = containers[0]
        require(item["Name"] == "/" + name and item["Config"].get("Labels", {}).get(LABEL) == config["owner"]
                and item["Config"].get("Labels", {}).get(INSTANCE_LABEL) == config["instance"]
                and not any(mount.get("Destination") == "/opt/keycloak/data" for mount in item.get("Mounts", [])),
                "Private identity ownership/realm storage changed; cleanup refused.")
        identifier = item["Id"]
        invoke(["docker", "rm", "--force", "--volumes", identifier], log=Path(directory).parent / ("cleanup-identity-" + config["instance"] + ".log"))
        require(not container_present(identifier=identifier), "Private identity cleanup could not be confirmed.")
    deadline = time.monotonic() + 15
    while True:
        remaining = invoke(["docker", "container", "ls", "--all", "--filter", "label=" + LABEL + "=" + config["owner"],
                            "--filter", "label=" + INSTANCE_LABEL + "=" + config["instance"], "--format", "{{.ID}}"]).strip()
        if not remaining:
            break
        # Session disposal can still be draining after CLI registration disappears.
        # Observe only this exact owner/instance; never force-delete uncertain peers.
        require(time.monotonic() < deadline, "Private mTLS containers remain; credentials and recovery files retained.")
        time.sleep(min(0.1, max(0, deadline - time.monotonic())))
    require_native_process_absence(directory, wait_seconds=15)
    state = Path(directory) / "mtls-plane.json"
    if state.exists():
        run, issuer = read_mtls_paths(directory, config)
        # No process/container can still read these private credentials. Delete only
        # the canonical helper's validated fresh paths, never its parent workspace.
        for path in (issuer, run):
            if path.exists(): shutil.rmtree(path)
            require(not path.exists(), "Canonical mTLS files remain; recovery metadata retained.")
        state.unlink()
    require(not list((Path(directory) / "mtls").iterdir()), "Unexpected private issuer entries; recovery files retained.")


def prepare_instance(directory):
    config = load_configuration(directory)
    require_native_process_absence(directory)
    require(not (Path(directory) / "mtls-plane.json").exists()
            and not list((Path(directory) / "mtls").iterdir()), "Prior mTLS recovery files prevent a fresh launch.")
    config["instance"] = secrets.token_hex(16)
    require(not container_present(name=identity_name(config)), "Fresh private identity name already exists.")
    (Path(directory) / "configuration.json").write_text(json.dumps(config))


def remove_plain_control_plane(directory):
    config = load_configuration(directory)
    for name in ("placement", "scheduler"):
        container = inspect_container(config, name, config["containerIds"][name])
        invoke(["docker", "rm", "--force", "--volumes", container["Id"]], log=Path(directory).parent / ("maintenance-remove-" + name + ".log"))
        require(not container_present(identifier=container["Id"]), "Plaintext control-plane cleanup was not confirmed.")


def verify_mtls_plane(directory, env):
    config = load_configuration(directory)
    read_mtls_paths(directory, config)
    report = []
    for name in ("dapr-sentry", "dapr-placement-mtls", "dapr-scheduler-mtls"):
        item = resource(name, env)
        require(item.get("state") == "Running" and item.get("healthStatus") == "Healthy", "A private mTLS control-plane resource is unhealthy.")
        identifier = item["properties"]["container.id"]
        containers = json.loads(invoke(["docker", "inspect", identifier]))
        require(len(containers) == 1 and containers[0]["Id"] == identifier, "Ambiguous mTLS container identity.")
        container = containers[0]
        labels = container["Config"].get("Labels", {})
        require(labels.get(LABEL) == config["owner"] and labels.get(INSTANCE_LABEL) == config["instance"]
                and container["Image"] == config["images"]["sentry" if name == "dapr-sentry" else "placement"],
                "mTLS container ownership/image changed.")
        bindings = container["HostConfig"].get("PortBindings") or {}
        require(bindings and all(binding["HostIp"] in {"127.0.0.1", "::1"} for entries in bindings.values() for binding in entries),
                "mTLS infrastructure exposed an unowned/non-loopback listener.")
        require(any(Path(mount.get("Source", "")) == read_mtls_paths(directory, config)[1] for mount in container.get("Mounts", [])),
                "mTLS issuer mount is not the owned private directory.")
        report.append({"resource": name, "containerId": identifier, "imageId": container["Image"], "bindings": bindings})
    Path(directory).parent.joinpath("mtls-plane-observed.json").write_text(json.dumps(report, indent=2))


def read_owned_realm(identifier, config):
    try:
        containers = json.loads(invoke(["docker", "inspect", identifier]))
    except RuntimeError:
        raise RuntimeError("Owned Keycloak container inspection failed; private realm access stopped.") from None
    require(len(containers) == 1 and containers[0]["Id"] == identifier
            and containers[0].get("Name") == "/" + identity_name(config)
            and containers[0].get("Config", {}).get("Labels", {}).get(LABEL) == config["owner"]
            and (config.get("topology") != "full" or containers[0].get("Config", {}).get("Labels", {}).get(INSTANCE_LABEL) == config["instance"]),
            "Keycloak container ownership changed; refusing private realm access.")
    # The selected Aspire Keycloak integration copies imports with WithContainerFiles;
    # it does not bind-mount the original private directory. Read only the known copy
    # in the exact container discovered from this owned AppHost; never log its body.
    try:
        return json.loads(invoke(["docker", "exec", identifier, "cat",
                                 "/opt/keycloak/data/import/hexalith-realm.json"]))
    except RuntimeError:
        raise RuntimeError("Owned Keycloak private realm could not be read; verification stopped.") from None


def verify_workload_identity(env, config):
    security = resource("security", env)
    realm = read_owned_realm(security["properties"]["container.id"], config)
    client = next(item for item in realm["clients"] if item["clientId"] == "eventstore")
    code, result = request(endpoint(security) + "/realms/" + realm["realm"] + "/protocol/openid-connect/token", {
        "grant_type": "client_credentials", "client_id": "eventstore", "client_secret": client["secret"],
        "scope": "eventstore-audience.sample eventstore-operation.domain-service.metadata"}, form=True)
    require(code == 200 and "access_token" in result, "Fresh workload credential exchange failed.")
    encoded = result["access_token"].split(".")[1]
    claims = json.loads(base64.urlsafe_b64decode(encoded + "=" * (-len(encoded) % 4)))
    audiences = claims.get("aud", [])
    require(claims.get("azp") == "eventstore" and "sample" in ([audiences] if isinstance(audiences, str) else audiences)
            and claims.get("eventstore:operation") == "domain-service:metadata", "Fresh workload claims are inconsistent.")
    print("Fresh owned realm workload exchange: HTTP 200; expected client, audience and operation verified.", flush=True)


def correlate_sidecar(gateway, sidecar, name="eventstore", *, native=False):
    command = Path(sidecar.get("properties", {}).get("executable.path", ""))
    require(sidecar.get("displayName") == name + "-dapr-cli" and sidecar.get("state") == "Running"
            and (command == NATIVE_RUNTIME if native else command.name == "dapr"),
            "The gateway's owned sidecar is not running.")
    environment = gateway.get("environment", {})
    if native:
        peer = sidecar.get("environment", {})
        require(all(peer.get(key) for key in ("DAPR_TRUST_ANCHORS", "DAPR_CERT_CHAIN", "DAPR_CERT_KEY"))
                and peer.get("NAMESPACE") == "default" and peer.get("DAPR_CONTROLPLANE_NAMESPACE") == "default"
                and peer.get("DAPR_CONTROLPLANE_TRUST_DOMAIN") == "localhost",
                "Native sidecar lacks its private SPIFFE issuer/control-plane configuration.")
    for name, variable in [("http", "DAPR_HTTP_PORT"), ("grpc", "DAPR_GRPC_PORT")]:
        listener = endpoint({"urls": [url for url in sidecar.get("urls", []) if url.get("name") == name]}, "http")
        port = urllib.parse.urlparse(listener).port
        require(str(environment.get(variable)) == str(port), "Gateway and owned sidecar listener do not match.")
    return endpoint({"urls": [url for url in sidecar["urls"] if url.get("name") == "http"]}, "http")


def require_native_channel(directory, application, name, proc_root=Path("/proc")):
    # Aspire can expose a parameter reference/redaction instead of a resolved
    # secret. Compare only this exact application's/native child's actual secret
    # in memory, after executable, UID, SDK port and private config ownership.
    require_private_path(directory)
    projects = {"eventstore": ("Hexalith.EventStore", "src/Hexalith.EventStore"),
                "tenants": ("Hexalith.Tenants", "src/Hexalith.Tenants"),
                "parties": ("Hexalith.Parties", "src/Hexalith.Parties"),
                "sample": ("Hexalith.Tenants", "samples/Hexalith.Tenants.Sample")}
    module, project = projects[name]
    binary = ROOT / "references" / module / project / "bin/Debug/net10.0" / Path(project).name
    port = str(application.get("environment", {}).get("DAPR_HTTP_PORT", ""))
    require(port.isdecimal() and int(port) > 0, "Missing owned SDK port for channel credential verification.")
    config = str(Path(directory) / ("accesscontrol.sample.yaml" if name == "sample" else "accesscontrol.yaml"))
    found = {"application": [], "sidecar": []}
    for entry in proc_root.iterdir():
        if not entry.name.isdecimal(): continue
        try:
            if entry.stat().st_uid != os.getuid(): continue
            command = entry.joinpath("cmdline").read_bytes().decode().split("\0")
            if not command[0]: continue
            # Avoid unrelated protected executables before resolving their link.
            if command[0] not in {str(binary), str(NATIVE_RUNTIME)}: continue
            executable = str(entry.joinpath("exe").readlink()).removesuffix(" (deleted)")
            if executable not in {str(binary), str(NATIVE_RUNTIME)}: continue
            kind = "application" if executable == str(binary) else "sidecar"
            if kind == "sidecar":
                def option(flag):
                    values = [argument.split("=", 1)[1] for argument in command if argument.startswith(flag + "=")]
                    values += [command[index + 1] for index, argument in enumerate(command[:-1]) if argument == flag]
                    return values[0] if len(values) == 1 else None
                if option("--app-id") != name or option("--dapr-http-port") != port or option("--config") != config:
                    continue
            entries = entry.joinpath("environ").read_bytes().split(b"\0")
            tokens = [value.split(b"=", 1)[1] for value in entries if value.startswith(b"APP_API_TOKEN=")]
            ports = [value.split(b"=", 1)[1].decode() for value in entries if value.startswith(b"DAPR_HTTP_PORT=")]
            if kind == "application" and ports != [port]: continue
            found[kind].append(tokens[0] if len(tokens) == 1 and tokens[0] else None)
        except (FileNotFoundError, ProcessLookupError): continue
        except (PermissionError, UnicodeDecodeError):
            raise RuntimeError("Owned channel process credentials could not be verified.") from None
    observed = {"applicationCount": len(found["application"]), "sidecarCount": len(found["sidecar"]),
                "applicationTokenPresent": len(found["application"]) == 1 and bool(found["application"][0]),
                "sidecarTokenPresent": len(found["sidecar"]) == 1 and bool(found["sidecar"][0]),
                "matching": len(found["application"]) == len(found["sidecar"]) == 1
                    and bool(found["application"][0]) and found["application"][0] == found["sidecar"][0]}
    report_path = Path(directory).parent / "native-channel-observed.json"
    report = json.loads(report_path.read_text()) if report_path.exists() else {}
    report[name] = observed
    report_path.write_text(json.dumps(report, indent=2))
    require(len(found["application"]) == len(found["sidecar"]) == 1
            and found["application"][0] and found["application"][0] == found["sidecar"][0],
            "Actual owned application/native sidecar channel credentials are missing, ambiguous or mismatched (" + name + ").")
    print("Owned application/native channel match verified:", name, flush=True)


def verify_gateway(directory, env, maintenance):
    config = load_configuration(directory)
    native = config.get("topology") == "full" and not maintenance
    for name in (("redis",) if native else TARGETS):
        inspect_container(config, name, config.get("containerIds", {}).get(name))
    if native:
        verify_mtls_plane(directory, env)
    gateway = resource("eventstore", env)
    environment = gateway.get("environment", {})
    require((str(environment.get(WORKERS)).lower() == "false") if maintenance else WORKERS not in environment,
            "Gateway worker configuration does not match the verification phase.")
    # Aspire --isolated can reassign requested sidecar ports. Correlate both actual
    # listeners with the exact sidecar discovered under the owned AppHost instead.
    sidecar_base = correlate_sidecar(gateway, resource("eventstore-dapr-cli", env), native=native)
    if native: require_native_channel(directory, gateway, "eventstore")
    base = endpoint(gateway, "http")
    code, health = request(base + "/health")
    Path(directory).parent.joinpath("health-maintenance.json" if maintenance else "health-final.json").write_text(json.dumps(health))
    print("Application health components:", {key: value.get("status") for key, value in health.get("results", {}).items()}, flush=True)
    validate_health(code, health, maintenance)
    if not maintenance:
        ready_code, ready = request(base + "/ready")
        require(ready_code == 200 and ready.get("status") == "Healthy", "Application readiness is not Healthy.")
        print("Direct /health and /ready: HTTP 200 / Healthy; ordinary gateway workers restored.", flush=True)
    code, metadata = request(sidecar_base + "/v1.0/metadata")
    require(code == 200 and metadata.get("id") == "eventstore", "Unknown Dapr application.")
    require({item["name"] for item in metadata.get("components", [])} == {"statestore", "pubsub", "configstore"},
            "Unexpected Dapr component inventory; refusing shared resources.")
    verify_workload_identity(env, config)
    return base


def validate_application(item, name):
    require(item.get("displayName") == name and item.get("state") == "Running"
            and item.get("healthStatus") == "Healthy", "A full-topology application is missing or unhealthy.")
    environment = item.get("environment", {})
    require(WORKERS not in environment, "Full topology refuses a projection-worker override.")
    if name == "tenants":
        require(environment.get("Tenants__BootstrapGlobalAdminEnabled") == "false",
                "Disposable Tenants must suppress automatic bootstrap; the runner retains the actual attempt IDs.")
    if name == "parties":
        require(environment.get("Parties__MemoriesSearch__Enabled") == "false"
                and all(environment.get("Parties__MemoriesSearch__" + key) == "" for key in ("Endpoint", "TenantId", "CaseId", "ApiToken")),
                "Disposable Parties must retain disabled indexing with no external Memories route.")
    return endpoint(item, "http")


def verify_activated_source(directory, source_commit):
    config = load_configuration(directory)
    validate_marker(json.loads(redis(config, "HGET", MARKER, "data")), source_commit)
    verify_source_commit(source_commit)


def validate_application_health(name, path, code, result):
    if name == "sample":
        healthy = code == 200 and result == ("alive" if path == "/alive" else "healthy")
    else:
        healthy = (code == 200 and isinstance(result, dict) and result.get("status") == "Healthy"
                   and all(entry.get("status") == "Healthy" for entry in result.get("results", {}).values()))
    if name == "tenants-api" and healthy:
        expected = {"eventstore-gateway", "self"} if path == "/health" else {"eventstore-gateway"}
        healthy = expected <= result.get("results", {}).keys()
    require(healthy, f"{name} {path} failed its application health contract (HTTP {code}).")


def verify_full_topology(directory, env, source_commit):
    config = load_configuration(directory)
    require(config.get("topology") == "full" and env.get("FrontComposerVerification__Maintenance") == "false",
            "Full topology requires completed maintenance and ordinary workers.")
    # Recheck persisted state before accepting any full-graph result.
    verify_activated_source(directory, source_commit)
    verify_gateway(directory, env, False)
    endpoints = {}
    for name in FULL_APPLICATIONS:
        item = resource(name, env)
        base = validate_application(item, name)
        endpoints[name] = endpoint(item, "https") if name in {"frontcomposer-ui", "tenants-ui", "eventstore-admin-ui", "tenants-api"} else base
        if name == "tenants-api":
            base = endpoints[name]
        if name in {"eventstore-admin", "tenants", "parties", "sample", "tenants-api"}:
            for path in (("/health", "/alive") if name == "sample" else ("/health", "/ready")):
                code, result = request(base + path)
                summary = {"statusCode": code, "status": result.get("status") if isinstance(result, dict)
                           else result if result in ("healthy", "alive", "Healthy") else "Unexpected response shape",
                           "components": {key: entry.get("status") for key, entry in result.get("results", {}).items()}
                           if isinstance(result, dict) else {}}
                Path(directory).parent.joinpath(name + path.replace("/", "-") + ".json").write_text(json.dumps(summary))
                validate_application_health(name, path, code, result)
        if name in {"tenants", "parties", "sample"}: require_native_channel(directory, item, name)
        if name in FULL_SIDECARS:
            sidecar = correlate_sidecar(item, resource(name + "-dapr-cli", env), name, native=True)
            code, metadata = request(sidecar + "/v1.0/metadata")
            require(code == 200 and metadata.get("id") == name
                    and {component["name"] for component in metadata.get("components", [])} == FULL_SIDECARS[name],
                    "A full-topology sidecar has an unexpected app ID or component inventory.")
    print("Full topology: all ten ordinary applications Running/Healthy; direct endpoints and nine sidecars verified.", flush=True)
    return endpoints


def require_navigation_composition(env):
    snapshot = json.loads(invoke(["aspire", "describe", "--apphost", str(APPHOST),
                                  "--format", "Json", "--non-interactive", "--nologo"], env=env))
    inventory = snapshot.get("resources", [])
    required = ("tenants-api", "tenants-api-dapr-cli", "tenants-ui-dapr-cli", "frontcomposer-ui-dapr-cli")
    missing = [name for name in required if len([item for item in inventory if item.get("displayName") == name]) != 1]
    require(not missing, "Authenticated full navigation requires its approved API/BFF composition; missing " + ", ".join(missing) + ".")
    for name in ("tenants-api", "tenants-ui", "frontcomposer-ui"):
        applications = [item for item in inventory if item.get("displayName") == name]
        require(len(applications) == 1, "Ambiguous BFF ownership; authenticated navigation refused.")
        validate_application(applications[0], name)
        sidecar = next(item for item in inventory if item.get("displayName") == name + "-dapr-cli")
        # Never permit an SDK default such as localhost:3500 to stand in for this
        # BFF's exact Aspire-owned sidecar listener.
        owned = urllib.parse.urlparse(correlate_sidecar(applications[0], sidecar, name, native=True))
        selected = urllib.parse.urlparse(applications[0].get("environment", {}).get("DAPR_HTTP_ENDPOINT", ""))
        require(selected.scheme == "http" and selected.hostname in {"localhost", "127.0.0.1", "::1"}
                and selected.port == owned.port and selected.path in {"", "/"}
                and not selected.username and not selected.password and not selected.query and not selected.fragment,
                "Missing or unowned SDK endpoint; authenticated navigation refused.")


def authenticate_administrator(directory, env):
    config = load_configuration(directory)
    security = resource("security", env)
    realm = read_owned_realm(security["properties"]["container.id"], config)
    administrator = next(user for user in realm["users"] if user["username"] == "admin-user")
    code, result = request(endpoint(security) + "/realms/" + realm["realm"] + "/protocol/openid-connect/token", {
        "grant_type": "password", "client_id": "hexalith-eventstore", "username": administrator["username"],
        "password": administrator["credentials"][0]["value"], "scope": "openid profile email"}, form=True)
    require(code == 200 and "access_token" in result, "Private administrator credential exchange failed.")
    return result["access_token"]


def wait_for_query_catalog(directory, env):
    # The ordinary gateway refreshes its derived catalog after domain hosts start.
    # Never seed an index or use a healthy resource as evidence of query routing.
    gateway = resource("eventstore", env)
    base = correlate_sidecar(gateway, resource("eventstore-dapr-cli", env), native=True)
    deadline = time.monotonic() + 120
    observations = []
    while True:
        current = []
        for domain, query in (("tenants", "list-tenants"), ("global-administrators", "get-global-administrators")):
            remaining = deadline - time.monotonic()
            require(remaining > 0, "Ordinary handler catalog did not become ready within its bounded refresh budget.")
            code, body = request(base + "/v1.0/state/statestore/admin:query-types:" + domain, timeout=min(5, remaining))
            current.append({"domain": domain, "statusCode": code, "expectedHandlerRegistered": code == 200 and isinstance(body, list) and query in body})
        observations.append(current)
        Path(directory).parent.joinpath("query-catalog-readiness.json").write_text(json.dumps(observations, indent=2))
        if all(item["expectedHandlerRegistered"] for item in current): return
        time.sleep(min(2, max(0, deadline - time.monotonic())))


def projection_version(headers):
    def one(name, required=True):
        values = headers.get_all(name, [])
        require(len(values) == 1 or (not required and not values), "Membership projection metadata is missing or ambiguous.")
        return values[0].strip() if values else None
    require(one("X-Hexalith-Query-Provenance") == "ProjectionBacked", "Membership is not an authoritative projected read.")
    lifecycle = one("X-Hexalith-Projection-Lifecycle", False)
    stale = one("X-Hexalith-Is-Stale", False)
    degraded = one("X-Hexalith-Is-Degraded", False)
    require((lifecycle is None or lifecycle == "Current") and (stale is None or stale.lower() == "false")
            and (lifecycle is not None or stale is not None) and (degraded is None or degraded.lower() == "false"),
            "Membership projection is stale, degraded or has contradictory freshness evidence.")
    version = one("X-Hexalith-Projection-Version")
    require(version and len(version) <= 4096 and all(ord(value) >= 32 and ord(value) != 127 for value in version),
            "Membership projection version is invalid.")
    return version


def administrator_membership(base, token, subject, deadline):
    cursor = ""
    cursors = set()
    version = None
    found = False
    for _ in range(100):
        remaining = deadline - time.monotonic()
        require(remaining > 0, "Membership verification exceeded its bounded paging budget.")
        code, body, headers = request(base + "/api/global-administrators?" + urllib.parse.urlencode({"pageSize": 100, "cursor": cursor}),
                                      token=token, include_headers=True, timeout=min(5, remaining))
        if code == 403: return False, None
        require(code == 200 and isinstance(body, dict) and isinstance(body.get("items"), list) and type(body.get("hasMore")) is bool,
                "Membership lookup failed its native HTTP/page contract.")
        current = projection_version(headers)
        require(version is None or version == current, "Membership projection changed version across pages.")
        version = current
        found = found or any(item.get("userId") == subject for item in body["items"])
        if not body["hasMore"]: return found, version
        cursor = body.get("cursor")
        require(isinstance(cursor, str) and cursor and cursor not in cursors, "Membership paging cursor is missing or repeated.")
        cursors.add(cursor)
    raise RuntimeError("Membership lookup exceeded its native page bound.")


def new_ulid():
    alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"
    value = (int(time.time() * 1000) << 80) | secrets.randbits(80)
    return "".join(alphabet[(value >> (5 * index)) & 31] for index in range(25, -1, -1))


def capture_bootstrap_failure(directory, env, endpoints, token, subject, record, response=None):
    # Read only the retained attempt and bounded logs before the owned stop. A
    # missing status never proves non-delivery and never authorizes replacement.
    artifacts = Path(directory).parent
    redactions = (token, subject)
    if response is not None:
        write_diagnostics(artifacts / "bootstrap-dispatch-response.json", json.dumps(response, indent=2).encode(), redactions)
    try:
        code, status = request(endpoints["eventstore"] + "/api/v1/commands/status/" + record["correlationId"], token=token, timeout=5)
        write_diagnostics(artifacts / "bootstrap-status-after-failure.json", json.dumps({"requestedCorrelationId": record["correlationId"],
                          "httpStatus": code, "body": status}, indent=2).encode(), redactions)
    except VerificationCancelled: raise
    except Exception as failure:
        artifacts.joinpath("bootstrap-status-after-failure.json").write_text(json.dumps({"captureError": type(failure).__name__}))
    for name in ("eventstore", "eventstore-dapr-cli", "tenants", "tenants-dapr-cli", "dapr-placement-mtls"):
        try:
            invoke(["aspire", "logs", name, "--tail", "150", "--apphost", str(APPHOST), "--non-interactive", "--nologo"],
                   env=env, log=artifacts / ("bootstrap-failure-" + name + ".log"), timeout=10, redactions=redactions)
        except VerificationCancelled: raise
        except Exception as failure:
            artifacts.joinpath("bootstrap-failure-" + name + ".json").write_text(json.dumps({"captureError": type(failure).__name__}))


def ensure_administrator_membership(directory, env, endpoints):
    config = load_configuration(directory)
    verify_activated_source(directory, invoke(["git", "-C", str(EVENTSTORE), "rev-parse", "HEAD"]).decode().strip())
    validate_application(resource("tenants", env), "tenants")
    token = authenticate_administrator(directory, env)
    encoded = token.split(".")[1]
    subject = json.loads(base64.urlsafe_b64decode(encoded + "=" * (-len(encoded) % 4))).get("sub")
    require(isinstance(subject, str) and subject.strip(), "The actual private human token has no unambiguous subject.")
    record_path = Path(directory).parent / "bootstrap-result.json"
    require(not record_path.exists(), "Retained bootstrap attempt handles prohibit automatic redispatch.")
    member, version = administrator_membership(endpoints["tenants-api"], token, subject, time.monotonic() + 60)
    if member:
        record_path.write_text(json.dumps({"alreadyMember": True, "membershipVerified": True, "projectionVersion": version}))
        return
    # A denied membership read is not non-delivery evidence. Independently require
    # the real pristine cutover proof and no native aggregate history/read model.
    maintenance = json.loads(Path(directory).parent.joinpath("maintenance-result.json").read_text())
    require(maintenance.get("owner") == config["owner"] and maintenance.get("backupVerifiedNoDomainOrControlData") is True
            and maintenance.get("verifiedBackupKeyCount") == 1, "Bootstrap refuses an unverified or historical store.")
    keys = json.loads(redis(config, "--json", "KEYS", "*system:global-administrators:global-administrators*"))
    require(keys == [], "Native administrator aggregate history exists; bootstrap redispatch refused.")
    base = correlate_sidecar(resource("eventstore", env), resource("eventstore-dapr-cli", env), native=True)
    code, _ = request(base + "/v1.0/state/statestore/projection:global-administrators:singleton", timeout=5)
    require(code == 204, "Native administrator projection exists or is unavailable; bootstrap requires independent unbootstrapped evidence.")
    record = {"owner": config["owner"], "subjectSha256": hashlib.sha256(subject.encode()).hexdigest(),
              "freshStoreVerified": True, "automaticBootstrapSuppressed": True, "nativeAggregateHistoryAbsent": True,
              "nativeAdministratorProjectionAbsent": True, "messageId": new_ulid(), "correlationId": new_ulid(), "phase": "attempt-retained"}
    record_path.write_text(json.dumps(record, indent=2))
    # This is the documented one-time delegated-human command. The target is the
    # actual token sub; credentials/principal/domain authorization remain intact.
    command = {"messageId": record["messageId"], "correlationId": record["correlationId"], "tenant": "system",
               "domain": "global-administrators", "aggregateId": "global-administrators", "commandType": "BootstrapGlobalAdmin", "payload": {"UserId": subject}}
    response = None
    try:
        code, response = request(endpoints["eventstore"] + "/api/v1/commands", command, token=token, timeout=5)
        record.update({"dispatchHttpStatus": code, "phase": "dispatch-returned"})
        record_path.write_text(json.dumps(record, indent=2))
        require(code == 202 and isinstance(response, dict) and response.get("correlationId") == record["correlationId"],
                "Bootstrap delivery was not confirmed; retained IDs prohibit replacement.")
        record["phase"] = "status-verification"
        record_path.write_text(json.dumps(record, indent=2))
        deadline = time.monotonic() + 60
        while True:
            remaining = deadline - time.monotonic()
            require(remaining > 0, "Bootstrap status remains unresolved; retained IDs prohibit replacement.")
            code, status = request(endpoints["eventstore"] + "/api/v1/commands/status/" + record["correlationId"], token=token, timeout=min(5, remaining))
            state = status.get("status") if isinstance(status, dict) else None
            record.update({"statusHttpStatus": code, "terminalStatus": state, "phase": "status-verification"})
            record_path.write_text(json.dumps(record, indent=2))
            require(code == 200 and state in {"Completed", "Received", "Processing", "EventsStored", "EventsPublished"},
                    "Bootstrap status failed or is ambiguous; retained IDs prohibit replacement.")
            if state == "Completed": break
            time.sleep(min(1, max(0, deadline - time.monotonic())))
        record["phase"] = "membership-verification"
        record_path.write_text(json.dumps(record, indent=2))
        deadline = time.monotonic() + 60
        while True:
            member, version = administrator_membership(endpoints["tenants-api"], token, subject, deadline)
            if member: break
            require(time.monotonic() < deadline, "Bootstrap completed without qualifying projected membership; IDs retained.")
            time.sleep(min(1, max(0, deadline - time.monotonic())))
        record.update({"membershipVerified": True, "projectionVersion": version, "phase": "complete"})
        record_path.write_text(json.dumps(record, indent=2))
    except VerificationCancelled: raise
    except Exception:
        capture_bootstrap_failure(directory, env, endpoints, token, subject, record, response)
        raise
    print("Delegated-human bootstrap Completed; actual subject membership is fresh, non-degraded and version-consistent.", flush=True)


def diagnose_admin_path(directory, env, endpoints):
    # Admin owns a separate EventStore query path. API/BFF composition alone
    # cannot qualify its bearer forwarding or turn failed envelopes into data.
    require_navigation_composition(env)
    token = authenticate_administrator(directory, env)
    query = {"tenant": "system", "domain": "tenants", "aggregateId": "index",
             "queryType": "list-tenants", "payload": {"cursor": None, "pageSize": 100}}
    code, result = request(endpoints["eventstore"] + "/api/v1/queries", query, token=token)
    query_result = {"httpStatus": code, "success": result.get("success") if isinstance(result, dict) else None}
    if isinstance(result, dict):
        error = result.get("errorMessage")
        query_result["failureCategory"] = error if error in (None, "Forbidden", "Tenant not found") else "Other upstream failure"
    admin = resource("eventstore-admin", env)
    admin_code, admin_result = request(endpoint(admin, "https") + "/api/v1/admin/tenants", token=token)
    membership_code, _ = request(endpoints["tenants-api"] + "/api/global-administrators?pageSize=100", token=token)
    report = {"eventStoreQuery": query_result, "adminHttpStatus": admin_code,
              "adminResultShape": "array" if isinstance(admin_result, list) else "object" if isinstance(admin_result, dict) else "unexpected",
              "membershipHttpStatus": membership_code}
    Path(directory).parent.joinpath("admin-path.json").write_text(json.dumps(report, indent=2))
    print("Independent Admin path:", report, flush=True)


def verify_api_boundaries(directory, env, endpoints):
    require_navigation_composition(env)
    token = authenticate_administrator(directory, env)
    results = []

    def record(result):
        results.append(result)
        Path(directory).parent.joinpath("api-boundaries.json").write_text(json.dumps(results, indent=2))

    for name in ("tenants-ui", "frontcomposer-ui"):
        sidecar = correlate_sidecar(resource(name, env), resource(name + "-dapr-cli", env), name, native=True)
        invoke_api = sidecar + "/v1.0/invoke/tenants-api/method"
        code, result = request(invoke_api + "/api/tenants", token=token)
        record({"caller": name, "check": "approved-read", "status": code, "expectedStatus": 200})
        require(code == 200 and isinstance(result, dict) and isinstance(result.get("items"), list)
                and type(result.get("hasMore")) is bool,
                "Approved SPIFFE caller/user bearer did not reach an authorized tenant read (HTTP " + str(code) + ").")
        for label, path, method, bearer, expected in (
                ("missing-bearer", "/api/tenants", "GET", None, 401),
                ("invalid-bearer", "/api/tenants", "GET", "invalid.disposable.bearer", 401),
                ("disallowed-verb", "/api/tenants", "POST", token, 403),
                ("disallowed-path", "/health", "GET", token, 403)):
            code, _ = request(invoke_api + path, token=bearer, method=method)
            record({"caller": name, "check": label, "status": code, "expectedStatus": expected})
            require(code == expected, "API caller/route/verb/bearer rejection failed (" + name + "/" + label + ", HTTP " + str(code) + "; expected " + str(expected) + ").")
    outsider = correlate_sidecar(resource("sample", env), resource("sample-dapr-cli", env), "sample", native=True)
    code, _ = request(outsider + "/v1.0/invoke/tenants-api/method/api/tenants", token=token)
    record({"caller": "sample", "check": "unapproved-caller", "status": code, "expectedStatus": 403})
    require(code == 403, "An unapproved API caller was admitted.")
    api_sidecar = correlate_sidecar(resource("tenants-api", env), resource("tenants-api-dapr-cli", env), "tenants-api", native=True)
    for name in ("tenants-ui", "frontcomposer-ui"):
        code, _ = request(api_sidecar + "/v1.0/invoke/" + name + "/method/", token=token)
        record({"receiver": name, "check": "closed-bff", "status": code, "expectedStatus": 403})
        require(code == 403, "A BFF receiver exposed an invocation operation.")
    print("API/BFF boundaries: missing/invalid bearer, unapproved caller, verb/path and BFF invocation refused.", flush=True)


def verify_navigation(directory, env, endpoints):
    require_navigation_composition(env)
    realm = read_owned_realm(resource("security", env)["properties"]["container.id"], load_configuration(directory))
    administrator = next(user for user in realm["users"] if user["username"] == "admin-user")
    navigation_env = {**env, "PLAYWRIGHT_SKIP_WEBSERVER": "1", "FC_FULL_TOPOLOGY_LIVE": "true",
                      "FC_FULL_TOPOLOGY_COMBINED_URL": endpoints["frontcomposer-ui"],
                      "FC_FULL_TOPOLOGY_TENANTS_URL": endpoints["tenants-ui"],
                      "FC_FULL_TOPOLOGY_ADMIN_URL": endpoints["eventstore-admin-ui"],
                      "FC_FULL_TOPOLOGY_AUTHORITY": endpoint(resource("security", env)),
                      "FC_FULL_TOPOLOGY_USERNAME": administrator["username"],
                      "FC_FULL_TOPOLOGY_PASSWORD": administrator["credentials"][0]["value"],
                      "FC_E2E_OUTPUT_DIR": str(Path(directory).parent / "navigation-results"),
                      "FC_E2E_HTML_REPORT_DIR": str(Path(directory).parent / "navigation-report"),
                      "FC_E2E_JSON_PATH": str(Path(directory).parent / "navigation-results/playwright.json"),
                      "FC_E2E_JUNIT_PATH": str(Path(directory).parent / "navigation-results/junit.xml")}
    invoke(["npm", "--prefix", str(ROOT / "tests/e2e"), "exec", "--", "playwright", "test",
            "--config", str(ROOT / "tests/e2e/playwright.config.ts"), "full-topology-readiness.spec.ts",
            "--project=chromium", "--workers=1", "--retries=0", "--reporter=line"], env=navigation_env,
           log=Path(directory).parent / "navigation.log", timeout=300,
           redactions=(navigation_env["FC_FULL_TOPOLOGY_PASSWORD"],))
    print("Authenticated combined UI, Tenants and Admin navigation passed.", flush=True)


def redis(config, *arguments):
    identifier = inspect_container(config, "redis", config.get("containerIds", {}).get("redis"))["Id"]
    # The credential is supplied only on stdin, never argv, logs or a token artifact.
    return invoke(["docker", "exec", "-i", identifier, "sh", "-c",
                   'IFS= read -r REDISCLI_AUTH; export REDISCLI_AUTH; exec redis-cli "$@"',
                   "sh", "--raw", *arguments], input=(config["redisPassword"] + "\n").encode()).decode().strip()


def redis_fields(value):
    # redis-cli's JSON output represents XINFO maps as objects; retain support
    # for explicit RESP pair arrays without weakening the required field values.
    if isinstance(value, dict):
        return value
    require(isinstance(value, list) and len(value) % 2 == 0
            and all(isinstance(key, str) for key in value[::2])
            and len(set(value[::2])) == len(value) // 2, "Unexpected Redis infrastructure metadata.")
    return dict(zip(value[::2], value[1::2]))


def verify_pristine_store(config):
    count = redis(config, "DBSIZE")
    keys = json.loads(redis(config, "--json", "KEYS", "*"))
    require(count == "1" and keys == [INFRASTRUCTURE_STREAM]
            and redis(config, "TYPE", INFRASTRUCTURE_STREAM) == "stream",
            "Disposable store contains unexpected state; activation refused.")
    stream = redis_fields(json.loads(redis(config, "--json", "XINFO", "STREAM", INFRASTRUCTURE_STREAM)))
    require(stream.get("length") == 0 and stream.get("entries-added") == 0
            and stream.get("last-generated-id") == "0-0" and stream.get("max-deleted-entry-id") == "0-0",
            "Infrastructure stream has prior message activity; activation refused.")
    groups = json.loads(redis(config, "--json", "XINFO", "GROUPS", INFRASTRUCTURE_STREAM))
    require(isinstance(groups, list) and len(groups) == 1, "Unexpected infrastructure consumer groups.")
    group = redis_fields(groups[0])
    require(group.get("name") == "eventstore" and group.get("pending") == 0
            and group.get("last-delivered-id") == "0-0" and group.get("lag") == 0
            and not redis(config, "HGET", MARKER, "data"),
            "Infrastructure group has progress or pending work; activation refused.")
    return 1


def refuse_ignored_automatic_configuration(path=None):
    # The SDK consumes these inputs even when Git ignores them. Query only their
    # names, excluding generated/cache trees and nested reference checkouts.
    command = ["git", "-C", str(path)] if path is not None else ["git"]
    patterns = [":(glob)**/*.csproj.user", ":(glob)**/*.fsproj.user", ":(glob)**/*.vbproj.user",
                ":(glob)**/*.proj.user", ":(glob)**/Directory.Build.rsp", ":(glob)**/MSBuild.rsp"]
    excluded = [f":(glob,exclude)**/{name}/**" for name in ("bin", "obj", "node_modules", ".aspire")]
    ignored = invoke([*command, "ls-files", "--others", "--ignored", "--exclude-standard", "-z", "--",
                      *patterns, *excluded, ":(glob,exclude)references/**"])
    require(not ignored, "Unexpected ignored automatic build configuration refused before startup.")


def root_untracked_inputs():
    # Git supplies the nonignored worktree boundary. Build/cache directories stay
    # excluded even when represented by a development symlink.
    excluded = [value for name in ("bin", "obj", "node_modules", ".aspire")
                for value in (f":(glob,exclude)**/{name}", f":(glob,exclude)**/{name}/**")]
    raw = invoke(["git", "ls-files", "--others", "--exclude-standard", "-z", "--", ".", *excluded,
                  ":(glob,exclude)references/**"])
    inputs = {}
    for relative in raw.decode().split("\0"):
        if not relative:
            continue
        path = Path(relative)
        if (path.parts[0] not in {"src", "samples", "eng", "tests"}
                and not relative.endswith((".props", ".targets", ".csproj", ".proj", ".slnx", ".user", ".rsp"))
                and path.name.lower() != "nuget.config"
                and relative not in {".gitignore", ".gitmodules", ".gitattributes", ".editorconfig",
                                     "global.json", "aspire.config.json", "package.json", "package-lock.json"}):
            continue
        selected = ROOT / path
        require(not path.is_absolute() and ".." not in path.parts and not selected.is_symlink() and selected.is_file(),
                "Ambiguous untracked build input refused before startup.")
        inputs[relative] = hashlib.sha256(selected.read_bytes()).hexdigest()
    return dict(sorted(inputs.items()))


def source_inputs():
    refuse_ignored_automatic_configuration()
    sources = {}
    for name in ("Hexalith.Builds", "Hexalith.Commons", "Hexalith.EventStore", "Hexalith.Tenants", "Hexalith.Parties", "Hexalith.Memories",
                 "Hexalith.Platform", "Hexalith.PolymorphicSerializations", "Hexalith.AI.Tools"):
        path = ROOT / "references" / name
        commit = invoke(["git", "-C", str(path), "rev-parse", "HEAD"]).decode().strip()
        require(re.fullmatch(r"[0-9a-f]{40}", commit)
                and not invoke(["git", "-C", str(path), "status", "--porcelain"]).strip(),
                "A selected source is dirty; full qualification requires an exact input identity.")
        refuse_ignored_automatic_configuration(path)
        sources[name] = commit
    paths = [*APPHOST.parent.glob("*.cs"), APPHOST, *APPHOST.parent.joinpath("DaprComponents").glob("*.yaml"),
             ROOT / "eng/verify-isolated-readiness.py", ROOT / "eng/FrontComposer.SourceReferences.targets", ROOT / "eng/verify-apphost-invalid-bearer.sh",
             ROOT / "eng/run-epic9-live-proof.sh", ROOT / "tests/e2e/specs/full-topology-readiness.spec.ts",
             *[ROOT / "references/Hexalith.AI.Tools" / name for name in ("hexalith-llm-instructions.md", "hexalith-state-instructions.md", "hexalith-git-instructions.md")]]
    require(sources["Hexalith.Platform"] == "b742b1b7a496ac72d6961ca6db5213d3df42d234", "Canonical Platform source differs from the approved enrollment.")
    working_patch = invoke(["git", "diff", "HEAD", "--", ".gitignore", ".gitattributes", ".gitmodules", ".editorconfig", "_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json", "src", "samples", "eng", "tests", "Directory.Build.props",
                            "Directory.Build.targets", "Directory.Packages.props", ":(glob,icase)**/nuget.config", "global.json",
                            "deps.local.props", "deps.nuget.props", "aspire.config.json", "package.json", "package-lock.json", ":(glob)**/*.user", ":(glob)**/*.rsp"])
    return {"root": invoke(["git", "rev-parse", "HEAD"]).decode().strip(), "sources": sources,
            "workingPatchSha256": hashlib.sha256(working_patch).hexdigest(),
            "untrackedInputSha256": root_untracked_inputs(),
            "nativeRuntimeSha256": hashlib.sha256(NATIVE_RUNTIME.read_bytes()).hexdigest(),
            "inputSha256": {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest() for path in sorted(paths)}}


def verify_source_inputs(expected):
    require(source_inputs() == expected, "Selected full-topology inputs changed during verification; qualification refused.")


def verify_source_commit(source_commit):
    current = invoke(["git", "-C", str(EVENTSTORE), "rev-parse", "HEAD"]).decode().strip()
    require(re.fullmatch(r"[0-9a-f]{40}", source_commit) and current == source_commit
            and not invoke(["git", "-C", str(EVENTSTORE), "status", "--porcelain"]).strip(),
            "Selected EventStore identity changed; cutover refused.")


def activate(directory, env, source_commit):
    config = load_configuration(directory)
    base = verify_gateway(directory, env, True)
    verify_source_commit(source_commit)
    backup_key_count = verify_pristine_store(config)
    # The only server connected to this new password-protected store is this owned gateway.
    # Its four background projection effects are disabled; no command or domain clients are composed.
    require(redis(config, "SAVE") == "OK", "Native Redis backup failed.")
    identifier = inspect_container(config, "redis", config["containerIds"]["redis"])["Id"]
    backup = Path(directory).parent / "pre-cutover.rdb"
    check = invoke(["docker", "exec", identifier, "redis-check-rdb", "/data/dump.rdb"])
    checked_count = re.search(rb"\b(\d+) keys read\b", check)
    require(b"Checksum OK" in check and checked_count is not None and int(checked_count.group(1)) == backup_key_count,
            "Native backup verification did not establish the inventoried snapshot.")
    invoke(["docker", "cp", identifier + ":/data/dump.rdb", str(backup)])
    os.chmod(backup, 0o600)
    digest = hashlib.sha256(backup.read_bytes()).hexdigest()
    original_digest = invoke(["docker", "exec", identifier, "sha256sum", "/data/dump.rdb"]).decode().split()[0]
    require(backup.read_bytes().startswith(b"REDIS") and digest == original_digest and verify_pristine_store(config) == backup_key_count,
            "Backup copy or writer quiescence could not be verified.")
    backup_reference = backup.as_uri() + "#sha256=" + digest
    realm = read_owned_realm(resource("security", env)["properties"]["container.id"], config)
    administrator = next(user for user in realm["users"] if user["username"] == "admin-user")
    auth_base = endpoint(resource("security", env))
    code, authentication = request(auth_base + "/realms/" + realm["realm"] + "/protocol/openid-connect/token", {
        "grant_type": "password", "client_id": "hexalith-eventstore", "username": administrator["username"],
        "password": administrator["credentials"][0]["value"], "scope": "openid profile email"}, form=True)
    require(code == 200 and "access_token" in authentication, "Local administrator authentication failed.")
    require(verify_pristine_store(config) == backup_key_count, "State appeared before authorized activation.")
    verify_source_commit(source_commit)
    code, result = request(base + "/api/v1/admin/projections/delivery-writer-protocol/activate", {
        "cutoverCommit": source_commit, "backupReference": backup_reference,
        "writersQuiesced": True, "retryWorkersQuiesced": True, "downgradeProhibitedAcknowledged": True,
    }, token=authentication["access_token"])
    require(code == 200 and result.get("status") == "Activated", "Authenticated writer activation did not succeed.")
    marker = json.loads(redis(config, "HGET", MARKER, "data"))
    validate_marker(marker, source_commit)
    server_information = redis(config, "INFO", "server")
    server_version = re.search(r"(?m)^redis_version:([^\r\n]+)", server_information)
    require(server_version is not None, "Redis provider version could not be read.")
    report = {"sourceCommit": source_commit, "owner": config["owner"], "containerIds": config["containerIds"],
              "daprRuntimeVersion": config["daprVersion"], "containerImageIds": config["images"],
              "redisServerVersion": server_version.group(1), "stateComponent": {"type": "state.redis", "version": "v1"},
              "backupReference": backup_reference, "backupVerifiedNoDomainOrControlData": True,
              "verifiedBackupKeyCount": backup_key_count, "neverPublishedInfrastructureStreams": 1,
              "workersDisabledDuringMaintenance": True,
              "marker": marker, "activationHttpStatus": code}
    private_file(Path(directory) / "activation.json", json.dumps({"owner": config["owner"], "freshStoreVerified": True, **marker}))
    Path(directory).parent.joinpath("maintenance-result.json").write_text(json.dumps(report, indent=2))
    print("Native backup checksum/no domain or control state and writer quiescence verified; authenticated activation 200; persisted v2 marker verified.", flush=True)


def processes(env):
    return json.loads(invoke(["aspire", "ps", "--format", "Json", "--non-interactive", "--nologo"], env=env))


@contextlib.contextmanager
def owned_apphost(directory, env, *, build=False, applications=None, isolated=True):
    phase = "full" if applications == FULL_APPLICATIONS else "maintenance"
    lock_id = hashlib.sha256(str(APPHOST).encode()).hexdigest()
    with open(Path(os.environ.get("FC_EPIC9_LOCK_ROOT", "/tmp")) / ("hexalith-epic9-" + lock_id + ".lock"), "w") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        require(not any(item["appHostPath"].endswith("/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj") for item in processes(env)),
                "Another FrontComposer AppHost is running; refusing ownership.")
        full = env.get("FrontComposerVerification__Topology") == "full"
        if full:
            prepare_instance(directory)
        if build:
            invoke(["dotnet", "build", str(APPHOST), "--configuration", "Debug", "--disable-build-servers", "-m:1",
                    "-p:BuildInParallel=false", "-p:NuGetAudit=false", "-p:CentralPackageTransitivePinningEnabled=false"], env=env,
                   log=Path(directory).parent / "source-build.log", timeout=1200)
            require(not any(item["appHostPath"].endswith("/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj") for item in processes(env)),
                    "A FrontComposer AppHost appeared during the build; refusing startup.")
        pid = None
        attempted = False
        rejected = False
        try:
            attempted = True
            raw = invoke(["aspire", "start", "--apphost", str(APPHOST), *(["--isolated"] if isolated else []), "--no-build", "--format", "Json", "--non-interactive", "--nologo"],
                         env=env, log=Path(directory).parent / (phase + "-start.log")).decode()
            try:
                start = json.loads(raw[re.search(r"(?m)^\s*\{", raw).start():])
                require(isinstance(start, dict), "Invalid start record.")
            except (AttributeError, ValueError, RuntimeError):
                rejected = True
                raise RuntimeError("Successful start returned no valid ownership record.") from None
            rejected = start.get("appHostPath") != str(APPHOST) or type(start.get("appHostPid")) is not int or start["appHostPid"] <= 0
            require(not rejected, "Unexpected AppHost ownership record.")
            pid = start["appHostPid"]
            matching = [item for item in processes(env) if item["appHostPath"] == str(APPHOST)]
            rejected = len(matching) != 1 or matching[0]["appHostPid"] != pid
            require(not rejected, "Started AppHost identity was not confirmed.")
            control_plane = ["dapr-sentry", "dapr-placement-mtls", "dapr-scheduler-mtls"] if full and env.get("FrontComposerVerification__Maintenance") == "false" else ["verification-placement", "verification-scheduler"]
            for name in ["verification-redis", *control_plane, "security", *(applications or ("eventstore",))]:
                timeout = "90" if name.startswith("verification-") else "300"
                invoke(["aspire", "wait", name, "--status", "healthy", "--timeout", timeout, "--apphost", str(APPHOST), "--non-interactive", "--nologo"],
                       env=env, log=Path(directory).parent / (phase + "-wait-" + name + ".log"))
            matching = [item for item in processes(env) if item["appHostPath"] == str(APPHOST)]
            rejected = len(matching) != 1 or matching[0]["appHostPid"] != pid
            require(not rejected, "AppHost identity changed before maintenance.")
            yield
        finally:
            matching = [item for item in processes(env) if item["appHostPath"] == str(APPHOST)]
            if matching:
                require(attempted and not rejected and len(matching) == 1 and (pid is None or matching[0]["appHostPid"] == pid),
                        "AppHost ownership changed; refusing cleanup.")
                invoke(["aspire", "stop", "--apphost", str(APPHOST), "--non-interactive", "--nologo"], env=env,
                       log=Path(directory).parent / (phase + "-stop.log"))
                require(not any(item["appHostPath"] == str(APPHOST) for item in processes(env)), "Owned AppHost cleanup failed.")
            if full:
                cleanup_runtime_phase(directory, env)


def create_configuration(directory, topology="gateway"):
    sockets = []
    try:
        for _ in range(4):
            for attempt in range(100):
                connection = socket.socket()
                try:
                    # Docker Desktop may reserve high Windows host ports even when the
                    # corresponding Linux socket is free. Use the AppHost's low-port approach.
                    connection.bind(("127.0.0.1", 10000 + secrets.randbelow(10000)))
                    sockets.append(connection)
                    break
                except OSError:
                    connection.close()
            else:
                raise RuntimeError("No unoccupied verification port is available.")
        ports = dict(zip([*TARGETS, "daprHttp"], [connection.getsockname()[1] for connection in sockets]))
    finally:
        for connection in sockets:
            connection.close()
    version = invoke([str(Path.home() / ".dapr/bin/daprd"), "--version"]).decode().strip()
    require(re.fullmatch(r"1\.18\.\d+", version), "Installed Dapr runtime cannot be identified.")
    require(topology in {"gateway", "full"}, "Unsupported verification topology.")
    config = {"owner": secrets.token_hex(16), "instance": secrets.token_hex(16), "ports": ports, "daprVersion": version, "topology": topology,
              "redisPassword": secrets.token_hex(32), "images": {}}
    for name in TARGETS:
        image = "redis:7.4-alpine" if name == "redis" else "daprio/dapr:" + version
        try:
            config["images"][name] = json.loads(invoke(["docker", "image", "inspect", image]))[0]["Id"]
        except VerificationCancelled:
            raise
        except RuntimeError:
            raise RuntimeError(f"Required local image {image} is unavailable; ensure Docker access and provision that image before verification. No image was pulled.") from None
        require(not container_present(name=container_name(config, name)), "Verification container name already exists.")
    if topology == "full":
        config["images"]["sentry"] = json.loads(invoke(["docker", "image", "inspect", "daprio/sentry:" + version]))[0]["Id"]
        (directory / "mtls").mkdir(mode=0o700)
    private_file(directory / "configuration.json", json.dumps(config))
    private_file(directory / "redis.conf", f"bind 0.0.0.0\nprotected-mode yes\nrequirepass {config['redisPassword']}\nsave \"\"\ndir /data\ndbfilename dump.rdb\n")
    components = directory / "components"
    runtime = directory / "runtime"
    components.mkdir(mode=0o700)
    runtime.mkdir(mode=0o700)
    (directory / "empty").mkdir(mode=0o700)
    scopes = {"statestore": ["eventstore", "eventstore-admin", "tenants", "parties"],
              "pubsub": ["eventstore", "tenants", "parties"], "configstore": ["eventstore"]}
    for name, component_type in {"statestore": "state.redis", "pubsub": "pubsub.redis", "configstore": "configuration.redis"}.items():
        metadata = [{"name": "redisHost", "value": "127.0.0.1:" + str(ports["redis"])},
                    {"name": "redisPassword", "value": config["redisPassword"]}]
        if name == "statestore":
            metadata += [{"name": "actorStateStore", "value": "true"}, {"name": "keyPrefix", "value": "none"}]
        # JSON is YAML-compatible and permits strict validation without a second parser.
        component = {"apiVersion": "dapr.io/v1alpha1", "kind": "Component", "metadata": {"name": name},
                     "spec": {"type": component_type, "version": "v1", "metadata": metadata},
                     "scopes": scopes[name] if topology == "full" else ["eventstore"]}
        private_file(components / (name + ".yaml"), json.dumps(component))
    for filename in ACCESS_CONTROL_FILES:
        access = (ROOT / "src/Hexalith.FrontComposer.AppHost/DaprComponents" / filename).read_text()
        require(access.count('connectionString: "nr.db"') == 1, "Discovery isolation template changed.")
        private_file(directory / filename, access.replace('connectionString: "nr.db"', "connectionString: " + json.dumps(str(runtime / "nr.db"))))
    return config


def require_full_process_facilities():
    try:
        current = PROC_ROOT / str(os.getpid())
        require(PROC_ROOT.is_dir() and current.is_dir(), "Full topology requires readable procfs process metadata.")
        os.readlink(current / "exe")
        for name in ("cmdline", "environ"):
            with current.joinpath(name).open("rb") as source:
                source.read(1)
    except OSError:
        raise RuntimeError("Full topology requires readable procfs process metadata.") from None


def run(topology="gateway"):
    refuse_inherited_isolation_escapes(os.environ)
    os.umask(0o077)
    require(os.name == "posix", "Disposable readiness runner requires a POSIX host.")
    if topology == "full":
        require_full_process_facilities()
    artifacts = Path(tempfile.mkdtemp(prefix="story13-6-isolated-readiness-"))
    directory = artifacts / "private"
    directory.mkdir(mode=0o700)
    print("Disposable readiness diagnostics:", artifacts, flush=True)
    config = None
    topology_attempted = False
    env = {**os.environ, "HexalithFrontComposerFromSource": "true", "UseHexalithProjectReferences": "true", "FrontComposerVerification__Enabled": "true",
           "FrontComposerVerification__Directory": str(directory), "FrontComposerVerification__Maintenance": "true",
           "FrontComposerVerification__Topology": topology}
    try:
        inputs = source_inputs() if topology == "full" else None
        config = create_configuration(directory) if topology == "gateway" else create_configuration(directory, topology)
        if inputs is not None:
            (artifacts / "source-inputs.json").write_text(json.dumps(inputs, indent=2))
        source_commit = invoke(["git", "-C", str(EVENTSTORE), "rev-parse", "HEAD"]).decode().strip()
        require(not invoke(["git", "-C", str(EVENTSTORE), "status", "--porcelain"]).strip(), "Selected EventStore source is dirty; exact cutover identity unavailable.")
        if inputs is not None:
            verify_source_inputs(inputs)
        topology_attempted = True
        with owned_apphost(directory, env, build=True, applications=selected_applications(config, maintenance=True)):
            config = load_configuration(directory)
            config["containerIds"] = {name: inspect_container(config, name)["Id"] for name in TARGETS}
            (directory / "configuration.json").write_text(json.dumps(config))
            if inputs is not None:
                verify_source_inputs(inputs)
            activate(directory, env, source_commit)
        env["FrontComposerVerification__Maintenance"] = "false"
        if topology == "full":
            remove_plain_control_plane(directory)
            verify_source_inputs(inputs)
            verify_activated_source(directory, source_commit)
            with owned_apphost(directory, env, applications=FULL_APPLICATIONS, isolated=False):
                endpoints = verify_full_topology(directory, env, source_commit)
                wait_for_query_catalog(directory, env)
                diagnose_admin_path(directory, env, endpoints)
                verify_api_boundaries(directory, env, endpoints)
                ensure_administrator_membership(directory, env, endpoints)
                verify_navigation(directory, env, endpoints)
                verify_source_inputs(inputs)
        # Existing rejection and Epic 9 helpers retain their own lifecycle/identity
        # checks and run unchanged against the explicitly selected composition.
        if topology == "full": prepare_instance(directory)
        try:
            invoke([str(ROOT / "eng/verify-apphost-invalid-bearer.sh")], env=env, log=artifacts / "invalid-bearer.log")
        finally:
            if topology == "full": cleanup_runtime_phase(directory, env)
        lock_id = hashlib.sha256(str(APPHOST).encode()).hexdigest()
        lock_path = Path(env.get("FC_EPIC9_LOCK_ROOT", "/tmp")) / ("hexalith-epic9-" + lock_id + ".lock")
        invoke(["flock", "--wait", "30", str(lock_path), "true"])
        env["FC_EPIC9_ARTIFACT_ROOT"] = str(artifacts / "epic9")
        env["FC_EPIC9_REQUIRE_CLEAN"] = "false"
        if topology == "full": prepare_instance(directory)
        try:
            invoke([str(ROOT / "eng/run-epic9-live-proof.sh")], env=env, log=artifacts / "epic9.log", timeout=1800)
        finally:
            if topology == "full": cleanup_runtime_phase(directory, env)
        if inputs is not None:
            verify_source_inputs(inputs)
        print("Isolated readiness, four-route rejection and unchanged Epic 9 proof passed.", flush=True)
    finally:
        if topology_attempted:
            require(not any(item["appHostPath"] == str(APPHOST) for item in processes(env)),
                    "A remaining AppHost prevents safe infrastructure cleanup.")
        if topology_attempted and topology == "full":
            cleanup_runtime_phase(directory, env)
        for name in TARGETS if topology_attempted else []:
            if container_present(name=container_name(config, name)):
                container = inspect_container(config, name, config.get("containerIds", {}).get(name))
                identifier = container["Id"]
                invoke(["docker", "rm", "--force", "--volumes", identifier], log=artifacts / ("cleanup-" + name + ".log"))
                require(not container_present(identifier=identifier), "Owned infrastructure cleanup failed.")
        shutil.rmtree(directory)
        print("Owned disposable infrastructure and private configuration removed.", flush=True)


def main():
    with cancellation_handlers():
        try:
            if len(sys.argv) == 3 and sys.argv[1] == "check":
                verify_gateway(Path(sys.argv[2]), os.environ.copy(), False)
            else:
                parser = argparse.ArgumentParser(description=__doc__)
                parser.add_argument("--topology", choices=("gateway", "full"), default="gateway")
                arguments = parser.parse_args()
                run() if arguments.topology == "gateway" else run(arguments.topology)
        except (RuntimeError, OSError, ValueError, KeyError, StopIteration, AttributeError, TypeError, IndexError,
                subprocess.TimeoutExpired) as failure:
            # Shape errors may contain credentials in their exception text. Emit only
            # their type; RuntimeError messages are controlled, bounded diagnostics.
            message = str(failure) if isinstance(failure, RuntimeError) else type(failure).__name__
            print("::error::Disposable readiness verification failed: " + message, file=sys.stderr)
            return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
