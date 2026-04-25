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
- `400 Bad Request` becomes inline field validation on generated forms when fields can be matched; unmapped/global validation errors become a form-level validation MessageBar while preserving entered values.
- `401 Unauthorized` invokes a framework-owned auth-redirect seam, abandons the current transport result, preserves existing client-side form/query state where the component already owns it, and does not mutate the ETag cache.
- `403`, `404`, and `429` surface friendly warning `FluentMessageBar` copy, while `409 Conflict` continues to flow through the domain-rejection path so Story 2-5's rejection UX remains intact. `429` includes `Retry-After` guidance when available; `404` remains an inline warning unless a later story defines navigation recovery.
- Query-side `200 OK + ETag` writes a bounded cache entry via `IStorageService` without blocking render. Query-side `304 Not Modified` reuses cached payload and must not trigger a fake "new data loaded" state transition, badge animation, timestamp churn, selection churn, or success toast.
- Cached payloads carry enough framework-owned entry metadata to reject stale or incompatible entries after deployment, serializer, or projection-contract changes. A rejected cache entry is a diagnostic miss, never a migration requirement and never a reason to clear visible UI.
- The shared EventStore classifier is a Shell-side transport classifier with separate command and query outcomes. UI placement is handled by generated forms, DataGrid effects, badge services, and auth-redirect adapters, not by raw `HttpClient` code.

---

## Story

As a business user,  
I want the framework to handle all server responses gracefully and cache data intelligently,  
so that I see appropriate feedback for every situation and the application feels fast even with repeated queries.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | The framework receives an EventStore query response | The response status is evaluated | The centralized classifier emits query outcomes: `200 OK` returns data normally and captures the response `ETag`; `304 Not Modified` resolves from cached data without inventing a new payload; `401`, `403`, `404`, and `429` are mapped to explicit query-side failure types instead of opaque `HttpRequestException`; cached snapshots/counts are never used as speculative fallback for network failures or 5xx responses. |
| AC2 | The framework receives an EventStore command response | The response status is evaluated | The centralized classifier emits command outcomes: `202 Accepted` keeps the existing lifecycle acknowledge path; `400 Bad Request` surfaces field-level validation errors when fields match and form-level validation otherwise; `401` invokes the auth-redirect seam without cache mutation or lifecycle rejection; `403` shows a warning MessageBar explaining the missing permission; `404` shows an inline warning that the target entity no longer exists; `409 Conflict` maps to domain-specific rejection copy with entity + resolution; `429` shows a warning MessageBar with retry-after guidance when available. |
| AC3 | A successful query response includes an `ETag` | The result is cached | The cache entry is scoped to `{tenantId}:{userId}:etag:{discriminator}`, stored through `IStorageService`, written fire-and-forget so render is not blocked, carries framework-owned entry metadata for cache format/projection payload compatibility, and is evicted by a configurable global per-cache LRU entry cap before it can crowd out unrelated preference state. Missing/blank/colon-containing tenant, user, or discriminator segments skip cache read/write and perform an uncached request. Storage quota/write/serialization failures are logged diagnostically and do not fail the user operation. |
| AC4 | A later query targets the same framework-owned projection snapshot or badge-count route | The same cache entry exists | The cached validator is sent via `If-None-Match`; `304 Not Modified` reuses the cached payload only when the cache entry is readable and compatible with the current cache format/projection payload contract; `200 OK` replaces the cached payload + `ETag`; a `304` without a matching readable compatible cache entry is treated as protocol inconsistency, retried once uncached, succeeds only if that retry returns `200`, and otherwise fails loudly while preserving the currently visible grid rows or badge count. |
| AC5 | The application is closing | The browser `beforeunload` path fires | Story 3-1's existing `FrontComposerShell -> IStorageService.FlushAsync` hook drains pending cache writes; Story 5-2 adds no second unload hook or parallel persistence channel. |
| AC6 | Cache keys are constructed | Tenant/user/feature/discriminator segments are resolved | All security-sensitive segments come from framework-controlled values (JWT-derived tenant/user and an allowlisted discriminator for compile-time projection/query identity plus framework-generated page/count lanes). Raw user-entered form values, search text, free-text filters, arbitrary serialized query payloads, PII values, and hashed user input are forbidden in cache keys; cross-tenant, cross-user, cross-feature, and cross-discriminator reads remain fail-closed. |
| AC7 | Projection pages and action-queue badges consume query results | They use Story 5-2's response layer | `IProjectionPageLoader` and `IActionQueueCountReader` reuse the same response classifier + ETag cache seam; 304 no-change paths do not fake "new" page loads or badge changes; `429` preserves the currently visible badge count/grid data while surfacing retry guidance; warning/error copy is consistent across projection and badge refresh lanes. |
| AC8 | Tests run | Contracts, Shell services, generator output, and component tests execute | Tests cover the response matrix, field-level and form-level validation mapping, warning-banner copy, `401` redirect path and negative assertions, 304 no-op and retry behavior, bounded cache eviction, fire-and-forget persistence + flush drain, storage-write failure diagnostics, fail-closed tenant/user/discriminator scope, and consumer seams (`LoadPageEffects`, action-queue counts) using the same cache logic. |

