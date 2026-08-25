---
---

# Step 5: Present

## RULES

- **Language** — Speak in `{{.communication_language}}`. Write any file output in `{{.document_output_language}}`.
- NEVER auto-push.

## INSTRUCTIONS

### Generate Suggested Review Order

Read `{baseline_commit}` from `{spec_file}` frontmatter and construct the diff of all changes since that commit.

Append the review order as a `## Suggested Review Order` section to `{spec_file}` **after the last existing section**. Do not modify the Code Map.

Build the trail as an ordered sequence of **stops** — clickable `path:line` references with brief framing — optimized for a human reviewer reading top-down to understand the change:

1. **Order by concern, not by file.** Group stops by the conceptual concern they address (e.g., "validation logic", "schema change", "UI binding"). A single file may appear under multiple concerns.
2. **Lead with the entry point** — the single highest-leverage file:line a reviewer should look at first to grasp the design intent.
3. **Inside each concern**, order stops from most important / architecturally interesting to supporting. Lightly bias toward higher-risk or boundary-crossing stops.
4. **End with peripherals** — tests, config, types, and other supporting changes come last.
5. **Every code reference is a clickable spec-file-relative link.** Compute each link target as a relative path from `{spec_file}`'s directory to the changed file. Format each stop as a markdown link: `[short-name:line](../../path/to/file.ts#L42)`. Use a `#L` line anchor. Use the file's basename (or shortest unambiguous suffix) plus line number as the link text. The relative path must be dynamically derived — never hardcode the depth.
6. **Each stop gets one ultra-concise line of framing** (≤15 words) — why this approach was chosen here and what it achieves in the context of the change. No paragraphs.

Format each stop as framing first, link on the next indented line:

```markdown
## Suggested Review Order

**{Concern name}**

- {one-line framing}
  [`file.ts:42`](../../src/path/to/file.ts#L42)

- {one-line framing}
  [`other.ts:17`](../../src/path/to/other.ts#L17)

**{Next concern}**

- {one-line framing}
  [`file.ts:88`](../../src/path/to/file.ts#L88)
```

> The `../../` prefix above is illustrative — compute the actual relative path from `{spec_file}`'s directory to each target file.

When there is only one concern, omit the bold label — just list the stops directly.

### Mark Spec Done

Before changing status, resolve the canonical story ID with the same precedence as
step-04 and repeat the same compatible gate selection so review-fix work is reconciled:

- With a resolved story ID, available version control, and a usable non-`NO_VCS`
  `{baseline_commit}`, run:

```bash
python3 eng/validate-story-artifacts.py --story {spec_file} --candidate HEAD
```

- For freeform specs, missing/`NO_VCS`/unresolvable baselines, or no-VCS execution, run:

```bash
python3 eng/validate-story-artifacts.py --story {spec_file}
```

HALT on either gate's non-zero exit. Do not promote the spec or sprint status around
the failure.

Change `{spec_file}` status to `done` in the frontmatter.

Follow `[[bmad-snapshot:sync-sprint-status.md]]` with `target_status` = `review`.

### Commit and Complete

If version control is available and the tree is dirty, stage only the reconciled
story-owned paths from the story File List and the passing validator report. Inspect the
staged name-status and staged diff before committing. Never use a blanket add, and
never stage a path from `Documented Unrelated Changes` / `Documented Unrelated
Workspace State` or a CLI `--unrelated` declaration.

Create a local commit with a conventional message derived from the spec title. For a
numbered story, include the canonical ID resolved by the validator — including a legacy
title/H1/filename resolution when frontmatter lacks `story_id` — in the commit subject
so implementation, review, and final transition remain attributable. A freeform spec
does not invent or require a story ID.

After the commit (or immediately when the tree was already clean), repeat the same
strict-or-legacy gate selected above. In strict mode this checks the new `HEAD`:

```bash
python3 eng/validate-story-artifacts.py --story {spec_file} --candidate HEAD
```

A non-zero exit is a hard completion blocker. Do not display a completion summary or
offer push/PR actions until the final-transition commit and the entire range pass.

{workflow.open_spec}

### Display Summary

Display summary of your work to the user, including the commit hash if one was created. Any file paths shown in conversation/terminal output must use CWD-relative format (no leading `/`) with `:line` notation (e.g., `src/path/file.ts:42`) for terminal clickability — the goal is to make paths clickable in terminal emulators.

Offer to push and/or create a pull request.

Workflow complete.

## On Complete

If anything appears below, follow it as the final terminal instruction before exiting; otherwise exit normally.

{workflow.on_complete}
