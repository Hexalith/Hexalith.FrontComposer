---
title: 'Story 16.1: Capture the Current EventStore Runtime Tuple'
type: 'chore'
created: '2026-10-30'
status: 'in-progress'
route: 'dispatch'
story_id: '16.1'
story_key: '16-1-i-evt-id-1-capture-the-current-eventstore-runtime-tuple'
baseline_commit: '92eff2e7b1b0f53631c53fa8284b466e202ae921'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-16-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** G-3 has no record of the tuple currently tested: FrontComposer commit, EventStore source gitlink, Builds gitlink, and the catalog-owned EventStore package version. Without one, no release candidate can be compared against live compatibility evidence, and the 2026-09-22 snapshot (`db1e9d73…`/`3.106.0`/`2fba3497…`) is history.

**Approach:** Choose one FrontComposer commit that has a passing Quality run. Read its four identities directly from git and the Builds catalog at that commit. Record them, the run link, and the Gate 2c and `epic9-live-acceptance` step results in a dated, immutable `## Tuple Record` section of this spec. Only this spec and `sprint-status.yaml` are committed.

## Boundaries & Constraints

**Always:** use full 40-character lowercase SHAs, and read gitlinks with `git ls-tree <tuple-sha>` rather than from the submodule checkouts. Read the catalog value at the Builds gitlink commit with `git show`. The record states that it claims no migration approval, carries no sign-off, and leaves G-3 open. Judge the run by individual step conclusions, never by the overall run status. The tuple SHA is the commit the Quality run tested; the later commit that adds this record is not the tuple, and release review compares the candidate against the tuple SHA.

**Never:** move gitlinks, edit submodule files, initialize nested submodules, or pin or override package versions to make a tuple match. Never edit a written `## Tuple Record` (drift adds a new, later-dated record). Never cite runs from Story 11.24, 11.25, or v3, or from any other tuple. Never add an identity schema, validator, evidence packet, ledger, or bundle. Never copy raw logs, tokens, or payloads into the record.

**Decisions (human, 2026-10-30):**
- *Gate 2c blocker — wait.* On HEAD `92eff2e7`, Quality `build-and-test` fails at "Prepare exact governance catalog objects" (`consumer source projection owner pin changed`). The cause is `eng/dependency-graph-policy.json`, which seals Platform at `a8e6276b…` (Story 15.5), while the gitlink has moved to `deee5d2f…`; Gate 2c is skipped as a result. 16.1 neither reseals nor works around this. Implementation waits until a Quality run on some FrontComposer commit has all Gate 2c and `epic9-live-acceptance` steps `success`; that commit becomes the tuple SHA. No blocked-attempt record is committed.
- *Package version — record both.* The tuple's catalog-owned field is the `HexalithEventStoreVersion` default in the Builds catalog at the tuple's Builds gitlink (`3.128.4` at `0b17f802…`). The record also states the effective version selected by the root `Directory.Packages.props` preset (`3.128.2` at baseline, set in `a54b5c8d`) and flags any difference as a divergence for the 16.3 approvers. 16.1 does not change the preset.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Green tuple | Quality run on tuple SHA: all 4 Gate 2c steps + `epic9-live-acceptance` steps `success` | Tuple Record written with run URL and per-step results/counts | N/A |
| Gate 2c skipped or failed | Earlier build-and-test step red (e.g. run `41081954759`) | No passing record; story cannot reach review | Report run, step, and error; story stays in progress |
| Drift after recording | HEAD, either gitlink, or catalog value changes | Prior record untouched; any new capture appends a new record | Never edit the earlier record |

</frozen-after-approval>

## Code Map

