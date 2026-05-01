# Story 8.3: Two-Call Lifecycle & Agent Command Semantics

Status: ready-for-dev

> **Epic 8** - MCP & Agent Integration. Covers **FR52**, **FR57**, **NFR6**, and **NFR44-NFR47**. Builds on Story **8-1** MCP package/descriptor/adapter seams, Story **8-2** hallucination rejection and tenant-scoped visibility, Epic 5 command/query/EventStore reliability, and the existing web lifecycle services. Applies lessons **L03**, **L06**, **L08**, **L10**, **L12**, and **L14**.

---

## Executive Summary

Story 8-3 makes MCP command execution usable for agents without inventing a second lifecycle model:

- Add a two-call command pattern: `tools/call` for the command returns a safe acknowledgment, and a separate lifecycle tool/resource returns state transitions until a terminal outcome.
- Reuse existing FrontComposer command lifecycle contracts and pending-command resolution behavior instead of creating an MCP-only state machine.
- Guarantee exactly one terminal outcome for each accepted command: `Confirmed`, `Rejected`, or `IdempotentConfirmed`.
- Return structured rejection and idempotency payloads that agents can parse without string matching.
- Preserve Story 8-2 admission checks: unknown, hidden, unauthorized, stale, malformed, and cross-tenant calls still fail before lifecycle registration or backend side effects.
- Prove agent command-to-projection read-your-writes P95 < 1500 ms on localhost Aspire topology.

---

## Story

As an LLM agent,
I want to issue commands with the same lifecycle semantics as the web surface, using a two-call pattern that gives me an acknowledgment and a way to track state transitions,
so that I can reliably submit commands and wait for confirmed outcomes without polling blindly.

### Adopter Job To Preserve

