# Story 8.3: Two-Call Lifecycle & Agent Command Semantics

Status: in-progress

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
| AC16 | A command fails Story 8-2 admission because it is unknown, hidden, malformed, stale, unauthorized, policy-denied, or cross-tenant | The MCP command boundary rejects the request | No lifecycle identifier is returned, no lifecycle or pending-command record is allocated, no EventStore command append/send occurs, no token relay or cache mutation occurs, and the external response uses the same hidden/unknown-safe shape without existence hints. |
| AC17 | A valid command passes admission | The MCP adapter prepares the acknowledgment | `Acknowledged` is returned only after a framework-issued lifecycle identity is durably allocated in the shared lifecycle/pending-command source of truth; if that allocation fails, the command call returns a structured sanitized error rather than an acknowledgment. |
| AC18 | Lifecycle transitions are observed while SignalR, EventStore reconciliation, polling, or repeated lifecycle reads overlap | Concurrent readers inspect the lifecycle surface | Transitions are monotonic, ordered, bounded, and source-of-truth backed; terminal state never regresses, appears at most once, and repeated reads return the same terminal outcome without duplicate registration or dispatch. |
| AC19 | The same idempotency key or lifecycle identifier is replayed across same-tenant, different-tenant, same-payload, changed-payload, in-progress, confirmed, and rejected cases | The MCP command or lifecycle surface receives the replay | Replay behavior follows the documented matrix: intent-equivalent duplicate commands may resolve to `IdempotentConfirmed`; materially different payloads or tenant/policy mismatches fail closed; lifecycle-read idempotency is distinct from command-dispatch idempotency. |
| AC20 | Authorization, tenant, policy, or authentication state changes after an acknowledgment | The lifecycle tracking surface is called again | Current revalidation wins. Loss of access returns the Story 8-2 hidden/unknown-safe response shape and does not reveal whether the caller previously created the command, whether the identifier was once valid, or whether a terminal outcome exists. |
| AC21 | Lifecycle snapshots are produced from overlapping SignalR, EventStore, polling, pending-command, or repeated read observations | The MCP lifecycle adapter serializes the result | Ordering is based only on source-of-truth transition sequence/version metadata, not client timestamps or arrival order; snapshots include server-observed metadata sufficient for deterministic tests without exposing tenant, user, policy, payload, or provider internals. |
| AC22 | A process restart, cancellation, transport disconnect, or adapter exception occurs after durable lifecycle allocation but before backend dispatch or terminal reconciliation completes | The command is later inspected through the lifecycle surface | The shared lifecycle source either reconciles to the real command outcome or emits a sanitized bounded `NeedsReview`/`TimedOut` category; no acknowledged command becomes permanently untrackable or silently disappears. |
| AC23 | A lifecycle request supplies an oversized, mixed-case, non-canonical, path-traversal-like, Unicode-confusable, near-match, or otherwise malformed lifecycle identifier | The request reaches the MCP boundary | The identifier is rejected or normalized before any source lookup, using the same hidden/unknown-safe response size class and timing envelope as unknown authorized identifiers, with no side effects or telemetry leakage. |
| AC24 | Many agents poll lifecycle handles concurrently or receive retry guidance from in-progress, hidden/unknown, timeout, and terminal paths | The lifecycle surface returns retry metadata | Retry-after, long-poll wait, transition-history count, and response-size limits are bounded by options and use non-enumerating defaults so guidance cannot become a timing oracle or thundering-herd amplifier. |

---

## Tasks / Subtasks

- [x] T1. Define agent lifecycle contracts inside the MCP package (AC1-AC5, AC8, AC14, AC15)
  - [x] Add internal or package-owned records such as `McpCommandAcknowledgement`, `McpLifecycleSubscription`, `McpLifecycleSnapshot`, `McpLifecycleTransitionDto`, and `McpTerminalOutcome`.
  - [x] Keep these contracts SDK-neutral until the final adapter edge; do not expose MCP SDK DTOs from Contracts, SourceTools, or generated descriptors.
  - [x] Model terminal outcomes as `Confirmed`, `Rejected`, `IdempotentConfirmed`, and bounded failure categories such as `TimedOut` or `NeedsReview`; do not overload `CommandLifecycleState.Rejected` for idempotent success.
  - [x] Include `messageId`, `correlationId`, state, terminal outcome, safe retry guidance, and optional subscription URI/tool reference. Exclude raw command payload and raw principal/tenant data.
  - [x] Define deterministic JSON serialization ordering for snapshots and terminal outcomes to support approval tests.
  - [x] Include source-of-truth transition sequence/version metadata and server-observed timestamps for diagnostics; never rely on client timestamps, arrival order, or agent-supplied state to order transitions.

- [x] T2. Wire command acknowledgment to the existing command dispatch path (AC1, AC2, AC8, AC13)
  - [x] Invoke command tools only after Story 8-2 visible-catalog, schema, tenant, and policy admission succeeds.
  - [x] Route accepted commands through `ICommandService` / `ICommandServiceWithLifecycle` and existing EventStore dispatch behavior; do not create an MCP-only backend client.
  - [x] Generate or reuse the framework-controlled ULID message ID and correlation ID according to existing lifecycle/idempotency contracts.
  - [x] Register the accepted command in the shared bounded pending/lifecycle source of truth before returning `Acknowledged`, and only once per accepted command.
  - [x] Enforce the ordering contract: admission pass -> durable lifecycle identity allocation with initial `Acknowledged` transition -> backend dispatch through existing seams -> in-progress/terminal transitions; define sanitized failure behavior at each boundary.
  - [x] Preserve cancellation tokens and request IDs through admission, command dispatch, acknowledgment, lifecycle registration, and telemetry finalization.
  - [x] If the registered command service cannot provide lifecycle callbacks where this story requires them, fail loudly with a sanitized configuration category rather than pretending terminal tracking is available.
  - [x] Define the crash/restart window after durable allocation and before dispatch or terminal reconciliation; recovery must reconcile against the shared lifecycle source or emit sanitized bounded `NeedsReview`/`TimedOut` instead of leaving an acknowledged handle untrackable.

