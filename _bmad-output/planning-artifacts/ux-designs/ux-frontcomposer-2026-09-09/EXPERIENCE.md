---
name: Hexalith Common Application UX
status: draft
updated: 2026-09-09
sources:
  - _bmad-output/planning-artifacts/ux-design.md
  - _bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md
  - _bmad-output/planning-artifacts/ux-experience-2026-07-05.md
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/prd-addendum-2026-09-08.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md
  - _bmad-output/planning-artifacts/epics.md
---

# Hexalith Common Application Experience

This file owns **how FrontComposer works**: information architecture, behavior, state, interaction, accessibility, and journeys. `DESIGN.md` is its peer and owns appearance. Together they supersede the legacy single-file UX precedence chain. Within their respective domains, both spines win over mockups, wireframes, imports, historical supplements, and implementation examples.

## Foundation

The primary human form factor is desktop-first responsive web. The shared shell also serves compact and narrow browser viewports; it is not a native mobile or desktop product. Nonvisual peer surfaces are part of the same product experience: generated C# output, the `frontcomposer` CLI, the MCP protocol, and the Testing package.

The UI system is **FrontComposer + Blazor Fluent UI V5**. It inherits Fluent behavior unless this spine defines a FrontComposer delta. `DESIGN.md` is the sole visual identity reference and exposes the configurable default accent as `{colors.accent-thread}` without redefining Fluent.

The audience is operational: adopter developers, authenticated operators, AI-agent integrators, framework maintainers, and release owners. Bespoke consumer marketing or transactional UX is outside this contract. Hexalith.Tenants is the obligated first adopter; Hexalith.Parties is only a dated Product-selected D-7 fallback and inherits the identical evidence obligation.

Every human surface is tenant- and user-scoped. Missing or stale tenant context is an explicit fail-closed state, never empty-looking data. Every agent surface is host-authenticated and follows the exact MCP disclosure boundaries below.

## Information Architecture

| Surface | Entry / route | Purpose | Journey landing |
|---|---|---|---|
| Application shell | Authenticated app open | Global frame, skip links, account, settings, palette, one Module entry per bounded context | UJ-1, UJ-2, UJ-3 |
| Home directory | `/` and `/home` | Progressive Module discovery ordered by urgency | UJ-1, UJ-2 |
| Module workspace | One shell Module entry; `/{module}` | Parent surface for one bounded context; aliases its required default Module Tab | UJ-2 |
| Module Tab | `/{module}/{tab}` | Primary route-backed view inside the Module; default name is module plural label, fallback `Overview` | UJ-2 |
| Projection flyout | Secondary action on a Module entry | Lists projection links into Module Tabs; never a second primary entry | UJ-2 |
| Projection list | A projection Module Tab | Search, filter, sort, status, paging/virtualization, freshness, row actions | UJ-2 |
| Projection row detail | Expand action from a projection row | Inspect one entity in an accessible nested region and launch allowed commands | UJ-2, UJ-3 |
| Generated command form | Inline or CompactInline host; FullPage at `/commands/{BoundedContext}/{CommandTypeName}`; palette or authorized CTA | Validate and submit one domain command | UJ-3 |
| Command lifecycle | Attached to the active generated command | Separate transport acknowledgement from confirmed/rejected/review/degraded outcome | UJ-3 |
| Settings | Header control or `Ctrl+,` | Theme and density selection, preview, reset, persistence | UJ-2, UJ-3 |
| Account control | Always-present header control | Sign-in/sign-out and account access through `/authentication/challenge` and `/authentication/sign-out` | UJ-2, UJ-3 |
| Customization diagnostics | Development-only override host | Explain customization-contract mismatches without exposing operator data | UJ-5 |
| MCP tool catalog/call | `tools/list`; `tools/call` | Discover and execute visible generated command tools | UJ-4 |
| MCP projection/skill resources | `resources/list`; `resources/read` | Read registered projection descriptors/data and validated skill reference content | UJ-4 |
| MCP lifecycle polling | `frontcomposer.lifecycle.subscribe` | Observe a command across later requests and DI scopes | UJ-4 |
| Generated output and inspect | `obj/{Config}/{TFM}/generated/HexalithFrontComposer/`; `frontcomposer inspect` | Inspect generated forms, grids, registrations, manifests, diagnostics, and drift | UJ-1, UJ-5 |
| Migration plan/apply | `frontcomposer migrate` | Preview or atomically apply an allowlisted migration edge; dry-run by default | UJ-5 |
| Adopter test harness | FrontComposer Testing package in bUnit | Exercise generated consumer success, failure, authorization, paging, focus, and accessibility behavior | UJ-6 |

Navigation invariants:

