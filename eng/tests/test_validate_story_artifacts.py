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
        )
        for text in claims:
            with self.subTest(text=text):
                self.assertTrue(
                    self.validator.mention_claims_creation(text, text.index("`"))
                )
        # A prior sentence's creation verb must not govern a later clause.
        text = "Fix: create the seam. The idiomatic `Foo.cs` split stays supported."
        self.assertFalse(self.validator.mention_claims_creation(text, text.index("`")))

    def test_verb_alternation_covers_third_person_inflections(self) -> None:
        """`s?` cannot form `modifies` or `touches`; suppression must not hinge on tense."""
        alternation = self.validator.verb_alternation(("modify", "touch", "update"))
        for form in ("modifies", "touches", "updates", "modify", "touch", "update"):
            with self.subTest(form=form):
                self.assertIn(form, alternation.split("|"))

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
