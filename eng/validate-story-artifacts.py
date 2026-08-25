#!/usr/bin/env python3
"""Validate BMAD story artifact hygiene."""

from __future__ import annotations

import argparse
import fnmatch
import json
import re
import subprocess
import sys
import unicodedata
from dataclasses import dataclass
from functools import lru_cache
from pathlib import Path


DEFAULT_SENTINEL_ROOTS = ("_bmad-output", "docs")
DEFAULT_EXCLUDE_PATTERNS = (
    ".git/**",
    "**/bin/**",
    "**/obj/**",
    "**/node_modules/**",
    "docs/_site/**",
    "_bmad-output/story-automator/**",
)
AUTHORING_SENTINEL_TAGS = (
    "argument",
    "arguments",
    "content",
    "function",
    "function_call",
    "function_calls",
    "invoke",
    "parameter",
    "parameters",
    "tool",
    "tool-call",
    "tool-calls",
    "tool_call",
    "tool_calls",
    "tool-use",
    "tool_use",
)
SENTINEL_LINE = re.compile(
    r"^</?(?:"
    + "|".join(re.escape(tag) for tag in AUTHORING_SENTINEL_TAGS)
    + r")(?:\s[^>]*)?>\s*$",
    re.IGNORECASE,
)
FRONTMATTER_LINE = re.compile(r"^([A-Za-z_][A-Za-z0-9_-]*):\s*(.*?)\s*$")
CRITICAL_FRONTMATTER_SCALAR_KEYS = frozenset(
    {"title", "story_id", "baseline_commit"}
)
CHECKED_TASK = re.compile(r"^\s*-\s*\[x\]\s*(.+)$", re.IGNORECASE)
CHECKED_TASK_HEADINGS = frozenset({"tasks", "tasks / subtasks", "tasks & acceptance"})
EXCLUDED_TASK_SUBSECTION_HEADINGS = frozenset({"review findings"})
MARKDOWN_HEADING = re.compile(r"^(#{1,6})\s+(.+?)\s*$")
DOCUMENTED_UNRELATED_HEADINGS = {
    "documented unrelated changes",
    "documented unrelated workspace state",
    "unrelated changes",
    "unrelated workspace state",
}
DOCUMENTED_BLOCKER_HEADINGS = {
    "documented blockers",
    "blockers",
    "known blockers",
}
ACCEPTED_EXTRA_REASONS = (
    "unrelated",
    "pre-existing",
    "preexisting",
    "generated evidence",
    "accepted submodule drift",
    "named exception",
    "exception",
    "blocker",
    "ci-authoritative",
)
TASK_EVIDENCE_KEYWORDS = (
    "add",
    "compare",
    "detect",
    "documentation",
    "ensure",
    "extend",
    "file list",
    "implement",
    "model",
    "parse",
    "report",
    "run",
    "support",
    "test",
    "update",
    "validate",
    "verification",
    "wire",
)
TASK_PATH_SUFFIXES = {
    ".cs",
    ".csproj",
    ".css",
    ".js",
    ".json",
    ".md",
    ".props",
    ".ps1",
    ".py",
    ".razor",
    ".sh",
    ".slnx",
    ".targets",
    ".toml",
    ".ts",
    ".tsx",
    ".txt",
    ".xml",
    ".yaml",
    ".yml",
}
# Verbs that denote producing or altering an output file. Single source of truth: the
# negation probe ("does not update `x`") and the preservation-clause exception ("keep
# behavior and update `x`") must not drift apart, because a verb present in one and
# absent from the other produced contradictory results — "must not update" was
# suppressed while "must not move" was demanded as evidence.
ACTION_VERBS = (
    "add",
    "change",
    "create",
    "delete",
    "edit",
    "extend",
    "generate",
    "implement",
    "modify",
    "move",
    "remove",
    "rename",
    "retarget",
    "split",
    "touch",
    "update",
    "wire",
    "write",
)
# A story may also deny a requirement ("does not require `x`"). That is not an action
# that produces output, so it belongs to the negation probe only.
NEGATION_ONLY_VERBS = ("require",)
# Verbs claiming a file was brought into existence. A creation claim keeps full
# strictness even when the token is a bare basename absent from the tree: a file this
# story created is absent from `git ls-files` by construction, so exempting it would
# make every phantom new-file claim unenforceable.
CREATION_VERBS = ("add", "create", "generate", "introduce", "write")
NEGATION_PREFIXES = (
    "do not",
    "don't",
    "does not",
    "did not",
    "must not",
    "should not",
    "never",
    "no longer",
)


# Irregular simple-past / past-participle forms for ACTION_VERBS / CREATION_VERBS.
# Regular past is derived in verb_alternation; only non-regular stems belong here.
_IRREGULAR_PAST = {
    "split": ("split",),
    "write": ("wrote", "written"),
}


def verb_alternation(verbs: tuple[str, ...]) -> str:
    """Alternation covering each verb, third-person, and simple past / past participle.

    Suppression must not depend on which conjugation the author happened to write, and a
    bare `s?` quantifier cannot form `modifies` or `touches`. Past tense matters too:
    `created` / `updated` must not evade creation-claim strictness or the preserve-clause
    exception that `create` / `update` already hit.
    """
    forms: set[str] = set()
    for verb in verbs:
        forms.add(verb)
        if verb.endswith("y") and verb[-2:-1] not in "aeiou":
            forms.add(verb[:-1] + "ies")
        elif verb.endswith(("s", "x", "z", "ch", "sh")):
            forms.add(verb + "es")
        else:
            forms.add(verb + "s")
        if verb in _IRREGULAR_PAST:
            forms.update(_IRREGULAR_PAST[verb])
        elif verb.endswith("e"):
            forms.add(verb + "d")
        elif verb.endswith("y") and verb[-2:-1] not in "aeiou":
            forms.add(verb[:-1] + "ied")
        else:
            forms.add(verb + "ed")
    return "|".join(sorted(forms, key=lambda form: (-len(form), form)))


NEGATED_ACTION = re.compile(
    r"\b(?:" + "|".join(NEGATION_PREFIXES) + r")\s+"
    r"(?:" + verb_alternation(ACTION_VERBS + NEGATION_ONLY_VERBS) + r")\s*$",
    re.IGNORECASE,
)
POSITIVE_ACTION = re.compile(
    r"\b(?:" + verb_alternation(ACTION_VERBS) + r")\s*$",
    re.IGNORECASE,
)
CREATION_ACTION = re.compile(
    r"\b(?:" + verb_alternation(CREATION_VERBS) + r")\b",
    re.IGNORECASE,
)
PATH_COORDINATE = re.compile(r":\d+(?:[-,:]\d+)*$")
OWNERSHIP_CONTRIBUTING_CLASSIFICATIONS = frozenset(
    {"owned", "interleaved", "bootstrap-owned"}
)
BOOTSTRAP_OWNED_STORY_ID = "9.7"
BOOTSTRAP_OWNED_BASELINE = "ceae00a4f9788222ed19153acfc05d68d0bc85d1"
BOOTSTRAP_OWNED_COMMIT = "fd04bdd97fbdd4976a0f213e46a316be199fd8a9"
BOOTSTRAP_OWNED_STORY_PATH = (
    "_bmad-output/implementation-artifacts/"
    "spec-9-7-add-story-id-and-commit-scope-evidence.md"
)
BOOTSTRAP_OWNED_VALIDATOR_PATH = "eng/validate-story-artifacts.py"
BOOTSTRAP_OWNED_VALIDATOR_TEST_PATH = "eng/tests/test_validate_story_artifacts.py"
BOOTSTRAP_OWNED_PATHS = frozenset(
    {
        ".agents/skills/bmad-build/spec-template.md",
        ".agents/skills/bmad-build/step-02-plan.md",
        ".agents/skills/bmad-build/step-04-review.md",
        ".agents/skills/bmad-build/step-05-present.md",
        ".github/workflows/quality.yml",
        "_bmad-output/implementation-artifacts/deferred-work.md",
        "_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md",
        "_bmad-output/implementation-artifacts/sprint-status.yaml",
        "_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md",
        BOOTSTRAP_OWNED_VALIDATOR_TEST_PATH,
        BOOTSTRAP_OWNED_VALIDATOR_PATH,
        "references/Hexalith.Tenants",
        "tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs",
    }
)
BOOTSTRAP_OWNED_GUARD_PATHS = frozenset(
    {BOOTSTRAP_OWNED_VALIDATOR_PATH, BOOTSTRAP_OWNED_VALIDATOR_TEST_PATH}
)


