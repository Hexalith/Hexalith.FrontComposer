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
| `QueryRequest` / `QueryResult` | Projection query request/response DTOs. |
| `ProblemDetailsPayload` | RFC 7807 error payload with optional bounded `CommandRejectionDetails`. |
| `CommandRejectionDetails` | Typed rejection metadata (`errorCode`, `reasonCategory`, `suggestedAction`, `docsCode`) shown by command lifecycle UI. |
| `CommandLifecycleState` (enum) | `Idle → Submitting → Acknowledged → Syncing → Confirmed / Rejected`. |
| `CommandLifecycleTransition` | A single state-machine edge (for trackers/UI). |
| `McpLifecycleStateNames` | Canonical MCP wire names for the lifecycle states. |
| Exceptions | `CommandRejectedException`, `CommandWarningException`, `CommandValidationException`, `AuthRedirectRequiredException`. |

**Identity:** correlation/message identifiers are **ULIDs** (`IUlidFactory`, 26-char Crockford base32) — never GUIDs.

## 3. Rendering model

The render pipeline is described by data, not hard-coded UI:

| Type | Role |
|---|---|
| `FcTypoToken` (readonly record struct) | `(Size, Weight, Tag, Font?)` typography tuple. |
| `Typography` (static) | 9 role constants (e.g. `AppTitle`, `ViewTitle`) → FluentUI v5 `TextSize`/`TextWeight`/`TextTag`. **net10.0 only** (`#if NET10_0_OR_GREATER`). Version-pinned by `ContractsMetadata.TypographyMappingVersion = "3.1.0"`. |
| `ProjectionContext` | Cascading Blazor parameter carrying row-level field values to command renderers. |
| `FrontComposerRenderContract` | A render surface's capabilities, bounds, content-type, and fingerprint. |
| `RenderSurfaceKind`, `RenderCapability`, `RenderBounds`, `DensityLevel`, `DensitySurface` | Rendering-model enums/records. |
| `FieldDescriptor`, `FieldSlotContext<TProjection,TField>` | Field-level metadata for slot overrides. |
| `ProjectionSlotDescriptor`/`Selector`, `ProjectionTemplateDescriptor`/`Context<T>`, `ProjectionViewOverrideDescriptor`/`Context<T>` | Level 2–4 customization registration records. |
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
| `SchemaFingerprint` | A `(algorithmId, value)` SHA-256-over-canonical-JSON identity. |
| `SchemaFingerprintAlgorithm` | Algorithm-id constants. |
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
| `FcShellOptions` | Runtime shell configuration (validated via DataAnnotations / `OptionsBuilder.ValidateDataAnnotations` plus cross-property validators). Includes lifecycle thresholds, command-status polling cadence, pending-command polling duration, per-tick cap, and retained pending-entry cap. |
| `FrontComposerAuthenticationOptions` | OIDC/auth options (Shell). |
| `FrontComposerMcpOptions` | MCP endpoint, API-key map, arg/render/lifecycle bounds, claim types. |
| `LifecycleOptions` | Lifecycle tracker timing/retention. |
| `GeneratedOutputPathContract` | Public template `obj/{Config}/{TFM}/generated/HexalithFrontComposer/{Type}.g.razor.cs`; use `BuildProjectRelativePath(...)`, never hardcode. |
| `ShortcutBinding` / `ShortcutRegistration` | Keyboard-shortcut registry entries. |
| `BadgeCountChangedArgs` | Badge-count change event payload. |
| `ComponentTreeNode`, `ConventionDescriptor`, `CustomizationDiagnostic*` | Dev-mode component-tree + customization diagnostics. |

## 7. Persistence

There is no relational/document database in FrontComposer itself. Persistence seams are:

- **`IStorageService`** — key-value abstraction; runtime impl is `LocalStorageService` (browser `localStorage` via JS interop); `InMemoryStorageService` for tests/defaults.
- **EventStore** — the actual event-sourced backend lives in the `Hexalith.EventStore` submodule (external). The Shell talks to it via `EventStoreCommandClient` / `EventStoreQueryClient` (HTTP) and `ProjectionSubscriptionService` (SignalR). See [architecture.md](./architecture.md) §8.

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