---

## Tasks / Subtasks

- [ ] T1. Extend the Contracts response surface append-only (AC1, AC2, AC4)
  - [ ] Read Story 5-1's seams first: `ICommandService`, `ICommandServiceWithLifecycle`, `CommandResult`, `CommandRejectedException`, `QueryRequest`, and `QueryResult<T>`. Preserve existing Story 2.x generated form call sites and Story 4-4 projection-loader call sites.
  - [ ] Add explicit query no-change semantics append-only, preferably `QueryResult<T>.IsNotModified` / `NotModified`, rather than reusing exceptions for the expected `304` path.
  - [ ] Introduce a response/error taxonomy that lets generated forms distinguish validation errors, auth redirects, warning-level failures, and domain rejections without collapsing everything into `CommandRejectedException`.
  - [ ] Keep command and query outcomes separate in the taxonomy: query outcomes carry ETag/cache/no-change metadata; command outcomes carry accepted metadata, validation details, auth redirect intent, warning details, or domain rejection details.
  - [ ] Keep `409 Conflict` compatible with the existing rejection flow. If new exception types are introduced, `CommandRejectedException` remains the domain-conflict shape consumed by generated forms and `FcLifecycleWrapper`.
  - [ ] Parse RFC 7807 / `ValidationProblemDetails` payloads append-only so field errors, form-level errors, title/detail, entity labels, and retry-after metadata are available without leaking raw HTTP details into Razor emitters.
  - [ ] Map validation fields only through the generated command model's allowlisted property names. Unknown paths, nested paths that do not resolve exactly to a generated editable field, duplicate aliases, or hostile field names degrade to form-level validation instead of polluting unrelated fields.
  - [ ] Unknown or legacy 400 payloads must degrade to a form-level validation/warning result rather than throwing from generated form code or pretending field mapping succeeded.

