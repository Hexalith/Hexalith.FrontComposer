# Story 8.6: Schema Versioning & Multi-Surface Abstraction

Status: ready-for-dev

> **Epic 8** - MCP & Agent Integration. Covers **FR59**, **FR60**, **FR61**, and the versioning handoff from Stories **8-1** through **8-5**. Builds on SDK-neutral MCP descriptors, Story 8-2 stale descriptor handling, Story 8-3 lifecycle schema stability, Story 8-4 Markdown projection contracts, Story 8-5 corpus/resource versioning, and existing customization contract-version patterns. Applies lessons **L01**, **L03**, **L04**, **L06**, **L07**, **L08**, **L10**, and **L14**.

---

## Executive Summary

Story 8-6 makes FrontComposer version-aware across web, MCP, skill corpus, and future renderer surfaces without making clients guess whether a schema changed:

- Emit deterministic schema fingerprints for generated MCP manifests, projection resource contracts, command tool schemas, lifecycle result schemas, skill corpus manifests, and renderer contract metadata.
- Compare client/server fingerprints through a sanitized negotiation contract that distinguishes exact match, compatible additive drift, incompatible drift, unknown/unsupported versions, and stale descriptor epochs.
- Produce actionable migration delta diagnostics when a prior known schema differs from the current generated schema.
- Define the multi-surface rendering abstraction as a small, SDK-neutral contract around SourceTools IR and renderer capabilities, while leaving actual non-web surfaces to their owning stories.
- Keep fingerprints structural, bounded, deterministic, and free of tenant/user/runtime data.

---

## Story

As a developer,
I want schema hash fingerprints and a rendering abstraction that enable graceful version negotiation across deployments,
so that framework version mismatches between client and server degrade gracefully instead of breaking silently.

### Adopter Job To Preserve

