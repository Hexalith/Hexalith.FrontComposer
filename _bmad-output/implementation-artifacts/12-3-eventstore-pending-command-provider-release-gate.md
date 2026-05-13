# Story 12.3: EventStore Pending-Command Provider Release Gate

Status: ready-for-dev

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
| Current runtime state | `ServiceCollectionExtensions` registers `IPendingCommandStatusQuery` as `NullPendingCommandStatusQuery`; `PendingCommandPollingCoordinator` already consumes the seam. |
| Release-blocking rows | Primary rows: `DW-0461` and `DW-0465`. Related evidence rows: `DW-0232` and superseded `DW-0469`. Ordered row-id hash: `sha256:0dab7c485e56f0637271be6e1b6af8c15037e48a209a3886d3e22e7269846702`. |
| Provider behavior if implemented | Cover 202 pending/accepted, 200 terminal, 304 not modified, 429 rate limit, 503 unavailable, malformed payload, duplicate terminal, stale terminal, and provider exception. |
| HTTP contract | Reuse the EventStore HTTP contract style from `EventStoreCommandClient`, `EventStoreQueryClient`, `EventStoreResponseClassifier`, `EventStoreOptions`, and existing Shell tests. |
| Tenant/user safety | Fail closed before HTTP calls when tenant, user, command metadata, status URI, token, or provider contract data is missing or mismatched. |
| ETag and retry | Status polling must have deterministic validator, cache/no-change, retry-after, retry-budget, cancellation, and backoff behavior. Use fake clock or injected scheduler where timing matters. |
| Reconnect epochs | Stale terminal observations from an older reconnect/status epoch must not override newer pending-command state. If epoch metadata is unavailable, record it as a release constraint or release blocker. |
| Evidence and docs | All evidence is bounded and sanitized: no raw headers, tokens, tenant/user IDs, command payloads, local absolute paths, raw response bodies, or unbounded logs. |
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

---

## Tasks / Subtasks

- [ ] T1. Capture pending-status release-gate inventory (AC1)
  - [ ] Read `deferred-work.md` around `DW-0232`, `DW-0461`, `DW-0465`, and `DW-0469`.
  - [ ] Record row state, owner/decision owner, release risk, reopen trigger, and ordered row-id hash in the Dev Agent Record.
  - [ ] Confirm whether Story 12.1 or Story 12.2 changed the route before starting code changes.

- [ ] T2. Make the provider decision (AC2, AC14)
  - [ ] Decide with the current repository evidence whether v1 must implement the provider or can accept null-provider-only behavior.
  - [ ] If accepted, write the full release-constraint metadata and release-note wording.
  - [ ] If implementation is required, identify the stable EventStore status endpoint contract and metadata source before writing code.

- [ ] T3. Design the provider contract boundary (AC3-AC5, AC13)
  - [ ] Add or confirm `EventStoreOptions` status endpoint/status metadata fields only if the contract is stable.
  - [ ] Keep tenant/user/token/message/correlation validation fail-closed before HTTP send.
  - [ ] Reuse `HttpClientFactory`, `EventStoreAccessTokenGuard`, `EventStoreRequestContent`, classifier/redaction patterns, and central option validation.
  - [ ] Avoid building status URLs from raw response values unless they are validated as relative, same-host, bounded, and contract-approved.

- [ ] T4. Implement provider-backed status if chosen (AC6-AC13, AC15)
  - [ ] Implement a concrete EventStore `IPendingCommandStatusQuery` provider in the Shell EventStore boundary or extraction-ready EventStore seam.
  - [ ] Map pending, confirmed, idempotent confirmed, rejected, and needs-review outcomes to existing `PendingCommandOutcomeObservation` and resolver paths.
  - [ ] Add ETag/304, retry-after, 429/503, malformed payload, duplicate terminal, stale terminal, and provider-exception handling.
  - [ ] Add fake-clock or injected-scheduler tests for retry/backoff/budget behavior where time is involved.

- [ ] T5. Update release evidence and docs (AC14, AC16)
  - [ ] Update `deferred-work.md` rows with final state: implemented, accepted constraint, or release blocker.
  - [ ] Update this story's Dev Agent Record with decision, changed files, test evidence, and residual risk.
  - [ ] Add bounded release-note/docs wording if null-provider-only behavior remains a v1 constraint.

- [ ] T6. Validate completion (AC15-AC17)
  - [ ] Run focused tests:

```powershell
dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~PendingCommand|FullyQualifiedName~EventStore"
```

  - [ ] Run YAML parse and status-artifact consistency.
  - [ ] Run `git diff --check`.
  - [ ] Run the main-lane filter if shared contracts, source-generation, registration, or EventStore HTTP behavior changed broadly.

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
- Run broader main-lane tests only if shared contracts, EventStore HTTP behavior, DI registration, or options validation changes have cross-cutting impact.

---

## Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 11.7 | Story 12.3 | `DW-0461` and `DW-0465` define the remaining release-blocking pending-status provider decision. |
| `EventStoreCommandClient` | Story 12.3 provider | Accepted command metadata, message id, correlation id, retry hints, and optional status location must remain bounded and safe. |
| EventStore status endpoint | `IPendingCommandStatusQuery` provider | Stable status URL/schema/ETag/retry contract is required before provider-backed readiness can be claimed. |
| `PendingCommandPollingCoordinator` | `PendingCommandOutcomeResolver` | Provider observations must map through existing resolver semantics without bypassing duplicate, unknown, disposed, or lifecycle failure handling. |
| Story 12.3 | Release owner | Provider status is either implemented with evidence, accepted as a v1 constraint, or left as a named release blocker. |
| Story 12.3 | Stories 12.4 and 12.5 | Trusted release publication and acceptance evidence remain separate gates even if provider status is resolved. |

---

## Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Trusted release-context signing, SBOM, attestation, package inventory, and dry-run evidence. | Story 12.4 |
| Manual accessibility, localization/RTL/AT, real-device, and stakeholder acceptance evidence. | Story 12.5 |
| Visible-lane DataGrid/count `RegisterLane` generator wiring. | Story 11.4 successor or named visible-lane story |
| SignalR factory wrapper production seam tests beyond pending-status needs. | Named SignalR factory wrapper test story |
| Enum/status consolidation across optimistic badge, pending-command status, and terminal outcomes. | Story 9.4 enum/status governance |

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

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-13: Story created via `/bmad-create-story 12-3-eventstore-pending-command-provider-release-gate` during recurring pre-dev hardening job.
- 2026-05-13: Pre-creation audit parsed `sprint-status.yaml`, confirmed status-artifact consistency had no drift, found ready buffer count 2, and selected the first backlog story `12-3-eventstore-pending-command-provider-release-gate`.
- 2026-05-13: Starting pending-status release-gate audit identified primary rows `DW-0461` and `DW-0465`, related evidence row `DW-0232`, superseded row `DW-0469`, and ordered row-id fingerprint `sha256:0dab7c485e56f0637271be6e1b6af8c15037e48a209a3886d3e22e7269846702`.

### Completion Notes List

- 2026-05-13: Created the Story 12.3 developer guide and marked it ready for development. Ready for party-mode review on a later recurring run.

### Change Log

- 2026-05-13: Created Story 12.3 and marked ready-for-dev.

### File List

- `_bmad-output/implementation-artifacts/12-3-eventstore-pending-command-provider-release-gate.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
