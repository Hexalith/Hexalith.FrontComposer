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
- [x] Pick as the tuple SHA the newest FrontComposer `main` commit whose Quality run passes; if none passes yet, stop here and report the blocker. Then run `gh run list --workflow quality.yml --commit <sha>` and `gh run view <id> --json jobs`. Confirm that every Gate 2c step and every `epic9-live-acceptance` step concluded `success`. Take the provider-verification pass/fail counts from the job log.
- [x] Capture the identities with `git rev-parse`, `git ls-tree <sha> references/Hexalith.EventStore references/Hexalith.Builds`, `git show <sha>:Directory.Packages.props`, and `git -C references/Hexalith.Builds show <builds-sha>:Props/Directory.Packages.props`.
- [x] `spec-16-1-…md` -- append the dated `## Tuple Record`: the four identities, the effective package preset and any divergence from the catalog, run URL, step results and counts, the commands used, and "Migration approval: not claimed · Sign-off: none · G-3: open".
- [x] `sprint-status.yaml` -- set `epic-16` to `in-progress` and the 16-1 row to the story's current state. Keep CRLF line endings.
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

## File List

- `_bmad-output/implementation-artifacts/epic-16-context.md` — pre-existing context, read in full; unchanged and not included in the capture commit.
- `_bmad-output/implementation-artifacts/spec-16-1-capture-the-current-eventstore-runtime-tuple.md` — tuple identities, same-attempt step conclusions, counts, commands and story verification.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — current Story 16.1 capture progress; Epic 16 remains in progress and G-3 remains open.

## Documented Unrelated Changes

- `references/Hexalith.Builds` — pre-existing checkout drift; this capture reads the committed gitlink and catalog object only.
- `references/Hexalith.EventStore` — pre-existing checkout drift; this capture reads the committed gitlink only.
- `references/Hexalith.Memories` — unrelated checkout drift observed during capture; preserved.
- `references/Hexalith.Parties` — unrelated checkout drift observed during capture; preserved.
- `references/Hexalith.Platform` — unrelated checkout drift observed during capture; preserved.
- `references/Hexalith.Tenants` — pre-existing checkout drift; preserved.

## Tuple Record — 2026-11-06

**Migration approval: not claimed · Sign-off: none · G-3: open**

### Identities and current-selection check

| Identity | Recorded value |
|----------|----------------|
| FrontComposer tuple commit tested by Quality | `aa8578318971da04ce7d7bd07928d08c6b95bc1a` |
| EventStore source gitlink at that commit | `1be8dcf43347efaf007538f1cd652fc5488bc6e6` |
| Builds gitlink at that commit | `06a51f55bfe2e03a06e1121d94a41e98f1e3e251` |
| Catalog-owned `HexalithEventStoreVersion` default at that Builds commit | `4.1.0` |
| Root `Directory.Packages.props` preset at the tuple commit | None; the root imports the selected Builds catalog without an EventStore version override. |
| Effective EventStore package version selected by that import | `4.1.0`; no divergence from the catalog-owned default. |

The tuple commit is the newest hosted `main` commit at capture. Local `main` HEAD
is `4c24c287cde520631e960a322413bee804f7c3d4`, an ancestor of the tested commit.
`git ls-tree` at both commits selects exactly the EventStore and Builds identities
above, and their root package files are byte-identical. The selected Builds
catalog therefore also matches. Uncommitted dependency checkouts are not tuple
inputs. The baseline's `3.128.2` preset is absent at the captured commit; no preset
or package version was changed for this story.

The later commit that adds this record is not the tuple. Release review compares
its candidate against `aa8578318971da04ce7d7bd07928d08c6b95bc1a` and that commit's
selected gitlinks and catalog. Any later drift requires a new dated record and
matching Quality evidence; this section remains immutable.

### Quality run and Gate 2c

[Quality run 42141933127, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/42141933127/attempts/1)
was triggered by a push to `main` at
`aa8578318971da04ce7d7bd07928d08c6b95bc1a`, started 2026-11-06 01:09:43 UTC,
and completed 01:34:01 UTC. The run and both jobs report that exact `headSha`.
Qualification uses the individual conclusions below, rather than the overall run
conclusion.

[build-and-test job 127644096976](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/42141933127/job/127644096976)
belongs to attempt 1. Its earlier `Prepare exact governance catalog objects`
step 13 also concluded `success`; the previous Platform-owner-pin blocker no
longer skips these gates on this tuple.

| Step | Exact step name | Conclusion | Counts or result |
|------|-----------------|------------|------------------|
| 23 | Gate 2c: Contract pacts | `success` | Job log: 3 succeeded, 0 failed, 0 skipped. |
| 24 | Gate 2c: Live EventStore provider verification | `success` | Provider test job log: 80 passed, 0 failed, 0 errors, 0 skipped, 0 not run. Live report: 19 requested / 19 reported interactions, 19 passed, 0 failed. |
| 25 | Gate 2c: Validate contract artifacts | `success` | Existing CI summary: 19 interactions; provider report `PRESENT_AND_CLEAN`; redaction scan clean. |
| 26 | Gate 2c: Fail on stale pact diff | `success` | Source-owned Pact files have no stale diff. |

