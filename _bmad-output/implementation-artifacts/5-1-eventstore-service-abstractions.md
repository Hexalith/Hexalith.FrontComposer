# Story 5.1: EventStore Service Abstractions

Status: ready-for-dev

> **Epic 5** -- Reliable Real-Time Experience. **FR32** swappable EventStore contracts, **NFR74** zero direct infrastructure coupling, plus the first concrete bridge from Epic 2's stub command lifecycle to the pinned Hexalith.EventStore submodule. Applies lessons **L01**, **L03**, **L06**, **L07**, and **L10**.

---

## Executive Summary

Story 5-1 replaces the current stub-only communication seam with explicit, swappable EventStore service abstractions and default implementations. The developer must evolve the existing `Contracts/Communication` interfaces rather than create a second communication stack:

- `ICommandService` already exists and returns `CommandResult`; add a narrower EventStore-facing dispatcher seam only if it removes ambiguity. Do not break existing generated command-form callers.
- `IQueryService`, `QueryRequest`, and `QueryResult<T>` already exist with ETag fields; extend them append-only so Story 5-2 can handle 304/cache behavior without schema churn.
- `IProjectionSubscription` already exists but only joins/leaves groups; it must become the typed subscription surface that can deliver projection change nudges to Shell state/effects.
- `ServiceCollectionExtensions.AddHexalithFrontComposer()` currently registers `StubCommandService` via `TryAddScoped<ICommandService, StubCommandService>()`; Story 5-1 adds `AddHexalithEventStore()` so adopters can replace the stub without touching framework internals.

Endpoint ownership decision: `Hexalith.EventStore` owns the endpoint contract. FrontComposer consumes the pinned EventStore REST and SignalR surface through a configurable adapter; it does not define competing route conventions. Default paths must match the pinned EventStore contract: commands `POST /api/v1/commands`, queries `POST /api/v1/queries`, and projection changes hub `/hubs/projection-changes`. Add options for deployment/versioning overrides, but treat `/projections-hub` only as a configurable non-default alias if a consumer explicitly needs it.

---

## Story

As a developer,  
I want all event-store communication abstracted behind swappable service contracts,  
so that the framework is decoupled from infrastructure providers and I can swap implementations without changing application code.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | The framework's EventStore communication layer | The service contracts are inspected | Contracts expose command dispatch returning a correlation/message ID plus accepted-response metadata, query execution with ETag and explicit not-modified support, and projection subscription/unsubscription with change nudges. Contracts live in `Hexalith.FrontComposer.Contracts` and have no infrastructure dependencies. |
| AC2 | The default command implementation | A command is dispatched | It sends `POST /api/v1/commands`, uses camelCase JSON, includes a client-generated ULID message ID, expects `202 Accepted`, captures `Location`, `Retry-After`, and response `correlationId`, and maps the result to existing lifecycle-compatible `CommandResult`. |
| AC3 | The default query implementation | A query is executed | It sends `POST /api/v1/queries`, sends at most 10 `If-None-Match` values, expects `200 OK + ETag` for changed data, preserves the `304 Not Modified` path for Story 5-2, and uses camelCase JSON. |
| AC4 | The default subscription implementation | A projection is subscribed | It connects to the configured SignalR hub, defaulting to EventStore-owned `/hubs/projection-changes`, invokes `JoinGroup(projectionType, tenantId)`, invokes `LeaveGroup(...)` when unsubscribing, and raises change nudges from `ProjectionChanged(projectionType, tenantId)` without carrying projection payloads. |
| AC5 | A consumer project references the framework | EventStore services are registered | `AddHexalithEventStore(...)` registers default clients via DI using replaceable interfaces and EventStore-owned default paths. Consumers can override any contract implementation after registration and can override command, query, or projection hub paths through options. |
| AC6 | Infrastructure is accessed | The implementation is inspected | FrontComposer does not add custom wrappers over DAPR state, pubsub, actors, or secrets. EventStore internals remain inside the EventStore submodule/service; FrontComposer talks through REST, SignalR, and contracts only. |
| AC7 | Wire constraints are inspected | Requests and group names are built | `TenantId`, required user identity, `ProjectionType`, domain, aggregate/group parts, and other EventStore routing values fail validation before send when missing or colon-containing; serialized UTF-8 HTTP request bodies over 1 MB are rejected before `HttpClient.SendAsync`; command IDs are ULID strings; JSON is camelCase. |
| AC8 | Tests run | Contract and Shell/EventStore client tests execute | Tests prove DI replacement, EventStore-owned default endpoint paths, configured path overrides, JSON shape, ETag headers, SignalR group join/leave, path configurability, and no dependency from Contracts to infrastructure packages. |

