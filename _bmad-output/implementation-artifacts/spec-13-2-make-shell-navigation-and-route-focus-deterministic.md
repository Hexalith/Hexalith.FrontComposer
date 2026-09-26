---
title: 'Story 13.2: Make Shell Navigation and Route Focus Deterministic'
type: 'feature'
created: '2026-09-27'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Shell route, tab, palette, settings, and search focus can lose the operator's place. Custom header slots can remove account or hamburger controls; projection links currently compete with Module navigation.

**Approach:** Make one Module entry and default tab per bounded context, coordinate route success/failure focus, capture and restore overlay origins, and target the route's single page search for `/`. Complete the specified shell, Home, and palette states with support-safe, single-path messages.

## Boundaries & Constraints

**Always:** Apply Story 13.2 and canonical UX rows FM-01–05, FM-10 return, FM-12, OF-01–02, AM-23/25/28/29/31, and assigned SS rows. Preserve the existing Fluent V5 controls, registry, breakpoint watcher, settings live changes, framework sign-out/token eviction, and command route family. Keep one main landmark, unique focusable route h1, one active Module item by longest segment prefix, route-backed tabs, and safe localized labels. Scope preference persistence by tenant/user. Do not expose routes, payloads, tokens, or exception details in messages/evidence. Update intentional public API baselines.

**Never:** Move ownership of AM-01–03/26 or shared announcement coalescing from Story 13.4; implement command confirmation/abandonment focus from 13.3; claim OI-16, G-4, Fluent, or Product approval. Do not add raw interactive controls, new breakpoint widths, or a second modal layer.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Route/tab | Deep link, CTA, palette, Module tab, invalid/disabled tab | Valid route focuses unobscured h1; keyboard tab retains focus and updates labelled panel; `/{module}` selects default | Failure preserves invoker/current heading with one AM-23; invalid tab selects default with one AM-31; canonicalization silent |
| Search/overlay | `/`, Ctrl+K, Ctrl+,, visible trigger | Only one enabled toolbar search receives `/` without value loss; palette query or fallback and settings heading receive entry focus; closing restores captured invoker | Editable/IME/ambiguous search does nothing; removed/disabled origin falls back to route h1, never body |
| Shell states | Bootstrap, no registrations, Home loading/empty/data, startup failure | No-data and loading remain distinct; AM-25 once; Home order is readiness, actionable count, ordinal name | Startup failure focuses AM-29 heading without live attributes or exception detail |
| Palette results | Debounced zero, stale query, denied/failed activation | One palette-owned AM-28 after trailing 250ms; stale result discarded; query remains editable | Denial uses 13.4 AM-26; failed navigation uses AM-23 and retains route |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.UI/Components/Routes.razor`, `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs` — existing found-route `FocusOnNavigate` and optional h1 tabindex; coordinate focus after render, including failures.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor{,.cs}` — route events and replaceable header/nav slots; hamburger/account must be framework owned.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor{,.cs}`, `Routing/ProjectionRouteBuilder.cs`, `Registration/FrontComposerRegistry.cs`, `src/Hexalith.FrontComposer.Contracts/Registration/FrontComposerNavEntry.cs` — reuse prefix selection and manifests; no Module/default-tab model exists.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageTabs.razor{,.cs}` — Fluent tab/panel semantics, currently caller-owned IDs without route coupling.
- `references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`, `samples/Counter/Counter.Web/Components/Pages/CounterPage.razor` — existing `/tenants` and `/counter` workspaces; route-backed tab adoption must preserve them.
- `src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs`, `Components/Layout/FcPageToolbar.razor{,.cs}`, `wwwroot/js/fc-keyboard.js` — replace first-grid-filter `/` target with one enabled page search; retain editable-key guard.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor{,.cs}`, `FcSettingsDialog.razor{,.cs}`, `wwwroot/js/fc-focus.js` — add connected origin capture, entry/return focus, and palette-owned zero-result status; remove body fallback/count speech.
- `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor{,.cs}` — preserve state layout and deterministic ordering.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/`, `Shortcuts/FrontComposerShortcutRegistrarTests.cs`, `tests/e2e/specs/{route-contract,sidebar-responsive,page-toolbar,settings-persistence}.spec.ts` — existing focused coverage to extend.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.FrontComposer.Shell/Routing/ModuleRouteCatalog.cs`, `Components/Layout/FrontComposerNavigation.razor{,.cs}`, `FcPageTabs.razor{,.cs}` — derive one Module entry from each manifest; require an explicit default tab ID on route-backed `FcPageTabs`, resolve aliases and invalid/disabled tabs, keep projection flyouts secondary and current-prefix selection.
- [ ] `references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`, `samples/Counter/Counter.Web/Components/Pages/CounterPage.razor` — adopt route-backed tabs/default aliases without changing existing page or projection content; follow the owning repository's guidance for Tenants.
- [ ] `src/Hexalith.FrontComposer.UI/Components/Routes.razor`, `Components/Layout/{FrontComposerShell,FcPageHeader}.razor{,.cs}` — coordinate post-render h1 focus for successful navigation/auth return, tab-focus retention, route failure and safe AM-23, startup failure heading; keep focus clear of chrome.
- [ ] `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor{,.cs}` — make hamburger/account present in every viewport regardless of adopter slots/opt-outs; preserve framework sign-out; baseline changed public surface.
- [ ] `src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs`, `Components/Layout/FcPageToolbar.razor{,.cs}`, `wwwroot/js/fc-keyboard.js` — register only the active route's sole enabled toolbar search for `/`; ignore editable, IME, chord, absent and ambiguous cases.
- [ ] `src/Hexalith.FrontComposer.Shell/Components/Layout/{FcCommandPalette,FcSettingsDialog}.razor{,.cs}`, `wwwroot/js/fc-focus.js` — capture direct origin before keyboard/pointer open, focus entry target, restore on Escape/close or h1 fallback; keep Fluent modal/combobox semantics and settings changes.
- [ ] `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs`, `Components/Layout/FcCommandPalette.razor{,.cs}` — retain 150ms authorized search, discard stale/denied activation, add one palette-owned AM-28 status with fake-time 249/250ms evidence, suppress duplicate result-count speech.
- [ ] `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor{,.cs}`, `Components/Layout/FrontComposerShell.razor{,.cs}` — distinguish loading, empty, no registrations and startup failure; implement AM-25/29 and stable Home ordering without false readiness.
- [ ] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`, `FcPageTabsTests.cs`, `FcCommandPaletteTests.cs`, `FcSettingsDialogTests.cs`, `Shortcuts/FrontComposerShortcutRegistrarTests.cs`, `tests/e2e/specs/route-contract.spec.ts`, `sidebar-responsive.spec.ts` — extend focused coverage for every matrix edge, focus/message count, tab/overlay behavior, viewport modes and support-safe evidence; update API baselines.

