#!/usr/bin/env python3
"""Build and execute the finite private Stryker compatibility variant.

The official 5.1.0 manifest pin is its upstream base, not the identity of this
executable. No third-party tool is published, and no mutation options are exposed.
"""
from __future__ import annotations

import argparse
import base64
import ctypes
import hashlib
import json
import os
import signal
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile
import tempfile
import time
import urllib.request

DEFINITION = "eng/stryker-compat/definition.json"
PATCH = "eng/stryker-compat/static-reset-compatibility.patch"
INSPECTOR = "eng/stryker-compat/inspect-metadata.cs"
UPSTREAM = "7052fa6c646a6bdfb143608e695d11542e2be815"
PATCH_SHA = "b631f905b07a85b408ddbbe4b2f0ff062a38538c32d6c0ed5fb47e2b2d3da19c"
ARCHIVE_SHA = "0ec3bf04498243ba5d0816754a5a379d19596ca4f2c717387fb96a5d8ce741f2"
LICENSE_SHA = "0d542e0c8804e39aa7f37eb00da5a762149dc682d7829451287e11b938e94594"
LICENSE = "eng/stryker-compat/LICENSE"
CUSTOM_VERSION = "5.1.0-hexalith.compat.1"
INFORMATIONAL_VERSION = CUSTOM_VERSION + "+upstream.7052fa6c.patch.b631f905b07a"
CLI_PROJECT = "src/Stryker.CLI/Stryker.CLI/Stryker.CLI.csproj"
PROJECT = "src/Hexalith.FrontComposer.SourceTools"
TEST_PROJECT = "tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj"
MANIFEST = "tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json"
SEGMENTS = ("happy-path", "error-handling")
ASSEMBLIES = ("Stryker.CLI.dll", "Stryker.Core.dll", "Stryker.TestRunner.MicrosoftTestPlatform.dll")


class CompatibilityError(RuntimeError):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise CompatibilityError(message)


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def canonical(data: bytes) -> bytes:
    return data.replace(b"\r\n", b"\n")


def json_digest(value: object) -> str:
    return digest(json.dumps(value, sort_keys=True, separators=(",", ":")).encode())


def file_hash(path: Path) -> str:
    require(path.is_file() and not path.is_symlink(), "Missing or symlinked authenticated input")
    return digest(path.read_bytes())


def read_json(path: Path) -> dict:
    require(path.is_file() and not path.is_symlink(), "Missing or symlinked JSON input")
    value = json.loads(path.read_text(encoding="utf-8"))
    require(isinstance(value, dict), "Expected a JSON object")
    return value


def save_json(path: Path, value: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + ".new")
    temporary.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


class PreparationCancelled(BaseException):
    def __init__(self, number: int):
        self.number = number


_cancel_signal: int | None = None


def preparation_signal(number: int, _frame: object) -> None:
    global _cancel_signal
    if _cancel_signal is None:
        _cancel_signal = number
        raise PreparationCancelled(number)


def live_session(session: int) -> bool:
    for path in Path("/proc").glob("[0-9]*/stat"):
        try:
            fields = path.read_text().rsplit(")", 1)[1].split()
            if int(fields[3]) == session and fields[0] != "Z":
                return True
        except (OSError, ValueError, IndexError):
            continue
    return False


