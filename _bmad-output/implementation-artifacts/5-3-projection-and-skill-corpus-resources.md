---
baseline_commit: e01697d
---

# Story 5.3: Projection and skill-corpus resources

Status: done

<!-- Note: Validation completed during create-story. -->

## Story

As an AI agent,
I want projections and skill docs as MCP resources,
so that I can read tenant data and framework reference material.

## Acceptance Criteria

1. Given a projection resource URI `frontcomposer://<context>/<projection>`, when the MCP resource is read, then the response is tenant-scoped Markdown rendered by `McpMarkdownProjectionRenderer` and the query uses only the authenticated agent context tenant. (FR17, FR18)
2. Given a projection resource is malformed, unknown, tenant-hidden, auth/tenant-invalid, stale after descriptor or visibility changes, schema-incompatible, canceled, timed out, unsupported, or too large, when it is read, then it returns the existing sanitized MCP projection failure taxonomy without leaking tenant IDs, user IDs, raw URIs, JWT fragments, query exceptions, descriptor internals, or raw payloads. (FR17, FR18, FR19)
3. Given a skill resource `frontcomposer://skills/<id>`, when it is read, then only the conforming document's `agent-reference` section is served as `text/markdown`, within the 32 KB cap; oversized resources return `SkillResourceTooLarge`/`response_too_large` and are not truncated. (FR17)
4. Given the skill corpus resource set, when resources are listed or read, then per-skill resources plus `frontcomposer://skills/manifest` are exposed from the embedded `docs/skills/frontcomposer/**/*.md` corpus, use canonical lowercase exact URIs, include schema fingerprints for per-skill resources, and bypass the projection visibility gate by design as framework-global reference material. (FR17)
5. Given projection and skill resource URIs share the MCP server, when `AddFrontComposerMcp(...)` registers resources, then projection URI collisions with `frontcomposer://skills/*` are rejected at startup and the fail-closed tenant/resource gate requirements remain intact for tenant-scoped projection reads. (FR18)

## Tasks / Subtasks

- [x] Record the FC-MCP-RESOURCES v1 resource contract (AC: 1, 2, 3, 4, 5)
  - [x] Create `_bmad-output/contracts/fc-mcp-resources-contract-2026-06-05.md`.
  - [x] Define projection resource URI grammar, skill resource URI grammar, `frontcomposer://skills/manifest`, content types, failure tokens, response size bounds, and the skill/projection security split.
  - [x] Resolve the URI grammar ambiguity explicitly: the epic shorthand says `frontcomposer://<context>/<projection>`, while live descriptors/tests use paths like `frontcomposer://Billing/projections/InvoiceProjection`. Record the actual v1 grammar before changing any URI behavior.
  - [x] Record binding decisions: projection resources are tenant-scoped and gate-checked; skill resources are framework-global and bypass `IFrontComposerMcpResourceVisibilityGate`; no URI templates in v1; no streaming resource reads; no package upgrades.
  - [x] Record non-goals: no command tool changes, no lifecycle tool changes, no schema-fingerprint negotiation redesign, no new authorization framework, no docs corpus authoring beyond tests if a gap is found.

- [x] Confirm and pin MCP resource registration at the SDK edge (AC: 4, 5)
  - [x] Extend `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs`, or add a focused resource-hosting test.
  - [x] Prove `AddFrontComposerMcp(...)` registers both manifest projection resources (`FrontComposerMcpResource`) and skill resources (`FrontComposerSkillMcpResource`) via `.WithResources(...)`.
  - [x] Prove each resource advertises `text/markdown` on `ProtocolResource`, carries its descriptor in `Metadata`, matches only exact canonical URI strings, and throws `NotSupportedException` for resource templates.
  - [x] Prove `frontcomposer://skills/manifest` is listed with the per-skill resources.
  - [x] Prove a manifest projection descriptor whose `ProtocolUri` collides with any skill URI, including `frontcomposer://skills/manifest`, fails startup with the existing collision message.

