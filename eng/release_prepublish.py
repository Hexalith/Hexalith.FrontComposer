#!/usr/bin/env python3
"""FR24 pre-publication orchestration for semantic-release (REL-3).

Repository-owned exact-artifact gate. ``prepare`` runs as the semantic-release
``prepareCmd`` and enforces the Required Artifact Invariant strictly in order:

    pack once -> inventory -> tests -> package-consumer validation -> SBOM/symbols
    -> sign + timestamp the exact candidates -> verify signatures -> benchmark
    candidate evidence -> checksums -> prepare/seal/verify manifest (attestation
    bound) -> classify-release --require-publishable

Every phase is fail-closed: a non-zero exit stops semantic-release BEFORE any
publication side effect (NuGet, GitHub Release, tag, changelog). There is no
G1-style record-and-proceed path: missing signing credentials, unsigned packages,
invalid chains, missing timestamps, or an invalid manifest all abort preparation
(REL-3 AC5/AC10).

``publish`` runs as the semantic-release ``publishCmd``: it re-verifies the sealed
manifest and the publishable readiness classification immediately before pushing,
then pushes ONLY the manifest-authorized signed ``.nupkg`` paths and their matching
``.snupkg`` symbol packages. It never rebuilds, repacks, or substitutes artifacts
(AC11). Divergent per-package outcomes are recorded as a partial-publication
incident and fail the release (AC14).

Local, non-publishing validation (REL-3 T6) uses ``prepare --non-publishing``:
the full chain runs identically, classification runs with ``--dry-run true
--dry-run-clean-exit`` so a healthy candidate exits 0 while remaining honestly
non-publishable (``publish_authorized=false`` in a ``local-candidate`` context).
The publish-capable path never uses that carve-out.

Secrets (signing certificate, NuGet API key) are consumed from the environment and
never echoed; failure output is redacted to tool + phase names and evidence
transcripts are path-sanitized before landing under ``release-evidence/``.
"""

from __future__ import annotations

import argparse
import base64
import datetime as _dt
import hashlib
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile

REPO_ROOT = pathlib.Path(__file__).resolve().parents[1]
EVIDENCE_DIR = pathlib.Path("release-evidence")
NUPKGS_DIR = pathlib.Path("nupkgs")
SIGNED_DIR = pathlib.Path("nupkgs-signed")
TEST_RESULTS_DIR = pathlib.Path("TestResults")
INVENTORY_FILE = pathlib.Path("eng/release-package-inventory.json")
SOLUTION = "Hexalith.FrontComposer.slnx"
DEFAULT_TIMESTAMPER = "http://timestamp.digicert.com"

# Mirrors the release test lane previously hosted by release-evidence.yml (G1):
# the seven CI-authoritative test projects, Quarantined excluded.
TEST_PROJECTS = [
    "tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj",
    "tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj",
    "tests/Hexalith.FrontComposer.Contracts.UI.Tests/Hexalith.FrontComposer.Contracts.UI.Tests.csproj",
    "tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj",
    "tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj",
    "tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj",
    "tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj",
]

_PATH_SANITIZER = re.compile(r"/(?:home|Users|tmp|var)/[^\s'\"]*")
_SHA256_RE = re.compile(r"^[a-fA-F0-9]{64}$")


class PhaseFailure(Exception):
    """A fail-closed phase failure. Carries phase name and exit code only (no secrets)."""

    def __init__(self, phase: str, message: str, exit_code: int = 1) -> None:
        super().__init__(message)
        self.phase = phase
        self.exit_code = exit_code


def log(phase: str, message: str) -> None:
    print(f"[release-prepublish] {phase}: {message}", flush=True)


def sanitize_paths(text: str) -> str:
    return _PATH_SANITIZER.sub("<path>", text)


def run(phase: str, cmd: list[str], *, env: dict[str, str] | None = None,
        capture: bool = False, redact_command: bool = False,
        tolerate_failure: bool = False) -> subprocess.CompletedProcess:
    """Run a command fail-closed. With ``redact_command`` only the executable name is logged."""
    shown = cmd[0] if redact_command else " ".join(cmd)
    log(phase, f"run: {sanitize_paths(shown)}")
    merged_env = {**os.environ, **(env or {})}
    result = subprocess.run(
        cmd,
        cwd=REPO_ROOT,
        env=merged_env,
        capture_output=capture,
        text=capture,
        check=False,
    )
    if result.returncode != 0 and not tolerate_failure:
        raise PhaseFailure(phase, f"{cmd[0]} exited {result.returncode}", exit_code=1)
    return result


def write_json(path: pathlib.Path, payload: dict) -> None:
    target = REPO_ROOT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, sort_keys=True, separators=(",", ":"))
        handle.write("\n")


def read_json(path: pathlib.Path) -> dict:
    with (REPO_ROOT / path).open("r", encoding="utf-8") as handle:
        return json.load(handle)


