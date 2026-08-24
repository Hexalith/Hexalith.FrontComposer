from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
import tempfile
import textwrap
import unittest
from dataclasses import replace
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
VALIDATOR = REPO_ROOT / "eng" / "validate-story-artifacts.py"
STORY_AUTOMATOR_SRC = REPO_ROOT / ".agents" / "skills" / "bmad-story-automator" / "src"


def run(command: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, cwd=cwd, check=False, capture_output=True, text=True)


def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(textwrap.dedent(text).lstrip(), encoding="utf-8")


def git(cwd: Path, *args: str) -> subprocess.CompletedProcess[str]:
    return run(["git", *args], cwd)


def init_repo(root: Path) -> str:
    git(root, "init")
    git(root, "config", "user.email", "test@example.invalid")
    git(root, "config", "user.name", "Story Validator Test")
    write(root / "README.md", "initial\n")
    git(root, "add", "README.md")
    git(root, "commit", "-m", "initial")
    return git(root, "rev-parse", "HEAD").stdout.strip()


def commit_files(root: Path, subject: str, files: dict[str, str]) -> str:
    for path, content in files.items():
        write(root / path, content)
    git(root, "add", *files)
    result = git(root, "commit", "-m", subject)
    if result.returncode != 0:
        raise AssertionError(result.stdout + result.stderr)
    return git(root, "rev-parse", "HEAD").stdout.strip()


def story_text(
    *,
    baseline: str,
    file_list: str = "",
    tasks: str = "- [ ] Pending task",
    story_id: str = "1.1",
) -> str:
    return (
        f"---\n"
        f"baseline_commit: {baseline}\n"
        f"story_id: '{story_id}'\n"
        f"---\n\n"
        "# Story 1.1: Validator fixture\n\n"
        "Status: review\n\n"
        "## Tasks / Subtasks\n\n"
        f"{tasks}\n\n"
        "## Dev Agent Record\n\n"
        "### Completion Notes List\n\n"
        "- Test fixture completion notes.\n\n"
        "### File List\n\n"
        f"{file_list}\n"
    )


