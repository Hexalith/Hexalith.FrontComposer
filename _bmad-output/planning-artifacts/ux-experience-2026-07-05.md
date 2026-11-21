---
name: Hexalith Common Application UX
status: accepted-supplement
product_approval: pending-reapproval
updated: 2026-11-21
reconciliation_revision: prd-and-architecture-2026-11-21
sources:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/prd-addendum-2026-09-08.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-design.md
  - _bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-nav-single-active-item.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-tenants-ui-menu-icon-label-stack.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05.md
---

# Hexalith Common Application Experience

This behavior/journey supplement owns how the FrontComposer experience works. The D-8 canonical UX
authority is `_bmad-output/planning-artifacts/ux-design.md`; the visual identity reference is
`ux-design-detailed-2026-07-05.md`. The canonical file wins any conflict with this supplement,
mockups, wireframes, imports, or historical decisions. This reconciliation does not close OI-16,
OI-19, or G-4 and does not record Product approval.

## Foundation

FrontComposer is both a responsive web product for developers and an operations shell. Primary daily
use is desktop web; 320 CSS-pixel reflow and 400% zoom are required functional presentations, not a
separate mobile-native product. The UI system is **FrontComposer + Blazor Fluent UI V5**. FrontComposer owns
public composition and behavioral deltas; Fluent V5 owns inherited component behavior unless this
contract states a stricter outcome.

Audience is mixed: adopter developers, authenticated operators, AI-agent integrators, framework
maintainers, and release/test owners. Human-facing shell copy stays professional, support-safe, and
task-focused. It does not depend on bespoke host CSS or module-specific themes.

## Information Architecture

| Surface | Reached from | Purpose | Required state family |
| --- | --- | --- | --- |
| Application shell | App open / authenticated route | Global frame, skip link, account access, shortcuts, content, footer | Bootstrap, no registrations, no tenant, no access, route failure |
| Home Module directory | Shell default route | Urgency-ordered visible Modules | Loading, Empty, Data, no tenant/access |
| Module workspace | Module entry or deep link | One bounded-context workspace and its required default Module Tab | Loading, default/selected tab, no access, invalid route fallback |
| Module tabs | Module workspace / projection flyout | Deep-linkable page-body views at `/{module}/{tab}` | Selected, disabled, invalid/default fallback, focus retained |
| Projection grid/detail | Module Tab | Filter, sort, inspect status and row detail, launch allowed commands | All FR-11 states, filter-no-results, hidden expanded detail, fresh row |
| Command form | CTA, row action, or `/commands/{BoundedContext}/{CommandTypeName}` | Edit allowed fields, validate, confirm, submit safely | Ready, validation blocked, authorization denied, abandonment, blocked second submit |
| Command lifecycle | Valid submit | Separate transport, synchronization, terminal, and degraded meaning | Every FR-15 state in UX-SS-1 |
| Compatibility MCP host | Authenticated agent client | Discover permitted tools, read resources under FR-19 and poll an admitted command | Empty tool list, opaque denial, visible schema mismatch, acknowledged or terminal result (UJ-4) |
| Local compiler CLI | Developer runs `frontcomposer inspect` or `frontcomposer migrate` | Inspect generated output and preview or apply an allowlisted migration | On-disk output, dry run, per-file apply, refusal or partial write (UJ-5) |
| Adopter test host | Consumer bUnit test project | Exercise generated UI with deterministic fakes | Success, denial, rejection, timeout and paged or filtered data (UJ-6) |
| Palette/dialog overlay | Shortcut or invoking action | Search/navigate/configure/confirm without losing context | Open, no results, denied/failure, close/cancel, removed origin fallback |

Each Module has one primary shell entry. The default tab uses the Module plural label, falling back to
`Overview`. Bare `/{module}` selects the default silently. An explicitly requested unknown or disabled tab falls back to the default with AM-31. Projection flyouts remain secondary and route to
`/{module}/{tab}`. Generated command routes remain `/commands/{BoundedContext}/{CommandTypeName}`.

## Voice and Tone

Microcopy reports observable truth. Brand posture and visual contrast live in the detailed supplement.
All message templates are localized and exclude tokens, JWT contents, policy names, tenant/user IDs,
raw EventStore metadata, stack traces, event payloads, and unrestricted PII.

