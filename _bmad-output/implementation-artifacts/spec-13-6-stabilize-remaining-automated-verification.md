---
title: 'Story 13.6: Stabilize remaining automated verification'
type: 'bugfix'
created: '2026-10-17'
status: 'ready-for-dev'
route: 'dispatch'
story_id: '13.6'
baseline_commit: '27e7605d0032cb118332f131e6b022d9cb534ac9'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A retry-cleanup assertion and a Firefox dark-shell contrast flake undermine repeatable Story 13.6 evidence.

**Approach:** Synchronize cancellation observation, investigate the real Firefox overlap, and refresh the existing result. Preserve prior story decisions.

## Boundaries & Constraints

**Always:** Preserve acceptance/cleanup bounds, late-fault observation, tenant isolation, speech ownership, Fluent styling, typography/density and measurement thresholds. Use Debug locally; retain failed attempts.

**Never:** Change dependencies/policy/submodules, stage/commit/push, regenerate images, add public APIs/workflows, weaken checks, add blind delays, or infer human/platform approval. Missing manual evidence leaves the story incomplete.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Failure Handling |
|---|---|---|---|
| Cleanup | Initial/retry; late success/fault | Bounded acceptance, separate cancellation observation, finite cleanup, unchanged accepted result | Preserve all four rows |
| Firefox | 320px, spacing, reduced motion | Correct text/background measurements; real overlap rejected | Reproduce before patching |
| Gates | Version/visual failures; missing human evidence | Readiness stays open | Preserve exact failures |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs` — nested waits share a deadline; `CancelAsync` propagation can outlive dispatch. No production fix is established.
- `tests/e2e/helpers/responsive-accessibility.ts` — reuse paint audit/`settlePaint`; preserve obstruction negatives.
- Existing result links source manifests, historical reports and artifact validators; reuse those mechanisms.

## Tasks & Acceptance

**Execution:**

- [ ] `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStoreClientTests.cs:1307` — register cancellation after `ReadAttempted`, before advancing fake time; reuse line 1185's pattern. Await bounded dispatch, then cancellation within five seconds. Preserve identity, sends, unfinished read, disposal and late-outcome assertions.
- [ ] `tests/e2e/specs/accessibility-state-matrix.spec.ts:358` — reproduce dark-shell contrast five times without retries; inspect the reported 1.7px shadow-root overlap. Patch only a demonstrated defect here or in the mapped helper; add positive/negative regression proof. Otherwise retain the unresolved flake.
- [ ] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md` — bind current outcomes/blockers to the candidate; preserve historical evidence, manual boxes and open status.

**Acceptance Criteria:**

- Given four cleanup scenarios, when rebuilt tests run, then existing safety assertions pass independently of cancellation scheduling.
- Given the Firefox failure, when verification finishes, then a causal regression proves its correction or it remains unresolved.
- Given failed/missing gates, when results are reported, then passing subsets never establish story completion.

## Implementation Notes

No intent gaps or irreversible operations. Footprint: existing tests/helper/result. Start/inspect the exact AppHost through Aspire before code edits; use its Test endpoint.

All 27 prior source inputs and 24 image hashes match. Prior browser: 567 passed, 12 failed, one flaky, two skipped. Shell: 3692 passed, three failed.

Open: five catalog-policy mismatches; Parties-owned overrides; Fluent 5.0.0 assets versus selected 5.0.1; hosted visual reconciliation; Windows/WebKit actual forced palette; manual AT/zoom/devices; OI-16/G-4/Fluent/Product/live-backend decisions. A temporary policy diagnostic exposed Parties drift; no policy-only fix can close the gate.

## Spec Change Log

## Review Triage Log

## Verification

- Debug-build Shell.Tests; run `EventStoreClientTests` with xUnit `-class`, then the configured lane/five exclusions. Set `DiffEngine_Disabled=true`; record restore/asset limitations.
- `npm --prefix tests/e2e run typecheck`; Firefox dark-shell case: `--workers=1 --retries=0 --repeat-each=5`.
- Browser changes require the existing three accessibility specs on all three engines and existing per-run/attempt validators; preserve failures/images.
- `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs`; visual governance; `git diff --check`.