- Operators see one **Module** per bounded context and exactly one primary shell entry for it.
- Every Module has a required default **Module Tab**. `/{module}` and `/{module}/{default}` render the same tab; non-default tabs use `/{module}/{tab}`.
- Projection flyouts are secondary. They route into Module Tabs and cannot replace the workspace/default-tab model.
- Exactly one navigation item is active, selected by longest segment-prefix matching.
- Home ordering is deterministic: ready Modules first, then descending actionable count, then Module name by ordinal comparison.
- Generated commands use `/commands/{BoundedContext}/{CommandTypeName}` from palette, CTA, and direct activation.
- Application navigation never promotes individual commands, projections, or module subpages into additional top-level entries.

### Surface closure

| Source journey | Surfaces that deliver it |
|---|---|
| UJ-1 | Generated output → validated bootstrap → Application shell → Home directory |
| UJ-2 | Home directory → Module workspace/default Module Tab → Projection flyout/list → Row detail |
| UJ-3 | Projection row/detail, palette, or CTA → Generated command form → Command lifecycle → refreshed projection |
| UJ-4 | Authenticated MCP catalog → tool/resource admission → call/read → MCP lifecycle polling |
| UJ-5 | Annotated source/generated output → diagnostics/drift → inspect → migrate → compatibility evidence |
| UJ-6 | Adopter test harness → configured scenario → rendered/asserted generated consumer surface → redacted evidence |

## Voice and Tone

Microcopy is direct, localized, evidence-based, and support-safe. Brand posture lives in `DESIGN.md`.

| Do | Don't |
|---|---|
| “Command accepted. Waiting for projection confirmation.” | “Saved successfully” on HTTP acceptance |
| “No parties match these filters.” Preserve the filters and offer reset. | “Nothing here!” or an unexplained blank grid |
| “This data may be stale. Refresh or wait for reconnect.” | Report “Realtime disconnected” without consequence or recovery |
| Name the domain action: “Create party”, “Edit party” | Generic “Submit” when the action is known |
| Explain rejection using support-safe error code, category, suggested action, and docs code | Expose raw EventStore metadata, event payloads, tokens, JWT claims, stack traces, or unrestricted PII |
| Announce meaningful state once; coalesce progress | Announce every retry, poll tick, render, or row expiry |

[NOTE FOR UX] Exact copy for Warning, NeedsReview, Degraded, missing tenant, FallbackPolling, SlowQuery, MaxItems, and fresh-row dismissal is not approved. Preserve the meanings and announcement rules below; do not invent final microcopy.

## Component Patterns

Visual specifications for every row live in `DESIGN.md` Components.

