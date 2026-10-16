---
title: 'Story 13.6: Verify Responsive and Assistive Accessibility'
type: 'feature'
created: '2026-10-15'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '3c8679628ebdb8995afc09f712a04fcde623695c'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Existing specimen checks do not establish AE-01–AE-09 across changed operator surfaces. The axe defaults omit WCAG 2.2, simulated zoom permits page overflow, and manual assistive/device evidence is missing.

**Approach:** Extend existing browser/component lanes, correct demonstrated accessibility failures, and assemble one candidate-bound result referencing reused and new evidence for shell, Home, navigation, tabs, toolbar, projection/detail, form, lifecycle, palette and dialogs.

## Boundaries & Constraints

**Always:** Follow canonical UX-AE-1/UX-VC-1/UX-RM-1. Preserve Fluent V5 ownership, nine typography mappings/version 3.1.0, default 32px compact rows, truthful virtualization, 72px/48px rails and 75rem constrained measure. Rows may grow under spacing/reflow. Preserve tenant isolation and 13.2–13.5 focus, lifecycle, speech and silent-expiry behavior. Missing human evidence leaves this story incomplete.

**Never:** Add independent palettes, type ramps, invented breakpoints, broad axe exclusions or duplicate workflows. Treat viewport resizing as actual browser zoom, DOM mutations as heard speech, or automation as manual evidence. Infer OI-16, G-4, FLUENT-APP-1 or Product approval.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected behavior | Failure handling |
|---|---|---|---|
| AE-01–03 | 320px; actual 400% zoom from 1280px; WCAG text spacing | No loss/clipping/page overflow; only labelled grid/tab regions scroll horizontally | Record exact failing surface; fix and rerun |
| AE-04–05 | Every pointer target and keyboard stop, including active overlays | 24×24px or one evidenced permitted exception; whole target/focus ring visible and unobscured | Reject missing inventories, invalid spacing circles/equivalents |
| AE-06–08 | Both themes, forced colors, reduced motion | Required contrast; immediate text/icon/shape/focus; no essential meaning lost | Retain computed-style/state evidence |
| AE-09 | Semantics, keyboard and speech sequences | WCAG 2.2 tags; no critical findings, duplicate speech or hover-only actions | Retain scoped findings and dispositions |
| Manual | Supported AT/device journeys | Dated human observations tied to candidate and versions | Missing runs remain unperformed/blocked |

## Decisions (2026-10-15)

The user approved both recommendations and requested application.

- Missing item: retain neutral unavailable-item copy. Focus its non-live explanation only when removal of the focused detail loses focus; otherwise preserve focus and announce once through the shared polite owner. Suppress duplicate speech and repeated refreshes; cancel stale recovery after scope/navigation/operator focus changes. Reuse focus-before-replacement; AM-22 remains filter-only.
- Zero-field failure: show compact Fluent feedback beside its trigger, wrapping below when needed. Carry the existing safe outcome from hidden form to renderer; keep trigger focus and one speech owner. Retain feedback until next attempt/navigation/scope change. Accepted commands continue through the existing pending summary. No automatic retry or new dialog/service.

</frozen-after-approval>

## Code Map

- Reuse journey specs under `tests/e2e/specs/`; preserve the fresh-row live-only lane.
- `docs/accessibility-verification/README.md` and `manual-log-template.md`: human NVDA/Firefox, JAWS/Chrome, VoiceOver/Safari and tablet/phone evidence.

## Tasks & Acceptance

**Execution:**

