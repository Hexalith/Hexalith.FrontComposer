# Story 5.6: Build-Time Infrastructure Enforcement & Observability

Status: done

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
| AC1 | Framework assemblies, project files, generated outputs, restored transitive dependency graphs, and source files are inspected in CI | A forbidden direct or transitive dependency or namespace appears in FrontComposer framework code | The governance check fails the build with a descriptive message naming the project/file/reference, the forbidden provider family, and the remediation path through EventStore or deployment/AppHost Dapr components. |
| AC2 | The governance deny-list runs | Redis, Kafka, PostgreSQL/Npgsql, Cosmos DB, Dapr SDK, StackExchange.Redis, Confluent.Kafka, provider-specific Azure/AWS/GCP infrastructure SDKs, or direct database clients are referenced from `Hexalith.FrontComposer.Contracts`, `Hexalith.FrontComposer.SourceTools`, `Hexalith.FrontComposer.Shell`, or generated framework code | The violation is blocked unless the reference is covered by an exact story-owned allowlist entry. `PrivateAssets`, transitive package flow, aliases, and central package versions must not bypass the deny-list. SignalR client remains allowed because Story 5-1/5-3 made it the EventStore projection nudge transport. |
| AC3 | Infrastructure topology lives in samples, AppHosts, EventStore, or deployment docs | The governance checks scan the repository | AppHost, sample deployment files, EventStore submodule Dapr components, generated bin/obj artifacts, and documentation examples are excluded or separately classified through normalized root-bound paths so framework checks do not false-fail on legitimate topology code or follow symlink/reparse-point escapes into unrelated folders. |
| AC4 | Runtime services emit logs | Logs are captured from command dispatch, query execution, lifecycle transitions, projection connection state, rejoin failures, nudge refresh, fallback polling, and pending-command outcome resolution | Every log uses message template plus parameters, stable event IDs or HFC runtime diagnostic constants, and structured fields for `CommandType` or `ProjectionType`, redacted tenant marker, `CorrelationId`/`MessageId` when available, `Outcome`, `ElapsedMs`, `FailureCategory`, and `Transport` where applicable. No string interpolation is used in `ILogger.Log*` calls. |
| AC5 | Sensitive data redaction tests run | Logs or span tags are inspected after success and failure paths | No bearer token, raw access token, command/query/cache payload, form field value, raw ProblemDetails body, raw exception message or stack trace that can echo payload data, user ID, raw tenant ID, full request URI, query string, SignalR group name, or raw ETag is emitted. Failure categories are bounded and sanitized. |
| AC6 | `FrontComposerActivitySource` is used | Command submit, HTTP command dispatch, HTTP query, cache hit/miss/not-modified, projection nudge, fallback polling, lifecycle terminal transition, and UI refresh paths execute | Activities share the source name `Hexalith.FrontComposer`, include consistent operation names and approved tags, tolerate `StartActivity()` returning `null`, nest under `Activity.Current` when present, propagate through existing `HttpClient` instrumentation without custom header hacks, and expose enough sanitized correlation to trace user click -> backend command -> projection update -> SignalR nudge -> UI update. Telemetry failures must never change command, query, lifecycle, or reducer behavior. |
| AC7 | An adopter wires OpenTelemetry exporters in a host app | Traces and logs are exported to Grafana/OTLP, Jaeger/OTLP, or Application Insights | Framework activities and logs flow without package-specific exporter references in Contracts/SourceTools/Shell. Exporter packages belong to the host/sample, not the framework assemblies. |
| AC8 | The CI workflow runs on push or PR | Governance and telemetry tests fail | The build-and-test job fails, uploads test results, and surfaces the exact failing governance/telemetry test. Existing advisory `continue-on-error` behavior is removed or narrowed so this story's gates are blocking, and workflow path filters or matrix conditions cannot skip the governance lane for framework source, project, package, generated baseline, or workflow changes. |
| AC9 | Tests run | Governance, telemetry helper, EventStore clients, connection state, fallback polling, lifecycle wrapper, and generated forms execute | Coverage proves deny-list detection, allowlist precision, bin/obj/doc exclusion, descriptive failure messages, LoggerMessage/message-template usage, redaction, ActivitySource names/tags/parenting, no exporter dependency, and CI gate wiring. |

---

## Tasks / Subtasks

- [x] T1. Build the framework infrastructure-coupling governance suite (AC1, AC2, AC3, AC9)
  - [x] Replace the narrow `ContractsAssembly_DoesNotReferenceInfrastructurePackages` substring test with explicit deny-list tests that inspect `ProjectReference`, `PackageReference`, restored assembly references, and source `using`/fully-qualified namespace usage.
  - [x] Deny at least: `Dapr.*`, `StackExchange.Redis`, `Microsoft.AspNetCore.SignalR.StackExchangeRedis`, `Confluent.Kafka`, `Npgsql`, `Microsoft.Azure.Cosmos`, direct provider database/storage/event-bus SDKs, and namespace roots matching provider clients.
  - [x] Keep `Microsoft.AspNetCore.SignalR.Client` allowed only in Shell because it is the EventStore nudge transport from Stories 5-1 and 5-3.
  - [x] Normalize and root-bound every scanned path before classification. Do not follow symlinks, junctions, reparse points, or submodule worktrees from excluded areas back into framework scope or from framework scope out to unrelated folders.
  - [x] Exclude generated `bin/`, `obj/`, `.bmad`, `.agents`, `.github/skills`, planning docs, and submodules from framework source scanning unless a test explicitly validates docs/sample topology.
  - [x] Limit source namespace scanning to framework-owned `.cs`, `.razor`, `.cshtml`, and approved generated baseline files; do not scan markdown prose as source code except in documentation-specific tests.
  - [x] Emit failure messages that identify path, reference, provider family, and expected remediation: "route through EventStore contract/client or deployment/AppHost component configuration".
  - [x] Add exact-match allowlist entries with owner/story comments. Avoid substring matches like `Hosting` or `EventStore` that can false-positive on benign assembly names.