| Component / pattern | Behavioral contract |
|---|---|
| **shell-frame — `FrontComposerShell`** | Hosts one main landmark, skip links, providers, header, navigation, route content, footer, shortcuts, and the always-present account control. Successful client navigation focuses the route `h1`; failed navigation preserves usable focus and announces failure. |
| **navigation-rail — `FrontComposerNavigation` + `FcHamburgerToggle`** | Shows exactly one primary entry per Module and one active item. Hamburger is always available; Desktop toggles labeled/icon-only rail, while compact/narrow behavior reveals the same Module list without promoting subpages. |
| **account-control — `FcAccountMenu`** | Always renders despite adopter header customization. Exposes sign-in/sign-out routes and returns focus to the invoker when its menu closes. Host/server security owns generic authentication wiring. |
| **home-directory — `FcHomeDirectory` + `FcHomeCard`** | Implements No Modules, Hydrating, Partially Ready, and Ready outcomes. Ready items sort by the canonical urgency rule; activation enters the Module/default tab. Missing tenant overrides all data-like states with fail-closed feedback. |
| **command-palette — `FcCommandPalette`** | Opens with `Ctrl+K` as an ARIA combobox, searches authorization-visible registry entries after a `150ms` debounce, supports keyboard result navigation, and routes commands/pages through canonical routes. Close returns focus to invoker; navigation focuses destination `h1`. |
| **settings — `FcSettingsDialog`** | Opens from header or `Ctrl+,`, supports theme/density selection, preview, reset, and confirmation. Preferences persist only with resolved tenant/user scope; changes update the body density attribute and are announced once. |
| **page-frame — `FcPageHeader` + `FcPageLayout`** | Provides the route `h1` and full-width default or opt-in `{spacing.constrained-content-max}` content. Failed route activation leaves focus on a stable heading/status target. |
| **module-tabs — `FcPageTabs` / inherited Fluent tabs** | Encodes selection in `/{module}/{tab}`, is deep-linkable, and preserves Fluent arrow-key behavior. Keyboard selection keeps focus on the active tab while its labelled tabpanel changes. |
| **page-toolbar — `FcPageToolbar`** | Presents leading search, filter, view/overflow controls, and end-aligned authorized actions. `/` focuses page search only when enabled. Its public behavior is stable; internal Fluent composition is implementation-owned. |
| **page-sections — inherited Fluent accordion** | Groups two or more sibling titled regions on a page/dialog/detail panel. The primary item is expanded by default when included. The only primary content region is never hidden behind expansion. |
| **projection-grid — generated inherited Fluent data grid** | Filters are debounced and resettable; row details, column priority over 15 columns, sticky header, paging/virtualization, keyboard focus, and state notices stay within one labelled grid context. Server-side virtualization begins at `500` rows; unfiltered results cap at `10,000`. |
| **projection-placeholders — `FcProjectionLoadingSkeleton` + `FcProjectionEmptyPlaceholder`** | Skeleton matches expected Card/Timeline/Grid layout and is marked busy. Empty distinguishes “no data” from “no filter matches,” retains context, and offers an authorized create CTA only when available. |
| **projection-health — `FcProjectionConnectionStatus` + `FcSlowQueryNotice` + `FcMaxItemsCapNotice`** | Surfaces Reconnecting/FallbackPolling/recovery, SlowQuery after `2,000ms`, and MaxItems at `10,000` without disabling safe read actions. Meaningful transitions announce once; retry/poll ticks remain silent. |
| **row-detail — `FcExpandInRowDetail` + `FcExpandedRowHiddenBanner`** | Expansion creates a labelled `region` in the row. If a filter hides an expanded row, announce that transition and provide a stable recovery path; do not leave focus on removed content. |
| **status-affordance — `FcStatusIcon` + inherited icon/tooltip; `FcDesaturatedBadge` + inherited badge** | Status has semantic icon, accessible name, and tooltip on hover and keyboard focus. Counts remain badges. Neither color nor hover is required to obtain the meaning. |
| **command-form — generated form + `FcFieldPlaceholder`** | Density uses non-derivable property count: 0–1 Inline, 2–4 CompactInline, 5+ FullPage. Server-controlled/derived fields are hidden and injected later. Unsupported field types show a placeholder. Validation links summary items to controls, focuses the summary on failed submit, preserves useful input, and lets keyboard users reach the first invalid field. |
| **command-authorization — `FcAuthorizedCommandRegion`** | Resolves Pending, Authorized, or NotAuthorized before revealing/activating protected commands. `[RequiresPolicy]` runs before `BeforeSubmit` and again afterward for protected commands; the service boundary also authorizes. |
| **command-safety — `FcDestructiveConfirmationDialog` + `FcFormAbandonmentGuard`** | Destructive commands require explicit confirmation. Dirty-form navigation after `30s` of edits is guarded; cancel restores the form/focus. Dialog close returns focus to invoker; modal depth stays one. |
| **lifecycle-feedback — `FcLifecycleWrapper` + `FcPendingCommandSummary`** | Shows the exact lifecycle vocabulary and never calls transport acceptance confirmed success. Progress is polite; focused validation/rejection uses the error summary/alert path. FC-CNC permits one in-flight local command and blocks—never queues/batches—a later local submit with one accessible announcement. |
| **fresh-row-indicator — `FcNewItemIndicator`** | Publishes only from resolver-owned eligible terminal outcomes using immutable pre-dispatch target identity and Material disposition. Updates an already-rendered grid live, scopes before read/render, announces a newly material row at most once, and expires silently after the existing ten-second active window. |
| **customization-diagnostic — `FcCustomizationDiagnosticPanel`** | Development-only. Appears for override contract mismatch or render fault, gives bounded corrective guidance, and never leaks raw payload, tenant/user values, tokens, or stack traces. |

## State Patterns

### Shell, home, navigation, and account

| State | Entry evidence | Meaning and actions | Recovery / announcement |
|---|---|---|---|
| No Modules | Registry has no domain manifests | Valid empty shell; link to getting started | Terminal for current registry; route heading identifies Home |
| Hydrating | Manifests exist; capability hydration not started/resolved | Geometry-matched skeletons; shell remains operable | Non-terminal; busy state, no noisy per-card announcements |
| Partially Ready | Some Module counts ready, others loading | Ready Modules work; pending Modules remain skeletons | Non-terminal; one useful aggregate update, not one per count |
| Ready | Hydration seeded | Actionable Modules ordered first; zero-action Modules follow by name/secondary grouping | Stable until live count change; reordering preserves focus |
| Route failure | Client navigation cannot activate target | Current surface remains usable; failure status offers retry/back | Announce once; focus remains on stable heading/status |
| No Module access | Policy hides entry or direct route denies | Hidden when policy requires; otherwise support-safe denied surface | Terminal until auth changes; alert/heading is focus target |
| Missing/stale tenant | Tenant resolution absent, invalid, or changes under a live surface | Explicit blocked state; never render stale/empty-looking data | Clear prior scope before render; announce once as blocking |
| Signed out / signed in | Host auth state | Account menu offers challenge or sign-out | Sign-out evicts token state; navigation returns to a stable route |

Home is reachable through both `/` and `/home`. Settings preference hydration and all badge/home counts are tenant/user scoped; storage is skipped rather than defaulted when scope is absent.

