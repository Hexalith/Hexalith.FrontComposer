# Story 12.3: EventStore Pending-Command Provider Release Gate

Status: review

> **Epic 12** - Release Certification and Evidence Alignment. This story converts the remaining pending-command provider split into a release decision. It applies lessons **L03**, **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 11.7 completed EventStore reliability and CI governance hardening, but it deliberately did not claim provider-backed pending-command status readiness. The current runtime has an `IPendingCommandStatusQuery` seam and default `NullPendingCommandStatusQuery`, while the release-blocking ledger row `DW-0461` says no EventStore status endpoint URL or status-resource metadata is stable enough to prove provider parity.

Story 12.3 is the narrow release-gate pass for that decision. It either implements and validates a real EventStore-backed pending-command status provider, or records a deliberate v1 release constraint with owner, downstream impact, release-note language, and reopen trigger. It must not treat mock-only `IPendingCommandStatusQuery` tests or the null provider as release readiness.

---

## Story

As a release owner,
I want pending-command status provider readiness resolved,
so that command lifecycle confidence is based on provider-backed behavior.

### Release-Readiness Job To Preserve

A release owner should be able to inspect Story 12.3 evidence and know whether pending-command status is provider-backed for v1, explicitly release-blocking, or accepted as a named v1 constraint with visible docs and reopen conditions.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Primary decision | Decide whether v1 implements a real EventStore-backed `IPendingCommandStatusQuery` or ships with a named release constraint. Do not leave "real provider later" ambiguous. |
| Allowed outcomes | Exactly one final outcome is allowed: provider-backed release-ready, named accepted v1 constraint, or release blocker. Mock-only/null-provider-only evidence cannot satisfy the provider-backed outcome. |
| Release owner table | Record evidence found, decision outcome, release implication, required artifact, owner, expiry/revalidation trigger, and linked follow-up or blocker row. |
| Current runtime state | `ServiceCollectionExtensions` registers `IPendingCommandStatusQuery` as `NullPendingCommandStatusQuery`; `PendingCommandPollingCoordinator` already consumes the seam. |
| Release-blocking rows | Primary rows: `DW-0461` and `DW-0465`. Related evidence rows: `DW-0232` and superseded `DW-0469`. Ordered row-id hash: `sha256:0dab7c485e56f0637271be6e1b6af8c15037e48a209a3886d3e22e7269846702`. |
| Inventory drift | Recompute row IDs and hash before ledger mutation. Duplicate, missing, malformed, or drifted rows fail closed until the release-owner table explains the difference. |
| Runtime proof | Provider-backed readiness requires DI/configuration evidence from the real EventStore registration path; hand-registered test doubles or coordinator-only fixtures are insufficient. |
| Provider behavior if implemented | Cover 202 pending/accepted, 200 terminal, 304 not modified, 429 rate limit, 503 unavailable, malformed payload, duplicate terminal, stale terminal, and provider exception. |
| HTTP contract | Reuse the EventStore HTTP contract style from `EventStoreCommandClient`, `EventStoreQueryClient`, `EventStoreResponseClassifier`, `EventStoreOptions`, and existing Shell tests. |
| Tenant/user safety | Fail closed before HTTP calls when tenant, user, command metadata, status URI, token, or provider contract data is missing or mismatched. |
| ETag and retry | Status polling must have deterministic validator, cache/no-change, retry-after, retry-budget, cancellation, and backoff behavior. Use fake clock or injected scheduler where timing matters. |
| Hostile status metadata | Treat status URIs, validators, retry hints, redirects, and terminal timestamps/versions as hostile until bounded, same-host or approved-relative, monotonic, and redacted. |
| Reconnect epochs | Stale terminal observations from an older reconnect/status epoch must not override newer pending-command state. If epoch metadata is unavailable, record it as a release constraint or release blocker. |
| Evidence and docs | All evidence is bounded and sanitized: no raw headers, tokens, tenant/user IDs, command payloads, local absolute paths, raw response bodies, or unbounded logs. |
| Evidence gate | The provider-backed path requires runtime provider fixture evidence. The accepted-constraint path requires owner, user/operator impact, release-note wording, expiry, reopen trigger, and linked follow-up story before any ledger row can close. |
| Scope guardrail | Do not reopen broad EventStore, SignalR, visible-lane, release publishing, MCP, accessibility, or diagnostic governance work. Split adjacent rows to named owners. |
| Validation | Run focused pending-command/EventStore Shell tests; run status-artifact consistency and `git diff --check`. Run broader main-lane only if shared contracts or EventStore source are touched. |

