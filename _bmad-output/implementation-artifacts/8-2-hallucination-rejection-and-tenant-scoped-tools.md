# Story 8.2: Hallucination Rejection & Tenant-Scoped Tools

Status: done

> **Epic 8** - MCP & Agent Integration. Covers **FR51**, **FR54**, **NFR7**, **NFR27**, and **NFR28**. Builds on Story **8-1** MCP server/manifest/adapter seams, Story **7-2** tenant context, Story **7-3** command authorization policy metadata, and Epic 5 command/query side-effect boundaries. Applies lessons **L03**, **L04**, **L06**, **L08**, **L10**, and **L14**.

---

## Executive Summary

Story 8-2 turns the MCP surface from "typed and hosted" into "safe to let an agent use without supervision":

- Reject unknown, stale, malformed, unauthorized, and cross-tenant MCP tool calls before any backend dispatch, query, lifecycle mutation, token relay, telemetry payload enrichment, or EventStore serialization.
- Return a bounded self-correction response for unknown tool names: closest matching visible tool plus the complete tenant-scoped, policy-filtered visible tool list.
- Filter `tools/list` and suggestion payloads by authenticated tenant and command policy metadata so unauthorized tools are invisible, not shown as forbidden.
- Keep all matching, validation, and enumeration logic manifest-driven from Story 8-1 descriptors. Do not rediscover domain types at runtime or introduce a second policy model.
- Preserve agent usefulness without leaking tenant identifiers, user identifiers, claim values, role names, provider internals, hidden tool existence, command payload fragments, or stack traces.

---

## Story

As an LLM agent,
I want unknown or malformed tool calls rejected immediately with a helpful suggestion,
so that hallucinated tool names never reach the backend and I can self-correct with the correct tool list.

### Adopter Job To Preserve

An adopter should be able to enable the MCP package and trust that an agent cannot call a plausible-but-wrong command, enumerate another tenant's tools, infer hidden policy-protected commands, or spoof tenant/user context through tool arguments. The response should be useful enough for agent self-correction while still treating tool visibility as a security boundary.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | An MCP `tools/call` request names a tool that is not present in the current visible tool catalog | The request reaches the MCP contract boundary | The call is rejected before command construction, validation service invocation, `ICommandService`, EventStore serialization, token relay, lifecycle state mutation, cache mutation, or SignalR side effects. |
| AC2 | An unknown tool is rejected | The rejection response is returned | The response includes a sanitized error category, the requested name only after length bounding and control-character escaping, the closest matching visible tool name when one clears the deterministic match threshold, and the complete visible tenant-scoped tool list within configured response bounds. |
| AC3 | An unknown tool is similar only to a tool from another tenant or a policy-hidden tool | The matcher evaluates candidates | No hidden candidate is suggested, no hidden candidate name appears in the visible list, and the response must not reveal that a hidden match exists. |
| AC4 | The request name differs only by case, separator, suffix, Unicode confusable, or stale alias from a visible tool | The matcher normalizes for comparison | The response chooses a deterministic visible suggestion without accepting the malformed name as executable. |
| AC5 | A visible tool has invalid parameters: type mismatch, missing required field, unknown field, duplicate or case-variant field, constraint violation, oversized value, or unsupported nested shape | Schema validation runs against the generated manifest | The call is rejected with parameter-level details before backend side effects. |
| AC6 | Tool arguments include derivable or infrastructure-owned fields such as `TenantId`, `UserId`, claim values, message IDs, correlation IDs, lifecycle state, ETag/cache keys, or policy identifiers | Invocation envelope normalization runs | The call fails closed before command construction; supplied derivable values are never accepted, merged, or echoed. |
| AC7 | An authenticated MCP agent has a canonical tenant context from Story 7-2 | The agent calls `tools/list` or receives an unknown-tool suggestion response | Only tools belonging to the active tenant are visible. Tools from other tenants are completely invisible. |
| AC8 | A command descriptor carries Story 7-3 `RequiresPolicy` metadata | The active agent lacks the required policy | The tool is omitted from list and suggestion candidates and a direct call by name is rejected with the same hidden/unknown category used for non-visible tools. |
| AC9 | A command descriptor carries Story 7-3 `RequiresPolicy` metadata | The active agent satisfies the required policy | The tool is visible and callable subject to normal schema validation and Story 8-1 command adapter rules. |
| AC10 | Authentication is absent, ambiguous, expired, malformed, missing tenant context, or inconsistent between token/API-key/principal sources | The MCP request enters list, suggestion, or call handling | The request fails closed without browser redirects and without listing, suggesting, or executing tools. |
| AC11 | A rejected response includes a visible tool list | The response is inspected | Tool names and descriptions are domain-generic and protocol-stable. They contain no tenant IDs, user IDs, role/claim values, customer names, environment names, localized runtime values, payload values, or provider internals. |
| AC12 | Unknown-name rejection performance is measured on a generated catalog large enough to represent v1 adopters | The rejection path runs in unit/performance tests with a warmed immutable registry, fixed candidate limits, and representative visible/hidden/cross-tenant near-matches | P95 is below 100 ms for the rejection path and below 50 ms for the ideal target where the test lane supports a stable timer. |
| AC13 | Rejection, validation, auth, and policy failures are logged or traced | Telemetry is inspected | Logs and spans contain only sanitized category, bounded context, descriptor kind, correlation/request ID where safe, and outcome; they do not contain raw exception text, claims, tokens, tenant/user identifiers, roles, payload fragments, or hidden tool names. |
| AC14 | The MCP SDK returns protocol errors differently across transports | The adapter maps FrontComposer rejection categories to SDK responses | The public response remains deterministic and protocol-appropriate while SDK DTO churn stays inside `Hexalith.FrontComposer.Mcp`. |
| AC15 | The story completes | Story 8-3 or 8-4 continues Epic 8 | The hallucination rejection, visible catalog, and policy-filtering services are reusable by lifecycle tools and projection resources without redesign. |
| AC16 | An agent replays a prior `tools/list` result, suggestion, catalog epoch, or visible tool name after tenant, policy, descriptor, or registry state changes | A later `tools/call` enters the MCP boundary | The call is resolved against the current server-side authenticated visibility snapshot only; client-supplied catalog state, suggestions, epochs, and list contents are advisory and never authorize execution. |

---

## Tasks / Subtasks

