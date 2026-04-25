# Story 5.2: HTTP Response Handling & ETag Caching

Status: ready-for-dev

> **Epic 5** -- Reliable Real-Time Experience. **FR33** bounded opportunistic ETag caching, **FR34** full HTTP response handling, plus the first framework-owned bridge from Story 5-1's EventStore transport seam into user-facing query/command behavior. Applies lessons **L01**, **L03**, **L10**, and **L14**.

---

## Executive Summary

Story 5-2 turns Story 5-1's transport clients into a coherent runtime behavior contract for both command forms and projection queries. The developer must keep three constraints front and center:

- Reuse the existing Shell seams (`IStorageService`, `StorageKeys`, `FrontComposerShell.FlushAsync`, `IProjectionPageLoader`, `BadgeCountService`, generated command forms, `FcLifecycleWrapper`) instead of building a second persistence or notification stack.
- Keep `Contracts` append-only. Generated forms, lifecycle bridges, projection loaders, and badge-count readers already compile against `CommandResult`, `CommandRejectedException`, `QueryRequest`, and `QueryResult<T>`; extend them without breaking Story 2.x / 3.x / 4.x call sites.
- Treat the ETag cache as **opportunistic and bounded**. Correctness always comes from the server. A cache hit or `304 Not Modified` must avoid user-visible churn, but missing or stale cache data must never fabricate state.

The intended shape is:

- `202 Accepted` stays on the existing lifecycle path (`Submitted -> Acknowledged`) with richer response metadata preserved for later stories.
- `400 Bad Request` becomes inline field validation on generated forms, not a generic rejection toast.
- `403`, `404`, and `429` surface friendly warning `FluentMessageBar` copy, while `409 Conflict` continues to flow through the domain-rejection path so Story 2-5's rejection UX remains intact.
- Query-side `200 OK + ETag` writes a bounded cache entry via `IStorageService` without blocking render. Query-side `304 Not Modified` reuses cached payload and must not trigger a fake "new data loaded" state transition.

---

## Story

As a business user,  
I want the framework to handle all server responses gracefully and cache data intelligently,  
so that I see appropriate feedback for every situation and the application feels fast even with repeated queries.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | The framework receives an EventStore query response | The response status is evaluated | `200 OK` returns data normally and captures the response `ETag`; `304 Not Modified` resolves from cached data without inventing a new payload; `401`, `403`, `404`, and `429` are mapped to explicit query-side failure types instead of opaque `HttpRequestException`. |
| AC2 | The framework receives an EventStore command response | The response status is evaluated | `202 Accepted` keeps the existing lifecycle acknowledge path; `400 Bad Request` surfaces inline field-level validation errors; `401` redirects to the authentication flow; `403` shows a warning MessageBar explaining the missing permission; `404` shows a warning MessageBar that the target entity no longer exists; `409 Conflict` maps to domain-specific rejection copy with entity + resolution; `429` shows a warning MessageBar with retry-after guidance. |
| AC3 | A successful query response includes an `ETag` | The result is cached | The cache entry is scoped to `{tenantId}:{userId}:{featureName}:{discriminator}`, stored through `IStorageService`, written fire-and-forget so render is not blocked, and evicted by a configurable per-ETag-cache LRU cap before it can crowd out unrelated preference state. |
| AC4 | A later query targets the same framework-owned projection snapshot or badge-count route | The same cache entry exists | The cached validator is sent via `If-None-Match`; `304 Not Modified` reuses the cached payload; `200 OK` replaces the cached payload + `ETag`; a `304` without a matching cache entry is treated as a protocol inconsistency and retried once uncached before failing loudly. |
| AC5 | The application is closing | The browser `beforeunload` path fires | Story 3-1's existing `FrontComposerShell -> IStorageService.FlushAsync` hook drains pending cache writes; Story 5-2 adds no second unload hook or parallel persistence channel. |
| AC6 | Cache keys are constructed | Tenant/user/feature/discriminator segments are resolved | All security-sensitive segments come from framework-controlled values (JWT-derived tenant/user, compile-time projection/query identifiers, framework-generated page/count discriminators). No raw user-entered form value or search text is embedded in cache keys, and cross-tenant reads remain fail-closed. |
| AC7 | Projection pages and action-queue badges consume query results | They use Story 5-2's response layer | `IProjectionPageLoader` and `IActionQueueCountReader` reuse the same response classifier + ETag cache seam; 304 no-change paths do not fake "new" page loads; warning/error copy is consistent across projection and badge refresh lanes. |
| AC8 | Tests run | Contracts, Shell services, generator output, and component tests execute | Tests cover the response matrix, field-level validation mapping, warning-banner copy, `401` redirect path, 304 no-op behavior, bounded cache eviction, fire-and-forget persistence + flush drain, fail-closed tenant/user scope, and consumer seams (`LoadPageEffects`, action-queue counts) using the same cache logic. |