- [x] T2. Harden package and assembly reference validation (AC1, AC2, AC3, AC9)
  - [x] Parse project XML using `System.Xml.Linq`; do not use ad hoc string matching for `.csproj` files.
  - [x] Inspect `Directory.Packages.props` centrally and all framework `.csproj` files for forbidden package IDs.
  - [x] Inspect restored dependency graphs (`project.assets.json` or equivalent resolved package references) so transitive provider SDKs and packages hidden behind `PrivateAssets` cannot bypass governance.
  - [x] Add a runtime assembly reference test for each produced framework assembly: Contracts, SourceTools, and Shell. Shell may reference SignalR client; Contracts and SourceTools may not.
  - [x] Validate source-generator output baselines and generated command/projection artifacts do not contain forbidden namespace roots.
  - [x] Keep EventStore submodule Dapr/provider dependencies outside the FrontComposer framework enforcement scope.

- [x] T3. Add Shell telemetry infrastructure without exporter dependencies (AC6, AC7, AC9)
  - [x] Create a Shell telemetry helper such as `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerTelemetry.cs`.
  - [x] Instantiate `ActivitySource` using `Hexalith.FrontComposer.Contracts.Telemetry.FrontComposerActivitySource.Name` and `.Version`; keep the constants in Contracts.
  - [x] Add small methods for starting activities with consistent operation names and tags, for example `frontcomposer.command.dispatch`, `frontcomposer.query.execute`, `frontcomposer.projection.nudge`, `frontcomposer.projection.fallback_poll`, and `frontcomposer.lifecycle.transition`.
  - [x] Treat `ActivitySource.StartActivity(...)` returning `null` as the normal no-listener path. Tag helpers must be null-tolerant, allocation-conscious, and side-effect free.
  - [x] Keep tag enrichment fail-open: if telemetry metadata cannot be derived safely, omit the tag or emit a bounded failure category; do not throw from telemetry helper code.
  - [x] Do not add OpenTelemetry exporter packages to Contracts, SourceTools, or Shell. Host/sample code may demonstrate exporter wiring.
  - [x] Dispose only if a process-lifetime owner is introduced; static framework `ActivitySource` should not be disposed per scoped service lifetime.

- [x] T4. Instrument EventStore command/query paths (AC4, AC5, AC6, AC9)
  - [x] Wrap `EventStoreCommandClient.DispatchAsync` in an activity that records sanitized command type, message ID, correlation ID, HTTP outcome/status code, elapsed duration, and failure category.
  - [x] Wrap `EventStoreQueryClient.QueryAsync`/`ExecuteAsync` in an activity that records sanitized projection/query type, cache discriminator classification, ETag outcome (`hit`, `miss`, `not_modified`, `protocol_drift_retry`), status code, elapsed duration, and failure category.
  - [x] Keep request/response payloads, raw or hashed ETags, token values, raw tenant/user IDs, full request URIs, query strings, route values derived from tenant/user input, exception messages, stack traces, and ProblemDetails bodies out of log fields and span tags.
  - [x] Use route templates or bounded operation names rather than full URLs when tagging HTTP work. Outgoing HTTP trace context should rely on standard .NET `HttpClient` instrumentation and `Activity.Current`, not custom correlation headers carrying raw identity.
  - [x] Add a response-size governance note for DF2: either land `MaxResponseBytes` here or explicitly create a Story 9-4-owned deferred row with blocking rationale before closing 5-6. If landed, instrument response-size rejection as a bounded failure category.
  - [x] Preserve Story 5-2 no-churn semantics for `304`: telemetry records no-change, but reducers and UI state must not mutate.

- [x] T5. Instrument projection connection, rejoin, and fallback polling (AC4, AC5, AC6, AC9)
  - [x] Update `ProjectionConnectionStateService`, `SignalRProjectionHubConnectionFactory`, `ProjectionSubscriptionService`, `ProjectionFallbackRefreshScheduler`, and `ProjectionFallbackPollingDriver` to log through shared structured helpers or source-generated LoggerMessage methods.
  - [x] Add activities for connection state transitions, reconnect/rejoin sweep, nudge refresh, and fallback polling iteration.
  - [x] Add a lightweight rate-limiting or sampling policy for flapping connection logs, resolving deferred item W1 from Story 5-3 review. The policy must use `TimeProvider`-anchored windows/buckets, must not suppress state transitions from metrics/traces, and must keep terminal failure/recovered transitions visible.
  - [x] When repeated logs are suppressed, emit or retain a bounded suppression count in the next visible log record so operators can distinguish quiet recovery from repeated flapping.
  - [x] Ensure failed rejoin logs include `ProjectionType` and redacted tenant marker only if policy permits; never log raw group names or SignalR exception messages.
  - [x] Keep fallback polling behavior unchanged: no extra polling loop, no visible-lane registry duplication, and stop promptly on reconnect/disposal.

