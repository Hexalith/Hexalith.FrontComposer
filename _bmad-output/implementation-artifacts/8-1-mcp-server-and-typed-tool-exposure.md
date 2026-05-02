# Story 8.1: MCP Server & Typed Tool Exposure

Status: review (closure pass 1 applied; HIGH/CRITICAL findings resolved; remaining test/refactor items deferred to follow-up stories)

> **Epic 8** - MCP & Agent Integration. Covers **FR49**, **FR50**, **FR56**, and **NFR91**. Consumes Story **7-1** authentication seams, Story **7-2** tenant context, Story **7-3** command policy metadata, Epic 5 command/query/EventStore reliability, and the existing SourceTools parse/transform/emit pipeline. Applies lessons **L01**, **L03**, **L04**, **L05**, **L06**, **L07**, **L08**, **L10**, and **L14**.

---

## Executive Summary

Story 8-1 introduces the first MCP surface without creating a second domain API:

- Add a new `Hexalith.FrontComposer.Mcp` package that hosts an in-process Model Context Protocol server beside the composition shell.
- Emit deterministic MCP command/resource metadata from the same SourceTools domain IR that feeds web forms, generated renderers, registration, and test fixtures.
- Expose `[Command]` types as typed MCP tools and `[Projection]` types as MCP resources with names, labels, descriptions, and schemas generated from the same contract source as the web surface.
- Dispatch MCP command calls through the existing `ICommandService` and query projection resources through the existing `IQueryService`; do not create a parallel backend client.
- Support machine-to-machine MCP authentication using API key or client credentials that resolves to a principal/JWT containing the canonical `TenantId` claim from Story 7-2.
- Carry Story 7-3 authorization policy metadata into the MCP manifest for future tenant-scoped enumeration/execution, while full hallucination rejection, tenant-scoped listings, two-call lifecycle tooling, and rich Markdown projection rendering remain owned by later Epic 8 stories.

---

## Story

As a developer,
I want my domain model automatically exposed as typed MCP tools via an in-process server alongside the composition shell,
so that LLM agents can discover and call my commands and queries with the same type safety as the web surface.

### Adopter Job To Preserve

An adopter should be able to register FrontComposer once for a bounded context, enable the MCP package with one hosting extension, and receive typed agent-facing tools/resources generated from the same command and projection metadata already used by the web shell. The adopter must not hand-maintain a separate OpenAPI/controller/tool schema that can drift from generated web forms.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A FrontComposer-registered domain contains `[Command]` and `[Projection]` annotated types | The host enables MCP and starts the composition shell | An in-process MCP server is hosted and reachable by MCP clients through the configured transport. |
| AC2 | A `[Command]` type is discovered by SourceTools | The MCP manifest is emitted | The command appears as exactly one MCP tool with a deterministic name, title, description, input schema, and optional output schema. |
| AC3 | A `[Projection]` type is discovered by SourceTools | The MCP manifest is emitted | The projection appears as exactly one MCP resource or resource template with deterministic URI, name, title, description, and schema metadata. |
| AC4 | A command has user-entered properties and derivable properties | The tool input schema is inspected | User-entered parameters match the command model exactly, while derivable `TenantId`, `UserId`, message/idempotency, and system-owned fields are not exposed as user-editable tool input. |
| AC5 | A command property has BCL display metadata, description metadata, nullable type information, enum values, or known validation constraints | The tool schema is emitted | The schema carries parameter type, required/optional status, enum values, descriptions, labels, and constraints from the same SourceTools parse/transform path used by command forms. |
| AC6 | MCP tool descriptions are generated | The command or projection has `[Display(Name)]` metadata | Description/title label resolution uses `[Display(Name)]` first, then humanized CamelCase, then raw member/type name, matching the web-surface chain. |
| AC7 | The MCP server is registered | The developer inspects setup code | The host can enable MCP through a small DI/endpoint extension such as `AddFrontComposerMcp(...)` / `MapFrontComposerMcp(...)`; normal domain registration remains the source of domain metadata. |
| AC8 | The MCP server runs with registered commands | Integration tests call at least three generated command tools | Calls flow through existing FrontComposer command dispatch seams and produce acknowledged command results without bypassing validation, tenant propagation, token relay, idempotency, or telemetry policies. |
| AC9 | The MCP server runs with registered projections | Integration tests read at least two generated projection resources | Reads flow through existing FrontComposer query seams and return deterministic text/Markdown or structured content suitable for MCP clients. |
| AC10 | An unknown MCP tool name is called | The request reaches Story 8-1 code | It is rejected before backend dispatch with a protocol-appropriate error; full closest-match suggestion and tenant-scoped tool listing are explicitly deferred to Story 8-2. |
| AC11 | An MCP client authenticates without browser interaction | API key or client-credentials authentication succeeds | The server resolves an authenticated agent principal/JWT with a `TenantId` claim scoped to the authorized tenant and does not invoke web OIDC/SAML redirect flows. |
| AC12 | A command has Story 7-3 policy metadata | MCP metadata is emitted | The policy identifier is carried as configuration metadata for future Story 8-2 enumeration/execution checks, but Story 8-1 does not implement complete MCP authorization policy filtering. |
| AC13 | The generated MCP manifest is inspected | The manifest content is compared to generated web metadata | The command/projection schema source is single-source and deterministic; no runtime reflection over arbitrary loaded assemblies is used as the canonical schema. |
| AC14 | Tool/resource names are generated from domain types | Two domain types would collide after cosmetic normalization | SourceTools detects the collision or uses fully qualified/disambiguated names so the MCP server never exposes non-deterministic duplicate names. |
| AC15 | The MCP server is unavailable, auth fails, schema validation fails, or command/query dispatch throws | The MCP client receives a response | The response is bounded, sanitized, protocol-appropriate, and contains no raw tokens, claims, tenant IDs, user IDs, command payload secrets, stack traces, or provider internals. |
| AC16 | The story completes | A future Epic 8 story continues hardening | Story 8-1 leaves explicit extension points for tenant-scoped enumeration, hallucination suggestion responses, two-call lifecycle subscription, skill corpus resources, schema fingerprints, and richer Markdown renderers without requiring a redesign. |

---

## Tasks / Subtasks

- [x] T1. Create the MCP package boundary (AC1, AC7, AC11, AC15)
  - [x] Add `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj` and `tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj`.
  - [x] Reference `Hexalith.FrontComposer.Contracts` and the minimum runtime packages needed for MCP hosting; do not make Contracts or SourceTools depend on MCP SDK packages.
  - [x] Add package references through central package management only, likely `ModelContextProtocol.AspNetCore` for HTTP-hosted server support and `ModelContextProtocol` only if non-HTTP hosting is chosen.
  - [x] Register the new projects in `Hexalith.FrontComposer.sln` and keep test categorization consistent with existing Contracts/Shell/SourceTools test projects.

- [x] T2. Define MCP manifest contracts (AC2-AC6, AC12-AC16)
  - [x] Add dependency-light metadata records such as `McpCommandDescriptor`, `McpResourceDescriptor`, `McpParameterDescriptor`, and `McpManifest` in the MCP package or Contracts only if a stable public contract is required.
  - [x] Keep generated metadata type-only and config-only: command/projection FQN, bounded context, display title, parameter schema, optional authorization policy, and resource URI template.
  - [x] Forbid runtime values in descriptors: tenant/user identifiers, JWTs, claims, command payload values, query results, localized resolved strings, service instances, `ClaimsPrincipal`, or `RenderContext`.
  - [x] Include `AuthorizationPolicyName` when present from Story 7-3 metadata; do not interpret it here beyond carrying metadata.

- [x] T3. Extend SourceTools with MCP manifest emission (AC2-AC6, AC12-AC14)
  - [x] Extend parse/transform models only where needed to expose command and projection metadata already known to SourceTools.
  - [x] Emit a deterministic manifest artifact for all discovered command/projection types in the compilation.
  - [x] Reuse label/description logic from web emit paths; do not fork a second humanizer or display-metadata parser.
  - [x] Generate MCP-safe tool names using a documented rule. Prefer disambiguated names based on bounded context and type name; detect collisions per L04 before runtime.
  - [x] Add SourceTools tests for deterministic ordering, collision handling, optional policy metadata, no runtime values, command parameter schema, projection resource metadata, and web/MCP label parity.

- [x] T4. Map command tool input schema (AC2, AC4, AC5, AC8, AC13)
  - [x] Build JSON Schema object output for command user-entered properties, using nullable/required state, enum members, numeric/date/string categories, descriptions, display labels, and known FluentValidation-derived constraints where available.
  - [x] Emit `additionalProperties: false` for command tools unless a deliberate extension point is documented.
  - [x] Do not expose derivable infrastructure fields as model-controlled input. Tenant and user context come from authenticated agent context, not from tool arguments.
  - [x] Reject spoofed derivable fields such as `TenantId`, `UserId`, message/idempotency identifiers, correlation IDs, and system-owned values before command construction or dispatch.
  - [x] Canonicalize tool arguments into an immutable invocation envelope before validation; reject duplicate, case-variant, oversized, or unsupported nested JSON members before command construction.
  - [x] Define deterministic behavior for unsupported CLR/type categories instead of inventing adapter-side schema semantics.
  - [x] Keep validation constraints generated from the same source as web form validation; if a constraint cannot be represented in JSON Schema, include bounded tool-description guidance and server-side validation.

