# Story 11.7: EventStore Reliability and CI Governance Follow-ups

Status: ready-for-dev

> **Epic 11** - Deferred Hardening & Release Readiness. Closes EventStore integration, realtime reliability, command-status polling, telemetry/exporter, release workflow, and CI governance follow-ups routed from Epics 1, 3, 5, and 10. Applies lessons **L03**, **L06**, **L07**, **L08**, **L10**, and **L14**.

---

## Executive Summary

Story 11.7 is the release-readiness hardening pass for FrontComposer's EventStore-facing runtime and the CI/release checks that prove it is shippable.

Earlier stories delivered EventStore command/query clients, ETag cache behavior, SignalR projection nudges, reconnect reconciliation, pending-command status polling, telemetry, pacts, quarantine governance, and release evidence. Review notes left focused gaps around provider-backed command status integration, response-size and response-parity behavior, SignalR reconnect/fallback coverage, telemetry redaction and exporter guidance, visible-lane registration, CI lane isolation, and release workflow ordering.

This story does not reopen all EventStore, diagnostics, SourceTools, or release-packaging work. It inventories the Story 11.7-owned deferred rows, implements the high-value reliability and governance fixes, and records accepted constraints or splits with concrete evidence. The release owner should finish this story with a clear pass/fail view of EventStore runtime readiness and the remaining release-gate risks.

---

## Story

As a release owner,
I want EventStore integration, realtime reliability, and CI governance deferrals closed,
so that release readiness is based on tested provider behavior rather than review-note intent.

### Release-Readiness Job To Preserve

