# Architecture Decision Records

## ADR-027: FrontComposerShell owns the top-level FluentLayout — MainLayout becomes a 3-line wrapper

**Status:** Accepted (3-1).

**Context:** Story 1-3 and the Counter.Web sample demonstrated that composing `FluentLayout` + `FluentProviders` + `Fluxor.Blazor.Web.StoreInitializer` at the adopter's `MainLayout.razor` works, but it re-exposes three framework-critical integrations to every adopter application. Three independent Story 1-x / 2-x dev notes (1-8 Fluent UI bootstrap note, 2-1 StoreInitializer placement reminder, 2-4 theme-service initialisation note) each called out a separate silent-failure class that landed on the adopter. Epic 3's UX spec codifies specific layout decisions (header pinned 48 px, form max 720 px, six color slots, breadcrumb + palette + theme + settings header composition) that must stay consistent across deployments for the zero-override invariant to hold at composition scale.

**Decision:** Ship `FrontComposerShell.razor` as a framework-owned Razor Class Library component that composes:
1. `<FluentLayout>` with Header / Navigation / Content / Footer `FluentLayoutItem`s.
2. `<FluentProviders />` as a sibling of the layout (Fluent UI v5 requires it for dialog/tooltip overlay mounting).
3. `<Fluxor.Blazor.Web.StoreInitializer />` as a sibling (one-time per render tree — placement documented in XML comment).
4. The five-area header composition (app title, breadcrumbs slot, Ctrl+K placeholder, theme toggle, settings placeholder).
5. The 48 px header-height override via scoped CSS.

The adopter's `MainLayout.razor` becomes `@inherits LayoutComponentBase\n<FrontComposerShell>@Body</FrontComposerShell>`.

**Rejected alternatives (L09 ≥ 2 required; 3 listed):**
1. **Adopter keeps their own FluentLayout and references Fc fragments (e.g., `<FcThemeToggle />`, `<FcCommandPaletteTrigger />`) individually.** Rejected because header composition — element order, spacing, breadcrumb position, responsive collapse rules — becomes per-adopter, defeating consistency-is-trust. Every adopter would debug the same Fluent UI v5 integration edge cases.
2. **Framework ships a `CascadingValue<ShellContext>` that configures styles but leaves layout to the adopter.** Rejected because the cascading approach requires adopters to still author the `FluentLayout` block, which re-exposes the `FluentProviders` + `StoreInitializer` placement risk. The value prop of `FrontComposerShell` IS the composition.
3. **Auto-inject `FrontComposerShell` via a Blazor hosting hook (source generator adds `App.razor` markup).** Rejected because it hides the shell from adopter source — surprising, hard to debug, and incompatible with Epic 6 customization (slot overrides assume the adopter sees the markup).

**Consequences:**
- Adopter onboarding friction drops: one component wraps three integrations.
- The `FrontComposerShell` parameter surface becomes an append-only contract — every Epic 3 and future story adding header elements does it via slot parameters, not by editing the shell internals.
- The shell component carries both `Components/Layout/` source + `wwwroot/js/fc-beforeunload.js` + `FcShellResources.resx`. Shell package grows ~6 KB on first ship — acceptable.
- Counter.Web `MainLayout.razor` regression: removing the inline FluentLayout + `IThemeService.SetThemeAsync` call loses no functionality (both move into the shell) but requires the sample-refresh E2E test in Task 10.9 to certify.

---

## ADR-028: IThemeService (Fluent UI) is the theme applier; Fluxor ThemeState is the tenant-scoped cache

**Status:** Accepted (3-1).

**Context:** Fluent UI v5's `IThemeService` has two overlapping persistence paths:
1. `SetThemeAsync(ThemeSettings)` + `SetThemeAsync(Theme)` — explicit-apply, does not auto-persist.
2. Internal `SwitchThemeAsync`, `ClearStoredThemeSettingsAsync` — implies Fluent UI persists theme to its own `localStorage` key.