@dataclass(frozen=True)
class StoryMetadata:
    baseline_commit: str
    story_id: str
    metadata_failures: list[str]
    file_list: dict[str, str]
    unrelated: dict[str, str]
    blockers: dict[str, str]
    commit_scope_dispositions: dict[str, tuple[str, str]]
    commit_scope_disposition_failures: list[str]
    checked_tasks: list[tuple[int, str]]
    notices: list[str]
    evidence_text: str


@dataclass(frozen=True)
class ChangedFiles:
    files: list[str]
    source: str
    base: str


@dataclass(frozen=True)
class CommitEvidence:
    sha: str
    subject: str
    paths: list[str]
    story_id_matches: bool
    classification: str
    disposition_reason: str


@dataclass(frozen=True)
class MergeEvidence:
    sha: str
    subject: str
    declared_disposition: str
    disposition_reason: str


@dataclass(frozen=True)
class WorkspaceEvidence:
    staged: list[str]
    unstaged: list[str]
    untracked: list[str]
    unresolved: list[str]


@dataclass(frozen=True)
class CommitScopeEvidence:
    story_id: str
    baseline: str
    candidate: str
    commits: list[CommitEvidence]
    merges: list[MergeEvidence]
    workspace: WorkspaceEvidence


def main() -> int:
    args = parse_args()
    root = Path(args.project_root).resolve()
    failures: list[str] = []
    notices: list[str] = []

    candidate_mode = args.candidate is not None
    candidate_ref = args.candidate.strip() if candidate_mode else ""
    candidate_has_value = bool(candidate_ref)
    base_override_valid = args.base is None or bool(args.base.strip())
    candidate_valid = candidate_has_value and not args.changed_file and base_override_valid

    if candidate_mode and not args.story:
        failures.append("--candidate requires --story")
    if candidate_mode and not candidate_has_value:
        failures.append("--candidate requires a non-empty ref")
    if candidate_mode and args.changed_file:
        failures.append("--changed-file cannot be combined with --candidate")
    if not base_override_valid:
        failures.append("--base requires a non-empty ref")

    if not args.skip_sentinel:
        failures.extend(scan_sentinels(root, args.sentinel_root, args.exclude))

    if args.story:
        story = resolve_under_root(root, args.story)
        metadata = parse_story_metadata(story)
        failures.extend(metadata.metadata_failures)
        notices.extend(metadata.notices)
        base = metadata.baseline_commit if args.base is None else args.base.strip()
        cli_unrelated = parse_cli_unrelated(root, args.unrelated, args.reason)
        unrelated = {**metadata.unrelated, **cli_unrelated}
        commit_evidence: CommitScopeEvidence | None = None
        if candidate_valid:
            commit_evidence, commit_failures = collect_commit_scope_evidence(
                root,
                base,
                candidate_ref,
                metadata,
                story,
            )
            failures.extend(
                failure for failure in commit_failures if failure not in failures
            )
        else:
            failures.extend(
                failure
                for failure in metadata.commit_scope_disposition_failures
                if failure not in failures
            )

        if not candidate_mode:
            changed_files = collect_changed_files(root, base, args.changed_file, args.exclude)
            if changed_files.source.startswith("git workspace fallback"):
                notices.append(
                    "degraded changed-file discovery: "
                    f"{changed_files.source}; the declared baseline was not used"
                )
        elif not candidate_valid:
            changed_files = ChangedFiles([], "invalid candidate invocation", base or "")
        else:
            try:
                changed_files = collect_reconciled_changed_files(
                    root,
                    commit_evidence,
                    metadata.file_list,
                    args.exclude,
                    base,
                )
            except RuntimeError as exc:
                failures.append(str(exc))
                changed_files = ChangedFiles([], "commit scope unavailable", base or "")
        usable_unrelated = usable_classified_paths(root, unrelated, set(changed_files.files))
        bounded_unrelated = {path: unrelated[path] for path in usable_unrelated}
        failures.extend(check_file_list(root, story, changed_files, metadata.file_list, bounded_unrelated))
        failures.extend(check_checked_tasks(root, story, changed_files.files, metadata, bounded_unrelated))
        unrelated_changed = [
            path
            for path in changed_files.files
            if path_is_classified_unrelated(path, usable_unrelated)
        ]
        if unrelated_changed:
            notices.append(
                "unrelated dirty files documented for "
                f"{story.relative_to(root).as_posix()}:\n"
                + "\n".join(
                    f"  - {format_git_path(path)}: "
                    f"{format_report_value(classified_path_reason(path, bounded_unrelated))}"
                    for path in unrelated_changed
                )
            )

        if commit_evidence is not None:
            notices.append(format_commit_scope_evidence(commit_evidence, metadata.file_list, bounded_unrelated))

    for notice in notices:
        print(notice)

    if failures:
        for failure in failures:
            print(failure, file=sys.stderr)
        return 1

    print("Story artifact validation passed.")
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", default=".", help="Repository root. Defaults to current directory.")
    parser.add_argument("--story", help="Story markdown file whose File List should be checked.")
    parser.add_argument("--base", help="Optional git base ref for changed-file discovery.")
    parser.add_argument(
        "--candidate",
        default=None,
        help=(
            "Candidate git ref for strict baseline-to-candidate story commit evidence. "
            "Requires --story and a baseline from --base or story frontmatter."
        ),
    )
    parser.add_argument(
        "--changed-file",
        action="append",
        default=[],
        help=(
            "Changed file path. Can be supplied multiple times; bypasses git discovery and default changed-file "
            "exclusions for File List checks."
        ),
    )
    parser.add_argument(
        "--unrelated",
        action="append",
        default=[],
        help="Dirty path that is explicitly unrelated to the story. Pair repeatably with --reason.",
    )
    parser.add_argument(
        "--reason",
        action="append",
        default=[],
        help="Reason for the matching --unrelated path.",
    )
    parser.add_argument(
        "--sentinel-root",
        action="append",
        default=[],
        help="Root or markdown file to scan for raw authoring sentinels. Defaults to _bmad-output and docs.",
    )
    parser.add_argument(
        "--exclude",
        action="append",
        default=[],
        help="Glob pattern to exclude. Story-automator logs, build output, and docs/_site are excluded by default.",
    )
    parser.add_argument("--skip-sentinel", action="store_true", help="Skip raw authoring sentinel scan.")
    return parser.parse_args()


def resolve_under_root(root: Path, value: str | Path) -> Path:
    raw = Path(value)
    resolved = (root / raw).resolve() if not raw.is_absolute() else raw.resolve()
    try:
        resolved.relative_to(root)
    except ValueError as exc:
        raise SystemExit(f"Path escapes project root: {value}") from exc
    return resolved


def merged_excludes(extra: list[str]) -> tuple[str, ...]:
    return (*DEFAULT_EXCLUDE_PATTERNS, *extra)


def is_excluded(path: str, patterns: list[str] | tuple[str, ...]) -> bool:
    normalized = path.replace("\\", "/")
    return any(fnmatch.fnmatch(normalized, pattern) for pattern in patterns)


def markdown_files(root: Path, roots: list[str], excludes: list[str]) -> list[Path]:
    selected = roots or list(DEFAULT_SENTINEL_ROOTS)
    patterns = merged_excludes(excludes)
    files: list[Path] = []
    for raw in selected:
        path = resolve_under_root(root, raw)
        if path.is_file():
            if path.suffix.lower() == ".md" and not is_excluded(path.relative_to(root).as_posix(), patterns):
                files.append(path)
            continue
        if not path.exists():
            continue
        for candidate in path.rglob("*.md"):
            rel = candidate.relative_to(root).as_posix()
            if not is_excluded(rel, patterns):
                files.append(candidate)
    return sorted(set(files))


def scan_sentinels(root: Path, roots: list[str], excludes: list[str]) -> list[str]:
    failures: list[str] = []
    for path in markdown_files(root, roots, excludes):
        in_fence = False
        fence_marker = ""
        for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            stripped = line.strip()
            if stripped.startswith(("```", "~~~")):
                marker = stripped[:3]
                if in_fence and marker == fence_marker:
                    in_fence = False
                    fence_marker = ""
                elif not in_fence:
                    in_fence = True
                    fence_marker = marker
                continue
            if in_fence or stripped.startswith(">"):
                continue
            if SENTINEL_LINE.match(stripped):
                failures.append(
                    f"raw authoring sentinel: {path.relative_to(root).as_posix()}:{line_number}: {stripped}"
                )
    return failures


