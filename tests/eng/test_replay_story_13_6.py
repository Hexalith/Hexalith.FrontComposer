"""Historical reconstruction must fail before executing unverifiable inputs."""

from __future__ import annotations

import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest import mock


SCRIPT = Path(__file__).resolve().parents[2] / "eng/replay_story_13_6.py"
SPEC = importlib.util.spec_from_file_location("replay_story_13_6", SCRIPT)
proof = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(proof)


def identity(data):
    return {"bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()}


def valid_audit():
    return {"structuralIntegrity": "passed", "rawReplayExitCode": 1, "overallAccessibilityClassification": "blocked",
            "completeStateRowsPerEngine": 116, "supportLogsVerified": 6, "runtimeInputsVerified": 3,
            "completeLaneIdentitiesVerified": proof.ENGINES, "replay": {"summary": [
                {"lane": engine, "completeGate": True, "filesVerified": 693, "requiredStates": 116,
                 "outcomesUnchanged": True, "junitOutcomesUnchanged": True, "artifactValidationExitCode": 1,
                 "stats": {"expected": 252 if engine == "chromium" else 250, "unexpected": 6,
                           "skipped": 0 if engine == "chromium" else 2, "flaky": 0}} for engine in proof.ENGINES]}}


def replay_processes(audit, during_install=lambda: None):
    def execute(command, **kwargs):
        if command[1] == "ci":
            package = kwargs["cwd"] / "node_modules/playwright-core/package.json"
            package.parent.mkdir(parents=True)
            package.write_text('{"version":"1.63.0"}')
            during_install()
            return subprocess.CompletedProcess(command, 0)
        return subprocess.CompletedProcess(command, 0, json.dumps(audit), "")
    return execute


class ReplayStory136Tests(unittest.TestCase):
    def fixture(self, root):
        packet = root / proof.PACKET
        packet.mkdir(parents=True)
        runtime_paths = ["tests/e2e/scripts/a11y-state-evidence.mjs", "tests/e2e/scripts/validate-a11y-artifacts.mjs",
                         "tests/e2e/specimens/frontcomposer-specimen-manifest.json"]
        blobs = {path: b"historical\n" for path in runtime_paths}
        blobs.update({"tests/e2e/package.json": b"{}\n", "tests/e2e/package-lock.json": b"{}\n"})
        baselines = {f"tests/e2e/specs/baseline-{number}.png": b"raw\r\nPNG\x00" for number in range(24)}
        blobs.update(baselines)
        files = {path: {"recipe": "raw" if path.endswith(".png") else "crlf",
                        **identity(proof.reconstruct_bytes(data, "raw" if path.endswith(".png") else "crlf"))}
                 for path, data in blobs.items()}
        candidate = {"protectedBaselines": {path: identity(data)["sha256"] for path, data in baselines.items()}}
        runtime = {"files": {path: identity(proof.reconstruct_bytes(blobs[path], "crlf")) for path in runtime_paths}}
        (packet / "candidate.json").write_text(json.dumps(candidate))
        (packet / "runtime-inputs.json").write_text(json.dumps(runtime))
        manifest = {"schema": "frontcomposer.story-13-6-historical-runtime.v1", "revision": proof.REVISION,
                    "packet": proof.PACKET, "files": files,
                    "packet_files": {path.name: identity(path.read_bytes()) for path in packet.iterdir()}}
        manifest_path = root / proof.MANIFEST.relative_to(proof.ROOT)
        manifest_path.parent.mkdir(parents=True)
        manifest_path.write_text(json.dumps(manifest))
        return manifest, blobs

    def test_exact_line_endings_and_binary_bytes(self):
        raw = b"".join(f"line-{number}\n".encode() for number in range(1, 17))
        reconstructed = proof.reconstruct_bytes(raw, "crlf-except-lines-14-15")
        self.assertEqual([f"line-{number}".encode() + (b"\n" if number in (14, 15) else b"\r\n")
                          for number in range(1, 17)], reconstructed.splitlines(keepends=True))
        self.assertEqual(b"PNG\r\n\x00\n", proof.reconstruct_bytes(b"PNG\r\n\x00\n", "raw"))
        with self.assertRaises(proof.ReplayError):
            proof.reconstruct_bytes(raw, "guess")

    def test_reconstruction_is_independent_of_live_helpers_and_preserves_source(self):
        with tempfile.TemporaryDirectory() as temporary:
            root, scratch = Path(temporary) / "repo", Path(temporary) / "scratch"
            manifest, blobs = self.fixture(root)
            live = root / "tests/e2e/scripts/a11y-state-evidence.mjs"
            live.parent.mkdir(parents=True)
            live.write_bytes(b"mutable current helper")
            before = {str(path.relative_to(root)): path.read_bytes() for path in root.rglob("*") if path.is_file()}
            with mock.patch.object(proof, "historical_blob", side_effect=lambda repository, path: blobs[path]):
                proof.materialize(root, scratch, manifest)
            self.assertEqual(b"historical\r\n", (scratch / "tests/e2e/scripts/a11y-state-evidence.mjs").read_bytes())
            self.assertEqual(before, {str(path.relative_to(root)): path.read_bytes() for path in root.rglob("*") if path.is_file()})
            self.assertFalse(any(path.is_symlink() for path in scratch.rglob("*")))

    def test_corrupt_runtime_baseline_and_packet_fail_before_execution(self):
        for kind in ("runtime", "baseline", "packet"):
            with self.subTest(kind=kind), tempfile.TemporaryDirectory() as temporary:
                root, parent = Path(temporary) / "repo", Path(temporary) / "scratch"
                parent.mkdir()
                manifest, blobs = self.fixture(root)
                if kind == "packet":
                    (root / proof.PACKET / "candidate.json").write_bytes(b"corrupt")
                else:
                    path = next(path for path in blobs if path.endswith(".png") == (kind == "baseline"))
                    blobs[path] += b"corrupt"
                with mock.patch.object(proof, "historical_blob", side_effect=lambda repository, path: blobs[path]), \
                        mock.patch.object(proof.subprocess, "run") as execute:
                    with self.assertRaisesRegex(proof.ReplayError, "Size/hash mismatch"):
                        proof.replay(root, parent)
                    execute.assert_not_called()

    def test_missing_historical_object_fails_closed(self):
        with mock.patch.object(proof.subprocess, "run", return_value=subprocess.CompletedProcess([], 128, b"", b"missing")):
            with self.assertRaisesRegex(proof.ReplayError, "Missing historical Git input: " + proof.REVISION):
                proof.historical_blob(Path("."), "tests/e2e/package.json")

    def test_scratch_within_checkout_is_rejected_before_writes(self):
        with tempfile.TemporaryDirectory() as temporary, mock.patch.object(proof.tempfile, "mkdtemp") as create:
            root = Path(temporary)
            with self.assertRaisesRegex(proof.ReplayError, "outside the repository"):
                proof.replay(root, root / "scratch")
            create.assert_not_called()

    def test_node_resolution_cannot_use_parent_environment(self):
        with tempfile.TemporaryDirectory() as temporary, mock.patch.dict(os.environ, {
                "NODE_PATH": "/outside/modules", "NODE_OPTIONS": "--import=/outside/code.mjs",
                "TEMP": "/outside/temp", "TMP": "/outside/tmp", "TMPDIR": "/outside/tmpdir"}):
            environment = proof.isolated_environment(Path(temporary))
            self.assertNotIn("NODE_PATH", environment)
            self.assertNotIn("NODE_OPTIONS", environment)
            for variable in ("TMPDIR", "TEMP", "TMP"):
                self.assertEqual(str(Path(temporary) / "replay-output"), environment[variable])

    def test_receipt_hashes_the_manifest_snapshot_used_for_reconstruction(self):
        with tempfile.TemporaryDirectory() as temporary:
            root, parent = Path(temporary) / "repo", Path(temporary) / "scratch"
            parent.mkdir()
            _, blobs = self.fixture(root)
            manifest = root / proof.MANIFEST.relative_to(proof.ROOT)
            original_bytes = manifest.read_bytes()
            with mock.patch.object(proof, "historical_blob", side_effect=lambda repository, path: blobs[path]), \
                    mock.patch.object(proof.shutil, "which", side_effect=lambda name: name), \
                    mock.patch.object(proof.subprocess, "check_output", return_value="test-version\n"), \
                    mock.patch.object(proof.subprocess, "run", side_effect=replay_processes(
                        valid_audit(), lambda: manifest.write_bytes(b"mutated during replay"))):
                receipt = proof.replay(root, parent)
            self.assertEqual(identity(original_bytes)["sha256"], receipt["reconstructionManifestSha256"])
            self.assertNotEqual(identity(manifest.read_bytes())["sha256"], receipt["reconstructionManifestSha256"])

    def test_failures_retain_bounded_diagnostics_and_report_scratch_location(self):
        for stage in ("materialization", "versions", "outcomes"):
            with self.subTest(stage=stage), tempfile.TemporaryDirectory() as temporary:
                root, parent = Path(temporary) / "repo", Path(temporary) / "scratch"
                parent.mkdir()
                _, blobs = self.fixture(root)
                if stage == "materialization":
                    (root / proof.PACKET / "candidate.json").write_bytes(b"corrupt")
                audit = valid_audit()
                audit["rawReplayExitCode"] = 0
                with mock.patch.object(proof, "historical_blob", side_effect=lambda repository, path: blobs[path]), \
                        mock.patch.object(proof.shutil, "which", side_effect=lambda name: None if stage == "versions" else name), \
                        mock.patch.object(proof.subprocess, "check_output", return_value="test-version\n"), \
                        mock.patch.object(proof.subprocess, "run", side_effect=replay_processes(audit)):
                    with self.assertRaises(proof.ReplayError) as error:
                        proof.replay(root, parent)
                scratch, = parent.iterdir()
                self.assertIn(f"scratch retained at {scratch}", str(error.exception))
                self.assertIn("see failure.json", str(error.exception))
                failure = (scratch / "failure.json").read_bytes()
                self.assertLessEqual(len(failure), 4096)
                self.assertEqual(stage, json.loads(failure)["stage"])
                self.assertFalse((scratch / "receipt.json").exists())

    def test_real_cli_rejects_an_external_corrupt_fixture_without_success_receipt(self):
        with tempfile.TemporaryDirectory() as temporary:
            root, parent = Path(temporary) / "copied-repo", Path(temporary) / "scratch"
            parent.mkdir()
            self.fixture(root)
            copied_script = root / "eng/replay_story_13_6.py"
            copied_script.parent.mkdir()
            copied_script.write_bytes(SCRIPT.read_bytes())
            (root / proof.PACKET / "candidate.json").write_bytes(b"corrupt")

            result = subprocess.run([sys.executable, str(copied_script), "--scratch-root", str(parent)],
                                    cwd=root, text=True, capture_output=True, check=False)

            self.assertEqual(1, result.returncode, result.stderr)
            self.assertEqual("", result.stdout)
            self.assertIn("Story 13.6 replay failed closed:", result.stderr)
            self.assertIn("Size/hash mismatch:", result.stderr)
            scratch, = parent.iterdir()
            self.assertIn(f"scratch retained at {scratch}", result.stderr)
            self.assertTrue((scratch / "failure.json").is_file())
            self.assertFalse((scratch / "receipt.json").exists())

    def test_reconstruction_path_escape_and_symlink_are_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            root, outside = Path(temporary) / "scratch", Path(temporary) / "outside"
            root.mkdir()
            outside.mkdir()
            (root / "linked").symlink_to(outside, target_is_directory=True)
            for path in ("../outside", "/absolute", "linked/escaped", "a/../b", "a\\b"):
                with self.subTest(path=path), self.assertRaises(proof.ReplayError):
                    proof.safe_path(root, path)

    def test_only_exact_blocked_historical_outcomes_can_pass(self):
        audit = valid_audit()
        self.assertEqual({"passed": 752, "failed": 18, "skipped": 4, "flaky": 0}, proof.check_outcomes(audit))
        mutations = [("rawReplayExitCode", 0), ("overallAccessibilityClassification", "ready"),
                     ("completeStateRowsPerEngine", 115), ("structuralIntegrity", "failed")]
        for key, value in mutations:
            changed = copy.deepcopy(audit)
            changed[key] = value
            with self.subTest(key=key), self.assertRaises(proof.ReplayError):
                proof.check_outcomes(changed)
        for key, value in (("filesVerified", 692), ("requiredStates", 115), ("outcomesUnchanged", False),
                           ("artifactValidationExitCode", 0), ("junitOutcomesUnchanged", False)):
            changed = copy.deepcopy(audit)
            changed["replay"]["summary"][0][key] = value
            with self.subTest(key=key), self.assertRaises(proof.ReplayError):
                proof.check_outcomes(changed)


if __name__ == "__main__":
    unittest.main()
