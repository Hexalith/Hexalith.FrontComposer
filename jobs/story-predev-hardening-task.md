# Recurring Job: BMAD Story Pre-Dev Hardening

You are running a recurring BMAD pre-development hardening job for this repository. Each run executes **at most one** heavy BMAD operation, then stops. A later run advances the next step in a fresh context.

## Goal

Maintain a buffer of at least two `ready-for-dev` stories, then harden them in order:

1. Create the next backlog story when the `ready-for-dev` buffer is below the target.
2. Run BMAD party-mode review on a ready story that has no completed party-mode trace.
3. Run BMAD advanced elicitation on a ready story that has a party-mode trace but no completed elicitation trace.

## Single-Operation Rule

A single run executes **exactly one** of:

- create-story
- party-mode-review (party-mode round + canonical trace recording)
- advanced-elicitation (two-batch interaction + canonical trace recording)

After the operation completes (or aborts), stop immediately. Do not chain another operation in the same context.

## Configuration

- `READY_BUFFER_TARGET = 5` — minimum count of `ready-for-dev` stories to maintain.
- `ELICITATION_PROMPT_RETRY_LIMIT = 3` — max attempts to recognize a `1-5, r, a, x` option prompt before aborting the elicitation as `blocked`.

## Inputs

- Sprint status: `_bmad-output/implementation-artifacts/sprint-status.yaml`
- Story artifacts root: `_bmad-output/implementation-artifacts`
- Optional review trace root: `_bmad-output/implementation-artifacts/review-runs`
- Lessons ledger: `_bmad-output/process-notes/story-creation-lessons.md`
- Run log: `_bmad-output/process-notes/predev-hardening-runs.log`

## Pre-Flight Checks

Before any selection, verify:

1. `sprint-status.yaml` exists and parses as YAML.
2. The artifacts root directory exists.
3. The lessons ledger exists and is readable.
4. **Status–artifact consistency.** For each `development_status` entry whose key is neither `epic-*` nor `*-retrospective`, resolve the artifact using the **Story Artifact Resolution** rule. The status and the artifact must agree:
   - `status == backlog` ⇒ no artifact must exist for that key.
   - `status` in `{ready-for-dev, in-progress, review, done}` ⇒ an artifact must exist.
   - `status == blocked` is exempt (blocked stories may legitimately have a partial or no artifact pending human reconciliation).

   On any mismatch, abort with `operation: failed` and `notes: "status-artifact drift on {key}: status={status}, artifact={present|absent}"`. Do not auto-repair: a human must reconcile the yaml and the disk before the next run. This protects against silent overwrite when a prior partial run, a `git reset`, or a manual edit leaves yaml and disk out of sync.

If any check fails, append a `failed` row to the run log, emit the structured output with `operation: failed` and a `notes` field naming the failed check, and stop.

## Story Artifact Resolution

For a story key `{name}`, the canonical artifact is the **first** match in this order:

1. `_bmad-output/implementation-artifacts/{name}.md`
2. `_bmad-output/implementation-artifacts/{name}/index.md`
3. Any `*.md` file inside `_bmad-output/implementation-artifacts/{name}/` (folder-based stories — scan all of them when reading or writing traces)

Trace evidence may live in any of these files; always scan them all before declaring evidence absent.

## Selection Algorithm

1. Read the sprint status. Iterate `development_status` entries in document order.
2. Drop entries whose key starts with `epic-` or ends with `-retrospective`.
3. Compute `ready_count` = entries whose status is exactly `ready-for-dev`.
4. Decide the operation by the first matching rule:
   - **A. create-story** — if `ready_count < READY_BUFFER_TARGET` AND at least one entry has status `backlog`. Selected story = first `backlog` entry in document order.
   - **B. party-mode-review** — else, if any `ready-for-dev` entry has no completed party-mode trace. Selected story = the first such entry in document order.
   - **C. advanced-elicitation** — else, if any `ready-for-dev` entry has a completed party-mode trace AND no completed advanced-elicitation trace. Selected story = the first such entry in document order.
   - **D. none** — otherwise. Emit the structured output with `operation: none` and stop.

If rule A's threshold is met but no `backlog` entry exists, fall through to rule B with the available ready stories and set `notes: "no backlog story available to create"` in the final output.

## Trace Evidence Detection

Read every `*.md` file resolved by **Story Artifact Resolution** for the story under consideration.

### Party-mode review — POSITIVE trace

A trace counts as completed when **at least one** of these structured patterns matches:

