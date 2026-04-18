# Dev Notes

## Service Binding Reference

All additions in 3-1 happen inside `AddHexalithFrontComposer` (existing extension), the NEW `AddHexalithShellLocalization` extension, and the NEW `AddHexalithFrontComposerQuickstart` sugar (D28 — chains all three + `AddLocalization`). No new DI types outside these methods.

- `IStorageService` — Scoped per D18 (was Singleton). `LocalStorageService` for Server + WASM; tests override via `services.Replace` with `InMemoryStorageService`.
- `LocalStorageService` dependencies: `IJSRuntime`, `IOptions<FcShellOptions>`, `TimeProvider` (already registered in Story 2-4), `ILogger<LocalStorageService>` (logging pipeline already active).
- `IThemeService` — Fluent UI v5 auto-registered via `services.AddFluentUIComponents()` (Story 1-8).
- `IUserContextAccessor` — Scoped (Story 2-2 D31). `NullUserContextAccessor` is the fail-closed default; Counter.Web registers `DemoUserContextAccessor` (Story 2-2 Task 9.4).
- `IStringLocalizer<FcShellResources>` — resolved via `services.AddLocalization()` (adopter-owned) + the resource file conventions; no explicit registration needed per D24.
- `IUlidFactory` — Singleton (Story 2-3 D2/D3).

## FrontComposerShell Composition Diagram

```
┌─ <FrontComposerShell>
│
│  ┌─ <FluentLayout MobileBreakdownWidth="768">
│  │
│  │  ┌─ <FluentLayoutItem Area=Header Height="48px">
│  │  │    <FluentStack Horizontal VerticalAlignment=Center>
│  │  │    <FluentStack Horizontal VerticalAlignment=Center
│  │  │                 HorizontalAlignment=SpaceBetween>   ← AC1 addendum
│  │  │      ├─ @HeaderStart          (slot, default empty; Story 3-2 hamburger)
│  │  │      ├─ <FluentText Typo=@Typography.AppTitle>@AppTitle</>
│  │  │      ├─ (Ctrl+K trigger ABSENT in 3-1 per D26 / ADR-032 — Story 3-4 adds)
│  │  │      ├─ <FcThemeToggle />
│  │  │      ├─ (Settings trigger ABSENT in 3-1 per D26 / ADR-032 — Story 3-3 adds)
│  │  │      └─ @HeaderEnd            (slot, default empty)
│  │  │
│  │  ├─ <FluentLayoutItem Area=Navigation Width="220px" Hidden=@(Navigation is null)>
│  │  │    @Navigation                (slot, empty in 3-1 — Story 3-2 populates;
│  │  │                                 Hidden=true when null per AC1 Nav-hide addendum —
│  │  │                                 elicitation War-Room output)
│  │  │
│  │  ├─ <FluentLayoutItem Area=Content Padding=@Padding.All3>
│  │  │    <FcSystemThemeWatcher />   (zero-DOM; subscribes to prefers-color-scheme)
│  │  │    @ChildContent              (the @Body from adopter MainLayout)
│  │  │
│  │  └─ <FluentLayoutItem Area=Footer>
│  │       @Footer ?? "Hexalith FrontComposer © @DateTime.Now.Year"
│  │
│  ├─ <FluentProviders />            (Fluent UI overlay mounting)
│  └─ <Fluxor.Blazor.Web.StoreInitializer />
│
└─ (inside adopter MainLayout.razor:
     @inherits LayoutComponentBase
     <FrontComposerShell>@Body</FrontComposerShell>)
```

## Theme Application Flow

