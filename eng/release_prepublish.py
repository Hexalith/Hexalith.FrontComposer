#!/usr/bin/env python3
"""FR24 pre-publication orchestration for exact-source releases (REL-3).

Unsigned-author / NuGet.org repository-signature model. The exact-artifact gate is
split across the Release workflow and semantic-release:

* ``prepare`` + ``bundle`` run in ``release.yml`` ``prepare-candidate`` and enforce the
  Required Artifact Invariant fail-closed in order:

    pack once (unsigned ``nupkgs/*``) -> inventory -> tests -> package-consumer validation
    -> SBOM/symbols -> benchmark candidate evidence -> checksums -> prepare/seal/verify
    manifest (attestation bound) -> classify-release --require-publishable -> bundle

* semantic-release (``.releaserc.json``) then consumes that prepared candidate only:

    ``verifyReleaseCmd`` = ``restore``
    ``prepareCmd`` = ``verify-prepared``
    ``publishCmd`` = ``publish``

``verify-prepared`` and ``publish`` re-verify the sealed manifest and consume the
**sealed** ``release-readiness.json`` classification (sealed-readiness-only). They do
**not** re-run ``classify-release``. Re-classify at publish changes the authorization
clock and requires an explicit Ask First decision before enabling.

Every prepare phase is fail-closed: a non-zero exit stops the Release candidate before
any publication side effect (NuGet, GitHub Release, tag, changelog). There is no
G1-style record-and-proceed path: a missing package, checksum drift, invalid
manifest, or incomplete evidence aborts preparation (REL-3 AC5/AC10).

``publish`` re-verifies the sealed manifest and the sealed publishable readiness
immediately before pushing, then pushes ONLY the manifest-authorized sealed ``.nupkg``
paths and their matching ``.snupkg`` symbol packages. It never rebuilds, repacks,
author-signs, or substitutes artifacts (AC11). Divergent per-package outcomes are
recorded as a partial-publication incident and fail the release (AC14).

Local, non-publishing validation (REL-3 T6) uses ``prepare --non-publishing``:
the full chain runs identically, classification runs with ``--dry-run true
--dry-run-clean-exit`` so a healthy candidate exits 0 while remaining honestly
non-publishable (``publish_authorized=false`` in a ``local-candidate`` context).
The publish-capable path never uses that carve-out.

The NuGet API key is consumed from the environment and never echoed; failure output
is redacted to tool + phase names before landing under ``release-evidence/``.
"""

from __future__ import annotations

import argparse
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

import release_contract
from release_compatibility import (
    PUBLISHED_BASELINE_VERSION,
    ReleaseCompatibilityError,
    release_properties,
    validate_release_policy,
)

REPO_ROOT = pathlib.Path(__file__).resolve().parents[1]
EVIDENCE_DIR = pathlib.Path("release-evidence")
NUPKGS_DIR = pathlib.Path("nupkgs")
TEST_RESULTS_DIR = pathlib.Path("TestResults")
INVENTORY_FILE = pathlib.Path("eng/release-package-inventory.json")
PACKAGE_VERSION_VERIFIER = pathlib.Path("eng/verify-candidate-packages.cs")
PACKAGE_MANIFEST = pathlib.Path("tools/release-packages.json")
EXPECTED_PACKAGE_COUNT = 8
SOLUTION = "Hexalith.FrontComposer.slnx"

# Mirrors the release test lane previously hosted by release-evidence.yml (G1):
# the seven CI-authoritative test projects, Gate 3a filter
# (Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined).
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

def phase_release_policy(version: str) -> None:
    """Fail before build or package-output mutation when release compatibility is stale."""
    try:
        release_line = validate_release_policy(REPO_ROOT, version)
    except ReleaseCompatibilityError as error:
        raise PhaseFailure("compatibility-policy", str(error)) from error
    log(
        "compatibility-policy",
        f"validated {release_line} against published baseline {PUBLISHED_BASELINE_VERSION}",
    )


def phase_build(version: str) -> None:
    """Restore + build Release once so pack --no-build and test --no-build can consume it.

    On the domain-release runner the solution is already built; this is an incremental
    no-op there. It is a BUILD prerequisite, not a repack: candidate packages are packed
    exactly once in phase_pack.

    Restore opts into package validation so the published PackageValidationBaselineVersion
    packages (currently 4.1.1) are cached before Contract package-boundary tests run.
    Quality uses the same switch; omitting it leaves prepare-candidate unable to find the
    MCP baseline under NUGET_PACKAGES.
    """
    properties = release_properties(version)
    run("build", [
        "dotnet", "restore", SOLUTION, "-p:Configuration=Release",
        *properties,
    ])
    run("build", [
        "dotnet", "build",
        "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj",
        "-f", "netstandard2.0", "--configuration", "Release", "--no-restore", "-m:1", "/nr:false",
        *properties,
    ])
    run("build", [
        "dotnet", "build", SOLUTION, "--configuration", "Release", "--no-restore", *properties,
    ])


