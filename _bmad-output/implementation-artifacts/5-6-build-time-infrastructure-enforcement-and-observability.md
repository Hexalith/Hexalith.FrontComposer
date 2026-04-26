# Story 5.6: Build-Time Infrastructure Enforcement & Observability

Status: ready-for-dev

> **Epic 5** - Reliable Real-Time Experience. **FR48 / FR72** build-time portability enforcement, structured logging, and distributed trace continuity after Stories 5-1 through 5-5. Applies lessons **L01**, **L03**, **L06**, **L07**, **L08**, **L10**, and **L14**.

---

## Executive Summary

Story 5-6 adds governance and telemetry around the real-time stack without changing the user-facing resilience behavior already owned by Stories 5-3 through 5-5:

- Enforce that FrontComposer framework assemblies do not take direct dependencies on infrastructure provider SDKs or storage/broker clients. Redis, Kafka, PostgreSQL, Cosmos DB, Dapr SDK, and provider-specific packages stay out of Contracts, SourceTools, Shell, tests that validate framework package closure, and generated framework code.
- Keep EventStore as the boundary. FrontComposer talks to EventStore through the Story 5-1 command/query/subscription contracts and HTTP/SignalR clients. Dapr component bindings, Redis/Postgres/Kafka/Cosmos provider swaps, Kubernetes, Azure Container Apps, ECS/EKS, and Cloud Run topology remain deployment/EventStore/AppHost concerns.
- Replace fragile substring-only reference checks with a deterministic governance test suite that scans project references, package references, assembly references, generated output, and selected source namespaces with an explicit allowlist.
- Add the Shell-side `ActivitySource` instance using `Hexalith.FrontComposer.Contracts.Telemetry.FrontComposerActivitySource.Name` and `.Version`. Do not introduce exporter dependencies into framework packages; adopters and sample hosts wire OpenTelemetry exporters.
- Standardize runtime log shape for EventStore command dispatch, query execution, projection connection transitions, rejoin failures, fallback polling, lifecycle transitions, and pending command outcomes.
- Preserve the existing redaction policy. Logs and spans carry framework-controlled command/projection type names, correlation/message IDs, bounded failure categories, and a redacted tenant marker. They must not carry raw tokens, user IDs, raw tenant values when policy marks them sensitive, command payloads, query payloads, cache payloads, or ProblemDetails bodies.
- Add CI gates for governance and telemetry regression tests. This story should harden the relevant `build-and-test` gate so these checks block instead of only reporting advisory failures.

The intended implementation shape is a small governance test layer plus a Shell telemetry helper. Runtime code uses `ActivitySource`, `Activity.Current`, `ILogger` message templates/source-generated logging helpers, and DI-registered telemetry options. Deployment exporters stay outside the framework.

---

## Story