- [x] Confirm and pin projection resource reads through the actual MCP resource adapter (AC: 1, 2)
  - [x] Add or extend tests that call `FrontComposerMcpResource.ReadAsync(...)`, not only `FrontComposerMcpProjectionReader.ReadAsync(...)`.
  - [x] Prove a valid `ReadResourceRequestParams.Uri` routes through DI to `FrontComposerMcpProjectionReader`, queries `IQueryService.QueryAsync<T>` with `QueryRequest.TenantId` from `IFrontComposerMcpAgentContextAccessor.GetContext()`, and returns `TextResourceContents` with the canonical descriptor URI and `text/markdown`.
  - [x] Prove missing `request.Services`, blank/missing URI, malformed URI, and unknown resource do not throw raw exceptions and return sanitized text/plain failure content.
  - [x] Prove no query is executed before auth context and resource visibility succeed.
  - [x] Prove projection failures do not echo attacker-controlled URI query/fragment data, tenant/user IDs, JWT-like values, exception messages, or raw payloads.

- [x] Confirm and pin projection security, schema, and revalidation semantics (AC: 1, 2, 5)
  - [x] Extend `ProjectionReaderSchemaGateTests.cs`, `ProjectionReaderTaxonomyTests.cs`, or add a focused resource-read security test.
  - [x] Prove `IFrontComposerMcpResourceVisibilityGate` is resolved once per read and used at admission, pre-query, and pre-render revalidation.
  - [x] Prove visibility loss before query and between query/render returns the hidden-equivalent projection failure and does not render partial Markdown.
  - [x] Prove descriptor/catalog epoch drift before lookup and after lookup returns `StaleDescriptor`.
  - [x] Prove `SchemaNegotiationRuntimeGate.EvaluateResource(...)` blocks incompatible reads before `IQueryService` and does not duplicate structured-log/schema decisions during later visibility revalidation.
  - [x] Preserve Story 5.4 fail-closed gate scope: do not add allow-all defaults; sample/dev hosts must register `AllowAllResourceVisibilityGate` explicitly.

- [x] Confirm and pin `McpMarkdownProjectionRenderer` output contracts (AC: 1, 2)
  - [x] Extend `tests/Hexalith.FrontComposer.Mcp.Tests/Rendering/McpMarkdownProjectionRendererTests.cs`.
  - [x] Prove `Default`, `ActionQueue`, and `DetailRecord` render bounded Markdown tables; `StatusOverview` groups by semantic badge slot; `Timeline` sorts newest first with stable null tail; unsupported/dashboard strategies return `UnsupportedRender`.
  - [x] Prove row, field, timeline-entry, status-group, cell, and document character budgets use `FrontComposerMcpOptions` and emit the configured truncation marker only when the result is still safe and complete enough to serve.
  - [x] Prove document-budget failure returns `ResponseTooLarge` with no partial document when the marker cannot fit.
  - [x] Prove Markdown escaping/redaction covers descriptor titles, entity labels, field titles, cell values, pipes, backticks, links, checkbox syntax, JWT/API-key/client-secret-looking values, unsupported fields, and empty-state command suggestions.
  - [x] Prove empty-state suggestions are built only from the visible tool catalog, match bounded context and terminal command/protocol segment exactly, are capped by `MaxProjectionSuggestions`, and reject link-shaped or slash-command-looking suggestions.

- [x] Confirm and pin skill corpus parsing and embedded-resource behavior (AC: 3, 4)
  - [x] Extend `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillCorpusTests.cs`.
  - [x] Prove `SkillCorpusLoader.LoadEmbedded()` loads all embedded `docs/skills/frontcomposer/**/*.md` files deterministically and maps embedded resource names back to stable doc paths.
  - [x] Prove required front matter is enforced: `id`, `title`, `version`, `audience: agent`, `docfx`, `mcpResource: true`, `resourceUri`, `order`, `sourceDoc`, `narrative`, `references`; optional `migrationOwner`, `owningStory`, `publicApiReferences`, and `samplePaths`.
  - [x] Prove IDs are lowercase kebab-case starting with a letter and at most 128 chars; resource URIs canonicalize to lowercase `frontcomposer://skills/...`; duplicates are rejected case-insensitively.
  - [x] Prove exactly one `agent-reference` section is required, narrative content is not served, nested/unterminated/duplicate/unknown section markers fail, and markers inside fenced code blocks are ignored.
  - [x] Prove unsafe imperative bypass/impersonation wording is diagnosed without flagging negated boundary guidance such as "do not bypass validation".
  - [x] Prove per-resource fingerprints include the Markdown body digest so body-only edits change the fingerprint.
  - [x] Prove public API references, sample paths, and fenced C# snippets are validated by the existing validators.

