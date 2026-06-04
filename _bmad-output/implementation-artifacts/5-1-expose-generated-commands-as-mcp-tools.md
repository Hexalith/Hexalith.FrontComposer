---
baseline_commit: f696f2146db6f409d8c9b148d4430ed27789ad5e
---

# Story 5.1: Expose generated commands as MCP tools

Status: done

<!-- Note: Validation completed during create-story. -->
<!-- Note: Autonomous code review completed 2026-06-05 (story-automator-review); 0 critical issues. -->

## Story

As an AI agent,
I want each generated command available as an MCP tool,
so that I can invoke domain commands through the protocol.

## Acceptance Criteria

1. Given a generated `McpManifest`, when I call `tools/list`, then one tool per `McpCommandDescriptor` is built dynamically with its per-descriptor JSON schema. (FR16, FR4)
2. Given a `tools/call`, when the args pass admission -> schema negotiation -> validation, then the command instantiates, derivable values inject server-side, and dispatch returns an `McpCommandAcknowledgement`. (FR16)
3. Given server-controlled fields (`TenantId`/`UserId`/`MessageId`/`CorrelationId`) in tool input, when received, then they are blocked/ignored. (FR18)

## Tasks / Subtasks

- [x] Record the FC-MCP-TOOLS v1 command-tool invocation contract (AC: 1, 2, 3)
  - [x] Create `_bmad-output/contracts/fc-mcp-command-tool-invocation-contract-2026-06-05.md`.
  - [x] Record the invocation order: visible-tool admission -> schema negotiation -> primitive argument validation -> command construction -> non-derivable argument application -> server-side derivable injection -> DataAnnotations/current-contract validation -> `ICommandService.DispatchAsync<T>` -> lifecycle acknowledgement.
  - [x] State non-goals: no MCP retry loop, no queue/batch policy, no bypass of `[RequiresPolicy]`, no projection/resource work from Stories 5.2-5.5.

- [x] Confirm and pin generated command descriptor emission (AC: 1, 3)
  - [x] Extend `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/McpManifestEmitterTests.cs` and/or `tests/Hexalith.FrontComposer.Mcp.Tests/ManifestTransformTests.cs`.
  - [x] Prove each `[Command]` becomes exactly one `McpCommandDescriptor` with stable `ProtocolName`, `CommandTypeName`, `BoundedContext`, `Title`, `AuthorizationPolicyName`, per-command `Fingerprint`, non-derivable `Parameters`, and `DerivablePropertyNames`.
  - [x] Prove duplicate command base names use namespace disambiguation instead of overwriting descriptors.
  - [x] Prove server-controlled fields and unsupported properties are excluded from tool input schema.

- [x] Confirm and pin `tools/list` dynamic catalog behavior (AC: 1)
  - [x] Extend `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs` or add a focused list-mapping test.
  - [x] Prove `FrontComposerMcpToolAdmissionService.BuildVisibleCatalogAsync` materializes visible generated command tools from `FrontComposerMcpDescriptorRegistry.Commands` on each list operation, applying tenant and policy visibility gates.
  - [x] Prove `FrontComposerMcpProtocolMapper.ToProtocolTool` builds `Tool.InputSchema` via `McpJsonSchemaBuilder.BuildInputSchema(entry.Descriptor.Parameters)` and preserves `additionalProperties=false`.
  - [x] Keep the fixed lifecycle tool present in `tools/list`, but do not count it as a generated command tool for AC1.

- [x] Harden and pin side-effecting `tools/call` ordering (AC: 2, 3)
  - [x] Extend `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs`, `CommandInvokerCoverageTests.cs`, and `CommandInvokerSchemaGateTests.cs`.
  - [x] Prove exact-name resolution re-evaluates the current visible catalog; a previously visible policy/tenant-hidden tool must return hidden/unknown and must not dispatch.
  - [x] Prove stale/incompatible schema categories short-circuit before command construction, derivable injection, lifecycle tracking, and dispatch.
  - [x] Prove invalid primitive shapes, unknown arguments, duplicate case-variant keys, arrays/objects, oversized payloads, and unsupported parameters fail before command construction or dispatch.
  - [x] Prove DataAnnotations validation runs after derivable injection and before dispatch so `[Required]` server fields can pass only after framework injection.

