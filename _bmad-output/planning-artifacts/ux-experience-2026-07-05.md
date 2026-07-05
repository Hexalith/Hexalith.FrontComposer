---
name: Hexalith Common Application UX
status: draft
updated: 2026-07-05
sources:
  - ../../prd.md
  - ../../architecture.md
  - ../../ux-design.md
  - ../../epics.md
  - ../../prds/prd-frontcomposer-2026-07-05/prd.md
  - ../../sprint-change-proposal-2026-06-19-nav-single-active-item.md
  - ../../sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md
  - ../../sprint-change-proposal-2026-07-01-tenants-ui-menu-icon-label-stack.md
  - ../../sprint-change-proposal-2026-07-05.md
---

# Hexalith Common Application Experience

These spines win on conflict with mockups, wireframes, imported screens, or older sprint-change notes.

## Foundation

Primary form factor is **web desktop**. The UI system is **FrontComposer + Blazor Fluent UI V5**. `DESIGN.md` is the visual identity reference; this file owns information architecture, behavior, states, interactions, accessibility, and journeys.

Audience is mixed: consumer-facing module users and administrators. The shared experience must therefore stay professional, support-safe, and task-focused. It must not depend on bespoke host CSS or module-specific visual themes.

## Information Architecture

| Surface | Reached from | Purpose |
|---|---|---|
| Application shell | App open / authenticated route | Global frame, account menu, command palette, settings, module navigation |
| Module menu entry | Shell navigation rail | One primary entry per Hexalith module |
| Module main page | Module menu entry | Workspace/dashboard for the selected module |
| Module tabs | Module main page | Navigation across that module's UX pages |
| Search/list tab | Module tabs | Find domain aggregate items, filter, sort, inspect status |
| Detail/edit surface | Search result row / detail action | Review one item and launch allowed modification commands |
| Add/create surface | Toolbar action / empty-state CTA / command palette | Create a new domain aggregate item |
| Command lifecycle surface | Submit command | Show transport, projection-confirmation, rejection, degraded, and needs-review states |

Module tabs are route-backed or otherwise deep-linkable so command palette entries, browser refresh, support links, CTAs, tests, and command-result links can land on a specific module UX page without adding extra application-level menu entries.

Application-level navigation must not list every projection, command, or module subpage as a primary menu item. A module may expose counts, badges, or secondary flyout links only if they route into the module workspace/tab model and preserve the one-entry-per-module rule.

## Voice and Tone

Microcopy is direct, support-safe, and evidence-based. Brand posture lives in `DESIGN.md`.

| Do | Don't |
|---|---|
| "No parties match these filters." | "Nothing here!" |
| "Command accepted. Waiting for projection confirmation." | "Saved successfully" before projection evidence exists |
| "This data may be stale. Refresh or wait for reconnect." | "Realtime disconnected" without an action or consequence |
| "You do not have access to this module." | Expose policy names, tenant internals, raw EventStore metadata, tokens, or stack traces |
| "Create party" / "Edit party" | Generic "Submit" when the domain action is known |

## Component Patterns

Behavioral rules live here; visual rules live in `DESIGN.md.Components`.

| Component | Use | Behavioral rules |
|---|---|---|
| Module navigation entry | Shell rail | Exactly one primary entry per module. It opens the module main page and reflects active module state. |
| Module workspace/dashboard | Module root | Shows the module page header, optional module summary, and the tab set for module UX pages. |
| Module tabs | Module UX page navigation | Use `FluentTabs`. Preserve keyboard navigation, selected state, and deep links. Tabs do not duplicate shell nav. |
| Page toolbar | Search/list and multi-view tabs | Use `FcPageToolbar`: search first, filters in popovers, view/overflow menu, actions on the right. |
| Search/list grid | Domain aggregate discovery | Search/filter/sort with `FluentDataGrid`; row detail regions remain accessible. |
| Add action | Toolbar or empty state | Launch the generated command form or route-backed command page for create flows. |
| Modify action | Detail surface / row action | Launch the generated command form for allowed mutations. Server-controlled fields are not editable. |
| Command lifecycle | After submit | Distinguish Submitting, Acknowledged, Syncing, Confirmed, Rejected, IdempotentConfirmed, NeedsReview, and Degraded. |
| Status affordance | Grids, rows, command lifecycle | Use icon + tooltip + `aria-label`; never rely on color alone. |
| Multi-section page body | Page/dialog/detail with sibling titled regions | Use one `FluentAccordion`; primary item expanded by default. Do not hide the only primary content region. |