An adopter should be able to upgrade a FrontComposer package, run the generator, and see clear compile-time or runtime evidence when a generated web/MCP/skill/rendering contract no longer matches a deployed client or cached artifact. They should receive a stable diagnostic with what changed, why it matters, and how to remediate, not a runtime crash, hidden MCP failure, or stale agent behavior.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A command, projection, lifecycle result, skill corpus manifest, or renderer descriptor is generated | SourceTools emits the contract metadata | A deterministic schema fingerprint is emitted from canonical structural fields only. |
| AC2 | The same domain source is generated twice on the same framework version | Fingerprints are compared | The fingerprint values are byte-for-byte identical regardless of assembly enumeration order, dictionary order, path separators, current culture, user, tenant, or build machine. |
| AC3 | Display labels, descriptions, command titles, projection fields, parameter constraints, resource URIs, lifecycle result fields, or skill resource manifests change | The fingerprint input changes | The affected contract fingerprint changes and the diagnostic delta identifies the changed structural field. |
| AC4 | Runtime tenant IDs, user IDs, claims, tokens, API keys, command payload values, query rows, cache keys, ETags, local paths, timestamps, service instances, or raw exception text exist | Fingerprints are computed or logged | None of those runtime values are included in fingerprint material, migration deltas, logs, telemetry, or agent-visible negotiation output. |
| AC5 | An MCP client sends or caches an older manifest/resource/tool/lifecycle/schema fingerprint | The server compares it with the current generated descriptor | Exact matches proceed normally, compatible additive drift returns a sanitized warning category, and incompatible drift returns a sanitized schema-mismatch category without invoking command/query side effects. |
| AC6 | A projection or MCP tool manifest fingerprint mismatch is detected | Migration delta diagnostics run | A diagnostic names what changed, impact category, compatibility decision, and remediation path using stable HFC diagnostic IDs and docs links. |
| AC7 | The migration delta tool has a known previous schema snapshot | It compares old and new canonical shapes | It classifies added optional fields, added required fields, removed fields, type/category changes, enum/member changes, validation constraint changes, URI/name changes, renderer role changes, and lifecycle result schema changes. |
| AC8 | No trusted previous schema snapshot exists | A mismatch or unknown client fingerprint is seen | The system fails closed to a sanitized unknown-version category and tells maintainers how to capture or register a baseline without exposing hidden resource names or tenant data. |
| AC9 | Story 8-2 hidden/unknown/stale descriptor semantics apply | A client supplies a stale or mismatched fingerprint for a hidden/unauthorized/cross-tenant resource | Hidden/unknown equivalence wins; the response must not confirm that the hidden resource exists just because a schema fingerprint was supplied. |
| AC10 | Story 8-3 lifecycle results are serialized for agents | Their schema evolves | Lifecycle result schema fingerprints and compatibility decisions are stable and do not rely on client timestamps, arrival order, or agent-supplied state. |
| AC11 | Story 8-4 Markdown projection rendering produces `text/markdown` output | Its renderer contract changes | Markdown renderer contract fingerprints capture role, output content type, renderer capability, bounds, and safe metadata shape without hashing rendered row data. |
| AC12 | Story 8-5 skill corpus resources are packaged | The corpus manifest changes | Skill corpus manifest/resource fingerprints are available for compatibility checks while preserving Story 8-5's SDK-neutral source and external-team-overlay boundary. |
| AC13 | A renderer abstraction is defined | Web, MCP Markdown, skill resource, and future surfaces consume it | Composition logic is decoupled from surface-specific renderer adapters through a narrow SDK-neutral contract with explicit capability, output content type, schema fingerprint, and bounds metadata. |
| AC14 | v1 ships only existing web and MCP Markdown paths through the abstraction | Future surfaces are not implemented yet | The abstraction supports future surfaces without requiring a redesign, but Story 8-6 does not build new chat clients, IDE clients, custom per-agent templates, or streaming renderers. |
| AC15 | MCP SDK DTOs, protocol revisions, or C# SDK package versions change | FrontComposer maps descriptors and resources | Fingerprint inputs and renderer contracts remain SDK-neutral; SDK DTO mapping stays inside `Hexalith.FrontComposer.Mcp`. |
| AC16 | Fingerprint computation, snapshot comparison, or migration diagnostics fail | The caller receives a result | Failures are bounded, sanitized, deterministic, and never fall back to "proceed anyway" for incompatible or unknown schema state. |
| AC17 | A breaking schema delta is detected for shipped skill corpus examples or public descriptors | The release/build runs | The build requires a migration guide or explicit deferral owner before package publication. |
| AC18 | Tests execute across web, SourceTools, MCP, Shell, and corpus fixtures | Fingerprint and negotiation behavior is checked | P0 coverage proves deterministic hashing, no runtime data in fingerprints, hidden/unknown precedence, compatible/incompatible classification, actionable diagnostics, and SDK-boundary containment. |

---

## Tasks / Subtasks

- [ ] T1. Define canonical schema fingerprint input models (AC1-AC4, AC10-AC12, AC15)
  - [ ] Add SDK-neutral structural models for command tool schema, projection resource schema, lifecycle result schema, Markdown renderer contract, skill corpus manifest/resource schema, and aggregate MCP manifest schema.
  - [ ] Keep models pure data: stable names, bounded context, FQN, schema version, protocol name/URI, parameters/fields, required/nullable flags, type category, validation constraints, enum values, role/capability, output content type, bounds, and compatibility policy.
  - [ ] Exclude runtime state by type and by tests: no tenant/user/claim/token/payload/query row/cache/ETag/local path/timestamp/service object.
  - [ ] Define one canonical JSON serialization policy: ordinal property order, invariant culture, normalized newlines, normalized protocol identifiers, no indentation-sensitive meaning, and versioned root discriminator.
  - [ ] Add a `SchemaFingerprintAlgorithm` identifier so future hashing changes are detectable without pretending hashes are comparable.

- [ ] T2. Implement deterministic fingerprint generation in SourceTools (AC1-AC4, AC11, AC12)
  - [ ] Extend `McpManifestTransform` or a companion transform to compute fingerprints from the generated descriptor models, not from runtime reflection or loaded SDK DTOs.
  - [ ] Emit per-command, per-resource, lifecycle-result, renderer-contract, skill-corpus-manifest, and aggregate manifest fingerprints where the owning artifact exists.
  - [ ] Use a stable cryptographic hash such as SHA-256 over the canonical payload; expose the fingerprint as a lower-case hex string plus algorithm id.
  - [ ] Add collision and ordering tests for protocol names, projection URIs, fields, parameters, enum values, validation constraints, resource ordering, and renderer capability ordering.
  - [ ] Ensure any protocol-name/URI disambiguation from Stories 8-1/8-2 is reflected before hashing.

