# Sprint Change Proposal — Auto-register domain menus into the global shell nav

_Workflow: bmad-correct-course · Date: 2026-06-09 · Mode: Batch · Status: **Implemented & verified**_

## Section 1 — Issue Summary

The Tenants UI left nav rendered as bare plain-text links (no icons, group header, active state,
Fluent styling) while its header was correct Fluent UI — far from the Fluent UI Blazor v5 sample's
`FluentNavMenu` (icons, category group headers, expand/collapse, active accent). **Root cause:** the
framework's `FrontComposerNavigation` rendered only `DomainManifest.Projections`; Tenants registered an
empty manifest and hand-rolled `OperationsShellNavigation` (Fluent-less links) via the shell's
`<Navigation>` slot, and `IFrontComposerRegistry.AddNavGroup(...)` was dead code (stored, never
rendered). A domain had **no working way** to contribute menu items to the global nav.

**Goal:** domain modules' menus appear automatically in the global left nav, with **minimal coupling
between the domain stack (Hexalith.Tenants) and the technical stack (Hexalith.FrontComposer)** — the
domain contributes plain data through the `Contracts` abstraction; the shell owns all rendering.

## Section 2 — Path Forward (chosen)

**Option 1 — Direct Adjustment**, via the framework's own companion-interface extensibility pattern
(like `IFrontComposerFullPageRouteRegistry`), additive so all pinned nav tests stay green. Extends
Epic 2 / FR14; no epic invalidated or re-sequenced; no PRD/architecture conflict. Scope: Moderate.

## Section 3 — Changes implemented

### Hexalith.FrontComposer (technical stack)
- **Contracts** — new `FrontComposerNavEntry` record (BoundedContext, Title, Href, Icon?, Order,
  RequiredPolicy?, Enabled, DisabledReason?); new companion interface `IFrontComposerNavEntryRegistry`;
  `AddNavEntry`/`GetNavEntries` extension methods with permissive fallback. `IFrontComposerRegistry`
  **unchanged** (zero breakage to its many implementers). Multi-TFM clean (netstandard2.0 safe).
- **Shell** — `FrontComposerRegistry` implements the companion (ordered snapshot by Order then Title);
  `FrontComposerNavigation` renders a `FluentNavCategory` per bounded context with **projections OR
  nav entries**, each entry a `FluentNavItem` (icon via `FcFluentIcons`, ordering, `AuthorizeView`
  policy gating, disabled affordance + reason), plus an orphan-context fallback; `FcCollapsedNavRail`
  surfaces entry-only contexts; `FrontComposerShell.HasRenderableManifest()` now also true when nav
  entries exist (so the menu shows for projection-less domains).

### Hexalith.Tenants (domain — submodule)
- `TenantsFrontComposerRegistration` registers nav entries as data (Tenants `/tenants`, My tenants
  `/tenants/my`, User lookup `/tenants/users` [search icon], Global Administrators
  `/global-administrators` [settings icon, gated by `Tenants.GlobalAdministrator` policy]); references
  **Contracts only**.
- `Program.cs` registers the `Tenants.GlobalAdministrator` policy (role `GlobalAdministrator` + claim
  `eventstore:tenant=system`), mirroring the BFF reflection so gating is preserved.
- `MainLayout.razor` collapsed to `<FrontComposerShell>…</FrontComposerShell>` (auth bar retained); the
  framework auto-renders the nav.
- **Deleted** `OperationsShellNavigation.razor` + `.razor.css` (the coupling leak).

### Tests
- Shell: +6 `FrontComposerNavigationNavEntryTests` (entry rendering, empty-projections-with-entries
  category, disabled+reason, policy gating shown/hidden, orphan context) + 2 `FrontComposerRegistryTests`
  (ordering, empty). Tenants: `TenantsUiCompositionTests` asserts the nav-entry set + new MainLayout;
  removed `OperationsShellNavigationTests` and the obsolete top-level-Audit pin in `AuditEvidenceEntryPointTests`.

## Section 4 — Verification (all green)

- `dotnet build Hexalith.FrontComposer.slnx -c Release` → **0 errors** (TWAE).
- FrontComposer default test lane: **+8 new tests pass; failure count unchanged** vs a stashed clean-HEAD
  baseline (Shell 6 / SourceTools 3 / Testing 1 = 10 pre-existing/environmental failures — generated
  snapshots, governance/package-inventory, NU5104 bunit-prerelease packaging, IDE-parity Linux paths;
  none introduced by this change, proven by re-running the lane on clean HEAD).
- `Hexalith.Tenants.UI.Tests`: **669 passed / 0 failed**.
- Visual (standalone Tenants UI): left nav now shows a collapsible **"Tenants"** category (folder icon
  + chevron) with **Tenants / My tenants / User lookup** items; **Global Administrators hidden** when
  unauthenticated — matching the Fluent v5 sample's `FluentNavMenu` structure.

## Section 5 — Follow-ups / notes

- **Menu-title localization (known gap):** entry titles register as static strings, so the menu shows
  English ("Tenants", "My tenants") even in the French UI (page content stays localized). The bespoke
  nav previously localized via `IStringLocalizer<TenantsResources>`. A localized nav-entry mechanism
  (e.g. resource-key + base-name on the entry, resolved by the shell at render) is a recommended
  follow-up. Unused `Tenants.Navigation.*` resx keys were left in place (harmless) pending that work.
- **No commit made** (not requested). Tenants submodule changes are staged/working-tree only.
- Handoff: Developer (implemented this session). No sprint-status.yaml change (no epics added/removed).