FrontComposer's Story 1-3 already ships `FrontComposerThemeState` + `ThemeEffects.HandleThemeChanged` persisting to `IStorageService` under a `{tenantId}:{userId}:theme` key. Double-persistence causes restore drift: two writers, two keys, order-dependent hydration, user sees intermittent flashes on reload.

**Decision:** Use `IThemeService` as a stateless APPLIER — we call `SetThemeAsync(ThemeSettings)` explicitly whenever we want Fluent UI to rerender the theme. The single source of truth for persistence is our Fluxor `FrontComposerThemeState` + `IStorageService`. We never call the auto-persisting overloads (`SwitchThemeAsync`, `SetThemeAsync(ThemeMode)` single-arg path, `ClearStoredThemeSettingsAsync` which would erase Fluent's store-key we never used).

The ThemeSettings we build on every apply: `new ThemeSettings(Color: opts.AccentColor, HueTorsion: 0, Vibrancy: 0, Mode: themeValueToFluentMode(state.CurrentTheme), IsExact: true)` where `themeValueToFluentMode(System)` resolves to the OS preference via `FcSystemThemeWatcher`'s latest value (cached in the watcher component, not in global state — single responsibility).

**Rejected alternatives:**
1. **Let Fluent UI persist to its own localStorage key (via `SwitchThemeAsync`).** Rejected because the key is global (`fluentui-settings` or similar) — no tenant/user scoping. Epic 7 + memory feedback `feedback_tenant_isolation_fail_closed.md` require per-(tenant, user) isolation. Cross-tenant theme leaks on shared devices.
2. **Drop Fluxor `ThemeState` entirely; rely only on Fluent UI persistence.** Rejected because other Fluxor features (density, navigation) need the same state-observable-across-components pattern; deleting one Fluxor feature for the theme special-case fractures the state architecture.
3. **Custom `IThemeApplier` abstraction in Contracts + per-feature implementation.** Rejected because it re-implements `IThemeService` with no new capability — a zero-override violation. Fluent UI's API is good enough.

**Consequences:**
- Shell component's `OnAfterRenderAsync(firstRender: true)` does the initial apply after Fluxor hydration completes (Story 1-3 `AppInitializedAction` dispatch happens in `StoreInitializer`).
- System mode requires a reactive bridge: `FcSystemThemeWatcher` listens to media-query changes, calls `IThemeService.SetThemeAsync(ThemeMode.Dark|Light)` without changing `FrontComposerThemeState.CurrentTheme` (still `System`). The user's intent + OS preference compose at render time.
- Test discipline: every `IThemeService.SetThemeAsync` call is observable in tests via NSubstitute `Received.InOrder` — regressions that double-call or miss a call fail fast.
- Fluent UI version upgrades: if `ThemeSettings` gains new fields, 3-1 adds them with defaults; if `IThemeService` renames `SetThemeAsync`, we update one call site in `FrontComposerShell` + `ThemeEffects`. The blast radius stays tiny.

---

## ADR-029: LocalStorageService replaces StorageKeys static defaults with IUserContextAccessor fail-closed reads

**Status:** Accepted (3-1).

**Context:** `StorageKeys.DefaultTenantId = "default"` and `DefaultUserId = "anonymous"` were introduced in Story 1-3 as placeholders before authentication was implemented (Epic 7). They enable a naive sample-mode where every storage key collapses to `"default:anonymous:theme"`, `"default:anonymous:density"`, etc. When Epic 7 lands (or before, via a bespoke OIDC middleware in an adopter deployment), these defaults become cross-tenant collision points: User A's persisted preferences on shared kiosk devices leak into User B's session until localStorage is cleared.

Story 2-2 Decision D31 established the fail-closed precedent for `LastUsedValueProvider` (skip when tenant/user null), and `NullUserContextAccessor` is registered as the DI default.

**Decision:** Delete `StorageKeys.DefaultTenantId` + `DefaultUserId`. Every caller of `StorageKeys.BuildKey(tenantId, userId, feature[, discriminator])` resolves the two segments from the actual `IUserContextAccessor` interface — which is a pair of flat properties `string? TenantId { get; }` and `string? UserId { get; }` (see `src/Hexalith.FrontComposer.Contracts/Rendering/IUserContextAccessor.cs:17-31`). There is no `.Current` accessor and no nested `UserContext`/`User` record — earlier drafts of this ADR referenced a shape that does not exist; the shape below is authoritative. Callers short-circuit — logging `HFC2105` Information — when either property fails `string.IsNullOrWhiteSpace`, which is the only guard consistent with the interface's XML contract (null / empty / whitespace are semantically equivalent). The effects (`ThemeEffects`, `DensityEffects` in 3-1 scope; Epic 3 later stories' effects follow suit) perform the check; `LocalStorageService` never receives a malformed key.

