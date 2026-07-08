---
title: '11.3 MCP cross-request lifecycle and operability'
type: 'feature'
created: '2026-07-06T19:21:55+02:00'
status: 'done'
baseline_revision: '92edc300e5de8778c81d60bbc781613d9e1d1f21'
final_revision: '39569e58613487dce074843239d14c759538b9c1'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-11-2-projection-realtime-resilience.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** MCP lifecycle state is scoped with the request facade, so an agent command call can return a lifecycle handle that a later MCP request cannot read. Several fail-closed MCP paths also return safe outputs without any sanitized operator trace, and registration still builds a temporary provider.

**Approach:** Split lifecycle memory into a singleton store and keep admission/context as scoped facade logic, prove command subscribe then lifecycle poll across separate DI scopes, add source-generated sanitized logs for the named fail-closed branches, remove the registration-time provider build, and stop matching API keys against raw stored keys.

## Boundaries & Constraints

**Always:** Keep MCP agent-visible hidden/unknown surfaces unchanged, revalidate lifecycle reads through scoped admission, keep tenant/resource gates fail-closed, use source-generated `LoggerMessage` for new warnings, and keep API-key/log evidence free of raw keys, tenant IDs, user IDs, lifecycle handles, arguments, stack traces, and exception messages.

**Block If:** Removing `BuildServiceProvider()` requires an MCP SDK upgrade, a public package/API break, or a change to static SDK resource behavior that cannot be proven with `McpServerOptions` tests.