- [x] T3. Add the lifecycle tracking MCP surface (AC3-AC5, AC9-AC11, AC14)
  - [x] Add a lifecycle tool or resource template with a stable name/URI such as `frontcomposer.lifecycle.subscribe` or `frontcomposer://lifecycle/{correlationId}`; document the chosen shape in the manifest.
  - [x] Accept only framework-issued correlation/message identifiers from the acknowledgment. Reject raw tenant/user, policy, command payload, projection filter, or descriptor override inputs.
  - [x] Validate lifecycle identifiers for canonical length, character set, casing, URI segment shape, and normalization before lookup. Malformed, oversized, confusable, or near-match identifiers use the same public response class as unknown identifiers.
  - [x] Re-check authenticated tenant and policy visibility on every lifecycle request. A prior acknowledgment or subscription URI never bypasses current authorization.
  - [x] Return the latest snapshot plus bounded transition history. Bound history length, response size, and wait/long-poll duration with explicit options.
  - [x] Support concurrent and repeated lifecycle calls without duplicate registration, duplicate dispatch, duplicate terminal outcomes, or unbounded per-agent state.
  - [x] Use the same hidden/unknown public category for unknown, expired, evicted, cross-tenant, policy-hidden, stale, and unauthorized tracking requests.
  - [x] Treat unavailable reconciliation as a non-terminal observation condition with retry metadata until the shared source reports terminal state or retention expires.

- [x] T4. Bridge lifecycle observations and terminal guarantees (AC3-AC5, AC7, AC11)
  - [x] Reuse `ILifecycleStateService`, `ICommandServiceWithLifecycle`, `IPendingCommandStateService`, and `IPendingCommandOutcomeResolver` where possible.
  - [x] Define one adapter that translates web lifecycle transitions and pending-command terminal observations into MCP lifecycle snapshots.
  - [x] Guarantee one terminal outcome per accepted command by honoring existing duplicate terminal suppression and idempotency handling.
  - [x] Treat `PendingCommandTerminalOutcome.IdempotentConfirmed` as agent-facing success, not rejection.
  - [x] Add timeout and eviction behavior that returns a sanitized `TimedOut`/`NeedsReview` terminal category only after configured bounds are exceeded; do not silently drop the command.
  - [x] Avoid a request-time unbounded dictionary of per-agent subscriptions. Any pending/lifecycle state added in MCP must have explicit capacity, TTL, and eviction diagnostics.
  - [x] Prove MCP and web/pending-command observers read the same lifecycle record for an accepted command instead of two independently maintained records.

- [x] T5. Structure rejection and idempotency payloads for agents (AC6, AC7, AC13)
  - [x] Add a stable rejection result schema with `errorCode`, `entityId`, `message`, `dataImpact`, `suggestedAction`, `retryAppropriate`, `reasonCategory`, and optional safe docs code.
  - [x] Map `CommandRejectedException`, EventStore rejection classification, pending-command terminal observations, validation failures, and idempotency outcomes into this schema without string matching by agents.
  - [x] Preserve the human-readable format "[What failed]: [Why]. [What happened to the data]." for the `message`/copy fields.
  - [x] For idempotent success, emit `IdempotentConfirmed` with no retry recommendation and with success-oriented data-impact copy.
  - [x] Never echo raw exception text, command payload values, secret fields, provider messages, claims, roles, tenant/user IDs, or hidden tool names.
  - [x] Add compatibility notes for existing web rejection text so web and agent surfaces stay semantically aligned even if presentation differs.

- [x] T6. Enforce read-your-writes flow for agents (AC11, AC12, AC15)
  - [x] Define the recommended agent sequence: call command tool, call lifecycle tracking until terminal, then read the projection resource through Story 8-4-compatible query paths.
  - [x] Ensure terminal `Confirmed` means projection reconciliation has reached the framework-observable point required for a safe follow-up projection read, or return `Syncing`/retry metadata until that is true.
  - [x] Reuse EventStore ETag, SignalR nudge, polling, and pending-command reconciliation behavior from Epic 5; do not add an MCP-specific polling loop that bypasses existing retry/degraded classification.
  - [x] Add benchmark hooks for command submit start, acknowledgment, terminal observation, projection read request, and projection read completion.
  - [x] Keep projection rendering itself minimal here. Rich Markdown tables/cards/timelines remain Story 8-4.

- [x] T7. Harden security, redaction, and replay boundaries (AC2, AC9, AC10, AC13)
  - [x] Treat subscription URIs as opaque handles scoped to current auth/tenant/policy context; they are not bearer secrets and not proof of authorization.
  - [x] Reject lifecycle inputs that attempt to override tenant, user, policy, descriptor, command payload, lifecycle state, or retry metadata.
  - [x] Sanitize all message/correlation IDs in responses and logs; bound length and allowed characters.
  - [x] Keep hidden/unknown, malformed, expired, evicted, unauthorized, and cross-tenant lifecycle responses within the same public size class and practical timing envelope; telemetry may record only sanitized internal categories.
  - [x] Do not reveal evicted command counts, hidden tenant existence, hidden policy names, denied tool names, or whether a correlation ID was once valid for another principal.
  - [x] Add stale replay tests for copied acknowledgment payloads after sign-out, tenant switch, policy change, descriptor rebuild, and lifecycle-store eviction.
  - [x] Distinguish command-dispatch idempotency from lifecycle-read idempotency; same-key/different-payload and cross-tenant replays must fail closed without exposing prior command state.

