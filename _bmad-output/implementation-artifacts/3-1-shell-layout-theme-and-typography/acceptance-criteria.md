# Acceptance Criteria

## AC1: FrontComposerShell renders three regions with header pinned to 48 px and form max 720 px

**Given** the application shell renders via `<FrontComposerShell>@Body</FrontComposerShell>` from Counter.Web's `MainLayout.razor`
**When** the layout is inspected in the DOM
**Then** a single `<FluentLayout>` contains four `<FluentLayoutItem>` children — `Area="LayoutArea.Header"`, `Area="LayoutArea.Navigation"` (Width=`220px`, empty in 3-1 — Story 3-2 populates), `Area="LayoutArea.Content"` (no max-width on the layout item — forms self-limit via `FcShellOptions.FullPageFormMaxWidth` per Story 2-2 D26), `Area="LayoutArea.Footer"` (single-line copyright `Hexalith FrontComposer © {Year}`)
**And** the Header's `<FluentLayoutItem>` sets `Height="48px"` AND the component's scoped CSS sets `:host { --layout-header-height: 48px; }` (both mechanisms — Height parameter + CSS var — because Fluent UI v5 uses whichever is more specific; belt-and-braces locks the spec AC)
**And** the Header contains the following DOM order, rendered via a single top-level `<FluentStack Horizontal="true" VerticalAlignment="VerticalAlignment.Center" HorizontalAlignment="HorizontalAlignment.SpaceBetween">` (the SpaceBetween distribution keeps `HeaderStart + AppTitle` left-aligned and `FcThemeToggle + HeaderEnd` right-aligned so Story 3-2's breadcrumbs slot drops cleanly into the middle without a layout redo — elicitation War-Room output): (1) `HeaderStart` render fragment slot (empty by default; Story 3-2 hamburger lands here), (2) `<FluentText Typo="@Typography.AppTitle">` resolving `AppTitle` parameter or `FcShellResources.AppTitle`, (3) `HeaderCenter` NOT present in 3-1 — Story 3-2 introduces, (4) `<FcThemeToggle />`, (5) `HeaderEnd` render fragment slot (empty by default). The Ctrl+K command-palette trigger and the Settings trigger are **NOT rendered in 3-1** — they are wrapped in a compile-away guard (`@if (false)` or equivalent feature flag) per D26 + ADR-032 and become live in Stories 3-4 and 3-3 respectively, without changing the shell's parameter surface
**And** when the `Navigation` render fragment parameter is null (no adopter-supplied nav content — a brand-new adopter without the D25 pass-through block), the Navigation `<FluentLayoutItem>` is either omitted from the rendered markup OR has `Hidden="true"` set so Content spans edge-to-edge — prevents an ugly empty 220 px rail during the 3-1/3-2 sprint gap (elicitation War-Room output)
**And** `<FluentProviders />` renders as a sibling AFTER the `<FluentLayout>`
**And** `<Fluxor.Blazor.Web.StoreInitializer />` renders as a sibling AFTER `<FluentProviders>` (order matters — Fluxor must mount after Fluent UI providers so effects that depend on Fluent UI services resolve correctly)
**And** the Content `<FluentLayoutItem>` renders `ChildContent` as its only child wrapped in `<FcSystemThemeWatcher>` (zero-DOM wrapper)

**Verification:** `FrontComposerShellTests.cs` tests 1-4 (bUnit). Navigation-hide-when-null verified in an added bUnit assertion (Task 10.1): rendering `<FrontComposerShell>@Body</FrontComposerShell>` without a `<Navigation>` render fragment asserts the Navigation `<FluentLayoutItem>` is absent from `cut.Markup` OR has `hidden` attribute set.

---

## AC2: Default accent is #0097A7, overridable via FcShellOptions.AccentColor with hex-format validation

**Given** the application boots without `Hexalith:Shell:AccentColor` in configuration
**When** `IOptions<FcShellOptions>.Value.AccentColor` is read
**Then** the value is `"#0097A7"` (exact, case-insensitive)
**And** `FrontComposerShell.OnAfterRenderAsync(firstRender: true)` calls `IThemeService.SetThemeAsync(new ThemeSettings(Color: "#0097A7", HueTorsion: 0, Vibrancy: 0, Mode: themeModeForCurrentThemeState, IsExact: true))`

**Given** an adopter sets `"Hexalith:Shell:AccentColor": "#512BD4"` in `appsettings.Production.json`
**When** the application boots with `ValidateOnStart`
**Then** startup succeeds AND `FrontComposerShell` calls `SetThemeAsync` with `Color: "#512BD4"`

**Given** an adopter sets `"Hexalith:Shell:AccentColor": "teal"` or `"rgb(0, 151, 167)"` or `"#09A7"` or empty string
**When** the application boots
**Then** `OptionsValidationException` is thrown at `ValidateOnStart` with a message matching "`AccentColor must be a 6-digit hex color`" (from the `[RegularExpression]` `ErrorMessage`)

**Verification:** `FcShellOptionsValidationTests.cs` new `AccentColor_*` tests (Task 10.8); `FrontComposerShellTests.cs` test 6 asserts the `SetThemeAsync` call parameter.

---

## AC3: Theme toggle applies Light/Dark/System instantly via IThemeService and persists via LocalStorageService

**Given** `FcThemeToggle` is rendered in the header and `FrontComposerThemeState.CurrentTheme = ThemeValue.System`
**When** the user opens the menu and selects "Dark"
**Then** `IDispatcher.Dispatch(new ThemeChangedAction(correlationId, ThemeValue.Dark))` is invoked (correlation ID from `IUlidFactory` per Story 2-3)
**And** the Fluxor reducer updates `CurrentTheme = Dark`
**And** `ThemeEffects.HandleThemeChanged` calls `IThemeService.SetThemeAsync(new ThemeSettings(Color: opts.AccentColor, HueTorsion: 0, Vibrancy: 0, Mode: ThemeMode.Dark, IsExact: true))`
**And** if `IUserContextAccessor.TenantId` and `IUserContextAccessor.UserId` (flat string? properties per `src/Hexalith.FrontComposer.Contracts/Rendering/IUserContextAccessor.cs:17-31`) both pass `!string.IsNullOrWhiteSpace(...)`, the effect calls `IStorageService.SetAsync("{tenant}:{user}:theme", ThemeValue.Dark)` (fire-and-forget per D9)
**And** within 16 ms (observable via `TimeProvider`-based bUnit test — D18) the DOM reflects Fluent UI's dark variant

**Given** the browser is refreshed after the Dark selection
**When** `Fluxor.Blazor.Web.StoreInitializer` fires `AppInitializedAction` on app bootstrap
**Then** `ThemeEffects.HandleAppInitialized` calls `IStorageService.GetAsync<ThemeValue?>(key)`, receives `Dark`, dispatches `ThemeChangedAction(correlationId, Dark)`
**And** `FrontComposerShell.OnAfterRenderAsync(firstRender: true)` observes `CurrentTheme = Dark` and calls `SetThemeAsync` with `Mode: Dark`

**Given** the user selects "System" from the toggle
**When** the media query `(prefers-color-scheme: dark)` evaluates to `true`
**Then** `FcSystemThemeWatcher` invokes `OnSystemThemeChangedAsync(true)` via its `DotNetObjectReference`
**And** because `CurrentTheme == ThemeValue.System`, the watcher dispatches a lightweight action (or direct call) that invokes `IThemeService.SetThemeAsync(ThemeMode.Dark)` — `FrontComposerThemeState.CurrentTheme` remains `System`
**And** if the OS switches to Light mode, the media query fires with `false`, the watcher calls `SetThemeAsync(ThemeMode.Light)`

**Given** `IUserContextAccessor.TenantId` or `IUserContextAccessor.UserId` is null, empty, or whitespace (e.g., no `DemoUserContextAccessor` wired — `NullUserContextAccessor` is the fail-closed default from Story 2-2 D31)
**When** the user selects a theme
**Then** `ThemeEffects.HandleThemeChanged` logs `HFC2105` Information AND skips `IStorageService.SetAsync`
**And** `IThemeService.SetThemeAsync` still fires — in-memory theme change works; only persistence skips

**Verification:** `FcThemeToggleTests.cs` 5 tests; `FcSystemThemeWatcherTests.cs` 4 tests; `ThemeEffectsScopeTests.cs` 4 tests; `ShellThemeToggleE2ETests.cs` Playwright smoke (Task 10.9).

---

## AC4: Six semantic color slots expose CSS custom properties with lifecycle-state mapping

**Given** `FrontComposerShell.razor.css` is compiled and scoped
**When** the DOM of any page rendered inside `FrontComposerShell` is inspected
**Then** the following CSS custom properties are defined on the shell's root element and available to descendant components:
- `--fc-color-accent: var(--accent-base-color)` (accent is overridable via `FcShellOptions.AccentColor` per AC2)
- `--fc-color-neutral: var(--colorNeutralForeground1)`
- `--fc-color-success: var(--colorStatusSuccessForeground1)`
- `--fc-color-warning: var(--colorStatusWarningForeground1)`
- `--fc-color-danger: var(--colorStatusDangerForeground1)`
- `--fc-color-info: var(--colorStatusInfoForeground1)`

**And** the command-lifecycle → slot mapping table is locked via `SlotMappingRegressionTests.cs` snapshot, matching:
| Lifecycle state | Slot |
|---|---|
| `Idle` | Neutral |
| `Submitting` | Accent |
| `Acknowledged` | Neutral |
| `Syncing` | Accent |
| `Confirmed` | Success |
| `Rejected` | Danger |

(Info and Warning slots exist for future badge mapping in Stories 4-2 + 3-5 but are not bound to lifecycle states.)

**Verification:** `SlotMappingRegressionTests.cs` (Task 7.2) verifies a serialised representation of the binding table against a checked-in `.verified.txt` baseline.

---

## AC5: Typography static class exposes 9 constants with version-pinned mappings

**Given** `Hexalith.FrontComposer.Contracts.Rendering.Typography`
**When** an adopter or generated component references a constant
**Then** the following 9 `public static readonly Typography` fields resolve to the corresponding Fluent UI v5 `Typography` enum values:
- `Typography.AppTitle` → `Typography.Title1`
- `Typography.BoundedContextHeading` → `Typography.Subtitle1`
- `Typography.ViewTitle` → `Typography.Title3`
- `Typography.SectionHeading` → `Typography.Subtitle2`
- `Typography.FieldLabel` → `Typography.Body1Strong`
- `Typography.Body` → `Typography.Body1`
- `Typography.Secondary` → `Typography.Body2`
- `Typography.Caption` → `Typography.Caption1`
- `Typography.Code` → `Typography.Body1` **plus** a companion `TypographyStyle.CodeFontFamily` string = `"'Cascadia Code', 'Cascadia Mono', Consolas, 'Courier New', monospace"` for consumers to apply via `Style=`

**And** the class's XML comment documents the living-table policy:
- Patch version (x.y.Z → x.y.Z+1): no mapping changes.
- Minor version (x.Y.z → x.Y+1.0): mapping change requires changelog entry + before/after screenshot committed to `docs/typography-baseline/`.
- Major version (X.y.z → X+1.0.0): restructurable with migration notes.

**And** `Hexalith.FrontComposer.Contracts.ContractsMetadata.TypographyMappingVersion = "3.1.0"` is defined; `TypographyConstantsTests.cs` asserts the constants match the 3.1.0 table exactly. A drift (e.g., `ViewTitle` pointing at `Subtitle1`) fails the test.

**Verification:** `TypographyConstantsTests.cs` 2 tests (Task 10.10).

---

## AC6: LocalStorageService implements IStorageService with LRU eviction and beforeunload FlushAsync

**Given** `services.AddHexalithFrontComposer()` is called
**When** `IServiceProvider` is built
**Then** `IStorageService` resolves to `LocalStorageService` (Scoped lifetime per D18) — `InMemoryStorageService` is no longer the default for non-test hosts

**Given** `LocalStorageService.SetAsync(key, value)` is called
**When** observed from the caller's perspective
**Then** the returned task completes in ≤ 1 ms (fire-and-forget — the actual `InvokeVoidAsync("localStorage.setItem", ...)` happens on a drain worker)

**Given** `LocalStorageService.FlushAsync()` is called
**When** the drain worker has pending writes
**Then** the call awaits completion of all enqueued writes before returning

**Given** `FcShellOptions.LocalStorageMaxEntries = 500` (default) and 501 distinct keys have been set
**When** key #501 is being inserted
**Then** the entry with the oldest `TimeProvider.GetUtcNow().UtcTicks` is evicted via `InvokeVoidAsync("localStorage.removeItem", oldestKey)`
**And** an internal `Dictionary<string, long>` LRU tracker is updated atomically

**Given** the user navigates away from the page
**When** `beforeunload` fires
**Then** `window.onbeforeunload` → `fc-beforeunload.js` → `DotNetObjectReference.invokeMethodAsync("FlushAsync")` is called
**And** the drain completes within 200 ms (or the browser force-terminates; we do our best with no hard guarantee beyond Chrome/Edge grace window)

**Given** the user context is null (no tenant/user)
**When** any effect attempts to persist
**Then** the effect itself short-circuits BEFORE calling `LocalStorageService` (per AC3 + D8) — `LocalStorageService` never sees a malformed key

**Verification:** `LocalStorageServiceTests.cs` 9 tests (Task 10.4) via `BunitJSInterop`; `beforeunload` E2E covered in Task 10.9.

---

## AC7: EN + FR resource files resolve framework-generated UI strings via IStringLocalizer

**Given** `services.AddLocalization().AddHexalithShellLocalization()` is called
**When** `IStringLocalizer<FcShellResources>` is injected into a shell component
**Then** `CultureInfo.CurrentUICulture = new("en")` → `localizer["AppTitle"]` returns `"Hexalith FrontComposer"`
**And** `CultureInfo.CurrentUICulture = new("fr")` → `localizer["ThemeToggleAriaLabel"]` returns `"Changer de thème"`

**Given** `FcShellResources.resx` (EN) defines key `X`
**When** `FcShellResources.fr.resx` is inspected
**Then** key `X` is present (key parity enforced by `FcShellResourcesTests.CanonicalKeysHaveFrenchCounterparts`)

**Given** an adopter wants to add a third language
**When** they register their own `IStringLocalizer<FcShellResources>` via `services.Replace` or add a `FcShellResources.ja.resx` resource file compiled into their deployment
**Then** the shell resolves their strings without framework code changes

**Verification:** `FcShellResourcesTests.cs` 3 tests (Task 10.7).

---
