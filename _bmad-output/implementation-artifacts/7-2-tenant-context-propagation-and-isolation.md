# Story 7.2: Tenant Context Propagation & Isolation

Status: ready-for-dev

> **Epic 7** - Authentication, Authorization & Multi-Tenancy. Covers **FR35**, consumes Story **7-1** authentication seams, prepares Story **7-3** authorization policies and Epic **8** tenant-scoped MCP tools, and enforces **NFR21 / NFR22 / NFR28 / NFR102**. Applies lessons **L01**, **L03**, **L06**, **L07**, **L08**, **L10**, and **L14**.

---

## Executive Summary

Story 7-2 makes the authenticated tenant context impossible to forget at the framework boundary:

- Introduce a single tenant-context resolver/validator used by command dispatch, query execution, SignalR subscription, ETag cache keying, and future MCP tool enumeration.
- Keep the public `IUserContextAccessor` shape unchanged. Story 7-1 supplies authenticated `TenantId` / `UserId`; Story 7-2 makes every operation consume those values consistently.
- Preserve the existing fail-closed EventStore behavior: no authenticated tenant, no user, malformed identifiers, or a requested tenant different from the authenticated tenant blocks work before backend dispatch.
- Replace ad hoc tenant arguments at call sites with canonical validation helpers where needed, but do not change backend API semantics beyond ensuring the same tenant is propagated everywhere.
- Keep v0.1 single-tenant compatibility through the existing Counter `DemoUserContextAccessor` / host-provided stub, with explicit production guardrails against `"default"`, `"anonymous"`, or empty synthetic tenants.
- Scope SignalR groups as `{projectionType}:{tenantId}` using the existing non-colon segment guards, and prove groups are not joined, rejoined, or notified across tenants.
- Keep ETag keys in the existing `{tenantId}:{userId}:etag:{discriminator}` shape and add cross-tenant cache oracles that fail if equal users in different tenants can share entries.
- Seed the contract needed by Epic 8 MCP tenant-scoped enumeration without implementing the MCP server in this story.

---

## Story

As a business user,
I want my data to be completely isolated from other tenants with no possibility of cross-tenant data leakage,
so that I can trust the application with my organization's data.

### Adopter Job To Preserve