Start here: T1 snapshot pending-status ledger rows -> T2 decide implement vs accept constraint -> T3 design provider contract or constraint metadata -> T4 implement provider and tests if chosen -> T5 update ledger/story/release notes -> T6 validate focused lanes and redaction.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | `deferred-work.md` contains pending-status release-gate rows | Story 12.3 starts | The implementer records the current state of `DW-0461`, `DW-0465`, related `DW-0232`, and superseded `DW-0469`, including ordered row IDs and hash. |
| AC2 | `IPendingCommandStatusQuery` is currently registered as `NullPendingCommandStatusQuery` | Release readiness is assessed | The story records one explicit decision: implement provider-backed status before v1, or accept/null-provider status as a named release constraint. |
| AC3 | The real provider is implemented | Dependency injection runs | `AddHexalithFrontComposer` or EventStore-specific registration replaces the null provider only when a valid EventStore status endpoint contract is configured. |
| AC4 | Provider metadata or endpoint configuration is missing | Pending-command polling runs | The path fails closed or remains null-provider-only with a logged, bounded release constraint; it does not construct ad hoc URLs or infer status resources from untrusted data. |
| AC5 | EventStore command dispatch returns status-resource metadata | The pending command is registered | The status resource is sanitized, bounded, tied to the accepted message id/correlation id, and stored without tenant/user values or command payloads. |
| AC6 | Provider status returns a pending outcome | Polling resolves the response | 202 or equivalent pending/accepted status keeps the entry pending, updates safe validators/retry hints when present, and does not count as a terminal resolution. |
| AC7 | Provider status returns a terminal success | Polling resolves the response | 200 terminal confirmed/idempotent-confirmed maps to `PendingCommandTerminalOutcome.Confirmed` or `IdempotentConfirmed` with duplicate handling preserved. |
| AC8 | Provider status returns a terminal rejection | Polling resolves the response | Rejected/needs-review maps to bounded rejection title/detail/data-impact fields without raw ProblemDetails, payload fragments, tenant/user IDs, or exception text. |
| AC9 | Provider status returns 304 not modified | Polling resolves the response | The previous safe status/validator state is reused without extra terminal dispatch, and invalid/control-character validators are evicted or ignored before send. |
| AC10 | Provider status returns 429 or 503 | Polling resolves the response | Retry-after/backoff and budget behavior are deterministic, redacted, and cannot busy-loop or mark the command terminal. |
| AC11 | Provider status payload is malformed, incomplete, unknown, duplicated, or stale | Polling resolves the response | The path fails closed into `NeedsReview`, duplicate ignored, or explicit retry according to a documented matrix; no stale terminal from an older epoch can override newer state. |
| AC12 | Provider code throws or cancellation is requested | Polling runs | User cancellation is rethrown; provider exceptions are logged by safe category and do not leak exception messages, raw response bodies, or identifiers. |
| AC13 | Tenant, user, token, message id, correlation id, status URI, or command metadata is missing or mismatched | Provider query is attempted | The provider fails before HTTP send and records bounded evidence; no cross-tenant or cross-user cache/status reuse occurs. |
| AC14 | The project chooses to accept null-provider-only behavior for v1 | Release documentation is prepared | The acceptance names owner, likelihood, impact, release risk, downstream user/agent impact, evidence, release-note wording, expiry/revalidation trigger, and reopen event. |
| AC15 | The project implements provider-backed status | Contract tests run | A single fixture matrix covers 202, 200 terminal, 304, 429, 503, malformed, duplicate, stale, provider-exception, tenant/user fail-closed, ETag, retry-after, redaction, and cancellation behavior. |
| AC16 | Story 12.3 closes | Artifacts are updated | `deferred-work.md`, this story's Dev Agent Record, and any release-note/docs artifact agree on final provider status: implemented, accepted constraint, or release blocker. |
| AC17 | Story 12.3 validation completes | Evidence is recorded | Focused pending-command/EventStore tests, YAML/status-artifact consistency, `git diff --check`, and any broader lanes triggered by source changes are recorded with outcomes. |
| AC18 | The release owner reads Story 12.3 completion evidence | The final decision is recorded | The story includes a release decision table with evidence found, final outcome, release implication, required artifact, owner, expiry/revalidation trigger, reopen event, and linked follow-up or blocker row. |
| AC19 | A provider-backed implementation is claimed | The provider contract is reviewed | The trusted request preconditions and response shape are explicit: message id, correlation id, tenant/principal scope validation, typed status, terminal timestamp or version, safe ETag, retry-after metadata, and sanitized evidence fields. |
| AC20 | Status observations arrive out of order or across reconnect/status epochs | The coordinator resolves them | Terminal observations dominate pending observations, stale epochs cannot override newer state, duplicates are idempotent, 304 never creates a new terminal event, and 429/503 retry budgets cannot busy-loop. |
| AC21 | Fail-closed preconditions reject a provider query | The implementation is tested | Tests assert zero outbound HTTP requests when tenant, user/principal, token, message id, correlation id, status URI, or contract metadata is absent, mismatched, poisoned, external, or untrusted. |
| AC22 | Release evidence is emitted | Redaction validation runs | Logs, markdown evidence, snapshots, test artifacts, status-resource values, exception surfaces, and release-constraint notes are scanned for tenant/user IDs, bearer tokens, raw command payloads, connection strings, local absolute paths, and unsanitized status URIs. |
| AC23 | The story defers provider implementation or adjacent EventStore work | Follow-up ownership is recorded | The deferral names the owner, linked story or blocker row, downstream user/operator/agent impact, release-note wording if user-visible, and revalidation trigger; broad endpoint discovery or schema expansion is not silently absorbed into Story 12.3. |
| AC24 | The starting pending-status row inventory differs from the Story 12.3 creation baseline | Implementation starts or ledger rows are mutated | The implementer records the new ordered row IDs/hash, explains added/removed/renamed rows, and treats duplicate, malformed, missing, or unexplained drift as a release-gate condition before any provider decision is certified. |
| AC25 | Provider-backed readiness is claimed | DI and runtime evidence are reviewed | Evidence proves the real EventStore registration replaces `NullPendingCommandStatusQuery` only under valid configuration and that the focused provider tests exercise that registered provider path, not only a hand-wired fake or coordinator mock. |
| AC26 | Status-resource metadata, validators, redirects, retry hints, terminal timestamps, or versions are malformed, oversized, cross-host, control-character-bearing, non-monotonic, or otherwise hostile | The provider builds or sends a status request | The provider sends zero outbound HTTP requests or fails closed into bounded degraded evidence; it never follows unapproved redirects, persists poisoned validators, trusts non-monotonic terminal data, or logs raw metadata. |
| AC27 | 304, 429, 503, cancellation, and retry-after observations interleave across polling attempts | Retry/backoff evidence is recorded | The story proves deterministic budget consumption, scheduler/fake-clock control, cancellation precedence, bounded retry-after handling, and no busy-loop or terminal-status fabrication across the interleaving. |
| AC28 | Null-provider-only behavior or an incomplete status endpoint is accepted as a v1 constraint | The release owner signs off | The constraint names the trigger watcher, expiry/revalidation date or condition, release-note/package-promotion impact, downstream adopter/agent impact, and the exact event that reopens `DW-0461` or creates the linked follow-up. |
| AC29 | Final provider evidence is summarized | Story 12.3 closes | `deferred-work.md`, this story, provider test evidence, release-note/docs wording, and any release-owner summary reconcile to exactly one final outcome and pass an adversarial redaction scan covering status URIs, validators, retry hints, terminal metadata, exception surfaces, and local path specimens. |