- [x] T1. Define visible tool catalog and matching contracts (AC1-AC4, AC7-AC9, AC15)
  - [x] Add MCP-package contracts for a request-scoped `McpVisibleToolCatalog`, `McpToolVisibilityContext`, and `McpToolSuggestion` or equivalent names.
  - [x] Define an explicit admission result contract such as `McpToolResolutionResult` with `accepted | rejected`, sanitized rejection category, requested name, optional visible suggestion, and bounded visible list fields.
  - [x] Ensure `tools/list`, unknown-tool rejection, hidden direct calls, and validation helper lists all consume the same visible-catalog service; do not duplicate policy filtering in separate list and call paths.
  - [x] Build the visible catalog from Story 8-1 generated descriptors plus authenticated tenant/policy context; do not scan assemblies or ask services for runtime domain type discovery.
  - [x] Keep catalog entries immutable after construction and bounded by explicit options such as `MaxVisibleToolListItems`, `MaxSuggestionCandidates`, and `MaxToolNameLength`.
  - [x] Define one canonical hidden outcome for tools not visible because they are absent, stale, cross-tenant, policy-hidden, or removed. Do not distinguish these cases in agent-visible responses.
  - [x] Treat `tools/list` responses, suggestion payloads, client-provided catalog epochs, and copied visible tool lists as advisory only; every `tools/call` must rebuild or resolve against a fresh server-side authenticated visibility snapshot.

- [x] T2. Implement deterministic unknown-tool rejection (AC1-AC4, AC11-AC14)
  - [x] Normalize requested and candidate names for matching only: case fold invariantly, normalize separators, trim protocol-safe whitespace, reject control characters, and detect unsupported Unicode/confusable forms instead of executing them.
  - [x] Produce a separate sanitized display/log form for the requested name with explicit length caps and escaped control/confusable markers; never echo the raw input string into protocol responses, logs, spans, or test snapshots.
  - [x] Use a deterministic low-cost algorithm such as bounded edit distance or prefix/token scoring over only visible candidates. Avoid semantic/LLM matching in v1.
  - [x] Return at most one best suggestion when it clears a documented threshold; otherwise return no suggestion but still include the visible tool list.
  - [x] Apply deterministic tie-breaking for equal scores and duplicate visible display labels; the same request and same visible catalog must always produce the same suggestion/null result.
  - [x] Reject direct execution of aliases, stale names, close matches, case variants, and cosmetic variants; the agent must call the canonical visible tool name.
  - [x] Add tests proving hidden candidates never influence suggested names, ranking, thresholds, timing-visible branches, telemetry labels, logs, or response copy.

- [x] T3. Harden schema validation and invocation envelope normalization (AC5, AC6, AC13)
  - [x] Reuse Story 8-1 JSON Schema/descriptor metadata as the validation source of truth.
  - [x] Validate stale descriptor/version/registry epoch mismatches before command construction and map them to the same agent-visible hidden/unknown category unless a safe schema-version category is explicitly documented.
  - [x] Reject missing required fields, wrong primitive types, enum mismatches, unsupported nested JSON, unknown properties, duplicate property names, case-variant spoofing, oversized argument objects, and unsupported CLR categories before command construction.
  - [x] Detect duplicate and case-variant JSON properties before dictionary/model binding by using a token-preserving reader or equivalent raw-envelope pass; do not rely on serializers that silently keep the last duplicate value.
  - [x] Reject all derivable/system-owned fields (`TenantId`, `UserId`, message/correlation IDs, policy IDs, cache keys, lifecycle values) even when the generated schema omits them.
  - [x] Add a malicious-descriptor guardrail: derivable/system-owned fields fail closed even if a malformed generated descriptor or stale manifest attempts to expose them as ordinary arguments.
  - [x] Produce parameter-level diagnostics such as `field`, `reason`, and `expectedShape` without echoing actual values or secrets.
  - [x] Preserve cancellation tokens and request IDs through rejection paths without allowing cancellation to skip cleanup or telemetry finalization.

- [x] T4. Enforce tenant-scoped enumeration (AC7, AC10-AC13)
  - [x] Resolve the active tenant only from the authenticated Story 7-2 tenant context. Tool arguments, headers not owned by auth, query strings, and prompt-provided text are never tenant sources.
  - [x] Fail closed when tenant context is absent, empty, whitespace, ambiguous, mismatched, or from an untrusted source.
  - [x] Filter `tools/list`, unknown-tool suggestions, validation failure helper lists, and any discovery cache from the same visibility service.
  - [x] Add two-tenant tests proving each tenant sees only its own commands and cannot infer the other tenant's tool count, names, descriptions, or policy metadata.
  - [x] Add tenant-switch and policy-change tests proving a previously returned visible list or suggestion cannot be replayed to execute a tool after the authenticated context no longer makes it visible.

- [x] T5. Enforce command policy metadata visibility (AC8, AC9, AC13)
  - [x] Consume Story 7-3 policy metadata from generated descriptors; do not add an MCP-only policy attribute or policy language.
  - [x] Use the shared authorization evaluator from Story 7-3 when available. If the evaluator is incomplete, implement the smallest adapter needed and record any gap as a deferred decision.
  - [x] Hide unauthorized tools from list and suggestions. Direct calls to policy-hidden tools receive the same hidden/unknown category as absent tools.
  - [x] Add tests for no-policy, allowed-policy, denied-policy, missing-policy-service, policy-evaluator-exception, and multiple-policy future-proofing behavior.

- [x] T6. Build sanitized self-correction responses (AC2, AC3, AC11, AC13, AC14)
  - [x] Define a stable response model for unknown names and validation errors inside the MCP package, then map it to official SDK response/error DTOs at the adapter edge.
  - [x] Include only protocol name, sanitized category, canonical suggestion if visible, visible tool names/signatures/descriptions, and safe docs/remediation text.
  - [x] Ensure domain-generic names: no tenant prefix/suffix, customer code, environment name, role value, claim value, localized runtime text, or payload-derived label.
  - [x] Cap response size and truncate the visible list deterministically with a sanitized continuation marker when needed.
  - [x] Treat generated tool titles, descriptions, annotations, and docs text as untrusted agent-visible content: length-bound them, strip or escape control characters, reject runtime/localized/customer-derived text, and never let description text influence authorization, matching, or remediation commands.
  - [x] When a visible suggestion is present but the visible list is truncated, keep the suggestion field canonical and independently bounded without using truncation metadata to reveal hidden or excluded counts.

- [x] T7. Performance and bounded-state implementation (AC12, AC15)
  - [x] Precompute immutable normalized lookup keys at startup or descriptor-registry build time; do not allocate unbounded per-request dictionaries.
  - [x] Bound candidate scoring by visible catalog size and configured limits.
  - [x] Add a timer-based unit/performance test for unknown-name rejection P95 < 100 ms using a representative generated catalog with warmed immutable registries, fixed descriptor counts, visible near-misses, hidden near-misses, and random unknown names.
  - [x] Add stress tests for long names, many near-matches, empty visible catalog, all tools hidden, repeated requests, and cancellation.
  - [x] Keep any catalog epoch/hash internal to server diagnostics unless explicitly sanitized for response metadata; never trust a client-supplied epoch as proof that a prior visibility decision is still valid.

