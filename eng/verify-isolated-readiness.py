#!/usr/bin/env python3
"""Owned disposable maintenance rehearsal; never accepts a caller-supplied datastore."""
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
TERMINATION_GRACE_SECONDS = 10


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


class VerificationCancelled(RuntimeError):
    pass


@contextlib.contextmanager
def cancellation_handlers():
    def cancel(signum, frame):
        # A second cancellation must not interrupt the owned finally cleanup.
        for value in (signal.SIGINT, signal.SIGTERM):
            signal.signal(value, signal.SIG_IGN)
        raise VerificationCancelled("Verification cancelled; owned cleanup requested.")

    previous = {value: signal.signal(value, cancel) for value in (signal.SIGINT, signal.SIGTERM)}
    try:
        yield
    finally:
        for value, handler in previous.items():
            signal.signal(value, handler)


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


def invoke(arguments, *, env=None, input=None, log=None, timeout=600):
    with subprocess.Popen(arguments, cwd=ROOT, env=env, stdin=subprocess.PIPE,
                          stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True) as process:
        try:
            output, errors = process.communicate(input=input, timeout=timeout)
        except BaseException:
            # Give shell EXIT/TERM traps time to stop their exact owned AppHost.
            output, errors = terminate_child(process)
            if log:
                Path(log).write_bytes(output + errors)
            raise
        if log:
            Path(log).write_bytes(output + errors)
        suffix = "; private diagnostics retained." if log else "."
        require(process.returncode == 0, f"{Path(arguments[0]).name} operation failed" + suffix)
        return output


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


def request(url, data=None, token=None, form=False):
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
        with opener.open(urllib.request.Request(url, data=data, headers=headers), timeout=20) as response:
            return response.status, json.load(response)
    except urllib.error.HTTPError as failure:
        try:
            payload = json.loads(failure.read())
        except json.JSONDecodeError:
            payload = {}
        failure.close()
        return failure.code, payload


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


def read_owned_realm(identifier):
    containers = json.loads(invoke(["docker", "inspect", identifier]))
    require(len(containers) == 1 and containers[0]["Id"] == identifier, "Keycloak container identity changed.")
    # The selected Aspire Keycloak integration copies imports with WithContainerFiles;
    # it does not bind-mount the original private directory. Read only the known copy
    # in the exact container discovered from this owned AppHost; never log its body.
    return json.loads(invoke(["docker", "exec", identifier, "cat",
                              "/opt/keycloak/data/import/hexalith-realm.json"]))


def verify_workload_identity(env):
    security = resource("security", env)
    realm = read_owned_realm(security["properties"]["container.id"])
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


def correlate_sidecar(gateway, sidecar):
    require(sidecar.get("displayName") == "eventstore-dapr-cli" and sidecar.get("state") == "Running"
            and Path(sidecar.get("properties", {}).get("executable.path", "")).name == "dapr",
            "The gateway's owned sidecar is not running.")
    environment = gateway.get("environment", {})
    for name, variable in [("http", "DAPR_HTTP_PORT"), ("grpc", "DAPR_GRPC_PORT")]:
        listener = endpoint({"urls": [url for url in sidecar.get("urls", []) if url.get("name") == name]}, "http")
        port = urllib.parse.urlparse(listener).port
        require(str(environment.get(variable)) == str(port), "Gateway and owned sidecar listener do not match.")
    return endpoint({"urls": [url for url in sidecar["urls"] if url.get("name") == "http"]}, "http")


def verify_gateway(directory, env, maintenance):
    config = load_configuration(directory)
    for name in TARGETS:
        inspect_container(config, name, config.get("containerIds", {}).get(name))
    gateway = resource("eventstore", env)
    environment = gateway.get("environment", {})
    require((str(environment.get(WORKERS)).lower() == "false") if maintenance else WORKERS not in environment,
            "Gateway worker configuration does not match the verification phase.")
    # Aspire --isolated can reassign requested sidecar ports. Correlate both actual
    # listeners with the exact sidecar discovered under the owned AppHost instead.
    sidecar_base = correlate_sidecar(gateway, resource("eventstore-dapr-cli", env))
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
    verify_workload_identity(env)
    return base


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
    realm = json.loads((ROOT / "src/Hexalith.FrontComposer.AppHost/KeycloakRealms/hexalith-realm.json").read_text())
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
    Path(directory).parent.joinpath("maintenance-result.json").write_text(json.dumps(report, indent=2))
    print("Native backup checksum/no domain or control state and writer quiescence verified; authenticated activation 200; persisted v2 marker verified.", flush=True)


def processes(env):
    return json.loads(invoke(["aspire", "ps", "--format", "Json", "--non-interactive", "--nologo"], env=env))


