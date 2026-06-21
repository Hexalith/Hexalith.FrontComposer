# FC-RETRY v1 Retry and Degraded-State Contract

Date: 2026-06-05
Owner: FrontComposer Shell and EventStore command adapter
Status: confirmed v1 contract (ratified 2026-06-21 — sprint-change-proposal-2026-06-21)

## Decision

FrontComposer retries only EventStore command dispatch failures that happen before `202 Accepted`.
The default v1 budget is one retry after the initial attempt, with a deterministic 250 ms delay and no
jitter. Every attempt reuses the same `MessageId`, tenant, domain, aggregate id, command type, and
serialized payload.

## Retryable Faults

Retryable faults are narrowly scoped to pre-accept transport `HttpRequestException` without a
non-retryable status and HTTP `408`, `502`, `503`, and `504`.

Non-retryable outcomes include `400`, `401`, `403`, `404`, `409`, `429`, malformed accepted
responses, oversized request or response guards, tenant resolution failures, caller cancellation,
FC-CNC denial, authorization denial, domain rejection, and anything after pending registration.

## Slow Accepted Commands

Accepted commands are never re-dispatched. After `202 Accepted`, generated forms register pending
state and continue through the existing command-status polling flow. `FcLifecycleWrapper` surfaces
the degraded action prompt after `TimeoutActionThresholdMs`; `PendingCommandPollingDriver` continues
at `PendingCommandPollingIntervalMs` until `MaxPendingCommandPollingDurationMs`, then the resolver
moves unresolved entries to `NeedsReview`. First terminal outcome wins.

## Presentation

Retry exhaustion is a retryable degraded warning, not a terminal lifecycle outcome. Generated forms
reset to `Idle`, preserve entered values, show accessible warning feedback, and may display the
bounded retry-delay hint. They do not dispatch `AcknowledgedAction`, register pending state, or claim
EventStore queued the command because no `202 Accepted` was observed.

`FcPendingCommandSummary` lists active pending entries and terminal rejected or needs-review entries
inside a bounded `aria-live="polite"` summary. Rejected entries remain non-dismissible error message
bars. No command payloads, tenant or user claims, access tokens, or raw exception/server body text are
rendered.

## Non-Goals

No queueing, batching, background re-dispatch after acceptance, MCP tool retry policy, EventStore
status endpoint shape change, projection fallback change, Polly or third-party retry dependency, or
automatic retry for the non-EventStore Stub dispatcher is introduced.