| Situation | Use | Do not use |
| --- | --- | --- |
| Transport accepted | “Command accepted. Waiting for confirmation.” | “Saved successfully.” |
| Syncing | “Command accepted. Updating the view.” | “Done.” |
| Rejected | “Command rejected. Review the message and try again.” | Raw exception or payload text |
| Blocked second submit | “This command did not run. Another command is already in progress.” | “Queued” or “Will run next.” |
| Stale/reconnecting | “Data may be out of date.” / “Connection lost. Reconnecting.” | “Realtime error.” |
| No tenant/access | “A tenant is required to view this Module.” / “You do not have access to this Module.” | Internal policy or tenant details |
| Empty/filter empty | “No {items} are available.” / “No {items} match these filters.” | “Nothing here!” |

UX-AM-1 defines the announcement templates and channels. AM-13 defaults to “Command confirmed.” for both confirmation identities. A nonblank `FcLifecycleWrapper.IdempotentInfoMessage` replaces the spoken message for an idempotent outcome, once for the operation. Visible copy can add a safe
domain noun or recovery action but cannot weaken the stated meaning.

## Component Patterns

These identifiers match the detailed supplement exactly. Visual rules live in its Components section.

| Component | Behavioral contract |
| --- | --- |
| `FrontComposerShell` | The shell renders the hamburger before `HeaderStart` and the account menu after header actions. It coordinates shortcuts and route focus. Navigation follows the current scope and layout. |
| `FrontComposerNavigation` | Consumes shell/caller navigation state and targets exactly one current Module; labelled/icon-only modes retain accessible names and current state; projection flyouts route within the Module. The shell, not this component, owns the default hamburger. |
| `FcPageTabs` | Presents a labelled page-body tab list, forwards caller-owned `ActiveTabId`/`ActiveTabIdChanged` state into keyboard selection, and associates one panel; successful keyboard selection follows FM-02. |
| `FcPageTab` | Owns its real panel content and reciprocal tab/panel relationship; disabled tabs are skipped by keyboard selection. |
| `FcPageToolbar` | Public search/filter/view/overflow/action contract; internal Fluent composition is implementation-owned. The `/` shortcut follows FM-12 when the active route has exactly one enabled page search. |
| `FcCommandPalette` | `Ctrl+K` opens; authorization-aware results debounce at 150 ms; navigation follows FM-01; close follows FM-04; no-results keeps a usable query/close path. |
| `FcSettingsDialog` | `Ctrl+,` opens Settings silently. The saved density and its preview can differ from the applied density. Phone and Tablet force Comfortable. After hydration, a later effective density change is announced once. Entry, containment, Escape and return follow UX-OF-1. |
| `FcDestructiveConfirmationDialog` | Requires explicit confirmation; UX-OF-1 focuses Cancel, traps within the modal, makes Escape cancel, and returns by FM-05. |
| `FcFormAbandonmentGuard` | After 30 seconds of edits, intercepted navigation exposes an in-flow warning; OF-04 focuses “Stay on form,” Escape stays, and “Leave anyway” is explicit. It is not a modal dialog. |
| `FcLifecycleWrapper` | Presents each FR-15 state without converting Acknowledged into Confirmed; follows UX-AM-1 and UX-SS-1. |
| `FcProjectionLoadingSkeleton` | Represents layout while AM-01 provides semantic loading meaning; repeated animation is disabled under reduced motion. |
| `FcProjectionEmptyPlaceholder` | Distinguishes true Empty from filter-no-results and exposes only an authorized valid CTA. |
| `FcProjectionConnectionStatus` | Represents Stale, Reconnecting, FallbackPolling, and recovery; polling/backoff ticks remain silent. |
| `FcPendingCommandSummary` | Keeps bounded pending/rejected operations visible with state and recovery; never merges separate operation identities. |
| `FcNewItemIndicator` | Shows one cue per eligible entry above the grid. Default visible and spoken text names `EntityKey`. A nonblank `Text` overrides only the visible label. Materialization, filter dismissal, scope change and expiry remove the cue silently. DW-679 rows receive no cue. |
| `FluentAccordion` (inherited) | Groups two or more sibling titled sections; the primary item starts expanded; a lone primary region is not hidden. |
| `FluentBadge` (inherited) | Carries numeric counts only; semantic status uses persistent text/icon/accessible state. |
| `FluentTooltip` (inherited) | Repeats a concise label on hover and focus; never supplies the only action or state meaning. |

