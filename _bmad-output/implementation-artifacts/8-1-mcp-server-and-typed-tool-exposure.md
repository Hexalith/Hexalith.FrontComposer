# Story 8.1: MCP Server & Typed Tool Exposure

Status: ready-for-dev

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

- [ ] T1. Create the MCP package boundary (AC1, AC7, AC11, AC15)
  - [ ] Add `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj` and `tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj`.
  - [ ] Reference `Hexalith.FrontComposer.Contracts` and the minimum runtime packages needed for MCP hosting; do not make Contracts or SourceTools depend on MCP SDK packages.
  - [ ] Add package references through central package management only, likely `ModelContextProtocol.AspNetCore` for HTTP-hosted server support and `ModelContextProtocol` only if non-HTTP hosting is chosen.
  - [ ] Register the new projects in `Hexalith.FrontComposer.sln` and keep test categorization consistent with existing Contracts/Shell/SourceTools test projects.

- [ ] T2. Define MCP manifest contracts (AC2-AC6, AC12-AC16)
  - [ ] Add dependency-light metadata records such as `McpCommandDescriptor`, `McpResourceDescriptor`, `McpParameterDescriptor`, and `McpManifest` in the MCP package or Contracts only if a stable public contract is required.
  - [ ] Keep generated metadata type-only and config-only: command/projection FQN, bounded context, display title, parameter schema, optional authorization policy, and resource URI template.
  - [ ] Forbid runtime values in descriptors: tenant/user identifiers, JWTs, claims, command payload values, query results, localized resolved strings, service instances, `ClaimsPrincipal`, or `RenderContext`.
  - [ ] Include `AuthorizationPolicyName` when present from Story 7-3 metadata; do not interpret it here beyond carrying metadata.

- [ ] T3. Extend SourceTools with MCP manifest emission (AC2-AC6, AC12-AC14)
  - [ ] Extend parse/transform models only where needed to expose command and projection metadata already known to SourceTools.
  - [ ] Emit a deterministic manifest artifact for all discovered command/projection types in the compilation.
  - [ ] Reuse label/description logic from web emit paths; do not fork a second humanizer or display-metadata parser.
  - [ ] Generate MCP-safe tool names using a documented rule. Prefer disambiguated names based on bounded context and type name; detect collisions per L04 before runtime.
  - [ ] Add SourceTools tests for deterministic ordering, collision handling, optional policy metadata, no runtime values, command parameter schema, projection resource metadata, and web/MCP label parity.

- [ ] T4. Map command tool input schema (AC2, AC4, AC5, AC8, AC13)
  - [ ] Build JSON Schema object output for command user-entered properties, using nullable/required state, enum members, numeric/date/string categories, descriptions, display labels, and known FluentValidation-derived constraints where available.
  - [ ] Emit `additionalProperties: false` for command tools unless a deliberate extension point is documented.
  - [ ] Do not expose derivable infrastructure fields as model-controlled input. Tenant and user context come from authenticated agent context, not from tool arguments.
  - [ ] Reject spoofed derivable fields such as `TenantId`, `UserId`, message/idempotency identifiers, correlation IDs, and system-owned values before command construction or dispatch.
  - [ ] Define deterministic behavior for unsupported CLR/type categories instead of inventing adapter-side schema semantics.
  - [ ] Keep validation constraints generated from the same source as web form validation; if a constraint cannot be represented in JSON Schema, include bounded tool-description guidance and server-side validation.

- [ ] T5. Host and route the MCP server (AC1, AC7, AC10, AC15)
  - [ ] Add hosting extensions such as `AddFrontComposerMcp` and `MapFrontComposerMcp`.
  - [ ] Use the official MCP C# SDK server APIs rather than hand-rolling JSON-RPC framing.
  - [ ] Support the configured transport selected for v1; HTTP/SSE or streamable HTTP should live behind the MCP package and not leak into Contracts.
  - [ ] Implement unknown tool/resource handling as a bounded protocol error in Story 8-1. Closest-match suggestion, tenant-scoped list, and self-correction copy are Story 8-2.
  - [ ] Add startup validation for duplicate descriptors, missing manifest registration, unsupported transport config, and sanitized diagnostic output.

- [ ] T6. Implement MCP command invocation adapter (AC8, AC10-AC12, AC15)
  - [ ] Route generated tool calls to the existing `ICommandService.DispatchAsync<TCommand>` path or its lifecycle-aware companion when already available.
  - [ ] Preserve EventStore behavior from Epic 5: tenant propagation, token relay, idempotency/message ID discipline, ETag/cache non-interference, retry/degraded classification, and sanitized telemetry.
  - [ ] Validate tool arguments against generated schema before constructing the command. Rejections must occur before `ICommandService`, EventStore serialization, token acquisition, HTTP send, lifecycle state mutation, or SignalR/cache side effects.
  - [ ] Return a minimal acknowledged result shape with command/message/correlation data already available from `CommandResult`. Full two-call lifecycle subscription remains Story 8-3.
  - [ ] Carry but do not fully enforce Story 7-3 policy metadata unless the existing shared evaluator is ready; record any gap as a deferred decision rather than adding a parallel policy engine.

