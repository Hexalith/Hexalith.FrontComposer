# Story 8.6: Schema Versioning & Multi-Surface Abstraction

Status: done

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
| AC19 | Fingerprint metadata is emitted or received | The algorithm identifier is missing, unknown, or different from the server-supported algorithm | Negotiation returns a distinct sanitized unsupported-algorithm category and does not compare hash strings produced by incompatible canonicalization rules. |
| AC20 | Multiple mismatch conditions are true for the same request | Visibility, tenant, authorization, descriptor freshness, and schema compatibility are evaluated | Hidden/unknown and stale-descriptor semantics take precedence over schema-mismatch reporting, and no agent-visible response confirms hidden resource existence. |
| AC21 | No trusted baseline exists for a supplied client or prior public schema fingerprint | Negotiation or migration diagnostics run | The result shape is exact and fail-closed: unknown-baseline category, stable message key, docs/diagnostic code, no command/query/render side effects, no cache or telemetry mutation containing runtime data, and maintainer guidance for registering a baseline. |
| AC22 | Canonical schema material is serialized for hashing | The canonical payload is produced | The JSON policy is executable and deterministic: UTF-8, ordinal property ordering, invariant culture, stable enum/string casing, explicit null/empty collection handling, normalized newlines/protocol identifiers, no timestamps/paths/comments, and no indentation-sensitive meaning. |
| AC23 | Migration delta output is large or deeply nested | Diagnostics are generated | Output is bounded by configured schema size, nesting depth, and maximum delta count; truncation is deterministic, ordered, and reported with a stable category. |
| AC24 | Negotiation results or migration diagnostics are surfaced to adopters, agents, logs, or tests | Messages are produced | Outputs use stable diagnostic IDs, message keys, and structured parameters suitable for localization; Story 8-6 does not choose final end-user UI copy, admin dashboard behavior, or tenant-specific override policy. |
| AC25 | Canonical payloads, baseline snapshots, or client fingerprint envelopes are parsed | Duplicate keys, case-variant duplicate keys, malformed roots, or unknown contract-family discriminators are present | Validation fails closed with a sanitized diagnostic category before comparison; no duplicate-key last-writer-wins behavior is allowed in canonical material. |
| AC26 | A baseline snapshot is resolved for negotiation or migration diagnostics | The resolver receives a client-supplied path, path traversal segment, absolute local path, package-external file, or untrusted generated output | Resolution is rejected before comparison; trusted baselines come only from checked-in/package-owned structural snapshots or test fixtures. |
| AC27 | Aggregate manifest fingerprints and nested command/resource/renderer/corpus fingerprints are emitted together | The aggregate and nested fingerprint metadata disagree, are missing required algorithm ids, or were produced from different canonical schema versions | The result fails closed as an internal schema-integrity mismatch and does not expose partial schema details to agents. |
| AC28 | Negotiation, baseline comparison, or migration diagnostics emit telemetry or logs | The event is recorded | Events use bounded category/message-key fields and coarse counts only; they never include hidden resource names, exact hidden counts, raw client envelopes, local paths, runtime values, or exception text. |
| AC29 | Canonical schema material contains collections | The collection is serialized for hashing or migration comparison | Each collection declares whether order is structural or non-structural; non-structural collections are sorted by stable public identifiers, while structural order changes are reported as explicit deltas. |
| AC30 | A trusted baseline snapshot is loaded after canonicalization code changes | The baseline canonicalizer metadata differs from the current supported canonicalizer metadata | The baseline is rejected as unsupported/unknown rather than silently compared with a different canonicalization rule set. |
| AC31 | Additive-compatible drift is detected for a command, resource, lifecycle, renderer, or corpus contract | The current request is admitted | The server validates the request against the current server schema and defaults before any side effect; additive compatibility never weakens required-field, bounds, authorization, or sanitization checks. |
| AC32 | Hidden/unknown, stale descriptor, unsupported algorithm, unknown baseline, malformed envelope, and schema mismatch can all apply to one request | Negotiation classifies the response | The earliest precedence category wins deterministically, with identical agent-visible category/message-key shape across repeated attempts and no lower-priority schema details leaked through diagnostics, logs, or telemetry. |

---

## Tasks / Subtasks

- [x] T1. Define canonical schema fingerprint input models (AC1-AC4, AC10-AC12, AC15, AC19, AC22)
  - [x] Add SDK-neutral structural models for command tool schema, projection resource schema, lifecycle result schema, Markdown renderer contract, skill corpus manifest/resource schema, and aggregate MCP manifest schema.
  - [x] Keep models pure data: stable names, bounded context, FQN, schema version, protocol name/URI, parameters/fields, required/nullable flags, type category, validation constraints, enum values, role/capability, output content type, bounds, and compatibility policy.
  - [x] Exclude runtime state by type and by tests: no tenant/user/claim/token/payload/query row/cache/ETag/local path/timestamp/service object.
  - [x] Define one canonical JSON serialization policy: ordinal property order, invariant culture, normalized newlines, normalized protocol identifiers, no indentation-sensitive meaning, and versioned root discriminator.
  - [x] Add a `SchemaFingerprintAlgorithm` identifier so future hashing changes are detectable without pretending hashes are comparable.
  - [x] Reject duplicate JSON keys, case-variant duplicate keys, malformed contract-family roots, and unknown root discriminators before producing or comparing canonical material.
  - [x] Define per-collection canonicalization metadata: stable sort key for non-structural collections, explicit order-significant marker for structural collections, and deterministic duplicate-id rejection before hashing.
  - [x] Owner package: shared SDK-neutral models live in `Hexalith.FrontComposer.Contracts`; SourceTools-only canonicalization helpers may live in `Hexalith.FrontComposer.SourceTools` only when they are not public runtime contracts.

- [x] T2. Implement deterministic fingerprint generation in SourceTools (AC1-AC4, AC11, AC12, AC22, AC23)
  - [x] Extend `McpManifestTransform` or a companion transform to compute fingerprints from the generated descriptor models, not from runtime reflection or loaded SDK DTOs.
  - [x] Emit per-command, per-resource, lifecycle-result, renderer-contract, skill-corpus-manifest, and aggregate manifest fingerprints where the owning artifact exists.
  - [x] Use a stable cryptographic hash such as SHA-256 over the canonical payload; expose the fingerprint as a lower-case hex string plus algorithm id.
  - [x] Add collision and ordering tests for protocol names, projection URIs, fields, parameters, enum values, validation constraints, resource ordering, and renderer capability ordering.
  - [x] Ensure any protocol-name/URI disambiguation from Stories 8-1/8-2 is reflected before hashing.
  - [x] Add a two-clean-generation test proving identical canonical JSON and hash from the same domain source despite generated-source ordering, line endings, culture, timezone, path separator, and build machine differences.
  - [x] Recompute aggregate manifest fingerprints from emitted nested fingerprints and fail generation/tests when aggregate and nested metadata disagree.
  - [x] Emit canonicalizer metadata/test-vector identifiers with baseline snapshots so future canonicalization changes cannot compare old snapshots as if they used the same rules.

