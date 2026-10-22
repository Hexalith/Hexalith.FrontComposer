---
title: 'Epic 13 retrospective follow-through'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'bf40b9276f440646e753a0636854595cc7d0affc'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-retro-2026-10-22.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 13 has unsafe focus disposal, broken historical replay, tracker drift, missing human observations and advisory live tenant coverage.

**Approach:** Keep E13R-AI-1–5 together: repair lifecycle/replay, reconcile tracking, and execute existing manual/live checks when prerequisites exist.

## Boundaries & Constraints

**Always:** Preserve concurrent edits, historical evidence, approvals, dependencies and baselines. Reuse fixtures, verifiers and MAN-136; close actions only with evidence.

**Never:** Invent manual results/approvals; close OI-16/Fluent/G-4; alter sealed packets; publish/delete evidence; expand into full 15.6/15.7 migration; commit/push.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behavior | Failure handling |
|---|---|---|---|
| Lifecycle | Import held across disposal | Release late module once; no callback allocation/focus/recovery after disposal | Preserve disconnected-JS handling |
| Normal focus | Mounted owner; import completes | Existing route focus works | Preserve recovery semantics |
| Replay | Historical inputs available | Verified isolated reconstruction; original failing accessibility outcomes retained | Missing/corrupt inputs fail closed |

## Decisions — 2026-10-22

The user's “apply recommendation” approves implementation of all five actions. Prepare the existing manual handoff; AI-2 stays open pending named human observations. Preserve delivery history, retain primary 13.6 `in-review`, and reconcile sprint acceptance to `review`, explicitly superseding the storage amendment's status preservation. Reuse the existing tenant fixture and zero-skip validator at the documented relevant-change trigger. No new acceptance or dependency approval is supplied.

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcRouteFocus.razor.cs`: lifecycle owner; preserve navigation semantics.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcRouteFocusTests.cs`: existing bUnit regressions; separate new helper types.
- `eng/replay_story_13_6.py`, `tests/eng/test_replay_story_13_6.py` (new): reuse sealed audit/state verifiers.
- `_bmad-output/implementation-artifacts/tests/story-13-6-historical-runtime.json` (new): hash-bound reconstruction recipes.
- `tests/Hexalith.FrontComposer.CounterFixture/README.md`: existing opt-in AppHost provisioning. `.github/scripts/ci_governance.py` already enforces exact Passed test identity.

## Tasks & Acceptance

**Execution:**