**Acceptance Criteria:**
- Given each route, overlay, shell state, and keyboard entry above, when the focused bUnit/e2e scenarios run, then the corresponding matrix outcome and canonical FM/OF/AM/SS semantics hold with no duplicate speech path.
- Given a header override or account/navigation opt-out, when Desktop, Compact, or Narrow shell renders, then account and hamburger remain reachable; Module navigation has one current entry and no projection promotion.
- Given the focused regression suite, when Story 13.2 is verified, then existing toolbar, settings, page-section, sign-out, and tenant-boundary behavior remains valid; independent readiness/approval gates remain open.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Preserve `FrontComposerNavigation.LongestNavPrefix` and FluentTabs' inherited arrow-key behavior. Use a declared default Module tab, with the Module plural or `Overview` label per canonical IA; never guess from a URL or first visible projection. Keep one route-focus owner so `FocusOnNavigate` and component focus do not race.

## Verification

**Commands:**
- `dotnet build Hexalith.FrontComposer.slnx -c Debug --no-restore -m:1 -v:q` — zero warnings/errors.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests -class '*FrontComposerShellTests'` — pass; repeat with each changed focused test class.
- `npm --prefix tests/e2e run test -- specs/route-contract.spec.ts specs/sidebar-responsive.spec.ts specs/page-toolbar.spec.ts specs/settings-persistence.spec.ts --project=chromium` — route/focus behavior passes.