- `.github/workflows/quality.yml:220-268` -- Gate 2c steps: Contract pacts, Live EventStore provider verification, Validate contract artifacts, Fail on stale pact diff.
- `.github/workflows/quality.yml:837-936` -- `epic9-live-acceptance` job: the live proof (`FC_EPIC9_EXPECTED_COMMIT=github.sha`) and the invalid-bearer AppHost check.
- `eng/dependency_graph.py:321-327` + `eng/dependency-graph-policy.json` `consumer_source_projections` -- the Platform owner pin behind the current Gate 2c skip (read-only for 16.1; owned by Story 15.5).
- `Directory.Packages.props:6` -- root `HexalithEventStoreVersion` preset (`3.128.2`).
- `references/Hexalith.Builds` @ gitlink `Props/Directory.Packages.props:9` -- catalog-owned default (`3.128.4`).
- Current values at baseline: FrontComposer `92eff2e7b1b0f53631c53fa8284b466e202ae921`, EventStore `7b1df62102efb19da03553a890c62eb5ad6aeafe`, Builds `0b17f8022cdc23b8b242b000f5b2aa15580184ac` (checkouts match the gitlinks).
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- `16-1-…` row and `epic-16` (both `backlog`).

## Tasks & Acceptance

**Execution:**
- [ ] Pick as the tuple SHA the newest FrontComposer `main` commit whose Quality run passes; if none passes yet, stop here and report the blocker. Then run `gh run list --workflow quality.yml --commit <sha>` and `gh run view <id> --json jobs`. Confirm that every Gate 2c step and every `epic9-live-acceptance` step concluded `success`. Take the provider-verification pass/fail counts from the job log.
- [ ] Capture the identities with `git rev-parse`, `git ls-tree <sha> references/Hexalith.EventStore references/Hexalith.Builds`, `git show <sha>:Directory.Packages.props`, and `git -C references/Hexalith.Builds show <builds-sha>:Props/Directory.Packages.props`.
- [ ] `spec-16-1-…md` -- append the dated `## Tuple Record`: the four identities, the effective package preset and any divergence from the catalog, run URL, step results and counts, the commands used, and "Migration approval: not claimed · Sign-off: none · G-3: open".
- [ ] `sprint-status.yaml` -- set `epic-16` to `in-progress` and the 16-1 row to the story's current state. Keep CRLF line endings.
- [ ] Add `epic-16-context.md`, this spec, and `sprint-status.yaml` to a `## File List`, then commit them in a story-ID commit validated with commitlint.

**Acceptance Criteria:**
- Given the Tuple Record, when each SHA is compared with `git ls-tree`/`git rev-parse` at the tuple SHA, then all of them match exactly.
- Given the cited run, when its jobs are inspected, then its `headSha` equals the tuple SHA and every Gate 2c and `epic9-live-acceptance` step is `success`.
- Given the committed diff, when it is inspected, then it touches only `_bmad-output/` record files, adds no schema, validator, or evidence file, and leaves earlier records byte-identical.

## Implementation Notes

- **2026-10-30, attempt 1, blocked; nothing recorded.** Every Quality run on `main` after 2026-10-28 skipped all four Gate 2c steps. On HEAD `2255fb8a` (run `41090740352`) and baseline `92eff2e7` (run `41081954759`), `build-and-test` fails at "Prepare exact governance catalog objects": the policy seals Platform at `a8e6276b…` (`eng/dependency-graph-policy.json`), but the gitlink is `deee5d2f…`. `epic9-live-acceptance` was green, but that alone does not qualify.
- **Human decision (2026-10-30): a stale green commit is not the tuple.** `1ccb4afc47ec8f1bb5379fd98452242546bbd06c` (run `40619281927`) passed every Gate 2c and `epic9-live-acceptance` step, but it is 46 commits behind HEAD. Its EventStore (`b62a5f4b…`), Builds (`4250edc9…`) and catalog (`3.128.3`) identities have all changed since. The human chose to wait: the tuple SHA must be a current `main` commit, meaning one whose gitlinks and catalog value equal those HEAD selects when the record is written. Resume once the Story 15.5 Platform reseal lands and that commit's Quality run is green.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `git cat-file -t <tuple-sha>` and `git ls-tree <tuple-sha> references/Hexalith.EventStore references/Hexalith.Builds` -- expected: commit and the recorded gitlinks.
- `gh run view <run-id> --json headSha,jobs` -- expected: headSha = tuple SHA; required steps `success`.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-1-capture-the-current-eventstore-runtime-tuple.md` -- expected: pass.