## State Patterns

Canonical UX-SS-1 contains the required entry evidence, visible meaning, actions, recovery/timeout,
announcement, terminality, and baseline/open-delta status. The shell, projection, command and overlay surfaces map to the rows below. UJ-4–UJ-6 cover the nonvisual surfaces. The
index adds the focus/recovery join without redefining the canonical state contract.

| UX-SS-1 rows | Surface/state coverage | Focus/recovery join |
| --- | --- | --- |
| SS-01 | Application shell / no registrations | Neutral-state heading; account/settings remain reachable; AM-25 announces once |
| SS-02–SS-04 | Home directory / Loading, Empty, Data | Directory heading remains stable; results do not reset focus without operator action |
| SS-05–SS-06 | No tenant / no access | FM-10; fail closed without empty-looking data |
| SS-07–SS-15 | Projection / Loading, Empty, Data, Stale, Reconnecting, FallbackPolling, SlowQuery, MaxItems, filter-no-results | Grid/detail focus stays usable; recovery controls follow each state message; hidden expanded detail uses AM-22 |
| SS-16 | Submitting | Focus remains in form/lifecycle context; duplicate activation routes to SS-25 |
| SS-17 | Acknowledged | Transport truth only; no focus move or Confirmed copy |
| SS-18 | Syncing | Focus remains stable while intermediate progress is coalesced |
| SS-19 | Confirmed | Terminal action and affected-surface route are keyboard reachable |
| SS-20 | Rejected | VR-02 or VR-03; safe edit/retry/cancel path |
| SS-21 | IdempotentConfirmed | Shared default AM-13 with distinct idempotent identity. A nonblank idempotent message replaces the default terminal announcement |
| SS-22 | NeedsReview | Review path follows the state explanation; no false success |
| SS-23 | Warning | Details and close/support path are keyboard reachable |
| SS-24 | Degraded before deadline | The result is unconfirmed. Status/review/continue actions remain available. Checks run only when polling is enabled |
| SS-25 | Blocked second submit | VR-05 and FM-09; only the original lifecycle can advance |
| SS-26 | Degraded at deadline | If unresolved, AM-24 closes the local lifecycle at the configured duration from acknowledgement (default 120,000 ms), even with polling off. Later status is separately correlated |
| SS-27–SS-29 | Shell Bootstrap, startup failure, route failure | FM-01/FM-10; AM-01, AM-29, or AM-23 supplies the single context path |
| SS-30–SS-33 | Module workspace/tab Loading, selected/default, invalid fallback, disabled | FM-01/FM-02; valid selection is silent, fallback uses AM-31, denied route uses AM-26 |
| SS-34–SS-35 | Projection query failure / offline | Safe retry/navigation and AM-30; never substitute an Empty state |
| SS-36–SS-39 | Command form Ready, validation blocked, authorization denied, abandonment decision | UX-VR-1 plus OF-04/FM-11; silent context or one focus-only summary/heading/warning path |
| SS-40–SS-42 | Command palette Open, no results, denied/navigation failure | UX-OF-1; AM-28, AM-26, or AM-23 as applicable |
| SS-43–SS-47 | Settings/destructive overlays, in-flow abandonment warning, close/cancel, removed origin | OF-02–OF-04 and FM-05/FM-11; correct entry, modal containment only where applicable, and deterministic fallback |
| SS-48–SS-49 | Fresh-row appearance/expiry and filter-hidden expanded detail | AM-21 appearance with silent expiry; AM-22 when filtering hides detail |

## Announcement Behavior

