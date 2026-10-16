# Story 13.6 accessibility result and human checklist

Updated 2026-10-16. **Readiness remains open.** The configured Debug Shell gate now passes. Twelve Firefox/WebKit visual comparisons, one additional canonical Chromium Settings focus-return failure, fifteen evidence-hardening review groups, Windows execution and manual/owner observations remain open. Automated review supplies no human approval.

## Candidate and evidence identity

- Branch `main`; full source HEAD `cda47ce88bd80f2b9ddf995eba2f0bd27add1cdc` plus three uncommitted source/test changes. Baseline remains `c3708690f8e1dbebfb49682c67a8aba88fd4a092`.
- Final candidate **`sha256:c14be5f5413337c1e5a55b7e49075804761c57015e810113b0f94accb9190732`**, 43 baseline-relative implementation/test/CI/evidence-policy files. [Manifest](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/candidate.json), [raw-byte rows](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/candidate.sha256) and [raw-byte audit](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/parent-audit.json) and [independent audit](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/independent-audit.json) bind source and both unchanged frozen intent blocks. Documentation/evidence are excluded from the aggregate to avoid circular hashing.
- The browser run executed on `sha256:36097daec9b85bb2a11b15e504e7d667a80af9430727789d87afbbaa52396f39`. [Input reuse proof](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/input-reuse.json) establishes that all 42 existing candidate files remain byte-identical; the final candidate adds only the subscription-test file. The rebuilt Debug host, production code, browser assertions/helpers/config and all CSS/JS match the final candidate. The final Shell run includes that test fix.
- Ubuntu 26.04, .NET SDK 10.0.401, Debug/project references, Node 26.4.0, Playwright 1.63.0. Chromium 153.0.8010.12, Firefox 155.0, WebKit 26.6. [Environment inventory](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/font-environment.txt) and [capability probes](evidence/story-13-6/2026-10-16-completion/browser-capabilities.json) retain environment and forced-palette limits.
- All 24 committed platform baseline images, screenshot thresholds, approved Fluent typography/density mappings and the preceding forty candidate inputs are preserved. [Baseline hashes](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/baseline-preservation.json) and source hashes provide exact proof. No staging, commit, push, dependency update or baseline regeneration occurred.

## Root causes and fixes

**Scheduler: test synchronization.** The replay pump admits B and C without waiting for either custom callback; `Task.Run` does not guarantee callback execution order. The old test waited only for C, then incorrectly assumed B's read counter had advanced. Before the fix, a single ThreadPool worker reproduced `readsB=1` three times; concurrent stress failed 5/64. The test now awaits both start signals under its original single five-second deadline while B remains unresolved. Budgets and read assertions remain unchanged. Afterward, the scheduler class passes 88/88, constrained-worker runs pass 3/3 and matching stress passes 64/64. [Diagnosis and exact commands](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/scheduler/diagnosis.json).

**Accepted cleanup: production blocking path exposed by the full lane.** A separate Shell attempt hung inside the accepted-recovery test. Captured stacks show linked cancellation-source disposal waiting synchronously for a parent provider callback; the test cannot release the callback until its clock advance returns. Real dispatch/timer continuations are vulnerable to the same blocking operation. The fix offloads only `Dispose`; immediate cancellation, the two-second recovery deadline and one-second cleanup observation bound remain intact. A deterministic held-parent probe blocks before and returns after; all 68 EventStore tests pass. Moving the whole cleanup was rejected because three existing cancellation-order tests caught the regression. [Final diagnosis, rejected trial and commands](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/scheduler/cleanup-diagnosis.json). No probe was added as a production API.

**Subscription: test-owned task publication.** The next full run exposed the previously recorded subscription-test race: a rejoin clears the fake connection's last callback before the test reads it. The test now retains its own original close/restart task. Production subscription behavior and assertions are unchanged. All 41 subscription tests and the subsequent full Shell lane pass. The failed full attempt remains under [failed-subscription](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/failed-subscription/shell.xml).