## State Patterns

| State | Surface | Treatment |
|---|---|---|
| Empty shell | Application shell | Valid state. Show neutral empty module directory or message without errors. |
| Module loading | Module main page | Skeletons matching the expected layout; do not shift shell chrome. |
| No module access | Module entry or page | Hide inaccessible modules when policy requires hiding; otherwise show support-safe denied state. |
| Empty aggregate list | Search/list tab | Explain that no items exist and offer the allowed create action when available. |
| Search no results | Search/list tab | Preserve filters and offer reset. Do not show create as the only recovery unless creation is allowed. |
| Stale projection | Grid/detail | Visible stale indicator; mutation actions fail closed where the domain requires current projection evidence. |
| Reconnecting | Grid/detail | Show reconnecting or fallback polling state without treating SignalR nudges as success evidence. |
| Command accepted | Command lifecycle | Show that transport succeeded but projection confirmation is pending. |
| Command confirmed | Command lifecycle + grid/detail | Show confirmation only after projection or confirmed status evidence. |
| Command rejected | Command lifecycle | Show structured, localized, support-safe reason. Preserve input when retry is meaningful. |
| Destructive action | Command form | Require `FcDestructiveConfirmationDialog` or an equivalent FrontComposer pattern. |

## Interaction Primitives

- Shell navigation opens module workspaces, not individual module pages.
- Tabs switch module UX pages and must be keyboard-operable through Fluent tab behavior.
- Search is first-class on aggregate list pages. `/` may focus page search when that shortcut is enabled.
- `Ctrl+K` opens the command palette. Palette results for module pages or commands must land on real routes inside the module workspace/tab model.
- `Ctrl+,` opens settings.
- `Esc` closes the topmost menu, popover, dialog, or palette and returns focus.
- Hover-only affordances are not sufficient; every action must be reachable by keyboard.
- Modal stacks should stay one level deep. Prefer tab/detail/command surfaces over nested dialogs.

## Accessibility Floor

Behavioral accessibility lives here; visual contrast lives in `DESIGN.md`.

- WCAG 2.2 AA target for common shell and generated module pages.
- Skip links, route-level heading focus, visible focus indicators, and logical tab order are required.
- Every interactive Fluent component must have an accessible name and preserve `data-testid` where it is part of the testing contract.
- Status icons require `aria-label`; tooltips must appear on hover and keyboard focus.
- Loading, command lifecycle, stale, reconnecting, and fresh-row indicators use live regions only where the state change is useful and non-noisy.
- Forced-colors and reduced-motion modes must not remove meaning.
- Support-safe copy is mandatory: no bearer tokens, decoded JWT payloads, raw EventStore metadata, stack traces, raw event payloads, or unrestricted PII.

## Responsive & Platform

Primary target is desktop web. The shell may retain existing responsive behavior for compact desktop and mobile-width browsers, but this UX spine does not define a mobile-native product.

| Width | Behavior |
|---|---|
| Desktop | Navigation rail visible; one entry per module; module workspace uses tabs and full-width data surfaces. |
| Compact desktop | Rail may collapse to icon-only; module workspace and tabs remain reachable. |
| Narrow browser | Drawer behavior may be used, but it still lists modules rather than every module page. |

## Inspiration & Anti-patterns

- **Lifted from .NET Aspire Dashboard:** neutral chrome, accent as thread, compact data density, sticky grid headers, toolbar/search discipline, and lightweight status icons.
- **Lifted from FrontComposer brownfield policy:** registry-driven shell composition, generated command/projection surfaces, command lifecycle truth, and Fluent governance.
- **Rejected: primary-nav explosion.** Listing every projection, command, and module subpage in the shell makes Hexalith applications hard to scan.
- **Rejected: custom module themes.** Module pages must not define their own palettes, typographic systems, or raw-control styling.
- **Rejected: false success.** Accepted command transport is not user-visible success until projection confirmation or approved command status evidence exists.

