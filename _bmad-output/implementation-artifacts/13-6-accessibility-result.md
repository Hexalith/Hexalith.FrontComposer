# Story 13.6 accessibility result and human checklist

Updated 2026-10-17. **Readiness remains open; Story 13.6 stays in-review.** The current review patch is based on root `0618c1102d7858c14e3d5f5af13b88b06497ab37` on `main`. Its tracked code/test diff has `sha256:65b5464381deda2e0786e3f8aa3c83ebb7b5fa00c0125aad8e8a47091626b51f` (`git diff --binary 0618c1102d7858c14e3d5f5af13b88b06497ab37 -- src tests | sha256sum`). This identifies the review patch, not a full candidate manifest.

**Prior broad Story 13.6 browser gate (before the current review patch):** From `tests/e2e`, `npx playwright test specs/specimen-accessibility.spec.ts specs/responsive-accessibility.spec.ts specs/accessibility-state-matrix.spec.ts --workers=4 --retries=0` ran 732 cases across Chromium, Firefox and WebKit: **711 passed, 18 failed, 3 skipped, 0 flaky**. All 18 failures are the retained ordinary visual comparisons; there are **zero nonvisual failures**. [Prior broad verification summary](evidence/story-13-6/2026-10-17-review-continuation/verification.json) retains the failed-test inventory and SHA-256 of the raw Playwright report. The generated destructive-form validation journey also passed 3/3 across the same engines, proving that its description remains linked while Fluent displays the single validation error. DOM focus and status checks do not establish heard speech.

**Prior broad .NET gates and current build checks:** Debug Shell.Tests and SourceTools.Tests builds pass with zero warnings/errors after local restores against the declared catalog. The earlier full Shell run reported **3709 total, 7 failed, 1 skipped**; failures include existing catalog/governance and Fluent snapshot drift. The earlier full SourceTools run reported **1313 total, 1 failed**: its packaged analyzer expects Fluent 5.0.0 while the declared catalog resolves 5.0.1. Focused `CommandRendererWrapperIntegrationTests` pass 70/70 and `CommandFormEmitterTests` pass 68/68. The earlier affected hidden-banner, StatusOverview and virtualization tests and 6/6 synthetic browser fixtures passed. Current AppHost, Shell.Tests and SourceTools.Tests serialized Debug builds, TypeScript typecheck and `git diff --check` pass. No tracked dependency declaration changed.

**Current review checks:** Against the gated direct Test host, the full responsive accessibility spec passed **453/453** across Chromium, Firefox and WebKit. The accessibility state matrix passed **224**, with **one WebKit capability skip** and no failures. The new shadow-label, offscreen-target, clipped-cover and transformed-pseudo fixtures passed 12/12 across the three engines. The palette forced-colors journey passed 6/6; Chromium and Firefox assert disabled label and hint `GrayText`, while WebKit retains the actual palette-paint evidence limit. The reduced-motion CSS fixture passed 6/6 and does not establish a live reconciliation transition. [Current review verification](evidence/story-13-6/2026-10-17-review-continuation/review-2026-10-17-verification.json) records commands, counts, the retained state-matrix report digest and limitations. No current combined visual or full .NET test gate was run.

**Prior complete candidate:** Source `sha256:0bfddb4016c3bfe00e84ae88ca698b3a63c373b1ac2be5048577cdae0ee6b77a` at root `008736825376e6e2b419cb8d3b81067a8f9cc3b1` completed the earlier 726-case browser packet: 705 passed, 18 visual comparisons failed and 3 skipped. Its [source manifest](evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/source.json), [verification report](evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/report.md) and [browser outcomes](evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17/verification/browser-outcomes.json) are historical evidence; the current review patch has its own results above.

Manual NVDA/JAWS/VoiceOver, actual 400% zoom, tablet/phone and heard-speech observations, Windows execution, WebKit actual forced-palette evidence, owner approvals, visual approval, catalog-policy alignment, raw artifact-size gate and live EventStore fresh-row acceptance remain open. All 24 approved image bytes, four frozen intent blocks and eight root submodules remain unchanged. The original story baseline is `3c8679628ebdb8995afc09f712a04fcde623695c`.

## Human evidence checklist — leave each box open until observed

Use [the existing manual log template](../../docs/accessibility-verification/manual-log-template.md) for every run. Record full candidate identity, date, human tester, OS/version, browser/version, AT/version (or device/model/OS), route/flow, responsive tier, observed Pass/Fail, issue links for failures, reviewer/sign-off owner and approval reference, and sanitized attachment paths. Do not paste raw screen-reader transcripts or tenant/user values.

