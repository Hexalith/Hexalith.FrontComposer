#!/usr/bin/env python3
"""Exact-blob workflow and composite-action source closure for GOV-1.

The collector deliberately implements a closed YAML subset instead of importing a YAML
package.  It follows every literal ``uses:`` mapping entry, including entries guarded by
conditions, from exact Git blobs.  Reusable workflows and composite actions recurse;
JavaScript actions terminate at their exact metadata blob.  Unsupported or ambiguous YAML,
mutable/dynamic references, Docker actions, cycles, and AD-13 resource-limit violations fail
closed.
"""

from __future__ import annotations

import hashlib
import json
import re
import subprocess
from pathlib import Path
from typing import Any, Mapping


DEFAULT_LIMITS = {
    "max_workflow_closure_depth": 16,
    "max_workflow_closure_sources": 256,
    "max_workflow_source_blob_bytes": 1_048_576,
    "max_workflow_source_total_bytes": 16_777_216,
}

_LIMIT_KEYS = frozenset(DEFAULT_LIMITS)
_POLICY_STAGES = ("ci", "release", "post_release")
_BUILDS_IDENTITY = "github.com/hexalith/hexalith.builds"
_BUILDS_LOCAL_EXECUTION_PREFIX = ".hexalith/builds-execution/"
_COMMIT_RE = re.compile(r"^[0-9a-f]{40}$")
_SHA256_RE = re.compile(r"^[0-9a-f]{64}$")
_IDENTITY_RE = re.compile(
    r"^github\.com/(?P<owner>[a-z0-9._-]+)/(?P<repository>[a-z0-9._-]+)$"
)
_EXTERNAL_USES_RE = re.compile(
    r"^(?P<owner>[A-Za-z0-9._-]+)/(?P<repository>[A-Za-z0-9._-]+)"
    r"(?P<path>/[^@\s]+)?@(?P<commit>[0-9a-f]{40})$"
)
_BUILDS_EXECUTION_CHECKOUT_RE = re.compile(
    r"(?ms)repository:\s*Hexalith/Hexalith\.Builds\s*\n"
    r"(?:[^\n]*\n){0,6}?"
    r"\s*ref:\s*(?P<commit>[0-9a-f]{40})\s*\n"
    r"(?:[^\n]*\n){0,6}?"
    r"\s*path:\s*\.hexalith/builds-execution\b"
)
_MAPPING_RE = re.compile(
    r"^(?P<indent> *)(?:(?P<dash>-) +)?(?P<key>[A-Za-z_][A-Za-z0-9_-]*) *:"
    r"(?P<value>.*)$"
)
_QUOTED_USES_KEY_RE = re.compile(r"^ *(?:- +)?['\"]uses['\"] *:")
_BLOCK_SCALAR_RE = re.compile(r"^[>|](?:[+-]?[1-9]?|[1-9][+-])$")
_ANCHOR_RE = re.compile(r"(?:^|[\s:\-\[,])&[A-Za-z0-9_-]+(?:$|[\s,\]}])")
_ALIAS_RE = re.compile(r"(?:^|[\s:\-\[,])\*[A-Za-z0-9_-]+(?:$|[\s,\]}])")


class WorkflowClosureError(Exception):
    """Raised whenever the static source closure cannot be proven exactly."""


def _canonical_bytes(value: Any) -> bytes:
    return json.dumps(
        value,
        ensure_ascii=True,
        allow_nan=False,
        sort_keys=True,
        separators=(",", ":"),
    ).encode("utf-8")


def _canonical_digest(value: Any) -> str:
    return hashlib.sha256(_canonical_bytes(value)).hexdigest()


def _require_closed_object(
    value: Any,
    expected_keys: set[str] | frozenset[str],
    context: str,
) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise WorkflowClosureError(f"{context}: expected an object")
    actual = set(value)
    if actual != set(expected_keys):
        missing = sorted(set(expected_keys) - actual)
        unknown = sorted(actual - set(expected_keys))
        raise WorkflowClosureError(
            f"{context}: closed member set mismatch (missing={missing}, unknown={unknown})"
        )
    return value


def _require_identity(value: Any, context: str) -> str:
    if not isinstance(value, str) or not value.isascii():
        raise WorkflowClosureError(f"{context}: repository identity must be an ASCII string")
    match = _IDENTITY_RE.fullmatch(value)
    if match is None:
        raise WorkflowClosureError(
            f"{context}: expected canonical lowercase github.com/owner/repository identity, "
            f"got {value!r}"
        )
    if match.group("owner") in (".", "..") or match.group("repository") in (".", ".."):
        raise WorkflowClosureError(f"{context}: unsafe repository identity {value!r}")
    return value


def _require_commit(value: Any, context: str) -> str:
    if not isinstance(value, str) or _COMMIT_RE.fullmatch(value) is None:
        raise WorkflowClosureError(
            f"{context}: expected a strict lowercase 40-hex commit, got {value!r}"
        )
    return value