def parse_story_metadata(story: Path) -> StoryMetadata:
    text = story.read_text(encoding="utf-8")
    frontmatter, frontmatter_failures, invalid_frontmatter_keys = extract_frontmatter(text)
    story_id, story_id_failures = extract_story_id(frontmatter, text, story.name)
    if "story_id" in invalid_frontmatter_keys:
        story_id = ""
    checked_tasks, checked_task_failures, checked_task_notices = extract_checked_tasks(text)
    metadata_failures = [
        *frontmatter_failures,
        *story_id_failures,
        *checked_task_failures,
    ]
    sections = extract_sections(text)
    section_heading_lines = extract_section_heading_lines(text)
    file_list = extract_story_file_list(sections.get("file list", ""))
    unrelated = extract_classified_paths(sections, DOCUMENTED_UNRELATED_HEADINGS)
    blockers = extract_classified_paths(sections, DOCUMENTED_BLOCKER_HEADINGS)
    dispositions, disposition_failures = extract_commit_scope_dispositions(
        sections.get("commit scope dispositions", ""),
        line_offset=section_heading_lines.get("commit scope dispositions", 0),
    )
    evidence_text = "\n".join(
        sections.get(name, "")
        for name in (
            "debug log references",
            "completion notes list",
            "completion notes",
            "test evidence",
            "change log",
        )
    )
    return StoryMetadata(
        baseline_commit=(
            ""
            if "baseline_commit" in invalid_frontmatter_keys
            else frontmatter.get("baseline_commit", "")
        ),
        story_id=story_id,
        metadata_failures=metadata_failures,
        file_list=file_list,
        unrelated=unrelated,
        blockers=blockers,
        commit_scope_dispositions=dispositions,
        commit_scope_disposition_failures=disposition_failures,
        checked_tasks=checked_tasks,
        notices=checked_task_notices,
        evidence_text=evidence_text,
    )


def extract_story_id(
    frontmatter: dict[str, str],
    text: str,
    filename: str,
) -> tuple[str, list[str]]:
    explicit = frontmatter.get("story_id", "").strip()
    if explicit:
        match = re.fullmatch(r"(\d+)[.-](\d+)", explicit)
        if not match:
            return "", [
                "invalid explicit story_id: expected exactly two numeric segments separated by '.' or '-': "
                f"{explicit!r}"
            ]
        return f"{match.group(1)}.{match.group(2)}", []

    failures: list[str] = []
    detected: list[tuple[str, str]] = []
    malformed_story = re.compile(r"\bStory\s+\d+[.-]\d+[.-]\d+", re.IGNORECASE)
    text_story = re.compile(
        r"\bStory\s+(\d+)[.-](\d+)(?![A-Za-z0-9]|[.-]\d)",
        re.IGNORECASE,
    )

    title = frontmatter.get("title", "").strip()
    if title:
        if malformed_story.search(title):
            failures.append(f"invalid legacy story identity in title: {title!r}")
        elif match := text_story.search(title):
            detected.append(("title", f"{match.group(1)}.{match.group(2)}"))

    for heading in markdown_h1_headings(text):
        if malformed_story.search(heading):
            failures.append(f"invalid legacy story identity in H1: {heading!r}")
        elif match := text_story.search(heading):
            detected.append(("H1", f"{match.group(1)}.{match.group(2)}"))
        break

    malformed_filename = re.match(
        r"^(?:spec-)?\d+[.-]\d+[.-]\d+(?:-|\.md$)",
        filename,
    )
    if malformed_filename:
        failures.append(f"invalid legacy story identity in filename: {filename!r}")

    dotted_filename_identity = re.match(
        r"^(?:spec-)?(\d+)[.-](\d+)(?!\d|[.-]\d)(?:-|\.md$)",
        filename,
    )
    if dotted_filename_identity:
        detected.append(
            (
                "filename",
                f"{dotted_filename_identity.group(1)}.{dotted_filename_identity.group(2)}",
            )
        )

    identities = {identity for _, identity in detected}
    if len(identities) > 1:
        evidence = ", ".join(f"{source}={identity}" for source, identity in detected)
        failures.append(f"conflicting legacy story identities: {evidence}")
        return "", failures
    if failures:
        return "", failures
    return next(iter(identities), ""), []


def markdown_h1_headings(text: str) -> list[str]:
    """Return H1 headings outside YAML frontmatter and fenced code blocks."""
    return [
        heading.group(2).strip()
        for _, line in markdown_lines_outside_frontmatter_and_fences(text)
        if (heading := MARKDOWN_HEADING.match(line.strip()))
        and len(heading.group(1)) == 1
    ]


def markdown_lines_outside_frontmatter_and_fences(
    text: str,
) -> list[tuple[int, str]]:
    """Return source-numbered Markdown lines that can carry document semantics.

    YAML frontmatter and fenced examples are author-controlled data, not document
    structure. A fence closes only with the same marker, at least the opening
    length, and whitespace after it; an info-like suffix inside a fence is content.
    """
    # Markdown line endings are CR/LF sequences. Unicode line and paragraph
    # separators remain author-controlled scalar content and must reach the
    # report-quoting boundary intact.
    lines = [line.rstrip("\r") for line in text.split("\n")]
    index = 0
    if lines and lines[0].strip() == "---":
        index = 1
        while index < len(lines):
            if lines[index].strip() == "---":
                index += 1
                break
            index += 1

    semantic_lines: list[tuple[int, str]] = []
    fence_character = ""
    fence_length = 0
    for line_number, line in enumerate(lines[index:], start=index + 1):
        fence = re.match(r"^ {0,3}(`{3,}|~{3,})(.*)$", line)
        if fence_character:
            if fence:
                marker = fence.group(1)
                suffix = fence.group(2)
                if (
                    marker[0] == fence_character
                    and len(marker) >= fence_length
                    and not suffix.strip()
                ):
                    fence_character = ""
                    fence_length = 0
            continue
        if fence:
            marker = fence.group(1)
            fence_character = marker[0]
            fence_length = len(marker)
            continue
        semantic_lines.append((line_number, line))
    return semantic_lines


def extract_commit_scope_dispositions(
    body: str,
    *,
    line_offset: int = 0,
) -> tuple[dict[str, tuple[str, str]], list[str]]:
    dispositions: dict[str, tuple[str, str]] = {}
    failures: list[str] = []
    declaration = re.compile(
        r"^-\s*`([0-9A-Fa-f]{40})`\s*\|\s*`(shared|process|bootstrap-owned)`\s*\|\s*(\S.*)$"
    )
    bootstrap_owned_declarations = 0
    for line_number, line in enumerate(body.split("\n"), start=1):
        line = line.rstrip("\r")
        stripped = line.strip()
        if not stripped:
            continue
        if not stripped.startswith("-"):
            continue
        match = declaration.fullmatch(stripped)
        if not match:
            failures.append(
                "malformed Commit Scope Dispositions declaration "
                f"at line {line_offset + line_number}: {stripped}"
            )
            continue
        sha = match.group(1).lower()
        kind = match.group(2)
        reason = match.group(3).strip()
        if kind == "bootstrap-owned":
            bootstrap_owned_declarations += 1
        if sha in dispositions:
            failures.append(f"duplicate Commit Scope Dispositions declaration for {sha}")
            continue
        dispositions[sha] = (kind, reason)
    if bootstrap_owned_declarations > 1:
        failures.append(
            "multiple bootstrap-owned Commit Scope Dispositions declarations are not allowed"
        )
    return dispositions, failures


def extract_frontmatter(text: str) -> tuple[dict[str, str], list[str], set[str]]:
    if not text.startswith("---"):
        return {}, [], set()
    parts = text.split("---", 2)
    if len(parts) < 3:
        return {}, [], set()
    values: dict[str, str] = {}
    failures: list[str] = []
    invalid_keys: set[str] = set()
    for line in parts[1].splitlines():
        match = FRONTMATTER_LINE.match(line.strip())
        if match:
            key = match.group(1).strip()
            if key in invalid_keys:
                continue
            if key in values:
                qualifier = "critical " if key in CRITICAL_FRONTMATTER_SCALAR_KEYS else ""
                failures.append(f"duplicate {qualifier}frontmatter key {key!r}")
                invalid_keys.add(key)
                values.pop(key)
                continue
            values[key] = parse_frontmatter_scalar(match.group(2))
    return values, failures, invalid_keys


def parse_frontmatter_scalar(raw: str) -> str:
    """Return a simple YAML scalar without a trailing, unquoted comment."""
    stripped = raw.lstrip()
    quote = stripped[0] if stripped.startswith(("'", '"')) else ""
    opening_index = len(raw) - len(stripped) if quote else -1
    escaped = False
    comment_start: int | None = None
    for index, character in enumerate(raw):
        if index == opening_index:
            continue
        if escaped:
            escaped = False
            continue
        if quote == '"' and character == "\\":
            escaped = True
            continue
        if quote and character == quote:
            if quote == "'" and index + 1 < len(raw) and raw[index + 1] == "'":
                escaped = True
                continue
            if quote == character:
                quote = ""
            continue
        if character == "#" and not quote and (index == 0 or raw[index - 1].isspace()):
            comment_start = index
            break
    value = raw[:comment_start] if comment_start is not None else raw
    value = value.strip()
    if len(value) >= 2 and value[0] == value[-1] and value[0] in {"'", '"'}:
        unquoted = value[1:-1]
        return unquoted.replace("''", "'") if value[0] == "'" else unquoted
    return value