def phase_pack(version: str) -> None:
    run("pack-once", [
        "python3", "scripts/pack-release-packages.py", str(NUPKGS_DIR), version,
        "--release-policy",
    ])
    # semantic-release version contract: every packable candidate must exist at the
    # supplied version before anything downstream consumes it (semantic_release_state=matches).
    missing = [
        f"{row['package_id']}.{version}.nupkg"
        for row in packable_rows()
        if not (REPO_ROOT / NUPKGS_DIR / f"{row['package_id']}.{version}.nupkg").is_file()
    ]
    if missing:
        raise PhaseFailure("pack-once", f"candidates missing for semantic-release version {version}: {missing}")
    run("package-version", [
        "dotnet", "run", "--file", str(PACKAGE_VERSION_VERIFIER), "--",
        str(NUPKGS_DIR), str(INVENTORY_FILE), version,
    ])
    _validate_unsigned_candidates(REPO_ROOT, "pack-once")


def _validate_unsigned_candidates(base: pathlib.Path, phase: str) -> None:
    package_root = base / NUPKGS_DIR
    packages = sorted(package_root.rglob("*.nupkg")) if package_root.is_dir() else []
    if len(packages) != EXPECTED_PACKAGE_COUNT or any(package.parent != package_root for package in packages):
        raise PhaseFailure(
            phase,
            f"prepared candidate must contain exactly {EXPECTED_PACKAGE_COUNT} root-level NuGet packages",
        )
    for package in packages:
        try:
            release_contract.validate_unsigned_candidate(package)
        except release_contract.ContractError as exc:
            raise PhaseFailure(
                phase,
                f"{package.name}: candidate must be an unsigned, safe NuGet archive: {exc}",
            ) from exc


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
    # Prepare-candidate sets exact-source provenance in the job env. Governance tests that
    # assert fail-closed prepare-manifest behavior must not inherit those values, or the
    # unsigned-candidate contract exits 0 under Release while Quality (no provenance env) stays green.
    # Keep RELEASE_ATTESTATION_STATUS at the Quality/default value: scrubbing it to "" makes
    # classify-release skip the approved-unsupported fallback branch and hides AC18 reasons.
    test_env = {
        "DiffEngine_Disabled": "true",
        "DEPENDENCY_RELEASE_SOURCE_PROOF": "",
        "DEPENDENCY_RELEASE_HANDOFF": "",
        "RELEASE_EVALUATOR": "",
        "HEXALITH_BUILDS_EXECUTION_SHA": "",
        "RELEASE_ATTESTATION_STATUS": "approved-unsupported",
        "RELEASE_ATTESTATION_FALLBACK_APPROVER": "",
        "RELEASE_ATTESTATION_FALLBACK_APPROVED_AT": "",
        "RELEASE_ATTESTATION_FALLBACK_EXPIRES_AT": "",
    }
    for project in TEST_PROJECTS:
        name = pathlib.Path(project).stem
        run("tests", [
            "dotnet", "test", project,
            "--configuration", "Release", "--no-build",
            "--filter", "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined",
            "--results-directory", str(TEST_RESULTS_DIR / name),
            "--logger", f"trx;LogFileName={name}.trx",
        ], env=test_env)
    run("tests", [
        "python3", "eng/release_evidence.py", "test-results",
        "--results-dir", str(TEST_RESULTS_DIR),
        "--output", str(EVIDENCE_DIR / "test-results.json"),
    ], env=test_env)


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
        "nupkgs/*.nupkg",
        "nupkgs/*.snupkg",
        "release-evidence/sbom.json",
        "release-evidence/test-results.json",
        "release-evidence/package-inventory.json",
        "release-evidence/dependency-release-source.json",
        "release-evidence/consumer-validation.json",
        "release-evidence/benchmark-summary.json",
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
        "--diagnostics-output", str(EVIDENCE_DIR / "manifest-diagnostics.json"),
        "--output", str(EVIDENCE_DIR / "pre-manifest.json"),
    ]
    source_proof = os.environ.get("DEPENDENCY_RELEASE_SOURCE_PROOF", "")
    ci_handoff = os.environ.get("DEPENDENCY_RELEASE_HANDOFF", "")
    release_evaluator = os.environ.get("RELEASE_EVALUATOR", "")
    builds_sha = os.environ.get("HEXALITH_BUILDS_EXECUTION_SHA", "")
    if ci_handoff:
        prepare_cmd.extend(["--ci-handoff", ci_handoff])
    if release_evaluator:
        prepare_cmd.extend(["--release-evaluator", release_evaluator])
    if source_proof:
        prepare_cmd.extend(["--source-proof", source_proof])
    if builds_sha:
        prepare_cmd.extend(["--builds-execution-sha", builds_sha])
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
        # operator dispatch is bound to one exact-source CI success, and semantic-release
        # itself fails before publishCmd when the computed tag already exists. There is
        # no in-process probe to consume here; every other blocking check (manifest,
        # attestation, context, inventory) remains evidence-derived.
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
        # The publish-capable candidate can only be prepared after workflow_dispatch
        # exact-source validation reaches the protected production environment.
        mechanism = "workflow_dispatch exact-source gate plus protected production environment approval"
        run_id = os.environ.get("GITHUB_RUN_ID", "")
        if run_id:
            # Bind the asserting run's identity into the recorded mechanism so the
            # readiness evidence is traceable to a concrete gated execution (review BH-13).
            mechanism += f" (asserted by run {run_id})"
        cmd.extend([
            "--owner-approved", "true",
            "--approver", "production environment reviewer",
            "--approval-mechanism", mechanism,
        ])
    run("classify", cmd)