```csharp
string? tenantId = userContextAccessor.TenantId;
string? userId = userContextAccessor.UserId;
if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId))
{
    logger.LogInformation(
        "{DiagnosticId}: Theme persistence skipped — null/empty/whitespace tenant or user context.",
        FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    return;
}
string key = StorageKeys.BuildKey(tenantId, userId, "theme");
```

**Rejected alternatives:**
1. **Keep static defaults but log a Warning to encourage adopter action.** Rejected because logged warnings become ambient noise after the first week; the default stays wired in production until something breaks. Fail-closed is the only safe default for a multi-tenant framework.
2. **Fall back to a per-browser-fingerprint tenant ID (e.g., hash of user agent + timezone).** Rejected because browser fingerprinting is (a) privacy-hostile, (b) unstable across browser updates, (c) not actually tenant-isolating (just user-isolating at best). Doesn't solve the problem.
3. **Make `IUserContextAccessor.Current` non-nullable, requiring Epic 7 to land first.** Rejected because Epic 7 is scheduled after Epic 3-6 per the epic-list; forcing Epic 7 first disrupts the dependency graph.

**Consequences:**
- Counter.Web sample already registers `DemoUserContextAccessor` (Story 2-2 Task 9.4), so it continues to persist preferences — no sample regression.
- Brand-new adopter deployments without a user context accessor see preferences reset on every refresh (fail-closed behaviour). Documented in the shell's XML `<remarks>` and in the quickstart.
- Test assertions in `ThemeEffectsScopeTests.cs` + `DensityEffectsScopeTests.cs` cover the null-tenant / null-user / both-null / both-present matrix so the contract is in CI.
- `HFC2105` (Information severity) gives adopters a way to grep their logs and find preferences that were attempted-but-dropped.
- No migration needed for existing stored keys: the old `"default:anonymous:theme"` keys simply stop being read/written — they orphan in localStorage and eventually age out via LRU.
- Story 9-4 owns the escalation to `Warning` + `[SuppressHFC2105]` analyzer (documented in Known Gap G3).

---

## ADR-030: IStorageService lifetime migration — Singleton → Scoped

**Status:** Accepted (3-1).

**Context:** `AddHexalithFrontComposer` currently registers `services.AddSingleton<IStorageService, InMemoryStorageService>()` (`src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs:135`). The `InMemoryStorageService` implementation is a process-wide dictionary — convenient for tests and Story 1-3's initial Fluxor persistence story, but wrong for real browser-backed storage because:

1. **`LocalStorageService` requires `IJSRuntime`.** `IJSRuntime` is registered as **Scoped** in Blazor Server (per-circuit) and Singleton-equivalent per-app in Blazor WebAssembly. A Singleton consuming a Scoped dependency is a DI validation error under `ServiceProviderOptions.ValidateScopes = true`.
2. **Circuit-scoped state.** LRU tracking, pending-write channels, `DotNetObjectReference` disposal, and the drain-worker cancellation token all have circuit lifetimes. Singleton lifetime would either leak across circuits (Server) or behave inconsistently between render modes.
3. **`TryAddScoped` is not a safe swap.** The existing `AddSingleton<IStorageService, InMemoryStorageService>()` descriptor is already in the service collection; `TryAddScoped<IStorageService, LocalStorageService>()` would silently no-op (the Singleton wins). The swap must explicitly remove the old registration before adding the new one.