- [ ] T7. Implement projection resource adapter (AC3, AC9, AC15)
  - [ ] Route projection reads to the existing `IQueryService.QueryAsync<T>` path with tenant context from authenticated agent state.
  - [ ] Return deterministic text/Markdown or structured content for at least Default/ActionQueue-style table output sufficient for AC9.
  - [ ] Define resource vs. resource-template behavior, URI shape, query parameter binding, validation failure mapping, pagination or size limits, and content type before implementation.
  - [ ] Keep rich role-specific Markdown rendering, status cards, timelines, empty-state suggestions, and badge text polish scoped to Story 8-4 unless simple reuse already exists.
  - [ ] Do not let clients supply raw tenant IDs to query resources; tenant comes from authenticated context.

- [ ] T8. Add machine-to-machine authentication seam (AC11, AC12, AC15)
  - [ ] Add options for API key or client credentials and document which one is first-class in v1.
  - [ ] Normalize successful authentication into the existing Story 7-1 / Story 7-2 principal and tenant-context seams.
  - [ ] Define precedence and failure behavior for API key/client-credentials identities, JWT/principal claims, and any attempted tenant/user values in tool/resource input; ambiguous or mismatched values fail closed.
  - [ ] Fail closed when auth is absent, token/API key is invalid, tenant claim is missing/empty/whitespace, policy metadata requires a future check, or auth state cannot be resolved.
  - [ ] Ensure auth failures do not trigger browser redirects and do not leak token, provider, client secret, tenant, or user values in responses or logs.

- [ ] T9. Tests and verification (AC1-AC16)
  - [ ] Contracts/package-boundary tests proving MCP dependencies do not enter Contracts or SourceTools public surfaces.
  - [ ] SourceTools manifest snapshot tests for commands, projections, labels, descriptions, enum/nullable/required schema, policy metadata, collision diagnostics, and no-runtime-value descriptors.
  - [ ] MCP hosting tests proving DI registration, endpoint mapping, duplicate descriptor rejection, unknown tool/resource behavior, and sanitized diagnostics.
  - [ ] Command adapter tests covering three sample commands: valid call acknowledged, invalid arguments rejected before side effects, unauthorized/unauthenticated rejected before side effects, and command-service rejection translated to protocol-safe output.
  - [ ] Projection adapter tests covering two sample projections, tenant context injection, query-service invocation, Markdown/text determinism, and no raw tenant/user leakage.
  - [ ] Determinism/golden tests covering descriptor ordering, stable tool/resource identifiers, URI templates, JSON Schema serialization, localization-neutral fallback labels, optional policy metadata, and repeatable output across builds.
  - [ ] Auth and tenant-context tests covering missing, malformed, expired, wrong-tenant, ambiguous, and spoofed tenant/user inputs for both API key and client-credentials paths.
  - [ ] Adapter boundary tests proving request IDs and cancellation flow through, invalid arguments fail before side effects, unknown tools/resources/templates return deterministic errors, stale client manifest names do not dispatch, and command/query adapters preserve existing validation, tenant scoping, authorization hooks, and domain error mapping.
  - [ ] Redaction tests with JWT-like strings, API keys, claim values, role names, tenant IDs, user IDs, command payload fragments, query filters, exception messages, and provider internals.
  - [ ] Policy metadata guardrail tests proving the descriptor carries Story 7-3 policy identifiers while 8-1 does not claim full tenant-scoped authorization filtering.
  - [ ] Regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.
  - [ ] Targeted tests: `tests/Hexalith.FrontComposer.SourceTools.Tests`, `tests/Hexalith.FrontComposer.Mcp.Tests`, plus Shell/EventStore tests only if shared runtime seams are changed.

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

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-01: Story created via `/bmad-create-story 8-1-mcp-server-and-typed-tool-exposure` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### Party-Mode Review

- **Date/time:** 2026-05-01T11:14:48+02:00
- **Selected story key:** `8-1-mcp-server-and-typed-tool-exposure`
- **Command/skill invocation used:** `/bmad-party-mode 8-1-mcp-server-and-typed-tool-exposure; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- **Findings summary:** The review found that the story was ready in product scope but needed sharper pre-dev contracts for deterministic SourceTools-driven MCP descriptors, stable schema serialization, tenant/auth precedence, spoofed derivable field rejection, adapter error mapping, policy-metadata non-goals, resource/template boundaries, and testable sanitization.
- **Changes applied:** Added stable-abstraction dependency guidance; restricted MCP descriptor generation to mechanical SourceTools IR transforms; clarified tenant/auth precedence and fail-closed mismatches; added hardened party-mode clarifications for determinism, command input schema, projection resources, adapter response mapping, policy metadata non-goals, and localization contracts; tightened name/URI collision domains; expanded T4/T7/T8/T9 with spoofed-field, unsupported-type, resource-template, auth, determinism, adapter-boundary, and policy guardrail tests.
- **Findings deferred:** Full tenant-scoped listing/filtering, policy-based hiding, closest-match/hallucination suggestions, lifecycle subscription, rich role-specific Markdown rendering, skill corpus resources, schema fingerprints/version negotiation, and new authorization policy language remain deferred to their owning Epic 8 / Epic 7 follow-up stories.
- **Final recommendation:** ready-for-dev

### File List

(to be filled in by dev agent)
