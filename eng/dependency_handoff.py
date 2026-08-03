#!/usr/bin/env python3
"""Exact-candidate dependency handoff contracts for GOV-1 (AD-13/AD-15).

The helper validates already-authenticated run/artifact inputs. GitHub Actions callers are
responsible for obtaining those bytes through read-only Actions APIs and for checking the
remote run metadata before invoking this offline, fail-closed contract implementation.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any

import dependency_graph as dg

CI_HANDOFF_SCHEMA = "hexalith.dependency-release-handoff.v1"
RELEASE_HANDOFF_SCHEMA = "hexalith.release-verification-handoff.v1"
ROOT_REPOSITORY = "github.com/hexalith/hexalith.frontcomposer"
CI_WORKFLOW_PATH = ".github/workflows/ci.yml"
RELEASE_WORKFLOW_PATH = ".github/workflows/release.yml"
_IDENTITY_RE = re.compile(r"github\.com/[a-z0-9._-]+/[a-z0-9._-]+")


class HandoffError(dg.GraphError):
    """Raised when a handoff is incomplete, inconsistent, or unauthorized."""


def _closed(value: Any, members: set[str], context: str) -> dict[str, Any]:
    if not isinstance(value, dict) or set(value) != members:
        found = sorted(value) if isinstance(value, dict) else type(value).__name__
        raise HandoffError(f"{context} members must be exactly {sorted(members)}, found {found}")
    return value


def _positive_integer(value: Any, context: str) -> int:
    if isinstance(value, bool) or not isinstance(value, int) or value < 1:
        raise HandoffError(f"{context} must be a JSON integer >= 1")
    return value


def _sha256(value: Any, context: str) -> str:
    if not isinstance(value, str) or re.fullmatch(r"[0-9a-f]{64}", value) is None:
        raise HandoffError(f"{context} must be lowercase 64-hex")
    return value


def _source(value: Any, context: str, *, workflow: bool) -> dict[str, Any]:
    path_name = "workflow_path" if workflow else "path"
    source = _closed(value, {"repository", path_name, "commit", "blob_sha256"}, context)
    if not isinstance(source["repository"], str) or _IDENTITY_RE.fullmatch(source["repository"]) is None:
        raise HandoffError(f"{context}.repository must be a normalized ASCII identity")
    dg.normalize_path(source[path_name], f"{context}.{path_name}")
    dg.require_commit(source["commit"], f"{context}.commit")
    _sha256(source["blob_sha256"], f"{context}.blob_sha256")
    return source


def validate_evaluator(value: Any, context: str = "evaluator") -> dict[str, Any]:
    evaluator = _closed(value, {"caller", "reusable", "actions", "definition_digest"}, context)
    _source(evaluator["caller"], f"{context}.caller", workflow=True)
    _source(evaluator["reusable"], f"{context}.reusable", workflow=True)
    actions = evaluator["actions"]
    if not isinstance(actions, list):
        raise HandoffError(f"{context}.actions must be an array")
    for index, action in enumerate(actions):
        _source(action, f"{context}.actions[{index}]", workflow=False)
    expected_actions = sorted(
        actions,
        key=lambda item: (item["repository"], item["path"], item["commit"], item["blob_sha256"]),
    )
    if actions != expected_actions or len({dg.canonical_bytes(item) for item in actions}) != len(actions):
        raise HandoffError(f"{context}.actions must be ordinally sorted and unique")
    expected_digest = dg.canonical_digest({
        "caller": evaluator["caller"],
        "reusable": evaluator["reusable"],
        "actions": actions,
    })
    if evaluator["definition_digest"] != expected_digest:
        raise HandoffError(f"{context}.definition_digest mismatch: expected {expected_digest}")
    return evaluator


def require_evaluator_authorized(policy: dict[str, Any], stage: str, evaluator: dict[str, Any]) -> dict[str, Any]:
    validate_evaluator(evaluator)
    caller = evaluator["caller"]
    projection = {
        "stage": stage,
        "caller": {
            "repository": caller["repository"],
            "workflow_path": caller["workflow_path"],
            "blob_sha256": caller["blob_sha256"],
        },
        "reusable": evaluator["reusable"],
        "actions": evaluator["actions"],
    }
    projection["closure_digest"] = dg.canonical_digest(projection)
    matches = [row for row in policy["evaluator_authorizations"][stage] if row == projection]
    if len(matches) != 1:
        raise HandoffError(
            f"{stage} evaluator must project exactly one active-policy authorization; found {len(matches)}"
        )
    return matches[0]


def validate_policy_projection(value: Any) -> dict[str, Any]:
    projection = _closed(
        value, {"schema", "repository", "path", "commit", "sha256"}, "dependency_policy"
    )
    if projection["schema"] != dg.POLICY_SCHEMA:
        raise HandoffError("dependency_policy schema mismatch")
    if projection["repository"] != ROOT_REPOSITORY or projection["path"] != dg.POLICY_PATH:
        raise HandoffError("dependency_policy must name the canonical FrontComposer policy coordinate")
    dg.require_commit(projection["commit"], "dependency_policy.commit")
    _sha256(projection["sha256"], "dependency_policy.sha256")
    return projection


def _load_active_policy(root: Path, projection: dict[str, Any]) -> dict[str, Any]:
    policy, _raw, expected = dg.load_policy_at_commit(root, projection["commit"])
    if expected != projection:
        raise HandoffError("dependency_policy raw-byte coordinate does not match the sealed projection")
    return policy


def validate_ci_handoff(value: Any, *, root: Path | None = None) -> dict[str, Any]:
    handoff = _closed(
        value,
        {"schema", "run", "revisions", "evaluator", "dependency_policy", "dependency_graph"},
        "CI handoff",
    )
    if handoff["schema"] != CI_HANDOFF_SCHEMA:
        raise HandoffError(f"CI handoff schema must be {CI_HANDOFF_SCHEMA!r}")
    run = _closed(
        handoff["run"],
        {"repository", "workflow_path", "run_id", "run_attempt", "event", "branch", "candidate"},
        "CI handoff run",
    )
    if run["repository"] != ROOT_REPOSITORY or run["workflow_path"] != CI_WORKFLOW_PATH:
        raise HandoffError("CI handoff run repository/workflow_path mismatch")
    if run["event"] != "push" or run["branch"] != "main":
        raise HandoffError("release-eligible CI handoff must come from a push to main")
    _positive_integer(run["run_id"], "CI handoff run_id")
    _positive_integer(run["run_attempt"], "CI handoff run_attempt")
    candidate = dg.require_commit(run["candidate"], "CI handoff candidate")
    revisions = _closed(handoff["revisions"], {"base", "candidate", "merge_base"}, "CI handoff revisions")
    dg.require_commit(revisions["base"], "CI handoff base")
    dg.require_commit(revisions["candidate"], "CI handoff revisions candidate")
    if revisions["merge_base"] is not None:
        raise HandoffError("push CI handoff merge_base must be null")
    graph = dg.validate_graph_envelope(handoff["dependency_graph"])
    if candidate != revisions["candidate"] or candidate != graph["root"]["commit"]:
        raise HandoffError("CI handoff candidate, revisions, and graph root must be identical")
    evaluator = validate_evaluator(handoff["evaluator"], "CI handoff evaluator")
    projection = validate_policy_projection(handoff["dependency_policy"])
    if projection["commit"] != revisions["base"]:
        raise HandoffError("CI handoff active-policy commit must equal the non-zero push base")
    if (
        evaluator["caller"]["repository"] != ROOT_REPOSITORY
        or evaluator["caller"]["workflow_path"] != CI_WORKFLOW_PATH
        or evaluator["caller"]["commit"] != candidate
    ):
        raise HandoffError("CI handoff evaluator caller must be the exact candidate CI workflow blob")
    if root is not None:
        policy = _load_active_policy(root, projection)
        require_evaluator_authorized(policy, "ci", evaluator)
        live = dg.collect_graph(root, graph["root"]["repository"], candidate, policy)
        if live != graph:
            raise HandoffError("CI handoff dependency graph differs from live exact-candidate graph")
    return handoff


def create_ci_handoff(
    *, run_id: int, run_attempt: int, base: str, candidate: str,
    evaluator: dict[str, Any], dependency_policy: dict[str, Any], dependency_graph: dict[str, Any],
    policy: dict[str, Any],
) -> dict[str, Any]:
    require_evaluator_authorized(policy, "ci", evaluator)
    handoff = {
        "schema": CI_HANDOFF_SCHEMA,
        "run": {
            "repository": ROOT_REPOSITORY,
            "workflow_path": CI_WORKFLOW_PATH,
            "run_id": run_id,
            "run_attempt": run_attempt,
            "event": "push",
            "branch": "main",
            "candidate": candidate,
        },
        "revisions": {"base": base, "candidate": candidate, "merge_base": None},
        "evaluator": evaluator,
        "dependency_policy": dependency_policy,
        "dependency_graph": dependency_graph,
    }
    return validate_ci_handoff(handoff)


def validate_release_handoff(
    value: Any,
    *,
    ci_handoff_raw: bytes | None = None,
    root: Path | None = None,
) -> dict[str, Any]:
    handoff = _closed(
        value,
        {"schema", "release_run", "ci_handoff", "candidate", "dependency_policy", "release", "manifest", "assets", "evaluator"},
        "Release handoff",
    )
    if handoff["schema"] != RELEASE_HANDOFF_SCHEMA:
        raise HandoffError(f"Release handoff schema must be {RELEASE_HANDOFF_SCHEMA!r}")
    release_run = _closed(
        handoff["release_run"], {"repository", "workflow_path", "run_id", "run_attempt", "conclusion"},
        "Release handoff release_run",
    )
    if release_run["repository"] != ROOT_REPOSITORY or release_run["workflow_path"] != RELEASE_WORKFLOW_PATH:
        raise HandoffError("Release handoff run repository/workflow_path mismatch")
    _positive_integer(release_run["run_id"], "Release handoff run_id")
    _positive_integer(release_run["run_attempt"], "Release handoff run_attempt")
    if not isinstance(release_run["conclusion"], str) or not release_run["conclusion"]:
        raise HandoffError("Release handoff conclusion must be non-empty")
    ci_ref = _closed(
        handoff["ci_handoff"], {"repository", "workflow_path", "run_id", "run_attempt", "evidence_sha256"},
        "Release handoff ci_handoff",
    )
    if ci_ref["repository"] != ROOT_REPOSITORY or ci_ref["workflow_path"] != CI_WORKFLOW_PATH:
        raise HandoffError("Release handoff CI repository/workflow_path mismatch")
    _positive_integer(ci_ref["run_id"], "Release handoff CI run_id")
    _positive_integer(ci_ref["run_attempt"], "Release handoff CI run_attempt")
    _sha256(ci_ref["evidence_sha256"], "Release handoff CI evidence_sha256")
    candidate = dg.require_commit(handoff["candidate"], "Release handoff candidate")
    policy_projection = validate_policy_projection(handoff["dependency_policy"])
    release = _closed(handoff["release"], {"version", "tag", "github_release_id", "published"}, "Release handoff release")
    if not isinstance(release["published"], bool):
        raise HandoffError("Release handoff published must be a JSON boolean")
    manifest = _closed(handoff["manifest"], {"path", "sha256", "seal"}, "Release handoff manifest")
    assets = handoff["assets"]
    if not isinstance(assets, list):
        raise HandoffError("Release handoff assets must be an array")
    for index, asset in enumerate(assets):
        row = _closed(asset, {"name", "sha256", "size"}, f"Release handoff assets[{index}]")
        if not isinstance(row["name"], str) or not row["name"] or not row["name"].isascii():
            raise HandoffError(f"Release handoff assets[{index}].name is invalid")
        _sha256(row["sha256"], f"Release handoff assets[{index}].sha256")
        if isinstance(row["size"], bool) or not isinstance(row["size"], int) or row["size"] < 0:
            raise HandoffError(f"Release handoff assets[{index}].size must be a nonnegative integer")
    expected_assets = sorted(assets, key=lambda row: (row["name"], row["sha256"], row["size"]))
    if assets != expected_assets or len({dg.canonical_bytes(item) for item in assets}) != len(assets):
        raise HandoffError("Release handoff assets must be ordinally sorted and unique")
    if release["published"]:
        if any(release[name] is None for name in ("version", "tag", "github_release_id")):
            raise HandoffError("published Release handoff requires version, tag, and release ID")
        if not isinstance(release["version"], str) or not release["version"]:
            raise HandoffError("published Release handoff version must be non-empty")
        if not isinstance(release["tag"], str) or not release["tag"]:
            raise HandoffError("published Release handoff tag must be non-empty")
        _positive_integer(release["github_release_id"], "Release handoff github_release_id")
        if any(manifest[name] is None for name in ("path", "sha256", "seal")) or not assets:
            raise HandoffError("published Release handoff requires manifest coordinates and assets")
        dg.normalize_path(manifest["path"], "Release handoff manifest.path")
        _sha256(manifest["sha256"], "Release handoff manifest.sha256")
        _sha256(manifest["seal"], "Release handoff manifest.seal")
    else:
        if any(release[name] is not None for name in ("version", "tag", "github_release_id")):
            raise HandoffError("unpublished Release handoff must use null release coordinates")
        if any(manifest[name] is not None for name in ("path", "sha256", "seal")) or assets:
            raise HandoffError("unpublished Release handoff must use a null manifest and empty assets")
    evaluator = validate_evaluator(handoff["evaluator"], "Release handoff evaluator")
    if evaluator["caller"]["repository"] != ROOT_REPOSITORY or evaluator["caller"]["workflow_path"] != RELEASE_WORKFLOW_PATH:
        raise HandoffError("Release handoff evaluator caller must be the canonical Release workflow")
    if ci_handoff_raw is not None:
        if hashlib.sha256(ci_handoff_raw).hexdigest() != ci_ref["evidence_sha256"]:
            raise HandoffError("Release handoff raw CI evidence hash mismatch")
        ci_value = dg.load_json_bytes(ci_handoff_raw, "CI handoff")
        ci = validate_ci_handoff(ci_value, root=root)
        if candidate != ci["run"]["candidate"] or policy_projection != ci["dependency_policy"]:
            raise HandoffError("Release handoff candidate/policy differs from authenticated CI handoff")
        if ci_ref["run_id"] != ci["run"]["run_id"] or ci_ref["run_attempt"] != ci["run"]["run_attempt"]:
            raise HandoffError("Release handoff CI run coordinate mismatch")
    if root is not None:
        policy = _load_active_policy(root, policy_projection)
        require_evaluator_authorized(policy, "release", evaluator)
    return handoff


def create_release_handoff(
    *,
    release_run_id: int,
    release_run_attempt: int,
    conclusion: str,
    ci_handoff_raw: bytes,
    evaluator: dict[str, Any],
    policy: dict[str, Any],
    release: dict[str, Any],
    manifest: dict[str, Any],
    assets: list[dict[str, Any]],
) -> dict[str, Any]:
    """Create the mandatory AD-15 success/failure/partial-attempt handoff."""
    ci_value = dg.load_json_bytes(ci_handoff_raw, "CI handoff")
    ci = validate_ci_handoff(ci_value)
    require_evaluator_authorized(policy, "release", evaluator)
    handoff = {
        "schema": RELEASE_HANDOFF_SCHEMA,
        "release_run": {
            "repository": ROOT_REPOSITORY,
            "workflow_path": RELEASE_WORKFLOW_PATH,
            "run_id": release_run_id,
            "run_attempt": release_run_attempt,
            "conclusion": conclusion,
        },
        "ci_handoff": {
            "repository": ROOT_REPOSITORY,
            "workflow_path": CI_WORKFLOW_PATH,
            "run_id": ci["run"]["run_id"],
            "run_attempt": ci["run"]["run_attempt"],
            "evidence_sha256": hashlib.sha256(ci_handoff_raw).hexdigest(),
        },
        "candidate": ci["run"]["candidate"],
        "dependency_policy": ci["dependency_policy"],
        "release": release,
        "manifest": manifest,
        "assets": assets,
        "evaluator": evaluator,
    }
    return validate_release_handoff(handoff, ci_handoff_raw=ci_handoff_raw)


def _load(path: str) -> tuple[Any, bytes]:
    raw = Path(path).read_bytes()
    return dg.load_json_bytes(raw, path), raw


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", default=".")
    sub = parser.add_subparsers(dest="command", required=True)
    verify_ci = sub.add_parser("verify-ci")
    verify_ci.add_argument("--handoff", required=True)
    verify_ci.add_argument("--live", action="store_true")
    verify_release = sub.add_parser("verify-release")
    verify_release.add_argument("--handoff", required=True)
    verify_release.add_argument("--ci-handoff", required=True)
    verify_release.add_argument("--live", action="store_true")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    root = Path(args.root).resolve() if args.live else None
    try:
        value, raw = _load(args.handoff)
        if args.command == "verify-ci":
            validate_ci_handoff(value, root=root)
        elif args.command == "verify-release":
            _ci_value, ci_raw = _load(args.ci_handoff)
            validate_release_handoff(value, ci_handoff_raw=ci_raw, root=root)
        else:
            raise HandoffError(f"unsupported command {args.command!r}")
        print(json.dumps({"ok": True, "sha256": hashlib.sha256(raw).hexdigest()}, sort_keys=True))
        return 0
    except dg.GraphError as exc:
        print(json.dumps({"ok": False, "error": str(exc)}, sort_keys=True))
        return 1


if __name__ == "__main__":
    sys.exit(main())