---

## Tasks / Subtasks

- [ ] T1. Extend the Contracts response surface append-only (AC1, AC2, AC4)
  - [ ] Read Story 5-1's seams first: `ICommandService`, `ICommandServiceWithLifecycle`, `CommandResult`, `CommandRejectedException`, `QueryRequest`, and `QueryResult<T>`. Preserve existing Story 2.x generated form call sites and Story 4-4 projection-loader call sites.
  - [ ] Add explicit query no-change semantics append-only, preferably `QueryResult<T>.IsNotModified` / `NotModified`, rather than reusing exceptions for the expected `304` path.
  - [ ] Introduce a response/error taxonomy that lets generated forms distinguish validation errors, auth redirects, warning-level failures, and domain rejections without collapsing everything into `CommandRejectedException`.
  - [ ] Keep `409 Conflict` compatible with the existing rejection flow. If new exception types are introduced, `CommandRejectedException` remains the domain-conflict shape consumed by generated forms and `FcLifecycleWrapper`.
  - [ ] Parse RFC 7807 / `ValidationProblemDetails` payloads append-only so field errors, title/detail, entity labels, and retry-after metadata are available without leaking raw HTTP details into Razor emitters.

- [ ] T2. Add a bounded ETag cache seam on top of existing storage infrastructure (AC3, AC5, AC6)
  - [ ] Reuse `IStorageService`, `LocalStorageService`, `InMemoryStorageService`, `StorageKeys`, and Story 3-1's beforeunload flush. Do not add a second browser-storage abstraction or a second JS unload hook.
  - [ ] Add a dedicated cache seam under `Shell/State/ETagCache/` or `Shell/Infrastructure/EventStore/ETagCache/` so Story 5-2 can evolve without leaking storage details into query clients. The seam may be service-centric, but keep the folder/namespace aligned with the architecture extraction plan.
  - [ ] Add a dedicated `FcShellOptions` cap for ETag-cache entries (separate from `LocalStorageMaxEntries`) and validate it alongside existing cross-property option checks.
  - [ ] Persist only server-derived projection snapshots / count payloads plus metadata (`ETag`, cached-at, discriminator, projection/query identity). Never store user-entered command drafts or raw auth tokens.
  - [ ] Implement per-cache LRU eviction within the ETag-cache seam so rapid query churn does not evict unrelated persisted state like theme, density, navigation, or DataGrid preferences.

- [ ] T3. Define a safe cache-key discriminator policy (AC3, AC4, AC6)
  - [ ] Use `StorageKeys.BuildKey(tenantId, userId, "etag", discriminator)` as the canonical prefix/pattern.
  - [ ] The discriminator must be framework-controlled: compile-time projection/query identity plus framework-generated page/count lane identifiers. Do not embed raw user-entered search terms, free-text filters, or arbitrary form values into storage keys.
  - [ ] If a query shape cannot be keyed safely under that rule, skip caching for that shape rather than weakening the key policy.
  - [ ] Document exactly which query families Story 5-2 caches on day one: projection snapshot/page queries whose discriminator is framework-generated, and action-queue count queries keyed by projection runtime type.

- [ ] T4. Wire query-side 200 / 304 handling through projection and badge consumers (AC1, AC4, AC7)
  - [ ] Extend the default EventStore query client from Story 5-1 so `If-None-Match` is set from the ETag cache and `200 OK` writes fresh cache entries fire-and-forget.
  - [ ] Introduce an explicit 304 no-change path for the server-side DataGrid lane. A "304 but dispatch `LoadPageSucceededAction` anyway" implementation is not acceptable because it still churns Fluxor/UI state.
  - [ ] Prefer an explicit `LoadPageNotModifiedAction` or equivalent no-op reducer/effect path that resolves pending TCS completion from the cached page while leaving `LoadedPageState.PagesByKey`, `TotalCountByKey`, and user-visible render state unchanged.
  - [ ] Add the first real `IActionQueueCountReader` implementation on top of the query client and the same cache seam so Story 3-5 badge refreshes benefit from 304/cache behavior too.
  - [ ] Keep the 5-2 cache as an optimization only. If the cache is empty, corrupt, over budget, or out of scope for safe discriminator construction, fall back to a normal network query.