- [ ] T3. Extend SDK-neutral descriptor contracts without breaking package boundaries (AC1, AC5, AC10-AC15)
  - [ ] Add optional fingerprint metadata to `McpManifest`, `McpCommandDescriptor`, `McpResourceDescriptor`, and equivalent lifecycle/renderer/corpus descriptors only where those records are already stable public contracts.
  - [ ] Keep `Hexalith.FrontComposer.Contracts` dependency-free. Do not reference MCP SDK, Shell components, Fluent UI types, source-generator internals, EventStore clients, or docs infrastructure from Contracts.
  - [ ] Add backwards-compatible constructors or companion records if existing consumers need source compatibility during the transition.
  - [ ] Version descriptor schema constants deliberately; do not overload `McpManifestTransform.SchemaVersion = "frontcomposer.mcp.v1"` to mean every nested renderer/corpus schema version.
  - [ ] Add API compatibility tests for public records and SourceTools compile tests for generated manifest output.

- [ ] T4. Implement negotiation and stale-fingerprint handling in the MCP adapter (AC5, AC8, AC9, AC15, AC16)
  - [ ] Define internal negotiation results: `Exact`, `CompatibleAdditive`, `Incompatible`, `UnknownClientVersion`, `UnknownServerBaseline`, `HiddenOrUnknown`, `StaleDescriptor`, `UnsupportedAlgorithm`, and `Unavailable`.
  - [ ] Accept client fingerprint hints only as hints. Never trust them as proof of current visibility, authorization, tenant scope, or descriptor freshness.
  - [ ] Reuse Story 8-2 admission order: visibility, tenant, policy, descriptor/catalog epoch, and request bounds are validated before side effects; hidden/unknown equivalence overrides schema mismatch reporting.
  - [ ] Return sanitized agent-visible categories such as `schema-compatible-warning`, `schema-mismatch`, or `projection temporarily unavailable`; no hidden names, tenant identifiers, policy names, exact hidden counts, raw baseline paths, or exception text.
  - [ ] Add zero-side-effect tests proving incompatible/unknown/stale negotiation does not invoke command dispatch, query execution, lifecycle mutation, cache writes, or renderer buffers.

- [ ] T5. Build migration delta diagnostics and baselines (AC6-AC8, AC16, AC17)
  - [ ] Define a checked-in baseline snapshot format for shipped public schema shapes. Store only canonical structural data and fingerprint metadata, not generated code blobs or runtime payloads.
  - [ ] Compare old/new snapshots and classify deltas into additive-compatible, compatible-warning, breaking, unknown, or unsupported-algorithm.
  - [ ] Emit HFC diagnostics with the existing Expected/Got/Fix/DocsLink style: changed field/path, old value category, new value category, impact, and remediation.
  - [ ] Map migration-guide requirements for breaking public descriptor, skill corpus example, renderer contract, lifecycle result, and projection resource deltas.
  - [ ] Keep CLI UX to Story 9-2 unless a minimal test-only runner is needed; Story 8-6 owns the library contract and build/test diagnostics.

- [ ] T6. Define the multi-surface rendering abstraction contract (AC11, AC13-AC15)
  - [ ] Create or refine a narrow abstraction around `IRenderer<TModel,TOutput>` with a surface-neutral `FrontComposerRenderContract`, `RenderSurfaceKind`, `RenderCapability`, `RenderSchemaFingerprint`, `RenderOutputContentType`, and bounded metadata.
  - [ ] Use SourceTools IR and generated descriptors as the model source of truth. Do not make Shell components, Fluent UI tokens, Razor component instances, or MCP SDK DTOs canonical renderer input.
  - [ ] Represent current supported outputs honestly: web/Blazor render path and MCP Markdown projection output. Future chat/IDE/custom templates are capability placeholders, not implemented outputs.
  - [ ] Define compatibility rules for renderer contract changes: additive capability, removed capability, changed output content type, changed bounds, changed role support, changed field metadata, and changed sanitization guarantees.
  - [ ] Add tests proving renderer abstraction metadata can describe Story 8-4 Markdown contracts without hashing rendered projection rows.