UX-AM-1 is normative. Implementations use one polite status channel per surface for progress and
meaningful state changes, and one focus-only summary/heading path with no live attributes for blocking
errors and replacement states. Event-dedupe keys include state/result identity and suppress repeated
renders. The state-free coalescing group keys are the lifecycle operation, surface + connection epoch,
surface + operator-initiated load/filter operation, and palette session. Non-terminal changes within a
group restart a trailing 250 ms timer; stale async results are discarded and the last eligible message
wins. Terminal, focused-summary, and navigation-failure messages cancel the group's pending message
and announce immediately. Fake-time checks assert the 249/250 ms boundary, a cross-state sequence, and
the exact count. Fresh-row appearance uses the shared projection status region. AM-21 names the `EntityKey` once per tenant, user, `EntityKey` and `MessageId` across views during the active ten-second window. It runs independently of connection or query progress. Retry/backoff/poll ticks,
same-state renders, and fresh-row expiry are silent. A focus move and live region never announce the
same message twice.

## Validation And Rejection Behavior

| UX-VR-1 row | Distinction | Required behavior |
| --- | --- | --- |
| VR-01 | Client validation | Declared group name/description/order, stable field/error targets, Fluent input relationship, linked summary in declared DOM order, focus-only summary, first-invalid navigation, input preservation |
| VR-02 | Safely mapped server rejection | Remains `Rejected` while adding safe field links; one focused summary/announcement path |
| VR-03 | Unmapped server rejection | Lifecycle outcome only; safe reason and keyboard edit/retry/return path; no fabricated field error |
| VR-04 | Authorization denial | Replacement state, no invalid-field semantics or policy leakage |
| VR-05 | Blocked second submit | No dispatch/queue, no validation errors, original lifecycle visible, attempted-control focus retained |
| VR-06 | Abandonment | In-flow warning; Stay/Escape preserves input and restores the edited origin; Leave is explicit; no modal trap |

## Interaction Primitives

- Shell navigation opens Module workspaces; `FcPageTabs`/`FcPageTab` switch the Module's page-body
  views. Hover-only affordances are forbidden.
- `Ctrl+K` opens the command palette. `Ctrl+,` opens settings. The `/` shortcut focuses the
  active route's page search only when exactly one enabled `FcPageToolbar` page-search input exists;
  otherwise focus and input values stay in place.
- Shortcuts do not fire while focus is in an editable control, during IME composition, or when a more
  specific component owns the chord. Menus/settings expose the shortcuts; every action has a visible,
  keyboard-operable alternative that works despite browser, OS, or localized-keyboard-layout
  conflicts.
- `Esc` closes only the topmost dismissible overlay and returns focus by UX-FM-1. Destructive
  confirmation does not treat Escape as confirmation. On the in-flow abandonment warning, Escape
  means Stay, hides the warning, preserves input, and returns by FM-11.
- Tab/Shift+Tab order follows reading order. Arrow/Home/End behavior stays with the inherited tab,
  menu, grid, and radio patterns rather than creating global shortcuts.
- Modal stacks remain one level deep. Prefer route, tab, detail, or command surfaces over nested dialogs.

### Focus Behavior

| UX-FM-1 row | Trigger | Testable outcome |
| --- | --- | --- |
| FM-01 | Route/deep-link/CTA/palette navigation | Success focuses unobscured route `h1`; failure retains usable focus and uses AM-23 |
| FM-02 | Keyboard tab selection | Selected tab retains focus; labelled panel changes and active tab stays visible |
| FM-03–FM-04 | Palette open/close | Query receives focus; close restores invoker or route-heading fallback, never `body` when a target exists |
| FM-05 | Settings/destructive dialog close/cancel | Captured origin; if removed, disabled, or disconnected, current route `h1` |
| FM-06–FM-07 | Failed submit and summary navigation | Summary receives focus; link/first-error action focuses the linked invalid input |
| FM-08 | Unmapped server rejection | Do not steal focus solely for a polite update; recovery action is next in tab order |
| FM-09 | Blocked second submit | Attempted control retains focus; explicit action may focus original lifecycle |
| FM-10 | Tenant/access replacement | Replacement heading, then recovery; return navigation lands on route `h1` |
| FM-11 | In-flow abandonment warning opens/closes | “Stay on form,” then captured edited control after Stay/Escape; removed-origin fallback is the form heading |
| FM-12 | `/` page-search shortcut | One enabled active-route `FcPageToolbar` search receives focus; missing/disabled/ambiguous search is a silent no-op |

