#!/usr/bin/env python3
"""Run the copyable, candidate-bound FrontComposer adopter fixture."""

from __future__ import annotations

import argparse
import base64
import datetime as dt
import hashlib
import http.client
import io
import json
import os
from pathlib import Path
import re
import shutil
import socket
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
from zipfile import BadZipFile, ZipFile


PACKAGE_IDS = (
    "Hexalith.FrontComposer.Contracts",
    "Hexalith.FrontComposer.Contracts.UI",
    "Hexalith.FrontComposer.Shell",
    "Hexalith.FrontComposer.SourceTools",
)
REPOSITORY = "https://github.com/Hexalith/Hexalith.FrontComposer"
SHA = re.compile(r"[0-9a-f]{40}\Z")
VERSION = re.compile(r"[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?\Z")
RUNTIME = re.compile(r"[0-9]+\.[0-9]+\.[0-9]+\Z")
FIXTURE = Path(__file__).resolve().parents[1] / "samples" / "AdopterProofKit" / "v1"
NEGATIVE_DIAGNOSTICS = {
    "missing-quickstart": "FrontComposer bootstrap is incomplete: AddHexalithEventStore(...) was called but AddHexalithFrontComposerQuickstart()",
    "misordered": "FrontComposer bootstrap is mis-ordered: AddHexalithEventStore(...) was called before AddHexalithFrontComposerQuickstart().",
}
PROJECTION_FRAGMENTS = (
    "Proof projection",
    "fc-projection-empty-placeholder",
)
PROJECTION_NAV_TAG = re.compile(
    r'<fluent-menu-item\b(?=[^>]*\bdata-testid="fc-nav-flyout-projection-Proof-ProofProjection")'
    r'(?=[^>]*\bdata-href="/proof/proof-projection")[^>]*>'
)


class ProofError(Exception):
    """A named, allowlisted proof failure without private diagnostic content."""

    def __init__(self, code: str):
        super().__init__(code)
        self.code = code


def verify_packages(source: Path, candidate: str, version: str) -> dict[str, str]:
    """Bind every directly consumed package to the exact candidate via its nuspec."""
    if not SHA.fullmatch(candidate):
        raise ProofError("invalid-candidate-sha")
    if not VERSION.fullmatch(version):
        raise ProofError("invalid-package-version")
    if not source.is_dir():
        raise ProofError("package-source-unavailable")
    archive_hashes = {}
    for package_id in PACKAGE_IDS:
        matches = list(source.glob(f"{package_id}.{version}.nupkg"))
        if len(matches) != 1:
            raise ProofError("package-missing")
        try:
            archive_bytes = matches[0].read_bytes()
            with ZipFile(io.BytesIO(archive_bytes)) as archive:
                nuspecs = [name for name in archive.namelist() if name.endswith(".nuspec")]
                if len(nuspecs) != 1:
                    raise ProofError("package-identity-invalid")
                root = ET.fromstring(archive.read(nuspecs[0]))
            metadata = next(child for child in root if child.tag.rsplit("}", 1)[-1] == "metadata")
            fields = {child.tag.rsplit("}", 1)[-1]: child for child in metadata}
            repository = fields.get("repository")
            if (fields["id"].text != package_id
                    or fields["version"].text != version
                    or repository is None
                    or repository.get("url", "").rstrip("/").lower() != REPOSITORY.lower()
                    or repository.get("commit") != candidate):
                raise ProofError("package-candidate-mismatch")
            archive_hashes[package_id] = base64.b64encode(
                hashlib.sha512(archive_bytes).digest()).decode("ascii")
        except (OSError, BadZipFile, KeyError, ValueError, RuntimeError, ET.ParseError, StopIteration):
            raise ProofError("package-identity-invalid") from None
    return archive_hashes


def verify_restored_packages(cache: Path, version: str, archive_hashes: dict[str, str]) -> None:
    """Ensure restore used the exact archives whose candidate provenance was checked."""
    for package_id in PACKAGE_IDS:
        path = cache / package_id.lower() / version.lower() / f"{package_id.lower()}.{version.lower()}.nupkg.sha512"
        try:
            restored_hash = path.read_text(encoding="ascii").strip()
        except OSError:
            raise ProofError("restored-package-missing") from None
        if restored_hash != archive_hashes[package_id]:
            raise ProofError("restored-package-mismatch")


