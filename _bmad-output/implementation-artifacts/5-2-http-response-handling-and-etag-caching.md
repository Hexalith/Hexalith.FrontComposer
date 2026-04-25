# Story 5.2: HTTP Response Handling & ETag Caching

Status: ready-for-dev

> **Epic 5** -- Reliable Real-Time Experience. Covers **FR33** client-side ETag caching, **FR34** full HTTP response-matrix handling, and **NFR17-NFR19** data-posture constraints. Applies lessons **L03**, **L06**, **L10**, and **L14**.

---

## Executive Summary

Story 5-2 builds directly on Story 5-1's transport seams. Do not introduce a second HTTP/query stack. The implementation should extend the EventStore command/query clients and the generated command-form/runtime surfaces so the framework handles the full response matrix without leaking infrastructure concerns into adopters' code.

Five design constraints dominate this story:

- Query caching must be **tenant + user scoped**, must use `IStorageService`, and must remain **opportunistic**. Correctness still comes from the server. A `304 Not Modified` without a matching cached snapshot is a recoverable protocol mismatch, not a silent empty result.
- Story 4-3 and 4-4 already added filter, sort, search, pagination, and virtualization. Therefore an ETag cache keyed only by `projectionType` is no longer sufficient; 5-2 must key by a framework-generated **canonical query fingerprint** while keeping raw user input out of storage-key segments.
- `IStorageService.SetAsync` is already fire-and-forget and `FrontComposerShell` already flushes it on `beforeunload`. Reuse those seams; do not invent a second write queue or a second unload hook.
- Business-data caching must not contaminate the existing NFR17 UI-preferences persistence rail under `State/`. Keep the ETag cache in a dedicated Shell-owned EventStore/cache service and add dedicated tests for its data posture.
- The HTTP status matrix splits cleanly by UX: `200/304` are query/cache paths, `202` remains lifecycle acknowledgement, `400` becomes inline field validation, `401` triggers auth redirect, `403/404/429` become warning feedback, and `409` stays the domain-rejection path.

---

## Story

As a business user,  
I want the framework to handle all server responses gracefully and cache data intelligently,  
so that I see appropriate feedback for every situation and the application feels fast even with repeated queries.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | The framework receives EventStore HTTP responses | The response status is evaluated | `200 OK` stores the returned ETag + snapshot and renders normally; `202 Accepted` still drives lifecycle acknowledgement; `304 Not Modified` reuses the cached snapshot without a user-visible stale-data glitch; `400/401/403/404/409/429` map to the user-facing behaviors defined below. |
| AC2 | A successful query response includes an ETag | The result is cached | The cache entry is written through `IStorageService`, scoped to `{tenantId}:{userId}`, bounded by a dedicated configurable ETag-cache cap, and keyed by a framework-generated query fingerprint rather than raw user input. |
| AC3 | A subsequent query for the same effective request is executed | The client has a cached validator | The client sends `If-None-Match`, consumes `304 Not Modified` using the cached snapshot, performs one unconditional recovery retry if the server returns `304` but no matching cache entry exists, and never silently returns an empty result for that path. |
| AC4 | A command submission returns `400 Bad Request` | Validation details are present | The generated form surfaces field-level validation inline via `EditContext`/`ValidationMessageStore`, keeps user input intact, and also preserves a summary-level explanation for non-field validation messages. |
| AC5 | A command or query returns `401`, `403`, `404`, `409`, or `429` | The framework maps the response | `401` redirects/challenges through a framework abstraction, `403` shows a warning message saying the user lacks permission, `404` shows a warning that the target no longer exists, `409` remains a domain rejection with entity-specific recovery copy, and `429` shows retry guidance including parsed `Retry-After` data when present. |
| AC6 | The browser is unloading | `beforeunload` fires | The existing `FrontComposerShell` unload hook calls `IStorageService.FlushAsync`, and pending ETag cache writes are drained without a second unload-registration path. |
| AC7 | Query paging and projection loading run through Story 4-4's server-side lane | A cached page is reused or a query failure occurs | The virtualization pipeline receives cached results on `304`, and structured query failures for `403/404/429` are surfaced without collapsing into a generic `"errorMessage"`-only experience. |
| AC8 | Tests run | Contracts, Shell, generator, and cache tests execute | Tests cover cache-key isolation, no raw filter/search strings in storage keys, bounded eviction, `304` recovery, fire-and-forget writes + flush, `400` inline validation, `401/403/404/409/429` mapping, and no regression of existing lifecycle / DataGrid behavior. |