- A heading `## Party-Mode Review` (or any heading containing the case-insensitive substring `party-mode review`) followed within 10 lines by an ISO date `YYYY-MM-DD`.
- A line containing `bmad-party-mode` or `party-mode review` followed within 3 lines by an ISO date.
- A change-log / dev-agent-record / review-round row containing `party-mode` AND one of `applied | triaged | completed | resolved`.
- **Fallback only:** a file at `_bmad-output/implementation-artifacts/review-runs/{name}-party-mode-review.md` containing both an ISO date and a `final recommendation:` line.

### Advanced elicitation — POSITIVE trace

- A heading `## Advanced Elicitation` (or any heading containing `advanced elicitation`) followed within 10 lines by an ISO date.
- A line containing `bmad-advanced-elicitation` followed within 3 lines by an ISO date.
- One of: `Advanced elicitation pass applied`, `advanced-elicitation findings applied`.
- A change-log / dev-agent-record / review-round row containing `advanced-elicitation` AND one of `applied | triaged | completed | resolved`.

### NEGATIVE phrases (never count as positive)

- `has NOT yet been reviewed`
- `recommended flow`
- `run party mode first`
- `run advanced elicitation`
- Any sentence prefixed with `Recommendation:`, `Pending:`, or `TODO:`.

### Conflict resolution

If the same story shows both positive and negative patterns, the positive trace counts only when its dated occurrence is **strictly newer** than every negative phrase. Otherwise treat the trace as absent.

## Operation: create-story

Invoke the skill (slash form is the default — natural-language is a fallback if the skill does not engage):

```text
/bmad-create-story {story-name}
```

Fallback:

```text
bmad create story {story-name}
```

Example:

```text
/bmad-create-story 5-2-http-response-handling-and-etag-caching
```

After the skill returns, stop immediately. Do not run party mode or elicitation in the same context. Do not edit `sprint-status.yaml` directly — the skill owns that update.

## Operation: party-mode-review

### Context preload

Before invoking the skill, read and summarize for the agents:

- The selected story artifact (canonical file plus every `*.md` in the story folder if folder-based).
- The current `sprint-status.yaml` entry for the story.
- The lessons ledger, with emphasis on **L08 — party review vs. elicitation**.
- The first matching `project-context.md` discoverable in the repo.

### Invocation

```text
/bmad-party-mode {story-name}; review;
```

Fallback:

```text
bmad party mode {story-name}; review;
```

### Review focus areas

- Architecture and cross-story contract risk
- Missing or ambiguous acceptance criteria
- Scope creep and decision-budget pressure (L06)
- Test strategy gaps
- Accessibility, localization, adopter-experience risk
- Implementation traps to clarify before `bmad-dev-story`

### After the round

1. Capture findings.
2. Record the canonical trace inside the story artifact. Prefer an existing `## Change Log` / `## Dev Agent Record` / `## Review Round` section. If none exists, append a concise `## Party-Mode Review` section.
3. Apply findings inline only when they are coherent, low-risk, and do not change product scope, architecture policy, or cross-story contracts.
4. Findings that require human product or architecture judgment must be recorded as **deferred decisions**, not silently applied.
5. Create a fallback trace file **only** when the story artifact cannot safely be edited or the report exceeds what fits cleanly inline:

   ```text
   _bmad-output/implementation-artifacts/review-runs/{story-name}-party-mode-review.md
   ```

### Trace required fields

- ISO date and time
- Selected story key
- Command/skill invocation used
- Participating BMAD agents
- Findings summary
- Changes applied
- Findings deferred
- Final recommendation: `ready-for-dev` | `needs-story-update` | `blocked`

Stop immediately after the trace is recorded.

## Operation: advanced-elicitation

### Context preload

Before invoking the skill, read and summarize:

- The selected story artifact (all `*.md` if folder-based).
- The current `sprint-status.yaml` entry for the story.
- Any party-mode review notes already inside the story artifact.
- The lessons ledger, with emphasis on **L08**.

### Invocation

```text
/bmad-advanced-elicitation {story-name}
```

Fallback:

```text
bmad advanced elicitations {story-name}
```

The content being enhanced is the selected story artifact. Focus areas: security, robustness, edge cases, hidden coupling, cross-story contract drift, missing decision points, ambiguous acceptance criteria, test strategy gaps, accessibility / localization / adopter UX failure modes, over-engineering or unnecessary scope.

### Interaction sequence — exactly two batches

