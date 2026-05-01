# Story 8.4: Projection Rendering for Agents

Status: ready-for-dev

> **Epic 8** - MCP & Agent Integration. Covers **FR53**, **UX-DR35**, **UX-DR44**, and the agent read surface of **FR57**. Builds on Story **8-1** MCP resource descriptors/query adapter, Story **8-2** tenant-scoped visibility and hidden/unknown semantics, Story **8-3** command lifecycle/read-your-writes flow, Epic 4 projection role metadata, Epic 5 query/EventStore reliability, and Epic 7 authentication/tenant context. Applies lessons **L03**, **L04**, **L06**, **L08**, **L10**, and **L14**.

---

## Executive Summary

Story 8-4 turns projection reads into deterministic Markdown that agents can show to users without parsing raw JSON or guessing web UI semantics:

- Add an agent projection renderer in `Hexalith.FrontComposer.Mcp` that renders MCP projection resources as `text/markdown`.
- Reuse SourceTools projection IR, projection role hints, column labels, badge mappings, empty-state metadata, and query seams already created for the web surface.
- Render Default and ActionQueue projections as Markdown tables, StatusOverview projections as compact status summaries, and Timeline projections as chronological Markdown lists.
- Preserve tenant, policy, query, read-your-writes, cache, ETag, and redaction behavior from Stories 8-1 through 8-3 and Epic 5.
- Keep Markdown output deterministic, bounded, localization-safe, and suitable for LLM chat surfaces. Do not build schema fingerprints, skill corpus resources, or a full renderer abstraction redesign in this story.

---

## Story

As an LLM agent,
I want to read projection data rendered as structured Markdown consumable through chat surfaces,
so that I can present domain data to users in a readable format without parsing raw JSON.

### Adopter Job To Preserve

An adopter should keep defining projections once with FrontComposer attributes and metadata. When MCP is enabled, agents should receive readable Markdown for the same projection roles used by the web shell, with the same labels, formatting intent, tenant isolation, and lifecycle read-your-writes discipline. The adopter must not hand-maintain separate chat templates or duplicate projection formatters.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A projection with Default role or no explicit role is exposed through Story 8-1 MCP resources | An authenticated and authorized agent reads the resource | The response is `text/markdown` and renders a deterministic Markdown table using SourceTools column order and label resolution. |
| AC2 | A projection with ActionQueue role hint is read by an agent | Items are returned from the existing query seam | The response renders a Markdown table optimized for pending action review, preserving ActionQueue role filtering, command CTA metadata, and badge text while not invoking commands. |
| AC3 | A projection with StatusOverview role hint is read by an agent | Aggregate status fields or badge mappings are available | The response renders a compact Markdown status card/summary with totals, semantic badge labels, and source field labels. |
| AC4 | A projection with Timeline role hint is read by an agent | Items contain chronological fields | The response renders a Markdown timeline sorted by the same deterministic date/time ordering expected by the web surface, with timestamps, status labels, and event descriptions. |
| AC5 | Projection columns include nullable values, enums, booleans, numeric values, date/time-like values, relative-time display hints, or unsupported placeholders | Markdown cells are rendered | Formatting follows the same SourceTools/web metadata: labels from the label chain, em dash-equivalent `-` for nulls, humanized enum/member text, invariant protocol identifiers, and deterministic culture handling. |
| AC6 | Badge mappings are present | Badge values are rendered for agents | Badge output is text-only using the six semantic slots `Neutral`, `Info`, `Success`, `Warning`, `Danger`, and `Accent`; no colors, icons, CSS classes, tenant values, or renderer-specific markup are required. |
| AC7 | A projection query returns zero items | The agent reads the resource | The response returns a meaningful empty state such as `No [entities] found.` and includes safe available command suggestions only when Story 8-2 visibility confirms the commands are visible for the current tenant/policy scope. |
| AC8 | A projection read follows a Story 8-3 terminal command result | The agent reads the affected projection | The read uses the existing `IQueryService`/EventStore query path and respects read-your-writes, ETag/cache, SignalR/polling, and degraded/fallback classifications without adding an MCP-specific query backend. |
| AC9 | A projection resource is hidden, unknown, stale, unauthorized, cross-tenant, malformed, oversized, or policy-filtered | The read request reaches Story 8-4 code | The response uses Story 8-2 hidden/unknown semantics, performs no query side effects, and leaks no hidden names, tenant IDs, user IDs, claims, tokens, query filters, resource-existence hints, or raw exception text. |
| AC10 | Projection output is generated repeatedly for the same query result and descriptor set | Results are compared across runs | Markdown ordering, heading levels, table column order, status grouping, timeline order, truncation text, and serialization are deterministic and independent of assembly enumeration order, current UI culture, tenant/user values, and service instance order. |
| AC11 | Query results exceed configured size, row, column, or character limits | Markdown is rendered | Output is bounded by explicit options and returns sanitized truncation/pagination hints without revealing hidden row counts or hidden tenant/resource existence. |
| AC12 | MCP SDK resource DTOs or transport APIs change | Story 8-4 maps internal renderer results to SDK output | Internal Markdown renderer contracts and tests remain SDK-neutral; SDK DTO conversion stays inside `Hexalith.FrontComposer.Mcp`. |
| AC13 | Accessibility/localization-sensitive labels and descriptions exist in SourceTools IR | Markdown output is produced | Human-readable text preserves existing labels/descriptions and localization keys where available, while MCP resource names, URIs, and protocol identifiers remain invariant technical contracts. |
| AC14 | Story 8-4 completes | Later Epic 8 stories continue | Projection Markdown rendering is reusable by skill corpus resources, schema versioning, and agent E2E benchmarks without redesigning Story 8-1 descriptor or Story 8-3 lifecycle contracts. |

