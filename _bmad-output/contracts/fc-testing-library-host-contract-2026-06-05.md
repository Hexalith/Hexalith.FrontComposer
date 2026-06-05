# FC Testing Library Host Contract - Story 7.5

Date: 2026-06-05
Story: 7.5 - Testing library - bUnit host and deterministic fakes
Status: v1 contract

## Scope

`Hexalith.FrontComposer.Testing` provides adopter-facing bUnit utilities for generated FrontComposer component tests. The package must let tests run without EventStore, SignalR, browser storage, DAPR, or a running app host.

## Host Setup APIs

- Inheritance setup: derive from `FrontComposerTestBase`.
- Composition setup: call `Services.AddFrontComposerTestHost(context, configure)` from a `BunitContext`.
- The returned `FrontComposerTestHostBuilder` exposes `Options`, `UserContext`, `CommandService`, `QueryService`, `PageLoader`, and `FaultProvider`.
- Direct composition owns a culture scope. Dispose `FrontComposerTestHostBuilder` to restore `CultureInfo.CurrentCulture` and `CurrentUICulture`.
- `AddDomainAssembly<TMarker>()` adds a generated domain assembly once, scans it with Fluxor, and registers `AddHexalithDomain<TMarker>()`.

## Default Host Wiring

The host registers these defaults:

- `BunitContext.JSInterop.Mode = JSRuntimeMode.Loose` unless `FrontComposerTestOptions.JSInteropMode` overrides it.
- `AddLocalization()`.
- FluentUI Blazor v5 components through `AddFluentUIComponents()`.
- Shell defaults through `AddHexalithFrontComposer(...)`.
- `IStorageService -> InMemoryStorageService`.
- `FrontComposerTestUserContextAccessor` and `IUserContextAccessor`.
- `ICommandPageContext`.
- `ICommandServiceWithLifecycle` and `ICommandService` backed by `TestCommandService`.
- `IQueryService` backed by `TestQueryService`.
- `IProjectionPageLoader` backed by `TestProjectionPageLoader`.
- Concrete fake services for direct assertions.
- `TimeProvider` from `FrontComposerTestOptions.TimeProvider`.
- `TestFaultInjectionProvider`.

Store initialization is explicit by default (`StoreInitializationMode.OnDemand`). When `StoreInitializationMode.DuringHostSetup` is configured, composition setup initializes the Fluxor store during host setup; `FrontComposerTestBase.InitializeStoreAsync()` remains idempotent and uses `ConfigureAwait(false)`.

## Default Options

- `TestTenantId = "test-tenant"`.
- `TestUserId = "test-user"`.
- `BoundedContext = "Test"`.
- `CommandName = "Test Command"`.
- `Culture = CultureInfo.InvariantCulture`.
- `TimeProvider = TimeProvider.System`.
- `StoreInitialization = OnDemand`.
- `JSInteropMode = Loose`.
- `MaxEvidenceRecords = 100`.
- `MaxDiagnosticPayloadCharacters = 256`.

## Fake Service Behavior

`TestCommandService`:

- Never calls EventStore, SignalR, DAPR, network, or browser storage.
- Honors cancellation before creating evidence.
- Returns deterministic IDs scoped to the fake instance: `test-message-0001`, `test-correlation-0001`, and so on.
- Invokes lifecycle callbacks in this order: `Acknowledged -> Syncing -> Confirmed`.
- Captures command type, tenant, user, bounded context, command name, message ID, correlation ID, status, lifecycle states, timestamp, and redacted payload.
- Retains at most `MaxEvidenceRecords` evidence records.

`TestQueryService`:

- Never calls network or EventStore.
- Honors cancellation before creating evidence.
- Supports configured success results through `SucceedWith<T>()`.
- Supports configured not-modified cached results through `NotModifiedWith<T>()`.
- Returns an empty result for unconfigured query types.
- Captures request projection type, skip, take, tenant, user, mode (`configured`, `not-modified`, or `empty`), and timestamp.
- Retains at most `MaxEvidenceRecords` evidence records.