**Decision:**

1. **Remove the existing Singleton registration** in `AddHexalithFrontComposer` via `services.RemoveAll<IStorageService>()` (or an equivalent descriptor-by-descriptor removal) BEFORE adding the Scoped one.
2. **Register `LocalStorageService` as `Scoped`** via `services.AddScoped<IStorageService, LocalStorageService>()`. Use `AddScoped`, NOT `TryAddScoped`, so the registration is authoritative after the `RemoveAll`.
3. **Audit every consumer of `IStorageService` in the codebase** (Task 2.0) and confirm each is Scoped or Transient. Any captured-Singleton consumer must migrate to Scoped OR take `IServiceScopeFactory` and create a scope on demand. As of 2026-04-17 the known consumers are `ThemeEffects` (Scoped — Fluxor effects), `DensityEffects` (Scoped), and any DataGrid state code in `State/DataGridNavigation/` (verify). Test hosts replace via `services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>())`.
4. **Enable `ValidateScopes` in the Counter.Web sample.** Confirms at boot that no Singleton holds an `IStorageService` capture. If a future regression introduces one, startup fails loudly instead of shipping a time-bomb.

**Rejected alternatives (L09 ≥ 2 required; 3 listed):**

1. **`TryAddScoped` without removing the Singleton.** Rejected because `TryAdd*` is a no-op when a matching `ServiceType` descriptor already exists. The Singleton would win, `LocalStorageService` would never resolve, and `localStorage` would silently not persist. This is the default failure mode if Task 2.3 is written without the `RemoveAll`.
2. **Keep `IStorageService` as Singleton and make `LocalStorageService` resolve `IJSRuntime` lazily per-call via `IServiceScopeFactory`.** Rejected because it pushes DI complexity into infrastructure code, undermines ValidateScopes diagnostics, and conflicts with Fluxor effect patterns that are already Scoped. Simpler to align lifetimes than work around them.
3. **Register both `InMemoryStorageService` (Singleton, for non-browser hosts) and `LocalStorageService` (Scoped, for browser hosts) and let adopters pick via a config flag.** Rejected because render-mode detection at DI-build time is unreliable in Blazor Server + WASM hybrid scenarios (the mode is per-circuit), and dual registration creates the same confusion the zero-override invariant is meant to prevent. Tests override via `services.Replace` — a single uniform mechanism.

**Consequences:**

- Any adopter that captured `IStorageService` into a Singleton gets a startup-time `InvalidOperationException` under ValidateScopes; the fix is to move their consumer to Scoped (or Transient). Documented in the `AddHexalithFrontComposer` XML `<remarks>` as a breaking change since Story 1-3.
- The scope of impact is narrow — `IStorageService` is a relatively new abstraction and consumers are all inside the framework today.
- Tests keep using `InMemoryStorageService` because the test host registers it via `services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>())`. No test-fixture regressions.
- `InMemoryStorageService` itself moves nowhere — it stays in Contracts/Storage for cross-target reuse; only the DI lifetime changes. Adopters who explicitly want the in-memory implementation keep getting it by replacing after `AddHexalithFrontComposer`.
- A new test (Task 10.12) asserts that `IStorageService` resolves as Scoped and that distinct scopes produce distinct instances — cheap CI insurance against a future Singleton-shaped regression.
- **SemVer commitment:** this is a breaking DI contract change against Story 1-3's published behaviour. The 3-1 release ships as **v0.2.0-preview** (Minor bump per SemVer §8 — "pre-1.0, anything may change but we signal breaking changes via a Minor bump"). A patch-level release of this change would violate consumer expectations. `RELEASE-NOTES.md` documents the lifetime flip under "Breaking changes" with the migration recipe (move captures to Scoped or take `IServiceScopeFactory`).