def finish_preparation_process(process: subprocess.Popen) -> None:
    # Only preparation subprocesses receive a private session. Actual mutation
    # execution below uses exec and retains its original process group/session.
    def record_signal(number: int, _frame: object) -> None:
        global _cancel_signal
        _cancel_signal = _cancel_signal or number
    previous = {number: signal.signal(number, record_signal) for number in (signal.SIGINT, signal.SIGTERM)}
    try:
        if live_session(process.pid):
            try:
                os.killpg(process.pid, signal.SIGTERM)
            except ProcessLookupError:
                pass
            deadline = time.monotonic() + 5
            while live_session(process.pid) and time.monotonic() < deadline:
                time.sleep(0.05)
            if live_session(process.pid):
                try:
                    os.killpg(process.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
        process.wait()
        # The preparation process is a child subreaper. Reap any grandchildren
        # left by a completed build; never touch another job's process/session.
        while True:
            try:
                child, _status = os.waitpid(-process.pid, os.WNOHANG)
            except ChildProcessError:
                break
            if child == 0:
                time.sleep(0.05)
        require(not live_session(process.pid), "Owned preparation work remained after cleanup")
        for stream in (process.stdout, process.stderr):
            if stream is not None:
                stream.close()
    finally:
        for number, handler in previous.items():
            signal.signal(number, handler)


def command(args: list[str], *, cwd: Path, env: dict | None = None,
            capture: bool = False, timeout: int = 600) -> str:
    if env is None:
        result = subprocess.run(args, cwd=cwd, check=True, timeout=30,
                                stdout=subprocess.PIPE if capture else None, text=capture)
        return result.stdout.strip() if capture else ""
    process = None
    try:
        # Defer cancellation during spawn without blocking signals inherited
        # by the new process. The assigned child is then always cleaned/joined.
        def record_spawn_signal(number: int, _frame: object) -> None:
            global _cancel_signal
            _cancel_signal = _cancel_signal or number
        previous = {number: signal.signal(number, record_spawn_signal) for number in (signal.SIGINT, signal.SIGTERM)}
        try:
            process = subprocess.Popen(args, cwd=cwd, env=env, start_new_session=True,
                                       stdout=subprocess.PIPE if capture else None, text=capture)
        finally:
            for number, handler in previous.items():
                signal.signal(number, handler)
        if _cancel_signal is not None:
            raise PreparationCancelled(_cancel_signal)
        output, _ = process.communicate(timeout=timeout)
        result = process.returncode
    finally:
        if process is not None:
            finish_preparation_process(process)
    if _cancel_signal is not None:
        raise PreparationCancelled(_cancel_signal)
    if result != 0:
        raise subprocess.CalledProcessError(result, args)
    return output.strip() if capture else ""


def load_definition(root: Path) -> tuple[dict, dict]:
    definition = read_json(root / DEFINITION)
    require(set(definition) == {"schema", "upstream_version", "upstream_commit", "upstream_tree", "archive_url",
                               "archive_sha256", "archive_size", "license_sha256", "patch_sha256", "custom_version",
                               "informational_version", "sdk", "target_framework", "patched_source_files", "html_library"},
            "Unexpected compatibility definition members")
    require(definition["archive_sha256"] == ARCHIVE_SHA and definition["archive_size"] == 1360457
            and definition["license_sha256"] == LICENSE_SHA
            and definition["upstream_tree"] == "d86c4a3163fe0d1ef91a24624aed176eb3c3a40d", "Unapproved source archive/license/tree")
    expected_paths = {"src/Stryker.TestRunner.MicrosoftTestPlatform/MicrosoftTestingPlatformRunner.cs",
                      "src/Stryker.TestRunner.MicrosoftTestPlatform.UnitTest/MicrosoftTestingPlatformRunnerTests.cs",
                      "src/Stryker.TestRunner.MicrosoftTestPlatform.UnitTest/MicrosoftTestingPlatformRunnerIsolationCompatibilityTests.cs"}
    require(isinstance(definition["patched_source_files"], dict)
            and set(definition["patched_source_files"]) == expected_paths, "Unapproved patch path set")
    for row in definition["patched_source_files"].values():
        require(isinstance(row, dict) and set(row) == {"before", "after"}
                and isinstance(row["after"], str) and len(row["after"]) == 64
                and all(character in "0123456789abcdef" for character in row["after"])
                and (row["before"] is None or (isinstance(row["before"], str) and len(row["before"]) == 64
                     and all(character in "0123456789abcdef" for character in row["before"]))),
                "Unexpected patch file checksum shape")
    require(definition["html_library"] == {
        "version": "3.9.0", "metadata_url": "https://data.jsdelivr.com/v1/package/npm/mutation-testing-elements@3.9.0/flat",
        "asset_url": "https://cdn.jsdelivr.net/npm/mutation-testing-elements@3.9.0/dist/mutation-test-elements.js",
        "asset_sha256": "751fb010242b0b44e32d84fe7fe0b9ff1da182823b94f59f5c52b001fcfc163b"}, "Unapproved HTML library input")
    require(definition.get("schema") == "hexalith.stryker-compatibility.v1", "Unsupported compatibility schema")
    require(definition.get("upstream_version") == "5.1.0" and definition.get("upstream_commit") == UPSTREAM,
            "Unsupported upstream compiler/runner semantics")
    require(definition.get("patch_sha256") == PATCH_SHA and definition.get("custom_version") == CUSTOM_VERSION
            and definition.get("informational_version") == INFORMATIONAL_VERSION, "Unsupported compatibility identity")
    require(definition.get("archive_url") == f"https://codeload.github.com/stryker-mutator/stryker-net/tar.gz/{UPSTREAM}",
            "Unexpected source archive endpoint")
    require(definition.get("sdk") == "11.0.100" and definition.get("target_framework") == ".NETCoreApp,Version=v10.0",
            "Unsupported build SDK or target framework")
    official = read_json(root / ".config/dotnet-tools.json")
    require(official["tools"]["dotnet-stryker"]["version"] == definition["upstream_version"], "Official base pin drift")
    inputs = {path: digest(canonical((root / path).read_bytes())) for path in (DEFINITION, PATCH, INSPECTOR, LICENSE, "eng/stryker_compat.py")}
    require(inputs[PATCH] == PATCH_SHA, "Compatibility patch checksum mismatch")
    require(inputs[LICENSE] == LICENSE_SHA, "Tracked upstream license checksum mismatch")
    return definition, inputs


def tool_root(inputs: dict[str, str]) -> Path:
    # Only the current job's scratch root can select the executable; there is no
    # arbitrary runtime/receipt-path argument or fallback to a global tool.
    key = "RUNNER_TEMP" if os.environ.get("GITHUB_ACTIONS") == "true" else "TMPDIR"
    base = os.environ.get(key)
    require(bool(base) and Path(base).is_absolute() and Path(base).is_dir(), f"Existing private {key} is required")
    return Path(base).resolve() / ("hfcst-" + json_digest(inputs)[:16])


def runtime_files(directory: Path) -> dict[str, str]:
    require(directory.is_dir() and not directory.is_symlink(), "Missing private tool output")
    all_paths = sorted(directory.rglob("*"))
    require(all(not path.is_symlink() for path in all_paths), "Symlinked private runtime input")
    paths = [path for path in all_paths if not path.is_dir()]
    require(0 < len(paths) <= 1000, "Private runtime file inventory is empty or exceeds its bound")
    require(sum(path.stat().st_size for path in paths) <= 200 * 1024 * 1024, "Private runtime exceeds its byte bound")
    return {path.relative_to(directory).as_posix(): file_hash(path) for path in paths}


def verify_metadata(rows: list, definition: dict) -> None:
    require(isinstance(rows, list) and len(rows) == len(ASSEMBLIES) and all(isinstance(row, dict) for row in rows), "Missing compiled assembly metadata")
    require({row.get("file") for row in rows} == set(ASSEMBLIES), "Compiled assembly identity set differs")
    for row in rows:
        require(row.get("assemblyName") == row["file"][:-4] and row.get("assemblyVersion") == "5.1.0.0",
                "Compiled assembly name/version differs")
        values = row.get("values", {})
        require(isinstance(values, dict), "Malformed compiled attribute metadata")
        require(values.get("System.Reflection.AssemblyInformationalVersionAttribute") == definition["informational_version"]
                and values.get("System.Runtime.Versioning.TargetFrameworkAttribute") == definition["target_framework"],
                "Actual compiled informational version or framework differs")


def verify_build(directory: Path, definition: dict, inputs: dict) -> dict:
    require(directory.is_dir() and not directory.is_symlink(), "Missing or symlinked private build directory")
    receipt = read_json(directory / "build-provenance.json")
    require(set(receipt) == {"schema", "definition_sha256", "authenticated_inputs", "upstream_commit", "custom_version",
                            "informational_version", "sdk", "runtimes", "source_lockfiles", "resolved_package_assets",
                            "html_inputs", "assembly_metadata", "runtime_files", "build_digest"}, "Unexpected build receipt members")
    unsigned = {key: value for key, value in receipt.items() if key != "build_digest"}
    require(receipt["schema"] == "hexalith.stryker-private-build.v1" and receipt["build_digest"] == json_digest(unsigned),
            "Build provenance digest mismatch")
    require(receipt["authenticated_inputs"] == inputs and receipt["definition_sha256"] == inputs[DEFINITION], "Stale build inputs")
    for field in ("upstream_commit", "custom_version", "informational_version", "sdk"):
        require(receipt[field] == definition[field], "Build provenance identity differs")
    require(receipt["runtime_files"] == runtime_files(directory / "runtime"), "Private runtime file checksum/inventory differs")
    require(set(ASSEMBLIES) <= set(receipt["runtime_files"]), "Missing CLI/Core/MTP output")
    verify_metadata(receipt["assembly_metadata"], definition)
    return receipt


def download(url: str, path: Path, maximum: int, expected: str | None = None) -> bytes:
    request = urllib.request.Request(url, headers={"User-Agent": "Hexalith-Private-Stryker-Compatibility/1.0"})
    with urllib.request.urlopen(request, timeout=120) as response:
        data = response.read(maximum + 1)
    require(len(data) <= maximum and (expected is None or digest(data) == expected), "Downloaded input checksum/size mismatch")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)
    return data


