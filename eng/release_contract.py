#!/usr/bin/env python3
"""Fail-closed contracts for FrontComposer's operator-controlled release entry point."""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import subprocess
import sys
from typing import Any

EXPECTED_PACKAGES = (
    ("Hexalith.FrontComposer.Cli", "src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj"),
    ("Hexalith.FrontComposer.Contracts", "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj"),
    ("Hexalith.FrontComposer.Contracts.UI", "src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj"),
    ("Hexalith.FrontComposer.Mcp", "src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj"),
    ("Hexalith.FrontComposer.Schema", "src/Hexalith.FrontComposer.Schema/Hexalith.FrontComposer.Schema.csproj"),
    ("Hexalith.FrontComposer.Shell", "src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj"),
    ("Hexalith.FrontComposer.SourceTools", "src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj"),
    ("Hexalith.FrontComposer.Testing", "src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj"),
)
SHA_RE = re.compile(r"[0-9a-f]{40}")


class ContractError(ValueError):
    """Raised when a release boundary input is missing or ambiguous."""


def _strict_load(path: pathlib.Path) -> Any:
    def reject_duplicate(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
        result: dict[str, Any] = {}
        for key, value in pairs:
            if key in result:
                raise ContractError(f"{path}: duplicate JSON member {key!r}")
            result[key] = value
        return result

    try:
        return json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=reject_duplicate)
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise ContractError(f"{path}: malformed JSON: {exc}") from exc


def validate_package_manifest(root: pathlib.Path, manifest_path: pathlib.Path, expected_count: int) -> dict[str, Any]:
    value = _strict_load(manifest_path)
    if not isinstance(value, dict) or set(value) != {"packages"}:
        raise ContractError("package manifest must be an object with exactly the 'packages' member")
    rows = value["packages"]
    if not isinstance(rows, list):
        raise ContractError("package manifest packages must be an array")
    if isinstance(expected_count, bool) or expected_count < 1 or len(rows) != expected_count:
        raise ContractError(f"package manifest must contain exactly {expected_count} rows; found {len(rows)}")
    actual: list[tuple[str, str]] = []
    for index, row in enumerate(rows):
        if not isinstance(row, dict) or set(row) != {"id", "project"}:
            raise ContractError(f"packages[{index}] must contain exactly id and project")
        package_id, project = row["id"], row["project"]
        if not isinstance(package_id, str) or not isinstance(project, str):
            raise ContractError(f"packages[{index}] id and project must be strings")
        if pathlib.PurePosixPath(project).is_absolute() or ".." in pathlib.PurePosixPath(project).parts:
            raise ContractError(f"packages[{index}] project must be a confined POSIX path")
        if not (root / project).is_file():
            raise ContractError(f"packages[{index}] project does not exist: {project}")
        actual.append((package_id, project))
    if tuple(actual) != EXPECTED_PACKAGES:
        raise ContractError("package manifest differs from the approved ordered eight-package inventory")

    inventory = _strict_load(root / "eng/release-package-inventory.json")
    packable = tuple(
        (row.get("package_id"), row.get("project"))
        for row in inventory.get("packages", [])
        if isinstance(row, dict) and row.get("packable") is True
    ) if isinstance(inventory, dict) else ()
    if packable != EXPECTED_PACKAGES:
        raise ContractError("release-package-inventory packable rows drifted from the approved package manifest")
    return value