- [x] Confirm and pin skill resource provider/read semantics (AC: 3, 4)
  - [x] Extend `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs`.
  - [x] Prove `FrontComposerSkillResourceProvider` fails fast with `InvalidSkillCorpusException` when parser diagnostics exist.
  - [x] Prove `ListResources()` returns per-skill descriptors plus aggregate manifest in deterministic order and with `text/markdown`.
  - [x] Prove `Read("frontcomposer://skills/<id>")` returns only `resource.Markdown` from the `agent-reference` block and never tenant data or narrative-only content.
  - [x] Prove `Read("frontcomposer://skills/manifest")` returns a deterministic manifest with `manifestSchemaVersion`, `corpusVersion`, `resourceCount`, and each resource's id/uri/source/version/owningStory/publicApiReferences/samplePaths.
  - [x] Prove unknown, auth-equivalent, malformed, canceled, and oversized skill reads use stable public tokens (`unknown_resource`, `malformed_request`, `canceled`, `response_too_large`) rather than enum names.
  - [x] Prove cancellation cannot escape `FrontComposerSkillResourceProvider.Read(...)` or `FrontComposerSkillMcpResource.ReadAsync(...)` as a raw `OperationCanceledException`.
  - [x] Prove `FrontComposerSkillMcpResource.ReadAsync(...)` echoes the requested URI when present and falls back to descriptor URI only when the request supplied none.
  - [x] Prove skill reads do not resolve or call `IFrontComposerMcpResourceVisibilityGate`, while projection reads still require it.

- [x] Preserve prior MCP command and lifecycle semantics (AC: 1, 2, 5)
  - [x] Re-run focused Story 5.1 and 5.2 MCP lanes after resource changes.
  - [x] Preserve dynamic command `tools/list`, `tools/call` admission ordering, server-issued ULID identity, hidden/unknown redaction, lifecycle handle validation, and nested lifecycle retry snapshot shape.
  - [x] Do not change `FrontComposerMcpCommandInvoker`, `FrontComposerMcpLifecycleTracker`, `McpLifecycleModels`, or `McpJsonSchemaBuilder` unless a resource test exposes a direct regression in shared MCP mapping.
  - [x] Do not change `CanonicalSchemaMaterial`, schema fingerprint canonicalization, package versions, or `Directory.Packages.props`.

- [x] Verification
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
  - [x] If local VSTest/MSBuild socket restrictions block the exact solution command, run focused in-process/fallback lanes for `Hexalith.FrontComposer.Mcp.Tests`, especially Invocation, Rendering, Skills, Hosting, and Schema tests; record the blocker and fallback evidence honestly.
  - [x] Reconcile `git diff --name-only` against this story's File List before review promotion.

## Dev Notes

### Brownfield Reality

- The resource surface already exists. Treat this as confirm-and-pin plus bounded hardening, not a rebuild.
- Projection resources are currently registered from `FrontComposerMcpDescriptorRegistry.Resources` as `FrontComposerMcpResource` instances in `AddFrontComposerMcp(...).WithResources(...)`.
- Skill resources are currently loaded from embedded markdown by `SkillCorpusLoader.LoadEmbedded()`, exposed by `FrontComposerSkillResourceProvider`, and appended to the same MCP resource list through `CreateMcpResources()`.
- `AddFrontComposerMcp(...)` already requires both `IFrontComposerMcpTenantToolGate` and `IFrontComposerMcpResourceVisibilityGate` before registration. Skill resources bypass the visibility gate by design; projection resources must not.
- `AddFrontComposerMcp(...)` already checks URI collisions between manifest projection resources and skill resources, including the reserved `frontcomposer://skills/manifest`.
- Current comments reference older story numbers (`Story 8-1`, `8-5`, `8-6a`) in MCP resource code. Do not treat those as current planning IDs; use the code behavior and this story's FC-MCP-RESOURCES contract as the implementation source of truth.