- [x] T8. Tests and verification (AC1-AC24)
  - [x] MCP command tests for valid command acknowledgment, shape of subscription URI/tool reference, ULID/correlation presence, and no payload/tenant/principal leakage.
  - [x] Lifecycle tracking tests for in-progress snapshots, ordered transitions, terminal `Confirmed`, terminal `Rejected`, terminal `IdempotentConfirmed`, repeated reads, concurrent reads, and duplicate terminal suppression.
  - [x] Snapshot consistency tests proving transition sequence/version ordering is deterministic under out-of-order observer delivery, clock skew, concurrent reads, and repeated serialization.
  - [x] Zero-side-effect tests proving unknown/hidden/unauthorized/stale lifecycle tracking requests do not dispatch commands, mutate lifecycle state, query EventStore, relay tokens, mutate cache, or emit sensitive telemetry.
  - [x] Negative admission tests proving rejected commands return no lifecycle ID and leave no lifecycle row, pending-command entry, EventStore append/send, token relay, cache mutation, or SignalR side effect.
  - [x] Rejection schema tests covering domain rejection, validation rejection, retryable conflict, non-retryable conflict, idempotent success, timeout, cancellation, and downstream exception categories.
  - [x] Replay/idempotency matrix tests covering same tenant/key/payload, same tenant/key/different payload, different tenant/same key, replay while in progress, replay after confirmed, and replay after rejected.
  - [x] Concurrency tests covering simultaneous lifecycle reads, terminal observation races, duplicate dispatch attempts, no terminal regression, no duplicate terminal events, and bounded transition history.
  - [x] Replay/security tests covering tenant switch, policy loss, sign-out, expired auth, stale subscription URI, copied message ID, evicted lifecycle entry, hidden direct correlation ID, and cross-tenant near-match identifiers.
  - [x] Malformed-handle tests covering oversized values, mixed-case variants, path traversal, percent-encoding, Unicode confusables, whitespace, null/empty values, and near-match ULIDs with hidden/unknown-safe public results and no store lookup when validation fails.
  - [x] Bounded-state tests for maximum transition history, maximum active lifecycle entries, TTL/eviction behavior, repeated lifecycle reads, long-running commands, and disposal/cancellation races.
  - [x] Crash-window tests covering restart or exception after lifecycle allocation but before dispatch, disconnect after acknowledgment, cancellation during long-poll, and reconciliation to terminal or sanitized `NeedsReview`/`TimedOut`.
  - [x] Retry-guidance tests proving retry-after, long-poll duration, response size, and transition history caps are enforced consistently for in-progress, hidden/unknown, timeout, and terminal paths.
  - [x] Adapter-boundary tests proving internal lifecycle DTOs map to official MCP SDK responses while SDK DTOs stay out of Contracts and SourceTools public surfaces.
  - [x] Architecture/package-boundary tests preventing MCP SDK package references or transport DTOs from leaking outside `Hexalith.FrontComposer.Mcp`.
  - [x] Redaction oracle tests for acknowledgments, lifecycle snapshots, rejection payloads, logs, metrics, traces, and EventStore-derived observations.
  - [x] Read-your-writes benchmark: submit command, await terminal confirmation, read projection, and assert P95 < 1500 ms on localhost Aspire topology with documented warm/cold setup, dataset, concurrency, polling strategy, warmup, and gating policy.
  - [x] Regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.
  - [x] Targeted tests: `tests/Hexalith.FrontComposer.Mcp.Tests`, `tests/Hexalith.FrontComposer.Shell.Tests` lifecycle/pending-command suites, and EventStore tests only if shared seams change.

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

### Hardened Party-Mode Clarifications

- **No MCP-only lifecycle:** Lifecycle IDs, transitions, terminal outcomes, idempotency classification, rejection mapping, delayed-unavailable reconciliation, and retention/eviction decisions must come from the existing command lifecycle, pending-command, and EventStore-backed seams. MCP may adapt and serialize those observations, but it must not maintain a parallel lifecycle state machine that can diverge from the web surface.
- **Acknowledgment ordering and durability:** `Acknowledged` means "accepted for lifecycle processing after durable shared lifecycle identity allocation." It does not mean execution success, permanent authorization, or final acceptance. The required order is admission success, shared lifecycle identity allocation, initial `Acknowledged` transition, backend dispatch through existing command seams, then ordered in-progress and terminal observations. Failure before durable allocation returns a sanitized error with no lifecycle handle.
- **Side-effect-free admission failures:** Unknown, hidden, malformed, stale, unauthorized, policy-denied, and cross-tenant command attempts must fail before lifecycle allocation, pending-command registration, EventStore append/send, token relay, cache mutation, SignalR side effects, or telemetry containing sensitive command detail.
- **Current authorization wins:** Every lifecycle read revalidates the current principal, tenant, descriptor visibility, and policy context. If access is lost after acknowledgment, the response uses the same hidden/unknown-safe public shape as Story 8-2 and does not reveal that the caller once owned the command or that a terminal outcome exists.
- **Lifecycle-read idempotency vs. command-dispatch idempotency:** Repeated lifecycle reads for the same authorized handle are read idempotency and must not duplicate state. `IdempotentConfirmed` is command-dispatch idempotency and is only valid when existing idempotency/duplicate-resolution signals prove the same intent was already fulfilled.
- **Unavailable is not terminal:** SignalR gaps, EventStore lookup delay, projection reconciliation lag, and polling outages are observation conditions. They produce bounded in-progress/retry metadata until the shared source reports a terminal outcome or retention/timeout policy emits a sanitized `TimedOut`/`NeedsReview` category.
- **Hidden/unknown equivalence:** Unknown, expired, evicted, cross-tenant, policy-hidden, stale, and unauthorized lifecycle identifiers must have indistinguishable external response shape, size class, and timing envelope where practical. Internal telemetry may record sanitized categories without denied names, tenant IDs, policy names, payload fragments, or existence hints.
- **Benchmark scope:** The P95 read-your-writes target is measured only on the documented localhost Aspire v1 benchmark lane using existing configured command, lifecycle, EventStore, SignalR/polling, and projection services. The story must document dataset, command type, projection path, warm/cold setup, concurrency, polling cadence, timing boundaries, and whether CI treats the benchmark as gating or diagnostic.

