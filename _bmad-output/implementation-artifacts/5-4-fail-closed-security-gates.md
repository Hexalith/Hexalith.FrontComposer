---
baseline_commit: f1c69bd6f6ea6c320b137fee12d4c36dc83e2643
---

# Story 5.4: Fail-closed security gates

Status: done

<!-- Note: Validation completed during create-story. -->

## Story

As a platform owner,
I want the MCP server to fail closed,
so that missing gates or auth failures never leak the domain surface.

## Acceptance Criteria

1. Given startup without `IFrontComposerMcpTenantToolGate`, when `AddFrontComposerMcp(...)` is called, then registration throws before the MCP server can be mapped or used, and no default tenant allow-all gate is registered implicitly. (FR18)
2. Given startup without `IFrontComposerMcpResourceVisibilityGate`, when `AddFrontComposerMcp(...)` is called, then registration throws before projection resources can be served, and no default resource allow-all gate is registered implicitly. (FR18)
3. Given auth, tenant, policy-hidden, tenant-hidden, unknown-tool, malformed lifecycle handle, unknown lifecycle handle, hidden lifecycle handle, unknown projection resource, auth/tenant-hidden projection resource, or unknown skill resource failures, when they reach the MCP protocol surface, then the public response uses the single opaque hidden/unknown shape for that surface and does not leak tenant IDs, user IDs, policy names, tool/resource existence, handles, raw URIs with query/fragment data, JWT/API-key-looking values, descriptor internals, exception messages, stack traces, command args, or raw payloads. (FR18)
4. Given `tools/list` runs without a valid authenticated tenant context or catalog admission fails with `AuthFailed`/`TenantMissing`, when the MCP SDK list handler is invoked, then the result is a successful empty tool list and not a protocol error; given a valid context where tenant/policy gates hide every generated command, then the result may contain only the fixed lifecycle tool and must not expose hidden command descriptors. (FR18)
5. Given sample, test, or dev hosts intentionally want permissive behavior, when they configure MCP, then they register `AllowAllMcpTenantToolGate` and `AllowAllResourceVisibilityGate` explicitly before `AddFrontComposerMcp(...)`; production extension methods must not add permissive defaults. (FR18)

## Tasks / Subtasks

- [x] Record the FC-MCP-SECURITY v1 fail-closed security contract (AC: 1, 2, 3, 4, 5)
  - [x] Create `_bmad-output/contracts/fc-mcp-fail-closed-security-contract-2026-06-05.md`.
  - [x] Define mandatory startup gates, explicit sample/dev allow-all registrations, opaque public failure shapes by surface, `tools/list` empty-list behavior, sanitized logging requirements, and non-goals.
  - [x] Record that skill resources are framework-global and bypass `IFrontComposerMcpResourceVisibilityGate` by design, while projection resources remain tenant-scoped and gate-checked.
  - [x] Record non-goals: no new authorization framework, no schema negotiation redesign, no command dispatch ordering changes, no resource URI grammar changes, no lifecycle wire-shape changes, no package upgrades.

- [x] Pin startup fail-closed gate requirements (AC: 1, 2, 5)
  - [x] Extend `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs`.
  - [x] Preserve the existing `AddFrontComposerMcp_FailsClosed_WhenTenantGateNotRegistered` behavior.
  - [x] Add the missing companion pin for `AddFrontComposerMcp(...)` without `IFrontComposerMcpResourceVisibilityGate`.
  - [x] Prove both thrown messages name the missing interface and instruct explicit host registration rather than silently registering permissive defaults.
  - [x] Prove `IFrontComposerMcpTenantToolGate` and `IFrontComposerMcpResourceVisibilityGate` are not registered by `AddFrontComposerMcp(...)` itself when absent.
  - [x] Prove sample/dev hosts that use `AllowAllMcpTenantToolGate` and `AllowAllResourceVisibilityGate` register them explicitly before `AddFrontComposerMcp(...)`.

