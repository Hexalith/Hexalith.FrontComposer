# Story 8.4a: Projection Rendering Sanitized Taxonomy and Snapshot

Status: review

> **Epic 8** - MCP & Agent Integration. Follow-up for Story **8-4 Projection Rendering for Agents**. Covers the deferred parts of **FR53**, **FR57**, **NFR27**, **NFR28**, and Story 8-4 acceptance criteria **AC9**, **AC11**, **AC16**, **AC17**, **AC18**, and **AC19**. Builds on Stories **8-1** through **8-4**, Epic 4 projection metadata, Epic 5 query/cache reliability, and Epic 7 tenant/policy context. Applies lessons **L03**, **L06**, **L08**, **L10**, **L14**, and **L15**.

---

## Executive Summary

Story 8-4 shipped Markdown projection rendering and then deferred the cross-cutting hardening that needs one focused pass:

- Replace generic `Request failed.` projection-read failures with a canonical sanitized response taxonomy that agents can parse without learning hidden resource facts.
- Introduce an immutable per-read snapshot and descriptor/catalog epoch revalidation so admission, query, and render cannot mix different tenant, policy, or catalog states.
- Finish atomic render/failure semantics so bounds, stale descriptors, cancellation, query failures, unsupported renders, and formatter failures never return partial Markdown.
- Add the P0 risk-tier tests deferred from Story 8-4 and explicitly audit the unsupported-column behavior against Story 4-6/L15.
- Preserve the 8-4 fixes already completed for truncation marker metadata, currency `DisplayFormat`, and typed `McpProjectionRenderStrategy`; do not redo them unless a regression test proves drift.

---

## Story

As an LLM agent,
I want projection-read failures to return stable sanitized categories and safe retry guidance,
so that I can distinguish hidden, stale, unavailable, oversized, unsupported, and degraded reads without receiving tenant data, hidden names, exact row counts, or raw exception text.

### Adopter Job To Preserve

Adopters should keep the Story 8-4 projection Markdown surface while gaining safer failure behavior. A projection resource that is hidden, stale, oversized, unauthorized, or failing downstream must remain safe for agent loops: no leaked resource existence, no tenant/user identifiers, no raw filters, no partial Markdown, no hidden counts, and no hand-maintained retry prose outside the framework.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A projection read fails before query because the URI is malformed, unknown, hidden, unauthorized, cross-tenant, policy-filtered, or tenant/user context is invalid | The MCP projection reader returns a result | The result uses a canonical sanitized category and agent-visible message, performs no query/render side effects, and preserves hidden/unknown equivalence where Story 8-2 requires it. |
| AC2 | A descriptor, visible catalog, tenant scope, or policy scope can change after admission | The request reaches query or render handoff | The reader captures an immutable per-read snapshot and revalidates descriptor/catalog epoch plus visibility before each handoff; stale or mismatched state returns the sanitized stale/unavailable category without querying or rendering. |
| AC3 | Query, EventStore, cache, timeout, cancellation, degraded-result, unsupported-render, response-too-large, or formatter failure occurs | The adapter maps the outcome for an agent | The response contains a stable category, docs code, retryability flag, and safe bounded text; it does not include exception text, stack traces, tenant/user IDs, claims, raw filters, resource names hidden from the current principal, exact hidden row counts, or cache keys. |
| AC4 | Rendering exceeds row, column, status-group, timeline-entry, cell, or document bounds | The renderer/adapter commits output | Coherent successful output may include the configured truncation marker and `IsTruncated=true`; incoherent or non-newline-bounded output is discarded and mapped to the sanitized bounds category with no partial Markdown. |
| AC5 | Cancellation, timeout, stale epoch, query failure, or formatter failure happens after a buffer has been allocated | The result is returned and telemetry/logging runs | Local buffers are discarded, no partial table/status/timeline text is cached or returned, and telemetry/logs contain only sanitized category, bounded context, role, request duration bucket, and no row payload. |
| AC6 | The current implementation has `FrontComposerMcpResult.Failure(category)` returning `Request failed.` | Projection read failures are inspected by tests | Projection-specific failures expose taxonomy text and structured content through a shared mapper while existing command/lifecycle hidden-unknown contracts remain source-compatible. |
| AC7 | Story 8-4 left unsupported fields dropped from MCP Markdown despite Story 4-6's no-silent-omit rule | The unsupported-column behavior is reviewed | The story either emits an explicit safe unsupported placeholder column for agent reads or records a binding decision with regression tests proving why MCP intentionally differs from the web grid; silent accidental drops are forbidden. |
| AC8 | P0 security and determinism tests run | Hidden/unknown/unauthorized/stale/bounds/atomic cases are exercised | Tests prove zero query side effects on admission failure, no hidden-name or tenant leakage, deterministic taxonomy strings, stale epoch rejection, atomic no-partial output, and unchanged happy-path Markdown roles. |
| AC9 | SourceTools manifest or public descriptor metadata is rebuilt | Regression tests inspect descriptors and generated code | Typed `McpProjectionRenderStrategy`, currency `DisplayFormat`, truncation marker/metadata parity, field ordering, and badge mappings remain covered so this follow-up does not regress Story 8-4's review patches. |
| AC10 | Story 8-4a completes | Stories 8-5, 8-6, 9-2, 10-2, and 10-6 continue | The taxonomy and snapshot contracts are stable enough for skill corpus guidance, schema versioning, CLI inspection, agent E2E, and benchmarks without requiring another 8-4 renderer redesign. |