def cmd_prepare(args: argparse.Namespace) -> int:
    phase_release_policy(args.version)
    run("manifest-contract", [
        "python3", "eng/release_contract.py", "manifest",
        "--root", ".",
        "--manifest", str(PACKAGE_MANIFEST),
        "--expected-count", str(EXPECTED_PACKAGE_COUNT),
    ])
    # Stale-evidence guard (review EC-9): a leftover release-evidence/ (or benchmark
    # artifact) from a prior run must never satisfy a later existence guard and get
    # sealed into a fresh manifest. Every prepare starts from empty evidence, exactly
    # like nupkgs/ and TestResults/.
    for stale in (EVIDENCE_DIR, pathlib.Path("artifacts") / "benchmark"):
        target = REPO_ROOT / stale
        if target.exists():
            shutil.rmtree(target)
    (REPO_ROOT / EVIDENCE_DIR).mkdir(parents=True, exist_ok=True)
    source_proof = os.environ.get("DEPENDENCY_RELEASE_SOURCE_PROOF", "")
    if source_proof:
        proof_path = pathlib.Path(source_proof)
        if not proof_path.is_absolute():
            proof_path = REPO_ROOT / proof_path
        if not proof_path.is_file():
            raise PhaseFailure("source-proof", "authenticated dependency release source proof is missing")
        shutil.copyfile(proof_path, REPO_ROOT / EVIDENCE_DIR / "dependency-release-source.json")
    if os.environ.get("RELEASE_ATTESTATION_STATUS", "approved-unsupported") == "approved-unsupported":
        (REPO_ROOT / EVIDENCE_DIR / "attestation-unavailable.md").write_text(
            "# Attestation unavailable\n\n"
            "The current NuGet release path does not emit a supported platform attestation bundle.\n"
            "Publication therefore requires the separately sealed, time-limited Release Owner fallback\n"
            "record validated by the release evidence classifier. This document is not approval.\n",
            encoding="utf-8",
            newline="\n",
        )
    tag = f"v{args.version}"
    phase_build(args.version)
    phase_pack(args.version)
    phase_inventory()
    phase_tests()
    phase_consumer_validation()
    phase_sbom_and_symbols(args.version)
    phase_benchmark()
    phase_checksums()
    phase_manifest(args.version, tag)
    phase_classify(args.non_publishing)
    log("prepare", f"pre-publication gate complete for {tag}")
    return 0


def _candidate_files(base: pathlib.Path) -> list[pathlib.Path]:
    files: list[pathlib.Path] = []
    for directory in (NUPKGS_DIR, EVIDENCE_DIR):
        candidate = base / directory
        if candidate.is_dir():
            files.extend(path for path in candidate.rglob("*") if path.is_file())
    return sorted(files)