```
User picks Dark (FcThemeToggle)
   ↓ (dispatcher)
ThemeChangedAction(correlationId, Dark)
   ↓ (reducer)
FrontComposerThemeState { CurrentTheme = Dark }
   ↓ (effect HandleThemeChanged)
   ├─ Guard: IUserContextAccessor.Current?.{TenantId, User.Id}
   │    both present  → persist via IStorageService.SetAsync(key, Dark)
   │    either null   → log HFC2105 + skip persist
   │
   └─ IThemeService.SetThemeAsync(new ThemeSettings(
        Color:       Options.AccentColor,       // e.g., "#0097A7"
        HueTorsion:  0,
        Vibrancy:    0,
        Mode:        ThemeMode.Dark,             // explicit from Dark
        IsExact:     true))
        ↓
      Fluent UI rerenders — body data-theme attribute flips

Page refresh
   ↓
Fluxor.Blazor.Web.StoreInitializer → AppInitializedAction
   ↓ (effect HandleAppInitialized)
   ├─ Guard (same as above)
   ├─ stored = await IStorageService.GetAsync<ThemeValue?>(key)
   ├─ stored is null  → log HFC2106 + return (defaults apply)
   ├─ stored is Dark  → dispatch ThemeChangedAction(correlationId, Dark)
   │                      → reducer updates state → effect fires again to persist (idempotent)
   └─ FrontComposerShell.OnAfterRenderAsync(firstRender: true) reads current ThemeState
      and calls IThemeService.SetThemeAsync once so the first paint is correct

User selects System (CurrentTheme = System)
   ↓
FcSystemThemeWatcher subscribed via fc-prefers-color-scheme.js
   ↓ (media-query change fires OR initial subscription emits current value)
OnSystemThemeChangedAsync(isDark)
   ↓
Guard: if state.CurrentTheme != System → no-op
Guard pass → IThemeService.SetThemeAsync(ThemeMode.Dark or Light)
   (Fluxor state stays System; only Fluent UI's applied theme flips)
```

## LocalStorageService Contract Notes

```csharp
// Not sealed — adopters may wrap via decorator for cross-device sync (v2)
public class LocalStorageService(
    IJSRuntime js,
    IOptions<FcShellOptions> options,
    TimeProvider time,
    ILogger<LocalStorageService> logger) : IStorageService, IAsyncDisposable
{
    // ConcurrentDictionary (not Dictionary) — Blazor Server circuit async continuations can interleave
    // SetAsync calls, and Dictionary mutation during the OrderBy(kvp => kvp.Value).First() eviction scan
    // throws "Collection was modified during enumeration". Pre-mortem Analysis finding #2.
    private readonly ConcurrentDictionary<string, long> _lruTimestamps = new(StringComparer.Ordinal);
    private readonly Channel<PendingWrite> _writes = Channel.CreateUnbounded<PendingWrite>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _drainTask;

    // ctor starts _drainTask = Task.Run(DrainLoopAsync, _cts.Token)

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) {
        // Update LRU timestamp (read-touched)
        _lruTimestamps[key] = time.GetUtcNow().UtcTicks;
        string? json = await js.InvokeAsync<string?>("localStorage.getItem", ct, key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public Task SetAsync<T>(string key, T value, CancellationToken ct = default) {
        _lruTimestamps[key] = time.GetUtcNow().UtcTicks;
        EvictIfOverCap(); // synchronous O(n) scan + enqueue evict writes
        _writes.Writer.TryWrite(new PendingWrite(key, JsonSerializer.Serialize(value)));
        return Task.CompletedTask;
    }

    public async Task FlushAsync(CancellationToken ct = default) {
        // Signal drain to finish pending; do NOT Complete the writer permanently — we keep accepting writes.
        // Flush pattern: insert a sentinel + TaskCompletionSource, await the TCS.
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _writes.Writer.TryWrite(new PendingWrite(SentinelKey, string.Empty, tcs));
        await tcs.Task.WaitAsync(ct);
    }

    // [JSInvokable] for beforeunload handler mounted in FrontComposerShell
    // (actual JSInvokable sits on FrontComposerShell for DotNetObjectReference lifetime mgmt)
}
```

- **LRU eviction:** synchronous scan on every `SetAsync` — acceptable because `LocalStorageMaxEntries ≤ 10_000` per D15 validator.
- **Drain worker stop:** on `DisposeAsync`, cancel `_cts`, await `_drainTask`, dispose channel.
- **Concurrency:** `Channel.CreateUnbounded<T>(SingleReader = true, SingleWriter = false)` — multiple components can call `SetAsync` concurrently without locking; the drain worker is the single reader.
- **Serialization:** `JsonSerializer.Serialize(value)` — ThemeValue / DensityValue serialize as strings (enum); DataGrid state in 3-6 will serialize as records.