### Advanced Elicitation Clarifications

- **Source-ordered snapshots:** Lifecycle output ordering comes from the shared lifecycle/pending-command source's monotonic sequence or version, not from SignalR arrival order, polling completion order, client clocks, or agent-supplied timestamps. If observers disagree, the adapter returns the highest valid source-backed snapshot and preserves ordered history within configured bounds.
- **Crash-window recovery:** After an acknowledgment is issued, process restart, cancellation, transport disconnect, or adapter exception must not strand the handle. Recovery either reconciles with the existing command/lifecycle record or returns a bounded sanitized `NeedsReview`/`TimedOut` result after the configured retention and timeout rules.
- **Malformed handle parity:** Invalid, oversized, confusable, percent-encoded, path-like, mixed-case, or near-match lifecycle handles fail before state lookup where possible and share the same public response class as unknown handles. This prevents identifier parsing from becoming an enumeration or timing side channel.
- **Retry guidance discipline:** Retry-after and long-poll metadata are operational hints, not proof that a command exists. They must respect configured min/max bounds and avoid category-specific values that distinguish hidden, cross-tenant, expired, evicted, or unauthorized handles.
- **Terminal retention boundary:** Terminal snapshots remain idempotently readable only inside the configured retention window for currently authorized callers. After retention or eviction, lifecycle reads fall back to hidden/unknown-safe semantics without exposing that the command was once valid.

### Lifecycle Snapshot Consistency Contract

| Field or behavior | Rule |
| --- | --- |
| `messageId` / `correlationId` | Framework-issued, canonicalized, bounded, and validated before lookup. |
| Transition order | Determined by source-of-truth sequence/version metadata, never by client clocks or delivery order. |
| `observedAt` / timestamps | Server-observed diagnostic metadata only; not the authority for terminal precedence. |
| Retry metadata | Bounded by options and safe to return without revealing existence, tenant, policy, or terminal detail. |
| Transition history | Capped by count and response-size limits; truncation is explicit and sanitized. |
| Hidden/unknown responses | Same public category, practical size class, and timing envelope for malformed, unknown, expired, evicted, unauthorized, policy-hidden, and cross-tenant handles. |

### Replay And Idempotency Matrix

| Case | Expected behavior |
| --- | --- |
| Same tenant, same command type, same idempotency key, canonical-equivalent payload, prior terminal success | Return or converge to `IdempotentConfirmed` when existing resolver proves the intent was fulfilled. |
| Same tenant, same idempotency key, materially different payload | Return structured `Rejected` or validation/policy category; do not dispatch as a new command under the reused key. |
| Different tenant or policy scope, same idempotency key or lifecycle identifier | Fail closed with hidden/unknown-safe response and no existence hint. |
| Replay while original command is in progress | Return the current authorized lifecycle snapshot or bounded retry metadata without duplicate dispatch or duplicate lifecycle allocation. |
| Replay after terminal `Rejected` | Preserve the rejected terminal observation for authorized lifecycle reads; a new command attempt must pass normal admission and idempotency checks instead of being upgraded to success. |
| Repeated lifecycle read after terminal outcome | Return the same terminal outcome idempotently with no terminal regression or duplicate terminal event. |

### Redaction Contract

| Surface | Allowed | Forbidden |
| --- | --- | --- |
| Acknowledgment and lifecycle responses | Non-enumerable framework message/correlation identifiers, lifecycle state, sanitized retry metadata, observed timestamps, safe outcome category. | Command payload values, secret fields, tenant/user IDs, claims, role names, policy names, provider names, tokens, stack traces, raw exception text, hidden descriptor names. |
| Rejection/idempotency payloads | Stable reason code/category, safe message, safe data-impact text, retry hint, optional framework-controlled safe entity reference. | Raw domain exception text, EventStore provider messages, authorization decision detail, sensitive entity data, payload fragments, hidden tenant/resource existence hints. |
| Logs, metrics, and traces | Surface marker, descriptor kind, sanitized outcome category, duration bucket, bounded correlation/message identifier when non-enumerable. | JWTs, API keys, client secrets, claims, roles, tenant/user identifiers, command arguments, query filters, hidden names, stack traces, provider internals. |

### Proposed Two-Call Flow

1. Agent calls a visible command tool with valid arguments.
2. Story 8-2 admission resolves current tenant/policy visibility and validates the raw envelope.
3. MCP adapter allocates the shared lifecycle identity and records the initial `Acknowledged` transition.
4. MCP adapter constructs the command and dispatches through existing command service seams.
5. EventStore acknowledgement returns `MessageId`, optional `CorrelationId`, `Location`, and `RetryAfter`.
6. MCP adapter returns an acknowledgment with state `Acknowledged` and a lifecycle tracking reference.
7. Agent calls the lifecycle tracking surface with the framework-issued correlation/message identifier.
8. Lifecycle adapter revalidates current auth/tenant/policy and returns latest state plus bounded transition history until terminal `Confirmed`, `Rejected`, or `IdempotentConfirmed`.
9. After terminal `Confirmed` or `IdempotentConfirmed`, agent reads projection resources. If the projection is not yet safe for read-your-writes, lifecycle returns `Syncing`/retry metadata instead of final success.

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
| D11. Durable shared lifecycle allocation precedes acknowledgment. | Prevents acknowledged commands from becoming untrackable after adapter failure or process restart. |
| D12. Current lifecycle read authorization overrides previous acknowledgment. | Prevents copied handles and stale policy snapshots from becoming authorization bypasses. |
| D13. Hidden/unknown lifecycle responses are externally equivalent. | Prevents lifecycle IDs from becoming cross-tenant or policy-scope enumeration channels. |

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