---

## Tasks / Subtasks

- [ ] T1. Extend the response contracts append-only (AC1, AC4, AC5, AC7)
  - [ ] Read Story 5-1's final implementation before coding. Adapt to the actual names it lands with; do not fork a parallel response model.
  - [ ] Keep `ICommandService`, `IQueryService`, `CommandResult`, and `QueryResult<T>` source-compatible for existing callers.
  - [ ] Add explicit response metadata for query/cache handling if Story 5-1 did not already land it: at minimum a no-change flag, the effective cache key/fingerprint, and typed response details needed by consumers.
  - [ ] Introduce typed command/query failure models in Contracts without leaking Fluent UI types there. Use framework-owned semantic intent/category enums or typed exceptions instead of raw HTTP-status integers scattered through UI code.
  - [ ] Model `400` validation errors separately from `409` domain rejection. Prefer a derived `CommandValidationException : CommandRejectedException` with field-error payloads over overloading the existing rejection type for every failure mode.
  - [ ] Normalize `429 Retry-After` to a framework-friendly value (UTC deadline and/or `TimeSpan`) so UI and tests do not parse header strings repeatedly.

- [ ] T2. Add a dedicated ETag cache service and bounded key strategy (AC2, AC3, AC6)
  - [ ] Create a Shell-owned cache service under `Infrastructure/EventStore` or `Services/EventStore`; do not put business-data cache persistence under `State/`.
  - [ ] Add a cache-envelope type that stores at least: `ETag`, `Items`, `TotalCount`, `CachedAtUtc`, `LastAccessedUtc`, a cache-schema version, and the canonical query fingerprint it corresponds to.
  - [ ] Build storage keys with `StorageKeys.BuildKey(tenantId, userId, "etag", discriminator)`.
  - [ ] Make `discriminator` an opaque framework-generated fingerprint from a canonical query signature: projection type, skip/take, sorted column filters, sorted status filters, search query, sort column, and sort direction. Raw filter/search values must not appear directly in the storage key string.
  - [ ] Add a dedicated options cap such as `FcShellOptions.MaxCachedEtagEntries` so ETag snapshots cannot evict all preference-state keys under the global `LocalStorageMaxEntries` budget.
  - [ ] Implement feature-local eviction by scanning only the current tenant/user's `etag` prefix and removing the least-recently-used cache envelopes when the dedicated cap is exceeded.
  - [ ] Keep writes fire-and-forget through `IStorageService.SetAsync`. Only reads, enumerations, removals, and `FlushAsync` are awaited.

- [ ] T3. Wire query execution and `304` recovery (AC1, AC2, AC3, AC7)
  - [ ] Extend the EventStore query client from Story 5-1 to consult the ETag cache before send, emit `If-None-Match`, and write successful `200` snapshots asynchronously after deserialization succeeds.
  - [ ] On `304 Not Modified`, prefer the already-loaded in-memory snapshot when the caller already has one; otherwise load the cached envelope from `IStorageService`.
  - [ ] If the server returns `304` and the cache is empty/corrupt/mismatched, remove the bad entry and immediately retry the request once without `If-None-Match`. Do not spin and do not surface an empty-success response.
  - [ ] If deserialization of a cached envelope fails, purge that entry and fall back to a network request rather than surfacing stale/corrupt data.
  - [ ] Flow the resulting structured response into Story 4-4's `IProjectionPageLoader` / `LoadPageEffects` path so `304` hits resolve `ProjectionPageResult` cleanly.
  - [ ] Replace the current plain-string query failure path with a structured failure payload that can carry intent and retry metadata for `403/404/429`.