## Key Flows

### Flow 1 - Find and modify a party (Alex, authenticated administrator, weekday morning)

1. Alex opens the Hexalith application on desktop.
2. The shell renders one menu entry per module. Alex selects **Parties**.
3. Parties workspace opens with module tabs. The **Search** tab is selected.
4. Alex types part of a party name in the toolbar search.
5. The grid filters and shows matching parties with status icons and accessible row details.
6. Alex opens one party row and chooses **Edit party**.
7. The generated command form opens with only editable fields.
8. Alex submits the change.
9. Command lifecycle moves from Submitting to Acknowledged to Syncing.
10. **Climax:** the Parties grid/detail updates from projection evidence and the lifecycle state becomes Confirmed; Alex can trust the change is visible, not merely accepted by transport.

Failure: projection becomes stale during the edit. The edit action fails closed or warns according to module policy; Alex sees a support-safe stale-data message and a refresh path.

### Flow 2 - Add a new party (Maya, consumer-facing operations user, support shift)

1. Maya opens the application and selects **Parties** from the module menu.
2. Parties workspace opens on the default tab.
3. She searches for the person first to avoid duplicates.
4. No matching party appears.
5. The toolbar offers **Create party** because Maya has permission.
6. The generated create command form opens.
7. Maya fills required fields and submits.
8. The lifecycle shows accepted-but-waiting rather than immediate success.
9. **Climax:** once projection evidence arrives, the new party appears in the list and is marked through the approved fresh-row indicator contract when available.

Failure: the command is rejected as duplicate or invalid. The form preserves useful input and shows a structured rejection without exposing internal payloads.

## Brownfield Reconciliation

- The one-entry-per-module rule supersedes older navigation patterns that expose individual projections or module UX pages as primary shell menu items.
- Epic 8 projection flyout behavior must be reconciled with this spine: any flyout must be secondary and route into module workspace tabs, not become a second primary IA.
- Story 11.0 route-contract decision must account for module workspace tabs so palette/CTA command activation lands on real routes.
- FC-NIP fresh-row indicators remain blocked until row identity is confirmed; broad row marking or diff-based inference is not allowed.

## IA Decision Gate — FC-IA-1 (module-tab route encoding + projection-flyout IA)

**Status: signed off 2026-07-05.** Recorded in
`_bmad-output/contracts/fc-ia-1-module-tab-ia-decision-2026-07-05.md` and referenced by Story 11.7. The
gate no longer blocks navigation-route work.

Added by correct course 2026-07-05. The four IA questions below were a **blocking decision gate**:
Story 11.7 (command/projection route-contract implementation) and any other navigation/module-tab route
story could **not** move to ready-for-dev until FC-IA-1 was recorded as a Product/UX-signed-off decision.
Owner **Product/UX + Architect**, assigned **2026-07-05**, due **before Epic 11 route/navigation dev
kickoff**. The recommended answers below were accepted as-is on 2026-07-05 and are now the recorded
decision.

1. **Required default tab.** Recommended: **Yes** — each module workspace opens on a required default
   tab. Provisional name = the module plural label (e.g. "Parties"), fallback "Overview". Preserves the
   one-entry-per-module rule.
2. **Tab selection encoding.** Recommended: **route-path segment** (`/{module}/{tab}`) — deep-linkable
   and shareable, consistent with the Story 11.0 path contract
   `/commands/{BoundedContext}/{CommandTypeName}`. Not query string; not internal-only router state.
3. **Projection flyout.** Recommended: **keep as strictly secondary** — the rail flyout routes into
   module workspace tabs and never becomes a second primary IA. This reconciles the Epic 8 projection
   flyout with the one-entry-per-module spine.
4. **First reference module.** Recommended (provisional): **Tenants** (already mid Fluent v5
   conversion); Parties acceptable if Product prefers a domain-richer exemplar.

**Closure rule (met):** FC-IA-1 is signed off — Product/UX accepted answers 1–4 on 2026-07-05, recorded
in `_bmad-output/contracts/fc-ia-1-module-tab-ia-decision-2026-07-05.md`, and Story 11.7 references the
decision. Amending answer 4 (reference module) does not reopen answers 1–3.