- [x] Pin `tools/list` fail-closed behavior at the actual MCP SDK handler edge (AC: 3, 4)
  - [x] Extend `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpCommandToolAdapterTests.cs` or add a focused handler test around the private list handler registered by `AddFrontComposerMcp(...).WithListToolsHandler(...)`.
  - [x] Prove `request.Services == null` returns `ListToolsResult { Tools = [] }`.
  - [x] Prove `IFrontComposerMcpAgentContextAccessor.GetContext()` throwing `AuthFailed`, `TenantMissing`, or an unexpected exception results in an empty list without exposing the exception text.
  - [x] Prove tenant gate denial and policy gate denial remove hidden tools from the visible catalog, and a denied catalog returns only the fixed lifecycle tool if context is valid, or an empty list if context is invalid.
  - [x] Prove a tenant gate exception is sanitized in logs and treated as not visible, not as a protocol error.

- [x] Pin command `tools/call` hidden-equivalent failures (AC: 3)
  - [x] Extend `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerCoverageTests.cs`, `ToolAdmissionTests.cs`, or `ToolAdmissionSpecGapTests.cs`.
  - [x] Prove canonical calls to tenant-hidden or policy-hidden generated tools return the same public unknown-tool envelope as absent tools.
  - [x] Prove the unknown-tool structured content never includes hidden tool names, authorization policy names, tenant/user IDs, descriptor descriptions containing context-sensitive text, raw argument values, or exception messages.
  - [x] Preserve Story 5.1 ordering: hidden/unknown/schema failures must short-circuit before command construction, derivable identity allocation, lifecycle tracking, and dispatch.

- [x] Pin lifecycle hidden/unknown redaction at direct and handler paths (AC: 3)
  - [x] Extend `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs` and `McpCommandToolAdapterTests.cs`.
  - [x] Prove malformed, unknown, auth-failed, tenant-missing, tenant-hidden, policy-hidden, stale-after-visibility-loss, and unexpected context-accessor exceptions collapse to `FrontComposerMcpToolAdmissionService.BuildHiddenUnknownStructuredContent()` or generic `Request failed.` at the protocol edge as appropriate.
  - [x] Prove lifecycle failures do not include the submitted `messageId`/`correlationId`, command args, tenant/user values, policy names, descriptor names, exception text, or stack traces.
  - [x] Preserve Story 5.2 nested retry snapshot shape for successful reads.

- [x] Pin projection and skill resource hidden-equivalent failures (AC: 3)
  - [x] Extend `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTaxonomyTests.cs`, `ProjectionResourceAdapterTests.cs`, and `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs`.
  - [x] Prove `AuthFailed`, `TenantMissing`, `PolicyFiltered`, visibility loss, and `UnknownResource` projection reads share the public `unknown_resource` payload and do not leak raw URIs, query fragments, tenant/user IDs, JWT-like values, query exception text, or descriptor internals.
  - [x] Prove `FrontComposerMcpResource.ReadAsync(...)` maps those failures to sanitized MCP text/resource content at the SDK adapter edge.
  - [x] Prove skill `UnknownResource` and auth-equivalent failures keep the stable public `unknown_resource` token, while skill resources still do not resolve or call `IFrontComposerMcpResourceVisibilityGate`.
  - [x] Preserve Story 5.3 projection visibility admission, pre-query, and pre-render revalidation.

- [x] Preserve prior MCP command, lifecycle, resource, and schema boundaries (AC: 1, 2, 3, 4, 5)
  - [x] Do not add allow-all gate defaults to `AddFrontComposerMcp(...)`; samples/tests must register allow-all gates explicitly.
  - [x] Do not change `CanonicalSchemaMaterial`, schema fingerprint canonicalization, `McpSchemaNegotiator`, package versions, or `Directory.Packages.props`.
  - [x] Do not change Story 5.1 command identity injection, server-controlled input rejection, or MCP invocation order.
  - [x] Do not change Story 5.2 lifecycle model JSON shape or add streaming/long-poll server behavior.
  - [x] Do not change Story 5.3 resource URI grammar, skill-corpus parser, resource bounds, or skill/projection security split unless a pin exposes direct drift.