- [ ] T4. Surface command-side HTTP failures in generated forms (AC1, AC4, AC5)
  - [ ] Read `CommandFormEmitter.cs` and the verified emitter snapshots before editing. Preserve the existing lifecycle callback and rejection flow.
  - [ ] Add `ValidationMessageStore` ownership to generated forms so `400` responses can annotate field-level errors against the existing `EditContext`.
  - [ ] Keep field values intact after `400`, `409`, and `429`. No reset-on-error behavior is allowed.
  - [ ] Route `409 Conflict` through the existing rejection semantics so `FcLifecycleWrapper` continues to render the domain-rejection message.
  - [ ] Surface `403`, `404`, and `429` as framework-owned warning feedback outside the lifecycle wrapper so `Rejected` remains reserved for domain/business rejection.
  - [ ] Add a minimal auth-redirect abstraction for `401` (for example `IAuthenticationRedirector` or equivalent) instead of hard-coding a route. It must work for both Blazor Server and WASM adopters.
  - [ ] Preserve `202 Accepted -> Acknowledged` behavior exactly as Story 2-4 / 5-1 expect.

- [ ] T5. Localize and render shared response feedback (AC4, AC5, AC7)
  - [ ] Add EN + FR resource keys for at least: unauthorized challenge fallback, forbidden warning, not-found warning, rate-limit warning, retry-after formatting, validation-summary fallback, and stale-cache recovery warning if surfaced.
  - [ ] Keep Contracts free of Fluent UI types; Shell components may translate the response-feedback intent into `FluentMessageBar` intent locally.
  - [ ] Add a small shared Shell component or helper for response feedback only if it materially reduces duplicate form/DataGrid message-bar markup. Avoid a generic "everything bar" abstraction if the existing components stay clearer.
  - [ ] Ensure warning/error bars set appropriate accessibility semantics (`role`, `aria-live`) consistent with existing lifecycle components.