---

## Tasks / Subtasks

- [x] T1. Define the projection-read response taxonomy contract (AC1, AC3, AC6)
  - [x] Add a single mapper for projection-resource failures in `Hexalith.FrontComposer.Mcp`, close to the adapter/reader edge rather than inside the pure Markdown renderer.
  - [x] Cover at least: `unknown_resource`, `malformed_resource`, `auth_failed`, `tenant_missing`, `policy_filtered`, `stale_descriptor`, `response_too_large`, `unsupported_render`, `query_failed`, `timeout`, `canceled`, `degraded_result`, and `downstream_failed`.
  - [x] For each category define safe text, `docsCode`, `retryable`, `refreshResources`, and `isHiddenEquivalent` where applicable.
  - [x] Preserve Story 8-2 hidden/unknown equivalence: hidden, unauthorized, cross-tenant, and policy-filtered resources must not reveal whether a named resource exists.
  - [x] Keep command-tool rejection suggestions and lifecycle hidden-unknown shape unchanged unless tests prove a shared helper is safe.

- [x] T2. Wire taxonomy output into projection resource reads (AC1, AC3, AC6)
  - [x] Update `FrontComposerMcpProjectionReader.ReadAsync` so projection failures return taxonomy text and structured content instead of bare `Request failed.`.
  - [x] Include `contentType = "text/markdown"` only for successful Markdown documents; failure content should be clearly structured as sanitized error metadata and safe text.
  - [x] Ensure `FrontComposerMcpException` categories from query/render paths are mapped without leaking exception messages.
  - [x] Add tests that `UnknownResource`, `MalformedRequest`, `ResponseTooLarge`, `UnsupportedRender`, `Timeout`, `Canceled`, `DownstreamFailed`, and `StaleDescriptor` use deterministic category payloads.

- [x] T3. Add immutable per-read snapshot and epoch contracts (AC2, AC5, AC10)
  - [x] Introduce an SDK-neutral snapshot model carrying only safe immutable values: projection key, protocol URI category or visible descriptor handle, render strategy, bounded context, descriptor epoch, catalog epoch, query shape category, request ID, and cancellation token.
  - [x] Do not carry raw tenant IDs, user IDs, claims, roles, tokens, principals, query filters, ETags, cache keys, lifecycle handles, mutable descriptors, or service instances into renderer models.
  - [x] Extend `FrontComposerMcpDescriptorRegistry` and/or the visible-catalog/admission seam with monotonic descriptor/catalog epoch values. If Story 8-2 lacks an epoch source, add the minimal shared interface here and wire default static epochs for immutable manifests.
  - [x] Revalidate epoch and current visibility before query dispatch and before render dispatch. Any mismatch returns `stale_descriptor` or the hidden-equivalent unavailable category without performing the next side effect.
  - [x] Add concurrency tests where a fake registry/gate changes epoch between admission/query/render and prove no query or partial render occurs after staleness is detected.

