#!/usr/bin/env python3
"""Validate BMAD story artifact hygiene."""

from __future__ import annotations

import argparse
import fnmatch
import re
import subprocess
import sys
from dataclasses import dataclass
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
        failures.extend(check_checked_tasks(root, story, changed_files.files, metadata))
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


def check_checked_tasks(root: Path, story: Path, changed_files: list[str], metadata: StoryMetadata) -> list[str]:
    failures: list[str] = []
    changed = set(changed_files)
    listed = set(metadata.file_list)
    evidence_basenames = {path.rsplit("/", 1)[-1] for path in (changed | listed)}
    blocker_text = "\n".join(metadata.blockers.values()).lower()
    evidence_text = metadata.evidence_text.lower()
    for line_number, task in metadata.checked_tasks:
        if not task_needs_evidence(task):
            continue
        if task_is_classified_defer(task):
            continue
        task_paths = extract_path_mentions(task)
        missing_paths = sorted(
            path
            for path in task_paths
            if path not in changed
            and path not in listed
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


def extract_path_mentions(text: str) -> set[str]:
    paths: set[str] = set()
    for candidate in re.findall(r"`([^`]+)`", text):
        normalized = candidate.strip().replace("\\", "/")
        if " " in normalized or normalized.startswith("--") or normalized.startswith("<"):
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


if __name__ == "__main__":
    sys.exit(main())