---

## ADR-031: Six semantic color slots declared in FrontComposerShell.razor.css with CI-locked lifecycle-state binding table

**Status:** Accepted (3-1).

**Context:** Story 2-4 introduced lifecycle-state colors in `FcLifecycleWrapper.razor.css` via ad-hoc `var(--colorNeutralForeground1)` etc. references scattered across rules. Every future component that needs status color (Epic 4 `FcDesaturatedBadge`, Epic 6 customization gradient, Epic 3-5 home-directory badge counts) is headed toward the same ad-hoc pattern. Without a single source of truth, a downstream "make warnings more amber" request turns into a grep-and-replace across 8+ files — error-prone, breaks muscle memory, and silently drifts across dark/light themes because each rule re-resolves Fluent UI's token independently.

**Decision:** Declare six `--fc-color-*` CSS custom properties in `FrontComposerShell.razor.css` under the shell's `:host` block. Components downstream consume them via `var(--fc-color-success)` etc. — NEVER via direct Fluent UI token references. The mapping:

```css
:host {
    --fc-color-accent: var(--accent-base-color);
    --fc-color-neutral: var(--colorNeutralForeground1);
    --fc-color-success: var(--colorStatusSuccessForeground1);
    --fc-color-warning: var(--colorStatusWarningForeground1);
    --fc-color-danger: var(--colorStatusDangerForeground1);
    --fc-color-info: var(--colorStatusInfoForeground1);
}
```

Additionally, the command-lifecycle → slot binding table (Idle=Neutral / Submitting=Accent / Acknowledged=Neutral / Syncing=Accent / Confirmed=Success / Rejected=Danger) is snapshot-locked via `SlotMappingRegressionTests.cs` against a checked-in `.verified.txt` baseline. Any rename, remap, or new-slot addition fails CI until a reviewer re-approves the verified file.

**Rejected alternatives (L09 ≥ 2 required; 3 listed):**

1. **Inline CSS per component, each referencing Fluent UI tokens directly.** Rejected because every downstream component re-derives the semantic-status mapping, drifts on Fluent UI version bumps, and the "make warnings amber" request becomes a grep across N files with no CI gate. Matches the 2-4 ad-hoc pattern we're leaving behind.
2. **Adopter-defined tokens via `CascadingValue<ColorSlots>` or `FcShellOptions.ColorSlots` record.** Rejected because opening the semantic-status surface to adopter override is a zero-override violation — the whole point of a 6-slot palette is consistency across adopters. Epic 6's customization gradient will tackle per-tenant accent (G7) via a targeted escape hatch, not general override.
3. **One slot per lifecycle state instead of six semantic slots (e.g., `--fc-color-idle`, `--fc-color-submitting`, …).** Rejected because it couples CSS vocabulary to command-lifecycle domain vocabulary. Badges (4-2), notifications, general UI copy, and future capabilities don't map onto lifecycle states — semantic naming (accent/neutral/success/warning/danger/info) generalises.

**Consequences:**

- `FcLifecycleWrapper` (Story 2-4) can migrate to `var(--fc-color-*)` in a non-breaking follow-up PR; the visual result is identical because the slots resolve to the same Fluent UI tokens the wrapper already used.
- `SlotMappingRegressionTests.BindingTable.verified.txt` becomes the single change-control gate for slot semantics. A reviewer re-approves the baseline via the Verify.XunitV3 CLI, which creates a visible diff in the PR.
- Dark/light theme switch automatically cascades: Fluent UI flips its underlying tokens; our `var(--fc-color-*)` references re-resolve on the next render. No duplicated theme rules needed in FrontComposer.
- Adding a 7th slot (e.g., `--fc-color-selection`) is a non-breaking Minor bump IF it doesn't remap the existing six. A 10-2 type-specimen view will surface the need if it exists.
- Cross-story stability: Epic 4 badge component references `var(--fc-color-warning)` for "stale data" indicators; Epic 6 customization uses the same six variables as the customization seam. Locks the grammar once.