An adopter should be able to expose command tools to an agent and trust that command submission, idempotency, rejection, terminal-state delivery, and read-your-writes behavior match the web surface. The agent should not need to infer success from projection guesses, retry blindly, or parse human-only prose to decide whether to retry, abort, or take an alternative action.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | An authenticated and authorized agent issues a valid command through an MCP command tool | Story 8-2 admission, schema, tenant, and policy checks pass | The command is dispatched once through the existing FrontComposer command path and the first call returns an acknowledgment representing `Acknowledged` lifecycle state. |
| AC2 | The first call acknowledges a command | The MCP response is returned | The response includes a non-empty ULID `messageId`, a stable `correlationId`, a lifecycle subscription URI/tool reference, current state `Acknowledged`, retry-after/polling guidance when available, and no command payload values, tenant IDs, user IDs, tokens, claims, role names, provider internals, or stack traces. |
| AC3 | The agent has the subscription URI or lifecycle tool name from the acknowledgment | The agent calls the lifecycle tracking surface with the correlation ID or message ID | The response exposes ordered transitions from `Acknowledged` through `Syncing` to exactly one terminal state: `Confirmed`, `Rejected`, or `IdempotentConfirmed`. |
| AC4 | A lifecycle tracking request is made for a known accepted command | The command is still in progress | The response returns the latest known state, safe elapsed/next-poll metadata, and bounded transition history without registering duplicate commands or mutating command state. |
| AC5 | A command reaches a terminal outcome | The lifecycle tracking surface is called repeatedly or concurrently | The same terminal outcome is returned idempotently; duplicate terminal observations do not create duplicate user-visible or agent-visible outcomes. |
| AC6 | A command is rejected by domain logic | The agent reads the terminal outcome | The response includes structured fields: `errorCode`, `entityId` when safe and framework-controlled, `message`, `dataImpact`, `suggestedAction`, `retryAppropriate`, and `reasonCategory`, plus human-readable copy following "[What failed]: [Why]. [What happened to the data]." |
| AC7 | A rejected-but-intent-fulfilled command is observed | The terminal outcome is resolved through idempotency or duplicate detection | The lifecycle result is `IdempotentConfirmed`, distinct from `Rejected`, with copy equivalent to "This [entity] was already [action] (by another user). No action needed." and structured `retryAppropriate: false`. |
| AC8 | The command lifecycle is compared with the web surface | Web and MCP command paths are inspected | Both use the same conceptual states: `Idle`, `Submitting`, `Acknowledged`, `Syncing`, and terminal `Confirmed`/`Rejected`, with MCP adding an agent-facing `IdempotentConfirmed` terminal result only where existing idempotency resolution proves intent fulfilled. |
| AC9 | A lifecycle tracking request references an unknown, stale, cross-tenant, policy-hidden, unauthorized, expired, malformed, or already-evicted command | The request reaches the MCP lifecycle boundary | The response fails closed using Story 8-2 hidden/unknown semantics and never reveals whether a hidden command exists in another tenant or policy scope. |
| AC10 | Authentication, tenant context, or authorization changes after the first command acknowledgment | The agent calls the lifecycle tracking surface | The request is re-evaluated against the current authenticated tenant/policy context; stale subscription URIs, copied message IDs, and previous visibility snapshots are not proof of authorization. |
| AC11 | SignalR, EventStore status lookup, projection reconciliation, or polling is delayed or unavailable | The lifecycle guarantee runs | The command either reaches a terminal outcome through the available reconciliation path or returns a bounded retryable in-progress result until the configured timeout, after which it returns a sanitized `NeedsReview`/`TimedOut` category rather than silently disappearing. |
| AC12 | Command-to-projection read-your-writes is measured on localhost Aspire topology | The agent submits a command, observes terminal confirmation, and reads the affected projection | P95 is below 1500 ms for the v1 benchmark lane, with the benchmark documenting command type, projection read path, warm/cold setup, and timing boundaries. |
| AC13 | Rejection, lifecycle, idempotency, timeout, and downstream failure telemetry is emitted | Logs/spans are inspected | Telemetry contains safe correlation/message identifiers, descriptor kind, state/outcome category, duration bucket, and sanitized failure category only. It excludes raw payloads, claims, tokens, tenant/user identifiers, hidden names, stack traces, and provider internals. |
| AC14 | MCP SDK response DTOs differ across transports or versions | The lifecycle adapter maps internal lifecycle results to SDK output | Internal FrontComposer lifecycle contracts and tests remain stable; SDK DTO mapping stays inside `Hexalith.FrontComposer.Mcp`. |
| AC15 | Story 8-3 completes | Story 8-4 and later Epic 8 work continue | The lifecycle acknowledgment, tracking, terminal outcome, and structured rejection contracts are reusable by projection rendering, skill corpus guidance, schema versioning, and future agent E2E benchmarks without redesign. |

---

## Tasks / Subtasks

- [ ] T1. Define agent lifecycle contracts inside the MCP package (AC1-AC5, AC8, AC14, AC15)
  - [ ] Add internal or package-owned records such as `McpCommandAcknowledgement`, `McpLifecycleSubscription`, `McpLifecycleSnapshot`, `McpLifecycleTransitionDto`, and `McpTerminalOutcome`.
  - [ ] Keep these contracts SDK-neutral until the final adapter edge; do not expose MCP SDK DTOs from Contracts, SourceTools, or generated descriptors.
  - [ ] Model terminal outcomes as `Confirmed`, `Rejected`, `IdempotentConfirmed`, and bounded failure categories such as `TimedOut` or `NeedsReview`; do not overload `CommandLifecycleState.Rejected` for idempotent success.
  - [ ] Include `messageId`, `correlationId`, state, terminal outcome, safe retry guidance, and optional subscription URI/tool reference. Exclude raw command payload and raw principal/tenant data.
  - [ ] Define deterministic JSON serialization ordering for snapshots and terminal outcomes to support approval tests.

