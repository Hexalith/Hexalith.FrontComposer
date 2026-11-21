---
title: Hexalith.FrontComposer UX Design Planning Source
status: canonical-planning-source
product_approval: pending-reapproval
g_4: open
oi_16: open-implementation-and-evidence-work
oi_19: open-documentation-parity-work
created: 2026-07-05
updated: 2026-11-21
reconciliation_revision: prd-and-architecture-2026-11-21
sourceOfRecord:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/prd-addendum-2026-09-08.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md
  - _bmad-output/planning-artifacts/ux-experience-2026-07-05.md
---

# Hexalith.FrontComposer UX Design Planning Source

This document is the canonical UX authority in the D-8 three-file chain. The detailed UX artifact is
the visual/style supplement; the experience artifact is the behavior/journey supplement. If UX
artifacts conflict, this file wins. The PRD and addendum own product outcomes, stable requirement IDs,
gates, and approval state; this file makes those outcomes implementable and testable. Supplements,
mockups, wireframes, imports, and historical decisions cannot override this contract.

The November 20 PRD and November 21 architecture govern this revision. The matrices below retain their stable identifiers. The [requirement and evidence ledger](#requirement-and-evidence-ledger) links the implementation stories to those matrices. Product still lists OI-16 and OI-19 as open and SM-6 as unmet. The November 20 PRD document acceptance is recorded separately from formal D-9 re-approval and G-4. [PRD gates](prd.md#01-v10-readiness-milestone-gates) · [Document acceptance record](prds/prd-frontcomposer-2026-11-20/post-approval/validation-report.md)

## Canonical Information Architecture

- A bounded context is presented to operators as one **Module**.
- Each Module has one primary shell entry and one required default **Module Tab**.
- The default tab uses the module plural label, falling back to `Overview`.
- `/{module}` selects its required default tab silently (SS-31). An explicit unknown or disabled `/{module}/{tab}` falls back to the default and announces AM-31 (SS-32).
- Projection flyouts are secondary navigation into `/{module}/{tab}`. They never add a top-level entry.
- Generated commands use `/commands/{BoundedContext}/{CommandTypeName}` from palette, CTA, and direct
  activation paths.
- Home Modules sort by readiness, descending actionable count, then ordinal module name.
- FC-IA-1 is supporting decision history. The active route and navigation contract is stated above.

### Agent and developer entry points

For agent consumers, the existing FrontComposer HTTP MCP host preserves the admission, resource disclosure and lifecycle contracts in FR-17–FR-19a. The G-7 reviewer accepted the September 26 candidate. Each deployment owns its authentication policy. The target agent-facing transport belongs to `Hexalith.McpCli`. Retirement of the HTTP host is coupled to the Projects plug-in and requires approved capability dispositions, consumer cutover, and live, release and rollback evidence. FrontComposer continues to support the developer-operated `inspect` and `migrate` compiler commands. [PRD host contract](prd.md#55-mcp-agent-surface) · [Compatibility inventory](../../README.md#legacy-compatibility-surfaces)

## UX Requirements

Stable identifiers `UX-DR1` through `UX-DR8` are retained. New matrix identifiers are appended and
must not be renumbered.

### UX-DR1 - Design Tokens

`Typography`, `FcTypoToken`, `TypographyStyle`, and their Fluent mappings are supplied by the
`Hexalith.FrontComposer.Contracts.UI` package/assembly while retaining the public
`Hexalith.FrontComposer.Contracts.Rendering` namespace. The nine roles and
`TypographyMappingVersion = "3.1.0"` remain unchanged. `DensityLevel` and `DensitySurface` remain
kernel-safe. `<body data-fc-density>` reflects effective density. Phone and Tablet force Comfortable even when the operator saves Compact or Roomy. Settings previews the saved choice and explains a viewport override. Opening and initial hydration are silent. After hydration, a change to effective density caused by the operator or a subsequent viewport change announces the new value once. [Density precedence](../../src/Hexalith.FrontComposer.Shell/State/Density/DensityPrecedence.cs) · [Density announcer](../../src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityAnnouncer.razor.cs)

### UX-DR2 - Semantic Status Slots

Status values render as semantic icon-plus-visible-text affordances. A persistent accessible name,
state text, and icon/shape carry meaning without color, motion, or hover. `FluentTooltip` may repeat a
concise label on hover and keyboard focus but is never the only label. Numeric count slots remain
`FluentBadge` pills. UX-VC-1 defines the visual fallback. At `431aa71b`, an annotated enum cell renders `FcStatusIcon` with a persistent accessible name and a tooltip label. Its text is absent at rest. The generated presentation still needs a visible localized label next to the glyph in normal and forced colors. [Generated status column](../../src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs) · [Status icon](../../src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusIcon.razor)

### UX-DR3 - Responsive Layout

The shell renders its hamburger before optional `HeaderStart` content. On desktop it switches `FrontComposerNavigation` between labelled and icon-only rail modes. The rail appears when navigation is available for the current scope and layout. Longest segment-prefix matching selects exactly one current item. The framework provides a 48 px icon rail on CompactDesktop (1024 to below 1366 CSS px) and a labelled or collapsed rail on Desktop (1366 and wider). Phone (below 768) and Tablet (768 to below 1024) require an operable drawer containing the authorized Module destinations. [Viewport tier implementation](../../src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-layout-breakpoints.js)

The local Counter.Web check on `431aa71b` found only the drawer title and Close control at 320 CSS px, including after a desktop resize. At 900 CSS px, activating the hamburger exposed no dialog or Module destination. The implementation handoff is to supply the authorized list once, then keyboard-activate Counter from cold and resized layouts and verify the default tab, route heading focus (FM-01), and current item. [November validation](ux-designs/ux-frontcomposer-2026-09-09/validation-2026-11-21/validation-report.md)

Compact projection grids use the exact `32px` row metric from `DataGridDensityMetrics` under default
text settings. That metric is not a clipping ceiling: at text-spacing overrides, 400% zoom, or content
growth, rows expand or content reflows without loss. Grid headers remain sticky while the grid owns
its scroll. At 320 CSS pixels and 400% browser zoom, all operations remain available without
page-level horizontal scrolling. Intrinsically two-dimensional grids and the tab strip may each own
bounded, labelled horizontal scrolling while reading order and keyboard access remain intact and the
selected tab/focus indicator stays visible. Preserved baseline metrics: the Desktop navigation rail is `72px` wide in labelled mode and `48px` in icon-only mode, and projections with more than 15 columns activate column prioritization (PRD FR-11), which determines responsive column retention while the labelled grid region stays operable. These record delivered runtime behavior; they are not numeric responsive breakpoints.

[Responsive navigation reference](ux-designs/ux-frontcomposer-2026-09-09/mockups/key-responsive-navigation.html)

### UX-DR4 - Reusable Interaction Components

The exact public FrontComposer component registry is shared by both supplements:
`FrontComposerShell`, `FrontComposerNavigation`, `FcPageTabs`, `FcPageTab`, `FcPageToolbar`,
`FcCommandPalette`, `FcSettingsDialog`, `FcDestructiveConfirmationDialog`,
`FcFormAbandonmentGuard`, `FcLifecycleWrapper`, `FcProjectionLoadingSkeleton`,
`FcProjectionEmptyPlaceholder`, `FcProjectionConnectionStatus`, `FcPendingCommandSummary`, and
`FcNewItemIndicator`. `FcPageToolbar` is the public product contract; its current internal Fluent
composition is implementation-owned. Interactive UI uses FrontComposer or Fluent UI Blazor V5
components before custom markup.

FC-CNC permits one in-flight local command. A second local submit is blocked, never queued or batched,
and receives localized accessible feedback that it did not run while the existing command stays
visible. UX-VR-1, UX-FM-1, and UX-SS-1 define the complete outcome.

The FR-8 `/` shortcut is enabled only when the active route exposes exactly one enabled
`FcPageToolbar` page-search input. It focuses that input without changing its value. If the route has
no eligible search or the shortcut is disabled, activation is a silent no-op and focus remains where
it is. Editable controls, IME composition, and component-owned chord handling suppress it. Story 13.2 covers the focus destination and the silent no-op cases.

### UX-DR5 - Status And Empty/Loading UX

Projection loading, empty, connection, and pending-command states use the reusable components in
UX-DR4. Lifecycle UX distinguishes HTTP acceptance from projection/status confirmation and names
`Submitting`, `Acknowledged`, `Syncing`, `Confirmed`, `Rejected`, `IdempotentConfirmed`,
`NeedsReview`, `Warning`, and `Degraded`.

The default confirmation profile starts at acknowledgement. Status polls run every 1,000 ms. If the operation is still unresolved after 10,000 ms with time left before the deadline, it reaches non-terminal Degraded (AM-17). At `MaxPendingCommandPollingDurationMs` (default 120,000 ms), the mounted lifecycle closes an unresolved operation as DegradedExhausted (AM-24).

`PendingCommandPollingIntervalMs=0` or `MaxPendingCommandPollingPerTick=0` disables command-status requests. Test each independently. The mounted lifecycle keeps its acknowledgement-based deadline and delivers AM-17 and AM-24 when applicable. Projection fallback uses its own settings. With status polling disabled, the operator must see the unconfirmed outcome without a claim of ongoing checks. At `431aa71b`, `FcPendingCommandSummary` still tells the operator “FrontComposer is checking status” for every pending entry. The summary needs copy that reflects whether status polling is enabled. [Pending summary](../../src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor.cs)

Pre-accept transport failures have zero automatic retries. After acceptance, an unreadable correlation response may trigger one recovery request using the original `MessageId` and payload. `CommandDispatchRetryAttempts=0` disables recovery. Positive supported values allow at most one attempt after `CommandDispatchRetryDelayMs` (default 250 ms), within the remaining recovery budget. A skipped or unsuccessful recovery returns Accepted with the original ID and unknown correlation. No second operation or lifecycle transition follows from that failure. [Shell options](../../src/Hexalith.FrontComposer.Shell/Options/FcShellOptions.cs) · [Accepted correlation recovery](../../src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs) · [Mounted lifecycle deadlines](../../src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs)

On loss of the live projection connection, reconnect attempts use jittered delays capped at `30_000` ms. With fallback polling enabled, a terminal hub close schedules restart within `10` s. The default polling interval is `15` s across at most eight lanes. With `ProjectionFallbackPollingIntervalSeconds=0` the terminal close records disconnection, leaves fallback polling off, and skips automatic closed-connection restart. A successful reconnect shows a notice for `3_000` ms. [Recovery evidence](../implementation-artifacts/spec-11-2-projection-realtime-resilience.md)

Generated projection views render one `FcNewItemIndicator` per eligible entry above the grid. The info icon and localized visible text identify the affected row by `EntityKey`. The default English text is “Updated row: {EntityKey}.” A nonblank `Text` overrides the visible label. AM-21 still announces the localized row-specific text via the shared projection status region. `AriaLabelOverride` applies only to the standalone component without an `Entry`.

Publication is scoped to the tenant and user. The first entry wins for `(ViewKey, EntityKey)` and retains its original ten-second lifetime. Speech is claimed once per tenant, user, `EntityKey` and `MessageId`, across views during that window. Materialization of the row, filter dismissal, expiry and scope change remove the cue silently. DW-679 excludes server-allocated target keys. [Story 13.5 placement decision](../implementation-artifacts/spec-13-5-preserve-fresh-row-meaning-across-visual-and-data-changes.md) · [Indicator text and shared announcement](../../src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcNewItemIndicator.razor.cs)

[Command lifecycle reference](ux-designs/ux-frontcomposer-2026-09-09/mockups/key-command-lifecycle.html)

### UX-DR6 - Accessibility Patterns

Generated and hand-authored UI conforms to WCAG 2.2 AA. UX-AM-1 owns announcements, UX-VR-1 owns
validation and rejection, UX-FM-1 owns focus, UX-SS-1 owns surface states, and UX-AE-1 owns measurable
responsive/accessibility evidence. The detailed supplement owns inherited Fluent contrast and visual
fallbacks; the experience supplement owns behavior and recovery.

### UX-DR7 - Page Layout Contract

Full-width content is the default. Constrained content uses the approved `75rem` maximum measure.
Page-like surfaces with two or more sibling titled sections use one `FluentAccordion`, with the primary
item expanded by default. A single primary content region remains directly visible.

### UX-DR8 - Account Control And Server Security

The shell renders its framework-owned account menu after the header actions, including custom `HeaderEnd` content. The public `ShowAccountMenu` option is retained for compatibility and has no effect. Domain modules supply domain policies and configuration. Missing tenant context or denied access leads to the corresponding safe heading and recovery action (SS-05/SS-06 and FM-10).

In the combined host, Parties administration requires one selected tenant and current Tenants API evidence for that tenant and user. The tenant must be active and the caller authorized as `TenantOwner`. If the principal changes during the lookup, the host discards the old result. [Host authorization contract](architecture.md#host-and-runtime-boundaries)

## UX-AM-1 - Announcement Matrix

`Polite status` means one shared `role="status"`/`aria-live="polite"` channel per surface. `Polite
combobox status` is the palette-owned instance of that same channel: one
`role="status" aria-live="polite" aria-atomic="true"` node emits AM-28, and any inherited/native
result-count speech is omitted so only one owner announces zero results. `Focused summary` means a
visible `role="group"`, an accessible error-summary label, and `tabindex="-1"`; the complete summary
is inserted before programmatic focus, has no `aria-live` or `role="alert"`, and focus is its only
speech path. This prevents live-region plus focus duplication.

Each row's event-dedupe key includes the state or result identity and suppresses repeated renders of
that same event. Cross-state coalescing uses a separate group key that intentionally excludes state:
lifecycle operation ID; surface + connection epoch; surface + operator-initiated load/filter
operation; or palette session. An eligible non-terminal change in the same group restarts a trailing
`250` ms timer, stale async results are discarded, and only the last eligible message is announced
when the window closes. A terminal outcome, focused summary, or navigation failure cancels any pending
intermediate message for its group and announces immediately. Fake-time tests drive `249` ms and
`250` ms boundaries, including two different intermediate states in one group, and assert the exact
message sequence and count. Messages are localized and contain no support-sensitive values.

| ID | Trigger or transition | Observable announcement | Channel | Dedupe/coalescing rule | Silent behavior | Acceptance evidence |
| --- | --- | --- | --- | --- | --- | --- |
| AM-01 | Loading begins | “Loading {surface label}.” | Polite status | Once per surface/load operation | Skeleton frames and retry ticks | bUnit transition count + screen-reader DOM assertion |
| AM-02 | Empty settles | “No {item label} available.” plus the permitted recovery | Polite status | Once per query result | Re-render of the same empty result | bUnit result identity/dedupe assertion |
| AM-03 | Data settles | Updated result count or “{surface label} loaded.” only when the operator initiated load/filter | Polite status | Dedupe by operation + result identity; coalesce by surface + originating operation, trailing 250 ms, last eligible result wins | Background refresh with no meaningful change | Fake-time count + e2e filter/reload assertion |
| AM-04 | Stale | “Data may be out of date.” | Polite status | Once per surface/connection epoch/state | Repeated stale observations | Connection-state bUnit assertion |
| AM-05 | Reconnecting | “Connection lost. Reconnecting.” | Polite status | Once per surface/connection epoch/state | Backoff attempts | Fake-time bUnit assertion |
| AM-06 | FallbackPolling | “Live updates are unavailable. Checking for updates.” | Polite status | Once per surface/connection epoch/state | Each 15 s poll | Fake-time bUnit assertion |
| AM-07 | Recovery | “Connection restored. Data refreshed.” | Polite status | Once per connection epoch | Notice expiry after 3,000 ms | Fake-time bUnit + e2e assertion |
| AM-08 | SlowQuery | “This is taking longer than expected.” | Polite status | Once per query at 2,000 ms | Elapsed-time ticks | Fake-time bUnit assertion |
| AM-09 | MaxItems | “Showing the first {limit} items. Refine filters to narrow the result.” | Polite status | Once per result/limit | Virtualization batches | bUnit result/limit assertion |
| AM-10 | Submitting | “Submitting command.” | Polite status | Operation ID + `Submitting` | Re-render in the same state | Lifecycle bUnit assertion |
| AM-11 | Acknowledged | “Command accepted. Waiting for confirmation.” | Polite status | Operation ID + `Acknowledged` | The post-ack retry tick | Lifecycle fake-time assertion |
| AM-12 | Syncing | “Command accepted. Updating the view.” | Polite status | Dedupe by operation ID + `Syncing`; coalesce all intermediate states by operation ID, trailing 250 ms, last eligible state wins | Enabled status polls (default 1,000 ms) | Lifecycle fake-time sequence/count assertion |
| AM-13 | Confirmed or IdempotentConfirmed | “Command confirmed.” by default. For an idempotent outcome, a nonblank `FcLifecycleWrapper.IdempotentInfoMessage` supplies the announcement | Polite status | One terminal announcement per operation, with the confirmed and idempotent identities preserved | Later duplicate terminal observations | Lifecycle first-terminal-wins assertion |
| AM-14 | Rejected | “Command rejected. Review the message and try again.” | Polite status | One terminal outcome per operation | Duplicate rejection observations | Lifecycle + keyboard recovery assertion |
| AM-15 | NeedsReview | “Command needs review before its result can be confirmed.” | Polite status | One terminal outcome per operation | Poll/retry ticks | Lifecycle bUnit assertion |
| AM-16 | Warning | “Command completed with a warning. Review the details.” | Polite status | One terminal outcome per operation | Duplicate terminal observations | Lifecycle bUnit assertion |
| AM-17 | Degraded, before the local deadline | “Confirmation is taking longer than expected. Review status or continue working.” | Polite status | Once at 10,000 ms for a still-active operation with time left before the deadline | Any later status polls until the configured deadline | Fake-time budget assertion |
| AM-18 | Client validation blocks submit | “Correct the errors before submitting.” plus summary count | Focused summary; no live attributes | Once per failed submit attempt | Individual error insertion before the complete summary is focused | Rendered summary/link/focus/speech-count assertion |
| AM-19 | Safely field-mapped server rejection | Rejected lifecycle message plus mapped field summary | Focused summary; no live attributes | Once per rejected operation; cancel/suppress AM-14 for the mapped outcome | Repeated server payload | Server-map + summary/focus/speech-count assertion |
| AM-20 | Blocked second submit | “This command did not run. Another command is already in progress.” | Polite status | Once per blocked attempt | Original command progress continues under its own operation ID | Concurrent-submit bUnit assertion |
| AM-21 | Fresh row appears | “Updated row: {EntityKey}.” | Shared polite projection status | Once per tenant + user + EntityKey + MessageId across views during the active window | Expiry/removal and suppressed duplicate publication | FC-NIP bUnit/e2e assertion across two views plus silent-expiry assertion |
| AM-22 | Expanded detail becomes filter-hidden | “The expanded row is hidden by the current filters.” | Polite status | Once per row/filter result | Further renders of the same result | Grid bUnit assertion |
| AM-23 | Navigation fails | “Could not open {safe destination label}. You remain on {current page label}.” | Polite status | Once per activation attempt | Internal route/error detail | Router/palette e2e assertion |
| AM-24 | Degraded, local deadline exhausted | “Confirmation was not received. Review status later or continue working.” | Polite status | Once when the accepted operation reaches its configured deadline (default 120,000 ms), including with status polling disabled. Terminal and immediate | Every later poll/result for that closed local lifecycle | Fake-time ceiling and terminal-count assertion |
| AM-25 | Shell has no registrations | “No modules are available.” | Polite status | Shell bootstrap identity + settled `no-registrations` state | Same-state renders | Bootstrap bUnit message/count assertion |
| AM-26 | No tenant, no access, or authorization denial | Exact visible heading for the outcome | Focused heading; no live attributes | Activation attempt + outcome; focus is the only speech path | Hidden inaccessible navigation items and same-state renders | Route/policy focus and speech-count assertion |
| AM-27 | Filter has no matches | “No {item label} match these filters.” | Polite status | Filter operation + normalized filter/result identity | Same zero-result render | Filter bUnit message/count/reset assertion |
| AM-28 | Palette has no matches | “No commands or pages match.” | Polite combobox status | Dedupe by palette session + normalized query + zero-result identity; coalesce by palette session, trailing 250 ms, and discard stale query results | Repeated render of the same query/result | Palette fake-time message/count assertion |
| AM-29 | Startup fails | “FrontComposer could not start. Review the configuration.” | Focused heading; no live attributes | Bootstrap attempt + failure category; focus is the only speech path | Exception detail and repeated renders | Startup-panel focus/redaction assertion |
| AM-30 | Projection query fails or the surface is offline | “Data could not be loaded.” or “You are offline. Data cannot be refreshed.” | Polite status | Query operation + safe failure category, or connection epoch + offline | Retry ticks and repeated same failure | Query/offline message/count/recovery assertion |
| AM-31 | Invalid Module Tab falls back to default | “That page is unavailable. Showing {default tab label}.” | Polite status | Activation attempt + requested safe route + resolved default | Canonicalization of the same valid default route | Router/tab fallback assertion |

## UX-VR-1 - Validation And Rejection Matrix

| ID | Case | Field relationship and summary | Focus | Input | Keyboard-only recovery | Announcement/evidence |
| --- | --- | --- | --- | --- | --- | --- |
| VR-01 | Client validation | Each declared field group preserves its visible label, programmatic group name/description, declared group/field order, and stable error targets. Each Fluent input exposes invalid state and references its error; one top summary links to every invalid control in that declared DOM order. | Focus the complete summary after failed submit; summary links and “first error” move focus to the target control | Preserve all useful values | Activate summary links, correct fields, resubmit, or cancel | AM-18; bUnit asserts group/field relationships and order, stable targets, focus, one speech event, and preserved values |
| VR-02 | Server rejection with a support-safe field map | Lifecycle remains `Rejected`; mapped errors also use VR-01 relationships | Focus mapped summary once; no second live announcement | Preserve values | Navigate mapped links, edit, retry, or cancel | AM-19; test proves lifecycle identity is retained |
| VR-03 | Server rejection without a safe field map | Lifecycle rejection region shows support-safe reason and recovery actions; no fabricated field error | Keep focus in the lifecycle/recovery context; first recovery action follows the message in tab order | Preserve values when retry/edit is meaningful | Edit and retry, return, copy safe support reference, or cancel | AM-14; bUnit/e2e prove recovery without pointer input |
| VR-04 | Authorization denial | No field is marked invalid; fail-closed panel names the unavailable action without policy internals | Focus denied-state heading when it replaces the requested surface | Preserve no sensitive command payload beyond the owned form lifetime | Return to the prior surface or re-authenticate when offered | AM-26 focus-only path; route/policy test |
| VR-05 | Blocked second submit | No validation errors are added; the original lifecycle remains the only advancing operation | Focus remains on the attempted submit control; “View active command” may move to the original lifecycle | Preserve both visible forms; do not dispatch/queue the second | Review active command, edit/cancel second form, or retry after terminal state | AM-20; dispatch-count, focus, copy, and announcement-count assertions |
| VR-06 | Navigation with unsaved edits | After 30 seconds of edits, `FcFormAbandonmentGuard` prevents the requested navigation and exposes an in-flow warning with a programmatic name/description | Focus “Stay on form”; after staying/Escape, return to the captured edited control or form heading fallback | Preserve on “Stay on form”; discard only through explicit “Leave anyway” | Tab/Shift+Tab continues in page order; Enter/Space activates either action; Escape stays and hides the warning | OF-04/FM-11 relationship, focus-return, navigation-count, and value-preservation assertions |

## UX-FM-1 - Focus Matrix

| ID | Event | Required destination | Failure/fallback | Unobscured-focus assertion |
| --- | --- | --- | --- | --- |
| FM-01 | Successful client route, direct deep link, CTA, or palette navigation | Route-level `h1` with programmatic focus after the new view is ready | If activation fails, retain focus on the invoker or current route heading and use AM-23 | Heading bounding box is fully outside sticky chrome and overlays |
| FM-02 | Keyboard Module Tab selection | Selected tab retains focus; its labelled tabpanel changes | Invalid tab resolves to the default tab and exposes the selected state | Active tab and focus ring remain fully visible in the tab-strip scrollport |
| FM-03 | Open command palette | Palette query/input | If no query input renders, first enabled result or close control | Focus is inside the active overlay and not behind shell chrome |
| FM-04 | Close palette without navigation | Palette invoker | If removed, route `h1`; never `body` while an interactive target exists | Target is scrolled clear of sticky chrome |
| FM-05 | Close settings or destructive-confirmation dialog | Captured dialog origin | If the origin is removed, disabled, or disconnected, focus the current route `h1` | Target is in the active document, visible, and outside any closing overlay |
| FM-06 | Failed client submit | Validation summary | If the summary cannot render, first invalid Fluent input | Summary is scrolled below sticky chrome and above fixed footer/content |
| FM-07 | Activate summary link/first-invalid action | Linked invalid input | Next invalid input in DOM order if the target disappeared | Full input and focus indicator are visible within page/dialog scrollport |
| FM-08 | Unmapped server rejection | Current lifecycle/recovery context; do not steal focus solely because a polite message updated | First recovery action is next in tab order | Recovery message/action are not covered by banners or pending summaries |
| FM-09 | Blocked second submit | Attempted submit control | “View active command” explicitly moves to the active lifecycle heading | Both the retained control and optional destination are unobscured |
| FM-10 | No-tenant/no-access replaces a surface | Replacement-state heading | On authentication redirect, use platform login focus; on return use route `h1` | Heading and primary recovery action are visible without pointer scrolling |
| FM-11 | Unsaved-edit navigation exposes or closes the in-flow abandonment warning | On exposure, “Stay on form”; after Stay/Escape, the captured edited control | Form heading when the captured control is removed, disabled, or disconnected; successful “Leave anyway” uses FM-01 at the destination | Warning, focused action, restored control, and its focus indicator are entirely outside sticky chrome/messages |
| FM-12 | `/` on an active route outside editable/IME/component-owned contexts | The route's single enabled `FcPageToolbar` page-search input, with its value unchanged | If absent, disabled, or ambiguous, perform no action and retain current focus | The search input and its full focus indicator are visible outside sticky chrome and expanded toolbar content |

## UX-OF-1 - Overlay And Guard Focus Matrix

Before shortcut activation, the shell captures the connected, enabled `document.activeElement` as a
direct origin handle and retains its stable evidence locator (`data-testid` when contract-owned).
Mouse/pointer activation uses the visible control as the same origin. Closing returns to that element;
if it is removed, disabled, or disconnected, FM-04/FM-05 supplies the route-heading fallback. Tests
open from shell navigation and page content and remove one captured origin before close.

Palette/dialog rows have a programmatic accessible name from their visible heading or palette label and
a description from visible consequence/help text when present. `Tab`/`Shift+Tab` cycle within a modal
dialog; palette results follow the inherited combobox/listbox model; background content is not focusable
while a modal is active. OF-04 is deliberately an in-flow guard, not a dialog, and does not trap focus.

| ID | Surface | Invoker/origin | Initial focus | Containment and submit/error destination | Escape | Close/return | September 9 implementation and evidence snapshot |
| --- | --- | --- | --- | --- | --- | --- | --- |
| OF-01 | `FcCommandPalette` overlay | Captured active element for `Ctrl+K`, or visible palette button | Search/query input; otherwise first enabled result, then close | Query/results/close follow the inherited combobox/listbox model; navigation success uses FM-01 and failure uses AM-23 | Closes without navigation | FM-04 | Palette baseline delivered; origin capture, stale-result, containment, return/fallback, and message-count implementation/evidence open |
| OF-02 | `FcSettingsDialog` modal | Captured active element for `Ctrl+,`, or visible settings action | Dialog heading (`tabindex="-1"`), then first setting in tab order | Modal cycle; settings apply live; any exposed error focuses a complete local summary by AM-18 | Closes; live changes are not rolled back | FM-05 | Live-update/Restore-defaults/Done baseline delivered; deterministic origin, entry, error, containment, and return evidence open |
| OF-03 | `FcDestructiveConfirmationDialog` modal | Destructive action | Cancel, the non-destructive action | Modal cycle; validation/policy failure focuses a complete local summary or denied heading | Cancels; never confirms | FM-05 | Cancel autofocus and explicit-confirm baseline delivered; name/description, containment, return/fallback, and error evidence open |
| OF-04 | `FcFormAbandonmentGuard` in-flow warning | Captured edited control when close/back/navigation is intercepted after the 30-second threshold | “Stay on form,” the non-destructive action | No modal cycle; actions remain in page order; Stay/Escape restores the origin, explicit “Leave anyway” proceeds | Stays, hides warning, preserves input | FM-11 | In-flow warning, Stay autofocus, Escape-stays, and explicit Leave baseline delivered; accessible relationship and deterministic origin-return/unobscured evidence open |

## UX-SS-1 - State-By-Surface Acceptance Matrix

The classification subject is the attempt, result, connection epoch, overlay session, or guard session
named in `Surface / state`. `Terminal` means that subject has concluded; a permitted action may start a
new subject. `Non-terminal` means the same subject can still advance through owned user input or
observed system evidence. It does not mean the overall surface is permanently closed.

| ID | Surface / state | Entry evidence | Visible meaning | Permitted actions | Recovery / timeout | Announcement | Class | September 9 implementation and evidence snapshot |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SS-01 | Application shell / no registrations | Valid bootstrap, zero Domain Manifest modules | “No modules are available.” | Settings/account | Re-evaluate after registrations or reload | AM-25 | Terminal | Delivered baseline; announcement evidence open |
| SS-02 | Home directory / Loading | Manifest/count query pending | Skeleton matching directory layout | Navigate shell chrome; cancel route | Resolve to Empty or Data | AM-01 | Non-terminal | Delivered baseline; OI-16 announcement evidence open |
| SS-03 | Home directory / Empty | Query succeeds with no visible modules | No accessible Modules; no error | Account/settings; authorized setup guidance | New registration/access or refresh | AM-02 | Terminal | Delivered baseline; OI-16 evidence open |
| SS-04 | Home directory / Data | One or more visible Modules | Urgency-ordered Module cards/counts | Open Module, palette, settings | Refresh/realtime updates | AM-03 only for operator-initiated load | Terminal | Delivered baseline; ordering regression evidence |
| SS-05 | Shell/module / no tenant | Tenant accessor missing or stale | Explicit fail-closed context message, never empty-looking data | Select/sign in to tenant if offered; return | Context change then reload | AM-26 focus-only path | Terminal | Delivered fail-closed baseline; focus/copy evidence open |
| SS-06 | Module / no access | Authorization denies visibility or activation | Hidden when policy requires; otherwise support-safe denied state | Return, re-authenticate if offered | Permission/auth change | AM-26 for an activated denied state; hidden entries are silent | Terminal | Delivered baseline; focus/recovery evidence open |
| SS-07 | Projection / Loading | Query dispatched, no settled result | Layout-matched skeleton | Cancel navigation; use shell chrome | Resolves to Empty, Data, or failure state | AM-01 | Non-terminal | Delivered baseline; dedupe evidence open |
| SS-08 | Projection / Empty | Successful unfiltered result has zero rows | No items exist; authorized create CTA only when allowed | Create, refresh, navigate | New item or refresh | AM-02 | Terminal | Delivered baseline; announcement evidence open |
| SS-09 | Projection / Data | Successful result has rows | Current rows, semantic statuses, details | Filter, sort, page, expand, allowed command | Realtime/refresh changes state | AM-03 as applicable | Terminal | Delivered baseline; responsive matrix evidence open |
| SS-10 | Projection / Stale | Freshness evidence expires or reconciliation marks stale | Data may be outdated; freshness meaning remains non-color | Refresh; view; mutations fail closed when current evidence is required | Reconcile or transition to reconnect/fallback | AM-04 | Non-terminal | Delivered baseline; full evidence open |
| SS-11 | Projection / Reconnecting | SignalR disconnect detected | Live connection lost; reconnect underway | Continue safe reading; manual refresh where supported | Jittered backoff capped at 30,000 ms. On terminal close, restart within 10 s when fallback is enabled. With interval zero the shell records disconnection and skips automatic restart | AM-05 | Non-terminal | Delivered recovery; retry-silence evidence open |
| SS-12 | Projection / FallbackPolling | Realtime unavailable and fallback polling is enabled | Periodic checks replace live updates | Continue safe reading; refresh | Poll at the configured interval (default 15 s) across at most eight lanes until reconnect | AM-06 | Non-terminal | Delivered recovery; poll-silence evidence open |
| SS-13 | Projection / SlowQuery | Query remains pending at 2,000 ms | Query is slow, not failed | Wait, adjust/cancel filters if supported, navigate | Clears when result/failure settles | AM-08 | Non-terminal | Delivered baseline; fake-time announcement evidence open |
| SS-14 | Projection / MaxItems | Unfiltered result reaches 10,000 item cap; virtualization from 500 rows | Result is intentionally limited | Filter, sort, page, inspect visible rows | New query below cap clears state | AM-09 | Terminal | Delivered baseline; reflow/keyboard evidence open |
| SS-15 | Projection / filter-no-results | Active filters yield zero rows while data exists | No matches; filters remain visible | Reset/edit filters | Next filter result | AM-27 | Terminal | Delivered baseline; detail-hidden evidence open |
| SS-16 | Command lifecycle / Submitting | Valid form accepted locally, dispatch not acknowledged | Command is being sent | Cancel only if contract supports it; do not resubmit | Zero pre-accept retries; retryable failure preserves input | AM-10 | Non-terminal | Delivered core state; complete evidence open |
| SS-17 | Command lifecycle / Acknowledged | HTTP/backend acknowledgement exists | Transport accepted; outcome unconfirmed | Continue work; inspect status | Optional correlation recovery per UX-DR5. A skipped or failed recovery preserves Accepted and the original MessageId | AM-11 | Non-terminal | Delivered core state; wording/dedupe evidence open |
| SS-18 | Command lifecycle / Syncing | Awaiting projection/status confirmation | View is updating; not yet Confirmed | Continue work; inspect status | Default status cadence 1,000 ms. If unresolved at 10,000 ms with time left, enter SS-24. The configured local deadline applies even with status polling disabled | AM-12 | Non-terminal | Delivered core state; coalescing/budget evidence open |
| SS-19 | Command lifecycle / Confirmed | Projection or confirmed status evidence matches operation | Requested material outcome confirmed | Close, inspect affected surface, start another command | None | AM-13 | Terminal | Delivered baseline; terminal-once evidence open |
| SS-20 | Command lifecycle / Rejected | Authoritative rejection evidence | Command did not apply; safe reason/recovery | Edit/retry when allowed, close, support path | New submit is a new operation | AM-14 or AM-19 | Terminal | Delivered core state; accessible recovery evidence open |
| SS-21 | Command lifecycle / IdempotentConfirmed | Duplicate/idempotent outcome is authoritatively confirmed | Desired outcome already holds | Close, inspect surface | None | AM-13 | Terminal | Delivered product semantic; renderer/evidence expansion open |
| SS-22 | Command lifecycle / NeedsReview | Outcome cannot be safely classified automatically | Human review is required; no success claim | Inspect safe details; follow review path; close | New evidence or manual review | AM-15 | Terminal | Delivered product semantic; renderer/evidence expansion open |
| SS-23 | Command lifecycle / Warning | Terminal evidence includes a non-blocking warning | Outcome has a warning; inspect details | Inspect, close, support/retry only when offered | New operation for retry | AM-16 | Terminal | Delivered product semantic; renderer/evidence expansion open |
| SS-24 | Command lifecycle / Degraded, before deadline | Still unresolved 10,000 ms after acceptance with time left before the configured deadline | Outcome is unconfirmed. Status polling continues only when enabled | Review status, continue working, close | With polling enabled, use the configured cadence until terminal evidence or the deadline. With polling disabled, retain the local deadline | AM-17 | Non-terminal | Delivered lifecycle semantics; timing/announcement implementation and evidence open |
| SS-25 | Command form / blocked second submit | Another local command is in flight and dispatch gate refuses this attempt | Second command did not run or queue; original stays visible | View active command, edit/cancel second form, retry later | Ends immediately for blocked attempt | AM-20 | Terminal for second attempt | Delivered block behavior; localized focus/once-only evidence open |
| SS-26 | Command lifecycle / Degraded, deadline exhausted | The operation is unresolved at the configured deadline measured from acceptance (default 120,000 ms), including with zero polls | Confirmation was not received. The local lifecycle is closed | Review status later, continue working, close, or start a separately identified recovery operation | No automatic transition in this local lifecycle; later backend evidence is presented as a newly correlated update, never retroactive mutation | AM-24 | Terminal | Open implementation and fake-time evidence delta |
| SS-27 | Application shell / Bootstrap | Providers and shell initialize before Module discovery settles | Shell is starting; chrome does not imply data is ready | Use skip/account controls only when ready; wait | Resolve to no registrations, Home Loading, or startup failure | AM-01 scoped to shell bootstrap | Non-terminal | Delivered baseline; exact transition evidence open |
| SS-28 | Application shell / startup failure | Required bootstrap is missing/misordered or shell initialization fails safely | FrontComposer did not start; configuration must be reviewed | Return to host error path; developer follows safe diagnostic | New corrected startup attempt | AM-29 focus-only path | Terminal | Delivered fail-fast baseline; focus/redaction evidence open |
| SS-29 | Application shell / route failure | Router/palette/CTA cannot activate a safe destination | Current page remains active; destination did not open | Retry, choose another destination, remain on current page | New activation attempt | AM-23 | Terminal for attempt | Open focus/announcement implementation and evidence delta |
| SS-30 | Module workspace / Loading | Module route is valid while manifest/page data resolves | Requested Module is loading; shell chrome remains stable | Use shell chrome; cancel navigation | Resolve to selected/default tab, no access, or failure | AM-01 scoped to Module route | Non-terminal | Delivered baseline; announcement/focus evidence open |
| SS-31 | Module workspace / selected/default tab | Valid route resolves requested tab or `/{module}` resolves the required default | One tab is selected and its labelled panel is visible | Select another enabled tab, use page operations | Route/tab change | Intentionally silent; route heading or retained tab focus communicates context | Terminal | Delivered baseline; FM-01/FM-02 evidence open |
| SS-32 | Module workspace / invalid tab fallback | An explicit tab segment is unknown or disabled. Bare Module entry uses SS-31 | Default tab is shown and invalid target is not selected | Continue on default or choose another tab | New valid route/tab activation | AM-31 | Terminal for attempt | Open fallback announcement/focus implementation and evidence delta |
| SS-33 | Module tabs / disabled tab | Manifest/policy marks a tab unavailable | Tab is visibly and programmatically disabled | Skip to an enabled tab; use other Module operations | Manifest/policy change | Intentionally silent until attempted external route, which uses AM-31 or AM-26 | Terminal | Delivered baseline; keyboard/visual evidence open |
| SS-34 | Projection / query failure | Query ends with a safe classified error before a result settles | Data could not be loaded; no empty-state substitution | Retry, adjust filters, navigate, support path when offered | New query attempt | AM-30 | Terminal for query | Delivered failure surface varies; normalized implementation/evidence delta open |
| SS-35 | Projection / offline | Browser/network is offline and no current query can complete | Data cannot be refreshed; cached/stale content is labelled when present | Continue safe reading, navigate, retry after reconnect | The same connection epoch observes recovery, then starts query reconciliation | AM-30 | Non-terminal connection epoch | Open normalized offline implementation/evidence delta |
| SS-36 | Command form / Ready | Route/dialog and authorized editable model are loaded | Form is ready; no command is in flight | Edit, validate, submit, cancel | Submit, denial, or close changes state | Intentionally silent; heading/legend and focus establish context | Terminal | Delivered baseline; field-group evidence open |
| SS-37 | Command form / validation blocked | Client validation prevents dispatch | Errors must be corrected; no command ran | Follow summary links, edit, resubmit, cancel | Corrected validation or close | AM-18 focus-only path | Terminal for attempt | Core validation delivered; relationship/focus implementation and evidence open |
| SS-38 | Command form / authorization denied | Policy evaluation before/after `BeforeSubmit` or service boundary denies execution | Command did not run; no policy internals exposed | Return, re-authenticate if offered, cancel | Permission/auth change and a new attempt | AM-26 focus-only path | Terminal for attempt | Delivered enforcement; replacement/focus evidence open |
| SS-39 | Command form / abandonment decision | Close/back/navigation occurs after 30 seconds of edits | In-flow warning asks whether to stay or leave | Stay on form, Leave anyway, Escape to stay | Same guard session advances only through explicit choice; no implicit timeout decision | Intentionally silent; OF-04 focus/name/description provide context | Non-terminal guard session | Delivered in-flow guard baseline; relationship/origin-return evidence open |
| SS-40 | Command palette / Open | `Ctrl+K` or visible invoker activates palette | Search/navigation overlay is active | Type, move through authorized results, activate, close | Close or navigation | Intentionally silent; UX-OF-1 initial focus/name provide context | Non-terminal | Delivered baseline; focus containment/return evidence open |
| SS-41 | Command palette / no results | Debounced 150 ms authorized search yields zero results | No commands or pages match; query remains editable | Edit/clear query, close | Next debounced query or close | AM-28 | Non-terminal | Delivered no-result surface varies; exact announcement evidence open |
| SS-42 | Command palette / denied or navigation failure | A previously visible result becomes unauthorized, or activation fails | Result does not open; current route remains usable | Edit search, retry when allowed, close | New query/activation | AM-26 for denial; AM-23 for navigation failure | Terminal for activation attempt | Open deterministic policy/route evidence delta |
| SS-43 | Settings dialog / Open | Settings invoker activates dialog | Settings are available in one modal context and changes apply live | Edit preferences, Restore defaults, Done/close | Correct an exposed setting error, Restore defaults, or close | Entry is silent. OF-02 heading focus gives context. Changed effective density follows UX-DR1 | Non-terminal dialog session | Delivered live-update baseline; entry/return/error evidence open |
| SS-44 | Destructive confirmation / Open | Authorized destructive action requires confirmation | Consequence and safe cancel are explicit | Cancel or confirm once | Same dialog session advances through an explicit choice | Intentionally silent; OF-03 cancel focus provides context | Non-terminal dialog session | Delivered baseline; entry/return evidence open |
| SS-45 | Abandonment guard / warning visible | SS-39 intercepts navigation and renders the in-flow warning | Unsaved changes need a stay/leave choice; page content remains the owning context | Stay on form, Leave anyway, Escape to stay | Same guard session advances through an explicit choice | Intentionally silent; OF-04 focus/name/description provide context | Non-terminal guard session | Delivered in-flow baseline; accessible relationship/origin-return evidence open |
| SS-46 | Overlay / close or cancel | Active palette/settings/destructive dialog dismisses without successful navigation | Prior route/form context resumes | Continue from captured origin/context | Immediate OF-01/OF-02/OF-03 return | Intentionally silent unless close itself failed | Terminal for overlay session | Delivered baseline; deterministic return evidence open |
| SS-47 | Overlay / origin removed | Overlay closes after its captured origin is removed, disabled, or disconnected | Current route context resumes | Continue from route heading | Focus current route `h1` through FM-04/FM-05 | Intentionally silent; heading focus is context | Terminal for overlay session | Open deterministic fallback evidence delta |
| SS-48 | Projection / fresh-row indicator | FC-NIP publishes eligible material identity for tenant/user/view/entity | An above-grid cue names the affected row | Inspect row/detail; continue working | Remove silently on materialization, filter dismissal, ten-second expiry or scope change. Clear before rendering a new scope | AM-21 on appearance only | Non-terminal grid-adjacent cue | Delivered FC-NIP baseline; forced-colors/motion/silent-expiry evidence open |
| SS-49 | Projection detail / filter-hidden | Expanded row no longer matches active filters | Expanded content is hidden because filters changed | Reset/edit filters; continue with visible rows | New filter result | AM-22 | Terminal for filter result | Delivered baseline varies; announcement evidence open |

## UX-AE-1 - Responsive And Accessibility Evidence Matrix

Use these criteria for changed shell, navigation, tab, toolbar, projection, form, lifecycle, palette, and dialog surfaces. Story 13.6 records the browser matrix, assistive-technology evidence and the Owner's exact-candidate exceptions. On November 15 the Owner accepted commit `b22be84b` plus the source patch with SHA-256 prefix `31b7fe31`. The receipt allows reuse of the recorded NVDA/Firefox and actual zoom results and documents a WebKit system-palette limitation. Eighteen human checks remain unchecked. JAWS/Chrome, VoiceOver/Safari, physical tablet and physical phone checks are deferred until after v1. [Owner receipt and scope](../implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md#owner-acceptance-of-the-h6-review-patches-and-code-review-completion--2026-11-15)

| ID | Dimension | Test setup | Pass condition | Verification method |
| --- | --- | --- | --- | --- |
| AE-01 | 320 CSS-pixel reflow | Set a 320 CSS-pixel viewport; exercise all operations and overlays | No page-level horizontal scroll or content/operation loss; a labelled grid region and the tab strip may each own bounded horizontal scroll; reading/focus order stays logical and the selected tab/focus indicator remains visible | Playwright reflow checks on the changed surface |
| AE-02 | 400% zoom | Browser zoom 400% at a 1280 CSS-pixel reference viewport and repeat AE-01 flows | Same outcome as AE-01; no clipped control, inaccessible overlay, or fixed-content trap | Actual browser zoom and geometry checks under the Story 13.6 receipt |
| AE-03 | WCAG 1.4.12 text spacing | Inject line-height 1.5× font size, paragraph spacing 2× font size, letter spacing 0.12×, word spacing 0.16× | No loss, clipping, overlap, or control truncation; default 32px grid rows expand when needed | Text-spacing override and content geometry checks |
| AE-04 | WCAG 2.2 AA target size (2.5.8) | Inventory and measure every pointer target on every changed surface, including skip/account/settings/menu controls, Home cards/CTAs, rail, tabs, toolbar, grid/detail, forms, lifecycle/recovery actions, palette, dialogs, and the abandonment guard | At least 24 by 24 CSS px, or record exactly one Inline, Spacing, Equivalent, User Agent Control, or Essential exception. Spacing proves a 24 CSS-pixel diameter circle centered on each undersized target does not intersect another target or such a neighboring circle. Equivalent names a separate conforming control for the same function. Evidence records target, exception, measurement/equivalent control, rationale, and keyboard path. | Pointer target inventory, exception classification and geometry assertions |
| AE-05 | Focus not obscured (2.4.11 plus UX floor) | Keyboard through each surface with sticky chrome, scroll regions, popovers, drawers, messages, and dialogs active | Entire target and focus indicator stay visible within the active viewport/scrollport; focus is never behind app-owned content | Keyboard navigation, scroll and focus-ring visibility assertions |
| AE-06 | Light/dark contrast | Render each UX-VC-1 pair in both active themes | Text meets 4.5:1, large text 3:1, and meaningful non-text/focus boundaries 3:1 against adjacent colors | Light/dark computed contrast and visual regression checks |
| AE-07 | Forced colors | Emulate `forced-colors: active` and traverse UX-VC-1 states | System color/text/border/icon/shape/current-state cues survive; no meaning relies on authored color or background image | Forced-colors state checks under the recorded browser capabilities |
| AE-08 | Reduced motion | Emulate `prefers-reduced-motion: reduce`; trigger navigation, lifecycle, reconnect, and fresh-row states | Non-essential transitions/pulse/smooth scrolling stop; state text/icon/shape and focus changes remain immediate; expiry stays silent | Reduced-motion state and silent-expiry checks |
| AE-09 | Names, roles, keyboard, announcements | Run axe plus semantic DOM, keyboard, focus, and announcement-count assertions | WCAG 2.2 AA tags and product-specific assertions pass; zero critical accessibility finding; no hover-only action | The axe helper includes `wcag22aa` and blocks serious and critical impacts. Pair it with semantic, keyboard and announcement assertions |

## Requirement And Evidence Ledger

| Requirement | Observable result | Implementation and verification record |
| --- | --- | --- |
| FR-8, FR-10 | The account menu and hamburger survive header customization. Route activation focuses the heading, keyboard selection retains tab focus, dialogs return to their invoker or fallback, and `/` targets the sole enabled page search. | [UX-A · Story 13.2](../implementation-artifacts/spec-13-2-make-shell-navigation-and-route-focus-deterministic.md) |
| FR-14, FR-16 | Linked errors lead to the affected field, denial keeps policy details private, and a blocked second command leaves the original lifecycle visible. | [UX-B · Story 13.3](../implementation-artifacts/spec-13-3-preserve-focus-and-input-through-command-safety-outcomes.md) |
| FR-11, FR-12, FR-15 | State changes announce once. Poll and retry ticks stay silent. Acknowledgement, confirmation, rejection and the degraded polling ceiling have distinct outcomes. | [UX-C · Story 13.4](../implementation-artifacts/spec-13-4-announce-projection-and-command-state-without-noise.md) |
| FR-13 | A scoped, materially changed row gets a visible, non-color cue. Duplicate publication and silent expiry follow UX-SS-1. The historical G-5 acceptance covers the live proof with the server-allocated-key exclusion. | [UX-D · Story 13.5](../implementation-artifacts/spec-13-5-preserve-fresh-row-meaning-across-visual-and-data-changes.md) · [G-5 receipt](../implementation-artifacts/spec-13-8-accept-the-completed-fresh-row-live-proof.md#product-decision-receipt) |
| NFR-3, SM-6 | AE-01–AE-09 govern responsive, visual and assistive checks. The Owner accepted the named Story 13.6 candidate with its recorded exceptions. The PRD still lists SM-6 as unmet. | [UX-E · Story 13.6 receipt](../implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md#owner-acceptance-of-the-h6-review-patches-and-code-review-completion--2026-11-15) |
| FR-22 | Adopter tests can express focus, validation, state announcements and fresh-row expiry using the shared Testing package. | [UX-F · Story 13.7](../implementation-artifacts/spec-13-7-provide-reusable-ux-assertions-for-adopters.md) |
| FR-23, OI-19 | Component and migration parity checks exist. The documentation owner still has to reconcile coverage, including DOC-B AC4, against the public inventory for OI-19. | [DOC-A · Story 15.2](../implementation-artifacts/spec-15-2-reconcile-component-and-page-pattern-documentation.md) · [DOC-B · Story 15.3](../implementation-artifacts/spec-15-3-align-migration-and-classification-guidance.md) · [Retrospective](../implementation-artifacts/epic-15-retro-2026-11-01.md#acceptance-verdict) |
| OI-10, OI-4 | Story 15.4 delivered the contingency documentation. The catalog selects Fluent Components and Icons 5.1.0 GA. Product and Architecture still owe the decision on that exact identity. | [DOC-C · Story 15.4](../implementation-artifacts/spec-15-4-correct-the-fluent-ui-v5-contingency-identity.md) · [Fluent decision and evidence](../../docs/fluent-ui-v5-contingency.md) |

Stories carry evidence for their tested revisions. Product owns OI-16, OI-19 and G-4 as recorded in the [PRD gate register](prd.md#01-v10-readiness-milestone-gates). The September status columns in UX-OF-1 and UX-SS-1 preserve the original implementation gaps.

## September 2026 Story Aliases

The September course correction defined these aliases. The ledger above links them to delivered stories and records the remaining acceptance work.

| Alias | Independently completable boundary | Traces |
| --- | --- | --- |
| UX-A | Shell/account/hamburger/search and route/tab/palette/dialog focus behavior passes the canonical keyboard and focus matrices. | FR-8, FR-10 |
| UX-B | Validation, rejection, FC-CNC, command-dialog, and navigation-guard focus/relationship behavior is deterministic and evidenced. | FR-14, FR-16 |
| UX-C | Projection and command lifecycle announcements, coalescing, terminal ceilings, retry/poll, and auth/scope-loss states are deterministic. | FR-11, FR-12, FR-15 |
| UX-D | Fresh-row indicator visual semantics, forced-colors/reduced-motion behavior, duplicate suppression, and silent expiry pass. | FR-13 |
| UX-E | Responsive accessibility evidence covers 320 CSS px, 400 percent zoom, text spacing, target size, focus not obscured, forced colors, reduced motion, keyboard, and required manual assistive checks. | NFR-3, SM-6 |
| UX-F | Reusable Testing helpers express the canonical focus, validation, announcement, state, and expiry matrices without app-specific selectors or hidden timing assumptions. | FR-22 |
| DOC-A | Component, status, public index, page-toolbar, and page-tab documentation matches implemented APIs and evidence. | FR-23, OI-19 |
| DOC-B | Migration catalog/index classification distinguishes CLI-supported executable guidance from manual-only package/API guidance and proves parity. | FR-23, OI-19 |
| DOC-C | Fluent v5 contingency documentation resolves the exact catalog-owned identity or points to a dated Product exception with expiry and revisit trigger. | D-13, OI-10 |
| FLUENT-APP-1 | Product and Architecture decide the exact Fluent v5 catalog identity and RC/GA posture. | D-13, OI-4 |

The November 20 PRD places the exact-identity Fluent decision (OI-4), UX evidence (OI-16) and documentation parity (OI-19) ahead of the digest-bound review and formal Product decision (D-9/G-4).

## UX-OI16-1 - OI-16 Evidence Trail

| Artifact or evidence | Disposition on 2026-09-09 |
| --- | --- |
| `ux-design.md` | Canonical contract repaired; implementation/evidence work remains open and the final document review is pending |
| `ux-design-detailed-2026-07-05.md` | Visual supplement reconciled; implementation/evidence work remains open and the final document review is pending |
| `ux-experience-2026-07-05.md` | Behavior/journey supplement reconciled; implementation/evidence work remains open and the final document review is pending |
| `reconcile-prd-2026-09-09.md` / `reconcile-prd-addendum-2026-09-09.md` | Source decisions and dropped conflicts recorded |
| `review-rubric.md`, `review-accessibility.md`, `review-fluent-ui-v5.md`, `validation-report.md` | Historical first pass: 0 Critical, 8 High; findings must remain visible as superseded evidence |
| `review-rubric-oi-16-2026-09-09.md` / `review-accessibility-oi-16-2026-09-09.md` | Initial dated remediation reviews: 0 Critical with unresolved High findings; retained as history |
| `review-rubric-oi-16-2026-09-09-pass2.md` / `review-accessibility-oi-16-2026-09-09-pass2.md` / `review-fluent-ui-v5-oi-16-2026-09-09-pass2.md` | Intermediate reviews: rubric 0 Critical/1 High, accessibility 0 Critical/0 High, Fluent/source 0 Critical/3 High; all cited High findings were remediated after their reviewed digests |
| `review-rubric-oi-16-2026-09-09-pass3.md` / `review-accessibility-oi-16-2026-09-09-pass3.md` / `review-fluent-ui-v5-oi-16-2026-09-09-pass3.md` | Final reviewer candidates; their contents, exact UX-file digests, and synthesized validation—not this row—determine the document-review result |
| `review-structure-oi-16-2026-09-09.md` / `review-prose-oi-16-2026-09-09.md` | Required structure/prose review trail; editorial review changes no requirement or gate state |
| Existing bUnit/e2e/Governance evidence | Partial historical evidence only; see Requirement And Evidence Ledger |
| New SM-6 implementation evidence | Open: validation links/focus, complete announcements/states, 320/400%, text spacing, target size, unobscured focus, forced colors, and reduced motion |
| OI-19 | Open independently; this repair does not establish full FR-23 parity |
| OI-16 / SM-6 | Open until new deterministic evidence exists; a clean document review closes only the contract-quality portion |
| G-4 / D-9 / Product approval | Open / pending; only Product can approve the exact PRD/addendum digest pair after all G-4 prerequisites close |

## Governance Rules

- Use FrontComposer or Fluent UI Blazor V5 components for interactive UI.
- Do not introduce raw `<button>`, `<input>`, `<select>`, or `<textarea>` outside documented
  test/specimen carve-outs.
- Use Fluent 2 tokens and component parameters; do not recreate theme primitives in custom CSS.
- `--fc-color-accent`, if present, is only an alias of the active Fluent V5 accent role; it never owns
  an independent seed or palette.
- Preserve stable accessible names and test selectors where they form an evidence contract.
- Visual/accessibility-sensitive changes require rendered DOM, computed style, bUnit, e2e, or
  governance evidence mapped to the matrices above.
- Matrix results follow the PRD §7 evidence policy (2026-10-23, superseding the 2026-10-21 storage amendment and Stories 15.6/15.7), which changes no AE-01–AE-09, manual-evidence, approval or committed visual-baseline requirement and closes no Product approval, OI-16 or G-4.

## Story Design Notes

Visual or layout-sensitive stories cite this file and the owning supplement sections. Each story names
which existing runtime behavior it preserves and which new matrix rows it implements or evidences.
Supplementary artifacts can add detail but cannot change the canonical IA, WCAG 2.2 AA floor, FC-CNC,
FC-NIP, timing budgets, or approval state.

## Related Planning Artifacts

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-22.md`
- `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md`
- `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md`
- `_bmad-output/contracts/fc-ia-1-module-tab-ia-decision-2026-07-05.md`