---

## Tasks / Subtasks

- [ ] T1. Contract alignment and append-only model changes (AC1, AC7)
  - [ ] Read existing `Contracts/Communication/*` before editing; preserve source compatibility for existing `ICommandService`, `ICommandServiceWithLifecycle`, `IQueryService`, and `IProjectionSubscription` callers.
  - [ ] Inspect the pinned EventStore request/response DTOs or docs before coding `SubmitCommandRequest`, `SubmitQueryRequest`, `ProjectionChanged`, `JoinGroup`, and `LeaveGroup`; treat those names and shapes as authoritative for the adapter.
  - [ ] Add or rename only with compatibility shims. Do not add `ICommandDispatcher` / `IQueryExecutor` unless existing `ICommandService` / `IQueryService` cannot express the EventStore contract append-only; if new names are unavoidable, introduce them as thin contracts and adapt existing services without duplicating behavior.
  - [ ] Add immutable DTOs for EventStore command/query request metadata only where existing `QueryRequest` cannot express required fields (`Domain`, `AggregateId`, `QueryType`, `ProjectionActorType`, `UserId` source, ETag list).
  - [ ] Enrich command acceptance metadata append-only: preserve existing `CommandResult(MessageId, Status)` callers while carrying server `CorrelationId`, `Location`, and `RetryAfter` for later lifecycle/error stories.
  - [ ] Add an explicit `QueryResult<T>` not-modified signal, preferably append-only `IsNotModified`/`NotModified`, rather than using exceptions for the expected `304 Not Modified` cache-validation path.
  - [ ] Add validation helpers for colon rejection and 1 MB serialized UTF-8 HTTP body guard in Contracts or Shell-owned client code.
  - [ ] Inspect existing tenant/user context services. If no suitable seam exists, introduce a minimal Shell-owned provider abstraction for required EventStore tenant/user values; never default to `anonymous`, `default`, or empty strings.

- [ ] T2. EventStore options and DI registration (AC5, AC6)
  - [ ] Add `EventStoreOptions` under `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/` or `.../Options/` with `BaseAddress`, `CommandEndpointPath` default `/api/v1/commands`, `QueryEndpointPath` default `/api/v1/queries`, `ProjectionChangesHubPath` default `/hubs/projection-changes`, `AccessTokenProvider`/token hook abstraction, timeout, and retry settings.
  - [ ] Validate options consistently during registration or first use: `BaseAddress` required, endpoint and hub paths start with `/`, timeout positive, max ETag count positive and no greater than 10, and max request bytes positive and defaulting to 1 MB.
  - [ ] Add `AddHexalithEventStore(this IServiceCollection, Action<EventStoreOptions>?)` in a new `EventStoreServiceExtensions.cs`.
  - [ ] Register REST clients through typed or named `HttpClient` using configured `BaseAddress`, timeout, and token hook; do not hand-new `HttpClient` inside EventStore clients.
  - [ ] Register defaults as `Scoped` for Blazor circuit safety unless a client is demonstrably stateless. Do not register Singleton services that capture tenant/user/token state.
  - [ ] Use `TryAdd*` for default implementations so consumers can replace services. Add tests proving replacement wins.