A release owner should be able to inspect Story 11.7 evidence and know which EventStore command/query, SignalR/reconnect, pending-command polling, telemetry, CI, release, and submodule-governance rows are fixed, accepted, split, or blocked without rereading every Epic 1, Epic 3, Epic 5, and Epic 10 review note.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Primary runtime files | Harden `EventStoreCommandClient`, `EventStoreQueryClient`, `ProjectionSubscriptionService`, `SignalRProjectionHubConnectionFactory`, pending-command polling, reconnect reconciliation, telemetry, and badge/count EventStore seams only where Story 11.7 rows require it. |
| Primary CI/release files | Review `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `eng/release_evidence.py`, release inventory, quarantine governance, and package/attestation evidence only where the rows require pass/fail governance. |
| Deferred ledger | Close or explicitly accept the 71 Story 11.7-owned rows in `_bmad-output/implementation-artifacts/deferred-work.md`, especially DW-0248 through DW-0252, DW-0278 through DW-0290, DW-0332 through DW-0350, and DW-0444 through DW-0483. |
| EventStore command/query | Preserve tenant/user fail-closed behavior, bounded request payloads, ETag/304 semantics, typed classification, pact artifacts, cache no-churn behavior, and redacted telemetry. |
| SignalR/reconnect | Prove reconnect event publication, group rejoin, fallback polling, visible-lane registration, disposal/cancellation, and degraded group behavior with deterministic fakes. |
| Pending commands | Land or consciously defer the real `IPendingCommandStatusQuery` provider integration; if landed, cover 202/200 terminal mapping, ETag/304/429/503 parity, retry-after semantics, reconnect epochs, and budget limits. |
| CI/release governance | Decide release ordering, CI/release race, performance advisory isolation, root-level submodule checkout policy, semantic-release credential behavior, and evidence retention without enabling recursive nested submodules. |
| Scope guardrail | Do not absorb broad diagnostic registry governance, MCP schema work, Shell UX polish, SourceTools drift hardening, or EventStore submodule implementation unless explicitly needed to close an 11.7 row. |
| Validation | Start with focused EventStore/Shell tests and CI governance tests; run full solution, pact validation, release evidence, and workflow checks only when touched. |

Start here: T1 inventory Story 11.7 rows -> T2 classify fix/accept/split -> T3 harden EventStore command/query and telemetry -> T4 harden SignalR/reconnect/fallback lanes -> T5 close pending-command provider parity -> T6 reconcile CI/release governance -> T7 update ledger and evidence.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- |
| AC1 | Story 11.7-owned deferred rows exist in `deferred-work.md` | Story 11.7 completes | Each row is marked resolved, superseded, split, accepted, blocked, or non-action with date, owner, rationale, and validation evidence; no row is silently deleted. |
| AC2 | The Story 11.7 bucket currently contains 71 routed rows | The implementer builds the starting inventory | The inventory groups rows by EventStore command/query, SignalR/reconnect, pending command status, badge/count/home, generator/release governance, and CI workflow evidence, preserving row IDs and aliases. |
| AC3 | A row is accepted instead of fixed | The ledger and Dev Agent Record are updated | The acceptance names likelihood, impact, release risk, downstream consumer impact, evidence, owner, and reopen trigger. |
| AC4 | A row belongs to Story 11.2, 11.3, 11.4, 11.5, 11.6, 9.4, 9.5, Product, or Architecture | The story triages it | The row is split or reaffirmed with a named owner and reason; Story 11.7 does not silently implement adjacent governance. |
| AC5 | `EventStoreQueryClient` receives an oversized 200 OK response | Query execution runs | The response is bounded by a `MaxResponseBytes` policy or the accepted constraint states why v1 keeps the current behavior, with redaction proof that raw response bodies and local paths are not logged. |
| AC6 | EventStore command or query responses are non-successful | Runtime and tests exercise the classifier | Operators receive bounded status, failure category, and safe context without leaking command payloads, tenant/user values, bearer tokens, raw ProblemDetails, or unbounded response bodies. |
| AC7 | ETag cache integration handles 200, 304, 429, and 503 outcomes | Query and pending-command tests run | Cache reuse, retry-after, not-modified, backoff, visible-data preservation, and protocol-drift failure behavior are either implemented or explicitly accepted with a row-scoped rationale. |
| AC8 | A cached entry carries an invalid/control-character ETag | Query execution builds validators | The entry is either evicted or explicitly accepted as bounded cache hygiene; it must not be re-sent as `If-None-Match` or leak through telemetry. |
| AC9 | Command dispatch returns a server correlation id | Command response parsing runs | Correlation IDs are sanitized, bounded, and either validated against the agreed ULID/GUID policy or accepted with examples and reopen trigger. |
| AC10 | EventStore query deserialization or schema mismatch fails | Runtime and tests exercise failure paths | Cache invalidation, activity tags, logs, and thrown exceptions preserve useful failure categories without propagating raw payload fragments into public evidence. |
| AC11 | SignalR connection factory wraps `HubConnection` | Tests observe factory behavior | `WithAutomaticReconnect()`, `Reconnecting`, `Reconnected`, `Closed`, initial-start failure, access-token callback behavior, and per-handler isolation have deterministic tests or accepted constraints. |
| AC12 | SignalR access-token refresh is slow or hangs during connect/reconnect | Runtime behavior is reviewed | The `CancellationToken.None` limitation is mitigated with service-level timeout/wrapper behavior or accepted as an ASP.NET SignalR API constraint with a concrete owner. |
| AC13 | Duplicate subscribe/unsubscribe, reconnect, disposal, and rejoin overlap | Race-staged tests run | Active group state cannot resurrect removed groups, skip disposal cancellation, flood rejoin attempts, or invoke callbacks after disposal without bounded evidence. |
| AC14 | A projection nudge arrives while disconnected or degraded | Fallback paths run | Visible lanes are registered for DataGrid and badge/count consumers or the missing callsites are split with explicit generator/runtime owners and proof that existing query paths remain correct. |
| AC15 | Reconnection sweep markers are inserted | Long-lived circuit tests or analysis run | Marker growth is bounded by scheduled clear behavior, cap, or accepted constraint; stale markers cannot leak indefinitely in normal runtime paths. |
| AC16 | `LoadPageFailedAction` follows a schema-mismatch failure | Reducer/effect tests run | Any pending page TCS is resolved or failed deterministically; callers cannot hang waiting for a terminal action. |
| AC17 | Pending-command polling runs with a real provider | Provider-backed tests run | `IPendingCommandStatusQuery` integrates with the EventStore status endpoint or remains explicitly null-provider-only with a named owner; no ambiguous "real provider later" text remains. |
| AC18 | Pending-command provider returns 202, 200 terminal, 304, 429, 503, malformed, duplicate, or stale terminal responses | Polling tests run | Resolver state, ETag use, retry-after/backoff, budget counters, duplicate handling, and failure logs are deterministic and redacted. |
| AC19 | Pending commands survive reconnect epochs | Reconnect and polling tests run | Stale terminal observations from an older reconnect epoch cannot override newer state, or the story records why the current resolver contract accepts the edge. |
| AC20 | Pending-command state hits cap, eviction, disposal, or explicit-empty UI cases | Component/state tests run | User-facing lifecycle output distinguishes expected empty state from fallback snapshot and records cap-eviction semantics if contract changes are deferred. |
| AC21 | Badge/count and home discovery rows remain in Story 11.7 | Triage completes | Rows DW-0278 through DW-0290 are fixed, accepted as low-risk runtime constraints, or split to Shell UX/diagnostic owners; EventStore notifier hot-path risks are not ignored. |
| AC22 | Source generator/release-governance rows remain in Story 11.7 | Triage completes | DisplayLabel, source naming, malformed projection gating, HFC1010/RS2002, and NFR10 rows are either split to Story 11.4/11.2/11.6 or resolved only if directly needed for release CI evidence. |
| AC23 | CI and release workflows prepare a release candidate | Governance checks run | Release does not publish artifacts before required blocking CI evidence, package inventory, SBOM/checksum/signing/attestation fallback, and NuGet/GitHub release ordering decisions are recorded. |
| AC24 | Root-level submodules are needed for CI | Workflow review completes | Workflows use root-level submodule checkout only; no recursive nested submodule initialization or update is introduced unless a human explicitly approves it. |
| AC25 | Performance, palette, nightly, visual, and quarantined lanes are advisory | CI governance tests or workflow review run | Advisory lanes are isolated from blocking functional failures, and artifacts clearly state warning-only status, duration budgets, and pass/fail evidence. |
| AC26 | Release credential and semantic-release push behavior is reviewed | Release dry-run or static workflow validation runs | `persist-credentials`, token-injected remote, NuGet push ordering, changelog/tag push, and GitHub Release failure modes have an accepted or fixed path. |
| AC27 | Validation completes | Story 11.7 moves to review | The Dev Agent Record lists commands, outcomes, touched files, unresolved accepted constraints, split rows, residual release risks, and evidence paths. |

---

## Tasks / Subtasks

- [ ] T1. Inventory and classify Story 11.7 deferred rows (AC1-AC4, AC27)
  - [ ] Read `_bmad-output/implementation-artifacts/deferred-work.md` from top to bottom before code changes.
  - [ ] Capture all rows with `Owner: Story 11.7`, preserving canonical row IDs, aliases, related stories, evidence paths, and source review labels.
  - [ ] Group rows into EventStore command/query, SignalR/reconnect/fallback, pending command status, badge/count/home, generator/release governance, and CI/release workflow buckets.
  - [ ] Create a row-to-evidence matrix naming the intended outcome for each row: fix now, accept, split, supersede, non-action, or block.
  - [ ] For every accepted row, record likelihood, impact, release risk, downstream consumer impact, owner, reopen trigger, and validation evidence.
  - [ ] Preserve historical review text; append resolution notes rather than rewriting or deleting old rows.

- [ ] T2. Harden EventStore command/query response behavior and redacted telemetry (AC5-AC10)
  - [ ] Revisit `EventStoreQueryClient` response-size behavior and either add a bounded `MaxResponseBytes` option with validator/default/docs/tests or record why v1 accepts the current `ReadAsStringAsync`/`JsonDocument` path.
  - [ ] Re-run classifier tests for command and query non-success responses; make sure failure categories remain typed and no raw response body, ProblemDetails, token, tenant/user value, or local path is logged.
  - [ ] Decide whether invalid cached ETags should be evicted when detected; add cache hygiene tests if fixed.
  - [ ] Check `EventStoreCommandClient.ReadCorrelationIdAsync` and `FrontComposerTelemetry.SafeIdentifierOrAbsent` for strict ULID/GUID behavior, truncation markers, and malformed correlation evidence.
  - [ ] Confirm schema mismatch invalidates affected projection cache entries and preserves original cause category without leaking payload fragments.
  - [ ] Tighten exact-deny-list tests for Contracts infrastructure references if this story keeps DW-0250 instead of splitting to Story 11.4.

- [ ] T3. Harden SignalR connection, reconnect, and fallback behavior (AC11-AC16)
  - [ ] Add or extend `SignalRProjectionHubConnectionFactory` tests for `WithAutomaticReconnect()`, state event publication, initial-start failure, per-handler isolation, and access-token callback observation.
  - [ ] Decide the cancellation strategy for `AccessTokenProvider` in SignalR's tokenless callback API; document accepted framework limitation if no code change is safe.
  - [ ] Add deterministic race-staged tests for duplicate subscribe/unsubscribe during reconnect, dispose-suppresses-callbacks, rejoin cancellation, blocked/degraded group recovery, and removed group non-resurrection.
  - [ ] Wire or split visible-lane `RegisterLane`/`UnregisterLane` callsites for generated DataGrid view hosts and `BadgeCountService`.
  - [ ] Verify fallback polling preserves visible data on 429/503 and clears reconciliation state on reconnect.
  - [ ] Audit `ReconciliationSweepState` and coordinator clear scheduling; add cap/clear tests or accepted constraint evidence.
  - [ ] Verify `LoadPageFailedAction` schema-mismatch path resolves or fails pending TCS entries.

- [ ] T4. Close pending-command status provider parity (AC17-AC20)
  - [ ] Decide whether Story 11.7 implements the real EventStore-backed `IPendingCommandStatusQuery` or records an explicit release-blocking split.
  - [ ] If implemented, map EventStore status endpoint responses into `PendingCommandOutcomeObservation` without exposing raw payloads.
  - [ ] Cover 202 pending, 200 terminal, 304 not modified, 429/503 retry-after, malformed body, duplicate terminal, stale terminal, and provider exception cases.
  - [ ] Validate `MaxPendingCommandPollingPerTick`, processed counters, null-provider short-circuit behavior, and burst/live-nudge coalescing.
  - [ ] Revisit reconnect-epoch awareness in `PendingCommandOutcomeResolver` and stale terminal observations.
  - [ ] Review pending-command UI/component semantics for explicit empty state, disposed reads, cap eviction, and long-running Confirming escalation; split UX contract changes if needed.

- [ ] T5. Triage badge/count, home, and generator-adjacent release rows (AC21-AC22)
  - [ ] Reconcile DW-0278 through DW-0290 against current Shell behavior, Story 3.6 scope-flip fixes, Story 11.6 UX ownership, and Story 9.4 diagnostic governance.
  - [ ] Decide whether `_unresolvedTypes` needs a cap once SignalR notifiers can surface adversarial type names; if fixed, use a bounded cache policy per L14.
  - [ ] Confirm `BadgeCountService` hot-path type lookups, duplicate projection FQNs, DOM/test-id generation, and disposal races are either fixed or accepted.
  - [ ] Split DisplayLabel, namespace-collision source naming, malformed projection gating, HFC1010/RS2002, and XML-doc escaping rows to Story 11.4/11.2/11.6 unless they directly block release CI governance.
  - [ ] Re-measure or reclassify the NFR10 hot-reload latency deviation with non-harness evidence, or record the accepted release constraint.

- [ ] T6. Reconcile CI, release, submodule, and governance workflow risk (AC23-AC26)
  - [ ] Review `.github/workflows/ci.yml` for blocking default lane, advisory performance/palette/quarantine lanes, fail-fast "tests actually ran" guard, duration evidence, and root-level submodule checkout only.
  - [ ] Review `.github/workflows/release.yml` for CI-before-release dependency, semantic-release/NuGet/GitHub Release ordering, attestation fallback gate, release budget evidence, and credential push behavior.
  - [ ] Decide whether release should move from direct `push` to `workflow_run` after successful CI or keep direct push with explicit accepted risk.
  - [ ] Preserve root-level submodule behavior (`submodules: true`) and do not introduce recursive nested submodule checkout/update.
  - [ ] Validate release package inventory, SBOM/checksum/signing evidence, NuGet credentials, and GitHub Release failure behavior if workflow changes are made.
  - [ ] Keep artifact output bounded and redacted: no secrets, tokens, local absolute paths, tenant/user values, raw logs, or unbounded workflow dumps.

- [ ] T7. Validate, reconcile, and record evidence (AC1, AC23-AC27)
  - [ ] Run focused EventStore/Shell tests first:
    `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~EventStore|FullyQualifiedName~ProjectionSubscription|FullyQualifiedName~PendingCommand|FullyQualifiedName~ReconnectionReconciliation|FullyQualifiedName~ProjectionConnection|FullyQualifiedName~BadgeCount"`
  - [ ] Run contract pact tests if command/query provider behavior changes:
    `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "Category=Contract"`
  - [ ] Run governance tests if CI/release/quarantine evidence changes:
    `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category=Governance"`
  - [ ] Run full main lane when runtime or workflow risk is broad:
    `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`
  - [ ] Run release evidence helpers when release inventory or workflow evidence changes:
    `python eng/release_evidence.py inventory --root . --expected eng/release-package-inventory.json --output artifacts/release/package-inventory-preflight.json`
  - [ ] Run a bounded forbidden-token/redaction scan across updated logs, docs, snapshots, workflow summaries, release evidence, and ledger evidence.
  - [ ] Update `_bmad-output/implementation-artifacts/deferred-work.md` with row-scoped resolution evidence.
  - [ ] Update this story's Dev Agent Record with commands, outcomes, file list, accepted constraints, split rows, and residual release risks.