- [x] T4. Complete atomic render and bounds-failure behavior (AC3, AC4, AC5)
  - [x] Keep `McpMarkdownProjectionRenderer` pure and SDK-neutral; it may return success documents or failure categories, but it must never return partially formatted Markdown on exception.
  - [x] Treat `ResponseTooLarge` as a sanitized failure unless the renderer can produce newline-bounded coherent output with the exact truncation marker.
  - [x] Ensure local render buffers are not retained in result metadata, logs, exceptions, caches, or static state.
  - [x] Add tests for document budget smaller than the heading/marker, no newline before budget, formatter exception after at least one row, cancellation mid-row, and status/timeline bounds.
  - [x] Keep the exact marker `Output truncated by FrontComposer agent rendering limits.` aligned with `McpMarkdownProjectionDocument.IsTruncated`.

- [x] T5. Audit unsupported-column behavior against Story 4-6/L15 (AC7)
  - [x] Review the current `descriptor.Fields.Where(f => !f.IsUnsupported)` behavior in `McpMarkdownProjectionRenderer`.
  - [x] If MCP should match Story 4-6, render unsupported fields as inert `(unsupported)` placeholder columns using SourceTools order and add a regression test named around "EveryInputProjectionFieldAppearsInAgentOutput".
  - [x] If MCP intentionally differs, add a binding decision explaining the agent-surface rule, the reason it is safe, and a test proving unsupported drops are deliberate and do not hide supported fields.
  - [x] Do not add new projection role attributes or new unsupported-type analysis in this story.

- [x] T6. Add P0 risk-tier regression tests (AC8, AC9)
  - [x] Projection reader tests for hidden/unknown/unauthorized/cross-tenant equivalence with zero `IQueryService` calls.
  - [x] Admission failure tests proving no renderer allocation or suggestion catalog enumeration when resource visibility fails before query.
  - [x] Stale descriptor/catalog epoch tests for admission-to-query and query-to-render windows.
  - [x] Taxonomy snapshot tests for category strings, docs codes, retry flags, and redaction of tenant/user/JWT/API-key-looking payloads.
  - [x] Renderer atomicity tests for bounds, cancellation, formatter exception, unsupported render, and no partial table rows.
  - [x] Regression tests preserving typed render strategy, numeric `DisplayFormat`, badge mappings, deterministic field order, `IsTruncated` parity, and visible empty-state CTA scoping.

- [x] T7. Update docs-facing references and dev notes only where required (AC10)
  - [x] Add concise notes to the story artifact and any local process/deferred-work entry only if implementation discovers a genuinely new follow-up.
  - [x] Do not build the skill corpus, schema fingerprint negotiation, CLI inspection, or agent E2E in this story; only keep their consuming contracts stable.

- [x] T8. Verification
  - [x] `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`
  - [x] `dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj`
  - [x] `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --filter McpManifestEmitterTests`
  - [x] If touching public Contracts descriptors: `dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj`

---

## Dev Notes

### Existing State To Preserve