- [ ] T3. Command client implementation (AC2, AC7)
  - [ ] Implement `EventStoreCommandClient` as the default command dispatcher/service.
  - [ ] Generate ULID via existing `IUlidFactory` (`src/Hexalith.FrontComposer.Shell/Services/Lifecycle/UlidFactory.cs`); do not introduce another ID library.
  - [ ] Serialize request shape matching pinned EventStore `SubmitCommandRequest`: `messageId`, `tenant`, `domain`, `aggregateId`, `commandType`, `payload`, optional `correlationId`, optional `extensions`.
  - [ ] Read `202 Accepted` response `correlationId`, `Location`, and `Retry-After`; return `CommandResult(MessageId, "Accepted")` or an append-only enriched result without breaking existing code.
  - [ ] Flow `CancellationToken` through dispatch, tenant/user lookup, access-token acquisition, serialization guard, and outbound HTTP send.
  - [ ] If `AccessTokenProvider` throws or returns an empty token when a token is required, fail before send and prove no outbound HTTP call occurs.
  - [ ] Map non-202 responses minimally and leave full response matrix to Story 5-2. Do not implement 400/403/409/429 UX here.

- [ ] T4. Query client implementation (AC3, AC7)
  - [ ] Implement `EventStoreQueryClient` as the default query executor/service.
  - [ ] Serialize request shape matching pinned EventStore `SubmitQueryRequest`: `tenant`, `domain`, `aggregateId`, `queryType`, optional `projectionType`, optional `payload`, optional `entityId`, optional `projectionActorType`.
  - [ ] Send `If-None-Match` using at most 10 validators. If more are provided, reject before send with a documented exception; do not trim silently.
  - [ ] On `200 OK`, deserialize `payload` to `QueryResult<T>.Items` according to the existing FrontComposer projection contract; capture response `ETag`.
  - [ ] On `304 Not Modified`, return the explicit append-only no-change result from T1. Do not throw for expected cache validation and do not silently return an empty list.
  - [ ] Flow `CancellationToken` through query execution, tenant/user lookup, access-token acquisition, serialization guard, and outbound HTTP send.
  - [ ] If `AccessTokenProvider` throws or returns an empty token when a token is required, fail before send and prove no outbound HTTP call occurs.

- [ ] T5. Projection subscription implementation (AC4, AC7)
  - [ ] Implement `ProjectionSubscriptionService` behind `IProjectionSubscription` and/or a new callback-aware companion contract.
  - [ ] Use `Microsoft.AspNetCore.SignalR.Client` or the pinned submodule's `Hexalith.EventStore.SignalR.EventStoreSignalRClient` pattern. If referencing the submodule package/project would create an undesirable dependency, copy the minimal behavior behind FrontComposer contracts instead.
  - [ ] Default hub path to EventStore-owned `/hubs/projection-changes`; do not introduce `/projections-hub` as a competing convention. If compatibility is required, cover it only as an explicit configured override.
  - [ ] Implement `IAsyncDisposable`; stop/dispose the hub connection when the circuit-scoped service is disposed.
  - [ ] Use idempotent set semantics for duplicate subscribe/unsubscribe calls in Story 5-1; reference-counted leases are deferred unless a later story proves separate subscriber ownership is required.
  - [ ] Flow `CancellationToken` through SignalR start, join, and leave calls where the underlying APIs support it.
  - [ ] Track subscribed groups per circuit and rejoin on reconnect only as far as the underlying client provides automatically. User-facing reconnect UX is Story 5-3/5-4.
  - [ ] Handle concurrent `SubscribeAsync`/`UnsubscribeAsync`, dispose during active subscribe, and `ProjectionChanged` arriving after dispose without unhandled exceptions or callbacks after disposal.
  - [ ] Raise `IProjectionChangeNotifier.NotifyChanged(projectionType)` and include tenant in a companion event/result where needed for tenant isolation.