---

## Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Story 11.7 owns EventStore runtime reliability plus CI/release governance evidence, not broad SourceTools, diagnostic registry, MCP, or Shell UX policy. | Keeps Epic 11 stories independently implementable and prevents bucket bleed. |
| D2 | The deferred ledger is the source of truth for this story's scope. | The story exists to close scattered review notes; silent row loss is a release-readiness failure. |
| D3 | Tenant/user fail-closed behavior remains non-negotiable for command, query, SignalR, cache, pending-command, and telemetry paths. | Applies L03 and protects the highest-risk boundary in the EventStore integration. |
| D4 | SignalR projection nudges are never source-of-truth data; every nudge must re-query REST with tenant/user/cache context. | Matches project context and prevents stale or cross-tenant projection state. |
| D5 | EventStore response hardening must preserve typed classifier contracts and existing pact artifacts. | Operators need better evidence without breaking generated UI consumers. |
| D6 | Pending-command provider parity is a release decision, not a vague follow-up. | The null provider is safe but cannot prove provider-backed command status behavior. |
| D7 | Runtime caches and transient marker stores must be bounded by policy or explicitly accepted with evidence. | Applies L14 and avoids long-lived circuit memory leaks. |
| D8 | CI/release workflow changes must preserve root-level submodule checkout and avoid recursive nested submodule updates. | Matches repository and AGENTS.md submodule policy. |
| D9 | Release publication requires evidence before irreversible external side effects. | NuGet publishing, GitHub Release creation, attestations, and tags need coherent failure semantics. |
| D10 | Accepted constraints must include likelihood, impact, downstream consumer impact, owner, evidence, and reopen trigger. | "Low priority" closure is not sufficient for release readiness. |
| D11 | Advisory lanes must be isolated from blocking functional lanes. | Performance, visual, palette, nightly, and quarantined instability must not hide functional CI failure. |
| D12 | Public evidence and telemetry must be redacted by construction. | Raw payloads, tenants, users, tokens, response bodies, local paths, and full logs cannot leak into artifacts. |

### Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs` | Update likely | Response-size policy, ETag cache hygiene, 304/429/503 parity, schema mismatch, redacted telemetry. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs` | Update possible | Correlation parsing, non-success response context, redaction, provider status integration. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs` | Update possible | Typed classification for command/query/pending status response parity. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs` | Update likely | Factory wrapper tests, automatic reconnect behavior, access-token callback limitation. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` | Update likely | Reconnect race coverage, active group state, disposal, pending polling, degraded groups. |
| `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/**` | Update likely | Sweep marker cap/clear behavior, coordinator callbacks, visible-lane reconciliation. |
| `src/Hexalith.FrontComposer.Shell/State/PendingCommands/**` | Update likely | Real status query provider, ETag/retry semantics, stale terminal, cap/eviction behavior. |
| `src/Hexalith.FrontComposer.Shell/Components/EventStore/**` | Update possible | Pending command summary, projection connection status, explicit empty and UX edge cases. |
| `src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs` | Update possible | EventStore notifier hot path, bounded unresolved type cache, duplicate FQN handling. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/**` | Update possible | Sanitized identifiers, truncation markers, failure category consistency. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/**` | Update likely | Command/query/classifier, SignalR factory, subscription fault injection, pact evidence. |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/**` | Update likely | Provider-backed polling, retry-after, stale/duplicate terminal, caps. |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/ReconnectionReconciliation/**` | Update likely | Sweep markers, coordinator, visible-lane reconciliation behavior. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/EventStore/**` | Update possible | Connection status and pending command summary user-facing behavior. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Badges/**` | Update possible | Badge count hot-path and duplicate projection FQN evidence. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Pact/**` | Update possible | EventStore REST consumer contract updates if response/provider behavior changes. |
| `.github/workflows/ci.yml` | Update possible | Advisory lane isolation, blocking evidence, root-level submodule checkout. |
| `.github/workflows/release.yml` | Update possible | CI-before-release dependency, credential push behavior, irreversible release ordering. |
| `eng/release_evidence.py` and `eng/release-package-inventory.json` | Update possible | Release inventory, budget, manifest, and evidence checks. |
| `tests/ci-governance/**` | Update possible | Workflow/evidence fixture tests for CI and release governance. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update | Mark Story 11.7 rows resolved/accepted/split after implementation. |
| `_bmad-output/implementation-artifacts/11-7-eventstore-reliability-and-ci-governance-follow-ups.md` | Update | Dev Agent Record, validation, file list, completion notes. |

### Project Structure Notes

- EventStore integration code lives under `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore`; do not move it into the `Hexalith.EventStore` submodule.
- Contracts under `src/Hexalith.FrontComposer.Contracts/Communication` are high blast-radius and multi-targeted; any change needs contract tests and downstream pact review.
- SignalR and REST behavior are separate channels: REST is source-of-truth for commands/queries; SignalR only nudges clients to refresh.
- SourceTools changes are only in scope when visible-lane registration or generated DataGrid callsites require them. Keep SourceTools `netstandard2.0` and Shell dependencies separate.
- CI workflow checkout may use root-level `submodules: true`; do not introduce recursive nested submodule initialization.
- Release artifacts must remain bounded and redacted. Never write raw logs, raw response bodies, tokens, local absolute paths, tenant/user values, or unbounded JSON into committed evidence.

### Testing Strategy

- Run focused EventStore/Shell tests first:
  - `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~EventStore|FullyQualifiedName~ProjectionSubscription|FullyQualifiedName~PendingCommand|FullyQualifiedName~ReconnectionReconciliation|FullyQualifiedName~ProjectionConnection|FullyQualifiedName~BadgeCount"`
- Run pact contract tests when command/query contracts change:
  - `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "Category=Contract"`
- Run governance tests when CI/release/quarantine evidence changes:
  - `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category=Governance"`
- Run the main lane before review when runtime changes cross boundaries:
  - `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`
- Run release evidence helpers if release files change:
  - `python eng/release_evidence.py inventory --root . --expected eng/release-package-inventory.json --output artifacts/release/package-inventory-preflight.json`
- Run a redaction scan over updated evidence surfaces before review.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 5.1 | Story 11.7 | EventStore service abstractions, infrastructure reference boundaries, SignalR factory seams, and response parity rows are closed or accepted. |
| Story 5.2 | Story 11.7 | Query response classification, ETag/304 behavior, cache no-churn semantics, and response-size risks remain coherent. |
| Story 5.3 | Story 11.7 | SignalR reconnect, state event publication, group rejoin, fallback polling, and connection-status UX evidence are hardened. |
| Story 5.4 | Story 11.7 | Reconnection reconciliation, visible-lane registration, sweep markers, and schema-mismatch fallback have runtime evidence. |
| Story 5.5 | Story 11.7 | Pending-command polling, provider status query, duplicate/stale terminal handling, cap behavior, and pending summary UX are closed or accepted. |
| Story 5.6 | Story 11.7 | Telemetry, observability, redaction, infrastructure governance, and response-size policy remain aligned. |
| Story 5.7 | Story 11.7 | Fault-injection harness coverage is reused or extended for reconnect and SignalR race scenarios. |
| Story 10.5 | Story 11.7 | Quarantine and CI governance evidence remain warning-only or blocking according to documented lane policy. |
| Story 10.6 | Story 11.7 | Release package inventory, SBOM, signing, attestation fallback, and release budget evidence remain synchronized. |
| Story 11.1 | Story 11.7 | Routed deferred rows must close with evidence or accepted constraints. |
| Story 11.2 | Story 11.7 | Diagnostic registry and release-row governance remain separate unless CI evidence directly depends on them. |
| Story 11.4 | Story 11.7 | Broad SourceTools drift/generator hardening remains separate; visible-lane generated callsites are in scope only when required for EventStore fallback. |
| Story 11.5 | Story 11.7 | MCP/schema and release-package contract constraints remain separate, with CI/release handoffs named explicitly. |
| Story 11.6 | Story 11.7 | Shell UX/accessibility/sample polish remains separate; connection-status and pending-command components are in scope only for EventStore reliability. |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Diagnostic registry schema, HFC release tracking, and docs stub governance. | Story 11.2 |
| CLI/IDE migration and help/reference semantics. | Story 11.3 |
| Broad SourceTools drift/generator diagnostics and snapshot governance. | Story 11.4 |
| MCP schema negotiation, agent categories, and schema fingerprint material. | Story 11.5 |
| Shell UX, accessibility/specimen, localization/RTL, and Counter sample polish. | Story 11.6 |
| Diataxis adopter docs for CI/release and package-consumption guidance. | Story 9.5 |
| Product/UX decisions for long-running Confirming escalation and broader compliance audit logging. | Product/Architecture roadmap after Story 11.7 evidence |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-11-deferred-hardening-release-readiness.md#Story-11.7`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md`] - Story 11.7 routed row bucket and row-level evidence.
- [Source: `_bmad-output/implementation-artifacts/5-1-eventstore-service-abstractions.md`] - EventStore abstraction and factory baseline.
- [Source: `_bmad-output/implementation-artifacts/5-2-http-response-handling-and-etag-caching.md`] - response classification, ETag, cache, and HTTP parity baseline.
- [Source: `_bmad-output/implementation-artifacts/5-3-signalr-connection-and-disconnection-handling.md`] - SignalR connection state and reconnect baseline.
- [Source: `_bmad-output/implementation-artifacts/5-4-reconnection-reconciliation-and-batched-updates.md`] - reconnect reconciliation and fallback polling baseline.
- [Source: `_bmad-output/implementation-artifacts/5-5-command-idempotency-and-optimistic-updates.md`] - pending-command state, polling, and optimistic UI baseline.
- [Source: `_bmad-output/implementation-artifacts/5-6-build-time-validation-error-boundaries-and-diagnostics.md`] - infrastructure governance and observability baseline.
- [Source: `_bmad-output/implementation-artifacts/5-7-signalr-fault-injection-test-harness.md`] - fault-injection harness baseline.
- [Source: `_bmad-output/implementation-artifacts/10-5-flaky-test-quarantine-and-ci-governance.md`] - quarantine and CI lane governance.
- [Source: `_bmad-output/implementation-artifacts/10-6-llm-benchmark-signed-releases-and-sbom.md`] - release evidence, SBOM, signing, and attestation governance.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`] - current EventStore query and ETag cache implementation.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs`] - current EventStore command dispatch implementation.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`] - current SignalR subscription, group, reconnect, and pending polling implementation.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs`] - current SignalR hub connection wrapper.
- [Source: `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs`] - current null-provider and polling coordinator seam.
- [Source: `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconciliationSweepState.cs`] - current sweep marker state and reducers.
- [Source: `.github/workflows/ci.yml`] - current CI gates, advisory lanes, root-level submodule checkout, and evidence uploads.
- [Source: `.github/workflows/release.yml`] - current release workflow, semantic-release, package inventory, attestation fallback, and credential behavior.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L03--Tenantuser-isolation-guards-fail-closed`] - tenant/user fail-closed guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06--Defense-in-depth-budget-per-story`] - scope and decision budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L07--Test-count-inflation-is-a-cost`] - test-scope budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08--Party-review-vs-elicitation--different-roles`] - this story should receive later party review and elicitation hardening.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10--Deferrals-need-story-specificity-not-epic-specificity`] - owner specificity requirement.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L14--Bounded-by-policy-beats-documented-unbounded-for-any-in-memory-cache`] - runtime cache/marker bound guidance.
- [Source: `_bmad-output/project-context.md`] - project rules for EventStore channels, tenancy, tests, redaction, release work, and submodules.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

- 2026-05-11: Story created via `/bmad-create-story 11-7-eventstore-reliability-and-ci-governance-follow-ups` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### Change Log

- 2026-05-11: Created Story 11.7 and marked ready-for-dev.

### File List

- `_bmad-output/implementation-artifacts/11-7-eventstore-reliability-and-ci-governance-follow-ups.md`