---

## Tasks / Subtasks

- [x] T1. Capture pending-status release-gate inventory (AC1)
  - [x] Read `deferred-work.md` around `DW-0232`, `DW-0461`, `DW-0465`, and `DW-0469`.
  - [x] Record row state, owner/decision owner, release risk, reopen trigger, and ordered row-id hash in the Dev Agent Record.
  - [x] Confirm whether Story 12.1 or Story 12.2 changed the route before starting code changes.
  - [x] Recompute the ordered row-id hash before ledger mutation and record any added, removed, duplicate, malformed, or renamed row IDs as either explained drift or a release gate. (AC24)

- [x] T2. Make the provider decision (AC2, AC14, AC18, AC23, AC28)
  - [x] Decide with the current repository evidence whether v1 must implement the provider or can accept null-provider-only behavior.
  - [x] Fill a release decision table with the mutually exclusive outcome: provider-backed release-ready, named accepted v1 constraint, or release blocker.
  - [x] If accepted, write the full release-constraint metadata and release-note wording.
  - [x] If accepted, name the owner, linked follow-up story or blocker row, user/operator/agent impact, expiry/revalidation trigger, and reopen event.
  - [x] If accepted, name who watches each expiry/revalidation trigger and whether the trigger blocks release notes, package promotion, or only post-v1 backlog.
  - [x] If implementation is required, identify the stable EventStore status endpoint contract and metadata source before writing code.

- [x] T3. Design the provider contract boundary (AC3-AC5, AC13, AC19, AC21, AC25, AC26)
  - [x] Add or confirm `EventStoreOptions` status endpoint/status metadata fields only if the contract is stable.
  - [x] Keep tenant/user/token/message/correlation validation fail-closed before HTTP send.
  - [x] Define typed provider statuses and forbid null-success or stringly success paths.
  - [x] Define trusted request preconditions and response fields before implementing the provider.
  - [x] Reuse `HttpClientFactory`, `EventStoreAccessTokenGuard`, `EventStoreRequestContent`, classifier/redaction patterns, and central option validation.
  - [x] Avoid building status URLs from raw response values unless they are validated as relative, same-host, bounded, and contract-approved.
  - [x] Add a zero-outbound-request fixture for missing, mismatched, poisoned, external, or untrusted status preconditions.
  - [x] Add hostile metadata fixtures for absolute/cross-host URLs, redirects, CRLF/control-character validators, oversized retry hints, non-monotonic terminal timestamps/versions, and raw status-resource fragments.
  - [x] Define how DI evidence proves the real configured provider path is active and the null provider remains the default when configuration is absent or invalid.

- [x] T4. Implement provider-backed status if chosen (AC6-AC13, AC15, AC20-AC22, AC25-AC27)
  - [x] Implement a concrete EventStore `IPendingCommandStatusQuery` provider in the Shell EventStore boundary or extraction-ready EventStore seam.
  - [x] Map pending, confirmed, idempotent confirmed, rejected, and needs-review outcomes to existing `PendingCommandOutcomeObservation` and resolver paths.
  - [x] Add ETag/304, retry-after, 429/503, malformed payload, duplicate terminal, stale terminal, and provider-exception handling.
  - [x] Prove terminal dominance, duplicate idempotency, stale epoch suppression, 304 no-change behavior, and non-terminal 429/503 retry budget behavior.
  - [x] Treat provider exceptions as degraded/non-ready evidence unless explicitly classified; do not swallow them as readiness and do not convert them into terminal success.
  - [x] Add fake-clock or injected-scheduler tests for retry/backoff/budget behavior where time is involved.
  - [x] Prove cancellation wins over retry-after/backoff scheduling and does not leave cached poisoned validators or terminal observations behind.
  - [x] Prove provider-backed tests use the production DI/registration path and fail if only `NullPendingCommandStatusQuery` or a hand-wired fake is present.

