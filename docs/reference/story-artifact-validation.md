---
title: "Story artifact validation"
description: "Reference for story IDs, commit-scope dispositions, path labels, and strict reconciliation."
genre: reference
audience: framework-contributor
ownerStory: 9-7-add-story-id-and-commit-scope-evidence
status: published
reviewed: 2026-08-25
uid: frontcomposer.reference.story-artifact-validation
slug: reference/story-artifact-validation/
---

# Story artifact validation

Numbered story artifacts declare a canonical `story_id` and an immutable
`baseline_commit`. Before review or completion, validate the complete range and the
current workspace:

```bash
python3 eng/validate-story-artifacts.py --story <story-file> --candidate HEAD
```

## Invocation

Strict mode is the `--candidate` form above. It requires a story artifact and a
resolvable baseline, and it reports every commit in `baseline..candidate` plus the
current workspace.

Legacy, best-effort mode omits `--candidate`. Use it for a freeform spec, a missing,
`NO_VCS`, or unresolvable `baseline_commit`, or an environment without git. It compares
the working tree against the baseline when one resolves and otherwise falls back to a
bare workspace diff, saying so on stdout. It reports no commit evidence.

| Option | Effect |
| --- | --- |
| `--candidate <ref>` | Strict mode. Requires `--story`; rejects an empty ref and `--changed-file`. |
| `--base <commit>` | Override the story's `baseline_commit`. An empty or unresolvable value fails; a story-declared baseline that cannot resolve degrades to the reported bare-diff fallback instead. |
| `--changed-file <path>` | Supply changed paths directly instead of discovering them. Legacy mode only. |
| `--unrelated <path> --reason <text>` | Classify a dirty path as unrelated from the command line, pairwise, as an alternative to a story section. |
| `--exclude <glob>` | Add an exclusion pattern. |
| `--project-root <dir>` | Repository root. Defaults to the current directory. |
| `--sentinel-root <path>` / `--skip-sentinel` | Scope or skip the raw authoring-sentinel scan. |

Excluded by default: `.git/**`, `**/bin/**`, `**/obj/**`, `**/node_modules/**`,
`docs/_site/**`, and `_bmad-output/story-automator/**`. Each pattern matches the named
directory wherever it sits, including at the repository root, and covers the directory
entry itself as well as the tree beneath it. Exclusions bound classification and both
halves of reconciliation. An excluded path is still printed, labelled `excluded`, and
carries no ownership; it also cannot make a story commit `interleaved`.

One carve-out: paths supplied with `--changed-file` bypass git discovery *and*
exclusion filtering, because naming a path explicitly is a stronger signal than a
default pattern. Everything else the validator discovers is filtered.

A File List entry that an exclusion pattern covers still fails: the change to it
contributes no ownership, so it has no matching story-owned change. Resolve it by
removing the entry or narrowing the exclusion — the failure names both edits.

Exit code `0` means validation passed and `1` means it failed. Failures print to stderr,
the evidence report and notices print to stdout. An invalid invocation prints only the
invocation error and validates nothing.

## Declaring unrelated workspace state

Dirty paths that are not story output belong under `## Documented Unrelated Changes` or
`## Documented Unrelated Workspace State` (`### ` is also accepted), one bullet per
entry:

```text
- `path/to/file` - short reason
```

An entry naming a directory or submodule covers the paths beneath it, but only for
uncommitted state: a committed path needs its own exact entry, and a bare top-level
name such as `src` is refused. Declared paths stay visible in the report with their
reason, and never enter the File List as story-owned changes.

## Commit classification and path ownership

The strict report keeps commit classification separate from path ownership:

| Commit classification | Meaning | Contributes listed paths to reconciliation |
| --- | --- | --- |
| `owned` | Subject matches the story and every touched path is listed. | Yes |
| `interleaved` | Subject matches the story but at least one touched path is unlisted. This is a hard failure. | Yes, listed paths only |
| `unmapped` | Subject does not match the story but touches a listed path. This is a hard failure unless an allowed non-owning disposition applies. | No |
| `shared` | An exact declared non-matching commit belongs to shared work. It may classify an otherwise `unmapped` commit; matching commits remain `owned` or `interleaved`. | No |
| `process` | An exact declared non-matching commit belongs to workflow-only work. It has the same non-owning boundary as `shared`. | No |
| `bootstrap-owned` | A repository-code-authorized historical recovery. Story prose cannot create this authority. | Yes, authorized listed paths only |
| `unrelated` | Subject does not match and no touched path is listed. | No |

