# Story 13.6 accessibility result and human checklist

Prepared 2026-10-15. **Readiness: blocked; manual checks remain open.** The user explicitly requested preparation of this checklist while keeping manual checks open. No human AT, actual browser-zoom, real-device, Product, or Fluent approval is asserted here.

## Candidate and evidence identity

- Repository: Hexalith/Hexalith.FrontComposer; branch: `main`.
- Full baseline commit: `3c8679628ebdb8995afc09f712a04fcde623695c`.
- Content-addressed implementation candidate: `sha256:789ad2a214a1c2a213713f63d062e74fad16e7063cc9629345513fd2c8978d17` (66 implementation/test files). `evidence/story-13-6/candidate.json` records the SHA-256 of each changed implementation/test file and a digest of their sorted manifest. The implementation input documents are hash-bound; later workflow/triage edits are recorded separately, with the approved intent unchanged. Any subsequent implementation edit invalidates affected results until revalidation.
- Runtime: Linux, .NET SDK `10.0.401`, Debug project references; Node `v26.4.0`, Playwright `1.63.0`, axe-core `4.13.0`. Chromium `153.0.8010.12` is the executed browser; Firefox/WebKit and Windows runners are not performed. Automation does not identify a human tester.
- Evidence uses relative paths and bounded selectors/geometry/copy classifications. No credentials, real tenant/user values, command payloads, raw backend metadata, or full DOM dumps are included in this result or its retained evidence files. Transient development traces/reports are not release approval evidence.

## Implemented behavior

The missing-detail banner keeps the neutral unavailable-item copy. It tracks detail focus before removal and consumes that capture only if replacement actually loses focus. Recovery focuses the non-live explanation; otherwise the shared polite owner announces the neutral explanation once. Repeated missing-result refreshes are silent. Scope changes, disposal, navigation, and subsequent operator focus/input cancel stale recovery. Filter-hidden detail continues to use its existing Clear Filter/AM-22 path.

A hidden zero-field form forwards its existing safe failure outcome to its visible renderer. A compact Fluent message appears beside the trigger and wraps below it. The trigger keeps focus, the shared status remains the sole speech owner, and the message persists until the next attempt or the containing navigation/scope lifetime ends. Transport acceptance continues through the existing pending-command summary. There is no automatic retry or extra dialog.

Both axe assertions now share the original WCAG A/AA and 2.1 tags plus `wcag22aa`. New geometry assertions cover page overflow, all four spacing overrides including Fluent shadow roots, complete target inventories and recorded exceptions, whole-target/ring clipping and occlusion, contrast composition, keyboard cycles, forced colors, and reduced motion. Synthetic negative fixtures exercise clipped rings, off-center occlusion, invalid spacing circles/equivalent targets, and missing exception evidence.

The shell/header, toolbar, palette, tooltip invokers, and generated grid scrolling retain their Fluent-owned styling while allowing reflow. Desktop header geometry is explicitly 48px with a normal right-aligned action row in both themes; narrow content can wrap without collapsing the controls. Grids own labelled bounded scrolling. Uniform rendered row measurements feed the virtualizer's ItemSize and remount key together; the same value drives viewport-page calculations. Density metrics remain 32/44/56px minima and typography mapping version remains 3.1.0. New focus recovery does not treat disappearance as filter evidence.

## Automated verification inventory

The command/result inventory and sanitized browser findings below are finalized with the candidate manifest. `not performed` entries remain blocking; no successful fixture is promoted to a human or full-matrix pass.

| Lane | Result / evidence |
| --- | --- |
| TypeScript | `npm --prefix tests/e2e run typecheck` — passed. |
| Debug Shell build | `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1 --no-restore -v:q` — passed. |
| Focused approved-behavior regressions | Direct xUnit v3 DLL run of the approved behaviors, grid/shell integration pins, density lint and semantic-token binding — **201 passed** on the settled code. Exact class filters are in `verification.json`. |
| Affected integration pins | Included in the final 201-test run above. Both new zero-field admission/policy cases and the delayed-import disposal race pass. |
| SourceTools | `dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -noLogo -parallelMode none` — **1,313 passed**. Cache-miss median 29.653ms, p95 333.403ms. A prior concurrent run failed its 1,000ms p95 budget at 1,074.469ms; the serialized full run passed. Intentional generator approvals include the Fluent Small disclosure and truthful row metric. |
| Full Shell | 3,678 tests: 3,674 passed, three stale snapshot/token pins failed, one live two-tenant test skipped for absent endpoints/identities. The three pins were updated for the intended Fluent Small disclosure and active brand alias; all pass in the final 201-test run. The full suite was not repeated after these focused fixes. |
| Final browser + existing specimen lane | **50 passed**, including 28 new/helper cases, 15 existing nonvisual specimen cases, six visual comparisons without update, and one production-exposure case. Exact commands in `verification.json`; reports `tests/e2e/playwright-report/story-13-6-matrix` and `tests/e2e/playwright-report/story-13-6-final`, matching artifacts under `tests/e2e/test-results/`. The production-exposure subcase uses the existing Release binary; it is not current Release/package or V5 posture acceptance. |
| Reused journeys | **42 passed** on the final fixture boundaries, including silent restore/new-message, route focus, toolbar and overlay-return journeys. Report `tests/e2e/playwright-report/story-13-6-journeys`. |
| Existing specimen/visual lane | Included as 22 independently named cases in the final 50. The intentional six-image update was followed by six passing ordinary comparisons. Windows, Firefox and WebKit execution remain not performed; QA Engineer owns the corresponding CI follow-up and platform artifacts. |
| Visual governance | `env -u GITHUB_BASE_REF npm --prefix tests/e2e run validate:visual-governance` — passed for all six changed snapshots; no remote operation. |
| Whitespace | `git diff --check` — passed. |