Run a defensive loop. Track `prompt_retry_count`. If a `1-5, r, a, x` option prompt is not recognized within `ELICITATION_PROMPT_RETRY_LIMIT` interactions for any single step, abort the run with `final recommendation: blocked` and record the abort cause in the trace.

#### Batch 1

1. On the first `1-5, r, a, x` prompt, reply exactly:

   ```text
   1-5
   ```

   If the skill rejects the range, retry once with:

   ```text
   1,2,3,4,5
   ```

2. Execute the five methods in sequence against the current story content.
3. On the apply prompt, answer yes and apply changes that are coherent, low-risk, and grounded in the story. Any proposal that changes product scope, architecture policy, or cross-story contracts must be recorded as a deferred decision instead of being silently applied.

#### Reshuffle

4. On the next `1-5, r, a, x` prompt, reply exactly:

   ```text
   r
   ```

5. Wait for the reshuffled list of five new methods.

#### Batch 2

6. On the reshuffled `1-5, r, a, x` prompt, reply exactly:

   ```text
   1-5
   ```

   (Same `1,2,3,4,5` fallback applies.)

7. Execute the second five methods in sequence against the already-enhanced story content.
8. Apply selectively as in Batch 1; defer scope/architecture/contract changes.

#### Completion

9. On the next `1-5, r, a, x` prompt, after all accepted changes are applied, reply exactly:

   ```text
   x
   ```

10. Capture the final story content.

### After elicitation

1. Persist accepted improvements in the story artifact.
2. Record the canonical trace inside the story artifact. Prefer an existing `## Change Log` / `## Dev Agent Record` / `## Review Round` section. Otherwise append a concise `## Advanced Elicitation` section.
3. Trace required fields:
   - ISO date and time
   - Selected story key
   - Command/skill invocation used
   - Batch 1 method names
   - Reshuffled Batch 2 method names
   - Findings summary
   - Changes applied
   - Findings deferred
   - Final recommendation: `ready-for-dev` | `needs-story-update` | `blocked`

Stop immediately after the trace is recorded.

## Sprint-Status Guardrail

Do not change a story's value in `development_status` during this job UNLESS the operation explicitly determines:

- `blocked` — set the story to `blocked` and record `last_updated` with a one-line cause.
- `needs-story-update` requiring scope rework that invalidates ready state — set the story back to `backlog` and record `last_updated` with a one-line cause.

Otherwise leave the status untouched. The `bmad-create-story` skill owns the `backlog → ready-for-dev` transition; do not duplicate it.

## Final Git Sync

When the selected operation is finished and all story artifacts, sprint status updates, traces, and run-log entries are written:

1. Review the working tree.
2. Commit the changes produced by this job.
3. Push the commit to the current branch.

Do not mix unrelated user changes into the job commit. If unrelated changes are present, leave them untouched and report that only this job's changes were pushed.

## Failure Handling

Abort the run immediately when any of these occur:

- A pre-flight check failed.
- `sprint-status.yaml` is unparseable.
- The selected story artifact cannot be read.
- A skill invocation returns an error or refuses to engage with both the slash and natural-language forms.
- The elicitation interaction loop exceeds `ELICITATION_PROMPT_RETRY_LIMIT` for any prompt step.
- A trace write fails (filesystem error).

On abort: leave `sprint-status.yaml` and the story artifact in a best-effort consistent state (do not partial-edit if avoidable), append a `failed` row to the run log, and emit the structured output with `operation: failed` and a `notes` field describing the cause. Do not retry inside the same context.

## Run Log

Append exactly one tab-separated line to `_bmad-output/process-notes/predev-hardening-runs.log` per run:

```text
{ISO timestamp}\t{operation}\t{story-name|-}\t{result: ok|failed|none}\t{short-message}
```

The log is append-only and human-readable. Never rewrite or truncate it.

## Output

End the run with a single fenced YAML block summarizing what happened. Use literal `null` for absent values.

```yaml
operation: create-story | party-mode-review | advanced-elicitation | none | failed
story_created: <story-name|null>
selected_story: <story-name|null>
trace_location: <path|null>
fallback_trace_file: <path|null>
batch_1_methods: <[list]|null>
batch_2_methods: <[list]|null>
commit_hash_pushed: <hash|null>
changes_applied: <[list]>
deferred_or_blocking: <[list]>
notes: <string|null>
```

This block is the canonical machine-readable summary. The run log captures one row per run for cross-run tracking.