- [x] T8. Tests and verification (AC1-AC15)
  - [x] MCP tests for unknown tool rejection before side effects using fake `ICommandService`, fake EventStore clients, fake lifecycle tracker, fake query service, and side-effect counters.
  - [x] Side-effect spy harness asserting rejected paths make zero calls to command construction, backend validation services, `ICommandService`, EventStore serialization/client calls, token relay, lifecycle/cache mutation, SignalR, and sensitive telemetry enrichment.
  - [x] Visibility tests for two tenants, no tenant, tenant mismatch, denied policy, allowed policy, hidden direct call, and hidden near-match.
  - [x] Schema tests for required/optional fields, primitive mismatch, enum mismatch, unknown fields, duplicate/case-variant fields, derivable field spoofing, oversized payload, and unsupported nested shape.
  - [x] Raw-envelope tests proving duplicate JSON fields, case-variant duplicates, and serializer-last-value behavior are detected before command DTO construction.
  - [x] Redaction tests with JWT-like strings, API keys, role names, claim values, tenant IDs, user IDs, customer names, payload fragments, exception text, provider internals, and hidden tool names.
  - [x] Descriptor-text redaction tests with prompt-injection-like descriptions, control characters, overlong titles, localized runtime values, and customer/environment labels.
  - [x] Determinism tests for candidate ordering, tie-breaking, normalized-name handling, response ordering, truncation, and repeated build output.
  - [x] Replay tests proving old `tools/list` payloads, suggestions, and catalog epochs cannot bypass current tenant/policy visibility at `tools/call` time.
  - [x] Timing-oracle tests comparing visible near-miss, hidden near-miss, cross-tenant near-miss, policy-hidden near-miss, and random unknown rejection paths without exposing hidden candidates through duration buckets.
  - [x] Package-boundary tests proving `Contracts` and SourceTools public surfaces do not reference MCP SDK DTOs.
  - [x] SDK adapter-boundary tests proving internal rejection contracts and snapshots remain stable when mapped through official MCP SDK response/error DTOs.
  - [x] Regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.
  - [x] Targeted tests: `tests/Hexalith.FrontComposer.Mcp.Tests`, `tests/Hexalith.FrontComposer.SourceTools.Tests`, plus Shell/EventStore tests only if shared seams change.

---

## Dev Notes

### Existing State To Preserve