---

## ADR-032: Header placeholder triggers are hidden via compile-away guard, not rendered as aria-disabled buttons

**Status:** Accepted (3-1).

**Context:** Epic 3's shell header specifies five interactive elements (left-to-right): app title, breadcrumbs, Ctrl+K command-palette trigger, theme toggle, settings trigger. Stories 3-3 and 3-4 own the settings dialog and command palette respectively — neither ships until mid-Epic 3. The naive approach is to render the Ctrl+K and Settings buttons NOW with `aria-disabled="true"` and a click handler that logs "feature coming in Story 3-x", so the header layout is visually complete through the epic.

Sally's party-mode review flagged this as a trust-erosion pattern: `aria-disabled="true"` reads to screen readers and keyboard users as "this feature exists but is currently unavailable" — which is a lie when the feature doesn't exist at all. Priya's 90-minute evaluation window is the audience who sees dead chrome and concludes the framework ships half-finished features.

**Decision:** Hide the Ctrl+K and Settings placeholder buttons in 3-1 via a compile-away Razor guard (`@if (false) { ... }` or equivalent feature flag that resolves to `false` at compile time). The buttons are **absent from the DOM**, not present-with-aria-disabled. The shell's parameter surface (D4) still includes `HeaderStart` / `HeaderEnd` slots so Stories 3-3 and 3-4 can inject the real triggers via a parameter (non-breaking, append-only contract). When 3-3 ships the settings dialog, Story 3-3 removes the `@if (false)` guard around its own button and exposes a shell parameter (or injects via HeaderEnd). Same for 3-4 Ctrl+K.

**Rejected alternatives (L09 ≥ 2 required; 3 listed):**

1. **Render `aria-disabled="true"` placeholder buttons that log on click.** Rejected because it fails accessibility truth-in-advertising (Sally's finding) and erodes onboarding-window trust (Priya persona). The button claims "disabled" status which means "temporarily unavailable" in a11y grammar — our state is "unimplemented", not "unavailable".
2. **Feature flag (`FcShellOptions.ShowCommandPaletteTrigger` defaulting to `false`) gating render.** Rejected because it adds options surface for a transitory state, requires documentation, and introduces a configuration path that later stories have to explicitly remove (or leave as a zombie option forever). `@if (false)` is a single-line deletion when the real feature ships — no ambient complexity.
3. **Render the buttons but with `hidden` attribute.** Rejected because `hidden` is still in the DOM, still serialised over the wire in Blazor Server, and a user with browser devtools sees the element and wonders about its purpose. Compile-away guard produces zero-DOM output.

**Consequences:**

- Stories 3-3 and 3-4 each include a single-line edit to `FrontComposerShell.razor` (remove their `@if (false)` guard) as part of their own PRs — traceable in git history as the feature activation point.
- `FrontComposerShellTests.cs` (Task 10.1) asserts the placeholder buttons are ABSENT from the rendered DOM — turns the party-mode finding into a CI regression gate. If a future PR re-introduces aria-disabled placeholders, the test fails.
- The shell's parameter surface (D4 append-only) remains stable — adding the real triggers in 3-3 / 3-4 does NOT require shell parameter changes. Consumers who adopted 3-1's parameter set continue to work.
- Sets a pattern for future epic-transitional dead chrome: when Story X depends on Story Y that hasn't shipped, hide, don't aria-disable.

---

## ADR-033: IStringLocalizer<FcShellResources> replaces IFluentLocalizer in the framework resource-resolution path

**Status:** Accepted (3-1).

**Context:** Epic 3.1 AC (UX-DR60) text references `IFluentLocalizer` — Fluent UI v5's own localisation abstraction — for framework-generated UI strings (app title, theme toggle labels, aria labels, etc.). `IFluentLocalizer` is designed for Fluent UI's own component strings: its cadence follows Fluent UI releases, its extension points are internal to `Microsoft.FluentUI.AspNetCore.Components`, and overriding a single string requires subclassing Fluent UI's internal pipeline.

FrontComposer's framework-generated strings have a different cadence (Hexalith framework releases), different audiences (FrontComposer adopters, not Fluent UI consumers), and different governance (resource parity enforced in FrontComposer CI, not Fluent UI's). Shadowing Fluent UI's localisation pipeline means every time Fluent UI bumps, we verify our strings aren't accidentally clobbered — ongoing maintenance cost for zero benefit.