### Current Projection Resource Path

- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResource.cs`
  - Current state: wraps a manifest `McpResourceDescriptor` as an MCP SDK `McpServerResource`; advertises `text/markdown`; rejects resource templates; resolves `FrontComposerMcpProjectionReader` from request DI; maps `FrontComposerMcpResult` to `ReadResourceResult`.
  - This story changes: add missing adapter-level tests if absent. Production changes should be minimal unless tests reveal URI echo, content type, DI, or failure mapping drift.
  - Preserve: exact `IsMatch` behavior, descriptor metadata, no resource templates in v1, and no direct query/render logic in this adapter.

- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`
  - Current state: validates absolute `frontcomposer://` URIs; samples descriptor/catalog epochs; resolves the resource descriptor; resolves auth/tenant context; checks `IFrontComposerMcpResourceVisibilityGate`; evaluates resource schema negotiation; creates an immutable snapshot; validates visibility before query and before render; calls `IQueryService.QueryAsync<T>` with tenant-scoped `QueryRequest`; renders through `IFrontComposerMcpProjectionRenderer`.
  - This story changes: add pins for actual MCP resource reads and any missing security/revalidation branch. Avoid changing the reader unless a pin exposes a real behavior gap.
  - Preserve: no query before auth/visibility/schema admission, no partial render after visibility loss, schema gate evaluated once at admission, bounded take calculation, and sanitized failure taxonomy.

- `src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs`
  - Current state: renders table/status/timeline Markdown; escapes/redacts untrusted text; enforces row/field/cell/document caps; returns sanitized failures for unsupported render, cancellation, response-too-large, and downstream formatter errors.
  - This story changes: add missing contract pins around budget/failure/redaction behavior. Do not invent a new renderer or change the Markdown grammar without recording it in the FC-MCP-RESOURCES contract.
  - Preserve: `text/markdown`, inert Markdown escaping, no partial document on formatter/cancellation/too-small-budget failures, and no raw unsupported field values.

### Current Skill Resource Path

- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs`
  - Current state: contains parser, embedded loader, validators, aggregate manifest builder, read-result factories, provider, and `FrontComposerSkillMcpResource` SDK adapter.
  - This story changes: add missing tests for parser/provider/adapter contracts and bounded read behavior. Production changes should be limited to real drift found by those tests.
  - Preserve: default 32 KB cap, no truncation for oversized skill resources, exact lowercase URI lookup, `agent-reference`-only serving, deterministic manifest, and `InvalidSkillCorpusException` startup failure on diagnostics.

- `docs/skills/frontcomposer/**/*.md`
  - Current state: embedded skill corpus. These are published DocFX docs and MCP resources.
  - This story changes: avoid editing corpus content unless a validator/pin proves a conforming-resource bug. Do not use `docs/` as scratch space.
  - Preserve: front matter contract, section markers, sample/public API references, and snippet validation.

### Must-Fix Risks

- Adapter-level MCP resource coverage is thinner than direct reader/provider coverage. Pin `FrontComposerMcpResource.ReadAsync(...)` and `FrontComposerSkillMcpResource.ReadAsync(...)` directly so SDK-edge URI/content-type behavior cannot drift.
- Do not accidentally apply projection tenant visibility to skill resources. Skill resources are framework reference material; projection resources are tenant data.
- Do not accidentally remove projection visibility revalidation. Projection reads need admission, pre-query, and pre-render checks because query/render are separated by async work and visibility can change.
- Do not return partial Markdown on formatter failures, cancellation, response-too-large, or visibility loss. Partial rows or partial fenced snippets can mislead agents.
- Do not leak raw resource URIs, tenant IDs, JWT fragments, exception text, descriptor internals, command args, or raw query payloads into projection/skill failure responses.
- Do not treat `frontcomposer://skills/*` as a valid bounded context namespace for projections. That prefix is reserved for framework skill resources.
- Do not silently normalize the projection URI grammar. Record whether v1 is `frontcomposer://<context>/<projection>` or `frontcomposer://<context>/projections/<projection>` and make tests match the recorded contract.