**Visuals: demonstrated environment and layout differences.** There are no web-font requests or registered web-font faces, all engines report font readiness, and successive stable captures are byte-identical. Chromium resolves the Fluent stack to Liberation Sans; Firefox/WebKit resolve it to Noto Sans on this host. An isolated DejaVu Fontconfig experiment reproduces the unchanged heading pixels exactly in all twelve retained Firefox/WebKit images; current Noto matches none. Font alignment still leaves the approved target/wrapping/grid changes that postdate those hosted captures. [Causal report](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/visual-diagnosis.md), [all twelve font comparisons](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/font-region-comparisons.json) and [final expected/actual/diff inventory](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/visual-comparisons/inventory.json) retain the proof. This does not approve every residual pixel or clear any comparison. The hosted font/native-renderer inventory and visual-owner reconciliation remain required before a separately authorized image update.

## Executed checks and remaining automated failures

[Verification](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/verification.json), [Shell command/result](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/shell-verification.json), [browser command/result](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/browser-verification.json) and [JUnit](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/browser-junit.xml) retain exact attribution.

| Lane | Exact result |
|---|---|
| Configured Debug Shell | **3681 passed, 0 failed, 0 skipped / 3681**, 64.126s, normal 14-thread collection parallelism; all five existing exclusions preserved. `-longRunning 30` adds diagnostics only. |
| Focused final Shell | Scheduler **88/88**, EventStore **68/68**, subscription **41/41** passed. |
| Final three-engine accessibility/visual | **366 passed, 13 failed, 2 skipped / 381**, 375.286s. Chromium126/1/0; Firefox120/6/1; WebKit120/6/1. |
| Nonvisual portion | **360 passed, 1 failed, 2 skipped / 363**. The failure is Chromium dark Settings initial origin return at state-matrix line381. |
| Visual portion | **6 passed, 12 failed / 18**. Every Firefox/WebKit Light/Dark × Compact/Comfortable/Roomy comparison remains failed. |
| Debug builds | Counter.Web and Shell.Tests passed with **0 warnings/errors**. |
| TypeScript / validator negatives / CI governance | Typecheck passed; **4/4** Node negative tests and **10/10** CI-governance tests passed. |
| State inventory | All **116 rows per engine** retained. Chromium/Firefox strict validation0 errors. WebKit strict448 errors; explicit media-only capability validation0, with **116 actual forced-palette cells uncovered**. |
| Artifact/visual policy | Curated Chromium artifact validation passed under the unchanged50MiB bound; visual governance passed, all baseline hashes preserved. |

The final Settings failure is retained with its [screenshot, trace and video](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/failed-settings-focus/). Six instrumented repetitions passed; their [diagnostic record](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/settings-focus-diagnostic/verification.json) does **not** replace the failed canonical run. Its root cause remains unattributed. QA/Shell focus owner must reproduce the complete dark toolbar→palette→Settings journey and reconcile the failure; no production focus fix or passing full browser gate is claimed.

Current skips are the existing Chromium-serialized Story11.5 computed-style/reduced-motion case in Firefox/WebKit. Historical ten-spec evidence retains eight skips; none is counted as a current pass.

The generated full HTML report expanded to61.70MiB because of failure traces/videos. The curated report preserves the original HTML plus required images/JSON/JUnit below50MiB. Every removed optional trace/video byte is losslessly retained in a separate archive; [retention manifest and restore instructions](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/report-retention.json) explain how to restore optional links. The failing Settings trace/video are also retained directly. Artifact inventory success supplies no passing browser claim.

The earlier interrupted Shell run, rejected cleanup trial, failed subscription full run and pre-cleanup367/12/2 browser run remain separately attributed. The [incoming result](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/handoff-result.md) and preceding Build-review packet remain historical evidence; no interrupted or failed run was promoted to a pass.

## Build review and evidence limits

All three required layers returned before triage. They reported **18 Medium rows, grouped into15 open evidence-hardening issues** in the preceding unchanged measurement helpers/tests; no finding alleges a defect in this continuation's three source changes. [Individual verdicts and retained reproducers](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/review/triage.md) and the append-only deferred ledger record every disposition.

The open groups cover keyboard-boundary traps, pseudo/placeholder opacity, SVG shape/stroke coverage, filters/effective glyph fill/nonancestor backgrounds, ancestor opacity/descendant obstruction, generated text, collapsed or unchanged shadow layers, inset pseudo adjacency and Settings delayed-return regression coverage. They are separate follow-up work; the current scheduler/visual diagnostic continuation preserves those earlier inputs. Passing computed inventories are subject to these known limits and do not certify complete AE coverage. Manual observations and owner approvals remain separate.

