---
title: 'Story 13.6: Stabilize remaining automated verification'
type: 'bugfix'
created: '2026-10-17'
status: 'done'
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

- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStoreClientTests.cs:1307` — register cancellation after `ReadAttempted`, before advancing fake time; reuse line 1185's pattern. Await bounded dispatch, then cancellation within five seconds. Preserve identity, sends, unfinished read, disposal and late-outcome assertions.
- [x] `tests/e2e/specs/accessibility-state-matrix.spec.ts:358` — reproduce dark-shell contrast five times without retries; inspect the reported 1.7px shadow-root overlap. Patch only a demonstrated defect here or in the mapped helper; add positive/negative regression proof. Otherwise retain the unresolved flake.
- [x] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md` — bind current outcomes/blockers to the candidate; preserve historical evidence, manual boxes and open status.

**Acceptance Criteria:**

- Given four cleanup scenarios, when rebuilt tests run, then existing safety assertions pass independently of cancellation scheduling.
- Given the Firefox failure, when verification finishes, then a causal regression proves its correction or it remains unresolved.
- Given failed/missing gates, when results are reported, then passing subsets never establish story completion.

## Implementation Notes

No intent gaps or irreversible operations. Footprint: existing tests/helper/result. Start/inspect the exact AppHost through Aspire before code edits; use its Test endpoint.

All 27 prior source inputs and 24 image hashes match. Prior browser: 567 passed, 12 failed, one flaky, two skipped. Shell: 3692 passed, three failed.

Open: five catalog-policy mismatches; Parties-owned overrides; Fluent 5.0.0 assets versus selected 5.0.1; hosted visual reconciliation; Windows/WebKit actual forced palette; manual AT/zoom/devices; OI-16/G-4/Fluent/Product/live-backend decisions. A temporary policy diagnostic exposed Parties drift; no policy-only fix can close the gate.

## Spec Change Log

- 2026-10-18 (code review of `02626ead`): production fix accepted as in-scope. The footprint sentence "existing tests/helper/result" and "No production fix is established" predate the finding. A fast Escape leaves Fluent's delayed 25ms `previousActiveElement` return live and undoes the operator's Tab. `fc-focus.js` (`containDialogFocus`, palette and Settings dialogs only) and `FcCommandPalette.razor.cs` (containment before entry focus, `_disposed` guards) fix it. The e2e test `repeatable palette dismissal preserves Tab through delayed entry and native return callbacks` proves it, and Story 13.6 permits correcting demonstrated accessibility failures.

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-01 transformed scrollport | medium | patch P1 | Parent replay confirms the original helper rejects visible background paint while the new candidate clipping reports 21:1; viewport rectangles and unscaled client dimensions diverge under scale(2). |
| BH-02 overflow clip margin | medium | patch P2 | Parent replay confirms a 30px overflow clip margin visibly retains background paint that the new padding-box intersection removes. |
| BH-03 inline overflow | medium | patch P3 | Parent replay confirms a non-replaced inline ancestor has zero client dimensions but does not clip the visible background; the new helper accepts it. |
| BH-04 fractional scrollport | medium | patch P4 | Parent replay confirms a 60.1875px scrollport rounds to clientHeight 60 and removes a real 0.09375px overlap that the original rejects. |
| BH-05 negative inset | medium | patch P5 | Parent replay confirms applying the inherited inset parser to competing backgrounds clamps an expanded negative inset, newly hiding visible paint. |
| BH-06 bounded report assets | medium | patch P6 | audit.mjs copies only index.html from a 502-file report; restored bounded indexes retain links to attachment assets absent from their report tree. |
| BH-07 missing archive roots | medium | patch P7 | pack.py unconditionally continues for absent declared roots before publishing a new completeness manifest; rerunning it can silently omit evidence. |
| BH-08 audit exit status | medium | patch P8 | audit.mjs records validator status and state errors without setting a failing exit status; automation cannot rely on process success to establish validity. |
| BH-09 replay portability | medium | patch P9 | Exact causal replay imports the author's absolute checkout and testDir; audit.mjs hardcodes temporary input roots. Restored evidence cannot be replayed from another location. |
| BH-10 optimized restore checks | medium | patch P10 | restore.py uses assert for hashes, byte counts and destination containment; Python optimization removes those claimed verification checks. |
| EC-01 transformed scrollport | medium | patch P1 | Independent parent replay confirms this scaled fixture accepts visible black background paint as 21:1; same demonstrated coordinate defect as BH-01. |
| EC-02 overflow clip margin | medium | patch P2 | Independent parent replay confirms expanded clip paint disappears from the measured background; same demonstrated defect as BH-02. |
| EC-03 display contents | medium | patch P3 | Independent parent replay confirms a display:contents ancestor has no clipping box, but its zero client dimensions erase the visible candidate. |

