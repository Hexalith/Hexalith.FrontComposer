# Hexalith.FrontComposer — Data Model (Contracts & Schema)

> **Generated:** 2026-06-02 · deep scan. FrontComposer has **no database** — its "data models" are the in-memory/wire **contract records** that flow between the generator, the runtime shell, the MCP server, and the CLI, plus the **schema fingerprint / baseline / delta** types that govern compatibility. All defined in `Hexalith.FrontComposer.Contracts` and `Hexalith.FrontComposer.Schema`.

## 1. The registration model (generator → runtime)

The generator emits `RegisterDomain(...)` calls that populate `IFrontComposerRegistry` with **`DomainManifest`** records:

```
DomainManifest
├── Name : string
├── BoundedContext : string
├── Projections : IReadOnlyList<…>      # generated projection descriptors
├── Commands : IReadOnlyList<…>         # generated command descriptors
└── CommandPolicies : IReadOnlyDictionary<…>   # init accessor coerces null → empty
```

- `IFrontComposerRegistry.GetManifests()` is the runtime catalog the navigation, home directory, and command palette read from.
- `IFrontComposerFullPageRouteRegistry` and `IFrontComposerCommandWriteAccessRegistry` are companion registries for route-reachability and write-access policy.
- **Invariant:** `CommandPolicies` must go through the null-coercing `init` accessor — do not bypass it via raw positional construction.

## 2. Communication & lifecycle records

| Record / type | Shape & role |
|---|---|
| `CommandResult` | Outcome of `ICommandService.DispatchAsync`; carries accepted `MessageId`, optional EventStore `CorrelationId`, and optional retry hint. |
| `ProjectionQuery` | Canonical projection criteria: projection type, paging, column/status filters, search, and ordering. |
| `QueryRequest` / `QueryResult` | `QueryRequest.Create` composes `ProjectionQuery` with tenant, EventStore routing, ETags, and cache metadata. HFC0001/CS0618 retains the v1.12 flattened source/deconstruction surface and exact flat JSON throughout 2.x, with removal targeted for `3.0.0`; no nested `criteria` member is emitted. |
| `ProblemDetailsPayload` | RFC 7807 error payload with optional bounded `CommandRejectionDetails`. |
| `CommandRejectionDetails` | Typed rejection metadata (`errorCode`, `reasonCategory`, `suggestedAction`, `docsCode`) shown by command lifecycle UI. |
| `CommandWarningKind` / `CommandWarningException` | Warning-class command outcomes (`Forbidden`, `NotFound`, `RateLimited`, `Pending`, `RetryableDispatchFailed`) rendered outside terminal lifecycle state. |
| `CommandLifecycleState` (enum) | `Idle → Submitting → Acknowledged → Syncing → Confirmed / Rejected`. |
| `CommandLifecycleTransition` | A single state-machine edge (for trackers/UI). |
| `PendingCommandRegistration` / `PendingCommandEntry` | Circuit-local metadata for accepted commands; stores framework identity and bounded status data, never command payloads, tenant/user claims, or form values. |
| `PendingCommandStatus` | `Pending`, `Confirmed`, `Rejected`, `IdempotentConfirmed`, `NeedsReview`. |
| `McpLifecycleStateNames` | Canonical MCP wire names for the lifecycle states. |
| Exceptions | `CommandRejectedException`, `CommandWarningException`, `CommandValidationException`, `AuthRedirectRequiredException`. |

**Identity:** correlation/message identifiers are **ULIDs** (`IUlidFactory`, 26-char Crockford base32) — never GUIDs.

## 3. Rendering model

The render pipeline is described by data, not hard-coded UI:

