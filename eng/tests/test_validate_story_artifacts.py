from __future__ import annotations

import os
import shutil
import subprocess
import sys
import tempfile
import textwrap
import unittest
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


def story_text(*, baseline: str, file_list: str = "", tasks: str = "- [ ] Pending task") -> str:
    return (
        f"---\n"
        f"baseline_commit: {baseline}\n"
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


if __name__ == "__main__":
    unittest.main()