## Tenant/User Scope Migration (L03 fail-closed)

Before 3-1:
```csharp
string key = StorageKeys.BuildKey(
    StorageKeys.DefaultTenantId,   // "default"
    StorageKeys.DefaultUserId,     // "anonymous"
    "theme");
await storage.SetAsync(key, value);
```

After 3-1:
```csharp
string? tenantId = userContextAccessor.TenantId;
string? userId = userContextAccessor.UserId;
if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId)) {
    logger.LogInformation(
        "{DiagnosticId}: Theme persistence skipped — null/empty/whitespace tenant or user context.",
        FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
    return;
}
string key = StorageKeys.BuildKey(tenantId, userId, "theme");
await storage.SetAsync(key, value);
```

**Interface shape note:** `IUserContextAccessor` exposes `TenantId` and `UserId` as flat `string?` properties (see `src/Hexalith.FrontComposer.Contracts/Rendering/IUserContextAccessor.cs:17-31`). There is no `.Current`, no nested `UserContext`, no `User.Id`. The XML contract explicitly says null / empty / whitespace are semantically equivalent, so `IsNullOrWhiteSpace` is the only guard that honors the interface. Earlier drafts of this spec assumed a nested shape; the code above is authoritative.

Migration risk surface = 2 call sites (ThemeEffects + DensityEffects). Any future effect reaching for `StorageKeys.BuildKey` must follow the same guard — enforce via a code review checklist item until Epic 7 codifies via a non-null accessor interface.

## Typography API Surface

```csharp
namespace Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.FluentUI.AspNetCore.Components;

public static class Typography
{
    // Living table — see ContractsMetadata.TypographyMappingVersion for the pinned version.
    // Mapping changes: patch=forbidden, minor=allowed+changelog, major=restructurable.
    public static readonly Microsoft.FluentUI.AspNetCore.Components.Typography AppTitle = Microsoft.FluentUI.AspNetCore.Components.Typography.Title1;
    public static readonly Microsoft.FluentUI.AspNetCore.Components.Typography BoundedContextHeading = Microsoft.FluentUI.AspNetCore.Components.Typography.Subtitle1;
    public static readonly Microsoft.FluentUI.AspNetCore.Components.Typography ViewTitle = Microsoft.FluentUI.AspNetCore.Components.Typography.Title3;
    public static readonly Microsoft.FluentUI.AspNetCore.Components.Typography SectionHeading = Microsoft.FluentUI.AspNetCore.Components.Typography.Subtitle2;
    public static readonly Microsoft.FluentUI.AspNetCore.Components.Typography FieldLabel = Microsoft.FluentUI.AspNetCore.Components.Typography.Body1Strong;
    public static readonly Microsoft.FluentUI.AspNetCore.Components.Typography Body = Microsoft.FluentUI.AspNetCore.Components.Typography.Body1;
    public static readonly Microsoft.FluentUI.AspNetCore.Components.Typography Secondary = Microsoft.FluentUI.AspNetCore.Components.Typography.Body2;
    public static readonly Microsoft.FluentUI.AspNetCore.Components.Typography Caption = Microsoft.FluentUI.AspNetCore.Components.Typography.Caption1;
    public static readonly Microsoft.FluentUI.AspNetCore.Components.Typography Code = Microsoft.FluentUI.AspNetCore.Components.Typography.Body1;
}

public static class TypographyStyle
{
    public const string CodeFontFamily =
        "'Cascadia Code', 'Cascadia Mono', Consolas, 'Courier New', monospace";
}
```

**Consumption:**
```razor
<FluentText Typo="@Typography.ViewTitle">Order List</FluentText>
<FluentText Typo="@Typography.Code" Style="@($"font-family: {TypographyStyle.CodeFontFamily}")">
    ORD-1847
</FluentText>
```