def sha256_file(path: pathlib.Path, base: pathlib.Path | None = None) -> str:
    digest = hashlib.sha256()
    with ((base or REPO_ROOT) / path).open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def packable_rows() -> list[dict]:
    payload = read_json(INVENTORY_FILE)
    return [
        row for row in payload.get("packages", [])
        if isinstance(row, dict) and row.get("packable") is True
    ]


def context_env() -> dict[str, str]:
    return {
        "event_name": os.environ.get("GITHUB_EVENT_NAME", "local"),
        "ref": os.environ.get("GITHUB_REF", "local"),
        "ref_protected": os.environ.get("GITHUB_REF_PROTECTED", "false"),
    }


# ---------------------------------------------------------------------------
# prepare phases
# ---------------------------------------------------------------------------

def phase_build() -> None:
    """Restore + build Release once so pack --no-build and test --no-build can consume it.

    On the domain-release runner the solution is already built; this is an incremental
    no-op there. It is a BUILD prerequisite, not a repack: candidate packages are packed
    exactly once in phase_pack.
    """
    run("build", ["dotnet", "restore", SOLUTION, "-p:Configuration=Release"])
    run("build", [
        "dotnet", "build",
        "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj",
        "-f", "netstandard2.0", "--configuration", "Release", "--no-restore", "-m:1", "/nr:false",
    ])
    run("build", ["dotnet", "build", SOLUTION, "--configuration", "Release", "--no-restore"])


def phase_pack(version: str) -> None:
    for directory in (NUPKGS_DIR, SIGNED_DIR):
        target = REPO_ROOT / directory
        if target.exists():
            shutil.rmtree(target)
    run("pack-once", ["python3", "scripts/pack-release-packages.py", str(NUPKGS_DIR), version])
    # semantic-release version contract: every packable candidate must exist at the
    # supplied version before anything downstream consumes it (semantic_release_state=matches).
    missing = [
        f"{row['package_id']}.{version}.nupkg"
        for row in packable_rows()
        if not (REPO_ROOT / NUPKGS_DIR / f"{row['package_id']}.{version}.nupkg").is_file()
    ]
    if missing:
        raise PhaseFailure("pack-once", f"candidates missing for semantic-release version {version}: {missing}")


def phase_inventory() -> None:
    run("inventory", [
        "python3", "eng/release_evidence.py", "inventory",
        "--root", ".",
        "--expected", str(INVENTORY_FILE),
        "--output", str(EVIDENCE_DIR / "package-inventory.json"),
    ])


def phase_tests() -> None:
    results_root = REPO_ROOT / TEST_RESULTS_DIR
    if results_root.exists():
        shutil.rmtree(results_root)
    for project in TEST_PROJECTS:
        name = pathlib.Path(project).stem
        run("tests", [
            "dotnet", "test", project,
            "--configuration", "Release", "--no-build",
            "--filter", "Category!=Quarantined",
            "--results-directory", str(TEST_RESULTS_DIR / name),
            "--logger", f"trx;LogFileName={name}.trx",
        ], env={"DiffEngine_Disabled": "true"})
    run("tests", [
        "python3", "eng/release_evidence.py", "test-results",
        "--results-dir", str(TEST_RESULTS_DIR),
        "--output", str(EVIDENCE_DIR / "test-results.json"),
    ])


def phase_consumer_validation() -> None:
    checks: list[dict[str, str]] = []
    for script, check_name in (
        ("scripts/validate-nuget-packages.py", "package-metadata-and-kernel-split"),
        ("scripts/validate-consumer-package-references.py", "consumer-boundaries"),
    ):
        run("consumer-validation", ["python3", script, str(NUPKGS_DIR)])
        checks.append({"check": check_name, "script": script, "status": "passed"})
    write_json(EVIDENCE_DIR / "consumer-validation.json", {
        "decision_contract": "frontcomposer.consumer-validation.v1",
        "status": "valid",
        "package_directory": str(NUPKGS_DIR),
        "checks": checks,
    })


def phase_sbom_and_symbols(version: str) -> None:
    probe = run("sbom", ["dotnet", "CycloneDX", "--version"], capture=True, tolerate_failure=True)
    if probe.returncode != 0:
        install = run("sbom", [
            "dotnet", "tool", "install", "--global", "CycloneDX", "--version", "5.*",
        ], tolerate_failure=True)
        if install.returncode != 0:
            raise PhaseFailure("sbom", "CycloneDX tool is unavailable and cannot be installed")
    run("sbom", [
        "dotnet", "CycloneDX", SOLUTION,
        "-o", str(EVIDENCE_DIR), "-fn", "sbom.json", "-j",
    ])
    missing_symbols = [
        f"{row['package_id']}.{version}.snupkg"
        for row in packable_rows()
        if row.get("symbol_required")
        and not (REPO_ROOT / NUPKGS_DIR / f"{row['package_id']}.{version}.snupkg").is_file()
    ]
    if missing_symbols:
        raise PhaseFailure("sbom", f"required symbol packages missing: {missing_symbols}")