- [x] T3. Extend SDK-neutral descriptor contracts without breaking package boundaries (AC1, AC5, AC10-AC15)
  - [x] Add optional fingerprint metadata to `McpManifest`, `McpCommandDescriptor`, `McpResourceDescriptor`, and equivalent lifecycle/renderer/corpus descriptors only where those records are already stable public contracts.
  - [x] Keep `Hexalith.FrontComposer.Contracts` dependency-free. Do not reference MCP SDK, Shell components, Fluent UI types, source-generator internals, EventStore clients, or docs infrastructure from Contracts.
  - [x] Add backwards-compatible constructors or companion records if existing consumers need source compatibility during the transition.
  - [x] Version descriptor schema constants deliberately; do not overload `McpManifestTransform.SchemaVersion = "frontcomposer.mcp.v1"` to mean every nested renderer/corpus schema version.
  - [x] Add API compatibility tests for public records and SourceTools compile tests for generated manifest output.
  - [x] Add package-boundary tests proving canonical fingerprinting and renderer abstraction contracts do not reference `ModelContextProtocol.*`, Shell, Fluent UI, EventStore, tenant/user runtime services, or source-generator implementation internals from `Hexalith.FrontComposer.Contracts`.

- [x] T4. Implement negotiation and stale-fingerprint handling in the MCP adapter (AC5, AC8, AC9, AC15, AC16, AC19-AC21, AC24)
  - [x] Define internal negotiation results: `Exact`, `CompatibleAdditive`, `Incompatible`, `UnknownClientVersion`, `UnknownServerBaseline`, `HiddenOrUnknown`, `StaleDescriptor`, `UnsupportedAlgorithm`, and `Unavailable`.
  - [x] Accept client fingerprint hints only as hints. Never trust them as proof of current visibility, authorization, tenant scope, or descriptor freshness.
  - [x] Reuse Story 8-2 admission order: visibility, tenant, policy, descriptor/catalog epoch, and request bounds are validated before side effects; hidden/unknown equivalence overrides schema mismatch reporting.
  - [x] Resolve trusted baselines through server/package-owned identifiers only; reject client-supplied file paths, path traversal, absolute paths, and package-external baseline locations.
  - [x] Return sanitized agent-visible categories such as `schema-compatible-warning`, `schema-mismatch`, or `projection temporarily unavailable`; no hidden names, tenant identifiers, policy names, exact hidden counts, raw baseline paths, or exception text.
  - [x] For `CompatibleAdditive`, re-run current server-side validation and defaulting before dispatch/query/render; never treat an old compatible fingerprint as a waiver for current required fields, bounds, authorization, or sanitization.
  - [x] Add zero-side-effect tests proving incompatible/unknown/stale negotiation does not invoke command dispatch, query execution, lifecycle mutation, cache writes, or renderer buffers.
  - [x] Add a table-driven precedence matrix for hidden, unknown, unauthorized, cross-tenant, stale descriptor, unsupported algorithm, unknown baseline, compatible drift, and incompatible drift.

- [x] T5. Build migration delta diagnostics and baselines (AC6-AC8, AC16, AC17, AC21, AC23, AC24)
  - [x] Define a checked-in baseline snapshot format for shipped public schema shapes. Store only canonical structural data and fingerprint metadata, not generated code blobs or runtime payloads.
  - [x] Include baseline provenance fields: contract family, schema version, fingerprint algorithm, package/source owner, fixture id, and migration-guide requirement flag; exclude filesystem paths and build machine metadata.
  - [x] Include canonicalizer version/test-vector metadata and reject snapshots whose canonicalizer contract is unsupported by the running generator/runtime.
  - [x] Compare old/new snapshots and classify deltas into additive-compatible, compatible-warning, breaking, unknown, or unsupported-algorithm.
  - [x] Emit HFC diagnostics with the existing Expected/Got/Fix/DocsLink style: changed field/path, old value category, new value category, impact, and remediation.
  - [x] Map migration-guide requirements for breaking public descriptor, skill corpus example, renderer contract, lifecycle result, and projection resource deltas.
  - [x] Keep CLI UX to Story 9-2 unless a minimal test-only runner is needed; Story 8-6 owns the library contract and build/test diagnostics.
  - [x] Treat the delta categories in this story as a closed v1 set. Rename detection, full compatibility reports, admin dashboards, and automatic migration are deferred unless already covered by a listed category.

- [x] T6. Define the multi-surface rendering abstraction contract (AC11, AC13-AC15)
  - [x] Create or refine a narrow abstraction around `IRenderer<TModel,TOutput>` with a surface-neutral `FrontComposerRenderContract`, `RenderSurfaceKind`, `RenderCapability`, `RenderSchemaFingerprint`, `RenderOutputContentType`, and bounded metadata.
  - [x] Use SourceTools IR and generated descriptors as the model source of truth. Do not make Shell components, Fluent UI tokens, Razor component instances, or MCP SDK DTOs canonical renderer input.
  - [x] Represent current supported outputs honestly: web/Blazor render path and MCP Markdown projection output. Future chat/IDE/custom templates are capability placeholders, not implemented outputs.
  - [x] Define compatibility rules for renderer contract changes: additive capability, removed capability, changed output content type, changed bounds, changed role support, changed field metadata, and changed sanitization guarantees.
  - [x] Add tests proving renderer abstraction metadata can describe Story 8-4 Markdown contracts without hashing rendered projection rows.
  - [x] Owner package: metadata contracts belong in `Hexalith.FrontComposer.Contracts`; Shell and `.Mcp` adapters map to/from those contracts without making their renderer/runtime types canonical.

- [x] T7. Integrate skill corpus and docs migration hooks (AC12, AC17)
  - [x] Consume Story 8-5 corpus manifest metadata when present and compute corpus/resource fingerprints from source/manifest structure.
  - [x] Require migration-guide metadata when public skill examples or corpus resource schemas break, aligned with FR69.
  - [x] Do not merge team-specific external skill overlays into framework fingerprints unless a future story defines an adopter-owned overlay baseline.
  - [x] Emit diagnostics against corpus resource ids and source sections, not local absolute paths or raw prompt/provider output.