def extract_sections(text: str) -> dict[str, str]:
    sections: dict[str, list[str]] = {}
    heading_lines: dict[str, int] = {}
    current = ""
    for line_number, line in markdown_lines_outside_frontmatter_and_fences(text):
        heading = MARKDOWN_HEADING.match(line.strip())
        if heading and 2 <= len(heading.group(1)) <= 6:
            current = heading.group(2).strip().lower()
            sections.setdefault(current, [])
            heading_lines.setdefault(current, line_number)
            continue
        if current:
            body = sections.setdefault(current, [])
            body_index = line_number - heading_lines[current] - 1
            if body_index >= len(body):
                body.extend("" for _ in range(body_index - len(body) + 1))
            body[body_index] = line
    return {key: "\n".join(value) for key, value in sections.items()}


def extract_section_heading_lines(text: str) -> dict[str, int]:
    """Return the first source line for each Markdown section heading."""
    headings: dict[str, int] = {}
    for line_number, line in markdown_lines_outside_frontmatter_and_fences(text):
        heading = MARKDOWN_HEADING.match(line.strip())
        if heading and 2 <= len(heading.group(1)) <= 6:
            headings.setdefault(heading.group(2).strip().lower(), line_number)
    return headings


def extract_classified_paths(sections: dict[str, str], headings: set[str]) -> dict[str, str]:
    classified: dict[str, str] = {}
    for heading in headings:
        body = sections.get(heading, "")
        for line in body.split("\n"):
            line = line.rstrip("\r")
            stripped = line.strip()
            if not stripped.startswith("-"):
                continue
            path = extract_file_list_entry(stripped)
            if not path:
                continue
            normalized = path.rstrip("/")
            if normalized:
                classified[normalized] = extract_reason(
                    stripped,
                    default="documented exception",
                )
    return classified


def parse_cli_unrelated(root: Path, paths: list[str], reasons: list[str]) -> dict[str, str]:
    unrelated: dict[str, str] = {}
    for index, path in enumerate(paths):
        normalized = normalize_changed_path(root, path).rstrip("/")
        if not normalized:
            continue
        reason = reasons[index].strip() if index < len(reasons) and reasons[index].strip() else "documented unrelated"
        unrelated[normalized] = reason
    return unrelated


def collect_changed_files(root: Path, base: str | None, provided: list[str], excludes: list[str]) -> ChangedFiles:
    patterns = merged_excludes(excludes)
    if provided:
        normalized = {normalize_changed_path(root, path) for path in provided}
        return ChangedFiles(sorted(path for path in normalized if path), "provided --changed-file", "")

    changed: set[str] = set()
    diff_args = ["git", "diff", "--name-only", "--diff-filter=ACMRTD"]
    resolved_base, fallback_reason = resolve_legacy_diff_base(root, base)
    if resolved_base:
        diff_args.append(resolved_base)
    changed.update(run_git_lines(root, diff_args))
    changed.update(run_git_lines(root, ["git", "diff", "--cached", "--name-only", "--diff-filter=ACMRTD"]))
    changed.update(run_git_lines(root, ["git", "ls-files", "--others", "--exclude-standard"]))
    source = "git" if resolved_base else f"git workspace fallback ({fallback_reason})"
    return ChangedFiles(
        sorted(path for path in changed if path and not is_excluded(path, patterns)),
        source,
        base or "",
    )


def resolve_legacy_diff_base(root: Path, base: str | None) -> tuple[str, str]:
    """Resolve a usable legacy baseline or select the explicit bare-diff fallback."""
    requested = (base or "").strip()
    if not requested:
        return "", "missing baseline; bare diff fallback"
    if requested == "NO_VCS":
        return "", "NO_VCS baseline; bare diff fallback"
    try:
        return canonical_commit(root, requested, "legacy baseline"), ""
    except RuntimeError:
        return "", f"unresolvable baseline {requested!r}; bare diff fallback"


def normalize_changed_path(root: Path, value: str) -> str:
    raw = Path(value)
    resolved = (root / raw).resolve() if not raw.is_absolute() else raw.resolve()
    try:
        return resolved.relative_to(root).as_posix()
    except ValueError:
        return ""


def run_git_lines(root: Path, args: list[str]) -> list[str]:
    try:
        result = subprocess.run(args, cwd=root, check=False, capture_output=True, text=True)
    except FileNotFoundError:
        return []
    if result.returncode != 0:
        return []
    return [line.strip().replace("\\", "/") for line in result.stdout.splitlines() if line.strip()]


def run_git_checked(root: Path, args: list[str], operation: str) -> subprocess.CompletedProcess[str]:
    try:
        result = subprocess.run(args, cwd=root, check=False, capture_output=True, text=True)
    except (FileNotFoundError, OSError) as exc:
        raise RuntimeError(f"git failure while {operation}: {exc}") from exc
    if result.returncode != 0:
        detail = result.stderr.strip() or result.stdout.strip() or f"git exited {result.returncode}"
        raise RuntimeError(f"git failure while {operation}: {detail}")
    return result


def run_git_checked_bytes(root: Path, args: list[str], operation: str) -> subprocess.CompletedProcess[bytes]:
    try:
        result = subprocess.run(args, cwd=root, check=False, capture_output=True)
    except (FileNotFoundError, OSError) as exc:
        raise RuntimeError(f"git failure while {operation}: {exc}") from exc
    if result.returncode != 0:
        detail = (
            result.stderr.decode("utf-8", errors="surrogateescape").strip()
            or result.stdout.decode("utf-8", errors="surrogateescape").strip()
            or f"git exited {result.returncode}"
        )
        raise RuntimeError(f"git failure while {operation}: {detail}")
    return result


def decode_nul_paths(output: bytes) -> list[str]:
    if output and not output.endswith(b"\0"):
        raise RuntimeError("git failure while parsing NUL-delimited paths: output lacks a trailing NUL")
    return [
        raw.decode("utf-8", errors="surrogateescape")
        for raw in output.split(b"\0")
        if raw
    ]


def canonical_commit(root: Path, ref: str, label: str) -> str:
    if not ref.strip():
        raise RuntimeError(f"git failure while resolving {label}: ref is empty")
    result = run_git_checked(
        root,
        ["git", "rev-parse", "--verify", "--end-of-options", f"{ref}^{{commit}}"],
        f"resolving {label} ref {ref!r}",
    )
    sha = result.stdout.strip().lower()
    if not re.fullmatch(r"[0-9a-f]{40}", sha):
        raise RuntimeError(f"git failure while resolving {label}: expected a full 40-character SHA, got {sha!r}")
    return sha


def story_id_pattern(story_id: str) -> re.Pattern[str]:
    match = re.fullmatch(r"(\d+)[.-](\d+)", story_id.strip())
    if not match:
        raise ValueError(
            f"story_id must contain exactly two numeric segments separated by '.' or '-': {story_id!r}"
        )
    epic, story = match.groups()
    return re.compile(
        rf"(?<![A-Za-z0-9])(?<!\d[.-]){re.escape(epic)}[.-]{re.escape(story)}"
        r"(?![A-Za-z0-9]|[.-]\d)"
    )


