#!/usr/bin/env python3
"""Stage a hermetic sealed-release state for release-governance runtime tests.

Builds, under a caller-supplied work root, a self-consistent sealed release state
derived from ``tests/ci-governance/fixtures/release-readiness-cases.json``:

- copies the release-definition files + package inventory so root-relative
  fingerprints are computable against the work root;
- materializes every manifest artifact/symbol with real bytes and rewrites the
  sealed checksums to match (so ``verify-manifest --root <work-root>`` passes);
- embeds live-format ``release_definition_fingerprints`` / ``package_set_fingerprint``
  (computed against the work root) and the live ``helper_version`` record;
- re-seals the manifest with the same canonical formula the helper uses.

Modes (positional): ``evidence`` writes ``<work-root>/evidence.json`` (the full
typed-evidence payload with ``context.dry_run=true``) for the classify-release
CLI exit-gate test; ``publish`` writes ``<work-root>/release-evidence/`` with the
sealed manifest plus a ``release-readiness.json`` claiming ready/authorized, for
the publisher pre-push audit negatives. ``publish --corrupt-artifact`` flips one
artifact byte AFTER sealing so every hash audit must fail closed.

Prints the staged manifest version on stdout (last line) so the caller can pass a
matching or deliberately mismatched ``--version``.
"""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import pathlib
import subprocess
import sys