## Surface, state and AE mapping

[The complete per-engine state ledger](evidence/story-13-6/2026-10-16-completion/gate-reconciliation/state-ledger.json) enumerates every row and artifact, viewport, sample counts and precise forced-color status. Raw state JSON and actual system-state images are under `gate-reconciliation/browser/<engine>/`.

| Surface/state family | Automated evidence | Remaining limit |
| --- | --- | --- |
| Shell/Home/nav/tabs | Shell chrome, theme menu, exact desktop rails, Home/navigation state rows; real drawer Tab/Shift+Tab cycles, spacing, targets, scanner and current/focus cues | Supported AT, actual zoom, physical devices |
| Toolbar/search/filter/view | Toolbar, filter-popover, view-menu and active tabs in both themes; real search focus/keyboard and existing toolbar journeys | Human speech/device observations |
| Projection/grid/detail | Two/loading/empty/error, reconnect/disconnect/connected, stale, slow query, result cap, filter-empty/hidden, fresh presentation, real missing-detail loss/external focus | Separate live backend fresh-row acceptance and human AT |
| Generated forms/lifecycle | Generated forms, validation, policy denial; all eleven lifecycle states and their icons/cues | Human validation/recovery speech and device use |
| Zero-field | All eleven real failure families plus accepted pending summary, both themes, focus/ownership/no retry/clear/policy gates | Heard safe message once and physical-device operation |
| Palette/settings/dialogs | Default/selected/empty palette, radio focus/settings, destructive and abandonment states, theme/view menus, actual modal return sequencing | Supported AT/system-platform pairing |
| AE-01/03/04/05 | 320 CSS-pixel reflow, simultaneous spacing, target inventories/exceptions, sharp-indicator and keyboard samples with the open measurement limits recorded above | AE-02 actual 400% zoom remains unperformed; the fifteen measurement-hardening groups remain open |
| AE-06/07/08/09 | Nonempty text/icon/boundary/state cue samples; post-switch system snapshots, immediate reduced-motion cues, WCAG 2.2 axe and ownership/keyboard journeys | All 116 Linux WebKit actual forced-palette cells remain uncovered: its engine exposes media rules without authored-color replacement. Actual AT speech is unobserved. |

The only executed undersized-target exception is evidenced Spacing; no unmeasured User Agent Control or blanket exception is inferred. Decorative redundant empty-state illustrations and loading skeletons are explicitly identified in each state artifact. Browser fixtures establish presentation/DOM behavior only.

## Prior UX evidence review and readiness

[Scoped accessibility/Fluent report](evidence/story-13-6/2026-10-16-completion/ux-review/validation-report.md) and [HTML twin](evidence/story-13-6/2026-10-16-completion/ux-review/validation-report.html) preserve the original four Medium evidence findings and one subsequent Medium focus-return finding, with regression dispositions. All five scoped findings and all sixteen subsequent Build-review rows have patches and executed proof. The deterministic delayed-vendor-return regression closes the independently reproduced second return. This is prior automated evidence review. The continuation Build review separately leaves fifteen measurement/test-hardening groups open; its earlier scoped results do not certify those paths. Windows, manual AT/zoom/devices and Product/Fluent/live-backend acceptance remain separate.

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

All manual boxes above stay open. NVDA/JAWS/VoiceOver, actual browser zoom, tablet/phone, heard speech and reviewer observations were not performed. OI-16, G-4, FLUENT-APP-1, Product approval/reapproval and separate live EventStore fresh-row acceptance remain unresolved. The configured Shell gate now passes; the twelve visual failures, additional Settings focus failure and fifteen evidence-hardening groups must be reconciled; a passing fixture does not close an uncovered platform or surface cell.

```yaml
story: 13.6
final_classification: open
configured_shell: passed
visual_comparisons: failed; 12
additional_browser_focus: failed; 1
open_evidence_hardening_groups: 15
windows_execution: not performed
manual_at: not performed
actual_400_percent_zoom: not performed
real_tablet: not performed
real_phone: not performed
zero_field_generated_browser_surface: executed
ux_evidence_review: performed; all five recorded Medium findings resolved
product_approval_inferred: false
```