- [x] Verification
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
  - [x] If local VSTest/MSBuild socket restrictions block the exact solution command, run focused in-process/fallback lanes for `Hexalith.FrontComposer.Mcp.Tests`, especially Hosting, Invocation, Projection, Skills, and Schema tests; record the blocker and fallback evidence honestly.
  - [x] Reconcile `git diff --name-only` against this story's File List before review promotion.

## Dev Notes

### Brownfield Reality

- The fail-closed behavior mostly exists. Treat this as confirm-and-pin plus bounded hardening, not a rebuild.
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs` already checks the service collection for both mandatory gates before building the probe provider. It does not `TryAdd` either gate.
- `src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpTenantToolGate.cs` and `src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpResourceVisibilityGate.cs` both include explicit `AllowAll*` implementations for sample/dev hosts. These are not defaults.
- `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs` already pins missing tenant-gate startup failure, duplicate command rejection, projection/skill resource registration, and reserved skill URI rejection. It does not yet appear to have the symmetric missing resource-gate startup test.
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs` already returns an empty list from `ListToolsAsync` when `request.Services` is missing or `BuildVisibleCatalogAsync` throws a `FrontComposerMcpException`.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs` already centralizes visible catalog construction, tenant/policy gate checks, context validation, and the shared hidden/unknown structured content.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionFailureMapper.cs` already collapses hidden-equivalent projection failures to the public `unknown_resource` payload while keeping internal categories for host telemetry.
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs` already maps unknown/auth-equivalent skill reads to stable public tokens and intentionally bypasses resource visibility gates for framework-global skill docs.

### Current Source Paths To Preserve Or Pin

- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`
  - Current state: registers MCP services, probes mandatory gate registrations by `ServiceDescriptor`, materializes static SDK tools/resources, maps list/call handlers, returns empty list for `FrontComposerMcpException` in `tools/list`, gates lifecycle context before handle lookup, and collapses unexpected lifecycle context exceptions to `AuthFailed`.
  - This story changes: add missing pins and only adjust production behavior if tests expose a leak or default registration path.
  - Preserve: no default gate registration, explicit allow-all guidance, resource collision checks, lifecycle exact-name routing, and `ConfigureAwait(false)`.

- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs`
  - Current state: resolves authenticated tenant context, filters generated command descriptors through tenant and optional policy gates, hides context-sensitive descriptor text, builds unknown-tool suggestions only from visible tools, and treats gate exceptions as not visible with sanitized log context.
  - This story changes: pin hidden-equivalent behavior across auth/tenant/unknown/gate exception cases.
  - Preserve: visible-only suggestions, no hidden descriptor names, no policy names, no tenant/user IDs, and no dispatch on hidden/unknown.

- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`
  - Current state: resolves visible tool catalog before schema negotiation, argument validation, command construction, derivable injection, validation, dispatch, and lifecycle acknowledgement.
  - This story changes: likely tests only. Do not modify unless pins expose a hidden-equivalent leak.
  - Preserve: Story 5.1 server-issued ULID identity and short-circuit ordering.

- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs`
  - Current state: returns hidden/unknown structured content for malformed, unknown, hidden, and stale lifecycle reads while preserving successful nested retry snapshots.
  - This story changes: likely redaction pins only.
  - Preserve: Story 5.2 handle grammar, first-wins terminal semantics, bounded history, and nested `retry` shape.

- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs` and `FrontComposerMcpProjectionFailureMapper.cs`
  - Current state: projection reads require agent context and resource visibility, revalidate visibility, evaluate schema, query tenant-scoped data, render Markdown, and collapse hidden-equivalent public failures.
  - This story changes: likely pins only.
  - Preserve: no query before auth/visibility/schema gates, no partial render after visibility loss, no raw URI/query/exception leakage, and Story 5.3 resource grammar.

- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs`
  - Current state: skill resources are embedded framework docs, serve only `agent-reference`, expose stable public tokens, and bypass projection visibility gates.
  - This story changes: likely pins only.
  - Preserve: no tenant data, no resource visibility gate call, exact lowercase skill URIs, 32 KB no-truncation cap.

### Must-Fix Risks

- The existing startup test covers missing tenant gate; add the symmetric missing resource visibility gate test so the FR18 two-gate contract cannot regress.
- Do not weaken fail-closed startup by registering `AllowAllMcpTenantToolGate` or `AllowAllResourceVisibilityGate` inside `AddFrontComposerMcp(...)`. Explicit sample/dev host registration is the contract.
- `tools/list` has different expected behavior from `tools/call`: invalid auth/tenant context returns a successful empty list, while side-effecting calls return sanitized failures. Keep that distinction.
- Unknown-tool suggestions can become an oracle if they include hidden descriptors. Suggestions and visible lists must be built only from the current visible catalog.
- Lifecycle handle failures must not echo the handle. A valid-looking ULID is sensitive because it can reveal command existence or timing.
- Projection resource failures must not echo raw URI query/fragment values. Raw resource URIs can contain attacker-supplied secrets even when the descriptor URI is safe.
- Skill resources intentionally bypass `IFrontComposerMcpResourceVisibilityGate`; do not "fix" that by applying projection visibility to framework docs. Host-wide transport auth is the place to hide skill docs if a product needs that.

### Architecture Guardrails

- Dependency direction remains `Mcp -> Contracts + Schema`; do not add dependencies from `Contracts` or `SourceTools` to MCP or net10-only packages.
- Keep package versions centralized in `Directory.Packages.props`; do not add `Version=` to project files.
- `ModelContextProtocol.AspNetCore` is pinned to `1.3.0`; NuGet still lists `1.3.0` as the current package version checked during story creation on 2026-06-05. Do not upgrade packages in this story.
- Use xUnit v3, Shouldly, NSubstitute, and existing MCP test helpers. Do not introduce a new test framework.
- Use `ConfigureAwait(false)` on awaited production code. `TreatWarningsAsErrors=true` means analyzer/style warnings break Release builds.
- Do not hand-edit generated files under `obj/**/generated/HexalithFrontComposer/`.
- Do not change `CanonicalSchemaMaterial` encoder, sentinel, comparer, or canonical serialization.

### Project Structure Notes

- Expected production source touch points only if tests expose drift:
  - `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`
  - `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs`
  - `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`
  - `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs`
  - `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`
  - `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionFailureMapper.cs`
  - `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs`
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpCommandToolAdapterTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionSpecGapTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerCoverageTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTaxonomyTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionResourceAdapterTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs`
- Expected artifact:
  - `_bmad-output/contracts/fc-mcp-fail-closed-security-contract-2026-06-05.md`

### Previous Story Intelligence

- Story 5.1 recorded FC-MCP-TOOLS v1 and proved generated command `tools/list`/`tools/call` ordering, server-issued ULID identity, server-controlled input rejection, visible-catalog admission, and no MCP retry loop. Preserve all of it.
- Story 5.2 recorded FC-MCP-LIFECYCLE v1, blessed the nested retry wire shape, added handler-level route pins, and hardened context-gate redaction. Preserve the lifecycle shape and route ordering.
- Story 5.3 recorded FC-MCP-RESOURCES v1, resolved the actual projection URI grammar to `frontcomposer://<bounded-context>/projections/<projection-name>`, pinned projection resource adapter reads, reserved `frontcomposer://skills/`, and confirmed skill docs bypass projection visibility by design. Preserve the resource grammar and split.
- Recent reviews repeatedly found story File List drift. Reconcile actual changed files before moving to review.

