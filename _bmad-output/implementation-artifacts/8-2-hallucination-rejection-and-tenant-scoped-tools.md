# Story 8.2: Hallucination Rejection & Tenant-Scoped Tools

Status: ready-for-dev

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
| AC2 | An unknown tool is rejected | The rejection response is returned | The response includes a sanitized error category, the requested name, the closest matching visible tool name when one clears the deterministic match threshold, and the complete visible tenant-scoped tool list. |
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

---

## Tasks / Subtasks

- [ ] T1. Define visible tool catalog and matching contracts (AC1-AC4, AC7-AC9, AC15)
  - [ ] Add MCP-package contracts for a request-scoped `McpVisibleToolCatalog`, `McpToolVisibilityContext`, and `McpToolSuggestion` or equivalent names.
  - [ ] Define an explicit admission result contract such as `McpToolResolutionResult` with `accepted | rejected`, sanitized rejection category, requested name, optional visible suggestion, and bounded visible list fields.
  - [ ] Ensure `tools/list`, unknown-tool rejection, hidden direct calls, and validation helper lists all consume the same visible-catalog service; do not duplicate policy filtering in separate list and call paths.
  - [ ] Build the visible catalog from Story 8-1 generated descriptors plus authenticated tenant/policy context; do not scan assemblies or ask services for runtime domain type discovery.
  - [ ] Keep catalog entries immutable after construction and bounded by explicit options such as `MaxVisibleToolListItems`, `MaxSuggestionCandidates`, and `MaxToolNameLength`.
  - [ ] Define one canonical hidden outcome for tools not visible because they are absent, stale, cross-tenant, policy-hidden, or removed. Do not distinguish these cases in agent-visible responses.

- [ ] T2. Implement deterministic unknown-tool rejection (AC1-AC4, AC11-AC14)
  - [ ] Normalize requested and candidate names for matching only: case fold invariantly, normalize separators, trim protocol-safe whitespace, reject control characters, and detect unsupported Unicode/confusable forms instead of executing them.
  - [ ] Use a deterministic low-cost algorithm such as bounded edit distance or prefix/token scoring over only visible candidates. Avoid semantic/LLM matching in v1.
  - [ ] Return at most one best suggestion when it clears a documented threshold; otherwise return no suggestion but still include the visible tool list.
  - [ ] Apply deterministic tie-breaking for equal scores and duplicate visible display labels; the same request and same visible catalog must always produce the same suggestion/null result.
  - [ ] Reject direct execution of aliases, stale names, close matches, case variants, and cosmetic variants; the agent must call the canonical visible tool name.
  - [ ] Add tests proving hidden candidates never influence suggested names, ranking, thresholds, timing-visible branches, telemetry labels, logs, or response copy.

- [ ] T3. Harden schema validation and invocation envelope normalization (AC5, AC6, AC13)
  - [ ] Reuse Story 8-1 JSON Schema/descriptor metadata as the validation source of truth.
  - [ ] Validate stale descriptor/version/registry epoch mismatches before command construction and map them to the same agent-visible hidden/unknown category unless a safe schema-version category is explicitly documented.
  - [ ] Reject missing required fields, wrong primitive types, enum mismatches, unsupported nested JSON, unknown properties, duplicate property names, case-variant spoofing, oversized argument objects, and unsupported CLR categories before command construction.
  - [ ] Reject all derivable/system-owned fields (`TenantId`, `UserId`, message/correlation IDs, policy IDs, cache keys, lifecycle values) even when the generated schema omits them.
  - [ ] Add a malicious-descriptor guardrail: derivable/system-owned fields fail closed even if a malformed generated descriptor or stale manifest attempts to expose them as ordinary arguments.
  - [ ] Produce parameter-level diagnostics such as `field`, `reason`, and `expectedShape` without echoing actual values or secrets.
  - [ ] Preserve cancellation tokens and request IDs through rejection paths without allowing cancellation to skip cleanup or telemetry finalization.

- [ ] T4. Enforce tenant-scoped enumeration (AC7, AC10-AC13)
  - [ ] Resolve the active tenant only from the authenticated Story 7-2 tenant context. Tool arguments, headers not owned by auth, query strings, and prompt-provided text are never tenant sources.
  - [ ] Fail closed when tenant context is absent, empty, whitespace, ambiguous, mismatched, or from an untrusted source.
  - [ ] Filter `tools/list`, unknown-tool suggestions, validation failure helper lists, and any discovery cache from the same visibility service.
  - [ ] Add two-tenant tests proving each tenant sees only its own commands and cannot infer the other tenant's tool count, names, descriptions, or policy metadata.

- [ ] T5. Enforce command policy metadata visibility (AC8, AC9, AC13)
  - [ ] Consume Story 7-3 policy metadata from generated descriptors; do not add an MCP-only policy attribute or policy language.
  - [ ] Use the shared authorization evaluator from Story 7-3 when available. If the evaluator is incomplete, implement the smallest adapter needed and record any gap as a deferred decision.
  - [ ] Hide unauthorized tools from list and suggestions. Direct calls to policy-hidden tools receive the same hidden/unknown category as absent tools.
  - [ ] Add tests for no-policy, allowed-policy, denied-policy, missing-policy-service, policy-evaluator-exception, and multiple-policy future-proofing behavior.