## Fluent UI v5 Component Reference

Verify via `mcp__fluent-ui-blazor__get_component_details` at Task 0.2 (MCP version `5.0.0.26098` vs project `5.0.0-rc.2-26098.1` — same build-number prefix, treat as compatible but spot-check each API):

- `FluentLayout` — `Height`, `Width`, `MobileBreakdownWidth` (default 768), `GlobalScrollbar`, `NavigationDeferredLoading`. (D3, D20, AC1)
- `FluentLayoutItem` — `Area` (LayoutArea.{Header, Navigation, Content, Aside, Footer}), `Width`, `Height`, `Padding`, `Sticky`. (D3, AC1)
- `FluentProviders` — no parameters; placement sibling of FluentLayout. (D3)
- `IThemeService.SetThemeAsync(ThemeSettings)` — the single overload we use; takes `ThemeSettings` record with positional args `(string Color, double HueTorsion, double Vibrancy, ThemeMode Mode, bool IsExact)`. (D6, D28)
- `ThemeMode` enum — confirm values `Light / Dark / System` at Task 0.2. If FCv5 uses `Default` instead of `System`, update D23 watcher dispatch.
- `Typography` enum — confirm `Title1, Subtitle1, Title3, Subtitle2, Body1Strong, Body1, Body2, Caption1` present.
- `FluentMenuButton` — `IconStart`, child `<FluentMenuItem>` with `OnClick`. (D5)
- `FluentButton` — `Appearance=Appearance.Stealth` for icon-only header buttons. (AC1)
- `IStringLocalizer<T>` — standard `Microsoft.Extensions.Localization.IStringLocalizer<FcShellResources>` — not Fluent-specific. (D21)

## Files Touched Summary

