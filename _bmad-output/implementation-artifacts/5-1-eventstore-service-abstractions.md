# Story 5.1: EventStore Service Abstractions

Status: done

> **Epic 5** -- Reliable Real-Time Experience. **FR32** swappable EventStore contracts, **NFR74** zero direct infrastructure coupling, plus the first concrete bridge from Epic 2's stub command lifecycle to the pinned Hexalith.EventStore submodule. Applies lessons **L01**, **L03**, **L06**, **L07**, and **L10**.

---

## Executive Summary

Story 5-1 replaces the current stub-only communication seam with explicit, swappable EventStore service abstractions and default implementations. The developer must evolve the existing `Contracts/Communication` interfaces rather than create a second communication stack:

- `ICommandService` already exists and returns `CommandResult`; add a narrower EventStore-facing dispatcher seam only if it removes ambiguity. Do not break existing generated command-form callers.
- `IQueryService`, `QueryRequest`, and `QueryResult<T>` already exist with ETag fields; extend them append-only so Story 5-2 can handle 304/cache behavior without schema churn.
- `IProjectionSubscription` already exists but only joins/leaves groups; it must become the typed subscription surface that can deliver projection change nudges to Shell state/effects.
- `ServiceCollectionExtensions.AddHexalithFrontComposer()` currently registers `StubCommandService` via `TryAddScoped<ICommandService, StubCommandService>()`; Story 5-1 adds `AddHexalithEventStore()` so adopters can replace the stub without touching framework internals.

Endpoint ownership decision: `Hexalith.EventStore` owns the endpoint contract. FrontComposer consumes the pinned EventStore REST and SignalR surface through a configurable adapter; it does not define competing route conventions. Default paths must match the pinned EventStore contract: commands `POST /api/v1/commands`, queries `POST /api/v1/queries`, and projection changes hub `/hubs/projection-changes`. Add options for deployment/versioning overrides, but treat `/projections-hub` only as a configurable non-default alias if a consumer explicitly needs it.

Adopter outcome and done boundary: after Story 5-1, a feature module can opt into EventStore-backed FrontComposer communication by calling `AddHexalithEventStore(...)`, then continue depending on FrontComposer command/query/subscription contracts without referencing Dapr, EventStore implementation packages, SignalR client APIs from Contracts, or persistence-specific APIs. The story ships the contract extensions, default REST/SignalR adapter clients, DI registration, and deterministic test seams; it does not ship projection framework changes, cache persistence, retry UX, EventStore schema policy, or provider/Pact certification.

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
| AC4 | The default subscription implementation | A projection is subscribed | It connects to the configured SignalR hub, defaulting to EventStore-owned `/hubs/projection-changes`, invokes `JoinGroup(projectionType, tenantId)`, records a subscription as active only after the join succeeds, invokes `LeaveGroup(...)` when unsubscribing an active group, and raises change nudges from `ProjectionChanged(projectionType, tenantId)` without carrying projection payloads or invoking callbacks after disposal. |
| AC5 | A consumer project references the framework | EventStore services are registered | `AddHexalithEventStore(...)` registers default clients via DI using replaceable interfaces and EventStore-owned default paths. Consumers can override any contract implementation after registration and can override command, query, or projection hub paths through options. |
| AC6 | Infrastructure is accessed | The implementation is inspected | FrontComposer does not add custom wrappers over DAPR state, pubsub, actors, or secrets. EventStore internals remain inside the EventStore submodule/service; FrontComposer talks through REST, SignalR, and contracts only. |
| AC7 | Wire constraints are inspected | Requests and group names are built | `TenantId`, required user identity, `ProjectionType`, domain, aggregate/group parts, and other EventStore routing values fail validation before send when missing or colon-containing; serialized UTF-8 HTTP request bodies over 1 MB are rejected before `HttpClient.SendAsync`; command IDs are ULID strings; JSON is camelCase; diagnostics redact bearer tokens, raw payload bodies, and PII-bearing routing values. |
| AC8 | Tests run | Contract and Shell/EventStore client tests execute | Tests prove DI replacement, EventStore-owned default endpoint paths, configured path overrides, JSON shape, ETag headers, SignalR group join/leave, cancellation propagation, subscription concurrency/disposal behavior, path configurability, and no dependency from Contracts to infrastructure packages. |