As a developer,
I want build-time guarantees that framework code does not directly couple to infrastructure providers, and structured logging across the full lifecycle,
so that the framework remains portable across deployment targets and I can trace any operation end-to-end.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | Framework assemblies, project files, generated outputs, and source files are inspected in CI | A forbidden direct dependency or namespace appears in FrontComposer framework code | The governance check fails the build with a descriptive message naming the project/file/reference, the forbidden provider family, and the remediation path through EventStore or deployment/AppHost Dapr components. |
| AC2 | The governance deny-list runs | Redis, Kafka, PostgreSQL/Npgsql, Cosmos DB, Dapr SDK, StackExchange.Redis, Confluent.Kafka, provider-specific Azure/AWS/GCP infrastructure SDKs, or direct database clients are referenced from `Hexalith.FrontComposer.Contracts`, `Hexalith.FrontComposer.SourceTools`, `Hexalith.FrontComposer.Shell`, or generated framework code | The violation is blocked unless the reference is covered by an explicit story-owned allowlist entry. SignalR client remains allowed because Story 5-1/5-3 made it the EventStore projection nudge transport. |
| AC3 | Infrastructure topology lives in samples, AppHosts, EventStore, or deployment docs | The governance checks scan the repository | AppHost, sample deployment files, EventStore submodule Dapr components, generated bin/obj artifacts, and documentation examples are excluded or separately classified so framework checks do not false-fail on legitimate topology code. |
| AC4 | Runtime services emit logs | Logs are captured from command dispatch, query execution, lifecycle transitions, projection connection state, rejoin failures, nudge refresh, fallback polling, and pending-command outcome resolution | Every log uses message template plus parameters, stable event IDs or HFC runtime diagnostic constants, and structured fields for `CommandType` or `ProjectionType`, redacted tenant marker, `CorrelationId`/`MessageId` when available, `Outcome`, `ElapsedMs`, `FailureCategory`, and `Transport` where applicable. No string interpolation is used in `ILogger.Log*` calls. |
| AC5 | Sensitive data redaction tests run | Logs or span tags are inspected after success and failure paths | No bearer token, raw access token, command/query/cache payload, form field value, raw ProblemDetails body, raw exception message that can echo payload data, user ID, or raw tenant ID is emitted. Failure categories are bounded and sanitized. |
| AC6 | `FrontComposerActivitySource` is used | Command submit, HTTP command dispatch, HTTP query, cache hit/miss/not-modified, projection nudge, fallback polling, lifecycle terminal transition, and UI refresh paths execute | Activities share the source name `Hexalith.FrontComposer`, include consistent operation names and tags, nest under `Activity.Current` when present, and expose enough correlation to trace user click -> backend command -> projection update -> SignalR nudge -> UI update. |
| AC7 | An adopter wires OpenTelemetry exporters in a host app | Traces and logs are exported to Grafana/OTLP, Jaeger/OTLP, or Application Insights | Framework activities and logs flow without package-specific exporter references in Contracts/SourceTools/Shell. Exporter packages belong to the host/sample, not the framework assemblies. |
| AC8 | The CI workflow runs on push or PR | Governance and telemetry tests fail | The build-and-test job fails, uploads test results, and surfaces the exact failing governance/telemetry test. Existing advisory `continue-on-error` behavior is removed or narrowed so this story's gates are blocking. |
| AC9 | Tests run | Governance, telemetry helper, EventStore clients, connection state, fallback polling, lifecycle wrapper, and generated forms execute | Coverage proves deny-list detection, allowlist precision, bin/obj/doc exclusion, descriptive failure messages, LoggerMessage/message-template usage, redaction, ActivitySource names/tags/parenting, no exporter dependency, and CI gate wiring. |

---

## Tasks / Subtasks

- [ ] T1. Build the framework infrastructure-coupling governance suite (AC1, AC2, AC3, AC9)
  - [ ] Replace the narrow `ContractsAssembly_DoesNotReferenceInfrastructurePackages` substring test with explicit deny-list tests that inspect `ProjectReference`, `PackageReference`, restored assembly references, and source `using`/fully-qualified namespace usage.
  - [ ] Deny at least: `Dapr.*`, `StackExchange.Redis`, `Microsoft.AspNetCore.SignalR.StackExchangeRedis`, `Confluent.Kafka`, `Npgsql`, `Microsoft.Azure.Cosmos`, direct provider database/storage/event-bus SDKs, and namespace roots matching provider clients.
  - [ ] Keep `Microsoft.AspNetCore.SignalR.Client` allowed only in Shell because it is the EventStore nudge transport from Stories 5-1 and 5-3.
  - [ ] Exclude generated `bin/`, `obj/`, `.bmad`, `.agents`, `.github/skills`, planning docs, and submodules from framework source scanning unless a test explicitly validates docs/sample topology.
  - [ ] Emit failure messages that identify path, reference, provider family, and expected remediation: "route through EventStore contract/client or deployment/AppHost component configuration".
  - [ ] Add exact-match allowlist entries with owner/story comments. Avoid substring matches like `Hosting` or `EventStore` that can false-positive on benign assembly names.