UX-OF-1 completes deterministic surface entry: OF-01 palette starts on its query, OF-02 settings on
its heading, OF-03 destructive confirmation on Cancel, and OF-04 in-flow abandonment warning on “Stay
on form.” Modal containment applies only to OF-02/OF-03; OF-04 stays in page order. The matrix also
owns accessible name/description sources, submit/error destinations, Escape, captured shortcut origin,
and the FM-04/FM-05/FM-11 return fallbacks.

## Accessibility Floor

- WCAG 2.2 AA applies to generated and hand-authored FrontComposer UI.
- Accessible names, roles, values, relationships, keyboard operation, and logical focus order are
  mandatory; stable `data-testid` values remain where they are part of the evidence contract.
- Status and lifecycle meaning never depends on color, motion, hover, tooltip, or animation alone.
- UX-AM-1, UX-VR-1, UX-FM-1, UX-OF-1, and UX-SS-1 are behavioral acceptance authorities.
- Canonical UX-AE-1 and detailed UX-VC-1, UX-TS-1, UX-RF-1, and UX-RM-1 define measurable reflow,
  zoom, text-spacing, target-size, focus-obscuration, contrast, forced-colors, and reduced-motion proof.
- Automated axe results are supporting evidence, not a substitute for product assertions or the manual
  screen-reader/device evidence required by `docs/accessibility-verification/README.md`.
- Support-safe copy and evidence never include bearer tokens, decoded JWTs, policy internals, raw
  EventStore metadata, stack traces, event payloads, or unrestricted PII.

## Responsive & Platform

| Tier / condition | Required behavior | Evidence join |
| --- | --- | --- |
| Desktop (1366 CSS px and wider) | Labelled or icon-only `FrontComposerNavigation`; one current Module; tabs and full data operations | UX-RF-1 plus light/dark/forced-colors screenshots |
| Compact desktop (1024 to below 1366 CSS px) | 48 px icon rail with accessible labels. Toolbars and tabs wrap or own their scroll. | Keyboard/focus-order and target-size assertions |
| Tablet (768 to below 1024 CSS px) and Phone (below 768 CSS px) | The drawer must expose each authorized Module once. Effective density is Comfortable. | Cold entry and desktop resize: open by keyboard, activate the Module, verify the default tab, heading focus and current item. The Counter fixture gap is recorded in UX-DR3. |
| 320 CSS-pixel reflow | Drawer/navigation, content, toolbar, forms, and overlays retain every operation; no page-level horizontal scroll; labelled grids and the tab strip may each own bounded horizontal scroll while selected tab/focus remain visible | UX-AE-1 AE-01 per changed surface |
| 400% zoom | Same functional and reading-order outcome as 320 CSS pixels | UX-AE-1 AE-02 per changed surface |
| Text-spacing override | Content-driven height; no clipping/overlap/loss; compact grid rows expand | UX-TS-1 + AE-03 |
| Forced colors | System text/border/icon/current/focus cues preserve every state | UX-VC-1 + AE-07 |
| Reduced motion | Decorative transitions/pulse/smooth scrolling stop; semantics/focus/announcements remain | UX-RM-1 + AE-08 |

This contract does not define a mobile-native product. Tablet and phone-width browsers remain
functional evidence tiers and must not be described as desktop-equivalent daily-use design without a
separate Product decision.

[Responsive navigation reference](ux-designs/ux-frontcomposer-2026-09-09/mockups/key-responsive-navigation.html)

## Inspiration & Anti-patterns

- **Lifted from .NET Aspire Dashboard:** neutral chrome, accent as thread, compact data density, sticky
  grid headers, toolbar/search discipline, and lightweight status icon-plus-text treatment.
- **Lifted from FrontComposer brownfield policy:** registry-driven composition, generated command and
  projection surfaces, lifecycle truth, and Fluent V5 governance.
- **Rejected: primary-navigation explosion.** Projections, commands, and subpages stay within a Module.
- **Rejected: custom module themes.** Modules do not define their own palettes, typography, or raw
  control styling.