- [ ] T2. Wire command acknowledgment to the existing command dispatch path (AC1, AC2, AC8, AC13)
  - [ ] Invoke command tools only after Story 8-2 visible-catalog, schema, tenant, and policy admission succeeds.
  - [ ] Route accepted commands through `ICommandService` / `ICommandServiceWithLifecycle` and existing EventStore dispatch behavior; do not create an MCP-only backend client.
  - [ ] Generate or reuse the framework-controlled ULID message ID and correlation ID according to existing lifecycle/idempotency contracts.
  - [ ] Register the accepted command in the bounded pending/lifecycle store only after backend acknowledgment, and only once per accepted command.
  - [ ] Preserve cancellation tokens and request IDs through admission, command dispatch, acknowledgment, lifecycle registration, and telemetry finalization.
  - [ ] If the registered command service cannot provide lifecycle callbacks where this story requires them, fail loudly with a sanitized configuration category rather than pretending terminal tracking is available.

- [ ] T3. Add the lifecycle tracking MCP surface (AC3-AC5, AC9-AC11, AC14)
  - [ ] Add a lifecycle tool or resource template with a stable name/URI such as `frontcomposer.lifecycle.subscribe` or `frontcomposer://lifecycle/{correlationId}`; document the chosen shape in the manifest.
  - [ ] Accept only framework-issued correlation/message identifiers from the acknowledgment. Reject raw tenant/user, policy, command payload, projection filter, or descriptor override inputs.
  - [ ] Re-check authenticated tenant and policy visibility on every lifecycle request. A prior acknowledgment or subscription URI never bypasses current authorization.
  - [ ] Return the latest snapshot plus bounded transition history. Bound history length, response size, and wait/long-poll duration with explicit options.
  - [ ] Support concurrent and repeated lifecycle calls without duplicate registration, duplicate dispatch, duplicate terminal outcomes, or unbounded per-agent state.
  - [ ] Use the same hidden/unknown public category for unknown, expired, evicted, cross-tenant, policy-hidden, stale, and unauthorized tracking requests.

- [ ] T4. Bridge lifecycle observations and terminal guarantees (AC3-AC5, AC7, AC11)
  - [ ] Reuse `ILifecycleStateService`, `ICommandServiceWithLifecycle`, `IPendingCommandStateService`, and `IPendingCommandOutcomeResolver` where possible.
  - [ ] Define one adapter that translates web lifecycle transitions and pending-command terminal observations into MCP lifecycle snapshots.
  - [ ] Guarantee one terminal outcome per accepted command by honoring existing duplicate terminal suppression and idempotency handling.
  - [ ] Treat `PendingCommandTerminalOutcome.IdempotentConfirmed` as agent-facing success, not rejection.
  - [ ] Add timeout and eviction behavior that returns a sanitized `TimedOut`/`NeedsReview` terminal category only after configured bounds are exceeded; do not silently drop the command.
  - [ ] Avoid a request-time unbounded dictionary of per-agent subscriptions. Any pending/lifecycle state added in MCP must have explicit capacity, TTL, and eviction diagnostics.

- [ ] T5. Structure rejection and idempotency payloads for agents (AC6, AC7, AC13)
  - [ ] Add a stable rejection result schema with `errorCode`, `entityId`, `message`, `dataImpact`, `suggestedAction`, `retryAppropriate`, `reasonCategory`, and optional safe docs code.
  - [ ] Map `CommandRejectedException`, EventStore rejection classification, pending-command terminal observations, validation failures, and idempotency outcomes into this schema without string matching by agents.
  - [ ] Preserve the human-readable format "[What failed]: [Why]. [What happened to the data]." for the `message`/copy fields.
  - [ ] For idempotent success, emit `IdempotentConfirmed` with no retry recommendation and with success-oriented data-impact copy.
  - [ ] Never echo raw exception text, command payload values, secret fields, provider messages, claims, roles, tenant/user IDs, or hidden tool names.
  - [ ] Add compatibility notes for existing web rejection text so web and agent surfaces stay semantically aligned even if presentation differs.