- [ ] T5. Map command-response statuses to form UX without breaking the lifecycle wrapper contract (AC2, AC8)
  - [ ] Keep `FcLifecycleWrapper` focused on lifecycle states (`Submitting`, `Acknowledged`, `Syncing`, `Confirmed`, domain `Rejected`). Do not overload it with every HTTP warning state unless that proves strictly simpler than a dedicated generated-form feedback region.
  - [ ] Add generated-form support for server-side `400 Bad Request` field errors using `ValidationMessageStore` / `EditContext`, clearing stale server errors on re-submit or field edit.
  - [ ] Add a framework-owned warning-banner path for `403`, `404`, and `429` copy that generated forms can render consistently without abusing the rejection/error path.
  - [ ] Introduce a minimal auth-redirect seam for `401 Unauthorized` rather than hard-coding a login URL. The default may be a no-op/exception until adopters register a real auth redirector, but the contract must be explicit.
  - [ ] Preserve Story 2-5's domain rejection experience for `409 Conflict`: entity name + why it failed + what the user should do next, all as plain text (no HTML / no `MarkupString`).

- [ ] T6. Keep response classification centralized and reusable (AC1, AC2, AC7)
  - [ ] Put EventStore HTTP status parsing, ProblemDetails decoding, `Retry-After` parsing, and `ETag` extraction in one Shell-side helper/service. Do not duplicate status-switch blocks in the query client, count reader, projection page loader, and generated forms.
  - [ ] Treat `304` + missing cache, malformed `ETag`, malformed problem payload, or impossible response combinations as explicit diagnostics/failures rather than silent fallbacks that hide protocol drift.
  - [ ] Leave `503 Service Unavailable`, reconnect sweep UX, and polling fallback user journeys to Stories 5-3 through 5-5, but preserve enough metadata now so later stories do not need a breaking contract change.

- [ ] T7. Tests and verification (AC1-AC8)
  - [ ] Contracts tests: append-only compatibility of new result/exception types; `QueryResult<T>` 304 semantics; `CommandRejectedException` remains compatible with generated forms.
  - [ ] Cache tests: per-cache LRU eviction, fire-and-forget writes, `FlushAsync` drain on pending writes, fail-closed tenant/user scope, and "304 without cache -> one uncached retry -> fail loud if still inconsistent".
  - [ ] Query/client tests: `If-None-Match` emission, `ETag` capture, `200` overwrite, `304` reuse, and `401/403/404/429` classification.
  - [ ] DataGrid tests: explicit 304 no-change path proves no new `LoadedPageState.PagesByKey` write and no synthetic `LastElapsedMsByKey` / `TotalCountByKey` churn.
  - [ ] Badge tests: `IActionQueueCountReader` returns cached counts on 304 and re-fetches cleanly on 200.
  - [ ] Generator/component tests: `400` field-level validation mapping into `ValidationMessageStore`; `403/404/429` warning banner rendering; `409` domain rejection copy; `401` auth redirect invocation.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs` | Already carries projection type, tenant, paging, filters, search, sort, and a single `ETag` field. | Extend append-only; do not force Story 4-4 consumers to learn raw HTTP concepts. |
| `src/Hexalith.FrontComposer.Contracts/Communication/QueryResult.cs` | Today returns `Items`, `TotalCount`, `ETag` only. | Add explicit 304/no-change semantics append-only rather than replacing the record or switching to exceptions. |
| `src/Hexalith.FrontComposer.Contracts/Communication/CommandResult.cs` and `CommandRejectedException.cs` | Generated forms already expect `MessageId` on success and a rejection exception on domain conflict. | Keep Story 2-3 / 2-5 generator output compiling. Domain rejection remains the compatibility path for `409 Conflict`. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs` | Fire-and-forget `SetAsync`, single drain worker, global `LocalStorageMaxEntries`, and `FlushAsync` sentinel drain already exist. | Reuse this service; Story 5-2 adds cache-specific policy above it, not a parallel storage stack. |
| `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` | Already wires `fc-beforeunload.js` to `IStorageService.FlushAsync()`. | Story 5-2 must route all pending cache writes through the same flush path. |
| `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs` and `LoadedPageReducers.cs` | Server-side paging already has typed TCS completion + success/failure/cancel paths. | Add an explicit 304/no-change path; do not fake success with a new state write. |
| `src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs` plus `Contracts/Badges/IActionQueueCountReader.cs` | Story 3-5 already expects a future real count reader with Story 5-2 ETag caching. | Implement the real reader on top of the same query/cache seam rather than inventing a badge-only path. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Generated forms currently catch `CommandRejectedException` and notify validation state changes, but they do not yet inject server field errors. | Keep emitter changes incremental and append-only; use `ValidationMessageStore` instead of replacing the form architecture. |
| `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.*` | Lifecycle wrapper handles success/info/error around `CommandLifecycleState`, not general HTTP warnings. | Keep lifecycle semantics intact. Prefer a separate warning-banner path for `403/404/429`. |
| `src/Hexalith.FrontComposer.Shell/State/StorageKeys.cs` and `Contracts/Rendering/IUserContextAccessor.cs` | Tenant/user storage keys are fail-closed and colon-guarded; null/whitespace identity is the sanctioned unauthenticated state. | Reuse the same fail-closed scope rules for the ETag cache. No `"anonymous"` or `"default"` fallback. |