**Never:** Do not change generated manifests, SourceTools output, Contracts kernel/package split, command/projection routes, projection realtime code from Story 11.2, EventStore contracts, package versions, docs site files, or submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Cross-request lifecycle poll | Command tool invoked in service scope A; lifecycle tool polled by `correlationId` or `messageId` in scope B after scope A is disposed | Scope B returns the real terminal or in-progress lifecycle snapshot from the shared store | If scoped admission no longer permits the original command, return the existing hidden-unknown shape |
| Lifecycle state store capacity | Active entries exceed configured active or retained-terminal limits across requests | Oldest active entry moves to `NeedsReview`; oldest terminal entries are evicted exactly as before | Timers/subscriptions are disposed when store evicts or provider disposes |
| Tools-list fail closed | Missing/invalid context, tenant failure, or unexpected catalog exception with request services present | Empty or lifecycle-only tool list remains protocol-safe and exactly one sanitized log event records the fail-closed category | No raw exception text, tenant/user, API key, requested tool, or descriptor-sensitive name is logged |
| Projection reader downstream failure | Projection read hits the existing catch-all failure path | Result stays mapped to `DownstreamFailed` and exactly one sanitized log event records exception type/category | No stack trace, raw URI, tenant/user, query payload, or raw exception message is logged |
| Lifecycle auth failure | Lifecycle tool call fails auth/tenant/context precheck before handle lookup | Existing sanitized MCP failure shape is preserved and exactly one sanitized log event records the auth category | Known and unknown handles remain indistinguishable |
| API-key matching | API keys configured in options and request header supplies a candidate | Matching uses hashed stored credentials and constant-time hash comparison; valid keys still produce tenant/user context | Invalid, empty, whitespace, and multi-valued headers fail closed without logging or returning key material |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs` -- current scoped tracker, lifecycle dictionaries, timers, capacity, hidden-unknown reads, and admission revalidation.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs` -- add singleton lifecycle state store that owns dictionaries, timers, capacity, observed transitions, and disposal without scoped admission dependencies.
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpEndpointRouteBuilderExtensions.cs` -- force MCP server option materialization during endpoint mapping so DI-native resource checks still fail at startup.
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpServerOptionsConfigurator.cs` -- add DI-native MCP resource materialization from the real provider, replacing registration-time probing.
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs` -- add source-generated sanitized log helpers for MCP fail-closed paths.
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpApiKeyCredentialStore.cs` -- add hashed API-key credential matching store.
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs` -- MCP DI lifetimes, `tools/list`, `tools/call`, lifecycle route auth precheck, SDK resource registration, and `BuildServiceProvider()` removal.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs` -- projection read fail-closed catch-all branch requiring a sanitized log.
- `src/Hexalith.FrontComposer.Mcp/HttpFrontComposerMcpAgentContextAccessor.cs` -- API-key context resolution and schema fingerprint request cache.
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs` -- API-key option shape and lifecycle option validation constraints.
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs` and `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs` -- registry/skill inputs currently probed during registration and needed for DI-native resource materialization.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs` -- lifecycle tracker behavior tests that currently mask the production lifetime by singleton re-registration.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpCommandToolAdapterTests.cs` -- private handler tests for `tools/list`/`tools/call`, best place to prove cross-request command then lifecycle poll.
- `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs` -- AddFrontComposerMcp lifetime/resource registration tripwires.
- `tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs` -- API-key matching and auth-context regression tests.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs` -- add singleton store and move lifecycle entry dictionaries, capacity queues, terminal retention, timers, transition observation, normalization helpers, and disposal into it -- preserves lifecycle handles after request scope disposal without retaining scoped lifecycle subscriptions.
- [x] `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs` -- refactor to a scoped facade that injects the singleton store plus scoped admission/options/logger, delegates acknowledgement storage to the store, and performs read-time scoped admission revalidation before returning snapshots -- avoids captive scoped dependencies in singleton state.
- [x] `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpServerOptionsConfigurator.cs`, `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`, and `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpEndpointRouteBuilderExtensions.cs` -- register `FrontComposerMcpLifecycleStore` as singleton and `FrontComposerMcpLifecycleTracker` as scoped; replace registration-time `BuildServiceProvider()` with an `IConfigureOptions<McpServerOptions>` path that materializes projection and skill resources from the real provider; force option materialization at endpoint mapping so resource collisions still fail during startup; keep gate descriptor checks fail-closed.
- [x] `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs`, `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`, and `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs` -- add source-generated sanitized log events for tools-list fail-closed catches, lifecycle auth/context precheck failures, and projection reader catch-all failures -- gives operators one trace per silent denial/downgrade without changing MCP response shapes.
- [x] `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpApiKeyCredentialStore.cs`, `src/Hexalith.FrontComposer.Mcp/HttpFrontComposerMcpAgentContextAccessor.cs`, and `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs` -- introduce hashed API-key credential storage/matching while preserving `FrontComposerMcpOptions.ApiKeys` configuration compatibility and constant-time comparison -- avoids long-lived raw-key matching state.
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs` -- stop singleton-registering `FrontComposerMcpLifecycleTracker`, register production-like singleton store plus scoped facade, and keep existing lifecycle behavior tests green -- removes the test mask called out by the story.
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpCommandToolAdapterTests.cs` -- add cross-request hosting tests where command invocation in one scope is followed by lifecycle poll in a new scope by correlation and by message id, plus policy/tenant-loss hidden-unknown across scopes -- proves real `subscribe -> poll` behavior.
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs` -- add DI lifetime/resource registration tests proving lifecycle store singleton, tracker/admission scoped, AddFrontComposerMcp does not instantiate services through a temporary provider, SDK resources still include projection and skill resources, and lifecycle option validation rejects invalid bounds/URI prefix -- pins operability.
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs` and `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpCommandToolAdapterTests.cs` -- add regression coverage for hashed API-key matching and sanitized logging redaction; assert no raw API key, tenant/user, lifecycle handle, descriptor-sensitive name, exception message, or stack trace appears in captured log messages.
- [x] `_bmad-output/implementation-artifacts/spec-11-3-mcp-cross-request-lifecycle-and-operability.md` and `_bmad-output/implementation-artifacts/sprint-status.yaml` -- update status, validation evidence, file list, and auto-run result -- keeps BMAD artifacts consistent.

**Acceptance Criteria:**
- Given `FrontComposerMcpLifecycleTracker` is registered by `AddFrontComposerMcp`, when DI validates lifetimes or services are resolved from separate scopes, then lifecycle memory is singleton-owned while tracker, admission, context accessor, command invoker, and projection reader remain scoped.
- Given an agent invokes a command tool in one MCP request scope, when the lifecycle tool is called in a later request scope by `correlationId` or `messageId`, then the real lifecycle transitions are returned and the original request scope can already be disposed.
- Given an existing lifecycle handle becomes unauthorized by tenant or policy before a later poll, when the later request reads it, then the response is the same hidden-unknown shape as an absent handle and the handle value is not logged or returned.
- Given projection-reader catch-all, tools-list fail-closed, or lifecycle auth failure branches execute, when logs are captured, then each branch emits exactly one sanitized source-generated log event and preserves existing MCP response categories.
- Given API keys are configured, when valid and invalid API-key requests are evaluated, then valid keys still resolve context, invalid keys fail closed, and matching uses hashed stored credentials instead of repeated raw-key comparisons.

## Spec Change Log

- 2026-07-06: Implemented Story 11.3 and moved story to in-review. Focused MCP validation passed 110/110; full MCP project passed 368/368. Standard filtered solution lane is blocked by unrelated SourceTools IDE-parity metadata failure `IdeParityMatrixContractTests.MatrixJson_HasFailClosedSchemaForEveryRow` expecting SDK `10.0.300` while the current environment reports `10.0.301`.
- 2026-07-06: Completed review loop 1 patches. Focused MCP validation passed 114/114; full MCP project passed 372/372. Removed solution-level `.slnx` test evidence because repo instructions require individual test projects.

## Review Triage Log

- Review loop 1, patch: singleton lifecycle store retained a scoped `ILifecycleStateService` subscription. Fixed by keeping only a short-lived subscription during acknowledgement recording and storing observed transition snapshots without retaining scoped services.
- Review loop 1, patch: cross-request tests masked the scoped-lifecycle bug with singleton fake lifecycle services. Updated Story 11.3 lifecycle tests to use scoped lifecycle services and store-level observed-transition recording for monotonicity/idempotency cases.
- Review loop 1, patch: `FrontComposerMcpLifecycleStore` is a new public package type. Kept it public because `FrontComposerMcpLifecycleTracker` is already public and has a public DI constructor using the store; added XML summaries instead of creating a public API break. No missing-summary baseline entry is required for a documented type.
- Review loop 1, patch: `LifecycleUriPrefix` accepted local-path URI schemes. Validation now allows only `frontcomposer://` and `https://`, with tests for `file://` and `http://` rejection.
- Review loop 1, patch: API-key matching used a construction-time credential snapshot. Matching now hashes the current options on demand, so revocation/rotation on the existing options instance takes effect without provider rebuild; regression coverage added.
- Review loop 1, patch: resource collision checks moved to `IConfigureOptions<McpServerOptions>` and could be deferred. `MapFrontComposerMcp` now materializes `McpServerOptions`, and the configurator avoids duplicate resource additions if configured repeatedly.
- Review loop 1, patch: bounded-context fail-closed logs only trimmed raw manifest text. Tenant/policy gate logs now emit a stable SHA-256 token instead of raw bounded context; policy-gate redaction coverage added.
- Review loop 1, patch: policy-gate fail-closed logging lacked direct redaction coverage. Added `ListToolsHandler_PolicyGateException_IsSanitizedAndTreatedAsHidden`.
- Review loop 1, patch: story verification recorded a forbidden solution-level `.slnx` `dotnet test`. Removed it from acceptance evidence and retained individual test-project validation only, per repo instructions.
- Review loop 1, patch: new internal helper types lacked XML summaries. Added summaries to the credential store, log helper, server-options configurator, and lifecycle store public surface.