- [ ] T7. Integrate skill corpus and docs migration hooks (AC12, AC17)
  - [ ] Consume Story 8-5 corpus manifest metadata when present and compute corpus/resource fingerprints from source/manifest structure.
  - [ ] Require migration-guide metadata when public skill examples or corpus resource schemas break, aligned with FR69.
  - [ ] Do not merge team-specific external skill overlays into framework fingerprints unless a future story defines an adopter-owned overlay baseline.
  - [ ] Emit diagnostics against corpus resource ids and source sections, not local absolute paths or raw prompt/provider output.

- [ ] T8. Tests and verification (AC1-AC18)
  - [ ] SourceTools unit tests for canonical serialization, stable SHA-256 output, ordering independence, changed-field sensitivity, algorithm-id changes, and no runtime field inclusion.
  - [ ] Manifest snapshot tests for command descriptor, projection resource descriptor, aggregate MCP manifest, lifecycle schema, renderer contract, and skill corpus fingerprints.
  - [ ] Negotiation tests for exact, additive, breaking, unsupported algorithm, unknown baseline, stale descriptor, hidden/unknown, unauthorized, cross-tenant, and malformed fingerprint hints.
  - [ ] Migration delta tests for added optional field, added required field, removed field, type/category change, enum change, validation constraint change, URI/name change, lifecycle result change, renderer capability change, and corpus resource change.
  - [ ] Security tests proving tenant IDs, user IDs, claims, tokens, payload values, query rows, local paths, ETags, timestamps, exception text, and provider internals never appear in fingerprints, deltas, logs, telemetry, or agent-visible responses.
  - [ ] API/package-boundary tests proving Contracts stays SDK-free, SourceTools computes canonical fingerprints, and `.Mcp` performs only adapter mapping.
  - [ ] Existing compatibility regression: `ProjectionSchemaCompatibilityFixtureTests` still deserialize supported projection fixtures and map incompatible payloads to the schema mismatch path.
  - [ ] Build regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.
  - [ ] Targeted tests: `tests/Hexalith.FrontComposer.SourceTools.Tests`, `tests/Hexalith.FrontComposer.Contracts.Tests`, `tests/Hexalith.FrontComposer.Mcp.Tests`, and Shell schema compatibility tests.

---

## Dev Notes

### Existing State To Preserve

| File / Area | Current state | Preserve / Change |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Mcp/McpManifest.cs` | SDK-neutral generated descriptor root with `SchemaVersion`, `Commands`, and `Resources`. | Extend with fingerprint metadata only through compatible public-contract changes. Keep SDK-free. |
| `McpCommandDescriptor` / `McpResourceDescriptor` | Carry protocol name/URI, FQN, bounded context, labels/descriptions, policy, fields/parameters. | Fingerprints must be derived from these structural fields after canonicalization. Do not add runtime values. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/McpManifestTransform.cs` | Current `SchemaVersion = "frontcomposer.mcp.v1"` and descriptor construction from SourceTools models. | Add fingerprint/canonical schema transforms here or in a companion type; do not use `.Mcp` runtime registry reflection as the schema source of truth. |
| `FrontComposerMcpDescriptorRegistry` | Loads generated manifests and enforces duplicate command/resource descriptors case-insensitively. | Preserve deterministic registry ordering and duplicate rejection. Add schema metadata without weakening duplicate detection. |
| `FrontComposerMcpProjectionReader` | Currently renders basic Markdown and returns `text/markdown` metadata. | Negotiation must happen before query/render side effects. Story 8-4 remains owner of rich Markdown rendering. |
| `IRenderer<TModel,TOutput>` | Provisional generic renderer contract already allows `RenderFragment` or `string` output. | Story 8-6 may refine companion metadata/contracts, but should not break existing implementers without migration diagnostics. |
| `CustomizationContractVersion` and projection template/slot/view contract versions | Existing packed major/minor/build comparison model and diagnostics for customization drift. | Reuse the comparison discipline where useful, but schema fingerprints are structural hashes, not just semantic version triplets. |
| Shell projection schema compatibility fixtures | Current/prior/forward/incompatible JSON fixtures prove deserialize behavior and mismatch exception path. | Reuse this as a regression baseline; do not replace EventStore payload compatibility with MCP-only fingerprints. |