### Git Intelligence

- Recent commits:
  - `f1c69bd feat(story-5.3): Projection and skill-corpus resources`
  - `e01697d feat(story-5.2): Lifecycle subscription tool`
  - `c221237 feat(story-5.1): Expose generated commands as MCP tools`
  - `f696f21 docs: record epic 4 retrospective`
  - `1be0a05 feat(story-4.5): Retry and degraded state handling`
- Relevant implementation pattern: contract artifact first, focused MCP tests next, production code changed only where pins expose drift, final story record reconciled against `git diff --name-only`.

### Latest Technical Information

- `ModelContextProtocol.AspNetCore` remains at `1.3.0` on NuGet as checked during story creation on 2026-06-05. The repo already pins this version, and this story should not upgrade or change MCP SDK package versions.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Epic 5 / Story 5.4]
- [Source: `_bmad-output/planning-artifacts/epics.md` - FR18]
- [Source: `_bmad-output/project-context.md` - MCP Server Rules, Testing Rules, Schema Fingerprint & Integrity Rules]
- [Source: `_bmad-output/project-docs/architecture.md` - AI-agent surface (MCP)]
- [Source: `_bmad-output/project-docs/api-contracts.md` - MCP tool & resource surface / Request flow & security]
- [Source: `_bmad-output/project-docs/component-inventory.md` - MCP server surface]
- [Source: `_bmad-output/contracts/fc-mcp-command-tool-invocation-contract-2026-06-05.md` - Story 5.1 command tool contract]
- [Source: `_bmad-output/contracts/fc-mcp-lifecycle-subscription-contract-2026-06-05.md` - Story 5.2 lifecycle contract]
- [Source: `_bmad-output/contracts/fc-mcp-resources-contract-2026-06-05.md` - Story 5.3 resource contract]
- [Source: `_bmad-output/implementation-artifacts/5-1-expose-generated-commands-as-mcp-tools.md` - previous story intelligence]
- [Source: `_bmad-output/implementation-artifacts/5-2-lifecycle-subscription-tool.md` - previous story intelligence]
- [Source: `_bmad-output/implementation-artifacts/5-3-projection-and-skill-corpus-resources.md` - previous story intelligence]
- [Source: `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpTenantToolGate.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpResourceVisibilityGate.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionFailureMapper.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs`]
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs`]
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs`]
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpCommandToolAdapterTests.cs`]
- [Source: NuGet Gallery, `ModelContextProtocol.AspNetCore` package page, checked 2026-06-05]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Release --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests -noLogo -noColor -parallel none` - passed, 350/350.
- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` - blocked by local MSBuild named-pipe/socket permission restriction (`System.Net.Sockets.SocketException (13): Permission denied`) before tests could run.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Recorded FC-MCP-SECURITY v1 fail-closed contract covering mandatory gates, explicit allow-all host registration, opaque public failure shapes, `tools/list` empty-list behavior, sanitized logs, projection-vs-skill resource split, and non-goals.
- Added startup pins for both mandatory gates and explicit sample/dev allow-all registration.
- Hardened `tools/list` so unexpected context-accessor failures return an empty tool list instead of surfacing protocol errors.
- Hardened command admission so canonical hidden generated tools do not echo hidden descriptor names through `requestedToolName`.
- Removed exception objects from tenant/policy gate warning logs for hidden-equivalent admission failures, retaining bounded diagnostic context only.
- Added handler/resource/skill pins for empty-list, hidden-equivalent, lifecycle, projection adapter, and skill visibility-bypass behavior.
- Review fix (2026-06-05): removed the tool-existence oracle introduced by conditional `requestedToolName` echoing; the unknown-tool envelope is now identical for tenant-hidden, policy-hidden, and absent tools.

### File List

- `_bmad-output/contracts/fc-mcp-fail-closed-security-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/5-4-fail-closed-security-gates.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs`
- `src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpCommandToolAdapterTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionResourceAdapterTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs`
- `tests/e2e/specs/mcp-fail-closed-security.spec.ts`
- `tests/e2e/helpers/mcp-client.ts`
- `tests/e2e/package.json`

### Change Log

- 2026-06-05: Recorded FC-MCP-SECURITY v1 and pinned fail-closed startup, list/call, lifecycle, projection, and skill security behavior. Hardened unexpected `tools/list` context failures, hidden-tool echo suppression, and tenant/policy gate log redaction. Status moved to review after Release build and MCP fallback tests passed.
- 2026-06-05: Senior Developer Review (story-automator-review). Auto-fixed 1 High and 1 Medium. **High (security):** the `RequestedToolIsKnownButHidden` flag omitted `requestedToolName` only for real-but-hidden tools while still echoing it for absent tools, so the field's presence/absence was a tool-existence oracle — a direct AC3/FR18 violation and contrary to the FC-MCP-SECURITY contract's "same public unknown-tool envelope ... as absent tools". Fix: the unknown-tool envelope never echoes `requestedToolName`; reverted the `RequestedToolIsKnownButHidden` plumbing on `McpToolResolutionResult`; added anti-oracle parity test `HiddenTool_AndAbsentTool_ProduceStructurallyIdenticalUnknownEnvelope`; updated `UnknownTool_ReturnsVisibleSuggestion`/`UnknownTool_SanitizesControlCharacters` unit pins and the e2e absent-tool assertion accordingly. **Medium:** File List drift — added the three story-delivered e2e files. Re-verified Release build 0/0 and MCP in-process 351/351. Status moved to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (story-automator-review) · **Date:** 2026-06-05 · **Outcome:** Approved after auto-fix

### Summary

Adversarial review of Story 5.4 against its five fail-closed ACs, the FC-MCP-SECURITY v1 contract, and the actual implementation. AC1/AC2 (mandatory two-gate startup) and AC5 (explicit sample/dev allow-all) are correctly implemented and now symmetrically pinned in `HostingTests`. AC4 (`tools/list` empty-list vs lifecycle-only) is pinned at the real SDK handler edge. AC3 (single opaque hidden/unknown shape with no existence leak) was **violated** by this story's own hardening and has been fixed.

### Findings

| Sev | Finding | Resolution |
| --- | --- | --- |
| High | **Tool-existence oracle.** `BuildUnknownToolStructuredContent` omitted `requestedToolName` only when the requested name matched a real-but-hidden command (`RequestedToolIsKnownButHidden`), while still echoing it for genuinely-absent names. Presence/absence of the field therefore distinguished hidden tools from non-existent ones over both the in-process and HTTP transports — violating AC3 ("does not leak ... tool/resource existence"), FR18, and the contract requirement that hidden/unknown/absent paths share one envelope. | Fixed. Envelope never echoes `requestedToolName`; flag plumbing reverted; parity test added; unit + e2e pins updated. |
| Medium | **File List drift.** `tests/e2e/specs/mcp-fail-closed-security.spec.ts`, `tests/e2e/helpers/mcp-client.ts`, and `tests/e2e/package.json` were changed but undocumented, despite the story's own Verification task requiring `git diff --name-only` reconciliation. | Fixed. Added to File List. |
| Low | **Fragile assertion.** `CanonicalPolicyHiddenTool_...` uses `json.ShouldNotContain("42")` (bare numeric substring). Safe (errs strict) but brittle. | Noted; not changed to avoid churning a security pin. |

### Verification

- `dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/...csproj -c Release` — 0 warnings, 0 errors.
- MCP in-process lane (`-parallel none`) — 351/351 passing (was 350; +1 anti-oracle parity test).
- Exact solution-level VSTest command remains blocked by the documented local MSBuild named-pipe/socket restriction; in-process fallback used per the story's recorded procedure.