### Supporting surface states

| Surface | Required states | Contract |
|---|---|---|
| Module workspace / Module Tab | Loading, Available, No Access, Missing Tenant, Unknown Tab | Loading preserves shell geometry; Available selects exactly one route-backed tab; denied/tenant failures are explicit; an unknown tab produces a route failure rather than silently selecting unrelated content. |
| Projection flyout | Closed, Open, No Projections, Authorization-filtered, Navigation Failure | Open is keyboard navigable; no-projection Modules retain their workspace/default tab; hidden projections are not teased; close/failure returns or preserves useful focus. |
| Command palette | Closed, Hydrating, Results, No Matches, Authorization-filtered, Navigation Failure | Search results update after `150ms`, announce useful result-count changes without per-keystroke noise, exclude unauthorized actions, and keep query/focus on a recoverable target after failure. |
| Settings | Hydrating, Ready, Dirty Preview, Persisted, Persistence Skipped/Failed | Preview does not commit. Confirm updates theme/density and announces once; missing scope skips storage without inventing a default tenant; failure preserves the selected value in-session and gives support-safe feedback. |
| Account control | Signed Out, Challenge Pending, Signed In, Sign-out Pending/Failed | Menu is always reachable. Challenge/sign-out use framework routes; sign-out invalidates/evicts token state; failure exposes no token or claim detail and leaves a stable retry route. |
| Projection row detail | Collapsed, Expanded, Hidden by Filter, Query/Permission Error | Expanded region is labelled; hiding moves/preserves focus at the grid and announces once; detail errors retain row context and do not expose backend internals. |
| Generated command form | Initial, Authorization Pending/Denied, Dirty, Invalid, Dispatching, Retryable Failure, Lifecycle Outcome | Only editable fields render; dirty navigation is guarded; invalid submission focuses the linked summary; dispatch locks the local lane; input survives meaningful correction/retry. |
| Customization diagnostics | Hidden, Mismatch, Render Fault | Hidden outside Development; visible states provide bounded corrective metadata and never replace the operator-facing projection with unsafe details. |
| MCP tool/resource/lifecycle surfaces | Visible, Hidden/Unknown, Schema Mismatch, Malformed, Canceled, Oversized, Lifecycle Pending/Terminal | Public shapes and disclosure boundaries are fixed by the MCP matrix below; hidden/unknown handling precedes schema details where specified. |
| Inspect / migrate / generated output | Success, Actionable Findings, Unavailable, Dry-run Plan, Apply Success, Refused Target, Drift | Machine and text results remain deterministic; unsafe writes and unapproved drift fail closed. |
| Adopter test harness | Configured Success, Rejection, Timeout/Stall, Authorization Denial, Query Variants, Assertion Failure | Fakes are deterministic; assertion/evidence output is redacted; a failing UX assertion remains failure evidence and is not replaced by another green lane. |

### Projection state-by-surface matrix

Each state is visible and accessible within the projection list/detail context. Data, Stale, Reconnecting, FallbackPolling, SlowQuery, and MaxItems can combine; the highest-consequence meaning is announced once without hiding usable data.

| State | Entry evidence | User-visible meaning / permitted action | Recovery and classification | Announcement |
|---|---|---|---|---|
| Loading | Initial or explicit query in flight with valid scope | Expected-layout skeleton; safe navigation remains available | Non-terminal; exits on data, empty, or error | Busy semantics; no repetitive speech |
| Empty | Successful scoped query has zero rows and no active filter mismatch | No records exist; authorized create CTA may appear | Terminal for query result; create, refresh, or navigate | Heading/status once |
| Data | Successful scoped query returns rows | Filter, sort, expand, page, run authorized actions | Stable until refresh/connection/query change | No bulk row announcement |
| Stale | Freshness evidence says displayed projection may be old | Keep readable data; show consequence; refresh/wait; mutations fail closed where current evidence is required | Non-terminal; exits on successful reconciliation | Polite once per projection/stale transition |
| Reconnecting | SignalR disconnected/retry active | Live updates unavailable; data may age; manual safe reads remain | Non-terminal; jittered exponential retries unbounded, delay capped at `30,000ms`; closed connection restart within `10s` | Announce entry once; retries silent |
| FallbackPolling | Realtime unavailable and fallback driver active | Data refreshes by HTTP while connection recovers | Non-terminal; every `15s` across at most `8` lanes; exits automatically on reconnect | Entry/recovery once; ticks silent |
| SlowQuery | Active query exceeds `2,000ms` | Query continues; user may wait, reset/refine filters, or navigate | Non-terminal; clears on completion/cancel/new query | Polite once per query |
| MaxItems | Unfiltered result reaches `10,000` cap | Result is bounded; refine filters; server virtualization applies from `500` rows | Stable constraint until query narrows | Once per query shape; no per-page repeats |
| Reconnected | Realtime resumes and reconciliation succeeds | Data refreshed; live mode restored | Transient notice for `3,000ms`, then silent | Polite once |
| Query/permission error | Scoped query fails or policy denies | Preserve safe context; no raw backend detail; retry only when allowed | Terminal for attempt; new auth/query/refresh may recover | Alert/focused summary when blocking |