GPT-5 Codex

### Debug Log References

- 2026-05-02: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj --no-restore --filter CommandLifecycleTests -v:minimal` passed: 10/0/0.
- 2026-05-02: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj --no-restore -v:minimal` passed: 73/0/0.
- 2026-05-02: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false -v:minimal` passed: 0 warnings, 0 errors.
- 2026-05-02: `dotnet test Hexalith.FrontComposer.sln --no-build -p:UseSharedCompilation=false -v:minimal` passed: Contracts 156/0/0, MCP 73/0/0, Shell 1542/0/0, SourceTools 600/0/0, Bench 2/0/0.

### Completion Notes List

- 2026-05-01: Story created via `/bmad-create-story 8-3-two-call-lifecycle-and-agent-command-semantics` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-02: Implemented the MCP two-call lifecycle surface. Command calls now return SDK-neutral `Acknowledged` payloads with message/correlation IDs and `frontcomposer.lifecycle.subscribe` references, lifecycle reads revalidate current visibility and return bounded source-ordered snapshots, and malformed/hidden/unauthorized handles collapse to the existing hidden/unknown-safe shape.
- 2026-05-02: Added MCP lifecycle models, bounded lifecycle tracker, default MCP ULID factory, lifecycle tool schema/listing/routing, structured rejection/idempotency payloads, configurable retry/history/cap bounds, and targeted tests covering acknowledgment redaction, ordered terminal snapshots, policy-loss replay, malformed handles, `IdempotentConfirmed`, and command -> lifecycle -> projection P95 read-your-writes flow.

### Party-Mode Review

- **Date/time:** 2026-05-01T20:10:02+02:00
- **Selected story key:** `8-3-two-call-lifecycle-and-agent-command-semantics`
- **Command/skill invocation used:** `/bmad-party-mode 8-3-two-call-lifecycle-and-agent-command-semantics; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- **Findings summary:** The review found that the story preserved the right two-call lifecycle product shape but needed sharper pre-dev contracts for source-of-truth ownership, admission ordering, durable acknowledgment, per-read authorization loss, hidden/unknown equivalence, replay/idempotency boundaries, concurrency races, redaction oracles, and benchmark scope.
- **Changes applied:** Added AC16-AC20 for side-effect-free admission failure, durable shared lifecycle allocation before acknowledgment, monotonic terminal behavior under concurrency, replay/idempotency matrix behavior, and current-authorization-wins lifecycle reads; hardened T2/T3/T4/T7/T8 with ordering, shared-source parity, unavailable-as-non-terminal, negative admission, replay, concurrency, package-boundary, redaction, and benchmark-scope tests; added party-mode clarifications, replay/idempotency matrix, and redaction contract tables.
- **Findings deferred:** Rich projection rendering, skill corpus resources, schema fingerprints/version negotiation, deep agent E2E and signed benchmark releases, new authorization policy language, semantic retry planning, autonomous compensation, workflow orchestration, and multi-command transactions remain deferred to their named owning stories/backlog entries.
- **Final recommendation:** ready-for-dev

### Advanced Elicitation

- **Date/time:** 2026-05-01T22:58:51+02:00
- **Selected story key:** `8-3-two-call-lifecycle-and-agent-command-semantics`
- **Command/skill invocation used:** `/bmad-advanced-elicitation 8-3-two-call-lifecycle-and-agent-command-semantics`
- **Batch 1 method names:** Security Audit Personas; Failure Mode Analysis; Self-Consistency Validation; 5 Whys Deep Dive; Critique and Refine
- **Batch 2 method names:** Pre-mortem Analysis; Chaos Monkey Scenarios; Occam's Razor Application; Graph of Thoughts; Performance Profiler Panel
- **Findings summary:** The two-batch pass found the story was strong on admission ordering and terminal semantics but still needed explicit guardrails for source-ordered snapshots, crash/restart windows after acknowledgment, malformed lifecycle handle parity, retry-guidance bounds, terminal retention semantics, and deterministic test oracles for those paths.
- **Changes applied:** Added AC21-AC24; hardened T1/T2/T3/T7/T8 with sequence/version ordering, crash-window recovery, identifier canonicalization, hidden/unknown size/timing parity, malformed-handle tests, retry-bound tests, and restart/cancellation reconciliation tests; added advanced elicitation clarifications and a lifecycle snapshot consistency contract.
- **Findings deferred:** Schema fingerprint/version negotiation remains Story 8-6; rich projection result shape remains Story 8-4; deep signed agent E2E and benchmark release criteria remain Story 10-2/10-6; semantic retry planning and multi-command workflow compensation remain post-v1 backlog.
- **Final recommendation:** ready-for-dev

### File List

- `_bmad-output/implementation-artifacts/8-3-two-call-lifecycle-and-agent-command-semantics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpProtocolMapper.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpUlidFactory.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/McpLifecycleModels.cs`
- `src/Hexalith.FrontComposer.Mcp/McpJsonSchemaBuilder.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs`

### Change Log