@contextlib.contextmanager
def owned_apphost(directory, env, *, build=False):
    lock_id = hashlib.sha256(str(APPHOST).encode()).hexdigest()
    with open(Path(os.environ.get("FC_EPIC9_LOCK_ROOT", "/tmp")) / ("hexalith-epic9-" + lock_id + ".lock"), "w") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        require(not any(item["appHostPath"].endswith("/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj") for item in processes(env)),
                "Another FrontComposer AppHost is running; refusing ownership.")
        if build:
            invoke(["dotnet", "build", str(APPHOST), "--configuration", "Debug", "--disable-build-servers", "-m:1",
                    "-p:BuildInParallel=false", "-p:NuGetAudit=false", "-p:CentralPackageTransitivePinningEnabled=false"], env=env,
                   log=Path(directory).parent / "source-build.log", timeout=1200)
        pid = None
        attempted = False
        rejected = False
        try:
            attempted = True
            raw = invoke(["aspire", "start", "--apphost", str(APPHOST), "--isolated", "--no-build", "--format", "Json", "--non-interactive", "--nologo"],
                         env=env, log=Path(directory).parent / "maintenance-start.log").decode()
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
            for name in ["verification-redis", "verification-placement", "verification-scheduler", "security", "eventstore"]:
                timeout = "90" if name.startswith("verification-") else "300"
                invoke(["aspire", "wait", name, "--status", "healthy", "--timeout", timeout, "--apphost", str(APPHOST), "--non-interactive", "--nologo"],
                       env=env, log=Path(directory).parent / ("maintenance-wait-" + name + ".log"))
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
                       log=Path(directory).parent / "maintenance-stop.log")
                require(not any(item["appHostPath"] == str(APPHOST) for item in processes(env)), "Owned AppHost cleanup failed.")


def create_configuration(directory):
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
    config = {"owner": secrets.token_hex(16), "ports": ports, "daprVersion": version,
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
    private_file(directory / "configuration.json", json.dumps(config))
    private_file(directory / "redis.conf", f"bind 0.0.0.0\nprotected-mode yes\nrequirepass {config['redisPassword']}\nsave \"\"\ndir /data\ndbfilename dump.rdb\n")
    components = directory / "components"
    runtime = directory / "runtime"
    components.mkdir(mode=0o700)
    runtime.mkdir(mode=0o700)
    for name, component_type in {"statestore": "state.redis", "pubsub": "pubsub.redis", "configstore": "configuration.redis"}.items():
        extra = "    - name: actorStateStore\n      value: \"true\"\n    - name: keyPrefix\n      value: none\n" if name == "statestore" else ""
        private_file(components / (name + ".yaml"), f"apiVersion: dapr.io/v1alpha1\nkind: Component\nmetadata:\n  name: {name}\nspec:\n  type: {component_type}\n  version: v1\n  metadata:\n    - name: redisHost\n      value: 127.0.0.1:{ports['redis']}\n    - name: redisPassword\n      value: \"{config['redisPassword']}\"\n{extra}scopes:\n  - eventstore\n")
    access = (ROOT / "src/Hexalith.FrontComposer.AppHost/DaprComponents/accesscontrol.yaml").read_text()
    require(access.count('connectionString: "nr.db"') == 1, "Discovery isolation template changed.")
    private_file(directory / "accesscontrol.yaml", access.replace('connectionString: "nr.db"', "connectionString: " + json.dumps(str(runtime / "nr.db"))))
    return config


def run():
    os.umask(0o077)
    require(os.name == "posix", "Disposable readiness runner requires a POSIX host.")
    artifacts = Path(tempfile.mkdtemp(prefix="story13-6-isolated-readiness-"))
    directory = artifacts / "private"
    directory.mkdir(mode=0o700)
    print("Disposable readiness diagnostics:", artifacts, flush=True)
    config = None
    topology_attempted = False
    env = {**os.environ, "HexalithFrontComposerFromSource": "true", "FrontComposerVerification__Enabled": "true",
           "FrontComposerVerification__Directory": str(directory), "FrontComposerVerification__Maintenance": "true"}
    try:
        config = create_configuration(directory)
        source_commit = invoke(["git", "-C", str(EVENTSTORE), "rev-parse", "HEAD"]).decode().strip()
        require(not invoke(["git", "-C", str(EVENTSTORE), "status", "--porcelain"]).strip(), "Selected EventStore source is dirty; exact cutover identity unavailable.")
        topology_attempted = True
        with owned_apphost(directory, env, build=True):
            config["containerIds"] = {name: inspect_container(config, name)["Id"] for name in TARGETS}
            (directory / "configuration.json").write_text(json.dumps(config))
            activate(directory, env, source_commit)
        env["FrontComposerVerification__Maintenance"] = "false"
        invoke([str(ROOT / "eng/verify-apphost-invalid-bearer.sh")], env=env, log=artifacts / "invalid-bearer.log")
        lock_id = hashlib.sha256(str(APPHOST).encode()).hexdigest()
        lock_path = Path(env.get("FC_EPIC9_LOCK_ROOT", "/tmp")) / ("hexalith-epic9-" + lock_id + ".lock")
        invoke(["flock", "--wait", "30", str(lock_path), "true"])
        env["FC_EPIC9_ARTIFACT_ROOT"] = str(artifacts / "epic9")
        env["FC_EPIC9_REQUIRE_CLEAN"] = "false"
        invoke([str(ROOT / "eng/run-epic9-live-proof.sh")], env=env, log=artifacts / "epic9.log", timeout=1800)
        print("Isolated readiness, four-route rejection and unchanged Epic 9 proof passed.", flush=True)
    finally:
        if topology_attempted:
            require(not any(item["appHostPath"] == str(APPHOST) for item in processes(env)),
                    "A remaining AppHost prevents safe infrastructure cleanup.")
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
            elif len(sys.argv) == 1:
                run()
            else:
                raise RuntimeError("Unsupported disposable verification invocation.")
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