---

## Tasks / Subtasks

- [ ] T1. Define SDK-neutral Markdown projection renderer contracts (AC1-AC6, AC10, AC12, AC14)
  - [ ] Add package-owned models such as `McpProjectionRenderRequest`, `McpProjectionRenderResult`, `McpMarkdownProjectionDocument`, `McpMarkdownTable`, `McpMarkdownStatusSummary`, and `McpMarkdownTimeline`.
  - [ ] Keep contracts SDK-neutral until the final MCP resource adapter edge; do not expose MCP SDK DTOs from Contracts, SourceTools, or generated descriptors.
  - [ ] Represent rendered output as `text/markdown` plus safe metadata: projection identifier, role, bounded context, row count category, truncation state, and correlation/request ID when already available.
  - [ ] Exclude tenant IDs, user IDs, claims, roles, API keys, tokens, raw query filters, hidden resource names, service instances, `ClaimsPrincipal`, and raw exception text from renderer contracts.
  - [ ] Define deterministic Markdown serialization rules for headings, tables, escaped cell text, status summaries, timelines, empty states, and truncation notices.

- [ ] T2. Reuse SourceTools projection metadata as the render source of truth (AC1-AC6, AC10, AC13)
  - [ ] Consume the same projection descriptor/manifest data emitted by Story 8-1 rather than reflecting over arbitrary loaded assemblies at request time.
  - [ ] Use `RazorModel`, `ColumnModel`, `ProjectionRenderStrategy`, badge mappings, entity labels, plural labels, empty-state CTA command names, descriptions, `FieldDisplayFormat`, and SourceTools label resolution.
  - [ ] Preserve column order from the SourceTools transform: priority ordering and declaration-order tiebreaks where already defined by web rendering.
  - [ ] Reuse web label/humanizer behavior. Do not add an MCP-only label parser or hard-coded adapter copy when IR metadata exists.
  - [ ] Add manifest/descriptor tests proving Markdown projection metadata is single-source with web projection metadata and stable across repeated builds.

- [ ] T3. Implement Default and ActionQueue Markdown tables (AC1, AC2, AC5-AC8, AC10, AC11)
  - [ ] Render Default projections as GitHub-flavored Markdown tables with deterministic headers, escaped cell values, and stable row ordering from the query result.
  - [ ] Render ActionQueue projections with the same table baseline plus pending-action context, safe CTA command labels, and semantic badge text where visible.
  - [ ] Format null values as `-`, enums as human-readable labels, booleans as deterministic text, dates/times using configured agent-surface culture/invariant fallback, numeric values with deterministic precision, and unsupported values through existing placeholder semantics.
  - [ ] Do not include Markdown links or command suggestions that would bypass Story 8-2 tenant/policy visibility.
  - [ ] Add tests for Markdown escaping of pipes, backticks, brackets, HTML-like text, newlines, RTL/BOM/control characters, long words, and secret-looking payload fragments.
  - [ ] Add bounds for max rows, max columns, max cell characters, max document characters, and truncation/paging hints.