def _require_sha256(value: Any, context: str) -> str:
    if not isinstance(value, str) or _SHA256_RE.fullmatch(value) is None:
        raise WorkflowClosureError(
            f"{context}: expected a strict lowercase 64-hex SHA-256, got {value!r}"
        )
    return value


def _normalize_path(value: Any, context: str, *, allow_empty: bool = False) -> str:
    if not isinstance(value, str) or not value.isascii():
        raise WorkflowClosureError(f"{context}: path must be an ASCII string")
    if value == "" and allow_empty:
        return value
    if not value or value.startswith("/") or "\\" in value:
        raise WorkflowClosureError(f"{context}: unsafe relative POSIX path {value!r}")
    if any(ord(character) < 0x20 or ord(character) == 0x7F for character in value):
        raise WorkflowClosureError(f"{context}: control character in path {value!r}")
    if any(segment in ("", ".", "..") for segment in value.split("/")):
        raise WorkflowClosureError(f"{context}: unsafe path segment in {value!r}")
    return value


def _require_workflow_path(value: Any, context: str) -> str:
    path = _normalize_path(value, context)
    if not path.startswith(".github/workflows/") or not path.endswith((".yml", ".yaml")):
        raise WorkflowClosureError(
            f"{context}: reusable workflow must be a .github/workflows/*.yml or *.yaml blob, "
            f"got {path!r}"
        )
    return path


def _validate_limits(limits: Mapping[str, Any] | None) -> dict[str, int]:
    effective: Mapping[str, Any] = DEFAULT_LIMITS if limits is None else limits
    if set(effective) != _LIMIT_KEYS:
        raise WorkflowClosureError(
            "closure limits: closed member set mismatch "
            f"(missing={sorted(_LIMIT_KEYS - set(effective))}, "
            f"unknown={sorted(set(effective) - _LIMIT_KEYS)})"
        )
    validated: dict[str, int] = {}
    for key, adopted_ceiling in DEFAULT_LIMITS.items():
        value = effective[key]
        if isinstance(value, bool) or not isinstance(value, int) or value < 1:
            raise WorkflowClosureError(f"closure limits: {key} must be a positive integer")
        if value > adopted_ceiling:
            raise WorkflowClosureError(
                f"closure limits: {key}={value} exceeds the adopted AD-13 ceiling "
                f"{adopted_ceiling}"
            )
        validated[key] = value
    return validated


def closure_limits_from_policy(policy: Mapping[str, Any]) -> dict[str, int]:
    """Project the four AD-13 closure ceilings from the active policy.

    Governed callers use this hook so the active policy remains the executable source of
    limit values.  Direct tests may pass a stricter closed limit object to the collector.
    """

    resource_limits = policy.get("resource_limits")
    if not isinstance(resource_limits, dict):
        raise WorkflowClosureError("policy.resource_limits: expected an object")
    missing = sorted(_LIMIT_KEYS - set(resource_limits))
    if missing:
        raise WorkflowClosureError(
            f"policy.resource_limits: missing workflow-closure limits {missing}"
        )
    return _validate_limits({key: resource_limits[key] for key in _LIMIT_KEYS})


def _validate_coordinate(value: Any, context: str) -> dict[str, str]:
    coordinate = _require_closed_object(
        value,
        {"repository", "workflow_path", "commit"},
        context,
    )
    return {
        "repository": _require_identity(coordinate["repository"], f"{context}.repository"),
        "workflow_path": _require_workflow_path(
            coordinate["workflow_path"],
            f"{context}.workflow_path",
        ),
        "commit": _require_commit(coordinate["commit"], f"{context}.commit"),
    }


def _run_git(args: list[str], store: Path, context: str) -> bytes:
    process = subprocess.run(
        ["git", *args],
        cwd=str(store),
        capture_output=True,
        check=False,
    )
    if process.returncode != 0:
        diagnostic = process.stderr.decode("utf-8", "replace").strip()
        raise WorkflowClosureError(
            f"{context}: git {' '.join(args)} failed in {store}: {diagnostic}"
        )
    return process.stdout