def verify_runtime(dotnet: str, selected: str, cwd: Path) -> dict[str, str]:
    """Require the exact installed SDK and both shared frameworks used by the host."""
    if not RUNTIME.fullmatch(selected):
        raise ProofError("runtime-identity-invalid")
    try:
        sdk = subprocess.check_output([dotnet, "--version"], cwd=cwd, timeout=15,
                                      text=True, stderr=subprocess.DEVNULL).strip()
        listing = subprocess.check_output([dotnet, "--list-runtimes"], cwd=cwd, timeout=15,
                                          text=True, stderr=subprocess.DEVNULL)
    except (OSError, subprocess.SubprocessError):
        raise ProofError("runtime-unavailable") from None
    if not RUNTIME.fullmatch(sdk):
        raise ProofError("runtime-identity-invalid")
    for name in ("Microsoft.NETCore.App", "Microsoft.AspNetCore.App"):
        if not re.search(rf"^{re.escape(name)} {re.escape(selected)} \[", listing, re.MULTILINE):
            raise ProofError("runtime-identity-mismatch")
    return {"dotnet_sdk": sdk, "netcore": selected, "aspnetcore": selected}


def pin_host_runtime(assembly: Path, selected: str) -> None:
    """Require both host frameworks at the selected patch with no roll-forward."""
    path = assembly.with_suffix(".runtimeconfig.json")
    try:
        config = json.loads(path.read_text(encoding="utf-8"))
        options = config["runtimeOptions"]
        frameworks = options["frameworks"]
        if not isinstance(frameworks, list) or {entry["name"] for entry in frameworks} != {
                "Microsoft.NETCore.App", "Microsoft.AspNetCore.App"}:
            raise ValueError("unexpected framework list")
        for entry in frameworks:
            entry["version"] = selected
        options["rollForward"] = "Disable"
        path.write_text(json.dumps(config, indent=2) + "\n", encoding="utf-8")
    except (OSError, json.JSONDecodeError, KeyError, TypeError, ValueError):
        raise ProofError("runtime-config-invalid") from None


def _port() -> int:
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        return listener.getsockname()[1]


def _get(url: str) -> str | None:
    try:
        with urllib.request.urlopen(url, timeout=2) as response:
            if response.status != 200:
                return None
            return response.read(2_000_000).decode("utf-8", errors="replace")
    except (OSError, urllib.error.URLError, http.client.IncompleteRead):
        return None


def _start_and_probe(dotnet: str, assembly: Path, runtime: str, endpoint: str,
                     mode: str, path: str, expected: tuple[str, ...] = ()) -> bool:
    port = _port()
    env = os.environ.copy()
    env.update({
        "ASPNETCORE_ENVIRONMENT": "Production",
        "ASPNETCORE_URLS": f"http://127.0.0.1:{port}",
        "ADOPTER_PROOF_BOOTSTRAP": mode,
        "ADOPTER_PROOF_EVENTSTORE_ENDPOINT": endpoint,
        "DOTNET_ROLL_FORWARD": "Disable",
    })
    with tempfile.TemporaryFile(mode="w+t") as output:
        process = subprocess.Popen(
            [dotnet, "exec", "--fx-version", runtime, str(assembly)],
            cwd=assembly.parent, env=env, stdout=output, stderr=subprocess.STDOUT, text=True,
        )
        try:
            deadline = time.monotonic() + 30
            while time.monotonic() < deadline:
                page = _get(f"http://127.0.0.1:{port}{path}")
                if page is not None:
                    if not expected:
                        raise ProofError("bootstrap-unexpected-render")
                    return all(fragment in page for fragment in expected) and (
                        path != "/proof/proof-projection" or PROJECTION_NAV_TAG.search(page) is not None)
                if process.poll() is not None:
                    break
                time.sleep(0.2)
            if expected:
                raise ProofError("host-start-or-render-failed")
            exit_code = process.poll()
            if exit_code is None:
                raise ProofError("bootstrap-did-not-exit")
            output.seek(0)
            diagnostic = output.read()
            if exit_code == 0 or NEGATIVE_DIAGNOSTICS[mode] not in diagnostic:
                raise ProofError("bootstrap-diagnostic-missing")
            return True
        finally:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=5)