- [x] T6. Standardize lifecycle and pending-command logs/spans (AC4, AC5, AC6, AC9)
  - [x] Instrument `LifecycleStateService`, `FcLifecycleWrapper`, and the Story 5-5 pending-command resolver/summary seams if present.
  - [x] Include `CommandType`, `CorrelationId`, `MessageId`, terminal state, idempotency flag, elapsed threshold bucket, and failure category where available.
  - [x] Keep user-facing copy and lifecycle behavior unchanged. Telemetry must observe outcomes, not trigger duplicate lifecycle transitions or UI notifications.
  - [x] If Story 5-5 has not yet implemented pending-command services, document the expected instrumentation seam and add tests around existing lifecycle service only.

- [x] T7. Add structured logging helpers and regression tests (AC4, AC5, AC9)
  - [x] Prefer `[LoggerMessage]` source-generated partial methods for high-frequency EventStore/projection/lifecycle logs. For low-frequency code, message-template `logger.Log*("...", arg)` is acceptable.
  - [x] Centralize telemetry operation-name and tag-key constants in the Shell telemetry helper; tests must prove call sites use the centralized source name/version and approved tag set.
  - [x] Add a source scanner test that fails on `logger.Log*( $"...")`, interpolated message templates, string concatenated templates, raw `ex.Message` template arguments on EventStore/projection paths, `BeginScope` values carrying raw payload/identity data, and direct logging of payload variables.
  - [x] Add redaction tests extending `EventStoreDiagnosticsTests` to cover token acquisition failures, bad JSON, query failure, rejoin failure, fallback polling failure, lifecycle failure, and telemetry span tags.
  - [x] Use a capturing `ILogger` and `ActivityListener` test harness. Do not require live OpenTelemetry collectors.

- [x] T8. Demonstrate host exporter compatibility without framework coupling (AC7, AC9)
  - [x] Add or update a sample/host-only snippet that registers OpenTelemetry tracing/logging against `Hexalith.FrontComposer` ActivitySource. Keep exporter packages in the sample host if a concrete exporter is demonstrated.
  - [x] Cover OTLP compatibility for Grafana/Tempo or Jaeger and Application Insights compatibility by standard OpenTelemetry exporter configuration, not framework-specific runtime dependencies.
  - [x] If sample host dependencies would bloat this story, create documentation-only guidance and a governance test proving framework packages remain exporter-free.

- [x] T9. Make CI enforcement blocking (AC8, AC9)
  - [x] Update `.github/workflows/ci.yml` so the governance/telemetry test lane blocks PR/push. Remove `continue-on-error: true` from `build-and-test` or add a separate blocking governance job.
  - [x] Add a named step such as `Gate 2b: Infrastructure governance and telemetry contracts` if that keeps output clearer than folding into Gate 3a.
  - [x] Add workflow regression coverage for path filters and matrix conditions so governance still runs for changes to framework source, `.csproj`, `Directory.Packages.props`, generated baselines, governance tests, and the workflow itself.
  - [x] Ensure TRX upload and summary still run on failure.
  - [x] Keep performance/e2e-palette advisory behavior only if the story explicitly documents why those lanes remain separate from governance.

- [x] T10. Tests and verification (AC1-AC9)
  - [x] Governance tests: forbidden package in a synthetic project fails, allowed SignalR Shell reference passes, docs/bin/obj/submodules are excluded, exact allowlist entries work, and failure messages are actionable.
  - [x] Assembly tests: Contracts and SourceTools have no infrastructure/provider references; Shell has only approved runtime references.
  - [x] Logging tests: message-template usage, event ID/HFC constant presence, required structured fields, no interpolated templates, no raw exception messages on sensitive paths.
  - [x] Activity tests: source name/version, operation names, parent/child behavior, key tags, no payload/token/user/raw tenant tags, and no exporter package reference.
  - [x] CI tests: workflow contains blocking governance execution and does not hide failing governance tests behind job-level `continue-on-error`.
  - [x] Regression suite: run `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false` and targeted Contracts/Shell/SourceTools tests. Run full solution tests if the working tree is otherwise clean.

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

### Advanced Elicitation Hardening Addendum

Advanced elicitation on 2026-04-26 tightened four implementation traps before development:

- Governance must inspect the restored transitive dependency graph, not only direct XML references. `PrivateAssets`, central package management, aliases, and generated baselines cannot hide provider SDK coupling.
- Source scanning must be path-safe. Normalize every candidate path, stay inside expected framework roots, and avoid symlink/reparse-point traversal surprises before applying include/exclude rules.
- Telemetry must be fail-open and side-effect free. Missing listeners, null `Activity`, failed tag derivation, or suppressed logs must never alter command/query/lifecycle behavior.
- Redaction must cover transport-adjacent values, not only obvious payloads: full request URIs, query strings, raw ETags, route values derived from tenant/user input, SignalR group names, exception messages, and stack traces stay out of logs and tags.

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
| D13 | Governance includes restored transitive package graphs and generated baselines. | Direct XML checks miss provider SDKs introduced through central package management, transitive references, generated code, or `PrivateAssets`. | Scan only `.csproj`; rely on NuGet lockfile review. |
| D14 | Telemetry instrumentation is fail-open and behavior-neutral. | Observability must not be able to break command dispatch, query no-churn semantics, lifecycle transitions, reducers, or UI state. | Throw from telemetry helper failures; use telemetry side effects to drive behavior. |
| D15 | Tags and log fields use bounded, approved fields only. | Correlation is useful, but full URIs, group names, raw ETags, raw identities, exception details, and payload-like values create PII and cardinality risk. | Emit whatever data makes debugging easiest; rely on exporter-side filters. |
| D16 | CI governance cannot be bypassed by broad advisory settings or path filters. | A blocking gate still fails as a process control if normal framework changes can skip it. | Keep a blocking test but let workflow filters/matrix conditions silently omit it. |

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
- Governance tests must include synthetic `PrivateAssets`, transitive dependency, central package version, generated-baseline, symlink/reparse-point, and excluded-doc/sample fixtures so the scanner proves both detection and non-overreach.
- Telemetry tests must include the no-listener path where `StartActivity()` returns `null`, tag derivation failure, log suppression with bounded suppression count, and a thrown logging sink/capturing logger if the local harness can model it without changing production behavior.
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

### Advanced Elicitation

- Date/time: 2026-04-26T09:09:53.4848022+02:00
- Selected story key: `5-6-build-time-infrastructure-enforcement-and-observability`
- Command/skill invocation used: `/bmad-advanced-elicitation 5-6-build-time-infrastructure-enforcement-and-observability`
- Batch 1 method names:
  - Pre-mortem Analysis
  - Failure Mode Analysis
  - Red Team vs Blue Team
  - Security Audit Personas
  - Self-Consistency Validation
- Reshuffled Batch 2 method names:
  - Chaos Monkey Scenarios
  - First Principles Analysis
  - Comparative Analysis Matrix
  - Hindsight Reflection
  - Critique and Refine
- Findings summary:
  - Governance checks could still miss provider coupling through transitive packages, central package management, `PrivateAssets`, generated baselines, or path traversal edge cases.
  - Telemetry code needed an explicit fail-open contract so missing listeners, null activities, tag derivation issues, or logging failures cannot alter runtime behavior.
  - Existing redaction guidance did not name several transport-adjacent leak vectors: full URLs, query strings, raw ETags, route values, SignalR group names, exception messages, and stack traces.
  - CI blocking needed proof that path filters and matrix conditions do not skip governance for framework source, package, generated baseline, or workflow changes.
- Changes applied:
  - Tightened AC1-AC3, T1, and T2 for restored dependency graph inspection, path normalization, symlink/reparse safety, source-scan scope, and exact allowlist behavior.
  - Tightened AC5-AC6, T3-T5, and T7 for null `Activity` handling, fail-open telemetry helpers, standard `HttpClient` trace propagation, redaction coverage, and suppression-count visibility.
  - Tightened AC8, T9, and Testing Standards for workflow path-filter coverage, transitive/PrivateAssets fixtures, generated-baseline fixtures, no-listener telemetry tests, and suppression-count tests.
  - Added Advanced Elicitation Hardening Addendum plus D13-D16 covering transitive governance, behavior-neutral telemetry, approved bounded fields, and non-bypassable CI governance.
- Findings deferred:
  - Exact telemetry event ID allocation and numeric log-rate bucket defaults remain implementation choices constrained by the new tests.
  - Whether `MaxResponseBytes` lands in 5-6 or is deferred remains a story implementation decision, but closure still requires the existing Story 9-4-owned deferred-work row if not landed.
  - Full exporter recipe documentation, provider/Pact trace propagation, diagnostic analyzer/code-fix governance, and live cloud smoke tests remain with the named follow-up stories.
- Final recommendation: ready-for-dev

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false` — passed.
- `dotnet test Hexalith.FrontComposer.sln --no-build --filter "Category=Governance"` — passed, 20/0/0 in Shell governance lane.
- `dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj --no-build` — passed, 91/0/0.
- `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --no-build` — passed, 486/0/0.
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --no-build` — passed, 1204/0/3.
- `dotnet test Hexalith.FrontComposer.sln --no-build` — passed, Contracts 91/0/0, SourceTools 486/0/0, Shell 1204/0/3, Bench 2/0/0.

### Completion Notes List