- **Rejected: false success.** Transport acceptance and SignalR nudges are never Confirmed evidence.
- **Rejected: visual-only state.** Color, motion, hover, tooltip, shimmer, or pulse cannot carry meaning.

## Brownfield Reconciliation

- The one-entry-per-Module rule supersedes older primary-navigation patterns that exposed individual
  projections or subpages.
- FC-NIP Stories 9.1–9.8 remain delivery history; current behavior and residual DW-679 live in the PRD
  and canonical UX-DR5/UX-SS-1.
- **FC-IA-1 history:** Product/UX and Architecture recorded the Module Tab route/default/flyout decision
  on 2026-07-05 in `_bmad-output/contracts/fc-ia-1-module-tab-ia-decision-2026-07-05.md`. It supports the
  current IA but is not active authority and does not close D-9 or G-4 for this reconciliation.
- Historical first-pass UX review findings remain evidence. The dated OI-16 follow-up report determines
  whether the repaired documents have unresolved Critical/High findings; it does not establish SM-6
  implementation evidence or Product approval.

## Key Flows

The six journeys retain the PRD's names and scope. Product accepted Tenants as the first adopter under G-6 on September 26. The historical FC-NIP live proof has G-5 acceptance, and the compatibility host audit has candidate-specific G-7 acceptance. OI-16 and OI-19 remain open under G-4. [Product decisions](prd.md#01-v10-readiness-milestone-gates)

### UJ-1. Nina boots a domain shell from annotated types.

1. Nina, an adopter developer, annotates a projection and command and configures the documented
   three-call bootstrap.
2. She renders `FrontComposerShell` around the host body and starts the reference app.
3. The shell exposes skip target, account access, navigation, and a useful empty/loading state while
   generated registrations resolve.
4. Nina opens the generated Module and verifies its default Module Tab and generated surfaces.
5. **Climax:** the domain shell is operable without bespoke framework plumbing, and the visible routes
   and component names match the generated manifest.

Failure: missing or misordered bootstrap fails fast with a named developer-facing startup error; no
partially initialized operator shell claims success.

### UJ-2. Marc investigates a live projection.

1. Marc, an authenticated operator, opens the Home Module directory.
2. He selects a Module ordered by urgency and lands on its default Module Tab.
3. He follows a projection flyout to `/{module}/{tab}`, then filters and sorts the grid.
4. He expands a row detail while connection and query states remain explicit and non-noisy.
5. Marc sees one above-grid cue naming the row affected by his eligible command. It disappears silently when the row materializes, the filter dismisses it, its ten seconds expire, or he changes scope.
6. **Climax:** Marc identifies the affected row and can tell whether the read model is Data, Stale,
   Reconnecting, or FallbackPolling without relying on color or animation.