An adopter should be able to configure authentication once, then rely on FrontComposer to carry the authenticated tenant through commands, queries, live projection subscriptions, and client cache keys without every generated component or business page manually re-validating tenant identity.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A validated Story 7-1 principal exposes non-empty `TenantId` and `UserId` through `IUserContextAccessor` | Any FrontComposer command, query, subscription, cache, or tenant-aware manifest operation executes | The framework resolves one canonical tenant context for the operation and uses ordinal comparison for all tenant equality checks. |
| AC2 | A command DTO has no `TenantId` property | The command is dispatched through `EventStoreCommandClient` | The authenticated tenant is inserted into the command envelope sent to EventStore. |
| AC3 | A command DTO has a `TenantId` property matching the authenticated tenant | The command is dispatched | Dispatch proceeds and the envelope tenant remains the authenticated tenant. |
| AC4 | A command DTO requests a different tenant than the authenticated tenant | The command is dispatched | The framework blocks before HTTP send, records a sanitized security diagnostic, and does not serialize or send the command payload. |
| AC5 | A query request has no tenant, a blank tenant, or a tenant matching the authenticated tenant | The query executes | The outbound query payload uses the authenticated tenant and preserves existing query/cache behavior. |
| AC6 | A query request names a different tenant | The query executes | The framework blocks before HTTP send, does not read or write the cache for that request, and surfaces a sanitized tenant-mismatch failure. |
| AC7 | A projection subscription is started | `SubscribeAsync(projectionType, tenantId)` is called by generated or handwritten code | The subscription validates projection type and tenant as non-colon segments, joins only `{projectionType}:{authenticatedTenant}`, and rejects any tenant argument that differs from the authenticated tenant. |
| AC8 | A SignalR reconnect occurs | Active groups are rejoined | Only groups for the authenticated tenant are rejoined; stale groups for a prior tenant/user context are removed or marked blocked before reconnect reconciliation runs. |
| AC9 | A SignalR nudge arrives for a projection and tenant | The callback is handled | The notifier, fallback scheduler, pending-command polling, and reconciliation paths process the nudge only when the nudge tenant matches an active group for the current authenticated tenant. |
| AC10 | ETag cache keys are built | Tenant A user `u1` and Tenant B user `u1` query the same projection/discriminator | The resulting cache keys are distinct, cache reads never cross tenants, and cache writes are skipped when tenant/user context is invalid. |
| AC11 | Tenant or user identifiers are null, empty, whitespace, multi-valued upstream, colon-containing, or synthetic production defaults | Any tenant-scoped operation starts | The operation fails closed before backend dispatch, group join, cache access, or future MCP enumeration. |
| AC12 | v0.1 single-tenant/demo mode is configured | The Counter sample or test host runs without a real IdP | A visibly named demo tenant provider continues to work in development/test, while production configuration cannot silently use the demo/default tenant. |
| AC13 | Tenant isolation diagnostics are emitted | A mismatch or malformed tenant is blocked | Logs and telemetry include only sanitized categories, presence booleans, failure codes, projection/command/query type, and correlation/message IDs; they do not log raw tenant IDs, user IDs, claims, tokens, command payloads, query filters, ETags, or cache keys. |
| AC14 | DAPR actor IDs or SignalR group names are constructed | Projection type and tenant are combined | The pattern remains `{projectionType}:{tenantId}` and both segments reject colons/control characters before composition. |
| AC15 | Epic 8 MCP tooling consumes domain manifests later | The story completes | A tenant-context abstraction or documented contract exists for tenant-scoped tool enumeration, but MCP server/tool implementation remains out of scope. |
| AC16 | Story 7-3 authorization policies are implemented later | The story completes | Authorization can depend on the same canonical tenant context without changing command/query/subscription propagation again. |

---

## Tasks / Subtasks

- [ ] T1. Define canonical tenant-context contract and validation helpers (AC1, AC11, AC13-AC16)
  - [ ] Add a Shell-owned `IFrontComposerTenantContext` / `ITenantContextAccessor` or equivalent internal service that reads `IUserContextAccessor`, validates tenant/user identifiers, and returns a typed result with `TenantId`, `UserId`, `IsAuthenticated`, and sanitized failure category.
  - [ ] Keep `IUserContextAccessor` unchanged; do not push `ClaimsPrincipal`, provider SDKs, or auth handler types into Contracts.
  - [ ] Centralize colon/control-character rejection and ordinal tenant comparison. Do not lowercase tenant IDs; current code treats tenant identity as case-sensitive.
  - [ ] Add production guardrails that reject configured synthetic tenants such as `"default"` / `"anonymous"` unless the host explicitly opts into demo/test mode.
  - [ ] Add HFC20xx Shell diagnostic IDs for tenant-context missing, malformed segment, tenant mismatch, demo tenant in production, and stale tenant context during reconnect.

- [ ] T2. Harden command tenant propagation (AC2-AC4, AC11, AC13)
  - [ ] Refactor `EventStoreCommandClient` to resolve tenant/user through the canonical context service before reading any optional command `TenantId`.
  - [ ] Preserve current behavior where command `TenantId` is optional and matching values are accepted.
  - [ ] On mismatch, fail before `SerializeCommandPayload`, before `ApplyAuthorizationAsync`, and before `HttpClient.SendAsync`.
  - [ ] Add tests proving no payload serialization, no Authorization header/token acquisition, no HTTP send, and no raw tenant/user/payload values in logs on mismatch.
  - [ ] Add regression tests for missing context, colon-containing tenant, colon-containing user, whitespace, and exact case-sensitive mismatch.

