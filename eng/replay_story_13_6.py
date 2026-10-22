"""Replay the retained Story 13.6 packet with its hash-bound historical runtime.

Only reconstruction integrity can pass. The original accessibility gate stays blocked.
No checkout files, sealed packet bytes, dependencies or image baselines are replaced.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import shutil
import subprocess
import sys
import tempfile


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "_bmad-output/implementation-artifacts/tests/story-13-6-historical-runtime.json"
REVISION = "397dac34a1bd1bf4d789161794a66811360cb59a"
PACKET = "_bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-18-repeatable-browser-reviewed"
ENGINES = ["chromium", "firefox", "webkit"]


class ReplayError(Exception):
    """A required historical input or replay result failed verification."""


def verify_bytes(data: bytes, expected: dict, label: str) -> None:
    if len(data) != expected["bytes"] or hashlib.sha256(data).hexdigest() != expected["sha256"]:
        raise ReplayError(f"Size/hash mismatch: {label}")


def reconstruct_bytes(data: bytes, recipe: str) -> bytes:
    if recipe == "raw":
        return data
    if recipe not in {"crlf", "crlf-except-lines-14-15"}:
        raise ReplayError(f"Unsupported byte recipe: {recipe}")
    lines = data.replace(b"\r\n", b"\n").splitlines(keepends=True)
    return b"".join(line if recipe == "crlf-except-lines-14-15" and number in (14, 15)
                    else line.replace(b"\n", b"\r\n") for number, line in enumerate(lines, 1))


def safe_path(root: Path, name: str) -> Path:
    path = PurePosixPath(name)
    if path.is_absolute() or ".." in path.parts or "\\" in name or path.as_posix() != name:
        raise ReplayError(f"Unsafe reconstruction path: {name}")
    target = root.joinpath(*path.parts)
    if not target.resolve().is_relative_to(root.resolve()):
        raise ReplayError(f"Reconstruction escapes scratch: {name}")
    return target


def historical_blob(repository: Path, path: str) -> bytes:
    result = subprocess.run(["git", "-C", str(repository), "cat-file", "blob", f"{REVISION}:{path}"],
                            capture_output=True, check=False)
    if result.returncode:
        raise ReplayError(f"Missing historical Git input: {REVISION}:{path}; provide the retained Git objects")
    return result.stdout


def materialize(repository: Path, scratch: Path, manifest: dict) -> None:
    """Verify every required byte before any npm or JavaScript execution."""
    if manifest["schema"] != "frontcomposer.story-13-6-historical-runtime.v1" or manifest["revision"] != REVISION or manifest["packet"] != PACKET:
        raise ReplayError("Unsupported historical reconstruction identity")
    packet_source = repository / PACKET
    if packet_source.is_symlink():
        raise ReplayError("Packet must be a regular directory")
    actual = {str(path.relative_to(packet_source)) for path in packet_source.rglob("*") if path.is_file()}
    if actual != set(manifest["packet_files"]):
        raise ReplayError("Retained packet inventory differs")
    # Verify and stage all bytes first; a corrupt late input must never execute early code.
    staged = {}
    for path, expected in manifest["packet_files"].items():
        source = safe_path(packet_source, path)
        if source.is_symlink() or not source.is_file():
            raise ReplayError(f"Packet input is not a regular file: {path}")
        data = source.read_bytes()
        verify_bytes(data, expected, path)
        staged[f"{PACKET}/{path}"] = data
    candidate = json.loads(staged[f"{PACKET}/candidate.json"])
    runtime = json.loads(staged[f"{PACKET}/runtime-inputs.json"])["files"]
    required = set(runtime) | {"tests/e2e/package.json", "tests/e2e/package-lock.json"} | {
        path for path in candidate["protectedBaselines"] if path.endswith(".png")}
    if set(manifest["files"]) != required or len(required) != 29:
        raise ReplayError("Exact runtime, locked npm inputs and 24 PNG baselines required")
    for path, expected in manifest["files"].items():
        safe_path(scratch, path)
        data = reconstruct_bytes(historical_blob(repository, path), expected["recipe"])
        verify_bytes(data, expected, path)
        if path in runtime:
            verify_bytes(data, runtime[path], path)
        if path.endswith(".png") and hashlib.sha256(data).hexdigest() != candidate["protectedBaselines"][path]:
            raise ReplayError(f"Candidate baseline differs: {path}")
        staged[path] = data
    for path, data in staged.items():
        target = safe_path(scratch, path)
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)


def isolated_environment(scratch: Path) -> dict[str, str]:
    environment = dict(os.environ)
    environment.pop("NODE_PATH", None)
    environment.pop("NODE_OPTIONS", None)
    output = str(scratch / "replay-output")
    environment.update(PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD="1", TMPDIR=output, TEMP=output, TMP=output)
    (scratch / "replay-output").mkdir()
    return environment


def check_outcomes(audit: dict) -> dict:
    rows = audit["replay"]["summary"]
    complete = [row for row in rows if row.get("completeGate")]
    totals = {key: sum(row["stats"][key] for row in complete) for key in ("expected", "unexpected", "skipped", "flaky")}
    if (audit["structuralIntegrity"] != "passed" or audit["rawReplayExitCode"] != 1
            or audit["overallAccessibilityClassification"] != "blocked"
            or audit["completeStateRowsPerEngine"] != 116 or audit["supportLogsVerified"] != 6
            or audit["runtimeInputsVerified"] != 3 or audit["completeLaneIdentitiesVerified"] != ENGINES
            or sorted(row["lane"] for row in complete) != ENGINES
            or sum(row["filesVerified"] for row in rows) != 2079
            or any(row["requiredStates"] != 116 or not row["outcomesUnchanged"] or not row["junitOutcomesUnchanged"]
                   or row["artifactValidationExitCode"] != 1 for row in complete)
            or totals != {"expected": 752, "unexpected": 18, "skipped": 4, "flaky": 0}):
        raise ReplayError("Historical replay integrity/outcomes drifted")
    return {"passed": 752, "failed": 18, "skipped": 4, "flaky": 0}


def replay(repository: Path, scratch_parent: Path | None = None) -> dict:
    parent = (scratch_parent or Path(tempfile.gettempdir())).resolve()
    if parent.is_relative_to(repository.resolve()):
        raise ReplayError("Scratch must be outside the repository")
    manifest_bytes = (repository / MANIFEST.relative_to(ROOT)).read_bytes()
    manifest = json.loads(manifest_bytes)
    manifest_digest = hashlib.sha256(manifest_bytes).hexdigest()
    scratch = Path(tempfile.mkdtemp(prefix="frontcomposer-story-13-6-", dir=parent))
    stage = "materialization"
    try:
        materialize(repository, scratch, manifest)
        environment = isolated_environment(scratch)
        stage = "versions"
        versions = {}
        for tool in ("node", "npm"):
            binary = shutil.which(tool)
            if binary is None:
                raise ReplayError(f"Required executable unavailable: {tool}")
            versions[tool] = subprocess.check_output([binary, "--version"], env=environment, text=True).strip()
        e2e = scratch / "tests/e2e"
        stage = "dependencies"
        with (scratch / "npm-ci.log").open("w", encoding="utf-8") as log:
            install = subprocess.run([shutil.which("npm"), "ci", "--ignore-scripts", "--no-audit", "--no-fund"],
                                     cwd=e2e, env=environment, stdout=log, stderr=subprocess.STDOUT, check=False)
        if install.returncode:
            raise ReplayError(f"Locked npm restore failed (exit {install.returncode}); see npm-ci.log")
        # npm must not change the verified inputs; use only scratch-local package resolution.
        for path, expected in manifest["files"].items():
            verify_bytes((scratch / path).read_bytes(), expected, path)
        versions["playwright-core"] = json.loads((e2e / "node_modules/playwright-core/package.json").read_text())["version"]
        stage = "audit"
        execution = subprocess.run([shutil.which("node"), str(scratch / PACKET / "audit-relocation.mjs")],
                                   cwd=scratch, env=environment, text=True, capture_output=True, check=False)
        (scratch / "audit.stdout.json").write_text(execution.stdout, encoding="utf-8")
        (scratch / "audit.stderr.txt").write_text(execution.stderr, encoding="utf-8")
        if execution.returncode or execution.stderr.strip():
            raise ReplayError(f"Sealed replay audit failed (exit {execution.returncode}); see audit.stdout.json and audit.stderr.txt")
        stage = "outcomes"
        outcomes = check_outcomes(json.loads(execution.stdout))
        receipt = {"historicalRevision": REVISION, "reconstructionManifestSha256": manifest_digest, "scratch": str(scratch),
            "versions": versions, "structuralIntegrity": "passed", "filesVerified": 2079,
            "statesPerEngine": 116, "rawReplayExitCode": 1, "outcomes": outcomes,
            "overallAccessibilityClassification": "blocked", "accessibilityApprovalInferred": False}
        stage = "receipt"
        (scratch / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
        return receipt
    except Exception as error:
        failure = {"status": "failed", "stage": stage, "errorType": type(error).__name__,
                   "message": str(error)[:512], "reconstructionManifestSha256": manifest_digest}
        record = scratch / "failure.json"
        try:
            record.write_text(json.dumps(failure, indent=2) + "\n", encoding="utf-8")
            diagnostic = "see failure.json"
        except OSError as record_error:
            diagnostic = f"failure.json could not be written: {type(record_error).__name__}"
        raise ReplayError(f"{type(error).__name__}: {str(error)[:512]}; scratch retained at {scratch}; {diagnostic}") from error


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--scratch-root", type=Path, help="Existing parent outside the checkout; scratch is retained")
    args = parser.parse_args()
    try:
        print(json.dumps(replay(ROOT, args.scratch_root), indent=2))
        return 0
    except (ReplayError, OSError, ValueError, KeyError, TypeError, subprocess.SubprocessError) as error:
        print(f"Story 13.6 replay failed closed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