All three review layers returned before classification. The verification-gap layer found no gaps. Every filed finding was verified before grouping; none was dropped or deferred. P1–P10 are bounded corrections to the changed measurement/evidence code, add no public surface, and guard only reproduced conditions. Historical raw observations must remain retained. The evidence and full browser results above describe the pre-review candidate; a new candidate and affected verification are required after the measurement patches.

## Verification

- Debug-build Shell.Tests; run `EventStoreClientTests` with xUnit `-class`, then the configured lane/five exclusions. Set `DiffEngine_Disabled=true`; record restore/asset limitations.
- `npm --prefix tests/e2e run typecheck`; Firefox dark-shell case: `--workers=1 --retries=0 --repeat-each=5`.
- Browser changes require the existing three accessibility specs on all three engines and existing per-run/attempt validators; preserve failures/images.
- `node --test tests/e2e/scripts/a11y-state-evidence.test.mjs`; visual governance; `git diff --check`.

Parent implementation audit: all three execution tasks and the three acceptance criteria are satisfied for this bounded stabilization. The four initial/retry × late-success/fault cleanup cases passed in the rebuilt 69-test class and configured Shell lane. Firefox causal replay fails on the original helper; the current helper passes all five zero-retry journeys and five positive/negative fixtures. The complete three-engine lane covers 320px, spacing and reduced-motion cases and retains every visual failure and skip. Artifact-authority tests pass 12/12; all 116 state rows per engine have final-attempt attribution. The refreshed result explicitly retains version/visual, platform/manual and owner gates as open. Parent source/image hash audit passed. [Verification report](evidence/story-13-6/2026-10-16-gap-closure/stabilize-2026-10-17/report.md) records 571/12/0/2 browser and 3693/2 Shell outcomes; this does not establish Story 13.6 completion.

Post-review verification: P1–P10 are patched; all thirteen individual findings retain their verified verdicts above. Six new geometry negatives plus the original clipping fixture execute 20 passes and one explicit WebKit capability skip. Five evidence-tool checks pass, including optimized-Python corrupt-input and missing-root preservation cases. Parent replay confirms the five former geometry false passes now reject. The final configured browser lane is 588 passed / 12 visual failures / 0 flaky / 3 skipped / 603; the Firefox five-repeat lane is 10/10 without retries. The configured Shell lane remains 3693 passed / 2 policy failures, with all four cleanup rows passing. An omitted existing Test notice setting was corrected in the parent verification launch; its interrupted failed attempt remains retained and no production default changed. [Current verification](evidence/story-13-6/2026-10-16-gap-closure/stabilize-2026-10-17/parent-verification/configured-host/report.md) binds `sha256:e23d846e6e8f87f26f997866a8e82aa355e8ae89f1f94b67d4fa793add1db7b0`. Story 13.6 remains in-review and incomplete for retained visual/catalog/platform/manual/owner gates.

The done status closes this bounded stabilization specification only. Sprint Story 13.6 remains in review and incomplete until its retained automated, platform, manual and owner gates are satisfied. The explicit no-staging/no-commit/no-push constraint remains in force.

### Review Findings

Code review of `02626ead` (5 code files, +186/−14), 2026-10-18.