`TestProjectionPageLoader`:

- Never calls network or EventStore.
- Honors cancellation before creating evidence.
- Supports configured success pages through `SucceedWith(...)`.
- Supports configured not-modified pages through `NotModified(...)`.
- Returns an empty page for unconfigured projection types.
- Captures projection type, skip, take, tenant, user, mode (`configured`, `not-modified`, or `empty`), and timestamp.
- Retains at most `MaxEvidenceRecords` evidence records.

Fakes are per test host instance. Parallel bUnit contexts do not share user context or evidence. Deterministic IDs may repeat across isolated contexts.

## Fault Modes

`TestFaultInjectionProvider` is an evidence recorder, not a live SignalR simulator. It exposes deterministic methods for:

- `Drop(correlationId)`
- `Delay(correlationId)`
- `PartialDelivery(correlationId)`
- `Reorder(correlationId)`
- `ReconnectNudge(correlationId)`

Each fault record captures mode, tenant, user, correlation ID, and timestamp from the configured `TimeProvider`. Evidence is bounded by `MaxEvidenceRecords`.

## Evidence And Redaction

Evidence records are public Testing package contracts:

- `CommandDispatchEvidence`
- `ProjectionPageEvidence`
- `FaultInjectionEvidence`

`RedactedEvidenceFormatter.Format(...)` serializes bounded assertion payloads and redacts:

- configured tenant ID values,
- configured user ID values,
- token, secret, and password keyed values case-insensitively. Redaction is JSON-string-aware: a keyed string value is redacted through its closing quote, so values containing commas (for example `"password":"a,b,c"`) are fully redacted, not just up to the first comma. Non-string scalars are bounded by the next `,` or `}`.
- oversized payloads beyond `MaxDiagnosticPayloadCharacters`, with the `...<truncated>` marker.

Tests must not log raw command payloads, tenant IDs, user IDs, tokens, secrets, passwords, or external paths in failure messages.

## Public API Baseline Policy

`src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt` is authoritative. `PackageBoundaryTests.PublicApi_ExportedTypes_MatchIntentionalBaseline` fails if exported Testing package types or members drift.

Story 7.5 intentionally adds one public API method:

- `TestQueryService.NotModifiedWith<T>(IReadOnlyList<T>, string?)`

Reason: AC2 requires the query fake to expose a configured not-modified path, not only success and empty paths.

## Package Contents

The Testing package must include:

- `lib/net10.0/Hexalith.FrontComposer.Testing.dll`
- root `README.md`
- `build/Hexalith.FrontComposer.Testing.PublicAPI.Shipped.txt`

The package must not include repo/test/build artifacts such as `tests/`, `bin/`, `obj/`, screenshots, `.git`, internal test assemblies, `NSubstitute`, `Shouldly`, or `xunit.v3`.

The clean-consumer smoke project restores and builds from locally packed `Contracts`, `Shell`, and `Testing` `.nupkg` files without repo-relative `ProjectReference`s.

## Version Alignment Rule

`FrontComposerTestHostBuilder.ValidateVersionAlignment(...)` requires Testing, Contracts, Shell, and optional SourceTools assemblies to share one major/minor version before rendering.

## Verification Evidence

Passed:

- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` - passed with 0 warnings and 0 errors (re-verified after the Senior Developer Review redaction fix).
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests` - passed 23/23 through the xUnit v3 in-process runner. (Originally 22/22; the Senior Developer Review added `RedactedEvidenceFormatter_Format_RedactsSecretValuesContainingCommas`, which failed before the redaction fix and passes after it.)

Local blocker:

- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false --no-build -c Release` aborted before test execution in each assembly because VSTest cannot create its TCP listener in this sandbox: `System.Net.Sockets.SocketException (13): Permission denied`.

Focused VSTest note:

- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release -m:1 /nr:false` compiled successfully, then hit the same VSTest socket restriction. The xUnit v3 in-process executable is the local verification lane.