Each reported path is labelled independently:

- `owned` means the path is listed and its commit classification contributes ownership.
- `listed-unowned` means the path is listed but its commit classification contributes no ownership.
- `unowned` means the touched path is outside the story File List.
- `excluded` means an exclusion pattern covers the path, so it is reported but never
  reconciled or classified.

For compatibility, each non-merge row renders its classification through the stable
`disposition=<classification>` text field. A merge row uses `disposition` only when the
story contains an actual declaration for that merge.

A bare canonical story ID anywhere in a commit subject counts as a story match. This
includes prose and generated subjects: `Revert "fix(9.7): ..."` and
`see 9.7 for context` both match Story 9.7. If such a commit touches an unlisted path,
it is `interleaved` and a `shared` or `process` declaration cannot suppress the failure.
Authors should therefore keep unrelated subjects free of another story's bare ID and
inspect generated revert subjects before publishing them.

## Story identity

The frontmatter `story_id` is authoritative. It must hold exactly two numeric segments
separated by `.` or `-`, and it must not be empty: a blank value fails instead of
falling back to inference. Zero padding is normalized, so `09-07` and `9.7` are the same
identity and either spelling matches a commit subject.

An explicit `story_id` is cross-checked against the identity the document already
carries in its filename, `title`, and first H1. A contradiction fails closed rather than
silently reclassifying the whole range.

Without an explicit `story_id`, the identity is inferred from those same three sources
and any disagreement between them fails closed.

Identity is parsed for every run, including legacy mode, which never uses the resulting
ID. A malformed or self-contradicting `title`, H1, or filename therefore fails a legacy
run too. Fix the identity, or delete the contradicting text, rather than switching modes.

## What counts as document structure

Only real Markdown outside YAML frontmatter, fenced blocks, and four-space indented
examples is parsed. An example may therefore show a `## File List`, a declaration row, or
a checked task without injecting one.

Checked tasks are read from any list marker (`-`, `*`, `+`, `1.`) under a heading named
`Tasks`, `Tasks / Subtasks`, or `Tasks & Acceptance` at levels 2 to 6, with or without a
trailing suffix. A level-1 heading is the document title, not a task section. Nested subsections stay inside the open task section; a `Review Findings`
subsection and its descendants are excluded. A checked item outside any recognized task
section is reported as a notice, and checked work that no recognized section collected
fails closed -- naming whether no heading matched, or whether one matched but the checked
items sit outside it.

An unterminated fenced code block, unterminated frontmatter, and a story artifact that
cannot be read as UTF-8 are each reported as their own failure rather than surfacing as
a missing File List.

## Commit Scope Dispositions grammar

Put optional declarations under the exact `## Commit Scope Dispositions` heading. Each
declaration is one Markdown bullet with this grammar:

```text
- `<full-40-character-SHA>` | `shared` | <non-empty reason>
```

Use `process` in place of `shared` when the reason describes workflow-only work. The SHA
must be canonical, in the validated range, and declared only once. Multiple reasoned
`shared` or `process` rows are allowed. A disposition never hides the commit or its paths,
never grants ownership, and never suppresses an `interleaved` failure.

The heading is exact, including its level: a `Commit Scope Dispositions` heading at any
other level is reported rather than honoured. Inside the section, a line carrying the `|`
delimiter or a full SHA is a declaration and must satisfy the grammar exactly; anything
else is explanatory prose and is ignored.

A listed path that only a declared `shared` or `process` commit touched is explained by
that declaration: it is reported as `listed-unowned` and is not also reported as a File
List entry without a matching change.

`bootstrap-owned` is not a general grammar extension. It is accepted only when the
validator itself authorizes the exact artifact, story ID, baseline, commit topology,
guard paths, and immutable path inventory. Copying a declaration cannot create that
authorization.

Excluded paths — the defaults and any `--exclude` pattern — bound committed and
workspace paths alike. An excluded path stays visible in the report and contributes no
reconciliation evidence in either half.

Merge commits are listed separately. The current gate does not derive merge-resolution
paths from merge commits; that limitation remains tracked as deferred work and must not
be mistaken for whole-merge ownership.
