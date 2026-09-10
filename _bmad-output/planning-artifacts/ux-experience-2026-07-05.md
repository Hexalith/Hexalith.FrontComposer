---
name: Hexalith Common Application UX
status: accepted-supplement
product_approval: pending-reapproval
updated: 2026-09-09
reconciliation_revision: oi-16-2026-09-09
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
| Palette/dialog overlay | Shortcut or invoking action | Search/navigate/configure/confirm without losing context | Open, no results, denied/failure, close/cancel, removed origin fallback |

Each Module has one primary shell entry. The default tab uses the Module plural label, falling back to
`Overview`; `/{module}` renders it. Projection flyouts remain secondary and route to
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

The exact announcement templates and channels are canonical UX-AM-1. Visible copy can add a safe
domain noun or recovery action but cannot weaken the stated meaning.

## Component Patterns

These identifiers match the detailed supplement exactly. Visual rules live in its Components section.

| Component | Behavioral contract |
| --- | --- |
| `FrontComposerShell` | Target contract always renders skip target, account access, default hamburger, navigation, route content, and footer and coordinates shortcuts/route focus. Current source conditionally renders account/navigation and places the default hamburger in replaceable `HeaderStart`; FR-8 convergence remains open. |
| `FrontComposerNavigation` | Consumes shell/caller navigation state and targets exactly one current Module; labelled/icon-only modes retain accessible names and current state; projection flyouts route within the Module. The shell, not this component, owns the default hamburger. |
| `FcPageTabs` | Presents a labelled page-body tab list, forwards caller-owned `ActiveTabId`/`ActiveTabIdChanged` state into keyboard selection, and associates one panel; successful keyboard selection follows FM-02. |
| `FcPageTab` | Owns its real panel content and reciprocal tab/panel relationship; disabled tabs are skipped by keyboard selection. |
| `FcPageToolbar` | Public search/filter/view/overflow/action contract; internal Fluent composition is implementation-owned. The target `/` shortcut follows FM-12 only when the active route has exactly one enabled page search; the current registrar focuses the first active DataGrid column filter. |
| `FcCommandPalette` | `Ctrl+K` opens; authorization-aware results debounce at 150 ms; navigation follows FM-01; close follows FM-04; no-results keeps a usable query/close path. |
| `FcSettingsDialog` | `Ctrl+,` opens; entry, name/description, modal containment, errors, Escape, and return follow UX-OF-1. |
| `FcDestructiveConfirmationDialog` | Requires explicit confirmation; UX-OF-1 focuses Cancel, traps within the modal, makes Escape cancel, and returns by FM-05. |
| `FcFormAbandonmentGuard` | After 30 seconds of edits, intercepted navigation exposes an in-flow warning; OF-04 focuses “Stay on form,” Escape stays, and “Leave anyway” is explicit. It is not a modal dialog. |
| `FcLifecycleWrapper` | Presents each FR-15 state without converting Acknowledged into Confirmed; follows UX-AM-1 and UX-SS-1. |
| `FcProjectionLoadingSkeleton` | Represents layout while AM-01 provides semantic loading meaning; repeated animation is disabled under reduced motion. |
| `FcProjectionEmptyPlaceholder` | Distinguishes true Empty from filter-no-results and exposes only an authorized valid CTA. |
| `FcProjectionConnectionStatus` | Represents Stale, Reconnecting, FallbackPolling, and recovery; polling/backoff ticks remain silent. |
| `FcPendingCommandSummary` | Keeps bounded pending/rejected operations visible with state and recovery; never merges separate operation identities. |
| `FcNewItemIndicator` | Appears only from FC-NIP immutable target/materiality proof, once per scope/entity transition; expiry is silent; DW-679 rows receive no marker. |
| `FluentAccordion` (inherited) | Groups two or more sibling titled sections; the primary item starts expanded; a lone primary region is not hidden. |
| `FluentBadge` (inherited) | Carries numeric counts only; semantic status uses persistent text/icon/accessible state. |
| `FluentTooltip` (inherited) | Repeats a concise label on hover and focus; never supplies the only action or state meaning. |

## State Patterns

Canonical UX-SS-1 contains the required entry evidence, visible meaning, actions, recovery/timeout,
announcement, terminality, and baseline/open-delta status. Every IA surface lands on a row below; the
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
| SS-21 | IdempotentConfirmed | Same user-facing confirmation as Confirmed while retaining distinct machine state |
| SS-22 | NeedsReview | Review path follows the state explanation; no false success |
| SS-23 | Warning | Details and close/support path are keyboard reachable |
| SS-24 | Degraded with polling active | Non-terminal; status/review/continue actions remain available until confirmation or ceiling |
| SS-25 | Blocked second submit | VR-05 and FM-09; only the original lifecycle can advance |
| SS-26 | Degraded with polling exhausted | Terminal local lifecycle at 120,000 ms; AM-24 and separately correlated later status |
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
the exact count. Fresh-row appearance is deduplicated separately by tenant + user + entity transition,
regardless of view; it is not coalesced with connection or query progress. Retry/backoff/poll ticks,
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
- `Ctrl+K` opens the command palette. `Ctrl+,` opens settings. The target `/` shortcut focuses the
  active route's page search only when exactly one enabled `FcPageToolbar` page-search input exists;
  otherwise it is a silent no-op and never changes a value. The current registrar focuses the first
  active DataGrid column filter, so
  convergence and its destination evidence remain open under FR-8.
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
| Desktop | Labelled or icon-only `FrontComposerNavigation`; one current Module; tabs and full data operations | UX-RF-1 plus light/dark/forced-colors screenshots |
| Compact desktop | Rail can use icon-only mode; all labels remain accessible; toolbar/tabs wrap or own scroll | Keyboard/focus-order and target-size assertions |
| 320 CSS-pixel reflow | Drawer/navigation, content, toolbar, forms, and overlays retain every operation; no page-level horizontal scroll; labelled grids and the tab strip may each own bounded horizontal scroll while selected tab/focus remain visible | UX-AE-1 AE-01 per changed surface |
| 400% zoom | Same functional and reading-order outcome as 320 CSS pixels | UX-AE-1 AE-02 per changed surface |
| Text-spacing override | Content-driven height; no clipping/overlap/loss; compact grid rows expand | UX-TS-1 + AE-03 |
| Forced colors | System text/border/icon/current/focus cues preserve every state | UX-VC-1 + AE-07 |
| Reduced motion | Decorative transitions/pulse/smooth scrolling stop; semantics/focus/announcements remain | UX-RM-1 + AE-08 |