- [ ] T4. Implement StatusOverview Markdown summaries (AC3, AC5, AC6, AC10, AC11)
  - [ ] Map status/badge fields to text-only semantic slots: `Neutral`, `Info`, `Success`, `Warning`, `Danger`, and `Accent`.
  - [ ] Render totals and grouped counts without colors, icons, CSS class names, or UI-only tokens.
  - [ ] Preserve source labels and status text from projection metadata; fall back deterministically when metadata is absent.
  - [ ] Define behavior for missing badge mappings, unknown status values, unsupported fields, empty groups, and multiple status fields.
  - [ ] Add approval tests covering stable grouping order, totals, zero-count suppression policy, and redaction.

- [ ] T5. Implement Timeline Markdown rendering (AC4-AC6, AC10, AC11)
  - [ ] Reuse the web timeline field selection rule where available, including first suitable date/time field and deterministic tiebreaks.
  - [ ] Render chronological entries with timestamp, status label, primary title/description, and selected supporting fields.
  - [ ] Define ascending vs. descending order explicitly and keep it stable across repeated reads.
  - [ ] Handle missing timestamps, duplicate timestamps, unsupported event fields, null descriptions, and large timelines with bounded output.
  - [ ] Add tests for timestamp ordering, duplicate ordering, badge text, multiline descriptions, truncation, and no raw tenant/user leakage.

- [ ] T6. Wire MCP resource reads to the query and lifecycle seams (AC7-AC9, AC11, AC12)
  - [ ] Route all projection reads through existing `IQueryService.QueryAsync<T>` or the Story 8-1 projection adapter; do not add a second EventStore or REST query client.
  - [ ] Accept only Story 8-1/8-2 approved resource names, URI templates, and query parameters. Reject raw tenant/user overrides and descriptor overrides.
  - [ ] Revalidate current auth, tenant, resource visibility, and policy scope for every read; previous list results, lifecycle terminal results, and copied URIs do not bypass visibility.
  - [ ] Preserve cancellation tokens, request IDs, ETag/cache discriminator rules, pagination, and degraded EventStore categories.
  - [ ] Map successful render output to MCP C# SDK resource contents inside the MCP package, using `text/markdown` text resources.
  - [ ] Treat malformed resource URIs, stale descriptors, oversized query parameters, hidden resources, unauthorized resources, query failures, timeouts, and cancellations as deterministic sanitized categories.

- [ ] T7. Empty states, command suggestions, and scope discipline (AC7, AC9, AC13, AC14)
  - [ ] Return `No [entities] found.` using entity plural labels from SourceTools when the result set is empty.
  - [ ] Include safe command suggestions only from the Story 8-2 visible catalog, only when already represented by projection empty-state CTA metadata or explicit visible descriptors.
  - [ ] Never suggest hidden, unauthorized, cross-tenant, stale, destructive, or policy-filtered commands.
  - [ ] Do not implement fuzzy command suggestions, hallucination correction, or tenant-scoped listing rules here; consume Story 8-2 results only.
  - [ ] Keep rich natural-language advice bounded and deterministic. No per-agent memory, dynamic planning, or generated prose that depends on request history.

- [ ] T8. Security, redaction, and bounded runtime state (AC8-AC11)
  - [ ] Redact JWT-like strings, API keys, client secrets, claims, role names, tenant IDs, user IDs, raw query filters, command payload fragments, provider internals, and exception text from Markdown, logs, and telemetry.
  - [ ] Add zero-side-effect tests proving hidden/unknown/malformed/unauthorized/stale resource reads do not query EventStore, mutate cache, relay tokens, update SignalR state, allocate renderer state, or emit sensitive telemetry.
  - [ ] Make descriptor registries, renderer lookup tables, response buffers, and render caches immutable after startup or bounded by explicit options.
  - [ ] Add options for max projection rows, max cell length, max Markdown bytes/chars, max timeline entries, max status groups, and truncation behavior with range validation.
  - [ ] Log sanitized outcome categories and duration buckets only; do not log rendered row payloads.