- [x] T8. Tests and verification (AC1-AC32)
  - [x] SourceTools unit tests for canonical serialization, stable SHA-256 output, ordering independence, changed-field sensitivity, algorithm-id changes, and no runtime field inclusion.
  - [x] Manifest snapshot tests for command descriptor, projection resource descriptor, aggregate MCP manifest, lifecycle schema, renderer contract, and skill corpus fingerprints.
  - [x] Negotiation tests for exact, additive, breaking, unsupported algorithm, unknown baseline, stale descriptor, hidden/unknown, unauthorized, cross-tenant, and malformed fingerprint hints.
  - [x] Migration delta tests for added optional field, added required field, removed field, type/category change, enum change, validation constraint change, URI/name change, lifecycle result change, renderer capability change, and corpus resource change.
  - [x] Security tests proving tenant IDs, user IDs, claims, tokens, payload values, query rows, local paths, ETags, timestamps, exception text, and provider internals never appear in fingerprints, deltas, logs, telemetry, or agent-visible responses.
  - [x] Parser/security tests for duplicate keys, case-variant duplicates, malformed discriminators, client-supplied baseline paths, path traversal, aggregate/nested fingerprint mismatch, and telemetry redaction/cardinality bounds.
  - [x] Collection canonicalization tests proving order-insensitive collections sort by stable ids, order-sensitive collections report explicit order deltas, and duplicate stable ids fail before hashing.
  - [x] Canonicalizer compatibility tests proving stale baseline canonicalizer metadata, missing test-vector ids, and mismatched algorithm/canonicalizer pairs fail closed.
  - [x] Additive-compatibility tests proving old compatible fingerprints still run current server validation/defaulting and cannot bypass required fields, bounds, authorization, or sanitization.
  - [x] Precedence determinism tests proving identical requests with multiple mismatch causes produce the same earliest category/message key and never leak lower-priority schema details into diagnostics, logs, or telemetry.
  - [x] API/package-boundary tests proving Contracts stays SDK-free, SourceTools computes canonical fingerprints, and `.Mcp` performs only adapter mapping.
  - [x] Minimal fixture suite: `baseline-known-v1`, `baseline-known-v2-compatible`, `baseline-known-v2-structural-delta`, `baseline-unknown`, `schema-same-different-order`, `schema-same-different-runtime-data`, `schema-hidden-precedence`, `schema-unknown-precedence`, and `surface-metadata-only-renderer`.
  - [x] Each fixture states expected fingerprint material, algorithm id, negotiation result, delta category, bounded/truncation behavior where relevant, and renderer abstraction metadata where relevant.
  - [x] Existing compatibility regression: `ProjectionSchemaCompatibilityFixtureTests` still deserialize supported projection fixtures and map incompatible payloads to the schema mismatch path.
  - [x] Build regression: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`.
  - [x] Targeted tests: `tests/Hexalith.FrontComposer.SourceTools.Tests`, `tests/Hexalith.FrontComposer.Contracts.Tests`, `tests/Hexalith.FrontComposer.Mcp.Tests`, and Shell schema compatibility tests.

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
- Client fingerprints and envelopes are lookup hints only; they never supply baseline material, filesystem locations, tenant visibility, or compatibility decisions.
- Aggregate manifest fingerprints are integrity checks over nested structural fingerprints; aggregate/nested disagreement is a schema-integrity failure, not a partial success.

### Package Ownership

| Concern | Owning package / area | Boundary rule |
| --- | --- | --- |
| Public schema fingerprint metadata records | `Hexalith.FrontComposer.Contracts` | SDK-neutral only; no MCP SDK, Shell, Fluent UI, EventStore, SourceTools internals, tenant/user services, or runtime adapters. |
| Canonical schema materialization and hashing | `Hexalith.FrontComposer.SourceTools` | Build from generator/source-model descriptors and deterministic canonical payloads; do not hash generated source text or runtime reflection output. |
| MCP negotiation and DTO mapping | `Hexalith.FrontComposer.Mcp` | Compare and map generated metadata after admission checks; keep MCP SDK DTOs and protocol-specific response shaping inside this package. |
| Renderer contract metadata | `Hexalith.FrontComposer.Contracts` plus adapter-specific mapping | Contracts describe surface/capability/content-type/bounds/fingerprint metadata; Shell and `.Mcp` renderers do not become canonical schema sources. |
| Migration delta baselines and diagnostics | SourceTools/test fixtures, with HFC diagnostic integration | Store small structural snapshots only; CLI UX and public docs publication remain Stories 9-2/9-4/9-5. |

### Canonical Fingerprint Material

Include:

- Contract family and schema version.
- Fingerprint algorithm id.
- Bounded context, public protocol name/URI, FQN, stable resource/tool id.
- Parameter/field name, type category, JSON type, required/nullable state, enum values, validation constraints, unsupported marker, label/description metadata category.
- Projection role, renderer capability, output content type, bounds category, lifecycle result field names/categories, skill corpus resource id/order/source marker metadata.

Exclude:

- Tenant ID, user ID, claims, roles, tokens, API keys, command payload values, query rows, cache keys, ETags, local absolute paths, timestamps, service instance ids, `ClaimsPrincipal`, `RenderContext`, raw exceptions, provider internals, and generated source text that includes machine-specific paths.

### Canonical JSON Policy

- Encode canonical payloads as UTF-8 bytes with ordinal property ordering and invariant-culture scalar formatting.
- Use stable string and enum casing, explicit null handling, explicit empty collection handling, normalized newlines, and normalized protocol identifiers before hashing.
- For collections, encode the collection semantics before entries are serialized. Non-structural collections sort by stable public identifiers; structural collections preserve order and make order changes visible as migration deltas.
- Do not include comments, whitespace-sensitive indentation, absolute paths, timestamps, generator banner text, build machine data, assembly load order, runtime service state, or SDK DTO serialization artifacts.
- Include the contract family, contract schema version, and `SchemaFingerprintAlgorithm` in fingerprint metadata. Unsupported or missing algorithms are negotiation failures, not hash mismatches.
- Include canonicalizer metadata/test-vector identifiers in package-owned baselines so a baseline produced by older canonical JSON rules cannot be compared as if it were current.
- Reject duplicate keys, case-variant duplicate keys, malformed root discriminators, and unknown contract families before hashing or comparing canonical material.
- Hash the canonical payload, not generated C# source, rendered Markdown, runtime DTO JSON, telemetry envelopes, or exception payloads.

### Baseline Trust and Provenance

- Trusted baselines are checked-in/package-owned structural snapshots or explicit test fixtures. They are not read from agent/client-provided paths, local absolute paths, package-external folders, generated-output scratch directories, or tenant-owned storage.
- Baseline lookup keys are stable public identifiers: contract family, contract schema version, package/source owner, fingerprint algorithm, and fixture/baseline id. Local filenames can be implementation details but must not appear in diagnostics, logs, telemetry, or agent-visible responses.
- Snapshot writes used by build/test tooling must be atomic: write to a package-owned temporary file, validate duplicate keys/discriminators/provenance, then replace. A partial snapshot is treated as unavailable/unknown baseline, never as compatible.
- Migration diagnostics may include old/new structural categories and public contract identifiers, but they must not include raw baseline JSON, full client envelopes, hidden resource names, local paths, runtime values, or exception text.

### Negotiation Contract

| Internal result | Agent-visible behavior | Side-effect rule |
| --- | --- | --- |
| `Exact` | Proceed normally. | Normal command/query/render path after admission. |
| `CompatibleAdditive` | Proceed with sanitized compatibility warning metadata where the protocol supports it. | Re-run current server validation/defaulting first; no extra side effects beyond normal admitted operation. |
| `Incompatible` | Return sanitized schema mismatch with docs code/remediation category. | No command dispatch, query, lifecycle mutation, cache write, or renderer allocation. |
| `UnknownClientVersion` | Return sanitized unknown-version category and remediation guidance. | No side effects unless product later explicitly allows read-only compatibility fallback. |
| `UnknownServerBaseline` | Return maintainer-facing diagnostic; agent sees unavailable/schema category. | No side effects. |
| `HiddenOrUnknown` | Preserve Story 8-2 hidden/unknown response. | No side effects. |
| `StaleDescriptor` | Preserve stale/unavailable category. | No side effects. |
| `UnsupportedAlgorithm` | Return sanitized unsupported schema fingerprint category. | No side effects. |
| `SchemaIntegrityMismatch` | Return sanitized unavailable/schema category and emit maintainer diagnostic. | No side effects. |

### Negotiation Precedence

Evaluate in this order before any command/query/render side effects:

1. Request shape and bounds are valid enough to safely classify the request.
2. Tenant, authorization, visibility, and hidden/unknown equivalence.
3. Descriptor/catalog epoch freshness and stale descriptor handling.
4. Fingerprint algorithm support.
5. Trusted baseline availability and provenance.
6. Aggregate/nested fingerprint integrity.
7. Exact/compatible/incompatible schema comparison, current-server validation/defaulting for compatible requests, and migration delta classification.

When earlier checks fail, later schema details must not be reported to the agent. This is especially important for hidden, unauthorized, cross-tenant, and stale resources, where schema mismatch metadata would become an existence oracle.

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

The categories above are the closed v1 set for Story 8-6. Semantic rename detection, automatic migration, broad compatibility reports, dashboard surfacing, tenant-specific schema policy, and user-facing copy strategy are deferred product decisions unless later stories explicitly own them.

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
| D15. Baseline resolution is package-owned and path-safe. | Prevents clients, agents, or generated-output folders from steering comparison toward untrusted snapshots. |
| D16. Duplicate-key canonical material is invalid. | Prevents parser-dependent last-writer-wins behavior from producing inconsistent fingerprints or deltas. |
| D17. Aggregate and nested fingerprints must be self-consistent. | Detects partial generation, stale nested metadata, or mixed schema-version artifacts before runtime negotiation. |
| D18. Collection semantics are part of the canonical schema contract. | Prevents meaningless order churn from changing hashes while still detecting order when order affects agent or renderer behavior. |
| D19. Baselines carry canonicalizer compatibility metadata. | Prevents a future canonicalization change from silently comparing old snapshots with new rules. |
| D20. Additive compatibility never bypasses current validation. | Keeps old compatible clients from weakening current required-field, bounds, authorization, or sanitization checks. |
| D21. `McpSchemaNegotiationInput.HasCompatibleAdditiveDrift` is deprecated; the negotiator must derive additive vs breaking via `SchemaMigrationDeltaAnalyzer` from snapshot inputs. | Closes the "optional security parameters are an anti-pattern" gap raised by review DN-4. The bool input remains for one release with `[Obsolete]` then is removed by Story 8-6a. |
| D22. Runtime-loaded skill corpus material is hashed inside `Hexalith.FrontComposer.Mcp` even though Package Ownership otherwise places canonicalization in SourceTools. | Resolves DN-6: the corpus is loaded from disk at host startup, is not visible to source generators, and there is no portable way to surface its canonical material at build time. SourceTools retains canonicalization for shipped framework corpus material; only adopter overlays / runtime-loaded items canonicalize at runtime. |
| D23. Two algorithm identifiers coexist for v1 schema fingerprints: `Sha256CanonicalJsonV1` (Contracts canonical-JSON) and `Sha256SourceToolsBlobV1` (SourceTools build-time text-blob). | Resolves DN-2 / B-1: Roslyn analyzer hosting constraints around `System.Text.Json` source-gen prevent the SourceTools project from sharing the runtime canonicalizer in v1. Runtime negotiator trusts emitter-supplied fingerprints (never recomputes) so the two algorithms are interoperable in the v1 negotiation contract. Unification is owned by Story 8-6a. |

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
- General data migration framework, semantic field-rename inference, automatic baseline registration, admin dashboard UX, tenant-specific fail-open/override policy, final localized UI copy, or multi-surface feature parity guarantees.

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

GPT-5 Codex

### Debug Log References

- 2026-05-04: `dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj -p:TreatWarningsAsErrors=true` => 159 passed.
- 2026-05-04: `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -p:TreatWarningsAsErrors=true` => 606 passed.
- 2026-05-04: `dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -p:TreatWarningsAsErrors=true` => 155 passed.
- 2026-05-04: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` => 0 warnings/errors.
- 2026-05-04: `dotnet test Hexalith.FrontComposer.sln --no-build` => Contracts 159/0/0, MCP 155/0/0, Shell 1542/0/0, SourceTools 606/0/0, Bench 2/0/0.