- [x] T5. Update release evidence and docs (AC14, AC16, AC18, AC22, AC23, AC28, AC29)
  - [x] Update `deferred-work.md` rows with final state: implemented, accepted constraint, or release blocker.
  - [x] Update this story's Dev Agent Record with decision, changed files, test evidence, and residual risk.
  - [x] Add bounded release-note/docs wording if null-provider-only behavior remains a v1 constraint.
  - [x] Record the sanitized evidence manifest fields and redaction scan result.
  - [x] Reconcile `deferred-work.md`, release-note/docs wording, provider test evidence, and the release-owner summary to exactly one final outcome.
  - [x] Record adversarial redaction fixtures for status URI, validator, retry hint, terminal metadata, exception category, and local path specimens.

- [x] T6. Validate completion (AC15-AC17, AC21-AC22, AC24-AC29)
  - [x] Run focused tests:

```powershell
dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~PendingCommand|FullyQualifiedName~EventStore"
```

  - [x] Run focused provider contract, DI, and fail-closed lanes when matching tests exist:

```powershell
dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~PendingCommandStatus|FullyQualifiedName~EventStorePending|FullyQualifiedName~NullPendingCommandStatusQuery"
```

  - [x] Run a repository evidence redaction scan over changed markdown, logs, snapshots, and provider test artifacts.
  - [x] Run the row inventory/hash check again and record any drift disposition before final ledger update.
  - [x] Run a production-DI provider replacement proof or record the missing proof as a release blocker/accepted constraint.
  - [x] Run YAML parse and status-artifact consistency.
  - [x] Run `git diff --check`.
  - [x] Run the main-lane filter if shared contracts, source-generation, registration, or EventStore HTTP behavior changed broadly.

---

## Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Mock-only and null-provider-only pending-command polling is not release-certification evidence. | Story 11.7 explicitly split provider parity as release-blocking until a real status contract exists or is accepted as a constraint. |
| D2 | Story 12.3 owns only pending-command provider release readiness. | Trusted release evidence, accessibility evidence, MCP ledger closure, visible-lane registration, and broad SignalR work have separate owners. |
| D3 | A provider implementation requires a stable EventStore status endpoint contract. | Ad hoc URL inference or unstable metadata would make release readiness look stronger than it is. |
| D4 | Tenant/user and command metadata validation happens before HTTP send or cache/status reuse. | Cross-tenant status lookup is a security bug and violates project context rules. |
| D5 | Status polling shares EventStore HTTP safety patterns. | Query/command/status endpoints must classify HTTP drift, ETag, retry-after, redaction, and payload bounds consistently. |
| D6 | Accepted null-provider behavior is a release decision, not a developer note. | It must name owner, impact, reopen trigger, release-note wording, and validation evidence. |
| D7 | Reconnect/stale terminal behavior must be explicit. | Provider-backed status introduces races that mock-only tests cannot prove safe. |
| D8 | Evidence remains bounded and sanitized. | Release artifacts cannot include raw headers, tokens, tenant/user IDs, payloads, local paths, or unbounded logs. |
| D9 | No recursive nested submodule commands are needed. | This story works in FrontComposer Shell/EventStore seams and repository artifacts only. |
| D10 | The final release outcome is mutually exclusive. | A story cannot both claim provider-backed readiness and accept null-provider-only behavior; release owners need one clear state. |
| D11 | Provider-backed readiness requires runtime provider evidence, not coordinator-only mocks. | Coordinator tests prove the seam, but not EventStore provider parity or contract stability. |
| D12 | Fail-closed means no outbound status request and no terminal success. | Missing or mismatched tenant/user/token/message/correlation/status metadata must stop before side effects. |
| D13 | Terminal-state ordering is part of the provider contract. | Duplicate, stale, 304, reconnect, and retry observations can otherwise make release evidence nondeterministic. |
| D14 | Accepted constraints require a named follow-up or blocker row. | L10 prevents vague "future EventStore work" from becoming a hidden release debt. |
| D15 | Row inventory drift fails closed before provider certification. | Certifying the wrong `DW-0461`/`DW-0465` evidence set would create false release readiness even if provider tests pass. |
| D16 | Runtime DI replacement proof is mandatory for provider-backed readiness. | A provider implemented only in isolated tests does not prove that release configuration stops using the null provider. |
| D17 | Status-resource metadata is hostile by default. | URLs, validators, retry hints, timestamps, and versions cross a service boundary and must be bounded before send, cache, log, or decision use. |
| D18 | Retry/backoff behavior is release evidence, not an implementation detail. | A busy-loop, swallowed cancellation, or fabricated terminal after retry pressure can break command lifecycle confidence under outage conditions. |
| D19 | Accepted constraints need active trigger watching. | Expiry/revalidation text without a watcher and package/release consequence silently converts a release constraint into forgotten backlog. |
| D20 | Final provider outcome must reconcile across artifacts. | Divergent ledger, story, test, and release-note evidence lets release owners over-certify or under-document pending-command readiness. |