- [ ] T6. Tests and verification (AC1-AC8)
  - [ ] Contracts tests: append-only compatibility, retry-after parsing, typed validation-error payloads, and cache-fingerprint determinism.
  - [ ] Shell cache tests: bounded eviction by tenant/user prefix, no raw search/filter strings in storage keys, `200` write path, `304` hit path, `304` missing-cache recovery retry, corrupt-cache purge, and unload flush reuse.
  - [ ] Generator tests: verified output includes `ValidationMessageStore`, inline field-message wiring, preserved rejection flow, and warning-bar rendering for non-domain HTTP failures.
  - [ ] DataGrid/virtualization tests: `LoadPageEffects` or the page loader consume structured query failures for `403/404/429`, and `304` reuses cached rows without a generic failure dispatch.
  - [ ] NFR tests: add a dedicated cache-data-posture test proving only query snapshots flow through the ETag cache service, and keep the Story 3-6 NFR17 tripwire scoped to preference-state call sites.
  - [ ] Regression suite: no breakage to `FcLifecycleWrapper`, `FrontComposerShell.FlushAsync`, `LocalStorageService`, `StorageKeys`, or Story 4-4 paging behavior.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs` | Already carries `ProjectionType`, pagination, ETag, column filters, status filters, search, and sort state. | Story 5-2 must honor the full effective query shape when generating cache fingerprints; keying only by projection type will corrupt filtered/sorted/paged caches. |
| `src/Hexalith.FrontComposer.Contracts/Communication/QueryResult.cs` | Currently only `Items`, `TotalCount`, and `ETag`. | Extend append-only; do not break Story 4-4 query callers. |
| `src/Hexalith.FrontComposer.Contracts/Storage/IStorageService.cs` | Stable 5-method abstraction with documented `{tenantId}:{userId}:{feature}:{discriminator}` pattern. | Reuse it. Do not add a second browser-storage abstraction for ETag caching. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs` | Already implements fire-and-forget `SetAsync`, global LRU, and `FlushAsync` drain semantics. | Reuse write queue + flush path. Story 5-2 may add a feature-local cache cap, but not a second persistence queue. |
| `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` + `wwwroot/js/fc-beforeunload.js` | Already registers a `beforeunload` hook that races `FlushAsync` against a short JS budget. | Reuse this unload path for ETag writes; do not register another unload listener from the cache service. |
| `src/Hexalith.FrontComposer.Shell/State/StorageKeys.cs` | Guards `tenantId`/`userId` against colon collisions and centralizes storage-key composition. | Keep all ETag cache keys on this builder; do not hand-concatenate strings elsewhere. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Today catches `CommandRejectedException`, dispatches `RejectedAction`, and calls `_editContext?.NotifyValidationStateChanged()`, but does not own `ValidationMessageStore`. | Extend this path for `400` validation without regressing the existing rejection/lifecycle flow. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/VirtualizationActions.cs` + `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs` | Query-load failures currently collapse to a plain `errorMessage` string. | Story 5-2 must enrich this path enough to distinguish retryable/rate-limited/auth/query-not-found failures from generic transport failures. |

### Cross-Story Contract Table

| Seam | Producer | Consumer | Story 5-2 decision |
| --- | --- | --- | --- |
| Transport seams | Story 5-1 EventStore clients | Story 5-2 response mapping + cache service | Extend 5-1 clients; do not fork a second HTTP/query implementation. |
| Filter/sort/search/paging | Stories 4-3 and 4-4 `QueryRequest` additions | Story 5-2 ETag cache | Query fingerprint must include the full effective request shape, not just projection type. |
| Storage infrastructure | Story 3-1 `IStorageService` + `LocalStorageService` | Story 5-2 ETag cache writes | Reuse fire-and-forget writes and beforeunload flush; no new storage abstraction. |
| Lifecycle UX | Stories 2-4 and 2-5 `FcLifecycleWrapper` + generated forms | Story 5-2 command error mapping | Preserve `202 -> Acknowledged` and `409 -> Rejected`; non-domain HTTP warnings render outside the lifecycle wrapper. |
| Tenant isolation | Story 2-2 L03 + `IUserContextAccessor` / `StorageKeys` | Story 5-2 cache scope | Fail closed when tenant/user are missing. No anonymous/default fallback cache segments. |
| Auth stack variability | Epic 7 future auth integration | Story 5-2 `401` handling | Introduce a minimal redirect/challenge seam rather than hard-coding routes or providers. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | Keep ETag snapshots in a dedicated Shell-owned cache service, not in generic Fluxor preference state. | NFR17/NFR18 distinguish UI preference persistence from ETag-validated business-data snapshots. | Add ETag cache writes inside `State/*` effects; persist snapshots directly in DataGrid feature state. |
| D2 | Cache keys use `StorageKeys.BuildKey(tenantId, userId, "etag", fingerprint)`. | Keeps identity scoping centralized and preserves colon-guard behavior. | Hand-build keys in multiple EventStore classes; add a second key-format convention. |
| D3 | `fingerprint` is a canonical framework-generated hash of the effective query shape. | Story 4-3/4-4 filters and search make `projectionType`-only caches unsafe, but raw user text should not appear in key strings. | Key only by projection type; concatenate raw search/filter text into the storage key. |
| D4 | Add a dedicated ETag cache cap such as `FcShellOptions.MaxCachedEtagEntries`. | Prevents cached projection snapshots from consuming the entire global localStorage budget and starving preference state. | Rely only on `LocalStorageMaxEntries`; leave ETag cache effectively unbounded. |
| D5 | `304 Not Modified` with a missing or corrupt cache entry triggers one unconditional recovery retry. | A `304` without a usable snapshot is a bounded recoverable mismatch, not a silent empty success. | Return an empty result; throw immediately without recovery; retry indefinitely. |
| D6 | `400` validation is modeled separately from `409` domain rejection. | Inline field validation and domain/business rejection are distinct user experiences and should not be conflated. | Treat every non-2xx command failure as `CommandRejectedException`; force everything through `FcLifecycleWrapper`'s rejected bar. |
| D7 | `409 Conflict` continues to use the domain-rejection path. | Existing generated forms and lifecycle wrapper already know how to surface domain rejection cleanly. | Invent a second UI path for domain conflicts. |
| D8 | `401` handling goes through a minimal framework auth-redirect abstraction. | FrontComposer must support Blazor Server and WASM adopters with different challenge/navigation stacks. | Hard-code `/login`; directly depend on a specific auth provider API. |
| D9 | `403`, `404`, and `429` use a shared semantic feedback model, translated to Fluent UI only in Shell/UI code. | Contracts remain UI-framework-neutral while Shell still renders the required message bars. | Put `MessageBarIntent` in Contracts; scatter integer status-code checks across components. |
| D10 | ETag cache writes remain fire-and-forget. | Story 3-1 already built the correct queue + flush semantics; render-path latency should not block on storage. | Await every cache write inline; add a second background worker. |
| D11 | Feature-local eviction uses least-recently-used metadata stored with each cache envelope. | Keeps ETag-specific eviction deterministic even though `LocalStorageService` also has a broader global LRU. | FIFO-only feature eviction; global-cap-only eviction with no feature budget. |
| D12 | Structured query failures extend the virtualization/load-page path instead of remaining plain strings. | Story 4-4's generic string failure path is insufficient for 403/404/429 UX requirements. | Keep only `errorMessage` strings and lose retry/auth/not-found semantics. |

### Library / Framework Requirements

- Keep target frameworks unchanged: Contracts `net10.0;netstandard2.0`, Shell `net10.0`.
- Shell already has `FrameworkReference Include="Microsoft.AspNetCore.App"`, so prefer in-box ASP.NET Core types such as `ProblemDetails` / `ValidationProblemDetails` if they fit the EventStore response shape.
- Keep using `System.Text.Json` web defaults. Cache-envelope serialization should use the same production serializer discipline already locked by `LocalStorageService.SchemaLockJsonOptions`.
- Do not add a second local-storage or cache package. The repo already owns storage and eviction behavior.
- If hashing the query fingerprint, use in-box .NET cryptography (`SHA256.HashData`) rather than introducing a hashing package.
- External HTTP references checked on 2026-04-25:
  - MDN ETag header -- `ETag` identifies a specific representation and supports conditional cache validation.
  - MDN If-None-Match header -- conditional `GET` with matching validators must return `304 Not Modified`.
  - MDN 304 Not Modified -- response has no body and should still carry cache-relevant headers such as `ETag`.
  - MDN Retry-After header -- `429 Too Many Requests` may specify either delay-seconds or an HTTP date.

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Communication/QueryResult.cs` | Append-only query/cache response metadata. |
| `src/Hexalith.FrontComposer.Contracts/Communication/CommandRejectedException.cs` and/or new companion types | Separate `400` validation payloads from `409` domain rejection and carry structured response metadata. |
| `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs` | Add `MaxCachedEtagEntries` or equivalent dedicated cache-cap setting. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs` | `200` cache write path, `304` cache-read/recovery path, structured query failure mapping. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs` | Full command response matrix mapping (`400/401/403/404/409/429`). |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/*Cache*.cs` | Dedicated ETag cache service, envelope, fingerprint helper, and feature-local eviction logic. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Inline field-validation wiring and non-domain HTTP warning feedback. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/VirtualizationActions.cs` | Structured query-failure payload for server-side page loading. |
| `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs` and related UI | Consume structured query failures and `304` cache hits cleanly. |
| `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources*.resx` | Localized command/query feedback copy. |
| `tests/Hexalith.FrontComposer.Contracts.Tests/Communication/*` | Contract + response-model compatibility tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/*` | Cache, response-mapping, and recovery tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/*CommandForm*` | Verified generator-output tests for inline validation + warning bars. |

### Testing Standards

- Use xUnit v3, Shouldly, NSubstitute, and bUnit.
- Keep EventStore transport tests deterministic with fake `HttpMessageHandler`s; no live EventStore dependency.
- Preserve the Story 3-6 NFR17 persistence tripwire and add a separate ETag-cache posture test rather than widening the old whitelist casually.
- Add serializer round-trip tests for the cache envelope using production JSON options.
- Add at least one test proving the query fingerprint is stable across equivalent dictionary ordering and changes when search/filter/sort/paging actually change.
- Add one regression test proving `304` recovery does not loop indefinitely when the cache entry is absent.

### Scope Guardrails

Do not implement these in Story 5-2:

- SignalR connection loss, reconnect banners, or form-preservation UX -- Story 5-3.
- Batched reconciliation sweeps or schema-evolution invalidation UX -- Story 5-4.
- Polling fallback cadence, idempotent terminal outcome resolution, or optimistic badge desaturation -- Story 5-5.
- Build-time infrastructure analyzers or distributed tracing spans -- Story 5-6.
- SignalR fault injection harness -- Story 5-7.
- A generalized cache layer for arbitrary framework/business data beyond EventStore query snapshots.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Auth redirect/challenge abstraction will be intentionally minimal until Epic 7 lands the full auth stack. | Story 7-1 |
| Reconnect-driven ETag invalidation and stale-projection sweep remain out of scope here. | Story 5-4 |
| Polling fallback still needs the same response-matrix semantics once SignalR is unavailable. | Story 5-5 |
| Product/docs need to reconcile the older architecture shorthand `{tenantId}:{userId}:etag:{projectionType}` with Story 4-3/4-4's newer filter/search/sort requirements. | Story 9-5 or planning-correction follow-up |

---

## References

- [Source: _bmad-output/planning-artifacts/epics/epic-5-reliable-real-time-experience.md#Story-5.2] -- Story statement and base acceptance criteria.
- [Source: _bmad-output/planning-artifacts/prd/functional-requirements.md#FR33-FR34] -- ETag caching + full HTTP response matrix.
- [Source: _bmad-output/planning-artifacts/prd/non-functional-requirements.md#NFR17-NFR19] -- Data-posture constraints for cache contents and scoping.
- [Source: _bmad-output/planning-artifacts/architecture.md#IStorageService-Contract] -- `IStorageService` shape, fire-and-forget writes, and localStorage strategy.
- [Source: _bmad-output/planning-artifacts/architecture.md#EventStore-communication-contract] -- REST query + ETag contract and tenant-scope expectations.
- [Source: _bmad-output/implementation-artifacts/5-1-eventstore-service-abstractions.md] -- Prior story transport seam and DI assumptions.
- [Source: src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs] -- Existing query shape including filters/search/sort/paging.
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs] -- Existing fire-and-forget queue + flush semantics.
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs] -- Existing unload-time `FlushAsync` wiring.
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs] -- Current rejection/validation pipeline entry point.
- [Source: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/ETag] -- Current ETag semantics.
- [Source: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/If-None-Match] -- Conditional request / `304` behavior.
- [Source: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/304] -- `304 Not Modified` body/header constraints.
- [Source: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Retry-After] -- `Retry-After` semantics for `429` and `503`.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L03] -- tenant/user fail-closed.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L14] -- bounded caches must ship with a default cap.

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