- [ ] T6. Tests and verification (AC1-AC8)
  - [ ] Contracts tests: no infrastructure package references; colon validation; ETag max-10 rejection; append-only compatibility of existing records.
  - [ ] Shell tests: `AddHexalithEventStore()` registers command/query/subscription defaults, consumer replacements win, and invalid options fail consistently.
  - [ ] HTTP tests: command uses EventStore default `POST /api/v1/commands`, query uses EventStore default `POST /api/v1/queries`, configured path overrides are honored, camelCase body, exact 1 MB body accepted, 1 MB + 1 byte rejected before send, auth/token hook success and failure behavior, and cancellation token propagation.
  - [ ] SignalR tests: EventStore default hub path `/hubs/projection-changes`, configured hub path overrides, group key validation, join/leave method names, duplicate subscribe/unsubscribe idempotency, concurrent subscribe/unsubscribe behavior, dispose-during-subscribe cleanup, no callback after disposal, and `ProjectionChanged` nudge routing.
  - [ ] Smoke test: `SeamExtractionSmokeTests` resolves EventStore services from a DI provider without starting a live EventStore.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Communication/ICommandService.cs` | Generic `DispatchAsync<TCommand>` returns `CommandResult`; comments say EventStore REST API. | Keep existing callers compiling. Existing command forms and lifecycle wrappers depend on this seam. |
| `src/Hexalith.FrontComposer.Contracts/Communication/ICommandServiceWithLifecycle.cs` | Companion lifecycle-aware command service used by `CommandServiceExtensions`. | Do not collapse or bypass lifecycle callback support from Epic 2. |
| `src/Hexalith.FrontComposer.Contracts/Communication/IQueryService.cs` | Generic `QueryAsync<T>(QueryRequest)` returns `QueryResult<T>`. | Extend request/result append-only; do not force projection views to know EventStore HTTP details. |
| `src/Hexalith.FrontComposer.Contracts/Communication/IProjectionSubscription.cs` | `SubscribeAsync(projectionType, tenantId)` / `UnsubscribeAsync(...)` only. | Add nudge delivery with a companion notifier/adaptor rather than breaking the existing method pair. |
| `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` | Registers `StubCommandService` as default via `TryAddScoped<ICommandService, StubCommandService>()`. | `AddHexalithFrontComposer()` must still boot without EventStore. EventStore registration is opt-in and replaces the stub. |
| `src/Hexalith.FrontComposer.Contracts/Badges/IActionQueueCountReader.cs` | Comments state Story 5-1 will register real EventStore-backed reader later. | Real count reader may be registered here only if the EventStore query client is sufficient; do not implement ETag cache yet. |

### Cross-Story Contract Table

| Seam | Producer | Consumer | Story 5-1 decision |
| --- | --- | --- | --- |
| Command lifecycle | Stories 2-1 through 2-5 | EventStore command client | Return accepted correlation/message IDs in a shape compatible with existing lifecycle callbacks. Full response matrix remains Story 5-2. |
| ETag query support | Story 4-4 `IProjectionPageLoader` and `QueryRequest.ETag` | Story 5-2 cache and Story 5-4 reconciliation | 5-1 sends/receives ETags but does not persist cache entries. |
| SignalR nudges | Story 5-1 subscription service | Stories 5-3 and 5-4 connection UX/reconciliation | 5-1 delivers signal-only `ProjectionChanged` events; no user-facing disconnected banner or batch animation. |
| Tenant isolation | Story 2-2 L03 lesson and Epic 7 future auth | All EventStore clients | Fail closed on empty tenant/user where the service needs them. Never default to `anonymous`, `default`, or empty segments. |
| EventStore submodule | Pinned submodule at `Hexalith.EventStore` | Default clients | Treat current API docs/code as authoritative for endpoint paths, request/response body shape, and SignalR hub behavior. Configured overrides are adapter settings, not alternate defaults. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | Keep `Contracts` infrastructure-free. | AC1 and NFR74 require swappable contracts and no provider coupling. | Add SignalR/Dapr packages to Contracts; place EventStore DTOs directly in generated Razor. |
| D2 | EventStore registration is opt-in via `AddHexalithEventStore()`. | `AddHexalithFrontComposer()` must keep the current stub boot path for samples/tests. | Replace the stub globally; require live EventStore for all Shell consumers. |
| D3 | EventStore owns the default endpoint contract: `/api/v1/commands`, `/api/v1/queries`, and `/hubs/projection-changes`; each path remains configurable. | Aligns FrontComposer with the pinned EventStore surface while preserving deployment/versioning escape hatches. | Treat `/projections-hub` as a competing default; hard-code EventStore paths with no configuration. |
| D4 | Command IDs use existing `IUlidFactory`. | Avoids duplicate ID policy and matches Epic 2 idempotency groundwork. | Use `Guid.NewGuid()`; add a second ULID package. |
| D5 | SignalR messages remain nudge-only. | EventStore docs explicitly send projection type + tenant only; data correctness comes from REST re-query with ETag. | Push full projection payloads over SignalR; mutate UI state directly from SignalR payloads. |
| D6 | Story 5-1 handles transport seams, not response UX. | Story 5-2 owns HTTP response matrix and cache behavior; Story 5-3/5-4 own disconnect/reconnect UX. | Implement the whole Epic 5 degraded-network experience in 5-1. |
| D7 | Query `304 Not Modified` is represented as append-only result state, not an exception. | Cache validation is an expected query path and Story 5-2 should consume it without exception-driven control flow. | Throw a special exception; return an empty successful result. |
| D8 | REST clients use DI-managed typed or named `HttpClient`. | Keeps base address, timeout, auth token behavior, and test fakes centralized and replaceable. | Instantiate `HttpClient` inside clients; hide auth behavior in ad hoc wrappers. |
| D9 | Projection subscriptions have deterministic lifetime and duplicate-call behavior. | Circuit-scoped SignalR resources must clean up predictably and tolerate repeated subscribe/unsubscribe calls from component lifecycle edges. | Let duplicate calls race the hub; rely on finalizers or process shutdown for cleanup. |
| D10 | Duplicate projection subscriptions use idempotent set semantics in 5-1. | One active group membership per projection/tenant per circuit is enough for current lifecycle needs and simpler to reason about under component re-render edges. | Reference counting leases before a concrete multi-owner requirement exists. |
| D11 | ETag overflow rejects before send instead of trimming. | Silent trimming changes caller cache intent and can create misleading no-change results. | Trim to the first or last 10 validators. |
| D12 | Request-size enforcement measures the serialized UTF-8 HTTP body. | This matches the wire payload the EventStore limit protects and gives deterministic 1 MB boundary tests. | Estimate object graph size; rely on server rejection. |
| D13 | Auth token failure fails locally before outbound transport. | Empty or failed token acquisition should not degrade into anonymous EventStore calls. | Send without a token and let EventStore decide. |

### Library / Framework Requirements

- Target existing project TFMs: Contracts `net10.0;netstandard2.0`, Shell `net10.0`.
- Current repo pins `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.2-26098.1`, `Fluxor.Blazor.Web` `6.9.0`, `NUlid` `1.7.3`, xUnit v3, bUnit, Shouldly, NSubstitute.
- Add `Microsoft.AspNetCore.SignalR.Client` only if needed by Shell. Latest stable observed on NuGet is `10.0.6`; if added, pin centrally in `Directory.Packages.props` and use the same major line as `net10.0`.
- `HubConnectionBuilder.WithAutomaticReconnect()` does not retry initial start failures and defaults to reconnect delays of 0, 2, 10, and 30 seconds. Initial-start retry, permanent disconnect UX, and reconnect banner are later stories.
- Use `System.Text.Json` web defaults (`JsonSerializerOptions.Web` or `JsonSerializerDefaults.Web`) for camelCase JSON. Do not hand-roll casing.
- Dapr remains inside EventStore infrastructure for this story. Dapr docs currently support .NET 8, 9, and 10, but FrontComposer 5-1 should not add direct Dapr access unless a later story explicitly needs it.

External references checked on 2026-04-25:

- Microsoft Learn: ASP.NET Core SignalR .NET client automatic reconnect behavior -- https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client
- NuGet: `Microsoft.AspNetCore.SignalR.Client` latest stable observed as `10.0.6` -- https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client
- Microsoft Learn: `System.Text.Json` web defaults and `JsonSerializerOptions.Web` -- https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/configure-options
- Dapr docs: .NET SDK supports .NET 8, .NET 9, and .NET 10 -- https://docs.dapr.io/developing-applications/sdks/dotnet/

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Communication/*` | Append-only contract/DTO updates for dispatcher/query/subscription seams. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreOptions.cs` | EventStore base URL, command/query endpoint paths, projection hub path/URL, timeout, ETag header count, request-size limit. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs` | Default `POST /api/v1/commands` client. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs` | Default `POST /api/v1/queries` client with ETag header handling. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` | Default SignalR group subscription/nudge service. |
| `src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs` | `AddHexalithEventStore()` opt-in DI registration. |
| `tests/Hexalith.FrontComposer.Contracts.Tests/Communication/*` | Contract validation and compatibility tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/*` | HTTP/SignalR client and DI tests. |

No new UI components, CSS, source generator outputs, Razor emitters, or localized resource strings are expected in Story 5-1.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute for unit tests.
- Use fake `HttpMessageHandler` or equivalent deterministic HTTP handler for REST clients; no live EventStore required.
- Do not add Playwright or browser tests for 5-1.
- Do not run submodule test suites from FrontComposer tests; submodule behavior is consumed as a pinned contract reference.
- Add at least one dependency/assembly test that proves `Hexalith.FrontComposer.Contracts` does not reference assemblies whose names contain `Dapr`, `SignalR`, `AspNetCore`, `Hosting`, or `EventStore` implementation packages.

### Scope Guardrails

Do not implement these in Story 5-1:

- HTTP response matrix UX and error message mapping -- Story 5-2.
- ETag cache persistence, LRU eviction, or `IStorageService` integration -- Story 5-2.
- SignalR disconnection banner, lifecycle timeout escalation, or form-state preservation -- Story 5-3.
- Rejoin sweep animation, schema evolution mismatch UX, or reconciliation summary -- Story 5-4.
- Idempotent terminal outcome resolution, optimistic badge desaturation, polling fallback UX -- Story 5-5.
- Build-time infrastructure enforcement analyzer and OpenTelemetry activity source -- Story 5-6.
- Fault injection harness -- Story 5-7.
- Dapr component YAML, actor calls, pub/sub topics, state-store access, or secrets access inside FrontComposer.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Planning docs still mention `/projections-hub`; EventStore owns the implemented default `/hubs/projection-changes`. Product docs should mark `/projections-hub` as superseded or a configured non-default alias. | Story 9-5 documentation site or a planning-correction task before 5-3 |
| Full response handling for 400/401/403/404/409/429/503. | Story 5-2 |
| ETag cache storage and 304 no-change consumption by visible projections. | Story 5-2 |
| SignalR permanent-disconnect restart policy and user messaging. | Story 5-3 |
| Provider compatibility/Pact verification against EventStore OpenAPI. | Story 10-3 |

---

## References

- [Source: _bmad-output/planning-artifacts/epics/epic-5-reliable-real-time-experience.md#Story-5.1] -- Story statement, ACs, FR32/NFR74 references.
- [Source: _bmad-output/planning-artifacts/architecture.md#EventStore-communication-contract] -- REST commands/queries plus SignalR nudge architecture.
- [Source: _bmad-output/planning-artifacts/architecture.md#API-Boundaries-EventStore] -- `POST /api/v1/commands`, `POST /api/v1/queries`, projection nudges.
- [Source: _bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md#NuGet-package-strategy] -- EventStore integration package and public service API expectations.
- [Source: _bmad-output/planning-artifacts/prd/non-functional-requirements.md#Data-Privacy] -- Framework should not store business data outside ETag-validated cache.
- [Source: _bmad-output/planning-artifacts/ux-design-specification/core-user-experience.md#Production-deployment] -- Blazor Auto implications and infrastructure invisibility.
- [Source: Hexalith.EventStore/docs/reference/command-api.md#POST-api-v1-commands] -- Current command request/response shape, 1 MB limit, camelCase JSON, JWT, correlation ID.
- [Source: Hexalith.EventStore/docs/reference/query-api.md#POST-api-v1-queries] -- Current query request/response shape and `If-None-Match` / ETag behavior.
- [Source: Hexalith.EventStore/docs/reference/query-api.md#SignalR-Hub-hubs-projection-changes] -- Current SignalR hub path, methods, group format, and nudge-only behavior.
- [Source: Hexalith.EventStore/src/Hexalith.EventStore/SignalRHub/ProjectionChangedHub.cs] -- `HubPath = "/hubs/projection-changes"`, `JoinGroup`, `LeaveGroup`, colon guard.
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs] -- Existing `StubCommandService` registration and scoped DI patterns.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L03] -- Tenant/user isolation fail-closed.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

(to be filled in by dev agent)

### File List

(to be filled in by dev agent)
