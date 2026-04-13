# Story 1.2: Contracts Package with Core Abstractions

Status: done

## Story

As a developer,
I want a Contracts package containing domain model attributes, renderer abstractions, and storage service contracts,
So that all other framework packages and adopter code can depend on stable, change-controlled contracts.

## Acceptance Criteria

1. **Given** the `Hexalith.FrontComposer.Contracts` project exists
   **When** the project is built
   **Then** it multi-targets `net10.0` and `netstandard2.0`
   **And** it has zero package dependencies (dependency-free)
   **And** it includes an `IsExternalInit` polyfill for netstandard2.0 (required for `record` types and `init` accessors)
   **And** it compiles successfully under both target frameworks with zero warnings and no `<NoWarn>` or `#pragma warning disable` suppressions

2. **Given** the Contracts package is referenced
   **When** a developer inspects available attributes
   **Then** the following attributes are defined: `[BoundedContext(name)]`, `[Projection]`, `[Command]`, `[ProjectionRole(role)]`, `[ProjectionBadge(slot)]`
   **And** `ProjectionRole` supports `ActionQueue`, `StatusOverview`, `DetailRecord`, `Timeline`, `Dashboard` (capped at 5)
   **And** `BadgeSlot` supports `Neutral`, `Info`, `Success`, `Warning`, `Danger`, `Accent` (6 slots)
   **And** for display metadata, the framework uses `System.ComponentModel.DataAnnotations.DisplayAttribute` (no custom attribute needed -- it is in the BCL for both target frameworks)

3. **Given** the Contracts package is referenced
   **When** a developer inspects renderer abstractions
   **Then** `IRenderer<TModel, TOutput>` interface is defined with `Render` and `CanRender` methods
   **And** `RenderContext` record is defined with `TenantId`, `UserId`, `Mode` (FcRenderMode), `DensityLevel`, `IsReadOnly` properties
   **And** `FieldDescriptor` record is defined with at minimum `Name` (string), `TypeName` (string), `IsNullable` (bool), and `Hints` (RenderHints) properties
   **And** `RenderHints` record is defined with rendering customization properties (badge slot, currency, date format, sortable, filterable)

4. **Given** the Contracts package is referenced
   **When** a developer inspects storage abstractions
   **Then** `IStorageService` is defined with 5 methods: `GetAsync<T>`, `SetAsync<T>`, `RemoveAsync`, `GetKeysAsync`, `FlushAsync`
   **And** `InMemoryStorageService` implementation exists for Server-side and bUnit testing
   **And** `InMemoryStorageService` returns null (not throws) when a key is not found
   **And** cache key pattern follows `{tenantId}:{userId}:{featureName}:{discriminator}`

5. **Given** the Contracts package is referenced
   **When** a developer inspects communication abstractions
   **Then** `ICommandService` is defined with a `DispatchAsync` method returning `CommandResult`
   **And** `IQueryService` is defined with a `QueryAsync` method taking `QueryRequest` and returning `QueryResult<T>`
   **And** `IProjectionSubscription` is defined with `SubscribeAsync` and `UnsubscribeAsync` methods
   **And** `IProjectionChangeNotifier` is defined with a `ProjectionChanged` event and `NotifyChanged` method
   **And** all four interfaces are marked as provisional via XML doc comment (may receive non-breaking additions after Story 1.3)

6. **Given** the Contracts package is referenced
   **When** a developer inspects registration abstractions
   **Then** `IFrontComposerRegistry`, `IOverrideRegistry`, and `DomainManifest` are defined
   **And** `IFrontComposerRegistry` has methods for registering domains and nav groups

7. **Given** the Contracts package is referenced
   **When** a developer inspects lifecycle abstractions
   **Then** `CommandLifecycleState` enum and `ICommandLifecycleTracker` interface are defined
   **And** `ICommandLifecycleTracker` has `GetState`, `Transition`, and `GetActiveCommandIds` methods

8. **Given** the `IRenderer` and `IStorageService` contracts
   **When** they are designed in this story
   **Then** they are provisional -- designed with awareness of Fluxor state patterns (Story 1.3) but may be hardened after Fluxor setup validates the state shape
   **And** any contract changes in Story 1.3 are applied as non-breaking additions (new methods/properties), not redesigns

## Definition of Done

- [ ] All files compile with zero warnings on both `net10.0` and `netstandard2.0` targets
- [ ] Zero `<PackageReference>` or `<ProjectReference>` items in Contracts `.csproj`
- [ ] Every namespace matches its folder path exactly
- [ ] All public types have `/// <summary>` XML doc comments
- [ ] `InMemoryStorageService` has minimum 9 passing unit tests in `Contracts.Tests` (including concurrent access)
- [ ] `dotnet build` from repo root succeeds for all 7 projects with zero warnings
- [ ] `dotnet test` from repo root passes with zero failures
- [ ] All provisional interfaces marked with XML doc comment indicating provisional status