- [ ] T6. Enforce read-your-writes flow for agents (AC11, AC12, AC15)
  - [ ] Define the recommended agent sequence: call command tool, call lifecycle tracking until terminal, then read the projection resource through Story 8-4-compatible query paths.
  - [ ] Ensure terminal `Confirmed` means projection reconciliation has reached the framework-observable point required for a safe follow-up projection read, or return `Syncing`/retry metadata until that is true.
  - [ ] Reuse EventStore ETag, SignalR nudge, polling, and pending-command reconciliation behavior from Epic 5; do not add an MCP-specific polling loop that bypasses existing retry/degraded classification.
  - [ ] Add benchmark hooks for command submit start, acknowledgment, terminal observation, projection read request, and projection read completion.
  - [ ] Keep projection rendering itself minimal here. Rich Markdown tables/cards/timelines remain Story 8-4.

- [ ] T7. Harden security, redaction, and replay boundaries (AC2, AC9, AC10, AC13)
  - [ ] Treat subscription URIs as opaque handles scoped to current auth/tenant/policy context; they are not bearer secrets and not proof of authorization.
  - [ ] Reject lifecycle inputs that attempt to override tenant, user, policy, descriptor, command payload, lifecycle state, or retry metadata.
  - [ ] Sanitize all message/correlation IDs in responses and logs; bound length and allowed characters.
  - [ ] Do not reveal evicted command counts, hidden tenant existence, hidden policy names, denied tool names, or whether a correlation ID was once valid for another principal.
  - [ ] Add stale replay tests for copied acknowledgment payloads after sign-out, tenant switch, policy change, descriptor rebuild, and lifecycle-store eviction.

- [ ] T8. Tests and verification (AC1-AC15)
  - [ ] MCP command tests for valid command acknowledgment, shape of subscription URI/tool reference, ULID/correlation presence, and no payload/tenant/principal leakage.
  - [ ] Lifecycle tracking tests for in-progress snapshots, ordered transitions, terminal `Confirmed`, terminal `Rejected`, terminal `IdempotentConfirmed`, repeated reads, concurrent reads, and duplicate terminal suppression.
  - [ ] Zero-side-effect tests proving unknown/hidden/unauthorized/stale lifecycle tracking requests do not dispatch commands, mutate lifecycle state, query EventStore, relay tokens, mutate cache, or emit sensitive telemetry.
  - [ ] Rejection schema tests covering domain rejection, validation rejection, retryable conflict, non-retryable conflict, idempotent success, timeout, cancellation, and downstream exception categories.
  - [ ] Replay/security tests covering tenant switch, policy loss, sign-out, expired auth, stale subscription URI, copied message ID, evicted lifecycle entry, hidden direct correlation ID, and cross-tenant near-match identifiers.
  - [ ] Bounded-state tests for maximum transition history, maximum active lifecycle entries, TTL/eviction behavior, repeated lifecycle reads, long-running commands, and disposal/cancellation races.
  - [ ] Adapter-boundary tests proving internal lifecycle DTOs map to official MCP SDK responses while SDK DTOs stay out of Contracts and SourceTools public surfaces.
  - [ ] Read-your-writes benchmark: submit command, await terminal confirmation, read projection, and assert P95 < 1500 ms on localhost Aspire topology with documented warm/cold setup.
  - [ ] Regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.
  - [ ] Targeted tests: `tests/Hexalith.FrontComposer.Mcp.Tests`, `tests/Hexalith.FrontComposer.Shell.Tests` lifecycle/pending-command suites, and EventStore tests only if shared seams change.

---

## Dev Notes

### Existing State To Preserve

