---
baseline_commit: c221237
---

# Story 5.2: Lifecycle subscription tool

Status: done

<!-- Note: Validation completed during create-story. -->

## Story

As an AI agent,
I want to poll a command's lifecycle,
so that I can await confirmation after invoking it.

## Acceptance Criteria

1. Given the fixed `frontcomposer.lifecycle.subscribe` tool, when I pass exactly one framework-issued `correlationId` or `messageId`, then I receive an `McpLifecycleSnapshot` with state, terminal flag, outcome, bounded transition history, retry guidance, and max long-poll guidance. (FR16)
2. Given the lifecycle handle is malformed, unknown, tenant-hidden, policy-hidden, unauthenticated, or stale after visibility changes, when the tool is called, then the call is rejected with the same opaque hidden/unknown shape and without leaking internal state, identifiers, tenant/user values, policy names, command args, exception text, or raw payloads. (FR18)
3. Given a lifecycle entry has reached a terminal state, when duplicate, out-of-order, late, parallel, or replayed observations/read calls occur, then the first terminal outcome remains authoritative and transition history remains bounded, ordered, and idempotent. (FR16)
4. Given lifecycle retry and retention options are configured, when acknowledgement and later snapshots are produced, then `retryAfterMs`, `maxLongPollMs`, timeout, active-entry capacity, and retained-terminal capacity use validated option bounds and deterministic time in tests. (FR16)

## Tasks / Subtasks

- [x] Record the FC-MCP-LIFECYCLE v1 lifecycle subscription contract (AC: 1, 2, 3, 4)
  - [x] Create `_bmad-output/contracts/fc-mcp-lifecycle-subscription-contract-2026-06-05.md`.
  - [x] Define the fixed tool name from `FrontComposerMcpOptions.LifecycleToolName`, the one-of input contract, the accepted handle grammar, and the exact `McpLifecycleSnapshot` JSON shape.
  - [x] Resolve the current doc/source ambiguity explicitly: source currently emits retry guidance as `retry.retryAfterMs` and `retry.maxLongPollMs`; either bless that nested v1 shape in the contract and update docs/tests, or intentionally add top-level compatibility fields with tests.
  - [x] Record non-goals: no streaming subscription transport, no MCP retry loop, no command-status HTTP client, no projection/resource work, no schema-fingerprint negotiation changes, no new authorization framework.

- [x] Confirm and pin lifecycle tool catalog/schema behavior (AC: 1, 2)
  - [x] Extend `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs` or add a focused lifecycle-tool schema test.
  - [x] Prove `tools/list` includes exactly one fixed lifecycle tool after the currently visible generated command catalog and that it is not counted as a generated command descriptor.
  - [x] Prove `FrontComposerMcpProtocolMapper.ToLifecycleTool` uses `FrontComposerMcpOptions.LifecycleToolName` and `McpJsonSchemaBuilder.BuildLifecycleInputSchema()`.
  - [x] Add schema metadata to `src/Hexalith.FrontComposer.Mcp/McpJsonSchemaBuilder.cs`: both `correlationId` and `messageId` should advertise string type, `maxLength: 64`, canonical ULID pattern `^[0-9A-HJKMNP-TV-Z]{26}$`, `additionalProperties=false`, and `oneOf` for exactly one required handle.
  - [x] Keep runtime validation authoritative even after schema metadata is added; malformed input must still fail closed server-side.

- [x] Pin the actual MCP `tools/call` lifecycle route, not only direct tracker calls (AC: 1, 2)
  - [x] Add or extend MCP adapter/hosting tests that exercise the call handler path registered by `AddFrontComposerMcp(...).WithCallToolHandler(...)`.
  - [x] Prove exact, case-sensitive `frontcomposer.lifecycle.subscribe` routing resolves `IFrontComposerMcpAgentContextAccessor.GetContext()` before handle lookup.
  - [x] Prove missing/invalid agent context collapses to the existing opaque auth/tenant failure and does not reveal whether the handle exists.
  - [x] Prove non-lifecycle tool names still route to `FrontComposerMcpCommandInvoker` and Story 5.1 command tool behavior remains unchanged.