Party-mode review (Winston + Murat + Sally + Amelia converged): the Story is right, the Epic document is the one that drifted. Recording the divergence in a binding ADR prevents re-litigation in a future retrospective.

**Decision:** Use `IStringLocalizer<FcShellResources>` (the standard `Microsoft.Extensions.Localization` idiom) for every FrontComposer-framework-generated UI string. `FcShellResources.resx` (EN) + `FcShellResources.fr.resx` are the canonical sources. Fluent UI's own `IFluentLocalizer` pipeline remains UNTOUCHED — we do not shadow or subclass it. Adopters who want to add a fourth language (e.g., Japanese) register their own `IStringLocalizer<FcShellResources>` implementation via `services.Replace` OR drop a compiled `FcShellResources.ja.resx` into their deployment. Fluent UI's component strings follow Fluent UI's governance independently.

The divergence from Epic 3.1 AC text is acknowledged as a documentation lag — Epic 3.1 AC will be amended in a follow-up PR (non-blocking for 3-1).

**Rejected alternatives (L09 ≥ 2 required; 3 listed):**

1. **Use `IFluentLocalizer` as Epic 3.1 AC specifies.** Rejected because it couples FrontComposer's string governance to Fluent UI's release cadence, requires subclassing an internal pipeline to override a string, and provides zero benefit over `IStringLocalizer<T>`. The Epic AC reference was a drift from party-mode alignment.
2. **Ship our own `IFcLocalizer` abstraction that wraps `IStringLocalizer<T>`.** Rejected because it re-implements `IStringLocalizer<T>` with no new capability. Adopters already know the `IStringLocalizer<T>` idiom from ASP.NET Core conventions; a custom wrapper adds a learning curve for no benefit.
3. **Hybrid — use `IFluentLocalizer` for Fluent-UI-derived strings + `IStringLocalizer<T>` for others.** Rejected because the split is contrived: what counts as "Fluent-UI-derived" vs. "framework-specific" is a policy call with no clean boundary, and every adopter needs to learn two resolution paths.

**Consequences:**

- `FcShellResources.resx` + `.fr.resx` ship with 8 keys in 3-1; adopter deployments that compile these into their assembly can override individual strings via the standard .NET resource-fallback pipeline (culture → neutral → default).
- Fourth-language adopters (Japanese, Spanish, etc.) follow the standard `IStringLocalizer<T>` pattern — familiar to any ASP.NET Core developer.
- Fluent UI version upgrades don't touch our resource pipeline. Fluent UI's own strings continue to resolve via `IFluentLocalizer` as before.
- Epic 3.1 AC divergence: a follow-up PR amends the Epic AC text to reference `IStringLocalizer<FcShellResources>`. Documented as a non-blocking ticket in Task 11.4 deferred-work review.
- `FcShellResourcesTests.CanonicalKeysHaveFrenchCounterparts` locks EN↔FR key parity. Adopter-supplied resources (e.g., FR key override) don't need to satisfy this test because they compose via the standard fallback.

---

## ADR-034: Scoped CSS bundle filename contract for the FrontComposerShell RCL

**Status:** Accepted (3-1).