### Cross-Story Contract Table

| Seam | Producer | Consumer | Story 5-2 decision |
| --- | --- | --- | --- |
| EventStore transport seam | Story 5-1 command/query clients | Story 5-2 response classifier | 5-2 adds runtime behavior on top of 5-1's transport clients without changing endpoint ownership. |
| Command lifecycle wrapper | Stories 2-3 through 2-5 | 5-2 command response mapping | `202` stays lifecycle-compatible; only `409` continues through the rejection path; validation/warning/auth flows use separate handling. |
| Browser storage + beforeunload | Story 3-1 `IStorageService` + `FrontComposerShell.FlushAsync` | 5-2 ETag cache | 5-2 reuses the existing storage/drain pipeline and does not add new unload hooks. |
| DataGrid server-side paging | Story 4-4 `IProjectionPageLoader`, `LoadPageEffects`, `LoadedPageState` | 5-2 query cache / 304 path | 304 must resolve pending page requests without a fake success-state mutation. |
| Action queue badge refresh | Story 3-5 `BadgeCountService` + `IActionQueueCountReader` | 5-2 real count reader | The badge lane shares the same EventStore query + ETag cache seam as projection pages. |
| Reconnect and polling recovery | Stories 5-3 through 5-5 | 5-2 cache metadata | 5-2 persists enough `ETag` + cached payload metadata so later stories can requery / poll without breaking contracts. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | Keep the ETag cache on top of the existing `IStorageService` seam. | Story 3-1 and 3-6 already solved browser storage, fire-and-forget writes, and beforeunload draining. | Add a second browser-storage abstraction; write straight to JS from query clients. |
| D2 | Add a dedicated per-ETag-cache entry cap in `FcShellOptions`. | Global `LocalStorageMaxEntries` is too coarse; a busy query cache must not evict theme/navigation preferences by accident. | Reuse only `LocalStorageMaxEntries`; ship an unbounded ETag cache. |
| D3 | Cache only query families whose discriminator is framework-controlled. | AC security language forbids raw user-entered values in cache keys; correctness beats cache coverage. | Embed raw search/filter text in keys; hash arbitrary user input and call it "framework-controlled." |
| D4 | `304 Not Modified` is an explicit no-change path, not a disguised success path. | A success dispatch with unchanged data still churns Fluxor/UI state and violates the "no re-render" acceptance intent. | Re-dispatch `LoadPageSucceededAction` with cached items; ignore 304 and always render a fresh success state. |
| D5 | `400 Bad Request` maps to `EditContext` / `ValidationMessageStore`, not the rejection bar. | Validation errors are field-level correction work, not a domain rejection after server processing. | Show a generic error toast; collapse validation into `CommandRejectedException`. |
| D6 | `403`, `404`, and `429` use warning banners separate from `FcLifecycleWrapper`. | The lifecycle wrapper is intentionally about state transitions, not every warning-class HTTP failure. | Reuse the rejection/error bar for warnings; add more lifecycle states for forbidden/not-found/rate-limit. |
| D7 | `409 Conflict` stays on the domain-rejection contract. | Generated forms and Story 2-5 UX already expect conflict-like business failures through `CommandRejectedException`. | Treat 409 like validation; downgrade it to a warning banner. |
| D8 | `401 Unauthorized` flows through an explicit auth-redirect seam. | The framework cannot assume a login URL or host-specific auth wiring. | Hard-code `/authentication/login`; silently swallow 401 and hope a later story fixes it. |
| D9 | Cache writes remain fire-and-forget and rely on the existing flush barrier. | Blocking render on storage defeats the "opportunistic cache" goal and duplicates LocalStorageService behavior. | Await every cache write inline; build a second synchronous flush mechanism. |
| D10 | A `304` without a matching cache entry is treated as protocol drift and retried uncached once. | The server says the client already has a valid representation; if it does not, that inconsistency must not become silent empty state. | Treat it as success with empty data; keep retrying indefinitely; silently ignore the response. |
| D11 | Only server-derived projection snapshots and count payloads are cached. | NFR17-19 explicitly reject storing user-entered data/PII at the framework layer. | Cache full command drafts or arbitrary component state for convenience. |
| D12 | Response decoding is centralized in one EventStore HTTP classifier/parser. | Query clients, badge readers, projection page loaders, and generated forms all need the same status/problem parsing rules. | Duplicate `switch (StatusCode)` logic in every caller. |