---

## Tasks / Subtasks

- [x] T1. Contract alignment and append-only model changes (AC1, AC7)
  - [x] Read existing `Contracts/Communication/*` before editing; preserve source compatibility for existing `ICommandService`, `ICommandServiceWithLifecycle`, `IQueryService`, and `IProjectionSubscription` callers.
  - [x] Inspect the pinned EventStore request/response DTOs or docs before coding `SubmitCommandRequest`, `SubmitQueryRequest`, `ProjectionChanged`, `JoinGroup`, and `LeaveGroup`; treat those names and shapes as authoritative for the adapter.
  - [x] Add or rename only with compatibility shims. Do not add `ICommandDispatcher` / `IQueryExecutor` unless existing `ICommandService` / `IQueryService` cannot express the EventStore contract append-only; if new names are unavoidable, introduce them as thin contracts and adapt existing services without duplicating behavior.
  - [x] Add immutable DTOs for EventStore command/query request metadata only where existing `QueryRequest` cannot express required fields (`Domain`, `AggregateId`, `QueryType`, `ProjectionActorType`, `UserId` source, ETag list).
  - [x] Enrich command acceptance metadata append-only: preserve existing `CommandResult(MessageId, Status)` callers while carrying server `CorrelationId`, `Location`, and `RetryAfter` for later lifecycle/error stories.
  - [x] Add an explicit `QueryResult<T>` not-modified signal, preferably append-only `IsNotModified`/`NotModified`, rather than using exceptions for the expected `304 Not Modified` cache-validation path.
  - [x] Add validation helpers for colon rejection and 1 MB serialized UTF-8 HTTP body guard in Contracts or Shell-owned client code.
  - [x] Inspect existing tenant/user context services. If no suitable seam exists, introduce a minimal Shell-owned provider abstraction for required EventStore tenant/user values; never default to `anonymous`, `default`, or empty strings.

- [x] T2. EventStore options and DI registration (AC5, AC6)
  - [x] Add `EventStoreOptions` under `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/` or `.../Options/` with `BaseAddress`, `CommandEndpointPath` default `/api/v1/commands`, `QueryEndpointPath` default `/api/v1/queries`, `ProjectionChangesHubPath` default `/hubs/projection-changes`, `AccessTokenProvider`/token hook abstraction, timeout, and retry settings.
  - [x] Validate options consistently during registration or first use: `BaseAddress` required, endpoint and hub paths start with `/`, timeout positive, max ETag count positive and no greater than 10, and max request bytes positive and defaulting to 1 MB.
  - [x] Add `AddHexalithEventStore(this IServiceCollection, Action<EventStoreOptions>?)` in a new `EventStoreServiceExtensions.cs`.
  - [x] Register REST clients through typed or named `HttpClient` using configured `BaseAddress`, timeout, and token hook; do not hand-new `HttpClient` inside EventStore clients.
  - [x] Register defaults as `Scoped` for Blazor circuit safety unless a client is demonstrably stateless. Do not register Singleton services that capture tenant/user/token state.
  - [x] Resolve tenant/user/token values per operation, not at DI registration or service construction time, so circuit/user changes cannot leak stale identity into later sends.
  - [x] Keep all transport diagnostics structured and redacted: never log bearer tokens, serialized command/query payloads, raw tenant/user IDs, or unbounded ProblemDetails bodies from the remote service.
  - [x] Use `TryAdd*` for default implementations so consumers can replace services. Add tests proving replacement wins.