| Gate | Canonical status | Owner / release impact / reason / next action / reopen trigger |
| --- | --- | --- |
| MAN-136-NVDA | `not performed` | Accessibility/Stakeholder role; individual unassigned. Blocks story/release accessibility completion. No dated human NVDA/Firefox observation supplied. Assign a tester and run the checklist; reopen on a matching candidate with exact versions and sanitized evidence. |
| MAN-136-JAWS | `not performed` | Accessibility/Stakeholder role; individual unassigned. Blocks completion. No dated human JAWS/Chrome observation supplied. Assign a tester and run the checklist; same candidate/version/evidence trigger. |
| MAN-136-VO | `not performed` | Accessibility/Stakeholder role; individual unassigned. Blocks completion. No dated human VoiceOver/Safari observation supplied. Assign a tester and run the checklist; same candidate/version/evidence trigger. |
| MAN-136-ZOOM | `not performed` | Quality/Test role; individual unassigned. Blocks AE-02. Actual browser zoom has not been performed. Run at 400% from a measured 1280 CSS-pixel reference viewport; reopen with browser/version and real zoom evidence. |
| MAN-136-TABLET | `not performed` | Quality/Test role; individual unassigned. Blocks real-device gate. No actual tablet supplied. Exercise touch-adapted tier on a physical tablet; reopen with model, OS/browser, orientation, viewport and dated human results. |
| MAN-136-PHONE | `not performed` | Quality/Test role; individual unassigned. Blocks real-device gate. No actual phone supplied. Exercise functional fallback on a physical phone; reopen with the same device/version evidence. |
| MAN-136-ZERO-FIELD | `not performed` | Quality/Test and Accessibility/Stakeholder roles; individuals unassigned. Human AT/heard-speech observations remain open. The gated Test host now exposes the real generated renderer and its automated matrix has run. Execute the approved sequence with a supported AT/browser pairing; automation does not supply heard speech. |
| UX-136-REVIEW | `performed: scoped automated evidence review` | Both independent Build review passes and parent verification are retained in the prior complete-candidate report and current spec. Human UX/Product/Fluent sign-off remains open and is not inferred. |

All gate rows above share task/story `13.6` and release branch `main`; human observations must name the candidate actually tested. The review-patch digest above identifies code/test changes only. The manual gates retain **unassigned** individual testers/reviewer owners and no human approval reference. UX-136-REVIEW records the performed scoped automated review only. NVDA, JAWS, VoiceOver, tablet, phone and zero-field gates cover AE-01–AE-09 plus the manual-evidence acceptance criterion; the zoom gate specifically covers AE-02, and the performed scoped UX review covers the opt-in evidence-review criterion. The remaining rows are prepared checklist entries, not completed manual logs.

### Common operator journey on every supported AT/browser pairing

- [ ] Exercise shell skip links, hamburger/drawer/72px and 48px rails, account/settings/menu controls, Home cards and CTAs, navigation and route headings, tab selection, toolbar search/filter/view/actions, projection loading/empty/error/recovery/grid/detail, command form validation, lifecycle recovery, palette results, dialogs and the in-flow abandonment guard.
- [ ] At 320 CSS pixels and separately at **actual browser zoom 400% from 1280 CSS pixels**, repeat every operation and overlay. Confirm logical reading/focus order, no lost or clipped control, no page-level horizontal scrolling, and bounded labelled grid/tab scrolling with the entire focused target and ring visible.
- [ ] Apply all four spacing overrides together. Confirm complete labels, instructions, statuses, errors and controls; grid rows grow when needed without inaccurate virtual spacers. Preserve the ordinary compact 32px minimum and 75rem constrained measure.
- [ ] Inventory every pointer target including open overlays. For each target below 24×24, record exactly one permitted exception, dimensions/equivalent control, rationale, evidence and keyboard path. Validate spacing circles against both neighboring target rectangles and undersized-target circles.
- [ ] Traverse every keyboard stop with sticky chrome, messages, drawers, scrollports and active overlays present. Confirm the whole target and ring are visible, modal focus is contained, and each return/fallback destination remains unobscured.
- [ ] In light/dark and forced colors, inspect UX-VC-1 text, secondary instructions, current navigation/tab, focus, status/lifecycle, fresh cue, validation, unavailable and no-access states. Retain measured 4.5:1 normal text, 3:1 large text/non-text/focus evidence. Confirm text/icon/shape/current-state meaning survives authored-color suppression.
- [ ] With reduced motion enabled, verify immediate navigation, validation, lifecycle, reconnect and fresh-row cues; no essential information requires animation, hover or smooth scrolling.