Expanded row detail is a labelled region. If filtering removes its row, the hidden-expansion notice announces once, moves or preserves focus at a valid grid control, and never implies deletion.

### Command authorization, validation, and lifecycle

Authorization states are Pending, Authorized, and NotAuthorized. Pending exposes no enabled protected command; Authorized allows form interaction; NotAuthorized supplies support-safe denial and no dispatch path.

Client validation runs before dispatch. A failed submit focuses a linked error summary; each item navigates to its Fluent input, useful values remain, and the first invalid field is keyboard reachable. An asynchronous server Rejected outcome remains lifecycle feedback unless a support-safe field mapping exists.

| Lifecycle state | Entry evidence and meaning | Permitted action / recovery | Announcement / classification |
|---|---|---|---|
| Submitting | Valid authorized command is dispatching; not yet accepted | Wait/cancel only where contract permits; second local submit blocked | Polite once; non-terminal |
| Acknowledged | HTTP/EventStore accepted the command | Wait for status/projection evidence | Polite once; non-terminal; never success |
| Syncing | Accepted command awaits confirmed status/projection | Wait; polling continues; start-over only where provided | Coalesced polite progress; non-terminal |
| Confirmed | Approved status/projection evidence proves outcome | Return to refreshed list/detail | Polite once; terminal success |
| Rejected | Structured domain rejection | Review support-safe reason; correct/retry when meaningful | Focused alert/error summary once; terminal |
| IdempotentConfirmed | Evidence proves requested material result was already confirmed | No repeat dispatch required | Polite once; terminal success; fresh marker only if terminal materiality is Material |
| NeedsReview | Outcome requires human review; not confirmed success | Follow named support/review action when supplied | Once; terminal for automated lifecycle |
| Warning | Outcome succeeded or progressed with a qualifying warning; not equivalent to Confirmed unless evidence says so | Follow support-safe action; retain context | Once; terminal/non-terminal only as supplied by lifecycle evidence |
| Degraded | Confirmation exceeds `10,000ms` or retryable dispatch path exhausts its budget | Continue status polling, start over where safe, or follow support guidance | Once on entry; non-terminal until terminal evidence or `120,000ms` polling ceiling |

Lifecycle polling uses the confirmed status endpoint every `1,000ms` for at most `120,000ms`. The lifecycle coordinator has zero pre-accept lifecycle retries. Separately, the transient **dispatch** retry is exactly once after `250ms` with the same `MessageId`; it is not a lifecycle-poll retry. FC-CNC keeps exactly one in-flight local command; later submits do not run.

### Fresh-row state

The resolver is the only terminal owner. Target identity is an immutable pre-dispatch snapshot from explicit command-to-projection metadata plus a typed target provider or declared SameAsSource. Terminal materiality is independently Material, NoOp, or Unknown.

Publish only when both identity and Material disposition are known and the outcome is eligible. Suppress Unknown identity/materiality, NoOp, delete, Rejected, NeedsReview, and server-allocated target keys. A material idempotent confirmation remains eligible. SignalR nudges, visible-row diffs, EventStore aggregate IDs, and untyped results are never row identity.

Indicator state is tenant/user/lane scoped before read and render. Add, materialization, dismiss, filter/requery, expiry, clear, and scope change invalidate live generated consumers. Active identity is `(ViewKey, EntityKey)` with atomic first-wins across message IDs: later outcomes cannot replace provenance or extend expiry.

### MCP state and disclosure matrix

| Request | Public result | Behavioral guarantee |
|---|---|---|
| `tools/list` without auth/tenant or on catalog failure | Successful empty tools collection | Fail closed; empty remains a credential-validity signal because authenticated callers see at least the lifecycle tool |
| `tools/call` unknown/hidden/unauthorized/tenant-less/policy-denied | `category: unknown_tool`, `docsCode: HFC-MCP-UNKNOWN-TOOL`, suggestion, caller-visible tools, optional truncation continuation; text `Request failed.` | Hidden and absent are byte-identical for the same caller |
| Visible tool with incompatible schema | Distinct schema-mismatch result; no side effect | Exact, CompatibleAdditive, and CompatibleWarning may allow side effects; every other negotiation kind blocks |
| `resources/list` | Static generated projection/skill descriptor catalog | Same catalog for all callers; bounded-context/projection names are disclosed pending the independent security disposition |
| Registered projection resource hidden/unauthorized/tenant-less/policy-denied | `unknown_resource` | Those registered-resource causes are indistinguishable |
| Unregistered resource URI | ModelContextProtocol SDK default not-found | Distinguishable from registered hidden resources by stated contract |
| Visible projection with incompatible schema | Distinct schema result | No read side effect under incompatible negotiation |
| Skill resource failure | `unknown_resource`, `malformed_request`, `canceled`, or `response_too_large` | Skills are framework-global validated reference content, not tenant data |