def execute(args: argparse.Namespace) -> dict:
    """Execute every matrix case with an isolated package cache and copied fixture."""
    candidate = args.candidate_sha
    version = args.package_version
    result = {
        "schema": "frontcomposer-adopter-proof/v1",
        "execution_date": dt.datetime.now(dt.timezone.utc).date().isoformat(),
        "candidate_sha": candidate if SHA.fullmatch(candidate) else "invalid",
        "package_identity": {"version": version if VERSION.fullmatch(version) else "invalid",
                             "ids": list(PACKAGE_IDS), "archive_sha512": {}, "verified": False},
        "runtime_identity": None,
        "surfaces": {"projection": "ProofProjectionView@/proof/proof-projection",
                     "command": "CreateProofCommandPage@/commands/Proof/CreateProofCommand"},
        "assertions": {"projection_rendered": False, "command_rendered": False,
                       "bootstrap_rejected": False, "empty_registry_rendered": False},
        "result": "fail",
        "failure": None,
    }
    try:
        archive_hashes = verify_packages(Path(args.package_source), candidate, version)
        if not args.eventstore_endpoint.startswith(("https://", "http://")):
            raise ProofError("eventstore-endpoint-invalid")
        with tempfile.TemporaryDirectory(prefix="frontcomposer-adopter-proof-") as temp:
            root = Path(temp)
            fixture = root / "fixture"
            shutil.copytree(FIXTURE, fixture, ignore=shutil.ignore_patterns("bin", "obj"))
            result["runtime_identity"] = verify_runtime(args.dotnet, args.runtime_version, fixture)
            config = root / "nuget.config"
            import xml.sax.saxutils as xml
            config.write_text(
                '<?xml version="1.0"?><configuration><packageSources><clear />'
                f'<add key="candidate" value={xml.quoteattr(str(Path(args.package_source).resolve()))} />'
                '<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />'
                '</packageSources><packageSourceMapping>'
                '<packageSource key="candidate"><package pattern="Hexalith.FrontComposer.*" /></packageSource>'
                '<packageSource key="nuget.org"><package pattern="*" /></packageSource>'
                '</packageSourceMapping></configuration>', encoding="utf-8")
            env = os.environ.copy()
            env["NUGET_PACKAGES"] = str(root / "packages")
            project = fixture / "AdopterProofKit.Web" / "AdopterProofKit.Web.csproj"
            commands = (
                [args.dotnet, "restore", str(project), "--configfile", str(config),
                 f"-p:FrontComposerPackageVersion={version}", "-p:NuGetAudit=false"],
                [args.dotnet, "build", str(project), "-c", "Release", "--no-restore",
                 f"-p:FrontComposerPackageVersion={version}"],
            )
            for command in commands:
                try:
                    completed = subprocess.run(command, cwd=fixture, env=env, capture_output=True,
                                               text=True, timeout=180)
                except subprocess.TimeoutExpired:
                    raise ProofError("fixture-command-timeout") from None
                if completed.returncode:
                    raise ProofError("fixture-build-failed")
            verify_restored_packages(root / "packages", version, archive_hashes)
            result["package_identity"]["archive_sha512"] = archive_hashes
            result["package_identity"]["verified"] = True
            assembly = fixture / "AdopterProofKit.Web" / "bin" / "Release" / "net10.0" / "AdopterProofKit.Web.dll"
            pin_host_runtime(assembly, args.runtime_version)
            endpoint = args.eventstore_endpoint
            result["assertions"]["projection_rendered"] = _start_and_probe(
                args.dotnet, assembly, args.runtime_version, endpoint, "three-call", "/proof/proof-projection",
                PROJECTION_FRAGMENTS)
            result["assertions"]["command_rendered"] = _start_and_probe(
                args.dotnet, assembly, args.runtime_version, endpoint, "three-call",
                "/commands/Proof/CreateProofCommand", ("fc-command-form", "Proof label"))
            if not result["assertions"]["projection_rendered"] or not result["assertions"]["command_rendered"]:
                raise ProofError("render-assertion-missing")
            for mode in ("missing-quickstart", "misordered"):
                _start_and_probe(args.dotnet, assembly, args.runtime_version, endpoint, mode, "/")
            result["assertions"]["bootstrap_rejected"] = True
            result["assertions"]["empty_registry_rendered"] = _start_and_probe(
                args.dotnet, assembly, args.runtime_version, endpoint, "empty", "/",
                ("fc-home-empty-no-microservices",))
            if not result["assertions"]["empty_registry_rendered"]:
                raise ProofError("empty-registry-render-missing")
        result["result"] = "pass"
    except ProofError as error:
        result["failure"] = error.code
    except (OSError, subprocess.SubprocessError, ValueError):
        result["failure"] = "proof-execution-failed"
    return result


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--candidate-sha", required=True, help="Exact 40-character FrontComposer Git SHA")
    parser.add_argument("--package-version", required=True, help="Exact version of all four candidate packages")
    parser.add_argument("--package-source", required=True, help="Directory containing the candidate .nupkg files")
    parser.add_argument("--runtime-version", required=True, help="Exact installed .NET and ASP.NET Core runtime patch version")
    parser.add_argument("--eventstore-endpoint", required=True, help="Absolute EventStore base URL; never saved in the result")
    parser.add_argument("--result", required=True, help="Path for the redacted JSON result")
    parser.add_argument("--dotnet", default="dotnet", help="dotnet executable (default: dotnet)")
    args = parser.parse_args(argv)
    result = execute(args)
    path = Path(args.result)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(f"adopter proof: {result['result']} ({result['failure'] or 'all-assertions-passed'})")
    return 0 if result["result"] == "pass" else 1


if __name__ == "__main__":
    sys.exit(main())