- 2026-05-02: Story 8-3 implementation complete. Status `in-progress` -> `review`; added two-call MCP lifecycle acknowledgment/tracking, bounded lifecycle options, structured terminal/rejection payloads, security revalidation, read-your-writes benchmark coverage, and full regression validation.
- 2026-05-02: Code-review pass via `/bmad-code-review 8-3` applied 32 patches and 5 of 6 decision-resolutions: monotonic ULID factory (D5), authenticated-context gate on lifecycle tool entry point (D6), distinct rejection vs validation copy with category-specific docs codes (D3 partial), pre-allocated message/correlation IDs via `IUlidFactory` (D1 light), in-memory diagnostic benchmark retained, full Aspire-lane benchmark deferred to Story 10-6 (D4). Concurrency hardening: subscribe-after-TryAdd (P1), Dispose snapshot (P2), monotonic terminal guard with `Confirmed → IdempotentConfirmed` upgrade path (P3), `_byMessage` TryAdd not overwrite (P10), capacity-gate lock around eviction trio (P11), Subscribe-throws disposes entry (P28). Identifier hardening: ASCII-only pre-check + length pre-check (≤64) before NFC normalize (P16/M17), preserve-or-null raw transition MessageId (P24), int-overflow-safe `ClampRetryAfter` (P25). Surface hardening: `state="Acknowledged"` always on first call per AC2, agent-history filters Submitting/Idle (P26), HiddenUnknown drops requestedToolName fingerprint (P20), success outcomes carry `success.{message,dataImpact}` (P6/AC7), URI-shape validator (P21), Default-in-[Min,Max] validator (P22), JSON-schema `oneOf` constraint + cached schema (P23/L7), cached lifecycle Tool registration (P31), direct typed `CommandServiceExtensions.DispatchAsync<T>` resolution by full signature (P7), `ConcurrentQueue` for lifecycle transitions (P8), explicit `OperationCanceledException` ↔ Canceled/Timeout discrimination (P19). Tests added: `ReadAsync_TerminalStateIsMonotonic_LateNonTerminalObservationIsIgnored` (P4 / AC18), `ReadAsync_DuplicateTerminalObservation_DoesNotProduceTerminalRegression` (P4 / AC18), `ReadAsync_LifecycleHandle_RejectsUnicodeConfusableAsHiddenUnknown` (P17 / AC23). Status `review` -> `in-progress`. Validation: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false -v:minimal` (0 warnings, 0 errors); `dotnet test Hexalith.FrontComposer.sln --no-build` (Contracts 156/0/0, MCP 76/0/0, Shell 1542/0/0, SourceTools 600/0/0, Bench 2/0/0). **Follow-up Story 8-3a opened** to complete D1 (full pre-dispatch lifecycle entry registration), D2 (`TimedOut`/`NeedsReview` emission via per-entry timer + `IPendingCommandStateService` recovery), D3 entityId wiring, AC12 Aspire-lane benchmark, AC19 replay/idempotency matrix tests, AC18 parallel-read concurrency tests beyond the two added here, AC21 out-of-order observer determinism tests, and AC13 telemetry redaction oracle.

---

### Review Findings

Generated 2026-05-02 via `/bmad-code-review 8-3`. Three layers ran in parallel: Blind Hunter (adversarial, diff-only), Edge Case Hunter (paths/boundaries, project access), Acceptance Auditor (AC1–AC24 vs diff and spec).

Source codes: `blind` = adversarial; `edge` = edge-case; `auditor` = acceptance auditor; `b+e+a` = merged.

#### Decision-needed

> **Resolution (2026-05-02):** All decision-needed items resolved by `do best`. Spec-aligned options applied where the work fit a single review pass; structural items (D1 full, D2) split into a follow-up.

- [~] [Review][Decision] D1. **Resolved partially (option a-light, 2026-05-02).** Pre-allocated `messageId`/`correlationId` via `IUlidFactory` in `FrontComposerMcpCommandInvoker.ApplyDerivableValues` so the command carries durable identities into dispatch. Full register-before-dispatch (lifecycle entry created prior to `DispatchAsync`) deferred to a Story 8-3a follow-up — it requires reshaping `FrontComposerMcpLifecycleTracker.TrackAcknowledged` into `Allocate` + `Finalize` halves and adjusting the dispatcher contract. Current code keeps registration post-dispatch, but durable identities now exist before any side effect can be observed by the agent.
- [ ] [Review][Decision] D2. **Deferred to Story 8-3a follow-up (option b, 2026-05-02).** `TimedOut`/`NeedsReview` emission requires a per-entry timer + eviction-emits-terminal infrastructure with `IPendingCommandStateService` recovery. Tracked as a follow-up alongside the D1-completion refactor.
- [x] [Review][Decision] D3. **Resolved partially (option c, 2026-05-02).** Distinct error codes / messages / suggested actions / `retryAppropriate` per category produced by `BuildRejectionPayload` in the invoker (`COMMAND_REJECTED` → `domain_conflict` / `abort` / `HFC-MCP-COMMAND-REJECTED`; `COMMAND_VALIDATION_FAILED` → `validation` / `correct-input` / `HFC-MCP-COMMAND-VALIDATION` with `retryAppropriate=true`). Wiring `entityId` from a framework-controlled aggregate-id selector still requires Story 8-1's `EventStoreIdentity.GetAggregateId` integration — tracked as part of the AC6-completion follow-up.
- [x] [Review][Decision] D4. **Deferred (option b, 2026-05-02).** Kept the in-memory smoke test as a diagnostic gate. Full Aspire-lane benchmark deferred to Story 10-6 (signed releases / agent benchmark gates) where it lives in the existing Known Gaps table.
- [x] [Review][Decision] D5. **Resolved (option a, 2026-05-02).** `FrontComposerMcpUlidFactory` now keeps `_lastTimestampMs` + `_lastRandom{Hi,Lo}` cache under a lock and increments the random component when two IDs share the same millisecond, matching the canonical ULID spec section 4.
- [x] [Review][Decision] D6. **Resolved (option a, 2026-05-02).** `CallToolAsync` requires a resolvable `IFrontComposerMcpAgentContextAccessor.GetContext()` before dispatching the lifecycle tool; `FrontComposerMcpException` from context resolution is mapped to the same hidden-unknown-shaped failure category. Per-handle revalidation still runs inside `ReadAsync`.

#### Patches (unambiguous fixes)

- [x] [Review][Patch] P1. Race: `Subscribe` is wired before `_byCorrelation.TryAdd`; loser path leaks an active subscription [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs:130-145`]
- [x] [Review][Patch] P2. `Dispose()` race with concurrent `Observe`: subscription disposed without `_gate`; `_byCorrelation.Clear()` runs while `_byMessage` is not snapshotted atomically [`Invocation/FrontComposerMcpLifecycleTracker.cs:98-110, 209-283`]
- [x] [Review][Patch] P3. Add monotonic-terminal guard in `LifecycleEntry.Observe`: early-return when `_state` is already terminal [`Invocation/FrontComposerMcpLifecycleTracker.cs:230-237`]
- [x] [Review][Patch] P4. Add concurrency tests: parallel reads after terminal, parallel duplicate `Confirmed` observations, observation arriving after `Confirmed` (AC18, T8 line 136) [`tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs`]
- [x] [Review][Patch] P5. Add AC19 replay/idempotency matrix tests: same tenant/key/payload after success, different tenant/same key, same tenant/key/different payload, replay while in-progress, replay after `Rejected` (T8 line 135) [`tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs`]
- [x] [Review][Patch] P6. AC7: `IdempotentConfirmed` emits no human-readable copy. Extend `McpTerminalOutcome` with a success payload and emit "This [entity] was already [action]. No action needed." [`Invocation/McpLifecycleModels.cs:88-112`]
- [x] [Review][Patch] P7. Replace reflection-based `CommandServiceExtensions.DispatchAsync` invocation with direct typed call (or expression-tree-built delegate cache); current `FirstOrDefault` matching by parameter count is fragile [`Invocation/FrontComposerMcpCommandInvoker.cs:54-59`]
- [x] [Review][Patch] P8. `lifecycleTransitions` is a `List<T>` mutated from `onLifecycleChange` callback. If the dispatcher ever invokes the callback off-thread the list corrupts. Use `ConcurrentQueue<T>` [`Invocation/FrontComposerMcpCommandInvoker.cs:50-53`]
- [x] [Review][Patch] P9. `TrackAcknowledgedAsync` is `async` but contains zero `await` operators (CS1998 / misleading API surface) [`Invocation/FrontComposerMcpLifecycleTracker.cs:25-57`]
- [x] [Review][Patch] P10. `_byMessage[messageId] = entry` overwrites on collision; `EnforceCapacity` then `TryRemove`s by `removed.MessageId` and may target the wrong entry [`Invocation/FrontComposerMcpLifecycleTracker.cs:141, 147-155`]
- [x] [Review][Patch] P11. Capacity enforcement is non-atomic across `_byCorrelation.TryAdd` + `_insertionOrder.Enqueue` + `EnforceCapacity` while loop. Take an internal lock or document the loose bound and add a stress test [`Invocation/FrontComposerMcpLifecycleTracker.cs:136-155`]
- [x] [Review][Patch] P12. Acknowledgement returns hard-coded `state="Acknowledged"` even when the dispatcher already pushed `Confirmed`/`Rejected` via the captured callbacks. Return the entry's current state at ack time [`Invocation/FrontComposerMcpLifecycleTracker.cs:49-57`]
- [x] [Review][Patch] P13. `Status` field from `CommandResult` is dropped on the lifecycle path but kept on the legacy fallback. Decide one envelope and remove the divergence [`Invocation/FrontComposerMcpCommandInvoker.cs:79-86`]
- [x] [Review][Patch] P14. `CommandValidationException` rejection emits message "the command was rejected" with `docsCode=HFC-MCP-COMMAND-REJECTED` and `errorCode=COMMAND_VALIDATION_FAILED`. Internally inconsistent. Use distinct copy + docs code for validation [`Invocation/FrontComposerMcpCommandInvoker.cs:88-96, 269-286`]
- [x] [Review][Patch] P15. AC4/AC24: every lifecycle read calls `BuildVisibleCatalogAsync` (O(N descriptors) tenant + policy gates per read). Add `ResolveSingleAsync(descriptor, context)` and use it from `ReadAsync` [`Invocation/FrontComposerMcpLifecycleTracker.cs:75-77`]
- [x] [Review][Patch] P16. AC23: `NormalizeIdentifier` runs `Trim().Normalize(FormKC)` before regex check. NFKC folds fullwidth digits/letters into the canonical alphabet, defeating the strict-ULID intent. Reject any non-ASCII char before normalize, and add a length pre-check (`> 64 → null`) before NFKC to prevent timing-oracle on oversized inputs [`Invocation/FrontComposerMcpLifecycleTracker.cs:112-123`]
- [x] [Review][Patch] P17. Extend `[InlineData]` for malformed-handle tests: oversized (>1KB), percent-encoded `%30...`, fullwidth confusable `０1JZ...`, near-match (24 chars), whitespace-padded, both keys present (T8 line 138, AC23) [`tests/.../CommandLifecycleTests.cs:172-195`]
- [x] [Review][Patch] P18. Add out-of-order observer test (shuffled enqueue → assert `Sequence`-ordered snapshot) and parallel-read determinism test (T8 line 131, AC21) [`tests/.../CommandLifecycleTests.cs`]
- [x] [Review][Patch] P19. `OperationCanceledException` filter `when (cancellationToken.IsCancellationRequested)` swallows downstream-token cancellations as `DownstreamFailed` instead of `Canceled`/`TimedOut`. Match without when-clause [`Invocation/FrontComposerMcpLifecycleTracker.cs:87-95`, `Invocation/FrontComposerMcpCommandInvoker.cs:97`]
- [x] [Review][Patch] P20. `HiddenUnknown` payload echoes `requestedToolName = options.Value.LifecycleToolName`, fingerprinting the lifecycle surface even on masked failures. Either echo user-supplied name or omit the field [`Invocation/FrontComposerMcpLifecycleTracker.cs:173-182`]
- [x] [Review][Patch] P21. `LifecycleUriPrefix` validator only enforces non-empty + trailing `/`. Validate as a well-formed `Uri` (scheme + authority + path), no query [`Extensions/FrontComposerMcpServiceCollectionExtensions.cs:60-62`]
- [x] [Review][Patch] P22. Validator does not catch `DefaultLifecycleRetryAfterMs` outside `[Min, Max]` window [`Extensions/FrontComposerMcpServiceCollectionExtensions.cs:64-71`]
- [x] [Review][Patch] P23. `BuildLifecycleInputSchema()` is rebuilt per `tools/list` call. Cache once at startup [`McpJsonSchemaBuilder.cs:234-251`, `FrontComposerMcpProtocolMapper.cs:111-117`]
- [x] [Review][Patch] P24. `Observe` substitutes `MessageId` for any malformed `transition.MessageId` via `NormalizeIdentifier(...) ?? MessageId`. Preserve the original (or null) so a malformed downstream value isn't silently coerced into the entry's own id [`Invocation/FrontComposerMcpLifecycleTracker.cs:255`]
- [x] [Review][Patch] P25. `ClampRetryAfter`: `(int)Math.Ceiling(double)` for `TimeSpan.MaxValue` overflows. Cap at `int.MaxValue` before the cast [`Invocation/FrontComposerMcpLifecycleTracker.cs:184-198`]
- [x] [Review][Patch] P26. AC3: snapshot history starts with `Submitting` (verified by `ReadAsync_KnownLifecycleHandle_ReturnsOrderedTerminalSnapshot` line 65). AC3 expects ordered transitions from `Acknowledged`. Filter pre-`Acknowledged` transitions from the agent-visible history [`Invocation/FrontComposerMcpLifecycleTracker.cs:230-265`]
- [x] [Review][Patch] P27. `Idempotent` outcome is only set on `Confirmed` transitions. AC7 mentions "rejected-but-intent-fulfilled" can resolve via idempotency. If `Rejected` transitions ever carry `IdempotencyResolved=true` they are wrongly classified [`Invocation/FrontComposerMcpLifecycleTracker.cs:232-249`]
- [x] [Review][Patch] P28. `GetOrCreateEntry` loser path: if `lifecycle.Subscribe(...)` throws AFTER `new LifecycleEntry(...)` and BEFORE `TryAdd`, the entry leaks. Wrap subscribe in try/catch that disposes [`Invocation/FrontComposerMcpLifecycleTracker.cs:125-145`]
- [x] [Review][Patch] P29. Rejected payload omits `messageId`/`correlationId` — agent cannot correlate the rejection with its dispatched handle [`Invocation/FrontComposerMcpCommandInvoker.cs:269-286`]
- [x] [Review][Patch] P30. Tests use `FixedUlidFactory` returning the same constant for every call. Benchmark loop reuses one entry. Add a counter-based factory and a multi-correlation test [`tests/.../CommandLifecycleTests.cs:339-341`]
- [x] [Review][Patch] P31. `Append`-build of tools list resolves `IOptions<>` and rebuilds enumerable per call. Cache `Tool` instance for lifecycle tool [`Extensions/FrontComposerMcpServiceCollectionExtensions.cs:31-34`]
- [x] [Review][Patch] P32. `FrontComposerMcpCommandInvoker` falls back silently when `FrontComposerMcpLifecycleTracker` is missing. Log a misconfiguration warning when lifecycle is required by adopter [`Invocation/FrontComposerMcpCommandInvoker.cs:48-49`]