- [x] Fix and pin MCP identity injection (AC: 2, 3)
  - [x] Update `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`.
  - [x] Remove the side-effecting `Guid.NewGuid().ToString("N")` fallback and the `Activity.Current.TraceId` command-correlation fallback from `ApplyDerivableValues`.
  - [x] Require `IUlidFactory` for command `MessageId` and `CorrelationId` injection. If it is missing, fail closed with `UnsupportedSchema` before dispatch.
  - [x] Generate both `MessageId` and `CorrelationId` as canonical 26-character Crockford ULIDs. Use separate `IUlidFactory.NewUlid()` calls unless an existing FC-CMD contract explicitly requires sameness; do not use trace IDs as command correlation IDs.
  - [x] Add a counting ULID-factory test proving exactly the intended allocation count and proving caller-supplied `TenantId`, `UserId`, `MessageId`, `CommandId`, and `CorrelationId` are rejected before any server-side identity allocation or dispatch.
  - [x] Assert the dispatched command receives the framework-issued tenant/user/message/correlation values and the `McpCommandAcknowledgement` carries the same accepted message/correlation handles.

- [x] Preserve command safety and failure semantics from Epics 3-4 (AC: 2, 3)
  - [x] Keep `[RequiresPolicy]` visibility/admission checks in `FrontComposerMcpToolAdmissionService`; do not add a new authorization framework.
  - [x] Keep `AuthorizingCommandServiceDecorator` as the direct-dispatch backstop when the host command service is the Shell/EventStore path.
  - [x] Do not add MCP-level retry. EventStore pre-accept retry remains an adapter behavior under `EventStoreCommandClient`.
  - [x] Preserve opaque hidden-equivalent failures for auth/tenant/policy-hidden/unknown-tool paths; do not leak tool names, tenant IDs, user IDs, policy names, exception messages, or raw payloads.

- [x] Verification
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
  - [x] If local VSTest socket restrictions block the exact solution test command, run focused in-process/fallback lanes for `Hexalith.FrontComposer.Mcp.Tests`, `Hexalith.FrontComposer.SourceTools.Tests`, and any changed Shell/Contracts tests; record the blocker and fallback evidence honestly.

## Dev Notes

### Brownfield Reality

- The MCP surface already exists. Do not rebuild it. Work should be confirm-and-pin plus bounded fixes.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs` already performs: tool admission, schema negotiation, argument validation, command construction, argument application, derivable injection, DataAnnotations validation, generic `CommandServiceExtensions.DispatchAsync<TCommand>`, and optional `FrontComposerMcpLifecycleTracker.TrackAcknowledged`.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs` already builds a visible catalog from the registry, applies tenant visibility, optional command-policy visibility, hidden-equivalent suggestions, and exact-name resolution.
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpProtocolMapper.cs` maps visible catalog entries to MCP SDK `Tool` instances and delegates input schema construction to `McpJsonSchemaBuilder`.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs` already emits `FrontComposerMcpManifest.g.cs`, one `McpCommandDescriptor` per command descriptor model, resources, and aggregate fingerprints.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/McpManifestTransform.cs` already maps non-derivable command properties to MCP parameters and derivable properties to `DerivablePropertyNames`.
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs` registers `FrontComposerMcpUlidFactory` as `IUlidFactory` in the normal `AddFrontComposerMcp` path and fails startup when tenant/resource gates are absent.

### Must-Fix Risk

- `FrontComposerMcpCommandInvoker.ApplyDerivableValues` currently falls back to `Guid.NewGuid().ToString("N")` when no `IUlidFactory` is registered and uses `Activity.Current.TraceId` as `CorrelationId` when an activity exists. That conflicts with FC-CMD: both `MessageId` and `CorrelationId` must be 26-character Crockford ULIDs. Fix this in Story 5.1, even if the standard host path already registers `FrontComposerMcpUlidFactory`.
- Existing invoker tests manually assemble services and often omit `IUlidFactory`; update test fixtures deliberately instead of preserving the fallback.

### Architecture Guardrails

- Dependency direction remains `Mcp -> Contracts + Schema`; do not add a dependency from `SourceTools` to `Mcp` or any net10-only package. SourceTools must stay netstandard2.0-clean.
- Keep versions centralized in `Directory.Packages.props`; do not add package `Version=` attributes to project files.
- `ModelContextProtocol.AspNetCore` is already pinned to `1.3.0` locally and NuGet lists `1.3.0` as the package version available as of 2026-06-05. Do not upgrade packages in this story.
- Server-controlled fields are never accepted from tool input: `TenantId`, `UserId`, `MessageId`, `CommandId`, and `CorrelationId`.
- Command tool input schemas must use `additionalProperties=false`; runtime validation must still reject unknown keys because schemas are advisory to clients, not a security boundary.
- Use ordinal/case-sensitive canonical names for tool execution. Case variants may be suggested, but they must not dispatch.
- Do not hand-edit generated files under `obj/**/generated/HexalithFrontComposer/`.

