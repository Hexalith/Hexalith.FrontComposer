---
title: 'Epic 13 retrospective follow-through'
type: 'bugfix'
created: '2026-10-22'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'bf40b9276f440646e753a0636854595cc7d0affc'
context:
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

</frozen-after-approval>

## Open Questions

1. Manual ownership: name testers for NVDA/Firefox, JAWS/Chrome, VoiceOver/Safari, zoom, tablet, phone and zero-field speech; or prepare the handoff and keep AI-2 open. Bind observations to the post-fix candidate.
2. Tracking policy: keep primary 13.6 `in-review` and reconcile sprint to `review`, preserving delivery history but explicitly superseding storage-amendment status preservation (recommended); or retain delivered `done` with a documented separate blocked-acceptance model requiring tooling support. Neither option supplies manual evidence or approval.

## Code Map

- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcRouteFocus.razor.cs`: lifecycle owner; preserve navigation semantics.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcRouteFocusTests.cs`: existing bUnit regressions; separate new helper types.
- `eng/replay_story_13_6.py`, `tests/eng/test_replay_story_13_6.py` (new): reuse sealed audit/state verifiers.
- `_bmad-output/implementation-artifacts/tests/story-13-6-historical-runtime.json` (new): hash-bound reconstruction recipes.
- `tests/Hexalith.FrontComposer.CounterFixture/README.md`: existing opt-in AppHost provisioning. `.github/scripts/ci_governance.py` already enforces exact Passed test identity.

## Tasks & Acceptance

**Execution:**

- [ ] `src/Hexalith.FrontComposer.Shell/Components/Layout/FcRouteFocus.razor.cs` and existing test above — invalidate acquisition; detach before idempotent disposal; guard callbacks/recovery. Test held import, late failure, repeated disposal and normal focus.
- [ ] `eng/replay_story_13_6.py`, `tests/eng/test_replay_story_13_6.py`, `_bmad-output/implementation-artifacts/tests/story-13-6-historical-runtime.json` — reconstruct external scratch from Git `397dac34a1bd1bf4d789161794a66811360cb59a`; verify size/hash before execution. Runtime/package JSON: CRLF; lockfile: CRLF except LF on lines 14/15; 24 PNGs: raw bytes. Restore locked npm dependencies, clear NODE_PATH, record versions. Test corruption, missing history and isolation.
- [ ] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md` — append replay command and MAN-136 handoff/observations; preserve history and blockers.
- [ ] `_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md`, `sprint-status.yaml`, `deferred-work.md` in that directory — reconcile dated delivery/verification and UX-A–F evidence; update only satisfied E13R actions/DW-1987, preserving concurrent changes.
- [ ] `tests/Hexalith.FrontComposer.CounterFixture/README.md` — require zero-skip query/count/SignalR proof before approving scope/auth/storage/adapter/server/AppHost/EventStore changes; rerun after relevant changes. Provision existing AppHost/realm, keep tokens in memory, retain candidate-bound sanitized receipt. Preserve advisory defaults.

**Acceptance Criteria:**

- Given a clean checkout and historical Git objects, when supported replay runs, then the wrapper exits 0 only with matching outcomes and structural integrity for 2,079 files and 116 states/engine; raw replay remains exit 1 with 752 passed/18 failed/4 skipped/0 flaky and blocked classification. Drift fails closed.
- Given the selected tracking decision, when records are reconciled, then delivery, blocked verification and approval remain distinct and evidence-linked.
- Given manual or tenant prerequisites are unavailable, when reporting completion, then actions remain open with exact blockers; automation cannot replace human observations.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- Debug build the individual Shell test project; invoke its built assembly with `-class '*FcRouteFocusTests'` and separately `-class '*LiveTwoTenantProductionAdapterTests'`.
- Validate fresh live TRX with `.github/scripts/ci_governance.py validate-mtp-evidence`, `Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.LiveTwoTenantProductionAdapterTests.ProvisionedTenants_QueryCountAndSubscribe_StayIsolated`, `--expected-trx-files 1 --require-tests --require-distinct-modules`.
- `python3 -m unittest discover -s tests/eng -p test_replay_story_13_6.py`; execute supported scratch replay; `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs`.
- `python3 eng/validate-story-artifacts.py`; `git diff --check`. Record exact blockers and baseline fallback evidence.