def select_exact_ci_run(dispatch_sha: str, live_ref: Any, runs_response: Any) -> dict[str, int]:
    """Validate GitHub API responses and select one successful exact-source push CI run."""
    if not isinstance(dispatch_sha, str) or SHA_RE.fullmatch(dispatch_sha) is None:
        raise ContractError("dispatch SHA must be lowercase 40-hex")
    if not isinstance(live_ref, dict) or set(live_ref) < {"ref", "object"}:
        raise ContractError("main-ref API response is malformed")
    obj = live_ref.get("object")
    if live_ref.get("ref") != "refs/heads/main" or not isinstance(obj, dict) or obj.get("type") != "commit":
        raise ContractError("main-ref API response does not identify refs/heads/main as a commit")
    live_sha = obj.get("sha")
    if not isinstance(live_sha, str) or SHA_RE.fullmatch(live_sha) is None:
        raise ContractError("main-ref API response has a malformed SHA")
    if live_sha != dispatch_sha:
        raise ContractError("main advanced after dispatch")
    if not isinstance(runs_response, dict) or set(runs_response) < {"total_count", "workflow_runs"}:
        raise ContractError("CI-runs API response is malformed")
    runs = runs_response.get("workflow_runs")
    if isinstance(runs_response.get("total_count"), bool) or not isinstance(runs_response.get("total_count"), int) or not isinstance(runs, list):
        raise ContractError("CI-runs API response has invalid field types")
    if runs_response["total_count"] != len(runs):
        raise ContractError("CI-runs API response is ambiguous or paginated")
    matches: list[dict[str, Any]] = []
    for run in runs:
        if not isinstance(run, dict):
            raise ContractError("CI-runs API response contains a non-object run")
        if run.get("head_sha") == dispatch_sha:
            matches.append(run)
    if len(matches) != 1:
        raise ContractError(f"expected exactly one exact-source CI run; found {len(matches)}")
    run = matches[0]
    required = {
        "event": "push", "head_branch": "main", "status": "completed", "conclusion": "success",
        "path": ".github/workflows/ci.yml",
    }
    for field, expected in required.items():
        if run.get(field) != expected:
            raise ContractError(f"exact-source CI {field} must be {expected!r}")
    run_id, attempt = run.get("id"), run.get("run_attempt")
    if isinstance(run_id, bool) or not isinstance(run_id, int) or run_id < 1:
        raise ContractError("exact-source CI id must be a positive integer")
    if isinstance(attempt, bool) or not isinstance(attempt, int) or attempt < 1:
        raise ContractError("exact-source CI run_attempt must be a positive integer")
    return {"run_id": run_id, "run_attempt": attempt}


def validate_publication(
    expected_sha: str,
    expected_tag: str,
    release: Any,
    tag_ref: Any,
    tag_objects: Any,
    *,
    require_immutable: bool = False,
) -> None:
    if SHA_RE.fullmatch(expected_sha) is None or not re.fullmatch(r"v[0-9]+\.[0-9]+\.[0-9]+(?:[+-][0-9A-Za-z.-]+)?", expected_tag):
        raise ContractError("expected publication coordinate is malformed")
    if not isinstance(release, dict) or release.get("tag_name") != expected_tag or release.get("draft") is not False:
        raise ContractError("GitHub Release must be a non-draft object for the expected tag")
    if isinstance(release.get("id"), bool) or not isinstance(release.get("id"), int) or not isinstance(release.get("assets"), list):
        raise ContractError("GitHub Release API response has malformed id/assets")
    if require_immutable and release.get("immutable") is not True:
        raise ContractError("GitHub Release must be immutable")
    if not isinstance(tag_ref, dict) or tag_ref.get("ref") != f"refs/tags/{expected_tag}":
        raise ContractError("tag-ref API response does not identify the expected tag")
    target = tag_ref.get("object")
    if not isinstance(tag_objects, list):
        raise ContractError("annotated tag objects must be an array")
    consumed = 0
    seen: set[str] = set()
    while isinstance(target, dict) and target.get("type") == "tag":
        sha = target.get("sha")
        if not isinstance(sha, str) or SHA_RE.fullmatch(sha) is None or sha in seen or consumed >= 5:
            raise ContractError("annotated tag chain is malformed, cyclic, or too deep")
        seen.add(sha)
        if consumed >= len(tag_objects):
            raise ContractError("annotated tag chain is incomplete")
        item = tag_objects[consumed]
        consumed += 1
        if not isinstance(item, dict) or item.get("sha") != sha or not isinstance(item.get("object"), dict):
            raise ContractError("annotated tag API response does not match the requested object")
        target = item["object"]
    if consumed != len(tag_objects):
        raise ContractError("annotated tag API responses contain unused ambiguous objects")
    if not isinstance(target, dict) or target.get("type") != "commit" or target.get("sha") != expected_sha:
        raise ContractError("GitHub Release tag does not resolve to the exact dispatched SHA")