### Existing Tests To Reuse

- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerCoverageTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerSchemaGateTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionSchemaGateTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/McpManifestEmitterTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/ManifestTransformTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs`

### Project Structure Notes

- Expected production source touch points:
  - `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`
  - Possibly `src/Hexalith.FrontComposer.Mcp/McpJsonSchemaBuilder.cs`, `FrontComposerMcpProtocolMapper.cs`, or `FrontComposerMcpTool.cs` only if tests prove a schema/list gap.
  - Possibly `src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs` or `Transforms/McpManifestTransform.cs` only if descriptor emission pins expose a real gap.
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/*`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/McpManifestEmitterTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/ManifestTransformTests.cs`
- Expected artifact:
  - `_bmad-output/contracts/fc-mcp-command-tool-invocation-contract-2026-06-05.md`

### Previous Story Intelligence

- Epic 4 retrospective action E4-AI-4 explicitly requires Story 5.1 to name the MCP invocation order before development.
- Epic 4 retry policy is Shell/EventStore adapter behavior only. Do not introduce an agent-facing MCP retry policy in Story 5.1.
- Reuse the Story 4.5 test lesson: do not use a constant identity factory when proving identity invariants. Use a counting factory that fails if IDs are regenerated too often, not generated, or generated on rejected input.
- File List drift recurred in Epic 4. Reconcile `git diff --name-only` against this story's File List before review promotion.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Epic 5 / Story 5.1]
- [Source: `_bmad-output/project-context.md` - MCP Server Rules, Identity, Testing Rules]
- [Source: `_bmad-output/project-docs/architecture.md` - AI-agent surface (MCP), Runtime composition]
- [Source: `_bmad-output/project-docs/api-contracts.md` - MCP tool & resource surface, Runtime command safety contract]
- [Source: `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md` - MessageId and CorrelationId]
- [Source: `_bmad-output/contracts/fc-auth-policy-gated-command-authorization-2026-06-04.md` - FC-AUTH non-goals and dispatch backstop]
- [Source: `_bmad-output/contracts/fc-cmd-retry-degraded-state-contract-2026-06-05.md` - no MCP tool retry policy]
- [Source: `_bmad-output/implementation-artifacts/epic-4-retro-2026-06-05.md` - Next Epic Preparation]
- [Source: `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpProtocolMapper.cs`]
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs`]
- [Source: `src/Hexalith.FrontComposer.SourceTools/Transforms/McpManifestTransform.cs`]
- [Source: NuGet Gallery, `ModelContextProtocol.AspNetCore` package page, checked 2026-06-05]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-05: Created FC-MCP-TOOLS v1 contract artifact with the side-effecting command-tool invocation order, server-controlled identity handling, acknowledgement contract, opaque failure semantics, and non-goals.
- 2026-06-05: Added descriptor/list pins in `McpManifestEmitterTests`, `ManifestTransformTests`, and `ToolAdmissionTests`.
- 2026-06-05: Removed MCP command GUID and `Activity.Current.TraceId` identity fallbacks; command `MessageId` and `CorrelationId` now require separate canonical ULIDs from `IUlidFactory` and fail closed with `UnsupportedSchema` when unavailable or invalid.
- 2026-06-05: Required solution VSTest lane was attempted and blocked locally by `System.Net.Sockets.SocketException (13): Permission denied`; fallback evidence captured via xUnit v3 in-process runner.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- FC-MCP-TOOLS v1 contract recorded before implementation, including exact call order and non-goals.
- Generated command descriptor emission is pinned for one descriptor per command, stable metadata/fingerprint emission, namespace disambiguation for duplicate command base names, derivable server fields, and unsupported parameter handling.
- `tools/list` visible catalog mapping is pinned for generated command tools, policy visibility, per-descriptor input schema, `additionalProperties=false`, unsupported/server-controlled schema exclusion, and lifecycle tool separation.
- `tools/call` identity injection now requires `IUlidFactory`, allocates separate canonical ULIDs for `MessageId` and `CorrelationId`, rejects caller-supplied `TenantId`/`UserId`/`MessageId`/`CommandId`/`CorrelationId` before allocation/dispatch, and keeps policy/tenant-hidden failures opaque.
- Validation: Release build passed 0/0. Exact solution VSTest command is locally socket-blocked; fallback lanes passed MCP 299/299 and focused SourceTools emitter 2/2. Broad SourceTools fallback reproduced 3 known unrelated baseline failures.

