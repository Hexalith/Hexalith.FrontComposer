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

The strict report keeps commit classification separate from path ownership:

For compatibility, each non-merge row renders its classification through the stable
`disposition=<classification>` text field. A merge row uses `disposition` only when the
story contains an actual declaration for that merge.

A bare canonical story ID anywhere in a commit subject counts as a story match. This
includes prose and generated subjects: `Revert "fix(9.7): ..."` and
`see 9.7 for context` both match Story 9.7. If such a commit touches an unlisted path,
it is `interleaved` and a `shared` or `process` declaration cannot suppress the failure.
Authors should therefore keep unrelated subjects free of another story's bare ID and
inspect generated revert subjects before publishing them.

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

`bootstrap-owned` is not a general grammar extension. It is accepted only when the
validator itself authorizes the exact artifact, story ID, baseline, commit topology,
guard paths, and immutable path inventory. Copying a declaration cannot create that
authorization.

Merge commits are listed separately. The current gate does not derive merge-resolution
paths from merge commits; that limitation remains tracked as deferred work and must not
be mistaken for whole-merge ownership.
