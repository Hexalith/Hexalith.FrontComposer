# Hexalith.FrontComposer — API & Contract Surfaces

> **Generated:** 2026-06-02 · deep scan. FrontComposer is a library/tooling product, so its "APIs" are: (1) the **source-generator contract** (attributes in → generated files out, governed by HFC diagnostics), (2) the **MCP tool/resource surface**, and (3) the **CLI commands**. Runtime service interfaces are in [component-inventory.md](./component-inventory.md); record shapes in [data-models.md](./data-models.md).

---

## 1. Source-generator contract (`FrontComposerGenerator`)

The generator (netstandard2.0) reads attributes from [Contracts](src/Hexalith.FrontComposer.Contracts/) and emits code into `obj/{Config}/{TFM}/generated/HexalithFrontComposer/` (the **public** `GeneratedOutputPathContract.Template`).

### 1.1 Attributes consumed (the input vocabulary)

| Attribute | Applies to | Effect |
|---|---|---|
| `[Projection]` | class (read model) | Generate projection view + Fluxor state + registration. Type **must be `partial`**. |
| `[Command]` | class/record | Generate command form + lifecycle + renderer + registration. Must have a public parameterless ctor and a `MessageId` property. |
| `[BoundedContext(name, DisplayLabel?)]` | type | Groups types under a navigation context. |
| `[ProjectionRole(role, WhenState?)]` | projection | Rendering strategy: Default, DetailRecord, Timeline, ActionQueue, StatusOverview, Dashboard. |
| `[ProjectionBadge(BadgeSlot)]` | enum member | Map status-enum member → semantic badge slot. |
| `[ColumnPriority(n)]` | projection property | Explicit DataGrid column ordering. |
| `[ProjectionFieldGroup(name)]` | projection property | Group detail fields under a named section. |
| `[ProjectionEmptyStateCta(commandType)]` | projection | Empty-state call-to-action. |
| `[ProjectionTemplate]` | Blazor component | Register a Level-2 view template for a projection. |
| `[Destructive(ConfirmationTitle?, ConfirmationBody?)]` | command | Require a confirmation dialog. |
| `[RequiresPolicy(policyName)]` | command | Attach an authorization policy. |
| `[DerivedFrom]` | command property | Mark as infrastructure-sourced → excluded from the form. |
| `[Icon(name)]` | command/projection | FluentUI icon name. |
| `[RelativeTime(days?)]`, `[Currency]` | projection property | Level-1 display format hints. |

Standard attributes also honored: `[Display]`, `[Description]`, `[DefaultValue]`, `[Flags]`.

### 1.2 Generated output

**Per `[Projection]` → 5 files:** `{T}.g.razor.cs` (view; Loading/Empty/Data dispatched by `ProjectionRole`), `{T}Feature.g.cs`, `{T}Actions.g.cs`, `{T}Reducers.g.cs`, `{T}Registration.g.cs`.

**Per `[Command]` → 6–7 files** (`.Command` hint segment): `CommandForm.g.razor.cs`, `CommandActions.g.cs`, `CommandLifecycleFeature.g.cs`, `CommandRegistration.g.cs`, `CommandRenderer.g.razor.cs`, `CommandLastUsedSubscriber.g.cs`, `CommandLifecycleBridge.g.cs`, plus `CommandPage.g.razor.cs` when density = `FullPage`.

**Compilation-level:** `FrontComposerMcpManifest.g.cs` (MCP tool/resource manifest with schema fingerprints) and `FrontComposerProjectionTemplateManifest.g.cs`.

**Command density rule (spec-locked):** non-derivable property count ≤1 → `Inline`; 2–4 → `CompactInline`; ≥5 → `FullPage`. Derivable (form-excluded) fields: `MessageId`, `CommandId`, `CorrelationId`, `TenantId`, `UserId`, `Timestamp`, `CreatedAt`, `ModifiedAt`, or any `[DerivedFrom]`. Supported field types auto-render; unsupported → `FcFieldPlaceholder`.

### 1.3 MSBuild / analyzer-config options (drift detection)