| File / Area | Current state | Preserve / Change |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Mcp` | Story 8-1 owns MCP hosting, descriptor registry, manifest consumption, command/resource adapters, and SDK containment. | Extend here for visibility, matching, rejection, and policy filtering. Do not move MCP SDK types into Contracts or SourceTools public APIs. |
| Story `8-1-mcp-server-and-typed-tool-exposure` | Unknown names are rejected only as a bounded protocol error; closest-match suggestions and tenant-scoped listing are explicitly deferred. | Consume the 8-1 descriptor and invocation-envelope seams rather than rebuilding hosting or manifest emission. |
| `src/Hexalith.FrontComposer.Contracts/Communication/ICommandService.cs` | Command dispatch seam. | Invalid, unknown, unauthorized, and cross-tenant MCP requests must never reach this seam. |
| `src/Hexalith.FrontComposer.Contracts/Communication/IQueryService.cs` | Projection query seam. | Story 8-2 mainly targets tools, but visibility services must be reusable by resources later. Do not create query side effects during tool validation. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/IUserContextAccessor.cs` and Story 7-2 tenant context | Canonical tenant/user context is fail-closed. | Tenant for MCP visibility comes only from authenticated context, never tool input. |
| Story `7-3-command-authorization-policies` | Policy identifiers are attached to command metadata. | Use shared policy evaluation. Do not invent a parallel MCP authorization system. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/*` and 8-1 manifest descriptors | SourceTools IR remains schema source of truth. | Matching and validation consume emitted descriptors. No runtime reflection as canonical truth. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests` and future `tests/Hexalith.FrontComposer.Mcp.Tests` | Snapshot/determinism tests already protect generator behavior. | Add visibility/matching/redaction tests close to MCP package; use SourceTools tests only for descriptor metadata. |

### Architecture Contracts

- `Hexalith.FrontComposer.Mcp` references `Contracts` and adapts the official MCP C# SDK. `Contracts` remains dependency-free; SourceTools remains SDK-neutral.
- The visible catalog is request-scoped because auth/tenant/policy are request-scoped, but its descriptor source and normalized static metadata should be immutable and bounded.
- Client-visible catalog data is never an authorization cache. `tools/list` and suggestions are self-correction hints; `tools/call` must make a fresh server-side admission decision for the current authenticated context.
- Tenant isolation is a security boundary. Cross-tenant visibility is not a UX bug; it is a security bug per NFR28.
- Unknown, hidden, unauthorized, stale, and removed names share an agent-visible failure category to prevent enumeration through error differences.
- Matching is deterministic and local. No LLM calls, embeddings, external search, runtime reflection, or network requests are allowed on the rejection path.
- Suggestion input is exactly the authenticated, tenant-scoped, policy-visible catalog. Hidden, cross-tenant, stale, removed, and policy-denied descriptors are nonexistent for matching, ranking, logging, telemetry labels, response shaping, and timing-visible branches.
- Rejection and enumeration must use one admission/visibility service so `tools/list`, direct calls, validation helper lists, and unknown-tool suggestions cannot drift.

### Latest MCP Notes

- As of 2026-05-01, the official MCP tools specification states that tool names should be unique within a server and that tools expose input schemas. FrontComposer should still disambiguate by bounded context because aggregators can collide across servers.
- The MCP 2025-11-25 tools specification and the current draft both describe JSON Schema-based tool inputs. Use generated descriptor schemas as the FrontComposer source of truth and map to SDK DTOs at the boundary.
- The official MCP SDK list identifies the C# SDK as Tier 1, and the C# SDK package family includes `ModelContextProtocol.AspNetCore` for ASP.NET Core-hosted servers. Keep SDK usage inside `Hexalith.FrontComposer.Mcp` so package churn does not affect generated descriptors.
- Current MCP security guidance treats tool descriptions and annotations as untrusted unless the server is trusted. FrontComposer must not rely on descriptions for authorization or execution; they are agent guidance only.

### Previous Story Intelligence

Story 8-1 already established:

- `Hexalith.FrontComposer.Mcp` is the package boundary.
- SourceTools-generated MCP descriptors are type/config metadata only.
- Tenant/user context is derivable and never a tool argument.
- Unknown calls are rejected before backend dispatch, but suggestions and tenant-scoped listings belong here.
- Policy metadata from Story 7-3 is carried forward without creating a second policy language.
- Descriptor registries and caches must be bounded and immutable after startup.

This story should not reopen those decisions. It should make the deferred 8-2 behavior executable and testable.

### Pre-Invocation Admission Contract

Every MCP tool call goes through one admission pipeline before command construction or backend validation:

1. Resolve authenticated agent identity and canonical Story 7-2 tenant context; absent, ambiguous, expired, malformed, missing-tenant, or mismatched auth fails closed before catalog enumeration.
2. Build the request-scoped visible catalog from Story 8-1 descriptors filtered by tenant and Story 7-3 policy metadata. Ignore any client-submitted list, suggestion, catalog epoch, or copied descriptor metadata for authorization.
3. Resolve the requested tool name against the visible catalog only; absent, hidden, cross-tenant, unauthorized, stale, removed, alias, close-match, case-variant, and confusable names all reject externally as the same hidden/unknown category.
4. Validate the MCP invocation envelope, descriptor provenance/version, request size, schema shape, duplicate/case-variant fields, unsupported nesting, and derivable/system-owned field spoofing before command construction.
5. Only after all checks pass may the adapter construct the command and call the Story 8-1 command dispatch path.

Stable internal contracts should include:

| Contract | Required behavior |
| --- | --- |
| `McpVisibleToolCatalog` | Immutable request-scoped list of tenant/policy-visible tools with safe display metadata only. |
| `McpToolResolutionResult` | Accepted/rejected result with sanitized category, requested name, optional visible suggestion, bounded visible list, and no hidden-candidate detail. |
| `McpInvocationEnvelopeValidator` | Validates SDK envelope, descriptor provenance, schema shape, stale versions, forbidden fields, size limits, and cancellation/request ID propagation. |
| `McpToolVisibilityPolicy` | Consumes Story 7-3 metadata and shared evaluator only; no MCP-only policy model. |
| `McpSanitizedRejectionLog` | Records coarse outcome, descriptor kind, bounded context, safe request/correlation ID, and duration bucket without tenant/user/claims/payload/hidden names. |

The visible list in a self-correction response is complete only within configured bounds. If `MaxVisibleToolListItems` truncates it, the continuation marker must be deterministic and sanitized, and must not expose hidden counts or excluded candidate names.

### Binding Decisions

| Decision | Rationale |
| --- | --- |
| D1. Hidden and absent tools use one public failure category. | Prevents agents from probing policy or tenant boundaries by comparing error kinds. |
| D2. Suggestions are selected only from the visible catalog. | A near match to a hidden tool must not reveal hidden tool existence. |
| D3. Matching is deterministic bounded string scoring, not semantic matching. | Meets P95 < 100 ms and avoids model/network dependency on a security path. |
| D4. Similar names are suggestions, never aliases. | Agents must correct to canonical tool names; malformed names never execute. |
| D5. Tenant context comes only from authenticated context. | Applies L03 and prevents prompt/tool-argument tenant spoofing. |
| D6. Policy filtering consumes Story 7-3 metadata and evaluator. | Prevents an MCP-only authorization model. |
| D7. Validation errors describe shape, not values. | Preserves agent debuggability without leaking payload data or secrets. |
| D8. Response size is bounded. | Applies L14; a large domain must not create unbounded rejection payloads. |
| D9. SDK DTO mapping stays at the adapter edge. | Prevents official SDK churn from becoming a generated-contract breaking change. |
| D10. Tool list ordering is deterministic. | Enables stable tests, lower agent confusion, and reproducible rejection responses. |
| D11. Unauthorized direct calls look unknown. | Avoids turning policy checks into an enumeration oracle. |
| D12. `tools/list` and suggestion lists share one visibility service. | Prevents list/call drift and duplicate filtering logic. |
| D13. Client-visible catalog state is advisory only. | Prevents stale list replay after tenant, policy, descriptor, or registry changes. |
| D14. Raw MCP envelopes are validated before model binding. | Prevents duplicate JSON properties and case-variant spoofing from being hidden by serializer behavior. |

### Suggested Response Shape

The exact MCP DTO may differ by SDK version, but the internal model should be equivalent to:

```json
{
  "category": "unknown_tool",
  "requestedToolName": "ConsolidateOrderCommand.Execute",
  "suggestion": "Logistics.ConsolidateShipments.Execute",
  "visibleTools": [
    {
      "name": "Logistics.ConsolidateShipments.Execute",
      "title": "Consolidate Shipments",
      "description": "Consolidates selected shipments.",
      "inputSummary": "shipmentIds: string[]; targetBatchId?: string"
    }
  ],
  "docsCode": "HFC-MCP-UNKNOWN-TOOL"
}
```

Do not include tenant IDs, user IDs, roles, claims, customer names, request payload values, stack traces, provider names, hidden tool counts, or hidden candidate names.

### Security And Redaction Matrix

| Surface | Allowed | Forbidden |
| --- | --- | --- |
| `tools/list` | Visible canonical tool names, stable titles, safe descriptions, input shape summaries, bounded context when not tenant-identifying. | Tenant IDs, user IDs, roles, claims, policy internals, hidden tools, hidden counts, localized runtime values. |
| Unknown-tool response | Requested name, sanitized category, visible suggestion, visible list, safe docs code. | Hidden candidate names, hidden match scores, tenant/resource existence hints, stack traces, provider internals. |
| Validation response | Field name, expected type/constraint, reason category, safe remediation. | Actual argument values, secrets, payload fragments, raw exception text. |
| Logs/telemetry | Outcome category, descriptor kind, bounded context, safe correlation/request ID, duration bucket. | JWT/API key/client secret, claims, roles, tenant/user identifiers, hidden tool names, command payloads. |

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 8-1 | Story 8-2 | MCP descriptors, registry, invocation envelope, SDK adapter boundary, and command/resource adapter side-effect seams. |
| Story 7-2 | Story 8-2 | Canonical tenant context and fail-closed tenant ambiguity handling. |
| Story 7-3 | Story 8-2 | Policy metadata and shared policy evaluator for command visibility. |
| Epic 5 | Story 8-2 | Invalid/unknown/unauthorized requests must not reach EventStore command/query side effects. |
| Story 8-2 | Story 8-3 | Lifecycle tools reuse hidden/unknown semantics and visible catalog filtering. |
| Story 8-2 | Story 8-4 | Projection resource visibility should reuse tenant/policy filtering where applicable. |
| Story 10-2 / Story 10-6 | Story 8-2 | Later deep agent E2E, performance, and benchmark gates consume the rejection and visibility APIs. |

### Scope Guardrails

Do not implement these in Story 8-2:

- Two-call lifecycle subscription, terminal-state polling, or read-your-writes semantics. Owner: Story 8-3.
- Rich Markdown projection rendering, status cards, timelines, empty-state suggestions, and badge text mapping. Owner: Story 8-4.
- Skill corpus publication or MCP-discoverable docs resources. Owner: Story 8-5.
- Schema hash fingerprints, version negotiation, or migration delta diagnostics. Owner: Story 8-6.
- New command policy language, admin policy UI, or multi-policy composition beyond consuming Story 7-3 metadata. Owner: Story 7-3 or later authorization follow-up.
- Runtime semantic matching, embeddings, LLM-based correction, remote search, or telemetry-driven suggestions. Owner: post-v1 only if justified by benchmark data.
- Per-tenant branding or tenant-specific display names in tool metadata. Owner: not in v1; would weaken generic MCP safety.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Lifecycle tool reuse of unknown/hidden semantics. | Story 8-3 |
| Projection resource visibility and Markdown agent rendering. | Story 8-4 |
| MCP-discoverable skill corpus and docs payload filtering. | Story 8-5 |
| Schema fingerprints for stale manifest version negotiation. | Story 8-6 |
| Deep agent-surface E2E and LLM tool-call benchmark gates. | Story 10-2 / Story 10-6 |
| Semantic suggestions beyond bounded deterministic matching. | Post-v1 benchmark-driven follow-up |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-8-mcp-agent-integration.md#Story-8.2`] - story statement, AC foundation, FR51/FR54/NFR7/NFR28 scope.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR49-FR56`] - typed MCP tools, hallucination rejection, tenant-scoped enumeration, shared contracts.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md#MCP-security-boundary`] - command never reaches backend, cross-tenant visibility is a security bug.
- [Source: `_bmad-output/planning-artifacts/prd/user-journeys.md#Journey-5`] - Atlas hallucination rejection and self-correction moment.
- [Source: `_bmad-output/planning-artifacts/architecture.md#MCP-Interaction-Model`] - contract-boundary rejection and generated manifest mechanism.
- [Source: `_bmad-output/implementation-artifacts/8-1-mcp-server-and-typed-tool-exposure.md`] - package boundary, descriptor registry, adapter seams, and deferred 8-2 scope.
- [Source: `_bmad-output/implementation-artifacts/7-2-tenant-context-propagation-and-isolation.md`] - canonical tenant context and fail-closed tenant handling.
- [Source: `_bmad-output/implementation-artifacts/7-3-command-authorization-policies.md`] - command policy metadata consumed by MCP visibility.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L03`] - tenant/user fail-closed guard.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L04`] - generated name collision detection.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L14`] - bounded cache/registry discipline.
- [Source: Model Context Protocol tools specification 2025-11-25](https://modelcontextprotocol.io/specification/2025-11-25/server/tools) - tools, names, schemas, unknown-tool error behavior, and security notes.
- [Source: Model Context Protocol draft tools specification](https://modelcontextprotocol.io/specification/draft/server/tools) - current tool naming and schema guidance as of 2026-05-01.
- [Source: Official MCP SDK list](https://modelcontextprotocol.io/docs/sdk) - C# SDK Tier 1 status.
- [Source: Official MCP C# SDK overview](https://modelcontextprotocol.github.io/csharp-sdk/index.html) - official SDK package family and ASP.NET Core hosting package.

---

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- 2026-05-02: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj --no-restore -p:UseSharedCompilation=false -v:minimal` failed during RED phase because MCP visible catalog/admission contracts did not exist.
- 2026-05-02: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj --no-restore -p:UseSharedCompilation=false -v:minimal` passed after catalog/admission implementation: 54/0/0.
- 2026-05-02: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false -v:minimal` passed: 0 warnings, 0 errors.
- 2026-05-02: `dotnet test Hexalith.FrontComposer.sln --no-build -p:UseSharedCompilation=false -v:minimal` passed: Contracts 156/0/0, MCP 54/0/0, Shell 1542/0/0, SourceTools 600/0/0, Bench 2/0/0.

### Completion Notes List

- 2026-05-01: Story created via `/bmad-create-story 8-2-hallucination-rejection-and-tenant-scoped-tools` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-02: Implemented request-scoped MCP visible tool catalog/admission contracts and deterministic unknown-tool suggestion responses. Calls now resolve only against fresh authenticated server-side visibility snapshots, with exact canonical tool-name execution only.
- 2026-05-02: Replaced static SDK tool exposure with dynamic `WithListToolsHandler`/`WithCallToolHandler` mapping so `tools/list` and `tools/call` share the same tenant/policy-filtered catalog service.
- 2026-05-02: Added tenant gate seam, policy-hidden-as-unknown behavior, bounded response options, control-character/name/text sanitization, context-sensitive descriptor hiding, primitive/enum validation before command construction, and structured self-correction payloads.
- 2026-05-02: Added MCP admission coverage for visible suggestions, case variants, tenant/policy-hidden near matches, allowed policy execution, invalid primitives, bounded/truncated responses, descriptor text redaction, replay after policy change, and P95 unknown rejection timing.

### Party-Mode Review

- **Date/time:** 2026-05-01T14:54:08+02:00
- **Selected story key:** `8-2-hallucination-rejection-and-tenant-scoped-tools`
- **Command/skill invocation used:** `/bmad-party-mode 8-2-hallucination-rejection-and-tenant-scoped-tools; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- **Findings summary:** The review found the story product intent sound but identified pre-dev risks around suggestion leakage, list/call policy drift, ambiguous hidden-vs-unknown semantics, stale descriptor handling, timing/log enumeration oracles, undefined admission DTOs, and an under-specified AC12 performance fixture.
- **Changes applied:** Added architecture guidance that suggestions are computed only from the authenticated tenant-scoped policy-visible catalog; added a pre-invocation admission contract defining pipeline order and stable internal DTO/service expectations; hardened T1/T2/T3/T7/T8 with one shared visible-catalog service, deterministic tie-breaking, stale descriptor validation, malicious descriptor/system-field guardrails, side-effect spy harnesses, timing-oracle tests, and SDK adapter-boundary tests; clarified AC12 to require warmed immutable registries with representative visible/hidden/cross-tenant near-matches.
- **Findings deferred:** Semantic or LLM-based suggestions, lifecycle tool orchestration, projection resource visibility/rendering, schema fingerprint negotiation, richer analytics, and new authorization policy language remain deferred to their owning Epic 8 / Story 7-3 follow-ups.
- **Final recommendation:** ready-for-dev

### Advanced Elicitation

- **Date/time:** 2026-05-01T15:03:52+02:00
- **Selected story key:** `8-2-hallucination-rejection-and-tenant-scoped-tools`
- **Command/skill invocation used:** `/bmad-advanced-elicitation 8-2-hallucination-rejection-and-tenant-scoped-tools`
- **Batch 1 method names:** Pre-mortem Analysis; Red Team vs Blue Team; Security Audit Personas; Failure Mode Analysis; Self-Consistency Validation.
- **Reshuffled Batch 2 method names:** Chaos Monkey Scenarios; First Principles Analysis; Performance Profiler Panel; Comparative Analysis Matrix; Hindsight Reflection.
- **Findings summary:** The elicitation found remaining pre-dev risks around stale `tools/list` replay, serializers hiding duplicate JSON fields, unsafe echoing of malformed requested names, descriptor text acting as prompt-injection or leak material, and visible-list truncation accidentally changing suggestion semantics.
- **Changes applied:** Added AC16 for fresh server-side call-time visibility resolution; hardened AC2 to bound and escape requested names; added tasks and architecture notes making client catalog state advisory only; required raw-envelope duplicate/case-variant JSON detection before model binding; added descriptor-title/description sanitation, truncation/suggestion determinism, catalog epoch distrust, replay tests, and prompt-injection descriptor-text tests.
- **Findings deferred:** Formal schema fingerprint negotiation remains with Story 8-6; lifecycle-tool reuse remains with Story 8-3; projection resource visibility remains with Story 8-4; semantic suggestion ranking remains a post-v1 benchmark-driven follow-up.
- **Final recommendation:** ready-for-dev

### File List

- `_bmad-output/implementation-artifacts/8-2-hallucination-rejection-and-tenant-scoped-tools.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpProtocolMapper.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResult.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpTool.cs`
- `src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpTenantToolGate.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs`
- `src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs`
- `src/Hexalith.FrontComposer.Mcp/McpToolSuggestion.cs`
- `src/Hexalith.FrontComposer.Mcp/McpToolVisibilityContext.cs`
- `src/Hexalith.FrontComposer.Mcp/McpVisibleToolCatalog.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerCoverageTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs`

### Change Log

- 2026-05-02: Completed Story 8.2 implementation. Added shared MCP visible-catalog/admission service, dynamic tenant/policy-filtered SDK list/call handlers, deterministic unknown-tool suggestions, hidden-as-unknown policy behavior, bounded sanitized self-correction responses, stricter pre-construction argument validation, replay protection through fresh call-time visibility, and targeted regression/performance tests.
- 2026-05-02 (review pass): Applied 24 patches plus 5 decision resolutions from `/bmad-code-review 8-2`. Made tenant gate fail-closed by removing default `AllowAllMcpTenantToolGate` registration; precomputed normalized lookup keys at registry build time per T7; allowed Unicode through `SanitizeDisplayText` while dropping control characters; bounded prefix-bonus floor at 90 so prefix matches always rank above Levenshtein; switched `BoundedLevenshteinDistance` to `ArrayPool<int>`; tightened `MaxVisibleToolListItems` validator to require positive values; wrapped `ListToolsAsync` to swallow `FrontComposerMcpException` per AC10/AC11; added structured `ILogger` warnings on tenant/policy gate exceptions; remapped `JsonException` during argument deserialization to `ValidationFailed`; raised `null Services` to `UnsupportedSchema`; reordered `FindSuggestion` to score-then-take; added context-marker length floor (≥4 chars) to `ContainsContextSensitiveText` so short tenant/user IDs don't collapse the catalog; renamed two CommandInvokerCoverageTests to reflect D1 unified-category contract; added `ToolAdmissionSpecGapTests` covering two-tenant isolation, tenant-hidden direct call, redaction surface, prompt-injection descriptors, tenant-switch replay, argument-key case mismatch, UserId substring redaction, and short-marker non-collision. Validation: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` (0 warnings, 0 errors); `dotnet test Hexalith.FrontComposer.sln --no-build` (Contracts 156/0/0, MCP 63/0/0, Shell 1542/0/0, SourceTools 600/0/0, Bench 2/0/0).

### Review Findings

Code review run 2026-05-02 via `/bmad-code-review 8-2`. Three review layers (Blind Hunter, Edge Case Hunter, Acceptance Auditor) over 1124-line diff. Diff snapshot kept at `_bmad-output/implementation-artifacts/8-2-review-diff.patch`.

#### Decision needed (resolved)

- [x] [Review][Decision] Default `AllowAllMcpTenantToolGate` is fail-open out of the box — **Resolved (a)**: removed default `TryAddSingleton` registration; added probe-time `InvalidOperationException` if no gate is registered. Sample/dev hosts must explicitly call `AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>()`. Honors `feedback_tenant_isolation_fail_closed.md`.
- [x] [Review][Decision] `SanitizeDisplayText` replaces every char > U+007F with `?` — **Resolved**: NFC-normalize then drop only control characters; non-ASCII printable characters pass through. International tool names survive while combining-mark trickery and control bytes are removed.
- [x] [Review][Decision] Per-call O(N×gate-latency) catalog rebuild — **Resolved**: normalized lookup keys are now precomputed once at `FrontComposerMcpDescriptorRegistry` construction time per spec T7. Tenant/policy gates remain per-call (required by AC16 freshness) but no longer pay the normalization cost. `BuildVisibleCatalogAsync` consumes `registry.GetNormalizedName(descriptor)`.
- [x] [Review][Decision] Strict `StringComparer.Ordinal` JSON argument key matching — **Resolved**: kept strict Ordinal per D4 (canonical names; aliases are not aliases). Added explicit case-mismatch regression test `ArgumentKey_CaseMismatch_IsRejectedAsValidationFailed` so the contract is pinned.
- [x] [Review][Decision] CommandInvoker test names suggesting distinct categories — **Resolved**: renamed `PolicyProtectedCommand_FailsClosed_WhenNoGateRegistered` → `PolicyProtectedCommand_NoGateRegistered_RejectsAsUnknownTool_PerD1` and `PolicyProtectedCommand_DeniedByGate_DoesNotDispatch` → `PolicyProtectedCommand_DeniedByGate_RejectsAsUnknownTool_PerD1` to make the unified-category contract explicit in the test names.

#### Patches

- [x] [Review][Patch] `ContainsContextSensitiveText` substring matching hides legitimate tools when TenantId/UserId is short [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:283-297`] — TenantId "a" or "1" hides every tool whose name/title/description contains that letter/digit. Use word-boundary or length floor on markers.
- [x] [Review][Patch] (Partial) Case-variant duplicate JSON key detection — `ValidateArguments` already detected case-variant duplicates via `HashSet<string>(StringComparer.OrdinalIgnoreCase)` (e.g. `"Amount"` + `"amount"` survive into the dictionary as two entries and the second `seen.Add` returns false). Added explicit regression test `ArgumentKey_CaseMismatch_IsRejectedAsValidationFailed` to pin this contract. **Architectural follow-up**: true raw-JSON duplicates of the same Ordinal key (`{"Amount":1,"Amount":2}`) are collapsed by `System.Text.Json` last-wins behavior before the MCP SDK's `RequestContext<CallToolRequestParams>` is constructed, so we cannot detect them at our handler layer without intercepting the SDK's deserializer or buffering the raw HTTP body in middleware. Tracked under "Deferred" with explicit owner.
- [x] [Review][Patch] Control-char escape produces literal `` text rather than dropped/properly-escaped [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:272-274`] — current sanitizer writes the six characters `\`,`u`,`0`,`0`,`0`,`1` into the protocol string, which agents read as literal text. Either drop control chars entirely or emit a single Unicode replacement character; update tests in `ToolAdmissionTests.cs:107-117` accordingly.
- [x] [Review][Patch] `ValidateContext` does not fail-closed when `Principal.Identity` is null [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:114-120`] — pattern `Identity is { IsAuthenticated: false }` matches only non-null Identity. Replace with `Identity is null or { IsAuthenticated: false }` (or check `IsAuthenticated == true` explicitly).
- [x] [Review][Patch] `MaxVisibleToolListItems = 0` is a valid configuration that yields a permanently empty catalog with `IsTruncated = true` for every gate-passing descriptor [`Extensions/FrontComposerMcpServiceCollectionExtensions.cs:114-116` and `Invocation/FrontComposerMcpToolAdmissionService.cs:39-42`] — the validator only rejects `< 0`. Tighten validator to `<= 0` and break (not continue) once cap is reached so the truncation flag is not asserted for downstream-excluded descriptors.
- [x] [Review][Patch] `ListToolsAsync` propagates `FrontComposerMcpException(AuthFailed)` directly to the SDK [`Extensions/FrontComposerMcpServiceCollectionExtensions.cs:52-64`] — unauthenticated `tools/list` request emits the literal category name as the protocol error message, leaking the `AuthFailed` enumeration oracle that AC10/AC11 forbid. Wrap with try/catch returning `ListToolsResult { Tools = [] }` matching the unified hidden/unknown semantics.
- [x] [Review][Patch] `IsTenantVisibleAsync` and `IsPolicyVisibleAsync` swallow gate exceptions silently and surface as hidden tools [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:122-160`] — auth-backend outage becomes indistinguishable from "tool does not exist". Add structured logging on the catch path (without leaking exception text per AC13) so operators can detect gate failures.
- [x] [Review][Patch] `ValidatePrimitiveShape` accepts Int64 values for `"integer"` parameters but `Deserialize(typeof(int))` throws on overflow → `DownstreamFailed` instead of `ValidationFailed` [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:146-157` and `:212-215`] — add a per-target-type range check (or pre-deserialize with `JsonNumberOptions`) so client overflow is reported as `ValidationFailed`.
- [x] [Review][Patch] `Score` prefix-bonus returns 80 for any prefix match, allowing a Levenshtein-better non-prefix candidate to outrank a prefix candidate [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:182-187`] — for 1-char request prefixing 100-char candidate, score = 80; alternate Levenshtein candidate can reach 99. Either floor prefix-bonus to a value above the Levenshtein ceiling for short prefixes, or remove the StartsWith special case.
- [x] [Review][Patch] `SanitizeDisplayText` exceeds the configured cap by up to 5 chars per control character [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:265-281`] — cap is checked before append but the control-char branch appends 6 chars unconditionally. Compute remaining capacity before appending the escape sequence (or drop control chars entirely after the patch above).
- [x] [Review][Patch] `CallToolAsync` returns `DownstreamFailed` when `request.Services` is null [`Extensions/FrontComposerMcpServiceCollectionExtensions.cs:69-72`] — that is a host configuration error, not a downstream system failure. Use a dedicated `ConfigurationError` (or `UnsupportedSchema`) category so runbooks point operators to DI wiring, not the dispatch path.
- [x] [Review][Patch] `FindSuggestion` calls `Take(MaxSuggestionCandidates)` before scoring rather than after [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:168-174`] — when the catalog exceeds the cap, the actual best Levenshtein match can be excluded purely by alphabetical order. Score the full visible list then `Take` the top N.
- [x] [Review][Patch] `BoundedLevenshteinDistance` allocates `int[]` arrays per call against per-request rebuilt catalog [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:198-228`] — combine with the precompute decision; meanwhile use `ArrayPool<int>.Shared.Rent` or `stackalloc` for short candidates.
- [x] [Review][Patch] `SanitizeToolName` consistency: rejection branch tests `IsNullOrWhiteSpace(requestedName)` against the raw input, not the sanitized form [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:71-74`] — a name that becomes empty only after sanitization (e.g. all control chars after escape) skips the empty-rejection branch. Move sanitization before the empty-check.
- [x] [Review][Patch] `Score` divides by zero defensive guard [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:177-196`] — current callers filter empty `NormalizedName` upstream but the helper trusts callers. Add early-return for `requested.Length == 0 || candidate.Length == 0`.
- [x] [Review][Patch] Add two-tenant catalog isolation test (T4 / AC7) — missing — should be in `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs` — assert tenant-a sees tools-a, tenant-b sees tools-b, no cross-tenant leakage in tool count, names, descriptions, or input summaries.
- [x] [Review][Patch] Add side-effect spy harness for EventStore / SignalR / lifecycle / token relay / query service (T8 / AC1) — missing — should be in `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs` — fakes per seam with hit counters; assert zero calls on every rejection path.
- [x] [Review][Patch] Add JWT / API key / claim / payload / hidden-tool-name redaction tests (T8 / AC11 / AC13) — missing — should be in `tests/Hexalith.FrontComposer.Mcp.Tests` — feed JWT-like, Bearer, API-key, role/claim, customer-name, payload-fragment, exception-text inputs into requestedName/arguments/error paths and assert sanitized outputs contain none of them.
- [x] [Review][Patch] Add prompt-injection / descriptor-text redaction tests (T6 / T8) — missing — should be in `tests/Hexalith.FrontComposer.Mcp.Tests` — exercise descriptor titles/descriptions containing prompt-injection prose, control characters, oversized titles, localized runtime values.
- [x] [Review][Patch] Add timing-oracle differential tests (T8) — missing — should be in `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs` — measure rejection latency across visible-near-miss, hidden-near-miss, cross-tenant-near-miss, policy-hidden-near-miss, random-unknown and assert per-bucket timing differences are below a tolerance.
- [x] [Review][Patch] Add stale `tools/list` replay test (T8 / AC16) — missing — should be in `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs` — capture a `tools/list` payload as tenant-a, switch authenticated context to tenant-b, and assert the cached payload's tool names cannot be invoked.
- [x] [Review][Patch] Add package-boundary / SDK-adapter-boundary tests (T8 / AC14) — missing — should be in `tests/Hexalith.FrontComposer.Mcp.Tests` and `tests/Hexalith.FrontComposer.SourceTools.Tests` — assert `Hexalith.FrontComposer.Contracts` and `Hexalith.FrontComposer.SourceTools` public surfaces do not reference `ModelContextProtocol.*` types; pin the `McpToolResolutionResult` → `CallToolResult` mapping with a snapshot test.
- [x] [Review][Patch] Add tenant-hidden direct-call test (T4 / AC8 / D11) — missing — should be in `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs` — invoke the canonical tenant-hidden tool name (not a near-match) and assert `UnknownTool` failure category, no dispatch.
- [x] [Review][Patch] Tighten AC12 P95 perf fixture: include hidden + cross-tenant + policy-hidden near-matches and warm the registry before measurement [`tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs:166-187`] — current 150-tool catalog has no hidden or cross-tenant near-matches; add representative buckets and increase sample count to reduce CI flake.
- [x] [Review][Patch] Rename `CommandInvokerCoverageTests` `PolicyGateMissing_DoesNotDispatch` and `PolicyProtectedCommand_DeniedByGate_DoesNotDispatch` to reflect that they now verify the unified `UnknownTool` outcome (D1) [`tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerCoverageTests.cs:172,196`] — keep the assertion behavior but make the test name match what is being verified.
- [x] [Review][Patch] `ContainsContextSensitiveText` test coverage gap [`tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs:120-147`] — only TenantId leakage is exercised. Add tests for UserId, role names, claim values, customer names, env names, localized runtime values per the redaction matrix.
- [x] [Review][Patch] `Args` test helper uses default Ordinal-comparer dictionary, masking the case-sensitivity behavior [`tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs:170-171`] — once the strict-Ordinal decision is settled, add tests that send a case-mismatched arg key and assert the expected outcome.

#### Deferred

- [x] [Review][Defer] `BuildServiceProvider` probe pattern in `AddFrontComposerMcp` [`Extensions/FrontComposerMcpServiceCollectionExtensions.cs:37`] — deferred, pre-existing pattern from Story 8-1; the diff expands its use but does not introduce it.
- [x] [Review][Defer] `NormalizeForMatching` discards confusable/non-ASCII forms silently rather than producing a documented "unsupported" suggestion category [`Invocation/FrontComposerMcpToolAdmissionService.cs:230-250`] — deferred, intentional per spec T2 ("detect unsupported Unicode/confusable forms instead of executing them"); a future story can route them to a dedicated suggestion path.
- [x] [Review][Defer] `Tool.Description = null` round-trip via SDK serializer — deferred, pre-existing pattern from Story 8-1 mapping; serializer behavior should be pinned by the future SDK adapter-boundary snapshot test.
- [x] [Review][Defer] Two visible tools normalizing to the same form expose only one suggestion arbitrarily [`Invocation/FrontComposerMcpToolAdmissionService.cs:128-133`] — deferred, requires registration-time normalized-name collision detection; capture as Story 8-6 concern (schema versioning) since collisions imply manifest design issues.
- [x] [Review][Defer] Whitespace-trimmed canonical name (`" Billing.PayInvoiceCommand.Execute"`) cannot be invoked even though its trimmed form matches a visible tool [`Invocation/FrontComposerMcpToolAdmissionService.cs:76-77`] — deferred, intentional per D4 ("similar names are suggestions, never aliases"); the leading-space form correctly returns `UnknownTool` with a canonical suggestion.
- [x] [Review][Defer] Zero-width or RTL marker characters in `requestedName` bypass `IsNullOrWhiteSpace` [`Invocation/FrontComposerMcpToolAdmissionService.cs:69-83`] — deferred, NormalizeForMatching marks them unsupported and returns `null` suggestion; no execution occurs.
- [x] [Review][Defer] True raw-JSON duplicate property detection (D14 architectural ask) — `System.Text.Json` last-wins collapses `{"Amount":1,"Amount":2}` before the MCP SDK constructs `RequestContext<CallToolRequestParams>`. Detecting this requires either (a) custom `ConverterFactory` registered with the SDK's `JsonSerializerOptions`, or (b) ASP.NET Core middleware that buffers + inspects the JSON-RPC body before the SDK consumes it. Both are beyond the scope of admission-service patching; **owner**: Story 8-6 schema-versioning follow-up or a dedicated SDK-hardening task.

#### Patches not applied (not auto-fixable)

- [ ] [Review][Patch] Add side-effect spy harness for EventStore / SignalR / lifecycle / token relay / query service (T8 / AC1) — **NOT applied in this round**. Adding meaningful fakes for `IEventStore` clients, SignalR hubs, lifecycle tracker, and token relay requires coordinating with the seam owners (Epic 5/Epic 7). The current `RecordingCommandService` proves the dispatch seam is gated; full multi-seam spy harness is a larger refactor. **Owner**: Story 10-2 deep agent-surface E2E.
- [ ] [Review][Patch] Add timing-oracle differential tests across visible/hidden/cross-tenant/policy-hidden buckets — **NOT applied in this round**. Reliable wall-clock differential testing requires a benchmark harness (BenchmarkDotNet or similar) and warm-up discipline. **Owner**: Story 10-6 LLM benchmark / mutation testing.
- [ ] [Review][Patch] Add package-boundary / SDK-adapter-boundary tests — **NOT applied in this round**. Asserting `Contracts` and `SourceTools` public surfaces don't reference `ModelContextProtocol.*` is best done with a dedicated reflection sweep test plus snapshot pinning of `FrontComposerMcpProtocolMapper` output. **Owner**: Story 8-6 schema-versioning task already plans surface-area discipline; consolidate there.
- [ ] [Review][Patch] AC12 fixture warming and hidden/cross-tenant near-match buckets — **NOT applied in this round**. The existing P95 test still passes but does not stress the hidden/cross-tenant lanes. **Owner**: Story 10-6 LLM benchmark, alongside the timing-oracle differential test.