def load_helper(repo_root: pathlib.Path):
    spec = importlib.util.spec_from_file_location("fc_release_evidence", repo_root / "eng/release_evidence.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=["evidence", "publish"])
    parser.add_argument("repo_root")
    parser.add_argument("work_root")
    parser.add_argument("--corrupt-artifact", action="store_true")
    args = parser.parse_args()

    repo_root = pathlib.Path(args.repo_root).resolve()
    work_root = pathlib.Path(args.work_root).resolve()
    work_root.mkdir(parents=True, exist_ok=True)
    helper = load_helper(repo_root)

    fixture = json.loads((repo_root / "tests/ci-governance/fixtures/release-readiness-cases.json").read_text(encoding="utf-8"))
    evidence = json.loads(json.dumps(fixture["base_evidence"]))
    manifest = evidence["manifest"]

    staged_files = set(helper.RELEASE_DEFINITION_FILES) | set(helper.FALLBACK_INVALIDATION_FILES) | {
        "eng/release-package-inventory.json",
    }
    for relative in sorted(staged_files):
        source = repo_root / relative
        if source.is_file():
            destination = work_root / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(source.read_bytes())

    # GOV-1 manifest v2 live verification is exact-object and policy-authorized. Build
    # a tiny committed-object repository rather than weakening the verifier for runtime
    # publisher tests whose real subject is the post-seal artifact/version audit.
    ci = manifest["workflow_provenance"]["ci"]
    release = manifest["workflow_provenance"]["release"]
    ci_evaluator = {"caller": ci["caller"], "reusable": ci["reusable"], "actions": ci["actions"]}
    ci_evaluator["definition_digest"] = helper.canonical_sha256(ci_evaluator)
    release_evaluator = {
        "caller": release["caller"],
        "reusable": release["reusable"],
        "actions": release["actions"],
    }
    release_evaluator["definition_digest"] = helper.canonical_sha256(release_evaluator)
    policy = {
        "schema": helper.POLICY_SCHEMA,
        "builds_identity": "github.com/hexalith/hexalith.builds",
        "trusted_identities": [
            {"identity": "github.com/hexalith/hexalith.frontcomposer", "local_path": "."},
            {"identity": "github.com/hexalith/hexalith.builds", "local_path": "builds"},
        ],
        "semantic_profiles": {
            "github.com/hexalith/hexalith.frontcomposer": "fixture-profile",
            "github.com/hexalith/hexalith.builds": "fixture-profile",
        },
        "profiles": {"fixture-profile": {}},
        "module_build_registry": {
            identity: {
                "disposition": "evidence-only",
                "solution": None,
                "builds_contract_source": "none",
                "restore_argv": None,
                "build_argv": None,
            }
            for identity in (
                "github.com/hexalith/hexalith.frontcomposer",
                "github.com/hexalith/hexalith.builds",
            )
        },
        "resource_limits": {
            "max_edges": 4096,
            "max_ls_tree_bytes_per_owner_commit": 67108864,
            "max_gitmodules_blob_bytes": 1048576,
            "max_catalog_blob_bytes": 4194304,
            "max_contract_tree_files": 16384,
            "max_contract_tree_blob_bytes": 16777216,
            "max_contract_tree_total_bytes": 268435456,
            "max_workflow_closure_depth": 16,
            "max_workflow_closure_sources": 256,
            "max_workflow_source_blob_bytes": 1048576,
            "max_workflow_source_total_bytes": 16777216,
        },
        "evaluator_authorizations": {
            "ci": [helper._authorization_projection(ci_evaluator, "ci")],
            "release": [helper._authorization_projection(release_evaluator, "release")],
            "post_release": [],
        },
    }
    (work_root / helper.POLICY_PATH).write_text(
        json.dumps(policy, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )
    for command in (
        ["git", "init", "-q"],
        ["git", "config", "user.email", "release-state@example.test"],
        ["git", "config", "user.name", "Release State Fixture"],
        ["git", "add", "."],
        ["git", "commit", "-q", "-m", "test: stage active release policy"],
    ):
        subprocess.run(command, cwd=work_root, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    base = subprocess.run(
        ["git", "rev-parse", "HEAD"],
        cwd=work_root,
        check=True,
        text=True,
        stdout=subprocess.PIPE,
    ).stdout.strip()
    subprocess.run(
        ["git", "commit", "-q", "--allow-empty", "-m", "test: stage exact release candidate"],
        cwd=work_root,
        check=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    candidate = subprocess.run(
        ["git", "rev-parse", "HEAD"],
        cwd=work_root,
        check=True,
        text=True,
        stdout=subprocess.PIPE,
    ).stdout.strip()
    policy_raw = subprocess.run(
        ["git", "show", f"{base}:{helper.POLICY_PATH}"],
        cwd=work_root,
        check=True,
        stdout=subprocess.PIPE,
    ).stdout
    engine = helper._load_dependency_graph_engine()
    manifest["commit_sha"] = candidate
    manifest["dependency_graph"] = engine.collect_graph(
        work_root,
        "github.com/hexalith/hexalith.frontcomposer",
        candidate,
        policy,
    )
    manifest["dependency_policy"] = {
        "schema": helper.POLICY_SCHEMA,
        "repository": "github.com/hexalith/hexalith.frontcomposer",
        "path": helper.POLICY_PATH,
        "commit": base,
        "sha256": hashlib.sha256(policy_raw).hexdigest(),
    }
    for section in (ci, release):
        section["caller"]["commit"] = candidate
    ci["run"]["head_sha"] = candidate
    manifest["workflow_provenance"]["definition_digest"] = helper.canonical_sha256(
        {
            "ci": {"caller": ci["caller"], "reusable": ci["reusable"], "actions": ci["actions"]},
            "release": release,
        }
    )

    for row in manifest["packages"]:
        row["commit_sha"] = manifest["commit_sha"]
        artifact = work_root / row["artifact_path"]
        artifact.parent.mkdir(parents=True, exist_ok=True)
        artifact.write_bytes(b"staged nupkg bytes for " + row["package_id"].encode("utf-8"))
        row["checksum"] = hashlib.sha256(artifact.read_bytes()).hexdigest()
        symbol = row.get("symbol_artifact", "")
        if isinstance(symbol, str) and symbol.endswith(".snupkg"):
            symbol_file = work_root / symbol
            symbol_file.parent.mkdir(parents=True, exist_ok=True)
            symbol_file.write_bytes(b"staged snupkg bytes for " + row["package_id"].encode("utf-8"))
            row["symbol_checksum"] = hashlib.sha256(symbol_file.read_bytes()).hexdigest()

    bundle = manifest.get("attestation_bundle")
    if isinstance(bundle, str) and bundle:
        bundle_file = work_root / bundle
        bundle_file.parent.mkdir(parents=True, exist_ok=True)
        bundle_file.write_bytes(b"{}")

    manifest["release_definition_fingerprints"] = helper.release_definition_fingerprints(work_root)
    manifest["package_set_fingerprint"] = helper.package_set_fingerprint(work_root)
    manifest["helper_version"] = helper.helper_version_record()
    canonical = json.dumps({k: v for k, v in manifest.items() if k != "seal"}, sort_keys=True, separators=(",", ":"))
    manifest["seal"] = {
        "algorithm": "sha256",
        "hash": helper.sha256_text(canonical),
        "sealed_at": "2026-08-02T00:00:00+00:00",
    }

    if args.mode == "evidence":
        evidence["context"]["dry_run"] = True
        (work_root / "evidence.json").write_text(json.dumps(evidence), encoding="utf-8")
    else:
        evidence_dir = work_root / "release-evidence"
        evidence_dir.mkdir(parents=True, exist_ok=True)
        (evidence_dir / "sealed-manifest.json").write_text(json.dumps(manifest), encoding="utf-8")
        (evidence_dir / "release-readiness.json").write_text(
            json.dumps({"classification": "ready", "publish_authorized": True}), encoding="utf-8")
        if args.corrupt_artifact:
            # Flip bytes AFTER sealing: every hash audit over this artifact must now fail.
            first = manifest["packages"][0]
            target = work_root / first["artifact_path"]
            target.write_bytes(b"post-seal mutated bytes that must never be pushed")

    versions = {str(row.get("version", "")) for row in manifest["packages"]}
    print(sorted(versions)[0] if versions else "")
    return 0


if __name__ == "__main__":
    sys.exit(main())