## Design Notes

The lifecycle split keeps `FrontComposerMcpLifecycleStore` unaware of scoped admission/context services and avoids retaining scoped lifecycle subscriptions. The scoped tracker passes descriptor/result/lifecycle service into the store for write-time transition recording, then performs current-scope admission revalidation before exposing a snapshot. For `BuildServiceProvider()` removal, the DI-native `IConfigureOptions<McpServerOptions>` path appends projection and skill resources after the real provider exists; `MapFrontComposerMcp` forces option materialization so startup checks remain observable in `HostingTests`.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release --filter "FullyQualifiedName~CommandLifecycleTests|FullyQualifiedName~McpCommandToolAdapterTests|FullyQualifiedName~HostingTests|FullyQualifiedName~AuthContextAccessorTests"` -- passed: 114/114.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release` -- passed: 372/372.
- Solution-level `dotnet test Hexalith.FrontComposer.slnx` was not rerun because repository instructions require running test projects individually and reserve `.slnx` for restore/build only.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-11-3-mcp-cross-request-lifecycle-and-operability.md` -- passed.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-11-3-mcp-cross-request-lifecycle-and-operability.md --base 92edc300e5de8778c81d60bbc781613d9e1d1f21` -- passed after recording `final_revision`.
- `git diff --check` -- passed; only line-ending normalization warnings.

## File List

- `_bmad-output/implementation-artifacts/spec-11-3-mcp-cross-request-lifecycle-and-operability.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/epic-11-context.md`
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpEndpointRouteBuilderExtensions.cs`
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpApiKeyCredentialStore.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpServerOptionsConfigurator.cs`
- `src/Hexalith.FrontComposer.Mcp/HttpFrontComposerMcpAgentContextAccessor.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpCommandToolAdapterTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTests.cs`

## Auto Run Result

Status: done

Summary: Implemented MCP cross-request lifecycle continuity and operability for Story 11.3, then completed review-loop patches. Lifecycle memory is singleton-owned with scoped tracker admission revalidation and no retained scoped lifecycle subscription; `AddFrontComposerMcp` no longer builds a temporary provider and configures SDK resources through `IConfigureOptions<McpServerOptions>` with endpoint-time materialization; tools-list, lifecycle precheck, tenant/policy gate, and projection-reader fail-closed paths emit sanitized source-generated warnings; API-key matching hashes current configured keys so revocation and rotation take effect.

Verification: focused Story 11.3 MCP lane passed 114/114; full MCP project passed 372/372. Solution-level `dotnet test` was not run because repository instructions require test projects individually and reserve `.slnx` for restore/build only.

Residual risk: review loop completed with no unresolved patch, intent-gap, or bad-spec findings.