| Property | Meaning |
|---|---|
| `HfcDriftDetectionEnabled` (alias `FrontComposerDriftDetectionEnabled`) | Opt in to drift comparison. |
| `HfcDriftBaselinePath` | Path to a `frontcomposer.generated-ui-baseline*.json` / `frontcomposer.drift-baseline*.json` `AdditionalText`. |
| `HfcDriftMaxDiagnostics` | 1–500, default 50. |
| `HfcDriftMaxBaselineBytes` | 1–10 MiB, default 256 KiB. |
| `HfcDriftSeverity` | Warning \| Error \| Info (default Warning). |
| `PublishTrimmed` / `PublishAot` | Auto-enable the HFC1070 trim/AOT advisory. |

### 1.4 Diagnostic catalog (HFC1001–HFC1070)

> Build-time diagnostics use the `HFC1xxx` band; runtime diagnostics use `HFC2xxx`. Symbolic IDs live in `FcDiagnosticIds` (Contracts) and descriptors in `Diagnostics/DiagnosticDescriptors.cs` (SourceTools). `(reserved)` = declared but not yet active.

| ID | Severity | Meaning |
|---|---|---|
| HFC1001 | Warn | No `[Command]`/`[Projection]` types found |
| HFC1002 | Warn | Unsupported projection field type |
| HFC1003 | Warn | `[Projection]` not `partial` |
| HFC1004 | Warn | Attribute on unsupported type kind (struct/abstract/generic) |
| HFC1005 | Warn | Invalid attribute argument |
| HFC1006 | Warn | `[Command]` missing `MessageId` |
| HFC1007 | Warn/Error | Command >30 (warn) / >100 (error) non-derivable properties |
| HFC1008 | Warn | `[Flags]` enum in single-value UI context |
| HFC1009 | Error | `[Command]` has no public parameterless ctor |
| HFC1010 | Info | Full rebuild required for customization metadata change (reserved) |
| HFC1011 | Error | `[Command]` exceeds 200-property hard limit |
| HFC1012 | Error | `[DefaultValue]` type mismatch |
| HFC1014 | Error | Nested `[Command]` type |
| HFC1015 | Warn | RenderMode incompatible with command density |
| HFC1016 | Error | Non-derivable command property is read-only/init-only |
| HFC1017 | Error | Generic `[Command]` type |
| HFC1020 | Info | Destructive-verb name without `[Destructive]` |
| HFC1021 | Error | `[Destructive]` command has zero non-derivable properties |
| HFC1022 | Warn | `ProjectionRole.WhenState` references unknown enum member |
| HFC1023 | Info | Dashboard role falls back to Default rendering |
| HFC1024 | Warn | Unknown `ProjectionRole` value |
| HFC1025 | Info | Partial `[ProjectionBadge]` enum coverage |
| HFC1026 | Warn | Color-only badge (reserved) |
| HFC1027 | Info | Collection column unsupported for filtering |
| HFC1028 | Info | `[ColumnPriority]` collision |
| HFC1029 | Info | Projection >15 columns; `FcColumnPrioritizer` activates |
| HFC1030 | Info | `[ProjectionFieldGroup]` collides with reserved "Additional details" |
| HFC1031 | Info | `[ProjectionFieldGroup]` ignored for non-Detail role |
| HFC1032 | Warn | Invalid Level-1 format annotation (`[RelativeTime]`/`[Currency]`) |
| HFC1033 | Error | `[ProjectionTemplate]` references invalid projection type |
| HFC1034 | Warn | `[ProjectionTemplate]` missing typed `Context` parameter |
| HFC1035 | Warn | `[ProjectionTemplate]` contract major-version mismatch |
| HFC1036 | Warn | `[ProjectionTemplate]` contract minor-version drift |
| HFC1037 | Error | Duplicate `[ProjectionTemplate]` for same projection+role |
| HFC1038 | Error | Invalid Level-3 slot selector |
| HFC1039 | Warn | Invalid Level-3 slot component |
| HFC1040 | Warn | Duplicate Level-3 slot override |
| HFC1041 | Warn | Level-3 slot contract version mismatch |
| HFC1042–HFC1049 | — | Level-4 view override + dev-mode (reserved) |
| HFC1050 | Warn | Custom override interactive element missing accessible name |
| HFC1051 | Warn | Custom override keyboard reachability issue |
| HFC1052 | Warn | Custom override suppresses focus visibility |
| HFC1053 | Warn | Custom override missing `aria-live` parity |
| HFC1054 | Warn | Custom override motion without reduced-motion fallback |
| HFC1055 | Warn | Custom override color without forced-colors fallback |
| HFC1056 | Error | `[RequiresPolicy]` value invalid |
| HFC1057 | Error | Duplicate `[RequiresPolicy]` |
| HFC1058 | Warn | Drift enabled but baseline file missing |
| HFC1059 | Error | `HfcDriftBaselinePath` matches no `AdditionalText` |
| HFC1060 | Error | Baseline empty/malformed JSON |
| HFC1061 | Error | Baseline schema version unsupported |
| HFC1062 | Error | Baseline algorithm version unsupported |
| HFC1063 | Error | Baseline exceeds size/count bounds |
| HFC1064 | Error | Baseline duplicate identities / invariant violation |
| HFC1065 | Warn* | **Structural drift** (added/removed/renamed decl or property, type-category/nullability change) |
| HFC1066 | Warn* | **Metadata drift** (DisplayName, ProjectionRole, Icon, RequiresPolicy, badge signature…) |
| HFC1067 | Warn | Drift MSBuild option invalid value |
| HFC1068 | Warn | Drift diagnostics truncated by cap |
| HFC1069 | Error | Drift diagnostic suppressed — untrusted values failed redaction |
| HFC1070 | Warn | Trim/AOT build uses reflection projection catalog (no `IActionQueueProjectionCatalog` override) |