- [x] Harden and pin lifecycle handle validation (AC: 1, 2)
  - [x] Extend `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs`.
  - [x] Prove exactly one string argument is accepted: `correlationId` or `messageId`, never both, never neither, never null/number/bool/object/array.
  - [x] Prove accepted handles are uppercase canonical 26-character Crockford ULIDs, ASCII-only, no leading/trailing whitespace, no Unicode/confusable folding, no percent/path fragments, no lowercase, and no oversized values.
  - [x] Prove malformed and unknown handles return the shared hidden/unknown structured content from `FrontComposerMcpToolAdmissionService.BuildHiddenUnknownStructuredContent()`.
  - [x] Prove malformed/unknown/hidden reads do not call command dispatch, do not allocate new IDs, do not create lifecycle entries, and do not leak the submitted handle.

- [x] Confirm and pin lifecycle snapshot semantics (AC: 1, 3, 4)
  - [x] Reuse `FrontComposerMcpLifecycleTracker`, `McpLifecycleSnapshot`, `McpLifecycleTransitionDto`, and `McpTerminalOutcome`; do not build a parallel lifecycle store.
  - [x] Prove lookups work by both `correlationId` and `messageId` for the same entry and return the same current lifecycle state.
  - [x] Prove the first command acknowledgement returns `Acknowledged` and a lifecycle reference, while later lifecycle snapshots may report `Syncing`, `Confirmed`, `Rejected`, `timed_out`, `needs_review`, or `idempotent_confirmed`.
  - [x] Prove agent-visible history starts at `Acknowledged`, excludes `Idle`/`Submitting`, is ordered by monotonic sequence, is bounded by `MaxLifecycleTransitionHistory`, and sets `historyTruncated` when truncated.
  - [x] Prove terminal state is monotonic: late non-terminal observations, late opposite terminals, replayed rejected/confirmed observations, and parallel reads cannot change the first terminal outcome or grow history incorrectly.
  - [x] Prove snapshot output never includes command argument values, tenant IDs, user IDs, policy names, exception messages, stack traces, or raw domain rejection text.

- [x] Confirm and pin retry, timeout, and capacity behavior (AC: 3, 4)
  - [x] Prove `McpCommandAcknowledgement.Lifecycle.RetryAfterMs` and later snapshot retry guidance use the same clamped dispatcher-provided retry hint when present.
  - [x] Prove option fallback/clamping for `DefaultLifecycleRetryAfterMs`, `MinLifecycleRetryAfterMs`, `MaxLifecycleRetryAfterMs`, and `MaxLifecycleLongPollMs`.
  - [x] Use `FakeTimeProvider` for `MaxLifecycleInProgressMs`; no wall-clock sleeps.
  - [x] Prove timeout creates a synthetic terminal with `outcome.category = "timed_out"` and structured remediation payload.
  - [x] Prove active capacity eviction marks the oldest active entry as `needs_review`, retained-terminal capacity removes only terminal entries, and message-id lookup is removed with the evicted entry.

- [x] Preserve security and command-safety semantics from Stories 3.3-5.1 (AC: 1, 2, 3, 4)
  - [x] Keep Story 5.1 identity behavior: command `MessageId` and `CorrelationId` are server-issued ULIDs from `IUlidFactory`; lifecycle reads must not accept caller-supplied command identity outside the framework-issued handle.
  - [x] Revalidate the current visible catalog through `FrontComposerMcpToolAdmissionService.ResolveAsync(entry.Descriptor.ProtocolName, ...)` before returning a known lifecycle snapshot.
  - [x] Preserve `[RequiresPolicy]` visibility loss behavior: a handle that was valid when acknowledged becomes hidden/unknown if the tool is no longer visible for the current tenant/policy context.
  - [x] Preserve FC-CMD and FC-RETRY boundaries: no MCP-level retry, no EventStore status endpoint polling in the MCP adapter, no change to Shell pending-command polling budgets, and no direct state-store mutation.
  - [x] Preserve fail-closed MCP gate requirements from `AddFrontComposerMcp`; do not add allow-all defaults.

- [x] Verification
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
  - [x] If local VSTest socket restrictions block the exact solution test command, run focused in-process/fallback lanes for `Hexalith.FrontComposer.Mcp.Tests` and any changed SourceTools/Shell/Contracts tests; record the blocker and fallback evidence honestly.

