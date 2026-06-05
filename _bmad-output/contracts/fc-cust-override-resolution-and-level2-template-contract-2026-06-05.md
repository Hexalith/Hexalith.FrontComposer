# FC-CUST Override Resolution and Level-2 Template Contract

Date: 2026-06-05
Status: confirmed-stable
Scope: FC-CUST v1 override-resolution precedence record and Level-2 `[ProjectionTemplate]` contract.
Owner: FrontComposer Epic 6, Story 6.1.

## Story Number Reconciliation

The live source and tests contain historical comments such as `Story 6-2`, `Story 6-3`, `Story 6-6`, `Story 9-4`, `GB-P10`, and `D15/AC15`. Those labels came from an earlier planning decomposition. Under the current Epic 6 plan, Level-2 `[ProjectionTemplate]` overrides are Story 6.1. This artifact maps that existing implementation to Story 6.1 without relabeling stable source comments.

## Override-Resolution Precedence

Status: confirmed-stable.

The generator emits deterministic body precedence in this order:

1. Level 4 full view override via `ProjectionViewOverrideRegistry.Resolve(...)`.
2. Level 2 template via `ProjectionTemplateRegistry.Resolve(...)`.
3. Generated default body via `defaultBody(builder)`.

Evidence:

- Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:1308` resolves the Level 4 descriptor before any Level 2 lookup.
- Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:1337` resolves the Level 2 template only inside the Level 4 miss path.
- Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:1362` invokes the generated default body only after both override layers miss.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs:150` proves a registered Level 2 template renders inside the generated grid envelope and uses generated field rendering.

Level 3 field slots compose within whichever body renders. Level 2 context exposes `FieldRenderer`, `RowRenderer`, and `SectionRenderer`, so templates can rearrange layout while delegating field rendering to FrontComposer-owned generated delegates. Evidence: `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateContext.cs:47`, `:50`, `:51`, and `:52`; `RazorEmitter.cs:1350` through `:1353`.

## Level-2 Attribute and Registration Contract

Status: confirmed-stable.

Attribute shape:

- `[ProjectionTemplate](Type projectionType, int expectedContractVersion)` targets classes only.
- `Role` is optional and is read from named-argument presence at the call site, not from the enum property's default value.

Evidence:

- Source: `src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionTemplateAttribute.cs:21` restricts usage to classes.
- Source: `ProjectionTemplateAttribute.cs:32` defines the constructor.
- Source: `ProjectionTemplateAttribute.cs:47` through `:55` defines the optional `Role`.
- Source: `src/Hexalith.FrontComposer.SourceTools/Parsing/ProjectionTemplateMarkerParser.cs:78` through `:108` parses explicit role named arguments and suppresses unsafe numeric roles.

Template surface:

- The template must be a non-static, non-abstract, non-generic class.
- It must expose a public `[Parameter] ProjectionTemplateContext<TProjection> Context` property matching the projection type.

Evidence:

- Source: `ProjectionTemplateMarkerParser.cs:197` through `:221` validates the template type.
- Source: `ProjectionTemplateMarkerParser.cs:262` through `:323` validates the typed `Context` parameter.
- Test: `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/ProjectionTemplateMarkerTests.cs` pins HFC1034 for missing or non-parameter `Context`.

Manifest and registration:

- SourceTools discovers markers with `ForAttributeWithMetadataName(...)` and tracking name `ParseProjectionTemplate`.
- The generator emits `__FrontComposerProjectionTemplatesRegistration.g.cs` with a `Descriptors` list and `ContractVersion`.
- Adopters register descriptors via `AddHexalithProjectionTemplates<TMarker>()` or the descriptor-list overload.
- `AddHexalithFrontComposer` / Quickstart registers `IProjectionTemplateRegistry`, `IProjectionSlotRegistry`, and `IProjectionViewOverrideRegistry` as singleton descriptor registries.

Evidence:

- Source: `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs:38` through `:45` wires marker discovery.
- Source: `FrontComposerGenerator.cs:219` through `:252` emits the consolidated manifest and duplicate diagnostics.
- Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionTemplateManifestEmitter.cs:23` and `:24` define the generated type and hint name.
- Source: `ProjectionTemplateManifestEmitter.cs:116` through `:123` emits `ContractVersion` and `Descriptors`.
- Source: `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs:537` through `:550` registers by marker assembly.
- Source: `ServiceCollectionExtensions.cs:560` through `:573` registers by descriptor list.
- Source: `ServiceCollectionExtensions.cs:389` through `:402` registers Level 2, Level 3, and Level 4 registries as singletons.
- Source: `samples/Counter/Counter.Web/Program.cs:65` through `:71` exercises both registration overloads.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionTemplates/ProjectionTemplateAssemblySourceTests.cs:20` pins manifest-absent behavior.

Resolution semantics:

- Exact role wins over any-role.
- Null-role/any-role falls back when no exact-role descriptor is registered.
- Duplicate or ambiguous slots fail closed by returning `null`.
- Major contract mismatch is rejected. Minor drift is retained and logged.

