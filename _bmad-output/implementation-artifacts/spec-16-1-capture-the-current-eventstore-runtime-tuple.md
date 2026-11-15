---
title: 'Story 16.1: Capture the Current EventStore Runtime Tuple'
type: 'chore'
created: '2026-10-30'
status: 'done'
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
- [x] Add `epic-16-context.md`, this spec, and `sprint-status.yaml` to a `## File List`, then commit them in a story-ID commit validated with commitlint.

**Acceptance Criteria:**
- Given the Tuple Record, when each SHA is compared with `git ls-tree`/`git rev-parse` at the tuple SHA, then all of them match exactly.
- Given the cited run, when its jobs are inspected, then its `headSha` equals the tuple SHA and every Gate 2c and `epic9-live-acceptance` step is `success`.
- Given the committed diff, when it is inspected, then it touches only `_bmad-output/` record files, adds no schema, validator, or evidence file, and leaves earlier records byte-identical.

## Implementation Notes

- **2026-10-30, attempt 1, blocked; nothing recorded.** Every Quality run on `main` after 2026-10-28 skipped all four Gate 2c steps. On HEAD `2255fb8a` (run `41090740352`) and baseline `92eff2e7` (run `41081954759`), `build-and-test` fails at "Prepare exact governance catalog objects": the policy seals Platform at `a8e6276b…` (`eng/dependency-graph-policy.json`), but the gitlink is `deee5d2f…`. `epic9-live-acceptance` was green, but that alone does not qualify.
- **Human decision (2026-10-30): a stale green commit is not the tuple.** `1ccb4afc47ec8f1bb5379fd98452242546bbd06c` (run `40619281927`) passed every Gate 2c and `epic9-live-acceptance` step, but it is 46 commits behind HEAD. Its EventStore (`b62a5f4b…`), Builds (`4250edc9…`) and catalog (`3.128.3`) identities have all changed since. The human chose to wait: the tuple SHA must be a current `main` commit, meaning one whose gitlinks and catalog value equal those HEAD selects when the record is written. Resume once the Story 15.5 Platform reseal lands and that commit's Quality run is green.
- **2026-11-06, attempt 2, recorded.** Quality run `42141933127` attempt 1 passed every required step at `aa8578318971da04ce7d7bd07928d08c6b95bc1a`, then the newest hosted `main` commit, and the `## Tuple Record — 2026-11-06` below was written against it. The workspace `/pushall` sync committed that record, the File List and the sprint comment as `fbb9c3822dfa407e72739238cd472567f45f1e82` (`build: sync local changes via /pushall`, no story ID). The same commit moved six gitlinks, among them Builds `06a51f55bfe2e03a06e1121d94a41e98f1e3e251` → `7c7c1b9646821174e013f6fffe75bb36ebf09c77` and EventStore `1be8dcf43347efaf007538f1cd652fc5488bc6e6` → `eff9f6947a3762da61d10642f20d002123ccbe6a`. Story 16.1 did not make and does not own those moves. As the Boundaries require, the record commit is not the tuple: release review compares against `aa8578318971da04ce7d7bd07928d08c6b95bc1a`. The attempt-1 note commit `dd575b67c82c377bd17c34fb3d14366277d2ac62` (`fix: story 16.1`) likewise moved the Platform gitlink `8ff6db8feb5b3165bdaaf90899778199823bc59e` → `9e01ffacc45cb6a5ee12853d44f4b7f6e1f4084c`; that path is not story-owned (see Verification).
- **2026-11-15, completion pass; no new record.** The 2026-11-06 record is unchanged. A drift check at HEAD `60d2088c26e5cedb7c61471d2e4b813a86996ea2` (equal to `origin/main`) finds EventStore `fdb59895975e86a1e234325a01237f259cdbd53b`, Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee`, and catalog default `HexalithEventStoreVersion` `5.1.0` (`Props/Directory.Packages.props:149`; line 148 selects `4.2.0` only for the opt-in SDK 4.2 catalog views). The root `Directory.Packages.props` is byte-identical to the tuple's, with no preset. HEAD `60d2088c26e5cedb7c61471d2e4b813a86996ea2` is also not the tuple commit `aa8578318971da04ce7d7bd07928d08c6b95bc1a`, so all four tuple identities have drifted since the record. Every completed Quality run on a commit that selects them fails `build-and-test` step 14, "Prepare exact governance catalog objects": `bca9dff291ce42c57516598732340a80703dfb6d` (`43702436144`), `0d065fceb2bfca9999307cb864a3f93392fb3400` (`43710270227`), `b22be84bdbb23d9263a8835496ad0c95adf1843f` (`43728107585`) and HEAD (`43736184654`, completed 16:55 UTC). Each run reports `consumer source projection owner pin changed` for a Platform selection: `5a58ba20102847a3110a0a6c71430ec4d99157e5`, the gitlink at parent `09e5dd0fce32df9653365759d955e1e78df74ed7`, in the first run, and HEAD's `f6f85032598865b9123f16364448bfd02c0ff5b0` in the later three. All four Gate 2c steps are skipped in each. `epic9-live-acceptance` is green on those runs, but that alone does not qualify. Under the frozen wait decision no current tuple can be captured, and no blocked-attempt record is added. The 2026-11-06 record stays unchanged as history, but its tuple has drifted. Under the Cross-Story Dependencies in `epic-16-context.md`, a tuple that drifts before approval must be captured again first. Story 16.3 must therefore wait for a new, later-dated capture on a current commit whose Gate 2c and `epic9-live-acceptance` steps pass. At the tuple, Gate 2c built and ran ProviderVerification from the `references/Hexalith.EventStore` source checkout (`.github/workflows/quality.yml:234-251` at the tuple commit). `epic9-live-acceptance` ran with `HexalithFrontComposerFromSource=true` (`quality.yml:927`, `eng/run-epic9-live-proof.sh:36`). The record therefore claims no package-mode runtime evidence for `4.1.0`. The 19-interaction count comes from `contract-diagnostics` artifact `13469365738`, which expires at `2026-11-20T01:22:41Z`. The 80-test count comes from the cited job logs, which expire 90 days after the run. Neither count can be re-checked from CI after those dates.
- **Routed Story 15.5 findings.** `deferred-work.md`, under "Deferred from: code review of spec-15-5-prepare-the-inactive-policy-precursor.md (2026-10-30)", routes precursor findings BH-R4–BH-R10 to the Story 16.1 owner. They are addressed as follows:
  - R4: the `## File List` is present.
  - R5: the strict `--candidate HEAD` run is recorded in Verification Results.
  - R6: the record carries the current-selection check.
  - R7: judged false at source; no change needed.
  - R8: two paths are committed, and `epic-16-context.md` is listed as unchanged.
  - R9: qualification uses per-step conclusions.
  - R10: the record binds attempt 1 and both job IDs.

## Historical scope exception receipt — 2026-11-15

The requesting user approved the repair spec and the Epic 16 course-correction proposal with “approve both and continue.” This is the recorded approval of the bounded historical engineering exception D1; no role credentials are inferred from that request.

AC 3 did not pass historically. Pushed `dd575b67c82c377bd17c34fb3d14366277d2ac62` moved Platform `8ff6db8feb5b3165bdaaf90899778199823bc59e` → `9e01ffacc45cb6a5ee12853d44f4b7f6e1f4084c`. Pushed `fbb9c3822dfa407e72739238cd472567f45f1e82` landed the record with six gitlink moves, including EventStore `1be8dcf43347efaf007538f1cd652fc5488bc6e6` → `eff9f6947a3762da61d10642f20d002123ccbe6a` and Builds `06a51f55bfe2e03a06e1121d94a41e98f1e3e251` → `7c7c1b9646821174e013f6fffe75bb36ebf09c77`. Their pushed history and the strict attribution failure remain intact; no validator bypass is supplied. New capture commits must contain only the declared documentation paths and must preserve the old Tuple Record byte-for-byte.

This receipt disposes only of that historical scope violation for engineering acceptance. It does not establish FrontComposer-maintainer, Architect, EventStore-maintainer or Release Owner credentials, grant migration approval, transfer approval authority, approve a descendant commit, or close G-3. Story 16.1 engineering acceptance of the new capture follows verification. The scheduling and candidate-lineage rules are adopted in the PRD, Epic 16, architecture and epic context under the approved proposal.

## Spec Change Log

## Review Triage Log

Iteration 1, 2026-11-15: Blind Hunter (BH), Edge Case Hunter (ECH) and Verification Gap (no gaps) over the story-owned diff (spec, `epic-16-context.md`, and the `sprint-status.yaml` hunks from `dd575b67`, `fbb9c382` and `38856d02`); the 223 unrelated commits since the baseline were out of review scope.