class _Collector:
    def __init__(
        self,
        repository_stores: Mapping[str, Path | str],
        limits: Mapping[str, Any] | None,
    ) -> None:
        self.limits = _validate_limits(limits)
        self.stores: dict[str, Path] = {}
        for identity, raw_path in repository_stores.items():
            canonical = _require_identity(identity, "repository_stores key")
            if canonical in self.stores:
                raise WorkflowClosureError(
                    f"repository_stores: duplicate repository identity {canonical!r}"
                )
            path = Path(raw_path).resolve()
            if not path.exists():
                raise WorkflowClosureError(
                    f"repository_stores[{canonical!r}]: object store does not exist: {path}"
                )
            self.stores[canonical] = path

        self._validated_stores: set[str] = set()
        self._validated_commits: set[tuple[str, str]] = set()
        self._records: dict[tuple[str, str, str], dict[str, str]] = {}
        self._active: list[tuple[str, str, str]] = []
        self._visited: set[tuple[str, str, str]] = set()
        self._total_bytes = 0

    def _store(self, repository: str) -> Path:
        store = self.stores.get(repository)
        if store is None:
            raise WorkflowClosureError(
                f"{repository}: repository is not present in the trusted exact-object store map"
            )
        if repository not in self._validated_stores:
            object_format = _run_git(
                ["rev-parse", "--show-object-format"],
                store,
                repository,
            ).decode("ascii", "strict").strip()
            if object_format != "sha1":
                raise WorkflowClosureError(
                    f"{repository}: AD-13 supports Git SHA-1 object stores only, found "
                    f"{object_format!r}"
                )
            self._validated_stores.add(repository)
        return store

    def _ensure_commit(self, repository: str, commit: str) -> Path:
        key = (repository, commit)
        store = self._store(repository)
        if key not in self._validated_commits:
            object_type = _run_git(
                ["cat-file", "-t", commit],
                store,
                f"{repository}@{commit}",
            ).decode("ascii", "strict").strip()
            if object_type != "commit":
                raise WorkflowClosureError(
                    f"{repository}@{commit}: expected a commit object, found {object_type!r}"
                )
            self._validated_commits.add(key)
        return store

    def _blob_entry(
        self,
        repository: str,
        commit: str,
        path: str,
    ) -> tuple[str, int] | None:
        store = self._ensure_commit(repository, commit)
        output = _run_git(
            ["ls-tree", "-z", commit, "--", path],
            store,
            f"{repository}@{commit}:{path}",
        )
        records = [record for record in output.split(b"\x00") if record]
        if not records:
            return None
        if len(records) != 1:
            raise WorkflowClosureError(
                f"{repository}@{commit}:{path}: ambiguous tree lookup returned {len(records)} entries"
            )
        metadata, separator, raw_path = records[0].partition(b"\t")
        if not separator or raw_path.decode("utf-8", "strict") != path:
            raise WorkflowClosureError(
                f"{repository}@{commit}:{path}: exact tree path did not round-trip"
            )
        mode, object_type, object_id = metadata.decode("ascii", "strict").split()
        if mode not in ("100644", "100755") or object_type != "blob":
            raise WorkflowClosureError(
                f"{repository}@{commit}:{path}: source must be a regular blob, found "
                f"mode={mode} type={object_type}"
            )
        size_text = _run_git(
            ["cat-file", "-s", object_id],
            store,
            f"{repository}@{commit}:{path}",
        ).decode("ascii", "strict").strip()
        try:
            size = int(size_text)
        except ValueError as error:
            raise WorkflowClosureError(
                f"{repository}@{commit}:{path}: invalid Git blob size {size_text!r}"
            ) from error
        return object_id, size

    def _read_source(self, repository: str, commit: str, path: str) -> bytes:
        key = (repository, commit, path)
        existing = self._records.get(key)
        if existing is not None:
            return b""
        entry = self._blob_entry(repository, commit, path)
        if entry is None:
            raise WorkflowClosureError(f"{repository}@{commit}:{path}: source blob is missing")
        object_id, size = entry
        per_blob = self.limits["max_workflow_source_blob_bytes"]
        if size > per_blob:
            raise WorkflowClosureError(
                f"{repository}@{commit}:{path}: {size} bytes exceeds the {per_blob}-byte "
                "workflow/action blob ceiling"
            )
        total_limit = self.limits["max_workflow_source_total_bytes"]
        if self._total_bytes + size > total_limit:
            raise WorkflowClosureError(
                f"workflow/action metadata would total {self._total_bytes + size} bytes, "
                f"exceeding the {total_limit}-byte closure ceiling"
            )
        source_limit = self.limits["max_workflow_closure_sources"]
        if len(self._records) >= source_limit:
            raise WorkflowClosureError(
                f"workflow/action closure exceeds the {source_limit}-source ceiling"
            )
        store = self._store(repository)
        blob = _run_git(
            ["cat-file", "blob", object_id],
            store,
            f"{repository}@{commit}:{path}",
        )
        if len(blob) != size:
            raise WorkflowClosureError(
                f"{repository}@{commit}:{path}: Git reported {size} bytes but returned "
                f"{len(blob)}"
            )
        self._total_bytes += size
        self._records[key] = {
            "repository": repository,
            "path": path,
            "commit": commit,
            "blob_sha256": hashlib.sha256(blob).hexdigest(),
        }
        return blob

    def _enter(self, key: tuple[str, str, str], depth: int) -> bool:
        depth_limit = self.limits["max_workflow_closure_depth"]
        if depth > depth_limit:
            raise WorkflowClosureError(
                f"workflow/action closure depth {depth} exceeds the {depth_limit}-level ceiling "
                f"at {key[0]}@{key[1]}:{key[2]}"
            )
        if key in self._active:
            cycle_start = self._active.index(key)
            cycle = self._active[cycle_start:] + [key]
            rendered = " -> ".join(f"{repo}@{commit}:{path}" for repo, commit, path in cycle)
            raise WorkflowClosureError(f"workflow/composite source cycle: {rendered}")
        if key in self._visited:
            return False
        self._active.append(key)
        return True

    def _leave(self, key: tuple[str, str, str]) -> None:
        popped = self._active.pop()
        if popped != key:
            raise WorkflowClosureError("internal closure stack corruption")
        self._visited.add(key)

    def _resolve_action_metadata(
        self,
        repository: str,
        commit: str,
        directory: str,
    ) -> str:
        candidates = []
        for name in ("action.yml", "action.yaml"):
            path = f"{directory}/{name}" if directory else name
            if self._blob_entry(repository, commit, path) is not None:
                candidates.append(path)
        if not candidates:
            display = directory or "."
            raise WorkflowClosureError(
                f"{repository}@{commit}:{display}: missing action.yml/action.yaml metadata"
            )
        if len(candidates) != 1:
            raise WorkflowClosureError(
                f"{repository}@{commit}:{directory or '.'}: ambiguous action metadata "
                f"{candidates}"
            )
        return candidates[0]

    def _builds_execution_commit_for_caller(
        self,
        repository: str,
        commit: str,
        context: str,
    ) -> str:
        """Resolve the literal Builds checkout behind a caller builds-execution path."""

        workflow_keys = [
            key
            for key in reversed(self._active)
            if key[0] == repository and key[1] == commit and _is_workflow_path(key[2])
        ]
        if not workflow_keys:
            raise WorkflowClosureError(
                f"{context}: builds-execution local uses requires an enclosing workflow source"
            )
        workflow_repository, workflow_commit, workflow_path = workflow_keys[0]
        # `_read_source` returns empty bytes on a cache hit (it only records hashes), so
        # re-load the exact enclosing workflow blob from the object store for this parse.
        entry = self._blob_entry(workflow_repository, workflow_commit, workflow_path)
        if entry is None:
            raise WorkflowClosureError(
                f"{context}: enclosing workflow blob is missing at "
                f"{workflow_repository}@{workflow_commit}:{workflow_path}"
            )
        object_id, _size = entry
        blob = _run_git(
            ["cat-file", "blob", object_id],
            self._store(workflow_repository),
            f"{workflow_repository}@{workflow_commit}:{workflow_path}",
        )
        try:
            text = blob.decode("utf-8-sig", "strict")
        except UnicodeDecodeError as error:
            raise WorkflowClosureError(
                f"{context}: enclosing workflow is not valid UTF-8 ({error})"
            ) from error
        matches = list(_BUILDS_EXECUTION_CHECKOUT_RE.finditer(text))
        commits = sorted({match.group("commit") for match in matches})
        if len(commits) != 1:
            raise WorkflowClosureError(
                f"{context}: expected exactly one literal Hexalith.Builds checkout into "
                f".hexalith/builds-execution with a 40-hex ref, found {len(commits)}"
            )
        return _require_commit(commits[0], f"{context} builds-execution ref")

    def _resolve_uses(
        self,
        literal: str,
        repository: str,
        commit: str,
        context: str,
    ) -> tuple[str, str, str, str]:
        if "${{" in literal or "}}" in literal:
            raise WorkflowClosureError(f"{context}: dynamic uses expression is forbidden: {literal!r}")
        if literal.lower().startswith("docker://"):
            raise WorkflowClosureError(f"{context}: Docker actions are forbidden: {literal!r}")
        if literal.startswith("./"):
            if "@" in literal:
                raise WorkflowClosureError(
                    f"{context}: local uses reference must not contain a ref: {literal!r}"
                )
            target_path = _normalize_path(literal[2:], f"{context} local uses path")
            if target_path.startswith(_BUILDS_LOCAL_EXECUTION_PREFIX):
                # Builds governed composites are checked out under this runtime prefix and must
                # hash as exact blobs from the Builds commit that supplied that checkout.
                relative = _normalize_path(
                    target_path[len(_BUILDS_LOCAL_EXECUTION_PREFIX) :],
                    f"{context} builds-execution path",
                )
                if repository == _BUILDS_IDENTITY:
                    target_repository = repository
                    target_commit = commit
                else:
                    target_repository = _BUILDS_IDENTITY
                    target_commit = self._builds_execution_commit_for_caller(
                        repository,
                        commit,
                        context,
                    )
                if _is_workflow_path(relative):
                    return target_repository, target_commit, relative, "workflow"
                return target_repository, target_commit, relative, "action"
            if _is_workflow_path(target_path):
                return repository, commit, target_path, "workflow"
            return repository, commit, target_path, "action"

        match = _EXTERNAL_USES_RE.fullmatch(literal)
        if match is None:
            raise WorkflowClosureError(
                f"{context}: external uses reference must contain a literal lowercase 40-hex "
                f"commit, got {literal!r}"
            )
        owner = match.group("owner").lower()
        repository_name = match.group("repository").lower()
        target_repository = _require_identity(
            f"github.com/{owner}/{repository_name}",
            f"{context} external repository",
        )
        target_commit = _require_commit(match.group("commit"), f"{context} external commit")
        raw_path = match.group("path")
        target_path = _normalize_path(
            raw_path[1:] if raw_path else "",
            f"{context} external path",
            allow_empty=True,
        )
        if _is_workflow_path(target_path):
            return target_repository, target_commit, target_path, "workflow"
        return target_repository, target_commit, target_path, "action"

    def _visit_literal(
        self,
        literal: str,
        repository: str,
        commit: str,
        depth: int,
        context: str,
    ) -> None:
        target_repository, target_commit, target_path, kind = self._resolve_uses(
            literal,
            repository,
            commit,
            context,
        )
        if kind == "workflow":
            self.visit_workflow(target_repository, target_commit, target_path, depth)
            return
        metadata_path = self._resolve_action_metadata(
            target_repository,
            target_commit,
            target_path,
        )
        self.visit_action(target_repository, target_commit, metadata_path, depth)

    def visit_workflow(self, repository: str, commit: str, path: str, depth: int) -> None:
        key = (repository, commit, path)
        if not self._enter(key, depth):
            return
        try:
            blob = self._read_source(repository, commit, path)
            entries = _scan_yaml(blob, f"{repository}@{commit}:{path}")
            for value, line_number in _uses_entries(entries):
                self._visit_literal(
                    value,
                    repository,
                    commit,
                    depth + 1,
                    f"{repository}@{commit}:{path}:{line_number}",
                )
        finally:
            if self._active and self._active[-1] == key:
                self._leave(key)

    def visit_action(self, repository: str, commit: str, path: str, depth: int) -> None:
        key = (repository, commit, path)
        if not self._enter(key, depth):
            return
        try:
            blob = self._read_source(repository, commit, path)
            context = f"{repository}@{commit}:{path}"
            entries = _scan_yaml(blob, context)
            using = _action_using(entries, context)
            uses = _uses_entries(entries)
            if using == "docker":
                raise WorkflowClosureError(f"{context}: Docker actions are forbidden")
            if using == "composite":
                for value, line_number in uses:
                    self._visit_literal(
                        value,
                        repository,
                        commit,
                        depth + 1,
                        f"{context}:{line_number}",
                    )
            elif re.fullmatch(r"node[0-9]+", using) is not None:
                if uses:
                    raise WorkflowClosureError(
                        f"{context}: JavaScript action metadata contains unexpected uses entries"
                    )
            else:
                raise WorkflowClosureError(
                    f"{context}: unsupported action runs.using value {using!r}"
                )
        finally:
            if self._active and self._active[-1] == key:
                self._leave(key)


