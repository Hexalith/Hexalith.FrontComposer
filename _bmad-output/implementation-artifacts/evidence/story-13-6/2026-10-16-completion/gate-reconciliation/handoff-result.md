# Story 13.6 accessibility result and human checklist

Prepared 2026-10-16. **Readiness remains open.** Automated coverage and review fixes are implemented. One configured Shell failure, twelve current visual comparison failures, Windows execution and manual/owner observations prevent Story 13.6 completion. UX evidence review supplies no human approval.

## Candidate and evidence identity

- Branch `main`; full source HEAD `b9e13697cbcd98a0ad0e8003545b779a7a6ce8dc` plus the inventoried working-tree changes. Baseline: `c3708690f8e1dbebfb49682c67a8aba88fd4a092`.
- Current candidate **`sha256:f8d9a2b2a0608d5ed0bc461f433734e153b2836fa1237658539cc638bb2c03cf`**, 40 implementation/test/CI/evidence-policy files. [Manifest](evidence/story-13-6/2026-10-16-completion/build-review/candidate.json), [raw-byte rows](evidence/story-13-6/2026-10-16-completion/build-review/candidate.sha256) and [independent parent audit](evidence/story-13-6/2026-10-16-completion/build-review/parent-audit.json) bind source and both unchanged frozen intent blocks. Documentation/evidence are excluded from the aggregate to avoid circular hashes. The dated packet has Git byte-preservation attributes. No staging, commit, push, dependency upgrade or visual-baseline update was performed.
- Linux, .NET SDK 10.0.401, Debug/project references, Node 26.4.0, Playwright 1.63.0, axe 4.13.0; Chromium 153.0.8010.12, Firefox 155.0, WebKit 26.6. [Capability probes](evidence/story-13-6/2026-10-16-completion/browser-capabilities.json) distinguish actual system-color replacement from WebKit media-only emulation.
- [Pre-review verification](evidence/story-13-6/2026-10-16-completion/verification.json) retains candidate `16abc`'s **532 passed / 12 failed / 8 skipped of 552** across ten specs. Its twelve image failures and existing project skips remain explicit. The [earlier result](evidence/story-13-6/2026-10-16-coverage/historical-result.md), failed proof and both interrupted parent runs are preserved with their own identities; their outcomes are not counted as current passes.

## Implemented behavior and coverage

Existing opt-in Test/specimen gates expose the real generated zero-field renderer and completed projection-result changes. Eleven failure families cover validation, returned/exception rejection, forbidden, unavailable/blank-unavailable, rate limiting/blank rate limiting, transport, admission and policy denial. Tests assert safe visible feedback, retained trigger focus, one automation-observed owner, no pre-accept retry and next-attempt clearing. Acceptance uses the existing pending summary. Ordinary production routes reject the scenario gate.

Real generated detail removal verifies neutral non-live recovery, preserved external focus and repeated-result silence. The delayed test holds an already-entered recovery after actual detail focus capture/removal, then releases it after operator movement or real navigation. Filter-hidden detail retains its separate Clear Filter behavior. DOM assertions supply no heard-speech observation.

The matrix retains 116 light/dark state rows per engine: 320×900 with all four spacing overrides, plus 1600×900 desktop rails. Composed-tree text/icons/boundaries, focus/current cues, target exceptions, clipping, keyboard cycles and post-switch system paint are measured. Validators require a complete matrix for each engine/run, adjacent snapshot ownership, truthful target dimensions/exceptions and forced-mode focus geometry.

The three-layer Build review recorded 16 Medium evidence defects, grouped into 13 direct patches. Negative fixtures now reject permanent/transparent/nondrawing focus paint, clipped or obscured indicators, unsupported masks, low-alpha icons, inward low-contrast outlines, unrelated contrast masking, mixed-engine/run inventories and incomplete forced-mode proof. The negative validator suite runs in the existing CI lane. Supported rectangular clips and Fluent underlines remain measurable; unsupported geometry fails closed. Sole blurred indicators, unsupported pseudo geometry/transforms and closed-root interiors need additional pixel/platform evidence; exact blur boundaries and inaccessible interiors are not claimed. A bounded passive dropdown indicator is recognized as owned decoration; overlapping SVG rectangles and real sibling/stacking/open-shadow obstructions remain rejected.

Actual-browser fixes preserve Fluent ownership and existing defaults: menu roles, settings radio paint, scrollport focus, system glyph paint and palette selection contour. Persistent modal-session tracking preserves operator movement. The pinned Fluent dialog's second 25ms return is disabled only for palette/settings, whose return the shell already owns; a deterministic regression holds that actual callback until after real Tab reaches Settings. Narrow specimen navigation gets 8px focus-ring clearance. Shared grid regions and diagnostic links use a 2px inset contour with existing focus/CanvasText tokens; the specimen adapts only color to its local theme at every width. Desktop content, typography 3.1.0, 32/44/56 densities, 72/48 rails, 48px header and 75rem measure remain preserved.

