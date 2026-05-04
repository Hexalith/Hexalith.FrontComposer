# Story 8.4a: Projection Rendering Sanitized Taxonomy and Snapshot

Status: done

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
- 2026-05-04: Review follow-up red phase: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj --filter "ProjectionReaderTaxonomyTests|McpMarkdownProjectionRendererTests"` failed on missing renderer/catalog test seams.
- 2026-05-04: Review follow-up focused green phase: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj --filter "ProjectionReaderTaxonomyTests|McpMarkdownProjectionRendererTests"` passed, 35/0/0.
- 2026-05-04: Review follow-up MCP regression: `dotnet test tests\Hexalith.FrontComposer.Mcp.Tests\Hexalith.FrontComposer.Mcp.Tests.csproj` passed, 158/0/0.
- 2026-05-04: Review follow-up build gate: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` passed with 0 warnings/errors.
- 2026-05-04: Review follow-up full regression: `dotnet test Hexalith.FrontComposer.sln --no-build` passed, Contracts 159/0/0, MCP 158/0/0, Shell 1542/0/0, SourceTools 606/0/0, Bench 2/0/0.

### Completion Notes List

- 2026-05-04: Story created via `/bmad-create-story 8-4a-projection-rendering-sanitized-taxonomy-and-snapshot` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-04: Implemented projection-specific sanitized taxonomy mapping at the reader edge with stable category strings, docs codes, retry and refresh hints, hidden-equivalence metadata, and non-Markdown structured failure payloads.
- 2026-05-04: Added immutable per-read snapshot handling, descriptor/catalog epoch provider contracts, optional resource visibility revalidation, and stale checks before query and before render.
- 2026-05-04: Hardened atomic renderer behavior for response bounds and formatter failures; unsupported projection fields now remain visible as inert `(unsupported)` placeholder columns.
- 2026-05-04: Added P0 regression tests for admission failures, context failures, stale epochs, taxonomy redaction, hidden-equivalent resource denial, response bounds, unsupported render, and unsupported field placeholder output.
- 2026-05-04: Resolved remaining review follow-ups P-20, P-22, P-24, P-25, and P-26. Snapshot state now uses a narrowed detached descriptor snapshot, stale-before-render tests count zero renderer calls, admission failure tests count zero visible-catalog enumeration, and renderer P0 tests cover mid-row cancellation plus status/timeline truncation caps.

### File List

- `_bmad-output/implementation-artifacts/8-4a-projection-rendering-sanitized-taxonomy-and-snapshot.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Contracts/Mcp/McpProjectionRenderStrategy.cs`
- `src/Hexalith.FrontComposer.Contracts/Mcp/McpResourceDescriptor.cs`
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpFailureCategory.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpResult.cs`
- `src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpDescriptorEpochProvider.cs`
- `src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpResourceVisibilityGate.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionDescriptorSnapshot.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionFailureMapper.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReadSnapshot.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/IFrontComposerMcpVisibleToolCatalogProvider.cs`
- `src/Hexalith.FrontComposer.Mcp/Rendering/DefaultFrontComposerMcpProjectionRenderer.cs`
- `src/Hexalith.FrontComposer.Mcp/Rendering/IFrontComposerMcpProjectionRenderer.cs`
- `src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerCoverageTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderCoverageTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTaxonomyTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionSpecGapTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/ManifestTransformTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Rendering/McpMarkdownProjectionRendererTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/McpManifestEmitterTests.cs`

### Change Log