Evidence:

- Source: `src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs:63` through `:92` rejects incompatible major versions.
- Source: `ProjectionTemplateRegistry.cs:98` through `:111` retains minor drift and logs HFC1036.
- Source: `ProjectionTemplateRegistry.cs:113` through `:130` marks duplicate runtime slots ambiguous.
- Source: `ProjectionTemplateRegistry.cs:134` through `:148` implements exact-role then any-role fallback.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionTemplates/ProjectionTemplateRegistryTests.cs:27`, `:39`, `:56`, and `:115` pin exact match, fallback, ambiguous fail-closed, and major mismatch.

Contract version:

- `ProjectionTemplateContractVersion.Current = Major * 1_000_000 + Minor * 1_000 + Build`.
- Current value is `1_000_000`.

Evidence:

- Source: `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateContractVersion.cs:27` through `:38`.
- Test: `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionTemplateContractsTests.cs:18` pins the packed value.

Cache-safety invariant:

- The generated manifest and runtime descriptors are type metadata only.
- They must not include timestamps, absolute file paths, tenant IDs, user IDs, localized strings, item payloads, render fragments, or per-render context objects.
- `ProjectionTemplateMarkerInfo` and `ProjectionTemplateMarkerResult` are pure, Roslyn-free, fully equatable incremental-generator IR. No `ISymbol` escapes the parse stage.

Evidence:

- Source: `src/Hexalith.FrontComposer.SourceTools/Parsing/ProjectionTemplateMarkerInfo.cs` contains only string, nullable string, and int marker fields plus equatable diagnostics.
- Source: `ProjectionTemplateManifestEmitter.cs:15` through `:20` documents type-only manifest metadata.
- Source: `ProjectionTemplateManifestEmitter.cs:125` through `:135` emits only projection type, role, template type, and contract version.
- Source: `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateDescriptor.cs:15` through `:19` defines descriptor cache safety.
- Source: `src/Hexalith.FrontComposer.Contracts/Rendering/IProjectionTemplateRegistry.cs:13` through `:21` forbids caching render contexts and per-user/per-tenant state.
- Test: `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/ProjectionTemplateMarkerTests.cs` pins deterministic manifests without timestamp/path/tenant/user tokens and now explicitly pins `ProjectionTemplateMarkerInfo` / `ProjectionTemplateMarkerResult` cache-key equality.

## Diagnostics Disposition

Status: confirmed-stable.

| Diagnostic | Phase | Severity | Disposition |
|---|---|---|---|
| HFC1024 | build | Warning | Unknown/unsafe-cast `ProjectionRole`; descriptor suppressed. |
| HFC1033 | build | Error | Projection type is missing or invalid; descriptor suppressed. |
| HFC1034 | build | Warning | Template type or typed `[Parameter] Context` is invalid; descriptor suppressed. |
| HFC1035 | build/runtime | Warning | Major contract mismatch; descriptor suppressed or rejected. |
| HFC1036 | build/runtime | Warning/Information | Minor drift; descriptor retained and selectable. Build-only drift emits nothing. |
| HFC1037 | build | Error | Duplicate `(projection, role)` tuple; all duplicates suppressed deterministically. |
| HFC2115 | runtime | Warning | Template render fault isolated; generated shell/sibling surfaces remain alive. |

Evidence:

- Source IDs: `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs:48` through `:165` defines HFC1024 and HFC1033-HFC1037.
- Parser call sites: `ProjectionTemplateMarkerParser.cs:95`, `:116`, `:130`, `:148`, `:293`, `:311`, `:337`, and `:357`.
- Emitter duplicate call site: `ProjectionTemplateManifestEmitter.cs:47` through `:91`.
- Runtime mismatch call sites: `ProjectionTemplateRegistry.cs:63` through `:111`.
- Runtime render-fault call site: `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs:56` through `:102`.
- Build diagnostic tests: `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/ProjectionTemplateMarkerTests.cs` pins HFC1024 and HFC1033-HFC1037.
- Runtime diagnostic tests: `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionTemplateHostTests.cs:53` and `:85` pin HFC2115 redaction and publish-once behavior.

## Non-Goals and Boundaries

Status: confirmed-stable.

- Level 3 field-slot behavior is out of scope except for documenting that slots compose inside whichever body renders. Follow-up: Story 6.2.
- Level 4 full-view behavior is out of scope except for documenting precedence. Follow-up: Story 6.3.
- Accessibility-safety diagnostics HFC1050-HFC1055 and `FcCustomizationDiagnosticPanel` authoring guidance are out of scope. Follow-up: Story 6.4.
- This contract does not change `CanonicalSchemaMaterial`, schema fingerprints, generated-output-path compatibility, pacts, or public API baselines.
- This contract does not introduce alternate MCP projection URI forms or change MCP tenant/resource visibility.
- This contract does not add `IStorageService.SetAsync` call sites under `Shell/State/`.

## Open Items

None.