- [ ] T2. Harden package and assembly reference validation (AC1, AC2, AC3, AC9)
  - [ ] Parse project XML using `System.Xml.Linq`; do not use ad hoc string matching for `.csproj` files.
  - [ ] Inspect `Directory.Packages.props` centrally and all framework `.csproj` files for forbidden package IDs.
  - [ ] Add a runtime assembly reference test for each produced framework assembly: Contracts, SourceTools, and Shell. Shell may reference SignalR client; Contracts and SourceTools may not.
  - [ ] Validate source-generator output baselines and generated command/projection artifacts do not contain forbidden namespace roots.
  - [ ] Keep EventStore submodule Dapr/provider dependencies outside the FrontComposer framework enforcement scope.

- [ ] T3. Add Shell telemetry infrastructure without exporter dependencies (AC6, AC7, AC9)
  - [ ] Create a Shell telemetry helper such as `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerTelemetry.cs`.
  - [ ] Instantiate `ActivitySource` using `Hexalith.FrontComposer.Contracts.Telemetry.FrontComposerActivitySource.Name` and `.Version`; keep the constants in Contracts.
  - [ ] Add small methods for starting activities with consistent operation names and tags, for example `frontcomposer.command.dispatch`, `frontcomposer.query.execute`, `frontcomposer.projection.nudge`, `frontcomposer.projection.fallback_poll`, and `frontcomposer.lifecycle.transition`.
  - [ ] Do not add OpenTelemetry exporter packages to Contracts, SourceTools, or Shell. Host/sample code may demonstrate exporter wiring.
  - [ ] Dispose only if a process-lifetime owner is introduced; static framework `ActivitySource` should not be disposed per scoped service lifetime.

- [ ] T4. Instrument EventStore command/query paths (AC4, AC5, AC6, AC9)
  - [ ] Wrap `EventStoreCommandClient.DispatchAsync` in an activity that records sanitized command type, message ID, correlation ID, HTTP outcome/status code, elapsed duration, and failure category.
  - [ ] Wrap `EventStoreQueryClient.QueryAsync`/`ExecuteAsync` in an activity that records sanitized projection/query type, cache discriminator classification, ETag outcome (`hit`, `miss`, `not_modified`, `protocol_drift_retry`), status code, elapsed duration, and failure category.
  - [ ] Keep request/response payloads, ETags if considered sensitive, token values, raw tenant/user IDs, and ProblemDetails bodies out of log fields and span tags.
  - [ ] Add a response-size governance note for DF2: either land `MaxResponseBytes` here or explicitly create a Story 9-4-owned deferred row with blocking rationale before closing 5-6. If landed, instrument response-size rejection as a bounded failure category.
  - [ ] Preserve Story 5-2 no-churn semantics for `304`: telemetry records no-change, but reducers and UI state must not mutate.

- [ ] T5. Instrument projection connection, rejoin, and fallback polling (AC4, AC5, AC6, AC9)
  - [ ] Update `ProjectionConnectionStateService`, `SignalRProjectionHubConnectionFactory`, `ProjectionSubscriptionService`, `ProjectionFallbackRefreshScheduler`, and `ProjectionFallbackPollingDriver` to log through shared structured helpers or source-generated LoggerMessage methods.
  - [ ] Add activities for connection state transitions, reconnect/rejoin sweep, nudge refresh, and fallback polling iteration.
  - [ ] Add a lightweight rate-limiting or sampling policy for flapping connection logs, resolving deferred item W1 from Story 5-3 review. The policy must use `TimeProvider`-anchored windows/buckets, must not suppress state transitions from metrics/traces, and must keep terminal failure/recovered transitions visible.
  - [ ] Ensure failed rejoin logs include `ProjectionType` and redacted tenant marker only if policy permits; never log raw group names or SignalR exception messages.
  - [ ] Keep fallback polling behavior unchanged: no extra polling loop, no visible-lane registry duplication, and stop promptly on reconnect/disposal.