- [ ] T9. Tests and verification (AC1-AC14)
  - [ ] Markdown renderer unit tests for Default, ActionQueue, StatusOverview, and Timeline roles.
  - [ ] SourceTools parity tests for labels, descriptions, column ordering, badge mappings, entity labels, empty-state CTA metadata, field display formats, and unsupported placeholders.
  - [ ] MCP resource adapter tests for direct resources vs. resource templates, `text/markdown` content mapping, SDK boundary containment, and cancellation propagation.
  - [ ] Query integration tests covering two sample projections, tenant context injection, pagination, ETag/cache behavior, read-your-writes after Story 8-3 terminal confirmation, and degraded query outcomes.
  - [ ] Hidden/unknown/unauthorized/stale resource tests consuming Story 8-2 semantics and proving no query or render side effects.
  - [ ] Markdown safety tests for escaping, control characters, oversized rows, secret-looking values, raw exception text, tenant/user values, and deterministic truncation.
  - [ ] Snapshot/golden tests proving stable Markdown for all supported projection roles across repeated runs.
  - [ ] Regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.
  - [ ] Targeted tests: `tests/Hexalith.FrontComposer.Mcp.Tests`, `tests/Hexalith.FrontComposer.SourceTools.Tests`, and Shell/EventStore query tests only if shared query/render seams change.

---

## Dev Notes

### Existing State To Preserve