def extract_archive(path: Path, destination: Path, definition: dict) -> None:
    require(file_hash(path) == definition["archive_sha256"] and path.stat().st_size == definition["archive_size"],
            "Source archive checksum/size mismatch before extraction")
    prefix = "stryker-net-" + UPSTREAM
    with tarfile.open(path) as archive:
        for member in archive:
            if member.name == prefix:
                continue
            require(member.name.startswith(prefix + "/") and (member.isfile() or member.isdir()), "Unsafe source archive member")
            member.name = member.name[len(prefix) + 1:]
            require(not Path(member.name).is_absolute() and ".." not in Path(member.name).parts, "Source archive path escapes")
            archive.extract(member, destination, filter="data")


def prepare(root: Path, definition: dict, inputs: dict, directory: Path) -> dict:
    if directory.exists() or directory.is_symlink():
        return verify_build(directory, definition, inputs)
    require(sys.platform == "linux", "Private mutation tool preparation requires Linux")
    # An interrupted attempt can only leave an unselected staging path. A valid
    # receipt and its runtime become visible at the selected path together.
    with tempfile.TemporaryDirectory(prefix=f".{directory.name}-", dir=directory.parent) as staging_root:
        staging = Path(staging_root) / "build"
        staging.mkdir()
        receipt = _prepare_build(root, definition, inputs, staging)
        if _cancel_signal is not None:
            raise PreparationCancelled(_cancel_signal)
        try:
            publish_build(staging, directory)
        except FileExistsError:
            # Another preparation published first. Verify the selected winner.
            return verify_build(directory, definition, inputs)
        return receipt