---

## Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs` | Update possible | Existing seam consumer; preserve budget, cancellation, duplicate handling, and safe logging. |
| `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandModels.cs` | Update possible | Only if status-resource metadata must be stored; avoid raw payload, tenant, user, or headers. |
| `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` | Update possible | Replace null provider only through explicit EventStore/provider registration. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/**` | Update possible | Preferred location for EventStore-backed provider, options, validator, HTTP parsing, and redaction helpers. |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/**` | Update | Provider seam, resolver, polling, duplicate, stale, and exception coverage. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/**` | Update | HTTP matrix, tenant/user fail-closed, ETag, retry-after, payload-bound, and redaction coverage. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update | Final row-state decision for `DW-0461`, `DW-0465`, related rows, and release constraint/reopen metadata. |
| `_bmad-output/implementation-artifacts/12-3-eventstore-pending-command-provider-release-gate.md` | Update | Dev Agent Record, validation evidence, file list, and completion notes. |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Avoid during implementation | Normal dev workflow owns status transitions; do not change during this story unless a blocker is explicitly declared. |

No unrelated MCP, SourceTools, Shell UX, accessibility pack, release publishing, docs-site generation, or submodule content should be changed by default.

---

## Project Structure Notes

- Pending-command runtime state lives under `src/Hexalith.FrontComposer.Shell/State/PendingCommands`.
- EventStore HTTP clients and options live under `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore`.
- Shell tests mirror those boundaries under `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands` and `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore`.
- Keep status evidence repository-relative and sanitized. Do not paste raw response bodies, bearer tokens, tenant/user IDs, command payloads, local absolute paths, or unbounded logs.
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`; do not initialize or update nested submodules.

---

## Testing Strategy

- Start with focused Shell tests for `PendingCommandPollingCoordinator`, `PendingCommandStateService`, `PendingCommandOutcomeResolver`, and EventStore HTTP classification.
- Add a provider contract fixture matrix that names each status case and expected resolver/caching/retry/redaction outcome.
- Use `Microsoft.Extensions.Time.Testing.FakeTimeProvider` or an injected scheduler for retry/backoff timing. Do not add sleeps.
- Add negative redaction tests for raw header/body/token/tenant/user/path leakage.
- Add production-DI replacement proof for the EventStore-backed provider and a negative proof that missing/invalid configuration keeps the null provider or fails closed.
- Include hostile status metadata fixtures for cross-host or absolute URLs, redirects, poisoned validators, oversized retry hints, non-monotonic terminal metadata, stale epochs, and cancellation interleavings.
- Reconcile final outcome evidence across the ledger, story Dev Agent Record, release notes/docs, provider fixture matrix, and redaction scan before marking the story complete.
- Run broader main-lane tests only if shared contracts, EventStore HTTP behavior, DI registration, or options validation changes have cross-cutting impact.

---

## Release Outcome Evidence Contract

| Outcome | Required evidence | Required artifact updates |
| --- | --- | --- |
| Provider-backed release-ready | Runtime EventStore-backed provider tests, DI replacement proof, zero-outbound fail-closed tests, HTTP/status fixture matrix, redaction scan, and final ledger row state. | `deferred-work.md`, this story's Dev Agent Record, provider test evidence, and any release evidence note agree that provider-backed status is implemented. |
| Named accepted v1 constraint | Constraint owner, likelihood, impact, downstream user/operator/agent impact, release-note wording, expiry/revalidation trigger, reopen event, and linked follow-up story or blocker row. | `deferred-work.md`, this story's Dev Agent Record, and release-note/docs artifact agree that null-provider-only behavior is intentionally accepted for v1. |
| Release blocker | Blocking row owner, exact missing contract/evidence, affected release lane, and next decision owner. | `deferred-work.md`, this story's Dev Agent Record, and sprint/release evidence agree that Story 12.3 cannot certify pending-command provider readiness yet. |

## Provider Contract Minimum

If provider-backed status is implemented, the story must document the trusted contract before code is accepted:

- Request preconditions: tenant/principal scope, bearer token availability, message id, correlation id, status URI, and provider metadata are present, bounded, same-host or approved-relative, and tied to the accepted command.
- Response shape: typed status, message id or equivalent command binding, correlation id, terminal timestamp or monotonic version, safe ETag/validator, retry-after hint, and sanitized evidence fields.
- State ordering: terminal outcomes dominate pending outcomes, stale epochs cannot override newer state, duplicate terminal observations are idempotent, 304 is no-change only, and 429/503 remain non-terminal with bounded retry.
- Failure behavior: malformed payloads, poisoned validators, external status URIs, provider exceptions, and missing contract data become retry, `NeedsReview`, degraded evidence, or blocker according to the fixture matrix; they never become terminal success by default.

## Row-Addressable Fixture Matrix

| Dimension | Required rows |
| --- | --- |
| Provider mode | null provider accepted constraint, valid EventStore provider, provider configured but invalid, provider throws. |
| HTTP/status outcome | 202 pending/accepted, 200 confirmed, 200 idempotent confirmed, 200 rejected/needs-review, 304, 429, 503, malformed body/header, unknown status. |
| Identity and metadata | missing/mismatched tenant, user/principal, token, message id, correlation id, status URI, command metadata, poisoned/external status URI. |
| Ordering and lifecycle | duplicate terminal, stale terminal after newer pending, terminal without matching command, cancellation before query, cancellation during query, reconnect/status epoch mismatch. |
| DI/configuration proof | null-provider default, valid EventStore provider replacement, invalid options fail closed, test double cannot satisfy release-ready proof. |
| Retry and validator hostility | CRLF/control-character validator, oversized validator, hostile retry-after, redirect response, cross-host status URL, non-monotonic terminal timestamp/version. |
| Redaction | logs, markdown evidence, snapshots, test artifacts, exception surfaces, correlation metadata, EventStore payload specimens, and release-constraint notes. |

---

## Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 11.7 | Story 12.3 | `DW-0461` and `DW-0465` define the remaining release-blocking pending-status provider decision. |
| `EventStoreCommandClient` | Story 12.3 provider | Accepted command metadata, message id, correlation id, retry hints, and optional status location must remain bounded and safe. |
| EventStore status endpoint | `IPendingCommandStatusQuery` provider | Stable status URL/schema/ETag/retry contract is required before provider-backed readiness can be claimed. |
| `PendingCommandPollingCoordinator` | `PendingCommandOutcomeResolver` | Provider observations must map through existing resolver semantics without bypassing duplicate, unknown, disposed, or lifecycle failure handling. |
| Story 12.3 | Release owner | Provider status is either implemented with evidence, accepted as a v1 constraint, or left as a named release blocker. |
| Story 12.3 | Future provider follow-up or blocker owner | Any deferred status endpoint discovery, schema expansion, or accepted null-provider constraint must name the owner, linked row/story, expiry, and reopen trigger. |
| Story 12.3 | Stories 12.4 and 12.5 | Trusted release publication and acceptance evidence remain separate gates even if provider status is resolved. |
| Story 12.3 | Release notes/docs and package promotion evidence | Accepted constraints or release blockers must be visible in release wording before package promotion can claim command lifecycle readiness. |

---

## Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Trusted release-context signing, SBOM, attestation, package inventory, and dry-run evidence. | Story 12.4 |
| Manual accessibility, localization/RTL/AT, real-device, and stakeholder acceptance evidence. | Story 12.5 |
| Visible-lane DataGrid/count `RegisterLane` generator wiring. | Story 11.4 successor or named visible-lane story |
| SignalR factory wrapper production seam tests beyond pending-status needs. | Named SignalR factory wrapper test story |
| Enum/status consolidation across optimistic badge, pending-command status, and terminal outcomes. | Story 9.4 enum/status governance |
| Broader EventStore status endpoint discovery or schema negotiation beyond the minimum provider contract. | Named EventStore provider follow-up or explicit release blocker row |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-12-release-certification-evidence-alignment.md#Story-12.3`] - story statement, acceptance criteria, and Epic 12 release-certification scope.
- [Source: `_bmad-output/implementation-artifacts/11-7-eventstore-reliability-and-ci-governance-follow-ups.md`] - provider-backed status split, AC32, decisions D15/D23, and implementation evidence.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#DW-0461`] - release-blocking pending-command status provider row.
- [Source: `_bmad-output/implementation-artifacts/epic-11-retro-2026-05-13.md`] - critical path item naming provider-backed pending-command status as release-blocking unless accepted.
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13.md`] - approved Epic 12 release-certification correction.
- [Source: `_bmad-output/planning-artifacts/architecture.md#API-Boundaries-EventStore-external-service`] - EventStore REST, query, ETag, SignalR, and contract boundary.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR23`] - command lifecycle contract.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR52`] - two-call lifecycle and terminal-state expectations.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md`] - EventStore seam, security, reliability, and provider-verification expectations.
- [Source: `_bmad-output/project-context.md`] - project rules for EventStore, tenant/user safety, testing, release evidence, redaction, and submodules.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L03--Tenantuser-isolation-guards-fail-closed`] - tenant/user fail-closed guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06--Defense-in-depth-budget-per-story`] - scope and decision budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L07--Test-count-inflation-is-a-cost`] - test-scope budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08--Party-review-vs-elicitation--different-roles`] - later party review and elicitation sequencing.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10--Deferrals-need-story-specificity-not-epic-specificity`] - named owner requirement.

---

## Dev Agent Record

## Party-Mode Review

- Date/time: 2026-05-13T20:59:06+02:00
- Selected story: `12-3-eventstore-pending-command-provider-release-gate`
- Command/skill invocation used: `/bmad-party-mode 12-3-eventstore-pending-command-provider-release-gate; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary: All agents initially recommended `needs-story-update`. They found that the story targeted the right release risk but left the provider contract, mutually exclusive release outcome, accepted-constraint ownership, fail-closed proof, polling state-machine ordering, row-addressable fixture matrix, and redaction evidence too implicit for development.
- Changes applied: Added explicit allowed outcomes, release-owner decision table expectations, AC18-AC23, strengthened T2-T6, added D10-D14, added release outcome evidence contract, provider contract minimum, row-addressable fixture matrix, future-follow-up contract, and broader EventStore follow-up guardrail.
- Findings deferred: Product/architecture decision on whether v1 implements provider-backed status or accepts null-provider-only behavior; any broad EventStore status endpoint discovery or schema expansion; trusted release evidence, accessibility evidence, MCP ledger closure, docs-site generation, release publishing, and SignalR work outside pending-status needs.
- Final recommendation: `ready-for-dev`

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-13: Story created via `/bmad-create-story 12-3-eventstore-pending-command-provider-release-gate` during recurring pre-dev hardening job.
- 2026-05-13: Pre-creation audit parsed `sprint-status.yaml`, confirmed status-artifact consistency had no drift, found ready buffer count 2, and selected the first backlog story `12-3-eventstore-pending-command-provider-release-gate`.
- 2026-05-13: Starting pending-status release-gate audit identified primary rows `DW-0461` and `DW-0465`, related evidence row `DW-0232`, superseded row `DW-0469`, and ordered row-id fingerprint `sha256:0dab7c485e56f0637271be6e1b6af8c15037e48a209a3886d3e22e7269846702`.
- 2026-05-13: Party-mode pre-dev review found missing provider-contract, release-outcome, constraint-owner, state-machine, fixture-matrix, and redaction gates; low-risk story hardening applied inline.
- 2026-05-14T10:01:27+02:00: Advanced elicitation pre-dev hardening found residual drift, runtime-DI proof, hostile status metadata, retry/cancellation, constraint-aging, and final-evidence reconciliation risks; low-risk story guardrails applied inline.
- 2026-05-15: Started Story 12.3 under `/bmad-dev-story`; sprint status moved to `in-progress`.
- 2026-05-15: Starting row inventory found ordered rows `DW-0232`, `DW-0461`, `DW-0465`, `DW-0469`; row count 666; duplicate row IDs none; missing target rows none; ordered target fingerprint `sha256:0dab7c485e56f0637271be6e1b6af8c15037e48a209a3886d3e22e7269846702`.
- 2026-05-15: Story 12.1 and Story 12.2 reviewed. Both explicitly routed provider-backed pending-command status release-gate work to Story 12.3 and did not change the EventStore provider route.
- 2026-05-15: Runtime evidence reviewed: `AddHexalithFrontComposer` registers `IPendingCommandStatusQuery` as `NullPendingCommandStatusQuery`; `AddHexalithEventStore` registers EventStore command/query/subscription services but does not replace the null provider.
- 2026-05-15: Final decision recorded as named accepted v1 constraint `PENDING-STATUS-NULL-PROVIDER-V1`; provider-backed readiness is not claimed.

### Implementation Plan

- Use the accepted-constraint path because the repository still lacks a stable EventStore pending-command status endpoint URL, schema, validator, retry, and reconnect/status epoch contract.
- Do not implement a concrete provider or new `EventStoreOptions` status fields without that stable contract.
- Reconcile `DW-0461`, `DW-0465`, related `DW-0232`, and superseded `DW-0469` to the same final release outcome.
- Add bounded release-note wording and validate current pending-command/EventStore behavior plus evidence hygiene.

### Release-Gate Inventory

| Row | Starting state | Story 12.3 disposition | Owner / watcher | Release risk | Reopen trigger |
| --- | --- | --- | --- | --- | --- |
| `DW-0232` | Split to Story 11.7 for strict server correlation ULID validation | Accepted constraint under `PENDING-STATUS-NULL-PROVIDER-V1` | Shell/EventStore integration owner / release owner role | Non-blocking while provider-backed status is not claimed | Reopen with `DW-0461` before consuming server-supplied status metadata |
| `DW-0461` | Split to EventStore status-endpoint contract; release-blocking until provider contract lands | Accepted constraint under `PENDING-STATUS-NULL-PROVIDER-V1` | Shell/EventStore integration owner / release owner role | Blocks only provider-backed readiness language; v1 may ship with explicit null-provider constraint | Reopen before provider-backed readiness claim, status-resource metadata consumption, or EventStore endpoint promotion |
| `DW-0465` | Split to pending-status provider/reconnect epoch story | Accepted constraint under `PENDING-STATUS-NULL-PROVIDER-V1` | Shell/EventStore integration owner / release owner role | Blocks future provider-backed status until epoch metadata exists | Reopen when provider observes terminal statuses across reconnect/status epochs |
| `DW-0469` | Superseded by `DW-0461` | Superseded-preserved | Shell/EventStore integration owner / release owner role | Inherited by `DW-0461` | Reopen only through `DW-0461` |

Ordered target row IDs: `DW-0232`, `DW-0461`, `DW-0465`, `DW-0469`.
Starting and final target fingerprint: `sha256:0dab7c485e56f0637271be6e1b6af8c15037e48a209a3886d3e22e7269846702`.
Inventory drift: none. Duplicate row IDs: none. Missing target rows: none.

### Release Decision

| Field | Decision |
| --- | --- |
| Final outcome | Named accepted v1 constraint |
| Constraint name | `PENDING-STATUS-NULL-PROVIDER-V1` |
| Evidence found | `NullPendingCommandStatusQuery` is the registered default; EventStore registration does not replace it; no stable status endpoint contract exists in repository evidence. |
| Release implication | v1 must not claim provider-backed pending-command status readiness. |
| Required artifact | `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md` |
| Owner | Shell/EventStore integration owner |
| Trigger watcher | Release owner role |
| Expiry/revalidation trigger | 2026-06-30 or stable EventStore status-resource metadata, whichever comes first |
| Reopen event | Provider-backed readiness claim, status-resource metadata consumption, or EventStore endpoint promotion |
| Linked row | `DW-0461` |

Release-note wording is recorded in `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md`. The note states that command dispatch, lifecycle registration, live projection nudges, reconnect reconciliation, and bounded fallback polling remain supported, while direct EventStore-backed status polling is an accepted v1 constraint.

### Provider Contract Boundary

Provider-backed implementation was not chosen. The future provider gate remains closed until the contract explicitly defines trusted request preconditions, typed response shape, safe validator and retry metadata, terminal timestamp or monotonic version behavior, reconnect/status epoch ordering, redaction fixtures, zero-outbound fail-closed cases, and production DI replacement proof. `NullPendingCommandStatusQuery` remains the valid default when configuration is absent or invalid.

### Validation Evidence

| Check | Outcome |
| --- | --- |
| Focused pending-command/EventStore tests | Passed: 177/177 with `dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~PendingCommand|FullyQualifiedName~EventStore"` |
| Provider-specific filter | No matching tests, expected because no concrete provider was implemented: `FullyQualifiedName~PendingCommandStatus|FullyQualifiedName~EventStorePending|FullyQualifiedName~NullPendingCommandStatusQuery` |
| Row inventory/hash | Passed: row count 666, no duplicate row IDs, ordered target hash unchanged |
| YAML parse/status consistency | Passed: Story 12.3 transitioned `in-progress` during work and `review` at completion |
| Redaction scan | Passed for Story 12.3 evidence lines; earlier broad scans flagged historical/spec wording and pre-existing unrelated deferred-work diff, not Story 12.3 leaked values |
| `git diff --check` | Passed; Git reported line-ending normalization warnings only |
| Main-lane filter | Not run because no shared contracts, source-generation, registration code, or EventStore HTTP behavior changed |

### Completion Notes List

- 2026-05-13: Created the Story 12.3 developer guide and marked it ready for development. Ready for party-mode review on a later recurring run.
- 2026-05-13: Party-mode hardening applied. Story remains ready-for-dev after adding release outcome, provider contract, fixture matrix, accepted-constraint, and evidence redaction guardrails.
- 2026-05-14T10:01:27+02:00: Advanced elicitation hardening applied. Story remains ready-for-dev after adding row-drift, runtime provider proof, hostile metadata, retry budget, constraint trigger, and cross-artifact reconciliation guardrails.
- 2026-05-15: Completed Story 12.3 as the named accepted v1 constraint `PENDING-STATUS-NULL-PROVIDER-V1`. Updated the ledger rows, added release-note wording, validated focused Shell tests, and moved the story to review.

### Change Log

- 2026-05-13: Created Story 12.3 and marked ready-for-dev.
- 2026-05-13: Applied party-mode review hardening for Story 12.3.
- 2026-05-14T10:01:27+02:00: Applied advanced elicitation hardening; added AC24-AC29, D15-D20, and task/testing guardrails for row drift, runtime DI provider proof, hostile status metadata, retry/cancellation budgets, constraint aging, and final evidence reconciliation.
- 2026-05-15: Accepted null-provider-only pending-command status as a named v1 constraint, updated deferred-work rows and release-note wording, recorded validation evidence, and marked story ready for review.

## Advanced Elicitation

- **Date/time:** 2026-05-14T10:01:27+02:00
- **Selected story:** `12-3-eventstore-pending-command-provider-release-gate`
- **Command/skill invocation used:** `/bmad-advanced-elicitation 12-3-eventstore-pending-command-provider-release-gate`
- **Batch 1 methods:** Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation.
- **Batch 2 methods:** Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records.
- **Findings summary:** The remaining failure modes were false provider-backed readiness from test-only DI, certifying drifted ledger rows, trusting hostile status metadata, losing cancellation/retry ordering under outage pressure, letting accepted null-provider constraints age without a watcher, and producing final ledger/story/release-note evidence that disagrees.
- **Changes applied:** Added AC24-AC29; tightened T1-T6; added D15-D20; expanded the cheat sheet, testing strategy, fixture matrix, and cross-story contracts with row inventory drift, runtime provider replacement proof, hostile status metadata, retry/backoff/cancellation evidence, accepted-constraint trigger ownership, and final cross-artifact reconciliation.
- **Findings deferred:** The final product/architecture decision on implementing provider-backed status versus accepting null-provider-only behavior; the exact owner who watches each accepted constraint trigger; whether long-lived provider evidence is stored as a generated machine-readable matrix or only in Dev Agent Record tables; any broader EventStore status endpoint discovery or schema expansion beyond Story 12.3.
- **Final recommendation:** `ready-for-dev`

### File List

- `_bmad-output/implementation-artifacts/12-3-eventstore-pending-command-provider-release-gate.md`
- `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