Failure: missing tenant context shows SS-05 rather than empty-looking data. A server-allocated-key
command covered by DW-679 produces no fresh marker. If fallback polling is disabled, a terminal hub close leaves the surface disconnected and skips the closed-restart loop. With polling enabled, the default interval is 15 seconds. If an HTTP 304 refers to an unreadable cached projection, the client evicts that scoped entry and retries once without the validator. [Host and runtime boundaries](architecture.md#host-and-runtime-boundaries)

### UJ-3. Marc executes a command safely.

1. Marc opens a generated command form from the row, CTA, palette, or canonical command route.
2. Fields with command-provided defaults share one command instance for this form. Opening another form creates fresh defaults. Only editable fields render; destructive intent and authorization are checked at their owned gates.
3. A failed client submit focuses the linked error summary; Marc follows the first invalid link and
   corrects the field without losing useful input.
4. A valid submit enters Submitting, Acknowledged, and Syncing; transport acceptance never says
   Confirmed.
5. With default settings the shell checks status every second. If still unresolved, it announces AM-17 at 10 seconds and AM-24 at the 120-second deadline. UX-DR5 defines the independent zero-poll and zero-retry settings. Each terminal outcome announces once.
6. **Climax:** Marc can distinguish Confirmed, Rejected, IdempotentConfirmed, NeedsReview, Warning,
   and Degraded and knows what, if anything, to do next.

When Marc administers Parties in the combined host, the selected tenant and user must match current, active, authorized TenantOwner evidence from the caller’s Tenants API. If his principal changes while the check runs, the previous result is discarded. Missing or denied context leads to SS-05/SS-06.

If an accepted response has unreadable correlation data, the optional recovery request reuses the original `MessageId`. Disabling or failing that retry preserves Accepted with unknown correlation. Marc waits for new outcome evidence or the local deadline.

Failure: a second local submit does not dispatch or queue. Focus stays usable, the original lifecycle
remains visible, and AM-20 explains once that the attempted command did not run.

[Command lifecycle reference](ux-designs/ux-frontcomposer-2026-09-09/mockups/key-command-lifecycle.html)

### UJ-4. Ravi exposes the domain surface to an AI agent.

1. Ravi configures the existing FrontComposer HTTP MCP host for a compatibility consumer. The endpoint authenticates the caller, and the host supplies the tenant and resource visibility policies.
2. The agent sees its permitted tools. The static resource catalog exposes registered projection and skill descriptors. Reading projection data requires admission.
3. An admitted tool call checks schema and arguments, injects server-owned fields, and returns an acknowledgement.
4. In a later request, `frontcomposer.lifecycle.subscribe` rechecks reader admission and observes the retained command outcome.
5. **Climax:** Ravi can distinguish an acknowledgement from a confirmed domain outcome across requests.

Failure: for one caller, hidden and absent tools return the same public result. Registered hidden projection reads return `unknown_resource`. The SDK gives its own not-found response for an unregistered URI. The September 26 G-7 receipt covers the exact audited candidate. [FR-19 disclosure table](prd.md#fr-19-enforce-mcp-fail-closed-admission-and-compatibility)

`Hexalith.McpCli` owns the target agent transport. The FrontComposer host and Projects plug-in retire together after approved replacement or withdrawal for each capability, FK01–FK04 consumer migration, K12 Web/Chatbot evidence, Projects 6.7 cutover, DW-82 publication, and selected-environment live, release and rollback acceptance. [Compatibility inventory](../../README.md#legacy-compatibility-surfaces)

### UJ-5. Camille preserves generator/runtime compatibility.

1. Camille, a framework maintainer, changes a generated or runtime contract.
2. She updates the diagnostic registry, snapshots, fingerprints, public API baselines, and migration or
   deprecation material required by that change.
3. She runs the supported local `frontcomposer inspect` command on generated files. Without `--build`, the report describes the files already on disk. Generated-file paths are project-relative unless she specifies `--absolute-paths`. Project and diagnostic sidecar paths stay relative or redacted.
4. She previews an allowlisted change with `frontcomposer migrate`. On apply, each eligible file is rechecked and replaced atomically. She verifies the resulting diff and runs the contract and documentation checks.
5. **Climax:** a downstream adopter can identify the intentional change and its migration path without
   a stale component name or silent schema mismatch.

Failure: a dead source link, unresolved component/API name, or catalog/index mismatch fails the owning
gate. A handled write failure exits `4` and the report says `applied=false` even if an earlier file changed. Cancellation may leave earlier edits and an incomplete or absent report. Camille inspects the working tree before retrying. [Compiler CLI](../../docs/reference/cli.md)

### UJ-6. Sophie tests a generated consumer experience.

1. Sophie, an adopter test engineer, uses the Testing package host and deterministic fakes.
2. She exercises success, rejection, stall/timeout, authorization denial, paging, filtering, and sorting.
3. She adds assertions for summary links, first-invalid focus, input preservation, client/server
   distinction, announcement counts, blocked submit, route/tab/palette/dialog focus, and silent expiry.
4. She runs 320 CSS-pixel, 400% zoom, text-spacing, target-size, focus-obscuration, theme,
   forced-colors, and reduced-motion evidence for changed surfaces.
5. **Climax:** Sophie can point from each changed behavior to its matrix row and verification on a named revision. The story records commands, pass/fail counts, commit and CI link. Raw reports, traces and screenshots stay local or in CI artifacts. Source-owned visual baselines remain tracked.

Failure: missing, unsanitized, historical-only, or severity-filtered evidence remains open; it cannot
be relabelled as an SM-6 pass or Product approval.