### Library / Framework Requirements

- Target existing TFMs and package lines already in the repo: Contracts `net10.0;netstandard2.0`, Shell `net10.0`, xUnit v3, bUnit, Shouldly, NSubstitute.
- Reuse `System.Text.Json` web defaults and the existing Shell serializer conventions; do not introduce Newtonsoft.Json for ProblemDetails or cache payloads.
- Reuse ASP.NET Core `ProblemDetails` / `ValidationProblemDetails` conventions when decoding 400/403/404/409/429 payloads; do not invent a FrontComposer-specific error envelope unless the EventStore payload demonstrably deviates.
- Prefer the built-in `System.Net.Http.Headers` ETag/Retry-After parsing types where practical rather than string-splitting headers by hand.
- Keep the EventStore REST + SignalR contract authoritative from the pinned submodule docs/code. FrontComposer is the consumer, not the endpoint owner.

External references checked on 2026-04-25:

- EventStore local docs: `Hexalith.EventStore/docs/reference/query-api.md` -- 200/304/400/401/403/404/429/503 query surface, `If-None-Match`, `ETag`, SignalR nudge contract.
- EventStore local docs: `Hexalith.EventStore/docs/reference/command-api.md` -- 202/400/401/403/409/413/415/429 command surface, `Retry-After`, `X-Correlation-ID`, ProblemDetails examples.
- Microsoft Learn: Handle errors in ASP.NET Core APIs (.NET 10) -- `AddProblemDetails`, problem-details service, status-code middleware behavior.
- Microsoft Learn: Model validation in ASP.NET Core MVC (.NET 10) -- `ValidationProblemDetails`, validation pipeline shape.
- RFC 9110 section 15.4.5 -- `304 Not Modified` semantics and required response headers (`ETag`, `Date`, etc.).

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Communication/QueryResult.cs` | Append-only 304/no-change semantics for query results. |
| `src/Hexalith.FrontComposer.Contracts/Communication/CommandResult.cs` plus new response/error types | Append-only response metadata and command/query failure taxonomy. |
| `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs` | Dedicated ETag-cache entry cap and any other story-owned cache settings. |
| `src/Hexalith.FrontComposer.Shell/Options/FcShellOptionsThresholdValidator.cs` | Cross-property validation for the new cache settings. |
| `src/Hexalith.FrontComposer.Shell/State/ETagCache/*` | Cache entry model, optional scoped cache service, and any LRU bookkeeping kept aligned with the architecture extraction seam. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/*` | Central HTTP response classifier/parser, query-client cache integration, and any EventStore-specific adapters. |
| `src/Hexalith.FrontComposer.Shell/Badges/*` | Real `IActionQueueCountReader` implementation reusing the query/cache seam. |
| `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/*` | Explicit 304/no-change path for server-side paging. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Generated-form support for inline validation + warning banners + auth redirect seam. |
| `tests/Hexalith.FrontComposer.Contracts.Tests/Communication/*` | Contract/result/exception compatibility and parser tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/*` | Query/command response classification, cache integration, and header parsing tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/ETagCache/*` | LRU/persistence/fail-closed scope tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/*` | 304 no-change paging behavior tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/*` | Generated command-form feedback/validation output coverage. |

### Testing Standards

- Use deterministic fake `HttpMessageHandler` or equivalent HTTP doubles; no live EventStore required.
- Use `InMemoryStorageService` in unit tests where browser JS interop is irrelevant; use the real `LocalStorageService` test harness for fire-and-forget + flush + LRU behavior.
- Keep field-validation tests at the generator/component layer so the emitted form code proves server validation actually reaches `EditContext`.
- Add regression coverage for the exact 304 no-change guarantee on DataGrid pages and badge counts.
- Add at least one guard test proving cache scope is fail-closed when `IUserContextAccessor.TenantId` or `.UserId` is null/empty/whitespace.

### Scope Guardrails

Do not implement these in Story 5-2:

- SignalR disconnect banners, reconnect UX, or form-state preservation -- Story 5-3.
- Rejoin sweeps, stale-row animation, and schema-mismatch invalidation UX -- Story 5-4.
- Polling fallback UX, optimistic badge transitions, or idempotent terminal reconciliation -- Story 5-5.
- OpenTelemetry / activity source / build-time seam enforcement -- Story 5-6.
- Fault injection harness -- Story 5-7.
- Runtime customization of cache policy per projection type -- future Epic 6 / 9 work.
- Caching arbitrary user-entered search/filter payloads merely to increase hit rate.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Full reconnect + polling recovery UX that consumes Story 5-2 cache metadata. | Story 5-3 through 5-5 |
| Query-side 503 / dependency-unavailable messaging policy. | Story 5-3 or 5-5 |
| Documentation alignment where older planning text still says `/projections-hub` while the pinned EventStore hub is `/hubs/projection-changes`. | Story 9-5 documentation site or planning-correction task |
| Pact/provider verification of the richer ProblemDetails/status matrix against the EventStore submodule. | Story 10-3 |

---

## References

- [Source: _bmad-output/planning-artifacts/epics/epic-5-reliable-real-time-experience.md#Story-5.2] -- Story statement, baseline ACs, FR33/FR34 intent.
- [Source: _bmad-output/implementation-artifacts/5-1-eventstore-service-abstractions.md] -- Transport seams, endpoint ownership, and compatibility constraints Story 5-2 must preserve.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L03] -- Fail-closed tenant/user scope.
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L14] -- Bounded-by-policy cache requirement.
- [Source: _bmad-output/planning-artifacts/architecture.md#EventStore-communication-contract] -- REST + ETag + SignalR contract sketch.
- [Source: _bmad-output/planning-artifacts/architecture.md#Per-Concern-Fluxor-Features] -- `ETagCacheState` and storage-key expectations.
- [Source: _bmad-output/planning-artifacts/architecture.md#Data-Boundaries] -- ETag cache ownership and storage scope.
- [Source: _bmad-output/planning-artifacts/prd/functional-requirements.md#FR33-FR34] -- Bounded client cache + full HTTP response matrix.
- [Source: _bmad-output/planning-artifacts/prd/non-functional-requirements.md#Security-Data-Handling] -- zero-PII framework posture and opportunistic cache rule.
- [Source: _bmad-output/planning-artifacts/prd/user-journeys.md#Ayse-and-the-40-second-dropout] -- real-time trust story motivating no-fake-refresh/no-silent-failure behavior.
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs] -- fire-and-forget storage + LRU + flush barrier.
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs] -- existing beforeunload flush hook.
- [Source: src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs] -- current query-loading IO boundary.
- [Source: src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadedPageReducers.cs] -- current success/failure/cancel reducer behavior that Story 5-2 must extend for 304.
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs] -- current generated form command submit/rejection flow.
- [Source: Hexalith.EventStore/docs/reference/query-api.md] -- query response matrix, `If-None-Match`, `ETag`, `304`, `429`, SignalR nudge contract.
- [Source: Hexalith.EventStore/docs/reference/command-api.md] -- command response matrix, `Retry-After`, ProblemDetails examples, `202 Accepted`.

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