| File / Area | Current state | Preserve / Change |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs` | Resolves descriptors, obtains `IQueryService`, builds `QueryRequest` with tenant context, renders Markdown, and maps failures to generic `FrontComposerMcpResult.Failure(category)`. | Add taxonomy mapping and snapshot/epoch revalidation. Preserve existing query seam and do not add a second backend client. |
| `src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs` | Pure SDK-neutral renderer, supports Default/ActionQueue/DetailRecord tables, StatusOverview, Timeline, redaction, escaping, bounds, truncation marker, cancellation, and formatter failure categories. | Keep pure and SDK-neutral. Strengthen atomic/bounds behavior and unsupported-field decision without moving SDK DTOs into renderer contracts. |
| `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpFailureCategory.cs` | Already contains `ResponseTooLarge`, `UnsupportedRender`, `StaleDescriptor`, and `DegradedResult` added for 8-4a. | Prefer mapping these categories before adding new enum values. Add only if a test names an unrepresented category. |
| `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResult.cs` | `Failure(category)` returns `Request failed.` with optional structured content overload. | Keep generic command/lifecycle compatibility; projection reader can use the overload or a projection-specific factory for safe text. |
| `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs` | Loads manifests, builds immutable command/resource dictionaries, ordered descriptors, and normalized command lookup keys. | Add descriptor/catalog epoch support here or behind an interface. Registry construction should remain immutable and deterministic. |
| `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs` | Builds visible tool catalog using tenant/policy gates and sanitized descriptor text. | Reuse visibility concepts; do not duplicate fuzzy suggestion or tool-list behavior. If a resource visibility gate is needed, keep it shared and deterministic. |
| `src/Hexalith.FrontComposer.Contracts/Mcp/McpResourceDescriptor.cs` | Public SDK-neutral descriptor now uses typed `McpProjectionRenderStrategy`. | Preserve typed strategy and generated enum-literal emission. Do not reintroduce string render strategy on public descriptors. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs` | Emits typed `McpProjectionRenderStrategy.<value>` literals while keeping SourceTools intermediate transform analyzer-load safe. | Preserve SourceTools analyzer-load safety; do not make SourceTools depend on Contracts runtime enum types in intermediate models. |

### Canonical Taxonomy Shape

The exact strings can be adjusted during implementation, but tests must pin the final contract. A recommended starting shape:

| Category | Safe text | Retryable | Refresh resources |
| --- | --- | --- | --- |
| `unknown_resource` | `Projection resource is not available.` | false | true |
| `malformed_resource` | `Projection resource request is invalid.` | false | false |
| `auth_failed` | `Projection resource is not available for the current agent context.` | false | true |
| `stale_descriptor` | `Projection descriptor is stale. Refresh available resources and retry.` | true | true |
| `response_too_large` | `Projection output exceeded FrontComposer agent rendering limits.` | false | false |
| `unsupported_render` | `Projection rendering strategy is not supported for the agent surface.` | false | false |
| `query_failed` | `Projection data is temporarily unavailable.` | true | false |
| `timeout` | `Projection read timed out before a safe response was produced.` | true | false |
| `canceled` | `Projection read was canceled before a safe response was produced.` | true | false |
| `degraded_result` | `Projection data is available with degraded freshness.` | true | false |

Hidden, unauthorized, cross-tenant, and policy-filtered resources should map to the hidden-equivalent public text unless an already-visible descriptor can safely expose a more specific state.

### Immutable Read Snapshot Contract

The snapshot exists to prevent TOCTOU drift, not to move domain state into renderer models.

| Stage | Requirement |
| --- | --- |
| Admission | Validate URI/template, visible descriptor, tenant context, policy scope, request bounds, descriptor epoch, and catalog epoch before query. |
| Snapshot | Store safe immutable values only: projection key, bounded context, render strategy, descriptor/catalog epoch, safe labels/metadata copy, query shape category, request ID, and cancellation token. |
| Revalidation before query | If descriptor/catalog epoch, tenant scope, or policy visibility changed, stop before `IQueryService.QueryAsync<T>`. |
| Revalidation before render | If descriptor/catalog epoch or visibility changed after query, discard query result for this request and return stale/unavailable taxonomy with no render. |
| Telemetry | Log sanitized category and duration bucket only; no rows, filters, ETags, tenant IDs, user IDs, claims, or hidden names. |

### Binding Decisions