def _codesign_bundle_path() -> pathlib.Path:
    sdk_version = subprocess.run(
        ["dotnet", "--version"], capture_output=True, text=True, check=True, cwd=REPO_ROOT,
    ).stdout.strip()
    sdk_list = subprocess.run(
        ["dotnet", "--list-sdks"], capture_output=True, text=True, check=True, cwd=REPO_ROOT,
    ).stdout
    for line in sdk_list.splitlines():
        if line.startswith(f"{sdk_version} "):
            base = line[line.index("[") + 1: line.rindex("]")]
            return pathlib.Path(base) / sdk_version / "trustedroots" / "codesignctl.pem"
    raise PhaseFailure("sign", f"SDK base path not found for version {sdk_version}")


def phase_sign_and_verify(non_publishing: bool) -> None:
    """Sign + RFC 3161 timestamp the exact candidates, then verify. Fail-closed (AC5).

    Unlike G1 there is NO record-and-proceed: absent credentials, unsigned packages,
    invalid chains, or missing timestamps abort preparation before any side effect.
    In ``--non-publishing`` mode the codesignctl.pem trust append is reverted after
    verification so a local validation CA is never left permanently trusted for
    code signing on the developer machine (review BH-9); CI runners are ephemeral
    and keep the plain append.
    """
    cert_base64 = os.environ.get("NUGET_SIGNING_CERTIFICATE_BASE64", "")
    cert_password = os.environ.get("NUGET_SIGNING_CERTIFICATE_PASSWORD", "")
    timestamper = os.environ.get("NUGET_SIGNING_TIMESTAMPER", "") or DEFAULT_TIMESTAMPER
    if not cert_base64:
        write_json(EVIDENCE_DIR / "signing-readiness.json", {
            "decision_contract": "frontcomposer.signing-readiness.v1",
            "signed": False,
            "verified": False,
            "blocking": True,
            "readiness_reason": (
                "signing certificate secret NUGET_SIGNING_CERTIFICATE_BASE64 is not provisioned; "
                "REL-3 pre-publication signing is fail-closed (FR24 AC2/AC5)."
            ),
        })
        raise PhaseFailure("sign", "signing certificate secret is not provisioned; preparation fails closed")

    cert_fd, cert_name = tempfile.mkstemp(suffix=".pfx")
    os.close(cert_fd)
    pem_fd, pem_name = tempfile.mkstemp(suffix=".pem")
    os.close(pem_fd)
    cert_path = pathlib.Path(cert_name)
    root_pem = pathlib.Path(pem_name)
    bundle_backup: bytes | None = None
    bundle: pathlib.Path | None = None
    try:
        cert_path.write_bytes(base64.b64decode(cert_base64))
        # Recover the issuing CA from the PFX chain so Linux `dotnet nuget sign/verify`
        # (which trusts code-signing roots only via the SDK codesignctl.pem fallback
        # bundle) can build and trust the chain. Legacy retry covers PKCS#12 legacy
        # ciphers; a PFX without its chain fails closed on the certificate guard below.
        # The password rides env indirection, never argv (/proc-visible — review BH-10).
        openssl_env = {**os.environ, "FC_SIGNING_CERT_PASSWORD": cert_password}
        recovered = subprocess.run(
            ["openssl", "pkcs12", "-in", str(cert_path), "-cacerts", "-nokeys",
             "-passin", "env:FC_SIGNING_CERT_PASSWORD", "-out", str(root_pem)],
            capture_output=True, check=False, env=openssl_env,
        )
        if recovered.returncode != 0:
            subprocess.run(
                ["openssl", "pkcs12", "-in", str(cert_path), "-cacerts", "-nokeys", "-legacy",
                 "-passin", "env:FC_SIGNING_CERT_PASSWORD", "-out", str(root_pem)],
                capture_output=True, check=False, env=openssl_env,
            )
        if "BEGIN CERTIFICATE" not in root_pem.read_text(encoding="utf-8", errors="replace"):
            raise PhaseFailure(
                "sign",
                "signing PFX does not embed its issuing CA; export it with the root chain "
                "(openssl pkcs12 -certfile root.crt) so verification can trust the code-signing root",
            )
        bundle = _codesign_bundle_path()
        if not bundle.is_file():
            raise PhaseFailure("sign", "NuGet code-signing trust bundle codesignctl.pem not found")
        chain_text = "\n" + root_pem.read_text(encoding="utf-8", errors="replace")
        if non_publishing and os.access(bundle, os.R_OK):
            bundle_backup = bundle.read_bytes()
        if os.access(bundle, os.W_OK):
            with bundle.open("a", encoding="utf-8") as handle:
                handle.write(chain_text)
        else:
            # `sudo -n`: fail immediately with a typed message instead of hanging the
            # run on an interactive password prompt (review BH-9).
            escalated = subprocess.run(
                ["sudo", "-n", "tee", "-a", str(bundle)], input=chain_text.encode("utf-8"),
                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=False,
            )
            if escalated.returncode != 0:
                raise PhaseFailure(
                    "sign",
                    "codesignctl.pem is not writable and passwordless sudo is unavailable; "
                    "grant write access to the SDK trust bundle or run with sudo -n capability",
                )

        signed_root = REPO_ROOT / SIGNED_DIR
        signed_root.mkdir(parents=True, exist_ok=True)
        for package in sorted((REPO_ROOT / NUPKGS_DIR).glob("*.nupkg")):
            shutil.copy2(package, signed_root / package.name)
        for package in sorted(signed_root.glob("*.nupkg")):
            sign_result = subprocess.run(
                ["dotnet", "nuget", "sign", str(package),
                 "--certificate-path", str(cert_path),
                 "--certificate-password", cert_password,
                 "--timestamper", timestamper,
                 "--overwrite"],
                cwd=REPO_ROOT, capture_output=True, text=True, check=False,
            )
            if sign_result.returncode != 0:
                # Redacted: never echo the sign command line (certificate password).
                raise PhaseFailure("sign", f"dotnet nuget sign failed for {package.name}")

        verify_cmd = ["dotnet", "nuget", "verify", "--all", "-v", "normal"]
        verify_cmd.extend(str(p) for p in sorted(signed_root.glob("*.nupkg")))
        verify_result = subprocess.run(
            verify_cmd, cwd=REPO_ROOT, capture_output=True, text=True, check=False,
        )
        transcript = sanitize_paths(verify_result.stdout or "")
        evidence_root = REPO_ROOT / EVIDENCE_DIR
        evidence_root.mkdir(parents=True, exist_ok=True)
        (evidence_root / "signing-verification.txt").write_text(
            transcript, encoding="utf-8", newline="\n")
        if verify_result.returncode != 0:
            write_json(EVIDENCE_DIR / "signing-readiness.json", {
                "decision_contract": "frontcomposer.signing-readiness.v1",
                "signed": True,
                "verified": False,
                "blocking": True,
                "readiness_reason": "dotnet nuget verify --all failed for signed candidates (FR24 AC5).",
            })
            raise PhaseFailure("sign", "signature/timestamp verification failed for signed candidates")
        write_json(EVIDENCE_DIR / "signing-readiness.json", {
            "decision_contract": "frontcomposer.signing-readiness.v1",
            "signed": True,
            "verified": True,
            "blocking": False,
            "readiness_reason": "packages signed and verified (dotnet nuget verify --all, RFC 3161 timestamp).",
        })
    finally:
        cert_path.unlink(missing_ok=True)
        root_pem.unlink(missing_ok=True)
        if bundle_backup is not None and bundle is not None:
            # Non-publishing runs restore the pristine trust bundle after
            # verification (review BH-9); best-effort with an explicit warning so a
            # failed restore is never silent.
            try:
                if os.access(bundle, os.W_OK):
                    bundle.write_bytes(bundle_backup)
                else:
                    restore = subprocess.run(
                        ["sudo", "-n", "tee", str(bundle)], input=bundle_backup,
                        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=False,
                    )
                    if restore.returncode != 0:
                        log("sign", "warning: could not restore codesignctl.pem; the local validation CA remains appended")
            except OSError:
                log("sign", "warning: could not restore codesignctl.pem; the local validation CA remains appended")