### Architecture Guardrails

- Dependency direction remains `Mcp -> Contracts + Schema`; do not add a dependency from `SourceTools` to `Mcp` or from `Contracts` to net10-only packages.
- Keep package versions centralized in `Directory.Packages.props`; do not add `Version=` to project files.
- `ModelContextProtocol.AspNetCore` is already pinned to `1.3.0`; NuGet lists `1.3.0` as the current package version checked during story creation on 2026-06-05. Do not upgrade packages in this story.
- Use `ConfigureAwait(false)` on awaited calls. TreatWarningsAsErrors means analyzer warnings can break Release builds.
- Do not hand-edit generated files under `obj/**/generated/HexalithFrontComposer/`.
- Do not change `CanonicalSchemaMaterial` encoder, sentinel, comparer, or canonical serialization.
- Use xUnit v3, Shouldly, NSubstitute, bUnit/Verify where applicable; do not introduce a new test framework.

### Expected Source Touch Points

- Expected production source touch points only if tests expose drift:
  - `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResource.cs`
  - `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`
  - `src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs`
  - `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs`
  - `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`
  - `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs`
- Expected test touch points:
  - `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderCoverageTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderSchemaGateTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTaxonomyTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Rendering/McpMarkdownProjectionRendererTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillCorpusTests.cs`
  - `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs`
- Expected artifact:
  - `_bmad-output/contracts/fc-mcp-resources-contract-2026-06-05.md`

### Previous Story Intelligence

- Story 5.2 created `_bmad-output/contracts/fc-mcp-lifecycle-subscription-contract-2026-06-05.md` and pinned the actual MCP call-handler route for lifecycle. Story 5.3 should do the same for the MCP resource adapter path, not only direct reader/provider calls.
- Story 5.2 preserved the nested lifecycle retry shape and hidden/unknown redaction. Do not change lifecycle models or tool routing while implementing resource pins.
- Story 5.1 created `_bmad-output/contracts/fc-mcp-command-tool-invocation-contract-2026-06-05.md` and removed GUID/`Activity.TraceId` command identity fallbacks. Resource work must not reopen command identity or `tools/call`.
- Stories 4.4, 5.1, and 5.2 reinforced opaque MCP failure shapes. Resource failures should preserve shared hidden-equivalent behavior and avoid creating a resource-existence oracle.
- Recent reviews repeatedly found File List drift. Reconcile actual changed files before moving this story to review.

### Git Intelligence

- Recent commits:
  - `e01697d feat(story-5.2): Lifecycle subscription tool`
  - `c221237 feat(story-5.1): Expose generated commands as MCP tools`
  - `f696f21 docs: record epic 4 retrospective`
  - `1be0a05 feat(story-4.5): Retry and degraded state handling`
  - `db5e045 feat(story-4.4): Policy-gated command authorization`