def cmd_bundle(args: argparse.Namespace) -> int:
    """Bind the pack-once candidate to this run/attempt before artifact upload."""
    source_sha = os.environ.get("GITHUB_SHA", "")
    run_id = os.environ.get("GITHUB_RUN_ID", "")
    run_attempt = os.environ.get("GITHUB_RUN_ATTEMPT", "")
    if re.fullmatch(r"[0-9a-f]{40}", source_sha) is None:
        raise PhaseFailure("bundle", "GITHUB_SHA must be an exact lowercase commit SHA")
    if not run_id.isdigit() or int(run_id) < 1 or not run_attempt.isdigit() or int(run_attempt) < 1:
        raise PhaseFailure("bundle", "run identity must contain positive integers")
    _validate_unsigned_candidates(REPO_ROOT, "bundle")
    descriptor_path = REPO_ROOT / EVIDENCE_DIR / "prepared-candidate.json"
    files = [path for path in _candidate_files(REPO_ROOT) if path != descriptor_path]
    if not files:
        raise PhaseFailure("bundle", "prepared candidate contains no files")
    descriptor = {
        "schema": "hexalith.frontcomposer.prepared-candidate.v1",
        "source_sha": source_sha,
        "version": args.version,
        "run_id": int(run_id),
        "run_attempt": int(run_attempt),
        "files": [
            {
                "path": path.relative_to(REPO_ROOT).as_posix(),
                "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
            }
            for path in files
        ],
    }
    write_json(EVIDENCE_DIR / "prepared-candidate.json", descriptor)
    log("bundle", f"sealed {len(files)} prepared-candidate files for run {run_id}/{run_attempt}")
    return 0