class StoryArtifactValidatorTests(unittest.TestCase):
    def test_raw_tool_call_tag_line_fails_in_bmad_test_artifact(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write(
                root / "_bmad-output/implementation-artifacts/tests/test-summary.md",
                """
                # Test Summary

                <tool_call name="functions.exec_command">
                """,
            )

            result = run([sys.executable, str(VALIDATOR), "--project-root", str(root)], root)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("raw authoring sentinel", result.stderr)
            self.assertIn("_bmad-output/implementation-artifacts/tests/test-summary.md", result.stderr)
            self.assertIn('<tool_call name="functions.exec_command">', result.stderr)

    def test_raw_tool_call_tag_line_with_backtick_attribute_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                """
                # Story 1.1: Validator fixture

                <tool_call name="`functions.exec_command`">
                """,
            )

            result = run([sys.executable, str(VALIDATOR), "--project-root", str(root)], root)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("raw authoring sentinel", result.stderr)
            self.assertIn('<tool_call name="`functions.exec_command`">', result.stderr)

    def test_quoted_tool_call_examples_are_allowed_in_bmad_artifacts(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                """
                # Story 1.1: Validator fixture

                > <tool_call name="functions.exec_command">

                Example inline code: `<tool_call name="functions.exec_command">`

                `<tool_call name="functions.exec_command">`

                ```markdown
                <tool_call name="functions.exec_command">
                ```
                """,
            )

            result = run([sys.executable, str(VALIDATOR), "--project-root", str(root)], root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("Story artifact validation passed.", result.stdout)

    def test_baseline_commit_from_frontmatter_detects_missing_file_list_entry(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md", story_text(baseline=baseline))
            write(root / "src/owned.txt", "owned\n")

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("baseline_commit", result.stderr)
            self.assertIn("src/owned.txt", result.stderr)
            self.assertIn("missing from story File List", result.stderr)

    def test_extra_file_list_entry_without_documented_exception_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(baseline=baseline, file_list="- `src/not-changed.txt`"),
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("src/not-changed.txt", result.stderr)
            self.assertIn("no matching story-owned change", result.stderr)

    def test_documented_unrelated_dirty_file_is_visible_but_not_required_in_file_list(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(
                    baseline=baseline,
                    file_list=(
                        "- `_bmad-output/implementation-artifacts/1-1-validator-fixture.md`\n"
                        "- `src/owned.txt`"
                    ),
                    tasks="- [x] Update `src/owned.txt` with implementation evidence.",
                )
                + "\n### Documented Unrelated Changes\n\n- `notes/unrelated.md` - pre-existing editor scratch.\n",
            )
            write(root / "src/owned.txt", "owned\n")
            write(root / "notes/unrelated.md", "unrelated\n")

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("unrelated dirty files", result.stdout)
            self.assertIn("notes/unrelated.md", result.stdout)

    def test_checked_task_without_evidence_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(
                    baseline=baseline,
                    file_list="- `README.md` - pre-existing documentation exception.",
                    tasks="- [x] Update `src/missing.txt` with implementation evidence.",
                ),
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("checked task lacks evidence", result.stderr)
            self.assertIn("src/missing.txt", result.stderr)

    def test_checked_task_under_tasks_and_acceptance_is_extracted_and_validated(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            content = story_text(
                baseline=baseline,
                file_list="- `README.md` - pre-existing documentation exception.",
                tasks="- [x] Update `src/missing.txt` with implementation evidence.",
            ).replace("## Tasks / Subtasks", "## Tasks & Acceptance")
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                content,
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("checked task lacks evidence", result.stderr)
            self.assertIn("missing evidence path: src/missing.txt", result.stderr)

    def test_checked_task_ignores_extension_and_assembly_name_tokens(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(
                    baseline=baseline,
                    file_list="- `README.md` - test evidence.",
                    tasks=(
                        "- [x] Parse production `.cs` files for direct declarations.\n"
                        "- [x] Run the `Hexalith.FrontComposer.Cli.Tests` suite."
                    ),
                ),
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--changed-file",
                    "README.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("Story artifact validation passed.", result.stdout)

    def test_checked_task_ignores_explicitly_non_evidence_path_mentions(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(
                    baseline=baseline,
                    file_list="- `README.md` - test evidence.",
                    tasks=(
                        "- [x] Run package validation. Do not require `.nupkg` byte identity.\n"
                        "- [x] Leave planning artifacts, "
                        "`_bmad-output/implementation-artifacts/deferred-work.md`, and review provenance untouched."
                    ),
                ),
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--changed-file",
                    "README.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("Story artifact validation passed.", result.stdout)

    def test_checked_task_non_evidence_path_does_not_hide_required_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(
                    baseline=baseline,
                    file_list="- `README.md` - test evidence.",
                    tasks="- [x] Update `src/missing.txt`; do not require `.nupkg` byte identity.",
                ),
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--changed-file",
                    "README.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("src/missing.txt", result.stderr)
            self.assertIn("missing evidence path: src/missing.txt", result.stderr)
            self.assertNotIn("missing evidence path: .nupkg", result.stderr)

    def test_checked_review_patch_ignores_descriptive_path_mentions(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(
                    baseline=baseline,
                    file_list="- `README.md` - test evidence.",
                    tasks=(
                        "- [x] [Review][Patch] `RuntimeKind` is also discussed at "
                        "`src/reference.cs:42`; `.First()`, `.g.cs`, and `Foo.cs` are examples. "
                        "Fix: keep the classifier strict."
                    ),
                ),
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--changed-file",
                    "README.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("Story artifact validation passed.", result.stdout)

    def _assert_review_patch_task_is_strict(self, task: str, *expected_paths: str) -> None:
        """A qualified output path in a checked review task must be reconciled regardless
        of the prose around it. Phrasing must never decide enforcement."""
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(
                    baseline=baseline,
                    file_list="- `README.md` - test evidence.",
                    tasks=task,
                ),
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--changed-file",
                    "README.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
            for expected in expected_paths:
                self.assertIn(expected, result.stderr)

    def test_checked_review_patch_keeps_action_governed_path_strict(self) -> None:
        self._assert_review_patch_task_is_strict(
            "- [x] [Review][Patch] Fix: update `src/missing.cs` with the strict classifier.",
            "missing evidence path: src/missing.cs",
        )

    def test_checked_review_patch_keeps_past_tense_claim_strict(self) -> None:
        self._assert_review_patch_task_is_strict(
            "- [x] [Review][Patch] Fix: updated `src/missing.cs` with the strict classifier.",
            "missing evidence path: src/missing.cs",
        )

    def test_checked_review_patch_keeps_non_adjacent_object_strict(self) -> None:
        self._assert_review_patch_task_is_strict(
            "- [x] [Review][Patch] Fix: update the file `src/missing.cs` in place.",
            "missing evidence path: src/missing.cs",
        )

    def test_checked_review_patch_keeps_every_path_in_a_list_strict(self) -> None:
        self._assert_review_patch_task_is_strict(
            "- [x] [Review][Patch] Fix: update `src/first.cs` and `src/second.cs` together.",
            "missing evidence path: src/first.cs, src/second.cs",
        )

    def test_checked_review_patch_keeps_qualified_hypothetical_path_strict(self) -> None:
        """Only a bare, tree-absent basename is treated as a scenario placeholder; a
        qualified path that resolves to nothing is still a phantom-fix claim."""
        self._assert_review_patch_task_is_strict(
            "- [x] [Review][Patch] Fix: add `src/Fabricated.cs` for the new seam.",
            "missing evidence path: src/Fabricated.cs",
        )

    def test_checked_review_patch_ignores_directory_and_negated_mentions(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(root / "src/scanned/keep.cs", "// scanned\n")
            subprocess.run(["git", "-C", str(root), "add", "-A"], check=True, capture_output=True)
            subprocess.run(
                ["git", "-C", str(root), "commit", "-m", "add scanned tree"],
                check=True,
                capture_output=True,
            )
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(
                    baseline=baseline,
                    file_list="- `README.md` - test evidence.",
                    tasks=(
                        "- [x] [Review][Patch] Fix: update the guard to scan `src/scanned` "
                        "recursively. This story did not move `src/scanned/keep.cs`."
                    ),
                ),
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--changed-file",
                    "README.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("Story artifact validation passed.", result.stdout)

    def test_checked_deferred_review_task_accepts_preexisting_path_classification(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(
                    baseline=baseline,
                    file_list="- `README.md` - test evidence.",
                    tasks=(
                        "- [x] [Review][Defer] Parse `.gitmodules` section and key names "
                        "case-insensitively - deferred, pre-existing."
                    ),
                ),
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--changed-file",
                    "README.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("Story artifact validation passed.", result.stdout)

    def test_checked_task_preserve_clause_does_not_hide_action_governed_path(self) -> None:
        # A preserve/unchanged clause must not exempt a path that a positive action verb
        # directly governs ("... update `src/ghost.cs` ..."); that path is real evidence.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(
                    baseline=baseline,
                    file_list="- `README.md` - test evidence.",
                    tasks="- [x] Keep behavior and update `src/ghost.cs` so output stays unchanged.",
                ),
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--changed-file",
                    "README.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("missing evidence path: src/ghost.cs", result.stderr)

    def test_checked_deferred_review_task_rejects_nonexistent_cited_path(self) -> None:
        # A deferred task is exempt from output-path evidence reconciliation, but it may not
        # self-exempt by citing a fabricated location: a qualified path must actually exist.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(
                    baseline=baseline,
                    file_list="- `README.md` - test evidence.",
                    tasks=(
                        "- [x] [Review][Defer] Update `src/ghost.cs` handling - "
                        "deferred, pre-existing."
                    ),
                ),
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--changed-file",
                    "README.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("deferred path does not exist: src/ghost.cs", result.stderr)

    def test_dotfile_file_list_entry_reconciles_without_stripping_leading_dot(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(
                    baseline=baseline,
                    file_list=(
                        "- `_bmad-output/implementation-artifacts/1-1-validator-fixture.md`\n"
                        "- `.agents/skills/example/gate.py`"
                    ),
                    tasks="- [x] Update `.agents/skills/example/gate.py` with the review-promotion gate.",
                ),
            )
            write(root / ".agents/skills/example/gate.py", "gate\n")

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            # The leading dot must survive parsing, so the entry matches the real dotdir change
            # instead of being mis-reported as both missing (.agents/...) and extra (agents/...).
            self.assertNotIn("agents/skills/example/gate.py (reason: no matching", result.stderr)

    def test_submodule_pointer_path_is_not_silently_ignored(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md", story_text(baseline=baseline))

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--changed-file",
                    "references/Hexalith.EventStore",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("references/Hexalith.EventStore", result.stderr)
            self.assertIn("missing from story File List", result.stderr)


class CommitScopeEvidenceTests(unittest.TestCase):
    story_path = "_bmad-output/implementation-artifacts/9-7-validator-fixture.md"

    def write_story(
        self,
        root: Path,
        baseline: str,
        paths: list[str],
        *,
        dispositions: str = "",
        unrelated: str = "",
    ) -> None:
        file_list = "\n".join(f"- `{path}`" for path in [self.story_path, *paths])
        content = story_text(
            baseline=baseline,
            file_list=file_list,
            story_id="9.7",
        )
        if dispositions:
            content += f"\n## Commit Scope Dispositions\n\n{dispositions}\n"
        if unrelated:
            content += f"\n### Documented Unrelated Workspace State\n\n{unrelated}\n"
        write(root / self.story_path, content)

    def validate(
        self,
        root: Path,
        candidate: str = "HEAD",
        *,
        base: str | None = None,
        changed_files: tuple[str, ...] = (),
    ) -> subprocess.CompletedProcess[str]:
        command = [
            sys.executable,
            str(VALIDATOR),
            "--project-root",
            str(root),
            "--story",
            self.story_path,
            "--candidate",
            candidate,
            "--skip-sentinel",
        ]
        if base is not None:
            command.extend(("--base", base))
        for path in changed_files:
            command.extend(("--changed-file", path))
        return run(command, root)

    def test_matching_story_commit_reports_full_sha_match_and_owned_paths(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            candidate = commit_files(root, "fix(9.7): add owned evidence", {"src/owned.txt": "owned\n"})
            self.write_story(root, baseline, ["src/owned.txt"])

            result = self.validate(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn(f"candidate: {candidate}", result.stdout)
            self.assertIn(f"{candidate} | story-id=match | disposition=owned", result.stdout)
            self.assertIn("owned | src/owned.txt", result.stdout)

    def test_dot_and_hyphen_story_ids_match_but_larger_story_id_does_not(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            dotted = commit_files(root, "fix(9.7): add dotted evidence", {"src/dotted.txt": "dotted\n"})
            hyphenated = commit_files(root, "test: cover story 9-7 evidence", {"src/hyphen.txt": "hyphen\n"})
            self.write_story(root, baseline, ["src/dotted.txt", "src/hyphen.txt"])

            result = self.validate(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn(f"{dotted} | story-id=match", result.stdout)
            self.assertIn(f"{hyphenated} | story-id=match", result.stdout)

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            candidate = commit_files(root, "fix(19.7): wrong story", {"src/owned.txt": "owned\n"})
            self.write_story(root, baseline, ["src/owned.txt"])

            result = self.validate(root)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn(f"{candidate} | story-id=no-match | disposition=unmapped", result.stdout)
            self.assertIn("listed-unowned | src/owned.txt", result.stdout)
            self.assertIn("unmapped story delivery commit", result.stderr)

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            padded = commit_files(root, "fix(9.70): wrong story", {"notes/padded.txt": "padded\n"})
            segmented = commit_files(root, "fix(9.7.1): wrong story", {"notes/segmented.txt": "segment\n"})
            prefixed = commit_files(root, "fix(x9.7): embedded story", {"notes/prefixed.txt": "prefix\n"})
            suffixed = commit_files(root, "fix(9.7x): embedded story", {"notes/suffixed.txt": "suffix\n"})
            owned = commit_files(root, "fix(9.7): owned story", {"src/owned.txt": "owned\n"})
            self.write_story(root, baseline, ["src/owned.txt"])

            result = self.validate(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            for sha in (padded, segmented, prefixed, suffixed):
                self.assertIn(f"{sha} | story-id=no-match | disposition=unrelated", result.stdout)
            self.assertIn(f"{owned} | story-id=match | disposition=owned", result.stdout)

    def test_missing_story_id_commit_touching_listed_path_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            candidate = commit_files(root, "fix: omit story id", {"src/owned.txt": "owned\n"})
            self.write_story(root, baseline, ["src/owned.txt"])

            result = self.validate(root)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn(f"{candidate} | story-id=no-match | disposition=unmapped", result.stdout)
            self.assertIn("src/owned.txt", result.stderr)

    def test_story_id_falls_back_to_legacy_story_filename(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            candidate = commit_files(root, "fix(9.7): add legacy evidence", {"src/owned.txt": "owned\n"})
            content = story_text(
                baseline=baseline,
                file_list=f"- `{self.story_path}`\n- `src/owned.txt`",
                story_id="9.7",
            ).replace("story_id: '9.7'\n", "").replace(
                "# Story 1.1: Validator fixture",
                "# Validator fixture",
            )
            write(root / self.story_path, content)

            result = self.validate(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn(f"{candidate} | story-id=match | disposition=owned", result.stdout)

    def test_in_range_shared_and_process_dispositions_exclude_commits_from_ownership(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            shared = commit_files(root, "build: shared fixture", {"src/shared.txt": "shared\n"})
            process = commit_files(root, "docs: process fixture", {"src/process.txt": "process\n"})
            owned = commit_files(
                root,
                "fix(9.7): own disposed paths",
                {"src/shared.txt": "shared owned\n", "src/process.txt": "process owned\n"},
            )
            self.write_story(
                root,
                baseline,
                ["src/shared.txt", "src/process.txt"],
                dispositions=(
                    f"- `{shared}` | `shared` | shared infrastructure update\n"
                    f"- `{process}` | `process` | review transition"
                ),
            )

            result = self.validate(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn(f"{shared} | story-id=no-match | disposition=shared", result.stdout)
            self.assertIn(f"{process} | story-id=no-match | disposition=process", result.stdout)
            shared_row = f"    - {shared} |"
            process_row = f"    - {process} |"
            owned_row = f"    - {owned} |"
            shared_report = result.stdout[
                result.stdout.index(shared_row) : result.stdout.index(process_row)
            ]
            process_report = result.stdout[
                result.stdout.index(process_row) : result.stdout.index(owned_row)
            ]
            owned_report = result.stdout[result.stdout.index(owned_row) :]
            self.assertIn("listed-unowned | src/shared.txt", shared_report)
            self.assertIn("listed-unowned | src/process.txt", process_report)
            self.assertNotIn("      - owned | src/shared.txt", shared_report)
            self.assertNotIn("      - owned | src/process.txt", process_report)
            self.assertIn("      - owned | src/shared.txt", owned_report)
            self.assertIn("      - owned | src/process.txt", owned_report)

    def test_malformed_stale_or_empty_dispositions_fail_closed(self) -> None:
        declarations = (
            "- `1234` | `shared` | short SHA",
            "- `1234` | `bootstrap-owned` | short SHA",
            f"- `{'0' * 40}` | `shared` | stale SHA",
            f"- `{'1' * 40}` | `other` | invalid kind",
            f"- `{'2' * 40}` | `process` |",
            f"- `{'3' * 40}` | `bootstrap-owned` |",
        )
        for declaration in declarations:
            with self.subTest(declaration=declaration), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                baseline = init_repo(root)
                commit_files(root, "fix(9.7): owned fixture", {"src/owned.txt": "owned\n"})
                self.write_story(root, baseline, ["src/owned.txt"], dispositions=declaration)

                result = self.validate(root)

                self.assertNotEqual(result.returncode, 0)
                self.assertTrue(
                    "malformed Commit Scope Dispositions declaration" in result.stderr
                    or "stale Commit Scope Dispositions declaration" in result.stderr,
                    result.stderr,
                )

    def test_duplicate_or_conflicting_dispositions_fail_closed(self) -> None:
        for second_kind in ("shared", "process"):
            with self.subTest(second_kind=second_kind), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                baseline = init_repo(root)
                disposed = commit_files(root, "build: disposed fixture", {"src/owned.txt": "first\n"})
                commit_files(root, "fix(9.7): own fixture", {"src/owned.txt": "owned\n"})
                self.write_story(
                    root,
                    baseline,
                    ["src/owned.txt"],
                    dispositions=(
                        f"- `{disposed}` | `shared` | first declaration\n"
                        f"- `{disposed}` | `{second_kind}` | duplicate declaration"
                    ),
                )

                result = self.validate(root)

                self.assertNotEqual(result.returncode, 0)
                self.assertIn("duplicate Commit Scope Dispositions declaration", result.stderr)

    def test_matching_commit_with_unowned_path_is_interleaved_even_when_path_is_unrelated(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            candidate = commit_files(
                root,
                "fix(9.7): interleave delivery",
                {"src/owned.txt": "owned\n", "notes/unrelated.txt": "unrelated\n"},
            )
            self.write_story(
                root,
                baseline,
                ["src/owned.txt"],
                unrelated="- `notes/unrelated.txt` - pre-existing unrelated work.",
            )

            result = self.validate(root)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn(f"{candidate} | story-id=match | disposition=interleaved", result.stdout)
            self.assertIn("unowned | notes/unrelated.txt", result.stdout)
            self.assertIn("interleaved story commit", result.stderr)

    def test_unrelated_commit_is_reported_without_becoming_file_list_ownership(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            owned = commit_files(root, "fix(9.7): add owned evidence", {"src/owned.txt": "owned\n"})
            unrelated = commit_files(root, "docs: update another story", {"notes/other.txt": "other\n"})
            self.write_story(root, baseline, ["src/owned.txt"])

            result = self.validate(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn(f"{owned} | story-id=match | disposition=owned", result.stdout)
            self.assertIn(f"{unrelated} | story-id=no-match | disposition=unrelated", result.stdout)
            self.assertIn("unowned | notes/other.txt", result.stdout)
            self.assertNotIn("notes/other.txt", result.stderr)

    def test_merge_is_listed_separately_from_non_merge_commits(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            main_branch = git(root, "branch", "--show-current").stdout.strip()
            owned = commit_files(root, "fix(9.7): add owned evidence", {"src/owned.txt": "owned\n"})
            git(root, "checkout", "-b", "side")
            unrelated = commit_files(root, "docs: add unrelated note", {"notes/side.txt": "side\n"})
            git(root, "checkout", main_branch)
            merge_result = git(root, "merge", "--no-ff", "side", "-m", "Merge side fixture")
            self.assertEqual(merge_result.returncode, 0, merge_result.stdout + merge_result.stderr)
            merge_sha = git(root, "rev-parse", "HEAD").stdout.strip()
            self.write_story(
                root,
                baseline,
                ["src/owned.txt"],
                unrelated="- `notes/side.txt` - unrelated branch fixture.",
            )

            result = self.validate(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn(f"{owned} | story-id=match | disposition=owned", result.stdout)
            self.assertIn(f"{unrelated} | story-id=no-match | disposition=unrelated", result.stdout)
            self.assertIn(f"    - {merge_sha} | Merge side fixture", result.stdout)

    def test_invalid_candidate_and_non_ancestral_range_fail(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            self.write_story(root, baseline, [])

            result = self.validate(root, "missing-candidate")

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("git failure while resolving candidate", result.stderr)

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            initial = init_repo(root)
            newer = commit_files(root, "fix(9.7): newer baseline", {"src/newer.txt": "newer\n"})
            self.write_story(root, newer, [])

            result = self.validate(root, initial)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("to be an ancestor of candidate", result.stderr)

    def test_candidate_mode_rejects_empty_ref_and_changed_file_override(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            self.write_story(root, baseline, [])

            empty = self.validate(root, "")
            overridden = self.validate(root, changed_files=("README.md",))

            self.assertNotEqual(empty.returncode, 0)
            self.assertIn("--candidate requires a non-empty ref", empty.stderr)
            self.assertNotIn("Traceback", empty.stderr)
            self.assertNotEqual(overridden.returncode, 0)
            self.assertIn("--changed-file cannot be combined with --candidate", overridden.stderr)

    def test_candidate_mode_base_override_replaces_frontmatter_baseline(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            initial = init_repo(root)
            overridden_base = commit_files(root, "docs: before story", {"notes/before.txt": "before\n"})
            owned = commit_files(root, "fix(9.7): owned change", {"src/owned.txt": "owned\n"})
            self.write_story(root, initial, ["src/owned.txt"])

            result = self.validate(root, base=overridden_base)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn(f"baseline: {overridden_base}", result.stdout)
            self.assertIn(f"{owned} | story-id=match | disposition=owned", result.stdout)
            self.assertNotIn("docs: before story", result.stdout)

    def test_committed_deletion_is_reported_and_reconciled(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            (root / "README.md").unlink()
            git(root, "add", "-u", "README.md")
            deleted = git(root, "commit", "-m", "fix(9.7): delete fixture")
            self.assertEqual(deleted.returncode, 0, deleted.stdout + deleted.stderr)
            candidate = git(root, "rev-parse", "HEAD").stdout.strip()
            self.write_story(root, baseline, ["README.md"])

            result = self.validate(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn(f"{candidate} | story-id=match | disposition=owned", result.stdout)
            self.assertIn("owned | README.md", result.stdout)

    def test_story_metadata_is_authoritative_conflict_safe_and_zero_preserving(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            commit_files(root, "fix(9.7): tempting fallback", {"src/owned.txt": "owned\n"})
            invalid = story_text(
                baseline=baseline,
                file_list=f"- `{self.story_path}`\n- `src/owned.txt`",
                story_id="9.7.1",
            )
            write(root / self.story_path, invalid)

            result = self.validate(root)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("invalid explicit story_id", result.stderr)
            self.assertNotIn("Traceback", result.stderr)

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            commit_files(root, "fix(9.7): conflict fixture", {"src/owned.txt": "owned\n"})
            conflict = story_text(
                baseline=baseline,
                file_list=f"- `{self.story_path}`\n- `src/owned.txt`",
            ).replace("story_id: '1.1'\n", "title: 'Story 9.7: Title identity'\n").replace(
                "# Story 1.1: Validator fixture",
                "# Story 9.8: H1 identity",
            )
            write(root / self.story_path, conflict)

            result = self.validate(root)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("conflicting legacy story identities", result.stderr)
            self.assertNotIn("Traceback", result.stderr)

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            padded = commit_files(root, "fix(09.07): preserve padded ID", {"src/owned.txt": "owned\n"})
            self.write_story(root, baseline, ["src/owned.txt"])
            story = (root / self.story_path).read_text(encoding="utf-8").replace("story_id: '9.7'", "story_id: '09-07'")
            write(root / self.story_path, story)

            result = self.validate(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("story-id: 09.07", result.stdout)
            self.assertIn(f"{padded} | story-id=match | disposition=owned", result.stdout)

    def test_legacy_three_segment_identity_is_not_truncated(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            commit_files(root, "fix(9.7): must not inherit", {"src/owned.txt": "owned\n"})
            content = story_text(
                baseline=baseline,
                file_list=f"- `{self.story_path}`\n- `src/owned.txt`",
            ).replace("story_id: '1.1'\n", "").replace(
                "# Story 1.1: Validator fixture",
                "# Story 9.7.1: Three segment identity",
            )
            write(root / self.story_path, content)

            result = self.validate(root)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("invalid legacy story identity", result.stderr)
            self.assertNotIn("Traceback", result.stderr)

    def test_nul_delimited_paths_preserve_unusual_filenames(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            unusual = (
                "notes/line\nbreak.txt",
                "notes/back\\slash.txt",
                'notes/double"quote.txt',
                "notes/évidence.txt",
                "notes/ leading-and-trailing ",
            )
            commit_files(root, "docs: add unusual paths", {path: "value\n" for path in unusual})
            self.write_story(root, baseline, [])

            result = self.validate(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            for path in unusual:
                rendered = (
                    json.dumps(path, ensure_ascii=False)
                    if path != path.strip() or any(character in path for character in ('"', "\\", "\n", "\r", "\t"))
                    else path
                )
                self.assertIn(f"unowned | {rendered}", result.stdout)

    def test_unresolved_workspace_state_is_reported_from_one_porcelain_snapshot(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            main_branch = git(root, "branch", "--show-current").stdout.strip()
            git(root, "checkout", "-b", "other")
            commit_files(root, "docs: other side", {"README.md": "other\n"})
            git(root, "checkout", main_branch)
            commit_files(root, "fix(9.7): owned side", {"README.md": "owned\n"})
            conflict = git(root, "merge", "other")
            self.assertNotEqual(conflict.returncode, 0)
            self.write_story(root, baseline, ["README.md"])

            result = self.validate(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("    unresolved:\n      - README.md", result.stdout)

    def test_documented_unrelated_directory_covers_changed_descendants_consistently(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(root / "notes/scratch/nested.txt", "scratch\n")
            self.write_story(
                root,
                baseline,
                [],
                unrelated="- `notes/scratch` - bounded scratch directory.",
            )

            result = self.validate(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn(
                "notes/scratch/nested.txt | documented-unrelated=bounded scratch directory.",
                result.stdout,
            )

    def test_workspace_state_is_reported_separately_with_unrelated_annotation(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            commit_files(root, "fix(9.7): add committed evidence", {"src/owned.txt": "owned\n"})
            write(root / "src/staged.txt", "staged\n")
            git(root, "add", "src/staged.txt")
            write(root / "README.md", "unstaged\n")
            write(root / "src/untracked.txt", "untracked\n")
            write(root / "notes/unrelated.txt", "unrelated\n")
            self.write_story(
                root,
                baseline,
                ["src/owned.txt", "src/staged.txt", "README.md", "src/untracked.txt"],
                unrelated="- `notes/unrelated.txt` - editor scratch.",
            )

            result = self.validate(root)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn("    staged:\n      - src/staged.txt", result.stdout)
            self.assertIn("    unstaged:\n      - README.md", result.stdout)
            self.assertIn("src/untracked.txt", result.stdout)
            self.assertIn("notes/unrelated.txt | documented-unrelated=editor scratch.", result.stdout)
            self.assertIn(
                "notes/unrelated.txt | state=untracked | reason=editor scratch.",
                result.stdout,
            )


class BootstrapOwnedAuthorizationTests(unittest.TestCase):
    @staticmethod
    def _module():
        import importlib.util

        spec = importlib.util.spec_from_file_location(
            "story_artifact_validator_bootstrap", VALIDATOR
        )
        module = importlib.util.module_from_spec(spec)
        sys.modules["story_artifact_validator_bootstrap"] = module
        spec.loader.exec_module(module)
        return module

    def setUp(self) -> None:
        self.validator = self._module()

    def valid_authorization(self) -> dict[str, object]:
        return {
            "story_path": self.validator.BOOTSTRAP_OWNED_STORY_PATH,
            "story_id": self.validator.BOOTSTRAP_OWNED_STORY_ID,
            "declared_baseline": self.validator.BOOTSTRAP_OWNED_BASELINE,
            "resolved_baseline": self.validator.BOOTSTRAP_OWNED_BASELINE,
            "sha": self.validator.BOOTSTRAP_OWNED_COMMIT,
            "parents": [self.validator.BOOTSTRAP_OWNED_BASELINE],
            "subject_matches": False,
            "paths": set(self.validator.BOOTSTRAP_OWNED_PATHS)
            | {"_bmad-output/implementation-artifacts/spec-actions-29316660112-fix-cicd.md"},
            "file_list": set(self.validator.BOOTSTRAP_OWNED_PATHS),
            "disposition_failures": [],
            "bootstrap_declaration_count": 1,
        }

    def test_exact_historical_tuple_and_immutable_path_intersection_are_authorized(self) -> None:
        failures = self.validator.bootstrap_owned_authorization_failures(
            **self.valid_authorization()
        )

        self.assertEqual(failures, [])

    def test_every_authorization_dimension_fails_closed_when_changed(self) -> None:
        other_sha = "0" * 40
        cases = {
            "artifact": {"story_path": "_bmad-output/implementation-artifacts/copied.md"},
            "story": {"story_id": "9.8"},
            "declared-baseline": {"declared_baseline": other_sha},
            "resolved-baseline": {"resolved_baseline": other_sha},
            "commit": {"sha": other_sha},
            "no-parent": {"parents": []},
            "merge": {"parents": [self.validator.BOOTSTRAP_OWNED_BASELINE, other_sha]},
            "matching-subject": {"subject_matches": True},
            "invalid-declaration": {"disposition_failures": ["malformed declaration"]},
            "multiple-declarations": {"bootstrap_declaration_count": 2},
            "missing-touched-guard": {
                "paths": set(self.validator.BOOTSTRAP_OWNED_PATHS)
                - {"eng/validate-story-artifacts.py"}
            },
            "missing-listed-guard": {
                "file_list": set(self.validator.BOOTSTRAP_OWNED_PATHS)
                - {"eng/tests/test_validate_story_artifacts.py"}
            },
        }
        for name, change in cases.items():
            with self.subTest(name=name):
                authorization = self.valid_authorization()
                authorization.update(change)

                failures = self.validator.bootstrap_owned_authorization_failures(
                    **authorization
                )

                self.assertTrue(failures)

    def test_file_list_cannot_add_or_remove_a_bootstrap_touched_path(self) -> None:
        historically_unowned = (
            "_bmad-output/implementation-artifacts/spec-actions-29316660112-fix-cicd.md"
        )
        expanded = self.valid_authorization()
        expanded["file_list"] = set(self.validator.BOOTSTRAP_OWNED_PATHS) | {
            historically_unowned
        }
        reduced = self.valid_authorization()
        reduced["file_list"] = set(self.validator.BOOTSTRAP_OWNED_PATHS) - {
            ".agents/skills/bmad-build/spec-template.md"
        }

        expanded_failures = self.validator.bootstrap_owned_authorization_failures(**expanded)
        reduced_failures = self.validator.bootstrap_owned_authorization_failures(**reduced)

        self.assertTrue(any("unexpected" in failure for failure in expanded_failures))
        self.assertTrue(any("missing" in failure for failure in reduced_failures))

    def test_future_file_list_path_not_touched_by_bootstrap_does_not_change_authorization(self) -> None:
        authorization = self.valid_authorization()
        authorization["file_list"] = set(self.validator.BOOTSTRAP_OWNED_PATHS) | {
            "future/story-owned-path.txt"
        }

        failures = self.validator.bootstrap_owned_authorization_failures(**authorization)

        self.assertEqual(failures, [])

    def test_multiple_bootstrap_declarations_are_rejected_by_the_parser(self) -> None:
        dispositions, failures = self.validator.extract_commit_scope_dispositions(
            f"- `{self.validator.BOOTSTRAP_OWNED_COMMIT}` | `bootstrap-owned` | exact\n"
            f"- `{'0' * 40}` | `bootstrap-owned` | second"
        )

        self.assertEqual(len(dispositions), 2)
        self.assertIn(
            "multiple bootstrap-owned Commit Scope Dispositions declarations are not allowed",
            failures,
        )

    def test_canonical_historical_cli_report_preserves_owned_and_unowned_labels(self) -> None:
        story = REPO_ROOT / self.validator.BOOTSTRAP_OWNED_STORY_PATH
        result = run(
            [
                sys.executable,
                str(VALIDATOR),
                "--project-root",
                str(REPO_ROOT),
                "--story",
                self.validator.BOOTSTRAP_OWNED_STORY_PATH,
                "--candidate",
                "2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06",
                "--skip-sentinel",
            ],
            REPO_ROOT,
        )

        self.assertTrue(story.is_file())
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        bootstrap_start = result.stdout.index(
            f"    - {self.validator.BOOTSTRAP_OWNED_COMMIT} |"
        )
        shared_start = result.stdout.index(
            "    - 2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06 |"
        )
        bootstrap_report = result.stdout[bootstrap_start:shared_start]
        shared_report = result.stdout[shared_start:]
        self.assertIn("story-id=no-match | disposition=bootstrap-owned", bootstrap_report)
        for path in self.validator.BOOTSTRAP_OWNED_PATHS:
            self.assertIn(f"      - owned | {path}", bootstrap_report)
        self.assertIn(
            "unowned | _bmad-output/implementation-artifacts/"
            "spec-actions-29316660112-fix-cicd.md",
            bootstrap_report,
        )
        self.assertIn("story-id=no-match | disposition=shared", shared_report)
        self.assertIn("listed-unowned | .github/workflows/quality.yml", shared_report)
        self.assertIn(
            "listed-unowned | tests/Hexalith.FrontComposer.Shell.Tests/"
            "Governance/CiGovernanceTests.cs",
            shared_report,
        )
        self.assertNotIn("      - owned | .github/workflows/quality.yml", shared_report)

    def assert_bootstrap_has_no_ownership(
        self,
        evidence,
        failures: list[str],
        metadata,
        expected_failure: str,
    ) -> None:
        self.assertIsNotNone(evidence)
        self.assertTrue(any(expected_failure in failure for failure in failures), failures)
        bootstrap = next(
            commit
            for commit in evidence.commits
            if commit.sha == self.validator.BOOTSTRAP_OWNED_COMMIT
        )
        self.assertEqual(bootstrap.classification, "unmapped")
        report = self.validator.format_commit_scope_evidence(
            evidence,
            metadata.file_list,
            {},
        )
        bootstrap_start = report.index(
            f"    - {self.validator.BOOTSTRAP_OWNED_COMMIT} |"
        )
        shared_start = report.index(
            "    - 2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06 |",
            bootstrap_start,
        )
        bootstrap_report = report[bootstrap_start:shared_start]
        self.assertNotIn("disposition=bootstrap-owned", bootstrap_report)
        for path in self.validator.BOOTSTRAP_OWNED_PATHS:
            self.assertIn(f"      - listed-unowned | {path}", bootstrap_report)
            self.assertNotIn(f"      - owned | {path}", bootstrap_report)

    def test_symbolic_declared_baseline_resolving_to_exact_sha_grants_no_ownership(self) -> None:
        story = REPO_ROOT / self.validator.BOOTSTRAP_OWNED_STORY_PATH
        metadata = self.validator.parse_story_metadata(story)
        symbolic_baseline = f"{self.validator.BOOTSTRAP_OWNED_COMMIT}^1"
        metadata = replace(metadata, baseline_commit=symbolic_baseline)

        evidence, failures = self.validator.collect_commit_scope_evidence(
            REPO_ROOT,
            symbolic_baseline,
            "2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06",
            metadata,
            story,
        )

        self.assertEqual(
            self.validator.canonical_commit(REPO_ROOT, symbolic_baseline, "test baseline"),
            self.validator.BOOTSTRAP_OWNED_BASELINE,
        )
        self.assert_bootstrap_has_no_ownership(
            evidence,
            failures,
            metadata,
            "declared story baseline_commit",
        )

    def test_resolved_baseline_deviation_with_exact_declaration_grants_no_ownership(self) -> None:
        story = REPO_ROOT / self.validator.BOOTSTRAP_OWNED_STORY_PATH
        metadata = self.validator.parse_story_metadata(story)
        earlier_baseline = f"{self.validator.BOOTSTRAP_OWNED_BASELINE}^1"

        evidence, failures = self.validator.collect_commit_scope_evidence(
            REPO_ROOT,
            earlier_baseline,
            "2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06",
            metadata,
            story,
        )

        self.assertEqual(metadata.baseline_commit, self.validator.BOOTSTRAP_OWNED_BASELINE)
        self.assertNotEqual(
            self.validator.canonical_commit(REPO_ROOT, earlier_baseline, "test baseline"),
            self.validator.BOOTSTRAP_OWNED_BASELINE,
        )
        self.assert_bootstrap_has_no_ownership(
            evidence,
            failures,
            metadata,
            "resolved baseline",
        )

    def test_mutable_canonical_file_list_cannot_broaden_bootstrap_ownership(self) -> None:
        story = REPO_ROOT / self.validator.BOOTSTRAP_OWNED_STORY_PATH
        metadata = self.validator.parse_story_metadata(story)
        historically_unowned = (
            "_bmad-output/implementation-artifacts/spec-actions-29316660112-fix-cicd.md"
        )
        metadata = replace(
            metadata,
            file_list={**metadata.file_list, historically_unowned: ""},
        )

        evidence, failures = self.validator.collect_commit_scope_evidence(
            REPO_ROOT,
            self.validator.BOOTSTRAP_OWNED_BASELINE,
            "2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06",
            metadata,
            story,
        )

        self.assertIsNotNone(evidence)
        self.assertTrue(any("unexpected" in failure for failure in failures))
        bootstrap = next(
            commit
            for commit in evidence.commits
            if commit.sha == self.validator.BOOTSTRAP_OWNED_COMMIT
        )
        self.assertEqual(bootstrap.classification, "unmapped")
        report = self.validator.format_commit_scope_evidence(
            evidence,
            metadata.file_list,
            {},
        )
        bootstrap_report = report[
            report.index(f"    - {self.validator.BOOTSTRAP_OWNED_COMMIT} |") : report.index(
                "    - 2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06 |"
            )
        ]
        self.assertIn(f"listed-unowned | {historically_unowned}", bootstrap_report)
        self.assertNotIn(f"      - owned | {historically_unowned}", bootstrap_report)

    def test_copied_story_artifact_cannot_reuse_bootstrap_authorization(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / "clone"
            cloned = run(
                ["git", "clone", "--quiet", "--shared", str(REPO_ROOT), str(root)],
                Path(temp),
            )
            self.assertEqual(cloned.returncode, 0, cloned.stdout + cloned.stderr)
            copied_story = "_bmad-output/implementation-artifacts/copied-story.md"
            write(
                root / copied_story,
                (REPO_ROOT / self.validator.BOOTSTRAP_OWNED_STORY_PATH).read_text(
                    encoding="utf-8"
                ),
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    copied_story,
                    "--candidate",
                    "2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("invalid bootstrap-owned disposition", result.stderr)
            self.assertIn("the story artifact path must be", result.stderr)
            self.assertNotIn("disposition=bootstrap-owned", result.stdout)
            self.assertIn("disposition=unmapped", result.stdout)


@unittest.skipUnless(
    (STORY_AUTOMATOR_SRC / "story_automator/core/success_verifiers.py").is_file(),
    "bmad-story-automator skill is not installed",
)
class ReviewVerifierTests(unittest.TestCase):
    def test_incomplete_review_reports_workflow_not_complete(self) -> None:
        sys.path.insert(0, str(STORY_AUTOMATOR_SRC))
        from story_automator.core.success_verifiers import review_completion

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/sprint-status.yaml",
                """
                development_status:
                  1-1-validator-fixture: review
                """,
            )
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(baseline=baseline),
            )

            result = review_completion(project_root=str(root), story_key="1-1-validator-fixture")

            self.assertFalse(result["verified"])
            self.assertEqual(result["reason"], "workflow_not_complete")

    def test_artifact_validation_failure_prevents_done_review_completion(self) -> None:
        sys.path.insert(0, str(STORY_AUTOMATOR_SRC))
        from story_automator.core.success_verifiers import review_completion

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            (root / "eng").mkdir()
            shutil.copy2(VALIDATOR, root / "eng/validate-story-artifacts.py")
            write(
                root / "_bmad-output/implementation-artifacts/sprint-status.yaml",
                """
                development_status:
                  1-1-validator-fixture: done
                """,
            )
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(baseline=baseline),
            )
            write(root / "src/owned.txt", "owned\n")

            result = review_completion(project_root=str(root), story_key="1-1-validator-fixture")

            self.assertFalse(result["verified"])
            self.assertEqual(result["reason"], "artifact_validation_failed")
            self.assertIn("src/owned.txt", str(result.get("artifactValidationOutput")))


class PathMentionRejectionTests(unittest.TestCase):
    """Pin each non-output-path rejection class directly.

    End-to-end story fixtures cannot isolate these: a bare token is rejected by the
    tree-absent-basename rule before the suffix-literal or invocation-token rule is
    reached, so deleting either rule leaves an end-to-end suite green. These unit
    assertions make every rule independently load-bearing.
    """

    @staticmethod
    def _module():
        import importlib.util

        spec = importlib.util.spec_from_file_location("story_artifact_validator", VALIDATOR)
        module = importlib.util.module_from_spec(spec)
        sys.modules["story_artifact_validator"] = module
        spec.loader.exec_module(module)
        return module

    def setUp(self) -> None:
        self.validator = self._module()
        self.root = Path(REPO_ROOT)

    def _rejected(self, token: str) -> bool:
        return self.validator.mention_is_not_an_output_path(token, self.root)

    def test_bare_suffix_literals_are_rejected(self) -> None:
        """Rejected via the tree-absent-basename rule; there is no separate suffix rule."""
        for token in (".g.cs", ".generated.cs", ".designer.cs", ".AssemblyInfo.cs", ".razor.cs"):
            with self.subTest(token=token):
                self.assertTrue(self._rejected(token))

    def test_path_line_coordinates_are_rejected(self) -> None:
        for token in (
            "src/Foo.cs:42",
            "tests/Bar.cs:487-496",
            "eng/baz.py:1,7",
        ):
            with self.subTest(token=token):
                self.assertTrue(self._rejected(token))

    def test_invocation_tokens_are_rejected(self) -> None:
        # The qualified form is the load-bearing case: a directory-carrying token skips
        # the bare-basename rule, so only the invocation rule can reject it.
        for token in (".First()", "Foo.Bar()", "GetMethod(name)", "src/Foo.Bar()", "eng/x.py:run()"):
            with self.subTest(token=token):
                self.assertTrue(self._rejected(token))

    def test_elided_citations_are_rejected(self) -> None:
        self.assertTrue(self._rejected(".../CommandAuthorizationResource.cs"))

    def test_directories_are_rejected(self) -> None:
        self.assertTrue(self._rejected("src/Hexalith.FrontComposer.Shell"))
        self.assertTrue(self._rejected("eng"))

    def test_tree_absent_bare_basenames_are_rejected(self) -> None:
        for token in ("Foo.cs", "Foo.Handlers.cs", "NotARealFileAnywhere.cs"):
            with self.subTest(token=token):
                self.assertTrue(self._rejected(token))

    def test_real_output_paths_are_never_rejected(self) -> None:
        """The fail-closed floor: anything that could denote produced work stays strict."""
        for token in (
            "src/Hexalith.FrontComposer.Shell/Extensions/DomainBootstrapMarker.cs",
            "eng/validate-story-artifacts.py",
            "src/Fabricated.cs",
            "src/does/not/exist/Phantom.cs",
        ):
            with self.subTest(token=token):
                self.assertFalse(self._rejected(token))

    def test_tree_present_bare_basename_stays_strict(self) -> None:
        self.assertFalse(self._rejected("validate-story-artifacts.py"))

    def test_path_line_column_coordinates_are_rejected(self) -> None:
        """The grep/compiler `file:line:column` form is a citation, not an output."""
        for token in ("src/reference.cs:487:12", "tests/Bar.cs:42-50:3", "src/Makefile:42"):
            with self.subTest(token=token):
                self.assertTrue(self._rejected(token))

    def test_colon_token_without_a_line_number_stays_strict(self) -> None:
        """Pins the coordinate rule's shape: widening it to `":" in token` must fail here."""
        self.assertFalse(self._rejected("src/Foo.cs:abc"))
        self.assertFalse(self._rejected("src/weird:name.cs"))

    def test_mid_token_elisions_are_rejected(self) -> None:
        """extract_file_list_entry rejects `...` anywhere; both parsers must agree."""
        for token in ("src/.../Shell/Services/Widget.cs", "src/…/Widget.cs"):
            with self.subTest(token=token):
                self.assertTrue(self._rejected(token))

    def test_dot_prefixed_real_paths_are_never_rejected(self) -> None:
        """Pins the ellipsis rule's shape: narrowing it to `startswith(".")` must fail here."""
        for token in (".github/workflows/ci.yml", ".editorconfig", ".gitmodules"):
            with self.subTest(token=token):
                self.assertFalse(self._rejected(token))

    def test_creation_claim_keeps_a_bare_basename_strict(self) -> None:
        """A file the story claims to have created is tree-absent by construction."""
        for token in ("NewStrictGuard.cs", "PendingCommandBatchReducers.cs"):
            with self.subTest(token=token):
                self.assertTrue(self._rejected(token))
                self.assertFalse(
                    self.validator.mention_is_not_an_output_path(
                        token, self.root, creation_claimed=True
                    )
                )

    def test_creation_verbs_are_detected_within_the_clause(self) -> None:
        claims = (
            "Fix: add `NewStrictGuard.cs` implementing the check.",
            "Fix: create a new file `NewStrictGuard.cs` for the seam.",
            "Fix: generate `NewStrictGuard.cs` from the template.",
            "Fix: created `NewStrictGuard.cs` for the seam.",
            "Fix: added `NewStrictGuard.cs` implementing the check.",
            "Fix: wrote `NewStrictGuard.cs` from the template.",
        )
        for text in claims:
            with self.subTest(text=text):
                self.assertTrue(
                    self.validator.mention_claims_creation(text, text.index("`"))
                )
        # A prior sentence's creation verb must not govern a later clause.
        text = "Fix: create the seam. The idiomatic `Foo.cs` split stays supported."
        self.assertFalse(self.validator.mention_claims_creation(text, text.index("`")))

    def test_verb_alternation_covers_third_person_and_past_inflections(self) -> None:
        """`s?` cannot form `modifies` or `touches`; past tense must not evade either."""
        alternation = self.validator.verb_alternation(("modify", "touch", "update", "create", "write"))
        for form in (
            "modifies",
            "touches",
            "updates",
            "creates",
            "writes",
            "modified",
            "touched",
            "updated",
            "created",
            "wrote",
            "written",
            "modify",
            "touch",
            "update",
            "create",
            "write",
        ):
            with self.subTest(form=form):
                self.assertIn(form, alternation.split("|"))

    def test_past_tense_preserve_clause_does_not_hide_action_governed_path(self) -> None:
        # Past-tense "updated" must trip the preserve-clause exception the same way "update" does.
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline = init_repo(root)
            write(
                root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                story_text(
                    baseline=baseline,
                    file_list="- `README.md` - test evidence.",
                    tasks="- [x] Keep behavior and updated `src/ghost.cs` so output stays unchanged.",
                ),
            )

            result = run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--project-root",
                    str(root),
                    "--story",
                    "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                    "--changed-file",
                    "README.md",
                    "--skip-sentinel",
                ],
                root,
            )

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("missing evidence path: src/ghost.cs", result.stderr)

    def test_past_tense_creation_claim_keeps_bare_basename_strict(self) -> None:
        text = "Fix: created `PhantomNew.cs` for the guard."
        self.assertTrue(self.validator.mention_claims_creation(text, text.index("`")))
        self.assertFalse(
            self.validator.mention_is_not_an_output_path(
                "PhantomNew.cs", self.root, creation_claimed=True
            )
        )

    def test_negation_and_positive_verb_sets_cannot_drift(self) -> None:
        """`remove` was a negation verb but not a positive one, so a genuine deletion
        claim inside a preservation clause was suppressed while `delete` was enforced.

        The expected vocabulary is spelled out rather than read from the module: deriving
        it from ACTION_VERBS would let a dropped verb shrink the loop instead of failing.
        """
        expected = {
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
        }
        self.assertEqual(set(self.validator.ACTION_VERBS), expected)
        for verb in sorted(expected):
            with self.subTest(verb=verb):
                self.assertIsNotNone(
                    self.validator.POSITIVE_ACTION.search(f"and {verb} ")
                )
                self.assertIsNotNone(
                    self.validator.NEGATED_ACTION.search(f"this story did not {verb} ")
                )

    def test_unlistable_tree_fails_closed(self) -> None:
        """A tree that cannot be listed must not exempt every bare basename wholesale."""
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self.assertTrue(self.validator.basename_exists_in_tree("Anything.cs", root))
            self.assertFalse(self.validator.path_is_tracked("references/Thing", root))


class ClassifiedUnrelatedTests(unittest.TestCase):
    """Pin the classification-as-evidence mechanism.

    The story author writes the Documented Unrelated section, so this set grants evidence
    from prose the author controls. Every bound on it must be independently load-bearing.
    """

    @staticmethod
    def _module():
        import importlib.util

        spec = importlib.util.spec_from_file_location("story_artifact_validator", VALIDATOR)
        module = importlib.util.module_from_spec(spec)
        sys.modules["story_artifact_validator"] = module
        spec.loader.exec_module(module)
        return module

    def setUp(self) -> None:
        self.validator = self._module()

    def test_classified_directory_covers_nested_paths(self) -> None:
        covered = self.validator.path_is_classified_unrelated(
            "references/Hexalith.Builds/Props/Directory.Packages.props",
            {"references/Hexalith.Builds"},
        )
        self.assertTrue(covered)

    def test_classification_matches_whole_segments_only(self) -> None:
        """`references/Hexalith.BuildsExtra` is not covered by `references/Hexalith.Builds`."""
        covered = self.validator.path_is_classified_unrelated(
            "references/Hexalith.BuildsExtra/x.cs",
            {"references/Hexalith.Builds"},
        )
        self.assertFalse(covered)

    def test_trailing_slash_classification_covers_the_same_paths(self) -> None:
        for entry in ("references/Hexalith.Builds", "references/Hexalith.Builds/"):
            with self.subTest(entry=entry):
                self.assertTrue(
                    self.validator.path_is_classified_unrelated(
                        "references/Hexalith.Builds/Props/Directory.Packages.props",
                        {entry},
                    )
                )

    def test_top_level_directory_classification_is_refused(self) -> None:
        """One `src` bullet must not exempt every path beneath it."""
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            init_repo(root)
            write(root / "src/real.cs", "// real\n")
            git(root, "add", "src/real.cs")
            git(root, "commit", "-m", "add src")

            usable = self.validator.usable_classified_paths(
                root, {"src": "pre-existing", "src/real.cs": "pre-existing"}, set()
            )

            self.assertNotIn("src", usable)
            self.assertIn("src/real.cs", usable)

    def test_top_level_directory_classification_is_refused_without_workdir_dir(self) -> None:
        """Refuse bare `src` even when the working-tree directory is gone but paths remain tracked."""
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            init_repo(root)
            write(root / "src/real.cs", "// real\n")
            git(root, "add", "src/real.cs")
            git(root, "commit", "-m", "add src")
            # Remove the working-tree directory without untracking: is_dir() alone would miss this.
            import shutil

            shutil.rmtree(root / "src")

            usable = self.validator.usable_classified_paths(
                root, {"src": "pre-existing"}, set()
            )

            self.assertNotIn("src", usable)

    def test_bare_top_level_in_changed_refused_when_tracked_listing_fails(self) -> None:
        """When git ls-files fails, a bare name present only in `changed` must not be usable."""
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            # No git repository → tracked_files returns None.
            usable = self.validator.usable_classified_paths(
                root, {"src": "pre-existing"}, {"src"}
            )
            self.assertNotIn("src", usable)

    def test_classification_of_an_unreal_path_is_refused(self) -> None:
        """A story must not account for a fabricated path by inventing a bullet for it."""
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            init_repo(root)

            usable = self.validator.usable_classified_paths(
                root, {"src/ghost.cs": "pre-existing"}, set()
            )
            self.assertEqual(usable, set())

            dirty = self.validator.usable_classified_paths(
                root, {"src/ghost.cs": "pre-existing"}, {"src/ghost.cs"}
            )
            self.assertEqual(dirty, {"src/ghost.cs"})

    def _run_story(self, root: Path, unrelated_section: str, task: str) -> subprocess.CompletedProcess[str]:
        baseline = init_repo(root)
        write(root / "references/Hexalith.Builds/Props/Directory.Packages.props", "<Project />\n")
        git(root, "add", "references/Hexalith.Builds/Props/Directory.Packages.props")
        git(root, "commit", "-m", "add builds")
        write(
            root / "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
            story_text(baseline=baseline, file_list="- `README.md` - test evidence.", tasks=task)
            + unrelated_section,
        )
        return run(
            [
                sys.executable,
                str(VALIDATOR),
                "--project-root",
                str(root),
                "--story",
                "_bmad-output/implementation-artifacts/1-1-validator-fixture.md",
                "--changed-file",
                "README.md",
                "--skip-sentinel",
            ],
            root,
        )

    def test_classified_path_counts_as_evidence_end_to_end(self) -> None:
        task = (
            "- [x] [Review][Patch] Fix: update "
            "`references/Hexalith.Builds/Props/Directory.Packages.props` for the pin."
        )
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            result = self._run_story(
                root,
                "\n### Documented Unrelated Workspace State\n\n"
                "- `references/Hexalith.Builds` - accepted submodule drift.\n",
                task,
            )
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            result = self._run_story(root, "", task)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn(
                "references/Hexalith.Builds/Props/Directory.Packages.props", result.stderr
            )

    def test_classified_basename_counts_as_evidence(self) -> None:
        """Pins the classified_unrelated contribution to the basename evidence set."""
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            result = self._run_story(
                root,
                "\n### Documented Unrelated Workspace State\n\n"
                "- `references/Hexalith.Builds/Props/Directory.Packages.props`"
                " - accepted submodule drift.\n",
                "- [x] [Review][Patch] Fix: update `Directory.Packages.props` for the pin.",
            )
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