The live provider executable emits no interaction-count summary to the job log;
the 80-test pass/fail counts above are taken directly from that log. The separate
19-interaction result comes from this same attempt's existing
[contract-diagnostics artifact 13469365738](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/42141933127/artifacts/13469365738),
not from a historical tuple. Its `provider-verification.json` reports
`live-compatibility`, verdict `passed`, all 19 results `interaction.passed`,
19 provider states, 19 setup events and 19 teardown events. Completeness, host
startup, readiness, host shutdown and port closure are all true; reason codes
are empty. These are summary counts only; no report, raw log or payload is
committed.

### Every epic9-live-acceptance step

[epic9-live-acceptance job 127644096895](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/42141933127/job/127644096895)
belongs to the same run attempt 1 and tuple commit. All 22 API-reported steps,
including setup and cleanup, concluded `success`; zero failed or skipped.

| Step | Exact step name | Conclusion |
|------|-----------------|------------|
| 1 | Set up job | `success` |
| 2 | Run actions/checkout@9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0 | `success` |
| 3 | Initialize build submodules | `success` |
| 4 | Run Hexalith/Hexalith.Builds/Github/initialize-dotnet@main | `success` |
| 5 | Install source-resource compatibility SDK | `success` |
| 6 | Verify root and source-resource SDKs | `success` |
| 7 | Run actions/setup-node@48b55a011bda9f5d6aeb4c2d9c7362e8dae4041e | `success` |
| 8 | Install Playwright workspace dependencies | `success` |
| 9 | Install Chromium browser | `success` |
| 10 | Install Aspire CLI | `success` |
| 11 | Install Dapr CLI | `success` |
| 12 | Initialize Dapr runtime | `success` |
| 13 | Typecheck Epic 9 browser proof | `success` |
| 14 | Test Epic 9 evidence tooling | `success` |
| 15 | Run AppHost-discovered Epic 9 live proof | `success` |
| 16 | AppHost rejects invalid bearer tokens | `success` |
| 17 | Upload Epic 9 acceptance artifacts | `success` |
| 31 | Post Run actions/setup-node@48b55a011bda9f5d6aeb4c2d9c7362e8dae4041e | `success` |
| 32 | Post Install source-resource compatibility SDK | `success` |
| 33 | Post Run Hexalith/Hexalith.Builds/Github/initialize-dotnet@main | `success` |
| 34 | Post Run actions/checkout@9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0 | `success` |
| 35 | Complete job | `success` |

Step 15's log binds `FC_EPIC9_EXPECTED_COMMIT` to
`aa8578318971da04ce7d7bd07928d08c6b95bc1a`: the generated create/update browser
acceptance test passed (1 passed, 0 failed), and the live proof completed. Step
16's owned AppHost check rejected invalid bearers with HTTP 401 for all four
surfaces: command submission, command status, query execution and projection-hub
negotiation. No migration approval or role-equivalent sign-off is inferred.

### Capture commands

All identity reads below use explicit committed objects, independent of the
submodule checkouts. Run-attempt job metadata and logs were inspected read-only.

```bash
git rev-parse HEAD aa8578318971da04ce7d7bd07928d08c6b95bc1a
git cat-file -t aa8578318971da04ce7d7bd07928d08c6b95bc1a
git merge-base --is-ancestor HEAD aa8578318971da04ce7d7bd07928d08c6b95bc1a
git ls-tree aa8578318971da04ce7d7bd07928d08c6b95bc1a references/Hexalith.EventStore references/Hexalith.Builds
git ls-tree HEAD references/Hexalith.EventStore references/Hexalith.Builds
git show aa8578318971da04ce7d7bd07928d08c6b95bc1a:Directory.Packages.props
git -C references/Hexalith.Builds show 06a51f55bfe2e03a06e1121d94a41e98f1e3e251:Props/Directory.Packages.props
git diff HEAD aa8578318971da04ce7d7bd07928d08c6b95bc1a -- Directory.Packages.props references/Hexalith.EventStore references/Hexalith.Builds
gh api repos/Hexalith/Hexalith.FrontComposer/branches/main --jq .commit.sha
gh run list --workflow quality.yml --branch main --limit 30 --json databaseId,headSha,conclusion,status,createdAt,displayTitle,url
gh run list --workflow quality.yml --commit aa8578318971da04ce7d7bd07928d08c6b95bc1a --json databaseId,headSha,headBranch,conclusion,status,url,createdAt
gh run view 42141933127 --json headSha,headBranch,url,conclusion,jobs
gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/42141933127
gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/42141933127/attempts/1/jobs --paginate
gh api repos/Hexalith/Hexalith.FrontComposer/actions/jobs/127644096976/logs
gh api repos/Hexalith/Hexalith.FrontComposer/actions/jobs/127644096895/logs
gh api repos/Hexalith/Hexalith.FrontComposer/actions/artifacts/13469365738/zip
```

`gh run view 42141933127 --job 127644096976 --log` returned zero bytes locally;
the read-only job-logs API supplied the counts instead. The existing artifact ZIP
was parsed in memory for its summary fields; no evidence file was added to the
repository. No runtime, package preset, source gitlink or catalog was changed.