- Relevant implementation pattern: contract artifact first, focused MCP tests next, production code changed only where pins expose drift, final story record reconciled against actual `git diff --name-only`.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` - Epic 5 / Story 5.3]
- [Source: `_bmad-output/planning-artifacts/epics.md` - FR17, FR18, FR19]
- [Source: `_bmad-output/project-context.md` - MCP Server Rules, Schema Fingerprint & Integrity Rules, Testing Rules]
- [Source: `_bmad-output/project-docs/api-contracts.md` - MCP tool & resource surface / skill-corpus authoring contract]
- [Source: `_bmad-output/project-docs/architecture.md` - AI-agent surface (MCP)]
- [Source: `_bmad-output/project-docs/component-inventory.md` - MCP server surface]
- [Source: `_bmad-output/contracts/fc-mcp-command-tool-invocation-contract-2026-06-05.md` - Story 5.1 command tool contract]
- [Source: `_bmad-output/contracts/fc-mcp-lifecycle-subscription-contract-2026-06-05.md` - Story 5.2 lifecycle contract]
- [Source: `_bmad-output/implementation-artifacts/5-2-lifecycle-subscription-tool.md` - previous story intelligence]
- [Source: `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResource.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`]
- [Source: `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs`]
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTests.cs`]
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderCoverageTests.cs`]
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/Rendering/McpMarkdownProjectionRendererTests.cs`]
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillCorpusTests.cs`]
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs`]
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs`]
- [Source: NuGet Gallery, `ModelContextProtocol.AspNetCore` package page, checked 2026-06-05]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` — passed, 0 warnings / 0 errors.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false --no-restore` — VSTest socket-blocked locally with `System.Net.Sockets.SocketException (13): Permission denied`.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Mcp.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Mcp.Tests.dll -parallel none` — passed, 338/338.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests.dll -parallel none` — passed, 338/338.
- Focused in-process Story 5.3 lane passed, 27/27: `ProjectionResourceAdapterTests`, `SkillResourceTests`, and `HostingTests`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Recorded the FC-MCP-RESOURCES v1 contract, including the live projection grammar `frontcomposer://<bounded-context>/projections/<projection-name>`, skill URI grammar, manifest resource, content types, bounds, failure tokens, no templates/streaming/package upgrades, and the projection-vs-skill security split.
- Added SDK-edge projection resource adapter pins proving exact URI metadata, no templates, DI routing to `FrontComposerMcpProjectionReader`, tenant-scoped query request behavior, sanitized invalid reads, and no query before admission succeeds.
- Added resource hosting pins proving `AddFrontComposerMcp(...)` supplies both projection and skill resources to the SDK resource collection and rejects the reserved `frontcomposer://skills/` namespace for projection descriptors.
- Added skill resource pins for exact canonical matches, manifest read determinism, agent-reference-only serving, stable oversized/canceled/malformed tokens, adapter URI echo behavior, and cancellation not escaping as raw `OperationCanceledException`.
- Hardened production behavior only where tests exposed drift: startup now rejects every manifest projection URI under `frontcomposer://skills/`, skill reads return the stable `canceled` token on post-lookup cancellation, and projection/skill SDK resources provide explicit static resource IDs so `.WithResources(...)` can materialize the SDK resource collection without exposing URI templates.
- Preserved Story 5.1 command and Story 5.2 lifecycle semantics; the full MCP in-process suite covers Invocation, Rendering, Skills, Hosting, Schema, command, and lifecycle lanes.
- Added end-to-end coverage of the live resource surface: `tests/e2e/specs/mcp-resources.spec.ts` plus the `tests/e2e/helpers/mcp-client.ts` JSON-RPC/SSE client drive `resources/list` and `resources/read` against a sample host wired in `samples/Counter/Counter.Web/Program.cs` (MCP enabled only for Development/Test) with `samples/Counter/Counter.Web/CounterMcpSampleQueryService.cs` supplying deterministic projection rows, asserting Markdown content types, agent-reference-only skill serving, manifest determinism, and sanitized projection failures with no tenant/user/JWT leakage. The sample host registers `AllowAllMcpTenantToolGate`/`AllowAllResourceVisibilityGate` explicitly, preserving the Story 5.4 fail-closed default.

### File List