- [x] `tests/e2e/helpers/a11y.ts`, `tests/e2e/specs/a11y-helper.spec.ts` — add and prove default `wcag22aa` coverage in both helpers without weakening existing checks.
- [ ] `tests/e2e/helpers/responsive-accessibility.ts` (new), `tests/e2e/specs/responsive-accessibility.spec.ts` (new) — exercise the matrix with spacing, target exceptions, full focus/occlusion geometry, contrast and media assertions; test geometry failures with synthetic fixtures. Reuse existing journeys/artifacts.
- [x] `tests/e2e/specs/specimen-accessibility.spec.ts` — distinguish reflow from real zoom; remove overflow allowance; reuse visual/contrast proof.
- [x] `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor{,.css}`, `FcCommandPalette.razor.css`, `FcPaletteResultList.razor.css`, `FcPageToolbar.razor` in that directory — fix clipping and Fluent token ownership; preserve defaults.
- [x] `src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-projection.css`, `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs`, `RazorEmitter.cs` in that directory — support lossless text spacing and bounded grid scrolling with truthful row sizing; update affected generator approvals and focused density/rendering tests.
- [x] `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcExpandedRowHiddenBanner.razor{,.cs}`, `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`, `CommandFormEmitter.cs` in that directory, `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-focus.js` — implement decisions and focused regressions.
- [ ] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md` (new) — bind full candidate identity and sanitized references to every AE/surface; record manual journeys/outcomes, unresolved gates and opt-in UX review or authorized skip; disposition every Critical/High finding.

**Acceptance Criteria:**

- Given each changed surface, when AE-01–AE-09 journeys run, then the matrix passes, including 1.5× line height, 2× paragraph spacing, 0.12em letter spacing, 0.16em word spacing; 4.5:1 normal text, 3:1 large text/non-text/focus; and individually justified Inline/Spacing/Equivalent/User Agent Control/Essential target exceptions.
- Given automated and dated human evidence, when readiness is assessed, then each requirement cites candidate-valid proof; actual 400% zoom, heard speech (including direct cues and silent-restore/new-message sequences) and real-device outcomes are never inferred, and missing evidence prevents completion.

## Implementation Notes

### Completion follow-up, 2026-10-16

The current completion candidate `sha256:f8d9a2b2a0608d5ed0bc461f433734e153b2836fa1237658539cc638bb2c03cf` executes real generated zero-field outcomes, real generated-detail removal and 116 themed state rows per engine. The final three-spec nonvisual run is 361 passed / 0 failed / 2 skipped; sixteen Build-review findings are patched. Latest configured Shell remains failed at3680 passed / 1 failed on its separately attributed source; the preceding `16abc` run retains 12 visual failures and8 existing skips. Unsupported WebKit actual palette paint, Windows and all manual/owner gates remain open. [Current result and human checklist](13-6-accessibility-result.md) gives candidate-bound proof; no completion or release approval is inferred. Frozen decisions are unchanged.

### Historical implementation record, 2026-10-15

The user requested both recommendations be applied and asked for a human checklist with manual checks open. Implementation and executed focused lanes pass; Story 13.6 remains incomplete. Automated matrix expansion and the candidate/checklist are available, with full remaining surface/state/theme coverage, missing browser integrations, manual observations and owner gates explicitly open in `13-6-accessibility-result.md`. The unchecked tasks retain those limitations. No completion or release approval is inferred.

Evidence inspected: 201 focused Shell/integration tests, 1,313 serialized SourceTools tests, 28 new/helper Chromium cases and 22 existing specimen cases (six ordinary visual comparisons included). Earlier broad Shell ran 3,678 tests with three subsequently corrected pins and one environment skip; no settled full-suite pass is claimed. Candidate implementation hashes were checked independently and match. Isolated generator p95 is 333.403ms. Final real-grid spacing/ItemSize is 32→47; enlarged-user-text stress reaches 113, then Roomy plus override removal restores 56→Compact 32. The final 42 route/toolbar/announcement journeys and the real-banner fixture case also pass.

The review resolved 18 patch groups, including the Settings accordion boundary and final fixture isolation. BH12 was rejected as a low-severity unused exception path; no deferred-work entry was created. Spec and sprint remain in review, with manual checks and remaining matrix coverage open as requested.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence | Route |
|---|---|---|---|
| BH01 | medium | The detail cancellation handler uses the first composed-path HTMLElement and closest(), which cannot cross a Fluent shadow boundary; the reviewer reproduced cancellation on an in-detail activation. | patch G01 |
| BH02 | medium | observeRowMetrics initializes height to the density floor, suppressing its first floor-valued callback. Returning from a different density can leave the generated cached ItemSize above the actual CSS row height. | patch G02 |
| BH03 | medium | The cell measurement takes the largest immediate child, not the complete vertical extent. Two stacked 30px blocks reproduce a 60px content extent reported as 32px. | patch G03 |
| BH04 | medium | ShowValidationSummaryAsync forwards only form-level errors for hidden forms; allowlisted field-only validation lives in EditContext and is omitted from the visible outcome. | patch G04 |
| BH05 | medium | The normal returned Rejected branch dispatches RejectedAction but never forwards its safe copy, while the hidden lifecycle card remains invisible. The exception path does forward. | patch G05 |
| BH06 | medium | The warning callback uses a null-coalescing chain without a nonblank fallback; NotFound/RateLimited with empty or whitespace problem text can clear feedback and speech. | patch G06 |
| BH07 | medium | Fluent 5.0.0 AllowDismiss defaults to true (verified with package API documentation and dotnet-inspect). The new bar omits the parameter, permitting feedback to disappear before the agreed next-attempt lifetime. | patch G07 |
| BH08 | medium | applyTextSpacing visits only roots present at invocation. A subsequently rendered open shadow root receives none of the four overrides, invalidating spacing evidence after remount. | patch G08 |
| BH09 | medium | focusGeometry accepts a focused target with zero outline and no box shadow; no assertion requires any visible focus indication. The negative fixture reproduced an empty failures array. | patch G09 |
| BH10 | medium | focusGeometry excludes pointer-events:none boxes and relies on hit testing. An opaque visually covering overlay with that property is ignored, although it obscures the target. | patch G10 |
| BH11 | medium | verifyKeyboardCycle succeeds on the first repeated active element after any one stop; it does not prove the expected controls were reached or forward/reverse modal boundaries. | patch G11 |
| BH12 | low | The User Agent Control exception validates native tags but not CSS ownership; an authored 8px native input can therefore be accepted if a caller supplies that exception. Current real inventories use only Spacing, with no User Agent Control claims. Establishing CSS-origin provenance adds machinery for an unused path. | reject: unlikely current use; nontrivial provenance checks |
| EC01 | medium | Independently confirmed the blank NotFound/RateLimited outcome described in BH06; the null/whitespace chain leaves no local recovery message. | patch G06 |
| EC02 | medium | The hidden warning callback omits RetryAfter, although the existing ordinary warning presents it; a rate-limited zero-field action loses its recovery guidance. | patch G12 |
| EC03 | medium | The generated grid key includes measured row height, so spacing changes remove the focused grid subtree. The observer has no capture/recovery for that demonstrated remount. | patch G13 |
| EC04 | medium | The same no-indicator helper defect as BH09 was independently reproduced with outline:none and box-shadow:none. | patch G09 |
| VG01 | medium | Pre-verified gap: component tests mock replacement focus and the browser fixture invokes tracking manually. Removing the real banner trackDetailFocus registration is not detected by those tests. | patch G14 |
| VG02 | medium | Pre-verified gap: the viewport failure/retry test uses the 44px floor and never accepts measured metrics. Reverting the measured-height divisor is undetected; an 88px row changes the required failure/retry page. | patch G15 |
| VG03 | medium | Pre-verified gap: modal keyboard coverage can pass after a short repeated cycle without the expected settings controls or reverse boundary. Existing body-escape checks do not establish those claims. | patch G11 |
| VG04 | medium | Verified Other finding: the measured-height grid key remounts focused descendants, with no existing focus preservation around the row-metric update. | patch G13 |
| P01 | medium | PresentHiddenOutcomeAsync awaits an external callback then can claim speech after disposal; SubmitAsync also continues after the awaited clear callback. A callback can synchronously dispose its component before returning, so both continuations need the existing disposed guard. | patch G16 |
| P02 | high | The stronger Settings cycle reproduced a premature wrap at Theme: containDialogFocus omitted fluent-accordion-item, so an internal heading was treated as an unknown boundary and Preview, Reset and Done were skipped. Adding that existing host to the selector is the direct correction; forward/reverse inventory detects its removal. | patch G17 |
| P03 | medium | The settled reused-journey run failed because the new missing-detail fixture was nested inside the old announcement fixture, exposing two fc-surface-status nodes to its uniquely scoped assertion. Move the new fixture beside the old section within the existing query gate; each then owns its existing status boundary. | patch G18 |

All findings were classified individually before grouping. G01–G18 are direct corrections in existing implementation/test seams, add no production public surface, and cover the demonstrated states above. No intent change or spec amendment is needed. BH12 is rejected under the low-severity rule; no current result claims that unused exception.

## Verification

- `npm --prefix tests/e2e run typecheck`; focused Playwright specs against the running Debug host; retain existing accessibility/visual evidence.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug`; run affected classes through its DLL with xUnit v3 `-class`. Run SourceTools.Tests separately and required broader gates.
- `git diff --check`; manual matrix follows the existing log template and remains incomplete until human evidence exists.