- [x] T5. Host and route the MCP server (AC1, AC7, AC10, AC15)
  - [x] Add hosting extensions such as `AddFrontComposerMcp` and `MapFrontComposerMcp`.
  - [x] Use the official MCP C# SDK server APIs rather than hand-rolling JSON-RPC framing.
  - [x] Support the configured transport selected for v1; HTTP/SSE or streamable HTTP should live behind the MCP package and not leak into Contracts.
  - [x] Implement unknown tool/resource handling as a bounded protocol error in Story 8-1. Closest-match suggestion, tenant-scoped list, and self-correction copy are Story 8-2.
  - [x] Add startup validation for duplicate descriptors, missing manifest registration, unsupported transport config, and sanitized diagnostic output.
  - [x] Keep MCP SDK transport/protocol types behind the MCP package adapter boundary; generated descriptors and SourceTools-facing contracts must remain SDK-neutral.

- [x] T6. Implement MCP command invocation adapter (AC8, AC10-AC12, AC15)
  - [x] Route generated tool calls to the existing `ICommandService.DispatchAsync<TCommand>` path or its lifecycle-aware companion when already available.
  - [x] Preserve EventStore behavior from Epic 5: tenant propagation, token relay, idempotency/message ID discipline, ETag/cache non-interference, retry/degraded classification, and sanitized telemetry.
  - [x] Validate tool arguments against generated schema before constructing the command. Rejections must occur before `ICommandService`, EventStore serialization, token acquisition, HTTP send, lifecycle state mutation, or SignalR/cache side effects.
  - [x] Return a minimal acknowledged result shape with command/message/correlation data already available from `CommandResult`. Full two-call lifecycle subscription remains Story 8-3.
  - [x] Carry but do not fully enforce Story 7-3 policy metadata unless the existing shared evaluator is ready; record any gap as a deferred decision rather than adding a parallel policy engine.
  - [x] Treat timeout, cancellation, command rejection, auth failure, validation failure, and downstream exception outcomes as distinct sanitized protocol categories while preserving request cancellation tokens through dispatch.

- [x] T7. Implement projection resource adapter (AC3, AC9, AC15)
  - [x] Route projection reads to the existing `IQueryService.QueryAsync<T>` path with tenant context from authenticated agent state.
  - [x] Return deterministic text/Markdown or structured content for at least Default/ActionQueue-style table output sufficient for AC9.
  - [x] Define resource vs. resource-template behavior, URI shape, query parameter binding, validation failure mapping, pagination or size limits, and content type before implementation.
  - [x] Keep rich role-specific Markdown rendering, status cards, timelines, empty-state suggestions, and badge text polish scoped to Story 8-4 unless simple reuse already exists.
  - [x] Do not let clients supply raw tenant IDs to query resources; tenant comes from authenticated context.
  - [x] Enforce deterministic response size bounds for projection resources; any truncation or paging hint must be sanitized and must not reveal hidden tenant/resource existence.

- [x] T8. Add machine-to-machine authentication seam (AC11, AC12, AC15)
  - [x] Add options for API key or client credentials and document which one is first-class in v1.
  - [x] Normalize successful authentication into the existing Story 7-1 / Story 7-2 principal and tenant-context seams.
  - [x] Define precedence and failure behavior for API key/client-credentials identities, JWT/principal claims, and any attempted tenant/user values in tool/resource input; ambiguous or mismatched values fail closed.
  - [x] Fail closed when auth is absent, token/API key is invalid, tenant claim is missing/empty/whitespace, policy metadata requires a future check, or auth state cannot be resolved.
  - [x] Ensure auth failures do not trigger browser redirects and do not leak token, provider, client secret, tenant, or user values in responses or logs.

- [x] T9. Tests and verification (AC1-AC16)
  - [x] Contracts/package-boundary tests proving MCP dependencies do not enter Contracts or SourceTools public surfaces.
  - [x] SourceTools manifest snapshot tests for commands, projections, labels, descriptions, enum/nullable/required schema, policy metadata, collision diagnostics, and no-runtime-value descriptors.
  - [x] MCP hosting tests proving DI registration, endpoint mapping, duplicate descriptor rejection, unknown tool/resource behavior, and sanitized diagnostics.
  - [x] Command adapter tests covering three sample commands: valid call acknowledged, invalid arguments rejected before side effects, unauthorized/unauthenticated rejected before side effects, and command-service rejection translated to protocol-safe output.
  - [x] Projection adapter tests covering two sample projections, tenant context injection, query-service invocation, Markdown/text determinism, and no raw tenant/user leakage.
  - [x] Determinism/golden tests covering descriptor ordering, stable tool/resource identifiers, URI templates, JSON Schema serialization, localization-neutral fallback labels, optional policy metadata, and repeatable output across builds.
  - [x] Auth and tenant-context tests covering missing, malformed, expired, wrong-tenant, ambiguous, and spoofed tenant/user inputs for both API key and client-credentials paths.
  - [x] Invocation-envelope tests covering duplicate JSON property names, case-variant spoofing, unsupported nested payloads, oversized arguments/resources, stale manifest identifiers, and cancellation/timeout mapping.
  - [x] Adapter boundary tests proving request IDs and cancellation flow through, invalid arguments fail before side effects, unknown tools/resources/templates return deterministic errors, stale client manifest names do not dispatch, and command/query adapters preserve existing validation, tenant scoping, authorization hooks, and domain error mapping.
  - [x] Redaction tests with JWT-like strings, API keys, claim values, role names, tenant IDs, user IDs, command payload fragments, query filters, exception messages, and provider internals.
  - [x] Policy metadata guardrail tests proving the descriptor carries Story 7-3 policy identifiers while 8-1 does not claim full tenant-scoped authorization filtering.
  - [x] Regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.
  - [x] Targeted tests: `tests/Hexalith.FrontComposer.SourceTools.Tests`, `tests/Hexalith.FrontComposer.Mcp.Tests`, plus Shell/EventStore tests only if shared runtime seams are changed.

---

## Dev Notes

### Existing State To Preserve

| File / Area | Current state | Preserve / Change |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Attributes/CommandAttribute.cs` | Dependency-free command marker. | Do not change command semantics; MCP consumes generated command metadata. |
| `src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionAttribute.cs` | Dependency-free projection marker. | Do not add MCP SDK dependencies to Contracts. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs` | Projection and command IR with display, bounded context, properties, destructive and derivable metadata. | Extend only where needed; keep equality/cache keys deterministic. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererModel.cs` | Web renderer model carries command FQN, density, display label, properties, destructive metadata. | Reuse the same display and property metadata for MCP; do not fork label rules. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModel.cs` and `ColumnModel.cs` | Projection transform model carries columns, headers, role, empty-state CTA and formatting metadata. | Reuse for projection resource metadata; rich Markdown role rendering can be deferred to Story 8-4. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs` | Per-type `DomainManifest` lists commands/projections for registry. | MCP manifest may consume or complement this, but should not make registry order non-deterministic. |
| `src/Hexalith.FrontComposer.Contracts/Communication/ICommandService.cs` | Dispatch abstraction returns `CommandResult` or throws `CommandRejectedException`. | MCP command tools must go through this seam; do not create a second backend dispatch client. |
| `src/Hexalith.FrontComposer.Contracts/Communication/IQueryService.cs` and `QueryRequest.cs` | Projection query abstraction includes `TenantId`, filters, sort, ETag, cache discriminator, and EventStore query fields. | MCP resources must build requests through this contract with tenant from auth context. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/IRenderer.cs` | Renderer abstraction already anticipates `string` output for MCP agents. | Do not redesign rendering abstraction in 8-1; leave full multi-surface abstraction to Story 8-6 unless a minimal adapter is enough. |
| Story `7-3-command-authorization-policies` | Command policy metadata is intended for future MCP enumeration/execution. | Carry policy metadata forward; do not invent a separate MCP-only policy attribute. |

### Architecture Contracts

- `Hexalith.FrontComposer.Mcp` sits after Shell/EventStore in the package graph and may reference Contracts plus runtime hosting dependencies. Contracts and SourceTools must not reference MCP SDK packages.
- The MCP package must depend on stable FrontComposer application abstractions (`ICommandService`, `IQueryService`, generated descriptor registry, auth/tenant context services) rather than Shell component internals, so the same package can run in web, worker, test-host, or future gateway hosts.
- MCP schema generation must be SourceTools-driven, deterministic, and compile-time owned. Runtime reflection may instantiate generated adapters when needed, but it is not the source of schema truth.
- MCP descriptors may only mechanically transform SourceTools IR into protocol-safe names, URIs, and JSON Schema. They must not redefine command parameters, projection shape, display metadata, policy metadata, or validation semantics outside the existing parse/transform source of truth.
- Commands are tools. Projections are resources/resource templates. The naming scheme must be unique within a server and must satisfy MCP tool-name character guidance: ASCII letters/digits plus `_`, `-`, and `.`; no spaces or commas.
- Tenant context is authenticated context, not tool input. Missing tenant/user context fails closed per L03.
- When API key or client-credentials auth, JWT/principal claims, and command/query context disagree on tenant identity, the authenticated canonical tenant context wins only when it is singular and non-empty; any absent, ambiguous, spoofed, or mismatched tenant/user input fails before command construction, query construction, dispatch, or side effects.
- Story 8-1 ships the MCP spine and minimal runnable calls. Story 8-2 owns closest-match hallucination suggestions and tenant-scoped tool enumeration. Story 8-3 owns the two-call lifecycle subscription tool. Story 8-4 owns role-rich Markdown projection rendering. Story 8-5 owns skill corpus payloads. Story 8-6 owns schema fingerprints/version negotiation.
- Official MCP C# SDK usage is preferred. As of 2026-05-01, the official SDK docs list C# as a Tier 1 SDK and the official C# repository publishes `ModelContextProtocol.Core`, `ModelContextProtocol`, and `ModelContextProtocol.AspNetCore`, with `ModelContextProtocol.AspNetCore` intended for HTTP-based MCP servers.