| Finding | Verdict | Evidence | Route |
|---|---|---|---|
| BH01 | low | The spec moved to `in-review` while the sprint row stays `in-progress`; the task asks the row to track the story's current state. Same defect as ECH04. | patch P9 |
| BH02 | medium | The 2026-11-15 note lets 16.3 "approve or reject it as recorded", but `epic-16-context.md` Cross-Story Dependencies requires a drifted tuple to be captured again before approval. | patch P1 |
| BH03 | false | The frozen wait condition (a green current `main` commit) was met on 2026-11-06 and is documented in the attempt-2 note; review needs no further human decision. The Spec Change Log records only bad_spec loopbacks, so empty is correct. | reject |
| BH04 | low | "three of the four identities have drifted" omits the FrontComposer commit: HEAD `60d2088c…` is 125 commits after the tuple `aa857831…`, which is a drift trigger in its own right. | patch P2 |
| BH05 | medium | `fbb9c382` (the `/pushall` sync that landed the record) moved EventStore `1be8dcf4…`→`eff9f694…` and Builds `06a51f55…`→`7c7c1b96…`, so no `main` state containing the record selects the tuple. Same root cause as BH11, ECH02 and ECH05: workspace auto-commits swept gitlink moves into this story's commits. | defer D1 |
| BH06 | medium | At the tuple, Gate 2c builds and runs ProviderVerification from `references/Hexalith.EventStore` source (`quality.yml:234-251`), and `epic9-live-acceptance` runs with `HexalithFrontComposerFromSource=true` (`quality.yml:927`, `eng/run-epic9-live-proof.sh:36`). Nothing tells 16.3 which identity the evidence covers. The record is immutable, so the note goes in Implementation Notes. | patch P3 |
| BH07 | low | The text-search method is weaker than MSBuild evaluation, but the reviewer's wider grep over `.github`, `eng` and the scripts confirmed no override, so the recorded `4.1.0` holds. A fix needs an MSBuild evaluation against a tuple checkout and corrects nothing. | reject |
| BH08 | low | At Builds `73e60af8…`, `Props/Directory.Packages.props:148-149` selects `4.2.0` for the SDK 4.2 project views (`eng/dependency_graph.py:1526,1568` lists the opt-in projects) and `5.1.0` otherwise. The drift conclusion holds either way; the next capture must pick which evaluated value counts. | defer D4 |
| BH09 | medium | The current `consumer source projection owner pin changed` refusals (Platform `5a58ba20…`/`f6f85032…`; runs `43702436144`, `43710270227`, `43728107585`, `43736184654`) appear nowhere in `deferred-work.md` or `sprint-status.yaml`, so the only route to a current tuple has no owner. | defer D2 |
| BH10 | low | `deferred-work.md` routes Story 15.5's BH-R4–BH-R10 findings to the 16.1 owner. This spec addresses them, but never cites that entry, so the sweep cannot close it. | patch P4 |
| BH11 | medium | AC 3 ("touches only `_bmad-output/` record files") fails in history: `dd575b67` (`fix: story 16.1`) moved `references/Hexalith.Platform`. It is pushed and cannot be rewritten; strict `--candidate` reports it as unsuppressible `interleaved`. | defer D1 |
| BH12 | low | The planned legacy command still says "expected: pass", while the Results record its exit 1 and the passing `--base` variant. The fix would edit this build's planning section, and the Results already explain it. | reject |
| BH13 | low | The record omits a capture timestamp and per-job conclusions, but it is immutable. The frozen rule qualifies by individual step conclusions, which are recorded, and the listed `gh api …/branches/main` command backs the selection. | reject |
| BH14a | low | The 2026-11-15 sprint comment says "HEAD" without naming `60d2088c…` and uses the short tuple SHA. | patch P5 |
| BH14b | low | The `fbb9c382` disposition refers to "the later Story 16.1 commit" without naming `38856d023dedd7c49da69b1cdb2fc54396f22f38`, which now exists. | patch P6 |
| BH14c | low | The header `# last_updated: 2026-11-01` (last set by `bd189a4e`) disagrees with the `last_updated:` key `11-15-2026`. Same defect as ECH03. | patch P7 |
| BH15 | low | `git status --ignore-submodules=none` shows all six declared checkouts clean at the completion pass; the undated section reads as current. | patch P8 |
| BH16a | low | `epic-16-context.md` still says "No current-tuple record exists yet". It is a compiled planning digest that the story does not own, and 16.3's build loads the done 16.1 spec as continuity context. | reject |
| BH16b | false | The intent requires "Sign-off: none". Naming a capturing owner in the record is not required and could read as a sign-off; ownership stays in the epic. | reject |
| BH16c | medium | Epic 16 combines "recapture on any drift" with "approve only the exact tuple", while workspace auto-commits keep moving gitlinks and seal transitions turn Quality red, with no freeze window or escalation owner. G-3 can livelock. This is a planning gap, not caused by this story. | defer D3 |
| ECH01 | low | Artifact `13469365738` (`contract-diagnostics`) expires `2026-11-20T01:22:41Z`, and job logs expire after 90 days, so the cited 19-interaction and 80-test counts cannot be re-checked afterwards. | patch P10 |
| ECH02 | medium | Same outcome as BH05: the record commit itself moved the tuple's gitlinks, so release review against any state containing the record reports a mismatch. | defer D1 |
| ECH03 | low | Same defect as BH14c. | patch P7 |
| ECH04 | low | Same defect as BH01. | patch P9 |
| ECH05 | medium | Same root cause as BH05 and BH11: `fbb9c382` and `dd575b67` carry submodule gitlink moves despite AC 3. | defer D1 |

## Verification

**Commands:**
- `git cat-file -t <tuple-sha>` and `git ls-tree <tuple-sha> references/Hexalith.EventStore references/Hexalith.Builds` -- expected: commit and the recorded gitlinks.
- `gh run view <run-id> --json headSha,jobs` -- expected: headSha = tuple SHA; required steps `success`.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-1-capture-the-current-eventstore-runtime-tuple.md` -- expected: pass.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-1-capture-the-current-eventstore-runtime-tuple.md --candidate HEAD` -- strict attribution over the whole baseline range.

### Results — 2026-11-15

- `git cat-file -t aa8578318971da04ce7d7bd07928d08c6b95bc1a` returns `commit`. `git ls-tree` at that commit returns EventStore `1be8dcf43347efaf007538f1cd652fc5488bc6e6` and Builds `06a51f55bfe2e03a06e1121d94a41e98f1e3e251`, exactly as recorded.
- `git show aa8578318971da04ce7d7bd07928d08c6b95bc1a:Directory.Packages.props` sets no `HexalithEventStoreVersion`; it only imports the Builds catalog. A `git grep` of the tuple's `*.props`/`*.targets` finds only the `Directory.Build.targets` forwarding of an already-set value, which is not an override. `git -C references/Hexalith.Builds show 06a51f55bfe2e03a06e1121d94a41e98f1e3e251:Props/Directory.Packages.props` sets the default `4.1.0` (line 16). The recorded catalog value and the absence of divergence both hold.
- `gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/42141933127` reports `head_sha` `aa8578318971da04ce7d7bd07928d08c6b95bc1a`, branch `main`, event `push`, attempt 1. `.../runs/42141933127/attempts/1/jobs` shows the same `head_sha` on `build-and-test` `127644096976` and `epic9-live-acceptance` `127644096895`. In `build-and-test`, step 13 and Gate 2c steps 23–26 are `success`; in `epic9-live-acceptance`, all 22 steps are `success`.
- `git diff HEAD` on this spec changes only the Tasks checkbox, Implementation Notes, Verification, Commit Scope Dispositions and File List. No hunk touches the `<frozen-after-approval>` block or `## Tuple Record — 2026-11-06`; that section's bytes are identical before and after this pass and match the committed blob.
- The planned legacy command exits 1. It flags 435 paths, every one committed by other work in the 223 commits from baseline `92eff2e7b1b0f53631c53fa8284b466e202ae921` to `60d2088c26e5cedb7c61471d2e4b813a86996ea2`; none is a Story 16.1 change. Re-run against the completion-pass base (`--base 60d2088c26e5cedb7c61471d2e4b813a86996ea2`), it exits 0.
- Strict `--candidate HEAD` exits 1 with exactly one failure: `interleaved story commit dd575b67c82c377bd17c34fb3d14366277d2ac62 matches story 16.1 but touches unowned paths: references/Hexalith.Platform`. That commit is pushed history. No disposition or `--unrelated` entry suppresses it, and listing the Platform gitlink would claim a move this story must never make. It belongs to the same class as Story 15.5's `e07e2f42605ffde307606f4ee027c382ef1e8aa4`, which the human deferred on 2026-10-30: an exception needs its own guard-change story. It is recorded here and accepted as history, not as a passing gate. The 21 other non-matching commits that touch a listed path are declared under Commit Scope Dispositions (2 `process`, 19 `shared`), and no File List accounting failure remains.
- Repository-integrity `python3 eng/validate-story-artifacts.py` exits 0. `git diff --check` is clean. `yaml.safe_load` parses `sprint-status.yaml`: `epic-16` is `in-progress`, the 16.1 row moved to `review` with the spec's `in-review` status, and 16.2 and 16.3 stay `backlog`.
- `git diff --stat HEAD -- references src tests docs eng .github Directory.Packages.props Directory.Build.props Directory.Build.targets` is empty. No gitlink, submodule file, package preset, schema, validator or evidence file changed in this pass.