## Dev Notes

### Brownfield Reality

- The lifecycle subscription surface already exists. Treat this as confirm-and-pin plus bounded hardening, not a rebuild.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs` already owns lifecycle entries, correlation/message lookup, handle normalization, policy revalidation, hidden/unknown output, timeout, active capacity, retained terminal capacity, and subscription disposal.
- `src/Hexalith.FrontComposer.Mcp/Invocation/McpLifecycleModels.cs` already defines `McpCommandAcknowledgement`, `McpLifecycleSubscription`, `McpLifecycleSnapshot`, bounded transition DTOs, and terminal outcomes.
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs` already adds the lifecycle tool to `tools/list` and routes exact lifecycle calls through `FrontComposerMcpLifecycleTracker.ReadAsync`.
- `src/Hexalith.FrontComposer.Mcp/McpJsonSchemaBuilder.cs` already emits a lifecycle one-of schema, but it currently does not advertise the ULID pattern or max-length constraint that runtime validation enforces.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs` already covers many lifecycle tracker cases directly: acknowledgement, known-handle snapshots, malformed handles, policy loss, timeout, needs-review capacity, idempotent confirmed, parallel reads, retry hint carryover, and monotonic terminal behavior.

### Must-Fix Risks

- The current lifecycle schema is under-specified for agents: `correlationId`/`messageId` have only string titles. Add schema `pattern` and `maxLength` while keeping runtime validation as the security boundary.
- Existing tests mostly call `FrontComposerMcpLifecycleTracker.ReadAsync` directly. Add coverage for the actual MCP `CallToolAsync` routing path so auth-context gating and exact tool-name routing cannot drift.
- The epic wording names `retryAfterMs` and `maxLongPollMs`; current JSON nests them under `retry`. Record the v1 wire contract before changing shape. Do not leave this ambiguous for dev or agent clients.
- Do not leak command args or tenant/user context from lifecycle snapshots. Story 5.1 already proved command acknowledgement redaction; lifecycle snapshots need the same evidence.

### Architecture Guardrails

- Dependency direction remains `Mcp -> Contracts + Schema`; do not add a dependency from `SourceTools` to `Mcp` or net10-only packages.
- Keep package versions centralized in `Directory.Packages.props`; do not add `Version=` to project files.
- `ModelContextProtocol.AspNetCore` is already pinned to `1.3.0`; NuGet lists `1.3.0` as the current package version checked during story creation on 2026-06-05. Do not upgrade packages in this story.
- Use `IUlidFactory` and canonical 26-character Crockford ULIDs only. No GUIDs, no `Activity.TraceId`, no client-provided identity fallback.
- Keep opaque hidden-equivalent failures shared with tool admission. Hidden lifecycle reads must not become a distinct "lifecycle not found" oracle.
- Use `TimeProvider`/`FakeTimeProvider` for timeout tests. No wall-clock sleeps or timing flakes.
- Do not hand-edit generated files under `obj/**/generated/HexalithFrontComposer/`.

### File Structure Notes

- Expected production source touch points:
  - `src/Hexalith.FrontComposer.Mcp/McpJsonSchemaBuilder.cs`
  - Possibly `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpProtocolMapper.cs` if lifecycle tool metadata needs to change.
  - Possibly `src/Hexalith.FrontComposer.Mcp/Invocation/McpLifecycleModels.cs` if the v1 snapshot wire shape is intentionally changed.
  - Possibly `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs` if tests expose a runtime validation, lookup, timeout, retention, or redaction gap.
  - Possibly `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs` if call-handler routing/auth coverage exposes a real gap.
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs`
  - Possibly `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpCommandToolAdapterTests.cs` or a new lifecycle adapter/hosting test file for actual MCP call-handler coverage.
- Expected artifact:
  - `_bmad-output/contracts/fc-mcp-lifecycle-subscription-contract-2026-06-05.md`

### Previous Story Intelligence