def _is_workflow_path(path: str) -> bool:
    return path.startswith(".github/workflows/") and path.endswith((".yml", ".yaml"))


def _strip_yaml_comment(value: str) -> str:
    single = False
    double = False
    index = 0
    while index < len(value):
        character = value[index]
        if single:
            if character == "'":
                if index + 1 < len(value) and value[index + 1] == "'":
                    index += 2
                    continue
                single = False
        elif double:
            if character == "\\":
                index += 2
                continue
            if character == '"':
                double = False
        else:
            if character == "'":
                # Plain-scalar apostrophes inside words (it's / there's) are not YAML quotes.
                previous = value[index - 1] if index > 0 else ""
                nxt = value[index + 1] if index + 1 < len(value) else ""
                if previous.isalnum() and nxt.isalnum():
                    index += 1
                    continue
                single = True
            elif character == '"':
                double = True
            elif character == "#" and (index == 0 or value[index - 1].isspace()):
                return value[:index].rstrip()
        index += 1
    if single or double:
        raise WorkflowClosureError("unterminated quoted YAML scalar")
    return value.rstrip()


def _unquoted_token(value: str, token: str) -> bool:
    single = False
    double = False
    index = 0
    while index <= len(value) - len(token):
        character = value[index]
        if single:
            if character == "'":
                if index + 1 < len(value) and value[index + 1] == "'":
                    index += 2
                    continue
                single = False
        elif double:
            if character == "\\":
                index += 2
                continue
            if character == '"':
                double = False
        else:
            if character == "'":
                single = True
            elif character == '"':
                double = True
            elif value.startswith(token, index):
                return True
        index += 1
    return False