The final fixture-only boundary correction reuses unaffected .NET and default-specimen evidence. The 42 relevant journeys and the real-banner removal case were rerun after that correction; the latter is retained under `story-13-6-fixture-boundary`.

Initial Aspire startup hit parallel build file contention on Shell.dll. `aspire stop` reported no running AppHost. The serialized Debug AppHost build passed; the next Aspire start and `aspire wait counter-web` succeeded. Counter was healthy; dependency-backed resources remained waiting behind an unhealthy security resource. Aspire was then stopped to release the development lifecycle. The browser lane runs the rebuilt Debug Counter host with its existing Test/specimen configuration. None of this is live EventStore/fresh-row acceptance proof.

### AE and changed-surface map

| Requirement | Candidate evidence and practical limit |
| --- | --- |
| AE-01 reflow | New 320 CSS-pixel routes cover Home, module navigation/tabs, type/lifecycle/form/detail, formatting/grid, toolbar and active palette/settings. Existing specimen reflow names now explicitly describe viewport resizing and allow only a 1px rounding tolerance for page overflow. |
| AE-02 actual 400% browser zoom | **Not performed.** A 320px viewport is reflow evidence only. Complete the real zoom checklist on a 1280 CSS-pixel reference viewport. |
| AE-03 spacing | New helper applies line-height 1.5, paragraph margin 2em, letter spacing .12em, and word spacing .16em together, including open shadow roots; real-surface results and unresolved failures remain recorded below. |
| AE-04 targets | Real-surface pointer inventories include controls and active modal content. Each undersized target requires one evidenced Inline/Spacing/Equivalent/User Agent Control/Essential exception; no general exemption was added. Synthetic fixtures reject invalid circle and equivalent claims. |
| AE-05 focus | Keyboard-cycle, whole rectangle/ring, scroll-ancestor, and off-center app-occlusion checks; real-banner removal and lost-detail focus recovery/cancellation browser fixtures. This does not establish assistive-technology speech or real-device visibility. |
| AE-06 contrast | Existing specimen contrast and visual lane reused; new nonoptional computed text/alpha samples from the opened palette in both active shell themes (heading, label, hint, current-context, selected/hover and no-results pairs). Complete all UX-VC-1 foreground/background and meaningful non-text/focus pairs on each remaining surface before claiming matrix-wide completion. |
| AE-07 forced colors | Chromium media-state assertions and type/lifecycle snapshots; existing fresh-row state/component evidence retained. Remaining full state families and supported AT/device journeys stay open. |
| AE-08 reduced motion | Static transition/animation/media assertions and retained lifecycle/fresh-row component/journey coverage. No heard-speech or live fresh-row inference. |
| AE-09 semantics/keyboard/speech | WCAG 2.2 default scanner fixture, scoped axe artifacts and reused navigation/toolbar/announcement journeys. DOM mutation counts are automation evidence only. Heard speech requires the human runs below. |

The production Counter/specimen browser host currently exposes no generated zero-field inline command surface. Its failure UI is covered by the passing component regressions `TransportFailureRemainsBesideTriggerAndSpeaksOnceWithoutRetry`, `NextAttemptClearsFailureWhileAcceptedCommandUsesExistingSummary`, `ScopeUnavailableIsVisibleWithoutDispatchOrHiddenFocusAndEachAttemptSpeaksOnce`, and `SubmitTimePolicyDenialUsesVisibleOwnerWithoutDispatchOrHiddenHeadingFocus`. `NextAttemptClearsFailureWhileAcceptedCommandUsesExistingSummary` verifies clearing while the next dispatch is pending; accepted pending-summary behavior is supplied by the existing lifecycle lanes. **Zero-field browser failure/focus/geometry coverage remains not performed**, including each non-accepted outcome family. It must be run on an adopter/test host that actually exposes that generated shape; no extra sample workflow was introduced to imply coverage.

Executed undersized-target exception records use **Spacing**. The helper does not establish CSS-origin ownership for a User Agent Control exception; no executed inventory claims that exception. A future native-control exception needs separate evidence that its size is controlled by the browser rather than authored CSS.

### Surface evidence and remaining coverage