| Decision | Rationale |
| --- | --- |
| D1. Projection-read failures get a category-specific sanitized contract. | Agents need parseable outcomes; generic `Request failed.` encourages brittle retries and hides actionable stale/bounds states. |
| D2. Hidden-equivalent resource failures remain indistinguishable. | Story 8-2/NFR28 treat cross-tenant visibility as a security bug; failure text must not confirm existence. |
| D3. Descriptor/catalog epoch validation is a shared admission contract. | A private 8-4-only epoch would miss Story 8-2 list replay and policy changes. |
| D4. Snapshot models contain only safe immutable metadata. | Prevents claims, principals, filters, tokens, and mutable descriptors from leaking into renderer output or logs. |
| D5. The renderer remains SDK-neutral and atomic. | SDK churn stays at the MCP adapter edge; partial Markdown is a security boundary, not a formatting issue. |
| D6. Unsupported-column behavior must be explicit. | Story 4-6/L15 forbids accidental silent omission; if MCP differs, the difference must be deliberate and tested. |
| D7. Story 8-4 completed patch fixes are regression rails, not new scope. | Typed render strategy, currency format, and truncation metadata are already done; this story protects them while focusing on taxonomy/snapshot hardening. |

### Scope Guardrails

Do not implement these in Story 8-4a:

- Skill corpus resources, benchmark prompts, or agent code-generation docs. Owner: Story 8-5.
- Schema fingerprints, compatibility negotiation, migration delta diagnostics, or renderer abstraction redesign. Owner: Story 8-6.
- CLI commands for inspecting manifests or migrations. Owner: Story 9-2.
- Agent E2E across Claude Code, Codex, Cursor, Mistral, browser chat, or visual specimen gates. Owner: Story 10-2.
- Signed LLM benchmark releases, SBOM, or live provider CI gates. Owner: Story 10-6.
- New authorization policy language or UI. Owner: Story 7-3 or later authorization follow-up.
- New projection role attributes, new badge slots, custom chat templates, streaming/partial Markdown, or renderer-owned cross-request Markdown caching.

### Previous Story Intelligence

- Story 8-4 already implemented happy-path Markdown roles, SourceTools metadata propagation, redaction, escaping, cancellation handling, formatter failure atomicity, visible empty-state CTA scoping, truncation marker alignment, numeric currency formatting, and typed public render strategy descriptors.
- Story 8-4 code-review explicitly deferred to this story: sanitized taxonomy strings, immutable read snapshot and descriptor/catalog epoch revalidation, P0 risk-tier tests, full atomic category emission on bounds failure, and unsupported-column audit.
- Story 8-5 is now in progress and consumes the projection Markdown conventions. Do not change agent-visible success Markdown grammar unless a P0 security test forces it.
- Story 8-6 is ready-for-dev and will own fingerprints/version negotiation. Story 8-4a can expose stable epoch/version hooks but must not implement compatibility negotiation.

### Test Strategy

Use a small number of high-signal fixtures instead of duplicating every Story 8-4 renderer case. The minimum P0 set is:

| Test area | Required proof |
| --- | --- |
| Hidden/unknown equivalence | Unknown, hidden, unauthorized, cross-tenant, and policy-filtered resources return indistinguishable public text and no query call. |
| Stale epochs | Epoch changes before query and before render return stale/unavailable taxonomy and stop side effects. |
| Atomic failures | Bounds, cancellation, timeout, formatter exception, unsupported render, and query failure return no partial Markdown. |
| Taxonomy | Category strings, docs codes, retry flags, refresh flags, content type, and structured content are deterministic. |
| Redaction | Failure paths do not echo tenant/user IDs, JWT fragments, API keys, raw filters, ETags, cache keys, exception text, or hidden resource names. |
| Regression rails | Happy-path Default/ActionQueue/StatusOverview/Timeline output, typed render strategy, currency display format, badge mappings, field order, and `IsTruncated` parity remain green. |

---

## References