- 2026-05-04: Completed Story 8.4a implementation and moved status to review.
- 2026-05-04: Code review pass via `/bmad-code-review 8-4a` against commit 9f1299c — 5 decision-needed, 28 patch, 0 defer, ~20 dismissed. All 5 decisions resolved and 25 of 28 patches applied; build clean (0 warnings/errors), tests green (Contracts 159/0/0, MCP 155/0/0, Shell 1542/0/0, SourceTools 606/0/0, Bench 2/0/0).
- 2026-05-04: Resolved deferred review patches P-20/P-22/P-24/P-25/P-26 and moved status back to review; build clean and full solution tests green.
- 2026-05-04: Re-review pass via `/bmad-code-review 8-4a` against `dadb439`. Three adversarial layers (Blind Hunter, Edge Case Hunter, Acceptance Auditor) over the 28-file path-filtered diff produced 0 decision-needed, 12 patch, 11 defer, 18 dismiss. Applied 11 of 12 patches (R2-P1 timeline title redaction, R2-P2 empty-state plural redaction, R2-P3 last-segment-only CTA matching, R2-P4 PolicyGateMissing non-retryable, R2-P5 resolve epoch provider + visibility gate once per read, R2-P6 visibility-flip-before-render P0 test, R2-P7 OperationCanceledException propagation in ReadStableItemKey, R2-P8 NonBacktracking compiled redaction regexes, R2-P10 visibility gate descriptor contract docs, R2-P11 strategy field-filter binding decision, R2-P12 SequenceEpochProvider throw-on-overrun); reclassified R2-P9 as dismissed (false positive — file already contains U+001F Unit Separator, same as prior-pass P-11). Status: review → done. Validation: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` (0 warnings/errors); `dotnet test Hexalith.FrontComposer.sln --no-build` => Contracts 159/0/0, MCP 159/0/0 (+1 new VisibilityFlipAfterQuery test), Shell 1542/0/0, SourceTools 606/0/0, Bench 2/0/0.

### Review Findings

#### Decision-needed (resolved)

- [x] [Review][Decision] DN-1 Markdown grammar drift — **Resolved: revert date/bool/empty-string; keep `IEnumerable→(unsupported)` (security)**. `FormatCell` reverted to ISO 8601 `'o'` for `DateTime`/`DateTimeOffset`, `bool` reverted to `true/false`, empty-string `-` substitution removed; timeline timestamp restored to `'o'`; `FormatDateTime` helper deleted; `Suggestions:` label was already correctly removed in 8-4 (D5) — not re-added. `IEnumerable→(unsupported)` retained as defense-in-depth against arbitrary collection content leakage.
- [x] [Review][Decision] DN-2 Hidden-equivalent indistinguishability — **Resolved: collapse to identical `unknown_resource` public payload when `IsHiddenEquivalent: true`**. Internal `Category` enum preserved for telemetry/logging; agent-visible `category`/`message`/`docsCode` collapse so an adversary cannot branch on the structured payload to learn whether a resource is hidden, unauthorized, tenant-mismatched, or policy-filtered. File: `FrontComposerMcpProjectionFailureMapper.cs` (added `HiddenEquivalentPublic` and pre-emit selection).
- [x] [Review][Decision] DN-3 Static `(1, 1)` epoch as registry default — **Resolved: keep static, document explicitly**. Added a comment to `FrontComposerMcpDescriptorRegistry.GetEpochs()` clarifying the in-memory manifest registry is immutable for the host lifetime and that hot-reload hosts must register a custom `IFrontComposerMcpDescriptorEpochProvider`. The reader's snapshot/revalidation contract detects drift via the provider and emits `StaleDescriptor` without rendering partial output.
- [x] [Review][Decision] DN-4 `MatchesEmptyStateCta` cross-bounded-context filter — **Resolved: keep new filter + record as binding decision** (see "Critical Decisions" addendum below). Empty-state suggestions now require strict bounded-context anchoring; projections without a bounded context emit no suggestions (fail-closed) so cross-context bleed cannot occur even on missing metadata.
- [x] [Review][Decision] DN-5 Duplicate `OperationCanceledException` handlers — **Resolved: collapsed into one handler**. The redundant second clause was removed; explicit timeouts continue to surface via `TimeoutException` and map to the `Timeout` category. File: `FrontComposerMcpProjectionReader.cs` `ReadAsync` exception handlers.

#### Critical Decisions addendum (DN-2 / DN-3 / DN-4 / IEnumerable)

- **CD-DN-2 Hidden-equivalent collapse** — All categories with `IsHiddenEquivalent: true` (`UnknownResource`, `AuthFailed`, `TenantMissing`, `PolicyFiltered`) emit an identical agent-visible payload (`category="unknown_resource"`, `docsCode="HFC-MCP-PROJECTION-UNKNOWN-RESOURCE"`). Internal categorization is retained for host-side telemetry and host gating decisions. Tests pin indistinguishability via `InvalidAgentContext_*` and the `PolicyFiltered` branch of `QueryAndRenderFailures_*`.
- **CD-DN-3 Immutable-manifest registry epoch** — `FrontComposerMcpDescriptorRegistry.GetEpochs()` returns `(1, 1)` constants because the in-memory manifest set is immutable across the host lifetime. Hot-reload hosts MUST register a custom `IFrontComposerMcpDescriptorEpochProvider` whose values strictly increase on every catalog/descriptor mutation. Snapshot/revalidation drift is detected via the provider, never via the registry.
- **CD-DN-4 Cross-bounded-context fail-closed CTA filter** — `MatchesEmptyStateCta` requires a non-empty `boundedContext` on the projection AND exact-match on the candidate command's `BoundedContext`. Empty / missing bounded context yields zero suggestions. Story 8-5 skill corpora MUST anchor empty-state CTAs by bounded context.
- **CD-IEnumerable Inert collection rendering** — `FormatCell` returns the `(unsupported)` placeholder for any non-string `IEnumerable` value rather than serializing collection content. Defense-in-depth against arbitrary nested payloads slipping past redaction; fields that need scalar rendering must be marked `IsUnsupported=false` and provide a primitive value.

#### Patches

- [x] [Review][Patch] P-1 Snapshot epoch TOCTOU — descriptor read at `reader.cs:25` before `CurrentEpochs()` sampled in `CreateSnapshot` at `:127`. Sample epoch first or detect epoch advance between lookup and snapshot creation [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:25-37,124-140`]
- [x] [Review][Patch] P-2 `ValidateContext` null-deref on `context.Principal` — `context.Principal.Identity` accessed without null-checking `context.Principal` [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:117-121`]
- [x] [Review][Patch] P-3 `IFrontComposerMcpResourceVisibilityGate` is optional with `null` short-circuit (anti-pattern per memory rule `feedback_no_optional_security_params.md`); require DI registration or use a decorator chain [`src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpResourceVisibilityGate.cs:5`, reader `IsResourceVisibleAsync`]
- [x] [Review][Patch] P-4 `registry.TryGetResource` outside try block leaks raw exceptions to MCP transport — move inside try or wrap with sanitized failure mapping [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:25`]
- [x] [Review][Patch] P-5 `BuildSafeSuggestionsAsync` cross-bounded-context bleed when `boundedContext` null/empty — `MatchesEmptyStateCta` skips cross-context filter on missing context; fail-closed instead [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:245-256`]
- [x] [Review][Patch] P-6 `ReadStableItemKey` property getter throwing crashes the entire timeline render — wrap reflection access in try/catch and treat throws as null [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:519-534`]
- [x] [Review][Patch] P-7 `Humanize` throws `ArgumentOutOfRangeException` on unpaired surrogate via `new Rune(codePoint)` — switch to `Rune.TryCreate` and treat invalid runes as non-letter [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:469-502`]
- [x] [Review][Patch] P-8 `BoundDocument` no-newline-in-budget hard-fails entire document — fall back to Rune-boundary cut + marker so legitimate single-line prefixes still emit content [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:557-580`]
- [x] [Review][Patch] P-9 `BoundDocument` final length not clamped to `MaxProjectionMarkdownCharacters` — output may exceed cap by `Environment.NewLine.Length`; assert `result.Length <= max` or trim [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:557-580`]
- [x] [Review][Patch] P-10 `IsTruncated` false-positive when null entries are filtered — `totalCount > items.Count` flips true when query returns N including some nulls and items.Count < N; compare against pre-filter count or `take` instead [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:194-214`]
- [x] [Review][Patch] P-11 ~~Status-overview key collision~~ — **DISMISSED (false positive)**: file contains a Unit Separator () between slot and member; reviewers misread it as empty quotes
- [x] [Review][Patch] P-12 `RenderStatusOverviewDocument` empty-status path emits truncation marker even when nothing was truncated [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:194-199`]
- [x] [Review][Patch] P-13 Truncation marker can emit twice (once by table render, once by `BoundDocument` post-cut) — track marker state in `BoundDocument` to avoid double-emit [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:622-626,1065-1077`]
- [x] [Review][Patch] P-14 ~~ set incomplete~~ — **DISMISSED**:  already escapes  per character, so they cannot form Markdown constructs after escape; the shape filter only needs to reject backticks (code-fence) and  schemes / leading-slash commands which are still rejected
- [x] [Review][Patch] P-15 Bearer regex over-redacts dotted prose because `.` is in the token char class — anchor on `\s` or word boundary, or remove `\.` [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:954`]
- [x] [Review][Patch] P-16 `DateTime { Kind: Unspecified }` silently treated as UTC by `SpecifyKind(..., Utc)` — silent shift on local timestamps; preserve original kind or render with explicit unspecified marker [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:884-886`]
- [x] [Review][Patch] P-17 `AppendEmptyState` and `## Title` paths emit descriptor `Title`/`EntityPluralLabel` without `RedactSensitiveText` — apply redaction to all agent-visible descriptor text [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:298-301`]
- [x] [Review][Patch] P-18 Manifest emitter interpolates `resource.RenderStrategy` raw into generated source after `McpProjectionRenderStrategy.` — use `nameof()` or constrain `renderStrategy` field to the enum type to remove codegen fragility [`src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs:143`]
- [x] [Review][Patch] P-19 `CopyDescriptor` shallow copy — `Fields` array is copied but each `McpParameterDescriptor` element (and its inner collections such as `BadgeMappings`) is shared with the live registry; either deep-clone or assert/document immutability invariant [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:142-145`]
- [x] [Review][Patch] P-20 Snapshot subset — **RESOLVED**: added `FrontComposerMcpProjectionDescriptorSnapshot` as the narrowed detached snapshot handle. The read snapshot no longer stores the registry descriptor object; it carries only safe immutable descriptor values needed for query/render handoff and copies field enum/badge collections.
- [x] [Review][Patch] P-21 ~~ taxonomy~~ — **RESOLVED via DN-2**: hidden-equivalent collapse means PolicyFiltered now emits the canonical  payload regardless of whether the read pipeline produces it. Mapper entry retained for hosts that throw  from custom gates; internal  distinguishes telemetry while public surface stays indistinguishable
- [x] [Review][Patch] P-22 Render-call counter on stale-descriptor test — **RESOLVED**: added `IFrontComposerMcpProjectionRenderer` with a default adapter and updated the post-query stale test to inject a counting renderer. The test now proves query CallCount=1 and renderer CallCount=0 when epoch advances before render.
- [x] [Review][Patch] P-23 Hidden/unknown indistinguishability — **RESOLVED via DN-2**: tests  and  now assert the collapsed  payload; identical // across all 4 hidden-equivalent categories proves indistinguishability
- [x] [Review][Patch] P-24 Mid-row cancellation P0 test — **RESOLVED**: added a fault-injection cell that cancels during row formatting; renderer returns `Canceled` with no partial document.
- [x] [Review][Patch] P-25 Status-group / timeline-entry bounds atomic-truncation tests — **RESOLVED**: added per-axis tests proving status-group and timeline-entry caps emit one bounded truncation marker, preserve coherent output, and set `IsTruncated=true`.
- [x] [Review][Patch] P-26 Admission-side catalog-enumeration counter — **RESOLVED**: added `IFrontComposerMcpVisibleToolCatalogProvider` and a counting test provider; resource visibility denial now proves query CallCount=0 and visible-catalog enumeration CallCount=0.
- [x] [Review][Patch] P-27 Suggestion link/command filter strips legitimate parens — **APPLIED**:  reduced to backtick only.  already neutralizes  per character, so the shape filter no longer drops labels like . Schemes () and leading-slash commands remain rejected outright
- [x] [Review][Patch] P-28 Spec File List incomplete — add `src/Hexalith.FrontComposer.Contracts/Mcp/McpProjectionRenderStrategy.cs`, `src/Hexalith.FrontComposer.Contracts/Mcp/McpResourceDescriptor.cs`, `src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTests.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/ManifestTransformTests.cs`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/McpManifestEmitterTests.cs` [`_bmad-output/implementation-artifacts/8-4a-projection-rendering-sanitized-taxonomy-and-snapshot.md:249-264`]

#### Re-review findings (2026-05-04, `/bmad-code-review 8-4a` pass 2)

Adversarial pass after `dadb439` review-follow-up commit. Three layers (Blind Hunter, Edge Case Hunter, Acceptance Auditor) over the 28-file path-filtered diff `7a69cdf..HEAD`. Verified findings against the actual source before triage.

##### Patches

- [x] [Review][Patch] R2-P1 **P-17 incomplete: Timeline strategy emits `descriptor.Title` without `RedactSensitiveText`** — Table and StatusOverview both call `RedactSensitiveText(descriptor.Title)` (lines 78, 134); Timeline at line 227 escapes only. Operator-supplied titles containing redactable patterns (`api_key=...`, `Bearer xxx`, JWT fragments) leak into agent-visible Markdown via the Timeline path. [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:227`]
- [x] [Review][Patch] R2-P2 **P-17 incomplete: `AppendEmptyState` emits descriptor `EntityPluralLabel` / `Title` fallback without redaction** — Line 306 emits `EscapeMarkdownText(plural.ToLowerInvariant())` where `plural` is `descriptor.EntityPluralLabel` or fallback `descriptor.Title`, neither passed through `RedactSensitiveText`. Same leak class as R2-P1 for the empty-state heading. [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:303-306`]
- [x] [Review][Patch] R2-P3 **`SegmentMatches` allows non-suffix namespace segments to match the empty-state CTA** — Iterates every dotted segment of `CommandTypeName`/`ProtocolName` and accepts any equal segment, so a CTA `"Create"` matches `Other.Create.IrrelevantCommand`. Comment intent (line 270) is "anchor on full namespace-segment equality so 'Create' cannot match 'Other.Foo.CreateInvoiceCommand'", but the implementation also matches namespace-internal segments. Constrain to last segment only (the type name) or require exact equality with `CommandTypeName`/`ProtocolName`. [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:284-296`]
- [x] [Review][Patch] R2-P4 **`PolicyGateMissing` mapped to `downstream_failed` retryable=true** — A missing security gate is a host configuration error and should never be retryable. Telling the agent to retry indefinitely is wasteful and noisy. Map to a non-retryable category (or split `policy_gate_missing` from `downstream_failed`). [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionFailureMapper.cs:120-126`]
- [x] [Review][Patch] R2-P5 **`CurrentEpochs()` resolves `IFrontComposerMcpDescriptorEpochProvider` four times per read** — The provider is resolved on every call (admission pre-lookup, post-lookup, pre-query, pre-render). Scoped/transient providers may return four different instances, defeating monotonic counters; stateful providers cannot rely on per-read identity. Resolve once at the top of `ReadAsync` and pass the captured provider through. [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:163-165`]
- [x] [Review][Patch] R2-P6 **Missing P0 test: visibility flip between query and render with renderer.CallCount=0 assertion** — P-22 added a renderer-call counter for *epoch* advance, but no equivalent test pins `renderer.CallCount=0` when the visibility gate flips false between query and render. AC2 requires visibility revalidation before render. Add `VisibilityFlipAfterQuery_ReturnsHiddenEquivalentWithoutRendering`. [`tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/`]
- [x] [Review][Patch] R2-P7 **`ReadStableItemKey` `catch (Exception)` swallows `OperationCanceledException` from a property getter** — A getter that throws OCE linked to the render token is silently dropped, defeating cancellation propagation. Filter the catch (`catch (Exception ex) when (ex is not OperationCanceledException)`) or rethrow OCE explicitly. [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs` `ReadStableItemKey`]
- [x] [Review][Patch] R2-P8 **`RedactSensitiveText` regex has no `MatchTimeout` and no `RegexOptions.NonBacktracking`** — The dotted Bearer pattern `(?i)\bbearer\s+[A-Za-z0-9_\-]+(?:\.[A-Za-z0-9_\-]+)*` is bounded in practice by `MaxProjectionCellCharacters` and trim, but adversary-supplied descriptor titles, suggestions, and cell content go through it without a regex timeout. Convert to compiled `Regex` instances with `RegexOptions.NonBacktracking | RegexOptions.IgnoreCase` (or set a 50ms `MatchTimeout`). [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:429-443`]
- [x] [Review][Patch] R2-P9 ~~`slot + "" + member` group key has no delimiter~~ — **DISMISSED (false positive, identical to prior-pass P-11)**: the file already contains a U+001F (Unit Separator) between the apparent empty quotes; both Blind Hunter and Edge Case Hunter misread the non-printable character as `""`. Verified via `od -c`: bytes show `+ " 037 " +`. No code change required. Original prose follows for the audit trail: — Today's slot list (`Danger`, `Warning`, `Success`, `Info`, `Accent`, `Neutral`) has no prefix collisions, so the bug is latent. Adding any future slot whose name shares a prefix with another (or that has a member named like another slot+member combination) silently merges status groups. Use a delimiter that cannot appear in either component (`''` or `(slot, member)` ValueTuple key). [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:172`]
- [x] [Review][Patch] R2-P10 **Visibility-gate descriptor contract is implicit** — `IsResourceVisibleAsync` is invoked three times per read with three different `McpResourceDescriptor` instances (live registry descriptor pre-snapshot, then `snapshot.Descriptor.ToDescriptor()` twice). Hosts that key gate caches off `descriptor` reference identity will mis-cache. Document on the interface that `descriptor` may be a reconstructed snapshot copy and gates must compare by descriptor `Name`/`BoundedContext` equality. [`src/Hexalith.FrontComposer.Mcp/IFrontComposerMcpResourceVisibilityGate.cs:5`]
- [x] [Review][Patch] R2-P11 **Field `IsUnsupported` parity differs across strategies** — `RenderTableDocument` keeps unsupported fields in the projected `fields` and emits the `(unsupported)` placeholder column (per CD-DN-1 and the AC7 test). `RenderStatusOverviewDocument` (line 130 area) and `RenderTimelineDocument` (line 238-240) re-derive `renderableFields = descriptor.Fields.Where(!IsUnsupported)` and silently drop them. AC7's "EveryInputProjectionFieldAppearsInAgentOutput" pins only the table strategy; status/timeline still silently omit. Either align all strategies to keep unsupported columns or add a binding decision recording the per-strategy difference and a regression test that pins the divergent behavior. [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:238-244`]
- [x] [Review][Patch] R2-P12 **`SequenceEpochProvider` test fixture clamps past end of array** — `int index = Math.Min(_index, epochs.Length - 1);` plus `_index++` silently returns the last epoch forever after exhaustion, masking unexpected extra calls in tests. Throw `InvalidOperationException("epoch sequence exhausted")` on overrun so test bugs (and any new per-read resolution count) surface immediately. [`tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTaxonomyTests.cs` `SequenceEpochProvider`]