def phase_benchmark() -> None:
    artifacts = REPO_ROOT / "artifacts" / "benchmark"
    artifacts.mkdir(parents=True, exist_ok=True)
    run("benchmark", [
        "python3", "eng/llm_benchmark.py", "validate-prompt-set",
        "--root", ".", "--output", "artifacts/benchmark/prompt-set.json",
    ])
    budget = run("benchmark", [
        "python3", "eng/llm_benchmark.py", "budget-status",
        "--output", "artifacts/benchmark/budget.json",
    ], tolerate_failure=True)
    if budget.returncode != 0:
        log("benchmark", "budget not available; recording candidate evidence without provider spend")
    bench = run("benchmark", [
        "python3", "eng/llm_benchmark.py", "run-benchmark",
        "--root", ".",
        "--budget-artifact", "artifacts/benchmark/budget.json",
        "--output", str(EVIDENCE_DIR / "benchmark-summary.json"),
    ], tolerate_failure=True)
    if bench.returncode != 0:
        log("benchmark", "recorded as candidate evidence (budget-blocked / no provider spend)")
    if not (REPO_ROOT / EVIDENCE_DIR / "benchmark-summary.json").is_file():
        raise PhaseFailure("benchmark", "benchmark-summary.json was not produced")


def phase_checksums() -> None:
    cmd = ["python3", "eng/release_evidence.py", "checksums", "--root", "."]
    for pattern in (
        "nupkgs-signed/*.nupkg",
        "nupkgs/*.nupkg",
        "nupkgs/*.snupkg",
        "release-evidence/sbom.json",
        "release-evidence/test-results.json",
        "release-evidence/package-inventory.json",
        "release-evidence/consumer-validation.json",
        "release-evidence/benchmark-summary.json",
        "release-evidence/signing-verification.txt",
    ):
        cmd.extend(["--pattern", pattern])
    cmd.extend(["--output", str(EVIDENCE_DIR / "checksums.json")])
    run("checksums", cmd)