def publish_build(staging: Path, directory: Path) -> None:
    # Linux rename ordinarily replaces an empty destination directory. Publish
    # only if the destination is absent, including when another process races us.
    libc = ctypes.CDLL(None, use_errno=True)
    rename_noreplace = getattr(libc, "renameat2", None)
    require(rename_noreplace is not None, "Atomic no-replace publication requires renameat2")
    at_fdcwd = -100
    rename_noreplace_flag = 1
    if rename_noreplace(at_fdcwd, os.fsencode(staging), at_fdcwd, os.fsencode(directory), rename_noreplace_flag) != 0:
        error = ctypes.get_errno()
        raise OSError(error, os.strerror(error), directory)


def _prepare_build(root: Path, definition: dict, inputs: dict, directory: Path) -> dict:
    # Local process-only supervision; no host settings or privileges are changed.
    libc = ctypes.CDLL(None, use_errno=True)
    require(libc.prctl(36, 1, 0, 0, 0) == 0, "Preparation child-subreaper setup failed")
    env = os.environ.copy()
    env.update(MSBUILDDISABLENODEREUSE="1", DOTNET_CLI_USE_MSBUILD_SERVER="0", UseSharedCompilation="false",
               DOTNET_NOLOGO="1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1")
    for key, relative in {"TMPDIR": "tmp", "TMP": "tmp", "TEMP": "tmp", "NUGET_PACKAGES": "nuget",
                          "NUGET_HTTP_CACHE_PATH": "http-cache", "DOTNET_CLI_HOME": "cli-home", "XDG_DATA_HOME": "xdg-data",
                          "XDG_CACHE_HOME": "xdg-cache", "XDG_CONFIG_HOME": "xdg-config"}.items():
        folder = directory / relative
        folder.mkdir(exist_ok=True)
        env[key] = str(folder)
    env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    # Control SDK resolution without changing the authenticated upstream archive.
    save_json(directory / "global.json", {"sdk": {"version": definition["sdk"], "rollForward": "disable"}})
    sdk = command(["dotnet", "--version"], cwd=directory, env=env, capture=True, timeout=30)
    require(sdk == definition["sdk"], "Resolved SDK differs from the exact build definition")
    runtimes = [line.split()[:2] for line in command(["dotnet", "--list-runtimes"], cwd=directory, env=env, capture=True, timeout=30).splitlines()]
    archive = directory / "source.tar.gz"
    download(definition["archive_url"], archive, 20 * 1024 * 1024, definition["archive_sha256"])
    source = directory / "stryker-source"
    extract_archive(archive, source, definition)
    require(file_hash(source / "LICENSE") == definition["license_sha256"], "Upstream license checksum differs")
    pristine = {path.relative_to(source).as_posix(): file_hash(path) for path in source.rglob("*") if path.is_file()}
    for path, row in definition["patched_source_files"].items():
        require(pristine.get(path) == row["before"], "Pristine patch target checksum differs")
    applied_patch = directory / "compatibility.patch"
    applied_patch.write_bytes(canonical((root / PATCH).read_bytes()))
    command(["patch", "--batch", "--fuzz=0", "-p1", "-i", str(applied_patch)], cwd=source, env=env, timeout=30)
    patched = {path.relative_to(source).as_posix(): file_hash(path) for path in source.rglob("*") if path.is_file()}
    changed = {path for path in pristine.keys() | patched.keys() if pristine.get(path) != patched.get(path)}
    require(changed == set(definition["patched_source_files"]), "Patch changes an unapproved source path")
    for path, row in definition["patched_source_files"].items():
        require(patched[path] == row["after"], "Applied patch target checksum differs")
    html = definition["html_library"]
    cache = directory / "xdg-data/.librarymanager/cache/jsdelivr/mutation-testing-elements"
    metadata = download(html["metadata_url"], cache / "3.9.0-filelist.json", 200 * 1024)
    asset = download(html["asset_url"], cache / "3.9.0/dist/mutation-test-elements.js", 1024 * 1024, html["asset_sha256"])
    entries = [row for row in json.loads(metadata)["files"] if row.get("name") == "/dist/mutation-test-elements.js"]
    require(len(entries) == 1 and base64.b64decode(entries[0]["hash"]).hex() == digest(asset), "HTML library metadata/asset disagree")
    properties = ["-p:Version=" + definition["custom_version"], "-p:InformationalVersion=" + definition["informational_version"],
                  "-p:RepositoryCommit=" + UPSTREAM, "-p:PathMap=" + str(directory) + "=/_/HexalithStrykerCompat"]
    artifacts = directory / "build-artifacts"
    command(["dotnet", "restore", CLI_PROJECT, "--source", "https://api.nuget.org/v3/index.json", "--locked-mode",
             "--disable-parallel", "-m:1", "-nr:false", "-p:UseSharedCompilation=false", "--artifacts-path", str(artifacts), *properties], cwd=source, env=env)
    command(["dotnet", "build", CLI_PROJECT, "-c", "Release", "--no-restore", "-m:1", "-nr:false", "-p:UseSharedCompilation=false",
             "--artifacts-path", str(artifacts), *properties], cwd=source, env=env)
    resource = source / "src/Stryker.Core/Stryker.Core/Reporters/Html/Files/dist/mutation-test-elements.js"
    require(file_hash(resource) == html["asset_sha256"], "Actual compiled HTML resource input differs")
    locks = {path: patched[path] for path in patched if path.endswith("packages.lock.json")}
    require(all(file_hash(source / path) == sha for path, sha in locks.items()), "Locked package inputs changed during restore")
    shutil.copytree(artifacts / "bin/Stryker.CLI/release", directory / "runtime")
    inspector = directory / "metadata-inspector"
    inspector.mkdir()
    (inspector / "Program.cs").write_bytes((root / INSPECTOR).read_bytes())
    (inspector / "Inspect.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net11.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>\n')
    command(["dotnet", "build", "Inspect.csproj", "-c", "Release", "--nologo", "-m:1", "-nr:false", "-p:UseSharedCompilation=false", "-p:RestoreSources=https://api.nuget.org/v3/index.json"], cwd=inspector, env=env, timeout=120)
    metadata_rows = json.loads(command(["dotnet", str(inspector / "bin/Release/net11.0/Inspect.dll"),
                                      *[str(directory / "runtime" / name) for name in ASSEMBLIES]], cwd=inspector, env=env, capture=True, timeout=30))
    verify_metadata(metadata_rows, definition)
    receipt = {"schema": "hexalith.stryker-private-build.v1", "definition_sha256": inputs[DEFINITION],
               "authenticated_inputs": inputs, "upstream_commit": UPSTREAM, "custom_version": CUSTOM_VERSION,
               "informational_version": INFORMATIONAL_VERSION, "sdk": sdk, "runtimes": runtimes, "source_lockfiles": locks,
               "resolved_package_assets": {path.relative_to(artifacts).as_posix(): file_hash(path)
                                           for path in artifacts.rglob("project.assets.json")},
               "html_inputs": {"metadata_sha256": digest(metadata), "asset_sha256": digest(asset),
                               "libman_manifest_sha256": file_hash(source / "src/Stryker.Core/Stryker.Core/libman.json")},
               "assembly_metadata": metadata_rows, "runtime_files": runtime_files(directory / "runtime")}
    receipt["build_digest"] = json_digest(receipt)
    save_json(directory / "build-provenance.json", receipt)
    return verify_build(directory, definition, inputs)


def git(root: Path, *args: str) -> bytes:
    return subprocess.check_output(["git", *args], cwd=root)


def invocation(root: Path, directory: Path, segment: str, build: dict) -> tuple[list[str], dict]:
    require(segment in SEGMENTS, "Unsupported mutation segment")
    rows = [row for row in read_json(root / MANIFEST)["segments"] if row["name"] == segment]
    require(len(rows) == 1, "Expected one exact mutation segment definition")
    config = f"tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-{segment}.json"
    require(rows[0]["config"] == config, "Segment config path differs")
    require(canonical((root / config).read_bytes()) == canonical(git(root, "show", "HEAD:" + config)), "Uncommitted segment config")
    output = "artifacts/mutation/" + segment
    receipt = {"schema": "hexalith.stryker-execution.v1", "source_commit": git(root, "rev-parse", "HEAD").decode().strip(),
               "segment": segment, "config": config, "config_sha256": file_hash(root / config),
               "config_git_blob": git(root, "rev-parse", "HEAD:" + config).decode().strip(), "output": output,
               "invoked_dll": "Stryker.CLI.dll", "invoked_dll_sha256": build["runtime_files"]["Stryker.CLI.dll"],
               "build": build}
    args = ["dotnet", str(directory / "runtime/Stryker.CLI.dll"), "--test-runner", "mtp", "--verbosity", "debug",
            "--break-on-initial-test-failure", "--project", "Hexalith.FrontComposer.SourceTools.csproj",
            "--test-project", "../../" + TEST_PROJECT, "--config-file", "../../" + config, "--output", "../../" + output]
    return args, receipt


def verify_source(root: Path, inputs: dict) -> None:
    command(["git", "diff", "--quiet", "--ignore-submodules=none", "HEAD"], cwd=root)
    untracked = git(root, "ls-files", "--others", "--exclude-standard")
    require(not untracked, "Untracked source/test inputs cannot claim an exact source commit")
    for path, sha in inputs.items():
        require(digest(canonical(git(root, "show", "HEAD:" + path))) == sha, "Uncommitted compatibility input")


def verify_segment(root: Path, directory: Path, segment: str, definition: dict, inputs: dict) -> dict:
    build = verify_build(directory, definition, inputs)
    verify_source(root, inputs)
    _, expected = invocation(root, directory, segment, build)
    receipt = read_json(root / expected["output"] / "tool-provenance.json")
    require(receipt == expected, "Segment source/config/output/executable provenance differs")
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("prepare")
    for name in ("run", "verify"):
        segment_command = commands.add_parser(name)
        segment_command.add_argument("segment", choices=SEGMENTS)
    args = parser.parse_args()
    root = Path(__file__).resolve().parent.parent
    definition, inputs = load_definition(root)
    directory = tool_root(inputs)
    if args.command == "prepare":
        previous = {number: signal.signal(number, preparation_signal) for number in (signal.SIGINT, signal.SIGTERM)}
        try:
            build = prepare(root, definition, inputs, directory)
            if _cancel_signal is not None:
                raise PreparationCancelled(_cancel_signal)
            print(json.dumps({"private_tool": build["informational_version"], "build_digest": build["build_digest"]}))
            return 0
        finally:
            for number, handler in previous.items():
                signal.signal(number, handler)
    if args.command == "verify":
        print(json.dumps(verify_segment(root, directory, args.segment, definition, inputs)))
        return 0
    build = verify_build(directory, definition, inputs)
    verify_source(root, inputs)
    command_args, receipt = invocation(root, directory, args.segment, build)
    output = root / receipt["output"]
    require(not output.exists() or (output.is_dir() and not output.is_symlink() and not any(output.iterdir())),
            "Existing segment evidence cannot be reused as a fresh execution")
    save_json(output / "tool-provenance.json", receipt)
    os.chdir(root / PROJECT)
    # Preserve the actual tool's process, group, cancellation and exit. This is
    # pre-execution provenance and does not claim completion or a mutation score.
    os.execvp(command_args[0], command_args)
    return 1


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except PreparationCancelled as cancellation:
        raise SystemExit(128 + cancellation.number)
    except (CompatibilityError, OSError, ValueError, subprocess.SubprocessError) as error:
        if _cancel_signal is not None:
            raise SystemExit(128 + _cancel_signal)
        print("Private Stryker compatibility refused: " + str(error), file=sys.stderr)
        raise SystemExit(1)