The fixed lifecycle tool crosses requests through singleton state with a scoped tracker. Logs and responses never echo requested hidden tool names, fingerprints, tenant identifiers, raw exception text, tokens, or payloads.

### Developer/tooling states

| Surface | States and recovery |
|---|---|
| Bootstrap | Valid three-call order starts; missing/misordered registration fails at startup with the named missing stage; no first-render failure |
| Generated output | Valid annotated types emit deterministic artifacts; invalid use emits governed HFC diagnostics; generated files are never hand-edited |
| Inspect | Text/JSON success, warning/error filtered result, actionable-findings exit, or unavailable-output exit; paths are repository-relative/redacted |
| Migrate | Dry-run plan by default; apply is atomic; generated, submodule, symlink, out-of-root, `bin`, `obj`, and `.git` targets fail closed |
| Customization | Resolution order is full-view override, projection template, generated default; field slots participate only when the selected renderer delegates; development-only mismatch panel |
| Testing | Deterministic success, rejection, timeout/stall, authorization denial, paging/filter/sort, focus/accessibility, and redacted evidence states |

## Interaction Primitives

- `Ctrl+K` opens the command palette; `Ctrl+,` opens settings; `/` focuses page search only where enabled.
- `Esc` closes the topmost menu, popover, palette, or dialog and restores focus to its invoker.
- Shell/module navigation activates a real route and focuses its `h1`. A navigation failure preserves a stable focus target and announces once.
- Module tabs follow inherited arrow-key selection. Focus remains on the active tab while its labelled tabpanel changes.
- Projection grids support keyboard filtering, sorting, row expansion, paging/virtualization, and authorized row actions. No action is hover-only.
- Palette and column filtering are debounced; only the palette delay is fixed here (`150ms`).
- One modal layer at a time. Prefer route, tab, detail, and full-page command surfaces over nested dialogs.
- A destructive confirmation requires an explicit confirm action. Cancel/close returns focus and causes no dispatch.
- A second local command while one is in flight is blocked, never queued, batched, or raced.
- Meaningful status transitions announce once, deduplicated by operation/entity and state. Coalesce rapid intermediate progress; suppress poll/retry ticks and silent expiry.
- Banned: primary-nav explosion, false success on HTTP acceptance, row freshness inferred from nudges/diffs, hover-only meaning, custom module themes, raw interactive controls where FrontComposer/Fluent equivalents exist, and rich AuditTimeline/ConsequencePreview interactions in v1.

## Accessibility Floor

Behavioral accessibility is mandatory; visual contrast/focus styling lives in `DESIGN.md`.

- Meet WCAG 2.2 AA for generated and hand-authored shell UI.
- Preserve one main landmark, skip links to content, unique route `h1`, semantic navigation, labelled tablist/tab/tabpanel relationships, combobox/listbox semantics for palette, dialog semantics, grid semantics, and `role="region"` for row detail.
- Every interactive element has an accessible name. Status icons have `aria-label` and focusable tooltip access; count badges include textual context.
- Focus order follows reading order. Route/tab/palette/dialog behavior is deterministic; focus never lands in removed, hidden, or obscured content.
- Progress and non-blocking transitions use a polite status channel. Submit-blocking validation and Rejected outcomes use the focused linked error summary/alert path. Each meaningful transition is announced once.
- At `320 CSS px` reflow and `400%` zoom, meaning and operation remain available without two-dimensional scrolling except content with an essential two-dimensional layout.
- Text remains operable when tested with WCAG text-spacing overrides: line height `1.5` times font size, paragraph spacing `2` times font size, letter spacing `0.12` times font size, and word spacing `0.16` times font size.
- Pointer targets meet WCAG 2.2 AA `24×24 CSS px` minimum or a documented exception; keyboard operation remains equivalent.
- Focus is not entirely hidden by sticky headers, dialogs, notices, or other authored content.
- Reduced motion removes pulses/transitions without removing timing, state, or fresh-row meaning. Forced colors preserves borders, focus, semantic icons/text, and fresh-row meaning without color dependence.
- Stable `data-testid` selectors remain on behavior governed by bUnit/e2e evidence; selectors supplement rather than replace accessible semantics.

## Security, Tenancy, and Support Safety