- [ ] T6. Build sanitized self-correction responses (AC2, AC3, AC11, AC13, AC14)
  - [ ] Define a stable response model for unknown names and validation errors inside the MCP package, then map it to official SDK response/error DTOs at the adapter edge.
  - [ ] Include only protocol name, sanitized category, canonical suggestion if visible, visible tool names/signatures/descriptions, and safe docs/remediation text.
  - [ ] Ensure domain-generic names: no tenant prefix/suffix, customer code, environment name, role value, claim value, localized runtime text, or payload-derived label.
  - [ ] Cap response size and truncate the visible list deterministically with a sanitized continuation marker when needed.

- [ ] T7. Performance and bounded-state implementation (AC12, AC15)
  - [ ] Precompute immutable normalized lookup keys at startup or descriptor-registry build time; do not allocate unbounded per-request dictionaries.
  - [ ] Bound candidate scoring by visible catalog size and configured limits.
  - [ ] Add a timer-based unit/performance test for unknown-name rejection P95 < 100 ms using a representative generated catalog with warmed immutable registries, fixed descriptor counts, visible near-misses, hidden near-misses, and random unknown names.
  - [ ] Add stress tests for long names, many near-matches, empty visible catalog, all tools hidden, repeated requests, and cancellation.

- [ ] T8. Tests and verification (AC1-AC15)
  - [ ] MCP tests for unknown tool rejection before side effects using fake `ICommandService`, fake EventStore clients, fake lifecycle tracker, fake query service, and side-effect counters.
  - [ ] Side-effect spy harness asserting rejected paths make zero calls to command construction, backend validation services, `ICommandService`, EventStore serialization/client calls, token relay, lifecycle/cache mutation, SignalR, and sensitive telemetry enrichment.
  - [ ] Visibility tests for two tenants, no tenant, tenant mismatch, denied policy, allowed policy, hidden direct call, and hidden near-match.
  - [ ] Schema tests for required/optional fields, primitive mismatch, enum mismatch, unknown fields, duplicate/case-variant fields, derivable field spoofing, oversized payload, and unsupported nested shape.
  - [ ] Redaction tests with JWT-like strings, API keys, role names, claim values, tenant IDs, user IDs, customer names, payload fragments, exception text, provider internals, and hidden tool names.
  - [ ] Determinism tests for candidate ordering, tie-breaking, normalized-name handling, response ordering, truncation, and repeated build output.
  - [ ] Timing-oracle tests comparing visible near-miss, hidden near-miss, cross-tenant near-miss, policy-hidden near-miss, and random unknown rejection paths without exposing hidden candidates through duration buckets.
  - [ ] Package-boundary tests proving `Contracts` and SourceTools public surfaces do not reference MCP SDK DTOs.
  - [ ] SDK adapter-boundary tests proving internal rejection contracts and snapshots remain stable when mapped through official MCP SDK response/error DTOs.
  - [ ] Regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.
  - [ ] Targeted tests: `tests/Hexalith.FrontComposer.Mcp.Tests`, `tests/Hexalith.FrontComposer.SourceTools.Tests`, plus Shell/EventStore tests only if shared seams change.

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
2. Build the request-scoped visible catalog from Story 8-1 descriptors filtered by tenant and Story 7-3 policy metadata.
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

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-01: Story created via `/bmad-create-story 8-2-hallucination-rejection-and-tenant-scoped-tools` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### Party-Mode Review

- **Date/time:** 2026-05-01T14:54:08+02:00
- **Selected story key:** `8-2-hallucination-rejection-and-tenant-scoped-tools`
- **Command/skill invocation used:** `/bmad-party-mode 8-2-hallucination-rejection-and-tenant-scoped-tools; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- **Findings summary:** The review found the story product intent sound but identified pre-dev risks around suggestion leakage, list/call policy drift, ambiguous hidden-vs-unknown semantics, stale descriptor handling, timing/log enumeration oracles, undefined admission DTOs, and an under-specified AC12 performance fixture.
- **Changes applied:** Added architecture guidance that suggestions are computed only from the authenticated tenant-scoped policy-visible catalog; added a pre-invocation admission contract defining pipeline order and stable internal DTO/service expectations; hardened T1/T2/T3/T7/T8 with one shared visible-catalog service, deterministic tie-breaking, stale descriptor validation, malicious descriptor/system-field guardrails, side-effect spy harnesses, timing-oracle tests, and SDK adapter-boundary tests; clarified AC12 to require warmed immutable registries with representative visible/hidden/cross-tenant near-matches.
- **Findings deferred:** Semantic or LLM-based suggestions, lifecycle tool orchestration, projection resource visibility/rendering, schema fingerprint negotiation, richer analytics, and new authorization policy language remain deferred to their owning Epic 8 / Story 7-3 follow-ups.
- **Final recommendation:** ready-for-dev

### File List

(to be filled in by dev agent)