- [ ] T6. Standardize lifecycle and pending-command logs/spans (AC4, AC5, AC6, AC9)
  - [ ] Instrument `LifecycleStateService`, `FcLifecycleWrapper`, and the Story 5-5 pending-command resolver/summary seams if present.
  - [ ] Include `CommandType`, `CorrelationId`, `MessageId`, terminal state, idempotency flag, elapsed threshold bucket, and failure category where available.
  - [ ] Keep user-facing copy and lifecycle behavior unchanged. Telemetry must observe outcomes, not trigger duplicate lifecycle transitions or UI notifications.
  - [ ] If Story 5-5 has not yet implemented pending-command services, document the expected instrumentation seam and add tests around existing lifecycle service only.

- [ ] T7. Add structured logging helpers and regression tests (AC4, AC5, AC9)
  - [ ] Prefer `[LoggerMessage]` source-generated partial methods for high-frequency EventStore/projection/lifecycle logs. For low-frequency code, message-template `logger.Log*("...", arg)` is acceptable.
  - [ ] Centralize telemetry operation-name and tag-key constants in the Shell telemetry helper; tests must prove call sites use the centralized source name/version and approved tag set.
  - [ ] Add a source scanner test that fails on `logger.Log*( $"...")`, interpolated message templates, string concatenated templates, raw `ex.Message` template arguments on EventStore/projection paths, and direct logging of payload variables.
  - [ ] Add redaction tests extending `EventStoreDiagnosticsTests` to cover token acquisition failures, bad JSON, query failure, rejoin failure, fallback polling failure, lifecycle failure, and telemetry span tags.
  - [ ] Use a capturing `ILogger` and `ActivityListener` test harness. Do not require live OpenTelemetry collectors.

- [ ] T8. Demonstrate host exporter compatibility without framework coupling (AC7, AC9)
  - [ ] Add or update a sample/host-only snippet that registers OpenTelemetry tracing/logging against `Hexalith.FrontComposer` ActivitySource. Keep exporter packages in the sample host if a concrete exporter is demonstrated.
  - [ ] Cover OTLP compatibility for Grafana/Tempo or Jaeger and Application Insights compatibility by standard OpenTelemetry exporter configuration, not framework-specific runtime dependencies.
  - [ ] If sample host dependencies would bloat this story, create documentation-only guidance and a governance test proving framework packages remain exporter-free.

- [ ] T9. Make CI enforcement blocking (AC8, AC9)
  - [ ] Update `.github/workflows/ci.yml` so the governance/telemetry test lane blocks PR/push. Remove `continue-on-error: true` from `build-and-test` or add a separate blocking governance job.
  - [ ] Add a named step such as `Gate 2b: Infrastructure governance and telemetry contracts` if that keeps output clearer than folding into Gate 3a.
  - [ ] Ensure TRX upload and summary still run on failure.
  - [ ] Keep performance/e2e-palette advisory behavior only if the story explicitly documents why those lanes remain separate from governance.