- [x] T3. Command client implementation (AC2, AC7)
  - [x] Implement `EventStoreCommandClient` as the default command dispatcher/service.
  - [x] Generate ULID via existing `IUlidFactory` (`src/Hexalith.FrontComposer.Shell/Services/Lifecycle/UlidFactory.cs`); do not introduce another ID library.
  - [x] Serialize request shape matching pinned EventStore `SubmitCommandRequest`: `messageId`, `tenant`, `domain`, `aggregateId`, `commandType`, `payload`, optional `correlationId`, optional `extensions`.
  - [x] Read `202 Accepted` response `correlationId`, `Location`, and `Retry-After`; return `CommandResult(MessageId, "Accepted")` or an append-only enriched result without breaking existing code.
  - [x] Flow `CancellationToken` through dispatch, tenant/user lookup, access-token acquisition, serialization guard, and outbound HTTP send.
  - [x] If `AccessTokenProvider` throws or returns an empty token when a token is required, fail before send and prove no outbound HTTP call occurs.
  - [x] Map non-202 responses minimally and leave full response matrix to Story 5-2. Do not implement 400/403/409/429 UX here.

- [x] T4. Query client implementation (AC3, AC7)
  - [x] Implement `EventStoreQueryClient` as the default query executor/service.
  - [x] Serialize request shape matching pinned EventStore `SubmitQueryRequest`: `tenant`, `domain`, `aggregateId`, `queryType`, optional `projectionType`, optional `payload`, optional `entityId`, optional `projectionActorType`.
  - [x] Send `If-None-Match` using at most 10 validators. If more are provided, reject before send with a documented exception; do not trim silently.
  - [x] On `200 OK`, deserialize `payload` to `QueryResult<T>.Items` according to the existing FrontComposer projection contract; capture response `ETag`.
  - [x] On `304 Not Modified`, return the explicit append-only no-change result from T1. Do not throw for expected cache validation and do not silently return an empty list.
  - [x] Flow `CancellationToken` through query execution, tenant/user lookup, access-token acquisition, serialization guard, and outbound HTTP send.
  - [x] If `AccessTokenProvider` throws or returns an empty token when a token is required, fail before send and prove no outbound HTTP call occurs.

- [x] T5. Projection subscription implementation (AC4, AC7)
  - [x] Implement `ProjectionSubscriptionService` behind `IProjectionSubscription` and/or a new callback-aware companion contract.
  - [x] Use `Microsoft.AspNetCore.SignalR.Client` or the pinned submodule's `Hexalith.EventStore.SignalR.EventStoreSignalRClient` pattern. If referencing the submodule package/project would create an undesirable dependency, copy the minimal behavior behind FrontComposer contracts instead.
  - [x] Default hub path to EventStore-owned `/hubs/projection-changes`; do not introduce `/projections-hub` as a competing convention. If compatibility is required, cover it only as an explicit configured override.
  - [x] Implement `IAsyncDisposable`; stop/dispose the hub connection when the circuit-scoped service is disposed.
  - [x] Use idempotent set semantics for duplicate subscribe/unsubscribe calls in Story 5-1; reference-counted leases are deferred unless a later story proves separate subscriber ownership is required.
  - [x] Flow `CancellationToken` through SignalR start, join, and leave calls where the underlying APIs support it.
  - [x] Do not add a group to the active subscription set until `JoinGroup` succeeds. If start/join fails or cancellation fires, leave the service in a no-active-subscription state for that group and make a later `UnsubscribeAsync` a no-op except for safe cleanup.
  - [x] Track subscribed groups per circuit and rejoin on reconnect only as far as the underlying client provides automatically. User-facing reconnect UX is Story 5-3/5-4.
  - [x] Handle concurrent `SubscribeAsync`/`UnsubscribeAsync`, duplicate subscribe while a join is in flight, dispose during active subscribe, and `ProjectionChanged` arriving after dispose without unhandled exceptions, leaked active-group entries, or callbacks after disposal.
  - [x] Raise `IProjectionChangeNotifier.NotifyChanged(projectionType)` and include tenant in a companion event/result where needed for tenant isolation.