### 2026-10-16 gate-reconciliation continuation

Final candidate `sha256:c14be5f5413337c1e5a55b7e49075804761c57015e810113b0f94accb9190732` retains both frozen intent blocks and the preceding forty source inputs. The scheduler test now observes both callback starts under its existing shared deadline. Accepted cleanup offloads only potentially blocking linked CTS disposal; immediate cancellation and all bounds remain intact. The subscription test retains its own close/restart task across rejoin. Final configured Debug Shell:3681 passed/0 failed/0 skipped. Focused scheduler88, EventStore68 and subscription41 all pass.

The rebuilt three-engine run executes381 cases:366 passed/13 failed/2 existing skips. Twelve failures are unchanged Firefox/WebKit image comparisons, causally traced to system-font resolution plus later approved layout changes; one additional Chromium dark Settings return failure remains unattributed. Six focused instrumented repetitions do not replace that canonical failure. All116 state rows per engine are retained, subject to WebKit's116 actual-palette omissions and the additional measurement-review limits. Every existing42 browser-run candidate input matches final bytes; final manifest adds only the subscription-test file.

[Current result](13-6-accessibility-result.md), [verification](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/verification.json), [input reuse](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/input-reuse.json) and [visual diagnosis](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/visual-diagnosis.md) retain exact outcomes. Windows, manual AT/zoom/devices, owner approvals and live-backend acceptance remain open. Status stays in-review; no completion, staging, commit or push is asserted.