## Commit Scope Dispositions

These rows explain the commits after the baseline that touch a listed path without the Story 16.1 ID. None grants ownership. The attempt-1 `fix: story 16.1` commit is not declared here, because its `interleaved` classification cannot be suppressed (see Verification).

- `312af896e57821e26ae654b1817f14914f3371f7` | `process` | Epic 16 planning commit (`fix: epic 16`) created `epic-16-context.md` and this approved spec before implementation; its Platform gitlink move stays unowned. It carries no tuple capture.
- `fc17308d969129edc461c451e16dedacf777b2b0` | `shared` | Concurrent Story 15.5 work changed its own row and `last_updated` in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `decbb195f1c010460530fde6b662a6118a83ee7c` | `shared` | Concurrent Story 15.5 work changed its own row and comments in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `eb1f0f7158b254dfacf460b2e30199ac8912944f` | `shared` | Concurrent Story 15.5 work changed its own row and comments in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `188d8b733090a83108bd006aaa9fa71cd9a5a2d8` | `shared` | Concurrent Story 15.5 work changed its own row, comments and `last_updated` in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `6811bfd6ca47e3272c692288503c68552040df4c` | `shared` | Concurrent Story 15.5 work changed its own row, comments and `last_updated` in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `c46c1fb540604f76ce6754437ba68c894007ca8f` | `shared` | Concurrent Story 15.5 work changed its own comments and `last_updated` in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `589d63aa1a91c9c602f4caa15deef48bcf035b0c` | `shared` | Concurrent Story 15.5 closure changed its own row, comments and action items in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `b2f9fa4bed285f28b6a379cf511a40b4bd9e524c` | `shared` | Concurrent Story 15.5 work changed its own row, comments and `last_updated` in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `bd189a4e6b2e0a784ad4e57ea5e08f25d5ca09eb` | `shared` | Concurrent Story 15.5 work changed its own comments and `last_updated` in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `8993f605b90b3a42210598316363db770fa6cc41` | `shared` | The Epic 15 retrospective recorded its rows and action items in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `98a382a33517084564ecaad3eb4ca313a728ef62` | `shared` | Closing E15R-AI-1 changed one action-item status and `last_updated` in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `fbb9c3822dfa407e72739238cd472567f45f1e82` | `process` | The workspace `/pushall` sync committed the 2026-11-06 capture edits to this spec and the sprint comment without the story ID, and moved six unrelated gitlinks, which stay unowned. Ownership of those files comes from the later Story 16.1 commit `38856d023dedd7c49da69b1cdb2fc54396f22f38`; the record's tuple commit is unchanged.
- `ca9c03d43c6488a4bd18357921a14c56418c84dd` | `shared` | The Epic 14 retrospective recorded its action items in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `57aa756c02edbae4294412885823533308b7db0d` | `shared` | A `fix(ci)` commit changed one action-item status in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `b13587aae9a40f2aa18b5cfee873edde5d61c29a` | `shared` | Concurrent Story 13.6 work changed its own row, comments and `last_updated` in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `b76cfb91bae1dff34bea126865c4b10a9a69a0eb` | `shared` | Concurrent Story 13.6 work changed its own row, comments and `last_updated` in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `eee90d5338c7ad4bf89365919c900e8bc3ab746e` | `shared` | Concurrent Story 13.6 work changed its own row, comments and `last_updated` in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `0d065fceb2bfca9999307cb864a3f93392fb3400` | `shared` | Concurrent Story 13.6 review changed its own row, comments and `last_updated` in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `b22be84bdbb23d9263a8835496ad0c95adf1843f` | `shared` | Concurrent Story 13.6 work changed its own row, comments and `last_updated` in the shared `sprint-status.yaml`; no Epic 16 line changed.
- `60d2088c26e5cedb7c61471d2e4b813a86996ea2` | `shared` | Concurrent Story 13.6 work changed its own row, comments and `last_updated` in the shared `sprint-status.yaml`; no Epic 16 line changed.


The 2026-11-15 final provenance audit also records the following shared/companion document touches. Each commit was checked against its actual changed-file list; none changes the historical D1 verdict or claims unrelated source ownership.

- `e07e2f42605ffde307606f4ee027c382ef1e8aa4` | `shared` | Story 15.5 updated the shared deferred ledger and its own release/precursor records; its Platform pointer is not Story 16.1 work.
- `e9995e31b054267ca053c45829ba6806cee144cf` | `shared` | Story 15.5 review recorded its findings in the shared deferred ledger; no tuple capture or approval belongs to this commit.
- `11d31d446c7b86b5e810f468bda4c2a74f01683f` | `shared` | Story 15.5 hosted governance qualification updated its own story and the shared deferrals; no Story 16.1 tuple changed.
- `7fd0d09f36e38dfeb044bf54402cf360fe7a11b6` | `shared` | Owning CI work resolved an invalid-bearer regression deferral in the shared ledger, independent of the tuple capture.
- `bd54321b716f5cd553e930b78f88578367cbeff4` | `shared` | Story 13.6 accessibility acceptance updated its records, walkthrough and shared deferrals; source ownership is separate.
- `9724c7e5db5f744366f10e7ee175723cf1ae134c` | `shared` | Story 13.6 locator qualification updated its accessibility records and the shared deferred ledger.
- `e4df7256be0e89b48c830c0792033ecfeabec9ad` | `shared` | Story 13.6 palette verification updated its browser test, acceptance records and shared deferrals; no Story 16.1 source ownership is claimed.
- `fbe01c913cb863abb0fd4a317d370d36fff593b3` | `shared` | The perceptible-outline accessibility fix updated Story 13.6 records, its CSS/browser checks and the shared ledger.
- `369e69efed76e91eb6d798407b448306bc036d52` | `process` | The unrelated workspace sync recorded accessibility work and shared deferrals with six dependency pointers; those gitlink and source moves are not Story 16.1 work.
- `85d223d6cf799b530e7ef68b3e77663c5a5ef84a` | `shared` | Story 13.6 updated accessibility acceptance and shared deferrals; no tuple capture or approval is inferred.
- `b70627f649826b1a2338ccc13295ba02b71f7424` | `shared` | The native-NVDA accessibility fix updated its own shell/browser source, Story 13.6 records and shared deferrals.
- `8a51e0a1891f08bbfb51e168b69f766c787b1f46` | `shared` | The owning dependency-refresh repair updated its policy, gitlinks, manifests and shared deferrals; none of its source or pointer moves is attributed to Story 16.1.
- `5c54d54e740ec5be87e29eabbd311f56831ced64` | `shared` | The GOV-1 implementation updated its workflows, governance engine/tests and shared ledger; no runtime or policy ownership is transferred to this capture.
- `13b382bc721cbe2f835bf0f724796e6f825175f0` | `shared` | Accessibility focus recovery updated Story 13.6 source, evidence helpers and the shared ledger; it is separate from the tuple capture.
- `50def1fe238787e9365c93991e3dc83af8c2e299` | `process` | The approved companion dependency-governance build created its audit spec and resealed policy. This accounts for the shared audit document only; eng policy remains outside Story 16.1 capture ownership.

- `902fe13fc411c9eb7cd3786180761a904a3b47fb` | `shared` | The approved companion governance/correct-course delivery records seal proof, planning cadence, scoped property audit and shared deferrals. The new tuple capture and sprint acceptance are isolated in the following Story 16.1 delivery; no runtime code, policy, workflow or gitlink ownership is assigned here.

## File List

The approved 2026-11-15 companion dependency-governance/correct-course build also updates the shared planning records below. They are listed for complete attribution of the activation commit, not as ownership of runtime code, policy, workflows or gitlinks. The final tuple-capture delivery is isolated to this spec and sprint-status.yaml; companion planning is committed separately.