- [x] T6. Tests and verification (AC1-AC8)
  - [x] Contracts tests: no infrastructure package references; colon validation; ETag max-10 rejection; append-only compatibility of existing records.
  - [x] Shell tests: `AddHexalithEventStore()` registers command/query/subscription defaults, consumer replacements win, and invalid options fail consistently.
  - [x] HTTP tests: command uses EventStore default `POST /api/v1/commands`, query uses EventStore default `POST /api/v1/queries`, configured path overrides are honored, camelCase body, exact 1 MB body accepted, 1 MB + 1 byte rejected before send, auth/token hook success and failure behavior, and cancellation token propagation.
  - [x] SignalR tests: EventStore default hub path `/hubs/projection-changes`, configured hub path overrides, group key validation, join/leave method names, duplicate subscribe/unsubscribe idempotency, concurrent subscribe/unsubscribe behavior, dispose-during-subscribe cleanup, no callback after disposal, and `ProjectionChanged` nudge routing.
  - [x] Smoke test: `SeamExtractionSmokeTests` resolves EventStore services from a DI provider without starting a live EventStore.
  - [x] Test-first sequence: land failing contract/compatibility tests first, then options/DI tests, then HTTP adapter tests, then SignalR lifetime/concurrency tests, then the smoke test.

### Review Findings

_From `bmad-code-review` Pass-1 on 2026-04-25 — three-layer adversarial pass (Blind Hunter + Edge Case Hunter + Acceptance Auditor) against uncommitted 5-1 working-tree (1,595-line diff). 17 dismissed as noise/out-of-scope/already-correct. 4 decision-needed, 8 patches, 5 deferred. **All decision-needed resolved under "do best" autonomous delegation; all 8 patches applied.** Build clean `dotnet build -warnaserror`; full suite Shell 1006/0/3 + Contracts 83/0/0 + Bench 2/0/0 (3 pre-existing E2E skips unchanged). Net +16 Shell tests across the resolution patches._

- [x] [Review][Decision] DN1 = a — **Authenticated tenant ALWAYS wins; mismatched override is rejected.** `EventStoreIdentity.RequireUserContext` now fails closed when `IUserContextAccessor.TenantId` is null/whitespace AND throws `InvalidOperationException("Requested tenant does not match the authenticated tenant context.")` when a command/query body supplies a non-matching `TenantId`. Same fix protects both command path and query path because both call the same helper. Reflection lookup of `command.TenantId` retained as integrity verification, not authority. New tests: `CommandClient_RejectsTenantMismatch_BeforeSend`, `CommandClient_FailsClosed_WhenAuthenticatedTenantIsMissing`.
- [x] [Review][Decision] DN2 = c — **Tenant passes raw end-to-end; only the attribute-derived domain is normalized.** Removed `NormalizeRouteSegment(tenant)` from `EventStoreIdentity.RequireUserContext`. Domain normalization preserved for command path (`[BoundedContext("OrdersSales")]` → `orders-sales`) and for query path (`request.Domain`). Subscribe/unsubscribe was already raw-pass-through, so the asymmetry is resolved. New test: `CommandClient_PreservesAuthenticatedTenantCasing_VerbatimOnTheWire` proves a tenant of `Acme_Corp` round-trips exactly.
- [x] [Review][Decision] DN3 = a — **Append-only `IProjectionChangeNotifierWithTenant : IProjectionChangeNotifier` companion interface added in Contracts.** Adds `NotifyChanged(string projectionType, string tenantId)` + `event Action<string, string>? ProjectionChangedForTenant`. `ProjectionChangeNotifier` implements both surfaces; DI registers the same instance under both interfaces. `ProjectionSubscriptionService.OnProjectionChangedAsync` pattern-matches and prefers the tenant-aware path when available. Existing `BadgeCountService.ProjectionChanged += handler` usage continues to compile and work unchanged. New test: `OnNudge_RaisesTenantAwareEvent_WhenNotifierImplementsCompanionInterface`.
- [x] [Review][Decision] DN4 = a — **`EventStoreOptionsValidator.IsPath` tightened.** Rejects empty/whitespace, missing leading `/`, leading `//` (path-network-form), embedded whitespace/control chars, `?`, `#`, and fails closed via `Uri.TryCreate(value, UriKind.Relative, out _)`.