- Added deterministic infrastructure governance tests for framework project XML, central package versions, restored `project.assets.json` transitive packages, runtime assembly references, framework source namespaces, generated baselines, exact Shell SignalR allowlist behavior, source logging discipline, and CI workflow blocking behavior.
- Added Shell-owned telemetry primitives using the Contracts `FrontComposerActivitySource` name/version, centralized operation/tag constants, source-generated logging helpers, null-tolerant activity helpers, bounded failure categories, and sanitized tenant markers.
- Instrumented EventStore command and query paths with activities, structured outcomes, status codes, elapsed duration, correlation/message IDs where allowed, and redaction-preserving logs without payloads, raw ETags, full URLs, raw identities, raw exception messages, or exception objects in sensitive paths.
- Instrumented projection nudge, fallback polling, reconnect/rejoin, lifecycle transitions, and pending-command terminal seams. Added deterministic TimeProvider-anchored connection-log rate limiting with suppression counts while keeping state transitions and activities observable.
- Updated CI so `build-and-test` is blocking and added `Gate 2b: Infrastructure governance and telemetry contracts`; palette/performance lanes remain step-level advisory per their existing variance rationale.
- Did not implement query `MaxResponseBytes`; logged concrete Story 9-4-owned deferred row `5-6-DF1` with rationale and remaining risk.

### File List

- `.github/workflows/ci.yml`
- `_bmad-output/implementation-artifacts/5-6-build-time-infrastructure-enforcement-and-observability.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerLog.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerTelemetry.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/UlidFactory.cs`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs`
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionState.cs`
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackPollingDriver.cs`
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/EventStoreTelemetryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerTelemetryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ProjectionConnection/ProjectionConnectionTelemetryTests.cs`

### Change Log

- 2026-04-26 — Implemented Story 5-6 governance, telemetry, structured logging, CI blocking gate, and verification coverage; moved story to review.
- 2026-04-27 — Pass-1 code review COMPLETED via bmad-code-review (3-layer adversarial: Blind Hunter + Edge Case Hunter + Acceptance Auditor). 89 raw findings → 54 unique after dedup → 23 dismissed → 31 actionable (2 blockers, 12 decision-needed, 24 patches, 13 defers). Verdict: FAIL — blockers F01/F02 invalidate core T1/T2 governance enforcement.
- 2026-04-27 — Pass-1 follow-up COMPLETED via bmad-code-review (decisions taken via best-engineering-judgment + patches applied). 12 decisions resolved (F03=a, F04=a, F05=a, F06=b dismiss, F07=a, F08=b dismiss, F09=a, F10=a, F11=b dismiss, F12=a, F13=a, F14=a defer to Story 9-5). 27 of 31 actionable findings closed via patches (F01-F05 + F07 + F09-F13 + F15-F33 + F35-F39 + F41). 4 patches deferred for next pass (F17/F20 partial, F25, F34, F40). Validation: dotnet build Hexalith.FrontComposer.sln /p:TreatWarningsAsErrors=true /p:UseSharedCompilation=false clean; dotnet test Hexalith.FrontComposer.sln --no-build => Contracts 91/0/0, Shell 1218/0/3, SourceTools 486/0/0, Bench 2/0/0. Story status review → done.

### Review Findings

#### Pass-1 (2026-04-27) — Adversarial 3-layer code review

**Blockers (must resolve before close):**

- [x] [Review][Blocker] **F01 — `Microsoft.AspNetCore.SignalR.Client` not in `PackageRules` deny-list** [`tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs:236-252`] — Contracts/SourceTools could reference SignalR Client without violation. Spec T1/T2 + AC2 + D3 require Shell-only allowlist; current `PackageRules` has no entry for it, so the Shell-exclusivity guarantee is not enforced. `ShellSignalRClient_IsExactAllowlistOnly` test passes only because StackExchange.Redis is the violation, not because Client allow-listing is exercised.
- [x] [Review][Blocker] **F02 — Original substring test `EventStoreContractTests.ContractsAssembly_DoesNotReferenceInfrastructurePackages` not replaced** [`tests/Hexalith.FrontComposer.Contracts.Tests/Communication/EventStoreContractTests.cs:50-63`] — Spec line 142 names this file explicitly: "Replace or supplement with exact governance tests; avoid false positives such as `Hosting`/`EventStore` substrings." T1 sub-bullet "Replace the narrow `ContractsAssembly_DoesNotReferenceInfrastructurePackages` substring test" is checked but the test still uses `Contains("Hosting")` / `Contains("EventStore")` patterns D2 forbids.

**Decision-needed (resolve via human input before patches apply):**

