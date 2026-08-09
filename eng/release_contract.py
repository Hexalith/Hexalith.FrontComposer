#!/usr/bin/env python3
"""Fail-closed contracts for FrontComposer's operator-controlled release entry point."""

from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import re
import subprocess
import sys
import zipfile
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
NUGET_SIGNATURE_MEMBER = ".signature.p7s"
MAX_PACKAGE_ENTRIES = 65_536
MAX_PACKAGE_MEMBER_BYTES = 512 * 1024 * 1024
MAX_PACKAGE_CONTENT_BYTES = 2 * 1024 * 1024 * 1024
MAX_SIGNATURE_BYTES = 16 * 1024 * 1024
_VERIFIED_PACKAGE_RE = re.compile(
    r"Successfully verified package '(.+?)\.(\d+(?:\.[0-9A-Za-z.+\-]+)*)'\."
)
_REPOSITORY_SIGNATURE_RE = re.compile(r"^\s*Signature type:\s*Repository\s*$", re.IGNORECASE | re.MULTILINE)
_NUGET_ORG_SERVICE_INDEX_RE = re.compile(
    r"^\s*Service index:\s*https://api\.nuget\.org/v3/index\.json\s*$",
    re.IGNORECASE | re.MULTILINE,
)


class ContractError(ValueError):
    """Raised when a release boundary input is missing or ambiguous."""


def _sha256_stream(source: Any) -> str:
    digest = hashlib.sha256()
    for chunk in iter(lambda: source.read(1024 * 1024), b""):
        digest.update(chunk)
    return digest.hexdigest()


def _sha256_file(path: pathlib.Path) -> str:
    try:
        with path.open("rb") as source:
            return _sha256_stream(source)
    except OSError as exc:
        raise ContractError(f"{path}: cannot read package bytes") from exc


def _package_content_projection(path: pathlib.Path, *, require_repository_signature: bool) -> list[dict[str, Any]]:
    """Return the normalized package payload, excluding only NuGet's root signature entry."""
    try:
        archive = zipfile.ZipFile(path)
    except (OSError, zipfile.BadZipFile) as exc:
        raise ContractError(f"{path}: malformed NuGet ZIP archive") from exc
    with archive:
        entries = archive.infolist()
        if not entries or len(entries) > MAX_PACKAGE_ENTRIES:
            raise ContractError(f"{path}: NuGet archive entry count is empty or exceeds {MAX_PACKAGE_ENTRIES}")
        seen: set[str] = set()
        signature_count = 0
        total_size = 0
        projection: list[dict[str, Any]] = []
        for entry in entries:
            name = entry.filename
            pure = pathlib.PurePosixPath(name)
            windows = pathlib.PureWindowsPath(name)
            folded = name.casefold()
            if (
                not name
                or "\\" in name
                or pure.is_absolute()
                or bool(windows.drive)
                or any(ord(character) < 32 or ord(character) == 127 for character in name)
                or ".." in pure.parts
                or pure.as_posix() != name
                or folded in seen
            ):
                raise ContractError(f"{path}: unsafe, non-normalized, or duplicate ZIP member {name!r}")
            seen.add(folded)
            if folded == NUGET_SIGNATURE_MEMBER.casefold() and name != NUGET_SIGNATURE_MEMBER:
                raise ContractError(f"{path}: NuGet signature entry must use the exact root name {NUGET_SIGNATURE_MEMBER}")
            if name == NUGET_SIGNATURE_MEMBER:
                if entry.is_dir() or entry.flag_bits & 0x1 or not 0 < entry.file_size <= MAX_SIGNATURE_BYTES:
                    raise ContractError(f"{path}: NuGet repository signature entry is malformed")
                signature_count += 1
                continue
            if entry.flag_bits & 0x1:
                raise ContractError(f"{path}: encrypted ZIP member is not permitted: {name}")
            if entry.file_size > MAX_PACKAGE_MEMBER_BYTES:
                raise ContractError(f"{path}: ZIP member exceeds the size limit: {name}")
            total_size += entry.file_size
            if total_size > MAX_PACKAGE_CONTENT_BYTES:
                raise ContractError(f"{path}: expanded package content exceeds the size limit")
            try:
                with archive.open(entry, "r") as source:
                    member_hash = _sha256_stream(source)
            except (OSError, RuntimeError, zipfile.BadZipFile) as exc:
                raise ContractError(f"{path}: cannot read ZIP member {name!r}") from exc
            projection.append({"path": name, "size": entry.file_size, "sha256": member_hash})
        if signature_count > 1:
            raise ContractError(f"{path}: NuGet archive contains duplicate repository signature entries")
        if require_repository_signature and signature_count != 1:
            raise ContractError(f"{path}: NuGet.org download lacks its root {NUGET_SIGNATURE_MEMBER} entry")
        if not require_repository_signature and signature_count != 0:
            raise ContractError(f"{path}: prepared candidate must not contain an author or repository signature")
        return sorted(projection, key=lambda row: row["path"])