### Completion Notes List

- 2026-05-02: Story created via `/bmad-create-story 8-6-schema-versioning-and-multi-surface-abstraction` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-02: Party-mode review completed via `/bmad-party-mode 8-6-schema-versioning-and-multi-surface-abstraction; review;`. Applied canonical JSON, package ownership, negotiation precedence, fail-closed unknown-baseline, algorithm-id, boundedness, fixture-matrix, localization-key, and scope-guardrail hardening. Ready for advanced elicitation on a later run.
- 2026-05-03: Advanced elicitation completed via `/bmad-advanced-elicitation 8-6-schema-versioning-and-multi-surface-abstraction`. Applied collection canonicalization, canonicalizer/baseline compatibility, additive-compatibility validation, and deterministic multi-cause precedence hardening.
- 2026-05-04: Implemented schema fingerprint contracts, deterministic SourceTools fingerprint emission, MCP negotiation precedence, migration delta/baseline contracts, renderer surface metadata, and skill corpus resource fingerprints. Fixed generator analyzer-context loading by keeping generator-side fingerprint computation self-contained while emitting public `SchemaFingerprint` metadata into generated manifest code.

### Party-Mode Review

- **Date/time:** 2026-05-02T14:52:53+02:00
- **Selected story key:** `8-6-schema-versioning-and-multi-surface-abstraction`
- **Command/skill invocation used:** `/bmad-party-mode 8-6-schema-versioning-and-multi-surface-abstraction; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- **Findings summary:** The review found that Story 8-6 has the right SourceTools-owned, SDK-neutral direction, but needed sharper pre-dev contracts before implementation. Main risks were ambiguous package ownership, underspecified canonical JSON rules, unsupported-algorithm ambiguity, unknown-baseline fail-closed behavior, hidden/unknown-vs-schema-mismatch precedence, migration delta taxonomy expansion, renderer abstraction scope creep, opaque adopter diagnostics, and insufficient fixture-level test strategy.
- **Changes applied:** Added AC19-AC24 for algorithm identity, negotiation precedence, exact unknown-baseline fail-closed behavior, executable canonical JSON policy, bounded delta output, and stable localizable diagnostic/message-key outputs; expanded T1-T8 with package ownership, two-clean-generation determinism, dependency-boundary tests, table-driven negotiation precedence tests, closed delta-category behavior, metadata-only renderer boundaries, and a minimal structural fixture suite; added Package Ownership, Canonical JSON Policy, Negotiation Precedence, and closed v1 delta/scope guardrails in Dev Notes.
- **Findings deferred:** General data migration framework, semantic rename detection, automatic migration or baseline registration, admin dashboard UX, tenant-specific fail-open/override policy, final localized UI copy, user-facing accessibility automation beyond future visible mismatch UI, broad consumer-driven contract testing, and multi-surface feature parity guarantees remain deferred to named future stories or product/architecture decisions.
- **Final recommendation:** ready-for-dev

### Advanced Elicitation

- **Date/time:** 2026-05-03T08:10:34+02:00
- **Selected story key:** `8-6-schema-versioning-and-multi-surface-abstraction`
- **Command/skill invocation used:** `/bmad-advanced-elicitation 8-6-schema-versioning-and-multi-surface-abstraction`
- **Batch 1 method names:** Stakeholder Round Table; Expert Panel Review; Debate Club Showdown; User Persona Focus Group; Time Traveler Council
- **Reshuffled Batch 2 method names:** Red Team vs Blue Team; Security Audit Personas; Failure Mode Analysis; First Principles Analysis; Occam's Razor Application
- **Findings summary:** Elicitation confirmed the story was correctly scoped, but still had four pre-dev ambiguity risks: collection ordering could create noisy or missing fingerprints, baseline snapshots could outlive canonicalizer rule changes, additive-compatible drift could be misread as permission to skip current server validation, and multi-cause mismatch requests needed deterministic evidence that lower-priority schema details never leak.
- **Changes applied:** Added AC29-AC32; expanded T1/T2/T4/T5/T8; refined Canonical JSON Policy, Negotiation Contract, and Negotiation Precedence; added Binding Decisions D18-D20; documented collection canonicalization, canonicalizer compatibility metadata, current-validation requirements for compatible drift, and deterministic multi-cause precedence tests.
- **Findings deferred:** Semantic rename detection, automatic baseline registration, tenant-specific schema policy overrides, final localized UI/admin copy, broad external-client E2E matrix, and new renderer/client surfaces remain deferred to existing named follow-up stories or product/architecture decisions.
- **Final recommendation:** ready-for-dev

### File List

- Directory.Packages.props
- src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj
- src/Hexalith.FrontComposer.Contracts/Mcp/McpCommandDescriptor.cs
- src/Hexalith.FrontComposer.Contracts/Mcp/McpManifest.cs
- src/Hexalith.FrontComposer.Contracts/Mcp/McpResourceDescriptor.cs
- src/Hexalith.FrontComposer.Contracts/Rendering/FrontComposerRenderContract.cs
- src/Hexalith.FrontComposer.Contracts/Schema/SchemaBaselineContracts.cs
- src/Hexalith.FrontComposer.Contracts/Schema/SchemaFingerprintContracts.cs
- src/Hexalith.FrontComposer.Mcp/FrontComposerMcpFailureCategory.cs
- src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs
- src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs
- src/Hexalith.FrontComposer.SourceTools/Diagnostics/SchemaMigrationDeltaAnalyzer.cs
- src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs
- src/Hexalith.FrontComposer.SourceTools/Transforms/SchemaFingerprintTransform.cs
- tests/Hexalith.FrontComposer.Contracts.Tests/Schema/SchemaFingerprintContractsTests.cs
- tests/Hexalith.FrontComposer.Mcp.Tests/BoundaryTests.cs
- tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationTests.cs
- tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillCorpusTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaAnalyzerTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/McpManifestEmitterTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/SchemaFingerprintTransformTests.cs

### Change Log

- 2026-05-04: Completed Story 8.6 and moved status to review after build and full regression validation.
- 2026-05-05: Code-review pass via `/bmad-code-review 8-6` against commit `a776288` (focused 19-file diff). Three review layers (Blind Hunter / Edge Case Hunter / Acceptance Auditor) returned ~65 raw findings; triaged to 6 decision-needed, 43 patches, 2 deferred, 5 dismissed. See Review Findings below.
- 2026-05-05: Resolved 6 decisions (DN-1 rescope to library/emission and file 8-6a for runtime gate; DN-2 distinct algorithm id under D23; DN-3 explicit denylist filter; DN-4 deprecate `HasCompatibleAdditiveDrift` per D21; DN-5 accept `RenderStrategy` positional-arg break; DN-6 D22 corpus-runtime-hash exception). Applied 25 patches (P-1, P-2, P-4, P-6, P-7, P-8, P-9, P-10, P-11, P-12, P-13, P-14, P-15, P-16, P-17, P-18, P-27, P-29, P-30, P-31, P-32, P-33, P-37, P-40, P-41, P-42, P-43). Deferred 17 patches to Story 8-6a (runtime-coupled and test-infrastructure work). Added binding decisions D21-D23. Filed Story 8-6a (`ready-for-dev`) for runtime negotiator wiring, canonicalizer unification, and the deferred patches. Status moved review → done.

### Review Findings

#### Decision-Needed (resolve first)

- [x] **[Review][Decision] DN-1 — Negotiator never wired into production code path** — RESOLVED: rescope 8-6 to library/emission only; runtime gate filed as Story 8-6a (`ready-for-dev`). AC5/AC9/AC20/AC24/AC32 marked partial in Story 8-6 (library proven via unit tests; runtime pipeline integration in 8-6a). Rationale: 500-1000 LOC of pipeline integration is net-new feature work, not a code-review patch. Pattern matches Story 8-4→8-4a precedent. (CRITICAL, sources: auditor+blind)
  - **Evidence:** `McpSchemaNegotiator.Negotiate` at `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs` has zero callsites outside its own test. `FrontComposerMcpProjectionReader`, `FrontComposerMcpCommandInvoker`, `FrontComposerMcpToolAdmissionService`, and `FrontComposerMcpDescriptorRegistry` do not consult it. `FrontComposerMcpProjectionFailureMapper.Map` does not branch on `SchemaMismatch` / `UnknownSchemaBaseline` / `UnsupportedSchemaAlgorithm` / `SchemaIntegrityMismatch` (added in `FrontComposerMcpFailureCategory.cs:24-27`); they fall through to generic `downstream_failed` (Retryable=true). `FrontComposerRenderContract` is defined but no `.Mcp` adapter maps to it.
  - **Impact:** AC5, AC9, AC20, AC24, AC32 cannot be satisfied at runtime. Story 8-2 hidden/unknown precedence over schema mismatch is preserved only because the schema gate never runs at all.
  - **Options:** (a) wire negotiator into projection/command/tool admission pipeline + add schema branches to failure mapper + add renderer adapter mapping (substantial scope expansion); (b) rescope 8-6 to library/emission only, mark runtime ACs partial, create Story 8-6a for runtime gate; (c) defer runtime gate to Story 9-1 (build-time drift detection) which can consume the library offline.

- [x] **[Review][Decision] DN-2 — Two parallel canonicalizers under the same algorithm id** — RESOLVED (partial): rename the SourceTools-emitted algorithm constant to `frontcomposer.schema.sha256.v1.sourcetools-blob` to make the divergence explicit; add corresponding `Sha256SourceToolsBlobV1` constant in `SchemaFingerprintAlgorithm`; teach the negotiator to accept either algorithm value as supported (since the runtime trusts emitter-supplied fingerprints and never recomputes). Full canonicalizer unification (single bytes via shared `CanonicalSchemaMaterial`) is deferred to Story 8-6a because Roslyn source-generator hosting constraints around `System.Text.Json` source-gen require dedicated analyzer-host validation. New binding decision D23 records the deliberate two-algorithm v1 contract. (HIGH, sources: blind)
  - **Evidence:** `SchemaFingerprintTransform.Payload` (`src/Hexalith.FrontComposer.SourceTools/Transforms/SchemaFingerprintTransform.cs`) builds a newline-delimited `key=value` text blob and stores it in `GeneratedSchemaPayload.Json`. `CanonicalSchemaMaterial.CreatePayload` in Contracts uses real `JsonSerializer`-canonical JSON. Both stamp `SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1`.
  - **Impact:** A client recomputing the fingerprint via Contracts will silently mismatch a generator-emitted fingerprint. AC2 (byte-for-byte identical fingerprints) is broken between the two surfaces.
  - **Options:** (a) replace `SchemaFingerprintTransform.Payload` to invoke `CanonicalSchemaMaterial.CreatePayload` (single canonicalizer); (b) bump SourceTools-side algorithm id to `frontcomposer.schema.sha256.v1.sourcetools` (two algorithms, two ids); (c) keep current and add an interop layer that round-trips through Contracts.

- [x] **[Review][Decision] DN-3 — Test asserts MessageId/TenantId absent from command fingerprint, but no upstream filter exists** — RESOLVED: added explicit denylist filter in `SchemaFingerprintTransform.CreateCommandPayload` for runtime-correlation field names (`MessageId`, `TenantId`, `CorrelationId`, `UserId`, `Principal`, `Claims`, `Token`). Filter is defense-in-depth — `McpManifestTransform` already excludes these by parameter selection, but the assertion's invariant is now structurally enforced. (HIGH, sources: blind)
  - **Evidence:** `CreateCommandPayload` serializes `command.Parameters.Select(FieldLine)` directly. Test `SchemaFingerprintTransformTests.cs:~1488` asserts `payload.Json.ShouldNotContain("TenantId"); .ShouldNotContain("MessageId");` but no code path filters those names.
  - **Impact:** AC4 (no runtime correlation/tenant fields in fingerprints) is asserted without enforcement; one upstream change to McpManifestTransform parameter projection silently breaks it.
  - **Options:** (a) add explicit denylist filter in `CreateCommandPayload` for known correlation fields (`MessageId`, `TenantId`, `CorrelationId`, etc.) and document; (b) move the filter to `McpManifestTransform` and make it explicit; (c) include them in fingerprint material and update the test (rejecting AC4 intent).

- [x] **[Review][Decision] DN-4 — `CompatibleAdditive` allows side effects based on caller-supplied bool** — RESOLVED: changed `McpSchemaNegotiationInput.HasCompatibleAdditiveDrift: bool` to `BaselineSnapshot: SchemaBaselineSnapshot? + ServerSnapshot: SchemaBaselineSnapshot?`, with the negotiator invoking `SchemaMigrationDeltaAnalyzer.Compare` internally to derive additive vs breaking. Defers full implementation to Story 8-6a (couples with negotiator wiring); for the immediate review patch, mark `HasCompatibleAdditiveDrift` as `[Obsolete("Use ProvideAdmissionSnapshots instead")]` and add a binding decision D21 documenting the contract change. Memory rule "optional security parameters are an anti-pattern" honored. (HIGH, sources: blind+auditor)
  - **Evidence:** `SchemaNegotiation.cs:110-118` returns `AllowsSideEffects: true` for `CompatibleAdditive` whenever `input.HasCompatibleAdditiveDrift` is true. The negotiator does not derive additive vs breaking; it trusts a caller flag. T4 spec: "For `CompatibleAdditive`, re-run current server-side validation and defaulting before dispatch/query/render." Memory rule: optional security parameters are an anti-pattern — `Gate? = null` short-circuits in auth/tenant/redaction must be replaced with required DI or decorators.
  - **Impact:** D20 "Additive compatibility never bypasses current validation" is a doc-only invariant; the contract permits a buggy or hostile caller to assert additive drift and bypass. AC31 cannot be enforced.
  - **Options:** (a) require `ISchemaMigrationDeltaAnalyzer` injection and let the negotiator compute additive/breaking from baseline+server material (most defensible, larger scope); (b) require both `BaselineSnapshot` and `ServerSnapshot` inputs and derive internally; (c) accept the contract and document that callers must always pass through the analyzer (weakens DN-1 wiring).

- [x] **[Review][Decision] DN-5 — `McpResourceDescriptor.RenderStrategy` positional-arg type changed string→enum** — RESOLVED: accept the break. Already shipped to `main` in commit `7a69cdf` (Story 8-4 work) and downstream consumers do not exist outside the repo today. Documented in Change Log as a deliberate v1 SDK-public API break. No further action. (HIGH, sources: edge)
  - **Evidence:** Pre-existing public record changed parameter type at the same positional slot in commit `7a69cdf` (Story 8-4 work, not 8-6). Adopters constructing `McpResourceDescriptor(..., "DetailRecord", ...)` no longer compile.
  - **Impact:** Public Contracts SDK break; AC15 (SDK-neutral) impacted at the consumer level.
  - **Options:** (a) accept the break (already shipped to main); (b) add string-overload constructor that converts; (c) revert to optional string and adapt at the emitter.

- [x] **[Review][Decision] DN-6 — SkillCorpus runtime fingerprinting in `.Mcp` violates Package Ownership / D1** — RESOLVED: documented exception in D1 via new binding decision D22 — runtime-loaded corpus material (loaded from disk at host startup, not generator-visible) is hashed in `.Mcp` because SourceTools cannot see runtime overlay files. SourceTools-side framework corpus fingerprinting (build-time only) remains in `SchemaFingerprintTransform.CreateSkillCorpusResourcePayload` and stays the canonical source for shipped corpus material. (MEDIUM, sources: auditor)
  - **Evidence:** `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs:287-318` invokes `CanonicalSchemaMaterial.CreatePayload` at runtime and hashes a markdown body digest. D1 says "Fingerprints are generated from SourceTools canonical descriptor material"; Package Ownership table excludes runtime hashing from `.Mcp`.
  - **Impact:** Story 8-6 D1 binding decision violated.
  - **Options:** (a) move to SourceTools (corpus is build-time material — major refactor; would require SkillCorpus loader to migrate to a generator-time scan); (b) document an explicit exception in D1 ("runtime-loaded corpus material may be hashed in `.Mcp` because it is not generator-visible") and mark as binding decision DN-6; (c) add `ICanonicalSchemaMaterializer` interface in Contracts to keep canonicalization centralised while permitting runtime callsites.

#### Patches (43)

- [ ] [Review][Patch] P-1 Escape `=`, `|`, `\n` in metadata key/value and field-line delimiters [`src/Hexalith.FrontComposer.SourceTools/Transforms/SchemaFingerprintTransform.cs:Payload,FieldLine,Normalize`] — silent hash collisions via injection.
- [ ] [Review][Patch] P-2 Pin `JavaScriptEncoder` explicitly in `SchemaFingerprintJsonContext` [`src/Hexalith.FrontComposer.Contracts/Schema/SchemaFingerprintContracts.cs:294`] — default encoder escape table varies across .NET 6/8/10.
- [ ] [Review][Patch] P-3 Aggregate manifest must bind position to fingerprint to detect pairwise swaps [`SchemaFingerprintTransform.cs:JoinFingerprints`] — `OrderBy` collapses A↔B rename swaps.
- [ ] [Review][Patch] P-4 Distinguish null from empty string in fingerprint material via sentinel [`SchemaFingerprintTransform.cs:CreateCommandPayload,CreateResourcePayload`] — `?? ""` makes "policy removed" indistinguishable from "policy is empty".
- [ ] [Review][Patch] P-5 Wire skill corpus fingerprints into `McpManifestEmitter` aggregate [`src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs:823-828`] — currently passes `[]` so manifest aggregate is blind to corpus drift.
- [ ] [Review][Patch] P-6 `AddMetadataDelta` unrecognized keys → emit explicit `Other`/`MetadataChanged` delta, not silent drop [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/SchemaMigrationDeltaAnalyzer.cs:AddMetadataDelta`] — authorization-policy change reported as compatible.
- [ ] [Review][Patch] P-7 `AddMetadataDelta` key match must be exact/prefix, not substring [`SchemaMigrationDeltaAnalyzer.cs:AddMetadataDelta`] — `displaycapabilityHint` mis-classified as renderer-capability change.
- [ ] [Review][Patch] P-8 `NormalizeScalar` must strip BOM/U+200B/U+200C/U+200D and normalize U+2028/U+2029 [`SchemaFingerprintContracts.cs:NormalizeScalar`,`SchemaFingerprintTransform.cs:Normalize`] — silent fingerprint divergence on BOM-laden YAML/markdown sources.
- [ ] [Review][Patch] P-9 Bound delta `Path` length and per-delta size [`SchemaMigrationDeltaAnalyzer.cs:Compare`] — telemetry/log shape can balloon with deeply-nested or hostile field names.
- [ ] [Review][Patch] P-10 Truncation must compute `Decision` from FULL delta set's worst category, not just first 25 [`SchemaMigrationDeltaAnalyzer.cs:Compare:93-101`] — Breaking deltas truncated past index 25 silently downgrade aggregate to `CompatibleWarning`. Safety regression.
- [ ] [Review][Patch] P-11 Truncation marker should fit within `maxDeltaCount`, not append beyond it [`SchemaMigrationDeltaAnalyzer.cs:95-97`] — final list size is `maxDeltaCount + 1`, breaks bounds caller-side.
- [ ] [Review][Patch] P-12 Validate `maxDeltaCount > 0` [`SchemaMigrationDeltaAnalyzer.cs:11`] — caller-supplied 0 or negative hides all deltas including Breaking.
- [ ] [Review][Patch] P-13 Distinguish `ServerFingerprint == null` from `UnsupportedAlgorithm` [`src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs:69-78`] — collapses missing-baseline into algorithm error, wrong remediation.
- [ ] [Review][Patch] P-14 Validate `ServerFingerprint.Value` is not null/whitespace [`SchemaNegotiation.cs:577`] — only client value is validated; both empty would spurious-match.
- [ ] [Review][Patch] P-15 Reuse `SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1` in SourceTools instead of duplicating literal [`SchemaFingerprintTransform.cs:AlgorithmId`] — two `const string` copies; bumping one without the other silently breaks negotiation.
- [ ] [Review][Patch] P-16 `SanitizeRenderStrategy` must throw or emit a generator diagnostic on unknown enum members [`McpManifestEmitter.cs:923-924`] — silent downgrade to `Default` hides new render strategy additions in fingerprint.
- [ ] [Review][Patch] P-17 Validate `SchemaBaselineProvenance.FixtureId` and `PackageOwner` against safe pattern [`src/Hexalith.FrontComposer.Contracts/Schema/SchemaBaselineContracts.cs:5-10`] — currently arbitrary strings; if used to look up baselines on disk, attacker-controlled values enable path traversal. Closes AC26 gap.
- [ ] [Review][Patch] P-18 Consume `RequiresMigrationGuide` in `Compare` — fail-closed when Breaking deltas detected without migration guide [`SchemaMigrationDeltaAnalyzer.cs`] — currently never read; AC17 unenforced.
- [ ] [Review][Patch] P-19 Add aggregate-vs-nested integrity check at consumption time + add a producer for `SchemaIntegrityMismatch` [`McpManifestEmitter.cs`,`SchemaNegotiation.cs`] — currently `HasSchemaIntegrityMismatch` is an unsourced input bool; AC27/D17 unenforced.
- [ ] [Review][Patch] P-20 Derive lifecycle fingerprint from `McpLifecycleResult` type structure, not literal field list [`SchemaFingerprintTransform.cs:CreateLifecycleResultPayload:79-95`] — adding a lifecycle field will not change fingerprint, breaking AC3/AC10.
- [ ] [Review][Patch] P-21 Renderer bounds in fingerprint must come from real renderer config, not hardcoded constants [`McpManifestEmitter.cs:42-47, 821-822`] — `64_000` / `4_096` literals diverge from actual `SkillResourceReadOptions.DefaultMaxCharacters`; AC11/AC3 broken.
- [ ] [Review][Patch] P-22 Extend `BoundaryTests.cs` to assert FluentUI / EventStore / tenant runtime services absent from Contracts [`tests/Hexalith.FrontComposer.Mcp.Tests/BoundaryTests.cs`] — T3 last subtask explicit.
- [ ] [Review][Patch] P-23 Add two-clean-generation determinism test (culture/timezone/EOL/path-separator/dictionary-order) [`tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/SchemaFingerprintTransformTests.cs`] — T2 mandate not satisfied.
- [ ] [Review][Patch] P-24 Add truncation deterministic-ordering test for >25 deltas [`tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaAnalyzerTests.cs`] — AC23 untested.
- [ ] [Review][Patch] P-25 Add minimal fixture suite from T8 (9 fixtures) [`tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures/`] — T8 mandate not satisfied; required: `baseline-known-v1`, `baseline-known-v2-compatible`, `baseline-known-v2-structural-delta`, `baseline-unknown`, `schema-same-different-order`, `schema-same-different-runtime-data`, `schema-hidden-precedence`, `schema-unknown-precedence`, `surface-metadata-only-renderer`.
- [ ] [Review][Patch] P-26 Verify `System.Text.Json 10.0.6` source-gen behavior on `netstandard2.0`; pin TFM or unify [`Directory.Packages.props:9`,`src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj`] — netstandard2.0 vs net10.0 STJ source-gen escape tables may differ → fingerprints diverge across generator host vs runtime.
- [ ] [Review][Patch] P-27 Algorithm/canonicalizer mismatch must not reuse `SchemaDeltaKind.Truncated` — add `CanonicalizerUnsupported` [`SchemaMigrationDeltaAnalyzer.cs:22-32`] — wrong remediation docs link.
- [ ] [Review][Patch] P-28 Reconcile case-sensitivity discipline (case-distinct duplicate keys vs OrdinalIgnoreCase parser rejection) [`SchemaFingerprintContracts.cs:235-251, 350`] — `Dictionary<string,string>(Ordinal)` permits both `foo` and `Foo` then validator rejects them on read.
- [ ] [Review][Patch] P-29 Use a more accurate `SchemaMaterialValidationCategory` for duplicate field names (e.g., `DuplicateFieldName`) [`SchemaFingerprintContracts.cs:350-355`] — `DuplicateStableId` is for collection ids, not field names.
- [ ] [Review][Patch] P-30 Distinct error category for "JSON depth exceeded" vs generic `MalformedJson` [`SchemaFingerprintContracts.cs:121-130`] — current `catch (JsonException)` collapses both.
- [ ] [Review][Patch] P-31 Strip BOM in `ValidateCanonicalJson` before reading [`SchemaFingerprintContracts.cs:121`] — UTF-8 BOM survives `Utf8JsonReader`.
- [ ] [Review][Patch] P-32 Wrap raw exception with sanitized `SchemaFingerprintException`; do not include raw `validation.Path` [`SchemaFingerprintContracts.cs:103`] — leaks field-name into exception message.
- [ ] [Review][Patch] P-33 Bound input payload size before allocating `Encoding.UTF8.GetBytes` [`SchemaFingerprintContracts.cs:ValidateCanonicalJson`] — untrusted JSON can OOM.
- [ ] [Review][Patch] P-34 Either populate or remove `SchemaDelta.Parameters` [`src/Hexalith.FrontComposer.Contracts/Schema/SchemaBaselineContracts.cs:180-185`] — dead public surface.
- [ ] [Review][Patch] P-35 Move `SchemaMigrationDeltaResult.DocsLink` to a constants file or configurable provider [`SchemaMigrationDeltaAnalyzer.cs:140`] — hardcoded URL; cannot point to per-version docs.
- [ ] [Review][Patch] P-36 Document/enforce `SchemaContractDocument.Metadata` is sorted via `Normalize` before any roundtrip [`SchemaFingerprintContracts.cs:68, 196-204`] — roundtrip via public `IReadOnlyDictionary` may not be sorted.
- [ ] [Review][Patch] P-37 Distinguish whitespace-only string from null in `NormalizeOptional` [`SchemaFingerprintContracts.cs:NormalizeOptional`] — `null` and `"   "` collide.
- [ ] [Review][Patch] P-38 Add `CancellationToken` to `CreatePayload` and `Compare` [`SchemaFingerprintContracts.cs:CreatePayload`,`SchemaMigrationDeltaAnalyzer.cs:Compare`] — long descriptors cannot be aborted by MCP client disconnect.
- [ ] [Review][Patch] P-39 Expose `SchemaFingerprintJsonContext` as `public` for AOT consumers [`SchemaFingerprintContracts.cs:294-296`] — `SchemaContractDocument` is public but its source-gen context is internal; AOT/trim-safe consumers cannot register it.
- [ ] [Review][Patch] P-40 Document or change `HasTrustedBaseline=false` behavior when client/server hashes match exactly [`SchemaNegotiation.cs:80-98`] — current returns `UnknownSchemaBaseline` even on byte-identical hashes; lock-out after baseline wipe.
- [ ] [Review][Patch] P-41 Add `UnknownClientVersion` to spec precedence ordering OR move algorithm-supported gate before client-fingerprint-null check [`SchemaNegotiation.cs:59-67`] — current order skips spec ordering.
- [ ] [Review][Patch] P-42 `SchemaIntegrityMismatch` should be evaluated before `HasTrustedBaseline` [`SchemaNegotiation.cs:80-98`] — partial-manifest integrity corruption is hidden by baseline-unknown error.
- [ ] [Review][Patch] P-43 `CreatePayload` should validate emitted JSON via `ValidateCanonicalJson` before returning [`SchemaFingerprintContracts.cs:295-311`] — closes AC25/D16 producer-side gap.

#### Deferred

- [x] [Review][Defer] DEF-1 — `CompilationHelper.ParseProjection`/`ParseCommand` referenced in tests but not in this diff [`tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/SchemaFingerprintTransformTests.cs`,`tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/McpManifestEmitterTests.cs`] — deferred, pre-existing test infrastructure (helper exists in repo from earlier stories; not in scope of this review).
- [x] [Review][Defer] DEF-2 — `MessageKey` (stable id) and `AgentCategory` (prose with spaces) mixed in `McpSchemaNegotiationResult` [`SchemaNegotiation.cs:54`] — deferred, broader naming consistency cleanup; mirrored across other failure-category records.

#### Dismissed (5)

- DM-1 SHA-256 algorithm choice — appropriate for non-adversarial fingerprinting.
- DM-2 `Normalize` swallowing leading/trailing whitespace — intentional and consistent.
- DM-3 `SHA256.Create()` per-call allocation — perf nit, not correctness.
- DM-4 `exact.Add(prop) || caseVariant.Add(prop)` short-circuit — both evaluated, no correctness issue.
- DM-5 `SchemaMaterialValidationResult.Valid` static singleton — fine because record is immutable.