**Contracts/** (modified):
- `FcShellOptions.cs` — +4 properties (Task 1.1, D1, D14)
- `Diagnostics/FcDiagnosticIds.cs` — +2 constants (Task 1.3)

**Contracts/** (new):
- `Rendering/Typography.cs` (Task 1.2, D2, D11)
- `Rendering/TypographyStyle.cs` (Task 1.2)
- `ContractsMetadata.cs` (Task 1.4)

**Shell/** (new):
- `Components/Layout/FrontComposerShell.razor[.cs][.css]` (Task 5.1, D3, D4, D6, D20)
- `Components/Layout/FcThemeToggle.razor[.cs]` (Task 6.1, D5, D7, D22)
- `Components/Layout/FcSystemThemeWatcher.razor[.cs]` (Task 6.2, D10, D23)
- `Infrastructure/Storage/LocalStorageService.cs` (Task 2.1, D9, D15, D16, D18)
- `Infrastructure/Storage/LocalStorageDrainWorker.cs` (Task 2.2)
- `wwwroot/js/fc-beforeunload.js` (Task 3.1, D17)
- `wwwroot/js/fc-prefers-color-scheme.js` (Task 3.2, D23)
- `Resources/FcShellResources.resx` + `FcShellResources.fr.resx` (Task 8.1, D12, D19)

**Shell/** (modified):
- `Extensions/ServiceCollectionExtensions.cs` — swap `IStorageService` default + add `AddHexalithShellLocalization` + add `AddHexalithFrontComposerQuickstart` sugar (Task 2.3, 2.4, 8.2, D18, D24, D28)
- `State/StorageKeys.cs` — delete DefaultTenantId + DefaultUserId (Task 4.1, D8)
- `State/Theme/ThemeEffects.cs` — IUserContextAccessor guard (Task 4.2, D8)
- `State/Density/DensityEffects.cs` — symmetric refactor (Task 4.3, D8)
- `_Imports.razor` — add two usings (Task 6.1)

**samples/Counter/Counter.Web/** (modified):
- `Components/Layout/MainLayout.razor` — collapse to `<FrontComposerShell>` (Task 9.1, D3)
- `Components/App.razor` — add scoped CSS link (Task 9.2)
- `Program.cs` — chain `AddHexalithShellLocalization` (Task 9.3, D24)
- `appsettings.Development.json` — expose `Hexalith:Shell:AccentColor` etc. (Task 9.4)

**tests/Hexalith.FrontComposer.Shell.Tests/** (new):
- `Components/Layout/FrontComposerShellTests.cs` (Task 10.1) — 7 tests (D26: placeholder buttons absent, one row cut)
- `Components/Layout/FrontComposerShellParameterSurfaceTests.cs` (Task 5.2 / 10.1) — 1 snapshot
- `Components/Layout/FcThemeToggleTests.cs` (Task 10.2) — 5 tests
- `Components/Layout/FcSystemThemeWatcherTests.cs` (Task 10.3) — 4 tests
- `Infrastructure/Storage/LocalStorageServiceTests.cs` (Task 10.4) — 9 tests
- `Infrastructure/Storage/IStorageServiceLifetimeTests.cs` (Task 10.12) — 1 DI test (ADR-030)
- `State/Theme/ThemeEffectsScopeTests.cs` (Task 10.5) — 5 tests (whitespace-segment case added)
- `State/Density/DensityEffectsScopeTests.cs` (Task 10.6) — 5 tests (whitespace-segment case added)
- `State/PersistencePrecedenceTests.cs` (Task 10.13) — 1 deterministic channel-level test (party-mode gap + Challenge-from-Critical-Perspective rewrite)
- `Extensions/AddHexalithFrontComposerQuickstartTests.cs` (Task 10.14) — 3 tests (D28 sugar extension)
- `Resources/FcShellResourcesTests.cs` (Task 10.7) — 3 tests
- `SlotMappingRegressionTests.cs` (Task 7.2) — 1 snapshot
- `EndToEnd/ShellThemeToggleE2ETests.cs` (Task 10.9) — conditional E2E

**tests/Hexalith.FrontComposer.Shell.Tests/** (modified):
- `Options/FcShellOptionsValidationTests.cs` — +4 tests (Task 10.8)

**tests/Hexalith.FrontComposer.Contracts.Tests/** (new):
- `Rendering/TypographyConstantsTests.cs` (Task 10.10) — 2 tests

## Naming Convention Reference

| Element | Pattern | Example |
|---|---|---|
| Shell layout component | `FrontComposer{Concern}` | `FrontComposerShell` |
| Shell Fc-prefixed adopter-facing component | `Fc{Concern}` | `FcThemeToggle`, `FcSystemThemeWatcher` |
| New FcShellOptions property | PascalCase, unit-suffixed where relevant | `AccentColor`, `LocalStorageMaxEntries` |
| Typography constants | PascalCase field names | `Typography.ViewTitle` |
| JS module filename | `fc-{concern}.js` | `fc-beforeunload.js`, `fc-prefers-color-scheme.js` |
| JS module export | lowercase verbs | `register`, `subscribe` |
| Runtime diagnostic ID | `HFC2xxx` in Shell range | `HFC2105`, `HFC2106` |
| Resource file | `{Class}Resources.resx` (+ `.fr.resx`) | `FcShellResources` |
| Scoped CSS | `{Component}.razor.css` | `FrontComposerShell.razor.css` |

## Testing Standards

- xUnit v3 (3.2.2), Verify.XunitV3, Shouldly, NSubstitute, bUnit 2.7.2 — inherited from 2-x.
- `FakeTimeProvider` via `Microsoft.Extensions.TimeProvider.Testing` — reused for LRU ordering.
- `TestContext.Current.CancellationToken` on async tests (xUnit1051).
- `TreatWarningsAsErrors=true` global.
- `DiffEngine_Disabled: true` in CI.
- `BunitJSInterop` strict mode: explicit `.SetupVoid` / `.Setup<T>` for every interop call; unhandled interops fail tests.
- **Test count budget (L07):** **~43 new tests** (post-elicitation adjustment: +3 from D28 `AddHexalithFrontComposerQuickstartTests` per Task 10.14; Task 10.13 rewrite is 1:1). Cumulative target rebaselined at Task 0.1 — grep estimate ~533 `[Fact]`/`[Theory]` pre-3-1. Decisions-to-tests ratio: 43 / 29 ≈ 1.48 per decision — at Murat's 1.5 under-coverage floor; PR-review gate (Task 10.11) decides whether party-mode adds + D28/D29 adds justify the ratio or trim is warranted. Cuts during Task 10.11 (reviewer-applied, not dev-applied): one `FrontComposerShellTests` row (placeholder-button assertion no longer applicable — D26 removes the buttons outright), candidate collapses in options validation + typography.

## Build & CI

- `Microsoft.FluentUI.AspNetCore.Components` stays at `5.0.0-rc.2-26098.1` — do NOT bump in this story.
- Roslyn 4.12.0 pinned (inherited).
- No new `AnalyzerReleases.Unshipped.md` entries — HFC2105/HFC2106 are runtime-only.
- Scoped CSS emits a `{AssemblyName}.styles.css` bundle under `_content/Hexalith.FrontComposer.Shell/` automatically via the Blazor RCL build target — adopter `App.razor` `<link>` picks it up (Task 9.2).
- No new CI jobs — everything rides the existing `dotnet build` + `dotnet test` pipeline.
- Playwright E2E in Task 10.9 is gated on Aspire MCP availability (per `feedback_no_manual_validation.md`) — fallback is a GIF captured via Aspire MCP + Claude browser.

## Previous Story Intelligence

**From Story 2-5 (immediate predecessor):**
- **Sharded story format** — index.md + per-section markdown files — matches 2-3 / 2-4 / 2-5 patterns. 3-1 follows the same structure.
- **L06 budget discipline** — 2-5 landed at 24 decisions (feature, ≤25). 3-1 lands at 29 decisions post-elicitation (infrastructure, ≤40) — still plenty of headroom for review-round additions.
- **L07 test-to-decision ratio** — 2-5 at 1.6, 3-1 at 1.48 post-elicitation. On the edge of Murat's 1.5 floor; PR-review gate (Task 10.11) decides final count.
- **FcShellOptions growth** — 2-5 bumped to 10 properties; 3-1 bumps to 14. Split now formally triggered (G1) → Story 9-2.

**From Story 2-4:**
- **Append-only parameter discipline (D1 for FcLifecycleWrapper)** — 3-1 applies the same discipline to `FrontComposerShell` via D4 and the parameter-surface snapshot test.
- **TimeProvider registration** — already in place; LocalStorageService reuses.
- **IStringLocalizer infrastructure** — 2-4 introduced `IFluentLocalizer` references in the spec but may have deferred actual resource materialisation. 3-1 materialises `FcShellResources` (EN + FR) and pre-populates lifecycle-state keys so 2-4 can opt in non-blocking (Known Gap G2).
- **XSS plain-text rendering invariant (D22)** — resource strings resolved via `IStringLocalizer<FcShellResources>` are static content (authored by framework team), not adopter input — XSS posture unchanged.

**From Story 2-3:**
- **Single-writer invariant (D19)** — 3-1's D7 applies the same principle to `FrontComposerThemeState`: `ThemeChangedAction` is the single writer.
- **Correlation ID via IUlidFactory** — reused in `FcThemeToggle.razor.cs`.

**From Story 2-2:**
- **IUserContextAccessor fail-closed default (D31)** — 3-1's D8 + ADR-029 extend the precedent from `LastUsedValueProvider` to `ThemeEffects` + `DensityEffects`.
- **FcShellOptions.FullPageFormMaxWidth (D26)** — honored by 3-1; shell layout does NOT add a parallel max-width.

**From Story 1-3:**
- **FrontComposerThemeState + ThemeActions + ThemeReducers + ThemeEffects** — already shipped. 3-1 extends `ThemeEffects` with the guard (Task 4.2) and does not add new actions or reducers.
- **IState<T> + IStateSelection<T, U>** Fluxor patterns — 3-1 `FcThemeToggle` uses `IStateSelection` for minimal-rerender subscription (D22).

**From Story 1-8:**
- **Fluent UI RC pin** — `5.0.0-rc.2-26098.1` — 3-1 stays pinned.
- **Placement of `<FluentProviders />` + `<StoreInitializer />`** — 3-1 moves both into the shell component, which is the final home (per Story 1-8 dev-note "eventually absorbed by shell component").

## Lessons Ledger Citations (from `_bmad-output/process-notes/story-creation-lessons.md`)

- **L01 Cross-story contract clarity** — Every Binding Contract row in the cheat sheet names both producer and consumer. ADR-027/028/029 document cross-story seams.
- **L02 Fluxor feature producer+consumer scope** — 3-1 does NOT introduce new Fluxor features; it extends `ThemeEffects` + `DensityEffects` with the guard. Preserves the L02 discipline by keeping producer/consumer alignment in Story 1-3's original feature.
- **L03 Tenant/user isolation fail-closed** — D8 + ADR-029 codify the precedent set by Story 2-2 D31. Memory feedback `feedback_tenant_isolation_fail_closed.md` and `feedback_no_manual_validation.md` both honored.
- **L04 Generated name collision detection** — Not applicable in 3-1 (no emitter changes).
- **L05 Hand-written service + emitted per-type wiring** — Not applicable (no emitter changes in 3-1; LocalStorageService is hand-written infrastructure).
- **L06 Defense-in-depth budget** — 29 decisions at the infrastructure story ceiling of 40 (up from 27 post-elicitation: D28 Quickstart + D29 scoped-CSS contract). Still well within headroom.
- **L07 Test count inflation** — 43 tests / 29 decisions ≈ 1.48 per decision; on the edge of Murat's 1.5 under-coverage floor. Task 10.11 PR-review gate decides whether the three party-mode adds + D28/D29 adds justify the ratio or trim is warranted.
- **L08 Party review vs. elicitation** — 3-1 has not yet been reviewed via party mode or advanced elicitation. The spec is drafted for developer-agent consumption now; a party review round is recommended **before** dev-story execution starts, especially for decisions that touch shared state (D8, D18) and adopter DX (D3, D24). **Recommended flow:** `/bmad-party-mode` with Winston / Amelia / Sally / Murat on this story → apply any critical findings → then `/bmad-advanced-elicitation` (Pre-mortem / Red Team / Chaos / Hindsight) → then `dev-story`.
- **L09 ADR rejected-alternatives discipline** — ADR-027 cites 3, ADR-028 cites 3, ADR-029 cites 3. All ≥ 2 satisfied.
- **L10 Deferrals name a story** — All 16 Known Gaps cite specific owning stories (3-2, 3-3, 3-4, 3-5, 3-6, 5-2, 9-2, 9-4, 10-2, v1.x, v2).
- **L11 Dev Agent Cheat Sheet** — Present. Infrastructure story with 4 major new components (shell, toggle, watcher, storage service) + JS modules + resource files warrants fast-path entry.

## References

- [Source: _bmad-output/planning-artifacts/epics/epic-3-composition-shell-navigation-experience.md#Story 3.1 — AC source of truth, §5-56]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#FR14 Customizable accent color, §18]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#FR15 Light/Dark/System theme toggle, §19]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR14 Application shell layout, §313]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR23 Six-slot color system, §322]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR25 Light/Dark/System theme support, §324]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR26 Typography API, §325]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR57 Zero-override strategy, §356]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR60 EN+FR localization, §359]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#NFR17 Zero PII in client-side storage, §106]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md#Color System, §40-108]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md#Typography System, §110-167]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md#Spacing & Layout Foundation, §169-264]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/responsive-design-accessibility.md#Breakpoint behavior matrix, §22-37]
- [Source: _bmad-output/planning-artifacts/architecture.md#IStorageService Contract, §508-525]
- [Source: _bmad-output/planning-artifacts/architecture.md#Per-Concern Fluxor Features, §527-536]
- [Source: _bmad-output/planning-artifacts/architecture.md#Diagnostic ID ranges, §648-657]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation Sequence, §581-594]
- [Source: _bmad-output/planning-artifacts/architecture.md#Shell source tree, §919-982]
- [Source: _bmad-output/implementation-artifacts/2-5-command-rejection-confirmation-and-form-protection/critical-decisions-read-first-do-not-revisit.md#FcShellOptions growth]
- [Source: _bmad-output/implementation-artifacts/2-4-fclifecyclewrapper-visual-lifecycle-feedback/dev-notes.md#IStringLocalizer gaps]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management/critical-decisions-read-first-do-not-revisit.md#Decision D19 — single-writer invariant]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes/architecture-decision-records.md#Decision D26 FullPageFormMaxWidth + D31 IUserContextAccessor fail-closed]
- [Source: _bmad-output/implementation-artifacts/1-8-hot-reload-and-fluent-ui-contingency.md#Fluent UI bootstrap + IFluentLocalizer]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L01-L11 — all lessons applied]
- [Source: memory/feedback_no_manual_validation.md — automated bUnit + Aspire MCP preferred over manual validation]
- [Source: memory/feedback_cross_story_contracts.md — explicit binding contracts per ADR-016 canonical example; ADR-027/028/029 mirror]
- [Source: memory/feedback_tenant_isolation_fail_closed.md — D8 + ADR-029 inherit]
- [Source: memory/feedback_defense_budget.md — 24 decisions, under ≤40 infrastructure cap]
- [Source: src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs — extended in Task 1.1]
- [Source: src/Hexalith.FrontComposer.Contracts/Storage/IStorageService.cs — implemented by LocalStorageService]
- [Source: src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs — guarded in Task 4.2]
- [Source: src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs — guarded in Task 4.3]
- [Source: src/Hexalith.FrontComposer.Shell/State/StorageKeys.cs — constants deleted in Task 4.1]
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs — IStorageService swap + AddHexalithShellLocalization extension]
- [Source: samples/Counter/Counter.Web/Components/Layout/MainLayout.razor — rewired in Task 9.1]
- [Source: samples/Counter/Counter.Web/Program.cs — AddHexalithShellLocalization chain in Task 9.3]

## Project Structure Notes

- **Alignment with architecture blueprint** (architecture.md §919-982, §648-657):
  - New `Shell/Components/Layout/` subfolder — consistent with existing `Shell/Components/Lifecycle/`, `Shell/Components/Rendering/`, `Shell/Components/Forms/` organisation. `FrontComposerShell` + `FcThemeToggle` + `FcSystemThemeWatcher` live there.
  - `Shell/Infrastructure/Storage/` subfolder for `LocalStorageService` + `LocalStorageDrainWorker` — matches architecture §961-962 which lists both `LocalStorageService.cs` and `InMemoryStorageService.cs` at v0.1 (`InMemoryStorageService` stays in Contracts/Storage/ per its existing location to preserve netstandard2.0 compatibility for non-browser test hosts).
  - `Shell/Resources/` subfolder — new; resx convention + `IStringLocalizer` supports it automatically.
  - `Typography.cs` in `Contracts/Rendering/` — adopter-facing API; consistent with other Contracts-hosted rendering types.
  - Typography + Options + diagnostic IDs stay in Contracts — architecture §1144 dependency-free invariant preserved (no new namespace pulls).
  - HFC2105 + HFC2106 in the `HFC2xxx` runtime-log range per §648 diagnostic-ID policy.
  - No Contracts → Shell reverse references. `Typography.cs` in Contracts references `Microsoft.FluentUI.AspNetCore.Components.Typography` — Contracts.csproj already pulls the package transitively via Shell's reference.
- **Fluent UI `Fc` prefix convention** honored — `FcThemeToggle`, `FcSystemThemeWatcher`.
- **`FrontComposer{Concern}` naming** for framework-owned non-adopter-facing layout components — `FrontComposerShell` matches `FrontComposerNavigation` (architecture §928, Story 3-2 will ship).
- **`FcShellOptions` extension honored** — 4 new properties added to existing class. Split trigger now reached (G1); owning story named (9-2). Matches 2-5 precedent of recording the trigger without acting.

---