def _parse_yaml_scalar(value: str, context: str) -> str:
    scalar = _strip_yaml_comment(value).strip()
    if not scalar:
        raise WorkflowClosureError(f"{context}: uses must have a scalar value on the same line")
    if _BLOCK_SCALAR_RE.fullmatch(scalar):
        raise WorkflowClosureError(f"{context}: multiline uses values are unsupported")
    if scalar.startswith("'"):
        if len(scalar) < 2 or not scalar.endswith("'"):
            raise WorkflowClosureError(f"{context}: malformed single-quoted uses scalar")
        return scalar[1:-1].replace("''", "'")
    if scalar.startswith('"'):
        try:
            parsed = json.loads(scalar)
        except json.JSONDecodeError as error:
            raise WorkflowClosureError(
                f"{context}: unsupported double-quoted uses scalar ({error.msg})"
            ) from error
        if not isinstance(parsed, str):
            raise WorkflowClosureError(f"{context}: uses value must be a string")
        return parsed
    if scalar[0] in "[{&*!|>" or any(character.isspace() for character in scalar):
        raise WorkflowClosureError(f"{context}: unsupported plain uses scalar {scalar!r}")
    return scalar


def _scan_yaml(blob: bytes, context: str) -> list[tuple[tuple[str, ...], str, int]]:
    try:
        text = blob.decode("utf-8-sig", "strict")
    except UnicodeDecodeError as error:
        raise WorkflowClosureError(f"{context}: source is not valid UTF-8 ({error})") from error
    if "\x00" in text:
        raise WorkflowClosureError(f"{context}: source contains a NUL character")

    entries: list[tuple[tuple[str, ...], str, int]] = []
    stack: list[tuple[int, str]] = []
    block_scalar_indent: int | None = None
    for line_number, raw_line in enumerate(text.splitlines(), 1):
        if not raw_line.strip():
            continue
        indent = len(raw_line) - len(raw_line.lstrip(" "))
        if raw_line[:indent].find("\t") >= 0 or raw_line.startswith("\t"):
            raise WorkflowClosureError(f"{context}:{line_number}: tab indentation is unsupported")
        if block_scalar_indent is not None:
            if indent > block_scalar_indent:
                continue
            block_scalar_indent = None

        code = _strip_yaml_comment(raw_line).rstrip()
        if not code.strip():
            continue
        if "\t" in code[: len(code) - len(code.lstrip())]:
            raise WorkflowClosureError(f"{context}:{line_number}: tab indentation is unsupported")
        if _QUOTED_USES_KEY_RE.match(code):
            raise WorkflowClosureError(
                f"{context}:{line_number}: quoted uses keys are unsupported"
            )
        if _unquoted_token(code, "<<:") or _ANCHOR_RE.search(code) or _ALIAS_RE.search(code):
            raise WorkflowClosureError(
                f"{context}:{line_number}: YAML anchors, aliases, and merge keys are unsupported"
            )

        match = _MAPPING_RE.match(code)
        if match is None:
            if _unquoted_token(code, "uses:"):
                raise WorkflowClosureError(
                    f"{context}:{line_number}: unsupported inline or explicit uses form"
                )
            continue

        key = match.group("key")
        value = match.group("value").strip()
        while stack and stack[-1][0] >= indent:
            stack.pop()
        key_path = tuple(item[1] for item in stack) + (key,)
        entries.append((key_path, value, line_number))

        if key == "uses":
            _parse_yaml_scalar(value, f"{context}:{line_number}")
        elif _unquoted_token(value, "uses:"):
            raise WorkflowClosureError(
                f"{context}:{line_number}: unsupported inline uses form"
            )

        scalar_without_comment = _strip_yaml_comment(value).strip()
        if _BLOCK_SCALAR_RE.fullmatch(scalar_without_comment):
            block_scalar_indent = indent
        elif value == "":
            stack.append((indent, key))
    return entries