- [x] [Review][Patch] P1 — Added `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/SeamExtractionSmokeTests.cs`. Resolves every EventStore-registered service through a built `ServiceProvider` and asserts the two notifier interfaces map to the same concrete instance.
- [x] [Review][Patch] P2 — Added `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStoreDiagnosticsTests.cs`. Negative diagnostics suite captures logger output via `CapturingLogger<T>` and asserts that bearer token, payload value, and PII user-id strings never appear in any log entry across non-202 command responses, token-acquisition failures, non-200 query responses, and unparseable command response bodies (P8 path).
- [x] [Review][Patch] P3 — `ProjectionSubscriptionService.UnsubscribeAsync` reordered: `LeaveGroupAsync` runs first; `_activeGroups.TryRemove` happens only on success. If the leave throws, client state matches server state so a retry can finish the cleanup. New test: `Unsubscribe_KeepsActiveGroup_WhenLeaveGroupThrows`.
- [x] [Review][Patch] P4 — `EventStoreQueryClient.ContainsHeaderInjectionChar` guard scans each ETag for `char.IsControl` chars before `TryAddWithoutValidation`; throws `ArgumentException` with `nameof(request)`. New test: `QueryClient_RejectsEtagsContainingControlCharacters_BeforeSend` covers `\"etag-1\"\r\nInjected-Header: value`.
- [x] [Review][Patch] P5 — `EventStoreCommandClient.ResolveRetryAfter` helper extracted: prefers `RetryAfter.Delta`, falls back to `RetryAfter.Date - DateTimeOffset.UtcNow` clamped to `TimeSpan.Zero` when negative. New test: `CommandClient_FallsBackToRetryAfterDate_WhenDeltaIsAbsent`.
- [x] [Review][Patch] P6 — `EventStoreValidation.ValidateETagCount` error message now interpolates the configured `maxCount` (`$"At most {maxCount} ETag validators..."`). New test: `EventStoreValidation_InterpolatesConfiguredMaxCount_InErrorMessage` proves a custom `maxCount: 2` produces "At most 2".
- [x] [Review][Patch] P7 — Added `EventStoreCancellationTests.QueryClient_PropagatesCancellationToken_ToHttpClientSend` (asserts `handler.ObservedToken.CanBeCanceled` — HttpClient wraps the user token via linked source) and `Subscribe_PropagatesCancellationToken_ToJoin` in `ProjectionSubscriptionServiceTests`.
- [x] [Review][Patch] P8 — `EventStoreCommandClient.ReadCorrelationIdAsync` now logs a structured `LogWarning` on `JsonException` (deliberately omitting `ex.Message` since `JsonException` can echo response-body fragments — surfaced via P2 test). `ProjectionSubscriptionService.OnProjectionChangedAsync` wraps notifier invocation in try/catch (`ex is not OutOfMemoryException`), logging at warning level so a buggy subscriber can't kill the SignalR callback dispatcher. New test: `OnNudge_DoesNotPropagateSubscriberException_ToSignalRDispatcher`.