## Tasks / Subtasks

- [x] Task 0: Create netstandard2.0 polyfill (AC: #1)
  - [x] 0.1 Create `Internals/IsExternalInit.cs` -- `#if NETSTANDARD2_0` polyfill for `System.Runtime.CompilerServices.IsExternalInit` (required for `record` and `init` on netstandard2.0). The `#if NETSTANDARD2_0` guard MUST wrap both the namespace declaration AND the class -- not just the class body. Otherwise net10.0 build fails with CS0436 (duplicate type).
  - [x] 0.2 Run `dotnet build -f netstandard2.0 src/Hexalith.FrontComposer.Contracts/` to verify the polyfill is in place before proceeding with other tasks

- [x] Task 1: Create attribute definitions (AC: #2)
  - [x] 1.1 Create `Attributes/BoundedContextAttribute.cs` -- `[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]`, takes `string name` constructor parameter
  - [x] 1.2 Create `Attributes/ProjectionAttribute.cs` -- `[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]`, marker attribute
  - [x] 1.3 Create `Attributes/CommandAttribute.cs` -- `[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]`, marker attribute
  - [x] 1.4 Create `Attributes/ProjectionRoleAttribute.cs` -- `[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]`, takes `ProjectionRole role` enum parameter
  - [x] 1.5 Create `Attributes/ProjectionBadgeAttribute.cs` -- `[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]`, takes `BadgeSlot slot` enum parameter
  - [x] 1.6 Document in a code comment in `ProjectionAttribute.cs` that `System.ComponentModel.DataAnnotations.DisplayAttribute` is the recommended display metadata attribute (no custom attribute needed)
  - [x] 1.7 Create `Attributes/ProjectionRole.cs` enum -- `ActionQueue`, `StatusOverview`, `DetailRecord`, `Timeline`, `Dashboard` (5 values)
  - [x] 1.8 Create `Attributes/BadgeSlot.cs` enum -- `Neutral`, `Info`, `Success`, `Warning`, `Danger`, `Accent` (6 values)

- [x] Task 2: Create rendering abstractions (AC: #3)
  - [x] 2.1 Create `Rendering/IRenderer.cs` -- generic `IRenderer<TModel, TOutput>` with `TOutput Render(TModel model, RenderContext context)` and `bool CanRender(TModel model)`
  - [x] 2.2 Create `Rendering/RenderContext.cs` -- immutable record with `TenantId` (string), `UserId` (string), `Mode` (FcRenderMode enum), `DensityLevel` (DensityLevel enum), `IsReadOnly` (bool)
  - [x] 2.3 Create `Rendering/FcRenderMode.cs` enum -- `Server`, `WebAssembly`, `Auto` (named `FcRenderMode` to avoid collision with Blazor's `Microsoft.AspNetCore.Components.Web.RenderMode`)
  - [x] 2.4 Create `Rendering/DensityLevel.cs` enum -- `Compact`, `Comfortable`, `Roomy`
  - [x] 2.5 Create `Rendering/FieldDescriptor.cs` -- `public record FieldDescriptor(string Name, string TypeName, bool IsNullable, string? DisplayName = null, string? Format = null, int? Order = null, bool IsReadOnly = false, RenderHints? Hints = null);` -- see Dev Notes for property descriptions
  - [x] 2.6 Create `Rendering/RenderHints.cs` -- `public record RenderHints(BadgeSlot? BadgeSlot = null, string? CurrencyCode = null, string? DateFormat = null, string? Icon = null, bool IsSortable = true, bool IsFilterable = true);` (AC: #3 -- renderer abstractions, used by FieldDescriptor)

- [x] Task 3: Create storage abstractions (AC: #4)
  - [x] 3.1 Create `Storage/IStorageService.cs` -- 5 async methods using `Task<T?>` (NOT `ValueTask` -- see Dev Notes on netstandard2.0 compatibility)
  - [x] 3.2 Create `Storage/InMemoryStorageService.cs` -- `sealed` class, thread-safe implementation using `ConcurrentDictionary<string, object>` (requires `using System.Collections.Concurrent;`, available in netstandard2.0 BCL). Do NOT use `async` keyword -- return `Task.FromResult` / `Task.CompletedTask` directly to avoid CA2007 (ConfigureAwait) warnings. For `FlushAsync`, reference the dictionary field (e.g., `_ = _store.Count;`) to avoid CA1822 ("can be made static").

- [x] Task 4: Create communication abstractions (AC: #5)
  - [x] 4.1 Create `Communication/ICommandService.cs` -- command dispatch contract with `DispatchAsync` method
  - [x] 4.2 Create `Communication/CommandResult.cs` -- `public record CommandResult(string MessageId, string Status);`
  - [x] 4.3 Create `Communication/IQueryService.cs` -- query execution contract with `QueryAsync` method
  - [x] 4.4 Create `Communication/QueryRequest.cs` -- `public record QueryRequest(string ProjectionType, string TenantId, string? Filter = null, int? Skip = null, int? Take = null, string? ETag = null);`
  - [x] 4.5 Create `Communication/QueryResult.cs` -- `public record QueryResult<T>(IReadOnlyList<T> Items, int TotalCount, string? ETag);`
  - [x] 4.6 Create `Communication/IProjectionSubscription.cs` -- `SubscribeAsync` and `UnsubscribeAsync` methods
  - [x] 4.7 Create `Communication/IProjectionChangeNotifier.cs` -- `event Action<string>? ProjectionChanged;` and `void NotifyChanged(string projectionType);`

- [x] Task 5: Create registration abstractions (AC: #6)
  - [x] 5.1 Create `Registration/IFrontComposerRegistry.cs` -- see Dev Notes for method signatures
  - [x] 5.2 Create `Registration/DomainManifest.cs` -- record with `Name`, `BoundedContext`, `Projections`, `Commands`
  - [x] 5.3 Create `Registration/IOverrideRegistry.cs` -- customization gradient support, see Dev Notes for method signatures (AC: #6 -- registration abstractions)

- [x] Task 6: Create lifecycle abstractions (AC: #7)
  - [x] 6.1 Create `Lifecycle/CommandLifecycleState.cs` -- enum: `Idle`, `Submitting`, `Acknowledged`, `Syncing`, `Confirmed`, `Rejected`
  - [x] 6.2 Create `Lifecycle/ICommandLifecycleTracker.cs` -- see Dev Notes for method signatures

- [x] Task 7: Create telemetry contract (AC: #1 -- zero-dependency build constraint applies to the static constants class)
  - [x] 7.1 Create `Telemetry/FrontComposerActivitySource.cs` -- static class with `Name = "Hexalith.FrontComposer"` and `Version = "0.1.0"` constants

- [x] Task 8: Create Contracts.Tests project (AC: #4)
  - [x] 8.1 Create `tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj` -- target `net10.0` only, reference xUnit v3, Shouldly, add `<ProjectReference>` to Contracts
  - [x] 8.2 Add project to `Hexalith.FrontComposer.sln` under the `tests` solution folder
  - [x] 8.3 Create `InMemoryStorageServiceTests.cs` with minimum 9 tests:
    - `GetAsync_KeyDoesNotExist_ReturnsNull`
    - `SetAsync_ThenGetAsync_ReturnsStoredValue` (roundtrip with correct type)
    - `SetAsync_SameKeyTwice_OverwritesValue`
    - `RemoveAsync_ExistingKey_RemovesIt`
    - `RemoveAsync_MissingKey_DoesNotThrow`
    - `GetKeysAsync_WithPrefix_ReturnsOnlyMatchingKeys`
    - `GetKeysAsync_NoMatchingKeys_ReturnsEmptyList`
    - `FlushAsync_CompletesSuccessfully`
    - `SetAndGetAsync_ConcurrentAccess_NoDataCorruption` (100 parallel writes + reads, validates no lost updates)

- [x] Task 9: Verify build and constraints (AC: #1) -- run AFTER Task 8 so all 7 projects exist
  - [x] 9.1 Verify `dotnet build` succeeds for both `net10.0` and `netstandard2.0` targets with zero warnings
  - [x] 9.2 Verify zero package dependencies in Contracts `.csproj` (only framework references, no `<NoWarn>` or `<WarningsNotAsErrors>` directives)
  - [x] 9.3 Verify namespace matches folder path for every file
  - [x] 9.4 Run `dotnet build` from repo root -- all 8 projects (including Contracts.Tests) compile cleanly (no regressions)
  - [x] 9.5 Run `dotnet test` from repo root -- all tests pass with zero failures

### Review Findings

- [x] `[Review][Patch]` Align `FlushAsync` with the no-op flush contract [src/Hexalith.FrontComposer.Contracts/Storage/InMemoryStorageService.cs:14]
- [x] `[Review][Patch]` Remove the impossible "non-breaking additions" promise from provisional interfaces [src/Hexalith.FrontComposer.Contracts/Storage/IStorageService.cs:4]
- [x] `[Review][Patch]` Mark `IRenderer` as provisional like the other Story 1.2 contracts [src/Hexalith.FrontComposer.Contracts/Rendering/IRenderer.cs:3]

## Dev Notes

### CRITICAL: netstandard2.0 Compatibility Constraints

The Contracts project multi-targets `net10.0;netstandard2.0` and MUST remain dependency-free. The `netstandard2.0` target exists so this package can be referenced by Roslyn analyzer/source-generator host processes (which run in a netstandard2.0 load context inside VS/Rider) and by any future .NET Standard-compatible consumer. Without it, the SourceTools generator could not reference Contracts attribute types at compile time. This creates specific design constraints:

1. **NO `ValueTask`** -- `ValueTask<T>` requires `System.Threading.Tasks.Extensions` NuGet package on netstandard2.0. Use `Task<T?>` for all async interface methods instead. The implementations (in Shell or other packages targeting net10.0) can internally use `ValueTask` if needed.

2. **NO Blazor types in Contracts** -- `RenderFragment`, `RenderTreeBuilder`, `ComponentBase`, `RenderMode` (from `Microsoft.AspNetCore.Components`) are NOT available in netstandard2.0 and would add a dependency. The architecture's `IProjectionRenderer<TProjection> : IRenderer<TProjection, RenderFragment>` binding CANNOT live in Contracts. Solution:
   - `IRenderer<TModel, TOutput>` stays in Contracts with generic `TOutput` (no Blazor dependency)
   - `IProjectionRenderer<TProjection>` that binds `TOutput = RenderFragment` will be defined in Shell (Story 1.3+), NOT in Contracts
   - The render mode enum is named `FcRenderMode` in the `Rendering` namespace to avoid collision with Blazor's `Microsoft.AspNetCore.Components.Web.RenderMode`

3. **NO `System.Diagnostics.DiagnosticSource`** -- `ActivitySource` requires this package on netstandard2.0. Define `FrontComposerActivitySource` as a simple static class with string constants only. The actual `ActivitySource` instantiation happens in Shell.

4. **NO `System.Text.Json`** attributes or types -- not available in netstandard2.0 without package reference.

5. **`record` types** -- C# records require `LangVersion=latest` (already set in `Directory.Build.props`) but also need `System.Runtime.CompilerServices.IsExternalInit` for netstandard2.0. The compiler synthesizes this for net10.0, but for netstandard2.0 you MUST add a polyfill:
   ```csharp
   // Place in Internals/IsExternalInit.cs
   // The #if MUST wrap BOTH namespace and class -- otherwise net10.0 gets CS0436 (duplicate type)
   #if NETSTANDARD2_0
   namespace System.Runtime.CompilerServices
   {
       internal static class IsExternalInit { }
   }
   #endif
   ```
   Without this, records will fail to compile on the netstandard2.0 target with CS0518.

6. **`init` accessors** -- Same `IsExternalInit` polyfill covers this.

7. **Nullable reference types** -- Supported via `LangVersion=latest` + `<Nullable>enable</Nullable>` (already in `Directory.Build.props`). Works on netstandard2.0 at the syntax level.

8. **DO NOT use `required` modifier** -- `required` (C# 11) needs `RequiredMemberAttribute` and `CompilerFeatureRequiredAttribute` polyfills NOT included here. Modern C# training data is full of `required` properties -- resist the temptation.

9. **DO NOT use default interface method implementations (DIM)** -- Not supported on netstandard2.0 runtime. The compiler may emit the code but the target runtime does not support DIM.

10. **DO NOT use `static abstract` interface members** -- .NET 7+ feature, unavailable on netstandard2.0.

11. **File-scoped namespaces ARE supported** -- `namespace X.Y.Z;` works with `LangVersion=latest` on netstandard2.0. MUST be used per `.editorconfig` (`csharp_style_namespace_declarations = file_scoped:warning` + `TreatWarningsAsErrors=true`).

12. **DO NOT add `PolySharp`, `Polyfill`, or any polyfill NuGet package** -- use the hand-written `IsExternalInit` polyfill only.

13. **DO NOT add `System.ComponentModel.Annotations` as a PackageReference** -- `DisplayAttribute` is already available via the BCL framework reference on both targets.

14. **`IReadOnlyList<T>` is the ceiling for collection types** -- do NOT use `ImmutableArray<T>` (requires `System.Collections.Immutable` NuGet on netstandard2.0).

### CRITICAL: IProjectionRenderer Does NOT Belong in Contracts

The architecture document shows `IProjectionRenderer<TProjection>` in `Contracts/Rendering/`, but this interface returns `RenderFragment` (a Blazor type). Since Contracts is dependency-free, `IProjectionRenderer` MUST live in Shell or a separate Blazor-aware package. In this story, only define:
- `IRenderer<TModel, TOutput>` (generic, no Blazor dependency)
- `FieldDescriptor` (pure data record)
- `RenderContext` (pure data record with custom enums)
- `RenderHints` (pure data record)

The Blazor-specific renderer binding happens in Story 1.3+ when Shell gets actual component code.

### Display Attribute Strategy

**Decision: Reuse `System.ComponentModel.DataAnnotations.DisplayAttribute` from the BCL.** It is available on both net10.0 and netstandard2.0 without any package reference, and the source generator can read it via Roslyn symbol analysis. No custom attribute file is created. Document this decision in a code comment in `ProjectionAttribute.cs`.

### IStorageService Design

```csharp
/// <summary>
/// Provisional: may receive non-breaking additions after Story 1.3 (Fluxor integration).
/// </summary>
public interface IStorageService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetKeysAsync(string prefix, CancellationToken cancellationToken = default);
    Task FlushAsync(CancellationToken cancellationToken = default);
}
```

Key design decisions:
- Use `Task` not `ValueTask` (netstandard2.0 compatibility)
- Include `CancellationToken` on all async methods (best practice)
- `GetAsync<T>` returns `T?` (nullable) for cache miss -- returns null, never throws for missing keys
- Cache key pattern: `{tenantId}:{userId}:{featureName}:{discriminator}`
- `FlushAsync` drains pending writes (called on `beforeunload` via JS interop in Shell)
- `SetAsync` is fire-and-forget semantically -- implementation should write asynchronously without blocking render path

**Architecture reconciliation:** The architecture document shows `ValueTask<T?>` signatures. This story intentionally uses `Task<T?>` instead because `ValueTask` requires the `System.Threading.Tasks.Extensions` NuGet package on netstandard2.0, which would violate the zero-dependency constraint. The architecture document should be updated to match after this story ships.

### InMemoryStorageService Implementation

- Use `ConcurrentDictionary<string, object>` for thread safety (requires `using System.Collections.Concurrent;`)
- Store values as `object` and cast on retrieval via `(T)(object)value!`
- `FlushAsync` is a no-op for in-memory (no pending writes) -- reference the dictionary field (e.g., `_ = _store.Count;`) to avoid CA1822 ("can be made static")
- `GetKeysAsync` filters by prefix using `Keys.Where(k => k.StartsWith(prefix))`
- Do NOT mark methods as `async` -- return `Task.FromResult<T?>()` / `Task.CompletedTask` directly to avoid CA2007 (ConfigureAwait) warnings
- Do NOT add redundant `using` directives -- `ImplicitUsings=enable` is on
- Class must be `sealed`

**Placement decision:** `InMemoryStorageService` lives in Contracts (not Shell) intentionally. It is the server-side and bUnit testing implementation that ships as part of the contracts package. It has zero dependencies beyond BCL types (`ConcurrentDictionary<string, object>`), so it does not violate the dependency-free constraint. Consumers reference Contracts alone for both the interface and the server/test-double implementation. The localStorage implementation (`LocalStorageService`) lives in Shell because it depends on Blazor JS interop.

### Communication Interface Signatures

These are provisional -- designed to express intent, hardened in later stories. Mark all interfaces with `/// <summary>Provisional: may receive non-breaking additions after Story 1.3.</summary>`.

```csharp
// ICommandService -- dispatches commands to EventStore REST API
public interface ICommandService
{
    Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class;
}

// CommandResult -- 202 Accepted response shape
// MessageId: non-empty ULID string (generated on command dispatch)
// Status: "Accepted" (command queued) or "Rejected" (domain validation failed)
public record CommandResult(string MessageId, string Status);

// IQueryService -- executes queries against projections
public interface IQueryService
{
    Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default);
}

// QueryRequest -- projection query parameters
public record QueryRequest(
    string ProjectionType,
    string TenantId,
    string? Filter = null,
    int? Skip = null,
    int? Take = null,
    string? ETag = null);

// QueryResult<T> -- query response with ETag for cache invalidation
public record QueryResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    string? ETag);

// IProjectionSubscription -- manages SignalR group subscriptions
public interface IProjectionSubscription
{
    Task SubscribeAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default);
}

// IProjectionChangeNotifier -- notifies Shell of projection changes
public interface IProjectionChangeNotifier
{
    event Action<string>? ProjectionChanged;
    void NotifyChanged(string projectionType);
}
```

### DomainManifest Record

```csharp
public record DomainManifest(
    string Name,
    string BoundedContext,
    IReadOnlyList<string> Projections,
    IReadOnlyList<string> Commands);
```

### IFrontComposerRegistry Interface

```csharp
/// <summary>
/// Runtime composition bridge. Source generator emits RegisterDomain() calls against this interface.
/// Provisional: may receive non-breaking additions after Story 1.3.
/// </summary>
public interface IFrontComposerRegistry
{
    void RegisterDomain(DomainManifest manifest);
    void AddNavGroup(string name, string boundedContext);
    IReadOnlyList<DomainManifest> GetManifests();
}
```

Keep method signatures minimal. `RegisterDomain` takes a complete `DomainManifest` (which already carries projection and command lists), avoiding redundant per-type registration methods. The implementation lives in Shell (Story 1.3+). Generic convenience overloads will be added as non-breaking additions when the generator is implemented.

### IOverrideRegistry Interface

```csharp
/// <summary>
/// Customization gradient support. Placeholder for v1 -- method set is provisional.
/// </summary>
public interface IOverrideRegistry
{
    void Register(string projectionType, string overrideType, Type implementationType);
    Type? Resolve(string projectionType, string overrideType);
}
```

Keep non-generic for netstandard2.0 compatibility. Generic convenience methods (`AddSlotOverride<T,TSlot>()`, `AddViewOverride<T,TView>()`) will be added as extension methods in Shell when the customization gradient feature is implemented.

### CommandLifecycleState Enum

Five states plus Rejected (6 total):
```csharp
public enum CommandLifecycleState
{
    Idle,          // Default -- no command in flight
    Submitting,    // Command sent, awaiting acknowledgement
    Acknowledged,  // EventStore accepted (202), awaiting projection sync
    Syncing,       // Projection update detected, applying to UI
    Confirmed,     // Projection state confirmed in UI
    Rejected       // Command rejected by domain logic
}
```

### ICommandLifecycleTracker Interface

```csharp
/// <summary>
/// Tracks command lifecycle state transitions.
/// Provisional: may receive non-breaking additions after Story 1.3 (Fluxor integration).
/// </summary>
public interface ICommandLifecycleTracker
{
    /// <summary>
    /// Returns the current lifecycle state for the given command.
    /// Returns <see cref="CommandLifecycleState.Idle"/> for unrecognized command IDs.
    /// </summary>
    CommandLifecycleState GetState(string commandId);
    void Transition(string commandId, CommandLifecycleState newState);
    IReadOnlyList<string> GetActiveCommandIds();
}
```

The implementation lives in Shell (backed by Fluxor state). `commandId` corresponds to the ULID `MessageId` from `CommandResult`. `GetState` returns `Idle` (not null, not throw) for unknown IDs -- this is the safe default because unrecognized IDs represent commands that have completed and been evicted from the tracker.

### FieldDescriptor Record

```csharp
/// <summary>
/// Immutable field-level rendering metadata. Used by convention-based ProjectionRenderer
/// to determine how to render each field in data grids, detail views, and forms.
/// </summary>
public record FieldDescriptor(
    string Name,
    string TypeName,
    bool IsNullable,
    string? DisplayName = null,
    string? Format = null,
    int? Order = null,
    bool IsReadOnly = false,
    RenderHints? Hints = null);
```

`TypeName` is the CLR type name as a string (e.g., `"System.Int32"`, `"System.String"`) rather than `System.Type` to keep the record serialization-friendly and avoid reflection dependencies. `DisplayName` falls back to `Name` if null. `Format` is a standard .NET format string (e.g., `"C2"` for currency).

### RenderHints Record

```csharp
/// <summary>
/// Additional rendering hints for field-level customization.
/// </summary>
public record RenderHints(
    BadgeSlot? BadgeSlot = null,
    string? CurrencyCode = null,
    string? DateFormat = null,
    string? Icon = null,
    bool IsSortable = true,
    bool IsFilterable = true);
```

### Diagnostic ID Range

Contracts owns `HFC0001-HFC0999`. No diagnostics are emitted in this story (Contracts is a library, not an analyzer), but if you add any `[Obsolete]` or compilation messages, use IDs from this range.

### FrontComposerActivitySource Strategy

For telemetry, define a simple constants class rather than instantiating `ActivitySource` directly (which requires `System.Diagnostics.DiagnosticSource`):

```csharp
namespace Hexalith.FrontComposer.Contracts.Telemetry;

/// <summary>
/// Shared telemetry source name and version constants for the FrontComposer framework.
/// The actual ActivitySource instance is created in Shell (net10.0).
/// </summary>
public static class FrontComposerActivitySource
{
    public const string Name = "Hexalith.FrontComposer";
    public const string Version = "0.1.0";
}
```

### Project Structure Notes

All files go in `src/Hexalith.FrontComposer.Contracts/` with this folder structure:
```
src/Hexalith.FrontComposer.Contracts/
├── Hexalith.FrontComposer.Contracts.csproj  (exists from Story 1.1 -- DO NOT modify)
├── Internals/
│   └── IsExternalInit.cs                    (netstandard2.0 polyfill for records)
├── Attributes/
│   ├── BoundedContextAttribute.cs
│   ├── CommandAttribute.cs
│   ├── ProjectionAttribute.cs
│   ├── ProjectionRoleAttribute.cs
│   ├── ProjectionBadgeAttribute.cs
│   ├── ProjectionRole.cs                    (enum)
│   └── BadgeSlot.cs                         (enum)
├── Rendering/
│   ├── IRenderer.cs
│   ├── RenderContext.cs
│   ├── FcRenderMode.cs                      (custom enum, NOT Blazor's RenderMode)
│   ├── DensityLevel.cs                      (enum)
│   ├── FieldDescriptor.cs
│   └── RenderHints.cs
├── Storage/
│   ├── IStorageService.cs
│   └── InMemoryStorageService.cs
├── Communication/
│   ├── ICommandService.cs
│   ├── CommandResult.cs
│   ├── IQueryService.cs
│   ├── QueryRequest.cs
│   ├── QueryResult.cs
│   ├── IProjectionSubscription.cs
│   └── IProjectionChangeNotifier.cs
├── Registration/
│   ├── IFrontComposerRegistry.cs
│   ├── DomainManifest.cs
│   └── IOverrideRegistry.cs
├── Lifecycle/
│   ├── CommandLifecycleState.cs
│   └── ICommandLifecycleTracker.cs
└── Telemetry/
    └── FrontComposerActivitySource.cs

tests/Hexalith.FrontComposer.Contracts.Tests/
├── Hexalith.FrontComposer.Contracts.Tests.csproj
└── InMemoryStorageServiceTests.cs
```

Namespace matches folder path exactly:
- `Hexalith.FrontComposer.Contracts.Attributes`
- `Hexalith.FrontComposer.Contracts.Rendering`
- `Hexalith.FrontComposer.Contracts.Storage`
- `Hexalith.FrontComposer.Contracts.Communication`
- `Hexalith.FrontComposer.Contracts.Registration`
- `Hexalith.FrontComposer.Contracts.Lifecycle`
- `Hexalith.FrontComposer.Contracts.Telemetry`

Exception: `Internals/IsExternalInit.cs` uses `namespace System.Runtime.CompilerServices` (required by compiler).

### Code Style (from Story 1.1 .editorconfig)

- File-scoped namespaces: `namespace X.Y.Z;`
- Allman braces (new line before opening brace)
- Private fields: `_camelCase`
- Interfaces: `I` prefix
- One type per file
- 4-space indent, CRLF, UTF-8
- `TreatWarningsAsErrors=true` -- zero warnings allowed
- `Nullable=enable` -- all reference types explicitly nullable or non-nullable
- `LangVersion=latest`
- All public types and members MUST have `/// <summary>` XML documentation comments
- All records must use positional parameters or `init` accessors -- no mutable `set` properties
- `InMemoryStorageService` must be `sealed`
- Do NOT create helper classes, base classes, extension methods, or utility types not listed in the project structure
- Do NOT modify the Contracts `.csproj` file. It must remain exactly as created in Story 1.1.

### Package Boundary Enforcement

Contracts MUST reference NOTHING. Verify the `.csproj` has no `<PackageReference>`, `<ProjectReference>`, `<NoWarn>`, or `<WarningsNotAsErrors>` items. The only allowed content is:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>
  </PropertyGroup>
</Project>
```

### Regression Prevention

After implementation, verify from repo root:
- `dotnet restore` -- zero warnings
- `dotnet build` -- zero errors, zero warnings (all 7 projects, including new Contracts.Tests)
- `dotnet build src/Hexalith.FrontComposer.Contracts/ -f netstandard2.0` -- explicit netstandard2.0-only validation
- `dotnet test` -- zero test runner errors (Contracts.Tests must pass 9+ InMemoryStorageService tests including concurrent access; SourceTools.Tests is still empty but must run cleanly)

### Previous Story Intelligence (Story 1.1)

**Key learnings to apply:**
- `LangVersion=latest` is already set globally -- enables records and nullable on netstandard2.0
- `TreatWarningsAsErrors=true` is enforced -- every warning is a build failure
- Solution uses classic `.sln` format (not `.slnx`)
- EventStore submodule uses xUnit v3 3.2.2 -- test infrastructure is consistent
- `Microsoft.CodeAnalysis.CSharp` pinned to 4.12.0 exactly (relevant for SourceTools, not Contracts)
- Fluent UI v5 RC2 pinned (relevant for Shell, not Contracts)

**Files created in Story 1.1 that this story builds upon:**
- `src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj` -- exists, empty project shell
- `Directory.Build.props` -- global build properties (LangVersion, Nullable, TreatWarningsAsErrors)
- `Directory.Packages.props` -- central package management (not relevant for dependency-free Contracts, but Contracts.Tests uses test package versions from here)
- `.editorconfig` -- code style rules

### References

- [Source: _bmad-output/planning-artifacts/epics.md] -- Story 1.2 acceptance criteria, dependency chain
- [Source: _bmad-output/planning-artifacts/architecture.md] -- ADR-001 (multi-targeting), package boundaries, interface skeletons, namespace hierarchy, folder structure
- [Source: _bmad-output/planning-artifacts/prd.md] -- FR3, FR6, FR13, 8 headline packages, IStorageService 5 methods, command lifecycle states
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md] -- DensityLevel values, FcRenderMode, badge slots, lifecycle states, Fc naming convention
- [Source: _bmad-output/implementation-artifacts/1-1-solution-structure-and-build-infrastructure.md] -- MSBuild isolation, build quality gates, code style, project reference patterns

## Change Log

- 2026-04-13: Implemented all Contracts package abstractions (Tasks 0-7), test project (Task 8), verified build and constraints (Task 9). All 9 InMemoryStorageService tests pass. Zero warnings on both net10.0 and netstandard2.0. Zero package dependencies confirmed.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- xUnit v3 requires `TestContext.Current.CancellationToken` for methods accepting CancellationToken (xUnit1051 rule, treated as error). Tests updated accordingly.
- `T?` on unconstrained generics does not produce `Nullable<T>` for value types — it is a nullability annotation only. Concurrent test uses `string` instead of `int` to work with `Task<T?>` array typing.

### Completion Notes List

- Task 0: Created `Internals/IsExternalInit.cs` polyfill with `#if NETSTANDARD2_0` guard wrapping both namespace and class. Verified both targets build clean.
- Task 1: Created all 7 attribute files + 1 code comment documenting DisplayAttribute strategy in ProjectionAttribute.cs. All enums match spec exactly (ProjectionRole: 5 values, BadgeSlot: 6 values).
- Task 2: Created 6 rendering files — IRenderer<TModel,TOutput>, RenderContext record, FcRenderMode enum, DensityLevel enum, FieldDescriptor record, RenderHints record. All use positional record parameters.
- Task 3: Created IStorageService (5 async methods with Task<T?>, CancellationToken) and sealed InMemoryStorageService using ConcurrentDictionary<string,object>. No async keyword — returns Task.FromResult/Task.CompletedTask directly per CA2007.
- Task 4: Created 7 communication files — all 4 interfaces marked provisional via XML doc. CommandResult/QueryRequest/QueryResult use positional records.
- Task 5: Created IFrontComposerRegistry (3 methods), DomainManifest record, IOverrideRegistry (non-generic for netstandard2.0). All match spec signatures.
- Task 6: Created CommandLifecycleState enum (6 states) and ICommandLifecycleTracker interface (3 methods). Marked provisional.
- Task 7: Created FrontComposerActivitySource static class with Name and Version string constants only (no ActivitySource instantiation).
- Task 8: Created Contracts.Tests project (xUnit v3, Shouldly, net10.0), added to solution. All 9 InMemoryStorageService tests pass including concurrent access (100 parallel writes + reads).
- Task 9: Verified dotnet build (8 projects, 0 warnings, 0 errors), dotnet test (9 passed, 0 failed), netstandard2.0-only build, zero dependencies, namespace-folder consistency.

### File List

New files:
- src/Hexalith.FrontComposer.Contracts/Internals/IsExternalInit.cs
- src/Hexalith.FrontComposer.Contracts/Attributes/BoundedContextAttribute.cs
- src/Hexalith.FrontComposer.Contracts/Attributes/CommandAttribute.cs
- src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionAttribute.cs
- src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionRoleAttribute.cs
- src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionBadgeAttribute.cs
- src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionRole.cs
- src/Hexalith.FrontComposer.Contracts/Attributes/BadgeSlot.cs
- src/Hexalith.FrontComposer.Contracts/Rendering/IRenderer.cs
- src/Hexalith.FrontComposer.Contracts/Rendering/RenderContext.cs
- src/Hexalith.FrontComposer.Contracts/Rendering/FcRenderMode.cs
- src/Hexalith.FrontComposer.Contracts/Rendering/DensityLevel.cs
- src/Hexalith.FrontComposer.Contracts/Rendering/FieldDescriptor.cs
- src/Hexalith.FrontComposer.Contracts/Rendering/RenderHints.cs
- src/Hexalith.FrontComposer.Contracts/Storage/IStorageService.cs
- src/Hexalith.FrontComposer.Contracts/Storage/InMemoryStorageService.cs
- src/Hexalith.FrontComposer.Contracts/Communication/ICommandService.cs
- src/Hexalith.FrontComposer.Contracts/Communication/CommandResult.cs
- src/Hexalith.FrontComposer.Contracts/Communication/IQueryService.cs
- src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs
- src/Hexalith.FrontComposer.Contracts/Communication/QueryResult.cs
- src/Hexalith.FrontComposer.Contracts/Communication/IProjectionSubscription.cs
- src/Hexalith.FrontComposer.Contracts/Communication/IProjectionChangeNotifier.cs
- src/Hexalith.FrontComposer.Contracts/Registration/IFrontComposerRegistry.cs
- src/Hexalith.FrontComposer.Contracts/Registration/DomainManifest.cs
- src/Hexalith.FrontComposer.Contracts/Registration/IOverrideRegistry.cs
- src/Hexalith.FrontComposer.Contracts/Lifecycle/CommandLifecycleState.cs
- src/Hexalith.FrontComposer.Contracts/Lifecycle/ICommandLifecycleTracker.cs
- src/Hexalith.FrontComposer.Contracts/Telemetry/FrontComposerActivitySource.cs
- tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj
- tests/Hexalith.FrontComposer.Contracts.Tests/InMemoryStorageServiceTests.cs

Modified files:
- Hexalith.FrontComposer.sln (added Contracts.Tests project)
- _bmad-output/implementation-artifacts/sprint-status.yaml (status: ready-for-dev -> review)
- _bmad-output/implementation-artifacts/1-2-contracts-package-with-core-abstractions.md (task checkboxes, Dev Agent Record, File List, Change Log, Status)
