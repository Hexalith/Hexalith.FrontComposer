# Story 1.3: Fluxor State Management Foundation

Status: done

**Depends on:** Story 1.1 (done), Story 1.2 (done)

## Story

As a developer,
I want Fluxor state management configured with base feature infrastructure and explicit subscription patterns,
so that all components share a single predictable state management pattern that preserves future migration flexibility.

## Acceptance Criteria

### AC1: Fluxor Registration and Render Mode Support

**Given** the Shell project references Fluxor.Blazor.Web 6.9.0
**When** Fluxor is registered in the DI container via `AddHexalithFrontComposer()`
**Then** `IStore`, `IState<FrontComposerThemeState>`, `IState<FrontComposerDensityState>`, and `IDispatcher` all resolve from the service provider
**And** `StoreInitializer` placement is documented in `AddHexalithFrontComposer()` XML doc comment (consumer must place it in their root layout)

### AC2: AOT-Friendly Subscription Pattern (Verified via Test Component)

**Given** a test component in Shell.Tests uses the framework's subscription pattern
**When** the component renders and an action is dispatched
**Then** the component uses explicit `IState<T>` inject with `StateChanged += OnStateChanged` and `IDisposable.Dispose()` cleanup
**And** `FluxorComponent` base class is NEVER used anywhere in the codebase
**And** the component re-renders when state changes and stops re-rendering after dispose

### AC3: Base State Features and Feature Classes

**Given** the base state features are initialized
**When** the application starts
**Then** `FrontComposerThemeFeature : Feature<FrontComposerThemeState>` exists with `GetInitialState()` returning `ThemeValue.Light`
**And** `FrontComposerDensityFeature : Feature<FrontComposerDensityState>` exists with `GetInitialState()` returning `DensityLevel.Comfortable`
**And** all actions are immutable records with past-tense naming (e.g., `ThemeChangedAction`, `DensityChangedAction`)
**And** all actions include a `CorrelationId` property

> **Note:** AC3 specifies a single default of `Comfortable` for DensityState. Context-dependent density (Compact for DataGrids, Comfortable for forms) is a rendering concern resolved at the component level in a later story via the 4-tier density cascade (user > OS > deployment > factory).

### AC4: State Persistence via IStorageService

**Given** ThemeState or DensityState changes
**When** the persistence effect runs
**Then** `IStorageService.SetAsync` is called with the correct storage key and value
**And** if `IStorageService` throws, the exception is caught, logged via `ILogger`, and the store is NOT crashed
**And** storage keys use placeholder identity: `"default"` for tenantId, `"anonymous"` for userId (real values deferred to Epic 7)

### AC5: State Hydration on Startup

**Given** `IStorageService` contains previously persisted theme/density values
**When** the Fluxor store initializes and an `AppInitializedAction` is dispatched
**Then** `ThemeEffects` reads from `IStorageService.GetAsync` and dispatches `ThemeChangedAction` with the stored value
**And** `DensityEffects` reads from `IStorageService.GetAsync` and dispatches `DensityChangedAction` with the stored value
**And** if storage is empty or returns null, the Feature defaults apply (Light / Comfortable)

### AC6: bUnit Test Base Pre-Configures Fluxor

**Given** a bUnit test inherits `FrontComposerTestBase`
**When** the test context is initialized
**Then** the Fluxor store is registered with all framework features and `IStore.InitializeAsync()` is called
**And** `InMemoryStorageService` is registered as `IStorageService`
**And** a fake `IOverrideRegistry` is available
**And** `IDispatcher` resolves from the service provider

## Definition of Done

- [ ] All ACs verified
- [ ] `dotnet build` from repo root = zero errors and zero warnings
- [ ] `dotnet test` from repo root passes all tests (Contracts.Tests + Shell.Tests)
- [ ] EventStore submodule tests still pass (isolation check)
- [ ] All new public types have XML `/// <summary>` doc comments
- [ ] Zero `FluxorComponent` usage anywhere in codebase
- [ ] New Shell.Tests project added to solution file under `tests/` solution folder

## Tasks / Subtasks