- Story 5.1 created `_bmad-output/contracts/fc-mcp-command-tool-invocation-contract-2026-06-05.md` and pinned FC-MCP-TOOLS v1. Story 5.2 must not reopen generated command tool admission/dispatch ordering except to prove lifecycle routing does not regress it.
- Story 5.1 removed GUID and `Activity.Current.TraceId` identity fallbacks. Preserve the server-issued ULID invariant and use counting/fixed ULID factories carefully in tests.
- Story 5.1 review verified MCP tests at `306/306` and found only documentation/file-list drift. Reconcile `git diff --name-only` against this story's File List before review promotion.
- Epic 4 and Story 5.1 both reinforced that MCP should not add its own retry loop. EventStore retry remains an adapter behavior below command dispatch; lifecycle subscribe only reports retry guidance.

### Git Intelligence

- Recent commits:
  - `c221237 feat(story-5.1): Expose generated commands as MCP tools`
  - `f696f21 docs: record epic 4 retrospective`
  - `1be0a05 feat(story-4.5): Retry and degraded state handling`
  - `db5e045 feat(story-4.4): Policy-gated command authorization`
  - `7f5d056 feat(story-4.3): One-at-a-time execution policy FC-CNC`
- Relevant pattern from recent work: contract artifact first, focused MCP/Shell tests next, production code changed only where pins expose drift, and final story record reconciled against actual changed files.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Epic 5 / Story 5.2]
- [Source: `_bmad-output/project-context.md` - MCP Server Rules, Identity, Testing Rules]
- [Source: `_bmad-output/project-docs/api-contracts.md` - MCP tool & resource surface]
- [Source: `_bmad-output/project-docs/architecture.md` - AI-agent surface (MCP)]
- [Source: `_bmad-output/project-docs/component-inventory.md` - Lifecycle and MCP services]
- [Source: `_bmad-output/contracts/fc-mcp-command-tool-invocation-contract-2026-06-05.md` - Story 5.1 command tool contract]
- [Source: `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md` - MessageId and CorrelationId]
- [Source: `_bmad-output/contracts/fc-cmd-command-budget-contract-2026-06-04.md` - command lifecycle/polling budgets]
- [Source: `_bmad-output/contracts/fc-cmd-retry-degraded-state-contract-2026-06-05.md` - no MCP retry policy]
- [Source: `_bmad-output/implementation-artifacts/5-1-expose-generated-commands-as-mcp-tools.md` - previous story intelligence]
- [Source: `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/Invocation/McpLifecycleModels.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/McpJsonSchemaBuilder.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpProtocolMapper.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`]
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs`]
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs`]
- [Source: NuGet Gallery, `ModelContextProtocol.AspNetCore` package page, checked 2026-06-05]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-05: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` -> passed, 0 warnings, 0 errors.
- 2026-06-05: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -> blocked by local MSBuild named-pipe/socket restriction (`System.Net.Sockets.SocketException (13): Permission denied`) before tests could run.
- 2026-06-05: `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests.dll -trait- Category=Performance -trait- Category=e2e-palette -trait- Category=NightlyProperty -trait- Category=Quarantined -parallel none -noLogo` -> passed, 321/321.
- 2026-06-05: Focused MCP fallback via xUnit in-process runner for `McpCommandToolAdapterTests`, `ToolAdmissionTests`, and `CommandLifecycleTests` -> passed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Created FC-MCP-LIFECYCLE v1 contract and explicitly blessed the existing nested retry guidance shape (`retry.retryAfterMs`, `retry.maxLongPollMs`) with no top-level compatibility fields.
- Added lifecycle input schema metadata for canonical ULID handles: string type, `maxLength: 64`, `pattern: ^[0-9A-HJKMNP-TV-Z]{26}$`, `additionalProperties=false`, and one-of required handle shape.
- Added MCP handler-level lifecycle pins for `tools/list` catalog ordering/schema metadata, non-lifecycle command routing, auth-context gating before lifecycle handle lookup, and nested retry snapshot output.
- Extended lifecycle runtime validation coverage for never-neither and never-null/number/bool/object/array handle inputs while preserving hidden/unknown redaction.
- Preserved the existing lifecycle tracker/model/store implementation and Story 5.1 server-issued ULID command identity behavior; no package, resource, projection, Shell polling, EventStore status, or MCP retry-loop changes were introduced.

### File List

- `_bmad-output/contracts/fc-mcp-lifecycle-subscription-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/5-2-lifecycle-subscription-tool.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.FrontComposer.Mcp/McpJsonSchemaBuilder.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpCommandToolAdapterTests.cs`

### Change Log

- 2026-06-05: Implemented Story 5.2 lifecycle subscription contract, schema hardening, MCP handler route pins, lifecycle handle validation pins, and verification record.
- 2026-06-05: Senior Developer Review (AI) — adversarial review, 0 Critical / 0 High. Auto-fixed 1 Medium (File List drift) + 2 Low (using-order, P42 redaction coverage). Status review → done.

## Senior Developer Review (AI)

Reviewer: Administrator — 2026-06-05

### Outcome

**Approved.** All four acceptance criteria are implemented and pinned by passing tests; every `[x]`
task was independently verified against live source. Adversarial review found 0 Critical and 0 High
issues — appropriate for a confirm-and-pin story whose only production delta is advisory schema
metadata. 1 Medium and 2 Low issues were found and auto-fixed.

### Verification reproduced independently

- `dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Release -m:1 /nr:false` → **0 warnings, 0 errors** (confirms the dev's "0/0" claim; `TreatWarningsAsErrors=true` is active repo-wide).
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/...csproj -c Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` → **323/323 passed** (322 pre-fix + 1 new redaction-hardening test). The exact-solution VSTest lane remains socket-blocked in this sandbox, as the dev recorded; the per-project Release lane runs clean and supersedes that blocker for MCP scope.