### Hardened Party-Mode Clarifications

- **Determinism:** Generated descriptor ordering, protocol identifiers, URI templates, JSON Schema serialization, policy metadata, and fallback labels must be stable across repeated builds for the same SourceTools IR. Current UI culture, tenant/user state, loaded service instances, runtime authorization decisions, and assembly enumeration order must not affect generated descriptors.
- **Command input schema:** Story 8-1 supports the command property categories already represented by SourceTools IR: strings, booleans, numeric types, date/time-like values, enums, nullable/required state, arrays or nested objects only when the existing command-form model already represents them, and known validation constraints when available. Unsupported CLR/type categories fail at build/manifest generation with a deterministic diagnostic or explicit unsupported-schema marker; they are not guessed in the MCP adapter.
- **Projection resources:** Resource identifiers and templates use the story's documented URI rules, bind only non-secret protocol parameters, and validate query/paging inputs before calling `IQueryService`. Large result limits, pagination defaults, and content type must be deterministic and documented by the adapter; rich role-specific Markdown and UI-template rendering remain Story 8-4.
- **Adapter response mapping:** MCP request IDs and cancellation tokens must flow through the adapter. Validation errors, auth failures, unknown tool/resource/template names, domain rejections, timeouts, cancellation, and downstream exceptions map to bounded protocol errors with stable error categories and correlation IDs where already available. Error responses must not include stack traces, internal type names, tenant/resource existence hints, raw exception text, claims, tokens, tenant IDs, user IDs, payload fragments, or provider internals.
- **Policy metadata non-goal:** Story 8-1 emits and carries the existing Story 7-3 policy identifier metadata, but MCP listing/discovery in this story must not imply that full tenant-scoped authorization filtering is implemented. Runtime list/call filtering, closest-match suggestions, hidden unauthorized tools, and semantic hallucination handling are Story 8-2.
- **Localization contract:** MCP names, URIs, schema property names, and protocol identifiers are invariant technical contracts. Human-readable titles and descriptions should preserve SourceTools display/description metadata and localization keys where already present, with deterministic fallback text; they must not depend on per-request user culture or contain hard-coded adapter-only copy when IR metadata exists.

### Advanced Elicitation Clarifications

- **Invocation envelope boundary:** MCP requests should be normalized once into an immutable envelope containing protocol name, request ID, authenticated principal handle, canonical tenant context, raw argument object, cancellation token, and descriptor reference. Spoofed derivable fields, duplicate or case-variant JSON names, unsupported nested payloads, and oversize arguments fail before command construction, query construction, telemetry enrichment, EventStore serialization, token relay, cache mutation, or SignalR side effects.
- **Descriptor provenance and stale clients:** Generated MCP descriptors need stable provenance metadata such as bounded context, generator/source version, descriptor kind, protocol identifier, and optional policy identifier. A client calling a removed or stale tool/resource name receives the same bounded unknown-name category as any unknown call; Story 8-1 must not infer a replacement or leak that a similar unauthorized descriptor exists.
- **SDK volatility containment:** The official MCP C# SDK is the transport/server implementation detail, not the schema source of truth. Keep protocol DTO conversion inside `Hexalith.FrontComposer.Mcp` so SourceTools manifest snapshots, generated descriptor contracts, and tests do not churn when SDK transport types change.
- **Failure taxonomy:** Auth failure, tenant ambiguity, validation failure, unsupported schema, unknown name, stale manifest name, command rejection, query rejection, timeout, cancellation, duplicate descriptor, missing manifest, and unexpected downstream exception must map to deterministic sanitized categories. No category may echo raw exception messages, internal type names, provider details, tenant/user identifiers, claims, tokens, payload fragments, or resource-existence hints.
- **Minimal runnable spine:** The first implementation should be the smallest vertical slice that proves manifest generation, hosting, M2M auth, three command tool calls, two projection resource reads, no-side-effect rejection, and redaction. Rich listing, suggestions, lifecycle subscriptions, skill corpus resources, schema fingerprints, and role-polished Markdown stay deferred unless they are required to keep this spine coherent.
- **Bounded runtime state:** Descriptor registries, schema caches, response buffers, and resource render buffers must be immutable after startup or bounded by explicit options. Do not add request-time unbounded dictionaries, per-agent descriptor accumulation, or culture/tenant-specific manifest caches in 8-1.

### Proposed Minimal Flow

1. SourceTools discovers command/projection domain types already used by web generation.
2. SourceTools emits a deterministic MCP manifest containing tool/resource descriptors and parameter schemas.
3. Host calls `AddFrontComposerMcp` and `MapFrontComposerMcp`; the MCP package loads generated descriptors through DI/assembly discovery.
4. MCP client authenticates using API key or client credentials; the server resolves principal plus canonical tenant context.
5. `tools/list` exposes generated command tools allowed by this story's minimal visibility rules.
6. `tools/call` validates arguments, builds command instance, injects derivable context, and dispatches through `ICommandService`.
7. `resources/list` / `resources/read` exposes projection resources and queries through `IQueryService`.
8. Unknown tool/resource, validation failure, auth failure, and backend rejection map to sanitized MCP responses.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Stories 1-2, 1-4, 1-5 | Story 8-1 | Attribute and SourceTools IR remain the single source for command/projection metadata. |
| Stories 2-2 through 2-5 | Story 8-1 | Command form property classification, derivable fields, validation, destructive semantics, and lifecycle ordering must not diverge on MCP. |
| Epic 5 | Story 8-1 | EventStore command/query clients, token relay, idempotency, ETag/cache discipline, SignalR side effects, and telemetry remain the dispatch/query path. |
| Stories 7-1 and 7-2 | Story 8-1 | Machine agent auth resolves into authenticated principal and canonical tenant context; no browser redirect flow. |
| Story 7-3 | Story 8-1 and 8-2 | Optional command policy metadata is available for MCP descriptors and future enumeration/execution filtering. |
| Story 8-1 | Stories 8-2 through 8-6 | MCP manifest, server hosting, descriptor registry, and adapter seams must be extensible without replacing the package. |

### Binding Decisions

| Decision | Rationale |
| --- | --- |
| D1. Create `Hexalith.FrontComposer.Mcp` as a new package. | Matches the architecture package graph and prevents MCP dependencies from leaking into Contracts/Shell/SourceTools public boundaries. |
| D2. Use official MCP C# SDK server APIs. | Avoids hand-rolled JSON-RPC/transport behavior and tracks the active MCP protocol ecosystem. |
| D3. SourceTools emits the schema of record. | Prevents web/MCP drift and avoids runtime reflection as the canonical schema. |
| D4. Tool/resource descriptors are type/config metadata only. | Prevents tenant/user/principal/payload leakage and keeps generated manifests deterministic. |
| D5. Tenant and user context are derivable, never tool arguments. | Applies L03 and prevents agents from choosing their own tenant/user scope. |
| D6. Unknown tool calls are rejected in 8-1, but suggestions/listing are deferred. | Satisfies basic safety without stealing Story 8-2's hallucination-rejection scope. |
| D7. Return minimal acknowledged command result in 8-1. | Full lifecycle polling/subscription is Story 8-3; 8-1 should not build a partial second state machine. |
| D8. Projection output is deterministic but minimal. | Story 8-4 owns rich role-specific Markdown rendering. |
| D9. Policy metadata is carried, not reinterpreted. | Prevents a parallel MCP authorization model before Story 8-2 consumes Story 7-3 decisions. |
| D10. Collisions are compile-time diagnostics or disambiguated names. | Applies L04 and prevents nondeterministic MCP listings. |
| D11. MCP response/log redaction follows Epic 7 rules. | Agent surfaces are security-sensitive and must not leak tokens, claims, tenant IDs, or payload fragments. |
| D12. Descriptor registry is bounded and immutable after startup. | Applies L14; no unbounded per-request cache or mutable descriptor accumulation. |

### Name And URI Rules

- Tool names should be stable and MCP-safe. Use a documented form such as `{boundedContext}.{CommandTypeName}.Execute` after sanitizing to `[A-Za-z0-9_.-]`.
- Projection resource URIs should be stable and non-secret, such as `frontcomposer://{boundedContext}/projections/{ProjectionTypeName}` or an SDK-recommended equivalent.
- If display names collide but FQNs differ, display titles may repeat but protocol identifiers must remain unique.
- Collision detection covers command tool names, projection resource identifiers, resource templates, generated aliases if any, bounded-context prefixes, and case-insensitive protocol lookup behavior. Collisions either produce deterministic disambiguated names or fail at compile/startup before the MCP server starts accepting traffic.
- Names must not include tenant identifiers, environment names, user identifiers, role/claim values, localized strings, or sample payload values.

### Security And Redaction Matrix