- [x] [Review][Defer] DF1 — `SignalRProjectionHubConnectionFactory` passes `CancellationToken.None` to `accessTokenProvider` [`SignalRProjectionHubConnectionFactory.cs:10`] — deferred; SignalR's `AccessTokenProvider` API has no token parameter (inherent framework limitation). Revisit with Story 5-3 reconnect-policy work where a service-level cancellation source can be wired in.
- [x] [Review][Defer] DF2 — `EventStoreQueryClient.QueryAsync` parses unbounded response body [`EventStoreQueryClient.cs:79-80`] — deferred; spec only enforces request body limit. Add `MaxResponseBytes` option in Story 5-2 (response handling) or Story 5-6 (governance).
- [x] [Review][Defer] DF3 — `ContractsAssembly_DoesNotReferenceInfrastructurePackages` substring match against `"Hosting"`/`"EventStore"` [`EventStoreContractTests.cs:54-62`] — deferred; could false-positive on benign assembly names. Tighten with an exact deny-list when Story 9-1 (drift detection) lands.
- [x] [Review][Defer] DF4 — `EventStoreIdentity.GetAggregateId` allocates `new[]` per call and runs reflection lookup unmemoized [`EventStoreIdentity.cs:35-43`] — deferred; perf optimization in command-dispatch hot path. Address with Story 9-4 governance/AOT cleanup.
- [x] [Review][Defer] DF5 — Non-202/non-200 responses discard the response body before throwing [`EventStoreCommandClient.cs:60-67`, `EventStoreQueryClient.cs:72-77`] — deferred per spec D6: full HTTP response matrix and ProblemDetails handling are Story 5-2.

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
| D14 | Story 5-1's done boundary is contract extensions plus the default EventStore adapter, not a new event-sourcing API. | Keeps the story aligned with existing `ICommandService`, `IQueryService`, and `IProjectionSubscription` seams and prevents stream/versioning/product policy from leaking into implementation. | Introduce generic append/read stream abstractions; redesign event envelope taxonomy; certify multiple providers in this story. |
| D15 | Transport diagnostics are redacted and bounded. | EventStore requests carry tenant, user, authorization, and business payload data; support logs must prove behavior without becoming a data exfiltration path. | Log raw HTTP bodies and headers for easier debugging; rely on adopters to scrub logs downstream. |
| D16 | SignalR active-group state is commit-after-join. | Marking a group active before `JoinGroup` succeeds creates false unsubscribe/rejoin behavior after cancellation, hub start failure, or disposal races. | Optimistically mark active before the hub call; rely on later unsubscribe to clean failed joins. |

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
- Separate test intent: contract tests guard source compatibility and package boundaries; HTTP handler tests guard REST wire shape; SignalR fakes guard group, lifetime, and concurrency behavior; the smoke test guards DI composition.
- Do not add Playwright or browser tests for 5-1.
- Do not run submodule test suites from FrontComposer tests; submodule behavior is consumed as a pinned contract reference.
- Add at least one dependency/assembly test that proves `Hexalith.FrontComposer.Contracts` does not reference assemblies whose names contain `Dapr`, `SignalR`, `AspNetCore`, `Hosting`, or `EventStore` implementation packages.
- Add negative diagnostics tests proving access tokens, raw payload bodies, and raw tenant/user values are absent from captured logs on validation, HTTP failure, and SignalR join failure paths.

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
- Generic event append/read stream APIs, expected-revision conflict UX, event naming/versioning policy, snapshot/retention policy, and multi-provider certification.

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

## Change Log

| Date | Change |
| --- | --- |
| 2026-04-25 | Implemented EventStore service abstractions, opt-in DI registration, REST command/query clients, SignalR projection subscription service, contract append-only metadata, validation helpers, and focused regression tests. |

## Dev Agent Record

### Party-Mode Review

- Date/time: 2026-04-25T12:55:24.5676224+02:00
- Selected story key: `5-1-eventstore-service-abstractions`
- Command/skill invocation used: `/bmad-party-mode 5-1-eventstore-service-abstractions; review;`
- Participating BMAD agents: Winston (System Architect), John (Product Manager), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor)
- Findings summary: endpoint ownership and done-boundary language needed to be crisper; existing public communication contracts and source compatibility must remain the implementation anchor; cancellation, tenant/user fail-closed behavior, request-size validation, subscription concurrency/disposal, and package-boundary tests need explicit developer-facing coverage; Pact/live-provider certification and generic append/read stream APIs were judged out of scope for this story.
- Changes applied: added adopter outcome and done-boundary language; expanded AC8 with cancellation and subscription concurrency/disposal coverage; added explicit test-first sequencing; added D14 to prevent generic event-sourcing API scope creep; clarified test intent by layer; expanded scope guardrails for stream APIs, expected-revision UX, event taxonomy, retention/snapshot policy, and multi-provider certification.
- Findings deferred: Pact/provider compatibility remains Story 10-3; full response/error UX remains Story 5-2; reconnect and permanent-disconnect UX remain Stories 5-3/5-4; event naming/versioning, snapshot, retention, and multi-provider certification require later architecture/product decisions.
- Final recommendation: ready-for-dev

### Advanced Elicitation