- [x] [Review][Decision] Production source changed beyond the spec footprint — Implementation Notes limit the footprint to "existing tests/helper/result" and the Code Map says "No production fix is established", yet `FcCommandPalette.razor.cs` (entry focus now follows `containDialogFocus`, two `_disposed` returns) and `fc-focus.js` (permanent `previousActiveElement` override, native `cancel`/`close` listeners) changed. The diff is not tied to the two stated problems (retry-cleanup assertion, Firefox dark-shell contrast). Options: (1) accept as a sanctioned production fix and record it in Spec Change Log / Code Map; (2) split it into its own story or spec and revert it from this one. **Resolved 2026-10-18: option 1 — accepted; recorded in Spec Change Log.** [src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-focus.js:273, src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs:113]
- [x] [Review][Defer] Palette `_disposed` guards are untested — no bUnit test holds the keyboard/focus interop pending, disposes, then releases and asserts `focusElement` is not invoked. Removing both guards keeps every existing test green. [src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs:113] — deferred: narrow harm (post-dispose focus/interop call); the ordering change itself is covered by the new e2e test.
- [x] [Review][Defer] `host.shadowRoot?.querySelector('dialog')` read once at registration; if Fluent has not rendered the native dialog yet, `cancel`/`close` listeners are never attached and `containedDialogs.add` blocks retry [src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-focus.js:296] — deferred (unverified medium): already recorded in `deferred-work.md` from `spec-13-6-make-browser-accessibility-verification-repeatable.md`; settle with the actual Settings/palette component under delayed Fluent native initialization.

#### Rejected

- `previousActiveElement` override unscoped (Blind, Edge, Auditor) — false: it sits inside `if (testId === 'fc-palette-root' || testId === 'fc-settings-dialog')`; the destructive dialog is untouched.
- `state.dismissed` never reset on reuse (Blind, Edge) — false: `dialogReturns.set(host, {...})` builds a fresh state on every opening, and the reuse regression test covers it.
- `defineProperty` may throw on a non-configurable property (Edge) — false/unreached: the property is writable (`host.previousActiveElement = null` just ran) and the e2e lanes pass.
- Cancelled Escape disarms tracking (Edge) — low, no demonstrated path where the palette or Settings cancel is prevented; the fix adds a branch.
- `_disposed` early return suppresses palette feedback/speech (Auditor, Edge) — false: `QueueActivationFailureStatus` already stops on `_disposed` (line 494), so a disposed palette never spoke.
- R15 test dropped its explicit `containDialogFocus` call (Blind, Auditor) — low: the test now exercises the component's own containment and fails on its focus assertions if it never attached.
- Zoom guard only when `plane` is set / `'1.0'`, `'100%'` forms (Blind, Edge) — low: browsers serialize computed zoom as a number, and the non-plane path is not circular-radius coverage.
- NaN / percent / elliptical radii (Blind, Edge) — false: they fail closed (`covered()` returns false), never over-certify.
- Missing `filter`/`clip-path`/`mask`/`mix-blend-mode` check, `corner-shape` absent on engine (Edge) — pre-existing and low; engines without `corner-shape` cannot paint bevel either.
- Untested `scale`/`rotate`/`transform`/`backgroundClip` disqualifiers and capability-skipped bevel test (Blind, Verification Gap) — low: fail-closed conditions; adding per-branch tests is disproportionate.
- Brittle `setTimeout` stack match / `fc-keyboard.js` route rewrite / leaked `__aeReleasePaletteEntry` (Blind, Edge, Auditor, Verification Gap) — low/false: a miss fails loudly via `queued > 0`, and the page is fresh per test.
- `.gitattributes` names directories that do not exist (Blind, Auditor) — false: both are tracked (104 and 36 files).
- No `EventStoreClientTests` / `accessibility-state-matrix` / result hunks, unseen P6–P10 patches (Auditor) — false: those changes live in earlier commits, outside the reviewed diff.
- Spec status `done` vs `review_loop_iteration: 0` (Auditor) — rejected: the fix would edit the spec under review.