def _uses_entries(
    entries: list[tuple[tuple[str, ...], str, int]],
) -> list[tuple[str, int]]:
    result = []
    for key_path, value, line_number in entries:
        if key_path[-1] == "uses":
            result.append((_parse_yaml_scalar(value, f"line {line_number}"), line_number))
    return result


def _action_using(
    entries: list[tuple[tuple[str, ...], str, int]],
    context: str,
) -> str:
    values = [
        (value, line_number)
        for key_path, value, line_number in entries
        if key_path == ("runs", "using")
    ]
    if len(values) != 1:
        raise WorkflowClosureError(
            f"{context}: action metadata must contain exactly one block-form runs.using value "
            f"(found {len(values)})"
        )
    value, line_number = values[0]
    using = _parse_yaml_scalar(value, f"{context}:{line_number} runs.using").lower()
    return using


def _workflow_source(record: Mapping[str, str]) -> dict[str, str]:
    return {
        "repository": record["repository"],
        "workflow_path": record["path"],
        "commit": record["commit"],
        "blob_sha256": record["blob_sha256"],
    }


def collect_workflow_source_closure(
    repository_stores: Mapping[str, Path | str],
    caller: Mapping[str, Any],
    reusable: Mapping[str, Any],
    *,
    limits: Mapping[str, Any] | None = None,
    require_reusable_edge: bool = True,
) -> dict[str, Any]:
    """Collect the exact caller/reusable/action closure from immutable Git blobs.

    ``repository_stores`` is the trusted identity-to-local-object-store map prepared by
    acquisition.  Workflow content never supplies a remote URL.  ``caller`` and
    ``reusable`` each contain exactly ``repository``, ``workflow_path``, and ``commit``.

    When ``require_reusable_edge`` is true (default), the caller static ``uses:`` closure
    must contain the reusable workflow.  Post-release verifiers authorize their own caller
    closure plus the Builds revision they check out, so they may pass false and have both
    roots visited independently.
    """

    caller_coordinate = _validate_coordinate(caller, "caller")
    reusable_coordinate = _validate_coordinate(reusable, "reusable")
    caller_key = (
        caller_coordinate["repository"],
        caller_coordinate["commit"],
        caller_coordinate["workflow_path"],
    )
    reusable_key = (
        reusable_coordinate["repository"],
        reusable_coordinate["commit"],
        reusable_coordinate["workflow_path"],
    )
    if caller_key == reusable_key:
        raise WorkflowClosureError("caller and reusable workflow coordinates must be distinct")

    collector = _Collector(repository_stores, limits)
    collector.visit_workflow(*caller_key, depth=0)
    if require_reusable_edge:
        if reusable_key not in collector._visited:
            raise WorkflowClosureError(
                "caller static closure does not contain the expected reusable workflow "
                f"{reusable_key[0]}@{reusable_key[1]}:{reusable_key[2]}"
            )
    elif reusable_key not in collector._visited:
        collector.visit_workflow(*reusable_key, depth=0)

    caller_record = collector._records[caller_key]
    reusable_record = collector._records[reusable_key]
    actions = [
        dict(record)
        for key, record in collector._records.items()
        if key not in (caller_key, reusable_key)
    ]
    actions.sort(
        key=lambda source: (
            source["repository"],
            source["path"],
            source["commit"],
            source["blob_sha256"],
        )
    )
    material: dict[str, Any] = {
        "caller": _workflow_source(caller_record),
        "reusable": _workflow_source(reusable_record),
        "actions": actions,
    }
    material["definition_digest"] = _canonical_digest(material)
    return material