- `_bmad-output/implementation-artifacts/spec-restore-quality-gate-2c-current-tuple.md` — companion seal/activation verification, scheduling change, independent review and scoped property-evaluation audit. This is a pre-existing shared companion record delivered before the final two-file capture; unchanged by this capture commit.
- `_bmad-output/planning-artifacts/prd.md` — approved G-3/D-12 cadence, escalation and exact-candidate boundary. This is a pre-existing shared companion record delivered before the final two-file capture; unchanged by this capture commit.
- `_bmad-output/planning-artifacts/epics.md` — approved Epic 16 qualification cadence and candidate lineage. This is a pre-existing shared companion record delivered before the final two-file capture; unchanged by this capture commit.
- `_bmad-output/planning-artifacts/architecture.md` — approved scheduling and exact-tested-commit interpretation. This is a pre-existing shared companion record delivered before the final two-file capture; unchanged by this capture commit.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-tuple-freeze.md` — approved companion proposal, incident due time and outcomes. This is a pre-existing shared companion record delivered before the final two-file capture; unchanged by this capture commit.


- `_bmad-output/implementation-artifacts/epic-16-context.md` — pre-existing context created by planning commit `312af896e57821e26ae654b1817f14914f3371f7` before implementation; read in full and unchanged by this story.
- `_bmad-output/implementation-artifacts/spec-16-1-capture-the-current-eventstore-runtime-tuple.md` — tuple identities, same-attempt step conclusions, counts and commands, plus the completion-pass drift check, commit-scope dispositions and story verification.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — current Story 16.1 capture progress and completion-pass drift note; Epic 16 remains in progress and G-3 remains open.
- `_bmad-output/implementation-artifacts/deferred-work.md` — four review-iteration-1 deferrals (D1–D4) appended at the end of the ledger; no existing entry changed. This is a pre-existing shared companion record delivered before the final two-file capture; unchanged by this capture commit.

## Documented Unrelated Changes

These entries were declared during the 2026-11-06 capture. At the 2026-11-15 completion pass, `git -c diff.ignoreSubmodules=none status --ignore-submodules=none` shows all six checkouts clean. The Platform gitlink move in `dd575b67` is disclosed separately in Implementation Notes.

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

## Tuple Record — 2026-11-15

**Migration approval: not claimed · Sign-off: none · G-3: open**

**Overall Quality: failure.** Its later default unit/bUnit lane reported **5 failed, 6,680 succeeded, 0 skipped**. This record qualifies only under the frozen individual-step rule: catalog preparation, all four Gate 2c steps and every `epic9-live-acceptance` step succeeded together in attempt 1. No overall-green Quality run or engineering acceptance is claimed here; the five bounded residuals below remain for their owning maintainers and parent review.

### Identities and current selection

Captured `2026-11-15T18:44:11Z`, within two hours of the final required success at `2026-11-15T18:26:36Z` (capture deadline `2026-11-15T20:26:36Z`). Local HEAD, `origin/main` and a fresh `git ls-remote origin refs/heads/main` all selected the following exact tested commit immediately before writing. This follows two cancelled observations, documented in the repair spec; neither supplies this record's counts.

| Identity | Recorded value |
|---|---|
| FrontComposer commit tested by Quality | `8ce46cfc1293925459c8b6af15ef7719cf0fc071` |
| EventStore source gitlink | `fdb59895975e86a1e234325a01237f259cdbd53b` |
| Builds gitlink | `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` |
| Catalog-owned default `HexalithEventStoreVersion` | `5.1.0` |
| Root `Directory.Packages.props` preset | None; the selected Builds catalog is imported without an EventStore version preset. |
| Per-project evaluated package selection | `5.1.0` in all six contexts below; no divergence from the catalog-owned default. |
| Provider source assembly informational version | `5.1.0+fdb59895975e86a1e234325a01237f259cdbd53b`; its source and Builds identities match the gitlinks above. |

The later record/receipt commit is a distinct commit, not this tested tuple. Approval covers only the exact tested commit; no receipt transfers automatically to a descendant. Any latest-main mismatch needs new evidence or an explicitly approved planning resolution. The original outer qualification deadline remains `2026-11-16T17:27:10Z`; named migration authority and Release Owner/Architect escalation dispositions are not invented by this capture.

### Quality attempt and required steps

[Quality 43744582233, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43744582233/attempts/1), triggered by a main push at `8ce46cfc1293925459c8b6af15ef7719cf0fc071`, reports that exact `head_sha`. Both required jobs independently report the same SHA. Its overall conclusion is `failure`, from the later default lane; qualification uses the following individual conclusions.

[build-and-test 132905063083](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43744582233/job/132905063083):

| Step | Exact step name | Conclusion |
|---|---|---|
| 14 | Prepare exact governance catalog objects | `success` |
| 24 | Gate 2c: Contract pacts | `success` |
| 25 | Gate 2c: Live EventStore provider verification | `success` |
| 26 | Gate 2c: Validate contract artifacts | `success` |
| 27 | Gate 2c: Fail on stale pact diff | `success` |

Contract pacts: **3 succeeded, 0 failed, 0 skipped**. Provider verification tests: **80 passed, 0 errors, 0 failed, 0 skipped, 0 not run**, read from this job's log. The live report confirms **19 requested / 19 reported / 19 passed / 0 failed interactions**, 19 requested provider states, 19 setup events and 19 teardown events. Completeness, host startup, readiness, host shutdown and port closure are all true; reason codes are empty. Contract artifact validation and stale-pact checks concluded success.

These live counts come from this same attempt's existing [contract-diagnostics artifact 14174636740](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43744582233/artifacts/14174636740), expiring `2026-11-29T18:26:36Z`. The matching [contract-artifacts 14174262464](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43744582233/artifacts/14174262464) expires `2026-11-29T18:26:37Z`. Job logs have the existing 90-day retention. Only safe counts and identities are recorded; no report, raw payload or log is committed.

### Every Epic 9 live acceptance step

[epic9-live-acceptance 132905062942](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43744582233/job/132905062942), attempt 1: all **27 API-reported steps** concluded success; zero failed or skipped.

| Step | Exact step name | Conclusion |
|---|---|---|
| 1 | Set up job | `success` |
| 2 | Run actions/checkout@9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0 | `success` |
| 3 | Initialize build submodules | `success` |
| 4 | Run Hexalith/Hexalith.Builds/Github/initialize-dotnet@6e77e79e8914a375b0c43b0af3bd142405675ecd | `success` |
| 5 | Install .NET 11 runtimes | `success` |
| 6 | Install source-resource compatibility SDK | `success` |
| 7 | Install dependency source-resource SDK | `success` |
| 8 | Verify root and source-resource SDKs | `success` |
| 9 | Run actions/setup-node@48b55a011bda9f5d6aeb4c2d9c7362e8dae4041e | `success` |
| 10 | Install Playwright workspace dependencies | `success` |
| 11 | Initialize Ubuntu APT for browser dependencies | `success` |
| 12 | Use the Ubuntu HTTPS archive for browser dependencies | `success` |
| 13 | Install Chromium browser | `success` |
| 14 | Install Aspire CLI | `success` |
| 15 | Install Dapr CLI | `success` |
| 16 | Initialize Dapr runtime | `success` |
| 17 | Typecheck Epic 9 browser proof | `success` |
| 18 | Test Epic 9 evidence tooling | `success` |
| 19 | Run AppHost-discovered Epic 9 live proof | `success` |
| 20 | AppHost rejects invalid bearer tokens | `success` |
| 21 | Upload Epic 9 acceptance artifacts | `success` |
| 38 | Post Run actions/setup-node@48b55a011bda9f5d6aeb4c2d9c7362e8dae4041e | `success` |
| 39 | Post Install dependency source-resource SDK | `success` |
| 40 | Post Install source-resource compatibility SDK | `success` |
| 41 | Post Run Hexalith/Hexalith.Builds/Github/initialize-dotnet@6e77e79e8914a375b0c43b0af3bd142405675ecd | `success` |
| 42 | Post Run actions/checkout@9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0 | `success` |
| 43 | Complete job | `success` |

The same-attempt [Epic 9 artifact 14173977718](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43744582233/artifacts/14173977718), expiring `2026-11-29T18:16:58Z`, binds `candidateCommit` to `8ce46cfc1293925459c8b6af15ef7719cf0fc071`, `workingTreeDirty=false` and final evidence mode. Its browser JUnit report has **1 test, 0 failures, 0 skips, 0 errors**; Playwright reports 1 expected result, 0 unexpected, 0 skipped and **0 flaky**. The invalid-bearer job log records HTTP **401** for command submission, command status, query and projection-hub negotiation. Verification exit is zero.

### Per-project property evaluation and lane identities (D4)

After the current required steps passed, `dotnet msbuild -getProperty:HexalithEventStoreVersion` was repeated for every direct/hosted FrontComposer EventStore-consuming project, explicitly selecting configuration, `TargetFramework=net11.0` and source/package mode. Root configuration files match committed objects; the selected Builds/EventStore/module configurations and checkout SHAs are clean and exact.

| Consuming project | Configuration / mode | Target framework | Evaluated `HexalithEventStoreVersion` |
|---|---|---|---|
| `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` | Release / package | `net11.0` | `5.1.0` |
| `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` | Debug / source | `net11.0` | `5.1.0` |
| `src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj` | Release / package | `net11.0` | `5.1.0` |
| `src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj` | Debug / source | `net11.0` | `5.1.0` |
| `tests/Hexalith.FrontComposer.CounterFixture/Hexalith.FrontComposer.CounterFixture.csproj` | Release / package | `net11.0` | `5.1.0` |
| `tests/Hexalith.FrontComposer.CounterFixture/Hexalith.FrontComposer.CounterFixture.csproj` | Debug / source | `net11.0` | `5.1.0` |

Release queries use `UseNuGetDeps=true` (`UseHexalithProjectReferences=false`, EventStore/FrontComposer source flags false). Debug queries use `UseNuGetDeps=false` (the inverse flags true), matching the AppHost live source graph. All three SDK 4.2 catalog-view properties evaluate false in every row. The catalog's opt-in `4.2.0` branch therefore does not describe any of these contexts. These are property evaluations; they do not add package-mode runtime coverage.

| Exercised lane | EventStore identity and scope |
|---|---|
| Gate 2c provider | Release-built ProviderVerification and its 80-test suite execute the selected EventStore source checkout `fdb59895975e86a1e234325a01237f259cdbd53b`; report source/Builds identities match and source informational version is recorded above. |
| Epic 9 live proof and invalid-bearer check | Debug source graph, `HexalithFrontComposerFromSource=true`, exact FrontComposer `8ce46cfc1293925459c8b6af15ef7719cf0fc071` and selected EventStore source gitlink `fdb59895975e86a1e234325a01237f259cdbd53b`; existing scripts build and launch the same source AppHost. |
| Release/package evaluation and shared CI | Catalog property `5.1.0` is selected; [CI 43744582708](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43744582708) succeeded at this exact commit. The two live lanes above exercise source; no EventStore NuGet `5.1.0` package-mode runtime compatibility is claimed. |

### Later default-lane failures and bounded residuals

The existing [test-results artifact 14175076344](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43744582233/artifacts/14175076344) contains exactly five failed `UnitTestResult` entries. The repair spec and append-only `deferred-work.md` entries route them separately; this capture changes no guard or unrelated source/test code and calls neither observed timeout flaky.

| Failed test invocation | Observed cause / attribution |
|---|---|
| `Hexalith.FrontComposer.Shell.Tests.Generated.CommandTargetGeneratedFormTests.SynchronouslyBlockingProvider_DeadlineStillAllowsDispatch` | Observed `TimeoutException` waiting up to two seconds for `provider.Started`; causal origin is unproven. The test file predates and is unchanged by both concurrent commits. |
| `Hexalith.FrontComposer.SourceTools.Tests.Emitters.CommandFormValidationCompilationTests.EmittedValidationCompilesWithoutWarningsAgainstTheSupportedFramework(frameworkMajor: 11)` | `ArgumentOutOfRangeException(startIndex)`: the fixture searches the old emitted `_editContext` validation marker, then calls `IndexOf` with -1. Concurrent `923988d855cd6131b4287e3da763f12baf60ab22` renamed that marker to `validatedEditContext`; the fixture was not updated. |
| `Hexalith.FrontComposer.SourceTools.Tests.Emitters.CommandFormValidationCompilationTests.EmittedValidationCompilesWithoutWarningsAgainstTheSupportedFramework(frameworkMajor: 10)` | Same proven stale-marker failure introduced by the concurrent forms change; no framework compilation verdict was reached. |
| `Hexalith.FrontComposer.Shell.Tests.Services.Lifecycle.UlidFactoryTests.ProductionCommandIdentityPaths_DoNotUseGuidIdentityApis` | Broad source-token assertion finds `Guid.TryParse(` in the emitter's GUID-valued field parsing. Both that occurrence and the test already exist at baseline `22f76488e8878e3f879c1dd1b6d7bf75534b2b1b`; no command-identity use is inferred from this scan. |
| `Hexalith.FrontComposer.Shell.Tests.Generated.CommandRendererWrapperIntegrationTests.GeneratedForm_SubmitAccepted_RendersLifecyclePhasesAndKeepsFormPresent` | Observed bUnit assertion timeout: expected wrapper class `fc-lifecycle-pulse`, actual `fc-lifecycle-wrapper `. The test file is unchanged by both concurrent commits; causality and repeatability are unproven. |

### Capture and execution commands

Identity reads use committed objects, independent of checkout drift. Metadata, log counts and artifact summaries were inspected read-only; all disposable verification uses `run-with-temp`.

```bash
git rev-parse HEAD origin/main
git ls-remote origin refs/heads/main
git ls-tree 8ce46cfc1293925459c8b6af15ef7719cf0fc071 references/Hexalith.EventStore references/Hexalith.Builds
git show 8ce46cfc1293925459c8b6af15ef7719cf0fc071:Directory.Packages.props
git -C references/Hexalith.Builds show 73e60af8a1fcdf3f06726d5d1b6e1199cce778ee:Props/Directory.Packages.props
gh run list --workflow quality.yml --commit 8ce46cfc1293925459c8b6af15ef7719cf0fc071
gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/43744582233
gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/43744582233/attempts/1/jobs --paginate
gh api repos/Hexalith/Hexalith.FrontComposer/actions/jobs/132905063083/logs
gh api repos/Hexalith/Hexalith.FrontComposer/actions/jobs/132905062942/logs
gh api repos/Hexalith/Hexalith.FrontComposer/actions/artifacts/14174636740/zip
gh api repos/Hexalith/Hexalith.FrontComposer/actions/artifacts/14173977718/zip
gh api repos/Hexalith/Hexalith.FrontComposer/actions/artifacts/14175076344/zip
```

For each full project path in the evaluation table, both commands below were run under `run-with-temp`; the actual query also requested the mode and three SDK 4.2-view properties shown above.

```bash
dotnet msbuild <project> -nologo -p:Configuration=Release -p:TargetFramework=net11.0 -p:UseNuGetDeps=true -getProperty:HexalithEventStoreVersion,TargetFramework,UseHexalithProjectReferences,UseNuGetDeps,HexalithEventStoreFromSource,HexalithFrontComposerFromSource,HexalithPartiesSdk42CatalogView,HexalithPlatformSdk42CatalogView,HexalithBuildsPackagedHostSdk42CatalogView
dotnet msbuild <project> -nologo -p:Configuration=Debug -p:TargetFramework=net11.0 -p:UseNuGetDeps=false -getProperty:HexalithEventStoreVersion,TargetFramework,UseHexalithProjectReferences,UseNuGetDeps,HexalithEventStoreFromSource,HexalithFrontComposerFromSource,HexalithPartiesSdk42CatalogView,HexalithPlatformSdk42CatalogView,HexalithBuildsPackagedHostSdk42CatalogView
```

CI's provider step restored and built `references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification.Tests/Hexalith.EventStore.ProviderVerification.Tests.csproj` in Release, ran its test assembly, then ran ProviderVerification with `--verification-mode live-compatibility`, the existing Pact directory/manifest/state catalog and `--report-output artifacts/contracts/provider-verification.json`. Its target frameworks were obtained with `dotnet msbuild -getProperty:TargetFramework`. The AppHost step ran `./eng/run-epic9-live-proof.sh` with `FC_EPIC9_EXPECTED_COMMIT=8ce46cfc1293925459c8b6af15ef7719cf0fc071` and `FC_EPIC9_REQUIRE_CLEAN=true`; it built Debug source with `--disable-build-servers -m:1 -p:BuildInParallel=false -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false`, then launched via `aspire start --isolated --no-build`. The invalid-bearer step ran `./eng/verify-apphost-invalid-bearer.sh` with `HexalithFrontComposerFromSource=true`. No package version, gitlink, execution pin or guard was changed for this capture.

## Review Addendum — D4 context correction, 2026-11-15

This append-only addendum corrects the scope interpretation in the 2026-11-15 Tuple Record. Both written Tuple Records remain byte-identical. The earlier six top-level property queries did not prove all hosted referenced catalog contexts; the prior blanket D4-completeness interpretation is withdrawn.

**CounterFixture correction.** The standalone Release query with `UseNuGetDeps=true` returns property `5.1.0`, but `EventStorePath` is empty and the unconditional `ProjectReference` resolves to `/src/Hexalith.EventStore.DomainService/Hexalith.EventStore.DomainService.csproj`. That dangling source reference supplies no valid Release/package consumer context. The Release package-context interpretation is withdrawn; its returned literal proves only a property value. CounterFixture's valid Debug/source context, evaluated through AppHost's forwarded edge, has `HexalithEventStoreFromSource=true`, `HexalithEventStoreVersion=5.1.0` and the existing root-selected `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/Hexalith.EventStore.DomainService.csproj` path.

**Actual forwarded source contexts.** Targeted `run-with-temp dotnet msbuild` evaluations reproduce the live source build's global properties: `Configuration=Debug`, `BuildInParallel=false`, `NuGetAudit=false`, `CentralPackageTransitivePinningEnabled=false`, with `HexalithFrontComposerFromSource=true` in the environment. AppHost's evaluated `ProjectReference.AdditionalProperties` are passed as child global properties after `GlobalPropertiesToRemove`; UI is first evaluated under its AppHost edge, then its child edges inherit those globals plus their own evaluated additions/removals. In particular, the edges forward `HexalithEventStoreVersion=5.1.0`, the selected module roots/source flags, `CustomAfterDirectoryBuildTargets` pointing to `eng/FrontComposer.SourceReferences.targets`, and UI's `CustomAfterMicrosoftCommonTargets` pointing to `DropPublishedFrontComposerAssemblies.targets` where present. No version is supplied from a hand-selected replacement value.

Each table row was evaluated with `-getProperty:Configuration,TargetFramework,HexalithEventStoreVersion,EventStorePath,UseHexalithProjectReferences,HexalithEventStoreFromSource,HexalithPartiesSdk42CatalogView,HexalithPlatformSdk42CatalogView,HexalithBuildsPackagedHostSdk42CatalogView -getItem:ProjectReference,PackageReference`. The standalone control uses only `Configuration=Debug`, with no forwarded globals or ambient module-source flags. The referenced Builds/EventStore/Parties/Tenants checkouts match the committed gitlinks selected by captured `8ce46cfc1293925459c8b6af15ef7719cf0fc071`, and evaluated configuration/source files are unchanged. All inherited rows target `net11.0` and have EventStore source mode true. No `TargetFramework` global is passed: each child evaluates its own framework. Evaluated `SetTargetFramework`, `SetConfiguration` and `SetPlatform` metadata is empty on these edges; no synthetic framework is inherited. Build-target-generated framework negotiation is not exercised by property/item evaluation and remains part of the completeness gap.

| Parent source edge | Referenced project | SDK 4.2 Parties view | Standalone Debug selection | Inherited effective selection |
|---|---|---|---|---|
| AppHost | `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj` | `false` | `5.1.0` | `5.1.0` |
| AppHost | `tests/Hexalith.FrontComposer.CounterFixture/Hexalith.FrontComposer.CounterFixture.csproj` | `false` | `5.1.0` | `5.1.0` |
| AppHost | `references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Hexalith.Builds.Tooling.csproj` | `false` | `5.1.0` | `5.1.0` |
| AppHost | `references/Hexalith.EventStore/src/Hexalith.EventStore/Hexalith.EventStore.csproj` | `false` | `5.1.0` | `5.1.0` |
| AppHost | `references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.Server.Host/Hexalith.EventStore.Admin.Server.Host.csproj` | `false` | `5.1.0` | `5.1.0` |
| AppHost | `references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Hexalith.EventStore.Admin.UI.csproj` | `false` | `5.1.0` | `5.1.0` |
| AppHost | `references/Hexalith.Tenants/src/Hexalith.Tenants/Hexalith.Tenants.csproj` | `false` | `5.1.0` | `5.1.0` |
| AppHost | `references/Hexalith.Tenants/src/Hexalith.Tenants.Api/Hexalith.Tenants.Api.csproj` | `false` | `5.1.0` | `5.1.0` |
| AppHost | `references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj` | `false` | `5.1.0` | `5.1.0` |
| AppHost | `references/Hexalith.Tenants/samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj` | `false` | `5.1.0` | `5.1.0` |
| AppHost | `references/Hexalith.Parties/src/Hexalith.Parties/Hexalith.Parties.csproj` | `true` | `4.2.0` | `5.1.0` |
| AppHost | `references/Hexalith.Parties/src/Hexalith.Parties.UI/Hexalith.Parties.UI.csproj` | `true` | `4.2.0` | `5.1.0` |
| UI via AppHost | `references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj` | `false` | `5.1.0` | `5.1.0` |
| UI via AppHost | `references/Hexalith.Parties/src/Hexalith.Parties.UI/Hexalith.Parties.UI.csproj` | `true` | `4.2.0` | `5.1.0` |
| UI via AppHost | `references/Hexalith.Parties/src/Hexalith.Parties.AdminPortal/Hexalith.Parties.AdminPortal.csproj` | `true` | `4.2.0` | `5.1.0` |
| UI via AppHost | `references/Hexalith.Parties/src/Hexalith.Parties.ConsumerPortal/Hexalith.Parties.ConsumerPortal.csproj` | `true` | `4.2.0` | `5.1.0` |

**Divergence.** The Parties, Parties.UI, AdminPortal and ConsumerPortal standalone Debug controls select SDK 4.2 view `4.2.0`; those project-specific views differ from the root default `5.1.0`. Their inherited hosted contexts still have the SDK 4.2 view true, but the AppHost/UI global `HexalithEventStoreVersion=5.1.0` prevents the catalog's empty-value `4.2.0` default from taking effect. Thus “all SDK 4.2 views are false” describes only the earlier top-level queries, not the actual referenced contexts. No inherited row diverged from the forwarded/root `5.1.0` selection; standalone `4.2.0` must not be substituted for that inherited value.

**Bounded D4 status.** Sixteen immediate AppHost/UI edge contexts across fourteen distinct child projects, their sixteen standalone controls, and CounterFixture's valid source path are evaluated. This closes only those specific property-context questions. A complete recursive restore/build graph, all transitive `GlobalPropertiesToRemove`/edge overrides, every path-only resource-launch context, and package-mode runtime coverage are not proved by these queries. D4 remains open for that completeness gap; earlier blanket closure statements in the immutable record and tracking are superseded by this scoped addendum. No CounterFixture Release/package validity, migration approval or sign-off is inferred.

**Release gate and incident.** The capture meets only Story 16.1's individual-step evidence rule. Overall Quality `43744582233` remains failed, so G-3's unchanged passing-overall-Quality workflow condition is unmet. Repository-wide coordination of all active sessions and the workspace automation owner was not achieved; it remains an open prerequisite/Release Owner action, despite the observed session-only hold. No business calendar was supplied for “four working hours.” For the current incident, first drift at `2026-11-15T17:49:35Z` has conservative UTC disposition due time **`2026-11-15T21:49:35Z`**, four elapsed hours later, without extending the original outer deadline `2026-11-16T17:27:10Z` or claiming a named owner receipt. Failure escalation concerns a successor's required qualification steps; the planned seal-transition refusal under its previous event-base policy is not itself failed successor qualification. Other drift/authority/timeout triggers remain unchanged.

## Tuple Record — 2026-11-15

**Later green successor — captured `2026-11-15T20:32:40Z` · FrontComposer `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc` · primary automatic main-push Quality `43755163930`, attempt 1.** This is the later same-day record, distinct from the earlier failed 2026-11-15 record and its Review Addendum. Those records and the addendum remain immutable history.

**Migration approval: not claimed · Sign-off: none · G-3: open**

### Exact identities, current selection and cadence

Immediately before writing, fresh `git fetch origin main`, `git rev-parse HEAD origin/main` and `git ls-remote origin refs/heads/main` all selected `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`. Identity reads use explicit committed objects; no gitlink, dependency version, source file or workflow is changed by this record.

| Identity | Green successor value |
|---|---|
| FrontComposer tested commit | `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc` |
| EventStore source gitlink | `fdb59895975e86a1e234325a01237f259cdbd53b` |
| Builds gitlink | `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` |
| Catalog-owned default `HexalithEventStoreVersion` | `5.1.0` |
| Root package preset | None; `Directory.Packages.props` imports the selected Builds catalog without an EventStore version preset. |
| Effective source-consumer property | `5.1.0` in all 35 actual source-consuming contexts across 27 projects below. |
| Provider observed source informational version | `5.1.0+fdb59895975e86a1e234325a01237f259cdbd53b`; expected/observed source and Builds identities match the selected gitlinks. |

The last required primary success was `2026-11-15T20:12:06Z`. Capture at `2026-11-15T20:32:40Z` is within its two-hour deadline `2026-11-15T22:12:06Z` and the unchanged outer deadline `2026-11-16T17:27:10Z`. Repository-wide coordination and named Release Owner/Architect disposition are not claimed by this successful observation. The existing conservative incident due time remains `2026-11-15T21:49:35Z`; no business calendar or named owner receipt is invented.

The documentation commit that will add this record is a different commit. No receipt or evidence transfers automatically to that descendant; release review must resolve the exact candidate/record lineage under the existing rules.

### Primary automatic run and same-attempt counts

[Primary Quality 43755163930, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930/attempts/1) is a main push at `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`, started `2026-11-15T19:56:02Z`, completed successfully by `2026-11-15T20:21:26Z`. **All eight Quality jobs concluded success.** This record's job metadata, log counts and artifact summaries come only from that primary attempt. No count is copied from a cancelled/failed run or the supplemental manual run.

[Primary build-and-test job 132937646575](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930/job/132937646575) and [primary Epic 9 job 132937646633](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930/job/132937646633) both report the exact tested SHA and success.

| Step | Exact primary build-and-test step | Conclusion |
|---|---|---|
| 14 | Prepare exact governance catalog objects | `success` |
| 24 | Gate 2c: Contract pacts | `success` |
| 25 | Gate 2c: Live EventStore provider verification | `success` |
| 26 | Gate 2c: Validate contract artifacts | `success` |
| 27 | Gate 2c: Fail on stale pact diff | `success` |

Primary log counts: Contract pacts **3 succeeded, 0 failed, 0 skipped**; provider-verification tests **80 passed, 0 errors, 0 failed, 0 skipped, 0 not run**; default unit/bUnit lane **6,685 succeeded, 0 failed, 0 skipped**. Other advisory/quarantine lanes retain their own reported skip accounting; this is not a claim that every lane contains zero skips.

The same-attempt [contract-diagnostics artifact 14178432272](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930/artifacts/14178432272), expiring `2026-11-29T20:12:06Z`, reports `live-compatibility`, verdict `passed`, **19 requested / 19 reported / 19 passed / 0 failed interactions**, 19 requested states, 19 setup events and 19 teardown events. Completeness, startup, readiness, shutdown and port closure are all true; reason codes are empty. The matching [contract-artifacts 14178258122](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930/artifacts/14178258122) expires `2026-11-29T20:12:07Z`. Job logs retain the existing 90-day policy; no raw log, report, token or payload is committed.

### Every primary Epic 9 step

All **27 API-reported primary Epic 9 steps** succeeded, including invalid-bearer verification and cleanup:

| Step | Exact primary Epic 9 step | Conclusion |
|---|---|---|
| 1 | Set up job | `success` |
| 2 | Run actions/checkout@9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0 | `success` |
| 3 | Initialize build submodules | `success` |
| 4 | Run Hexalith/Hexalith.Builds/Github/initialize-dotnet@6e77e79e8914a375b0c43b0af3bd142405675ecd | `success` |
| 5 | Install .NET 11 runtimes | `success` |
| 6 | Install source-resource compatibility SDK | `success` |
| 7 | Install dependency source-resource SDK | `success` |
| 8 | Verify root and source-resource SDKs | `success` |
| 9 | Run actions/setup-node@48b55a011bda9f5d6aeb4c2d9c7362e8dae4041e | `success` |
| 10 | Install Playwright workspace dependencies | `success` |
| 11 | Initialize Ubuntu APT for browser dependencies | `success` |
| 12 | Use the Ubuntu HTTPS archive for browser dependencies | `success` |
| 13 | Install Chromium browser | `success` |
| 14 | Install Aspire CLI | `success` |
| 15 | Install Dapr CLI | `success` |
| 16 | Initialize Dapr runtime | `success` |
| 17 | Typecheck Epic 9 browser proof | `success` |
| 18 | Test Epic 9 evidence tooling | `success` |
| 19 | Run AppHost-discovered Epic 9 live proof | `success` |
| 20 | AppHost rejects invalid bearer tokens | `success` |
| 21 | Upload Epic 9 acceptance artifacts | `success` |
| 38 | Post Run actions/setup-node@48b55a011bda9f5d6aeb4c2d9c7362e8dae4041e | `success` |
| 39 | Post Install dependency source-resource SDK | `success` |
| 40 | Post Install source-resource compatibility SDK | `success` |
| 41 | Post Run Hexalith/Hexalith.Builds/Github/initialize-dotnet@6e77e79e8914a375b0c43b0af3bd142405675ecd | `success` |
| 42 | Post Run actions/checkout@9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0 | `success` |
| 43 | Complete job | `success` |

The primary [Epic 9 artifact 14178247242](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930/artifacts/14178247242), expiring `2026-11-29T20:07:35Z`, binds `candidateCommit` to `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`, `workingTreeDirty=false`, final evidence mode and the isolated source-graph launch. Its JUnit report has **1 final passing test, 0 failures, 0 skips, 0 errors**. Playwright classifies that test as **1 flaky result**: retry 0 failed and retry 1 passed. This is a final successful configured-retry result, not a first-attempt browser pass. The primary job log confirms HTTP **401** on all four invalid-bearer surfaces: command submission, command status, query and projection-hub negotiation; verification exit is zero.

### Current per-consumer source contexts (D4)

The saved recursive ProjectReference evaluation reached 79 contexts / 51 projects / 177 edges with no queued contexts; 44 contexts merely had an incidental EventStore property and are excluded from the consuming-project count. The **35 contexts / 27 projects** below have active direct EventStore source ProjectReferences. All 35 were freshly re-evaluated on `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc` with three read-only workers under `run-with-temp`, from their exact inherited argv/globals. Fresh actual AppHost/UI root properties, inherited UI globals and relevant ProjectReference metadata match the saved graph with **zero discrepancies and zero query errors**. The selected owner configurations and gitlink checkouts remain exact and clean.

The actual source build globals are `Configuration=Debug`, `BuildInParallel=false`, `NuGetAudit=false`, `CentralPackageTransitivePinningEnabled=false`, with `HexalithFrontComposerFromSource=true` in the environment. Per-edge AdditionalProperties, GlobalPropertiesToRemove and evaluated Set metadata are honored. No synthetic TargetFramework global is passed to these consumers; each evaluates its own `net11.0` framework. The root-forwarded EventStore version is `5.1.0`. All Platform and Builds SDK42 view flags are false; Parties SDK42 is true in eight contexts and false in 27.

| Actual source-consuming project | Distinct inherited contexts | Own framework | Effective EventStore | EventStore source | Parties SDK42 view |
|---|---|---|---|---|---|
| `references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.Abstractions/Hexalith.EventStore.Admin.Abstractions.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.Server.Host/Hexalith.EventStore.Admin.Server.Host.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.Server/Hexalith.EventStore.Admin.Server.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Hexalith.EventStore.Admin.UI.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Hexalith.EventStore.Client.csproj` | 3 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/Hexalith.EventStore.DomainService.csproj` | 2 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.Gateway/Hexalith.EventStore.Gateway.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Hexalith.EventStore.Server.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Hexalith.EventStore.ServiceDefaults.csproj` | 2 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.SignalR/Hexalith.EventStore.SignalR.csproj` | 2 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.EventStore/src/Hexalith.EventStore/Hexalith.EventStore.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.Parties/src/Hexalith.Parties.Client/Hexalith.Parties.Client.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `true` |
| `references/Hexalith.Parties/src/Hexalith.Parties.Contracts/Hexalith.Parties.Contracts.csproj` | 2 | `net11.0` | `5.1.0` | `true` | `true` |
| `references/Hexalith.Parties/src/Hexalith.Parties.EventStoreExtensions/Hexalith.Parties.EventStoreExtensions.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `true` |
| `references/Hexalith.Parties/src/Hexalith.Parties.Projections/Hexalith.Parties.Projections.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `true` |
| `references/Hexalith.Parties/src/Hexalith.Parties.Security/Hexalith.Parties.Security.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `true` |
| `references/Hexalith.Parties/src/Hexalith.Parties.UI/Hexalith.Parties.UI.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `true` |
| `references/Hexalith.Parties/src/Hexalith.Parties/Hexalith.Parties.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `true` |
| `references/Hexalith.Tenants/samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.Tenants/src/Hexalith.Tenants.Api/Hexalith.Tenants.Api.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.Tenants/src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj` | 2 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj` | 2 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.Tenants/src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |
| `references/Hexalith.Tenants/src/Hexalith.Tenants/Hexalith.Tenants.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |
| `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |
| `tests/Hexalith.FrontComposer.CounterFixture/Hexalith.FrontComposer.CounterFixture.csproj` | 1 | `net11.0` | `5.1.0` | `true` | `false` |

The current standalone controls below use Debug with no forwarded source globals or ambient module-source flags. These **seven project-specific SDK42 controls select `4.2.0`**, while the hosted inherited contexts select `5.1.0` because the global property is already supplied before the catalog's empty-value default. This context divergence is explicit; the standalone value is not substituted for the hosted value, and “all SDK42 views false” is not claimed.

| Standalone Debug project | SDK42 view | Standalone EventStore | Hosted inherited EventStore |
|---|---|---|---|
| `references/Hexalith.Parties/src/Hexalith.Parties.Client/Hexalith.Parties.Client.csproj` | `true` | `4.2.0` | `5.1.0` |
| `references/Hexalith.Parties/src/Hexalith.Parties.Contracts/Hexalith.Parties.Contracts.csproj` | `true` | `4.2.0` | `5.1.0` |
| `references/Hexalith.Parties/src/Hexalith.Parties.EventStoreExtensions/Hexalith.Parties.EventStoreExtensions.csproj` | `true` | `4.2.0` | `5.1.0` |
| `references/Hexalith.Parties/src/Hexalith.Parties.Projections/Hexalith.Parties.Projections.csproj` | `true` | `4.2.0` | `5.1.0` |
| `references/Hexalith.Parties/src/Hexalith.Parties.Security/Hexalith.Parties.Security.csproj` | `true` | `4.2.0` | `5.1.0` |
| `references/Hexalith.Parties/src/Hexalith.Parties.UI/Hexalith.Parties.UI.csproj` | `true` | `4.2.0` | `5.1.0` |
| `references/Hexalith.Parties/src/Hexalith.Parties/Hexalith.Parties.csproj` | `true` | `4.2.0` | `5.1.0` |

### Valid Release/package property contexts and exercised lane identities

| Root query / exercised lane | Identity, value and scope |
|---|---|
| AppHost Release with `UseNuGetDeps=true` | Own `net11.0` property `5.1.0`; source flags false, project-reference mode false, direct `Hexalith.EventStore.Aspire` PackageReference centrally selects `5.1.0`. Direct project paths exist. This is a valid own property/item query, not proof of the entire Release reference graph or package runtime. |
| UI Release with `UseNuGetDeps=true` | Own `net11.0` property `5.1.0`; source flags false, project-reference mode false. There is **no direct EventStore PackageReference** and `FrontComposerUiUsePublishedModulePackages=false`; it is a property-only context, not an additional EventStore package consumer. |
| CounterFixture | Valid inherited Debug/source context resolves the selected EventStore.DomainService source path. Its standalone Release/package interpretation remains invalid and excluded: empty EventStorePath plus dangling unconditional source edge. |
| Gate 2c live provider | Release-built provider and its 80-test suite exercise EventStore source `fdb59895975e86a1e234325a01237f259cdbd53b`; observed source assembly and Builds identities match above. |
| Epic 9 / invalid bearer | Existing scripts build Debug source at exact FrontComposer `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`, then launch the same source AppHost with the selected EventStore source gitlink. |
| Shared CI | [CI 43755164227](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755164227) completed success at this exact commit. No EventStore NuGet `5.1.0` package-mode runtime compatibility is inferred from the source live lanes or property queries. |

**Scoped property conclusion and explicit limits.** The 35 actual source-consumer property contexts, their seven standalone SDK42 controls, and the two valid own Release root queries are directly evidenced. Four unresolved **nonconsumer** outer Contracts/Schema negotiation contexts remain (Contracts contexts 26/37, Schema 60/73), each declaring `net11.0;netstandard2.0`; their four dependent Schema-inner edges to Contracts (172/173/175/176) are not asserted to have build-selected frameworks. Eight declared inner candidates were evaluated only within their own projects, never propagated as invented child framework globals. No build/restore/GetNearestTargetFramework task was run. These nonconsumer negotiation cases do not inflate the 35 consuming contexts. Aspire path-only launch contexts, unmodeled launch-time globals, restore-selected package assets and package-mode runtime coverage remain outside the property-evaluation conclusion. D4 is complete only to this stated property scope; no blanket framework, launch or runtime completeness is claimed.

### Source-fix and protected-manual outcomes

The prior failed 8ce record remains failed history. Source fixes `984b6be79a09e5e5796f75c9c0eb35ab4ccc2707` and `a6ed4b94b74aacf833c76ba1391a51e383bd6478` are now ancestors of this green successor. The former uses the captured non-null EditContext in both framework branches, aligns the async replacement/extraction anchors, and repairs the scoped identity-scan/test cleanup/wait behavior; the latter aligns the captured validation contract assertions across C#, integration and browser checks. The primary [test-results artifact 14179161540](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930/artifacts/14179161540), expiring `2026-11-29T20:21:03Z`, reports each formerly failed invocation Passed:

| Previously failed invocation | Primary default-lane outcome on green successor |
|---|---|
| `Hexalith.FrontComposer.SourceTools.Tests.Emitters.CommandFormValidationCompilationTests.EmittedValidationCompilesWithoutWarningsAgainstTheSupportedFramework(frameworkMajor: 11)` | `Passed` |
| `Hexalith.FrontComposer.SourceTools.Tests.Emitters.CommandFormValidationCompilationTests.EmittedValidationCompilesWithoutWarningsAgainstTheSupportedFramework(frameworkMajor: 10)` | `Passed` |
| `Hexalith.FrontComposer.Shell.Tests.Generated.CommandTargetGeneratedFormTests.SynchronouslyBlockingProvider_DeadlineStillAllowsDispatch` | `Passed` |
| `Hexalith.FrontComposer.Shell.Tests.Services.Lifecycle.UlidFactoryTests.ProductionCommandIdentityPaths_DoNotUseGuidIdentityApis` | `Passed` |
| `Hexalith.FrontComposer.Shell.Tests.Generated.CommandRendererWrapperIntegrationTests.GeneratedForm_SubmitAccepted_RendersLifecyclePhasesAndKeepsFormPresent` | `Passed` |

Protected manual concurrency is committed in `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`. It keeps the automatic group `quality-<github.ref>` and isolates each manual run as `quality-manual-<github.run_id>`. The main-only acquisition guard, event-base policy, job/step gates, permissions, gitlinks and execution pins are unchanged by that scheduling change. The primary automatic run and [supplemental isolated main manual run 43755164106](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755164106) both completed success on the exact same commit. The supplemental run is corroborating scheduler evidence only; none of its job counts or artifacts enter this record.

### Capture commands and retained scope

All capture identity reads use the explicit committed tuple and catalog, and run-bound summaries come from the primary automatic attempt only:

```bash
git fetch origin main
git rev-parse HEAD origin/main
git ls-remote origin refs/heads/main
git ls-tree e0eef14ddbbcd03823863a4f417a4b07f6f5bafc references/Hexalith.EventStore references/Hexalith.Builds
git show e0eef14ddbbcd03823863a4f417a4b07f6f5bafc:Directory.Packages.props
git -C references/Hexalith.Builds show 73e60af8a1fcdf3f06726d5d1b6e1199cce778ee:Props/Directory.Packages.props
gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930
gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930/attempts/1/jobs --paginate
gh api repos/Hexalith/Hexalith.FrontComposer/actions/jobs/132937646575/logs
gh api repos/Hexalith/Hexalith.FrontComposer/actions/jobs/132937646633/logs
gh api repos/Hexalith/Hexalith.FrontComposer/actions/artifacts/14178432272/zip
gh api repos/Hexalith/Hexalith.FrontComposer/actions/artifacts/14178247242/zip
gh api repos/Hexalith/Hexalith.FrontComposer/actions/artifacts/14179161540/zip
```

The read-only source queries reproduce each evaluated edge's globals using `dotnet msbuild <project> -nologo -nr:false -m:1 <inherited -p:Name=Value arguments> -getProperty:Configuration,TargetFramework,TargetFrameworks,HexalithEventStoreVersion,HexalithEventStoreFromSource,UseHexalithProjectReferences,UseNuGetDeps,HexalithPartiesSdk42CatalogView,HexalithPlatformSdk42CatalogView,HexalithBuildsPackagedHostSdk42CatalogView -getItem:ProjectReference,PackageReference`. The saved queries also requested full path and SDK global-removal metadata; exact argv and safe grouped results remain ignored local scratch under `d4-current-e0eef14d-*`. Release root controls add `-p:Configuration=Release -p:UseNuGetDeps=true` and query `EventStorePath`, UI package-mode flag and central PackageVersion items. Standalone controls supply only Debug configuration. No build/restore was run for property evaluation.

Primary runtime execution uses the unchanged workflow commands: provider restore/build/test in Release followed by ProviderVerification `--verification-mode live-compatibility` against the tracked Pact inputs; `FC_EPIC9_EXPECTED_COMMIT=e0eef14ddbbcd03823863a4f417a4b07f6f5bafc FC_EPIC9_REQUIRE_CLEAN=true ./eng/run-epic9-live-proof.sh`; then `HexalithFrontComposerFromSource=true ./eng/verify-apphost-invalid-bearer.sh`. Only safe identities/counts/commands/URLs are committed; existing CI artifacts/logs retain their own expiry limits.

## Engineering Acceptance Receipt — 2026-11-15

At `2026-11-15T20:32:40Z`, the parent accepts Story 16.1's evidence-capture implementation under its approved criteria, using the later green successor `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc` and primary Quality `43755163930` attempt 1. This engineering acceptance includes the requesting-user-approved bounded D1 historical scope exception (“approve both and continue”). Historical AC 3 and strict-attribution failures remain disclosed; both earlier Tuple Records and the D4 Review Addendum are unchanged. Story 16.1 is `done` for engineering capture, with D4 limited to the explicit evaluated property scope.

This is not EventStore/FrontComposer/Release migration approval, role equivalence, transfer of authority, a named-owner scheduling disposition, or publication authorization. No migration receipt is fabricated; Stories 16.2/16.3 and their pending role decisions remain unchanged. G-3 stays open on its named approvals and exact candidate/record-descendant lineage requirements. The passing-workflow evidence applies only to `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`; the later documentation commit receives no automatic evidence or approval transfer.

Remaining bounded review work is tracked separately: pre-existing derived-value BeforeSubmit/revision semantics (BH2); the mutable-model window associated with the concurrent freshness check from `923988d855cd6131b4287e3da763f12baf60ab22` (BH3); future durable CI execution of the manual-concurrency expression regression checks from `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc` (QC-BH1/QC-VG1); the primary Epic 9 configured retry; and the explicit nonconsumer framework/launch/restored-package/runtime limits above. A green run does not prove these untested scenarios or relabel earlier failures as successful.