- [ ] T2. Add a bounded ETag cache seam on top of existing storage infrastructure (AC3, AC5, AC6)
  - [ ] Reuse `IStorageService`, `LocalStorageService`, `InMemoryStorageService`, `StorageKeys`, and Story 3-1's beforeunload flush. Do not add a second browser-storage abstraction or a second JS unload hook.
  - [ ] Add a dedicated cache seam under `Shell/State/ETagCache/` or `Shell/Infrastructure/EventStore/ETagCache/` so Story 5-2 can evolve without leaking storage details into query clients. The seam may be service-centric, but keep the folder/namespace aligned with the architecture extraction plan.
  - [ ] Add `FcShellOptions.MaxETagCacheEntries` with a conservative default of 200 entries, a minimum of 0, and a maximum no greater than `LocalStorageMaxEntries`; `0` disables ETag caching without disabling network queries.
  - [ ] Persist only server-derived projection snapshots / count payloads plus metadata (`ETag`, cached-at, discriminator, projection/query identity). Never store user-entered command drafts or raw auth tokens.
  - [ ] Include cache-entry format and projection payload compatibility metadata in each entry. If the current runtime cannot read the entry, treat it as a diagnostic cache miss, remove/drop it best-effort, and continue with an uncached request.
  - [ ] Implement global per-cache LRU eviction within the ETag-cache seam using `cached-at` / last-access metadata so rapid query churn does not evict unrelated persisted state like theme, density, navigation, or DataGrid preferences.
  - [ ] Treat storage quota/write/read failures as diagnostic cache misses: log, skip or drop the affected cache entry, and continue the user operation from the server response.

- [ ] T3. Define a safe cache-key discriminator policy (AC3, AC4, AC6)
  - [ ] Use `StorageKeys.BuildKey(tenantId, userId, "etag", discriminator)` as the canonical prefix/pattern.
  - [ ] The discriminator must be framework-controlled and allowlisted: compile-time projection/query identity plus framework-generated page/count lane identifiers. Do not embed raw user-entered search terms, free-text filters, arbitrary form values, PII, hashes of user input, or arbitrary serialized query payloads into storage keys.
  - [ ] If `tenantId`, `userId`, or discriminator is null, whitespace, colon-containing, not allowlisted, or derived from user input, skip cache read/write and perform the request uncached.
  - [ ] If a query shape cannot be keyed safely under that rule, skip caching for that shape rather than weakening the key policy.
  - [ ] Document exactly which query families Story 5-2 caches on day one: projection snapshot/page queries whose discriminator is framework-generated, and action-queue count queries keyed by projection runtime type.

- [ ] T4. Wire query-side 200 / 304 handling through projection and badge consumers (AC1, AC4, AC7)
  - [ ] Extend the default EventStore query client from Story 5-1 so `If-None-Match` is set from the ETag cache and `200 OK` writes fresh cache entries fire-and-forget.
  - [ ] Introduce an explicit 304 no-change path for the server-side DataGrid lane. A "304 but dispatch `LoadPageSucceededAction` anyway" implementation is not acceptable because it still churns Fluxor/UI state.
  - [ ] Prefer an explicit `LoadPageNotModifiedAction` or equivalent no-op reducer/effect path that resolves pending TCS completion from the cached page while leaving `LoadedPageState.PagesByKey`, `TotalCountByKey`, and user-visible render state unchanged.
  - [ ] Add the first real `IActionQueueCountReader` implementation on top of the query client and the same cache seam so Story 3-5 badge refreshes benefit from 304/cache behavior too.
  - [ ] Keep the 5-2 cache as an optimization only. If the cache is empty, corrupt, over budget, or out of scope for safe discriminator construction, fall back to a normal network query without a validator.
  - [ ] For `304` with missing/corrupt/incompatible/evicted cache, retry once without `If-None-Match`; if retry returns `200`, replace the cache; if retry returns `304` again or a failure response, surface protocol drift/failure while preserving currently visible grid rows or badge count.
  - [ ] Badge reads on `304` keep the visible count unchanged and do not emit changed notifications, animations, or refresh timestamps.