### Architecture Contracts

- SourceTools owns canonical schema materialization. Runtime adapters can compare and map generated metadata, but they do not invent schema shape through reflection.
- Fingerprints are structural compatibility evidence, not authorization credentials. A matching hash never bypasses tenant, policy, visibility, descriptor epoch, or request validation.
- Hidden/unknown equivalence has higher priority than schema mismatch reporting for agent-visible responses.
- Migration diagnostics must be actionable: what changed, impact, and fix. Pure "hash mismatch" output is not enough.
- Renderer abstraction metadata is a compatibility layer, not permission to implement new surfaces in this story.
- Baselines are shipped framework contracts. They must be small, deterministic, reviewable, and free of runtime data.

### Canonical Fingerprint Material

Include:

- Contract family and schema version.
- Fingerprint algorithm id.
- Bounded context, public protocol name/URI, FQN, stable resource/tool id.
- Parameter/field name, type category, JSON type, required/nullable state, enum values, validation constraints, unsupported marker, label/description metadata category.
- Projection role, renderer capability, output content type, bounds category, lifecycle result field names/categories, skill corpus resource id/order/source marker metadata.

Exclude:

- Tenant ID, user ID, claims, roles, tokens, API keys, command payload values, query rows, cache keys, ETags, local absolute paths, timestamps, service instance ids, `ClaimsPrincipal`, `RenderContext`, raw exceptions, provider internals, and generated source text that includes machine-specific paths.

### Negotiation Contract

| Internal result | Agent-visible behavior | Side-effect rule |
| --- | --- | --- |
| `Exact` | Proceed normally. | Normal command/query/render path after admission. |
| `CompatibleAdditive` | Proceed with sanitized compatibility warning metadata where the protocol supports it. | No extra side effects beyond normal admitted operation. |
| `Incompatible` | Return sanitized schema mismatch with docs code/remediation category. | No command dispatch, query, lifecycle mutation, cache write, or renderer allocation. |
| `UnknownClientVersion` | Return sanitized unknown-version category and remediation guidance. | No side effects unless product later explicitly allows read-only compatibility fallback. |
| `UnknownServerBaseline` | Return maintainer-facing diagnostic; agent sees unavailable/schema category. | No side effects. |
| `HiddenOrUnknown` | Preserve Story 8-2 hidden/unknown response. | No side effects. |
| `StaleDescriptor` | Preserve stale/unavailable category. | No side effects. |
| `UnsupportedAlgorithm` | Return sanitized unsupported schema fingerprint category. | No side effects. |

### Migration Delta Categories

| Delta | Default compatibility | Required output |
| --- | --- | --- |
| Added optional field/parameter | Compatible warning | Name, category, affected contract, docs link. |
| Added required field/parameter | Breaking | Name, affected tool/resource, required remediation. |
| Removed field/parameter | Breaking | Removed name/category and replacement if known. |
| Type/category change | Breaking unless explicitly compatible | Old/new categories and affected renderer/tool/resource. |
| Enum/member addition | Compatible warning by default | Added values and consumer guidance. |
| Enum/member removal/rename | Breaking | Old/new values and migration path. |
| Validation constraint tightened | Breaking or warning by rule | Constraint id/category and impact. |
| Validation constraint relaxed | Compatible warning | Constraint id/category and risk note. |
| Protocol name/URI change | Breaking | Old/new identifier and redirect/alias guidance if available. |
| Renderer capability removed or content type changed | Breaking | Surface, capability, old/new output type. |
| Bounds tightened | Warning or breaking by severity | Old/new bounds and truncation impact. |
| Skill corpus resource id/order/source marker change | Warning or breaking by rule | Resource id and migration-guide requirement when examples break. |

### Multi-Surface Abstraction Shape

Recommended internal model names can change, but the contract should be equivalent to:

```csharp
public sealed record FrontComposerRenderContract(
    string ContractId,
    string ContractSchemaVersion,
    RenderSurfaceKind Surface,
    string OutputContentType,
    IReadOnlyList<RenderCapability> Capabilities,
    string SchemaFingerprint,
    string FingerprintAlgorithm,
    RenderBounds Bounds);
```