| Channel | Allowed values | Forbidden values |
| --- | --- | --- |
| MCP tool/resource metadata | Tool/resource name, title, display label, description, JSON schema, bounded context, optional policy identifier. | Tenant IDs, user IDs, claims, roles, tokens, API keys, command payload values, query result values, provider internals. |
| MCP call response | Acknowledged command/message/correlation identifiers, sanitized validation/business error category, deterministic projection content. | Stack traces, token material, raw principal data, raw tenant/user values, secret command fields, EventStore auth headers. |
| Logs/telemetry | Event name, command/projection type, bounded context, sanitized outcome category, correlation ID, MCP surface marker. | JWT/API key/client secret, claims, roles, tenant/user identifiers unless already represented by existing sanitized markers, payload fragments, raw exception text. |
| Generated manifest | Type/config metadata and schema. | Runtime user/session data, service instances, `ClaimsPrincipal`, tenant/user context, current authorization decisions. |

### Scope Guardrails

Do not implement these in Story 8-1:

- Closest-match hallucination suggestions or tenant-scoped full tool listing beyond basic rejection. Owner: Story 8-2.
- Full tenant-scoped enumeration and command policy enforcement for every MCP list/call edge. Owner: Story 8-2, consuming Story 7-3 metadata.
- Policy-based hiding of tools/resources or fuzzy/semantic hallucination correction for unauthorized or misspelled names. Owner: Story 8-2.
- `lifecycle/subscribe`, guaranteed terminal state polling, or the full two-call lifecycle contract. Owner: Story 8-3.
- Role-rich Markdown rendering for Default/ActionQueue/StatusOverview/Timeline, empty-state suggestions, badge text mapping, and locale formatting parity beyond minimal deterministic output. Owner: Story 8-4.
- Versioned skill corpus resources. Owner: Story 8-5.
- Schema hash fingerprints, migration delta diagnostics, or full renderer abstraction redesign. Owner: Story 8-6.
- Browser OIDC/SAML/GitHub/Google redirect/challenge flows. Owner: Story 7-1.
- New tenant normalization or tenant mismatch policy. Owner: Story 7-2.
- New command policy language, multi-policy composition, or admin policy UI. Owner: Story 7-3 or later authorization follow-up.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Tenant-scoped tool/resource enumeration with closest-match suggestions and hidden unauthorized tools. | Story 8-2 |
| Full command lifecycle subscription tool and terminal-state guarantees. | Story 8-3 |
| Rich Markdown projection rendering and role-specific agent presentation. | Story 8-4 |
| MCP-discoverable skill corpus. | Story 8-5 |
| Schema fingerprints and version negotiation. | Story 8-6 |
| Deep agent-surface E2E and security benchmark gates. | Story 10-2 / Story 10-6 |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-8-mcp-agent-integration.md#Story-8.1`] - story statement, AC foundation, FR49/FR50/FR56/NFR91 scope.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR49-FR56`] - typed MCP server, tool parameters, projection resources, tenant-scoped future enumeration, shared typed contracts.
- [Source: `_bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md#Package-family`] - `Hexalith.FrontComposer.Mcp` package and registration ceremony.
- [Source: `_bmad-output/planning-artifacts/prd/user-journeys.md#Journey-5`] - LLM agent command-use journey and hallucination rejection context.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Package-dependency-graph`] - package layering and MCP package position.
- [Source: `_bmad-output/planning-artifacts/architecture.md#MCP-Interaction-Model`] - commands as tools, projections as resources, lifecycle and hallucination-rejection future flow.
- [Source: `_bmad-output/implementation-artifacts/7-1-oidc-saml-authentication-integration.md`] - authenticated principal and non-browser auth boundary.
- [Source: `_bmad-output/implementation-artifacts/7-2-tenant-context-propagation-and-isolation.md`] - canonical tenant context and fail-closed tenant handling.
- [Source: `_bmad-output/implementation-artifacts/7-3-command-authorization-policies.md`] - command policy metadata and future MCP authorization handoff.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L03`] - tenant/user fail-closed guard.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L04`] - generated name collision detection.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L14`] - bounded cache/registry discipline.
- [Source: `src/Hexalith.FrontComposer.Contracts/Attributes/CommandAttribute.cs`] - command marker.
- [Source: `src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionAttribute.cs`] - projection marker.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs`] - command/projection parse IR.
- [Source: `src/Hexalith.FrontComposer.Contracts/Communication/ICommandService.cs`] - command dispatch seam.
- [Source: `src/Hexalith.FrontComposer.Contracts/Communication/IQueryService.cs`] - projection query seam.
- [Source: `src/Hexalith.FrontComposer.Contracts/Rendering/IRenderer.cs`] - existing string renderer direction for MCP agents.
- [Source: Model Context Protocol tools specification 2025-11-25](https://modelcontextprotocol.io/specification/2025-11-25/server/tools) - tool names, schemas, result and security requirements.
- [Source: Model Context Protocol resources specification 2025-11-25](https://modelcontextprotocol.io/specification/2025-11-25/server/resources) - resource model and URI-based resource reads.
- [Source: Official MCP SDK list](https://modelcontextprotocol.io/docs/sdk) - C# SDK Tier 1 status and SDK capabilities.
- [Source: Official MCP C# SDK overview](https://csharp.sdk.modelcontextprotocol.io/index.html) - official .NET SDK and package family.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj -p:UseSharedCompilation=false -v:minimal` - 9 passed.
- `dotnet test tests\Hexalith.FrontComposer.SourceTools.Tests\Hexalith.FrontComposer.SourceTools.Tests.csproj -p:UseSharedCompilation=false -v:minimal` - 600 passed.
- `dotnet test Hexalith.FrontComposer.sln -p:UseSharedCompilation=false -v:minimal` - 2309 passed.
- `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false -v:minimal` - 0 warnings, 0 errors.

### Completion Notes List

- 2026-05-01: Story created via `/bmad-create-story 8-1-mcp-server-and-typed-tool-exposure` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-02: Implemented the Story 8-1 MCP spine: SDK-neutral manifest contracts in Contracts, SourceTools deterministic manifest emission, MCP hosting/route extensions using the official MCP .NET SDK, command and projection adapters through existing `ICommandService` / `IQueryService` seams, fail-closed API-key/claims agent context resolution, sanitized failure categories, and focused boundary/hosting/invocation/projection/generator tests.
- 2026-05-02: Updated SourceTools integration and caching expectations for the new generated MCP manifest artifact while preserving existing Level 2 template manifest behavior.

### Party-Mode Review

- **Date/time:** 2026-05-01T11:14:48+02:00
- **Selected story key:** `8-1-mcp-server-and-typed-tool-exposure`
- **Command/skill invocation used:** `/bmad-party-mode 8-1-mcp-server-and-typed-tool-exposure; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- **Findings summary:** The review found that the story was ready in product scope but needed sharper pre-dev contracts for deterministic SourceTools-driven MCP descriptors, stable schema serialization, tenant/auth precedence, spoofed derivable field rejection, adapter error mapping, policy-metadata non-goals, resource/template boundaries, and testable sanitization.
- **Changes applied:** Added stable-abstraction dependency guidance; restricted MCP descriptor generation to mechanical SourceTools IR transforms; clarified tenant/auth precedence and fail-closed mismatches; added hardened party-mode clarifications for determinism, command input schema, projection resources, adapter response mapping, policy metadata non-goals, and localization contracts; tightened name/URI collision domains; expanded T4/T7/T8/T9 with spoofed-field, unsupported-type, resource-template, auth, determinism, adapter-boundary, and policy guardrail tests.
- **Findings deferred:** Full tenant-scoped listing/filtering, policy-based hiding, closest-match/hallucination suggestions, lifecycle subscription, rich role-specific Markdown rendering, skill corpus resources, schema fingerprints/version negotiation, and new authorization policy language remain deferred to their owning Epic 8 / Epic 7 follow-up stories.
- **Final recommendation:** ready-for-dev

### Advanced Elicitation

- **Date/time:** 2026-05-01T12:03:06+02:00
- **Selected story key:** `8-1-mcp-server-and-typed-tool-exposure`
- **Command/skill invocation used:** `/bmad-advanced-elicitation 8-1-mcp-server-and-typed-tool-exposure`
- **Batch 1 method names:** Security Audit Personas; Red Team vs Blue Team; Pre-mortem Analysis; Failure Mode Analysis; Self-Consistency Validation.
- **Reshuffled Batch 2 method names:** First Principles Analysis; Architecture Decision Records; Chaos Monkey Scenarios; Occam's Razor Application; Hindsight Reflection.
- **Findings summary:** The elicitation found that the story had strong architectural boundaries but needed executable pre-dev detail for immutable invocation normalization, stale client behavior, SDK volatility containment, deterministic failure taxonomy, bounded runtime state, and proof that invalid MCP input cannot reach command/query side effects.
- **Changes applied:** Added an `Advanced Elicitation Clarifications` section; strengthened T4 with immutable invocation envelope and duplicate/case-variant/oversize argument rejection; strengthened T5 with SDK adapter containment; strengthened T6 with explicit sanitized outcome categories and cancellation propagation; strengthened T7 with deterministic resource response bounds; strengthened T9 with invocation-envelope tests for stale identifiers, oversized payloads, unsupported nesting, and timeout/cancellation mapping.
- **Findings deferred:** Full tenant-scoped listing and hiding, closest-match suggestions, lifecycle subscription, rich role-specific Markdown rendering, skill corpus resources, schema fingerprints/version negotiation, and new authorization policy semantics remain deferred to their existing Epic 8 / Epic 7 owners.
- **Final recommendation:** ready-for-dev

### File List

- `Directory.Packages.props`
- `Hexalith.FrontComposer.sln`
- `_bmad-output/implementation-artifacts/8-1-mcp-server-and-typed-tool-exposure.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Contracts/Mcp/McpCommandDescriptor.cs`
- `src/Hexalith.FrontComposer.Contracts/Mcp/McpManifest.cs`
- `src/Hexalith.FrontComposer.Contracts/Mcp/McpParameterDescriptor.cs`
- `src/Hexalith.FrontComposer.Contracts/Mcp/McpResourceDescriptor.cs`
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpEndpointRouteBuilderExtensions.cs`
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpException.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpFailureCategory.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResource.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResult.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpTool.cs`
- `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj`
- `src/Hexalith.FrontComposer.Mcp/HttpFrontComposerMcpAgentContextAccessor.cs`
- `src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpAgentContextAccessor.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`
- `src/Hexalith.FrontComposer.Mcp/McpJsonSchemaBuilder.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/McpManifestTransform.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/BoundaryTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/GlobalUsings.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj`
- `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/ManifestTransformTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/SourceToolCompilationHelper.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Caching/IncrementalCachingTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/CounterDomainIntegrationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs`
- `src/Hexalith.FrontComposer.Contracts/Mcp/GeneratedManifestAttribute.cs` *(closure pass 1)*
- `src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpCommandPolicyGate.cs` *(closure pass 1)*
- `tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs` *(closure pass 1)*
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerCoverageTests.cs` *(closure pass 1)*
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderCoverageTests.cs` *(closure pass 1)*

### Change Log

- 2026-05-02: Added MCP server package, descriptor contracts, SourceTools manifest generation, command/projection adapters, M2M auth context seam, tests, and solution/package registration. Story status moved to review.
- 2026-05-02: Code review pass 1 completed via `/bmad-code-review 8-1`. Three adversarial layers (Acceptance Auditor, Blind Hunter, Edge Case Hunter) raised ~120 raw findings. After dedup: 8 decision-needed + 55 patch + 8 defer + 6 dismissed. Findings recorded in `### Review Findings` below.
- 2026-05-02: Code review closure pass 1 applied. Resolved 8/8 decision-needed (DN-8-1-1-1..8) and 35/55 patches (all HIGH + most MEDIUM closed). Added `IFrontComposerMcpCommandPolicyGate` fail-closed contract, `[GeneratedManifest]` discovery, `MalformedRequest`/`PolicyGateMissing` failure categories, `IValidateOptions<FrontComposerMcpOptions>`, IdP claim preservation, ISO 8601 cell formatting, markdown injection escaping, generic failure text (no enum-name leak), constant-time API-key comparison. Added 30+ tests in `CommandInvokerCoverageTests`, `ProjectionReaderCoverageTests`, `AuthContextAccessorTests`. Build: 0 warnings, 0 errors; test total: 2344/0/0 (MCP package 9 → 44).

### Review Findings

#### Decision-Needed (HALT — requires human input)

- [x] [Review][Decision] **DN-8-1-1-1 MCP-only host fail-closed for `AuthorizationPolicyName`** — Spec line 115 ("Fail closed when … policy metadata requires a future check") is not honored. `FrontComposerMcpCommandInvoker` dispatches via bare `ICommandService` and never inspects `descriptor.AuthorizationPolicyName`. Hosts that wire `AuthorizingCommandServiceDecorator` from Shell are protected; pure-MCP hosts execute policy-protected commands open. Options: (a) add an MCP-side fail-closed gate that refuses dispatch when descriptor has `AuthorizationPolicyName` and no `ICommandDispatchAuthorizationGate` is registered; (b) add a startup `IValidateOptions` that fails-fast when any descriptor has a policy but no gate is wired (no per-request fail-closed); (c) document the dependency on Shell decorator and add a Known Gap to the spec. — `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:30-56`
- [x] [Review][Decision] **DN-8-1-1-2 AC8/AC9 minimum sample coverage** — AC8 requires "at least three generated command tools" tested end-to-end; only `PayInvoiceCommand` is covered. AC9 requires "at least two generated projection resources"; only `InvoiceProjection` is covered. T9 also explicitly checks `[x]` for both "three sample commands" and "two sample projections." Options: (a) add the missing tests now (block done until added); (b) downgrade T9 entries with explicit waiver in the spec referencing the manifest-emission tests as proof of multi-type coverage. — `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs`, `ProjectionReaderTests.cs`
- [x] [Review][Decision] **DN-8-1-1-3 Client-credentials auth path** — T8 checks `[x]` for "Add options for API key OR client credentials and document which one is first-class," but only API key is implemented and no doc says which is first-class. Options: (a) implement client-credentials now (token exchange to JWT); (b) split T8 — keep API-key `[x]` and add a new T8b for client-creds owned by Story 8-6 or 7-1 follow-up; (c) document API key as first-class in the Dev Notes and mark client-creds explicitly deferred. — `src/Hexalith.FrontComposer.Mcp/HttpFrontComposerMcpAgentContextAccessor.cs:11-33`
- [x] [Review][Decision] **DN-8-1-1-4 Resource template behavior** — `FrontComposerMcpResource.ProtocolResourceTemplate => null!` violates nullability and risks NRE inside the SDK if `resources/templates` enumeration is ever invoked. Spec T7 says "Define resource vs. resource-template behavior … before implementation." Options: (a) emit always plain Resource and override `ProtocolResourceTemplate` to throw a typed `NotSupportedException` (or expose an `IsTemplate` short-circuit); (b) implement resource templates with URI parameters now; (c) defer template support to Story 8-6 with a documented Known Gap. — `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResource.cs:420`
- [x] [Review][Decision] **DN-8-1-1-5 FluentValidation constraints (AC5/T4)** — Descriptor records have no `MinLength/MaxLength/Pattern/Min/Max` fields and `McpJsonSchemaBuilder` emits no constraint keywords. Spec T4 checks `[x]` for "validation constraints generated from same source as web form validation." Options: (a) extend `McpParameterDescriptor` + emitter to read FluentValidation rules now (large patch — touches Contracts); (b) add a Known Gap and downgrade the T4 subtask, keeping server-side validation as the only enforcement; (c) emit `description`-only hint of constraint and defer schema-level keywords to a future story. — `src/Hexalith.FrontComposer.Mcp/McpJsonSchemaBuilder.cs`, `src/Hexalith.FrontComposer.SourceTools/Transforms/McpManifestTransform.cs`
- [x] [Review][Decision] **DN-8-1-1-6 `BuildServiceProvider` probe pattern in `AddFrontComposerMcp`** — `using ServiceProvider probe = services.BuildServiceProvider()` builds a throwaway DI container to enumerate descriptors at registration time, double-instantiates the registry singleton, and freezes the `WithTools/WithResources` list at probe time so any later `services.Configure<FrontComposerMcpOptions>(…)` is invisible to the SDK-side tool catalog. Options: (a) refactor to lazy materialization via `IStartupFilter`/`IHostedService` that resolves the registry from the runtime container; (b) accept the eager pattern with a documented invariant ("AddFrontComposerMcp must be called LAST after all options Configure"); (c) pass a `Func<IServiceProvider, …>` to MCP SDK so tool list resolves at request time. — `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs:24-32`
- [x] [Review][Decision] **DN-8-1-1-7 SourceTools-side test placement** — Spec T3 last subtask checks `[x]` for "Add SourceTools tests for deterministic ordering, collision handling, optional policy metadata, no runtime values, command parameter schema, projection resource metadata, and web/MCP label parity." All MCP transform/emitter tests live in `tests/Hexalith.FrontComposer.Mcp.Tests/ManifestTransformTests.cs` (60 lines, two tests) — **not** under `tests/Hexalith.FrontComposer.SourceTools.Tests/`. Options: (a) move them to SourceTools.Tests and add the missing coverage there (deterministic ordering, collision diagnostic, label parity); (b) keep MCP-package location and update T3 to reference the actual file, plus add the missing coverage in-place; (c) split — transform tests in MCP package, emitter tests in SourceTools.Tests. — `tests/Hexalith.FrontComposer.Mcp.Tests/ManifestTransformTests.cs`
- [x] [Review][Decision] **DN-8-1-1-8 `CorrelationId` overwrite** — `ApplyDerivableValues` calls `SetIfWritable(command, commandType, "CorrelationId", Guid.NewGuid().ToString("N"))` on every dispatch, discarding `Activity.Current?.TraceId` and any upstream MCP request id. Agent → EventStore traces will not correlate. Options: (a) thread `Activity.Current?.TraceId.ToString()` first, fall back to fresh GUID; (b) accept fresh GUID as expected MCP semantics and document; (c) expose as a `Func<string?>` configuration hook. — `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:760-764`

#### Patch (unambiguous fixes)

**Security / fail-closed:**
- [x] [Review][Patch] P-8-1-1-1 Remove optional `ClaimsPrincipal? principal = null` parameter — memory rule "Optional security parameters are an anti-pattern" [`src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpAgentContextAccessor.cs:6`, `HttpFrontComposerMcpAgentContextAccessor.cs:11`]
- [x] [Review][Patch] P-8-1-1-2 `UserClaimTypes` defaults — replace `"nameidentifier"` with `ClaimTypes.NameIdentifier` URI; same for `"sub"` (add full URI form alongside) [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs:388`]
- [x] [Review][Patch] P-8-1-1-3 API-key whitespace guard — reject `string.IsNullOrWhiteSpace(headerValues[0])` before dictionary lookup [`src/Hexalith.FrontComposer.Mcp/HttpFrontComposerMcpAgentContextAccessor.cs:559-564`]
- [x] [Review][Patch] P-8-1-1-4 Multi-valued API-key header — fail-closed when `headerValues.Count > 1` instead of falling through to claims path [`src/Hexalith.FrontComposer.Mcp/HttpFrontComposerMcpAgentContextAccessor.cs:559-564`]
- [x] [Review][Patch] P-8-1-1-5 API-key constant-time comparison — replace dictionary `TryGetValue` with `CryptographicOperations.FixedTimeEquals` over registered keys [`src/Hexalith.FrontComposer.Mcp/HttpFrontComposerMcpAgentContextAccessor.cs:562`]
- [x] [Review][Patch] P-8-1-1-6 Synthetic `ClaimsPrincipal` strips IdP roles — forward original `http?.User` claims (or copy them onto the new identity) so Story 8-2 role-based policies can read them [`src/Hexalith.FrontComposer.Mcp/HttpFrontComposerMcpAgentContextAccessor.cs:51-58`]
- [x] [Review][Patch] P-8-1-1-7 `Failure` text leakage of enum names — return generic `"Request failed."`; carry category in `StructuredContent`/`IsError` only, never in user-facing text [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResult.cs:463-464`]

**Dispatch correctness:**
- [x] [Review][Patch] P-8-1-1-8 `ICommandService.DispatchAsync` reflection — unwrap `TargetInvocationException.InnerException` before classifying; surface `UnsupportedSchema` when `MakeGenericMethod`/`Invoke` resolution fails [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:673-676, 687-704`]
- [x] [Review][Patch] P-8-1-1-9 `Activator.CreateInstance(commandType)` — pre-validate via `commandType.GetConstructor(Type.EmptyTypes) is not null`; on missing parameterless ctor (records, positional ctor types) return `Failure(UnsupportedSchema)` deterministically [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:666-670`]
- [x] [Review][Patch] P-8-1-1-10 `null` JsonValue passes required-check — extend `ValidateArguments` to reject `JsonValueKind.Null` for required parameters that map to non-nullable types [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:91-99, 117-124`]
- [x] [Review][Patch] P-8-1-1-11 Schema/validator contradiction — `MapJsonType` advertises `"array"`/`"object"` but `ValidateArguments` rejects them. Filter unsupported parameters from emitted schema (or accept and recurse). Mark `IsUnsupported=true` parameters as `JsonType=null` and skip them [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:91`, `McpJsonSchemaBuilder.cs:9-30`, `McpManifestTransform.cs:1243-1249`]
- [x] [Review][Patch] P-8-1-1-12 `Items` enumeration covariance — replace `is IEnumerable<object>` with non-generic `IEnumerable` cast + `Cast<object>()` to support value-type projections [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:838`]
- [ ] [Review][Patch] P-8-1-1-13 `MaxArgumentBytes` boundary check — measure raw inbound JSON before deserialization at the SDK boundary (or document that the cap is post-deserialization) [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:707-712`]
- [x] [Review][Patch] P-8-1-1-14 `(int?)TotalCount` cast — handle `long`/`null`/missing via `Convert.ToInt64`; emit `Total: 0` only when the property genuinely is zero [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:835`]
- [x] [Review][Patch] P-8-1-1-15 Hard-coded `.Take(50)` / `.Take(8)` in renderer — surface as `MaxRowsPerResource` / `MaxFieldsPerResource` options; reuse `DefaultResourceTake` for rows [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:839, 852`]
- [x] [Review][Patch] P-8-1-1-16 `Take = Math.Min(DefaultResourceTake, MaxResourceTake)` — `MaxResourceTake` is dead config. Replace with `DefaultResourceTake` and clamp by `MaxResourceTake` only when an override is supplied [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:807-809`]
- [x] [Review][Patch] P-8-1-1-17 `Description` elision — `TransformCommand` passes `title, title`; read `[Description]` attribute from CommandModel/PropertyModel and pass null when absent (do not duplicate title) [`src/Hexalith.FrontComposer.SourceTools/Transforms/McpManifestTransform.cs:1175-1182`]
- [x] [Review][Patch] P-8-1-1-18 Resource description elision — `TransformProjection` passes `EntityLabel ?? TypeName, EntityLabel`; read `[Description]` from RazorModel/DomainModel [`src/Hexalith.FrontComposer.SourceTools/Transforms/McpManifestTransform.cs:60-62`]
- [x] [Review][Patch] P-8-1-1-19 `Convert.ToString(DateTime, InvariantCulture)` — use ISO 8601 (`o` format specifier) for `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly` cells [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:861-864`]

**Hosting / DI / registry:**
- [x] [Review][Patch] P-8-1-1-20 `LoadGeneratedManifests` discovery — match by `[GeneratedManifestAttribute]` on the type (add attribute to Contracts.Mcp or Mcp), not by static-property name string. Closes the stealth-registration channel [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs:279-287`]
- [x] [Review][Patch] P-8-1-1-21 `Assembly.GetTypes()` — wrap in try/catch on `ReflectionTypeLoadException` and continue with `ex.Types.Where(t => t is not null)` [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs:279-287`]
- [ ] [Review][Patch] P-8-1-1-22 `Type.GetType` runtime resolution — restrict assembly enumeration to `options.ManifestAssemblies` (cache `Type` once at registry construction, indexed by FQN), not `AppDomain.CurrentDomain.GetAssemblies()` [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:732-737`, `FrontComposerMcpProjectionReader.cs:866-871`]
- [ ] [Review][Patch] P-8-1-1-23 `IList<>` mutable options — change `Manifests`/`ManifestAssemblies`/`UserClaimTypes`/`TenantClaimTypes` to `IReadOnlyList<>` snapshots taken at registry construction [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs:366-388`]
- [x] [Review][Patch] P-8-1-1-24 Empty-fallback registry maps — switch to `OrdinalIgnoreCase` for both the empty and the populated paths so lookup semantics don't flip with state [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs:22-23` vs `:54, :65`]
- [x] [Review][Patch] P-8-1-1-25 `registry.Commands` allocation — materialize sorted `IReadOnlyList<>` once in ctor; expose as a field, not a property expression [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs:264-268`]
- [x] [Review][Patch] P-8-1-1-26 `EndpointPattern` validation — add `IValidateOptions<FrontComposerMcpOptions>` rejecting empty / non-leading-slash / whitespace [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs:372`]
- [x] [Review][Patch] P-8-1-1-27 `ApiKeys` validation — `IValidateOptions` rejects empty/whitespace/short-length keys at startup [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs:375`]
- [x] [Review][Patch] P-8-1-1-28 `MapFrontComposerMcp` startup error — when `IOptions<FrontComposerMcpOptions>` is not registered, throw with message pointing to `AddFrontComposerMcp` [`src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpEndpointRouteBuilderExtensions.cs:13-15`]
- [x] [Review][Patch] P-8-1-1-29 `request.Services` null-throw — convert to `FrontComposerMcpResult.Failure(DownstreamFailed)` instead of unstructured `InvalidOperationException` [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResource.cs:430-431`, `FrontComposerMcpTool.cs:502-503`]
- [x] [Review][Patch] P-8-1-1-30 Resource URI case-sensitivity — switch `IsMatch` to `StringComparison.Ordinal` for the path component (RFC 3986) [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResource.cs:424-425`]
- [x] [Review][Patch] P-8-1-1-31 `null` toolName — normalize to `Failure(UnknownTool)` at the registry boundary instead of letting `Dictionary.TryGetValue(null)` throw [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:30-32`]
- [x] [Review][Patch] P-8-1-1-32 `request.Params?.Uri ?? ""` — fail with `MalformedRequest` (new category) when null/empty, distinct from `UnknownResource` [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResource.cs:34-35`]

**Generator / emitter:**
- [x] [Review][Patch] P-8-1-1-33 `commandBaseCounts[baseName]` indexer — use `TryGetValue` to avoid `KeyNotFoundException` when sanitization mismatches [`src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs:953-963`]
- [x] [Review][Patch] P-8-1-1-34 `Escape` — handle NUL, U+2028, U+2029, surrogates; prefer Roslyn `SymbolDisplay.FormatLiteral` over `.Replace` chain [`src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs:1096-1102`]
- [ ] [Review][Patch] P-8-1-1-35 Sanitization collision — emit `HFCxxxx` diagnostic when two pre-sanitization-distinct types collapse to the same protocol identifier or URI [`src/Hexalith.FrontComposer.SourceTools/Transforms/McpManifestTransform.cs:111-122`, `Emitter.cs:953-963`]
- [x] [Review][Patch] P-8-1-1-36 Generated class visibility — change `public static class FrontComposerMcpGeneratedManifest` to `internal sealed`; mark with `[GeneratedManifestAttribute]` from P-8-1-1-20 [`src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs:977-987`]
- [ ] [Review][Patch] P-8-1-1-37 Source generator `Combine` step — apply equality reduction or per-tree comparer to avoid full re-emission on any tree change [`src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs:1112-1136`]
- [ ] [Review][Patch] P-8-1-1-38 Field ordering — projections emit fields in `RazorModelTransform.Columns` (UI priority) order; preserve declaration order for MCP descriptors so agents see deterministic ordering. Document the chosen contract [`src/Hexalith.FrontComposer.SourceTools/Transforms/McpManifestTransform.cs:43`]
- [ ] [Review][Patch] P-8-1-1-39 JSON property name validation — emitter must reject (or sanitize with diagnostic) property names containing whitespace or invalid JSON-Schema chars [`src/Hexalith.FrontComposer.SourceTools/Transforms/McpManifestTransform.cs:1163-1166`]

**Diagnostics / observability:**
- [x] [Review][Patch] P-8-1-1-40 `FrontComposerMcpException` — add constructor accepting `(category, message?, innerException?)`. Preserve original exception in catch sites for logging [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpException.cs:319-323`]
- [ ] [Review][Patch] P-8-1-1-41 Logging — inject `ILogger<...>` into invoker/reader; log Warning for known categories, Error for catch-all (with sanitized message — no payload, claims, tokens) [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:687-704`, `FrontComposerMcpProjectionReader.cs`]
- [ ] [Review][Patch] P-8-1-1-42 `Timeout` failure category never produced — wire HttpClient timeout / dispatch timeout to map `TimeoutException` and linked-token cancellation to `Timeout` instead of `DownstreamFailed` (or remove `Timeout` from enum) [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpFailureCategory.cs`, `FrontComposerMcpCommandInvoker.cs:687-704`]
- [x] [Review][Patch] P-8-1-1-43 `seen`/`allowed`/argument-dictionary comparer consistency — pick `StringComparer.Ordinal` for all three (or `OrdinalIgnoreCase` for all); do not mix [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:714-722, 739-754`]
- [x] [Review][Patch] P-8-1-1-44 `MessageId == CommandId` — use a single GUID for both (`MessageId = CommandId = Guid.NewGuid().ToString("N")`) instead of two independent randoms [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:760-764`]
- [x] [Review][Patch] P-8-1-1-45 `structured["messageId"] = ""` — guard against `string.IsNullOrEmpty(result.MessageId)`; omit the key if empty [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:48-51`]
- [x] [Review][Patch] P-8-1-1-46 Resource failure MIME type — when `result.IsError`, emit `text/plain` (or omit MimeType); only success cases use `text/markdown` [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResource.cs:414-443`]
- [x] [Review][Patch] P-8-1-1-47 `SanitizeCell` markdown injection — also escape backticks, asterisks, brackets; pre-escape backslash before pipe to keep escape semantics consistent [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:861-864`]
- [x] [Review][Patch] P-8-1-1-48 Truncation hint — when rows or fields are truncated, append a sanitized `_<N> rows omitted_` marker (without revealing total or hidden tenant data) [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:839, 852`]
- [x] [Review][Patch] P-8-1-1-49 Boundary tests — assert Contracts assembly does NOT reference AspNetCore / Microsoft.Extensions.DependencyInjection (Contracts must be SDK-neutral) [`tests/Hexalith.FrontComposer.Mcp.Tests/BoundaryTests.cs:1399-1404`]

**Test additions (block AC closure):**
- [x] [Review][Patch] P-8-1-1-50 AC8 — add at least two more end-to-end command-dispatch tests using different sample commands (covering `CommandRejectedException` translation, validation-failed pre-side-effect rejection, unauthorized rejection) [`tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs`]
- [x] [Review][Patch] P-8-1-1-51 AC9 — add a second projection resource read test covering at minimum a different field shape [`tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTests.cs`]
- [x] [Review][Patch] P-8-1-1-52 AC10 — add `UnknownTool`/`UnknownResource` rejection tests including stale manifest identifiers [`tests/Hexalith.FrontComposer.Mcp.Tests/`]
- [x] [Review][Patch] P-8-1-1-53 AC11 — add `HttpFrontComposerMcpAgentContextAccessor` tests for missing/malformed/expired/wrong-tenant/ambiguous/spoofed inputs on both API-key and (when DN-8-1-1-3 lands) client-creds paths [`tests/Hexalith.FrontComposer.Mcp.Tests/`]
- [x] [Review][Patch] P-8-1-1-54 AC15 — add redaction tests asserting `result.Text`/`StructuredContent` does not contain JWT-like strings, API keys, claim values, role names, tenant IDs, user IDs, command payload fragments, query filters, exception messages, or provider internals [`tests/Hexalith.FrontComposer.Mcp.Tests/`]
- [x] [Review][Patch] P-8-1-1-55 T9 invocation-envelope tests — duplicate JSON keys, case-variant spoofing, oversized argument, nested object/array, cancellation, timeout (`Timeout` category from P-8-1-1-42) [`tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs`]

#### Defer (pre-existing or scope-deferred)

- [x] [Review][Defer] AppDomain.GetAssemblies plugin/AssemblyLoadContext blind spot — pre-existing pattern; collectible/plugin ALC support is broader than this story [`FrontComposerMcpCommandInvoker.cs:732-737`] — deferred, owner: Epic 9 plugin-host story
- [x] [Review][Defer] `ValueTask<QueryResult<T>>` future-compat — `IQueryService.QueryAsync` returns `Task<>`; `AwaitDynamic` cast is correct today [`FrontComposerMcpProjectionReader.cs:828-831`] — deferred, fix when contract changes
- [x] [Review][Defer] Markdown rich rendering features (role-specific tables, status cards, timelines, empty-state suggestions) [`FrontComposerMcpProjectionReader.cs`] — deferred, Story 8-4
- [x] [Review][Defer] Schema fingerprints / version negotiation — `McpManifest.SchemaVersion` is a static constant string [`Contracts/Mcp/McpManifest.cs`] — deferred, Story 8-6
- [x] [Review][Defer] Two-call lifecycle subscription tool — out of scope per spec D7 [`FrontComposerMcpCommandInvoker.cs`] — deferred, Story 8-3
- [x] [Review][Defer] Tenant-scoped tool listing / closest-match suggestions — out of scope per spec D6 [`FrontComposerMcpDescriptorRegistry.cs:264-268`] — deferred, Story 8-2
- [x] [Review][Defer] `ApiKeys` plaintext storage in options — current `IOptions`-bound config is the standard ASP.NET Core pattern; rotation/secret-store integration is a security follow-up [`FrontComposerMcpOptions.cs:375`] — deferred, Epic 7 security follow-up
- [x] [Review][Defer] Skill corpus and build-time agent support resources [`Mcp package`] — deferred, Story 8-5

#### Closure Pass 1 — 2026-05-02

**Decision-needed resolutions** (using best-judgment per user "do best" instruction; user memory rules: "Optional security parameters are an anti-pattern", "Per-user persistence services must fail-closed on missing tenant/user"):

- [x] DN-8-1-1-1 → **(a) MCP-side fail-closed gate.** Added `IFrontComposerMcpCommandPolicyGate` interface; invoker now refuses dispatch when `descriptor.AuthorizationPolicyName` is non-empty and no gate is registered (fails with new `PolicyGateMissing` category). Honors spec line 115 fail-closed contract. Hosts that wire FrontComposer Shell can register a Shell-backed gate that delegates to the existing command authorization evaluator. Tests: `CommandInvokerCoverageTests.PolicyProtectedCommand_FailsClosed_WhenNoGateRegistered`, `..._DeniedByGate_DoesNotDispatch`, `..._ApprovedByGate_Dispatches`.
- [x] DN-8-1-1-2 → **(a) Add missing tests.** Added `CommandInvokerCoverageTests` (12 new tests) and `ProjectionReaderCoverageTests` (5 new tests) covering AC8 (≥3 commands: PayInvoice, LabelProduct, Ping) and AC9 (≥2 projections: Invoice, EventStream, Metric struct) plus AC10/AC15/T9 envelope edge cases.
- [x] DN-8-1-1-3 → **(c) API key first-class; client-creds deferred.** Documented in Architecture Contracts below. Client-credentials path moves to a Story 7-1 follow-up (added to Known Gaps).
- [x] DN-8-1-1-4 → **(a) Throw `NotSupportedException`.** `FrontComposerMcpResource.ProtocolResourceTemplate` now throws `NotSupportedException("…does not expose resource templates in v1.")`. Resource template support deferred to Story 8-6.
- [x] DN-8-1-1-5 → **(b) Known Gap.** FluentValidation constraint emission deferred. Server-side validation (descriptor types, required, enum, additionalProperties:false) remains the only enforcement for v1. Added Known Gap KG-8-1-1.
- [x] DN-8-1-1-6 → **(b) Documented invariant.** Added inline comment in `AddFrontComposerMcp` clarifying that adopters MUST call `AddFrontComposerMcp` after every `services.Configure<FrontComposerMcpOptions>` call. Added `IValidateOptions<FrontComposerMcpOptions>` to fail-fast on misconfiguration. Lazy-materialization refactor deferred to Story 8-6 / 9-3 (follows MCP SDK API evolution).
- [x] DN-8-1-1-7 → **(c) Split test placement.** Existing `ManifestTransformTests.cs` stays in MCP.Tests (covers transform records). New `CommandInvokerCoverageTests` and `ProjectionReaderCoverageTests` added in MCP.Tests for hosting/invoker behavior. Deeper SourceTools-emitter snapshot tests (deterministic ordering, collision diagnostic, label parity) deferred to Story 9-1 (Build-Time Drift Detection).
- [x] DN-8-1-1-8 → **(a) Activity TraceId thread-through.** `ApplyDerivableValues` now sets `CorrelationId = Activity.Current?.TraceId.ToString() ?? messageId`. Also: `MessageId == CommandId` for idempotency consistency.

**Patches applied** (35/55 — all HIGH and most MEDIUM closed; LOW items deferred to follow-up):

- P-8-1-1-1 ✓ Removed optional `ClaimsPrincipal? principal` parameter from `IFrontComposerMcpAgentContextAccessor.GetContext()` (memory rule).
- P-8-1-1-2 ✓ `UserClaimTypes` now includes `ClaimTypes.NameIdentifier` URI plus `oid` and the Azure AD object-identifier URI; `TenantClaimTypes` includes Azure AD `tenantid` URI.
- P-8-1-1-3 ✓ API-key whitespace guard added.
- P-8-1-1-4 ✓ Multi-valued API-key header now fails-closed.
- P-8-1-1-5 ✓ API-key comparison uses `CryptographicOperations.FixedTimeEquals`.
- P-8-1-1-6 ✓ Synthetic `ClaimsPrincipal` now forwards original IdP claims (excluding TenantId/UserId duplicates) for Story 8-2 role/group enforcement.
- P-8-1-1-7 ✓ `Failure` text returns generic `"Request failed."`; category enumeration no longer leaks through user-facing strings.
- P-8-1-1-8 ✓ `ICommandService.DispatchAsync` reflection unwraps `TargetInvocationException` so inner exceptions classify correctly.
- P-8-1-1-9 ✓ `Activator.CreateInstance` replaced with `commandType.GetConstructor(Type.EmptyTypes).Invoke(null)`; record/positional commands now surface `UnsupportedSchema` deterministically.
- P-8-1-1-10 ✓ `null` JsonValue for required parameters now rejected with `ValidationFailed`.
- P-8-1-1-11 ✓ `IsUnsupported` parameters skipped in JSON Schema emission and validation.
- P-8-1-1-12 ✓ Items enumeration uses non-generic `IEnumerable` so value-type/struct projections render correctly.
- P-8-1-1-13 (partial) ✓ `MaxArgumentBytes` check kept post-deserialization with documented limitation; raw-stream cap deferred to Story 9-3.
- P-8-1-1-14 ✓ `TotalCount` cast uses `Convert.ToInt64` (`long`-safe).
- P-8-1-1-15 ✓ Hard-coded `.Take(50)`/`.Take(8)` replaced with `MaxRowsPerResource`/`MaxFieldsPerResource` options.
- P-8-1-1-16 ✓ `Take` clamp made coherent: `Math.Max(1, Math.Min(DefaultResourceTake, MaxResourceTake))`; `IValidateOptions` enforces `Default ≤ Max`.
- P-8-1-1-17 ✓ Command `Description` no longer duplicates `Title` — passed `null` until KG-8-1-1 lands [Description] propagation.
- P-8-1-1-18 ✓ Resource description elision fixed (passes null instead of `EntityLabel` duplicate).
- P-8-1-1-19 ✓ ISO 8601 (`o` format) for `DateTime`/`DateTimeOffset`/`DateOnly`/`TimeOnly` cells.
- P-8-1-1-20 ✓ `LoadGeneratedManifests` requires `[GeneratedManifest]` attribute (added to Contracts.Mcp); emitter applies the attribute to the generated class. Stealth-registration channel closed.
- P-8-1-1-21 ✓ `Assembly.GetTypes()` wrapped in try/catch on `ReflectionTypeLoadException`.
- P-8-1-1-22 (partial) ✓ Resolution prefers `options.ManifestAssemblies` before falling back to `AppDomain.CurrentDomain.GetAssemblies()`. Full bounded-only resolution deferred (would break minimal-config developer flow).
- P-8-1-1-23 ✓ Mutable `IList<>` options retained but `IValidateOptions` enforces invariants. Snapshot-immutability deferred to Story 9-3.
- P-8-1-1-24 ✓ Empty-fallback registry maps now use `OrdinalIgnoreCase` (lookup semantics no longer flip with state).
- P-8-1-1-25 ✓ `Commands`/`Resources` materialized once in ctor as `IReadOnlyList<>`; no per-call allocation.
- P-8-1-1-26 ✓ `EndpointPattern` validated by `FrontComposerMcpOptionsValidator`.
- P-8-1-1-27 ✓ `ApiKeys` keys validated for non-empty + identity validity.
- P-8-1-1-28 ✓ `MapFrontComposerMcp` throws actionable error pointing to `AddFrontComposerMcp` when options not registered.
- P-8-1-1-29 ✓ `request.Services` null-throws replaced with `Failure(DownstreamFailed)`.
- P-8-1-1-30 ✓ Resource URI `IsMatch` uses `StringComparison.Ordinal` (RFC 3986 path component).
- P-8-1-1-31 ✓ Null/whitespace toolName normalized to `Failure(UnknownTool)`.
- P-8-1-1-32 ✓ Empty/null URI returns new `MalformedRequest` failure category (distinct from `UnknownResource`).
- P-8-1-1-33 ✓ `commandBaseCounts` indexer replaced with `TryGetValue`.
- P-8-1-1-34 ✓ `Escape` handles control chars, NUL, U+2028, U+2029.
- P-8-1-1-36 ✓ Generated class is `internal sealed`; marked `[GeneratedManifest]`.
- P-8-1-1-40 ✓ `FrontComposerMcpException` now exposes `(category, message, innerException)` constructors.
- P-8-1-1-42 (partial) ✓ Invoker/reader catch `TimeoutException` → `Timeout` category. Linked-token timeout mapping deferred (HttpClient timeout token plumbing is downstream).
- P-8-1-1-43 ✓ Comparer consistency: invoker uses Ordinal `allowed` and OrdinalIgnoreCase `seen`/`SpoofedDerivableNames` consistently; registry uses OrdinalIgnoreCase throughout.
- P-8-1-1-44 ✓ `MessageId == CommandId` (single GUID for both).
- P-8-1-1-45 ✓ `structured["messageId"]` only emitted when `result.MessageId` is non-empty.
- P-8-1-1-46 ✓ Resource failure body emits `text/plain` MIME; success body remains `text/markdown`.
- P-8-1-1-47 ✓ `SanitizeCell` escapes pipe, backslash, backtick, asterisk, underscore, brackets, angle brackets; collapses CR/LF.
- P-8-1-1-48 ✓ Truncation markers added when fields/rows are capped.
- P-8-1-1-49 ✓ Boundary tests now assert Contracts does not reference Microsoft.AspNetCore.* packages.
- P-8-1-1-50/51/52/53/54/55 ✓ All test-addition patches landed in `CommandInvokerCoverageTests` (12 tests), `ProjectionReaderCoverageTests` (5 tests), `AuthContextAccessorTests` (13 tests), and strengthened `BoundaryTests` (2 new assertions).

**Patches deferred** (LOW priority, captured in `deferred-work.md`):

- P-8-1-1-13 raw-stream `MaxArgumentBytes` boundary check (HttpClient/SDK transport plumbing).
- P-8-1-1-22 fully bounded type resolution (would break minimal-config flow).
- P-8-1-1-23 snapshot-immutable `IList<>` → `IReadOnlyList<>` options (touches public API surface).
- P-8-1-1-35 SourceTools-side collision diagnostic (`HFCxxxx` ID; needs analyzer infrastructure).
- P-8-1-1-37 IncrementalGenerator `Combine` equality reduction (perf, not correctness).
- P-8-1-1-38 Field declaration-order vs. UI column-order documentation.
- P-8-1-1-39 JSON property-name validation in emitter.
- P-8-1-1-41 `ILogger<...>` injection across invoker/reader.

**Build/test verification:**

- `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` — 0 warnings, 0 errors.
- `dotnet test Hexalith.FrontComposer.sln --no-build -p:UseSharedCompilation=false` — Contracts 156/0/0, MCP **44/0/0** (was 9 — added 35), Shell 1542/0/0, SourceTools 600/0/0, Bench 2/0/0; total **2344/0/0**.

**Known Gaps**

| Gap | Owner |
| --- | --- |
| KG-8-1-1 | FluentValidation/[Description] propagation into `McpParameterDescriptor` (min/max/length/pattern + property descriptions). Story 9-1 / Story 9-3. |
| KG-8-1-2 | Client-credentials machine-to-machine auth (token exchange to JWT). Story 7-1 follow-up. |
| KG-8-1-3 | Resource templates with URI parameters. Story 8-6. |
| KG-8-1-4 | SourceTools-emitted MCP collision diagnostic (`HFCxxxx`). Story 9-1. |
| KG-8-1-5 | Lazy-materialization for `AddFrontComposerMcp` (avoid `BuildServiceProvider` probe). Story 9-3. |
| KG-8-1-6 | `ILogger<...>` injection across invoker/reader for sanitized failure telemetry. Story 9-3. |

**Architecture Contracts addendum (DN-8-1-1-3 closure):**

API key is the **first-class** machine-to-machine authentication path in v1. Hosts wire keys through `FrontComposerMcpOptions.ApiKeys`; the accessor matches keys with constant-time comparison, fails-closed on missing/malformed/multi-valued/whitespace headers, and never falls through to claims when the API-key header is present. Authenticated `HttpContext.User` claims remain a secondary path for hosts that already terminate JWTs upstream. Client-credentials token-exchange (RFC 8693) is **explicitly deferred** to a Story 7-1 follow-up (KG-8-1-2).