- [Source: `_bmad-output/implementation-artifacts/8-4-projection-rendering-for-agents.md`] - deferred review decisions, AC9/AC11/AC16/AC17/AC18/AC19, renderer contracts, taxonomy notes, snapshot contract, atomic commit contract.
- [Source: `_bmad-output/planning-artifacts/epics/epic-8-mcp-agent-integration.md#Story-8.4`] - FR53 projection Markdown scope and Epic 8 sequencing.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR53`] - projections as Markdown tables/status cards/timelines for agents.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR57`] - agent runtime command/query semantics.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md#MCP-security-boundary`] - tenant-safe MCP behavior.
- [Source: `_bmad-output/planning-artifacts/architecture.md#MCP-Interaction-Model`] - projections as MCP resources returning structured Markdown.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L03`] - tenant/user fail-closed guard.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06`] - defense-in-depth budget discipline.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review and elicitation are complementary.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10`] - deferrals need story-specific ownership.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L14`] - runtime state must be bounded by policy.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L15`] - silent-drop fixes need placeholder emission plus guardrail tests.
- [Source: `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`] - current projection read/query/render adapter.
- [Source: `src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs`] - current SDK-neutral renderer and atomic/bounds behavior.
- [Source: `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResult.cs`] - current generic failure result shape.
- [Source: `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpFailureCategory.cs`] - existing categories for 8-4a.
- [Source: `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs`] - immutable descriptor registry.
- [Source: `src/Hexalith.FrontComposer.Contracts/Mcp/McpResourceDescriptor.cs`] - public typed render strategy descriptor.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs`] - generated manifest enum literal emission.
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/Rendering/McpMarkdownProjectionRendererTests.cs`] - current renderer regression coverage.
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTests.cs`] - current projection reader coverage.
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderCoverageTests.cs`] - unknown/malformed/redaction/value-type coverage.
- [Source: `tests/Hexalith.FrontComposer.Mcp.Tests/ManifestTransformTests.cs`] - descriptor metadata regression tests.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-04: Red phase: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj --filter "ProjectionReaderTaxonomyTests|Render_EveryInputProjectionFieldAppearsInAgentOutput_WithUnsupportedPlaceholder"` failed on missing epoch/visibility contracts.
- 2026-05-04: Focused green phase: new projection taxonomy and renderer tests passed, 17/0/0.
- 2026-05-04: MCP regression: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj` passed, 151/0/0.
- 2026-05-04: SourceTools regression: `dotnet test tests\Hexalith.FrontComposer.SourceTools.Tests\Hexalith.FrontComposer.SourceTools.Tests.csproj --filter McpManifestEmitterTests` passed, 1/0/0.
- 2026-05-04: Contracts regression: `dotnet test tests\Hexalith.FrontComposer.Contracts.Tests\Hexalith.FrontComposer.Contracts.Tests.csproj` passed, 156/0/0.
- 2026-05-04: Build gate: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` passed with 0 warnings/errors.
- 2026-05-04: Full regression: `dotnet test Hexalith.FrontComposer.sln --no-build` passed, Contracts 156/0/0, MCP 151/0/0, Shell 1542/0/0, SourceTools 601/0/0, Bench 2/0/0.

### Completion Notes List

- 2026-05-04: Story created via `/bmad-create-story 8-4a-projection-rendering-sanitized-taxonomy-and-snapshot` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-04: Implemented projection-specific sanitized taxonomy mapping at the reader edge with stable category strings, docs codes, retry and refresh hints, hidden-equivalence metadata, and non-Markdown structured failure payloads.
- 2026-05-04: Added immutable per-read snapshot handling, descriptor/catalog epoch provider contracts, optional resource visibility revalidation, and stale checks before query and before render.
- 2026-05-04: Hardened atomic renderer behavior for response bounds and formatter failures; unsupported projection fields now remain visible as inert `(unsupported)` placeholder columns.
- 2026-05-04: Added P0 regression tests for admission failures, context failures, stale epochs, taxonomy redaction, hidden-equivalent resource denial, response bounds, unsupported render, and unsupported field placeholder output.

### File List

- `_bmad-output/implementation-artifacts/8-4a-projection-rendering-sanitized-taxonomy-and-snapshot.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpFailureCategory.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResult.cs`
- `src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpDescriptorEpochProvider.cs`
- `src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpResourceVisibilityGate.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionFailureMapper.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReadSnapshot.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`
- `src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderCoverageTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTaxonomyTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Rendering/McpMarkdownProjectionRendererTests.cs`

### Change Log

- 2026-05-04: Completed Story 8.4a implementation and moved status to review.