def validate_builds_identity(workflow_text: str, selected_gitlink: str, approved: str) -> None:
    if SHA_RE.fullmatch(approved) is None or selected_gitlink != approved:
        raise ContractError("selected Builds gitlink must equal the approved exact execution SHA")
    workflow_pins = re.findall(
        r"uses:\s*Hexalith/Hexalith\.Builds/\.github/workflows/domain-release\.yml@([0-9a-f]{40})\b",
        workflow_text,
    )
    input_pins = re.findall(r"(?m)^\s+builds-execution-sha:\s*([0-9a-f]{40})\s*$", workflow_text)
    if workflow_pins != [approved] or input_pins != [approved]:
        raise ContractError("release workflow and builds-execution-sha must use the identical approved Builds commit")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    manifest = sub.add_parser("manifest")
    manifest.add_argument("--root", default=".")
    manifest.add_argument("--manifest", required=True)
    manifest.add_argument("--expected-count", required=True, type=int)
    source = sub.add_parser("select-ci")
    source.add_argument("--dispatch-sha", required=True)
    source.add_argument("--main-ref", required=True)
    source.add_argument("--runs", required=True)
    source.add_argument("--output", required=True)
    publication = sub.add_parser("publication")
    publication.add_argument("--expected-sha", required=True)
    publication.add_argument("--expected-tag", required=True)
    publication.add_argument("--release", required=True)
    publication.add_argument("--tag-ref", required=True)
    publication.add_argument("--tag-objects", required=True)
    publication.add_argument("--require-immutable", action="store_true")
    builds = sub.add_parser("builds")
    builds.add_argument("--root", default=".")
    builds.add_argument("--commit", required=True)
    builds.add_argument("--approved", required=True)
    args = parser.parse_args(argv)
    try:
        if args.command == "manifest":
            validate_package_manifest(pathlib.Path(args.root).resolve(), pathlib.Path(args.manifest), args.expected_count)
            print(json.dumps({"ok": True, "package_count": args.expected_count}, sort_keys=True))
        elif args.command == "select-ci":
            selected = select_exact_ci_run(
                args.dispatch_sha,
                _strict_load(pathlib.Path(args.main_ref)),
                _strict_load(pathlib.Path(args.runs)),
            )
            pathlib.Path(args.output).write_text(json.dumps(selected, sort_keys=True) + "\n", encoding="utf-8")
            print(json.dumps({"ok": True, **selected}, sort_keys=True))
        elif args.command == "publication":
            validate_publication(
                args.expected_sha,
                args.expected_tag,
                _strict_load(pathlib.Path(args.release)),
                _strict_load(pathlib.Path(args.tag_ref)),
                _strict_load(pathlib.Path(args.tag_objects)),
                require_immutable=args.require_immutable,
            )
            print(json.dumps({"ok": True, "tag": args.expected_tag, "source_sha": args.expected_sha}, sort_keys=True))
        else:
            root = pathlib.Path(args.root).resolve()
            result = subprocess.run(
                ["git", "-C", str(root), "ls-tree", args.commit, "references/Hexalith.Builds"],
                capture_output=True,
                text=True,
                check=False,
            )
            fields = result.stdout.strip().split()
            if result.returncode != 0 or len(fields) < 3 or fields[0] != "160000":
                raise ContractError("cannot resolve the candidate Builds gitlink")
            workflow_bytes = subprocess.run(
                ["git", "-C", str(root), "show", f"{args.commit}:.github/workflows/release.yml"],
                capture_output=True,
                check=False,
            )
            if workflow_bytes.returncode != 0:
                raise ContractError("cannot resolve the exact candidate release workflow")
            validate_builds_identity(workflow_bytes.stdout.decode("utf-8-sig"), fields[2], args.approved)
            print(json.dumps({"ok": True, "builds_execution_sha": args.approved}, sort_keys=True))
    except ContractError as exc:
        print(f"release contract rejected: {exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