- `_bmad-output/contracts/fc-mcp-resources-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/5-3-projection-and-skill-corpus-resources.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResource.cs`
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionResourceAdapterTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs`
- `samples/Counter/Counter.Web/Counter.Web.csproj`
- `samples/Counter/Counter.Web/Program.cs`
- `samples/Counter/Counter.Web/CounterMcpSampleQueryService.cs`
- `tests/e2e/package.json`
- `tests/e2e/helpers/mcp-client.ts`
- `tests/e2e/specs/mcp-resources.spec.ts`

### Change Log

- 2026-06-05: Implemented Story 5.3 projection and skill-corpus MCP resource pins; recorded FC-MCP-RESOURCES v1; hardened reserved skill URI startup rejection and skill cancellation token behavior; validated Release build and MCP fallback lanes.
- 2026-06-05: Senior Developer Review (AI) — adversarial review, 0 Critical / 0 High. Independently reproduced Release build 0 warnings / 0 errors and MCP suite 338/338. Auto-fixed 1 Medium: File List drift — added the six story-delivered sample-host and e2e files (`Counter.Web.csproj`, `Program.cs`, `CounterMcpSampleQueryService.cs`, `tests/e2e/package.json`, `tests/e2e/helpers/mcp-client.ts`, `tests/e2e/specs/mcp-resources.spec.ts`) that were changed in git but missing from the File List. No production code change. Status review → done.

## Senior Developer Review (AI)

**Reviewer:** Administrator · **Date:** 2026-06-05 · **Outcome:** Approve (0 Critical / 0 High; 1 Medium auto-fixed)

### Summary

Story 5.3 is a confirm-and-pin story over the existing MCP resource surface. All five acceptance criteria are implemented and backed by passing tests. The FC-MCP-RESOURCES v1 contract was recorded and correctly resolves the projection URI-grammar ambiguity in favour of the live `frontcomposer://<bounded-context>/projections/<projection-name>` form. The single substantive review finding was documentation drift, now corrected.

### Independently Verified

- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` → Build succeeded, 0 Warning(s), 0 Error(s) (reproduced; `TreatWarningsAsErrors` active).
- `Hexalith.FrontComposer.Mcp.Tests.dll -parallel none` (Release) → Total: 338, Failed: 0, Skipped: 0.
- Confirm-and-pin subtasks marked `[x]` without touching their named test files are legitimately satisfied by pre-existing coverage: renderer (18 facts), schema-gate (6), taxonomy (7), skill-corpus (20).
- AC2/AC3 public failure tokens exist in production (`malformed_resource`, `response_too_large`, `malformed_request`, `canceled`) and match the e2e assertions; `Canceled` → `"canceled"` mapping confirmed in `SkillCorpus.cs:853`.
- AC5 collision rejection: startup now rejects every manifest projection URI under the reserved `frontcomposer://skills/` prefix (`FrontComposerMcpServiceCollectionExtensions.cs`), pinned by `HostingTests` theory.
- AC1/AC4 adapter pins added in `ProjectionResourceAdapterTests.cs` and extended `SkillResourceTests.cs`/`HostingTests.cs`; projection reads route through DI with tenant from agent context, skill reads bypass the visibility gate by design.

### Findings

- **[Medium] File List drift (auto-fixed).** Six story-delivered files were changed in git but absent from the File List, and the Verification subtask "Reconcile `git diff --name-only` against this story's File List" was marked `[x]` despite the drift (a recurrence the Dev Notes explicitly warned about): `samples/Counter/Counter.Web/{Counter.Web.csproj,Program.cs,CounterMcpSampleQueryService.cs}` and `tests/e2e/{package.json,helpers/mcp-client.ts,specs/mcp-resources.spec.ts}`. These are legitimate scope (sample host + e2e validation of the resource surface), not scope creep. Fix applied: added all six to the File List and recorded the e2e/sample coverage in Completion Notes. No code change required.

### Considered and dismissed (not findings)

- `FrontComposerMcpResource.BuildResult` always returns the descriptor URI as the content `Uri` (even on malformed/missing-URI failures) rather than echoing the request like the skill adapter's P-12 path. Benign and arguably safer: `IsMatch` guarantees exact-URI routing on success, and never echoing attacker-supplied URI input on failure avoids a reflection vector.
- `FrontComposerSkillResourceProvider.Read` checks cancellation at method entry and again before the per-skill size check; the manifest branch relies on the entry check only. Harmless cosmetic asymmetry — both reachable paths return the stable `canceled` token before serving content.