### AC trace

- **AC1** (snapshot shape): `McpLifecycleSnapshot.ToJson` emits `state`, `terminal`, `outcome`, ordered+bounded `transitions`, nested `retry.retryAfterMs`/`retry.maxLongPollMs`, `historyTruncated`. Pinned by `ReadAsync_KnownLifecycleHandle_*` and `CallToolHandler_LifecycleReadReturnsNestedRetrySnapshot`. ✔
- **AC2** (opaque hidden/unknown): malformed/unknown/oversized/tenant-loss/policy-loss/unauthenticated all route through `FrontComposerMcpToolAdmissionService.BuildHiddenUnknownStructuredContent()` with no handle/tenant/arg leakage. The new tenant-visibility-loss test confirms `ResolveAsync` → `BuildVisibleCatalogAsync` re-evaluates the tenant gate. ✔
- **AC3** (terminal idempotency / bounded history): first-wins terminal, same-state re-delivery does not grow history, late opposite terminals ignored. Pinned by the monotonic/replay/duplicate/parallel-read suite. ✔
- **AC4** (retry/timeout/capacity bounds, deterministic time): `ClampRetryAfter` + option clamping in `ToSnapshot`; `FakeTimeProvider`-driven timeout and `needs_review` eviction. ✔

### Findings & fixes

- **MEDIUM — File List drift:** `_bmad-output/implementation-artifacts/tests/test-summary.md` was updated by this story but omitted from the File List (same drift class the 5.1 review corrected). → Added to File List.
- **LOW — using-order:** `using System.Reflection;` was placed after `using System.Security.Claims;` in `McpCommandToolAdapterTests.cs`. Not build-enforced, but off-convention. → Reordered.
- **LOW — coverage gap:** the P42 catch-all in `CallToolAsync` (non-`FrontComposerMcpException` from `GetContext()` → sanitized `AuthFailed`) was untested. → Added `CallToolHandler_LifecycleRouteRedactsUnexpectedContextAccessorException`, proving an accessor that throws an `InvalidOperationException` carrying secret-looking text collapses to "Request failed." with no structured content and no message leakage.

### Accepted trade-offs (no change)

- The handler tests invoke private static `ListToolsAsync`/`CallToolAsync` via reflection over a hand-built DI graph; they pin routing logic (the real drift risk) but not the `AddFrontComposerMcp(...).WithCallToolHandler(...)` registration itself. Routing the test through a full MCP host build was judged higher-risk than its incremental value.
- `git` also shows `_bmad-output/story-automator/orchestration-1-20260604-140358.md` modified — automation log, correctly excluded from the File List.
- Schema `maxLength: 64` alongside the exact-26 `pattern` is intentional per the FC-MCP-LIFECYCLE v1 contract (mirrors the runtime `MaxIdentifierLength` early-reject bound); runtime validation stays authoritative. Left as-is.