| File / Area | Current state | Preserve / Change |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Mcp` | Story 8-1 owns MCP hosting, descriptor registry, SDK containment, and basic projection resource adapter. | Add Markdown projection rendering here; keep SDK conversion at this boundary. |
| Story `8-2-hallucination-rejection-and-tenant-scoped-tools` | Owns visible catalog, hidden/unknown semantics, stale descriptor/list replay rejection, and tenant/policy filtering. | Projection reads and suggestions must consume these services, not duplicate or weaken them. |
| Story `8-3-two-call-lifecycle-and-agent-command-semantics` | Owns command terminal tracking and read-your-writes sequence before projection reads. | Projection rendering assumes terminal confirmation where required and does not implement lifecycle polling. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModel.cs` | Carries projection type, namespace, bounded context, columns, render strategy, state filters, entity labels, and empty-state CTA command name. | Use as the primary metadata source for Markdown projection rendering. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/ColumnModel.cs` | Carries property name, header, type category, format hints, nullability, badge mappings, enum names, priority, group, description, unsupported type, display format, and relative-time window. | Reuse this metadata for Markdown cell formatting and table/status/timeline structure. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/ProjectionRenderStrategy.cs` | Maps Default, ActionQueue, StatusOverview, DetailRecord, Timeline, and Dashboard web strategies. | Implement Default/ActionQueue/StatusOverview/Timeline. DetailRecord can render through minimal table/summary fallback unless implementation proves a coherent low-cost path. Dashboard remains reserved/deferred. |
| `src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionRole.cs` | Public projection role hints: ActionQueue, StatusOverview, DetailRecord, Timeline, Dashboard. | Do not change public role semantics for MCP; map existing roles to Markdown behavior. |
| `src/Hexalith.FrontComposer.Contracts/Attributes/BadgeSlot.cs` | Six semantic badge slots. | Render slot names as text-only labels for agents. |
| `src/Hexalith.FrontComposer.Contracts/Communication/IQueryService.cs` and `QueryRequest.cs` | Query seam supports projection type, tenant context, filters, paging, ETag/cache, sort, EventStore query fields, and cache discriminator rules. | MCP resources must query through this seam and must not accept raw tenant/user overrides from clients. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/IRenderer.cs` | Existing generic renderer contract already anticipates `string` output for MCP agents. | Use only if it fits without expanding Story 8-4 into Story 8-6's renderer abstraction redesign. |
| `src/Hexalith.FrontComposer.Shell/Components/Badges/*` | Web badge components render Fluent UI colors/appearance. | Do not reuse visual tokens in Markdown; consume semantic slot metadata only. |
| `src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs` | Web empty-state command CTA resolution. | Use only through visibility-safe metadata or Story 8-2 visible catalog; no direct hidden command probing. |

### Architecture Contracts

- Markdown rendering is a surface adapter over projection query results and SourceTools metadata, not a new projection model.
- SourceTools/web metadata remains the source of truth for labels, ordering, role hints, badge mappings, nullability, and display intent.
- MCP resources return text resources with `text/markdown`; JSON remains an internal/query shape, not the public agent presentation in this story.
- Every projection read revalidates current authentication, tenant context, resource visibility, and policy scope.
- Read-your-writes belongs to Story 8-3 and Epic 5 query/reconciliation behavior. Story 8-4 renders the resulting projection read; it does not create a second lifecycle or polling subsystem.
- Markdown is deterministic and bounded. No request-time unbounded caches, per-agent descriptor accumulation, or culture/tenant-specific manifest caches.
- SDK volatility containment follows Story 8-1: official MCP C# SDK types stay at the `Hexalith.FrontComposer.Mcp` edge.

### Proposed Projection Read Flow

1. Agent receives a visible projection resource or template from Story 8-2 filtered catalog.
2. Agent reads the resource after authentication and, when applicable, after Story 8-3 terminal command confirmation.
3. MCP adapter revalidates auth, tenant, policy, descriptor epoch, URI/template parameters, query bounds, and visibility.
4. Adapter builds a `QueryRequest` with tenant context from authenticated state, not client-supplied tenant input.
5. Existing `IQueryService` / EventStore query path returns items, total count, ETag, and degraded/not-modified classifications where available.
6. Markdown renderer combines query results with SourceTools projection metadata.
7. MCP package maps the SDK-neutral render result to `text/markdown` resource contents.
8. Bounded sanitized errors are returned for unknown, hidden, stale, unauthorized, malformed, oversized, timeout, cancellation, and downstream failures.

### Binding Decisions

| Decision | Rationale |
| --- | --- |
| D1. Markdown output lives in `Hexalith.FrontComposer.Mcp`. | Keeps agent presentation with the MCP package and avoids dragging MCP concerns into Contracts or SourceTools public surfaces. |
| D2. SourceTools projection IR is the renderer source of truth. | Prevents web/MCP drift and avoids runtime reflection as canonical metadata. |
| D3. `text/markdown` is the public MCP content type for projection reads. | Aligns with FR53 and chat-agent consumption while keeping structured query results internal. |
| D4. Default and ActionQueue render as tables. | Matches the existing web grid mental model and is easy for agents/users to inspect. |
| D5. StatusOverview renders text-only status summaries. | Preserves semantic badge meaning without CSS/color dependencies. |
| D6. Timeline renders chronological Markdown lists. | Preserves the event-sequence job without forcing agents to infer order from raw JSON. |
| D7. Empty-state suggestions consume Story 8-2 visible catalog only. | Prevents hidden command leakage and avoids reimplementing hallucination logic. |
| D8. Query and read-your-writes behavior stays on existing seams. | Avoids a second EventStore client and keeps Epic 5/Story 8-3 contracts authoritative. |
| D9. Renderer output is bounded by options. | Applies L14 and prevents long-lived or large projections from producing unbounded chat payloads. |
| D10. Protocol identifiers are invariant; display text can be localized metadata. | Separates machine contracts from human-readable labels and avoids per-request culture drift. |
| D11. Markdown escaping is a security boundary. | Prevents table injection, hidden links, prompt-like payloads, and secret-looking content from becoming agent instructions. |
| D12. SDK DTO mapping remains at the MCP edge. | Protects tests and internal render contracts from MCP C# SDK transport churn. |

### Markdown Shape Examples

Default/ActionQueue table shape:

```markdown
## Shipments

| Status | Customer | Estimated dispatch | Action |
| --- | --- | --- | --- |
| Warning: Pending | ACME | 2026-05-01 14:30 | Release shipment |
```

StatusOverview shape:

```markdown
## Shipment status

- Total: 42
- Success: 31 confirmed
- Warning: 8 pending
- Danger: 3 blocked
```

Timeline shape:

```markdown
## Shipment timeline

- 2026-05-01 14:30 - Warning: Shipment delayed. Carrier confirmation pending.
- 2026-05-01 13:10 - Info: Shipment assigned. Route calculated.
```

Examples are illustrative. Implementation must derive labels, fields, order, badge text, and formatting from SourceTools/query metadata.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 8-1 | Story 8-4 | MCP resource descriptors, URI/template shape, projection adapter boundary, SDK containment, and minimal projection read seam. |
| Story 8-2 | Story 8-4 | Visible catalog, tenant/policy filtering, hidden/unknown semantics, stale descriptor handling, and safe command suggestions. |
| Story 8-3 | Story 8-4 | Terminal command lifecycle and read-your-writes sequence before projection reads. |
| Epic 4 | Story 8-4 | Projection roles, column metadata, badge mappings, labels, empty-state metadata, field formats, ordering, and placeholder semantics. |
| Epic 5 | Story 8-4 | EventStore query path, ETag/cache, SignalR/polling reconciliation, degraded categories, cancellation, and telemetry. |
| Stories 7-1 through 7-3 | Story 8-4 | Authenticated principal, canonical tenant context, and policy scope checks. |
| Story 8-4 | Stories 8-5 and 8-6 | Markdown projection output can be referenced by skill corpus guidance and versioned by future schema fingerprints. |
| Story 10-2 / Story 10-6 | Story 8-4 | Agent E2E and benchmark lanes consume stable projection Markdown contracts. |

### Scope Guardrails

Do not implement these in Story 8-4:

- Fuzzy hallucination suggestions, closest-match ranking, or full tenant-scoped list behavior. Owner: Story 8-2.
- Command lifecycle polling, terminal-state guarantee, or idempotent command semantics. Owner: Story 8-3.
- Versioned skill corpus resources or agent code-generation instructions. Owner: Story 8-5.
- Schema fingerprints, migration delta diagnostics, client/server version negotiation, or full multi-surface renderer abstraction redesign. Owner: Story 8-6.
- New projection role attributes, new badge slots, custom per-agent templates, or dynamic LLM-authored Markdown templates. Owner: post-v1 or explicit future story.
- Renderer-specific demos for Claude Code, Codex, Cursor, Mistral, or native chat matrix. Owner: Story 10-2.
- New command policy language, policy UI, or backend authorization engine. Owner: Story 7-3 or later authorization follow-up.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| MCP-discoverable skill corpus explaining projection Markdown conventions. | Story 8-5 |
| Schema fingerprints and version negotiation for Markdown projection contracts. | Story 8-6 |
| Deep agent E2E across command, lifecycle, and projection rendering. | Story 10-2 |
| Signed LLM benchmark releases and agent projection-read quality gates. | Story 10-6 |
| Full five-renderer chat matrix demos. | Story 10-2 |
| Custom adopter Markdown templates for agents. | Post-v1 customization backlog |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-8-mcp-agent-integration.md#Story-8.4`] - story statement, AC foundation, FR53 scope, and UX-DR35/UX-DR44 references.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR53`] - Markdown projection rendering for agents.
- [Source: `_bmad-output/planning-artifacts/prd/user-journeys.md#Journey-5`] - agent reads structured Markdown projection data before acting.
- [Source: `_bmad-output/planning-artifacts/prd/product-scope.md#Multi-surface-foundation`] - Markdown tables, status cards, and timelines in v1 alpha scope.
- [Source: `_bmad-output/planning-artifacts/architecture.md#MCP-Interaction-Model`] - projections as MCP resources returning structured Markdown.
- [Source: `_bmad-output/implementation-artifacts/8-1-mcp-server-and-typed-tool-exposure.md`] - MCP package, resource descriptors, projection adapter, and deferred rich Markdown scope.
- [Source: `_bmad-output/implementation-artifacts/8-2-hallucination-rejection-and-tenant-scoped-tools.md`] - tenant-scoped visibility, hidden/unknown semantics, and projection-resource follow-up.
- [Source: `_bmad-output/implementation-artifacts/8-3-two-call-lifecycle-and-agent-command-semantics.md`] - lifecycle/read-your-writes handoff into projection reads.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L03`] - tenant/user fail-closed guard.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L04`] - generated name collision detection.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10`] - deferrals need story specificity.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L14`] - bounded runtime state.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModel.cs`] - projection role and entity metadata.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Transforms/ColumnModel.cs`] - column, badge, display, and placeholder metadata.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Transforms/ProjectionRenderStrategy.cs`] - web projection strategy mapping.
- [Source: `src/Hexalith.FrontComposer.Contracts/Communication/IQueryService.cs`] - existing query seam.
- [Source: `src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs`] - tenant/filter/paging/ETag/cache query contract.
- [Source: MCP C# SDK resources documentation](https://csharp.sdk.modelcontextprotocol.io/concepts/resources/resources.html) - direct resources, resource templates, and `TextResourceContents`/`text/markdown`.
- [Source: Official MCP C# SDK API docs](https://csharp.sdk.modelcontextprotocol.io/api/ModelContextProtocol.AspNetCore.html) - ASP.NET Core Streamable HTTP adapter boundary.
- [Source: NuGet `ModelContextProtocol.AspNetCore` 1.2.0](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore/) - current package version and .NET 8/9/10 compatibility as of 2026-05-01.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-01: Story created via `/bmad-create-story 8-4-projection-rendering-for-agents` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### File List

(to be filled in by dev agent)