##### Defer

- [x] [Review][Defer] R2-D1 **`ProtocolUriCategory` snapshot field is hardcoded `"frontcomposer_projection"`** — Decorative metadata not differentiated by URI shape. Pre-existing decision per CD-DN-3. **Owner:** Story 8-6 schema-fingerprint follow-up. Sources: auditor.
- [x] [Review][Defer] R2-D2 **`QueryShapeCategory` snapshot field is hardcoded `"take"`** — Same as R2-D1: telemetry-quality field with no per-call differentiation. **Owner:** Story 8-6 (paired with telemetry hardening). Sources: auditor.
- [x] [Review][Defer] R2-D3 **Schema fingerprint emission and `SchemaMismatch`/`UnknownSchemaBaseline`/`UnsupportedSchemaAlgorithm`/`SchemaIntegrityMismatch` failure categories present in the diff** — Story 8-4a's path-filtered diff catches these because `McpManifestEmitter.cs`, `FrontComposerMcpFailureCategory.cs`, and `McpResourceDescriptor.cs` were also touched for Story 8-6 in the same PR (#9). Per scope guardrails, schema fingerprints are Story 8-6 territory. **Owner:** Story 8-6 review (`8-6-schema-versioning-and-multi-surface-abstraction.md`, status: `review`). Sources: auditor.
- [x] [Review][Defer] R2-D4 **Skill corpus resource provider registered in `AddFrontComposerMcp`** — `FrontComposerSkillResourceProvider` and `CreateMcpResources()` concat at lines 47-48, 73 of `FrontComposerMcpServiceCollectionExtensions.cs`. Same scope-creep pattern as R2-D3 — coalesced PR. **Owner:** Story 8-5 review (`8-5-skill-corpus-and-build-time-agent-support.md`, status: `review`). Sources: auditor.
- [x] [Review][Defer] R2-D5 **`IsMalformedResourceUri` validates only Scheme + Host** — `Uri.TryCreate(Absolute)` accepts percent-encoded control bytes and bidi overrides in the host; the registry lookup is exact-match so non-matching URIs return `UnknownResource` safely, but the malformed-URI contract is narrower than the docs imply. Pre-existing scope; tighten in a future hardening pass. **Owner:** v1.x URI-parsing audit. Sources: blind+edge.
- [x] [Review][Defer] R2-D6 **`LooksLikeLinkOrCommand` does not detect bidi-prefixed slash commands (` /danger`, `‎/danger`)** — `Trim()` removes some Unicode whitespace categories but not all bidi/zero-width formatters; an attacker-supplied suggestion could embed a slash-command with a leading bidi marker. Suggestions go through `LooksLikeLinkOrCommand` but the inline-`/` check requires `char.IsWhiteSpace` on the preceding char, which is false for `‎`/` `. **Owner:** v1.x untrusted-text contract hardening. Sources: edge.
- [x] [Review][Defer] R2-D7 **Bearer regex over-redacts plain prose like "Bearer authentication required"** — Pattern matches `Bearer` followed by alphanumerics with optional dotted segments — legitimate prose collapses to `Bearer [redacted] required`. False-positive is harmless (defense-in-depth); false-negative would be a real leak. Track as a known trade-off. **Owner:** v1.x redaction policy review (paired with R2-D5). Sources: edge.
- [x] [Review][Defer] R2-D8 **`BoundDocument` defensive clamp at `result[..max]` may split a surrogate pair from an operator-supplied truncation marker containing emoji at trim boundary** — Extremely narrow edge: requires marker text to contain a surrogate pair landing exactly at `max` after newline prepending. P-9 was applied; the residual case is hostile-marker-only. **Owner:** v1.x marker-validation hardening. Sources: edge.
- [x] [Review][Defer] R2-D9 **Visibility gate captive-dependency risk if reader is resolved from root scope** — `services` is the constructor-injected `IServiceProvider`; if the host registers the reader as a singleton or resolves from the root provider, the scoped gate would resolve at root scope, bypassing per-request tenant context. Pre-existing host-config concern; reader is registered scoped (line 38 of extensions). **Owner:** v1.x hosting docs. Sources: blind.
- [x] [Review][Defer] R2-D10 **`Render_DocumentBudgetTooSmallForMarker` boundary at `budget == 1`** — Test pins `budget <= 0` throws; no test pins exactly `budget == 1` emits one prefix character + marker. Boundary regression test would tighten the contract. **Owner:** v1.x renderer-bounds hardening. Sources: edge.
- [x] [Review][Defer] R2-D11 **Snapshot `CopyParameter` always uses `StringComparer.Ordinal` for `BadgeMappings` even if the live descriptor used a different comparer** — If a descriptor's BadgeMappings was built with `OrdinalIgnoreCase`, the snapshot silently changes to case-sensitive lookup. Public surface today builds dictionaries Ordinal so no current breakage. **Owner:** v1.x snapshot-copy hardening. Sources: edge.