- [ ] T3. Harden query tenant propagation and cache gating (AC5, AC6, AC10, AC11, AC13)
  - [ ] Decide whether `QueryRequest.TenantId` remains required for compatibility or becomes nullable in a new overload/factory; either way, generated callers must not be forced to duplicate tenant lookup logic.
  - [ ] Ensure `EventStoreQueryClient` replaces blank/missing tenant requests with the authenticated tenant and blocks mismatches before cache key resolution.
  - [ ] Prove cache read and write are skipped on tenant mismatch or invalid context.
  - [ ] Add cross-tenant same-user tests: Tenant A / User X and Tenant B / User X produce distinct ETag keys and cannot read each other's cached payload.
  - [ ] Add tests for cache discriminator safety staying unchanged; raw filters/search terms remain forbidden as cache key material.

- [ ] T4. Harden SignalR subscription tenant propagation (AC7-AC9, AC11, AC13, AC14)
  - [ ] Inject the canonical tenant context into `ProjectionSubscriptionService`.
  - [ ] Validate `SubscribeAsync` tenant against the authenticated tenant before starting the hub connection or joining a group.
  - [ ] On tenant mismatch, do not start the connection, do not join any group, do not register active group state, and record a sanitized security diagnostic.
  - [ ] On reconnect, re-check the active authenticated tenant before rejoining groups; stale groups for a prior tenant/user context must be removed or fail closed.
  - [ ] Keep group composition as `{projectionType}:{tenantId}` and preserve `EventStoreValidation.RequireNonColonSegment` behavior.
  - [ ] Extend fault-injection harness tests to cover duplicate tenants, tenant switch during reconnect, stale nudge after tenant switch, and disposal during tenant mismatch.

- [ ] T5. Generated UI and caller contract updates (AC1-AC7, AC10, AC15, AC16)
  - [ ] Update SourceTools emit paths so generated query/subscription callers either pass the authenticated tenant through a framework helper or omit explicit tenant where the Shell fills it safely.
  - [ ] Do not add provider-specific auth references to generated components.
  - [ ] Add approval/snapshot tests proving generated DataGrid, Dashboard, StatusOverview, Timeline, DetailRecord, and ActionQueue surfaces use the tenant propagation contract consistently.
  - [ ] Keep Story 7-3 policy placeholders separate: no `[RequiresPolicy]`, button gating, or authorization service calls in this story.

- [ ] T6. Demo/single-tenant compatibility and production safety (AC11, AC12)
  - [ ] Preserve `samples/Counter/Counter.Web/DemoUserContextAccessor.cs` for local development and tests.
  - [ ] Add an explicit option such as `AllowDemoTenantContext` / `TenantContextMode` so sample/test hosts can use the demo tenant while production rejects it.
  - [ ] Add production-mode tests proving demo/default/anonymous tenants fail closed unless explicitly enabled.
  - [ ] Update Counter sample docs to explain where Story 7-1 auth bridge supplies real tenant/user values and where Story 7-2 propagation begins.

- [ ] T7. Tenant-scoped manifest contract for future MCP (AC15, AC16)
  - [ ] Add a small documented interface or design note that future MCP tool enumeration must call before listing tools.
  - [ ] Ensure the contract returns no tools when tenant context is missing or invalid.
  - [ ] Do not implement MCP server endpoints, tool execution, hallucination rejection, or authorization policies in this story.
  - [ ] Add compile-time or unit guardrails so any future MCP enumeration can reuse the same canonical tenant context rather than building a parallel accessor.

- [ ] T8. Security redaction and telemetry (AC4, AC6, AC7, AC11, AC13)
  - [ ] Add structured logging helpers for tenant-context failures using sanitized failure categories only.
  - [ ] Add redaction tests with raw tenant IDs, user IDs, emails, JWT-like strings, ETags, cache keys, command payload fragments, and query filters in exception messages.
  - [ ] Ensure telemetry tags use tenant markers or redacted booleans only, following existing `FrontComposerTelemetry.TenantMarker` discipline.
  - [ ] Treat any cross-tenant visibility path as a security bug; tests should fail if a cross-tenant operation reaches an HTTP send, group join, cache get/set, or future enumeration boundary.