- [ ] T5. Map command-response statuses to form UX without breaking the lifecycle wrapper contract (AC2, AC8)
  - [ ] Keep `FcLifecycleWrapper` focused on lifecycle states (`Submitting`, `Acknowledged`, `Syncing`, `Confirmed`, domain `Rejected`). Do not overload it with every HTTP warning state unless that proves strictly simpler than a dedicated generated-form feedback region.
  - [ ] Add generated-form support for server-side `400 Bad Request` field errors using `ValidationMessageStore` / `EditContext`, clearing stale server errors on re-submit or field edit, preserving user-entered values, and routing unknown/global errors to a form-level validation MessageBar.
  - [ ] Render ProblemDetails title/detail, validation messages, warning copy, and domain rejection text as bounded plain text only. Do not emit raw HTML, stack traces, exception type names, or `MarkupString` from server payloads.
  - [ ] Add a framework-owned warning-banner path for `403`, `404`, and `429` copy that generated forms can render consistently without abusing the rejection/error path; warning responses must not dispatch lifecycle acknowledgement or domain rejection.
  - [ ] Introduce a minimal auth-redirect seam for `401 Unauthorized` rather than hard-coding a login URL. The default may be a no-op/exception until adopters register a real auth redirector, but the contract must be explicit: no warning MessageBar, no lifecycle rejection, no validation pollution, no cache mutation, and no automatic command/query retry in Story 5-2.
  - [ ] Preserve Story 2-5's domain rejection experience for `409 Conflict`: entity name + why it failed + what the user should do next, all as plain text (no HTML / no `MarkupString`).

- [ ] T6. Keep response classification centralized and reusable (AC1, AC2, AC7)
  - [ ] Put EventStore HTTP status parsing, ProblemDetails decoding, `Retry-After` parsing, and `ETag` extraction in one Shell-side helper/service. Do not duplicate status-switch blocks in the query client, count reader, projection page loader, and generated forms.
  - [ ] Implement and test the classifier before DataGrid, badge, and generated-form UI wiring so UI layers consume classified command/query outcomes instead of branching on raw HTTP status codes.
  - [ ] Treat `304` + missing cache, malformed `ETag`, malformed problem payload, or impossible response combinations as explicit diagnostics/failures rather than silent fallbacks that hide protocol drift.
  - [ ] Leave `503 Service Unavailable`, reconnect sweep UX, and polling fallback user journeys to Stories 5-3 through 5-5, but preserve enough metadata now so later stories do not need a breaking contract change.