\* severity configurable via `HfcDriftSeverity`.

**CLI migration code-fix:** `HFCM9001` — `AddFrontComposerDebugOverlay` → `AddFrontComposerDevMode` (the single allowlisted fix for the 9.1→9.2 edge).

---

## 2. MCP tool & resource surface (`Hexalith.FrontComposer.Mcp`)

HTTP streamable MCP server (default endpoint `/mcp`). Tools are built **dynamically** from the generated `McpManifest` at each `tools/list`.

### 2.1 Tools

| Tool | Source | Inputs | Output |
|---|---|---|---|
| *(one per command)* `descriptor.ProtocolName` | each `McpCommandDescriptor` | per-descriptor JSON schema (`McpJsonSchemaBuilder`); server-controlled fields `TenantId`/`UserId`/`MessageId`/`CorrelationId` are **blocked** | success → `McpCommandAcknowledgement` (`messageId`, `correlationId`, state `Acknowledged`, `McpLifecycleSubscription`); failure → typed rejection (`errorCode`, `reasonCategory`, `suggestedAction`, `docsCode`) |
| `frontcomposer.lifecycle.subscribe` *(name = `FrontComposerMcpOptions.LifecycleToolName`)* | fixed | one arg: `correlationId` or `messageId` (ULID, 26 Crockford chars, ASCII, ≤64) | `McpLifecycleSnapshot` (`state`, `terminal`, `outcome`, bounded `history`, `retryAfterMs`, `maxLongPollMs`, `historyTruncated`) |

### 2.2 Resources

| Resource | URI | Content |
|---|---|---|
| Projection | `frontcomposer://<context>/<projection>` | tenant-scoped query results rendered as Markdown (`McpMarkdownProjectionRenderer`) |
| Skill corpus | `frontcomposer://skills/<id>` | embedded markdown docs (`docs/skills/frontcomposer/**/*.md`); bypass visibility gate |
| Skill manifest | `frontcomposer://skills/manifest` | aggregate of all skill resources (id/uri/version/owningStory/publicApiReferences/samplePaths) |

### 2.3 Request flow & security