def phase_manifest(version: str, tag: str) -> None:
    sbom_hash = sha256_file(EVIDENCE_DIR / "sbom.json")
    benchmark_hash = sha256_file(EVIDENCE_DIR / "benchmark-summary.json")
    attestation_status = os.environ.get("RELEASE_ATTESTATION_STATUS", "approved-unsupported")
    attestation_bundle = os.environ.get("RELEASE_ATTESTATION_BUNDLE", "")
    prepare_cmd = [
        "python3", "eng/release_evidence.py", "prepare-manifest",
        "--inventory", str(EVIDENCE_DIR / "package-inventory.json"),
        "--checksums", str(EVIDENCE_DIR / "checksums.json"),
        "--version", version,
        "--tag", tag,
        "--root", ".",
        "--sbom-hash", sbom_hash,
        "--benchmark-summary-hash", benchmark_hash,
        "--attestation-status", attestation_status,
        "--signing-verification", str(EVIDENCE_DIR / "signing-verification.txt"),
        "--diagnostics-output", str(EVIDENCE_DIR / "manifest-diagnostics.json"),
        "--output", str(EVIDENCE_DIR / "pre-manifest.json"),
    ]
    if attestation_bundle:
        prepare_cmd.extend(["--attestation-bundle", attestation_bundle])
    run("manifest", prepare_cmd)
    run("manifest", [
        "python3", "eng/release_evidence.py", "seal-manifest",
        "--manifest", str(EVIDENCE_DIR / "pre-manifest.json"),
        "--output", str(EVIDENCE_DIR / "sealed-manifest.json"),
    ])
    run("manifest", [
        "python3", "eng/release_evidence.py", "verify-manifest",
        "--manifest", str(EVIDENCE_DIR / "sealed-manifest.json"),
        "--root", ".",
        "--output", str(EVIDENCE_DIR / "release-verification.json"),
    ])
    incident_path = REPO_ROOT / EVIDENCE_DIR / "partial-publish-incident.json"
    if not incident_path.is_file():
        run("manifest", [
            "python3", "eng/release_evidence.py", "partial-publish-incident",
            "--manifest", str(EVIDENCE_DIR / "sealed-manifest.json"),
            "--output", str(EVIDENCE_DIR / "partial-publish-incident.json"),
            "--phase", "none",
            "--classification", "none",
        ])


def _validation_fallback_args() -> list[str]:
    """Local-validation AC18 fallback inputs (non-publishing mode only).

    Without an attestation bundle, classification blocks unless a COMPLETE sealed
    ``approved-unsupported`` fallback record exists. A local validation run exercises
    that sealing machinery end-to-end with a clearly-labeled, short-lived record that
    is NOT a Release Owner approval: the run stays in the ``local-candidate`` context,
    so ``publish_authorized`` remains false and nothing it produces can authorize a
    real release. The publish-capable path never uses these values — there the
    fallback fields come from the Release Owner-sealed repository variables.
    """
    # Read the digest from the typed JSON output, not stdout scraping — a stray
    # warning line would silently swap the digest (review BH-21).
    digest_fd, digest_name = tempfile.mkstemp(suffix=".json")
    os.close(digest_fd)
    try:
        run("classify", [
            "python3", "eng/release_evidence.py", "fallback-digest",
            "--root", ".", "--output", digest_name,
        ], capture=True)
        with open(digest_name, "r", encoding="utf-8") as handle:
            digest = str(json.load(handle).get("digest_sha256", ""))
    finally:
        os.unlink(digest_name)
    if not _SHA256_RE.fullmatch(digest):
        raise PhaseFailure("classify", "fallback-digest did not produce a well-formed sha256 digest")
    evidence_doc = REPO_ROOT / EVIDENCE_DIR / "attestation-unavailable.md"
    evidence_doc.write_text(
        "# Attestation unavailable — local non-publishing validation\n\n"
        "This record is produced by `release_prepublish.py prepare --non-publishing` to\n"
        "exercise the AC18 sealed-fallback machinery locally. It is NOT a Release Owner\n"
        "approval and cannot authorize publication (local-candidate context;\n"
        "`publish_authorized=false`). Real releases require the upstream governed\n"
        "attestation bundle or the Release Owner-sealed fallback variables.\n",
        encoding="utf-8", newline="\n")
    now = _dt.datetime.now(_dt.timezone.utc)
    return [
        "--fallback-approver", "local-validation (non-publishing; not a Release Owner approval)",
        "--fallback-approved-at", (now - _dt.timedelta(minutes=1)).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "--fallback-expires-at", (now + _dt.timedelta(days=1)).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "--fallback-approved-against-fingerprints-sha256", digest,
        "--fallback-scope", "local non-publishing validation run only",
    ]