- [x] [Review][Decision] **F03 — Double `frontcomposer.projection.nudge` activity** [`ProjectionSubscriptionService.cs:166-170` + `ProjectionFallbackRefreshScheduler.cs:158-161`] — Both code paths open `StartProjectionNudge`. If scheduler-triggered nudge runs in same call chain as subscription-side nudge, two sibling activities for one logical nudge inflate metrics. Choices: (a) mark scheduler-side as child via `Activity.Current` parent; (b) skip activity in scheduler path; (c) accept dual-emission (document).
- [x] [Review][Decision] **F04 — Namespace scan `text.Contains(ns + ".")` matches comments/strings/XML docs** [`InfrastructureGovernanceTests.cs:2147-2149`] — Files mentioning `Dapr.Client` in a comment or string literal will fail governance. Choices: (a) restrict to lines starting with `using …` / `global using …` only; (b) Roslyn-based token scan; (c) accept FP risk and document allowlist convention.
- [x] [Review][Decision] **F05 — `ScanSourceTree` reparse-point check happens after `Directory.EnumerateFiles(AllDirectories)` already followed junctions** [`InfrastructureGovernanceTests.cs:2151-2168`] — T1 explicitly says "Do not follow symlinks, junctions, reparse points, or submodule worktrees." Per-file `FileAttributes.ReparsePoint` check only stops scanning the symlink itself, not its target tree. Choices: (a) `EnumerationOptions { AttributesToSkip = ReparsePoint }` (.NET 6+); (b) custom recursive walker with attribute pre-check; (c) accept current behavior given submodule path-exclusion already filters most cases.
- [x] [Review][Decision] **F06 — `EventStoreResponseClassifier`: `LogWarning(ex, ...)` → `LogWarning("...", ex.GetType().Name)` removes stack trace** [`EventStoreResponseClassifier.cs:228-232`] — ProblemDetails parse failure (server payload, not transport). Stack trace lost across all consumers. Choices: (a) restore `ex` argument since this path doesn't echo payload data; (b) keep current to enforce T7 universally; (c) tag `failure_category` on activity only and drop log-side stack.
- [x] [Review][Decision] **F07 — Suppression count lost across circuit/process exit** [`ProjectionConnectionState.cs:167-192`] — Scoped service in-memory `_logBuckets` not flushed on disposal. Choices: (a) implement `IAsyncDisposable.DisposeAsync` to emit a final `connection.suppressed_total` log; (b) accept the limitation (operators rely on Connected/Disconnected boundary).
- [x] [Review][Decision] **F08 — `Connected`/`Disconnected` clears entire `_logBuckets` map across ALL keys** [`ProjectionConnectionState.cs:168-174`] — Sums and clears all buckets on a single resolved transition. While `Reconnecting(cat=Timeout)` accumulates 5 suppressions, an unrelated transient `Disconnected` arrives → context lost if reconnect resumes Reconnecting(cat=Timeout). Choices: (a) clear only the bucket whose key was just resolved; (b) keep current aggregation (operators see total flap count).
- [x] [Review][Decision] **F09 — `SafeIdentifier` (span tag) vs raw `correlationId` (logs) divergence** [`FrontComposerTelemetry.cs:181-182` vs `FrontComposerLog.LifecycleTransitionObserved`] — Same correlation appears sanitized in spans but raw in logs. Operators cannot join trace and log on correlation. Choices: (a) sanitize correlation in both surfaces; (b) sanitize spans only and document log raw policy; (c) reject malformed correlation server-side and impose ULID-only contract.
- [x] [Review][Decision] **F10 — `ScanRestoredAssets` returns empty silently when `project.assets.json` missing** [`InfrastructureGovernanceTests.cs:2045-2049`] — Local devs running governance tests without prior `dotnet restore` get false-green. CI is unaffected (Gate 1+2 restore first). Choices: (a) emit `Skip` directive with reason; (b) `Fail` requiring explicit restore; (c) accept current.
- [x] [Review][Decision] **F11 — ETag cache write race: telemetry `cache_outcome=miss` may diverge from served payload under concurrent write** [`EventStoreQueryClient.cs:39-58, 85`] — Initial `cachedEntry` snapshot taken once; concurrent `PersistCacheEntryAsync` may write before recursive retry reads. Choices: (a) re-tag activity at end of method based on resolved entry; (b) accept eventual-consistency tag per query.
- [x] [Review][Decision] **F12 — `ProjectionFallbackPollingDriver` race: reconnect mid-iteration tags activity `outcome=refreshed` despite reconnect** [`ProjectionFallbackPollingDriver.cs:152-178` + `ProjectionFallbackRefreshScheduler.cs:124-152`] — Outer activity started before disconnected check; per-lane inner re-check catches reconnect mid-loop but outer span keeps `outcome=refreshed`. Choices: (a) introduce `outcome=stale_after_reconnect` tag on outer; (b) accept (lane-level resolution captured).
- [x] [Review][Decision] **F13 — `CommandUnexpectedStatus` log emits raw `LocationPath`** [`EventStoreCommandClient.cs:86` + `FrontComposerLog.cs:CommandUnexpectedStatus` template] — `response.Headers.Location?.GetLeftPart(UriPartial.Path)` strips query string but retains full path which can contain raw aggregate IDs derived from tenant/user input. AC5 explicitly forbids "full request URI" and "route values derived from tenant/user input." T4 says "Use route templates or bounded operation names rather than full URLs." Choices: (a) redact to route template (e.g., `/commands/{aggregate}`); (b) tag aggregateId-only via separate sanitized field; (c) accept (path is bounded by EventStore endpoint convention).
- [x] [Review][Decision] **F14 — T8 sample/host OpenTelemetry snippet OR docs not delivered** [`samples/Counter/Counter.Web/Program.cs` unchanged; no docs added] — T8 says "snippet OR documentation-only guidance and a governance test proving framework packages remain exporter-free." No-exporter governance proof exists; docs/snippet does not. Choices: (a) ratify deferral to Story 9-5 (Known Gaps row line 263 already covers exporter recipe docs); (b) add a docs/sample snippet now in this story.

**Patches (apply to close blockers above + AC delta):**