def _validate_candidate_descriptor(base: pathlib.Path, version: str) -> dict:
    descriptor_path = base / EVIDENCE_DIR / "prepared-candidate.json"
    try:
        descriptor = json.loads(descriptor_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise PhaseFailure("restore", "prepared-candidate descriptor is missing or malformed") from exc
    if not isinstance(descriptor, dict) or set(descriptor) != {
        "schema", "source_sha", "version", "run_id", "run_attempt", "files"
    }:
        raise PhaseFailure("restore", "prepared-candidate descriptor has an invalid shape")
    expected = {
        "schema": "hexalith.frontcomposer.prepared-candidate.v1",
        "source_sha": os.environ.get("GITHUB_SHA", ""),
        "version": version,
        "run_id": int(os.environ.get("GITHUB_RUN_ID", "0")),
        "run_attempt": int(os.environ.get("GITHUB_RUN_ATTEMPT", "0")),
    }
    for field, value in expected.items():
        if descriptor.get(field) != value:
            raise PhaseFailure("restore", f"prepared-candidate {field} mismatch")
    rows = descriptor["files"]
    if not isinstance(rows, list) or not rows:
        raise PhaseFailure("restore", "prepared-candidate file inventory is empty")
    seen: set[str] = set()
    for row in rows:
        if not isinstance(row, dict) or set(row) != {"path", "sha256"}:
            raise PhaseFailure("restore", "prepared-candidate file row is malformed")
        relative = pathlib.PurePosixPath(str(row["path"]))
        if relative.is_absolute() or ".." in relative.parts or str(relative) in seen:
            raise PhaseFailure("restore", "prepared-candidate contains an unsafe or duplicate path")
        seen.add(str(relative))
        path = base.joinpath(*relative.parts)
        if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != row["sha256"]:
            raise PhaseFailure("restore", f"prepared-candidate byte mismatch: {relative}")
    actual = {
        path.relative_to(base).as_posix()
        for path in _candidate_files(base)
        if path != descriptor_path
    }
    if actual != seen:
        raise PhaseFailure("restore", "prepared-candidate archive files differ from its sealed inventory")
    _validate_unsigned_candidates(base, "restore")
    return descriptor


def cmd_restore(args: argparse.Namespace) -> int:
    run_id = os.environ.get("GITHUB_RUN_ID", "")
    run_attempt = os.environ.get("GITHUB_RUN_ATTEMPT", "")
    if not run_id.isdigit() or not run_attempt.isdigit():
        raise PhaseFailure("restore", "current release run identity is malformed")
    artifact_name = f"release-candidate-{run_id}-{run_attempt}"
    with tempfile.TemporaryDirectory(prefix="frontcomposer-candidate-") as temporary:
        staging = pathlib.Path(temporary)
        run("restore", [
            "gh", "run", "download", run_id,
            "--repo", os.environ.get("GITHUB_REPOSITORY", ""),
            "--name", artifact_name,
            "--dir", str(staging),
        ], redact_command=True)
        _validate_candidate_descriptor(staging, args.version)
        for directory in (NUPKGS_DIR, EVIDENCE_DIR):
            target = REPO_ROOT / directory
            if target.exists():
                shutil.rmtree(target)
            shutil.copytree(staging / directory, target)
    _validate_candidate_descriptor(REPO_ROOT, args.version)
    log("restore", f"restored exact prepared candidate {artifact_name}")
    return 0


def cmd_verify_prepared(args: argparse.Namespace) -> int:
    # Sealed-readiness-only contract (REL-3 residual): do not re-run classify-release.
    # Authorization is the readiness seal produced during prepare; re-classify would
    # move the authorization clock after the candidate was already sealed.
    _validate_candidate_descriptor(REPO_ROOT, args.version)
    result = run("prepared-verify", [
        "python3", "eng/release_evidence.py", "verify-manifest",
        "--manifest", str(EVIDENCE_DIR / "sealed-manifest.json"),
        "--root", ".", "--graph-root", ".",
        "--output", str(EVIDENCE_DIR / "release-verification.json"),
    ], tolerate_failure=True)
    if result.returncode != 0:
        _record_incident("post-restore-verification")
        raise PhaseFailure("prepared-verify", "restored sealed manifest failed live verification")
    readiness = read_json(EVIDENCE_DIR / "release-readiness.json")
    if readiness.get("publish_authorized") is not True or readiness.get("classification") not in {"ready", "fallback-approved"}:
        raise PhaseFailure("prepared-verify", "restored release readiness is not publish-authorized")
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

    # Re-verify the sealed manifest and sealed readiness immediately before any push
    # (AC10/AC11). Sealed-readiness-only: do not re-run classify-release here.
    # A failed re-verification IS observed post-seal divergence: record the typed
    # incident (AC14) before failing closed.
    verify = run("publish-verify", [
        "python3", "eng/release_evidence.py", "verify-manifest",
        "--manifest", str(base / EVIDENCE_DIR / "sealed-manifest.json"),
        "--root", str(base),
        "--graph-root", str(base),
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
        artifact_parts = pathlib.PurePosixPath(raw_artifact).parts
        if artifact_parts != ("nupkgs", f"{package_id}.{expected_version}.nupkg"):
            _record_incident("post-seal-verification", base)
            raise PhaseFailure("publish-verify", f"{package_id}: artifact path is not a sealed candidate path")
        artifact = _confine_to_repo(package_id, raw_artifact, base)
        if not (base / artifact).is_file() or sha256_file(artifact, base) != row.get("checksum"):
            _record_incident("post-seal-verification", base)
            raise PhaseFailure("publish-verify", f"{package_id}: sealed artifact missing or checksum mismatch")
        try:
            release_contract.validate_unsigned_candidate(base / artifact)
        except release_contract.ContractError as exc:
            _record_incident("post-seal-verification", base)
            raise PhaseFailure(
                "publish-verify",
                f"{package_id}: sealed candidate is signed or has unsafe NuGet content: {exc}",
            ) from exc
        push_plan.append(("package-push", artifact))
        symbol = str(row.get("symbol_artifact", ""))
        sealed_symbol_hash = str(row.get("symbol_checksum", ""))
        if pathlib.PurePosixPath(symbol).parts == ("nupkgs", f"{package_id}.{expected_version}.snupkg"):
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
            _nuget_push_command(phase, base / artifact, api_key),
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


def _nuget_push_command(phase: str, artifact: pathlib.Path, api_key: str) -> list[str]:
    command = [
        "dotnet", "nuget", "push", str(artifact),
        "--source", "https://api.nuget.org/v3/index.json",
        "--api-key", api_key,
    ]
    if phase == "package-push":
        # Symbols are pushed explicitly from the sealed plan; suppress NuGet's
        # same-directory auto-discovery so each .snupkg is uploaded exactly once.
        command.append("--no-symbols")
    return command


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

    bundle = sub.add_parser("bundle", help="Seal the prepared candidate for run-bound artifact transfer.")
    bundle.add_argument("--version", required=True)
    bundle.set_defaults(func=cmd_bundle)

    restore = sub.add_parser("restore", help="Restore and authenticate this run's prepared candidate.")
    restore.add_argument("--version", required=True)
    restore.set_defaults(func=cmd_restore)

    verify_prepared = sub.add_parser("verify-prepared", help="Reverify restored bytes before publication.")
    verify_prepared.add_argument("--version", required=True)
    verify_prepared.set_defaults(func=cmd_verify_prepared)

    publish = sub.add_parser("publish", help="Push the manifest-authorized sealed artifacts only.")
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
        # traceback. Secrets never ride exception text: push subprocesses run
        # check=False and output is sanitized before logging.
        log("fatal", f"FAIL-CLOSED: {type(exc).__name__}: {sanitize_paths(str(exc))}")
        return 1


if __name__ == "__main__":
    sys.exit(main())