| Type | Role |
|---|---|
| `FcTypoToken` (readonly record struct, Contracts.UI) | `(Size, Weight, Tag, Font?)` typography tuple under the retained `Hexalith.FrontComposer.Contracts.Rendering` namespace. |
| `Typography` (static, Contracts.UI) | 9 role constants (e.g. `AppTitle`, `ViewTitle`) → FluentUI v5 `TextSize`/`TextWeight`/`TextTag`. Version-pinned by kernel `ContractsMetadata.TypographyMappingVersion = "3.1.0"`. |
| `ProjectionContext` | Cascading Blazor parameter carrying row-level field values to command renderers. |
| `FrontComposerRenderContract` | A render surface's capabilities, bounds, content-type, and fingerprint. |
| `RenderSurfaceKind`, `RenderCapability`, `RenderBounds`, `DensityLevel`, `DensitySurface` | Rendering-model enums/records. |
| `FieldDescriptor` (Contracts) / `FieldSlotContext<TProjection,TField>` (Contracts.UI) | UI-neutral metadata remains in the kernel; render-fragment context is UI-owned. |
| Projection slot/template/view descriptors and selectors (Contracts); render-fragment contexts/delegates (Contracts.UI) | Level 2–4 customization keeps the dependency direction inward while preserving public namespaces. |
| `FcRenderMode`, `CommandRenderMode` | Render-mode enums. |
| `DerivedValueResult` (+ `IDerivedValueProvider`) | Computed-field support. |

## 4. MCP descriptor records

Emitted into `FrontComposerMcpManifest.g.cs` and consumed by the MCP server:

| Record | Role |
|---|---|
| `McpManifest` | The generated descriptor set; carries a `SchemaFingerprint`. |
| `McpCommandDescriptor` | One command → one MCP tool (protocol name, parameters, policy). |
| `McpResourceDescriptor` | One projection → one MCP resource. |
| `McpParameterDescriptor` | A single tool parameter. |
| `McpProjectionRenderStrategy` | Projection-rendering hint for MCP markdown output. |
| `GeneratedManifestAttribute` | Marks the generated manifest class for runtime discovery. |

## 5. Schema fingerprinting, baselines & deltas (the compatibility model)

This is the backbone of FrontComposer's "drift detection" — how producer (generator) and consumers (runtime/MCP/CLI) stay aligned.

### 5.1 Structural schema document
| Type | Role |
|---|---|
| `SchemaContractDocument` | Structural description of a contract (fields/collections). |
| `SchemaFieldContract`, `SchemaCollectionContract` | Field/collection entries within a document. |
| `SchemaContractFamily` (enum) | `CommandTool`, `ProjectionResource`, `LifecycleResult`, `MarkdownRendererContract`, `SkillCorpusManifest`, `SkillCorpusResource`, `AggregateMcpManifest`. |
| `SchemaContractFamilyNames` (static, in Schema) | enum → canonical kebab name (`"command-tool"`, `"projection-resource"`, …). |

### 5.2 Fingerprint
| Type | Role |
|---|---|
| `SchemaFingerprint` | A `(algorithmId, value)` SHA-256 identity used by generated descriptors, runtime resources, and baselines. |
| `SchemaFingerprintAlgorithm` | Algorithm-id constants: `frontcomposer.schema.sha256.canonical-json.v1` and `frontcomposer.schema.sha256.v1.sourcetools-blob` are both supported in v1. |
| `CanonicalSchemaMaterial` | Deterministic canonical-JSON + SHA-256 helper. **Pins** `JavaScriptEncoder.Create(UnicodeRanges.All)` + a STJ source-gen context (`SchemaFingerprintJsonContext`) for AOT; uses `AbsentValueSentinel = "<absent>"` and `StringComparer.Ordinal`. |
| `SchemaMaterialValidationException` / `…Result` | Validation surface for canonicalization. |

> **Critical invariant:** changing the encoder, the sentinel, or comparer in `CanonicalSchemaMaterial` silently invalidates **every** stored fingerprint and baseline. Don't.

### 5.3 Baseline & provenance
| Type | Role |
|---|---|
| `SchemaBaselineSnapshot` | A checked-in baseline of contract fingerprints to compare against. |
| `SchemaBaselineProvenance` | Who/what produced the baseline. **`PackageOwner` and `FixtureId` must match `^[a-zA-Z0-9][a-zA-Z0-9._-]{0,127}$`** — a security boundary against path traversal; do not relax. |