## Executed verification and failed gates

[Current verification](evidence/story-13-6/2026-10-16-completion/build-review/verification.json), [browser JUnit](evidence/story-13-6/2026-10-16-completion/build-review/browser-junit.xml), [browser log](evidence/story-13-6/2026-10-16-completion/build-review/browser.log) and [state validation](evidence/story-13-6/2026-10-16-completion/build-review/state-validation.json) retain exact commands and ownership.

| Lane | Result and attribution |
| --- | --- |
| Current three accessibility specs, nonvisual | **361 passed, 0 failed, 2 skipped / 363**, 344.043s. chromium: 121 passed, 0 failed, 0 skipped; firefox: 120 passed, 0 failed, 1 skipped; webkit: 120 passed, 0 failed, 1 skipped. Visual-baseline cases ran separately below. |
| Debug builds | Counter.Web and Shell.Tests passed, 0 warnings/errors. Counter.Web was rebuilt after the narrow CSS changes; final log `build-review/web-build-shared-final.log`. |
| TypeScript / validator negatives / CI governance | Typecheck passed on current source; 4 Node negative tests and 10 CI-governance tests passed. Their unchanged inputs and exact commands are retained. |
| Latest configured full Shell | **3680 passed, 1 failed, 0 skipped / 3681**, 55.313s on `sha256:a42bb176228cf40abd70106bf30b4bb499f6a2834ef292b5a2adb909cc282b23`. Five configured exclusions retained. This failed full gate predates the subsequent shared grid/specimen focus CSS and helper/evidence-policy delta. |
| Current affected Shell | **163/163 passed** after the shared grid CSS change. The separately attributed full run also contained these classes; the focused result does not clear its scheduler failure. |
| Scheduler serial diagnostic | **88/88 passed**, 1.900s. It does not convert the failed full lane into a pass. |
| Focused fixes | 12/12 vendor-return/live journeys and 27/27 ownership, full forward/reverse type cycles and light/dark normal/forced focus cases plus 24/24 shared-grid route/theme checks passed. Current broader run supersedes them for the changed browser inputs. |
| Artifact/state gates | Strict curated Chromium inventory validation passed. All 116 Chromium/Firefox rows validate with 0 errors. WebKit strict validation reports 448 errors; explicit capability-limited validation has 0 errors and leaves 116 actual forced-palette cells uncovered. |
| Visual governance | Passed; no committed baseline images changed. The separate current visual run is 6 passed / 12 failed; all comparison images are retained below. |

The full Shell failure is `ProjectionFallbackRefreshSchedulerTests.ReadyReplayBehindNewlyStalledReplayStillStartsWithinBudget`: observed one read instead of two. Its unchanged class passes serially. The earlier two-failure configured result remains in the preceding verification; this latest run passes the earlier subscription-service case. A current full configured pass is not claimed.

Current skips are the existing Chromium-serialized Story 11.5 computed-style/reduced-motion case in Firefox/WebKit. The preceding ten-spec run also records six other non-Chromium skips (one generated-editor accessibility-tree case and two dedicated rejection-host cases per engine). All eight platform omissions remain explicit; a skip is not a pass.

The retained first instrumented palette probe also had one pre-close empty selected-cue inventory failure whose cause remains unattributed. Its assertion was preserved; the current canonical run did not reproduce it. [Diagnostic handoff](evidence/story-13-6/2026-10-16-completion/failed-proof/palette-settings-order/handoff.md) retains that failure separately from the conclusively traced and fixed vendor return. The preceding completed parent run retained 353 passed / 8 failed / 2 skipped; its eight grid/link focus failures are corrected by the current run. Interrupted parent runs remain failed/incomplete proof, never promoted to passes.

Current ordinary visual comparison run: **6 passed, 12 failed / 18**, on the final candidate. Firefox and WebKit comparisons remain failed checks. [All expected/actual/diff images and hashes](evidence/story-13-6/2026-10-16-completion/build-review/visual-comparisons/inventory.json) are retained. Representative [Firefox expected](evidence/story-13-6/2026-10-16-completion/build-review/visual-comparisons/firefox/frontcomposer-type-light-compact-expected.png), [actual](evidence/story-13-6/2026-10-16-completion/build-review/visual-comparisons/firefox/frontcomposer-type-light-compact-actual.png), [diff](evidence/story-13-6/2026-10-16-completion/build-review/visual-comparisons/firefox/frontcomposer-type-light-compact-diff.png), and [WebKit dark compact diff](evidence/story-13-6/2026-10-16-completion/build-review/visual-comparisons/webkit/frontcomposer-type-dark-compact-diff.png) show the same specimen sections but broad font/measure/layout differences, including wraps, controls and compact grid row height. Current Firefox expected/actual/diff and WebKit dark-compact diff were visually inspected; parent inspection of the separately retained pre-UX images independently confirmed that classification; they exceed a tiny raster/antialiasing delta. They are not external execution blockers and are not passing checks. Baselines were preserved for hosted reconciliation.