- **Bootstrap:** host registers `IFrontComposerMcpTenantToolGate` + `IFrontComposerMcpResourceVisibilityGate` **before** `AddFrontComposerMcp(...)` (startup throws otherwise — **fail-closed**); then `MapFrontComposerMcp()`.
- **`tools/list`** → `ToolAdmissionService.BuildVisibleCatalogAsync` (auth + tenant + policy gates). Auth/tenant failure returns an **empty list**, not an error.
- **`tools/call`** → lifecycle tool path, or `CommandInvoker`: admission → schema negotiation → arg validation → instantiate → inject derivable values → DataAnnotations → `ICommandService.DispatchAsync<T>` → lifecycle tracking.
- **Auth:** `X-FrontComposer-Mcp-Key` header (constant-time compare) or `ClaimsPrincipal`. Schema fingerprint header `x-frontcomposer-schema-fingerprint` = `algorithmId:64-hex-lowercase`.
- **Hidden-equivalent errors:** `AuthFailed` / `TenantMissing` / `UnknownResource` / `unknown_tool` return the same opaque shape.

### 2.4 Skill-corpus authoring contract

Each `docs/skills/frontcomposer/**/*.md` must start with YAML front matter: required `id` (kebab, starts with letter, ≤128), `title`, `version`, `audience: agent`, `docfx` (bool), `mcpResource: true`, `resourceUri` (`frontcomposer://skills/...`), `order` (int ≥0), `sourceDoc`, `narrative` (bool), `references` (bool); optional `migrationOwner`, `owningStory`, `publicApiReferences[]`, `samplePaths[]`. The body must contain exactly one `<!-- frontcomposer:section agent-reference --> … <!-- /frontcomposer:section -->` block — **only that block is served to agents**. Read cap 32 KB (oversized → `SkillResourceTooLarge`, no truncation). Each resource carries a `SchemaFingerprint` incorporating a `bodyDigest` so any content change changes the fingerprint.

---

## 3. CLI commands (`frontcomposer` tool)

Packaged via `<PackAsTool>true</PackAsTool>`; invoked as `frontcomposer <command> [options]` after `dotnet tool install`. No third-party CLI framework. All user-visible output passes through `OutputSanitizer`.

### 3.1 `frontcomposer inspect`

Reads generated files from `obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer/` plus `*.diagnostics.json` sidecars.

Options: `--project <path>`, `--solution <path>`, `--configuration <name>` (default Debug), `--framework <tfm>`, `--build` (runs `dotnet build` with `EmitCompilerGeneratedFiles=true` first), `--type <metadata-name>` (fuzzy match + Levenshtein suggestions), `--severity hidden|info|warning|error`, `--fail-on-warning`, `--fail-on-error`, `--format text|json` (default text), `--absolute-paths`.

JSON: `schemaVersion = frontcomposer.cli.inspect.v1`; summary fields `generatedFiles`, `forms`, `grids`, `registrations`, `mcpManifestEntries`, `warnings`, `errors`.

### 3.2 `frontcomposer migrate`

Plans/applies Roslyn code-fix migrations for allowlisted diagnostic IDs across declared version edges (`MigrationCatalog.Edges`; current edge `9.1.0 → 9.2.0` → `docs/migrations/9.1-to-9.2.md`).

Options: `--from <version>` + `--to <version>` (required; must match a catalog edge), `--dry-run` (default), `--apply` (atomic temp-file + rename), `--project`/`--solution`, `--format text|json`, `--fail-on-findings`.

JSON: `schemaVersion = frontcomposer.cli.migrate.v1`; `applied=true` only after a clean `--apply`. Entry `kind`: `safe-fix`, `unchanged`, `skipped`, `failed`, `manual-only`, `conflict`. Diff budget 8 000 chars/entry, 64 000 aggregate. **Path safety:** writes refused into `bin`/`obj`/`.git`/`packages`/`.nuget`/`nupkgs`/any `/generated/` segment and into submodule roots; out-of-root paths → `[redacted-path]`. `.slnx`/`.fsproj` fail closed.

### 3.3 Exit codes

`0` Success · `1` ActionableFindings · `2` InvalidArguments · `3` GeneratedOutputUnavailable · `4` ApplyWriteFailure.