- [ ] T7. Tests and verification (AC1-AC8)
  - [ ] Contracts tests: append-only compatibility of new result/exception types; `QueryResult<T>` 304 semantics; `CommandRejectedException` remains compatible with generated forms.
  - [ ] Cache tests: per-cache LRU eviction, fire-and-forget writes, `FlushAsync` drain on pending writes, storage write/read/serialization failure as diagnostic cache miss, cache-entry format/projection payload incompatibility as diagnostic miss, fail-closed tenant/user/feature/discriminator scope, malicious discriminator rejection, user-entered filter/search rejection, and "304 without cache -> one uncached retry -> fail loud if still inconsistent".
  - [ ] Query/client tests: `If-None-Match` emission, `ETag` capture, `200` overwrite, `304` reuse, `304` with corrupt/incompatible/evicted cache, and `401/403/404/429` classification.
  - [ ] DataGrid tests: explicit 304 no-change path proves no loading flash, page reset, selection churn, new `LoadedPageState.PagesByKey` write, synthetic `LastElapsedMsByKey` / `TotalCountByKey` churn, or success toast/message.
  - [ ] Badge tests: `IActionQueueCountReader` returns cached counts on 304, re-fetches cleanly on 200, preserves prior visible count on 429, and emits no changed notification/animation on 304.
  - [ ] Generator/component tests: `400` known-field, unknown-field, nested-hostile-field, duplicate-alias, multiple-field, and form-level validation mapping; stale server errors clear on re-submit or field edit; entered values are preserved; ProblemDetails and warning/rejection text renders as bounded plain text with no raw HTML or stack traces; `403/404/429` warning banner rendering; `409` domain rejection copy; `401` auth redirect invocation with no warning, lifecycle rejection, validation pollution, or cache mutation.

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
| D2 | Add `FcShellOptions.MaxETagCacheEntries` as a dedicated global per-cache entry cap with default `200`, `0` disabling cache, and validation against `LocalStorageMaxEntries`. | Global `LocalStorageMaxEntries` is too coarse; a busy query cache must not evict theme/navigation preferences by accident, and tests need a concrete bound. | Reuse only `LocalStorageMaxEntries`; ship an unbounded ETag cache; use byte-size accounting in v1 before a clear payload-size requirement exists. |
| D3 | Cache only query families whose discriminator is framework-controlled and allowlisted. | AC security language forbids raw user-entered values in cache keys; correctness beats cache coverage. | Embed raw search/filter text in keys; hash arbitrary user input and call it "framework-controlled"; serialize arbitrary query payloads into keys. |
| D4 | `304 Not Modified` is an explicit no-change path, not a disguised success path. | A success dispatch with unchanged data still churns Fluxor/UI state and violates the "no re-render" acceptance intent. | Re-dispatch `LoadPageSucceededAction` with cached items; ignore 304 and always render a fresh success state; animate badge counts on cache validation. |
| D5 | `400 Bad Request` maps to `EditContext` / `ValidationMessageStore`, not the rejection bar. | Validation errors are field-level correction work, not a domain rejection after server processing. | Show a generic error toast; collapse validation into `CommandRejectedException`. |
| D6 | `403`, `404`, and `429` use warning banners separate from `FcLifecycleWrapper`. | The lifecycle wrapper is intentionally about state transitions, not every warning-class HTTP failure. | Reuse the rejection/error bar for warnings; add more lifecycle states for forbidden/not-found/rate-limit. |
| D7 | `409 Conflict` stays on the domain-rejection contract. | Generated forms and Story 2-5 UX already expect conflict-like business failures through `CommandRejectedException`. | Treat 409 like validation; downgrade it to a warning banner. |
| D8 | `401 Unauthorized` flows through an explicit auth-redirect seam and never mutates cache, validation state, or lifecycle state in Story 5-2. | The framework cannot assume a login URL or host-specific auth wiring, and auth failure must not look like validation, rejection, or cached data. | Hard-code `/authentication/login`; silently swallow 401 and hope a later story fixes it; show a generic warning MessageBar while also redirecting. |
| D9 | Cache writes remain fire-and-forget and rely on the existing flush barrier. | Blocking render on storage defeats the "opportunistic cache" goal and duplicates LocalStorageService behavior. | Await every cache write inline; build a second synchronous flush mechanism. |
| D10 | A `304` without a matching readable cache entry is treated as protocol drift and retried uncached once while preserving currently visible UI state. | The server says the client already has a valid representation; if it does not, that inconsistency must not become silent empty state or visible churn. | Treat it as success with empty data; keep retrying indefinitely; silently ignore the response; clear the grid/badge while retrying. |
| D11 | Only server-derived projection snapshots and count payloads are cached. | NFR17-19 explicitly reject storing user-entered data/PII at the framework layer. | Cache full command drafts or arbitrary component state for convenience. |
| D12 | Response decoding is centralized in one EventStore HTTP classifier/parser. | Query clients, badge readers, projection page loaders, and generated forms all need the same status/problem parsing rules. | Duplicate `switch (StatusCode)` logic in every caller. |
| D13 | ETag cache entries carry framework-owned format and projection-payload compatibility metadata and fail closed when unreadable or incompatible. | A deployment, serializer, or projection-contract change can make a locally stored representation unsafe even when the server returns `304`; correctness still comes from the server, so incompatible local data becomes a diagnostic miss and one uncached retry. | Trust any deserializable JSON shape; add a cache migration framework in 5-2; clear visible grid/badge state when cache metadata mismatches. |
| D14 | Server response text is rendered as bounded plain text and field validation is mapped only through generated-field allowlists. | ProblemDetails may contain hostile HTML, stack traces, or fields that do not belong to the generated command form; UX should be helpful without becoming an injection or validation-pollution path. | Render server detail via `MarkupString`; map arbitrary field paths by string similarity; drop all server detail and lose useful validation feedback. |

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