### 5.4 Delta & decision
| Type | Role |
|---|---|
| `SchemaCompatibilityDecision` (enum) | `Exact` / `AdditiveCompatible` / `CompatibleWarning` / `Breaking` / `Unknown` / `UnsupportedAlgorithm`. |
| `SchemaDeltaKind` (enum) | 14 delta categories (added/removed/renamed declaration or field, type-category change, nullability change, …). |
| `SchemaDelta` | One detected difference. |
| `SchemaMigrationDeltaResult` | The ordered, **bounded** delta set + decision. |
| `SchemaMigrationDeltaAnalyzer` (static, in Schema) | `Compare(baseline, current, maxDeltaCount)` → `SchemaMigrationDeltaResult`. Stateless. |

> **Invariants:** the analyzer must keep the `Truncated` and `MissingMigrationGuide` markers strictly within `maxDeltaCount` (the "markerSlot" reservation). Baseline **identity** is `family + "|" + type + "|" + boundedContext`; comparison uses `IdentityWithoutContext`. Changing identity formation breaks existing baselines.

## 6. Configuration & service-contract records

| Type | Role |
|---|---|
| `FcShellOptions` (Shell.Options) | Runtime shell configuration (validated via DataAnnotations / `OptionsBuilder.ValidateDataAnnotations` plus cross-property validators). Includes lifecycle thresholds, command-status polling cadence and duration, retained pending-entry cap, per-tick cap, and EventStore pre-accept command dispatch retry attempts/delay. |
| `FrontComposerAuthenticationOptions` | OIDC/auth options (Shell). |
| `FrontComposerAuthorizationOptions` | Host-owned policy catalog for `[RequiresPolicy]` commands, including strict startup validation. |
| `FrontComposerMcpOptions` | MCP endpoint, API-key map, arg/render/lifecycle bounds, claim types. |
| `LifecycleOptions` | Lifecycle tracker timing/retention. |
| `GeneratedOutputPathContract` | Public template `obj/{Config}/{TFM}/generated/HexalithFrontComposer/{Type}.g.razor.cs`; use `BuildProjectRelativePath(...)`, never hardcode. |
| `ShortcutBinding` (Contracts.UI) / `ShortcutRegistration` (Contracts) | Keyboard-event mapping is UI-owned; the UI-neutral registration record remains in the kernel. |
| `BadgeCountChangedArgs` | Badge-count change event payload. |
| `ComponentTreeNode`, `ConventionDescriptor`, `CustomizationDiagnostic*` | Dev-mode component-tree + customization diagnostics. |

## 7. Persistence

There is no relational/document database in FrontComposer itself. Persistence seams are:

- **`IStorageService`** — kernel key-value abstraction; runtime impl is Shell `LocalStorageService` (browser `localStorage` via JS interop); adopter test fake is `Hexalith.FrontComposer.Testing.InMemoryStorageService`.
- **EventStore** — the actual event-sourced backend lives in the `references/Hexalith.EventStore` submodule (external). The Shell talks to it via `EventStoreCommandClient` / `EventStoreQueryClient` (HTTP) and `ProjectionSubscriptionService` (SignalR). See [architecture.md](./architecture.md) §8.

## 8. Entity-relationship sketch (conceptual)

```
[Projection] type ──generates──▶ ProjectionDescriptor ─┐
[Command] type    ──generates──▶ CommandDescriptor ─────┼──▶ DomainManifest ──▶ IFrontComposerRegistry
                                                        │                         │
                                  McpManifest ◀─────────┘                         ▼
                                      │                                  Shell UI (nav/grid/forms)
                                      ▼
                            MCP tools + resources
                                      │
              SchemaFingerprint (per contract) ──baseline──▶ SchemaMigrationDeltaAnalyzer ──▶ SchemaCompatibilityDecision
```