def bootstrap_owned_authorization_failures(
    *,
    story_path: str,
    story_id: str,
    declared_baseline: str,
    resolved_baseline: str,
    sha: str,
    parents: list[str],
    subject_matches: bool,
    paths: set[str] | None,
    file_list: set[str],
    disposition_failures: list[str],
    bootstrap_declaration_count: int,
) -> list[str]:
    """Return every reason the one historical bootstrap authorization is invalid."""
    failures: list[str] = []
    if disposition_failures:
        failures.append("the Commit Scope Dispositions section contains invalid declarations")
    if bootstrap_declaration_count != 1:
        failures.append(
            "the Commit Scope Dispositions section must contain exactly one bootstrap-owned declaration"
        )
    if story_path != BOOTSTRAP_OWNED_STORY_PATH:
        failures.append(
            "the story artifact path must be "
            f"{BOOTSTRAP_OWNED_STORY_PATH}, got {story_path}"
        )
    if story_id != BOOTSTRAP_OWNED_STORY_ID:
        failures.append(
            f"the story ID must be {BOOTSTRAP_OWNED_STORY_ID}, got {story_id or '(missing)'}"
        )
    if declared_baseline != BOOTSTRAP_OWNED_BASELINE:
        failures.append(
            "the declared story baseline_commit must be the exact 40-character SHA "
            f"{BOOTSTRAP_OWNED_BASELINE}, got {declared_baseline}"
        )
    if resolved_baseline != BOOTSTRAP_OWNED_BASELINE:
        failures.append(
            f"the resolved baseline must be {BOOTSTRAP_OWNED_BASELINE}, got {resolved_baseline}"
        )
    if sha != BOOTSTRAP_OWNED_COMMIT:
        failures.append(
            f"the commit must be {BOOTSTRAP_OWNED_COMMIT}, got {sha}"
        )
    if parents != [BOOTSTRAP_OWNED_BASELINE]:
        rendered_parents = " ".join(parents) if parents else "(none)"
        failures.append(
            "the commit must be a non-merge whose sole parent is "
            f"{BOOTSTRAP_OWNED_BASELINE}, got {rendered_parents}"
        )
    if subject_matches:
        failures.append("the historical bootstrap commit subject must not match story 9.7")

    missing_listed_guards = sorted(BOOTSTRAP_OWNED_GUARD_PATHS - file_list)
    if missing_listed_guards:
        failures.append(
            "the story File List must contain both guard paths; missing: "
            + ", ".join(missing_listed_guards)
        )

    if paths is not None:
        missing_touched_guards = sorted(BOOTSTRAP_OWNED_GUARD_PATHS - paths)
        if missing_touched_guards:
            failures.append(
                "the bootstrap commit must touch both guard paths; missing: "
                + ", ".join(missing_touched_guards)
            )

        listed_touched_paths = paths & file_list
        if listed_touched_paths != BOOTSTRAP_OWNED_PATHS:
            missing_paths = sorted(BOOTSTRAP_OWNED_PATHS - listed_touched_paths)
            unexpected_paths = sorted(listed_touched_paths - BOOTSTRAP_OWNED_PATHS)
            details: list[str] = []
            if missing_paths:
                details.append("missing " + ", ".join(missing_paths))
            if unexpected_paths:
                details.append("unexpected " + ", ".join(unexpected_paths))
            failures.append(
                "the bootstrap commit/File List intersection must equal the immutable authorized path set: "
                + "; ".join(details)
            )
    return failures


def collect_commit_scope_evidence(
    root: Path,
    base_ref: str,
    candidate_ref: str,
    metadata: StoryMetadata,
    story: Path,
) -> tuple[CommitScopeEvidence | None, list[str]]:
    failures = list(metadata.commit_scope_disposition_failures)
    if not base_ref.strip():
        failures.append(
            "commit scope evidence requires a non-empty baseline_commit or --base ref"
        )
        return None, failures
    if base_ref.strip() == "NO_VCS":
        failures.append(
            "commit scope evidence cannot use baseline NO_VCS; provide a resolvable commit via baseline_commit or --base"
        )
        return None, failures
    try:
        matcher = story_id_pattern(metadata.story_id)
    except ValueError as exc:
        failures.append(f"commit scope evidence cannot run: {exc}")
        return None, failures

    try:
        baseline = canonical_commit(root, base_ref, "baseline")
        candidate = canonical_commit(root, candidate_ref, "candidate")
        ancestry = subprocess.run(
            ["git", "merge-base", "--is-ancestor", baseline, candidate],
            cwd=root,
            check=False,
            capture_output=True,
            text=True,
        )
        if ancestry.returncode == 1:
            failures.append(
                f"commit scope evidence requires baseline {baseline} to be an ancestor of candidate {candidate}"
            )
            return None, failures
        if ancestry.returncode != 0:
            detail = ancestry.stderr.strip() or ancestry.stdout.strip() or f"git exited {ancestry.returncode}"
            raise RuntimeError(f"git failure while checking baseline ancestry: {detail}")

        log = run_git_checked(
            root,
            [
                "git",
                "log",
                "--reverse",
                "--topo-order",
                "--format=%H%x1f%P%x1f%s",
                f"{baseline}..{candidate}",
                "--",
            ],
            "listing baseline-to-candidate commits",
        )
        commit_rows: list[tuple[str, list[str], str]] = []
        # Git records are LF-delimited. Unicode line/paragraph separators are valid
        # subject content and must survive until deterministic report quoting.
        for line in log.stdout.split("\n"):
            line = line.rstrip("\r")
            if not line.strip():
                continue
            parts = line.split("\x1f", 2)
            if len(parts) != 3:
                raise RuntimeError(f"git failure while parsing commit evidence: malformed log row {line!r}")
            sha, parents, subject = parts
            parent_list = [parent for parent in parents.split() if parent]
            commit_rows.append((sha.lower(), parent_list, subject))
    except (FileNotFoundError, OSError, RuntimeError) as exc:
        failures.append(str(exc))
        return None, failures

    range_shas = {sha for sha, _, _ in commit_rows}
    for sha in metadata.commit_scope_dispositions:
        if sha not in range_shas:
            failures.append(
                f"stale Commit Scope Dispositions declaration: {sha} is not in range {baseline}..{candidate}"
            )

    commits: list[CommitEvidence] = []
    merges: list[MergeEvidence] = []
    listed = set(metadata.file_list)
    story_path = story.relative_to(root).as_posix()
    bootstrap_declaration_count = sum(
        1
        for kind, _ in metadata.commit_scope_dispositions.values()
        if kind == "bootstrap-owned"
    )
    for sha, parents, subject in commit_rows:
        disposition = metadata.commit_scope_dispositions.get(sha)
        if len(parents) > 1:
            if disposition and disposition[0] == "bootstrap-owned":
                authorization_failures = bootstrap_owned_authorization_failures(
                    story_path=story_path,
                    story_id=metadata.story_id,
                    declared_baseline=metadata.baseline_commit,
                    resolved_baseline=baseline,
                    sha=sha,
                    parents=parents,
                    subject_matches=matcher.search(subject) is not None,
                    paths=None,
                    file_list=listed,
                    disposition_failures=metadata.commit_scope_disposition_failures,
                    bootstrap_declaration_count=bootstrap_declaration_count,
                )
                failures.extend(
                    f"invalid bootstrap-owned disposition for {sha}: {failure}"
                    for failure in authorization_failures
                )
                disposition = None
            merges.append(
                MergeEvidence(
                    sha=sha,
                    subject=subject,
                    declared_disposition=disposition[0] if disposition else "",
                    disposition_reason=disposition[1] if disposition else "",
                )
            )
            continue

        try:
            diff = run_git_checked_bytes(
                root,
                [
                    "git",
                    "diff-tree",
                    "--root",
                    "--no-commit-id",
                    "--name-only",
                    "-z",
                    "--no-renames",
                    "-r",
                    "--diff-filter=ACMRTD",
                    sha,
                    "--",
                ],
                f"listing paths for commit {sha}",
            )
        except RuntimeError as exc:
            failures.append(str(exc))
            continue
        try:
            paths = sorted(decode_nul_paths(diff.stdout))
        except RuntimeError as exc:
            failures.append(f"{exc} for commit {sha}")
            continue
        matches = matcher.search(subject) is not None
        owned_paths = [path for path in paths if path in listed]
        unowned_paths = [path for path in paths if path not in listed]

        if disposition and disposition[0] == "bootstrap-owned":
            authorization_failures = bootstrap_owned_authorization_failures(
                story_path=story_path,
                story_id=metadata.story_id,
                declared_baseline=metadata.baseline_commit,
                resolved_baseline=baseline,
                sha=sha,
                parents=parents,
                subject_matches=matches,
                paths=set(paths),
                file_list=listed,
                disposition_failures=metadata.commit_scope_disposition_failures,
                bootstrap_declaration_count=bootstrap_declaration_count,
            )
            if authorization_failures:
                failures.extend(
                    f"invalid bootstrap-owned disposition for {sha}: {failure}"
                    for failure in authorization_failures
                )
                disposition = None

        if matches and unowned_paths:
            classification = "interleaved"
            reason = ""
            failures.append(
                f"interleaved story commit {sha} matches story {metadata.story_id} but touches unowned paths: "
                + ", ".join(format_git_path(path) for path in unowned_paths)
            )
        elif matches:
            classification = "owned"
            reason = ""
        elif disposition:
            classification = disposition[0]
            reason = disposition[1]
        elif owned_paths:
            classification = "unmapped"
            reason = ""
            failures.append(
                f"unmapped story delivery commit {sha} does not match story {metadata.story_id} but touches listed paths: "
                + ", ".join(format_git_path(path) for path in owned_paths)
            )
        else:
            classification = "unrelated"
            reason = ""

        commits.append(
            CommitEvidence(
                sha=sha,
                subject=subject,
                paths=paths,
                story_id_matches=matches,
                classification=classification,
                disposition_reason=reason,
            )
        )

    try:
        workspace = collect_workspace_evidence(root)
        workspace_snapshot_collected = True
    except RuntimeError as exc:
        failures.append(str(exc))
        workspace = WorkspaceEvidence([], [], [], [])
        workspace_snapshot_collected = False

    try:
        candidate_after_collection = canonical_commit(root, candidate_ref, "candidate after evidence collection")
        if candidate_after_collection != candidate:
            failures.append(
                f"candidate ref moved during validation: {candidate} -> {candidate_after_collection}"
            )
    except RuntimeError as exc:
        failures.append(str(exc))

    try:
        workspace_after_collection = collect_workspace_evidence(root)
        if workspace_snapshot_collected and workspace_after_collection != workspace:
            failures.append(
                "workspace state changed during validation: "
                + format_workspace_snapshot_change(workspace, workspace_after_collection)
            )
    except RuntimeError as exc:
        failures.append(str(exc))

    return (
        CommitScopeEvidence(
            story_id=metadata.story_id,
            baseline=baseline,
            candidate=candidate,
            commits=commits,
            merges=merges,
            workspace=workspace,
        ),
        failures,
    )