- [ ] T10. Tests and verification (AC1-AC9)
  - [ ] Governance tests: forbidden package in a synthetic project fails, allowed SignalR Shell reference passes, docs/bin/obj/submodules are excluded, exact allowlist entries work, and failure messages are actionable.
  - [ ] Assembly tests: Contracts and SourceTools have no infrastructure/provider references; Shell has only approved runtime references.
  - [ ] Logging tests: message-template usage, event ID/HFC constant presence, required structured fields, no interpolated templates, no raw exception messages on sensitive paths.
  - [ ] Activity tests: source name/version, operation names, parent/child behavior, key tags, no payload/token/user/raw tenant tags, and no exporter package reference.
  - [ ] CI tests: workflow contains blocking governance execution and does not hide failing governance tests behind job-level `continue-on-error`.
  - [ ] Regression suite: run `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false` and targeted Contracts/Shell/SourceTools tests. Run full solution tests if the working tree is otherwise clean.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Telemetry/FrontComposerActivitySource.cs` | Defines shared telemetry source name `Hexalith.FrontComposer` and version `0.1.0`; no actual `ActivitySource` instance. | Keep Contracts dependency-free. Shell creates the runtime `ActivitySource`; exporters stay host-owned. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs` | Dispatches commands through EventStore HTTP client, validates tenant/user context, uses ULID message ID, logs unexpected status with message template. | Add activities/log standardization without logging payload/token/raw identity data or changing response classification. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs` | Central query path with ETag cache integration, 304 no-change semantics, protocol-drift retry, and redacted warnings. | Preserve no-churn behavior and cache safety; telemetry observes outcomes only. |
| `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionState.cs` | Scoped connection-state service logs every logical transition at Information and redacts failure category to exception type. | Add rate policy and telemetry without breaking subscriber isolation or transition dedupe. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs` | Shell-only SignalR wrapper using `WithAutomaticReconnect()` and state callbacks. | SignalR client package remains allowed in Shell only; do not expose SignalR types through Contracts. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` | Owns active groups, rejoin health, SignalR nudge handling, and fallback driver startup. | Keep active group source of truth and nudge-only contract; telemetry must not add another group registry. |
| `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackPollingDriver.cs` | Runs one bounded fallback loop only while disconnected. | Keep loop ownership unchanged; add span/log per iteration with sampling/rate limits. |
| `tests/Hexalith.FrontComposer.Contracts.Tests/Communication/EventStoreContractTests.cs` | Contains a broad substring reference check for Contracts. | Replace or supplement with exact governance tests; avoid false positives such as `Hosting`/`EventStore` substrings. |
| `.github/workflows/ci.yml` | `build-and-test` is still advisory with `continue-on-error: true`. | Story 5-6 should make governance/telemetry failures blocking. |

### Cross-Story Contract Table

| Seam | Producer | Consumer | Story 5-6 decision |
| --- | --- | --- | --- |
| EventStore command/query/subscription contracts | Story 5-1 | Governance and telemetry | FrontComposer depends on its own contracts and EventStore HTTP/SignalR boundary, not provider SDKs. |
| HTTP response and ETag outcomes | Story 5-2 | Telemetry tags and logs | Record outcomes without changing classifier or cache no-churn behavior. |
| Projection connection state and fallback polling | Story 5-3 | Telemetry/rate limiting | Add observability around existing state, resolving log-flood deferred item W1. |
| Reconnect reconciliation | Story 5-4 | Trace continuity | Reconciliation spans should compose with connection/query spans, not duplicate reconciliation logic. |
| Pending command outcomes | Story 5-5 | Lifecycle/pending telemetry | Instrument exactly-once outcomes if the seam exists; do not implement pending-command behavior here. |
| Diagnostic ID governance | Architecture + Story 9-4 | Runtime log IDs | Add only story-owned HFC/runtime IDs needed now; deeper analyzer/deprecation governance remains Story 9-4. |
| Deployment portability | PRD NFR73/NFR74 | CI governance | Framework package closure must be provider-neutral; deployment/AppHost files may choose providers through Dapr/EventStore topology. |

### Party-Mode Hardening Addendum

Party-mode review on 2026-04-26 tightened three implementation contracts before development:

- Telemetry names and tags are story-owned API. The implementation must centralize operation names and tag keys in the Shell telemetry helper, test them with `ActivityListener`, and avoid ad hoc string literals at call sites except through that helper.
- Connection log rate limiting must be deterministic and testable. It may suppress repeated log records for identical transition/failure buckets, but it must not suppress state changes, terminal lifecycle observations, activity creation, or reducer-visible behavior.
- DF2 `MaxResponseBytes` cannot close as an ambiguous "maybe later" item. If the response-size guard does not land in Story 5-6, the implementation must append a concrete deferred-work row owned by Story 9-4 with the reason and the remaining risk.

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | Governance is test/CI enforced, not a new runtime guard. | Direct infrastructure coupling is a build/package integrity problem. | Detect provider SDKs at startup; rely on code review only. |
| D2 | Deny-list uses exact package/namespace/provider-family rules with an explicit allowlist. | Avoids brittle substring false positives while still blocking provider SDK drift. | Keep `Contains("EventStore")`/`Contains("Hosting")`; block every external package. |
| D3 | SignalR client is allowed in Shell only. | Story 5-1/5-3 made SignalR the EventStore projection nudge transport. | Ban SignalR entirely; expose SignalR types in Contracts. |
| D4 | Dapr SDK is forbidden in FrontComposer framework assemblies for this story. | Dapr component/provider choices belong in EventStore/AppHost/deployment topology; FrontComposer remains portable through EventStore contracts. | Add `DaprClient` directly to Shell; add a custom Dapr wrapper in FrontComposer. |
| D5 | Framework packages do not reference OpenTelemetry exporters. | Exporters are deployment choices; ActivitySource is enough for host integration. | Add Jaeger/ApplicationInsights/Grafana exporter packages to Shell. |
| D6 | ActivitySource name/version remain contract constants. | Hosts need stable names to configure tracing once. | Create package-specific source names; derive name from assembly at runtime. |
| D7 | Runtime identity tags use redacted tenant markers by default. | Existing story review findings forbid raw tenant/user leakage in diagnostics. | Log raw tenant/user values because AC text asks for tenant context. |
| D8 | Message-template logging is mandatory; source-generated LoggerMessage is preferred for hot paths. | Prevents allocation-heavy string interpolation and keeps structured fields queryable. | Accept interpolated strings; build a custom logging abstraction. |
| D9 | Flapping connection logs are rate-limited/sampled but trace state remains observable. | Operators need signal without telemetry flood during reconnect loops. | Log every transition forever; suppress all reconnect logs. |
| D10 | CI governance must block. | A portability guarantee is not real if the build can stay green. | Leave job-level advisory `continue-on-error` for all tests. |
| D11 | Response-size protection is either landed here or explicitly deferred with a named owner. | DF2 can create memory pressure in query telemetry paths. | Ignore DF2 because it is "only observability"; read full response bodies into logs/spans. |
| D12 | Deployment target parity is validated through package neutrality and sample/topology checks, not full cloud deployments in this story. | Local Kubernetes/ACA/ECS/EKS/Cloud Run integration would exceed one story. | Require live cloud deployments in unit CI; skip portability evidence entirely. |

### Library / Framework Requirements

- Target current repo package lines and TFMs: .NET 10, Blazor, Fluxor, Fluent UI Blazor, Microsoft.AspNetCore.SignalR.Client 10.0.6, Roslyn 4.12.0, xUnit v3, bUnit, Shouldly, NSubstitute, Verify.XunitV3, and FakeTimeProvider.
- Use `System.Diagnostics.ActivitySource` and `Activity` from the BCL. Do not add OpenTelemetry exporter packages to framework assemblies.
- Use Microsoft.Extensions.Logging message templates. Prefer `[LoggerMessage]` source generation for high-frequency EventStore/projection/lifecycle logs.
- Use `System.Xml.Linq` or MSBuild/asset-file structured parsing for project/package checks. Avoid regex-only XML parsing.
- Use `ActivityListener` in tests to capture spans without an OpenTelemetry collector.
- Use `TimeProvider` or deterministic fake time for log rate limiting and elapsed duration tests.

External references checked on 2026-04-26:

- Microsoft Learn: High-performance logging in .NET: https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging
- Microsoft Learn: Distributed tracing instrumentation in .NET: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs
- OpenTelemetry .NET documentation: https://opentelemetry.io/docs/languages/dotnet/
- OpenTelemetry semantic conventions: https://opentelemetry.io/docs/specs/semconv/

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `tests/Hexalith.FrontComposer.Contracts.Tests/Architecture/*Infrastructure*Tests.cs` or `tests/Hexalith.FrontComposer.Shell.Tests/Governance/*` | Framework dependency deny-list and assembly/package/source governance tests. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerTelemetry.cs` | Shell-owned `ActivitySource` and activity helper methods. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerLog.cs` or adjacent per-area logging partials | Source-generated logging helpers/event IDs for hot paths. |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` | Add runtime HFC constants only if new log diagnostic IDs are allocated. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs` | Command dispatch activity/log fields. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs` | Query/cache activity/log fields and optional `MaxResponseBytes` response guard. |
| `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/*` | Connection/fallback spans, structured logs, rate limiting. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` | Rejoin/nudge structured logs and spans. |
| `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/*` and `Components/Lifecycle/*` | Lifecycle spans/log fields where existing seams allow. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/*` | ActivitySource, logging, redaction, and rate-limit tests. |
| `.github/workflows/ci.yml` | Blocking governance/telemetry gate. |
| `samples/Counter/Counter.Web` or docs | Optional host-owned OpenTelemetry registration snippet, if included. |

### Testing Standards

- Governance tests should be deterministic and file-system based. Build synthetic project/source snippets under test temp directories when testing deny-list failures.
- Do not require Docker, Dapr, EventStore, SignalR server, Kubernetes, Azure, Jaeger, Grafana, or Application Insights in tests.
- Use `ActivityListener` to assert activity names, approved tag keys, status, parent/child linkage, and redaction.
- Use capturing `ILogger` implementations and inspect structured state, not only formatted strings, when possible.
- Redaction tests must assert absence of token, raw command/query payload values, raw form values, raw tenant/user IDs, raw ProblemDetails body text, and raw exception messages from sensitive EventStore/projection paths.
- CI test should inspect workflow YAML text or parsed YAML to prove governance tests are not hidden behind `continue-on-error`.

### Scope Guardrails

Do not implement these in Story 5-6:

- EventStore command/query/subscription contracts or endpoint defaults - Story 5-1.
- HTTP response classification matrix, validation mapping, auth redirects, or ETag cache semantics - Story 5-2.
- Disconnected banner, form-state preservation, basic fallback polling loop, or connection-state UX - Story 5-3.
- Reconnect reconciliation sweep, schema mismatch UX, or changed-lane animation - Story 5-4.
- Pending command resolver, optimistic badges, new item indicator, or command outcome summary - Story 5-5.
- SignalR fault injection test harness package - Story 5-7.
- Full diagnostic ID/deprecation governance analyzers, code fixes, or migration tooling - Story 9-4.
- Live cloud deployment validation for Azure Container Apps, AWS, GCP, or Kubernetes. This story validates framework neutrality and sample wiring only.
- Adding Dapr, Redis, Kafka, PostgreSQL, Cosmos DB, Jaeger, Grafana, or Application Insights exporter packages to framework projects.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Reusable SignalR fault-injection package and live fault scenarios. | Story 5-7 |
| Full diagnostic ID catalog governance, deprecation analyzer, and code-fix migration path. | Story 9-4 |
| Provider/Pact verification of EventStore HTTP/SignalR behavior and trace propagation. | Story 10-3 |
| Live cloud deployment smoke tests for Kubernetes, Azure Container Apps, ECS/EKS, and Cloud Run. | Story 10-2 or deployment validation story |
| OpenTelemetry exporter recipe docs in Diataxis documentation site. | Story 9-5 |
| Response body `MaxResponseBytes` if not completed in this story. | Story 9-4 governance/AOT cleanup, with a deferred-work row created before 5-6 closes |

---

## References

- [Source: _bmad-output/planning-artifacts/epics/epic-5-reliable-real-time-experience.md#Story-5.6] - story statement, baseline ACs, FR48/FR72, NFR73/NFR74/NFR79/NFR80.
- [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting-Concerns] - Dapr-only/provider-neutral infrastructure policy, diagnostic policy, and structured logging shape.
- [Source: _bmad-output/planning-artifacts/prd/functional-requirements.md#Observability] - FR72 structured lifecycle observability.
- [Source: _bmad-output/planning-artifacts/prd/non-functional-requirements.md#Maintainability] - diagnostic ranges, logging contract, and infrastructure coupling constraints.
- [Source: _bmad-output/implementation-artifacts/5-1-eventstore-service-abstractions.md] - EventStore contracts, SignalR nudge-only transport, and zero infrastructure coupling baseline.
- [Source: _bmad-output/implementation-artifacts/5-2-http-response-handling-and-etag-caching.md] - response classifier, ETag cache, no-churn 304, and diagnostics redaction.
- [Source: _bmad-output/implementation-artifacts/5-3-signalr-connection-and-disconnection-handling.md] - connection-state service, fallback polling, and log-flood deferred item.
- [Source: _bmad-output/implementation-artifacts/5-4-reconnection-reconciliation-and-batched-updates.md] - reconciliation boundaries and 5-6 observability follow-up.
- [Source: _bmad-output/implementation-artifacts/5-5-command-idempotency-and-optimistic-updates.md] - pending-command observability seam and 5-6 exclusions.
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#Deferred-from-code-review-of-5-3-signalr-connection-and-disconnection-handling] - W1 log rate-limiting follow-up.
- [Source: src/Hexalith.FrontComposer.Contracts/Telemetry/FrontComposerActivitySource.cs] - shared ActivitySource constants.
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs] - current command dispatch/logging path.
- [Source: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs] - current query/cache/logging path.
- [Source: src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionState.cs] - current connection transition log behavior.
- [Source: tests/Hexalith.FrontComposer.Contracts.Tests/Communication/EventStoreContractTests.cs] - existing reference check to replace or harden.
- [Source: .github/workflows/ci.yml] - current advisory build-and-test gate.
- [Source: Microsoft Learn: High-performance logging in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging) - LoggerMessage/source-generated logging guidance.
- [Source: Microsoft Learn: Distributed tracing instrumentation in .NET](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs) - ActivitySource instrumentation guidance.
- [Source: OpenTelemetry .NET documentation](https://opentelemetry.io/docs/languages/dotnet/) - host/exporter integration guidance.
- [Source: OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/) - semantic convention reference for tags/log alignment.

---

## Dev Agent Record

### Party-Mode Review

- Date/time: 2026-04-26T07:04:31.7376522+02:00
- Selected story key: `5-6-build-time-infrastructure-enforcement-and-observability`
- Command/skill invocation used: `/bmad-party-mode 5-6-build-time-infrastructure-enforcement-and-observability; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), Mary (Business Analyst), John (Product Manager)
- Findings summary:
  - Architecture: telemetry operation names and tag keys needed one centralized contract to prevent drift across EventStore, projection, lifecycle, and pending-command call sites.
  - Implementation: connection-log rate limiting was directionally correct but needed deterministic `TimeProvider`-anchored windows/buckets and explicit terminal-transition visibility.
  - Test strategy: `ActivityListener` coverage should assert approved tag keys, not only activity names and redaction absence.
  - Scope / L10: DF2 `MaxResponseBytes` had an ambiguous owner if not completed in this story; closure now requires a Story 9-4-owned deferred-work row.
- Changes applied:
  - Added a Party-Mode Hardening Addendum for telemetry constants, deterministic rate limiting, and DF2 ownership.
  - Tightened T4, T5, T7, and Testing Standards around `MaxResponseBytes`, rate-limit semantics, centralized telemetry constants, and approved tag-key tests.
  - Tightened Known Gaps so deferred response-size protection has a concrete Story 9-4 owner.
- Findings deferred:
  - Exact numeric rate-limit defaults and bucket sizes remain implementation choices constrained by `TimeProvider`-based tests.
  - Final OpenTelemetry exporter recipe depth remains owned by Story 9-5 unless 5-6 adds only a host/sample snippet.
  - Broader diagnostic ID catalog governance remains Story 9-4.
- Final recommendation: ready-for-dev

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

(to be filled in by dev agent)

### File List

(to be filled in by dev agent)
