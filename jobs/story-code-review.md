# Recurring Job: BMAD Story Code Review

You are running a recurring BMAD code-review job for this repository. Each run executes **at most one** code review against a single story currently in `review` status, then stops. A later run advances the next story in a fresh context.

## Goal

Drain the queue of stories in `review` status by running the `bmad-code-review` skill against each one in order:

1. Pick the first `review` story whose review trace is missing or stale relative to its current code state.
2. Run `bmad-code-review` against the story, with the story file as the spec context.
3. Persist findings inside the story artifact, keep `sprint-status.yaml` consistent, and record the run.
4. Stop. The story's new status (`done`, `in-progress`, or unchanged on failure) is owned by the skill, not by this job.

## Single-Operation Rule

A single run executes **exactly one** of:

- code-review — full bmad-code-review pass + canonical trace recording inside the story artifact
- none — no eligible `review` story
- failed — pre-flight or skill-execution failure

After the operation completes (or aborts), stop immediately. Do not chain another review in the same context.

## Configuration

- `MAX_DIFF_LINES = 3000` — soft cap for the unified diff fed to review subagents. Beyond this the job chunks by file group as the skill instructs.
- `DIFF_BASELINE_FALLBACK_DAYS = 14` — look-back window when no `baseline_commit` is recorded and no commit-message match is found. The job uses `HEAD~N` covering this window as the baseline.
- `REVIEW_PROMPT_RETRY_LIMIT = 3` — max attempts to recognize a numbered/option HALT prompt in the bmad-code-review skill before aborting the run as `blocked`.
- `MAX_PATCHES_AUTO_APPLY = 0` — recurring runs **never** auto-apply patches. Patches are always recorded as action items (skill's option 2). Raise this only if the operator explicitly opts in by editing the job file.

## Inputs

- Sprint status: `_bmad-output/implementation-artifacts/sprint-status.yaml`
- Story artifacts root: `_bmad-output/implementation-artifacts`
- Deferred-work ledger: `_bmad-output/implementation-artifacts/deferred-work.md`
- Optional review trace root: `_bmad-output/implementation-artifacts/review-runs`
- Lessons ledger: `_bmad-output/process-notes/story-creation-lessons.md`
- Run log: `_bmad-output/process-notes/code-review-runs.log`

## Pre-Flight Checks

Before any selection, verify:

1. `sprint-status.yaml` exists and parses as YAML.
2. The artifacts root directory exists.
3. The lessons ledger exists and is readable.
4. The `deferred-work.md` ledger exists OR can be created (the skill writes deferred items there).
5. The `bmad-code-review` skill is installed and discoverable (slash-form `/bmad-code-review` or natural-language form must engage).
6. **Status–artifact consistency.** For each `development_status` entry whose key is neither `epic-*` nor `*-retrospective`, resolve the artifact using the **Story Artifact Resolution** rule. The status and the artifact must agree:
   - `status == backlog` ⇒ no artifact must exist for that key.
   - `status` in `{ready-for-dev, in-progress, review, done}` ⇒ an artifact must exist.
   - `status == blocked` is exempt (blocked stories may legitimately have a partial or no artifact pending human reconciliation).

   On any mismatch, abort with `operation: failed` and `notes: "status-artifact drift on {key}: status={status}, artifact={present|absent}"`. Do not auto-repair: a human must reconcile the yaml and the disk before the next run.

7. **Working tree cleanliness.** `git status --porcelain` must be empty. If there are unrelated uncommitted changes, abort with `operation: failed` and `notes: "working tree not clean — refusing to interleave review edits with unrelated changes"`. The job is allowed to **produce** edits, but it must not **start** alongside a dirty tree.

If any check fails, append a `failed` row to the run log, emit the structured output with `operation: failed` and a `notes` field naming the failed check, and stop.

## Story Artifact Resolution

For a story key `{name}`, the canonical artifact is the **first** match in this order:

1. `_bmad-output/implementation-artifacts/{name}.md`
2. `_bmad-output/implementation-artifacts/{name}/index.md`
3. Any `*.md` file inside `_bmad-output/implementation-artifacts/{name}/` (folder-based stories — scan all of them when reading or writing traces).

Trace evidence may live in any of these files; always scan them all before declaring evidence absent.

## Selection Algorithm

1. Read the sprint status. Iterate `development_status` entries in document order.
2. Drop entries whose key starts with `epic-` or ends with `-retrospective`.
3. Build `review_candidates` = entries whose status is exactly `review`, in document order.
4. Decide the operation by the first matching rule:
   - **A. code-review** — if `review_candidates` is non-empty AND the first candidate's review trace is missing or stale (see **Trace Freshness** below). Selected story = that first candidate.
   - **B. code-review (next candidate)** — if rule A's first candidate has a fresh trace, advance to the next candidate in `review_candidates` whose trace is missing or stale. Selected story = that candidate.
   - **C. none** — `review_candidates` empty, OR every candidate has a fresh trace (the skill apparently could not transition them out of `review`; surface this as a stalled-queue signal in `notes`).

If rule C fires because every `review` story has a fresh trace but is still in `review`, set `notes: "stalled review queue: {N} stories with fresh traces still in review status — human triage required"` and stop. Do not re-review.

## Trace Freshness

A story's review trace is considered **fresh** when **all** of the following hold:

1. A POSITIVE trace marker exists inside the story artifact (see **Trace Evidence Detection**).
2. The most recent dated POSITIVE trace marker is newer than the latest git commit that touched any file referenced by the story's "Files to create" / "Files to extend" tables (or, if those tables are absent, newer than the latest commit that modified the story artifact itself).

Otherwise the trace is **stale** (or missing) and the story is eligible for a new code-review pass.

Resolve "latest commit touching files" with:

```text
git log -1 --format=%cI -- <path1> <path2> ...
```

If the file list cannot be resolved, fall back to the most recent commit that mentions the story key (`git log -1 --grep "{story-key}" --format=%cI`); if that also fails, treat the trace as stale and proceed.

## Trace Evidence Detection

Read every `*.md` file resolved by **Story Artifact Resolution** for the story under consideration.

### Code review — POSITIVE trace

A trace counts as completed when **at least one** of these structured patterns matches inside the story artifact:

- A heading `### Review Findings` (or any heading containing the case-insensitive substring `review findings`) followed within 10 lines by an ISO date `YYYY-MM-DD`.
- A heading `## Code Review` (or any heading containing `code review`) followed within 10 lines by an ISO date.
- A line containing `bmad-code-review` followed within 3 lines by an ISO date.
- A change-log / dev-agent-record / review-round row containing `code-review` AND one of `applied | triaged | completed | resolved | recorded`.
- One or more bullets matching `[Review][Decision]`, `[Review][Patch]`, or `[Review][Defer]` with an ISO date present in the surrounding 20 lines.
- **Fallback only:** a file at `_bmad-output/implementation-artifacts/review-runs/{name}/code-review.md` (or `_bmad-output/implementation-artifacts/review-runs/{name}-code-review.md`) containing both an ISO date and a `final recommendation:` line.

### NEGATIVE phrases (never count as positive)

- `pending code review`
- `code review not yet run`
- Any sentence prefixed with `Recommendation:`, `Pending:`, or `TODO:` that mentions code review.

### Conflict resolution

If the same story shows both positive and negative patterns, the positive trace counts only when its dated occurrence is **strictly newer** than every negative phrase. Otherwise treat the trace as absent.

## Diff Source Resolution

The `bmad-code-review` skill needs a non-empty unified diff for the review subagents. Resolve the diff source for the selected story in this order — stop at the first that yields a non-empty diff:

1. **Story frontmatter `baseline_commit`.** If the story file has YAML frontmatter with a `baseline_commit:` field, use `git diff {baseline_commit}..HEAD -- <story-files>` where `<story-files>` is the union of paths in the story's "Files to create" + "Files to extend" tables.
2. **Commit-message grep.** Run `git log --grep "{story-key}" --format=%H --reverse`. If matches exist, use the parent of the oldest match as the baseline. Diff: `git diff {baseline}^..HEAD -- <story-files>`.
3. **File-mtime window.** From the story's referenced files, find the oldest commit in the last `DIFF_BASELINE_FALLBACK_DAYS` that touched any of them. Use its parent as baseline.
4. **Generic look-back.** `git diff HEAD~N..HEAD` where `N` is the count of commits in the last `DIFF_BASELINE_FALLBACK_DAYS`. Filter to story files.
5. **Abort.** If every strategy yields an empty diff, abort with `operation: failed` and `notes: "no diff resolvable for {story-key}: review skipped"`. Do not invent a diff.

If the resolved diff exceeds `MAX_DIFF_LINES`, chunk by directory group (e.g., `src/Contracts/...`, `src/Shell/...`, `src/SourceTools/...`, `tests/...`) and process the **first** group only this run. Record the remaining groups in the run output's `notes` so a follow-up run can continue.

## Operation: code-review

### Context preload

Before invoking the skill, read and summarize:

- The selected story artifact (canonical file plus every `*.md` in the story folder if folder-based).
- The current `sprint-status.yaml` entry for the story.
- The lessons ledger (full file).
- The first matching `project-context.md` discoverable in the repo, if present.
- The diff resolved by **Diff Source Resolution**.

### Invocation

```text
/bmad-code-review {story-name}
```

Fallback:

```text
bmad code review {story-name}
```

Example:

```text
/bmad-code-review 4-1-projection-role-hints-and-view-rendering
```

### Autonomous responses to the skill's HALT checkpoints

The `bmad-code-review` skill is interactive. The recurring job answers each HALT deterministically. Track `prompt_retry_count` per HALT — if a prompt is not recognized within `REVIEW_PROMPT_RETRY_LIMIT` interactions, abort the run with `operation: failed` and `notes: "code-review prompt {step} not recognized after {N} retries"`.

#### Step 1 — Gather Context

- **Tier 1/2 (explicit argument / recent conversation):** treated as the recurring job's preamble. Pass the selected `{story-name}` and the resolved diff source explicitly.
- **Tier 3 (sprint tracking):** if the skill asks "I found story X in `review` status. Proceed?", answer `Y` for the **already-selected** story key. If the skill enumerates multiple `review` stories (the `Multiple review stories` branch), reply with the number that matches the selected key. If the skill picks a different story than the one the job selected, abort with `operation: failed` and `notes: "skill diff-source picked {other-key} instead of {selected-key}"`.
- **"What do you want to review?" prompt:** answer with the resolution from **Diff Source Resolution**:
  - If we resolved a commit range, reply `Specific commit range` and supply `{baseline}..HEAD`.
  - If we resolved branch diff, reply `Branch diff` and supply the base branch.
  - If we have only uncommitted edits (rare), reply `Uncommitted changes`.
- **Spec context prompt** ("Is there a spec or story file…"): reply `yes` and provide the canonical story artifact path.
- **Diff-too-large warning:** if the diff exceeds `MAX_DIFF_LINES`, accept the chunk offer and review the **first** group only. Record remaining groups for follow-up.
- **Step 1 checkpoint** ("Present a summary before proceeding"): auto-confirm.

#### Step 2 — Review

Subagents run without conversation context. The job does **not** intervene during this step beyond passing the selected layers. If subagents are unavailable and the skill writes prompt files for human relay, abort with `operation: failed` and `notes: "subagents unavailable — recurring job cannot relay manual review prompts"`. A human must run the review interactively.

#### Step 4 — Present and Act

- **Step 4.4 (decision-needed):** for **every** `decision-needed` finding, defer it. When the skill asks for a one-line reason, reply exactly:

  ```text
  deferred — recurring code-review job records decisions for human resolution; see deferred-work.md
  ```

  Never silently apply a decision-needed finding. Never dismiss one.
- **Step 4.5 (patch handling):** reply `2` (Leave as action items) when `MAX_PATCHES_AUTO_APPLY == 0`. The patches are persisted in the story artifact as `[ ] [Review][Patch] …` bullets. The story status will transition to `in-progress` on the next sprint-status update step (the skill owns this).
- **Step 4.6 (sprint-status sync):** the skill owns this transition. Verify after the skill returns that `development_status[{story-key}]` is one of `done` or `in-progress` (or unchanged with a documented abort reason). If it is anything else, abort with `operation: failed` and `notes: "unexpected status {status} after code-review on {story-key}"`.
- **Step 4.7 (next steps):** reply `3` (Done). Do not chain another operation.

### Trace required fields

After the skill returns, verify the canonical trace was recorded inside the story artifact under a `### Review Findings` heading (or appended `## Code Review` section). The trace must contain at minimum:

- ISO date and time
- Selected story key
- Command/skill invocation used
- Diff baseline used (commit hash or range)
- Findings summary (count by category: decision-needed / patch / defer / dismissed)
- Final recommendation: `done` | `in-progress` | `blocked`

If the trace is missing a required field after the skill returns, append it manually to the story artifact before stopping. Do not modify findings — only fill metadata gaps.

### Fallback trace file

Create a fallback trace file **only** when the story artifact cannot safely be edited or the report exceeds what fits cleanly inline:

```text
_bmad-output/implementation-artifacts/review-runs/{story-name}/code-review-{ISO-date}.md
```

Stop immediately after the trace is verified.

## Sprint-Status Guardrail

Do not change a story's value in `development_status` during this job UNLESS:

- The `bmad-code-review` skill itself drove the change (Step 4.6) — accept it as authoritative.
- The job aborts the review with cause `blocked` — set the story to `blocked` and record `last_updated` with a one-line cause.

The bmad-code-review skill owns the `review → done` and `review → in-progress` transitions. Do not duplicate or override them.

## Final Git Sync

When the selected operation is finished and all story artifacts, sprint-status updates, the deferred-work ledger, traces, and run-log entries are written:

1. Review the working tree.
2. Stage and commit only files modified by this job: the story artifact(s), `sprint-status.yaml`, `deferred-work.md`, the run log, and any new files under `_bmad-output/implementation-artifacts/review-runs/{story-name}/`.
3. Push the commit to the current branch.

Do not mix unrelated user changes into the job commit. If unrelated changes are present (the working tree was clean at pre-flight, so any new unrelated content is suspicious), leave them untouched and report that only this job's changes were pushed.

## Failure Handling

Abort the run immediately when any of these occur:

- A pre-flight check failed.
- `sprint-status.yaml` is unparseable.
- The selected story artifact cannot be read.
- The skill invocation returns an error or refuses to engage with both the slash and natural-language forms.
- The diff source resolution exhausts all strategies without yielding a non-empty diff.
- The skill HALT-prompt loop exceeds `REVIEW_PROMPT_RETRY_LIMIT` for any prompt step.
- A subagent layer fails AND the remaining layers return zero findings (the skill itself surfaces this; treat the run as `failed` rather than reporting a clean review).
- The skill picks a different story than the job-selected key.
- A trace write fails (filesystem error).

On abort: leave `sprint-status.yaml` and the story artifact in a best-effort consistent state (do not partial-edit if avoidable), append a `failed` row to the run log, and emit the structured output with `operation: failed` and a `notes` field describing the cause. Do not retry inside the same context.

## Run Log

Append exactly one tab-separated line to `_bmad-output/process-notes/code-review-runs.log` per run:

```text
{ISO timestamp}\t{operation}\t{story-name|-}\t{result: ok|failed|none}\t{short-message}
```

The log is append-only and human-readable. Never rewrite or truncate it.

Suggested short-message content:

- `ok` — `"trace recorded; new status={status}; findings D={d}/P={p}/W={w}/R={r}"`
- `failed` — the abort cause, ≤ 120 characters
- `none` — `"no review-status stories"` or `"stalled queue: {N} fresh traces still in review"`

## Output

End the run with a single fenced YAML block summarizing what happened. Use literal `null` for absent values.

```yaml
operation: code-review | none | failed
selected_story: <story-name|null>
diff_baseline: <commit-hash-or-range|null>
diff_files_reviewed: <integer|null>
diff_chunked: <true|false>
remaining_chunks: <[list]|null>
trace_location: <path|null>
fallback_trace_file: <path|null>
findings_decision_needed: <integer|null>
findings_patch: <integer|null>
findings_defer: <integer|null>
findings_dismissed: <integer|null>
new_status: <done|in-progress|blocked|review|null>
patches_applied: <integer|null>
commit_hash_pushed: <hash|null>
deferred_or_blocking: <[list]>
notes: <string|null>
```

This block is the canonical machine-readable summary. The run log captures one row per run for cross-run tracking.