def collect_workspace_evidence(root: Path) -> WorkspaceEvidence:
    result = run_git_checked_bytes(
        root,
        [
            "git",
            "-c",
            "status.renames=false",
            "status",
            "--porcelain=v1",
            "-z",
            "--untracked-files=all",
        ],
        "collecting workspace evidence",
    )
    staged: set[str] = set()
    unstaged: set[str] = set()
    untracked: set[str] = set()
    unresolved: set[str] = set()
    unmerged_states = {"DD", "AU", "UD", "UA", "DU", "AA", "UU"}
    for record in decode_nul_paths(result.stdout):
        if len(record) < 3 or record[2] != " ":
            raise RuntimeError(f"git failure while parsing workspace evidence: malformed status row {record!r}")
        state = record[:2]
        path = record[3:]
        if state == "??":
            untracked.add(path)
            continue
        if state in unmerged_states or "U" in state:
            unresolved.add(path)
            continue
        if state[0] not in {" ", "?"}:
            staged.add(path)
        if state[1] not in {" ", "?"}:
            unstaged.add(path)
    return WorkspaceEvidence(
        sorted(staged),
        sorted(unstaged),
        sorted(untracked),
        sorted(unresolved),
    )


def format_workspace_snapshot_change(
    before: WorkspaceEvidence,
    after: WorkspaceEvidence,
) -> str:
    changes: list[str] = []
    for label in ("staged", "unstaged", "untracked", "unresolved"):
        before_paths = getattr(before, label)
        after_paths = getattr(after, label)
        if before_paths == after_paths:
            continue
        before_text = ", ".join(format_git_path(path) for path in before_paths) or "(none)"
        after_text = ", ".join(format_git_path(path) for path in after_paths) or "(none)"
        changes.append(f"{label} [{before_text}] -> [{after_text}]")
    return "; ".join(changes)


def collect_reconciled_changed_files(
    root: Path,
    evidence: CommitScopeEvidence | None,
    file_list: dict[str, str],
    excludes: list[str],
    base: str | None,
) -> ChangedFiles:
    workspace = evidence.workspace if evidence is not None else collect_workspace_evidence(root)
    patterns = merged_excludes(excludes)
    changed = {
        path
        for path in (*workspace.staged, *workspace.unstaged, *workspace.untracked, *workspace.unresolved)
        if path and not is_excluded(path, patterns)
    }
    if evidence is not None:
        listed = set(file_list)
        changed.update(
            path
            for commit in evidence.commits
            if commit.classification in OWNERSHIP_CONTRIBUTING_CLASSIFICATIONS
            for path in commit.paths
            if path in listed
        )
    return ChangedFiles(sorted(changed), "story commits plus workspace", base or "")


def format_commit_scope_evidence(
    evidence: CommitScopeEvidence,
    file_list: dict[str, str],
    unrelated: dict[str, str],
) -> str:
    lines = [
        "Commit scope evidence:",
        f"  story-id: {evidence.story_id}",
        f"  baseline: {evidence.baseline}",
        f"  candidate: {evidence.candidate}",
        "  non-merge commits:",
    ]
    if not evidence.commits:
        lines.append("    - (none)")
    for commit in evidence.commits:
        match = "match" if commit.story_id_matches else "no-match"
        # `disposition=` is the frozen human-readable report key for a non-merge
        # classification. MergeEvidence uses `declared_disposition` internally so an
        # actual story declaration is not confused with this compatibility label.
        disposition = (
            f" | reason={format_report_value(commit.disposition_reason)}"
            if commit.disposition_reason
            else ""
        )
        lines.append(
            f"    - {commit.sha} | story-id={match} | disposition={commit.classification}{disposition} | "
            f"{format_report_value(commit.subject)}"
        )
        if not commit.paths:
            lines.append("      - (no paths)")
        for path in commit.paths:
            if path not in file_list:
                path_kind = "unowned"
            elif commit.classification in OWNERSHIP_CONTRIBUTING_CLASSIFICATIONS:
                path_kind = "owned"
            else:
                path_kind = "listed-unowned"
            lines.append(f"      - {path_kind} | {format_git_path(path)}")

    lines.append("  merges:")
    if not evidence.merges:
        lines.append("    - (none)")
    for merge in evidence.merges:
        disposition = ""
        if merge.declared_disposition:
            disposition = (
                f" | disposition={merge.declared_disposition} | "
                f"reason={format_report_value(merge.disposition_reason)}"
            )
        lines.append(
            f"    - {merge.sha}{disposition} | {format_report_value(merge.subject)}"
        )

    lines.append("  workspace:")
    for label, paths in (
        ("staged", evidence.workspace.staged),
        ("unstaged", evidence.workspace.unstaged),
        ("untracked", evidence.workspace.untracked),
        ("unresolved", evidence.workspace.unresolved),
    ):
        lines.append(f"    {label}:")
        if not paths:
            lines.append("      - (none)")
        for path in paths:
            reason = classified_path_reason(path, unrelated)
            suffix = (
                f" | documented-unrelated={format_report_value(reason)}"
                if reason
                else ""
            )
            lines.append(f"      - {format_git_path(path)}{suffix}")

    lines.append("    documented-unrelated:")
    if not unrelated:
        lines.append("      - (none)")
    workspace_states = {
        "staged": evidence.workspace.staged,
        "unstaged": evidence.workspace.unstaged,
        "untracked": evidence.workspace.untracked,
        "unresolved": evidence.workspace.unresolved,
    }
    for entry, reason in sorted(unrelated.items()):
        states = [
            label
            for label, paths in workspace_states.items()
            if any(path == entry.rstrip("/") or path.startswith(entry.rstrip("/") + "/") for path in paths)
        ]
        state = ",".join(states) if states else "declared"
        lines.append(
            f"      - {format_git_path(entry)} | state={state} | "
            f"reason={format_report_value(reason)}"
        )
    return "\n".join(lines)


def format_git_path(path: str) -> str:
    if contains_terminal_control(path):
        return format_report_value(path)
    if path != path.strip() or any(character in path for character in ('"', "\\", "\n", "\r", "\t")):
        return json.dumps(path, ensure_ascii=False)
    return path


def format_report_value(value: str) -> str:
    """Quote values containing terminal or format controls while preserving ordinary output."""
    if contains_terminal_control(value):
        return json.dumps(value, ensure_ascii=True)
    return value


def contains_terminal_control(value: str) -> bool:
    return any(
        character == "|"
        or ord(character) < 0x20
        or 0x7F <= ord(character) <= 0x9F
        or unicodedata.category(character) in {"Cf", "Zl", "Zp"}
        for character in value
    )


def classified_path_reason(path: str, classified: dict[str, str]) -> str:
    for entry, reason in classified.items():
        normalized = entry.rstrip("/")
        if path == normalized or path.startswith(normalized + "/"):
            return reason
    return ""