def phase_classify(non_publishing: bool) -> None:
    context = context_env()
    cmd = [
        "python3", "eng/release_evidence.py", "classify-release",
        "--root", ".",
        "--evidence-root", str(EVIDENCE_DIR),
        "--manifest", str(EVIDENCE_DIR / "sealed-manifest.json"),
        "--test-results", str(EVIDENCE_DIR / "test-results.json"),
        "--output", str(EVIDENCE_DIR / "release-readiness.json"),
        "--event-name", context["event_name"],
        "--ref", context["ref"],
        "--ref-protected", context["ref_protected"],
        "--semantic-release-state", "matches",
        # Same-version concurrency is controlled upstream of this command: release.yml
        # serializes Release runs (concurrency group, cancel-in-progress false), the
        # workflow_run trigger is single-flight per CI success, and semantic-release
        # itself fails before publishCmd when the computed tag already exists. There is
        # no in-process probe to consume here; every other blocking check (manifest,
        # signing, attestation, context, inventory) remains evidence-derived.
        "--concurrent-same-version", "false",
        "--require-publishable",
    ]
    if non_publishing:
        # Honest local validation: the local-candidate context blocker is the ONLY
        # tolerated blocker (--dry-run-clean-exit exits 0 solely when the underlying
        # evidence would classify ready/fallback-approved in a trusted context);
        # publish_authorized stays false in the readiness JSON. The validation-scoped
        # approval and fallback inputs below make a HEALTHY candidate reach that state
        # while remaining unauthorized; a broken candidate still fails closed.
        cmd.extend(["--dry-run", "true", "--dry-run-clean-exit"])
        cmd.extend([
            "--owner-approved", "true",
            "--approver", "local-validation (non-publishing; not a Release Owner approval)",
            "--approval-mechanism", "non-publishing validation run; publication remains frozen and unauthorized",
        ])
        if os.environ.get("RELEASE_ATTESTATION_STATUS", "approved-unsupported") != "attested":
            cmd.extend(_validation_fallback_args())
    else:
        # REL-3 AC20 approval mechanism: the Release Owner's authorization is the REL-4
        # freeze gate — the gated release.yml freeze-guard (vars.HEXALITH_RELEASE_PUBLISH_ENABLED
        # exactly 'true') is the ONLY path into domain-release.yml's semantic-release step, so
        # reaching this publish-capable prepareCmd proves the owner-controlled variable enabled
        # the run. Outside that context the classifier still fails closed on the
        # non-trusted context blocker regardless of these values.
        mechanism = "vars.HEXALITH_RELEASE_PUBLISH_ENABLED exactly 'true' via the REL-4 freeze-guard gate in release.yml"
        run_id = os.environ.get("GITHUB_RUN_ID", "")
        if run_id:
            # Bind the asserting run's identity into the recorded mechanism so the
            # readiness evidence is traceable to a concrete gated execution (review BH-13).
            mechanism += f" (asserted by run {run_id})"
        cmd.extend([
            "--owner-approved", "true",
            "--approver", "release-owner (HEXALITH_RELEASE_PUBLISH_ENABLED custody)",
            "--approval-mechanism", mechanism,
        ])
    run("classify", cmd)


def cmd_prepare(args: argparse.Namespace) -> int:
    # Stale-evidence guard (review EC-9): a leftover release-evidence/ (or benchmark
    # artifact) from a prior run must never satisfy a later existence guard and get
    # sealed into a fresh manifest. Every prepare starts from empty evidence, exactly
    # like nupkgs/ and TestResults/.
    for stale in (EVIDENCE_DIR, pathlib.Path("artifacts") / "benchmark"):
        target = REPO_ROOT / stale
        if target.exists():
            shutil.rmtree(target)
    (REPO_ROOT / EVIDENCE_DIR).mkdir(parents=True, exist_ok=True)
    tag = f"v{args.version}"
    phase_build()
    phase_pack(args.version)
    phase_inventory()
    phase_tests()
    phase_consumer_validation()
    phase_sbom_and_symbols(args.version)
    phase_sign_and_verify(args.non_publishing)
    phase_benchmark()
    phase_checksums()
    phase_manifest(args.version, tag)
    phase_classify(args.non_publishing)
    log("prepare", f"pre-publication gate complete for {tag}")
    return 0