`RenderSurfaceKind` should honestly model shipped surfaces and placeholders: `WebBlazor`, `McpMarkdown`, `SkillResourceMarkdown`, and `Future`. Future values are not implemented behavior.

### Binding Decisions

| Decision | Rationale |
| --- | --- |
| D1. Fingerprints are generated from SourceTools canonical descriptor material. | Prevents runtime reflection, SDK DTOs, and loaded assembly order from becoming schema truth. |
| D2. SHA-256 over canonical JSON is the v1 fingerprint algorithm. | Stable, reviewable, available in BCL, and adequate for compatibility identifiers. |
| D3. Algorithm id is part of fingerprint metadata. | Avoids comparing hashes produced by incompatible canonicalization or algorithms. |
| D4. Fingerprints exclude runtime values by construction. | Applies L03/L14 and prevents PII/security leakage through diagnostics. |
| D5. Hidden/unknown precedence beats schema mismatch visibility. | Prevents fingerprint hints from becoming resource-existence or tenant leaks. |
| D6. Mismatch handling is side-effect-free until compatibility is accepted. | Prevents stale clients from dispatching commands or querying projections before admission. |
| D7. Migration deltas must explain changed structure, not just hash values. | FR60 requires remediation, not detection alone. |
| D8. Schema baselines are small structural snapshots. | Keeps release review practical and avoids committing runtime/generated noise. |
| D9. Renderer abstraction is metadata-first in Story 8-6. | Supports FR61 without building new renderer surfaces outside story scope. |
| D10. Existing customization contract-version discipline is reused but not conflated. | Version triplets help public override contracts; structural hashes handle generated schema drift. |
| D11. SDK DTO mapping remains in `.Mcp`. | Keeps Contracts and SourceTools stable against MCP SDK churn. |
| D12. Skill corpus fingerprints cover framework corpus only. | Team overlays are adopter data and need a separate future baseline story. |
| D13. Unknown baseline fails closed. | Prevents "no baseline, proceed anyway" from hiding real schema drift. |
| D14. Fingerprint computation is bounded and deterministic. | Applies L14 to generated metadata and avoids unbounded manifest/corpus growth. |

### Latest MCP Notes

- As of 2026-05-02, `ModelContextProtocol.AspNetCore` latest NuGet release remains `1.2.0` from 2026-03-27 and supports .NET 8/9/10. Keep package-specific DTO usage isolated in `.Mcp`.
- MCP servers expose tools, resources, and prompts; Story 8-6 should version FrontComposer's tools/resources/render contracts, not invent MCP protocol negotiation beyond safe descriptor metadata and adapter behavior.
- MCP tasks are experimental in protocol revision `2025-11-25`; do not base FrontComposer schema negotiation on task lifecycle semantics in this story.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 8-1 | Story 8-6 | SDK-neutral MCP descriptors, generated manifest emission, protocol names/URIs, descriptor registry ordering, and SDK boundary. |
| Story 8-2 | Story 8-6 | Hidden/unknown semantics, stale descriptor/category handling, no-side-effect admission, tenant/policy visibility precedence. |
| Story 8-3 | Story 8-6 | Lifecycle result schema, source-ordered transition metadata, and terminal result compatibility. |
| Story 8-4 | Story 8-6 | Markdown projection renderer contract, role support, bounds, content type, and inert-text/sanitization guarantees. |
| Story 8-5 | Story 8-6 | Skill corpus manifest/resource schema, migration-guide trigger for broken examples, and SDK-neutral resource source. |
| Story 9-2 | Story 8-6 | Future CLI inspection/migration command can consume the library and baselines created here. |
| Story 9-4 | Story 8-6 | Diagnostic ID docs and telemetry taxonomy can publish migration delta details. |
| Story 9-5 | Story 8-6 | Documentation site owns public migration pages and Diataxis IA. |
| Story 10-2 | Story 8-6 | Agent E2E consumes stable version negotiation behavior across clients. |
| Story 10-6 | Story 8-6 | Signed benchmark releases can include fingerprint/scorer/corpus versions. |

### Scope Guardrails

Do not implement these in Story 8-6:

- New MCP command invocation semantics, hallucination suggestions, or tenant-scoped enumeration. Owners: Stories 8-1 and 8-2.
- Command lifecycle orchestration, polling, idempotency, or restart recovery. Owner: Story 8-3.
- Rich Markdown projection rendering, streaming, partial responses, exact total-count policy, or renderer-owned cross-request Markdown cache. Owner: Story 8-4 or later numbered performance/transport story.
- Skill corpus authoring, benchmark prompt scoring, or live model provider execution. Owners: Story 8-5 and Story 10-6.
- Full CLI migration command or docs site publishing. Owners: Stories 9-2 and 9-5.
- Real IDE/chat client matrix demos, visual specimens, or agent browser tests. Owner: Story 10-2.
- Per-agent custom Markdown templates, LLM-authored renderers, team-overlay fingerprint baselines, or semantic LLM judging.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| CLI command for inspecting fingerprints and migration deltas. | Story 9-2 |
| Public diagnostic documentation pages for schema drift HFC IDs. | Story 9-4 |
| Full migration guide publication and docs navigation. | Story 9-5 |
| Agent E2E proving negotiation across Claude Code, Codex, Cursor, and native chat. | Story 10-2 |
| Signed LLM benchmark artifacts including schema/corpus/scorer fingerprints. | Story 10-6 |
| Team-specific skill overlay fingerprint baseline and override policy. | Post-v1 adopter tooling follow-up |
| Streaming renderer negotiation and renderer-owned scoped cache. | Later numbered MCP transport/performance story |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-8-mcp-agent-integration.md#Story-8.6`] - story statement, AC foundation, and FR59-FR61 scope.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR59`] - schema hash fingerprints for projection and MCP tool manifest.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR60`] - migration delta/breaking-change diagnostic with remediation.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR61`] - rendering abstraction contract for multi-surface rendering.
- [Source: `_bmad-output/planning-artifacts/architecture.md#MCP-Interaction-Model`] - MCP tools/resources and structured Markdown resource model.
- [Source: `_bmad-output/implementation-artifacts/8-1-mcp-server-and-typed-tool-exposure.md`] - MCP descriptors, SDK-neutral contracts, and schema source-of-truth.
- [Source: `_bmad-output/implementation-artifacts/8-2-hallucination-rejection-and-tenant-scoped-tools.md`] - stale descriptor handling and hidden/unknown semantics.
- [Source: `_bmad-output/implementation-artifacts/8-3-two-call-lifecycle-and-agent-command-semantics.md`] - lifecycle schema/version negotiation deferral.
- [Source: `_bmad-output/implementation-artifacts/8-4-projection-rendering-for-agents.md`] - Markdown renderer contract and deferred schema/versioning work.
- [Source: `_bmad-output/implementation-artifacts/8-5-skill-corpus-and-build-time-agent-support.md`] - skill corpus manifest/resource versioning handoff.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review and elicitation are complementary.
- [Source: `src/Hexalith.FrontComposer.Contracts/Mcp/McpManifest.cs`] - existing SDK-neutral MCP manifest contract.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Transforms/McpManifestTransform.cs`] - existing manifest schema version and descriptor transform.
- [Source: `src/Hexalith.FrontComposer.Contracts/Rendering/IRenderer.cs`] - provisional generic renderer contract.
- [Source: `src/Hexalith.FrontComposer.Contracts/Rendering/CustomizationContractVersion.cs`] - existing major/minor/build comparison pattern.
- [Source: `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSchemaCompatibilityFixtureTests.cs`] - existing projection schema compatibility fixture tests.
- [Source: NuGet `ModelContextProtocol.AspNetCore` 1.2.0](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore/) - current package version and .NET 8/9/10 compatibility as of 2026-05-02.
- [Source: MCP 2025-11-25 server overview](https://modelcontextprotocol.io/specification/2025-11-25/server/index) - tools/resources/prompts server primitives.
- [Source: MCP 2025-11-25 tasks utility](https://modelcontextprotocol.io/specification/2025-11-25/basic/utilities/tasks) - tasks are experimental; do not base schema negotiation on them.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-02: Story created via `/bmad-create-story 8-6-schema-versioning-and-multi-surface-abstraction` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### File List

(to be filled in by dev agent)