def validate_unsigned_candidate(path: pathlib.Path) -> dict[str, Any]:
    """Require a readable, safe NuGet archive with no author/repository signature."""
    projection = _package_content_projection(path, require_repository_signature=False)
    material = json.dumps(projection, ensure_ascii=True, sort_keys=True, separators=(",", ":"))
    return {
        "package_sha256": _sha256_file(path),
        "content_sha256": hashlib.sha256(material.encode("utf-8")).hexdigest(),
        "member_count": len(projection),
    }


def compare_nuget_package_content(candidate: pathlib.Path, published: pathlib.Path) -> dict[str, Any]:
    """Require NuGet.org to add only its repository signature, without payload drift."""
    candidate_projection = _package_content_projection(candidate, require_repository_signature=False)
    published_projection = _package_content_projection(published, require_repository_signature=True)
    if candidate_projection != published_projection:
        raise ContractError(
            "NuGet.org package content differs from the sealed candidate beyond the repository signature"
        )
    material = json.dumps(candidate_projection, ensure_ascii=True, sort_keys=True, separators=(",", ":"))
    return {
        "candidate_sha256": _sha256_file(candidate),
        "published_sha256": _sha256_file(published),
        "content_sha256": hashlib.sha256(material.encode("utf-8")).hexdigest(),
        "member_count": len(candidate_projection),
        "repository_signature_member": NUGET_SIGNATURE_MEMBER,
    }


def validate_repository_signature_transcript(text: str, package_ids: list[str], version: str) -> dict[str, Any]:
    """Require one successful NuGet repository-signature verification per expected package."""
    folded_package_ids = [package_id.casefold() for package_id in package_ids]
    if not package_ids or len(set(folded_package_ids)) != len(package_ids):
        raise ContractError("expected repository-signature package IDs must be non-empty and case-insensitively unique")
    if re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+(?:[+-][0-9A-Za-z.-]+)?", version) is None:
        raise ContractError("repository-signature version is malformed")
    expected = {package_id.lower(): package_id for package_id in package_ids}
    observed: dict[str, str] = {}
    previous_end = 0
    for match in _VERIFIED_PACKAGE_RE.finditer(text):
        package_id = match.group(1)
        observed_version = match.group(2)
        block = text[previous_end:match.end()]
        previous_end = match.end()
        key = package_id.lower()
        if key not in expected or observed_version != version:
            raise ContractError(f"signature transcript contains an unexpected package coordinate: {package_id}.{observed_version}")
        if key in observed:
            raise ContractError(f"signature transcript contains duplicate verification for {package_id}")
        if _REPOSITORY_SIGNATURE_RE.search(block) is None:
            raise ContractError(f"{package_id}: successful verification is not a repository signature")
        if _NUGET_ORG_SERVICE_INDEX_RE.search(block) is None:
            raise ContractError(f"{package_id}: repository signature does not identify the NuGet.org service index")
        observed[key] = package_id
    missing = sorted(expected[key] for key in expected.keys() - observed.keys())
    if missing:
        raise ContractError(f"repository-signature verification is missing package(s): {', '.join(missing)}")
    return {"package_count": len(observed), "packages": [expected[key] for key in expected]}


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
    if require_immutable and len(release.get("assets") or []) < 1:
        raise ContractError("GitHub Release must include at least one durable asset")
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
    content = sub.add_parser("package-content")
    content.add_argument("--candidate", required=True)
    content.add_argument("--published", required=True)
    candidate = sub.add_parser("package-candidate")
    candidate.add_argument("--package", required=True)
    repository = sub.add_parser("repository-signatures")
    repository.add_argument("--transcript", required=True)
    repository.add_argument("--package-id", action="append", required=True)
    repository.add_argument("--version", required=True)
    repository.add_argument("--output")
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
        elif args.command == "builds":
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
        elif args.command == "package-content":
            comparison = compare_nuget_package_content(
                pathlib.Path(args.candidate),
                pathlib.Path(args.published),
            )
            print(json.dumps({"ok": True, **comparison}, sort_keys=True))
        elif args.command == "package-candidate":
            candidate = validate_unsigned_candidate(pathlib.Path(args.package))
            print(json.dumps({"ok": True, **candidate}, sort_keys=True))
        else:
            transcript_path = pathlib.Path(args.transcript)
            try:
                transcript = transcript_path.read_text(encoding="utf-8")
            except (OSError, UnicodeError) as exc:
                raise ContractError(f"{transcript_path}: cannot read repository-signature transcript") from exc
            verified = validate_repository_signature_transcript(transcript, args.package_id, args.version)
            payload = {"ok": True, "version": args.version, **verified}
            if args.output:
                pathlib.Path(args.output).write_text(
                    json.dumps(payload, indent=2, sort_keys=True) + "\n",
                    encoding="utf-8",
                )
            print(json.dumps(payload, sort_keys=True))
    except ContractError as exc:
        print(f"release contract rejected: {exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