# ---------------------------------------------------------------------------
# publish
# ---------------------------------------------------------------------------

def _record_incident(phase: str, base: pathlib.Path | None = None) -> None:
    base = base or REPO_ROOT
    incident = base / EVIDENCE_DIR / "partial-publish-incident.json"
    if incident.is_file():
        try:
            existing = json.loads(incident.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            existing = None
        if isinstance(existing, dict) and str(existing.get("failed_phase", "none")).lower() in {"", "none"}:
            incident.unlink()
    subprocess.run(
        ["python3", "eng/release_evidence.py", "partial-publish-incident",
         "--manifest", str(base / EVIDENCE_DIR / "sealed-manifest.json"),
         "--output", str(base / EVIDENCE_DIR / "partial-publish-incident.json"),
         "--phase", phase,
         "--classification", "partial-publish-incident"],
        cwd=REPO_ROOT, check=False,
    )
    # Publisher-side durability (review BH-4): when the publish aborts here,
    # semantic-release stops before @semantic-release/github can attach any asset and
    # the runner is discarded — so surface the sanitized incident record in the job
    # log, the one durable trace the domain-release run keeps. The independent
    # verifier re-derives the divergence from published state regardless.
    if incident.is_file():
        log("publish", f"incident record: {sanitize_paths(incident.read_text(encoding='utf-8')).strip()}")


def _confine_to_repo(package_id: str, raw_path: str, base: pathlib.Path | None = None) -> pathlib.Path:
    """Reject absolute or root-escaping manifest paths before any byte audit (review BH-12)."""
    base = base or REPO_ROOT
    candidate = pathlib.Path(raw_path)
    resolved_root = base.resolve()
    if candidate.is_absolute() or not (resolved_root / candidate).resolve().is_relative_to(resolved_root):
        _record_incident("post-seal-verification", base)
        raise PhaseFailure("publish-verify", f"{package_id}: manifest path escapes the repository root")
    return candidate


def cmd_publish(args: argparse.Namespace) -> int:
    # --work-root exists so the pre-push audit is runtime-testable against a staged
    # evidence set (review VG-2); production publishCmd never passes it and audits the
    # repository root exactly as before.
    base = pathlib.Path(args.work_root).resolve() if getattr(args, "work_root", None) else REPO_ROOT
    api_key = os.environ.get("NUGET_API_KEY", "")
    if not api_key:
        raise PhaseFailure("publish", "NUGET_API_KEY is not available; publication fails closed")

    # Re-verify the sealed manifest and readiness immediately before any push (AC10/AC11).
    # A failed re-verification IS observed post-seal divergence: record the typed
    # incident (AC14) before failing closed.
    verify = run("publish-verify", [
        "python3", "eng/release_evidence.py", "verify-manifest",
        "--manifest", str(base / EVIDENCE_DIR / "sealed-manifest.json"),
        "--root", str(base),
        "--output", str(base / EVIDENCE_DIR / "release-verification.json"),
    ], tolerate_failure=True)
    if verify.returncode != 0:
        _record_incident("post-seal-verification", base)
        raise PhaseFailure("publish-verify", "sealed manifest failed re-verification immediately before push")
    readiness = json.loads((base / EVIDENCE_DIR / "release-readiness.json").read_text(encoding="utf-8"))
    classification = str(readiness.get("classification", ""))
    authorized = readiness.get("publish_authorized") is True
    if classification not in {"ready", "fallback-approved"} or not authorized:
        raise PhaseFailure(
            "publish-verify",
            f"release is not publish-authorized (classification={classification or 'missing'}); refusing to push",
        )

    manifest = json.loads((base / EVIDENCE_DIR / "sealed-manifest.json").read_text(encoding="utf-8"))
    rows = [row for row in manifest.get("packages", []) if isinstance(row, dict)]
    if not rows:
        raise PhaseFailure("publish-verify", "sealed manifest contains no package rows")
    expected_version = args.version

    # Exact-byte pre-push audit: every manifest-authorized artifact must exist and
    # hash-match its SEALED checksum (packages and symbols both live in the sealed
    # manifest rows — review BH-1/VG-3 removed the unsealed checksums.json indirection).
    # Any divergence is a post-seal mutation and fails before any push.
    push_plan: list[tuple[str, pathlib.Path]] = []
    for row in rows:
        package_id = str(row.get("package_id", "<unknown>"))
        if str(row.get("version", "")) != expected_version:
            _record_incident("post-seal-verification", base)
            raise PhaseFailure("publish-verify", f"{package_id}: manifest version differs from semantic-release version")
        raw_artifact = str(row.get("artifact_path", ""))
        if not raw_artifact.startswith("nupkgs-signed/"):
            _record_incident("post-seal-verification", base)
            raise PhaseFailure("publish-verify", f"{package_id}: artifact path is not a signed candidate path")
        artifact = _confine_to_repo(package_id, raw_artifact, base)
        if not (base / artifact).is_file() or sha256_file(artifact, base) != row.get("checksum"):
            _record_incident("post-seal-verification", base)
            raise PhaseFailure("publish-verify", f"{package_id}: signed artifact missing or checksum mismatch")
        push_plan.append(("package-push", artifact))
        symbol = str(row.get("symbol_artifact", ""))
        sealed_symbol_hash = str(row.get("symbol_checksum", ""))
        if symbol.startswith("nupkgs/") and symbol.endswith(".snupkg"):
            symbol_path = _confine_to_repo(package_id, symbol, base)
            if not _SHA256_RE.fullmatch(sealed_symbol_hash):
                # Fail-open seam closed (review VG-3/EC-12): a symbol without a sealed
                # hash must never be pushed on existence alone.
                _record_incident("post-seal-verification", base)
                raise PhaseFailure("publish-verify", f"{package_id}: symbol has no sealed checksum")
            if not (base / symbol_path).is_file() or sha256_file(symbol_path, base) != sealed_symbol_hash:
                _record_incident("post-seal-verification", base)
                raise PhaseFailure("publish-verify", f"{package_id}: symbol package missing or checksum mismatch")
            push_plan.append(("symbol-push", symbol_path))
        elif sealed_symbol_hash == "not-required" and not symbol.endswith(".snupkg"):
            # Documented non-symbol exception row (inventory symbol_required=false):
            # nothing to push. Any other shape is a malformed or substituted symbol
            # reference and fails closed (review BH-2/EC-11).
            pass
        else:
            _record_incident("post-seal-verification", base)
            raise PhaseFailure("publish-verify", f"{package_id}: malformed symbol path for symbol package")

    pushed = 0
    for phase, artifact in push_plan:
        result = subprocess.run(
            ["dotnet", "nuget", "push", str(base / artifact),
             "--source", "https://api.nuget.org/v3/index.json",
             "--api-key", api_key],
            cwd=REPO_ROOT, capture_output=True, text=True, check=False,
        )
        if result.returncode != 0:
            _record_incident(phase, base)
            # Surface a sanitized failure tail so the operator can distinguish
            # 403-key / 409-conflict / quota / outage during reconciliation (review
            # BH-11). The api key rides argv, never the captured streams; paths are
            # sanitized before logging.
            detail_source = (result.stderr or "") + "\n" + (result.stdout or "")
            detail = " | ".join(line.strip() for line in detail_source.splitlines() if line.strip())[-400:]
            log("publish", f"push failed for {artifact.name} after {pushed} successful pushes; partial-publication incident recorded")
            log("publish", f"push failure detail: {sanitize_paths(detail)}")
            raise PhaseFailure("publish", f"push failed during {phase}; release is failed pending owner-led reconciliation")
        pushed += 1
        log("publish", f"pushed {artifact.name}")
    log("publish", f"published {pushed} artifacts from the sealed manifest")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    prepare = sub.add_parser("prepare", help="Run the FR24 exact-artifact pre-publication gate.")
    prepare.add_argument("--version", required=True)
    prepare.add_argument(
        "--non-publishing", action="store_true",
        help="Local validation mode: full chain, classification tolerates ONLY the local-candidate context blocker.",
    )
    prepare.set_defaults(func=cmd_prepare)

    publish = sub.add_parser("publish", help="Push the manifest-authorized signed artifacts only.")
    publish.add_argument("--version", required=True)
    publish.add_argument(
        "--work-root", default=None,
        help="Test-only override: audit evidence + artifacts under this directory instead of the "
             "repository root (governance runtime negatives). Production publishCmd omits it.",
    )
    publish.set_defaults(func=cmd_publish)

    args = parser.parse_args()
    try:
        return args.func(args)
    except PhaseFailure as failure:
        log(failure.phase, f"FAIL-CLOSED: {sanitize_paths(str(failure))}")
        return failure.exit_code
    except Exception as exc:  # noqa: BLE001 — final fail-closed guard (review EC-10):
        # an unexpected crash (missing evidence file, malformed inventory row, sudo
        # failure) must exit 1 with a sanitized FAIL-CLOSED line, never a raw
        # traceback. Secrets never ride exception text: sign/push subprocesses run
        # check=False and the openssl password moved to env indirection.
        log("fatal", f"FAIL-CLOSED: {type(exc).__name__}: {sanitize_paths(str(exc))}")
        return 1


if __name__ == "__main__":
    sys.exit(main())
