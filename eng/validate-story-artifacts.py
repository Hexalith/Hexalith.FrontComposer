#!/usr/bin/env python3
"""Validate BMAD story artifact hygiene."""

from __future__ import annotations

import argparse
import fnmatch
import re
import subprocess
import sys
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
CHECKED_TASK = re.compile(r"^\s*-\s*\[x\]\s*(.+)$", re.IGNORECASE)
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


@dataclass(frozen=True)
class StoryMetadata:
    baseline_commit: str
    file_list: dict[str, str]
    unrelated: dict[str, str]
    blockers: dict[str, str]
    checked_tasks: list[tuple[int, str]]
    evidence_text: str


@dataclass(frozen=True)
class ChangedFiles:
    files: list[str]
    source: str
    base: str


def main() -> int:
    args = parse_args()
    root = Path(args.project_root).resolve()
    failures: list[str] = []
    notices: list[str] = []

    if not args.skip_sentinel:
        failures.extend(scan_sentinels(root, args.sentinel_root, args.exclude))

    if args.story:
        story = resolve_under_root(root, args.story)
        metadata = parse_story_metadata(story)
        base = args.base or metadata.baseline_commit
        changed_files = collect_changed_files(root, base, args.changed_file, args.exclude)
        cli_unrelated = parse_cli_unrelated(root, args.unrelated, args.reason)
        unrelated = {**metadata.unrelated, **cli_unrelated}
        failures.extend(check_file_list(root, story, changed_files, metadata.file_list, unrelated))
        failures.extend(check_checked_tasks(root, story, changed_files.files, metadata, unrelated))
        unrelated_changed = [path for path in changed_files.files if path in unrelated]
        if unrelated_changed:
            notices.append(
                "unrelated dirty files documented for "
                f"{story.relative_to(root).as_posix()}:\n"
                + "\n".join(f"  - {path}: {unrelated[path]}" for path in unrelated_changed)
            )

    if failures:
        for failure in failures:
            print(failure, file=sys.stderr)
        return 1

    for notice in notices:
        print(notice)
    print("Story artifact validation passed.")
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", default=".", help="Repository root. Defaults to current directory.")
    parser.add_argument("--story", help="Story markdown file whose File List should be checked.")
    parser.add_argument("--base", help="Optional git base ref for changed-file discovery.")
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
    frontmatter = extract_frontmatter(text)
    sections = extract_sections(text)
    file_list = extract_story_file_list(sections.get("file list", ""))
    unrelated = extract_classified_paths(sections, DOCUMENTED_UNRELATED_HEADINGS)
    blockers = extract_classified_paths(sections, DOCUMENTED_BLOCKER_HEADINGS)
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
        baseline_commit=frontmatter.get("baseline_commit", ""),
        file_list=file_list,
        unrelated=unrelated,
        blockers=blockers,
        checked_tasks=extract_checked_tasks(text),
        evidence_text=evidence_text,
    )


def extract_frontmatter(text: str) -> dict[str, str]:
    if not text.startswith("---"):
        return {}
    parts = text.split("---", 2)
    if len(parts) < 3:
        return {}
    values: dict[str, str] = {}
    for line in parts[1].splitlines():
        match = FRONTMATTER_LINE.match(line.strip())
        if match:
            values[match.group(1).strip()] = match.group(2).strip().strip("'\"")
    return values


def extract_sections(text: str) -> dict[str, str]:
    sections: dict[str, list[str]] = {}
    current = ""
    for line in text.splitlines():
        heading = re.match(r"^(#{2,6})\s+(.+?)\s*$", line)
        if heading:
            current = heading.group(2).strip().lower()
            sections.setdefault(current, [])
            continue
        if current:
            sections.setdefault(current, []).append(line)
    return {key: "\n".join(value) for key, value in sections.items()}


def extract_classified_paths(sections: dict[str, str], headings: set[str]) -> dict[str, str]:
    classified: dict[str, str] = {}
    for heading in headings:
        body = sections.get(heading, "")
        for line in body.splitlines():
            stripped = line.strip()
            if not stripped.startswith("-"):
                continue
            path = extract_file_list_entry(stripped)
            if not path:
                continue
            classified[path] = extract_reason(stripped, default="documented exception")
    return classified


def parse_cli_unrelated(root: Path, paths: list[str], reasons: list[str]) -> dict[str, str]:
    unrelated: dict[str, str] = {}
    for index, path in enumerate(paths):
        normalized = normalize_changed_path(root, path)
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
    if base:
        diff_args.append(base)
    changed.update(run_git_lines(root, diff_args))
    changed.update(run_git_lines(root, ["git", "diff", "--cached", "--name-only", "--diff-filter=ACMRTD"]))
    changed.update(run_git_lines(root, ["git", "ls-files", "--others", "--exclude-standard"]))
    return ChangedFiles(sorted(path for path in changed if path and not is_excluded(path, patterns)), "git", base or "")


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


def check_file_list(
    root: Path,
    story: Path,
    changed_files: ChangedFiles,
    listed: dict[str, str],
    unrelated: dict[str, str],
) -> list[str]:
    if not listed:
        base_note = f" (baseline_commit {changed_files.base})" if changed_files.base else ""
        changed_note = ""
        if changed_files.files:
            changed_note = "\n" + "\n".join(
                f"  - {path} (reason: story-owned change from {changed_files.source} cannot be reconciled)"
                for path in changed_files.files
                if path not in unrelated
            )
        return [
            "story File List not found or empty; changed files missing from story File List in "
            f"{story.relative_to(root).as_posix()}{base_note}{changed_note}"
        ]
    changed_set = set(changed_files.files)
    missing = [path for path in changed_files.files if path not in listed and path not in unrelated]
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
                f"  - {path} (reason: story-owned change from {changed_files.source} is not documented)"
                for path in missing
            )
        )
    if extra:
        failures.append(
            "File List entries with no matching story-owned change in "
            f"{story.relative_to(root).as_posix()}:\n"
            + "\n".join(
                f"  - {path} (reason: no matching story-owned change and no accepted classification)"
                for path in extra
            )
        )
    return failures


def extract_story_file_list(body: str) -> dict[str, str]:
    entries: dict[str, str] = {}
    for line in body.splitlines():
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


def extract_checked_tasks(text: str) -> list[tuple[int, str]]:
    lines = text.splitlines()
    in_tasks = False
    tasks: list[tuple[int, str]] = []
    for line_number, line in enumerate(lines, start=1):
        stripped = line.strip()
        if stripped.lower() == "## tasks / subtasks":
            in_tasks = True
            continue
        if in_tasks and stripped.startswith("## "):
            break
        if not in_tasks:
            continue
        checked = CHECKED_TASK.match(line)
        if checked:
            tasks.append((line_number, checked.group(1).strip()))
    return tasks


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
        if path in changed or path_is_tracked(path, root):
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