def check_file_list(
    root: Path,
    story: Path,
    changed_files: ChangedFiles,
    listed: dict[str, str],
    unrelated: dict[str, str],
) -> list[str]:
    classified_paths = set(unrelated)
    if not listed:
        base_note = f" (baseline_commit {changed_files.base})" if changed_files.base else ""
        changed_note = ""
        if changed_files.files:
            changed_note = "\n" + "\n".join(
                f"  - {format_git_path(path)} "
                f"(reason: story-owned change from {changed_files.source} cannot be reconciled)"
                for path in changed_files.files
                if not path_is_classified_unrelated(path, classified_paths)
            )
        return [
            "story File List not found or empty; changed files missing from story File List in "
            f"{story.relative_to(root).as_posix()}{base_note}{changed_note}"
        ]
    changed_set = set(changed_files.files)
    missing = [
        path
        for path in changed_files.files
        if path not in listed and not path_is_classified_unrelated(path, classified_paths)
    ]
    extra = [
        path
        for path, reason in listed.items()
        if path not in changed_set and not is_accepted_extra_reason(reason)
    ]
    failures: list[str] = []
    if missing:
        base_note = f" (baseline_commit {changed_files.base})" if changed_files.base else ""
        failures.append(
            "changed files missing from story File List in "
            f"{story.relative_to(root).as_posix()}{base_note}:\n"
            + "\n".join(
                f"  - {format_git_path(path)} "
                f"(reason: story-owned change from {changed_files.source} is not documented)"
                for path in missing
            )
        )
    if extra:
        failures.append(
            "File List entries with no matching story-owned change in "
            f"{story.relative_to(root).as_posix()}:\n"
            + "\n".join(
                f"  - {format_git_path(path)} "
                "(reason: no matching story-owned change and no accepted classification)"
                for path in extra
            )
        )
    return failures


def extract_story_file_list(body: str) -> dict[str, str]:
    entries: dict[str, str] = {}
    for line in body.split("\n"):
        line = line.rstrip("\r")
        stripped = line.strip()
        if not stripped.startswith("-"):
            continue
        entry = extract_file_list_entry(stripped)
        if entry:
            entries[entry] = extract_reason(stripped)
    return entries


def extract_file_list_entry(line: str) -> str:
    backtick = re.search(r"`([^`]+)`", line)
    if backtick:
        candidate = backtick.group(1)
    else:
        candidate = line.lstrip("-").strip().split(" ", 1)[0]
    # rstrip only: leading dots are significant for dotfile/dotdir paths (.agents, .github, …).
    candidate = candidate.strip().rstrip(".,;:")
    if not candidate or "..." in candidate or "{" in candidate:
        return ""
    return candidate.replace("\\", "/")


def extract_reason(line: str, *, default: str = "") -> str:
    backtick = re.search(r"`([^`]+)`", line)
    if backtick:
        reason = line[backtick.end() :].lstrip(" -:").strip()
    else:
        parts = line.lstrip("-").strip().split(" ", 1)
        reason = parts[1].strip() if len(parts) > 1 else ""
    return reason or default


def is_accepted_extra_reason(reason: str) -> bool:
    lowered = reason.lower()
    return any(keyword in lowered for keyword in ACCEPTED_EXTRA_REASONS)


def extract_checked_tasks(
    text: str,
) -> tuple[list[tuple[int, str]], list[str], list[str]]:
    in_tasks = False
    task_heading_level = 0
    excluded_subsection_level: int | None = None
    outside_review_level: int | None = None
    recognized_heading_count = 0
    checked_task_count = 0
    tasks: list[tuple[int, str]] = []
    notices: list[str] = []
    for line_number, line in markdown_lines_outside_frontmatter_and_fences(text):
        stripped = line.strip()
        checked = CHECKED_TASK.match(line)
        heading = MARKDOWN_HEADING.match(stripped)
        heading_text = heading.group(2).strip().lower() if heading else ""
        if heading and heading_text in CHECKED_TASK_HEADINGS:
            in_tasks = True
            task_heading_level = len(heading.group(1))
            excluded_subsection_level = None
            outside_review_level = None
            recognized_heading_count += 1
            continue
        if heading:
            heading_level = len(heading.group(1))
            if in_tasks and heading_level <= task_heading_level:
                in_tasks = False
                excluded_subsection_level = None
            if in_tasks and (
                excluded_subsection_level is not None
                and heading_level <= excluded_subsection_level
            ):
                excluded_subsection_level = None
            if in_tasks and any(
                heading_text.startswith(excluded)
                for excluded in EXCLUDED_TASK_SUBSECTION_HEADINGS
            ):
                excluded_subsection_level = heading_level
            if not in_tasks:
                if outside_review_level is not None and heading_level <= outside_review_level:
                    outside_review_level = None
                if any(
                    heading_text.startswith(excluded)
                    for excluded in EXCLUDED_TASK_SUBSECTION_HEADINGS
                ):
                    outside_review_level = heading_level
            continue
        if not checked:
            continue
        if excluded_subsection_level is not None or outside_review_level is not None:
            continue
        checked_task_count += 1
        if in_tasks and excluded_subsection_level is None:
            tasks.append((line_number, checked.group(1).strip()))
        else:
            notices.append(
                "checked item outside recognized task section at line "
                f"{line_number}: {checked.group(1).strip()}"
            )
    failures: list[str] = []
    if checked_task_count and not recognized_heading_count:
        failures.append(
            "checked tasks found but no recognized task heading matched; expected "
            "a Markdown heading named 'Tasks', 'Tasks / Subtasks', or 'Tasks & Acceptance'"
        )
    return tasks, failures, notices


def check_checked_tasks(
    root: Path,
    story: Path,
    changed_files: list[str],
    metadata: StoryMetadata,
    unrelated: dict[str, str] | None = None,
) -> list[str]:
    failures: list[str] = []
    changed = set(changed_files)
    listed = set(metadata.file_list)
    # A path the story explicitly classified as unrelated workspace state is accounted
    # for — it is declared, reviewable, and asserted not to be story output. Citing one
    # in review prose is therefore evidenced, exactly as a File List row would be. This
    # consults an existing explicit classification; it does not infer exemption from prose.
    # The classification is bounded so it cannot become a blanket exemption: see
    # usable_classified_paths.
    classified_unrelated = usable_classified_paths(root, unrelated or {}, changed)
    evidence_basenames = {
        path.rsplit("/", 1)[-1] for path in (changed | listed | classified_unrelated)
    }
    blocker_text = "\n".join(metadata.blockers.values()).lower()
    evidence_text = metadata.evidence_text.lower()
    for line_number, task in metadata.checked_tasks:
        if not task_needs_evidence(task):
            continue
        task_paths = extract_path_mentions(task, root=root)
        if task_is_classified_defer(task):
            # Deferred pre-existing work is intentionally absent from the changed set and
            # File List, so it is exempt from output-path evidence reconciliation. But a
            # deferral must still cite a real pre-existing location — verify each fully
            # qualified path exists so a task cannot self-exempt by naming a fabricated one.
            nonexistent_paths = sorted(
                path
                for path in task_paths
                if "/" in path and not (root / path).exists()
            )
            if nonexistent_paths:
                failures.append(
                    "deferred review task cites nonexistent path in "
                    f"{story.relative_to(root).as_posix()}:{line_number}: {task}"
                    f"\n  - deferred path does not exist: {', '.join(nonexistent_paths)}"
                )
            continue
        missing_paths = sorted(
            path
            for path in task_paths
            if path not in changed
            and path not in listed
            and not path_is_classified_unrelated(path, classified_unrelated)
            # A bare basename (no directory) is evidenced when a changed/listed path shares it,
            # so "`checklist.md`" shorthand beside a full path is not flagged as brittle overreach.
            and not ("/" not in path and path in evidence_basenames)
        )
        has_path_evidence = bool(task_paths) and not missing_paths
        has_general_evidence = bool(changed or metadata.file_list or metadata.evidence_text.strip())
        has_blocker = "blocker" in task.lower() or "blocked" in task.lower() or "blocker" in evidence_text or blocker_text
        if missing_paths or not (has_path_evidence or has_general_evidence or has_blocker):
            failures.append(
                "checked task lacks evidence in "
                f"{story.relative_to(root).as_posix()}:{line_number}: {task}"
                + (f"\n  - missing evidence path: {', '.join(missing_paths)}" if missing_paths else "")
            )
    return failures


def task_needs_evidence(task: str) -> bool:
    lowered = task.lower()
    return any(keyword in lowered for keyword in TASK_EVIDENCE_KEYWORDS)


def task_is_classified_defer(task: str) -> bool:
    lowered = task.lower()
    return (
        lowered.startswith("[review][defer]")
        and "deferred" in lowered
        and ("pre-existing" in lowered or "preexisting" in lowered)
    )


def extract_path_mentions(text: str, *, root: Path | None = None) -> set[str]:
    paths: set[str] = set()
    for match in re.finditer(r"`([^`]+)`", text):
        if path_mention_is_explicitly_non_evidence(text, match.start(), match.end()):
            continue
        normalized = match.group(1).strip().replace("\\", "/")
        if " " in normalized or normalized.startswith("--") or normalized.startswith("<"):
            continue
        if mention_is_not_an_output_path(
            normalized,
            root,
            creation_claimed=mention_claims_creation(text, match.start()),
        ):
            continue
        if any(token in normalized for token in ("*", "?")):
            continue
        if "/" in normalized:
            paths.add(normalized)
            continue
        if normalized.startswith("."):
            if normalized.lower() not in TASK_PATH_SUFFIXES:
                paths.add(normalized)
            continue
        if Path(normalized).suffix.lower() in TASK_PATH_SUFFIXES:
            paths.add(normalized)
    return paths