| Surface | Automated evidence actually exercised | Still open before acceptance |
| --- | --- | --- |
| Shell, Home, navigation and tabs | 320px reflow/spacing, pointer inventory, executed Tab cycles and WCAG 2.2 scans; route-contract journeys reused | Full state/theme/non-text contrast matrix, supported AT, actual zoom and physical devices |
| Toolbar/search/filter/actions | Existing toolbar journeys and manifest route; narrow reflow/spacing, targets, focus cycle and scanner | All active filter/popover states on each AT/device pairing |
| Projection/grid/detail | Existing generated/component pins, actual generated default-body grid, bounded scrolling/row metrics, narrow focus cycles and scanner | Live fresh-row backend acceptance; every state/theme contrast pair; missing-detail recovery on the real generated DOM and supported AT |
| Command form and lifecycle | Specimen form/lifecycle/denial/destructive affordances, existing journey/component pins, narrow scanner/focus/spacing | Every generated density/validation/recovery state across AT/zoom/devices; human direct-cue speech |
| Palette and settings dialogs | Active narrow overlays with spacing, pointer inventories, contained Tab cycles; reused route/announcement journeys | All other dialog/menu states and supported AT/device pairings |
| Missing-item decision | Component focus/speech ownership and cancellation; real banner/Fluent-action removal browser case, plus focus-module recovery and cancellation fixtures | Generated-detail browser surface integration and heard neutral explanation once |
| Zero-field decision | Component regressions covering validation, returned rejection, warning fallback/retry guidance, transport, new attempt, admission scope denial, submit-time policy denial and disposal | Browser host exposing this generated shape, full failure family geometry/focus and heard speech |

The generated default-body grid seam is confined to `generatedGrid=true` on the existing gated type specimen; ordinary specimen routes keep their existing template. Actual compact rows and ItemSize are 32px. With the bounded diagnostic link allowed to wrap, the four canonical spacing overrides grow the real row and ItemSize together to 47px; a separately labelled additional 24px user-text-size stress grows both to 113px. The final regression then switches to Roomy while overrides remain active, removes the overrides to recover 56px, and returns to Compact 32px. It also proves focus recovery after an actual control remount and preserves later command-input focus. This extra stress is separate from actual browser zoom; row growth reflects this fixture’s content, rather than a universal fixed spacing height.

Visual changes are intentional: 24px tooltip/link targets, lossless wrapping, active Fluent brand-role aliasing, and compact grid-only unsupported-field spacing/disclosure sizing alter the six Linux Chromium baselines. Original Light/Dark × Compact/Comfortable/Roomy images are retained under [visual-before](evidence/story-13-6/visual-before/); corresponding after images are the six files under [the existing snapshot directory](../../tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/). No visual regeneration is stakeholder approval. Windows baselines and Firefox/WebKit execution remain not performed on their owning runners.

Computed palette evidence is retained in [palette-contrast.json](evidence/story-13-6/palette-contrast.json): all seven samples per active theme exceed 4.5:1 (lowest Light 5.934:1; Dark 6.483:1). The final row stress, complete target inventories and 92 named browser outcomes are retained beside it. These samples do not close the broader unchanged contrast/state matrix.

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
| MAN-136-ZERO-FIELD | `not performed` | Quality/Test and Accessibility/Stakeholder roles; individuals unassigned. Blocks zero-field browser/AT matrix. Existing browser host lacks this shape. Supply a host exposing the generated inline zero-field renderer and run the approved-fix sequence below. |
| UX-136-REVIEW | `not performed` | UX review owner unassigned. No opt-in UX review or authorized skip recorded. Obtain an explicit review or authorized skip and attach its dated reference; elapsed time is not approval. |

All gate rows above share task/story `13.6`, release branch `main`, and the immutable candidate digest in the identity section. Their reviewer/sign-off owner and individual tester are **unassigned**; no approval reference exists. NVDA, JAWS, VoiceOver, tablet, phone and zero-field gates cover AE-01–AE-09 plus the manual-evidence acceptance criterion; the zoom gate specifically covers AE-02, and UX review covers the opt-in-review acceptance criterion. These are prepared checklist entries, not completed manual logs.

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

### Approved zero-field failure sequence — requires a host exposing that renderer

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

## Findings, remaining gates and final classification

The executed final automated cases have no unresolved Critical/High finding; historical failures and their passing focused dispositions, coverage limits, and final command counts are listed in `evidence/story-13-6/verification.json`. All 18 review patch groups are resolved, including the Settings accordion boundary; no new deferred-work ledger entry was created. BH12 remains a rejected low-severity unused helper path, with its limitation recorded above. A passing helper fixture does not close an uncovered surface/state. Any remaining matrix failure is a blocker, not an axe suppression or accepted constraint.

OI-16, G-4, FLUENT-APP-1, Product approval/re-approval, the separate fresh-row live acceptance, and the exact Fluent V5 posture remain unresolved by this story. Missing manual observations, individual owners, reviewer approvals and UX-review disposition prevent completion. No accepted constraint, roadmap approval, stakeholder sign-off, or release promotion is inferred.

```yaml
story: 13.6
final_classification: blocked
manual_at: not performed
actual_400_percent_zoom: not performed
real_tablet: not performed
real_phone: not performed
zero_field_browser_surface: not performed
ux_review_or_authorized_skip: not performed
product_approval_inferred: false
```