### File List

- `_bmad-output/contracts/fc-mcp-command-tool-invocation-contract-2026-06-05.md` — added FC-MCP-TOOLS v1 command-tool invocation contract.
- `_bmad-output/implementation-artifacts/5-1-expose-generated-commands-as-mcp-tools.md` — updated story status, task checkboxes, Dev Agent Record, File List, and Change Log.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — updated story 5.1 status tracking.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs` — removed GUID/trace-ID identity fallbacks; require canonical ULIDs from `IUlidFactory`.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs` — added identity allocation, server-controlled rejection, missing factory, and acknowledgement handle pins.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerCoverageTests.cs` — updated manual MCP invoker fixtures with deterministic ULID factory.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerSchemaGateTests.cs` — updated schema-gate fixture with deterministic ULID factory.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs` — extended counting ULID fixture for separate message/correlation allocations across two commands.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs` — added visible catalog/protocol schema pin and deterministic ULID fixture.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionSpecGapTests.cs` — updated admission fixture with deterministic ULID factory.
- `tests/Hexalith.FrontComposer.Mcp.Tests/ManifestTransformTests.cs` — added descriptor metadata/derivable/unsupported parameter pin.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpCommandToolAdapterTests.cs` — added `FrontComposerMcpTool` adapter coverage: per-descriptor protocol schema with server-controlled fields hidden, end-to-end `tools/call` dispatch + acknowledgement, and server-controlled argument rejection before dispatch (7 cases).
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/McpManifestEmitterTests.cs` — added generated descriptor count, fingerprint, and duplicate-name namespace disambiguation pin.
- `_bmad-output/implementation-artifacts/tests/5-1-test-summary.md` — recorded Story 5.1 MCP adapter test-automation summary and validation evidence.

### Senior Developer Review (AI)

- Reviewer: Administrator — 2026-06-05 (autonomous story-automator-review, adversarial mode).
- Outcome: Approved → Status `done`. 0 Critical, 0 High issues. 1 Medium + 2 Low, all documentation-only and auto-fixed.
- Re-verified independently:
  - `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` → `0 Warning(s), 0 Error(s)`.
  - `Hexalith.FrontComposer.Mcp.Tests` (filtered) → `Passed: 306, Failed: 0`.
  - `McpManifestEmitterTests` + `ManifestTransformTests` (SourceTools) → `Passed: 2, Failed: 0`.
  - AC1/AC2/AC3 each traced to a passing pin; the GUID + `Activity.Current.TraceId` identity fallbacks are removed and identity now fails closed (`UnsupportedSchema`) when `IUlidFactory` is absent or returns a non-canonical ULID.
- Findings auto-fixed (no production code change required):
  - [Medium] File List omitted the new `McpCommandToolAdapterTests.cs` (git-tracked, 7 cases) — added to File List, reconciling against `git diff --name-only` per the Epic-4 drift lesson.
  - [Low] Completion Notes cited a stale `MCP 299/299`; the suite is now `306/306` after the +7 adapter cases — corrected below.
  - [Low] `tests/5-1-test-summary.md` story artifact was undocumented — added to File List.
- Informational (not changed): `FrontComposerMcpCommandInvoker.IsCanonicalUlid` does not constrain the position-0 timestamp character to `0–7`. The ULID source is the trusted server `FrontComposerMcpUlidFactory` and the FC-MCP-TOOLS contract only requires a canonical 26-char Crockford ULID, so no change was made.

### Change Log

- 2026-06-05: Implemented Story 5.1 and moved to review. Added FC-MCP-TOOLS v1 contract, descriptor/list/call pins, MCP command ULID identity hardening, and focused fallback validation evidence.
- 2026-06-05: Autonomous code review (story-automator-review). Re-verified build 0/0 and MCP `306/306` (corrects the earlier `299/299` note) plus SourceTools emitter/transform `2/2`. Reconciled File List drift (`McpCommandToolAdapterTests.cs`, `5-1-test-summary.md`). 0 critical issues → Status `done`.