- Date/time: 2026-04-25T14:03:11.7274912+02:00
- Selected story key: `5-1-eventstore-service-abstractions`
- Command/skill invocation used: `/bmad-advanced-elicitation 5-1-eventstore-service-abstractions`
- Batch 1 method names: Pre-mortem Analysis; Red Team vs Blue Team; Failure Mode Analysis; First Principles Analysis; Occam's Razor Application
- Reshuffled Batch 2 method names: Security Audit Personas; Chaos Monkey Scenarios; Self-Consistency Validation; Comparative Analysis Matrix; Hindsight Reflection
- Findings summary: the elicitation confirmed that the story's architecture boundary is sound, but implementation traps remained around diagnostics accidentally logging secrets/PII, DI services capturing tenant/user/token context too early, SignalR subscriptions being marked active before `JoinGroup` succeeds, and partial subscribe/cancel/dispose races leaving stale active-group state.
- Changes applied: added AC-level redaction and commit-after-join requirements; added task guidance to resolve tenant/user/token per operation rather than at construction time; added redacted structured logging rules; added subscription failure/cancellation cleanup requirements; added D15 and D16; added diagnostics negative tests for tokens, payloads, tenant/user values, HTTP failures, and SignalR join failures.
- Findings deferred: richer reconnect/rejoin policy, user-facing disconnected UX, and permanent failure recovery remain Stories 5-3/5-4; provider/Pact certification remains Story 10-3; full response classification and ProblemDetails body handling remain Story 5-2.
- Final recommendation: ready-for-dev

### Agent Model Used

GPT-5.2

### Debug Log References

- `dotnet build .\Hexalith.FrontComposer.sln -warnaserror -maxcpucount:1` -- passed, 0 warnings/errors.
- `dotnet test .\Hexalith.FrontComposer.sln --no-build -maxcpucount:1` -- passed: SourceTools 481, Contracts 83, Shell 990 passed / 3 skipped, Bench 2.

### Implementation Plan

- Preserve existing `ICommandService`, `ICommandServiceWithLifecycle`, `IQueryService`, and `IProjectionSubscription` seams by extending records append-only instead of adding parallel dispatcher/executor contracts.
- Keep EventStore infrastructure in Shell: Contracts gained only metadata/result/validation helpers; Shell owns REST clients, SignalR connection creation, DI registration, and option validation.
- Use `IUserContextAccessor` and command/query request metadata per operation, fail closed on missing/colon-containing tenant/user/routing values, and keep logging free of bearer tokens and raw request bodies.
- Use named `HttpClient` registrations for command/query transport and a small internal SignalR abstraction for deterministic subscription lifetime/concurrency tests.

### Completion Notes List

- Extended `CommandResult`, `QueryRequest`, and `QueryResult<T>` append-only for EventStore accepted metadata, query routing metadata, ETag validator lists, and explicit 304 not-modified results.
- Added `EventStoreOptions`, `AddHexalithEventStore(...)`, `EventStoreCommandClient`, `EventStoreQueryClient`, `ProjectionSubscriptionService`, default projection notifier, request-size guards, tenant/user/routing validation, and SignalR hub connection wrapper.
- Registered EventStore services as opt-in scoped defaults, replacing only the FrontComposer stub command service while preserving consumer replacement after registration.
- Added contract, DI, HTTP, request-size, auth/token, path override, query ETag/304, and SignalR subscription lifetime tests.
- Verified full build and solution regression suite pass.

### File List

- `Directory.Packages.props`
- `_bmad-output/implementation-artifacts/5-1-eventstore-service-abstractions.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Contracts/Communication/CommandResult.cs`
- `src/Hexalith.FrontComposer.Contracts/Communication/EventStoreValidation.cs`
- `src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs`
- `src/Hexalith.FrontComposer.Contracts/Communication/QueryResult.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreIdentity.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreOptions.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreOptionsValidator.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreRequestContent.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnection.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionChangeNotifier.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Communication/EventStoreContractTests.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Communication/QueryRequestTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStoreClientTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStoreRegistrationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs`