QA Engineer and the visual baseline owner must run, from `tests/e2e`, `npx playwright test specs/specimen-accessibility.spec.ts --project=firefox --project=webkit --grep "visual baseline" --workers 4` on the existing hosted environment, inspect every comparison and reconcile the cause before any authorized baseline change. QA Engineer owns the existing Windows accessibility/visual job: its Release build, `npm run test:a11y` and `npm run validate:a11y-artifacts`, with retained artifacts. Windows was not executed on this Linux host.

## Surface, state and AE mapping

[The complete per-engine state ledger](evidence/story-13-6/2026-10-16-completion/build-review/state-ledger.json) enumerates every row and artifact, viewport, sample counts and precise forced-color status. Raw state JSON and actual system-state images are under `build-review/browser/<engine>/`.

| Surface/state family | Automated evidence | Remaining limit |
| --- | --- | --- |
| Shell/Home/nav/tabs | Shell chrome, theme menu, exact desktop rails, Home/navigation state rows; real drawer Tab/Shift+Tab cycles, spacing, targets, scanner and current/focus cues | Supported AT, actual zoom, physical devices |
| Toolbar/search/filter/view | Toolbar, filter-popover, view-menu and active tabs in both themes; real search focus/keyboard and existing toolbar journeys | Human speech/device observations |
| Projection/grid/detail | Two/loading/empty/error, reconnect/disconnect/connected, stale, slow query, result cap, filter-empty/hidden, fresh presentation, real missing-detail loss/external focus | Separate live backend fresh-row acceptance and human AT |
| Generated forms/lifecycle | Generated forms, validation, policy denial; all eleven lifecycle states and their icons/cues | Human validation/recovery speech and device use |
| Zero-field | All eleven real failure families plus accepted pending summary, both themes, focus/ownership/no retry/clear/policy gates | Heard safe message once and physical-device operation |
| Palette/settings/dialogs | Default/selected/empty palette, radio focus/settings, destructive and abandonment states, theme/view menus, actual modal return sequencing | Supported AT/system-platform pairing |
| AE-01/03/04/05 | 320 CSS-pixel reflow, simultaneous spacing, target inventories/exceptions, whole sharp indicator clipping/occlusion and actual forward/reverse Tab | AE-02 actual 400% zoom remains unperformed; unsupported geometry fails closed |
| AE-06/07/08/09 | Nonempty text/icon/boundary/state cue samples; post-switch system snapshots, immediate reduced-motion cues, WCAG 2.2 axe and ownership/keyboard journeys | All 116 Linux WebKit actual forced-palette cells remain uncovered: its engine exposes media rules without authored-color replacement. Actual AT speech is unobserved. |

The only executed undersized-target exception is evidenced Spacing; no unmeasured User Agent Control or blanket exception is inferred. Decorative redundant empty-state illustrations and loading skeletons are explicitly identified in each state artifact. Browser fixtures establish presentation/DOM behavior only.

## UX evidence review and readiness

[Scoped accessibility/Fluent report](evidence/story-13-6/2026-10-16-completion/ux-review/validation-report.md) and [HTML twin](evidence/story-13-6/2026-10-16-completion/ux-review/validation-report.html) preserve the original four Medium evidence findings and one subsequent Medium focus-return finding, with regression dispositions. All five scoped findings and all sixteen subsequent Build-review rows have patches and executed proof. The deterministic delayed-vendor-return regression closes the independently reproduced second return. This is automated evidence review; Windows, manual AT/zoom/devices and Product/Fluent/live-backend acceptance remain separate.

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
| UX-136-REVIEW | `performed: scoped automated evidence review` | Authorized headless accessibility/Fluent review completed 2026-10-16; four original Medium findings and one follow-up Medium finding resolved. Reports and parent audit above bind the final candidate. Human UX/Product/Fluent sign-off remains separate and is not inferred. |

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

All manual boxes above stay open. NVDA/JAWS/VoiceOver, actual browser zoom, tablet/phone, heard speech and reviewer observations were not performed. OI-16, G-4, FLUENT-APP-1, Product approval/reapproval and separate live EventStore fresh-row acceptance remain unresolved. The failed comparison/configured-test gates must be reconciled; a passing fixture does not close an uncovered platform or surface cell.

```yaml
story: 13.6
final_classification: open
windows_execution: not performed
manual_at: not performed
actual_400_percent_zoom: not performed
real_tablet: not performed
real_phone: not performed
zero_field_generated_browser_surface: executed
ux_evidence_review: performed; all five recorded Medium findings resolved
product_approval_inferred: false
```