- [x] Task 1: Fluxor DI registration in Shell (AC: #1)
  - [x] 1.1 Create `Shell/Extensions/ServiceCollectionExtensions.cs` with `AddHexalithFrontComposer()` method
  - [x] 1.2 Register Fluxor via `services.AddFluxor(o => o.ScanAssemblies(typeof(FrontComposerThemeState).Assembly))`
  - [x] 1.3 Register `InMemoryStorageService` as `IStorageService` (singleton, server-side default)
  - [x] 1.4 Add XML doc comment on `AddHexalithFrontComposer()` instructing consumers to place `<Fluxor.Blazor.Web.StoreInitializer />` in their root layout (Shell is a RCL, not a host -- it cannot place the component itself)
  - [x] 1.5 Add code comment documenting DI scope divergence: Server = scoped-per-circuit, WASM = scoped-per-app
  - [x] 1.6 Update `Shell/_Imports.razor` with `@using Fluxor` and state namespace usings

- [x] Task 2: ThemeState feature (AC: #3)
  - [x] 2.1 Create `Shell/State/Theme/ThemeValue.cs` enum: `Light`, `Dark`, `System`
  - [x] 2.2 Create `Shell/State/Theme/FrontComposerThemeState.cs` as positional record: `public record FrontComposerThemeState(ThemeValue CurrentTheme);` -- positional syntax enables `state with { CurrentTheme = action.NewTheme }` in reducers and establishes the pattern for generated features in Story 1.4
  - [x] 2.3 Create `Shell/State/Theme/FrontComposerThemeFeature.cs` inheriting `Feature<FrontComposerThemeState>` with `GetName()` returning `"FrontComposerTheme"` and `GetInitialState()` returning `new(ThemeValue.Light)`
  - [x] 2.4 Create `Shell/State/Theme/ThemeActions.cs` with `ThemeChangedAction(string CorrelationId, ThemeValue NewTheme)`
  - [x] 2.5 Create `Shell/State/Theme/ThemeReducers.cs` with `[ReducerMethod]` static method handling `ThemeChangedAction`
  - [x] 2.6 Create `Shell/State/Theme/ThemeEffects.cs` with: (a) persistence effect calling `IStorageService.SetAsync` on `ThemeChangedAction`, (b) hydration effect reading from `IStorageService.GetAsync` on `AppInitializedAction`, (c) try/catch with `ILogger` on all storage calls -- never crash the store

- [x] Task 3: DensityState feature (AC: #3)
  - [x] 3.1 Create `Shell/State/Density/FrontComposerDensityState.cs` as positional record: `public record FrontComposerDensityState(DensityLevel CurrentDensity);` -- same pattern as ThemeState. Add `using Hexalith.FrontComposer.Contracts.Rendering;` for `DensityLevel`
  - [x] 3.2 Create `Shell/State/Density/FrontComposerDensityFeature.cs` inheriting `Feature<FrontComposerDensityState>` with `GetName()` returning `"FrontComposerDensity"` and `GetInitialState()` returning `new(DensityLevel.Comfortable)`
  - [x] 3.3 Create `Shell/State/Density/DensityActions.cs` with `DensityChangedAction(string CorrelationId, DensityLevel NewDensity)`
  - [x] 3.4 Create `Shell/State/Density/DensityReducers.cs` with `[ReducerMethod]` static method handling `DensityChangedAction`
  - [x] 3.5 Create `Shell/State/Density/DensityEffects.cs` following same pattern as ThemeEffects (persistence + hydration + error handling)
  - [x] 3.6 Reuse `DensityLevel` enum from `Contracts.Rendering` -- do NOT duplicate

- [x] Task 4: Shared action, storage keys, and identity placeholders (AC: #4, #5)
  - [x] 4.0 Create `Shell/State/AppInitializedAction.cs` with `public record AppInitializedAction(string CorrelationId);` in namespace `Hexalith.FrontComposer.Shell.State` -- this is a cross-cutting action consumed by both ThemeEffects and DensityEffects. Do NOT place it in the Theme or Density folder. Do NOT auto-dispatch it from `AddHexalithFrontComposer()` -- the consuming app is responsible for dispatching it from its root layout after store initialization.
  - [x] 4.1 Create `Shell/State/StorageKeys.cs` with two overloads: `BuildKey(string tenantId, string userId, string feature)` returning `{tenantId}:{userId}:{feature}` (for theme/density) and `BuildKey(string tenantId, string userId, string feature, string discriminator)` returning `{tenantId}:{userId}:{feature}:{discriminator}` (for DataGridState/ETagCacheState in later stories, matching the 4-segment pattern documented in IStorageService)
  - [x] 4.2 Define placeholder constants: `DefaultTenantId = "default"`, `DefaultUserId = "anonymous"` with `// TODO: Replace with ITenantContext/IUserContext when authentication is implemented (Epic 7)` comment
  - [x] 4.3 Use `StorageKeys.BuildKey(DefaultTenantId, DefaultUserId, "theme")` in ThemeEffects and `..."density"` in DensityEffects

- [x] Task 5: Shell.Tests project and bUnit test infrastructure (AC: #6)
  - [x] 5.1 Create `tests/Hexalith.FrontComposer.Shell.Tests/` project using `Microsoft.NET.Sdk.Razor` (NOT `Microsoft.NET.Sdk` -- required because Shell.Tests contains `.razor` test components). Set `<IsPackable>false</IsPackable>`, mirror Contracts.Tests for test package references including `coverlet.collector`, `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`
  - [x] 5.2 Add ProjectReference to Shell and explicit PackageReferences for `Fluxor.Blazor.Web`, `bunit`, `NSubstitute` (do not rely on transitive with CPM -- Contracts.Tests does not include bunit or NSubstitute, so they must be added explicitly)
  - [x] 5.3 Add `_Imports.razor` in Shell.Tests with bUnit and Fluxor usings
  - [x] 5.4 Create `FrontComposerTestBase.cs` inheriting bUnit `TestContext`:
    - Register Fluxor via `Services.AddFluxor(o => o.ScanAssemblies(...))`
    - Register `InMemoryStorageService` as `IStorageService`
    - Register `Substitute.For<IOverrideRegistry>()` as `IOverrideRegistry`
    - Call `Services.GetRequiredService<IStore>().InitializeAsync()` in setup (Fluxor requires this before dispatch)
  - [x] 5.5 Add Shell.Tests project to `Hexalith.FrontComposer.sln` under `tests/` solution folder

- [x] Task 6: Reducer and Effect unit tests (AC: #3, #4)
  - [x] 6.1 `ThemeReducers` [Theory] test: all 3 `ThemeValue` enum values via `[InlineData]` -- dispatch `ThemeChangedAction`, assert new state
  - [x] 6.2 `DensityReducers` [Theory] test: all 3 `DensityLevel` enum values via `[InlineData]` -- dispatch `DensityChangedAction`, assert new state
  - [x] 6.3 `ThemeEffects` persistence test: mock `IStorageService`, dispatch `ThemeChangedAction`, verify `SetAsync` called with key `"default:anonymous:theme"` and correct value
  - [x] 6.4 `DensityEffects` persistence test: same pattern with key `"default:anonymous:density"`
  - [x] 6.5 `ThemeEffects_StorageServiceThrows_DoesNotCrashStore`: mock `IStorageService.SetAsync` to throw, dispatch action, verify store still functions
  - [x] 6.6 `DensityEffects_StorageServiceThrows_DoesNotCrashStore`: same pattern
  - [x] 6.7 `FrontComposerThemeFeature_GetInitialState_ReturnsLight`: trivial but protects against accidental default changes
  - [x] 6.8 `FrontComposerDensityFeature_GetInitialState_ReturnsComfortable`: same

- [x] Task 7: State hydration round-trip tests (AC: #5)
  - [x] 7.1 `ThemeHydration_StorageContainsValue_DispatchesRestoredTheme`: pre-seed `InMemoryStorageService` with `Dark`, dispatch `AppInitializedAction`, verify state is `Dark`
  - [x] 7.2 `ThemeHydration_StorageEmpty_UsesDefaultLight`: no seed, dispatch `AppInitializedAction`, verify state stays `Light`
  - [x] 7.3 `DensityHydration_StorageContainsValue_DispatchesRestoredDensity`: pre-seed with `Compact`, verify
  - [x] 7.4 `DensityHydration_StorageEmpty_UsesDefaultComfortable`: verify default

- [x] Task 8: bUnit component subscription lifecycle tests (AC: #2)
  - [x] 8.1 Create minimal `TestThemeComponent.razor` in Shell.Tests that demonstrates the `IState<T>` + `IDisposable` subscription pattern
  - [x] 8.2 `ThemeSubscription_ComponentRendered_ReceivesInitialState`: render component, verify it displays `Light`
  - [x] 8.3 `ThemeSubscription_ActionDispatched_ComponentRerendersWithNewState`: dispatch `ThemeChangedAction(Dark)`, verify re-render with `Dark`
  - [x] 8.4 `ThemeSubscription_ComponentDisposed_UnsubscribesFromStateChanged`: dispose, dispatch again, verify no re-render and no `ObjectDisposedException`
  - [x] 8.5 Create minimal `TestDensityComponent.razor` in Shell.Tests demonstrating the `IState<FrontComposerDensityState>` + `IDisposable` subscription pattern (same approach as TestThemeComponent)
  - [x] 8.6 Mirror tests 8.2-8.4 for DensityState using TestDensityComponent (3 additional tests)

- [x] Task 9: DI registration and TestBase validation tests (AC: #1, #6)
  - [x] 9.1 `FluxorRegistration_AddHexalithFrontComposer_ResolvesIStore`: verify `IStore` resolves
  - [x] 9.2 `FluxorRegistration_AddHexalithFrontComposer_ResolvesIDispatcher`: verify `IDispatcher` resolves
  - [x] 9.3 `FluxorRegistration_AddHexalithFrontComposer_ResolvesAllStateTypes`: verify `IState<FrontComposerThemeState>` and `IState<FrontComposerDensityState>` resolve
  - [x] 9.4 `FrontComposerTestBase_StoreInitialized_DispatchDoesNotThrow`: dispatch an action from test base, verify no exception
  - [x] 9.5 `FrontComposerTestBase_ServicesRegistered_AllDependenciesResolve`: verify `IStorageService`, `IOverrideRegistry`, `IDispatcher` all resolve

- [x] Task 10: Build verification (AC: all, DoD)
  - [x] 10.1 `dotnet build` from repo root = zero errors and zero warnings
  - [x] 10.2 `dotnet test` passes all tests (Contracts.Tests + Shell.Tests)
  - [x] 10.3 EventStore submodule tests still pass (isolation verification)
  - [x] 10.4 Verify zero `FluxorComponent` usage via codebase search

### Review Findings

- [x] `[Review][Dismiss]` Clarify the default `IStorageService` lifetime and override behavior — dismissed for Story 1.3 because Task 1.3 explicitly requires singleton `InMemoryStorageService` with placeholder identity. Revisit when Epic 7 introduces tenant and user context.
- [x] `[Review][Patch]` Initialize the Fluxor store automatically in `FrontComposerTestBase` as required by AC6 [`tests/Hexalith.FrontComposer.Shell.Tests/FrontComposerTestBase.cs:15`]
- [x] `[Review][Patch]` Hydrate theme and density through the typed storage contract instead of `GetAsync<object>` boxing [`src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs:24`]
- [x] `[Review][Patch]` Tighten the AC-mapped tests that currently pass without verifying the claimed behavior [`tests/Hexalith.FrontComposer.Shell.Tests/State/Theme/ThemeFeatureTests.cs:14`]

## Dev Notes

### Architecture Patterns and Constraints

**ADR-008 (Fluxor State Shape Convention):**
- One Feature per domain type
- Framework features use `FrontComposer` prefix (e.g., `FrontComposerThemeFeature`)
- Generated domain features use type name (e.g., `CounterProjectionFeature`) -- not in scope for this story
- Actions are immutable records, always past-tense, always include `CorrelationId`
- Reducers are static `[ReducerMethod]` methods in dedicated `{Concern}Reducers.cs`
- Effects handle async side effects (persistence, API calls)

> **Note:** The architecture doc's Per-Concern Features table uses bare names (`ThemeState`, `DensityState`) without the `FrontComposer` prefix, but the naming convention section mandates the prefix. This story applies the prefix per the naming convention. The architecture doc's 4-file folder pattern (`State.cs`, `Actions.cs`, `Reducers.cs`, `Effects.cs`) also omits the Feature class file -- this story adds `{Concern}Feature.cs` which is required by Fluxor.

**Subscription Pattern (CRITICAL -- ADR-008, Architecture Path B):**
```csharp
// CORRECT: Explicit IState<T> subscribe/dispose + IDispatcher for actions
public partial class SomeComponent : ComponentBase, IDisposable
{
    [Inject] private IState<FrontComposerThemeState> ThemeState { get; set; } = default!;
    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

    protected override void OnInitialized()
    {
        ThemeState.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
        => InvokeAsync(StateHasChanged);

    private void ChangeTheme(ThemeValue newTheme)
        => Dispatcher.Dispatch(new ThemeChangedAction(Guid.NewGuid().ToString(), newTheme));

    public void Dispose()
    {
        ThemeState.StateChanged -= OnStateChanged;
    }
}

// WRONG: Never use FluxorComponent base class (breaks AOT, blocks migration path)
// public partial class SomeComponent : FluxorComponent { ... }
```

Use `IDisposable` (not `IAsyncDisposable`) for state subscription cleanup -- `StateChanged` event unsubscription is synchronous. `IAsyncDisposable` is only needed if the component holds async resources (not the case here).

This pattern preserves migration path away from Fluxor -- replacing `IState<T>` with `IObservable<T>` requires changing the generator template, not every component.

**Per-Concern Feature Organization:**

```
Shell/State/{Concern}/
  FrontComposer{Concern}Feature.cs  -- Feature<T> subclass (required by Fluxor)
  FrontComposer{Concern}State.cs    -- state record
  {Concern}Actions.cs               -- action records (up to 5 per file)
  {Concern}Reducers.cs              -- [ReducerMethod] static methods
  {Concern}Effects.cs               -- [EffectMethod] for async (persistence, hydration)
```

Namespace: `Hexalith.FrontComposer.Shell.State.Theme`, `...State.Density`, etc.

**Per-Concern Features Defined in Architecture (implement ThemeState and DensityState in this story):**

| Feature | Persisted | Storage Key Pattern | Eviction | Story |
|---------|-----------|-------------------|----------|-------|
| `FrontComposerThemeState` | Yes | `{tenantId}:{userId}:theme` | None | **1.3** |
| `FrontComposerDensityState` | Yes | `{tenantId}:{userId}:density` | None | **1.3** |
| `NavigationState` | Yes | `{tenantId}:{userId}:nav` | None | Later |
| `DataGridState` | Yes | `{tenantId}:{userId}:grid:{projectionType}` | LRU | Later |
| `ETagCacheState` | Yes | `{tenantId}:{userId}:etag:{projectionType}` | LRU | Later |
| `CommandLifecycleState` | **No** (ephemeral) | -- | Evicted on terminal | Later |

**Fluxor Naming Conventions:**

| Element | Pattern | Example (this story) |
|---------|---------|---------------------|
| Feature class | `FrontComposer{Concern}Feature` | `FrontComposerThemeFeature` |
| State record | `FrontComposer{Concern}State` | `FrontComposerThemeState` |
| Action | `{Concern}ChangedAction` (past-tense) | `ThemeChangedAction` |
| Reducer class | `{Concern}Reducers` | `ThemeReducers` |
| Effect class | `{Concern}Effects` | `ThemeEffects` |

**Action Payload Rules:**
- Actions are immutable records
- Always include `CorrelationId` (string)
- Never include services or mutable objects
- Past-tense naming only (`Changed`, `Submitted`, never `Change`, `Submit`)

```csharp
public record ThemeChangedAction(string CorrelationId, ThemeValue NewTheme);
public record DensityChangedAction(string CorrelationId, DensityLevel NewDensity);
public record AppInitializedAction(string CorrelationId);
```

**Reducer Pattern (CRITICAL -- method MUST be static):**
```csharp
public static class ThemeReducers
{
    [ReducerMethod]
    public static FrontComposerThemeState ReduceThemeChanged(FrontComposerThemeState state, ThemeChangedAction action)
        => state with { CurrentTheme = action.NewTheme };
}
```
Fluxor discovers reducers by `[ReducerMethod]` on **static** methods only. If the method is non-static, it compiles but the reducer is never called -- state silently never updates.

**Effect Error Handling Pattern (CRITICAL -- Architecture: "No exception ever swallowed"):**
```csharp
public class ThemeEffects(IStorageService storage, ILogger<ThemeEffects> logger)
{
    [EffectMethod]
    public async Task HandleThemeChanged(ThemeChangedAction action, IDispatcher dispatcher)
    {
        try
        {
            string key = StorageKeys.BuildKey(StorageKeys.DefaultTenantId, StorageKeys.DefaultUserId, "theme");
            await storage.SetAsync(key, action.NewTheme);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist theme state");
            // Degrade gracefully -- state change still applies in memory
        }
    }
}
```

**Storage Key Identity Placeholders:**
```csharp
// Shell/State/StorageKeys.cs
public static class StorageKeys
{
    // TODO: Replace with ITenantContext/IUserContext when authentication is implemented (Epic 7)
    public const string DefaultTenantId = "default";
    public const string DefaultUserId = "anonymous";

    /// <summary>3-segment key for simple features (theme, density, nav).</summary>
    public static string BuildKey(string tenantId, string userId, string feature)
        => $"{tenantId}:{userId}:{feature}";

    /// <summary>4-segment key for discriminated features (DataGrid, ETagCache). Matches IStorageService doc pattern.</summary>
    public static string BuildKey(string tenantId, string userId, string feature, string discriminator)
        => $"{tenantId}:{userId}:{feature}:{discriminator}";
}
```

> Theme/Density use the 3-segment variant (no discriminator). DataGridState (`grid:{projectionType}`) and ETagCacheState (`etag:{projectionType}`) in later stories use the 4-segment variant matching the `{tenantId}:{userId}:{featureName}:{discriminator}` pattern documented in `IStorageService`.

**Hydration Effect Pattern:**
```csharp
[EffectMethod]
public async Task HandleAppInitialized(AppInitializedAction action, IDispatcher dispatcher)
{
    try
    {
        string key = StorageKeys.BuildKey(StorageKeys.DefaultTenantId, StorageKeys.DefaultUserId, "theme");
        ThemeValue? stored = await storage.GetAsync<ThemeValue>(key);
        if (stored.HasValue)
        {
            dispatcher.Dispatch(new ThemeChangedAction(action.CorrelationId, stored.Value));
        }
        // If null (key not found), Feature default applies -- do NOT dispatch
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to hydrate theme state from storage");
    }
}
```

> **IMPORTANT -- Enum Nullability:** `GetAsync<ThemeValue>` returns `ThemeValue?` (nullable enum). Use `stored.HasValue` to check -- do NOT use `stored != default` because `default(ThemeValue)` is `Light` (enum value 0), which is a valid stored value. For `DensityLevel`, `default` is `Compact` (value 0), NOT the intended default of `Comfortable` -- using `!= default` would silently hydrate `Compact` instead of skipping.

> **IMPORTANT -- Fluxor Fan-Out:** A single `AppInitializedAction` dispatch triggers ALL registered `[EffectMethod]` handlers for that action type. Both `ThemeEffects.HandleAppInitialized` and `DensityEffects.HandleAppInitialized` fire simultaneously from one dispatch -- this is by design.

**Hydration Trigger:**
The consumer dispatches `AppInitializedAction` once after the store initializes (e.g., from their root layout's `OnInitialized`). Do NOT auto-dispatch from `AddHexalithFrontComposer()` or any middleware -- the consuming app controls when hydration fires. If storage returns null, the Feature defaults apply -- no action dispatched.

**Why `AppInitializedAction` instead of Fluxor store init hooks:** Fluxor's `IStore.InitializeAsync()` runs during DI resolution / component initialization. Hooking into it couples hydration to the DI lifecycle, which is fragile across render mode transitions (Server prerender -> WASM takeover). A dedicated action gives the consumer explicit control over timing and makes hydration testable in isolation (tests dispatch it explicitly after seeding storage).

### Source Tree Components to Touch

**New files to create in `src/Hexalith.FrontComposer.Shell/`:**

```
Extensions/
  ServiceCollectionExtensions.cs     -- AddHexalithFrontComposer() entry point

State/
  AppInitializedAction.cs            -- cross-cutting hydration trigger (shared, not in Theme or Density)
  StorageKeys.cs                     -- key builder + placeholder identity constants

  Theme/
    FrontComposerThemeFeature.cs     -- Feature<FrontComposerThemeState>
    FrontComposerThemeState.cs       -- positional record: (ThemeValue CurrentTheme)
    ThemeActions.cs                  -- ThemeChangedAction record
    ThemeReducers.cs                 -- [ReducerMethod] static methods
    ThemeEffects.cs                  -- persistence + hydration + error handling
    ThemeValue.cs                    -- enum: Light, Dark, System (Shell-only, not in Contracts)

  Density/
    FrontComposerDensityFeature.cs   -- Feature<FrontComposerDensityState>
    FrontComposerDensityState.cs     -- positional record: (DensityLevel CurrentDensity)
    DensityActions.cs                -- DensityChangedAction record
    DensityReducers.cs               -- [ReducerMethod] static methods
    DensityEffects.cs                -- persistence + hydration + error handling
```

**New files to create in `tests/`:**

```
Hexalith.FrontComposer.Shell.Tests/  -- NEW test project
  Hexalith.FrontComposer.Shell.Tests.csproj  -- mirror Contracts.Tests structure
  _Imports.razor                     -- bUnit + Fluxor usings
  FrontComposerTestBase.cs           -- bUnit base with Fluxor pre-config + store init
  Components/
    TestThemeComponent.razor         -- minimal subscription pattern demo for AC2
    TestDensityComponent.razor       -- same for density
  State/
    Theme/
      ThemeReducersTests.cs
      ThemeEffectsTests.cs
      ThemeFeatureTests.cs
    Density/
      DensityReducersTests.cs
      DensityEffectsTests.cs
      DensityFeatureTests.cs
    HydrationTests.cs
    SubscriptionLifecycleTests.cs
    FluxorRegistrationTests.cs
    TestBaseTests.cs
```

**Existing files that may need updates:**
- `Hexalith.FrontComposer.Shell.csproj` -- already has Fluxor.Blazor.Web and Contracts references (verified, no changes needed)
- `Hexalith.FrontComposer.sln` -- add new Shell.Tests project under `tests/` folder
- `Shell/_Imports.razor` -- add `@using Fluxor` and state namespace usings

### Testing Standards

- **Test naming:** `{Method}_{Scenario}_{Expected}` (pick this convention for Shell.Tests, consistent with Contracts.Tests)
- **Assertions:** Shouldly (4.3.0)
- **Mocking:** NSubstitute (5.3.0). Reference pattern:
  ```csharp
  var storage = Substitute.For<IStorageService>();
  storage.SetAsync(Arg.Any<string>(), Arg.Any<ThemeValue>(), Arg.Any<CancellationToken>())
      .Returns(Task.CompletedTask);
  ```
- **bUnit:** Version 2.7.2, compatible with xUnit v3 3.2.2
- **Enum coverage:** Use `[Theory]` with `[InlineData]` for reducers to cover all enum values at zero marginal cost
- **Test folder mirrors source:** `tests/Shell.Tests/State/Theme/` mirrors `src/Shell/State/Theme/`
- **Reducer tests:** Pure function tests -- call static reducer method, assert new state
- **Effect tests:** Mock `IStorageService` and `ILogger`, instantiate effect, call handler, verify interactions
- **Registration tests:** Build service provider, verify types resolve
- **bUnit subscription tests:** Render test component, dispatch action, assert markup change; dispose, dispatch again, assert no change
- **Each test class covers one source class** -- `ThemeReducersTests.cs` for `ThemeReducers.cs`, etc.

### Project Structure Notes

- Shell.csproj already references `Fluxor.Blazor.Web` (6.9.0) and `Contracts` -- no csproj changes needed for Shell
- New `Shell.Tests.csproj` must mirror Contracts.Tests structure:
  - `<IsPackable>false</IsPackable>`
  - ProjectReference to Shell
  - Explicit PackageReferences: `Fluxor.Blazor.Web`, `xunit.v3`, `xunit.v3.assert`, `xunit.runner.visualstudio`, `bunit`, `Shouldly`, `NSubstitute`, `coverlet.collector`, `Microsoft.NET.Test.Sdk`
  - Do NOT rely on transitive Fluxor from Shell ProjectReference (CPM may not flow versions)
- `DensityLevel` enum already exists in `Contracts.Rendering` namespace -- reuse it, do NOT create a duplicate
- `ThemeValue` enum is Shell-only (not needed cross-package currently). If Contracts ever needs it, extract then.
- `InMemoryStorageService` already exists in `Contracts.Storage` namespace -- use it for test and server-side persistence
- Namespace matches folder path exactly. One type per file (except up to 5 related Fluxor action records may share a file)
- File-scoped namespaces required (`namespace X.Y.Z;`) per .editorconfig
- `TreatWarningsAsErrors=true` is enforced globally -- zero warnings allowed

### IStorageService Contract (from Story 1.2)

Already implemented in Contracts. Key points for this story:
- 5 async methods: `GetAsync<T>`, `SetAsync<T>`, `RemoveAsync`, `GetKeysAsync`, `FlushAsync`
- Uses `Task` (not `ValueTask`) with `CancellationToken` parameters -- this differs from the architecture doc which specifies `ValueTask` without `CancellationToken`. Story 1.2 deviated for netstandard2.0 compatibility. Use the actual contract signatures.
- Returns `null` (not throws) when key not found
- `InMemoryStorageService` is a sealed class using `ConcurrentDictionary<string, object>`
- Storage key pattern: `{tenantId}:{userId}:{featureName}:{discriminator}`

### Render Mode Constraints

- **`[PersistentState]` is NOT in scope for this story.** It is required for cross-render-mode state survival when Blazor Auto transitions from Server to WASM, but this story targets Server + bUnit only. Will be added when Blazor Auto render mode is exercised in a later story.
- DI scope divergence: Server = scoped-per-circuit, WASM = scoped-per-app. Document in code comment on `AddHexalithFrontComposer()`.
- `IStorageService` adapter pattern: `LocalStorageService` (WASM, future), `InMemoryStorageService` (Server + bUnit, current)
- `beforeunload` JS interop hook for `FlushAsync` is deferred to a later story -- `InMemoryStorageService.FlushAsync` is a no-op

### Previous Story Intelligence

**From Story 1.1 (Build Infrastructure):**
- .NET SDK 10.0.104 resolved via `rollForward: latestPatch` from pin 10.0.103
- `TreatWarningsAsErrors=true` is enforced -- no warnings allowed
- `LangVersion=latest` in Directory.Build.props enables file-scoped namespaces on all targets
- Allman braces, `_camelCase` for private fields, `I` prefix for interfaces
- Solution uses classic `.sln` format (not `.slnx`)

**From Story 1.2 (Contracts):**
- All public types have XML `/// <summary>` documentation
- Provisional interfaces marked with doc comment: "Provisional: may receive non-breaking additions after Story 1.3"
- `record` types used extensively (positional and init properties)
- No `async/await` in InMemoryStorageService -- returns `Task.FromResult()` / `Task.CompletedTask` directly (avoids CA2007)
- 9 comprehensive tests in Contracts.Tests prove InMemoryStorageService works (thread safety, prefix filtering, flush)
- xUnit v3 3.2.2 with Shouldly 4.3.0 for assertions

**Package versions confirmed in Directory.Packages.props:**
- `Fluxor.Blazor.Web` 6.9.0 (already pinned)
- `xunit.v3` 3.2.2 / `xunit.v3.assert` 3.2.2
- `bunit` 2.7.2
- `Shouldly` 4.3.0
- `NSubstitute` 5.3.0
- `Microsoft.FluentUI.AspNetCore.Components` 5.0.0-rc.2-26098.1

### Git Intelligence

Recent commits are planning/documentation artifacts only. No implementation code has been committed yet for stories 1.1 and 1.2 (they exist as uncommitted changes per git status). Story 1.3 builds directly on the uncommitted codebase state.

### Latest Technical Information

**Fluxor 6.9.0 (latest stable as of April 2026):**
- Zero-boilerplate Flux/Redux library for .NET and Blazor
- Supports `IState<T>` injection pattern (preferred over `FluxorComponent` base class)
- `[ReducerMethod]` attribute for static reducer methods
- `[EffectMethod]` attribute for async side effects
- Store registered as scoped service -- each Blazor circuit gets its own store instance
- `StoreInitializer` component required in layout for store initialization
- Supports `ScanAssemblies()` for automatic discovery of features, reducers, and effects
- In bUnit tests, call `IStore.InitializeAsync()` explicitly (no `StoreInitializer` component needed)

**Fluxor Feature Class Pattern:**
```csharp
public class FrontComposerThemeFeature : Feature<FrontComposerThemeState>
{
    public override string GetName() => "FrontComposerTheme";
    public override FrontComposerThemeState GetInitialState()
        => new(ThemeValue.Light);
}
```

### References

- [Source: _bmad-output/planning-artifacts/architecture.md -- ADR-008: Fluxor State Shape Convention]
- [Source: _bmad-output/planning-artifacts/architecture.md -- State Management section, Per-Concern Features table]
- [Source: _bmad-output/planning-artifacts/architecture.md -- Component Subscription Wiring Path B]
- [Source: _bmad-output/planning-artifacts/architecture.md -- Folder structure: Shell/State/{Concern}/]
- [Source: _bmad-output/planning-artifacts/architecture.md -- Error handling: "No exception ever swallowed"]
- [Source: _bmad-output/planning-artifacts/epics.md -- Epic 1, Story 1.3]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md -- UX-DR23 (color/theme), UX-DR27 (density)]
- [Source: _bmad-output/planning-artifacts/prd.md -- FR2, FR3 state management requirements]
- [Source: _bmad-output/implementation-artifacts/1-1-solution-structure-and-build-infrastructure.md -- build patterns]
- [Source: _bmad-output/implementation-artifacts/1-2-contracts-package-with-core-abstractions.md -- IStorageService contract]
- [Source: src/Hexalith.FrontComposer.Contracts/Storage/IStorageService.cs -- 5-method interface, Task not ValueTask]
- [Source: src/Hexalith.FrontComposer.Contracts/Rendering/RenderContext.cs -- DensityLevel reuse]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- CA1062/CA2007 analyzer errors fixed by adding ArgumentNullException.ThrowIfNull and ConfigureAwait(false) per project .editorconfig
- bUnit 2.7.2 uses BunitContext (not deprecated TestContext); xUnit v3 IAsyncLifetime removed from base
- NSubstitute .Returns() incompatible with nullable value types from unconstrained generic GetAsync<T> — switched to InMemoryStorageService in effect tests
- GetAsync<T> for value types with unconstrained generics returns default(T) not null — fixed hydration effects to use GetAsync<object> with pattern matching (stored is ThemeValue theme) for correct null detection
- xUnit1051 enforces CancellationToken on all methods accepting it — added TestContext.Current.CancellationToken to all test methods

### Completion Notes List

- AC1: Fluxor registered via AddHexalithFrontComposer(). IStore, IState<ThemeState>, IState<DensityState>, IDispatcher all resolve. StoreInitializer placement documented in XML doc.
- AC2: Test components (TestThemeComponent.razor, TestDensityComponent.razor) demonstrate IState<T> + IDisposable pattern. FluxorComponent is never used. Subscription/dispose lifecycle verified via bUnit tests.
- AC3: FrontComposerThemeFeature (Light default) and FrontComposerDensityFeature (Comfortable default) with immutable record actions, CorrelationId, past-tense naming. Static [ReducerMethod] reducers.
- AC4: Effects persist to IStorageService with try/catch + ILogger. Keys use StorageKeys.BuildKey with default/anonymous placeholders.
- AC5: Hydration effects respond to AppInitializedAction, retrieve stored values via GetAsync<object> with pattern matching. Feature defaults apply when storage is empty.
- AC6: FrontComposerTestBase pre-configures Fluxor, InMemoryStorageService, mock IOverrideRegistry. InitializeStoreAsync() called per test.
- Key design decision: Effects use GetAsync<object> instead of GetAsync<ThemeValue> for hydration because IStorageService.GetAsync<T> with unconstrained generics returns default(T) for value types instead of null when key not found. Pattern matching (stored is ThemeValue theme) properly handles null.

### File List

**New files (src/Hexalith.FrontComposer.Shell/):**
- Extensions/ServiceCollectionExtensions.cs
- State/AppInitializedAction.cs
- State/StorageKeys.cs
- State/Theme/ThemeValue.cs
- State/Theme/FrontComposerThemeState.cs
- State/Theme/FrontComposerThemeFeature.cs
- State/Theme/ThemeActions.cs
- State/Theme/ThemeReducers.cs
- State/Theme/ThemeEffects.cs
- State/Density/FrontComposerDensityState.cs
- State/Density/FrontComposerDensityFeature.cs
- State/Density/DensityActions.cs
- State/Density/DensityReducers.cs
- State/Density/DensityEffects.cs

**Modified files:**
- src/Hexalith.FrontComposer.Shell/_Imports.razor (added Fluxor and State usings)
- Hexalith.FrontComposer.sln (added Shell.Tests project under tests/)

**New files (tests/Hexalith.FrontComposer.Shell.Tests/):**
- Hexalith.FrontComposer.Shell.Tests.csproj
- _Imports.razor
- FrontComposerTestBase.cs
- Components/TestThemeComponent.razor
- Components/TestDensityComponent.razor
- State/Theme/ThemeReducersTests.cs
- State/Theme/ThemeFeatureTests.cs
- State/Theme/ThemeEffectsTests.cs
- State/Density/DensityReducersTests.cs
- State/Density/DensityFeatureTests.cs
- State/Density/DensityEffectsTests.cs
- State/HydrationTests.cs
- State/SubscriptionLifecycleTests.cs
- State/FluxorRegistrationTests.cs
- State/TestBaseTests.cs

### Change Log

- 2026-04-13: Story 1.3 implemented — Fluxor state management foundation with Theme/Density features, persistence effects, hydration, bUnit test infrastructure, and 32 comprehensive tests. All ACs satisfied.