def _validate_runtime_source(value: Any, context: str) -> dict[str, str]:
    source = _require_closed_object(
        value,
        {"repository", "workflow_path", "commit", "blob_sha256"},
        context,
    )
    return {
        "repository": _require_identity(source["repository"], f"{context}.repository"),
        "workflow_path": _require_workflow_path(
            source["workflow_path"],
            f"{context}.workflow_path",
        ),
        "commit": _require_commit(source["commit"], f"{context}.commit"),
        "blob_sha256": _require_sha256(source["blob_sha256"], f"{context}.blob_sha256"),
    }


def _validate_action_source(value: Any, context: str) -> dict[str, str]:
    source = _require_closed_object(
        value,
        {"repository", "path", "commit", "blob_sha256"},
        context,
    )
    return {
        "repository": _require_identity(source["repository"], f"{context}.repository"),
        "path": _normalize_path(source["path"], f"{context}.path"),
        "commit": _require_commit(source["commit"], f"{context}.commit"),
        "blob_sha256": _require_sha256(source["blob_sha256"], f"{context}.blob_sha256"),
    }


def _validate_closure(value: Any) -> dict[str, Any]:
    closure = _require_closed_object(
        value,
        {"caller", "reusable", "actions", "definition_digest"},
        "closure",
    )
    caller = _validate_runtime_source(closure["caller"], "closure.caller")
    reusable = _validate_runtime_source(closure["reusable"], "closure.reusable")
    raw_actions = closure["actions"]
    if not isinstance(raw_actions, list):
        raise WorkflowClosureError("closure.actions: expected an array")
    actions = [
        _validate_action_source(action, f"closure.actions[{index}]")
        for index, action in enumerate(raw_actions)
    ]
    expected_order = sorted(
        actions,
        key=lambda source: (
            source["repository"],
            source["path"],
            source["commit"],
            source["blob_sha256"],
        ),
    )
    if actions != expected_order:
        raise WorkflowClosureError("closure.actions: sources are not in canonical ordinal order")
    if len({_canonical_bytes(action) for action in actions}) != len(actions):
        raise WorkflowClosureError("closure.actions: duplicate source row")
    expected_digest = _canonical_digest(
        {"caller": caller, "reusable": reusable, "actions": actions}
    )
    observed_digest = _require_sha256(
        closure["definition_digest"],
        "closure.definition_digest",
    )
    if observed_digest != expected_digest:
        raise WorkflowClosureError(
            "closure.definition_digest: digest does not match caller/reusable/actions"
        )
    return {
        "caller": caller,
        "reusable": reusable,
        "actions": actions,
        "definition_digest": observed_digest,
    }


def project_policy_authorization(stage: str, closure: Mapping[str, Any]) -> dict[str, Any]:
    """Project a runtime closure to the exact AD-12 policy-authorization shape."""

    if stage not in _POLICY_STAGES:
        raise WorkflowClosureError(
            f"authorization stage must be one of {_POLICY_STAGES}, got {stage!r}"
        )
    validated = _validate_closure(closure)
    caller = {
        "repository": validated["caller"]["repository"],
        "workflow_path": validated["caller"]["workflow_path"],
        "blob_sha256": validated["caller"]["blob_sha256"],
    }
    reusable = dict(validated["reusable"])
    actions = [dict(action) for action in validated["actions"]]
    material: dict[str, Any] = {
        "stage": stage,
        "caller": caller,
        "reusable": reusable,
        "actions": actions,
    }
    material["closure_digest"] = _canonical_digest(material)
    return material