- Resolve tenant/user scope before every projection query, subscription, home/count read, preference read/write, pending state read, or fresh-row render. A stale tenant blocks its SignalR group; there is no default tenant.
- Clear old scope before the next scope can render. Storage keys are tenant/user/feature scoped, and persistence is skipped when scope is unavailable.
- Server-controlled command fields—TenantId, UserId, MessageId, CorrelationId, timestamps, and declared derived values—never appear as editable input and are injected server-side.
- Authorization runs both in the visible command region and at the service boundary. A hidden UI control is not authorization.
- MCP endpoints sit behind host authentication and require both tenant-tool and resource-visibility gates. Allow-all gates are development/sample-only and remain a readiness enforcement gap, not accepted production behavior.
- UI, MCP, logs, telemetry, snapshots, and evidence never expose raw tokens, JWT payloads, EventStore metadata, raw event payloads, stack traces, unrestricted PII, or hidden resource/tool identifiers.

## Responsive & Platform

| Semantic viewport | Behavior |
|---|---|
| Desktop | Persistent primary Module rail; hamburger toggles `{spacing.navigation-rail-labeled}` labelled and `{spacing.navigation-rail-icon-only}` icon-only modes; full-width data surfaces by default |
| Compact | Rail may remain icon-only or move behind the shell's compact navigation control; Module Tabs, palette, settings, and row actions remain keyboard/touch reachable |
| Narrow browser | Drawer-style navigation may present the same one-entry-per-Module list; content reflows to one logical reading order; tables retain an accessible bounded horizontal strategy only where essential |

[NOTE FOR UX] The sources define semantic behavior but no numeric breakpoints. Implementations must use the shared breakpoint watcher and must not invent product-contract widths in this spine.

## Inspiration & Anti-patterns

- **Lifted from .NET Aspire Dashboard:** neutral chrome, accent as a thread, compact data density, sticky headers, disciplined toolbar/search, and lightweight status affordances—translated to Fluent UI V5 rather than copied from legacy V4/FAST tokens.
- **Lifted from FrontComposer brownfield contracts:** registry-driven composition, generated projection/command surfaces, lifecycle truth, fail-closed tenancy, and Fluent governance.
- **Rejected—primary navigation explosion:** projections and commands belong under one Module entry.
- **Rejected—false success:** acknowledgement is not projection/status confirmation.
- **Rejected—bespoke module themes and raw controls:** modules inherit the common Fluent system.
- **Rejected—ambient fresh-row inference:** nudges, diffs, aggregate IDs, and untyped results cannot identify a row.
- **Rejected for v1—rich AuditTimeline and ConsequencePreview:** lifecycle wrapper and destructive-confirmation behavior remain the approved fallback.

## Key Flows

### UJ-1. Nina boots a domain shell from annotated types.

1. Nina, an adopter developer adding an operations UI to Hexalith.Tenants, annotates projection and command types.
2. She calls `AddHexalithFrontComposerQuickstart()`, `AddHexalithDomain<TMarker>()`, and `AddHexalithEventStore(...)` in the documented order.
3. She wraps the app body in `<FrontComposerShell>@Body</FrontComposerShell>`.
4. The source generator emits deterministic registrations, projection/command artifacts, and manifests; startup validation verifies the bootstrap stages.
5. The shell opens at Home with its account control, shortcuts, one Tenants Module entry, and a useful empty/progressive state even before domain data exists.
6. **Climax:** Nina enters Tenants through its default Module Tab and sees both a generated projection and generated command reachable without bespoke framework plumbing—the candidate-bound G-6 proof target.

Failure: a missing or misordered bootstrap call stops startup with the named missing stage. Nina corrects registration before any first-render failure; an empty domain registry remains valid and is not an error.

### UJ-2. Marc investigates a live projection.

1. Marc, an authenticated support operator, opens `/home` in the desktop web shell.
2. Home presents Modules ready first, then by descending actionable count, then by ordinal name.
3. He activates Tenants, landing on `/{module}`, the alias of its required default `/{module}/{default}` Module Tab.
4. He uses the secondary projection flyout to select another `/{module}/{tab}` without creating another top-level active entry.
5. The projection list progresses through Loading to Data; Marc filters the grid, whose `32px` compact rows, sticky header, status affordances, and column priority preserve scanability.
6. He expands a labelled row-detail region and checks projection health. SlowQuery at `2,000ms`, MaxItems at `10,000`, Stale, Reconnecting, or FallbackPolling remain explicit when applicable.
7. A row his own recent command materially changed appears through the scoped fresh-row contract on the already-rendered grid.
8. **Climax:** Marc identifies the relevant row and can state whether the read model is current, fallback-polled, stale, bounded, or reconnecting—without mistaking a nudge for proof.

Failure: tenant context is missing or goes stale. The prior scope clears before render and an explicit fail-closed state replaces data; Marc never sees an empty-looking grid that could be misread as “no records.”

### UJ-3. Marc executes a command safely.