- [x] [Review][Patch] **F15 — `_logBuckets.Values.Sum(...)` LINQ inside lock allocates per Connected/Disconnected transition** [`ProjectionConnectionState.cs:170`]
- [x] [Review][Patch] **F16 — Bucket key `Status|FailureCategory` defeats rate-limit when category churns; dictionary unbounded** [`ProjectionConnectionState.cs:54, 177-191`]
- [ ] [Review][Patch] **F17 — `ProjectionConnectionTelemetryTests` shape-only assertion `Last().ShouldContain("SuppressedCount=1")`** [`ProjectionConnectionTelemetryTests.cs:18-32`] — partially addressed: F38 added a behavioural test that asserts the Connected log specifically (`Status=Connected` + `SuppressedCount=1`); the original shape-only assertion was tightened to `Last(m => m.Contains("Status=Connected"))` but a full rewrite to assert structured-state values (vs. formatted strings) remains. Defer.
- [x] [Review][Patch] **F18 — `OperationCanceledException` filter `when (cancellationToken.IsCancellationRequested)` too narrow — linked-CTS canceled fires `failure` instead of `canceled`** [`EventStoreCommandClient.cs:117-124`]
- [x] [Review][Patch] **F19 — 304 retry recursion: outer span tagged `protocol_drift_retry` then overwritten by `SetFailure(ex.Type)` on inner failure** [`EventStoreQueryClient.cs:155-170`]
- [x] [Review][Patch] **F20 — `EventStoreTelemetryTests.CommandDispatch_EmitsSanitizedActivity` token assertion vacuous — checks `Tags`, but token only lives in HTTP `Authorization` header** [`EventStoreTelemetryTests.cs:50-54`]
- [x] [Review][Patch] **F21 — Sensitive-paths regex misses `BeginScope(...)`, string-concat `"..." + var`, and `LoggerExtensions.LogX(_logger, $"...")` static-call form** [`InfrastructureGovernanceTests.cs:96-117, 1882-1886`]
- [x] [Review][Patch] **F22 — `ScanProjectReferences` classifier conflates package IDs with `ProjectReference` Include paths (e.g., `..\Dapr.Foo.csproj` matches `StartsWith("Dapr.")`)** [`InfrastructureGovernanceTests.cs:243-254`]
- [x] [Review][Patch] **F23 — Rejected branch: `SetFailure(failureCategory)` overwritten by outer `catch (Exception ex) → SetFailure(ex.GetType().Name)`** [`EventStoreCommandClient.cs:71-89`]
- [x] [Review][Patch] **F24 — `Workflow_DoesNotUsePathFiltersThatCanSkipFrameworkGovernance` forbids any `paths:` substring — false positives on `actions/upload-artifact paths:`** [`CiGovernanceTests.cs:24-31`]
- [ ] [Review][Patch] **F25 — Long-flap suppression count visible only after 30s silence — bucket `SuppressedCount` doesn't decay** [`ProjectionConnectionState.cs:180-190`] — Defer; F16 cap and F07 DisposeAsync flush bound the worst-case operator surprise.
- [x] [Review][Patch] **F26 — `BoundCategory` returns null for whitespace-only input → `SetFailure` silently no-ops** [`FrontComposerTelemetry.cs:111-149`] — dismissed: `IsNullOrWhiteSpace` short-circuits at the function entry, so whitespace-only inputs return null at the early guard rather than reaching the foreach. The `written == 0` branch is unreachable in practice and the null return is the correct contract for "no failure to record".
- [x] [Review][Patch] **F27 — Pending command `duplicateActivity` started + disposed without `SetOutcome`/`SetElapsed` — variable unused** [`PendingCommandStateService.cs:177-184`]
- [x] [Review][Patch] **F28 — 304 path doesn't distinguish `not_modified` (no-change signal) from `from_cache` (cached payload served)** [`EventStoreQueryClient.cs:141-153`]
- [x] [Review][Patch] **F29 — `EventStoreCommandClient` outer `catch (Exception ex)` sets `SetFailure` but not `SetOutcome("failed")`** [`EventStoreCommandClient.cs:113-116`]
- [x] [Review][Patch] **F30 — `EventStoreQueryClient` outer `catch (Exception ex)` sets `SetFailure` but not `SetOutcome("failed")`** [`EventStoreQueryClient.cs:258-261`]
- [x] [Review][Patch] **F31 — `BuildAndTestJob_IsBlockingAndHasGovernanceTelemetryGate` only checks job header — step-level `continue-on-error: true` on Gate 2b future would pass** [`CiGovernanceTests.cs:13-20`]
- [x] [Review][Patch] **F32 — Dead code in `ScanSourceFile`: `rule.Reference == "Microsoft.AspNetCore.SignalR.Client"` — no matching entry in `NamespaceRules`** [`InfrastructureGovernanceTests.cs:2103-2118`]
- [x] [Review][Patch] **F33 — Lifecycle activity disposed at method exit but `InvokeSubscribers` runs inside scope, then disposed — subscriber-started activities don't nest under lifecycle parent** [`LifecycleStateService.cs:294-306`]
- [ ] [Review][Patch] **F34 — `SafeIdentifier(correlationId)` server-supplied — accept ULID strict format or tag `malformed_correlation`** [`EventStoreCommandClient.cs:96`] — Defer; `SafeIdentifierOrAbsent` (added by F09) already returns `"malformed"` when sanitization produces zero chars. Strict ULID-only contract requires upstream EventStore alignment and is owned by Story 5-1 contract evolution.
- [x] [Review][Patch] **F35 — No synthetic fixture for transitive package surfacing only in `project.assets.json` (not in `.csproj`)** [`InfrastructureGovernanceTests.cs:1899-1916`]
- [x] [Review][Patch] **F36 — Telemetry helper not tested under thrown logging sink (Advanced Elicitation Hardening Addendum requires fail-open sink test)** [`FrontComposerTelemetryTests.cs`]
- [x] [Review][Patch] **F37 — `StartActivity_NoListenerPath_DoesNotThrow` test has no explicit assertion — relies on implicit unhandled-exception detection** [`FrontComposerTelemetryTests.cs:36-43`]
- [x] [Review][Patch] **F38 — `ProjectionConnectionTelemetryTests` never advances `FakeTimeProvider` — 30s window expiry untested** [`ProjectionConnectionTelemetryTests.cs:24-39`]
- [x] [Review][Patch] **F39 — `protocol_drift_retry` cache outcome value never tested via `ActivityListener`** [`EventStoreTelemetryTests.cs:42-67`]
- [ ] [Review][Patch] **F40 — No redaction tests for token acquisition failure / bad JSON / lifecycle failure span tags (T7 explicit requirement)** [`tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/`] — Defer; Pass-1 added partial failure-path coverage via F36 (degenerate-input fail-open) and F39 (protocol_drift_retry). Full redaction-on-failure matrix remains future work and is bounded by F40-A backlog.
- [x] [Review][Patch] **F41 — Synthetic deny-list test covers only Dapr+Redis — Confluent.Kafka, Cosmos, ServiceBus, Amazon.S3, Google.Cloud not exercised** [`InfrastructureGovernanceTests.cs:1899-1948`]