def _validate_authorization(value: Any, stage: str, context: str) -> dict[str, Any]:
    authorization = _require_closed_object(
        value,
        {"stage", "caller", "reusable", "actions", "closure_digest"},
        context,
    )
    if authorization["stage"] != stage:
        raise WorkflowClosureError(
            f"{context}.stage: expected {stage!r}, found {authorization['stage']!r}"
        )
    caller_raw = _require_closed_object(
        authorization["caller"],
        {"repository", "workflow_path", "blob_sha256"},
        f"{context}.caller",
    )
    caller = {
        "repository": _require_identity(
            caller_raw["repository"],
            f"{context}.caller.repository",
        ),
        "workflow_path": _require_workflow_path(
            caller_raw["workflow_path"],
            f"{context}.caller.workflow_path",
        ),
        "blob_sha256": _require_sha256(
            caller_raw["blob_sha256"],
            f"{context}.caller.blob_sha256",
        ),
    }
    reusable = _validate_runtime_source(authorization["reusable"], f"{context}.reusable")
    raw_actions = authorization["actions"]
    if not isinstance(raw_actions, list):
        raise WorkflowClosureError(f"{context}.actions: expected an array")
    actions = [
        _validate_action_source(action, f"{context}.actions[{index}]")
        for index, action in enumerate(raw_actions)
    ]
    ordered = sorted(
        actions,
        key=lambda source: (
            source["repository"],
            source["path"],
            source["commit"],
            source["blob_sha256"],
        ),
    )
    if actions != ordered:
        raise WorkflowClosureError(f"{context}.actions: sources are not canonically sorted")
    if len({_canonical_bytes(action) for action in actions}) != len(actions):
        raise WorkflowClosureError(f"{context}.actions: duplicate source row")
    material = {
        "stage": stage,
        "caller": caller,
        "reusable": reusable,
        "actions": actions,
    }
    observed_digest = _require_sha256(
        authorization["closure_digest"],
        f"{context}.closure_digest",
    )
    if observed_digest != _canonical_digest(material):
        raise WorkflowClosureError(f"{context}.closure_digest: canonical digest mismatch")
    material["closure_digest"] = observed_digest
    return material


def _authorization_sort_key(value: Mapping[str, Any]) -> tuple[str, ...]:
    caller = value["caller"]
    reusable = value["reusable"]
    return (
        caller["repository"],
        caller["workflow_path"],
        caller["blob_sha256"],
        reusable["repository"],
        reusable["workflow_path"],
        reusable["commit"],
        reusable["blob_sha256"],
        value["closure_digest"],
    )


def require_policy_authorization(
    policy: Mapping[str, Any],
    stage: str,
    closure: Mapping[str, Any],
) -> dict[str, Any]:
    """Require a runtime closure to project exactly one active-policy authorization."""

    expected = project_policy_authorization(stage, closure)
    registry = policy.get("evaluator_authorizations")
    if not isinstance(registry, dict) or set(registry) != set(_POLICY_STAGES):
        actual = set(registry) if isinstance(registry, dict) else set()
        raise WorkflowClosureError(
            "policy.evaluator_authorizations: closed member set mismatch "
            f"(missing={sorted(set(_POLICY_STAGES) - actual)}, "
            f"unknown={sorted(actual - set(_POLICY_STAGES))})"
        )

    validated_by_stage: dict[str, list[dict[str, Any]]] = {}
    for registry_stage in _POLICY_STAGES:
        raw_entries = registry[registry_stage]
        if not isinstance(raw_entries, list):
            raise WorkflowClosureError(
                f"policy.evaluator_authorizations.{registry_stage}: expected an array"
            )
        entries = [
            _validate_authorization(
                entry,
                registry_stage,
                f"policy.evaluator_authorizations.{registry_stage}[{index}]",
            )
            for index, entry in enumerate(raw_entries)
        ]
        if entries != sorted(entries, key=_authorization_sort_key):
            raise WorkflowClosureError(
                f"policy.evaluator_authorizations.{registry_stage}: entries are not "
                "canonically sorted"
            )
        encoded = [_canonical_bytes(entry) for entry in entries]
        if len(set(encoded)) != len(encoded):
            raise WorkflowClosureError(
                f"policy.evaluator_authorizations.{registry_stage}: duplicate authorization"
            )
        validated_by_stage[registry_stage] = entries

    matches = [entry for entry in validated_by_stage[stage] if entry == expected]
    if len(matches) != 1:
        raise WorkflowClosureError(
            f"active policy authorizes {len(matches)} {stage!r} evaluator closures; expected exactly one"
        )
    return matches[0]