### Approved missing-item sequence

- [ ] Expand a generated detail, focus a detail control, then remove the item through the completed data result without changing filters. Hear the neutral unavailable explanation once through its focused non-live text; verify no Clear Filter control and no AM-22 copy.
- [ ] Repeat with focus in a surviving control outside detail. Preserve that focus; hear one shared polite neutral explanation. Refresh the unchanged missing result repeatedly: no refocus or duplicate speech.
- [ ] Delay recovery, then navigate, change tenant/user scope, or move operator focus/input elsewhere. Confirm the old recovery never takes focus or reads stale content. Recheck a genuinely filter-hidden item separately: preserve its existing filter-only AM-22/Clear Filter behavior.

### Approved zero-field failure sequence — gated Test host now exposes the real renderer

- [ ] Trigger safe validation failure, permission/unavailable/rate-limit/transport failure and unmapped rejection. Include field-only server validation, a normally returned rejection, blank unavailable/rate-limit problem text, Retry-After guidance, admission-scope-unavailable and submit-time policy denial. Confirm existing safe outcome text appears in compact Fluent feedback beside the trigger, wrapping below at narrow width/zoom/spacing. Keep trigger focus and hear exactly one message; inspect shared status ownership, without counting DOM changes as heard speech.
- [ ] Wait and refresh: feedback remains, no retry or duplicate speech. Start a new attempt: prior feedback clears. Navigate/change scope: prior feedback is removed. No extra dialog opens.
- [ ] Accept a new command: use the existing pending-command summary and truthful lifecycle; do not infer confirmation from transport acceptance.

### Direct speech, silent restore and new-message sequence

- [ ] Hear direct route/denial headings, focused non-live validation/unavailable text, shared polite lifecycle/projection status, and palette-owned combobox result status. Each event uses its specified owner once; no competing live node reads the same event.
- [ ] Navigate away/back or remount/restore already-delivered state. The old announcement stays silent. Then cause a distinct valid result/command/connection/palette event: the new message is heard once.
- [ ] Observe silent polling/retry ticks, unchanged background refreshes, rejected/suppressed fresh cues, dismissal and expiry. Fresh New/Updated appearance speaks only for eligible material change. Use the preserved live-only fresh-row lane for EventStore proof; this candidate does not close it.

### Physical tablet and phone

- [ ] On a tablet, use touch-adapted navigation, target activation, fields, toolbar/popovers, grid/detail, palette/dialogs, lifecycle/recovery and rotation. Confirm visible controls, correct return focus, readable status and system-color/motion preferences.
- [ ] On a phone, establish functional fallback for the same essential operations, including row actions exposed through detail and bounded two-dimensional scrolling. Record constraints honestly; do not claim full daily-use phone support.

## Remaining acceptance gates

All 18 manual boxes above stay open. NVDA/JAWS/VoiceOver, actual browser zoom, tablet/phone, heard speech and human reviewer observations were not performed. OI-16, G-4, FLUENT-APP-1, Product approval/reapproval and separate live EventStore fresh-row acceptance remain unresolved. The prior broad Shell and browser gates remain failed on catalog/governance, Fluent snapshots and 18 visual comparisons; those broad gates were not rerun for this review patch. Raw artifact-size gates, Windows execution and WebKit actual forced-palette coverage remain open. Every prior failed or interrupted attempt retains its own evidence and disposition.

```yaml
story: 13.6
story_status: in-review
sprint_status: review
final_classification: open
prior_configured_shell: failed; 3709 total / 7 failed / 1 skipped
prior_source_tools: failed; 1313 total / 1 failed (Fluent 5.0.0 expectation versus 5.0.1 catalog)
prior_complete_browser: failed; 711 passed / 18 visual failed / 3 skipped / 0 flaky / 732 total
prior_visual_comparisons: failed; 18
prior_nonvisual_browser_failures: 0
current_responsive_browser: passed; 453/453
current_state_matrix_browser: passed; 224 passed / 1 skipped / 0 failed
prior_candidate_state_rows_per_engine: chromium=116, firefox=116, webkit=116
prior_candidate_focused_type_header_fresh_row: passed; 18/18 across three engines
raw_artifact_size_gate: prior candidate failed; current run not evaluated
webkit_actual_forced_palette_cells: uncovered; 116
windows_execution: not performed
manual_at: not performed
actual_400_percent_zoom: not performed
real_tablet: not performed
real_phone: not performed
heard_speech: not observed
zero_field_generated_browser_surface: executed in current Story 13.6 browser gate
ux_evidence_review: scoped automated reviews performed; human sign-off open
product_approval_inferred: false
```