This contract does not define a mobile-native product. Tablet and phone-width browsers remain
functional evidence tiers and must not be described as desktop-equivalent daily-use design without a
separate Product decision.

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

The titles below preserve all six source journey IDs and names and retain their previously delivered
runtime baselines. They do not claim complete end-to-end delivery: UJ-4 retains open G-7 work, UJ-5
retains open OI-19 work, and UJ-6 retains open OI-16/SM-6 implementation and evidence work. The
2026-09-09 delta adds deterministic obligations, not new journey identifiers.

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
5. A row changed materially by Marc's own eligible command receives the scoped `FcNewItemIndicator`.
6. **Climax:** Marc identifies the affected row and can tell whether the read model is Data, Stale,
   Reconnecting, or FallbackPolling without relying on color or animation.

Failure: missing tenant context shows SS-05 rather than empty-looking data. A server-allocated-key
command covered by DW-679 produces no fresh marker rather than a wrong one.

### UJ-3. Marc executes a command safely.

1. Marc opens a generated command form from the row, CTA, palette, or canonical command route.
2. Only editable fields render; destructive intent and authorization are checked at their owned gates.
3. A failed client submit focuses the linked error summary; Marc follows the first invalid link and
   corrects the field without losing useful input.
4. A valid submit enters Submitting, Acknowledged, and Syncing; transport acceptance never says
   Confirmed.
5. A terminal or degraded landing exposes its safe recovery actions and announces once.
6. **Climax:** Marc can distinguish Confirmed, Rejected, IdempotentConfirmed, NeedsReview, Warning,
   and Degraded and knows what, if anything, to do next.

Failure: a second local submit does not dispatch or queue. Focus stays usable, the original lifecycle
remains visible, and AM-20 explains once that the attempted command did not run.

### UJ-4. Ravi exposes the domain surface to an AI agent.

1. Ravi, an AI-agent integrator, hosts the MCP surface behind host authentication and registers the
   required tenant/resource gates.
2. The agent discovers only the contractually visible tools while projection/skill resources follow
   the PRD's exact disclosure rules.
3. A command passes admission, schema negotiation, validation, and server-owned field injection.
4. The agent follows the same lifecycle truth across requests through
   `frontcomposer.lifecycle.subscribe`.
5. **Climax:** human UI and agent surfaces agree on command identity and outcome without the visual UX
   contract inventing a second MCP state vocabulary.

Failure: hidden/unknown/unauthorized cases use the PRD's opaque public shapes. This behavior supplement
does not claim that G-7 security acceptance is complete.

### UJ-5. Camille preserves generator/runtime compatibility.

1. Camille, a framework maintainer, changes a generated or runtime contract.
2. She updates the diagnostic registry, snapshots, fingerprints, public API baselines, and migration or
   deprecation material required by that change.
3. She checks that UX docs use exact public FrontComposer identifiers and selected-pin Fluent APIs.
4. She runs the owning contract and documentation lanes rather than approving syntax-only parity.
5. **Climax:** a downstream adopter can identify the intentional change and its migration path without
   a stale component name or silent schema mismatch.

Failure: a dead source link, unresolved component/API name, or catalog/index mismatch fails the owning
gate. OI-19 remains open until full FR-23 parity evidence exists.

### UJ-6. Sophie tests a generated consumer experience.

1. Sophie, an adopter test engineer, uses the Testing package host and deterministic fakes.
2. She exercises success, rejection, stall/timeout, authorization denial, paging, filtering, and sorting.
3. She adds assertions for summary links, first-invalid focus, input preservation, client/server
   distinction, announcement counts, blocked submit, route/tab/palette/dialog focus, and silent expiry.
4. She runs 320 CSS-pixel, 400% zoom, text-spacing, target-size, focus-obscuration, theme,
   forced-colors, and reduced-motion evidence for changed surfaces.
5. **Climax:** the retained, redacted evidence maps every changed behavior to a stable UX matrix row and
   exposes any missing proof instead of treating an existing green lane as sufficient.

Failure: missing, unsanitized, historical-only, or severity-filtered evidence remains open; it cannot
be relabelled as an SM-6 pass or Product approval.
