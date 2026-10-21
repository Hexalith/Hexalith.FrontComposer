---
title: "Story artifact validation"
description: "Reference for story IDs, commit-scope dispositions, path labels, and strict reconciliation."
genre: reference
audience: framework-contributor
ownerStory: 9-7-add-story-id-and-commit-scope-evidence
status: published
reviewed: 2026-10-21
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

## Repository integrity mode

Running the validator without `--story` keeps the authoring-sentinel scan and also
validates the maintained Epic 11 corpus. Once either maintained planning data or an
Epic 11 implementation artifact is present, these inputs are mandatory:

- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/epics.md`
- both containing artifact directories

The pass is standard-library-only and fail-closed. It validates the independently
declared 11.17a-d, 11.18a-c, and 11.19a-d child manifest; rejects implementable parent
queue rows; and requires child artifact, queue, and planning status to agree. Every
active Epic 11 artifact needs one supported lifecycle status and exactly one matching
queue row. Explicit planning statuses must also map to exactly one row and agree.
Missing inputs, malformed or duplicate rows, unsupported statuses, and ambiguous
mappings are errors rather than reasons to skip validation.

Every declared `epic-<number>` row also agrees with its numeric child queue rows:
`done` requires every child to be `done`, and `backlog` requires every child to
remain `backlog`. A `done` epic must have at least one numeric child row. Epic
statuses are limited to `backlog`, `in-progress`, and `done`; story-only or
retrospective-only statuses fail on an epic. An `in-progress` epic may contain
completed children while it awaits closure. Numeric identities ignore leading
zeros and letter-suffix case. Retrospective rows and approval actions do not count
as numeric children. Diagnostics name the epic and contradicting child in stable
key order.

Done artifacts may not retain unchecked work under `Tasks` or `Review Findings`.
Disposition labels such as `Defer`, `Dismiss`, `Supersede`, and `Reopen` do not resolve
an unchecked row; record the disposition and check the row. Review scope ends at a peer
or higher heading, so an unrelated checklist after the review section is not captured.

Only one active artifact may represent a Story. A historical shadow must use
`status: superseded` and name a different repository-relative `superseded_by` file
under `implementation-artifacts`. The successor must be an active artifact for the
same Story; self-links, missing files, evidence documents, different Story identities,
and chains to another superseded record fail.

A done artifact's optional `final_revision` must be a canonical lowercase 40-character
commit SHA, resolve to a commit, and be an ancestor of `HEAD`. The commit must change at
least one non-artifact delivery path declared by that artifact's `Code Map` or
`File List`; a Story ID in the subject or an artifact-only bookkeeping commit is not
delivery evidence. Root commits are inspected with the same rule.

The eight `E11R-AI-*` records are also checked for unique integrity-bearing fields,
`epic: 11`, their exact implementation-Story mapping, supported `open`/`done` status,
an ISO close date only when done, resolving evidence, and an evidence link to the
active matching Story artifact. E11R-AI-1 stays open while its successor evidence is
not done, and E11R-AI-8 stays open until Story 11.32 itself is done.

Recovery is corrective, not suppressive: restore a missing ledger or child, reconcile
the artifact/queue/planning status, replace stale revisions with reachable delivery
commits, explicitly supersede a shadow, or disposition and check unresolved work. The
repository gate has no per-story exclusion or known-bad bypass.

## Bounded historical inventory validation

Story 11.32's delivered scope is the 21-path Git diff from baseline
`00d4da4788692ee405a081884e78482a9556b0c3` through delivery
`9aad029f837b8ecbd9f58e6f89ffdd76bf403bcf`. From the repository root, replay that
inventory using existing explicit-path mode:

```bash
python3 - <<'PY'
import subprocess

baseline = "00d4da4788692ee405a081884e78482a9556b0c3"
delivery = "9aad029f837b8ecbd9f58e6f89ffdd76bf403bcf"
paths = subprocess.check_output(
    ["git", "diff", "--name-only", "-z", baseline, delivery], text=True
).split("\0")[:-1]
assert len(paths) == 21
command = [
    "python3", "eng/validate-story-artifacts.py", "--story",
    "_bmad-output/implementation-artifacts/spec-11-32-epic-11-artifact-integrity-enforcement.md",
]
for path in paths:
    command.extend(["--changed-file", path])
raise SystemExit(subprocess.run(command, check=False).returncode)
PY
```

The blocking unit suite separately asserts exact equality between that immutable
diff and the current File List, including entries with accepted-extra reasons.
This replay validates inventory; it grants no strict commit ownership. Run
`python3 eng/validate-story-artifacts.py` separately for current global integrity.

An old legacy invocation with only `--story` discovers changes from the original
baseline through the current workspace, so it includes later stories and unrelated
dirty paths. Its broader result is not the historical delivery inventory. Strict
and legacy discovery retain their existing meanings.

A missing or empty File List reports one contradiction naming the escaped relative
artifact path, independent of checkout root, baseline, or discovered changes.
A nonempty incomplete list still names the missing changes; unmatched entries
still fail under the existing accepted-extra rules.

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
| `historical-owned` | The separately maintainer-authorized Story 13.7 recovery for two exact commits. It is assigned by code, never by a story declaration. | Yes, immutable authorized listed paths only |
| `unrelated` | Subject does not match and no touched path is listed. | No |

The dedicated `historical-owned` authorization is checked before ordinary commit
classification. It is the sole exception for the two historical commits described
below; other matching commits with unlisted paths still fail as `interleaved`.

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

## Story 13.7 historical recovery

The maintainer approved a dedicated code-only recovery for the canonical artifact
`_bmad-output/implementation-artifacts/spec-13-7-provide-reusable-ux-assertions-for-adopters.md`.
Its declared and resolved baseline must both remain
`b9e1490cfe1c51406d3c8d4dc213971d06911a24`, with Story ID `13.7`.

- `7f57da411c3abf8e1d459d0611c49632c07393a7` is the original delivery, whose subject
  lacks the Story ID. Its sole parent is the baseline. All 44 touched paths must
  remain listed and are authorized as owned.
- `ee9762e6c74914ccd33ec1d2caf322bdc21a10b4` has a matching subject and sole parent
  `23fdfaf7257a1bcb089908b5636c613508dd0a7a`. Only its five Testing paths are owned:
  `TestAccessibilityEvidenceRecorder.cs`, `UxAccessibilityAssertions.cs`,
  `UxAnnouncementAssertions.cs`, and `UxAnnouncementCatalog.cs` under
  `src/Hexalith.FrontComposer.Testing/`, plus
  `tests/Hexalith.FrontComposer.Testing.Tests/TestingFailureModeTests.cs`.
  Its other 11 paths remain visible and unowned.

The `HISTORICAL_OWNED_*` constants in `eng/validate-story-artifacts.py` pin the full
path sets. Authorization checks each commit's exact identity, parent, subject-match
state, complete unfiltered touched-path inventory, and immutable File List
intersection. Missing owned entries or listing any historically unowned path refuses
the grant. Later File List entries that neither commit touched do not expand it.
Exclusions still bound reconciliation and never grant ownership.
An ordinary `shared` or `process` declaration cannot downgrade either authorized
commit; the report marks that declaration as ignored and retains historical ownership.

This adds no declaration grammar, CLI flag, or external registry. A
`historical-owned` story declaration is malformed. The Story 9.7 `bootstrap-owned`
authorization and all ordinary `shared`, `process`, and interleaving rules are
unchanged. Published history and the baseline are preserved.