- [ ] T9. Tests and verification (AC1-AC16)
  - [ ] Shell unit tests for canonical tenant context success/failure matrix.
  - [ ] EventStore command tests for no-tenant, matching tenant, mismatched tenant, colon tenant/user, and payload-not-serialized on mismatch.
  - [ ] EventStore query tests for blank/matching/mismatched tenant, no cache touch on mismatch, same-user-different-tenant cache isolation, and cache-discriminator safety unchanged.
  - [ ] Projection subscription tests for join/rejoin/nudge tenant isolation and tenant switch during reconnect.
  - [ ] SourceTools approval tests for generated tenant propagation call sites.
  - [ ] Counter sample tests for demo mode preserved and production guard active.
  - [ ] Redaction tests across logs, telemetry, diagnostics, cache, serialized state, and exception surfaces.
  - [ ] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [ ] Targeted tests: `tests/Hexalith.FrontComposer.Shell.Tests` EventStore/auth/cache/subscription lanes and `tests/Hexalith.FrontComposer.SourceTools.Tests` generated emission lanes.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve / Change |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Rendering/IUserContextAccessor.cs` | Flat `TenantId` / `UserId`; null/empty/whitespace means unauthenticated. | Preserve interface shape and fail-closed semantics. Story 7-2 may add Shell helpers, not a Contracts breaking change. |
| `samples/Counter/Counter.Web/DemoUserContextAccessor.cs` | Returns `counter-demo` / `demo-user` for sample local development. | Keep demo path for dev/test; add production guardrail rather than deleting it. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreIdentity.cs` | `RequireUserContext` already rejects missing tenant, requested-tenant mismatch, and colon-containing tenant/user through `EventStoreValidation`. | Use as precedent or refactor into canonical tenant-context service; do not weaken ordinal equality or colon guard. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs` | Reads optional command `TenantId`, validates against authenticated tenant, sends tenant in `SubmitCommandRequest`, applies token per operation. | Ensure mismatch blocks before payload serialization, token acquisition, and HTTP send. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs` | Requires `QueryRequest.TenantId`, validates against authenticated tenant, builds ETag keys from validated tenant/user, uses `IAuthRedirector` on 401. | Allow generated callers to avoid duplicating tenant lookup; keep cache correctness and block mismatches before cache access. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` | Accepts tenant as a method argument, validates non-colon segments, tracks active groups, rejoins on reconnect, notifies tenant-aware listeners when available. | Add authenticated-tenant matching and stale-group handling; preserve fault-injection harness and no-cross-tenant group shape. |
| `src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs` | `TryBuildKey` requires nonblank tenant/user, rejects colons, allowlists discriminators, builds via `StorageKeys.BuildKey(tenant,user,"etag",discriminator)`. | Preserve discriminator allowlist and LRU behavior; add same-user/different-tenant isolation tests. |
| `src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs` | `TenantId` is a non-nullable positional record parameter today. | Changing constructor shape is risky; prefer additive overload/factory/helper unless a clear compatibility plan is documented. |
| `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` | Registers `NullUserContextAccessor`, `IETagCache`, EventStore/reconciliation/pending-command services, and fail-fast `NoOpAuthRedirector`. | Keep default fail-closed behavior. Add tenant-context service with scoped lifetime because tenant/user are per circuit/request. |

### Architecture Contracts

- FrontComposer v1 multi-tenancy is logical isolation by tenant discriminator, not separate state stores, databases, per-tenant themes, or tenant-specific feature flags.
- Tenant identity comes from Story 7-1 authentication and must be propagated through commands, queries, SignalR subscriptions, ETag cache keys, and later MCP tool enumeration.
- `TenantId` equality is ordinal and case-sensitive unless a future product/architecture decision explicitly introduces canonicalization.
- No colons are allowed inside `ProjectionType`, `TenantId`, user ID, domain, aggregate, or actor/group segments because `:` is the framework separator for DAPR actor IDs, SignalR groups, and storage keys.
- Shell owns tenant-context validation and propagation. Contracts exposes only stable abstractions, and SourceTools emits calls into framework seams rather than provider-specific auth code.
- Cross-tenant data visibility, cache reuse, SignalR group reuse, or tool enumeration is a security bug and must be tested as a hard failure.

### Tenant Flow Sequence

1. Story 7-1 authenticates the user and exposes `TenantId` / `UserId` through `IUserContextAccessor`.
2. Story 7-2 tenant-context service validates nonblank, non-colon, non-synthetic production identifiers.
3. Command dispatch compares optional command tenant to the authenticated tenant, then sends the authenticated tenant in the EventStore envelope.
4. Query execution compares request tenant to the authenticated tenant, builds the query payload with the authenticated tenant, and resolves cache keys only after validation succeeds.
5. Projection subscription validates group tenant against the authenticated tenant before starting/joining, and revalidates before reconnect rejoin.
6. Future MCP enumeration receives the same validated tenant context and returns no tools when the context is missing or invalid.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 7-1 auth bridge | Story 7-2 tenant propagation | 7-1 supplies validated `TenantId` / `UserId`; 7-2 consumes and enforces propagation without changing provider login code. |
| Story 2-2 `IUserContextAccessor` | Story 7-2 canonical context | Null/empty/whitespace remains the only unauthenticated signal at the Contracts boundary. |
| Story 3-1/3-6 storage persistence | Story 7-2 cache isolation | Storage keys stay `{tenantId}:{userId}:{feature}:{discriminator}` and fail closed when tenant/user context is invalid. |
| Story 5-1/5-2 EventStore clients | Story 7-2 command/query propagation | Tenant mismatch is blocked before HTTP send; token relay remains owned by Story 7-1/EventStore options. |
| Story 5-3/5-4 SignalR/reconnect | Story 7-2 subscription isolation | Active groups and rejoin health are scoped by authenticated tenant; stale groups cannot receive nudges after tenant switch. |
| Story 5-5 pending-command polling | Story 7-2 live nudge path | Nudge-triggered pending polling remains same-tenant only and must not fan out across tenant groups. |
| Story 7-3 authorization policies | Story 7-2 canonical context | Policy checks can rely on the same tenant context; 7-2 does not implement policy attributes. |
| Epic 8 MCP tools | Story 7-2 tenant contract | MCP enumeration must use the canonical tenant context and return no tools for missing/mismatched tenants. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | Add a Shell-owned canonical tenant-context service instead of changing `IUserContextAccessor`. | Centralizes validation without breaking Contracts or sample code. | Add `ClaimsPrincipal` or provider-specific auth objects to Contracts. |
| D2 | Tenant equality is ordinal and case-sensitive. | Existing EventStore validation uses ordinal equality and some adopters may have case-sensitive tenant IDs. | Lowercase tenant IDs globally as architecture's early draft suggested. |
| D3 | Command/query/subscription mismatches block before side effects. | Security failures must not acquire tokens, serialize payloads, touch cache, or open SignalR connections. | Let backend reject cross-tenant requests after dispatch. |
| D4 | Keep `{projectionType}:{tenantId}` group/actor shape and reject colons in segments. | Aligns with architecture and current `EventStoreValidation` usage. | Escape colons or introduce multi-separator parsing in v1. |
| D5 | Demo/single-tenant support is explicit dev/test configuration. | Preserves Counter and v0.1 while preventing production default leaks. | Leave `"default"` / `"anonymous"` fallbacks silently enabled. |
| D6 | Cache isolation is verified by cross-tenant same-user tests. | Same user ID across organizations is common and must not collide. | Test only missing/blank tenant inputs. |
| D7 | MCP receives a contract, not an implementation, in this story. | Epic 8 owns tool server and hallucination rejection; 7-2 only prevents a parallel tenant accessor later. | Build tenant-scoped MCP enumeration now. |
| D8 | SourceTools emits tenant propagation through framework helpers, never provider-specific auth code. | Generated UI must remain portable across OIDC, SAML, GitHub OAuth, fake auth, and future hosts. | Emit direct `AuthenticationStateProvider` or `ClaimsPrincipal` logic into generated views. |
| D9 | Tenant-isolation diagnostics redact raw tenant/user values. | Tenant IDs can reveal customer identity and are adjacent to PII/secrets. | Log tenant and user IDs directly for operator convenience. |
| D10 | Stale active SignalR groups are revalidated on reconnect and tenant switch. | Blazor Server/Auto circuits can change auth state or reconnect; active group state must not outlive context validity. | Assume a circuit's tenant never changes after first subscription. |

### Library / Framework Requirements

- Use ASP.NET Core authentication outputs only through Story 7-1's bridge abstractions and the existing `IUserContextAccessor`.
- Use current .NET 10 / Blazor Auto lifetime assumptions: tenant context is request/circuit scoped, not singleton.
- Use existing SignalR group mechanics; group membership is not a security boundary by itself, so FrontComposer must validate tenant before joining or rejoining.
- Keep DAPR actor IDs as string identities. DAPR actors allow caller-provided string IDs; FrontComposer owns the safe segment policy before constructing `{projectionType}:{tenantId}`.
- Do not introduce new external packages for tenant propagation unless implementation proves a gap. This story should mostly be Shell/SourceTools/test code.

External references checked on 2026-04-30:

- Microsoft Learn: Mapping, customizing, and transforming claims in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims?view=aspnetcore-10.0
- Microsoft Learn: Overview of ASP.NET Core authentication: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0
- Microsoft Learn: Use HttpContext in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/use-http-context?view=aspnetcore-10.0
- Microsoft Learn: Manage users and groups in SignalR: https://learn.microsoft.com/en-us/aspnet/core/signalr/groups?view=aspnetcore-10.0
- Dapr Docs: Actors overview: https://docs.dapr.io/developing-applications/building-blocks/actors/

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Shell/Services/TenantContext/*` | Canonical tenant context result, validator/accessor, failure categories, and options. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreIdentity.cs` | Refactor or delegate tenant validation to the new canonical context. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs` | Pre-send command tenant validation and no-side-effect mismatch behavior. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs` | Authenticated tenant fill/validate, cache gating, and mismatch behavior. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` | Authenticated tenant validation for subscribe/rejoin/nudge paths. |
| `src/Hexalith.FrontComposer.Shell/State/ETagCache/*` | Additional tests or small helper exposure for cache isolation, ideally minimal production changes. |
| `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` | Scoped registration for the canonical tenant-context service and options validation. |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` | New HFC20xx constants only if shared diagnostic constants are needed. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/*` | Generated query/subscription tenant propagation call-site updates. |
| `samples/Counter/Counter.Web/*` | Demo mode docs/options wiring only; no real auth dependency. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Services/TenantContext/*` | Tenant context success/failure matrix. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/*` | Command/query/subscription tenant isolation tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/*` | Generated tenant propagation snapshots/approval tests. |

### Testing Standards

- P0 coverage: no backend send on tenant mismatch, no cache touch on mismatch, no group join on mismatch, stale group cleanup on tenant switch, same-user/different-tenant cache isolation, and no raw tenant/user/token/payload leakage.
- Prefer table-driven tests for tenant values: null, empty, whitespace, valid, valid mixed case, colon-containing, control-character-containing, matching, mismatched by case, `"default"`, `"anonymous"`, and sample-only `counter-demo`.
- Use the existing SignalR fault-injection harness rather than introducing sleeps or real hub servers.
- SourceTools tests should prove generated call sites, not attempt to run real IdP flows.
- Any new diagnostic ID must have emission-condition tests and release-note/catalog updates per existing diagnostic discipline.

### Scope Guardrails

Do not implement these in Story 7-2:

- Authentication provider setup, login/challenge UI, token storage, token refresh, or provider recipes. Story 7-1 owns these.
- `[RequiresPolicy]`, role mapping, command policy evaluation, authorization UI/button hiding, or policy diagnostics. Story 7-3 owns these.
- MCP server endpoints, tool execution, hallucination rejection, two-call lifecycle tools, or agent rendering. Epic 8 owns these.
- Per-tenant branding, per-tenant feature flags, tenant-specific provider selection, tenant onboarding, membership validation, account linking, or user provisioning.
- Backend EventStore authorization/validation changes outside the client envelope contract.
- New DAPR abstractions, direct infrastructure references, or custom actor placement logic.
- Raw tenant/user values in logs to make tests easier.

### Non-Goals With Owning Stories

| Non-goal | Owner |
| --- | --- |
| OIDC/SAML/GitHub provider configuration and token relay. | Story 7-1 |
| Declarative authorization policies and UI gating. | Story 7-3 |
| Tenant-scoped MCP tool enumeration implementation. | Story 8-1 / Story 8-2 |
| Tenant-aware docs cookbook and deployment recipes. | Story 9-5 |
| Diagnostic catalog/governance cleanup beyond new story-owned IDs. | Story 9-4 |
| Browser/E2E proof of auth tenant switching. | Story 10-2 or dedicated auth E2E follow-up |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Backend-side proof that EventStore rejects mismatched tenant envelopes independently of FrontComposer. | EventStore auth/isolation backlog or consumer-driven contract story |
| Full tenant-scoped MCP tool list with hallucination rejection suggestions. | Epic 8 |
| Per-tenant identity-provider routing or tenant discovery before challenge. | v1.x auth follow-up |
| Cross-browser login + tenant-switch E2E matrix. | Story 10-2 / dedicated E2E story |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-7-authentication-authorization-multi-tenancy.md#Story-7.2`] - story statement, AC foundation, FR35/NFR scope.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR35`] - tenant context propagation through command/query operations.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md`] - NFR21 JWT propagation, NFR22 tenant isolation, NFR28 cross-tenant visibility security bug, NFR102 zero PII.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Multi-Tenancy-Characterization`] - logical tenant isolation, tenant-scoped commands/queries/subscriptions/cache/MCP.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Communication-Protocols`] - EventStore REST, SignalR group, DAPR actor ID, and no-colon constraints.
- [Source: `_bmad-output/implementation-artifacts/7-1-oidc-saml-authentication-integration.md`] - authenticated `TenantId` / `UserId` seams and Story 7-2 deferrals.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L03`] - tenant/user isolation fail-closed.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review before advanced elicitation.
- [Source: `src/Hexalith.FrontComposer.Contracts/Rendering/IUserContextAccessor.cs`] - existing user/tenant contract.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreIdentity.cs`] - current tenant validation and non-colon segment guard.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs`] - command envelope tenant propagation.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`] - query tenant validation and ETag cache integration.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`] - SignalR group join/rejoin/nudge behavior.
- [Source: `src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs`] - tenant/user-scoped cache key builder.
- [Source: `samples/Counter/Counter.Web/DemoUserContextAccessor.cs`] - sample single-tenant demo seam.
- [Source: Microsoft Learn: Mapping, customizing, and transforming claims in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims?view=aspnetcore-10.0) - claims mapping and transformation concepts for Story 7-1/7-2 boundary.
- [Source: Microsoft Learn: Overview of ASP.NET Core authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0) - authentication scheme and `ClaimsPrincipal` concepts.
- [Source: Microsoft Learn: Use HttpContext in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/use-http-context?view=aspnetcore-10.0) - `HttpContext.User` as authenticated `ClaimsPrincipal`.
- [Source: Microsoft Learn: Manage users and groups in SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/groups?view=aspnetcore-10.0) - SignalR group mechanics.
- [Source: Dapr Docs: Actors overview](https://docs.dapr.io/developing-applications/building-blocks/actors/) - actor identity as caller-chosen string IDs.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-04-30: Story created via `/bmad-create-story 7-2-tenant-context-propagation-and-isolation` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### File List

(to be filled in by dev agent)