#### Deferred (pre-existing, scoped, or paired with a follow-up story)

- [x] [Review][Defer] AC13 — MCP-side telemetry + redaction oracle test missing [`Invocation/FrontComposerMcpLifecycleTracker.cs`, `Invocation/FrontComposerMcpCommandInvoker.cs`] — paired with broader Epic 8 telemetry hardening (T8 line 144)
- [x] [Review][Defer] T4 — `IPendingCommandStateService` / `IPendingCommandOutcomeResolver` not consumed; `IdempotentConfirmed` derived from boolean flag [`Invocation/FrontComposerMcpLifecycleTracker.cs`] — listed as future hardening; current behavior correct for tested cases. Track to Story 8-4 / 8-6.
- [x] [Review][Defer] AC24 size-class parity between hidden/unknown vs in-progress vs terminal not asserted [`tests/.../CommandLifecycleTests.cs`] — pair with timing-oracle/retry-guidance hardening (Story 10-6)
- [x] [Review][Defer] `_historyTruncated` never resets — semantics intentional ("ever truncated") but should be documented or replaced with `transitionsObservedCount`
- [x] [Review][Defer] `CommandRejectedException` inner reason dropped without trace logging — pair with telemetry/redaction work
- [x] [Review][Defer] Per-entry `_gate` reentrancy under recursive transitions — speculative; depends on `ILifecycleStateService` callback contract not documented as reentrant