1. Marc opens a generated command from an authorized row action, empty-state CTA, palette result, or direct `/commands/{BoundedContext}/{CommandTypeName}` route.
2. Authorization resolves Pending → Authorized; only editable fields appear in Inline, CompactInline, or FullPage density.
3. A destructive command requires confirmation. Client validation links a focused summary to invalid fields and preserves input.
4. Marc submits. FC-CNC makes this the only in-flight local command; a rapid second submit is blocked and announced as not run.
5. The lifecycle moves through Submitting, Acknowledged, and Syncing. Acknowledged explicitly means transport acceptance, not success.
6. The status poll supplies Confirmed, Rejected, IdempotentConfirmed, NeedsReview, Warning, or Degraded evidence; `10,000ms` produces Degraded while `1,000ms` polling may continue to `120,000ms`.
7. **Climax:** Confirmed projection/status evidence refreshes the row/list and the lifecycle becomes Confirmed; Marc trusts the visible outcome rather than the HTTP response.

Failure: Rejected focuses a support-safe error path and preserves correctable input. A transient dispatch fault may retry exactly once after `250ms` with the same `MessageId`; another failure degrades without a duplicate command.

### UJ-4. Ravi exposes the domain surface to an AI agent.

1. Ravi, an AI-agent integrator, places the MCP endpoint behind host authentication.
2. He registers the tenant-tool and resource-visibility gates and excludes allow-all gates from the non-Development host.
3. The agent calls `tools/list` and sees only its visible generated commands plus `frontcomposer.lifecycle.subscribe`; it reads the disclosed static resource catalog with the documented boundary.
4. The agent calls a command after admission, schema negotiation, argument validation, and server-side controlled-field injection.
5. The server returns a bounded acknowledgement; the agent polls lifecycle in a later request/DI scope.
6. **Climax:** the agent observes a real terminal lifecycle snapshot without receiving tenant internals or gaining a side effect through an incompatible schema.

Failure: unknown, hidden, unauthorized, or tenant-less tool calls collapse to the same `unknown_tool` shape for that caller. Registered hidden resource reads return `unknown_resource`; unregistered URIs retain the stated SDK not-found distinction. No response/log echoes the requested secret surface.

### UJ-5. Camille preserves generator/runtime compatibility.

1. Camille, a framework maintainer, changes a generator contract in annotated-source handling or emission.
2. She updates the governed HFC diagnostic behavior, generated-output snapshots, schema fingerprints, and public API baselines intentionally.
3. She runs `frontcomposer inspect` to compare generated files, forms, grids, registrations, manifests, warnings, and errors in deterministic text/JSON forms.
4. Where consumers need an upgrade, she adds an allowlisted `frontcomposer migrate` edge with dry-run preview and atomic, path-safe apply.
5. She validates Contracts/Contracts.UI boundaries and the public generated-output path without editing `obj` output.
6. **Climax:** drift gates detect exactly the intended change and consumers receive a named diagnostic and migration path instead of a silent runtime mismatch.

Failure: accidental structural/metadata drift, invalid migration target, public API mismatch, or unsupported schema blocks the lane. Camille changes the source generator/contract or migration plan; she never edits generated files or bypasses the baseline.

### UJ-6. Sophie tests a generated consumer experience.

1. Sophie, a test engineer proving the Tenants adopter, creates a bUnit host from the FrontComposer Testing package.
2. She configures deterministic command success, rejection, timeout/stall, authorization denial, and query paging/filter/sort scenarios.
3. She asserts bootstrap and projection states, linked validation, lifecycle truth, blocked-submit feedback, and fresh-row scope/first-wins behavior.
4. She verifies route/tab/palette/dialog focus, keyboard-only recovery, deduplicated live announcements, silent expiry, `320 CSS px` reflow, `400%` zoom, text spacing, target size, unobscured focus, forced colors, and reduced motion.
5. Evidence recorders redact tenant/user values, secrets, tokens, raw paths, payloads, and stack traces.
6. **Climax:** Sophie produces candidate-bound proof that Tenants boots and renders a generated projection and command while realistic failure UX remains testable and accessible.

Failure: a regression or unredacted value fails the focused/default lane. The evidence names the behavior and candidate; a green unrelated lane cannot substitute, and Parties cannot replace Tenants without a dated D-7 Product decision and equivalent proof.

## Open Questions

- [NOTE FOR UX] Exact visual treatment and final microcopy remain unresolved for Warning, NeedsReview, Degraded, missing tenant, FallbackPolling, SlowQuery, MaxItems, and fresh-row dismissal/forced-colors states.
- [NOTE FOR UX] Numeric responsive breakpoints remain unresolved; only semantic Desktop, Compact, and Narrow-browser behavior is committed.
- [NOTE FOR UX] Product/Architecture must confirm the selected Fluent V5 pin and exact supported token/API names before final handoff.
- The spine remains `draft` until the opt-in reviewer gate runs or is explicitly skipped and these source-owned gaps receive a disposition.
