"""Replay using only Git-visible packet/runtime inputs and a fresh npm install."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

packet = Path(__file__).resolve().parent
repository = packet.parents[4]
prefix = packet.relative_to(repository).as_posix() + "/"
visible = subprocess.check_output(
    ["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"],
    cwd=repository,
).decode().split("\0")
runtime = {
    "tests/e2e/package.json", "tests/e2e/package-lock.json",
    "tests/e2e/scripts/a11y-state-evidence.mjs",
    "tests/e2e/scripts/validate-a11y-artifacts.mjs",
    "tests/e2e/specimens/frontcomposer-specimen-manifest.json",
}
selected = sorted(path for path in set(visible) if path and (
    path.startswith(prefix) or path in runtime or
    path.startswith("tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/")
))
workspace = Path(tempfile.mkdtemp(prefix="story-13-6-git-visible-"))
copied = []
for path in selected:
    source = repository / path
    if not source.is_file() or source.is_symlink():
        raise ValueError(f"Only visible regular inputs are allowed: {path}")
    destination = workspace / path
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)
    copied.append({"path": path, "bytes": source.stat().st_size,
                   "sha256": hashlib.sha256(source.read_bytes()).hexdigest()})
destination_packet = workspace / packet.relative_to(repository)
if list(workspace.rglob("*.log")):
    raise ValueError("Ignored support logs were copied")
visible_logs = list(destination_packet.glob("*.log.txt"))
if len(visible_logs) != 29:
    raise ValueError("All 29 retained support logs are required")
environment = dict(os.environ)
environment.pop("NODE_PATH", None)
install_command = ["npm", "ci", "--ignore-scripts", "--no-audit", "--fund=false"]
installation = subprocess.run(install_command, cwd=workspace / "tests/e2e", env=environment,
                              stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
(packet / "git-visible-copy-npm-ci.txt").write_bytes(installation.stdout)
if installation.returncode:
    raise RuntimeError("Fresh pinned npm install failed; see git-visible-copy-npm-ci.txt")
replay_command = ["node", str(destination_packet / "audit-relocation.mjs")]
replay = subprocess.run(replay_command, cwd=workspace, env=environment,
                        stdout=subprocess.PIPE, stderr=subprocess.PIPE)
(packet / "git-visible-copy-replay.json").write_bytes(replay.stdout)
(packet / "git-visible-copy-replay-stderr.txt").write_bytes(replay.stderr)
if replay.returncode:
    raise RuntimeError("Git-visible-only replay failed; see retained stdout/stderr")
outcome = json.loads(replay.stdout)
proof = {
    "workspace": str(workspace), "copiedFiles": copied,
    "everyCopiedInputGitVisible": True, "copiedIgnoredSupportLogs": 0,
    "copiedVisibleSupportLogs": len(visible_logs),
    "originalCheckoutSupportFilesUsed": False,
    "dependencies": "Fresh npm ci from copied pinned package-lock.json; no original node_modules symlink",
    "installCommand": install_command, "installExitCode": installation.returncode,
    "replayCommand": replay_command, "replayExitCode": replay.returncode,
    "replayOutputSha256": hashlib.sha256(replay.stdout).hexdigest(),
    "supportLogsVerified": outcome["supportLogsVerified"],
    "structuralIntegrity": outcome["structuralIntegrity"],
    "overallAccessibilityClassification": outcome["overallAccessibilityClassification"],
}
(packet / "git-visible-copy-proof.json").write_text(json.dumps(proof, indent=2) + "\n")
print(json.dumps({key: value for key, value in proof.items() if key != "copiedFiles"}, indent=2))
