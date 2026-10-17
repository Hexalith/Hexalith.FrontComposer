# Story 13.6 accessibility result and human checklist

Updated 2026-10-17. **Readiness remains open.** Current complete browser verification: **588 passed, 12 failed, 0 flaky, 3 skipped / 603**. The configured direct Debug Shell lane is **3693 passed / 2 failed / 3695**. All four cleanup scenarios pass in the rebuilt 69-test client class and configured lane. The original Firefox correction and review geometry regressions have current proof; five dark-shell repeats plus five clipping fixtures pass without retries. Every executed nonvisual case passes on its first attempt. Visual, dependency-policy, platform/manual and owner gates remain explicit.

Current source: **`sha256:e23d846e6e8f87f26f997866a8e82aa355e8ae89f1f94b67d4fa793add1db7b0`**, root `489f3ad4cbf09965c675d7bf2007c6a8b7a0861e` on `main`. [Current manifest](evidence/story-13-6/2026-10-16-gap-closure/stabilize-2026-10-17/parent-verification/configured-host/source.json), [parent verification and review dispositions](evidence/story-13-6/2026-10-16-gap-closure/stabilize-2026-10-17/parent-verification/configured-host/report.md), [loaded binaries and actual Test environment](evidence/story-13-6/2026-10-16-gap-closure/stabilize-2026-10-17/parent-verification/configured-host/startup-binaries.json), [browser outcomes](evidence/story-13-6/2026-10-16-gap-closure/stabilize-2026-10-17/parent-verification/configured-host/browser-outcomes.json), [per-attempt state validation](evidence/story-13-6/2026-10-16-gap-closure/stabilize-2026-10-17/parent-verification/configured-host/state-validation.json) and [final audit](evidence/story-13-6/2026-10-16-gap-closure/stabilize-2026-10-17/parent-verification/configured-host/audit.json) bind this candidate. All 24 approved image bytes and four frozen intent blocks remain unchanged. The [preceding implementation packet](evidence/story-13-6/2026-10-16-gap-closure/stabilize-2026-10-17/report.md), earlier 2026-10-17 and 2026-10-16 packets remain historical. Original failures, interrupted runs and the parent launch with an omitted Test notice setting remain separate. The final launch supplies the existing Playwright 30000 ms notice setting; all six reconnect cases pass without retries. No production default changed.

**Dependency limit:** Debug checks used existing Fluent 5.0.0 restore assets. The catalog now requests 5.0.1 and its Localization pin also differs from the policy expectation. No clean-restore compatibility or dependency upgrade is claimed. Failed and interrupted browser attempts are retained separately; passing diagnostics do not replace failed complete gates. Missing human evidence prevents story completion.

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
| UX-136-REVIEW | `performed: scoped automated evidence review` | Previously performed scoped automated reviews remain retained in the historical packets. Current parent Build review verified thirteen findings and ten patch groups; human UX/Product/Fluent sign-off remains separate and is not inferred. |

All gate rows above share task/story `13.6`, release branch `main`, and the immutable candidate digest in the identity section. The manual gates retain **unassigned** individual testers/reviewer owners and no human approval reference. UX-136-REVIEW records the performed scoped automated review only. NVDA, JAWS, VoiceOver, tablet, phone and zero-field gates cover AE-01–AE-09 plus the manual-evidence acceptance criterion; the zoom gate specifically covers AE-02, and the performed scoped UX review covers the opt-in evidence-review criterion. The remaining rows are prepared checklist entries, not completed manual logs.

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

All manual boxes above stay open. NVDA/JAWS/VoiceOver, actual browser zoom, tablet/phone, heard speech and human reviewer observations were not performed. OI-16, G-4, FLUENT-APP-1, Product approval/reapproval and separate live EventStore fresh-row acceptance remain unresolved. The configured Shell gate fails at 3693 passed / 2 failed; visual comparisons, Windows execution, WebKit actual forced-palette coverage and manual/owner gates remain open. The retry-cleanup scheduling assertion and Firefox clipping defect now have current passing proof. Earlier Settings/cancellation/contrast failures and prior review dispositions remain historical; no passing diagnostic erases them or closes an uncovered platform or surface cell.

```yaml
story: 13.6
final_classification: open
configured_shell: failed; 3693 passed / 2 failed
visual_comparisons: failed; 12
additional_browser_focus: current configured complete run has 0 nonvisual failures or flaky cases; earlier failed/interrupted attempts retained
firefox_dark_shell: causal clipping regression and five zero-retry repeats passed
accepted_retry_cleanup: four scenarios passed in rebuilt class and configured lane
open_evidence_hardening_groups: H01-H11 prior review retained; effective AppHost configuration harness deferred
webkit_actual_forced_palette_cells: uncovered; 116
windows_execution: not performed
manual_at: not performed
actual_400_percent_zoom: not performed
real_tablet: not performed
real_phone: not performed
zero_field_generated_browser_surface: executed
ux_evidence_review: historical scoped automated reviews retained; human sign-off open
product_approval_inferred: false
```