##### Dismissed (false positives, intentional, or already-handled)

- [x] [Review][Dismiss] Bare `catch` in `ReadAsync` line 107 mapping to `DownstreamFailed` — sanitized fallback is the documented design (no exception text leaks). Logging is out of scope for 8-4a. Sources: blind.
- [x] [Review][Dismiss] `OperationCanceledException` collapses linked-CTS timeouts and user cancels — DN-5 documented decision; explicit `TimeoutException` differentiates timeouts. Sources: blind.
- [x] [Review][Dismiss] `FormatEnumerable` returns `(unsupported)` for all collections — CD-IEnumerable binding decision; defense-in-depth against arbitrary nested payloads. Sources: blind+auditor.
- [x] [Review][Dismiss] `DateTime { Kind: Unspecified } => null` — P-16 documented decision (avoid silent UTC shift). Sources: blind.
- [x] [Review][Dismiss] `LinkOrCommandChars` collapsed to single backtick — P-27 already applied; per-char escape neutralizes other constructs. Sources: blind.
- [x] [Review][Dismiss] Acceptance auditor "File List/diff disagreement" — files `ManifestTransformTests.cs`, `McpManifestEmitterTests.cs`, `sprint-status.yaml` ARE in the diff at non-MCP/Invocation paths; my path filter happened to scope to story-8-4a's primary directories. Verified via `git log 7a69cdf..HEAD -- <files>`. P-28 closure stands. Sources: auditor.
- [x] [Review][Dismiss] `isHiddenEquivalent` boolean leaks hidden-equivalence — verified false positive: the boolean is fully redundant with `category` (`unknown_resource` ⇔ hidden-equivalent), no extra information channel. Sources: auditor.
- [x] [Review][Dismiss] `Dashboard` enum value is "Reserved" — strategy throws `UnsupportedRender` at the renderer; not a "new projection role attribute" per scope guardrails (which forbid descriptor attributes, not strategy enum values). Sources: auditor.
- [x] [Review][Dismiss] `LooksLikeLinkOrCommand` IndexOutOfRange on all-zero-width input — verified safe: ZWSP / ZWNJ are not stripped by `Span.Trim()` (`char.IsWhiteSpace('​')` is false), so `trimmed[0]` always has a valid index when `IsNullOrWhiteSpace` is false. Sources: edge.
- [x] [Review][Dismiss] `FormatFormattable` hardcoded `"C"` for Currency — intentional; tests pin `CultureInfo.InvariantCulture.NumberFormat.CurrencySymbol` (U+00A4). Sources: blind+edge.
- [x] [Review][Dismiss] `TrimCell` `budget == 0` returns just `"..."` — unreachable from `FormatCell` because `MaxProjectionCellCharacters >= 4` validator floor guarantees `budget >= 1`. Sources: edge.
- [x] [Review][Dismiss] `MaxProjectionCellCharacters = 4` off-by-one — verified safe: `budget = 4 - 3 = 1`, output = `text[..1] + "..."` = 4 chars total. Sources: blind.
- [x] [Review][Dismiss] `Dictionary<string, string>(StringComparer.Ordinal)` emit shape — Blind Hunter self-downgraded; emitted code parses correctly under both empty and non-empty paths. Sources: blind.
- [x] [Review][Dismiss] `_ = enumerable;` discard in `FormatEnumerable` — intentional unused-arg marker per CD-IEnumerable. Sources: blind.
- [x] [Review][Dismiss] `MatchesEmptyStateCta` does not re-validate auth between query and render — `ValidateContext` is invoked once before snapshot creation; the snapshot freezes `context` and re-running visibility gate uses the frozen context. AC2 requires re-validation of visibility/epoch, not full auth re-validation. Sources: edge.
- [x] [Review][Dismiss] `Guid.NewGuid()` for `RequestId` is overwritten by upstream caller — the reader's `ReadAsync` does not accept a caller-supplied `RequestId`; the `BuildRenderRequestAsync` `RequestId: snapshot.RequestId` is internal-only. Sources: blind.
- [x] [Review][Dismiss] `CommandLifecycleTests.cs` registers `FrontComposerMcpProjectionReader` as Singleton — test helper; production wiring is `TryAddScoped`. Sources: auditor.
- [x] [Review][Dismiss] `IFrontComposerMcpDescriptorEpochProvider` short-circuits to registry's static `(1, 1)` — DN-3 / CD-DN-3 documented decision (immutable manifest registry). Hot-reload hosts must register a custom provider. Sources: auditor.