### Party-Mode Review

- Date/time: 2026-04-25T13:05:13.0872821+02:00
- Selected story key: `5-2-http-response-handling-and-etag-caching`
- Command/skill invocation used: `/bmad-party-mode 5-2-http-response-handling-and-etag-caching; review;`
- Participating BMAD agents: Winston (System Architect), John (Product Manager), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor)
- Findings summary: all four reviewers initially recommended `needs-story-update` because the story left too much implementation latitude around the shared EventStore response classifier, command-vs-query outcome boundaries, 401 auth redirect semantics, cache discriminator safety, cache eviction behavior, 304 missing-cache retry behavior, generated-form 400 fallback handling, badge no-churn expectations, and test precision. The concerns were clarification-level hardening items inside the existing story scope rather than new product scope.
- Changes applied: clarified that the classifier emits separate command and query outcomes; added explicit 401 behavior with no cache mutation, lifecycle rejection, validation pollution, or automatic retry; specified field-level vs form-level 400 validation fallback; added 403/404/429 warning boundaries; made cache keys fail-closed for missing/blank/colon-containing or non-allowlisted segments; forbade raw/hashes of user-entered search, filter, form, PII, or arbitrary payload values in discriminators; set `MaxETagCacheEntries` default to 200 with `0` disabling cache; defined global per-cache LRU behavior and storage failure diagnostics; sharpened `304` missing/corrupt/evicted-cache retry semantics; required DataGrid and badge no-churn behavior; expanded contract, cache, query, DataGrid, badge, generator, and auth negative tests.
- Findings deferred: byte-size cache accounting remains out of v1 unless later evidence requires it; SignalR disconnect/reconnect UX, polling fallback, OpenTelemetry/build-time enforcement, fault injection, and Pact/provider verification remain assigned to their existing follow-up stories.
- Final recommendation: ready-for-dev

### Advanced Elicitation

- Date/time: 2026-04-25T15:03:15.2298800+02:00
- Selected story key: `5-2-http-response-handling-and-etag-caching`
- Command/skill invocation used: `/bmad-advanced-elicitation 5-2-http-response-handling-and-etag-caching`
- Batch 1 method names: Security Audit Personas; Pre-mortem Analysis; Failure Mode Analysis; Self-Consistency Validation; Occam's Razor Application
- Reshuffled Batch 2 method names: Chaos Monkey Scenarios; First Principles Analysis; 5 Whys Deep Dive; Cross-Functional War Room; Comparative Analysis Matrix
- Findings summary: the elicitation found the story was structurally ready but still left three implementation traps: a valid `304` could reuse a locally stale cache entry after deployment or projection-contract drift; generated-form validation could be polluted by arbitrary ProblemDetails field paths; and server-provided detail text could accidentally become an HTML/stack-trace display path. The two-batch pass also rechecked the existing party-mode decisions against L08 and confirmed the remaining concerns are clarification-level hardening inside the current Story 5-2 scope.
- Changes applied: added cache-entry format/projection-payload compatibility metadata to the executive summary, AC3/AC4, T2, T4, T7, and new binding decision D13; tightened generated-form field mapping to generated-property allowlists in T1/T7; required bounded plain-text rendering for ProblemDetails, warning, validation, and rejection copy in T5/T7; added new binding decision D14 for plain-text response copy and validation-field allowlisting.
- Findings deferred: cache-entry migration/version-negotiation framework remains out of 5-2; byte-size cache accounting remains deferred until evidence requires it; multi-tab cache locking/coalescing, automatic retry scheduling from `Retry-After`, SignalR reconnect UX, polling fallback UX, schema-mismatch invalidation UX, and Pact/provider verification remain assigned to existing follow-up stories or future backlog work.
- Final recommendation: ready-for-dev

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

(to be filled in by dev agent)

### File List

(to be filled in by dev agent)
