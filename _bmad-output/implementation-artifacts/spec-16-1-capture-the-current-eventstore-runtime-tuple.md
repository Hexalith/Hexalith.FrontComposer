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

## File List

- `_bmad-output/implementation-artifacts/epic-16-context.md` — pre-existing context created by planning commit `312af896e57821e26ae654b1817f14914f3371f7` before implementation; read in full and unchanged by this story.
- `_bmad-output/implementation-artifacts/spec-16-1-capture-the-current-eventstore-runtime-tuple.md` — tuple identities, same-attempt step conclusions, counts and commands, plus the completion-pass drift check, commit-scope dispositions and story verification.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — current Story 16.1 capture progress and completion-pass drift note; Epic 16 remains in progress and G-3 remains open.
- `_bmad-output/implementation-artifacts/deferred-work.md` — four review-iteration-1 deferrals (D1–D4) appended at the end of the ledger; no existing entry changed.

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