def usable_classified_paths(root: Path, classified: dict[str, str], changed: set[str]) -> set[str]:
    """Bound the classification so it accounts for a path without exempting the tree.

    Two bounds, because this set grants evidence and the story author writes it.
    A top-level directory (`src`, `tests`) covers so much of the repository that one
    bullet would exempt every path beneath it, so it is refused; a classification must
    name a file, or a directory at least one level down such as
    `references/Hexalith.Builds`. And the entry must be real — tracked in the repository
    or present in the changed set — so a story cannot account for a fabricated path by
    inventing a bullet for it. Trailing slashes are stripped: a directory written the
    natural way (`references/Hexalith.Builds/`) must cover the same paths as without.
    """
    usable: set[str] = set()
    tracked = tracked_files(root)
    for entry in classified:
        path = entry.strip().rstrip("/")
        if not path or path == ".":
            continue
        # Bare top-level names that cover nested paths are refused. Do not depend on
        # working-tree is_dir() alone: a deleted checkout of `src` while `src/...` remains
        # tracked must still refuse a bare `src` classification.
        if "/" not in path:
            covers_nested = tracked is not None and path not in tracked and any(
                tracked_path.startswith(path + "/") for tracked_path in tracked
            )
            if covers_nested or (root / path).is_dir():
                continue
            # Without a tracked listing we cannot prove a bare name is not a nested-covering
            # directory that vanished from the worktree; refuse unless it is a real file.
            if tracked is None and not (root / path).is_file():
                continue
        if (
            path in changed
            or any(candidate.startswith(path + "/") for candidate in changed)
            or path_is_tracked(path, root)
        ):
            usable.add(path)
    return usable


def path_is_classified_unrelated(path: str, classified: set[str]) -> bool:
    """A classified directory or submodule covers the paths beneath it.

    Classifying `references/Hexalith.Builds` as unrelated workspace state accounts for
    `references/Hexalith.Builds/Props/Directory.Packages.props` too; requiring every
    nested file to be listed separately would push authors toward blanket exemptions.
    Matching is on full path segments, so `references/Hexalith.BuildsExtra` is not
    covered by a `references/Hexalith.Builds` classification.
    """
    entries = {entry.rstrip("/") for entry in classified}
    if path in entries:
        return True
    return any(path.startswith(entry + "/") for entry in entries)


def mention_is_not_an_output_path(
    normalized: str,
    root: Path | None,
    *,
    creation_claimed: bool = False,
) -> bool:
    """Reject backticked tokens that cannot denote an output path this story produced.

    Review-follow-up prose legitimately cites code the story did not change: bare suffix
    literals, `path:line` coordinates, directories, method tokens, and hypothetical
    filenames used to describe a scenario. Each class is rejected on its own shape.

    The boundary, stated exactly: a *qualified* repository-relative path is never
    exempted. A *bare basename* is exempted when it matches nothing tracked in the tree,
    because that shape is how prose names a hypothetical ("the idiomatic `Foo.cs` +
    `Foo.Handlers.cs` split"). That exemption does not apply when a creation verb governs
    the token — a file the story claims to have created is absent from `git ls-files` by
    construction, so exempting it would make every phantom new-file claim unenforceable.

    Do NOT replace this with a surrounding-prose heuristic for the other classes: an
    action-verb probe was tried and silently exempted past-tense claims ("Updated
    `src/x.cs`"), non-adjacent objects ("update the file `src/x.cs`"), and every path
    after the first in a coordinated list, which is precisely the phantom-fix class this
    gate exists to catch. `creation_claimed` is deliberately the inverse: it only ever
    makes the gate stricter, so a false positive costs a demanded path, not a missed one.
    """
    # NOTE: bare suffix chains (".g.cs", ".AssemblyInfo.cs") need no rule of their own —
    # they carry no directory and match no tracked basename, so the tree-absent-basename
    # rule below rejects them. A dedicated suffix rule was written first and removed: it
    # could not be made load-bearing (deleting it left the suite green), and shipping a
    # guard no test can fail is the defect this review was closing.
    # A "path:line", "path:line-line", or "path:line:column" coordinate cites a location,
    # not an output. The trailing digit run is what makes it a coordinate: a token ending
    # in ":abc" is not one, and stays strict.
    if PATH_COORDINATE.search(normalized):
        return True
    # A method or invocation token (".First()", "Foo.Bar()") is code, not a path.
    if "(" in normalized or ")" in normalized:
        return True
    # An ellipsis-elided citation is not resolvable, wherever the elision falls
    # ("…/CommandAuthorizationResource.cs", "src/.../Widget.cs"). extract_file_list_entry
    # rejects the same notation anywhere in the token; the two parsers must agree.
    if "..." in normalized or "…" in normalized:
        return True
    if root is not None:
        # A directory names a scan scope, not a produced file.
        if (root / normalized).is_dir():
            return True
        # A bare basename that matches nothing in the tree is a hypothetical used to
        # describe a scenario ("the idiomatic `Foo.cs` + `Foo.Handlers.cs` split").
        # Qualified paths keep full strictness, so a fabricated `src/NewThing.cs` claim
        # is still reported — and so is a bare basename a creation verb governs.
        if (
            "/" not in normalized
            and not creation_claimed
            and not basename_exists_in_tree(normalized, root)
        ):
            return True
    return False


@lru_cache(maxsize=1)
def tracked_files(root: Path) -> frozenset[str] | None:
    """Every tracked path, or None when the tree cannot be listed.

    None is distinct from "no files": a missing git binary, a non-repository root, or a
    failed invocation must not read as "nothing is tracked", because every caller treats
    absence as grounds to relax. Callers fail closed on None instead.
    """
    try:
        result = subprocess.run(
            ["git", "-C", str(root), "ls-files"],
            capture_output=True,
            text=True,
            check=False,
        )
    except (FileNotFoundError, OSError):
        return None
    if result.returncode != 0:
        return None
    return frozenset(line for line in result.stdout.splitlines() if line)


def tracked_basenames(root: Path) -> frozenset[str] | None:
    paths = tracked_files(root)
    if paths is None:
        return None
    return frozenset(path.rsplit("/", 1)[-1] for path in paths)


def basename_exists_in_tree(basename: str, root: Path) -> bool:
    names = tracked_basenames(root)
    # Fail closed: an unlistable tree cannot prove the basename is hypothetical, so the
    # token keeps full strictness rather than being exempted wholesale.
    if names is None:
        return True
    return basename in names


def path_is_tracked(path: str, root: Path) -> bool:
    """True when the path is a tracked file, or a directory containing tracked files."""
    paths = tracked_files(root)
    # Fail closed in the other direction: without a listing no classification can be
    # shown to be real, so none is granted evidence.
    if paths is None:
        return False
    if path in paths:
        return True
    prefix = path + "/"
    return any(tracked.startswith(prefix) for tracked in paths)


def path_mention_is_explicitly_non_evidence(text: str, start: int, end: int) -> bool:
    clause_start = 0
    for boundary in re.finditer(r"[.!?;]\s+", text[:start]):
        clause_start = boundary.end()

    clause_end = len(text)
    boundary = re.search(r"[.!?;](?:\s+|$)", text[end:])
    if boundary:
        clause_end = end + boundary.start()

    before = text[clause_start:start]
    after = text[end:clause_end]
    # Both probes derive from ACTION_VERBS, so the two vocabularies cannot drift apart.
    if NEGATED_ACTION.search(before):
        return True

    return bool(
        re.search(r"\b(?:leave|keep|preserve)\b", before, re.IGNORECASE)
        and re.search(r"\b(?:untouched|unchanged|unmodified)\b", after, re.IGNORECASE)
        # ...but not when a positive action verb directly governs the path
        # (e.g. "Keep behavior and update `x` so output stays unchanged"), which makes
        # the path that verb's object — genuine evidence, not a preservation target.
        and not POSITIVE_ACTION.search(before)
    )


def mention_claims_creation(text: str, start: int) -> bool:
    """True when a creation verb governs the token's clause.

    Scoped to the clause, and matched anywhere within it rather than adjacent to the
    token, so "add a new file `Foo.cs`" counts. Breadth is safe here because the only
    effect is to withhold the tree-absent-basename exemption, which makes the gate
    stricter.
    """
    clause_start = 0
    for boundary in re.finditer(r"[.!?;]\s+", text[:start]):
        clause_start = boundary.end()
    return bool(CREATION_ACTION.search(text[clause_start:start]))


if __name__ == "__main__":
    sys.exit(main())
