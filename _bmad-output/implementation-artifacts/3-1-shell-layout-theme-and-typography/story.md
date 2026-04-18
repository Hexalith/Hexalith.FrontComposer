# Story

## Story Statement

**As a** business user,
**I want** a well-structured application shell with a configurable theme and consistent typography,
**So that** the composed application feels professional and I can switch between light and dark modes based on my preference.

## Story Goal (one sentence)

Ship a framework-owned `FrontComposerShell` component that composes Fluent UI v5's `FluentLayout` into the spec-pinned three-region application chrome (Header 48 px / Navigation ~220 px / Content), wires `IThemeService` + Fluxor `ThemeState` for Light/Dark/System persistence, exposes the Typography API + six semantic color slots, ships `LocalStorageService` with LRU + `FlushAsync` drain, and removes the `StorageKeys` static `"default"/"anonymous"` placeholders in favor of `IUserContextAccessor` fail-closed reads — all behind the zero-override invariant.

## Scope Statement

Story 3-1 is an **Epic 3 infrastructure opener**. It delivers foundational shell chrome, theming, typography, and durable client-side storage that every subsequent Epic 3 story (sidebar 3-2, density settings 3-3, command palette 3-4, home directory 3-5, session persistence 3-6) builds on. It also unblocks Epic 4 (projection DataGrid), Epic 5 (real-time), and Epic 6 (customization gradient) by establishing the shell layout seam and the tenant-scoped storage key discipline.

**In scope:**

- `FrontComposerShell.razor` component in `Hexalith.FrontComposer.Shell/Components/Layout/`, composed of FluentLayout + FluentLayoutItem areas (Header, Navigation, Content, Footer) with Header height pinned to 48 px and Content forms constrained to `FcShellOptions.FullPageFormMaxWidth` (default `720px` from Story 2-2).
- Header content: app title slot, breadcrumbs slot (empty placeholder in 3-1 — populated by Story 3-2 + consumer navigation), Ctrl+K command-palette trigger icon (placeholder button that just logs "Opens command palette — Story 3-4" + `aria-disabled="true"` until 3-4), `FcThemeToggle` with Light/Dark/System choice, and a settings icon (placeholder button that just logs "Opens settings — Story 3-3").
- `FcThemeToggle.razor` + `FcSystemThemeWatcher` pair: toggle dispatches `ThemeChangedAction`; watcher listens to `prefers-color-scheme` via JS interop and dispatches when `CurrentTheme == ThemeValue.System`.
- `FcShellOptions` adds `AccentColor` (default `#0097A7`, hex-regex validated), `LocalStorageMaxEntries` (default 500, range `[50, 10_000]`), `DefaultCulture` (default `"en"`), `SupportedCultures` (default `["en", "fr"]`).
- `Hexalith.FrontComposer.Typography` static class in `Hexalith.FrontComposer.Contracts/Rendering/` (9 constants returning `Typography` enum values — the Fluent UI v5 enum, not strings).
- Six semantic color-slot CSS custom properties (`--fc-color-accent`, `--fc-color-neutral`, `--fc-color-success`, `--fc-color-warning`, `--fc-color-danger`, `--fc-color-info`) declared in `FrontComposerShell.razor.css` and mapped to Fluent UI tokens.
- `LocalStorageService` in `Hexalith.FrontComposer.Shell/Infrastructure/Storage/`, implementing `IStorageService` over `IJSRuntime` with LRU eviction keyed on last-write timestamp, fire-and-forget `SetAsync`, and `FlushAsync` drain.
- `beforeunload.js` JS module + `DotNetObjectReference` bridge that calls `IStorageService.FlushAsync()` before page unload.
- `FcShellResources.resx` (EN default) + `FcShellResources.fr.resx` resource pair under `Hexalith.FrontComposer.Shell/Resources/` containing framework-generated UI strings (app title label, theme toggle labels, settings/palette aria labels, lifecycle state labels — inherited scope bump from Story 2-4 which referenced French copy without materialising the files).
- `AddHexalithShellLocalization` extension method on `IServiceCollection` registering EN + FR culture providers.
- Refactor `ThemeEffects` + `DensityEffects` to read `tenantId`/`userId` via `IUserContextAccessor` (already registered Scoped per Story 2-2 D31) and short-circuit persistence when context is null (fail-closed per L03 + memory feedback `feedback_tenant_isolation_fail_closed.md`). `StorageKeys.DefaultTenantId` / `DefaultUserId` constants are deleted.
- Counter.Web `MainLayout.razor` rewired to `<FrontComposerShell>` — the ad-hoc FluentLayout block collapses to a 3-line wrapper.
- `FluentProviders` + `Fluxor.Blazor.Web.StoreInitializer` placement rules documented in `FrontComposerShell.razor` header comment so adopters know where they live.

**Out of scope (Known Gaps / downstream stories):**

- Collapsible sidebar groups, sidebar collapse persistence, hamburger toggle state → **Story 3-2**.
- Density selection UI (settings dialog, live preview, radio options) → **Story 3-3**. Density system state + `--fc-density` application already exist from Story 1-3 / earlier work; 3-1 does not touch it.
- Full command-palette implementation (FluentSearch, debounce, fuzzy match, result categories, shortcut table) → **Story 3-4**. 3-1 only ships a placeholder trigger icon.
- Settings dialog UI → **Story 3-3**. 3-1 only ships a placeholder settings icon.
- `FcHomeDirectory`, badge counts, new-capability indicators → **Story 3-5**.
- Full session persistence (DataGrid filters, sort, scroll, expanded rows restoration) → **Story 3-6**. 3-1 ships only the generic `LocalStorageService` that 3-6 will consume.
- Multi-tenant **theme/branding** (per-tenant accent via Fluxor feature) → deferred to v1.x per architecture §598.
- Server-side preference storage / cross-device sync → deferred to v2 per UX spec §106.
- RTL verification baselines → v2 per UX spec §36.
- Type specimen view + CI-enforced screenshot diffing → **Story 10-2** (accessibility CI gates).
- axe-core / Playwright accessibility integration → **Story 10-2**.
- Breakpoint behaviour (compact-desktop auto-collapse, tablet drawer, phone single-column) → **Story 3-2** owns the responsive decisions; 3-1 ships the layout component baseline only.
- `IShortcutService` + conflict-warning analyzer → **Story 3-4** (command palette foundation story).

## Success Metric (observable)

- `dotnet test` passes with the four new test suites (shell bUnit, options validation, LocalStorageService unit, resource round-trip).
- Counter.Web sample boots with `FrontComposerShell` in place; header pinned to 48 px; theme toggle switches Light→Dark→System and persists across page refresh (manual verification via Aspire MCP browser, per `feedback_no_manual_validation.md` preference for automated flows — captured as a Playwright smoke test in Task 10.9).
- Zero new build warnings. No regression in the existing test suite from Stories 1-x / 2-x (exact baseline captured at Task 0.1; grep-based estimate pre-3-1 ≈ 533 `[Fact]`/`[Theory]` declarations).
- Bench: header render latency ≤ 16 ms on Counter.Web (Blazor Server) via existing E2E harness from Story 2-4.