**Context:** Blazor Razor Class Libraries emit a scoped-CSS bundle under the `_content/{AssemblyName}/` static web asset path. The bundle filename is `{AssemblyName}.styles.css` per current Blazor conventions (e.g., `Hexalith.FrontComposer.Shell.styles.css`). The `FrontComposerShell.razor.css` scoped CSS — which declares the six `--fc-color-*` CSS custom properties (D13 / ADR-031) AND the `--layout-header-height: 48px` override (D20) — ships through this bundle. Every adopter's `App.razor` must `<link>` the bundle in its `<head>` for the CSS variables to inject.

Pre-mortem Analysis flagged the scoped-CSS trap: bUnit renders scoped CSS via its own pipeline, so AC4 tests can pass even when the browser render is broken (because the `<link>` is wrong or omitted). A filename mismatch causes every downstream component that reads `--fc-color-*` to fall back to `currentColor` or empty — silently, with no diagnostic.

Task 9.2 acknowledged uncertainty about the exact filename. A story-level failure can slip through if Task 9.2 guesses wrong.

**Decision:**

1. **Pin the bundle href as `_content/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.styles.css`** in the adopter `App.razor` `<link>`. This is the Blazor RCL convention `{AssemblyName}.styles.css` applied to the Shell RCL.
2. **Verify the filename in Task 0.6 BEFORE any AC4 test runs.** The verification: run `dotnet build` on `Hexalith.FrontComposer.Shell.csproj`, list the files under `bin/**/_content/Hexalith.FrontComposer.Shell/`, and confirm `Hexalith.FrontComposer.Shell.styles.css` is present. If the RCL build emits a different filename (e.g., due to a future Blazor tooling change), escalate BEFORE continuing — do not silently adjust the `<link>` guess.
3. **Add a Counter.Web-level smoke check in Task 10.9's Playwright E2E** that reads `getComputedStyle(document.body).getPropertyValue('--fc-color-success')` and asserts the value is non-empty after `FrontComposerShell` mounts. This catches the "bundle filename right but `<link>` placement wrong" class of bug (e.g., `<link>` ordered before Fluent UI's stylesheet causes token resolution to fail).

**Rejected alternatives (L09 ≥ 2 required; 3 listed):**

1. **Ship a custom bundle filename (e.g., `fc-shell.bundle.css`) via explicit `<StaticWebAssetBasePath>` + MSBuild target overrides.** Rejected because it fights Blazor conventions for no benefit. Custom bundle names break Blazor tooling assumptions (scoped CSS attribute generation, hot-reload) and leave the next adopter wondering why our RCL is special.
2. **Inline the six `--fc-color-*` CSS custom properties in a `<style>` block inside `FrontComposerShell.razor`.** Rejected because it bypasses scoped CSS entirely, inflates the server-rendered HTML (every page-render ships the CSS), and loses the scoped-CSS attribute that isolates rules to the shell component. Fluent UI's theme tokens still cascade, but the semantic-slot abstraction loses its component-scoped nature.
3. **Trust the bUnit test as sufficient verification.** Rejected because bUnit's scoped-CSS rendering is subtly different from the browser — bUnit synthesises component-scoped attributes differently, and CSS variable cascading through `:host` can behave differently in a test harness. Pre-mortem Analysis #1 specifically identifies this as a silent-failure vector.

**Consequences:**

- Counter.Web's `App.razor` `<link>` hard-codes the RCL-convention filename. A future Blazor tooling change that renames the bundle breaks every adopter simultaneously — documented as a framework-upgrade-check item in `RELEASE-NOTES.md`.
- Task 0.6 becomes a story-level hard gate: if the filename is wrong, the story is blocked BEFORE any AC runs. No wasted AC4 test cycles on a mis-configured bundle.
- Playwright E2E smoke check (Task 10.9 extension) runs in an actual browser with an actual `<link>` load — the one verification path that exercises the full scoped-CSS pipeline as an adopter would.
- Adopter documentation: the shell component's XML `<remarks>` spell out the `<link>` requirement + ordering rule (AFTER Fluent UI's stylesheet) so new-adopter onboarding doesn't depend on reading ADR-034.

---