| File / Area | Current state | Preserve / Change |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Mcp` | Story 8-1 owns MCP hosting, descriptor registry, SDK containment, command/resource adapters, and sanitized adapter errors. | Extend here for acknowledgment/lifecycle tools. Keep SDK DTO mapping inside this package. |
| Story `8-2-hallucination-rejection-and-tenant-scoped-tools` | Owns visible catalog, hidden/unknown semantics, schema validation, policy filtering, stale list replay rejection, and sanitized self-correction responses. | Lifecycle and command execution must enter only after this admission pipeline succeeds. Do not duplicate visibility checks. |
| `src/Hexalith.FrontComposer.Contracts/Communication/CommandResult.cs` | Carries `MessageId`, `Status`, optional `CorrelationId`, `Location`, and `RetryAfter`. | Use as the acknowledgment source where available; avoid inventing a parallel dispatch result. |
| `src/Hexalith.FrontComposer.Contracts/Communication/ICommandServiceWithLifecycle.cs` | Optional command service extension for lifecycle callbacks after acknowledgment. | Prefer this for MCP lifecycle transition observation; fail loudly if required callbacks are unavailable. |
| `src/Hexalith.FrontComposer.Contracts/Lifecycle/CommandLifecycleState.cs` | Web lifecycle enum: `Idle`, `Submitting`, `Acknowledged`, `Syncing`, `Confirmed`, `Rejected`. | Reuse conceptual states. Add agent-facing terminal result types in MCP rather than changing this enum unless implementation proves a shared contract change is required. |
| `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs` | Scoped lifecycle service with transition validation, duplicate terminal suppression, bounded MessageId cache, and sanitized telemetry. | Reuse behavior and tests. Do not bypass state-machine validation. |
| `src/Hexalith.FrontComposer.Shell/State/PendingCommands/*` | Pending-command registration and terminal outcome resolver already model `Confirmed`, `Rejected`, `IdempotentConfirmed`, and `NeedsReview`. | Reuse this terminal vocabulary for agent results where possible. |
| Epic 5 EventStore services | Command/query clients own token relay, ETag/cache behavior, retry/degraded classification, SignalR nudges, and sanitized telemetry. | MCP lifecycle must observe these seams, not create a second EventStore client or polling subsystem. |

### Architecture Contracts

- MCP command execution is a surface adapter over the existing command lifecycle, not a new command bus.
- Story 8-2 admission is mandatory before any lifecycle behavior. Invalid MCP input must not allocate lifecycle entries or register pending commands.
- The first command call returns `Acknowledged`, not final success. Agents must use the lifecycle tracking surface for terminal state.
- Lifecycle tracking is read-only from the agent perspective. It reports server/framework-observed state and never accepts client-supplied state transitions.
- Terminal outcome is exactly-once per accepted command. Repeated reads return the same terminal result.
- Agent-facing `IdempotentConfirmed` is a success outcome derived from existing idempotency/duplicate-resolution signals, not a new backend status invented by MCP.
- Subscription URIs are opaque convenience handles. Every lifecycle read revalidates current authentication, tenant, and policy context.
- SDK churn containment follows Story 8-1: official MCP C# SDK types stay at the `Hexalith.FrontComposer.Mcp` boundary.

### Proposed Two-Call Flow

1. Agent calls a visible command tool with valid arguments.
2. Story 8-2 admission resolves current tenant/policy visibility and validates the raw envelope.
3. MCP adapter constructs the command and dispatches through existing command service seams.
4. EventStore acknowledgement returns `MessageId`, optional `CorrelationId`, `Location`, and `RetryAfter`.
5. MCP adapter returns an acknowledgment with state `Acknowledged` and a lifecycle tracking reference.
6. Agent calls the lifecycle tracking surface with the framework-issued correlation/message identifier.
7. Lifecycle adapter returns latest state and bounded transition history until terminal `Confirmed`, `Rejected`, or `IdempotentConfirmed`.
8. After terminal `Confirmed` or `IdempotentConfirmed`, agent reads projection resources. If the projection is not yet safe for read-your-writes, lifecycle returns `Syncing`/retry metadata instead of final success.

### Binding Decisions

| Decision | Rationale |
| --- | --- |
| D1. Two-call lifecycle is mandatory for agent commands. | Prevents agents from treating a 202/ack as final success and aligns with FR52. |
| D2. Existing lifecycle/pending-command services are the source of truth. | Avoids a parallel MCP state machine and preserves web/MCP parity. |
| D3. `IdempotentConfirmed` is agent-facing terminal success. | Agents need to distinguish "intent fulfilled" from "rejected, retry maybe useful." |
| D4. Lifecycle tracking revalidates auth/tenant/policy on every call. | Prevents replay of old subscription references across tenant or policy changes. |
| D5. Unknown lifecycle identifiers use Story 8-2 hidden/unknown semantics. | Prevents enumeration of another tenant's command state. |
| D6. Rejection output is structured plus human-readable. | Agents can decide retry/abort/alternative without brittle string parsing. |
| D7. Terminal state is idempotently readable. | Agents retry reads naturally; repeated tracking must not duplicate outcomes. |
| D8. Read-your-writes benchmark observes command, lifecycle, and projection. | NFR6 measures the actual agent job, not just dispatch latency. |
| D9. Subscription URI is not a bearer secret. | Security comes from current auth/tenant/policy checks, not obscurity. |
| D10. Runtime state is bounded by options. | Applies L14 and prevents long-lived agent sessions from accumulating unbounded entries. |

### Structured Result Shapes

The exact MCP DTO may differ by SDK version, but internal models should be equivalent to:

```json
{
  "state": "Acknowledged",
  "messageId": "01JZ0R5K9N8W4Y7V3Q2P6C1A0B",
  "correlationId": "01JZ0R5K9N8W4Y7V3Q2P6C1A0B",
  "lifecycle": {
    "tool": "frontcomposer.lifecycle.subscribe",
    "uri": "frontcomposer://lifecycle/01JZ0R5K9N8W4Y7V3Q2P6C1A0B",
    "retryAfterMs": 250
  }
}
```

Terminal rejection shape:

```json
{
  "state": "Rejected",
  "terminal": true,
  "outcome": {
    "errorCode": "ORDER_ALREADY_CLOSED",
    "entityId": "order-123",
    "message": "Order update failed: the order is already closed. No changes were applied.",
    "dataImpact": "No changes were applied.",
    "suggestedAction": "abort",
    "retryAppropriate": false,
    "reasonCategory": "domain_conflict"
  }
}
```

Do not include tenant IDs, user IDs, roles, claims, raw payload values, provider names, hidden tool names, stack traces, or raw exception text.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 8-1 | Story 8-3 | MCP package boundary, command adapter, descriptor registry, SDK containment, and minimal acknowledged command result. |
| Story 8-2 | Story 8-3 | Admission, tenant/policy visible catalog, hidden/unknown semantics, stale replay rejection, and sanitized error/list behavior. |
| Stories 2-3 through 2-5 | Story 8-3 | Lifecycle state machine, progressive thresholds, rejection copy, idempotency semantics, and exactly-one-outcome invariant. |
| Epic 5 | Story 8-3 | EventStore command/query, SignalR nudge, ETag reconciliation, fallback polling, token relay, and degraded/error classification. |
| Stories 7-1 through 7-3 | Story 8-3 | Authenticated principal, canonical tenant context, and command policy evaluator rechecks. |
| Story 8-3 | Story 8-4 | Projection rendering can rely on a terminal lifecycle result before read-your-writes projection reads. |
| Story 10-2 / Story 10-6 | Story 8-3 | Agent E2E, performance, and benchmark lanes consume stable lifecycle result contracts. |

### Scope Guardrails

Do not implement these in Story 8-3:

- Unknown-tool suggestions, tenant-scoped listing, visible catalog filtering, or hidden tool semantics beyond consuming Story 8-2 services. Owner: Story 8-2.
- Rich Markdown projection tables, status cards, timelines, empty-state suggestions, badge text rendering, or locale formatting parity. Owner: Story 8-4.
- Skill corpus resources or agent code-generation guidance. Owner: Story 8-5.
- Schema hash fingerprints, migration delta diagnostics, or multi-surface abstraction redesign. Owner: Story 8-6.
- New command policy language, multi-policy composition, or backend EventStore authorization policy enforcement. Owner: Story 7-3 or later auth follow-up.
- Semantic retry planning, autonomous compensation, workflow orchestration, or multi-command transactions. Owner: post-v1 agent workflow backlog.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Role-specific Markdown projection rendering after terminal lifecycle. | Story 8-4 |
| MCP-discoverable skill corpus explaining the lifecycle pattern to agents. | Story 8-5 |
| Schema fingerprints for lifecycle result version negotiation. | Story 8-6 |
| Deep agent E2E across command, lifecycle, and projection rendering. | Story 10-2 |
| Signed LLM benchmark releases and agent command-quality gates. | Story 10-6 |
| Multi-command orchestration and compensating actions. | Post-v1 agent workflow backlog |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-8-mcp-agent-integration.md#Story-8.3`] - story statement, AC foundation, FR52/FR57/NFR6 scope.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR52`] - two-call MCP lifecycle pattern.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR57`] - runtime agent parity with lifecycle and rollback messages.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md#Agent-surface-latency`] - command-to-projection read-your-writes P95 < 1500 ms.
- [Source: `_bmad-output/planning-artifacts/architecture.md#MCP-Interaction-Model`] - command tools, lifecycle subscription tool, and terminal-state guarantees.
- [Source: `_bmad-output/implementation-artifacts/8-1-mcp-server-and-typed-tool-exposure.md`] - MCP package, descriptor registry, command adapter, and deferred 8-3 lifecycle scope.
- [Source: `_bmad-output/implementation-artifacts/8-2-hallucination-rejection-and-tenant-scoped-tools.md`] - admission, visibility, replay, and hidden/unknown semantics.
- [Source: `_bmad-output/implementation-artifacts/7-3-command-authorization-policies.md`] - submit-time authorization freshness and policy evaluator reuse.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L12`] - async bridge lifecycle checklist.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L14`] - bounded runtime cache discipline.
- [Source: `src/Hexalith.FrontComposer.Contracts/Communication/CommandResult.cs`] - existing acknowledgment result shape.
- [Source: `src/Hexalith.FrontComposer.Contracts/Communication/ICommandServiceWithLifecycle.cs`] - optional lifecycle-aware dispatch seam.
- [Source: `src/Hexalith.FrontComposer.Contracts/Lifecycle/CommandLifecycleState.cs`] - existing web lifecycle state vocabulary.
- [Source: `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandModels.cs`] - existing pending terminal outcomes including `IdempotentConfirmed`.
- [Source: Model Context Protocol tools specification 2025-11-25](https://modelcontextprotocol.io/specification/2025-11-25/server/tools) - tool call/result/error behavior and schema expectations.
- [Source: Official MCP SDK list](https://modelcontextprotocol.io/docs/sdk) - C# SDK Tier 1 status and SDK capability set.
- [Source: Official MCP C# SDK API docs](https://csharp.sdk.modelcontextprotocol.io/api/ModelContextProtocol.AspNetCore.html) - ASP.NET Core MCP package and Streamable HTTP transport API boundary.
- [Source: NuGet `ModelContextProtocol.AspNetCore` 1.2.0](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore/) - current package version and .NET 10 compatibility as of 2026-05-01.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-01: Story created via `/bmad-create-story 8-3-two-call-lifecycle-and-agent-command-semantics` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### File List

(to be filled in by dev agent)