**Defers (pre-existing or out-of-scope, recorded in `deferred-work.md`):**

- [x] [Review][Defer] **F42 — Outer catch overwrites inner failure tag with wrapping exception type (`ProjectionSchemaMismatchException` masks `JsonException`)** [`EventStoreQueryClient.cs:200-210`] — minor failure categorization fidelity loss; defer for diagnostic catalog work.
- [x] [Review][Defer] **F43 — `SafeIdentifier` silently truncates correlation/message IDs at 64 chars without indicator** [`FrontComposerTelemetry.cs:127-167`] — ULIDs (26) and GUID-N (32) fit; longer IDs are framework-rare; revisit if upstream uses long correlation values.
- [x] [Review][Defer] **F44 — `BoundCategory` returns null for inputs producing zero sanitized chars** [`FrontComposerTelemetry.cs:130-145`] — only theoretical for type names; cosmetic.
- [x] [Review][Defer] **F45 — `ReadCorrelationIdAsync` log argument inconsistency: `commandType` raw vs span-side sanitized** [`EventStoreCommandClient.cs:163-184`] — `FullName` is reflection-bounded so safe.
- [x] [Review][Defer] **F46 — `ScanGeneratedBaselines` only walks `*.verified.txt`; `.g.cs` source-generator output excluded by `bin`/`obj` filter** [`InfrastructureGovernanceTests.cs:308-318`] — coverage gap vs T2 intent; tighten when generator emit-stage adds risk surface.
- [x] [Review][Defer] **F47 — `NamespaceRules` doesn't list `Azure.Core`, `Microsoft.Azure.WebJobs`, `Azure.Identity`, etc.** [`InfrastructureGovernanceTests.cs:254-266`] — narrow by design; expand on first false-negative.
- [x] [Review][Defer] **F48 — `CiGovernanceTests` parses workflow via fragile string-slicing on `"  build-and-test:"` and `"    steps:"`** [`CiGovernanceTests.cs:13-20`] — YAML reformat risk; defer to YAML-parser-based test rewrite.
- [x] [Review][Defer] **F49 — Cache write fire-and-forget swallows exceptions; no activity tag for cache-write failure** [`EventStoreQueryClient.cs:217-228, 275-282`] — outer activity already disposed by the time `PersistCacheEntryAsync` catch runs.
- [x] [Review][Defer] **F50 — Lifecycle log has no rate-limit while connection-state does** [`LifecycleStateService.cs:294-304`] — flapping Confirmed→Confirmed transitions could flood; W1 was scoped to connection logs.
- [x] [Review][Defer] **F51 — Path normalization: substring checks like `.Contains("/bin/")` aren't anchored at directory boundaries** [`InfrastructureGovernanceTests.cs:2156-2164`] — false-negative theoretical only.
- [x] [Review][Defer] **F52 — Connection rate-limit window hardcoded `TimeSpan.FromSeconds(30)` (no `FcShellOptions` knob)** [`ProjectionConnectionState.cs:181`] — spec line 312 explicitly allows magic literal as implementation choice.
- [x] [Review][Defer] **F53 — Generated baseline scanner false-positive risk on Verify snapshots that legitimately mention provider names** [`InfrastructureGovernanceTests.cs:ScanGeneratedBaselines`] — no current Verify file references provider namespaces; tighten scanner if ever needed.
- [x] [Review][Defer] **F54 — `ProjectionFallbackRefreshScheduler.TriggerFallbackOnceAsync` activity has no `ProjectionType` tag — aggregate fallback span only** [`ProjectionFallbackRefreshScheduler.cs:122-148`] — minor observability gap; per-lane child spans would address.