- [x] `src/Hexalith.FrontComposer.Shell/Components/Layout/FcRouteFocus.razor.cs` and existing test above — invalidate acquisition; detach before idempotent disposal; guard callbacks/recovery. Test held import, late failure, repeated disposal and normal focus.
- [x] `eng/replay_story_13_6.py`, `tests/eng/test_replay_story_13_6.py`, `_bmad-output/implementation-artifacts/tests/story-13-6-historical-runtime.json` — reconstruct external scratch from Git `397dac34a1bd1bf4d789161794a66811360cb59a`; verify size/hash before execution. Runtime/package JSON: CRLF; lockfile: CRLF except LF on lines 14/15; 24 PNGs: raw bytes. Restore locked npm dependencies, clear NODE_PATH, record versions. Test corruption, missing history and isolation.
- [x] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md` — append replay command and MAN-136 handoff/observations; preserve history and blockers.
- [x] `_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md`, `sprint-status.yaml`, `deferred-work.md` in that directory — reconcile dated delivery/verification and UX-A–F evidence; update only satisfied E13R actions/DW-1987, preserving concurrent changes.
- [x] `tests/Hexalith.FrontComposer.CounterFixture/README.md` — require zero-skip query/count/SignalR proof before approving scope/auth/storage/adapter/server/AppHost/EventStore changes; rerun after relevant changes. Provision existing AppHost/realm, keep tokens in memory, retain candidate-bound sanitized receipt. Preserve advisory defaults.

**Acceptance Criteria:**

- Given a clean checkout and historical Git objects, when supported replay runs, then the wrapper exits 0 only with matching outcomes and structural integrity for 2,079 files and 116 states/engine; raw replay remains exit 1 with 752 passed/18 failed/4 skipped/0 flaky and blocked classification. Drift fails closed.
- Given the selected tracking decision, when records are reconciled, then delivery, blocked verification and approval remain distinct and evidence-linked.
- Given manual or tenant prerequisites are unavailable, when reporting completion, then actions remain open with exact blockers; automation cannot replace human observations.

## Implementation Notes

- Approval received on 2026-10-22. HEAD advanced externally to `51a9bd04bb5bc118e549737a735912a7f01e6349` during planning; preserve that Epic 15.3 work and the original baseline. No irreversible operation is authorized.

- Parent acceptance audit read the complete 2,822-line cumulative diff at `/tmp/frontcomposer-e13r-audit-cywaqu_3.diff`, including preserved external Epic 15.3 changes. All five implementation tasks satisfy the approved conditional scope: manual observations and live proof remain open, as authorized. Receipt input hashes match current files.
- Verified logs: focused Debug build zero warnings/errors; lifecycle 21/21; Python 8/8; Node 24/24; historical reconstruction 2,079 files/116 states per engine with unchanged 752/18/4/0 and blocked classification.
- Parent rebuilt Counter in Debug, then ran three existing Chromium route-contract journeys (module routing, invalid-tab fallback, palette/settings focus return): 3/3 passed. External artifacts only; synthetic Test host stopped afterward. This supplies browser regression evidence, not live tenant or manual AT proof.
- Matrix audit: held-import/disposal and disconnected cleanup are covered by `ImportCompletedAfterDisposalReleasesModuleWithoutFocus`; mounted focus by `MountedImportFocusesNormallyAndRepeatedDisposalReleasesOnce`; verified historical replay by eight Python regressions plus the executed supported replay. All covering checks ran and passed.
- Full topology remains blocked by Tenants.UI CS1704 source/NuGet Memories duplicates after successful source restores. The live test skipped once and exact-identity validation rejected NotExecuted. E13R-AI-2/5 remain open; E13R-AI-1/3/4 and DW-1987 are complete. No healthy full-topology baseline preceded the patch.

- Final parent verification after review patches: focused Debug Shell build zero warnings/errors; FcRouteFocusTests **22/22**; replay Python suite **11/11**; Node state-evidence suite **24/24**. The supported command reran successfully with exact original blocked outcomes. Existing Chromium journeys remain **3/3** against unchanged production focus source.
- Final live-method retry again skipped once; fresh TRX exact-identity validation again rejected NotExecuted. This remains failed prerequisite evidence, with no live scope assertion executed. The receipt now binds final tested inputs/assembly and their explicit raw-byte convention.
- Reviewed all patch files directly. All seven direct review corrections are complete; nine deferred findings are retained in seven grouped pre-existing/unverified follow-ups. Full approved frozen intent matches the initial audit snapshot. CI YAML parses and the new suite is wired into the existing artifact-validator helper step.
- The approved frozen intent prohibits commits and pushes; that user-approved boundary overrides Step 5's default local-commit action. This bounded follow-through is complete; E13R-AI-2/5 and primary Story 13.6 acceptance remain open. Concurrent Story 15.3 edits were preserved.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence / disposition |
|---|---|---|---|
| BH-1 overlapping imports | medium | defer | Both baseline `??= await import` and current assignment can overwrite an acquisition when renders overlap. The external compiled-assembly probe releases modules 0/1 times. Pre-existing ownership issue, distinct from held-import after-disposal R-1. |
| BH-2 queued browser observer | medium | defer | Existing fc-focus.js:869–1040 retains an observer/timers independently of an IJSObjectReference. Baseline DisposeAsync also only released that reference. Owner-scoped cancellation needs composed browser proof and must preserve other mounted owners. |
| BH-3 pending confirmation after owner removal | maybe-false | defer | Unregistration only decrements owners, but baseline disposal already invalidated the JS callback reference and scope changes call ResetForScopeChange. A composed owner-removal case without replacement/scope reset is needed to prove the claimed indefinite palette wait; potential medium, unverified. |
| BH-4 migration ordinary target dedup | medium | defer | ProjectDocumentLoader groups canonical paths and keeps First before the 15.3 safety policy rejects links. Listing a symlink first can hide its ordinary target. This is preserved external 15.3 work, not introduced by this follow-through. |
| BH-5 catalog placeholder collision | medium | defer | Tokenization replaces literals with valid identifier strings __literalN, so real identifiers can select unrelated entries in the literal array. The implementation is preserved external 15.3 work. |
| BH-6 catalog character literal | medium | defer | The external 15.3 tokenizer has no character-literal alternative; an unrelated quote character can consume later string syntax and reject a valid catalog. Reviewer reproduced the publication failure. |
| BH-7 Python suite absent from CI | medium | patch | Quality explicitly names Python suites and omits the new replay suite. Add it to an existing helper-test step, with no new lane. |
| BH-8 raw receipt newline convention | low | patch | Realm/live-test files currently have mixed EOL; parent confirmed CRLF normalization changes their digests. Raw hashes correctly bind executing bytes; clarify this representation rather than replacing the evidence with normalized hashes. |
| BH-9 failed replay scratch diagnostics | medium | patch | Materialization/version/outcome errors can escape after mkdtemp without identifying retained scratch/audit output. Add scratch context and a bounded local failure receipt. |
| BH-10 subprocess time budgets | maybe-false | defer | Calls have no explicit wrapper timeout, but no stalled supported child was demonstrated. Potential medium, unverified: reproduce hung tool/restore/audit, select budgets, and test process-tree teardown on supported OSs before adding a timeout mechanism. |
| EC-1 overlapping imports | medium | defer | Same verified pre-existing acquisition-overwrite cause as BH-1; preserve as an individual finding, group its follow-up once. |
| EC-2 manifest changes during replay | medium | patch | Receipt rereads the manifest after executing a previously parsed snapshot. Hash the original loaded bytes so the receipt always identifies the reconstruction actually used. |
| EC-3 Windows temporary paths | medium | patch | Local Node os.tmpdir source confirms Windows selects TEMP/TMP, while wrapper sets only TMPDIR. Set all three to replay-output and cover the environment boundary; no Windows execution is claimed. |
| EC-4 catalog character literal | medium | defer | Same verified preserved 15.3 tokenizer cause as BH-6; one grouped follow-up. |
| VG-1 command failure exit status | medium | patch | Pre-verified mutation: changing main failure return to 0 leaves all eight tests green and a corrupt external fixture exits 0. Add an actual CLI corruption regression asserting exit 1 and no success receipt. |
| VG-2 repeated disposal with surviving owner | medium | patch | Pre-verified mutation: removing the disposal guard still releases the detached module once but unregisters twice. Add two real owners and pending/confirmed notifier assertions. |

Grouping: BH-1/EC-1 share acquisition overwrite; BH-6/EC-4 share missing character-literal tokenization. Other causes remain distinct. All current-change survivors have direct corrections or focused tests; no public API, intent gap or re-derivation is required.

Patch resolution: the command hashes its loaded manifest snapshot, confines TMPDIR/TEMP/TMP, and reports retained scratch/failure diagnostics. Real-CLI failure and two-owner notifier regressions pass. CI invokes the replay suite in its existing helper step; the receipt states the exact tested raw-byte convention. Parent verification below covers the reviewed tree.

## Commit Scope Dispositions

- `51a9bd04bb5bc118e549737a735912a7f01e6349` | shared | Independently owned Epic 15.3 work landed after this plan's baseline; preserved and excluded from this follow-through's delivery ownership.
- `02af22194d12b5101157560e7bbe4d47e8d6c79e` | shared | External Epic 15.3 finalization also recorded the contemporaneous follow-through spec; preserve history and judge current implementation by its explicit File List.

## Verification

- Debug build the individual Shell test project; invoke its built assembly with `-class '*FcRouteFocusTests'` and separately `-class '*LiveTwoTenantProductionAdapterTests'`.
- Validate fresh live TRX with `.github/scripts/ci_governance.py validate-mtp-evidence`, `Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.LiveTwoTenantProductionAdapterTests.ProvisionedTenants_QueryCountAndSubscribe_StayIsolated`, `--expected-trx-files 1 --require-tests --require-distinct-modules`.
- `python3 -m unittest discover -s tests/eng -p test_replay_story_13_6.py`; execute supported scratch replay; `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs`.
- `python3 eng/validate-story-artifacts.py`; `git diff --check`. Record exact blockers and baseline fallback evidence.

## File List

- `.github/workflows/quality.yml`

- `_bmad-output/implementation-artifacts/spec-epic-13-retrospective-follow-through.md`
- `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`
- `_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/tests/epic-13-follow-through-2026-10-22.json`
- `_bmad-output/implementation-artifacts/tests/story-13-6-historical-runtime.json`
- `eng/replay_story_13_6.py`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcRouteFocus.razor.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcRouteFocusTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/HeldRouteFocusRuntime.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/RecordingRouteFocusModule.cs`
- `tests/eng/test_replay_story_13_6.py`
- `tests/Hexalith.FrontComposer.CounterFixture/README.md`
